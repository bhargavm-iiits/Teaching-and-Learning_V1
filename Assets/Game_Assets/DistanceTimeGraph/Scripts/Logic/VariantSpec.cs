using System;
using System.Collections.Generic;

namespace DistanceTimeGraph.Logic
{
    /// The three physics ideas, one per level.
    public enum LevelKind { Rising = 0, Waiting = 1, Returning = 2 }

    /// The destination prop placed on the track for a variant.
    public enum Landmark { BlueGate, ForestStation, WindTower, ParcelStation, ChargingStation, CrystalPlatform, RepairGarage, EmergencyPad, BridgePlatform }

    /// One course configuration of a level. Level 1 uses start and goal. Level 2 uses waitPad, bridgeEnd, bridgeOpen,
    /// bridgeClose and waitTime. Level 3 uses turnaround and goal (the return target). theme, islands, lighting and
    /// story are decoration that changes between variants without changing the physics.
    [Serializable]
    public class VariantSpec
    {
        public string id = "1A";
        public LevelKind kind;
        public string title = "REACH THE 40 m GATE";
        public string story = "Reach the 40 m gate.";
        public string place = "Blue island gate";
        public Landmark landmark = Landmark.BlueGate;
        public int start;
        public int goal = 40;
        public int waitPad = 40;
        public int bridgeEnd = 60;
        public float bridgeOpen = 8f;
        public float bridgeClose = 12f;
        public float waitTime = 2f;
        public int turnaround = 60;
        public int theme;      // index into the palette of flags, colours and sky
        public int islands;    // which island arrangement is shown
        public int lighting;   // time of day / weather preset
        public bool generated;

        public VariantSpec Clone() => (VariantSpec)MemberwiseClone();

        /// A rough effort figure used so the first alternate after a failure is equal or slightly easier.
        public float Difficulty
        {
            get
            {
                switch (kind)
                {
                    case LevelKind.Rising: return Math.Abs(goal - start) / 10f;
                    case LevelKind.Waiting: return waitTime + 8f / Math.Max(1f, bridgeClose - bridgeOpen) + (bridgeEnd - waitPad) / 20f;
                    default: return (turnaround + (turnaround - goal)) / 20f;
                }
            }
        }

        /// How many of the listed course elements differ between two variants (goal, start, bridge location, bridge time,
        /// waiting time, charging-pad location, turnaround, colours, islands, lighting, story).
        public static int Differences(VariantSpec a, VariantSpec b)
        {
            int n = 0;
            if (a.goal != b.goal) n++;
            if (a.start != b.start) n++;
            if (a.kind == LevelKind.Waiting && (a.waitPad != b.waitPad || a.bridgeEnd != b.bridgeEnd)) n++;
            if (a.kind == LevelKind.Waiting && (a.bridgeOpen != b.bridgeOpen || a.bridgeClose != b.bridgeClose)) n++;
            if (a.kind == LevelKind.Waiting && a.waitTime != b.waitTime) n++;
            if (a.kind == LevelKind.Returning && a.turnaround != b.turnaround) n++;
            if (a.theme != b.theme) n++;
            if (a.islands != b.islands) n++;
            if (a.lighting != b.lighting) n++;
            if (a.story != b.story) n++;
            return n;
        }

        /// The sentence shown in the activity panel.
        public string Instruction
        {
            get
            {
                switch (kind)
                {
                    case LevelKind.Rising: return story;
                    case LevelKind.Waiting:
                        return $"Reach the {waitPad} m sync pad, wait there at least {waitTime:0} s, then cross while the bridge is open and reach {bridgeEnd} m.";
                    default: return $"Drive out to the {turnaround} m turnaround beacon. Then move the final point lower and return the car to {goal} m.";
                }
            }
        }

        public bool SameRoute(VariantSpec other) =>
            other != null && kind == other.kind && start == other.start && goal == other.goal && waitPad == other.waitPad &&
            bridgeEnd == other.bridgeEnd && bridgeOpen == other.bridgeOpen && bridgeClose == other.bridgeClose &&
            waitTime == other.waitTime && turnaround == other.turnaround;
    }

    /// The prepared variants from the design brief. The Unity catalog asset is filled from these and can be edited.
    public static class VariantCatalog
    {
        public static List<VariantSpec> Defaults()
        {
            return new List<VariantSpec>
            {
                L1("1A", 40, "REACH THE 40 m GATE", "Reach the 40 m gate.", "Blue island gate", Landmark.BlueGate, 0, 0, 0),
                L1("1B", 30, "REACH THE 30 m STATION", "Reach the 30 m observation station.", "Forest observation station", Landmark.ForestStation, 1, 1, 1),
                L1("1C", 50, "POWER THE 50 m TOWER", "Deliver power to the 50 m tower.", "Wind-energy tower", Landmark.WindTower, 2, 2, 2),
                L1("1D", 60, "REACH THE 60 m PARCEL STATION", "Reach the parcel station at 60 m.", "Floating parcel station", Landmark.ParcelStation, 3, 0, 3),

                L2("2A", 40, 60, 8, 12, 2, "Green glass bridge", 0, 0, 0),
                L2("2B", 30, 50, 7, 11, 2, "Cloud-energy bridge", 4, 1, 1),
                L2("2C", 50, 70, 10, 15, 3, "Wind canyon bridge", 2, 2, 2),
                L2("2D", 20, 40, 6, 10, 2, "Waterfall bridge", 5, 1, 3),

                L3("3A", 60, 20, "RETURN TO CHARGE", "Return to the charging station.", "Charging station", Landmark.ChargingStation, 0, 0, 0),
                L3("3B", 70, 30, "COLLECT THE CRYSTAL", "Bring the car back to the crystal collection platform.", "Crystal collection platform", Landmark.CrystalPlatform, 4, 1, 1),
                L3("3C", 50, 10, "BACK TO THE GARAGE", "Reverse into the repair garage.", "Repair garage", Landmark.RepairGarage, 2, 2, 2),
                L3("3D", 80, 40, "EMERGENCY RECHARGE", "Return to the emergency energy pad.", "Emergency energy pad", Landmark.EmergencyPad, 3, 0, 3),
            };
        }

        static VariantSpec L1(string id, int goal, string title, string story, string place, Landmark mark, int theme, int islands, int lighting) =>
            new VariantSpec { id = id, kind = LevelKind.Rising, goal = goal, title = title, story = story, place = place, landmark = mark, theme = theme, islands = islands, lighting = lighting };

        static VariantSpec L2(string id, int pad, int end, float open, float close, float wait, string place, int theme, int islands, int lighting) =>
            new VariantSpec
            {
                id = id, kind = LevelKind.Waiting, waitPad = pad, bridgeEnd = end, goal = end, bridgeOpen = open, bridgeClose = close, waitTime = wait,
                title = "CROSS THE TIMED BRIDGE", story = $"Cross the {place.ToLowerInvariant()}.", place = place, landmark = Landmark.BridgePlatform,
                theme = theme, islands = islands, lighting = lighting
            };

        static VariantSpec L3(string id, int turn, int target, string title, string story, string place, Landmark mark, int theme, int islands, int lighting) =>
            new VariantSpec { id = id, kind = LevelKind.Returning, turnaround = turn, goal = target, title = title, story = story, place = place, landmark = mark, theme = theme, islands = islands, lighting = lighting };

        public static readonly string[] Level1Stories =
        {
            "Reach the {0} m signal gate.", "Deliver the parcel to the {0} m drop point.", "Reach the {0} m lookout.", "Bring supplies to the {0} m camp."
        };
        public static readonly string[] Level1Places = { "Signal gate", "Parcel drop point", "Lookout tower", "Supply camp" };
        public static readonly string[] Level2Places = { "Crystal bridge", "Sunset bridge", "Glacier bridge", "Rainbow bridge" };
        public static readonly string[] Level3Places = { "Solar pad", "Battery depot", "Rescue pad", "Energy dock" };
    }
}
