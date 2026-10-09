using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DistanceTimeGraph.EditorTools
{
    /// Matte URP materials for the scene, saved in Assets/DistanceTimeGraph/Materials. The racing car's own materials
    /// use the Built-in Standard shader (pink in URP), so the car gets URP copies here and its pack is not touched.
    public static class Mats
    {
        public static Material Road, RoadEdge, RoadLine, Deck, Stone, Rail, Bollard, BollardLight, Platform;
        public static Material Cyan, Orange, Blue, Green, Yellow, White, Red, Wood, Dark, Glow;
        public static Material CarBody, CarLights, ReverseLight, PreviewCyan, Ghost;
        public static Material BridgeOpen, BridgeClosed, BridgeSafe, BridgeUnsafe, PadFill;
        public static Material Flag, Veil, Link, Ray, Sky, Glove, Sleeve, Crystal;

        public static void Create()
        {
            Road = Lit("DTG_Road", new Color(0.33f, 0.36f, 0.43f));
            Deck = Lit("DTG_Deck", new Color(0.44f, 0.47f, 0.55f));
            RoadEdge = Lit("DTG_RoadEdge", Palette.Cyan, emission: 2.2f);
            RoadLine = Lit("DTG_RoadLine", Palette.Yellow, emission: 0.6f);
            Stone = Lit("DTG_Stone", new Color(0.52f, 0.5f, 0.5f));
            Rail = Lit("DTG_Rail", new Color(0.38f, 0.41f, 0.48f));
            Bollard = Lit("DTG_Bollard", new Color(0.3f, 0.32f, 0.38f));
            BollardLight = Lit("DTG_BollardLight", Palette.Yellow, emission: 1.8f);
            Platform = Lit("DTG_Platform", new Color(0.26f, 0.3f, 0.4f));
            Cyan = Lit("DTG_Cyan", Palette.Cyan, emission: 1.2f);
            Orange = Lit("DTG_Orange", Palette.Orange, emission: 0.6f);
            Blue = Lit("DTG_Blue", Palette.Blue, emission: 0.4f);
            Green = Lit("DTG_Green", Palette.Green, emission: 0.8f);
            Yellow = Lit("DTG_Yellow", Palette.Yellow, emission: 0.8f);
            White = Lit("DTG_White", new Color(0.95f, 0.96f, 0.98f));
            Red = Lit("DTG_Red", Palette.Red, emission: 0.6f);
            Wood = Lit("DTG_Wood", new Color(0.5f, 0.34f, 0.2f));
            Dark = Lit("DTG_Dark", new Color(0.12f, 0.14f, 0.2f));
            Glow = Lit("DTG_Glow", Palette.Cyan, emission: 1.5f);
            Crystal = Lit("DTG_Crystal", new Color(0.55f, 0.85f, 1f), emission: 1.4f);
            Flag = Lit("DTG_Flag", Palette.Blue, emission: 0.35f, doubleSided: true);
            Glove = Lit("DTG_Glove", new Color(0.97f, 0.97f, 1f));
            Sleeve = Lit("DTG_Sleeve", new Color(0.12f, 0.28f, 0.75f));

            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(DtgBuilder.Root + "/Textures/DTG_Car_Atlas.png");
            CarBody = Lit("DTG_CarBody", Color.white, texture: atlas, smooth: 0.35f);
            var emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ARCADE - FREE Racing Car/Textures/AFRC_Tex_Emission.png");
            CarLights = Lit("DTG_CarLights", Color.white, texture: emissionTex, emission: 1.5f, emissionMap: emissionTex);
            ReverseLight = Lit("DTG_ReverseLight", new Color(1f, 0.3f, 0.25f), emission: 4f);
            PreviewCyan = Lit("DTG_PreviewCyan", GhostReplayController.PreviewCyan, emission: 0.6f, transparent: true);
            Ghost = Lit("DTG_Ghost", new Color(1f, 1f, 1f, 0.35f), emission: 0.2f, transparent: true);

            BridgeOpen = Lit("DTG_BridgeOpen", new Color(0.3f, 1f, 0.5f), emission: 1.6f);
            BridgeClosed = Lit("DTG_BridgeClosed", new Color(1f, 0.55f, 0.15f, 0.28f), emission: 0.8f, transparent: true);
            BridgeSafe = Lit("DTG_BridgeSafe", new Color(0.3f, 1f, 0.5f, 0.45f), emission: 1.0f, transparent: true);
            BridgeUnsafe = Lit("DTG_BridgeUnsafe", new Color(1f, 0.55f, 0.15f, 0.4f), emission: 0.9f, transparent: true);
            PadFill = Lit("DTG_PadFill", Palette.Green, emission: 2f);
            Link = Unlit("DTG_Link", Palette.Cyan, transparent: false);
            Ray = Unlit("DTG_Ray", new Color(0.7f, 0.95f, 1f, 0.7f), transparent: true);
            Veil = Unlit("DTG_Veil", new Color(0.02f, 0.04f, 0.1f, 0.55f), transparent: true, cullFront: true);
            Veil.renderQueue = 2990;   // before the world-space interface, so the panels stay bright

            Sky = AssetDatabase.LoadAssetAtPath<Material>(DtgBuilder.MatFolder + "/DTG_Sky.mat");
            if (Sky == null)
            {
                Sky = new Material(Shader.Find("Skybox/Procedural")) { name = "DTG_Sky" };
                AssetDatabase.CreateAsset(Sky, DtgBuilder.MatFolder + "/DTG_Sky.mat");
            }
            Sky.SetFloat("_SunSize", 0.035f);
            Sky.SetFloat("_AtmosphereThickness", 0.55f);
            Sky.SetFloat("_Exposure", 1.25f);
            Sky.SetColor("_SkyTint", new Color(0.46f, 0.53f, 0.64f));
            Sky.SetColor("_GroundColor", new Color(0.72f, 0.84f, 1f));
            EditorUtility.SetDirty(Sky);
        }

        public static Material Lit(string name, Color color, float emission = 0f, bool transparent = false, bool doubleSided = false,
            Texture2D texture = null, float smooth = 0.15f, Texture2D emissionMap = null)
        {
            var m = Load(name, "Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", texture);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * emission);
                m.SetTexture("_EmissionMap", emissionMap);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
            }
            SetTransparent(m, transparent);
            m.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Unlit(string name, Color color, bool transparent, bool cullFront = false)
        {
            var m = Load(name, "Universal Render Pipeline/Unlit");
            m.SetColor("_BaseColor", color);
            SetTransparent(m, transparent);
            m.SetFloat("_Cull", cullFront ? (float)CullMode.Front : (float)CullMode.Back);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Load(string name, string shader)
        {
            string path = $"{DtgBuilder.MatFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null && m.shader.name == shader) return m;
            if (m != null) { m.shader = Shader.Find(shader); return m; }
            m = new Material(Shader.Find(shader)) { name = name };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void SetTransparent(Material m, bool on)
        {
            m.SetFloat("_Surface", on ? 1f : 0f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", on ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
            m.SetFloat("_DstBlend", on ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            m.SetFloat("_SrcBlendAlpha", on ? (float)BlendMode.One : (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", on ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            m.SetFloat("_ZWrite", on ? 0f : 1f);
            m.SetOverrideTag("RenderType", on ? "Transparent" : "Opaque");
            if (on) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); else m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = on ? (int)RenderQueue.Transparent : -1;
        }
    }
}
