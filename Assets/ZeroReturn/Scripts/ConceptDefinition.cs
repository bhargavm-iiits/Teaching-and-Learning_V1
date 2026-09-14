using UnityEngine;
namespace ZeroReturn
{
    [CreateAssetMenu(menuName = "Zero Return/Concept")]
    public class ConceptDefinition : ScriptableObject
    {
        public string id = "position-direction-distance";
        public string title = "Position, Direction & Distance";
        [TextArea] public string explanation = "Position tells you where you are relative to zero. Path length adds every metre travelled. Displacement compares your final position with your starting position.";
        public string syllabus = "NCERT Class 11 Physics - Motion in a Straight Line";
        public LevelTemplate[] levels;
        public QuestionBank questions;
        public ConceptDefinition nextConcept;
    }
}
