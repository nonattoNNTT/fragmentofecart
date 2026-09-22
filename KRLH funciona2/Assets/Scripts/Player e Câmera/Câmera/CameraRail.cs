using UnityEngine;

public class CameraRail : MonoBehaviour
{
    [Header("Câmera Cinemática")]
    public Camera cameraCinematica;

    [Header("Pontos do Trilho")]
    public Transform[] pontos;

    [Header("Configuração")]
    public float duracaoPorTrecho = 3f;

    [Tooltip("Segundos parado no último ponto antes de devolver o controle ao jogador. 0 devolve na hora.")]
    public float esperaNoFim = 4f;

    [Header("Fim do trilho — devolve a gameplay")]
    [Tooltip("O CameraController do Player. No fim do trilho ele volta para a primeira pessoa.")]
    public CameraController cameraDoPlayer;

    [Tooltip("O PlayerMovement do Player. Fica desligado durante o filminho.")]
    public PlayerMovement movimentoDoPlayer;

    [Header("Some durante o filminho")]
    [Tooltip("Canvas da gameplay. Voltam no fim exatamente como estavam antes do filminho.")]
    public GameObject[] canvasDaGameplay;

    [Tooltip("Scripts que ficam pausados durante o filminho (interação, etc).")]
    public MonoBehaviour[] scriptsPausados;

    [Header("Valentina durante o filminho")]
    [Tooltip("Marcador de onde a Valentina fica sentada, em cima da cama. Mova este objeto na cena para ajustar a pose.")]
    public Transform poseSentada;

    [Tooltip("Animator da Valentina. Vazio = pega o do movimentoDoPlayer.")]
    public Animator animatorDaValentina;

    [Tooltip("Nome do parâmetro Bool do Animator que liga a animação sentada.")]
    public string parametroSentada = "Sentada";

    [Tooltip("Ligado: a Valentina fica colada no marcador o filminho inteiro, então dá para arrastar o marcador na janela Scene com o jogo rodando e acertar a pose. Desligado: ela só é posicionada uma vez, no começo.")]
    public bool seguirPoseDuranteOFilminho = true;

    private int pontoAtual = 0;
    private float tempo = 0f;
    private bool executando = false;

    // Segurando a câmera no último ponto antes da gameplay começar
    private bool esperando = false;
    private float tempoEspera = 0f;

    // Guardado para religar no fim do filminho
    private CharacterController controladorDoPlayer;

    // Guarda como cada canvas estava antes do filminho,
    // para não ligar no fim algo que já estava desligado.
    private bool[] estadoOriginalCanvas;

    private void Start()
    {
        Debug.Log("=================================");
        Debug.Log("🎬 CAMERA RAIL: START");
        Debug.Log("=================================");

        Debug.Log(
            "Câmera configurada: " +
            (cameraCinematica != null
                ? cameraCinematica.name
                : "NENHUMA")
        );

        Debug.Log(
            "Quantidade de pontos: " +
            (pontos != null ? pontos.Length : 0)
        );

        if (pontos != null)
        {
            for (int i = 0; i < pontos.Length; i++)
            {
                if (pontos[i] != null)
                {
                    Debug.Log(
                        "Ponto " + i +
                        ": " + pontos[i].name +
                        " | Posição: " +
                        pontos[i].position
                    );
                }
                else
                {
                    Debug.LogError(
                        "❌ Ponto " + i +
                        " está vazio!"
                    );
                }
            }
        }

        IniciarTrilho();
    }

    // =========================================================
    // INICIAR
    // =========================================================

    public void IniciarTrilho()
    {
        Debug.Log("🎬 Tentando iniciar trilho...");

        if (cameraCinematica == null)
        {
            Debug.LogError(
                "❌ ERRO: CameraCinematica não foi configurada!"
            );

            return;
        }

        if (pontos == null || pontos.Length < 2)
        {
            Debug.LogError(
                "❌ ERRO: O trilho precisa de pelo menos 2 pontos!"
            );

            return;
        }

        pontoAtual = 0;
        tempo = 0f;
        executando = true;

        esperando = false;
        tempoEspera = 0f;

        Debug.Log("✅ Trilho iniciado!");

        Debug.Log(
            "📍 Câmera indo para: " +
            pontos[0].name
        );

        cameraCinematica.transform.position =
            pontos[0].position;

        cameraCinematica.transform.rotation =
            pontos[0].rotation;

        cameraCinematica.gameObject.SetActive(true);

        EntrarNoModoFilme();

        Debug.Log(
            "📷 Câmera cinematográfica ativada!"
        );

        Debug.Log(
            "➡️ Próximo ponto: " +
            pontos[1].name
        );
    }

    // =========================================================
    // MODO FILME — esconde a interface e congela o Player
    // =========================================================

    private void EntrarNoModoFilme()
    {
        // Guarda o estado de cada canvas e desliga todos
        if (canvasDaGameplay != null)
        {
            estadoOriginalCanvas = new bool[canvasDaGameplay.Length];

            for (int i = 0; i < canvasDaGameplay.Length; i++)
            {
                if (canvasDaGameplay[i] == null)
                    continue;

                estadoOriginalCanvas[i] =
                    canvasDaGameplay[i].activeSelf;

                canvasDaGameplay[i].SetActive(false);
            }

            Debug.Log(
                "🖥️ Canvas da gameplay desligados: " +
                canvasDaGameplay.Length
            );
        }

        if (scriptsPausados != null)
        {
            foreach (MonoBehaviour script in scriptsPausados)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }
        }

        SentarNaCama();

        if (movimentoDoPlayer != null)
        {
            movimentoDoPlayer.enabled = false;
        }

        // Desliga as câmeras do Player para só a cinemática aparecer
        if (cameraDoPlayer != null)
        {
            cameraDoPlayer.DesativarControle();
        }
    }

    // Põe a Valentina na pose e liga a animação sentada.
    private void SentarNaCama()
    {
        // O CharacterController fica desligado o filminho inteiro.
        // Com ele ligado a PhysX devolve o Player para o lugar de antes,
        // e é ele que impediria você de arrastar o marcador com o jogo rodando.
        if (movimentoDoPlayer != null)
        {
            controladorDoPlayer =
                movimentoDoPlayer.GetComponent<CharacterController>();

            if (controladorDoPlayer != null)
            {
                controladorDoPlayer.enabled = false;
            }
        }

        AcompanharPose();

        if (poseSentada != null)
        {
            Debug.Log(
                "🛏️ Valentina na pose: " +
                poseSentada.position +
                "  (arraste o '" + poseSentada.name +
                "' na cena, com o jogo rodando, para ajustar)"
            );
        }
        else
        {
            Debug.LogWarning(
                "⚠️ 'poseSentada' está vazio — a Valentina fica onde estiver."
            );
        }

        Animator anim = ObterAnimator();

        if (anim != null)
        {
            anim.SetBool(parametroSentada, true);

            Debug.Log("🪑 Animação sentada ligada.");
        }
        else
        {
            Debug.LogWarning(
                "⚠️ Nenhum Animator encontrado — a Valentina não vai sentar."
            );
        }
    }

    // Enquanto o filminho roda, a Valentina fica colada no marcador.
    // É isto que deixa você arrastar o marcador na janela Scene, com o jogo
    // rodando, e ver ela indo junto na hora.
    private void AcompanharPose()
    {
        if (!seguirPoseDuranteOFilminho)
            return;

        if (movimentoDoPlayer == null || poseSentada == null)
            return;

        movimentoDoPlayer.transform.SetPositionAndRotation(
            poseSentada.position,
            poseSentada.rotation
        );
    }

    private Animator ObterAnimator()
    {
        if (animatorDaValentina != null)
            return animatorDaValentina;

        if (movimentoDoPlayer != null)
            return movimentoDoPlayer.animator;

        return null;
    }

    // =========================================================
    // ESPERA NO ÚLTIMO PONTO
    // =========================================================

    // A câmera chegou no fim do trilho, mas o jogo ainda não começa:
    // ela fica parada no último ponto por 'esperaNoFim' segundos.
    private void ComecarEspera()
    {
        executando = false;

        Debug.Log("=================================");
        Debug.Log("🎬 TRILHO TERMINOU!");

        Debug.Log(
            "📍 Último ponto: " +
            pontos[pontos.Length - 1].name
        );

        if (esperaNoFim <= 0f)
        {
            TerminarTrilho();

            return;
        }

        esperando = true;
        tempoEspera = esperaNoFim;

        Debug.Log(
            "⏳ Segurando a câmera por " +
            esperaNoFim +
            "s antes de devolver o controle..."
        );
    }

    private void EsperarNoFim()
    {
        tempoEspera -= Time.deltaTime;

        if (tempoEspera > 0f)
            return;

        esperando = false;

        TerminarTrilho();
    }

    // =========================================================
    // FIM DO TRILHO — devolve o jogo para o jogador
    // =========================================================

    private void TerminarTrilho()
    {
        executando = false;

        // Desliga a câmera do filminho antes de ligar a do Player,
        // senão ficam duas câmeras e dois AudioListener ativos.
        if (cameraCinematica != null)
        {
            cameraCinematica.gameObject.SetActive(false);
        }

        // Volta a interface exatamente como estava
        if (canvasDaGameplay != null && estadoOriginalCanvas != null)
        {
            for (int i = 0; i < canvasDaGameplay.Length; i++)
            {
                if (canvasDaGameplay[i] == null)
                    continue;

                if (i < estadoOriginalCanvas.Length)
                {
                    canvasDaGameplay[i].SetActive(
                        estadoOriginalCanvas[i]
                    );
                }
            }
        }

        if (scriptsPausados != null)
        {
            foreach (MonoBehaviour script in scriptsPausados)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
        }

        // Devolve o CharacterController, que ficou desligado o filminho inteiro
        if (controladorDoPlayer != null)
        {
            controladorDoPlayer.enabled = true;
        }

        // Volta para as animações normais (idle / walk / run)
        Animator anim = ObterAnimator();

        if (anim != null)
        {
            anim.SetBool(parametroSentada, false);

            Debug.Log("🚶 Animações normais de volta.");
        }

        if (movimentoDoPlayer != null)
        {
            movimentoDoPlayer.enabled = true;
        }

        // Liga a câmera de primeira pessoa da gameplay
        if (cameraDoPlayer != null)
        {
            cameraDoPlayer.AtivarControle();

            Debug.Log("🎮 Primeira pessoa ativada — controle devolvido ao jogador.");
        }
        else
        {
            Debug.LogError(
                "❌ ERRO: cameraDoPlayer não foi configurada! " +
                "O jogo fica sem câmera no fim do trilho."
            );
        }

        Debug.Log("=================================");
    }

    // =========================================================
    // MOVIMENTO DA CÂMERA
    // =========================================================

    private void Update()
    {
        if (esperando)
        {
            AcompanharPose();

            EsperarNoFim();

            return;
        }

        if (!executando)
            return;

        AcompanharPose();

        MoverCamera();
    }

    private void MoverCamera()
    {
        if (pontoAtual >= pontos.Length - 1)
        {
            ComecarEspera();

            return;
        }

        tempo += Time.deltaTime;

        float progresso =
            tempo / duracaoPorTrecho;

        progresso = Mathf.Clamp01(progresso);

        Transform pontoA =
            pontos[pontoAtual];

        Transform pontoB =
            pontos[pontoAtual + 1];

        float movimentoSuave =
            Mathf.SmoothStep(
                0f,
                1f,
                progresso
            );

        cameraCinematica.transform.position =
            Vector3.Lerp(
                pontoA.position,
                pontoB.position,
                movimentoSuave
            );

        cameraCinematica.transform.rotation =
            Quaternion.Slerp(
                pontoA.rotation,
                pontoB.rotation,
                movimentoSuave
            );

        if (progresso >= 1f)
        {
            Debug.Log(
                "➡️ Chegou em: " +
                pontoB.name
            );

            pontoAtual++;
            tempo = 0f;

            if (pontoAtual < pontos.Length - 1)
            {
                Debug.Log(
                    "➡️ Indo para: " +
                    pontos[pontoAtual + 1].name
                );
            }
        }
    }
}
