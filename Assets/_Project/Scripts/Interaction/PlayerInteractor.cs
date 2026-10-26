// PlayerInteractor.cs
// AstroGame 2.0 - Phase 1
// Location: Assets/_Project/Scripts/Interaction/PlayerInteractor.cs
// Attach to: the Player GameObject.
//
// Responsibilities (and nothing else):
//   - Each frame, cast a ray from the camera to find an IInteractable within reach
//   - Announce when the targeted object changes (the prompt UI listens to this)
//   - Call Interact() on the target when the Interact action is pressed
// It does not know what any object does when used; that lives in the object itself.

using System;
using AstroGame.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AstroGame.Interaction
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Where the ray starts and which way it points. Use the player's camera.")]
        [SerializeField] private Transform viewPoint;

        [Tooltip("Optional. When set, interaction is blocked while this controller's input is disabled (e.g. a UI panel is open).")]
        [SerializeField] private FirstPersonController playerController;

        [Header("Input Action (from InputSystem_Actions > Player)")]
        [SerializeField] private InputActionReference interactAction;

        [Header("Detection")]
        [Tooltip("Maximum reach in metres.")]
        [SerializeField, Min(0.1f)] private float interactRange = 2.5f;

        [Tooltip("Layers the ray can hit. Walls must be included so the player cannot interact through them.")]
        [SerializeField] private LayerMask detectionLayers = ~0;

        [Header("Debug")]
        [SerializeField] private bool logInteractions = true;

        private IInteractable _currentTarget;

        /// <summary>Raised whenever the targeted object changes. The argument is null when nothing is targeted.</summary>
        public event Action<IInteractable> TargetChanged;

        /// <summary>The object currently under the crosshair and available, or null.</summary>
        public IInteractable CurrentTarget => _currentTarget;

        private void Awake()
        {
            if (viewPoint == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null) viewPoint = childCamera.transform;
            }

            if (playerController == null)
            {
                playerController = GetComponent<FirstPersonController>();
            }

            if (viewPoint == null || interactAction == null)
            {
                Debug.LogError("[PlayerInteractor] View Point and Interact Action must be assigned in the Inspector. Interactor disabled.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            interactAction?.action?.Enable();
        }

        private void OnDisable()
        {
            SetTarget(null);
        }

        private void Update()
        {
            bool playerHasControl = (playerController == null || playerController.InputEnabled) && Time.timeScale > 0f;

            SetTarget(playerHasControl ? FindTarget() : null);

            // WasPressedThisFrame is true for exactly one frame per key press,
            // so holding E cannot trigger the object repeatedly.
            if (_currentTarget != null && interactAction.action.WasPressedThisFrame())
            {
                IInteractable target = _currentTarget;
                if (logInteractions)
                {
                    Debug.Log($"[PlayerInteractor] Interacting with \"{target.InteractionPrompt}\"", target as UnityEngine.Object);
                }
                target.Interact(gameObject);
            }
        }

        private IInteractable FindTarget()
        {
            Ray ray = new Ray(viewPoint.position, viewPoint.forward);

            // The ray stops at the first solid thing it hits (a wall blocks a console behind it).
            if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, detectionLayers, QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            // The collider may be on a child (e.g. the console's Desk), so search upward.
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract(gameObject))
            {
                return null;
            }

            return interactable;
        }

        private void SetTarget(IInteractable newTarget)
        {
            if (ReferenceEquals(newTarget, _currentTarget)) return;
            _currentTarget = newTarget;
            TargetChanged?.Invoke(_currentTarget);
        }

        // Draws the reach line in the Scene view when the Player is selected.
        private void OnDrawGizmosSelected()
        {
            if (viewPoint == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(viewPoint.position, viewPoint.position + viewPoint.forward * interactRange);
        }
    }
}
