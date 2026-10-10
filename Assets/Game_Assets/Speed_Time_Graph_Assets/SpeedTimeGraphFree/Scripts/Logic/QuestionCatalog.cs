using System;

namespace SpeedTimeGraphFree
{
    /// <summary>Course-wide constants (all loaded from the JSON, defaults match section 5 of the brief).</summary>
    [Serializable]
    public class CourseConfig
    {
        public double k = 0.125;            // world units per metre, identical on all lanes
        public double offCourse = 500;      // metres; sand trap
        public double stopTolerance = 4;    // metres either side of the STOP line
        public double maxTime = 16, maxSpeed = 50;
        public double sampleStep = 0.25, speedTolerance = 1;
        public int maxPoints = 10, maxRunsPerVariant = 3;
        public string[] vehicles = { "CAR", "TRAIN", "BIKE" };
        public string[] vehicleColors = { "#3D9BFF", "#FF4D5A", "#FF9F1A" };
        public double[] laneZ = { 16, 8, 0 };
    }

    /// <summary>One lane (vehicle) of one question variant. Lane order is always CAR, TRAIN, BIKE.</summary>
    [Serializable]
    public class LaneData
    {
        public string vehicle, story;
        public GraphPoint[] solution;
        public double stop, timeLimit = 16, speedLimit = 12;
        public double gateDistance, gateMin, zoneStart, zoneEnd, zoneMax;
        public bool HasGate => gateDistance > 0;
    }

    [Serializable]
    public class VariantData
    {
        public string id;          // DEMO, 1A..1D, 2A..2D, 3A..3D
        public int question;       // 0 = Demo, 1..3
        public double snap = 1;        // time snap (s)
        public double snapV;           // velocity snap (m/s); 0 = same as snap
        public string title, idea, banner, keySentence;
        public LaneData[] lanes;
    }

    [Serializable]
    public class FinalCheckItem
    {
        public string id, concept, wording, prompt, explanation;
        public string[] options, tags;
        public int correct;
        public GraphPoint[] a, b;
        public string aVehicle, bVehicle;
        public int aLane, bLane = -1;
        public int routeQuestion;  // slope -> 2, area -> 3, crossing -> 1
    }

    [Serializable]
    public class QuestionCatalog
    {
        public CourseConfig course = new CourseConfig();
        public VariantData demo;
        public VariantData[] variants;
        public FinalCheckItem[] finalCheck;   // 0..2 wording A, 3..5 wording B

        public VariantData Variant(int question, int index) => Array.Find(variants, v => v.question == question && v.id == question.ToString() + (char)('A' + index));
        public int VariantCount(int question) => Array.FindAll(variants, v => v.question == question).Length;
        public FinalCheckItem Item(int index, bool wordingB) => finalCheck[(wordingB ? 3 : 0) + index];

        public static QuestionCatalog Load(string json, Func<string, QuestionCatalog> parser)
        {
            var catalog = parser(json);
            if (catalog.course == null) catalog.course = new CourseConfig();
            return catalog;
        }
    }
}
