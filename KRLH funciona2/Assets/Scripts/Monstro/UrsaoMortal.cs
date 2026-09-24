using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Vai junto com o MonsterAI no Ursão.
// Faz duas coisas: liga as animações dele na velocidade do NavMeshAgent,
// e, quando ele pega a Valentina, troca de cena.
[RequireComponent(typeof(MonsterAI))]
public class UrsaoMortal : MonoBehaviour
{
    [Header("Quando ele mata")]
    [Tooltip("Cena que abre quando o Ursão pega a Valentina.")]
    public string cenaAoMorrer = "Tela_inicial";

    [Header("Caçada")]
    [Tooltip("Ligado: o Ursão enxerga a Valentina através das paredes e nunca desiste de caçar.")]
    public bool cacaSempre = true;

    [Tooltip("Uma layer SEM NENHUM objeto. É o truque para nada bloquear a visão dele.")]
    public int layerSemNada = 30;

    [Header("Animação")]
    [Tooltip("Animator do Ursão. Vazio = procura sozinho nos filhos.")]
    public Animator animador;

    [Tooltip("Nome do parâmetro Float que escolhe idle / walk / run.")]
    public string parametroVelocidade = "Speed";

    [Tooltip("Acima desta velocidade ele entra em Run.")]
    public float velocidadeDeCorrida = 4.5f;

    [Tooltip("Suaviza a troca entre as animações.")]
    public float suavizacao = 0.15f;

    private MonsterAI cerebro;
    private NavMeshAgent agente;
    private bool jaMatou;

    private void Awake()
    {
        cerebro = GetComponent<MonsterAI>();
        agente = GetComponent<NavMeshAgent>();

        if (animador == null)
        {
            animador = GetComponentInChildren<Animator>(true);
        }
    }

    private void OnEnable()
    {
        if (cerebro != null)
        {
            cerebro.AoPegarOJogador += AoPegar;
        }
    }

    private void OnDisable()
    {
        if (cerebro != null)
        {
            cerebro.AoPegarOJogador -= AoPegar;
        }
    }

    private void Start()
    {
        if (animador == null)
        {
            Debug.LogWarning(
                "⚠️ " + gameObject.name +
                " sem Animator — as animações do Ursão não vão rodar."
            );
        }

        AplicarCacadaEterna();
    }

    // O Start roda depois do Awake do MonsterAI, então isto manda por último.
    // Fica aqui de propósito: esses valores já se perderam duas vezes quando
    // moravam só no Inspector, e sem eles o Ursão volta a desistir da caçada.
    private void AplicarCacadaEterna()
    {
        if (!cacaSempre || cerebro == null)
            return;

        // Nada na cena está nessa layer, então o Linecast que procura parede
        // no caminho da visão nunca acerta nada: ele enxerga o labirinto todo.
        cerebro.obstacleMask = 1 << layerSemNada;

        cerebro.viewDistance = 1000f;
        cerebro.viewAngle = 360f;
        cerebro.peripheralDistance = 1000f;
        cerebro.peripheralAngle = 360f;
        cerebro.nearSenseRadius = 1000f;
        cerebro.darkViewMultiplier = 1f;

        cerebro.awarenessGain = 100f;
        cerebro.awarenessDecay = 0f;

        // Os dois que faziam ele desistir
        cerebro.menaceLimit = 999999f;
        cerebro.maxHuntTime = 999999f;
        cerebro.loseSightTime = 999999f;

        Debug.Log("🐻 Ursão em caçada eterna: enxerga através das paredes e não desiste.");
    }

    // =========================================================
    // ANIMAÇÃO
    // =========================================================

    private void Update()
    {
        if (animador == null || agente == null)
            return;

        // 0 = parado · 0,5 = andando · 1 = correndo.
        // Mesma escala do Animator da Valentina, para não inventar padrão novo.
        float velocidade = agente.velocity.magnitude;

        float valor = velocidade < 0.1f
            ? 0f
            : (velocidade >= velocidadeDeCorrida ? 1f : 0.5f);

        animador.SetFloat(
            parametroVelocidade,
            valor,
            suavizacao,
            Time.deltaTime
        );
    }

    // =========================================================
    // PEGOU
    // =========================================================

    private void AoPegar(MonsterAI quem)
    {
        // Sem isto, o MonsterAI chama de novo no quadro seguinte
        // e o LoadScene dispara duas vezes.
        if (jaMatou)
            return;

        jaMatou = true;

        if (string.IsNullOrEmpty(cenaAoMorrer))
        {
            Debug.LogError(
                "❌ " + gameObject.name +
                ": 'cenaAoMorrer' está vazio, não dá para trocar de cena."
            );

            return;
        }

        Debug.Log(
            "🐻 O Ursão pegou a Valentina. Abrindo '" + cenaAoMorrer + "'."
        );

        SceneManager.LoadScene(cenaAoMorrer);
    }
}
