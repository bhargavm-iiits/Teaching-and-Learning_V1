using System;
using System.Collections.Generic;

namespace SpeedTimeGraphFree
{
    /// <summary>One (time, speed) point of a speed–time graph.</summary>
    [Serializable]
    public struct GraphPoint
    {
        public double t, v;
        public GraphPoint(double time, double speed) { t = time; v = speed; }
        public override string ToString() => $"({t:0.##},{v:0.##})";
    }

    /// <summary>
    /// A free, editable speed–time graph. Rules: P0 is always at t = 0 (its speed is editable),
    /// points stay in time order with no duplicate times (so no vertical jumps), at most
    /// MaxPoints points, everything snapped, 20-step undo.
    /// </summary>
    public sealed class GraphModel
    {
        public const int UndoDepth = 20;
        public readonly List<GraphPoint> Points = new List<GraphPoint>();
        readonly List<GraphPoint[]> history = new List<GraphPoint[]>();
        public double Snap = 1, MaxTime = 16, MaxSpeed = 12;
        /// <summary>Velocity snap (m/s). Defaults to the time snap.</summary>
        public double SnapV;
        public int MaxPoints = 10;
        public int Selected = -1;
        /// <summary>Incremented on every change so views can cheaply detect edits.</summary>
        public int Version;

        public GraphModel(double snap = 1, double snapV = 0) { Snap = snap; SnapV = snapV > 0 ? snapV : snap; Points.Add(new GraphPoint(0, 0)); }

        static double Clamp(double x, double lo, double hi) => Math.Max(lo, Math.Min(hi, x));
        public double SnapValue(double x) => Math.Round(x / Snap, MidpointRounding.AwayFromZero) * Snap;
        public double SnapSpeed(double v) { double s = SnapV > 0 ? SnapV : Snap; return Math.Round(v / s, MidpointRounding.AwayFromZero) * s; }
        void Save() { history.Add(Points.ToArray()); if (history.Count > UndoDepth) history.RemoveAt(0); }
        void Changed() { Version++; }
        public int UndoCount => history.Count;

        /// <summary>Replace all points (data loading, hints, tests). Clears undo.</summary>
        public void Set(IEnumerable<GraphPoint> points)
        {
            Points.Clear(); Points.AddRange(points);
            if (Points.Count == 0 || Points[0].t != 0) Points.Insert(0, new GraphPoint(0, Points.Count == 0 ? 0 : Points[0].v));
            history.Clear(); Selected = -1; Changed();
        }

        /// <summary>
        /// Add a snapped point. If a point already exists at that time it is moved instead
        /// (this is also how the P0 speed is edited). Returns the index, or -1 if the graph is full.
        /// </summary>
        public int Add(double time, double speed)
        {
            time = Clamp(SnapValue(time), 0, MaxTime); speed = Clamp(SnapSpeed(speed), 0, MaxSpeed);
            for (int i = 0; i < Points.Count; i++)
                if (Math.Abs(Points[i].t - time) < 1e-6)
                {
                    Save(); Points[i] = new GraphPoint(time, speed); Selected = i; Changed(); return i;
                }
            if (Points.Count >= MaxPoints) return -1;
            Save();
            int index = Points.FindIndex(p => p.t > time); if (index < 0) index = Points.Count;
            Points.Insert(index, new GraphPoint(time, speed)); Selected = index; Changed();
            return index;
        }

        /// <summary>Call once when a drag starts so the whole drag is one undo step.</summary>
        public void BeginMove() { Save(); }

        /// <summary>Move a point: time clamped between neighbours (one snap step apart), speed 0..max. P0 time locked.</summary>
        public bool Move(int index, double time, double speed)
        {
            if (index < 0 || index >= Points.Count) return false;
            double lo = index == 0 ? 0 : Points[index - 1].t + Snap;
            double hi = index == 0 ? 0 : index == Points.Count - 1 ? MaxTime : Points[index + 1].t - Snap;
            if (lo > hi + 1e-9) return false;
            var next = new GraphPoint(index == 0 ? 0 : Clamp(SnapValue(time), lo, hi), Clamp(SnapSpeed(speed), 0, MaxSpeed));
            bool changed = Math.Abs(next.t - Points[index].t) > 1e-6 || Math.Abs(next.v - Points[index].v) > 1e-6;
            Points[index] = next; Selected = index; if (changed) Changed();
            return changed;
        }

        /// <summary>Delete the selected point. P0 can never be deleted.</summary>
        public bool DeleteSelected()
        {
            if (Selected <= 0 || Selected >= Points.Count) return false;
            Save(); Points.RemoveAt(Selected); Selected = -1; Changed(); return true;
        }

        /// <summary>Clear back to P0 only (P0 keeps its speed so already-moving stories survive).</summary>
        public void Clear()
        {
            Save(); var first = Points[0]; Points.Clear(); Points.Add(new GraphPoint(0, first.v)); Selected = 0; Changed();
        }

        public bool Undo()
        {
            if (history.Count == 0) return false;
            Points.Clear(); Points.AddRange(history[history.Count - 1]); history.RemoveAt(history.Count - 1);
            Selected = -1; Changed(); return true;
        }
    }
}
