using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpeedTimeGraphFree
{
    /// <summary>
    /// One vehicle. Nose x = k * s(t), where s(t) is the area under ITS graph (no multipliers).
    /// Wheels spin at omega = v / r, train bogies turn, the rider leans when a != 0, hazard lights
    /// blink when a graph ends above 0, and a procedural engine tone follows the speed.
    /// </summary>
    public class VehicleMover : MonoBehaviour
    {
        public int lane;
        public string vehicleName = "CAR";
        public Color color = Color.white;
        public float k = .5f;                 // world units per metre (same on every lane)
        public float offCourseMetres = 125;
        public Transform[] wheels;            // spin about local Z (lane width axis)
        public float wheelRadius = .35f;      // world units
        public Transform[] bogies;
        public Transform rider;
        public Renderer[] hazards;
        public AudioSource engine;
        public float engineBaseHz = 120;
        public bool hum;                      // the train hums instead of revving
        public TMP_Text speedTag;

        [System.NonSerialized] public double distance, speed, acceleration;
        [System.NonSerialized] public bool stillMoving, offCourse;
        float spin; Quaternion[] wheelBase, bogieBase; Quaternion riderBase; bool cached;
        float lastX; bool running;

        void Cache()
        {
            if (cached) return; cached = true;
            wheelBase = new Quaternion[wheels?.Length ?? 0]; for (int i = 0; i < wheelBase.Length; i++) wheelBase[i] = wheels[i].localRotation;
            bogieBase = new Quaternion[bogies?.Length ?? 0]; for (int i = 0; i < bogieBase.Length; i++) bogieBase[i] = bogies[i].localRotation;
            if (rider) riderBase = rider.localRotation;
        }

        void Awake()
        {
            Cache();
            if (engine && Application.isPlaying)
            {
                engine.clip = AudioCues.EngineLoop(hum ? 70 : engineBaseHz, hum);
                engine.loop = true; engine.volume = 0; engine.Play();
            }
            SetHazards(false);
        }

        /// <summary>Place the vehicle at time t of graph p. Pure function of (p, t): scrubbing works both ways.</summary>
        public void SetMotion(IList<GraphPoint> p, float t, bool isRunning = false)
        {
            Cache();
            running = isRunning;
            double end = MotionMath.Duration(p);
            distance = MotionMath.Distance(p, t);
            offCourse = distance > offCourseMetres;
            if (offCourse) distance = offCourseMetres;
            speed = t >= end ? 0 : MotionMath.Speed(p, t);
            acceleration = t >= end ? 0 : MotionMath.Acceleration(p, t);
            stillMoving = t >= end && MotionMath.EndsMoving(p) && p.Count >= 2;
            if (offCourse) { speed = 0; acceleration = 0; }

            var pos = transform.localPosition; float x = (float)(k * distance); pos.x = x; transform.localPosition = pos;

            // Wheels: rotation angle = travelled world distance / radius (so omega = v / r).
            if (wheels != null && wheelRadius > 0)
            {
                spin = x / wheelRadius * Mathf.Rad2Deg;
                for (int i = 0; i < wheels.Length; i++) if (wheels[i]) wheels[i].localRotation = wheelBase[i] * Quaternion.AngleAxis(-spin, Vector3.forward);
            }
            if (bogies != null)
                for (int i = 0; i < bogies.Length; i++) if (bogies[i]) bogies[i].localRotation = bogieBase[i] * Quaternion.Euler(0, Mathf.Sin(x * .35f + i * 1.7f) * 1.6f * Mathf.Clamp01((float)speed / 3f), 0);
            if (rider) rider.localRotation = riderBase * Quaternion.Euler(0, 0, Mathf.Clamp((float)acceleration * -2.2f, -9, 9));

            if (speedTag)
            {
                string extra = offCourse ? "\nOFF THE COURSE" : stillMoving ? "\nSTILL MOVING" : "";
                speedTag.text = $"{vehicleName} {(stillMoving ? MotionMath.Speed(p, end) : speed):0.0} m/s{extra}";
            }
            lastX = x;
        }

        void SetHazards(bool on) { if (hazards != null) foreach (var h in hazards) if (h) h.enabled = on; }

        void Update()
        {
            SetHazards(stillMoving && Mathf.Repeat(Time.time * 2.2f, 1) < .5f);
            if (engine)
            {
                float target = running ? (hum ? .05f : .06f) : 0;
                engine.volume = Mathf.MoveTowards(engine.volume, target, Time.deltaTime * .3f);
                engine.pitch = hum ? .8f + (float)speed / 12 * .5f : .55f + (float)speed / 12 * 1.6f;
            }
        }
    }
}