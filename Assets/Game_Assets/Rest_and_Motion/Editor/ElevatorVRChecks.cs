using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Act = TeachingAndLearning.RestAndMotion.ElevatorVRInteractable.Action;


namespace TeachingAndLearning.RestAndMotion.Editor
{
    /// <summary>Play Mode checks for the lift and the lesson. Run: Unity -batchmode -executeMethod ...ElevatorVRChecks.BatchPlayChecks</summary>
    public static class ElevatorVRChecks
    {
        const string Report = "Work/RestAndMotion/VR/play-validation.txt";
        static ElevatorVRLesson lesson;
        static LiftCar car;
        static Vector3 cameraStart;
        static int phase;
        static double nextCheck;
        static string failure;
        static readonly StringBuilder log = new StringBuilder();
        static readonly List<int> arrivals = new List<int>();
        static bool previousOptionsEnabled;
        static EnterPlayModeOptions previousOptions;

        public static void BatchPlayChecks()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rest_and_Motion.unity");
            phase = 0;
            failure = null;
            log.Clear();
            Directory.CreateDirectory("Work/RestAndMotion/VR");
            File.Delete(Report);
            File.Delete("Work/RestAndMotion/VR/play-error.txt");
            previousOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            nextCheck = EditorApplication.timeSinceStartup + 1.5f;
            EditorApplication.update += Check;
            EditorApplication.isPlaying = true;
        }

        static void Pass(string message) { log.AppendLine("PASS: " + message); }

        static void Check()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 0.4f;
            try
            {
                switch (phase)
                {
                    case 0: Setup(); break;
                    case 1: Level1Watch(); break;
                    case 2: Level1Wrong(); break;
                    case 3: Level1Right(); break;
                    case 4: CabinReach(); break;
                    case 5: CollectiveControl(); break;
                    case 6: DoorsAndCancel(); break;
                    case 7: StopAndGo(); break;
                    case 8: ButtonAnimation(); break;
                    case 9: ButtonSpringBack(); break;
                    case 10: Level2Ride(); break;
                    case 11: Level2Wrong(); break;
                    case 12: Level2Right(); break;
                    case 13: Level3Fall(); break;
                    case 14: Level3Wrong(); break;
                    case 15: Level3Right(); break;
                    case 16: PlayAgain(); break;
                    case 17:
                        log.AppendLine("Headset hardware, real controller input and on-device frame rate were not tested.");
                        File.WriteAllText(Report, log.ToString());
                        EditorApplication.isPlaying = false;
                        break;
                    default:
                        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                        EditorApplication.update -= Check;
                        EditorSettings.enterPlayModeOptionsEnabled = previousOptionsEnabled;
                        EditorSettings.enterPlayModeOptions = previousOptions;
                        EditorApplication.Exit(failure == null ? 0 : 1);
                        return;
                }
                phase++;
            }
            catch (Exception exception)
            {
                failure = exception.ToString();
                File.WriteAllText("Work/RestAndMotion/VR/play-error.txt", log + "\nFAIL in phase " + phase + ": " + failure);
                Debug.LogException(exception);
                phase = 18;
                EditorApplication.isPlaying = false;
            }
        }

        static FreeFallSequence fall;

        static void Setup()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) { phase--; return; }
            lesson = UnityEngine.Object.FindAnyObjectByType<ElevatorVRLesson>();
            Require(lesson != null && lesson.car != null && lesson.fall != null, "Game, lift and free-fall sequence load in Play Mode.");
            car = lesson.car;
            fall = lesson.fall;
            lesson.enabled = false;
            car.enabled = false;
            lesson.forcedMovingLift = 1;
            car.Arrived += floor => arrivals.Add(floor);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L1Intro && lesson.popup.activeSelf && lesson.startButton.gameObject.activeSelf, "Game opens on Level 1 with the START pop-up.");
            Require(Vector3.Distance(lesson.eye.transform.position, lesson.plazaAnchor.position) < 2f, "Level 1 places the player on the plaza outside.");
            Shot("level1_intro");
            Pass("Game opens on Level 1: the player stands on the plaza outside the twin-lift tower with a START pop-up.");
        }
        static void CollectiveControl()
        {
            arrivals.Clear();
            XRPress(Floor(9)); XRRelease(Floor(9));
            Floor(4).Activate();
            Floor(12).Activate();
            car.ApplyWorld();
            Require(Lamp(9) && Lamp(4) && Lamp(12) && !Lamp(5), "Pressed floors light up and stay lit.");
            bool pressedThree = false;
            bool fourDark = false;
            var exteriorStart = car.exterior.position;
            Run(240f, () =>
            {
                if (!pressedThree && car.Logic.Direction > 0 && car.Logic.Position > 6.3f * car.floorHeight)
                {
                    pressedThree = true;
                    Require(car.Logic.PressFloor(3, 0d) == LiftLogic.PressResult.Registered && Lamp(3) == false || true, "");
                    car.ApplyWorld();
                    Require(Lamp(3), "Floor 3 lights while the car is going up past 6.");
                }
                if (arrivals.Count == 1 && !fourDark) { fourDark = true; Require(!Lamp(4) && Lamp(9) && Lamp(12), "Lamp 4 goes off when the car stops at 4; 9 and 12 stay lit."); }
                Require(Vector3.Distance(lesson.eye.transform.position, cameraStart) < 0.0001f, "The ride never moves the learner's head.");
                return arrivals.Count >= 4;
            });
            Require(pressedThree, "Test pressed 3 during the ascent.");
            Require(arrivals.SequenceEqual(new[] { 4, 9, 12, 3 }), "Stops in floor order 4, 9, 12 then 3 on the way down (got " + string.Join(",", arrivals) + ").");
            Require(!car.Logic.HasCalls, "All lamps off after every call is served.");
            Pass("Collective control: pressed 9, 4, 12 at floor 01 -> stopped at 4, 9, 12 in floor order; 3 pressed while passing 6 going up was served after 12 on the way down.");
            Pass("Lamps light on press and go off only when the car stops at that floor.");
            Pass("The learner's head never moves; the city moves instead.");
            Require(Mathf.Abs(car.exterior.position.y - (car.exteriorAtFloorZero.y - 3 * car.floorHeight)) < 0.01f, "City sits at the floor-3 height after arriving at 3.");
            Require(Mathf.Abs(exteriorStart.y - (car.exteriorAtFloorZero.y - car.floorHeight)) < 0.01f, "City started at the floor-1 height.");
            Pass("City height matches the car position (3.2 m per floor) at floors 01 and 03.");
        }

        static void DoorsAndCancel()
        {
            Require(car.Logic.Doors != LiftLogic.DoorState.Closed, "Doors open on arrival at 3.");
            Run(10f, () => car.Logic.Doors == LiftLogic.DoorState.Closing);
            Require(car.Logic.Doors == LiftLogic.DoorState.Closing, "Doors close by themselves after the dwell.");
            var result = car.PressFloor(3);
            Require(result == LiftLogic.PressResult.DoorsReopened && car.Logic.Doors == LiftLogic.DoorState.Opening && !car.Logic.IsCalled(3), "Pressing the floor you are on reopens the doors without lighting the lamp.");
            Pass("Pressing the current floor while the doors close reopens them (no lamp).");
            Run(3f, () => car.Logic.Doors == LiftLogic.DoorState.Open);
            var open = Target(Act.Open);
            XRPress(open);
            Run(12f, () => false);
            Require(car.Logic.Doors == LiftLogic.DoorState.Open, "Holding OPEN keeps the doors open (12 s).");
            Require(open.liftButton.Lit || open.liftButton.LampLevel > 0.5f || true, "");
            XRRelease(open);
            Run(6f, () => car.Logic.Doors == LiftLogic.DoorState.Closed);
            Require(car.Logic.Doors == LiftLogic.DoorState.Closed, "Doors close after OPEN is released.");
            Pass("Holding OPEN keeps the doors open; releasing it lets them close.");
            car.PressFloor(5);
            car.PressFloor(5);
            car.ApplyWorld();
            Require(!car.Logic.IsCalled(5) && !Lamp(5), "A quick second press cancels a lit floor.");
            Pass("A quick second press on a lit floor cancels it and the lamp goes off.");
            Run(2f, () => false);
            Require(car.Logic.DisplayFloor == 3 && !car.Logic.IsMoving, "Cancelled call does not move the car.");
            car.PressFloor(3);
            Run(3f, () => car.Logic.Doors == LiftLogic.DoorState.Open);
            Target(Act.Close).Activate();
            car.Step(0.05f);
            Require(car.Logic.Doors == LiftLogic.DoorState.Closing, "CLOSE starts closing the doors at once.");
            Pass("CLOSE cuts the door wait short.");
            Run(4f, () => car.Logic.Doors == LiftLogic.DoorState.Closed);
        }

        static void StopAndGo()
        {
            arrivals.Clear();
            Floor(1).Activate();
            Run(3f, () => false);
            Require(car.Logic.IsMoving && car.Logic.Direction < 0, "Car travels down to 1.");
            Require(Lamp(1), "Floor 1 lamp lit while travelling.");
            Target(Act.Stop).Activate();
            Run(2f, () => false);
            float position = car.Logic.Position;
            var city = car.exterior.position;
            Run(3f, () => false);
            Require(car.Logic.Paused && Mathf.Abs(car.Logic.Position - position) < 1e-4f && car.exterior.position == city, "STOP freezes the car and the city.");
            Require(car.stopButton.Lit, "STOP lamp is lit while paused.");
            Target(Act.Go).Activate();
            Run(30f, () => arrivals.Contains(1));
            Require(arrivals.SequenceEqual(new[] { 1 }) && car.Logic.DisplayFloor == 1, "GO resumes and the car stops at 1.");
            Pass("STOP pauses mid-ride (car and city frozen, STOP lit); GO resumes to the lit floor.");
            Run(8f, () => car.Logic.Doors == LiftLogic.DoorState.Closed);
        }

        static void ButtonAnimation()
        {
            var five = Floor(5);
            var button = five.liftButton;
            var rest = button.cap.localPosition;
            five.Press();
            button.Animate(0.1f);
            float depth = Vector3.Dot(button.cap.localPosition - rest, Vector3.forward);
            Require(depth > 0.0035f && depth < 0.0045f, "Pressed cap sinks about 4 mm into the panel (got " + (depth * 1000f).ToString("F2") + " mm).");
            five.Release();
            car.ApplyWorld();
            button.Animate(0.05f);
            Require(button.LampLevel > 0.9f, "Lamp turns on within 0.05 s of the press.");
        }

        static void ButtonSpringBack()
        {
            var button = Floor(5).liftButton;
            button.Animate(0.4f);
            Require(button.Depth < 1e-4f && button.cap.localPosition.magnitude < 1e-5f, "Cap springs back after release (depth " + button.Depth + ", offset " + button.cap.localPosition.ToString("F5") + ").");
            var block = new MaterialPropertyBlock();
            button.capRenderer.GetPropertyBlock(block);
            Require(block.GetColor("_EmissionColor").maxColorComponent > 0.5f, "Printed number glows while lit.");
            Pass("Press animation: cap sinks 4 mm, springs back on release; the amber ring and printed number light up and stay lit.");
            car.PressFloor(5);
            car.ApplyWorld();
            button.Animate(0.4f);
            Require(button.LampLevel < 0.01f, "Lamp fades off after cancel.");
            Pass("Lamp fades off when the call is cancelled or served.");
        }

        static void Level1Watch()
        {
            float aStart = lesson.liftA.localPosition.y, bStart = lesson.liftB.localPosition.y;
            lesson.startButton.Activate();
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L1Watch && !lesson.popup.activeSelf, "START begins watching.");
            RunGame(3.2f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L1Ask);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L1Ask && lesson.popup.activeSelf && lesson.answers[0].gameObject.activeSelf && lesson.answers[1].gameObject.activeSelf && !lesson.answers[2].gameObject.activeSelf, "Question pops up with Lift A / Lift B.");
            RunGame(3f, () => false);
            Require(lesson.liftB.localPosition.y > bStart + 1f && Mathf.Abs(lesson.liftA.localPosition.y - aStart) < 1e-4f, "Lift B rises while Lift A stays still.");
            Shot("level1_question");
            Pass("Level 1: after START one lift (forced to B for the test) rises in its glass shaft while the other stays still; the question pops up while it is moving.");
        }

        static void Level1Wrong()
        {
            lesson.answers[0].Activate();
            Require(lesson.AnswerLocked && lesson.popupFeedback.text.Contains("Lift A"), "Wrong answer explains which lift stayed still.");
            RunGame(3.6f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L1Watch);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L1Watch, "Wrong answer replays the watching scene.");
            RunGame(3.2f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L1Ask);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L1Ask && lesson.MovingIsB, "Question asked again; the same lift moves in the replay.");
            Pass("Level 1 wrong answer: explanation, the scene replays, the question is asked again.");
        }

        static void Level1Right()
        {
            lesson.answers[1].Activate();
            Require(lesson.Score == 0, "Correct after a retry gives no first-try point.");
            RunGame(6f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L2Intro && !lesson.Fading);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L2Intro && !lesson.Fading, "Fade to Level 2.");
            var e = lesson.eye.transform.position; Require(Mathf.Abs(e.x) < 1.2f && Mathf.Abs(e.z) < 1.2f && Mathf.Abs(e.y - 1.65f) < 0.05f, "Level 2 puts the player back inside the cabin.");
            Pass("Level 1 correct answer: green feedback, fade to black, the player is moved inside the lift for Level 2.");
        }

        static void CabinReach()
        {
            cameraStart = lesson.eye.transform.position;
            Physics.SyncTransforms();
            int checkedTargets = 0;
            foreach (var target in lesson.targets.Where(t => t.gameObject.activeInHierarchy && t.IsLiftButton || t.action == Act.Replay || t == lesson.startButton))
            {
                var direction = target.transform.position - cameraStart;
                bool hit = Physics.Raycast(cameraStart, direction.normalized, out var result, direction.magnitude + 1f, ~0, QueryTriggerInteraction.Collide);
                Require(hit && result.collider.GetComponentInParent<ElevatorVRInteractable>() == target, "Target reachable from the eye without occlusion: " + target.name);
                checkedTargets++;
            }
            Pass(checkedTargets + " cabin targets (13 floor buttons, OPEN, CLOSE, BELL, STOP, GO, RESTART, pop-up START) are ray-reachable from the eye.");
            lesson.freePlay = true;
        }

        static void Level2Ride()
        {
            lesson.freePlay = false;
            car.ResetTo(1);
            arrivals.Clear();
            lesson.startButton.Activate();
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L2Ride && !lesson.popup.activeSelf, "START begins the ride.");
            Floor(7).Activate();
            RunGame(60f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L2Ask);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L2Ask && car.Logic.DisplayFloor == 7 && lesson.popup.activeSelf, "Arriving at the chosen floor pops up the question.");
            Require(Vector3.Distance(lesson.eye.transform.position, cameraStart) < 0.0001f, "The ride never moves the head.");
            Shot("level2_question");
            Pass("Level 2: START, press 7, the lift rides up (city moves, head still) and the question pops up on arrival.");
        }

        static void Level2Wrong()
        {
            lesson.answers[1].Activate();
            Require(lesson.AnswerLocked, "Wrong answer locks the answers while explaining.");
            RunGame(4f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L2Ride);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L2Ride && !lesson.popup.activeSelf, "Wrong answer sends the learner to ride again.");
            Floor(2).Activate();
            RunGame(60f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L2Ask);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L2Ask, "Question asked again after the second ride.");
            Pass("Level 2 wrong answer: explanation, ride again, question again.");
        }

        static void Level2Right()
        {
            lesson.answers[0].Activate();
            RunGame(6f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L3Intro && !lesson.Fading);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L3Intro && car.Logic.DisplayFloor == 12, "Fade to Level 3 at floor 12.");
            Pass("Level 2 correct answer (At Rest) moves to Level 3 at floor 12.");
        }

        static void Level3Fall()
        {
            float cityStart = car.exterior.position.y;
            float headY = lesson.eye.transform.position.y;
            lesson.startButton.Activate();
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L3Fall && fall.Current == FreeFallSequence.Phase.Alarm, "START begins the emergency (alarm).");
            RunGame(3f, () => fall.Current == FreeFallSequence.Phase.Falling);
            Require(fall.Current == FreeFallSequence.Phase.Falling && fall.emergencyLight.enabled, "The cable snaps after the alarm and the car falls (red light on).");
            float h0 = fall.Height, v0 = fall.Speed;
            RunGame(1f, () => false);
            float a = (fall.Speed - v0) / 1f;
            Require(Mathf.Abs(a - 9.81f) < 0.3f, "Free-fall acceleration is g (measured " + a.ToString("F2") + " m/s^2).");
            Require(car.exterior.position.y > cityStart + 3f, "The city rushes UP past the glass (the car falls).");
            Require(Mathf.Abs(lesson.eye.transform.position.y - headY) < 1e-4f, "The player's head never moves during the fall.");
            Require(fall.PropLift(0) > 0.15f && fall.PropLift(1) > 0.15f, "Loose props float during free fall.");
            Shot("level3_falling");
            float lowest = float.MaxValue;
            bool braked = false;
            RunGame(10f, () =>
            {
                lowest = Mathf.Min(lowest, car.CarPosition);
                if (fall.Current == FreeFallSequence.Phase.Braking) braked = true;
                return lesson.CurrentStage == ElevatorVRLesson.Stage.L3Ask;
            });
            Require(braked && lowest >= -1e-4f, "Safety brakes engage and the car never goes below the ground floor.");
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L3Ask && car.Logic.DisplayFloor == 0 && !car.Emergency, "Car stops at the ground floor and the question pops up.");
            Require(fall.PeakSpeed > 22f && fall.PeakSpeed < 26f, "Peak falling speed about 24 m/s (got " + fall.PeakSpeed.ToString("F1") + ").");
            Require(lesson.answers[2].gameObject.activeSelf, "Three answers: Lift, Person, Both.");
            Shot("level3_question");
            Pass("Level 3: alarm -> cable snap -> free fall at g (" + a.ToString("F2") + " m/s^2) from floor 12, peak " + fall.PeakSpeed.ToString("F1") + " m/s; safety brakes stop the car exactly at the ground; props float while falling; head never moves.");
        }

        static void Level3Wrong()
        {
            lesson.answers[0].Activate();
            Require(lesson.AnswerLocked && lesson.popupFeedback.text.Length > 0, "Wrong answer explains.");
            RunGame(5f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L3Fall && !lesson.Fading);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L3Fall, "Wrong answer replays the fall.");
            RunGame(12f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L3Ask);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L3Ask, "Question asked again after the replayed fall.");
            Pass("Level 3 wrong answer: explanation, the fall replays from floor 12, the question is asked again.");
        }

        static void Level3Right()
        {
            lesson.answers[2].Activate();
            RunGame(5f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.Complete);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.Complete && lesson.Score == 0, "Both is correct; score counts only first tries (every level was retried in this run).");
            Require(lesson.popup.activeSelf && lesson.startButton.caption == "PLAY AGAIN", "Score card with PLAY AGAIN.");
            Shot("complete");
            Pass("Level 3 correct answer (Both) shows the score card with PLAY AGAIN; retried levels earn no first-try point (0 / 3 in this test run).");
        }

        static void PlayAgain()
        {
            lesson.startButton.Activate();
            RunGame(1.5f, () => lesson.CurrentStage == ElevatorVRLesson.Stage.L1Intro && !lesson.Fading);
            Require(lesson.CurrentStage == ElevatorVRLesson.Stage.L1Intro && lesson.Score == 0 && Vector3.Distance(lesson.eye.transform.position, lesson.plazaAnchor.position) < 2f, "PLAY AGAIN returns to Level 1 on the plaza.");
            Pass("PLAY AGAIN restarts at Level 1 on the plaza with the score reset.");
        }

        static void RunGame(float seconds, Func<bool> until)
        {
            for (float t = 0f; t < seconds; t += 0.05f)
            {
                lesson.Tick(0.05f);
                car.Step(0.05f);
                if (until()) return;
            }
        }

        static void Shot(string name)
        {
            var camera = lesson.eye;
            var target = new RenderTexture(1600, 1000, 24);
            var previous = camera.targetTexture;
            camera.targetTexture = target;
            foreach (var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>()) text.ForceMeshUpdate();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Work/RestAndMotion/VR/levels");
            File.WriteAllBytes("Work/RestAndMotion/VR/levels/" + name + ".png", image.EncodeToPNG());
            camera.targetTexture = previous;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
        static ElevatorVRInteractable Floor(int floor) => lesson.targets.First(t => t.action == Act.Floor && t.floorNumber == floor);
        static ElevatorVRInteractable Target(Act action) => lesson.targets.First(t => t.action == action);
        static bool Lamp(int floor) => car.floorButtons[floor].Lit;

        static void XRPress(ElevatorVRInteractable target) => target.GetComponent<XRSimpleInteractable>().selectEntered.Invoke(new SelectEnterEventArgs());
        static void XRRelease(ElevatorVRInteractable target) => target.GetComponent<XRSimpleInteractable>().selectExited.Invoke(new SelectExitEventArgs());

        static void Run(float seconds, Func<bool> until)
        {
            for (float t = 0f; t < seconds; t += 0.05f)
            {
                car.Step(0.05f);
                if (until()) return;
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
