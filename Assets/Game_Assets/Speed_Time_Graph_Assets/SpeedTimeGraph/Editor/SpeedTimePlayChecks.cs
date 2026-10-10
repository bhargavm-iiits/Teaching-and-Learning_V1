using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using SpeedTimeGraphLesson;

namespace SpeedTimeGraphLesson.Editor
{
    [InitializeOnLoad]
    public static class SpeedTimePlayChecks
    {
        const string Request="Work/SpeedTimeGraph/playcheck.request";
        static int phase,frames;
        static float originalSpeed,originalGhostX;
        static Mouse mouse;
        static Vector2 screenPoint;
        static double started;
        static SpeedTimePlayChecks()
        {
            if(File.Exists(Request)) { started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick; }
        }
        public static void BatchPlay()
        {
            if(File.Exists("Work/SpeedTimeGraph/play-error.txt"))File.Delete("Work/SpeedTimeGraph/play-error.txt");
            Directory.CreateDirectory("Work/SpeedTimeGraph");File.WriteAllText(Request,"Run isolated Play Mode checks");
            EditorSceneManager.OpenScene("Assets/Scenes/Speed_Time_Graph.unity");started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
        }
        static void Require(bool pass,string message) { if(!pass)throw new Exception(message); }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Play Mode check timed out.");
                if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
                var c=UnityEngine.Object.FindFirstObjectByType<SpeedTimeChallenge>();if(!c||++frames<8)return;
                switch(phase)
                {
                    case 0:
                        Require(c.vehicles.All(v=>v!=null),"Vehicle missing after Play Mode initialization.");
                        Require(c.promptLabel.text.Contains("30 m"),"JSON question not initialized.");
                        c.questionButtons[1].onClick.Invoke();Require(c.questionIndex==1,"Q2 button failed.");
                        c.questionButtons[2].onClick.Invoke();Require(c.questionIndex==2&&c.speedGates.All(g=>g.gameObject.activeSelf),"Q3 gate configuration failed.");
                        c.questionButtons[0].onClick.Invoke();
                        originalSpeed=c.points[1].y;originalGhostX=c.ghosts[2].position.x;
                        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                        mouse=InputSystem.AddDevice<Mouse>("GraphTestMouse");mouse.MakeCurrent();
                        screenPoint=RectTransformUtility.WorldToScreenPoint(c.uiCamera,c.plot.transform.TransformPoint(c.plot.Pixel(c.points[1])));
                        InputSystem.QueueStateEvent(mouse,new MouseState { position=screenPoint,buttons=1 });InputSystem.Update();c.SendMessage("Update");phase=1;frames=0;break;
                    case 1:
                        mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState { position=screenPoint+Vector2.up*70,buttons=1 });InputSystem.Update();c.SendMessage("Update");phase=2;frames=0;break;
                    case 2:
                        InputSystem.QueueStateEvent(mouse,new MouseState { position=screenPoint+Vector2.up*70,buttons=0 });InputSystem.Update();c.SendMessage("Update");phase=3;frames=0;break;
                    case 3:
                        Require(c.points[1].y>originalSpeed,"Mouse drag did not edit the graph.");
                        Require(c.ghosts[2].position.x>originalGhostX,"Ghost predictions did not update after graph edit.");
                        for(int lane=1;lane<3;lane++)Require(Mathf.Abs((c.ghosts[lane*3+2].position.x-c.origins[lane].x)-(c.ghosts[2].position.x-c.origins[0].x))<.001f,"Ghost distances diverged.");
                        c.playButton.onClick.Invoke();Require(c.mode==1,"Play button did not start simulation.");phase=4;frames=0;break;
                    case 4:
                        if(c.time<.3f)return;
                        for(int lane=0;lane<3;lane++)Require(Mathf.Abs(c.vehicles[lane].position.x-c.origins[lane].x-c.Distance(c.time)*c.worldScale)<.001f,"Shared playback translation failed.");
                        c.resetButton.onClick.Invoke();Require(c.mode==0&&c.time==0,"Reset button failed.");
                        for(int lane=0;lane<3;lane++)Require(Vector3.Distance(c.vehicles[lane].position,c.origins[lane])<.001f,"Reset did not restore vehicle.");
                        c.SelectQuestion(0);InputSystem.RemoveDevice(mouse);
                        c.mode=2;c.scrubber.interactable=true;c.scrubber.value=.5f;
                        Require(Mathf.Abs(c.time-c.Duration*.5f)<.001f,"Review scrubber did not update shared time.");
                        c.nextButton.onClick.Invoke();Require(c.questionIndex==1,"Next question button failed.");c.SelectQuestion(0);
                        File.WriteAllText("Work/SpeedTimeGraph/play-validation.txt","PASS: Play Mode Awake, JSON question initialization, Q1/Q2/Q3 buttons.\nPASS: real Input System mouse drag modifies graph and all final ghost predictions.\nPASS: Play button starts shared translation; Reset restores all three vehicles.\nPASS: review timeline scrubber and Next question button.\n");
                        File.Delete(Request);EditorApplication.update-=Tick;EditorApplication.Exit(0);break;
                }
            }
            catch(Exception e)
            {
                File.WriteAllText("Work/SpeedTimeGraph/play-error.txt",e.ToString());File.Delete(Request);Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.Exit(1);
            }
        }
    }
}
