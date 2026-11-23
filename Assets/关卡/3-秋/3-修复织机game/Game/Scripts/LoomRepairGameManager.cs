using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Qingming.LevelBackground;

namespace JiangnanDye.LoomRepair
{
    /// <summary>
    /// 修复织机小游戏主管理器。
    /// 总进度自动增长 → 随机触发校准 → 成功/完美加进度、失败扣进度 → 100% 完成。
    /// </summary>
    public class LoomRepairGameManager : MonoBehaviour
    {
        public enum CalibrationResult { None, Success, Perfect, Fail }

        [Header("核心参数（均可在 Inspector 调整）")]
        [Tooltip("自动织造速度：每秒增加的百分比")]
        [Range(0.5f, 10f)] public float autoProgressPerSecond = 2f;

        [Tooltip("完美校准额外奖励百分比")]
        [Range(0f, 15f)] public float perfectBonus = 5f;

        [Tooltip("失败扣除百分比")]
        [Range(0f, 15f)] public float failPenalty = 5f;

        [Tooltip("校准最短触发间隔（秒）")]
        [Range(3f, 15f)] public float calibrationMinInterval = 8f;

        [Tooltip("校准最长触发间隔（秒）")]
        [Range(5f, 20f)] public float calibrationMaxInterval = 12f;

        [Tooltip("校准超时时间（秒），超过此时间自动判失败")]
        [Range(2f, 10f)] public float calibrationTimeout = 5f;

        [Tooltip("失败后暂停织造的时间（秒）")]
        [Range(0f, 3f)] public float failPauseTime = 1f;

        [Header("组件引用")]
        public CalibrationPanelController calibrationPanel;
        public LoomRepairUI ui;

        [Header("事件")]
        public UnityEvent OnGameCompleted;
        public UnityEvent OnReset;
        public UnityEvent OnClose;

        // ===== 运行时状态 =====
        private float totalProgress;           // 0-100
        private bool isPlaying;                // 游戏是否进行中
        private bool isCalibrating;            // 是否正在校准
        private bool gameCompleted;            // 是否已完成
        private float nextCalibrationCountdown;
        private int successfulCalibrations;    // 已成功校准次数（3 次完成）

        private void Awake()
        {
            // 改为固定 3 次成功校准完成，不再依赖自动织造进度
            autoProgressPerSecond = 0f;
            perfectBonus = 0f;
            failPenalty = 0f;
            calibrationMinInterval = 0.7f;
            calibrationMaxInterval = 1f;

            // 初始形态为最破损的织机-1
            if (ui != null) ui.SetLoomStage(0);
        }

        public float TotalProgress => totalProgress;
        public bool IsPlaying => isPlaying;
        public bool IsCalibrating => isCalibrating;
        public bool IsCompleted => gameCompleted;

        private void Update()
        {
            if (!isPlaying || gameCompleted || WorkshopMinigameMenu.IsPaused) return;

            if (isCalibrating)
            {
                // 校准中：总进度暂停，检查超时
                // 超时由 CalibrationPanelController 内部处理
            }
            else
            {
                // 正常织造：推进总进度
                totalProgress += autoProgressPerSecond * Time.deltaTime;
                if (totalProgress >= 100f)
                {
                    totalProgress = 100f;
                    CompleteGame();
                    return;
                }

                // 检查是否触发校准
                nextCalibrationCountdown -= Time.deltaTime;
                if (nextCalibrationCountdown <= 0f)
                {
                    StartCalibration();
                }
            }

            // 更新 UI
            if (ui != null) ui.UpdateProgress(totalProgress);
        }

        /// <summary>开始游戏（点击开始按钮或按空格）</summary>
        public void StartGame()
        {
            if (WorkshopMinigameMenu.IsPaused) return;
            if (gameCompleted) ResetGame();
            isPlaying = true;
            if (ui != null)
            {
                ui.HideStartHint();
                ui.UpdateProgress(totalProgress);
            }
            StartCalibration();
            Debug.Log("[LoomRepairGameManager] 游戏开始");
        }

        /// <summary>重置游戏</summary>
        public void ResetGame()
        {
            StopAllCoroutines();
            totalProgress = 0f;
            isPlaying = false;
            isCalibrating = false;
            gameCompleted = false;
            successfulCalibrations = 0;
            if (calibrationPanel != null) calibrationPanel.HidePanel();
            if (ui != null)
            {
                ui.ShowStartHint();
                ui.UpdateProgress(totalProgress);
                ui.HideCompletePanel();
                ui.HideResultText();
                ui.SetLoomStage(0);
            }
            OnReset?.Invoke();
            Debug.Log("[LoomRepairGameManager] 游戏已重置");
        }

        /// <summary>关闭游戏</summary>
        public void CloseGame()
        {
            StopAllCoroutines();
            isPlaying = false;
            gameCompleted = true;
            OnClose?.Invoke();

            // 以叠加层方式打开时，关闭后直接卸载小游戏场景，返回下方秋天场景
            if (MiniGameOverlay.IsActive)
            {
                MiniGameOverlay.Close();
                return;
            }

            // 兼容旧流程：从秋天关卡整场景进入时，关闭后返回关卡场景
            if (AutumnLevelState.ReturningFromLoom)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level3_Autumn");
            }
        }

        // ===== 校准逻辑 =====

        private void ScheduleNextCalibration()
        {
            nextCalibrationCountdown = Random.Range(calibrationMinInterval, calibrationMaxInterval);
        }

        private void StartCalibration()
        {
            if (isCalibrating || gameCompleted) return;
            isCalibrating = true;
            if (calibrationPanel != null)
            {
                calibrationPanel.ShowPanel();
                calibrationPanel.SetTimeout(calibrationTimeout);
                calibrationPanel.OnCalibrationDone += HandleCalibrationResult;
                calibrationPanel.StartSwinging();
            }
            Debug.Log("[LoomRepairGameManager] 校准开始");
        }

        private void HandleCalibrationResult(CalibrationResult result)
        {
            isCalibrating = false;
            if (calibrationPanel != null)
            {
                calibrationPanel.StopSwinging();
                calibrationPanel.HidePanel();
                calibrationPanel.OnCalibrationDone -= HandleCalibrationResult;
            }

            switch (result)
            {
                case CalibrationResult.Perfect:
                    AdvanceProgress();
                    if (totalProgress < 100f) WorkshopAudioFeedback.Confirm();
                    ShowResultAndResume("完美校准！", new Color(1f, 0.85f, 0.2f));
                    break;
                case CalibrationResult.Success:
                    AdvanceProgress();
                    if (totalProgress < 100f) WorkshopAudioFeedback.Progress();
                    ShowResultAndResume("校准成功", new Color(0.4f, 0.85f, 0.4f));
                    break;
                case CalibrationResult.Fail:
                    // 失败后先暂停，避免 Update 立即重新弹出校准面板
                    nextCalibrationCountdown = failPauseTime;
                    StartCoroutine(FailPauseThenResume());
                    break;
            }

            if (ui != null) ui.UpdateProgress(totalProgress);

            // 检查完成
            if (totalProgress >= 100f)
            {
                CompleteGame();
                return;
            }

            // 安排下一次校准
            if (result != CalibrationResult.Fail)
            {
                ScheduleNextCalibration();
            }
        }

        private void AdvanceProgress()
        {
            successfulCalibrations++;
            // 三次成功校准：33% → 66% → 100%
            totalProgress = successfulCalibrations >= 3 ? 100f : 33f * successfulCalibrations;

            // 每完成一次校准，织机形态向前推进一档：织机-1 → 织机-2 → 织机-3
            if (ui != null) ui.SetLoomStage(successfulCalibrations);
        }

        private void ShowResultAndResume(string text, Color color)
        {
            if (ui != null) ui.ShowResultText(text, color);
            // 恢复织造（无需暂停）
            ScheduleNextCalibration();
        }

        private IEnumerator FailPauseThenResume()
        {
            if (ui != null) ui.ShowResultText("校准失败", new Color(0.9f, 0.3f, 0.25f));
            yield return new WaitForSeconds(failPauseTime);
            while (WorkshopMinigameMenu.IsPaused) yield return null;
            if (ui != null) ui.HideResultText();
            ScheduleNextCalibration();
        }

        private void CompleteGame()
        {
            if (gameCompleted) return;
            gameCompleted = true;
            isPlaying = false;
            isCalibrating = false;
            totalProgress = 100f;
            AutumnLevelState.HasLoomRepaired = true;
            WorkshopProgress.MarkStep("autumn.loom");

            if (calibrationPanel != null)
            {
                calibrationPanel.StopSwinging();
                calibrationPanel.HidePanel();
            }
            if (ui != null)
            {
                ui.UpdateProgress(100f);
                ui.HideCompletePanel();
            }

            // 动画结束后再显示完成弹窗；点击“继续”返回秋天场景
            MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.LoomRepair, () =>
            {
                SimplePopup.Show("修复完成！", "织机已修复，可以开始织造了。", "继续", CloseGame);
                OnGameCompleted?.Invoke();
            });
            Debug.Log("[LoomRepairGameManager] 织造完成！");
        }

        // ===== 输入处理（空格键） =====

        private void LateUpdate()
        {
            // 空格键处理
            if (!WorkshopMinigameMenu.IsPaused && Input.GetKeyDown(KeyCode.Space))
            {
                HandleSpaceInput();
            }
        }

        private void HandleSpaceInput()
        {
            if (gameCompleted) return;

            if (!isPlaying)
            {
                // 未开始 → 开始游戏
                StartGame();
                return;
            }

            if (isCalibrating && calibrationPanel != null && calibrationPanel.IsWaitingForInput)
            {
                calibrationPanel.StopAtCurrentAngle();
            }
        }
    }
}
