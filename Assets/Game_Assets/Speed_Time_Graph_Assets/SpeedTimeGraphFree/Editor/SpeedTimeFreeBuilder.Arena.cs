using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// TripleVehicleArena: three parallel lanes along +X (k = 0.5 units per metre, START at x = 0 on every lane),
    /// distance boards every 10 m at the same x, STOP gantries, rock tunnel, track tiles, the three vehicles,
    /// ghosts, ribbons, the bike speed gate and slow zone, and the 125 m sand trap.
    /// </summary>
    public static partial class SpeedTimeFreeBuilder
    {
        public static readonly float[] LaneZ = { 16, 8, 0 };          // CAR, TRAIN, BIKE
        public static readonly float[] LaneWidth = { 6, 4, 5 };
        /// <summary>Terraced lane levels like the reference (car on the upper terrace) so the full-size train never hides the car.</summary>
        public static readonly float[] LaneY = { 5f, 1.2f, 0f };
        public const float LaneStart = -15, LaneEnd = 65, BallastTop = .25f, TrackHeight = .45f;
        static Material asphalt, lineWhite, rail, post, checkerBlack, checkerWhite, startGreen, stopRed, stripRed, stripGlow, navy, cyanGlow, gravel, sandTrap, barrierOrange, gateBlue, zoneYellow, good, bad, tunnelDark;

        static void ArenaMaterials()
        {
            asphalt = SpeedTimeFreeMaterials.Lit("Road asphalt", new Color(.23f, .25f, .29f), .1f);
            lineWhite = SpeedTimeFreeMaterials.Unlit("Road paint white", new Color(.95f, .95f, .95f));
            rail = SpeedTimeFreeMaterials.Lit("Guardrail steel", new Color(.78f, .8f, .83f), .55f, .6f);
            post = SpeedTimeFreeMaterials.Lit("Sign post dark", new Color(.2f, .21f, .24f), .3f, .3f);
            checkerBlack = SpeedTimeFreeMaterials.Lit("Checker black", new Color(.06f, .06f, .07f), .2f);
            checkerWhite = SpeedTimeFreeMaterials.Lit("Checker white", new Color(.97f, .97f, .97f), .2f);
            startGreen = SpeedTimeFreeMaterials.Lit("START green", new Color(.06f, .6f, .25f), .3f);
            stopRed = SpeedTimeFreeMaterials.Lit("STOP red", new Color(.88f, .08f, .12f), .3f);
            stripRed = SpeedTimeFreeMaterials.Unlit("STOP floor glow", new Color(1f, .16f, .14f));
            stripGlow = SpeedTimeFreeMaterials.Unlit("STOP floor halo", new Color(1f, .2f, .15f, .35f), true);
            navy = SpeedTimeFreeMaterials.Lit("Board navy", new Color(.04f, .16f, .35f), .3f);
            cyanGlow = SpeedTimeFreeMaterials.Unlit("Holo cyan", new Color(.16f, .86f, 1f));
            gravel = SpeedTimeFreeMaterials.Lit("Ballast gravel", new Color(.55f, .5f, .45f), .05f);
            sandTrap = SpeedTimeFreeMaterials.Lit("Sand trap", new Color(.9f, .8f, .55f), .05f);
            barrierOrange = SpeedTimeFreeMaterials.Lit("Barrier orange", new Color(1f, .45f, .05f), .3f);
            gateBlue = SpeedTimeFreeMaterials.Unlit("Speed gate blue", new Color(.17f, .45f, 1f));
            good = SpeedTimeFreeMaterials.Unlit("Flash green", new Color(.2f, .95f, .4f));
            bad = SpeedTimeFreeMaterials.Unlit("Flash red", new Color(1f, .15f, .15f));
            tunnelDark = SpeedTimeFreeMaterials.Unlit("Tunnel darkness", new Color(.02f, .02f, .03f));
            zoneYellow = SpeedTimeFreeMaterials.UnlitTextured("Slow zone stripes", StripeTexture(), Color.white, false);
        }

        static Texture2D StripeTexture()
        {
            string path = Folder + "/Generated/SlowZoneStripes.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) tex.SetPixel(x, y, ((x + y) / 16) % 2 == 0 ? new Color(1f, .82f, .05f) : new Color(.08f, .08f, .08f));
                tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path); imp.wrapMode = TextureWrapMode.Repeat; imp.filterMode = FilterMode.Point; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static TripleVehicleArena BuildArena(VehicleSet v)
        {
            ArenaMaterials();
            var arenaGo = Group("TripleVehicleArena", root); var arena = arenaGo.AddComponent<TripleVehicleArena>();
            var t = arenaGo.transform;
            arena.k = (float)catalog.course.k; arena.laneZ = LaneZ;
            var lanes = Group("Lanes (static)", t).transform;
            Lane(lanes, 0, "Car road"); Lane(lanes, 1, "Train track"); Lane(lanes, 2, "Motorcycle road");
            Tunnel(lanes); Embankments(lanes);
            for (int i = 0; i < 3; i++)
            {
                var tile = (GameObject)PrefabUtility.InstantiatePrefab(v.track, lanes);
                tile.name = "Track tile " + (i + 1); tile.transform.localPosition = new Vector3(LaneStart + i * (LaneEnd - LaneStart) / 3f, LaneY[1] + BallastTop, LaneZ[1]);
                foreach (var r in tile.GetComponentsInChildren<Renderer>()) r.meshLodSelectionBias = 3f;
            }
            foreach (var tr in lanes.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticEditorFlags.BatchingStatic);

            var vehicles = Group("Vehicles", t).transform;
            var sets = new[] { v.car, v.train, v.bike };
            var colors = new[] { GraphPanel.Hex("#3D9BFF"), GraphPanel.Hex("#FF4D5A"), GraphPanel.Hex("#FF9F1A") };
            for (int i = 0; i < 3; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(sets[i].prefab, vehicles);
                float y = LaneY[i] + (i == 1 ? BallastTop + TrackHeight : .03f);
                go.transform.localPosition = new Vector3(0, y, LaneZ[i]);
                var mover = go.GetComponent<VehicleMover>(); arena.vehicles[i] = mover;
                mover.lane = i; mover.color = colors[i]; mover.k = arena.k; mover.offCourseMetres = (float)catalog.course.offCourse;
                var audio = go.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 1; audio.minDistance = 8; audio.maxDistance = 140; audio.dopplerLevel = 0; mover.engine = audio;
                float h = sets[i].height;
                var tag = WorldText("Speed tag", go.transform, new Vector3(-Mathf.Min(sets[i].length * .5f, 3f), h + (i == 1 ? .9f : 1.4f), 0), mover.vehicleName + " 0.0 m/s", 1, colors[i], 14);
                Outline(tag);
                var bb = tag.gameObject.AddComponent<BillboardLabel>(); bb.constantAngularSize = true; bb.degrees = 1.6f; bb.lineHeightAtScale1 = 4f;
                mover.speedTag = tag;
                // Ghosts: low-poly proxy hulls (never the heavy meshes).
                var gp = Group(mover.vehicleName + " ghosts", t).AddComponent<GhostPreview>(); gp.transform.localPosition = new Vector3(0, y, LaneZ[i]);
                gp.k = arena.k; gp.offCourseMetres = (float)catalog.course.offCourse;
                gp.stopGhost = MeshObject("Predicted-stop ghost", gp.transform, sets[i].ghostMesh, v.ghost, Vector3.zero, false).transform;
                gp.timeGhost = MeshObject("Hover-time ghost", gp.transform, sets[i].ghostMesh, v.ghostFaint, Vector3.zero, false).transform;
                arena.ghosts[i] = gp;
                Ribbon(arena, t, i);
                Stop(arena, t, i);
            }
            GateAndZone(arena, t);
            var fx = arenaGo.AddComponent<GateZoneEffects>(); fx.arena = arena; arena.effects = fx;
            fx.good = good; fx.bad = bad; fx.gateIdle = gateBlue; fx.zoneIdle = zoneYellow; fx.stripIdle = stripRed;
            fx.gateRenderers = arena.speedGate.GetComponentsInChildren<MeshRenderer>(); fx.zoneRenderers = new[] { arena.slowZoneFloor.GetComponent<Renderer>() };
            arena.ApplyVariant(catalog.demo, catalog.course);
            return arena;
        }

        static void Lane(Transform parent, int i, string name)
        {
            float z = LaneZ[i], w = LaneWidth[i], len = LaneEnd - LaneStart, cx = (LaneStart + LaneEnd) * .5f;
            var lane = Group(name, parent).transform; lane.localPosition = new Vector3(0, LaneY[i], z);
            if (i == 1)
            {
                MeshObject("Ballast", lane, Ballast(len, 4f, 3.1f, BallastTop), gravel, new Vector3(LaneStart, 0, 0));
            }
            else
            {
                Box("Asphalt", lane, new Vector3(cx, .01f, 0), new Vector3(len, .04f, w), asphalt, false);
                for (float x = LaneStart + 1; x < LaneEnd - 1; x += 3.2f) Box("Centre dash", lane, new Vector3(x + .8f, .035f, 0), new Vector3(1.6f, .01f, .14f), lineWhite, false);
                foreach (float s in new[] { -1f, 1f })
                {
                    Box("Edge line", lane, new Vector3(cx, .035f, s * (w * .5f - .25f)), new Vector3(len, .01f, .12f), lineWhite, false);
                    Box("Guardrail", lane, new Vector3(cx, .62f, s * (w * .5f + .55f)), new Vector3(len, .26f, .07f), rail);
                    for (float x = LaneStart; x <= LaneEnd; x += 2.5f) Box("Guardrail post", lane, new Vector3(x, .32f, s * (w * .5f + .62f)), new Vector3(.1f, .64f, .1f), post);
                }
            }
            // START: checkered strip x = 0..0.4 and a green sign on a post at x = -1 on the far side.
            int cells = Mathf.RoundToInt(w / .4f);
            for (int r = 0; r < 2; r++) for (int c = 0; c < cells; c++)
                    Box("START checker", lane, new Vector3(.1f + r * .2f, i == 1 ? BallastTop + .02f : .04f, -w * .5f + (c + .5f) * w / cells), new Vector3(.2f, .015f, w / cells), (r + c) % 2 == 0 ? checkerWhite : checkerBlack, false);
            var sign = Group("START sign", lane).transform; sign.localPosition = new Vector3(-1, 0, w * .5f + 1.1f);
            Box("Post", sign, new Vector3(0, 1.4f, 0), new Vector3(.14f, 2.8f, .14f), post);
            var board = Group("Board", sign).transform; board.localPosition = new Vector3(0, 3.1f, 0);
            Box("Green board", board, Vector3.zero, new Vector3(3.2f, 1.25f, .12f), startGreen);
            Box("White frame", board, new Vector3(0, 0, .02f), new Vector3(3.38f, 1.43f, .1f), checkerWhite);
            WorldText("START", board, new Vector3(0, 0, -.08f), "START", .85f, Color.white, 3);
            FaceEye(board);
            // Distance boards every 10 m (5 units), labelled in real metres, same x on all lanes.
            for (int n = 0; n <= 12; n++)
            {
                int m = Mathf.RoundToInt(n * 5 / (float)catalog.course.k);          // 5 world units apart, labelled in real metres
                var b = Group("Distance board " + m + " m", lane).transform; b.localPosition = new Vector3(n * 5f, 0, -w * .5f - (i == 1 ? 1.2f : 1.25f));
                Box("Board post", b, new Vector3(0, .45f, 0), new Vector3(.06f, .9f, .06f), post);
                var plate = Group("Plate", b).transform; plate.localPosition = new Vector3(0, 1.05f, 0);
                Box("Navy plate", plate, Vector3.zero, new Vector3(1.6f, .62f, .05f), navy);
                WorldText("Distance", plate, new Vector3(0, 0, -.04f), m + " m", .42f, new Color(.4f, .92f, 1f), 1.5f);
                FaceEye(plate);
            }
            // Sand trap and barrier at 125 m (x = 62.5).
            Box("Sand trap", lane, new Vector3((62.5f + LaneEnd) * .5f, i == 1 ? BallastTop + .03f : .045f, 0), new Vector3(LaneEnd - 62.5f, .02f, w), sandTrap, false);
            for (int k = 0; k < 6; k++) Box("125 m barrier", lane, new Vector3(62.5f, .55f, -w * .5f + (k + .5f) * w / 6), new Vector3(.35f, 1.1f, w / 6 - .05f), k % 2 == 0 ? barrierOrange : checkerWhite);
        }

        /// <summary>Grassy banks with rock faces between the terraces (bike 0, train 1.2, car 3.5).</summary>
        static void Embankments(Transform parent)
        {
            var bank = SpeedTimeFreeMaterials.Lit("Bank grass", new Color(.36f, .66f, .2f), .05f);
            MeshObject("Bank train to car", parent, Slope(-30, 80, 10.4f, LaneY[1], 12.3f, LaneY[0]), bank, Vector3.zero);
            MeshObject("Bank bike to train (west of river)", parent, Slope(-30, 19, 3.4f, LaneY[2], 5.7f, LaneY[1]), bank, Vector3.zero);
            MeshObject("Bank river to train", parent, Slope(19, 80, 5.0f, -.15f, 5.75f, LaneY[1]), bank, Vector3.zero);
            Box("Train terrace", parent, new Vector3(25, LaneY[1] - .5f, 8), new Vector3(110, 1f, 4.7f), bank, false);
            Box("Car terrace", parent, new Vector3(25, LaneY[0] - .5f, 16.6f), new Vector3(110, 1f, 8.6f), bank, false);
            var rnd = new System.Random(4);
            string synty = "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_0";
            for (int i = 0; i < 24; i++)
            {
                float x = -14 + i * 3.3f + (float)rnd.NextDouble() * 1.5f; if (Mathf.Abs(x - 32) < 2.5f || Mathf.Abs(x) < 2.5f) continue;
                var r = Asset(synty + (1 + rnd.Next(9)) + ".prefab", "Bank rock", parent, new Vector3(x, LaneY[1] + .3f, 11.1f + (float)rnd.NextDouble() * .7f), .45f + (float)rnd.NextDouble() * .35f, (float)rnd.NextDouble() * 360);
            }
        }
        static Mesh Slope(float x0, float x1, float z0, float y0, float z1, float y1)
        {
            var m = new Mesh { name = "Bank slope" };
            m.SetVertices(new[] { new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return SaveMesh(m, "Slope_" + x0 + "_" + z0 + "_" + z1);
        }

        static Mesh Ballast(float len, float bottom, float top, float h)
        {
            var m = new Mesh { name = "Ballast" };
            Vector3[] p = { new Vector3(0, 0, -bottom / 2), new Vector3(0, h, -top / 2), new Vector3(0, h, top / 2), new Vector3(0, 0, bottom / 2) };
            var verts = new List<Vector3>(); var tris = new List<int>();
            void Q(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int n = verts.Count; verts.AddRange(new[] { a, b, c, d }); tris.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 }); }
            var L = new Vector3(len, 0, 0);
            Q(p[0], p[1], p[1] + L, p[0] + L); Q(p[1], p[2], p[2] + L, p[1] + L); Q(p[2], p[3], p[3] + L, p[2] + L);
            m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return SaveMesh(m, "Ballast");
        }

        static void Tunnel(Transform parent)
        {
            var tun = Group("Rock tunnel (mouth at x = -8)", parent).transform; tun.localPosition = new Vector3(-8, LaneY[1], LaneZ[1]);
            const float cy = 1.7f, inner = 3.5f, outer = 4.6f;
            // Rock mass behind the portal (never reaching past the mouth).
            var blob = FacetedCone("Tunnel rock", 10, .3f, 61);
            foreach (var (pos, size) in new[] { (new Vector3(-9.5f, -1, 0), new Vector3(9.5f, 10.5f, 9)), (new Vector3(-14f, -1, 3.5f), new Vector3(8, 8.5f, 7)), (new Vector3(-13, -1, -4), new Vector3(8, 7.5f, 6)) })
                MeshObject("Rock mass", tun, blob, rock, pos).transform.localScale = size;
            // Dark opening and stone arch ring.
            Box("Tunnel darkness", tun, new Vector3(-.15f, cy * .5f, 0), new Vector3(.1f, cy, inner * 2), tunnelDark, false);
            var disc = Prim(PrimitiveType.Cylinder, "Tunnel darkness arch", tun, new Vector3(-.15f, cy, 0), new Vector3(inner * 2, .05f, inner * 2), tunnelDark, false);
            disc.transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int s = 0; s <= 12; s++)
            {
                float a = Mathf.PI * s / 12f; var dir = new Vector3(0, Mathf.Sin(a), Mathf.Cos(a));
                var block = Box("Arch stone", tun, new Vector3(.1f, cy, 0) + dir * (inner + outer) * .5f, new Vector3(1.1f, outer - inner, .95f), rock);
                block.transform.localRotation = Quaternion.LookRotation(Vector3.right, dir);
            }
            foreach (float s in new[] { -1f, 1f }) Box("Arch pillar", tun, new Vector3(.1f, cy * .5f, s * (inner + outer) * .5f), new Vector3(1.1f, cy, outer - inner), rock);
            // Rock shoulders either side of the mouth so the train clearly emerges from the hill.
            foreach (float s in new[] { -1f, 1f }) MeshObject("Rock shoulder", tun, blob, rock, new Vector3(-2.6f, -.8f, s * 6.1f)).transform.localScale = new Vector3(3.2f, 6.2f, 2.4f);
        }

        static void Stop(TripleVehicleArena arena, Transform parent, int i)
        {
            float w = LaneWidth[i], y0 = i == 1 ? BallastTop : 0;
            var g = Group(new[] { "CAR", "TRAIN", "BIKE" }[i] + " STOP gantry", parent).transform; g.localPosition = new Vector3(32, LaneY[i], LaneZ[i]);
            arena.stopGantries[i] = g;
            var strip = Box("STOP floor strip ±1 m", g, new Vector3(0, y0 + .05f, 0), new Vector3(1f, .02f, w), stripRed, false);
            arena.stopStrips[i] = strip.GetComponent<Renderer>();
            Box("STOP floor halo", g, new Vector3(0, y0 + .045f, 0), new Vector3(1.8f, .01f, w + .4f), stripGlow, false);
            var banner = Group("Banner", g).transform; banner.localPosition = new Vector3(0, 4.3f, 0);
            float bw = w + 1.2f;
            foreach (float s in new[] { -1f, 1f }) Box("Gantry post", banner, new Vector3(s * bw * .5f, -2.15f, 0), new Vector3(.22f, 4.3f + 1.0f, .22f), post);
            Box("Red STOP board", banner, Vector3.zero, new Vector3(bw - .3f, 1.6f, .12f), stopRed);
            // Both faces: front reads from the START side, back from the far side (the deck camera).
            foreach (float face in new[] { -1f, 1f })
            {
                for (int side = -1; side <= 1; side += 2)
                    for (int r = 0; r < 4; r++) for (int c = 0; c < 2; c++)
                            Box("Checker", banner, new Vector3(side * (bw * .5f - .52f) + (c - .5f) * .36f, -.54f + r * .36f, face * .07f), new Vector3(.36f, .36f, .02f), (r + c) % 2 == 0 ? checkerWhite : checkerBlack, false);
                var stopText = WorldText("STOP", banner, new Vector3(0, .16f, face * .09f), "STOP", 1.0f, Color.white, bw * .62f);
                var dist = WorldText("STOP distance", banner, new Vector3(0, -.45f, face * .09f), "64 m", .62f, new Color(1f, .93f, .6f), bw * .62f);
                if (face > 0) { stopText.transform.localRotation = dist.transform.localRotation = Quaternion.Euler(0, 180, 0); arena.stopLabelsBack[i] = dist; }
                else arena.stopLabels[i] = dist;
            }
            // Square across the road: the banner spans the lane width (world Z) and faces along the road (world X).
            banner.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            var res = WorldText("Stop result", g, new Vector3(0, 5.8f, 0), "", 1, Color.white, 12);
            var bb = res.gameObject.AddComponent<BillboardLabel>(); bb.constantAngularSize = true; bb.degrees = 1.25f; bb.lineHeightAtScale1 = 4f;
            Outline(res);
            arena.stopResults[i] = res;
        }

        static void Ribbon(TripleVehicleArena arena, Transform parent, int i)
        {
            float w = LaneWidth[i], y0 = i == 1 ? BallastTop + .05f : .06f;
            var go = Group(new[] { "CAR", "TRAIN", "BIKE" }[i] + " distance ribbon", parent); go.transform.localPosition = new Vector3(0, LaneY[i], LaneZ[i] - w * .5f + (i == 1 ? .25f : .55f));
            var r = go.AddComponent<DistanceRibbon>(); r.k = arena.k; r.y = y0;
            var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = false; lr.positionCount = 2; lr.widthMultiplier = .32f; lr.sharedMaterial = cyanGlow;
            lr.alignment = LineAlignment.TransformZ; lr.shadowCastingMode = ShadowCastingMode.Off; lr.numCapVertices = 2;
            go.transform.localRotation = Quaternion.identity;
            lr.SetPosition(0, new Vector3(0, y0, 0)); lr.SetPosition(1, new Vector3(1, y0, 0));
            // TransformZ alignment would make the ribbon vertical; rotate the line's plane flat with a child instead.
            lr.alignment = LineAlignment.View;
            r.ribbon = lr; r.ticks = new Transform[10]; r.tickLabels = new TMP_Text[10];
            for (int k = 0; k < 10; k++)
            {
                var tick = Group("Time tick", go.transform).transform;
                Box("Tick post", tick, new Vector3(0, y0 + .35f, 0), new Vector3(.08f, .7f, .08f), cyanGlow, false);
                var label = WorldText("Tick label", tick, new Vector3(0, y0 + 1.15f, 0), "t = 4 s", .55f, new Color(.55f, .95f, 1f), 4);
                Outline(label);
                var bb = label.gameObject.AddComponent<BillboardLabel>(); bb.yawOnly = true;
                r.ticks[k] = tick; r.tickLabels[k] = label; tick.gameObject.SetActive(false);
            }
            arena.ribbons[i] = r;
        }

        static void GateAndZone(TripleVehicleArena arena, Transform parent)
        {
            float z = LaneZ[2], w = LaneWidth[2];
            var gate = Group("BIKE speed gate", parent).transform; gate.localPosition = new Vector3(7.5f, 0, z); arena.speedGate = gate;
            foreach (float s in new[] { -1f, 1f }) Box("Gate pillar", gate, new Vector3(0, 1.8f, s * (w * .5f + .35f)), new Vector3(.3f, 3.6f, .3f), gateBlue, false);
            Box("Gate arch", gate, new Vector3(0, 3.65f, 0), new Vector3(.3f, .35f, w + 1.0f), gateBlue, false);
            arena.gateLabel = WorldText("Gate rule", gate, new Vector3(0, 4.9f, 0), "SPEED GATE\n≥ 6 m/s", .6f, new Color(.55f, .8f, 1f), 6);
            Outline(arena.gateLabel);
            arena.gateLabel.gameObject.AddComponent<BillboardLabel>().yawOnly = true;
            var zone = Group("BIKE slow zone", parent).transform; zone.localPosition = new Vector3(15, 0, z); arena.slowZone = zone;
            var floor = Box("Yellow-striped floor", zone, new Vector3(2, .05f, 0), new Vector3(4, .02f, w), zoneYellow, false);
            arena.slowZoneFloor = floor.transform;
            arena.zoneLabel = WorldText("Zone rule", zone, new Vector3(2, 3.2f, -w * .5f - .6f), "SLOW ZONE\n≤ 4 m/s", .6f, new Color(1f, .85f, .2f), 6);
            Outline(arena.zoneLabel);
            arena.zoneLabel.gameObject.AddComponent<BillboardLabel>().yawOnly = true;
        }

        static void SaveArenaPrefab(TripleVehicleArena arena)
        {
            string path = Folder + "/Prefabs/TripleVehicleArena.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(arena.gameObject, path, InteractionMode.AutomatedAction);
            Debug.Log("[STGF] saved " + path);
        }
    }
}