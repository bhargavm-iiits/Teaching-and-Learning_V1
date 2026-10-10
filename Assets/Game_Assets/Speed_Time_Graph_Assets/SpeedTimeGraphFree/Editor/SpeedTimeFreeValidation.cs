using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpeedTimeGraphFree
{
    /// <summary>Writes Logs/SpeedTimeGraphFree/result.txt (PASS/FAIL per check) and the screenshots.</summary>
    public static class SpeedTimeFreeValidation
    {
        const string Log = "Logs/SpeedTimeGraphFree/";
        const string Baseline = "Work/SpeedTimeGraphFree/script-hashes-before.json";
        [Serializable] class HashItem { public string Path, Hash; }
        [Serializable] class HashList { public string created; public int count; public HashItem[] items; }

        static string Sha(string path) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""); }
        static IEnumerable<string> ScriptsOutsideNewFolder() => Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).Where(p => !p.StartsWith("Assets/SpeedTimeGraphFree/"));

        /// <summary>True when running in the isolated batch copy (Work/SpeedTimeGraph/BuildProject); the full hash check then runs again at install.</summary>
        public static bool IsolatedCopy => Application.dataPath.Replace(System.IO.Path.DirectorySeparatorChar, (char)47).Contains("/Work/SpeedTimeGraph/BuildProject/");
        public static void RecordBaselineIfMissing()
        {
            if (File.Exists(Baseline)) return;
            Directory.CreateDirectory("Work/SpeedTimeGraphFree");
            var items = ScriptsOutsideNewFolder().OrderBy(p => p).Select(p => new HashItem { Path = p, Hash = Sha(p) }).ToArray();
            File.WriteAllText(Baseline, JsonUtility.ToJson(new HashList { created = DateTime.Now.ToString("s"), count = items.Length, items = items }, true));
        }
        /// <summary>Re-hash every script recorded in the baseline (and report new ones). Empty list = unchanged.</summary>
        public static List<string> ChangedScripts()
        {
            var changed = new List<string>();
            if (!File.Exists(Baseline)) { changed.Add("baseline missing: " + Baseline); return changed; }
            var list = JsonUtility.FromJson<HashList>(File.ReadAllText(Baseline).TrimStart('\uFEFF'));
            bool isolated = IsolatedCopy;
            foreach (var item in list.items)
                if (!File.Exists(item.Path)) { if (!isolated) changed.Add("missing " + item.Path); }
                else if (!string.Equals(Sha(item.Path), item.Hash, StringComparison.OrdinalIgnoreCase)) changed.Add("changed " + item.Path);
            var known = new HashSet<string>(list.items.Select(i => i.Path));
            if (!isolated) foreach (var p in ScriptsOutsideNewFolder()) if (!known.Contains(p)) changed.Add("new script outside the new folder " + p);
            return changed;
        }

        public static void Validate(GameFlowManager game, Camera eye, SpeedTimeFreeBuilder.VehicleSet vehicles)
        {
            Directory.CreateDirectory(Log);
            var lines = new List<string>(); bool failed = false;
            void Check(bool ok, string text) { lines.Add((ok ? "PASS: " : "FAIL: ") + text); if (!ok) failed = true; }
            var scene = game.gameObject.scene; var roots = scene.GetRootGameObjects();
            var all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var course = game.catalog.course; var arena = game.arena;

            Check(Object.FindObjectsByType<TripleVehicleArena>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1, "exactly one TripleVehicleArena in the scene");
            Check(PrefabUtility.IsPartOfPrefabInstance(arena.gameObject) && PrefabUtility.GetCorrespondingObjectFromSource(arena.gameObject) != null, "TripleVehicleArena is an instance of Prefabs/TripleVehicleArena.prefab");
            var oldTypes = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Where(m => m && m.GetType().Namespace != null && (m.GetType().Namespace.StartsWith("SpeedTimeGraphLesson") || m.GetType().Namespace.StartsWith("DistanceTimeGraph"))).ToArray();
            Check(oldTypes.Length == 0, $"0 components from the old namespaces (found {oldTypes.Length})");

            // Vehicles persist through every part (same instance IDs, active).
            var ids = arena.vehicles.Select(v => v.GetEntityId()).ToArray();
            bool persist = true; var seen = new List<string>();
            foreach (var part in new[] { Part.Demo, Part.Q1, Part.Q2, Part.Q3, Part.Demo })
            {
                game.StartPart(part); game.popup.Hide();
                var now = arena.vehicles.Select(v => v ? v.GetEntityId() : default).ToArray();
                bool ok = now.SequenceEqual(ids) && arena.vehicles.All(v => v && v.gameObject.activeInHierarchy && v.GetComponentsInChildren<Renderer>().Any(r => r.enabled));
                persist &= ok; seen.Add(part + (ok ? "" : "!"));
            }
            Check(persist, "Car, Train and Motorcycle present and active in " + string.Join(" → ", seen) + " with the same instance IDs");
            Check(arena.vehicles[0].name.Contains("Car") && arena.vehicles[1].name.Contains("Train") && arena.vehicles[2].name.Contains("Motorcycle"), "vehicles built from Assets/Speed_Time_Graph_Assets GLBs (Car, Train, Motorcycle prefabs)");

            // Same k, START at x = 0, boards aligned.
            Check(arena.vehicles.All(v => Mathf.Abs(v.k - (float)course.k) < 1e-6f) && Mathf.Abs(arena.k - (float)course.k) < 1e-6f, $"same k = {course.k} on every lane");
            game.StartPart(Part.Demo); game.popup.Hide();
            Check(arena.vehicles.All(v => Mathf.Abs(v.transform.position.x) < 1e-4f), "all three noses at START x = 0");
            var starts = all.Where(t => t.name == "START checker").ToArray();
            Check(starts.Length > 0 && starts.All(t => t.position.x >= -.001f && t.position.x <= .401f), "START strips at x = 0–0.4 on every lane");
            var boards = all.Where(t => t.name.StartsWith("Distance board ")).ToArray();
            bool aligned = boards.Length == 39 && boards.GroupBy(t => t.name).All(g => g.Count() == 3 && g.All(t => Mathf.Abs(t.position.x - int.Parse(g.Key.Split(' ')[2]) * (float)course.k) < 1e-3f));
            Check(aligned, $"distance boards 0–120 m every 10 m at the same x on all lanes ({boards.Length} boards)");

            // Scripted RUN of every answer: each vehicle stops inside its STOP box (x = k * area).
            int runs = 0; var runFails = new List<string>();
            foreach (var v in new[] { game.catalog.demo }.Concat(game.catalog.variants))
            {
                game.part = v.question == 0 ? Part.Demo : v.question == 1 ? Part.Q1 : v.question == 2 ? Part.Q2 : Part.Q3;
                game.variant = v; game.graph.LoadVariant(v, course); arena.ApplyVariant(v, course);
                for (int i = 0; i < 3; i++) { var g = v.question == 0 ? game.graph.shared : game.graph.lanes[i]; g.Set(v.lanes[i].solution); }
                float T = (float)Enumerable.Range(0, 3).Max(i => MotionMath.Duration(game.graph.Graph(i).Points));
                arena.SetTime(game, T, false);
                for (int i = 0; i < 3; i++)
                {
                    float x = arena.vehicles[i].transform.localPosition.x, stopX = arena.stopGantries[i].localPosition.x;
                    if (Mathf.Abs(x - stopX) > (float)(course.k * course.stopTolerance) + 1e-3f || Mathf.Abs(x - (float)(course.k * v.lanes[i].stop)) > 1e-3f)
                        runFails.Add($"{v.id} {v.lanes[i].vehicle} x={x:0.###} stop={stopX:0.###}");
                }
                runs++;
            }
            Check(runFails.Count == 0, $"after a scripted RUN of each answer ({runs} parts) all three vehicles stop inside their STOP boxes" + (runFails.Count > 0 ? ": " + string.Join("; ", runFails) : ""));
            game.StartPart(Part.Demo); game.popup.Hide();

            // Sight lines and panel overlap in the 16:9 view.
            var eyePos = SpeedTimeFreeBuilder.Eye; var cam = eye;
            var savedRot = cam.transform.rotation; var savedPos = cam.transform.position; cam.transform.SetPositionAndRotation(eyePos, Quaternion.Euler(SpeedTimeFreeBuilder.EyePitch, SpeedTimeFreeBuilder.EyeYaw, 0)); cam.aspect = 16f / 9f; cam.fieldOfView = SpeedTimeFreeBuilder.Fov;
            var blockers = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>()).Where(r => r.enabled && r.gameObject.activeInHierarchy &&
                (r.transform.IsChildOf(arena.transform.Find("Lanes (static)")) == false || r.name.StartsWith("Rock")) && !r.name.StartsWith("Low-poly land") && r.name != "Sea" && !r.name.Contains("glow") && !r.name.StartsWith("Mountain") && !r.name.StartsWith("Snow") && !r.name.Contains("Glass") && !r.name.Contains("ghost") && !(r is LineRenderer)).ToArray();
            var targets = new List<(string name, Vector3 at, Transform own)>();
            foreach (var s in all.Where(t => t.name == "START sign")) targets.Add(("START " + s.parent.name, s.position + Vector3.up * 3.1f, s));
            for (int i = 0; i < 3; i++) targets.Add(("STOP " + i, arena.stopGantries[i].position + Vector3.up * 4.3f, arena.stopGantries[i]));
            var blocked = new List<string>();
            foreach (var (name, at, own) in targets)
            {
                var dir = at - eyePos; var ray = new Ray(eyePos, dir.normalized); float len = dir.magnitude - .6f;
                foreach (var r in blockers)
                {
                    if (r.transform.IsChildOf(own) || r.GetComponentInParent<VehicleMover>() || r is CanvasRenderer) continue;
                    if (r.bounds.IntersectRay(ray, out float d) && d < len && d > .2f) { blocked.Add($"{name} by {r.name}"); break; }
                }
                var vp = cam.WorldToViewportPoint(at);
                if (vp.z <= 0 || vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1) blocked.Add(name + " outside the 16:9 view");
            }
            Check(blocked.Count == 0, "eye-to-START and eye-to-STOP lines unobstructed and inside the 16:9 view" + (blocked.Count > 0 ? ": " + string.Join(", ", blocked.Take(6)) : ""));
            var covered = new List<string>();
            var lanePts = targets.Select(q => (q.name, (Vector2)cam.WorldToViewportPoint(q.at))).Concat(starts.Select(q => ("START strip", (Vector2)cam.WorldToViewportPoint(q.position)))).ToList();
            foreach (var canvas in roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Where(c => c.name.StartsWith("Velocity Time Graph")))
            {
                var corners = new Vector3[4]; ((RectTransform)canvas.transform).GetWorldCorners(corners);
                var screenVp = corners.Select(c => cam.WorldToViewportPoint(c)).ToArray();
                if (screenVp.Any(q => q.z <= 0)) continue;
                var rect = Rect.MinMaxRect(screenVp.Min(q => q.x), screenVp.Min(q => q.y), screenVp.Max(q => q.x), screenVp.Max(q => q.y));
                foreach (var (n, q) in lanePts) if (rect.Contains(q)) covered.Add(n + " under " + canvas.name);
            }
            Check(covered.Count == 0, "floating Velocity Time Graph screen covers no START or STOP marker" + (covered.Count > 0 ? ": " + string.Join(", ", covered.Take(5)) : ""));
            cam.transform.SetPositionAndRotation(savedPos, savedRot);

            // Materials, triangles, ghosts, fonts.
            var renderers = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToArray();
            var badMats = renderers.SelectMany(r => r.sharedMaterials).Where(m => !m || m.shader == null || m.shader.name == "Standard" || m.shader.name.Contains("InternalErrorShader") || !m.shader.isSupported).Select(m => m ? m.name + " (" + m.shader?.name + ")" : "null").Distinct().ToArray();
            Check(badMats.Length == 0, "no pink, missing or Built-in Standard materials" + (badMats.Length > 0 ? ": " + string.Join(", ", badMats.Take(8)) : ""));
            long tris = 0, vehicleTris = 0, trackTris = 0;
            foreach (var f in roots.SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true)))
            {
                if (!f.sharedMesh) continue; long n = 0; for (int s = 0; s < f.sharedMesh.subMeshCount; s++) n += f.sharedMesh.GetIndexCount(s) / 3;
                tris += n; if (f.GetComponentInParent<VehicleMover>()) vehicleTris += n; if (f.name.Contains("Track") || f.transform.parent && f.transform.parent.name.StartsWith("Track tile")) trackTris += n;
            }
            lines.Add($"INFO: triangles in scene (LOD0, incl. inactive) {tris:N0}: vehicles {vehicleTris:N0}, track tiles {trackTris:N0}. Mesh LODs generated: {MeshLodInfo(vehicles)}");
            var ghostMeshes = new[] { vehicles.car.ghostMesh, vehicles.train.ghostMesh, vehicles.bike.ghostMesh };
            Check(ghostMeshes.All(m => m && m.triangles.Length / 3 <= 2000), "ghost proxies ≤ 2k triangles (" + string.Join(", ", ghostMeshes.Select(m => m ? (m.triangles.Length / 3).ToString() : "missing")) + ")");
            var texts = roots.SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)).ToArray();
            float minFont = texts.Length == 0 ? 0 : texts.Min(t => t.fontSize);
            Check(minFont >= 30, $"minimum TMP font size ≥ 30 (min {minFont:0}, {texts.Length} texts)");
            var wanted = new[] { "RUN ▶ button", "PAUSE ❚❚ button", "RESET ⟲ button", "1× button", "UNDO button", "DELETE button", "CLEAR button", "HINT ? button" };
            var buttons = roots.SelectMany(r => r.GetComponentsInChildren<Button>(true)).Where(b => wanted.Contains(b.name)).ToArray();
            Check(buttons.Length == 8 && buttons.All(b => b.onClick.GetPersistentEventCount() > 0), $"Velocity Time Graph buttons RUN/PAUSE/RESET/1×/UNDO/DELETE/CLEAR/HINT wired ({buttons.Length})");
            var mm = buttons.Select(b => ((RectTransform)b.transform).rect.size * b.transform.lossyScale.x * 1000).ToArray();
            Check(mm.All(s => s.x >= 100 && s.y >= 60), $"bottom button bar, each button at least 100 × 60 mm (smallest {mm.Min(s => s.x):0} × {mm.Min(s => s.y):0} mm)");
            string occPath = SpeedTimeFreeBuilder.ScenePath.Replace(".unity", "") + "/OcclusionCullingData.asset";
            long occBytes = File.Exists(occPath) ? new FileInfo(occPath).Length : 0;
            Check(occBytes > 0 && eye.useOcclusionCulling, $"occlusion culling baked for the scene ({occPath}, {occBytes / 1024:N0} KB) and enabled on the camera");
            var changed = ChangedScripts();
            Check(changed.Count == 0, "script hashes unchanged outside Assets/SpeedTimeGraphFree" + (IsolatedCopy ? " (isolated copy: present files; full check repeated at install)" : "") + (changed.Count > 0 ? ": " + string.Join(", ", changed.Take(5)) : ""));
            try { lines.Add(LogicTestSuite.Run(game.catalog)); } catch (Exception e) { Check(false, e.Message); }
            lines.Add("NOT MEASURED HERE: 72+ fps must be checked in Play mode with the XR Device Simulator or a headset (batch mode has no real frame pacing).");
            File.WriteAllLines(Log + "result.txt", new[] { "Speed–Time Graph Free · validation " + DateTime.Now.ToString("s"), failed ? "OVERALL: FAIL" : "OVERALL: PASS" }.Concat(lines));
            Debug.Log("[STGF] validation " + (failed ? "FAIL" : "PASS") + "\n" + string.Join("\n", lines));
        }

        static string MeshLodInfo(SpeedTimeFreeBuilder.VehicleSet v)
        {
            try
            {
                var parts = new List<string>();
                foreach (var b in new[] { v.car, v.train, v.bike })
                    foreach (var f in b.prefab.GetComponentsInChildren<MeshFilter>())
                        if (f.name == "Body") { var p = typeof(Mesh).GetProperty("lodCount"); parts.Add(b.prefab.name + " " + (p != null ? p.GetValue(f.sharedMesh) : "?")); }
                return string.Join(", ", parts);
            }
            catch (Exception e) { return e.Message; }
        }

        // ------------------------------------------------------------------ screenshots
        public static void CaptureAll(GameFlowManager game, Camera eye)
        {
            Directory.CreateDirectory(Log);
            var pos = SpeedTimeFreeBuilder.Eye; var rot = Quaternion.Euler(SpeedTimeFreeBuilder.EyePitch, SpeedTimeFreeBuilder.EyeYaw, 0);
            void Pose(Vector3 p, Quaternion r, float fov) { eye.transform.SetPositionAndRotation(p, r); eye.fieldOfView = fov; }
            void Solutions(int t) { for (int i = 0; i < 3; i++) { var g = game.variant.question == 0 ? game.graph.shared : game.graph.lanes[i]; g.Set(game.variant.lanes[i].solution); } }

            game.StartPart(Part.Demo); game.popup.Hide();
            Pose(pos, rot, SpeedTimeFreeBuilder.Fov); Shot(game, eye, "game_view_16x9.png", 1920, 1080);
            Pose(pos, rot, 92); Shot(game, eye, "first_person.png", 1920, 1080);
            Solutions(0); game.mode = Mode.Run; game.playback.time = 5.5f; game.arena.SetTime(game, 5.5f, true); game.Refresh();
            Pose(pos, rot, SpeedTimeFreeBuilder.Fov); Shot(game, eye, "demo.png", 1920, 1080);
            Pose(SpeedTimeFreeBuilder.Eye, Quaternion.Euler(-9, SpeedTimeFreeBuilder.EyeYaw, 0), 62); Shot(game, eye, "graph_controller.png", 2400, 800); Pose(SpeedTimeFreeBuilder.Eye, Quaternion.Euler(-4, SpeedTimeFreeBuilder.EyeYaw, 0), 90); Shot(game, eye, "vr_windows_wide.png", 2400, 1000);
            int q = 1;
            foreach (var part in new[] { Part.Q1, Part.Q2, Part.Q3 })
            {
                game.StartPart(part); game.popup.Hide(); Solutions(0);
                if (part == Part.Q3) game.hintLevel = 1;
                game.lastRun = QuestionRules.EvaluateAll(game.variant, game.graph.AllGraphs, game.Course);
                float T = game.playback.Duration; game.mode = Mode.Review; game.playback.time = T; game.arena.SetTime(game, T, false);
                game.message = game.lastRun.Message; game.Refresh();
                Pose(pos, rot, SpeedTimeFreeBuilder.Fov); Shot(game, eye, $"q{q++}.png", 1920, 1080);
            }
            game.StartFinalCheck(false, new[] { 0, 1, 2 });
            Pose(pos, rot, SpeedTimeFreeBuilder.Fov); Shot(game, eye, "final_check.png", 1920, 1080);
            game.finalCheck.Hide(); game.ShowIntro(); Pose(pos, rot, SpeedTimeFreeBuilder.Fov); Shot(game, eye, "popup.png", 1920, 1080);
            game.finalCheck.Hide(); game.StartPart(Part.Demo); game.popup.Hide();
            Pose(pos, rot, SpeedTimeFreeBuilder.Fov);
        }

        static void Shot(GameFlowManager game, Camera eye, string file, int w, int h)
        {
            game.Refresh();
            if (game.links) game.links.UpdateBeams();
            foreach (var bb in Object.FindObjectsByType<BillboardLabel>(FindObjectsSortMode.None)) bb.SendMessage("LateUpdate");
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) t.ForceMeshUpdate(true, true);
            foreach (var g in Object.FindObjectsByType<GraphPanel>(FindObjectsSortMode.None)) g.ForceRebuild();
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            float aspect = eye.aspect; eye.aspect = (float)w / h; eye.targetTexture = rt; eye.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var img = new Texture2D(w, h, TextureFormat.RGB24, false); img.ReadPixels(new Rect(0, 0, w, h), 0, 0); img.Apply();
            File.WriteAllBytes(Log + file, img.EncodeToPNG());
            eye.targetTexture = null; eye.aspect = aspect; RenderTexture.active = prev; Object.DestroyImmediate(img); rt.Release(); Object.DestroyImmediate(rt);
            Debug.Log("[STGF] screenshot " + file);
        }
    }
}