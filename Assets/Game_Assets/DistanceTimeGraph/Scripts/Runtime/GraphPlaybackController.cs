using System;
using DistanceTimeGraph.Logic;
using UnityEngine;

namespace DistanceTimeGraph
{
    public enum PlaybackMode { Live, Replay }

    /// Owns the clock of a run or a replay. Every frame one time value drives the glowing graph cursor, the car, the
    /// ghosts and the timed bridge, so they cannot drift apart. During a safety stop the cursor waits at the stop time,
    /// where the graph's value is the car's position.
    public class GraphPlaybackController : MonoBehaviour
    {
        public GraphEditor graph;
        public CarMotionController car;
        public GhostReplayController ghosts;
        public TimedBridgeController bridge;
        public LevelManager level;

        public bool Playing { get; private set; }
        public bool Paused { get; private set; }
        public float Time { get; private set; }
        public PlaybackMode Mode { get; private set; }
        public RunProfile Profile { get; private set; }

        public event Action<float> Ticked;
        Action finished;
        GraphModel program;

        /// Resets the car to the first graph point and plays the profile from 0 s.
        public void Play(RunProfile profile, GraphModel graphProgram, PlaybackMode mode, Action onFinished)
        {
            Profile = profile;
            program = graphProgram;
            Mode = mode;
            finished = onFinished;
            Time = 0f;
            Playing = true;
            Paused = false;
            car.Teleport(profile.DistanceAt(0f));
            Apply();
            Dtg.Log($"{mode} start, {profile.duration:0.0} s, success={profile.success}");
        }

        public void Pause() { if (Playing) Paused = true; }
        public void Resume() { if (Playing) Paused = false; }

        public void Stop()
        {
            Playing = false;
            Paused = false;
            graph.SetCursor(-1f, 0f);
        }

        void Update()
        {
            if (!Playing || Paused) return;
            Time = Mathf.Min(Profile.duration, Time + UnityEngine.Time.deltaTime);
            Apply();
            if (Time >= Profile.duration)
            {
                Playing = false;
                var done = finished;
                finished = null;
                done?.Invoke();
            }
        }

        void Apply()
        {
            float t = Time;
            float cursorT = Profile.safetyStop ? Mathf.Min(t, Profile.stopTime) : Mathf.Min(t, program.EndTime);
            float d = Profile.DistanceAt(t);
            car.Place(d, Profile.VelocityAt(t));
            graph.SetCursor(cursorT, program.Evaluate(cursorT));
            if (ghosts != null) ghosts.SetTime(t);
            if (bridge != null) bridge.Live(t, d);
            if (level != null) level.OnCar(d, t);
            Ticked?.Invoke(t);
        }
    }
}
