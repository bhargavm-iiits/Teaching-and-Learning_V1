using UnityEngine;
namespace ZeroReturn
{
    [CreateAssetMenu(menuName = "Zero Return/Tuning")]
    public class TuningProfile : ScriptableObject
    {
        public float botSpeed = 6;
        public int violationBudget = 3;
        public float briefDismissSeconds = .18f;
        public float summarySeconds = 1.2f;
        public int passThreshold = 3;
        public float guidedHintSeconds = 6;
        public float replaySeconds = 15;
    }
}
