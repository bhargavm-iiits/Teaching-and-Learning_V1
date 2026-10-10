using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedTimeGraphFree
{
    public enum Part { Intro, Demo, DemoFree, Q1, Q2, Q3, FinalCheck, Result, TopicComplete }
    public enum Mode { Edit, Run, Review, Complete }

    /// <summary>
    /// INTRO -> DEMO (1 shared graph) -> FREE PLAY -> Q1 -> Q2 -> Q3 (3 graphs each) -> FINAL CHECK -> ROUTING.
    /// Every part is EDIT -> RUN -> REVIEW -> edit again; RUN is the check. Switching parts only loads data:
    /// the three vehicles in the TripleVehicleArena are never destroyed or re-created.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        public TextAsset data;
        public string nextTopicSceneName = "";
        public GraphController graph;
        public TripleVehicleArena arena;
        public PlaybackController playback;
        public PopupManager popup;
        public FinalCheckManager finalCheck;
        public ProveItReplay proof;
        public FeedbackPresenter feedback;
        public ChecklistPanel checklist;
        public GraphWorldLinks links;
        public AudioCues sounds;

        [NonSerialized] public QuestionCatalog catalog;
        [NonSerialized] public ProgressState progress = new ProgressState();
        [NonSerialized] public VariantData variant;
        [NonSerialized] public RunResult lastRun;
        public Part part = Part.Intro;
        public Mode mode = Mode.Edit;
        public int hintLevel, hintLane = -1, prediction = -1;
        [NonSerialized] public string message = "";

        readonly Queue<int> remedial = new Queue<int>();
        bool finalWordingB; int[] finalItems = { 0, 1, 2 };

        public bool IsGraphPart => part == Part.Demo || part == Part.DemoFree || part == Part.Q1 || part == Part.Q2 || part == Part.Q3 || part == Part.Intro;
        public int Question => part == Part.Q1 ? 1 : part == Part.Q2 ? 2 : part == Part.Q3 ? 3 : 0;
        public CourseConfig Course => catalog.course;

        void Awake() { Initialize(); }
        void Start() { if (Application.isPlaying) ShowIntro(); }

        public void Initialize()
        {
            if (catalog == null && data) catalog = QuestionCatalog.Load(data.text, s => JsonUtility.FromJson<QuestionCatalog>(s));
            if (variant == null && catalog != null) { variant = catalog.demo; graph.LoadVariant(variant, Course); arena.ApplyVariant(variant, Course); }
        }

        void Change(Part p, Mode m, string why = "")
        {
            part = p; mode = m;
            string line = $"[STGF] {p}/{m} {variant?.id} {why}".TrimEnd();
            Debug.Log(line); progress.Log(line);
            Refresh();
        }

        // ------------------------------------------------------------------ parts
        public void ShowIntro()
        {
            Initialize(); remedial.Clear(); finalWordingB = false; finalItems = new[] { 0, 1, 2 };
            Load(catalog.demo); Change(Part.Intro, Mode.Edit);
            popup.Show("CAR, TRAIN & MOTORCYCLE CHALLENGE",
                "You never drive. You draw speed–time graphs.\nPress RUN and the graph controls every ride:\nheight = speed, slope = acceleration, area = distance.",
                "START", () => StartPart(Part.Demo));
        }

        void Load(VariantData v)
        {
            variant = v; lastRun = null; hintLevel = 0; hintLane = -1; prediction = -1; progress.Runs = 0;
            graph.LoadVariant(v, Course); arena.ApplyVariant(v, Course); playback.ResetToStart();
        }

        public void StartPart(Part p, bool newVariant = true)
        {
            Initialize(); popup.Hide(); finalCheck.Hide();
            if (p == Part.Demo || p == Part.DemoFree) { if (p == Part.Demo || variant != catalog.demo) Load(catalog.demo); }
            else
            {
                int q = p == Part.Q1 ? 1 : p == Part.Q2 ? 2 : 3;
                int index = newVariant || variant == null || variant.question != q ? AdaptiveSelection.Next(q, progress, catalog.VariantCount(q)) : variant.id[1] - 'A';
                Load(catalog.Variant(q, index));
            }
            Change(p, Mode.Edit, "start");
            if (p == Part.DemoFree) { message = "FREE PLAY: draw any graph — watch all three respond."; Refresh(); return; }
            popup.Show(variant.title, variant.idea, "START", () => { popup.Hide(); Refresh(); });
        }

        // ------------------------------------------------------------------ buttons
        public void Run()
        {
            if (!IsGraphPart || popup.Blocking || proof.running) return;
            if (mode == Mode.Run) { playback.TogglePause(); return; }
            int required = graph.SharedMode ? 1 : 3;
            for (int i = 0; i < required; i++)
                if (graph.Graph(i).Points.Count < 2) { graph.Toast(graph.SharedMode ? "Add at least 2 points before RUN." : $"Add at least 2 points to the {Course.vehicles[i]} graph."); return; }
            if (part == Part.Q2 && prediction < 0)
            {
                popup.Show("PREDICT FIRST", "Which vehicle will stop farthest?\n(Not scored — compare after the run.)", "CAR", () => Predict(0), "TRAIN", () => Predict(1), "BIKE", () => Predict(2));
                return;
            }
            StartRun();
        }
        void Predict(int lane) { prediction = lane; popup.Hide(); progress.Log($"prediction {Course.vehicles[lane]}"); StartRun(); }

        void StartRun()
        {
            progress.Runs++; progress.TotalRuns++;
            Change(part, Mode.Run, $"run {progress.Runs}");
            message = "RUN: all three vehicles follow their graphs.";
            playback.Begin();
        }

        /// <summary>Called by the PlaybackController 0.5 s after the latest graph ends.</summary>
        public void FinishRun()
        {
            var graphs = graph.AllGraphs;
            lastRun = QuestionRules.EvaluateAll(variant, graphs, Course);
            bool demo = part == Part.Demo || part == Part.DemoFree;
            bool pass = lastRun.pass;
            Change(part, pass ? Mode.Complete : Mode.Review, pass ? "pass" : "fail: " + lastRun.Message);
            foreach (var v in lastRun.vehicles) progress.Log($"{v.vehicle}: {(v.pass ? "pass" : v.firstFailure)} (stop {v.distance:0.#} m)");
            if (demo)
            {
                double d = lastRun.vehicles[0].distance;
                if (part == Part.DemoFree)
                {
                    message = $"CAR {lastRun.vehicles[0].distance:0.#} m · TRAIN {lastRun.vehicles[1].distance:0.#} m · BIKE {lastRun.vehicles[2].distance:0.#} m — each stop = the area under its own graph.";
                    Refresh(); return;
                }
                message = Math.Abs(d - variant.lanes[0].stop) <= Course.stopTolerance
                    ? $"Same graph, same motion: all three stopped at {d:0.#} m."
                    : $"Same graph, same motion: all three stopped at {d:0.#} m. Match the rings to reach {variant.lanes[0].stop:0} m.";
                if (part == Part.Demo)
                {
                    progress.Completed[0] = true; sounds.Pass();
                    popup.Show("DEMO COMPLETE", message + "\nThe vehicles look different, but one graph gives one motion.", "NEXT", Next, "WATCH REPLAY", Replay);
                }
                Refresh(); return;
            }
            if (pass)
            {
                progress.Completed[Question] = true; sounds.Pass();
                message = variant.keySentence;
                string extra = PredictionText();
                popup.Show($"QUESTION {Question} COMPLETE", variant.keySentence + extra, "NEXT", Next, "WATCH REPLAY", Replay);
            }
            else
            {
                sounds.Fail();
                message = lastRun.Message + PredictionText();
                // Hint ladder: run 2 -> segment areas + red marker; run 3 -> dashed outline of one lane.
                hintLevel = Math.Max(hintLevel, progress.Runs >= 3 ? 2 : progress.Runs >= 2 ? 1 : 0);
                hintLane = lastRun.FirstFailure?.lane ?? 0;
                if (progress.Runs >= Course.maxRunsPerVariant)
                {
                    progress.Log($"attempt recorded on {variant.id}");
                    popup.Show("ATTEMPT RECORDED", lastRun.Message + "\nA new story with new numbers is ready.", "NEW STORY", () => StartPart(part, true));
                }
            }
            Refresh();
        }

        string PredictionText()
        {
            if (part != Part.Q2 || prediction < 0 || lastRun == null) return "";
            int far = Enumerable.Range(0, 3).OrderByDescending(i => lastRun.vehicles[i].distance).First();
            return $"\nYou predicted {Course.vehicles[prediction]}; {Course.vehicles[far]} went farthest ({lastRun.vehicles[far].distance:0.#} m).";
        }

        public void Next()
        {
            popup.Hide();
            if (part == Part.Intro) { StartPart(Part.Demo); return; }
            if (part == Part.Demo) { StartPart(Part.DemoFree); return; }
            if (remedial.Count > 0 && (part == Part.Q1 || part == Part.Q2 || part == Part.Q3 || part == Part.Result)) { StartPart(QuestionPart(remedial.Dequeue())); return; }
            switch (part)
            {
                case Part.DemoFree: StartPart(Part.Q1); break;
                case Part.Q1: StartPart(Part.Q2); break;
                case Part.Q2: StartPart(Part.Q3); break;
                default: StartFinalCheck(finalWordingB, finalItems); break;
            }
        }
        static Part QuestionPart(int q) => q == 1 ? Part.Q1 : q == 2 ? Part.Q2 : Part.Q3;

        public void Replay() { popup.Hide(); if (!IsGraphPart) return; Change(part, Mode.Run, "replay"); playback.Begin(); }
        public void EditAgain() { popup.Hide(); playback.ResetToStart(); Change(part, Mode.Edit, "edit"); }
        public void ResetRun() { if (!IsGraphPart || popup.Blocking) return; playback.ResetToStart(); Change(part, Mode.Edit, "reset"); }
        public void Hint()
        {
            if (Question == 0) { graph.Toast("Match the hollow rings: " + string.Join(" ", variant.lanes[0].solution.Select(p => $"({p.t:0.#},{p.v:0.#})")) + "."); return; }
            hintLevel = Math.Min(2, hintLevel + 1);
            if (hintLane < 0) hintLane = graph.SelectedLane;
            message = hintLevel == 1 ? "HINT: segment areas are shown. The red ring marks the first mismatch." : $"HINT: the dashed outline shows the {Course.vehicles[hintLane]} shape.";
            Refresh();
        }
        public void GraphEdited()
        {
            if (mode != Mode.Edit) { playback.ResetToStart(); mode = Mode.Edit; }
            Refresh();
        }

        public void TeacherJump(int target)
        {
            popup.Hide(); finalCheck.Hide(); remedial.Clear();
            Debug.Log("[STGF] teacher jump " + target);
            if (target == 0) StartPart(Part.Demo); else StartPart(QuestionPart(target));
        }

        // ------------------------------------------------------------------ final check + routing
        public void StartFinalCheck(bool wordingB, int[] items)
        {
            popup.Hide(); playback.ResetToStart();
            arena.ShowFinalCheck();
            Change(Part.FinalCheck, Mode.Edit, wordingB ? "wording B" : "wording A");
            finalCheck.Begin(wordingB, items);
            Refresh();
        }

        public void RouteFinal(bool[] correct, bool[] asked)
        {
            // Items not asked this round keep their earlier (correct) result.
            int score = FinalCheckRouting.Score(correct);
            var route = FinalCheckRouting.Route(correct);
            progress.Log($"final check {score}/3 route {route}");
            Change(Part.Result, Mode.Review, $"{score}/3 {route}");
            if (route == FinalRoute.TopicComplete)
            {
                Change(Part.TopicComplete, Mode.Complete);
                sounds.Pass(); SaveLog();
                popup.Show("TOPIC COMPLETE · 3 / 3", "Height = speed.  Slope = acceleration.\nArea = distance.  Crossing lines = equal speed, not the same place.",
                    string.IsNullOrEmpty(nextTopicSceneName) ? "PLAY AGAIN" : "NEXT TOPIC",
                    () => { if (string.IsNullOrEmpty(nextTopicSceneName)) { progress = new ProgressState(); ShowIntro(); } else SceneManager.LoadScene(nextTopicSceneName); });
                return;
            }
            if (route == FinalRoute.RestartDemo)
            {
                finalWordingB = true; finalItems = new[] { 0, 1, 2 }; remedial.Clear();
                popup.Show("0 / 3 — LET'S REPLAY THE DEMO", "Watch again how one graph controls every ride. New numbers are waiting in every question.", "START DEMO", () => StartPart(Part.Demo));
                return;
            }
            remedial.Clear(); foreach (int q in FinalCheckRouting.QuestionsToReplay(correct)) remedial.Enqueue(q);
            finalWordingB = true; finalItems = FinalCheckRouting.ItemsToReask(correct);
            string names = string.Join(", ", remedial.Select(q => $"Question {q} ({(q == 1 ? "graph shape" : q == 2 ? "slope" : "area")})"));
            popup.Show($"FINAL CHECK · {score} / 3", $"Let's revisit {names} with new numbers, then try only the missed items again.", "CONTINUE", () => StartPart(QuestionPart(remedial.Dequeue())));
        }

        // ------------------------------------------------------------------ view
        public void Refresh()
        {
            if (catalog == null || variant == null) return;
            graph.Refresh();
            arena.Refresh(this);
            if (checklist) checklist.Refresh(this);
            if (feedback) feedback.Refresh(this);
        }

        void SaveLog()
        {
            try
            {
                System.IO.Directory.CreateDirectory("Logs/SpeedTimeGraphFree");
                System.IO.File.WriteAllLines("Logs/SpeedTimeGraphFree/progress.txt", progress.Events.Concat(progress.Misconceptions.Select(m => "misconception " + m)));
            }
            catch (Exception) { }
        }
        void OnApplicationQuit() { SaveLog(); }
    }
}