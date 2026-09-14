using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZeroReturn.Editor
{
    public static class ZeroReturnValidation
    {
        static void Assert(bool condition,string message){if(!condition)throw new Exception("Zero Return validation: "+message);}
        [MenuItem("Tools/Zero Return/Validate Mathematics and Rules")]
        public static void Run()
        {
            var concept=AssetDatabase.LoadAssetAtPath<ConceptDefinition>("Assets/ZeroReturn/Data/Concept01.asset");
            Assert(concept!=null,"Concept asset missing.");
            float[] expected={22,34,40};
            for(int level=0;level<3;level++)
            {
                var initial=VariantGenerator.Build(concept.levels[level],123,0,null);
                Assert(initial.ExpectedPath==expected[level],"Example path mismatch.");
                var previous=initial;
                for(int i=1;i<=10000;i++)
                {
                    int seed=VariantGenerator.Seed(concept.id,"test-profile",i,level);
                    var next=VariantGenerator.Build(concept.levels[level],seed,i,previous);
                    Assert(VariantGenerator.Valid(next),"Invalid seed "+seed);
                    Assert(VariantGenerator.SufficientlyDifferent(next,previous),"Retry not different enough.");
                    var repeat=VariantGenerator.Build(concept.levels[level],seed,i,previous);
                    Assert(next.coordinates.SequenceEqual(repeat.coordinates),"Seed is not reproducible.");previous=next;
                }
            }
            var line=VariantGenerator.Build(concept.levels[0],1,0,null);var puzzle=new AssemblyModel(line);
            Assert(puzzle.CanLaunch(0,0,0)==null,"L1 must allow free launch.");Assert(puzzle.Grab(0),"First pickup refused.");
            Assert(!puzzle.Grab(1),"Two spheres fit in claw.");Assert(puzzle.CanLaunch(1,0,0)!=null && puzzle.violations==0,"R1/R3 should be free.");
            Assert(puzzle.Place(1,out _)!=null && puzzle.carry==0 && puzzle.violations==1,"Wrong socket must retain carry and cost one pip.");
            Assert(puzzle.Place(0,out _)==null,"Correct socket rejected.");puzzle.Grab(1);puzzle.Place(1,out _);Assert(puzzle.Complete,"Line should complete.");
            var tri=new AssemblyModel(VariantGenerator.Build(concept.levels[1],1,0,null));
            Assert(tri.CanLaunch(0,1,0)!=null && tri.violations==1,"Wrong heading not charged.");
            Assert(tri.CanLaunch(0,-1,0)==null,"Correct heading rejected.");
            var square=new AssemblyModel(VariantGenerator.Build(concept.levels[2],1,0,null));
            square.Grab(0);square.Place(0,out _);square.Grab(2);square.Place(1,out int[] eject);
            Assert(eject.Length==2 && square.carry<0 && !square.collected[0] && !square.collected[2],"Bad edge must eject both spheres.");
            for(int i=0;i<4;i++){square.Grab(i);square.Place(i,out _);}Assert(square.Complete,"Valid square rejected.");
            Assert(!VariantGenerator.SquareSeedIsUnique(new float[]{0,2,2,4},2),"Ambiguous square accepted.");
            var railObject=new GameObject("Motion Test Rail");var botObject=new GameObject("Motion Test Bot");
            try
            {
                var rail=railObject.AddComponent<NumberLineController>();rail.transform.position=new Vector3(9,2,-7);rail.transform.rotation=Quaternion.Euler(0,36,0);rail.WorldScale=2;
                var bot=botObject.AddComponent<CollectorBot>();bot.rail=rail;bot.ResetAtDock();var tracker=botObject.AddComponent<MotionTracker>();tracker.bot=bot;tracker.ResetRun();tracker.BeginTrip();
                foreach(float x in new[]{-1f,-4,-6,-3,0}){bot.transform.position=rail.ToWorld(x);tracker.Sample();}
                Assert(Mathf.Abs(tracker.PathLength-12)<.001f && Mathf.Abs(tracker.TripDisplacement)<.001f,"Round trip integral incorrect.");
                bot.transform.position=rail.ToWorld(5);tracker.Sample();Assert(Mathf.Abs(tracker.PathLength-17)<.001f,"Teleport not measured from actual transform.");
                tracker.ResetRun();Assert(tracker.PathLength==0 && tracker.RunDisplacement==0,"Run reset creates phantom travel.");
            }
            finally{UnityEngine.Object.DestroyImmediate(botObject);UnityEngine.Object.DestroyImmediate(railObject);}
            var seen=new HashSet<string>();var rng=new System.Random(12);
            for(int attempt=0;attempt<30;attempt++)
            {
                var draw=concept.questions.Draw(new LevelResult{instance=line,path=22,seconds=20},rng,seen,attempt);
                Assert(draw.Length==3 && draw.Select(q=>q.pool).Distinct().Count()==3,"Quiz pool coverage wrong.");
                Assert(draw.All(q=>q.options.Length==4 && q.correct>=0 && q.correct<4 && !q.prompt.Contains("{")),"Quiz templating failed.");
                Assert(seen.Count==(attempt+1)*3,"Quiz ID repeated.");
            }
            Directory.CreateDirectory("Logs/ZeroReturn");
            File.WriteAllText("Logs/ZeroReturn/validation.txt","PASS\n30,000 deterministic retry variants (10,000 per level), four-axis difference checks, square uniqueness, sample paths 22/34/40 m, all puzzle rules, transform-based motion including teleport and reset, 90 non-repeating quiz instances.\n"+DateTime.Now);
            Debug.Log("Zero Return mathematical and rule validation passed.");
        }
    }
}
