using DistanceTimeGraph.Logic;
using TMPro;
using UnityEngine;

namespace DistanceTimeGraph
{
    /// Lays one variant out on the straight 0-80 m track: which road modules exist (a gap for the timed bridge), where
    /// the destination, turnaround beacon and bridge are, the flag colours, the island arrangement and the lighting.
    public class LevelManager : MonoBehaviour
    {
        public DtgCatalog catalog;
        public Transform trackOrigin;
        public GameObject[] roadModules;          // module i covers i*5 .. i*5+5 m
        public MarkerGlow[] landmarks;            // indexed by Logic.Landmark
        public MarkerGlow turnaroundBeacon;
        public TimedBridgeController bridge;
        public GameObject[] islandLayouts;
        public Renderer[] flags;
        public Light sun;
        public Material skybox;
        public Transform backArrowsRoad;          // chevrons along the road shown while a return is planned or driven

        public VariantSpec Current { get; private set; }
        public const float ModuleLength = 5f;
        Material flagMaterial;

        public void Apply(VariantSpec v)
        {
            Current = v;
            bool gap = v.kind == LevelKind.Waiting;
            for (int i = 0; i < roadModules.Length; i++)
            {
                float a = i * ModuleLength, b = a + ModuleLength;
                bool inGap = gap && a >= v.waitPad - 0.01f && b <= v.bridgeEnd + 0.01f;
                roadModules[i].SetActive(!inGap);
            }
            if (bridge != null) bridge.Configure(v);

            foreach (var m in landmarks) if (m != null) { m.gameObject.SetActive(false); m.Target = false; m.SetReached(false); }
            turnaroundBeacon.gameObject.SetActive(false);
            turnaroundBeacon.SetReached(false);
            var theme = catalog.Theme(v.theme);
            switch (v.kind)
            {
                case LevelKind.Rising:
                    Place(landmarks[(int)v.landmark], v.goal, $"{v.goal} m", theme.landmark, true);
                    break;
                case LevelKind.Waiting:
                    Place(landmarks[(int)Landmark.BridgePlatform], v.bridgeEnd, $"{v.bridgeEnd} m", theme.landmark, true);
                    break;
                case LevelKind.Returning:
                    Place(turnaroundBeacon, v.turnaround, $"{v.turnaround} m", Palette.Yellow, true);
                    Place(landmarks[(int)v.landmark], v.goal, $"{v.goal} m", theme.landmark, true);
                    break;
            }

            // A run-time copy, so play mode never edits the saved material asset.
            if (Application.isPlaying && flagMaterial == null && flags.Length > 0 && flags[0] != null)
            {
                flagMaterial = new Material(flags[0].sharedMaterial);
                foreach (var f in flags) if (f != null) f.sharedMaterial = flagMaterial;
            }
            if (flagMaterial != null)
            {
                flagMaterial.SetColor("_BaseColor", theme.flag);
                flagMaterial.SetColor("_EmissionColor", theme.flag * 0.35f);
            }
            for (int i = 0; i < islandLayouts.Length; i++) islandLayouts[i].SetActive(i == Mathf.Abs(v.islands) % islandLayouts.Length);
            ApplyLighting(catalog.Light(v.lighting));
            ShowRoadReturn(false);
            Dtg.Log($"course {v.id}: {v.kind} goal {v.goal} pad {v.waitPad}-{v.bridgeEnd} open {v.bridgeOpen}-{v.bridgeClose} wait {v.waitTime} turn {v.turnaround} theme {theme.name} islands {v.islands} light {v.lighting}");
        }

        void Place(MarkerGlow m, float distance, string label, Color c, bool target)
        {
            if (m == null) return;
            m.gameObject.SetActive(true);
            m.transform.position = trackOrigin.position + trackOrigin.forward * distance;
            m.color = c;
            m.Target = target;
            m.SetLabel(label);
        }

        public void ApplyLighting(LightingPreset l)
        {
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(l.sunEuler);
                sun.color = l.sun;
                sun.intensity = l.sunIntensity;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = l.skyTop * 0.9f + Color.white * 0.1f;
            RenderSettings.ambientEquatorColor = l.skyHorizon * 0.8f;
            RenderSettings.ambientGroundColor = l.ground * 0.6f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(l.skyHorizon, new Color(0.62f, 0.8f, 1f), 0.5f);
            RenderSettings.fogDensity = l.fog;
            if (skybox != null)
            {
                // The procedural sky turns green at the horizon with a strong tint, so only lean the neutral grey toward the preset.
                if (skybox.HasProperty("_SkyTint")) skybox.SetColor("_SkyTint", Color.Lerp(new Color(0.5f, 0.5f, 0.5f), l.skyTop, 0.3f));
                if (skybox.HasProperty("_GroundColor")) skybox.SetColor("_GroundColor", l.skyHorizon);
                RenderSettings.skybox = skybox;
            }
            if (sun != null) RenderSettings.sun = sun;
        }

        /// Environment reactions to the car: targets flash when reached, the beacon lights at the turnaround.
        public void OnCar(float distance, float t)
        {
            var v = Current;
            if (v == null) return;
            if (v.kind == LevelKind.Rising)
                landmarks[(int)v.landmark].SetReached(Mathf.Abs(distance - v.goal) < LevelRules.Tolerance);
            else if (v.kind == LevelKind.Waiting)
                landmarks[(int)Landmark.BridgePlatform].SetReached(distance >= v.bridgeEnd - LevelRules.Tolerance);
            else
            {
                if (distance >= v.turnaround - LevelRules.Tolerance) turnaroundBeacon.SetReached(true);
                landmarks[(int)v.landmark].SetReached(turnaroundBeacon.Reached && Mathf.Abs(distance - v.goal) < LevelRules.Tolerance);
            }
        }

        public void ResetMarkers()
        {
            foreach (var m in landmarks) if (m != null) m.SetReached(false);
            turnaroundBeacon.SetReached(false);
        }

        /// Backward chevrons along the road, shown when the planned or driven motion returns toward the start.
        public void ShowRoadReturn(bool on)
        {
            if (backArrowsRoad != null && backArrowsRoad.gameObject.activeSelf != on) backArrowsRoad.gameObject.SetActive(on);
        }

        public void PlaceRoadReturn(float fromDistance, float toDistance)
        {
            if (backArrowsRoad == null) return;
            float span = fromDistance - toDistance;
            ShowRoadReturn(span > 1f);
            if (span <= 1f) return;
            // Chevrons pointing back toward the start, sliding from the far point to the return target.
            for (int i = 0; i < backArrowsRoad.childCount; i++)
            {
                var c = backArrowsRoad.GetChild(i);
                bool on = i * 4f < span;
                c.gameObject.SetActive(on);
                if (!on) continue;
                float s = Mathf.Repeat(i * 4f + Time.time * 3f, Mathf.Max(4f, span));
                c.position = trackOrigin.position + trackOrigin.forward * (fromDistance - s) + Vector3.up * 0.04f;
            }
        }
    }
}
