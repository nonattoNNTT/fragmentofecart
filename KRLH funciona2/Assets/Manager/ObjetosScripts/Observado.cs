using UnityEngine;

// Responde uma pergunta só: este objeto esta sendo visto pelo jogador agora?
//
// Todo o terror da Fase A depende disto. Os ursinhos ficam parados enquanto a
// resposta for true e se aproximam quando for false — quem move eles e outro
// script, este aqui so decide.
//
// Sao duas checagens, nesta ordem, da mais barata para a mais cara:
//   1. Esta dentro do que a camera enxerga? (frustum)
//   2. Tem parede no meio? (linha de visao)
// Se a primeira falha, a segunda nem roda.
public class Observado : MonoBehaviour
{
    // =========================================================
    // CONFIGURACAO
    // =========================================================

    [Header("Referências")]
    [Tooltip("Deixe vazio para usar a Camera.main automaticamente.")]
    public Camera cameraDoJogador;

    [Header("Visão")]
    [Tooltip("Além desta distância o objeto nunca conta como visto.")]
    public float distanciaMaxima = 30f;

    [Tooltip("O que bloqueia a visão. Deixe fora as camadas dos próprios monstros.")]
    public LayerMask camadasQueBloqueiam = ~0;

    [Header("Desempenho")]
    [Tooltip("Segundos entre checagens. 0 = todo frame. Com 40 ursinhos na cena, deixe em 0.1.")]
    public float intervaloDeChecagem = 0.1f;

    [Header("Depuração")]
    [Tooltip("Desenha as linhas de visão na janela Scene enquanto o jogo roda.")]
    public bool mostrarNaCena = false;

    // =========================================================
    // ESTADO
    // =========================================================

    // Leia isto de fora. Nao escreva.
    public bool EstaSendoVisto { get; private set; }

    // Util para o teleporte: quanto tempo faz que o jogador desviou o olhar.
    public float TempoSemSerVisto { get; private set; }

    private Collider meuCollider;
    private Renderer meuRenderer;
    private float proximaChecagem;

    // Reaproveitados a cada checagem para nao alocar lixo todo frame
    private readonly Vector3[] pontosDeTeste = new Vector3[5];
    private static readonly Plane[] planosDaCamera = new Plane[6];

    // =========================================================
    // CICLO
    // =========================================================

    private void Awake()
    {
        meuCollider = GetComponent<Collider>();
        meuRenderer = GetComponent<Renderer>();

        if (cameraDoJogador == null)
        {
            cameraDoJogador = Camera.main;
        }
    }

    private void Update()
    {
        // A camera pode trocar em jogo (1a / 3a pessoa), entao reconferimos
        if (cameraDoJogador == null)
        {
            cameraDoJogador = Camera.main;
            if (cameraDoJogador == null)
            {
                EstaSendoVisto = false;
                return;
            }
        }

        if (Time.time < proximaChecagem)
        {
            if (!EstaSendoVisto)
            {
                TempoSemSerVisto += Time.deltaTime;
            }
            return;
        }

        proximaChecagem = Time.time + intervaloDeChecagem;

        bool visto = Verificar();

        if (visto)
        {
            TempoSemSerVisto = 0f;
        }
        else
        {
            TempoSemSerVisto += intervaloDeChecagem;
        }

        EstaSendoVisto = visto;
    }

    // =========================================================
    // VERIFICAR
    // =========================================================

    private bool Verificar()
    {
        Vector3 olho = cameraDoJogador.transform.position;

        // --- 1. distancia (a checagem mais barata que existe)
        float distancia = Vector3.Distance(olho, transform.position);
        if (distancia > distanciaMaxima)
        {
            return false;
        }

        // --- 2. dentro do campo de visao da camera?
        Bounds caixa = PegarCaixa();
        GeometryUtility.CalculateFrustumPlanes(cameraDoJogador, planosDaCamera);

        if (!GeometryUtility.TestPlanesAABB(planosDaCamera, caixa))
        {
            return false;
        }

        // --- 3. tem parede no meio?
        // Testamos varios pontos: se o ursinho aparece so pela metade atras de
        // uma caixa, ele CONTA como visto. Testar so o centro daria falso
        // negativo — e ai ele andaria na cara do jogador.
        MontarPontosDeTeste(caixa);

        for (int i = 0; i < pontosDeTeste.Length; i++)
        {
            if (TemLinhaLivre(olho, pontosDeTeste[i]))
            {
                if (mostrarNaCena)
                {
                    Debug.DrawLine(olho, pontosDeTeste[i], Color.green, intervaloDeChecagem);
                }
                return true;
            }

            if (mostrarNaCena)
            {
                Debug.DrawLine(olho, pontosDeTeste[i], Color.red, intervaloDeChecagem);
            }
        }

        return false;
    }

    private bool TemLinhaLivre(Vector3 de, Vector3 para)
    {
        RaycastHit hit;

        if (!Physics.Linecast(de, para, out hit, camadasQueBloqueiam, QueryTriggerInteraction.Ignore))
        {
            // Nada no caminho
            return true;
        }

        // Bater no proprio objeto nao conta como estar bloqueado
        return hit.transform == transform || hit.transform.IsChildOf(transform);
    }

    // Centro + 4 pontos espalhados pela caixa. Nao usamos os 8 cantos porque
    // dobraria o custo sem mudar o resultado na pratica.
    private void MontarPontosDeTeste(Bounds caixa)
    {
        Vector3 c = caixa.center;
        Vector3 e = caixa.extents * 0.8f;

        pontosDeTeste[0] = c;
        pontosDeTeste[1] = c + new Vector3(0f, e.y, 0f);
        pontosDeTeste[2] = c + new Vector3(0f, -e.y, 0f);
        pontosDeTeste[3] = c + new Vector3(e.x, 0f, e.z);
        pontosDeTeste[4] = c + new Vector3(-e.x, 0f, -e.z);
    }

    private Bounds PegarCaixa()
    {
        if (meuRenderer != null)
        {
            return meuRenderer.bounds;
        }

        if (meuCollider != null)
        {
            return meuCollider.bounds;
        }

        // Sem Renderer nem Collider: uma caixinha de meio metro no transform
        return new Bounds(transform.position, Vector3.one * 0.5f);
    }

    // =========================================================
    // AJUDA VISUAL NO EDITOR
    // =========================================================

    private void OnDrawGizmos()
    {
        if (!mostrarNaCena)
        {
            return;
        }

        Gizmos.color = EstaSendoVisto ? Color.green : Color.red;
        Bounds caixa = PegarCaixa();
        Gizmos.DrawWireCube(caixa.center, caixa.size);
    }
}
