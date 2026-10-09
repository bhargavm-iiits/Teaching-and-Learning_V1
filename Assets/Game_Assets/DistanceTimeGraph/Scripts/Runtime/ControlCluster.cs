using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DistanceTimeGraph
{
    /// Keeps the graph, monitor, activity panel and buttons at a comfortable height for this learner: once tracking
    /// has settled the whole cluster moves up or down to match the head. Press F2 to re-centre it.
    public class ControlCluster : MonoBehaviour
    {
        public Transform head;
        [Tooltip("Head height the cluster was laid out for (world Y).")]
        public float designHeadY = 2.9f;
        float baseY;

        IEnumerator Start()
        {
            baseY = transform.position.y;
            yield return new WaitForSeconds(0.4f);
            Fit();
            yield return new WaitForSeconds(1.2f);
            Fit();
        }

        public void Fit()
        {
            if (head == null) return;
            var p = transform.position;
            // Stay between a seated and a tall standing learner.
            float dy = Mathf.Clamp(head.position.y - designHeadY, -0.7f, 0.5f);
            transform.position = new Vector3(p.x, baseY + dy, p.z);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.f2Key.wasPressedThisFrame) Fit();
        }
    }
}
