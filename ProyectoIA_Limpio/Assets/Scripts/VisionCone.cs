/*
 * =====================================================================================
 *
 * Filename:  VisionCone.cs
 *
 * Description:  Implementa la l�gica de un cono de visi�n para un agente de IA,
 * permiti�ndole detectar objetivos dentro de un rango y �ngulo definidos,
 * verificando la l�nea de visi�n y comunic�ndose con un script de Steering
 * Behaviors para la persecuci�n.
 *
 * Authors:  Carlos Hern�n Gonz�lez
 * Cesar Sasia Zayas
 *
 * Materia:  Inteligencia Artificial e Ingenier�a del Conocimiento
 *
 * =====================================================================================
 */

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// La clase VisionCone permite a un GameObject (el agente) detectar otros GameObjects
/// dentro de un �rea c�nica de visi�n. Tambi�n proporciona una representaci�n visual
/// de este cono y activa un comportamiento de persecuci�n al detectar un objetivo.
/// Este script debe adjuntarse a un GameObject hijo del agente principal.
/// </summary>
public class VisionCone : MonoBehaviour
{
    // Una referencia al 'Transform' del GameObject principal que act�a como el agente.
    // Es crucial para obtener la posici�n y direcci�n del agente para la detecci�n y movimiento.
    // Debe asignarse en el Editor de Unity.
    public Transform agentTransform;

    // ================================================================
    // Variables configurables por el dise�ador (expuestas en el editor de Unity)
    // ================================================================

    [Header("Configuraci�n del Cono de Visi�n")]
    [Tooltip("Define el radio m�ximo hasta donde el cono de visi�n puede detectar un objetivo.")]
    [Range(1f, 50f)]
    public float visionRange = 10f;

    [Tooltip("Define el �ngulo de visi�n del cono en grados. Este valor es desde el centro hacia cada lado.")]
    [Range(0f, 180f)]
    public float visionAngle = 60f;

    [Tooltip("M�scara de capas (LayerMask) que especifica qu� capas de GameObjects pueden ser detectadas.")]
    public LayerMask targetLayers;

    [Tooltip("Referencia al Transform del GameObject que ha sido detectado. Se asigna autom�ticamente.")]
    public Transform targetGameObject;

    [Header("Configuraci�n Visual del Cono")]
    [Tooltip("El color que tendr� el cono cuando NO haya un objetivo detectado.")]
    public Color noTargetColor = Color.green;

    [Tooltip("El color que tendr� el cono cuando S� haya un objetivo detectado.")]
    public Color targetDetectedColor = Color.red;

    [Tooltip("N�mero de segmentos utilizados para construir el Mesh visual del cono.")]
    [Range(8, 64)]
    public int coneSegments = 32;

    // ================================================================
    // Variables privadas (para uso interno del script y gesti�n de componentes)
    // ================================================================
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh visionMesh;
    private bool targetIsDetected = false;

    // Referencia al script de movimiento del agente (Steering Behaviors).
    private SteeringAgent steeringAgent;

    // ================================================================
    // M�todos de Unity (Ciclo de vida del script)
    // ================================================================

    void Awake()
    {
        // Obtiene el componente SteeringAgent del 'agentTransform' (el GameObject padre).
        // Este es el script que ahora controla el movimiento.
        steeringAgent = agentTransform.GetComponent<SteeringAgent>();
        if (steeringAgent == null)
        {
            Debug.LogWarning("VisionCone: Script 'SteeringAgent' no encontrado en el 'agentTransform'. La persecuci�n no funcionar�.");
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

        // L�gica de comunicaci�n con el script de movimiento (SteeringAgent).
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
    // L�gica del Cono de Visi�n (Detecci�n de objetivos)
    // ================================================================
    private void DetectTarget()
    {
        targetIsDetected = false;
        targetGameObject = null;

        // Paso 1: Encuentra todos los colliders en un radio, filtrando por las capas de inter�s.
        Collider[] hitColliders = Physics.OverlapSphere(agentTransform.position, visionRange, targetLayers);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == agentTransform.gameObject || hitCollider.gameObject == gameObject) continue;

            // Calcula la direcci�n y el �ngulo hacia el posible objetivo.
            Vector3 directionToTarget = (hitCollider.transform.position - agentTransform.position).normalized;
            float angleToTarget = Vector3.Angle(agentTransform.forward, directionToTarget);

            // Paso 2: Verifica si el objetivo est� dentro del �ngulo de visi�n.
            if (angleToTarget < visionAngle)
            {
                // Paso 3: Lanza un rayo para confirmar que no hay obst�culos en medio (l�nea de visi�n directa).
                RaycastHit hit;
                if (Physics.Raycast(agentTransform.position, directionToTarget, out hit, visionRange, targetLayers | (1 << LayerMask.NameToLayer("Obstacle"))))
                {
                    // Si el primer objeto golpeado por el rayo es el objetivo que estamos evaluando.
                    if (hit.collider.gameObject == hitCollider.gameObject)
                    {
                        targetIsDetected = true;
                        targetGameObject = hit.transform;
                        break; // Se encontr� un objetivo, no es necesario seguir buscando.
                    }
                }
            }
        }

        // Actualiza el color del cono visual basado en el resultado de la detecci�n.
        UpdateVisionMeshColor(targetIsDetected ? targetDetectedColor : noTargetColor);
    }

    // ================================================================
    // Representaci�n Visual del Cono (Generaci�n y actualizaci�n de la malla)
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

    // Dibuja ayudas visuales en el editor para facilitar la configuraci�n.
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
    // Secci�n de Agradecimientos y Referencias
    // ================================================================
    /*
    // Videos de Referencia:
    // - "Unity 3D AI Tutorial - Vision Cone": https://youtu.be/lV47ED8h61k?si=6m012cxUMIkJvd5z
    // - "How to Make an AI Vision Cone in Unity": https://youtu.be/j1-OyLo77ss?si=7B92T-AVf7LUOJh2

    // Consultas a Herramientas de Inteligencia Artificial (IA Generativa):
    // Se utilizaron herramientas de IA para resolver dudas espec�ficas y obtener orientaci�n en la estructuraci�n del c�digo y la depuraci�n.
    // - Pregunta a Gemini: "�C�mo puedo dibujar un cono 3D con Mesh en Unity para un campo de visi�n?"
    // - Pregunta a Gemini: "Ay�dame a implementar un Steering Behavior de 'Seek' con Rigidbody en Unity."
    // - Pregunta a Gemini: "Expl�came el uso de LayerMasks y Physics.Raycast con operadores de bits en Unity."
    */
}