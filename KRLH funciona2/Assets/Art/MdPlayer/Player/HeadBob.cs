using UnityEngine;

public class HeadBob : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;

    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;

    [Header("Head Bob - Walk")]
    public float walkFrequency = 8f;
    public float walkAmplitude = 0.05f;

    [Header("Head Bob - Sprint")]
    public float sprintFrequency = 12f;
    public float sprintAmplitude = 0.08f;

    [Header("Head Bob Smooth")]
    public float bobSmooth = 10f;

    [Header("FOV")]
    public float normalFOV = 60f;
    public float sprintFOV = 75f;

    [Header("FOV Smooth")]
    public float fovSmooth = 8f;

    [Header("Tremor do Ursão")]
    [Tooltip("O Ursão. Vazio = procura sozinho pelo UrsaoMortal na cena.")]
    public Transform ursao;

    [Tooltip("A partir desta distância a câmera começa a tremer.")]
    public float distanciaDoTremor = 45f;

    [Tooltip("Distância em que o tremor chega no máximo.")]
    public float distanciaDoTremorMaximo = 8f;

    [Tooltip("Quanto a câmera treme no pior caso, em metros.")]
    public float forcaDoTremor = 0.12f;

    [Tooltip("Quão rápido o tremor sacode.")]
    public float velocidadeDoTremor = 22f;

    [Tooltip("Desligue para tirar o tremor sem mexer no resto.")]
    public bool tremorLigado = true;

    private Vector3 originalPosition;
    private float bobTimer;
    private float tremorAtual;
    private float procuraTimer;

    private void Start()
    {
        originalPosition = transform.localPosition;

        SetFOV(firstPersonCamera, normalFOV);
        SetFOV(thirdPersonCamera, normalFOV);
    }

    private void LateUpdate()
    {
        if (playerMovement == null)
            return;

        UpdateHeadBob();
        UpdateTremor();
        UpdateFOV();
    }

    // =========================================================
    // TREMOR DO URSÃO
    // =========================================================

    // Quanto mais perto o Ursão chega, mais a câmera treme.
    // É aviso de perigo sem precisar de som nem de interface.
    private void UpdateTremor()
    {
        if (!tremorLigado)
            return;

        AcharUrsao();

        float alvo = 0f;

        if (ursao != null)
        {
            // Só o plano do chão: ele ser alto não conta como estar perto.
            Vector3 a = transform.position;
            Vector3 b = ursao.position;

            a.y = 0f;
            b.y = 0f;

            float distancia = Vector3.Distance(a, b);

            if (distancia < distanciaDoTremor)
            {
                float t = Mathf.InverseLerp(
                    distanciaDoTremor,
                    distanciaDoTremorMaximo,
                    distancia
                );

                // ao quadrado: longe quase não treme, perto sobe rápido
                alvo = t * t;
            }
        }

        // Sobe e desce suave, para não ligar e desligar de repente
        tremorAtual = Mathf.Lerp(
            tremorAtual,
            alvo,
            3f * Time.deltaTime
        );

        if (tremorAtual <= 0.001f)
            return;

        // Ruído em duas frequências: sacode sem virar vibração de motor
        float tempo = Time.time * velocidadeDoTremor;

        float x =
            (Mathf.PerlinNoise(tempo, 0f) - 0.5f) * 2f;

        float y =
            (Mathf.PerlinNoise(0f, tempo * 1.3f) - 0.5f) * 2f;

        transform.localPosition +=
            new Vector3(x, y, 0f) * forcaDoTremor * tremorAtual;
    }

    // Procura de tempos em tempos, não todo quadro:
    // o Ursão pode nascer depois, ou trocar de cena.
    private void AcharUrsao()
    {
        if (ursao != null)
            return;

        procuraTimer -= Time.deltaTime;

        if (procuraTimer > 0f)
            return;

        procuraTimer = 1f;

        UrsaoMortal achado =
            FindFirstObjectByType<UrsaoMortal>();

        if (achado != null)
        {
            ursao = achado.transform;
        }
    }

    public float TremorAtual
    {
        get { return tremorAtual; }
    }

    // =========================================================
    // HEAD BOB
    // =========================================================

    private void UpdateHeadBob()
    {
        Vector2 movement =
            playerMovement.GetMovementInput();

        bool isMoving =
            movement.magnitude > 0.1f;

        if (!isMoving)
        {
            bobTimer = 0f;

            transform.localPosition =
                Vector3.Lerp(
                    transform.localPosition,
                    originalPosition,
                    bobSmooth * Time.deltaTime
                );

            return;
        }

        bool isSprinting =
            playerMovement.IsSprinting();

        float frequency =
            isSprinting
                ? sprintFrequency
                : walkFrequency;

        float amplitude =
            isSprinting
                ? sprintAmplitude
                : walkAmplitude;

        bobTimer +=
            Time.deltaTime * frequency;

        float x =
            Mathf.Cos(bobTimer * 0.5f)
            * amplitude;

        float y =
            Mathf.Sin(bobTimer)
            * amplitude;

        Vector3 targetPosition =
            originalPosition +
            new Vector3(x, y, 0f);

        transform.localPosition =
            Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                bobSmooth * Time.deltaTime
            );
    }

    // =========================================================
    // FOV
    // =========================================================

    private void UpdateFOV()
    {
        Camera activeCamera =
            GetActiveCamera();

        if (activeCamera == null)
            return;

        bool isSprinting =
            playerMovement.IsSprinting();

        float targetFOV =
            isSprinting
                ? sprintFOV
                : normalFOV;

        activeCamera.fieldOfView =
            Mathf.Lerp(
                activeCamera.fieldOfView,
                targetFOV,
                fovSmooth * Time.deltaTime
            );
    }

    // =========================================================
    // ACTIVE CAMERA
    // =========================================================

    private Camera GetActiveCamera()
    {
        if (firstPersonCamera != null &&
            firstPersonCamera.isActiveAndEnabled)
        {
            return firstPersonCamera;
        }

        if (thirdPersonCamera != null &&
            thirdPersonCamera.isActiveAndEnabled)
        {
            return thirdPersonCamera;
        }

        return null;
    }

    // =========================================================
    // SET FOV
    // =========================================================

    private void SetFOV(Camera camera, float fov)
    {
        if (camera != null)
        {
            camera.fieldOfView = fov;
        }
    }
}