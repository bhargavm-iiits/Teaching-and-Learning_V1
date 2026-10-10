using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpeedTimeGraphFree
{
    /// <summary>Managers and every reference wired at build time (buttons get persistent listeners).</summary>
    public static partial class SpeedTimeFreeBuilder
    {
        static void Managers(TripleVehicleArena arena)
        {
            game.graph = controller; controller.game = game;
            game.arena = arena;
            game.playback = playbackController; playbackController.game = game;
            game.popup = popupManager; game.finalCheck = finalManager; finalManager.game = game;
            game.proof = root.gameObject.AddComponent<ProveItReplay>(); game.proof.game = game;
            game.feedback = presenter; game.checklist = checklistPanel;
            game.sounds = root.gameObject.AddComponent<AudioCues>();
            var src = eye.gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0; game.sounds.source = src;
            rigDeckInput.game = game; rigFallback.game = game;
            controller.vehicleColors = new[] { GraphPanel.Hex("#3D9BFF"), GraphPanel.Hex("#FF4D5A"), GraphPanel.Hex("#FF9F1A") };

            // Graph Controller buttons: RUN, PAUSE, RESET, 1x, UNDO, DELETE POINT, CLEAR GRAPH, HINT.
            var b = pendingButtons;
            UnityEventTools.AddPersistentListener(b[0].onClick, game.Run);
            UnityEventTools.AddPersistentListener(b[1].onClick, playbackController.TogglePause);
            UnityEventTools.AddPersistentListener(b[2].onClick, game.ResetRun);
            UnityEventTools.AddPersistentListener(b[3].onClick, playbackController.CycleRate);
            UnityEventTools.AddPersistentListener(b[4].onClick, controller.Undo);
            UnityEventTools.AddPersistentListener(b[5].onClick, controller.DeleteSelected);
            UnityEventTools.AddPersistentListener(b[6].onClick, controller.ClearGraph);
            UnityEventTools.AddPersistentListener(b[7].onClick, game.Hint);
            UnityEventTools.AddPersistentListener(presenter.watchReplay.onClick, game.Replay);
            UnityEventTools.AddPersistentListener(presenter.edit.onClick, game.EditAgain);
            UnityEventTools.AddPersistentListener(presenter.next.onClick, game.Next);
            UnityEventTools.AddPersistentListener(finalManager.prev.onClick, finalManager.Prev);
            UnityEventTools.AddPersistentListener(finalManager.next.onClick, finalManager.Next);
            UnityEventTools.AddPersistentListener(finalManager.submit.onClick, finalManager.Submit);
            UnityEventTools.AddPersistentListener(finalManager.proveIt.onClick, finalManager.ProveIt);

            // Graph-to-world beams removed at the user's request (no lines across the sky during the game).
            game.links = null;
        }

        static Material ParticlesUnlit(string name, Color c)
        {
            string path = Folder + "/Materials/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader; m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One); m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m); return m;
        }
    }
}