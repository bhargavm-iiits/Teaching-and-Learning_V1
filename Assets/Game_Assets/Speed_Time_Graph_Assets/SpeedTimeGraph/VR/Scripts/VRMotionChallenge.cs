using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace SpeedTimeGraphLesson.VR
{
    public class VRMotionChallenge : MonoBehaviour
    {
        public TextAsset questionData;
        public Vector2[] profile = { new Vector2(0,1),new Vector2(1,1.4f),new Vector2(2,1.4f),new Vector2(4,0) };
        // Serialized lane order: car, train, motorcycle.
        public float[] startingSpeeds = {20,30,10};
        public Transform[] vehicles,targets,ghosts,speedGates,slowZones;
        public Vector3[] origins;
        public VRGraphPlot plot;
        public TMP_Text title,questionText,status,readout,axisLabel;
        public TMP_Text[] vehicleLabels,targetLabels;
        public Button play,pause,reset,restore,next,recenter;
        public Button[] questions,vehicleSelectors,stationButtons;
        public Slider timeline;
        public VRPlayerStation player;
        public AudioSource feedbackAudio;
        public Camera eye;
        public int questionIndex,selectedVehicle,mode;
        public float time;
        ChallengeCatalog catalog;
        bool updatingTimeline;
        float nextLabelUpdate;
        AudioClip clickTone,successTone;
        public float Duration => profile[profile.Length-1].x;
        public float SpeedScale => startingSpeeds[selectedVehicle];
        public ChallengeQuestion Question => catalog.questions[questionIndex];
        public bool CanEdit => mode==0;

        void Awake()
        {
            InitializeData();
            play.onClick.AddListener(Play);pause.onClick.AddListener(TogglePause);reset.onClick.AddListener(ResetRun);
            restore.onClick.AddListener(()=>SelectQuestion(questionIndex));next.onClick.AddListener(()=>SelectQuestion((questionIndex+1)%3));
            recenter.onClick.AddListener(()=>player.RecenterConsole());
            for(int i=0;i<3;i++) { int index=i;questions[i].onClick.AddListener(()=>SelectQuestion(index));vehicleSelectors[i].onClick.AddListener(()=>SelectVehicle(index));stationButtons[i].onClick.AddListener(()=>player.Visit(index)); }
            timeline.onValueChanged.AddListener(v=> { if((mode==2||mode==3)&&!updatingTimeline)Scrub(v); });
            clickTone=Tone(550,.065f);successTone=Tone(880,.22f);
            SelectQuestion(0);
        }
        public void InitializeData() { catalog=JsonUtility.FromJson<ChallengeCatalog>(questionData.text); }
        public void SelectQuestion(int index)
        {
            if(catalog==null)InitializeData();
            questionIndex=Mathf.Clamp(index,0,2);mode=0;time=0;
            profile=index==2?new[]{new Vector2(0,1),new Vector2(1,1.4f),new Vector2(2,1.4f),new Vector2(3,.3f),new Vector2(6,.3f),new Vector2(7,0)}:
                index==1?new[]{new Vector2(0,1),new Vector2(2,1.4f),new Vector2(4,1.4f),new Vector2(6,0)}:
                new[]{new Vector2(0,1),new Vector2(1,1.4f),new Vector2(2,1.4f),new Vector2(4,0)};
            plot.maxTime=index==0?6:index==1?8:12;
            questionText.text=$"QUESTION {index+1}: {Question.title}\nStop targets: bike {TargetDistance(2):0} m / car {TargetDistance(0):0} m / train {TargetDistance(1):0} m";
            if(index==2)questionText.text+="\nBike gate: >=6 m/s at 15 m; slow zone: <=4 m/s at 30-38 m. Car values x2; train x3.";
            else questionText.text+=index==0?"\nCreate a rise, a steady section and a fall to zero.":"\nThe shaded graph area is the distance travelled.";
            for(int lane=0;lane<3;lane++)
            {
                targets[lane].position=new Vector3(origins[lane].x+TargetDistance(lane),targets[lane].position.y,origins[lane].z);
                targetLabels[lane].text=$"STOP\n{TargetDistance(lane):0} m";
                speedGates[lane].gameObject.SetActive(index==2);slowZones[lane].gameObject.SetActive(index==2);
                if(index==2)
                {
                    speedGates[lane].position=new Vector3(origins[lane].x+Question.speedGate.distanceM*Ratio(lane),.55f,origins[lane].z);
                    slowZones[lane].position=new Vector3(origins[lane].x+(Question.slowZone.startM+Question.slowZone.endM)*.5f*Ratio(lane),.56f,origins[lane].z);
                    var scale=slowZones[lane].localScale;scale.x=(Question.slowZone.endM-Question.slowZone.startM)*Ratio(lane);slowZones[lane].localScale=scale;
                }
            }
            SelectVehicle(selectedVehicle);ResetRun();Sound(false);
        }
        public void SelectVehicle(int index)
        {
            selectedVehicle=Mathf.Clamp(index,0,2);plot.maxSpeed=startingSpeeds.MaxValue()*2;
            title.text=$"{Name(selectedVehicle).ToUpperInvariant()} SPEED-TIME GRAPH";
            axisLabel.text=$"Speed (m/s)  |  Starts: bike 10 / car 20 / train 30";
            plot.SetAllDirty();UpdateLabels();Sound(false);
        }
        public static string Name(int lane) => lane==0?"Car":lane==1?"Train":"Bike";
        public float Ratio(int lane) => startingSpeeds[lane]/startingSpeeds[2];
        public float TargetDistance(int lane) => Question.targetDistanceM*Ratio(lane);
        public float NormalizedSpeed(float t)
        {
            for(int i=0;i<profile.Length-1;i++)if(t<=profile[i+1].x)return Mathf.Lerp(profile[i].y,profile[i+1].y,Mathf.Clamp01((t-profile[i].x)/(profile[i+1].x-profile[i].x)));
            return profile[profile.Length-1].y;
        }
        public float Integral(float t)
        {
            float area=0;
            for(int i=0;i<profile.Length-1;i++) { float dt=Mathf.Clamp(t-profile[i].x,0,profile[i+1].x-profile[i].x);float a=(profile[i+1].y-profile[i].y)/(profile[i+1].x-profile[i].x);area+=profile[i].y*dt+.5f*a*dt*dt; }
            return area;
        }
        public float Speed(int lane,float t)=>startingSpeeds[lane]*NormalizedSpeed(t);
        public float Distance(int lane,float t)=>startingSpeeds[lane]*Integral(t);
        public void PositionVehicles(float t) { for(int lane=0;lane<3;lane++)vehicles[lane].position=origins[lane]+Vector3.right*Distance(lane,t); }
        public void GraphChanged()
        {
            for(int i=0;i<ghosts.Length;i++)ghosts[i].position=origins[i/3]+Vector3.right*Distance(i/3,Duration*(i%3+1)/3f);
            plot.SetAllDirty();UpdateLabels();
        }
        public void EditPoint(int index,Vector2 local)
        {
            if(!CanEdit||index<=0||index>=profile.Length)return;
            var r=plot.rectTransform.rect;var p=profile[index];float displayedSpeed=Mathf.Clamp((local.y-r.yMin)/r.height*plot.maxSpeed,0,startingSpeeds[selectedVehicle]*2);
            p.y=Mathf.Round(displayedSpeed*2)/2/startingSpeeds[selectedVehicle];
            if(Question.allowHorizontalPointEditing)p.x=Mathf.Clamp(Mathf.Round((local.x-r.xMin)/r.width*plot.maxTime*4)/4,profile[index-1].x+.25f,index==profile.Length-1?plot.maxTime:profile[index+1].x-.25f);
            profile[index]=p;GraphChanged();status.text=$"{Name(selectedVehicle)} point: {p.x:0.00} s, {p.y*SpeedScale:0.0} m/s. All three profiles update.";
        }
        public void Play()
        {
            if(mode==3)mode=1;else { mode=1;time=0;PositionVehicles(0); }
            play.interactable=false;pause.interactable=true;timeline.interactable=false;UpdateButtonLabels();Sound(false);Haptic(.15f);
        }
        public void TogglePause()
        {
            if(mode==1) { mode=3;play.interactable=true;timeline.interactable=true; }else if(mode==3)Play();
            UpdateButtonLabels();Sound(false);
        }
        public void ResetRun()
        {
            mode=0;time=0;PositionVehicles(0);play.interactable=true;pause.interactable=false;timeline.interactable=false;next.interactable=true;
            updatingTimeline=true;timeline.value=0;updatingTimeline=false;GraphChanged();UpdateButtonLabels();
        }
        public void Scrub(float normalizedTime)
        {
            time=Mathf.Clamp01(normalizedTime)*Duration;PositionVehicles(time);plot.SetAllDirty();UpdateLabels();
        }
        void Update()
        {
            if(mode!=1)return;
            time=Mathf.Min(Duration,time+Time.deltaTime);PositionVehicles(time);plot.SetVerticesDirty();
            updatingTimeline=true;timeline.value=time/Duration;updatingTimeline=false;
            if(Time.unscaledTime>=nextLabelUpdate) { nextLabelUpdate=Time.unscaledTime+.08f;UpdateLabels(); }
            if(time>=Duration)
            {
                mode=2;play.interactable=true;pause.interactable=false;timeline.interactable=true;UpdateLabels();status.text=Feedback();UpdateButtonLabels();
                bool success=status.text.StartsWith("Great!");Sound(success);Haptic(success ? .4f : .1f);
            }
        }
        public void UpdateLabels()
        {
            if(readout==null)return;
            float at=mode==0?0:time;
            readout.text=$"<color=#3DD2FF>CAR {Speed(0,at):0.0} m/s</color>   <color=#FF7373>TRAIN {Speed(1,at):0.0} m/s</color>   <color=#FFB144>BIKE {Speed(2,at):0.0} m/s</color>\n"+
                $"Time {time:0.00} s | Distance: car {Distance(0,at):0.0} m / train {Distance(1,at):0.0} m / bike {Distance(2,at):0.0} m";
            for(int i=0;i<3;i++)if(vehicleLabels[i])vehicleLabels[i].text=$"{Name(i).ToUpperInvariant()}\n{Speed(i,at):0.0} m/s";
            if(mode==0)status.text=$"Trigger-drag a glowing point. Predicted stops: car {Distance(0,Duration):0.0} m / train {Distance(1,Duration):0.0} m / bike {Distance(2,Duration):0.0} m";
        }
        void UpdateButtonLabels()
        {
            play.GetComponentInChildren<TMP_Text>().text=mode==3?"RESUME":"RUN ALL THREE";
            pause.GetComponentInChildren<TMP_Text>().text=mode==3?"RESUME":"PAUSE";
        }
        public string Feedback()
        {
            if(NormalizedSpeed(Duration)>.001f)return "End the graph at zero so all three vehicles stop.";
            float d=Distance(2,Duration);
            if(Mathf.Abs(d-Question.targetDistanceM)>Question.toleranceM)return d<Question.targetDistanceM?"All three stopped short. Add graph area.":"All three overshot their targets. Reduce graph area.";
            if(questionIndex==0)
            {
                bool rise=false,flat=false,fall=false;
                for(int i=0;i<profile.Length-1;i++) { float dy=profile[i+1].y-profile[i].y;rise|=dy>.001f;flat|=Mathf.Abs(dy)<.001f&&profile[i].y>0;fall|=dy<-.001f; }
                if(!rise||!flat||!fall)return "Include rising, steady and falling sections.";
            }
            if(questionIndex==2)
            {
                if(Speed(2,TimeAtDistance(Question.speedGate.distanceM))<Question.speedGate.minSpeedMps-.001f)return "Speed Gate missed. Increase speed before the gate.";
                float a=TimeAtDistance(Question.slowZone.startM),b=TimeAtDistance(Question.slowZone.endM);
                if(Speed(2,a)>Question.slowZone.maxSpeedMps+.001f||Speed(2,b)>Question.slowZone.maxSpeedMps+.001f)return "Slow Zone: brake earlier and keep the speed lower.";
                foreach(var p in profile)if(p.x>a&&p.x<b&&p.y*startingSpeeds[2]>Question.slowZone.maxSpeedMps+.001f)return "Slow Zone: keep the entire section below the limit.";
            }
            return "Great! Bike, car and train stopped at their individual targets.";
        }
        float TimeAtDistance(float d) { float a=0,b=Duration;for(int i=0;i<35;i++) { float m=(a+b)*.5f;if(Distance(2,m)<d)a=m;else b=m; }return (a+b)*.5f; }
        void Sound(bool success) { if(feedbackAudio&&clickTone)feedbackAudio.PlayOneShot(success?successTone:clickTone,.18f); }
        public static void Haptic(float strength)
        {
            foreach(var node in new[]{XRNode.LeftHand,XRNode.RightHand}) { var device=InputDevices.GetDeviceAtXRNode(node);if(device.TryGetHapticCapabilities(out var cap)&&cap.supportsImpulse)device.SendHapticImpulse(0,strength,.04f); }
        }
        static AudioClip Tone(float frequency,float seconds)
        {
            int n=(int)(22050*seconds);var samples=new float[n];for(int i=0;i<n;i++)samples[i]=Mathf.Sin(2*Mathf.PI*frequency*i/22050f)*Mathf.Sin(Mathf.PI*i/n)*.15f;
            var clip=AudioClip.Create("Graph feedback",n,1,22050,false);clip.SetData(samples,0);return clip;
        }
    }
    static class SpeedArrayExtensions { public static float MaxValue(this float[] values) { float m=0;foreach(float v in values)m=Mathf.Max(v,m);return m; } }
}
