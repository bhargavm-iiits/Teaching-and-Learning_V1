using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SpeedTimeGraphFree
{
    public class SpeedTimeFreeLogicTests
    {
        static QuestionCatalog Catalog() => QuestionCatalog.Load(File.ReadAllText("Assets/SpeedTimeGraphFree/Data/speed_time_free_questions.json"), s => JsonUtility.FromJson<QuestionCatalog>(s));

        [Test] public void FullSuite_AllAnswersFailuresEditingSelectionAndRouting() => StringAssert.StartsWith("PASS", LogicTestSuite.Run(Catalog()));

        [Test] public void DemoAnswerIs256MetresForAllThree()
        {
            var c = Catalog();
            foreach (var lane in c.demo.lanes) Assert.AreEqual(256, MotionMath.Distance(lane.solution), 1e-9);
        }

        [Test] public void AreaSlopeAndExactInverse()
        {
            var p = new[] { new GraphPoint(0, 0), new GraphPoint(4, 8), new GraphPoint(8, 8), new GraphPoint(12, 0) };
            Assert.AreEqual(64, MotionMath.Distance(p), 1e-9);
            Assert.AreEqual(2, MotionMath.Acceleration(p, 2), 1e-9);
            Assert.AreEqual(4, MotionMath.TimeAtDistance(p, 16), 1e-9);
            Assert.AreEqual(10, MotionMath.TimeAtDistance(p, 64 - 4), 1e-9);
        }

        [Test] public void Q1C_TrainCorrectedTo224()
        {
            var v = Catalog().Variant(1, 2);
            Assert.AreEqual(224, v.lanes[1].stop, 1e-9);
        }

        [Test] public void FinalCheckRoutes()
        {
            Assert.AreEqual(FinalRoute.TopicComplete, FinalCheckRouting.Route(new[] { true, true, true }));
            Assert.AreEqual(FinalRoute.RestartDemo, FinalCheckRouting.Route(new[] { false, false, false }));
            CollectionAssert.AreEqual(new[] { 2, 3 }, FinalCheckRouting.QuestionsToReplay(new[] { false, false, true }));
            CollectionAssert.AreEqual(new[] { 1 }, FinalCheckRouting.QuestionsToReplay(new[] { true, true, false }));
        }
    }
}
