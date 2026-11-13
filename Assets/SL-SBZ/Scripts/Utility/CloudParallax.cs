using UnityEngine;

/// <summary>
/// Controls background cloud parallax scrolling and loops clouds from start to end seamlessly.
/// </summary>
internal class CloudParallax : MonoBehaviour
{
    [System.Serializable]
    internal class CloudLayer
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
        float endX = endPosition.localPosition.x;
        float moveDir = (endX >= startPosition.localPosition.x) ? 1f : -1f;
        float totalSpan = clouds.Length * 1917f;
        float stepBase = moveDir * 160f * Time.deltaTime;

        for (int i = 0; i < clouds.Length; i++)
        {
            CloudLayer layer = clouds[i];
            if (layer.cloud == null)
                continue;

            // Move cloud towards end position in local UI space (resolution-independent)
            Vector3 pos = layer.cloud.localPosition;
            pos.x += layer.speed * stepBase;

            // Check if cloud has reached or passed end position in local space and loop seamlessly
            if (moveDir > 0f)
            {
                if (pos.x >= endX)
                {
                    pos.x -= totalSpan;
                }
            }
            else
            {
                if (pos.x <= endX)
                {
                    pos.x += totalSpan;
                }
            }

            layer.cloud.localPosition = pos;
        }
    }
}
