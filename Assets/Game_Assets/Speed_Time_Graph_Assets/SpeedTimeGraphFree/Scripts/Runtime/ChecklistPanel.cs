using System.Linq;
using TMPro;
using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>Live ✓/○ status per vehicle (Q1-Q3), shown as one compact line under each vehicle graph.</summary>
    public class ChecklistPanel : MonoBehaviour
    {
        public GameObject surface; public TMP_Text[] columns = new TMP_Text[3]; public TMP_Text heading;
        static string Short(string s)
        {
            if (s.StartsWith("stop at")) return "Stop " + s.Split(' ')[2] + " m";
            if (s.StartsWith("end at")) return "Ends 0";
            if (s.StartsWith("matches")) return "Story";
            if (s.StartsWith("finish by")) return "Time";
            if (s.StartsWith("never above")) return "Limit";
            if (s.StartsWith("speed gate")) return "Gate";
            if (s.StartsWith("slow zone")) return "Zone";
            return s;
        }
        public void Refresh(GameFlowManager game)
        {
            bool show = game.Question > 0 && game.IsGraphPart;
            if (surface) surface.SetActive(show);
            for (int i = 0; i < 3; i++)
            {
                if (!columns[i]) continue;
                if (!show) { columns[i].text = game.part == Part.Demo || game.part == Part.Intro ? "<color=#28DCFF>SHARED GRAPH</color>\n<size=28>same for all three</size>" : game.part == Part.DemoFree ? "<color=#28DCFF>OWN GRAPH</color>" : ""; continue; }
                var r = QuestionRules.Evaluate(game.variant.question, i, game.variant.lanes[i], game.graph.Graph(i).Points, game.Course);
                columns[i].text = string.Join("   ", r.checklist.Select(c => (c.ok ? "<color=#3CDC6E>✓</color> " : "<color=#8FA3B8>○</color> ") + Short(c.text)));
            }
            if (heading) heading.text = show ? $"CHECKLIST · QUESTION {game.Question}" : "";
        }
    }
}