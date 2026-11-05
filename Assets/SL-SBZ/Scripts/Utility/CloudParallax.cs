using UnityEngine;

/// <summary>
/// Controls background cloud parallax scrolling and loops clouds from start to end seamlessly.
/// </summary>
public class CloudParallax : MonoBehaviour
{
    [System.Serializable]
    public class CloudLayer
    {
        public Transform cloud;

        [Range(0.01f, 5f)]
        public float speed = 0.2f;
    }

    [Header("Cloud Objects")]
    public CloudLayer[] clouds;

    [Header("Movement Positions")]
    public Transform startPosition;
    public Transform endPosition;

    void Update()
    {
        if (startPosition == null || endPosition == null || clouds == null)
            return;

        // Determine move direction based on start and end positions (+1: moving right, -1: moving left)
        float moveDir = (endPosition.position.x >= startPosition.position.x) ? 1f : -1f;

        foreach (CloudLayer layer in clouds)
        {
            if (layer.cloud == null)
                continue;

            // Move cloud towards end position
            layer.cloud.Translate(
                Vector3.right * (moveDir * layer.speed * Time.deltaTime),
                Space.World
            );

            // Check if cloud has reached or passed end position
            if (moveDir > 0f)
            {
                // Moving Left -> Right
                if (layer.cloud.position.x >= endPosition.position.x)
                {
                    float overshoot = layer.cloud.position.x - endPosition.position.x;
                    Vector3 pos = layer.cloud.position;
                    pos.x = startPosition.position.x + overshoot;
                    layer.cloud.position = pos;
                }
            }
            else
            {
                // Moving Right -> Left
                if (layer.cloud.position.x <= endPosition.position.x)
                {
                    float overshoot = endPosition.position.x - layer.cloud.position.x;
                    Vector3 pos = layer.cloud.position;
                    pos.x = startPosition.position.x - overshoot;
                    layer.cloud.position = pos;
                }
            }
        }
    }
}
