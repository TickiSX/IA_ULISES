/*
 * =====================================================================================
 *
 * Filename:  VisionCone.cs
 *
 * Description:  Implementa la lógica de un cono de visión para un agente de IA.
 *
 * Authors:  Carlos Hernán González
 * Cesar Sasia Zayas
 *
 * Materia:  Inteligencia Artificial e Ingeniería del Conocimiento
 *
 *
 * =====================================================================================
 */

// VisionCone.cs
using UnityEngine;
using System.Collections.Generic; // Para listas de GameObjects detectados

public class VisionCone : MonoBehaviour
{

    public Transform agentTransform;

    // ================================================================
    // Variables configurables por el diseñador (expuestas en el editor)
    // ================================================================

    [Header("Configuración del Cono de Visión")] // 
    [Tooltip("Radio máximo de detección del cono.")]
    [Range(1f, 50f)] // Puedes ajustar los rangos según tu juego
    public float visionRange = 10f;

    [Tooltip("Ángulo de visión en grados (desde el centro hacia cada lado).")]
    [Range(0f, 180f)]
    public float visionAngle = 60f; // 60 grados significa un cono total de 120 grados

    [Tooltip("Capas de GameObjects que el cono de visión puede detectar.")]
    public LayerMask targetLayers;

    [Tooltip("Transform del GameObject objetivo a perseguir.")]
    public Transform targetGameObject; // Lo puedes asignar directamente o detectarlo

    [Header("Configuración Visual del Cono")]
    [Tooltip("Color del cono cuando no hay objetivo detectado.")]
    public Color noTargetColor = Color.green;

    [Tooltip("Color del cono cuando se detecta un objetivo.")]
    public Color targetDetectedColor = Color.red;

    [Tooltip("Número de segmentos para dibujar el arco del cono visualmente.")]
    [Range(8, 64)]
    public int coneSegments = 32;

    // ================================================================
    // Variables privadas (para uso interno del script)
    // ================================================================
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh visionMesh;
    private bool targetIsDetected = false;

    // Referencias para el Steering Behavior
    private UnityEngine.AI.NavMeshAgent navMeshAgent; // Si usas NavMesh
    // O si usas un script de Steering específico (e.g., Pursue.cs)
    // private Pursue pursueBehavior;

    // ================================================================
    // Métodos de Unity
    // ================================================================

    void Awake()
    {
        // Inicializa el NavMeshAgent si lo vas a usar para perseguir
        navMeshAgent = agentTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogWarning("VisionCone: NavMeshAgent no encontrado en el Agente. La persecución no funcionará sin él.");
        }

        // Configuración para la representación visual del cono (Mesh)
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Standard")); // O un Shader de tu preferencia
        }

        visionMesh = new Mesh();
        meshFilter.mesh = visionMesh;
        UpdateVisionMeshColor(noTargetColor); // Color inicial
    }

    void Update()
    {
        DetectTarget();
        UpdateVisionConeVisuals();

        // Si se detecta un objetivo y se ha asignado un NavMeshAgent, iniciar persecución
        if (targetIsDetected && targetGameObject != null && navMeshAgent != null)
        {
            // Implementa el Steering Behavior de persecución
            // Si usas NavMeshAgent, simplemente le das el destino
            navMeshAgent.SetDestination(targetGameObject.position);
            // Si usas otro Steering Behavior, llama a su método de actualización aquí
            // pursueBehavior.SetTarget(targetGameObject);
            // pursueBehavior.UpdateBehavior();
        }
    }

    // ================================================================
    // Lógica del Cono de Visión
    // ================================================================

    private void DetectTarget()
    {
        targetIsDetected = false; // Asume que no hay objetivo detectado al inicio de cada frame

        // Usa Physics.OverlapSphere para obtener todos los colliders en el rango
        // Luego, filtra por ángulo de visión y por la LayerMask
        Collider[] hitColliders = Physics.OverlapSphere(agentTransform.position, visionRange, targetLayers);

        foreach (var hitCollider in hitColliders)
        {
            // Asegúrate de que el collider no sea el propio agente
            if (hitCollider.gameObject == gameObject) continue;

            Vector3 directionToTarget = (hitCollider.transform.position - agentTransform.position).normalized;
            float angleToTarget = Vector3.Angle(agentTransform.forward, directionToTarget);

            // Si el objetivo está dentro del ángulo de visión
            if (angleToTarget < visionAngle)
            {
                // Ahora, una comprobación de línea de visión (raycast) para asegurarse de que no haya obstáculos
                RaycastHit hit;
                if (Physics.Raycast(agentTransform.position, directionToTarget, out hit, visionRange, targetLayers | (1 << LayerMask.NameToLayer("Obstacle"))))
                {
                    // Si el Raycast golpea al objetivo antes que un obstáculo
                    if (hit.collider.gameObject == hitCollider.gameObject)
                    {
                        targetIsDetected = true;
                        targetGameObject = hit.transform; // Asigna el objetivo detectado si no estaba asignado
                        break; // Un objetivo es suficiente para detectar
                    }
                }
            }
        }

        // Actualiza el color visual del cono basándose en si hay un objetivo
        UpdateVisionMeshColor(targetIsDetected ? targetDetectedColor : noTargetColor);
    }

    // ================================================================
    // Representación Visual del Cono
    // ================================================================

    private void UpdateVisionConeVisuals()
    {
        // Limpia el mesh anterior
        visionMesh.Clear();

        // Calcula los vértices del cono
        Vector3[] vertices = new Vector3[coneSegments + 2];
        Vector3[] normals = new Vector3[vertices.Length];
        int[] triangles = new int[(coneSegments) * 3];

        vertices[0] = Vector3.zero; // El ápice del cono está en la posición del agente
        normals[0] = Vector3.up; // O la dirección que desees para las normales

        float currentAngle = -visionAngle; // Empezar desde el borde izquierdo del cono
        for (int i = 0; i <= coneSegments; i++)
        {
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 direction = rotation * Vector3.forward; // Dirección relativa al forward del agente
            vertices[i + 1] = direction * visionRange;
            normals[i + 1] = Vector3.up; // Asegúrate de que las normales estén correctas

            if (i < coneSegments)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            currentAngle += (visionAngle * 2 / coneSegments); // Incrementa el ángulo
        }

        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.normals = normals; // Asigna las normales
        visionMesh.RecalculateBounds(); // Recalcula los límites para que el renderizado sea correcto
    }

    private void UpdateVisionMeshColor(Color color)
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
    }

    // Método para dibujar Gizmos en el editor (opcional, pero útil para depuración)
    void OnDrawGizmos()
    {
        // Dibuja el rango de visión
        Gizmos.color = targetIsDetected ? targetDetectedColor : noTargetColor;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Dibuja el cono de visión
        Vector3 fovLine1 = Quaternion.Euler(0, visionAngle, 0) * transform.forward * visionRange;
        Vector3 fovLine2 = Quaternion.Euler(0, -visionAngle, 0) * transform.forward * visionRange;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
        // 

        // Dibuja el arco del cono
        if (Application.isPlaying)
        { // Dibuja el Mesh Renderer solo en juego para evitar conflictos visuales en editor
            // No se dibuja directamente con Gizmos en tiempo de ejecución de esta manera
            // El Mesh Renderer ya se encarga de la visualización en juego
        }
        else
        {
            // Dibuja el arco con Gizmos en el editor
            float currentAngle = -visionAngle;
            Vector3 previousPoint = transform.position + Quaternion.Euler(0, currentAngle, 0) * transform.forward * visionRange;
            for (int i = 0; i <= coneSegments; i++)
            {
                Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
                Vector3 currentPoint = transform.position + rotation * transform.forward * visionRange;
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
                currentAngle += (visionAngle * 2 / coneSegments);
            }
        }
    }

    // ================================================================
    // Recursos Consultados / Uso de IA
    // ================================================================
    /*
    // Videos de Referencia:
    // "Unity 3D AI Tutorial - Vision Cone" (similar a: https://youtu.be/lV47ED8h61k?si=6m012cxUMIkJvd5z)
    // "How to Make an AI Vision Cone in Unity" (similar a: https://youtu.be/j1-OyLo77ss?si=7B92T-AVf7LUOJh2)

    // Consultas a IA (Ejemplo - sé honesto con lo que preguntaste):
    // ChatGPT: "¿Cómo puedo dibujar un cono 3D con Mesh en Unity para un campo de visión?"
    // ChatGPT: "¿Cuál es la mejor manera de implementar un Steering Behavior de persecución con NavMeshAgent en Unity?"
    */
}