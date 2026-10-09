using TMPro;
using UnityEngine;

namespace DistanceTimeGraph
{
    public static class Dtg
    {
        public static void Log(string message) => Debug.Log("[DTG] " + message);
    }

    /// Puts a car on the straight track at a distance from the start. The car always faces away from the start: a
    /// negative velocity drives it backwards (wheels spin back, reverse lights on, backward arrows on the road) without
    /// turning it around. Used by the real car, the transparent preview car and the ghost cars.
    public class CarMotionController : MonoBehaviour
    {
        [Header("Track")]
        public Transform trackOrigin;          // world position of 0 m; the track runs along its +Z
        public float wheelRadius = 0.37f;
        [Tooltip("The car's position is its front bumper; the body lies this far behind it.")]
        public float length = 4.6f;

        [Header("Parts")]
        public Transform[] wheels;
        public GameObject reverseLights;
        public GameObject brakeLights;
        public Transform backArrows;           // chevrons on the road behind the car, shown while reversing
        public TextMeshPro statusTag;          // "MOVING AWAY" / "STOPPED" / "MOVING BACK"
        public bool showStatus = true;

        public float Distance { get; private set; }
        public float Velocity { get; private set; }
        public bool Reversing => Velocity < -0.05f;
        public Vector3 Center => transform.position - transform.forward * (length * 0.5f);

        float lastDistance;
        float stoppedFor;

        public Vector3 WorldAt(float distance)
        {
            var o = trackOrigin != null ? trackOrigin : transform.parent;
            return o.position + o.forward * distance;
        }

        /// Places the car at `distance` metres from the start, moving at `velocity` m/s.
        public void Place(float distance, float velocity)
        {
            var o = trackOrigin != null ? trackOrigin : transform.parent;
            transform.SetPositionAndRotation(o.position + o.forward * distance, o.rotation);
            float moved = distance - lastDistance;
            lastDistance = distance;
            Distance = distance;
            Velocity = velocity;
            if (wheels != null && Mathf.Abs(moved) > 1e-5f && Mathf.Abs(moved) < 20f)
            {
                float deg = moved / wheelRadius * Mathf.Rad2Deg;
                foreach (var w in wheels) if (w != null) w.Rotate(Vector3.right, deg, Space.Self);
            }
            stoppedFor = Mathf.Abs(velocity) < 0.05f ? stoppedFor + Time.deltaTime : 0f;
            ShowMotion(velocity);
        }

        /// Jumps without wheel spin (resets, new courses).
        public void Teleport(float distance)
        {
            lastDistance = distance;
            Place(distance, 0f);
        }

        public void ShowMotion(float velocity)
        {
            bool back = velocity < -0.05f;
            if (reverseLights != null && reverseLights.activeSelf != back) reverseLights.SetActive(back);
            if (brakeLights != null)
            {
                bool brake = Mathf.Abs(velocity) < 0.05f;
                if (brakeLights.activeSelf != brake) brakeLights.SetActive(brake);
            }
            if (backArrows != null)
            {
                if (backArrows.gameObject.activeSelf != back) backArrows.gameObject.SetActive(back);
                if (back)
                {
                    // Chevrons slide backwards along the road so the direction reads at a glance.
                    float phase = Mathf.Repeat(Time.time * 1.6f, 1f);
                    backArrows.localPosition = new Vector3(0f, 0.03f, -length - 0.8f - phase * 1.2f);
                }
            }
            if (statusTag != null)
            {
                bool on = showStatus && Mathf.Abs(velocity) > 0.05f;
                if (statusTag.gameObject.activeSelf != on) statusTag.gameObject.SetActive(on);
                if (on)
                {
                    statusTag.text = back ? "CAR MOVING BACK" : velocity > 0.05f ? "CAR MOVING AWAY" : "CAR STOPPED";
                    statusTag.color = back ? Palette.Orange : velocity > 0.05f ? Palette.Cyan : Palette.Yellow;
                    var cam = Camera.main;
                    if (cam != null) statusTag.transform.rotation = Quaternion.LookRotation(statusTag.transform.position - cam.transform.position);
                }
            }
        }
    }
}
