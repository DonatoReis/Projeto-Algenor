using UnityEngine;
using Unity.MLAgents.Actuators;

public class AgentMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float moveSpeed = 6f;
    public float rotateSpeed = 200f;

    [Header("Configurações de Pulo")]
    public float groundCheckRadius = 5f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask obstacleLayer;
    public float maxJumpHeight = 5f;
    public float minJumpHeightThreshold = 0.5f; // Threshold mínimo para considerar como pulo

    [Header("Configurações de Áudio")]
    public AudioClip landSound; // Som ao aterrissar
    private AudioSource audioSource;

    private Rigidbody rb;
    private Vector3 startPosition;
    private bool isGrounded;
    private NavigationAgentController agentController;
    private RaycastHit groundHit;

    // Variáveis para o Curriculum Learning
    private bool movementAllowed = true;
    private bool jumpAllowed = true;

    // Variáveis para monitorar o pulo
    private bool isJumpingOverObstacle = false;
    private bool collidedWithObstacle = false;
    private bool wasGrounded = true;
    private float maxHeightReached = 0f;
    private float jumpStartHeight = 0f;

    private int jumpCount = 0;

    public struct MovementData
    {
        public Vector3 position;
        public Vector3 velocity;
        public bool isGrounded;
        public Vector3 forward;
        public bool isJumpingOverObstacle;
        public bool collidedWithObstacle;
        public float currentJumpHeight;
        public bool isJumping;
    }

    public void InitializeMovement(NavigationAgentController controller)
    {
        agentController = controller;
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.linearDamping = 0f; // Usando 'drag' corretamente

        // Configura o AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource não encontrado! Adicione um componente AudioSource ao agente.");
        }
    }

    public void ResetMovement()
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity; // Reseta a rotação para zero
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Resetar as flags de pulo
        isJumpingOverObstacle = false;
        collidedWithObstacle = false;
        wasGrounded = true;
        maxHeightReached = 0f;
        jumpStartHeight = 0f;

        jumpCount = 0;
    }

    public void UpdateMovement()
    {
        isGrounded = Physics.SphereCast(
            transform.position + Vector3.up * groundCheckRadius,
            groundCheckRadius,
            Vector3.down,
            out groundHit,
            groundCheckRadius * 2,
            groundLayer | wallLayer | obstacleLayer
        );

        // Lógica de detecção de pulo
        if (!isGrounded && wasGrounded)
        {
            // Iniciou um pulo
            jumpStartHeight = transform.position.y;
            maxHeightReached = jumpStartHeight;
        }
        else if (!isGrounded)
        {
            // Durante o pulo
            maxHeightReached = Mathf.Max(maxHeightReached, transform.position.y);
        }
        else if (!wasGrounded && isGrounded)
        {
            // Finalizou um pulo (Aterrissou)
            float jumpHeight = maxHeightReached - jumpStartHeight;
            if (jumpHeight > minJumpHeightThreshold)
            {
                isJumpingOverObstacle = true;
            }

            // Toca o som de aterrissagem
            if (audioSource != null && landSound != null)
            {
                audioSource.PlayOneShot(landSound);
            }
        }

        wasGrounded = isGrounded;
    }

    public void ProcessActions(ActionBuffers actions)
    {
        // Resetar as flags de pulo a cada ação
        isJumpingOverObstacle = false;
        collidedWithObstacle = false;

        float moveForward = movementAllowed ? actions.ContinuousActions[0] : 0f;
        float rotate = movementAllowed ? actions.ContinuousActions[1] : 0f;
        bool jump = actions.DiscreteActions[0] == 1 && jumpAllowed;

        // Movimento
        Vector3 movement = transform.forward * moveForward * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        // Rotação
        transform.Rotate(0, rotate * rotateSpeed * Time.fixedDeltaTime, 0);

        // Pulo
        if (jump && isGrounded)
        {
            float gravity = Physics.gravity.y;
            float jumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * maxJumpHeight);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
            jumpStartHeight = transform.position.y;
            jumpCount++; // Incrementa o contador de pulos
        }
    }

    public int GetJumpCount()
    {
        return jumpCount;
    }

    public void ResetJumpCount()
    {
        jumpCount = 0;
    }

    public void ProcessHeuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Usando Input.GetAxis para um controle mais suave
        continuousActionsOut[0] = Input.GetAxis("Vertical");   // W/S ou setas para cima/baixo
        continuousActionsOut[1] = Input.GetAxis("Horizontal"); // A/D ou setas para esquerda/direita
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    public MovementData GetMovementData()
    {
        return new MovementData
        {
            position = transform.position,
            velocity = rb.linearVelocity,
            isGrounded = isGrounded,
            forward = transform.forward,
            isJumpingOverObstacle = isJumpingOverObstacle,
            collidedWithObstacle = collidedWithObstacle,
            currentJumpHeight = maxHeightReached - jumpStartHeight,
            isJumping = !isGrounded && rb.linearVelocity.y > 0
        };
    }

    public void SetMovementAllowed(bool allowed)
    {
        movementAllowed = allowed;
        if (!allowed)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    public void SetJumpAllowed(bool allowed)
    {
        jumpAllowed = allowed;
    }

    public void SetMaxJumpHeight(float height)
    {
        maxJumpHeight = height;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            if (!isGrounded && rb.linearVelocity.y > 0)
            {
                isJumpingOverObstacle = true;
            }
            else
            {
                collidedWithObstacle = true;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            collidedWithObstacle = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * groundCheckRadius, groundCheckRadius);
        }
    }
}