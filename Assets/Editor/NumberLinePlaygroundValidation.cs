using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class NumberLinePlaygroundValidation
{
    [MenuItem("Tools/Number Line Playground/Validate Saved Scene")]
    public static void Validate()
    {
        var root = GameObject.Find("NumberLinePlayground");
        if (root == null) throw new Exception("Playground scene is not open.");
        var tracker = root.GetComponent<NumberLineTracker>();
        var data = new SerializedObject(tracker);
        foreach (string name in new[] { "playerHead", "activityRoot", "boxesParent", "coinsParent", "remediationCoin",
            "currentText", "levelText", "progressText", "bodyText", "captionText", "introPanel", "startButton",
            "checkpointPanel", "checkpointStarsText", "checkpointQuestionText", "checkpointFeedbackText",
            "checkpointPositiveButton", "checkpointNegativeButton", "checkpointZeroButton", "checkpointContinueButton",
            "congratsPanel", "congratsText", "confetti" })
            if (data.FindProperty(name).objectReferenceValue == null) throw new Exception("Missing tracker reference: " + name);

        // Sign-based coloring rule the live "x = N m" label uses (Level 1: red<0, blue>0, white=0).
        if (NumberLineTracker.ColorForPosition(0f) != Color.white) throw new Exception("Zero position should be colored white.");
        if (NumberLineTracker.ColorForPosition(-2f) == NumberLineTracker.ColorForPosition(3f))
            throw new Exception("Negative and positive positions should be colored differently.");
        if (NumberLineTracker.ColorForPosition(-2f) != NumberLineTracker.ColorForPosition(-5f))
            throw new Exception("All negative positions should share the same color.");
        if (NumberLineTracker.ColorForPosition(3f) != NumberLineTracker.ColorForPosition(1f))
            throw new Exception("All positive positions should share the same color.");

        // Level 1 coin hunt: exactly one coin at each of +3, -2, 0, -5.
        var coinsParent = root.transform.Find("Coins");
        if (coinsParent == null) throw new Exception("Missing Coins container.");
        var expectedCoins = new HashSet<int> { 3, -2, 0, -5 };
        var foundCoins = new HashSet<int>();
        foreach (Transform child in coinsParent)
        {
            if (!child.name.StartsWith("Coin_")) continue;
            if (int.TryParse(child.name.Substring(5), out int v)) foundCoins.Add(v);
        }
        if (!foundCoins.SetEquals(expectedCoins)) throw new Exception("Coins should be exactly at +3, -2, 0, -5.");
        var coinMeshFilter = coinsParent.GetComponentInChildren<MeshFilter>();
        if (coinMeshFilter == null || !AssetDatabase.Contains(coinMeshFilter.sharedMesh))
            throw new Exception("Coin mesh not persisted.");

        // Coins reveal one at a time, not all at once: right after a fresh build, exactly
        // the first coin (+3) should be active and the other three still hidden.
        int activeCoins = 0;
        bool firstCoinActive = false;
        foreach (Transform child in coinsParent)
        {
            if (!child.name.StartsWith("Coin_") || !child.gameObject.activeSelf) continue;
            activeCoins++;
            if (child.name == "Coin_3") firstCoinActive = true;
        }
        if (activeCoins != 1) throw new Exception($"Expected exactly one active coin at a time, found {activeCoins}.");
        if (!firstCoinActive) throw new Exception("The first coin in the sequence (+3) should be the one active at level start.");

        if (root.transform.Find("NumberLine").GetComponentsInChildren<TextMesh>().Length != 11)
            throw new Exception("Expected eleven number-line labels.");
        foreach (var label in root.GetComponentsInChildren<TextMesh>())
            if (label.GetComponent<Billboard>() != null) throw new Exception("Fixed label has a billboard: " + label.name);
        var board = root.GetComponentInChildren<Canvas>();
        if (board == null || board.renderMode != RenderMode.WorldSpace)
            throw new Exception("Instructions should be a world-space popup (so the player's hands/controllers can occlude it).");
        if (board.worldCamera == null) throw new Exception("World-space popup canvas needs its worldCamera set for click raycasting to work.");
        if (board.GetComponent<NumberLinePlayground.Flow.PopupManager>() == null)
            throw new Exception("World-space popup should have a PopupManager to follow the player's gaze.");
        if (board.GetComponent<GraphicRaycaster>() == null)
            throw new Exception("Popup lost its plain GraphicRaycaster; mouse/pointer clicks (Simulator, Editor testing) would stop working.");
        if (board.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null)
            throw new Exception("Popup needs a TrackedDeviceGraphicRaycaster too, so XR controller rays can hit it.");
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            throw new Exception("No XRInteractionManager in scene; the controllers' ray interactors have nothing to register with.");
        if (!EditorBuildSettings.scenes.First(s => s.enabled).path.EndsWith("NumberLinePlayground.unity")) throw new Exception("Wrong startup scene.");

        // Level-flow state right after a fresh build: the intro popup should be showing
        // (with an armed Start button) and nothing should look like it already finished.
        var introPanel = (GameObject)data.FindProperty("introPanel").objectReferenceValue;
        if (!introPanel.activeSelf) throw new Exception("Intro popup should be visible before Start is pressed.");
        var startBtn = (Button)data.FindProperty("startButton").objectReferenceValue;
        if (!startBtn.interactable || !startBtn.gameObject.activeInHierarchy) throw new Exception("Start button should be visible and interactable.");
        var congratsPanel = (GameObject)data.FindProperty("congratsPanel").objectReferenceValue;
        if (congratsPanel.activeSelf) throw new Exception("Congrats panel should be hidden until the level is complete.");
        var confetti = (ParticleSystem)data.FindProperty("confetti").objectReferenceValue;
        if (confetti.isPlaying) throw new Exception("Confetti should not be playing before the level is complete.");
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            throw new Exception("No EventSystem in scene; the Start button's clicks would never register.");

        // Checkpoint question + remediation coin: both should be dormant until all four
        // main coins are collected.
        var checkpointPanel = (GameObject)data.FindProperty("checkpointPanel").objectReferenceValue;
        if (checkpointPanel.activeSelf) throw new Exception("Checkpoint panel should be hidden until all coins are collected.");
        var checkpointContinueButton = (Button)data.FindProperty("checkpointContinueButton").objectReferenceValue;
        if (checkpointContinueButton.gameObject.activeSelf) throw new Exception("Checkpoint 'Next' button should be hidden until answered correctly.");
        var remediationCoin = (GameObject)data.FindProperty("remediationCoin").objectReferenceValue;
        if (remediationCoin.activeSelf) throw new Exception("Remediation coin should be hidden until a checkpoint answer is wrong.");
        float remediationX = root.transform.InverseTransformPoint(remediationCoin.transform.position).x;
        if (Mathf.Abs(remediationX - (-4f)) > 0.01f) throw new Exception("Remediation coin should sit at x = -4.");

        Directory.CreateDirectory("Logs/NumberLineValidation");
        File.WriteAllText("Logs/NumberLineValidation/result.txt",
            "PASS: serialized references, sign-coloring rule, four coins at +3/-2/0/-5 revealed one at a time, " +
            "persisted coin mesh, eleven labels, fixed sign text, world-space gaze-following canvas, startup scene, " +
            "intro-popup/start-button/congrats/confetti wiring, checkpoint+remediation dormant at start, " +
            "EventSystem present, both GraphicRaycaster (mouse) and TrackedDeviceGraphicRaycaster (controller rays) present, " +
            "XRInteractionManager present.\n" + DateTime.Now);
        Capture();
        Debug.Log("Number Line Playground validation passed.");
    }

    [MenuItem("Tools/Number Line Playground/Rebuild and Validate %#j")]
    public static void Rebuild()
    {
        NumberLinePlaygroundBuilder.Build();
        Validate();
    }

    static void Capture()
    {
        var camera = Camera.main;
        if (camera == null) throw new Exception("No player camera.");
        var old = camera.targetTexture;
        var active = RenderTexture.active;
        var rt = new RenderTexture(1536, 1024, 24);
        var image = new Texture2D(1536, 1024, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            image.Apply();
            File.WriteAllBytes("Logs/NumberLineValidation/preview.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = old;
            RenderTexture.active = active;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
