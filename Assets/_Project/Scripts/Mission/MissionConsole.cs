// MissionConsole.cs
// AstroGame 2.0 - Phase 1
// Location: Assets/_Project/Scripts/Mission/MissionConsole.cs
// Attach to: SpaceStation > Interactables > MissionConsole
//
// Responsibilities (and nothing else):
//   - Be an interactable object with its own prompt text
//   - When used, raise the On Activated event (the briefing UI will listen to it)
//   - Refuse further use while it is "in use" (the briefing is open), until Release() is called
// It contains no UI code; the briefing panel is a separate script.

using AstroGame.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace AstroGame.Mission
{
    public class MissionConsole : MonoBehaviour, IInteractable
    {
        [Tooltip("Text shown after the key hint, e.g. \"[E] Access Mission Console\".")]
        [SerializeField] private string promptText = "Access Mission Console";

        [Tooltip("Called when the player uses the console. The mission briefing UI will be connected here.")]
        [SerializeField] private UnityEvent onActivated = new UnityEvent();

        private bool _inUse;

        public string InteractionPrompt => promptText;

        /// <summary>True while the console's screen (the briefing) is open.</summary>
        public bool InUse => _inUse;

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && !_inUse;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            // Only lock the console if something is listening (e.g. the briefing UI),
            // otherwise it would stay locked forever with nothing to release it.
            if (onActivated.GetPersistentEventCount() > 0)
            {
                _inUse = true;
            }

            Debug.Log("[MissionConsole] Console activated.", this);
            onActivated.Invoke();
        }

        /// <summary>Makes the console usable again. Called when the briefing closes.</summary>
        public void Release()
        {
            _inUse = false;
        }
    }
}
