using UnityEngine;

namespace NumberLinePlayground.Flow
{
    /// One level within an approach (e.g. Concept 1 / Approach A / Level 1 "Find Your Spot").
    [CreateAssetMenu(menuName = "Number Line Playground/Level", fileName = "NewLevel")]
    public class LevelData : ScriptableObject
    {
        public string levelName;
        [TextArea(2, 4)] public string startPopupText;
        [TextArea(2, 4)] public string learnedSummary;

        [Tooltip("The checkpoint question shown right after this level completes.")]
        public QuestionData checkpointQuestion;

        [Tooltip("Name of the scene or sub-area this level plays in.")]
        public string sceneOrAreaName;
    }
}
