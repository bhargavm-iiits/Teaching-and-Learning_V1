using System;
using TMPro;
using UnityEngine;

namespace TeachingAndLearning.RestAndMotion
{
    /// <summary>
    /// Rest and Motion, 3 levels:
    /// 1 Which lift is moving? (watch two lifts from the plaza outside),
    /// 2 Inside the moving lift (ride, then: at rest relative to the lift?),
    /// 3 Emergency free fall (who moved relative to the ground?).
    /// A wrong answer explains and replays the level's scene; a right answer fades to the next level.
    /// </summary>
    public sealed class ElevatorVRLesson : MonoBehaviour
    {
        public enum Stage { L1Intro, L1Watch, L1Ask, L2Intro, L2Ride, L2Ask, L3Intro, L3Fall, L3Ask, Complete }

        [Header("World")]
        public LiftCar car;
        public FreeFallSequence fall;
        public ScreenFade fade;
        public Transform exterior;
        public Camera eye;
        public Transform rig;
        public ElevatorDesktopPlayer desktop;
        public Transform plazaAnchor;
        public Vector3 plazaWalkSize = new Vector3(9f, 4f, 7f);
        public Transform liftA;
        public Transform liftB;
        public float twinLiftTravel = 22.4f;

        [Header("Cabin board")]
        public TMP_Text progress;
        public TMP_Text prompt;
        public TMP_Text feedback;
        public Renderer[] stepBadges = new Renderer[3];
        public TMP_Text[] stepLabels = new TMP_Text[3];

        [Header("Pop-up")]
        public GameObject popup;
        public Transform popupPlazaAnchor;
        public Transform popupCabinAnchor;
        public TMP_Text popupTitle;
        public TMP_Text popupText;
        public TMP_Text popupFeedback;
        public ElevatorVRInteractable startButton;
        public ElevatorVRInteractable[] answers = new ElevatorVRInteractable[3];
        public ElevatorVRInteractable[] targets;

        [Header("Testing")]
        public bool freePlay;
        public int forcedMovingLift = -1;

        public Stage CurrentStage { get; private set; }
        public int Score { get; private set; }
        public bool MovingIsB { get; private set; }
        public bool AnswerLocked { get; private set; }
        public bool Fading => fadePhase != 0;
        public float MovingLiftHeight { get; private set; }
        public ElevatorVRInteractable Landmark => null;

        readonly bool[] firstTry = { true, true, true };
        Vector3 cabinRigPosition;
        Quaternion cabinRigRotation;
        Bounds cabinWalk;
        bool cabinPoseKnown;
        float liftABase, liftBBase;
        float watchTime;
        int rideFrom = -1;
        float waitTimer;
        Action afterWait;
        int fadePhase;
        float fadeTime;
        Action fadeMiddle;
        MaterialPropertyBlock block;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color Done = new Color(0.2f, 0.75f, 0.35f);
        static readonly Color Active = new Color(0.18f, 0.44f, 0.88f);
        static readonly Color Pending = new Color(0.2f, 0.24f, 0.32f);

        void Awake() { InitializeLesson(); }
        void Update() { Tick(Time.deltaTime); }

        public void InitializeLesson()
        {
            if (!cabinPoseKnown && rig != null)
            {
                cabinRigPosition = rig.position;
                cabinRigRotation = rig.rotation;
                if (desktop != null) cabinWalk = desktop.walkBounds;
                cabinPoseKnown = true;
            }
            if (liftA != null) liftABase = liftA.localPosition.y;
            if (liftB != null) liftBBase = liftB.localPosition.y;
            if (car != null)
            {
                car.Initialise();
                car.Arrived -= OnArrived; car.Arrived += OnArrived;
                car.Departed -= OnDeparted; car.Departed += OnDeparted;
            }
            if (fall != null) { fall.Finished -= OnFallFinished; fall.Finished += OnFallFinished; }
            ResetGame();
        }

        /// <summary>Back to Level 1 at once (no fade).</summary>
        public void ResetGame()
        {
            Score = 0;
            for (int i = 0; i < firstTry.Length; i++) firstTry[i] = true;
            waitTimer = 0f; afterWait = null;
            if (fadePhase != 1) { fadePhase = 0; if (fade != null) fade.SetAlpha(0f); }
            if (fall != null) fall.Clear();
            EnterLevel1();
        }

        // ---------------- per-frame ----------------
        public void Tick(float dt)
        {
            if (fadePhase == 1)
            {
                fadeTime += dt;
                if (fade != null) fade.SetAlpha(fadeTime / 0.4f);
                if (fadeTime >= 0.4f)
                {
                    var middle = fadeMiddle; fadeMiddle = null;
                    middle?.Invoke();
                    fadePhase = 2; fadeTime = 0f;
                }
            }
            else if (fadePhase == 2)
            {
                fadeTime += dt;
                if (fade != null) fade.SetAlpha(1f - fadeTime / 0.4f);
                if (fadeTime >= 0.4f) { fadePhase = 0; if (fade != null) fade.SetAlpha(0f); }
            }
            if (waitTimer > 0f)
            {
                waitTimer -= dt;
                if (waitTimer <= 0f) { var next = afterWait; afterWait = null; next?.Invoke(); }
            }
            if (CurrentStage == Stage.L1Watch || CurrentStage == Stage.L1Ask)
            {
                watchTime += dt;
                AnimateTwinLifts();
                if (CurrentStage == Stage.L1Watch && watchTime >= 3f) AskLevel1();
            }
            if (CurrentStage == Stage.L3Fall && fall != null) fall.Step(dt);
        }

        void Wait(float seconds, Action next) { waitTimer = Mathf.Max(0.001f, seconds); afterWait = next; }
        void FadeThen(Action middle) { fadePhase = 1; fadeTime = 0f; fadeMiddle = middle; }

        // ---------------- Level 1 ----------------
        void EnterLevel1()
        {
            if (car != null) car.ResetTo(1);
            MovingLiftHeight = 0f;
            SetLift(liftA, liftABase, 0f);
            SetLift(liftB, liftBBase, 0f);
            if (plazaAnchor != null) PlaceRig(plazaAnchor.position, plazaAnchor.rotation, new Bounds(plazaAnchor.position, plazaWalkSize));
            AttachPopup(popupPlazaAnchor);
            CurrentStage = Stage.L1Intro;
            ShowIntro("LEVEL 1 - Which lift is moving?", "You are outside the building, looking at Lift A and Lift B.\nOne of them is going to move. Watch both, then answer.\nPress START.", "START");
            SetBoard("Level 1: watch the two lifts from outside.", "");
        }

        void StartLevel1Watch()
        {
            watchTime = 0f;
            SetLift(liftA, liftABase, 0f);
            SetLift(liftB, liftBBase, 0f);
            HidePopup();
            CurrentStage = Stage.L1Watch;
            SetBoard("Watch Lift A and Lift B.", "");
        }

        void AskLevel1()
        {
            CurrentStage = Stage.L1Ask;
            ShowQuestion("LEVEL 1", "Which lift is in motion?", "LIFT A", "LIFT B", null);
        }

        void AnimateTwinLifts()
        {
            const float rise = 10f, hold = 1.5f;
            float cycle = Mathf.Repeat(watchTime, 2f * (rise + hold));
            float s = cycle < rise ? cycle / rise : cycle < rise + hold ? 1f : cycle < 2f * rise + hold ? 1f - (cycle - rise - hold) / rise : 0f;
            s = s * s * (3f - 2f * s);
            MovingLiftHeight = s * twinLiftTravel;
            SetLift(MovingIsB ? liftB : liftA, MovingIsB ? liftBBase : liftABase, MovingLiftHeight);
            SetLift(MovingIsB ? liftA : liftB, MovingIsB ? liftABase : liftBBase, 0f);
        }

        static void SetLift(Transform lift, float baseY, float height)
        {
            if (lift == null) return;
            var p = lift.localPosition; p.y = baseY + height; lift.localPosition = p;
        }

        // ---------------- Level 2 ----------------
        void EnterLevel2()
        {
            SetLift(liftA, liftABase, 0f);
            SetLift(liftB, liftBBase, 0f);
            if (car != null) car.ResetTo(1);
            PlaceRig(cabinRigPosition, cabinRigRotation, cabinWalk);
            AttachPopup(popupCabinAnchor);
            CurrentStage = Stage.L2Intro;
            ShowIntro("LEVEL 2 - Inside the moving lift", "You are inside the lift now.\nPress START, then press any floor button on the panel (right wall) and ride.\nWatch the panel and the walls next to you.", "START");
            SetBoard("Level 2: ride the lift.", "");
        }

        void StartLevel2Ride()
        {
            rideFrom = -1;
            HidePopup();
            CurrentStage = Stage.L2Ride;
            SetBoard("Press any floor button at least 2 floors away and ride.", "Watch the panel beside you while the city slides past.");
        }

        void OnDeparted(int from, int to)
        {
            if (CurrentStage == Stage.L2Ride && rideFrom < 0) rideFrom = from;
        }

        void OnArrived(int floor)
        {
            if (CurrentStage != Stage.L2Ride) return;
            if (rideFrom >= 0 && Mathf.Abs(floor - rideFrom) >= 2)
            {
                rideFrom = -1;
                SetBoard("Floor " + LiftCar.FloorName(floor) + ".", "");
                Wait(1.5f, () => { CurrentStage = Stage.L2Ask; ShowQuestion("LEVEL 2", "While the lift was moving, were you at rest or in motion relative to the LIFT?", "AT REST", "IN MOTION", null); });
            }
            else
            {
                rideFrom = -1;
                SetBoard("That ride was very short.", "Choose a floor at least 2 floors away.");
            }
        }

        // ---------------- Level 3 ----------------
        void EnterLevel3()
        {
            if (fall != null) fall.Clear();
            if (car != null) car.ResetTo(LiftLogic.MaxFloor);
            PlaceRig(cabinRigPosition, cabinRigRotation, cabinWalk);
            AttachPopup(popupCabinAnchor);
            CurrentStage = Stage.L3Intro;
            ShowIntro("LEVEL 3 - Emergency free fall", "You are at floor 12. The lift is making strange noises...\nPress START and hold on to the rail.", "START");
            SetBoard("Level 3: emergency.", "");
        }

        void StartFall()
        {
            HidePopup();
            CurrentStage = Stage.L3Fall;
            SetBoard("EMERGENCY! The cable has snapped!", "The lift falls freely. Watch the city and the loose ball and clipboard.");
            if (fall != null) fall.Begin();
        }

        void OnFallFinished()
        {
            if (car != null) car.EndEmergency(0);
            SetBoard("The safety brakes stopped the lift at the ground floor.", "");
            Wait(1f, () => { CurrentStage = Stage.L3Ask; ShowQuestion("LEVEL 3", "During the fall, who was in motion relative to the GROUND?", "LIFT", "PERSON", "BOTH"); });
        }

        // ---------------- answers ----------------
        void Answer(int index)
        {
            if (AnswerLocked) return;
            switch (CurrentStage)
            {
                case Stage.L1Ask:
                {
                    string moving = MovingIsB ? "Lift B" : "Lift A", still = MovingIsB ? "Lift A" : "Lift B";
                    if (index == (MovingIsB ? 1 : 0)) Correct(0, "Correct! " + moving + " changes its position compared with the building, so it is in motion. " + still + " stays where it is: at rest.", EnterLevel2);
                    else Wrong(0, "Look again: " + still + " is not changing its position compared with the building. Watch both lifts once more.", StartLevel1Watch);
                    break;
                }
                case Stage.L2Ask:
                    if (index == 0) Correct(1, "Correct! Relative to the lift you were AT REST. But you AND the lift were both moving relative to the building.", EnterLevel3);
                    else Wrong(1, "The panel and the walls stayed the same distance from you during the whole ride, so relative to the lift you did not move. Ride again and watch the panel.", StartLevel2Ride);
                    break;
                case Stage.L3Ask:
                    if (index == 2) Correct(2, "Correct! BOTH the lift and the person fell relative to the ground. Relative to the lift the person was at rest, which is why the loose things floated along with you.", ShowComplete);
                    else Wrong(2, (index == 0 ? "You were inside the lift and fell with it, so the person moved too." : "The lift moved as well: it fell from floor 12 to the ground.") + " Watch the fall again.", ReplayFall);
                    break;
            }
        }

        void Correct(int level, string message, Action next)
        {
            if (firstTry[level]) Score++;
            AnswerLocked = true;
            SetAnswersVisible(false);
            SetPopupFeedback(message, new Color(0.45f, 1f, 0.55f));
            if (car != null) car.PlayChime();
            Refresh();
            Wait(level == 2 ? 3.5f : 4f, () => { if (next == ShowComplete) next(); else FadeThen(next); });
        }

        void Wrong(int level, string message, Action replay)
        {
            firstTry[level] = false;
            AnswerLocked = true;
            SetAnswersVisible(false);
            SetPopupFeedback(message, new Color(1f, 0.75f, 0.35f));
            Wait(3.5f, () => { if (replay == ReplayFall) FadeThen(replay); else replay(); });
        }

        void ReplayFall()
        {
            if (fall != null) fall.Clear();
            if (car != null) car.ResetTo(LiftLogic.MaxFloor);
            StartFall();
        }

        void ShowComplete()
        {
            CurrentStage = Stage.Complete;
            string Mark(int i) => firstTry[i] ? "first try" : "after a retry";
            ShowIntro("GAME COMPLETE - Score " + Score + " / 3",
                "Level 1: " + Mark(0) + "\nLevel 2: " + Mark(1) + "\nLevel 3: " + Mark(2) +
                "\nMotion depends on the reference point: at rest relative to the lift, moving relative to the building and the ground.", "PLAY AGAIN");
            SetBoard("Game complete: " + Score + " / 3 first try.", "Ride the lift freely, or press PLAY AGAIN.");
        }

        // ---------------- input ----------------
        bool InCabinStage => CurrentStage == Stage.L2Intro || CurrentStage == Stage.L2Ride || CurrentStage == Stage.L2Ask || CurrentStage == Stage.Complete;

        public bool IsAvailable(ElevatorVRInteractable target)
        {
            if (Fading) return false;
            switch (target.action)
            {
                case ElevatorVRInteractable.Action.Answer: return !AnswerLocked && (CurrentStage == Stage.L1Ask || CurrentStage == Stage.L2Ask || CurrentStage == Stage.L3Ask);
                case ElevatorVRInteractable.Action.Ready: return popup != null && popup.activeSelf && startButton != null && startButton.gameObject.activeSelf;
                case ElevatorVRInteractable.Action.Replay: return true;
                case ElevatorVRInteractable.Action.Bell: return true;
                case ElevatorVRInteractable.Action.Landmark: return false;
                default: return freePlay || InCabinStage;
            }
        }

        public void OnPressed(ElevatorVRInteractable target)
        {
            if (!IsAvailable(target))
            {
                if (target.IsLiftButton && CurrentStage == Stage.L3Fall) SetBoard("EMERGENCY! The buttons are not responding!", "");
                return;
            }
            switch (target.action)
            {
                case ElevatorVRInteractable.Action.Floor: FloorPressed(target.floorNumber); break;
                case ElevatorVRInteractable.Action.Open: car.PressOpen(true); break;
                case ElevatorVRInteractable.Action.Close: car.PressClose(); break;
                case ElevatorVRInteractable.Action.Bell: car.PressBell(); break;
                case ElevatorVRInteractable.Action.Stop: car.PressStop(); SetBoard(car.Logic.Paused ? "STOP: the lift pauses. Press GO to continue." : "Lift running again.", null); break;
                case ElevatorVRInteractable.Action.Go: car.PressGo(); break;
                case ElevatorVRInteractable.Action.Answer: Answer(target.answerIndex); break;
                case ElevatorVRInteractable.Action.Ready:
                    switch (CurrentStage)
                    {
                        case Stage.L1Intro: MovingIsB = forcedMovingLift >= 0 ? forcedMovingLift == 1 : UnityEngine.Random.value > 0.5f; StartLevel1Watch(); break;
                        case Stage.L2Intro: StartLevel2Ride(); break;
                        case Stage.L3Intro: StartFall(); break;
                        case Stage.Complete: FadeThen(ResetGame); break;
                    }
                    break;
                case ElevatorVRInteractable.Action.Replay: FadeThen(ResetGame); break;
            }
            Refresh();
        }

        public void OnReleased(ElevatorVRInteractable target)
        {
            if (target.action == ElevatorVRInteractable.Action.Open && car != null) car.ReleaseOpen();
        }

        void FloorPressed(int floor)
        {
            var result = car.PressFloor(floor);
            string name = LiftCar.FloorName(floor);
            switch (result)
            {
                case LiftLogic.PressResult.Registered: SetBoard(null, "Floor " + name + " lit. The lift stops there."); break;
                case LiftLogic.PressResult.Cancelled: SetBoard(null, "Floor " + name + " cancelled."); break;
                case LiftLogic.PressResult.DoorsReopened: SetBoard(null, "You are at floor " + name + ". Doors opening."); break;
            }
        }

        // ---------------- presentation ----------------
        void PlaceRig(Vector3 position, Quaternion rotation, Bounds walk)
        {
            if (desktop != null) desktop.Teleport(position, rotation.eulerAngles.y, walk);
            else if (rig != null) rig.SetPositionAndRotation(position, rotation);
        }

        void AttachPopup(Transform anchor)
        {
            if (popup == null || anchor == null) return;
            popup.transform.SetParent(anchor, false);
            popup.transform.localPosition = Vector3.zero;
            popup.transform.localRotation = Quaternion.identity;
        }

        void ShowIntro(string title, string text, string button)
        {
            AnswerLocked = false;
            if (popup != null) popup.SetActive(true);
            SetText(popupTitle, title);
            SetText(popupText, text);
            SetPopupFeedback("", Color.white);
            SetAnswersVisible(false);
            if (startButton != null) { startButton.caption = button; startButton.gameObject.SetActive(true); }
            Refresh();
        }

        void ShowQuestion(string title, string question, string a, string b, string c)
        {
            AnswerLocked = false;
            if (popup != null) popup.SetActive(true);
            SetText(popupTitle, title);
            SetText(popupText, question);
            SetPopupFeedback("", Color.white);
            if (startButton != null) startButton.gameObject.SetActive(false);
            var captions = new[] { a, b, c };
            for (int i = 0; i < answers.Length; i++)
            {
                if (answers[i] == null) continue;
                answers[i].caption = captions[i];
                answers[i].gameObject.SetActive(captions[i] != null);
            }
            SetBoard(question, null);
            Refresh();
        }

        void HidePopup() { if (popup != null) popup.SetActive(false); }

        void SetAnswersVisible(bool visible)
        {
            foreach (var answer in answers) if (answer != null && (!visible || !string.IsNullOrEmpty(answer.caption))) answer.gameObject.SetActive(visible);
        }

        void SetPopupFeedback(string message, Color color)
        {
            if (popupFeedback != null) { popupFeedback.text = message; popupFeedback.color = color; }
            if (!string.IsNullOrEmpty(message)) SetText(feedback, message);
        }

        void SetBoard(string promptText, string feedbackText)
        {
            if (promptText != null) SetText(prompt, promptText);
            if (feedbackText != null) SetText(feedback, feedbackText);
            Refresh();
        }

        int LevelIndex => CurrentStage <= Stage.L1Ask ? 0 : CurrentStage <= Stage.L2Ask ? 1 : CurrentStage <= Stage.L3Ask ? 2 : 3;

        void Refresh()
        {
            int level = LevelIndex;
            for (int i = 0; i < 3; i++)
            {
                var color = i < level ? Done : i == level ? Active : Pending;
                if (stepBadges != null && i < stepBadges.Length && stepBadges[i] != null)
                {
                    if (block == null) block = new MaterialPropertyBlock();
                    stepBadges[i].GetPropertyBlock(block);
                    block.SetColor(BaseColorId, color);
                    stepBadges[i].SetPropertyBlock(block);
                }
                if (stepLabels != null && i < stepLabels.Length && stepLabels[i] != null)
                    stepLabels[i].color = i == level ? Color.white : i < level ? new Color(0.7f, 0.9f, 0.75f) : new Color(0.55f, 0.6f, 0.7f);
            }
            SetText(progress, (level < 3 ? "LEVEL " + (level + 1) + " OF 3" : "COMPLETE") + "   |   SCORE " + Score + " / 3");
            if (targets != null) foreach (var t in targets) if (t != null) t.RefreshVisual();
        }

        static void SetText(TMP_Text label, string value) { if (label != null) label.text = value; }
    }
}
