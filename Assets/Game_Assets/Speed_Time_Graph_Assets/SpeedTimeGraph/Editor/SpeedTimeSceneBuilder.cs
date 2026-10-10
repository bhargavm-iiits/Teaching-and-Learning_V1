using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SpeedTimeGraphLesson;
using Object=UnityEngine.Object;

namespace SpeedTimeGraphLesson.Editor
{
    public static class SpeedTimeSceneBuilder
    {
        const string Folder="Assets/SpeedTimeGraph";
        static Transform root;
        static Material grass,road,white,metal,cyan,red,green,wood,water,rock;
        static Camera camera;
        static Sprite rounded;
        static TMP_FontAsset font;
        static readonly Color Navy=new Color(.018f,.075f,.20f),Cyan=new Color(.08f,.82f,1f);
        [MenuItem("Tools/Speed Time Graph/Build Reference Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder+"/Generated");Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Work/SpeedTimeGraph");
            if(File.Exists("Assets/Scenes/Speed_Time_Graph.unity"))
            {
                Directory.CreateDirectory("Backups/SpeedTimeGraph");
                File.Copy("Assets/Scenes/Speed_Time_Graph.unity","Backups/SpeedTimeGraph/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
            }
            if(!Application.isBatchMode&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path)&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                root=new GameObject("TripleVehicleArena").transform;
                grass=Mat("Meadow",new Color(.36f,.52f,.085f));road=Mat("Asphalt",new Color(.17f,.20f,.26f));white=Mat("White",new Color(.93f,.97f,1));metal=Mat("Guardrail",new Color(.42f,.5f,.57f));cyan=Mat("Hologram Glow",Cyan,true);red=Mat("Stop Red",new Color(.9f,.025f,.08f),true);green=Mat("Start Green",new Color(.025f,.58f,.13f));wood=Mat("Timber",new Color(.3f,.16f,.075f));water=Mat("Lake",new Color(.025f,.58f,.87f));rock=Mat("Rock",new Color(.48f,.46f,.43f));
                SetupLighting();
                World();
                var controller=root.gameObject.AddComponent<SpeedTimeChallenge>();controller.worldScale=.95f;
                controller.questionData=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/questions_all_3_vehicles.json");
                BuildLanes(controller);
                Interface(controller);
                controller.SendMessage("Preview");
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/Speed_Time_Graph.unity");
                Validate(controller);
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/Speed_Time_Graph.unity");
                Capture();
                File.WriteAllText("Work/SpeedTimeGraph/build-success.txt","Scene saved and verified at "+DateTime.Now+"\n");
                Debug.Log("Speed Time Graph reference arena built successfully.");
            }
            catch(Exception e) { File.WriteAllText("Work/SpeedTimeGraph/build-error.txt",e.ToString());Debug.LogException(e);throw; }
        }
        public static void BatchBuild()
        {
            Build();
            int frames=0;
            void Tick()
            {
                if(++frames<30)return;
                EditorApplication.update-=Tick;Capture();EditorApplication.Exit(0);
            }
            EditorApplication.update+=Tick;
        }
        static Material Mat(string name,Color c,bool emissive=false)
        {
            string path=Folder+"/Generated/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path); }
            m.color=c;m.SetFloat("_Smoothness",.16f);
            if(emissive) { m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*2.5f); }
            return m;
        }
        static GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent?parent:root,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static GameObject Sphere(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent?parent:root,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static void SetupLighting()
        {
            var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Folder+"/Generated/ArenaRenderer.asset");
            if(!rendererData) { rendererData=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(rendererData,Folder+"/Generated/ArenaRenderer.asset"); }
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder+"/Generated/ArenaPipeline.asset");
            if(!pipeline) { pipeline=UniversalRenderPipelineAsset.Create(rendererData);AssetDatabase.CreateAsset(pipeline,Folder+"/Generated/ArenaPipeline.asset"); }
            // The scene materials use the project's URP pipeline. Only the isolated build project needs a pipeline assigned.
            if(GraphicsSettings.defaultRenderPipeline==null) { GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline; }
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.66f,.78f,.92f);RenderSettings.ambientIntensity=1.1f;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.43f,.76f,.97f);RenderSettings.fogStartDistance=70;RenderSettings.fogEndDistance=150;RenderSettings.skybox=null;
            var sun=new GameObject("Sun • warm afternoon").AddComponent<Light>();sun.transform.SetParent(root);sun.type=LightType.Directional;sun.intensity=1.65f;sun.color=new Color(1,.96f,.84f);sun.transform.rotation=Quaternion.Euler(42,-35,0);sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;
            camera=new GameObject("Reference Composition Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(root);camera.transform.position=new Vector3(11,27,-40);camera.transform.LookAt(new Vector3(5,0,3));camera.orthographic=true;camera.orthographicSize=19.5f;camera.nearClipPlane=.1f;camera.farClipPlane=250;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.3f,.72f,.98f);
            camera.gameObject.AddComponent<AudioListener>();var cameraData=camera.gameObject.AddComponent<UniversalAdditionalCameraData>();cameraData.renderPostProcessing=true;
            var volume=new GameObject("Soft hologram bloom").AddComponent<Volume>();volume.transform.SetParent(root);volume.isGlobal=true;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder+"/Generated/ArenaVolume.asset");if(!profile) { profile=ScriptableObject.CreateInstance<VolumeProfile>();var bloom=profile.Add<Bloom>();bloom.intensity.Override(.22f);bloom.threshold.Override(1.2f);AssetDatabase.CreateAsset(profile,Folder+"/Generated/ArenaVolume.asset");AssetDatabase.AddObjectToAsset(bloom,profile); }volume.sharedProfile=profile;
        }
        static void World()
        {
            Box("Landscape",new Vector3(8,-1.2f,0),new Vector3(115,2,60),grass);
            Box("Blue lake",new Vector3(32,-.12f,22),new Vector3(65,.13f,10),water);
            Box("River below railway",new Vector3(32,-.12f,-5.7f),new Vector3(35,.16f,4),water);
            var rng=new System.Random(423);
            for(int i=0;i<32;i++)
            {
                float x=-45+i*3.8f,z=32+(float)rng.NextDouble()*3,h=3+(float)rng.NextDouble()*4;
                Cone("Distant mountain "+i,new Vector3(x,0,z),new Vector3(8,h,3),Mat("Mountain "+i,new Color(.35f+i%3*.055f,.51f+i%3*.04f,.64f+i%3*.04f)),5);
                if(i%3==0) Cone("Snowcap",new Vector3(x,h*.74f,z),new Vector3(2.1f,h*.28f,.8f),white,5);
            }
            for(int i=0;i<200;i++)
            {
                float x=-35+(float)rng.NextDouble()*90,z=-25+(float)rng.NextDouble()*70;
                if((x>-3&&x<48)&&(Mathf.Abs(z-12)<4||Mathf.Abs(z)<3||Mathf.Abs(z+12)<4)) continue;
                if(z>25||(z>16&&x>5))continue;
                string path="Assets/SimpleNaturePack/Prefabs/"+(i%4==0?"Rock_0"+(i%5+1):"Tree_0"+(i%2+1))+".prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab)
                {
                    var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);g.name="Scenery "+i;g.transform.position=new Vector3(x,0,z);g.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,0);g.transform.localScale=Vector3.one*(.7f+(float)rng.NextDouble()*.8f);ConvertMaterials(g,"Nature");
                }
                else Tree(new Vector3(x,0,z),.7f+(float)rng.NextDouble());
            }
            // Broken rock outcrops and grassy shoulders frame the parallel tracks.
            for(int i=0;i<55;i++)
            {
                float x=-10+(float)rng.NextDouble()*60,z=new[]{5.4f,-5.4f,17f,-18f}[i%4];
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleNaturePack/Prefabs/Rock_0"+(i%5+1)+".prefab");
                if(prefab) { var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);g.name="Rocky shoulder "+i;g.transform.position=new Vector3(x,-.3f,z+(float)rng.NextDouble()*1.5f);g.transform.localScale=new Vector3(2.2f,1.2f,1.5f)*(1+(float)rng.NextDouble());ConvertMaterials(g,"Nature"); }
                if(i%3==0)Cone("Grassy embankment",new Vector3(x,-.3f,z),new Vector3(4,1.6f,2),grass,7);
            }
            for(int i=0;i<14;i++)
            {
                float x=-30+i*6;var cloud=Mat("Cloud",new Color(.9f,.97f,1));Sphere("Cloud",new Vector3(x,18+i%3*2,45),new Vector3(5,1.1f,2),cloud);Sphere("Cloud crest",new Vector3(x-.4f,19+i%3*2,45),new Vector3(2.4f,2.2f,2),cloud);
            }
            // A coastal lighthouse anchors the horizon, as in the supplied composition.
            var lighthouse=new GameObject("Coastal lighthouse").transform;lighthouse.SetParent(root);lighthouse.position=new Vector3(9,0,19);
            Box("White tower",new Vector3(0,1.8f,0),new Vector3(1.2f,3.6f,1.2f),white,lighthouse);Box("Red band",new Vector3(0,3.1f,0),new Vector3(1.3f,.3f,1.3f),red,lighthouse);Box("Lantern",new Vector3(0,3.85f,0),new Vector3(1.1f,.6f,1.1f),cyan,lighthouse);Cone("Red roof",new Vector3(9,4.1f,19),new Vector3(1,.7f,1),red,8);
        }
        static void Tree(Vector3 pos,float s)
        {
            Box("Tree trunk",pos+Vector3.up*s,new Vector3(.3f,2*s,.3f),wood);
            Cone("Faceted pine",pos+Vector3.up*s,new Vector3(1.4f*s,3*s,1.4f*s),Mat("Pine",new Color(.19f,.43f,.09f)),7);
        }
        static GameObject Cone(string name,Vector3 pos,Vector3 scale,Material mat,int sides)
        {
            var mesh=new Mesh();var vs=new Vector3[sides*6];var triangles=new int[vs.Length];
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;int n=i*6;
                vs[n]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));vs[n+1]=Vector3.up;vs[n+2]=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));vs[n+3]=vs[n];vs[n+4]=vs[n+2];vs[n+5]=Vector3.zero;
                for(int k=0;k<6;k++)triangles[n+k]=n+k;
            }
            mesh.vertices=vs;mesh.triangles=triangles;mesh.RecalculateNormals();
            string path=Folder+"/Generated/Cone"+sides+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!saved) { AssetDatabase.CreateAsset(mesh,path);saved=mesh; }else Object.DestroyImmediate(mesh);
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<MeshFilter>().sharedMesh=saved;g.GetComponent<MeshRenderer>().sharedMaterial=mat;return g;
        }
        static void BuildLanes(SpeedTimeChallenge c)
        {
            c.vehicles=new Transform[3];c.targets=new Transform[3];c.origins=new Vector3[3];c.ghosts=new Transform[9];c.speedGates=new Transform[3];c.slowZones=new Transform[3];
            string[] names={"Car","Train","Motorcycle"};float[] z={12,0,-12};
            var ghost=Mat("Transparent cyan prediction",new Color(.4f,.72f,.95f,.17f));ghost.SetFloat("_Surface",1);ghost.SetFloat("_Blend",0);ghost.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);ghost.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);ghost.SetFloat("_ZWrite",0);ghost.SetFloat("_SpecularHighlights",0);ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ghost.renderQueue=3000;
            for(int i=0;i<3;i++)
            {
                var lane=new GameObject(names[i]+" Lane").transform;lane.SetParent(root);
                float width=i==1?3.6f:5.2f;
                Box(names[i]+" surface",new Vector3(20,.25f,z[i]),new Vector3(58,.45f,width),i==1?rock:road,lane);
                if(i!=1)
                {
                    for(int j=-4;j<51;j+=3) Box("Road center dash",new Vector3(j,.49f,z[i]),new Vector3(1.45f,.025f,.1f),white,lane);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Box("Road edge",new Vector3(20,.49f,z[i]+side*2.45f),new Vector3(58,.02f,.07f),white,lane);
                        Box("Continuous guardrail",new Vector3(20,1.1f,z[i]+side*2.9f),new Vector3(58,.18f,.12f),metal,lane);
                        for(int j=-8;j<50;j+=3)Box("Guardrail post",new Vector3(j,.8f,z[i]+side*2.9f),new Vector3(.12f,.85f,.12f),metal,lane);
                    }
                }
                else
                {
                    for(int j=-8;j<50;j++)Box("Railway sleeper",new Vector3(j,.55f,z[i]),new Vector3(.21f,.14f,2.9f),wood,lane);
                    for(int side=-1;side<=1;side+=2)Box("Steel rail",new Vector3(20,.67f,side*.88f),new Vector3(58,.16f,.12f),metal,lane);
                    // Include the supplied railway model as the station approach, clear of the playable rails.
                    var track=Model("Track",14,Quaternion.Euler(0,90,0),lane);track.name="Supplied track • tunnel approach";track.transform.position=new Vector3(-14,.46f,0);
                    Tunnel(new Vector3(-1,0,0));
                }
                float originX=new[]{2f,8f,5.5f}[i];
                Marker("START",new Vector3(originX-2,.5f,z[i]),green,width,lane);
                c.targets[i]=Marker("STOP",new Vector3(originX+28.5f,.5f,z[i]),red,width,lane);
                c.speedGates[i]=Box("Speed Gate • 6 m/s",new Vector3(14.25f,.55f,z[i]),new Vector3(.25f,.04f,width),green,lane).transform;c.speedGates[i].gameObject.SetActive(false);
                c.slowZones[i]=Box("Slow Zone • maximum 4 m/s",new Vector3(32.3f,.55f,z[i]),new Vector3(7.6f,.04f,width),Mat("Slow Zone Amber",new Color(1,.65f,.06f)),lane).transform;c.slowZones[i].gameObject.SetActive(false);
                var vehicle=Model(names[i],i==0?5.8f:i==1?12.5f:3.8f,Quaternion.Euler(0,90,0),lane);vehicle.name=names[i]+" • shared graph follower";
                vehicle.transform.position=new Vector3(originX,.72f,z[i]);c.vehicles[i]=vehicle.transform;c.origins[i]=vehicle.transform.position;
                if(i==2) Rider(vehicle.transform);
                var ghostMesh=GhostMesh(vehicle,names[i]);
                for(int j=0;j<3;j++)
                {
                    var g=new GameObject(names[i]+" Ghost "+(j+1),typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(lane);g.transform.position=c.origins[i]+Vector3.right*(8+j*8);g.GetComponent<MeshFilter>().sharedMesh=ghostMesh;
                    var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=ghost;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                    c.ghosts[i*3+j]=g.transform;
                }
            }
        }
        static GameObject Model(string name,float length,Quaternion rotation,Transform parent)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Models/"+name+"/"+name+".obj");
            if(!asset)throw new Exception("Missing imported supplied model: "+name);
            var g=new GameObject(name);g.transform.SetParent(parent,false);var model=Object.Instantiate(asset,g.transform);model.name="Supplied "+name+" geometry";model.transform.localRotation=rotation;
            var renderers=model.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            float scale=length/Mathf.Max(b.size.x,b.size.z);model.transform.localScale=Vector3.one*scale;model.transform.localPosition=new Vector3(-b.center.x*scale,-b.min.y*scale,-b.center.z*scale);
            ConvertMaterials(model,name);return g;
        }
        static void ConvertMaterials(GameObject g,string prefix)
        {
            foreach(var r in g.GetComponentsInChildren<Renderer>())
            {
                var mats=r.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {
                    var source=mats[i];if(!source)continue;
                    string path=Folder+"/Generated/"+prefix+"_"+source.name.Replace('/','_')+".mat";
                    var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=source.HasProperty("_Color")?source.color:Color.white;var tex=source.mainTexture;if(tex)m.SetTexture("_BaseMap",tex);m.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(m,path); }
                    if(prefix=="Car"&&source.name=="material_20") { m.SetTexture("_BaseMap",null);m.color=new Color(.015f,.17f,.82f); }
                    if(prefix=="Car"&&source.name=="material_13") { m.color=new Color(.015f,.055f,.11f); }
                    if(prefix=="Motorcycle"&&source.name=="material_19") { m.SetTexture("_BaseMap",null);m.color=new Color(.95f,.26f,.025f); }
                    if(prefix=="Train")
                    {
                        if(new[]{"material_0","material_9","material_19","material_20"}.Contains(source.name))m.color=new Color(.78f,.018f,.045f);
                        if(new[]{"material_1","material_3","material_13","material_16","material_25"}.Contains(source.name))m.color=new Color(.78f,.82f,.87f);
                        if(new[]{"material_7","material_8","material_17"}.Contains(source.name)) { m.SetTexture("_BaseMap",null);m.color=new Color(.025f,.075f,.13f); }
                    }
                    EditorUtility.SetDirty(m);mats[i]=m;
                }
                r.sharedMaterials=mats;
            }
        }
        static Mesh GhostMesh(GameObject vehicle,string name)
        {
            var parts=vehicle.GetComponentsInChildren<MeshFilter>().SelectMany(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Select(s=>new CombineInstance { mesh=f.sharedMesh,subMeshIndex=s,transform=vehicle.transform.worldToLocalMatrix*f.transform.localToWorldMatrix })).ToArray();
            var mesh=new Mesh { name=name+" prediction silhouette",indexFormat=IndexFormat.UInt32 };mesh.CombineMeshes(parts,true,true);mesh.RecalculateBounds();string path=Folder+"/Generated/"+name+"Ghost.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!saved) { AssetDatabase.CreateAsset(mesh,path);return mesh; }EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;
        }
        static void Rider(Transform parent)
        {
            var suit=Mat("Rider orange",new Color(1,.32f,.025f));var black=Mat("Rider visor",new Color(.015f,.025f,.035f));
            Sphere("Helmet",new Vector3(.05f,2.55f,0),new Vector3(.56f,.6f,.56f),suit,parent);
            Sphere("Visor",new Vector3(.28f,2.57f,-.05f),new Vector3(.14f,.26f,.48f),black,parent);
            var torso=Sphere("Rider jacket",new Vector3(-.22f,1.95f,0),new Vector3(.8f,.85f,.54f),suit,parent);torso.transform.localRotation=Quaternion.Euler(0,0,-27);
            for(int s=-1;s<=1;s+=2) { var arm=Box("Rider arm",new Vector3(.25f,1.91f,s*.34f),new Vector3(.8f,.18f,.2f),suit,parent);arm.transform.localRotation=Quaternion.Euler(0,0,-28);var leg=Box("Rider leg",new Vector3(-.25f,1.2f,s*.30f),new Vector3(.85f,.22f,.24f),black,parent);leg.transform.localRotation=Quaternion.Euler(0,0,38); }
        }
        static void Tunnel(Vector3 pos)
        {
            var entrance=new GameObject("Rock tunnel entrance").transform;entrance.SetParent(root);entrance.position=pos;entrance.rotation=Quaternion.Euler(0,70,0);
            Box("Tunnel darkness",new Vector3(-1.5f,2.5f,0),new Vector3(.3f,5,4.4f),Mat("Tunnel darkness",new Color(.025f,.035f,.045f)),entrance);
            for(int i=0;i<9;i++) { float a=i*Mathf.PI/8;var g=Box("Stone tunnel arch",new Vector3(0,1.2f+Mathf.Sin(a)*2.6f,Mathf.Cos(a)*2.6f),new Vector3(1.5f,1.1f,1.1f),rock,entrance);g.transform.localRotation=Quaternion.Euler(a*Mathf.Rad2Deg,0,0); }
        }
        static Transform Marker(string text,Vector3 pos,Material mat,float width,Transform parent)
        {
            var marker=new GameObject(text+" marker").transform;marker.SetParent(parent);marker.position=pos;
            Box(text+" line",Vector3.zero,new Vector3(.3f,.05f,width),text=="STOP"?red:white,marker);
            float signZ=width*.5f+1.1f;
            for(int s=-1;s<=1;s+=2)Box("Sign post",new Vector3(s*1.8f,1.4f,signZ),new Vector3(.13f,2.8f,.13f),metal,marker);
            Box("Sign board",new Vector3(0,2.75f,signZ),new Vector3(3.8f,1.3f,.16f),mat,marker);
            var label=new GameObject(text+" lettering",typeof(TextMeshPro)).GetComponent<TextMeshPro>();label.transform.SetParent(marker);label.transform.localPosition=new Vector3(0,2.75f,signZ-.10f);label.transform.localRotation=Quaternion.identity;label.text=text;label.fontSize=7;label.alignment=TextAlignmentOptions.Center;label.fontStyle=FontStyles.Bold;label.rectTransform.sizeDelta=new Vector2(3.4f,1.1f);label.color=Color.white;
            if(text=="STOP")for(int side=-1;side<=1;side+=2)for(int row=0;row<4;row++)for(int col=0;col<2;col++)Box("Checkered finish",new Vector3(side*(1.9f+col*.22f),2.3f+row*.29f,signZ-.1f),new Vector3(.22f,.29f,.05f),(row+col)%2==0?white:road,marker);
            return marker;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(pos.x,-pos.y);r.sizeDelta=size;return r;
        }
        static RectTransform Panel(string name,Transform parent,Vector2 pos,Vector2 size,Color color,bool border=true)
        {
            var outer=Rect(name,parent,pos,size);var image=outer.gameObject.AddComponent<Image>();image.color=border?Cyan:color;image.sprite=rounded;image.type=Image.Type.Sliced;image.raycastTarget=false;
            if(border) { var glow=outer.gameObject.AddComponent<Outline>();glow.effectColor=new Color(Cyan.r,Cyan.g,Cyan.b,.17f);glow.effectDistance=new Vector2(4,-4);glow.useGraphicAlpha=false; }
            if(border) { var inner=Rect("Inset",outer,new Vector2(2,2),size-new Vector2(4,4));var fill=inner.gameObject.AddComponent<Image>();fill.sprite=rounded;fill.type=Image.Type.Sliced;fill.color=color;fill.raycastTarget=false; }
            return outer;
        }
        static TMP_Text Text(string name,Transform parent,string value,Vector2 pos,Vector2 size,float sizePt,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
        {
            var r=Rect(name,parent,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();if(font)t.font=font;t.text=value;t.fontSize=sizePt;t.fontStyle=FontStyles.Bold;t.color=color;t.alignment=alignment;t.raycastTarget=false;return t;
        }
        static Button Button(string name,Transform parent,string label,Vector2 pos,Vector2 size,Color color)
        {
            var r=Panel(name,parent,pos,size,color);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();r.GetComponent<Image>().raycastTarget=true;Text("Label",r,label,Vector2.zero,size,18,Color.white);return b;
        }
        static void Interface(SpeedTimeChallenge c)
        {
            rounded=RoundedSprite();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var canvas=new GameObject("Shared holographic interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.transform.SetParent(root);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=10;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1672,941);scaler.matchWidthOrHeight=.5f;
            var header=Panel("Challenge title",canvas.transform,new Vector2(360,12),new Vector2(950,90),Navy);
            Text("Title",header,"<color=#22C9FF>Car</color>, <color=#FFB43F>Train</color> & <color=#57ED83>Motorcycle</color> Challenge",new Vector2(18,3),new Vector2(914,78),47,Color.white);
            var subtitle=Panel("Subtitle",canvas.transform,new Vector2(560,91),new Vector2(550,49),new Color(.64f,.89f,1));Text("Subtitle text",subtitle,"The graph controls every ride.",Vector2.zero,new Vector2(550,49),31,Navy);
            var steps=Panel("Three steps",canvas.transform,new Vector2(1333,59),new Vector2(321,144),new Color(.87f,.96f,1));
            string[] instructions={"Edit the graph","Watch all vehicles respond","Stop at the correct point"};string[] colors={"#078CFF","#FF9500","#00B24E"};
            for(int i=0;i<3;i++) Text("Step "+i,steps,$"<color={colors[i]}>{i+1}</color>  {instructions[i]}",new Vector2(12,9+i*41),new Vector2(301,38),19,Navy,TextAlignmentOptions.MidlineLeft);
            var panel=Panel("Speed-Time Graph Controller",canvas.transform,new Vector2(22,171),new Vector2(535,657),new Color(.02f,.115f,.245f,.97f));
            var panelTitle=Panel("Graph heading",panel,new Vector2(24,23),new Vector2(487,63),new Color(.025f,.30f,.65f));Text("Graph heading text",panelTitle,"Speed-Time Graph Controller",Vector2.zero,new Vector2(487,63),31,Color.white);
            var graphBg=Panel("Plot background",panel,new Vector2(24,102),new Vector2(487,291),new Color(.025f,.15f,.28f),false);
            Text("Speed axis",graphBg,"Speed\n(m/s)",new Vector2(5,1),new Vector2(81,63),23,Color.white);
            Text("Time axis",graphBg,"Time (s)",new Vector2(145,256),new Vector2(270,32),23,Color.white);
            var plotRect=Rect("Editable speed-time graph",graphBg,new Vector2(87,20),new Vector2(364,221));var plot=plotRect.gameObject.AddComponent<SpeedTimeGraphPlot>();plot.controller=c;plot.raycastTarget=true;c.plot=plot;c.uiCamera=camera;
            var tip=Panel("Connection explanation",panel,new Vector2(24,407),new Vector2(487,50),new Color(.56f,.89f,1),false);Text("Tip",tip,"Change the graph > vehicle motion changes.",Vector2.zero,new Vector2(487,50),20,Navy);
            var legend=Panel("Graph slope legend",panel,new Vector2(24,473),new Vector2(487,156),new Color(.018f,.12f,.25f));
            Text("Rising legend",legend,"Up slope = speed up",new Vector2(103,13),new Vector2(369,39),24,Color.white,TextAlignmentOptions.MidlineLeft);
            Text("Flat legend",legend,"Flat = constant speed",new Vector2(103,55),new Vector2(369,39),24,Color.white,TextAlignmentOptions.MidlineLeft);
            Text("Falling legend",legend,"Down slope = slow down",new Vector2(103,97),new Vector2(369,39),24,Color.white,TextAlignmentOptions.MidlineLeft);
            UILine(legend,new Vector2(28,45),new Vector2(89,18),new Color(1,.6f,.1f),5);
            UILine(legend,new Vector2(28,76),new Vector2(89,73),new Color(1,.9f,.1f),5);
            UILine(legend,new Vector2(28,111),new Vector2(89,140),new Color(.3f,1,.4f),5);
            for(int i=0;i<3;i++)UIConnection(canvas.transform,new Vector2(560,315+i*126),new Vector2(730,330+i*156));
            var slogan=Panel("Same graph different vehicles",canvas.transform,new Vector2(781,810),new Vector2(600,62),Navy);Text("Slogan",slogan,"<color=#62DCFF>Same graph</color>, <color=#FFE140>different vehicles.</color>",Vector2.zero,new Vector2(600,62),34,Color.white);
            var footer=Panel("Challenge controls",canvas.transform,new Vector2(22,844),new Vector2(725,84),Navy);
            c.questionLabel=Text("Question",footer,"QUESTION 1 / 3  •  GRAPH SHAPE",new Vector2(12,5),new Vector2(699,24),17,Cyan,TextAlignmentOptions.MidlineLeft);
            c.playButton=Button("Play",footer,"PLAY",new Vector2(12,35),new Vector2(110,37),new Color(.025f,.52f,.28f));
            c.resetButton=Button("Reset",footer,"RESET / RETRY",new Vector2(132,35),new Vector2(158,37),new Color(.07f,.29f,.52f));
            c.nextButton=Button("Next question",footer,"NEXT",new Vector2(302,35),new Vector2(83,37),new Color(.07f,.29f,.52f));
            c.questionButtons=new Button[3];for(int i=0;i<3;i++)c.questionButtons[i]=Button("Question "+(i+1),footer,"Q"+(i+1),new Vector2(398+i*61,35),new Vector2(51,37),new Color(.1f,.25f,.43f));
            var track=Panel("Timeline",footer,new Vector2(585,48),new Vector2(126,10),new Color(.12f,.38f,.57f),false);var handle=Panel("Scrub handle",track,new Vector2(0,-4),new Vector2(14,18),Cyan,false);track.GetComponent<Image>().raycastTarget=true;handle.GetComponent<Image>().raycastTarget=true;c.scrubber=track.gameObject.AddComponent<Slider>();c.scrubber.handleRect=handle;c.scrubber.targetGraphic=handle.GetComponent<Image>();c.scrubber.minValue=0;c.scrubber.maxValue=1;c.scrubber.interactable=false;
            var prompt=Panel("Question prompt and feedback",canvas.transform,new Vector2(780,880),new Vector2(869,49),new Color(.018f,.09f,.19f,.94f));
            c.promptLabel=Text("Question prompt",prompt,"Change the graph so all three vehicles stop inside the 30 m target.",new Vector2(10,1),new Vector2(850,23),16,Color.white);
            c.statusLabel=Text("Feedback",prompt,"Drag the glowing points  •  Predicted distance: 35.0 m",new Vector2(10,25),new Vector2(850,21),16,Cyan);
            var es=new GameObject("Graph UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));es.transform.SetParent(root);es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        static void UIConnection(Transform parent,Vector2 start,Vector2 end)
        {
            for(int i=0;i<24;i++)
            {
                float t=i/24f,t2=(i+1)/24f;Vector2 a=Vector2.Lerp(start,end,t)+new Vector2(0,Mathf.Sin(t*Mathf.PI*2)*17);Vector2 b=Vector2.Lerp(start,end,t2)+new Vector2(0,Mathf.Sin(t2*Mathf.PI*2)*17);
                var r=Rect("Graph-to-world light trail",parent,a,new Vector2(Vector2.Distance(a,b),5));r.pivot=new Vector2(0,.5f);r.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);var img=r.gameObject.AddComponent<Image>();img.color=Cyan;img.raycastTarget=false;
            }
            foreach(var pos in new[]{start,end}) { var r=Rect("Glowing graph port",parent,pos-new Vector2(10,10),new Vector2(20,20));var im=r.gameObject.AddComponent<Image>();im.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");im.color=Cyan;im.raycastTarget=false; }
        }
        static void UILine(Transform parent,Vector2 a,Vector2 b,Color color,float width)
        {
            var r=Rect("Graph legend stroke",parent,a,new Vector2(Vector2.Distance(a,b),width));r.pivot=new Vector2(0,.5f);r.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);var im=r.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;
        }
        static Sprite RoundedSprite()
        {
            string path=Folder+"/Generated/RoundedPanel.png";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite)return sprite;
            const int n=128;const float radius=20;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);
            var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++) { float dx=Mathf.Max(radius-x-.5f,x+.5f-(n-radius));float dy=Mathf.Max(radius-y-.5f,y+.5f-(n-radius));float d=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude-radius;pixels[y*n+x]=new Color(1,1,1,Mathf.Clamp01(.5f-d)); }
            texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=new Vector4(21,21,21,21);importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void Validate(SpeedTimeChallenge c)
        {
            if(c.vehicles.Length!=3||c.ghosts.Length!=9||c.targets.Length!=3)throw new Exception("Incomplete arena.");
            if(c.vehicles.Any(v=>!v)||!c.plot||!c.questionData)throw new Exception("Missing scene wiring.");
            if(Mathf.Abs(c.Distance(10)-35)>.001f)throw new Exception("Trapezoidal integrator failed 35 m test.");
            if(Mathf.Abs(c.Distance(1.5f)-1.875f)>.001f)throw new Exception("Partial acceleration integral failed.");
            if(Mathf.Abs(c.Speed(1.5f)-2.5f)>.001f)throw new Exception("Shared speed interpolation failed.");
            c.SendMessage("Awake");
            for(int q=0;q<3;q++)
            {
                c.SelectQuestion(q);
                if(q==2&& !c.Feedback().Contains("Slow Zone"))throw new Exception("Puzzle must reject fast entry into the slow zone.");
                c.points=q==2?new[]{new Vector2(0,0),new Vector2(2,8),new Vector2(4,8),new Vector2(5,22f/7),new Vector2(11,22f/7),new Vector2(12,0)}:new[]{new Vector2(0,0),new Vector2(3,q==0?30f/7:60f/7),new Vector2(7,q==0?30f/7:60f/7),new Vector2(10,0)};
                if(!c.Feedback().StartsWith("Great!"))throw new Exception("Valid solution rejected in question "+q+": "+c.Feedback());
                c.Play();c.SendMessage("PositionVehicles",c.Duration);
                for(int lane=0;lane<3;lane++)if(Mathf.Abs(c.vehicles[lane].position.x-c.targets[lane].position.x)>.01f)throw new Exception("Vehicle/target distance mismatch.");
            }
            c.SelectQuestion(0);
            File.WriteAllText("Work/SpeedTimeGraph/validation.txt","PASS: 3 supplied vehicles, 9 ghosts, 3 aligned target markers.\nPASS: full and partial trapezoidal integration and speed interpolation.\nPASS: all three question solutions, synchronized vehicle endpoints, puzzle slow-zone rejection.\nPASS: shared graph controller, JSON data, Play/Reset/Retry/Next/scrubber wiring.\n");
        }
        static void Capture()
        {
            var rt=new RenderTexture(1672,941,24);camera.targetTexture=rt;
            foreach(var text in root.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate(true,true);
            foreach(var graphic in root.GetComponentsInChildren<Graphic>()) { graphic.SetAllDirty();graphic.Rebuild(CanvasUpdate.PreRender); }
            foreach(var plot in root.GetComponentsInChildren<SpeedTimeGraphPlot>())plot.RefreshMesh();
            Canvas.ForceUpdateCanvases();camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1672,941,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1672,941),0,0);t.Apply();File.WriteAllBytes("Work/SpeedTimeGraph/scene-preview.png",t.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=prior;Object.DestroyImmediate(t);rt.Release();Object.DestroyImmediate(rt);
        }
    }
}
