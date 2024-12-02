using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour, IResettable
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float alturaAbertura = 4f;
    [SerializeField] private float velocidadeMovimento = 5f;

    [Header("Configurações de Áudio")]
    [SerializeField] private AudioClip somAbrirPorta;
    [SerializeField] private AudioClip somFecharPorta;
    [SerializeField] private AudioClip somMovimentoPorta;
    [SerializeField] private float volumeSom = 1f;
    [SerializeField] private bool usarSomContinuo = true;

    private Vector3 posicaoFechada;
    private Vector3 posicaoAberta;
    private Coroutine movimentoCoroutine;
    private bool portaAberta = false;
    private AudioSource audioSource;

    // Propriedade pública para acessar o estado da porta
    public bool PortaAberta
    {
        get { return portaAberta; }
    }

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        // Inicialização de posicaoFechada e posicaoAberta
        posicaoFechada = transform.position;
        posicaoAberta = posicaoFechada + Vector3.up * alturaAbertura;

        // Configuração do AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configurações básicas do AudioSource
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f;
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 50f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.volume = volumeSom;

        // Inicialização do MeshRenderer
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer não encontrado! Desativando o script.");
            enabled = false;
        }
    }

    public void ResetObject()
    {
        ResetDoor();
    }

    public void OpenDoor()
    {
        Debug.Log("OpenDoor chamada."); // Log para verificar a execução
        if (!portaAberta)
        {
            portaAberta = true;
            MoverPara(posicaoAberta);

            // Toca o som de abrir
            if (somAbrirPorta != null)
            {
                Debug.Log("Tocando som de abrir porta.");
                audioSource.PlayOneShot(somAbrirPorta, volumeSom);
            }
        }
    }

    public void CloseDoor()
    {
        Debug.Log("CloseDoor chamada."); // Log para verificar a execução
        if (portaAberta)
        {
            portaAberta = false;
            MoverPara(posicaoFechada);

            // Toca o som de fechar
            if (somFecharPorta != null)
            {
                Debug.Log("Tocando som de fechar porta.");
                audioSource.PlayOneShot(somFecharPorta, volumeSom);
            }
        }
    }

    private void MoverPara(Vector3 posicaoAlvo)
    {
        if (movimentoCoroutine != null)
        {
            StopCoroutine(movimentoCoroutine);
        }
        movimentoCoroutine = StartCoroutine(MoverPorta(posicaoAlvo));
    }

    private IEnumerator MoverPorta(Vector3 posicaoAlvo)
    {
        float distanciaInicial = Vector3.Distance(transform.position, posicaoAlvo);

        if (usarSomContinuo && somMovimentoPorta != null)
        {
            Debug.Log("Tocando som de movimento contínuo.");
            audioSource.clip = somMovimentoPorta;
            audioSource.loop = true;
            audioSource.Play();
        }

        while (Vector3.Distance(transform.position, posicaoAlvo) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, posicaoAlvo, velocidadeMovimento * Time.deltaTime);
            yield return null;
        }

        transform.position = posicaoAlvo;

        if (usarSomContinuo && audioSource.isPlaying && somMovimentoPorta != null)
        {
            Debug.Log("Parando som de movimento contínuo.");
            audioSource.Stop();
        }
    }

    public void ResetDoor()
    {
        Debug.Log("ResetDoor chamada.");

        // Parar quaisquer corrotinas em andamento
        StopCoroutines();

        // Resetar a posição da porta para fechada
        transform.position = posicaoFechada;

        // Resetar o estado da porta
        portaAberta = false;

        // Resetar o AudioSource
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void StopCoroutines()
    {
        if (movimentoCoroutine != null)
        {
            StopCoroutine(movimentoCoroutine);
            movimentoCoroutine = null;
        }

        // Caso existam outras corrotinas relacionadas à porta, pare-as aqui
    }

    private void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
