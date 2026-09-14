using UnityEngine;
namespace ZeroReturn
{
    [DefaultExecutionOrder(100)]
    public class MotionTracker : MonoBehaviour
    {
        public CollectorBot bot;
        public bool recording = true;
        public float PathLength { get; private set; }
        public float TripStartX { get; private set; }
        public float RunStartX { get; private set; }
        public float X => bot.Coordinate;
        public float TripDisplacement => X - TripStartX;
        public float RunDisplacement => X - RunStartX;
        float lastX;
        public void ResetRun() { PathLength = 0; lastX = RunStartX = TripStartX = X; }
        public void BeginTrip() { TripStartX = X; }
        public void Sample()
        {
            float current = X;
            if (recording) PathLength += Mathf.Abs(current - lastX);
            lastX = current;
        }
        void LateUpdate() => Sample();
    }
}
