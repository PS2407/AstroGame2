// MissionBriefingUI.cs
// AstroGame 2.0 - Phase 1
// Location: Assets/_Project/Scripts/UI/MissionBriefingUI.cs
// Attach to: HUD > MissionBriefing (created by AstroGame > Build Mission Briefing UI).
//            This object stays active; its child "Panel" is what is shown and hidden.
//
// Responsibilities (and nothing else):
//   - Open: fill the panel from a MissionData asset, show it, pause player control
//   - Start Mission: mark the mission accepted and show a placeholder confirmation
//   - Close (button, Acknowledge button or Esc): hide the panel, give control back
//   - Raise events so other objects can react (the console is released On Closed)
// It does not know about consoles or planets; those are connected through events.

using System.Collections.Generic;
using System.Text;
using AstroGame.Mission;
using AstroGame.Player;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AstroGame.UI
{
    /// <summary>A UnityEvent that passes the accepted mission to its listeners.</summary>
    [System.Serializable]
    public class MissionEvent : UnityEvent<MissionData> { }

    public class MissionBriefingUI : MonoBehaviour
    {
        [Header("Mission")]
        [SerializeField] private MissionData mission;

        [Header("Player")]
        [Tooltip("Movement and look are paused while the briefing is open.")]
        [SerializeField] private FirstPersonController playerController;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject briefingView;
        [SerializeField] private GameObject confirmationView;

        [Header("Briefing Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text targetPlanetText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text objectivesText;
        [SerializeField] private TMP_Text resourcesText;

        [Header("Confirmation")]
        [SerializeField] private TMP_Text confirmationText;
        [Tooltip("{0} = mission title, {1} = target planet.")]
        [SerializeField, TextArea(2, 5)] private string confirmationFormat =
            "{0} has been logged as ACCEPTED.\n\nLaunch to {1} is offline in this prototype.\nPlanetary deployment arrives in Phase 2.";

        [Header("Buttons")]
        [SerializeField] private Button startMissionButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button acknowledgeButton;
        [SerializeField] private string acceptedButtonLabel = "MISSION ACCEPTED";

        [Header("Input (from InputSystem_Actions > UI)")]
        [Tooltip("Closes the briefing. Use UI/Cancel (Esc).")]
        [SerializeField] private InputActionReference cancelAction;

        [Header("Events")]
        [SerializeField] private UnityEvent onOpened = new UnityEvent();
        [SerializeField] private MissionEvent onMissionAccepted = new MissionEvent();
        [Tooltip("Connect MissionConsole.Release here so the console can be used again.")]
        [SerializeField] private UnityEvent onClosed = new UnityEvent();

        private bool _isOpen;
        private TMP_Text _startButtonLabel;
        private string _startButtonDefaultLabel;

        public bool IsOpen => _isOpen;
        public bool MissionAccepted { get; private set; }

        // Awake: check the setup, hide the panel, connect the buttons.
        private void Awake()
        {
            if (panelRoot == null || briefingView == null || confirmationView == null ||
                startMissionButton == null || closeButton == null || acknowledgeButton == null)
            {
                Debug.LogError("[MissionBriefingUI] Panel, views and buttons must be assigned. Rebuild with AstroGame > Build Mission Briefing UI.", this);
                enabled = false;
                return;
            }

            if (panelRoot == gameObject)
            {
                Debug.LogError("[MissionBriefingUI] Panel Root must be a child object, not the object this script is on.", this);
                enabled = false;
                return;
            }

            if (playerController == null)
            {
                Debug.LogWarning("[MissionBriefingUI] Player Controller is not assigned; the player will keep moving while the briefing is open.", this);
            }

            _startButtonLabel = startMissionButton.GetComponentInChildren<TMP_Text>();
            if (_startButtonLabel != null) _startButtonDefaultLabel = _startButtonLabel.text;

            startMissionButton.onClick.AddListener(StartMission);
            closeButton.onClick.AddListener(Close);
            acknowledgeButton.onClick.AddListener(Close);

            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            cancelAction?.action?.Enable();
        }

        private void OnDestroy()
        {
            if (startMissionButton != null) startMissionButton.onClick.RemoveListener(StartMission);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (acknowledgeButton != null) acknowledgeButton.onClick.RemoveListener(Close);
        }

        private void Update()
        {
            if (_isOpen && cancelAction != null && cancelAction.action != null && cancelAction.action.WasPressedThisFrame())
            {
                Close();
            }
        }

        /// <summary>Shows the briefing. Connect this to MissionConsole's On Activated event.</summary>
        public void Open()
        {
            if (!enabled || _isOpen) return;

            Populate();
            briefingView.SetActive(true);
            confirmationView.SetActive(false);
            panelRoot.SetActive(true);
            _isOpen = true;

            if (playerController != null) playerController.SetInputEnabled(false);

            // Clear any leftover keyboard selection so no button looks pre-selected.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            Debug.Log("[MissionBriefingUI] Briefing opened.", this);
            onOpened.Invoke();
        }

        /// <summary>Hides the briefing and gives control back to the player.</summary>
        public void Close()
        {
            if (!_isOpen) return;

            panelRoot.SetActive(false);
            _isOpen = false;

            if (playerController != null) playerController.SetInputEnabled(true);

            Debug.Log("[MissionBriefingUI] Briefing closed.", this);
            onClosed.Invoke();
        }

        // Phase 1 placeholder: no planet is loaded. The mission is marked accepted
        // and a confirmation is shown. Phase 2 will connect a launch sequence to On Mission Accepted.
        private void StartMission()
        {
            if (!_isOpen || mission == null) return;

            MissionAccepted = true;

            if (confirmationText != null)
            {
                confirmationText.text = string.Format(confirmationFormat, mission.Title, mission.TargetPlanet);
            }
            briefingView.SetActive(false);
            confirmationView.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            Debug.Log($"[MissionBriefingUI] Mission accepted: {mission.MissionId} \"{mission.Title}\" -> {mission.TargetPlanet}", this);
            onMissionAccepted.Invoke(mission);
        }

        private void Populate()
        {
            if (mission == null)
            {
                Debug.LogWarning("[MissionBriefingUI] No Mission Data assigned.", this);
                SetText(titleText, "NO MISSION ASSIGNED");
                SetText(targetPlanetText, "");
                SetText(descriptionText, "Assign a Mission Data asset to the MissionBriefing object.");
                SetText(objectivesText, "");
                SetText(resourcesText, "");
                startMissionButton.interactable = false;
                return;
            }

            SetText(titleText, $"{mission.MissionId}  //  {mission.Title}");
            SetText(targetPlanetText, $"TARGET PLANET:  {mission.TargetPlanet}");
            SetText(descriptionText, mission.Description);
            SetText(objectivesText, BuildList(mission.PrimaryObjectives, numbered: true));
            SetText(resourcesText, BuildList(mission.ResourcesAndConstraints, numbered: false));

            startMissionButton.interactable = !MissionAccepted;
            if (_startButtonLabel != null)
            {
                _startButtonLabel.text = MissionAccepted ? acceptedButtonLabel : _startButtonDefaultLabel;
            }
        }

        private static string BuildList(IReadOnlyList<string> items, bool numbered)
        {
            if (items == null || items.Count == 0) return "-";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) builder.Append('\n');
                builder.Append(numbered ? $"{i + 1}.  " : "-  ");
                builder.Append(items[i]);
            }
            return builder.ToString();
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }
    }
}
