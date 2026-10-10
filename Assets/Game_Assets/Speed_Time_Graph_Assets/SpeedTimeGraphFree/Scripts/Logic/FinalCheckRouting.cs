using System.Collections.Generic;

namespace SpeedTimeGraphFree
{
    public enum FinalRoute { TopicComplete, RestartDemo, Remediate }

    /// <summary>
    /// 3/3 -> topic complete. 0/3 -> restart from the Demo. Otherwise replay the question of each
    /// missed concept (slope -> Q2, area -> Q3, crossing -> Q1) with an unseen variant, then re-ask only
    /// the missed items in wording B.
    /// </summary>
    public static class FinalCheckRouting
    {
        public static readonly int[] ConceptToQuestion = { 2, 3, 1 };

        public static int Score(bool[] correct) { int n = 0; foreach (bool c in correct) if (c) n++; return n; }

        public static FinalRoute Route(bool[] correct)
        {
            int score = Score(correct);
            return score == correct.Length ? FinalRoute.TopicComplete : score == 0 ? FinalRoute.RestartDemo : FinalRoute.Remediate;
        }

        /// <summary>Questions to replay, in course order.</summary>
        public static int[] QuestionsToReplay(bool[] correct)
        {
            var list = new List<int>();
            if (Route(correct) != FinalRoute.Remediate) return list.ToArray();
            for (int i = 0; i < correct.Length; i++) if (!correct[i]) list.Add(ConceptToQuestion[i]);
            list.Sort();
            return list.ToArray();
        }

        /// <summary>Items re-asked in wording B (only the missed ones).</summary>
        public static int[] ItemsToReask(bool[] correct)
        {
            var list = new List<int>();
            for (int i = 0; i < correct.Length; i++) if (!correct[i]) list.Add(i);
            return list.ToArray();
        }
    }
}
