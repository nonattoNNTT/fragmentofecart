using UnityEngine;
using UnityEngine.InputSystem;

public class ControlsPanel : MonoBehaviour
{
    [Header("Controls")]
    public GameObject controlsPanel;

    [Header("UI que deve sumir enquanto o TAB estiver pressionado")]
    public GameObject[] panelsToHide;

    private void Start()
    {
        controlsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnControls(InputAction.CallbackContext context)
    {
        // TAB pressionado
        if (context.started)
        {
            controlsPanel.SetActive(true);

            SetPanelsActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // TAB solto
        if (context.canceled)
        {
            controlsPanel.SetActive(false);

            SetPanelsActive(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SetPanelsActive(bool active)
    {
        foreach (GameObject panel in panelsToHide)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}