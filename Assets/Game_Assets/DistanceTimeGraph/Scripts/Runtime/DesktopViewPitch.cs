using System.Collections;
using UnityEngine;

namespace DistanceTimeGraph
{
    /// Tilts the rig's camera offset down by `pitch` degrees when no headset is running (Editor play-testing with the
    /// XR Device Simulator), so the first view matches the design: the road ahead with the car in the middle, the graph
    /// on the right and the monitor above it. With a headset the tilt is removed and the learner's own head decides.
    public class DesktopViewPitch : MonoBehaviour
    {
        public Transform cameraOffset;
        public float pitch = 15f;

        IEnumerator Start()
        {
            yield return null;   // XR loaders start during the first frame
            Apply();
            yield return new WaitForSeconds(1f);
            Apply();
        }

        void Apply()
        {
            if (cameraOffset == null) return;
            var e = cameraOffset.localEulerAngles;
            e.x = DesktopView.HeadsetRunning() ? 0f : pitch;
            cameraOffset.localEulerAngles = e;
        }
    }
}
