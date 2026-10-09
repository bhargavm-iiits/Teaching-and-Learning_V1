using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DistanceTimeGraph.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DistanceTimeGraph.EditorTools
{
    /// Checks the saved scene and writes Logs/DistanceTimeGraph/result.txt plus pictures of the key moments
    /// (first-person view of each level, the behind-car monitor, the popups, the final check, an overview).
    /// The pictures are staged in memory; the scene is reloaded from disk afterwards, so nothing is saved.
    public static class DtgValidation
    {
        const string OutDir = "Logs/DistanceTimeGraph";
        static readonly List<string> lines = new List<string>();
        static int failures;

        [MenuItem("Tools/Distance Time Graph/Build and Validate %#&g")]
        public static void BuildAndValidate()
        {
            if (DtgBuilder.Build()) Validate();
        }

        [MenuItem("Tools/Distance Time Graph/Validate")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory(OutDir);
            lines.Clear();
            failures = 0;
            var scene = EditorSceneManager.OpenScene(DtgBuilder.ScenePath, OpenSceneMode.Single);
            try
            {
                Check();
                Capture();
            }
            catch (Exception e)
            {
                Fail("validation threw: " + e);
            }
            string verdict = failures == 0 ? "PASS" : $"FAIL ({failures})";
            lines.Insert(0, $"{verdict}  Distance Time Graph validation {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            File.WriteAllText(Path.Combine(OutDir, "result.txt"), string.Join("\n", lines), Encoding.UTF8);
            EditorSceneManager.OpenScene(DtgBuilder.ScenePath, OpenSceneMode.Single);   // drop the staged picture states
            Debug.Log($"[DTG] validation {verdict}: see {OutDir}/result.txt");
            EditorUtility.DisplayDialog("Distance Time Graph", $"Validation {verdict}.\nDetails and pictures: {OutDir}", "OK");
        }

        static void Need(bool ok, string what) { if (ok) lines.Add("ok    " + what); else Fail(what); }
        static void Fail(string what) { failures++; lines.Add("FAIL  " + what); }

        static T One<T>() where T : UnityEngine.Object => UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

        static void Check()
        {
            var flow = One<GameFlowManager>();
            Need(flow != null, "GameFlowManager exists");
            if (flow == null) return;
            var so = new SerializedObject(flow);
            var it = so.GetIterator();
            for (bool enter = true; it.NextVisible(enter); enter = false)
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.name != "m_Script")
                    Need(it.objectReferenceValue != null, $"GameFlowManager.{it.name} is wired");

            var level = flow.level;
            Need(level.roadModules.Length == 16 && level.roadModules.All(m => m != null), "16 modular road pieces (0-80 m)");
            Need(level.landmarks.Length == Enum.GetValues(typeof(Landmark)).Length && level.landmarks.All(m => m != null), "every destination landmark exists");
            Need(level.islandLayouts.Length == AdaptiveSelection.IslandLayouts, "three island arrangements");
            Need(flow.bridge.tiles.Length == 10 && flow.bridge.tiles.All(t => t != null), "timed bridge has 10 tiles");
            Need(flow.catalog.variants.Count(v => v.kind == LevelKind.Rising) >= 4 && flow.catalog.variants.Count(v => v.kind == LevelKind.Waiting) >= 4
                 && flow.catalog.variants.Count(v => v.kind == LevelKind.Returning) >= 4, "catalog has 4 variants per level");

            // XR and input
            Need(One<Unity.XR.CoreUtils.XROrigin>() != null, "XR Origin in the scene");
            Need(One<XRUIInputModule>() != null, "EventSystem uses XRUIInputModule (rays + mouse)");
            foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                // Only this scene's own panels (the simulator's hidden overlay is not part of the game).
                if (c.GetComponentInParent<ControlCluster>(true) == null) continue;
                if (c.transform.parent != null && c.transform.parent.GetComponentInParent<Canvas>(true) != null) continue;
                Need(c.renderMode == RenderMode.WorldSpace && c.GetComponent<TrackedDeviceGraphicRaycaster>() != null && c.gameObject.layer == DtgBuilder.UiLayer,
                    $"canvas {c.name} is world-space, ray-pressable, on the UI layer");
            }

            // Rear camera
            var rear = flow.rearCamera;
            Need(rear.cam != null && rear.cam.targetTexture != null, "behind-car camera renders into a Render Texture");
            Need(rear.cam != null && (rear.cam.cullingMask & (1 << DtgBuilder.UiLayer)) == 0, "behind-car camera never shows the interface");
            var monitor = UnityEngine.Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include).FirstOrDefault(r => r.texture == rear.cam.targetTexture);
            Need(monitor != null, "monitor shows the behind-car texture");
            var texts = UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include).Select(t => t.text).ToList();
            Need(texts.Contains("BEHIND-CAR CAMERA") && texts.Contains("LIVE"), "monitor titled BEHIND-CAR CAMERA with a LIVE indicator");
            Need(texts.Contains("TIME (s)") && texts.Contains("DISTANCE FROM START (m)"), "graph axes: TIME (s) / DISTANCE FROM START (m)");
            Need(!texts.Any(t => t.IndexOf("total distance", StringComparison.OrdinalIgnoreCase) >= 0), "no 'Total Distance Travelled' label anywhere");

            // No steering, pedals or speed controls: the console only has graph buttons.
            var labels = flow.console.GetComponentsInChildren<Button>(true).Select(b => b.GetComponentInChildren<TMPro.TMP_Text>(true).text).ToArray();
            Need(labels.SequenceEqual(new[] { "RUN GRAPH", "PAUSE", "EDIT", "REPLAY", "RESET", "DELETE" }), "console = RUN GRAPH, PAUSE, EDIT, REPLAY, RESET, DELETE (" + string.Join(", ", labels) + ")");
            var forbidden = new[] { "steer", "accelerat", "brake", "speed", "joystick", "throttle" };
            Need(!texts.Any(t => forbidden.Any(f => t.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)), "no steering / accelerator / brake / speed control text");

            // Materials: nothing pink.
            var bad = new HashSet<string>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                foreach (var m in r.sharedMaterials)
                {
                    if (r is ParticleSystemRenderer && m == null) continue;
                    if (m == null) { bad.Add(r.name + ": missing material"); continue; }
                    var s = m.shader;
                    if (s == null || !s.isSupported || s.name == "Standard" || s.name.StartsWith("Legacy Shaders") || s.name.Contains("InternalError"))
                        bad.Add($"{r.name}: {m.name} ({(s != null ? s.name : "no shader")})");
                }
            Need(bad.Count == 0, "every material renders in URP" + (bad.Count > 0 ? ": " + string.Join("; ", bad.Take(12)) : ""));

            // Isolation from the other games in this project.
            var foreign = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                .Where(m => m != null && m.GetType().Namespace != null && (m.GetType().Namespace.StartsWith("NumberLinePlayground") || m.GetType().Namespace.StartsWith("ZeroReturn")))
                .Select(m => m.GetType().FullName).Distinct().ToList();
            Need(foreign.Count == 0, "uses none of the other games' scripts" + (foreign.Count > 0 ? ": " + string.Join(", ", foreign) : ""));

            // The rules, on the scene's own catalog.
            var cat = flow.catalog.variants;
            VariantSpec V(string id) => cat.First(v => v.id == id);
            GraphModel G(params (int t, int d)[] pts) { var g = new GraphModel(20, 80, 0); foreach (var p in pts) g.Add(p.t, p.d); return g; }
            Need(LevelRules.Run(G((4, 40)), V("1A")).success && !LevelRules.Run(G((4, 30)), V("1A")).success, "Level 1 rule (rising to the goal)");
            Need(LevelRules.Run(G((4, 40), (8, 40), (11, 60)), V("2A")).success && LevelRules.Run(G((4, 40), (6, 40), (9, 60)), V("2A")).safetyStop, "Level 2 rule (wait, cross in the window, safe stop)");
            Need(LevelRules.Run(G((6, 60), (10, 20)), V("3A")).success && !LevelRules.Run(G((6, 50), (10, 20)), V("3A")).success, "Level 3 rule (out to the beacon, back to the pad)");
            var qs = Assessment.Questions();
            Need(Assessment.Route(Assessment.Score(qs, new[] { 0, 1, 0 })) == AssessmentRoute.NextTopic && Assessment.Route(Assessment.Score(qs, new[] { 0, 1, 1 })) == AssessmentRoute.FromLevel2
                 && Assessment.Route(Assessment.Score(qs, new[] { 0, 0, 1 })) == AssessmentRoute.FromLevel1 && Assessment.Route(Assessment.Score(qs, new[] { 1, 0, 1 })) == AssessmentRoute.FromLevel1,
                "assessment routing for scores 3 / 2 / 1 / 0");
        }

        // ---------------- pictures ----------------

        static void Capture()
        {
            var flow = One<GameFlowManager>();
            var eye = flow.graph.GetComponentInParent<Canvas>().worldCamera;
            if (eye == null) { Fail("no eye camera for pictures"); return; }
            var cat = flow.catalog.variants;
            VariantSpec V(string id) => cat.First(v => v.id == id);
            var graph = flow.graph;

            // Level 1: a rising point being placed; the preview car waits at 40 m linked to it.
            Stage(flow, V("1A"), new[] { (4, 40) }, 1, carAt: 0f, carV: 0f);
            ShowPreview(flow, 1);
            Shot(flow, eye, "level1_edit.png");
            SaveMonitor(flow, "monitor_level1_preview.png");

            // Level 2: flat segment at the sync pad, bridge preview safe.
            Stage(flow, V("2A"), new[] { (4, 40), (8, 40), (11, 60) }, 2, carAt: 40f, carV: 0f);
            flow.bridge.Preview(true);
            flow.activity.SetPrediction("Predicted arrival at 60 m: 11 s\nBridge preview: SAFE", Palette.Green);
            ShowPreview(flow, 2);
            Shot(flow, eye, "level2_edit.png");
            // ... and the bridge open during the run.
            flow.preview.previewCar.gameObject.SetActive(false);
            flow.preview.link.enabled = false;
            flow.bridge.Live(9.5f, 45f);
            flow.car.Place(45f, 6.7f);
            graph.SetCursor(9.5f, 46.7f);
            Shot(flow, eye, "level2_run.png");

            // Level 3: the final point dragged lower; the car reverses toward the pad.
            Stage(flow, V("3A"), new[] { (6, 60), (10, 20) }, 2, carAt: 30f, carV: -10f);
            graph.ReturnHint = true;
            graph.SimulateDrag(2, new GraphPoint(10, 60));
            ShowPreview(flow, 2);
            flow.preview.previewCar.Place(20f, -10f);
            flow.level.PlaceRoadReturn(60f, 20f);
            flow.rearCamera.target = flow.car.transform;
            Shot(flow, eye, "level3_edit.png");
            SaveMonitor(flow, "monitor_level3_reverse.png");

            // Popups.
            flow.veil.veil.enabled = true;
            flow.popup.Show("LEVEL 1 COMPLETE", "An upward graph segment moved the car away from the start.", Palette.Green, ("NEXT LEVEL", null), ("REPLAY SUCCESS", null));
            Stage(flow, V("1A"), new[] { (4, 40) }, -1, carAt: 40f, carV: 0f);
            Shot(flow, eye, "popup_complete.png");
            Stage(flow, V("1A"), new[] { (4, 30) }, -1, carAt: 30f, carV: 0f);
            graph.SetHighlight(0, "The car stopped at 30 m.");
            flow.popup.Show("ATTEMPT RECORDED", "Observe what happened. Your next challenge will have a different route.\n\n<size=80%><color=#FFB060>The car stopped at 30 m. The blue island gate is at 40 m.</color></size>",
                Palette.Orange, ("WATCH RESULT", null), ("CONTINUE TO NEW CHALLENGE", null));
            Shot(flow, eye, "popup_attempt_recorded.png");
            flow.popup.Hide();

            // Final check, answered but not submitted, then submitted with two right.
            var answers = new[] { -1, -1, -1 };
            flow.assessment.Open(answers, null, null);
            flow.assessment.Choose(0, 0);
            flow.assessment.Choose(1, 1);
            Shot(flow, eye, "assessment_unanswered.png");
            flow.assessment.Choose(2, 1);
            flow.assessment.Submit();
            Shot(flow, eye, "assessment_result_2of3.png");
            flow.assessment.Close();
            flow.veil.veil.enabled = false;

            // Overview from above the track.
            var go = new GameObject("__DtgOverview", typeof(Camera));
            try
            {
                var cam = go.GetComponent<Camera>();
                cam.fieldOfView = 55f;
                cam.farClipPlane = 800f;
                go.transform.position = new Vector3(-38f, 34f, -30f);
                go.transform.LookAt(new Vector3(0f, 0f, 40f));
                Stage(flow, V("2A"), new[] { (4, 40), (8, 40), (11, 60) }, -1, carAt: 40f, carV: 0f);
                Render(cam, Path.Combine(OutDir, "overview.png"));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            lines.Add("pictures: " + string.Join(", ", Directory.GetFiles(OutDir, "*.png").Select(Path.GetFileName)));
        }

        static void Stage(GameFlowManager flow, VariantSpec v, (int t, int d)[] points, int select, float carAt, float carV)
        {
            flow.level.Apply(v);
            var g = new GraphModel(flow.catalog.maxTime, flow.catalog.maxDistance, v.start);
            foreach (var p in points) g.Add(p.t, p.d);
            flow.graph.SetModel(g);
            flow.graph.ReturnHint = false;
            flow.graph.SetBridgeBand(v.kind == LevelKind.Waiting, v.bridgeOpen, v.bridgeClose);
            flow.graph.SetHighlight(-1, null);
            flow.graph.SetCursor(-1f, 0f);
            if (select >= 0) flow.graph.Select(select);
            flow.car.Teleport(carAt);
            flow.car.Place(carAt, carV);
            flow.activity.SetActivity($"LEVEL {(int)v.kind + 1} · {v.title}", v.Instruction + (v.kind == LevelKind.Waiting ? $"\n<b>BRIDGE OPEN {v.bridgeOpen:0}–{v.bridgeClose:0} s</b>" : ""));
            var last = g[g.Count - 1];
            flow.activity.SetPrediction($"The graph ends at {last.d} m after {last.t} s.", Palette.SoftInk);
            flow.activity.SetStatus($"t = 0.0 s   ·   {carAt:0.0} m from start   ·   {(carV < 0 ? "moving back" : carV > 0 ? "moving away" : "stopped")}");
            flow.preview.previewCar.gameObject.SetActive(false);
            flow.preview.link.enabled = false;
            flow.level.ShowRoadReturn(false);
            flow.rearCamera.target = flow.car.transform;
        }

        static void ShowPreview(GameFlowManager flow, int index)
        {
            var p = flow.graph.Model[index];
            var car = flow.preview.previewCar;
            car.gameObject.SetActive(true);
            car.Teleport(p.d);
            car.Place(p.d, flow.graph.Model.Slope(index - 1));
            flow.preview.link.enabled = true;
            flow.preview.link.SetPosition(0, flow.graph.PointWorld(index));
            flow.preview.link.SetPosition(1, car.Center + Vector3.up * 1.4f);
            flow.rearCamera.target = car.transform;
        }

        static void Shot(GameFlowManager flow, Camera eye, string file)
        {
            Canvas.ForceUpdateCanvases();
            flow.rearCamera.SnapNow();
            flow.rearCamera.cam.Render();   // refresh the monitor's texture first
            Render(eye, Path.Combine(OutDir, file));
        }

        static void SaveMonitor(GameFlowManager flow, string file)
        {
            flow.rearCamera.SnapNow();
            var cam = flow.rearCamera.cam;
            cam.Render();
            var rt = cam.targetTexture;
            var active = RenderTexture.active;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, file), tex.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        static void Render(Camera camera, string path)
        {
            var old = camera.targetTexture;
            var active = RenderTexture.active;
            var rt = new RenderTexture(1600, 1000, 24) { antiAliasing = 4 };
            var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = old;
                RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
