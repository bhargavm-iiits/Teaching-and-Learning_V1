using System.Collections.Generic;
using DistanceTimeGraph.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DistanceTimeGraph
{
    /// The panel on the left: the current activity, the live time and distance, the plan's prediction and the replay
    /// cards of the last attempts of this level (including attempts on earlier courses).
    public class ActivityPanel : MonoBehaviour
    {
        public TextMeshProUGUI title, instruction, status, prediction, banner;
        public Image bannerBack;
        public GameObject[] cards;
        public MiniGraph[] cardGraphs;
        public TextMeshProUGUI[] cardCaptions;

        public void SetActivity(string heading, string text)
        {
            title.text = heading;
            instruction.text = text;
        }

        public void SetStatus(string text) => status.text = text;
        public void SetPrediction(string text, Color color) { prediction.text = text; prediction.color = color; }

        public void SetBanner(string text, Color color)
        {
            bool on = !string.IsNullOrEmpty(text);
            bannerBack.gameObject.SetActive(on);
            if (!on) return;
            banner.text = text;
            bannerBack.color = color;
        }

        public void SetCards(IList<AttemptRecord> attempts, int maxTime, int maxDistance)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                bool on = i < attempts.Count;
                cards[i].SetActive(on);
                if (!on) continue;
                var a = attempts[i];
                var color = a.success ? GhostReplayController.SuccessBlue.WithAlpha(1f) : Palette.Orange;
                cardGraphs[i].Draw(a.points, maxTime, maxDistance, color);
                cardCaptions[i].text = a.summary;
                cardCaptions[i].color = color;
            }
        }
    }
}
