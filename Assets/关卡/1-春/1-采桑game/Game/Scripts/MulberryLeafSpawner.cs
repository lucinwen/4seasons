using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Qingming.Harvest
{
    /// <summary>
    /// 桑叶生成管理器：定时从画面顶部随机位置生成桑叶，委托给 FallingMulberryLeaf 处理下落。
    /// </summary>
    public class MulberryLeafSpawner : MonoBehaviour
    {
        [Header("桑叶素材")]
        public Sprite[] leafSprites;

        [Header("Prefab（可为空，空则运行时动态创建）")]
        public FallingMulberryLeaf leafPrefab;

        [Header("生成频率（秒）")]
        public float spawnIntervalMin = 0.8f;
        public float spawnIntervalMax = 1.5f;

        [Header("下落速度（单位/秒）")]
        public float fallSpeedMin = 2.5f;
        public float fallSpeedMax = 4f;

        [Header("生成边界（世界坐标 X）")]
        [Tooltip("会从 Path bounds 自动算出（减去 swayAmplitude 避免飞出）")]
        public float spawnXMin;
        public float spawnXMax;

        [Header("Y 范围")]
        [Tooltip("在摄像机顶部外多少单位生成")]
        public float spawnY = 5f;
        [Tooltip("掉落到此 Y 之下即自动销毁")]
        public float destroyY = -5f;

        [Header("视觉尺寸统一")]
        [Tooltip("桑叶目标显示高度（世界单位），0 表示不调整")]
        public float targetWorldHeight = 0.7f;

        [Header("摆动 & 旋转")]
        public float swayAmplitude = 0.3f;
        public float swayFrequency = 1.5f;
        public float spinMaxDegPerSec = 60f;

        [Header("渲染排序")]
        [Tooltip("桑叶的 sortingOrder（覆盖层模式需高于黑色遮罩）")]
        public int leafSortingOrder = 5;

        private Coroutine _spawnRoutine;
        private readonly List<FallingMulberryLeaf> _alive = new List<FallingMulberryLeaf>();
        private bool _boundsAutoSet;

        private void Awake()
        {
            DoAutoDetect();
        }

        private void Update()
        {
            // 双保险：如果 Awake 没跑到，Update 第一帧必须跑到
            if (!_boundsAutoSet) DoAutoDetect();
        }

        private void DoAutoDetect()
        {
            var pathGo = GameObject.Find("Path");
            if (pathGo != null)
            {
                var sr = pathGo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    spawnXMin = sr.bounds.min.x + 0.3f + swayAmplitude;
                    spawnXMax = sr.bounds.max.x - 0.3f - swayAmplitude;
                    _boundsAutoSet = true;
                }
            }
        }

        /// <summary>启动持续生成。</summary>
        public void StartSpawning()
        {
            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
            _spawnRoutine = StartCoroutine(SpawnLoop());
        }

        /// <summary>停止生成，已生成的桑叶继续下落。</summary>
        public void StopSpawning()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }
        }

        /// <summary>清除所有已生成的桑叶。</summary>
        public void ClearAllLeaves()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (_alive[i] != null) Destroy(_alive[i].gameObject);
            }
            _alive.Clear();
        }

        private IEnumerator SpawnLoop()
        {
            UnityEngine.Debug.Log("[Spawner] SpawnLoop STARTED. spawnXMin=" + spawnXMin + " spawnXMax=" + spawnXMax);
            while (true)
            {
                SpawnOne();
                float interval = Random.Range(spawnIntervalMin, spawnIntervalMax);
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnOne()
        {
            Sprite chosen = leafSprites != null && leafSprites.Length > 0
                ? leafSprites[Random.Range(0, leafSprites.Length)]
                : null;

            Vector3 pos = new Vector3(Random.Range(spawnXMin, spawnXMax), spawnY, 0f);
            UnityEngine.Debug.Log("[Spawner] SpawnOne at " + pos + " sprite=" + (chosen?.name ?? "null"));

            FallingMulberryLeaf leaf;
            if (leafPrefab != null)
            {
                leaf = Instantiate(leafPrefab, pos, Quaternion.identity);
            }
            else
            {
                leaf = CreateLeafAt(pos, chosen);
            }

            float fallSpeed = Random.Range(fallSpeedMin, fallSpeedMax);
            leaf.Initialize(fallSpeed, destroyY, swayAmplitude, swayFrequency, spinMaxDegPerSec, this);
            UnityEngine.Debug.Log("[Spawner] Leaf created: name=" + leaf.name + " pos=" + leaf.transform.position + " fallSpeed=" + fallSpeed);

            _alive.Add(leaf);
        }

        private FallingMulberryLeaf CreateLeafAt(Vector3 pos, Sprite sprite)
        {
            GameObject go = new GameObject("MulberryLeaf");
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = leafSortingOrder;

            // 统一桑叶显示尺寸
            if (sprite != null && targetWorldHeight > 0f)
            {
                float worldHeight = sprite.rect.height / sprite.pixelsPerUnit;
                float scale = targetWorldHeight / worldHeight;
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }

            // 2D 触发器 + Kinematic Rigidbody2D
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            FallingMulberryLeaf leaf = go.AddComponent<FallingMulberryLeaf>();
            return leaf;
        }

        internal void Unregister(FallingMulberryLeaf leaf)
        {
            _alive.Remove(leaf);
        }
    }
}
