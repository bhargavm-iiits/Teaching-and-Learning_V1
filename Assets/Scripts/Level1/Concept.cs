using System;
using System.Globalization;
using UnityEngine;

namespace NumberLinePlayground.Level1
{
    public enum Figure { Line, Triangle, Square }

    /// One level of the topic: what to build, from which numbers, and where the marked spots sit on the table.
    public sealed class LevelConfig
    {
        public int number;
        public Figure figure;
        public string figureName;       // "line", "triangle", "square"
        public string machineTitle;     // shown on the machine's display
        public string task;             // what the popup says
        public string hint;             // one extra line under it, or empty; {d} stands for the side of the square
        public string lesson;           // one line shown when the level is done
        public string[] chips = { "One at a time", "Start at 0", "Match the spots" };   // the three short reminders
        public int[][] variants;        // the numbers the spheres carry; each replay uses the next set
        public Vector2[] layout;        // the spots, in the tilted table surface's own x and z
        public float[] lift;            // how far each spot stands above the surface (a raised spot sits on a post), or null
        public bool headingGate;        // before each trip, face the way you will go and squeeze at the ring
        public bool tallBeams;          // tall beams with the number on top mark where each sphere waits, so far ones can be found
        public bool pairRule;           // the spots carry no numbers: spheres go on in pairs that must be one side apart
        public int[][] rows;            // with the pair rule: the pairs of spots, by index; each pair is one edge of the figure
        public bool markTool;           // a SET MARK button is offered on replays, to measure a gap by walking it

        public int Count => layout.Length;

        public float LiftAt(int spot) => lift != null && spot < lift.Length ? lift[spot] : 0f;

        /// The numbers for one attempt, smallest first, so the spheres wait on the line in order.
        public int[] Values(int variant)
        {
            int n = variants.Length;
            var set = (int[])variants[((variant % n) + n) % n].Clone();
            Array.Sort(set);
            return set;
        }

        /// The side of the square for one attempt: how far apart the two spheres of a pair are.
        public int SideLength(int variant)
        {
            var set = Values(variant);
            return set.Length >= 2 ? PairRule.Side(set) : 0;
        }

        public string HintFor(int variant) =>
            (hint ?? "").Replace("{d}", SideLength(variant).ToString(CultureInfo.InvariantCulture));
    }

    /// What one finished level reports back to the flow.
    public sealed class LevelStats
    {
        public string figure;           // "Line   -4 to +7  (11 m)"
        public float path;              // metres walked
        public float displacement;      // metres from the origin at the end
        public int stars;               // 3, 2 or 1
    }

    public static class Concept
    {
        public const string TopicName = "Position, Origin and Direction";
        public const string NextTopicName = "Distance and Displacement";

        /// The number line runs from -LineHalfLength to +LineHalfLength; spheres carry whole numbers up to MaxNumber,
        /// which leaves the last metre or so of line beyond the farthest one.
        public const int LineHalfLength = 10;
        public const int MaxNumber = 9;

        // Numbers are whole and never 0 (that is the origin). Every replay takes the next set, so the same level comes
        // back with different spheres: "the same game, but not the same as before".
        public static readonly LevelConfig[] Levels =
        {
            new LevelConfig
            {
                number = 1, figure = Figure.Line, figureName = "line", machineTitle = "LINE MAKER",
                task = "Fetch 2 spheres and place them on the table to make a line.",
                hint = "",
                lesson = "The line's length, the distance you walked and your displacement are three different quantities.",
                variants = new[] { new[] { -4, 7 }, new[] { -6, 3 }, new[] { -2, 8 }, new[] { -7, 5 }, new[] { -3, 6 } },
                layout = new[] { new Vector2(-1.0f, -0.15f), new Vector2(1.0f, -0.15f) },
            },
            new LevelConfig
            {
                // Direction becomes your body: left is negative, right is positive. The base corners sit at waist height
                // and the top corner on a post at chest height, so "the top" is somewhere you reach up to. The first set,
                // -6, +2 and +9, is three trips of 12, 4 and 18 metres.
                number = 2, figure = Figure.Triangle, figureName = "triangle", machineTitle = "TRIANGLE MAKER",
                task = "Fetch 3 spheres and place them on the table to make a triangle.",
                hint = "Left is negative, right is positive. Face your way, press G.",
                lesson = "Every corner has one position, measured from the same origin. Left is negative, right is positive.",
                variants = new[] { new[] { -6, 2, 9 }, new[] { -7, 1, 8 }, new[] { -9, -2, 5 }, new[] { -5, 3, 9 }, new[] { -8, -1, 6 } },
                layout = new[] { new Vector2(-0.95f, -0.45f), new Vector2(0f, 0.35f), new Vector2(0.95f, -0.45f) },
                lift = new[] { 0f, 0.3f, 0f },
                headingGate = true, tallBeams = true,
            },
            new LevelConfig
            {
                // Plan first. The spots carry no numbers. The four numbers make two pairs that are exactly one side apart
                // (the first set: -8 and -4, +2 and +6, side 4 m), and each pair is one edge of the square: the front
                // edge and the back edge. A pair that is not a side apart is undone in plain sight. Four trips, 40 m.
                number = 3, figure = Figure.Square, figureName = "square", machineTitle = "SQUARE MAKER",
                task = "Fetch 4 spheres and place them on the table to make a square.",
                hint = "Plan first: find the two pairs that are {d} m apart.",
                lesson = "The distance between two positions is a difference: the far one minus the near one.",
                chips = new[] { "One at a time", "Start at 0", "Match the distance" },
                variants = new[] { new[] { -8, -4, 2, 6 }, new[] { -7, -4, 1, 4 }, new[] { -6, -2, 3, 7 }, new[] { -9, -6, 1, 4 }, new[] { -7, -2, 2, 7 } },
                layout = new[] { new Vector2(-0.6f, -0.5f), new Vector2(0.6f, -0.5f), new Vector2(0.6f, 0.7f), new Vector2(-0.6f, 0.7f) },
                rows = new[] { new[] { 0, 1 }, new[] { 3, 2 } },
                tallBeams = true, pairRule = true, markTool = true,
            },
        };

        /// "-4", "+7": a whole number with its sign.
        public static string Signed(int value) =>
            (value > 0 ? "+" : "") + value.ToString(CultureInfo.InvariantCulture);

        /// What the completion popup calls the figure that was built, from the numbers used.
        public static string Describe(LevelConfig level, int[] values)
        {
            string name = char.ToUpperInvariant(level.figureName[0]) + level.figureName.Substring(1);
            var parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++) parts[i] = Signed(values[i]);
            if (level.figure == Figure.Line)
                return name + "   " + parts[0] + " to " + parts[parts.Length - 1] + "  (" + (values[values.Length - 1] - values[0]) + " m)";
            if (level.pairRule)
                return name + "   side " + PairRule.Side(values) + " m  (" + string.Join(", ", parts) + ")";
            return name + "   " + string.Join(", ", parts);
        }
    }
}
