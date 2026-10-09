using System;
using DistanceTimeGraph.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DistanceTimeGraph
{
    /// FINAL CHECK · WHAT DID YOU DISCOVER? Three questions on one panel. SUBMIT ANSWERS is enabled only when every
    /// question has an answer, and nothing says right or wrong until all three have been submitted.
    public class AssessmentManager : MonoBehaviour
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI[] prompts = new TextMeshProUGUI[3];
        public Button[] options = new Button[9];   // question q, option o is options[q * 3 + o]
        public TextMeshProUGUI[] marks = new TextMeshProUGUI[3];
        public Button submit;
        public TextMeshProUGUI submitHint;
        public GameObject resultRoot;
        public TextMeshProUGUI resultTitle, resultMessage;
        public Button resultButton;

        static readonly Color Idle = new Color(0.1f, 0.24f, 0.5f);
        static readonly Color Chosen = new Color(0.16f, 0.5f, 0.95f);

        AssessmentQuestion[] questions;
        int[] answers;
        bool submitted;
        Action<int> onRoute;
        Action onSubmitted;

        public void Open(int[] answerSheet, Action<int> routeChosen, Action submittedCallback)
        {
            gameObject.SetActive(true);
            questions = Assessment.Questions();
            answers = answerSheet;
            for (int i = 0; i < answers.Length; i++) answers[i] = -1;
            submitted = false;
            onRoute = routeChosen;
            onSubmitted = submittedCallback;
            title.text = Assessment.Title;
            for (int q = 0; q < 3; q++)
            {
                prompts[q].text = $"{q + 1}.  {questions[q].prompt}";
                marks[q].text = "";
                for (int o = 0; o < 3; o++)
                {
                    int qq = q, oo = o;
                    var b = options[q * 3 + o];
                    UiKit.SetLabel(b, $"{(char)('A' + o)}.  {questions[q].options[o]}");
                    UiKit.OnClick(b, () => Choose(qq, oo));
                    b.interactable = true;
                }
            }
            UiKit.OnClick(submit, Submit);
            resultRoot.SetActive(false);
            Paint();
        }

        public void Close() => gameObject.SetActive(false);

        public void Choose(int q, int o)
        {
            if (submitted) return;
            answers[q] = o;
            Paint();
        }

        void Paint()
        {
            bool all = Assessment.AllAnswered(answers);
            submit.interactable = all && !submitted;
            submit.gameObject.SetActive(!submitted);
            int left = 0;
            foreach (var a in answers) if (a < 0) left++;
            submitHint.text = submitted ? "" : all ? "All answered. Submit when ready." : $"Answer every question ({left} left).";
            for (int q = 0; q < 3; q++)
                for (int o = 0; o < 3; o++)
                {
                    var img = options[q * 3 + o].GetComponent<Image>();
                    Color c = answers[q] == o ? Chosen : Idle;
                    if (submitted)
                    {
                        bool right = o == questions[q].correct;
                        c = right ? Palette.Green : answers[q] == o ? Palette.Red : Idle.WithAlpha(0.6f);
                    }
                    img.color = c;
                }
        }

        public void Submit()
        {
            if (submitted || !Assessment.AllAnswered(answers)) return;
            submitted = true;
            int score = Assessment.Score(questions, answers);
            for (int q = 0; q < 3; q++)
            {
                bool right = answers[q] == questions[q].correct;
                marks[q].text = right ? "CORRECT" : "NOT QUITE";
                marks[q].color = right ? Palette.Green : Palette.Orange;
                // The options stay interactable (so their colours are not greyed out), but Choose ignores them now.
            }
            Paint();
            resultRoot.SetActive(true);
            resultTitle.text = Assessment.ResultTitle(score);
            resultTitle.color = score >= 3 ? Palette.Green : score == 2 ? Palette.Yellow : Palette.Orange;
            resultMessage.text = Assessment.ResultMessage(score);
            UiKit.SetLabel(resultButton, Assessment.ResultButton(score));
            UiKit.OnClick(resultButton, () => onRoute?.Invoke(score));
            onSubmitted?.Invoke();
            Dtg.Log($"assessment submitted: answers {answers[0]},{answers[1]},{answers[2]} score {score}");
        }
    }
}
