using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace NumberLinePlayground.Level1
{
    /// The whole topic, "Position, Origin and Direction", from Play to the next topic:
    ///
    ///   Play > topic name > Level 1 popup > play Level 1 > back to the starting point
    ///        > Level 2 popup > play Level 2 > back to the starting point
    ///        > Level 3 popup > play Level 3 > back to the starting point
    ///        > 3 questions
    ///            all right    > next topic
    ///            1 wrong      > Level 3 again > that 1 question again
    ///            2 wrong      > Level 2, Level 3 again > those 2 questions again
    ///            3 wrong      > Level 1, 2, 3 again > all 3 questions again
    ///
    /// A replay uses different numbers from the time before (the same game, but not the same as before), and a question
    /// that is asked again is worded differently. The same rule applies after every quiz round, so a round that still
    /// has a wrong answer sends the player back again. The rules for what follows a quiz are in RetryRules.
    ///
    /// LineLevelController plays a level; this script sequences everything around it and owns the popups.
    public class ConceptFlow : MonoBehaviour
    {
        // ---- wired by NumberLinePlaygroundBuilder ----
        public LineLevelController level;
        public Transform root;                     // the playground root
        public Transform rig;                      // the XR Origin, moved to take the player back to the start
        public Transform head;                     // the player's camera
        public ScreenFade fade;
        public NumberLinePlayground.Flow.PopupManager popup;
        public Vector3 startLocal = new Vector3(0f, 0f, -6.5f);   // where the player's head starts, in the playground's space

        public float topicSeconds = 3f;            // how long the topic name stays
        public float completeSeconds = 8f;         // how long "Level N completed" stays unless CONTINUE is pressed
        public float feedbackSeconds = 2.6f;       // how long the answer is shown before the next question
        public float resultSeconds = 4.5f;         // how long "2 of 3 correct" stays

        public GameObject topicPanel;

        public GameObject slidePanel;
        public Text slideHeader, slideBody, slideHint;
        public Text[] slideChips;                  // the three short reminders under the task
        public Button startButton;

        public GameObject completePanel;
        public Text completeTitle, completeFigure, completePath, completeDisplacement, completeStars, completeLesson;
        public Button continueButton;

        public GameObject quizPanel;
        public Text quizHeader, quizPrompt, quizFeedback;
        public Button[] optionButtons;
        public Text[] optionTexts;

        public GameObject resultPanel;
        public Text resultTitle, resultMessage, resultButtonText;
        public Button resultButton;

        static readonly Color OptionIdle = new Color(0.96f, 0.96f, 1f);
        static readonly Color OptionRight = new Color(0.66f, 0.92f, 0.72f);
        static readonly Color OptionWrong = new Color(0.98f, 0.7f, 0.7f);

        bool startPressed, continuePressed, resultPressed, levelDone;
        int chosen = -1;
        LevelStats lastStats;
        readonly List<int> wrong = new List<int>();
        readonly int[] attempts = new int[QuizBank.QuestionCount];

        // ---------------------------------------------------------------------------------------------------------
        void Awake()
        {
            HideAll();
            if (startButton != null) startButton.onClick.AddListener(() => startPressed = true);
            if (continueButton != null) continueButton.onClick.AddListener(() => continuePressed = true);
            if (resultButton != null) resultButton.onClick.AddListener(() => resultPressed = true);
            // G presses these too, so a level can be played from the keyboard alone.
            Label(startButton, "START  (G)");
            Label(continueButton, "CONTINUE  (G)");
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int k = i;
                optionButtons[k].onClick.AddListener(() => { if (chosen < 0) chosen = k; });   // the first click counts
            }
            if (level != null) level.Completed += OnLevelCompleted;
        }

        static void Label(Button button, string text)
        {
            var label = button != null ? button.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = text;
        }

        static bool GPressed()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.gKey.wasPressedThisFrame;
        }

        void OnDestroy()
        {
            if (level != null) level.Completed -= OnLevelCompleted;
        }

        // Started here rather than in Awake, so every other script has woken up first.
        void Start()
        {
            StartCoroutine(Run());
        }

        void OnLevelCompleted(LevelStats stats)
        {
            lastStats = stats;
            levelDone = true;
        }

        // ---- the sequence -------------------------------------------------------------------------------------
        IEnumerator Run()
        {
            int levelCount = Concept.Levels.Length;
            while (true)                                       // PLAY AGAIN begins the topic over
            {
                System.Array.Clear(attempts, 0, attempts.Length);
                level.Configure(Concept.Levels[0], 0);         // the scene behind the topic name is already Level 1
                yield return ShowTopic();

                int round = 0;
                int[] toPlay = RetryRules.LevelsToPlay(1, levelCount);
                var toAsk = new List<int>();
                for (int q = 0; q < QuizBank.QuestionCount; q++) toAsk.Add(q);

                while (true)                                   // one pass: the levels, then the quiz
                {
                    foreach (int number in toPlay)
                        yield return PlayLevel(Concept.Levels[number - 1], round);

                    yield return Quiz(toAsk, round);
                    int firstToReplay = RetryRules.FirstLevelToReplay(wrong.Count, levelCount);
                    yield return ShowQuizResult(toAsk.Count, wrong.Count, firstToReplay, levelCount);
                    if (firstToReplay == 0) break;             // all right: on to the next topic

                    toPlay = RetryRules.LevelsToPlay(firstToReplay, levelCount);
                    toAsk = new List<int>(wrong);              // only what was missed is asked again
                    round++;
                }
                yield return ShowNextTopic();
            }
        }

        IEnumerator ShowTopic()
        {
            yield return Present(topicPanel);
            yield return level.CheckHeightOnce();              // once, while the topic is up: smaller for a shorter player
            yield return new WaitForSecondsRealtime(topicSeconds);
            yield return Dismiss(topicPanel);
        }

        /// One level: its popup, the play, "Level N completed", and then back to the starting point.
        IEnumerator PlayLevel(LevelConfig config, int round)
        {
            level.Configure(config, round);
            slideHeader.text = "Level " + config.number;
            slideBody.text = config.task;
            if (slideHint != null) slideHint.text = config.HintFor(round);
            if (slideChips != null)
                for (int k = 0; k < slideChips.Length && k < config.chips.Length; k++) slideChips[k].text = config.chips[k];
            level.SetMarkTool(config.markTool && round > 0);   // the mark tool is a scaffold: offered on replays only
            startPressed = false;
            yield return Present(slidePanel);
            yield return new WaitUntil(() => startPressed || GPressed());
            yield return Dismiss(slidePanel);

            levelDone = false;
            level.BeginPlay();
            yield return new WaitUntil(() => levelDone);

            FillComplete(config, lastStats);
            continuePressed = false;
            // This one is read standing at the table, where the popup's usual place is inside the machine: lift it clear of
            // the spheres and their tags.
            if (popup != null) popup.Configure(2f, 0.55f);
            yield return Present(completePanel);
            for (float waited = 0f; !continuePressed && waited < completeSeconds; waited += Time.unscaledDeltaTime)
            {
                if (GPressed()) break;
                yield return null;
            }
            yield return Dismiss(completePanel);
            if (popup != null) popup.Configure(2f, 0.35f);

            yield return ReturnToStart();
        }

        void FillComplete(LevelConfig config, LevelStats stats)
        {
            completeTitle.text = "Level " + config.number + " completed";
            completeFigure.text = stats.figure;
            completePath.text = "Walked   " + stats.path.ToString("0.0", CultureInfo.InvariantCulture) + " m";
            completeDisplacement.text = "Displacement   " + stats.displacement.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " m";
            completeStars.text = new string('*', stats.stars) + "   " + stats.stars + " / 3 stars";
            completeLesson.text = config.lesson;
        }

        // ---- the quiz -----------------------------------------------------------------------------------------
        IEnumerator Quiz(List<int> ask, int round)
        {
            wrong.Clear();
            for (int i = 0; i < ask.Count; i++)
            {
                int q = ask[i];
                var question = QuizBank.Get(q, attempts[q]);
                var order = QuizBank.Order(q * 17 + attempts[q] * 5 + round * 3 + 1, question.options.Length);
                attempts[q]++;

                quizHeader.text = "Question " + (i + 1) + " of " + ask.Count;
                quizPrompt.text = question.prompt;
                quizFeedback.text = "";
                for (int k = 0; k < optionButtons.Length; k++)
                {
                    optionTexts[k].text = ((char)('A' + k)).ToString() + "   " + question.options[order[k]];
                    optionButtons[k].image.color = OptionIdle;
                }
                chosen = -1;
                if (i == 0) yield return Present(quizPanel);
                yield return new WaitUntil(() => chosen >= 0);

                int right = System.Array.IndexOf(order, 0);    // options are written with the right one first
                bool correct = chosen == right;
                optionButtons[right].image.color = OptionRight;
                if (!correct)
                {
                    optionButtons[chosen].image.color = OptionWrong;
                    wrong.Add(q);
                }
                quizFeedback.text = (correct ? "Correct!  " : "Not quite.  ") + question.explanation;
                yield return new WaitForSecondsRealtime(feedbackSeconds);
            }
            yield return Dismiss(quizPanel);
        }

        IEnumerator ShowQuizResult(int asked, int wrongCount, int firstToReplay, int levelCount)
        {
            if (wrongCount == 0)
            {
                resultTitle.text = "All correct!";
                resultMessage.text = "You are ready for the next topic.";
            }
            else
            {
                resultTitle.text = (asked - wrongCount) + " of " + asked + " correct";
                resultMessage.text = "Back to Level " + firstToReplay
                    + (firstToReplay < levelCount ? " and on to Level " + levelCount : "")
                    + ".\nThen " + wrongCount + (wrongCount == 1 ? " question" : " questions") + " again.";
            }
            resultButton.gameObject.SetActive(false);
            yield return Present(resultPanel);
            yield return new WaitForSecondsRealtime(resultSeconds);
            yield return Dismiss(resultPanel);
            if (wrongCount > 0) yield return ReturnToStartIfAway();
        }

        /// There is no next-topic scene yet, so this names it and offers to play the topic again.
        IEnumerator ShowNextTopic()
        {
            resultTitle.text = "Next topic";
            resultMessage.text = Concept.NextTopicName;
            resultButtonText.text = "PLAY AGAIN  (G)";
            resultButton.gameObject.SetActive(true);
            resultPressed = false;
            yield return Present(resultPanel);
            yield return new WaitUntil(() => resultPressed || GPressed());
            yield return Dismiss(resultPanel);
            yield return ReturnToStartIfAway();
        }

        // ---- popups -------------------------------------------------------------------------------------------
        void HideAll()
        {
            foreach (var panel in new[] { topicPanel, slidePanel, completePanel, quizPanel, resultPanel })
                if (panel != null) panel.SetActive(false);
        }

        IEnumerator Present(GameObject panel)
        {
            var group = panel.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 0f;
            panel.SetActive(true);
            yield return FadeGroup(group, 0f, 1f, 0.3f);
        }

        IEnumerator Dismiss(GameObject panel)
        {
            yield return FadeGroup(panel.GetComponent<CanvasGroup>(), 1f, 0f, 0.25f);
            panel.SetActive(false);
        }

        static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float seconds)
        {
            if (group == null) yield break;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            group.alpha = to;
        }

        // ---- back to the starting point -----------------------------------------------------------------------
        IEnumerator ReturnToStart()
        {
            if (fade != null) yield return fade.FadeTo(1f, 0.4f);
            MoveToStart();
            yield return new WaitForSecondsRealtime(0.15f);
            if (fade != null) yield return fade.FadeTo(0f, 0.4f);
        }

        IEnumerator ReturnToStartIfAway()
        {
            if (head == null || root == null) yield break;
            Vector3 away = head.position - root.TransformPoint(startLocal);
            away.y = 0f;
            if (away.magnitude > 1f) yield return ReturnToStart();
        }

        /// Moves the XR Origin so the player's head is back at the starting point, facing along the playground. Done
        /// while the screen is black. The character controller is switched off for the move, or it would push back.
        void MoveToStart()
        {
            if (rig == null || head == null || root == null) return;
            var body = rig.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;

            Vector3 shift = root.TransformPoint(startLocal) - head.position;
            shift.y = 0f;
            rig.position += shift;

            Vector3 looking = head.forward;
            looking.y = 0f;
            if (looking.sqrMagnitude > 0.0001f)
            {
                float headYaw = Mathf.Atan2(looking.x, looking.z) * Mathf.Rad2Deg;
                float rootYaw = Mathf.Atan2(root.forward.x, root.forward.z) * Mathf.Rad2Deg;
                rig.RotateAround(head.position, Vector3.up, Mathf.DeltaAngle(headYaw, rootYaw));
            }

            if (body != null) body.enabled = true;
            Physics.SyncTransforms();
            if (popup != null) popup.SnapToPlayer();           // the popups meet the player, they do not fly in
        }
    }
}
