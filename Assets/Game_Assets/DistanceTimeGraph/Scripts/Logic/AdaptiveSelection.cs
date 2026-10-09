using System;
using System.Collections.Generic;
using System.Linq;

namespace DistanceTimeGraph.Logic
{
    /// Chooses the next course of a level. A learner never gets the exact same course straight after a failure: an
    /// unseen prepared variant is preferred (the first alternate equal or slightly easier), the one just shown is never
    /// chosen again, and once every prepared variant has been used a new one is generated inside safe limits.
    public class AdaptiveSelection
    {
        public const int MinDifferences = 3;
        public const int Themes = 6, IslandLayouts = 3, LightingPresets = 4;

        readonly List<VariantSpec> catalog;
        readonly Random rng;
        public readonly List<string> shown = new List<string>();   // every variant shown this session, in order
        readonly Dictionary<string, VariantSpec> generated = new Dictionary<string, VariantSpec>();
        int generatedCount;

        public AdaptiveSelection(IEnumerable<VariantSpec> catalog, int seed)
        {
            this.catalog = catalog.ToList();
            rng = new Random(seed);
        }

        public IEnumerable<VariantSpec> Prepared(LevelKind kind) => catalog.Where(v => v.kind == kind);
        public bool Seen(string id) => shown.Contains(id);

        public void MarkShown(VariantSpec v)
        {
            if (v != null && !shown.Contains(v.id)) shown.Add(v.id);
        }

        /// The first course of a level (session start, or a restart after the final check): an unseen prepared variant.
        /// The very first time, the brief's "A" variant is used.
        public VariantSpec First(LevelKind kind, VariantSpec avoid = null)
        {
            var prepared = Prepared(kind).ToList();
            if (shown.Count == 0 || !prepared.Any(v => Seen(v.id)))
            {
                var a = prepared.FirstOrDefault();
                if (a != null && !Seen(a.id) && Acceptable(a, avoid)) return Pick(a);
            }
            var unseen = prepared.Where(v => !Seen(v.id) && Acceptable(v, avoid)).ToList();
            if (unseen.Count > 0) return Pick(unseen[rng.Next(unseen.Count)]);
            return Pick(Generate(kind, avoid, avoid?.Difficulty ?? 99f));
        }

        /// The course after a failed attempt of `failed`: same idea, different route, equal or slightly easier first.
        public VariantSpec AfterFailure(VariantSpec failed)
        {
            var unseen = Prepared(failed.kind).Where(v => !Seen(v.id) && Acceptable(v, failed)).ToList();
            if (unseen.Count > 0)
            {
                // The first alternate should be equal or slightly easier: prefer the closest difficulty at or below.
                var easier = unseen.Where(v => v.Difficulty <= failed.Difficulty + 0.01f).OrderByDescending(v => v.Difficulty).ToList();
                return Pick(easier.Count > 0 ? easier[0] : unseen.OrderBy(v => v.Difficulty).First());
            }
            return Pick(Generate(failed.kind, failed, failed.Difficulty));
        }

        static bool Acceptable(VariantSpec candidate, VariantSpec previous) =>
            previous == null || (candidate.id != previous.id && !candidate.SameRoute(previous) && VariantSpec.Differences(candidate, previous) >= MinDifferences);

        VariantSpec Pick(VariantSpec v)
        {
            MarkShown(v);
            return v;
        }

        /// A new course inside the safe limits: goal 20-80 m, bridge opening 6-15 s and open at least 4 s, waiting
        /// 2-3 s, return target below the turnaround, everything on the 0-80 m track. It is no harder than `maxDifficulty`
        /// when that is possible, and differs from `avoid` in at least three elements.
        public VariantSpec Generate(LevelKind kind, VariantSpec avoid, float maxDifficulty)
        {
            VariantSpec best = null;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                var v = RandomSpec(kind);
                if (!Acceptable(v, avoid) || !WithinLimits(v)) continue;
                if (v.Difficulty <= maxDifficulty + 0.01f) { best = v; break; }
                if (best == null || v.Difficulty < best.Difficulty) best = v;
            }
            if (best == null) best = RandomSpec(kind);   // unreachable in practice; limits always allow a course
            generatedCount++;
            best.id = $"{(int)kind + 1}G{generatedCount}";
            best.generated = true;
            generated[best.id] = best;
            return best;
        }

        VariantSpec RandomSpec(LevelKind kind)
        {
            var v = new VariantSpec
            {
                kind = kind,
                theme = rng.Next(Themes),
                islands = rng.Next(IslandLayouts),
                lighting = rng.Next(LightingPresets),
            };
            int story = rng.Next(4);
            switch (kind)
            {
                case LevelKind.Rising:
                    v.start = 0;
                    v.goal = 5 * rng.Next(4, 17);                                    // 20..80
                    v.place = VariantCatalog.Level1Places[story];
                    v.story = string.Format(VariantCatalog.Level1Stories[story], v.goal);
                    v.title = $"REACH {v.goal} m";
                    v.landmark = new[] { Landmark.BlueGate, Landmark.WindTower, Landmark.ForestStation, Landmark.ParcelStation }[story];
                    break;
                case LevelKind.Waiting:
                    v.waitPad = 5 * rng.Next(4, 13);                                 // 20..60
                    v.bridgeEnd = v.waitPad + 20;                                    // ..80
                    v.goal = v.bridgeEnd;
                    v.waitTime = rng.Next(2, 4);                                     // 2..3
                    v.bridgeOpen = rng.Next(6, 12);                                  // 6..11
                    v.bridgeClose = Math.Min(15f, v.bridgeOpen + rng.Next(4, 6));    // open >= 4 s, closes by 15
                    v.place = VariantCatalog.Level2Places[story];
                    v.story = $"Cross the {v.place.ToLowerInvariant()}.";
                    v.title = "CROSS THE TIMED BRIDGE";
                    v.landmark = Landmark.BridgePlatform;
                    break;
                default:
                    v.turnaround = 5 * rng.Next(8, 17);                              // 40..80
                    v.goal = 5 * rng.Next(2, v.turnaround / 5 - 3);                  // 10..turnaround-20
                    v.place = VariantCatalog.Level3Places[story];
                    v.story = $"Return to the {v.place.ToLowerInvariant()}.";
                    v.title = "RETURN AND RECHARGE";
                    v.landmark = new[] { Landmark.ChargingStation, Landmark.CrystalPlatform, Landmark.RepairGarage, Landmark.EmergencyPad }[story];
                    break;
            }
            return v;
        }

        public static bool WithinLimits(VariantSpec v)
        {
            switch (v.kind)
            {
                case LevelKind.Rising:
                    return v.goal >= 20 && v.goal <= 80 && v.start >= 0 && v.start < v.goal;
                case LevelKind.Waiting:
                    return v.waitPad >= 20 && v.bridgeEnd <= 80 && v.bridgeEnd > v.waitPad && v.bridgeOpen >= 6 && v.bridgeClose <= 15 &&
                           v.bridgeClose - v.bridgeOpen >= 4 && v.waitTime >= 2 && v.waitTime <= 3 &&
                           // there is time to drive to the pad and wait before the bridge opens
                           v.bridgeOpen >= v.waitTime + 1;
                default:
                    return v.turnaround >= 20 && v.turnaround <= 80 && v.goal >= 0 && v.goal < v.turnaround;
            }
        }
    }
}
