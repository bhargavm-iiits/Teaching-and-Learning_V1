using System;
using UnityEngine;

namespace TeachingAndLearning.RestAndMotion
{
    /// <summary>
    /// Level 3 emergency: alarm and flicker, the cable snaps, the car free-falls at g, then the safety brakes
    /// clamp and stop it exactly at the ground floor. Only the outside world moves; the learner's head never does.
    /// Loose objects in the cabin float while the car is in free fall (everything falls together) and drop when the brakes bite.
    /// </summary>
    public sealed class FreeFallSequence : MonoBehaviour
    {
        public enum Phase { Idle, Alarm, Falling, Braking, Stopped }

        public LiftCar car;
        public Light cabinLight;
        public Light emergencyLight;
        public AudioSource alarmAudio;
        public AudioSource windAudio;
        public AudioSource effectsAudio;
        public Transform[] looseProps;
        public ParticleSystem[] sparks;
        public float gravity = 9.81f;
        public float alarmSeconds = 1.8f;
        public float brakeHeight = 9.6f;

        public Phase Current { get; private set; }
        public float Speed { get; private set; }
        public float Height { get; private set; }
        public float PeakSpeed { get; private set; }
        public float FallSeconds { get; private set; }
        public event Action Finished;

        float timer;
        float brakeDeceleration;
        float cabinIntensity;
        Vector3[] propRest;
        Quaternion[] propRestRotation;
        float[] propLift;
        AudioClip alarm, snap, wind, screech, thud;
        System.Random noise = new System.Random(3);

        void Awake() { Prepare(); }

        public void Prepare()
        {
            if (alarm != null) return;
            if (cabinLight != null) cabinIntensity = cabinLight.intensity;
            if (looseProps != null)
            {
                propRest = new Vector3[looseProps.Length];
                propRestRotation = new Quaternion[looseProps.Length];
                propLift = new float[looseProps.Length];
                for (int i = 0; i < looseProps.Length; i++) { propRest[i] = looseProps[i].localPosition; propRestRotation[i] = looseProps[i].localRotation; }
            }
            alarm = Clip("Lift alarm", 1f, t => 0.5f * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * (t < 0.5f ? 880f : 660f) * t)) * 0.5f);
            snap = Clip("Cable snap", 0.7f, t => (Noise() * 0.9f + Mathf.Sin(2f * Mathf.PI * 140f * t)) * Mathf.Exp(-t * 9f));
            wind = Clip("Falling wind", 2f, t => Noise() * 0.6f * (0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.5f * t)));
            screech = Clip("Brake screech", 1.2f, t => (0.5f * Mathf.Sin(2f * Mathf.PI * (2300f + 300f * Mathf.Sin(30f * t)) * t) + 0.35f * Noise()) * Mathf.Clamp01((1.2f - t) * 4f));
            thud = Clip("Buffer thud", 0.9f, t => (Mathf.Sin(2f * Mathf.PI * 55f * t) + 0.4f * Noise()) * Mathf.Exp(-t * 6f));
            if (alarmAudio != null) { alarmAudio.clip = alarm; alarmAudio.loop = true; alarmAudio.playOnAwake = false; }
            if (windAudio != null) { windAudio.clip = wind; windAudio.loop = true; windAudio.playOnAwake = false; windAudio.volume = 0f; }
        }

        float Noise() => (float)(noise.NextDouble() * 2.0 - 1.0);

        public void Begin()
        {
            Prepare();
            car.BeginEmergency();
            Height = car.CarPosition;
            Speed = 0f;
            PeakSpeed = 0f;
            FallSeconds = 0f;
            timer = 0f;
            Current = Phase.Alarm;
            if (emergencyLight != null) emergencyLight.enabled = true;
            if (alarmAudio != null) alarmAudio.Play();
            ResetProps();
        }

        /// <summary>Restore the cabin after the emergency (lights, props, sounds) and put the car at the ground floor.</summary>
        public void Clear()
        {
            Prepare();
            Current = Phase.Idle;
            Speed = 0f;
            if (cabinLight != null) { cabinLight.enabled = true; cabinLight.intensity = cabinIntensity; }
            if (emergencyLight != null) emergencyLight.enabled = false;
            if (alarmAudio != null) alarmAudio.Stop();
            if (windAudio != null) windAudio.Stop();
            SetSparks(false);
            ResetProps();
        }

        public void Step(float dt)
        {
            if (Current == Phase.Idle || dt <= 0f) return;
            timer += dt;
            switch (Current)
            {
                case Phase.Alarm:
                    if (cabinLight != null) { cabinLight.enabled = Mathf.Repeat(timer * 7f, 1f) > 0.35f; cabinLight.intensity = cabinIntensity * (0.4f + 0.6f * (float)noise.NextDouble()); }
                    if (emergencyLight != null) emergencyLight.intensity = 1.2f + Mathf.Sin(timer * 12f);
                    if (timer >= alarmSeconds)
                    {
                        Play(snap, 0.9f);
                        if (cabinLight != null) cabinLight.enabled = false;
                        if (windAudio != null) windAudio.Play();
                        Current = Phase.Falling;
                        timer = 0f;
                    }
                    break;
                case Phase.Falling:
                    Speed += gravity * dt;
                    Height -= Speed * dt;
                    FallSeconds += dt;
                    if (Height <= brakeHeight)
                    {
                        brakeDeceleration = Speed * Speed / (2f * Mathf.Max(0.5f, Height));
                        Current = Phase.Braking;
                        Play(screech, 0.8f);
                        SetSparks(true);
                        timer = 0f;
                    }
                    break;
                case Phase.Braking:
                    float step = Mathf.Min(Speed * dt, Height);
                    Height -= step;
                    Speed = Mathf.Max(0f, Speed - brakeDeceleration * dt);
                    if (Speed <= 0.02f || Height <= 0.001f)
                    {
                        Height = 0f;
                        Speed = 0f;
                        Current = Phase.Stopped;
                        Play(thud, 1f);
                        SetSparks(false);
                        if (windAudio != null) windAudio.Stop();
                        if (alarmAudio != null) alarmAudio.volume = 0.25f;
                        if (emergencyLight != null) emergencyLight.intensity = 1.6f;
                        timer = 0f;
                    }
                    break;
                case Phase.Stopped:
                    if (timer > 1.2f)
                    {
                        if (alarmAudio != null) alarmAudio.Stop();
                        Current = Phase.Idle;
                        Finished?.Invoke();
                    }
                    break;
            }
            PeakSpeed = Mathf.Max(PeakSpeed, Speed);
            if (windAudio != null) windAudio.volume = Mathf.Clamp01(Speed / 25f) * 0.7f;
            if (emergencyLight != null && Current != Phase.Alarm && Current != Phase.Idle) emergencyLight.intensity = 1.4f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);
            AnimateProps(dt);
            car.SetEmergencyPosition(Height);
        }

        void AnimateProps(float dt)
        {
            if (looseProps == null || propRest == null) return;
            for (int i = 0; i < looseProps.Length; i++)
            {
                if (looseProps[i] == null) continue;
                // Free fall: the props and the car fall together, so nothing presses them to the floor and they drift up from the small jolt.
                if (Current == Phase.Falling) propLift[i] = Mathf.Min(0.55f + 0.2f * i, propLift[i] + (0.25f + 0.1f * i) * dt);
                else if (Current == Phase.Braking || Current == Phase.Stopped) propLift[i] = Mathf.Max(0f, propLift[i] - 6f * dt);
                looseProps[i].localPosition = propRest[i] + Vector3.up * propLift[i];
                if (Current == Phase.Falling) looseProps[i].Rotate(new Vector3(20f + 15f * i, 35f, 10f) * dt, Space.Self);
            }
        }

        void ResetProps()
        {
            if (looseProps == null || propRest == null) return;
            for (int i = 0; i < looseProps.Length; i++)
            {
                if (looseProps[i] == null) continue;
                propLift[i] = 0f;
                looseProps[i].localPosition = propRest[i];
                looseProps[i].localRotation = propRestRotation[i];
            }
        }

        public float PropLift(int index) => propLift != null && index < propLift.Length ? propLift[index] : 0f;

        void SetSparks(bool on)
        {
            if (sparks == null) return;
            foreach (var s in sparks)
            {
                if (s == null) continue;
                if (on) s.Play(); else s.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        void Play(AudioClip clip, float volume) { if (effectsAudio != null && clip != null) effectsAudio.PlayOneShot(clip, volume); }

        static AudioClip Clip(string name, float seconds, Func<float, float> wave)
        {
            const int rate = 22050;
            var samples = new float[Mathf.RoundToInt(seconds * rate)];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Clamp(wave(i / (float)rate), -1f, 1f) * 0.6f;
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDestroy()
        {
            foreach (var clip in new[] { alarm, snap, wind, screech, thud }) if (clip != null) Destroy(clip);
        }
    }
}
