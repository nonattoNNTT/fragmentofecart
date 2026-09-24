using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Cameras")]
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;

    [Header("Camera Pivot")]
    public Transform cameraPivot;

    [Header("Mouse")]
    public float mouseSensitivity = 0.15f;
    public float minVerticalAngle = -60f;
    public float maxVerticalAngle = 60f;

    [Header("Third Person")]
    public float cameraDistance = 3.5f;
    public float cameraHeight = 1f;

    [Tooltip("Deslocamento lateral da câmera. Positivo = direita, negativo = esquerda.")]
    public float shoulderOffset = 0.6f;

    [Header("Camera Collision")]
    public float collisionRadius = 0.3f;
    public float collisionOffset = 0.15f;
    public float cameraReturnSpeed = 0.08f;
    public LayerMask collisionLayers;

    [Header("Cinemática")]
    [Tooltip("Desligado enquanto o trilho da câmera roda. Quem liga e desliga é o CameraRail.")]
    public bool controleAtivo = true;

    [Header("Modelo em primeira pessoa")]
    [Tooltip("O modelo da Valentina. Some em primeira pessoa e volta em terceira. Vazio = acha sozinho pelo Animator.")]
    public Transform modeloDoPlayer;

    [Tooltip("Desligue se quiser enxergar o corpo tambem em primeira pessoa.")]
    public bool esconderModeloEmPrimeiraPessoa = true;

    private Vector2 lookInput;

    private float verticalRotation = 0f;

    private bool firstPerson = true;

    private Renderer[] renderizadoresDoCorpo;

    private float currentCameraDistance;
    private float cameraDistanceVelocity;

    private void Awake()
    {
        // Precisa ser no Awake: o Start ja chama AplicarEstado,
        // que decide se o corpo aparece ou nao.
        if (modeloDoPlayer == null)
        {
            Animator animador = GetComponentInChildren<Animator>(true);

            if (animador != null)
            {
                modeloDoPlayer = animador.transform;
            }
        }

        if (modeloDoPlayer != null)
        {
            renderizadoresDoCorpo =
                modeloDoPlayer.GetComponentsInChildren<Renderer>(true);
        }
    }

    private void Start()
    {
        currentCameraDistance = cameraDistance;

        AplicarEstado();
    }

    private void Update()
    {
        if (!controleAtivo)
            return;

        RotateCamera();
    }

    private void LateUpdate()
    {
        if (!controleAtivo)
            return;

        UpdateThirdPersonCamera();
    }

    // =========================
    // MOUSE
    // =========================

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    // =========================
    // TROCAR CÂMERA
    // =========================

    public void OnSwitchCamera(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        firstPerson = !firstPerson;

        SetCamera(firstPerson);
    }

    // =========================
    // ROTAÇÃO
    // =========================

    private void RotateCamera()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // Gira o Player horizontalmente
        transform.Rotate(0f, mouseX, 0f);

        // Gira o Pivot verticalmente
        verticalRotation -= mouseY;

        verticalRotation = Mathf.Clamp(
            verticalRotation,
            minVerticalAngle,
            maxVerticalAngle
        );

        cameraPivot.localRotation = Quaternion.Euler(
            verticalRotation,
            0f,
            0f
        );
    }

    // =========================
    // CÂMERA 3ª PESSOA
    // =========================

    private void UpdateThirdPersonCamera()
    {
        if (firstPerson)
            return;

        if (cameraPivot == null || thirdPersonCamera == null)
            return;

        // Os valores do Inspector valem para o Player em escala 1.
        // Sem isto a câmera fica dentro da Valentina quando a escala sobe.
        float escala = Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.z
        );

        float distanciaMax = cameraDistance * escala;
        float raioColisao = collisionRadius * escala;
        float folgaColisao = collisionOffset * escala;
        float distanciaMin = 0.15f * escala;

        Vector3 pivotPosition =
            cameraPivot.position +
            Vector3.up * cameraHeight * escala;

        Vector3 shoulderPosition =
            cameraPivot.right * shoulderOffset * escala;

        // A câmera sai do ponto que já inclui o ombro, e o teste de parede
        // sai do mesmo ponto. Antes o teste saía do pivô e a câmera ia para
        // outro lugar, então ela parava num ponto que ninguém tinha conferido.
        Vector3 origem = pivotPosition + shoulderPosition;

        Vector3 direction =
            -cameraPivot.forward.normalized;

        float targetDistance = distanciaMax;

        // Detecta paredes e obstáculos.
        // É SphereCastAll porque o teste começa dentro da própria cápsula do
        // Player: um SphereCast comum acertava o próprio Player e devolvia
        // distância 0, o que grudava a câmera dentro da cabeça dela.
        RaycastHit[] batidas = Physics.SphereCastAll(
            origem,
            raioColisao,
            direction,
            distanciaMax,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        float maisPerto = distanciaMax;

        foreach (RaycastHit hit in batidas)
        {
            // Ignora o próprio Player e tudo que é filho dele
            if (hit.transform.IsChildOf(transform))
                continue;

            // distância 0 = o cast já nasceu dentro desse collider.
            // Acontece com o Teto, porque a Valentina é alta para o quarto.
            // Esse valor não mede nada, e usá-lo grudava a câmera no mínimo.
            if (hit.distance <= 0.001f)
                continue;

            if (hit.distance < maisPerto)
            {
                maisPerto = hit.distance;
            }
        }

        if (maisPerto < distanciaMax)
        {
            targetDistance = Mathf.Clamp(
                maisPerto - folgaColisao,
                distanciaMin,
                distanciaMax
            );

            // Aproxima na hora, para não atravessar a parede
            currentCameraDistance = targetDistance;
        }
        else
        {
            currentCameraDistance = Mathf.SmoothDamp(
                currentCameraDistance,
                distanciaMax,
                ref cameraDistanceVelocity,
                cameraReturnSpeed
            );
        }

        currentCameraDistance = Mathf.Clamp(
            currentCameraDistance,
            distanciaMin,
            distanciaMax
        );

        thirdPersonCamera.transform.position =
            origem + direction * currentCameraDistance;

        // =========================
        // OLHAR
        // =========================

        // Olha na direção em que o Player está olhando.
        // Mirar no pivô puro fazia a câmera girar para dentro quanto mais
        // perto ela chegava da parede: era isso que dava o efeito esquisito.
        Vector3 lookDirection = -direction;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            thirdPersonCamera.transform.rotation =
                Quaternion.LookRotation(lookDirection);
        }
    }

    // =========================
    // TROCA DE CÂMERA
    // =========================

    // Chamado pelo CameraRail quando o filminho começa:
    // desliga as duas câmeras do Player e para de ler o mouse.
    public void DesativarControle()
    {
        controleAtivo = false;

        AplicarEstado();
    }

    // Chamado pelo CameraRail quando o trilho termina:
    // volta para a primeira pessoa e devolve o controle ao jogador.
    public void AtivarControle()
    {
        controleAtivo = true;

        firstPerson = true;

        AplicarEstado();
    }

    // Deixa as câmeras e o cursor coerentes com controleAtivo.
    // Existe para a ordem dos Start() entre CameraRail e CameraController
    // não importar: quem rodar por último chega no mesmo estado.
    private void AplicarEstado()
    {
        if (controleAtivo)
        {
            SetCamera(firstPerson);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            return;
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.gameObject.SetActive(false);
        }

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.gameObject.SetActive(false);
        }

        // No filminho quem filma e a camera cinematica, entao o corpo
        // precisa aparecer mesmo com as cameras do Player desligadas.
        MostrarCorpo(true);
    }

    private void SetCamera(bool useFirstPerson)
    {
        firstPersonCamera.gameObject.SetActive(
            useFirstPerson
        );

        thirdPersonCamera.gameObject.SetActive(
            !useFirstPerson
        );

        // Em primeira pessoa a camera fica dentro do corpo: esconder o
        // modelo evita ver o pescoco e o cabelo por dentro.
        MostrarCorpo(
            !useFirstPerson || !esconderModeloEmPrimeiraPessoa
        );

        if (!useFirstPerson)
        {
            currentCameraDistance = cameraDistance;
            cameraDistanceVelocity = 0f;
        }
    }

    // Liga e desliga so os Renderer, nunca o GameObject:
    // desligar o objeto pararia o Animator e a animacao perderia o estado.
    private void MostrarCorpo(bool visivel)
    {
        if (renderizadoresDoCorpo == null)
            return;

        foreach (Renderer r in renderizadoresDoCorpo)
        {
            if (r != null)
            {
                r.enabled = visivel;
            }
        }
    }
}