using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Vector drawing of a speed–time graph on a world-space canvas (MaskableGraphic, so the XR ray, the
    /// simulator and the mouse all reach it through normal UI events). The rect is the plot area:
    /// x = 0..maxTime s, y = 0..maxSpeed m/s. Content is described by the caller each refresh.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class GraphPanel : MaskableGraphic
    {
        public static readonly Color Rising = Hex("#FF8A1E"), Flat = Hex("#FFD43C"), Falling = Hex("#3CDC6E"), Zero = Hex("#8FA3B8"), PointCyan = Hex("#28DCFF");
        public static Color Hex(string h) { h = h.TrimStart('#'); int v = System.Convert.ToInt32(h.Substring(0, 6), 16); return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f); }

        public float maxTime = 16, maxSpeed = 12;
        public float gridT = 1, gridV = 1, labelT = 2, labelV = 2;   // minor grid steps and number spacing
        public bool grid = true, numbers = true, mini;
        public TMP_FontAsset font;
        public float numberSize = 30;

        public class Line
        {
            public IList<GraphPoint> points; public Color color = Color.white; public float width = 6;
            public bool slopeColors, dashed, drawPoints, selected; public int selectedIndex = -1;
        }
        public class Label { public string text; public double t, v; public Vector2 offset; public Color color = Color.white; public float size = 30; public bool bold; }

        [NonSerialized] public readonly List<Line> lines = new List<Line>();
        [NonSerialized] public readonly List<(double t, double v, float radius, Color color, bool filled)> rings = new List<(double, double, float, Color, bool)>();
        [NonSerialized] public readonly List<Label> labels = new List<Label>();
        [NonSerialized] public IList<GraphPoint> area; [NonSerialized] public float areaUntil = float.PositiveInfinity; [NonSerialized] public Color areaColor = new Color(.16f, .86f, 1f, .16f);
        [NonSerialized] public float cursor = -1;
        [NonSerialized] public int highlightSegment = -1;

        readonly List<TextMeshProUGUI> pool = new List<TextMeshProUGUI>();

        public Rect Plot => rectTransform.rect;
        public Vector2 ToLocal(double t, double v) { var r = Plot; return new Vector2(r.xMin + (float)(t / maxTime) * r.width, r.yMin + (float)(v / maxSpeed) * r.height); }
        public Vector2 ToValue(Vector2 local) { var r = Plot; return new Vector2((local.x - r.xMin) / r.width * maxTime, (local.y - r.yMin) / r.height * maxSpeed); }
        public bool Inside(Vector2 local, float margin = 0) { var r = Plot; return local.x >= r.xMin - margin && local.x <= r.xMax + margin && local.y >= r.yMin - margin && local.y <= r.yMax + margin; }

        public void Clear() { lines.Clear(); rings.Clear(); labels.Clear(); area = null; areaUntil = float.PositiveInfinity; cursor = -1; highlightSegment = -1; }

        public void Commit()
        {
            SetVerticesDirty();
            int n = 0;
            if (numbers && !mini)
            {
                for (float x = 0; x <= maxTime + 1e-3f; x += labelT) Text(n++, x.ToString("0.#"), ToLocal(x, 0) + new Vector2(0, -26), Color.white, numberSize, false);
                for (float y = 0; y <= maxSpeed + 1e-3f; y += labelV) Text(n++, y.ToString("0.#"), ToLocal(0, y) + new Vector2(-34, 0), Color.white, numberSize, false);
            }
            foreach (var l in labels) Text(n++, l.text, ToLocal(l.t, l.v) + l.offset, l.color, l.size, l.bold);
            for (int i = n; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
        }

        void Text(int i, string s, Vector2 at, Color c, float size, bool bold)
        {
            while (pool.Count <= i)
            {
                var go = new GameObject("Graph label", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(transform, false); go.layer = gameObject.layer;
                var t = go.GetComponent<TextMeshProUGUI>(); t.font = font; t.raycastTarget = false; t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.NoWrap; t.rectTransform.sizeDelta = new Vector2(320, 60); pool.Add(t);
            }
            var label = pool[i]; label.gameObject.SetActive(true); label.text = s; label.color = c; label.fontSize = size;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.rectTransform.localPosition = new Vector3(at.x, at.y, -1);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = Plot;
            if (grid)
            {
                var minor = new Color(.25f, .62f, 1f, mini ? .16f : .22f); var major = new Color(.35f, .75f, 1f, mini ? .3f : .42f);
                for (int i = 0; i * gridT <= maxTime + 1e-3f; i++) Seg(vh, ToLocal(i * gridT, 0), ToLocal(i * gridT, maxSpeed), i % 2 == 0 ? 1.6f : .9f, i % 2 == 0 ? major : minor);
                for (int j = 0; j * gridV <= maxSpeed + 1e-3f; j++) Seg(vh, ToLocal(0, j * gridV), ToLocal(maxTime, j * gridV), j % 2 == 0 ? 1.6f : .9f, j % 2 == 0 ? major : minor);
            }
            // Axes with arrow heads.
            float aw = mini ? 2.5f : 4f;
            Seg(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax + (mini ? 6 : 22), r.yMin), aw, Color.white);
            Seg(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax + (mini ? 6 : 22)), aw, Color.white);
            if (!mini)
            {
                Tri(vh, new Vector2(r.xMax + 34, r.yMin), new Vector2(r.xMax + 16, r.yMin + 9), new Vector2(r.xMax + 16, r.yMin - 9), Color.white);
                Tri(vh, new Vector2(r.xMin, r.yMax + 34), new Vector2(r.xMin - 9, r.yMax + 16), new Vector2(r.xMin + 9, r.yMax + 16), Color.white);
            }
            // Area under the graph (optionally filled only up to areaUntil, used during Q3 runs).
            if (area != null && area.Count >= 2)
                for (int i = 1; i < area.Count; i++)
                {
                    double t0 = area[i - 1].t, t1 = Math.Min(area[i].t, areaUntil); if (t1 <= t0) break;
                    Quad(vh, ToLocal(t0, 0), ToLocal(t1, 0), ToLocal(t1, MotionMath.Speed(area, t1)), ToLocal(t0, area[i - 1].v), areaColor);
                }
            foreach (var line in lines) DrawLine(vh, line);
            foreach (var ring in rings)
            {
                var c = ToLocal(ring.t, ring.v);
                if (ring.filled) Disc(vh, c, ring.radius, ring.color); else Circle(vh, c, ring.radius, mini ? 1.5f : 3.5f, ring.color);
            }
            if (cursor >= 0) { var a = ToLocal(cursor, 0); Seg(vh, a, new Vector2(a.x, r.yMax), mini ? 2 : 4, Color.white); Disc(vh, new Vector2(a.x, r.yMin), mini ? 3 : 8, Color.white); }
        }

        void DrawLine(VertexHelper vh, Line line)
        {
            var p = line.points; if (p == null) return;
            for (int i = 1; i < p.Count; i++)
            {
                Color c = line.color;
                if (line.slopeColors) { double d = p[i].v - p[i - 1].v; c = p[i].v < 1e-9 && p[i - 1].v < 1e-9 ? Zero : d > 1e-9 ? Rising : d < -1e-9 ? Falling : Flat; }
                float w = line.width * (i - 1 == highlightSegment && line.selected ? 1.5f : 1);
                Vector2 a = ToLocal(p[i - 1].t, p[i - 1].v), b = ToLocal(p[i].t, p[i].v);
                if (line.dashed) { for (int k = 0; k < 14; k += 2) Seg(vh, Vector2.Lerp(a, b, k / 14f), Vector2.Lerp(a, b, (k + 1) / 14f), w, c); }
                else { if (!mini) Seg(vh, a, b, w * 2.4f, new Color(c.r, c.g, c.b, .18f)); Seg(vh, a, b, w, c); }
            }
            if (line.drawPoints)
                for (int i = 0; i < p.Count; i++)
                {
                    var at = ToLocal(p[i].t, p[i].v); float rr = mini ? 4 : 13;
                    bool sel = i == line.selectedIndex;
                    if (!mini) Disc(at.x, at.y, rr + 9, new Color(.16f, .86f, 1, sel ? .45f : .22f), vh);
                    Disc(vh, at, rr + (mini ? 1.5f : 4), Color.white);
                    Disc(vh, at, rr, sel ? Color.white : line.slopeColors ? PointCyan : line.color);
                    if (sel && !mini) Circle(vh, at, rr + 12, 3, Color.white);
                }
        }

        void Disc(float x, float y, float r, Color c, VertexHelper vh) => Disc(vh, new Vector2(x, y), r, c);

        // ------------------------------------------------------------- primitives
        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
        {
            int n = vh.currentVertCount; var z = Vector2.zero;
            vh.AddVert(a, col, z); vh.AddVert(b, col, z); vh.AddVert(c, col, z); vh.AddVert(d, col, z);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
        static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color col) { int n = vh.currentVertCount; vh.AddVert(a, col, Vector2.zero); vh.AddVert(b, col, Vector2.zero); vh.AddVert(c, col, Vector2.zero); vh.AddTriangle(n, n + 1, n + 2); }
        public static void Seg(VertexHelper vh, Vector2 a, Vector2 b, float width, Color col)
        {
            var d = b - a; if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized * width * .5f; var e = d.normalized * width * .5f;
            Quad(vh, a - n - e, b - n + e, b + n + e, a + n - e, col);
        }
        static void Disc(VertexHelper vh, Vector2 c, float r, Color col)
        {
            int n = vh.currentVertCount; vh.AddVert(c, col, Vector2.zero);
            for (int i = 0; i <= 24; i++) { float a = i * Mathf.PI * 2 / 24; vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col, Vector2.zero); }
            for (int i = 0; i < 24; i++) vh.AddTriangle(n, n + 1 + i, n + 2 + i);
        }
        static void Circle(VertexHelper vh, Vector2 c, float r, float width, Color col)
        {
            for (int i = 0; i < 28; i++)
            {
                float a = i * Mathf.PI * 2 / 28, b = (i + 1) * Mathf.PI * 2 / 28;
                Seg(vh, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r, width, col);
            }
        }

        /// <summary>Distance in canvas units from a local point to the polyline segment i (for hover).</summary>
        public float SegmentDistance(IList<GraphPoint> p, int i, Vector2 local)
        {
            Vector2 a = ToLocal(p[i].t, p[i].v), b = ToLocal(p[i + 1].t, p[i + 1].v), ab = b - a;
            float k = Mathf.Clamp01(Vector2.Dot(local - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
            return Vector2.Distance(local, a + ab * k);
        }

        /// <summary>Make the graph render even in batch/edit mode screenshots.</summary>
        public void ForceRebuild() { Commit(); Rebuild(CanvasUpdate.PreRender); UpdateGeometry(); }
    }
}