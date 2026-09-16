using UnityEngine;

namespace NumberLinePlayground.Level1
{
    /// The numbers painted on the ground lie flat, so walking along the line reads them sideways. This turns them in
    /// quarter turns so they read the right way up for the way you are facing, and only when you turn a good way past a
    /// quarter, so they do not flicker back and forth around a boundary.
    public class GroundLabels : MonoBehaviour
    {
        public Transform head;
        public Transform[] labels;

        float yaw;                                   // the current quarter turn, in this object's own frame

        void LateUpdate()
        {
            if (head == null || labels == null) return;
            Vector3 looking = transform.InverseTransformDirection(head.forward);
            looking.y = 0f;
            if (looking.sqrMagnitude < 0.04f) return;                    // looking straight up or down: leave them
            float heading = Mathf.Atan2(looking.x, looking.z) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(heading, yaw)) <= 65f) return;
            yaw = Mathf.Round(heading / 90f) * 90f;
            var turned = Quaternion.Euler(90f, yaw, 0f);                 // flat on the ground, top of the digits away from you
            foreach (var label in labels)
                if (label != null) label.localRotation = turned;
        }
    }
}
