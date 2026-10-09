using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Act = TeachingAndLearning.RestAndMotion.ElevatorVRInteractable.Action;

namespace TeachingAndLearning.RestAndMotion.Editor
{
    public static partial class ElevatorSceneBuilder
    {
        /// <summary>World-space pop-up (title, question, feedback, START / answer buttons). The game moves it between the plaza and the cabin.</summary>
        static void BuildPopup(Transform root, ElevatorVRLesson lesson, List<ElevatorVRInteractable> targets)
        {
            var cabinAnchor = new GameObject("Pop-up anchor (cabin)").transform;
            cabinAnchor.SetParent(root, false);
            var eye = new Vector3(-0.1f, 1.65f, -0.31f);
            cabinAnchor.position = new Vector3(-0.25f, 1.5f, 0.72f);
            cabinAnchor.rotation = Quaternion.LookRotation(cabinAnchor.position - eye, Vector3.up);
            cabinAnchor.localScale = Vector3.one * 0.8f;
            lesson.popupCabinAnchor = cabinAnchor;

            var popup = new GameObject("Pop-up question").transform;
            popup.SetParent(cabinAnchor, false);
            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Pop-up backing";
            backing.transform.SetParent(popup, false);
            backing.transform.localScale = new Vector3(1.02f, 0.68f, 0.02f);
            backing.transform.localPosition = new Vector3(0f, 0f, 0.015f);
            backing.GetComponent<Renderer>().sharedMaterial = UnlitMaterial("RamBoard", Navy);
            backing.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(backing.GetComponent<Collider>());
            VRBorder(popup, new Vector2(1.02f, 0.68f), 0.008f).sharedMaterial = OutlineMaterial();
            lesson.popupTitle = VRText(popup, "Title", "", new Vector3(0f, 0.27f, 0f), new Vector2(0.96f, 0.08f), 0.42f, new Color(0.5f, 0.75f, 1f));
            lesson.popupText = VRText(popup, "Question", "", new Vector3(0f, 0.11f, 0f), new Vector2(0.94f, 0.22f), 0.4f, Color.white);
            lesson.popupFeedback = VRText(popup, "Feedback", "", new Vector3(0f, -0.085f, 0f), new Vector2(0.94f, 0.16f), 0.28f, new Color(1f, 0.85f, 0.5f));
            for (int i = 0; i < 3; i++)
            {
                var answer = BoardButton(popup, lesson, Act.Answer, "ANSWER", new Vector3((i - 1) * 0.32f, -0.245f, 0f), new Vector2(0.29f, 0.1f), targets);
                answer.answerIndex = i;
                lesson.answers[i] = answer;
            }
            lesson.startButton = BoardButton(popup, lesson, Act.Ready, "START", new Vector3(0f, -0.245f, 0f), new Vector2(0.38f, 0.1f), targets);
            lesson.popup = popup.gameObject;
        }

        /// <summary>Level 3 hardware: red emergency light, alarm/wind/effect sounds, sparks at the brake shoes, and loose props on the floor.</summary>
        static void BuildFreeFall(Transform root, ElevatorVRLesson lesson, LiftCar car, Bounds cabinBounds)
        {
            var fall = root.gameObject.AddComponent<FreeFallSequence>();
            fall.car = car;
            fall.cabinLight = root.Find("Warm cabin light").GetComponent<Light>();
            var red = new GameObject("Emergency light (red)").AddComponent<Light>();
            red.transform.SetParent(root, false);
            red.transform.localPosition = new Vector3(0f, 3.0f, 0.3f);
            red.type = LightType.Point;
            red.color = new Color(1f, 0.12f, 0.1f);
            red.intensity = 1.5f;
            red.range = 5f;
            red.shadows = LightShadows.None;
            red.enabled = false;
            fall.emergencyLight = red;
            fall.alarmAudio = Source(root, "Alarm", new Vector3(0f, 3.2f, 0f), 0.4f);
            fall.windAudio = Source(root, "Falling wind", new Vector3(0f, 1.6f, 1.2f), 0.2f);
            fall.effectsAudio = Source(root, "Snap, screech and thud", new Vector3(0f, 0.3f, 0f), 0.3f);
            fall.brakeHeight = 3f * FloorHeight;

            var floor = FloorHeightAt(new Vector3(0.55f, 2f, 0.55f), cabinBounds);
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Loose ball (floats in free fall)";
            ball.transform.SetParent(root, false);
            ball.transform.localScale = Vector3.one * 0.22f;
            ball.transform.position = new Vector3(0.55f, floor + 0.11f, 0.55f);
            ball.GetComponent<Renderer>().sharedMaterial = LitMaterial("RamBall", new Color(1f, 0.78f, 0.15f), 0.4f, 0f);
            Object.DestroyImmediate(ball.GetComponent<Collider>());
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Loose clipboard (floats in free fall)";
            board.transform.SetParent(root, false);
            board.transform.localScale = new Vector3(0.3f, 0.02f, 0.4f);
            board.transform.position = new Vector3(-0.55f, FloorHeightAt(new Vector3(-0.55f, 2f, 0.6f), cabinBounds) + 0.01f, 0.6f);
            board.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            board.GetComponent<Renderer>().sharedMaterial = LitMaterial("RamClipboard", new Color(0.55f, 0.36f, 0.2f), 0.3f, 0f);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            fall.looseProps = new[] { ball.transform, board.transform };

            var sparkMaterial = UnlitParticleMaterial("RamSparks", new Color(1f, 0.72f, 0.25f));
            var sparks = new List<ParticleSystem>();
            foreach (float side in new[] { -1f, 1f })
            {
                var go = new GameObject("Brake sparks");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(side * 1.05f, 0.05f, cabinBounds.max.z + 0.08f);
                go.transform.rotation = Quaternion.Euler(-70f, side * 20f, 0f);
                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.playOnAwake = false;
                main.duration = 1f;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.4f), new Color(1f, 0.45f, 0.1f));
                main.gravityModifier = 0.8f;
                main.maxParticles = 600;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = ps.emission;
                emission.rateOverTime = 320f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 28f;
                shape.radius = 0.04f;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.05f;
                renderer.lengthScale = 1.5f;
                renderer.sharedMaterial = sparkMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                sparks.Add(ps);
            }
            fall.sparks = sparks.ToArray();
            lesson.fall = fall;
        }

        static float FloorHeightAt(Vector3 from, Bounds cabinBounds)
        {
            Physics.SyncTransforms();
            return Physics.Raycast(from, Vector3.down, out var hit, 3f) ? hit.point.y : cabinBounds.min.y + 0.05f;
        }

        static AudioSource Source(Transform root, string name, Vector3 position, float spatial)
        {
            var source = new GameObject(name).AddComponent<AudioSource>();
            source.transform.SetParent(root, false);
            source.transform.localPosition = position;
            source.playOnAwake = false;
            source.spatialBlend = spatial;
            return source;
        }

        static Material UnlitParticleMaterial(string name, Color color)
        {
            string path = ImportedPath + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Small black sphere around the eye for fades between levels.</summary>
        static ScreenFade BuildFade(Camera eye)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Fade sphere";
            sphere.transform.SetParent(eye.transform, false);
            sphere.transform.localScale = Vector3.one * 0.3f;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            string path = ImportedPath + "/Materials/RamFade.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", Color.black);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 4000;
            EditorUtility.SetDirty(material);
            var renderer = sphere.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.enabled = false;
            var fade = eye.gameObject.AddComponent<ScreenFade>();
            fade.sphere = renderer;
            return fade;
        }
    }
}
