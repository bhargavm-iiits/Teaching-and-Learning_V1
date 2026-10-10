using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Pure-logic checks from section 13 of the brief. Runs in the EditMode tests and in the builder
    /// validation. Throws on the first failure; returns a PASS summary.
    /// </summary>
    public static class LogicTestSuite
    {
        public static string Run(QuestionCatalog catalog)
        {
            int checks = 0;
            void Require(bool ok, string message) { checks++; if (!ok) throw new Exception("Logic test failed: " + message); }
            var course = catalog.course;
            GraphPoint[] Copy(GraphPoint[] p) => (GraphPoint[])p.Clone();
            bool Pass(VariantData v, int lane, IList<GraphPoint> g) => QuestionRules.Evaluate(v.question, lane, v.lanes[lane], g, course).pass;

            // Stop distance in the data equals the graph area for every answer.
            foreach (var v in catalog.variants.Concat(new[] { catalog.demo }))
                for (int lane = 0; lane < 3; lane++)
                    Require(Math.Abs(MotionMath.Distance(v.lanes[lane].solution) - v.lanes[lane].stop) < 1e-9, v.id + " lane " + lane + " stop != area");

            // Demo: shared answer is 64 m for all three and cannot fail.
            foreach (var lane in catalog.demo.lanes) Require(Math.Abs(lane.stop - catalog.demo.lanes[0].stop) < 1e-9, "Demo: one shared stop for all three");
            Require(QuestionRules.EvaluateAll(catalog.demo, new IList<GraphPoint>[] { new GraphPoint[] { new GraphPoint(0, 0), new GraphPoint(2, 3) }, new GraphPoint[0], new GraphPoint[0] }.Select(x => x.Count == 0 ? (IList<GraphPoint>)catalog.demo.lanes[0].solution : x).ToArray(), course).pass, "Demo must not fail");

            foreach (var v in catalog.variants)
            {
                // Every answer / sample solution passes, all three vehicles together.
                var all = QuestionRules.EvaluateAll(v, v.lanes.Select(l => (IList<GraphPoint>)l.solution).ToArray(), course);
                Require(all.pass, v.id + ": " + all.Message);
                for (int lane = 0; lane < 3; lane++)
                {
                    var s = v.lanes[lane].solution; string id = v.id + " " + v.lanes[lane].vehicle;
                    // Equivalent graph (extra collinear points) passes.
                    var expanded = new List<GraphPoint>();
                    for (int i = 0; i < s.Length; i++) { expanded.Add(s[i]); if (i < s.Length - 1) expanded.Add(new GraphPoint((s[i].t + s[i + 1].t) / 2, (s[i].v + s[i + 1].v) / 2)); }
                    if (expanded.Count <= course.maxPoints) Require(Pass(v, lane, expanded), id + " equivalent graph must pass");
                    // Last speed > 0 fails.
                    var moving = Copy(s); moving[moving.Length - 1].v = 1; Require(!Pass(v, lane, moving), id + " last speed > 0 must fail");
                    if (v.question <= 2)
                    {
                        // Key point changed by +-1 m/s fails.
                        double key = 4 * course.speedTolerance;
                        foreach (double d in new[] { -key, key })
                        {
                            var wrong = Copy(s); int k = s.Length > 2 ? 1 : 0; wrong[k].v = Math.Max(0, wrong[k].v + d);
                            if (Math.Abs(wrong[k].v - s[k].v) > 1e-9) Require(!Pass(v, lane, wrong), id + " key point " + d + " must fail");
                        }
                        // Wrong slope (first corner one second late) fails.
                        var slope = Copy(s); slope[1].t += 1;
                        if (s.Length > 2 && slope[1].t < s[2].t) Require(!Pass(v, lane, slope), id + " wrong slope must fail");
                    }
                    if (v.question == 3)
                    {
                        var lanedata = v.lanes[lane]; double area = MotionMath.Distance(s);
                        // +3 tolerance-widths of area fails (stretch time).
                        var stretched = Copy(s); for (int i = 0; i < stretched.Length; i++) stretched[i].t *= 1 + 3 * course.stopTolerance / area;
                        Require(!Pass(v, lane, stretched), id + " +3 m must fail");
                        // Over the time limit fails.
                        var late = Copy(s); late[late.Length - 1].t = lanedata.timeLimit + 1; Require(!Pass(v, lane, late), id + " time limit must fail");
                        // Over the speed limit fails.
                        var fast = Copy(s); int peak = Array.FindIndex(fast, p => p.v == fast.Max(q => q.v)); fast[peak].v = lanedata.speedLimit + 1;
                        Require(!Pass(v, lane, fast), id + " speed limit must fail");
                        if (lanedata.HasGate)
                        {
                            // Missing the gate speed: a slow triangle that still stops in the box.
                            double T = 2 * lanedata.stop / (lanedata.gateMin - 1) ;
                            var slowGate = new[] { new GraphPoint(0, 0), new GraphPoint(T / 2, lanedata.gateMin - 1), new GraphPoint(T, 0) };
                            var r = QuestionRules.Evaluate(3, lane, lanedata, slowGate, course);
                            Require(r.checklist.Any(c => !c.ok && c.text.StartsWith("speed gate")), id + " gate breach not detected");
                            // Slow-zone breach: cruise above the zone limit through the zone.
                            double vz = lanedata.zoneMax + 1, tAcc = 2, tz = (lanedata.zoneEnd + 1 - vz * tAcc / 2) / vz + tAcc;
                            var breach = new[] { new GraphPoint(0, 0), new GraphPoint(tAcc, vz), new GraphPoint(tz, vz), new GraphPoint(tz + 2, 0) };
                            var rz = QuestionRules.Evaluate(3, lane, lanedata, breach, course);
                            Require(rz.checklist.Any(c => !c.ok && c.text.StartsWith("slow zone")), id + " slow-zone breach not detected");
                        }
                    }
                    // TimeAtDistance matches bisection.
                    double total = MotionMath.Distance(s);
                    for (int k = 1; k <= 15; k++)
                    {
                        double dist = total * k / 16, exact = MotionMath.TimeAtDistance(s, dist), lo = 0, hi = MotionMath.Duration(s);
                        for (int it = 0; it < 80; it++) { double mid = (lo + hi) / 2; if (MotionMath.Distance(s, mid) < dist) lo = mid; else hi = mid; }
                        Require(Math.Abs(exact - (lo + hi) / 2) < 1e-7, id + " TimeAtDistance differs from bisection");
                    }
                }
            }

            // Graph editing rules.
            var g = new GraphModel();
            g.Add(4, 8); g.Add(8, 8); g.Add(12, 0);
            Require(g.Points.Count == 4 && g.Points[1].t == 4 && g.Points[3].t == 12, "points kept in time order");
            g.BeginMove(); g.Move(1, 15, 7); Require(g.Points[1].t == 7, "move clamped between neighbours");
            g.Move(0, 9, 6); Require(g.Points[0].t == 0 && g.Points[0].v == 6, "P0 time locked, speed editable");
            g.Add(8, 5); Require(g.Points.Count == 4 && g.Points[2].v == 5, "point at an existing time moves instead of duplicating");
            g.Add(2.4, 13); Require(g.Points.Any(p => p.t == 2 && p.v == 12), "snap and speed clamp");
            g.Selected = 0; Require(!g.DeleteSelected(), "P0 cannot be deleted");
            int before = g.Points.Count; g.Selected = 2; Require(g.DeleteSelected() && g.Points.Count == before - 1, "delete selected");
            Require(g.Undo() && g.Points.Count == before, "undo delete");
            g.Clear(); Require(g.Points.Count == 1 && g.Points[0].v == 6, "clear keeps P0 and its speed"); Require(g.Undo() && g.Points.Count == before, "undo clear");
            var half = new GraphModel(.5); half.Add(5.3, 3.7); Require(half.Points.Any(p => p.t == 5.5 && p.v == 3.5), "0.5 snap");
            var full = new GraphModel(.5); for (int i = 1; i <= 25; i++) full.Add(i * .5, 3); Require(full.Points.Count == course.maxPoints, "max 10 points");
            var hist = new GraphModel(); for (int i = 0; i < 30; i++) hist.Add(0, i % 12);
            for (int i = 0; i < GraphModel.UndoDepth; i++) Require(hist.Undo(), "20-step undo"); Require(!hist.Undo(), "undo limited to 20");
            for (int i = 0; i < g.Points.Count - 1; i++) Require(g.Points[i + 1].t > g.Points[i].t, "no duplicate times");

            // Adaptive selection: unseen first, never the variant just shown.
            var progress = new ProgressState();
            for (int q = 1; q <= 3; q++)
            {
                int previous = -1; var seen = new HashSet<int>();
                for (int i = 0; i < 16; i++)
                {
                    int next = AdaptiveSelection.Next(q, progress);
                    Require(next != previous, "adaptive repeated the last variant");
                    if (i < 4) Require(seen.Add(next), "adaptive repeated before all unseen were used");
                    previous = next;
                }
            }

            // Final check: every combination routes correctly.
            for (int mask = 0; mask < 8; mask++)
            {
                bool[] c = { (mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0 };
                int score = FinalCheckRouting.Score(c);
                var route = FinalCheckRouting.Route(c);
                Require(score == c.Count(x => x), "score");
                Require(route == (score == 3 ? FinalRoute.TopicComplete : score == 0 ? FinalRoute.RestartDemo : FinalRoute.Remediate), "route kind " + mask);
                var replay = FinalCheckRouting.QuestionsToReplay(c);
                if (route == FinalRoute.Remediate)
                {
                    Require(replay.Length == 3 - score, "replay count " + mask);
                    for (int i = 0; i < 3; i++) Require(Array.IndexOf(replay, FinalCheckRouting.ConceptToQuestion[i]) >= 0 == !c[i], "concept route " + mask);
                    Require(FinalCheckRouting.ItemsToReask(c).Length == 3 - score, "re-ask only missed " + mask);
                }
                else Require(replay.Length == 0, "no replay " + mask);
            }

            // Final-check content: slope, area, crossing facts.
            Require(MotionMath.Acceleration(catalog.Item(0, false).a, 1) == 12 && MotionMath.Acceleration(catalog.Item(0, true).a, 1) == 8, "final slope answers (12, 8 m/s²)");
            Require(MotionMath.Distance(catalog.Item(1, false).a) == 320 && MotionMath.Distance(catalog.Item(1, true).a) == 224, "final area answers (320, 224 m)");
            foreach (bool b in new[] { false, true })
            {
                var cross = catalog.Item(2, b); double tc = MotionMath.CrossingTime(cross.a, cross.b);
                Require(Math.Abs(tc - (b ? 6 : 5)) < 1e-9, "crossing time");
                Require(Math.Abs(MotionMath.Speed(cross.a, tc) - MotionMath.Speed(cross.b, tc)) < 1e-9, "crossing = equal speed");
                Require(MotionMath.Distance(cross.b, tc) == 2 * MotionMath.Distance(cross.a, tc), "crossing positions differ (" + (b ? "72 vs 144" : "50 vs 100") + ")");
                Require(Math.Abs(MotionMath.Distance(cross.a, 2 * tc) - MotionMath.Distance(cross.b, 2 * tc)) < 1e-9, "catch-up at twice the crossing time");
            }

            return $"PASS: {checks} pure-logic assertions (all answers and sample solutions for 3 vehicles, equivalent graphs, ±1 m/s, wrong slope, +3 m, slow zone, gate, last speed, time and speed limits, snap/ordering/P0, TimeAtDistance vs bisection, adaptive selection, all 8 final-check routes).";
        }
    }
}