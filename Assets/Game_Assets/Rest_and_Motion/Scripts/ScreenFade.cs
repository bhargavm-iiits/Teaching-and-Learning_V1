using UnityEngine;

namespace TeachingAndLearning.RestAndMotion
{
    /// <summary>Black sphere around the eye used to fade to black between levels (works in both VR eyes).</summary>
    public sealed class ScreenFade : MonoBehaviour
    {
        public Renderer sphere;
        public float Alpha { get; private set; }
        MaterialPropertyBlock block;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public void SetAlpha(float alpha)
        {
            Alpha = Mathf.Clamp01(alpha);
            if (sphere == null) return;
            sphere.enabled = Alpha > 0.001f;
            if (block == null) block = new MaterialPropertyBlock();
            sphere.GetPropertyBlock(block);
            block.SetColor(BaseColorId, new Color(0f, 0f, 0f, Alpha));
            sphere.SetPropertyBlock(block);
        }
    }
}
