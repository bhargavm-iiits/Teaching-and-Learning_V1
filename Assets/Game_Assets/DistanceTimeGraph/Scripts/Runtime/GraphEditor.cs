using System;
using System.Collections.Generic;
using DistanceTimeGraph.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DistanceTimeGraph
{
    /// The editable Distance from Start–Time graph. Press an empty place to add a snapped point, press a point to select
    /// it, drag to move it (1 s / 5 m snapping, never past its neighbours in time). The first point is the car's start
    /// and stays fixed. Works with the XR controller rays, the XR Device Simulator and the mouse.
    public class GraphEditor : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        [Header("Built by the scene builder")]
        public RectTransform plot;            // the grid; (0 s, 0 m) is its lower-left corner
        public RectTransform ghostLayer, bandLayer, lineLayer, pointLayer, overlayLayer;
        public Sprite circle;
        public TextMeshProUGUI hintText;      // small line under the title: what pressing does now

        public GraphModel Model { get; private set; } = new GraphModel();
        public bool Editable { get; set; } = true;
        public int Selected { get; private set; } = -1;
        public bool Dragging { get; private set; }
        public GraphPoint DragStartPoint { get; private set; }
        public bool ReturnHint { get; set; }  // Level 3: "LOWER POINT = RETURN" while a point is dragged down

        public event Action Changed;          // points added, moved or deleted
        public event Action SelectionChanged;

        const float PickRadius = 30f;         // mm on the panel
        const float PointSize = 22f, SelectedSize = 32f;

        readonly List<Image> segments = new List<Image>();
        readonly List<Image> dots = new List<Image>();
        readonly List<Image> rings = new List<Image>();
        readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        readonly List<List<Image>> ghostLines = new List<List<Image>>();
        Image cursorLine, cursorDot, highlight, band, fadedDot, arrowHead, trailPrefab;
        TextMeshProUGUI bandLabel, returnLabel, highlightLabel;
        RectTransform returnBox;
        readonly List<Image> trail = new List<Image>();
        int highlightSegment = -1;
        float cursorTime = -1f;

        public float Width => plot.rect.width;
        public float Height => plot.rect.height;

        void Awake()
        {
            foreach (var layer in new[] { ghostLayer, bandLayer, lineLayer, pointLayer, overlayLayer })
                for (int i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);
            segments.Clear(); dots.Clear(); rings.Clear(); labels.Clear(); ghostLines.Clear(); trail.Clear();
            cursorLine = null;
            EnsureOverlay();
            Refresh();
        }

        public void SetModel(GraphModel model)
        {
            Model = model;
            Select(-1);
            Refresh();
        }

        public void ResetGraph(int start)
        {
            Model.Reset(start);
            Select(-1);
            SetHighlight(-1, null);
            Refresh();
            Changed?.Invoke();
        }

        public void Select(int index)
        {
            int clamped = index >= 0 && index < Model.Count ? index : -1;
            if (clamped == Selected) return;
            Selected = clamped;
            Refresh();
            SelectionChanged?.Invoke();
        }

        public bool DeleteSelected()
        {
            if (!Editable || Selected <= 0) return false;
            Model.Delete(Selected);
            Selected = Mathf.Min(Selected, Model.Count - 1);
            if (Selected == 0) Selected = -1;
            Refresh();
            Changed?.Invoke();
            SelectionChanged?.Invoke();
            return true;
        }

        // ---------------- coordinates ----------------

        public Vector2 ToPanel(float t, float d) =>
            new Vector2(-Width * 0.5f + Width * t / Model.maxTime, -Height * 0.5f + Height * d / Model.maxDistance);

        public Vector2 ToGraph(Vector2 local) =>
            new Vector2((local.x + Width * 0.5f) / Width * Model.maxTime, (local.y + Height * 0.5f) / Height * Model.maxDistance);

        /// World position of point i on the panel (for the cyan link line to the preview car).
        public Vector3 PointWorld(int i)
        {
            var p = Model[Mathf.Clamp(i, 0, Model.Count - 1)];
            return plot.TransformPoint(ToPanel(p.t, p.d));
        }

        bool TryLocal(PointerEventData e, out Vector2 local)
        {
            local = default;
            var plane = new Plane(-plot.forward, plot.position);
            if (e is TrackedDeviceEventData td && td.rayPoints != null && td.rayPoints.Count >= 2)
            {
                for (int i = 1; i < td.rayPoints.Count; i++)
                {
                    Vector3 a = td.rayPoints[i - 1], b = td.rayPoints[i];
                    var ray = new Ray(a, b - a);
                    if (plane.Raycast(ray, out float dist) && dist <= (b - a).magnitude + 0.001f)
                    {
                        local = plot.InverseTransformPoint(ray.GetPoint(dist));
                        return true;
                    }
                }
                return false;
            }
            var cam = e.pressEventCamera != null ? e.pressEventCamera : e.enterEventCamera;
            if (cam == null) cam = Camera.main;
            if (cam == null) return false;
            var r = cam.ScreenPointToRay(e.position);
            if (!plane.Raycast(r, out float d)) return false;
            local = plot.InverseTransformPoint(r.GetPoint(d));
            return true;
        }

        // ---------------- input ----------------

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

        public void OnPointerDown(PointerEventData e)
        {
            if (!Editable || !TryLocal(e, out var local)) return;
            int near = Nearest(local);
            if (near < 0)
            {
                var g = ToGraph(local);
                if (g.x < 0.5f || g.x > Model.maxTime + 0.49f || g.y < -2.5f || g.y > Model.maxDistance + 2.5f) return;
                int added = Model.Add(g.x, g.y);
                if (added < 0) near = Model.IndexOfTime(GraphModel.SnapTime(g.x));   // that second already has a point: take it
                else
                {
                    near = added;
                    Dtg.Log($"point added {Model[added]}");
                    Changed?.Invoke();
                }
            }
            if (near < 0) return;
            Select(near);
            if (near > 0)
            {
                Dragging = true;
                DragStartPoint = Model[near];
            }
            Refresh();
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Dragging || !Editable || Selected <= 0 || !TryLocal(e, out var local)) return;
            var g = ToGraph(local);
            if (Model.Move(Selected, g.x, g.y))
            {
                Refresh();
                Changed?.Invoke();
            }
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!Dragging) return;
            Dragging = false;
            if (Selected > 0) Dtg.Log($"point {Selected} moved {DragStartPoint} -> {Model[Selected]}");
            Refresh();
            SelectionChanged?.Invoke();
        }

        int Nearest(Vector2 local)
        {
            int best = -1;
            float bestDist = PickRadius;
            for (int i = 0; i < Model.Count; i++)
            {
                float dist = Vector2.Distance(local, ToPanel(Model[i].t, Model[i].d));
                if (dist < bestDist) { bestDist = dist; best = i; }
            }
            return best;
        }

        /// Shows the drag feedback (faded old position, trail, return arrow) as if point `index` had been dragged from
        /// `from`. Used for the validation pictures; normal dragging sets this itself.
        public void SimulateDrag(int index, GraphPoint from)
        {
            Selected = index;
            Dragging = index > 0;
            DragStartPoint = from;
            Refresh();
        }

        /// Moves the selected point by whole steps (keyboard / thumbstick fallback).
        public void Nudge(int dt, int dd)
        {
            if (!Editable || Selected <= 0) return;
            var p = Model[Selected];
            if (Model.Move(Selected, p.t + dt, p.d + dd)) { Refresh(); Changed?.Invoke(); }
        }

        // ---------------- drawing ----------------

        void EnsureOverlay()
        {
            if (cursorLine != null) return;
            band = Img(bandLayer, "BridgeBand", Palette.Green.WithAlpha(0.18f), false);
            bandLabel = UiKit.Text(band.transform, "Label", "BRIDGE OPEN", 16, Palette.Green, Vector2.zero, new Vector2(120, 30), TextAlignmentOptions.Top, true);
            band.gameObject.SetActive(false);

            cursorLine = Img(overlayLayer, "Cursor", Palette.Cyan.WithAlpha(0.85f), false);
            cursorDot = UiKit.Dot(overlayLayer, "CursorDot", Color.white, 30);
            cursorDot.sprite = circle;
            var glow = UiKit.Dot(cursorDot.transform, "Glow", Palette.Cyan.WithAlpha(0.35f), 56);
            glow.sprite = circle;
            glow.transform.SetAsFirstSibling();
            cursorLine.gameObject.SetActive(false);
            cursorDot.gameObject.SetActive(false);

            highlight = Img(overlayLayer, "Highlight", Palette.Orange, false);
            highlightLabel = UiKit.Text(overlayLayer, "HighlightLabel", "", 18, Palette.Orange, Vector2.zero, new Vector2(260, 50), TextAlignmentOptions.Center, true);
            highlight.gameObject.SetActive(false);
            highlightLabel.gameObject.SetActive(false);

            fadedDot = UiKit.Dot(overlayLayer, "PreviousPosition", new Color(0.75f, 0.8f, 0.9f, 0.45f), PointSize);
            fadedDot.sprite = circle;
            fadedDot.gameObject.SetActive(false);
            arrowHead = Img(overlayLayer, "ReturnArrow", Palette.Cyan, false);
            arrowHead.gameObject.SetActive(false);
            var boxImg = UiKit.Panel(overlayLayer, "ReturnCallout", Palette.PanelDeep, Vector2.zero, new Vector2(150, 58));
            boxImg.gameObject.AddComponent<Outline>().effectColor = Palette.Cyan;
            returnBox = boxImg.rectTransform;
            returnLabel = UiKit.Text(returnBox, "Text", "LOWER POINT\n= RETURN", 17, Color.white, Vector2.zero, new Vector2(146, 54), TextAlignmentOptions.Center, true);
            returnBox.gameObject.SetActive(false);
        }

        Image Img(Transform parent, string name, Color c, bool round)
        {
            var img = UiKit.Panel(parent, name, c, Vector2.zero, new Vector2(4, 4), round);
            img.raycastTarget = false;
            return img;
        }

        public void Refresh()
        {
            if (plot == null) return;
            EnsureOverlay();
            int n = Model.Count;

            // Segments, coloured by what they make the car do: rising cyan, flat yellow, falling orange.
            while (segments.Count < n - 1) segments.Add(Img(lineLayer, "Segment", Color.white, false));
            for (int i = 0; i < segments.Count; i++)
            {
                bool on = i < n - 1;
                segments[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = ToPanel(Model[i].t, Model[i].d);
                var b = ToPanel(Model[i + 1].t, Model[i + 1].d);
                UiKit.Line(segments[i].rectTransform, a, b, 7f);
                float slope = Model.Slope(i);
                segments[i].color = slope > 0 ? Palette.Cyan : slope < 0 ? Palette.Orange : Palette.Yellow;
            }

            while (dots.Count < n)
            {
                var ring = UiKit.Dot(pointLayer, "Ring", Palette.Cyan, SelectedSize + 14); ring.sprite = circle;
                var dot = UiKit.Dot(pointLayer, "Point", Palette.Yellow, PointSize); dot.sprite = circle;
                var label = UiKit.Text(pointLayer, "PointLabel", "", 17, Color.white, Vector2.zero, new Vector2(140, 28), TextAlignmentOptions.Center, true);
                rings.Add(ring); dots.Add(dot); labels.Add(label);
            }
            for (int i = 0; i < dots.Count; i++)
            {
                bool on = i < n;
                dots[i].gameObject.SetActive(on);
                rings[i].gameObject.SetActive(on && i == Selected);
                labels[i].gameObject.SetActive(on);
                if (!on) continue;
                var p = ToPanel(Model[i].t, Model[i].d);
                bool sel = i == Selected;
                dots[i].rectTransform.anchoredPosition = p;
                dots[i].rectTransform.sizeDelta = Vector2.one * (sel ? SelectedSize : PointSize);
                dots[i].color = i == 0 ? new Color(1f, 0.93f, 0.6f) : Palette.Yellow;
                rings[i].rectTransform.anchoredPosition = p;
                labels[i].text = $"({Model[i].t} s, {Model[i].d} m)";
                labels[i].fontSize = sel ? 20 : 16;
                labels[i].color = sel ? Palette.Yellow : Color.white;
                // Labels above the point, below it near the top edge, and nudged inward at the sides.
                float x = Mathf.Clamp(p.x, -Width * 0.5f + 55, Width * 0.5f - 55);
                float y = p.y > Height * 0.5f - 40 ? p.y - 30 : p.y + 30;
                labels[i].rectTransform.anchoredPosition = new Vector2(x, y);
            }
            for (int i = 0; i < n; i++) rings[i].transform.SetAsFirstSibling();

            DrawDragFeedback();
            DrawHighlight();
            DrawCursor();
            if (hintText != null)
                hintText.text = !Editable ? "Graph locked while the car runs"
                    : Selected > 0 ? "Drag the point · DELETE removes it"
                    : "Press the grid to add a point";
        }

        void DrawDragFeedback()
        {
            bool show = Dragging && Selected > 0;
            var now = show ? Model[Selected] : default;
            bool moved = show && (now.t != DragStartPoint.t || now.d != DragStartPoint.d);
            fadedDot.gameObject.SetActive(moved);
            foreach (var t in trail) t.gameObject.SetActive(false);
            bool lower = moved && ReturnHint && now.d < DragStartPoint.d;
            arrowHead.gameObject.SetActive(lower);
            returnBox.gameObject.SetActive(lower);
            if (!moved) return;

            var from = ToPanel(DragStartPoint.t, DragStartPoint.d);
            var to = ToPanel(now.t, now.d);
            fadedDot.rectTransform.anchoredPosition = from;
            // A dashed trail from the faded old position to the point.
            float len = Vector2.Distance(from, to);
            int dashes = Mathf.Clamp((int)(len / 14f), 0, 40);
            while (trail.Count < dashes) trail.Add(Img(overlayLayer, "Dash", Color.white.WithAlpha(0.8f), false));
            for (int i = 0; i < dashes; i++)
            {
                var a = Vector2.Lerp(from, to, (i + 0.15f) / dashes);
                var b = Vector2.Lerp(from, to, (i + 0.6f) / dashes);
                trail[i].gameObject.SetActive(true);
                UiKit.Line(trail[i].rectTransform, a, b, 3f);
            }
            if (lower)
            {
                // Downward arrow beside the point with "LOWER POINT = RETURN".
                float side = to.x > Width * 0.5f - 190 ? -1f : 1f;
                var top = new Vector2(to.x + side * 38, from.y - 8);
                var bottom = new Vector2(to.x + side * 38, to.y + 16);
                UiKit.Line(arrowHead.rectTransform, top, bottom, 6f);
                returnBox.anchoredPosition = new Vector2(to.x + side * 125, Mathf.Clamp((from.y + to.y) * 0.5f, -Height * 0.5f + 30, Height * 0.5f - 30));
            }
        }

        // ---------------- run feedback ----------------

        /// Moves the glowing cursor to time t along the graph (negative hides it).
        public void SetCursor(float t, float distance)
        {
            cursorTime = t;
            curDistance = distance;
            DrawCursor();
        }

        float curDistance;

        void DrawCursor()
        {
            if (cursorLine == null) return;
            bool on = cursorTime >= 0f;
            cursorLine.gameObject.SetActive(on);
            cursorDot.gameObject.SetActive(on);
            if (!on) return;
            float t = Mathf.Min(cursorTime, Model.maxTime);
            var top = ToPanel(t, Model.maxDistance);
            var bottom = ToPanel(t, 0);
            UiKit.Line(cursorLine.rectTransform, bottom, top, 3f);
            cursorDot.rectTransform.anchoredPosition = ToPanel(t, curDistance);
            float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 8f);
            cursorDot.rectTransform.localScale = Vector3.one * pulse;
        }

        /// Highlights the graph section that explains a result (orange), with a short caption. -1 clears it.
        public void SetHighlight(int segment, string caption)
        {
            highlightSegment = segment;
            highlightLabel.text = caption ?? "";
            DrawHighlight();
        }

        void DrawHighlight()
        {
            if (highlight == null) return;
            bool on = highlightSegment >= 0 && highlightSegment < Model.SegmentCount;
            highlight.gameObject.SetActive(on);
            highlightLabel.gameObject.SetActive(on && !string.IsNullOrEmpty(highlightLabel.text));
            if (!on) return;
            var a = ToPanel(Model[highlightSegment].t, Model[highlightSegment].d);
            var b = ToPanel(Model[highlightSegment + 1].t, Model[highlightSegment + 1].d);
            UiKit.Line(highlight.rectTransform, a, b, 16f);
            highlight.color = Palette.Orange.WithAlpha(0.55f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
            var mid = (a + b) * 0.5f;
            highlightLabel.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(mid.x, -Width * 0.5f + 130, Width * 0.5f - 130),
                Mathf.Clamp(mid.y + (mid.y > 0 ? -44 : 44), -Height * 0.5f + 20, Height * 0.5f - 20));
        }

        /// The timed-bridge window as a faint band behind the grid (Level 2 only). It is the bridge's opening time,
        /// not a target line.
        public void SetBridgeBand(bool on, float open, float close)
        {
            EnsureOverlay();
            band.gameObject.SetActive(on);
            if (!on) return;
            var a = ToPanel(open, 0);
            var b = ToPanel(close, Model.maxDistance);
            band.rectTransform.anchoredPosition = (a + b) * 0.5f;
            band.rectTransform.sizeDelta = new Vector2(b.x - a.x, b.y - a.y);
            band.rectTransform.localRotation = Quaternion.identity;
            bandLabel.rectTransform.anchoredPosition = new Vector2(0, (b.y - a.y) * 0.5f - 16);
            bandLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(120, b.x - a.x), 30);
        }

        /// Earlier attempts drawn faintly behind the current graph: blue for a success, faded orange for a failure.
        public void SetGhosts(IList<(GraphPoint[] points, Color color)> ghosts)
        {
            while (ghostLines.Count < ghosts.Count) ghostLines.Add(new List<Image>());
            for (int g = 0; g < ghostLines.Count; g++)
            {
                var line = ghostLines[g];
                var pts = g < ghosts.Count ? ghosts[g].points : null;
                int need = pts == null ? 0 : pts.Length - 1;
                while (line.Count < need) line.Add(Img(ghostLayer, "Ghost", Color.white, false));
                for (int i = 0; i < line.Count; i++)
                {
                    bool on = i < need;
                    line[i].gameObject.SetActive(on);
                    if (!on) continue;
                    UiKit.Line(line[i].rectTransform, ToPanel(pts[i].t, pts[i].d), ToPanel(pts[i + 1].t, pts[i + 1].d), 5f);
                    line[i].color = ghosts[g].color;
                }
            }
        }

        void Update()
        {
            if (highlightSegment >= 0) DrawHighlight();
            if (cursorTime >= 0f) DrawCursor();
        }
    }
}
