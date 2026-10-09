using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Act = TeachingAndLearning.RestAndMotion.ElevatorVRInteractable.Action;

namespace TeachingAndLearning.RestAndMotion.Editor
{
    public static partial class ElevatorSceneBuilder
    {
        static readonly Color Navy = new Color(0.055f, 0.1f, 0.2f);

        static void BuildLessonBoard(Transform root, ElevatorVRLesson lesson, Bounds cabinBounds, List<ElevatorVRInteractable> targets)
        {
            var board = new GameObject("Cabin-mounted lesson board").transform;
            board.SetParent(root, false);
            board.position = new Vector3(cabinBounds.min.x + 0.18f, 1.58f, 0.05f);
            board.rotation = Quaternion.Euler(0f, -90f, 0f);
            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Board backing";
            backing.transform.SetParent(board, false);
            backing.transform.localScale = new Vector3(1.08f, 1.42f, 0.02f);
            backing.transform.localPosition = Vector3.forward * 0.018f;
            backing.GetComponent<Renderer>().sharedMaterial = UnlitMaterial("RamBoard", Navy);
            UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());

            var icon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            icon.name = "Title icon";
            icon.transform.SetParent(board, false);
            icon.transform.localPosition = new Vector3(-0.44f, 0.6f, -0.01f);
            icon.transform.localRotation = Quaternion.Euler(20f, 45f, 0f);
            icon.transform.localScale = Vector3.one * 0.05f;
            icon.GetComponent<Renderer>().sharedMaterial = UnlitMaterial("RamBlue", new Color(0.18f, 0.44f, 0.88f));
            UnityEngine.Object.DestroyImmediate(icon.GetComponent<Collider>());
            VRText(board, "Title", "Rest and Motion", new Vector3(0.05f, 0.615f, 0f), new Vector2(0.9f, 0.09f), 0.62f, Color.white, TextAlignmentOptions.Left);
            VRText(board, "Subtitle", "Elevator Ride", new Vector3(0.05f, 0.54f, 0f), new Vector2(0.9f, 0.06f), 0.34f, new Color(0.7f, 0.78f, 0.9f), TextAlignmentOptions.Left);
            lesson.progress = VRText(board, "Progress", "", new Vector3(0f, 0.47f, 0f), new Vector2(0.98f, 0.06f), 0.27f, new Color(0.45f, 0.7f, 1f), TextAlignmentOptions.Left);

            string[] steps = { "Level 1: Which lift is moving?", "Level 2: Inside the moving lift", "Level 3: Emergency free fall" };
            var badgeMesh = SaveAsset(Disc(0.032f, 24), ImportedPath + "/LiftPanel/Badge.asset");
            for (int i = 0; i < 3; i++)
            {
                float y = 0.38f - i * 0.085f;
                var badge = new GameObject("Step badge " + (i + 1));
                badge.transform.SetParent(board, false);
                badge.transform.localPosition = new Vector3(-0.45f, y, -0.004f);
                badge.AddComponent<MeshFilter>().sharedMesh = badgeMesh;
                var badgeRenderer = badge.AddComponent<MeshRenderer>();
                badgeRenderer.sharedMaterial = UnlitMaterial("RamBadge", new Color(0.18f, 0.44f, 0.88f));
                lesson.stepBadges[i] = badgeRenderer;
                VRText(board, "Step number " + (i + 1), (i + 1).ToString(), new Vector3(-0.45f, y, -0.008f), new Vector2(0.06f, 0.06f), 0.32f, Color.white, TextAlignmentOptions.Center);
                lesson.stepLabels[i] = VRText(board, "Step " + (i + 1), steps[i], new Vector3(0.06f, y, 0f), new Vector2(0.9f, 0.07f), 0.34f, Color.white, TextAlignmentOptions.Left);
            }
            lesson.prompt = VRText(board, "Prompt (caption)", "", new Vector3(0f, 0.0f, 0f), new Vector2(0.98f, 0.29f), 0.29f, Color.white, TextAlignmentOptions.TopLeft);
            lesson.feedback = VRText(board, "Feedback (caption)", "", new Vector3(0f, -0.26f, 0f), new Vector2(0.98f, 0.17f), 0.25f, new Color(1f, 0.85f, 0.5f), TextAlignmentOptions.TopLeft);

            BoardButton(board, lesson, Act.Replay, "RESTART", new Vector3(0.33f, -0.585f, 0f), new Vector2(0.28f, 0.09f), targets);
            VRText(board, "Controls hint", "VR: point + trigger, or press with your hand\nDesktop: right-drag look, WASD, click, Tab + Enter, keys 0-9, F10-F12",
                new Vector3(-0.17f, -0.59f, 0f), new Vector2(0.66f, 0.1f), 0.16f, new Color(0.6f, 0.65f, 0.75f), TextAlignmentOptions.Left);
        }

        static ElevatorVRInteractable BoardButton(Transform parent, ElevatorVRLesson lesson, Act action, string caption, Vector3 position, Vector2 size, List<ElevatorVRInteractable> targets)
        {
            var button = new GameObject("Board button - " + caption).transform;
            button.SetParent(parent, false);
            button.localPosition = position;
            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Button face";
            face.transform.SetParent(button, false);
            face.transform.localScale = new Vector3(size.x, size.y, 0.015f);
            face.transform.localPosition = new Vector3(0f, 0f, 0.002f);
            face.GetComponent<Renderer>().sharedMaterial = UnlitMaterial("RamButtonFace", new Color(0.1f, 0.17f, 0.32f));
            UnityEngine.Object.DestroyImmediate(face.GetComponent<Collider>());
            var collider = button.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x, size.y, 0.05f);
            var interactable = button.gameObject.AddComponent<ElevatorVRInteractable>();
            interactable.lesson = lesson;
            interactable.action = action;
            interactable.caption = caption;
            interactable.label = VRText(button, "Label", caption, new Vector3(0f, 0f, -0.01f), size, 0.3f, Color.white, TextAlignmentOptions.Center);
            interactable.outline = VRBorder(button, size, 0.008f);
            var xr = button.GetComponent<XRSimpleInteractable>();
            xr.interactionLayers = 1;
            xr.colliders.Clear();
            xr.colliders.Add(collider);
            targets.Add(interactable);
            return interactable;
        }

        /// <summary>
        /// Picks three tall supplied buildings spread left, centre and right that the learner can actually see
        /// through the glass, and hangs a selectable "BUILDING A/B/C" sign on each.
        /// </summary>
        static void BuildLandmarks(Transform exterior, LiftCar car, ElevatorVRLesson lesson, Camera eye, List<ElevatorVRInteractable> targets)
        {
            var renderers = exterior.GetComponentsInChildren<MeshRenderer>().Where(r => r.transform.IsChildOf(exterior) && r.name.Contains("_Source_")).ToList();
            var temporary = new List<MeshCollider>();
            foreach (var r in renderers) temporary.Add(r.gameObject.AddComponent<MeshCollider>());
            bool backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            try
            {
                car.Logic.Reset(6);
                car.ApplyWorld();
                Physics.SyncTransforms();
                float groundWorld = exterior.position.y + GroundY;
                var eyePosition = eye.transform.position;
                var hits = new List<(MeshRenderer part, Vector3 point, float yaw)>();
                for (float yaw = -38f; yaw <= 38f; yaw += 1f)
                    for (float pitch = -12f; pitch <= 20f; pitch += 2f)
                    {
                        var direction = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
                        if (!Physics.Raycast(eyePosition + direction * 1.6f, direction, out var hit, 200f)) continue;
                        var part = hit.collider.GetComponent<MeshRenderer>();
                        if (part == null || !renderers.Contains(part)) continue;
                        if (part.bounds.max.y - groundWorld < 24f || part.bounds.size.x > 30f) continue;

                        hits.Add((part, hit.point, yaw));
                    }
                var chosen = new List<(MeshRenderer part, Vector3 point, float yaw)>();
                foreach (float aim in new[] { -18f, 0f, 18f })
                {
                    var ordered = hits.Where(h => chosen.All(c => c.part != h.part && Mathf.Abs(c.yaw - h.yaw) > 9f))
                        .OrderBy(h => Mathf.Abs(h.yaw - aim) + Mathf.Abs(h.point.y - eyePosition.y - 3f) * 1.2f + Mathf.Max(0f, Vector3.Distance(h.point, eyePosition) - 32f) * 0.4f).Take(60);
                    foreach (var candidate in ordered)
                    {
                        float fraction = SignVisibleFraction(eye, eyePosition, candidate.point);
                        if (fraction < 0.97f) continue;
                        chosen.Add(candidate);
                        Debug.Log("[Rest and Motion] Sign candidate at yaw " + candidate.yaw + " is " + (fraction * 100f).ToString("F0") + "% visible.");
                        break;
                    }
                }                if (chosen.Count < 3) throw new InvalidOperationException("Only " + chosen.Count + " visible supplied buildings were found for the reference-point signs.");
                chosen = chosen.OrderBy(c => c.yaw).ToList();
                string[] letters = { "A", "B", "C" };
                for (int i = 0; i < 3; i++)
                {
                    var (part, point, _) = chosen[i];
                    var toEye = eyePosition - point; toEye.y = 0f;
                    var sign = new GameObject("Reference sign - BUILDING " + letters[i]).transform;
                    sign.SetParent(part.transform, true);
                    sign.position = point + toEye.normalized * 0.4f;
                    sign.rotation = Quaternion.LookRotation(-toEye.normalized, Vector3.up);
                    sign.localScale = Vector3.one * Mathf.Clamp(Vector3.Distance(point, eyePosition) / 26f, 0.8f, 1.6f) / sign.parent.lossyScale.x;
                    var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    panel.name = "Sign panel";
                    panel.transform.SetParent(sign, false);
                    panel.transform.localScale = new Vector3(4.6f, 1.9f, 0.12f);
                    panel.transform.localPosition = new Vector3(0f, 0f, 0.08f);
                    var panelRenderer = panel.GetComponent<Renderer>();
                    panelRenderer.sharedMaterial = UnlitMaterial("RamSign", new Color(0.18f, 0.45f, 1f));
                    panelRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
                    var target = sign.gameObject.AddComponent<ElevatorVRInteractable>();
                    target.lesson = lesson;
                    target.action = Act.Landmark;
                    target.referenceName = "Building " + letters[i];
                    target.caption = "BUILDING " + letters[i];
                    target.highlight = panelRenderer;
                    target.label = VRText(sign, "Sign text", target.caption, new Vector3(0f, 0f, -0.01f), new Vector2(4.4f, 1.6f), 7f, Color.white, TextAlignmentOptions.Center);
                    target.outline = VRBorder(sign, new Vector2(4.8f, 2.1f), 0.12f);
                    var collider = sign.gameObject.AddComponent<BoxCollider>();
                    collider.size = new Vector3(5f, 2.4f, 0.4f);
                    var xr = sign.GetComponent<XRSimpleInteractable>();
                    xr.interactionLayers = 1;
                    xr.colliders.Clear();
                    xr.colliders.Add(collider);
                    targets.Add(target);
                    Debug.Log("[Rest and Motion] Reference sign BUILDING " + letters[i] + " on " + part.name + " at " + sign.position);
                }
            }
            finally
            {
                Physics.queriesHitBackfaces = backfaces;
                foreach (var collider in temporary) UnityEngine.Object.DestroyImmediate(collider);
                car.Logic.Reset(car.startFloor);
                car.ApplyWorld();
            }
        }

        /// <summary>Renders a candidate sign in pure magenta and returns the share of it the learner can actually see.</summary>
        static float SignVisibleFraction(Camera camera, Vector3 eye, Vector3 point)
        {
            var toEye = eye - point; toEye.y = 0f;
            float scale = Mathf.Clamp(Vector3.Distance(point, eye) / 26f, 0.8f, 1.6f);
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", Color.magenta);
            probe.GetComponent<Renderer>().sharedMaterial = material;
            probe.layer = 31;
            probe.transform.position = point + toEye.normalized * 0.4f;
            probe.transform.rotation = Quaternion.LookRotation(-toEye.normalized, Vector3.up);
            probe.transform.localScale = new Vector3(4.6f, 1.9f, 0.12f) * scale;
            var target = new RenderTexture(480, 300, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            int mask = camera.cullingMask;
            var flags = camera.clearFlags;
            var background = camera.backgroundColor;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                int seen = CountMagenta(target);
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.Render();
                int total = CountMagenta(target);
                return total < 40 ? 0f : seen / (float)total;
            }
            finally
            {
                camera.cullingMask = mask;
                camera.clearFlags = flags;
                camera.backgroundColor = background;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(probe);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        static int CountMagenta(RenderTexture target)
        {
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            int count = 0;
            foreach (var pixel in image.GetPixels32())
                if (pixel.r - pixel.g > 70 && pixel.b - pixel.g > 70) count++;
            UnityEngine.Object.DestroyImmediate(image);
            return count;
        }

        static bool SignFullyVisible(Vector3 eye, Vector3 point, MeshRenderer part)
        {
            var toEye = eye - point; toEye.y = 0f;
            var right = Vector3.Cross(Vector3.up, toEye.normalized);
            float scale = Mathf.Clamp(Vector3.Distance(point, eye) / 26f, 0.8f, 1.6f);
            foreach (var corner in new[] { right * 2.5f * scale + Vector3.up * scale, -right * 2.5f * scale + Vector3.up * scale, right * 2.5f * scale - Vector3.up * scale, -right * 2.5f * scale - Vector3.up * scale })
            {
                var target = point + toEye.normalized * 0.4f + corner;
                var direction = target - eye;
                if (Physics.Raycast(eye + direction.normalized * 1.6f, direction.normalized, out var hit, direction.magnitude - 2.2f)) return false;
            }
            return true;
        }

        static TMP_Text VRText(Transform parent, string name, string value, Vector3 position, Vector2 size, float points, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = new GameObject(name, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.font = font;
            label.text = value;
            label.fontSize = points;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = FontStyles.Bold;
            label.rectTransform.sizeDelta = size;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        static LineRenderer VRBorder(Transform parent, Vector2 size, float width)
        {
            var line = new GameObject("Outline", typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 4;
            line.SetPositions(new[] { new Vector3(-size.x / 2f, -size.y / 2f, -0.02f), new Vector3(-size.x / 2f, size.y / 2f, -0.02f), new Vector3(size.x / 2f, size.y / 2f, -0.02f), new Vector3(size.x / 2f, -size.y / 2f, -0.02f) });
            line.widthMultiplier = width;
            line.alignment = LineAlignment.TransformZ;
            line.sharedMaterial = OutlineMaterial();
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        static Material OutlineMaterial()
        {
            string path = ImportedPath + "/Materials/VRTargetOutline.mat";
            var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                UnityEditor.AssetDatabase.CreateAsset(material, path);
            }
            material.color = Color.white;
            return material;
        }

        static Mesh Disc(float radius, int segments)
        {
            var mesh = new Mesh { name = "Disc" };
            var vertices = new List<Vector3> { Vector3.zero };
            var triangles = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
                triangles.AddRange(new[] { 0, 1 + (i + 1) % segments, 1 + i });
            }
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(Enumerable.Repeat(Vector3.back, vertices.Count).ToList());
            mesh.RecalculateBounds();
            return mesh;
        }

        static void ValidateVRScene(Transform root, ElevatorVRLesson lesson)
        {
            if (root.GetComponentInChildren<XROrigin>() == null || lesson.eye.stereoTargetEye != StereoTargetEyeMask.Both || lesson.eye.rect.width != 1f)
                throw new InvalidOperationException("A full-viewport head-tracked stereo XR Origin is required.");
            if (root.GetComponentsInChildren<Canvas>(true).Any(canvas => canvas.renderMode != RenderMode.WorldSpace))
                throw new InvalidOperationException("Screen-space lesson UI is not allowed in the VR scene.");
            foreach (Act action in Enum.GetValues(typeof(Act)).Cast<Act>().Where(a => a != Act.Landmark))
                if (!lesson.targets.Any(target => target.action == action && target.GetComponent<Collider>() != null))
                    throw new InvalidOperationException("Physical VR action missing: " + action);
            for (int floor = 0; floor <= LiftLogic.MaxFloor; floor++)
                if (lesson.car.floorButtons[floor] == null) throw new InvalidOperationException("Floor button missing: " + floor);
        }

        static void CaptureVRViews(Camera camera, LiftCar car, string folder)
        {
            Directory.CreateDirectory(folder);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var rotation = camera.transform.rotation;
            var position = camera.transform.position;
            var target = new RenderTexture(1600, 1000, 24);
            var views = new (string name, int floor, Vector3 euler, Vector3? at)[]
            {
                ("forward_floor01", 1, new Vector3(0f, 0f, 0f), null),
                ("forward_floor06", 6, new Vector3(0f, 0f, 0f), null),
                ("forward_floor12", 12, new Vector3(8f, 0f, 0f), null),
                ("panel", 1, new Vector3(5f, 60f, 0f), null),
                ("panel_closeup", 1, new Vector3(10f, 90f, 0f), new Vector3(0.45f, 1.45f, 0.36f)),
                ("lesson_board", 1, new Vector3(5f, -62f, 0f), null),
            };
            try
            {
                camera.targetTexture = target;
                foreach (var view in views)
                {
                    car.Logic.Reset(view.floor);
                    if (view.name.StartsWith("panel")) foreach (int lit in new[] { 4, 9, 12 }) car.Logic.PressFloor(lit, 0d);
                    car.ApplyWorld();
                    foreach (var button in car.GetComponentsInChildren<LiftButton>()) button.Animate(1f);
                    camera.transform.rotation = Quaternion.Euler(view.euler);
                    camera.transform.position = view.at ?? position;
                    foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.ForceMeshUpdate();
                    float fov = camera.fieldOfView;
                    if (view.name == "signs_zoom") camera.fieldOfView = 34f;
                    camera.Render();
                    camera.fieldOfView = fov;
                    RenderTexture.active = target;
                    var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Path.Combine(folder, view.name + ".png"), image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
            finally
            {
                car.Logic.Reset(car.startFloor);
                car.ApplyWorld();
                camera.transform.rotation = rotation;
                camera.transform.position = position;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
