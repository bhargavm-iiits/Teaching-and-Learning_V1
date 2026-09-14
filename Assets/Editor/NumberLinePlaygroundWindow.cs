using UnityEditor;
using UnityEngine;

/// Small dockable window with a one-click button to (re)build the Number Line Playground scene.
public class NumberLinePlaygroundWindow : EditorWindow
{
    [MenuItem("Tools/Number Line Playground/Open Builder Window")]
    public static void Open()
    {
        var window = GetWindow<NumberLinePlaygroundWindow>();
        window.titleContent = new GUIContent("Number Line Playground");
        window.minSize = new Vector2(280f, 90f);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Duplicates Demo_07 into a new scene and builds the number line, signs, player, and HUD.",
            MessageType.Info);

        EditorGUILayout.Space(8);
        var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 36 };
        if (GUILayout.Button("Build Environment", buttonStyle))
        {
            NumberLinePlaygroundBuilder.Build();
        }
    }
}
