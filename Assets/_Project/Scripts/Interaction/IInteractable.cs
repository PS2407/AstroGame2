// IInteractable.cs
// AstroGame 2.0 - Phase 1
// Location: Assets/_Project/Scripts/Interaction/IInteractable.cs
//
// The contract every usable object follows: mission consoles now; doors,
// terminals, mission items and planetary equipment later.
// PlayerInteractor only talks to this interface, so it never needs to know
// what kind of object it is looking at.

using UnityEngine;

namespace AstroGame.Interaction
{
    public interface IInteractable
    {
        /// <summary>Short action text shown in the prompt, e.g. "Access Mission Console".</summary>
        string InteractionPrompt { get; }

        /// <summary>False while the object is busy or unavailable. No prompt is shown then.</summary>
        bool CanInteract(GameObject interactor);

        /// <summary>Called once when the player presses the interact key while targeting this object.</summary>
        void Interact(GameObject interactor);
    }
}
