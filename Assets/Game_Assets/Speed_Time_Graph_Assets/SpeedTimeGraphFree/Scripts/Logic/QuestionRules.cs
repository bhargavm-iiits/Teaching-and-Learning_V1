using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpeedTimeGraphFree
{
    [Serializable]
    public struct CheckItem
    {
        public bool ok; public string text;
        public CheckItem(bool pass, string label) { ok = pass; text = label; }
        public override string ToString() => (ok ? "✓ " : "○ ") + text;
    }

    /// <summary>Result for one vehicle after (or before) a RUN.</summary>
    public class VehicleResult
    {
        public int lane; public string vehicle;
        public bool pass;
        public List<CheckItem> checklist = new List<CheckItem>();
        public string firstFailure = "";
        public double markerTime = -1;          // red marker on the graph (first mismatch)
        public double distance;                 // graph area = where the vehicle stops (m)
        public double stopError;                // distance - stop (m), signed
        public bool endsMoving, offCourse;
        public string Message => pass ? vehicle + ": correct!" : firstFailure;
    }

    public class RunResult
    {
        public bool pass; public VehicleResult[] vehicles;
        public VehicleResult FirstFailure => Array.Find(vehicles, v => !v.pass);
        public string Message => pass ? "All three vehicles stopped in their STOP boxes." : FirstFailure.firstFailure;
    }

    public abstract class QuestionRule
    {
        public abstract VehicleResult Evaluate(int lane, LaneData data, IList<GraphPoint> graph, CourseConfig course);
        public static QuestionRule For(int question) =>
            question == 0 ? new DemoRule() : question == 3 ? (QuestionRule)new DesignRule() : new ShapeMatchRule(question);
    }

    /// <summary>Demo: cannot fail. Reports the stop so the learner sees: same graph, same 64 m.</summary>
    public sealed class DemoRule : QuestionRule
    {
        public override VehicleResult Evaluate(int lane, LaneData data, IList<GraphPoint> graph, CourseConfig course)
        {
            var r = QuestionRules.Common(lane, data, graph, course);
            r.pass = true; r.firstFailure = "";
            return r;
        }
    }

    /// <summary>Q1 (shape) and Q2 (slope): v(t) must match the story within 0.25 m/s every 0.25 s; end at 0; stop in the box.</summary>
    public sealed class ShapeMatchRule : QuestionRule
    {
        readonly int question;
        public ShapeMatchRule(int q) { question = q; }
        public override VehicleResult Evaluate(int lane, LaneData data, IList<GraphPoint> graph, CourseConfig course)
        {
            var r = QuestionRules.Common(lane, data, graph, course);
            string name = r.vehicle;
            var fails = new List<string>();
            if (graph.Count < 2) fails.Add(name + ": add at least one more point.");

            double end = MotionMath.Duration(graph), storyEnd = MotionMath.Duration(data.solution);
            string mismatch = null;
            int steps = (int)Math.Round(Math.Max(end, storyEnd) / course.sampleStep);
            for (int k = 0; k <= steps; k++)
            {
                double t = k * course.sampleStep;
                double actual = t > end ? MotionMath.Speed(graph, end) : MotionMath.Speed(graph, t);
                double expected = t > storyEnd ? 0 : MotionMath.Speed(data.solution, t);
                if (Math.Abs(actual - expected) > course.speedTolerance + 1e-9)
                {
                    r.markerTime = t;
                    mismatch = string.Format(CultureInfo.InvariantCulture, "At t = {0:0.##} s the story says {1:0.##} m/s; your {2} graph shows {3:0.##} m/s.", t, expected, name, actual);
                    if (question == 2) mismatch = QuestionRules.SlopeHint(name, data.solution, graph) ?? mismatch;
                    break;
                }
            }
            r.checklist.Insert(0, new CheckItem(mismatch == null && graph.Count >= 2, question == 1 ? "matches the speed story" : "matches the acceleration story"));
            if (mismatch != null) fails.Add(mismatch);
            if (r.endsMoving) fails.Add(name + " is still moving at the end. Finish the graph at 0 m/s.");
            if (r.offCourse) fails.Add(QuestionRules.OffCourse(r.distance));
            else if (Math.Abs(r.stopError) > course.stopTolerance + 1e-9) fails.Add(QuestionRules.StopMessage(name, r.stopError));
            r.pass = fails.Count == 0; r.firstFailure = r.pass ? "" : fails[0];
            return r;
        }
    }

    /// <summary>Q3: design any graph that stops in the box, in time, under the speed limit, at rest, plus bike gate and slow zone.</summary>
    public sealed class DesignRule : QuestionRule
    {
        public override VehicleResult Evaluate(int lane, LaneData data, IList<GraphPoint> graph, CourseConfig course)
        {
            var r = QuestionRules.Common(lane, data, graph, course);
            string name = r.vehicle;
            var fails = new List<string>();
            var ci = CultureInfo.InvariantCulture;
            if (graph.Count < 2) fails.Add(name + ": add at least one more point.");

            double end = MotionMath.Duration(graph);
            bool inTime = end <= data.timeLimit + 1e-9;
            r.checklist.Add(new CheckItem(inTime && graph.Count >= 2, string.Format(ci, "finish by {0:0.#} s", data.timeLimit)));
            double above = MotionMath.FirstTimeAbove(graph, data.speedLimit);
            bool underLimit = double.IsNaN(above);
            r.checklist.Add(new CheckItem(underLimit, string.Format(ci, "never above {0:0.#} m/s", data.speedLimit)));

            string gateMsg = null, zoneMsg = null;
            if (data.HasGate)
            {
                double tg = MotionMath.TimeAtDistance(graph, data.gateDistance);
                double vg = double.IsNaN(tg) ? 0 : MotionMath.Speed(graph, tg);
                bool gateOk = !double.IsNaN(tg) && vg >= data.gateMin - 1e-9;
                r.checklist.Add(new CheckItem(gateOk, string.Format(ci, "speed gate {0:0.#} m at ≥ {1:0.#} m/s", data.gateDistance, data.gateMin)));
                if (!gateOk)
                {
                    gateMsg = double.IsNaN(tg) ? string.Format(ci, "{0} never reaches the speed gate at {1:0.#} m. Add area.", name, data.gateDistance)
                                               : string.Format(ci, "Speed gate: {0} crosses at {1:0.#} m/s; it needs ≥ {2:0.#} m/s. Speed up earlier.", name, vg, data.gateMin);
                    if (!double.IsNaN(tg)) r.markerTime = tg;
                }
                double at;
                double vz = MotionMath.MaxSpeedInDistanceRange(graph, data.zoneStart, data.zoneEnd, out at);
                bool zoneOk = !double.IsPositiveInfinity(vz) && vz <= data.zoneMax + 1e-9;
                r.checklist.Add(new CheckItem(zoneOk, string.Format(ci, "slow zone {0:0.#}–{1:0.#} m at ≤ {2:0.#} m/s", data.zoneStart, data.zoneEnd, data.zoneMax)));
                if (!zoneOk)
                {
                    zoneMsg = double.IsPositiveInfinity(vz) ? string.Format(ci, "{0} never reaches the slow zone. Add area.", name)
                                                            : string.Format(ci, "Slow Zone: {0:0.0} m/s at {1:0} m — brake earlier.", vz, at);
                    if (r.markerTime < 0 && !double.IsPositiveInfinity(vz)) r.markerTime = MotionMath.TimeAtDistance(graph, at);
                }
            }

            if (r.endsMoving) fails.Add(name + " is still moving at the end. Finish the graph at 0 m/s.");
            if (!underLimit) { fails.Add(string.Format(ci, "{0} broke the {1:0.#} m/s limit at {2:0.#} s.", name, data.speedLimit, above)); if (r.markerTime < 0) r.markerTime = above; }
            if (gateMsg != null) fails.Add(gateMsg);
            if (zoneMsg != null) fails.Add(zoneMsg);
            if (r.offCourse) fails.Add(QuestionRules.OffCourse(r.distance));
            else if (Math.Abs(r.stopError) > course.stopTolerance + 1e-9) fails.Add(QuestionRules.StopMessage(name, r.stopError));
            if (!inTime) fails.Add(string.Format(ci, "{0} finishes at {1:0.#} s; the limit is {2:0.#} s.", name, end, data.timeLimit));
            r.pass = fails.Count == 0; r.firstFailure = r.pass ? "" : fails[0];
            return r;
        }
    }

    public static class QuestionRules
    {
        static readonly CultureInfo ci = CultureInfo.InvariantCulture;

        internal static VehicleResult Common(int lane, LaneData data, IList<GraphPoint> graph, CourseConfig course)
        {
            var r = new VehicleResult { lane = lane, vehicle = data.vehicle.ToUpperInvariant() };
            r.distance = MotionMath.Distance(graph);
            r.stopError = r.distance - data.stop;
            r.endsMoving = MotionMath.EndsMoving(graph);
            r.offCourse = r.distance > course.offCourse + 1e-9;
            r.checklist.Add(new CheckItem(Math.Abs(r.stopError) <= course.stopTolerance + 1e-9 && !r.offCourse,
                string.Format(ci, "stop at {0:0.#} m (now {1:0.#} m)", data.stop, r.distance)));
            r.checklist.Add(new CheckItem(!r.endsMoving && graph.Count >= 2, "end at 0 m/s"));
            return r;
        }

        public static string StopMessage(string name, double error) =>
            error < 0 ? string.Format(ci, "{0} stopped short by {1:0.#} m — add {1:0.#} m of area.", name, -error)
                      : string.Format(ci, "{0} passed STOP by {1:0.#} m — remove {1:0.#} m of area.", name, error);

        public static string OffCourse(double area) => string.Format(ci, "Off the course! Your graph area is {0:0.#} m.", area);

        /// <summary>Q2 feedback that names the slope and shows dt = dv / a.</summary>
        public static string SlopeHint(string name, IList<GraphPoint> story, IList<GraphPoint> graph)
        {
            if (story.Count < 2 || graph.Count < 2) return null;
            double storyUp = MotionMath.SegmentSlope(story[0], story[1]);
            double top = story[1].v;
            double yourUp = MotionMath.SegmentSlope(graph[0], graph[1]);
            if (Math.Abs(yourUp - storyUp) > 1e-6)
                return string.Format(ci, "{0}: your speed-up slope is {1:0.##} m/s²; the story says {2:0.##}. Time to reach {3:0.##} m/s = {3:0.##} ÷ {2:0.##} = {4:0.##} s.", name, yourUp, storyUp, top, top / storyUp);
            double storyDown = -MotionMath.SegmentSlope(story[story.Count - 2], story[story.Count - 1]);
            double yourDown = -MotionMath.SegmentSlope(graph[graph.Count - 2], graph[graph.Count - 1]);
            if (Math.Abs(yourDown - storyDown) > 1e-6 && storyDown > 0)
            {
                double from = story[story.Count - 2].v;
                return string.Format(ci, "{0}: your braking slope is {1:0.##} m/s²; the story says {2:0.##}. Time to stop from {3:0.##} m/s = {3:0.##} ÷ {2:0.##} = {4:0.##} s.", name, yourDown, storyDown, from, from / storyDown);
            }
            return null;
        }

        public static VehicleResult Evaluate(int question, int lane, LaneData data, IList<GraphPoint> graph, CourseConfig course) =>
            QuestionRule.For(question).Evaluate(lane, data, graph, course);

        /// <summary>Evaluate all three vehicles. In the Demo every vehicle uses the shared graph.</summary>
        public static RunResult EvaluateAll(VariantData variant, IList<GraphPoint>[] graphs, CourseConfig course)
        {
            var rule = QuestionRule.For(variant.question);
            var result = new RunResult { vehicles = new VehicleResult[3] };
            for (int i = 0; i < 3; i++) result.vehicles[i] = rule.Evaluate(i, variant.lanes[i], graphs[i], course);
            result.pass = Array.TrueForAll(result.vehicles, v => v.pass);
            return result;
        }
    }
}