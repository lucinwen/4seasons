using UnityEngine;

/// <summary>Small ambient marker that shows where a main-hall interaction can be found.</summary>
public sealed class WorkshopHallMarker : MonoBehaviour
{
    private SpriteRenderer marker;
    private Vector3 restingPosition;
    private Vector3 restingScale;
    private float phase;

    public void Initialize(SpriteRenderer renderer, Color color, float phaseOffset)
    {
        marker = renderer;
        restingPosition = transform.localPosition;
        restingScale = transform.localScale;
        phase = phaseOffset;
        if (marker != null)
        {
            marker.color = color;
            marker.sortingOrder = 18;
        }
    }

    private void Update()
    {
        if (marker == null) return;
        float pulse = 0.92f + Mathf.Sin(Time.time * 3.1f + phase) * 0.08f;
        transform.localScale = restingScale * pulse;
        transform.localPosition = restingPosition + Vector3.up * (Mathf.Sin(Time.time * 2.4f + phase) * 0.08f);
    }
}
