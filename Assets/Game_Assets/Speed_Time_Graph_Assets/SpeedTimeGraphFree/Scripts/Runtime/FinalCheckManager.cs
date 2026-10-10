using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// Final check: 3 MCQs drawn as static graphs on the Graph Controller (slope, area, crossing).
    /// SUBMIT appears when all asked items are answered. Wrong options are logged with their misconception tag.
    /// </summary>
    public class FinalCheckManager : MonoBehaviour
    {
        public GameFlowManager game;
        public GameObject surface; public TMP_Text question, progressText, explanation;
        public Button[] options = new Button[4]; public TMP_Text[] optionLabels = new TMP_Text[4]; public Image[] optionFills = new Image[4];
        public Button prev, next, submit, proveIt;
        bool wordingB; int[] items = { 0, 1, 2 }; int current; readonly int[] answers = { -1, -1, -1 };
        // Display order per item so the correct answer (index 0 in the data) is not always first.
        static readonly int[][] order = { new[] { 2, 0, 3, 1 }, new[] { 1, 3, 0, 2 }, new[] { 3, 1, 2, 0 } };

        public FinalCheckItem Current => items.Length == 0 || game.catalog == null ? null : game.catalog.Item(items[current], wordingB);

        public void Begin(bool b, int[] askItems)
        {
            wordingB = b; items = askItems.Length > 0 ? askItems : new[] { 0, 1, 2 }; current = 0;
            for (int i = 0; i < 3; i++) answers[i] = -1;
            surface.SetActive(true); Show();
        }
        public void Hide() { if (surface) surface.SetActive(false); }
        public void Restore() { if (game.part == Part.FinalCheck) { surface.SetActive(true); Show(); } }

        void Show()
        {
            var item = Current; int slot = items[current];
            question.text = $"Q{current + 1} · {item.concept}\n<size=40>{item.prompt}</size>";
            progressText.text = $"{current + 1} / {items.Length}";
            explanation.text = "";
            for (int d = 0; d < 4; d++)
            {
                int option = order[slot][d]; int display = d;
                optionLabels[d].text = $"{(char)('A' + d)}.  {item.options[option]}";
                bool chosen = answers[slot] == option;
                optionFills[d].color = chosen ? new Color(.16f, .86f, 1f, .95f) : new Color(.05f, .22f, .45f, .95f);
                optionLabels[d].color = chosen ? new Color(.04f, .16f, .35f) : Color.white;
                options[d].onClick.RemoveAllListeners(); options[d].onClick.AddListener(() => Answer(option));
            }
            bool all = Array.TrueForAll(items, i => answers[i] >= 0);
            submit.gameObject.SetActive(all);
            prev.gameObject.SetActive(items.Length > 1); next.gameObject.SetActive(items.Length > 1);
            proveIt.gameObject.SetActive(item.concept == "CROSSING" && answers[slot] >= 0);
            game.graph.Refresh();
        }

        public void Answer(int option)
        {
            int slot = items[current]; answers[slot] = option; var item = Current;
            if (option != item.correct)
            {
                string tag = $"Q{slot + 1}{item.wording}: {item.tags[option]} ({item.options[option]})";
                game.progress.Misconceptions.Add(tag); Debug.Log("[STGF] misconception " + tag);
            }
            game.sounds.Tick(); Show();
        }
        public void Next() { current = (current + 1) % items.Length; Show(); }
        public void Prev() { current = (current + items.Length - 1) % items.Length; Show(); }
        public void Submit()
        {
            if (!Array.TrueForAll(items, i => answers[i] >= 0)) return;
            var correct = new bool[3]; var asked = new bool[3];
            for (int i = 0; i < 3; i++) { asked[i] = Array.IndexOf(items, i) >= 0; correct[i] = !asked[i] || answers[i] == game.catalog.Item(i, wordingB).correct; }
            Hide(); game.RouteFinal(correct, asked);
        }
        public void ProveIt() { if (Current != null && Current.concept == "CROSSING") { surface.SetActive(false); game.proof.Begin(Current); } }

        /// <summary>Static graph(s) of the current item on the big graph panel.</summary>
        public void Draw(GraphPanel panel)
        {
            var item = Current; if (item == null) return;
            var ci = CultureInfo.InvariantCulture;
            if (item.concept == "CROSSING")
            {
                panel.lines.Add(new GraphPanel.Line { points = item.a, color = game.graph.vehicleColors[item.aLane], width = 7, drawPoints = false });
                panel.lines.Add(new GraphPanel.Line { points = item.b, color = game.graph.vehicleColors[Mathf.Max(0, item.bLane)], width = 7, drawPoints = false });
                panel.labels.Add(new GraphPanel.Label { text = item.aVehicle, t = item.a[1].t * .85, v = item.a[1].v * .85 + panel.maxSpeed * .07, color = game.graph.vehicleColors[item.aLane], size = 34, bold = true });
                panel.labels.Add(new GraphPanel.Label { text = item.bVehicle, t = item.b[1].t * .9, v = item.b[1].v + panel.maxSpeed * .07, color = game.graph.vehicleColors[Mathf.Max(0, item.bLane)], size = 34, bold = true });
                double tc = MotionMath.CrossingTime(item.a, item.b); if (!double.IsNaN(tc)) panel.rings.Add((tc, MotionMath.Speed(item.a, tc), 16, Color.white, false));
                if (game.proof.running) panel.cursor = game.proof.time;
            }
            else
            {
                panel.lines.Add(new GraphPanel.Line { points = item.a, color = Color.white, width = 7, slopeColors = true, drawPoints = true });
                foreach (var p in item.a) panel.labels.Add(new GraphPanel.Label { text = string.Format(ci, "({0:0.#}, {1:0.#})", p.t, p.v), t = p.t, v = p.v, offset = new Vector2(p.t == 0 ? 50 : 0, 34), size = 30 });
            }
        }
    }
}