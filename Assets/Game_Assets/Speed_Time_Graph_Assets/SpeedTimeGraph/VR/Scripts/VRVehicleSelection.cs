using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SpeedTimeGraphLesson.VR
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class VRVehicleSelection : MonoBehaviour
    {
        public VRMotionChallenge controller;
        public int lane;
        void Awake() { GetComponent<XRSimpleInteractable>().selectEntered.AddListener(_=> { controller.SelectVehicle(lane);VRMotionChallenge.Haptic(.12f); }); }
    }
}
