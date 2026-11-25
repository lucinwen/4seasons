using UnityEngine;

/// <summary>
/// 喂蚕小游戏中拖拽桑叶成功后冒出的小爱心：快速放大 → 上浮 → 淡出。
/// </summary>
public class FeedHeartEffect : MonoBehaviour
{
    private const float Duration = 1.0f;
    private const float FloatHeight = 0.9f;

    private SpriteRenderer _sr;
    private Vector3 _start;
    private Vector3 _baseScale;
    private Color _color;
    private float _t;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _start = transform.position;
        _baseScale = transform.localScale;
        _color = _sr != null ? _sr.color : Color.white;
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / Duration);

        transform.position = _start + Vector3.up * (FloatHeight * k);

        float s = k < 0.25f
            ? Mathf.Lerp(0f, 1.25f, k / 0.25f)
            : Mathf.Lerp(1.25f, 1f, (k - 0.25f) / 0.75f);
        transform.localScale = _baseScale * s;

        if (_sr != null)
        {
            var c = _color;
            c.a = 1f - Mathf.Clamp01((k - 0.35f) / 0.65f);
            _sr.color = c;
        }

        if (k >= 1f) Destroy(gameObject);
    }
}
