/*
 * =====================================================================================
 *
 * Filename:  VisionCone.cs
 *
 * Description:  Implementa la l�gica de un cono de visi�n para un agente de IA.
 *
 * Authors:  Carlos Hern�n Gonz�lez
 * Cesar Sasia Zayas
 *
 * Materia:  Inteligencia Artificial e Ingenier�a del Conocimiento
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
    // Variables configurables por el dise�ador (expuestas en el editor)
    // ================================================================

    [Header("Configuraci�n del Cono de Visi�n")] // 
    [Tooltip("Radio m�ximo de detecci�n del cono.")]
    [Range(1f, 50f)] // Puedes ajustar los rangos seg�n tu juego
    public float visionRange = 10f;

    [Tooltip("�ngulo de visi�n en grados (desde el centro hacia cada lado).")]
    [Range(0f, 180f)]
    public float visionAngle = 60f; // 60 grados significa un cono total de 120 grados

    [Tooltip("Capas de GameObjects que el cono de visi�n puede detectar.")]
    public LayerMask targetLayers;

    [Tooltip("Transform del GameObject objetivo a perseguir.")]
    public Transform targetGameObject; // Lo puedes asignar directamente o detectarlo

    [Header("Configuraci�n Visual del Cono")]
    [Tooltip("Color del cono cuando no hay objetivo detectado.")]
    public Color noTargetColor = Color.green;

    [Tooltip("Color del cono cuando se detecta un objetivo.")]
    public Color targetDetectedColor = Color.red;

    [Tooltip("N�mero de segmentos para dibujar el arco del cono visualmente.")]
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
    // O si usas un script de Steering espec�fico (e.g., Pursue.cs)
    // private Pursue pursueBehavior;

    // ================================================================
    // M�todos de Unity
    // ================================================================

    void Awake()
    {
        // Inicializa el NavMeshAgent si lo vas a usar para perseguir
        navMeshAgent = agentTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogWarning("VisionCone: NavMeshAgent no encontrado en el Agente. La persecuci�n no funcionar� sin �l.");
        }

        // Configuraci�n para la representaci�n visual del cono (Mesh)
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

        // Si se detecta un objetivo y se ha asignado un NavMeshAgent, iniciar persecuci�n
        if (targetIsDetected && targetGameObject != null && navMeshAgent != null)
        {
            // Implementa el Steering Behavior de persecuci�n
            // Si usas NavMeshAgent, simplemente le das el destino
            navMeshAgent.SetDestination(targetGameObject.position);
            // Si usas otro Steering Behavior, llama a su m�todo de actualizaci�n aqu�
            // pursueBehavior.SetTarget(targetGameObject);
            // pursueBehavior.UpdateBehavior();
        }
    }

    // ================================================================
    // L�gica del Cono de Visi�n
    // ================================================================

    private void DetectTarget()
    {
        targetIsDetected = false; // Asume que no hay objetivo detectado al inicio de cada frame

        // Usa Physics.OverlapSphere para obtener todos los colliders en el rango
        // Luego, filtra por �ngulo de visi�n y por la LayerMask
        Collider[] hitColliders = Physics.OverlapSphere(agentTransform.position, visionRange, targetLayers);

        foreach (var hitCollider in hitColliders)
        {
            // Aseg�rate de que el collider no sea el propio agente
            if (hitCollider.gameObject == gameObject) continue;

            Vector3 directionToTarget = (hitCollider.transform.position - agentTransform.position).normalized;
            float angleToTarget = Vector3.Angle(agentTransform.forward, directionToTarget);

            // Si el objetivo est� dentro del �ngulo de visi�n
            if (angleToTarget < visionAngle)
            {
                // Ahora, una comprobaci�n de l�nea de visi�n (raycast) para asegurarse de que no haya obst�culos
                RaycastHit hit;
                if (Physics.Raycast(agentTransform.position, directionToTarget, out hit, visionRange, targetLayers | (1 << LayerMask.NameToLayer("Obstacle"))))
                {
                    // Si el Raycast golpea al objetivo antes que un obst�culo
                    if (hit.collider.gameObject == hitCollider.gameObject)
                    {
                        targetIsDetected = true;
                        targetGameObject = hit.transform; // Asigna el objetivo detectado si no estaba asignado
                        break; // Un objetivo es suficiente para detectar
                    }
                }
            }
        }

        // Actualiza el color visual del cono bas�ndose en si hay un objetivo
        UpdateVisionMeshColor(targetIsDetected ? targetDetectedColor : noTargetColor);
    }

    // ================================================================
    // Representaci�n Visual del Cono
    // ================================================================

    private void UpdateVisionConeVisuals()
    {
        // Limpia el mesh anterior
        visionMesh.Clear();

        // Calcula los v�rtices del cono
        Vector3[] vertices = new Vector3[coneSegments + 2];
        Vector3[] normals = new Vector3[vertices.Length];
        int[] triangles = new int[(coneSegments) * 3];

        vertices[0] = Vector3.zero; // El �pice del cono est� en la posici�n del agente
        normals[0] = Vector3.up; // O la direcci�n que desees para las normales

        float currentAngle = -visionAngle; // Empezar desde el borde izquierdo del cono
        for (int i = 0; i <= coneSegments; i++)
        {
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 direction = rotation * Vector3.forward; // Direcci�n relativa al forward del agente
            vertices[i + 1] = direction * visionRange;
            normals[i + 1] = Vector3.up; // Aseg�rate de que las normales est�n correctas

            if (i < coneSegments)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            currentAngle += (visionAngle * 2 / coneSegments); // Incrementa el �ngulo
        }

        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.normals = normals; // Asigna las normales
        visionMesh.RecalculateBounds(); // Recalcula los l�mites para que el renderizado sea correcto
    }

    private void UpdateVisionMeshColor(Color color)
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
    }

    // M�todo para dibujar Gizmos en el editor (opcional, pero �til para depuraci�n)
    void OnDrawGizmos()
    {
        // Dibuja el rango de visi�n
        Gizmos.color = targetIsDetected ? targetDetectedColor : noTargetColor;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Dibuja el cono de visi�n
        Vector3 fovLine1 = Quaternion.Euler(0, visionAngle, 0) * transform.forward * visionRange;
        Vector3 fovLine2 = Quaternion.Euler(0, -visionAngle, 0) * transform.forward * visionRange;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
        // 

        // Dibuja el arco del cono
        if (Application.isPlaying)
        { // Dibuja el Mesh Renderer solo en juego para evitar conflictos visuales en editor
            // No se dibuja directamente con Gizmos en tiempo de ejecuci�n de esta manera
            // El Mesh Renderer ya se encarga de la visualizaci�n en juego
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

    // Consultas a IA (Ejemplo - s� honesto con lo que preguntaste):
    // ChatGPT: "�C�mo puedo dibujar un cono 3D con Mesh en Unity para un campo de visi�n?"
    // ChatGPT: "�Cu�l es la mejor manera de implementar un Steering Behavior de persecuci�n con NavMeshAgent en Unity?"
    */
}