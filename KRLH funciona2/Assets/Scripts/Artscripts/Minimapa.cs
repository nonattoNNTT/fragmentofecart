using UnityEngine;
using UnityEngine.UI;

// Minimapa: o desenho do mapa fica parado na tela e o ponto anda em cima dele,
// na mesma proporcao em que a Valentina anda no labirinto.
//
// Nao usa camera nenhuma. A versao anterior desenhava o mapa ao vivo com uma
// segunda camera todo quadro; esta custa praticamente nada.
//
// PARA CALIBRAR: 'mundoMin' e 'mundoMax' sao o pedaco do mundo (X e Z) que o
// desenho cobre. O botao de contexto "Pegar limites do Labirinto", no canto do
// componente no Inspector, preenche os dois sozinho a partir da cena.
public class Minimapa : MonoBehaviour
{
    [Header("Quem seguir")]
    [Tooltip("A Valentina. Vazio = procura o PlayerMovement da cena.")]
    public Transform alvo;

    [Header("Pecas na tela")]
    [Tooltip("A imagem do mapa desenhado.")]
    public RectTransform mapa;

    [Tooltip("O ponto que mostra onde ela esta.")]
    public RectTransform ponto;

    [Header("Calibracao")]
    [Tooltip("Canto do mundo (X,Z) no canto INFERIOR ESQUERDO do desenho.")]
    public Vector2 mundoMin = new Vector2(-243f, -222f);

    [Tooltip("Canto do mundo (X,Z) no canto SUPERIOR DIREITO do desenho.")]
    public Vector2 mundoMax = new Vector2(243f, 222f);

    [Tooltip("Ligado: o ponto gira para onde ela olha. Desligue se o desenho for uma bolinha.")]
    public bool pontoGira = false;

    [Tooltip("Impede o ponto de sair da moldura do mapa.")]
    public bool prenderNaBorda = true;

    private PlayerMovement movimento;
    private float procuraTimer;

    private void Start()
    {
        if (mapa == null || ponto == null)
        {
            Debug.LogWarning(
                "Minimapa sem a imagem do mapa ou sem o ponto - nao vai aparecer."
            );

            enabled = false;

            return;
        }

        AcharAlvo();
    }

    private void LateUpdate()
    {
        AcharAlvo();

        if (alvo == null)
            return;

        PosicionarPonto();
    }

    // =========================================================
    // PONTO
    // =========================================================

    private void PosicionarPonto()
    {
        Vector2 tamanho = mundoMax - mundoMin;

        if (Mathf.Approximately(tamanho.x, 0f) ||
            Mathf.Approximately(tamanho.y, 0f))
            return;

        // Onde ela esta, de 0 a 1, dentro do pedaco de mundo que o desenho cobre
        float u = (alvo.position.x - mundoMin.x) / tamanho.x;
        float v = (alvo.position.z - mundoMin.y) / tamanho.y;

        if (prenderNaBorda)
        {
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);
        }

        // O mesmo 0 a 1, agora em pixels do desenho.
        // O -0,5 existe porque o ponto e posicionado a partir do centro do mapa.
        Vector2 tamanhoDoMapa = mapa.rect.size;

        ponto.anchoredPosition = new Vector2(
            (u - 0.5f) * tamanhoDoMapa.x,
            (v - 0.5f) * tamanhoDoMapa.y
        );

        if (pontoGira)
        {
            ponto.localRotation =
                Quaternion.Euler(0f, 0f, -alvo.eulerAngles.y);
        }
    }

    // =========================================================
    // ALVO
    // =========================================================

    // Procura de tempos em tempos, nao todo quadro: a Valentina pode
    // nascer depois, ou a cena trocar.
    private void AcharAlvo()
    {
        if (alvo != null)
            return;

        procuraTimer -= Time.deltaTime;

        if (procuraTimer > 0f)
            return;

        procuraTimer = 1f;

        movimento = FindFirstObjectByType<PlayerMovement>();

        if (movimento != null)
        {
            alvo = movimento.transform;
        }
    }

    // =========================================================
    // CALIBRACAO
    // =========================================================

    [ContextMenu("Pegar limites do Labirinto")]
    public void PegarLimitesDoLabirinto()
    {
        GameObject lab = GameObject.Find("Labirinto");

        if (lab == null)
        {
            Debug.LogWarning("Nao achei o objeto 'Labirinto' na cena.");
            return;
        }

        Renderer[] partes = lab.GetComponentsInChildren<Renderer>(true);

        if (partes.Length == 0)
            return;

        Bounds caixa = partes[0].bounds;

        foreach (Renderer r in partes)
        {
            caixa.Encapsulate(r.bounds);
        }

        mundoMin = new Vector2(caixa.min.x, caixa.min.z);
        mundoMax = new Vector2(caixa.max.x, caixa.max.z);

        Debug.Log(
            "Limites pegos do Labirinto: " + mundoMin + " ate " + mundoMax
        );
    }
}
