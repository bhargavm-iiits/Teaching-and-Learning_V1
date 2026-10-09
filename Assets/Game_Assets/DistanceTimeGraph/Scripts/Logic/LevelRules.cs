using System;

namespace DistanceTimeGraph.Logic
{
    /// What a run of the graph does to the car: the car's distance sampled over time (after any safety stop), whether
    /// the level's goal was met, and which graph section explains the result. Replays play the same profile back, so
    /// the car, the cursor and the bridge stay in step with the original run.
    [Serializable]
    public class RunProfile
    {
        public const float Step = 0.05f;

        public float[] distance = new float[0];
        public float duration;
        public bool success;
        public bool safetyStop;       // the timed bridge held the car at the platform
        public float stopTime = -1f;  // when the safety stop happened
        public int causeSegment = -1; // graph segment to highlight after a failure
        public string headline = "";
        public string detail = "";
        public float arrivalTime = -1f; // Level 2: when the far platform is (or would be) reached

        public float DistanceAt(float t)
        {
            if (distance.Length == 0) return 0f;
            float f = Math.Max(0f, t) / Step;
            int i = (int)f;
            if (i >= distance.Length - 1) return distance[distance.Length - 1];
            return distance[i] + (distance[i + 1] - distance[i]) * (f - i);
        }

        public float VelocityAt(float t)
        {
            const float h = Step;
            return (DistanceAt(t + h) - DistanceAt(t - h)) / (2 * h);
        }
    }

    /// The success checks of the three levels and the safe car motion they produce.
    public static class LevelRules
    {
        public const float Tolerance = 0.5f;
        public const float Hold = 0.6f;           // the car rests this long after the graph ends before the result shows
        public const float StopHold = 1.2f;       // and this long after a safety stop

        public static RunProfile Run(GraphModel g, VariantSpec v)
        {
            var p = new RunProfile();
            float end = Math.Max(1f, g.EndTime);
            float holdFrom = float.MaxValue, holdAt = 0f;

            if (v.kind == LevelKind.Waiting) JudgeBridge(g, v, p, ref holdFrom, ref holdAt);
            else if (v.kind == LevelKind.Rising) JudgeRising(g, v, p);
            else JudgeReturning(g, v, p);

            p.duration = p.safetyStop ? Math.Min(end, holdFrom) + StopHold : end + Hold;
            int n = (int)Math.Ceiling(p.duration / RunProfile.Step) + 1;
            p.distance = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i * RunProfile.Step;
                p.distance[i] = t >= holdFrom ? holdAt : g.Evaluate(t);
            }
            return p;
        }

        // ---------- Level 1: move away from the start ----------
        static void JudgeRising(GraphModel g, VariantSpec v, RunProfile p)
        {
            int last = g.SegmentCount - 1;
            float final = g[g.Count - 1].d;
            bool rising = g.HasRisingSegment();
            p.success = rising && Math.Abs(final - v.goal) <= Tolerance;
            p.causeSegment = last;
            if (p.success) { p.headline = $"The car reached {v.goal} m."; return; }
            if (g.Count < 2) { p.headline = "The graph has only its starting point."; p.detail = "With one point there is no line, so the car never moved."; return; }
            if (!rising) { p.headline = "The graph never went up."; p.detail = "The car never moved away from the start."; return; }
            if (final < v.goal) { p.headline = $"The car stopped at {final:0} m."; p.detail = $"The {v.place.ToLowerInvariant()} is at {v.goal} m."; }
            else { p.headline = $"The car went past, to {final:0} m."; p.detail = $"The {v.place.ToLowerInvariant()} is at {v.goal} m."; }
        }

        // ---------- Level 2: wait, then cross the timed bridge ----------
        static void JudgeBridge(GraphModel g, VariantSpec v, RunProfile p, ref float holdFrom, ref float holdAt)
        {
            int pad = v.waitPad, far = v.bridgeEnd;
            // k: the first point beyond the pad. The car leaves the pad during segment k-1 -> k.
            int k = -1;
            for (int i = 0; i < g.Count; i++) if (g[i].d > pad) { k = i; break; }
            bool reachedPad = false;
            for (int i = 0; i < g.Count; i++) if (g[i].d >= pad) { reachedPad = true; break; }
            if (!reachedPad)
            {
                p.causeSegment = g.SegmentCount - 1;
                p.headline = $"The car stopped at {g[g.Count - 1].d} m.";
                p.detail = $"It never reached the {pad} m sync pad.";
                return;
            }
            if (k < 0)
            {
                p.causeSegment = g.SegmentCount - 1;
                p.headline = "The car never started across the bridge.";
                p.detail = $"It stayed at {g[g.Count - 1].d} m.";
                return;
            }

            float leave, wait = 0f;
            var before = g[k - 1];
            if (before.d == pad)
            {
                leave = before.t;
                int j = k - 1;
                while (j > 0 && g[j - 1].d == pad) j--;
                wait = before.t - g[j].t;
            }
            else leave = Cross(g, k - 1, pad);   // passed through the pad without stopping

            float arrive = -1f;
            for (int s = k - 1; s < g.SegmentCount; s++)
                if (g[s + 1].d >= far) { arrive = Math.Max(leave, Cross(g, s, far)); break; }
            p.arrivalTime = arrive;

            string why = null, detail = null;
            if (wait + 1e-4f < v.waitTime)
            {
                why = wait <= 0f ? "The car did not wait at the sync pad." : $"The car waited only {wait:0} s at the sync pad.";
                detail = $"The pad needs {v.waitTime:0} s before the bridge will sync.";
            }
            else if (leave + 1e-4f < v.bridgeOpen)
            {
                why = $"The car tried to cross at {leave:0.#} s.";
                detail = $"The bridge opens at {v.bridgeOpen:0.#} s.";
            }
            else if (arrive < 0f)
            {
                why = "The car would stop on the bridge.";
                detail = $"It never reaches {far} m, so the pad kept it on the platform.";
            }
            else if (arrive > v.bridgeClose + 1e-4f)
            {
                why = $"The crossing would end at {arrive:0.#} s.";
                detail = $"The bridge closes at {v.bridgeClose:0.#} s.";
            }

            if (why != null)
            {
                // The sync pad holds the car safely on the platform: no crash, no fall.
                p.safetyStop = true;
                p.stopTime = leave;
                holdFrom = leave;
                holdAt = pad;
                p.causeSegment = k - 1;
                p.headline = why;
                p.detail = detail;
                return;
            }
            p.success = true;
            p.causeSegment = k - 1;
            p.headline = $"The car waited {wait:0} s, then crossed from {leave:0.#} s to {arrive:0.#} s.";
        }

        // ---------- Level 3: out to the beacon and back ----------
        static void JudgeReturning(GraphModel g, VariantSpec v, RunProfile p)
        {
            float reachTime = -1f;
            int max = 0, maxIndex = 0;
            for (int i = 0; i < g.Count; i++)
            {
                if (g[i].d > max) { max = g[i].d; maxIndex = i; }
                if (reachTime < 0 && g[i].d >= v.turnaround - Tolerance) reachTime = g[i].t;
            }
            bool descendsAfter = false;
            if (reachTime >= 0)
                for (int s = 0; s < g.SegmentCount; s++)
                    if (g[s].t >= reachTime && g.Slope(s) < 0) { descendsAfter = true; break; }
            float final = g[g.Count - 1].d;
            p.causeSegment = g.SegmentCount - 1;

            if (reachTime < 0)
            {
                p.causeSegment = Math.Max(0, maxIndex - 1);
                p.headline = max == 0 ? "The car never left the start." : $"The car turned back at {max} m.";
                p.detail = $"The turnaround beacon is at {v.turnaround} m.";
                return;
            }
            if (!descendsAfter)
            {
                p.headline = $"The car reached {v.turnaround} m but never came back.";
                p.detail = $"It ended at {final:0} m.";
                return;
            }
            if (Math.Abs(final - v.goal) > Tolerance)
            {
                p.headline = $"The car came back to {final:0} m.";
                p.detail = $"The {v.place.ToLowerInvariant()} is at {v.goal} m.";
                return;
            }
            p.success = true;
            p.headline = $"The car reached {v.turnaround} m and reversed to {v.goal} m.";
        }

        /// Time during segment s when the distance first equals d (the segment must reach d).
        static float Cross(GraphModel g, int s, float d)
        {
            var a = g[s];
            var b = g[s + 1];
            if (b.d == a.d) return a.t;
            float f = (d - a.d) / (b.d - a.d);
            return a.t + Math.Max(0f, Math.Min(1f, f)) * (b.t - a.t);
        }
    }
}
