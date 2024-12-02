using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;

public class AgentObservationSystem : MonoBehaviour
{
    private NavigationAgentController agentController;
    private AgentMovement movementSystem;
    private AgentObjectiveSystem objectiveSystem;
    private Door door; // Referência à porta

    [Header("Configurações do Raycast")]
    public int numRaycastsBaixo = 7;
    public int numRaycastsMedio = 17;
    public int numRaycastsAlto = 17;
    public float raycastFOV = 70f;
    public float rayLength = 20f;
    public LayerMask detectableLayers;

    [Header("Configurações de Observação")]
    public int stackedObservations = 6;

    private Queue<ObservationData> observationHistory;

    public void InitializeObservations(NavigationAgentController controller)
    {
        agentController = controller;
        movementSystem = controller.movementSystem;
        objectiveSystem = controller.objectiveSystem;

        observationHistory = new Queue<ObservationData>();
        door = agentController.objectiveSystem.GetCurrentRoom().door;
    }

    public void ResetObservations()
    {
        observationHistory.Clear();
        for (int i = 0; i < stackedObservations; i++)
        {
            observationHistory.Enqueue(new ObservationData
            {
                position = Vector3.zero,
                velocity = Vector3.zero,
                wasGrounded = false
            });
        }
    }

    public void UpdateObservations()
    {
        if (observationHistory.Count >= stackedObservations)
        {
            observationHistory.Dequeue();
        }

        var movementData = movementSystem.GetMovementData();

        ObservationData obsData = new ObservationData
        {
            position = transform.position,
            velocity = movementData.velocity,
            wasGrounded = movementData.isGrounded
        };

        observationHistory.Enqueue(obsData);
    }

    public void CollectObservations(VectorSensor sensor)
    {
        // Adiciona a posição do agente
        sensor.AddObservation(transform.position);

        // Adiciona histórico de observações
        foreach (var obs in observationHistory)
        {
            sensor.AddObservation(obs.position);
            sensor.AddObservation(obs.velocity);
            sensor.AddObservation(obs.wasGrounded ? 1.0f : 0.0f);
        }

        // Adiciona a posição relativa dos objetivos
        var currentGoals = objectiveSystem.GetCurrentRoomGoals();
        foreach (var goal in currentGoals)
        {
            Vector3 relativePosition = goal.transform.position - transform.position;
            sensor.AddObservation(relativePosition);
        }

        // Adiciona o número de objetivos restantes
        int goalsRemaining = objectiveSystem.totalGoals - objectiveSystem.visitedGoalsCount;
        sensor.AddObservation(goalsRemaining);

        // Adiciona o estado da porta (aberta = 1, fechada = 0)
        if (door != null)
        {
            float portaAberta = door.PortaAberta ? 1.0f : 0.0f;
            sensor.AddObservation(portaAberta);
        }
        else
        {
            // Se a referência à porta não estiver definida, assume que está fechada
            sensor.AddObservation(0.0f);
        }

        // Realiza os raycasts
        CastRaycasts(sensor);
    }

    private void CastRaycasts(VectorSensor sensor)
    {
        // Raycasts baixos (apontando para baixo)
        CastRaysAtAngle(-15f, numRaycastsBaixo, sensor);

        // Raycasts médios (horizontais)
        CastRaysAtAngle(0f, numRaycastsMedio, sensor);

        // Raycasts altos (apontando para cima)
        CastRaysAtAngle(15f, numRaycastsAlto, sensor);
    }

    private void CastRaysAtAngle(float pitchAngle, int numRays, VectorSensor sensor)
    {
        Vector3 rayStart = transform.position; // Origem dos raycasts é o centro do agente
        float angleStep = raycastFOV / (numRays - 1);
        float startAngle = -raycastFOV / 2;

        for (int i = 0; i < numRays; i++)
        {
            float yawAngle = startAngle + i * angleStep;

            // Calcula a direção do raycast com yaw e pitch
            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle + transform.eulerAngles.y, 0);
            Vector3 direction = rotation * Vector3.forward;

            RaycastHit hit;
            bool hasHit = Physics.Raycast(rayStart, direction, out hit, rayLength, detectableLayers);

            // Presença de objeto
            sensor.AddObservation(hasHit ? 1.0f : 0.0f);

            // Distância normalizada
            sensor.AddObservation(hasHit ? hit.distance / rayLength : 1.0f);

            // Tipo de objeto (one-hot encoding)
            float[] objectType = new float[5]; // Supondo 4 tipos de objetos detectáveis
            if (hasHit)
            {
                //Debug.Log($"Raycast hit: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                int layerIndex = hit.collider.gameObject.layer;
                if (layerIndex == LayerMask.NameToLayer("groudLayer"))
                    objectType[0] = 1.0f;
                else if (layerIndex == LayerMask.NameToLayer("wallLayer"))
                    objectType[1] = 1.0f;
                else if (layerIndex == LayerMask.NameToLayer("obstacleLayer"))
                    objectType[2] = 1.0f;
                else if (layerIndex == LayerMask.NameToLayer("Platform"))
                    objectType[3] = 1.0f;
                else if (layerIndex == LayerMask.NameToLayer("Door"))
                    objectType[4] = 1.0f;
            }
            // Adiciona o tipo de objeto
            foreach (var val in objectType)
            {
                sensor.AddObservation(val);
            }
        }
    }

    // Atualização do método para desenhar os gizmos com o novo ângulo
    private void OnDrawGizmos()
    {
        // Raycasts baixos (apontando para baixo)
        DrawRaycastsGizmos(-10f, numRaycastsBaixo, Color.green);

        // Raycasts médios (horizontais)
        DrawRaycastsGizmos(0f, numRaycastsMedio, Color.yellow);

        // Raycasts altos (apontando para cima)
        DrawRaycastsGizmos(10f, numRaycastsAlto, Color.red);
    }

    private void DrawRaycastsGizmos(float pitchAngle, int numRays, Color color)
    {
        Gizmos.color = color;
        Vector3 rayStart = transform.position; // Origem dos raycasts é o centro do agente
        float angleStep = raycastFOV / (numRays - 1);
        float startAngle = -raycastFOV / 2;

        for (int i = 0; i < numRays; i++)
        {
            float yawAngle = startAngle + i * angleStep;

            // Calcula a direção do raycast com yaw e pitch
            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle + transform.eulerAngles.y, 0);
            Vector3 direction = rotation * Vector3.forward;

            Gizmos.DrawLine(rayStart, rayStart + direction * rayLength);
        }
    }

    public struct ObservationData
    {
        public Vector3 position;
        public Vector3 velocity;
        public bool wasGrounded;
    }
}