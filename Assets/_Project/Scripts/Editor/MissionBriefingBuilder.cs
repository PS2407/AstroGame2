// MissionBriefingBuilder.cs
// AstroGame 2.0 - Phase 1
// Location: Assets/_Project/Scripts/Editor/MissionBriefingBuilder.cs
//
// Editor-only tool. Adds the menu item "AstroGame > Build Mission Briefing UI".
// It creates, inside the HUD canvas:
//   MissionBriefing (MissionBriefingUI, always active)
//     Panel (dark backdrop, shown/hidden)
//       Window
//         BriefingView      - branding, title, planet, description, objectives, resources, buttons
//         ConfirmationView  - "MISSION ACCEPTED" placeholder and an Acknowledge button
// It also creates a placeholder mission asset at Assets/_Project/Data/Missions,
// and fills in every UI reference on MissionBriefingUI.
// Left for you to connect in the Inspector: Player Controller and the two events.
// Running it again replaces the previous MissionBriefing (those connections must then be redone).

using System.Linq;
using AstroGame.Mission;
using AstroGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AstroGame.EditorTools
{
    public static class MissionBriefingBuilder
    {
        private const string HudName = "HUD";
        private const string RootName = "MissionBriefing";
        private const string MissionFolder = "Assets/_Project/Data/Missions";
        private const string MissionAssetPath = MissionFolder + "/Mission_01_FirstLight.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        // Window size in reference pixels (the HUD reference resolution is 1920 x 1080)
        private const float WindowWidth = 1200f;
        private const float WindowHeight = 760f;
        private const float Margin = 48f;

        private static readonly Color Cyan = new Color(0.45f, 0.88f, 1f);
        private static readonly Color Amber = new Color(1f, 0.72f, 0.3f);
        private static readonly Color TextMain = new Color(0.88f, 0.93f, 0.97f);
        private static readonly Color TextDim = new Color(0.55f, 0.65f, 0.72f);
        private static readonly Color WindowColor = new Color(0.035f, 0.06f, 0.085f, 0.96f);
        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.7f);

        private static TMP_FontAsset _font;

        [MenuItem("AstroGame/Build Mission Briefing UI")]
        public static void Build()
        {
            GameObject hud = GameObject.Find(HudName);
            if (hud == null || hud.GetComponent<Canvas>() == null)
            {
                EditorUtility.DisplayDialog("Mission Briefing Builder",
                    "No canvas named \"HUD\" was found in the open scene. Open the SpaceStation scene first.", "OK");
                return;
            }

            _font = TMP_Settings.defaultFontAsset;
            if (_font == null)
            {
                EditorUtility.DisplayDialog("Mission Briefing Builder",
                    "TextMeshPro has no default font. Import it with Window > TextMeshPro > Import TMP Essential Resources, then try again.", "OK");
                return;
            }

            Transform existing = hud.transform.Find(RootName);
            if (existing != null)
            {
                bool rebuild = EditorUtility.DisplayDialog("Mission Briefing Builder",
                    "A MissionBriefing already exists under HUD.\n\nReplace it? Its Inspector connections (Player Controller and events) will need to be redone.",
                    "Replace", "Cancel");
                if (!rebuild) return;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            MissionData mission = GetOrCreateMission();

            // Root: always active, holds the script
            GameObject root = NewUI(RootName, hud.transform);
            Stretch(root.GetComponent<RectTransform>());
            MissionBriefingUI ui = root.AddComponent<MissionBriefingUI>();

            // Panel: full-screen dark backdrop that also blocks clicks to anything behind it
            GameObject panel = NewUI("Panel", root.transform);
            Stretch(panel.GetComponent<RectTransform>());
            panel.AddComponent<Image>().color = BackdropColor;

            // Window
            GameObject window = NewUI("Window", panel.transform);
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(WindowWidth, WindowHeight);
            window.AddComponent<Image>().color = WindowColor;
            Outline windowOutline = window.AddComponent<Outline>();
            windowOutline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f);
            windowOutline.effectDistance = new Vector2(2f, -2f);
            Block(window.transform, "AccentBar", 0f, 0f, WindowWidth, 4f, Cyan);

            float contentWidth = WindowWidth - 2f * Margin;
            float columnWidth = (contentWidth - 44f) / 2f;
            float rightColumnX = Margin + columnWidth + 44f;

            // ---------------- Briefing view ----------------
            GameObject briefing = NewUI("BriefingView", window.transform);
            Stretch(briefing.GetComponent<RectTransform>());
            Transform b = briefing.transform;

            Label(b, "Brand", "ASTROGAME 2.0   //   MERIDIAN STATION MISSION CONTROL", Margin, 32f, contentWidth, 28f, 18f, Cyan, FontStyles.Bold, TextAlignmentOptions.TopLeft, 4f);
            Label(b, "Heading_Briefing", "MISSION BRIEFING", Margin, 74f, contentWidth, 24f, 16f, TextDim, FontStyles.Normal, TextAlignmentOptions.TopLeft, 6f);
            TextMeshProUGUI titleText = Label(b, "Title", "Mission title", Margin, 100f, contentWidth, 56f, 42f, TextMain, FontStyles.Bold);
            TextMeshProUGUI planetText = Label(b, "TargetPlanet", "TARGET PLANET:  -", Margin, 162f, contentWidth, 30f, 22f, Amber, FontStyles.Bold, TextAlignmentOptions.TopLeft, 2f);
            Block(b, "Divider", Margin, 206f, contentWidth, 1f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f));
            TextMeshProUGUI descriptionText = Label(b, "Description", "Mission description", Margin, 224f, contentWidth, 130f, 22f, TextMain);

            Label(b, "Heading_Objectives", "PRIMARY OBJECTIVES", Margin, 374f, columnWidth, 24f, 16f, Cyan, FontStyles.Bold, TextAlignmentOptions.TopLeft, 4f);
            TextMeshProUGUI objectivesText = Label(b, "Objectives", "1.  Objective", Margin, 406f, columnWidth, 220f, 20f, TextMain);
            Label(b, "Heading_Resources", "RESOURCES & CONSTRAINTS", rightColumnX, 374f, columnWidth, 24f, 16f, Cyan, FontStyles.Bold, TextAlignmentOptions.TopLeft, 4f);
            TextMeshProUGUI resourcesText = Label(b, "Resources", "-  Resource", rightColumnX, 406f, columnWidth, 220f, 20f, TextMain);

            const float buttonY = WindowHeight - Margin - 56f;
            Button startButton = MakeButton(b, "Button_StartMission", "START MISSION", WindowWidth - Margin - 280f, buttonY, 280f, 56f, true);
            Button closeButton = MakeButton(b, "Button_Close", "CLOSE", WindowWidth - Margin - 280f - 24f - 200f, buttonY, 200f, 56f, false);
            Label(b, "Hint", "Esc  to close", Margin, buttonY + 16f, 300f, 28f, 16f, TextDim);

            // ---------------- Confirmation view ----------------
            GameObject confirmation = NewUI("ConfirmationView", window.transform);
            Stretch(confirmation.GetComponent<RectTransform>());
            Transform c = confirmation.transform;

            Label(c, "Brand", "ASTROGAME 2.0   //   MERIDIAN STATION MISSION CONTROL", Margin, 32f, contentWidth, 28f, 18f, Cyan, FontStyles.Bold, TextAlignmentOptions.TopLeft, 4f);
            Label(c, "Heading", "MISSION ACCEPTED", Margin, 230f, contentWidth, 70f, 52f, Cyan, FontStyles.Bold, TextAlignmentOptions.Top, 8f);
            TextMeshProUGUI confirmationText = Label(c, "Message", "Confirmation message", Margin + 100f, 320f, contentWidth - 200f, 180f, 24f, TextMain, FontStyles.Normal, TextAlignmentOptions.Top);
            Button acknowledgeButton = MakeButton(c, "Button_Acknowledge", "ACKNOWLEDGE", (WindowWidth - 280f) / 2f, 540f, 280f, 56f, true);
            confirmation.SetActive(false);

            panel.SetActive(false);

            // ---------------- Fill in MissionBriefingUI's references ----------------
            SerializedObject so = new SerializedObject(ui);
            SetRef(so, "mission", mission);
            SetRef(so, "panelRoot", panel);
            SetRef(so, "briefingView", briefing);
            SetRef(so, "confirmationView", confirmation);
            SetRef(so, "titleText", titleText);
            SetRef(so, "targetPlanetText", planetText);
            SetRef(so, "descriptionText", descriptionText);
            SetRef(so, "objectivesText", objectivesText);
            SetRef(so, "resourcesText", resourcesText);
            SetRef(so, "confirmationText", confirmationText);
            SetRef(so, "startMissionButton", startButton);
            SetRef(so, "closeButton", closeButton);
            SetRef(so, "acknowledgeButton", acknowledgeButton);

            InputActionReference cancel = FindActionReference("UI", "Cancel");
            if (cancel != null) SetRef(so, "cancelAction", cancel);
            else Debug.LogWarning("[MissionBriefingBuilder] UI/Cancel action not found. Assign Cancel Action on MissionBriefing manually.");

            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Build Mission Briefing UI");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log("[MissionBriefingBuilder] Mission briefing UI built. Connect Player Controller and the events in the Inspector, then press Ctrl+S.");
        }

        // ------------------------------------------------------------------
        // Placeholder mission
        // ------------------------------------------------------------------

        private static MissionData GetOrCreateMission()
        {
            MissionData mission = AssetDatabase.LoadAssetAtPath<MissionData>(MissionAssetPath);
            if (mission != null) return mission;

            EnsureFolder(MissionFolder);
            mission = ScriptableObject.CreateInstance<MissionData>();
            AssetDatabase.CreateAsset(mission, MissionAssetPath);

            SerializedObject so = new SerializedObject(mission);
            so.FindProperty("missionId").stringValue = "M01";
            so.FindProperty("title").stringValue = "Operation First Light";
            so.FindProperty("targetPlanet").stringValue = "Kepler-7b";
            so.FindProperty("description").stringValue =
                "Long-range scans have detected an unexplained energy signature beneath the dust plains of Kepler-7b. " +
                "Land at the survey site, restore the relay network and recover the signal's data core before the storm front reaches the landing zone.";
            SetArray(so.FindProperty("primaryObjectives"), new[]
            {
                "Restore power to the three relay beacons.",
                "Route power through the grid without overloading any node.",
                "Recover the data core and return to Meridian Station."
            });
            SetArray(so.FindProperty("resourcesAndConstraints"), new[]
            {
                "Rover battery: 100 units",
                "Spare relay modules: 2",
                "Mission window: 20 minutes",
                "Storm front arrives at T+15 minutes"
            });
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(mission);
            AssetDatabase.SaveAssets();
            return mission;
        }

        private static void SetArray(SerializedProperty property, string[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        // ------------------------------------------------------------------
        // UI helpers
        // ------------------------------------------------------------------

        private static GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Positions a rectangle by its top-left corner, measured from the parent's top-left corner.
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Block(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            GameObject go = NewUI(name, parent);
            Place(go.GetComponent<RectTransform>(), x, y, width, height);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, float x, float y, float width, float height,
            float fontSize, Color color, FontStyles style = FontStyles.Normal,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft, float characterSpacing = 0f)
        {
            GameObject go = NewUI(name, parent);
            Place(go.GetComponent<RectTransform>(), x, y, width, height);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.fontStyle = style;
            label.alignment = alignment;
            label.characterSpacing = characterSpacing;
            label.raycastTarget = false;
            return label;
        }

        private static Button MakeButton(Transform parent, string name, string text, float x, float y, float width, float height, bool primary)
        {
            GameObject go = NewUI(name, parent);
            Place(go.GetComponent<RectTransform>(), x, y, width, height);

            Image image = go.AddComponent<Image>();
            image.color = Color.white; // tinted by the button's colour states below

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            Color normal = primary ? new Color(0.09f, 0.40f, 0.50f) : new Color(0.10f, 0.14f, 0.18f);
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Brighten(normal, 1.45f);
            colors.pressedColor = Brighten(normal, 0.75f);
            colors.selectedColor = normal;
            colors.disabledColor = new Color(0.16f, 0.19f, 0.22f, 0.8f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, primary ? 0.9f : 0.4f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject labelObject = NewUI("Label", go.transform);
            Stretch(labelObject.GetComponent<RectTransform>());
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.text = text;
            label.fontSize = 20f;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 3f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            return button;
        }

        private static Color Brighten(Color color, float factor)
        {
            return new Color(Mathf.Clamp01(color.r * factor), Mathf.Clamp01(color.g * factor), Mathf.Clamp01(color.b * factor), 1f);
        }

        // ------------------------------------------------------------------
        // Asset helpers
        // ------------------------------------------------------------------

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[MissionBriefingBuilder] Field \"{propertyName}\" not found on MissionBriefingUI.");
                return;
            }
            property.objectReferenceValue = value;
        }

        private static InputActionReference FindActionReference(string mapName, string actionName)
        {
            string fullName = mapName + "/" + actionName;
            return AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(r => r.name == fullName ||
                                     (r.action != null && r.action.name == actionName &&
                                      r.action.actionMap != null && r.action.actionMap.name == mapName));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
