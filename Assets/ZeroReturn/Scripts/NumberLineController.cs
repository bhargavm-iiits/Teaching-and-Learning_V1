using UnityEngine;
namespace ZeroReturn
{
    public class NumberLineController : MonoBehaviour
    {
        public float WorldScale = 1;
        public Vector3 ToWorld(float coordinate) => transform.TransformPoint(new Vector3(coordinate * WorldScale, .38f, 0));
        public float ToCoordinate(Vector3 position) => transform.InverseTransformPoint(position).x / WorldScale;
    }
}
