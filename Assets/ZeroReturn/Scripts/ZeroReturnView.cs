using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ZeroReturn
{
    public class ZeroReturnView : MonoBehaviour
    {
        public static readonly Color Ink = new Color(.07f, .12f, .11f);
        public static readonly Color Muted = new Color(.38f, .45f, .42f);
        public static readonly Color Amber = new Color(.96f, .68f, .32f);
        public static readonly Color Cyan = new Color(.34f, .78f, .93f);
        public static readonly Color Red = new Color(.72f, .2f, .16f);
        public static readonly Color Paper = new Color(.94f, .96f, .94f);
        Font font;
        public Canvas canvas;
        public RectTransform hud, popup, quiz, toastLayer, assembler;
        public Text pathText, displacementText, positionText, carryText, violationText, levelText, instructionText, summaryText, blueprintText;
        public RectTransform needle;
        public Image pathCell, dispCell;
        public Button left, right, returnButton, help;
        public GameObject modal;
        public RectTransform modalPanel;
        public CanvasGroup modalGroup;
        public List<Button> sphereButtons = new List<Button>(), socketButtons = new List<Button>();
        public List<Text> socketLabels = new List<Text>();
        float cursor;
        Action<int> launchAction, placeAction;

        public void Build(Action<int> launch, Action<int> heading, Action<int> place, Action home, Action showHelp)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            launchAction = launch; placeAction = place;
            var root = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            hud = Layer(root.transform, "HUDLayer", 0); popup = Layer(root.transform, "PopupLayer", 10);
            quiz = Layer(root.transform, "QuizLayer", 20); toastLayer = Layer(root.transform, "ToastLayer", 30);
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Panel(hud, "Header", new Vector2(800, 45), new Vector2(1600, 90), Paper);
            Label(hud, "ZERO RETURN", new Vector2(205, 36), new Vector2(330, 45), 32, Ink, TextAnchor.MiddleLeft, true);
            Label(hud, "MOTION IN A STRAIGHT LINE", new Vector2(220, 68), new Vector2(360, 20), 12, Muted, TextAnchor.MiddleLeft);
            levelText = Label(hud, "CONCEPT 01", new Vector2(1240, 42), new Vector2(450, 40), 19, Ink, TextAnchor.MiddleRight);
            help = Button(hud, "?", new Vector2(1535, 43), new Vector2(48, 48), showHelp);
            var instrument = Panel(hud, "InstrumentPanel", new Vector2(800, 159), new Vector2(1490, 114), Ink);
            pathCell = Panel(instrument, "Scalar", new Vector2(-586, 0), new Vector2(290, 100), Color.clear, true).GetComponent<Image>();
            dispCell = Panel(instrument, "Vector", new Vector2(-279, 0), new Vector2(300, 100), Color.clear, true).GetComponent<Image>();
            Label(instrument, "PATH LENGTH  /  s", new Vector2(-600, -34), new Vector2(250, 22), 12, Amber, TextAnchor.MiddleLeft, false, true);
            pathText = Label(instrument, "0.0 m", new Vector2(-600, 0), new Vector2(250, 40), 32, Amber, TextAnchor.MiddleLeft, true, true);
            Label(instrument, "scalar - never decreases", new Vector2(-600, 35), new Vector2(250, 20), 12, new Color(.65f,.72f,.68f), TextAnchor.MiddleLeft, false, true);
            Label(instrument, "DISPLACEMENT  /  Dx", new Vector2(-294, -34), new Vector2(270, 22), 12, Cyan, TextAnchor.MiddleLeft, false, true);
            displacementText = Label(instrument, "+0.0 m", new Vector2(-294, 0), new Vector2(270, 40), 32, Cyan, TextAnchor.MiddleLeft, true, true);
            var track = Panel(instrument, "NeedleTrack", new Vector2(-300, 37), new Vector2(220, 3), Muted, true);
            Panel(track, "Origin", Vector2.zero, new Vector2(1, 14), Color.white, true);
            needle = Panel(track, "Needle", Vector2.zero, new Vector2(4, 16), Cyan, true);
            positionText = Metric(instrument, "POSITION  /  x", "0.0 m", 30, Color.white);
            carryText = Metric(instrument, "CARRY  /  1 CLAW", "EMPTY", 300, Color.white);
            violationText = Metric(instrument, "REASONING PIPS", "3 / 3", 570, Color.white);
            instructionText = Label(hud, "", new Vector2(800, 248), new Vector2(1400, 34), 22, Ink, TextAnchor.MiddleCenter);
            assembler = Panel(hud, "Assembler", new Vector2(800, 744), new Vector2(660, 248), new Color(.98f,.99f,.98f,.97f));
            blueprintText = Label(assembler, "ASSEMBLER", new Vector2(0, -98), new Vector2(610, 30), 17, Ink, TextAnchor.MiddleCenter, true, true);
            Label(hud, "SET HEADING", new Vector2(226, 683), new Vector2(320, 30), 14, Muted, TextAnchor.MiddleLeft);
            left = Button(hud, "<  NEGATIVE", new Vector2(153, 731), new Vector2(180, 52), () => heading(-1));
            right = Button(hud, "POSITIVE  >", new Vector2(345, 731), new Vector2(180, 52), () => heading(1));
            returnButton = Button(hud, "RETURN TO DOCK", new Vector2(1350, 731), new Vector2(305, 54), home);
            Label(hud, "Click a sphere to fetch it.\nAt the dock, choose a socket.", new Vector2(1345, 803), new Vector2(330, 80), 17, Muted, TextAnchor.MiddleCenter);
            Label(hud, "A / D: heading\n1 - 4: choose sphere   Space: return", new Vector2(250, 806), new Vector2(375, 62), 15, Muted, TextAnchor.MiddleLeft);
            summaryText = Label(toastLayer, "", new Vector2(800, 295), new Vector2(1240, 54), 22, Ink, TextAnchor.MiddleCenter, true);
        }

        Text Metric(Transform parent, string title, string value, float x, Color color)
        {
            Label(parent, title, new Vector2(x, -34), new Vector2(240, 24), 12, new Color(.65f,.72f,.68f), TextAnchor.MiddleLeft, false, true);
            return Label(parent, value, new Vector2(x, 8), new Vector2(240, 52), 27, color, TextAnchor.MiddleLeft, true, true);
        }

        RectTransform Layer(Transform parent, string name, int order)
        {
            var rt = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)).GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            var c = rt.GetComponent<Canvas>(); c.overrideSorting = true; c.sortingOrder = order;
            return rt;
        }

        public void LoadLevel(LevelInstance level)
        {
            foreach (var b in sphereButtons) Destroy(b.gameObject);
            foreach (var b in socketButtons) Destroy(b.gameObject);
            sphereButtons.Clear(); socketButtons.Clear(); socketLabels.Clear();
            foreach (Transform child in assembler) if (child.name == "BlueprintEdge") Destroy(child.gameObject);
            blueprintText.text = level.shape == ShapeType.Square ? "THE SQUARE  /  REQUIRED SIDE d = " + level.requiredSide + " m" : "THE " + level.shape.ToString().ToUpperInvariant() + "  /  MATCH THE STAMPS";
            Vector2[] points = level.shape == ShapeType.Line ? new[] { new Vector2(-180, 22), new Vector2(180, 22) }
                : level.shape == ShapeType.Triangle ? new[] { new Vector2(-190, 62), new Vector2(0, -31), new Vector2(190, 62) }
                : new[] { new Vector2(-160, -31), new Vector2(160, -31), new Vector2(-160, 66), new Vector2(160, 66) };
            if (level.rotated && level.shape == ShapeType.Triangle)
                for (int i = 0; i < points.Length; i++) points[i].y = 32 - points[i].y;
            if (level.rotated && level.shape == ShapeType.Square)
                points = new[] { new Vector2(0,-46), new Vector2(180,22), new Vector2(-180,22), new Vector2(0,90) };
            Edge(points[0], points[1]);
            if (points.Length == 3) { Edge(points[1], points[2]); Edge(points[2], points[0]); }
            if (points.Length == 4) { Edge(points[2], points[3]); Edge(points[0], points[2]); Edge(points[1], points[3]); }
            for (int i=0; i<level.coordinates.Length; i++)
            {
                int index = i;
                var fetch = Button(hud, "FETCH " + Signed(level.coordinates[i]), new Vector2(800 + (i-(level.coordinates.Length-1)/2f)*174, 584), new Vector2(164,42), () => launchAction(index));
                sphereButtons.Add(fetch);
                string caption = level.shape == ShapeType.Square ? "SOCKET " + (i+1) : "x = " + Signed(level.coordinates[level.socketOrder[i]]);
                var socket = Button(assembler, caption, points[i], new Vector2(level.shape==ShapeType.Square ? 132 : 160, 46), () => placeAction(index), true);
                socketButtons.Add(socket); socketLabels.Add(socket.GetComponentInChildren<Text>());
            }
            left.gameObject.SetActive(level.levelIndex >= 1 || level.modifier == Modifier.HeadingGate);
            right.gameObject.SetActive(left.gameObject.activeSelf);
        }

        void Edge(Vector2 a, Vector2 b)
        {
            var line = Panel(assembler, "BlueprintEdge", (a+b)*.5f, new Vector2(Vector2.Distance(a,b), 2), new Color(.65f,.71f,.68f), true);
            line.localRotation = Quaternion.Euler(0,0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
        }

        public void UpdateModel(AssemblyModel model, bool enabled, int heading)
        {
            for (int i=0; i<sphereButtons.Count; i++)
            {
                sphereButtons[i].interactable = enabled && !model.collected[i];
                socketButtons[i].interactable = enabled && model.carry>=0 && model.sockets[i]<0;
                if (model.sockets[i]>=0) { socketLabels[i].text = "x = " + Signed(model.level.coordinates[model.sockets[i]]); socketButtons[i].image.color = new Color(.72f,.84f,.76f); }
                else { socketLabels[i].text = model.level.shape==ShapeType.Square ? "SOCKET "+(i+1) : "x = "+Signed(model.level.coordinates[model.level.socketOrder[i]]); socketButtons[i].image.color = Color.white; }
            }
            left.image.color = heading==-1 ? new Color(.72f,.84f,.76f) : Color.white;
            right.image.color = heading==1 ? new Color(.72f,.84f,.76f) : Color.white;
            left.interactable = right.interactable = enabled;
            returnButton.interactable = enabled;
        }

        public void BeginModal(string title, bool isQuiz = false, float height = 620)
        {
            HideModal();
            var layer = isQuiz ? quiz : popup;
            var backdrop = Panel(layer, "ModalBackdrop", new Vector2(800,450), new Vector2(8000,8000), new Color(0,0,0,.75f));
            backdrop.GetComponent<Image>().raycastTarget = true;
            modal = backdrop.gameObject;
            modalPanel = Panel(backdrop, "Panel", Vector2.zero, new Vector2(560,height), Paper, true);
            modalGroup = modalPanel.gameObject.AddComponent<CanvasGroup>();
            cursor = -height*.5f + 30;
            ModalText(title, 62, 30, Ink, true);
        }

        public Text ModalText(string text, float height=80, int size=20, Color? color=null, bool bold=false)
        {
            var label = Label(modalPanel,text,new Vector2(0,cursor+height/2),new Vector2(504,height),size,color??Ink,TextAnchor.MiddleLeft,bold,true);
            cursor += height + 10;
            return label;
        }
        public Button ModalButton(string text, Action action, float height=47)
        {
            var b=Button(modalPanel,text,new Vector2(0,cursor+height/2),new Vector2(504,height),action,true);
            cursor+=height+10; return b;
        }
        public void HideModal() { if(modal!=null) { modal.SetActive(false); Destroy(modal); } modal=null; }

        public RectTransform Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color,bool relative=false)
        {
            var rt=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
            rt.SetParent(parent,false); rt.anchorMin=rt.anchorMax=relative?new Vector2(.5f,.5f):new Vector2(.5f,1);
            rt.anchoredPosition=relative?new Vector2(position.x,-position.y):new Vector2(position.x-800,-position.y);
            rt.sizeDelta=size; rt.GetComponent<Image>().color=color; rt.GetComponent<Image>().raycastTarget=false; return rt;
        }
        public Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize,Color color,TextAnchor alignment=TextAnchor.MiddleCenter,bool bold=false,bool relative=false)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(Text)); var rt=go.GetComponent<RectTransform>();
            rt.SetParent(parent,false); rt.anchorMin=rt.anchorMax=relative?new Vector2(.5f,.5f):new Vector2(.5f,1);
            rt.anchoredPosition=relative?new Vector2(position.x,-position.y):new Vector2(position.x-800,-position.y); rt.sizeDelta=size;
            var t=go.GetComponent<Text>(); t.font=font; t.text=text; t.fontSize=fontSize; t.color=color; t.alignment=alignment;
            t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal; t.raycastTarget=false; t.supportRichText=true; t.horizontalOverflow=HorizontalWrapMode.Wrap; return t;
        }
        public Button Button(Transform parent,string text,Vector2 position,Vector2 size,Action action,bool relative=false)
        {
            var rt=Panel(parent,"Button "+text,position,size,Color.white,relative); rt.GetComponent<Image>().raycastTarget=true;
            var b=rt.gameObject.AddComponent<Button>(); b.targetGraphic=rt.GetComponent<Image>(); b.onClick.AddListener(()=>action());
            var colors=b.colors; colors.highlightedColor=new Color(.82f,.89f,.84f); colors.pressedColor=new Color(.65f,.79f,.7f); colors.disabledColor=new Color(.78f,.81f,.79f,.6f); b.colors=colors;
            Label(rt,text,Vector2.zero,size-new Vector2(16,4),18,Ink,TextAnchor.MiddleCenter,true,true); return b;
        }
        public static string Signed(float x) => x.ToString("+0.##;-0.##;0");
    }
}
