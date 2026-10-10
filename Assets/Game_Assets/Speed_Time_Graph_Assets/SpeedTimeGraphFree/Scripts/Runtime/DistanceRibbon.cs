using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>Glowing cyan ribbon on the lane from START to the predicted stop, with "t = 4 s" ticks at each point.</summary>
    public class DistanceRibbon : MonoBehaviour
    {
        public LineRenderer ribbon;
        public Transform[] ticks;
        public TMP_Text[] tickLabels;
        public float k = .5f, offCourseMetres = 125, y = .07f;
        public void Hide() { if (ribbon) ribbon.enabled = false; if (ticks != null) foreach (var t in ticks) t.gameObject.SetActive(false); }
        public void Refresh(IList<GraphPoint> p, bool visible)
        {
            bool show = visible && p.Count >= 2 && MotionMath.Distance(p) > .01;
            ribbon.enabled = show;
            float end = k * (float)System.Math.Min(MotionMath.Distance(p), offCourseMetres);
            ribbon.SetPosition(0, new Vector3(0, y, 0)); ribbon.SetPosition(1, new Vector3(end, y, 0));
            for (int i = 0; i < ticks.Length; i++)
            {
                bool on = show && i < p.Count && i > 0;
                ticks[i].gameObject.SetActive(on);
                if (!on) continue;
                ticks[i].localPosition = new Vector3(k * (float)System.Math.Min(MotionMath.Distance(p, p[i].t), offCourseMetres), 0, 0);
                tickLabels[i].text = string.Format(CultureInfo.InvariantCulture, "t = {0:0.#} s", p[i].t);
            }
        }
    }
}