using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class AlbertAgent : Agent
{
    [Header("Componentes")]
    public Rigidbody rb;
    public Transform target;

    [Header("Configuração dos Membros")]
    public ConfigurableJoint[] limbs;
    public float maxForcePerLimb = 1f;

    [Header("Configuração de Movimento")]
    public float moveSpeed = 1f;
    public float rotationSpeed = 50f;

    [Header("Limites de Velocidade")]
    public float maxVelocity = 5f;
    public float maxAngularVelocity = 7f;

    [Header("Configuração das Juntas")]
    public float jointSpring = 1000f;
    public float jointDamper = 100f;

    [Header("Penalização por Mudança de Força")]
    public float forceChangePenalty = 0.05f;

    [Header("Posição Inicial")]
    public Vector3 initialPosition = new Vector3(0, 0.5f, 0);
    public bool useRandomInitialPosition = false;
    public Vector3 randomPositionMin = new Vector3(-5f, 0.5f, -5f);
    public Vector3 randomPositionMax = new Vector3(5f, 0.5f, 5f);

    private Vector3[] previousForces;

    public override void Initialize()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        ConfigureJoints();

        if (limbs.Length > 0)
        {
            previousForces = new Vector3[limbs.Length];
            for (int i = 0; i < limbs.Length; i++)
            {
                previousForces[i] = Vector3.zero;
            }
        }

        // Configurar limites de velocidade dos rigidbodies
        rb.maxAngularVelocity = maxAngularVelocity;
        foreach (var limb in limbs)
        {
            var limbRb = limb.GetComponent<Rigidbody>();
            if (limbRb != null)
            {
                limbRb.maxAngularVelocity = maxAngularVelocity;
            }
        }
    }

    private void ConfigureJoints()
    {
        foreach (var joint in limbs)
        {
            // Configurar drives
            var drive = new JointDrive
            {
                positionSpring = jointSpring,
                positionDamper = jointDamper,
                maximumForce = 3.402823e+38f
            };

            joint.angularXDrive = drive;
            joint.angularYZDrive = drive;

            // Configurar projection
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.1f;
            joint.projectionAngle = 180;

            // Configurar quebra
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;

            // Configurar preprocessamento
            joint.enablePreprocessing = true;
        }
    }

    private void FixedUpdate()
    {
        // Limitar velocidade linear do corpo principal
        if (rb.linearVelocity.magnitude > maxVelocity)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
        }

        // Limitar velocidade angular do corpo principal
        if (rb.angularVelocity.magnitude > maxAngularVelocity)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
        }

        // Limitar velocidades dos membros
        foreach (var limb in limbs)
        {
            Rigidbody limbRb = limb.GetComponent<Rigidbody>();
            if (limbRb != null)
            {
                if (limbRb.linearVelocity.magnitude > maxVelocity)
                {
                    limbRb.linearVelocity = limbRb.linearVelocity.normalized * maxVelocity;
                }
                
                if (limbRb.angularVelocity.magnitude > maxAngularVelocity)
                {
                    limbRb.angularVelocity = limbRb.angularVelocity.normalized * maxAngularVelocity;
                }
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        // Resetar velocidades
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Resetar posição
        if (useRandomInitialPosition)
        {
            float randomX = Random.Range(randomPositionMin.x, randomPositionMax.x);
            float randomY = randomPositionMin.y;
            float randomZ = Random.Range(randomPositionMin.z, randomPositionMax.z);
            transform.position = new Vector3(randomX, randomY, randomZ);
        }
        else
        {
            transform.position = initialPosition;
        }

        // Resetar membros
        foreach (var joint in limbs)
        {
            var limbRb = joint.GetComponent<Rigidbody>();
            if (limbRb != null)
            {
                limbRb.linearVelocity = Vector3.zero;
                limbRb.angularVelocity = Vector3.zero;
                joint.transform.localRotation = Quaternion.identity;
            }
        }

        for (int i = 0; i < previousForces.Length; i++)
        {
            previousForces[i] = Vector3.zero;
        }

        // Mover alvo
        if (target != null)
        {
            target.position = new Vector3(
                Random.Range(-5f, 5f),
                0.5f,
                Random.Range(-5f, 5f)
            );
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Observações do corpo principal
        sensor.AddObservation(transform.position);
        sensor.AddObservation(rb.linearVelocity);
        sensor.AddObservation(rb.angularVelocity);

        // Observações do alvo
        if (target != null)
        {
            sensor.AddObservation(target.position);
            sensor.AddObservation((target.position - transform.position).normalized);
        }

        // Observações dos membros
        foreach (var limb in limbs)
        {
            // Posição e rotação relativas
            sensor.AddObservation(limb.transform.localPosition);
            sensor.AddObservation(limb.transform.localRotation);

            // Velocidades
            var limbRb = limb.GetComponent<Rigidbody>();
            if (limbRb != null)
            {
                sensor.AddObservation(limbRb.linearVelocity);
                sensor.AddObservation(limbRb.angularVelocity);
            }

            // Contato com o chão
            bool isGrounded = Physics.Raycast(
                limb.transform.position,
                Vector3.down,
                1.0f,
                LayerMask.GetMask("groundLayer")
            );
            sensor.AddObservation(isGrounded);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;

        // Movimento principal
        float moveX = continuousActions[0];
        float moveZ = continuousActions[1];
        float rotateY = continuousActions[2];

        // Aplicar movimento principal com ForceMode.Force
        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.fixedDeltaTime;
        rb.AddForce(move, ForceMode.Force);

        // Aplicar rotação
        float rotation = rotateY * rotationSpeed * Time.fixedDeltaTime;
        transform.Rotate(0, rotation, 0);

        // Controle independente dos membros
        for (int i = 0; i < limbs.Length; i++)
        {
            int baseIndex = 3 + (i * 3); // Índice inicial das forças para o membro atual

            if (baseIndex + 2 < continuousActions.Length)
            {
                // Extrair forças contínuas para o membro
                float forceX = continuousActions[baseIndex];
                float forceY = continuousActions[baseIndex + 1];
                float forceZ = continuousActions[baseIndex + 2];

                Vector3 limbForce = new Vector3(forceX, forceY, forceZ) * maxForcePerLimb;
                limbForce = Vector3.ClampMagnitude(limbForce, maxForcePerLimb);

                var limbRb = limbs[i].GetComponent<Rigidbody>();
                if (limbRb != null)
                {
                    limbRb.AddForce(limbForce, ForceMode.Force); // Aplicar força ao membro
                }

                // Penalidade por mudanças bruscas de força
                Vector3 forceChange = limbForce - previousForces[i];
                AddReward(-forceChangePenalty * forceChange.magnitude);

                // Atualizar força anterior
                previousForces[i] = limbForce;
            }
        }

        // Recompensa baseada na distância ao alvo
        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            // Recompensa contínua baseada na distância
            AddReward(-0.01f * distanceToTarget);

            // Verificar conclusão do episódio
            if (distanceToTarget < 1.0f)
            {
                SetReward(1.0f);
                EndEpisode();
            }
            else if (distanceToTarget > 20f)
            {
                SetReward(-1.0f);
                EndEpisode();
            }
        }
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        // Controles básicos de movimento
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetKey(KeyCode.Q) ? -1f : Input.GetKey(KeyCode.E) ? 1f : 0f;

        // Controles dos membros (torques)
        for (int i = 0; i < limbs.Length; i++)
        {
            int baseIndex = 3 + (i * 3);
            if (baseIndex + 2 < continuousActionsOut.Length)
            {
                continuousActionsOut[baseIndex] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f; // Torque X
                continuousActionsOut[baseIndex + 1] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f; // Torque Y
                continuousActionsOut[baseIndex + 2] = 0f; // Torque Z (opcional)
            }
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        // Penalidade por colisão com obstáculos
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            SetReward(-1.0f);
            EndEpisode();
        }
    }
}