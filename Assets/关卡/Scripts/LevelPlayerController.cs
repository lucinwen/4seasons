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
        private readonly ContactPoint2D[] _groundContacts = new ContactPoint2D[16];
#if UNITY_EDITOR
        internal string StepUpDiagnostic { get; private set; }
#endif

        /// <summary>攀爬/上下台阶期间由外部置为 true，避免动画切到 Jump。</summary>
        public bool IsClimbing { get; set; }
        public bool IsOnPorch { get; set; }

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
                DriveAnimation(0f, false, IsGrounded() || IsClimbing || IsOnPorch);
                return;
            }

            float h = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (IsClimbing)
            {
                h = 0f;
                _walkTargetX = null;
            }

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
            if (!IsOnPorch) TryStepUp(h);

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

            if (!IsClimbing && !IsOnPorch && Input.GetKeyDown(KeyCode.Space) && stableGrounded)
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

            DriveAnimation(h, isRunning, stableGrounded || IsClimbing || IsOnPorch);
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
        /// 用脚尖检测带寻找实际踏面，再从踏面边缘向下检测顶面。
        /// 薄台阶之间的间隙不会漏检，抬升前也会检查头部净空。
        /// </summary>
        private void TryStepUp(float h)
        {
            if (_rb == null || _col == null || stepHeight <= 0f || Mathf.Abs(h) < 0.01f) return;
            if (!IsGrounded()) { TraceStepUp("not grounded"); return; }

            float footY = _col.bounds.min.y;
            float dir = Mathf.Sign(h);

            // A single ankle ray can pass under a thin bridge tread: the authored
            // slabs have small vertical gaps. Search the full step band at the toe.
            // Box2D can also hold the foot just above a tread's corner by its
            // contact skin. Include that small band below the geometric sole.
            float skin = Mathf.Max(0.04f, Physics2D.defaultContactOffset * 2f);
            float frontX = dir > 0 ? _col.bounds.max.x : _col.bounds.min.x;
            Vector2 toeCenter = new Vector2(frontX + dir * 0.045f, footY + (stepHeight - skin) * 0.5f);
            var obstacles = Physics2D.OverlapBoxAll(toeCenter, new Vector2(0.18f, stepHeight + skin), 0f);
            TraceStepUp("no higher tread in toe band; front=" + frontX + " foot=" + footY);
            foreach (var obstacle in obstacles)
            {
                if (obstacle == null || obstacle.isTrigger || IsBodyCollider(obstacle)) continue;
                float topRise = obstacle.bounds.max.y - footY;
                if (topRise < -skin || topRise > stepHeight) continue;

                float edgeX = dir > 0 ? obstacle.bounds.min.x : obstacle.bounds.max.x;
                float edgeGap = (edgeX - frontX) * dir;
                // Only lift at an approaching vertical edge, never repeatedly
                // over the interior of a floor which is already supporting us.
                if (edgeGap < -skin || edgeGap > 0.14f) continue;
                if (topRise <= 0.01f && edgeGap > skin) continue;
                float probeX = edgeX + dir * Mathf.Min(0.035f, obstacle.bounds.size.x * 0.25f);
                var origin = new Vector2(probeX, footY + stepHeight + 0.05f);
                TraceStepUp("no top hit for " + obstacle.name + " rise=" + topRise + " probe=" + origin);
                foreach (var top in Physics2D.RaycastAll(origin, Vector2.down, stepHeight + 0.15f))
                {
                    if (top.collider != obstacle || top.fraction <= 0f || top.normal.y < 0.45f) continue;
                    float step = top.point.y - footY;
                    if (step < -skin || step > stepHeight) continue;

                    float lift = Mathf.Max(0.005f, step + skin);
                    Vector2 raisedCenter = (Vector2)_col.bounds.center + Vector2.up * lift;
                    Vector2 clearance = (Vector2)_col.bounds.size - Vector2.one * 0.04f;
                    bool clear = true;
                    foreach (var other in Physics2D.OverlapBoxAll(raisedCenter, clearance, 0f))
                        if (other != null && !other.isTrigger && !IsBodyCollider(other))
                        {
                            TraceStepUp("clearance blocked by " + other.name + " bounds=" + other.bounds +
                                " raising for=" + obstacle.name + " step=" + step);
                            clear = false;
                            break;
                        }
                    if (!clear) continue;

                    TraceStepUp("stepped " + obstacle.name + " rise=" + step);
                    _rb.position += Vector2.up * lift;
                    return;
                }
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void TraceStepUp(string detail)
        {
#if UNITY_EDITOR
            StepUpDiagnostic = detail;
#endif
        }

        private bool IsBodyCollider(Collider2D collider) => collider == _col ||
            collider.attachedRigidbody == _rb || collider.transform.IsChildOf(transform);

        /// <summary>
        /// 检测玩家是否站在地面/平台上。
        /// 先读取脚底真实接触，再检测双脚；宽碰撞盒登桥时支撑点常在前脚。
        /// </summary>
        private bool IsGrounded()
        {
            if (_col == null) return false;

            float footY = _col.bounds.min.y;
            if (_rb != null)
            {
                int count = _rb.GetContacts(_groundContacts);
                for (int i = 0; i < count; i++)
                    if (_groundContacts[i].normal.y >= 0.45f && _groundContacts[i].point.y <= footY + 0.1f)
                        return true;
            }

            Bounds bounds = _col.bounds;
            float inset = Mathf.Min(0.04f, bounds.extents.x * 0.25f);
            return HasGroundAt(bounds.min.x + inset, footY) || HasGroundAt(bounds.center.x, footY) ||
                HasGroundAt(bounds.max.x - inset, footY);
        }

        private bool HasGroundAt(float x, float footY)
        {
            // Start above the sole so a thin bridge tread is hit from outside.
            var origin = new Vector2(x, footY + 0.025f);
            foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, groundCheckDistance + 0.025f))
                if (hit.collider != null && !hit.collider.isTrigger && !IsBodyCollider(hit.collider) &&
                    hit.fraction > 0f && hit.normal.y >= 0.45f)
                    return true;
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
