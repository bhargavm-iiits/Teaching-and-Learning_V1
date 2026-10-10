using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// The one persistent arena: three lanes, Car / Train / Motorcycle, their ghosts, START, STOP gantries,
    /// the bike speed gate and slow zone. Applying a question only changes DATA (targets, gate positions);
    /// vehicles are never destroyed, re-created or hidden.
    /// </summary>
    public class TripleVehicleArena : MonoBehaviour
    {
        public VehicleMover[] vehicles = new VehicleMover[3];          // 0 CAR (z 16), 1 TRAIN (z 8), 2 BIKE (z 0)
        public GhostPreview[] ghosts = new GhostPreview[3];
        public DistanceRibbon[] ribbons = new DistanceRibbon[3];
        public Transform[] stopGantries = new Transform[3];
        public TMP_Text[] stopLabels = new TMP_Text[3];
        public TMP_Text[] stopLabelsBack = new TMP_Text[3];
        public TMP_Text[] stopResults = new TMP_Text[3];
        public Renderer[] stopStrips = new Renderer[3];
        public Transform speedGate, slowZone; public TMP_Text gateLabel, zoneLabel; public Transform slowZoneFloor;
        public GateZoneEffects effects;
        public float k = .5f;
        public float[] laneZ = { 16, 8, 0 };
        static readonly CultureInfo ci = CultureInfo.InvariantCulture;

        public void ApplyVariant(VariantData v, CourseConfig course)
        {
            k = (float)course.k;
            for (int i = 0; i < 3; i++)
            {
                var lane = v.lanes[i];
                if (vehicles[i]) { vehicles[i].k = k; vehicles[i].offCourseMetres = (float)course.offCourse; vehicles[i].gameObject.SetActive(true); }
                if (stopGantries[i])
                {
                    stopGantries[i].gameObject.SetActive(true);
                    var p = stopGantries[i].localPosition; p.x = (float)(k * lane.stop); stopGantries[i].localPosition = p;
                }
                if (stopLabels[i]) stopLabels[i].text = string.Format(ci, "{0:0.#} m", lane.stop);
                if (stopLabelsBack[i]) stopLabelsBack[i].text = string.Format(ci, "{0:0.#} m", lane.stop);
                if (stopResults[i]) stopResults[i].text = "";
                if (ghosts[i]) ghosts[i].SetK(k);
                if (ribbons[i]) ribbons[i].k = k;
            }
            var bike = v.lanes[2];
            bool q3 = v.question == 3 && bike.HasGate;
            if (speedGate) { speedGate.gameObject.SetActive(q3); if (q3) { var p = speedGate.localPosition; p.x = (float)(k * bike.gateDistance); speedGate.localPosition = p; } }
            if (gateLabel) gateLabel.text = q3 ? string.Format(ci, "SPEED GATE\n≥ {0:0.#} m/s", bike.gateMin) : "";
            if (slowZone)
            {
                slowZone.gameObject.SetActive(q3);
                if (q3)
                {
                    var p = slowZone.localPosition; p.x = (float)(k * bike.zoneStart); slowZone.localPosition = p;
                    if (slowZoneFloor) { var s = slowZoneFloor.localScale; s.x = (float)(k * (bike.zoneEnd - bike.zoneStart)); slowZoneFloor.localScale = s; slowZoneFloor.localPosition = new Vector3(s.x * .5f, slowZoneFloor.localPosition.y, slowZoneFloor.localPosition.z); }
                    if (zoneLabel) { zoneLabel.text = string.Format(ci, "SLOW ZONE\n≤ {0:0.#} m/s", bike.zoneMax); var lp = zoneLabel.transform.localPosition; lp.x = (float)(k * (bike.zoneEnd - bike.zoneStart) * .5f); zoneLabel.transform.localPosition = lp; }
                }
            }
            if (effects) effects.ResetColours();
            for (int i = 0; i < 3; i++) if (vehicles[i]) vehicles[i].SetMotion(new[] { new GraphPoint(0, 0) }, 0);
        }

        /// <summary>Final check: lanes stay, targets hide (PROVE IT drives the real vehicles).</summary>
        public void ShowFinalCheck()
        {
            for (int i = 0; i < 3; i++)
            {
                if (stopGantries[i]) stopGantries[i].gameObject.SetActive(false);
                if (ghosts[i]) ghosts[i].Hide(); if (ribbons[i]) ribbons[i].Hide();
                if (vehicles[i]) vehicles[i].SetMotion(new[] { new GraphPoint(0, 0) }, 0);
            }
            if (speedGate) speedGate.gameObject.SetActive(false); if (slowZone) slowZone.gameObject.SetActive(false);
        }

        /// <summary>Place every vehicle at playback time t on its own graph (Demo: the shared graph).</summary>
        public void SetTime(GameFlowManager game, float t, bool running)
        {
            for (int i = 0; i < 3; i++) vehicles[i].SetMotion(game.graph.Graph(i).Points, t, running);
            if (effects) effects.Track(game, t);
        }

        public void Refresh(GameFlowManager game)
        {
            if (game.part == Part.FinalCheck || game.part == Part.Result || game.part == Part.TopicComplete) return;
            bool edit = game.mode == Mode.Edit;
            for (int i = 0; i < 3; i++) if (vehicles[i].speedTag) vehicles[i].speedTag.gameObject.SetActive(!edit || game.proof.running);
            for (int i = 0; i < 3; i++)
            {
                var pts = game.graph.Graph(i).Points;
                if (edit && !game.proof.running) vehicles[i].SetMotion(pts, 0);
                if (ghosts[i]) ghosts[i].Refresh(pts, edit ? game.graph.HoverTime(i) : -1, edit && !game.proof.running);
                if (ribbons[i]) ribbons[i].Refresh(pts, edit || game.mode == Mode.Review || game.mode == Mode.Complete);
                // REVIEW: show each final stop against its STOP box.
                if (stopResults[i])
                {
                    if (!edit && game.mode != Mode.Run && game.lastRun != null && game.Question > 0)
                    {
                        var r = game.lastRun.vehicles[i];
                        bool inside = Math.Abs(r.stopError) <= game.Course.stopTolerance + 1e-9 && !r.endsMoving;
                        stopResults[i].text = inside ? "✓ IN THE BOX" : r.endsMoving ? "✗ STILL MOVING" : string.Format(ci, "✗ {0:+0.0;−0.0} m {1}", r.stopError, r.stopError > 0 ? "too far" : "short");
                        stopResults[i].color = inside ? new Color(.3f, 1f, .45f) : new Color(1f, .35f, .3f);
                    }
                    else stopResults[i].text = "";
                }
            }
            if (effects && edit) effects.ResetColours();
        }
    }
}