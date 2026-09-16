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

    private Vector3 originalPosition;
    private float bobTimer;

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
        UpdateFOV();
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