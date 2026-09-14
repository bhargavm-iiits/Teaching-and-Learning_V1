using System.Collections.Generic;
using UnityEngine;

namespace ZeroReturn
{
    public class ZeroReturnWorld : MonoBehaviour
    {
        public Camera sceneCamera;
        public CollectorBot bot;
        public MotionTracker motion;
        public NumberLineController rail;
        public Transform[] spheres = new Transform[4];
        public Transform[] assembled = new Transform[4];
        public Transform ghost;
        public Dictionary<Collider,int> picks = new Dictionary<Collider,int>();
        Transform ticks, figure;
        RailDressing dressing;
        readonly Dictionary<Color,Material> materials = new Dictionary<Color,Material>();

        public void Build(RailDressing dressing, float speed)
        {
            this.dressing=dressing;
            var cameraGo=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            cameraGo.transform.SetParent(transform,false); cameraGo.tag="MainCamera";
            sceneCamera=cameraGo.GetComponent<Camera>(); sceneCamera.orthographic=true; sceneCamera.orthographicSize=8.3f;
            sceneCamera.stereoTargetEye=StereoTargetEyeMask.None;
            sceneCamera.transform.position=new Vector3(0,16,-14); sceneCamera.transform.LookAt(Vector3.zero);
            sceneCamera.clearFlags=CameraClearFlags.SolidColor; sceneCamera.backgroundColor=dressing.background;
            sceneCamera.nearClipPlane=.1f; sceneCamera.farClipPlane=100;
            var lamp=new GameObject("Key Light",typeof(Light)); lamp.transform.SetParent(transform,false);
            lamp.transform.rotation=Quaternion.Euler(45,-25,0); var light=lamp.GetComponent<Light>(); light.type=LightType.Directional; light.intensity=1.3f; light.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.65f,.7f,.67f);
            RenderSettings.fog=false;
            Primitive("Stage",PrimitiveType.Cube,new Vector3(0,-.3f,0),new Vector3(70,.3f,60),dressing.background,transform);
            var line=new GameObject("Rail"); line.transform.SetParent(transform,false); rail=line.AddComponent<NumberLineController>();
            Primitive("Rail Bed",PrimitiveType.Cube,new Vector3(0,-.02f,0),new Vector3(26,.18f,.65f),dressing.rail,line.transform);
            Primitive("Rail Face",PrimitiveType.Cube,new Vector3(0,.08f,0),new Vector3(26,.05f,.16f),new Color(.75f,.79f,.76f),line.transform);
            ticks=new GameObject("Calibration").transform; ticks.SetParent(line.transform,false);
            Primitive("Dock",PrimitiveType.Cube,new Vector3(0,.05f,0),new Vector3(1.15f,.28f,1.1f),new Color(.35f,.43f,.38f),transform);
            WorldText("Dock Label","DOCK  /  0",new Vector3(0,.1f,-1.55f),.27f,ZeroReturnView.Ink,transform);
            Primitive("Assembler Platform",PrimitiveType.Cube,new Vector3(0,.02f,5.1f),new Vector3(7,.16f,3.7f),new Color(.79f,.84f,.8f),transform);
            figure=new GameObject("AssembledFigure").transform; figure.SetParent(transform,false);
            var botRoot=new GameObject("CollectorBot"); botRoot.transform.SetParent(transform,false); bot=botRoot.AddComponent<CollectorBot>(); bot.rail=rail; bot.speed=speed;
            Primitive("Bot Body",PrimitiveType.Cube,Vector3.zero,new Vector3(.76f,.48f,.66f),dressing.bot,botRoot.transform);
            Primitive("Face",PrimitiveType.Cube,new Vector3(0,.03f,-.34f),new Vector3(.46f,.19f,.025f),dressing.rail,botRoot.transform);
            Primitive("WheelL",PrimitiveType.Cylinder,new Vector3(-.31f,-.21f,0),new Vector3(.22f,.32f,.22f),dressing.rail,botRoot.transform).localRotation=Quaternion.Euler(90,0,0);
            Primitive("WheelR",PrimitiveType.Cylinder,new Vector3(.31f,-.21f,0),new Vector3(.22f,.32f,.22f),dressing.rail,botRoot.transform).localRotation=Quaternion.Euler(90,0,0);
            Primitive("Claw Arm",PrimitiveType.Cube,new Vector3(0,.48f,0),new Vector3(.08f,.55f,.08f),dressing.rail,botRoot.transform);
            Primitive("Claw",PrimitiveType.Cube,new Vector3(0,.7f,0),new Vector3(.5f,.08f,.24f),dressing.rail,botRoot.transform);
            bot.ResetAtDock(); motion=botRoot.AddComponent<MotionTracker>(); motion.bot=bot; motion.ResetRun();
            for(int i=0;i<4;i++)
            {
                spheres[i]=Primitive("Sphere_"+i,PrimitiveType.Sphere,new Vector3(0,1,0),Vector3.one*.55f,dressing.sphere,transform);
                picks[spheres[i].GetComponent<Collider>()]=i;
                WorldText("Stamp","",new Vector3(0,.58f,0),.25f,ZeroReturnView.Ink,spheres[i]);
                assembled[i]=Primitive("Socket Sphere_"+i,PrimitiveType.Sphere,Vector3.zero,Vector3.one*.35f,dressing.sphere,figure);
                assembled[i].gameObject.SetActive(false);
            }
            ghost=Primitive("Ghost Preview",PrimitiveType.Cube,new Vector3(0,.2f,0),new Vector3(.65f,.04f,.5f),new Color(.66f,.73f,.68f),transform);
            ghost.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            // Fit the full calibrated rail on narrower screens too.
            if(sceneCamera!=null) sceneCamera.orthographicSize=Mathf.Max(8.3f,14.5f/sceneCamera.aspect);
        }
        public void Load(LevelInstance level)
        {
            foreach(Transform child in ticks) Destroy(child.gameObject);
            for(float x=-12;x<=12.001f;x+=level.unitsPerTick)
            {
                bool major=Mathf.Abs(x/(4*level.unitsPerTick)-Mathf.Round(x/(4*level.unitsPerTick)))<.001f;
                Primitive("Tick",PrimitiveType.Cube,new Vector3(x,.125f,0),new Vector3(.03f,.02f,major?.6f:.35f),new Color(.8f,.85f,.82f),ticks);
                if(major && Mathf.Abs(x)>.001f) WorldText("Tick Label",ZeroReturnView.Signed(x),new Vector3(x,.08f,-.73f),.25f,ZeroReturnView.Ink,ticks);
            }
            for(int i=0;i<4;i++)
            {
                assembled[i].gameObject.SetActive(false);
                spheres[i].gameObject.SetActive(i<level.coordinates.Length);
                if(i>=level.coordinates.Length) continue;
                spheres[i].SetParent(transform,true); spheres[i].localScale=Vector3.one*.55f;
                spheres[i].position=rail.ToWorld(level.coordinates[i])+Vector3.up*.52f;
                spheres[i].GetComponentInChildren<TextMesh>().text="x = "+ZeroReturnView.Signed(level.coordinates[i]);
            }
            foreach(Transform child in figure) if(child.name=="Weld") Destroy(child.gameObject);
            ghost.gameObject.SetActive(false);
            bot.ResetAtDock(); motion.ResetRun();
        }

        public void Carry(int index)
        {
            spheres[index].SetParent(bot.transform,true); spheres[index].localPosition=new Vector3(0,1,0);
        }

        public void RefreshAssembly(AssemblyModel model)
        {
            Vector3[] points=model.level.shape==ShapeType.Line?new[]{new Vector3(-2,.4f,5),new Vector3(2,.4f,5)}
                :model.level.shape==ShapeType.Triangle?new[]{new Vector3(-2,.4f,4.3f),new Vector3(0,.4f,6),new Vector3(2,.4f,4.3f)}
                :new[]{new Vector3(-1.4f,.4f,4.2f),new Vector3(1.4f,.4f,4.2f),new Vector3(-1.4f,.4f,6.2f),new Vector3(1.4f,.4f,6.2f)};
            for(int i=0;i<model.sockets.Length;i++)
            {
                assembled[i].position=points[i]; assembled[i].gameObject.SetActive(model.sockets[i]>=0);
                if(model.sockets[i]>=0) spheres[model.sockets[i]].gameObject.SetActive(false);
            }
            if(!model.Complete)return;
            Weld(points[0],points[1]);
            if(points.Length==3){Weld(points[1],points[2]);Weld(points[2],points[0]);}
            if(points.Length==4){Weld(points[2],points[3]);Weld(points[0],points[2]);Weld(points[1],points[3]);}
        }
        public void Eject(int index,LevelInstance level)
        {
            spheres[index].SetParent(transform,true); spheres[index].position=rail.ToWorld(level.coordinates[index])+Vector3.up*.52f;
            spheres[index].gameObject.SetActive(true);
        }
        void Weld(Vector3 a,Vector3 b)
        {
            var t=Primitive("Weld",PrimitiveType.Cube,(a+b)/2,new Vector3(.08f,.08f,Vector3.Distance(a,b)),dressing.rail,figure);
            t.rotation=Quaternion.LookRotation(b-a);
        }
        Transform Primitive(string name,PrimitiveType type,Vector3 p,Vector3 scale,Color color,Transform parent)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            if(!materials.TryGetValue(color,out var material))
            { material=dressing.surface!=null?new Material(dressing.surface):new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.25f);materials[color]=material; }
            go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        void WorldText(string name,string text,Vector3 p,float height,Color color,Transform parent)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.rotation=sceneCamera.transform.rotation;
            var tm=go.AddComponent<TextMesh>();tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontSize=64;tm.characterSize=height*10/64;
            tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.text=text;tm.color=color;
            go.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
        }
        void OnDestroy(){foreach(var material in materials.Values) if(material!=null) Destroy(material);}
    }
}
