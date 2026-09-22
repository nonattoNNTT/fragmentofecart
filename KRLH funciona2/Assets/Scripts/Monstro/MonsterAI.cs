using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// =========================================================
// MONSTER AI - o cerebro de um monstro
// =========================================================
//
// COMO USAR
//   1. Salve este arquivo em qualquer pasta dentro de Assets/
//   2. Arraste o script para cima do objeto do monstro no Inspector
//   3. Faca o bake da NavMesh na cena (menu Window > AI > Navigation)
//   4. Aperte Play
//
// O NavMeshAgent e adicionado sozinho. Ele acha o jogador sozinho e ja comeca
// a rondar. Nao precisa arrastar nada.
//
// Se voce esquecer o bake, ele avisa no Console em vez de ficar parado sem
// explicacao - esse e o erro que mais custa tempo com NavMesh.
//
//
// QUEM FAZ O QUE
//
// O NavMesh responde UMA pergunta: "como eu chego la".
// Este script responde as outras, que sao as que assustam:
//
//   Sense()  ->  o que eu percebo agora?
//   Think()  ->  o que isso significa, e para onde eu decido ir?
//   Act()    ->  entrega o destino ao NavMeshAgent
//
// Um monstro que so tem NavMesh corre reto pra cima de voce pra sempre. Isso
// cansa em dois minutos. O que faz medo e a decisao antes do caminho.
//
// Cinco ideias fazem ele parecer vivo:
//
//   1. Ele DESCONFIA antes de ter certeza. A deteccao nao e liga/desliga:
//      existe uma barra (awareness) que sobe conforme distancia, angulo e
//      luz. E dela que vem o momento "sera que ele me viu?".
//
//   2. Ele TEM MEMORIA. Quando perde voce de vista, vai ate o ultimo lugar
//      onde te viu e procura em volta, como alguem procurando de verdade.
//
//   3. Ele CORTA CAMINHO. Enquanto te ve correndo, ele nao mira em voce -
//      mira onde voce VAI ESTAR. Aqui o NavMesh paga sozinho o custo de ter
//      sido usado: da pra perguntar se aquele ponto adiantado e alcancavel
//      antes de ir.
//
//   4. Ele DESISTE de proposito. Depois de um tempo cacando, recua e te da
//      ar. Terror sem pausa vira ruido: o susto precisa do vale antes.
//
//   5. Ele ESPERA. E decide isso com o pathfinding na mao: se o caminho ate
//      onde voce sumiu for longo demais, correr atras e burrice - melhor
//      ficar calado numa porta e esperar voce aparecer.
//
// O campo "currentThought" no Inspector mostra em portugues o que ele esta
// pensando agora. Deixe o objeto selecionado durante o Play e voce le o
// raciocinio dele ao vivo.
//
//
// SOM
//
// Todo campo de som e opcional - deixe vazio o que voce ainda nao tem e ele
// roda calado, sem erro. As tres AudioSources sao criadas sozinhas.
//
// Ele toca som em seis situacoes (desconfiou, te viu, procurou, desistiu,
// pegou, resmungo aleatorio), mais passos contados por metro andado. Em cada
// lista ponha 3 ou 4 variacoes: ele sorteia, nunca repete o clipe anterior e
// ainda muda o tom.
//
// A regra que vale mais que todas: DURANTE A EMBOSCADA ELE FICA MUDO.
// Monstro que espera escondido fazendo barulho nao esta emboscando, esta
// avisando. Leia o comentario em IsSilent() la embaixo.
//
// =========================================================

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour
{
    public enum MonsterState
    {
        Patrol,      // rondando, sem saber de nada
        Investigate, // ouviu ou viu algo, foi conferir
        Hunt,        // tem certeza, esta indo atras
        Ambush,      // perdeu voce e resolveu esperar calado
        Retreat      // cansou de cacar, esta dando um tempo
    }

    [Header("Alvo")]
    public Transform player;              // deixe vazio que ele acha sozinho
    public float playerHeightOffset = 1.2f;

    [Header("Visao")]
    // Tres cones sobrepostos, como o xenomorfo de Alien: Isolation. O de
    // longe e estreito, o do canto do olho e largo mas curto, e colado nele
    // nao existe cone nenhum. Parede bloqueia os tres.
    public float viewDistance = 22f;
    public float viewAngle = 110f;        // angulo total do cone de frente, em graus
    public float peripheralDistance = 10f; // canto do olho: perto, mas quase tudo em volta
    public float peripheralAngle = 200f;
    public float peripheralWeight = 0.45f; // ver de canto desconfia mais devagar que ver de frente
    public float nearSenseRadius = 4f;    // tao perto que ele sente, mesmo por tras
    public float darkViewMultiplier = 0.65f; // com a lanterna dela apagada ele enxerga menos
    public LayerMask obstacleMask;        // o que bloqueia a visao; vazio = tudo menos o jogador

    [Header("Audicao")]
    public float hearingRadius = 14f;
    public float runningSpeed = 6.5f;     // acima disso o jogador faz barulho alto (sprint do Player = 8)
    public float sneakingSpeed = 3f;      // abaixo disso e quase silencio (andar do Player = 5)
    public float hearingError = 3f;       // som entrega a regiao, nunca o ponto exato
    public float hearingUpdateInterval = 0.5f; // o ouvido atualiza a pista de tempos em tempos, nao todo quadro

    [Header("Faro que aperta")]
    // O truque do Mr. X (Resident Evil 2): quanto mais tempo sem achar ela,
    // mais afiado ele fica. E o que impede o empate - ficar parado num canto
    // funciona por um tempo, nunca para sempre.
    public float frustrationTime = 40f;      // tempo sem nenhum sinal ate o faro estar no maximo
    public float frustrationSenseBoost = 0.6f; // +60% de alcance de visao e ouvido no maximo

    [Header("Consciencia")]
    public float awarenessGain = 1.4f;    // por segundo, com o jogador bem visivel
    public float awarenessDecay = 0.25f;  // por segundo, sem nenhum sinal
    public float hearingWeight = 0.35f;   // ouvir sobe bem mais devagar que ver

    [Header("Lanterna - opcional")]
    public Transform flashlight;
    public float flashlightAngle = 45f;
    public float flashlightWeight = 1.8f; // ser iluminado entrega o jogador mais rapido

    [Header("Movimento - o NavMeshAgent recebe estes valores")]
    // Regra de ouro: mais rapido que o Player andando (5), mais lento que
    // correndo (8). Andar nao salva; correr salva, mas a stamina acaba.
    public float walkSpeed = 2.6f;
    public float huntSpeed = 6.3f;
    public float searchSpeed = 3.8f;
    public float angularSpeed = 220f;     // graus por segundo ao virar
    public float acceleration = 8f;
    public float turnSpeedWhenStopped = 6f; // giro manual parado, na emboscada

    [Header("Comportamento")]
    public float patrolRadius = 32f;      // raio MAXIMO do proximo ponto de ronda
    public float patrolMinRadius = 12f;   // raio minimo: obriga a cobrir terreno
    public int patrolMemory = 10;         // quantos pontos recentes ele evita repetir
    public float instinctChance = 0.3f;   // chance de a ronda puxar para o lado do jogador
    public float patrolWaitTime = 2.5f;
    public float arriveTolerance = 1.2f;
    public float investigateTime = 12f;   // conta a partir da CHEGADA, nao da decisao
    public float searchInterval = 2f;
    public float searchRadius = 9f;
    public float loseSightTime = 4f;      // sem ver por tanto tempo, a caca vira busca
    public float lostPredictTime = 1.5f;  // quantos segundos ele extrapola o rumo de quem sumiu
    public float catchDistance = 1.8f;

    [Header("Memoria - o mapa de onde ela pode estar")]
    // Ele guarda uma grade de "chance de ela estar aqui", difunde com o tempo
    // e zera o que olha. Ver OccupancyMap.cs. Ligue o gizmo e selecione o
    // Monstro no Play para ver a cabeca dele funcionando na Scene.
    public bool memoryEnabled = true;
    public float memoryCellSize = 4f;        // tamanho da celula, em metros
    public float memoryUpdateInterval = 0.2f;
    public float memoryDiffusion = 0.45f;    // o quanto a certeza escorre para as vizinhas a cada passo
    public float memoryDrift = 1.4f;         // o quanto a duvida segue o rumo em que ela sumiu
    public float memoryDistancePenalty = 0.004f; // desconto por metro ao escolher onde olhar
    public float memoryGiveUpConfidence = 0.08f; // abaixo disso ele aceita que perdeu
    public float searchMinTime = 10f;        // antes disso ele nao aceita ter perdido
    public float searchGiveUpTime = 45f;     // depois disso a pista esfriou de vez
    public float searchSpreadSpeed = 3.5f;   // m/s que ele supoe que ela andou, ao reabrir a busca
    public bool showMemoryGizmo = true;

    [Header("Olhar em volta - quando parado")]
    public float scanAngle = 75f;         // graus para cada lado
    public float scanSpeed = 1.1f;        // velocidade do vai-e-vem (rad/s)

    [Header("Cortar caminho")]
    // Enquanto ele te ve correndo, ele mira adiante de voce em vez de mirar em
    // voce. E o que transforma "correr atras" em "cortar o caminho".
    public bool interceptEnabled = true;
    public float interceptLookAhead = 1.1f;  // segundos de antecipacao
    public float interceptMinSpeed = 4f;     // so vale a pena se voce estiver andando rapido

    [Header("Ritmo do medo")]
    // "Menace" e a pressao que ELE ja colocou em VOCE - sobe rapido quando
    // esta perto e te vendo, devagar quando esta longe. Recuar por pressao
    // acumulada, e nao por cronometro, e o que faz a perseguicao curta e
    // colada valer tanto quanto a longa e distante. Ideia do medidor de
    // ameaca de Alien: Isolation.
    public float menaceLimit = 20f;       // pressao acumulada que dispara o recuo
    public float menaceNearBonus = 2f;    // quanto a proximidade multiplica a pressao
    public float menaceDecay = 1.2f;      // por segundo, longe dela
    public float maxHuntTime = 25f;       // teto duro: cacar mais que isso cansa o jogador
    public float retreatTime = 8f;
    public float ambushTime = 12f;
    public float ambushChance = 0.45f;    // chance base de esperar ao inves de procurar
    public float ambushPathThreshold = 18f; // caminho maior que isso favorece emboscar
    public float ambushSpotRadius = 12f;    // ate que distancia da rota dela ele procura uma quina
    public float ambushSpotMaxDetour = 30f; // caminho maximo da quina ate a rota (dar a volta na parede)
    public bool directorEnabled = true;
    public float boredomTime = 35f;       // tempo perdido antes de receber uma dica
    public float rumorError = 9f;         // o quanto a dica e imprecisa, em metros

    [Header("Pontos da fase - opcionais")]
    public Transform[] patrolPoints;      // vazio = ele sorteia pontos na propria NavMesh
    public Transform[] ambushPoints;      // portas e corredores onde vale esperar; vazio = ele acha uma quina sozinho

    // ---------------------------------------------------------
    // SOM
    // ---------------------------------------------------------
    // Todo campo daqui pra baixo e opcional. Deixe vazio o que voce ainda nao
    // tem: o monstro funciona calado e nao da erro.
    //
    // Cada campo e uma LISTA de proposito. Coloque 3 ou 4 variacoes do mesmo
    // som em cada uma: ele nunca repete o mesmo clipe duas vezes seguidas e
    // ainda sorteia o tom. Um passo identico repetindo 200 vezes e a coisa que
    // mais denuncia que aquilo e um jogo.

    [Header("Som - passos")]
    public AudioClip[] footstepClips;
    public float stepDistance = 1.1f;       // metros andados por passo
    public float huntStepMultiplier = 0.8f; // correndo o passo e mais curto
    public float footstepVolume = 0.8f;

    [Header("Som - voz por situacao")]
    public AudioClip[] patrolSounds;      // resmungo calmo, rondando
    public AudioClip[] alertSounds;       // desconfiou de alguma coisa
    public AudioClip[] spotSounds;        // te viu - o rugido do comeco da cacada
    public AudioClip[] searchSounds;      // procurando e nao achando
    public AudioClip[] giveUpSounds;      // desistindo, se afastando
    public AudioClip[] catchSounds;       // pegou voce
    public float voiceVolume = 1f;

    [Header("Som - respiracao continua")]
    public AudioClip patrolBreathing;     // loop calmo
    public AudioClip huntBreathing;       // loop ofegante
    public float breathingVolume = 0.5f;
    public float breathingFadeSpeed = 2f;

    [Header("Som - resmungo aleatorio")]
    // De vez em quando ele geme sem motivo. E isso que faz o jogador saber que
    // ele existe e nao saber onde ele esta.
    public float randomSoundMinDelay = 9f;
    public float randomSoundMaxDelay = 22f;

    [Header("Som - espacializacao 3D")]
    public float soundMinDistance = 2f;   // dentro disso toca em volume cheio
    public float soundMaxDistance = 28f;  // fora disso nao se ouve mais
    public bool silentDuringAmbush = true; // emboscada calada; leia o comentario abaixo

    [Header("O que ele esta pensando - so leitura")]
    public string currentThought = "...";
    public float menace;                  // pressao que ele ja colocou no jogador
    public float frustration;             // 0 a 1; em 1 os sentidos estao no maximo
    public float memoryConfidence;        // o quanto ele ainda acredita saber onde ela esta
    public string stateHistory = "";      // ultimas trocas de estado, com o tempo (mais recente primeiro)
    public MonsterState state = MonsterState.Patrol;
    public float awareness;
    public bool canSee;
    public bool canHear;

    // Percepcao interna
    private Vector3 lastKnownPosition;
    private Vector3 lastSeenVelocity;     // para onde ela ia quando sumiu
    private Vector3 lostSightPosition;    // onde EU estava quando ela sumiu
    private float timeSinceSeen = 999f;
    private float hearingTimer;
    private float playerSpeed;
    private Vector3 playerVelocity;
    private Vector3 previousPlayerPosition;
    private bool hasPreviousPosition;

    // Decisao interna
    private float stateTimer;
    private float huntFatigue;
    private float boredomTimer;
    private float waitTimer;
    private float searchTimer;
    private int patrolIndex = -1;
    private Vector3 currentTarget;
    private Transform chosenAmbush;
    private Vector3 ambushSpot;           // quina escolhida sozinho, quando nao ha ambushPoints
    private bool hasAmbushSpot;
    private bool investigateArrived;      // ja chegou no ponto e esta procurando em volta?
    private float travelBudget;           // tempo maximo para chegar antes de desistir
    private readonly List<Vector3> searchPlan = new List<Vector3>(); // lugares para checar, do mais provavel ao menos
    private readonly List<Vector3> recentPatrol = new List<Vector3>();
    private float destinationTimer;
    private float stuckTimer;
    private float memoryTimer;
    private bool sawInFocus;              // viu de frente ou so pelo canto do olho?
    private readonly OccupancyMap memory = new OccupancyMap();
    private bool scanning;
    private float scanBaseYaw;
    private float scanTimer;

    // Navegacao interna
    private NavMeshAgent agent;
    private NavMeshPath scratchPath;      // reaproveitado, para nao alocar todo quadro
    private bool navMeshWarned;

    // Som interno
    private AudioSource voiceSource;      // rugidos e gritos
    private AudioSource loopSource;       // respiracao continua
    private AudioSource stepSource;       // passos
    private float distanceWalked;
    private float randomSoundTimer;
    private int lastClipIndex = -1;

    // =========================================================
    // PREPARACAO
    // =========================================================

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        scratchPath = new NavMeshPath();

        agent.speed = walkSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = 0f;
        agent.autoBraking = true;

        currentTarget = transform.position;

        BuildMemory();
        SetupAudio();
        ScheduleNextRandomSound();

        if (player == null)
        {
            player = FindPlayer();
        }

        // Mascara vazia no Inspector: assume tudo, menos a camada do jogador
        if (obstacleMask == 0)
        {
            obstacleMask = player != null ? ~(1 << player.gameObject.layer) : ~0;
        }

        if (player == null)
        {
            currentThought = "Nao achei o jogador. Arraste ele no campo 'player'.";
            Debug.LogWarning("MonsterAI: nenhum jogador encontrado. Preencha o campo 'player'.", this);
            return;
        }

        lastKnownPosition = player.position;
        PickPatrolTarget();
    }

    // O jogador quase sempre e a raiz de quem carrega a camera principal.
    // Procurar pela tag "Player" e o plano B, porque projeto sem essa tag
    // criada faz o FindWithTag lancar excecao.
    private Transform FindPlayer()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform.root;
        }

        try
        {
            GameObject found = GameObject.FindWithTag("Player");

            if (found != null)
            {
                return found.transform;
            }
        }
        catch (UnityException)
        {
            // A tag "Player" nao existe neste projeto. Tudo bem, segue sem ela.
        }

        return null;
    }

    private void Update()
    {
        if (player == null || agent == null)
        {
            return;
        }

        // O erro numero um com NavMesh: esquecer o bake. Sem este aviso o
        // monstro fica parado sem dizer por que, e voce perde a tarde nisso.
        if (!agent.isOnNavMesh)
        {
            WarnMissingNavMesh();
            return;
        }

        stateTimer += Time.deltaTime;
        timeSinceSeen += Time.deltaTime;
        hearingTimer += Time.deltaTime;
        destinationTimer += Time.deltaTime;

        Sense();
        UpdateMemory();
        Think();
        Act();
        UpdateAudio();
    }

    private void WarnMissingNavMesh()
    {
        if (navMeshWarned)
        {
            return;
        }

        navMeshWarned = true;
        currentThought = "Estou fora da NavMesh. Falta o bake, ou eu nasci fora dela.";

        Debug.LogWarning(
            "MonsterAI: o agente nao esta sobre uma NavMesh. Faca o bake em " +
            "Window > AI > Navigation e confira se o monstro nasce em cima do chao.", this);
    }

    // =========================================================
    // SENSE - o que eu percebo agora
    // =========================================================

    private void Sense()
    {
        MeasurePlayerSpeed();

        bool couldSee = canSee;
        canSee = CanSeePlayer();

        if (canSee)
        {
            lastKnownPosition = player.position;
            lastSeenVelocity = playerVelocity;
            timeSinceSeen = 0f;

            // Vi: toda a certeza vai para onde ela esta, e o faro relaxa
            memory.SetKnown(player.position);
            frustration = 0f;
        }
        else if (couldSee)
        {
            // Acabou de perder de vista: guarda de onde eu olhava. E daqui que
            // a busca decide o que esta "atras da quina".
            lostSightPosition = transform.position;
        }

        canHear = CanHearPlayer();

        // Som so marca a regiao, e a visao sempre ganha do ouvido.
        // Atualiza de tempos em tempos: todo quadro faria o destino tremer.
        if (canHear && !canSee && hearingTimer >= hearingUpdateInterval)
        {
            hearingTimer = 0f;
            Vector2 error = Random.insideUnitCircle * hearingError;
            Vector3 heard = player.position + new Vector3(error.x, 0f, error.y);

            if (TrySnapToNavMesh(heard, hearingError, out Vector3 valid))
            {
                lastKnownPosition = valid;
                lastSeenVelocity = Vector3.zero;

                // Som nao entrega o ponto: espalha a certeza pela regiao
                memory.AddNoise(valid, hearingError * 1.5f, 0.6f);
            }
        }

        // Sem nenhum sinal, o faro vai apertando (ver frustrationTime)
        if (!canSee && !canHear)
        {
            frustration = Mathf.Clamp01(frustration + Time.deltaTime / Mathf.Max(1f, frustrationTime));
        }

        UpdateAwareness();
    }

    private void MeasurePlayerSpeed()
    {
        if (!hasPreviousPosition)
        {
            previousPlayerPosition = player.position;
            hasPreviousPosition = true;
            return;
        }

        float delta = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 rawVelocity = (player.position - previousPlayerPosition) / delta;
        rawVelocity.y = 0f;

        previousPlayerPosition = player.position;

        // Suaviza, senao o valor pula a cada quadro e a interceptacao fica louca
        playerVelocity = Vector3.Lerp(playerVelocity, rawVelocity, 10f * Time.deltaTime);
        playerSpeed = playerVelocity.magnitude;
    }

    private bool CanSeePlayer()
    {
        sawInFocus = false;

        Vector3 target = player.position + Vector3.up * playerHeightOffset;
        Vector3 origin = transform.position + Vector3.up * playerHeightOffset;

        float distance = Vector3.Distance(origin, target);

        if (distance > EffectiveViewDistance())
        {
            return false;
        }

        Vector3 direction = (target - origin).normalized;
        float angle = Vector3.Angle(transform.forward, direction);

        bool inFocus = angle <= viewAngle * 0.5f;
        bool inPeripheral = distance <= peripheralDistance && angle <= peripheralAngle * 0.5f;
        bool veryClose = distance <= nearSenseRadius;

        if (!inFocus && !inPeripheral && !veryClose)
        {
            return false;
        }

        // Tem parede no meio?
        if (Physics.Linecast(origin, target, obstacleMask))
        {
            return false;
        }

        sawInFocus = inFocus || veryClose;
        return true;
    }

    // Alcance da visao depois do escuro e do faro. A lanterna dela apagada
    // esconde de verdade; ficar sumido tempo demais deixa de esconder.
    private float EffectiveViewDistance()
    {
        float reach = viewDistance * (1f + frustration * frustrationSenseBoost);

        if (flashlight != null && !flashlight.gameObject.activeInHierarchy)
        {
            reach *= Mathf.Clamp(darkViewMultiplier, 0.1f, 1f);
        }

        return reach;
    }

    private float EffectiveHearingRadius()
    {
        return hearingRadius * (1f + frustration * frustrationSenseBoost);
    }

    // A audicao atravessa parede de proposito: e som. E justo porque so
    // entrega a regiao, nunca a posicao exata.
    private bool CanHearPlayer()
    {
        float noiseRadius;

        float reach = EffectiveHearingRadius();

        if (playerSpeed >= runningSpeed)
        {
            noiseRadius = reach;
        }
        else if (playerSpeed >= sneakingSpeed)
        {
            noiseRadius = reach * 0.5f;
        }
        else
        {
            noiseRadius = reach * 0.15f;
        }

        return Vector3.Distance(transform.position, player.position) <= noiseRadius;
    }

    private void UpdateAwareness()
    {
        float gain = 0f;

        if (canSee)
        {
            // Perto e no centro do cone sobe mais rapido que longe e na borda
            float distance = Vector3.Distance(transform.position, player.position);
            gain = awarenessGain * (0.35f + (1f - Mathf.Clamp01(distance / viewDistance)));

            // Visto so pelo canto do olho: desconfia, mas demora a ter certeza
            if (!sawInFocus)
            {
                gain *= Mathf.Clamp01(peripheralWeight);
            }

            if (IsLitByFlashlight())
            {
                gain *= flashlightWeight;
            }
        }
        else if (canHear)
        {
            gain = awarenessGain * hearingWeight;
        }

        awareness += (gain > 0f ? gain : -awarenessDecay) * Time.deltaTime;
        awareness = Mathf.Clamp01(awareness);
    }

    // Apontar a lanterna para o monstro e uma escolha do jogador:
    // enxergar custa ser enxergado.
    private bool IsLitByFlashlight()
    {
        if (flashlight == null || !flashlight.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 toMonster = (transform.position - flashlight.position).normalized;

        return Vector3.Angle(flashlight.forward, toMonster) <= flashlightAngle * 0.5f;
    }

    // =========================================================
    // MEMORIA - o mapa de onde ela pode estar
    // =========================================================

    // Monta a grade uma vez, em cima da NavMesh que ja existe na cena.
    // Sem bake nao ha grade: ele volta a se virar com o metodo antigo em
    // vez de quebrar.
    private void BuildMemory()
    {
        if (!memoryEnabled)
        {
            return;
        }

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        if (triangulation.vertices == null || triangulation.vertices.Length == 0)
        {
            Debug.LogWarning("MonsterAI: sem NavMesh bakeada, o mapa de memoria nao foi montado. " +
                             "Selecione o objeto NavMesh na cena e clique em Bake.", this);
            return;
        }

        Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);

        for (int i = 1; i < triangulation.vertices.Length; i++)
        {
            bounds.Encapsulate(triangulation.vertices[i]);
        }

        bounds.Expand(memoryCellSize * 2f);
        memory.Build(bounds, memoryCellSize, memoryCellSize * 0.75f);
    }

    // Difunde a certeza e apaga o que ele esta vendo agora. Roda algumas
    // vezes por segundo, nao todo quadro: e barato e ninguem percebe.
    private void UpdateMemory()
    {
        if (!memory.IsBuilt)
        {
            return;
        }

        memoryTimer += Time.deltaTime;

        if (memoryTimer < memoryUpdateInterval)
        {
            return;
        }

        memoryTimer = 0f;

        // A duvida escorre no rumo em que ela sumiu, nao em circulo
        memory.Diffuse(memoryDiffusion, lastSeenVelocity, memoryDrift);

        memory.ClearVisible(
            transform.position + Vector3.up * playerHeightOffset,
            transform.forward,
            EffectiveViewDistance(),
            viewAngle * 0.5f,
            playerHeightOffset,
            obstacleMask,
            nearSenseRadius);

        memoryConfidence = memory.Confidence;
    }

    // =========================================================
    // THINK - o que isso significa, e para onde eu vou
    // =========================================================

    private void Think()
    {
        RunDirector();

        // Longe e sem cacar, a pressao acumulada esfria sozinha
        if (state != MonsterState.Hunt)
        {
            menace = Mathf.Max(0f, menace - menaceDecay * Time.deltaTime);
        }

        // Recuo e sagrado: ele nao volta a cacar antes da hora.
        // E o respiro do jogador, e nada interrompe.
        if (state == MonsterState.Retreat)
        {
            if (stateTimer >= retreatTime)
            {
                huntFatigue = 0f;
                menace = 0f;
                awareness = 0f;
                EnterState(MonsterState.Patrol);
            }

            return;
        }

        // Certeza total: caca, venha de onde vier
        if (awareness >= 1f && state != MonsterState.Hunt)
        {
            EnterState(MonsterState.Hunt);
        }

        switch (state)
        {
            case MonsterState.Patrol:

                if (awareness > 0.3f)
                {
                    EnterState(MonsterState.Investigate);
                }

                break;

            case MonsterState.Investigate:

                // Ouviu de novo, em outro lugar: troca o alvo em vez de terminar
                // a busca velha num lugar que ja nao interessa.
                if (canHear && !canSee && Vector3.Distance(lastKnownPosition, currentTarget) > 3f)
                {
                    StartInvestigateTravel();
                }

                // Antes de chegar, o limite e o tempo de viagem; depois de
                // chegar, e o tempo de procura. Contar tudo junto fazia ele
                // desistir no meio do corredor.
                // Varreu tudo que achava possivel e nao tinha ninguem.
                // Enquanto a pista for recente, isso nao quer dizer que ela
                // sumiu - quer dizer que ela foi mais longe do que ele achava.
                if (investigateArrived && memory.IsBuilt && memory.Confidence < memoryGiveUpConfidence)
                {
                    if (timeSinceSeen < searchGiveUpTime)
                    {
                        ReseedSearch();
                    }
                    else if (stateTimer >= searchMinTime)
                    {
                        currentThought = "Procurei em tudo. Ela me perdeu.";
                        EnterState(MonsterState.Patrol);
                    }
                }
                else if (!investigateArrived && stateTimer >= travelBudget)
                {
                    currentThought = "Longe demais. Deixa pra la.";
                    EnterState(MonsterState.Patrol);
                }
                else if (investigateArrived && stateTimer >= investigateTime)
                {
                    // Com o mapa, quem encerra a busca e a pista esfriar, nao o
                    // cronometro: enquanto ele ainda tem em que acreditar, procura.
                    if (!memory.IsBuilt || timeSinceSeen >= searchGiveUpTime)
                    {
                        EnterState(MonsterState.Patrol);
                    }
                }

                break;

            case MonsterState.Hunt:

                huntFatigue += Time.deltaTime;

                // Pressao: colado e vendo pesa muito mais que longe e no escuro
                float nearness = 1f - Mathf.Clamp01(
                    Vector3.Distance(transform.position, player.position) / Mathf.Max(1f, viewDistance));
                menace += Time.deltaTime * (1f + nearness * menaceNearBonus) * (canSee ? 1f : 0.4f);

                // Ja assustou o bastante, ou cacou tempo demais: da o respiro.
                // Terror sem pausa vira ruido - o susto precisa do vale antes.
                if (menace >= menaceLimit || huntFatigue >= maxHuntTime)
                {
                    EnterState(MonsterState.Retreat);
                    break;
                }

                // Sem ver e sem ouvir por um tempo, a caca vira busca. Enquanto
                // ouve, continua indo atras do som - e o que impede o vai-e-vem
                // Hunt/Investigate a cada quadro quando a certeza veio do ouvido.
                if (!canSee && !canHear && timeSinceSeen > loseSightTime)
                {
                    EnterState(ShouldAmbush() ? MonsterState.Ambush : MonsterState.Investigate);
                }

                break;

            case MonsterState.Ambush:

                if (stateTimer >= ambushTime)
                {
                    EnterState(MonsterState.Patrol);
                }

                break;
        }
    }

    // Aqui o NavMesh deixa de ser so locomocao e vira informacao para decidir.
    //
    // Se o caminho ANDANDO ate onde voce sumiu for longo - voce deu a volta no
    // corredor, subiu por outro lado - correr atras e burrice: quando ele
    // chegar voce ja saiu. Nesse caso emboscar e a jogada certa, e ele passa a
    // preferir esperar.
    //
    // Repare que a distancia em linha reta nao serviria: voce pode estar a 3 m
    // dele com uma parede no meio e 20 m de caminho real.
    private bool ShouldAmbush()
    {
        hasAmbushSpot = false;

        if (FindAmbushPoint() == null)
        {
            // Sem pontos montados na cena: procura uma quina sozinho
            if (!TryFindAmbushSpot(out ambushSpot))
            {
                stateHistory = "(sem quina para emboscar)  |  " + stateHistory;
                return false;
            }

            hasAmbushSpot = true;
        }

        float chance = ambushChance;
        float pathLength = PathLengthTo(lastKnownPosition);

        // Caminho longo demais: ficar esperando passa a ser a melhor ideia
        if (pathLength > ambushPathThreshold)
        {
            chance = Mathf.Clamp01(chance + 0.35f);
        }

        return Random.value < chance;
    }

    // O truque que o Alien: Isolation usa. Se o monstro passou tempo demais
    // sem nenhum sinal, ele recebe a REGIAO onde o jogador esta - nunca o
    // ponto exato. Sem isso, quem se esconde bem nunca mais e ameacado e a
    // fase morre de tedio.
    private void RunDirector()
    {
        if (!directorEnabled)
        {
            return;
        }

        if (awareness > 0.3f || state == MonsterState.Hunt || state == MonsterState.Retreat)
        {
            boredomTimer = 0f;
            return;
        }

        boredomTimer += Time.deltaTime;

        if (boredomTimer < boredomTime)
        {
            return;
        }

        boredomTimer = 0f;

        Vector2 error = Random.insideUnitCircle.normalized * rumorError;
        Vector3 rumor = player.position + new Vector3(error.x, 0f, error.y);

        // A dica so vale se cair num lugar onde da pra andar
        if (!TrySnapToNavMesh(rumor, rumorError, out Vector3 valid))
        {
            return;
        }

        // A dica entra como REGIAO no mapa, nunca como ponto: ele passa a
        // acreditar que ela anda por ali e vai varrer aquilo. Se a dica for
        // errada, o proprio mapa desmente ele quando ele chega e nao acha.
        if (memory.IsBuilt)
        {
            memory.AddNoise(valid, rumorError, 0.5f);
        }

        lastKnownPosition = valid;
        EnterState(MonsterState.Investigate);
    }

    private void EnterState(MonsterState next)
    {
        stateHistory = Time.time.ToString("0.0") + "s " + next + "  |  " + stateHistory;
        if (stateHistory.Length > 160) stateHistory = stateHistory.Substring(0, 160);

        state = next;
        stateTimer = 0f;

        switch (next)
        {
            case MonsterState.Patrol:
                currentThought = "Nada por aqui. Vou continuar rondando.";
                agent.speed = walkSpeed;
                PickPatrolTarget();
                break;

            case MonsterState.Investigate:
                currentThought = "Ouvi alguma coisa ali. Vou conferir.";
                agent.speed = searchSpeed;
                StartInvestigateTravel();
                PlayVoice(alertSounds);
                break;

            case MonsterState.Hunt:
                currentThought = "Achei ela. Vou atras.";
                agent.speed = huntSpeed;
                PlayVoice(spotSounds);

                // Certeza que veio do ouvido: a pista e fresca agora, nao
                // "desde a ultima vez que vi" (que pode ter sido nunca).
                if (!canSee)
                {
                    timeSinceSeen = 0f;
                }

                break;

            case MonsterState.Ambush:
                chosenAmbush = FindAmbushPoint();
                currentThought = "Ela sumiu. Vou ficar quieto ali e esperar ela passar.";
                agent.speed = huntSpeed;

                if (chosenAmbush != null)
                {
                    SetDestination(chosenAmbush.position);
                }
                else if (hasAmbushSpot)
                {
                    SetDestination(ambushSpot);
                }

                break;

            case MonsterState.Retreat:
                currentThought = "Cansei de procurar. Vou dar um tempo.";
                agent.speed = walkSpeed;
                SetDestination(FarthestPoint());
                PlayVoice(giveUpSounds);
                break;
        }
    }

    // =========================================================
    // ACT - entrega o destino ao NavMeshAgent
    // =========================================================

    private void Act()
    {
        switch (state)
        {
            case MonsterState.Patrol:
                ActPatrol();
                break;

            case MonsterState.Investigate:
                ActInvestigate();
                break;

            case MonsterState.Hunt:
                ActHunt();
                break;

            case MonsterState.Ambush:
                ActAmbush();
                break;

            case MonsterState.Retreat:
                Resume();
                break;
        }
    }

    private void ActPatrol()
    {
        if (!Arrived() && !IsStuck())
        {
            Resume();
            return;
        }

        Stop();
        ScanAround();
        waitTimer += Time.deltaTime;

        if (waitTimer >= patrolWaitTime)
        {
            waitTimer = 0f;
            PickPatrolTarget();
        }
    }

    // Chegar no ultimo ponto conhecido e nao achar nada nao encerra a busca:
    // ele passa a checar pontos em volta, como alguem procurando de verdade.
    private void ActInvestigate()
    {
        if (!Arrived() && !IsStuck())
        {
            Resume();
            return;
        }

        if (!investigateArrived)
        {
            // Primeira chegada: a partir daqui conta o tempo de procura, e
            // monta a lista de lugares do mais provavel ao menos provavel.
            investigateArrived = true;
            stateTimer = 0f;
            searchTimer = 0f;
            BuildSearchPlan();
        }

        Stop();
        ScanAround();
        searchTimer += Time.deltaTime;

        if (searchTimer < searchInterval)
        {
            return;
        }

        searchTimer = 0f;
        currentThought = "Nao esta aqui. Vou olhar mais adiante.";
        PlayVoice(searchSounds);

        // Primeiro os lugares para onde ela provavelmente foi (rumo dela,
        // atras da quina). So quando esgota e que sorteia em volta.
        if (searchPlan.Count > 0)
        {
            Vector3 next = searchPlan[0];
            searchPlan.RemoveAt(0);
            SetDestination(next);
        }
        else if (memory.IsBuilt && memory.Confidence > memoryGiveUpConfidence)
        {
            // Esgotou a lista: pergunta ao mapa de novo, que ja mudou
            BuildSearchPlan();

            if (searchPlan.Count > 0)
            {
                Vector3 next = searchPlan[0];
                searchPlan.RemoveAt(0);
                SetDestination(next);
            }
        }
        else if (TryRandomNavPoint(lastKnownPosition, searchRadius, out Vector3 spot))
        {
            SetDestination(spot);
        }
    }

    private void ActHunt()
    {
        Resume();

        // Vendo: mira adiante dela. Sem ver: continua no rumo em que ela
        // sumiu, por alguns segundos, em vez de correr ate um ponto vazio.
        Vector3 target = canSee ? InterceptPoint() : PredictedLostPosition();

        if (destinationTimer >= 0.15f || Vector3.Distance(target, currentTarget) > 1.5f)
        {
            destinationTimer = 0f;
            SetDestination(target);
        }

        if (canSee && Vector3.Distance(transform.position, player.position) <= catchDistance)
        {
            Stop();
            OnCatchPlayer();
            return;
        }

        currentThought = canSee
            ? (IsIntercepting() ? "Ela vai passar ali. Vou cortar caminho." : "Estou vendo ela. Nao vou parar.")
            : (canHear ? "Estou ouvindo ela. Ta perto." : "Ela virou ali. Ainda da pra alcancar.");
    }

    // Mirar onde o jogador VAI ESTAR, nao onde ele esta.
    // So vale se ele estiver correndo e se o ponto adiantado for alcancavel -
    // sem essa checagem o monstro miraria dentro de uma parede e travaria.
    private Vector3 InterceptPoint()
    {
        if (!IsIntercepting())
        {
            return player.position;
        }

        Vector3 predicted = player.position + playerVelocity * interceptLookAhead;

        if (TrySnapToNavMesh(predicted, 2f, out Vector3 valid))
        {
            return valid;
        }

        return player.position;
    }

    private bool IsIntercepting()
    {
        return interceptEnabled && playerSpeed >= interceptMinSpeed;
    }

    // Parado, calado, virado para onde ela provavelmente vai aparecer
    private void ActAmbush()
    {
        if (chosenAmbush == null && !hasAmbushSpot)
        {
            EnterState(MonsterState.Patrol);
            return;
        }

        if (!Arrived() && !IsStuck())
        {
            Resume();
            return;
        }

        Stop();
        LookAt(lastKnownPosition);
    }

    // Chamado quando o monstro encosta no jogador.
    // Deixado assim de proposito: o que acontece ao ser pego e game design,
    // nao e decisao do cerebro do monstro. Ligue aqui a tela de morte.
    private void OnCatchPlayer()
    {
        currentThought = "Peguei ela.";
        PlayVoice(catchSounds);
        Debug.Log("MonsterAI: pegou o jogador.");
    }

    // =========================================================
    // NAVEGACAO
    // =========================================================

    private void SetDestination(Vector3 target)
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        // Destino fora da NavMesh: puxa para o ponto andavel mais proximo.
        // Sem isso o agente aceita o destino, calcula um caminho parcial e
        // fica encostado na parede mais perto, parecendo travado.
        if (TrySnapToNavMesh(target, 3f, out Vector3 valid))
        {
            currentTarget = valid;
            stuckTimer = 0f;
            agent.SetDestination(valid);
        }
    }

    // Da para chegar la ANDANDO? Um ponto num bolsao fechado por parede passa
    // no SamplePosition e mesmo assim nao tem caminho: o agente iria ate a
    // parede mais perto e ficaria la, parecendo burro.
    private bool IsReachable(Vector3 target, out float pathLength)
    {
        pathLength = PathLengthTo(target);
        return !float.IsPositiveInfinity(pathLength);
    }

    // Andando ha um tempo sem sair do lugar: caminho parcial, quina apertada,
    // outro agente no meio. Melhor escolher outro destino do que insistir.
    private bool IsStuck()
    {
        if (agent.isStopped || agent.pathPending)
        {
            stuckTimer = 0f;
            return false;
        }

        if (agent.velocity.sqrMagnitude < 0.04f && agent.remainingDistance > arriveTolerance)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }

        return stuckTimer >= 2f;
    }

    private void Resume()
    {
        if (agent.isStopped)
        {
            agent.isStopped = false;
        }

        agent.updateRotation = true;
        scanning = false;
    }

    // Parado, ele vira a cabeca de um lado para o outro. Sem isso o cone de
    // visao fica cravado numa direcao e da para passar por tras dele.
    private void ScanAround()
    {
        if (!scanning)
        {
            scanning = true;
            scanBaseYaw = transform.eulerAngles.y;
            scanTimer = 0f;
        }

        scanTimer += Time.deltaTime;
        agent.updateRotation = false;

        float yaw = scanBaseYaw + Mathf.Sin(scanTimer * scanSpeed) * scanAngle;
        Quaternion wanted = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, wanted, 4f * Time.deltaTime);
    }

    private void Stop()
    {
        if (!agent.isStopped)
        {
            agent.isStopped = true;
        }
    }

    // Chegou no destino? Espera o caminho terminar de ser calculado antes de
    // responder, senao remainingDistance vale zero no primeiro quadro e ele
    // acha que chegou sem ter saido do lugar.
    private bool Arrived()
    {
        if (agent.pathPending)
        {
            return false;
        }

        return agent.remainingDistance <= arriveTolerance;
    }

    private bool TrySnapToNavMesh(Vector3 desired, float range, out Vector3 result)
    {
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, range, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = desired;
        return false;
    }

    // Sorteia um ponto que exista na NavMesh. Tenta algumas vezes porque o
    // primeiro sorteio pode cair dentro de uma parede ou fora do mapa.
    private bool TryRandomNavPoint(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 candidate = center + Random.insideUnitSphere * radius;
            candidate.y = center.y;

            if (TrySnapToNavMesh(candidate, 2f, out result))
            {
                return true;
            }
        }

        result = center;
        return false;
    }

    // Comprimento do caminho ANDANDO, somando canto a canto.
    // Retorna infinito se nao houver caminho - e assim que ele descobre que
    // voce esta num lugar onde ele nao consegue chegar.
    private float PathLengthTo(Vector3 target)
    {
        if (!agent.isOnNavMesh || !agent.CalculatePath(target, scratchPath))
        {
            return float.PositiveInfinity;
        }

        if (scratchPath.status != NavMeshPathStatus.PathComplete)
        {
            return float.PositiveInfinity;
        }

        Vector3[] corners = scratchPath.corners;

        if (corners.Length < 2)
        {
            return 0f;
        }

        float total = 0f;

        for (int i = 1; i < corners.Length; i++)
        {
            total += Vector3.Distance(corners[i - 1], corners[i]);
        }

        return total;
    }

    private void LookAt(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        // O agente esta parado, entao a rotacao dele nao manda mais.
        // Assumo o giro na mao para ele encarar a porta enquanto espera.
        agent.updateRotation = false;

        Quaternion wanted = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, wanted, turnSpeedWhenStopped * Time.deltaTime);
    }

    // Ela nao evaporou: alarga a busca para o raio que ela ja teria alcancado
    // andando, e recomeca. O miolo fica de fora porque o miolo ele ja viu.
    private void ReseedSearch()
    {
        float reach = Mathf.Clamp(timeSinceSeen * searchSpreadSpeed, searchRadius, 45f);

        memory.AddRing(lastKnownPosition, reach * 0.55f, reach, 1f);
        memoryConfidence = memory.Confidence;

        searchPlan.Clear();
        BuildSearchPlan();

        currentThought = "Ela ja deve estar mais longe. Vou abrir a busca.";

        if (searchPlan.Count > 0)
        {
            Vector3 next = searchPlan[0];
            searchPlan.RemoveAt(0);
            SetDestination(next);
        }
    }

    // Comeca (ou recomeca) a viagem ate o ultimo rastro. O orcamento de tempo
    // vem do comprimento real do caminho, entao um rastro a 60 m ganha tempo
    // de chegar, e um rastro inalcancavel e descartado na hora.
    private void StartInvestigateTravel()
    {
        investigateArrived = false;
        searchTimer = 0f;
        stateTimer = 0f;
        searchPlan.Clear();

        if (!IsReachable(lastKnownPosition, out float length))
        {
            if (TryRandomNavPoint(lastKnownPosition, searchRadius, out Vector3 near) && IsReachable(near, out length))
            {
                lastKnownPosition = near;
            }
            else
            {
                travelBudget = 0f; // cai em Patrol no proximo Think
                return;
            }
        }

        float speed = Mathf.Max(agent.speed, 0.5f);
        travelBudget = Mathf.Clamp(length / speed + 4f, 5f, 45f);
        SetDestination(lastKnownPosition);
    }

    // Ela sumiu ha pouco: continua no rumo dela, limitado a alguns segundos.
    // Depois disso a pista envelhece e volta a valer o ultimo ponto visto.
    private Vector3 PredictedLostPosition()
    {
        float t = Mathf.Min(timeSinceSeen, lostPredictTime);
        Vector3 predicted = lastKnownPosition + lastSeenVelocity * t;

        if (TrySnapToNavMesh(predicted, 2f, out Vector3 valid) && IsReachable(valid, out _))
        {
            return valid;
        }

        return lastKnownPosition;
    }

    // Monta a lista de lugares para checar a partir de onde ela sumiu.
    // Pontuacao: perto e melhor; no rumo em que ela ia e melhor; fora da
    // minha linha de visao de quando ela sumiu (atras da quina) e MUITO
    // melhor - se eu enxergasse o lugar, ela nao estaria la.
    private void BuildSearchPlan()
    {
        searchPlan.Clear();

        // Com o mapa montado, os lugares saem dele: as celulas em que ele
        // ainda acredita, descontada a distancia. O resultado e uma varredura
        // que segue o corredor em vez de pular de ponto sorteado em ponto
        // sorteado - e ele nunca volta para onde acabou de olhar, porque
        // olhar zera a celula.
        if (memory.IsBuilt && memory.Confidence > memoryGiveUpConfidence)
        {
            memory.GetBestCandidates(transform.position, 6, memoryDistancePenalty, searchPlan);

            for (int i = searchPlan.Count - 1; i >= 0; i--)
            {
                if (!IsReachable(searchPlan[i], out _))
                {
                    searchPlan.RemoveAt(i);
                }
            }

            if (searchPlan.Count > 0)
            {
                return;
            }
        }

        List<Vector3> candidates = new List<Vector3>();
        List<float> scores = new List<float>();

        Vector3 hint = lastSeenVelocity;
        hint.y = 0f;
        bool hasHint = hint.sqrMagnitude > 0.25f;
        if (hasHint) hint.Normalize();

        Vector3 eyes = lostSightPosition + Vector3.up * playerHeightOffset;

        for (int i = 0; i < 18; i++)
        {
            float angle;

            // Metade dos chutes segue o rumo dela, a outra metade cobre o resto
            if (hasHint && i % 2 == 0)
            {
                angle = Mathf.Atan2(hint.x, hint.z) * Mathf.Rad2Deg + Random.Range(-60f, 60f);
            }
            else
            {
                angle = Random.Range(0f, 360f);
            }

            float radius = Random.Range(searchRadius * 0.35f, searchRadius);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 candidate = lastKnownPosition + dir * radius;

            if (!TrySnapToNavMesh(candidate, 2.5f, out Vector3 valid)) continue;
            if (!IsReachable(valid, out float length) || length > searchRadius * 3f) continue;

            float score = length;

            if (hasHint)
            {
                float alignment = Vector3.Dot(hint, (valid - lastKnownPosition).normalized);
                score -= alignment * searchRadius * 0.6f;
            }

            bool hidden = Physics.Linecast(eyes, valid + Vector3.up * playerHeightOffset, obstacleMask);
            if (hidden) score -= searchRadius;

            candidates.Add(valid);
            scores.Add(score);
        }

        // Do menor score (mais provavel) ao maior, sem dois vizinhos colados
        while (candidates.Count > 0 && searchPlan.Count < 5)
        {
            int best = 0;
            for (int i = 1; i < scores.Count; i++)
            {
                if (scores[i] < scores[best]) best = i;
            }

            Vector3 chosen = candidates[best];
            candidates.RemoveAt(best);
            scores.RemoveAt(best);

            bool tooClose = false;
            for (int i = 0; i < searchPlan.Count; i++)
            {
                if (Vector3.Distance(searchPlan[i], chosen) < 3f) { tooClose = true; break; }
            }

            if (!tooClose) searchPlan.Add(chosen);
        }
    }

    // Sem ambushPoints na cena, ele procura sozinho uma quina: um lugar
    // perto de onde ela vai passar, alcancavel, e que NAO se ve de la.
    private bool TryFindAmbushSpot(out Vector3 spot)
    {
        Vector3 expected = lastKnownPosition + lastSeenVelocity * 2f;
        if (!TrySnapToNavMesh(expected, 3f, out expected)) expected = lastKnownPosition;

        Vector3 eyes = expected + Vector3.up * playerHeightOffset;
        float bestScore = float.PositiveInfinity;
        spot = transform.position;
        bool found = false;

        // Varre a volta toda em passos fixos (com um pouco de jitter) em vez
        // de sortear: sorteio puro deixava de achar quina uma vez em dez.
        for (int i = 0; i < 36; i++)
        {
            float angle = i * 10f + Random.Range(-5f, 5f);
            float radius = (i % 3 == 0) ? 5f : (i % 3 == 1) ? 8f : ambushSpotRadius;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 candidate = expected + dir * radius;

            if (!TrySnapToNavMesh(candidate, 2.5f, out Vector3 valid)) continue;

            // Precisa ser escondido de onde ela vem...
            if (!Physics.Linecast(eyes, valid + Vector3.up * playerHeightOffset, obstacleMask)) continue;

            // ...mas perto o bastante andando para ela passar por ali. O limite
            // precisa caber a volta numa parede inteira, senao nunca acha quina.
            float toExpected = PathLengthBetween(valid, expected);
            if (float.IsPositiveInfinity(toExpected) || toExpected > ambushSpotMaxDetour) continue;

            if (!IsReachable(valid, out float fromMe)) continue;

            float score = fromMe + toExpected * 0.5f;
            if (score < bestScore)
            {
                bestScore = score;
                spot = valid;
                found = true;
            }
        }

        return found;
    }

    // Comprimento do caminho entre dois pontos quaisquer da NavMesh.
    private float PathLengthBetween(Vector3 from, Vector3 to)
    {
        if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, scratchPath) ||
            scratchPath.status != NavMeshPathStatus.PathComplete)
        {
            return float.PositiveInfinity;
        }

        float total = 0f;
        Vector3[] corners = scratchPath.corners;
        for (int i = 1; i < corners.Length; i++)
        {
            total += Vector3.Distance(corners[i - 1], corners[i]);
        }
        return total;
    }

    // =========================================================
    // ESCOLHA DE DESTINO
    // =========================================================

    private void PickPatrolTarget()
    {
        // Sem pontos montados na cena ele sorteia lugares na propria NavMesh,
        // entao o script ja funciona no primeiro Play sem ninguem preparar
        // nada - e sem sortear pontos dentro de parede.
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            if (TryPickCoveragePoint(out Vector3 spot))
            {
                SetDestination(spot);
            }
            else if (TryRandomNavPoint(transform.position, patrolRadius, out spot))
            {
                SetDestination(spot);
            }

            return;
        }

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        if (patrolPoints[patrolIndex] != null)
        {
            SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    // Ronda que cobre terreno em vez de rodar em circulo: pontos longe o
    // bastante, alcancaveis andando, longe do que ele ja visitou ha pouco.
    // De vez em quando (instinctChance) o sorteio puxa para o lado onde o
    // jogador esta - e o "faro" que impede a fase de morrer de tedio num
    // mapa grande, sem entregar a posicao exata.
    private bool TryPickCoveragePoint(out Vector3 result)
    {
        result = transform.position;
        float bestScore = float.NegativeInfinity;
        bool found = false;

        bool instinct = Random.value < instinctChance;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float playerAngle = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;

        for (int i = 0; i < 14; i++)
        {
            float angle = instinct
                ? playerAngle + Random.Range(-50f, 50f)
                : Random.Range(0f, 360f);

            float radius = Random.Range(patrolMinRadius, patrolRadius);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 candidate = transform.position + dir * radius;

            if (!TrySnapToNavMesh(candidate, 4f, out Vector3 valid)) continue;
            if (!IsReachable(valid, out float length) || length > patrolRadius * 2f) continue;

            // Quanto mais longe do que ja visitou, melhor
            float novelty = float.PositiveInfinity;
            for (int j = 0; j < recentPatrol.Count; j++)
            {
                novelty = Mathf.Min(novelty, Vector3.Distance(recentPatrol[j], valid));
            }
            if (float.IsPositiveInfinity(novelty)) novelty = patrolRadius;

            float score = Mathf.Min(novelty, patrolRadius) - length * 0.25f;

            if (score > bestScore)
            {
                bestScore = score;
                result = valid;
                found = true;
            }
        }

        if (found)
        {
            recentPatrol.Add(result);
            if (recentPatrol.Count > patrolMemory) recentPatrol.RemoveAt(0);
        }

        return found;
    }

    // O melhor lugar para emboscar e o mais perto do ultimo rastro do jogador,
    // medido pelo caminho andando e nao em linha reta: uma porta a 4 m com
    // parede no meio nao serve de emboscada.
    private Transform FindAmbushPoint()
    {
        if (ambushPoints == null || ambushPoints.Length == 0)
        {
            return null;
        }

        Transform best = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < ambushPoints.Length; i++)
        {
            if (ambushPoints[i] == null)
            {
                continue;
            }

            float distance = Vector3.Distance(ambushPoints[i].position, lastKnownPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = ambushPoints[i];
            }
        }

        return best;
    }

    private Vector3 FarthestPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Vector3 away = (transform.position - lastKnownPosition).normalized;

            for (int i = 0; i < 6; i++)
            {
                if (TryRandomNavPoint(transform.position + away * patrolMinRadius, patrolMinRadius * 0.6f, out Vector3 spot)
                    && IsReachable(spot, out _))
                {
                    return spot;
                }
            }

            return transform.position;
        }

        Vector3 best = transform.position;
        float bestDistance = -1f;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
            {
                continue;
            }

            float distance = Vector3.Distance(patrolPoints[i].position, lastKnownPosition);

            if (distance > bestDistance)
            {
                bestDistance = distance;
                best = patrolPoints[i].position;
            }
        }

        return best;
    }

    // =========================================================
    // SOM
    // =========================================================
    //
    // Tres AudioSources separadas, criadas sozinhas no Awake. Sao tres e nao
    // uma porque um som corta o outro: se o passo e o rugido dividissem a mesma
    // fonte, cada passo engoliria o rugido pela metade.
    //
    //   stepSource   -> passos
    //   voiceSource  -> rugidos, gritos, resmungos
    //   loopSource   -> respiracao continua
    //
    // Todas em 3D puro (spatialBlend = 1). Isso importa mais do que parece:
    // e o que deixa o jogador virar a cabeca e saber de que lado ele vem.

    private void SetupAudio()
    {
        stepSource = CreateAudioSource(false);
        voiceSource = CreateAudioSource(false);
        loopSource = CreateAudioSource(true);
    }

    private AudioSource CreateAudioSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 1f;                        // 3D puro, nada de som na cabeca
        source.rolloffMode = AudioRolloffMode.Linear;    // Linear e mais previsivel para ajustar
        source.minDistance = soundMinDistance;
        source.maxDistance = soundMaxDistance;

        return source;
    }

    private void UpdateAudio()
    {
        UpdateFootsteps();
        UpdateBreathing();
        UpdateRandomSounds();
    }

    // =========================
    // PASSOS
    // =========================

    // Contados por metro andado, e o metro vem da velocidade real do agente.
    // Assim o passo acompanha sozinho a mudanca entre andar e correr, e um
    // monstro travado numa quina nao toca passo parado no lugar.
    private void UpdateFootsteps()
    {
        if (IsSilent() || footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        distanceWalked += agent.velocity.magnitude * Time.deltaTime;

        float threshold = state == MonsterState.Hunt
            ? stepDistance * huntStepMultiplier
            : stepDistance;

        if (distanceWalked < threshold)
        {
            return;
        }

        distanceWalked = 0f;
        PlayRandom(stepSource, footstepClips, footstepVolume);
    }

    // =========================
    // RESPIRACAO
    // =========================

    // Troca o loop conforme o estado e faz fade no volume, para a respiracao
    // nao aparecer e sumir com um clique.
    private void UpdateBreathing()
    {
        if (loopSource == null)
        {
            return;
        }

        AudioClip wanted = null;

        if (!IsSilent())
        {
            wanted = state == MonsterState.Hunt ? huntBreathing : patrolBreathing;
        }

        if (wanted == null)
        {
            loopSource.volume = Mathf.MoveTowards(loopSource.volume, 0f, breathingFadeSpeed * Time.deltaTime);

            if (loopSource.volume <= 0.01f && loopSource.isPlaying)
            {
                loopSource.Stop();
            }

            return;
        }

        if (loopSource.clip != wanted)
        {
            loopSource.clip = wanted;
            loopSource.volume = 0f;
            loopSource.Play();
        }

        loopSource.volume = Mathf.MoveTowards(loopSource.volume, breathingVolume, breathingFadeSpeed * Time.deltaTime);
    }

    // =========================
    // RESMUNGO ALEATORIO
    // =========================

    // O som que ele solta sem motivo, longe do jogador. Serve para lembrar que
    // ele existe e nao deixar o jogador saber onde ele esta.
    private void UpdateRandomSounds()
    {
        if (IsSilent() || state == MonsterState.Hunt)
        {
            return;
        }

        randomSoundTimer -= Time.deltaTime;

        if (randomSoundTimer > 0f)
        {
            return;
        }

        ScheduleNextRandomSound();
        PlayVoice(patrolSounds);
    }

    private void ScheduleNextRandomSound()
    {
        randomSoundTimer = Random.Range(randomSoundMinDelay, randomSoundMaxDelay);
    }

    // =========================
    // O SILENCIO DA EMBOSCADA
    // =========================

    // A regra mais importante deste arquivo inteiro.
    //
    // Um monstro que resolveu esperar escondido e continua respirando alto e
    // pisando forte nao esta emboscando ninguem - ele esta avisando. Enquanto
    // ele espera, tudo cala: passos, respiracao e resmungo.
    //
    // O silencio repentino tambem funciona como aviso ao contrario. O jogador
    // percebe que parou de ouvir o monstro e entende que alguma coisa mudou,
    // sem saber o que. Isso e melhor que qualquer rugido.
    private bool IsSilent()
    {
        return silentDuringAmbush && state == MonsterState.Ambush;
    }

    // =========================
    // TOCAR
    // =========================

    private void PlayVoice(AudioClip[] clips)
    {
        if (IsSilent())
        {
            return;
        }

        PlayRandom(voiceSource, clips, voiceVolume);
    }

    // Sorteia um clipe da lista, nunca repete o anterior, e varia o tom.
    // Som identico repetido e o que mais mata a ilusao de uma criatura viva.
    private void PlayRandom(AudioSource source, AudioClip[] clips, float volume)
    {
        if (source == null || clips == null || clips.Length == 0)
        {
            return;
        }

        int index = Random.Range(0, clips.Length);

        // Com 2 ou mais variacoes, evita tocar a mesma duas vezes seguidas
        if (clips.Length > 1 && index == lastClipIndex)
        {
            index = (index + 1) % clips.Length;
        }

        lastClipIndex = index;

        if (clips[index] == null)
        {
            return;
        }

        source.pitch = Random.Range(0.92f, 1.08f);
        source.PlayOneShot(clips[index], volume);
    }

    // =========================================================
    // VISUALIZACAO NO EDITOR
    // =========================================================

    // Selecione o monstro na Scene para ver o cone de visao, o raio de
    // audicao, a bolinha rosa do ultimo lugar onde ele te viu e o caminho
    // que o NavMesh calculou para ele.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = canSee ? Color.red : Color.yellow;

        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 eyes = transform.position + Vector3.up * playerHeightOffset;

        Gizmos.DrawRay(eyes, left * viewDistance);
        Gizmos.DrawRay(eyes, right * viewDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        // Canto do olho: curto, mas quase tudo em volta
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Vector3 pLeft = Quaternion.Euler(0f, -peripheralAngle * 0.5f, 0f) * transform.forward;
        Vector3 pRight = Quaternion.Euler(0f, peripheralAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawRay(eyes, pLeft * peripheralDistance);
        Gizmos.DrawRay(eyes, pRight * peripheralDistance);

        if (!Application.isPlaying)
        {
            return;
        }

        // O mapa de memoria: laranja onde ele acha que ela esta
        if (showMemoryGizmo)
        {
            memory.DrawGizmos(0.55f);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);

        // Lugares que ele ainda vai checar nesta busca
        Gizmos.color = new Color(1f, 0.5f, 0f);
        for (int i = 0; i < searchPlan.Count; i++)
        {
            Gizmos.DrawWireSphere(searchPlan[i], 0.35f);
        }

        if (hasAmbushSpot)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(ambushSpot, Vector3.one * 0.6f);
        }

        // O caminho real que ele vai percorrer
        if (agent != null && agent.hasPath)
        {
            Vector3[] corners = agent.path.corners;

            Gizmos.color = Color.green;

            for (int i = 1; i < corners.Length; i++)
            {
                Gizmos.DrawLine(corners[i - 1], corners[i]);
            }
        }
    }
}
