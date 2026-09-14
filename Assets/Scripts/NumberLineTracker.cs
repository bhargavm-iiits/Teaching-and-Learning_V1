using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Drives Level 1, "Find Your Spot": shows an intro popup with a Start button; once started,
/// tracks the player's position along the number line, keeps a live sign-colored "x = N m"
/// label updated, highlights the box the player is standing on, and lets them collect four
/// coins (at +3, -2, 0, -5), revealed one at a time in that order — collecting one reveals
/// the next — each showing a Pixel caption with that coin's position.
///
/// Hints: if the player makes no progress on the current coin for 20s, it (and its ring)
/// glows; every further 20s idle escalates the hint count again. Hints used determine the
/// 1-3 star rating shown at the end (0 hints = 3 stars, 1-2 = 2 stars, 3+ = 1 star).
///
/// Collecting all four coins shows a checkpoint question ("you're on the red/blue side — is
/// your position positive, negative or zero?") based on wherever the player actually is at
/// that moment. A correct answer proceeds to the confetti "Level 1 Complete!" screen; a wrong
/// answer explains the mistake and sends the player to collect one remediation coin at -4
/// before asking again (re-derived from their new position, so it's a fresh question).
public class NumberLineTracker : MonoBehaviour
{
    [SerializeField] Transform playerHead;
    [SerializeField] Transform activityRoot; // the whole "NumberLinePlayground" root; positions below are local to this
    [SerializeField] Transform boxesParent;  // the "NumberLine" container, for box highlighting
    [SerializeField] Transform coinsParent;  // the "Coins" container, for pickups
    [SerializeField] GameObject remediationCoin; // pre-built, inactive coin at x = -4

    [SerializeField] Text currentText;
    [SerializeField] Text levelText;
    [SerializeField] Text progressText;
    [SerializeField] Text bodyText;
    [SerializeField] Text captionText;
    [SerializeField] GameObject introPanel;
    [SerializeField] Button startButton;

    [SerializeField] GameObject checkpointPanel;
    [SerializeField] Text checkpointStarsText;
    [SerializeField] Text checkpointQuestionText;
    [SerializeField] Text checkpointFeedbackText;
    [SerializeField] Button checkpointPositiveButton;
    [SerializeField] Button checkpointNegativeButton;
    [SerializeField] Button checkpointZeroButton;
    [SerializeField] Button checkpointContinueButton;

    [SerializeField] GameObject congratsPanel;
    [SerializeField] Text congratsText;
    [SerializeField] ParticleSystem confetti;

    int totalCoins;
    int coinsCollected;
    bool started;
    bool completed;
    bool atCheckpoint;
    bool remediationActive;
    int correctAnswerSign; // -1, 0, or 1 — captured when the checkpoint question is posed

    float captionTimer;
    float idleTimer;
    int idleStage; // number of 20s idle thresholds already fired since the last pickup
    int hintsUsed; // total hint escalations this level, drives the star rating
    int? highlightedNearbyCoin;

    readonly Dictionary<int, GameObject> coins = new Dictionary<int, GameObject>();
    readonly List<int> coinOrder = new List<int>(); // reveal order: +3, -2, 0, -5
    readonly Dictionary<int, Renderer> coinRenderers = new Dictionary<int, Renderer>();
    readonly Dictionary<int, Renderer[]> coinRingRenderers = new Dictionary<int, Renderer[]>();
    readonly Dictionary<int, Renderer> boxes = new Dictionary<int, Renderer>();
    MaterialPropertyBlock highlightBlock;
    MaterialPropertyBlock clearBlock;
    int? highlightedBox;

    static readonly Color NegativeColor = new Color(0.95f, 0.15f, 0.2f);
    static readonly Color PositiveColor = new Color(0.2f, 0.45f, 1f);
    static readonly Color ZeroColor = Color.white;
    static readonly Color HighlightColor = new Color(1f, 0.95f, 0.4f);

    /// Exposed for testing: the same red/blue/white sign rule the live label uses.
    public static Color ColorForPosition(float x) => x < -0.001f ? NegativeColor : x > 0.001f ? PositiveColor : ZeroColor;

    void Awake()
    {
        highlightBlock = new MaterialPropertyBlock();
        highlightBlock.SetColor("_BaseColor", HighlightColor);
        highlightBlock.SetColor("_EmissionColor", HighlightColor * 1.2f);
        clearBlock = new MaterialPropertyBlock();
        CacheBoxes();
        CacheCoins();
    }

    public void Init(Transform playerHead, Transform activityRoot, Transform boxesParent, Transform coinsParent,
        GameObject remediationCoin, Text currentText, Text levelText, Text progressText, Text bodyText, Text captionText,
        GameObject introPanel, Button startButton,
        GameObject checkpointPanel, Text checkpointStarsText, Text checkpointQuestionText, Text checkpointFeedbackText,
        Button checkpointPositiveButton, Button checkpointNegativeButton, Button checkpointZeroButton, Button checkpointContinueButton,
        GameObject congratsPanel, Text congratsText, ParticleSystem confetti)
    {
        this.playerHead = playerHead;
        this.activityRoot = activityRoot;
        this.boxesParent = boxesParent;
        this.coinsParent = coinsParent;
        this.remediationCoin = remediationCoin;
        this.currentText = currentText;
        this.levelText = levelText;
        this.progressText = progressText;
        this.bodyText = bodyText;
        this.captionText = captionText;
        this.introPanel = introPanel;
        this.startButton = startButton;
        this.checkpointPanel = checkpointPanel;
        this.checkpointStarsText = checkpointStarsText;
        this.checkpointQuestionText = checkpointQuestionText;
        this.checkpointFeedbackText = checkpointFeedbackText;
        this.checkpointPositiveButton = checkpointPositiveButton;
        this.checkpointNegativeButton = checkpointNegativeButton;
        this.checkpointZeroButton = checkpointZeroButton;
        this.checkpointContinueButton = checkpointContinueButton;
        this.congratsPanel = congratsPanel;
        this.congratsText = congratsText;
        this.confetti = confetti;

        CacheBoxes();
        CacheCoins();
        BeginIntro();
    }

    void CacheBoxes()
    {
        boxes.Clear();
        if (boxesParent == null) return;
        foreach (Transform child in boxesParent)
        {
            if (!child.name.StartsWith("Box_")) continue;
            if (int.TryParse(child.name.Substring(4), out int n))
            {
                var rend = child.GetComponent<Renderer>();
                if (rend != null) boxes[n] = rend;
            }
        }
    }

    void CacheCoins()
    {
        coins.Clear();
        coinRenderers.Clear();
        coinRingRenderers.Clear();
        coinOrder.Clear();
        if (coinsParent == null) return;
        foreach (Transform child in coinsParent)
        {
            if (child.name.StartsWith("CoinRing_"))
            {
                if (int.TryParse(child.name.Substring(9), out int rv))
                    coinRingRenderers[rv] = child.GetComponentsInChildren<Renderer>();
            }
            else if (child.name.StartsWith("Coin_"))
            {
                if (int.TryParse(child.name.Substring(5), out int cv))
                {
                    coins[cv] = child.gameObject;
                    coinOrder.Add(cv); // sibling order == the Builder's creation order (+3, -2, 0, -5)
                    var rend = child.GetComponent<Renderer>();
                    if (rend != null) coinRenderers[cv] = rend;
                }
            }
        }
        totalCoins = coins.Count;
    }

    /// Shows the intro popup and arms the Start button. Also resets all level state, so this
    /// is safe to call again (e.g. if Init were ever re-run) without leaving stale progress.
    void BeginIntro()
    {
        started = false;
        completed = false;
        atCheckpoint = false;
        remediationActive = false;
        coinsCollected = 0;
        hintsUsed = 0;
        idleTimer = 0f;
        idleStage = 0;
        ClearNearbyHighlight();
        // Reveal coins one at a time in order, not all at once.
        foreach (var coin in coins.Values) coin.SetActive(false);
        if (coinOrder.Count > 0) coins[coinOrder[0]].SetActive(true);
        if (remediationCoin != null) remediationCoin.SetActive(false);

        if (levelText != null) levelText.text = "Level 1: Find Your Spot";
        if (bodyText != null) bodyText.text = "Collect all the coins hidden along\nthe number line. Watch the label\nbelow — it shows your position.\n\n(click Start, or press Enter)";
        if (progressText != null) progressText.text = $"Coins: 0 / {totalCoins}";
        if (currentText != null) { currentText.text = "x = -- m"; currentText.color = Color.white; }
        if (captionText != null) captionText.text = "";
        if (checkpointPanel != null) checkpointPanel.SetActive(false);
        if (congratsPanel != null) congratsPanel.SetActive(false);
        if (introPanel != null) introPanel.SetActive(true);

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartLevel);
        }
    }

    /// Called by the popup's Start button: closes the popup and begins the coin hunt.
    void StartLevel()
    {
        started = true;
        if (introPanel != null) introPanel.SetActive(false);
    }

    void Update()
    {
        if (playerHead == null || activityRoot == null) return;

        HandleKeyboardFallback();

        Vector3 local = activityRoot.InverseTransformPoint(playerHead.position);
        float x = local.x;

        UpdateBoxHighlight(local, x);

        if (currentText != null)
        {
            currentText.text = $"x = {x:F1} m";
            currentText.color = ColorForPosition(x);
        }

        if (captionTimer > 0f)
        {
            captionTimer -= Time.deltaTime;
            if (captionTimer <= 0f && captionText != null) captionText.text = "";
        }

        if (completed || !started) return;

        // The remediation coin can be collected even while the checkpoint popup is up.
        if (remediationActive && remediationCoin != null && remediationCoin.activeSelf)
        {
            float remX = activityRoot.InverseTransformPoint(remediationCoin.transform.position).x;
            if (Mathf.Abs(local.x - remX) <= 0.5f && Mathf.Abs(local.z) <= 1f)
            {
                remediationCoin.SetActive(false);
                remediationActive = false;
                ShowCheckpoint(); // re-derive the question from wherever the player is now
            }
        }

        if (atCheckpoint) return;

        UpdateIdleHint();

        // Only the current coin in the sequence is ever active — collect it to reveal the next.
        if (coinsCollected < coinOrder.Count)
        {
            int currentValue = coinOrder[coinsCollected];
            if (coins.TryGetValue(currentValue, out var currentCoin) && currentCoin.activeSelf
                && Mathf.Abs(local.x - currentValue) <= 0.5f && Mathf.Abs(local.z) <= 1f)
            {
                currentCoin.SetActive(false);
                coinsCollected++;
                idleTimer = 0f;
                idleStage = 0;
                ClearNearbyHighlight();
                if (captionText != null) { captionText.text = $"Pixel: You are at x = {currentValue} metres!"; captionTimer = 2.5f; }
                if (progressText != null) progressText.text = $"Coins: {coinsCollected} / {totalCoins}";

                if (coinsCollected < coinOrder.Count)
                    coins[coinOrder[coinsCollected]].SetActive(true); // reveal the next coin
            }
        }

        if (totalCoins > 0 && coinsCollected >= totalCoins)
        {
            atCheckpoint = true;
            ShowCheckpoint();
        }
    }

    /// Guaranteed way to progress through every popup even if a mouse click never reaches a
    /// button (unfocused Game view, a broken EventSystem, etc.): Enter for Start/Next, and
    /// 1/2/3 for the checkpoint's Positive/Negative/Zero answers.
    /// Deliberately NOT Space — the XR Device Simulator's own control scheme reads Space for
    /// manipulating the simulated controllers, so binding it here would fire both at once.
    void HandleKeyboardFallback()
    {
        var kb = Keyboard.current;
        if (kb == null || completed) return;
        bool confirmPressed = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;

        if (!started)
        {
            if (introPanel != null && introPanel.activeSelf && confirmPressed)
                StartLevel();
            return;
        }

        if (!atCheckpoint) return;

        if (checkpointContinueButton != null && checkpointContinueButton.gameObject.activeSelf)
        {
            if (confirmPressed) FinishLevel();
            return;
        }

        if (checkpointPanel != null && checkpointPanel.activeSelf)
        {
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) OnCheckpointAnswer(1);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) OnCheckpointAnswer(-1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) OnCheckpointAnswer(0);
        }
    }

    void UpdateBoxHighlight(Vector3 local, float x)
    {
        int rounded = Mathf.Abs(local.z) <= 0.65f ? Mathf.RoundToInt(x) : int.MinValue;
        if (highlightedBox == rounded) return;

        if (highlightedBox.HasValue && boxes.TryGetValue(highlightedBox.Value, out var prevRend))
            prevRend.SetPropertyBlock(clearBlock);
        if (boxes.TryGetValue(rounded, out var newRend))
            newRend.SetPropertyBlock(highlightBlock);
        highlightedBox = rounded;
    }

    /// Every 20 seconds without collecting the current coin, it (and its ring) glows a little
    /// brighter, and the hint count (which drives the star rating) goes up.
    void UpdateIdleHint()
    {
        idleTimer += Time.deltaTime;
        int stage = Mathf.FloorToInt(idleTimer / 20f);
        if (stage <= idleStage) return;
        idleStage = stage;
        hintsUsed++;

        int? current = CurrentCoinValue();
        if (highlightedNearbyCoin.HasValue && highlightedNearbyCoin != current)
            SetCoinHighlight(highlightedNearbyCoin.Value, false);
        if (current.HasValue) SetCoinHighlight(current.Value, true);
        highlightedNearbyCoin = current;
    }

    int? CurrentCoinValue() => coinsCollected < coinOrder.Count ? coinOrder[coinsCollected] : (int?)null;

    void SetCoinHighlight(int value, bool on)
    {
        var block = on ? highlightBlock : clearBlock;
        if (coinRenderers.TryGetValue(value, out var rend)) rend.SetPropertyBlock(block);
        if (coinRingRenderers.TryGetValue(value, out var rings))
            foreach (var r in rings) r.SetPropertyBlock(block);
    }

    void ClearNearbyHighlight()
    {
        if (highlightedNearbyCoin.HasValue) SetCoinHighlight(highlightedNearbyCoin.Value, false);
        highlightedNearbyCoin = null;
    }

    /// Poses the checkpoint question based on wherever the player actually is right now.
    void ShowCheckpoint()
    {
        float x = activityRoot.InverseTransformPoint(playerHead.position).x;
        correctAnswerSign = x < -0.25f ? -1 : x > 0.25f ? 1 : 0;
        string side = correctAnswerSign < 0 ? "red" : correctAnswerSign > 0 ? "blue" : "middle";

        int stars = hintsUsed == 0 ? 3 : hintsUsed <= 2 ? 2 : 1;
        if (checkpointStarsText != null) checkpointStarsText.text = "Level 1 complete! " + new string('⭐', stars);
        if (checkpointQuestionText != null)
            checkpointQuestionText.text = "You learned: positions left of the origin are\nnegative, right of the origin are positive.\n\n" +
                $"Quick check: you're standing on the {side} side.\nIs your position… (or press 1 / 2 / 3)";
        if (checkpointFeedbackText != null) checkpointFeedbackText.text = "";
        if (checkpointContinueButton != null) checkpointContinueButton.gameObject.SetActive(false);
        SetCheckpointAnswerButtonsActive(true);
        if (checkpointPanel != null) checkpointPanel.SetActive(true);

        WireCheckpointButton(checkpointPositiveButton, 1);
        WireCheckpointButton(checkpointNegativeButton, -1);
        WireCheckpointButton(checkpointZeroButton, 0);
    }

    void WireCheckpointButton(Button button, int answer)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnCheckpointAnswer(answer));
    }

    void SetCheckpointAnswerButtonsActive(bool active)
    {
        if (checkpointPositiveButton != null) checkpointPositiveButton.gameObject.SetActive(active);
        if (checkpointNegativeButton != null) checkpointNegativeButton.gameObject.SetActive(active);
        if (checkpointZeroButton != null) checkpointZeroButton.gameObject.SetActive(active);
    }

    void OnCheckpointAnswer(int chosen)
    {
        SetCheckpointAnswerButtonsActive(false);
        bool correct = chosen == correctAnswerSign;

        if (correct)
        {
            if (checkpointFeedbackText != null)
                checkpointFeedbackText.text = (correctAnswerSign < 0 ? "Yes! Red side = left of the origin = negative."
                    : correctAnswerSign > 0 ? "Yes! Blue side = right of the origin = positive."
                    : "Yes! Right at the origin, the position is zero.") + " (press Enter)";
            if (checkpointContinueButton != null)
            {
                checkpointContinueButton.gameObject.SetActive(true);
                checkpointContinueButton.onClick.RemoveAllListeners();
                checkpointContinueButton.onClick.AddListener(FinishLevel);
            }
        }
        else
        {
            string explanation = correctAnswerSign < 0 ? "Red means left of the origin, so the number is negative."
                : correctAnswerSign > 0 ? "Blue means right of the origin, so the number is positive."
                : "You're right at the origin, so the number is zero.";
            if (checkpointFeedbackText != null) checkpointFeedbackText.text = "Not quite. Look at the colour under your feet.\n" + explanation;
            StartRemediation();
        }
    }

    void StartRemediation()
    {
        if (checkpointPanel != null) checkpointPanel.SetActive(false);
        if (remediationCoin != null)
        {
            remediationCoin.SetActive(true);
            remediationActive = true;
            if (captionText != null) { captionText.text = "Collect one more coin, then we'll try again."; captionTimer = 4f; }
        }
        else
        {
            ShowCheckpoint(); // no remediation coin available; just retry immediately
        }
    }

    void FinishLevel()
    {
        completed = true;
        if (checkpointPanel != null) checkpointPanel.SetActive(false);
        if (introPanel != null) introPanel.SetActive(false);
        if (captionText != null) captionText.text = "";
        if (congratsPanel != null) congratsPanel.SetActive(true);
        if (congratsText != null) congratsText.text = "Level 1 Complete!\nYou can read your position\nand its sign.";
        if (confetti != null) confetti.Play();
    }
}
