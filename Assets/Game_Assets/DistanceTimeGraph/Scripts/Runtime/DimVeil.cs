using UnityEngine;

namespace DistanceTimeGraph
{
    /// A dark transparent shell around the player's head that dims the world behind a popup. The panels float inside
    /// it, so the graph and the popup stay bright. It draws before the interface.
    public class DimVeil : MonoBehaviour
    {
        public Renderer veil;
        public float dimAlpha = 0.55f;
        float target, current;
        Material mat;

        public void SetDim(bool on) => target = on ? dimAlpha : 0f;

        void Update()
        {
            if (veil == null) return;
            if (mat == null) mat = veil.material;
            current = Mathf.MoveTowards(current, target, Time.deltaTime * 1.6f);
            bool visible = current > 0.005f;
            if (veil.enabled != visible) veil.enabled = visible;
            if (visible) mat.SetColor("_BaseColor", new Color(0.02f, 0.04f, 0.1f, current));
        }
    }
}
