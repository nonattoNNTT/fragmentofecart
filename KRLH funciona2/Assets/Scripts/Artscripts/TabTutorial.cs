using UnityEngine;
using UnityEngine.InputSystem;

public class TabTutorial : MonoBehaviour
{
    private PlayerInputActions controls;

    void Awake()
    {
        controls = new PlayerInputActions();

        Debug.Log("TabTutorial: Awake funcionando.");
    }

    void OnEnable()
    {
        controls.Enable();

        controls.Player.Objectives.performed += OnTabPressed;

        Debug.Log("TabTutorial: Input System ativado.");
        Debug.Log("TabTutorial: esperando TAB...");
    }

    void OnDisable()
    {
        controls.Player.Objectives.performed -= OnTabPressed;
        controls.Disable();

        Debug.Log("TabTutorial: desativado.");
    }

    private void OnTabPressed(InputAction.CallbackContext ctx)
    {
        Debug.Log("🔥 TAB FOI DETECTADO PELO TABTUTORIAL!");

        gameObject.SetActive(false);
    }
}