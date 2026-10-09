using TMPro;
using UnityEngine;

namespace DistanceTimeGraph
{
    /// A glowing marker (gate, station, beacon, pad) that pulses while it is the target and flashes when the car arrives.
    public class MarkerGlow : MonoBehaviour
    {
        public Renderer[] glowRenderers;
        public Color color = Palette.Cyan;
        public TextMeshPro label;
        public ParticleSystem burst;
        public bool Target { get; set; }
        public bool Reached => reached;
        bool reached;
        MaterialPropertyBlock block;

        public void SetReached(bool on)
        {
            if (on && !reached && burst != null) burst.Play();
            reached = on;
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        void Update()
        {
            if (glowRenderers == null) return;
            block ??= new MaterialPropertyBlock();
            float k = reached ? 2.6f : Target ? 1.2f + 0.8f * Mathf.Abs(Mathf.Sin(Time.time * 2.4f)) : 0.6f;
            foreach (var r in glowRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_EmissionColor", color * k);
                r.SetPropertyBlock(block);
            }
            if (label != null)
            {
                var cam = Camera.main;
                if (cam != null) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position);
            }
        }
    }
}
