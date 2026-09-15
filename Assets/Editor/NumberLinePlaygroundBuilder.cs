using System.IO;
using System.Linq;
using NumberLinePlayground.Level1;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Builds the "Number Line Playground" scene (see Assets/Docs/2.png and Work/NumberLinePlayground/reference)
/// into a new scene duplicated from the HQP Low Poly Trees Demo_07 environment.
///
/// The scene holds everything for the topic "Position, Origin and Direction": a thin glowing number line from -8 to +8,
/// an origin ring with a flag, four sphere slots, and a table with a maker machine and four spot slots. Each level
/// (line, triangle, square) uses as many slots as it needs. The rules of a level live in LineLevelController, the order
/// of the whole topic (popups, levels, quiz, going back to the start) in ConceptFlow; this class only places things and
/// wires them up. The saved scene shows Level 1 with its first numbers.
/// Run via Tools > Number Line Playground > Build Environment.
public static class NumberLinePlaygroundBuilder
{
    const string SourceScenePath = "Assets/HQP STUDIOS/Low Poly Trees and Vegetation - Pack/Demo/Demo_07.unity";
    const string NewScenePath = "Assets/Free Low Poly Game Assets/Scene/NumberLinePlayground.unity";
    const string MatFolder = "Assets/Free Low Poly Game Assets/Materials/NumberLinePlayground";

    // Where the player's head starts: back from the origin ring, facing the line and the table. Every level ends here too.
    static readonly Vector3 PlayerStart = new Vector3(0f, 0.08f, -6.5f);

    [MenuItem("Tools/Number Line Playground/Build Environment")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Number Line Playground", "Exit Play Mode first.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // Import XRI samples (if needed) before touching the scene at all: if this is the
        // first import, Unity needs to compile the new sample scripts before it's safe to
        // instantiate prefabs that depend on them, so we bail out and ask for a second click.
        if (ImportXriSamples())
        {
            EditorUtility.DisplayDialog("Number Line Playground",
                "The XR Interaction Toolkit sample assets (XR Origin, controllers, Device Simulator) " +
                "were just imported for the first time.\n\nUnity needs a moment to compile the new scripts. " +
                "Wait for the compiling spinner (bottom-right corner) to finish, then click Build Environment again.",
                "OK");
            return;
        }

        // Fresh copy of the demo environment so Demo_07.unity itself is never touched.
        // Preserve the existing scene GUID and a recoverable copy before rebuilding.
        Directory.CreateDirectory("Backups/NumberLinePlayground");
        if (File.Exists(NewScenePath))
            File.Copy(NewScenePath, "Backups/NumberLinePlayground/" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
        if (File.Exists(NewScenePath)) File.Copy(SourceScenePath, NewScenePath, true);
        if (!File.Exists(NewScenePath) && !AssetDatabase.CopyAsset(SourceScenePath, NewScenePath))
        {
            Debug.LogError($"Could not copy {SourceScenePath} to {NewScenePath}");
            return;
        }
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.OpenScene(NewScenePath, OpenSceneMode.Single);

        Directory.CreateDirectory(MatFolder);

        var root = new GameObject("NumberLinePlayground").transform;

        // Demo_07 ships with no colliders at all, so the XR rig's CharacterController would
        // fall straight through the terrain. Give the terrain meshes real colliders so the
        // player actually stands on the ground (also used below for ground-height sampling).
        EnsureTerrainColliders();
        Physics.SyncTransforms();
        EnsureEventSystem(); // needed for mouse/gamepad clicks on the slide's buttons
        EnsureInteractionManager(); // needed for the XR controllers' ray interactors to function at all

        // Place the whole playground directly in front of whatever camera is actually
        // in this scene, facing it, instead of at world origin (which could be anywhere
        // relative to Demo_07's camera and cause everything to overlap on screen).
        Camera cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0f, 2f, -15f);
        Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 flatForward = new Vector3(camForward.x, 0f, camForward.z);
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
        flatForward.Normalize();

        const float distanceFromCamera = 15f;
        Vector3 centerXZ = camPos + flatForward * distanceFromCamera;
        float groundY = GetGroundY(centerXZ.x, centerXZ.z);
        // Keep the entire activity surface above the rolling source terrain.
        var heading = Quaternion.LookRotation(flatForward, Vector3.up);
        for (int x = -11; x <= 11; x += 2)
            for (int z = -9; z <= 7; z += 2)
            {
                var sample = centerXZ + heading * new Vector3(x, 0, z);
                groundY = Mathf.Max(groundY, GetGroundY(sample.x, sample.z));
            }
        groundY += .06f;

        root.position = new Vector3(centerXZ.x, groundY, centerXZ.z);
        root.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

        // ---- materials ----
        var matRed = GetOrCreateMat("NLP_Red", new Color(1f, 0.06f, 0.18f), emissive: true);
        var matBlue = GetOrCreateMat("NLP_Blue", new Color(0.05f, 0.35f, 1f), emissive: true);
        var matRedHalo = GetOrCreateMat("NLP_RedHalo", new Color(0.55f, 0.07f, 0.13f));
        var matBlueHalo = GetOrCreateMat("NLP_BlueHalo", new Color(0.09f, 0.22f, 0.6f));
        var matWhite = GetOrCreateMat("NLP_White", Color.white);
        var matGlowWhite = GetOrCreateMat("NLP_GlowWhite", Color.white, emissive: true);
        var matOrange = GetOrCreateMat("NLP_Orange", new Color(0.95f, 0.55f, 0.1f), emissive: true, doubleSided: true);
        var matWood = GetOrCreateMat("NLP_Wood", new Color(0.42f, 0.27f, 0.15f));
        var matPost = GetOrCreateMat("NLP_Post", new Color(0.33f, 0.21f, 0.11f));
        var matCoral = GetOrCreateMat("NLP_Coral", new Color(1f, 0.32f, 0.42f), emissive: true);
        var matRingIdle = GetOrCreateMat("NLP_RingIdle", Color.white, emissive: true);
        var matRingArmed = GetOrCreateMat("NLP_RingArmed", new Color(0.24f, 0.88f, 0.5f), emissive: true);
        var matCyan = GetOrCreateMat("NLP_Cyan", new Color(0.36f, 0.88f, 0.9f), emissive: true);
        var matCyanHot = GetOrCreateMat("NLP_CyanHot", new Color(0.7f, 1f, 0.85f), emissive: true);
        var matSlate = GetOrCreateMat("NLP_Slate", new Color(0.17f, 0.29f, 0.38f));
        var matSteel = GetOrCreateMat("NLP_Steel", new Color(0.52f, 0.57f, 0.66f));
        var matScreen = GetOrCreateMat("NLP_Screen", new Color(0.05f, 0.1f, 0.14f));
        var matLantern = GetOrCreateMat("NLP_Lantern", new Color(1f, 0.72f, 0.3f), emissive: true);
        var matTagCoral = GetOrCreateMat("NLP_TagCoral", new Color(0.95f, 0.25f, 0.38f));
        var matTagBlue = GetOrCreateMat("NLP_TagBlue", new Color(0.12f, 0.42f, 0.95f));

        BuildClearing(root, matWood, matPost);

        // ---- the number line, the origin ring and its flag ----
        var lineRoot = BuildNumberLine(root, matRed, matBlue, matRedHalo, matBlueHalo, matGlowWhite);
        var originRing = BuildRing(root, "OriginRing", new Vector3(0f, 0.05f, 0f), 0.5f, matRingIdle, 0.07f);
        BuildZeroFlag(root, matWhite, matOrange);
        // Beside the ring: arrows that light up as the player faces left or right. Level 2 asks the player to face the
        // way they will go and squeeze; the other levels hide them.
        var leftArrow = BuildArrow(root, "RingArrowLeft", new Vector3(-1.0f, 0.05f, 0f), -1, matRingIdle, true);
        var rightArrow = BuildArrow(root, "RingArrowRight", new Vector3(1.0f, 0.05f, 0f), 1, matRingIdle, true);
        leftArrow.localScale = rightArrow.localScale = Vector3.one * 1.4f;

        // ---- first-person VR player: the real XR Interaction Toolkit rig, so controllers
        // are visible and drivable with mouse/keyboard (via the XR Device Simulator) in the
        // Editor Game view without needing a physical headset. ----
        string xrOriginPath = FindAssetPath("XR Origin (XR Rig)");
        string simulatorPath = FindAssetPath("XR Device Simulator");

        Transform xrCamera = null;
        Transform xrRig = null;
        if (xrOriginPath != null)
        {
            GameObject xrOrigin = InstantiatePrefab(xrOriginPath, root, PlayerStart, Quaternion.identity);
            xrOrigin.name = "XR Origin (Player)";
            xrRig = xrOrigin.transform;
            xrCamera = FindChildRecursive(xrOrigin.transform, "Main Camera");
            DisableAffordanceSystem(xrOrigin);
            if (xrCamera != null)
            {
                var playerCamera = xrCamera.GetComponent<Camera>();
                playerCamera.nearClipPlane = .05f;
                playerCamera.farClipPlane = 400f;
                playerCamera.fieldOfView = 75f;
            }
        }
        else
        {
            Debug.LogWarning("Number Line Playground: couldn't find the 'XR Origin (XR Rig)' prefab. " +
                "Import it via Window > Package Manager > XR Interaction Toolkit > Samples > Starter Assets, then rebuild.");
        }

        if (simulatorPath != null)
        {
            var simulator = InstantiatePrefab(simulatorPath, root, Vector3.zero, Quaternion.identity);
            foreach (var component in simulator.GetComponents<MonoBehaviour>())
            {
                if (component == null) continue;
                var settings = new SerializedObject(component);
                var ui = settings.FindProperty("m_DeviceSimulatorUI");
                if (ui != null) { ui.objectReferenceValue = null; settings.ApplyModifiedPropertiesWithoutUndo(); }
            }
            // Keep simulator input; remove the large help overlay from the scene composition.
            foreach (var component in simulator.GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null && component.GetType().Name == "XRDeviceSimulatorUI")
                    component.enabled = false;
            foreach (var canvas in simulator.GetComponentsInChildren<Canvas>(true))
                canvas.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Number Line Playground: couldn't find the 'XR Device Simulator' prefab. " +
                "Import it via Window > Package Manager > XR Interaction Toolkit > Samples > XR Device Simulator, then rebuild " +
                "to control the VR controllers with mouse/keyboard in the Game view.");
        }

        // The demo scene's own fixed camera is no longer the viewpoint once the player can walk around.
        if (cam != null && xrCamera != null) cam.gameObject.SetActive(false);

        // ---- four sphere slots and the table with four spot slots; each level uses as many as it needs ----
        var spheres = new FetchSphere[4];
        for (int slot = 0; slot < spheres.Length; slot++)
            spheres[slot] = BuildFetchSphere(root, slot, matCoral, matBlue, matTagCoral, matTagBlue);
        var matBlueprint = GetOrCreateMat("NLP_Blueprint", new Color(0.2f, 0.5f, 0.56f));
        var matAmber = GetOrCreateMat("NLP_Amber", new Color(1f, 0.75f, 0.2f), emissive: true);
        BuildTable(root, matWood, matPost, matSlate, matSteel, matScreen, matCyan, matCyanHot, matBlueprint,
            out TableSocket[] sockets, out TextMesh machineTitle, out TextMesh machineCount, out TextMesh machineNote,
            out Transform[] blueprintBars, out Transform[] weldBars, out Transform tableRoot, out Transform post);
        BuildProps(root, matWood, matPost, matSteel, matLantern);

        // Entrance sign
        Vector3 signBasePos = new Vector3(-4.7f, 0f, 2.8f);
        BuildSignPost(root, "EntranceSign", signBasePos, 3.5f, 3.1f, 1.35f, matPost, matWood,
            new[] { ("Number Line\nPlayground", 0.36f, Color.white), ("Move  •  Explore  •  Learn", 0.16f, new Color(0.95f, 0.9f, 0.8f)) });

        // Side signs
        Vector3 leftSignPos = new Vector3(-5.6f, 0f, 1.4f);
        BuildSignPost(root, "SignLeft", leftSignPos, 1.5f, 1.4f, 0.85f, matPost, matWood,
            new[] { ("Negative\nnumbers", 0.19f, Color.white) });
        BuildArrow(root, "LeftSignArrow", leftSignPos + new Vector3(0, .57f, -.07f), -1, matWhite, false);

        Vector3 rightSignPos = new Vector3(5.6f, 0f, 1.4f);
        BuildSignPost(root, "SignRight", rightSignPos, 1.5f, 1.4f, 0.85f, matPost, matWood,
            new[] { ("Positive\nnumbers", 0.19f, Color.white) });
        BuildArrow(root, "RightSignArrow", rightSignPos + new Vector3(0, .57f, -.07f), 1, matWhite, false);

        // ---- floor label, trip summary, and the world-space slide / completion / toast panels ----
        var feetLabel = CreateLabel("FeetLabel", "x = 0.0 m", new Vector3(0f, 0.04f, -6f), Color.white, 0.32f, root);
        feetLabel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var tripPath = CreateLabel("TripPathLabel", "Path 0.0 m", new Vector3(0f, 1.95f, 0.8f), new Color(0.96f, 0.68f, 0.32f), 0.13f, root);
        var tripDisplacement = CreateLabel("TripDisplacementLabel", "Displacement 0.0 m", new Vector3(0f, 1.78f, 0.8f), new Color(0.34f, 0.78f, 0.93f), 0.13f, root);
        tripPath.AddComponent<Billboard>();
        tripDisplacement.AddComponent<Billboard>();
        tripPath.SetActive(false);
        tripDisplacement.SetActive(false);

        var groundLabels = lineRoot.GetComponent<GroundLabels>();
        if (groundLabels != null) groundLabels.head = xrCamera;
        var hud = BuildLevelHud(root, xrCamera, out NumberLinePlayground.Flow.PopupManager popup);
        var confetti = BuildConfetti(xrCamera != null ? xrCamera : root);
        var fade = xrCamera != null ? BuildScreenFade(xrCamera) : null;
        var splash = BuildSplash(root);
        var mark = BuildMark(root, matAmber);
        Transform carryPoint = xrCamera != null ? BuildCarryPoint(xrCamera) : null;

        // ---- the level controller plays one level; the flow sequences the whole topic around it ----
        root.gameObject.AddComponent<GrabComfort>();   // press once to pick up, once to put down; trigger works as well as grip
        var controller = root.gameObject.AddComponent<LineLevelController>();
        controller.root = root;
        controller.head = xrCamera;
        controller.spheres = spheres;
        controller.sockets = sockets;
        controller.machineTitle = machineTitle;
        controller.machineCount = machineCount;
        controller.blueprintBars = blueprintBars;
        controller.weldBars = weldBars;
        controller.table = tableRoot;
        controller.post = post;
        controller.leftArrow = leftArrow.GetComponentsInChildren<Renderer>();
        controller.rightArrow = rightArrow.GetComponentsInChildren<Renderer>();
        controller.machineNote = machineNote;
        controller.splash = splash;
        controller.markObject = mark;
        controller.markButton = hud.markButton;
        controller.markText = hud.markText;
        controller.promptBox = hud.promptBox;
        controller.promptText = hud.promptText;
        controller.carryPoint = carryPoint;
        controller.originRing = originRing.GetComponentsInChildren<Renderer>();
        controller.ringIdleMaterial = matRingIdle;
        controller.ringArmedMaterial = matRingArmed;
        controller.feetLabel = feetLabel.GetComponent<TextMesh>();
        controller.tripPathLabel = tripPath.GetComponent<TextMesh>();
        controller.tripDisplacementLabel = tripDisplacement.GetComponent<TextMesh>();
        controller.toastBox = hud.toastBox;
        controller.toastText = hud.toastText;
        controller.positionText = hud.positionText;
        controller.confetti = confetti;
        controller.ApplyLayout(Concept.Levels[0], 0);   // what the saved scene shows: Level 1 with its first numbers

        var flow = root.gameObject.AddComponent<ConceptFlow>();
        flow.level = controller;
        flow.root = root;
        flow.rig = xrRig;
        flow.head = xrCamera;
        flow.fade = fade;
        flow.popup = popup;
        flow.startLocal = PlayerStart;
        flow.topicPanel = hud.topicPanel;
        flow.slidePanel = hud.slidePanel;
        flow.slideHeader = hud.slideHeader;
        flow.slideBody = hud.slideBody;
        flow.slideHint = hud.slideHint;
        flow.slideChips = hud.slideChips;
        flow.startButton = hud.startButton;
        flow.completePanel = hud.completePanel;
        flow.completeTitle = hud.completeTitle;
        flow.completeFigure = hud.completeFigure;
        flow.completePath = hud.completePath;
        flow.completeDisplacement = hud.completeDisplacement;
        flow.completeStars = hud.completeStars;
        flow.completeLesson = hud.completeLesson;
        flow.continueButton = hud.continueButton;
        flow.quizPanel = hud.quizPanel;
        flow.quizHeader = hud.quizHeader;
        flow.quizPrompt = hud.quizPrompt;
        flow.quizFeedback = hud.quizFeedback;
        flow.optionButtons = hud.optionButtons;
        flow.optionTexts = hud.optionTexts;
        flow.resultPanel = hud.resultPanel;
        flow.resultTitle = hud.resultTitle;
        flow.resultMessage = hud.resultMessage;
        flow.resultButton = hud.resultButton;
        flow.resultButtonText = hud.resultButtonText;
        if (xrCamera == null)
            Debug.LogWarning("Number Line Playground: no player camera was found, so the level controller has no head to track. " +
                "Import the XRI Starter Assets and rebuild.");

        var otherScenes = EditorBuildSettings.scenes.Where(s => s.path != NewScenePath && s.path != "Assets/Scenes/SampleScene.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(NewScenePath, true) }.Concat(otherScenes).ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = root.gameObject;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log("Number Line Playground (topic flow: Levels 1 to 3 and the quiz) built at " + NewScenePath);
    }

    // ---------- helpers ----------

    /// Demo_07 ships with no colliders at all. Give every terrain mesh a real MeshCollider
    /// (permanently — this is what lets the XR rig's CharacterController stand on the ground
    /// instead of falling through it) and reuse the same meshes for ground-height sampling.
    static void EnsureTerrainColliders()
    {
        Transform terrainRoot = GameObject.Find("Terrains")?.transform;
        MeshFilter[] meshFilters = terrainRoot != null
            ? terrainRoot.GetComponentsInChildren<MeshFilter>()
            : Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }
    }

    /// UI buttons need an EventSystem + an Input-System-aware UI module to receive
    /// mouse/gamepad clicks; nothing in Demo_07 or the XR rig provides one.
    static void EnsureEventSystem()
    {
        var existing = Object.FindFirstObjectByType<EventSystem>();
        var go = existing != null ? existing.gameObject : new GameObject("EventSystem", typeof(EventSystem));

        if (go.GetComponent<InputSystemUIInputModule>() != null) return;

        // A pre-existing EventSystem (e.g. from the XR rig/simulator) may carry a legacy
        // BaseInputModule (StandaloneInputModule). That can't read any input at all once the
        // project's Active Input Handling is Input System only (which this project is) —
        // it fails silently, so every UI button just looks broken. Replace it.
        var legacy = go.GetComponent<BaseInputModule>();
        if (legacy != null) Object.DestroyImmediate(legacy);

        var module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    /// The XR controllers' ray interactors register with an XRInteractionManager to function
    /// at all. The Starter Assets "XR Origin (XR Rig)" prefab doesn't bundle one itself, so
    /// without an explicit one in the scene the interactors have nothing to communicate with.
    static void EnsureInteractionManager()
    {
        if (Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() != null) return;
        new GameObject("XR Interaction Manager", typeof(UnityEngine.XR.Interaction.Toolkit.XRInteractionManager));
    }

    static float GetGroundY(float x, float z)
    {
        if (Physics.Raycast(new Vector3(x, 500f, z), Vector3.down, out RaycastHit hit, 2000f))
            return hit.point.y;

        Debug.LogWarning($"Number Line Playground: couldn't find ground under ({x:F1},{z:F1}); defaulting to y=0. " +
            "Move the 'NumberLinePlayground' object up/down if it ends up floating or buried.");
        return 0f;
    }

    static Material GetOrCreateMat(string name, Color color, bool emissive = false, bool doubleSided = false)
    {
        string path = $"{MatFolder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);


        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = existing != null ? existing : new Material(shader) { name = name };
        mat.SetColor("_BaseColor", color);
        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.5f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (doubleSided)
        {
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        }
        if (existing == null) AssetDatabase.CreateAsset(mat, path);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    const string XriPackageId = "com.unity.xr.interaction.toolkit";

    /// Imports any missing XRI samples we need. Returns true if something new was imported
    /// (meaning the caller should stop and let Unity recompile before building further).
    static bool ImportXriSamples()
    {
        var pkg = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => p.name == XriPackageId);
        if (pkg == null)
        {
            Debug.LogWarning($"Number Line Playground: package '{XriPackageId}' isn't installed.");
            return false;
        }

        bool importedSomething = false;
        var samples = Sample.FindByPackage(XriPackageId, pkg.version);
        foreach (var sample in samples)
        {
            if (sample.displayName != "Starter Assets" && sample.displayName != "XR Device Simulator")
                continue;
            if (sample.isImported)
                continue;
            if (sample.Import())
                importedSomething = true;
            else
                Debug.LogWarning($"Number Line Playground: failed to import XRI sample '{sample.displayName}'.");
        }
        if (importedSomething) AssetDatabase.Refresh();
        return importedSomething;
    }

    static string FindAssetPath(string nameWithoutExtension)
    {
        var guids = AssetDatabase.FindAssets($"{nameWithoutExtension} t:Prefab");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == nameWithoutExtension)
                return path;
        }
        return null;
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject CreateLabel(string name, string text, Vector3 pos, Color color, float charSize, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.text = text;
        tm.characterSize = charSize * 10f / 64f;
        tm.fontSize = 64;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = tm.font.material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    static GameObject InstantiatePrefab(string path, Transform parent, Vector3 localPos, Quaternion localRot)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"Number Line Playground: prefab not found at {path}");
            return null;
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = localRot;
        return instance;
    }

    static Transform FindChildRecursive(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var found = FindChildRecursive(t.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    /// XRI's controller "affordance" (hover/press color-tween feedback) components can throw
    /// NullReferenceExceptions on Update when their nested renderer references don't survive
    /// being instantiated/reparented via script. They're purely cosmetic, so disable them
    /// rather than leave the console spammed with errors every frame.
    static void DisableAffordanceSystem(GameObject rig)
    {
        foreach (var mb in rig.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue; // missing script reference
            var t = mb.GetType();
            bool isAffordance = (t.Namespace != null && t.Namespace.Contains("Affordance")) || t.Name.Contains("Affordance");
            if (isAffordance) mb.enabled = false;
        }
    }

    static void BuildSignPost(Transform parent, string name, Vector3 basePos, float postHeight, float postSpacing,
        float boardHeight, Material postMat, Material boardMat, (string text, float size, Color color)[] lines)
    {
        var signRoot = new GameObject(name).transform;
        signRoot.SetParent(parent, false);
        signRoot.localPosition = basePos;

        CreatePrimitive(PrimitiveType.Cylinder, "PostL", new Vector3(-postSpacing * 0.5f, postHeight * 0.5f, 0f),
            new Vector3(0.12f, postHeight * 0.5f, 0.12f), postMat, signRoot);
        CreatePrimitive(PrimitiveType.Cylinder, "PostR", new Vector3(postSpacing * 0.5f, postHeight * 0.5f, 0f),
            new Vector3(0.12f, postHeight * 0.5f, 0.12f), postMat, signRoot);

        Vector3 boardCenter = new Vector3(0f, postHeight - boardHeight * 0.5f - 0.15f, 0f);
        CreatePrimitive(PrimitiveType.Cube, "Board", boardCenter, new Vector3(postSpacing + 0.3f, boardHeight, 0.08f), boardMat, signRoot);

        float totalLines = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            float t = lines.Length == 1 ? 0.5f : 1f - (i / (totalLines - 1f));
            float yOffset = lines.Length == 1 ? .07f : (i == 0 ? .15f : -.48f);
            CreateLabel($"Line{i}", lines[i].text, boardCenter + new Vector3(0f, yOffset, -0.055f), lines[i].color, lines[i].size, signRoot);
        }
    }

    // ---------- Level 1 pieces ----------

    /// A thin glowing line from -8 (red) to +8 (blue) with a tick at every whole number, the numbers lying on the
    /// ground below every second tick, and an arrowhead at each end.
    static Transform BuildNumberLine(Transform root, Material red, Material blue, Material redHalo, Material blueHalo, Material glowWhite)
    {
        var line = new GameObject("NumberLine").transform;
        line.SetParent(root, false);
        const float y = 0.03f;

        // The line runs from -half to +half. A darker, wider band under each bright core stands in for the glow.
        const float half = Concept.LineHalfLength;
        CreatePrimitive(PrimitiveType.Cube, "RedHalo", new Vector3(-half / 2f, 0.012f, 0f), new Vector3(half, 0.02f, 0.34f), redHalo, line);
        CreatePrimitive(PrimitiveType.Cube, "BlueHalo", new Vector3(half / 2f, 0.012f, 0f), new Vector3(half, 0.02f, 0.34f), blueHalo, line);
        CreatePrimitive(PrimitiveType.Cube, "RedLine", new Vector3(-half / 2f, y, 0f), new Vector3(half, 0.03f, 0.09f), red, line);
        CreatePrimitive(PrimitiveType.Cube, "BlueLine", new Vector3(half / 2f, y, 0f), new Vector3(half, 0.03f, 0.09f), blue, line);

        for (int n = -Concept.LineHalfLength; n <= Concept.LineHalfLength; n++)
        {
            if (n == 0) continue;
            bool even = n % 2 == 0;
            CreatePrimitive(PrimitiveType.Cube, $"Tick_{n}", new Vector3(n, y + 0.004f, 0f),
                new Vector3(0.05f, 0.035f, even ? 0.5f : 0.3f), glowWhite, line);
        }

        var groundNumbers = new System.Collections.Generic.List<Transform>();
        for (int n = -Concept.LineHalfLength; n <= Concept.LineHalfLength; n += 2)
        {
            Color color = n < 0 ? new Color(1f, 0.62f, 0.68f) : n > 0 ? new Color(0.72f, 0.84f, 1f) : Color.white;
            var label = CreateLabel($"Label_{n}", n.ToString(), new Vector3(n, 0.03f, -0.8f), color, 0.5f, line);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            groundNumbers.Add(label.transform);
        }
        line.gameObject.AddComponent<GroundLabels>().labels = groundNumbers.ToArray();   // the camera is given once the rig is found

        BuildArrow(line, "NegativeArrow", new Vector3(-half - 0.45f, 0.05f, 0f), -1, glowWhite, true);
        BuildArrow(line, "PositiveArrow", new Vector3(half + 0.45f, 0.05f, 0f), 1, glowWhite, true);
        return line;
    }

    /// A tall white pole with a ball on top and an orange pennant marked 0, just behind the origin ring.
    static void BuildZeroFlag(Transform root, Material white, Material orange)
    {
        var flag = new GameObject("ZeroFlag").transform;
        flag.SetParent(root, false);
        flag.localPosition = new Vector3(0f, 0f, 0.3f);
        CreatePrimitive(PrimitiveType.Cylinder, "Pole", new Vector3(0f, 1.1f, 0f), new Vector3(0.05f, 1.1f, 0.05f), white, flag);
        CreatePrimitive(PrimitiveType.Sphere, "Ball", new Vector3(0f, 2.24f, 0f), Vector3.one * 0.16f, white, flag);
        var pennant = new GameObject("Pennant");
        pennant.transform.SetParent(flag, false);
        pennant.transform.localPosition = new Vector3(0.03f, 1.95f, 0f);
        pennant.AddComponent<MeshFilter>().sharedMesh = GetOrCreatePennantMesh();
        pennant.AddComponent<MeshRenderer>().sharedMaterial = orange;
        CreateLabel("Zero", "0", new Vector3(0.2f, 1.95f, -0.02f), Color.white, 0.3f, flag);
    }

    static Mesh GetOrCreatePennantMesh()
    {
        // The first version drew both faces from one set of vertices, so their normals cancelled to nothing and the
        // flag rendered black. Each face now has its own vertices and its own normal.
        const string oldPath = MatFolder + "/PennantMesh.asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(oldPath) != null) AssetDatabase.DeleteAsset(oldPath);
        string path = MatFolder + "/PennantFlag.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        var top = new Vector3(0f, 0.28f, 0f);
        var tip = new Vector3(0.62f, 0f, 0f);
        var bottom = new Vector3(0f, -0.28f, 0f);
        mesh = new Mesh { name = "PennantFlag" };
        mesh.vertices = new[] { top, tip, bottom, top, bottom, tip };
        mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.forward, Vector3.forward, Vector3.forward };
        mesh.uv = new Vector2[6];
        mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };   // front faces the player at the start; the back reads from behind
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    /// One sphere slot: a grabbable root (collider, kinematic body, XR grab) holding the visible ball and a floating tag
    /// that faces the player, plus a marker (a beam and a ring) showing its place on the line. Its number, colours, tag
    /// and marker are set by FetchSphere.SetHome each time a level is laid out.
    static FetchSphere BuildFetchSphere(Transform root, int slot, Material coral, Material blue, Material coralTag, Material blueTag)
    {
        var go = new GameObject($"Sphere_{slot}");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        var collider = go.AddComponent<SphereCollider>();
        collider.radius = 0.38f;
        var body = CreatePrimitive(PrimitiveType.Sphere, "Body", Vector3.zero, Vector3.one * 0.6f, coral, go.transform);

        var rigidbody = go.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        var grab = go.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.throwOnDetach = false;
        grab.trackRotation = false;
        grab.attachEaseInTime = 0.1f;
        grab.farAttachMode = InteractableFarAttachMode.Near;   // a sphere picked up with the ray flies to your hand

        var tag = new GameObject("Tag").transform;
        tag.SetParent(go.transform, false);
        tag.localPosition = new Vector3(0f, 0.62f, 0f);
        tag.gameObject.AddComponent<Billboard>();
        var backing = CreatePrimitive(PrimitiveType.Cube, "Backing", new Vector3(0f, 0f, 0.02f), new Vector3(1.0f, 0.26f, 0.02f), coralTag, tag);
        var tagLabel = CreateLabel("Text", "x = 0 m", new Vector3(0f, 0f, -0.02f), Color.white, 0.14f, tag);

        // The marker on the line: a beam down to a ring on the ground, moved to the sphere's number by SetHome.
        var marker = new GameObject($"Marker_{slot}").transform;
        marker.SetParent(root, false);
        var beam = CreatePrimitive(PrimitiveType.Cylinder, "Beam", new Vector3(0f, 0.46f, 0f), new Vector3(0.03f, 0.42f, 0.03f), coral, marker);
        var ring = BuildRing(marker, "HomeRing", new Vector3(0f, 0.05f, 0f), 0.3f, coral, 0.04f);
        var ringParts = ring.GetComponentsInChildren<Renderer>();

        // A tall beam with the number on top, for levels where the spheres are far away: the one at +9 is beyond easy sight.
        var pillar = new GameObject("Pillar").transform;
        pillar.SetParent(marker, false);
        var pillarBeam = CreatePrimitive(PrimitiveType.Cylinder, "Beam", new Vector3(0f, 2.4f, 0f), new Vector3(0.025f, 2.4f, 0.025f), coral, pillar);
        var pillarNumber = CreateLabel("Number", "", new Vector3(0f, 5.1f, 0f), Color.white, 0.6f, pillar);
        pillarNumber.AddComponent<Billboard>();
        pillar.gameObject.SetActive(false);

        var parts = new Renderer[ringParts.Length + 2];
        parts[0] = beam.GetComponent<Renderer>();
        parts[1] = pillarBeam.GetComponent<Renderer>();
        ringParts.CopyTo(parts, 2);

        var sphere = go.AddComponent<FetchSphere>();
        sphere.homeLocalPosition = go.transform.localPosition;
        sphere.body = body.GetComponent<Renderer>();
        sphere.tagText = tagLabel.GetComponent<TextMesh>();
        sphere.tagBacking = backing.GetComponent<Renderer>();
        sphere.marker = marker;
        sphere.markerParts = parts;
        sphere.pillar = pillar;
        sphere.pillarNumber = pillarNumber.GetComponent<TextMesh>();
        sphere.negativeMarker = coral;
        sphere.positiveMarker = blue;
        sphere.negativeTag = coralTag;
        sphere.positiveTag = blueTag;
        return sphere;
    }

    /// The table: a wooden frame with a slate top tilted towards the player, four glowing spot slots on it, and the
    /// maker gantry behind them. The table's front edge is close enough to the origin ring that standing in the ring
    /// puts the spots within reach, which is what makes "start and end every trip at the origin" natural. Where the
    /// spots sit, and how many are in use, is set per level by LineLevelController.
    static void BuildTable(Transform root, Material wood, Material post, Material slate, Material steel, Material screen,
        Material cyan, Material cyanHot, Material blueprint,
        out TableSocket[] sockets, out TextMesh title, out TextMesh count, out TextMesh note,
        out Transform[] blueprintBars, out Transform[] weldBars, out Transform tableRoot, out Transform raisedPost)
    {
        var table = new GameObject("Table").transform;
        table.SetParent(root, false);
        tableRoot = table;

        foreach (var leg in new[] { new Vector2(-2.0f, 1.0f), new Vector2(2.0f, 1.0f), new Vector2(-2.0f, 2.7f), new Vector2(2.0f, 2.7f) })
            CreatePrimitive(PrimitiveType.Cube, "Leg", new Vector3(leg.x, 0.45f, leg.y), new Vector3(0.16f, 0.9f, 0.16f), post, table);
        CreatePrimitive(PrimitiveType.Cube, "ApronFront", new Vector3(0f, 0.8f, 0.95f), new Vector3(4.2f, 0.2f, 0.08f), wood, table);
        CreatePrimitive(PrimitiveType.Cube, "ApronBack", new Vector3(0f, 0.8f, 2.75f), new Vector3(4.2f, 0.2f, 0.08f), wood, table);

        // Solid block so the player's body stops at the table instead of walking through it.
        var block = new GameObject("TableCollider");
        block.transform.SetParent(table, false);
        var blockCollider = block.AddComponent<BoxCollider>();
        blockCollider.center = new Vector3(0f, 0.5f, 1.85f);
        blockCollider.size = new Vector3(4.2f, 1.0f, 1.9f);

        // The working surface: an unscaled pivot (so rings stay round) tilted 10 degrees, back edge raised.
        var surface = new GameObject("Surface").transform;
        surface.SetParent(table, false);
        surface.localPosition = new Vector3(0f, 0.98f, 1.85f);
        surface.localRotation = Quaternion.Euler(-10f, 0f, 0f);
        CreatePrimitive(PrimitiveType.Cube, "Slab", Vector3.zero, new Vector3(4.3f, 0.1f, 2.0f), slate, surface);
        CreatePrimitive(PrimitiveType.Cube, "TrimFront", new Vector3(0f, 0f, -1.0f), new Vector3(4.36f, 0.13f, 0.07f), wood, surface);

        sockets = new TableSocket[4];
        for (int slot = 0; slot < sockets.Length; slot++) sockets[slot] = BuildSocket(surface, slot, cyan, cyanHot);

        // Bars for the figure: a faint outline under the spots, and the glowing figure that appears when it is done.
        // LineLevelController lays them along the edges of whatever figure the level makes and hides the ones it does not need.
        blueprintBars = new Transform[4];
        weldBars = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            blueprintBars[i] = CreatePrimitive(PrimitiveType.Cube, "Blueprint_" + i, new Vector3(0f, 0.056f, 0f), new Vector3(0.03f, 0.006f, 0.5f), blueprint, surface).transform;
            weldBars[i] = CreatePrimitive(PrimitiveType.Cube, "Weld_" + i, new Vector3(0f, 0.36f, 0f), new Vector3(0.05f, 0.05f, 0.5f), cyanHot, surface).transform;
            weldBars[i].gameObject.SetActive(false);
        }

        // The post under a raised spot (the top of the triangle): a shaft and a cap. LineLevelController sizes and
        // places them for the level, and hides them when no spot is raised.
        raisedPost = new GameObject("Post").transform;
        raisedPost.SetParent(surface, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Shaft", new Vector3(0f, 0.2f, 0f), new Vector3(0.07f, 0.15f, 0.07f), steel, raisedPost);
        CreatePrimitive(PrimitiveType.Cylinder, "Cap", new Vector3(0f, 0.3f, 0f), new Vector3(0.72f, 0.012f, 0.72f), slate, raisedPost);
        raisedPost.gameObject.SetActive(false);

        // The gantry: two pillars, a bar, a nozzle and a display that reads "LINE MAKER  n / 2".
        var gantry = new GameObject("LineMaker").transform;
        gantry.SetParent(table, false);
        gantry.localPosition = new Vector3(0f, 1.1f, 2.75f);
        foreach (float x in new[] { -1.95f, 1.95f })
            CreatePrimitive(PrimitiveType.Cube, "Pillar", new Vector3(x, 0.65f, 0f), new Vector3(0.14f, 1.3f, 0.14f), steel, gantry);
        CreatePrimitive(PrimitiveType.Cube, "Bar", new Vector3(0f, 1.3f, 0f), new Vector3(4.1f, 0.16f, 0.16f), steel, gantry);
        CreatePrimitive(PrimitiveType.Cylinder, "Nozzle", new Vector3(0f, 1.15f, 0f), new Vector3(0.12f, 0.09f, 0.12f), steel, gantry);
        CreatePrimitive(PrimitiveType.Cube, "Display", new Vector3(0f, 1.9f, 0f), new Vector3(1.8f, 0.66f, 0.06f), screen, gantry);
        title = CreateLabel("Title", "LINE MAKER", new Vector3(0f, 2.1f, -0.045f), new Color(0.36f, 0.88f, 0.9f), 0.125f, gantry).GetComponent<TextMesh>();
        count = CreateLabel("Count", "0 / 2", new Vector3(0f, 1.91f, -0.045f), new Color(0.24f, 0.88f, 0.5f), 0.21f, gantry).GetComponent<TextMesh>();
        note = CreateLabel("Note", "", new Vector3(0f, 1.71f, -0.045f), new Color(0.96f, 0.82f, 0.45f), 0.11f, gantry).GetComponent<TextMesh>();
    }

    /// One spot slot: a glowing ring on the table surface with its number painted inside, and an anchor above it where
    /// a placed sphere settles. TableSocket.SetSpot gives it its place and number for each level.
    static TableSocket BuildSocket(Transform surface, int slot, Material idle, Material hot)
    {
        var socket = new GameObject($"Socket_{slot}").transform;
        socket.SetParent(surface, false);
        socket.localPosition = new Vector3(0f, 0.06f, 0f);
        var ring = BuildRing(socket, "Ring", Vector3.zero, 0.32f, idle, 0.06f);
        var anchor = new GameObject("Anchor").transform;
        anchor.SetParent(socket, false);
        anchor.localPosition = new Vector3(0f, 0.3f, 0f);   // the sphere's centre sits one radius above the surface
        var stamp = CreateLabel("Stamp", "", new Vector3(0f, 0.005f, 0f), Color.white, 0.3f, socket);
        stamp.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var component = socket.gameObject.AddComponent<TableSocket>();
        component.anchor = anchor;
        component.stamp = stamp.GetComponent<TextMesh>();
        component.ring = ring.GetComponentsInChildren<Renderer>();
        component.idleMaterial = idle;
        component.hotMaterial = hot;
        return component;
    }

    /// Crates, a barrel and a lantern beside the table, so it reads as a workshop corner.
    static void BuildProps(Transform root, Material wood, Material post, Material steel, Material lantern)
    {
        var props = new GameObject("Props").transform;
        props.SetParent(root, false);
        CreatePrimitive(PrimitiveType.Cube, "Crate_A", new Vector3(3.1f, 0.35f, 1.7f), new Vector3(0.7f, 0.7f, 0.7f), post, props);
        var crateB = CreatePrimitive(PrimitiveType.Cube, "Crate_B", new Vector3(3.6f, 0.26f, 2.4f), new Vector3(0.52f, 0.52f, 0.52f), wood, props);
        crateB.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
        CreatePrimitive(PrimitiveType.Cylinder, "Barrel", new Vector3(-3.1f, 0.42f, 1.8f), new Vector3(0.5f, 0.42f, 0.5f), wood, props);
        CreatePrimitive(PrimitiveType.Cylinder, "BarrelBand_A", new Vector3(-3.1f, 0.62f, 1.8f), new Vector3(0.53f, 0.02f, 0.53f), steel, props);
        CreatePrimitive(PrimitiveType.Cylinder, "BarrelBand_B", new Vector3(-3.1f, 0.24f, 1.8f), new Vector3(0.53f, 0.02f, 0.53f), steel, props);

        // Lantern on the first crate.
        CreatePrimitive(PrimitiveType.Cylinder, "LanternBase", new Vector3(3.1f, 0.72f, 1.7f), new Vector3(0.16f, 0.03f, 0.16f), steel, props);
        CreatePrimitive(PrimitiveType.Sphere, "LanternGlow", new Vector3(3.1f, 0.86f, 1.7f), Vector3.one * 0.2f, lantern, props);
        var light = new GameObject("LanternLight").AddComponent<Light>();
        light.transform.SetParent(props, false);
        light.transform.localPosition = new Vector3(3.1f, 0.9f, 1.7f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.75f, 0.4f);
        light.intensity = 1.6f;
        light.range = 3.5f;
    }

    // ---------- world-space HUD ----------

    /// Everything the flow needs from the popups: the topic, the Level N slide, "Level N completed", a question with
    /// four options, the quiz result, and the short messages.
    class LevelHud
    {
        public GameObject topicPanel, slidePanel, completePanel, quizPanel, resultPanel, toastBox;
        public Text slideHeader, slideBody, slideHint;
        public Text[] slideChips;
        public Button markButton;
        public Text markText;
        public GameObject promptBox;
        public Text promptText;
        public Button startButton, continueButton, resultButton;
        public Text completeTitle, completeFigure, completePath, completeDisplacement, completeStars, completeLesson;
        public Text quizHeader, quizPrompt, quizFeedback;
        public Button[] optionButtons;
        public Text[] optionTexts;
        public Text resultTitle, resultMessage, resultButtonText;
        public Text toastText, positionText;
    }

    static LevelHud BuildLevelHud(Transform root, Transform xrCamera, out NumberLinePlayground.Flow.PopupManager popupManager)
    {
        // A world-space popup that floats ~2m ahead and a little above eye level and gently follows the player's
        // gaze (see PopupManager) rather than a flat screen overlay. A Screen Space Overlay canvas always draws on
        // top of the entire 3D scene with no way for anything in the world, including the player's own hand
        // controllers, to render in front of it; world-space participates in normal depth testing.
        // Both raycasters, not one instead of the other: GraphicRaycaster handles regular mouse/pointer clicks
        // (the Simulator and Editor testing), while TrackedDeviceGraphicRaycaster separately handles the XR
        // controllers' 3D rays. TrackedDeviceGraphicRaycaster is NOT a drop-in replacement for GraphicRaycaster.
        var go = new GameObject("InstructionPopup", typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster), typeof(UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster));
        go.transform.SetParent(root, false);
        // Start where PopupManager will float it (two metres ahead of the player's head, a little above eye level).
        if (xrCamera != null)
        {
            go.transform.position = xrCamera.position + root.forward * 2f + Vector3.up * 0.35f;
            go.transform.rotation = root.rotation;
        }
        else go.transform.localPosition = new Vector3(0f, 1.9f, -4.5f);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = xrCamera != null ? xrCamera.GetComponent<Camera>() : null;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
        go.transform.localScale = Vector3.one * 0.0011f; // ~2.1m wide floating panel
        popupManager = go.AddComponent<NumberLinePlayground.Flow.PopupManager>();
        popupManager.Configure(2f, 0.35f);
        if (xrCamera != null) popupManager.SetPlayerCamera(xrCamera);

        var hud = new LevelHud();
        var center = Vector2.one * .5f;
        var ink = new Color(.06f, .1f, .25f);

        // ---- The topic: the first thing shown after Play. Big, bold, black text on a white card. ----
        hud.topicPanel = CreateUiPanel(go.transform, "TopicPanel", Color.clear, center, center, new Vector2(0, 40), new Vector2(1500, 520));
        var topicBorder = CreateUiPanel(hud.topicPanel.transform, "Border", new Color(.11f, .31f, .85f), center, center, Vector2.zero, new Vector2(1500, 520));
        var topicCard = CreateUiPanel(topicBorder.transform, "Card", Color.white, center, center, Vector2.zero, new Vector2(1468, 488));
        var topicText = BoardText(topicCard.transform, "Position, Origin\nand Direction", 150, Color.black, Vector2.zero, new Vector2(1400, 420));
        topicText.verticalOverflow = VerticalWrapMode.Overflow;

        // ---- The Level N popup: what to do in this level, with a Start button. It vanishes when Start is pressed. ----
        hud.slidePanel = CreateUiPanel(go.transform, "SlidePanel", Color.clear, center, center, new Vector2(0, 120), new Vector2(980, 540));
        var frame = CreateUiPanel(hud.slidePanel.transform, "Frame", new Color(.11f, .31f, .85f), center, center, Vector2.zero, new Vector2(980, 540));
        var header = CreateUiPanel(frame.transform, "Header", new Color(.17f, .39f, .92f), center, center, new Vector2(0, 228), new Vector2(960, 84));
        hud.slideHeader = BoardText(header.transform, "Level 1", 46, Color.white, Vector2.zero, new Vector2(900, 76));
        var body = CreateUiPanel(frame.transform, "Body", new Color(.93f, .95f, 1f), center, center, new Vector2(0, -38), new Vector2(940, 400));
        hud.slideBody = BoardText(body.transform, Concept.Levels[0].task, 36, ink, new Vector2(0, 118), new Vector2(880, 130));
        hud.slideHint = BoardText(body.transform, Concept.Levels[0].hint, 26, new Color(.3f, .35f, .5f), new Vector2(0, -58), new Vector2(880, 40));
        string[] chipText = Concept.Levels[0].chips;
        Color[] chipColor = { new Color(.68f, .83f, 1f), new Color(1f, .92f, .62f), new Color(.68f, .93f, .8f) };
        hud.slideChips = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var chip = CreateUiPanel(body.transform, "Chip_" + i, chipColor[i], center, center, new Vector2(-300 + i * 300, 0), new Vector2(280, 66));
            hud.slideChips[i] = BoardText(chip.transform, chipText[i], 26, ink, Vector2.zero, new Vector2(264, 60));
        }
        hud.startButton = CreateUiButton(body.transform, "START", new Vector2(0, -122), new Vector2(320, 82), new Color(.1f, .62f, .28f), Color.white, 44);
        hud.startButton.name = "StartButton";

        // ---- Level completed ----
        hud.completePanel = CreateUiPanel(go.transform, "CompletePanel", Color.clear, center, center, new Vector2(0, 120), new Vector2(900, 620));
        var completeFrame = CreateUiPanel(hud.completePanel.transform, "Frame", new Color(.09f, .55f, .25f), center, center, Vector2.zero, new Vector2(900, 620));
        hud.completeTitle = BoardText(completeFrame.transform, "Level 1 completed", 48, Color.white, new Vector2(0, 260), new Vector2(860, 80));
        var completeBody = CreateUiPanel(completeFrame.transform, "Body", new Color(.93f, .97f, .94f), center, center, new Vector2(0, -40), new Vector2(860, 480));
        hud.completeFigure = BoardText(completeBody.transform, "Line   -4 to +7  (11 m)", 40, ink, new Vector2(0, 175), new Vector2(800, 62));
        hud.completePath = BoardText(completeBody.transform, "Walked   22.0 m", 40, new Color(.72f, .42f, .02f), new Vector2(0, 108), new Vector2(800, 62));
        hud.completeDisplacement = BoardText(completeBody.transform, "Displacement   0.0 m", 40, new Color(.02f, .45f, .7f), new Vector2(0, 41), new Vector2(800, 62));
        hud.completeStars = BoardText(completeBody.transform, "***   3 / 3 stars", 38, ink, new Vector2(0, -28), new Vector2(800, 62));
        hud.completeLesson = BoardText(completeBody.transform, Concept.Levels[0].lesson, 28, new Color(.25f, .3f, .4f), new Vector2(0, -105), new Vector2(780, 110));
        hud.continueButton = CreateUiButton(completeBody.transform, "CONTINUE", new Vector2(0, -200), new Vector2(360, 76), new Color(.1f, .45f, .8f), Color.white, 40);

        // ---- A question with four options ----
        var sample = QuizBank.Get(0, 0);
        hud.quizPanel = CreateUiPanel(go.transform, "QuizPanel", Color.clear, center, center, new Vector2(0, -40), new Vector2(1250, 800));
        var quizFrame = CreateUiPanel(hud.quizPanel.transform, "Frame", new Color(.3f, .14f, .55f), center, center, Vector2.zero, new Vector2(1250, 800));
        hud.quizHeader = BoardText(quizFrame.transform, "Question 1 of 3", 52, Color.white, new Vector2(0, 350), new Vector2(1180, 84));
        var quizBody = CreateUiPanel(quizFrame.transform, "Body", new Color(.95f, .93f, .98f), center, center, new Vector2(0, -45), new Vector2(1210, 690));
        var quizInk = new Color(.15f, .08f, .25f);
        hud.quizPrompt = BoardText(quizBody.transform, sample.prompt, 44, quizInk, new Vector2(0, 250), new Vector2(1140, 140));
        hud.optionButtons = new Button[4];
        hud.optionTexts = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            var option = CreateUiButton(quizBody.transform, "Option " + (i + 1), new Vector2(0, 100 - i * 100), new Vector2(1100, 84), new Color(.96f, .96f, 1f), quizInk, 32);
            var optionText = option.GetComponentInChildren<Text>();
            optionText.text = ((char)('A' + i)).ToString() + "   " + sample.options[i];
            optionText.alignment = TextAnchor.MiddleLeft;
            optionText.rectTransform.offsetMin = new Vector2(36, 0);
            optionText.rectTransform.offsetMax = new Vector2(-24, 0);
            hud.optionButtons[i] = option;
            hud.optionTexts[i] = optionText;
        }
        hud.quizFeedback = BoardText(quizBody.transform, "", 34, new Color(.3f, .12f, .5f), new Vector2(0, -290), new Vector2(1140, 90));

        // ---- The quiz result, and the next topic ----
        hud.resultPanel = CreateUiPanel(go.transform, "ResultPanel", Color.clear, center, center, new Vector2(0, 20), new Vector2(1300, 560));
        var resultFrame = CreateUiPanel(hud.resultPanel.transform, "Frame", new Color(.09f, .38f, .72f), center, center, Vector2.zero, new Vector2(1300, 560));
        hud.resultTitle = BoardText(resultFrame.transform, "2 of 3 correct", 80, Color.white, new Vector2(0, 215), new Vector2(1240, 120));
        var resultBody = CreateUiPanel(resultFrame.transform, "Body", new Color(.93f, .96f, 1f), center, center, new Vector2(0, -60), new Vector2(1260, 380));
        hud.resultMessage = BoardText(resultBody.transform, "Back to Level 3.\nThen 1 question again.", 52, ink, new Vector2(0, 60), new Vector2(1180, 230));
        hud.resultButton = CreateUiButton(resultBody.transform, "PLAY AGAIN", new Vector2(0, -130), new Vector2(460, 96), new Color(.1f, .62f, .28f), Color.white, 44);
        hud.resultButtonText = hud.resultButton.GetComponentInChildren<Text>();

        // Every popup can fade. The saved scene shows the Level 1 popup; ConceptFlow shows the others when their turn comes.
        foreach (var panel in new[] { hud.topicPanel, hud.slidePanel, hud.completePanel, hud.quizPanel, hud.resultPanel })
            if (panel.GetComponent<CanvasGroup>() == null) panel.AddComponent<CanvasGroup>();
        foreach (var panel in new[] { hud.topicPanel, hud.completePanel, hud.quizPanel, hud.resultPanel })
            panel.SetActive(false);

        // ---- Short messages ("One sphere at a time") and the live position badge ----
        hud.toastBox = CreateUiPanel(go.transform, "Toast", new Color(0f, 0f, 0f, .72f), center, center, new Vector2(0, -420), new Vector2(1500, 170));
        hud.toastText = BoardText(hud.toastBox.transform, "", 58, Color.white, Vector2.zero, new Vector2(1460, 160));
        hud.toastBox.SetActive(false);
        // A line that says what G would do right now, so it is never a mystery why nothing happened.
        hud.promptBox = CreateUiPanel(go.transform, "GPrompt", new Color(0f, 0f, 0f, .55f), center, center, new Vector2(0, -540), new Vector2(1300, 64));
        hud.promptText = BoardText(hud.promptBox.transform, "", 40, new Color(1f, .9f, .6f), Vector2.zero, new Vector2(1280, 60));
        hud.promptBox.SetActive(false);
        var badge = CreateUiPanel(go.transform, "PositionBadge", new Color(0f, 0f, 0f, .55f), center, center, new Vector2(0, -640), new Vector2(560, 84));
        hud.positionText = BoardText(badge.transform, "x = 0.0 m", 46, Color.white, Vector2.zero, new Vector2(540, 76));

        // SET MARK (offered on replays of the square level) and the distance from the mark it drops. Low in view: look
        // down to press it with the ray.
        hud.markText = BoardText(go.transform, "from mark  0.0 m", 44, new Color(1f, .82f, .35f), new Vector2(0, -890), new Vector2(700, 64));
        hud.markText.gameObject.SetActive(false);
        hud.markButton = CreateUiButton(go.transform, "SET MARK", new Vector2(0, -970), new Vector2(380, 80), new Color(.85f, .55f, .1f), Color.white, 38);
        hud.markButton.gameObject.SetActive(false);
        return hud;
    }

    /// Droplets for a sphere that lands after a wrong pair. LineLevelController emits them where the sphere arrives.
    static ParticleSystem BuildSplash(Transform root)
    {
        var go = new GameObject("Splash");
        go.transform.SetParent(root, false);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // a cone fires along its own z: turn it to fire up
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.8f;
        main.startSpeed = 2.4f;
        main.startSize = 0.07f;
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.9f, 1f), new Color(0.35f, 0.65f, 1f));

        var emission = ps.emission;
        emission.rateOverTime = 0f;                                    // emitted by hand, where the sphere lands

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.15f;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
        ps.GetComponent<ParticleSystemRenderer>().material = new Material(shader);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    /// The marker the SET MARK button drops on the line: a flat amber disc with a short pole.
    static Transform BuildMark(Transform root, Material amber)
    {
        var mark = new GameObject("Mark").transform;
        mark.SetParent(root, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Disc", new Vector3(0f, 0.01f, 0f), new Vector3(0.6f, 0.01f, 0.6f), amber, mark);
        CreatePrimitive(PrimitiveType.Cylinder, "Pole", new Vector3(0f, 0.5f, 0f), new Vector3(0.03f, 0.5f, 0.03f), amber, mark);
        mark.gameObject.SetActive(false);
        return mark;
    }

    /// Where a sphere picked up with G rides: low and to the right, a little ahead of the player's eyes.
    static Transform BuildCarryPoint(Transform xrCamera)
    {
        var point = new GameObject("CarryPoint").transform;
        point.SetParent(xrCamera, false);
        point.localPosition = new Vector3(0.28f, -0.3f, 0.8f);
        return point;
    }

    /// A black veil a little way in front of the player's eyes, used to hide the jump back to the starting point.
    static ScreenFade BuildScreenFade(Transform xrCamera)
    {
        var holder = new GameObject("ScreenFade");
        holder.transform.SetParent(xrCamera, false);
        var fade = holder.AddComponent<ScreenFade>();

        var canvasObject = new GameObject("VeilCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(holder.transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, 0f, 0.3f);
        canvasObject.transform.localScale = Vector3.one * 0.001f;
        canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1600, 1600);   // 1.6 m across, 0.3 m from the eyes
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        var veilObject = new GameObject("Veil", typeof(RectTransform), typeof(Image));
        veilObject.transform.SetParent(canvasObject.transform, false);
        var rect = veilObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var veil = veilObject.GetComponent<Image>();
        veil.color = new Color(0f, 0f, 0f, 0f);
        veil.raycastTarget = false;

        fade.veil = veil;
        fade.veilCanvas = canvasObject;
        canvasObject.SetActive(false);
        return fade;
    }

    static Button CreateUiButton(Transform parent, string text, Vector2 position, Vector2 size, Color background, Color foreground, int fontSize = 30)
    {
        var go = new GameObject("Button_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.one * .5f;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = background;

        var label = CreateUiText(go.transform, text, fontSize, foreground, FontStyle.Bold);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.anchoredPosition = Vector2.zero;
        label.rectTransform.sizeDelta = Vector2.zero;
        label.raycastTarget = false;

        return go.GetComponent<Button>();
    }

    static ParticleSystem BuildConfetti(Transform parent)
    {
        var go = new GameObject("Confetti");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, 1.5f);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 3f;
        main.loop = false;
        main.playOnAwake = false; // only fire when LineLevelController completes the level
        main.startLifetime = 2.5f;
        main.startSpeed = 4f;
        main.startSize = 0.12f;
        main.gravityModifier = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(BuildConfettiGradient());

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 200) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.25f;

        // URP-compatible particle shader — the legacy "Particles/Standard Unlit" is a
        // Built-in-RP shader and would render pink (or null) under URP.
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
        ps.GetComponent<ParticleSystemRenderer>().material = new Material(shader);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    static Gradient BuildConfettiGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(.95f, .2f, .25f), 0f),
                new GradientColorKey(new Color(.2f, .5f, 1f), .33f),
                new GradientColorKey(new Color(1f, .85f, .15f), .66f),
                new GradientColorKey(new Color(.25f, .8f, .35f), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    static Text BoardText(Transform parent, string text, int size, Color color, Vector2 position, Vector2 dimensions)
    {
        var t = CreateUiText(parent, text, size, color, FontStyle.Bold);
        t.rectTransform.anchoredPosition = position;
        t.rectTransform.sizeDelta = dimensions;
        t.raycastTarget = false;
        return t;
    }

    static Transform BuildRing(Transform parent, string name, Vector3 position, float radius, Material material, float thickness = 0.045f)
    {
        var ring = new GameObject(name).transform;
        ring.SetParent(parent, false);
        ring.localPosition = position;
        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2 / 48;
            var part = CreatePrimitive(PrimitiveType.Cube, "RingSegment", new Vector3(Mathf.Cos(angle)*radius, 0, Mathf.Sin(angle)*radius), new Vector3(thickness, .025f, radius*.15f), material, ring);
            part.transform.localRotation = Quaternion.Euler(0, -angle*Mathf.Rad2Deg, 0);
        }
        return ring;
    }

    static Transform BuildArrow(Transform root, string name, Vector3 p, int direction, Material material, bool ground)
    {
        var arrow = new GameObject(name).transform;
        arrow.SetParent(root, false);
        arrow.localPosition = p;
        if (ground) arrow.localRotation = Quaternion.Euler(90, 0, 0);
        CreatePrimitive(PrimitiveType.Cube, "Shaft", Vector3.zero, new Vector3(.6f, .045f, .025f), material, arrow);
        for (int i = -1; i <= 1; i += 2)
        {
            var arm = CreatePrimitive(PrimitiveType.Cube, "Tip", new Vector3(direction*.22f, i*.07f, 0), new Vector3(.24f, .045f, .025f), material, arrow);
            arm.transform.localRotation = Quaternion.Euler(0, 0, -direction*i*40);
        }
        return arrow;
    }

    static void BuildClearing(Transform root, Material wood, Material post)
    {
        // Clear only vegetation intersecting the activity area; retain the source terrain.
        foreach (string group in new[] { "Vegetation", "Trees" })
        {
            var vegetation = GameObject.Find(group);
            if (vegetation == null) continue;
            foreach (var renderer in vegetation.GetComponentsInChildren<MeshRenderer>())
            {
                var p = root.InverseTransformPoint(renderer.bounds.center);
                if (Mathf.Abs(p.x) < 11.5f && p.z > -8 && p.z < 4) renderer.gameObject.SetActive(false);
            }
        }
        var sand = GetOrCreateMat("NLP_Sand", new Color(.63f, .43f, .23f));
        var ground = CreatePrimitive(PrimitiveType.Cube, "ActivityGround", new Vector3(0, -.13f, -1), new Vector3(23, .25f, 16), sand, root);
        ground.AddComponent<BoxCollider>();
        // Reuse the project's low-poly forest assets around the clear walking area.
        string fir = FindAssetPath("Fir_Tree_1");
        string rock = FindAssetPath("Rock_1");
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 6; i++)
            {
                if (fir != null) PlaceScenery(fir, root, new Vector3(side * (12.5f + i % 2 * 2), 0, 1 + i * 3.5f), 4.5f + i % 3);
                if (rock != null && i < 3) PlaceScenery(rock, root, new Vector3(side * (12.5f + i), 0, 4 + i * 4), 1.8f + i * .3f);
            }
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 4; i++)
            {
                float x = side * (2.5f + i*1.5f);
                CreatePrimitive(PrimitiveType.Cube, "FencePost", new Vector3(x, .55f, 4.7f), new Vector3(.16f, 1.1f, .16f), post, root);
                if (i < 3)
                    for (int j=0; j<2; j++)
                        CreatePrimitive(PrimitiveType.Cube, "FenceRail", new Vector3(x+side*.75f, .35f+j*.4f, 4.7f), new Vector3(1.5f, .12f, .1f), wood, root);
            }
    }

    static void PlaceScenery(string path, Transform root, Vector3 position, float height)
    {
        var instance = InstantiatePrefab(path, root, position, Quaternion.identity);
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        instance.transform.localScale *= height / Mathf.Max(.01f, bounds.size.y);
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        var world = instance.transform.position;
        world.y += GetGroundY(world.x, world.z) - bounds.min.y;
        instance.transform.position = world;
    }

    static GameObject CreateUiPanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Text CreateUiText(Transform parent, string text, int fontSize, Color color, FontStyle style)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380f, fontSize + 14f);
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return t;
    }
}
