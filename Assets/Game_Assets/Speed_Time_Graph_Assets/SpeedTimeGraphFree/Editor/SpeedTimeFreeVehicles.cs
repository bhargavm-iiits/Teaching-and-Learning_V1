using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Builds Car / Train / Motorcycle wrapper prefabs from the GLBs in Assets/Speed_Time_Graph_Assets,
    /// the train track tile mesh and low-poly ghost proxy hulls. Everything is written into
    /// Assets/SpeedTimeGraphFree; the source GLBs are only read.
    /// </summary>
    public static class SpeedTimeFreeVehicles
    {
        public const string Src = "Assets/Speed_Time_Graph_Assets/";
        public const string Folder = "Assets/SpeedTimeGraphFree";
        public static readonly Color CarBlue = Hex("#2F6BFF"), BikeOrange = Hex("#FF7A12"), TrainRed = Hex("#E0323C");

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        public class Built
        {
            public GameObject prefab; public float length, width, height, wheelRadius; public long triangles;
            public Mesh ghostMesh; public string report;
        }

        static Material fallback;
        static Material Fallback() { if (!fallback) fallback = SpeedTimeFreeMaterials.Lit("Vehicle default grey", new Color(.5f, .5f, .55f)); return fallback; }

        // ------------------------------------------------------------------ helpers for part grouping
        static Bounds WorldBounds(GlbImporter.Primitive p)
        {
            var b = new Bounds(p.world.MultiplyPoint3x4(p.positions[0]), Vector3.zero);
            for (int i = 0; i < p.positions.Length; i += Math.Max(1, p.positions.Length / 400)) b.Encapsulate(p.world.MultiplyPoint3x4(p.positions[i]));
            return b;
        }
        /// <summary>A primitive is part of a wheel if it lies completely inside that wheel box (glTF space).</summary>
        static string WheelPart(GlbImporter.Primitive p, IEnumerable<(string name, Bounds box)> boxes, ISet<string> keepOnBody)
        {
            if (keepOnBody.Contains(p.material)) return "Body";
            var b = WorldBounds(p);
            foreach (var w in boxes) if (w.box.Contains(b.min) && w.box.Contains(b.max)) return w.name;
            return "Body";
        }

        // ------------------------------------------------------------------ vehicles
        public static Built Car()
        {
            var keep = new HashSet<string> { "material_14", "material_19" };   // red brake calipers stay still
            var boxes = new[] { ("Wheel front", MinMax(new Vector3(-5.6f, -.1f, 3.15f), new Vector3(2.3f, 2.75f, 6.1f))),
                                ("Wheel rear", MinMax(new Vector3(-5.6f, -.1f, -6.05f), new Vector3(2.3f, 2.75f, -2.8f))) };
            var spec = new GlbImporter.Spec
            {
                name = "Car", source = Src + "lambo/source/lamborghini_venevo.glb", forwardSign = 1, length = 5.5f,
                part = p => WheelPart(p, boxes, keep),
                recolor = { ["material_20"] = CarBlue, ["material"] = CarBlue },
                dropTexture = { "material" },
            };
            return Wrap(spec, "Car", 1);
        }

        public static Built Train()
        {
            var boxes = new[] { ("Bogie front", MinMax(new Vector3(-1.6f, 0f, 4.1f), new Vector3(1.6f, 1.75f, 9.1f))),
                                ("Bogie rear", MinMax(new Vector3(-1.6f, 0f, -6.6f), new Vector3(1.6f, 1.75f, -1.5f))) };
            var spec = new GlbImporter.Spec
            {
                name = "Train", source = Src + "Train.glb", forwardSign = 1, length = 18.5f,
                part = p => p.material == "Wheels" || p.material == "Black_Plastic" ? WheelPart(p, boxes, new HashSet<string>()) : "Body",
                recolor = { ["Yellow"] = TrainRed, ["Orange"] = TrainRed, ["Body.001"] = TrainRed, ["White"] = new Color(.96f, .96f, .97f),
                            ["Cream"] = new Color(.92f, .92f, .94f), ["Emergency_White"] = Color.white, ["Roof_Black"] = new Color(.16f, .17f, .2f) },
            };
            return Wrap(spec, "Train", 2);
        }

        /// <summary>
        /// Motorcycle from Motor_Cycle/black_panthers.glb. The file holds two cornering bikes with black-panther riders;
        /// we use the second one (materials Material_3_2 bike + Material_2_2 rider), stood upright (it leans 28.7 deg
        /// about its forward axis) and facing +X. It has its own rider, so no procedural rider is added.
        /// </summary>
        public static Built Motorcycle()
        {
            var spec = new GlbImporter.Spec
            {
                name = "Motorcycle", source = Src + "Motor_Cycle/black_panthers.glb", forwardSign = 1, length = 3.0f,
                include = p => p.material == "Material_3_2" || p.material == "Material_2_2",
                pre = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 28.7f)) * Matrix4x4.Rotate(Quaternion.Euler(0, -.67f, 0)),
            };
            return Wrap(spec, "Motorcycle", 3, false);
        }

        /// <summary>
        /// The bike textures are spec/alpha masks (GTA-style "[PRIMARY]" paint slots), so every material gets a
        /// clean flat colour: paint orange like the reference, rubber and plastics black, metal grey, glass smoked.
        /// </summary>
        static GlbImporter.Override BikeMaterial(string n)
        {
            string s = n.ToLowerInvariant();
            GlbImporter.Override O(Color c, float smooth = .35f, float metal = 0, bool transparent = false) => new GlbImporter.Override { color = c, smoothness = smooth, metallic = metal, transparent = transparent };
            if (s.Contains("glass")) return O(new Color(.75f, .85f, .95f, .35f), .9f, 0, true);
            if (s.Contains("rgbab60000ff") || s.Contains("brakelight")) return O(new Color(.85f, .05f, .05f), .6f);
            if (s.Contains("tyres")) return O(new Color(.05f, .05f, .06f), .15f);
            if (s.Contains("[wheel]")) return O(new Color(.12f, .12f, .14f), .5f, .4f);
            if (s.Contains("livery") || s.Contains("[primary]") || s.Contains("vehicle_paint1") && !s.Contains("dashboard") || s.Contains("carbon fiber red") || s.Contains("carbonfiber_001"))
                return O(BikeOrange, .55f);
            if (s.Contains("brakes") || s.Contains("bikedisc")) return O(new Color(.72f, .73f, .76f), .6f, .7f);
            if (s.Contains("engine") || s.Contains("exhaust") || s.Contains("gear") || s.Contains("chain") || s.Contains("gold") || s.Contains("braided"))
                return O(new Color(.3f, .31f, .34f), .5f, .6f);
            return O(new Color(.07f, .07f, .08f), .3f);
        }

        static Bounds MinMax(Vector3 a, Vector3 b) { var x = new Bounds(); x.SetMinMax(a, b); return x; }

        static Built Wrap(GlbImporter.Spec spec, string name, int kind, bool addRider = true)
        {
            string folder = Folder + "/Models/" + name;
            var result = GlbImporter.Build(spec, folder, Fallback());
            var root = new GameObject(name);
            result.root.name = "Model"; result.root.transform.SetParent(root.transform, false);
            var b = result.bounds;
            var built = new Built { length = b.size.x, width = b.size.z, height = b.size.y, triangles = result.triangles };
            var mover = root.AddComponent<VehicleMover>();
            mover.vehicleName = kind == 1 ? "CAR" : kind == 2 ? "TRAIN" : "BIKE";
            mover.lane = kind == 1 ? 0 : kind == 2 ? 1 : 2;
            var wheelParts = result.parts.Where(kv => kv.Key.StartsWith("Wheel")).Select(kv => kv.Value).ToArray();
            mover.wheels = wheelParts;
            if (wheelParts.Length > 0)
            {
                var mf = wheelParts[0].GetComponent<MeshFilter>(); built.wheelRadius = mf.sharedMesh.bounds.extents.y;
                mover.wheelRadius = built.wheelRadius;
            }
            mover.bogies = result.parts.Where(kv => kv.Key.StartsWith("Bogie")).Select(kv => kv.Value).ToArray();
            if (kind == 2) { mover.hum = true; mover.wheelRadius = .45f; }
            mover.engineBaseHz = kind == 1 ? 110 : 160;
            // Hazard lights: two small emissive amber blocks at the rear corners.
            var hazardMat = SpeedTimeFreeMaterials.Unlit("Hazard amber", new Color(1f, .62f, .05f));
            var hz = new List<Renderer>();
            foreach (float side in new[] { -1f, 1f })
            {
                var h = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(h.GetComponent<Collider>());
                h.name = "Hazard light"; h.transform.SetParent(root.transform, false);
                float s = Mathf.Clamp(b.size.y * .09f, .12f, .35f);
                h.transform.localPosition = new Vector3(b.min.x - s * .3f, b.size.y * (kind == 3 ? .55f : .45f), side * b.extents.z * .78f);
                h.transform.localScale = new Vector3(s * .4f, s, s * 1.4f);
                var r = h.GetComponent<MeshRenderer>(); r.sharedMaterial = hazardMat; r.shadowCastingMode = ShadowCastingMode.Off; r.enabled = false; hz.Add(r);
            }
            mover.hazards = hz.ToArray();
            if (kind == 3 && addRider) mover.rider = Rider(root.transform, result.map);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>()) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            built.ghostMesh = GhostHull(root, name);
            string path = Folder + "/Prefabs/" + name + ".prefab";
            built.prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            built.report = $"{name}: {built.triangles:N0} triangles, {b.size.x:0.00} x {b.size.y:0.00} x {b.size.z:0.00} units, wheel radius {built.wheelRadius:0.00}, parts [{string.Join(", ", result.parts.Keys)}]";
            Debug.Log("[STGF] " + built.report);
            return built;
        }

        /// <summary>Low-poly helmeted rider (capsules + sphere helmet, orange/black) placed from bike reference points.</summary>
        static Transform Rider(Transform bike, Func<Vector3, Vector3> map)
        {
            var orange = SpeedTimeFreeMaterials.Lit("Rider orange", BikeOrange); var black = SpeedTimeFreeMaterials.Lit("Rider black", new Color(.06f, .06f, .08f));
            var visor = SpeedTimeFreeMaterials.Lit("Rider visor", new Color(.05f, .08f, .12f), .9f);
            Vector3 seat = map(new Vector3(0, .43f, .28f)), gripL = map(new Vector3(-.26f, .47f, -.44f)), gripR = map(new Vector3(.26f, .47f, -.44f));
            Vector3 pegL = map(new Vector3(-.17f, .02f, .2f)), pegR = map(new Vector3(.17f, .02f, .2f));
            float u = Vector3.Distance(gripL, gripR) / .52f * .1f;   // ~10 cm of real bike in world units
            var rider = new GameObject("Helmeted rider").transform; rider.SetParent(bike, false); rider.localPosition = seat;
            Vector3 L(Vector3 w) => w - seat;
            Vector3 hips = Vector3.up * 1.2f * u, shoulders = L(Vector3.Lerp(seat, (gripL + gripR) * .5f, .55f)) + Vector3.up * 3.6f * u;
            Limb(rider, "Torso", hips, shoulders, 1.35f * u, orange);
            var helmet = Ball(rider, "Helmet", shoulders + new Vector3(1.3f * u, 1.8f * u, 0), 2.35f * u, orange);
            Ball(helmet, "Visor", new Vector3(.32f, .05f, 0), .78f, visor).localScale = new Vector3(.55f, .55f, .82f);
            foreach (var side in new[] { -1, 1 })
            {
                Vector3 sh = shoulders + new Vector3(0, -.3f * u, side * 1.35f * u);
                var grip = L(side < 0 ? gripL : gripR);
                Vector3 elbow = Vector3.Lerp(sh, grip, .5f) + new Vector3(0, -.6f * u, side * .6f * u);
                Limb(rider, "Upper arm", sh, elbow, .55f * u, orange); Limb(rider, "Forearm", elbow, grip, .5f * u, black);
                Vector3 hip = hips + new Vector3(0, 0, side * 1.1f * u), peg = L(side < 0 ? pegL : pegR) + new Vector3(0, .4f * u, side * .4f * u);
                Vector3 knee = new Vector3(Mathf.Max(hip.x, peg.x) + 2.6f * u, Mathf.Lerp(hip.y, peg.y, .35f) + .4f * u, side * 2.0f * u);
                Limb(rider, "Thigh", hip, knee, .75f * u, black); Limb(rider, "Shin", knee, peg, .6f * u, black);
            }
            foreach (var c in rider.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return rider;
        }
        static void Limb(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Capsule); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = (a + b) * .5f; g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            g.transform.localScale = new Vector3(radius * 2, (Vector3.Distance(a, b) + radius * 2) * .5f, radius * 2);
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
        }
        static Transform Ball(Transform parent, string name, Vector3 at, float diameter, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = at; g.transform.localScale = Vector3.one * diameter; g.GetComponent<MeshRenderer>().sharedMaterial = m;
            return g.transform;
        }

        /// <summary>
        /// Translucent ghost proxy: a lofted hull through 24 cross-sections fitted to the vehicle silhouette
        /// (about 200 triangles). Ghosts never duplicate the heavy meshes.
        /// </summary>
        static Mesh GhostHull(GameObject root, string name)
        {
            const int slices = 24;
            var all = new List<Vector3>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.name.StartsWith("Hazard")) continue;
                var vs = mf.sharedMesh.vertices; var m = root.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int i = 0; i < vs.Length; i += Math.Max(1, vs.Length / 20000)) all.Add(m.MultiplyPoint3x4(vs[i]));
            }
            float minX = all.Min(v => v.x), maxX = all.Max(v => v.x);
            var lo = new float[slices + 1]; var hi = new float[slices + 1]; var hw = new float[slices + 1];
            for (int s = 0; s <= slices; s++) { lo[s] = float.MaxValue; hi[s] = float.MinValue; hw[s] = 0; }
            foreach (var v in all)
            {
                int s = Mathf.Clamp(Mathf.RoundToInt((v.x - minX) / (maxX - minX) * slices), 0, slices);
                lo[s] = Mathf.Min(lo[s], v.y); hi[s] = Mathf.Max(hi[s], v.y); hw[s] = Mathf.Max(hw[s], Mathf.Abs(v.z));
            }
            for (int s = 0; s <= slices; s++) if (lo[s] > hi[s]) { lo[s] = s > 0 ? lo[s - 1] : 0; hi[s] = s > 0 ? hi[s - 1] : .1f; hw[s] = s > 0 ? hw[s - 1] : .1f; }
            var verts = new List<Vector3>(); var tris = new List<int>();
            for (int s = 0; s <= slices; s++)
            {
                float x = Mathf.Lerp(minX, maxX, s / (float)slices);
                verts.Add(new Vector3(x, lo[s], -hw[s])); verts.Add(new Vector3(x, hi[s], -hw[s] * .8f)); verts.Add(new Vector3(x, hi[s], hw[s] * .8f)); verts.Add(new Vector3(x, lo[s], hw[s]));
            }
            for (int s = 0; s < slices; s++)
                for (int k = 0; k < 4; k++)
                {
                    int a = s * 4 + k, b = s * 4 + (k + 1) % 4, c = a + 4, d = b + 4;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }
            tris.AddRange(new[] { 0, 1, 2, 0, 2, 3 }); int e = slices * 4; tris.AddRange(new[] { e, e + 2, e + 1, e, e + 3, e + 2 });
            var mesh = new Mesh { name = name + " ghost hull" }; mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Folder + "/Models/" + name + "/" + name + "_GhostHull.asset"; AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>One track tile from train_track.glb, stretched so three tiles cover x = -15..65 exactly.</summary>
        public static GameObject TrackTilePrefab(float tileLength)
        {
            var natural = GlbImporter.Load(Src + "train_track.glb");
            var min = Vector3.positiveInfinity; var max = Vector3.negativeInfinity;
            foreach (var p in natural.primitives) foreach (var v in p.positions) { var w = p.world.MultiplyPoint3x4(v); min = Vector3.Min(min, w); max = Vector3.Max(max, w); }
            float naturalLength = max.z - min.z;
            var spec = new GlbImporter.Spec
            {
                name = "Track", source = Src + "train_track.glb", forwardSign = 1, length = naturalLength, stretchX = tileLength / naturalLength,
                pivotAtNose = false,
                recolor = { ["Track_Rust"] = new Color(.42f, .27f, .2f), ["Sleeper"] = new Color(.36f, .25f, .17f), ["Track_Silver"] = new Color(.75f, .77f, .8f) },
            };
            var result = GlbImporter.Build(spec, Folder + "/Models/Track", Fallback());
            result.root.name = "Track tile";
            foreach (var r in result.root.GetComponentsInChildren<MeshRenderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; }
            var prefab = PrefabUtility.SaveAsPrefabAsset(result.root, Folder + "/Prefabs/TrackTile.prefab");
            Object.DestroyImmediate(result.root);
            Debug.Log($"[STGF] Track tile: {result.triangles:N0} triangles, natural length {naturalLength:0.00}, stretched to {tileLength:0.00}, height {result.bounds.size.y:0.00}, width {result.bounds.size.z:0.00}");
            return prefab;
        }

        // ------------------------------------------------------------------ quick visual check (batch)
        public static void BatchVehicleCheck()
        {
            try
            {
                Directory.CreateDirectory("Logs/SpeedTimeGraphFree");
                foreach (var d in new[] { "Models", "Prefabs", "Materials", "Generated" }) Directory.CreateDirectory(Folder + "/" + d);
                var report = new List<string>();
                foreach (var f in new[] { "lambo/source/lamborghini_venevo.glb", "Train.glb", "Motor_Cycle/source/10211.glb" }) report.Add(f + "\n" + GlbImporter.Describe(Src + f));
                var car = Car(); var train = Train(); var bike = Motorcycle(); var track = TrackTilePrefab(80f / 3f);
                report.Add(car.report); report.Add(train.report); report.Add(bike.report);
                File.WriteAllText("Logs/SpeedTimeGraphFree/vehicles_report.txt", string.Join("\n", report));
                AssetDatabase.SaveAssets();
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
                var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(40, -40, 0); sun.intensity = 1.3f;
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.6f, .65f, .7f);
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.localScale = Vector3.one * 10; ground.GetComponent<Renderer>().sharedMaterial = SpeedTimeFreeMaterials.Lit("Check ground", new Color(.45f, .6f, .35f));
                var a = (GameObject)PrefabUtility.InstantiatePrefab(car.prefab); a.transform.position = new Vector3(0, 0, 8);
                var t = (GameObject)PrefabUtility.InstantiatePrefab(train.prefab); t.transform.position = new Vector3(8, 0, 0);
                var m = (GameObject)PrefabUtility.InstantiatePrefab(bike.prefab); m.transform.position = new Vector3(0, 0, -6);
                var tr = (GameObject)PrefabUtility.InstantiatePrefab(track); tr.transform.position = new Vector3(-12, 0, -12);
                var cam = new GameObject("Cam").AddComponent<Camera>(); cam.fieldOfView = 50; cam.backgroundColor = new Color(.55f, .75f, .95f); cam.clearFlags = CameraClearFlags.SolidColor;
                void Shot(string file, Vector3 pos, Vector3 look)
                {
                    cam.transform.position = pos; cam.transform.LookAt(look);
                    var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                    var img = new Texture2D(1600, 900, TextureFormat.RGB24, false); img.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); img.Apply();
                    File.WriteAllBytes("Logs/SpeedTimeGraphFree/" + file, img.EncodeToPNG()); cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(img); rt.Release();
                }
                Shot("vehicles_side.png", new Vector3(-2, 4, -26), new Vector3(-2, 1.5f, 2));
                Shot("vehicles_front.png", new Vector3(14, 6, -14), new Vector3(-2, 1, 2));
                Shot("bike_close.png", new Vector3(-1.5f, 2.2f, -10.5f), new Vector3(-1.5f, 1, -6));
                Shot("car_close.png", new Vector3(-2.5f, 3, 1.5f), new Vector3(-2.7f, 1, 8));
                EditorApplication.Exit(0);
            }
            catch (Exception e) { File.WriteAllText("Logs/SpeedTimeGraphFree/build-error.txt", e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}