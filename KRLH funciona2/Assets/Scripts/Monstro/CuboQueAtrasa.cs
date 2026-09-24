using UnityEngine;

// Vai junto com o MonsterAI nos cubinhos.
// Quando o cubo encosta na Valentina: ela fica lenta e ele recua para longe.
// Quem manda ele para longe é o próprio Retreat do MonsterAI; o tempo que ele
// fica fora é o 'retreatTime' do Inspector do MonsterAI.
[RequireComponent(typeof(MonsterAI))]
public class CuboQueAtrasa : MonoBehaviour
{
    [Header("Efeito no jogador")]
    [Tooltip("Segundos de lentidão. Vazio (0) usa o valor do próprio PlayerMovement.")]
    public float segundosDeLentidao = 10f;

    [Header("Recuo")]
    [Tooltip("Segundos que ele fica longe antes de voltar a procurar. Escreve no retreatTime do MonsterAI no Start.")]
    public float segundosLonge = 30f;

    private MonsterAI cerebro;

    private void Awake()
    {
        cerebro = GetComponent<MonsterAI>();
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
        // Mantém o Inspector do MonsterAI como fonte da verdade do tempo de recuo
        if (cerebro != null && segundosLonge > 0f)
        {
            cerebro.retreatTime = segundosLonge;
        }
    }

    // =========================================================
    // ENCOSTOU
    // =========================================================

    private void AoPegar(MonsterAI quem)
    {
        PlayerMovement movimento = AcharMovimento();

        if (movimento != null)
        {
            if (segundosDeLentidao > 0f)
            {
                movimento.AplicarLentidao(segundosDeLentidao);
            }
            else
            {
                movimento.AplicarLentidao();
            }
        }
        else
        {
            Debug.LogWarning(
                "⚠️ " + gameObject.name +
                " encostou mas não achou o PlayerMovement."
            );
        }

        Debug.Log(
            "🟦 " + gameObject.name +
            " encostou na Valentina. Recuando por " +
            (cerebro != null ? cerebro.retreatTime : segundosLonge).ToString("F0") + "s."
        );

        if (cerebro != null)
        {
            cerebro.ForcarRecuo();
        }
    }

    private PlayerMovement AcharMovimento()
    {
        // O MonsterAI já achou o jogador sozinho; aproveita a referência dele
        if (cerebro != null && cerebro.player != null)
        {
            PlayerMovement m =
                cerebro.player.GetComponentInParent<PlayerMovement>();

            if (m != null)
            {
                return m;
            }
        }

        return FindFirstObjectByType<PlayerMovement>();
    }
}
