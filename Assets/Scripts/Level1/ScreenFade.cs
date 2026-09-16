using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NumberLinePlayground.Level1
{
    /// A black veil a little way in front of the player's eyes. It hides the jump back to the starting point, so the
    /// player is never moved while they can see it.
    public class ScreenFade : MonoBehaviour
    {
        public Image veil;
        public GameObject veilCanvas;        // holds the veil; switched off while it is clear

        public IEnumerator FadeTo(float target, float seconds)
        {
            if (veil == null) yield break;
            if (veilCanvas != null) veilCanvas.SetActive(true);
            float from = veil.color.a;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, target, t / seconds));
                yield return null;
            }
            SetAlpha(target);
            if (target <= 0f && veilCanvas != null) veilCanvas.SetActive(false);
        }

        void SetAlpha(float alpha)
        {
            var color = veil.color;
            color.a = alpha;
            veil.color = color;
        }
    }
}
