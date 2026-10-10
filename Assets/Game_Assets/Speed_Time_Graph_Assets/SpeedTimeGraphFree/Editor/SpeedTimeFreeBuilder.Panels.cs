using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Object = UnityEngine.Object;

namespace SpeedTimeGraphFree
{
    /// <summary>World-space panels placed so the 16:9 desktop view matches the reference composition.</summary>
    public static partial class SpeedTimeFreeBuilder
    {
        public static readonly Color Navy = new Color(.039f, .165f, .353f, .85f), Cyan = new Color(.157f, .863f, 1f), NavyButton = new Color(.06f, .27f, .55f, .96f);
        static GraphController controller; static FeedbackPresenter presenter; static PopupManager popupManager; static FinalCheckManager finalManager; static ChecklistPanel checklistPanel;
        static PlaybackController playbackController;

        // Reference screen rects (normalised, y from the top) and distances (metres).
        public static readonly Rect GraphRect = Rect.MinMaxRect(.005f, .14f, .262f, .86f);
        public static readonly Rect TitleRect = Rect.MinMaxRect(.22f, .012f, .78f, .155f);
        public static readonly Rect StepsRect = Rect.MinMaxRect(.795f, .035f, .995f, .44f);
        public static readonly Rect BannerRect = Rect.MinMaxRect(.02f, .775f, .33f, .845f);
        public static readonly Rect ReviewRect = Rect.MinMaxRect(.02f, .68f, .33f, .765f);
        public static readonly Rect PopupRect = Rect.MinMaxRect(.355f, .37f, .645f, .63f);
        public static readonly Rect FinalRect = Rect.MinMaxRect(.3f, .42f, .74f, .97f);

        static RectTransform UICanvas(string name, Transform parent, Rect screen, float distance, float pixelWidth, bool interactive, float faceEye = 0)
        {
            var (pos, rot, w, h) = ScreenRect(screen.xMin, screen.yMin, screen.xMax, screen.yMax, distance, faceEye);
            float scale = w / pixelWidth; float pixelHeight = h / scale;
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); go.layer = 5;
            var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(pixelWidth, pixelHeight);
            rt.position = pos; rt.rotation = rot; rt.localScale = Vector3.one * scale;
            var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = eye; canvas.sortingOrder = 1;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.dynamicPixelsPerUnit = 3;
            if (interactive) { go.AddComponent<GraphicRaycaster>(); go.AddComponent<TrackedDeviceGraphicRaycaster>(); }
            return rt;
        }
        /// <summary>World-space canvas of a given physical size, placed at yaw/elevation/distance around the eye and facing it.</summary>
        static RectTransform WorldCanvas(string name, Transform parent, float yawFromDeck, float elevation, float distance, float widthMeters, float heightMeters, float pixelWidth)
        {
            var dir = Quaternion.Euler(-elevation, EyeYaw + yawFromDeck, 0) * Vector3.forward;
            var pos = Eye + dir * distance;
            float scale = widthMeters / pixelWidth;
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); go.layer = 5;
            var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(pixelWidth, heightMeters / scale);
            rt.position = pos; rt.rotation = Quaternion.LookRotation(pos - Eye, Vector3.up); rt.localScale = Vector3.one * scale;
            var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = eye; canvas.sortingOrder = 1;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 3;
            go.AddComponent<GraphicRaycaster>(); go.AddComponent<TrackedDeviceGraphicRaycaster>();
            return rt;
        }

        static RectTransform UIRect(string name, Transform parent, Vector2 size, Vector2 topLeft)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); go.layer = 5;
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = size; rt.anchoredPosition = new Vector2(topLeft.x + size.x * .5f, -topLeft.y - size.y * .5f);
            return rt;
        }
        static Image Img(string name, Transform parent, Vector2 size, Vector2 topLeft, Color c, Sprite s = null)
        {
            var rt = UIRect(name, parent, size, topLeft); var img = rt.gameObject.AddComponent<Image>(); img.color = c; img.raycastTarget = false;
            if (s) { img.sprite = s; img.type = Image.Type.Sliced; }
            return img;
        }
        static void Frame(RectTransform panel, float alpha = .85f)
        {
            var size = panel.sizeDelta;
            var g = Img("Cyan glow border", panel, size + new Vector2(70, 70), new Vector2(-35, -35), new Color(Cyan.r, Cyan.g, Cyan.b, .95f), glow); g.transform.SetAsFirstSibling();
            var bg = Img("Navy holographic panel", panel, size, Vector2.zero, new Color(Navy.r, Navy.g, Navy.b, alpha), rounded); bg.transform.SetSiblingIndex(1);
            var edge = Img("Cyan edge", panel, size, Vector2.zero, new Color(Cyan.r, Cyan.g, Cyan.b, .9f), ring); edge.transform.SetSiblingIndex(2);
        }
        static TextMeshProUGUI Text(string name, Transform parent, string text, Vector2 size, Vector2 topLeft, float px, bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null)
        {
            var rt = UIRect(name, parent, size, topLeft); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = bold ? fontBold : fontBody; t.fontSize = Mathf.Max(30, px); t.text = text; t.alignment = align; t.color = color ?? Color.white;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }
        static Button Btn(string label, Transform parent, Vector2 size, Vector2 topLeft, UnityAction action, Color? fill = null, float px = 34)
        {
            var img = Img(label + " button", parent, size, topLeft, fill ?? NavyButton, rounded); img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var colors = b.colors; colors.highlightedColor = new Color(.75f, .95f, 1f); colors.pressedColor = new Color(.5f, .85f, 1f); colors.selectedColor = Color.white; colors.fadeDuration = .05f; b.colors = colors;
            Img("Edge", img.transform, size, Vector2.zero, new Color(Cyan.r, Cyan.g, Cyan.b, .8f), ring);
            Text("Label", img.transform, label, size - new Vector2(10, 4), new Vector2(5, 2), px, true);
            if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
            return b;
        }

        // Floating "Velocity Time Graph" screen = two hovering canvases that frame the lanes:
        // the three big vehicle graphs across the top, the controls column on the left.
        public static readonly Rect GraphsRect = Rect.MinMaxRect(.006f, .008f, .994f, .31f);
        public static readonly Rect ControlsRect = Rect.MinMaxRect(.005f, .012f, .262f, .285f);
        public static readonly Rect ButtonBarRect = Rect.MinMaxRect(.235f, .862f, .765f, .985f);
        public const float ScreenDistance = 2.4f;

        static void Panels(TripleVehicleArena arena)
        {
            var ui = Group("Panels", root).transform;
            var screen = Group("Velocity Time Graph (floating screen)", arena.transform).transform;
            FloatingGraphs(screen); ButtonBar(screen);
            BottomBanner(ui); ReviewCard(ui); Popup(ui); FinalCheckCard(ui);
        }

        // Three separate floating windows (one per vehicle) across the top of the view; the side windows turn toward the learner.
        public static readonly float[] WindowYaw = { -48f, 0f, 48f };
        public const float WindowElevation = 15.5f, WindowDistance = 2.3f, WindowWidth = 1.8f, WindowHeight = 1.2f;
        public static readonly Rect[] WindowRects = { Rect.MinMaxRect(.02f, .04f, .318f, .3f), Rect.MinMaxRect(.345f, .014f, .655f, .305f), Rect.MinMaxRect(.682f, .04f, .98f, .3f) };

        static void FloatingGraphs(Transform parent)
        {
            var group = parent.gameObject;
            controller = group.AddComponent<GraphController>();
            checklistPanel = group.AddComponent<ChecklistPanel>();
            presenter = group.AddComponent<FeedbackPresenter>();
            string[] names = { "CAR", "TRAIN", "BIKE" };
            var colors = new[] { GraphPanel.Hex("#3D9BFF"), GraphPanel.Hex("#FF4D5A"), GraphPanel.Hex("#FF9F1A") };
            for (int i = 0; i < 3; i++)
            {
                // Big VR windows (1.8 m x 1.2 m, 2.3 m away) on an arc at eye level: CAR left, TRAIN ahead, BIKE right.
                var win = WorldCanvas("Velocity Time Graph · " + names[i] + " window", parent, WindowYaw[i], WindowElevation, WindowDistance, WindowWidth, WindowHeight, 1100);
                Frame(win, .86f);
                float W = win.sizeDelta.x, H = win.sizeDelta.y;
                var floating = win.gameObject.AddComponent<FloatingScreen>(); floating.head = eye.transform; floating.authoredYaw = EyeYaw;
                controller.frames[i] = Img("Window edge", win, new Vector2(W, H), Vector2.zero, new Color(colors[i].r, colors[i].g, colors[i].b, .55f), ring);
                // Top: vehicle name + live status, then the question.  Below: one big graph.  Bottom: readout.
                controller.names[i] = Text("Vehicle name", win, names[i], new Vector2(220, 52), new Vector2(22, 6), 46, true, TextAlignmentOptions.Left, colors[i]);
                controller.badges[i] = controller.names[i].transform;
                checklistPanel.columns[i] = Text("Status", win, "", new Vector2(W - 250, 48), new Vector2(232, 10), 30, false, TextAlignmentOptions.Right);
                controller.stories[i] = Text("Question", win, "", new Vector2(W - 44, 82), new Vector2(22, 56), 32, true, TextAlignmentOptions.TopLeft, new Color(1f, .97f, .85f));
                Text("Velocity axis", win, "Velocity (m/s)", new Vector2(260, 34), new Vector2(12, 140), 30, true, TextAlignmentOptions.Left);
                var plot = UIRect("Velocity–time graph " + names[i], win, new Vector2(W - 126, H - 176 - 100), new Vector2(76, 176));
                var gp = plot.gameObject.AddComponent<GraphPanel>(); gp.font = fontBody; gp.raycastTarget = true; gp.color = Color.white;
                gp.maxSpeed = (float)catalog.course.maxSpeed; gp.maxTime = (float)catalog.course.maxTime; gp.gridV = 5; gp.labelV = 10;
                controller.panels[i] = gp;
                var input = plot.gameObject.AddComponent<GraphInput>(); input.panel = gp; input.controller = controller; input.lane = i; controller.inputs[i] = input;
                Text("Time axis", win, "Time (s)", new Vector2(160, 34), new Vector2(W - 180, H - 40), 30, true, TextAlignmentOptions.Right);
                controller.readouts[i] = Text("Readout", win, "", new Vector2(W - 220, 36), new Vector2(22, H - 38), 30, false, TextAlignmentOptions.Left, new Color(.8f, .96f, 1f));
                Debug.Log($"[STGF] {names[i]} window canvas {W:0} x {H:0} px");
            }
        }

        /// <summary>Horizontal button bar along the bottom of the view (at the deck rail in VR).</summary>
        static void ButtonBar(Transform parent)
        {
            var panel = UICanvas("Velocity Time Graph · button bar", parent, ButtonBarRect, 1.3f, 2000, true);
            playbackController = root.gameObject.AddComponent<PlaybackController>();
            Frame(panel, .9f);
            float W = panel.sizeDelta.x, H = panel.sizeDelta.y;
            var floating = panel.gameObject.AddComponent<FloatingScreen>(); floating.head = eye.transform; floating.authoredYaw = EyeYaw;
            string[] labels = { "RUN ▶", "PAUSE ❚❚", "RESET ⟲", "1×", "UNDO", "DELETE", "CLEAR", "HINT ?" };
            controller.partTitle = Text("Part strip", panel, "DEMO", new Vector2(W * .58f, 52), new Vector2(24, 8), 34, true, TextAlignmentOptions.Left, Cyan);
            controller.toast = Text("Toast", panel, "", new Vector2(W * .4f - 30, 52), new Vector2(W * .6f, 8), 30, true, TextAlignmentOptions.Right, new Color(1f, .85f, .3f));
            float gapB = 12, bw = (W - 40 - 7 * gapB) / 8, bh = H - 84;
            var buttons = new Button[8];
            for (int i = 0; i < 8; i++)
            {
                buttons[i] = Btn(labels[i], panel, new Vector2(bw, bh), new Vector2(20 + i * (bw + gapB), 66), null,
                    i == 0 ? new Color(.07f, .6f, .3f, .97f) : i == 7 ? new Color(.55f, .38f, .05f, .97f) : (Color?)null, 36);
                var l = buttons[i].GetComponentInChildren<TMP_Text>(); l.textWrappingMode = TextWrappingModes.NoWrap;
            }
            pendingButtons = buttons;
            playbackController.rateLabel = buttons[3].GetComponentInChildren<TMP_Text>();
            Debug.Log($"[STGF] button bar canvas {W:0} x {H:0} px, buttons {bw:0} x {bh:0} px");
        }

        static Button[] pendingButtons;
        static void BottomBanner(Transform ui)
        {
            var c = UICanvas("Bottom banner (deck rail)", ui, BannerRect, 1.3f, 1200, false);
            float W = c.sizeDelta.x, H = c.sizeDelta.y;
            var plate = Img("Banner plate", c, new Vector2(W, H), Vector2.zero, new Color(Navy.r, Navy.g, Navy.b, .93f), rounded);
            Img("Banner glow", plate.transform, new Vector2(W + 50, H + 50), new Vector2(-25, -25), Cyan, glow).transform.SetAsFirstSibling();
            Img("Banner edge", plate.transform, new Vector2(W, H), Vector2.zero, Cyan, ring);
            presenter.bottomBanner = Text("Banner text", plate.transform, "Same graph, different vehicles.", new Vector2(W - 40, H - 8), new Vector2(20, 4), Mathf.Min(64, H * .55f), true);
        }

        static void ReviewCard(Transform ui)
        {
            var c = UICanvas("Review card (feedback)", ui, ReviewRect, 1.4f, 1100, true);
            float W = c.sizeDelta.x, H = c.sizeDelta.y;
            Frame(c, .92f);
            presenter.reviewCard = c.gameObject;
            presenter.reviewMessage = Text("Feedback message", c, "", new Vector2(W - 40, H - 96), new Vector2(20, 6), 34, true, TextAlignmentOptions.Center);
            float bw = (W - 80) / 3;
            presenter.watchReplay = Btn("WATCH REPLAY", c, new Vector2(bw, 70), new Vector2(20, H - 84), null, null, 32);
            presenter.edit = Btn("EDIT", c, new Vector2(bw, 70), new Vector2(40 + bw, H - 84), null);
            presenter.next = Btn("NEXT ▶", c, new Vector2(bw, 70), new Vector2(60 + 2 * bw, H - 84), null, new Color(.07f, .6f, .3f, .97f));
            presenter.nextLabel = presenter.next.GetComponentInChildren<TMP_Text>();
        }

        static void Popup(Transform ui)
        {
            var c = UICanvas("Popup (start / pass)", ui, PopupRect, 1.45f, 820, true);
            float W = c.sizeDelta.x, H = c.sizeDelta.y;
            Frame(c, .95f);
            popupManager = root.gameObject.AddComponent<PopupManager>(); popupManager.surface = c.gameObject;
            popupManager.title = Text("Popup title", c, "", new Vector2(W - 60, 64), new Vector2(30, 22), 44, true, TextAlignmentOptions.Center, Cyan);
            popupManager.title.enableAutoSizing = true; popupManager.title.fontSizeMin = 32; popupManager.title.fontSizeMax = 44; popupManager.title.textWrappingMode = TextWrappingModes.NoWrap;
            popupManager.body = Text("Popup idea", c, "", new Vector2(W - 60, H - 190), new Vector2(30, 92), 32, false, TextAlignmentOptions.Top);
            popupManager.body.enableAutoSizing = true; popupManager.body.fontSizeMin = 30; popupManager.body.fontSizeMax = 36;
            float bw = Mathf.Min(240, (W - 100) / 3);
            for (int i = 0; i < 3; i++)
            {
                var b = Btn("START", c, new Vector2(bw, 70), new Vector2(30 + i * (bw + 20), H - 92), null, i == 0 ? new Color(.07f, .6f, .3f, .97f) : (Color?)null, 32);
                popupManager.buttons[i] = b; popupManager.labels[i] = b.GetComponentInChildren<TMP_Text>();
            }
        }

        static void FinalCheckCard(Transform ui)
        {
            var c = UICanvas("Final check card", ui, FinalRect, 1.4f, 1200, true);
            float W = c.sizeDelta.x, H = c.sizeDelta.y;
            Frame(c, .95f);
            finalManager = root.gameObject.AddComponent<FinalCheckManager>(); finalManager.surface = c.gameObject;
            Text("Heading", c, "FINAL CHECK", new Vector2(W - 60, 70), new Vector2(30, 24), 58, true, TextAlignmentOptions.Left, Cyan);
            finalManager.progressText = Text("Progress", c, "1 / 3", new Vector2(200, 60), new Vector2(W - 230, 30), 36, true, TextAlignmentOptions.Right);
            finalManager.question = Text("Question", c, "", new Vector2(W - 60, 200), new Vector2(30, 104), 44, true, TextAlignmentOptions.TopLeft);
            Text("Look left", c, "The graph is drawn on the Graph Controller (left).", new Vector2(W - 60, 44), new Vector2(30, 300), 30, false, TextAlignmentOptions.Left, new Color(.7f, .9f, 1f));
            for (int i = 0; i < 4; i++)
            {
                var b = Btn("Option", c, new Vector2(W - 60, 88), new Vector2(30, 360 + i * 100), null, null, 38);
                finalManager.options[i] = b; finalManager.optionFills[i] = b.GetComponent<Image>(); finalManager.optionLabels[i] = b.GetComponentInChildren<TMP_Text>();
                finalManager.optionLabels[i].alignment = TextAlignmentOptions.Left;
            }
            finalManager.explanation = Text("Explanation", c, "", new Vector2(W - 60, 60), new Vector2(30, 770), 32, false, TextAlignmentOptions.Left);
            float bw = (W - 120) / 4, yb = H - 110;
            finalManager.prev = Btn("◀ PREV", c, new Vector2(bw, 80), new Vector2(30, yb), null);
            finalManager.next = Btn("NEXT ▶", c, new Vector2(bw, 80), new Vector2(50 + bw, yb), null);
            finalManager.proveIt = Btn("PROVE IT", c, new Vector2(bw, 80), new Vector2(70 + 2 * bw, yb), null, new Color(.55f, .38f, .05f, .97f));
            finalManager.submit = Btn("SUBMIT", c, new Vector2(bw, 80), new Vector2(90 + 3 * bw, yb), null, new Color(.07f, .6f, .3f, .97f));
        }
    }
}