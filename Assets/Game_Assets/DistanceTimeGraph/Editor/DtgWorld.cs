using System.Collections.Generic;
using DistanceTimeGraph.Logic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DistanceTimeGraph.EditorTools
{
    public static partial class DtgBuilder
    {
        const string Synty = "Assets/Synty/PolygonGeneric/Prefabs/Environment/";
        const string Hqp = "Assets/HQP STUDIOS/Low Poly Trees and Vegetation - Pack/Prefabs/";
        public const int Modules = 16;
        public const float RoadHalfWidth = 3f;

        // ---------------- lighting ----------------

        static Light BuildLighting(Transform root)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(root, false);
            go.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            var sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            RenderSettings.skybox = Mats.Sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.75f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.74f, 0.82f);
            RenderSettings.ambientGroundColor = new Color(0.4f, 0.45f, 0.5f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.78f, 0.9f, 1f);
            RenderSettings.fogDensity = 0.0035f;
            return sun;
        }

        // ---------------- the straight sky road ----------------

        static Transform BuildTrack(Transform root, out GameObject[] modules, out Transform returnArrows)
        {
            var track = new GameObject("Track (0 m, +Z)").transform;
            track.SetParent(root, false);
            var road = new GameObject("RoadModules").transform;
            road.SetParent(track, false);
            modules = new GameObject[Modules];
            for (int i = 0; i < Modules; i++)
            {
                var m = new GameObject($"Road_{i * 5:00}-{i * 5 + 5:00}m").transform;
                m.SetParent(road, false);
                m.localPosition = new Vector3(0f, 0f, i * 5f + 2.5f);
                Box(m, "Deck", new Vector3(0, -0.25f, 0), new Vector3(RoadHalfWidth * 2, 0.5f, 4.98f), Mats.Road);
                Box(m, "Underside", new Vector3(0, -0.62f, 0), new Vector3(RoadHalfWidth * 2 + 0.3f, 0.25f, 4.98f), Mats.Deck);
                Box(m, "EdgeL", new Vector3(-RoadHalfWidth, 0.06f, 0), new Vector3(0.16f, 0.12f, 5f), Mats.RoadEdge);
                Box(m, "EdgeR", new Vector3(RoadHalfWidth, 0.06f, 0), new Vector3(0.16f, 0.12f, 5f), Mats.RoadEdge);
                Box(m, "CentreDash", new Vector3(0, 0.012f, 0), new Vector3(0.14f, 0.02f, 2.2f), Mats.RoadLine);
                Box(m, "UnderGlow", new Vector3(0, -0.76f, 0), new Vector3(0.3f, 0.04f, 5f), Mats.Blue);
                foreach (float x in new[] { -RoadHalfWidth - 0.35f, RoadHalfWidth + 0.35f })
                {
                    Box(m, "Bollard", new Vector3(x, 0.28f, -2.5f), new Vector3(0.32f, 0.56f, 0.32f), Mats.Bollard);
                    Box(m, "BollardLight", new Vector3(x, 0.64f, -2.5f), new Vector3(0.26f, 0.2f, 0.26f), Mats.BollardLight);
                }
                modules[i] = m.gameObject;
            }

            // Start island with the learner's control platform, and the finish island at 80 m.
            var start = new GameObject("StartIsland").transform;
            start.SetParent(track, false);
            Box(start, "Slab", new Vector3(0, -0.5f, -8.5f), new Vector3(12f, 1f, 17f), Mats.Platform);
            Box(start, "SlabEdge", new Vector3(0, 0.02f, -0.1f), new Vector3(12f, 0.06f, 0.2f), Mats.Cyan);
            var control = new GameObject("ControlPlatform").transform;
            control.SetParent(start, false);
            control.localPosition = new Vector3(0, 0, PlayerFeet.z);
            Box(control, "Base", new Vector3(0, 0.75f, 0), new Vector3(3.4f, 1.5f, 3.4f), Mats.Stone);
            Box(control, "Top", new Vector3(0, 1.49f, 0), new Vector3(3.5f, 0.04f, 3.5f), Mats.Dark);
            Box(control, "TopTrim", new Vector3(0, 1.52f, 1.72f), new Vector3(3.5f, 0.04f, 0.08f), Mats.Cyan);
            Box(control, "RailL", new Vector3(-1.7f, 2.2f, -0.6f), new Vector3(0.08f, 0.08f, 2.2f), Mats.Rail);
            Box(control, "RailR", new Vector3(1.7f, 2.2f, -0.6f), new Vector3(0.08f, 0.08f, 2.2f), Mats.Rail);
            foreach (var p in new[] { new Vector3(-1.7f, 1.85f, 0.5f), new Vector3(1.7f, 1.85f, 0.5f), new Vector3(-1.7f, 1.85f, -1.7f), new Vector3(1.7f, 1.85f, -1.7f) })
                Box(control, "Post", p, new Vector3(0.14f, 0.7f, 0.14f), Mats.Rail);
            // Stone railing posts with warm lamps along both sides of the start island, framing the road.
            foreach (float x in new[] { -RoadHalfWidth - 0.6f, RoadHalfWidth + 0.6f })
            {
                for (float z = -9f; z <= -0.5f; z += 2.8f)
                {
                    Box(start, "RailPost", new Vector3(x, 0.55f, z), new Vector3(0.45f, 1.1f, 0.45f), Mats.Stone);
                    Box(start, "RailLamp", new Vector3(x, 1.25f, z), new Vector3(0.36f, 0.3f, 0.36f), Mats.BollardLight);
                    Box(start, "RailCap", new Vector3(x, 1.45f, z), new Vector3(0.5f, 0.1f, 0.5f), Mats.Stone);
                }
                Box(start, "RailBar", new Vector3(x, 0.85f, -4.8f), new Vector3(0.14f, 0.14f, 8.6f), Mats.Stone);
            }
            Scenery(Synty + "SM_Gen_Env_Stalactite_02.prefab", start, new Vector3(0, -9f, -8.5f), 18f, 20f);

            var finish = new GameObject("FinishIsland").transform;
            finish.SetParent(track, false);
            Box(finish, "Slab", new Vector3(0, -0.5f, 86f), new Vector3(14f, 1f, 12f), Mats.Platform);
            Box(finish, "SlabEdge", new Vector3(0, 0.02f, 80.1f), new Vector3(14f, 0.06f, 0.2f), Mats.Cyan);
            Scenery(Synty + "SM_Gen_Env_Stalactite_01.prefab", finish, new Vector3(0, -9f, 86f), 16f, 70f);

            // Backward chevrons along the road, shown while a return toward the start is planned or driven.
            returnArrows = new GameObject("ReturnArrows").transform;
            returnArrows.SetParent(track, false);
            for (int i = 0; i < 22; i++) Chevron(returnArrows, "Chevron_" + i, Vector3.zero, 1.2f, Mats.Cyan);
            returnArrows.gameObject.SetActive(false);
            return track;
        }

        /// A flat "^" on the road pointing back toward the start (-Z).
        public static Transform Chevron(Transform parent, string name, Vector3 localPos, float size, Material m)
        {
            var c = new GameObject(name).transform;
            c.SetParent(parent, false);
            c.localPosition = localPos;
            Box(c, "ArmL", new Vector3(-0.3f * size, 0, 0.18f * size), new Vector3(0.8f * size, 0.03f, 0.16f * size), m, Quaternion.Euler(0, 38f, 0));
            Box(c, "ArmR", new Vector3(0.3f * size, 0, 0.18f * size), new Vector3(0.8f * size, 0.03f, 0.16f * size), m, Quaternion.Euler(0, -38f, 0));
            return c;
        }

        // ---------------- floating islands, clouds, mountains ----------------

        static GameObject[] BuildEnvironment(Transform root)
        {
            var env = new GameObject("Environment").transform;
            env.SetParent(root, false);
            var rng = new System.Random(7);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            var mountains = new GameObject("Mountains").transform;
            mountains.SetParent(env, false);
            string[] mountainPrefabs = { "SM_Gen_Env_Background_Mountain_01", "SM_Gen_Env_Background_Mountain_02", "SM_Gen_Env_Background_Mountain_03", "SM_Gen_Env_Mountain_01", "SM_Gen_Env_Mountain_02", "SM_Gen_Env_Mountain_03" };
            for (int i = 0; i < 14; i++)
            {
                float ang = Mathf.Lerp(-80f, 80f, i / 13f) * Mathf.Deg2Rad;
                float dist = R(210f, 300f);
                var pos = new Vector3(Mathf.Sin(ang) * dist, -45f, 40f + Mathf.Cos(ang) * dist);
                Scenery(Synty + mountainPrefabs[i % mountainPrefabs.Length] + ".prefab", mountains, pos, R(140f, 220f), R(0, 360));
            }

            var clouds = new GameObject("CloudsBelow").transform;
            clouds.SetParent(env, false);
            string[] cloudPrefabs = { Synty + "SM_Gen_Env_Cloud_01.prefab", Synty + "SM_Gen_Env_Cloud_02.prefab", Synty + "SM_Gen_Env_Cloud_03.prefab", Hqp + "Terrains/Cloud/Cloud_01.prefab" };
            for (int i = 0; i < 46; i++)
            {
                var pos = new Vector3(R(-140f, 140f), R(-30f, -14f), R(-60f, 220f));
                Scenery(cloudPrefabs[i % cloudPrefabs.Length], clouds, pos, R(18f, 42f), R(0, 360));
            }
            for (int i = 0; i < 10; i++)   // a few soft clouds high in the sky
                Scenery(cloudPrefabs[i % 3], clouds, new Vector3(R(-160f, 160f), R(40f, 70f), R(60f, 300f)), R(25f, 45f), R(0, 360));

            var rocks = new GameObject("FloatingRocks").transform;
            rocks.SetParent(env, false);
            for (int i = 0; i < 18; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var pos = new Vector3(side * R(12f, 70f), R(5f, 26f), R(-10f, 150f));
                var rock = Scenery(Synty + $"SM_Gen_Env_Rock_{(i % 10) + 1:00}.prefab", rocks, pos, R(2.5f, 7f), R(0, 360));
                if (rock != null) rock.transform.rotation = Quaternion.Euler(R(-25, 25), R(0, 360), R(-25, 25));
            }

            // Decoration near the learner so the first view is framed by trees.
            var near = new GameObject("NearTrees").transform;
            near.SetParent(env, false);
            foreach (var p in new[] { new Vector3(-5.2f, 0, -3f), new Vector3(-5.4f, 0, -11f), new Vector3(5.3f, 0, -12f), new Vector3(5.2f, 0, -2.5f), new Vector3(-6f, 0, 84f), new Vector3(6f, 0, 90f) })
                Scenery(Synty + $"SM_Gen_Env_Tree_Pine_0{Mathf.Abs((int)p.z) % 3 + 1}.prefab", near, p, R(4.5f, 7f), R(0, 360), true);

            // Three island arrangements; each course shows one of them.
            var layouts = new GameObject[AdaptiveSelection.IslandLayouts];
            for (int l = 0; l < layouts.Length; l++)
            {
                var layout = new GameObject("IslandLayout_" + (char)('A' + l)).transform;
                layout.SetParent(env, false);
                layouts[l] = layout.gameObject;
                var lr = new System.Random(101 + l * 37);
                float LR(float a, float b) => a + (float)lr.NextDouble() * (b - a);
                for (int i = 0; i < 12; i++)
                {
                    float side = (i + l) % 2 == 0 ? -1f : 1f;
                    float z = Mathf.Lerp(-16f, 130f, i / 11f) + LR(-6f, 6f);
                    var pos = new Vector3(side * LR(12f, 36f), LR(-8f, 1.5f), z);
                    BuildIsland(layout, pos, LR(18f, 32f), lr, waterfall: i % 3 == l % 3);
                }
                layout.gameObject.SetActive(l == 0);
            }
            return layouts;
        }

        static void BuildIsland(Transform parent, Vector3 pos, float size, System.Random rng, bool waterfall)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var island = new GameObject("Island").transform;
            island.SetParent(parent, false);
            island.position = pos;
            var top = Scenery(Synty + $"SM_Gen_Env_Ground_Grass_Large_0{rng.Next(1, 4)}.prefab", island, pos, size, R(0, 360));
            if (top == null) return;
            var b = Bounds(top);
            float topY = b.max.y;
            Scenery(Synty + $"SM_Gen_Env_Stalactite_0{rng.Next(1, 5)}.prefab", island, new Vector3(pos.x, b.min.y - size * 0.3f, pos.z), size * 0.85f, R(0, 360));
            int trees = rng.Next(3, 7);
            for (int t = 0; t < trees; t++)
            {
                var p = new Vector3(pos.x + R(-0.3f, 0.3f) * b.size.x, topY - 0.3f, pos.z + R(-0.3f, 0.3f) * b.size.z);
                Scenery(Synty + $"SM_Gen_Env_Tree_Pine_0{rng.Next(1, 4)}.prefab", island, p, R(4.5f, 9f), R(0, 360), true);
            }
            if (waterfall)
            {
                // A waterfall spilling over the side that faces the road.
                float toward = pos.x > 0 ? -1f : 1f;
                var wp = new Vector3(pos.x + toward * b.extents.x * 0.7f, topY - 0.5f, pos.z);
                var fall = Scenery(Synty + "SM_Gen_Env_Waterfall_01.prefab", island, wp, size * 0.9f, toward > 0 ? 90f : -90f, true);
                if (fall != null)
                {
                    var fb = Bounds(fall);
                    fall.transform.position += Vector3.up * (topY - fb.max.y);
                }
            }
        }

        public static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // ---------------- distance signs, gates and flags ----------------

        static Renderer[] BuildMarkersAndFlags(Transform track)
        {
            var markers = new GameObject("DistanceMarkers").transform;
            markers.SetParent(track, false);
            var flags = new List<Renderer>();
            var pennant = PennantMesh();
            for (int d = 0; d <= 80; d += 10)
            {
                var sign = new GameObject($"Sign_{d}m").transform;
                sign.SetParent(markers, false);
                sign.localPosition = new Vector3(-RoadHalfWidth - 1.3f, 0, d);
                Box(sign, "Post", new Vector3(0, 1.1f, 0), new Vector3(0.14f, 2.2f, 0.14f), Mats.Rail);
                Box(sign, "Board", new Vector3(0, 2.45f, 0), new Vector3(1.7f, 0.75f, 0.1f), Mats.Blue);
                Box(sign, "BoardTrim", new Vector3(0, 2.05f, -0.01f), new Vector3(1.72f, 0.05f, 0.11f), Mats.Cyan);
                WorldText(sign, "Label", $"{d} m", 0.62f, Color.white, new Vector3(0, 2.45f, -0.07f));

                // Coloured flags on both sides; their colour follows the course theme.
                foreach (float x in new[] { RoadHalfWidth + 1.1f, -RoadHalfWidth - 2.4f })
                {
                    var pole = new GameObject("Flag").transform;
                    pole.SetParent(markers, false);
                    pole.localPosition = new Vector3(x, 0, d + (x > 0 ? 0f : 5f));
                    Box(pole, "Pole", new Vector3(0, 1.6f, 0), new Vector3(0.07f, 3.2f, 0.07f), Mats.White);
                    var cloth = new GameObject("Pennant", typeof(MeshFilter), typeof(MeshRenderer));
                    cloth.transform.SetParent(pole, false);
                    cloth.transform.localPosition = new Vector3(0, 3.1f, 0);
                    cloth.transform.localRotation = Quaternion.Euler(0, x > 0 ? 180f : 0f, 0);
                    cloth.GetComponent<MeshFilter>().sharedMesh = pennant;
                    var r = cloth.GetComponent<MeshRenderer>();
                    r.sharedMaterial = Mats.Flag;
                    flags.Add(r);
                }
            }
            var startText = WorldText(markers, "StartLine", "START", 1.2f, Color.white, new Vector3(0, 0.03f, -1.6f));
            startText.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Box(markers, "StartStripe", new Vector3(0, 0.015f, -0.15f), new Vector3(RoadHalfWidth * 2, 0.01f, 0.3f), Mats.White);
            Gate(markers, "FinishGate", 80f, "FINISH  80 m", Mats.Yellow);
            return flags.ToArray();
        }

        static void Gate(Transform parent, string name, float d, string label, Material accent)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            g.localPosition = new Vector3(0, 0, d);
            Box(g, "PostL", new Vector3(-RoadHalfWidth - 0.7f, 2.4f, 0), new Vector3(0.5f, 4.8f, 0.5f), Mats.Stone);
            Box(g, "PostR", new Vector3(RoadHalfWidth + 0.7f, 2.4f, 0), new Vector3(0.5f, 4.8f, 0.5f), Mats.Stone);
            Box(g, "Beam", new Vector3(0, 5f, 0), new Vector3(RoadHalfWidth * 2 + 2.2f, 0.7f, 0.55f), Mats.Stone);
            Box(g, "BeamGlow", new Vector3(0, 4.6f, -0.02f), new Vector3(RoadHalfWidth * 2 + 1.2f, 0.08f, 0.56f), accent);
            WorldText(g, "Label", label, 0.75f, Color.white, new Vector3(0, 5f, -0.3f));
        }

        static Mesh PennantMesh()
        {
            string path = GenFolder + "/DTG_Pennant.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "DTG_Pennant" };
            mesh.vertices = new[] { new Vector3(0, 0, 0), new Vector3(0, -0.7f, 0), new Vector3(1.1f, -0.35f, 0) };
            mesh.triangles = new[] { 0, 2, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ---------------- destinations ----------------

        static MarkerGlow[] BuildLandmarks(Transform track, out MarkerGlow beacon)
        {
            var root = new GameObject("Landmarks").transform;
            root.SetParent(track, false);
            var all = new MarkerGlow[System.Enum.GetValues(typeof(Landmark)).Length];
            foreach (Landmark kind in System.Enum.GetValues(typeof(Landmark)))
                all[(int)kind] = BuildLandmark(root, kind);

            var b = new GameObject("TurnaroundBeacon").transform;
            b.SetParent(root, false);
            var glow = new List<Renderer>();
            Box(b, "PostL", new Vector3(-RoadHalfWidth - 0.7f, 2.7f, 0), new Vector3(0.7f, 5.4f, 0.7f), Mats.Stone);
            Box(b, "PostR", new Vector3(RoadHalfWidth + 0.7f, 2.7f, 0), new Vector3(0.7f, 5.4f, 0.7f), Mats.Stone);
            Box(b, "Beam", new Vector3(0, 5.6f, 0), new Vector3(RoadHalfWidth * 2 + 2.4f, 0.9f, 0.8f), Mats.Stone);
            Box(b, "Board", new Vector3(0, 6.75f, 0), new Vector3(2.6f, 1.2f, 0.25f), Mats.Wood);
            glow.Add(Box(b, "BoardGlow", new Vector3(0, 6.75f, -0.14f), new Vector3(2.7f, 1.3f, 0.02f), Mats.Orange).GetComponent<Renderer>());
            glow.Add(Box(b, "Lamp", new Vector3(0, 5.1f, -0.42f), new Vector3(RoadHalfWidth * 2 + 0.6f, 0.12f, 0.05f), Mats.Yellow).GetComponent<Renderer>());
            glow.Add(Box(b, "StopLine", new Vector3(0, 0.02f, 0), new Vector3(RoadHalfWidth * 2, 0.02f, 0.35f), Mats.Yellow).GetComponent<Renderer>());
            Box(b, "FlagPole", new Vector3(0.9f, 8f, 0), new Vector3(0.08f, 1.8f, 0.08f), Mats.Rail);
            var bflag = new GameObject("Flag", typeof(MeshFilter), typeof(MeshRenderer));
            bflag.transform.SetParent(b, false);
            bflag.transform.localPosition = new Vector3(0.9f, 8.85f, 0);
            bflag.GetComponent<MeshFilter>().sharedMesh = PennantMesh();
            bflag.GetComponent<MeshRenderer>().sharedMaterial = Mats.Orange;
            beacon = Finish(b, glow, "60 m", 6.75f);
            beacon.label.transform.localPosition = new Vector3(0, 6.75f, -0.3f);
            b.gameObject.SetActive(false);
            return all;
        }

        static MarkerGlow BuildLandmark(Transform root, Landmark kind)
        {
            var t = new GameObject(kind.ToString()).transform;
            t.SetParent(root, false);
            var glow = new List<Renderer>();
            // Every destination has a glowing stop line across the road at its exact distance.
            glow.Add(Box(t, "StopLine", new Vector3(0, 0.02f, 0), new Vector3(RoadHalfWidth * 2, 0.02f, 0.3f), Mats.Glow).GetComponent<Renderer>());
            float side = RoadHalfWidth + 2.6f;
            switch (kind)
            {
                case Landmark.BlueGate:
                case Landmark.BridgePlatform:
                    Box(t, "PostL", new Vector3(-RoadHalfWidth - 0.6f, 2.6f, 0), new Vector3(0.6f, 5.2f, 0.6f), Mats.Stone);
                    Box(t, "PostR", new Vector3(RoadHalfWidth + 0.6f, 2.6f, 0), new Vector3(0.6f, 5.2f, 0.6f), Mats.Stone);
                    Box(t, "Beam", new Vector3(0, 5.4f, 0), new Vector3(RoadHalfWidth * 2 + 2f, 0.8f, 0.7f), Mats.Stone);
                    glow.Add(Box(t, "Ring", new Vector3(0, 4.9f, -0.05f), new Vector3(RoadHalfWidth * 2 + 0.8f, 0.14f, 0.72f), Mats.Glow).GetComponent<Renderer>());
                    glow.Add(Box(t, "PostGlowL", new Vector3(-RoadHalfWidth - 0.6f, 2.6f, -0.31f), new Vector3(0.16f, 4.6f, 0.02f), Mats.Glow).GetComponent<Renderer>());
                    glow.Add(Box(t, "PostGlowR", new Vector3(RoadHalfWidth + 0.6f, 2.6f, -0.31f), new Vector3(0.16f, 4.6f, 0.02f), Mats.Glow).GetComponent<Renderer>());
                    break;
                case Landmark.ForestStation:
                    for (int i = 0; i < 4; i++)
                        Box(t, "Leg", new Vector3(side + (i % 2 == 0 ? -1f : 1f), 1.6f, i < 2 ? -1f : 1f), new Vector3(0.18f, 3.2f, 0.18f), Mats.Wood);
                    Box(t, "Deck", new Vector3(side, 3.25f, 0), new Vector3(2.6f, 0.2f, 2.6f), Mats.Wood);
                    Box(t, "Roof", new Vector3(side, 4.6f, 0), new Vector3(3f, 0.25f, 3f), Mats.Green);
                    glow.Add(Box(t, "Window", new Vector3(side, 3.9f, -1.25f), new Vector3(2f, 0.6f, 0.05f), Mats.Glow).GetComponent<Renderer>());
                    Scenery(Synty + "SM_Gen_Env_Tree_Pine_01.prefab", t, t.position + new Vector3(side + 2.2f, 0, 1.5f), 6f, 30f, true);
                    Scenery(Synty + "SM_Gen_Env_Tree_Pine_03.prefab", t, t.position + new Vector3(side + 1.5f, 0, -2.2f), 5f, 80f, true);
                    break;
                case Landmark.WindTower:
                {
                    Prim(PrimitiveType.Cylinder, t, "Tower", new Vector3(side, 4f, 0), new Vector3(0.6f, 4f, 0.6f), Mats.White);
                    Box(t, "Nacelle", new Vector3(side, 8.1f, 0), new Vector3(0.7f, 0.7f, 1.4f), Mats.White);
                    var hub = new GameObject("Rotor").transform;
                    hub.SetParent(t, false);
                    hub.localPosition = new Vector3(side, 8.1f, -0.8f);
                    for (int i = 0; i < 3; i++)
                        Box(hub, "Blade", Quaternion.Euler(0, 0, i * 120f) * new Vector3(0, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.08f), Mats.White, Quaternion.Euler(0, 0, i * 120f));
                    var spin = hub.gameObject.AddComponent<Spinner>();
                    spin.axis = Vector3.forward;
                    spin.degreesPerSecond = 70f;
                    glow.Add(Box(t, "PowerBand", new Vector3(side, 6.5f, 0), new Vector3(0.7f, 0.25f, 0.7f), Mats.Glow).GetComponent<Renderer>());
                    break;
                }
                case Landmark.ParcelStation:
                    Box(t, "Platform", new Vector3(side, 0.3f, 0), new Vector3(3f, 0.6f, 3f), Mats.Platform);
                    Box(t, "Canopy", new Vector3(side, 3.2f, 0), new Vector3(3.2f, 0.2f, 3.2f), Mats.Orange);
                    Box(t, "PostA", new Vector3(side - 1.4f, 1.8f, -1.4f), new Vector3(0.15f, 2.8f, 0.15f), Mats.Rail);
                    Box(t, "PostB", new Vector3(side + 1.4f, 1.8f, -1.4f), new Vector3(0.15f, 2.8f, 0.15f), Mats.Rail);
                    Box(t, "ParcelA", new Vector3(side - 0.6f, 0.95f, 0.3f), new Vector3(0.7f, 0.7f, 0.7f), Mats.Wood);
                    Box(t, "ParcelB", new Vector3(side + 0.5f, 0.85f, -0.3f), new Vector3(0.5f, 0.5f, 0.5f), Mats.Orange);
                    Box(t, "ParcelC", new Vector3(side - 0.4f, 1.55f, 0.3f), new Vector3(0.45f, 0.45f, 0.45f), Mats.Yellow);
                    glow.Add(Box(t, "Sign", new Vector3(side, 3.55f, -1.5f), new Vector3(1.8f, 0.4f, 0.05f), Mats.Glow).GetComponent<Renderer>());
                    break;
                case Landmark.ChargingStation:
                case Landmark.EmergencyPad:
                {
                    bool emergency = kind == Landmark.EmergencyPad;
                    glow.Add(Prim(PrimitiveType.Cylinder, t, "Pad", new Vector3(0, 0.015f, -2.3f), new Vector3(4.2f, 0.012f, 4.2f), Mats.Glow).GetComponent<Renderer>());
                    Prim(PrimitiveType.Cylinder, t, "PadInner", new Vector3(0, 0.02f, -2.3f), new Vector3(3.4f, 0.012f, 3.4f), Mats.Dark);
                    // A glowing energy badge floating over the road above the pad, as in the reference.
                    var badge = new GameObject("EnergyBadge").transform;
                    badge.SetParent(t, false);
                    badge.localPosition = new Vector3(0, 2.3f, -2.3f);
                    glow.Add(Prim(PrimitiveType.Cylinder, badge, "Disc", Vector3.zero, new Vector3(1.3f, 0.06f, 1.3f), Mats.Glow, Quaternion.Euler(90, 0, 0)).GetComponent<Renderer>());
                    Prim(PrimitiveType.Cylinder, badge, "Rim", new Vector3(0, 0, 0.02f), new Vector3(1.45f, 0.04f, 1.45f), emergency ? Mats.Red : Mats.White, Quaternion.Euler(90, 0, 0));
                    Box(badge, "BoltTop", new Vector3(0.08f, 0.18f, -0.08f), new Vector3(0.16f, 0.45f, 0.04f), Mats.White, Quaternion.Euler(0, 0, -25));
                    Box(badge, "BoltBottom", new Vector3(-0.08f, -0.18f, -0.08f), new Vector3(0.16f, 0.45f, 0.04f), Mats.White, Quaternion.Euler(0, 0, -25));
                    Box(badge, "BoltMid", new Vector3(0, 0, -0.08f), new Vector3(0.36f, 0.12f, 0.04f), Mats.White);
                    glow.Add(Prim(PrimitiveType.Cylinder, t, "Halo", new Vector3(0, 0.03f, -2.3f), new Vector3(4.8f, 0.006f, 4.8f), Mats.Yellow).GetComponent<Renderer>());
                    Box(t, "Pillar", new Vector3(side - 0.6f, 1.2f, -2.3f), new Vector3(0.5f, 2.4f, 0.5f), emergency ? Mats.White : Mats.Rail);
                    if (emergency) Box(t, "Stripe", new Vector3(side - 0.6f, 1.6f, -2.56f), new Vector3(0.52f, 0.3f, 0.02f), Mats.Red);
                    break;
                }
                case Landmark.CrystalPlatform:
                {
                    glow.Add(Prim(PrimitiveType.Cylinder, t, "Pad", new Vector3(0, 0.015f, -2.3f), new Vector3(4.2f, 0.012f, 4.2f), Mats.Glow).GetComponent<Renderer>());
                    var cluster = new GameObject("Crystals").transform;
                    cluster.SetParent(t, false);
                    cluster.localPosition = new Vector3(side, 0, 0);
                    for (int i = 0; i < 5; i++)
                    {
                        var c = Box(cluster, "Crystal", new Vector3(Mathf.Sin(i * 1.3f) * 0.8f, 0.9f + i * 0.15f, Mathf.Cos(i * 1.3f) * 0.8f),
                            new Vector3(0.45f, 1.8f + i * 0.3f, 0.45f), Mats.Crystal, Quaternion.Euler(i * 7f - 14f, i * 40f, 45f + i * 5f));
                        glow.Add(c.GetComponent<Renderer>());
                    }
                    cluster.gameObject.AddComponent<Spinner>().axis = Vector3.up;
                    cluster.GetComponent<Spinner>().degreesPerSecond = 12f;
                    break;
                }
                case Landmark.RepairGarage:
                    Box(t, "Walls", new Vector3(side + 0.4f, 1.5f, 0), new Vector3(3.6f, 3f, 4f), Mats.Stone);
                    Box(t, "Roof", new Vector3(side + 0.4f, 3.2f, 0), new Vector3(3.9f, 0.3f, 4.3f), Mats.Orange);
                    Box(t, "Door", new Vector3(side - 1.41f, 1.2f, 0), new Vector3(0.05f, 2.2f, 2.6f), Mats.Dark);
                    glow.Add(Box(t, "DoorGlow", new Vector3(side - 1.44f, 2.4f, 0), new Vector3(0.04f, 0.15f, 2.7f), Mats.Glow).GetComponent<Renderer>());
                    glow.Add(Prim(PrimitiveType.Cylinder, t, "Pad", new Vector3(0, 0.015f, -2.3f), new Vector3(4.2f, 0.012f, 4.2f), Mats.Glow).GetComponent<Renderer>());
                    break;
            }
            var m = Finish(t, glow, "40 m", 7f);
            t.gameObject.SetActive(false);
            return m;
        }

        static MarkerGlow Finish(Transform t, List<Renderer> glow, string label, float labelHeight)
        {
            var m = t.gameObject.AddComponent<MarkerGlow>();
            m.glowRenderers = glow.ToArray();
            m.label = WorldText(t, "Label", label, 1.1f, Color.white, new Vector3(0, labelHeight, 0));
            m.burst = Burst(t, new Vector3(0, 2f, 0));
            return m;
        }

        static ParticleSystem Burst(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("ArrivalBurst", typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 1.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.gravityModifier = 0.6f;
            main.startColor = new ParticleSystem.MinMaxGradient(Palette.Yellow, Palette.Cyan);
            main.maxParticles = 80;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Yellow;
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        // ---------------- the timed bridge ----------------

        static TimedBridgeController BuildBridge(Transform track)
        {
            var root = new GameObject("TimedBridge").transform;
            root.SetParent(track, false);
            var ctl = root.gameObject.AddComponent<TimedBridgeController>();
            ctl.trackOrigin = track;
            ctl.tiles = new Transform[10];
            for (int i = 0; i < ctl.tiles.Length; i++)
                ctl.tiles[i] = Box(root, "Tile_" + i, new Vector3(0, -0.15f, 41f + i * 2f), new Vector3(6f, 0.3f, 1.88f), Mats.BridgeSafe).transform;
            ctl.openMaterial = Mats.BridgeOpen;
            ctl.closedMaterial = Mats.BridgeClosed;
            ctl.previewSafeMaterial = Mats.BridgeSafe;
            ctl.previewUnsafeMaterial = Mats.BridgeUnsafe;

            var pad = Prim(PrimitiveType.Cylinder, root, "SyncPad", new Vector3(0, 0.02f, 37.5f), new Vector3(4.6f, 0.01f, 4.6f), Mats.Dark).transform;
            Prim(PrimitiveType.Cylinder, pad, "Rim", new Vector3(0, 0.5f, 0), new Vector3(1.08f, 0.5f, 1.08f), Mats.Cyan).transform.SetAsFirstSibling();
            var fill = Prim(PrimitiveType.Cylinder, pad, "Fill", new Vector3(0, 1.5f, 0), new Vector3(1f, 1f, 1f), Mats.PadFill).transform;
            ctl.syncPad = pad;
            ctl.padFill = fill;
            var far = Prim(PrimitiveType.Cylinder, root, "FarPlatform", new Vector3(0, 0.02f, 62.5f), new Vector3(4.6f, 0.01f, 4.6f), Mats.Green).transform;
            ctl.farPlatform = far;

            var sign = new GameObject("BridgeSign").transform;
            sign.SetParent(root, false);
            sign.localPosition = new Vector3(RoadHalfWidth + 1.6f, 0, 40f);
            Box(sign, "Post", new Vector3(0, 1.5f, 0), new Vector3(0.18f, 3f, 0.18f), Mats.Rail);
            Box(sign, "Board", new Vector3(0, 3.6f, 0), new Vector3(4.6f, 1.6f, 0.12f), Mats.Dark);
            Box(sign, "Trim", new Vector3(0, 2.78f, -0.02f), new Vector3(4.62f, 0.06f, 0.14f), Mats.Green);
            ctl.signRoot = sign;
            ctl.sign = WorldText(sign, "Sign", "BRIDGE OPEN 8–12 s", 0.62f, Palette.Green, new Vector3(0, 3.95f, -0.08f));
            ctl.clock = WorldText(sign, "Clock", "SYNC PAD", 0.42f, Color.white, new Vector3(0, 3.3f, -0.08f));
            root.gameObject.SetActive(false);
            return ctl;
        }
    }
}
