using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace SpeedTimeGraphLesson.VR
{
    public class VRPlayerStation : MonoBehaviour
    {
        public Camera eye;
        public Transform console;
        public TeleportationProvider teleportProvider;
        public Transform[] stations;
        public Transform[] vehicleLabels;
        Vector3 consolePosition;
        Quaternion consoleRotation;
        float yaw,pitch=8;
        public int lastStation;
        void Awake() { consolePosition=console.position;consoleRotation=console.rotation; }
        public void RecenterConsole() { console.position=consolePosition;console.rotation=consoleRotation; }
        public void Visit(int index)
        {
            lastStation=Mathf.Clamp(index,0,stations.Length-1);
            teleportProvider.QueueTeleportRequest(new TeleportRequest { destinationPosition=stations[lastStation].position,destinationRotation=stations[lastStation].rotation,matchOrientation=MatchOrientation.TargetUpAndForward });
            // Bring the physical console to the destination so every station remains interactive.
            Vector3 at=stations[lastStation].position+Vector3.up*1.65f;
            Quaternion look=stations[lastStation].rotation*Quaternion.Euler(0,-24,0);
            consolePosition=at+look*new Vector3(0,-.9f,2.4f);consoleRotation=look;RecenterConsole();
        }
        void LateUpdate()
        {
            bool tracked=XRSettings.isDeviceActive||InputSystem.devices.OfType<XRHMD>().Any(h=>h.isTracked.isPressed);
            if(!tracked)
            {
                if(Mouse.current!=null&&Mouse.current.rightButton.isPressed) { var delta=Mouse.current.delta.ReadValue();yaw+=delta.x*.12f;pitch=Mathf.Clamp(pitch-delta.y*.12f,-60,60); }
                eye.transform.localPosition=Vector3.zero;eye.transform.localRotation=Quaternion.Euler(pitch,yaw,0);
            }
            foreach(var label in vehicleLabels)if(label)label.rotation=Quaternion.LookRotation(label.position-eye.transform.position,Vector3.up);
        }
    }
}
