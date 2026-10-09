using System.Collections;
using System.Collections.Generic;
using DistanceTimeGraph.Logic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DistanceTimeGraph
{
    /// Runs the topic: Introduction, then three levels (edit, run, complete or attempt recorded and a new course), the
    /// final check and its routing. Graph editing is only possible in the edit states.
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Data")]
        public DtgCatalog catalog;
        public ProgressData progress = new ProgressData();

        [Header("Systems")]
        public LevelManager level;
        public AdaptiveVariantManager variants;
        public GraphEditor graph;
        public GraphPlaybackController playback;
        public CarMotionController car;
        public CarPreviewController preview;
        public GhostReplayController ghosts;
        public TimedBridgeController bridge;
        public RearCarCameraController rearCamera;

        [Header("Interface")]
        public ActivityPanel activity;
        public ControlConsole console;
        public PopupPanel popup;
        public DimVeil veil;
        public AssessmentManager assessment;

        [Header("Next topic")]
        public UnityEvent NextTopicRequested = new UnityEvent();
        [Tooltip("Loaded after TOPIC COMPLETE when set (and in Build Settings). Falls back to the catalog's value.")]
        public string nextTopicSceneName = "";

        public GameState State { get; private set; } = GameState.Introduction;
        public VariantSpec Current { get; private set; }
        public int LevelIndex { get; private set; }

        static readonly string[] Lessons =
        {
            "An upward graph segment moved the car away from the start.",
            "A horizontal graph segment kept the car at the same distance while time continued.",
            "A descending distance-from-start graph made the car return toward the start.",
        };

        bool replaying;          // a replay started from the console or a popup
        GraphModel savedGraph;   // the learner's graph while an older attempt is replayed
        AttemptRecord lastAttempt;

        void Start()
        {
            UiKit.OnClick(console.run, RunGraph);
            UiKit.OnClick(console.pause, TogglePause);
            UiKit.OnClick(console.edit, EditGraph);
            UiKit.OnClick(console.replay, ReplayLatest);
            UiKit.OnClick(console.reset, ResetLevel);
            UiKit.OnClick(console.delete, () => graph.DeleteSelected());
            graph.Changed += OnGraphChanged;
            graph.SelectionChanged += RefreshConsole;
            assessment.Close();
            ShowIntroduction();
        }

        // ---------------- states ----------------

        void SetState(GameState s)
        {
            if (State != s) Dtg.Log($"state {State} -> {s}");
            State = s;
            graph.Editable = GameStates.CanEdit(s) && !replaying;
            preview.Active = graph.Editable;
            if (!graph.Editable) graph.Select(-1);
            graph.Refresh();
            RefreshConsole();
        }

        void ShowIntroduction()
        {
            SetState(GameState.Introduction);
            veil.SetDim(true);
            var first = variants.First(LevelKind.Rising);
            ApplyCourse(0, first);
            SetState(GameState.Introduction);
            popup.Show("DISTANCE FROM START–TIME GRAPH",
                "The graph is the car's program. Plot points on the graph to move the car.\n\n" +
                "<color=#29DBFF>Rising line</color>: the car moves away from the start.\n" +
                "<color=#FFD63D>Horizontal line</color>: the car stays in one place.\n" +
                "<color=#FF8C1F>Falling line</color>: the car moves back toward the start.",
                Palette.Cyan, ("START", () => { popup.Hide(); veil.SetDim(false); SetState(GameStates.Edit(0)); }));
        }

        /// Loads a course: the world layout, a fresh graph at the start point, ghosts of this course only.
        void ApplyCourse(int levelIndex, VariantSpec v, string banner = null)
        {
            LevelIndex = levelIndex;
            Current = v;
            level.Apply(v);
            var model = new GraphModel(catalog.maxTime, catalog.maxDistance, v.start);
            graph.SetModel(model);
            graph.ReturnHint = v.kind == LevelKind.Returning;
            graph.SetBridgeBand(v.kind == LevelKind.Waiting, v.bridgeOpen, v.bridgeClose);
            graph.SetHighlight(-1, null);
            graph.SetCursor(-1f, 0f);
            car.Teleport(v.start);
            preview.SyncFrom(car);
            // World ghosts only for attempts on this exact route; older courses keep their small replay card.
            ghosts.Show(progress.attempts.FindAll(a => a.variantId == v.id));
            string heading = $"LEVEL {levelIndex + 1} · {v.title}";
            string text = v.Instruction + (v.kind == LevelKind.Waiting ? $"\n<b>BRIDGE OPEN {v.bridgeOpen:0}–{v.bridgeClose:0} s</b>" : "");
            activity.SetActivity(heading, text);
            activity.SetBanner(banner, Palette.Blue);
            RefreshCards();
            rearCamera.Follow(car.transform, "LIVE");
            OnGraphChanged();
        }

        void BeginLevel(int levelIndex, VariantSpec v, string banner = null)
        {
            popup.Hide();
            assessment.Close();
            veil.SetDim(false);
            ApplyCourse(levelIndex, v, banner);
            SetState(GameStates.Edit(levelIndex));
        }

        // ---------------- editing ----------------

        void OnGraphChanged()
        {
            var v = Current;
            if (v == null) return;
            var model = graph.Model;
            var plan = LevelRules.Run(model, v);
            var last = model[model.Count - 1];
            switch (v.kind)
            {
                case LevelKind.Waiting:
                    bool safe = plan.success;
                    bridge.Preview(safe);
                    string arrival = plan.arrivalTime >= 0 ? $"Predicted arrival at {v.bridgeEnd} m: {plan.arrivalTime:0.#} s" : $"The plan does not reach {v.bridgeEnd} m yet";
                    activity.SetPrediction($"{arrival}\nBridge preview: {(safe ? "SAFE" : "NOT SAFE")}", safe ? Palette.Green : Palette.Orange);
                    break;
                case LevelKind.Returning:
                    activity.SetPrediction($"The graph ends at {last.d} m after {last.t} s.", Palette.SoftInk);
                    break;
                default:
                    activity.SetPrediction($"The graph ends at {last.d} m after {last.t} s.", Palette.SoftInk);
                    break;
            }
            RefreshConsole();
        }

        void Update()
        {
            if (Current == null) return;
            // Backward arrows on the road while the selected point (or the running car) returns toward the start.
            bool reversePlan = false;
            if (graph.Editable && graph.Selected > 0 && graph.Model.Slope(graph.Selected - 1) < 0)
            {
                reversePlan = true;
                level.PlaceRoadReturn(graph.Model[graph.Selected - 1].d, graph.Model[graph.Selected].d);
            }
            else if (playback.Playing && car.Reversing)
            {
                reversePlan = true;
                level.PlaceRoadReturn(car.Distance, Mathf.Max(0f, car.Distance - 20f));
            }
            if (!reversePlan) level.ShowRoadReturn(false);

            // The monitor watches the preview car while a point is being placed, otherwise the real car.
            if (preview.Visible) rearCamera.Follow(preview.previewCar.transform, "PREVIEW");
            else rearCamera.Follow(car.transform, replaying || playback.Mode == PlaybackMode.Replay && playback.Playing ? "REPLAY" : "LIVE");

            float t = playback.Playing ? playback.Time : 0f;
            string motion = car.Reversing ? "moving back" : car.Velocity > 0.05f ? "moving away" : "stopped";
            activity.SetStatus($"t = {t:0.0} s   ·   {car.Distance:0.0} m from start   ·   {motion}");
            Keys();
        }

        void RefreshConsole()
        {
            bool edit = GameStates.CanEdit(State) && !replaying;
            bool run = State == GameStates.Run(LevelIndex) && playback.Playing;
            bool anyReplay = replaying && playback.Playing;
            bool hasAttempt = progress.attempts.Exists(a => Current != null && a.variantId == Current.id);
            console.SetStates(
                canRun: edit && graph.Model.Count > 1,
                canPause: run || anyReplay,
                canEdit: run || (anyReplay && GameStates.CanEdit(State)),
                canReplay: edit && hasAttempt,
                canReset: edit,
                canDelete: edit && graph.Selected > 0,
                paused: playback.Paused);
        }

        void RefreshCards() => activity.SetCards(progress.Recent(LevelIndex), catalog.maxTime, catalog.maxDistance);

        // ---------------- console ----------------

        public void RunGraph()
        {
            if (!GameStates.CanEdit(State) || replaying || graph.Model.Count < 2) return;
            var program = graph.Model.Clone();
            var profile = LevelRules.Run(program, Current);
            graph.SetHighlight(-1, null);
            level.ResetMarkers();
            SetState(GameStates.Run(LevelIndex));
            playback.Play(profile, program, PlaybackMode.Live, () => OnRunFinished(program, profile));
            RefreshConsole();
        }

        public void TogglePause()
        {
            if (!playback.Playing) return;
            if (playback.Paused) playback.Resume(); else playback.Pause();
            RefreshConsole();
        }

        /// During a run: stop it and go back to editing (the run is not judged). During a replay: stop the replay.
        public void EditGraph()
        {
            if (replaying) { StopReplay(); return; }
            if (State != GameStates.Run(LevelIndex)) return;
            playback.Stop();
            car.Teleport(Current.start);
            ghosts.Rest();
            SetState(GameStates.Edit(LevelIndex));
        }

        public void ResetLevel()
        {
            if (!GameStates.CanEdit(State) || replaying) return;
            graph.ResetGraph(Current.start);
            car.Teleport(Current.start);
            level.ResetMarkers();
            ghosts.Rest();
            Dtg.Log("level reset");
        }

        /// Replays the newest attempt on this course: its graph, cursor, car and bridge together.
        public void ReplayLatest()
        {
            var a = progress.attempts.FindLast(x => Current != null && x.variantId == Current.id);
            if (a == null || !GameStates.CanEdit(State)) return;
            StartReplay(a, StopReplay);
        }

        void StartReplay(AttemptRecord a, System.Action done)
        {
            replaying = true;
            savedGraph ??= graph.Model;
            var shown = new GraphModel(catalog.maxTime, catalog.maxDistance, 0) { points = new List<GraphPoint>(a.points) };
            graph.SetModel(shown);
            if (!a.success) graph.SetHighlight(a.profile.causeSegment, a.profile.headline);
            SetState(State);
            playback.Play(a.profile, shown, PlaybackMode.Replay, () => done?.Invoke());
            RefreshConsole();
        }

        void StopReplay()
        {
            playback.Stop();
            replaying = false;
            if (savedGraph != null) { graph.SetModel(savedGraph); savedGraph = null; }
            graph.SetHighlight(-1, null);
            if (GameStates.CanEdit(State)) car.Teleport(Current.start);
            ghosts.Rest();
            SetState(State);
        }

        // ---------------- results ----------------

        void OnRunFinished(GraphModel program, RunProfile profile)
        {
            var record = new AttemptRecord
            {
                variantId = Current.id,
                level = LevelIndex,
                points = program.ToArray(),
                profile = profile,
                success = profile.success,
                summary = $"{Current.id} · {(profile.success ? "success" : profile.headline)}"
            };
            progress.Record(record);
            lastAttempt = record;
            Dtg.Log($"run finished on {Current.id}: {(profile.success ? "SUCCESS" : "FAIL")} - {profile.headline} {profile.detail}");
            RefreshCards();

            if (profile.success)
            {
                progress.levelComplete[LevelIndex] = true;
                progress.completedVariant[LevelIndex] = Current.id;
                ghosts.Show(progress.attempts.FindAll(a => a.variantId == Current.id));
                ShowComplete();
            }
            else
            {
                // The failed attempt stays on the road as a faded orange ghost until the next course loads.
                ghosts.Show(progress.attempts.FindAll(a => a.variantId == Current.id));
                SetState(GameState.AttemptRecorded);
                StartCoroutine(FailureSequence(record));
            }
        }

        void ShowComplete()
        {
            SetState(GameStates.Complete(LevelIndex));
            veil.SetDim(true);
            bool last = LevelIndex == 2;
            popup.Show($"LEVEL {LevelIndex + 1} COMPLETE", Lessons[LevelIndex], Palette.Green,
                (last ? "FINAL QUESTIONS" : "NEXT LEVEL", NextLevel),
                ("REPLAY SUCCESS", ReplaySuccess));
        }

        void ReplaySuccess()
        {
            popup.Hide();
            veil.SetDim(false);
            StartReplay(lastAttempt, () => { StopReplay(); ShowComplete(); });
        }

        IEnumerator FailureSequence(AttemptRecord record)
        {
            // 1. The car is already frozen safely where the run ended. 2-3. Replay once with the cursor and highlight.
            graph.SetHighlight(record.profile.causeSegment, record.profile.headline);
            yield return new WaitForSeconds(0.9f);
            bool done = false;
            StartReplay(record, () => done = true);
            while (!done) yield return null;
            playback.Stop();
            replaying = false;
            graph.SetHighlight(record.profile.causeSegment, record.profile.headline);
            ShowAttemptRecorded(record);
        }

        void ShowAttemptRecorded(AttemptRecord record)
        {
            SetState(GameState.AttemptRecorded);
            popup.Show("ATTEMPT RECORDED",
                "Observe what happened. Your next challenge will have a different route.\n\n" +
                $"<size=80%><color=#FFB060>{record.profile.headline} {record.profile.detail}</color></size>",
                Palette.Orange,
                ("WATCH RESULT", () => WatchResult(record)),
                ("CONTINUE TO NEW CHALLENGE", () => ContinueAfterFailure(record)));
        }

        void WatchResult(AttemptRecord record)
        {
            popup.Hide();
            StartReplay(record, () =>
            {
                playback.Stop();
                replaying = false;
                ShowAttemptRecorded(record);
            });
        }

        void ContinueAfterFailure(AttemptRecord record)
        {
            savedGraph = null;
            var failed = Current;
            var next = variants.AfterFailure(failed);
            BeginLevel(LevelIndex, next, $"NEW ROUTE · {next.place.ToUpperInvariant()}");
        }

        void NextLevel()
        {
            if (LevelIndex < 2)
            {
                int n = LevelIndex + 1;
                BeginLevel(n, variants.First((LevelKind)n, null));
                return;
            }
            // All three levels complete: freeze the car and open the final check.
            popup.Hide();
            playback.Stop();
            SetState(GameState.FinalAssessment);
            veil.SetDim(true);
            assessment.Open(progress.answers, OnAssessmentRoute, () => SetState(GameState.AssessmentResult));
        }

        // ---------------- assessment routing ----------------

        void OnAssessmentRoute(int score)
        {
            SetState(GameState.NextTopicOrReview);
            var route = Assessment.Route(score);
            Dtg.Log($"assessment route: {route} (score {score})");
            switch (route)
            {
                case AssessmentRoute.NextTopic:
                    NextTopicRequested?.Invoke();
                    string scene = !string.IsNullOrEmpty(nextTopicSceneName) ? nextTopicSceneName : catalog.nextTopicSceneName;
                    if (!string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
                    else if (NextTopicRequested.GetPersistentEventCount() == 0)
                    {
                        assessment.Close();
                        popup.Show("TOPIC COMPLETE", "You understand how graph shape controls straight-line motion.\n\nThe next topic will open here once it is assigned.", Palette.Green);
                    }
                    break;
                case AssessmentRoute.FromLevel2:
                {
                    var completed = FindSpec(progress.completedVariant[1]);
                    progress.ResetFromLevel2();
                    ghosts.Clear();
                    BeginLevel(1, variants.First(LevelKind.Waiting, completed), "REVIEW · NEW ROUTE");
                    break;
                }
                default:
                {
                    var completed = FindSpec(progress.completedVariant[0]);
                    progress.ResetAll();
                    ghosts.Clear();
                    BeginLevel(0, variants.First(LevelKind.Rising, completed), "RESTART · NEW ROUTE");
                    break;
                }
            }
        }

        VariantSpec FindSpec(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var v = catalog.variants.Find(x => x.id == id);
            return v ?? (Current != null && Current.id == id ? Current : null);
        }

        // ---------------- keyboard fallback (Editor play-testing) ----------------

        void Keys()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
            {
                if (popup.IsOpen) { if (popup.Primary != null) popup.Primary.onClick.Invoke(); }
                else RunGraph();
            }
            // Keys chosen so they do not clash with the XR Device Simulator (WASD, Q/E, Tab, Space, G, T/Y, mouse).
            if (k.pKey.wasPressedThisFrame) TogglePause();
            if (k.escapeKey.wasPressedThisFrame) EditGraph();
            if (k.backspaceKey.wasPressedThisFrame || k.deleteKey.wasPressedThisFrame) graph.DeleteSelected();
            if (k.homeKey.wasPressedThisFrame) ResetLevel();
            if (!graph.Editable) return;
            int n = graph.Model.Count;
            if (k.rightBracketKey.wasPressedThisFrame && n > 1) graph.Select(graph.Selected <= 0 ? 1 : graph.Selected % (n - 1) + 1);
            if (k.leftBracketKey.wasPressedThisFrame && n > 1) graph.Select(graph.Selected <= 1 ? n - 1 : graph.Selected - 1);
            if (k.insertKey.wasPressedThisFrame)
            {
                var last = graph.Model[graph.Model.Count - 1];
                int i = graph.Model.Add(last.t + 2, last.d);
                if (i > 0) { graph.Select(i); graph.Refresh(); OnGraphChanged(); }
            }
            if (k.upArrowKey.wasPressedThisFrame) graph.Nudge(0, GraphModel.DistanceStep);
            if (k.downArrowKey.wasPressedThisFrame) graph.Nudge(0, -GraphModel.DistanceStep);
            if (k.rightArrowKey.wasPressedThisFrame) graph.Nudge(GraphModel.TimeStep, 0);
            if (k.leftArrowKey.wasPressedThisFrame) graph.Nudge(-GraphModel.TimeStep, 0);
        }
    }
}
