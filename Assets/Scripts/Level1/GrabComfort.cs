using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace NumberLinePlayground.Level1
{
    /// Makes carrying a sphere possible and comfortable, with the XR Device Simulator as well as with controllers.
    ///
    /// By default a grab lasts only while the button is held. With the simulator that means holding Space (to steer the
    /// hand) and G together for the whole walk, and WASD then moves the hand instead of the body. So instead:
    ///  - press the grip (G in the simulator) or the trigger (left mouse button) once to pick a sphere up,
    ///    and once more to put it down; the sphere stays in your hand while you walk;
    ///  - the trigger works as well as the grip, so clicking a sphere with a ray does what you would expect.
    public class GrabComfort : MonoBehaviour
    {
        void Start()
        {
            foreach (var hand in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                hand.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.Toggle;
                if (hand.selectInput.bypass == null)
                    hand.selectInput.bypass = new GripOrTrigger(hand.selectInput, hand.activateInput);
            }
        }

        /// Reads "select" as pressed when either the grip (the original select input) or the trigger is pressed.
        sealed class GripOrTrigger : IXRInputButtonReader
        {
            readonly XRInputButtonReader grip;
            readonly XRInputButtonReader trigger;

            public GripOrTrigger(XRInputButtonReader grip, XRInputButtonReader trigger)
            {
                this.grip = grip;
                this.trigger = trigger;
            }

            // While this object is being asked, the grip reader hands back its own raw value instead of asking again.
            public bool ReadIsPerformed() => grip.ReadIsPerformed() || trigger.ReadIsPerformed();
            public bool ReadWasPerformedThisFrame() => grip.ReadWasPerformedThisFrame() || trigger.ReadWasPerformedThisFrame();
            public bool ReadWasCompletedThisFrame() => grip.ReadWasCompletedThisFrame() || trigger.ReadWasCompletedThisFrame();
            public float ReadValue() => Mathf.Max(grip.ReadValue(), trigger.ReadValue());

            public bool TryReadValue(out float value)
            {
                value = ReadValue();
                return value > 0f || ReadIsPerformed();
            }
        }
    }
}
