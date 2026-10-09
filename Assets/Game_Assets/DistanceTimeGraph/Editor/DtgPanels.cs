using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DistanceTimeGraph.EditorTools
{
    /// The contents of each panel, styled after the reference picture: glowing cyan frames on deep blue, a screen bezel
    /// with a title tab for the behind-car camera, a stone signboard for the activity and chunky console buttons.
    public static partial class DtgBuilder
    {
        static Image Frame(Transform parent, Vector2 size, Color fill)
        {
            UiKit.Panel(parent, "Glow", Palette.Cyan.WithAlpha(0.28f), Vector2.zero, size + new Vector2(22, 22));
            UiKit.Panel(parent, "Edge", Palette.Cyan, Vector2.zero, size + new Vector2(8, 8));
            return UiKit.Panel(parent, "Frame", fill, Vector2.zero, size);
        }

        // ---------------- graph ----------------

        static void BuildGraphPanel(UiRefs ui, Transform canvas, DtgCatalog catalog)
        {
            Frame(canvas, new Vector2(680, 500), new Color(0.04f, 0.2f, 0.45f, 0.93f));
            UiKit.Text(canvas, "Title", "DISTANCE FROM START–TIME GRAPH", 30, Color.white, new Vector2(10, 220), new Vector2(660, 42), TextAlignmentOptions.Center, true);
            var hint = UiKit.Text(canvas, "Hint", "Press the grid to add a point", 16, Palette.SoftInk, new Vector2(10, 191), new Vector2(640, 24));

            const float W = 540, H = 318;
            var plotImg = UiKit.Panel(canvas, "Plot", new Color(0.02f, 0.1f, 0.26f, 0.75f), new Vector2(34, 6), new Vector2(W, H), false, true);
            var plot = plotImg.rectTransform;
            var grid = UiKit.Rect(plot, "Grid", Vector2.zero, new Vector2(W, H));
            for (int t = 0; t <= catalog.maxTime; t++)
            {
                float x = -W / 2 + W * t / catalog.maxTime;
                bool major = t % 2 == 0;
                var line = UiKit.Panel(grid, "V" + t, major ? Palette.Grid : Palette.Grid.WithAlpha(0.25f), new Vector2(x, 0), new Vector2(t == 0 ? 4 : 1.5f, H), false);
                if (t == 0) line.color = Color.white;
                if (major) UiKit.Text(grid, "T" + t, t.ToString(), 19, Color.white, new Vector2(x, -H / 2 - 17), new Vector2(40, 26), TextAlignmentOptions.Center, true);
            }
            for (int d = 0; d <= catalog.maxDistance; d += 5)
            {
                float y = -H / 2 + H * d / catalog.maxDistance;
                var line = UiKit.Panel(grid, "H" + d, d % 10 == 0 ? Palette.Grid : Palette.Grid.WithAlpha(0.25f), new Vector2(0, y), new Vector2(W, d == 0 ? 4 : 1.5f), false);
                if (d == 0) line.color = Color.white;
                if (d % 20 == 0) UiKit.Text(grid, "D" + d, d.ToString(), 19, Color.white, new Vector2(-W / 2 - 22, y), new Vector2(40, 26), TextAlignmentOptions.Right, true);
            }
            UiKit.Text(canvas, "TimeAxis", "TIME (s)", 20, Color.white, new Vector2(34, 6 - H / 2 - 42), new Vector2(200, 28), TextAlignmentOptions.Center, true);
            var yAxis = UiKit.Text(canvas, "DistanceAxis", "DISTANCE FROM START (m)", 18, Color.white, new Vector2(-302, 6), new Vector2(330, 26), TextAlignmentOptions.Center, true);
            yAxis.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            UiKit.Text(canvas, "Legend", "<color=#29DBFF>rising: moves away</color>   ·   <color=#FFD63D>flat: waits</color>   ·   <color=#FF8C1F>falling: comes back</color>",
                14, Color.white, new Vector2(34, -232), new Vector2(620, 22));

            var ge = plot.gameObject.AddComponent<GraphEditor>();
            ge.plot = plot;
            ge.circle = UiKit.Circle;
            ge.hintText = hint;
            ge.bandLayer = UiKit.Rect(plot, "BandLayer", Vector2.zero, new Vector2(W, H));
            ge.ghostLayer = UiKit.Rect(plot, "GhostLayer", Vector2.zero, new Vector2(W, H));
            ge.lineLayer = UiKit.Rect(plot, "LineLayer", Vector2.zero, new Vector2(W, H));
            ge.pointLayer = UiKit.Rect(plot, "PointLayer", Vector2.zero, new Vector2(W, H));
            ge.overlayLayer = UiKit.Rect(plot, "OverlayLayer", Vector2.zero, new Vector2(W, H));
            ui.graph = ge;
        }

        // ---------------- behind-car camera monitor ----------------

        static void BuildMonitor(UiRefs ui, Transform canvas)
        {
            Frame(canvas, new Vector2(640, 350), new Color(0.03f, 0.15f, 0.36f, 0.95f));
            var tab = UiKit.Panel(canvas, "TitleTab", new Color(0.04f, 0.22f, 0.5f, 1f), new Vector2(-150, 170), new Vector2(330, 46));
            UiKit.Text(tab.transform, "Title", "BEHIND-CAR CAMERA", 25, Color.white, Vector2.zero, new Vector2(320, 40), TextAlignmentOptions.Center, true);
            var screen = UiKit.Rect(canvas, "Screen", new Vector2(0, -12), new Vector2(604, 292));
            ui.monitorImage = screen.gameObject.AddComponent<RawImage>();
            ui.monitorImage.raycastTarget = false;
            var pill = UiKit.Panel(canvas, "LivePill", new Color(0.05f, 0.05f, 0.08f, 0.92f), new Vector2(252, 110), new Vector2(92, 36));
            ui.liveDot = UiKit.Dot(pill.transform, "LiveDot", Palette.Red, 18);
            ui.liveDot.rectTransform.anchoredPosition = new Vector2(-26, 0);
            UiKit.Text(pill.transform, "Live", "LIVE", 19, Color.white, new Vector2(12, 0), new Vector2(58, 30), TextAlignmentOptions.Center, true);
            ui.monitorMode = UiKit.Text(canvas, "Mode", "LIVE", 15, Palette.Cyan, new Vector2(-230, -140), new Vector2(120, 22), TextAlignmentOptions.Left, true);
            ui.monitorDistance = UiKit.Text(canvas, "Distance", "0.0 m from start", 16, Color.white, new Vector2(200, -140), new Vector2(200, 24), TextAlignmentOptions.Right, true);
        }

        // ---------------- activity signboard ----------------

        static void BuildActivity(UiRefs ui, Transform canvas)
        {
            const float W = 820, H = 640;
            // A physical signboard: stone frame and two posts standing on the platform, with a small blue flag.
            float halfW = W * 0.0005f, halfH = H * 0.0005f;
            var frame = new GameObject("ActivitySignFrame").transform;
            frame.SetParent(canvas.parent, false);
            frame.SetPositionAndRotation(canvas.position, canvas.rotation);
            Box(frame, "Back", new Vector3(0, 0, 0.03f), new Vector3(W * 0.001f + 0.12f, H * 0.001f + 0.12f, 0.05f), Mats.Stone);
            Box(frame, "Cap", new Vector3(0, halfH + 0.08f, 0.02f), new Vector3(W * 0.001f + 0.22f, 0.07f, 0.12f), Mats.Stone);
            float postLen = Mathf.Max(0.1f, canvas.position.y - halfH - PlayerFeet.y);
            foreach (float x in new[] { -halfW - 0.02f, halfW + 0.02f })
            {
                Box(frame, "Post", new Vector3(x, -halfH - postLen * 0.5f, 0.03f), new Vector3(0.09f, postLen, 0.09f), Mats.Stone);
                Box(frame, "PostTop", new Vector3(x, halfH + 0.16f, 0.03f), new Vector3(0.12f, 0.12f, 0.12f), Mats.Stone);
            }
            Box(frame, "FlagPole", new Vector3(-halfW - 0.02f, halfH + 0.42f, 0.03f), new Vector3(0.025f, 0.45f, 0.025f), Mats.Rail);
            var flag = new GameObject("SignFlag", typeof(MeshFilter), typeof(MeshRenderer));
            flag.transform.SetParent(frame, false);
            flag.transform.localPosition = new Vector3(-halfW - 0.02f, halfH + 0.62f, 0.03f);
            flag.transform.localScale = Vector3.one * 0.3f;
            flag.GetComponent<MeshFilter>().sharedMesh = PennantMesh();
            flag.GetComponent<MeshRenderer>().sharedMaterial = Mats.Blue;

            UiKit.Panel(canvas, "Board", new Color(0.07f, 0.27f, 0.6f, 1f), Vector2.zero, new Vector2(W, H));
            var a = canvas.gameObject.AddComponent<ActivityPanel>();
            // Snowy mountain emblem above the title.
            var emblem = UiKit.Rect(canvas, "Emblem", new Vector2(0, 268), new Vector2(120, 56));
            var peak2 = UiKit.Panel(emblem, "PeakBack", new Color(0.55f, 0.75f, 1f), new Vector2(30, -18), new Vector2(34, 34), false);
            peak2.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var peak = UiKit.Panel(emblem, "Peak", Color.white, new Vector2(-4, -10), new Vector2(48, 48), false);
            peak.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            UiKit.Panel(emblem, "Cut", new Color(0.07f, 0.27f, 0.6f, 1f), new Vector2(6, -40), new Vector2(150, 28), false);

            a.title = UiKit.Text(canvas, "Title", "LEVEL 1 · REACH THE 40 m GATE", 52, Color.white, new Vector2(0, 172), new Vector2(W - 50, 124), TextAlignmentOptions.Center, true);
            var card = UiKit.Panel(canvas, "Card", new Color(0.96f, 0.97f, 1f), new Vector2(0, 22), new Vector2(W - 50, 170));
            a.instruction = UiKit.Text(card.transform, "Instruction", "Reach the 40 m gate.", 44, new Color(0.05f, 0.14f, 0.36f), Vector2.zero, new Vector2(W - 80, 160), TextAlignmentOptions.Center, true);
            a.instruction.enableAutoSizing = true;   // long instructions (Level 2, 3) shrink to fit the card
            a.instruction.fontSizeMin = 26;
            a.instruction.fontSizeMax = 44;
            a.status = UiKit.Text(canvas, "Status", "t = 0.0 s", 28, Color.white, new Vector2(0, -88), new Vector2(W - 50, 38), TextAlignmentOptions.Center, true);
            a.prediction = UiKit.Text(canvas, "Prediction", "", 27, Palette.SoftInk, new Vector2(0, -140), new Vector2(W - 50, 64), TextAlignmentOptions.Center, true);
            a.bannerBack = UiKit.Panel(canvas, "Banner", Palette.Blue, new Vector2(0, H / 2 + 30), new Vector2(460, 44));
            a.banner = UiKit.Text(a.bannerBack.transform, "Text", "NEW ROUTE", 24, Color.white, Vector2.zero, new Vector2(450, 40), TextAlignmentOptions.Center, true);
            UiKit.Text(canvas, "CardsTitle", "LAST ATTEMPTS", 20, Palette.SoftInk, new Vector2(-W / 2 + 150, -186), new Vector2(260, 26), TextAlignmentOptions.Left, true);
            a.cards = new GameObject[3];
            a.cardGraphs = new MiniGraph[3];
            a.cardCaptions = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                var cardGo = UiKit.Panel(canvas, "Attempt_" + i, new Color(0.03f, 0.14f, 0.34f, 0.95f), new Vector2(-260 + i * 260, -250), new Vector2(250, 100));
                var area = UiKit.Panel(cardGo.transform, "Graph", new Color(1, 1, 1, 0.06f), new Vector2(-62, 0), new Vector2(110, 80), false).rectTransform;
                var mg = cardGo.gameObject.AddComponent<MiniGraph>();
                mg.area = area;
                a.cardGraphs[i] = mg;
                a.cardCaptions[i] = UiKit.Text(cardGo.transform, "Caption", "", 16, Palette.Orange, new Vector2(60, 0), new Vector2(120, 92), TextAlignmentOptions.Center);
                a.cardCaptions[i].enableAutoSizing = true;
                a.cardCaptions[i].fontSizeMin = 11;
                a.cardCaptions[i].fontSizeMax = 16;
                a.cards[i] = cardGo.gameObject;
            }
            ui.activity = a;
        }

        // ---------------- console ----------------

        static void BuildConsole(UiRefs ui, Transform canvas)
        {
            // The buttons sit on a dark desk with a glowing front edge.
            var desk = new GameObject("ConsoleDesk").transform;
            desk.SetParent(canvas.parent, false);
            desk.SetPositionAndRotation(canvas.position, canvas.rotation);
            Box(desk, "Top", new Vector3(0, 0, 0.025f), new Vector3(0.72f, 0.14f, 0.04f), Mats.Dark);
            Box(desk, "Glow", new Vector3(0, -0.075f, 0.02f), new Vector3(0.72f, 0.012f, 0.045f), Mats.Cyan);
            Box(desk, "Body", new Vector3(0, -0.3f, 0.12f), new Vector3(0.7f, 0.45f, 0.2f), Mats.Platform);

            var c = canvas.gameObject.AddComponent<ControlConsole>();
            float x = -330 + 10;
            Button B(string label, float w, Color color)
            {
                var b = UiKit.Button(canvas, label.Replace(" ", ""), label, new Vector2(x + w / 2, 0), new Vector2(w, 78), color, label.Length > 7 ? 22 : 21);
                x += w + 8;
                return b;
            }
            c.run = B("RUN GRAPH", 152, new Color(0.14f, 0.7f, 0.28f));
            c.pause = B("PAUSE", 92, new Color(0.34f, 0.38f, 0.48f));
            c.edit = B("EDIT", 84, Palette.Blue);
            c.replay = B("REPLAY", 100, new Color(0.96f, 0.52f, 0.1f));
            c.reset = B("RESET", 86, new Color(0.5f, 0.56f, 0.66f));
            c.delete = B("DELETE", 90, new Color(0.78f, 0.24f, 0.24f));
            ui.console = c;
        }

        // ---------------- popups ----------------

        static void BuildPopup(UiRefs ui, Transform canvas)
        {
            var root = UiKit.Rect(canvas, "Popup", Vector2.zero, new Vector2(700, 460));
            var p = root.gameObject.AddComponent<PopupPanel>();
            p.group = root.gameObject.AddComponent<CanvasGroup>();
            Frame(root, new Vector2(700, 460), new Color(0.03f, 0.13f, 0.32f, 0.97f));
            p.accentBar = UiKit.Panel(root, "Accent", Palette.Green, new Vector2(0, 216), new Vector2(660, 8));
            p.title = UiKit.Text(root, "Title", "LEVEL 1 COMPLETE", 40, Palette.Green, new Vector2(0, 168), new Vector2(660, 62), TextAlignmentOptions.Center, true);
            p.body = UiKit.Text(root, "Body", "", 26, Color.white, new Vector2(0, 34), new Vector2(640, 200), TextAlignmentOptions.Center);
            p.buttons = new Button[3];
            for (int i = 0; i < 3; i++)
                p.buttons[i] = UiKit.Button(root, "Button_" + i, "OK", new Vector2(0, -170), new Vector2(310, 72), i == 0 ? new Color(0.14f, 0.7f, 0.28f) : Palette.Blue, 22);
            ui.popup = p;
        }

        // ---------------- final check ----------------

        static void BuildAssessment(UiRefs ui, Transform canvas)
        {
            var root = UiKit.Rect(canvas, "Assessment", Vector2.zero, new Vector2(1000, 760));
            var a = root.gameObject.AddComponent<AssessmentManager>();
            Frame(root, new Vector2(1000, 760), new Color(0.03f, 0.13f, 0.32f, 0.97f));
            a.title = UiKit.Text(root, "Title", Logic.Assessment.Title, 34, Palette.Yellow, new Vector2(0, 340), new Vector2(940, 50), TextAlignmentOptions.Center, true);
            var qs = Logic.Assessment.Questions();
            for (int q = 0; q < 3; q++)
            {
                float y = 262 - q * 150;
                a.prompts[q] = UiKit.Text(root, "Prompt_" + q, qs[q].prompt, 23, Color.white, new Vector2(-40, y), new Vector2(860, 40), TextAlignmentOptions.Left, true);
                a.marks[q] = UiKit.Text(root, "Mark_" + q, "", 19, Palette.Green, new Vector2(420, y), new Vector2(140, 34), TextAlignmentOptions.Right, true);
                for (int o = 0; o < 3; o++)
                    a.options[q * 3 + o] = UiKit.Button(root, $"Option_{q}_{o}", qs[q].options[o], new Vector2(-315 + o * 315, y - 62), new Vector2(300, 64), new Color(0.1f, 0.24f, 0.5f), 18);
            }
            a.submit = UiKit.Button(root, "Submit", "SUBMIT ANSWERS", new Vector2(0, -200), new Vector2(340, 72), new Color(0.14f, 0.7f, 0.28f), 25);
            a.submitHint = UiKit.Text(root, "SubmitHint", "Answer every question (3 left).", 19, Palette.SoftInk, new Vector2(0, -252), new Vector2(600, 28));
            var result = UiKit.Panel(root, "Result", new Color(0.05f, 0.16f, 0.36f, 0.97f), new Vector2(0, -278), new Vector2(940, 170));
            a.resultRoot = result.gameObject;
            a.resultTitle = UiKit.Text(result.transform, "Title", "TOPIC COMPLETE", 34, Palette.Green, new Vector2(-170, 40), new Vector2(560, 50), TextAlignmentOptions.Left, true);
            a.resultMessage = UiKit.Text(result.transform, "Message", "", 21, Color.white, new Vector2(-170, -22), new Vector2(560, 70), TextAlignmentOptions.TopLeft);
            a.resultButton = UiKit.Button(result.transform, "Route", "NEXT TOPIC", new Vector2(300, 0), new Vector2(300, 78), Palette.Blue, 22);
            result.gameObject.SetActive(false);
            ui.assessment = a;
        }
    }
}
