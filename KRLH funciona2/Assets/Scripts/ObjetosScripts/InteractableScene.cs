using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractableScene : MonoBehaviour
{
    [Header("Scene")]
    public string sceneName;

    [Header("Condition")]
    [SerializeField] private bool requireCondition = false;
    [SerializeField] private GameObject conditionObject;

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
        // Se precisa de condição, verifica primeiro
        if (requireCondition)
        {
            if (conditionObject == null)
            {
                Debug.LogWarning(
                    "A interação precisa de uma condição, " +
                    "mas nenhum Condition Object foi definido!"
                );

                return;
            }

            InteractionCondition condition =
                conditionObject.GetComponent<InteractionCondition>();

            if (condition == null)
            {
                Debug.LogWarning(
                    "O Condition Object não possui " +
                    "o script InteractionCondition!"
                );

                return;
            }

            if (!condition.IsCompleted)
            {
                Debug.Log(
                    "Interação bloqueada. " +
                    "A condição ainda não foi cumprida."
                );

                return;
            }
        }

        // Verifica o nome da cena
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning(
                "Nome da cena não foi definido!"
            );

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