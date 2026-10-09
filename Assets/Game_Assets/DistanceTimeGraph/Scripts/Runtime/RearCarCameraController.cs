using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DistanceTimeGraph
{
    /// The BEHIND-CAR CAMERA: a second camera that stays behind and slightly above whichever car is being watched (the
    /// preview car while editing, the real car during runs and replays) and renders into the monitor's Render Texture.
    /// It keeps facing away from the start, so it stays behind the car while the car reverses.
    [ExecuteAlways]   // also follows in the Editor, so the monitor shows the car before Play
    public class RearCarCameraController : MonoBehaviour
    {
        public Camera cam;
        public Transform target;
        public Vector3 offset = new Vector3(0f, 2.3f, -8.8f);   // from the front bumper: about 4 m behind the tail
        public float lookAhead = 7f;
        public Image liveDot;
        public TextMeshProUGUI modeText;
        public TextMeshProUGUI distanceText;

        void Awake()
        {
            if (cam == null) return;
            // A plain camera into the monitor texture. If XR rendering were allowed, URP would draw it from the headset's
            // pose, and the monitor would show the learner's own view instead of the car.
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data != null) data.allowXRRendering = false;
        }

        public void Follow(Transform car, string mode)
        {
            if (car != target) snap = true;
            target = car;
            if (modeText != null) modeText.text = mode;
        }

        bool snap = true;

        /// Places the camera behind its target immediately (scene building and validation pictures).
        public void SnapNow()
        {
            snap = true;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (target == null || cam == null) return;
            var fwd = target.forward;
            Vector3 want = target.position + fwd * offset.z + Vector3.up * offset.y;
            cam.transform.position = snap ? want : Vector3.Lerp(cam.transform.position, want, 1f - Mathf.Exp(-12f * Time.deltaTime));
            cam.transform.rotation = Quaternion.LookRotation(target.position + fwd * (lookAhead - 2.3f) + Vector3.up * 0.4f - cam.transform.position, Vector3.up);
            snap = false;
            if (!Application.isPlaying) return;
            if (liveDot != null) liveDot.color = Palette.Red.WithAlpha(0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 3f)));
            var car = target.GetComponent<CarMotionController>();
            if (distanceText != null && car != null) distanceText.text = $"{car.Distance:0.0} m from start";
        }
    }
}
