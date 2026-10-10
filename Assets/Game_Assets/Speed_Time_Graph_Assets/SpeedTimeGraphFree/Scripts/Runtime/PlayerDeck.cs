using UnityEngine;
using UnityEngine.InputSystem;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// The viewing deck (no forced movement). XR controller grips: a short grip press deletes the selected
    /// point; holding BOTH grips for 3 s opens teacher mode (Demo / Q1 / Q2 / Q3). Works with real controllers
    /// and the XR Device Simulator through Input System XR bindings.
    /// </summary>
    public class PlayerDeck : MonoBehaviour
    {
        public GameFlowManager game;
        InputAction left, right; float both; bool opened; bool lWas, rWas;

        void OnEnable()
        {
            left = new InputAction("left grip", InputActionType.Button, "<XRController>{LeftHand}/{GripButton}");
            right = new InputAction("right grip", InputActionType.Button, "<XRController>{RightHand}/{GripButton}");
            left.Enable(); right.Enable();
        }
        void OnDisable() { left?.Disable(); right?.Disable(); }

        void Update()
        {
            bool l = left != null && left.IsPressed(), r = right != null && right.IsPressed();
            if (l && r) { both += Time.deltaTime; if (both > 3 && !opened) { opened = true; OpenTeacher(); } }
            else
            {
                both = 0; opened = false;
                if ((l && !lWas) || (r && !rWas)) game.graph.DeleteSelected();
            }
            lWas = l; rWas = r;
        }

        void OpenTeacher()
        {
            Debug.Log("[STGF] teacher mode");
            game.popup.Show("TEACHER MODE", "Jump to a part. (Desktop: F1–F4.)", "DEMO", () => game.TeacherJump(0), "Q1 / Q2", () =>
                game.popup.Show("TEACHER MODE", "Choose the question.", "Q1 SHAPE", () => game.TeacherJump(1), "Q2 SLOPE", () => game.TeacherJump(2), "Q3 AREA", () => game.TeacherJump(3)),
                "Q3", () => game.TeacherJump(3));
        }
    }
}