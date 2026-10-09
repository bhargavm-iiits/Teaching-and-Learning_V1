using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DistanceTimeGraph
{
    /// Colours used across the scene: bright cyan, orange, blue, green and warm yellow on deep blue panels.
    public static class Palette
    {
        public static readonly Color Cyan = new Color(0.16f, 0.86f, 1f);
        public static readonly Color Orange = new Color(1f, 0.55f, 0.12f);
        public static readonly Color Blue = new Color(0.16f, 0.43f, 0.92f);
        public static readonly Color Green = new Color(0.24f, 0.8f, 0.36f);
        public static readonly Color Yellow = new Color(1f, 0.84f, 0.24f);
        public static readonly Color Red = new Color(0.9f, 0.25f, 0.25f);
        public static readonly Color Panel = new Color(0.03f, 0.13f, 0.32f, 0.92f);
        public static readonly Color PanelDeep = new Color(0.02f, 0.08f, 0.2f, 0.96f);
        public static readonly Color Grid = new Color(0.25f, 0.45f, 0.7f, 0.55f);
        public static readonly Color Ink = Color.white;
        public static readonly Color SoftInk = new Color(0.78f, 0.86f, 1f);
        public static readonly Color Grey = new Color(0.42f, 0.47f, 0.57f);

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }
    }

    /// Builds the world-space interface: panels, TextMeshPro text and buttons. Used by the editor builder (so the saved
    /// scene already contains the interface) and at run time for pooled pieces. 1 canvas unit = 1 mm.
    public static class UiKit
    {
        public static Sprite Rounded;   // 9-sliced rounded rectangle (set by the builder, saved in component fields)
        public static Sprite Circle;

        public static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, Vector2 pos, Vector2 size, bool rounded = true, bool raycast = false)
        {
            var rt = Rect(parent, name, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded && Rounded != null) { img.sprite = Rounded; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 0.6f; }
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Dot(Transform parent, string name, Color color, float diameter)
        {
            var rt = Rect(parent, name, Vector2.zero, new Vector2(diameter, diameter));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Circle;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 box,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var rt = Rect(parent, name, pos, box);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button Button(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, float fontSize = 26)
        {
            var img = Panel(parent, name, color, pos, size, true, true);
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.55f);
            b.colors = colors;
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
            outline.effectDistance = new Vector2(2, -2);
            var t = Text(img.transform, "Label", label, fontSize, Color.white, Vector2.zero, size - new Vector2(14, 8), TextAlignmentOptions.Center, true);
            // Shrink long labels to fit on one line rather than breaking a word ("REPLA Y").
            t.textWrappingMode = label.Contains(" ") && label.Length > 12 ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.enableAutoSizing = true;
            t.fontSizeMin = 10;
            t.fontSizeMax = fontSize;
            return b;
        }

        public static void SetLabel(Button b, string label)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) t.text = label;
        }

        public static void OnClick(Button b, UnityAction action)
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(action);
        }

        /// A straight line between two canvas points, drawn as a thin rotated image.
        public static void Line(RectTransform line, Vector2 a, Vector2 b, float width)
        {
            Vector2 d = b - a;
            line.anchoredPosition = (a + b) * 0.5f;
            line.sizeDelta = new Vector2(d.magnitude, width);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        /// A world-space canvas that the XR rays, the XR Device Simulator and the mouse can all press.
        public static Canvas WorldCanvas(Transform parent, string name, Vector3 localPos, Quaternion localRot, Vector2 sizeMm, Camera eventCamera)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
                typeof(UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = Vector3.one * 0.001f;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eventCamera;
            ((RectTransform)go.transform).sizeDelta = sizeMm;
            go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
            return canvas;
        }
    }
}
