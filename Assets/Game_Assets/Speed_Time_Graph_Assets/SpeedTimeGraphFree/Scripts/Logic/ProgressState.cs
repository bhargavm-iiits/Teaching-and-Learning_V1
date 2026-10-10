using System;
using System.Collections.Generic;

namespace SpeedTimeGraphFree
{
    /// <summary>Everything the game remembers about one learner session.</summary>
    [Serializable]
    public class ProgressState
    {
        public bool[] Completed = new bool[4];                 // 0 Demo, 1..3 questions
        readonly Dictionary<int, List<int>> seen = new Dictionary<int, List<int>>();
        public int Runs;                                        // runs on the current variant
        public int TotalRuns;
        public List<string> Misconceptions = new List<string>();
        public List<string> Events = new List<string>();

        public List<int> SeenOrder(int question)
        {
            if (!seen.TryGetValue(question, out var list)) seen[question] = list = new List<int>();
            return list;
        }
        public void Log(string text) { Events.Add(DateTime.Now.ToString("HH:mm:ss") + " " + text); }
    }
}
