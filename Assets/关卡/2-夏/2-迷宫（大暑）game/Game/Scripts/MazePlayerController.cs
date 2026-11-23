using UnityEngine;

namespace JiangnanDye.MazeGame
{
    /// <summary>
    /// 迷宫人物四方向移动控制。
    /// 使用 Rigidbody2D + MovePosition，由物理碰撞阻挡穿墙。
    /// Animator 参数：IsMoving（bool），控制 Idle ↔ Walk 切换。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MazePlayerController : MonoBehaviour
    {
        [Header("移动参数")]
        public float moveSpeed = 3.5f;

        [Header("地图边界（世界坐标，可选）")]
        [Tooltip("限制人物活动范围；留零则不限制，由迷宫碰撞体负责阻挡")]
        public Vector2 worldMin;
        public Vector2 worldMax;

        [Header("动画引用")]
        [Tooltip("人物 Visual 子物体的 SpriteRenderer，用于 flipX 朝向和动画")]
        public SpriteRenderer spriteRenderer;
        public Animator animator;

        [Header("状态")]
        public bool controlEnabled = true;

        private Rigidbody2D body;
        private Vector2 moveInput;
        private bool hasBounds;
        private bool facingLeft;

        public bool ControlEnabled
        {
            get => controlEnabled;
            set => controlEnabled = value;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hasBounds = worldMin != Vector2.zero || worldMax != Vector2.zero;

            // Animator 在 Player 根节点（与 4-寻找染料 结构一致）
            if (animator == null) animator = GetComponent<Animator>();

            // SpriteRenderer 在 Visual 子物体上
            if (spriteRenderer == null)
            {
                var visual = transform.Find("Visual");
                if (visual != null) spriteRenderer = visual.GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (!controlEnabled || WorkshopMinigameMenu.IsPaused)
            {
                moveInput = Vector2.zero;
                UpdateAnimator(false);
                return;
            }

            float h = 0f;
            float v = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;

            Vector2 raw = new Vector2(h, v);
            moveInput = raw.sqrMagnitude > 1f ? raw.normalized : raw;

            // 朝向翻转
            if (Mathf.Abs(moveInput.x) > 0.1f) facingLeft = moveInput.x < 0f;
            if (spriteRenderer != null) spriteRenderer.flipX = facingLeft;

            UpdateAnimator(moveInput.sqrMagnitude > 0.01f);
        }

        private void FixedUpdate()
        {
            if (!controlEnabled || moveInput.sqrMagnitude < 0.0001f) return;

            Vector2 next = body.position + moveInput * moveSpeed * Time.fixedDeltaTime;
            if (hasBounds)
            {
                next.x = Mathf.Clamp(next.x, worldMin.x, worldMax.x);
                next.y = Mathf.Clamp(next.y, worldMin.y, worldMax.y);
            }
            body.MovePosition(next);
        }

        private void UpdateAnimator(bool moving)
        {
            if (animator != null)
                animator.SetBool("IsMoving", moving);
        }

        public void Stop()
        {
            controlEnabled = false;
            moveInput = Vector2.zero;
            if (body != null) body.velocity = Vector2.zero;
            UpdateAnimator(false);
        }
    }
}
