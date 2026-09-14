using UnityEngine;

namespace NumberLinePlayground.Flow
{
    /// One teaching approach for a concept — the first attempt ("Approach A") or a fallback
    /// approach shown after a poor quiz result (e.g. "Move the Zero", "Two Observers").
    [CreateAssetMenu(menuName = "Number Line Playground/Approach", fileName = "NewApproach")]
    public class ApproachData : ScriptableObject
    {
        public string approachName;
        [TextArea(2, 4)] public string introText;
        public LevelData[] levels;
        public QuestionSet quiz;
    }
}
