using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Cortina preta que escurece a tela na saída e clareia na entrada.
//
// Cada cena tem o seu: a cena velha escurece, a Unity carrega a nova, e a nova
// clareia sozinha no Start. Assim não precisa de DontDestroyOnLoad nem de objeto
// que atravessa cena — que é onde esse tipo de sistema costuma quebrar.
//
// Quem chama a saída é o InterativoTrocaCena: se achar um FadeDeTela na cena ele
// usa, se não achar troca de cena na hora, como sempre fez.
public class FadeDeTela : MonoBehaviour
{
    [Header("Peças")]
    [Tooltip("A imagem que cobre a tela inteira. Vazio = procura nos filhos.")]
    public Image cortina;

    [Header("Entrada (clareia ao abrir a cena)")]
    [Tooltip("Desligue se esta cena não deve clarear ao abrir.")]
    public bool entrarComFade = true;

    [Tooltip("Segundos para clarear.")]
    public float duracaoDaEntrada = 0.8f;

    [Header("Saída (escurece antes de trocar de cena)")]
    [Tooltip("Segundos para escurecer.")]
    public float duracaoDaSaida = 0.6f;

    [Header("Cor")]
    [Tooltip("Cor da cortina. O padrão é o preto da paleta, #020106.")]
    public Color cor = new Color(0.008f, 0.004f, 0.024f, 1f);

    private bool trocando;

    private void Reset()
    {
        cortina = GetComponentInChildren<Image>(true);
    }

    private void Awake()
    {
        if (cortina == null)
        {
            cortina = GetComponentInChildren<Image>(true);
        }

        if (cortina == null)
        {
            Debug.LogWarning(
                "⚠️ " + gameObject.name +
                ": FadeDeTela sem a imagem da cortina — não vai escurecer nada."
            );

            enabled = false;

            return;
        }

        // Já começa preto, senão o primeiro quadro da cena aparece antes do fade
        AplicarAlpha(entrarComFade ? 1f : 0f);
    }

    private void Start()
    {
        if (entrarComFade)
        {
            StartCoroutine(Clarear());
        }
    }

    // =========================================================
    // ENTRADA
    // =========================================================

    private System.Collections.IEnumerator Clarear()
    {
        float tempo = 0f;

        while (tempo < duracaoDaEntrada)
        {
            // unscaled: se alguém pausar o jogo com timeScale, o fade não trava
            tempo += Time.unscaledDeltaTime;

            AplicarAlpha(1f - Mathf.Clamp01(tempo / duracaoDaEntrada));

            yield return null;
        }

        AplicarAlpha(0f);
    }

    // =========================================================
    // SAÍDA
    // =========================================================

    // Escurece e só então troca de cena
    public void SairEDepoisCarregar(string nomeDaCena)
    {
        if (trocando)
            return;

        if (string.IsNullOrEmpty(nomeDaCena))
        {
            Debug.LogError(
                "❌ FadeDeTela: pediram para trocar de cena sem dizer o nome."
            );

            return;
        }

        if (cortina == null || duracaoDaSaida <= 0f)
        {
            SceneManager.LoadScene(nomeDaCena);

            return;
        }

        trocando = true;

        StartCoroutine(EscurecerECarregar(nomeDaCena));
    }

    private System.Collections.IEnumerator EscurecerECarregar(string nomeDaCena)
    {
        float tempo = 0f;

        while (tempo < duracaoDaSaida)
        {
            tempo += Time.unscaledDeltaTime;

            AplicarAlpha(Mathf.Clamp01(tempo / duracaoDaSaida));

            yield return null;
        }

        AplicarAlpha(1f);

        SceneManager.LoadScene(nomeDaCena);
    }

    // =========================================================
    // CORTINA
    // =========================================================

    private void AplicarAlpha(float alpha)
    {
        if (cortina == null)
            return;

        cortina.color = new Color(cor.r, cor.g, cor.b, alpha);

        // Transparente não pode roubar o clique dos botões que estão embaixo
        cortina.raycastTarget = alpha > 0.01f;
    }
}
