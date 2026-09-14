using UnityEngine;

/// Slowly rotates a collectible (coin) so it reads as interactive/alive in the world.
public class Spin : MonoBehaviour
{
    public float degreesPerSecond = 90f;

    void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
