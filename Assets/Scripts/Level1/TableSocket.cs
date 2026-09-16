using UnityEngine;

namespace NumberLinePlayground.Level1
{
    /// One marked spot on the table, stamped with the position a sphere must carry to fit it.
    public class TableSocket : MonoBehaviour
    {
        public int expectedValue;            // the stamp: the number a sphere must carry to fit here
        public Transform anchor;             // where a placed sphere sits
        public TextMesh stamp;               // the number painted inside the ring
        public Renderer[] ring;              // the glowing ring segments
        public Material idleMaterial;
        public Material hotMaterial;

        [System.NonSerialized] public FetchSphere occupant;

        bool hot;

        public bool IsFilled => occupant != null;

        public const float RestHeight = 0.06f;      // a spot lies just above the surface

        /// Puts this spot at a place on the tilted table surface, stamped with the number it takes. A lift raises it
        /// above the surface, on a post.
        public void SetSpot(int value, Vector2 surfaceXZ, float lift = 0f, bool labelled = true)
        {
            expectedValue = value;
            transform.localPosition = new Vector3(surfaceXZ.x, RestHeight + lift, surfaceXZ.y);
            if (stamp != null)
            {
                // A spot without a label shows a question mark: which sphere goes here is for the player to work out.
                stamp.text = labelled ? Concept.Signed(value) : "?";
                stamp.color = !labelled ? new Color(0.8f, 0.92f, 0.98f)
                            : value < 0 ? new Color(1f, 0.62f, 0.68f) : new Color(0.72f, 0.84f, 1f);
            }
            Clear();
        }

        /// Brightens the ring while a carried sphere is close enough to snap into it.
        public void SetHot(bool value)
        {
            if (hot == value) return;
            hot = value;
            if (ring == null) return;
            foreach (var segment in ring)
                if (segment != null) segment.sharedMaterial = value ? hotMaterial : idleMaterial;
        }

        public void Clear()
        {
            occupant = null;
            SetHot(false);
        }
    }
}
