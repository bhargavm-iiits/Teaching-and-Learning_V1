using UnityEngine;

/// Rotates the object to face the active camera. Used for number-line labels and floating tags.
public class Billboard : MonoBehaviour
{
    static Camera cachedCam;

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            if (cachedCam == null) cachedCam = Object.FindFirstObjectByType<Camera>();
            cam = cachedCam;
        }
        if (cam == null) return;

        Vector3 dir = transform.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }
}
