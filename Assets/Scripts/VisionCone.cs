/*
 * =====================================================================================
 *
 * Filename:  VisionCone.cs
 *
 * Description:  Implementa la l�gica de un cono de visi�n para un agente de IA,
 * permiti�ndole detectar objetivos dentro de un rango y �ngulo definidos,
 * verificando la l�nea de visi�n y aplicando un comportamiento de persecuci�n.
 *
 * Authors:  Carlos Hern�n Gonz�lez
 * Cesar Sasia Zayas
 *
 * Materia:  Inteligencia Artificial e Ingenier�a del Conocimiento
 *
 * =====================================================================================
 */

using UnityEngine;
using System.Collections.Generic; // Necesario para colecciones, aunque en este script no se usa directamente para listas de objetos detectados, es una buena pr�ctica incluirlo si se prev� su uso.

/// <summary>
/// La clase VisionCone permite a un GameObject (el agente) detectar otros GameObjects
/// dentro de un �rea c�nica de visi�n. Tambi�n proporciona una representaci�n visual
/// de este cono y activa un comportamiento de persecuci�n al detectar un objetivo.
/// Este script debe adjuntarse a un GameObject hijo del agente principal para que
/// el cuerpo del agente permanezca visible.
/// </summary>
public class VisionCone : MonoBehaviour
{
    // Una referencia al 'Transform' del GameObject principal que act�a como el agente.
    // Es crucial para obtener la posici�n y direcci�n del agente para la detecci�n y movimiento.
    // Debe asignarse en el Editor de Unity.
    public Transform agentTransform;

    // ================================================================
    // Variables configurables por el dise�ador (expuestas en el editor de Unity)
    // Estas variables pueden ser ajustadas f�cilmente sin modificar el c�digo.
    // ================================================================

    [Header("Configuraci�n del Cono de Visi�n")] // T�tulo en el Inspector para organizar las variables.
    [Tooltip("Define el radio m�ximo hasta donde el cono de visi�n puede detectar un objetivo.")]
    [Range(1f, 50f)] // Limita el valor en el editor para un control m�s f�cil (slider).
    public float visionRange = 10f; // Distancia m�xima de detecci�n.

    [Tooltip("Define el �ngulo de visi�n del cono en grados. Este valor es desde el centro hacia cada lado. Un valor de 60f resultar� en un cono total de 120 grados (60 a la izquierda + 60 a la derecha).")]
    [Range(0f, 180f)] // Limita el valor del �ngulo.
    public float visionAngle = 60f; // Mitad del �ngulo total del cono.

    [Tooltip("M�scara de capas (LayerMask) que especifica qu� capas de GameObjects pueden ser detectadas por el cono de visi�n. Es vital para filtrar objetos irrelevantes.")]
    public LayerMask targetLayers; // Las capas que el agente debe intentar detectar.

    [Tooltip("Referencia al Transform del GameObject que ha sido detectado y que el agente debe perseguir. Se asigna autom�ticamente una vez que un objetivo es detectado.")]
    public Transform targetGameObject; // El objetivo actual a perseguir.

    [Header("Configuraci�n Visual del Cono")] // T�tulo para las opciones visuales.
    [Tooltip("El color que tendr� el cono de visi�n cuando NO haya un objetivo detectado.")]
    public Color noTargetColor = Color.green; // Color por defecto cuando no hay objetivo.

    [Tooltip("El color que tendr� el cono de visi�n cuando S� haya un objetivo detectado dentro de su campo de visi�n.")]
    public Color targetDetectedColor = Color.red; // Color cuando se detecta un objetivo.

    [Tooltip("N�mero de segmentos utilizados para construir el Mesh visual del cono. Un valor m�s alto hace el cono m�s suave, pero requiere m�s recursos.")]
    [Range(8, 64)] // Rango para el n�mero de segmentos del cono.
    public int coneSegments = 32; // Detalle de la malla del cono.

    // ================================================================
    // Variables privadas (para uso interno del script y gesti�n de componentes)
    // ================================================================
    private MeshFilter meshFilter;       // Componente para almacenar la malla (forma 3D) del cono.
    private MeshRenderer meshRenderer;   // Componente para renderizar la malla con un material y color.
    private Mesh visionMesh;             // La malla generada din�micamente que representa el cono de visi�n.
    private bool targetIsDetected = false; // Bandera que indica si un objetivo est� actualmente dentro del cono de visi�n.

    // Referencia al componente NavMeshAgent del agente principal.
    // Este componente se encarga de la navegaci�n aut�noma y el "Steering Behavior" de persecuci�n.
    private UnityEngine.AI.NavMeshAgent navMeshAgent;

    // ================================================================
    // M�todos de Unity (Ciclo de vida del script)
    // ================================================================

    /// <summary>
    /// Se llama una vez al inicio cuando el script est� habilitado, incluso antes de Start().
    /// Ideal para inicializar referencias a componentes y configurar la visualizaci�n del cono.
    /// </summary>
    void Awake()
    {
        // Obtiene el componente NavMeshAgent del 'agentTransform' (el GameObject padre).
        // Si no se encuentra, se muestra una advertencia, ya que la persecuci�n no funcionar�.
        navMeshAgent = agentTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogWarning("VisionCone: NavMeshAgent no encontrado en el 'agentTransform'. La persecuci�n no funcionar� sin �l.");
        }

        // Configuraci�n para la representaci�n visual del cono (Mesh).
        // Se aseguran de que el GameObject que contiene este script tenga un MeshFilter y un MeshRenderer.
        // Si no existen, se a�aden autom�ticamente.
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            // Asigna un material b�sico 'Standard' para que el cono sea visible.
            // Se puede reemplazar con un material personalizado para efectos visuales avanzados (ej. transparencia).
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }

        // Crea una nueva malla que ser� la forma del cono de visi�n.
        visionMesh = new Mesh();
        meshFilter.mesh = visionMesh; // Asigna la malla reci�n creada al MeshFilter.
        UpdateVisionMeshColor(noTargetColor); // Establece el color inicial del cono (sin detecci�n).
    }

    /// <summary>
    /// Se llama una vez por cada frame. Aqu� se ejecuta la l�gica principal del cono de visi�n:
    /// detecci�n de objetivos, actualizaci�n visual y activaci�n del comportamiento de persecuci�n.
    /// </summary>
    void Update()
    {
        DetectTarget();            // Llama a la funci�n que busca objetivos.
        UpdateVisionConeVisuals(); // Llama a la funci�n que actualiza la forma y color del cono.

        // Condici�n para iniciar la persecuci�n:
        // 1. Se ha detectado un objetivo (targetIsDetected es true).
        // 2. Se ha asignado una referencia al objetivo (targetGameObject no es nulo).
        // 3. El agente tiene un NavMeshAgent para moverse.
        if (targetIsDetected && targetGameObject != null && navMeshAgent != null)
        {
            // Implementa el Steering Behavior de persecuci�n.
            // El NavMeshAgent calcula autom�ticamente el camino m�s corto hacia el objetivo
            // y mueve al agente hacia esa posici�n.
            navMeshAgent.SetDestination(targetGameObject.position);
            // Si se usaran otros Steering Behaviors personalizados (ej. 'Pursue' manual),
            // se llamar�an aqu� los m�todos correspondientes.
        }
    }

    // ================================================================
    // L�gica del Cono de Visi�n (Detecci�n de objetivos)
    // ================================================================

    /// <summary>
    /// Este m�todo es el coraz�n de la detecci�n. Utiliza Physics.OverlapSphere
    /// para encontrar GameObjects en el rango, luego los filtra por �ngulo de visi�n
    /// y realiza un Raycast para asegurar la l�nea de visi�n sin obst�culos.
    /// </summary>
    private void DetectTarget()
    {
        targetIsDetected = false; // Restablece la bandera al inicio de cada frame. Asumimos que no hay objetivo.
        targetGameObject = null; // Limpia el objetivo detectado si no se encuentra uno nuevo.

        // Paso 1: Detecci�n inicial dentro de una esfera.
        // Physics.OverlapSphere devuelve todos los colliders dentro de un radio,
        // filtrando por las capas especificadas en 'targetLayers'.
        Collider[] hitColliders = Physics.OverlapSphere(agentTransform.position, visionRange, targetLayers);

        // Itera sobre todos los colliders encontrados para evaluarlos individualmente.
        foreach (var hitCollider in hitColliders)
        {
            // Asegura que el collider encontrado no sea el propio GameObject que tiene el cono visual,
            // o el propio agente, para evitar que el agente se detecte a s� mismo.
            if (hitCollider.gameObject == agentTransform.gameObject || hitCollider.gameObject == gameObject) continue;

            // Calcula la direcci�n desde el agente hacia el posible objetivo.
            Vector3 directionToTarget = (hitCollider.transform.position - agentTransform.position).normalized;
            // Calcula el �ngulo entre la direcci�n frontal del agente y la direcci�n hacia el objetivo.
            float angleToTarget = Vector3.Angle(agentTransform.forward, directionToTarget);

            // Paso 2: Filtrar por �ngulo de visi�n.
            // Si el objetivo est� dentro del �ngulo definido para el cono.
            if (angleToTarget < visionAngle)
            {
                // Paso 3: Comprobaci�n de l�nea de visi�n (Raycast).
                // Lanza un rayo desde el agente hacia el objetivo para verificar que no haya obst�culos
                // entre ellos.
                RaycastHit hit;
                // El Raycast puede golpear 'targetLayers' (el objetivo) o la capa "Obstacle".
                // Se usa el operador '|' (OR a nivel de bits) para combinar las LayerMasks.
                // (1 << LayerMask.NameToLayer("Obstacle")) crea una LayerMask para una capa espec�fica por su nombre.
                if (Physics.Raycast(agentTransform.position, directionToTarget, out hit, visionRange, targetLayers | (1 << LayerMask.NameToLayer("Obstacle"))))
                {
                    // Si el Raycast golpea algo...
                    // Comprueba si el primer objeto golpeado por el Raycast es realmente el objetivo que estamos buscando.
                    // Esto asegura que una pared (obst�culo) no est� bloqueando la visi�n.
                    if (hit.collider.gameObject == hitCollider.gameObject)
                    {
                        targetIsDetected = true;        // �Objetivo detectado!
                        targetGameObject = hit.transform; // Asigna el Transform del objetivo detectado.
                        break;                          // Sale del bucle, ya que un objetivo es suficiente.
                    }
                }
            }
        }

        // Una vez finalizada la detecci�n para este frame, actualiza el color del cono visual
        // bas�ndose en si se ha detectado o no un objetivo.
        UpdateVisionMeshColor(targetIsDetected ? targetDetectedColor : noTargetColor);
    }

    // ================================================================
    // Representaci�n Visual del Cono (Generaci�n y actualizaci�n de la malla)
    // ================================================================

    /// <summary>
    /// Construye y actualiza la malla 3D que representa el cono de visi�n.
    /// Esto permite una visualizaci�n din�mica en el juego.
    /// </summary>
    private void UpdateVisionConeVisuals()
    {
        visionMesh.Clear(); // Limpia la malla anterior para dibujar una nueva.

        // Calcula los v�rtices (puntos 3D) que formar�n el cono.
        // El cono tendr� un �pice (el punto de origen) + (coneSegments + 1) puntos en el arco exterior.
        Vector3[] vertices = new Vector3[coneSegments + 2];
        Vector3[] normals = new Vector3[vertices.Length]; // Normales para el sombreado de la malla.
        int[] triangles = new int[(coneSegments) * 3];   // �ndices de los v�rtices que forman los tri�ngulos de la malla.

        vertices[0] = Vector3.zero; // El primer v�rtice es el �pice del cono (en la posici�n local del GameObject del cono).
        normals[0] = Vector3.up;    // Normal del �pice.

        float currentAngle = -visionAngle; // Comienza desde el borde izquierdo del �ngulo de visi�n total.
        for (int i = 0; i <= coneSegments; i++)
        {
            // Calcula la rotaci�n para cada segmento del arco del cono.
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            // Calcula la direcci�n del v�rtice del arco, relativa al forward del GameObject del cono.
            Vector3 direction = rotation * Vector3.forward;
            // Establece el v�rtice en el borde del rango de visi�n.
            vertices[i + 1] = direction * visionRange;
            normals[i + 1] = Vector3.up; // Normales para los v�rtices del arco.

            // Si no es el �ltimo segmento, crea un tri�ngulo conectando el �pice y dos v�rtices adyacentes del arco.
            if (i < coneSegments)
            {
                triangles[i * 3] = 0;       // El �pice.
                triangles[i * 3 + 1] = i + 1; // El primer v�rtice del segmento del arco.
                triangles[i * 3 + 2] = i + 2; // El segundo v�rtice del segmento del arco.
            }

            // Incrementa el �ngulo para el siguiente segmento del arco.
            currentAngle += (visionAngle * 2 / coneSegments);
        }

        // Asigna los v�rtices, tri�ngulos y normales a la malla.
        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.normals = normals;
        visionMesh.RecalculateBounds(); // Recalcula los l�mites de la malla para un renderizado y oclusi�n correctos.
    }

    /// <summary>
    /// Actualiza el color del material del cono de visi�n.
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
    /// Esto es extremadamente �til para depurar y visualizar el rango y �ngulo del cono
    /// sin necesidad de ejecutar el juego.
    /// </summary>
    void OnDrawGizmos()
    {
        // Se asegura de que la variable agentTransform est� asignada para evitar errores en el editor.
        if (agentTransform == null) return;

        // Establece el color de los Gizmos seg�n si hay un objetivo detectado.
        Gizmos.color = targetIsDetected ? targetDetectedColor : noTargetColor;

        // Dibuja una esfera transparente para visualizar el rango de detecci�n total.
        Gizmos.DrawWireSphere(agentTransform.position, visionRange);

        // Dibuja las dos l�neas que definen los bordes exteriores del cono de visi�n.
        // Calcula la direcci�n de la primera l�nea (visionAngle a la derecha del forward).
        Vector3 fovLine1 = Quaternion.Euler(0, visionAngle, 0) * agentTransform.forward * visionRange;
        // Calcula la direcci�n de la segunda l�nea (visionAngle a la izquierda del forward).
        Vector3 fovLine2 = Quaternion.Euler(0, -visionAngle, 0) * agentTransform.forward * visionRange;
        // Dibuja las l�neas desde la posici�n del agente.
        Gizmos.DrawRay(agentTransform.position, fovLine1);
        Gizmos.DrawRay(agentTransform.position, fovLine2);

        // Dibuja el arco que conecta las dos l�neas del cono, creando una forma m�s completa del abanico.
        // Este se dibuja solo en el editor (cuando el juego no est� en ejecuci�n) para no duplicar
        // la visualizaci�n del MeshRenderer durante el juego.
        if (!Application.isPlaying)
        {
            float currentAngle = -visionAngle; // Comienza desde el borde izquierdo del �ngulo.
            // Calcula el primer punto del arco.
            Vector3 previousPoint = agentTransform.position + Quaternion.Euler(0, currentAngle, 0) * agentTransform.forward * visionRange;
            for (int i = 0; i <= coneSegments; i++)
            {
                // Calcula la rotaci�n para cada segmento del arco.
                Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
                // Calcula el punto actual en el arco.
                Vector3 currentPoint = agentTransform.position + rotation * agentTransform.forward * visionRange;
                // Dibuja una l�nea entre el punto anterior y el actual para formar el arco.
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint; // Actualiza el punto anterior para la siguiente iteraci�n.
                currentAngle += (visionAngle * 2 / coneSegments); // Incrementa el �ngulo.
            }
        }
    }

    // ================================================================
    // Secci�n de Referencias
    // ================================================================
    /*
    // Videos de Referencia:
    // Estos videos fueron consultados para entender los conceptos de implementaci�n de
    // conos de visi�n en Unity y la visualizaci�n de formas geom�tricas con Mesh.
    // - "Unity 3D AI Tutorial - Vision Cone": https://youtu.be/lV47ED8h61k?si=6m012cxUMIkJvd5z
    // - "How to Make an AI Vision Cone in Unity": https://youtu.be/j1-OyLo77ss?si=7B92T-AVF7LUOJh2

    // Consultas a Herramientas de Inteligencia Artificial (IA Generativa):
    // Se utilizaron herramientas de IA para resolver dudas espec�ficas y obtener orientaci�n en la estructuraci�n del c�digo y la depuraci�n.
    // - Pregunta a Gemini/ChatGPT: "�C�mo puedo dibujar un cono 3D con Mesh en Unity para un campo de visi�n?"
    // - Pregunta a Gemini/ChatGPT: "�Cu�l es la mejor manera de implementar un Steering Behavior de persecuci�n con NavMeshAgent en Unity?"
    // - Pregunta a Gemini/ChatGPT: "Ay�dame a depurar el script VisionCone en Unity, el agente no persigue al objetivo."
    // - Pregunta a Gemini/ChatGPT: "Expl�came el uso de LayerMasks y Physics.Raycast con operadores de bits en Unity."
    // - Pregunta a Gemini/ChatGPT: "C�mo a�adir comentarios extensivos a mi c�digo de Unity para un profesor."
    */
}