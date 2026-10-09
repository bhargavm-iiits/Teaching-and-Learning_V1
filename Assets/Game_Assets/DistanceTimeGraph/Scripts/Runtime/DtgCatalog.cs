using System;
using System.Collections.Generic;
using DistanceTimeGraph.Logic;
using UnityEngine;

namespace DistanceTimeGraph
{
    /// Flag and accent colours of one variant. Road edges stay cyan in every theme.
    [Serializable]
    public class ThemeColors
    {
        public string name = "Blue";
        public Color flag = Palette.Blue;
        public Color accent = Palette.Cyan;
        public Color landmark = Palette.Blue;
    }

    /// Sun, sky and fog of one variant.
    [Serializable]
    public class LightingPreset
    {
        public string name = "Morning";
        public Vector3 sunEuler = new Vector3(42f, -35f, 0f);
        public Color sun = new Color(1f, 0.95f, 0.86f);
        public float sunIntensity = 1.25f;
        public Color skyTop = new Color(0.36f, 0.62f, 0.96f);
        public Color skyHorizon = new Color(0.78f, 0.9f, 1f);
        public Color ground = new Color(0.62f, 0.7f, 0.82f);
        public float fog = 0.0035f;
    }

    /// Everything a designer tunes in the inspector: the prepared variants, graph limits, themes, lighting and the next
    /// topic. The builder fills it from VariantCatalog.Defaults() the first time.
    [CreateAssetMenu(menuName = "Distance Time Graph/Catalog", fileName = "DTG_Catalog")]
    public class DtgCatalog : ScriptableObject
    {
        [Header("Graph")]
        public int maxTime = 20;
        public int maxDistance = 80;
        [Tooltip("Metres. Every location stays on this straight track.")]
        public int trackLength = 80;

        [Header("Variants (same physics idea, different course)")]
        public List<VariantSpec> variants = new List<VariantSpec>();
        public int randomSeed = 0;   // 0 = a different order every session

        [Header("Decoration")]
        public List<ThemeColors> themes = new List<ThemeColors>();
        public List<LightingPreset> lighting = new List<LightingPreset>();

        [Header("After the final check")]
        public string nextTopicSceneName = "";

        public void FillDefaults()
        {
            variants = VariantCatalog.Defaults();
            themes = new List<ThemeColors>
            {
                new ThemeColors { name = "Blue", flag = Palette.Blue, accent = Palette.Cyan, landmark = Palette.Blue },
                new ThemeColors { name = "Forest", flag = Palette.Green, accent = Palette.Yellow, landmark = new Color(0.2f, 0.55f, 0.3f) },
                new ThemeColors { name = "Wind", flag = Palette.Cyan, accent = Color.white, landmark = new Color(0.85f, 0.9f, 0.95f) },
                new ThemeColors { name = "Parcel", flag = Palette.Orange, accent = Palette.Yellow, landmark = Palette.Orange },
                new ThemeColors { name = "Cloud", flag = new Color(0.62f, 0.45f, 0.95f), accent = Palette.Cyan, landmark = new Color(0.55f, 0.45f, 0.9f) },
                new ThemeColors { name = "Waterfall", flag = Palette.Yellow, accent = Palette.Blue, landmark = new Color(0.2f, 0.65f, 0.9f) },
            };
            lighting = new List<LightingPreset>
            {
                new LightingPreset(),
                new LightingPreset { name = "Noon", sunEuler = new Vector3(62f, 20f, 0f), sun = Color.white, sunIntensity = 1.35f,
                    skyTop = new Color(0.28f, 0.56f, 0.98f), skyHorizon = new Color(0.72f, 0.88f, 1f), fog = 0.0025f },
                new LightingPreset { name = "Golden", sunEuler = new Vector3(18f, -70f, 0f), sun = new Color(1f, 0.78f, 0.52f), sunIntensity = 1.15f,
                    skyTop = new Color(0.42f, 0.55f, 0.9f), skyHorizon = new Color(1f, 0.82f, 0.62f), ground = new Color(0.75f, 0.65f, 0.6f), fog = 0.004f },
                new LightingPreset { name = "Misty", sunEuler = new Vector3(35f, 60f, 0f), sun = new Color(0.9f, 0.93f, 1f), sunIntensity = 0.95f,
                    skyTop = new Color(0.55f, 0.66f, 0.82f), skyHorizon = new Color(0.86f, 0.9f, 0.95f), ground = new Color(0.7f, 0.74f, 0.8f), fog = 0.009f },
            };
        }

        public ThemeColors Theme(int i) => themes.Count == 0 ? new ThemeColors() : themes[Mathf.Abs(i) % themes.Count];
        public LightingPreset Light(int i) => lighting.Count == 0 ? new LightingPreset() : lighting[Mathf.Abs(i) % lighting.Count];
    }
}
