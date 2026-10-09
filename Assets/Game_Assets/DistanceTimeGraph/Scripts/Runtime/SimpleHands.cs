using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace DistanceTimeGraph
{
    /// White low-poly gloves in blue sleeves instead of the controller models. With a headset they follow the
    /// controllers. Without one (the XR Device Simulator starts its controllers inside the head), or while a controller
    /// sits too close to the eyes, they rest in the lower corners of the view: the left hand pointing at the road, the
    /// right hand at the graph. The pointer rays are kept.
    public class SimpleHands : MonoBehaviour
    {
        public Transform head;
        public Transform[] controllers;
        public Transform[] gloves;
        [Tooltip("Rest poses in head space: position, and the point the index finger aims at.")]
        public Vector3[] restPosition = { new Vector3(-0.2f, -0.3f, 0.36f), new Vector3(0.24f, -0.31f, 0.38f) };
        public Vector3[] restAim = { new Vector3(0.02f, -0.1f, 1.2f), new Vector3(0.4f, -0.14f, 1.05f) };
        public float minTrackedDistance = 0.22f;

        IEnumerator Start()
        {
            HideModels();
            yield return new WaitForSeconds(1f);   // controller models can be spawned a moment after start
            HideModels();
        }

        bool headset;
        float nextCheck;

        void LateUpdate()
        {
            if (Time.time >= nextCheck) { headset = DesktopView.HeadsetRunning(); nextCheck = Time.time + 1f; }
            Pose(!headset);
        }

        /// Puts each glove on its controller, or in its rest pose when the controller is not usable.
        public void Pose(bool forceRest)
        {
            if (head == null || gloves == null) return;
            for (int i = 0; i < gloves.Length && i < controllers.Length; i++)
            {
                var g = gloves[i];
                if (g == null) continue;
                bool tracked = !forceRest && controllers[i] != null && Vector3.Distance(controllers[i].position, head.position) > minTrackedDistance;
                if (tracked)
                {
                    g.localPosition = Vector3.zero;
                    g.localRotation = Quaternion.identity;
                }
                else
                {
                    var p = head.TransformPoint(restPosition[i]);
                    var aim = head.TransformPoint(restAim[i]);
                    g.SetPositionAndRotation(p, Quaternion.LookRotation(aim - p, head.up));
                }
            }
        }

        void HideModels()
        {
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] == null) continue;
                foreach (var r in controllers[i].GetComponentsInChildren<Renderer>(true))
                {
                    if (r is LineRenderer) continue;
                    if (gloves != null && i < gloves.Length && gloves[i] != null && r.transform.IsChildOf(gloves[i])) continue;
                    if (r.GetComponentInParent<Canvas>() != null) continue;
                    r.enabled = false;
                }
            }
        }
    }

    /// On a desktop (XR Device Simulator, no headset) the view starts tilted down the way a learner naturally looks at
    /// the road and the panels. With a real headset the head tracking decides and the tilt is removed.
    public static class DesktopView
    {
        public static bool HeadsetRunning()
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            return displays.Exists(d => d.running);
        }
    }
}
