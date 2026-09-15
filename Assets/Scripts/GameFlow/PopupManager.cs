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
        [SerializeField] float heightOffset = 0f;   // how far above eye level the popup floats
        [SerializeField] float followSpeed = 3.2f;      // it still eases, but keeps up: at 2 the messages trailed a turn and looked slanted
        [SerializeField] float rotationSpeed = 3.2f;

        float snapUntil;

        public void SetPlayerCamera(Transform camera) => playerCamera = camera;

        /// Puts the popup straight in front of the player for the next moments, for when the player has just been moved.
        public void SnapToPlayer() => snapUntil = Time.time + 0.3f;

        public void Configure(float distance, float heightOffset)
        {
            this.distance = distance;
            this.heightOffset = heightOffset;
        }

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
            targetPos.y += heightOffset;

            // For the first moments (while the headset settles at its height) place the popup straight in front of the
            // player instead of easing it in from wherever the scene was saved.
            bool settling = Time.timeSinceLevelLoad < 0.5f || Time.time < snapUntil;
            transform.position = settling ? targetPos : Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

            Vector3 lookDir = transform.position - playerCamera.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = settling ? targetRot : Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
