using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace TeachingAndLearning.RestAndMotion
{
    /// <summary>
    /// One physical thing the learner can press or point at: a lift button, a building marker or a
    /// lesson-board button. XR select (ray, poke or grip), mouse and keyboard all come through here.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class ElevatorVRInteractable : MonoBehaviour
    {
        public enum Action { Floor, Open, Close, Bell, Stop, Go, Landmark, Answer, Ready, Replay }
        public Action action;
        public int floorNumber;
        public int answerIndex;
        public ElevatorVRLesson lesson;
        public LiftButton liftButton;
        public TMP_Text label;
        public LineRenderer outline;
        public Renderer highlight;
        public string referenceName;
        public string caption;

        XRSimpleInteractable interactable;
        bool hovered;
        bool pressed;
        double lastActivation = -10d;
        MaterialPropertyBlock block;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public bool IsLiftButton => liftButton != null;
        public bool Hovered => hovered;

        void OnEnable()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(Selected);
            interactable.selectExited.AddListener(Deselected);
            interactable.hoverEntered.AddListener(HoverEntered);
            interactable.hoverExited.AddListener(HoverExited);
        }

        void OnDisable()
        {
            if (interactable == null) return;
            interactable.selectEntered.RemoveListener(Selected);
            interactable.selectExited.RemoveListener(Deselected);
            interactable.hoverEntered.RemoveListener(HoverEntered);
            interactable.hoverExited.RemoveListener(HoverExited);
            if (pressed) Release();
        }

        void Selected(SelectEnterEventArgs args)
        {
            if (!Press()) return;
            var haptics = args?.interactorObject?.transform.GetComponentInParent<HapticImpulsePlayer>();
            if (haptics != null) haptics.SendHapticImpulse(0.3f, 0.05f);
        }

        void Deselected(SelectExitEventArgs args) { Release(); }
        void HoverEntered(HoverEnterEventArgs args) { SetHovered(true); }
        void HoverExited(HoverExitEventArgs args) { SetHovered(interactable != null && interactable.isHovered); }

        /// <summary>Begin a press. Returns false when the press was ignored.</summary>
        public bool Press()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (lesson == null || now - lastActivation < 0.15d) return false;
            lastActivation = now;
            pressed = true;
            if (liftButton != null) liftButton.Press();
            lesson.OnPressed(this);
            RefreshVisual();
            return true;
        }

        public void Release()
        {
            if (!pressed) return;
            pressed = false;
            if (liftButton != null) liftButton.Release();
            if (lesson != null) lesson.OnReleased(this);
        }

        /// <summary>Click: press and release at once (mouse click, keyboard Enter, tests).</summary>
        public void Activate()
        {
            if (Press()) Release();
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            if (liftButton != null) liftButton.SetHovered(value);
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (lesson == null || liftButton != null) return;
            bool selected = action == Action.Landmark && lesson.Landmark == this;
            bool available = lesson.IsAvailable(this);
            var color = hovered && available ? new Color(1f, 0.92f, 0.3f)
                : selected ? new Color(1f, 0.62f, 0.12f)
                : available ? new Color(0.35f, 0.72f, 1f)
                : new Color(0.4f, 0.45f, 0.5f);
            if (label != null) { label.color = color; if (!string.IsNullOrEmpty(caption)) label.text = caption; }
            if (outline != null)
            {
                outline.startColor = outline.endColor = color;
                float baseWidth = action == Action.Landmark ? 0.12f : 0.008f;
                outline.widthMultiplier = hovered || selected ? baseWidth * 2f : baseWidth;
            }
            if (highlight != null)
            {
                if (block == null) block = new MaterialPropertyBlock();
                highlight.GetPropertyBlock(block);
                block.SetColor(BaseColorId, selected ? new Color(1f, 0.62f, 0.12f) : hovered ? new Color(1f, 0.92f, 0.3f) : new Color(0.18f, 0.45f, 1f));
                highlight.SetPropertyBlock(block);
            }
        }
    }
}
