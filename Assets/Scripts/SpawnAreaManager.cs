using UnityEngine;

public class SpawnAreaManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("GameObject que define o centro da área de spawn")]
    public Transform spawnAreaCenter;
    
    [Tooltip("Tamanho da área de spawn em X")]
    public float spawnAreaWidth = 10f;
    
    [Tooltip("Tamanho da área de spawn em Z")]
    public float spawnAreaLength = 10f;
    
    [Tooltip("Altura do agente ao spawnar")]
    public float spawnHeight = 0.5f;

    [Header("Visual Settings")]
    [Tooltip("Cor da área de spawn no editor")]
    public Color spawnAreaColor = new Color(1f, 1f, 0f, 0.2f);
    public Color spawnAreaWireColor = Color.yellow;
    public bool showGizmos = true;

    [Header("Layer Settings")]
    [Tooltip("Layer para verificação do chão")]
    public LayerMask groundCheckLayer;

    private void OnValidate()
    {
        // Garante valores mínimos para as dimensões
        spawnAreaWidth = Mathf.Max(1f, spawnAreaWidth);
        spawnAreaLength = Mathf.Max(1f, spawnAreaLength);
        spawnHeight = Mathf.Max(0.1f, spawnHeight);

        // Auto-referencia o centro se não estiver definido
        if (spawnAreaCenter == null)
            spawnAreaCenter = transform;
    }

    public Vector3 GetRandomSpawnPosition()
    {
        if (spawnAreaCenter == null)
        {
            Debug.LogError("Spawn Area Center não definido!");
            return transform.position;
        }

        // Calcula os limites baseados no centro e tamanho
        float halfWidth = spawnAreaWidth / 2f;
        float halfLength = spawnAreaLength / 2f;

        // Gera posição aleatória dentro da área definida
        float randomX = Random.Range(-halfWidth, halfWidth);
        float randomZ = Random.Range(-halfLength, halfLength);

        // Posição final relativa ao centro da área de spawn
        Vector3 spawnPosition = spawnAreaCenter.position + 
            spawnAreaCenter.right * randomX + 
            spawnAreaCenter.forward * randomZ;

        // Ajusta a altura com raycast
        if (Physics.Raycast(spawnPosition + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 5f, groundCheckLayer))
        {
            spawnPosition.y = hit.point.y + spawnHeight;
        }
        else
        {
            spawnPosition.y = spawnAreaCenter.position.y + spawnHeight;
        }

        return spawnPosition;
    }

    public void RespawnAgent(GameObject agent)
    {
        if (agent == null) return;

        // Define nova posição
        agent.transform.position = GetRandomSpawnPosition();
        
        // Define rotação aleatória
        agent.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Reseta velocidades se tiver Rigidbody
        if (agent.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || spawnAreaCenter == null) return;

        // Matriz para rotacionar a área com o centro
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            spawnAreaCenter.position, 
            spawnAreaCenter.rotation, 
            Vector3.one
        );
        Gizmos.matrix = rotationMatrix;

        // Desenha área sólida
        Gizmos.color = spawnAreaColor;
        Vector3 size = new Vector3(spawnAreaWidth, 0.1f, spawnAreaLength);
        Gizmos.DrawCube(Vector3.zero, size);

        // Desenha contorno
        Gizmos.color = spawnAreaWireColor;
        Gizmos.DrawWireCube(Vector3.zero, size);

        // Reseta matriz
        Gizmos.matrix = Matrix4x4.identity;

        // Desenha pontos nos cantos da área
        Vector3[] corners = GetSpawnAreaCorners();
        float pointSize = 0.2f;
        Gizmos.color = Color.red;
        foreach (Vector3 corner in corners)
        {
            Gizmos.DrawSphere(corner, pointSize);
        }
    }

    private Vector3[] GetSpawnAreaCorners()
    {
        if (spawnAreaCenter == null) return new Vector3[0];

        float halfWidth = spawnAreaWidth / 2f;
        float halfLength = spawnAreaLength / 2f;

        Vector3[] corners = new Vector3[4];
        Vector3 center = spawnAreaCenter.position;
        Vector3 right = spawnAreaCenter.right;
        Vector3 forward = spawnAreaCenter.forward;

        corners[0] = center + (right * halfWidth) + (forward * halfLength);    // Frente Direita
        corners[1] = center + (right * halfWidth) - (forward * halfLength);    // Trás Direita
        corners[2] = center - (right * halfWidth) - (forward * halfLength);    // Trás Esquerda
        corners[3] = center - (right * halfWidth) + (forward * halfLength);    // Frente Esquerda

        return corners;
    }
}