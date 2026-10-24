// StationBuilder.cs
// AstroGame 2.0 - Phase 1
// Location: Assets/_Project/Scripts/Editor/StationBuilder.cs
//
// Editor-only tool. Adds the menu item "AstroGame > Build Space Station".
// It builds the Phase 1 station (docking area, corridor, mission control,
// observation window, mission console, lights, starfield, planet) as real,
// editable GameObjects in the currently open scene.
// Materials are saved as assets in Assets/_Project/Art/Materials/Station.
// Running it again replaces the previous "SpaceStation" object.
// Units: 1 Unity unit = 1 metre. The player walks along +Z.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AstroGame.EditorTools
{
    public static class StationBuilder
    {
        private const string RootName = "SpaceStation";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Station";

        // Shared dimensions (metres)
        private const float Wall = 0.2f;          // wall thickness
        private const float SmallRoomHeight = 3.5f; // docking area + corridor
        private const float ControlRoomHeight = 5f; // mission control
        private const float DoorWidth = 2f;
        private const float DoorHeight = 2.5f;

        // Materials, created or refreshed on every build
        private static Material _floor, _wall, _ceiling, _trim, _console;
        private static Material _glowCyan, _glowAmber, _lightPanel, _glass, _planet, _star;

        [MenuItem("AstroGame/Build Space Station")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "SpaceStation")
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "AstroGame Station Builder",
                    $"The open scene is \"{scene.name}\", not \"SpaceStation\".\n\nBuild the station in this scene anyway?",
                    "Build anyway", "Cancel");
                if (!proceed) return;
            }

            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                bool rebuild = EditorUtility.DisplayDialog(
                    "AstroGame Station Builder",
                    "A SpaceStation already exists in this scene.\n\nDelete it and build a fresh one? Manual changes to it will be lost (you can undo with Ctrl+Z).",
                    "Rebuild", "Cancel");
                if (!rebuild) return;
                Undo.DestroyObjectImmediate(existing);
            }

            if (!CreateMaterials()) return;

            GameObject root = new GameObject(RootName);
            Transform environment = Group("Environment", root.transform);
            Transform lighting = Group("Lighting", root.transform);
            Transform interactables = Group("Interactables", root.transform);
            Transform space = Group("Space", root.transform);

            BuildDockingArea(Group("DockingArea", environment));
            BuildCorridor(Group("Corridor", environment));
            BuildMissionControl(Group("MissionControl", environment));
            BuildMissionConsole(interactables);
            BuildLights(lighting);
            BuildSpace(space);
            BuildPlayerSpawn(root.transform);

            ConfigureEnvironmentAndCamera();

            Undo.RegisterCreatedObjectUndo(root, "Build Space Station");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[StationBuilder] Space station built. Press Ctrl+S to save the scene.");
        }

        // ------------------------------------------------------------------
        // Rooms
        // ------------------------------------------------------------------

        // Docking area: x -4..4, z 0..8, height 3.5. Player spawns here.
        private static void BuildDockingArea(Transform parent)
        {
            const float h = SmallRoomHeight;
            Box("Floor", parent, new Vector3(0f, -0.1f, 4f), new Vector3(8f + 2 * Wall, 0.2f, 8f + 2 * Wall), _floor);
            Box("Ceiling", parent, new Vector3(0f, h + 0.1f, 4f), new Vector3(8f + 2 * Wall, 0.2f, 8f + 2 * Wall), _ceiling);

            Box("Wall_South", parent, new Vector3(0f, h / 2f, -0.1f), new Vector3(8f + 2 * Wall, h, Wall), _wall);
            Box("Wall_West", parent, new Vector3(-4.1f, h / 2f, 4f), new Vector3(Wall, h, 8f), _wall);
            Box("Wall_East", parent, new Vector3(4.1f, h / 2f, 4f), new Vector3(Wall, h, 8f), _wall);

            // North wall with a 3 m opening into the corridor
            Box("Wall_North_Left", parent, new Vector3(-2.85f, h / 2f, 8.1f), new Vector3(2.7f, h, Wall), _wall);
            Box("Wall_North_Right", parent, new Vector3(2.85f, h / 2f, 8.1f), new Vector3(2.7f, h, Wall), _wall);
            DoorFrame("Doorway_DockToCorridor", parent, 8.1f, h, -1f);

            // Airlock hatch on the south wall (decoration)
            Box("AirlockHatch", parent, new Vector3(0f, 1.4f, 0.02f), new Vector3(2.4f, 2.8f, 0.04f), _trim, false);
            Box("AirlockHatch_Light", parent, new Vector3(0f, 2.95f, 0.03f), new Vector3(2.4f, 0.08f, 0.04f), _glowAmber, false);

            // Cargo crates (decoration with collision)
            Box("Crate_A", parent, new Vector3(-3.0f, 0.4f, 6.6f), new Vector3(0.8f, 0.8f, 0.8f), _trim);
            Box("Crate_B", parent, new Vector3(-3.0f, 1.1f, 6.6f), new Vector3(0.6f, 0.6f, 0.6f), _trim);
            Box("Crate_C", parent, new Vector3(-2.1f, 0.4f, 6.9f), new Vector3(0.8f, 0.8f, 0.8f), _trim);

            Box("CeilingPanel", parent, new Vector3(0f, h - 0.02f, 4f), new Vector3(1.2f, 0.04f, 1.2f), _lightPanel, false);
        }

        // Corridor: x -1.5..1.5, z 8..24, height 3.5.
        private static void BuildCorridor(Transform parent)
        {
            const float h = SmallRoomHeight;
            Box("Floor", parent, new Vector3(0f, -0.1f, 16f), new Vector3(3f + 2 * Wall, 0.2f, 16f), _floor);
            Box("Ceiling", parent, new Vector3(0f, h + 0.1f, 16f), new Vector3(3f + 2 * Wall, 0.2f, 16f), _ceiling);
            Box("Wall_West", parent, new Vector3(-1.6f, h / 2f, 16f), new Vector3(Wall, h, 16f), _wall);
            Box("Wall_East", parent, new Vector3(1.6f, h / 2f, 16f), new Vector3(Wall, h, 16f), _wall);

            // Light strips (no collision)
            Box("LightStrip_Ceiling", parent, new Vector3(0f, h - 0.02f, 16f), new Vector3(0.3f, 0.04f, 15f), _glowCyan, false);
            Box("GuideStrip_Left", parent, new Vector3(-1.4f, 0.01f, 16f), new Vector3(0.06f, 0.02f, 15.5f), _glowAmber, false);
            Box("GuideStrip_Right", parent, new Vector3(1.4f, 0.01f, 16f), new Vector3(0.06f, 0.02f, 15.5f), _glowAmber, false);
        }

        // Mission control: x -6..6, z 24..38, height 5. Observation window on the north wall.
        private static void BuildMissionControl(Transform parent)
        {
            const float h = ControlRoomHeight;
            Box("Floor", parent, new Vector3(0f, -0.1f, 31f), new Vector3(12f + 2 * Wall, 0.2f, 14f + 2 * Wall), _floor);
            Box("Ceiling", parent, new Vector3(0f, h + 0.1f, 31f), new Vector3(12f + 2 * Wall, 0.2f, 14f + 2 * Wall), _ceiling);
            Box("Wall_West", parent, new Vector3(-6.1f, h / 2f, 31f), new Vector3(Wall, h, 14f + 2 * Wall), _wall);
            Box("Wall_East", parent, new Vector3(6.1f, h / 2f, 31f), new Vector3(Wall, h, 14f + 2 * Wall), _wall);

            // South wall with an opening from the corridor
            Box("Wall_South_Left", parent, new Vector3(-3.85f, h / 2f, 23.9f), new Vector3(4.7f, h, Wall), _wall);
            Box("Wall_South_Right", parent, new Vector3(3.85f, h / 2f, 23.9f), new Vector3(4.7f, h, Wall), _wall);
            DoorFrame("Doorway_CorridorToControl", parent, 23.9f, h, -1f);

            // North wall built around a 10 m x 3.5 m observation window
            Box("Wall_North_Sill", parent, new Vector3(0f, 0.5f, 38.1f), new Vector3(12f + 2 * Wall, 1f, Wall), _wall);
            Box("Wall_North_Header", parent, new Vector3(0f, 4.75f, 38.1f), new Vector3(12f + 2 * Wall, 0.5f, Wall), _wall);
            Box("Wall_North_PillarLeft", parent, new Vector3(-5.6f, 2.75f, 38.1f), new Vector3(1.2f, 3.5f, Wall), _wall);
            Box("Wall_North_PillarRight", parent, new Vector3(5.6f, 2.75f, 38.1f), new Vector3(1.2f, 3.5f, Wall), _wall);

            GameObject glass = Box("ObservationWindow_Glass", parent, new Vector3(0f, 2.75f, 38.1f), new Vector3(10f, 3.5f, 0.05f), _glass);
            glass.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box("ObservationWindow_FrameBottom", parent, new Vector3(0f, 1.02f, 37.98f), new Vector3(10f, 0.04f, 0.04f), _glowCyan, false);

            // Two non-interactive side workstations (decoration)
            Workstation("Workstation_Left", parent, new Vector3(-4f, 0f, 30f));
            Workstation("Workstation_Right", parent, new Vector3(4f, 0f, 30f));

            // Ceiling light panels
            Box("CeilingPanel_A", parent, new Vector3(-3f, h - 0.02f, 28f), new Vector3(1.2f, 0.04f, 1.2f), _lightPanel, false);
            Box("CeilingPanel_B", parent, new Vector3(3f, h - 0.02f, 28f), new Vector3(1.2f, 0.04f, 1.2f), _lightPanel, false);
            Box("CeilingPanel_C", parent, new Vector3(-3f, h - 0.02f, 35f), new Vector3(1.2f, 0.04f, 1.2f), _lightPanel, false);
            Box("CeilingPanel_D", parent, new Vector3(3f, h - 0.02f, 35f), new Vector3(1.2f, 0.04f, 1.2f), _lightPanel, false);
        }

        // The mission console the player will interact with (behaviour is added in a later step).
        private static void BuildMissionConsole(Transform parent)
        {
            GameObject console = new GameObject("MissionConsole");
            console.transform.SetParent(parent, false);
            console.transform.localPosition = new Vector3(0f, 0f, 34.5f);
            Transform t = console.transform;

            Box("Desk", t, new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1f, 0.9f), _console);
            Box("StatusStrip", t, new Vector3(0f, 0.85f, -0.46f), new Vector3(2.2f, 0.04f, 0.02f), _glowAmber, false);

            // Screen tilted back by 20 degrees so it faces a standing player
            GameObject bezel = Box("ScreenBezel", t, new Vector3(0f, 1.45f, 0.28f), new Vector3(2.1f, 1.0f, 0.04f), _trim);
            bezel.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            GameObject screen = Box("Screen", t, new Vector3(0f, 1.45f, 0.25f), new Vector3(2.0f, 0.9f, 0.02f), _glowCyan, false);
            screen.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        }

        // ------------------------------------------------------------------
        // Lighting, space backdrop, spawn point
        // ------------------------------------------------------------------

        private static void BuildLights(Transform parent)
        {
            Color coolWhite = new Color(0.85f, 0.92f, 1f);
            Color corridorBlue = new Color(0.7f, 0.85f, 1f);
            Color warmWhite = new Color(1f, 0.95f, 0.88f);
            Color screenCyan = new Color(0.3f, 0.85f, 1f);

            PointLight("Light_Dock", parent, new Vector3(0f, 3.2f, 4f), coolWhite, 3f, 9f);

            PointLight("Light_Corridor_1", parent, new Vector3(0f, 3.2f, 10.5f), corridorBlue, 2f, 7f);
            PointLight("Light_Corridor_2", parent, new Vector3(0f, 3.2f, 16f), corridorBlue, 2f, 7f);
            PointLight("Light_Corridor_3", parent, new Vector3(0f, 3.2f, 21.5f), corridorBlue, 2f, 7f);

            PointLight("Light_Control_A", parent, new Vector3(-3f, 4.6f, 28f), warmWhite, 3.5f, 10f);
            PointLight("Light_Control_B", parent, new Vector3(3f, 4.6f, 28f), warmWhite, 3.5f, 10f);
            PointLight("Light_Control_C", parent, new Vector3(-3f, 4.6f, 35f), warmWhite, 3.5f, 10f);
            PointLight("Light_Control_D", parent, new Vector3(3f, 4.6f, 35f), warmWhite, 3.5f, 10f);

            PointLight("Light_ConsoleGlow", parent, new Vector3(0f, 1.8f, 33.8f), screenCyan, 1.5f, 3f);
        }

        // A planet and a starfield, visible through the observation window.
        private static void BuildSpace(Transform parent)
        {
            GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "Planet_Kepler";
            planet.transform.SetParent(parent, false);
            planet.transform.localPosition = new Vector3(90f, 30f, 300f);
            planet.transform.localScale = Vector3.one * 160f;
            planet.GetComponent<MeshRenderer>().sharedMaterial = _planet;
            Object.DestroyImmediate(planet.GetComponent<Collider>());

            Transform stars = Group("Starfield", parent);
            Random.State previousState = Random.state;
            Random.InitState(2026); // same star layout on every build
            for (int i = 0; i < 400; i++)
            {
                Vector3 position = Random.onUnitSphere * Random.Range(550f, 700f);
                float size = Random.Range(0.8f, 2.2f);
                Box("Star_" + i, stars, position, Vector3.one * size, _star, false);
            }
            Random.state = previousState;
        }

        private static void BuildPlayerSpawn(Transform parent)
        {
            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(parent, false);
            spawn.transform.localPosition = new Vector3(0f, 0f, 2.5f);
            spawn.transform.localRotation = Quaternion.identity; // facing +Z, toward the corridor
        }

        private static void ConfigureEnvironmentAndCamera()
        {
            // Dark space: no skybox, low flat ambient light
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.11f, 0.14f);
            RenderSettings.fog = false;
            DynamicGI.UpdateEnvironment();

            // Place the existing Main Camera at the spawn point, at eye height,
            // so pressing Play shows the station before the player exists.
            GameObject cameraObject = GameObject.FindWithTag("MainCamera");
            if (cameraObject == null)
            {
                Debug.LogWarning("[StationBuilder] No camera tagged MainCamera was found; camera not moved.");
                return;
            }

            Undo.RecordObject(cameraObject.transform, "Move Main Camera");
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 2.5f), Quaternion.identity);

            Camera cam = cameraObject.GetComponent<Camera>();
            if (cam != null)
            {
                Undo.RecordObject(cam, "Configure Main Camera");
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 1500f;
            }
        }

        // ------------------------------------------------------------------
        // Building helpers
        // ------------------------------------------------------------------

        // A 2 m x 2.5 m doorway inside a 3 m wide, 'roomHeight' tall opening.
        // 'approachSide' is -1 when the player approaches from the -Z side (the amber
        // strip above the door is placed on that side), +1 for the +Z side.
        private static void DoorFrame(string name, Transform parent, float z, float roomHeight, float approachSide)
        {
            Transform frame = Group(name, parent);
            float jambWidth = (3f - DoorWidth) / 2f;           // 0.5 m each side
            float lintelHeight = roomHeight - DoorHeight;       // fills the space above the door

            Box("Jamb_Left", frame, new Vector3(-(DoorWidth / 2f + jambWidth / 2f), DoorHeight / 2f, z), new Vector3(jambWidth, DoorHeight, Wall), _trim);
            Box("Jamb_Right", frame, new Vector3(DoorWidth / 2f + jambWidth / 2f, DoorHeight / 2f, z), new Vector3(jambWidth, DoorHeight, Wall), _trim);
            Box("Lintel", frame, new Vector3(0f, DoorHeight + lintelHeight / 2f, z), new Vector3(3f, lintelHeight, Wall), _trim);

            // Amber light strip above the doorway, on the approach side
            float stripZ = z + approachSide * (Wall / 2f + 0.01f);
            Box("DoorLight", frame, new Vector3(0f, DoorHeight + 0.06f, stripZ), new Vector3(DoorWidth, 0.05f, 0.02f), _glowAmber, false);
        }

        private static void Workstation(string name, Transform parent, Vector3 position)
        {
            Transform station = Group(name, parent);
            station.localPosition = position;
            Box("Desk", station, new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 0.8f), _console);
            GameObject screen = Box("Screen", station, new Vector3(0f, 1.15f, 0.2f), new Vector3(1.2f, 0.6f, 0.04f), _glowCyan, false);
            screen.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        }

        private static Transform Group(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Box(string name, Transform parent, Vector3 localCenter, Vector3 size, Material material, bool withCollider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!withCollider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        private static void PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------
        // Materials (URP)
        // ------------------------------------------------------------------

        private static bool CreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (lit == null || unlit == null)
            {
                EditorUtility.DisplayDialog("AstroGame Station Builder",
                    "URP shaders were not found. This project must use the Universal Render Pipeline.", "OK");
                return false;
            }

            EnsureFolder(MaterialFolder);

            _floor = GetOrCreate("M_Floor", lit, m => SetLit(m, new Color(0.16f, 0.17f, 0.19f), 0.3f, 0.45f));
            _wall = GetOrCreate("M_Wall", lit, m => SetLit(m, new Color(0.52f, 0.55f, 0.58f), 0.1f, 0.35f));
            _ceiling = GetOrCreate("M_Ceiling", lit, m => SetLit(m, new Color(0.12f, 0.13f, 0.15f), 0.2f, 0.25f));
            _trim = GetOrCreate("M_Trim", lit, m => SetLit(m, new Color(0.22f, 0.23f, 0.25f), 0.7f, 0.5f));
            _console = GetOrCreate("M_Console", lit, m => SetLit(m, new Color(0.09f, 0.10f, 0.12f), 0.6f, 0.6f));

            _glowCyan = GetOrCreate("M_GlowCyan", lit, m => SetEmissive(m, new Color(0.05f, 0.25f, 0.3f), new Color(0.2f, 0.85f, 1f) * 3f));
            _glowAmber = GetOrCreate("M_GlowAmber", lit, m => SetEmissive(m, new Color(0.3f, 0.18f, 0.05f), new Color(1f, 0.6f, 0.15f) * 2.5f));
            _lightPanel = GetOrCreate("M_LightPanel", lit, m => SetEmissive(m, new Color(0.9f, 0.9f, 0.9f), new Color(1f, 0.97f, 0.9f) * 2f));
            _glass = GetOrCreate("M_Glass", lit, SetGlass);
            _planet = GetOrCreate("M_Planet", lit, m => SetLit(m, new Color(0.75f, 0.45f, 0.28f), 0f, 0.2f));
            _star = GetOrCreate("M_Star", unlit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.95f, 0.97f, 1f));
                m.enableInstancing = true;
            });
            return true;
        }

        private static Material GetOrCreate(string name, Shader shader, System.Action<Material> setup)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            setup(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetLit(Material m, Color baseColor, float metallic, float smoothness)
        {
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            m.enableInstancing = true;
        }

        private static void SetEmissive(Material m, Color baseColor, Color hdrEmission)
        {
            SetLit(m, baseColor, 0f, 0.5f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", hdrEmission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        private static void SetGlass(Material m)
        {
            SetLit(m, new Color(0.6f, 0.8f, 1f, 0.12f), 0f, 0.95f);
            m.SetFloat("_Surface", 1f); // Transparent
            m.SetFloat("_Blend", 0f);   // Alpha blending
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
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
