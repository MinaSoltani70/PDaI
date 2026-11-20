using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerUIMode : MonoBehaviour
{
    [SerializeField] private bool blockMovement = true;
    [SerializeField] private Behaviour[] disableWhileInUI = new Behaviour[0];

    private StarterAssetsInputs inputs;
#if ENABLE_INPUT_SYSTEM
    private PlayerInput playerInput;
#endif
    private int openMenus;

    public bool InUI => openMenus > 0;

    private void Awake()
    {
        inputs = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
        playerInput = GetComponent<PlayerInput>();
#endif
    }

    public void EnterUI()
    {
        openMenus++;
        if (openMenus == 1) Apply(true);
    }

    public void ExitUI()
    {
        if (openMenus == 0) return;
        openMenus--;
        if (openMenus == 0) Apply(false);
    }

    private void Apply(bool ui)
    {
        if (inputs != null)
        {
            inputs.cursorLocked = !ui;
            inputs.cursorInputForLook = !ui;
            inputs.look = Vector2.zero;
            if (ui && blockMovement)
            {
                inputs.move = Vector2.zero;
                inputs.jump = false;
                inputs.sprint = false;
            }
        }

#if ENABLE_INPUT_SYSTEM
        if (playerInput != null && blockMovement)
        {
            if (ui) playerInput.DeactivateInput();
            else playerInput.ActivateInput();
        }
#endif

        foreach (var behaviour in disableWhileInUI)
            if (behaviour != null) behaviour.enabled = !ui;

        Cursor.lockState = ui ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = ui;
    }
}