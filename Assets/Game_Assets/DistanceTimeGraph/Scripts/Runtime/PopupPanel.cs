using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DistanceTimeGraph
{
    /// A world-space message card with up to three buttons (level complete, attempt recorded, introduction). It sits
    /// low and to the left of the view so the graph on the right stays fully visible.
    public class PopupPanel : MonoBehaviour
    {
        public CanvasGroup group;
        public Image accentBar;
        public TextMeshProUGUI title, body;
        public Button[] buttons;

        public bool IsOpen => gameObject.activeSelf;
        /// The button Enter presses: the one that moves on (START, NEXT LEVEL, CONTINUE, FINAL QUESTIONS), else the first.
        public Button Primary { get; private set; }

        public void Show(string heading, string message, Color accent, params (string label, Action action)[] actions)
        {
            gameObject.SetActive(true);
            title.text = heading;
            body.text = message;
            if (accentBar != null) accentBar.color = accent;
            title.color = accent;
            int n = actions.Length;
            Primary = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i < n;
                buttons[i].gameObject.SetActive(on);
                if (!on) continue;
                var action = actions[i].action;
                UiKit.SetLabel(buttons[i], actions[i].label);
                UiKit.OnClick(buttons[i], () => action?.Invoke());
                string l = actions[i].label;
                if (Primary == null && (l.StartsWith("START") || l.StartsWith("NEXT") || l.StartsWith("CONTINUE") || l.StartsWith("FINAL") || l.StartsWith("RESTART"))) Primary = buttons[i];
                // Spread the used buttons evenly along the bottom of the card.
                var rt = (RectTransform)buttons[i].transform;
                float w = rt.sizeDelta.x + 24f;
                rt.anchoredPosition = new Vector2((i - (n - 1) * 0.5f) * w, rt.anchoredPosition.y);
            }
            if (Primary == null && n > 0) Primary = buttons[0];
            if (group != null) group.alpha = 1f;
            Dtg.Log($"popup: {heading}");
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
