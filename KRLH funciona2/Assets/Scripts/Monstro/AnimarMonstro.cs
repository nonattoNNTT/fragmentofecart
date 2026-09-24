using UnityEngine;
using UnityEngine.AI;

// Liga as animações de um monstro na velocidade real do NavMeshAgent.
// Serve para o ursinho e para qualquer outro que use MonsterAI.
//
// O parâmetro 'Speed' segue a mesma escala do Animator da Valentina e do Ursão:
// 0 = parado · 0,5 = andando · 1 = correndo. Não inventa padrão novo.
[RequireComponent(typeof(NavMeshAgent))]
public class AnimarMonstro : MonoBehaviour
{
    [Header("Animação")]
    [Tooltip("Animator do modelo. Vazio = procura sozinho nos filhos.")]
    public Animator animador;

    [Tooltip("Nome do parâmetro Float que escolhe idle / walk / run.")]
    public string parametroVelocidade = "Speed";

    [Tooltip("Acima desta velocidade ele usa a animação de correr.")]
    public float velocidadeDeCorrida = 3f;

    [Tooltip("Abaixo desta velocidade ele fica parado.")]
    public float velocidadeParado = 0.15f;

    [Tooltip("Suaviza a troca entre as animações.")]
    public float suavizacao = 0.12f;

    private NavMeshAgent agente;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();

        if (animador == null)
        {
            animador = GetComponentInChildren<Animator>(true);
        }
    }

    private void Start()
    {
        if (animador == null)
        {
            Debug.LogWarning(
                "⚠️ " + gameObject.name +
                " sem Animator — as animações não vão rodar."
            );

            enabled = false;

            return;
        }

        if (animador.runtimeAnimatorController == null)
        {
            Debug.LogWarning(
                "⚠️ " + gameObject.name +
                " tem Animator mas nenhum Controller."
            );
        }

        // Quem move o monstro é o NavMeshAgent, não a animação
        animador.applyRootMotion = false;
    }

    private void Update()
    {
        if (animador == null || agente == null)
            return;

        float velocidade = agente.velocity.magnitude;

        float valor;

        if (velocidade < velocidadeParado)
        {
            valor = 0f;
        }
        else if (velocidade >= velocidadeDeCorrida)
        {
            valor = 1f;
        }
        else
        {
            valor = 0.5f;
        }

        animador.SetFloat(
            parametroVelocidade,
            valor,
            suavizacao,
            Time.deltaTime
        );
    }
}
