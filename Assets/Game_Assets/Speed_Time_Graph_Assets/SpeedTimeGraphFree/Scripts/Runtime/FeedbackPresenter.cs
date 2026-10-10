using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedTimeGraphFree
{
    /// <summary>Title, steps card (active step glows), bottom banner per part and the REVIEW card.</summary>
    public class FeedbackPresenter : MonoBehaviour
    {
        public TMP_Text bottomBanner;
        public Image[] stepGlows = new Image[3]; public TMP_Text[] stepTexts = new TMP_Text[3];
        public GameObject reviewCard; public TMP_Text reviewMessage;
        public Button watchReplay, edit, next; public TMP_Text nextLabel;
        public GameObject freePlayCard; public Button startQ1;

        public void Refresh(GameFlowManager game)
        {
            var v = game.variant;
            if (bottomBanner)
            {
                string key = game.part == Part.FinalCheck || game.part == Part.Result ? "Lines crossing = equal speed, NOT the same place." :
                             game.part == Part.TopicComplete ? "Height = speed · Slope = acceleration · Area = distance" : v.banner;
                if (game.part == Part.Demo || game.part == Part.Intro || game.part == Part.DemoFree)
                    key = "<color=#28DCFF>Same graph,</color> <color=#FFD43C>different</color> <color=#FFFFFF>vehicles.</color>";
                bottomBanner.text = key;
            }
            int active = game.mode == Mode.Edit ? 0 : game.mode == Mode.Run ? 1 : 2;
            for (int i = 0; i < 3; i++)
            {
                if (stepGlows[i]) stepGlows[i].enabled = i == active && game.IsGraphPart;
                if (stepTexts[i]) stepTexts[i].fontStyle = i == active ? FontStyles.Bold : FontStyles.Normal;
            }
            bool review = game.IsGraphPart && (game.mode == Mode.Review || game.mode == Mode.Complete) && !game.popup.Blocking;
            bool runMsg = game.IsGraphPart && game.mode != Mode.Edit && !review && !string.IsNullOrEmpty(game.message);
            if (reviewCard) reviewCard.SetActive(review || (game.part == Part.DemoFree && game.mode == Mode.Edit) || runMsg || (game.mode == Mode.Edit && !string.IsNullOrEmpty(game.message) && game.hintLevel > 0) || game.proof.running || (game.part == Part.FinalCheck && !string.IsNullOrEmpty(game.message)));
            if (reviewMessage) reviewMessage.text = game.message;
            bool demoFreeEdit = game.part == Part.DemoFree && game.mode == Mode.Edit;
            if (watchReplay) watchReplay.gameObject.SetActive(review);
            if (edit) edit.gameObject.SetActive(review);
            if (next)
            {
                bool canNext = (review && game.mode == Mode.Complete) || demoFreeEdit || (review && (game.part == Part.Demo || game.part == Part.DemoFree));
                next.gameObject.SetActive(canNext);
                if (nextLabel) nextLabel.text = game.part == Part.DemoFree || game.part == Part.Demo ? (game.part == Part.Demo ? "FREE PLAY ▶" : "QUESTION 1 ▶") : "NEXT ▶";
            }
        }
    }
}