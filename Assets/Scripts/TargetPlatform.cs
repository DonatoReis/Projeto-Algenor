using UnityEngine;
using System.Collections;

public class TargetPlatform : MonoBehaviour, IResettable
{
    [Header("Configurações de Cor")]
    [SerializeField] private Color corNormal = Color.red;
    [SerializeField] private Color corAtivada = Color.green;

    [Header("Configurações de Movimento")]
    [SerializeField] private float distanciaAbaixar = 0.3f;
    [SerializeField] private float velocidadeMovimento = 2f;

    private MeshRenderer meshRenderer;
    private Vector3 posicaoInicial;
    private Vector3 posicaoAbaixada;
    private bool estaEmContato = false;
    private Coroutine movimentoCoroutine;
    private RoomManager roomManager;
    public bool IsActivated { get; private set; } = false;

    [Header("Configurações de Áudio")]
    [SerializeField] private AudioClip somPiso;
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer não encontrado! Desativando o script.");
            enabled = false;
            return;
        }

        // Clona o material para evitar alterar o material original
        meshRenderer.material = new Material(meshRenderer.material);
        meshRenderer.material.color = corNormal;

        posicaoInicial = transform.position;
        posicaoAbaixada = posicaoInicial - new Vector3(0, distanciaAbaixar, 0);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public void SetRoomManager(RoomManager manager)
    {
        roomManager = manager;
    }

    public void ResetPlatform()
    {
        StopCoroutines();

        // Reseta a posição e a cor da plataforma
        transform.position = posicaoInicial;
        meshRenderer.material.color = corNormal;

        // Certifique-se de que as flags estão resetadas
        estaEmContato = false;
        IsActivated = false;
    }

    public void ResetObject()
    {
        ResetPlatform();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            AtivaPlataforma();

            if (audioSource != null && somPiso != null)
            {
                audioSource.PlayOneShot(somPiso);
            }
        }
    }

    private void AtivaPlataforma()
    {
        if (IsActivated) return;

        IsActivated = true; // Marca a plataforma como ativada
        meshRenderer.material.color = corAtivada;

        movimentoCoroutine = StartCoroutine(MoverPlataforma(transform.position, posicaoAbaixada));

        // Notifica o RoomManager
        roomManager?.CheckPlatforms();

        if (audioSource != null && somPiso != null)
        {
            audioSource.PlayOneShot(somPiso);
        }
    }

    private IEnumerator MoverPlataforma(Vector3 posicaoInicio, Vector3 posicaoAlvo)
    {
        while (Vector3.Distance(transform.position, posicaoAlvo) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, posicaoAlvo, velocidadeMovimento * Time.deltaTime);
            yield return null;
        }
        transform.position = posicaoAlvo;
    }

    private void StopCoroutines()
    {
        if (movimentoCoroutine != null)
        {
            StopCoroutine(movimentoCoroutine);
            movimentoCoroutine = null;
        }
    }
}
