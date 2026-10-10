using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Object = UnityEngine.Object;

namespace SpeedTimeGraphFree
{
    /// <summary>Viewing deck (balcony view that matches the reference), XR Origin, XR Device Simulator, EventSystem.</summary>
    public static partial class SpeedTimeFreeBuilder
    {
        static Transform deck;

        static void Rig()
        {
            // Deck: small platform with glass rails, no forced movement. Floor at eye height - 1.6.
            deck = Group("Viewing deck", root).transform;
            deck.position = new Vector3(Eye.x, Eye.y - EyeHeight, Eye.z); deck.rotation = Quaternion.Euler(0, EyeYaw, 0);
            var wood = SpeedTimeFreeMaterials.Lit("Deck boards", new Color(.55f, .42f, .3f), .2f);
            var glass = SpeedTimeFreeMaterials.Transparent("Deck glass", new Color(.6f, .85f, 1f, .07f));
            var steel = SpeedTimeFreeMaterials.Lit("Deck steel", new Color(.7f, .74f, .78f), .6f, .6f);
            Box("Deck floor", deck, new Vector3(0, -.12f, -.6f), new Vector3(3.6f, .24f, 2.6f), wood);
            Box("Glass front rail", deck, new Vector3(0, .4f, .75f), new Vector3(3.6f, .8f, .03f), glass, false);
            foreach (float s in new[] { -1f, 1f }) Box("Glass side rail", deck, new Vector3(s * 1.8f, .5f, -.6f), new Vector3(.03f, 1.0f, 2.6f), glass, false);
            Box("Deck tower", deck, new Vector3(0, -(Eye.y - EyeHeight) * .5f - .2f, -1.2f), new Vector3(1.4f, Eye.y - EyeHeight, 1.4f), steel);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            rig.name = "XR Origin (XR Rig) · deck";
            rig.transform.position = deck.position; rig.transform.rotation = deck.rotation;
            var origin = rig.GetComponent<XROrigin>();
            origin.CameraYOffset = EyeHeight;
            eye = rig.GetComponentInChildren<Camera>(true);
            eye.tag = "MainCamera"; eye.fieldOfView = Fov; eye.nearClipPlane = .03f; eye.farClipPlane = 1500;
            eye.clearFlags = CameraClearFlags.Skybox;
            eye.transform.parent.localPosition = new Vector3(0, EyeHeight, 0);
            eye.transform.localPosition = Vector3.zero; eye.transform.localRotation = Quaternion.Euler(EyePitch, 0, 0);
            var data = eye.GetComponent<UniversalAdditionalCameraData>() ?? eye.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.allowXRRendering = true; data.renderPostProcessing = false; data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            // No forced movement: locomotion providers are disabled (the learner stays on the deck).
            foreach (var mb in rig.GetComponentsInChildren<LocomotionProvider>(true)) mb.enabled = false;
            foreach (var ray in rig.GetComponentsInChildren<XRRayInteractor>(true)) { ray.enableUIInteraction = true; ray.maxRaycastDistance = 30; }
            foreach (var nf in rig.GetComponentsInChildren<NearFarInteractor>(true)) nf.enableUIInteraction = true;
            foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterials.Any(m => m && (m.shader.name == "Standard" || m.shader.name.Contains("Error")))) r.sharedMaterial = SpeedTimeFreeMaterials.Unlit("Holo cyan", new Color(.16f, .86f, 1f));

            var fallback = rig.AddComponent<DesktopFallback>(); fallback.eye = eye; fallback.pitch = EyePitch; fallback.yaw = 0;
            var deckInput = rig.AddComponent<PlayerDeck>();

            // XR Device Simulator for desktop testing (inactive by default; enable it to test without a headset).
            var simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/XR Device Simulator/XR Device Simulator.prefab");
            if (simPrefab) { var sim = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab, root); sim.name = "XR Device Simulator (enable for testing)"; sim.SetActive(false); }

            if (!Object.FindAnyObjectByType<XRInteractionManager>()) Group("XR Interaction Manager", root).AddComponent<XRInteractionManager>();
            var es = Group("EventSystem (XR + mouse)", root); es.AddComponent<EventSystem>();
            var module = es.AddComponent<XRUIInputModule>(); module.enableXRInput = true; module.enableMouseInput = true; module.enableTouchInput = false;
            rigDeckInput = deckInput; rigFallback = fallback;
        }
        static PlayerDeck rigDeckInput; static DesktopFallback rigFallback;

        /// <summary>World pose of a panel that covers a normalised screen rect (x0,y0 top-left .. x1,y1) of the desktop view at a distance.</summary>
        public static (Vector3 pos, Quaternion rot, float width, float height) ScreenRect(float x0, float y0, float x1, float y1, float distance, float faceEye = 0)
        {
            var camPos = Eye; var camRot = Quaternion.Euler(EyePitch, EyeYaw, 0);
            float tanV = Mathf.Tan(Fov * .5f * Mathf.Deg2Rad), tanH = tanV * 16f / 9f;
            Vector3 Dir(float u, float v) => camRot * new Vector3((u * 2 - 1) * tanH, (1 - v * 2) * tanV, 1);
            var centreDir = Dir((x0 + x1) * .5f, (y0 + y1) * .5f).normalized;
            var pos = camPos + centreDir * distance;
            // Mostly parallel to the image plane (flat overlay look of the reference), turned 35% toward the eye for VR readability.
            // Turn toward the eye only about the camera's vertical axis (no roll), by a fraction of the horizontal angle.
            var local = Quaternion.Inverse(camRot) * centreDir;
            float yawToCentre = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            var rotation = camRot * Quaternion.Euler(0, yawToCentre * faceEye, 0);
            var plane = new Plane(-(rotation * Vector3.forward), pos);
            Vector3 Hit(float u, float v) { var r = new Ray(camPos, Dir(u, v)); plane.Raycast(r, out float d); return r.GetPoint(d); }
            var right = rotation * Vector3.right; var up = rotation * Vector3.up;
            float width = Mathf.Abs(Vector3.Dot(Hit(x1, (y0 + y1) * .5f) - Hit(x0, (y0 + y1) * .5f), right));
            float height = Mathf.Abs(Vector3.Dot(Hit((x0 + x1) * .5f, y0) - Hit((x0 + x1) * .5f, y1), up));
            // On a tilted plane the rect centre is not where the centre ray hits: re-centre on the projected edges.
            var hl = Hit(x0, (y0 + y1) * .5f); var hr = Hit(x1, (y0 + y1) * .5f); var ht = Hit((x0 + x1) * .5f, y0); var hb = Hit((x0 + x1) * .5f, y1);
            var centre = pos + right * Vector3.Dot((hl + hr) * .5f - pos, right) + up * Vector3.Dot((ht + hb) * .5f - pos, up);
            return (centre, rotation, width, height);
        }
    }
}