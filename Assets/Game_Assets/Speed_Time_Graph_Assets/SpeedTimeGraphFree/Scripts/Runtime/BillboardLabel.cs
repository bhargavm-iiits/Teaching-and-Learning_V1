using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>Faces the eye; optionally keeps a constant angular size (vehicle tags about 2 degrees tall).</summary>
    [ExecuteAlways]
    public class BillboardLabel : MonoBehaviour
    {
        public Camera eye;
        public bool constantAngularSize;
        public float degrees = 2f;            // apparent height of one text line
        public float lineHeightAtScale1 = 1f; // world height of one line at localScale 1
        public bool yawOnly;
        void LateUpdate()
        {
            var cam = eye ? eye : Camera.main; if (!cam) return;
            Vector3 d = transform.position - cam.transform.position;
            if (yawOnly) d.y = 0;
            if (d.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(d, Vector3.up);
            if (constantAngularSize)
            {
                float worldHeight = 2 * Mathf.Tan(degrees * .5f * Mathf.Deg2Rad) * d.magnitude;
                float s = worldHeight / Mathf.Max(1e-4f, lineHeightAtScale1);
                var parent = transform.parent ? transform.parent.lossyScale.x : 1;
                transform.localScale = Vector3.one * s / parent;
            }
        }
    }
}