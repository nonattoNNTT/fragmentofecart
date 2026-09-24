using UnityEngine;

// Faz a névoa respirar em vez de ser uma parede de cor parada.
// A densidade e a cor variam devagar, com ruído Perlin, e o resultado
// lê como fumaça passando na frente da câmera.
//
// Usa o fog do próprio Unity (RenderSettings), que no URP é praticamente
// de graça. Névoa volumétrica de verdade não cabe nos 40 fps do alvo.
public class NevoaViva : MonoBehaviour
{
    [Header("Densidade")]
    [Tooltip("Densidade base da névoa. Quanto maior, menos longe se enxerga.")]
    public float densidadeBase = 0.035f;

    [Tooltip("Quanto a densidade sobe e desce em volta da base, em fração. 0,4 = ±40%.")]
    [Range(0f, 1f)]
    public float variacao = 0.45f;

    [Tooltip("Velocidade da variação. Baixo = fumaça preguiçosa.")]
    public float velocidade = 0.08f;

    [Header("Cor")]
    [Tooltip("Cor principal da névoa. Use a paleta fechada do projeto.")]
    public Color corA = new Color(0.157f, 0.227f, 0.306f);

    [Tooltip("Cor para onde ela puxa de vez em quando. Dá o movimento de fumaça.")]
    public Color corB = new Color(0.008f, 0.004f, 0.024f);

    [Tooltip("Quanto a cor passeia entre as duas. 0 = cor fixa.")]
    [Range(0f, 1f)]
    public float variacaoDeCor = 0.35f;

    [Header("Rajadas")]
    [Tooltip("De vez em quando passa uma lufada mais densa. 0 desliga.")]
    [Range(0f, 1f)]
    public float forcaDasRajadas = 0.5f;

    [Tooltip("Velocidade das lufadas. Mais alto = passam mais seguido.")]
    public float velocidadeDasRajadas = 0.31f;

    private float semente;

    private void Start()
    {
        // Semente aleatória: duas partidas não começam com a mesma névoa
        semente = Random.Range(0f, 100f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
    }

    private void Update()
    {
        float t = Time.time;

        // Ruído lento: a massa da névoa indo e voltando
        float lento =
            Mathf.PerlinNoise(semente + t * velocidade, 0f);

        // Ruído mais rápido e mais raro: a lufada de fumaça
        float rajada =
            Mathf.PerlinNoise(0f, semente + t * velocidadeDasRajadas);

        // Ao cubo: fica perto de zero quase sempre e estoura de vez em quando,
        // que é o que faz parecer fumaça passando, e não pulsação.
        rajada = rajada * rajada * rajada;

        float fator =
            1f
            + (lento - 0.5f) * 2f * variacao
            + rajada * forcaDasRajadas;

        RenderSettings.fogDensity =
            Mathf.Max(0f, densidadeBase * fator);

        if (variacaoDeCor > 0f)
        {
            float mistura =
                Mathf.PerlinNoise(semente + t * velocidade * 0.7f, 10f)
                * variacaoDeCor;

            RenderSettings.fogColor =
                Color.Lerp(corA, corB, mistura);
        }
        else
        {
            RenderSettings.fogColor = corA;
        }
    }

    // Para conferir no Inspector durante o Play
    public float DensidadeAgora
    {
        get { return RenderSettings.fogDensity; }
    }
}
