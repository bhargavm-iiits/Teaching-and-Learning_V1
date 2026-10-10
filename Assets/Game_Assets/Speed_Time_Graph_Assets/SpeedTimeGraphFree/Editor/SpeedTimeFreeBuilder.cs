using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Tools > Speed Time Graph Free > Rebuild and Validate. Deterministic and re-runnable:
    /// 1 hash scripts, 2 back up the scene, 3 convert the models, 4 open the scene and remove the old roots,
    /// 5 build environment, TripleVehicleArena prefab, deck + XR rig, EventSystem, panels, managers,
    /// 6 wire references + JSON, 7 save (Build Settings untouched), 8 validate.
    /// Never calls the old Tools > Speed Time Graph menus.
    /// </summary>
    public static partial class SpeedTimeFreeBuilder
    {
        public const string ScenePath = "Assets/Scenes/Speed_Time_Graph.unity", Folder = "Assets/SpeedTimeGraphFree";
        public const string RootName = "SpeedTimeGraphFree • Scene";
        // Camera pose requested by the user (fitted to their Scene-view screenshot): low three-quarter view from the
        // STOP side looking back along the lanes toward the tunnel. Used for the whole game.
        public static readonly Vector3 Eye = new Vector3(39.4f, 11.5f, -17.8f);
        public const float EyeYaw = -43.5f, EyePitch = 13.6f, EyeHeight = 1.6f, Fov = 60;
        static readonly string[] OldRoots = { "Speed Time Graph • First Person VR", RootName, "Main Camera", "Directional Light", "Global Volume" };

        static Transform root; static GameFlowManager game; static Camera eye; static QuestionCatalog catalog;
        static TMP_FontAsset fontBody, fontBold;

        [MenuItem("Tools/Speed Time Graph Free/Rebuild and Validate %#&s")]
        public static void Rebuild() { Rebuild(true); }

        public static void Rebuild(bool convertModels)
        {
            Directory.CreateDirectory("Logs/SpeedTimeGraphFree");
            SpeedTimeFreeValidation.RecordBaselineIfMissing();
            Directory.CreateDirectory("Work/SpeedTimeGraphFree/backup");
            if (File.Exists(ScenePath)) File.Copy(ScenePath, $"Work/SpeedTimeGraphFree/backup/Speed_Time_Graph_{DateTime.Now:yyyyMMdd_HHmmss}.unity", true);
            foreach (var d in new[] { "Models", "Materials", "Prefabs", "Generated", "Data" }) Directory.CreateDirectory(Folder + "/" + d);

            var vehicles = convertModels || !File.Exists(Folder + "/Prefabs/Car.prefab") ? BuildVehicles() : LoadVehicles();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (OldRoots.Contains(go.name) || go.GetComponentsInChildren<MonoBehaviour>(true).Any(m => m && m.GetType().Namespace != null && m.GetType().Namespace.StartsWith("SpeedTimeGraph")))
                { Debug.Log("[STGF] removing old root " + go.name); go.SetActive(false); Object.DestroyImmediate(go); }
            foreach (var go in scene.GetRootGameObjects()) Debug.Log("[STGF] keeping existing root " + go.name);

            root = new GameObject(RootName).transform;
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/Data/speed_time_free_questions.json");
            catalog = QuestionCatalog.Load(json.text, s => JsonUtility.FromJson<QuestionCatalog>(s));
            Fonts(); Sprites();

            Environment();                                   // 5.1
            var arena = BuildArena(vehicles);                // 5.2 (saved as TripleVehicleArena.prefab below)
            Rig();                                           // 5.3 + 5.4
            game = root.gameObject.AddComponent<GameFlowManager>(); game.data = json;
            Panels(arena);                                   // 5.5
            Managers(arena);                                 // 5.6 + wiring
            SaveArenaPrefab(arena);

            game.catalog = null; game.variant = null; game.Initialize();
            game.part = Part.Demo; game.mode = Mode.Edit; game.popup.Hide(); game.finalCheck.Hide(); game.Refresh();
            Lighting.Apply();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            BakeOcclusion(scene);
            Debug.Log("[STGF] scene saved: " + ScenePath);
            SpeedTimeFreeValidation.Validate(game, eye, vehicles);
        }

        /// <summary>
        /// Occlusion culling: the static world (terrain, hills, lanes, tunnel, trees, rocks, signs) is marked
        /// Occluder/Occludee static, the camera culls, and the data is baked for this scene.
        /// </summary>
        static void BakeOcclusion(Scene scene)
        {
            var env = root.Find("Environment (static)"); var lanes = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Lanes (static)");
            foreach (var group in new[] { env, lanes })
                if (group) foreach (var t in group.GetComponentsInChildren<Transform>(true))
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            eye.useOcclusionCulling = true;
            StaticOcclusionCulling.smallestOccluder = 3f; StaticOcclusionCulling.smallestHole = .25f; StaticOcclusionCulling.backfaceThreshold = 100;
            var t0 = DateTime.Now;
            bool ok = StaticOcclusionCulling.Compute();
            Debug.Log($"[STGF] occlusion culling baked: {ok}, {StaticOcclusionCulling.umbraDataSize:N0} bytes in {(DateTime.Now - t0).TotalSeconds:0.0} s");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, ScenePath);
        }

        /// <summary>Batch entry: rebuild, then capture screenshots after a few editor frames.</summary>
        public static void BatchBuild()
        {
            try
            {
                if (File.Exists("Logs/SpeedTimeGraphFree/build-error.txt")) File.Delete("Logs/SpeedTimeGraphFree/build-error.txt");
                bool convert = !System.Environment.GetCommandLineArgs().Contains("-stgfSkipModels");
                Rebuild(convert);
                int frames = 0;
                void Tick()
                {
                    if (++frames < 20) return;
                    EditorApplication.update -= Tick;
                    try { SpeedTimeFreeValidation.CaptureAll(game, eye); EditorApplication.Exit(0); }
                    catch (Exception e) { Fail(e); }
                }
                EditorApplication.update += Tick;
            }
            catch (Exception e) { Fail(e); }
        }
        static void Fail(Exception e)
        {
            Directory.CreateDirectory("Logs/SpeedTimeGraphFree");
            File.WriteAllText("Logs/SpeedTimeGraphFree/build-error.txt", e.ToString()); Debug.LogException(e); EditorApplication.Exit(1);
        }

        // ------------------------------------------------------------------ vehicles
        public class VehicleSet { public SpeedTimeFreeVehicles.Built car, train, bike; public GameObject track; public Material ghost, ghostFaint; }
        static VehicleSet BuildVehicles()
        {
            var set = new VehicleSet { car = SpeedTimeFreeVehicles.Car(), train = SpeedTimeFreeVehicles.Train(), bike = SpeedTimeFreeVehicles.Motorcycle(), track = SpeedTimeFreeVehicles.TrackTilePrefab(80f / 3f) };
            GhostMaterials(set); AssetDatabase.SaveAssets(); return set;
        }
        static VehicleSet LoadVehicles()
        {
            SpeedTimeFreeVehicles.Built L(string n)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Prefabs/" + n + ".prefab");
                var b = new SpeedTimeFreeVehicles.Built { prefab = prefab, ghostMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Models/" + n + "/" + n + "_GhostHull.asset") };
                var bounds = new Bounds(); bool first = true;
                foreach (var r in prefab.GetComponentsInChildren<MeshFilter>()) { var mb = r.sharedMesh.bounds; var c = r.transform.localPosition + mb.center; var wb = new Bounds(c, mb.size); if (first) { bounds = wb; first = false; } else bounds.Encapsulate(wb); b.triangles += r.sharedMesh.triangles.Length / 3; }
                b.length = bounds.size.x; b.width = bounds.size.z; b.height = bounds.size.y; return b;
            }
            var set = new VehicleSet { car = L("Car"), train = L("Train"), bike = L("Motorcycle"), track = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Prefabs/TrackTile.prefab") };
            GhostMaterials(set); return set;
        }
        static void GhostMaterials(VehicleSet set)
        {
            set.ghost = SpeedTimeFreeMaterials.Unlit("Ghost cyan", new Color(.16f, .86f, 1f, .32f), true, true);
            set.ghostFaint = SpeedTimeFreeMaterials.Unlit("Ghost cyan faint", new Color(.16f, .86f, 1f, .16f), true, true);
        }

        // ------------------------------------------------------------------ shared helpers
        static GameObject Group(string name, Transform parent) { var g = new GameObject(name); g.transform.SetParent(parent ? parent : root, false); return g; }
        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 at, Vector3 scale, Material m, bool shadows = true)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = at; g.transform.localScale = scale;
            var c = g.GetComponent<Collider>(); if (c) Object.DestroyImmediate(c);
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return g;
        }
        static GameObject Box(string name, Transform parent, Vector3 at, Vector3 size, Material m, bool shadows = true) => Prim(PrimitiveType.Cube, name, parent, at, size, m, shadows);
        static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material m, Vector3 at, bool shadows = true)
        {
            var g = Group(name, parent); g.transform.localPosition = at;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off; return g;
        }
        static Mesh SaveMesh(Mesh mesh, string name)
        {
            // Every generated mesh gets UVs and tangents so baking (occlusion, lightmaps) can copy all vertex channels.
            if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount) { var uv = new Vector2[mesh.vertexCount]; var v = mesh.vertices; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(v[i].x, v[i].z) * .1f; mesh.uv = uv; }
            if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount) mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            string path = Folder + "/Generated/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { existing.Clear(); EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        /// <summary>3D TextMeshPro sign text (world units). One text line at scale 1 is ~fontSize/10 units tall.</summary>
        static TextMeshPro WorldText(string name, Transform parent, Vector3 at, string text, float lineHeight, Color color, float width, bool bold = true, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro)); g.transform.SetParent(parent, false);
            var t = g.GetComponent<TextMeshPro>(); t.font = bold ? fontBold : fontBody; t.fontSize = 40; t.color = color; t.text = text; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = false;
            float scale = lineHeight / 4f;                       // fontSize 40 ~ 4 units per line at scale 1
            g.transform.localScale = Vector3.one * scale; g.transform.localPosition = at;
            t.rectTransform.sizeDelta = new Vector2(width / scale, lineHeight / scale * 2.2f);
            return t;
        }
        static void FaceEye(Transform t, bool yawOnly = true)
        {
            var d = t.position - Eye; if (yawOnly) d.y = 0;
            t.rotation = Quaternion.LookRotation(d, Vector3.up);
        }

        // ------------------------------------------------------------------ fonts
        static Material outlineMaterial;
        /// <summary>
        /// Shared outlined material for 3D labels. Setting TMP outlineWidth in edit mode would instantiate
        /// (leak) one material per label, so every outlined label uses this one asset instead.
        /// </summary>
        static void Outline(TMP_Text t)
        {
            if (!outlineMaterial)
            {
                string path = Folder + "/Generated/STGF Bold Outline.mat";
                outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!outlineMaterial)
                {
                    outlineMaterial = new Material(fontBold.material) { name = "STGF Bold Outline" };
                    outlineMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
                    outlineMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, .25f);
                    outlineMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(5, 20, 45, 255));
                    AssetDatabase.CreateAsset(outlineMaterial, path);
                }
                var atlas = fontBold.material.GetTexture(ShaderUtilities.ID_MainTex);
                if (outlineMaterial.GetTexture(ShaderUtilities.ID_MainTex) != atlas) { outlineMaterial.SetTexture(ShaderUtilities.ID_MainTex, atlas); EditorUtility.SetDirty(outlineMaterial); }
            }
            if (t.font != fontBold) t.font = fontBold;
            t.fontSharedMaterial = outlineMaterial;
        }

        static void Fonts()
        {
            outlineMaterial = null;
            fontBody = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/Generated/STGF Body SDF.asset");
            if (!fontBody)
            {
                fontBody = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"));
                fontBody.name = "STGF Body SDF"; AssetDatabase.CreateAsset(fontBody, Folder + "/Generated/STGF Body SDF.asset");
            }
            fontBold = DynamicFont("Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold.ttf", "STGF Bold SDF") ?? fontBody;
            string symbolsTtf = Folder + "/Generated/seguisym.ttf";
            if (!File.Exists(symbolsTtf) && File.Exists("C:/Windows/Fonts/seguisym.ttf")) { File.Copy("C:/Windows/Fonts/seguisym.ttf", symbolsTtf); AssetDatabase.ImportAsset(symbolsTtf); }
            var symbols = DynamicFont(symbolsTtf, "STGF Symbols SDF");
            var bodyFallbacks = new List<TMP_FontAsset>(); if (symbols) bodyFallbacks.Add(symbols);
            // Only write the font assets when the fallback chain differs: dirtying a font asset makes TMP broadcast a
            // font change to every label, including labels of the scene roots this rebuild has just destroyed.
            if (fontBody.fallbackFontAssetTable == null || !fontBody.fallbackFontAssetTable.SequenceEqual(bodyFallbacks)) { fontBody.fallbackFontAssetTable = bodyFallbacks; EditorUtility.SetDirty(fontBody); }
            LockGlyphs(fontBody, GlyphSet(), "body"); if (fontBold != fontBody) LockGlyphs(fontBold, GlyphSet(), "bold"); if (symbols) LockGlyphs(symbols, SymbolSet, "symbols");
            if (fontBold != fontBody && (fontBold.fallbackFontAssetTable == null || fontBold.fallbackFontAssetTable.Count != 1 || fontBold.fallbackFontAssetTable[0] != fontBody))
            { fontBold.fallbackFontAssetTable = new List<TMP_FontAsset> { fontBody }; EditorUtility.SetDirty(fontBold); }
        }
        const string SymbolSet = "①②③▶❚⟲✓○✗■╱━╲→←≥≤÷×²Δ−·—–•°";
        static string GlyphSet()
        {
            var set = new System.Text.StringBuilder();
            for (char c = (char)32; c < 127; c++) set.Append(c);
            set.Append(SymbolSet);
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/Data/speed_time_free_questions.json");
            if (json) foreach (char c in json.text) if (c >= 160) set.Append(c);
            return new string(set.ToString().Distinct().ToArray());
        }
        /// <summary>Make sure every glyph exists, then keep the atlas static (no editor-time font changes).</summary>
        static void LockGlyphs(TMP_FontAsset font, string chars, string label)
        {
            if (!font) return;
            if (font.atlasPopulationMode == AtlasPopulationMode.Static && font.HasCharacters(chars, out List<char> _)) return;
            var mode = font.atlasPopulationMode;
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.TryAddCharacters(chars, out string missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            foreach (var atlas in font.atlasTextures) if (atlas && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font);
            Debug.Log($"[STGF] {label} font atlas locked (static); glyphs not in this font (served by fallbacks): {missing?.Length ?? 0}");
        }

        static TMP_FontAsset DynamicFont(string ttf, string name)
        {
            string path = Folder + "/Generated/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); if (existing) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(ttf); if (!source) return null;
            var fa = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            fa.name = name; AssetDatabase.CreateAsset(fa, path);
            fa.material.name = name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa);
            foreach (var atlas in fa.atlasTextures) { atlas.name = name + " Atlas"; AssetDatabase.AddObjectToAsset(atlas, fa); }
            return fa;
        }

        // ------------------------------------------------------------------ rounded / glow sprites (generated PNGs, 9-sliced)
        static Sprite rounded, glow, ring;
        static void Sprites()
        {
            rounded = SpriteAsset("RoundedPanel", 128, 30, 0, 40);
            glow = SpriteAsset("GlowFrameHollow", 160, 34, 24, 62);
            ring = SpriteAsset("RoundedOutline", 128, 30, 0, 40, true);
        }
        static Sprite SpriteAsset(string name, int size, float radius, float blur, int border, bool hollow = false)
        {
            string path = Folder + "/Generated/" + name + ".png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float pad = blur; float half = size * .5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        // signed distance to a rounded rectangle inset by pad
                        float px = Mathf.Abs(x + .5f - half) - (half - pad - radius), py = Mathf.Abs(y + .5f - half) - (half - pad - radius);
                        float outside = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - radius;
                        float a;
                        if (hollow) a = Mathf.Clamp01(1.5f - Mathf.Abs(outside + 3.5f) / 2.2f);
                        else if (blur > 0) a = outside <= 0 ? Mathf.Clamp01(1 + outside / 6f) * .9f : Mathf.Pow(Mathf.Clamp01(1 - outside / blur), 2.2f) * .85f;
                        else a = Mathf.Clamp01(.5f - outside);
                        tex.SetPixel(x, y, new Color(1, 1, 1, a));
                    }
                tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.alphaIsTransparency = true; imp.mipmapEnabled = false;
            imp.spriteBorder = new Vector4(border, border, border, border); imp.filterMode = FilterMode.Bilinear; imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}