using UnityEngine;

namespace NumberLinePlayground.Flow
{
    [System.Serializable]
    public class AnswerOption
    {
        public string label;
        public bool isCorrect;

        [Tooltip("What picking this wrong answer reveals about the student's thinking, e.g. 'ignored direction'.")]
        public string misconception;

        [Tooltip("If this wrong answer is picked, which approach to suggest next (e.g. 'Move the Zero'). Leave blank for none.")]
        public string suggestedApproach;
    }

    /// One quiz or checkpoint question: a prompt, its answer options, and — per wrong
    /// option — what misconception it reveals and which teaching approach to try next.
    [CreateAssetMenu(menuName = "Number Line Playground/Question", fileName = "NewQuestion")]
    public class QuestionData : ScriptableObject
    {
        [TextArea(2, 5)] public string prompt;
        public AnswerOption[] options;

        [Tooltip("Which skill this tests, e.g. 'Read it', 'Do it', 'Think it'.")]
        public string skillTag;
    }
}
