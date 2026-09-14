using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroReturn.Editor
{
    public static class ZeroReturnBuilder
    {
        public const string ScenePath="Assets/ZeroReturn/Scenes/ZeroReturn.unity";
        const string Data="Assets/ZeroReturn/Data/";
        [MenuItem("Tools/Zero Return/Build and Validate %#k")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Exit Play Mode before rebuilding Zero Return.");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Data);Directory.CreateDirectory("Assets/ZeroReturn/Scenes");
            if(File.Exists(ScenePath)){Directory.CreateDirectory("Backups/ZeroReturn");File.Copy(ScenePath,"Backups/ZeroReturn/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);}
            var tuning=Asset<TuningProfile>("Tuning");var dressing=Asset<RailDressing>("RailDressing");
            if(dressing.surface==null)
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(Data+"Surface.mat");
                if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Data+"Surface.mat");}
                dressing.surface=mat;EditorUtility.SetDirty(dressing);
            }
            var levels=new LevelTemplate[3];
            string[] objectives={"Fetch two spheres and match their coordinate stamps to build a line.","Fetch three spheres. Commit to a heading, then match the stamps to build a triangle.","Fetch four spheres. Pair equal separations to build a square; its required side length is published."};
            string[] lessons={"The segment's length, your path length, and your displacement are three different quantities.","Direction changes the sign of displacement. Every metre still adds to path length.","Separation is |a - b|. Equal distances can occur on opposite sides of the origin."};
            float[][] coords={new float[]{-4,7},new float[]{-6,2,9},new float[]{-8,-4,2,6}};
            for(int i=0;i<3;i++)
            {
                levels[i]=Asset<LevelTemplate>("Level"+(i+1));levels[i].levelIndex=i;levels[i].shape=(ShapeType)i;
                levels[i].modifier=i==0?Modifier.None:i==1?Modifier.HeadingGate:Modifier.BlindSockets;
                levels[i].exampleCoordinates=coords[i];levels[i].exampleSide=4;levels[i].objective=objectives[i];levels[i].lesson=lessons[i];EditorUtility.SetDirty(levels[i]);
            }
            var bank=Asset<QuestionBank>("QuestionBank");bank.questions=Questions();EditorUtility.SetDirty(bank);
            var concept=Asset<ConceptDefinition>("Concept01");concept.levels=levels;concept.questions=bank;EditorUtility.SetDirty(concept);
            AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var systems=new GameObject("ZeroReturn Systems");var flow=systems.AddComponent<GameFlowController>();flow.concept=concept;flow.tuning=tuning;flow.dressing=dressing;
            EditorSceneManager.SaveScene(scene,ScenePath);
            var rest=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).Select(s=>new EditorBuildSettingsScene(s.path,false));
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(rest).ToArray();
            Selection.activeGameObject=systems;ZeroReturnValidation.Run();
            Debug.Log("Zero Return built. Enter Play Mode to run the complete game.");
        }
        static T Asset<T>(string name) where T:ScriptableObject
        {
            string path=Data+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset;
        }
        static QuestionDef Q(string id,string prompt,string[] options,int correct,string explanation)=>new QuestionDef{id=id,pool=id.Substring(0,1),prompt=prompt,options=options,correct=correct,explanation=explanation};
        static QuestionDef[] Questions()=>new[]{
            Q("S1","You travelled {path} m in Level 1 and finished at the dock. What was your total displacement?",new[]{"{path} m","0 m","-{path} m","{sep} m"},1,"Your final and initial positions were both zero. Displacement is 0 - 0 = 0 m."),
            Q("S2","Fetching the sphere at x = -{a} m and returning home, what was the path length?",new[]{"{a} m","{twoa} m","-{a} m","0 m"},1,"Count both legs: {a} + {a} = {twoa} m."),
            Q("S3","Can path length ever be less than the magnitude of displacement?",new[]{"No, never","Yes, on the return leg","Yes, when displacement is negative","Only at the origin"},0,"The route cannot be shorter than the separation of its endpoints: |displacement| <= path length."),
            Q("S4","Your odometer reads {p} m. After one more trip to x = -{a} m and back, what does it read?",new[]{"{p} m","{pPlusA} m","{pPlus2A} m","{pMinus2A} m"},2,"The odometer adds {twoa} m for both legs. It never subtracts the return journey."),
            Q("P1","A sphere sits at x = -{a} m. Which statement is true?",new[]{"It is {a} m from zero, on the negative side","It is -{a} m from zero","Its position is +{a} m","It is closer to zero than a sphere at +{smaller} m"},0,"Position is signed. Distance from the origin is its non-negative magnitude."),
            Q("P2","If the reference dock moved from x = 0 to x = +3 m, a sphere at the old x = -5 m would have which position relative to it?",new[]{"-5 m","-8 m","+8 m","-2 m"},1,"New relative position = -5 - 3 = -8 m."),
            Q("P3","Which quantity needs a chosen reference point to specify it?",new[]{"Position","Path length","Speed","Mass"},0,"Position describes where something is relative to an origin."),
            Q("P4","Two spheres sit at x = -{a} m and x = +{b} m. How far apart are they?",new[]{"{difference} m","{sum} m","{b} m","{a} m"},1,"Across zero, separation is |{b} - (-{a})| = {sum} m."),
            Q("D1","The bot is docked at zero and must fetch x = +{b} m. Which heading should you set?",new[]{"Positive / right","Negative / left","Either","It depends on the odometer"},0,"A positive coordinate lies in the positive direction from zero."),
            Q("D2","The bot moves from x = +6 m to x = +2 m. Its displacement is...",new[]{"+4 m","-4 m","4 m, with no direction","-8 m"},1,"Displacement = final - initial = 2 - 6 = -4 m."),
            Q("D3","Which of these can be negative?",new[]{"Displacement","Path length","The odometer reading","Distance from the origin"},0,"Displacement carries direction through its sign; the other quantities are magnitudes."),
            Q("D4","You set a negative heading and moved 5 m in that direction. What was that leg's displacement?",new[]{"+5 m","-5 m","0 m","10 m"},1,"A 5 m move in the negative direction has displacement -5 m.")
        };
    }
}
