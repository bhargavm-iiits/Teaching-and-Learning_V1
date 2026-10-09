using DistanceTimeGraph.Logic;
using TMPro;
using UnityEngine;

namespace DistanceTimeGraph
{
    /// The timed bridge of Level 2. It is absent outside its open time: green glowing tiles mean safe, orange
    /// transparent tiles mean unavailable. While the graph is edited, the bridge preview turns green when the planned
    /// crossing is safe and orange when it is too early or too late. A sync pad at the near end fills while the car waits.
    public class TimedBridgeController : MonoBehaviour
    {
        public Transform trackOrigin;
        public Transform[] tiles;
        public Material openMaterial, closedMaterial, previewSafeMaterial, previewUnsafeMaterial;
        public TextMeshPro sign;        // "BRIDGE OPEN 8–12 s"
        public TextMeshPro clock;       // live time and state
        public Transform signRoot;
        public Transform syncPad, padFill, farPlatform;

        VariantSpec spec;
        float padTime;
        float lastT = -1f;
        const float TileLength = 2f;

        public bool Enabled => spec != null && spec.kind == LevelKind.Waiting;

        public void Configure(VariantSpec v)
        {
            spec = v;
            bool on = v.kind == LevelKind.Waiting;
            gameObject.SetActive(on);
            if (!on) return;
            float span = v.bridgeEnd - v.waitPad;
            int used = Mathf.Clamp(Mathf.RoundToInt(span / TileLength), 1, tiles.Length);
            float step = span / used;
            for (int i = 0; i < tiles.Length; i++)
            {
                bool tileOn = i < used;
                tiles[i].gameObject.SetActive(tileOn);
                if (!tileOn) continue;
                tiles[i].position = At(v.waitPad + step * (i + 0.5f)) + Vector3.down * 0.15f;
                tiles[i].localScale = new Vector3(6f, 0.3f, step - 0.12f);
            }
            syncPad.position = At(v.waitPad - 2.4f) + Vector3.up * 0.02f;   // the waiting car sits on it, nose at the bridge edge
            farPlatform.position = At(v.bridgeEnd + 2.4f) + Vector3.up * 0.02f;
            signRoot.position = At(v.waitPad) + trackOrigin.right * 4.6f;
            sign.text = $"BRIDGE OPEN {v.bridgeOpen:0}–{v.bridgeClose:0} s";
            clock.text = $"SYNC PAD {v.waitPad} m · WAIT {v.waitTime:0} s";
            SetFill(0f);
            Preview(false);
        }

        Vector3 At(float d) => trackOrigin.position + trackOrigin.forward * d;

        /// Edit-time look: green when the planned crossing is safe, orange when it is too early or too late.
        public void Preview(bool safe)
        {
            if (!Enabled) return;
            foreach (var t in tiles)
            {
                if (!t.gameObject.activeSelf) continue;
                t.GetComponent<Renderer>().sharedMaterial = safe ? previewSafeMaterial : previewUnsafeMaterial;
                var s = t.localScale; s.y = 0.3f; t.localScale = s;
            }
            clock.text = safe ? $"PLAN: SAFE CROSSING" : $"PLAN: BRIDGE NOT READY";
            clock.color = safe ? Palette.Green : Palette.Orange;
            SetFill(0f);
            lastT = -1f;
        }

        /// Run-time look at time t: the bridge materialises tile by tile when it opens and fades when it closes.
        public void Live(float t, float carDistance)
        {
            if (!Enabled) return;
            bool open = t >= spec.bridgeOpen && t <= spec.bridgeClose;
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (!tile.gameObject.activeSelf) continue;
                float since = t - spec.bridgeOpen - i * 0.035f;
                float grow = open ? Mathf.Clamp01(since / 0.35f) : 1f;
                tile.GetComponent<Renderer>().sharedMaterial = open && grow > 0f ? openMaterial : closedMaterial;
                var s = tile.localScale; s.y = open ? Mathf.Lerp(0.02f, 0.3f, grow) : 0.3f; tile.localScale = s;
            }
            if (lastT < 0f || t < lastT) padTime = 0f;
            float dt = lastT < 0f ? 0f : Mathf.Max(0f, t - lastT);
            lastT = t;
            if (Mathf.Abs(carDistance - spec.waitPad) < 0.05f) padTime += dt;
            else if (carDistance < spec.waitPad) padTime = 0f;
            SetFill(padTime / Mathf.Max(0.1f, spec.waitTime));
            clock.text = open ? $"BRIDGE OPEN  ·  {t:0.0} s" : t < spec.bridgeOpen ? $"OPENS AT {spec.bridgeOpen:0} s  ·  {t:0.0} s" : $"CLOSED  ·  {t:0.0} s";
            clock.color = open ? Palette.Green : Palette.Orange;
        }

        void SetFill(float f)
        {
            f = Mathf.Clamp01(f);
            padFill.localScale = new Vector3(f, 1f, f);
            padFill.gameObject.SetActive(f > 0.01f);
        }
    }
}
