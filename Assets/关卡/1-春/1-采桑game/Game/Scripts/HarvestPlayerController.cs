using System.Collections;
using UnityEngine;

namespace Qingming.Harvest
{
    /// <summary>
    /// 玩家水平移动控制器：A/D 或方向键，带边界限制、朝向翻转、跳跃和跑步。
    /// 跳跃用协程抛物线实现，彻底绕开手动物理计算。
    /// </summary>
    public class HarvestPlayerController : MonoBehaviour
    {
        [Header("移动")]
        [Tooltip("水平移动速度（单位/秒）")]
        public float moveSpeed = 5f;
        [Tooltip("跑步速度倍率（按住 Shift）")]
        public float runMultiplier = 1.6f;

        [Header("跳跃")]
        [Tooltip("跳跃最高点距离地面的高度")]
        public float jumpHeight = 3.5f;
        [Tooltip("跳跃从起跳到落地的总时间（秒）")]
        public float jumpDuration = 0.55f;

        [Header("边界（世界坐标）")]
        [Tooltip("会从 Path SpriteRenderer bounds 自动算出")]
        public float minX;
        public float maxX;

        [Header("初始位置")]
        public Vector3 spawnPosition = new Vector3(0f, -2.5f, 0f);

        [Header("子物体引用")]
        public Transform visualTransform;
        public Animator animator;

        [Header("朝向参数")]
        public float facingScale = 1f;

        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int WalkState = Animator.StringToHash("Base Layer.Walk");
        private static readonly int RunState = Animator.StringToHash("Base Layer.Run");
        private static readonly int JumpState = Animator.StringToHash("Base Layer.Jump");

        private bool _controlEnabled = true;
        private float _originalLocalScaleX;
        private bool _isGrounded = true;
        private float _groundY;
        private float _jumpProgress;
        private bool _boundsAutoSet;
        private Coroutine _jumpCoroutine;

        private void Awake()
        {
            transform.position = spawnPosition;
            _groundY = spawnPosition.y;
        }

        /// <summary>
        /// 覆盖层模式下初始化：AddComponent 时 Awake 已用默认值执行过，
        /// 这里重新设置出生点与左右边界（世界坐标）。
        /// </summary>
        public void Setup(Vector3 worldSpawn, float worldMinX, float worldMaxX)
        {
            spawnPosition = worldSpawn;
            _groundY = worldSpawn.y;
            transform.position = worldSpawn;
            minX = worldMinX;
            maxX = worldMaxX;
            _boundsAutoSet = true; // 使用显式边界，跳过按 Path 自动计算
        }

        private void Update()
        {
            if (WorkshopMinigameMenu.IsPaused) return;

            // 覆盖层模式下 visualTransform 在 AddComponent 后才赋值，
            // 这里用懒加载缓存原始缩放，避免 Awake 时拿到错误的 transform。
            if (_originalLocalScaleX == 0f && visualTransform != null)
            {
                _originalLocalScaleX = visualTransform.localScale.x;
            }

            // === 第一帧自动设置 Path 边界 ===
            if (!_boundsAutoSet)
            {
                var pathGo = GameObject.Find("Path");
                if (pathGo != null)
                {
                    var sr = pathGo.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        minX = sr.bounds.min.x + 0.3f;
                        maxX = sr.bounds.max.x - 0.3f;
                        _boundsAutoSet = true;
                    }
                }
            }

            Vector3 pos = transform.position;
            float h = 0f;
            bool isRunning = false;

            // === 边界 clamp ===
            pos.x = Mathf.Clamp(pos.x, minX, maxX);

            if (_controlEnabled)
            {
                // === 水平移动 ===
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;

                isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                float speed = moveSpeed * (isRunning ? runMultiplier : 1f);

                if (Mathf.Abs(h) > 0.01f)
                {
                    pos.x += h * speed * Time.deltaTime;
                    pos.x = Mathf.Clamp(pos.x, minX, maxX);

                    if (h > 0f) facingScale = Mathf.Abs(facingScale);
                    else if (h < 0f) facingScale = -Mathf.Abs(facingScale);

                    if (visualTransform != null)
                    {
                        Vector3 s = visualTransform.localScale;
                        s.x = Mathf.Sign(facingScale) * Mathf.Abs(_originalLocalScaleX);
                        visualTransform.localScale = s;
                    }
                }

                // === 跳跃检测 ===
                if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
                {
                    _isGrounded = false;
                    _jumpProgress = 0f;
                    // 把当前实际 Y 传给协程作为起跳点
                    _jumpCoroutine = StartCoroutine(DoJump(transform.position.y));
                }
            }

            // 注意：跳跃协程里会每帧改 pos.y，这里只处理水平位移
            pos.y = transform.position.y; // 保留协程设的 y

            transform.position = pos;

            // === 动画驱动 ===
            DriveAnimation(h, isRunning);
        }

        /// <summary>
        /// 跳跃协程：用 sin(π*t) 抛物线，0 时刻在地面，0.5 时刻到最高点，1 时刻落地。
        /// 接收起跳瞬间的实际 Y 坐标，确保原地起跳原地落下。
        /// </summary>
        private IEnumerator DoJump(float startY)
        {
            float elapsed = 0f;

            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                _jumpProgress = Mathf.Clamp01(elapsed / jumpDuration);
                float height = Mathf.Sin(_jumpProgress * Mathf.PI) * jumpHeight;

                Vector3 p = transform.position;
                p.y = startY + height;
                transform.position = p;

                yield return null;
            }

            // 精确落回起跳位置的 Y
            Vector3 land = transform.position;
            land.y = startY;
            transform.position = land;

            _isGrounded = true;
            _jumpCoroutine = null;
        }

        /// <summary>
        /// 根据移动和跳跃选择动画。采桑控制器由脚本切换状态。
        /// </summary>
        private void DriveAnimation(float h, bool isRunning)
        {
            if (animator == null) return;

            if (!_isGrounded)
            {
                // Play the full clip across the jump, including the falling frames.
                animator.Play(JumpState, 0, _jumpProgress);
                return;
            }

            int targetState = Mathf.Abs(h) <= 0.01f ? IdleState : isRunning ? RunState : WalkState;

            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash != targetState)
            {
                animator.Play(targetState, 0, 0f);
            }
        }

        /// <summary>启用/禁用玩家控制。</summary>
        public void SetControlEnabled(bool enabled)
        {
            _controlEnabled = enabled;
            if (!enabled)
            {
                // 强制落地 + 停止跳跃协程
                if (_jumpCoroutine != null)
                {
                    StopCoroutine(_jumpCoroutine);
                    _jumpCoroutine = null;
                }
                _isGrounded = true;
                Vector3 p = transform.position;
                p.y = _groundY;
                transform.position = p;

                DriveAnimation(0f, false);
            }
        }

        /// <summary>重置到初始出生点。</summary>
        public void ResetPosition()
        {
            if (_jumpCoroutine != null)
            {
                StopCoroutine(_jumpCoroutine);
                _jumpCoroutine = null;
            }
            transform.position = spawnPosition;
            facingScale = 1f;
            _isGrounded = true;
            if (visualTransform != null && _originalLocalScaleX != 0f)
            {
                Vector3 s = visualTransform.localScale;
                s.x = Mathf.Abs(_originalLocalScaleX);
                visualTransform.localScale = s;
            }
            DriveAnimation(0f, false);
        }

        /// <summary>供篮子等子物体查询当前朝向（1 右 / -1 左）。</summary>
        public int GetFacingSign()
        {
            return facingScale >= 0f ? 1 : -1;
        }
    }
}
