using System.IO;
using NumberLinePlayground.Flow;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// Builds (or updates) a reusable world-space Popup prefab: a title, a body, and up to
/// 3 buttons, with a PopupManager for gaze-following and a PopupUi for content wiring.
/// This is the "reusable pop-up prefab" piece of the GameFlowManager foundation — it isn't
/// wired into the current NumberLinePlayground scene, just built and saved as an asset.
public static class PopupPrefabBuilder
{
    const string PrefabPath = "Assets/Prefabs/GameFlow/Popup.prefab";

    [MenuItem("Tools/Game Flow/Build Reusable Popup Prefab")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Prefabs/GameFlow");

        var root = new GameObject("Popup", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 400);
        root.transform.localScale = Vector3.one * 0.002f; // ~1.2m wide at this pixel size
        root.AddComponent<PopupManager>();

        var frame = CreatePanel(root.transform, "Frame", new Color(.05f, .18f, .8f, .96f), new Vector2(600, 400));
        var title = CreateText(frame.transform, "Title", "Title", 34, Color.white, new Vector2(0, 150), new Vector2(560, 70));
        var body = CreateText(frame.transform, "Body", "Body text goes here.", 24, Color.white, new Vector2(0, 20), new Vector2(560, 180));

        var buttonRow = new GameObject("Buttons", typeof(RectTransform)).transform;
        buttonRow.SetParent(frame.transform, false);
        var rowRt = buttonRow.GetComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(.5f, 0f);
        rowRt.anchoredPosition = new Vector2(0, 60);
        rowRt.sizeDelta = new Vector2(560, 70);

        var buttonA = CreateButton(buttonRow, "ButtonA", "Option A", new Vector2(-190, 0));
        var buttonB = CreateButton(buttonRow, "ButtonB", "Option B", new Vector2(0, 0));
        var buttonC = CreateButton(buttonRow, "ButtonC", "Option C", new Vector2(190, 0));

        var popupUi = root.AddComponent<PopupUi>();
        popupUi.Title = title;
        popupUi.Body = body;
        popupUi.ButtonA = buttonA;
        popupUi.ButtonB = buttonB;
        popupUi.ButtonC = buttonC;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        Debug.Log("Popup prefab built at " + PrefabPath);
    }

    static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Text CreateText(Transform parent, string name, string text, int size, Color color, Vector2 pos, Vector2 dims)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = dims;
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.raycastTarget = false;
        return t;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(170, 60);
        go.GetComponent<Image>().color = new Color(.15f, .5f, .3f);
        CreateText(go.transform, "Label", label, 24, Color.white, Vector2.zero, new Vector2(160, 55));
        return go.GetComponent<Button>();
    }
}
