using UnityEngine;

namespace Qingming.Harvest
{
    /// <summary>
    /// 单片桑叶的下落行为：垂直下落 + 正弦左右摆动 + 缓慢旋转 + 可被拾取。
    /// 与玩家/篮子的拾取区（LeafCollector Trigger）接触时触发 OnCollected。
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class FallingMulberryLeaf : MonoBehaviour
    {
        [Header("运行时参数（由 Spawner 注入）")]
        private float _fallSpeed;
        private float _destroyY;
        private float _swayAmp;
        private float _swayFreq;
        private float _spinDegPerSec;
        private MulberryLeafSpawner _spawner;

        private float _phase;
        private Vector3 _basePos;
        private bool _collected;

        /// <summary>供 Spawner 在生成后调用。</summary>
        public void Initialize(float fallSpeed, float destroyY, float swayAmp, float swayFreq,
            float spinDegPerSec, MulberryLeafSpawner spawner)
        {
            _fallSpeed = fallSpeed;
            _destroyY = destroyY;
            _swayAmp = swayAmp;
            _swayFreq = swayFreq;
            _spinDegPerSec = spinDegPerSec;
            _spawner = spawner;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _basePos = transform.position;
            _collected = false;
        }

        private int _updateTicks;

        private void Update()
        {
            if (_collected) return;

            // 防御：如果没被 Initialize（_spawner==null 且 _fallSpeed==0），超过 5 帧就自毁
            if (_spawner == null && _fallSpeed == 0f)
            {
                _updateTicks++;
                if (_updateTicks > 5)
                {
                    UnityEngine.Debug.LogWarning("[Leaf] Destroying uninitialized leaf: " + gameObject.name + " pos=" + transform.position);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                _updateTicks = 0;
            }

            if (_updateTicks > 0 && _updateTicks <= 3)
                UnityEngine.Debug.Log("[Leaf] Update tick=" + _updateTicks + " pos.y=" + transform.position.y.ToString("F2") + " fallSpeed=" + _fallSpeed);

            Vector3 pos = transform.position;
            _basePos.y -= _fallSpeed * Time.deltaTime;
            pos.y = _basePos.y;

            // 水平摆动
            _phase += _swayFreq * Time.deltaTime;
            pos.x = _basePos.x + Mathf.Sin(_phase) * _swayAmp;
            transform.position = pos;

            // 旋转
            transform.Rotate(0f, 0f, _spinDegPerSec * Time.deltaTime);

            if (pos.y < _destroyY)
            {
                DestroySelf();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected) return;

            // 寻找任意一个带有 LeafCollector 或 PlayerTag 的物体
            LeafCollector collector = other.GetComponentInParent<LeafCollector>();
            if (collector == null) collector = other.GetComponent<LeafCollector>();

            if (collector != null)
            {
                OnCollected();
            }
        }

        private void OnCollected()
        {
            if (_collected) return;
            _collected = true;

            // 禁用碰撞防止重复拾取
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // 通知 GameManager
            var mgr = MulberryHarvestGameManager.Instance;
            if (mgr != null) mgr.NotifyLeafCollected();
            WorkshopAudioFeedback.Collect();

            // 简单拾取视觉：轻微放大后销毁
            StartCoroutine(PickupAndDestroy());
        }

        private System.Collections.IEnumerator PickupAndDestroy()
        {
            float t = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 bigScale = startScale * 1.4f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.12f);
                transform.localScale = Vector3.Lerp(startScale, bigScale, p);
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - p);
                yield return null;
            }
            DestroySelf();
        }

        private void DestroySelf()
        {
            if (_spawner != null) _spawner.Unregister(this);
            Destroy(gameObject);
        }
    }
}
