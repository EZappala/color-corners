using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private InputAction continue_action;

    private void Awake()
    {
        continue_action = InputSystem.actions.FindAction("Continue");
        if (continue_action == null)
        {
            Debug.LogError("No continue action found", this);
            return;
        }
        continue_action.performed += OnContinuePerformed;
    }

    private void OnContinuePerformed(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene("Main");
    }

    private void OnEnable()
    {
        continue_action.Enable();
    }

    private void OnDisable()
    {
        continue_action.performed -= OnContinuePerformed;
        continue_action.Disable();
    }
}
