using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DistanceTimeGraph.EditorTools
{
    public static partial class DtgBuilder
    {
        const string CarPrefab = "Assets/ARCADE - FREE Racing Car/Prefabs (Meshes Only)/Free Racing Car.prefab";
        public const int UiLayer = 5;

        // ---------------- cars ----------------

        /// The racing car, a transparent preview copy, or a ghost copy. The car's distance point is its front bumper.
        static CarMotionController BuildCar(Transform track, string name, Material body, Material lights, bool real)
        {
            var root = new GameObject(name).transform;
            root.SetParent(track, false);
            var ctl = root.gameObject.AddComponent<CarMotionController>();
            ctl.trackOrigin = track;

            var model = Prefab(CarPrefab, root, root.position, root.rotation);
            if (model == null)
            {
                // Fallback so the scene still works without the car pack.
                model = new GameObject("BoxCar");
                model.transform.SetParent(root, false);
                Box(model.transform, "Body", new Vector3(0, 0.7f, -2.3f), new Vector3(2f, 0.8f, 4.6f), body);
            }
            model.name = "Model";
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = i == 0 ? body : lights;
                r.sharedMaterials = mats;
                r.shadowCastingMode = real ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
            // Front bumper at the root, body behind it.
            var b = Bounds(model);
            model.transform.localPosition = new Vector3(0f, -b.min.y + root.position.y, -(b.max.z - root.position.z));
            b = Bounds(model);
            Vector3 L(Vector3 world) => root.InverseTransformPoint(world);
            float tailZ = L(b.min).z, width = b.size.x, height = b.size.y;
            ctl.length = b.size.z;

            ctl.wheels = new[] { "FrontLeftWheel", "FrontRightWheel", "RearLeftWheel", "RearRightWheel" }
                .Select(n => Find(model.transform, n)).Where(t => t != null).ToArray();
            if (ctl.wheels.Length > 0)
            {
                var wb = ctl.wheels[0].GetComponent<Renderer>();
                if (wb != null) ctl.wheelRadius = Mathf.Max(0.2f, wb.bounds.extents.y);
            }

            // Reverse lights: two bright lamps on the tail, only on while the car moves backwards.
            var rev = new GameObject("ReverseLights").transform;
            rev.SetParent(root, false);
            foreach (float x in new[] { -0.3f, 0.3f })
            {
                Box(rev, "Lamp", new Vector3(x * width, height * 0.5f, tailZ - 0.03f), new Vector3(width * 0.2f, height * 0.12f, 0.06f), real ? Mats.ReverseLight : Mats.ReverseLight);
                if (real) Box(rev, "Halo", new Vector3(x * width, height * 0.5f, tailZ - 0.07f), new Vector3(width * 0.3f, height * 0.2f, 0.02f), Mats.BridgeUnsafe);
            }
            rev.gameObject.SetActive(false);
            ctl.reverseLights = rev.gameObject;

            // Backward arrows on the road behind the car while it reverses.
            var arrows = new GameObject("BackArrows").transform;
            arrows.SetParent(root, false);
            for (int i = 0; i < 3; i++) Chevron(arrows, "Chevron_" + i, new Vector3(0, 0, -i * 1.3f), 0.9f, Mats.Cyan);
            arrows.gameObject.SetActive(false);
            ctl.backArrows = arrows;

            if (real)
            {
                ctl.statusTag = WorldText(root, "StatusTag", "CAR STOPPED", 0.55f, Palette.Yellow, new Vector3(0, height + 1.1f, -ctl.length * 0.5f));
                ctl.showStatus = true;
            }
            else ctl.showStatus = false;
            return ctl;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var f = Find(c, name); if (f != null) return f; }
            return null;
        }

        static LineRenderer BuildLinkLine(Transform parent)
        {
            var go = new GameObject("PreviewLink", typeof(LineRenderer));
            go.transform.SetParent(parent, false);
            var lr = go.GetComponent<LineRenderer>();
            lr.sharedMaterial = Mats.Link;
            lr.widthMultiplier = 0.012f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.enabled = false;
            return lr;
        }

        // ---------------- XR player ----------------

        static GameObject BuildPlayer(Transform root, string rigPath, out Transform head, out Transform[] controllers)
        {
            var rig = Prefab(rigPath, root, PlayerFeet, Quaternion.identity);
            rig.name = "XR Origin (Player)";
            head = Find(rig.transform, "Main Camera");
            var cam = head.GetComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 700f;
            cam.fieldOfView = 60f;   // desktop view only; a headset sets its own
            cam.clearFlags = CameraClearFlags.Skybox;
            // The learner stays on the platform: no walking, turning or teleporting, so nothing moves the view but the head.
            var loco = Find(rig.transform, "Locomotion");
            if (loco != null) loco.gameObject.SetActive(false);
            foreach (var mb in rig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                var t = mb.GetType();
                if ((t.Namespace != null && t.Namespace.Contains("Affordance")) || t.Name.Contains("Affordance")) mb.enabled = false;
            }
            controllers = new[] { Find(rig.transform, "Left Controller"), Find(rig.transform, "Right Controller") };
            // Rays drawn with a URP material (the rig's default line material is a built-in legacy shader).
            foreach (var lr in rig.GetComponentsInChildren<LineRenderer>(true)) lr.sharedMaterial = Mats.Ray;
            // Desktop view: start looking 15 degrees down at the road, like the reference; removed on a headset.
            var offset = Find(rig.transform, "Camera Offset");
            if (offset != null)
            {
                offset.localRotation = Quaternion.Euler(GazePitch, 0f, 0f);
                var pitch = rig.AddComponent<DesktopViewPitch>();
                pitch.cameraOffset = offset;
                pitch.pitch = GazePitch;
            }

            string simPath = FindPrefab("XR Device Simulator");
            if (simPath != null)
            {
                var sim = Prefab(simPath, root, Vector3.zero, Quaternion.identity);
                foreach (var mb in sim.GetComponents<MonoBehaviour>())
                {
                    if (mb == null) continue;
                    var so = new SerializedObject(mb);
                    var ui = so.FindProperty("m_DeviceSimulatorUI");
                    if (ui != null) { ui.objectReferenceValue = null; so.ApplyModifiedPropertiesWithoutUndo(); }
                }
                foreach (var mb in sim.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null && mb.GetType().Name == "XRDeviceSimulatorUI") mb.enabled = false;
                foreach (var c in sim.GetComponentsInChildren<Canvas>(true)) c.gameObject.SetActive(false);
            }
            return rig;
        }

        /// A cartoon white glove with a pointing index finger and a long blue sleeve; +Z is where the finger points.
        static Transform BuildGlove(Transform controller, bool left)
        {
            var g = new GameObject("Glove").transform;
            g.SetParent(controller, false);
            float s = left ? -1f : 1f;
            Prim(PrimitiveType.Cylinder, g, "Sleeve", new Vector3(0, -0.01f, -0.2f), new Vector3(0.1f, 0.13f, 0.1f), Mats.Sleeve, Quaternion.Euler(90, 0, 0));
            Prim(PrimitiveType.Cylinder, g, "Cuff", new Vector3(0, -0.005f, -0.065f), new Vector3(0.105f, 0.02f, 0.105f), Mats.Glove, Quaternion.Euler(90, 0, 0));
            Prim(PrimitiveType.Sphere, g, "Palm", new Vector3(0, 0, 0f), new Vector3(0.1f, 0.055f, 0.11f), Mats.Glove);
            Prim(PrimitiveType.Capsule, g, "Index", new Vector3(0.018f * s, 0.008f, 0.085f), new Vector3(0.026f, 0.045f, 0.026f), Mats.Glove, Quaternion.Euler(90, 0, 0));
            for (int f = 0; f < 3; f++)   // curled middle, ring and little fingers
                Prim(PrimitiveType.Sphere, g, "Knuckle", new Vector3((-0.008f - f * 0.022f) * s, -0.012f, 0.05f), new Vector3(0.026f, 0.028f, 0.034f), Mats.Glove);
            Prim(PrimitiveType.Capsule, g, "Thumb", new Vector3(0.048f * s, -0.006f, 0.03f), new Vector3(0.024f, 0.03f, 0.024f), Mats.Glove, Quaternion.Euler(70, 40f * s, 0));
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }

        // ---------------- behind-car camera ----------------

        static RearCarCameraController BuildRearCamera(Transform root, UiRefs ui)
        {
            var go = new GameObject("BehindCarCamera");
            go.transform.SetParent(root, false);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 450f;
            cam.depth = -2;
            cam.stereoTargetEye = StereoTargetEyeMask.None;   // a plain camera into the monitor, never an XR eye
            cam.cullingMask = ~(1 << UiLayer);   // never the world-space interface
            string rtPath = GenFolder + "/DTG_RearCamera.renderTexture";
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            if (rt == null)
            {
                rt = new RenderTexture(640, 310, 24) { name = "DTG_RearCamera", antiAliasing = 2 };
                AssetDatabase.CreateAsset(rt, rtPath);
            }
            if (rt.width != 640 || rt.height != 310)
            {
                rt.Release();
                rt.width = 640;
                rt.height = 310;
                EditorUtility.SetDirty(rt);
            }
            cam.targetTexture = rt;
            var data = go.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderShadows = false;
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.allowXRRendering = false;   // otherwise URP renders it from the headset pose in play mode
            ui.monitorImage.texture = rt;

            var ctl = go.AddComponent<RearCarCameraController>();
            ctl.cam = cam;
            ctl.liveDot = ui.liveDot;
            ctl.modeText = ui.monitorMode;
            ctl.distanceText = ui.monitorDistance;
            return ctl;
        }

        static DimVeil BuildVeil(Transform head)
        {
            var go = Prim(PrimitiveType.Sphere, head, "DimVeil", Vector3.zero, Vector3.one * 3.6f, Mats.Veil);
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.enabled = false;
            go.layer = UiLayer;
            var veil = go.AddComponent<DimVeil>();
            veil.veil = r;
            return veil;
        }
    }
}
