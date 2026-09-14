using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Builds the "Number Line Playground" demo environment (see Assets/Docs/Position and Scale.png)
/// into a new scene duplicated from the HQP Low Poly Trees Demo_07 environment.
/// Run via Tools > Number Line Playground > Build Environment.
public static class NumberLinePlaygroundBuilder
{
    const string SourceScenePath = "Assets/HQP STUDIOS/Low Poly Trees and Vegetation - Pack/Demo/Demo_07.unity";
    const string NewScenePath = "Assets/Free Low Poly Game Assets/Scene/NumberLinePlayground.unity";
    const string MatFolder = "Assets/Free Low Poly Game Assets/Materials/NumberLinePlayground";

    const string ArrowPrefabPath = "Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Icon_Arrow_Small_01.prefab";

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
        EnsureEventSystem(); // needed for mouse/gamepad clicks on the popup's Start button
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
        for (int x = -8; x <= 8; x += 2)
            for (int z = -9; z <= 7; z += 2)
            {
                var sample = centerXZ + heading * new Vector3(x, 0, z);
                groundY = Mathf.Max(groundY, GetGroundY(sample.x, sample.z));
            }
        groundY += .06f;

        root.position = new Vector3(centerXZ.x, groundY, centerXZ.z);
        root.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

        Vector3 basePos = Vector3.zero;

        // ---- materials ----
        var matRed = GetOrCreateMat("NLP_Red", new Color(1f, 0.06f, 0.18f), emissive: true);
        var matBlue = GetOrCreateMat("NLP_Blue", new Color(0.05f, 0.35f, 1f), emissive: true);
        var matWhite = GetOrCreateMat("NLP_White", Color.white);
        var matOrange = GetOrCreateMat("NLP_Orange", new Color(0.95f, 0.55f, 0.1f));
        var matGold = GetOrCreateMat("NLP_Gold", new Color(1f, 0.82f, 0.15f), emissive: true, doubleSided: true);
        var matWood = GetOrCreateMat("NLP_Wood", new Color(0.42f, 0.27f, 0.15f));
        var matPost = GetOrCreateMat("NLP_Post", new Color(0.33f, 0.21f, 0.11f));

        BuildClearing(root, matWood, matPost);
        var lineRoot = new GameObject("NumberLine").transform;
        lineRoot.SetParent(root, false);

        const float boxSize = 0.85f;
        const float boxHeight = 0.22f;
        for (int n = -5; n <= 5; n++)
        {
            var m = n < 0 ? matRed : n > 0 ? matBlue : matWhite;
            CreatePrimitive(PrimitiveType.Cube, $"Box_{n}", new Vector3(n, boxHeight * 0.5f, 0f),
                new Vector3(boxSize, boxHeight, boxSize), m, lineRoot);

            var label = CreateLabel($"Label_{n}", n.ToString(), new Vector3(n, boxHeight + 0.01f, 0f),
                n == 0 ? Color.black : Color.white, 0.34f, lineRoot);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        BuildArrow(lineRoot, "NegativeArrow", new Vector3(-5.5f, 0.04f, 0), -1, matRed, true);
        BuildArrow(lineRoot, "PositiveArrow", new Vector3(5.5f, 0.04f, 0), 1, matBlue, true);

        // Zero flagpole
        var flagRoot = new GameObject("ZeroFlag").transform;
        flagRoot.SetParent(root, false);
        flagRoot.localPosition = basePos;
        CreatePrimitive(PrimitiveType.Cylinder, "Pole", basePos + Vector3.up * 0.9f,
            new Vector3(0.04f, 0.9f, 0.04f), matWhite, flagRoot);
        var flagQuad = CreatePrimitive(PrimitiveType.Cube, "Flag", basePos + new Vector3(0.3f, 1.55f, 0f),
            new Vector3(0.55f, 0.3f, 0.02f), matOrange, flagRoot);
        CreateLabel("FlagLabel", "0", basePos + new Vector3(0.3f, 1.55f, -0.025f), Color.white, 0.3f, flagRoot);

        // ---- First-person VR player: the real XR Interaction Toolkit rig, so controllers
        // are visible and drivable with mouse/keyboard (via the XR Device Simulator) in the
        // Editor Game view without needing a physical headset. ----
        string xrOriginPath = FindAssetPath("XR Origin (XR Rig)");
        string simulatorPath = FindAssetPath("XR Device Simulator");

        Transform xrCamera = null;
        if (xrOriginPath != null)
        {
            Vector3 playerStart = basePos + new Vector3(0f, 0.08f, -6.5f); // stand back from the "0" flag, facing the line
            GameObject xrOrigin = InstantiatePrefab(xrOriginPath, root, playerStart, Quaternion.identity);
            xrOrigin.name = "XR Origin (Player)";
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

        // Level 1 "Find Your Spot" coin hunt: one coin at each of +3, -2, 0, -5.
        // Picking one up shows a caption with that coin's position (see NumberLineTracker).
        var coinsParent = new GameObject("Coins").transform;
        coinsParent.SetParent(root, false);
        int[] coinValues = { 3, -2, 0, -5 };
        Mesh coinMesh = GetOrCreateStarMesh();
        foreach (int value in coinValues)
            BuildCoin(coinsParent, value, coinMesh, matGold);

        // Remediation coin for the checkpoint question: pre-built but hidden, and lives
        // outside the "Coins" container so it isn't counted as one of the main four.
        GameObject remediationCoin = BuildCoin(root, -4, coinMesh, matGold);
        remediationCoin.name = "RemediationCoin";
        remediationCoin.SetActive(false);
        var remediationRing = root.Find("CoinRing_-4");
        if (remediationRing != null) Object.DestroyImmediate(remediationRing.gameObject);

        // Entrance sign
        Vector3 signBasePos = basePos + new Vector3(-4.7f, 0f, 2.8f);
        BuildSignPost(root, "EntranceSign", signBasePos, 3.5f, 3.1f, 1.35f, matPost, matWood,
            new[] { ("Number Line\nPlayground", 0.36f, Color.white), ("Move  •  Explore  •  Learn", 0.16f, new Color(0.95f, 0.9f, 0.8f)) });

        // Side signs
        Vector3 leftSignPos = basePos + new Vector3(-5.6f, 0f, 1.4f);
        BuildSignPost(root, "SignLeft", leftSignPos, 1.5f, 1.4f, 0.85f, matPost, matWood,
            new[] { ("Negative\nnumbers", 0.19f, Color.white) });
        BuildArrow(root, "LeftSignArrow", leftSignPos + new Vector3(0, .57f, -.07f), -1, matWhite, false);

        Vector3 rightSignPos = basePos + new Vector3(5.6f, 0f, 1.4f);
        BuildSignPost(root, "SignRight", rightSignPos, 1.5f, 1.4f, 0.85f, matPost, matWood,
            new[] { ("Positive\nnumbers", 0.19f, Color.white) });
        BuildArrow(root, "RightSignArrow", rightSignPos + new Vector3(0, .57f, -.07f), 1, matWhite, false);

        // HUD: intro popup (Start button) -> live sign-colored position label + coin
        // progress while playing -> checkpoint question once all coins are collected ->
        // confetti + congratulations once it's answered correctly.
        BuildHud(root, coinValues.Length, xrCamera,
            out Text currentPosText, out Text levelText, out Text progressText, out Text bodyText, out Text captionText,
            out GameObject introPanel, out Button startButton,
            out GameObject checkpointPanel, out Text checkpointStarsText, out Text checkpointQuestionText, out Text checkpointFeedbackText,
            out Button checkpointPositiveButton, out Button checkpointNegativeButton, out Button checkpointZeroButton, out Button checkpointContinueButton,
            out GameObject congratsPanel, out Text congratsText, out ParticleSystem confetti);

        // Wire the live tracker so the HUD reacts as the player walks along the number line
        // and collects coins.
        if (xrCamera != null && currentPosText != null)
        {
            var tracker = root.gameObject.AddComponent<NumberLineTracker>();
            tracker.Init(xrCamera, root, lineRoot, coinsParent, remediationCoin,
                currentPosText, levelText, progressText, bodyText, captionText, introPanel, startButton,
                checkpointPanel, checkpointStarsText, checkpointQuestionText, checkpointFeedbackText,
                checkpointPositiveButton, checkpointNegativeButton, checkpointZeroButton, checkpointContinueButton,
                congratsPanel, congratsText, confetti);
        }

        var otherScenes = EditorBuildSettings.scenes.Where(s => s.path != NewScenePath && s.path != "Assets/Scenes/SampleScene.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(NewScenePath, true) }.Concat(otherScenes).ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = root.gameObject;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log("Number Line Playground built at " + NewScenePath);
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

    static Mesh BuildStarMesh(float outerRadius, float innerRadius)
    {
        const int points = 5;
        int vertCount = points * 2 + 1;
        var verts = new Vector3[vertCount * 2]; // front + back
        var uvs = new Vector2[verts.Length];
        int centerFront = 0, centerBack = vertCount;

        verts[centerFront] = Vector3.zero;
        verts[centerBack] = new Vector3(0f, 0f, -0.02f);

        for (int i = 0; i < points * 2; i++)
        {
            float angle = Mathf.PI / 2f + i * Mathf.PI / points;
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            var p = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
            verts[1 + i] = p;
            verts[centerBack + 1 + i] = p + new Vector3(0f, 0f, -0.02f);
        }
        for (int i = 0; i < verts.Length; i++) uvs[i] = Vector2.zero;

        var tris = new System.Collections.Generic.List<int>();
        for (int i = 0; i < points * 2; i++)
        {
            int a = 1 + i;
            int b = 1 + (i + 1) % (points * 2);
            tris.Add(centerFront); tris.Add(a); tris.Add(b);
            int ba = centerBack + 1 + i;
            int bb = centerBack + 1 + (i + 1) % (points * 2);
            tris.Add(centerBack); tris.Add(bb); tris.Add(ba);
        }

        var mesh = new Mesh { name = "StarMesh" };
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh GetOrCreateStarMesh()
    {
        string path = MatFolder + "/StarMesh.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = BuildStarMesh(.4f, .17f);
            AssetDatabase.CreateAsset(mesh, path);
        }
        return mesh;
    }

    /// One Level-1 coin: a spinning gold star hovering over its position, with a ring on
    /// the ground beneath it. Named "Coin_{value}" so NumberLineTracker can find it and its
    /// value at runtime without a separate lookup table.
    static GameObject BuildCoin(Transform parent, int value, Mesh mesh, Material material)
    {
        var coin = new GameObject($"Coin_{value}");
        coin.transform.SetParent(parent, false);
        coin.transform.localPosition = new Vector3(value, .65f, 0f);
        var mf = coin.AddComponent<MeshFilter>();
        var mr = coin.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = material;
        coin.AddComponent<Spin>();
        BuildRing(parent, $"CoinRing_{value}", new Vector3(value, .09f, 0f), .42f, material);
        return coin;
    }

    static void BuildHud(Transform root, int totalCoins, Transform xrCamera,
        out Text currentText, out Text levelText, out Text progressText, out Text bodyText, out Text captionText,
        out GameObject introPanel, out Button startButton,
        out GameObject checkpointPanel, out Text checkpointStarsText, out Text checkpointQuestionText, out Text checkpointFeedbackText,
        out Button checkpointPositiveButton, out Button checkpointNegativeButton, out Button checkpointZeroButton, out Button checkpointContinueButton,
        out GameObject congratsPanel, out Text congratsText, out ParticleSystem confetti)
    {
        // A world-space popup that floats ~1.5m in front of the player and gently follows
        // their gaze (see PopupManager) rather than a flat screen overlay. A Screen Space
        // Overlay canvas always draws on top of the entire 3D scene with no way for anything
        // in the world — including the player's own hand controllers — to render in front of
        // it; world-space participates in normal depth testing, so closer things (like your
        // hands) correctly occlude it instead of disappearing behind it.
        // Both raycasters, not one instead of the other: GraphicRaycaster handles regular
        // mouse/pointer clicks (what the Simulator and Editor testing use), while
        // TrackedDeviceGraphicRaycaster separately handles the XR controllers' 3D rays.
        // TrackedDeviceGraphicRaycaster extends BaseRaycaster directly — it is NOT a drop-in
        // replacement for GraphicRaycaster and doesn't process mouse pointer events at all, so
        // having only it (as a previous fix did) silently broke every mouse click on this popup.
        var go = new GameObject("InstructionPopup", typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster), typeof(UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster));
        go.transform.SetParent(root, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = xrCamera != null ? xrCamera.GetComponent<Camera>() : null;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
        go.transform.localScale = Vector3.one * 0.0011f; // ~2.1m wide floating panel
        var popupManager = go.AddComponent<NumberLinePlayground.Flow.PopupManager>();
        if (xrCamera != null) popupManager.SetPlayerCamera(xrCamera);

        // ---- Intro popup: shown until the player presses Start. ----
        introPanel = CreateUiPanel(go.transform, "IntroPanel", Color.clear, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(560, 440));
        var frame = CreateUiPanel(introPanel.transform, "Frame", new Color(.04f, .17f, .75f), Vector2.one * .5f, Vector2.one * .5f, new Vector2(0, 30), new Vector2(560, 380));
        var header = CreateUiPanel(frame.transform, "Header", new Color(.05f, .18f, .8f), Vector2.one * .5f, Vector2.one * .5f, new Vector2(0, 155), new Vector2(540, 70));
        levelText = BoardText(header.transform, "Level 1: Find Your Spot", 30, Color.white, Vector2.zero, new Vector2(520, 65));
        var body = CreateUiPanel(frame.transform, "Body", new Color(.93f, .95f, 1f), Vector2.one * .5f, Vector2.one * .5f, new Vector2(0, -16), new Vector2(540, 288));
        bodyText = BoardText(body.transform, "Collect all the coins hidden along\nthe number line. Watch the label\nbelow — it shows your position.", 24, new Color(.06f, .1f, .25f), new Vector2(0, 95), new Vector2(500, 90));
        var progress = CreateUiPanel(body.transform, "Progress", new Color(1f, .87f, .6f), Vector2.one * .5f, Vector2.one * .5f, new Vector2(0, 0), new Vector2(490, 90));
        progressText = BoardText(progress.transform, $"Coins: 0 / {totalCoins}", 34, new Color(.55f, .35f, .02f), Vector2.zero, new Vector2(470, 80));
        startButton = CreateUiButton(body.transform, "Start", new Vector2(0, -115), new Vector2(200, 60), new Color(.1f, .6f, .25f), Color.white);

        // ---- Always-present small readout: the "floating label" showing live position,
        // colored red/negative, blue/positive, white/zero. ----
        var badge = CreateUiPanel(go.transform, "CurrentBadge", new Color(0f, 0f, 0f, .55f), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0, 60), new Vector2(360, 50));
        currentText = BoardText(badge.transform, "x = -- m", 26, Color.white, Vector2.zero, new Vector2(340, 45));

        // ---- Caption that briefly appears when a coin is collected ----
        var captionBox = CreateUiPanel(go.transform, "CaptionBox", new Color(0f, 0f, 0f, .6f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 220), new Vector2(560, 60));
        captionText = BoardText(captionBox.transform, "", 26, Color.white, Vector2.zero, new Vector2(540, 55));

        // ---- Checkpoint popup: shown once all coins are collected, before the level
        // actually finishes — poses a quick sign question based on where the player is. ----
        checkpointPanel = CreateUiPanel(go.transform, "CheckpointPanel", Color.clear, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(620, 480));
        var checkpointFrame = CreateUiPanel(checkpointPanel.transform, "Frame", new Color(.35f, .12f, .5f), Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(620, 480));
        checkpointStarsText = BoardText(checkpointFrame.transform, "Level 1 complete!", 26, Color.white, new Vector2(0, 195), new Vector2(580, 55));
        var checkpointBody = CreateUiPanel(checkpointFrame.transform, "Body", new Color(.95f, .93f, .98f), Vector2.one * .5f, Vector2.one * .5f, new Vector2(0, 55), new Vector2(580, 210));
        checkpointQuestionText = BoardText(checkpointBody.transform, "", 20, new Color(.15f, .08f, .2f), Vector2.zero, new Vector2(550, 200));
        checkpointFeedbackText = BoardText(checkpointFrame.transform, "", 18, new Color(1f, .9f, .5f), new Vector2(0, -90), new Vector2(580, 60));
        checkpointPositiveButton = CreateUiButton(checkpointFrame.transform, "Positive", new Vector2(-190, -175), new Vector2(170, 55), new Color(.1f, .45f, .8f), Color.white);
        checkpointNegativeButton = CreateUiButton(checkpointFrame.transform, "Negative", new Vector2(0, -175), new Vector2(170, 55), new Color(.8f, .2f, .2f), Color.white);
        checkpointZeroButton = CreateUiButton(checkpointFrame.transform, "Zero", new Vector2(190, -175), new Vector2(170, 55), new Color(.5f, .5f, .5f), Color.white);
        checkpointContinueButton = CreateUiButton(checkpointFrame.transform, "Next", new Vector2(0, -175), new Vector2(200, 55), new Color(.1f, .6f, .25f), Color.white);
        checkpointContinueButton.gameObject.SetActive(false);
        checkpointPanel.SetActive(false);

        // ---- Final celebration screen, shown once instead of the intro popup ----
        congratsPanel = CreateUiPanel(go.transform, "CongratsPanel", new Color(.08f, .5f, .2f, .96f), Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(600, 320));
        congratsText = BoardText(congratsPanel.transform, "Congratulations!\nYou completed all the levels!", 38, Color.white, Vector2.zero, new Vector2(560, 260));
        congratsPanel.SetActive(false);

        confetti = BuildConfetti(xrCamera != null ? xrCamera : root);
    }

    static Button CreateUiButton(Transform parent, string text, Vector2 position, Vector2 size, Color background, Color foreground)
    {
        var go = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.one * .5f;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = background;

        var label = CreateUiText(go.transform, text, 30, foreground, FontStyle.Bold);
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
        main.playOnAwake = false; // only fire when NumberLineTracker.FinishLevel() calls Play()
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

    static Transform BuildRing(Transform parent, string name, Vector3 position, float radius, Material material)
    {
        var ring = new GameObject(name).transform;
        ring.SetParent(parent, false);
        ring.localPosition = position;
        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2 / 48;
            var part = CreatePrimitive(PrimitiveType.Cube, "RingSegment", new Vector3(Mathf.Cos(angle)*radius, 0, Mathf.Sin(angle)*radius), new Vector3(.045f, .025f, radius*.15f), material, ring);
            part.transform.localRotation = Quaternion.Euler(0, -angle*Mathf.Rad2Deg, 0);
        }
        return ring;
    }

    static void BuildArrow(Transform root, string name, Vector3 p, int direction, Material material, bool ground)
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
                if (Mathf.Abs(p.x) < 7.5f && p.z > -8 && p.z < 4) renderer.gameObject.SetActive(false);
            }
        }
        var sand = GetOrCreateMat("NLP_Sand", new Color(.63f, .43f, .23f));
        var ground = CreatePrimitive(PrimitiveType.Cube, "ActivityGround", new Vector3(0, -.13f, -1), new Vector3(16, .25f, 16), sand, root);
        ground.AddComponent<BoxCollider>();
        // Reuse the project's low-poly forest assets around the clear walking area.
        string fir = FindAssetPath("Fir_Tree_1");
        string rock = FindAssetPath("Rock_1");
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 6; i++)
            {
                if (fir != null) PlaceScenery(fir, root, new Vector3(side * (8.5f + i % 2 * 2), 0, 1 + i * 3.5f), 4.5f + i % 3);
                if (rock != null && i < 3) PlaceScenery(rock, root, new Vector3(side * (7.5f + i), 0, 4 + i * 4), 1.8f + i * .3f);
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
