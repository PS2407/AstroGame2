// InteractionPromptUI.cs
// AstroGame 2.0 - Phase 1
// Location: Assets/_Project/Scripts/UI/InteractionPromptUI.cs
// Attach to: the HUD Canvas (NOT to the prompt panel itself, because the panel
//            gets switched off and a switched-off object stops listening).
//
// Responsibilities (and nothing else):
//   - Listen to PlayerInteractor.TargetChanged
//   - Show "[key] action" while something is targeted; hide it otherwise
// The key label is read from the Input System binding, so it stays correct
// if the Interact key is ever remapped.

using AstroGame.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AstroGame.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractor interactor;

        [Tooltip("The prompt panel that is shown and hidden.")]
        [SerializeField] private GameObject promptRoot;

        [Tooltip("The text inside the prompt panel.")]
        [SerializeField] private TMP_Text promptLabel;

        [Tooltip("The same Interact action the PlayerInteractor uses. Only used to display the key name.")]
        [SerializeField] private InputActionReference interactAction;

        [Header("Format")]
        [Tooltip("{0} = key name, {1} = action text from the targeted object.")]
        [SerializeField] private string format = "[{0}]  {1}";

        [Tooltip("Shown if the key name cannot be read from the input binding.")]
        [SerializeField] private string fallbackKeyName = "E";

        private void Awake()
        {
            if (interactor == null || promptRoot == null || promptLabel == null)
            {
                Debug.LogError("[InteractionPromptUI] Interactor, Prompt Root and Prompt Label must be assigned in the Inspector.", this);
                enabled = false;
                return;
            }

            if (promptRoot == gameObject)
            {
                Debug.LogError("[InteractionPromptUI] Prompt Root must be a child object, not the object this script is on.", this);
                enabled = false;
            }
        }

        // Subscribe when enabled, unsubscribe when disabled, so no listener is left behind.
        private void OnEnable()
        {
            if (interactor == null) return;
            interactor.TargetChanged += HandleTargetChanged;
            HandleTargetChanged(interactor.CurrentTarget);
        }

        private void OnDisable()
        {
            if (interactor != null) interactor.TargetChanged -= HandleTargetChanged;
        }

        private void HandleTargetChanged(IInteractable target)
        {
            bool show = target != null;
            if (show)
            {
                promptLabel.text = string.Format(format, GetKeyName(), target.InteractionPrompt);
            }
            promptRoot.SetActive(show);
        }

        private string GetKeyName()
        {
            if (interactAction != null && interactAction.action != null)
            {
                // Prefer the keyboard binding (e.g. "E") over gamepad bindings.
                // DontIncludeInteractions: show "E", not "Hold E", even if the binding has a Hold interaction.
                const InputBinding.DisplayStringOptions options = InputBinding.DisplayStringOptions.DontIncludeInteractions;

                string keyboard = interactAction.action.GetBindingDisplayString(InputBinding.MaskByGroup("Keyboard&Mouse"), options);
                if (!string.IsNullOrEmpty(keyboard)) return keyboard;

                string any = interactAction.action.GetBindingDisplayString(options);
                if (!string.IsNullOrEmpty(any)) return any;
            }
            return fallbackKeyName;
        }
    }
}
