using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Desktop / XR Device Simulator controls. Space RUN/PAUSE, R reset, 1/2/3 select the Car/Train/Bike graph,
    /// 4 ALL, Delete (or right-click on a point) deletes, Ctrl+Z undo, F1-F4 teacher jump (Demo/Q1/Q2/Q3),
    /// Enter presses the first popup button. Right-mouse drag looks around when no HMD is tracked.
    /// </summary>
    public class DesktopFallback : MonoBehaviour
    {
        public GameFlowManager game;
        public Camera eye;
        public Transform lookPivot;
        public float pitch = 32, yaw;
        bool looking;

        void Start() { if (lookPivot) { var e = lookPivot.localEulerAngles; } }

        void Update()
        {
            var k = Keyboard.current; var mouse = Mouse.current;
            if (k != null)
            {
                if (k.spaceKey.wasPressedThisFrame) game.Run();
                if (k.rKey.wasPressedThisFrame) game.ResetRun();
                if (k.deleteKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame) game.graph.DeleteSelected();
                if ((k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed) && k.zKey.wasPressedThisFrame) game.graph.Undo();
                if (k.digit1Key.wasPressedThisFrame) game.graph.SelectTab(0);
                if (k.digit2Key.wasPressedThisFrame) game.graph.SelectTab(1);
                if (k.digit3Key.wasPressedThisFrame) game.graph.SelectTab(2);
                if (k.digit4Key.wasPressedThisFrame) game.graph.SelectTab(GraphController.AllTab);
                if (k.f1Key.wasPressedThisFrame) game.TeacherJump(0);
                if (k.f2Key.wasPressedThisFrame) game.TeacherJump(1);
                if (k.f3Key.wasPressedThisFrame) game.TeacherJump(2);
                if (k.f4Key.wasPressedThisFrame) game.TeacherJump(3);
                if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) game.popup.Press(0);
            }
            if (HmdTracked() || mouse == null || !eye) return;
            // Mouse look (right drag) for the plain desktop view; the XR Device Simulator handles its own HMD.
            if (mouse.rightButton.isPressed)
            {
                var d = mouse.delta.ReadValue();
                if (d.sqrMagnitude > 0) looking = true;
                yaw += d.x * .12f; pitch = Mathf.Clamp(pitch - d.y * .12f, -60, 75);
            }
            eye.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
        }

        static bool HmdTracked()
        {
            if (XRSettings.isDeviceActive) return true;
            foreach (var device in InputSystem.devices)
                if (device is UnityEngine.InputSystem.XR.XRHMD hmd && hmd.added && hmd.trackingState.ReadValue() != 0) return true;
            return false;
        }
    }
}