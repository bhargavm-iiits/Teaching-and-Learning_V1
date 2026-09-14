using UnityEngine;
namespace ZeroReturn
{
    public class CollectorBot : MonoBehaviour
    {
        public NumberLineController rail;
        public float speed = 6;
        public bool paused;
        public bool IsMoving { get; private set; }
        public float Coordinate => rail.ToCoordinate(transform.position);
        float destination;
        public void MoveTo(float coordinate) { destination = Mathf.Clamp(coordinate, -12, 12); IsMoving = true; }
        public void ResetAtDock() { IsMoving = false; transform.position = rail.ToWorld(0); }
        void Update()
        {
            if (paused || !IsMoving) return;
            float next = Mathf.MoveTowards(Coordinate, destination, speed * Time.deltaTime);
            transform.position = rail.ToWorld(next);
            IsMoving = !Mathf.Approximately(next, destination);
        }
    }
}
