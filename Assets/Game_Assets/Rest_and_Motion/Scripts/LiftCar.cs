using System;
using TMPro;
using UnityEngine;

namespace TeachingAndLearning.RestAndMotion
{
    /// <summary>
    /// Connects <see cref="LiftLogic"/> to the scene: moves the outside world down while the car rises
    /// (the cabin and the learner never move), slides the doors, drives the floor display, arrows,
    /// button lamps and the lift sounds.
    /// </summary>
    public sealed class LiftCar : MonoBehaviour
    {
        public Transform exterior;
        public Vector3 exteriorAtFloorZero;
        public Transform[] doorLeaves;
        public Vector3[] doorClosedPositions;
        public Vector3 doorOpenOffset = new Vector3(1.1f, 0f, 0f);
        public TMP_Text floorDisplay;
        public TMP_Text statusDisplay;
        public Renderer upArrow;
        public Renderer downArrow;
        public LiftButton[] floorButtons = new LiftButton[LiftLogic.MaxFloor + 1];
        public LiftButton openButton, closeButton, bellButton, stopButton, goButton;
        public AudioSource panelAudio;
        public AudioSource motorAudio;
        public int startFloor = 1;
        public float floorHeight = 3.2f;

        public LiftLogic Logic { get; private set; }
        public bool Emergency { get; private set; }
        public float EmergencyPosition { get; private set; }
        public float CarPosition => Emergency ? EmergencyPosition : Logic.Position;
        public event Action<int> Arrived;
        public event Action<int, int> Departed;
        public event Action<int> FloorShown;

        AudioClip click, ding, bell, hum;
        MaterialPropertyBlock block;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color ArrowOn = new Color(0.2f, 1f, 0.35f);
        static readonly Color ArrowOff = new Color(0.04f, 0.08f, 0.05f);

        void Awake() { Initialise(); }

        public void Initialise()
        {
            if (Logic != null) return;
            Logic = new LiftLogic(startFloor) { FloorHeight = floorHeight };
            Logic.Arrived += floor => { PlayDing(); Arrived?.Invoke(floor); };
            Logic.Departed += (from, to) => Departed?.Invoke(from, to);
            Logic.DisplayFloorChanged += floor => FloorShown?.Invoke(floor);
            Logic.Reset(startFloor);
            MakeSounds();
            ApplyWorld();
        }

        public void ResetLift() { ResetTo(startFloor); }

        /// <summary>Place the car at a floor at once (doors closed, no calls). Used between levels behind a fade.</summary>
        public void ResetTo(int floor)
        {
            Initialise();
            Emergency = false;
            Logic.Reset(floor);
            ApplyWorld();
        }

        /// <summary>Emergency mode: the normal controller is bypassed and the free-fall sequence sets the car height.</summary>
        public void BeginEmergency()
        {
            Initialise();
            EmergencyPosition = Logic.Position;
            Emergency = true;
            ApplyWorld();
        }

        public void SetEmergencyPosition(float position)
        {
            EmergencyPosition = Mathf.Max(0f, position);
            ApplyWorld();
        }

        public void EndEmergency(int floor)
        {
            Emergency = false;
            Logic.Reset(floor);
            ApplyWorld();
        }

        void Update() { Step(Time.deltaTime); }

        public void Step(float dt)
        {
            Initialise();
            if (!Emergency) Logic.Tick(dt);
            ApplyWorld();
        }

        public void ApplyWorld()
        {
            if (exterior != null) exterior.position = exteriorAtFloorZero + Vector3.down * CarPosition;
            if (doorLeaves != null && doorClosedPositions != null)
            {
                float open = Smooth(Logic.DoorAmount);
                for (int i = 0; i < Mathf.Min(doorLeaves.Length, doorClosedPositions.Length); i++)
                    if (doorLeaves[i] != null)
                        doorLeaves[i].localPosition = doorClosedPositions[i] + (i == 0 ? -doorOpenOffset : doorOpenOffset) * open;
            }
            for (int f = 0; f < floorButtons.Length; f++) if (floorButtons[f] != null) floorButtons[f].SetLit(Logic.IsCalled(f));
            if (stopButton != null) stopButton.SetLit(Logic.Paused);
            if (floorDisplay != null)
            {
                int shown = Mathf.Clamp(Mathf.FloorToInt(CarPosition / floorHeight + 0.5f), 0, LiftLogic.MaxFloor);
                floorDisplay.text = Emergency ? FloorName(shown) : FloorName(Logic.DisplayFloor);
                floorDisplay.color = Emergency ? new Color(1f, 0.2f, 0.2f) : new Color(0.25f, 1f, 0.4f);
            }
            if (statusDisplay != null)
            {
                statusDisplay.text = Emergency ? "EMERGENCY" : StatusText();
                statusDisplay.color = Emergency ? new Color(1f, 0.2f, 0.2f) : new Color(0.25f, 1f, 0.4f);
            }
            SetArrow(upArrow, !Emergency && Logic.IndicatorDirection > 0);
            SetArrow(downArrow, Emergency || Logic.IndicatorDirection < 0);
            if (motorAudio != null)
            {
                float level = Emergency || Logic.Paused ? 0f : Mathf.Clamp01(Logic.Speed / Mathf.Max(0.01f, Logic.MaxSpeed));
                motorAudio.volume = 0.12f * level;
                if (level > 0.01f && !motorAudio.isPlaying) motorAudio.Play();
                else if (level <= 0.01f && motorAudio.isPlaying) motorAudio.Stop();
            }
        }

        string StatusText()
        {
            if (Logic.Paused) return "STOPPED";
            switch (Logic.Doors)
            {
                case LiftLogic.DoorState.Opening: return "DOORS OPENING";
                case LiftLogic.DoorState.Open: return "DOORS OPEN";
                case LiftLogic.DoorState.Closing: return "DOORS CLOSING";
            }
            if (Logic.IsMoving) return Logic.Direction > 0 ? "GOING UP" : "GOING DOWN";
            return "READY";
        }

        void SetArrow(Renderer arrow, bool on)
        {
            if (arrow == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            arrow.GetPropertyBlock(block);
            bool blink = on && Logic.Doors != LiftLogic.DoorState.Closed && !Logic.IsMoving && Mathf.Repeat(Time.unscaledTime, 0.8f) > 0.4f;
            block.SetColor(BaseColorId, on && !blink ? ArrowOn : ArrowOff);
            arrow.SetPropertyBlock(block);
        }

        // ---- button handlers (called by ElevatorVRLesson) ----
        public LiftLogic.PressResult PressFloor(int floor)
        {
            Click();
            return Logic.PressFloor(floor, Time.unscaledTimeAsDouble);
        }
        public void PressOpen(bool held) { Click(); Logic.HoldOpen = held; Logic.PressOpen(); }
        public void ReleaseOpen() { Logic.HoldOpen = false; }
        public void PressClose() { Click(); Logic.PressClose(); }
        public void PressBell() { if (panelAudio != null && bell != null) panelAudio.PlayOneShot(bell, 0.5f); }
        public void PressStop() { Click(); Logic.SetPaused(!Logic.Paused); }
        public void PressGo()
        {
            Click();
            if (Logic.Paused) Logic.SetPaused(false);
            else Logic.PressClose();
        }

        public void PlayChime() { PlayDing(); }

        public static string FloorName(int floor) => floor == 0 ? "P" : floor.ToString("00");

        void Click() { if (panelAudio != null && click != null) panelAudio.PlayOneShot(click, 0.35f); }
        void PlayDing() { if (panelAudio != null && ding != null) panelAudio.PlayOneShot(ding, 0.55f); }

        void MakeSounds()
        {
            if (click != null) return;
            click = Tone("Lift click", 0.05f, t => Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 90f));
            ding = Tone("Lift arrival ding", 1.1f, t =>
            {
                float first = t < 0.55f ? Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 4f) : 0f;
                float u = t - 0.45f;
                float second = u > 0f ? Mathf.Sin(2f * Mathf.PI * 698.5f * u) * Mathf.Exp(-u * 4f) : 0f;
                return 0.5f * (first + second);
            });
            bell = Tone("Lift alarm bell", 0.9f, t => Mathf.Sin(2f * Mathf.PI * 1040f * t) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 22f * t)) * Mathf.Clamp01((0.9f - t) * 6f));
            hum = Tone("Lift motor hum", 1f, t => 0.6f * Mathf.Sin(2f * Mathf.PI * 100f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 200f * t) + 0.1f * Mathf.Sin(2f * Mathf.PI * 300f * t));
            if (motorAudio != null) { motorAudio.clip = hum; motorAudio.loop = true; motorAudio.playOnAwake = false; }
        }

        static AudioClip Tone(string name, float seconds, Func<float, float> wave)
        {
            const int rate = 22050;
            var samples = new float[Mathf.Max(1, Mathf.RoundToInt(seconds * rate))];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Clamp(wave(i / (float)rate), -1f, 1f) * 0.6f;
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        void OnDestroy()
        {
            foreach (var clip in new[] { click, ding, bell, hum }) if (clip != null) Destroy(clip);
        }
    }
}
