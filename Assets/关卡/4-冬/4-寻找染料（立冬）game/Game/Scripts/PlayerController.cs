using UnityEngine;

namespace JiangnanDye
{
    /// <summary>人物四方向移动控制（WASD / 方向键），带斜向速度归一化。</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动参数")]
        public float moveSpeed = 3.2f;
        public float runSpeed = 5.0f;
        public bool runHeld;   // 当前是否按住 Shift（供外部只读）

        [Header("地图边界（世界坐标）")]
        [Tooltip("可走范围矩形：min=(左下), max=(右上)，在 Scene 视图里用蓝色半透明框可视化")]
        public Vector2 worldMin = new Vector2(-12.55f, -6.95f);
        public Vector2 worldMax = new Vector2(12.55f, 4.85f);

        [Header("表现引用")]
        public Transform visual;
        public SpriteRenderer spriteRenderer;
        public Animator animator;

        [Header("状态")]
        public bool controlEnabled = true;

        private Rigidbody2D body;
        private Vector2 moveInput;
        private Vector3 visualBasePos;
        private float bobTime;
        private bool facingLeft;

        public bool ControlEnabled
        {
            get { return controlEnabled; }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (visual == null && spriteRenderer != null) visual = spriteRenderer.transform;
            if (visual != null) visualBasePos = visual.localPosition;
        }

        private void Update()
        {
            float h = 0f;
            float v = 0f;
            if (controlEnabled && !WorkshopMinigameMenu.IsPaused)
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;

                // Shift 触发跑步
                runHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            else
            {
                runHeld = false;
            }

            Vector2 raw = new Vector2(h, v);
            moveInput = raw.sqrMagnitude > 1f ? raw.normalized : raw;   // 斜向速度归一化

            UpdateVisual();
        }

        private void FixedUpdate()
        {
            if (!controlEnabled || moveInput.sqrMagnitude < 0.0001f) return;

            float speed = runHeld ? runSpeed : moveSpeed;
            Vector2 next = body.position + moveInput * speed * Time.fixedDeltaTime;
            next.x = Mathf.Clamp(next.x, worldMin.x, worldMax.x);
            next.y = Mathf.Clamp(next.y, worldMin.y, worldMax.y);
            body.MovePosition(next);
        }

        private void UpdateVisual()
        {
            bool moving = moveInput.sqrMagnitude > 0.01f;
            bool running = moving && runHeld;

            if (animator != null)
            {
                animator.SetBool("IsMoving", moving);
                animator.SetBool("IsRunning", running);
            }

            // 只根据左右方向翻转精灵：右走保持原样，左走翻转
            if (Mathf.Abs(moveInput.x) > 0.1f) facingLeft = moveInput.x < 0f;
            if (spriteRenderer != null) spriteRenderer.flipX = facingLeft;

            if (visual != null)
            {
                if (moving)
                {
                    bobTime += Time.deltaTime * 11f;
                    visual.localPosition = visualBasePos + new Vector3(0f, Mathf.Abs(Mathf.Sin(bobTime)) * 0.06f, 0f);
                }
                else
                {
                    bobTime = 0f;
                    visual.localPosition = Vector3.Lerp(visual.localPosition, visualBasePos, Time.deltaTime * 12f);
                }
            }
        }

        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled;
            moveInput = Vector2.zero;
            if (body != null) body.velocity = Vector2.zero;
            if (!enabled && visual != null) visual.localPosition = visualBasePos;
        }

        // --------------------------------------------------------------
        // Scene 视图可视化
        // --------------------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            DrawWalkBoundsGizmo(/*thick*/ true);
        }

        private void OnDrawGizmos()
        {
            DrawWalkBoundsGizmo(/*thick*/ false);
        }

        private void DrawWalkBoundsGizmo(bool thick)
        {
            Vector2 size = worldMax - worldMin;
            Gizmos.matrix = Matrix4x4.Translate(new Vector3((worldMin.x + worldMax.x) * 0.5f,
                                                            (worldMin.y + worldMax.y) * 0.5f, 0f));
            // 半透明蓝色填充
            Gizmos.color = new Color(0.3f, 0.7f, 1f, thick ? 0.18f : 0.09f);
            Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
            // 边框
            Gizmos.color = new Color(0.2f, 0.55f, 0.95f, thick ? 0.9f : 0.55f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
            // 角落标记
            Gizmos.color = new Color(0.2f, 0.55f, 0.95f, 0.95f);
            float mark = 0.15f;
            Vector2[] corners = { worldMin, worldMax, new Vector2(worldMin.x, worldMax.y), new Vector2(worldMax.x, worldMin.y) };
            foreach (var c in corners)
            {
                Gizmos.DrawSphere(new Vector3(c.x, c.y, 0f), mark);
            }
        }
    }
}
