using UnityEngine;
namespace ZeroReturn
{
    [CreateAssetMenu(menuName = "Zero Return/Level Template")]
    public class LevelTemplate : ScriptableObject
    {
        public int levelIndex;
        public ShapeType shape;
        public Modifier modifier;
        public float[] exampleCoordinates;
        public float exampleSide = 4;
        [TextArea] public string objective;
        [TextArea] public string lesson;
    }
}
