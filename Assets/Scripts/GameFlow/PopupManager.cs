using UnityEngine;

namespace NumberLinePlayground.Flow
{
    /// Keeps a world-space popup canvas floating ~1.5m in front of the player at eye level,
    /// gently following their gaze (smoothed, not rigidly locked to the head) — per the
    /// design doc's motion-sickness note.
    [RequireComponent(typeof(Canvas))]
    public class PopupManager : MonoBehaviour
    {
        [SerializeField] Transform playerCamera;
        [SerializeField] float distance = 1.5f;
        [SerializeField] float followSpeed = 2f;
        [SerializeField] float rotationSpeed = 2f;

        public void SetPlayerCamera(Transform camera) => playerCamera = camera;

        void LateUpdate()
        {
            if (playerCamera == null) return;

            // Use only the horizontal (yaw) component of where the player is looking — not
            // full 3D forward including pitch. Otherwise looking down (which happens
            // constantly here, reading number boxes and searching for coins on the ground)
            // drags the popup down toward the ground and tilts it to an unreadable, unclickable
            // grazing angle instead of keeping it steady at eye level.
            Vector3 flatForward = playerCamera.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.ProjectOnPlane(playerCamera.up, Vector3.up);
            flatForward.Normalize();

            Vector3 targetPos = playerCamera.position + flatForward * distance;
            transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

            Vector3 lookDir = transform.position - playerCamera.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
