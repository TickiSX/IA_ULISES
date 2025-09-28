/*
 * =====================================================================================
 *
 * Filename:  VisionCone.cs
 *
 * Description:  Implementa la lógica de un cono de visión para un agente de IA,
 * permitiéndole detectar objetivos dentro de un rango y ángulo definidos,
 * verificando la línea de visión y comunicándose con un script de Steering
 * Behaviors para la persecución.
 *
 * Authors:  Carlos Hernán González
 * Cesar Sasia Zayas
 *
 * Materia:  Inteligencia Artificial e Ingeniería del Conocimiento
 *
 * =====================================================================================
 */

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// La clase VisionCone permite a un GameObject (el agente) detectar otros GameObjects
/// dentro de un área cónica de visión. También proporciona una representación visual
/// de este cono y activa un comportamiento de persecución al detectar un objetivo.
/// Este script debe adjuntarse a un GameObject hijo del agente principal.
/// </summary>
public class VisionCone : MonoBehaviour
{
    // Una referencia al 'Transform' del GameObject principal que actúa como el agente.
    // Es crucial para obtener la posición y dirección del agente para la detección y movimiento.
    // Debe asignarse en el Editor de Unity.
    public Transform agentTransform;

    // ================================================================
    // Variables configurables por el diseñador (expuestas en el editor de Unity)
    // ================================================================

    [Header("Configuración del Cono de Visión")]
    [Tooltip("Define el radio máximo hasta donde el cono de visión puede detectar un objetivo.")]
    [Range(1f, 50f)]
    public float visionRange = 10f;

    [Tooltip("Define el ángulo de visión del cono en grados. Este valor es desde el centro hacia cada lado.")]
    [Range(0f, 180f)]
    public float visionAngle = 60f;

    [Tooltip("Máscara de capas (LayerMask) que especifica qué capas de GameObjects pueden ser detectadas.")]
    public LayerMask targetLayers;

    [Tooltip("Referencia al Transform del GameObject que ha sido detectado. Se asigna automáticamente.")]
    public Transform targetGameObject;

    [Header("Configuración Visual del Cono")]
    [Tooltip("El color que tendrá el cono cuando NO haya un objetivo detectado.")]
    public Color noTargetColor = Color.green;

    [Tooltip("El color que tendrá el cono cuando SÍ haya un objetivo detectado.")]
    public Color targetDetectedColor = Color.red;

    [Tooltip("Número de segmentos utilizados para construir el Mesh visual del cono.")]
    [Range(8, 64)]
    public int coneSegments = 32;

    // ================================================================
    // Variables privadas (para uso interno del script y gestión de componentes)
    // ================================================================
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh visionMesh;
    private bool targetIsDetected = false;

    // Referencia al script de movimiento del agente (Steering Behaviors).
    private SteeringAgent steeringAgent;

    // ================================================================
    // Métodos de Unity (Ciclo de vida del script)
    // ================================================================

    void Awake()
    {
        // Obtiene el componente SteeringAgent del 'agentTransform' (el GameObject padre).
        // Este es el script que ahora controla el movimiento.
        steeringAgent = agentTransform.GetComponent<SteeringAgent>();
        if (steeringAgent == null)
        {
            Debug.LogWarning("VisionCone: Script 'SteeringAgent' no encontrado en el 'agentTransform'. La persecución no funcionará.");
        }

        // Se aseguran de que el GameObject que contiene este script tenga los componentes para dibujar la malla.
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }

        // Crea y asigna la malla para el cono visual.
        visionMesh = new Mesh();
        meshFilter.mesh = visionMesh;
        UpdateVisionMeshColor(noTargetColor);
    }

    void Update()
    {
        DetectTarget();
        UpdateVisionConeVisuals();

        // Lógica de comunicación con el script de movimiento (SteeringAgent).
        // Si se detecta un objetivo, se le asigna como 'target' al SteeringAgent para que lo persiga.
        if (targetIsDetected && targetGameObject != null && steeringAgent != null)
        {
            steeringAgent.target = targetGameObject;
        }
        // Si no se detecta nada, se le quita el objetivo (se asigna null) para que el agente se detenga.
        else if (steeringAgent != null)
        {
            steeringAgent.target = null;
        }
    }

    // ================================================================
    // Lógica del Cono de Visión (Detección de objetivos)
    // ================================================================
    private void DetectTarget()
    {
        targetIsDetected = false;
        targetGameObject = null;

        // Paso 1: Encuentra todos los colliders en un radio, filtrando por las capas de interés.
        Collider[] hitColliders = Physics.OverlapSphere(agentTransform.position, visionRange, targetLayers);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == agentTransform.gameObject || hitCollider.gameObject == gameObject) continue;

            // Calcula la dirección y el ángulo hacia el posible objetivo.
            Vector3 directionToTarget = (hitCollider.transform.position - agentTransform.position).normalized;
            float angleToTarget = Vector3.Angle(agentTransform.forward, directionToTarget);

            // Paso 2: Verifica si el objetivo está dentro del ángulo de visión.
            if (angleToTarget < visionAngle)
            {
                // Paso 3: Lanza un rayo para confirmar que no hay obstáculos en medio (línea de visión directa).
                RaycastHit hit;
                if (Physics.Raycast(agentTransform.position, directionToTarget, out hit, visionRange, targetLayers | (1 << LayerMask.NameToLayer("Obstacle"))))
                {
                    // Si el primer objeto golpeado por el rayo es el objetivo que estamos evaluando.
                    if (hit.collider.gameObject == hitCollider.gameObject)
                    {
                        targetIsDetected = true;
                        targetGameObject = hit.transform;
                        break; // Se encontró un objetivo, no es necesario seguir buscando.
                    }
                }
            }
        }

        // Actualiza el color del cono visual basado en el resultado de la detección.
        UpdateVisionMeshColor(targetIsDetected ? targetDetectedColor : noTargetColor);
    }

    // ================================================================
    // Representación Visual del Cono (Generación y actualización de la malla)
    // ================================================================
    private void UpdateVisionConeVisuals()
    {
        visionMesh.Clear();

        Vector3[] vertices = new Vector3[coneSegments + 2];
        Vector3[] normals = new Vector3[vertices.Length];
        int[] triangles = new int[coneSegments * 3];

        vertices[0] = Vector3.zero;
        normals[0] = Vector3.up;

        float currentAngle = -visionAngle;
        for (int i = 0; i <= coneSegments; i++)
        {
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 direction = rotation * transform.forward;
            vertices[i + 1] = direction * visionRange;
            normals[i + 1] = Vector3.up;

            if (i < coneSegments)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            currentAngle += (visionAngle * 2 / coneSegments);
        }

        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.normals = normals;
        visionMesh.RecalculateBounds();
    }

    private void UpdateVisionMeshColor(Color color)
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
    }

    // Dibuja ayudas visuales en el editor para facilitar la configuración.
    void OnDrawGizmos()
    {
        if (agentTransform == null) return;

        Gizmos.color = targetIsDetected ? targetDetectedColor : noTargetColor;
        Gizmos.DrawWireSphere(agentTransform.position, visionRange);

        Vector3 fovLine1 = Quaternion.Euler(0, visionAngle, 0) * agentTransform.forward * visionRange;
        Vector3 fovLine2 = Quaternion.Euler(0, -visionAngle, 0) * agentTransform.forward * visionRange;
        Gizmos.DrawRay(agentTransform.position, fovLine1);
        Gizmos.DrawRay(agentTransform.position, fovLine2);
    }

    // ================================================================
    // Sección de Agradecimientos y Referencias
    // ================================================================
    /*
    // Videos de Referencia:
    // - "Unity 3D AI Tutorial - Vision Cone": https://youtu.be/lV47ED8h61k?si=6m012cxUMIkJvd5z
    // - "How to Make an AI Vision Cone in Unity": https://youtu.be/j1-OyLo77ss?si=7B92T-AVf7LUOJh2

    // Consultas a Herramientas de Inteligencia Artificial (IA Generativa):
    // Se utilizaron herramientas de IA para resolver dudas específicas y obtener orientación en la estructuración del código y la depuración.
    // - Pregunta a Gemini: "¿Cómo puedo dibujar un cono 3D con Mesh en Unity para un campo de visión?"
    // - Pregunta a Gemini: "Ayúdame a implementar un Steering Behavior de 'Seek' con Rigidbody en Unity."
    // - Pregunta a Gemini: "Explícame el uso de LayerMasks y Physics.Raycast con operadores de bits en Unity."
    */
}