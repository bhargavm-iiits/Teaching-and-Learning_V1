using UnityEngine;

namespace NumberLinePlayground.Flow
{
    /// A set of 3 skill-check questions for one quiz attempt. Keep at least 5 versions of
    /// each skill question authored across different QuestionSets — a student can see up to
    /// 5 sets total (the first attempt, its retest, Approach B, Approach C, and guided mode).
    [CreateAssetMenu(menuName = "Number Line Playground/Question Set", fileName = "NewQuestionSet")]
    public class QuestionSet : ScriptableObject
    {
        public QuestionData[] questions;
    }
}
