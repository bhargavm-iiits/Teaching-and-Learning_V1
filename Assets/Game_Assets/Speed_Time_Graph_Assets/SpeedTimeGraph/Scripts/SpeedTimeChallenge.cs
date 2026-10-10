using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace SpeedTimeGraphLesson
{
    [Serializable] public class ChallengeQuestion
    {
        public int id; public string title,prompt; public float targetDistanceM,toleranceM,finalSpeedMps;
        public bool showAreaShading,allowHorizontalPointEditing;
        public SpeedGate speedGate; public SlowZone slowZone;
    }
    [Serializable] public class SpeedGate { public float distanceM,minSpeedMps; }
    [Serializable] public class SlowZone { public float startM,endM,maxSpeedMps; }
    [Serializable] public class ChallengeCatalog { public ChallengeQuestion[] questions; }
    public class SpeedTimeChallenge : MonoBehaviour
    {
        public TextAsset questionData;
        public Vector2[] points = { new Vector2(0,0),new Vector2(3,5),new Vector2(7,5),new Vector2(10,0) };
        public Transform[] vehicles, targets, speedGates, slowZones;
        public Transform[] ghosts;
        public Vector3[] origins;
        public SpeedTimeGraphPlot plot;
        public TMP_Text promptLabel, statusLabel, questionLabel;
        public Button playButton,resetButton,nextButton;
        public Button[] questionButtons;
        public Slider scrubber;
        public Camera uiCamera;
        public int questionIndex,mode;
        public float time,worldScale=.62f;
        ChallengeCatalog catalog;
        int dragged=-1;
        bool updatingSlider;
        public float Duration => points[points.Length-1].x;
        void Awake()
        {
            catalog=JsonUtility.FromJson<ChallengeCatalog>(questionData.text);
            playButton.onClick.AddListener(Play);resetButton.onClick.AddListener(ResetRun);nextButton.onClick.AddListener(()=>SelectQuestion((questionIndex+1)%catalog.questions.Length));
            for(int i=0;i<questionButtons.Length;i++) { int q=i;questionButtons[i].onClick.AddListener(()=>SelectQuestion(q)); }
            scrubber.onValueChanged.AddListener(v=> { if(mode==2&&!updatingSlider) { time=v*Duration;PositionVehicles(time);plot.SetVerticesDirty();UpdateStatus(); } });
            SelectQuestion(0);
        }
        public void SelectQuestion(int index)
        {
            questionIndex=index; mode=0;time=0;
            points=index==2 ? new[]{new Vector2(0,0),new Vector2(2,8),new Vector2(4,8),new Vector2(6,3),new Vector2(10,3),new Vector2(12,0)} : new[]{new Vector2(0,0),new Vector2(3,5),new Vector2(7,5),new Vector2(10,0)};
            var q=catalog.questions[index];worldScale=28.5f/q.targetDistanceM;plot.maxTime=index==2?12:10;plot.maxSpeed=index==0?8:12; promptLabel.text=q.prompt;questionLabel.text=$"QUESTION {index+1} / 3  •  {q.title.ToUpperInvariant()}";
            if(index==2)promptLabel.text=$"Gate at {q.speedGate.distanceM:0} m: >= {q.speedGate.minSpeedMps:0} m/s   |   Slow Zone {q.slowZone.startM:0}-{q.slowZone.endM:0} m: <= {q.slowZone.maxSpeedMps:0} m/s   |   Stop at {q.targetDistanceM:0} m.";
            for(int i=0;i<targets.Length;i++)
            {
                targets[i].position=new Vector3(origins[i].x+q.targetDistanceM*worldScale,targets[i].position.y,origins[i].z);
                speedGates[i].gameObject.SetActive(index==2);slowZones[i].gameObject.SetActive(index==2);
                if(index==2) { speedGates[i].position=new Vector3(origins[i].x+q.speedGate.distanceM*worldScale,.55f,origins[i].z); slowZones[i].position=new Vector3(origins[i].x+(q.slowZone.startM+q.slowZone.endM)*.5f*worldScale,.56f,origins[i].z);var size=slowZones[i].localScale;size.x=(q.slowZone.endM-q.slowZone.startM)*worldScale;slowZones[i].localScale=size; }
            }
            ResetRun();
        }
        public float Speed(float t)
        {
            for(int i=0;i<points.Length-1;i++) if(t<=points[i+1].x) return Mathf.Lerp(points[i].y,points[i+1].y,Mathf.Clamp01((t-points[i].x)/(points[i+1].x-points[i].x)));
            return points[points.Length-1].y;
        }
        public float Distance(float t)
        {
            float d=0;
            for(int i=0;i<points.Length-1;i++) { float dt=Mathf.Clamp(t-points[i].x,0,points[i+1].x-points[i].x); float a=(points[i+1].y-points[i].y)/(points[i+1].x-points[i].x);d+=points[i].y*dt+.5f*a*dt*dt; }
            return d;
        }
        void PositionVehicles(float t) { float d=Distance(t)*worldScale;for(int i=0;i<vehicles.Length;i++) vehicles[i].position=origins[i]+Vector3.right*d; }
        void Preview()
        {
            for(int i=0;i<ghosts.Length;i++) { int lane=i/3;float t=Duration*(i%3+1)/3f;ghosts[i].position=origins[lane]+Vector3.right*Distance(t)*worldScale; }
            plot.SetVerticesDirty();UpdateStatus();
        }
        public void Play() { mode=1; time=0;PositionVehicles(0);playButton.interactable=false;nextButton.interactable=false; }
        public void ResetRun() { mode=0;time=0;PositionVehicles(0);playButton.interactable=true;nextButton.interactable=true;scrubber.interactable=false;Preview(); }
        void Update()
        {
            if(mode==1)
            {
                time=Mathf.Min(time+Time.deltaTime,Duration);PositionVehicles(time);plot.SetVerticesDirty();UpdateStatus();
                updatingSlider=true;scrubber.value=time/Duration;updatingSlider=false;
                if(time>=Duration) { mode=2;playButton.interactable=true;nextButton.interactable=true;scrubber.interactable=true;statusLabel.text=Feedback(); }
            }
            if(mode!=0||Mouse.current==null) return;
            var mouse=Mouse.current;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(plot.rectTransform,mouse.position.ReadValue(),uiCamera,out var local);
            if(mouse.leftButton.wasPressedThisFrame)
                for(int i=1;i<points.Length;i++) if(Vector2.Distance(local,plot.Pixel(points[i]))<22) { dragged=i;break; }
            if(mouse.leftButton.wasReleasedThisFrame) dragged=-1;
            if(dragged>=0&&mouse.leftButton.isPressed)
            {
                var r=plot.rectTransform.rect;var p=points[dragged];p.y=Mathf.Clamp(Mathf.Round((local.y-r.yMin)/r.height*plot.maxSpeed*2)/2,0,plot.maxSpeed);
                if(catalog.questions[questionIndex].allowHorizontalPointEditing) p.x=Mathf.Clamp(Mathf.Round((local.x-r.xMin)/r.width*plot.maxTime*2)/2,points[dragged-1].x+.5f,dragged==points.Length-1?plot.maxTime:points[dragged+1].x-.5f);
                points[dragged]=p;Preview();
            }
        }
        void UpdateStatus() { statusLabel.text=mode==0?$"Drag the glowing points  •  Predicted distance: {Distance(Duration):0.0} m":$"Time {time:0.0} s   •   Speed {Speed(time):0.0} m/s   •   Distance {Distance(time):0.0} m"; }
        public string Feedback()
        {
            var q=catalog.questions[questionIndex];float d=Distance(Duration);
            if(Speed(Duration)>.01f) return "All three are still moving. End your graph at zero speed.";
            if(Mathf.Abs(d-q.targetDistanceM)>q.toleranceM) return d<q.targetDistanceM?"All three stopped early. Your graph needs more area.":"All three passed the target. Reduce the graph area.";
            if(questionIndex==0)
            {
                bool rise=false,flat=false,fall=false;
                for(int i=0;i<points.Length-1;i++) { float slope=points[i+1].y-points[i].y;rise|=slope>.01f;flat|=Mathf.Abs(slope)<.01f&&points[i].y>0;fall|=slope<-.01f; }
                if(!rise||!flat||!fall) return "Include a rising, flat and falling section.";
            }
            if(questionIndex==2)
            {
                float gateTime=TimeAtDistance(q.speedGate.distanceM);
                if(Speed(gateTime)<q.speedGate.minSpeedMps-.001f) return "Speed Gate: reach at least 6 m/s.";
                float a=TimeAtDistance(q.slowZone.startM),b=TimeAtDistance(q.slowZone.endM);
                if(Speed(a)>q.slowZone.maxSpeedMps+.001f||Speed(b)>q.slowZone.maxSpeedMps+.001f) return "Slow Zone: stay at or below 4 m/s.";
                foreach(var p in points) if(p.x>a&&p.x<b&&p.y>q.slowZone.maxSpeedMps+.001f) return "Slow Zone: stay at or below 4 m/s.";
            }
            return "Great! All three vehicles matched the motion target.";
        }
        float TimeAtDistance(float d) { float a=0,b=Duration;for(int i=0;i<35;i++) { float m=(a+b)*.5f;if(Distance(m)<d)a=m;else b=m; } return (a+b)*.5f; }
    }
}
