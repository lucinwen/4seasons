using UnityEngine;
using UnityEngine.EventSystems;

namespace Qingming.LevelBackground
{
    /// <summary>
    /// 四季关卡场景的玩家控制器：物理驱动的水平移动、跑步、跳跃。
    /// 使用 Rigidbody2D Dynamic + velocity，使楼梯/平台碰撞体生效，玩家可跳跃上楼梯。
    /// 动画参数对齐 Assets/animation/player.controller（Idle / Walk / Run / Jump）。
    /// </summary>
    public class LevelPlayerController : MonoBehaviour
    {
        [Header("移动")]
        public float moveSpeed = 5f;
        public float runMultiplier = 1.6f;

        [Header("台阶")]
        [Tooltip("可直接迈上去的台阶最大高度（世界单位）；超过这个高度仍需要跳跃")]
        public float stepHeight = 0.45f;

        [Header("跳跃")]
        [Tooltip("跳跃高度（世界单位），代码会据此自动计算跳跃力度")]
        public float jumpHeight = 2.5f;

        [Header("子物体引用")]
        [Tooltip("Visual 子物体（用于翻转朝向）")]
        public Transform visualTransform;
        public Animator animator;

        [Header("边界（可选，留空则不限制）")]
        public float minX = -100f;
        public float maxX = 100f;
        public bool clampHorizontal = false;
        [Tooltip("允许点击场景地面后自动走向该位置。")]
        public bool clickToMoveEnabled = false;

        [Header("落地检测")]
        public float groundCheckDistance = 0.3f;

        [Header("防掉落")]
        [Tooltip("玩家掉到此 Y 以下时重置到出生点")]
        public float fallResetY = -10f;
        public Vector3 spawnPoint = new Vector3(0, -1f, 0);

        private Rigidbody2D _rb;
        private Collider2D _col;
        private float _originalVisualScaleX;
        private float _lastGroundedTime = -10f;
        private bool _controlEnabled = true;
        private float? _walkTargetX;

        /// <summary>攀爬/上下台阶期间由外部置为 true，避免动画切到 Jump。</summary>
        public bool IsClimbing { get; set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
            {
                // 兼容 Rigidbody2D 被加在子物体（如 Visual）上的情况：
                // 物理体必须和控制器在同一物体上，否则移动只会作用在子物体上。
                var childRb = GetComponentInChildren<Rigidbody2D>(true);
                if (childRb != null)
                {
                    Debug.LogWarning($"[LevelPlayerController] Rigidbody2D 挂在子物体 {childRb.name} 上，已自动转移到 {name}");
                    childRb.simulated = false;
                    Destroy(childRb);
                }
                _rb = gameObject.AddComponent<Rigidbody2D>();
            }

            // 碰撞盒可能挂在子物体（如 Visual）上
            _col = GetComponent<Collider2D>();
            if (_col == null) _col = GetComponentInChildren<Collider2D>(true);

            if (visualTransform == null)
            {
                var v = transform.Find("Visual");
                if (v != null) visualTransform = v;
            }
            if (animator == null) animator = GetComponent<Animator>();

            if (visualTransform != null)
                _originalVisualScaleX = Mathf.Abs(visualTransform.localScale.x);

            // 配置刚体为 Dynamic，参与物理碰撞
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.gravityScale = 3f;
                _rb.freezeRotation = true;
                _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void Update()
        {
            if (!_controlEnabled ||
                WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive)
            {
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
                DriveAnimation(0f, false, IsGrounded() || IsClimbing);
                return;
            }

            float h = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;

            if (clickToMoveEnabled && Input.GetMouseButtonDown(0) && !PointerIsOverUI())
            {
                Camera camera = Camera.main;
                if (camera != null)
                {
                    Vector3 screenPoint = Input.mousePosition;
                    Vector3 worldPoint = camera.ScreenToWorldPoint(new Vector3(
                        screenPoint.x, screenPoint.y, Mathf.Abs(camera.transform.position.z)));
                    SetWalkTarget(worldPoint.x);
                }
            }

            if (Mathf.Abs(h) > 0.01f)
            {
                _walkTargetX = null;
            }
            else if (clickToMoveEnabled && _walkTargetX.HasValue)
            {
                float distance = _walkTargetX.Value - transform.position.x;
                if (Mathf.Abs(distance) <= 0.12f) _walkTargetX = null;
                else h = Mathf.Sign(distance);
            }

            bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float speed = moveSpeed * (isRunning ? runMultiplier : 1f);

            // 小台阶自动迈步，避免被地面碰撞盒的竖直边缘卡住
            TryStepUp(h);

            // 水平速度
            Vector2 vel = _rb.velocity;
            vel.x = h * speed;
            _rb.velocity = vel;

            // 翻转朝向
            if (Mathf.Abs(h) > 0.01f && visualTransform != null)
            {
                Vector3 s = visualTransform.localScale;
                s.x = Mathf.Sign(h) * _originalVisualScaleX;
                visualTransform.localScale = s;
            }

            // 边界限制
            if (clampHorizontal)
            {
                Vector3 pos = transform.position;
                if (pos.x < minX) { pos.x = minX; _rb.position = pos; }
                if (pos.x > maxX) { pos.x = maxX; _rb.position = pos; }
            }

            // 跳跃
            bool grounded = IsGrounded();
            if (grounded) _lastGroundedTime = Time.time;

            // 落地检测做短暂缓冲，避免站在地面时射线偶发丢失导致动画在 Idle/Jump 间抖动
            bool stableGrounded = grounded || (Time.time - _lastGroundedTime <= 0.1f);

            if (Input.GetKeyDown(KeyCode.Space) && stableGrounded)
            {
                // 根据跳跃高度反推初速度：v = sqrt(2 * g * h)
                float g = Mathf.Abs(Physics2D.gravity.y) * _rb.gravityScale;
                float jumpForce = Mathf.Sqrt(2f * g * jumpHeight);
                _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
            }

            // 防掉落：掉出底部则回到出生点
            if (transform.position.y < fallResetY)
            {
                _rb.velocity = Vector2.zero;
                _rb.position = spawnPoint;
            }

            DriveAnimation(h, isRunning, stableGrounded || IsClimbing);
        }

        public void SetControlEnabled(bool enabled)
        {
            _controlEnabled = enabled;
            if (!enabled && _rb != null)
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
            if (!enabled) _walkTargetX = null;
        }

        public void SetWalkTarget(float worldX)
        {
            _walkTargetX = Mathf.Clamp(worldX, minX, maxX);
        }

        private static bool PointerIsOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// 检测前进方向是否有不超过 stepHeight 的小台阶，若有则把玩家抬上去。
        /// 场景里的地面由多个矩形碰撞盒拼成，边缘会形成看不见的小台阶；
        /// 这里统一处理，避免玩家在平地上被卡住。
        /// 采用「脚前方向下探测地面高度」而不是水平射线：
        /// 台阶顶面可能只比脚底高 1~2cm，水平射线很容易贴着顶面漏检。
        /// </summary>
        private void TryStepUp(float h)
        {
            if (_rb == null || _col == null || stepHeight <= 0f || Mathf.Abs(h) < 0.01f) return;
            if (!IsGrounded()) return;

            float footY = _col.bounds.min.y;
            float dir = Mathf.Sign(h);

            // 1) 先确认脚踝高度确实被前方障碍物挡住（可能是只有 1~2cm 高的隐形台阶边缘）
            Vector2 sideOrigin = new Vector2(_col.bounds.center.x, footY + 0.005f);
            float sideDistance = _col.bounds.extents.x + 0.12f;
            bool blocked = false;
            foreach (var hit in Physics2D.RaycastAll(sideOrigin, Vector2.right * dir, sideDistance))
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.collider == _col || hit.collider.transform.IsChildOf(transform)) continue;
                blocked = true;
                break;
            }
            if (!blocked) return;

            // 2) 探测障碍物顶面高度：脚前一点向下打射线，避免贴着薄台阶顶面漏检
            float probeX = _col.bounds.center.x + dir * (_col.bounds.extents.x + 0.28f);
            Vector2 origin = new Vector2(probeX, footY + stepHeight + 0.05f);
            float rayLength = stepHeight + 0.15f;
            var hits = Physics2D.RaycastAll(origin, Vector2.down, rayLength);

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.collider == _col || hit.collider.transform.IsChildOf(transform)) continue;

                float step = hit.point.y - footY;
                if (step <= 0.01f || step > stepHeight) continue;

                Vector2 pos = _rb.position;
                pos.y += step + 0.02f;
                _rb.position = pos;
                return;
            }
        }

        /// <summary>
        /// 检测玩家是否站在地面/平台上。
        /// 从碰撞体底部向下发射短射线，碰到非 Trigger 碰撞体即为落地。
        /// </summary>
        private bool IsGrounded()
        {
            if (_col == null) return false;

            float footY = _col.bounds.min.y;
            Vector2 rayOrigin = new Vector2(_col.bounds.center.x, footY - 0.05f);
            var hits = Physics2D.RaycastAll(rayOrigin, Vector2.down, groundCheckDistance);

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.isTrigger) continue;
                if (hit.collider.gameObject == gameObject) continue;
                return true;
            }
            return false;
        }

        private void DriveAnimation(float h, bool isRunning, bool grounded)
        {
            if (animator == null) return;

            bool isMoving = Mathf.Abs(h) > 0.01f;

            string targetState;
            if (!grounded) targetState = "Jump";
            else if (isMoving && isRunning) targetState = "Run";
            else if (isMoving) targetState = "Walk";
            else targetState = "Idle";

            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(targetState))
                animator.Play(targetState, 0, 0f);
        }
    }
}
