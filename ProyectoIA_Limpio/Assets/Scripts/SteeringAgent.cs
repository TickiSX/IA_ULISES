/*
 * =====================================================================================
 *
 * Filename:  SteeringAgent.cs
 *
 * Description:  Maneja los comportamientos de movimiento de un agente (Steering Behaviors).
 * Utiliza un Rigidbody para aplicar fuerzas y mover al agente.
 *
 * Authors:  Carlos Hern�n Gonz�lez
 * Cesar Sasia Zayas
 *
 * =====================================================================================
 */

using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // Asegura que este script siempre tenga un Rigidbody en el mismo GameObject.
public class SteeringAgent : MonoBehaviour
{
    [Header("Configuraci�n de Movimiento")]
    [Tooltip("La velocidad m�xima a la que se puede mover el agente.")]
    public float maxSpeed = 10f;

    [Tooltip("La fuerza m�xima que se puede aplicar para cambiar de direcci�n.")]
    public float maxForce = 15f;

    // El objetivo (Transform) que el agente debe perseguir.
    // Ser� asignado por el script VisionCone cuando detecte algo.
    public Transform target;

    // Referencia al componente Rigidbody para aplicar las f�sicas.
    private Rigidbody rb;

    void Start()
    {
        // Obtenemos la referencia al Rigidbody al iniciar.
        rb = GetComponent<Rigidbody>();
    }

    // FixedUpdate es el mejor lugar para la l�gica de f�sicas.
    void FixedUpdate()
    {
        // Si tenemos un objetivo asignado, calculamos y aplicamos la fuerza de persecuci�n.
        if (target != null)
        {
            // Calcula la fuerza de "Seek" (persecuci�n).
            Vector3 seekingForce = Seek(target.position);

            // Aplica la fuerza calculada al Rigidbody.
            rb.AddForce(seekingForce);

            // Limita la velocidad para que no acelere indefinidamente.
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }
        else
        {
            // Si no hay objetivo, frenamos el agente gradualmente.
            rb.linearVelocity *= 0.95f;
        }
    }

    /// <summary>
    /// Calcula la fuerza necesaria para moverse hacia una posici�n objetivo (Comportamiento de "Seek").
    /// </summary>
    /// <param name="targetPosition">La posici�n del mundo a la que se quiere llegar.</param>
    /// <returns>La fuerza de steering a aplicar.</returns>
    private Vector3 Seek(Vector3 targetPosition)
    {
        // 1. Calcula la velocidad deseada: un vector que apunta desde la posici�n actual
        //    hacia el objetivo, con una magnitud igual a la velocidad m�xima.
        Vector3 desiredVelocity = (targetPosition - transform.position).normalized * maxSpeed;

        // 2. Calcula la fuerza de "steering": la diferencia entre la velocidad deseada y
        //    la velocidad actual. Este es el ajuste que necesitamos hacer.
        Vector3 steeringForce = desiredVelocity - rb.linearVelocity;

        // 3. Limita la fuerza de steering a su valor m�ximo para un movimiento m�s suave.
        steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);

        return steeringForce;
    }
}