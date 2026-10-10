using System.Globalization;
using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// PROVE IT: replays the crossing question on the real lanes. At the crossing time it pauses: equal speed,
    /// different places (12.5 m vs 25 m). It then continues until the car catches up (10 s; B: 12 s).
    /// </summary>
    public class ProveItReplay : MonoBehaviour
    {
        public GameFlowManager game;
        public bool running; public float time;
        FinalCheckItem item; float hold; bool paused;
        public void Begin(FinalCheckItem crossing)
        {
            item = crossing; time = 0; hold = 0; paused = false; running = true;
            Debug.Log("[STGF] PROVE IT " + crossing.id);
        }
        void Update()
        {
            if (!running) return;
            float meet = (float)MotionMath.CrossingTime(item.a, item.b), end = (float)MotionMath.Duration(item.a);
            if (hold > 0) hold -= Time.deltaTime; else time += Time.deltaTime * 1.5f;
            if (!paused && time >= meet) { time = meet; paused = true; hold = 3.5f; }
            time = Mathf.Min(time, end);
            Apply();
            if (time >= end && hold <= 0) { if (hold > -2.5f) hold -= Time.deltaTime; else Finish(); }
        }
        public void Apply()
        {
            var arena = game.arena; var ci = CultureInfo.InvariantCulture;
            for (int i = 0; i < 3; i++)
            {
                var g = i == item.aLane ? item.a : i == item.bLane ? item.b : new[] { new GraphPoint(0, 0) };
                arena.vehicles[i].SetMotion(g, time, true);
            }
            double sa = MotionMath.Distance(item.a, time), sb = MotionMath.Distance(item.b, time);
            game.message = string.Format(ci, "t = {0:0.0} s · CAR {1:0.0} m/s, {2:0.0} m · {3} {4:0.0} m/s, {5:0.0} m", time, MotionMath.Speed(item.a, time), sa, item.bVehicle, MotionMath.Speed(item.b, time), sb)
                + (paused && hold > 0 ? "\nSAME SPEED — but NOT the same place!" : time >= MotionMath.Duration(item.a) - .01f ? "\nOnly now (equal areas) is the car level with the " + item.bVehicle.ToLowerInvariant() + "." : "");
            game.Refresh();
        }
        void Finish()
        {
            running = false;
            for (int i = 0; i < 3; i++) game.arena.vehicles[i].SetMotion(new[] { new GraphPoint(0, 0) }, 0);
            game.message = item.explanation; game.finalCheck.Restore(); game.Refresh();
        }
    }
}