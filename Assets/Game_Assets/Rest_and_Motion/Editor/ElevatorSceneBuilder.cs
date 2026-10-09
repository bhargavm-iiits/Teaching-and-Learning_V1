using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Management;

namespace TeachingAndLearning.RestAndMotion.Editor
{
    public static partial class ElevatorSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Rest_and_Motion.unity";
        const string ImportedPath = "Assets/Rest_and_Motion/Imported";
        const string RigPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        static readonly Color Blue = new Color(0.12f, 0.48f, 0.98f);
        static readonly Color Green = new Color(0.14f, 0.8f, 0.22f);
        static TMP_FontAsset font;

        [MenuItem("Tools/Rest and Motion/Build Interactive VR Elevator")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before rebuilding the VR scene.");
            PrepareSources();
            var rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (rigAsset == null) throw new FileNotFoundException("The supplied XR Starter Assets rig is required.", RigPath);
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Backups/RestAndMotion");
                File.Copy(ScenePath, "Backups/RestAndMotion/Rest_and_Motion_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var root = new GameObject("Rest and Motion - Interactive VR Elevator").transform;
            BuildLighting(root);
            var exterior = BuildImportedWorld(root, out var cabinBounds);
            BuildScenery(exterior);
            BuildLandingTower(exterior);

            var car = root.gameObject.AddComponent<LiftCar>();
            car.exterior = exterior;
            car.exteriorAtFloorZero = new Vector3(0f, -GroundY - 0.05f, 0f);
            car.floorHeight = FloorHeight;
            car.startFloor = 1;
            var lesson = root.gameObject.AddComponent<ElevatorVRLesson>();
            lesson.car = car;
            lesson.exterior = exterior;
            var rig = BuildVRPlayer(root, rigAsset, cabinBounds);
            lesson.eye = rig.GetComponent<XROrigin>().Camera;
            rig.GetComponent<ElevatorDesktopPlayer>().lesson = lesson;

            lesson.rig = rig.transform;
            lesson.desktop = rig.GetComponent<ElevatorDesktopPlayer>();
            lesson.fade = BuildFade(lesson.eye);
            BuildTwinLiftTower(exterior, lesson, root.Find("Supplied Cabin - winda.fbx").gameObject);
            var targets = new List<ElevatorVRInteractable>();
            BuildLiftPanel(root, car, lesson, targets);
            BuildWorkingDoors(root, car);
            BuildLessonBoard(root, lesson, cabinBounds, targets);
            BuildPopup(root, lesson, targets);
            car.Initialise();
            lesson.targets = targets.ToArray();
            root.gameObject.AddComponent<ElevatorXRSession>().fallbackManager = DesktopXRManager();
            AddCabinColliders(root);
            BuildFreeFall(root, lesson, car, cabinBounds);
            ValidateVRScene(root, lesson);
            lesson.popup.SetActive(false);
            car.ResetTo(car.startFloor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            CaptureVRViews(lesson.eye, car, "Work/RestAndMotion/VR/renders");
            Debug.Log("[Rest and Motion VR] Saved the interactive lift scene to " + ScenePath);
        }

        /// <summary>Batch entry point: Unity -batchmode -executeMethod ...ElevatorSceneBuilder.BatchBuild</summary>
        public static void BatchBuild()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory("Work/RestAndMotion/VR");
                File.WriteAllText("Work/RestAndMotion/VR/build-error.txt", exception.ToString());
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static GameObject BuildVRPlayer(Transform root, GameObject rigAsset, Bounds bounds)
        {
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, root);
            rig.name = "XR Origin - Room-scale VR Player";
            rig.transform.position = new Vector3(-0.1f, 0f, -bounds.size.z * 0.25f);
            var origin = rig.GetComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = 1.65f;
            origin.CameraFloorOffsetObject.transform.localPosition = Vector3.up * 1.65f;
            var eye = origin.Camera;
            eye.tag = "MainCamera";
            eye.orthographic = false;
            eye.rect = new Rect(0f, 0f, 1f, 1f);
            eye.fieldOfView = 78f;
            eye.nearClipPlane = 0.025f;
            eye.farClipPlane = 1500f;
            eye.clearFlags = CameraClearFlags.Skybox;
            eye.backgroundColor = new Color(0.36f, 0.63f, 0.93f);
            eye.stereoTargetEye = StereoTargetEyeMask.Both;
            var cameraData = eye.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null) cameraData = eye.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.allowXRRendering = true;
            cameraData.renderPostProcessing = false;
            foreach (var behaviour in rig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                string type = behaviour.GetType().Name;
                if (type.Contains("MoveProvider") || type.Contains("GravityProvider") || type.Contains("TeleportationProvider") || type == "ContinuousTurnProvider")
                    behaviour.enabled = false;
            }
            foreach (var ray in rig.GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (ray.gameObject.name.Contains("Teleport")) ray.gameObject.SetActive(false);
                else
                {
                    ray.maxRaycastDistance = 120f;
                    ray.interactionLayers = 1;
                    ray.enableUIInteraction = false;
                }
            }
            foreach (var interactor in rig.GetComponentsInChildren<NearFarInteractor>(true))
            {
                interactor.interactionLayers = 1;
                interactor.enableFarCasting = true;
                interactor.enableUIInteraction = false;
                bool leftHand = false;
                foreach (var ancestor in interactor.GetComponentsInParent<Transform>())
                    if (ancestor.name == "Left Controller") leftHand = true;
                string hand = leftHand ? "LeftHand" : "RightHand";
                var select = new InputAction("Elevator " + hand + " Select", InputActionType.Button);
                select.AddBinding("<XRController>{" + hand + "}/triggerPressed");
                select.AddBinding("<XRController>{" + hand + "}/gripPressed");
                var value = new InputAction("Elevator " + hand + " Select Value", InputActionType.Value);
                value.AddBinding("<XRController>{" + hand + "}/trigger");
                value.AddBinding("<XRController>{" + hand + "}/grip");
                interactor.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction;
                interactor.selectInput.inputActionPerformed = select;
                interactor.selectInput.inputActionValue = value;
            }
            foreach (var caster in rig.GetComponentsInChildren<CurveInteractionCaster>(true)) caster.castDistance = 120f;
            var desktop = rig.AddComponent<ElevatorDesktopPlayer>();
            desktop.eye = eye;
            desktop.walkBounds = new Bounds(bounds.center, new Vector3(bounds.size.x - 0.65f, bounds.size.y, bounds.size.z - 0.7f));
            foreach (var child in rig.GetComponentsInChildren<Transform>(true))
                if (child.name == "Left Controller" || child.name == "Right Controller") desktop.controllerRoots.Add(child.gameObject);
            new GameObject("XR Interaction Manager", typeof(XRInteractionManager)).transform.SetParent(root, false);
            return rig;
        }

        static XRManagerSettings DesktopXRManager()
        {
            var settings = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (settings == null || settings.Manager == null || settings.Manager.activeLoaders.Count == 0)
                throw new InvalidOperationException("Enable the existing OpenXR loader for Standalone in XR Plug-in Management.");
            return settings.Manager;
        }

        static void AddCabinColliders(Transform root)
        {
            var cabin = root.Find("Supplied Cabin - winda.fbx");
            foreach (var filter in cabin.GetComponentsInChildren<MeshFilter>())
            {
                var source = filter.sharedMesh;
                var collisionMesh = UnityEngine.Object.Instantiate(source);
                var triangles = new List<int>();
                for (int submesh = 0; submesh < source.subMeshCount - 1; submesh++) triangles.AddRange(source.GetTriangles(submesh));
                collisionMesh.subMeshCount = 1;
                collisionMesh.SetTriangles(triangles, 0);
                var saved = SaveAsset(collisionMesh, ImportedPath + "/" + SafeName(source.name) + "_OpaqueCollision.asset");
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = saved;
            }
        }

        static void BuildLighting(Transform root)
        {
            var light = new GameObject("Daylight").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            light.color = new Color(1f, 0.93f, 0.8f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            var cabinLight = new GameObject("Warm cabin light").AddComponent<Light>();
            cabinLight.transform.SetParent(root, false);
            cabinLight.transform.localPosition = new Vector3(0f, 3.1f, -0.2f);
            cabinLight.type = LightType.Point;
            cabinLight.color = new Color(1f, 0.82f, 0.6f);
            cabinLight.intensity = 1.6f;
            cabinLight.range = 4.5f;
            cabinLight.shadows = LightShadows.None;
            var sky = AssetDatabase.LoadAssetAtPath<Material>(ImportedPath + "/Materials/RamSky.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, ImportedPath + "/Materials/RamSky.mat");
            }
            sky.SetColor("_SkyTint", new Color(0.45f, 0.62f, 0.9f));
            sky.SetColor("_GroundColor", new Color(0.42f, 0.5f, 0.42f));
            sky.SetFloat("_AtmosphereThickness", 0.85f);
            sky.SetFloat("_Exposure", 1.25f);
            sky.SetFloat("_SunSize", 0.035f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.68f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.64f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.32f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.92f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0011f;
        }

        static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
