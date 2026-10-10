using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>The speed gate, the slow zone and the STOP strips flash green or red as each vehicle passes.</summary>
    public class GateZoneEffects : MonoBehaviour
    {
        public TripleVehicleArena arena;
        public Renderer[] gateRenderers, zoneRenderers;
        public Material good, bad, gateIdle, zoneIdle, stripIdle;
        public void ResetColours()
        {
            Set(gateRenderers, gateIdle); Set(zoneRenderers, zoneIdle);
            foreach (var s in arena.stopStrips) if (s) s.sharedMaterial = stripIdle;
        }
        static void Set(Renderer[] rs, Material m) { if (rs != null && m) foreach (var r in rs) if (r) r.sharedMaterial = m; }

        public void Track(GameFlowManager game, float t)
        {
            bool flash = Mathf.Repeat(Time.time * 3, 1) < .65f || !Application.isPlaying;
            for (int i = 0; i < 3; i++)
            {
                var pts = game.graph.Graph(i).Points;
                if (t < MotionMath.Duration(pts)) { if (arena.stopStrips[i]) arena.stopStrips[i].sharedMaterial = stripIdle; continue; }
                double err = MotionMath.Distance(pts) - game.variant.lanes[i].stop;
                bool ok = System.Math.Abs(err) <= game.Course.stopTolerance + 1e-9 && !MotionMath.EndsMoving(pts);
                if (arena.stopStrips[i]) arena.stopStrips[i].sharedMaterial = flash ? (ok ? good : bad) : stripIdle;
            }
            var lane = game.variant.lanes[2];
            if (game.Question != 3 || !lane.HasGate) return;
            var bike = game.graph.Graph(2).Points; double s = MotionMath.Distance(bike, t);
            if (s >= lane.gateDistance)
            {
                double tg = MotionMath.TimeAtDistance(bike, lane.gateDistance);
                Set(gateRenderers, MotionMath.Speed(bike, tg) >= lane.gateMin - 1e-9 ? good : bad);
            }
            if (s >= lane.zoneStart)
            {
                double at; double vmax = MotionMath.MaxSpeedInDistanceRange(bike, lane.zoneStart, System.Math.Min(s, lane.zoneEnd), out at);
                Set(zoneRenderers, vmax <= lane.zoneMax + 1e-9 ? good : bad);
            }
        }
    }
}