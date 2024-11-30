using UnityEngine;
using System.Collections.Generic;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Configurações da Câmera")]
    public float smoothSpeed = 5f; // Velocidade de transição
    public Vector3 offset = new Vector3(0, 0, -10); // Offset padrão

    [Header("Posições e Objetivos da Câmera")]
    public List<Vector3> cameraPositions = new List<Vector3>(); // Lista de posições que a câmera pode assumir
    public List<GameObject> cameraGoals = new List<GameObject>(); // Lista de objetivos que ativam a mudança de câmera
    private int currentCameraPositionIndex = 0; // Índice atual da posição da câmera

    private Vector3 initialPosition; // Posição inicial da câmera

    private void Start()
    {
        // Salva a posição inicial da câmera
        initialPosition = transform.position;
    }

    void LateUpdate()
    {
        Vector3 desiredPosition;

        if (cameraPositions != null && cameraPositions.Count > 0 && currentCameraPositionIndex >= 0 && currentCameraPositionIndex < cameraPositions.Count)
        {
            // A posição desejada é a posição correspondente na lista
            desiredPosition = cameraPositions[currentCameraPositionIndex] + offset;
        }
        else
        {
            // Se não houver posições definidas, mantém a posição inicial
            desiredPosition = initialPosition + offset;
        }

        // Garante que a câmera só se mova no eixo X
        desiredPosition.y = transform.position.y; // Mantém o Y atual
        desiredPosition.z = transform.position.z; // Mantém o Z atual

        // Move a câmera suavemente para a posição desejada
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }

    public void OnAgentReachedGoal(GameObject goal)
    {
        int goalIndex = cameraGoals.IndexOf(goal);
        if (goalIndex != -1 && goalIndex < cameraPositions.Count)
        {
            currentCameraPositionIndex = goalIndex;
            Debug.Log($"Agente alcançou o objetivo '{goal.name}'. Atualizando posição da câmera para o índice {currentCameraPositionIndex}.");
        }
        else
        {
            Debug.LogWarning($"O objetivo '{goal.name}' não está na lista 'cameraGoals' ou o índice está fora dos limites.");
        }
    }

    public void ResetCamera()
    {
        currentCameraPositionIndex = 0; // Reinicia para a posição inicial
        transform.position = initialPosition + offset;
    }
}
