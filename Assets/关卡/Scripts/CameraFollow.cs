using UnityEngine;

namespace Qingming.LevelBackground
{
    /// <summary>
    /// 2D 摄像机跟随脚本：只平滑跟随目标的 X，Y 和 Z 固定不变。
    /// 可配置跟随偏移和平滑速度。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随目标")]
        [Tooltip("要跟随的玩家 Transform（留空则自动找名为 'Player' 的物体）")]
        public Transform target;

        [Header("跟随参数")]
        [Tooltip("摄像机相对于目标的偏移（X=水平，Y=垂直）")]
        public Vector2 offset = new Vector2(0f, 1.5f);

        [Tooltip("X 轴平滑速度（越大越快，1=瞬间，100=几乎瞬间）")]
        [Range(0.5f, 30f)]
        public float smoothSpeedX = 8f;

        [Tooltip("Y 轴平滑速度（可以比 X 小一点，让 Y 跟随更柔和）")]
        [Range(0.5f, 30f)]
        public float smoothSpeedY = 5f;

        [Header("边界限制（可选）")]
        [Tooltip("是否启用 X 轴边界限制")]
        public bool clampX = true;
        [Tooltip("摄像机中心允许的最小 X")]
        public float minX = -20f;
        [Tooltip("摄像机中心允许的最大 X")]
        public float maxX = 20f;

        private Camera _cam;
        private float _fixedZ;
        private float _fixedY;
        private bool _boundsInited;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _fixedZ = transform.position.z;
            _fixedY = transform.position.y;
        }

        private void Start()
        {
            // 自动从背景的 SpriteRenderer 算出 X 边界
            // 摄像机中心不能越出：minX = bg.min.x + camHalfW, maxX = bg.max.x - camHalfW
            if (clampX && !_boundsInited)
            {
                var bgGo = GameObject.Find("第一关-春");
                if (bgGo == null) bgGo = GameObject.Find("Background");
                if (bgGo != null)
                {
                    var sr = bgGo.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        float camHalfW = _cam.orthographicSize * _cam.aspect;
                        var b = sr.bounds;
                        minX = b.min.x + camHalfW;
                        maxX = b.max.x - camHalfW;
                        // 如果背景比摄像机还窄，就不 clamp
                        if (maxX < minX) { clampX = false; }
                        _boundsInited = true;
                        Debug.Log($"[CameraFollow] 自动算边界: minX={minX:F2}  maxX={maxX:F2}  bg=({b.min.x:F2}~{b.max.x:F2})  camHalfW={camHalfW:F2}");
                    }
                }
            }
        }

        private void LateUpdate()
        {
            // 自动找 Player
            if (target == null)
            {
                var go = GameObject.Find("Player");
                if (go != null) target = go.transform;
                else return;
            }

            // 只跟随 X，Y 固定在初始位置
            float desiredX = target.position.x + offset.x;

            if (clampX)
                desiredX = Mathf.Clamp(desiredX, minX, maxX);

            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, desiredX, smoothSpeedX * Time.deltaTime);
            pos.y = _fixedY;
            pos.z = _fixedZ;

            transform.position = pos;
        }

        /// <summary>供外部设置边界（比如根据背景 bounds 自动算）</summary>
        public void SetBounds(float min, float max)
        {
            minX = min;
            maxX = max;
            clampX = true;
        }
    }
}
