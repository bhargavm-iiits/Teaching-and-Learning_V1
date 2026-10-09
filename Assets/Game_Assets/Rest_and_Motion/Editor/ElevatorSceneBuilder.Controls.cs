using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Act = TeachingAndLearning.RestAndMotion.ElevatorVRInteractable.Action;

namespace TeachingAndLearning.RestAndMotion.Editor
{
    public static partial class ElevatorSceneBuilder
    {
        const string PanelFolder = ImportedPath + "/LiftPanel";
        const float CapHalf = 0.0395f;
        const float ButtonHalf = 0.047f;

        struct PrintedButton
        {
            public string name; public Vector2 uv; public Act action; public int floor; public LiftButton.LampMode lamp;
            public PrintedButton(string name, float u, float v, Act action, int floor, LiftButton.LampMode lamp)
            { this.name = name; uv = new Vector2(u, v); this.action = action; this.floor = floor; this.lamp = lamp; }
        }

        static readonly PrintedButton[] Printed =
        {
            new PrintedButton("P", 0.841f, 0.049f, Act.Floor, 0, LiftButton.LampMode.CallLamp),
            new PrintedButton("1", 0.842f, 0.150f, Act.Floor, 1, LiftButton.LampMode.CallLamp),
            new PrintedButton("2", 0.746f, 0.150f, Act.Floor, 2, LiftButton.LampMode.CallLamp),
            new PrintedButton("3", 0.930f, 0.050f, Act.Floor, 3, LiftButton.LampMode.CallLamp),
            new PrintedButton("4", 0.844f, 0.233f, Act.Floor, 4, LiftButton.LampMode.CallLamp),
            new PrintedButton("5", 0.929f, 0.234f, Act.Floor, 5, LiftButton.LampMode.CallLamp),
            new PrintedButton("6", 0.747f, 0.050f, Act.Floor, 6, LiftButton.LampMode.CallLamp),
            new PrintedButton("7", 0.650f, 0.268f, Act.Floor, 7, LiftButton.LampMode.CallLamp),
            new PrintedButton("8", 0.931f, 0.150f, Act.Floor, 8, LiftButton.LampMode.CallLamp),
            new PrintedButton("9", 0.746f, 0.324f, Act.Floor, 9, LiftButton.LampMode.CallLamp),
            new PrintedButton("GO", 0.926f, 0.323f, Act.Go, -1, LiftButton.LampMode.Flash),
            new PrintedButton("STOP", 0.651f, 0.053f, Act.Stop, -1, LiftButton.LampMode.CallLamp),
            new PrintedButton("OPEN", 0.747f, 0.230f, Act.Open, -1, LiftButton.LampMode.WhileHeld),
            new PrintedButton("CLOSE", 0.838f, 0.320f, Act.Close, -1, LiftButton.LampMode.Flash),
            new PrintedButton("BELL", 0.653f, 0.173f, Act.Bell, -1, LiftButton.LampMode.WhileHeld),
        };

        static void BuildLiftPanel(Transform root, LiftCar car, ElevatorVRLesson lesson, List<ElevatorVRInteractable> targets)
        {
            EnsureFolder(PanelFolder);
            var panel = root.Find("Supplied Controls - Elevator _ elevator key.fbx").gameObject;
            var filter = panel.GetComponentsInChildren<MeshFilter>().Single();
            var renderer = filter.GetComponent<MeshRenderer>();
            var source = filter.sharedMesh;
            if (source.subMeshCount != 1) throw new InvalidOperationException("Expected the supplied panel to be one sub-mesh.");
            var local = source.vertices;
            var world = local.Select(v => filter.transform.TransformPoint(v)).ToArray();
            var triangles = source.triangles;
            var panelBounds = RenderedBounds(panel);

            var centers = Printed.Select(b => FindPrintedButton(panel, b.uv)).ToArray();
            var fronts = new float[Printed.Length];
            for (int b = 0; b < Printed.Length; b++)
            {
                fronts[b] = float.MaxValue;
                for (int i = 0; i < world.Length; i++)
                    if (Mathf.Abs(world[i].y - centers[b].y) < ButtonHalf && Mathf.Abs(world[i].z - centers[b].z) < ButtonHalf) fronts[b] = Mathf.Min(fronts[b], world[i].x);
            }

            var capTriangles = Printed.Select(_ => new List<int>()).ToArray();
            var bezelOfP = new List<int>();
            var remaining = new List<int>();
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int owner = -1;
                for (int b = 0; b < Printed.Length && owner < 0; b++)
                {
                    bool inCap = true;
                    for (int k = 0; k < 3 && inCap; k++)
                    {
                        var p = world[triangles[t + k]];
                        inCap = Mathf.Max(Mathf.Abs(p.y - centers[b].y), Mathf.Abs(p.z - centers[b].z)) < CapHalf && p.x - fronts[b] < 0.0025f;
                    }
                    if (inCap) owner = b;
                }
                if (owner >= 0) capTriangles[owner].AddRange(new[] { triangles[t], triangles[t + 1], triangles[t + 2] });
                else
                {
                    remaining.AddRange(new[] { triangles[t], triangles[t + 1], triangles[t + 2] });
                    bool inP = true;
                    for (int k = 0; k < 3 && inP; k++)
                    {
                        var p = world[triangles[t + k]];
                        inP = Mathf.Abs(p.y - centers[0].y) < ButtonHalf && Mathf.Abs(p.z - centers[0].z) < ButtonHalf;
                    }
                    if (inP) bezelOfP.AddRange(new[] { triangles[t], triangles[t + 1], triangles[t + 2] });
                }
            }
            for (int b = 0; b < Printed.Length; b++)
                if (capTriangles[b].Count < 30) throw new InvalidOperationException("Could not separate the cap of printed button " + Printed[b].name + " (" + capTriangles[b].Count / 3 + " triangles).");

            var staticMesh = UnityEngine.Object.Instantiate(source);
            staticMesh.name = "LiftPanel_Static";
            staticMesh.SetTriangles(remaining, 0);
            filter.sharedMesh = SaveAsset(staticMesh, PanelFolder + "/LiftPanel_Static.asset");

            var capMaterial = new Material(renderer.sharedMaterial) { name = "LiftButtonCap" };
            capMaterial.EnableKeyword("_EMISSION");
            capMaterial.SetTexture("_EmissionMap", renderer.sharedMaterial.GetTexture("_BaseMap"));
            capMaterial.SetColor("_EmissionColor", Color.black);
            capMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            capMaterial = SaveAsset(capMaterial, ImportedPath + "/Materials/LiftButtonCap.mat");
            var ringMaterial = UnlitMaterial("LiftLampRing", new Color(0.09f, 0.09f, 0.1f));
            var coverMaterial = UnlitMaterial("LiftButtonFace", new Color(0.11f, 0.11f, 0.12f));
            var ringMesh = SaveAsset(SquareRing(0.0332f, 0.0388f), PanelFolder + "/LampRing.asset");
            var coverMesh = SaveAsset(Quad(0.058f, 0.058f), PanelFolder + "/ButtonFace.asset");

            var controls = new GameObject("Lift panel - pressable buttons").transform;
            controls.SetParent(root, false);
            var buttons = new Dictionary<string, LiftButton>();
            Mesh capOfP = null;
            for (int b = 0; b < Printed.Length; b++)
            {
                var capMesh = SaveAsset(SubMesh(source, capTriangles[b], "LiftCap_" + Printed[b].name), PanelFolder + "/Cap_" + Printed[b].name + ".asset");
                if (b == 0) capOfP = capMesh;
                var front = new Vector3(fronts[b], centers[b].y, centers[b].z);
                var button = CreateLiftButton(controls, Printed[b].name, front, Vector3.zero, filter.transform, capMesh, capMaterial, null, null, ringMesh, ringMaterial, null, null,
                    lesson, Printed[b].action, Printed[b].floor, Printed[b].lamp, targets);
                buttons[Printed[b].name] = button;
            }

            // Floors 10-12 do not exist on the supplied panel. Clone the supplied P button (cap + bezel) into the free slots
            // above the grid and cover the printed glyph with the new number.
            var bezelMesh = SaveAsset(SubMesh(source, bezelOfP, "LiftBezel_Clone"), PanelFolder + "/Bezel_Clone.asset");
            float rowStep = centers[1].y - centers[0].y;
            int bellIndex = Array.FindIndex(Printed, p => p.name == "BELL");
            var slots = new[]
            {
                ("10", new Vector3(fronts[0], centers[bellIndex].y, centers[0].z)),
                ("11", new Vector3(fronts[0], centers[bellIndex].y + rowStep, centers[0].z)),
                ("12", new Vector3(fronts[0], centers[bellIndex].y + rowStep, centers[bellIndex].z)),
            };
            foreach (var (name, front) in slots)
            {
                if (front.y + ButtonHalf > panelBounds.max.y - 0.5f) throw new InvalidOperationException("No room on the supplied panel for button " + name);
                var offset = front - new Vector3(fronts[0], centers[0].y, centers[0].z);
                var button = CreateLiftButton(controls, name, front, offset, filter.transform, capOfP, capMaterial, bezelMesh, renderer.sharedMaterial, ringMesh, ringMaterial, coverMesh, coverMaterial,
                    lesson, Act.Floor, int.Parse(name), LiftButton.LampMode.CallLamp, targets);
                buttons[name] = button;
            }

            for (int floor = 0; floor <= LiftLogic.MaxFloor; floor++)
                car.floorButtons[floor] = buttons[floor == 0 ? "P" : floor.ToString()];
            car.openButton = buttons["OPEN"]; car.closeButton = buttons["CLOSE"]; car.bellButton = buttons["BELL"];
            car.stopButton = buttons["STOP"]; car.goButton = buttons["GO"];

            // Displays in the two dark windows of the supplied panel.
            float screenX = panelBounds.min.x - 0.004f;
            float topWindowY = panelBounds.min.y + panelBounds.size.y * 0.87f;
            float zMid = (centers[0].z + centers[bellIndex].z) * 0.5f;
            float zSpan = Mathf.Abs(centers[0].z - centers[bellIndex].z);
            car.floorDisplay = DisplayText(controls, "Floor display", "01", new Vector3(screenX, topWindowY, zMid + zSpan * 0.22f), new Vector2(0.15f, 0.15f), 1.2f);
            car.upArrow = Arrow(controls, "Up arrow", new Vector3(screenX, topWindowY + 0.045f, zMid - zSpan * 0.36f), true);
            car.downArrow = Arrow(controls, "Down arrow", new Vector3(screenX, topWindowY - 0.045f, zMid - zSpan * 0.36f), false);
            float slotY = panelBounds.min.y + panelBounds.size.y * 0.705f;
            car.statusDisplay = DisplayText(controls, "Status display", "READY", new Vector3(screenX, slotY, zMid), new Vector2(0.22f, 0.07f), 0.24f);

            var audio = controls.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 1f; audio.minDistance = 0.6f; audio.maxDistance = 6f;
            car.panelAudio = audio;
            var motor = new GameObject("Lift motor hum").AddComponent<AudioSource>();
            motor.transform.SetParent(root, false);
            motor.transform.position = new Vector3(0f, 3.3f, 0f);
            motor.playOnAwake = false; motor.spatialBlend = 0.5f; motor.loop = true;
            car.motorAudio = motor;
        }

        static LiftButton CreateLiftButton(Transform parent, string name, Vector3 front, Vector3 cloneOffset, Transform sourceTransform, Mesh capMesh, Material capMaterial,
            Mesh bezelMesh, Material bezelMaterial, Mesh ringMesh, Material ringMaterial, Mesh coverMesh, Material coverMaterial,
            ElevatorVRLesson lesson, Act action, int floor, LiftButton.LampMode lampMode, List<ElevatorVRInteractable> targets)
        {
            var root = new GameObject("Lift button " + name).transform;
            root.SetParent(parent, false);
            root.position = front;
            root.rotation = Quaternion.Euler(0f, 90f, 0f);
            var mover = new GameObject("Moving cap").transform;
            mover.SetParent(root, false);

            var cap = new GameObject("Cap (supplied geometry)").transform;
            cap.SetParent(mover, false);
            cap.position = sourceTransform.position + cloneOffset;
            cap.rotation = sourceTransform.rotation;
            cap.localScale = sourceTransform.lossyScale;
            cap.gameObject.AddComponent<MeshFilter>().sharedMesh = capMesh;
            var capRenderer = cap.gameObject.AddComponent<MeshRenderer>();
            capRenderer.sharedMaterial = capMaterial;

            if (bezelMesh != null)
            {
                var bezel = new GameObject("Bezel (copy of supplied P button)").transform;
                bezel.SetParent(root, false);
                bezel.position = sourceTransform.position + cloneOffset;
                bezel.rotation = sourceTransform.rotation;
                bezel.localScale = sourceTransform.lossyScale;
                bezel.gameObject.AddComponent<MeshFilter>().sharedMesh = bezelMesh;
                bezel.gameObject.AddComponent<MeshRenderer>().sharedMaterial = bezelMaterial;
            }

            var ring = new GameObject("Lamp ring").transform;
            ring.SetParent(mover, false);
            ring.localPosition = new Vector3(0f, 0f, -0.0006f);
            ring.gameObject.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            var ringRenderer = ring.gameObject.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = ringMaterial;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            TMP_Text overlay = null;
            if (coverMesh != null)
            {
                var cover = new GameObject("Face for new number").transform;
                cover.SetParent(mover, false);
                cover.localPosition = new Vector3(0f, 0f, -0.0003f);
                cover.gameObject.AddComponent<MeshFilter>().sharedMesh = coverMesh;
                var coverRenderer = cover.gameObject.AddComponent<MeshRenderer>();
                coverRenderer.sharedMaterial = coverMaterial;
                coverRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var label = new GameObject("Number", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
                label.transform.SetParent(mover, false);
                label.transform.localPosition = new Vector3(0f, -0.001f, -0.0012f);
                label.font = font;
                label.text = name;
                label.fontSize = 0.38f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.rectTransform.sizeDelta = new Vector2(0.06f, 0.05f);
                label.color = Color.white;
                overlay = label;
            }

            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0f, 0.008f);
            collider.size = new Vector3(0.088f, 0.088f, 0.03f);
            var lift = root.gameObject.AddComponent<LiftButton>();
            lift.cap = mover;
            lift.pressDirectionLocal = Vector3.forward;
            lift.travel = 0.004f;
            lift.capRenderer = capRenderer;
            lift.lampRing = ringRenderer;
            lift.overlayLabel = overlay;
            lift.lampMode = lampMode;
            if (action == Act.Go) lift.lampColor = new Color(0.25f, 1f, 0.35f);
            if (action == Act.Stop || action == Act.Bell) lift.lampColor = new Color(1f, 0.22f, 0.18f);

            var interactable = root.gameObject.AddComponent<ElevatorVRInteractable>();
            interactable.lesson = lesson;
            interactable.action = action;
            interactable.floorNumber = floor;
            interactable.liftButton = lift;
            interactable.caption = name;
            var xr = root.GetComponent<XRSimpleInteractable>();
            xr.interactionLayers = 1;
            xr.colliders.Clear();
            xr.colliders.Add(collider);
            var poke = root.gameObject.AddComponent<XRPokeFilter>();
            poke.pokeInteractable = xr;
            poke.pokeCollider = collider;
            targets.Add(interactable);
            return lift;
        }

        static Vector3 FindPrintedButton(GameObject panel, Vector2 textureUV)
        {
            var panelBounds = RenderedBounds(panel);
            float closest = float.PositiveInfinity;
            var center = Vector3.zero;
            foreach (var filter in panel.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var coordinates = mesh.uv;
                var triangles = mesh.triangles;
                for (int index = 0; index < triangles.Length; index += 3)
                {
                    var first = filter.transform.TransformPoint(vertices[triangles[index]]);
                    var second = filter.transform.TransformPoint(vertices[triangles[index + 1]]);
                    var third = filter.transform.TransformPoint(vertices[triangles[index + 2]]);
                    var normal = Vector3.Cross(second - first, third - first).normalized;
                    var centroid = (first + second + third) / 3f;
                    if (Mathf.Abs(normal.x) < 0.9f || centroid.x > panelBounds.min.x + panelBounds.size.x * 0.6f) continue;
                    var uv = (coordinates[triangles[index]] + coordinates[triangles[index + 1]] + coordinates[triangles[index + 2]]) / 3f;
                    float distance = (uv - textureUV).sqrMagnitude;
                    if (distance >= closest) continue;
                    closest = distance;
                    var bounds = new Bounds(first, Vector3.zero);
                    bounds.Encapsulate(second);
                    bounds.Encapsulate(third);
                    center = bounds.center;
                }
            }
            if (closest > 0.008f) throw new InvalidOperationException("Cannot locate supplied printed button at texture UV " + textureUV);
            return center;
        }

        static Mesh SubMesh(Mesh source, List<int> triangles, string name)
        {
            var map = new Dictionary<int, int>();
            var positions = source.vertices; var normals = source.normals; var tangents = source.tangents; var uvs = source.uv;
            var p = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<Vector4>(); var u = new List<Vector2>();
            var indices = new List<int>();
            foreach (int index in triangles)
            {
                if (!map.TryGetValue(index, out int mapped))
                {
                    mapped = p.Count; map[index] = mapped;
                    p.Add(positions[index]);
                    if (normals.Length > 0) n.Add(normals[index]);
                    if (tangents.Length > 0) t.Add(tangents[index]);
                    if (uvs.Length > 0) u.Add(uvs[index]);
                }
                indices.Add(mapped);
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(p);
            if (n.Count == p.Count) mesh.SetNormals(n);
            if (t.Count == p.Count) mesh.SetTangents(t);
            if (u.Count == p.Count) mesh.SetUVs(0, u);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh SquareRing(float inner, float outer)
        {
            var mesh = new Mesh { name = "LampRing" };
            var v = new List<Vector3>();
            foreach (float s in new[] { outer, inner })
            {
                v.Add(new Vector3(-s, -s, 0f)); v.Add(new Vector3(s, -s, 0f)); v.Add(new Vector3(s, s, 0f)); v.Add(new Vector3(-s, s, 0f));
            }
            var tris = new List<int>();
            for (int side = 0; side < 4; side++)
            {
                int o0 = side, o1 = (side + 1) % 4, i0 = 4 + side, i1 = 4 + (side + 1) % 4;
                tris.AddRange(new[] { o0, i0, o1, o1, i0, i1 });
            }
            mesh.SetVertices(v);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(Enumerable.Repeat(Vector3.back, v.Count).ToList());
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh Quad(float width, float height)
        {
            var mesh = new Mesh { name = "Quad" };
            float w = width / 2f, h = height / 2f;
            mesh.SetVertices(new List<Vector3> { new Vector3(-w, -h, 0f), new Vector3(w, -h, 0f), new Vector3(w, h, 0f), new Vector3(-w, h, 0f) });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.SetNormals(Enumerable.Repeat(Vector3.back, 4).ToList());
            mesh.RecalculateBounds();
            return mesh;
        }

        static Renderer Arrow(Transform parent, string name, Vector3 position, bool up)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            float s = up ? 1f : -1f;
            var mesh = new Mesh { name = up ? "ArrowUp" : "ArrowDown" };
            mesh.SetVertices(new List<Vector3> { new Vector3(-0.028f, -0.022f * s, 0f), new Vector3(0f, 0.026f * s, 0f), new Vector3(0.028f, -0.022f * s, 0f) });
            mesh.SetTriangles(up ? new[] { 0, 1, 2 } : new[] { 0, 2, 1 }, 0);
            mesh.SetNormals(Enumerable.Repeat(Vector3.back, 3).ToList());
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = SaveAsset(mesh, PanelFolder + "/" + mesh.name + ".asset");
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = UnlitMaterial("LiftArrow", new Color(0.04f, 0.08f, 0.05f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }

        static TMP_Text DisplayText(Transform parent, string name, string value, Vector3 position, Vector2 size, float fontSize)
        {
            var text = new GameObject(name, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            text.transform.SetParent(parent, false);
            text.transform.position = position;
            text.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.25f, 1f, 0.4f);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = size;
            return text;
        }

        static Material UnlitMaterial(string name, Color color)
        {
            string path = ImportedPath + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            EnsureFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'));
            AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'), System.IO.Path.GetFileName(folder));
        }

        static void BuildWorkingDoors(Transform root, LiftCar car)
        {
            var cabin = root.Find("Supplied Cabin - winda.fbx");
            var bounds = RenderedBounds(cabin.gameObject);
            var filter = cabin.GetComponentsInChildren<MeshFilter>().Single();
            var source = filter.sharedMesh;
            var vertices = source.vertices;
            var window = source.GetTriangles(source.subMeshCount - 1);
            var left = new List<int>();
            var right = new List<int>();
            for (int index = 0; index < window.Length; index += 3)
            {
                var centroid = filter.transform.TransformPoint((vertices[window[index]] + vertices[window[index + 1]] + vertices[window[index + 2]]) / 3f);
                (centroid.x < bounds.center.x ? left : right).AddRange(new[] { window[index], window[index + 1], window[index + 2] });
            }
            if (left.Count == 0 || right.Count == 0) throw new InvalidOperationException("The supplied glass geometry cannot be divided into two door leaves.");
            var mount = new GameObject("Sliding glass doors (supplied glass faces)").transform;
            mount.SetParent(root, false);
            var glass = filter.GetComponent<Renderer>().sharedMaterials.Last();
            car.doorLeaves = new Transform[2];
            car.doorClosedPositions = new Vector3[2];
            car.doorOpenOffset = new Vector3(bounds.size.x * 0.42f, 0f, 0f);
            for (int side = 0; side < 2; side++)
            {
                var mesh = UnityEngine.Object.Instantiate(source);
                mesh.subMeshCount = 1;
                mesh.SetTriangles(side == 0 ? left : right, 0);
                var leaf = new GameObject(side == 0 ? "Left door leaf" : "Right door leaf").transform;
                leaf.SetParent(mount, false);
                leaf.position = filter.transform.position;
                leaf.rotation = filter.transform.rotation;
                leaf.localScale = filter.transform.lossyScale;
                leaf.gameObject.AddComponent<MeshFilter>().sharedMesh = SaveAsset(mesh, ImportedPath + "/LiftDoor" + side + ".asset");
                leaf.gameObject.AddComponent<MeshRenderer>().sharedMaterial = glass;
                car.doorLeaves[side] = leaf;
                car.doorClosedPositions[side] = leaf.localPosition;
            }
            var fixedMesh = UnityEngine.Object.Instantiate(source);
            fixedMesh.SetTriangles(Array.Empty<int>(), source.subMeshCount - 1);
            filter.sharedMesh = SaveAsset(fixedMesh, ImportedPath + "/LiftFixedCabin.asset");
        }
    }
}
