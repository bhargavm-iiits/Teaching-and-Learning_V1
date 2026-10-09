using UnityEngine;

namespace DistanceTimeGraph
{
    /// Turns decoration (wind-tower blades, crystals) slowly around a local axis.
    public class Spinner : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward;
        public float degreesPerSecond = 60f;

        void Update() => transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
