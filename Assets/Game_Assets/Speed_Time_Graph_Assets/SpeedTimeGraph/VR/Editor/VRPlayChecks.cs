using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SpeedTimeGraphLesson.VR.Editor
{
    [InitializeOnLoad]
    public static class VRPlayChecks
    {
        const string Folder="Work/SpeedTimeGraph/VR/",Request=Folder+"play.request";
        static int phase,frames;
        static float pausedTime;
        static double started;
        static VRPlayChecks() { if(File.Exists(Request)) { started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick; } }
        public static void BatchPlay()
        {
            Directory.CreateDirectory(Folder);File.WriteAllText(Request,"VR interaction validation");
            EditorSceneManager.OpenScene("Assets/Scenes/Speed_Time_Graph.unity");
            started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
        }
        static void Require(bool ok,string message) { if(!ok)throw new Exception(message); }
        static void Ray(VRGraphPlot plot,Vector2 point,Action<TrackedDeviceEventData> act)
        {
            Vector3 hit=plot.rectTransform.TransformPoint(point),normal=plot.transform.forward;
            act(new TrackedDeviceEventData(EventSystem.current) { pointerId=41,button=PointerEventData.InputButton.Left,rayPoints=new List<Vector3>{hit-normal,hit+normal} });
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>100)throw new Exception("VR Play Mode timeout");
                if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
                var c=UnityEngine.Object.FindFirstObjectByType<VRMotionChallenge>();if(!c||++frames<12)return;
                switch(phase)
                {
                    case 0:
                        for(int i=0;i<3;i++)Require(Mathf.Abs(c.Speed(i,0)-new[]{20f,30f,10f}[i])<.001f,"Starting speed incorrect");
                        c.questions[2].onClick.Invoke();Require(c.questionIndex==2&&c.speedGates[0].gameObject.activeSelf,"Advanced question button failed");
                        c.questions[0].onClick.Invoke();c.vehicleSelectors[2].onClick.Invoke();Require(c.selectedVehicle==2,"Vehicle selector failed");
                        c.vehicleSelectors[0].onClick.Invoke();float old=c.profile[1].y;
                        Vector2 point=c.plot.DisplayPoint(1,0);
                        Ray(c.plot,point,e=>ExecuteEvents.Execute(c.plot.gameObject,e,ExecuteEvents.pointerDownHandler));
                        Ray(c.plot,point+Vector2.up*35,e=>ExecuteEvents.Execute(c.plot.gameObject,e,ExecuteEvents.dragHandler));
                        Ray(c.plot,point,e=>ExecuteEvents.Execute(c.plot.gameObject,e,ExecuteEvents.pointerUpHandler));
                        Require(c.profile[1].y>old&&c.profile[0].y==1,"Tracked-controller ray drag failed");
                        for(int lane=0;lane<3;lane++)Require(Mathf.Abs(c.Distance(lane,c.Duration)/c.startingSpeeds[lane]-c.Integral(c.Duration))<.001f,"Shared profile scaling failed");
                        c.play.onClick.Invoke();Require(c.mode==1,"Run button failed");phase=1;frames=0;break;
                    case 1:
                        if(c.time<.3f)return;
                        for(int lane=0;lane<3;lane++)Require(Mathf.Abs(c.vehicles[lane].position.x-c.origins[lane].x-c.Distance(lane,c.time))<.01f,"Playback position failed");
                        c.pause.onClick.Invoke();Require(c.mode==3,"Pause failed");pausedTime=c.time;phase=2;frames=0;break;
                    case 2:
                        Require(Mathf.Abs(c.time-pausedTime)<.001f,"Paused clock advances");
                        c.pause.onClick.Invoke();Require(c.mode==1,"Resume failed");c.reset.onClick.Invoke();Require(c.mode==0&&c.time==0,"Reset failed");
                        c.mode=2;c.timeline.interactable=true;c.timeline.value=.5f;Require(Mathf.Abs(c.time-c.Duration*.5f)<.001f,"Review timeline failed");
                        c.restore.onClick.Invoke();Require(c.mode==0&&c.profile[1].y==1.4f,"Restore graph failed");
                        c.stationButtons[1].onClick.Invoke();phase=3;frames=0;break;
                    case 3:
                        Require(Mathf.Abs(c.eye.transform.position.x-c.player.stations[1].position.x)<.2f,"Teleport button did not move XR origin");
                        Require(Vector3.Distance(c.player.console.position,c.eye.transform.position)<5,"Console did not follow teleport");
                        c.stationButtons[0].onClick.Invoke();phase=4;frames=0;break;
                    case 4:
                        Require(Mathf.Abs(c.eye.transform.position.x-c.player.stations[0].position.x)<.2f,"Return teleport failed");
                        File.WriteAllText(Folder+"play-validation.txt","PASS: Awake, exact bike10/car20/train30 starting speeds, question and vehicle buttons.\nPASS: tracked-device controller ray drag edits graph while starting speed stays locked.\nPASS: real Play Mode vehicle motion, scaled distances, pause/resume/reset and review timeline.\nPASS: restore graph, teleport to mid-route and return, console follows player.\nHeadset hardware was not available for physical tracking/comfort testing.\n");
                        File.Delete(Request);EditorApplication.update-=Tick;EditorApplication.Exit(0);break;
                }
            }
            catch(Exception e) { File.WriteAllText(Folder+"play-error.txt",e.ToString());File.Delete(Request);Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.Exit(1); }
        }
    }
}
