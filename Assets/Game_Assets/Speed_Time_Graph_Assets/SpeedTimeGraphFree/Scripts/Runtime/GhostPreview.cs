using System.Collections.Generic;
using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Translucent cyan proxy hull at the predicted stop (k * area), updated every drag frame, plus a second
    /// fainter ghost at the hovered time. Proxies are low-poly hulls, never copies of the heavy meshes.
    /// </summary>
    public class GhostPreview : MonoBehaviour
    {
        public Transform stopGhost, timeGhost;
        public float k = .5f, offCourseMetres = 125;
        public void SetK(float scale) { k = scale; }
        public void Hide() { if (stopGhost) stopGhost.gameObject.SetActive(false); if (timeGhost) timeGhost.gameObject.SetActive(false); }
        public void Refresh(IList<GraphPoint> p, float hoverTime, bool visible)
        {
            bool show = visible && p.Count >= 2;
            stopGhost.gameObject.SetActive(show && MotionMath.Distance(p) > .01);
            if (show) Place(stopGhost, MotionMath.Distance(p));
            bool hover = show && hoverTime >= 0;
            timeGhost.gameObject.SetActive(hover);
            if (hover) Place(timeGhost, MotionMath.Distance(p, hoverTime));
        }
        void Place(Transform t, double metres) { var pos = t.localPosition; pos.x = k * (float)System.Math.Min(metres, offCourseMetres); t.localPosition = pos; }
    }
}