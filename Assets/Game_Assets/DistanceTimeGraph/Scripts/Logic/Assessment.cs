using System;
using System.Collections.Generic;

namespace DistanceTimeGraph.Logic
{
    [Serializable]
    public class AssessmentQuestion
    {
        public string prompt;
        public string[] options;
        public int correct;
    }

    public enum AssessmentRoute { NextTopic, FromLevel2, FromLevel1 }

    /// The final check: three questions, all of which must be answered before submitting, and the routing by score.
    public static class Assessment
    {
        public const string Title = "FINAL CHECK · WHAT DID YOU DISCOVER?";

        public static AssessmentQuestion[] Questions() => new[]
        {
            new AssessmentQuestion
            {
                prompt = "What does a steeper upward graph segment make the car do?",
                options = new[] { "Move away from the start faster", "Remain at the same distance", "Move back toward the start" },
                correct = 0
            },
            new AssessmentQuestion
            {
                prompt = "The graph remains at 40 m from 6 s to 9 s. What does the car do?",
                options = new[] { "Moves forward for three seconds", "Waits at 40 m for three seconds", "Returns to the start" },
                correct = 1
            },
            new AssessmentQuestion
            {
                prompt = "The graph falls from 60 m to 20 m as time increases. What does this mean?",
                options = new[] { "The car moves back toward the start", "The car remains at 60 m", "The car instantly resets" },
                correct = 0
            },
        };

        /// -1 means unanswered.
        public static bool AllAnswered(IList<int> answers)
        {
            if (answers == null || answers.Count < 3) return false;
            foreach (var a in answers) if (a < 0) return false;
            return true;
        }

        public static int Score(AssessmentQuestion[] questions, IList<int> answers)
        {
            int n = 0;
            for (int i = 0; i < questions.Length && i < answers.Count; i++) if (answers[i] == questions[i].correct) n++;
            return n;
        }

        public static AssessmentRoute Route(int score) =>
            score >= 3 ? AssessmentRoute.NextTopic : score == 2 ? AssessmentRoute.FromLevel2 : AssessmentRoute.FromLevel1;

        public static string ResultTitle(int score) => score >= 3 ? "TOPIC COMPLETE" : $"{score} OF 3 CORRECT";

        public static string ResultMessage(int score) =>
            score >= 3 ? "You understand how graph shape controls straight-line motion."
            : score == 2 ? "Review waiting and return motion from Level 2."
            : "Let’s rebuild the idea from the beginning.";

        public static string ResultButton(int score) =>
            score >= 3 ? "NEXT TOPIC" : score == 2 ? "START FROM LEVEL 2" : "RESTART FROM LEVEL 1";
    }

    /// The 13 steps of the topic (plus the failure review inside a level).
    public enum GameState
    {
        Introduction,
        Level1Edit, Level1Run, Level1Complete,
        Level2Edit, Level2Run, Level2Complete,
        Level3Edit, Level3Run, Level3Complete,
        FinalAssessment, AssessmentResult, NextTopicOrReview,
        AttemptRecorded
    }

    public static class GameStates
    {
        public static GameState Edit(int level) => level == 0 ? GameState.Level1Edit : level == 1 ? GameState.Level2Edit : GameState.Level3Edit;
        public static GameState Run(int level) => level == 0 ? GameState.Level1Run : level == 1 ? GameState.Level2Run : GameState.Level3Run;
        public static GameState Complete(int level) => level == 0 ? GameState.Level1Complete : level == 1 ? GameState.Level2Complete : GameState.Level3Complete;

        /// The graph can only be edited in the three edit states: never during a run, a popup or the assessment.
        public static bool CanEdit(GameState s) => s == GameState.Level1Edit || s == GameState.Level2Edit || s == GameState.Level3Edit;
    }

    /// One recorded run: the graph, the car motion it produced and whether it met the goal.
    [Serializable]
    public class AttemptRecord
    {
        public string variantId;
        public int level;
        public GraphPoint[] points;
        public RunProfile profile;
        public bool success;
        public string summary;
    }

    /// Everything that is reset by the routing rules, and what is not (settings).
    [Serializable]
    public class ProgressData
    {
        public bool[] levelComplete = new bool[3];
        public string[] completedVariant = new string[3];
        public List<AttemptRecord> attempts = new List<AttemptRecord>();
        public int[] answers = { -1, -1, -1 };
        // Kept through every reset.
        public float audioVolume = 1f;
        public float textScale = 1f;

        public const int VisibleAttempts = 3;

        public void Record(AttemptRecord a) => attempts.Add(a);

        /// The last few attempts of a level, newest last.
        public List<AttemptRecord> Recent(int level)
        {
            var list = attempts.FindAll(a => a.level == level);
            if (list.Count > VisibleAttempts) list.RemoveRange(0, list.Count - VisibleAttempts);
            return list;
        }

        public void ClearAnswers() { for (int i = 0; i < answers.Length; i++) answers[i] = -1; }

        /// Two correct: Level 1 stays complete; Levels 2 and 3 and their attempts are cleared.
        public void ResetFromLevel2()
        {
            for (int i = 1; i < 3; i++) { levelComplete[i] = false; }
            attempts.RemoveAll(a => a.level >= 1);
            ClearAnswers();
        }

        /// One or zero correct: all level progress, graphs, attempts and answers are cleared. Settings stay.
        public void ResetAll()
        {
            for (int i = 0; i < 3; i++) { levelComplete[i] = false; }
            attempts.Clear();
            ClearAnswers();
        }
    }
}
