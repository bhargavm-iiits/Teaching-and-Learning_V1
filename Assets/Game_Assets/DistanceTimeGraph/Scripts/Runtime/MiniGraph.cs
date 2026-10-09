using System.Collections.Generic;
using DistanceTimeGraph.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace DistanceTimeGraph
{
    /// A small read-only picture of an attempt's graph, used on the replay cards.
    public class MiniGraph : MonoBehaviour
    {
        public RectTransform area;
        readonly List<Image> lines = new List<Image>();

        public void Draw(GraphPoint[] points, int maxTime, int maxDistance, Color color)
        {
            int need = points == null ? 0 : points.Length - 1;
            while (lines.Count < need) lines.Add(UiKit.Panel(area, "Line", color, Vector2.zero, new Vector2(2, 2), false));
            float w = area.rect.width, h = area.rect.height;
            for (int i = 0; i < lines.Count; i++)
            {
                bool on = i < need;
                lines[i].gameObject.SetActive(on);
                if (!on) continue;
                Vector2 P(GraphPoint p) => new Vector2(-w * 0.5f + w * p.t / maxTime, -h * 0.5f + h * p.d / maxDistance);
                UiKit.Line(lines[i].rectTransform, P(points[i]), P(points[i + 1]), 4f);
                lines[i].color = color;
            }
        }
    }
}
