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

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;

    private Vector2 moveInput;
    private bool sprintInput;

    private float currentStamina;

    private bool staminaDepleted;

    private bool cooldownActive;
    private float cooldownTimer;

    private float gravity = -9.81f;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        currentStamina = maxStamina;
    }

    private void Update()
    {
        UpdateStamina();
        UpdateCooldown();
        Move();
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