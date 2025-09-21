/*
 * =====================================================================================
 *
 * Filename:  VisionCone.cs
 *
 * Description:  Implementa la lógica de un cono de visión para un agente de IA,
 * permitiéndole detectar objetivos dentro de un rango y ángulo definidos,
 * verificando la línea de visión y aplicando un comportamiento de persecución.
 *
 * Authors:  Carlos Hernán González
 * Cesar Sasia Zayas
 *
 * Materia:  Inteligencia Artificial e Ingeniería del Conocimiento
 *
 * =====================================================================================
 */

using UnityEngine;
using System.Collections.Generic; // Necesario para colecciones, aunque en este script no se usa directamente para listas de objetos detectados, es una buena práctica incluirlo si se prevé su uso.

/// <summary>
/// La clase VisionCone permite a un GameObject (el agente) detectar otros GameObjects
/// dentro de un área cónica de visión. También proporciona una representación visual
/// de este cono y activa un comportamiento de persecución al detectar un objetivo.
/// Este script debe adjuntarse a un GameObject hijo del agente principal para que
/// el cuerpo del agente permanezca visible.
/// </summary>
public class VisionCone : MonoBehaviour
{
    // Una referencia al 'Transform' del GameObject principal que actúa como el agente.
    // Es crucial para obtener la posición y dirección del agente para la detección y movimiento.
    // Debe asignarse en el Editor de Unity.
    public Transform agentTransform;

    // ================================================================
    // Variables configurables por el diseñador (expuestas en el editor de Unity)
    // Estas variables pueden ser ajustadas fácilmente sin modificar el código.
    // ================================================================

    [Header("Configuración del Cono de Visión")] // Título en el Inspector para organizar las variables.
    [Tooltip("Define el radio máximo hasta donde el cono de visión puede detectar un objetivo.")]
    [Range(1f, 50f)] // Limita el valor en el editor para un control más fácil (slider).
    public float visionRange = 10f; // Distancia máxima de detección.

    [Tooltip("Define el ángulo de visión del cono en grados. Este valor es desde el centro hacia cada lado. Un valor de 60f resultará en un cono total de 120 grados (60 a la izquierda + 60 a la derecha).")]
    [Range(0f, 180f)] // Limita el valor del ángulo.
    public float visionAngle = 60f; // Mitad del ángulo total del cono.

    [Tooltip("Máscara de capas (LayerMask) que especifica qué capas de GameObjects pueden ser detectadas por el cono de visión. Es vital para filtrar objetos irrelevantes.")]
    public LayerMask targetLayers; // Las capas que el agente debe intentar detectar.

    [Tooltip("Referencia al Transform del GameObject que ha sido detectado y que el agente debe perseguir. Se asigna automáticamente una vez que un objetivo es detectado.")]
    public Transform targetGameObject; // El objetivo actual a perseguir.

    [Header("Configuración Visual del Cono")] // Título para las opciones visuales.
    [Tooltip("El color que tendrá el cono de visión cuando NO haya un objetivo detectado.")]
    public Color noTargetColor = Color.green; // Color por defecto cuando no hay objetivo.

    [Tooltip("El color que tendrá el cono de visión cuando SÍ haya un objetivo detectado dentro de su campo de visión.")]
    public Color targetDetectedColor = Color.red; // Color cuando se detecta un objetivo.

    [Tooltip("Número de segmentos utilizados para construir el Mesh visual del cono. Un valor más alto hace el cono más suave, pero requiere más recursos.")]
    [Range(8, 64)] // Rango para el número de segmentos del cono.
    public int coneSegments = 32; // Detalle de la malla del cono.

    // ================================================================
    // Variables privadas (para uso interno del script y gestión de componentes)
    // ================================================================
    private MeshFilter meshFilter;       // Componente para almacenar la malla (forma 3D) del cono.
    private MeshRenderer meshRenderer;   // Componente para renderizar la malla con un material y color.
    private Mesh visionMesh;             // La malla generada dinámicamente que representa el cono de visión.
    private bool targetIsDetected = false; // Bandera que indica si un objetivo está actualmente dentro del cono de visión.

    // Referencia al componente NavMeshAgent del agente principal.
    // Este componente se encarga de la navegación autónoma y el "Steering Behavior" de persecución.
    private UnityEngine.AI.NavMeshAgent navMeshAgent;

    // ================================================================
    // Métodos de Unity (Ciclo de vida del script)
    // ================================================================

    /// <summary>
    /// Se llama una vez al inicio cuando el script está habilitado, incluso antes de Start().
    /// Ideal para inicializar referencias a componentes y configurar la visualización del cono.
    /// </summary>
    void Awake()
    {
        // Obtiene el componente NavMeshAgent del 'agentTransform' (el GameObject padre).
        // Si no se encuentra, se muestra una advertencia, ya que la persecución no funcionará.
        navMeshAgent = agentTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogWarning("VisionCone: NavMeshAgent no encontrado en el 'agentTransform'. La persecución no funcionará sin él.");
        }

        // Configuración para la representación visual del cono (Mesh).
        // Se aseguran de que el GameObject que contiene este script tenga un MeshFilter y un MeshRenderer.
        // Si no existen, se añaden automáticamente.
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            // Asigna un material básico 'Standard' para que el cono sea visible.
            // Se puede reemplazar con un material personalizado para efectos visuales avanzados (ej. transparencia).
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }

        // Crea una nueva malla que será la forma del cono de visión.
        visionMesh = new Mesh();
        meshFilter.mesh = visionMesh; // Asigna la malla recién creada al MeshFilter.
        UpdateVisionMeshColor(noTargetColor); // Establece el color inicial del cono (sin detección).
    }

    /// <summary>
    /// Se llama una vez por cada frame. Aquí se ejecuta la lógica principal del cono de visión:
    /// detección de objetivos, actualización visual y activación del comportamiento de persecución.
    /// </summary>
    void Update()
    {
        DetectTarget();            // Llama a la función que busca objetivos.
        UpdateVisionConeVisuals(); // Llama a la función que actualiza la forma y color del cono.

        // Condición para iniciar la persecución:
        // 1. Se ha detectado un objetivo (targetIsDetected es true).
        // 2. Se ha asignado una referencia al objetivo (targetGameObject no es nulo).
        // 3. El agente tiene un NavMeshAgent para moverse.
        if (targetIsDetected && targetGameObject != null && navMeshAgent != null)
        {
            // Implementa el Steering Behavior de persecución.
            // El NavMeshAgent calcula automáticamente el camino más corto hacia el objetivo
            // y mueve al agente hacia esa posición.
            navMeshAgent.SetDestination(targetGameObject.position);
            // Si se usaran otros Steering Behaviors personalizados (ej. 'Pursue' manual),
            // se llamarían aquí los métodos correspondientes.
        }
    }

    // ================================================================
    // Lógica del Cono de Visión (Detección de objetivos)
    // ================================================================

    /// <summary>
    /// Este método es el corazón de la detección. Utiliza Physics.OverlapSphere
    /// para encontrar GameObjects en el rango, luego los filtra por ángulo de visión
    /// y realiza un Raycast para asegurar la línea de visión sin obstáculos.
    /// </summary>
    private void DetectTarget()
    {
        targetIsDetected = false; // Restablece la bandera al inicio de cada frame. Asumimos que no hay objetivo.
        targetGameObject = null; // Limpia el objetivo detectado si no se encuentra uno nuevo.

        // Paso 1: Detección inicial dentro de una esfera.
        // Physics.OverlapSphere devuelve todos los colliders dentro de un radio,
        // filtrando por las capas especificadas en 'targetLayers'.
        Collider[] hitColliders = Physics.OverlapSphere(agentTransform.position, visionRange, targetLayers);

        // Itera sobre todos los colliders encontrados para evaluarlos individualmente.
        foreach (var hitCollider in hitColliders)
        {
            // Asegura que el collider encontrado no sea el propio GameObject que tiene el cono visual,
            // o el propio agente, para evitar que el agente se detecte a sí mismo.
            if (hitCollider.gameObject == agentTransform.gameObject || hitCollider.gameObject == gameObject) continue;

            // Calcula la dirección desde el agente hacia el posible objetivo.
            Vector3 directionToTarget = (hitCollider.transform.position - agentTransform.position).normalized;
            // Calcula el ángulo entre la dirección frontal del agente y la dirección hacia el objetivo.
            float angleToTarget = Vector3.Angle(agentTransform.forward, directionToTarget);

            // Paso 2: Filtrar por ángulo de visión.
            // Si el objetivo está dentro del ángulo definido para el cono.
            if (angleToTarget < visionAngle)
            {
                // Paso 3: Comprobación de línea de visión (Raycast).
                // Lanza un rayo desde el agente hacia el objetivo para verificar que no haya obstáculos
                // entre ellos.
                RaycastHit hit;
                // El Raycast puede golpear 'targetLayers' (el objetivo) o la capa "Obstacle".
                // Se usa el operador '|' (OR a nivel de bits) para combinar las LayerMasks.
                // (1 << LayerMask.NameToLayer("Obstacle")) crea una LayerMask para una capa específica por su nombre.
                if (Physics.Raycast(agentTransform.position, directionToTarget, out hit, visionRange, targetLayers | (1 << LayerMask.NameToLayer("Obstacle"))))
                {
                    // Si el Raycast golpea algo...
                    // Comprueba si el primer objeto golpeado por el Raycast es realmente el objetivo que estamos buscando.
                    // Esto asegura que una pared (obstáculo) no esté bloqueando la visión.
                    if (hit.collider.gameObject == hitCollider.gameObject)
                    {
                        targetIsDetected = true;        // ¡Objetivo detectado!
                        targetGameObject = hit.transform; // Asigna el Transform del objetivo detectado.
                        break;                          // Sale del bucle, ya que un objetivo es suficiente.
                    }
                }
            }
        }

        // Una vez finalizada la detección para este frame, actualiza el color del cono visual
        // basándose en si se ha detectado o no un objetivo.
        UpdateVisionMeshColor(targetIsDetected ? targetDetectedColor : noTargetColor);
    }

    // ================================================================
    // Representación Visual del Cono (Generación y actualización de la malla)
    // ================================================================

    /// <summary>
    /// Construye y actualiza la malla 3D que representa el cono de visión.
    /// Esto permite una visualización dinámica en el juego.
    /// </summary>
    private void UpdateVisionConeVisuals()
    {
        visionMesh.Clear(); // Limpia la malla anterior para dibujar una nueva.

        // Calcula los vértices (puntos 3D) que formarán el cono.
        // El cono tendrá un ápice (el punto de origen) + (coneSegments + 1) puntos en el arco exterior.
        Vector3[] vertices = new Vector3[coneSegments + 2];
        Vector3[] normals = new Vector3[vertices.Length]; // Normales para el sombreado de la malla.
        int[] triangles = new int[(coneSegments) * 3];   // Índices de los vértices que forman los triángulos de la malla.

        vertices[0] = Vector3.zero; // El primer vértice es el ápice del cono (en la posición local del GameObject del cono).
        normals[0] = Vector3.up;    // Normal del ápice.

        float currentAngle = -visionAngle; // Comienza desde el borde izquierdo del ángulo de visión total.
        for (int i = 0; i <= coneSegments; i++)
        {
            // Calcula la rotación para cada segmento del arco del cono.
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            // Calcula la dirección del vértice del arco, relativa al forward del GameObject del cono.
            Vector3 direction = rotation * Vector3.forward;
            // Establece el vértice en el borde del rango de visión.
            vertices[i + 1] = direction * visionRange;
            normals[i + 1] = Vector3.up; // Normales para los vértices del arco.

            // Si no es el último segmento, crea un triángulo conectando el ápice y dos vértices adyacentes del arco.
            if (i < coneSegments)
            {
                triangles[i * 3] = 0;       // El ápice.
                triangles[i * 3 + 1] = i + 1; // El primer vértice del segmento del arco.
                triangles[i * 3 + 2] = i + 2; // El segundo vértice del segmento del arco.
            }

            // Incrementa el ángulo para el siguiente segmento del arco.
            currentAngle += (visionAngle * 2 / coneSegments);
        }

        // Asigna los vértices, triángulos y normales a la malla.
        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.normals = normals;
        visionMesh.RecalculateBounds(); // Recalcula los límites de la malla para un renderizado y oclusión correctos.
    }

    /// <summary>
    /// Actualiza el color del material del cono de visión.
    /// </summary>
    /// <param name="color">El nuevo color a aplicar al cono.</param>
    private void UpdateVisionMeshColor(Color color)
    {
        // Asegura que el MeshRenderer y su material existan antes de intentar cambiar el color.
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
    }

    /// <summary>
    /// Dibuja ayudas visuales (Gizmos) en el editor de Unity.
    /// Esto es extremadamente útil para depurar y visualizar el rango y ángulo del cono
    /// sin necesidad de ejecutar el juego.
    /// </summary>
    void OnDrawGizmos()
    {
        // Se asegura de que la variable agentTransform esté asignada para evitar errores en el editor.
        if (agentTransform == null) return;

        // Establece el color de los Gizmos según si hay un objetivo detectado.
        Gizmos.color = targetIsDetected ? targetDetectedColor : noTargetColor;

        // Dibuja una esfera transparente para visualizar el rango de detección total.
        Gizmos.DrawWireSphere(agentTransform.position, visionRange);

        // Dibuja las dos líneas que definen los bordes exteriores del cono de visión.
        // Calcula la dirección de la primera línea (visionAngle a la derecha del forward).
        Vector3 fovLine1 = Quaternion.Euler(0, visionAngle, 0) * agentTransform.forward * visionRange;
        // Calcula la dirección de la segunda línea (visionAngle a la izquierda del forward).
        Vector3 fovLine2 = Quaternion.Euler(0, -visionAngle, 0) * agentTransform.forward * visionRange;
        // Dibuja las líneas desde la posición del agente.
        Gizmos.DrawRay(agentTransform.position, fovLine1);
        Gizmos.DrawRay(agentTransform.position, fovLine2);

        // Dibuja el arco que conecta las dos líneas del cono, creando una forma más completa del abanico.
        // Este se dibuja solo en el editor (cuando el juego no está en ejecución) para no duplicar
        // la visualización del MeshRenderer durante el juego.
        if (!Application.isPlaying)
        {
            float currentAngle = -visionAngle; // Comienza desde el borde izquierdo del ángulo.
            // Calcula el primer punto del arco.
            Vector3 previousPoint = agentTransform.position + Quaternion.Euler(0, currentAngle, 0) * agentTransform.forward * visionRange;
            for (int i = 0; i <= coneSegments; i++)
            {
                // Calcula la rotación para cada segmento del arco.
                Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
                // Calcula el punto actual en el arco.
                Vector3 currentPoint = agentTransform.position + rotation * agentTransform.forward * visionRange;
                // Dibuja una línea entre el punto anterior y el actual para formar el arco.
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint; // Actualiza el punto anterior para la siguiente iteración.
                currentAngle += (visionAngle * 2 / coneSegments); // Incrementa el ángulo.
            }
        }
    }

    // ================================================================
    // Sección de Referencias
    // ================================================================
    /*
    // Videos de Referencia:
    // Estos videos fueron consultados para entender los conceptos de implementación de
    // conos de visión en Unity y la visualización de formas geométricas con Mesh.
    // - "Unity 3D AI Tutorial - Vision Cone": https://youtu.be/lV47ED8h61k?si=6m012cxUMIkJvd5z
    // - "How to Make an AI Vision Cone in Unity": https://youtu.be/j1-OyLo77ss?si=7B92T-AVF7LUOJh2

    // Consultas a Herramientas de Inteligencia Artificial (IA Generativa):
    // Se utilizaron herramientas de IA para resolver dudas específicas y obtener orientación en la estructuración del código y la depuración.
    // - Pregunta a Gemini/ChatGPT: "¿Cómo puedo dibujar un cono 3D con Mesh en Unity para un campo de visión?"
    // - Pregunta a Gemini/ChatGPT: "¿Cuál es la mejor manera de implementar un Steering Behavior de persecución con NavMeshAgent en Unity?"
    // - Pregunta a Gemini/ChatGPT: "Ayúdame a depurar el script VisionCone en Unity, el agente no persigue al objetivo."
    // - Pregunta a Gemini/ChatGPT: "Explícame el uso de LayerMasks y Physics.Raycast con operadores de bits en Unity."
    // - Pregunta a Gemini/ChatGPT: "Cómo añadir comentarios extensivos a mi código de Unity para un profesor."
    */
}