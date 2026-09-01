using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractableScene : MonoBehaviour
{
    [Header("Scene")]
    public string sceneName;

    [Header("Highlight")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    [Header("Blink")]
    public float blinkSpeed = 4f;

    private Renderer[] renderers;

    private bool isHighlighted;
    private float blinkTimer;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        SetHighlight(false);
    }

    private void Update()
    {
        if (!isHighlighted)
            return;

        blinkTimer += Time.deltaTime * blinkSpeed;

        // Alterna entre normal e destaque
        bool showHighlight = Mathf.Sin(blinkTimer) > 0f;

        SetColor(
            showHighlight
                ? highlightColor
                : normalColor
        );
    }

    // =========================================================
    // INTERAÇÃO
    // =========================================================

    public void Interact()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Nome da cena não foi definido!");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // =========================================================
    // HIGHLIGHT
    // =========================================================

    public void SetHighlight(bool active)
    {
        isHighlighted = active;

        if (active)
        {
            blinkTimer = 0f;
            SetColor(highlightColor);
        }
        else
        {
            SetColor(normalColor);
        }
    }

    // =========================================================
    // COR
    // =========================================================

    private void SetColor(Color color)
    {
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                material.color = color;
            }
        }
    }
}