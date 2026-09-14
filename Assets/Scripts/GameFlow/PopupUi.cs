using System;
using UnityEngine;
using UnityEngine.UI;

namespace NumberLinePlayground.Flow
{
    /// Holds references to a Popup prefab's editable pieces (title, body, up to 3 buttons)
    /// so game code can set text and wire button callbacks without finding children by name.
    public class PopupUi : MonoBehaviour
    {
        public Text Title;
        public Text Body;
        public Button ButtonA;
        public Button ButtonB;
        public Button ButtonC;

        public void Show(string title, string body)
        {
            gameObject.SetActive(true);
            if (Title != null) Title.text = title;
            if (Body != null) Body.text = body;
        }

        public void Hide() => gameObject.SetActive(false);

        /// Sets a button's label and click handler, or hides it if onClick is null.
        public void SetButton(Button button, string label, Action onClick)
        {
            if (button == null) return;
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
            button.gameObject.SetActive(onClick != null);
        }
    }
}
