using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JiangnanDye.LoomRepair
{
    /// <summary>
    /// 校准面板控制器。
    /// 指针绕底部圆心在半圆范围内左右摆动。通过角度判定是否命中。
    /// 判定角度采用 0-180° 系统：0° = 正左，90° = 正上，180° = 正右。
    /// </summary>
    public class CalibrationPanelController : MonoBehaviour, IPointerClickHandler
    {
        public enum SwingDirection { LeftToRight, RightToLeft }

        [Header("面板组件")]
        [Tooltip("修复面板底图 Image")]
        public Image panelImage;

        [Tooltip("黄色打击区域 Image（用于显示，判定用角度配置）")]
        public Image hitZoneImage;

        [Tooltip("指针 RectTransform — pivot 必须是 (0.5, 0)，底部对齐面板中心")]
        public RectTransform pointerRect;

        [Header("摆动参数")]
        [Tooltip("指针摆动速度（度/秒）")]
        [Range(30f, 360f)] public float swingSpeed = 180f;

        [Tooltip("摆动范围：左边界角度（0-180）")]
        [Range(0f, 90f)] public float minAngle = 15f;

        [Tooltip("摆动范围：右边界角度（0-180），必须大于 minAngle")]
        [Range(90f, 180f)] public float maxAngle = 165f;

        [Header("判定角度配置（0-180°）")]
        [Tooltip("黄色成功判定区的中心角度")]
        [Range(0f, 180f)] public float hitZoneCenterAngle = 95f;

        [Tooltip("黄色成功判定区的半宽（角度），成功区 = center ± halfWidth")]
        [Range(2f, 30f)] public float hitZoneHalfWidth = 18f;

        [Tooltip("完美区占成功区的比例（在成功区中心）")]
        [Range(0.1f, 0.5f)] public float perfectRatio = 0.25f;

        [Header("弹出/隐藏动画")]
        [Tooltip("面板弹出时的缩放倍数")]
        [Range(1.0f, 1.5f)] public float popScale = 1.15f;

        [Tooltip("弹出动画时长（秒）")]
        [Range(0.1f, 0.5f)] public float popDuration = 0.25f;

        [Header("内部状态")]
        private float currentAngle;              // 当前指针角度 0-180
        private SwingDirection swingDir;         // 当前摆动方向
        private bool isSwinging;                 // 是否在摆动中
        private bool waitingForInput;            // 是否等待玩家点击
        private float countdownTimer;            // 倒计时
        private float timeoutDuration;           // 超时秒数
        private bool hasStopped;                 // 是否已经停止过

        // 角度限制（摆动范围）
        private float actualMinAngle;
        private float actualMaxAngle;

        /// <summary>校准完成回调（结果类型）</summary>
        public Action<LoomRepairGameManager.CalibrationResult> OnCalibrationDone;

        /// <summary>是否正在等待玩家输入</summary>
        public bool IsWaitingForInput => waitingForInput && isSwinging;

        // ===== 初始化 =====

        private void Awake()
        {
            // 根据 min/max 计算实际摆动边界
            actualMinAngle = Mathf.Min(minAngle, maxAngle);
            actualMaxAngle = Mathf.Max(minAngle, maxAngle);
        }

        /// <summary>外部设置超时秒数</summary>
        public void SetTimeout(float seconds)
        {
            timeoutDuration = seconds;
        }

        /// <summary>显示面板（带动画）</summary>
        public void ShowPanel()
        {
            gameObject.SetActive(true);
            // 先设置初始状态为 scale=0，然后弹出来
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one * 0.3f;
                StopAllCoroutines();
                StartCoroutine(PopInAnim(rt));
            }

            // 重置指针角度到起始位置
            currentAngle = actualMinAngle;
            ApplyPointerRotation();
            hasStopped = false;
        }

        /// <summary>隐藏面板</summary>
        public void HidePanel()
        {
            isSwinging = false;
            waitingForInput = false;
            StopAllCoroutines();
            gameObject.SetActive(false);
        }

        private IEnumerator PopInAnim(RectTransform rt)
        {
            float elapsed = 0f;
            float from = 0.3f;
            float to = popScale;
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                // 使用 ease-out 曲线
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float s = Mathf.Lerp(from, to, eased);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        /// <summary>开始指针摆动</summary>
        public void StartSwinging()
        {
            Awake(); // 确保边界计算正确
            swingDir = SwingDirection.LeftToRight;
            currentAngle = actualMinAngle;
            isSwinging = true;
            waitingForInput = true;
            countdownTimer = timeoutDuration;
            hasStopped = false;
            ApplyPointerRotation();
        }

        /// <summary>停止摆动（外部调用：校准完成或隐藏面板）</summary>
        public void StopSwinging()
        {
            isSwinging = false;
            waitingForInput = false;
            StopAllCoroutines();
        }

        /// <summary>外部调用：点击鼠标左键 → 在当前角度停止并判定</summary>
        public void StopAtCurrentAngle()
        {
            if (!isSwinging || !waitingForInput || hasStopped || WorkshopMinigameMenu.IsPaused) return;
            hasStopped = true;
            isSwinging = false;
            waitingForInput = false;

            var result = JudgeAngle();
            Debug.Log($"[Calibration] 停止于角度 {currentAngle:F1}°, 判定结果: {result}");

            // 延迟一小会儿让玩家看到指针停住
            StartCoroutine(DelayedCall(0.15f, () =>
            {
                OnCalibrationDone?.Invoke(result);
            }));
        }

        private System.Collections.IEnumerator DelayedCall(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            while (WorkshopMinigameMenu.IsPaused) yield return null;
            callback?.Invoke();
        }

        // ===== 每帧更新 =====

        private void Update()
        {
            if (!isSwinging || WorkshopMinigameMenu.IsPaused) return;

            // 1. 指针摆动
            if (swingDir == SwingDirection.LeftToRight)
            {
                currentAngle += swingSpeed * Time.deltaTime;
                if (currentAngle >= actualMaxAngle)
                {
                    currentAngle = actualMaxAngle;
                    swingDir = SwingDirection.RightToLeft;
                }
            }
            else
            {
                currentAngle -= swingSpeed * Time.deltaTime;
                if (currentAngle <= actualMinAngle)
                {
                    currentAngle = actualMinAngle;
                    swingDir = SwingDirection.LeftToRight;
                }
            }
            ApplyPointerRotation();

            // 2. 超时检查
            countdownTimer -= Time.deltaTime;
            if (countdownTimer <= 0f && waitingForInput)
            {
                Debug.Log("[Calibration] 超时 → 自动失败");
                hasStopped = true;
                isSwinging = false;
                waitingForInput = false;
                OnCalibrationDone?.Invoke(LoomRepairGameManager.CalibrationResult.Fail);
            }
        }

        private void ApplyPointerRotation()
        {
            if (pointerRect == null) return;
            // 角度系统：0=正左, 90=正上, 180=正右
            // Unity Z 轴旋转：0=正上, 90=正右, -90=正左
            // 转换: unityZ = angle - 90
            float unityZ = currentAngle - 90f;
            pointerRect.localEulerAngles = new Vector3(0, 0, unityZ);
        }

        // ===== 判定逻辑 =====

        /// <summary>根据当前指针角度判定结果</summary>
        private LoomRepairGameManager.CalibrationResult JudgeAngle()
        {
            float angle = currentAngle;
            float hitMin = hitZoneCenterAngle - hitZoneHalfWidth;
            float hitMax = hitZoneCenterAngle + hitZoneHalfWidth;
            float perfectHalfWidth = hitZoneHalfWidth * perfectRatio;
            float perfectMin = hitZoneCenterAngle - perfectHalfWidth;
            float perfectMax = hitZoneCenterAngle + perfectHalfWidth;

            // 先检查完美区
            if (angle >= perfectMin && angle <= perfectMax)
            {
                return LoomRepairGameManager.CalibrationResult.Perfect;
            }
            // 再检查成功区
            if (angle >= hitMin && angle <= hitMax)
            {
                return LoomRepairGameManager.CalibrationResult.Success;
            }
            // 失败
            return LoomRepairGameManager.CalibrationResult.Fail;
        }

        // ===== IPointerClickHandler =====
        // 点击面板时停止指针（也可以由外部 UI Button 触发 StopAtCurrentAngle）

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isSwinging && waitingForInput)
            {
                StopAtCurrentAngle();
            }
        }

        // ===== 调试：在 Scene 视图绘制判定区 =====

        private void OnValidate()
        {
            actualMinAngle = Mathf.Min(minAngle, maxAngle);
            actualMaxAngle = Mathf.Max(minAngle, maxAngle);
        }
    }
}
