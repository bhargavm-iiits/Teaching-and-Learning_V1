using UnityEngine;

namespace SpeedTimeGraphLesson.VR
{
    public class VRGraphConnections : MonoBehaviour
    {
        public VRMotionChallenge controller;
        public LineRenderer[] links;
        void LateUpdate()
        {
            for(int lane=0;lane<links.Length;lane++)
            {
                var p=controller.plot.DisplayPoint(0,lane);Vector3 a=controller.plot.rectTransform.TransformPoint(p),b=controller.vehicles[lane].position+Vector3.up;
                for(int i=0;i<links[lane].positionCount;i++) { float t=i/(float)(links[lane].positionCount-1);links[lane].SetPosition(i,Vector3.Lerp(a,b,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.45f); }
            }
        }
    }
}
