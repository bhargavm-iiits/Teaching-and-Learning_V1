using TMPro;
using UnityEngine;

namespace TeachingAndLearning.RestAndMotion
{
    /// <summary>
    /// Look and feel of one physical lift button: the cap sinks when pressed and springs back,
    /// and the lamp ring and printed number glow while the button is lit, like a real lift.
    /// </summary>
    public sealed class LiftButton : MonoBehaviour
    {
        public enum LampMode { CallLamp, WhileHeld, Flash }

        public Transform cap;
        public Vector3 pressDirectionLocal = Vector3.right;
        public float travel = 0.004f;
        public Renderer capRenderer;
        public Renderer lampRing;
        public TMP_Text overlayLabel;
        public LampMode lampMode = LampMode.CallLamp;
        public Color lampColor = new Color(1f, 0.62f, 0.12f);
        public Color ringOffColor = new Color(0.09f, 0.09f, 0.1f);
        public Color hoverColor = new Color(0.55f, 0.75f, 1f);
        public Color labelOffColor = Color.white;

        Vector3 capRest;
        bool lit;
        bool held;
        bool hovered;
        float flashRemaining;
        float dipRemaining;
        float depth;
        float lamp;
        MaterialPropertyBlock block;
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public bool Lit => lit;
        public float LampLevel => lamp;
        public float Depth => depth;

        void Awake()
        {
            if (cap != null) capRest = cap.localPosition;
            Apply();
        }

        public void SetLit(bool value) { lit = value; }
        public void SetHovered(bool value) { hovered = value; }

        public void Press()
        {
            held = true;
            dipRemaining = 0.12f;
            if (lampMode == LampMode.Flash) flashRemaining = 1f;
        }

        public void Release() { held = false; }

        /// <summary>Lamp and cap animation, also callable from tests with an explicit time step.</summary>
        public void Animate(float dt)
        {
            dipRemaining = Mathf.Max(0f, dipRemaining - dt);
            flashRemaining = Mathf.Max(0f, flashRemaining - dt);
            bool down = held || dipRemaining > 0f;
            depth = Mathf.MoveTowards(depth, down ? 1f : 0f, dt / (down ? 0.06f : 0.12f));
            bool lampOn = lampMode == LampMode.CallLamp ? lit : lampMode == LampMode.WhileHeld ? held || lit : flashRemaining > 0f || lit;
            lamp = Mathf.MoveTowards(lamp, lampOn ? 1f : 0f, dt / (lampOn ? 0.05f : 0.3f));
            Apply();
        }

        void Update() { Animate(Time.unscaledDeltaTime); }

        void Apply()
        {
            if (cap != null) cap.localPosition = capRest + pressDirectionLocal.normalized * (travel * Smooth(depth));
            if (block == null) block = new MaterialPropertyBlock();
            if (capRenderer != null)
            {
                capRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, lampColor * (1.6f * lamp));
                capRenderer.SetPropertyBlock(block);
            }
            if (lampRing != null)
            {
                var off = hovered ? Color.Lerp(ringOffColor, hoverColor, 0.6f) : ringOffColor;
                lampRing.GetPropertyBlock(block);
                block.SetColor(BaseColorId, Color.Lerp(off, lampColor * 1.25f, lamp));
                lampRing.SetPropertyBlock(block);
            }
            if (overlayLabel != null) overlayLabel.color = Color.Lerp(hovered ? hoverColor : labelOffColor, lampColor, lamp);
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
