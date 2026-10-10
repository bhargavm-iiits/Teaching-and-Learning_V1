using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedTimeGraphFree
{
    /// <summary>Start / pass / attempt popups: title + one-line idea + up to three buttons. Disappears on START.</summary>
    public class PopupManager : MonoBehaviour
    {
        public GameObject surface; public TMP_Text title, body; public Button[] buttons = new Button[3]; public TMP_Text[] labels = new TMP_Text[3];
        public bool Blocking => surface && surface.activeSelf;
        public void Show(string heading, string text, string first, Action a, string second = null, Action b = null, string third = null, Action c = null)
        {
            surface.SetActive(true); title.text = heading; body.text = text;
            string[] names = { first, second, third }; Action[] actions = { a, b, c };
            for (int i = 0; i < 3; i++)
            {
                int n = i; buttons[i].onClick.RemoveAllListeners();
                buttons[i].gameObject.SetActive(names[i] != null); labels[i].text = names[i] ?? "";
                if (actions[i] != null) buttons[i].onClick.AddListener(() => actions[n]());
            }
            // Centre the visible buttons in a row.
            int visible = 0; foreach (var btn in buttons) if (btn.gameObject.activeSelf) visible++;
            var panel = (RectTransform)surface.transform; float gap = 20, x = 0;
            float width = 0; foreach (var btn in buttons) if (btn.gameObject.activeSelf) width += ((RectTransform)btn.transform).sizeDelta.x;
            x = (panel.sizeDelta.x - width - gap * (visible - 1)) * .5f;
            foreach (var btn in buttons)
            {
                if (!btn.gameObject.activeSelf) continue;
                var rt = (RectTransform)btn.transform; rt.anchoredPosition = new Vector2(x + rt.sizeDelta.x * .5f, rt.anchoredPosition.y); x += rt.sizeDelta.x + gap;
            }
            Debug.Log("[STGF] popup " + heading);
        }
        public void Hide() { if (surface) surface.SetActive(false); }
        public void Press(int i) { if (Blocking && buttons[i].gameObject.activeSelf) buttons[i].onClick.Invoke(); }
    }
}