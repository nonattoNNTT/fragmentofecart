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

    [Header("Camera Collision")]
    public float collisionRadius = 0.3f;
    public float collisionOffset = 0.15f;
    public float cameraReturnSpeed = 0.08f;
    public LayerMask collisionLayers;

    private Vector2 lookInput;

    private float verticalRotation = 0f;

    private bool firstPerson = true;

    private float currentCameraDistance;
    private float cameraDistanceVelocity;

    private void Start()
    {
        currentCameraDistance = cameraDistance;

        SetCamera(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        RotateCamera();
    }

    private void LateUpdate()
    {
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

        Vector3 pivotPosition =
            cameraPivot.position +
            Vector3.up * cameraHeight;

        Vector3 direction =
            -cameraPivot.forward.normalized;

        float targetDistance = cameraDistance;

        // Detecta paredes e obstáculos
        if (Physics.SphereCast(
            pivotPosition,
            collisionRadius,
            direction,
            out RaycastHit hit,
            cameraDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            targetDistance = hit.distance - collisionOffset;

            targetDistance = Mathf.Clamp(
                targetDistance,
                0.15f,
                cameraDistance
            );

            // Quando existe uma parede,
            // a câmera vai imediatamente para uma distância segura.
            currentCameraDistance = Mathf.Min(
                currentCameraDistance,
                targetDistance
            );
        }
        else
        {
            // Sem parede:
            // volta suavemente para a distância normal.
            currentCameraDistance = Mathf.SmoothDamp(
                currentCameraDistance,
                cameraDistance,
                ref cameraDistanceVelocity,
                cameraReturnSpeed
            );
        }

        // Garante que nunca passe da distância permitida
        currentCameraDistance = Mathf.Clamp(
            currentCameraDistance,
            0.15f,
            cameraDistance
        );

        // Posição final da câmera
        Vector3 cameraPosition =
            pivotPosition +
            direction * currentCameraDistance;

        thirdPersonCamera.transform.position =
            cameraPosition;

        // Faz a câmera olhar para o Pivot
        Vector3 lookDirection =
            cameraPivot.position -
            thirdPersonCamera.transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            thirdPersonCamera.transform.rotation =
                Quaternion.LookRotation(lookDirection);
        }
    }

    // =========================
    // TROCA DE CÂMERA
    // =========================

    private void SetCamera(bool useFirstPerson)
    {
        firstPersonCamera.gameObject.SetActive(
            useFirstPerson
        );

        thirdPersonCamera.gameObject.SetActive(
            !useFirstPerson
        );

        if (!useFirstPerson)
        {
            currentCameraDistance = cameraDistance;
            cameraDistanceVelocity = 0f;
        }
    }
}