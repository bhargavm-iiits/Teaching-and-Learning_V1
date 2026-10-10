using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SpeedTimeGraphFree
{
    /// <summary>Bright low-poly world: faceted hills, sea and river, snowy mountains, lighthouse, far city, trees, rocks, clouds, warm sun.</summary>
    public static partial class SpeedTimeFreeBuilder
    {
        static Material grass, grassDark, sand, sea, river, rock, snow, mountain, cityA, cityB, white, redPaint, cloudMat;
        static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();

        static void WorldMaterials()
        {
            grass = SpeedTimeFreeMaterials.Lit("World grass", new Color(.42f, .74f, .22f), .05f);
            grassDark = SpeedTimeFreeMaterials.Lit("World grass dark", new Color(.33f, .62f, .17f), .05f);
            sand = SpeedTimeFreeMaterials.Lit("World sand", new Color(.93f, .84f, .6f), .05f);
            sea = SpeedTimeFreeMaterials.Lit("World sea", new Color(.11f, .55f, .9f), .85f);
            river = SpeedTimeFreeMaterials.Lit("World river", new Color(.18f, .66f, .95f), .9f);
            rock = SpeedTimeFreeMaterials.Lit("World rock", new Color(.55f, .57f, .6f), .1f);
            snow = SpeedTimeFreeMaterials.Lit("World snow", new Color(.97f, .98f, 1f), .2f);
            mountain = SpeedTimeFreeMaterials.Lit("World mountain blue", new Color(.42f, .52f, .68f), .05f);
            cityA = SpeedTimeFreeMaterials.Lit("World city pale", new Color(.82f, .88f, .95f), .3f);
            cityB = SpeedTimeFreeMaterials.Lit("World city blue", new Color(.6f, .72f, .86f), .3f);
            white = SpeedTimeFreeMaterials.Lit("World white", new Color(.96f, .96f, .96f), .2f);
            redPaint = SpeedTimeFreeMaterials.Lit("World red", new Color(.85f, .12f, .14f), .3f);
            cloudMat = SpeedTimeFreeMaterials.Unlit("World cloud", new Color(1f, 1f, 1f));
        }

        static void Environment()
        {
            WorldMaterials();
            var env = Group("Environment (static)", root).transform;
            // Sea everywhere below; land is a faceted heightfield above it.
            Box("Sea", env, new Vector3(150, -.6f, 150), new Vector3(1200, .2f, 1200), sea, false);
            MeshObject("Low-poly land", env, LandMesh(), grass, Vector3.zero);
            Box("Beach", env, new Vector3(69.5f, -.08f, 20), new Vector3(5, .2f, 110), sand, false);
            Box("River", env, new Vector3(46, -.22f, 4.35f), new Vector3(54, .2f, 2.3f), river, false);
            Box("River bank near", env, new Vector3(46, -.15f, 3.05f), new Vector3(54, .3f, .4f), sand, false);
            Box("River bank far", env, new Vector3(46, -.15f, 5.65f), new Vector3(54, .3f, .4f), sand, false);
            Mountains(env); Lighthouse(env); City(env); Vegetation(env); Clouds(env);
            foreach (var t in env.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        // ------------------------------------------------------------------ terrain
        static float LaneMask(float x, float z)
        {
            // 0 on and near the lanes/deck corridor, 1 away from them.
            float m = 1;
            if (x > -30 && x < 75)
            {
                foreach (float lz in new[] { 0f, 8f, 16f }) m = Mathf.Min(m, Mathf.Clamp01((Mathf.Abs(z - lz) - 4.5f) / 6f));
                if (z > -6 && z < 22) m = Mathf.Min(m, z > 2.5f && z < 6 ? 0 : m);
            }
            if (x > 64) m = Mathf.Min(m, Mathf.Clamp01((90 - x) / 20f) * (z > 110 ? 1 : 0));   // sea on the right
            return m;
        }
        static float Height(float x, float z)
        {
            float h = 0;
            h += 7 * Mathf.PerlinNoise(x * .018f + 3.1f, z * .018f + 7.3f);
            h += 3 * Mathf.PerlinNoise(x * .06f + 11f, z * .06f + 2f);
            if (z > 24) h += Mathf.Clamp((z - 24) * .22f, 0, 16);              // hills rise behind the car lane
            if (x < -26) h += Mathf.Clamp((-26 - x) * .25f, 0, 14);           // and on the left
            if (z < -8) h += Mathf.Clamp((-8 - z) * .12f, 0, 3) * Mathf.PerlinNoise(x * .1f, z * .1f) * 2; // foreground knolls
            float mask = LaneMask(x, z);
            if (x > 64 && z < 110) return -1.2f;                               // sea bed
            if (x > -32 && x < 70 && z > -4 && z < 22)
                return z < 3.4f ? 0 : z < 10.4f ? (x > 18 && z < 5.75f ? -.45f : Mathf.Min(LaneY[1], (z - 3.4f) / 2.3f * LaneY[1]) - .05f) : z < 12.3f ? LaneY[1] - .05f : LaneY[0] - .05f;
            if (z >= 22 && x > -32 && x < 70) h += LaneY[0];
            if (x > 18 && x < 76 && z > 2.9f && z < 5.8f) return -.45f;         // river bed between bike and train lanes
            return h * mask * mask - (1 - mask) * .02f;
        }
        static Mesh LandMesh()
        {
            const float x0 = -160, x1 = 95, z0 = -90, z1 = 150, cell = 2f;
            int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
            var verts = new List<Vector3>(); var tris = new List<int>();
            var rnd = new System.Random(17);
            float J() => (float)(rnd.NextDouble() - .5) * cell * .35f;
            var grid = new Vector3[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++) for (int j = 0; j <= nz; j++)
                {
                    float bx = x0 + i * cell, bz = z0 + j * cell; bool corridor = bx > -34 && bx < 72 && bz > -6 && bz < 24;
                    float x = bx + (i > 0 && i < nx && !corridor ? J() : 0), z = bz + (j > 0 && j < nz && !corridor ? J() : 0);
                    grid[i, j] = new Vector3(x, Height(x, z), z);
                }
            for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++)
                {
                    var a = grid[i, j]; var b = grid[i + 1, j]; var c = grid[i + 1, j + 1]; var d = grid[i, j + 1];
                    if (a.y < -.9f && b.y < -.9f && c.y < -.9f && d.y < -.9f) continue;
                    void T(Vector3 p, Vector3 q, Vector3 r) { int n = verts.Count; verts.Add(p); verts.Add(q); verts.Add(r); tris.Add(n); tris.Add(n + 1); tris.Add(n + 2); }
                    if ((i + j) % 2 == 0) { T(a, d, c); T(a, c, b); } else { T(a, d, b); T(b, d, c); }
                }
            var mesh = new Mesh { name = "STGF low-poly land", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return SaveMesh(mesh, "LowPolyLand");
        }

        static Mesh FacetedCone(string name, int sides, float jitter, int seed)
        {
            var rnd = new System.Random(seed); var verts = new List<Vector3>(); var tris = new List<int>();
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++) { float a = i * Mathf.PI * 2 / sides; float r = 1 + (float)(rnd.NextDouble() - .5) * jitter; ring[i] = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); }
            var mid = new Vector3[sides];
            for (int i = 0; i < sides; i++) mid[i] = ring[i] * .55f + new Vector3(0, .5f + (float)(rnd.NextDouble() - .5) * .12f, 0);
            var top = new Vector3((float)(rnd.NextDouble() - .5) * .15f, 1, (float)(rnd.NextDouble() - .5) * .15f);
            void T(Vector3 a, Vector3 b, Vector3 c) { int n = verts.Count; verts.Add(a); verts.Add(b); verts.Add(c); tris.Add(n); tris.Add(n + 1); tris.Add(n + 2); }
            for (int i = 0; i < sides; i++)
            {
                int k = (i + 1) % sides;
                T(ring[i], mid[i], ring[k]); T(ring[k], mid[i], mid[k]); T(mid[i], top, mid[k]);
            }
            var m = new Mesh { name = name }; m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        static void Mountains(Transform env)
        {
            var parent = Group("Snowy mountains", env).transform;
            var rnd = new System.Random(5);
            for (int i = 0; i < 14; i++)
            {
                float x = -260 + i * 42 + (float)rnd.NextDouble() * 20, z = 175 + (float)rnd.NextDouble() * 110;
                float h = 55 + (float)rnd.NextDouble() * 60, w = h * (1.1f + (float)rnd.NextDouble() * .5f);
                var body = FacetedCone("Mountain body " + (i % 4), 9, .35f, 100 + i % 4);
                var cap = FacetedCone("Mountain cap " + (i % 4), 9, .35f, 100 + i % 4);
                var m = MeshObject("Mountain", parent, body, mountain, new Vector3(x, -2, z), false); m.transform.localScale = new Vector3(w, h, w * .8f);
                m.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360, 0);
                var c = MeshObject("Snow cap", m.transform, cap, snow, new Vector3(0, .62f, 0), false); c.transform.localScale = new Vector3(.4f, .385f, .4f); c.transform.localPosition = new Vector3(0, .615f, 0);
            }
        }

        static void Lighthouse(Transform env)
        {
            var lh = Group("Lighthouse", env).transform; lh.position = new Vector3(86, 0, 74);
            MeshObject("Headland rocks", lh, FacetedCone("Headland", 8, .5f, 41), rock, new Vector3(0, -1, 0)).transform.localScale = new Vector3(12, 5, 10);
            for (int i = 0; i < 6; i++) Prim(PrimitiveType.Cylinder, "Tower band", lh, new Vector3(0, 4.5f + i * 2.1f, 0), new Vector3(3.2f - i * .18f, 1.05f, 3.2f - i * .18f), i % 2 == 0 ? white : redPaint);
            Prim(PrimitiveType.Cylinder, "Lantern", lh, new Vector3(0, 17.8f, 0), new Vector3(2.2f, 1.1f, 2.2f), SpeedTimeFreeMaterials.Unlit("Lighthouse lamp", new Color(1f, .95f, .7f)));
            MeshObject("Roof", lh, FacetedCone("Lighthouse roof", 8, 0, 3), redPaint, new Vector3(0, 18.9f, 0)).transform.localScale = new Vector3(1.6f, 1.8f, 1.6f);
        }

        static void City(Transform env)
        {
            var city = Group("Far city by the coast", env).transform; var rnd = new System.Random(9);
            Box("City shore", city, new Vector3(150, -.3f, 190), new Vector3(170, .8f, 70), sand, false);
            for (int i = 0; i < 46; i++)
            {
                float x = 75 + (float)rnd.NextDouble() * 150, z = 168 + (float)rnd.NextDouble() * 45, h = 6 + (float)rnd.NextDouble() * (i % 7 == 0 ? 34 : 16);
                Box("Tower", city, new Vector3(x, h * .5f, z), new Vector3(4 + (float)rnd.NextDouble() * 4, h, 4 + (float)rnd.NextDouble() * 4), i % 3 == 0 ? cityB : cityA, false);
            }
        }

        static GameObject Asset(string path, string name, Transform parent, Vector3 at, float scale, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (!prefab) return null;
            var g = (GameObject)Object.Instantiate(prefab, parent); g.name = name;
            g.transform.position = at; g.transform.rotation = Quaternion.Euler(0, yaw, 0); g.transform.localScale = Vector3.one * scale;
            ConvertMaterials(g); return g;
        }
        /// <summary>Asset-pack materials are replaced by URP/Lit copies in our folder (no pink, no Built-in Standard).</summary>
        static void ConvertMaterials(GameObject g)
        {
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i]; if (!src) { mats[i] = grass; continue; }
                    if (!converted.TryGetValue(src, out var dst))
                    {
                        Texture tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : null;
                        if (!tex && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
                        if (!tex && src.HasProperty("_Albedo_Map")) tex = src.GetTexture("_Albedo_Map");
                        if (!tex && src.HasProperty("_Albedo")) tex = src.GetTexture("_Albedo");
                        if (!tex) foreach (var n in src.GetTexturePropertyNames()) { var lower = n.ToLowerInvariant(); if ((lower.Contains("albedo") || lower.Contains("base") || lower.Contains("main") || lower.Contains("diffuse")) && src.GetTexture(n)) { tex = src.GetTexture(n); break; } }
                        Color col = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
                        dst = tex ? SpeedTimeFreeMaterials.LitTextured("Pack " + src.name, tex, col, .05f) : SpeedTimeFreeMaterials.Lit("Pack " + src.name, col, .05f);
                        converted[src] = dst;
                    }
                    mats[i] = dst;
                }
                r.sharedMaterials = mats;
            }
        }

        static bool Clear(float x, float z)
        {
            if (x > -22 && x < 72 && z > -16 && z < 21.5f) return false;           // lanes, margins and the view over the bike lane
            if (Vector2.Distance(new Vector2(x, z), new Vector2(Eye.x, Eye.z)) < 16) return false;   // around the viewing deck
            if (x > 64 && z < 110) return false;                                    // sea
            // keep the eye-to-START and eye-to-STOP sight lines free
            Vector2 e = new Vector2(Eye.x, Eye.z);
            foreach (var target in new[] { new Vector2(0, 0), new Vector2(0, 8), new Vector2(0, 16), new Vector2(40, 0), new Vector2(40, 16), new Vector2(-1, 19.9f), new Vector2(-1, 11), new Vector2(-1, 3.4f) })
            {
                var p = new Vector2(x, z); var ab = target - e; float t = Mathf.Clamp01(Vector2.Dot(p - e, ab) / ab.sqrMagnitude);
                if (Vector2.Distance(p, e + ab * t) < 6) return false;
            }
            return true;
        }

        static void Vegetation(Transform env)
        {
            var veg = Group("Trees and rocks", env).transform; var rnd = new System.Random(23);
            string synty = "Assets/Synty/PolygonGeneric/Prefabs/Environment/";
            string[] round = { synty + "SM_Gen_Env_Tree_01.prefab", synty + "SM_Gen_Env_Tree_02.prefab", synty + "SM_Gen_Env_Tree_03.prefab", "Assets/SimpleNaturePack/Prefabs/Tree_01.prefab", "Assets/SimpleNaturePack/Prefabs/Tree_02.prefab" };
            string[] pines = { synty + "SM_Gen_Env_Tree_Pine_01.prefab", synty + "SM_Gen_Env_Tree_Pine_02.prefab", synty + "SM_Gen_Env_Tree_Pine_03.prefab" };
            string[] rocks = { synty + "SM_Gen_Env_Rock_01.prefab", synty + "SM_Gen_Env_Rock_03.prefab", synty + "SM_Gen_Env_Rock_05.prefab", synty + "SM_Gen_Env_Rock_07.prefab" };
            int placed = 0;
            for (int i = 0; i < 900 && placed < 190; i++)
            {
                float x = -110 + (float)rnd.NextDouble() * 190, z = -60 + (float)rnd.NextDouble() * 170;
                if (!Clear(x, z)) continue;
                float y = Height(x, z); if (y < -.3f) continue;
                bool pine = rnd.NextDouble() < .45; var list = pine ? pines : round;
                var tree = Asset(list[rnd.Next(list.Length)], pine ? "Pine tree" : "Round tree", veg, new Vector3(x, y - .1f, z), .9f + (float)rnd.NextDouble() * .7f, (float)rnd.NextDouble() * 360);
                if (tree) placed++;
            }
            // Grey rocks along the lane margins and between lanes, like the reference.
            for (int i = 0; i < 70; i++)
            {
                float x = -16 + (float)rnd.NextDouble() * 82;
                float z = rnd.NextDouble() < .5 ? 11.2f + (float)rnd.NextDouble() * .8f : rnd.NextDouble() < .5 ? 21.5f + (float)rnd.NextDouble() * 5 : -4.6f - (float)rnd.NextDouble() * 6;
                if (x > 18 && z > 2 && z < 7) continue;
                if (Mathf.Abs(x - 0) < 3 || Mathf.Abs(x - 32) < 2.5f) continue;        // keep START/STOP areas clean
                Asset(rocks[rnd.Next(rocks.Length)], "Rock", veg, new Vector3(x, Height(x, z) - .2f, z), .35f + (float)rnd.NextDouble() * .5f, (float)rnd.NextDouble() * 360);
            }
            // Foreground: low trees, bushes and rocks below the bike lane (kept under the sight lines, away from the deck).
            for (int i = 0, n = 0; i < 400 && n < 45; i++)
            {
                float x = -12 + (float)rnd.NextDouble() * 84, z = -30 + (float)rnd.NextDouble() * 24;
                if (z > -10 || !Clear(x, z)) continue;
                bool pine = rnd.NextDouble() < .4;
                float s = (.45f + (float)rnd.NextDouble() * .35f);
                if (s < .35f) { Asset(synty + "SM_Gen_Env_Bush_0" + (1 + rnd.Next(4)) + ".prefab", "Foreground bush", veg, new Vector3(x, Height(x, z), z), .8f, (float)rnd.NextDouble() * 360); continue; }
                Asset((pine ? pines : round)[rnd.Next(pine ? pines.Length : round.Length)], "Foreground tree", veg, new Vector3(x, Height(x, z) - .1f, z), s, (float)rnd.NextDouble() * 360); n++;
            }
            for (int i = 0; i < 30; i++)
            {
                float x = -12 + (float)rnd.NextDouble() * 84, z = -6 - (float)rnd.NextDouble() * 22;
                if (!Clear(x, z)) continue;
                Asset(rocks[rnd.Next(rocks.Length)], "Foreground rock", veg, new Vector3(x, Height(x, z) - .2f, z), .5f + (float)rnd.NextDouble() * .7f, (float)rnd.NextDouble() * 360);
            }
            // Bushes between the train and car lanes.
            for (int i = 0; i < 40; i++)
            {
                float x = -14 + (float)rnd.NextDouble() * 76, z = 10.6f + (float)rnd.NextDouble() * 1.6f;
                if (Mathf.Abs(x - 32) < 3 || Mathf.Abs(x) < 3) continue;
                Asset(synty + "SM_Gen_Env_Bush_0" + (1 + rnd.Next(4)) + ".prefab", "Bush", veg, new Vector3(x, 0, z), .6f + (float)rnd.NextDouble() * .4f, (float)rnd.NextDouble() * 360);
            }
        }

        static void Clouds(Transform env)
        {
            var sky = Group("Soft clouds", env).transform; var rnd = new System.Random(31);
            string synty = "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cloud_0";
            for (int i = 0; i < 16; i++)
            {
                var c = Asset(synty + (1 + i % 3) + ".prefab", "Cloud", sky, new Vector3(-200 + i * 30 + (float)rnd.NextDouble() * 20, 70 + (float)rnd.NextDouble() * 40, 160 + (float)rnd.NextDouble() * 160), 2 + (float)rnd.NextDouble() * 2, (float)rnd.NextDouble() * 360);
                if (c) foreach (var r in c.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = cloudMat; r.shadowCastingMode = ShadowCastingMode.Off; }
            }
        }

        /// <summary>Warm sun with soft shadows, bright sky, gentle haze (scene render settings only, no project settings).</summary>
        static class Lighting
        {
            public static void Apply()
            {
                var sunGo = new GameObject("Warm sun"); sunGo.transform.SetParent(root, false);
                var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f, .95f, .84f); sun.intensity = 1.35f;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = .65f; sunGo.transform.rotation = Quaternion.Euler(48, 32, 0);
                var skyMat = SpeedTimeFreeMaterialsSky();
                RenderSettings.skybox = skyMat; RenderSettings.sun = sun;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.72f, .82f, .95f); RenderSettings.ambientEquatorColor = new Color(.66f, .72f, .7f); RenderSettings.ambientGroundColor = new Color(.42f, .45f, .35f);
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = new Color(.68f, .82f, .95f);
                RenderSettings.fogStartDistance = 160; RenderSettings.fogEndDistance = 700;
            }
            static Material SpeedTimeFreeMaterialsSky()
            {
                string path = Folder + "/Materials/Sky bright.mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m) { m = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(m, path); }
                m.SetFloat("_SunDisk", 1); m.SetFloat("_SunSize", .03f); m.SetFloat("_AtmosphereThickness", .75f);
                m.SetColor("_SkyTint", new Color(.38f, .62f, .98f)); m.SetColor("_GroundColor", new Color(.5f, .62f, .5f)); m.SetFloat("_Exposure", 1.25f);
                EditorUtility.SetDirty(m); return m;
            }
        }
    }
}