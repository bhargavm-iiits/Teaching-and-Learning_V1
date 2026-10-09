using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeachingAndLearning.RestAndMotion
{
    public sealed class ElevatorRideController : MonoBehaviour
    {
        public Transform exterior;
        public TMP_Text floorReadout;
        public TMP_Text prompt;
        public TMP_Text feedback;
        public TMP_Text stepStatus;
        public Button floorButton;
        public Button pauseButton;
        public Button replayButton;
        public Button continueButton;
        public Button watchButton;
        public GameObject questionPanel;
        public GameObject handAnchor;
        public Image[] stepIndicators;
        public Color activeStepColor = new Color(0.12f, 0.48f, 0.98f);
        public Color inactiveStepColor = new Color(0.16f, 0.2f, 0.28f);
        public float rideDuration = 10f;
        public float exteriorTravel = 18f;

        int currentFloor = 1;
        int lessonStep;
        bool riding;
        bool paused;
        Coroutine rideRoutine;
        Vector3 exteriorStart;

        void Awake()
        {
            if (exterior != null) exteriorStart = exterior.position;
            if (floorButton != null) floorButton.onClick.AddListener(SelectFloor);
            if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
            if (replayButton != null) replayButton.onClick.AddListener(Replay);
            if (continueButton != null) continueButton.onClick.AddListener(ContinueLesson);
            if (watchButton != null) watchButton.onClick.AddListener(BeginRide);
            SetStep(0);
            UpdateFloor();
            if (questionPanel != null) questionPanel.SetActive(false);
            if (pauseButton != null) pauseButton.interactable = false;
            if (replayButton != null) replayButton.interactable = false;
            SetPrompt("Stand inside the elevator. You and the cabin are at rest relative to each other.");
            SetFeedback("Look through the glass and choose a landmark outside.");
        }

        public void SelectFloor()
        {
            if (riding) return;
            currentFloor = 12;
            UpdateFloor();
            if (floorButton != null) floorButton.interactable = false;
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (watchButton != null) watchButton.gameObject.SetActive(true);
            SetFeedback("Floor 12 selected. Watch a building as the elevator rises.");
            SetStep(1);
            SetPrompt("Choose a building or landmark outside as your reference point.");
            if (watchButton != null) watchButton.interactable = true;
        }

        public void ContinueLesson()
        {
            if (lessonStep != 0) return;
            SetStep(1);
            SetPrompt("Choose a building or landmark outside as your reference point.");
            SetFeedback("The elevator is still. Select floor 12 when you are ready.");
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (watchButton != null) watchButton.gameObject.SetActive(false);
            if (watchButton != null) watchButton.interactable = false;
        }

        public void BeginRide()
        {
            if (riding) return;
            if (currentFloor != 12) SelectFloor();
            if (watchButton != null) watchButton.interactable = false;
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (questionPanel != null) questionPanel.SetActive(false);
            if (replayButton != null) replayButton.interactable = false;
            if (pauseButton != null) pauseButton.interactable = true;
            if (exterior != null) exterior.position = exteriorStart;
            SetStep(2);
            SetPrompt("Notice how the buildings appear to move as the elevator rises.");
            SetFeedback("The cabin stays steady around you while the city moves below.");
            rideRoutine = StartCoroutine(RunRide());
        }

        IEnumerator RunRide()
        {
            riding = true;
            paused = false;
            float elapsed = 0f;
            while (elapsed < rideDuration)
            {
                if (!paused)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / rideDuration);
                    float eased = t * t * (3f - 2f * t);
                    if (exterior != null) exterior.position = exteriorStart + Vector3.down * (exteriorTravel * eased);
                    currentFloor = Mathf.Clamp(1 + Mathf.FloorToInt(t * 11f), 1, 12);
                    UpdateFloor();
                }
                yield return null;
            }
            currentFloor = 12;
            UpdateFloor();
            riding = false;
            paused = false;
            rideRoutine = null;
            if (pauseButton != null) pauseButton.interactable = false;
            if (pauseButton != null) SetButtonLabel(pauseButton, "PAUSE RIDE");
            if (replayButton != null) replayButton.interactable = true;
            SetPrompt("You stayed at rest relative to the elevator and moved relative to the city.");
            SetFeedback("Now identify the reference point that shows your motion.");
            if (questionPanel != null) questionPanel.SetActive(true);
        }

        public void TogglePause()
        {
            if (!riding) return;
            paused = !paused;
            SetButtonLabel(pauseButton, paused ? "RESUME RIDE" : "PAUSE RIDE");
            SetFeedback(paused ? "Ride paused. The cabin and outside view are steady." : "Ride resumed. Keep watching your landmark.");
        }

        public void Replay()
        {
            if (rideRoutine != null) StopCoroutine(rideRoutine);
            riding = false;
            paused = false;
            currentFloor = 1;
            UpdateFloor();
            if (exterior != null) exterior.position = exteriorStart;
            if (pauseButton != null)
            {
                pauseButton.interactable = false;
                SetButtonLabel(pauseButton, "PAUSE RIDE");
            }
            if (replayButton != null) replayButton.interactable = false;
            if (floorButton != null) floorButton.interactable = true;
            if (watchButton != null) watchButton.interactable = false;
            if (questionPanel != null) questionPanel.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(true);
            SetStep(0);
            SetPrompt("Stand inside the elevator. You and the cabin are at rest relative to each other.");
            SetFeedback("Replay the ride and compare your motion with a landmark outside.");
        }

        public void Answer(string reference)
        {
            if (questionPanel != null) questionPanel.SetActive(false);
            if (reference == "city")
            {
                SetFeedback("Correct. The buildings are a reference point: you moved relative to the city and stayed at rest relative to the elevator.");
                SetPrompt("Motion depends on the reference point you choose.");
            }
            else
            {
                SetFeedback("Try again: compare your position with a building outside, then with the elevator cabin.");
                if (questionPanel != null) questionPanel.SetActive(true);
            }
        }

        void UpdateFloor()
        {
            if (floorReadout != null) floorReadout.text = currentFloor.ToString("00");
        }

        void SetStep(int step)
        {
            lessonStep = step;
            if (stepStatus != null) stepStatus.text = "STEP " + Mathf.Clamp(step + 1, 1, 3) + " OF 3";
            if (stepIndicators == null) return;
            for (int i = 0; i < stepIndicators.Length; i++)
                if (stepIndicators[i] != null) stepIndicators[i].color = i == step ? activeStepColor : inactiveStepColor;
        }

        void SetPrompt(string value)
        {
            if (prompt != null) prompt.text = value;
        }

        void SetFeedback(string value)
        {
            if (feedback != null) feedback.text = value;
        }

        static void SetButtonLabel(Button button, string value)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = value;
        }
    }
}
