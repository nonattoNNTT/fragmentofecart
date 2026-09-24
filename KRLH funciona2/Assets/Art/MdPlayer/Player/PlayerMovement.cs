using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;

    [Header("Stamina")]
    public float maxStamina = 5f;
    public float staminaDrain = 1f;
    public float staminaRecovery = 1.5f;

    [Header("Cooldown")]
    public float staminaCooldownDuration = 5f;

    [Header("Lentidão")]
    [Tooltip("Quanto da velocidade sobra enquanto está lenta. 0,45 = 45% do normal.")]
    public float fatorDeLentidao = 0.45f;

    [Tooltip("Quantos segundos a lentidão dura quando um monstro encosta.")]
    public float duracaoDaLentidao = 10f;

    [Header("Animação")]
    [Tooltip("Suaviza a troca entre parado, andando e correndo. 0 troca seco.")]
    public float suavizacaoAnimacao = 0.1f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    private CharacterController controller;

    private Vector2 moveInput;
    private bool sprintInput;

    private float currentStamina;

    private bool staminaDepleted;

    private bool cooldownActive;
    private float cooldownTimer;

    private float gravity = -9.81f;
    private float verticalVelocity;

    private float tempoDeLentidao;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        currentStamina = maxStamina;

        // Se o Animator não foi colocado no Inspector,
        // tenta encontrar automaticamente no Player ou nos filhos.
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        UpdateLentidao();
        UpdateStamina();
        UpdateCooldown();
        Move();
        UpdateAnimation();
    }

    // =========================================================
    // LENTIDÃO
    // =========================================================

    // Chamado pelos monstros quando encostam nela.
    // Encostar de novo antes de acabar renova o tempo, não soma.
    public void AplicarLentidao()
    {
        AplicarLentidao(duracaoDaLentidao);
    }

    public void AplicarLentidao(float segundos)
    {
        tempoDeLentidao = Mathf.Max(tempoDeLentidao, segundos);

        Debug.Log(
            "🐌 Valentina mais lenta por " +
            tempoDeLentidao.ToString("F0") + "s"
        );
    }

    private void UpdateLentidao()
    {
        if (tempoDeLentidao <= 0f)
            return;

        tempoDeLentidao -= Time.deltaTime;

        if (tempoDeLentidao <= 0f)
        {
            tempoDeLentidao = 0f;

            Debug.Log("🏃 Velocidade normal de volta.");
        }
    }

    public bool EstaLenta
    {
        get { return tempoDeLentidao > 0f; }
    }

    public float TempoDeLentidaoRestante
    {
        get { return tempoDeLentidao; }
    }

    // =========================
    // INPUT
    // =========================

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintInput = context.ReadValueAsButton();

        // Precisa soltar o Shift para correr novamente
        if (!sprintInput && !cooldownActive)
        {
            staminaDepleted = false;
        }
    }

    // =========================
    // MOVEMENT
    // =========================

    private void Move()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move =
            forward * moveInput.y +
            right * moveInput.x;

        bool isSprinting =
            sprintInput &&
            !staminaDepleted &&
            !cooldownActive &&
            moveInput.magnitude > 0.1f &&
            currentStamina > 0f;

        float speed = isSprinting
            ? sprintSpeed
            : walkSpeed;

        // Monstro encostou: ela anda devagar até o tempo passar
        if (tempoDeLentidao > 0f)
        {
            speed *= fatorDeLentidao;
        }

        // Gravidade
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed;
        velocity.y = verticalVelocity;

        controller.Move(
            velocity * Time.deltaTime
        );
    }

    // =========================
    // ANIMATION
    // =========================

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        // 0 = parado · até 0,5 = andando · 1 = correndo.
        // É esse número que faz o Animator escolher idle, walk ou run.
        float velocidade =
            moveInput.magnitude *
            (IsSprinting() ? 1f : 0.5f);

        animator.SetFloat(
            "Speed",
            velocidade,
            suavizacaoAnimacao,
            Time.deltaTime
        );
    }

    // =========================
    // STAMINA
    // =========================

    private void UpdateStamina()
    {
        // Durante o cooldown não recupera stamina
        if (cooldownActive)
            return;

        bool isSprinting =
            sprintInput &&
            !staminaDepleted &&
            moveInput.magnitude > 0.1f &&
            currentStamina > 0f;

        if (isSprinting)
        {
            currentStamina -=
                staminaDrain * Time.deltaTime;

            currentStamina = Mathf.Clamp(
                currentStamina,
                0f,
                maxStamina
            );

            // Chegou a zero
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;

                staminaDepleted = true;

                StartStaminaCooldown();
            }
        }
        else
        {
            currentStamina +=
                staminaRecovery * Time.deltaTime;

            currentStamina = Mathf.Clamp(
                currentStamina,
                0f,
                maxStamina
            );
        }
    }

    // =========================
    // COOLDOWN
    // =========================

    private void StartStaminaCooldown()
    {
        cooldownActive = true;

        cooldownTimer =
            staminaCooldownDuration;
    }

    private void UpdateCooldown()
    {
        if (!cooldownActive)
            return;

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer <= 0f)
        {
            cooldownTimer = 0f;

            cooldownActive = false;

            // A stamina pode começar a recuperar,
            // mas ainda precisa soltar o Shift.
        }
    }

    // =========================
    // GETTERS - STAMINA
    // =========================

    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }

    public float GetStaminaCooldown()
    {
        return cooldownTimer;
    }

    public bool IsStaminaOnCooldown()
    {
        return cooldownActive;
    }

    public bool IsStaminaDepleted()
    {
        return staminaDepleted;
    }

    // =========================
    // GETTERS - HEADBOB
    // =========================

    public Vector2 GetMovementInput()
    {
        return moveInput;
    }

    public bool IsSprinting()
    {
        return sprintInput &&
               !staminaDepleted &&
               !cooldownActive &&
               moveInput.magnitude > 0.1f &&
               currentStamina > 0f;
    }
}