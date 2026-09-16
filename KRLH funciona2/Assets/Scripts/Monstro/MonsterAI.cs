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
    public float viewDistance = 18f;
    public float viewAngle = 100f;        // angulo total do cone, em graus
    public LayerMask obstacleMask;        // o que bloqueia a visao; vazio = tudo menos o jogador

    [Header("Audicao")]
    public float hearingRadius = 12f;
    public float runningSpeed = 4.5f;     // acima disso o jogador faz barulho alto
    public float sneakingSpeed = 1.5f;    // abaixo disso e quase silencio
    public float hearingError = 3f;       // som entrega a regiao, nunca o ponto exato

    [Header("Consciencia")]
    public float awarenessGain = 1.4f;    // por segundo, com o jogador bem visivel
    public float awarenessDecay = 0.25f;  // por segundo, sem nenhum sinal
    public float hearingWeight = 0.35f;   // ouvir sobe bem mais devagar que ver

    [Header("Lanterna - opcional")]
    public Transform flashlight;
    public float flashlightAngle = 45f;
    public float flashlightWeight = 1.8f; // ser iluminado entrega o jogador mais rapido

    [Header("Movimento - o NavMeshAgent recebe estes valores")]
    public float walkSpeed = 1.6f;
    public float huntSpeed = 3.4f;
    public float searchSpeed = 2.2f;
    public float angularSpeed = 220f;     // graus por segundo ao virar
    public float acceleration = 8f;
    public float turnSpeedWhenStopped = 6f; // giro manual parado, na emboscada

    [Header("Comportamento")]
    public float patrolRadius = 12f;
    public float patrolWaitTime = 2.5f;
    public float arriveTolerance = 1.2f;
    public float investigateTime = 9f;
    public float searchInterval = 2.5f;
    public float searchRadius = 5f;
    public float catchDistance = 1.6f;

    [Header("Cortar caminho")]
    // Enquanto ele te ve correndo, ele mira adiante de voce em vez de mirar em
    // voce. E o que transforma "correr atras" em "cortar o caminho".
    public bool interceptEnabled = true;
    public float interceptLookAhead = 1.1f;  // segundos de antecipacao
    public float interceptMinSpeed = 2f;     // so vale a pena se voce estiver correndo

    [Header("Ritmo do medo")]
    public float maxHuntTime = 22f;       // cacar mais que isso cansa o jogador
    public float retreatTime = 8f;
    public float ambushTime = 12f;
    public float ambushChance = 0.45f;    // chance base de esperar ao inves de procurar
    public float ambushPathThreshold = 18f; // caminho maior que isso favorece emboscar
    public bool directorEnabled = true;
    public float boredomTime = 35f;       // tempo perdido antes de receber uma dica
    public float rumorError = 9f;         // o quanto a dica e imprecisa, em metros

    [Header("Pontos da fase - opcionais")]
    public Transform[] patrolPoints;      // vazio = ele sorteia pontos na propria NavMesh
    public Transform[] ambushPoints;      // portas e corredores onde vale esperar

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
    public MonsterState state = MonsterState.Patrol;
    public float awareness;
    public bool canSee;
    public bool canHear;

    // Percepcao interna
    private Vector3 lastKnownPosition;
    private float timeSinceSeen = 999f;
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

        Sense();
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

        canSee = CanSeePlayer();

        if (canSee)
        {
            lastKnownPosition = player.position;
            timeSinceSeen = 0f;
        }

        canHear = CanHearPlayer();

        // Som so marca a regiao, e a visao sempre ganha do ouvido
        if (canHear && !canSee)
        {
            Vector2 error = Random.insideUnitCircle * hearingError;
            lastKnownPosition = player.position + new Vector3(error.x, 0f, error.y);
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
        Vector3 target = player.position + Vector3.up * playerHeightOffset;
        Vector3 origin = transform.position + Vector3.up * playerHeightOffset;

        if (Vector3.Distance(origin, target) > viewDistance)
        {
            return false;
        }

        Vector3 direction = (target - origin).normalized;

        if (Vector3.Angle(transform.forward, direction) > viewAngle * 0.5f)
        {
            return false;
        }

        // Tem parede no meio?
        return !Physics.Linecast(origin, target, obstacleMask);
    }

    // A audicao atravessa parede de proposito: e som. E justo porque so
    // entrega a regiao, nunca a posicao exata.
    private bool CanHearPlayer()
    {
        float noiseRadius;

        if (playerSpeed >= runningSpeed)
        {
            noiseRadius = hearingRadius;
        }
        else if (playerSpeed >= sneakingSpeed)
        {
            noiseRadius = hearingRadius * 0.5f;
        }
        else
        {
            noiseRadius = hearingRadius * 0.15f;
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
    // THINK - o que isso significa, e para onde eu vou
    // =========================================================

    private void Think()
    {
        RunDirector();

        // Recuo e sagrado: ele nao volta a cacar antes da hora.
        // E o respiro do jogador, e nada interrompe.
        if (state == MonsterState.Retreat)
        {
            if (stateTimer >= retreatTime)
            {
                huntFatigue = 0f;
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

                if (stateTimer >= investigateTime)
                {
                    EnterState(MonsterState.Patrol);
                }

                break;

            case MonsterState.Hunt:

                huntFatigue += Time.deltaTime;

                // Cacar demais cansa o jogador e o susto perde a forca
                if (huntFatigue >= maxHuntTime)
                {
                    EnterState(MonsterState.Retreat);
                    break;
                }

                if (!canSee && timeSinceSeen > 3f)
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
        if (FindAmbushPoint() == null)
        {
            return false;
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
        if (TrySnapToNavMesh(rumor, rumorError, out Vector3 valid))
        {
            lastKnownPosition = valid;
            EnterState(MonsterState.Investigate);
        }
    }

    private void EnterState(MonsterState next)
    {
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
                searchTimer = 0f;
                SetDestination(lastKnownPosition);
                PlayVoice(alertSounds);
                break;

            case MonsterState.Hunt:
                currentThought = "Achei ela. Vou atras.";
                agent.speed = huntSpeed;
                PlayVoice(spotSounds);
                break;

            case MonsterState.Ambush:
                chosenAmbush = FindAmbushPoint();
                currentThought = "Ela sumiu. Vou ficar quieto ali e esperar ela passar.";
                agent.speed = huntSpeed;

                if (chosenAmbush != null)
                {
                    SetDestination(chosenAmbush.position);
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
        Resume();

        if (!Arrived())
        {
            return;
        }

        Stop();
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
        Resume();

        if (!Arrived())
        {
            return;
        }

        Stop();
        searchTimer += Time.deltaTime;

        if (searchTimer < searchInterval)
        {
            return;
        }

        searchTimer = 0f;
        currentThought = "Nao esta aqui. Vou olhar mais adiante.";
        PlayVoice(searchSounds);

        // Sorteia um lugar por perto que exista de verdade na NavMesh
        if (TryRandomNavPoint(lastKnownPosition, searchRadius, out Vector3 spot))
        {
            SetDestination(spot);
        }
    }

    private void ActHunt()
    {
        Resume();

        Vector3 target = canSee ? InterceptPoint() : lastKnownPosition;
        SetDestination(target);

        if (canSee && Vector3.Distance(transform.position, player.position) <= catchDistance)
        {
            Stop();
            OnCatchPlayer();
            return;
        }

        currentThought = canSee
            ? (IsIntercepting() ? "Ela vai passar ali. Vou cortar caminho." : "Estou vendo ela. Nao vou parar.")
            : "Ela virou ali. Ainda da pra alcancar.";
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
        if (chosenAmbush == null)
        {
            EnterState(MonsterState.Patrol);
            return;
        }

        if (!Arrived())
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
            agent.SetDestination(valid);
        }
    }

    private void Resume()
    {
        if (agent.isStopped)
        {
            agent.isStopped = false;
        }

        agent.updateRotation = true;
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
            if (TryRandomNavPoint(transform.position, patrolRadius, out Vector3 spot))
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

            if (TryRandomNavPoint(transform.position + away * patrolRadius, patrolRadius * 0.5f, out Vector3 spot))
            {
                return spot;
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

        if (!Application.isPlaying)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);

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
