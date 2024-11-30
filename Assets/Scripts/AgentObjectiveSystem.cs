using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteInEditMode]
public class AgentObjectiveSystem : MonoBehaviour
{
    [System.Serializable]
    public class RoomSettings
    {
        [Header("Objetivos")]
        public Transform GoalsParent;
        public List<GameObject> allGoals = new List<GameObject>();
        public GameObject specificGoal;

        [Header("Configurações")]
        public float goalReachDistance = 1.5f;

        [Header("Timer Settings")]
        public float maxTimeInRoom = 30f;

        [Header("UI Elements")]
        public TMP_Text timerText;
        public TMP_Text generationText;

        [Header("Spawn Settings")]
        public SpawnAreaManager spawnManager;

        [Header("Escape Target e Porta")]
        public GameObject escapeTarget;
        public Door door;

        [Header("Room Manager")]
        public RoomManager roomManager;

        [Header("Objetos a Resetar")]
        public List<GameObject> objectsToReset = new List<GameObject>();
    }

    public List<RoomSettings> rooms = new List<RoomSettings>();
    private int currentRoomIndex = 0;

    private RoomSettings currentRoom
    {
        get
        {
            if (rooms != null && rooms.Count > currentRoomIndex)
            {
                return rooms[currentRoomIndex];
            }
            return null;
        }
    }

    private NavigationAgentController agentController;
    private List<GameObject> visitedGoals = new List<GameObject>();

    // Propriedades para acessar os contadores
    public int totalGoals { get { return currentRoom != null ? currentRoom.allGoals.Count : 0; } }
    public int visitedGoalsCount { get { return visitedGoals.Count; } }

    // Timer
    private float currentRoomTime;

    // UI
    private int currentGeneration = 0;

    // Variáveis para o Curriculum Learning
    private bool explorationAllowed = true;
    private bool objectiveActive = true;

    // Flag para controle de reset por tempo
    private bool timeResetEnabled = true;

    public struct ObjectiveState
    {
        public float timeInRoom;
        public bool explorationAllowed;
        public int totalGoals;
        public int visitedGoalsCount;
    }

    public void InitializeObjectives(NavigationAgentController controller)
    {
        agentController = controller;
        currentGeneration = 0;
        currentRoomIndex = 0;
        InitializeCurrentRoom();
    }

    private void InitializeCurrentRoom()
    {
        // Fecha a porta da sala anterior se não for a primeira sala
        if (currentRoomIndex > 0)
        {
            var previousRoom = rooms[currentRoomIndex - 1];
            if (previousRoom.door != null)
            {
                previousRoom.door.CloseDoor();
            }
        }

        if (currentRoom != null)
        {
            // Código de inicialização existente...
            ResetTimer();
            UpdateUI();
            InitializeGoals();
            ResetRoomObjects();
            timeResetEnabled = true; // Habilita o reset por tempo para a nova sala
        }
    }


    private void InitializeGoals()
    {
        if (currentRoom != null)
        {
            currentRoom.allGoals.Clear();
            visitedGoals.Clear();

            if (currentRoom.GoalsParent != null)
            {
                foreach (Transform child in currentRoom.GoalsParent)
                {
                    if (child.CompareTag("Collectible"))
                    {
                        currentRoom.allGoals.Add(child.gameObject);
                        if (currentRoom.specificGoal == null)
                        {
                            currentRoom.specificGoal = child.gameObject; // Atribui o primeiro "Collectible" como specificGoal se não estiver definido
                            Debug.LogWarning($"specificGoal não estava definido. Atribuindo '{child.gameObject.name}' como specificGoal.");
                        }
                    }
                }
            }

            if (currentRoom.specificGoal == null && currentRoom.allGoals.Count > 0)
            {
                currentRoom.specificGoal = currentRoom.allGoals[0];
                Debug.LogWarning($"specificGoal ainda não está definido. Atribuindo '{currentRoom.allGoals[0].name}' como specificGoal.");
            }
        }
    }


    private void ResetTimer()
    {
        currentRoomTime = 0f;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (currentRoom != null)
        {
            if (currentRoom.timerText != null)
            {
                int remainingTime = Mathf.CeilToInt(currentRoom.maxTimeInRoom - currentRoomTime);
                currentRoom.timerText.text = remainingTime.ToString("00");
            }

            if (currentRoom.generationText != null)
            {
                currentRoom.generationText.text = currentGeneration.ToString("00");
            }
        }
    }

    public void ResetObjectives()
    {
        ResetTimer();
        currentGeneration++;
        visitedGoals.Clear();

        if (currentRoom != null && currentRoom.spawnManager != null)
        {
            currentRoom.spawnManager.RespawnAgent(gameObject);
        }

        ResetRoomObjects();
        UpdateUI();
    }

    private void ResetRoomObjects()
    {
        foreach (var obj in currentRoom.objectsToReset)
        {
            if (obj != null)
            {
                var resettable = obj.GetComponent<IResettable>();
                if (resettable != null)
                {
                    resettable.ResetObject();
                }
                else
                {
                    Debug.LogWarning($"O objeto {obj.name} não possui um componente que implementa IResettable.");
                }
            }
        }

        // Resetar o RoomManager
        if (currentRoom.roomManager != null)
        {
            currentRoom.roomManager.ResetRoom();
        }
    }

    private void DeactivateCurrentRoomRewards()
    {
        Debug.Log("DeactivateCurrentRoomRewards foi chamado, mas está desativado.");
        //if (currentRoom != null)
        //{
        //    foreach (var goal in currentRoom.allGoals)
        //    {
        //        goal.SetActive(false); // Desativa o GameObject da recompensa
        //    }
        //}
    }

    public void CheckObjectives()
    {
        if (timeResetEnabled)
        {
            currentRoomTime += Time.deltaTime;
            UpdateUI();

            if (currentRoom != null && currentRoomTime >= currentRoom.maxTimeInRoom)
            {
                agentController.AddReward(-0.5f);
                agentController.EndEpisode();
                ResetTimer();
                UpdateUI();
                return;
            }
        }

        // Verifica se todos os objetivos foram visitados
        if (visitedGoals.Count == totalGoals)
        {
            agentController.AddReward(1.0f);
            agentController.EndEpisode();
        }
    }

    public ObjectiveState GetCurrentState()
    {
        return new ObjectiveState
        {
            timeInRoom = currentRoomTime,
            explorationAllowed = explorationAllowed,
            totalGoals = totalGoals,
            visitedGoalsCount = visitedGoals.Count
        };
    }

    // Método público para obter os objetivos da sala atual
    public List<GameObject> GetCurrentRoomGoals()
    {
        if (currentRoom != null)
        {
            return currentRoom.allGoals;
        }
        return new List<GameObject>();
    }

    // Método público para obter a sala atual
    public RoomSettings GetCurrentRoom()
    {
        return currentRoom;
    }

    // Métodos para o Curriculum Learning
    public void SetExplorationAllowed(bool allowed)
    {
        explorationAllowed = allowed;
    }

    public void SetObjectiveActive(bool active)
    {
        objectiveActive = active;
    }

    public void ResetGenerationCount()
    {
        currentGeneration = 0;
        UpdateUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter chamado com " + other.gameObject.name);

        if (currentRoom != null && other.CompareTag("Collectible"))
        {
            if (!visitedGoals.Contains(other.gameObject))
            {
                visitedGoals.Add(other.gameObject);
                agentController.AddReward(0.5f);
                Debug.Log($"Recompensa de 0.5f adicionada por visitar o objetivo: {other.gameObject.name}");

                // Verifica se o objetivo específico foi alcançado
                if (currentRoom.specificGoal != null && other.gameObject == currentRoom.specificGoal)
                {
                    timeResetEnabled = false; // Desabilita o reset por tempo
                    Debug.Log("Objetivo específico alcançado. Reset por tempo desabilitado.");

                    // Notifica a câmera que o agente alcançou o objetivo
                    CameraFollow2D cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
                    if (cameraFollow != null)
                    {
                        cameraFollow.OnAgentReachedGoal(other.gameObject);
                    }

                    // Desativa recompensas da sala atual
                    //DeactivateCurrentRoomRewards();

                    // Avança para a próxima sala
                    currentRoomIndex++;
                    if (currentRoomIndex < rooms.Count)
                    {
                        InitializeCurrentRoom();
                    }
                    else
                    {
                        // Se todas as salas foram concluídas
                        agentController.AddReward(1.0f);
                        agentController.EndEpisode();
                    }
                }
                else
                {
                    Debug.LogWarning("specificGoal não está definido para a sala atual.");
                    // Notifica a câmera que o agente alcançou um objetivo (se desejar que a câmera se mova em outros objetivos)
                    CameraFollow2D cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
                    if (cameraFollow != null)
                    {
                        cameraFollow.OnAgentReachedGoal(other.gameObject);
                    }
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("OnCollisionEnter chamado com " + collision.gameObject.name);

        if (currentRoom != null && collision.gameObject.CompareTag("Collectible"))
        {
            if (!visitedGoals.Contains(collision.gameObject))
            {
                visitedGoals.Add(collision.gameObject);
                agentController.AddReward(0.5f);
                Debug.Log($"Recompensa de 0.5f adicionada por visitar o objetivo: {collision.gameObject.name}");

                // Verifica se o objetivo específico foi alcançado
                if (currentRoom.specificGoal != null && collision.gameObject == currentRoom.specificGoal)
                {
                    timeResetEnabled = false; // Desabilita o reset por tempo
                    Debug.Log("Objetivo específico alcançado. Reset por tempo desabilitado.");

                    // Notifica a câmera que o agente alcançou o objetivo
                    CameraFollow2D cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
                    if (cameraFollow != null)
                    {
                        cameraFollow.OnAgentReachedGoal(collision.gameObject);
                    }

                    // Desativa recompensas da sala atual
                    //DeactivateCurrentRoomRewards();

                    // Avança para a próxima sala
                    currentRoomIndex++;
                    if (currentRoomIndex < rooms.Count)
                    {
                        InitializeCurrentRoom();
                    }
                    else
                    {
                        // Se todas as salas foram concluídas
                        agentController.AddReward(1.0f);
                        agentController.EndEpisode();
                    }
                }
                else
                {
                    // Notifica a câmera que o agente alcançou um objetivo (se desejar que a câmera se mova em outros objetivos)
                    CameraFollow2D cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
                    if (cameraFollow != null)
                    {
                        cameraFollow.OnAgentReachedGoal(collision.gameObject);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Visualiza os objetivos da sala atual
        if (currentRoom != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var goal in currentRoom.allGoals)
            {
                Gizmos.DrawWireSphere(goal.transform.position, currentRoom.goalReachDistance);
            }

            // Mostra o tempo restante
            float remainingTime = currentRoom.maxTimeInRoom - currentRoomTime;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Time: {remainingTime:F1}s");
        }
    }

    private void OnValidate()
    {
        if (rooms != null)
        {
            foreach (var room in rooms)
            {
                room.maxTimeInRoom = Mathf.Max(1f, room.maxTimeInRoom);
                room.goalReachDistance = Mathf.Max(0.1f, room.goalReachDistance);
            }
        }
    }
}

public interface IResettable
{
    void ResetObject();
}
