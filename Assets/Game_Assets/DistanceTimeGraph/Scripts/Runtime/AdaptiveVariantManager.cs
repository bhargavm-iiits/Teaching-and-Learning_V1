using DistanceTimeGraph.Logic;
using UnityEngine;

namespace DistanceTimeGraph
{
    /// Tracks every course shown this session and hands out the next one: never the same course straight after a
    /// failure, unseen prepared variants first (the first alternate equal or slightly easier), then generated courses
    /// within safe limits. Decoration (colours, islands, lighting, story) changes with each course; physics never does.
    public class AdaptiveVariantManager : MonoBehaviour
    {
        public DtgCatalog catalog;
        AdaptiveSelection selection;

        public AdaptiveSelection Selection
        {
            get
            {
                if (selection == null)
                {
                    int seed = catalog != null && catalog.randomSeed != 0 ? catalog.randomSeed : System.Environment.TickCount;
                    selection = new AdaptiveSelection(catalog != null ? catalog.variants : VariantCatalog.Defaults(), seed);
                }
                return selection;
            }
        }

        public VariantSpec First(LevelKind kind, VariantSpec avoid = null)
        {
            var v = Selection.First(kind, avoid);
            Dtg.Log($"first course for {kind}: {v.id}{(v.generated ? " (generated)" : "")}; shown so far: {string.Join(",", Selection.shown)}");
            return v;
        }

        public VariantSpec AfterFailure(VariantSpec failed)
        {
            var v = Selection.AfterFailure(failed);
            Dtg.Log($"after failing {failed.id}: {v.id}{(v.generated ? " (generated)" : "")}, {VariantSpec.Differences(v, failed)} elements changed");
            return v;
        }
    }
}
