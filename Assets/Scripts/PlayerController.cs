/*
 * =====================================================================================
 *
 * Filename:  PlayerController.cs
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


// PlayerController.cs (o TargetMover.cs)
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Tooltip("Velocidad de movimiento del personaje.")]
    public float moveSpeed = 5f;

    void Update()
    {
        // Obtener la entrada del teclado
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D o Flechas Izq/Der
        float verticalInput = Input.GetAxis("Vertical");   // W/S o Flechas Arr/Aba

        // Calcular la dirección de movimiento
        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

        // Mover el GameObject
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }
}