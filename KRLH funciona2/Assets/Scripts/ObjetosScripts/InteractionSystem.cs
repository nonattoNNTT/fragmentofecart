using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionSystem : MonoBehaviour
{
    [Header("Cameras")]
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;

    [Header("Interaction Distance")]
    public float firstPersonDistance = 5f;
    public float thirdPersonDistance = 10f;

    [Header("Interaction UI")]
    public GameObject interactionCanvas;

    private InteractableScene currentInteractable;

    private void Start()
    {
        if (interactionCanvas != null)
        {
            interactionCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        CheckForInteractable();
    }

    // =========================================================
    // VERIFICAR OBJETO
    // =========================================================

    private void CheckForInteractable()
    {
        Camera activeCamera = GetActiveCamera();

        if (activeCamera == null)
        {
            RemoveHighlight();
            HideInteractionUI();
            return;
        }

        float interactionDistance;

        if (activeCamera == thirdPersonCamera)
        {
            interactionDistance = thirdPersonDistance;
        }
        else
        {
            interactionDistance = firstPersonDistance;
        }

        Ray ray = activeCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        // Organiza os objetos do mais próximo
        // para o mais distante
        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        InteractableScene foundInteractable = null;

        foreach (RaycastHit hit in hits)
        {
            // Ignora o próprio Player
            if (hit.transform.root == transform.root)
                continue;

            InteractableScene interactable =
                hit.collider.GetComponent<InteractableScene>();

            // Caso o collider esteja em um filho
            if (interactable == null)
            {
                interactable =
                    hit.collider.GetComponentInParent<InteractableScene>();
            }

            if (interactable != null)
            {
                foundInteractable = interactable;
                break;
            }
        }

        // =====================================================
        // MUDOU DE OBJETO
        // =====================================================

        if (foundInteractable != currentInteractable)
        {
            // Remove destaque do objeto anterior
            if (currentInteractable != null)
            {
                currentInteractable.SetHighlight(false);
            }

            // Coloca destaque no novo objeto
            if (foundInteractable != null)
            {
                foundInteractable.SetHighlight(true);
            }

            currentInteractable = foundInteractable;
        }

        // =====================================================
        // UI
        // =====================================================

        if (currentInteractable != null)
        {
            ShowInteractionUI();
        }
        else
        {
            HideInteractionUI();
        }
    }

    // =========================================================
    // INPUT
    // =========================================================

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    // =========================================================
    // UI
    // =========================================================

    private void ShowInteractionUI()
    {
        if (interactionCanvas != null &&
            !interactionCanvas.activeSelf)
        {
            interactionCanvas.SetActive(true);
        }
    }

    private void HideInteractionUI()
    {
        if (interactionCanvas != null &&
            interactionCanvas.activeSelf)
        {
            interactionCanvas.SetActive(false);
        }
    }

    // =========================================================
    // HIGHLIGHT
    // =========================================================

    private void RemoveHighlight()
    {
        if (currentInteractable != null)
        {
            currentInteractable.SetHighlight(false);
            currentInteractable = null;
        }
    }

    // =========================================================
    // CÂMERA
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
}