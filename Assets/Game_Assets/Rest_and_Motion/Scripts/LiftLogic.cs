using System;
using System.Collections.Generic;

namespace TeachingAndLearning.RestAndMotion
{
    /// <summary>
    /// Pure C# model of a real passenger lift with collective control:
    /// car calls are lit until served, the car keeps travelling in one direction while
    /// calls remain ahead of it, stops in floor order (not press order), then reverses.
    /// Doors open on arrival, dwell, then close. No UnityEngine dependency so it can be tested directly.
    /// </summary>
    public sealed class LiftLogic
    {
        public enum DoorState { Closed, Opening, Open, Closing }
        public enum PressResult { Registered, AlreadyLit, Cancelled, DoorsReopened, Ignored }

        public const int MinFloor = 0;
        public const int MaxFloor = 12;
        public float FloorHeight = 3.2f;
        public float MaxSpeed = 1.6f;
        public float Acceleration = 0.8f;
        public float DoorMoveTime = 1.2f;
        public float DwellTime = 3f;
        public float CancelWindow = 0.6f;

        public float Position { get; private set; }
        public float Speed { get; private set; }
        public int Direction { get; private set; }
        public int? Target { get; private set; }
        public DoorState Doors { get; private set; }
        public float DoorAmount { get; private set; }
        public bool Paused { get; private set; }
        public bool HoldOpen { get; set; }

        public event Action<int> CallRegistered;
        public event Action<int> CallCleared;
        public event Action<int> Arrived;
        public event Action<int, int> Departed;
        public event Action<int> DisplayFloorChanged;

        readonly bool[] calls = new bool[MaxFloor + 1];
        readonly double[] lastPress = new double[MaxFloor + 1];
        readonly List<int> callOrder = new List<int>();
        float dwell;
        bool silentStop;
        int displayFloor;

        public LiftLogic(int startFloor = 1)
        {
            for (int i = 0; i < lastPress.Length; i++) lastPress[i] = double.NegativeInfinity;
            Reset(startFloor);
        }

        public void Reset(int floor)
        {
            Array.Clear(calls, 0, calls.Length);
            callOrder.Clear();
            Position = Clamp(floor) * FloorHeight;
            Speed = 0f;
            Direction = 0;
            Target = null;
            Doors = DoorState.Closed;
            DoorAmount = 0f;
            Paused = false;
            HoldOpen = false;
            dwell = 0f;
            silentStop = false;
            displayFloor = Clamp(floor);
        }

        public int DisplayFloor => displayFloor;
        public bool IsCalled(int floor) => floor >= MinFloor && floor <= MaxFloor && calls[floor];
        public bool HasCalls { get { foreach (var c in calls) if (c) return true; return false; } }
        public bool IsMoving => Speed > 1e-4f || (Target.HasValue && Doors == DoorState.Closed && !Paused);
        public bool AtFloor(out int floor)
        {
            floor = (int)Math.Round(Position / FloorHeight);
            return Math.Abs(Position - floor * FloorHeight) < 0.003f && Speed < 1e-4f;
        }

        /// <summary>Indicator arrow: +1 up, -1 down, 0 none.</summary>
        public int IndicatorDirection => Direction;

        public PressResult PressFloor(int floor, double time)
        {
            if (floor < MinFloor || floor > MaxFloor) return PressResult.Ignored;
            if (AtFloor(out int here) && here == floor && !Target.HasValue)
            {
                if (Doors == DoorState.Closing || Doors == DoorState.Closed) { Doors = DoorState.Opening; return PressResult.DoorsReopened; }
                if (Doors == DoorState.Open) { dwell = DwellTime; return PressResult.DoorsReopened; }
                return PressResult.DoorsReopened;
            }
            if (calls[floor])
            {
                bool committedBraking = Target == floor && Speed > 1e-4f && RemainingTo(floor) <= StoppingDistance() + 0.05f;
                if (time - lastPress[floor] <= CancelWindow && !committedBraking)
                {
                    calls[floor] = false;
                    callOrder.Remove(floor);
                    lastPress[floor] = double.NegativeInfinity;
                    CallCleared?.Invoke(floor);
                    if (Target == floor) Target = null;
                    return PressResult.Cancelled;
                }
                lastPress[floor] = time;
                return PressResult.AlreadyLit;
            }
            calls[floor] = true;
            callOrder.Add(floor);
            lastPress[floor] = time;
            CallRegistered?.Invoke(floor);
            return PressResult.Registered;
        }

        public void PressOpen()
        {
            if (Doors == DoorState.Closing) Doors = DoorState.Opening;
            else if (Doors == DoorState.Open) dwell = DwellTime;
            else if (Doors == DoorState.Closed && AtFloor(out _) && !Target.HasValue) Doors = DoorState.Opening;
        }

        public void PressClose()
        {
            if (Doors == DoorState.Open && !HoldOpen) dwell = 0f;
        }

        public void SetPaused(bool paused) { Paused = paused; }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            TickDoors(dt);
            if (Doors != DoorState.Closed) { UpdateDisplay(); return; }

            if (!Target.HasValue && Speed < 1e-4f)
            {
                if (!Paused) ChooseNextTarget();
                if (!Target.HasValue) { UpdateDisplay(); return; }
            }

            if (Paused)
            {
                Speed = Math.Max(0f, Speed - 2.5f * Acceleration * dt);
                float brake = Speed * dt;
                if (Target.HasValue) brake = Math.Min(brake, Math.Max(0f, RemainingTo(Target.Value)));
                Position = ClampPosition(Position + Direction * brake);
                UpdateDisplay();
                return;
            }

            RetargetWhileMoving();
            if (!Target.HasValue) { UpdateDisplay(); return; }

            float remaining = RemainingTo(Target.Value);
            if (remaining <= 0.002f && Speed < 0.05f) { ArriveAt(Target.Value); UpdateDisplay(); return; }
            float desired = Math.Min(MaxSpeed, (float)Math.Sqrt(2f * Acceleration * Math.Max(0f, remaining)));
            if (Speed < desired) Speed = Math.Min(desired, Speed + Acceleration * dt);
            else Speed = desired;
            float step = Speed * dt;
            if (step >= remaining)
            {
                Position = Target.Value * FloorHeight;
                ArriveAt(Target.Value);
            }
            else Position = ClampPosition(Position + Direction * step);
            UpdateDisplay();
        }

        void TickDoors(float dt)
        {
            switch (Doors)
            {
                case DoorState.Opening:
                    DoorAmount = Math.Min(1f, DoorAmount + dt / DoorMoveTime);
                    if (DoorAmount >= 1f) { Doors = DoorState.Open; dwell = DwellTime; }
                    break;
                case DoorState.Open:
                    if (Paused) break;
                    if (HoldOpen) dwell = Math.Max(dwell, 1f);
                    dwell -= dt;
                    if (dwell <= 0f) Doors = DoorState.Closing;
                    break;
                case DoorState.Closing:
                    if (HoldOpen) { Doors = DoorState.Opening; break; }
                    DoorAmount = Math.Max(0f, DoorAmount - dt / DoorMoveTime);
                    if (DoorAmount <= 0f) Doors = DoorState.Closed;
                    break;
            }
        }

        void ChooseNextTarget()
        {
            int floor = CurrentFloorFromPosition();
            if (Direction != 0 && !AnyCallAhead(floor * FloorHeight, Direction)) Direction = 0;
            if (Direction == 0 && callOrder.Count > 0)
            {
                int first = callOrder[0];
                Direction = first * FloorHeight > Position ? 1 : -1;
            }
            if (Direction == 0) return;
            int? next = NearestCallAhead(Position, Direction, 0f);
            if (!next.HasValue) { Direction = -Direction; next = NearestCallAhead(Position, Direction, 0f); }
            if (!next.HasValue) { Direction = 0; return; }
            Target = next;
            silentStop = false;
            Departed?.Invoke(floor, next.Value);
        }

        void RetargetWhileMoving()
        {
            float stopping = StoppingDistance();
            int? best = NearestCallAhead(Position, Direction, stopping - 0.01f);
            if (best.HasValue)
            {
                if (!Target.HasValue || silentStop || RemainingTo(best.Value) < RemainingTo(Target.Value) || !calls[Target.Value]) { Target = best; silentStop = false; }
                return;
            }
            if (Target.HasValue && calls[Target.Value]) return;
            // Target was cancelled and nothing reachable ahead: stop at the next floor we can brake for, doors stay shut.
            float ahead = Position + Direction * Math.Max(stopping, 0.001f);
            int stopFloor = Direction > 0 ? (int)Math.Ceiling(ahead / FloorHeight - 1e-4) : (int)Math.Floor(ahead / FloorHeight + 1e-4);
            Target = Clamp(stopFloor);
            silentStop = true;
        }

        void ArriveAt(int floor)
        {
            Speed = 0f;
            Position = floor * FloorHeight;
            Target = null;
            bool wasCalled = calls[floor];
            if (wasCalled)
            {
                calls[floor] = false;
                callOrder.Remove(floor);
                CallCleared?.Invoke(floor);
            }
            if (!AnyCallAhead(Position, Direction)) Direction = AnyCallAhead(Position, -Direction) ? -Direction : 0;
            if (!silentStop || wasCalled)
            {
                Doors = DoorState.Opening;
                Arrived?.Invoke(floor);
            }
            silentStop = false;
        }

        int? NearestCallAhead(float from, int direction, float minDistance)
        {
            int? best = null;
            float bestDistance = float.MaxValue;
            for (int f = MinFloor; f <= MaxFloor; f++)
            {
                if (!calls[f]) continue;
                float d = (f * FloorHeight - from) * direction;
                if (d < minDistance - 1e-4f) continue;
                if (d < -1e-4f) continue;
                if (d < bestDistance) { bestDistance = d; best = f; }
            }
            return best;
        }

        bool AnyCallAhead(float from, int direction)
        {
            if (direction == 0) return false;
            for (int f = MinFloor; f <= MaxFloor; f++)
                if (calls[f] && (f * FloorHeight - from) * direction > 1e-3f) return true;
            return false;
        }

        float RemainingTo(int floor) => (floor * FloorHeight - Position) * (Direction == 0 ? Math.Sign(floor * FloorHeight - Position) : Direction);
        float StoppingDistance() => Speed * Speed / (2f * Acceleration);
        int CurrentFloorFromPosition() => Clamp((int)Math.Round(Position / FloorHeight));

        void UpdateDisplay()
        {
            int shown = Clamp((int)Math.Floor(Position / FloorHeight + 0.5f));
            if (shown == displayFloor) return;
            displayFloor = shown;
            DisplayFloorChanged?.Invoke(shown);
        }

        float ClampPosition(float position) => Math.Max(MinFloor * FloorHeight, Math.Min(MaxFloor * FloorHeight, position));
        static int Clamp(int floor) => Math.Max(MinFloor, Math.Min(MaxFloor, floor));
    }
}

