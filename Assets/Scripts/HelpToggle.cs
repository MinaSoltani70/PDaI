
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Toggles the player instructions when H is pressed.
/// Does not interfere with the mouse cursor or player movement.
/// </summary>
public class HelpToggle : MonoBehaviour
{
    [Header("Help UI")]
    [SerializeField] private GameObject instructionsText;

    private void Start()
    {
        // Hide instructions when the game starts.
        if (instructionsText != null)
            instructionsText.SetActive(false);
    }

    private void Update()
    {
        // Detect the H key using the active input system.
#if ENABLE_INPUT_SYSTEM
        bool hPressed = Keyboard.current != null &&
                        Keyboard.current.hKey.wasPressedThisFrame;
#else
        bool hPressed = Input.GetKeyDown(KeyCode.H);
#endif

        if (!hPressed || instructionsText == null)
            return;

        // Show or hide the instructions.
        instructionsText.SetActive(!instructionsText.activeSelf);
    }
}
