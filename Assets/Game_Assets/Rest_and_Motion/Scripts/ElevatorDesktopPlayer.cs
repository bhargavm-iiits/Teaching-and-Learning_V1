using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace TeachingAndLearning.RestAndMotion
{
    /// <summary>
    /// Desktop preview when no headset is active: right-mouse look, WASD inside the cabin,
    /// left mouse presses (hold works for OPEN and the bell), Tab/Shift+Tab moves a keyboard focus
    /// and Enter/Space presses it. Number keys 0-9 press P/1-9, F10-F12 press 10-12.
    /// </summary>
    public sealed class ElevatorDesktopPlayer : MonoBehaviour
    {
        public Camera eye;
        public Bounds walkBounds;
        public List<GameObject> controllerRoots = new List<GameObject>();
        public ElevatorVRLesson lesson;
        public float walkSpeed = 0.65f;
        TrackedPoseDriver poseDriver;
        ElevatorVRInteractable hovered;
        ElevatorVRInteractable mouseHeld;
        ElevatorVRInteractable keyHeld;
        int focusIndex = -1;
        bool wasTracked;
        bool configured;
        float yaw;
        float pitch;

        void Awake()
        {
            poseDriver = eye.GetComponent<TrackedPoseDriver>();
            yaw = transform.eulerAngles.y;
        }

        void Update()
        {
            bool tracked = XRSettings.isDeviceActive;
            if (!configured || tracked != wasTracked)
            {
                configured = true;
                wasTracked = tracked;
                if (poseDriver != null) poseDriver.enabled = tracked;
                foreach (var controller in controllerRoots) if (controller != null) controller.SetActive(tracked);
                if (!tracked)
                {
                    eye.transform.localPosition = Vector3.zero;
                    eye.transform.localRotation = Quaternion.identity;
                    pitch = 0f;
                }
            }
            if (tracked) return;
            if (Mouse.current != null) MouseControls();
            if (Keyboard.current != null) KeyboardControls();
        }

        void MouseControls()
        {
            var mouse = Mouse.current;
            if (mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * 0.12f;
                pitch = Mathf.Clamp(pitch - delta.y * 0.12f, -75f, 75f);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                eye.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            var target = TargetUnderMouse(mouse.position.ReadValue());
            if (hovered != target && focusIndex < 0)
            {
                if (hovered != null) hovered.SetHovered(false);
                hovered = target;
                if (hovered != null) hovered.SetHovered(true);
            }
            if (target != null && mouse.leftButton.wasPressedThisFrame && target.Press()) mouseHeld = target;
            if (mouseHeld != null && !mouse.leftButton.isPressed) { mouseHeld.Release(); mouseHeld = null; }
        }

        ElevatorVRInteractable TargetUnderMouse(Vector2 screen)
        {
            var ray = eye.ScreenPointToRay(screen);
            var hits = Physics.RaycastAll(ray, 400f, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var candidate = hit.collider.GetComponentInParent<ElevatorVRInteractable>();
                if (candidate != null) return candidate;
                if (!hit.collider.isTrigger) break;
            }
            return null;
        }

        void KeyboardControls()
        {
            var keyboard = Keyboard.current;
            var movement = new Vector3((keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f), 0f, (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            if (movement.sqrMagnitude > 0f)
            {
                var position = transform.position + transform.TransformDirection(movement.normalized) * walkSpeed * Time.deltaTime;
                position.x = Mathf.Clamp(position.x, walkBounds.min.x, walkBounds.max.x);
                position.z = Mathf.Clamp(position.z, walkBounds.min.z, walkBounds.max.z);
                transform.position = position;
            }
            if (lesson == null || lesson.targets == null) return;
            var usable = lesson.targets.Where(t => t != null && t.gameObject.activeInHierarchy).ToList();
            if (keyboard.tabKey.wasPressedThisFrame && usable.Count > 0)
            {
                bool back = keyboard.shiftKey.isPressed;
                focusIndex = focusIndex < 0 ? (back ? usable.Count - 1 : 0) : (focusIndex + (back ? usable.Count - 1 : 1)) % usable.Count;
                if (hovered != null) hovered.SetHovered(false);
                hovered = usable[focusIndex];
                hovered.SetHovered(true);
            }
            if (keyboard.escapeKey.wasPressedThisFrame && focusIndex >= 0)
            {
                focusIndex = -1;
                if (hovered != null) hovered.SetHovered(false);
                hovered = null;
            }
            if (focusIndex >= 0 && hovered != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) && hovered.Press()) keyHeld = hovered;
            if (keyHeld != null && !keyboard.enterKey.isPressed && !keyboard.spaceKey.isPressed) { keyHeld.Release(); keyHeld = null; }
            var digits = new[] { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.F10, Key.F11, Key.F12 };
            for (int floor = 0; floor < digits.Length; floor++)
                if (keyboard[digits[floor]].wasPressedThisFrame)
                {
                    var button = usable.FirstOrDefault(t => t.action == ElevatorVRInteractable.Action.Floor && t.floorNumber == floor);
                    if (button != null) button.Activate();
                }
        }

        /// <summary>Move the player between the plaza (Level 1) and the cabin (Levels 2-3) behind a fade.</summary>
        public void Teleport(Vector3 position, float newYaw, Bounds bounds)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, newYaw, 0f));
            yaw = newYaw;
            pitch = 0f;
            walkBounds = bounds;
            if (eye != null && !XRSettings.isDeviceActive) eye.transform.localRotation = Quaternion.identity;
        }

        void OnDisable()
        {
            if (hovered != null) hovered.SetHovered(false);
            if (mouseHeld != null) mouseHeld.Release();
            if (keyHeld != null) keyHeld.Release();
        }
    }
}
