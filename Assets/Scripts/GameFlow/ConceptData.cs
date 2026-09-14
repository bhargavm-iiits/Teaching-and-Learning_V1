using UnityEngine;

namespace NumberLinePlayground.Flow
{
    /// One of the 9 concept zones in the Physics Park hub (e.g. "Position, Origin & Direction").
    /// approaches[0] is the first attempt; further entries are fallback approaches tried in
    /// order after a poor quiz result (0-1 out of 3 correct).
    [CreateAssetMenu(menuName = "Number Line Playground/Concept", fileName = "NewConcept")]
    public class ConceptData : ScriptableObject
    {
        public string conceptName;
        [TextArea(2, 4)] public string storyHook;
        public string estimatedTime;
        public ApproachData[] approaches;
    }
}
