using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// The floating "Velocity Time Graph" screen: three big graphs side by side, one per vehicle (CAR, TRAIN, BIKE),
    /// each edited directly on its own panel. Guided Demo: the three panels mirror ONE shared graph
    /// ("same graph, different vehicles"). Free play and Q1-Q3: three independent graphs.
    /// UNDO / DELETE / CLEAR act on the graph touched last.
    /// </summary>
    public class GraphController : MonoBehaviour
    {
        public const int AllTab = 3;
        public GameFlowManager game;
        public GraphPanel[] panels = new GraphPanel[3];
        public GraphInput[] inputs = new GraphInput[3];
        public TMP_Text[] names = new TMP_Text[3], stories = new TMP_Text[3], readouts = new TMP_Text[3], statuses = new TMP_Text[3];
        public Image[] frames = new Image[3];
        public Transform[] badges = new Transform[3];
        public TMP_Text partTitle, caption, toast;
        public Color[] vehicleColors = { new Color(.239f, .608f, 1f), new Color(1f, .302f, .353f), new Color(1f, .624f, .102f) };

        [NonSerialized] public GraphModel shared = new GraphModel();
        [NonSerialized] public GraphModel[] lanes = { new GraphModel(), new GraphModel(), new GraphModel() };
        public int selectedLane;
        public int tab => selectedLane;
        [NonSerialized] public int hoverLane = -1;
        [NonSerialized] public float hoverTime = -1;
        [NonSerialized] public Vector2 hoverLocal;
        [NonSerialized] public int hoverSegment = -1;
        bool dragTag; int dragLane = -1; float toastUntil;
        static readonly CultureInfo ci = CultureInfo.InvariantCulture;

        public bool SharedMode => game.part == Part.Intro || game.part == Part.Demo;
        public GraphModel Graph(int lane) => SharedMode ? shared : lanes[Mathf.Clamp(lane, 0, 2)];
        public GraphModel Selected => Graph(selectedLane);
        public int SelectedLane => selectedLane;
        public bool Editable => game.mode == Mode.Edit && game.IsGraphPart && game.part != Part.Intro && !game.popup.Blocking && !game.proof.running;
        public bool CanScrub => (game.mode == Mode.Review || game.mode == Mode.Complete) && game.IsGraphPart;
        public IList<GraphPoint>[] AllGraphs => new IList<GraphPoint>[] { Graph(0).Points, Graph(1).Points, Graph(2).Points };
        /// <summary>Hover time for a lane's ghost: the hovered panel only (all three in the shared Demo).</summary>
        public float HoverTime(int lane) => hoverTime >= 0 && (SharedMode || lane == hoverLane) ? hoverTime : -1;

        public void LoadVariant(VariantData v, CourseConfig course)
        {
            shared = NewModel(v, course);
            for (int i = 0; i < 3; i++) lanes[i] = NewModel(v, course);
            foreach (var p in panels) if (p) { p.maxTime = (float)course.maxTime; p.maxSpeed = (float)course.maxSpeed; p.gridV = course.maxSpeed > 20 ? 5 : 1; p.labelV = course.maxSpeed > 20 ? 10 : 2; }
            selectedLane = 0; hoverLane = -1; hoverTime = -1; dragTag = false;
        }
        static GraphModel NewModel(VariantData v, CourseConfig c) => new GraphModel(v.snap, v.snapV) { MaxTime = c.maxTime, MaxSpeed = c.maxSpeed, MaxPoints = c.maxPoints };

        public void SelectTab(int lane) { selectedLane = Mathf.Clamp(lane, 0, 2); Debug.Log("[STGF] graph " + game.catalog.course.vehicles[selectedLane]); game.Refresh(); }

        public int HitPoint(int lane, Vector2 local)
        {
            var p = Graph(lane).Points; var panel = panels[lane]; int hit = -1; float best = 34;
            for (int i = 0; i < p.Count; i++) { float d = Vector2.Distance(local, panel.ToLocal(p[i].t, p[i].v)); if (d < best) { best = d; hit = i; } }
            return hit;
        }

        public void Hover(int lane, Vector2 local)
        {
            var panel = panels[lane];
            hoverLane = lane; hoverLocal = local;
            bool inside = panel.Inside(local, 10);
            hoverTime = inside ? Mathf.Clamp(panel.ToValue(local).x, 0, panel.maxTime) : -1;
            hoverSegment = -1;
            var p = Graph(lane).Points;
            if (inside) for (int i = 0; i + 1 < p.Count; i++) if (panel.SegmentDistance(p, i, local) < 18) { hoverSegment = i; break; }
            game.Refresh();
        }
        public void HoverEnd() { hoverLane = -1; hoverTime = -1; hoverSegment = -1; game.Refresh(); }
        public void Edited(int lane, Vector2 local)
        {
            selectedLane = lane; dragTag = true; dragLane = lane; hoverLane = lane; hoverLocal = local;
            hoverTime = Mathf.Clamp(panels[lane].ToValue(local).x, 0, panels[lane].maxTime);
            game.GraphEdited();
        }
        public void DragEnded() { dragTag = false; dragLane = -1; game.Refresh(); }
        public void Toast(string s) { if (toast) { toast.text = s; toast.gameObject.SetActive(true); toastUntil = Time.unscaledTime + 3; } game.Refresh(); }

        public void DeleteSelected() { if (Editable && Selected.DeleteSelected()) { game.sounds.Tick(); game.GraphEdited(); } else if (Editable) Toast("Select a point first (P0 cannot be deleted)."); }
        public void Undo() { if (Editable && Selected.Undo()) game.GraphEdited(); }
        public void ClearGraph() { if (Editable) { Selected.Clear(); game.GraphEdited(); } }

        void Update() { if (toast && toast.gameObject.activeSelf && Time.unscaledTime > toastUntil) toast.gameObject.SetActive(false); }

        // ------------------------------------------------------------------ view
        public void Refresh()
        {
            if (game == null || game.variant == null) return;
            var course = game.catalog.course;
            bool final = game.part == Part.FinalCheck, demo = SharedMode;
            if (toast) toast.gameObject.SetActive(Time.unscaledTime < toastUntil);
            partTitle.text = "Velocity Time Graph  ·  " + (final ? "FINAL CHECK · " + game.finalCheck.Current?.concept
                           : game.part == Part.DemoFree ? "FREE PLAY · three graphs, three rides"
                           : game.variant.title + (demo ? "" : "  ·  " + game.variant.id));
            float cursor = game.mode == Mode.Edit ? -1 : game.playback.time;
            float t = game.mode != Mode.Edit ? game.playback.time : hoverTime >= 0 ? hoverTime : 0;

            for (int i = 0; i < 3; i++)
            {
                var panel = panels[i]; panel.Clear();
                var col = vehicleColors[i];
                if (names[i]) { names[i].text = course.vehicles[i]; names[i].color = col; }
                if (frames[i]) frames[i].color = (i == selectedLane && !demo && !final) ? new Color(col.r, col.g, col.b, 1f) : new Color(col.r, col.g, col.b, .4f);
                if (final)
                {
                    if (i == 1) game.finalCheck.Draw(panel);
                    panel.Commit();
                    if (stories[i]) stories[i].text = i == 1 ? game.finalCheck.Current?.prompt ?? "" : "";
                    if (readouts[i]) readouts[i].text = ""; if (statuses[i] && game.Question == 0) statuses[i].text = "";
                    continue;
                }
                var model = Graph(i); var pts = model.Points;
                if (stories[i]) stories[i].text = demo ? game.variant.lanes[0].story : game.part == Part.DemoFree ? "Draw any graph for the " + course.vehicles[i].ToLowerInvariant() + "." : game.variant.lanes[i].story;
                // Area under the graph (Q3 fills left to right during RUN).
                panel.area = pts; panel.areaUntil = game.part == Part.Q3 && game.mode == Mode.Run ? cursor : float.PositiveInfinity;
                panel.areaColor = demo ? new Color(.16f, .86f, 1, .13f) : new Color(col.r, col.g, col.b, .17f);
                if (game.hintLevel >= 2 && !demo && game.hintLane == i && game.Question > 0)
                    panel.lines.Add(new GraphPanel.Line { points = game.variant.lanes[i].solution, color = new Color(1, 1, 1, .75f), width = 4, dashed = true });
                bool sel = demo || i == selectedLane;
                panel.lines.Add(new GraphPanel.Line { points = pts, color = demo ? Color.white : col, width = 7, slopeColors = demo, drawPoints = true, selected = true, selectedIndex = sel ? model.Selected : -1 });
                panel.highlightSegment = hoverLane == i ? hoverSegment : -1;
                if (game.part == Part.Demo || game.part == Part.Intro)
                    foreach (var q in game.variant.lanes[0].solution) panel.rings.Add((q.t, q.v, 20, new Color(.16f, .86f, 1, .8f), false));
                bool showArea = demo || game.part == Part.DemoFree || game.part == Part.Q3 || game.hintLevel >= 1;
                for (int k = 1; k < pts.Count; k++)
                {
                    var a = pts[k - 1]; var b = pts[k]; double mt = (a.t + b.t) / 2, mv = (a.v + b.v) / 2;
                    if (!demo) panel.labels.Add(new GraphPanel.Label { text = string.Format(ci, "a={0:+0.##;−0.##;0}", MotionMath.SegmentSlope(a, b)), t = mt, v = mv, offset = new Vector2(0, 30), size = 30 });
                    if (showArea && MotionMath.SegmentArea(a, b) > 0) panel.labels.Add(new GraphPanel.Label { text = string.Format(ci, "{0:0.#} m", MotionMath.SegmentArea(a, b)), t = mt, v = Math.Max(panel.maxSpeed * .06, mv * .45), color = new Color(.75f, .95f, 1), size = 30 });
                }
                panel.labels.Add(new GraphPanel.Label { text = string.Format(ci, "Area = {0:0.#} m", MotionMath.Distance(pts)), t = panel.maxTime * .8, v = panel.maxSpeed * .9, color = GraphPanel.PointCyan, size = 34, bold = true });
                if (game.hintLevel >= 1 && !demo && game.lastRun != null && game.Question > 0)
                {
                    var r = QuestionRules.Evaluate(game.variant.question, i, game.variant.lanes[i], pts, course);
                    if (r.markerTime >= 0) panel.rings.Add((r.markerTime, MotionMath.Speed(pts, r.markerTime), 24, new Color(1, .2f, .2f, .95f), false));
                }
                if (dragTag && (dragLane == i || demo) && model.Selected >= 0 && model.Selected < pts.Count)
                {
                    var p = pts[model.Selected];
                    panel.labels.Add(new GraphPanel.Label { text = string.Format(ci, "({0:0.#} s, {1:0.#} m/s)", p.t, p.v), t = p.t, v = p.v, offset = new Vector2(0, 46), size = 34, bold = true });
                }
                else if (hoverLane == i && Editable)
                {
                    var v = panel.ToValue(hoverLocal);
                    if (hoverSegment >= 0 && hoverSegment + 1 < pts.Count)
                        panel.labels.Add(new GraphPanel.Label { text = string.Format(ci, "a = {0:+0.0;−0.0;0.0} m/s²", MotionMath.SegmentSlope(pts[hoverSegment], pts[hoverSegment + 1])), t = v.x, v = v.y, offset = new Vector2(0, 44), size = 34, bold = true });
                    else panel.rings.Add((model.SnapValue(Mathf.Clamp(v.x, 0, panel.maxTime)), model.SnapSpeed(Mathf.Clamp(v.y, 0, panel.maxSpeed)), 14, new Color(.16f, .86f, 1, .45f), false));
                }
                panel.cursor = cursor;
                panel.Commit();
                if (readouts[i]) readouts[i].text = string.Format(ci, "t {0:0.0} s  ·  v {1:0.0} m/s  ·  a {2:+0.0;−0.0;0.0} m/s²  ·  s {3:0.0} m", t, MotionMath.Speed(pts, t), MotionMath.Acceleration(pts, t), MotionMath.Distance(pts, t));
            }
        }
    }
}