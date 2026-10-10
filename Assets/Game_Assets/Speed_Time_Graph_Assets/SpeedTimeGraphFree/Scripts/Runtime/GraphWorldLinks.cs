using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Curved glowing beams from the CAR / TRAIN / BIKE tabs of the Graph Controller to their vehicles
    /// (the reference graph-to-world glow). During RUN a pulse travels along each beam.
    /// </summary>
    public class GraphWorldLinks : MonoBehaviour
    {
        public GameFlowManager game;
        public LineRenderer[] beams = new LineRenderer[3];
        public Transform[] anchors = new Transform[3];   // tab buttons
        public Transform demoAnchor;                      // right edge of the graph in the Demo
        public Transform[] pulses = new Transform[3];
        public Camera eye;
        const int Segments = 32;

        void LateUpdate() { UpdateBeams(); }

        public void UpdateBeams()
        {
            // Null-safe: the beams are optional (removed from the current build); a stale scene must never throw.
            if (!game || game.arena == null || game.graph == null || beams == null || anchors == null) { enabled = false; return; }
            bool show = game.IsGraphPart || game.proof.running;
            for (int i = 0; i < 3; i++)
            {
                var beam = beams[i]; if (!beam) continue;
                if (i >= anchors.Length || !anchors[i] || !game.arena.vehicles[i]) { beam.enabled = false; if (i < pulses.Length && pulses[i]) pulses[i].gameObject.SetActive(false); continue; }
                beam.enabled = show;
                if (!show) { if (pulses[i]) pulses[i].gameObject.SetActive(false); continue; }
                var from = anchors[i];
                Vector3 a = from.position, b = game.arena.vehicles[i].transform.position + Vector3.up * 2.2f + Vector3.left * 1.5f;
                Vector3 mid = Vector3.Lerp(a, b, .45f) + Vector3.up * (4 + i * 1.5f);
                beam.positionCount = Segments;
                for (int j = 0; j < Segments; j++) { float t = j / (Segments - 1f); beam.SetPosition(j, Bezier(a, mid, b, t)); }
                bool selected = game.graph.SharedMode || game.graph.tab == i || game.graph.tab == GraphController.AllTab;
                var c = game.graph.vehicleColors[i];
                beam.startColor = new Color(.16f, .86f, 1f, selected ? .55f : .12f); beam.endColor = new Color(c.r, c.g, c.b, selected ? .6f : .12f);
                if (pulses[i])
                {
                    bool run = game.mode == Mode.Run && !game.playback.paused || game.proof.running;
                    pulses[i].gameObject.SetActive(run);
                    if (run)
                    {
                        float t = Mathf.Repeat(Time.time * .7f + i * .33f, 1);
                        pulses[i].position = Bezier(a, mid, b, t);
                        if (eye) pulses[i].localScale = Vector3.one * Mathf.Max(.02f, Vector3.Distance(eye.transform.position, pulses[i].position) * .012f);
                    }
                }
            }
        }
        static Vector3 Bezier(Vector3 a, Vector3 m, Vector3 b, float t) => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * m + t * t * b;
    }
}