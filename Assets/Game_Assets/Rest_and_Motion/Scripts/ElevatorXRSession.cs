using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace TeachingAndLearning.RestAndMotion
{
    public sealed class ElevatorXRSession : MonoBehaviour
    {
        public XRManagerSettings fallbackManager;
        XRManagerSettings manager;
        bool ownsSession;

        IEnumerator Start()
        {
            if (Application.isBatchMode) yield break;
            manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : fallbackManager;
            if (manager == null || manager.activeLoader != null) yield break;
            yield return manager.InitializeLoader();
            if (manager.activeLoader == null)
            {
                Debug.Log("[Rest and Motion VR] No active OpenXR headset. Desktop 3D preview remains available.");
                yield break;
            }
            manager.StartSubsystems();
            ownsSession = true;
        }

        void OnDestroy()
        {
            if (!ownsSession || manager == null) return;
            manager.StopSubsystems();
            manager.DeinitializeLoader();
        }
    }
}
