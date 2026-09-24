using UnityEngine;

// Deixa marcas acesas no chão por onde o jogador passou, para ele saber
// onde já esteve. Cada marca some sozinha depois de 'duracao' segundos.
//
// As marcas NÃO são Light de verdade: são plaquinhas com material unlit.
// Luz real aqui seria dezenas de luzes dinâmicas ao mesmo tempo, e o alvo
// do projeto é 40+ fps no PC mais fraco do laboratório.
public class RastroDeLuz : MonoBehaviour
{
    [Header("Rastro")]
    [Tooltip("Distância em metros entre uma marca e a próxima.")]
    public float distanciaEntreMarcas = 3f;

    [Tooltip("Quanto tempo cada marca dura, em segundos. 180 = 3 minutos.")]
    public float duracao = 180f;

    [Tooltip("Teto de marcas vivas ao mesmo tempo. Ao estourar, a mais velha é reaproveitada.")]
    public int maxDeMarcas = 200;

    [Header("Aparência")]
    [Tooltip("Material da pegada do pé esquerdo.")]
    public Material pegadaEsquerda;

    [Tooltip("Material da pegada do pé direito.")]
    public Material pegadaDireita;

    [Tooltip("Quanto cada pegada sai para o lado, em metros. Dá o zigue-zague de quem anda.")]
    public float afastamentoLateral = 0.45f;

    [Tooltip("Tinta aplicada por cima da arte da pegada. Branco = a pegada sai como foi desenhada.")]
    public Color cor = Color.white;

    [Tooltip("Opacidade da pegada. Mais baixo = mais clara, mais discreta.")]
    [Range(0f, 1f)]
    public float opacidade = 0.45f;

    [Tooltip("Largura da marca, em metros.")]
    public float tamanhoDaMarca = 1.2f;

    [Tooltip("Altura acima do chão, para a marca não brigar com o piso (z-fighting).")]
    public float alturaAcimaDoChao = 0.03f;

    [Tooltip("Fração final da vida em que a marca vai apagando. 0,3 = apaga no último terço.")]
    [Range(0f, 1f)]
    public float fracaoQueApaga = 0.35f;

    private Transform[] marcas;
    private Renderer[] visuais;
    private float[] nascimento;
    private int proxima;
    private int vivas;

    private Vector3 ultimaPosicao;
    private Transform pasta;
    private MaterialPropertyBlock bloco;
    private bool proximaEhEsquerda = true;

    private static readonly int IdCor = Shader.PropertyToID("_BaseColor");
    private static readonly int IdCorAntiga = Shader.PropertyToID("_Color");

    private void Start()
    {
        if (pegadaEsquerda == null || pegadaDireita == null)
        {
            Debug.LogWarning(
                "⚠️ RastroDeLuz sem os materiais das pegadas — o rastro não vai aparecer."
            );

            enabled = false;

            return;
        }

        if (maxDeMarcas < 1)
        {
            maxDeMarcas = 1;
        }

        bloco = new MaterialPropertyBlock();

        pasta = new GameObject("RastroDeLuz (marcas)").transform;

        marcas = new Transform[maxDeMarcas];
        visuais = new Renderer[maxDeMarcas];
        nascimento = new float[maxDeMarcas];

        // Cria a piscina inteira de uma vez: nada de Instantiate durante o jogo.
        for (int i = 0; i < maxDeMarcas; i++)
        {
            GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Quad);

            marca.name = "marca";
            marca.transform.SetParent(pasta);

            // Sem collider: se não, a marca entra na frente do raycast
            // de interação e o jogador não consegue clicar em mais nada.
            Collider col = marca.GetComponent<Collider>();

            if (col != null)
            {
                Destroy(col);
            }

            Renderer r = marca.GetComponent<Renderer>();

            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            marca.SetActive(false);

            marcas[i] = marca.transform;
            visuais[i] = r;
        }

        ultimaPosicao = transform.position;
    }

    private void Update()
    {
        AtualizarApagamento();

        // Só conta o que andou no plano: subir ou descer não vale marca.
        Vector3 aqui = transform.position;

        Vector3 a = new Vector3(aqui.x, 0f, aqui.z);
        Vector3 b = new Vector3(ultimaPosicao.x, 0f, ultimaPosicao.z);

        if (Vector3.Distance(a, b) < distanciaEntreMarcas)
            return;

        ultimaPosicao = aqui;

        DeixarMarca(aqui);
    }

    // =========================================================
    // MARCAS
    // =========================================================

    private void DeixarMarca(Vector3 posicao)
    {
        // Cola a marca no chão de verdade, em vez de usar a altura do jogador
        RaycastHit hit;

        Vector3 pontoNoChao = posicao;

        if (Physics.Raycast(
            posicao + Vector3.up * 1f,
            Vector3.down,
            out hit,
            20f,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            pontoNoChao = hit.point;
        }

        Transform marca = marcas[proxima];

        // Alterna pé esquerdo e direito, e joga cada um para o seu lado.
        // Sem esse afastamento as pegadas saem em fila indiana, no meio.
        Vector3 lado =
            transform.right * afastamentoLateral *
            (proximaEhEsquerda ? -1f : 1f);

        marca.position =
            pontoNoChao + lado + Vector3.up * alturaAcimaDoChao;

        // Quad nasce em pé; deitar no chão é girar 90° no X.
        // O Y acompanha para onde a Valentina está olhando, senão a
        // pegada fica virada para um lado qualquer.
        marca.rotation = Quaternion.Euler(
            90f,
            transform.eulerAngles.y,
            0f
        );

        marca.localScale = Vector3.one * tamanhoDaMarca;

        visuais[proxima].sharedMaterial = proximaEhEsquerda
            ? pegadaEsquerda
            : pegadaDireita;

        proximaEhEsquerda = !proximaEhEsquerda;

        nascimento[proxima] = Time.time;

        marca.gameObject.SetActive(true);

        if (vivas < maxDeMarcas)
        {
            vivas++;
        }

        proxima = (proxima + 1) % maxDeMarcas;
    }

    private void AtualizarApagamento()
    {
        if (marcas == null)
            return;

        for (int i = 0; i < maxDeMarcas; i++)
        {
            if (marcas[i] == null || !marcas[i].gameObject.activeSelf)
                continue;

            float idade = Time.time - nascimento[i];

            if (idade >= duracao)
            {
                marcas[i].gameObject.SetActive(false);

                if (vivas > 0)
                {
                    vivas--;
                }

                continue;
            }

            // Fica acesa a maior parte da vida e apaga só no fim
            float restante = 1f - (idade / duracao);
            float alfa = 1f;

            if (fracaoQueApaga > 0f && restante < fracaoQueApaga)
            {
                alfa = restante / fracaoQueApaga;
            }

            Color c = cor;
            c.a = alfa * opacidade;

            // MaterialPropertyBlock: pinta cada marca sem criar
            // um material novo por objeto.
            visuais[i].GetPropertyBlock(bloco);

            bloco.SetColor(IdCor, c);
            bloco.SetColor(IdCorAntiga, c);

            visuais[i].SetPropertyBlock(bloco);
        }
    }

    // =========================================================
    // CONSULTA
    // =========================================================

    public int MarcasVivas
    {
        get { return vivas; }
    }

    public void LimparRastro()
    {
        if (marcas == null)
            return;

        for (int i = 0; i < maxDeMarcas; i++)
        {
            if (marcas[i] != null)
            {
                marcas[i].gameObject.SetActive(false);
            }
        }

        vivas = 0;
        proxima = 0;

        Debug.Log("🧹 Rastro limpo.");
    }
}
