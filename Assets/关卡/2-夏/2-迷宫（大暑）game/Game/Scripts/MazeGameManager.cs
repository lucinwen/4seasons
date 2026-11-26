using System;
using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye.MazeGame
{
    public enum MazeState { Ready, Playing, Completed }

    /// <summary>
    /// 迷宫游戏管理器：状态机、通关判定、奖励发放、完成事件。
    /// </summary>
    public class MazeGameManager : MonoBehaviour
    {
        [Header("状态")]
        public MazeState state = MazeState.Ready;

        [Header("引用")]
        public MazePlayerController player;
        public MazeGoalTrigger goal;
        public MazeRewardPopup rewardPopup;

        [Header("UI")]
        public GameObject instructionPanel;
        public Button startButton;

        [Header("奖励")]
        public int whiteThreadReward = 1;

        public event Action OnMazeCompleted;

        private bool rewardGiven;
        private Vector3 spawnPosition;

        internal bool HasRunProgress => state == MazeState.Playing && player != null &&
            (player.transform.position - spawnPosition).sqrMagnitude > 0.0001f;

        private void Awake()
        {
            MinigameUiPresentation.Dialog(instructionPanel, "Body", "Title");
            MinigameUiPresentation.Action(startButton);
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
        }

        private void Start()
        {
            // 绑定 goal → manager
            if (goal != null) goal.BindManager(this);

            // 初始 Ready 状态：显示提示，禁止移动
            state = MazeState.Ready;
            if (instructionPanel != null) instructionPanel.SetActive(true);
            if (player != null)
            {
                spawnPosition = player.transform.position;
                player.ControlEnabled = false;
            }
            OnStartClicked();
        }

        private void OnStartClicked()
        {
            if (state != MazeState.Ready) return;
            state = MazeState.Playing;
            if (instructionPanel != null) instructionPanel.SetActive(false);
            if (player != null) player.ControlEnabled = true;
        }

        public void OnPlayerReachGoal()
        {
            if (state != MazeState.Playing) return;

            state = MazeState.Completed;
            if (player != null) player.Stop();

            MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.MazeSpool, () =>
            {
                // 动画结束后再显示奖励弹窗
                if (rewardPopup != null) rewardPopup.Show();

                // 奖励只发一次
                if (!rewardGiven)
                {
                    rewardGiven = true;
                    OnMazeCompleted?.Invoke();
                }
            });
        }
    }
}
