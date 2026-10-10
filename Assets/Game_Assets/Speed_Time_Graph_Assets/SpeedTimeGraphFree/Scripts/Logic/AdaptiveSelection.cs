using System.Collections.Generic;

namespace SpeedTimeGraphFree
{
    /// <summary>Unseen variants first; once all were seen, cycle the oldest, never the one just shown.</summary>
    public static class AdaptiveSelection
    {
        public static int Next(int question, ProgressState state, int count = 4)
        {
            var order = state.SeenOrder(question);
            for (int i = 0; i < count; i++)
                if (!order.Contains(i)) { order.Add(i); return i; }
            int last = order[order.Count - 1];
            int next = order[0];
            if (next == last && order.Count > 1) next = order[1];
            order.Remove(next); order.Add(next);
            return next;
        }
    }
}
