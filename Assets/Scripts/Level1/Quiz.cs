using System;

namespace NumberLinePlayground.Level1
{
    /// One multiple-choice question. The options are written with the right answer first; QuizBank.Order shuffles them.
    public sealed class QuizQuestion
    {
        public string prompt;
        public string[] options;        // four options, options[0] is the right one
        public string explanation;      // shown after the answer, right or wrong
    }

    /// What happens after a quiz, decided from how many answers were wrong. No Unity types, so it can be tested on its own.
    public static class RetryRules
    {
        /// One wrong answer goes back to the last level, two to the one before it, and so on; every wrong starts again
        /// from Level 1. Returns 0 when nothing was wrong (go on to the next topic).
        public static int FirstLevelToReplay(int wrongAnswers, int levelCount)
        {
            if (wrongAnswers <= 0) return 0;
            int level = levelCount + 1 - wrongAnswers;
            return level < 1 ? 1 : level;
        }

        /// The levels played again, from the first one to replay through to the last, so the whole run of levels that
        /// build up to the quiz is refreshed and the quiz then asks only what was missed.
        public static int[] LevelsToPlay(int firstLevel, int levelCount)
        {
            if (firstLevel < 1) return new int[0];
            var levels = new int[levelCount - firstLevel + 1];
            for (int i = 0; i < levels.Length; i++) levels[i] = firstLevel + i;
            return levels;
        }
    }

    /// The rule of the square level, on plain numbers. The spots carry no numbers: the four spheres must go on in two pairs,
    /// and the two spheres of a pair must be exactly one side apart on the number line.
    public static class PairRule
    {
        /// The side of the square, from the four numbers smallest first: the gap between the two lowest.
        public static int Side(int[] sorted) => sorted[1] - sorted[0];

        /// True when the four numbers (smallest first) make exactly two pairs a side apart: the two lowest and the two
        /// highest. The gap between the middle two must differ from the side, or another pairing would also work.
        public static bool IsSquareSet(int[] sorted) =>
            sorted.Length == 4 && sorted[3] - sorted[2] == Side(sorted) && sorted[2] - sorted[1] != Side(sorted);

        public static bool IsPair(int a, int b, int side) => (a > b ? a - b : b - a) == side;
    }

    /// The three questions of "Position, Origin and Direction", each with a second wording for when it is asked again.
    public static class QuizBank
    {
        public const int QuestionCount = 3;

        static readonly QuizQuestion[][] bank =
        {
            new[]   // 1: the origin
            {
                new QuizQuestion
                {
                    prompt = "What is the origin?",
                    options = new[] { "The place every position is measured from", "The biggest number on the line", "The end of the line", "A place with no position" },
                    explanation = "Every position is measured from the origin, x = 0.",
                },
                new QuizQuestion
                {
                    prompt = "Why did every trip start at the ring?",
                    options = new[] { "The ring is the origin, where x = 0", "It is the closest place to the spheres", "It is the only place you may stand", "It is where the line ends" },
                    explanation = "The ring marks the origin, the place positions are measured from.",
                },
            },
            new[]   // 2: direction and sign
            {
                new QuizQuestion
                {
                    prompt = "A sphere is at x = -4 m. Where is it?",
                    options = new[] { "4 m from the origin, on the negative side", "4 m from the origin, on the positive side", "At the origin", "4 m above the origin" },
                    explanation = "The minus sign puts it on the opposite side to the positive direction.",
                },
                new QuizQuestion
                {
                    prompt = "A sphere is at x = +7 m. What does the plus sign tell you?",
                    options = new[] { "It is on the positive side of the origin", "It is on the negative side of the origin", "It is at the origin", "It is heavier than the other sphere" },
                    explanation = "Plus means the positive direction from the origin.",
                },
            },
            new[]   // 3: the length between two positions
            {
                new QuizQuestion
                {
                    prompt = "One sphere is at -4 m and another at +7 m. How far apart are they?",
                    options = new[] { "11 m", "3 m", "7 m", "4 m" },
                    explanation = "From -4 to +7 is 7 - (-4) = 11 m.",
                },
                new QuizQuestion
                {
                    prompt = "One sphere is at -6 m and another at +3 m. How far apart are they?",
                    options = new[] { "9 m", "3 m", "6 m", "-3 m" },
                    explanation = "From -6 to +3 is 3 - (-6) = 9 m.",
                },
            },
        };

        /// Question 0, 1 or 2, in its first wording (attempt 0) or its second (attempt 1), then the first again, and so on.
        public static QuizQuestion Get(int question, int attempt)
        {
            var wordings = bank[question];
            return wordings[attempt % wordings.Length];
        }

        /// A shuffled order for the options: Order(seed, 4)[k] is which option to show in position k.
        public static int[] Order(int seed, int count)
        {
            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            var random = new Random(seed);
            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int swap = order[i];
                order[i] = order[j];
                order[j] = swap;
            }
            return order;
        }
    }
}
