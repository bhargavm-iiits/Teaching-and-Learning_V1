using System;
using System.Collections.Generic;

namespace DistanceTimeGraph.Logic
{
    /// One point on the Distance-from-Start–Time graph, already snapped: whole seconds and multiples of 5 m.
    [Serializable]
    public struct GraphPoint
    {
        public int t;   // seconds
        public int d;   // metres from the start

        public GraphPoint(int t, int d) { this.t = t; this.d = d; }
        public override string ToString() => $"({t} s, {d} m)";
    }

    /// The car's movement program. Points stay ordered by time, no two share a time, and the first point is the car's
    /// starting place at 0 s (it cannot be moved or deleted). Between points the distance is interpolated linearly.
    [Serializable]
    public class GraphModel
    {
        public const int TimeStep = 1;
        public const int DistanceStep = 5;

        public int maxTime = 20;
        public int maxDistance = 80;
        public List<GraphPoint> points = new List<GraphPoint> { new GraphPoint(0, 0) };

        public int Count => points.Count;
        public GraphPoint this[int i] => points[i];
        public int EndTime => points[points.Count - 1].t;

        public GraphModel() { }
        public GraphModel(int maxTime, int maxDistance, int start)
        {
            this.maxTime = maxTime;
            this.maxDistance = maxDistance;
            Reset(start);
        }

        public GraphModel Clone()
        {
            var copy = new GraphModel { maxTime = maxTime, maxDistance = maxDistance };
            copy.points = new List<GraphPoint>(points);
            return copy;
        }

        public void Reset(int start)
        {
            points.Clear();
            points.Add(new GraphPoint(0, SnapDistance(start)));
        }

        public static int SnapTime(float t) => (int)Math.Round(t / TimeStep, MidpointRounding.AwayFromZero) * TimeStep;
        public static int SnapDistance(float d) => (int)Math.Round(d / DistanceStep, MidpointRounding.AwayFromZero) * DistanceStep;

        public int ClampTime(int t) => Math.Max(0, Math.Min(maxTime, t));
        public int ClampDistance(int d) => Math.Max(0, Math.Min(maxDistance, d));

        public int IndexOfTime(int t)
        {
            for (int i = 0; i < points.Count; i++) if (points[i].t == t) return i;
            return -1;
        }

        /// Adds a snapped point. Returns its index, or -1 when that time is already taken or outside the graph.
        public int Add(float time, float distance)
        {
            int t = SnapTime(time), d = ClampDistance(SnapDistance(distance));
            if (t <= 0 || t > maxTime || IndexOfTime(t) >= 0) return -1;
            int i = 0;
            while (i < points.Count && points[i].t < t) i++;
            points.Insert(i, new GraphPoint(t, d));
            return i;
        }

        /// Moves point i as close as allowed to (time, distance): snapped, inside the graph, and strictly between its
        /// neighbours in time, so the order never changes and no two points share a time. Returns true if it moved.
        public bool Move(int i, float time, float distance)
        {
            if (i <= 0 || i >= points.Count) return false;
            int lo = points[i - 1].t + TimeStep;
            int hi = i + 1 < points.Count ? points[i + 1].t - TimeStep : maxTime;
            int t = Math.Max(lo, Math.Min(hi, SnapTime(time)));
            int d = ClampDistance(SnapDistance(distance));
            var old = points[i];
            if (old.t == t && old.d == d) return false;
            points[i] = new GraphPoint(t, d);
            return true;
        }

        public bool Delete(int i)
        {
            if (i <= 0 || i >= points.Count) return false;
            points.RemoveAt(i);
            return true;
        }

        /// Distance from the start at time t. Before 0 s it is the first point; after the last point the car stays put.
        public float Evaluate(float time)
        {
            if (time <= points[0].t) return points[0].d;
            for (int i = 1; i < points.Count; i++)
            {
                var a = points[i - 1];
                var b = points[i];
                if (time <= b.t)
                    return a.d + (b.d - a.d) * (time - a.t) / (float)(b.t - a.t);
            }
            return points[points.Count - 1].d;
        }

        /// Index of the segment (from point k to k+1) active at time t, or -1 before the first or after the last point.
        public int SegmentAt(float time)
        {
            for (int i = 1; i < points.Count; i++)
                if (time >= points[i - 1].t && time <= points[i].t) return i - 1;
            return -1;
        }

        /// Velocity in m/s at time t: the slope of the active segment. Zero after the graph ends.
        public float Velocity(float time)
        {
            int s = SegmentAt(time);
            return s < 0 ? 0f : Slope(s);
        }

        public float Slope(int segment)
        {
            var a = points[segment];
            var b = points[segment + 1];
            return (b.d - a.d) / (float)(b.t - a.t);
        }

        public int SegmentCount => points.Count - 1;
        public bool HasRisingSegment() { for (int s = 0; s < SegmentCount; s++) if (Slope(s) > 0) return true; return false; }

        public GraphPoint[] ToArray() => points.ToArray();
    }
}
