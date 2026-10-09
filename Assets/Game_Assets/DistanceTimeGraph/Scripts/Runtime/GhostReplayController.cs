using System.Collections.Generic;
using DistanceTimeGraph.Logic;
using UnityEngine;

namespace DistanceTimeGraph
{
    /// Keeps up to three earlier attempts of the current course visible as transparent ghost cars (blue for a success,
    /// faded orange for a failure) and as faint lines on the graph. Ghosts replay alongside a new run and rest at their
    /// final position between runs. They are cleared when a new course has different distances.
    public class GhostReplayController : MonoBehaviour
    {
        public static readonly Color SuccessBlue = new Color(0.25f, 0.5f, 1f, 0.42f);
        public static readonly Color FailOrange = new Color(1f, 0.55f, 0.15f, 0.32f);
        public static readonly Color PreviewCyan = new Color(0.2f, 0.9f, 1f, 0.4f);

        public CarMotionController[] ghostCars = new CarMotionController[3];
        public GraphEditor graph;

        readonly List<AttemptRecord> shown = new List<AttemptRecord>();
        MaterialPropertyBlock block;

        public void Show(IList<AttemptRecord> attempts)
        {
            shown.Clear();
            int start = Mathf.Max(0, attempts.Count - ghostCars.Length);
            for (int i = start; i < attempts.Count; i++) shown.Add(attempts[i]);
            var lines = new List<(GraphPoint[], Color)>();
            for (int i = 0; i < ghostCars.Length; i++)
            {
                bool on = i < shown.Count;
                ghostCars[i].gameObject.SetActive(on);
                if (!on) continue;
                var color = shown[i].success ? SuccessBlue : FailOrange;
                Tint(ghostCars[i], color);
                lines.Add((shown[i].points, color.WithAlpha(shown[i].success ? 0.7f : 0.55f)));
            }
            if (graph != null) graph.SetGhosts(lines);
            Rest();
        }

        public void Clear() => Show(new List<AttemptRecord>());

        /// Moves every ghost along its own recording.
        public void SetTime(float t)
        {
            for (int i = 0; i < shown.Count; i++)
            {
                var p = shown[i].profile;
                ghostCars[i].Place(p.DistanceAt(t), p.VelocityAt(t));
            }
        }

        /// Ghosts wait where their attempt ended.
        public void Rest()
        {
            for (int i = 0; i < shown.Count; i++)
            {
                var p = shown[i].profile;
                ghostCars[i].Teleport(p.DistanceAt(p.duration));
            }
        }

        void Tint(CarMotionController car, Color c)
        {
            block ??= new MaterialPropertyBlock();
            foreach (var r in car.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.gameObject.name.Contains("Light")) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", c);
                r.SetPropertyBlock(block);
            }
        }
    }
}
