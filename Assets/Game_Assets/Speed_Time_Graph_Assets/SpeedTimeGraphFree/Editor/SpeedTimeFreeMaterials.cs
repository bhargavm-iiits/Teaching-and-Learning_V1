using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpeedTimeGraphFree
{
    /// <summary>Creates (or refreshes) URP materials inside Assets/SpeedTimeGraphFree/Materials. Never Built-in Standard.</summary>
    public static class SpeedTimeFreeMaterials
    {
        const string Folder = "Assets/SpeedTimeGraphFree/Materials/";
        static Material Get(string name, string shader)
        {
            Directory.CreateDirectory(Folder);
            string path = Folder + name.Replace('/', '_') + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var s = Shader.Find(shader);
            if (!m) { m = new Material(s); AssetDatabase.CreateAsset(m, path); }
            if (m.shader != s) m.shader = s;
            return m;
        }
        public static Material Lit(string name, Color color, float smoothness = .25f, float metallic = 0)
        {
            var m = Get(name, "Universal Render Pipeline/Lit");
            GlbImporter.MakeOpaque(m);
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smoothness); m.SetFloat("_Metallic", metallic); m.SetTexture("_BaseMap", null);
            EditorUtility.SetDirty(m); return m;
        }
        public static Material LitTextured(string name, Texture texture, Color tint, float smoothness = .15f)
        {
            var m = Lit(name, tint, smoothness); m.SetTexture("_BaseMap", texture); EditorUtility.SetDirty(m); return m;
        }
        /// <summary>Flat-shaded, lighting-independent colour (glows, markings, holograms).</summary>
        public static Material Unlit(string name, Color color, bool transparent = false, bool doubleSided = false)
        {
            var m = Get(name, "Universal Render Pipeline/Unlit");
            m.SetColor("_BaseColor", color);
            if (transparent) GlbImporter.MakeTransparent(m, color.a); else GlbImporter.MakeOpaque(m);
            m.SetFloat("_Cull", doubleSided ? 0 : 2);
            EditorUtility.SetDirty(m); return m;
        }
        public static Material Transparent(string name, Color color, float smoothness = .8f)
        {
            var m = Get(name, "Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smoothness); m.SetTexture("_BaseMap", null);
            GlbImporter.MakeTransparent(m, color.a); EditorUtility.SetDirty(m); return m;
        }
        public static Material UnlitTextured(string name, Texture texture, Color tint, bool transparent = true)
        {
            var m = Unlit(name, tint, transparent); m.SetTexture("_BaseMap", texture); EditorUtility.SetDirty(m); return m;
        }
    }
}