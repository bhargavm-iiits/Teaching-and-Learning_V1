using UnityEngine;
using UnityEngine.XR;

namespace SpeedTimeGraphFree
{
    /// <summary>Procedural sounds (no audio assets) and controller haptics.</summary>
    public class AudioCues : MonoBehaviour
    {
        public AudioSource source;
        AudioClip tick, pass, fail;
        const int Rate = 24000;

        void Awake() { tick = Tone(880, .03f, .12f); pass = Chime(new[] { 660f, 880f, 1320f }, .5f); fail = Tone(180, .25f, .18f); }

        public static AudioClip Tone(float hz, float seconds, float volume)
        {
            int n = (int)(Rate * seconds); var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Sin(2 * Mathf.PI * hz * i / Rate) * volume * (1 - (float)i / n);
            var clip = AudioClip.Create("STGF tone", n, 1, Rate, false); clip.SetData(data, 0); return clip;
        }
        static AudioClip Chime(float[] notes, float seconds)
        {
            int n = (int)(Rate * seconds); var data = new float[n];
            for (int i = 0; i < n; i++) { float t = (float)i / Rate; int k = Mathf.Min(notes.Length - 1, (int)(t / seconds * notes.Length)); data[i] = Mathf.Sin(2 * Mathf.PI * notes[k] * t) * .15f * (1 - (float)i / n); }
            var clip = AudioClip.Create("STGF chime", n, 1, Rate, false); clip.SetData(data, 0); return clip;
        }
        /// <summary>One-second seamless engine loop: fundamental plus harmonics (or a soft hum for the train).</summary>
        public static AudioClip EngineLoop(float hz, bool hum)
        {
            int n = Rate; var data = new float[n]; float f = Mathf.Round(hz);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                data[i] = hum ? .5f * Mathf.Sin(2 * Mathf.PI * f * t) + .25f * Mathf.Sin(2 * Mathf.PI * f * 2 * t)
                              : .45f * Mathf.Sin(2 * Mathf.PI * f * t) + .3f * Mathf.Sin(2 * Mathf.PI * f * 2 * t) + .15f * Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * .5f * t));
            }
            var clip = AudioClip.Create(hum ? "STGF train hum" : "STGF engine", n, 1, Rate, false); clip.SetData(data, 0); return clip;
        }

        public void Tick() { Play(tick, .5f); Haptic(.12f, .02f); }
        public void Pass() { Play(pass, .8f); Haptic(.7f, .25f); }
        public void Fail() { Play(fail, .6f); Haptic(.3f, .1f); }
        void Play(AudioClip c, float v) { if (source && c) source.PlayOneShot(c, v); }
        static void Haptic(float amplitude, float duration)
        {
            foreach (var node in new[] { XRNode.LeftHand, XRNode.RightHand })
            {
                var device = InputDevices.GetDeviceAtXRNode(node);
                if (device.isValid && device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse) device.SendHapticImpulse(0, amplitude, duration);
            }
        }
    }
}