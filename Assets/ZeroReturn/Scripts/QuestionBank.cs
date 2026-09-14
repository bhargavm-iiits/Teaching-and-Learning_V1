using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ZeroReturn
{
    [Serializable] public class QuestionDef
    {
        public string id, pool;
        [TextArea] public string prompt;
        public string[] options;
        public int correct;
        [TextArea] public string explanation;
    }
    public class QuestionInstance
    {
        public string id, pool, prompt, explanation;
        public string[] options;
        public int correct;
    }
    [CreateAssetMenu(menuName = "Zero Return/Question Bank")]
    public class QuestionBank : ScriptableObject
    {
        public QuestionDef[] questions;
        public QuestionInstance[] Draw(LevelResult run, System.Random rng, HashSet<string> seen, int attempt)
        {
            var x = run.instance.coordinates;
            float a = Mathf.Abs(x.First(v => v < 0)), b = x.First(v => v > 0);
            var values = new Dictionary<string, string> {
                { "path", F(run.path) }, { "a", F(a) }, { "b", F(b) }, { "twoa", F(2*a) },
                { "p", F(run.path) }, { "pPlusA", F(run.path+a) }, { "pPlus2A", F(run.path+2*a) },
                { "pMinus2A", F(run.path-2*a) }, { "sep", F(run.instance.FigureSize) },
                { "sum", F(a+b) }, { "difference", F(b-a) }, { "smaller", F(a-1) },
                { "time", F(run.seconds) }, { "speed", F(run.path/Mathf.Max(.01f,run.seconds)) }
            };
            string Fill(string text) { foreach (var pair in values) text = text.Replace("{" + pair.Key + "}", pair.Value); return text; }
            var output = new List<QuestionInstance>();
            foreach (string pool in new[] { "S", "P", "D" })
            {
                var available = questions.Where(q => q.pool == pool && !seen.Contains(q.id)).ToArray();
                QuestionDef source;
                if (available.Length > 0) source = available[rng.Next(available.Length)];
                else
                {
                    // A finite 12-item bank cannot supply infinitely many unseen questions.
                    // Extend it with new numerical items instead of silently repeating an ID.
                    int n = 13 + attempt * 3;
                    source = pool == "S" ? new QuestionDef { id = "S-extra-" + attempt, pool = pool,
                        prompt = "After your {path} m run, imagine another out-and-back trip to x = -" + n + " m on an extended rail. How much does the path length increase?",
                        options = new[] { F(n), F(n*2), "0", F(-n) }, correct = 1, explanation = "Add both legs: " + n + " + " + n + " = " + (n*2) + " m." }
                        : pool == "P" ? new QuestionDef { id = "P-extra-" + attempt, pool = pool,
                        prompt = "On an extended rail, what is the separation between x = -" + n + " m and x = +2 m?",
                        options = new[] { F(n-2), F(n+2), F(n), "2" }, correct = 1, explanation = "Separation is the absolute difference: 2 - (-" + n + ")." }
                        : new QuestionDef { id = "D-extra-" + attempt, pool = pool,
                        prompt = "A bot moves from x = +" + n + " m to x = +2 m. What is its signed displacement?",
                        options = new[] { F(n-2), F(2-n), F(n+2), "0" }, correct = 1, explanation = "Final minus initial: 2 - " + n + " = " + (2-n) + " m." };
                }
                seen.Add(source.id);
                int[] order = Enumerable.Range(0, 4).OrderBy(_ => rng.Next()).ToArray();
                output.Add(new QuestionInstance { id = source.id, pool = pool, prompt = Fill(source.prompt),
                    options = order.Select(i => Fill(source.options[i])).ToArray(), correct = Array.IndexOf(order, source.correct), explanation = Fill(source.explanation) });
            }
            return output.ToArray();
        }
        static string F(float x) => x.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
