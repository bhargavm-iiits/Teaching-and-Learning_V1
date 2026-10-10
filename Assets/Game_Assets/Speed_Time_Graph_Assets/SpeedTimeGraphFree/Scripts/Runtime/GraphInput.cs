using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// All graph input goes through UI pointer events on the GraphPanel. The pointer position is found
    /// with a ray-plane hit: TrackedDeviceEventData.rayPoints for XR rays (and the XR Device Simulator),
    /// the event camera for the mouse. One code path for every device.
    /// Left click / trigger: add (snapped) or select and drag. Right click: delete the point under the pointer.
    /// In REVIEW, press and drag scrubs time for all three vehicles.
    /// </summary>
    public class GraphInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IInitializePotentialDragHandler, IPointerMoveHandler, IPointerExitHandler, IPointerEnterHandler
    {
        public GraphPanel panel;
        public GraphController controller;
        public int lane;
        int pointerId = int.MinValue; bool dragging, scrubbing;
        Vector2 lastSnap = new Vector2(-1, -1);

        public bool LocalPoint(PointerEventData e, out Vector2 local)
        {
            local = default;
            var plane = new Plane(panel.transform.forward, panel.transform.position);
            if (e is TrackedDeviceEventData xr && xr.rayPoints != null && xr.rayPoints.Count > 1)
            {
                for (int i = 1; i < xr.rayPoints.Count; i++)
                {
                    Vector3 a = xr.rayPoints[i - 1], b = xr.rayPoints[i]; var ray = new Ray(a, b - a);
                    if (plane.Raycast(ray, out float d) && d <= (b - a).magnitude + .001f) { local = panel.transform.InverseTransformPoint(ray.GetPoint(d)); return true; }
                }
                return false;
            }
            var cam = e.pressEventCamera ? e.pressEventCamera : e.enterEventCamera ? e.enterEventCamera : Camera.main;
            if (!cam) return false;
            var mouseRay = cam.ScreenPointToRay(e.position);
            if (!plane.Raycast(mouseRay, out float len)) return false;
            local = panel.transform.InverseTransformPoint(mouseRay.GetPoint(len));
            return true;
        }

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;
        public void OnPointerEnter(PointerEventData e) { if (LocalPoint(e, out var l)) controller.Hover(lane, l); }
        public void OnPointerExit(PointerEventData e) { if (!dragging) controller.HoverEnd(); }
        public void OnPointerMove(PointerEventData e) { if (!dragging && !scrubbing && LocalPoint(e, out var l)) controller.Hover(lane, l); }

        public void OnPointerDown(PointerEventData e)
        {
            if (!LocalPoint(e, out var local)) return;
            var game = controller.game;
            if (controller.CanScrub)
            {
                scrubbing = true; pointerId = e.pointerId; game.playback.Scrub(panel.ToValue(local).x); return;
            }
            if (!controller.Editable) return;
            controller.selectedLane = lane; var model = controller.Graph(lane);
            int hit = controller.HitPoint(lane, local);
            if (e.button == PointerEventData.InputButton.Right)
            {
                if (hit >= 0) { model.Selected = hit; controller.DeleteSelected(); }
                return;
            }
            var value = panel.ToValue(local);
            if (hit >= 0) { model.Selected = hit; model.BeginMove(); }
            else
            {
                if (!panel.Inside(local, 12)) return;
                hit = model.Add(value.x, value.y);
                if (hit < 0) { controller.Toast("Max 10 points. Delete one first."); return; }
            }
            dragging = true; pointerId = e.pointerId; lastSnap = new Vector2((float)model.Points[hit].t, (float)model.Points[hit].v);
            game.sounds.Tick(); controller.Edited(lane, local);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId || !LocalPoint(e, out var local)) return;
            if (scrubbing) { controller.game.playback.Scrub(panel.ToValue(local).x); return; }
            if (!dragging || !controller.Editable) return;
            var model = controller.Graph(lane); var value = panel.ToValue(local);
            model.Move(model.Selected, value.x, value.y);
            if (model.Selected >= 0)
            {
                var p = model.Points[model.Selected]; var snap = new Vector2((float)p.t, (float)p.v);
                if (snap != lastSnap) { lastSnap = snap; controller.game.sounds.Tick(); }
            }
            controller.Edited(lane, local);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            dragging = false; scrubbing = false; pointerId = int.MinValue; controller.DragEnded();
        }
    }
}