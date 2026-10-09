using TMPro;
using UnityEngine;

namespace DistanceTimeGraph
{
    /// While the learner edits the graph, a transparent cyan car shows where the selected point would put the car. It
    /// updates every frame while a point is dragged, is linked to the point by a thin cyan line and carries the
    /// point's predicted time and distance. The real car is never moved by editing.
    public class CarPreviewController : MonoBehaviour
    {
        public GraphEditor graph;
        public CarMotionController previewCar;
        public LineRenderer link;
        public TextMeshPro label;

        public bool Active { get; set; } = true;
        public bool Visible => previewCar != null && previewCar.gameObject.activeSelf;

        void LateUpdate()
        {
            int sel = graph != null ? graph.Selected : -1;
            bool show = Active && sel > 0 && graph.Editable;
            if (previewCar.gameObject.activeSelf != show) previewCar.gameObject.SetActive(show);
            if (link != null && link.enabled != show) link.enabled = show;
            if (label != null && label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
            if (!show) return;

            var p = graph.Model[sel];
            // The motion that arrives at this point decides the lights: a lower point than the one before means reversing.
            float v = graph.Model.Slope(sel - 1);
            float d = Mathf.Lerp(previewCar.Distance, p.d, 1f - Mathf.Exp(-18f * Time.deltaTime));
            if (Mathf.Abs(d - p.d) < 0.02f) d = p.d;
            previewCar.Place(d, v);

            Vector3 roof = previewCar.Center + Vector3.up * 1.4f;
            if (link != null)
            {
                link.positionCount = 2;
                link.SetPosition(0, graph.PointWorld(sel));
                link.SetPosition(1, roof);
            }
            if (label != null)
            {
                label.text = $"PREVIEW  {p.t} s · {p.d} m";
                label.transform.position = roof + Vector3.up * 0.9f;
                var cam = Camera.main;
                if (cam != null) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position);
            }
        }

        /// Starts the preview car from the real car's place so it glides rather than pops.
        public void SyncFrom(CarMotionController car) => previewCar.Teleport(car.Distance);
    }
}
