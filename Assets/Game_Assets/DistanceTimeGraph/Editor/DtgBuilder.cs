using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DistanceTimeGraph.EditorTools
{
    /// Builds Assets/Scenes/Distance_Time_Graph.unity from scratch: the sky road, environment, car, XR player, the
    /// graph interface and every game system, wired together. It only writes inside Assets/DistanceTimeGraph and
    /// that one scene; other games' scenes, scripts and the Build Settings are left alone.
    public static partial class DtgBuilder
    {
        public const string ScenePath = "Assets/Scenes/Distance_Time_Graph.unity";
        public const string Root = "Assets/DistanceTimeGraph";
        public const string MatFolder = Root + "/Materials";
        public const string DataFolder = Root + "/Data";
        public const string GenFolder = Root + "/Generated";
        public const string CatalogPath = DataFolder + "/DTG_Catalog.asset";

        // Player: standing on the control platform 11 m behind the start line, 1.5 m above the road deck.
        public static readonly Vector3 PlayerFeet = new Vector3(0f, 1.5f, -11f);
        public const float EyeHeight = 1.36f;   // the XR rig's default camera offset
        public static Vector3 Head => PlayerFeet + Vector3.up * EyeHeight;
        /// The comfortable resting gaze the panels are laid out around: straight ahead, 15 degrees down, where the car
        /// waits at the start. On a desktop the view starts there (DesktopViewPitch).
        public const float GazePitch = 15f;
        public static Quaternion Gaze => Quaternion.Euler(GazePitch, 0f, 0f);
        /// A point `r` metres from the eyes, `yaw` degrees right and `pitch` degrees up from the resting gaze.
        public static Vector3 AtAngles(float yaw, float pitch, float r) => Head + Gaze * (Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward) * r;

        [MenuItem("Tools/Distance Time Graph/Build Scene")]
        public static void BuildMenu()
        {
            if (Build()) EditorUtility.DisplayDialog("Distance Time Graph", "Scene built. Run Tools > Distance Time Graph > Validate to check it.", "OK");
        }

        public static bool Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Distance Time Graph", "Exit Play Mode first.", "OK");
                return false;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            string rigPath = FindPrefab("XR Origin (XR Rig)");
            if (rigPath == null)
            {
                EditorUtility.DisplayDialog("Distance Time Graph",
                    "The XR Interaction Toolkit 'Starter Assets' sample is missing (XR Origin (XR Rig) prefab). Import it from the Package Manager, then build again.", "OK");
                return false;
            }

            foreach (var f in new[] { MatFolder, DataFolder, GenFolder }) Directory.CreateDirectory(f);
            Directory.CreateDirectory("Backups/DistanceTimeGraph");
            if (File.Exists(ScenePath))
                File.Copy(ScenePath, "Backups/DistanceTimeGraph/" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
            AssetDatabase.Refresh();
            ConfigureCarTexture();

            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects()) Object.DestroyImmediate(go);

            UiKit.Rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            UiKit.Circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var catalog = GetOrCreateCatalog();
            Mats.Create();
            outlinedText = null;

            var root = new GameObject("DistanceTimeGraph").transform;
            var systems = new GameObject("Systems").transform;
            systems.SetParent(root, false);

            // ---- world ----
            var sun = BuildLighting(root);
            var track = BuildTrack(root, out var modules, out var returnArrows);
            var world = BuildEnvironment(root);
            var landmarks = BuildLandmarks(track, out var beacon);
            var bridge = BuildBridge(track);
            var flags = BuildMarkersAndFlags(track);

            // ---- cars ----
            var car = BuildCar(track, "Car", Mats.CarBody, Mats.CarLights, true);
            var previewCar = BuildCar(track, "PreviewCar", Mats.PreviewCyan, Mats.PreviewCyan, false);
            previewCar.gameObject.SetActive(false);
            var ghosts = new CarMotionController[3];
            for (int i = 0; i < 3; i++)
            {
                ghosts[i] = BuildCar(track, "GhostCar_" + (i + 1), Mats.Ghost, Mats.Ghost, false);
                ghosts[i].gameObject.SetActive(false);
            }

            // ---- player ----
            var rig = BuildPlayer(root, rigPath, out var head, out var controllers);
            var hands = systems.gameObject.AddComponent<SimpleHands>();
            hands.controllers = controllers;
            hands.gloves = controllers.Select((c, i) => c != null ? BuildGlove(c, i == 0) : null).ToArray();
            hands.head = head;
            hands.Pose(true);   // the saved scene shows the hands resting in the lower corners
            EnsureEventSystem();
            EnsureInteractionManager();

            // ---- interface ----
            var ui = BuildInterface(root, head.GetComponent<Camera>(), catalog);
            var veil = BuildVeil(head);
            var rearCam = BuildRearCamera(root, ui);

            // ---- systems ----
            var level = systems.gameObject.AddComponent<LevelManager>();
            level.catalog = catalog;
            level.trackOrigin = track;
            level.roadModules = modules;
            level.landmarks = landmarks;
            level.turnaroundBeacon = beacon;
            level.bridge = bridge;
            level.islandLayouts = world;
            level.flags = flags;
            level.sun = sun;
            level.skybox = Mats.Sky;
            level.backArrowsRoad = returnArrows;

            var variantManager = systems.gameObject.AddComponent<AdaptiveVariantManager>();
            variantManager.catalog = catalog;

            var ghostCtl = systems.gameObject.AddComponent<GhostReplayController>();
            ghostCtl.ghostCars = ghosts;
            ghostCtl.graph = ui.graph;

            var previewCtl = systems.gameObject.AddComponent<CarPreviewController>();
            previewCtl.graph = ui.graph;
            previewCtl.previewCar = previewCar;
            previewCtl.link = BuildLinkLine(systems);
            previewCtl.label = WorldText(systems, "PreviewLabel", "PREVIEW", 1.4f, Palette.Cyan, Vector3.zero);
            previewCtl.label.gameObject.SetActive(false);

            var playback = systems.gameObject.AddComponent<GraphPlaybackController>();
            playback.graph = ui.graph;
            playback.car = car;
            playback.ghosts = ghostCtl;
            playback.bridge = bridge;
            playback.level = level;

            var flow = systems.gameObject.AddComponent<GameFlowManager>();
            flow.catalog = catalog;
            flow.level = level;
            flow.variants = variantManager;
            flow.graph = ui.graph;
            flow.playback = playback;
            flow.car = car;
            flow.preview = previewCtl;
            flow.ghosts = ghostCtl;
            flow.bridge = bridge;
            flow.rearCamera = rearCam;
            flow.activity = ui.activity;
            flow.console = ui.console;
            flow.popup = ui.popup;
            flow.veil = veil;
            flow.assessment = ui.assessment;

            var cluster = ui.cluster.gameObject.AddComponent<ControlCluster>();
            cluster.head = head;
            cluster.designHeadY = Head.y;

            // What the saved scene shows: Level 1's first course with its start point.
            var first = catalog.variants.First(v => v.kind == Logic.LevelKind.Rising);
            level.Apply(first);
            ui.graph.SetModel(new Logic.GraphModel(catalog.maxTime, catalog.maxDistance, first.start));
            ui.activity.SetActivity($"LEVEL 1 · {first.title}", first.Instruction);
            ui.activity.SetPrediction("The graph ends at 0 m after 0 s.", Palette.SoftInk);
            ui.activity.SetStatus("t = 0.0 s   ·   0.0 m from start   ·   stopped");
            ui.activity.SetBanner(null, Palette.Blue);
            ui.activity.SetCards(new Logic.AttemptRecord[0], catalog.maxTime, catalog.maxDistance);
            car.Teleport(first.start);
            rearCam.target = car.transform;
            rearCam.SnapNow();   // so the monitor shows the car in the saved scene too, before Play
            ui.popup.Hide();
            ui.assessment.gameObject.SetActive(false);

            MarkStatic(world);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems.gameObject;
            Debug.Log("[DTG] Distance Time Graph scene built at " + ScenePath);
            return true;
        }

        // ---------------- shared helpers ----------------

        static DtgCatalog GetOrCreateCatalog()
        {
            var c = AssetDatabase.LoadAssetAtPath<DtgCatalog>(CatalogPath);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<DtgCatalog>();
                c.FillDefaults();
                AssetDatabase.CreateAsset(c, CatalogPath);
            }
            else if (c.variants.Count == 0 || c.themes.Count == 0 || c.lighting.Count == 0)
            {
                c.FillDefaults();
                EditorUtility.SetDirty(c);
            }
            return c;
        }

        public static string FindPrefab(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets($"\"{name}\" t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name) return path;
            }
            return null;
        }

        public static GameObject Prefab(string path, Transform parent, Vector3 pos, Quaternion rot)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning("[DTG] prefab missing: " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.SetPositionAndRotation(pos, rot);
            return go;
        }

        /// Places a prefab and scales it so its largest dimension (or its height) matches `size` metres.
        public static GameObject Scenery(string path, Transform parent, Vector3 pos, float size, float yaw = 0f, bool byHeight = false)
        {
            var go = Prefab(path, parent, pos, Quaternion.Euler(0, yaw, 0));
            if (go == null) return null;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return go;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float current = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (current > 0.001f) go.transform.localScale *= size / current;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go;
        }

        public static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material m, Quaternion? rot = null)
            => Prim(PrimitiveType.Cube, parent, name, localPos, size, m, rot);

        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material m, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot ?? Quaternion.identity;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            return go;
        }

        public static TMPro.TextMeshPro WorldText(Transform parent, string name, string text, float size, Color color, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var t = go.AddComponent<TMPro.TextMeshPro>();
            t.text = text;
            t.fontSize = size * 10f;
            t.color = color;
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.fontStyle = TMPro.FontStyles.Bold;
            t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(12f, 2f);
            // One shared outline material (setting outlineWidth here would copy the material for every label).
            t.fontSharedMaterial = OutlinedTextMaterial(t);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return t;
        }

        static Material outlinedText;

        static Material OutlinedTextMaterial(TMPro.TMP_Text sample)
        {
            if (outlinedText != null) return outlinedText;
            string path = MatFolder + "/DTG_WorldTextOutline.mat";
            outlinedText = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (outlinedText == null)
            {
                outlinedText = new Material(sample.fontSharedMaterial) { name = "DTG_WorldTextOutline" };
                AssetDatabase.CreateAsset(outlinedText, path);
            }
            outlinedText.EnableKeyword("OUTLINE_ON");
            outlinedText.SetFloat("_OutlineWidth", 0.2f);
            outlinedText.SetColor("_OutlineColor", new Color32(5, 20, 50, 255));
            EditorUtility.SetDirty(outlinedText);
            return outlinedText;
        }

        static void EnsureEventSystem()
        {
            var es = Object.FindAnyObjectByType<EventSystem>();
            var go = es != null ? es.gameObject : new GameObject("EventSystem", typeof(EventSystem));
            foreach (var m in go.GetComponents<BaseInputModule>()) if (!(m is XRUIInputModule)) Object.DestroyImmediate(m);
            var module = go.GetComponent<XRUIInputModule>();
            if (module == null) module = go.AddComponent<XRUIInputModule>();
            module.enableXRInput = true;
            module.enableMouseInput = true;
            module.enableBuiltinActionsAsFallback = true;
        }

        static void EnsureInteractionManager()
        {
            if (Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
                new GameObject("XR Interaction Manager", typeof(UnityEngine.XR.Interaction.Toolkit.XRInteractionManager));
        }

        static void MarkStatic(GameObject[] layouts)
        {
            var env = GameObject.Find("Environment");
            if (env == null) return;
            foreach (var t in env.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        static void ConfigureCarTexture()
        {
            var path = Root + "/Textures/DTG_Car_Atlas.png";
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && (ti.filterMode != FilterMode.Point || ti.mipmapEnabled))
            {
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
        }
    }
}
