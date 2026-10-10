using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace SpeedTimeGraphLesson.VR
{
    public class VRSessionBootstrap : MonoBehaviour
    {
        public XRManagerSettings managerSettings;
        XRManagerSettings activeManager;
        bool ownsSession;
        IEnumerator Start()
        {
            // Batch/editor interaction checks run without claiming the user's headset runtime.
            if(Application.isBatchMode)yield break;
            activeManager=XRGeneralSettings.Instance?XRGeneralSettings.Instance.Manager:managerSettings;
            if(!activeManager||activeManager.activeLoader)yield break;
            yield return activeManager.InitializeLoader();
            if(activeManager.activeLoader) { activeManager.StartSubsystems();ownsSession=true; }
        }
        void OnDestroy() { if(ownsSession&&activeManager) { activeManager.StopSubsystems();activeManager.DeinitializeLoader(); } }
    }
}
