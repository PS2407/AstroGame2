// FirstPersonController.cs
// AstroGame 2.0 - Phase 1
// Location: Assets/_Project/Scripts/Player/FirstPersonController.cs
//
// First-person movement and mouse look for the player.
// Responsibilities (and nothing else):
//   - Walk / sprint with a CharacterController (handles collision with walls)
//   - Gravity, so the player stays on the floor
//   - Mouse look: the body turns left/right, the camera tilts up/down
//   - Lock the cursor while the player is in control; unlock it when control is
//     disabled (other systems, such as the mission briefing UI, call SetInputEnabled)
//
// Input comes from the project's Input System actions asset (InputSystem_Actions),
// assigned in the Inspector. No keys are hardcoded in this script.

using UnityEngine;
using UnityEngine.InputSystem;

namespace AstroGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The child object holding the camera. It tilts up and down for looking.")]
        [SerializeField] private Transform cameraPivot;

        [Tooltip("Optional. If set, the player is moved here when the scene starts.")]
        [SerializeField] private Transform spawnPoint;

        [Header("Input Actions (from InputSystem_Actions > Player)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference sprintAction;

        [Header("Movement (metres per second)")]
        [SerializeField, Min(0f)] private float walkSpeed = 3.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 6f;
        [Tooltip("How quickly the player reaches full speed and stops. Higher = snappier.")]
        [SerializeField, Min(0.1f)] private float acceleration = 15f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -19.6f;
        [Tooltip("Small downward push while grounded so the controller stays on the floor.")]
        [SerializeField] private float groundedStickVelocity = -2f;

        [Header("Mouse Look")]
        [Tooltip("Degrees of rotation per pixel of mouse movement.")]
        [SerializeField, Range(0.01f, 1f)] private float mouseSensitivity = 0.1f;
        [SerializeField, Range(10f, 89f)] private float maxLookAngle = 85f;
        [SerializeField] private bool invertY = false;

        [Header("Cursor")]
        [SerializeField] private bool lockCursorOnStart = true;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _pitch;
        private bool _inputEnabled;

        /// <summary>True while the player can move and look around.</summary>
        public bool InputEnabled => _inputEnabled;

        // Awake runs once when the object is created, before Start.
        // Used here to find components on this object and check the setup.
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (cameraPivot == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null) cameraPivot = childCamera.transform;
            }

            if (cameraPivot == null || moveAction == null || lookAction == null)
            {
                Debug.LogError("[FirstPersonController] Camera Pivot, Move Action and Look Action must be assigned in the Inspector. Controller disabled.", this);
                enabled = false;
            }
        }

        // OnEnable runs every time this component is switched on.
        private void OnEnable()
        {
            moveAction?.action?.Enable();
            lookAction?.action?.Enable();
            sprintAction?.action?.Enable();
        }

        // Start runs once, on the first frame, after every object's Awake.
        private void Start()
        {
            if (spawnPoint != null)
            {
                // The CharacterController overrides position changes while enabled,
                // so switch it off for the move.
                _controller.enabled = false;
                transform.SetPositionAndRotation(spawnPoint.position, Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f));
                _controller.enabled = true;
            }

            _pitch = 0f;
            cameraPivot.localRotation = Quaternion.identity;

            SetInputEnabled(lockCursorOnStart);
        }

        // Update runs once per rendered frame. Input and CharacterController movement belong here.
        private void Update()
        {
            bool paused = Time.timeScale == 0f;

            if (_inputEnabled && !paused)
            {
                HandleLook();
            }

            HandleMovement(_inputEnabled && !paused);
        }

        // Re-lock the cursor when the game window regains focus (for example after Alt+Tab).
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _inputEnabled) ApplyCursorState(true);
        }

        /// <summary>
        /// Gives control to the player (true) or takes it away (false).
        /// When disabled, the player stops, the cursor is unlocked and visible,
        /// and gravity still applies.
        /// </summary>
        public void SetInputEnabled(bool inputEnabled)
        {
            _inputEnabled = inputEnabled;
            if (!inputEnabled) _horizontalVelocity = Vector3.zero;
            ApplyCursorState(inputEnabled);
        }

        private void HandleLook()
        {
            // Mouse delta is already "pixels moved this frame", so it is not multiplied by deltaTime.
            Vector2 look = lookAction.action.ReadValue<Vector2>() * mouseSensitivity;

            // Turn the whole body left/right.
            transform.Rotate(0f, look.x, 0f, Space.Self);

            // Tilt only the camera up/down, clamped so the player cannot flip over.
            _pitch += invertY ? look.y : -look.y;
            _pitch = Mathf.Clamp(_pitch, -maxLookAngle, maxLookAngle);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMovement(bool acceptInput)
        {
            Vector3 targetVelocity = Vector3.zero;

            if (acceptInput)
            {
                Vector2 input = Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f);
                bool sprinting = sprintAction != null && sprintAction.action != null && sprintAction.action.IsPressed();
                float speed = sprinting ? sprintSpeed : walkSpeed;
                targetVelocity = (transform.right * input.x + transform.forward * input.y) * speed;
            }

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = groundedStickVelocity;
            }
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime;
            _controller.Move(motion);
        }

        private static void ApplyCursorState(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // OnValidate runs in the Editor when a value changes in the Inspector.
        private void OnValidate()
        {
            if (sprintSpeed < walkSpeed) sprintSpeed = walkSpeed;
        }
    }
}
