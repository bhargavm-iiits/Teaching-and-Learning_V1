using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Minimal, dependency-free glTF 2.0 binary (.glb) reader for the four source models in
    /// Assets/Speed_Time_Graph_Assets. The source files are never modified: meshes, textures and
    /// URP/Lit materials are written as new assets under Assets/SpeedTimeGraphFree/Models.
    /// (Option A of the brief: no package change. This replaces the OBJ round trip, which loses
    /// normals, material colours and per-node grouping.)
    /// </summary>
    public static class GlbImporter
    {
        // ---------------------------------------------------------------- tiny JSON parser
        sealed class Json
        {
            readonly string s; int i;
            Json(string text) { s = text; }
            public static object Parse(string text) { var j = new Json(text); return j.Value(); }
            void Ws() { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
            object Value()
            {
                Ws(); char c = s[i];
                if (c == '{') { i++; var d = new Dictionary<string, object>(); Ws(); if (s[i] == '}') { i++; return d; }
                    while (true) { Ws(); string k = Str(); Ws(); i++; d[k] = Value(); Ws(); if (s[i] == ',') { i++; continue; } i++; return d; } }
                if (c == '[') { i++; var l = new List<object>(); Ws(); if (s[i] == ']') { i++; return l; }
                    while (true) { l.Add(Value()); Ws(); if (s[i] == ',') { i++; continue; } i++; return l; } }
                if (c == '"') return Str();
                if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
                if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
                if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
                int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
            }
            string Str()
            {
                i++; var sb = new StringBuilder();
                while (s[i] != '"')
                {
                    if (s[i] == '\\')
                    {
                        i++; char e = s[i];
                        if (e == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                        else sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'r' ? '\r' : e == 'b' ? '\b' : e == 'f' ? '\f' : e);
                    }
                    else sb.Append(s[i]);
                    i++;
                }
                i++; return sb.ToString();
            }
        }

        static Dictionary<string, object> O(object o) => o as Dictionary<string, object>;
        static List<object> L(object o) => o as List<object> ?? new List<object>();
        static double D(object o, double fallback = 0) => o is double d ? d : fallback;
        static object Get(Dictionary<string, object> d, string key) => d != null && d.TryGetValue(key, out var v) ? v : null;

        // ---------------------------------------------------------------- document
        public sealed class Primitive
        {
            public string node, mesh, material; public int materialIndex;
            public Vector3[] positions, normals; public Vector2[] uvs; public int[] indices;
            public Matrix4x4 world;
        }

        public sealed class Document
        {
            public Dictionary<string, object> json; public byte[] bin; public string path;
            public List<Primitive> primitives = new List<Primitive>();
            public List<Dictionary<string, object>> materials = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> images = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> textures = new List<Dictionary<string, object>>();
            public byte[] ImageBytes(int index, out string ext)
            {
                var img = images[index]; ext = ((Get(img, "mimeType") as string) ?? "").Contains("png") ? "png" : "jpg";
                if (Get(img, "bufferView") is double bv) { var view = O(L(json["bufferViews"])[(int)bv]); int off = (int)D(Get(view, "byteOffset")); int len = (int)D(view["byteLength"]); var bytes = new byte[len]; Buffer.BlockCopy(bin, off, bytes, 0, len); return bytes; }
                string uri = Get(img, "uri") as string; ext = Path.GetExtension(uri).TrimStart('.');
                return File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(path), uri));
            }
        }

        public static Document Load(string path)
        {
            var raw = File.ReadAllBytes(path);
            if (BitConverter.ToUInt32(raw, 0) != 0x46546C67) throw new Exception("Not a GLB: " + path);
            int jsonLen = BitConverter.ToInt32(raw, 12);
            var doc = new Document { path = path, json = O(Json.Parse(Encoding.UTF8.GetString(raw, 20, jsonLen))) };
            int binStart = 20 + jsonLen; int binLen = BitConverter.ToInt32(raw, binStart);
            doc.bin = new byte[binLen]; Buffer.BlockCopy(raw, binStart + 8, doc.bin, 0, binLen);
            doc.materials = L(Get(doc.json, "materials")).Select(O).ToList();
            doc.images = L(Get(doc.json, "images")).Select(O).ToList();
            doc.textures = L(Get(doc.json, "textures")).Select(O).ToList();
            var nodes = L(doc.json["nodes"]); var meshes = L(doc.json["meshes"]);
            var scene = O(L(doc.json["scenes"])[(int)D(Get(doc.json, "scene"))]);
            void Visit(int index, Matrix4x4 parent)
            {
                var n = O(nodes[index]); var m = parent * LocalMatrix(n); string name = Get(n, "name") as string ?? ("node" + index);
                if (Get(n, "mesh") is double mi)
                {
                    var mesh = O(meshes[(int)mi]);
                    foreach (var po in L(mesh["primitives"]))
                    {
                        var p = O(po); if ((int)D(Get(p, "mode"), 4) != 4) continue;
                        var attr = O(p["attributes"]);
                        var prim = new Primitive { node = name, mesh = Get(mesh, "name") as string ?? "", world = m };
                        prim.materialIndex = (int)D(Get(p, "material"), -1);
                        prim.material = prim.materialIndex >= 0 ? (Get(doc.materials[prim.materialIndex], "name") as string ?? "m" + prim.materialIndex) : "default";
                        prim.positions = ReadVec3(doc, (int)D(attr["POSITION"]));
                        prim.normals = Get(attr, "NORMAL") is double ni ? ReadVec3(doc, (int)ni) : null;
                        prim.uvs = Get(attr, "TEXCOORD_0") is double ti ? ReadVec2(doc, (int)ti) : null;
                        prim.indices = Get(p, "indices") is double ii ? ReadIndices(doc, (int)ii) : Enumerable.Range(0, prim.positions.Length).ToArray();
                        doc.primitives.Add(prim);
                    }
                }
                foreach (var c in L(Get(n, "children"))) Visit((int)D(c), m);
            }
            foreach (var r in L(scene["nodes"])) Visit((int)D(r), Matrix4x4.identity);
            return doc;
        }

        static Matrix4x4 LocalMatrix(Dictionary<string, object> n)
        {
            if (Get(n, "matrix") is List<object> mm)
            {
                var m = new Matrix4x4();
                for (int c = 0; c < 4; c++) for (int r = 0; r < 4; r++) m[r, c] = (float)D(mm[c * 4 + r]);
                return m;
            }
            var t = L(Get(n, "translation")); var q = L(Get(n, "rotation")); var s = L(Get(n, "scale"));
            var T = t.Count == 3 ? new Vector3((float)D(t[0]), (float)D(t[1]), (float)D(t[2])) : Vector3.zero;
            var R = q.Count == 4 ? new Quaternion((float)D(q[0]), (float)D(q[1]), (float)D(q[2]), (float)D(q[3])) : Quaternion.identity;
            var S = s.Count == 3 ? new Vector3((float)D(s[0]), (float)D(s[1]), (float)D(s[2])) : Vector3.one;
            return Matrix4x4.TRS(T, R.normalized, S);
        }

        static float[] ReadFloats(Document doc, int accessor, out int components)
        {
            var a = O(L(doc.json["accessors"])[accessor]); var view = O(L(doc.json["bufferViews"])[(int)D(a["bufferView"])]);
            int count = (int)D(a["count"]); string type = (string)a["type"]; int ct = (int)D(a["componentType"]);
            components = type == "SCALAR" ? 1 : type == "VEC2" ? 2 : type == "VEC3" ? 3 : 4;
            int size = ct == 5126 || ct == 5125 ? 4 : ct == 5123 || ct == 5122 ? 2 : 1;
            int stride = (int)D(Get(view, "byteStride"), size * components);
            int offset = (int)D(Get(view, "byteOffset")) + (int)D(Get(a, "byteOffset"));
            bool normalized = Get(a, "normalized") is bool b && b;
            var result = new float[count * components];
            for (int i = 0; i < count; i++)
                for (int c = 0; c < components; c++)
                {
                    int at = offset + i * stride + c * size; float v;
                    switch (ct)
                    {
                        case 5126: v = BitConverter.ToSingle(doc.bin, at); break;
                        case 5125: v = BitConverter.ToUInt32(doc.bin, at); break;
                        case 5123: v = BitConverter.ToUInt16(doc.bin, at); if (normalized) v /= 65535f; break;
                        case 5122: v = BitConverter.ToInt16(doc.bin, at); if (normalized) v = Mathf.Max(v / 32767f, -1); break;
                        case 5121: v = doc.bin[at]; if (normalized) v /= 255f; break;
                        default: v = (sbyte)doc.bin[at]; if (normalized) v = Mathf.Max(v / 127f, -1); break;
                    }
                    result[i * components + c] = v;
                }
            return result;
        }
        static Vector3[] ReadVec3(Document doc, int accessor) { var f = ReadFloats(doc, accessor, out _); var r = new Vector3[f.Length / 3]; for (int i = 0; i < r.Length; i++) r[i] = new Vector3(f[i * 3], f[i * 3 + 1], f[i * 3 + 2]); return r; }
        static Vector2[] ReadVec2(Document doc, int accessor) { var f = ReadFloats(doc, accessor, out _); var r = new Vector2[f.Length / 2]; for (int i = 0; i < r.Length; i++) r[i] = new Vector2(f[i * 2], f[i * 2 + 1]); return r; }
        static int[] ReadIndices(Document doc, int accessor) { var f = ReadFloats(doc, accessor, out _); var r = new int[f.Length]; for (int i = 0; i < r.Length; i++) r[i] = (int)f[i]; return r; }

        // ---------------------------------------------------------------- conversion
        /// <summary>
        /// How a source model maps into the game: glTF forward axis (+Z or -Z) becomes Unity +X,
        /// the model is scaled to a display length, the pivot goes to the NOSE (max X = 0), the
        /// lowest point (wheels) to y = 0 and the centre line to z = 0.
        /// </summary>
        public sealed class Spec
        {
            public string name, source; public float forwardSign = 1, length = 1;
            public float stretchX = 1;                       // extra scale along the lane (track tiles)
            public Func<Primitive, string> part = p => "Body";
            public Func<Primitive, bool> include = p => true;
            public Dictionary<string, Color> recolor = new Dictionary<string, Color>();
            public HashSet<string> dropTexture = new HashSet<string>();
            public bool pivotAtNose = true;
            /// <summary>Applied in glTF space before everything else (used to stand a leaning model upright).</summary>
            public Matrix4x4 pre = Matrix4x4.identity;
            /// <summary>Optional full material override by source material name (colour, opaque/transparent, no texture).</summary>
            public Func<string, Override> overrideRule;
            public int meshLods = 6;
        }

        public sealed class Override { public Color color; public bool transparent; public float smoothness = .35f, metallic; public bool keepTexture; }

        public sealed class Result
        {
            public GameObject root; public Bounds bounds; public Dictionary<string, Transform> parts = new Dictionary<string, Transform>();
            public long triangles; public Func<Vector3, Vector3> map;
        }

        /// <summary>glTF (right-handed) to Unity (left-handed) with forward -> +X. det = -1, so winding is reversed.</summary>
        static Vector3 Axes(Vector3 g, float forwardSign) => new Vector3(forwardSign * g.z, g.y, forwardSign * g.x);

        public static Result Build(Spec spec, string folder, Material fallback)
        {
            var doc = Load(spec.source);
            Directory.CreateDirectory(folder); Directory.CreateDirectory(folder + "/Textures");
            var prims = doc.primitives.Where(spec.include).ToList();
            foreach (var p in prims) p.world = spec.pre * p.world;

            // Bounds in mapped axes (before scale) to compute scale and pivot.
            var min = Vector3.positiveInfinity; var max = Vector3.negativeInfinity;
            foreach (var p in prims) foreach (var v in p.positions) { var w = Axes(p.world.MultiplyPoint3x4(v), spec.forwardSign); min = Vector3.Min(min, w); max = Vector3.Max(max, w); }
            float scale = spec.length / Mathf.Max(1e-4f, max.x - min.x);
            var pivot = new Vector3(spec.pivotAtNose ? max.x : min.x, min.y, (min.z + max.z) * .5f);
            Vector3 Map(Vector3 gltfWorld) { var w = Axes(gltfWorld, spec.forwardSign) - pivot; return new Vector3(w.x * scale * spec.stretchX, w.y * scale, w.z * scale); }

            var materials = BuildMaterials(doc, spec, folder, fallback);
            var result = new Result { map = g => Map(g) };
            var root = new GameObject(spec.name + " model"); result.root = root;
            var b = new Bounds(); bool first = true;
            foreach (var group in prims.GroupBy(spec.part))
            {
                var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>();
                var subs = new List<List<int>>(); var subMats = new List<Material>();
                var byMat = group.GroupBy(p => p.materialIndex).ToList();
                foreach (var mg in byMat)
                {
                    var tris = new List<int>();
                    foreach (var p in mg)
                    {
                        int baseIndex = verts.Count;
                        var nm = p.world.inverse.transpose;
                        for (int i = 0; i < p.positions.Length; i++)
                        {
                            verts.Add(Map(p.world.MultiplyPoint3x4(p.positions[i])));
                            var n = p.normals != null ? Axes(nm.MultiplyVector(p.normals[i]), spec.forwardSign) : Vector3.up;
                            if (spec.stretchX != 1) n = new Vector3(n.x / spec.stretchX, n.y, n.z);
                            norms.Add(n.normalized);
                            uvs.Add(p.uvs != null ? new Vector2(p.uvs[i].x, 1 - p.uvs[i].y) : Vector2.zero);
                        }
                        for (int i = 0; i + 2 < p.indices.Length; i += 3)
                        { tris.Add(baseIndex + p.indices[i]); tris.Add(baseIndex + p.indices[i + 2]); tris.Add(baseIndex + p.indices[i + 1]); }
                    }
                    subs.Add(tris); subMats.Add(mg.Key >= 0 ? materials[mg.Key] : fallback);
                }
                // Each part is pivoted at its own bounds centre so wheels can spin in place.
                var pb = new Bounds(verts[0], Vector3.zero); foreach (var v in verts) pb.Encapsulate(v);
                bool pivotCentre = group.Key != "Body";
                var offset = pivotCentre ? pb.center : Vector3.zero;
                for (int i = 0; i < verts.Count; i++) verts[i] -= offset;
                var mesh = new Mesh { name = spec.name + " " + group.Key, indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs);
                mesh.subMeshCount = subs.Count; for (int i = 0; i < subs.Count; i++) mesh.SetTriangles(subs[i], i, false);
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                if (spec.meshLods > 0) TryGenerateLods(mesh, spec.meshLods);
                string meshPath = folder + "/" + Safe(spec.name + "_" + group.Key) + ".asset";
                AssetDatabase.DeleteAsset(meshPath); AssetDatabase.CreateAsset(mesh, meshPath);
                var go = new GameObject(group.Key); go.transform.SetParent(root.transform, false); go.transform.localPosition = offset;
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = subMats.ToArray();
                result.parts[group.Key] = go.transform;
                result.triangles += subs.Sum(s => s.Count) / 3;
                var wb = new Bounds(pb.center, pb.size); if (first) { b = wb; first = false; } else b.Encapsulate(wb);
            }
            result.bounds = b;
            return result;
        }

        /// <summary>Unity 6.2+ automatic Mesh LOD (reduces far-away cost; the vehicles are 30-60 m from the deck).</summary>
        static void TryGenerateLods(Mesh mesh, int limit)
        {
            try { MeshLodUtility.GenerateMeshLods(mesh, limit); }
            catch (Exception e) { Debug.LogWarning("[STGF] Mesh LOD generation skipped for " + mesh.name + ": " + e.Message); }
        }

        static string Safe(string s) { foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s.Replace(' ', '_').Replace('[', '_').Replace(']', '_'); }

        static Material[] BuildMaterials(Document doc, Spec spec, string folder, Material fallback)
        {
            var mats = new Material[doc.materials.Count];
            var texCache = new Dictionary<string, Texture2D>();
            Texture2D Tex(int textureIndex, bool normal)
            {
                if (textureIndex < 0 || textureIndex >= doc.textures.Count) return null;
                int img = (int)D(Get(doc.textures[textureIndex], "source"), -1); if (img < 0) return null;
                string key = img + (normal ? "n" : "");
                if (texCache.TryGetValue(key, out var cached)) return cached;
                var bytes = doc.ImageBytes(img, out string ext);
                string path = folder + "/Textures/" + Safe(spec.name + "_image" + img + (normal ? "_normal" : "")) + "." + ext;
                File.WriteAllBytes(path, bytes); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer)
                {
                    importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    importer.sRGBTexture = !normal; importer.maxTextureSize = 2048; importer.mipmapEnabled = true;
                    importer.textureCompression = TextureImporterCompression.Compressed; importer.SaveAndReimport();
                }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path); texCache[key] = tex; return tex;
            }
            bool linear = PlayerSettings.colorSpace == ColorSpace.Linear;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            for (int i = 0; i < doc.materials.Count; i++)
            {
                var m = doc.materials[i]; string name = Get(m, "name") as string ?? "m" + i;
                var pbr = O(Get(m, "pbrMetallicRoughness"));
                var f = L(Get(pbr, "baseColorFactor"));
                var c = f.Count == 4 ? new Color((float)D(f[0]), (float)D(f[1]), (float)D(f[2]), (float)D(f[3])) : Color.white;
                if (linear) { float a = c.a; c = c.gamma; c.a = a; }   // glTF factors are linear; SetColor expects gamma
                string path = folder + "/" + Safe(spec.name + "_" + name) + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, path); }
                mat.shader = lit;
                bool recolored = spec.recolor.TryGetValue(name, out var rc);
                if (recolored) c = new Color(rc.r, rc.g, rc.b, c.a);
                Texture2D baseTex = null;
                if (!spec.dropTexture.Contains(name) && Get(pbr, "baseColorTexture") is Dictionary<string, object> bt) baseTex = Tex((int)D(bt["index"]), false);
                mat.SetTexture("_BaseMap", baseTex);
                mat.SetColor("_BaseColor", c);
                mat.SetFloat("_Metallic", Mathf.Clamp01((float)D(Get(pbr, "metallicFactor"), 1)) * .6f);
                mat.SetFloat("_Smoothness", Mathf.Clamp01(1 - (float)D(Get(pbr, "roughnessFactor"), 1)) * .85f);
                if (Get(m, "normalTexture") is Dictionary<string, object> nt) { var n = Tex((int)D(nt["index"]), true); if (n) { mat.SetTexture("_BumpMap", n); mat.EnableKeyword("_NORMALMAP"); } }
                if (Get(m, "emissiveTexture") is Dictionary<string, object> et || L(Get(m, "emissiveFactor")).Any(x => D(x) > 0))
                {
                    var ef = L(Get(m, "emissiveFactor")); var e = ef.Count == 3 ? new Color((float)D(ef[0]), (float)D(ef[1]), (float)D(ef[2])) : Color.black;
                    if (e.maxColorComponent > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", e); mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
                }
                string mode = Get(m, "alphaMode") as string ?? "OPAQUE";
                bool transmission = Get(O(Get(m, "extensions")), "KHR_materials_transmission") != null;
                if (mode == "BLEND" || transmission || c.a < .99f) MakeTransparent(mat, transmission ? Mathf.Min(c.a, .35f) : c.a);
                else if (mode == "MASK") { mat.SetFloat("_AlphaClip", 1); mat.SetFloat("_Cutoff", (float)D(Get(m, "alphaCutoff"), .5)); mat.EnableKeyword("_ALPHATEST_ON"); }
                else MakeOpaque(mat);
                if (Get(m, "doubleSided") is bool ds && ds) mat.SetFloat("_Cull", 0);
                var ov = spec.overrideRule?.Invoke(name);
                if (ov != null)
                {
                    if (!ov.keepTexture) { mat.SetTexture("_BaseMap", null); mat.SetTexture("_BumpMap", null); mat.DisableKeyword("_NORMALMAP"); }
                    mat.SetColor("_BaseColor", ov.color); mat.SetFloat("_Smoothness", ov.smoothness); mat.SetFloat("_Metallic", ov.metallic);
                    mat.SetFloat("_AlphaClip", 0); mat.DisableKeyword("_ALPHATEST_ON");
                    if (ov.transparent) MakeTransparent(mat, ov.color.a); else MakeOpaque(mat);
                }
                EditorUtility.SetDirty(mat); mats[i] = mat;
            }
            return mats;
        }

        public static void MakeTransparent(Material mat, float alpha)
        {
            var c = mat.GetColor("_BaseColor"); c.a = alpha; mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0); mat.SetFloat("_ZWrite", 0);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", 1); mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            try { BaseShaderGUI.SetMaterialKeywords(mat); } catch (Exception) { }
            mat.SetShaderPassEnabled("DepthOnly", false); mat.SetShaderPassEnabled("DepthNormals", false); mat.SetShaderPassEnabled("ShadowCaster", false);
        }
        public static void MakeOpaque(Material mat)
        {
            mat.SetFloat("_Surface", 0); mat.SetFloat("_ZWrite", 1); mat.SetFloat("_SrcBlend", 1); mat.SetFloat("_DstBlend", 0);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = -1; mat.SetOverrideTag("RenderType", "Opaque");
            mat.SetShaderPassEnabled("DepthOnly", true); mat.SetShaderPassEnabled("DepthNormals", true); mat.SetShaderPassEnabled("ShadowCaster", true);
            try { BaseShaderGUI.SetMaterialKeywords(mat); } catch (Exception) { }
        }

        /// <summary>Report of material names and triangle counts per material (helps choose recolours).</summary>
        public static string Describe(string source)
        {
            var doc = Load(source); var sb = new StringBuilder();
            foreach (var g in doc.primitives.GroupBy(p => p.material).OrderByDescending(g => g.Sum(p => p.indices.Length)))
                sb.AppendLine($"  {g.Key}: {g.Sum(p => p.indices.Length) / 3} tris, nodes {string.Join(",", g.Select(p => p.node).Distinct().Take(4))}");
            return sb.ToString();
        }
    }
}