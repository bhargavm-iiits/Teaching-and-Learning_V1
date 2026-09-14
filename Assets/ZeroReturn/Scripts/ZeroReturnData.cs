using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZeroReturn
{
    public enum GameState { Title, ConceptCard, LevelBrief, LevelPlay, LevelComplete, QuizIntro, QuizQuestion, QuizResult, Remediation, ConceptComplete }
    public enum ShapeType { Line, Triangle, Square }
    public enum Modifier { None, HeadingGate, NearestFirst, BlindSockets }

    [Serializable] public class TripRecord
    {
        public float target, pathLength, displacement, seconds;
        public List<float> samples = new List<float>();
    }

    [Serializable] public class LevelInstance
    {
        public int seed, levelIndex;
        public ShapeType shape;
        public float[] coordinates;
        public float unitsPerTick = 1;
        public bool mirrored, rotated;
        public Modifier modifier;
        public int[] socketOrder;
        public float requiredSide;
        public int quizCycle;
        public float ExpectedPath => 2 * coordinates.Sum(x => Mathf.Abs(x));
        public float FigureSize => shape == ShapeType.Square ? requiredSide : coordinates.Max() - coordinates.Min();
    }

    [Serializable] public class LevelResult
    {
        public LevelInstance instance;
        public float path, displacement, seconds;
        public int violations;
        public List<TripRecord> trips = new List<TripRecord>();
        public int Stars => violations == 0 ? 3 : violations <= 2 ? 2 : 1;
    }

    public static class VariantGenerator
    {
        public static int Seed(string concept, string player, int attempt, int level)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in concept + ":" + player + ":" + attempt + ":" + level) h = (h ^ c) * 16777619;
                return (int)(h & 0x7fffffff);
            }
        }

        public static LevelInstance Build(LevelTemplate template, int seed, int attempt, LevelInstance previous)
        {
            var rng = new System.Random(seed);
            for (int guard = 0; guard < 256; guard++)
            {
                var inst = new LevelInstance { seed = seed, levelIndex = template.levelIndex,
                    shape = template.shape, mirrored = attempt % 2 != 0, rotated = attempt % 2 != 0,
                    unitsPerTick = new[] { 1f, .5f, 2f }[attempt % 3], modifier = template.modifier,
                    quizCycle = attempt };
                int count = (int)template.shape + 2;
                if (attempt == 0 && template.exampleCoordinates.Length == count)
                {
                    inst.coordinates = (float[])template.exampleCoordinates.Clone();
                    inst.requiredSide = template.exampleSide;
                }
                else if (template.shape == ShapeType.Square)
                {
                    int d = rng.Next(2, 6), p = rng.Next(-12, -d), q = rng.Next(1, 13 - d);
                    inst.coordinates = new float[] { p, p + d, q, q + d };
                    inst.requiredSide = d;
                }
                else
                {
                    var values = new List<float> { rng.Next(-12, -1), rng.Next(2, 13) };
                    if (count == 3)
                    {
                        int x = rng.Next(-10, 11);
                        if (x == 0 || values.Any(v => Mathf.Abs(v - x) < 2)) continue;
                        values.Add(x);
                    }
                    inst.coordinates = values.OrderBy(v => v).ToArray();
                }
                if (inst.mirrored) inst.coordinates = inst.coordinates.Select(x => -x).OrderBy(x => x).ToArray();
                inst.socketOrder = Enumerable.Range(0, count).ToArray();
                if (attempt > 0 && inst.shape != ShapeType.Square)
                    inst.socketOrder = inst.socketOrder.OrderBy(_ => rng.Next()).ToArray();
                if (attempt > 0 && inst.shape == ShapeType.Line) inst.modifier = attempt % 2 == 1 ? Modifier.HeadingGate : Modifier.NearestFirst;
                if (!Valid(inst)) continue;
                if (previous != null && !SufficientlyDifferent(inst, previous)) continue;
                return inst;
            }
            // Never quietly accept a variant that failed the difference guarantee.
            throw new InvalidOperationException("Unable to generate a valid, sufficiently different level for seed " + seed);
        }

        public static bool Valid(LevelInstance x)
        {
            if (x.coordinates.Length != (int)x.shape + 2 || !x.coordinates.Any(v => v < 0) || !x.coordinates.Any(v => v > 0)) return false;
            var sorted = x.coordinates.OrderBy(v => v).ToArray();
            if (sorted.Any(v => Mathf.Abs(v) > 12 || v == 0)) return false;
            for (int i = 1; i < sorted.Length; i++) if (sorted[i] - sorted[i - 1] < 2) return false;
            return x.shape != ShapeType.Square || SquareSeedIsUnique(sorted, x.requiredSide);
        }

        public static bool SufficientlyDifferent(LevelInstance a, LevelInstance b)
        {
            bool coords = !a.coordinates.SequenceEqual(b.coordinates), quiz = a.quizCycle != b.quizCycle;
            int axes = (coords ? 1 : 0) + (quiz ? 1 : 0) + (a.mirrored != b.mirrored ? 1 : 0)
                + (a.unitsPerTick != b.unitsPerTick ? 1 : 0) + (!a.socketOrder.SequenceEqual(b.socketOrder) ? 1 : 0)
                + (a.rotated != b.rotated ? 1 : 0) + (a.modifier != b.modifier ? 1 : 0);
            return coords && quiz && axes >= 4;
        }

        public static bool SquareSeedIsUnique(float[] x, float d)
        {
            int[,] p = { { 0, 1, 2, 3 }, { 0, 2, 1, 3 }, { 0, 3, 1, 2 } };
            int matches = 0;
            for (int i = 0; i < 3; i++)
                if (Mathf.Approximately(Mathf.Abs(x[p[i, 0]] - x[p[i, 1]]), d)
                    && Mathf.Approximately(Mathf.Abs(x[p[i, 2]] - x[p[i, 3]]), d)) matches++;
            return matches == 1;
        }
    }

    /// Pure puzzle state: no visual effect can override these rules.
    public class AssemblyModel
    {
        public readonly LevelInstance level;
        public readonly int[] sockets;
        public readonly bool[] collected;
        public int carry = -1;
        public int violations;
        public bool Complete => sockets.All(x => x >= 0);
        public AssemblyModel(LevelInstance level)
        {
            this.level = level;
            sockets = Enumerable.Repeat(-1, level.coordinates.Length).ToArray();
            collected = new bool[sockets.Length];
        }

        public string CanLaunch(int sphere, int heading, float coordinate)
        {
            if (carry >= 0) return "Deposit your sphere before the next fetch."; // R3, free
            if (Mathf.Abs(coordinate) > .01f) return "Return to the origin before launching."; // R2, free
            if (sphere < 0 || sphere >= collected.Length || collected[sphere]) return "That sphere is already collected.";
            if (level.levelIndex >= 1 || level.modifier == Modifier.HeadingGate)
            {
                if (heading == 0) return "Choose a heading before launching.";
                if (heading != Math.Sign(level.coordinates[sphere])) { violations++; return "Wrong heading. Compare the sphere's sign with your arrow. (-1 pip)"; }
            }
            if (level.modifier == Modifier.NearestFirst)
            {
                float nearest = level.coordinates.Where((x, i) => !collected[i]).Min(x => Mathf.Abs(x));
                if (!Mathf.Approximately(Mathf.Abs(level.coordinates[sphere]), nearest)) { violations++; return "Nearest first: choose the smallest distance from zero. (-1 pip)"; }
            }
            return null;
        }

        public bool Grab(int sphere)
        {
            if (carry >= 0 || collected[sphere]) return false;
            carry = sphere; collected[sphere] = true; return true;
        }

        public string Place(int socket, out int[] ejected)
        {
            ejected = Array.Empty<int>();
            if (carry < 0) return "Fetch a sphere first.";
            if (sockets[socket] >= 0) return "That socket is occupied.";
            if (level.shape != ShapeType.Square && level.socketOrder[socket] != carry)
            { violations++; return "Wrong position. Match the coordinate stamp to the socket. (-1 pip)"; }
            int held = carry;
            sockets[socket] = carry; carry = -1;
            if (level.shape == ShapeType.Square)
            {
                int partner = socket ^ 1;
                if (sockets[partner] >= 0 && !Mathf.Approximately(Mathf.Abs(level.coordinates[held] - level.coordinates[sockets[partner]]), level.requiredSide))
                {
                    ejected = new[] { held, sockets[partner] };
                    foreach (int index in ejected) collected[index] = false;
                    sockets[socket] = sockets[partner] = -1;
                    violations++;
                    return "Both spheres returned to the rail: this pair is not " + level.requiredSide + " m apart. (-1 pip)";
                }
            }
            return null;
        }
    }
}
