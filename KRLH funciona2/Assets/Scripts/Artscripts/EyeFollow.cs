using UnityEngine;
using UnityEngine.InputSystem;

// Faz a íris olhar para o cursor sem sair da amêndoa do olho.
//
// A íris não gira: ela DESLIZA dentro do olho, como olho de verdade. O limite é
// uma elipse inclinada, porque as amêndoas desta arte estão tortas (uns -40°).
//
// A versão antiga girava o objeto com 'transform.up' e usava ScreenToWorldPoint,
// que não vale para Canvas em Screen Space Overlay — a íris saía voando.
//
// OS VALORES JÁ VÊM MEDIDOS da arte 'interface sem bolas.png': o centro é o meio
// da amêndoa e os raios são o quanto a íris anda antes de vazar para o preto.
public class EyeFollow : MonoBehaviour
{
    [Header("Repouso")]
    [Tooltip("Onde a íris fica quando o cursor está no centro. Vazio = usa a posição atual ao ligar o jogo.")]
    public bool usarPosicaoAtualComoCentro = true;

    [Tooltip("Centro da amêndoa, se não for usar a posição atual.")]
    public Vector2 centro;

    [Header("Limite do olho")]
    [Tooltip("Quanto a íris anda no sentido COMPRIDO do olho, em pixels.")]
    public float raioLongo = 90f;

    [Tooltip("Quanto a íris anda no sentido ESTREITO do olho, em pixels.")]
    public float raioCurto = 13f;

    [Tooltip("Inclinação da amêndoa, em graus. Esta arte usa entre -36 e -45.")]
    public float inclinacao = -40f;

    [Header("Resposta")]
    [Tooltip("A que distância do cursor a íris já está no limite. Menor = mais nervoso.")]
    public float distanciaMaxima = 700f;

    [Tooltip("Segundos para alcançar o cursor. 0 = gruda no cursor sem atraso.")]
    public float suavizacao = 0.08f;

    private RectTransform meuRect;
    private RectTransform rectDoPai;
    private Vector2 destino;
    private Vector2 velocidade;

    private void Awake()
    {
        meuRect = GetComponent<RectTransform>();

        if (meuRect == null)
        {
            Debug.LogWarning(
                "⚠️ " + gameObject.name +
                ": o EyeFollow só funciona em objeto de UI (RectTransform)."
            );

            enabled = false;

            return;
        }

        rectDoPai = meuRect.parent as RectTransform;

        if (usarPosicaoAtualComoCentro)
        {
            centro = meuRect.anchoredPosition;
        }

        destino = centro;
    }

    private void Update()
    {
        if (Mouse.current == null || rectDoPai == null)
            return;

        SeguirCursor();
    }

    // =========================================================
    // SEGUIR O CURSOR
    // =========================================================

    private void SeguirCursor()
    {
        Vector2 cursorNaTela = Mouse.current.position.ReadValue();

        Vector2 cursorLocal;

        // A câmera vai null de propósito: o Canvas é Screen Space Overlay.
        // Com Screen Space Camera, troque o null pela câmera do Canvas.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectDoPai,
                cursorNaTela,
                null,
                out cursorLocal))
        {
            return;
        }

        Vector2 paraOCursor = cursorLocal - centro;

        if (paraOCursor.sqrMagnitude < 0.0001f)
        {
            destino = centro;
        }
        else
        {
            // Gira para o sistema do olho, onde X é o sentido comprido
            float radianos = -inclinacao * Mathf.Deg2Rad;

            float cos = Mathf.Cos(radianos);
            float sen = Mathf.Sin(radianos);

            Vector2 direcao = new Vector2(
                paraOCursor.x * cos - paraOCursor.y * sen,
                paraOCursor.x * sen + paraOCursor.y * cos
            ).normalized;

            // Até onde a elipse deixa ir NESTA direção.
            // Sem isto a íris andaria o mesmo tanto para todo lado e vazaria
            // pelo lado estreito do olho.
            float limite = LimiteNaDirecao(direcao);

            // Perto do olho ela quase não mexe; longe, vai até o limite
            float forca = Mathf.Clamp01(
                paraOCursor.magnitude / Mathf.Max(1f, distanciaMaxima)
            );

            Vector2 deslocamento = direcao * limite * forca;

            // Volta para o sistema da tela
            float voltaCos = Mathf.Cos(-radianos);
            float voltaSen = Mathf.Sin(-radianos);

            destino = centro + new Vector2(
                deslocamento.x * voltaCos - deslocamento.y * voltaSen,
                deslocamento.x * voltaSen + deslocamento.y * voltaCos
            );
        }

        if (suavizacao <= 0f)
        {
            meuRect.anchoredPosition = destino;
        }
        else
        {
            meuRect.anchoredPosition = Vector2.SmoothDamp(
                meuRect.anchoredPosition,
                destino,
                ref velocidade,
                suavizacao
            );
        }
    }

    // Raio da elipse na direção pedida. 'direcao' já vem normalizada
    // e no sistema do olho.
    private float LimiteNaDirecao(Vector2 direcao)
    {
        float a = Mathf.Max(0.01f, raioLongo);
        float b = Mathf.Max(0.01f, raioCurto);

        float x = direcao.x / a;
        float y = direcao.y / b;

        return 1f / Mathf.Sqrt(x * x + y * y);
    }

    // =========================================================
    // CONFERIR NO EDITOR
    // =========================================================

    // Desenha a elipse do limite na janela Scene, para ajustar no olho
    private void OnDrawGizmosSelected()
    {
        RectTransform rect = GetComponent<RectTransform>();

        if (rect == null || rect.parent == null)
            return;

        Vector2 meio = Application.isPlaying
            ? centro
            : (usarPosicaoAtualComoCentro ? rect.anchoredPosition : centro);

        Gizmos.color = Color.cyan;

        Vector3 anterior = Vector3.zero;

        for (int i = 0; i <= 48; i++)
        {
            float t = i / 48f * Mathf.PI * 2f;

            Vector2 p = new Vector2(
                Mathf.Cos(t) * raioLongo,
                Mathf.Sin(t) * raioCurto
            );

            p = Quaternion.Euler(0f, 0f, inclinacao) * p;

            Vector3 mundo = rect.parent.TransformPoint(meio + p);

            if (i > 0)
                Gizmos.DrawLine(anterior, mundo);

            anterior = mundo;
        }
    }
}
