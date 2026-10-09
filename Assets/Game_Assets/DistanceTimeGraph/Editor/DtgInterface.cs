using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DistanceTimeGraph.EditorTools
{
    public class UiRefs
    {
        public Transform cluster;
        public GraphEditor graph;
        public ActivityPanel activity;
        public ControlConsole console;
        public PopupPanel popup;
        public AssessmentManager assessment;
        public RawImage monitorImage;
        public Image liveDot;
        public TextMeshProUGUI monitorMode, monitorDistance;
        public Canvas graphCanvas, monitorCanvas, activityCanvas, consoleCanvas, popupCanvas, assessmentCanvas;
    }

    public static partial class DtgBuilder
    {
        // Where each panel sits, as seen from the resting gaze (yaw right, pitch up, distance), matching the reference:
        // graph on the right, the behind-car monitor above it, buttons under it, the activity sign on the left, popups
        // left of centre so the graph stays visible.
        public static readonly Vector3 GraphAt = new Vector3(21f, -6f, 1.13f);
        public static readonly Vector3 MonitorAt = new Vector3(21.5f, 15.5f, 1.23f);
        public static readonly Vector3 ConsoleAt = new Vector3(19f, -22.5f, 1.08f);
        public static readonly Vector3 ActivityAt = new Vector3(-28f, 0f, 2.0f);
        public static readonly Vector3 PopupAt = new Vector3(-9f, 3f, 1.38f);
        public static readonly Vector3 AssessmentAt = new Vector3(0f, 0f, 1.05f);

        static UiRefs BuildInterface(Transform root, Camera eye, DtgCatalog catalog)
        {
            var ui = new UiRefs();
            var cluster = new GameObject("ControlCluster").transform;
            cluster.SetParent(root, false);
            cluster.position = PlayerFeet;
            ui.cluster = cluster;

            ui.graphCanvas = MakeCanvas(cluster, "GraphPanel", GraphAt, new Vector2(680, 500), eye, false);
            BuildGraphPanel(ui, ui.graphCanvas.transform, catalog);
            ui.monitorCanvas = MakeCanvas(cluster, "RearCameraMonitor", MonitorAt, new Vector2(640, 350), eye, false);
            BuildMonitor(ui, ui.monitorCanvas.transform);
            ui.activityCanvas = MakeCanvas(cluster, "ActivityPanel", ActivityAt, new Vector2(820, 640), eye, true);
            BuildActivity(ui, ui.activityCanvas.transform);
            ui.consoleCanvas = MakeCanvas(cluster, "ControlConsole", ConsoleAt, new Vector2(660, 96), eye, false);
            BuildConsole(ui, ui.consoleCanvas.transform);
            ui.popupCanvas = MakeCanvas(cluster, "Popups", PopupAt, new Vector2(700, 460), eye, false);
            BuildPopup(ui, ui.popupCanvas.transform);
            ui.assessmentCanvas = MakeCanvas(cluster, "FinalCheck", AssessmentAt, new Vector2(1000, 760), eye, false);
            BuildAssessment(ui, ui.assessmentCanvas.transform);

            foreach (var t in cluster.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = UiLayer;
            return ui;
        }

        /// A world-space canvas at (yaw, pitch, distance) from the resting gaze, turned to face the eyes. An upright
        /// canvas (a signboard standing in the world) only turns around the vertical axis.
        static Canvas MakeCanvas(Transform cluster, string name, Vector3 at, Vector2 sizeMm, Camera eye, bool upright)
        {
            var pos = AtAngles(at.x, at.y, at.z);
            var dir = pos - Head;
            if (upright) dir.y = 0f;
            var c = UiKit.WorldCanvas(cluster, name, cluster.InverseTransformPoint(pos), Quaternion.LookRotation(dir, Vector3.up), sizeMm, eye);
            return c;
        }

    }
}
