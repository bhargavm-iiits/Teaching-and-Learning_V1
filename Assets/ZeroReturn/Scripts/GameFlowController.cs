using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZeroReturn
{
    [DefaultExecutionOrder(200)]
    public class GameFlowController : MonoBehaviour
    {
        public ConceptDefinition concept;
        public TuningProfile tuning;
        public RailDressing dressing;
        public GameState State { get; private set; }
        public event Action<GameState,GameState> OnStateChanged;
        public event Action<TripRecord> TripCompleted;
        public ZeroReturnWorld World { get; private set; }
        public ZeroReturnView View { get; private set; }
        public AssemblyModel Model { get; private set; }
        public LevelInstance Instance { get; private set; }
        public IReadOnlyList<LevelResult> Results => results;
        public int Heading { get; private set; }
        public bool InputReady => State==GameState.LevelPlay && !helpOpen && !transitioning;
        public int Attempt => progress.Attempt;
        public string Phase => phase;
        ProgressionService progress;
        readonly List<LevelResult> results=new List<LevelResult>();
        readonly List<TripRecord> trips=new List<TripRecord>();
        LevelInstance[] previous=new LevelInstance[3];
        QuestionInstance[] questions;
        readonly List<bool> answers=new List<bool>();
        int levelIndex, selected=-1, questionIndex, targetIndex=-1;
        bool answerLocked, helpOpen, transitioning;
        string phase="Docked";
        float elapsed, tripTime, tripPathStart, toastUntil, idle;
        TripRecord currentTrip;
        AudioSource audioSource;
        AudioClip dockClick;
        const string AmberHex="#F2AD55",CyanHex="#58C8EE";

        void Awake()
        {
            if(concept==null || tuning==null || dressing==null) { Debug.LogError("Zero Return needs its concept, tuning and dressing assets."); enabled=false;return; }
            progress=new ProgressionService(concept.id);
            World=new GameObject("World").AddComponent<ZeroReturnWorld>(); World.transform.SetParent(transform,false); World.Build(dressing,tuning.botSpeed);
            View=gameObject.AddComponent<ZeroReturnView>(); View.Build(Launch,SetHeading,Place,ReturnHome,ShowHelp);
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.volume=.12f;
            dockClick=AudioClip.Create("Dock Click",2205,1,44100,false);var samples=new float[2205];
            for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*2*Mathf.PI*660/44100)*Mathf.Exp(-i/330f);dockClick.SetData(samples,0);
            BuildLevel(); ShowTitle();
        }

        void Go(GameState state)
        {
            var old=State;State=state;World.bot.paused=state!=GameState.LevelPlay;OnStateChanged?.Invoke(old,state);
        }
        public void ShowTitle()
        {
            Go(GameState.Title);View.BeginModal("ZERO RETURN",false,530);
            View.ModalText("One rail. Every trip comes home.\nTwo meters tell different stories.",105,25);
            View.ModalText(concept.title+"\n"+concept.syllabus,90,17,ZeroReturnView.Muted);
            View.ModalButton(progress.Complete?"PLAY AGAIN":"PLAY",ShowConcept);
        }
        public void ShowConcept()
        {
            Go(GameState.ConceptCard);View.BeginModal(concept.title,false,600);
            View.ModalText(concept.explanation,160,22);
            View.ModalText("<color="+AmberHex+">PATH LENGTH adds every metre.</color>\n<color="+CyanHex+">DISPLACEMENT = final - initial position.</color>",95,21);
            View.ModalText("Fetch one sphere. Return to zero. Place it in the assembler. Build three figures, then answer three questions.",90,18);
            View.ModalButton("BEGIN",()=>{levelIndex=0;results.Clear();BuildLevel();ShowBrief();});
        }
        void BuildLevel()
        {
            string saved=PlayerPrefs.GetString("ZeroReturn.Last."+concept.id+"."+levelIndex,"");
            if(!string.IsNullOrEmpty(saved)) { try { previous[levelIndex]=JsonUtility.FromJson<LevelInstance>(saved); } catch { previous[levelIndex]=null; } }
            int seed=VariantGenerator.Seed(concept.id,progress.PlayerId,progress.Attempt,levelIndex);
            var prior=previous[levelIndex];
            Instance=prior!=null && prior.seed==seed ? prior : VariantGenerator.Build(concept.levels[levelIndex],seed,progress.Attempt,prior);
            previous[levelIndex]=Instance;PlayerPrefs.SetString("ZeroReturn.Last."+concept.id+"."+levelIndex,JsonUtility.ToJson(Instance));
            Model=new AssemblyModel(Instance);World.Load(Instance);View.LoadLevel(Instance);
            elapsed=tripTime=idle=0;Heading=0;phase="Docked";targetIndex=-1;trips.Clear();currentTrip=null;
            View.summaryText.text="";
            View.levelText.text="LEVEL "+(levelIndex+1)+" / 3   -   ATTEMPT "+(progress.Attempt+1);
            RefreshHUD();
        }
        string Rules()
        {
            string extra=Instance.modifier==Modifier.NearestFirst?"Fetch the sphere nearest to zero first."
                :levelIndex==2?"Set a heading. Form two pairs separated by d = "+Instance.requiredSide+" m. Both spheres eject if the pair is wrong."
                :levelIndex==1 || Instance.modifier==Modifier.HeadingGate?"Choose NEGATIVE or POSITIVE before each launch.":"Match each coordinate stamp to its socket.";
            return "1  Carry one sphere at a time.\n2  Every fetch starts at the dock (x = 0).\n3  Deposit before the next fetch.\n\n"+extra;
        }
        public void ShowBrief()
        {
            Go(GameState.LevelBrief);View.BeginModal("LEVEL "+(levelIndex+1)+"  /  THE "+Instance.shape.ToString().ToUpperInvariant(),false,640);
            View.ModalText(concept.levels[levelIndex].objective,80,23);
            View.ModalText(Rules(),205,20);
            View.ModalText("Rail calibration: one tick = "+Instance.unitsPerTick+" m\nSpheres: "+string.Join("   ",Instance.coordinates.Select(ZeroReturnView.Signed))
                +(levelIndex==2?"\nRequired side d = "+Instance.requiredSide+" m":""),80,18,ZeroReturnView.Muted);
            View.ModalButton("START",()=>StartCoroutine(DismissBrief()));
        }
        IEnumerator DismissBrief()
        {
            if(transitioning)yield break;transitioning=true;
            var group=View.modalGroup;group.interactable=false;float time=0;
            while(time<tuning.briefDismissSeconds)
            {
                time+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(time/tuning.briefDismissSeconds);
                group.alpha=1-t;group.transform.localScale=Vector3.one*Mathf.Lerp(1,.96f,t);yield return null;
            }
            View.HideModal();transitioning=false;Go(GameState.LevelPlay);
        }

        public void SetHeading(int heading)
        {
            if(!InputReady || World.bot.IsMoving)return;Heading=heading;idle=0;
        }
        public void Launch(int index)
        {
            if(!InputReady || index<0 || index>=Instance.coordinates.Length)return;
            if(World.bot.IsMoving){Toast("The bot is already on a trip.");return;}
            string error=Model.CanLaunch(index,Heading,World.bot.Coordinate);
            if(error!=null)
            {
                Toast(error);
                if(Model.carry<0 && Mathf.Abs(World.bot.Coordinate)>.01f){phase="Returning";World.bot.MoveTo(0);}
                return;
            }
            idle=0;targetIndex=index;
            if(progress.Attempt>=2)StartCoroutine(PreviewThenLaunch());else BeginTrip();
        }
        IEnumerator PreviewThenLaunch()
        {
            transitioning=true;World.ghost.gameObject.SetActive(true);
            for(float t=0;t<1.8f;t+=Time.deltaTime)
            {
                float f=t/.9f;float x=Mathf.Lerp(0,Instance.coordinates[targetIndex],f<=1?f:2-f);
                World.ghost.position=World.rail.ToWorld(x);yield return null;
            }
            World.ghost.gameObject.SetActive(false);transitioning=false;BeginTrip();
        }
        void BeginTrip()
        {
            currentTrip=new TripRecord{target=Instance.coordinates[targetIndex]};currentTrip.samples.Add(0);
            tripPathStart=World.motion.PathLength;tripTime=0;World.motion.BeginTrip();phase="Outbound";World.bot.MoveTo(Instance.coordinates[targetIndex]);
        }
        public void ReturnHome()
        {
            if(!InputReady || World.bot.IsMoving)return;
            if(Mathf.Abs(World.bot.Coordinate)<.01f){Toast(Model.carry>=0?"Choose a socket to deposit your sphere.":"You are docked. Choose a sphere to fetch.");return;}
            phase="Returning";World.bot.MoveTo(0);
        }
        public void Place(int socket)
        {
            if(!InputReady || World.bot.IsMoving || Mathf.Abs(World.bot.Coordinate)>.01f)return;
            if(socket<0 || socket>=Model.sockets.Length)return;
            var error=Model.Place(socket,out int[] ejected);
            foreach(int index in ejected)World.Eject(index,Instance);
            World.RefreshAssembly(Model);idle=0;
            if(error!=null)Toast(error);else Toast("Sphere placed. "+Model.sockets.Count(x=>x>=0)+" / "+Model.sockets.Length+" sockets filled.");
            if(Model.Complete)StartCoroutine(CompleteLevel());
        }
        IEnumerator CompleteLevel()
        {
            Go(GameState.LevelComplete);yield return new WaitForSeconds(.55f);
            var result=new LevelResult{instance=Instance,path=World.motion.PathLength,displacement=World.motion.RunDisplacement,seconds=elapsed,violations=Model.violations,trips=new List<TripRecord>(trips)};
            results.Add(result);
            View.BeginModal("THE "+Instance.shape.ToString().ToUpperInvariant()+" IS COMPLETE",false,630);
            View.ModalText(new string('*',result.Stars)+"   "+result.Stars+" / 3 STARS",48,26);
            View.ModalText("FIGURE "+(Instance.shape==ShapeType.Square?"SIDE":"SEPARATION")+"     "+Instance.FigureSize+" m",60,24);
            View.ModalText("PATH WALKED     "+result.path.ToString("0.0")+" m",55,26,ZeroReturnView.Amber,true);
            View.ModalText("NET DISPLACEMENT     "+ZeroReturnView.Signed(result.displacement)+" m",55,26,ZeroReturnView.Cyan,true);
            View.ModalText(concept.levels[levelIndex].lesson,110,20);
            View.ModalButton("CONTINUE",ContinueLevel);
        }
        public void ContinueLevel()
        {
            if(State!=GameState.LevelComplete)return;
            if(++levelIndex<concept.levels.Length){BuildLevel();ShowBrief();}
            else
            {
                Go(GameState.QuizIntro);View.BeginModal("THREE QUESTIONS",false,470);
                View.ModalText("All three, and the concept is yours.\nNo timer. Select an option, then lock your answer.",130,23);
                View.ModalText("These questions use the journeys you just completed.",65,19,ZeroReturnView.Muted);
                View.ModalButton("BEGIN QUIZ",BeginQuiz);
            }
        }
        public void BeginQuiz()
        {
            answers.Clear();questionIndex=0;var seen=progress.Seen;
            questions=concept.questions.Draw(results[0],new System.Random(Instance.seed),seen,progress.Attempt);progress.SaveSeen(seen);ShowQuestion();
        }
        void ShowQuestion()
        {
            Go(GameState.QuizQuestion);selected=-1;answerLocked=false;
            var q=questions[questionIndex];View.BeginModal("QUESTION "+(questionIndex+1)+" / 3  -  "+PoolName(q.pool),true,780);
            View.ModalText(q.prompt,122,21);
            var options=new List<Button>();Button confirm=null;
            for(int i=0;i<4;i++)
            {
                int choice=i;
                var b=View.ModalButton(((char)('A'+i))+"   "+q.options[i],()=>{},52);
                options.Add(b);
                b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>
                {
                    if(answerLocked)return;selected=choice;
                    for(int j=0;j<options.Count;j++)options[j].image.color=j==selected?new Color(.7f,.83f,.75f):Color.white;
                    confirm.interactable=true;
                });
            }
            confirm=View.ModalButton("LOCK ANSWER",()=>
            {
                if(selected<0 || answerLocked)return;answerLocked=true;bool correct=selected==q.correct;answers.Add(correct);
                foreach(var b in options)b.interactable=false;confirm.interactable=false;
                options[q.correct].image.color=new Color(.55f,.8f,.62f);
                if(!correct)options[selected].image.color=new Color(.89f,.64f,.61f);
                View.ModalText((correct?"Correct. ":"Not quite. ")+q.explanation,86,18);
                View.ModalButton(questionIndex==2?"SEE RESULTS":"NEXT",()=>{questionIndex++;if(questionIndex<3)ShowQuestion();else ShowQuizResult();});
            });
            confirm.interactable=false;
        }
        void ShowQuizResult()
        {
            Go(GameState.QuizResult);int score=answers.Count(x=>x);View.BeginModal(score+" / 3  -  YOUR RESULTS",true,650);
            for(int i=0;i<3;i++)View.ModalText(PoolName(questions[i].pool)+": "+(answers[i]?"correct":"review needed")+"\n"+questions[i].explanation,112,19);
            View.ModalText(score>=tuning.passThreshold?"Concept cleared.":"Let's replay one of your own trips, then try a fresh variant.",70,20);
            View.ModalButton(score>=tuning.passThreshold?"CONTINUE":"REVIEW MY TRIP",()=>{if(score>=tuning.passThreshold)ShowConceptComplete();else StartCoroutine(Remediate());});
        }
        IEnumerator Remediate()
        {
            Go(GameState.Remediation);
            string missed=string.Join(", ",questions.Where((q,i)=>!answers[i]).Select(q=>PoolName(q.pool)));
            View.BeginModal("WATCH YOUR OWN TRIP",true,550);
            View.ModalText("Review: "+missed+"\nThe bot leaves zero, then returns. Watch how path length keeps growing while displacement returns to zero.",133,20);
            var meters=View.ModalText("",110,23);
            var button=View.ModalButton("REPLAYING...",Retry);button.interactable=false;
            var trip=results[0].trips[0];var samples=trip.samples;
            var paths=new float[samples.Count];for(int i=1;i<samples.Count;i++)paths[i]=paths[i-1]+Mathf.Abs(samples[i]-samples[i-1]);
            World.ghost.gameObject.SetActive(true);
            for(float t=0;t<tuning.replaySeconds;t+=Time.unscaledDeltaTime)
            {
                int i=Mathf.Min(samples.Count-1,Mathf.FloorToInt(t/tuning.replaySeconds*samples.Count));
                World.ghost.position=World.rail.ToWorld(samples[i]);
                meters.text="<color="+AmberHex+">Path: "+paths[i].ToString("0.0")+" m</color>\n<color="+CyanHex+">Displacement: "+ZeroReturnView.Signed(samples[i]-samples[0])+" m</color>";
                yield return null;
            }
            World.ghost.gameObject.SetActive(false);meters.text="<color="+AmberHex+">Path: "+trip.pathLength.ToString("0.0")+" m</color>\n<color="+CyanHex+">Displacement: 0 m</color>";
            button.GetComponentInChildren<Text>().text="TRY A NEW VARIANT";button.interactable=true;
        }
        public void Retry()
        {
            progress.Fail();levelIndex=0;results.Clear();BuildLevel();
            Debug.Log("Zero Return remediation: attempt "+(progress.Attempt+1));
            if(progress.Attempt>=3)ShowConcept();else ShowBrief();
        }
        void ShowConceptComplete()
        {
            progress.MarkComplete();Go(GameState.ConceptComplete);View.BeginModal("CONCEPT COMPLETE",true,470);
            View.ModalText("You returned to zero, but your journey was not zero.\n\nPath length counts the route. Displacement compares the endpoints.",185,24);
            if(concept.nextConcept!=null)View.ModalButton("NEXT CONCEPT",()=>{concept=concept.nextConcept;progress=new ProgressionService(concept.id);previous=new LevelInstance[3];levelIndex=0;results.Clear();ShowConcept();});
            else View.ModalButton("PRACTISE A NEW VARIANT",()=>{progress.NewPractice();levelIndex=0;results.Clear();BuildLevel();ShowBrief();});
        }
        public void ShowHelp()
        {
            if(!InputReady || transitioning)return;helpOpen=true;World.bot.paused=true;
            View.BeginModal("THE RULES",false,500);View.ModalText(Rules(),230,21);
            View.ModalButton("RESUME",()=>{helpOpen=false;World.bot.paused=false;View.HideModal();});
        }
        void Update()
        {
            if(!InputReady)return;
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.aKey.wasPressedThisFrame)SetHeading(-1);if(keyboard.dKey.wasPressedThisFrame)SetHeading(1);
                if(keyboard.spaceKey.wasPressedThisFrame)ReturnHome();
                if(keyboard.digit1Key.wasPressedThisFrame)Launch(0);if(keyboard.digit2Key.wasPressedThisFrame)Launch(1);
                if(keyboard.digit3Key.wasPressedThisFrame)Launch(2);if(keyboard.digit4Key.wasPressedThisFrame)Launch(3);
            }
            var mouse=Mouse.current;
            if(mouse!=null && mouse.leftButton.wasPressedThisFrame && (EventSystem.current==null || !EventSystem.current.IsPointerOverGameObject()))
                if(Physics.Raycast(World.sceneCamera.ScreenPointToRay(mouse.position.ReadValue()),out var hit,100) && World.picks.TryGetValue(hit.collider,out int index))Launch(index);
        }
        void LateUpdate()
        {
            if(Model==null)return;
            if(InputReady)
            {
                elapsed+=Time.deltaTime;idle+=Time.deltaTime;
                if(currentTrip!=null){tripTime+=Time.deltaTime;currentTrip.samples.Add(World.bot.Coordinate);}
                if(!World.bot.IsMoving && phase=="Outbound")
                {
                    if(Model.Grab(targetIndex)){World.Carry(targetIndex);phase="AtSphere";Toast("Sphere collected. Return to the dock.");}
                }
                else if(!World.bot.IsMoving && phase=="Returning")
                {
                    phase="Docked";Heading=0;
                    if(currentTrip!=null)
                    {
                        currentTrip.pathLength=World.motion.PathLength-tripPathStart;currentTrip.displacement=World.motion.TripDisplacement;currentTrip.seconds=tripTime;
                        trips.Add(currentTrip);TripCompleted?.Invoke(currentTrip);
                        Toast("<color="+AmberHex+">Path "+currentTrip.pathLength.ToString("0.0")+" m</color>   |   <color="+CyanHex+">Dx "+ZeroReturnView.Signed(currentTrip.displacement)+" m</color>",progress.Attempt>=2?2.4f:tuning.summarySeconds);
                        currentTrip=null;audioSource.PlayOneShot(dockClick);
                    }
                }
            }
            RefreshHUD();
            if(Time.unscaledTime>toastUntil)View.summaryText.text="";
        }
        void RefreshHUD()
        {
            View.pathText.text=World.motion.PathLength.ToString("0.0")+" m";View.displacementText.text=World.motion.TripDisplacement.ToString("+0.0;-0.0;+0.0")+" m";
            View.positionText.text=World.bot.Coordinate.ToString("+0.0;-0.0;0.0")+" m"+(Mathf.Abs(World.bot.Coordinate)<.01f?"\ndocked":"");
            View.carryText.text=Model.carry<0?"EMPTY":"x = "+ZeroReturnView.Signed(Instance.coordinates[Model.carry]);
            View.violationText.text=Mathf.Max(0,tuning.violationBudget-Model.violations)+" / "+tuning.violationBudget+" pips";
            var p=View.needle.anchoredPosition;p.x=Mathf.Clamp(World.motion.TripDisplacement/12,-1,1)*110;View.needle.anchoredPosition=p;
            View.instructionText.text=State==GameState.LevelPlay?(World.bot.IsMoving?"Travelling "+(phase=="Returning"?"home to zero":"to x = "+ZeroReturnView.Signed(Instance.coordinates[targetIndex]))
                :phase=="AtSphere"?"Return home with your sphere.":Model.carry>=0?"Deposit the sphere in a socket.":"Choose a sphere to fetch.  One tick = "+Instance.unitsPerTick+" m"):"";
            View.UpdateModel(Model,InputReady && !World.bot.IsMoving,Heading);
            float pulse=(Mathf.Sin(Time.unscaledTime*4)+1)*.06f;
            View.pathCell.color=progress.Attempt>=1 && trips.Count==0?new Color(1,.65f,.3f,pulse):Color.clear;
            View.dispCell.color=progress.Attempt>=1 && trips.Count==0?new Color(.3f,.8f,1,pulse):Color.clear;
            if(progress.Attempt>=3 && idle>=tuning.guidedHintSeconds && Model.carry>=0)
                for(int i=0;i<Model.sockets.Length;i++)
                    if(Model.sockets[i]<0 && (Instance.shape!=ShapeType.Square?Instance.socketOrder[i]==Model.carry:
                        Model.sockets[i^1]<0 || Mathf.Approximately(Mathf.Abs(Instance.coordinates[Model.carry]-Instance.coordinates[Model.sockets[i^1]]),Instance.requiredSide)))
                    {View.socketButtons[i].image.color=new Color(.55f,.78f,.62f);break;}
        }
        void Toast(string text,float seconds=3){View.summaryText.text=text;toastUntil=Time.unscaledTime+seconds;}
        static string PoolName(string pool)=>pool=="S"?"Distance vs displacement":pool=="P"?"Position":"Direction";
        void OnDestroy(){if(dockClick!=null)Destroy(dockClick);}
    }
}
