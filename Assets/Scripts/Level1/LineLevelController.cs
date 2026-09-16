using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace NumberLinePlayground.Level1
{
    public enum LevelState { Brief, Play, Complete }

    /// Plays one level of "Position, Origin and Direction": fetch the spheres from the number line and place them on the
    /// marked spots of the table, which makes a line, a triangle or a square. ConceptFlow decides which level this is
    /// (Configure), when it starts (BeginPlay) and what happens after it (the Completed event).
    ///
    /// Picking up: walk up to a sphere and press G, and it comes with you; press G again and it leaves your hand. That
    /// works from the keyboard alone. With a headset (or the simulator's hands) the same happens with the controllers:
    /// aim at a sphere and press grip or trigger once to pick it up, and once more to put it down.
    ///
    /// The rules (all enforced here, nothing else in the scene knows them):
    ///  - One sphere at a time.
    ///  - Every trip starts at the origin: a sphere can only be picked up after you have stood in the origin ring since
    ///    your last deposit or drop (the first trip of a level may start at the start point, which is on 0 too), and you
    ///    must walk to it.
    ///  - With the facing check (Level 2) you may declare your way at the ring: face left (the negative side) or right (the
    ///    positive side) and press G. The arrow that way lights up. Declaring a side with no sphere left, or then picking up a
    ///    sphere from the other side, is refused with a buzz and costs a pip. Declaring is optional: without it nothing is
    ///    blocked, and the sphere you pick up names its own side (its arrow lights while you carry it).
    ///  - A sphere counts only when released over the marked spot stamped with its own position, from inside the ring.
    ///    With the pair rule (Level 3) the spots carry no numbers: any sphere may go on any spot, and when the two spots of
    ///    an edge are full the two spheres must be exactly one side apart. If not, both arc back to the line and land with
    ///    a splash, and a pip is lost.
    ///  - A wrong spot costs a pip and the sphere returns to your hand. Dropping it anywhere else costs nothing:
    ///    it floats back to its place on the line.
    ///
    /// Position along the line is the headset's local x. Path length adds |change in x| every frame, so teleporting
    /// counts. Displacement is measured from the origin ring, where every trip starts and ends.
    public class LineLevelController : MonoBehaviour
    {
        // ---- wired by NumberLinePlaygroundBuilder ----
        public Transform root;                       // the playground root; all positions here are local to it
        public Transform head;                       // the player's camera
        public FetchSphere[] spheres;                // four slots; a level uses as many as it has numbers
        public TableSocket[] sockets;                // four slots, the same
        public float zoneRadius = 1f;                // how close to the origin counts as "in the ring"
        public float tableHalfWidth = 1.4f;          // the way up to the table counts as the ring too (see InZone)
        public float tableDepth = 3.2f;              // ... as far as the table's far edge, so the back row can be reached
        public float snapRadius = 0.5f;              // how close to a socket a released sphere must be to count
        public float reach = 1.8f;                   // how close you must stand to a sphere to pick it up
        public Transform carryPoint;                 // where a sphere picked up with G rides: low and to the right, ahead of the eyes
        public float carryScale = 0.32f;             // and how big it is while carried (smaller than it waits: it must not fill the view)

        public Transform table;                      // the whole table; made smaller once for a shorter player
        public Transform post;                       // the post under a raised spot (the top of the triangle)
        public TextMesh machineTitle;                // "LINE MAKER" on the machine's display
        public TextMesh machineCount;                // "0 / 2" below it
        public TextMesh machineNote;                 // "side d = 4 m" below that, on the square level
        public Transform[] blueprintBars;            // the faint outline of the figure on the table
        public Transform[] weldBars;                 // the glowing figure that appears when it is done
        public Renderer[] originRing;
        public Renderer[] leftArrow;                 // beside the ring: they light up as you face left or right
        public Renderer[] rightArrow;
        public Material ringIdleMaterial;
        public Material ringArmedMaterial;
        public TextMesh feetLabel;                   // "x = -3.2 m" on the floor in front of the player
        public TextMesh tripPathLabel;
        public TextMesh tripDisplacementLabel;
        public ParticleSystem splash;                // droplets where a sphere sent back lands

        public GameObject toastBox;
        public Text toastText;
        public Text positionText;
        public ParticleSystem confetti;

        public Button markButton;                    // SET MARK, offered on replays of the square level
        public Text markText;                        // "from mark 4.0 m"
        public Transform markObject;                 // the marker it drops on the line
        public GameObject promptBox;                 // a line that says what G would do right now
        public Text promptText;

        /// Raised once, when the last sphere is placed and the figure has been made.
        public event System.Action<LevelStats> Completed;

        public bool IsPlaying => state == LevelState.Play;

        // ---- run-time state ----
        LevelConfig config;
        int[] values = new int[0];
        int socketCount;
        int side;                    // the side of the square, when the level has one
        bool[] rowLocked = new bool[0];
        bool[] weldShown = new bool[0];   // which edges of the glowing figure are drawn
        int handledFrame = -1;            // the last frame something was picked up or let go, so G is never counted twice
        float pickedUpAt = -10f;          // when the sphere in hand was picked up
        string shownPrompt;
        TableSocket autoCandidate;        // the spot a carried sphere is being held over ...
        float autoDwell;                  // ... and for how long
        LevelState state = LevelState.Brief;
        FetchSphere carrying;
        bool keyboardCarry;          // the sphere in hand was picked up with G, not with a controller
        bool armed;                  // you have started from the origin and may pick up
        bool bypassGate;             // lets a rejected sphere return to your hand
        bool ringShownArmed;
        bool wasInZone;
        bool tripActive;
        bool headingGate;            // this level asks for a facing check at the ring
        int confirmedWay;            // -1 left, +1 right, 0 not chosen yet; only used with the heading gate
        int arrowLit = 99;           // which arrow is lit now: -1, 0, +1, or 99 to redraw
        int deposited;
        int pipsLost;
        int lockKey = -1;
        float lastX, startX, pathTotal, tripPath;
        float idleTimer, toastUntil, summaryUntil, lastBlockedAt = -10f;
        bool heightChecked;
        bool markToolOn, hasMark;
        float markX;
        NearFarInteractor[] hands = new NearFarInteractor[0];
        IXRSelectInteractor lastInteractor;
        AudioSource sfx;
        AudioClip chime, buzz, click, splashClip;

        static readonly Color Negative = new Color(1f, 0.42f, 0.5f);
        static readonly Color Positive = new Color(0.45f, 0.68f, 1f);

        // ------------------------------------------------------------------------------------------------------
        void Awake()
        {
            if (root == null) root = transform;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.spatialBlend = 0f;
            sfx.playOnAwake = false;
            chime = Tone("Chime", 660f, 990f, 0.35f, 0.35f, false);
            buzz = Tone("Buzz", 130f, 90f, 0.22f, 0.4f, true);
            click = Tone("Click", 1200f, 900f, 0.05f, 0.3f, false);
            splashClip = Noise("Splash", 0.4f, 0.3f);

            // Carrying works with the simulator and controllers alike, even in a scene saved before this was added.
            if (GetComponent<GrabComfort>() == null) gameObject.AddComponent<GrabComfort>();
            foreach (var sphere in spheres)
            {
                var s = sphere;
                s.Grab.farAttachMode = InteractableFarAttachMode.Near;   // picked up with the ray, it flies to your hand
                s.Grab.selectFilters.Add(new GateFilter(this, s));
                s.Grab.selectEntered.AddListener(args => OnGrabbed(s, args));
                s.Grab.selectExited.AddListener(args => OnReleased(s, args));
                s.Grab.hoverEntered.AddListener(args => OnHover(s, true));
                s.Grab.hoverExited.AddListener(args => OnHover(s, false));
                s.Landed += OnSphereLanded;
                s.viewer = head;
            }
            if (markButton != null) markButton.onClick.AddListener(SetMark);
        }

        // ---- setting a level up -------------------------------------------------------------------------------
        /// Puts the spheres, their markers, the spots, the post and the machine into the arrangement for this level.
        /// Safe in the Editor as well as in Play mode: it only moves, labels and shows things.
        public void ApplyLayout(LevelConfig level, int variant)
        {
            config = level;
            values = level.Values(variant);
            socketCount = values.Length;
            side = level.pairRule ? PairRule.Side(values) : 0;
            rowLocked = new bool[level.rows != null ? level.rows.Length : 0];
            headingGate = level.headingGate;
            confirmedWay = 0;
            arrowLit = 99;

            for (int i = 0; i < spheres.Length; i++)
            {
                var s = spheres[i];
                bool used = i < values.Length;
                s.gameObject.SetActive(used);
                if (s.marker != null) s.marker.gameObject.SetActive(used);
                if (!used) continue;
                s.SetHome(values[i]);
                s.SetPillar(level.tallBeams);
                s.ResetToHome();
            }
            for (int i = 0; i < sockets.Length; i++)
            {
                bool used = i < values.Length;
                sockets[i].gameObject.SetActive(used);
                if (used) sockets[i].SetSpot(values[i], level.layout[i], level.LiftAt(i), !level.pairRule);
            }
            if (machineTitle != null) machineTitle.text = level.machineTitle;
            if (machineNote != null) machineNote.text = level.pairRule ? "side d = " + side + " m" : "";
            deposited = 0;
            UpdateMachineDisplay();
            LayPost(level);
            SetArrowsShown(headingGate);
            SetMarkTool(false);
            DrawBlueprint();
            HideBars(weldBars);
            weldShown = new bool[weldBars != null ? weldBars.Length : 0];
        }

        /// Starts a level's slide stage: everything laid out for it, nothing pick-up-able until BeginPlay.
        public void Configure(LevelConfig level, int variant)
        {
            ApplyLayout(level, variant);
            state = LevelState.Brief;
            carrying = null;
            keyboardCarry = false;
            armed = false;
            wasInZone = false;
            bypassGate = false;
            tripActive = false;
            pipsLost = 0;
            pathTotal = 0f;
            tripPath = 0f;
            idleTimer = 0f;
            foreach (var s in spheres) s.Grab.enabled = false;
            shownPrompt = null;
            if (promptBox != null) promptBox.SetActive(false);
            if (toastBox != null) toastBox.SetActive(false);
            SetSummaryVisible(false);
            if (confetti != null) confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SetRingArmed(false);
            RefreshLocks(true);
        }

        /// Offers (or hides) the SET MARK button. On the square level's replays it lets the player drop a marker where
        /// they stand and read their distance from it while they walk, to measure a gap by walking it.
        public void SetMarkTool(bool on)
        {
            markToolOn = on;
            hasMark = false;
            if (markButton != null) markButton.gameObject.SetActive(on);
            if (markText != null) markText.gameObject.SetActive(false);
            if (markObject != null) markObject.gameObject.SetActive(false);
        }

        void SetMark()
        {
            if (!markToolOn || head == null || markObject == null) return;
            markX = HeadLocal().x;
            hasMark = true;
            markObject.localPosition = new Vector3(markX, 0.02f, 0f);
            markObject.gameObject.SetActive(true);
            if (markText != null) markText.gameObject.SetActive(true);
            Play(click);
            Toast("Mark dropped at x = " + markX.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " m. Walk, and read how far you are from it.", 3.5f);
        }

        /// The player pressed START: the spheres may now be fetched.
        public void BeginPlay()
        {
            state = LevelState.Play;
            for (int i = 0; i < values.Length; i++) spheres[i].Grab.enabled = true;
            hands = FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
            EnsureCarryPoint();
            Vector3 local = HeadLocal();
            lastX = startX = local.x;
            idleTimer = 0f;
            // The start point is on x = 0, like the ring, so the first trip may begin right here: no walk to the ring is
            // needed before the first sphere. Every later trip starts from the ring.
            armed = true;
            RefreshLocks(true);
            Toast(config != null && config.pairRule
                ? "Plan first: find the two pairs " + side + " m apart, then walk to a sphere and press G"
                : "Walk to a sphere and press G to pick it up", 4.5f);
        }

        /// A sphere picked up with G rides low and to the right, ahead of the eyes.
        void EnsureCarryPoint()
        {
            if (carryPoint != null || head == null) return;
            var point = new GameObject("CarryPoint").transform;
            point.SetParent(head, false);
            point.localPosition = new Vector3(0.3f, -0.35f, 0.7f);
            carryPoint = point;
        }

        /// One-time height check: a player whose eyes are lower than an adult's (a child, or someone seated) gets the
        /// whole table, and so the post and the spots on it, made smaller so everything stays within reach.
        /// A taller player keeps the table as built. The change is smooth, not a jump.
        public IEnumerator CheckHeightOnce()
        {
            if (heightChecked || table == null || head == null || root == null) yield break;
            heightChecked = true;
            float eyes = head.position.y - root.position.y;
            float scale = Mathf.Clamp(eyes / 1.55f, 0.7f, 1f);
            if (scale > 0.94f) yield break;                       // near enough to as built
            const float seconds = 0.6f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                table.localScale = Vector3.one * Mathf.Lerp(1f, scale, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            table.localScale = Vector3.one * scale;
        }

        // ---- per-frame ----------------------------------------------------------------------------------------
        void Update()
        {
            if (toastBox != null && toastBox.activeSelf && Time.unscaledTime > toastUntil) toastBox.SetActive(false);
            if (tripPathLabel != null && tripPathLabel.gameObject.activeSelf && Time.unscaledTime > summaryUntil) SetSummaryVisible(false);
            if (head == null || root == null) return;

            Vector3 local = HeadLocal();
            UpdateLabels(local);
            if (hasMark && markText != null && markText.gameObject.activeSelf)
                markText.text = "from mark  " + Mathf.Abs(local.x - markX).ToString("0.0", CultureInfo.InvariantCulture) + " m";
            if (state == LevelState.Play) TickPlay(local);
        }

        void TickPlay(Vector3 local)
        {
            bool inZone = InZone(local);
            float dx = Mathf.Abs(local.x - lastX);
            if (dx < 5f)                                    // ignore a one-off jump such as a re-centre
            {
                pathTotal += dx;
                if (tripActive) tripPath += dx;
                if (dx > 0.02f) idleTimer = 0f;
            }
            lastX = local.x;

            if (carrying == null && inZone) armed = true;    // standing in the ring arms the next trip
            if (wasInZone && !inZone && armed && carrying == null)
            {
                tripActive = true;                           // you just left the origin: a trip has begun
                tripPath = 0f;
            }
            wasInZone = inZone;

            // G on the keyboard, or a squeeze on a controller. In the simulator with a hand held, G is the squeeze itself,
            // and the controller may already have picked something up or let it go this frame: then G has been used.
            // In every other case G is handled here, so it works whether or not a hand is held.
            bool squeezed = SqueezedThisFrame();
            bool key = KeyPressed() && handledFrame != Time.frameCount;
            if (headingGate) UpdateArrows(inZone);
            // In the ring on the facing-check level G declares the way you face, unless a sphere is close enough to pick up.
            if (headingGate && inZone && armed && carrying == null && (squeezed || key) && !SphereWithinReach()) TryConfirmWay();
            else if (key) KeyboardToggle();

            SetRingArmed(armed && carrying == null);
            RefreshLocks(false);

            if (carrying != null)
            {
                var aimed = TargetSocket(carrying, lastInteractor);
                for (int i = 0; i < socketCount; i++) sockets[i].SetHot(sockets[i] == aimed && !sockets[i].IsFilled);
                TryAutoPlace();
            }
            else
            {
                for (int i = 0; i < socketCount; i++) sockets[i].SetHot(false);
                autoCandidate = null;
                autoDwell = 0f;
            }

            idleTimer += Time.deltaTime;
            if (idleTimer > 25f && carrying == null)
            {
                idleTimer = 0f;
                Toast("Walk to a sphere and press G to pick it up", 3.5f);
            }
            UpdatePrompt();
        }

        // ---- G on the keyboard: pick up, carry, put down ------------------------------------------------------
        static bool KeyPressed()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.gKey.wasPressedThisFrame;
        }

        void KeyboardToggle()
        {
            Debug.Log("[G] level " + (config != null ? config.number : 0) + ": " + PromptNow() + "   (armed " + armed + ", carrying "
                + (carrying != null) + ", way " + confirmedWay + ")");
            if (carrying != null)
            {
                LetGo();                                       // G again: it leaves your hand
                return;
            }
            KeyboardPickUp();
        }

        /// G near a sphere: the nearest one within reach comes with you, if the rules allow it.
        void KeyboardPickUp()
        {
            var best = NearestFetchable(out float bestDistance);
            if (best == null) return;
            if (bestDistance > reach) { Blocked("Walk to a sphere, then press G", null); return; }
            if (!CanGrab(best, null)) return;                 // it says why: start from the ring, the other way, and so on
            BeginKeyboardCarry(best);
        }

        bool SphereWithinReach()
        {
            var near = NearestFetchable(out float distance);
            return near != null && distance <= reach;
        }

        /// The sphere nearest to you on the level, and how far away it is. With the heading gate, spheres on the side you
        /// said you would go come first.
        FetchSphere NearestFetchable(out float distance)
        {
            FetchSphere best = null;
            float bestScore = float.MaxValue;
            distance = float.MaxValue;
            Vector3 headLocal = HeadLocal();
            int wanted = headingGate ? confirmedWay : 0;
            for (int i = 0; i < values.Length; i++)
            {
                var s = spheres[i];
                if (s.placed || !s.gameObject.activeSelf) continue;
                Vector3 p = root.InverseTransformPoint(s.transform.position);
                float d = new Vector2(headLocal.x - p.x, headLocal.z - p.z).magnitude;
                float score = wanted != 0 && SideOf(s.value) != wanted ? d + 1000f : d;
                if (score < bestScore) { bestScore = score; best = s; distance = d; }
            }
            return best;
        }

        /// What G would do right now, in a few words, so it is never a mystery why nothing happened.
        string PromptNow()
        {
            if (state != LevelState.Play || head == null) return "";
            if (carrying != null)
            {
                if (!InZone(HeadLocal())) return "Walk back to x = 0, in front of the table, to place it";
                if (!keyboardCarry) return "Aim at its ring and press again to put it down";
                return config != null && config.pairRule
                    ? "G  put it down   (hold it over a ring: it drops in)"
                    : "G  put it down   (it drops into its own ring by itself)";
            }
            if (!armed) return "Step into the ring, then fetch a sphere";
            var near = NearestFetchable(out float distance);
            if (headingGate && InZone(HeadLocal()) && (near == null || distance > reach))
            {
                if (confirmedWay != 0) return "Going " + (confirmedWay < 0 ? "left" : "right") + ": walk to a sphere on that side";
                int facing = FacingWay();
                return facing == 0 ? "Walk to a sphere, or face left or right and press G to declare your way"
                                   : "G  declare: going " + (facing < 0 ? "left (negative)" : "right (positive)");
            }
            if (near == null) return "";
            if (headingGate && confirmedWay != 0 && SideOf(near.value) != confirmedWay) return "No sphere left on the side you declared";
            if (distance > reach) return "Walk to the sphere at " + Concept.Signed(near.value) + ", then press G";
            return "G  pick up the sphere at x = " + Concept.Signed(near.value) + " m";
        }

        void UpdatePrompt()
        {
            if (promptText == null) return;
            string now = PromptNow();
            if (now == shownPrompt) return;
            shownPrompt = now;
            promptText.text = now;
            if (promptBox != null) promptBox.SetActive(now.Length > 0);
        }

        void BeginKeyboardCarry(FetchSphere sphere, bool announce = true)
        {
            EnsureCarryPoint();
            keyboardCarry = true;
            sphere.Grab.enabled = false;                      // the keyboard has it, not a controller
            sphere.Carry(carryPoint != null ? carryPoint : head, carryScale);
            AfterPickUp(sphere, null, announce);
        }

        /// G again: it leaves your hand and the level decides what that means (placed, refused, or back to the line).
        void DropCarried()
        {
            var sphere = carrying;
            keyboardCarry = false;
            sphere.StopCarry();
            if (state == LevelState.Play) sphere.Grab.enabled = true;
            HandleRelease(sphere, null);
        }

        /// The sphere leaves your hand, whichever way you picked it up.
        void LetGo()
        {
            if (carrying == null) return;
            if (keyboardCarry) DropCarried();
            else if (lastInteractor != null && carrying.Grab.interactionManager != null)
                carrying.Grab.interactionManager.SelectExit(lastInteractor, carrying.Grab);   // held by a controller: let go of it
        }

        /// A carried sphere that reaches the ring it belongs in drops into it by itself and stays there: no second press.
        /// "Reaches" means it is within the snap radius of the ring, or you are looking (or pointing) at the ring; either
        /// must last a moment, so a ring passed on the way is not taken. It only ever drops on a ring where it fits: the
        /// one stamped with its number, or, on the square level where the spots carry no numbers, any free spot (that one
        /// takes longer, since the choice is the point). A wrong ring is left alone; put it there with G to see why not.
        void TryAutoPlace()
        {
            // Not in the first moments after a pick-up: a sphere picked up close to the ring, with the table in view, must not
            // go straight onto it.
            if (carrying == null || !InZone(HeadLocal()) || config == null || Time.time - pickedUpAt < 1.2f)
            {
                autoCandidate = null;
                autoDwell = 0f;
                return;
            }
            var near = NearestSocket(carrying.transform.position);
            // Looking (or pointing) at a ring counts too, since the table is bigger than an arm's reach. On the square level
            // any free spot will do, so there it takes longer: the choice is the point.
            var target = near != null ? near : SocketOnRay(lastInteractor);
            bool fits = target != null && !target.IsFilled && (config.pairRule || target.expectedValue == carrying.value);
            if (!fits)
            {
                autoCandidate = null;
                autoDwell = 0f;
                return;
            }
            if (target != autoCandidate) { autoCandidate = target; autoDwell = 0f; }
            autoDwell += Time.deltaTime;
            float needed = config.pairRule ? (near != null ? 0.8f : 1.2f) : near != null ? 0.15f : 0.5f;
            if (autoDwell < needed) return;
            autoCandidate = null;
            autoDwell = 0f;
            LetGo();
        }

        // ---- facing the way you will go (the heading gate) ----------------------------------------------------
        /// -1 when you face left along the line, +1 when you face right, 0 when you face neither (within 45 degrees).
        int FacingWay()
        {
            Vector3 f = root.InverseTransformDirection(head.forward);
            f.y = 0f;
            if (f.sqrMagnitude < 0.01f) return 0;
            f.Normalize();
            if (f.x > 0.7f) return 1;
            if (f.x < -0.7f) return -1;
            return 0;
        }

        /// The arrow for the side you face lights up while you stand in the ring; once you have confirmed, it stays lit.
        void UpdateArrows(bool inZone)
        {
            int facing = inZone && armed && carrying == null && confirmedWay == 0 ? FacingWay() : 0;
            int lit = carrying != null ? SideOf(carrying.value) : confirmedWay != 0 ? confirmedWay : facing;
            if (lit == arrowLit) return;
            arrowLit = lit;
            SetArrow(leftArrow, lit < 0);
            SetArrow(rightArrow, lit > 0);
        }

        void SetArrow(Renderer[] parts, bool lit)
        {
            if (parts == null) return;
            foreach (var part in parts)
                if (part != null) part.sharedMaterial = lit ? ringArmedMaterial : ringIdleMaterial;
        }

        void SetArrowsShown(bool shown)
        {
            foreach (var parts in new[] { leftArrow, rightArrow })
            {
                if (parts == null) continue;
                foreach (var part in parts)
                    if (part != null) part.gameObject.SetActive(shown);
            }
            SetArrow(leftArrow, false);
            SetArrow(rightArrow, false);
        }

        /// The press at the ring: it declares the way you will go, if a sphere is left that way.
        void TryConfirmWay()
        {
            int facing = FacingWay();
            Debug.Log("[G] level " + (config != null ? config.number : 0) + ": confirm the way you face at the ring (facing " + facing + ")");
            if (facing == 0)
            {
                Toast("To declare your way, face left or right along the line, then press G", 2.5f);
                return;
            }
            if (!AnySphereThatWay(facing))
            {
                pipsLost++;
                Toast("Nothing left that way  (-1 pip)", 2.4f);
                Play(buzz);
                HapticAll(0.7f, 0.2f);
                return;
            }
            confirmedWay = facing;
            RefreshLocks(true);
            UpdateArrows(true);
            Play(click);
            HapticAll(0.35f, 0.08f);
            Toast(facing < 0 ? "Going left: the negative side" : "Going right: the positive side", 2.5f);
        }

        bool AnySphereThatWay(int way)
        {
            for (int i = 0; i < values.Length; i++)
                if (!spheres[i].placed && SideOf(spheres[i].value) == way) return true;
            return false;
        }

        static int SideOf(int value) => value < 0 ? -1 : 1;

        bool SqueezedThisFrame()
        {
            foreach (var hand in hands)
                if (hand != null && hand.selectInput.ReadWasPerformedThisFrame()) return true;
            return false;
        }

        // ---- picking up ---------------------------------------------------------------------------------------
        /// Called by the select filter each time a controller tries to pick a sphere up, and by G before it does.
        bool CanGrab(FetchSphere sphere, IXRSelectInteractor interactor)
        {
            if (state != LevelState.Play || sphere.placed) return false;
            if (bypassGate) return true;
            if (carrying != null)
            {
                if (carrying == sphere) return true;
                Blocked("One sphere at a time", interactor);
                return false;
            }
            if (!armed)
            {
                Blocked("Start from the origin ring", interactor);
                return false;
            }
            // Only a declared way limits you: once you have said which way you will go, the other side is refused.
            if (headingGate && confirmedWay != 0 && SideOf(sphere.value) != confirmedWay)
            {
                WrongWay(interactor);
                return false;
            }
            Vector3 headLocal = HeadLocal();
            Vector3 home = root.InverseTransformPoint(sphere.HomeWorld);
            if (new Vector2(headLocal.x - home.x, headLocal.z - home.z).magnitude > reach)
            {
                Blocked("Walk to the sphere first", interactor);   // the walk is the point: it is what the path measures
                return false;
            }
            return true;
        }

        /// A refusal with a buzz. From a controller's ray it only speaks when the trigger is really pressed (the ray
        /// asks all the time just by pointing); from the keyboard (no interactor) it always speaks.
        void Blocked(string message, IXRSelectInteractor interactor)
        {
            if (interactor != null && !interactor.isSelectActive) return;
            if (Time.unscaledTime - lastBlockedAt < 1f) return;             // react once a second at most
            lastBlockedAt = Time.unscaledTime;
            Toast(message, 2f);
            Play(buzz);
            Haptic(interactor, 0.5f, 0.12f);
        }

        /// Reaching for a sphere on the other side from the way you said you would go.
        void WrongWay(IXRSelectInteractor interactor)
        {
            if (interactor != null && !interactor.isSelectActive) return;
            if (Time.unscaledTime - lastBlockedAt < 1f) return;
            lastBlockedAt = Time.unscaledTime;
            pipsLost++;
            Toast("Wrong way: that one is on the other side  (-1 pip)", 2.6f);
            Play(buzz);
            Haptic(interactor, 0.7f, 0.2f);
        }

        void OnGrabbed(FetchSphere sphere, SelectEnterEventArgs args)
        {
            keyboardCarry = false;
            sphere.Shrink(carryScale);                       // in a controller's hand it is as small as one carried with G
            AfterPickUp(sphere, args.interactorObject, !bypassGate);   // not when a wrong drop returns it to your hand
        }

        /// What follows any pick-up, whether by controller or by G.
        void AfterPickUp(FetchSphere sphere, IXRSelectInteractor interactor, bool announce)
        {
            handledFrame = Time.frameCount;
            bool declared = confirmedWay != 0;
            pickedUpAt = Time.time;
            carrying = sphere;
            lastInteractor = interactor;
            armed = false;
            confirmedWay = 0;                                // the way you said you would go is spent
            arrowLit = 99;
            idleTimer = 0f;
            if (!tripActive) { tripActive = true; tripPath = 0f; }
            Play(click);
            Haptic(interactor, 0.35f, 0.08f);
            RefreshLocks(true);
            if (announce)
            {
                if (headingGate && !declared)
                    Toast("That one is on the " + (SideOf(sphere.value) < 0 ? "left: the negative side" : "right: the positive side")
                        + ". Take it to its ring.", 4f);
                else
                    Toast(config != null && config.pairRule
                        ? "Carry it to the spot you choose and hold it over the ring"
                        : "Take it to its ring: it drops in by itself", 4f);
            }
        }

        void OnHover(FetchSphere sphere, bool entered)
        {
            if (state != LevelState.Play || sphere.placed) return;
            if (entered && carrying == null && armed && MayFetch(sphere)) sphere.SetGlow(2f);
            else RefreshLocks(true);
        }

        /// Whether this sphere is one you may go for now: you have started from the ring and, with the heading gate,
        /// said you would go its way.
        bool MayFetch(FetchSphere sphere) =>
            armed && (!headingGate || confirmedWay == 0 || SideOf(sphere.value) == confirmedWay);

        // ---- releasing: place, reject, or drop ----------------------------------------------------------------
        void OnReleased(FetchSphere sphere, SelectExitEventArgs args)
        {
            HandleRelease(sphere, args.interactorObject);
        }

        /// A sphere has left your hand, from a controller (its interactor) or from G (none).
        void HandleRelease(FetchSphere sphere, IXRSelectInteractor interactor)
        {
            if (state != LevelState.Play || carrying != sphere) return;
            handledFrame = Time.frameCount;
            carrying = null;
            keyboardCarry = false;
            confirmedWay = 0;
            arrowLit = 99;
            sphere.StopCarry();                              // full size again unless it is placed
            for (int i = 0; i < socketCount; i++) sockets[i].SetHot(false);
            RefreshLocks(true);

            autoCandidate = null;
            autoDwell = 0f;
            var target = TargetSocket(sphere, interactor);
            if (target == null)
            {
                armed = false;                               // a drop ends the trip wherever you stand
                tripActive = false;
                sphere.GlideHome();
                Toast("It floats back to the line", 2f);
                return;
            }
            if (target.IsFilled) { Reject(sphere, interactor, "That spot is taken", false); return; }
            if (!InZone(HeadLocal())) { Reject(sphere, interactor, "Come back to x = 0, in front of the table, to place it", false); return; }
            // With the pair rule the spots carry no numbers, so any sphere may go on any free spot; its edge is checked
            // once both spots of the edge are full.
            if (!config.pairRule && sphere.value != target.expectedValue)
            {
                Reject(sphere, interactor, "Wrong spot: match the number", true);
                return;
            }
            StartCoroutine(PlaceRoutine(sphere, target, interactor));
        }

        void Reject(FetchSphere sphere, IXRSelectInteractor interactor, string message, bool costsPip)
        {
            if (costsPip) pipsLost++;
            Toast(message + (costsPip ? "  (-1 pip)" : ""), 2.4f);
            Play(buzz);
            Haptic(interactor, 0.7f, 0.2f);
            StartCoroutine(ReturnToHand(sphere, interactor));
        }

        IEnumerator ReturnToHand(FetchSphere sphere, IXRSelectInteractor interactor)
        {
            yield return null;                               // let the release finish before taking it again
            if (state != LevelState.Play || carrying != null) yield break;
            if (interactor == null)                          // it was carried with G: carry it again
            {
                BeginKeyboardCarry(sphere, false);
                yield break;
            }
            var manager = sphere.Grab.interactionManager;
            bool usable = interactor != null && (!(interactor is Object o) || o != null);
            if (usable && manager != null && !sphere.Grab.isSelected)
            {
                bypassGate = true;
                manager.SelectEnter(interactor, sphere.Grab);
                yield return null;
                bypassGate = false;
            }
            if (!sphere.Grab.isSelected)                     // could not put it back in your hand: send it home instead
            {
                armed = false;
                tripActive = false;
                sphere.GlideHome();
            }
        }

        IEnumerator PlaceRoutine(FetchSphere sphere, TableSocket socket, IXRSelectInteractor interactor)
        {
            sphere.placed = true;
            socket.occupant = sphere;
            deposited++;
            idleTimer = 0f;
            Play(chime);
            Haptic(interactor, 0.6f, 0.15f);
            ShowTripSummary();
            tripActive = false;
            tripPath = 0f;
            UpdateMachineDisplay();
            RefreshLocks(true);

            yield return null;                               // disabling an interactable mid-callback is unsafe
            sphere.Grab.enabled = false;
            foreach (var c in sphere.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            sphere.SnapTo(socket.anchor);
            yield return new WaitForSeconds(0.25f);          // let it settle into the ring

            if (config.pairRule) yield return CheckEdge(socket);   // the square looks at the edge this sphere belongs to
            else
            {
                RefreshWeld();                               // a line joins two placed spheres by itself
                if (deposited >= socketCount) StartCoroutine(CompleteRoutine());
            }
        }

        // ---- the square: two spheres of an edge must be a side apart -----------------------------------------
        /// When both spots of an edge are full, the two spheres on it must be exactly one side apart. If they are, the
        /// edge is locked. If not, the pair is plain to see for a moment and then both arc back to the line.
        IEnumerator CheckEdge(TableSocket placed)
        {
            int edge = EdgeOf(System.Array.IndexOf(sockets, placed));
            if (edge < 0 || rowLocked[edge]) yield break;
            var first = sockets[config.rows[edge][0]];
            var second = sockets[config.rows[edge][1]];
            if (!first.IsFilled || !second.IsFilled) yield break;   // the other end of this edge is still empty

            if (PairRule.IsPair(first.occupant.value, second.occupant.value, side))
            {
                rowLocked[edge] = true;
                Play(chime);
                HapticAll(0.6f, 0.15f);
                Toast("A pair " + side + " m apart: locked in", 2.4f);
                bool all = true;
                foreach (bool locked in rowLocked) if (!locked) all = false;
                if (all) StartCoroutine(CompleteRoutine());
                yield break;
            }

            pipsLost++;
            Toast("Not a pair: " + Mathf.Abs(first.occupant.value - second.occupant.value) + " m apart, not " + side + " m  (-1 pip)", 3.2f);
            Play(buzz);
            HapticAll(0.7f, 0.2f);
            yield return new WaitForSeconds(0.8f);                   // long enough to see what was wrong
            SendBack(first);
            SendBack(second);
        }

        int EdgeOf(int spot)
        {
            if (config.rows == null) return -1;
            for (int e = 0; e < config.rows.Length; e++)
                foreach (int s in config.rows[e])
                    if (s == spot) return e;
            return -1;
        }

        void SendBack(TableSocket spot)
        {
            var sphere = spot.occupant;
            spot.occupant = null;
            deposited--;
            if (sphere != null) sphere.SendBack(0.9f);
            UpdateMachineDisplay();
            RefreshLocks(true);
        }

        void OnSphereLanded(FetchSphere sphere)
        {
            if (splash != null)
            {
                if (!splash.isPlaying) splash.Play();          // a stopped system does not simulate what is emitted into it
                var at = new ParticleSystem.EmitParams { position = sphere.transform.position, applyShapeToPosition = true };
                splash.Emit(at, 28);
            }
            Play(splashClip);
        }

        IEnumerator CompleteRoutine()
        {
            state = LevelState.Complete;
            if (promptBox != null) promptBox.SetActive(false);
            yield return new WaitForSeconds(0.3f);
            DrawWeldAll();                                   // whatever of the figure is not drawn yet
            Play(chime);
            yield return new WaitForSeconds(0.9f);           // the line finishes drawing
            if (confetti != null) confetti.Play();
            yield return new WaitForSeconds(1.2f);
            Completed?.Invoke(BuildStats());
        }

        LevelStats BuildStats()
        {
            Vector3 local = HeadLocal();
            return new LevelStats
            {
                figure = Concept.Describe(config, values),
                path = pathTotal,
                displacement = InZone(local) ? 0f : local.x - startX,   // you finish in the ring: back at the origin
                stars = pipsLost == 0 ? 3 : pipsLost <= 2 ? 2 : 1,
            };
        }

        // ---- the figure on the table --------------------------------------------------------------------------
        /// Stands the post under a raised spot, if the level has one (the top of the triangle), and hides it if not.
        void LayPost(LevelConfig level)
        {
            if (post == null) return;
            int raised = -1;
            for (int i = 0; i < socketCount; i++)
                if (level.LiftAt(i) > 0f) raised = i;
            post.gameObject.SetActive(raised >= 0);
            if (raised < 0) return;

            float lift = level.LiftAt(raised);
            post.localPosition = new Vector3(level.layout[raised].x, 0f, level.layout[raised].y);
            var shaft = post.Find("Shaft");
            var cap = post.Find("Cap");
            const float surfaceTop = 0.05f;
            if (shaft != null)
            {
                shaft.localPosition = new Vector3(0f, surfaceTop + (TableSocket.RestHeight + lift - surfaceTop) * 0.5f, 0f);
                shaft.localScale = new Vector3(0.07f, (TableSocket.RestHeight + lift - surfaceTop) * 0.5f, 0.07f);   // a cylinder is 2 tall
            }
            if (cap != null) cap.localPosition = new Vector3(0f, TableSocket.RestHeight + lift - 0.012f, 0f);
        }

        /// The faint outline of the figure, joining the middle of each spot.
        void DrawBlueprint()
        {
            var points = new Vector3[socketCount];
            for (int i = 0; i < socketCount; i++) points[i] = sockets[i].transform.localPosition;
            LayBars(blueprintBars, points, 0.03f, 0.006f);
        }

        int EdgeCount => socketCount < 3 ? 1 : socketCount;

        /// The centre of a placed sphere, in the space of the table surface the bars live in.
        static Vector3 WeldPoint(TableSocket spot) =>
            spot.transform.localPosition + spot.anchor.localPosition - Vector3.up * FetchSphere.PlacedDrop;

        /// Draws each edge of the glowing figure as soon as both of its spheres are placed: a line appears when its second
        /// sphere lands, and a triangle grows corner by corner. The square is drawn once, when it is done, because its
        /// pairs are only accepted once they check out.
        void RefreshWeld()
        {
            if (config == null || config.pairRule || weldBars == null) return;
            for (int e = 0; e < EdgeCount && e < weldBars.Length; e++)
            {
                var a = sockets[e];
                var b = sockets[(e + 1) % socketCount];
                if (a.IsFilled && b.IsFilled && !weldShown[e])
                {
                    weldShown[e] = true;
                    StartCoroutine(GrowBar(weldBars[e], WeldPoint(a), WeldPoint(b)));
                }
            }
        }

        /// The whole figure, for whatever is not drawn yet.
        void DrawWeldAll()
        {
            if (weldBars == null) return;
            for (int e = 0; e < EdgeCount && e < weldBars.Length; e++)
            {
                if (weldShown[e]) continue;
                weldShown[e] = true;
                StartCoroutine(GrowBar(weldBars[e], WeldPoint(sockets[e]), WeldPoint(sockets[(e + 1) % socketCount])));
            }
        }

        /// One glowing bar growing from one sphere to the next.
        static IEnumerator GrowBar(Transform bar, Vector3 from, Vector3 to)
        {
            Vector3 along = to - from;
            float length = along.magnitude;
            if (bar == null || length < 0.001f) yield break;
            bar.gameObject.SetActive(true);
            bar.localRotation = Quaternion.LookRotation(along / length, Vector3.up);
            const float seconds = 0.6f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                bar.localPosition = from + along * (0.5f * k);
                bar.localScale = new Vector3(0.05f, 0.05f, Mathf.Max(0.001f, length * k));
                yield return null;
            }
            bar.localPosition = from + along * 0.5f;
            bar.localScale = new Vector3(0.05f, 0.05f, length);
        }

        static void HideBars(Transform[] bars)
        {
            if (bars == null) return;
            foreach (var bar in bars) if (bar != null) bar.gameObject.SetActive(false);
        }

        /// Lays one bar along each edge: one for a line, a closed loop for three or more points. The bars are children
        /// of the table surface, and so are the spots, so the points are in the bars' own space.
        static void LayBars(Transform[] bars, Vector3[] points, float width, float height)
        {
            if (bars == null) return;
            int edges = points.Length < 3 ? 1 : points.Length;
            for (int e = 0; e < bars.Length; e++)
            {
                var bar = bars[e];
                if (bar == null) continue;
                bool used = e < edges && points.Length >= 2;
                bar.gameObject.SetActive(used);
                if (!used) continue;
                Vector3 a = points[e];
                Vector3 b = points[(e + 1) % points.Length];
                Vector3 along = b - a;
                bar.localPosition = (a + b) * 0.5f;
                bar.localRotation = Quaternion.LookRotation(along.normalized, Vector3.up);
                bar.localScale = new Vector3(width, height, along.magnitude);
            }
        }

        // ---- labels, HUD and feedback -------------------------------------------------------------------------
        Vector3 HeadLocal() => root.InverseTransformPoint(head.position);

        /// "At the origin" means at x = 0 on the number line, which is what a trip's displacement is measured against. The
        /// ring marks it on the ground, but the spots on the table are up to 2.5 m further along z, so standing in front of
        /// (or over) the table with x near 0 counts as well. Only the ring counted at first: a sphere could not be dropped
        /// from the table's edge, and the back row, which is out of reach from inside the ring, could not be filled at all.
        bool InZone(Vector3 local)
        {
            if (new Vector2(local.x, local.z).magnitude <= zoneRadius) return true;
            return Mathf.Abs(local.x) <= tableHalfWidth && local.z > 0f && local.z <= tableDepth;
        }

        public bool IsInZone(Vector3 local) => InZone(local);

        void UpdateLabels(Vector3 local)
        {
            string text = "x = " + local.x.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " m";
            Color color = local.x < -0.05f ? Negative : local.x > 0.05f ? Positive : Color.white;
            if (positionText != null) { positionText.text = text; positionText.color = color; }
            if (feetLabel != null)
            {
                Vector3 forward = head.forward;
                forward.y = 0f;
                forward = forward.sqrMagnitude < 0.01f ? root.forward : forward.normalized;
                feetLabel.transform.position = new Vector3(head.position.x, root.position.y + 0.04f, head.position.z) + forward * 0.75f;
                feetLabel.transform.rotation = Quaternion.Euler(90f, Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 0f);
                feetLabel.text = text;
                feetLabel.color = color;
            }
        }

        void ShowTripSummary()
        {
            if (tripPathLabel == null) return;
            tripPathLabel.text = "Path " + tripPath.ToString("0.0", CultureInfo.InvariantCulture) + " m";
            if (tripDisplacementLabel != null) tripDisplacementLabel.text = "Displacement 0.0 m";   // you are back in the ring
            PlaceSummary();
            SetSummaryVisible(true);
            summaryUntil = Time.unscaledTime + 3f;
        }

        /// The two lines hang in the air a little way ahead of you, a bit above the horizon: clear of the table, the toast at
        /// the bottom of the view and the machine's display above. Where they were built (0.8 m ahead of the ring) they
        /// filled the view and hid the spheres.
        void PlaceSummary()
        {
            if (head == null || tripPathLabel == null) return;
            Vector3 ahead = head.forward;
            ahead.y = 0f;
            ahead = ahead.sqrMagnitude < 0.01f ? root.forward : ahead.normalized;
            Vector3 centre = head.position + ahead * 1.9f;
            tripPathLabel.transform.position = centre + Vector3.up * 0.2f;
            if (tripDisplacementLabel != null) tripDisplacementLabel.transform.position = centre + Vector3.down * 0.02f;
        }

        void SetSummaryVisible(bool visible)
        {
            if (tripPathLabel != null) tripPathLabel.gameObject.SetActive(visible);
            if (tripDisplacementLabel != null) tripDisplacementLabel.gameObject.SetActive(visible);
        }

        void UpdateMachineDisplay()
        {
            if (machineCount != null) machineCount.text = deposited + " / " + socketCount;
        }

        void SetRingArmed(bool value)
        {
            if (value == ringShownArmed && originRing != null && originRing.Length > 0 && originRing[0] != null
                && originRing[0].sharedMaterial == (value ? ringArmedMaterial : ringIdleMaterial)) return;
            ringShownArmed = value;
            if (originRing == null) return;
            foreach (var segment in originRing)
                if (segment != null) segment.sharedMaterial = value ? ringArmedMaterial : ringIdleMaterial;
        }

        /// Spheres glow when you may pick them up, dim when locked, and the one in your hand shines brightest. With the
        /// heading gate, once you have said which way you will go, only the spheres that way glow.
        void RefreshLocks(bool force)
        {
            int key = (int)state | (armed ? 8 : 0) | (carrying != null ? 16 : 0) | ((confirmedWay + 1) << 5);
            if (!force && key == lockKey) return;
            lockKey = key;
            for (int i = 0; i < spheres.Length; i++)
            {
                var s = spheres[i];
                if (!s.gameObject.activeSelf) continue;
                if (s.placed) { s.SetGlow(1.2f); continue; }   // one on the table stays lit: it was dimmed the moment it left your hand
                float glow;
                if (state != LevelState.Play) glow = 1f;
                else if (carrying != null) glow = s == carrying ? 1.6f : 0.25f;
                else if (!armed) glow = 0.35f;
                else if (headingGate && confirmedWay != 0) glow = SideOf(s.value) == confirmedWay ? 1.3f : 0.35f;
                else glow = 1.2f;
                s.SetGlow(glow);
            }
        }

        /// The spot a sphere is being put on: the one it is next to, or else the one the hand's ray points at (or, from
        /// the keyboard, the one you are looking at).
        TableSocket TargetSocket(FetchSphere sphere, IXRSelectInteractor interactor)
        {
            var near = NearestSocket(sphere.transform.position);
            return near != null ? near : SocketOnRay(interactor);
        }

        /// Pointing at a spot works as well as reaching over to it, which suits the simulator and short arms alike.
        /// With no controller (G on the keyboard) the ray is where you look.
        TableSocket SocketOnRay(IXRSelectInteractor interactor)
        {
            var origin = (interactor as IXRRayProvider)?.GetOrCreateRayOrigin();
            Vector3 from, along;
            if (origin != null) { from = origin.position; along = origin.forward; }
            else if (head != null) { from = head.position; along = head.forward; }
            else return null;
            TableSocket best = null;
            float bestOffset = 0.3f;
            for (int i = 0; i < socketCount; i++)
            {
                Vector3 to = sockets[i].anchor.position - from;
                if (Vector3.Dot(to, along) < 0.2f) continue;                      // behind you
                float offset = Vector3.Cross(along, to).magnitude;                 // how far the spot is from the ray
                if (offset < bestOffset) { bestOffset = offset; best = sockets[i]; }
            }
            return best;
        }

        TableSocket NearestSocket(Vector3 position)
        {
            TableSocket best = null;
            float bestDistance = snapRadius;
            for (int i = 0; i < socketCount; i++)
            {
                float d = Vector3.Distance(position, sockets[i].anchor.position);
                if (d < bestDistance) { bestDistance = d; best = sockets[i]; }
            }
            return best;
        }

        void Toast(string message, float seconds)
        {
            if (toastText == null || toastBox == null) return;
            toastText.text = message;
            toastBox.SetActive(true);
            toastUntil = Time.unscaledTime + seconds;
        }

        void Play(AudioClip clip)
        {
            if (sfx != null && clip != null) sfx.PlayOneShot(clip);
        }

        static void Haptic(IXRSelectInteractor interactor, float amplitude, float duration)
        {
            var component = interactor as Component;
            if (component == null) return;
            var player = component.GetComponentInParent<HapticImpulsePlayer>();
            if (player != null) player.SendHapticImpulse(amplitude, duration);
        }

        void HapticAll(float amplitude, float duration)
        {
            foreach (var hand in hands) Haptic(hand, amplitude, duration);
        }

        static AudioClip Tone(string name, float from, float to, float seconds, float volume, bool square)
        {
            const int rate = 44100;
            int count = (int)(rate * seconds);
            var data = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                phase += 2f * Mathf.PI * Mathf.Lerp(from, to, t) / rate;
                float wave = Mathf.Sin(phase);
                if (square) wave = Mathf.Sign(wave) * 0.5f;
                data[i] = wave * volume * Mathf.Exp(-3.5f * t);
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// A burst of noise that dies away: a splash.
        static AudioClip Noise(string name, float seconds, float volume)
        {
            const int rate = 44100;
            int count = (int)(rate * seconds);
            var data = new float[count];
            var random = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                data[i] = ((float)random.NextDouble() * 2f - 1f) * volume * Mathf.Exp(-6f * t);
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// Asks the controller before a sphere may be picked up.
        sealed class GateFilter : IXRSelectFilter
        {
            readonly LineLevelController owner;
            readonly FetchSphere sphere;

            public GateFilter(LineLevelController owner, FetchSphere sphere)
            {
                this.owner = owner;
                this.sphere = sphere;
            }

            public bool canProcess => true;

            public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => owner.CanGrab(sphere, interactor);
        }
    }
}
