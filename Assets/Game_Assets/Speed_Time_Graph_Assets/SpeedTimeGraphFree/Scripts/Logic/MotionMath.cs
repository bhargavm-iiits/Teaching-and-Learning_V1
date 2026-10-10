using System;
using System.Collections.Generic;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Single source of truth for the physics. v(t) is piecewise linear; a = dv/dt per segment;
    /// s(t) = sum of [v1*dt + a*dt*dt/2] = area under the graph up to t. After the last point the
    /// vehicle is frozen (it does not keep moving), see EndsMoving.
    /// </summary>
    public static class MotionMath
    {
        public static double Duration(IList<GraphPoint> p) => p.Count == 0 ? 0 : p[p.Count - 1].t;
        public static bool EndsMoving(IList<GraphPoint> p) => p.Count > 0 && p[p.Count - 1].v > 1e-9;

        public static double Speed(IList<GraphPoint> p, double time)
        {
            if (p.Count == 0) return 0;
            if (time <= 0) return p[0].v;
            for (int i = 1; i < p.Count; i++)
                if (time <= p[i].t) return p[i - 1].v + (p[i].v - p[i - 1].v) * (time - p[i - 1].t) / (p[i].t - p[i - 1].t);
            return p[p.Count - 1].v;
        }

        /// <summary>Acceleration of the segment containing the time (0 outside the graph).</summary>
        public static double Acceleration(IList<GraphPoint> p, double time)
        {
            for (int i = 1; i < p.Count; i++)
                if (time < p[i].t) return time < p[i - 1].t ? 0 : SegmentSlope(p[i - 1], p[i]);
            return 0;
        }

        public static double SegmentSlope(GraphPoint a, GraphPoint b) => (b.v - a.v) / (b.t - a.t);
        public static double SegmentArea(GraphPoint a, GraphPoint b) => (b.t - a.t) * (a.v + b.v) * .5;

        /// <summary>Distance travelled by time t (area under v from 0 to min(t, end)).</summary>
        public static double Distance(IList<GraphPoint> p, double time = double.PositiveInfinity)
        {
            double sum = 0;
            for (int i = 1; i < p.Count; i++)
            {
                double dt = Math.Max(0, Math.Min(time, p[i].t) - p[i - 1].t);
                double a = SegmentSlope(p[i - 1], p[i]);
                sum += p[i - 1].v * dt + .5 * a * dt * dt;
                if (time <= p[i].t) break;
            }
            return sum;
        }

        /// <summary>
        /// Exact time at which a distance is first reached, solving the quadratic inside the
        /// segment. NaN if never reached.
        /// </summary>
        public static double TimeAtDistance(IList<GraphPoint> p, double distance)
        {
            if (distance <= 0) return 0;
            double sum = 0;
            for (int i = 1; i < p.Count; i++)
            {
                double area = SegmentArea(p[i - 1], p[i]);
                if (area > 0 && distance <= sum + area + 1e-9)
                {
                    double d = Math.Max(0, distance - sum), v = p[i - 1].v, a = SegmentSlope(p[i - 1], p[i]);
                    if (d < 1e-12) return p[i - 1].t;
                    // Numerically stable root of a*dt*dt/2 + v*dt - d = 0.
                    double dt = Math.Abs(a) < 1e-12 ? d / v : 2 * d / (v + Math.Sqrt(Math.Max(0, v * v + 2 * a * d)));
                    return Math.Min(p[i].t, p[i - 1].t + dt);
                }
                sum += area;
            }
            return double.NaN;
        }

        /// <summary>
        /// Maximum speed while the vehicle is inside the distance range [start, end].
        /// v is piecewise linear in t, so the maximum lies at the entry or exit time or at a vertex.
        /// Returns +infinity if the range is never reached.
        /// </summary>
        public static double MaxSpeedInDistanceRange(IList<GraphPoint> p, double start, double end, out double atDistance)
        {
            atDistance = start;
            double t0 = TimeAtDistance(p, start);
            if (double.IsNaN(t0)) return double.PositiveInfinity;
            double t1 = TimeAtDistance(p, end);
            if (double.IsNaN(t1)) t1 = Duration(p);
            double max = Speed(p, t0);
            double v1 = Speed(p, t1);
            if (v1 > max) { max = v1; atDistance = Math.Min(end, Distance(p, t1)); }
            foreach (var point in p)
                if (point.t > t0 && point.t < t1 && point.v > max) { max = point.v; atDistance = Distance(p, point.t); }
            return max;
        }

        /// <summary>First time t &gt; 0 at which two graphs have equal speed (lines cross), or NaN. Exact on each common linear piece.</summary>
        public static double CrossingTime(IList<GraphPoint> a, IList<GraphPoint> b)
        {
            var ts = new SortedSet<double>(); foreach (var p in a) ts.Add(p.t); foreach (var p in b) ts.Add(p.t);
            double end = Math.Min(Duration(a), Duration(b)); double prev = 0;
            foreach (double t in ts)
            {
                if (t <= prev) continue; double t1 = Math.Min(t, end);
                double d0 = Speed(a, prev) - Speed(b, prev), d1 = Speed(a, t1) - Speed(b, t1);
                if (prev > 0 && Math.Abs(d0) < 1e-9) return prev;
                if (d0 * d1 < 0 || Math.Abs(d1) < 1e-9) return prev + (t1 - prev) * d0 / (d0 - d1);
                prev = t1; if (t1 >= end) break;
            }
            return double.NaN;
        }

        /// <summary>First time the speed rises above a limit, or NaN.</summary>
        public static double FirstTimeAbove(IList<GraphPoint> p, double limit)
        {
            if (p.Count == 0) return double.NaN;
            if (p[0].v > limit + 1e-9) return 0;
            for (int i = 1; i < p.Count; i++)
                if (p[i].v > limit + 1e-9)
                {
                    var a = p[i - 1]; var b = p[i];
                    return a.t + (limit - a.v) / (b.v - a.v) * (b.t - a.t);
                }
            return double.NaN;
        }
    }
}
