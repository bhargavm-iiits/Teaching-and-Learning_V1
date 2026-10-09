using UnityEngine;
using UnityEngine.UI;

namespace DistanceTimeGraph
{
    /// The buttons under the graph. There is no steering, accelerator, brake or speed control: the graph is the only way
    /// to move the car.
    public class ControlConsole : MonoBehaviour
    {
        public Button run, pause, edit, replay, reset, delete;

        public void SetStates(bool canRun, bool canPause, bool canEdit, bool canReplay, bool canReset, bool canDelete, bool paused)
        {
            run.interactable = canRun;
            pause.interactable = canPause;
            edit.interactable = canEdit;
            replay.interactable = canReplay;
            reset.interactable = canReset;
            delete.interactable = canDelete;
            UiKit.SetLabel(pause, paused ? "RESUME" : "PAUSE");
        }
    }
}
