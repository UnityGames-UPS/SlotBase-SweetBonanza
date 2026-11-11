using UnityEngine;

/// <summary>
/// Controls background cloud parallax scrolling and loops clouds from start to end seamlessly.
/// </summary>
public class CloudParallax : MonoBehaviour
{
    [System.Serializable]
    public class CloudLayer
    {
        [SerializeField] internal Transform cloud;

        [Range(0.01f, 5f)]
        [SerializeField] internal float speed = 0.2f;
    }

    [Header("Cloud Objects")]
    [SerializeField] private CloudLayer[] clouds;

    [Header("Movement Positions")]
    [SerializeField] private Transform startPosition;
    [SerializeField] private Transform endPosition;

    void Update()
    {
        if (startPosition == null || endPosition == null || clouds == null || clouds.Length == 0)
            return;

        // Determine move direction based on start and end positions in local space
        float moveDir = (endPosition.localPosition.x >= startPosition.localPosition.x) ? 1f : -1f;
        float totalSpan = clouds.Length * 1917f;

        foreach (CloudLayer layer in clouds)
        {
            if (layer.cloud == null)
                continue;

            // Move cloud towards end position in local UI space (resolution-independent)
            Vector3 pos = layer.cloud.localPosition;
            pos.x += moveDir * (layer.speed * 160f) * Time.deltaTime;

            // Check if cloud has reached or passed end position in local space and loop seamlessly
            if (moveDir > 0f)
            {
                if (pos.x >= endPosition.localPosition.x)
                {
                    pos.x -= totalSpan;
                }
            }
            else
            {
                if (pos.x <= endPosition.localPosition.x)
                {
                    pos.x += totalSpan;
                }
            }

            layer.cloud.localPosition = pos;
        }
    }
}
