using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using Unity.XR.CoreUtils;
using Object=UnityEngine.Object;

namespace SpeedTimeGraphLesson.VR.Editor
{
    public static class VRSceneBuilder
    {
        const string Path="Assets/Scenes/Speed_Time_Graph.unity",Folder="Assets/SpeedTimeGraph/VR/Generated";
        static Transform root;
        static Camera eye;
        static TMP_FontAsset font;
        static Sprite rounded;
        static Material deck,metal,cyan;
        static readonly Color Navy=new Color(.015f,.065f,.15f),Cyan=new Color(.13f,.84f,1);
        [MenuItem("Tools/Speed Time Graph/VR/Build First Person VR Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);Directory.CreateDirectory("Work/SpeedTimeGraph/VR");
            SpeedTimeGraphLesson.Editor.SpeedTimeSceneBuilder.Build();
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();root=scene.GetRootGameObjects().Single(g=>g.name=="TripleVehicleArena").transform;root.name="Speed Time Graph • First Person VR";
            var legacy=root.GetComponent<SpeedTimeChallenge>();
            var c=root.gameObject.AddComponent<VRMotionChallenge>();
            c.questionData=legacy.questionData;c.vehicles=legacy.vehicles;c.targets=legacy.targets;c.ghosts=legacy.ghosts;c.speedGates=legacy.speedGates;c.slowZones=legacy.slowZones;c.origins=legacy.origins;
            Object.DestroyImmediate(root.Find("Shared holographic interface").gameObject);Object.DestroyImmediate(root.Find("Graph UI EventSystem").gameObject);Object.DestroyImmediate(legacy.uiCamera.gameObject);Object.DestroyImmediate(legacy);
            foreach(var volume in root.GetComponentsInChildren<Volume>())volume.enabled=false;
            deck=Mat("Viewing platform",new Color(.075f,.13f,.2f));metal=Mat("Platform rail",new Color(.36f,.49f,.61f));cyan=Mat("Controller ray",Cyan,true);
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");rounded=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SpeedTimeGraph/Generated/RoundedPanel.png");
            ExtendWorld();var player=BuildPlayer(c);BuildConsole(c,player);ConfigureVehicles(c,player);BuildStations(player);BuildLinks(c);
            var settings=ConfigureOpenXR();root.gameObject.AddComponent<VRSessionBootstrap>().managerSettings=settings;
            c.InitializeData();c.SelectQuestion(0);
            Validate(c,player);c.SelectQuestion(0);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,Path);
            RenderSettings.fogStartDistance=140;RenderSettings.fogEndDistance=900;EditorSceneManager.SaveScene(scene,Path);
            File.WriteAllText("Work/SpeedTimeGraph/VR/build-success.txt","First person VR scene saved.\n");
        }
        public static void BatchBuild()
        {
            try
            {
                Build();int frames=0;
                void Tick() { if(++frames<35)return;EditorApplication.update-=Tick;Capture();EditorApplication.Exit(0); }
                EditorApplication.update+=Tick;
            }
            catch(Exception e) { File.WriteAllText("Work/SpeedTimeGraph/VR/build-error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1); }
        }
        static Material Mat(string name,Color color,bool unlit=false)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m) { m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path); }m.color=color;EditorUtility.SetDirty(m);return m;
        }
        static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool collider=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static Transform Find(Transform t,string name) { if(t.name==name)return t;foreach(Transform child in t) { var found=Find(child,name);if(found)return found; }return null; }
        static void ExtendWorld()
        {
            foreach(Transform scenery in root.Cast<Transform>().ToArray())
                if(scenery.name.StartsWith("Scenery ")&&scenery.position.x<24&&scenery.position.z<17)
                    Object.DestroyImmediate(scenery.gameObject);
            var ground=root.Find("Landscape");ground.position=new Vector3(360,-1.2f,0);ground.localScale=new Vector3(850,2,100);
            var lake=root.Find("Blue lake");lake.position=new Vector3(250,-.12f,26);lake.localScale=new Vector3(600,.13f,20);
            foreach(string name in new[]{"Car Lane","Train Lane","Motorcycle Lane"})
            {
                var lane=root.Find(name);var surface=lane.Cast<Transform>().First(t=>t.name.EndsWith(" surface"));float z=surface.position.z;
                var scale=surface.localScale;scale.x=760;surface.localScale=scale;surface.localPosition=new Vector3(360,surface.localPosition.y,z);
                foreach(Transform child in lane)if(child.name=="Road edge"||child.name=="Continuous guardrail"||child.name=="Steel rail") { var s=child.localScale;s.x=760;child.localScale=s;child.localPosition=new Vector3(360,child.localPosition.y,child.localPosition.z); }
                for(int x=50;x<740;x+=3)
                {
                    if(name=="Train Lane")
                    {
                        var timber=AssetDatabase.LoadAssetAtPath<Material>("Assets/SpeedTimeGraph/Generated/Timber.mat");
                        for(int j=0;j<3;j++)Box("Railway sleeper",lane,new Vector3(x+j,.55f,0),new Vector3(.21f,.14f,2.9f),timber);
                    }
                    else
                    {
                        Box("Road center dash",lane,new Vector3(x,.49f,z),new Vector3(1.45f,.025f,.1f),AssetDatabase.LoadAssetAtPath<Material>("Assets/SpeedTimeGraph/Generated/White.mat"));
                        foreach(int s in new[]{-1,1})Box("Guardrail post",lane,new Vector3(x,.8f,z+s*2.9f),new Vector3(.12f,.85f,.12f),metal);
                    }
                }
                for(int x=0;x<=210;x+=10)
                {
                    var label=WorldText(x+" m",lane,new Vector3(x,1.45f,z+3.4f),.55f,Cyan,new Vector2(2,1));label.transform.rotation=Quaternion.Euler(0,180,0);
                }
            }
            // Repeat the supplied low-poly nature assets around the extended observation route.
            var green=AssetDatabase.LoadAssetAtPath<Material>("Assets/SpeedTimeGraph/Generated/Nature_SimpleNaturePack_Texture_01.mat");
            for(int x=60;x<250;x+=12)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleNaturePack/Prefabs/Tree_01.prefab");if(!prefab)continue;
                foreach(float z in new[]{18f,-19f,6f}) { var tree=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);tree.name="Route scenery";tree.transform.position=new Vector3(x,0,z);tree.transform.localScale=Vector3.one*(1+x%3*.2f);if(green)foreach(var r in tree.GetComponentsInChildren<Renderer>())r.sharedMaterial=green; }
            }
        }
        static VRPlayerStation BuildPlayer(VRMotionChallenge c)
        {
            const string rigPath="Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);if(!asset)throw new Exception("XR Starter Assets rig missing.");
            var rig=(GameObject)PrefabUtility.InstantiatePrefab(asset,root);rig.name="XR Origin • First Person Player";rig.transform.position=new Vector3(-10,2.7f,-21);rig.transform.rotation=Quaternion.Euler(0,45,0);
            var origin=rig.GetComponent<XROrigin>();origin.CameraYOffset=1.65f;
            eye=Find(rig.transform,"Main Camera").GetComponent<Camera>();eye.orthographic=false;eye.fieldOfView=78;eye.nearClipPlane=.03f;eye.farClipPlane=1300;eye.clearFlags=CameraClearFlags.SolidColor;eye.backgroundColor=new Color(.31f,.72f,.98f);eye.stereoTargetEye=StereoTargetEyeMask.Both;eye.tag="MainCamera";
            eye.transform.localRotation=Quaternion.Euler(8,0,0);
            var data=eye.GetComponent<UniversalAdditionalCameraData>();if(!data)data=eye.gameObject.AddComponent<UniversalAdditionalCameraData>();data.allowXRRendering=true;data.renderPostProcessing=false;
            foreach(var mb in rig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if(!mb)continue;string type=mb.GetType().Name;
                if(type.Contains("Affordance"))mb.enabled=false;
                if(type=="DynamicMoveProvider"||type=="ContinuousMoveProvider"||type=="ContinuousTurnProvider"||type=="GravityProvider")mb.enabled=false;
            }
            foreach(var line in rig.GetComponentsInChildren<LineRenderer>(true))line.sharedMaterial=cyan;
            foreach(var ray in rig.GetComponentsInChildren<XRRayInteractor>(true)) { ray.enableUIInteraction=true;ray.maxRaycastDistance=30;ray.interactionLayers=1<<31; }
            foreach(var interactor in rig.GetComponentsInChildren<NearFarInteractor>(true)) { interactor.enableUIInteraction=true;interactor.interactionLayers=1; }
            if(!root.GetComponentInChildren<XRInteractionManager>())new GameObject("VR Interaction Manager",typeof(XRInteractionManager)).transform.SetParent(root);
            var events=new GameObject("VR Event System",typeof(EventSystem),typeof(XRUIInputModule));events.transform.SetParent(root);var ui=events.GetComponent<XRUIInputModule>();ui.enableXRInput=true;ui.enableMouseInput=true;
            var player=rig.AddComponent<VRPlayerStation>();player.eye=eye;player.teleportProvider=rig.GetComponentInChildren<TeleportationProvider>(true);c.eye=eye;c.player=player;
            c.feedbackAudio=eye.gameObject.AddComponent<AudioSource>();c.feedbackAudio.playOnAwake=false;c.feedbackAudio.spatialBlend=0;
            return player;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 at,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(at.x,-at.y);r.sizeDelta=size;return r;
        }
        static RectTransform Panel(string name,Transform parent,Vector2 at,Vector2 size,Color color)
        {
            var r=Rect(name,parent,at,size);var im=r.gameObject.AddComponent<Image>();im.sprite=rounded;im.type=Image.Type.Sliced;im.color=color;im.raycastTarget=false;
            var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(Cyan.r,Cyan.g,Cyan.b,.5f);outline.effectDistance=new Vector2(2,-2);return r;
        }
        static TMP_Text Text(string name,Transform parent,string value,Vector2 at,Vector2 size,float points,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
        {
            var r=Rect(name,parent,at,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=points;t.fontStyle=FontStyles.Bold;t.color=color;t.alignment=alignment;t.raycastTarget=false;return t;
        }
        static Button Button(string name,Transform parent,string value,Vector2 at,Vector2 size,Color color)
        {
            var r=Panel(name,parent,at,size,color);var button=r.gameObject.AddComponent<Button>();r.GetComponent<Image>().raycastTarget=true;button.targetGraphic=r.GetComponent<Image>();Text("Label",r,value,Vector2.zero,size,20,Color.white);return button;
        }
        static void BuildConsole(VRMotionChallenge c,VRPlayerStation player)
        {
            Vector3 head=eye.transform.position;Quaternion facing=player.transform.rotation*Quaternion.Euler(0,-24,0);
            var mount=new GameObject("Grabbable world-space graph console").transform;mount.SetParent(root);mount.position=head+facing*new Vector3(0,-.9f,2.4f);mount.rotation=facing;player.console=mount;
            Box("Left grip rail",mount,new Vector3(-.68f,0,.01f),new Vector3(.045f,1.24f,.055f),metal,true);
            Box("Right grip rail",mount,new Vector3(.68f,0,.01f),new Vector3(.045f,1.24f,.055f),metal,true);
            Box("Top grip rail",mount,new Vector3(0,.62f,.01f),new Vector3(1.4f,.045f,.055f),metal,true);
            Box("Bottom grip rail",mount,new Vector3(0,-.62f,.01f),new Vector3(1.4f,.045f,.055f),metal,true);
            var body=mount.gameObject.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
            var grab=mount.gameObject.AddComponent<XRGrabInteractable>();grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.useDynamicAttach=true;
            var canvas=new GameObject("VR Graph Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(TrackedDeviceGraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(mount,false);canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=eye;canvas.GetComponent<RectTransform>().sizeDelta=new Vector2(720,680);canvas.transform.localScale=Vector3.one*.0018f;
            canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit=10;
            var frame=Panel("Console surface",canvas.transform,Vector2.zero,new Vector2(720,680),Navy);
            c.title=Text("Graph title",frame,"CAR SPEED-TIME GRAPH",new Vector2(16,8),new Vector2(688,40),30,Cyan);
            c.vehicleSelectors=new Button[3];string[] choices={"CAR: 20 m/s","TRAIN: 30 m/s","BIKE: 10 m/s"};var colors=new[]{new Color(.03f,.28f,.48f),new Color(.48f,.1f,.16f),new Color(.48f,.26f,.035f)};
            for(int i=0;i<3;i++)c.vehicleSelectors[i]=Button("Select "+VRMotionChallenge.Name(i),frame,choices[i],new Vector2(16+i*234,53),new Vector2(220,47),colors[i]);
            c.axisLabel=Text("Speed axis",frame,"Speed (m/s)",new Vector2(30,106),new Vector2(660,28),20,Color.white);
            var graph=Rect("VR trigger-draggable graph",frame,new Vector2(59,145),new Vector2(630,225));c.plot=graph.gameObject.AddComponent<VRGraphPlot>();c.plot.controller=c;c.plot.raycastTarget=true;
            for(int i=0;i<=6;i++)Text("Speed tick "+i,frame,(i*10).ToString(),new Vector2(19,356-i*37.5f),new Vector2(31,25),16,Cyan);
            c.plot.timeAxis=Text("Time axis",frame,"Time (s)",new Vector2(200,374),new Vector2(320,25),20,Color.white);
            Text("Legend",frame,"Orange: speed up | Yellow: steady | Green: slow down",new Vector2(14,405),new Vector2(690,27),18,Color.white);
            c.play=Button("Run",frame,"RUN ALL THREE",new Vector2(16,440),new Vector2(228,49),new Color(.025f,.4f,.23f));
            c.pause=Button("Pause",frame,"PAUSE",new Vector2(256,440),new Vector2(136,49),new Color(.08f,.22f,.43f));c.reset=Button("Reset",frame,"RESET / RETRY",new Vector2(404,440),new Vector2(162,49),new Color(.08f,.22f,.43f));c.restore=Button("Restore graph",frame,"RESTORE",new Vector2(578,440),new Vector2(126,49),new Color(.08f,.22f,.43f));
            c.questions=new Button[3];for(int i=0;i<3;i++)c.questions[i]=Button("Question "+(i+1),frame,"Q"+(i+1),new Vector2(16+i*80,503),new Vector2(68,43),new Color(.08f,.22f,.43f));
            c.next=Button("Next",frame,"NEXT",new Vector2(256,503),new Vector2(96,43),new Color(.08f,.22f,.43f));c.recenter=Button("Recenter",frame,"RECENTER CONSOLE",new Vector2(364,503),new Vector2(340,43),new Color(.08f,.22f,.43f));
            var track=Panel("Review timeline",frame,new Vector2(28,565),new Vector2(664,20),new Color(.13f,.3f,.47f));track.GetComponent<Image>().raycastTarget=true;
            var handle=Panel("Timeline handle",track,new Vector2(0,-7),new Vector2(35,34),Cyan);handle.GetComponent<Image>().raycastTarget=true;c.timeline=track.gameObject.AddComponent<Slider>();c.timeline.handleRect=handle;c.timeline.targetGraphic=handle.GetComponent<Image>();c.timeline.minValue=0;c.timeline.maxValue=1;
            c.stationButtons=new Button[3];for(int i=0;i<3;i++)c.stationButtons[i]=Button("Viewing station "+i,frame,new[]{"START VIEW","MIDTRACK VIEW","FINISH VIEW"}[i],new Vector2(16+i*234,606),new Vector2(220,43),new Color(.075f,.24f,.32f));
            Text("Controls hint",frame,"Trigger: drag / press | Grip the frame to move it | Thumbstick: teleport / snap turn",new Vector2(14,652),new Vector2(690,23),14,Cyan);
            // An independent readout is fixed to the console, visible from every viewing station.
            var hud=new GameObject("VR Mission and Live Readout",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();hud.transform.SetParent(mount,false);hud.renderMode=RenderMode.WorldSpace;hud.worldCamera=eye;hud.GetComponent<RectTransform>().sizeDelta=new Vector2(860,256);hud.transform.localScale=Vector3.one*.0018f;hud.transform.localPosition=new Vector3(1.55f,.45f,.30f);
            var mission=Panel("Mission",hud.transform,Vector2.zero,new Vector2(860,256),Navy);
            Text("Challenge name",mission,"CAR, TRAIN & MOTORCYCLE • VR CHALLENGE",new Vector2(12,4),new Vector2(836,33),26,Cyan);
            c.questionText=Text("Question",mission,"",new Vector2(14,43),new Vector2(832,86),20,Color.white);
            c.readout=Text("Live speeds",mission,"",new Vector2(14,135),new Vector2(832,49),20,Color.white);
            c.status=Text("Feedback",mission,"",new Vector2(14,190),new Vector2(832,61),18,Cyan);
            foreach(var t in mount.GetComponentsInChildren<Transform>())t.gameObject.layer=5;
        }
        static TMP_Text WorldText(string value,Transform parent,Vector3 pos,float metres,Color color,Vector2 size)
        {
            var t=new GameObject(value,typeof(TextMeshPro)).GetComponent<TextMeshPro>();t.transform.SetParent(parent,false);t.transform.localPosition=pos;t.font=font;t.text=value;t.fontSize=metres*10;t.color=color;t.fontStyle=FontStyles.Bold;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=size;t.raycastTarget=false;return t;
        }
        static void ConfigureVehicles(VRMotionChallenge c,VRPlayerStation player)
        {
            c.vehicleLabels=new TMP_Text[3];c.targetLabels=new TMP_Text[3];player.vehicleLabels=new Transform[6];
            for(int i=0;i<3;i++)
            {
                foreach(var original in c.targets[i].GetComponentsInChildren<TMP_Text>())Object.DestroyImmediate(original.gameObject);
                c.targetLabels[i]=WorldText("STOP",c.targets[i],new Vector3(0,3.6f,0),.7f,Color.white,new Vector2(3.5f,1.2f));player.vehicleLabels[i+3]=c.targetLabels[i].transform;
                c.vehicleLabels[i]=WorldText(VRMotionChallenge.Name(i),c.vehicles[i],new Vector3(0,i==1?4.2f:3.4f,0),.55f,Cyan,new Vector2(3.6f,1.4f));player.vehicleLabels[i]=c.vehicleLabels[i].transform;
                var box=c.vehicles[i].gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,1,0);box.size=new Vector3(i==1?12.5f:i==0?5.8f:3.8f,2.4f,2.5f);
                var interactable=c.vehicles[i].gameObject.AddComponent<XRSimpleInteractable>();var selection=c.vehicles[i].gameObject.AddComponent<VRVehicleSelection>();selection.controller=c;selection.lane=i;
            }
            foreach(var label in player.vehicleLabels)label.rotation=Quaternion.LookRotation(label.position-eye.transform.position,Vector3.up);
        }
        static void BuildStations(VRPlayerStation player)
        {
            player.stations=new Transform[3];float[] x={-10,70,175};
            for(int i=0;i<3;i++)
            {
                var p=new GameObject(new[]{"Start viewing platform","Midtrack viewing platform","Finish viewing platform"}[i]).transform;p.SetParent(root);p.position=new Vector3(x[i],2.5f,-21);
                var surface=Box("Teleport deck",p,Vector3.zero,new Vector3(i==0?8:6,.4f,7),deck,true);
                var anchor=new GameObject("Standing spot").transform;anchor.SetParent(root);anchor.position=new Vector3(x[i],2.7f,-21);anchor.rotation=Quaternion.Euler(0,45,0);player.stations[i]=anchor;
                var teleport=surface.AddComponent<TeleportationAnchor>();teleport.teleportAnchorTransform=anchor;teleport.matchOrientation=MatchOrientation.TargetUpAndForward;teleport.interactionLayers=1<<31;
                Box("Platform support",p,new Vector3(0,-1.5f,0),new Vector3(1.3f,3,1.3f),metal);
                Box("Back safety rail",p,new Vector3(0,.65f,-3.35f),new Vector3(7,.1f,.1f),cyan);
                for(int s=-1;s<=1;s+=2)Box("Safety post",p,new Vector3(s*3,.3f,-3.35f),new Vector3(.08f,1.2f,.08f),metal);
                var sign=WorldText(new[]{"START OBSERVATION","MIDTRACK OBSERVATION","FINISH OBSERVATION"}[i],p,new Vector3(0,1,-3.3f),.45f,Cyan,new Vector2(6,1));sign.transform.rotation=Quaternion.Euler(0,180,0);
            }
        }
        static void BuildLinks(VRMotionChallenge c)
        {
            var links=root.gameObject.AddComponent<VRGraphConnections>();links.controller=c;links.links=new LineRenderer[3];
            for(int i=0;i<3;i++) { var line=new GameObject("Live graph link to "+VRMotionChallenge.Name(i),typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(root);line.sharedMaterial=cyan;line.widthMultiplier=.01f;line.positionCount=24;line.useWorldSpace=true;line.shadowCastingMode=ShadowCastingMode.Off;links.links[i]=line; }
            links.SendMessage("LateUpdate");
        }
        static XRManagerSettings ConfigureOpenXR()
        {
            Directory.CreateDirectory("Assets/XR");
            var per=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            if(!per) { per=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(per,"Assets/XR/XRGeneralSettingsPerBuildTarget.asset"); }
            EditorBuildSettings.AddConfigObject("com.unity.xr.management.loader_settings",per,true);
            XRManagerSettings desktop=null;
            foreach(var group in new[]{BuildTargetGroup.Standalone,BuildTargetGroup.Android})
            {
                string prefix=Folder+"/"+group;
                var loader=AssetDatabase.LoadAssetAtPath<OpenXRLoader>(prefix+"OpenXRLoader.asset");if(!loader) { loader=ScriptableObject.CreateInstance<OpenXRLoader>();AssetDatabase.CreateAsset(loader,prefix+"OpenXRLoader.asset"); }
                var manager=AssetDatabase.LoadAssetAtPath<XRManagerSettings>(prefix+"XRManager.asset");if(!manager) { manager=ScriptableObject.CreateInstance<XRManagerSettings>();AssetDatabase.CreateAsset(manager,prefix+"XRManager.asset"); }
                manager.automaticLoading=false;manager.automaticRunning=false;manager.TrySetLoaders(new System.Collections.Generic.List<XRLoader>{loader});
                var general=AssetDatabase.LoadAssetAtPath<XRGeneralSettings>(prefix+"XRGeneral.asset");if(!general) { general=ScriptableObject.CreateInstance<XRGeneralSettings>();AssetDatabase.CreateAsset(general,prefix+"XRGeneral.asset"); }
                general.Manager=manager;general.InitManagerOnStart=false;per.SetSettingsForBuildTarget(group,general);
                FeatureHelpers.RefreshFeatures(group);var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                foreach(var feature in settings.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>())
                {
                    if(feature is OculusTouchControllerProfile||feature is ValveIndexControllerProfile||feature is HTCViveControllerProfile||feature is MicrosoftMotionControllerProfile||feature.GetType().Name=="KHRSimpleControllerProfile")feature.enabled=true;
                }
                settings.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;EditorUtility.SetDirty(settings);EditorUtility.SetDirty(manager);EditorUtility.SetDirty(general);if(group==BuildTargetGroup.Standalone)desktop=manager;
            }
            EditorUtility.SetDirty(per);AssetDatabase.SaveAssets();return desktop;
        }
        static void Validate(VRMotionChallenge c,VRPlayerStation player)
        {
            for(int lane=0;lane<3;lane++)if(Mathf.Abs(c.Speed(lane,0)-new[]{20f,30f,10f}[lane])>.001f)throw new Exception("Incorrect initial vehicle speed.");
            if(eye.orthographic||eye.stereoTargetEye!=StereoTargetEyeMask.Both)throw new Exception("Not a first person stereo camera.");
            if(c.plot.canvas.renderMode!=RenderMode.WorldSpace||!c.plot.canvas.GetComponent<TrackedDeviceGraphicRaycaster>())throw new Exception("Graph is not a tracked VR world canvas.");
            if(!player.teleportProvider||player.stations.Length!=3||!player.console.GetComponent<XRGrabInteractable>())throw new Exception("VR interaction setup incomplete.");
            for(int q=0;q<3;q++)
            {
                c.SelectQuestion(q);
                c.profile=q==0?new[]{new Vector2(0,1),new Vector2(1,1.1f),new Vector2(2,1.1f),new Vector2(3.5454545f,0)}:
                    q==1?new[]{new Vector2(0,1),new Vector2(2,1.2f),new Vector2(4,1.2f),new Vector2(6.333333f,0)}:
                    new[]{new Vector2(0,1),new Vector2(1,1.3f),new Vector2(2,1.3f),new Vector2(2.5f,.3f),new Vector2(9.166667f,.3f),new Vector2(10.166667f,0)};
                if(!c.Feedback().StartsWith("Great!"))throw new Exception("Valid VR challenge solution rejected in Q"+q+": "+c.Feedback());
                c.PositionVehicles(c.Duration);
                for(int lane=0;lane<3;lane++)if(Mathf.Abs(c.vehicles[lane].position.x-c.targets[lane].position.x)>QuestionTolerance(c,lane))throw new Exception("Lane target mismatch.");
            }
            File.WriteAllText("Work/SpeedTimeGraph/VR/validation.txt","PASS: initial bike 10 m/s, car 20 m/s, train 30 m/s.\nPASS: first-person stereo XR rig; world-space tracked graph UI; grab console; three teleport stations.\nPASS: all three challenge solutions, analytic vehicle-specific integration and target scaling.\n");
        }
        static float QuestionTolerance(VRMotionChallenge c,int lane)=>c.Question.toleranceM*c.Ratio(lane)+.01f;
        static void Capture()
        {
            foreach(var text in root.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate(true,true);
            var rt=new RenderTexture(1672,941,24);eye.targetTexture=rt;Canvas.ForceUpdateCanvases();foreach(var plot in root.GetComponentsInChildren<VRGraphPlot>())plot.RefreshMesh();eye.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1672,941,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1672,941),0,0);image.Apply();File.WriteAllBytes("Work/SpeedTimeGraph/VR/first-person-preview.png",image.EncodeToPNG());eye.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);
        }
    }
}
