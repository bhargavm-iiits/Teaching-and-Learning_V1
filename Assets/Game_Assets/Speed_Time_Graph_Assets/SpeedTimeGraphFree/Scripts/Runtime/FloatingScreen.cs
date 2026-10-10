using UnityEngine;
using UnityEngine.XR;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Floating VR screen: hovers gently in front of the deck. With a headset, if the learner turns more than
    /// 55 degrees away from it for a second, it glides round the head to sit in front of them again (same distance,
    /// height and tilt). On the desktop it stays where the builder placed it, keeping the 16:9 composition.
    /// </summary>
    public class FloatingScreen : MonoBehaviour
    {
        public Transform head;
        public float authoredYaw;                  // head yaw the screen was placed for (deck forward)
        public float bobAmplitude = .012f, followAngle = 55, followSpeed = 1.8f;
        Vector3 pos0, offset0; Quaternion rot0; float currentYaw, targetYaw, away; bool init;

        void Init()
        {
            init = true; pos0 = transform.position; rot0 = transform.rotation;
            offset0 = head ? pos0 - head.position : Vector3.zero;
            currentYaw = targetYaw = authoredYaw;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (!init) Init();
            if (XRSettings.isDeviceActive && head)
            {
                float headYaw = head.eulerAngles.y;
                if (Mathf.Abs(Mathf.DeltaAngle(headYaw, currentYaw)) > followAngle) { away += Time.deltaTime; if (away > 1f) targetYaw = headYaw; }
                else away = 0;
                currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * followSpeed);
            }
            var turn = Quaternion.Euler(0, Mathf.DeltaAngle(authoredYaw, currentYaw), 0);
            var centre = head && XRSettings.isDeviceActive ? head.position : pos0 - offset0;
            var p = centre + turn * offset0 + Vector3.up * Mathf.Sin(Time.time * 1.1f) * bobAmplitude;
            transform.SetPositionAndRotation(p, turn * rot0);
        }
    }
}