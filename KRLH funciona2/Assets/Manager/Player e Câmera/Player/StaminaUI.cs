using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;

    [Header("Stamina Bar")]
    public Image fillImage;

    [Header("Cooldown")]
    public Image cooldownImage;

    [Tooltip("Imagens do cooldown, na ordem: 5s, 4.5s, 4s, 3.5s...")]
    public Sprite[] cooldownSprites;

    private void Start()
    {
        if (cooldownImage != null)
        {
            cooldownImage.gameObject.SetActive(false);
        }

        UpdateBar();
    }

    private void Update()
    {
        UpdateBar();
        UpdateCooldown();
    }

    // =========================
    // BARRA DE STAMINA
    // =========================

    private void UpdateBar()
    {
        if (playerMovement == null ||
            fillImage == null)
            return;

        float currentStamina =
            playerMovement.GetCurrentStamina();

        float maxStamina =
            playerMovement.GetMaxStamina();

        if (maxStamina <= 0f)
            return;

        fillImage.fillAmount =
            currentStamina / maxStamina;
    }

    // =========================
    // COOLDOWN
    // =========================

    private void UpdateCooldown()
    {
        if (playerMovement == null ||
            cooldownImage == null)
            return;

        // Se não está no cooldown
        if (!playerMovement.IsStaminaOnCooldown())
        {
            cooldownImage.gameObject.SetActive(false);
            return;
        }

        cooldownImage.gameObject.SetActive(true);

        float timeRemaining =
            playerMovement.GetStaminaCooldown();

        if (cooldownSprites == null ||
            cooldownSprites.Length == 0)
            return;

        // Cada imagem representa 0.5 segundo
        int spriteIndex =
            Mathf.CeilToInt(timeRemaining / 0.5f) - 1;

        spriteIndex = Mathf.Clamp(
            spriteIndex,
            0,
            cooldownSprites.Length - 1
        );

        cooldownImage.sprite =
            cooldownSprites[spriteIndex];
    }
}