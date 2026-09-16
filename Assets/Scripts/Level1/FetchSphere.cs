using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace NumberLinePlayground.Level1
{
    /// A collectible sphere waiting on the number line at a whole-number position.
    /// The controller decides when it may be grabbed; this component only knows its value, its home,
    /// how to glow (locked / normal / highlighted) and how to glide back or snap into a socket.
    [RequireComponent(typeof(XRGrabInteractable))]
    public class FetchSphere : MonoBehaviour
    {
        public int value;                    // signed position on the number line, for example -4 or +7
        public Vector3 homeLocalPosition;    // where it waits, local to its parent (the playground root)
        public Renderer body;                // the visible ball
        public Color color = Color.white;    // its base colour: coral for negative, blue for positive

        // Given a new number for each level (see SetHome): its tag, and the beam and ring that mark its place on the line.
        public TextMesh tagText;
        public Renderer tagBacking;
        public Transform marker;
        public Renderer[] markerParts;
        public Transform pillar;             // a tall beam with the number on top, shown when a level asks for it
        public TextMesh pillarNumber;
        public Material negativeMarker, positiveMarker, negativeTag, positiveTag;
        public Color negativeColor = new Color(1f, 0.32f, 0.42f);
        public Color positiveColor = new Color(0.3f, 0.6f, 1f);

        /// A sphere on the table is smaller than one waiting on the line, so it sits inside its ring instead of hiding it.
        public const float PlacedScale = 0.65f;
        const float Radius = 0.3f;                                   // of the ball at full size
        /// How far the centre of a placed sphere sits below the socket's anchor, which is one full-size radius up.
        public static float PlacedDrop => Radius * (1f - PlacedScale);

        // The floating tag is as built (1) while the sphere waits on the line, and small in the hand and on the table, where
        // a full-size one hangs across the view.
        public float heldTagSize = 0.3f;
        public float placedTagSize = 0.3f;
        public Transform viewer;             // the player's head: a tag waiting on the line shrinks as you walk up to it

        [System.NonSerialized] public bool placed;

        /// Raised when a glide that asked to announce itself has landed (a sphere sent back after a wrong pair).
        public event System.Action<FetchSphere> Landed;

        XRGrabInteractable grab;
        Rigidbody physics;
        Transform follow;                    // while carried with the keyboard: the point on the player it rides at
        float targetScale = 1f;
        Transform homeParent;
        MaterialPropertyBlock block;
        Coroutine motion;
        float glow = 1f;
        float bobPhase;
        Transform tagRoot;
        float tagSize = 1f;

        public XRGrabInteractable Grab => grab != null ? grab : (grab = GetComponent<XRGrabInteractable>());
        public bool IsMoving => motion != null;
        public bool IsCarried => follow != null;

        public Vector3 HomeWorld
        {
            get
            {
                CaptureHome();
                return homeParent != null ? homeParent.TransformPoint(homeLocalPosition) : homeLocalPosition;
            }
        }

        /// Remembers the playground root the first time anything asks. The level controller resets the spheres in its own
        /// Awake, which may run before this one, so this cannot wait for Awake: a sphere without a remembered root was
        /// taken off the playground and put at world (-4, 0.9, 0), which is under the ground.
        void CaptureHome()
        {
            if (homeParent == null) homeParent = transform.parent;
        }

        void Awake()
        {
            CaptureHome();
            physics = GetComponent<Rigidbody>();
            block = new MaterialPropertyBlock();
            bobPhase = Random.value * 6.2832f;
            ApplyGlow();
        }

        void Update()
        {
            // Carried with the keyboard: it rides at the player's carry point, smaller so it does not fill the view.
            if (follow != null) transform.position = Vector3.Lerp(transform.position, follow.position, 1f - Mathf.Exp(-18f * Time.deltaTime));
            if (!Mathf.Approximately(transform.localScale.x, targetScale))
                transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.one * targetScale, 2.5f * Time.deltaTime);
            UpdateTag();
            if (follow != null) return;

            if (placed || IsMoving || Grab.isSelected) return;
            // Nothing here may fall: a sphere that is not in your hand is held in the air by us.
            if (physics != null && !physics.isKinematic) { physics.isKinematic = true; physics.useGravity = false; }
            // A gentle bob while it waits on the line, so the spheres read as alive.
            Vector3 p = HomeWorld;
            p.y += Mathf.Sin(Time.time * 1.6f + bobPhase) * 0.03f;
            transform.position = p;
        }

        /// Sizes the tag for what the sphere is doing: as built while it waits on the line, small in the hand and on the
        /// table (the tag is a child of the ball, so the ball's own scale is undone to keep the tag one size in the world),
        /// and sitting just above the ball whatever the ball's size.
        void UpdateTag()
        {
            if (tagRoot == null && tagBacking != null) tagRoot = tagBacking.transform.parent;
            if (tagRoot == null) return;
            bool held = follow != null || Grab.isSelected;
            // Waiting on the line the tag is a full metre wide so it can be read from the far end; up close that fills the
            // view, so it shrinks from 3 m in to 40% at about a metre.
            float waiting = viewer != null ? Mathf.Clamp(Vector3.Distance(viewer.position, transform.position) / 3f, 0.4f, 1f) : 1f;
            float want = held ? heldTagSize : placed ? placedTagSize : waiting;
            tagSize = Mathf.MoveTowards(tagSize, want, 5f * Time.deltaTime);
            bool visible = tagSize > 0.02f;
            if (tagRoot.gameObject.activeSelf != visible) tagRoot.gameObject.SetActive(visible);
            if (!visible) return;
            ApplyTagSize();
        }

        void ApplyTagSize()
        {
            float ball = Mathf.Max(0.05f, transform.lossyScale.x);
            tagRoot.localScale = Vector3.one * (tagSize / ball);
            float smallRise = (Radius * ball + 0.05f + 0.13f * tagSize) / ball;      // just clear of the top of the ball
            tagRoot.localPosition = new Vector3(0f, Mathf.Lerp(smallRise, 0.62f, Mathf.InverseLerp(0.4f, 1f, tagSize)), 0f);
        }

        /// Gives this sphere a new number: its waiting place on the line, the marker under it, its tag and its colours.
        /// The caller then puts it there with ResetToHome.
        public void SetHome(int newValue)
        {
            value = newValue;
            homeLocalPosition = new Vector3(newValue, homeLocalPosition.y, 0f);
            bool negative = newValue < 0;
            color = negative ? negativeColor : positiveColor;
            name = "Sphere_" + newValue;
            if (tagText != null) tagText.text = "x = " + Concept.Signed(newValue) + " m";
            if (pillarNumber != null)
            {
                pillarNumber.text = Concept.Signed(newValue);
                pillarNumber.color = negative ? new Color(1f, 0.62f, 0.68f) : new Color(0.72f, 0.84f, 1f);
            }
            if (tagBacking != null && negativeTag != null && positiveTag != null)
                tagBacking.sharedMaterial = negative ? negativeTag : positiveTag;
            if (marker != null)
            {
                marker.localPosition = new Vector3(newValue, marker.localPosition.y, 0f);
                if (markerParts != null && negativeMarker != null && positiveMarker != null)
                    foreach (var part in markerParts)
                        if (part != null) part.sharedMaterial = negative ? negativeMarker : positiveMarker;
            }
            ApplyGlow();
        }

        /// Shows or hides the tall beam. Far spheres are hard to find without it.
        public void SetPillar(bool visible)
        {
            if (pillar != null) pillar.gameObject.SetActive(visible);
        }

        /// 0 = dimmed (locked), 1 = normal, above 1 = highlighted.
        public void SetGlow(float amount)
        {
            glow = amount;
            ApplyGlow();
        }

        void ApplyGlow()
        {
            if (body == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block);
            block.SetColor("_BaseColor", Color.Lerp(new Color(0.32f, 0.33f, 0.36f), color, Mathf.Clamp01(glow)));
            block.SetColor("_EmissionColor", color * (glow * 0.8f));
            body.SetPropertyBlock(block);
        }

        /// Takes the sphere into the player's hand: it follows the carry point, at a smaller size.
        public void Carry(Transform point, float scale)
        {
            if (motion != null) { StopCoroutine(motion); motion = null; }
            follow = point;
            targetScale = scale;
        }

        /// Held by a controller: the sphere stays in the hand's grasp but shrinks like a carried one.
        public void Shrink(float scale)
        {
            targetScale = scale;
        }

        /// Lets go of it (it grows back to full size as it goes).
        public void StopCarry()
        {
            follow = null;
            targetScale = 1f;
        }

        /// Floats back to its place on the line along a small arc. With announce, Landed is raised when it arrives.
        public void GlideHome(float seconds = 0.6f, bool announce = false)
        {
            StartMotion(GlideRoutine(seconds, announce));
        }

        /// Off the table and back to its place on the line, ready to be fetched again.
        public void SendBack(float seconds = 0.9f)
        {
            CaptureHome();
            transform.SetParent(homeParent, true);
            placed = false;
            targetScale = 1f;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;
            Grab.enabled = true;
            GlideHome(seconds, true);
        }

        IEnumerator GlideRoutine(float seconds, bool announce)
        {
            Vector3 from = transform.position;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                transform.position = Vector3.Lerp(from, HomeWorld, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.35f);
                yield return null;
            }
            transform.position = HomeWorld;
            motion = null;
            if (announce) Landed?.Invoke(this);
        }

        /// Settles into a table socket and stays there, at its smaller size, with its underside on the ring.
        public void SnapTo(Transform anchor, float seconds = 0.18f)
        {
            follow = null;
            targetScale = PlacedScale;
            StartMotion(SnapRoutine(anchor, seconds));
        }

        IEnumerator SnapRoutine(Transform anchor, float seconds)
        {
            transform.SetParent(anchor, true);
            Vector3 from = transform.localPosition;
            Vector3 rest = Vector3.down * PlacedDrop;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                transform.localPosition = Vector3.Lerp(from, rest, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            transform.localPosition = rest;
            motion = null;
        }

        /// Puts the sphere back exactly as it was at the start of the level.
        public void ResetToHome()
        {
            CaptureHome();
            if (motion != null) { StopCoroutine(motion); motion = null; }
            follow = null;
            targetScale = 1f;
            transform.localScale = Vector3.one;
            transform.SetParent(homeParent, true);
            transform.position = HomeWorld;
            placed = false;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;
            tagSize = 1f;
            if (tagRoot != null) { tagRoot.gameObject.SetActive(true); ApplyTagSize(); }
            SetGlow(1f);
        }

        void StartMotion(IEnumerator routine)
        {
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(routine);
        }
    }
}
