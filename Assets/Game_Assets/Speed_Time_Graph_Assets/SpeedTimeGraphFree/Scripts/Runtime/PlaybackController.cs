using System.Linq;
using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>ONE clock drives all three vehicles, the graph cursors, the Q3 area fill and the effects.</summary>
    public class PlaybackController : MonoBehaviour
    {
        public GameFlowManager game;
        public float time, rate = 1;
        public bool paused;
        public TMPro.TMP_Text rateLabel;

        public float Duration => game.graph == null ? 0 : (float)Enumerable.Range(0, 3).Max(i => MotionMath.Duration(game.graph.Graph(i).Points));

        public void Begin() { time = 0; paused = false; Position(true); }
        public void ResetToStart() { time = 0; paused = false; if (game && game.graph && game.variant != null) Position(false); }
        public void TogglePause()
        {
            if (game.mode != Mode.Run) return;
            paused = !paused; game.message = paused ? "PAUSED: read the height, slope and area at the cursor." : "RUN: all three vehicles follow their graphs.";
            Debug.Log("[STGF] " + (paused ? "pause" : "resume")); game.Refresh();
        }
        public void CycleRate() { rate = rate == 1 ? 2 : rate == 2 ? .5f : 1; if (rateLabel) rateLabel.text = rate == .5f ? "0.5×" : rate + "×"; }

        void Update()
        {
            if (!game || game.mode != Mode.Run || paused || game.proof.running) return;
            time += Time.deltaTime * rate;
            float end = Duration + .5f;
            if (time >= end) { time = Duration; Position(false); game.FinishRun(); return; }
            Position(true);
        }

        /// <summary>In REVIEW, dragging the cursor scrubs time for all three vehicles.</summary>
        public void Scrub(float t) { time = Mathf.Clamp(t, 0, Duration); Position(false); }

        public void Position(bool running)
        {
            game.arena.SetTime(game, Mathf.Min(time, Duration), running);
            game.Refresh();
        }
    }
}