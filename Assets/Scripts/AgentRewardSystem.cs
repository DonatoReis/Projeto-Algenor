using UnityEngine;

[ExecuteInEditMode]
public class AgentRewardSystem : MonoBehaviour
{
    [Header("Configurações de Recompensas")]
    public float recompensaPlacaRapida = 1.0f;
    public float recompensaPlacaMedia = 0.9f;
    public float recompensaPlacaLenta = 0.8f;
    public float recompensaExploracao = 0.0005f;
    public float penalizacaoQueda = -0.5f;
    public float penalizacaoParede = -0.1f;
    public float penalizacaoChao = -0.05f;
    public float penalizacaoPorTempo = 0.05f;

    [Header("Configurações de Penalização por Pulos")]
    public float jumpPenalty = -2f; // Penalização por pulo
    public float jumpPenaltyInterval = 1f; // Intervalo para aplicar a penalização

    [Header("Recompensa por Escapar da Sala")]
    public float recompensaEscaparSala = 5.0f;
    private bool recompensaConcedida = false;

    [Header("Configurações de Recompensas da Porta")]
    public float recompensaProximidadePorta = 0.5f; // Recompensa por unidade de proximidade
    public float recompensaAlcancarPorta = 2.0f; // Recompensa fixa ao alcançar a porta
    public float distanciaMaximaRecompensa = 10.0f; // Distância máxima considerada para a recompensa
    public float distanciaParaAlcancarPorta = 2.0f; // Distância para considerar que a porta foi alcançada

    private NavigationAgentController agentController;
    private float episodeStartTime;
    public LayerMask wallLayer;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public LayerMask Platform; // Layer da plataforma

    private Vector3 lastPosition;
    private float inactivityTimer = 0f;
    private const float inactivityThreshold = 5f; // Tempo em segundos

    // Variáveis para controle de recompensas e penalizações
    private float frameReward = 0f;
    private float timeSinceLastPenalty = 0f;
    private float timeSinceLastExplorationReward = 0f;
    private float timeSinceLastJumpPenalty = 0f;

    // Referência ao AgentObjectiveSystem
    private AgentObjectiveSystem objectiveSystem;

    public void InitializeRewards(NavigationAgentController controller)
    {
        agentController = controller;
        lastPosition = transform.position;
        Debug.Log($"AgentController inicializado: {agentController != null}");

        // Obter referência ao AgentObjectiveSystem
        objectiveSystem = agentController.objectiveSystem;
        if (objectiveSystem == null)
        {
            Debug.LogError("AgentObjectiveSystem não encontrado!");
        }

        // Subscrição ao evento de recompensa
        agentController.OnAddReward += OnAddReward;
    }

    public void Update()
    {
        // Retorna se o agentController não foi inicializado
        if (agentController == null)
        {
            return;
        }

        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < 0.1f)
        {
            inactivityTimer += Time.deltaTime;
            if (inactivityTimer >= inactivityThreshold)
            {
                agentController.AddReward(-0.01f); // Penaliza por inatividade
                inactivityTimer = 0f; // Reseta o temporizador
            }
        }
        else
        {
            inactivityTimer = 0f; // Reseta se o agente se mover
        }

        lastPosition = transform.position;

        // Imprime a recompensa acumulada do frame atual se for significativa
        if (Mathf.Abs(frameReward) > 0.5f)
        {
            Debug.Log($"Recompensa no frame {Time.frameCount}: {frameReward}");
        }
        frameReward = 0f;
    }

    public void ResetRewards()
    {
        episodeStartTime = Time.time;
        frameReward = 0f;
        timeSinceLastPenalty = 0f;
        timeSinceLastExplorationReward = 0f;
        timeSinceLastJumpPenalty = 0f;
        recompensaConcedida = false;
    }

    public void ProcessRewards(AgentObjectiveSystem.ObjectiveState objectiveState, AgentMovement.MovementData movementData)
    {
        // Retorna se o agentController não foi inicializado
        if (agentController == null)
        {
            return;
        }

        float timeInEpisode = Time.time - episodeStartTime;

        // Recompensa baseada no tempo para alcançar todos os objetivos
        if (objectiveState.visitedGoalsCount == objectiveState.totalGoals)
        {
            if (timeInEpisode < 5f)
                agentController.AddReward(recompensaPlacaRapida);
            else if (timeInEpisode < 10f)
                agentController.AddReward(recompensaPlacaMedia);
            else
                agentController.AddReward(recompensaPlacaLenta);

            agentController.EndEpisode();
        }
        else
        {
            // Recompensa por exploração aplicada a cada segundo
            timeSinceLastExplorationReward += Time.deltaTime;
            if (timeSinceLastExplorationReward >= 1f)
            {
                agentController.AddReward(recompensaExploracao);
                timeSinceLastExplorationReward = 0f;
            }
        }

        // Penalização por tempo aplicada a cada segundo
        timeSinceLastPenalty += Time.deltaTime;
        if (timeSinceLastPenalty >= 1f)
        {
            agentController.AddReward(-Mathf.Abs(penalizacaoPorTempo));
            timeSinceLastPenalty = 0f;
        }

        // Penalizações por cair
        if (movementData.position.y < -1f)
        {
            agentController.AddReward(penalizacaoQueda);
            agentController.EndEpisode();
        }

        // --- Implementação da Lógica de Recompensa Condicional ---

        // Obter a sala atual do AgentObjectiveSystem
        var currentRoom = objectiveSystem?.GetCurrentRoom();

        if (currentRoom != null && currentRoom.door != null && currentRoom.door.PortaAberta)
        {
            // Calcular a distância até a porta da sala atual
            float distanciaAtePorta = Vector3.Distance(transform.position, currentRoom.door.transform.position);

            // Verificar se o agente alcançou a porta
            if (distanciaAtePorta <= distanciaParaAlcancarPorta)
            {
                // Conceder recompensa fixa por alcançar a porta
                agentController.AddReward(recompensaAlcancarPorta);
                agentController.EndEpisode();
            }
            else if (distanciaAtePorta <= distanciaMaximaRecompensa)
            {
                // Conceder recompensa proporcional à proximidade
                float proporcaoProximidade = 1f - (distanciaAtePorta / distanciaMaximaRecompensa);
                float recompensaProximidade = recompensaProximidadePorta * proporcaoProximidade * Time.deltaTime;
                agentController.AddReward(recompensaProximidade);
            }
        }

        // Penalização por pulos excessivos
        timeSinceLastJumpPenalty += Time.deltaTime;
        if (timeSinceLastJumpPenalty >= jumpPenaltyInterval)
        {
            int jumpCount = agentController.movementSystem.GetJumpCount();

            float penalty = jumpPenalty * jumpCount;

            if (penalty != 0)
            {
                agentController.AddReward(-penalty);
                Debug.Log($"Penalização por {jumpCount} pulos nos últimos {jumpPenaltyInterval} segundos: {-penalty}");
            }

            timeSinceLastJumpPenalty = 0f;
            agentController.movementSystem.ResetJumpCount();
        }
    }

    private void OnDrawGizmos()
    {
        // Obter o objectiveSystem se estiver nulo
        if (objectiveSystem == null)
        {
            objectiveSystem = GetComponent<NavigationAgentController>()?.objectiveSystem;
            if (objectiveSystem == null)
            {
                objectiveSystem = FindFirstObjectByType<AgentObjectiveSystem>();
            }
        }

        if (objectiveSystem == null)
        {
            Debug.LogWarning("objectiveSystem é nulo em OnDrawGizmos.");
            return;
        }

        // Obter a lista de salas
        var rooms = objectiveSystem.rooms;

        if (rooms == null || rooms.Count == 0)
        {
            Debug.LogWarning("Nenhuma sala definida no AgentObjectiveSystem.");
            return;
        }

        foreach (var room in rooms)
        {
            if (room != null && room.door != null)
            {
                // Desenhar os Gizmos relacionados à porta
                // Desenha um círculo representando o limite máximo de recompensa
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(room.door.transform.position, distanciaMaximaRecompensa);

                // Desenha um círculo menor representando a distância para alcançar a porta
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(room.door.transform.position, distanciaParaAlcancarPorta);

                // Opcional: Desenha uma linha entre o agente e a porta se o jogo estiver rodando
                if (Application.isPlaying && agentController != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, room.door.transform.position);
                }
            }
            else
            {
                Debug.LogWarning("Sala ou porta não definida em OnDrawGizmos.");
            }
        }
    }

    public void ProcessCollision(Collision collision)
    {
        // Retorna se o agentController não foi inicializado
        if (agentController == null)
        {
            return;
        }

        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            agentController.AddReward(penalizacaoParede);
        }
        else if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            agentController.AddReward(penalizacaoChao);
        }
        else if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            // Penalização por colidir com obstáculo
            agentController.AddReward(penalizacaoChao);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Retorna se o agentController não foi inicializado
        if (agentController == null)
        {
            return;
        }

        // Verifica se o agente alcançou o escapeTarget ou colidiu com a Platform
        var currentRoom = objectiveSystem?.GetCurrentRoom();
        if (currentRoom != null)
        {
            if ((other.gameObject == currentRoom.escapeTarget || ((1 << other.gameObject.layer) & Platform) != 0) && !recompensaConcedida)
            {
                agentController.AddReward(recompensaEscaparSala);
                recompensaConcedida = true; // Marca a recompensa como concedida
                Debug.Log("Agente escapou da sala! Recompensa concedida.");
            }
        }
    }

    // Método chamado quando uma recompensa é adicionada
    public void OnAddReward(float reward)
    {
        Debug.Log($"Recompensa recebida no frame {Time.frameCount}: {reward}");
        frameReward += reward;
    }

    private void OnDestroy()
    {
        if (agentController != null)
        {
            agentController.OnAddReward -= OnAddReward;
        }
    }
}