using System.Collections;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Qingming.Harvest
{
    public enum HarvestGameState
    {
        WaitingToStart,
        Playing,
        Completed
    }

    /// <summary>
    /// 采桑小游戏总管：游戏状态、收集数量、通关判定、重置与主游戏接口。
    /// </summary>
    public class MulberryHarvestGameManager : MonoBehaviour
    {
        public static MulberryHarvestGameManager Instance { get; set; }

        [Header("通关目标")]
        public int targetLeaves = 8;

        [Header("引用")]
        public HarvestPlayerController player;
        public HarvestBasketController basket;
        public MulberryLeafSpawner spawner;
        public HarvestUIController ui;

        /// <summary>当前已收集数量（上限 targetLeaves）。</summary>
        public int CollectedLeaves { get; private set; }

        /// <summary>游戏当前状态。</summary>
        public HarvestGameState State { get; private set; } = HarvestGameState.WaitingToStart;

        /// <summary>通关后触发一次，供主游戏订阅。</summary>
        public event System.Action OnHarvestCompleted;

        private bool _completionFired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Debug.Log($"[HarvestGameManager] Start() called. player={player != null}, basket={basket != null}, spawner={spawner != null}, ui={ui != null}");
            // 防御性：清理任何来源的 FallingMulberryLeaf（不管是不是 Spawner 创建的）
            var allLeaves = FindObjectsOfType<FallingMulberryLeaf>(true);
            for (int i = allLeaves.Length - 1; i >= 0; i--)
            {
                if (allLeaves[i] != null) Destroy(allLeaves[i].gameObject);
            }

            // 初始等待状态：禁用玩家移动与生成
            if (player != null) player.SetControlEnabled(false);
            if (spawner != null)
            {
                spawner.StopSpawning();
                spawner.ClearAllLeaves();
            }
            if (ui != null)
            {
                ui.ShowIntro();
                Debug.Log("[HarvestGameManager] 开场面板已显示");
            }
            else
            {
                Debug.LogError("[HarvestGameManager] ui 为 null！");
            }
            UpdateUI();
        }

        /// <summary>开始按钮点击后调用，正式进入游玩状态。</summary>
        public void StartGame()
        {
            Debug.Log($"[HarvestGameManager] StartGame() called. State={State}");
            if (State != HarvestGameState.WaitingToStart) return;
            State = HarvestGameState.Playing;

            if (player != null) player.SetControlEnabled(true);
            if (spawner != null) spawner.StartSpawning();
            if (ui != null) ui.HideIntro();
            Debug.Log("[HarvestGameManager] 游戏已开始！");
        }

        /// <summary>
        /// 被 FallingMulberryLeaf.OnCollected 调用，记录一次拾取。
        /// </summary>
        public void NotifyLeafCollected()
        {
            if (State != HarvestGameState.Playing) return;

            CollectedLeaves++;
            if (CollectedLeaves > targetLeaves) CollectedLeaves = targetLeaves;

            if (basket != null) basket.UpdateBasketSprite(CollectedLeaves);
            UpdateUI();

            if (CollectedLeaves >= targetLeaves)
            {
                CompleteGame();
            }
        }

        private void CompleteGame()
        {
            State = HarvestGameState.Completed;

            // 标记全局状态：已获得桑叶（供后续喂蚕等环节判断）
            SpringPrompt.HasMulberryLeaves = true;

            if (spawner != null) spawner.StopSpawning();
            if (player != null) player.SetControlEnabled(false);
            if (basket != null) basket.UpdateBasketSprite(targetLeaves);
            UpdateUI();

            // 清理残留桑叶
            StartCoroutine(CleanupCoroutine());

            if (!_completionFired)
            {
                _completionFired = true;
                OnHarvestCompleted?.Invoke();
            }

            MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.MulberryHarvest, ExitGame);
        }

        private bool _returning;

        /// <summary>从采桑结算返回春季场景。</summary>
        public void ExitGame()
        {
            if (_returning || State != HarvestGameState.Completed) return;
            _returning = true;
            if (WorkshopProgress.Has(WorkshopItem.MulberryLeaf))
                ShowHarvestReward();
            else
                WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.SpringMulberryComplete, ShowHarvestReward);
        }

        private void ShowHarvestReward()
        {
            SimplePopup.ShowItem(WorkshopItem.MulberryLeaf, "新鲜桑叶已经收进篮子。", ReturnToSpring);
        }

        private void ReturnToSpring()
        {
            WorkshopProgress.Award(WorkshopItem.MulberryLeaf);

            // 覆盖层模式：隐藏采桑覆盖层，恢复原场景
            if (HarvestOverlayManager.Instance != null && HarvestOverlayManager.IsHarvestActive)
                HarvestOverlayManager.Instance.HideHarvest();
            else
                SceneManager.LoadScene("Level1_Spring");
        }

        private IEnumerator CleanupCoroutine()
        {
            // 等一小段时间让玩家看到最后的状态，再清理
            yield return new WaitForSeconds(0.3f);
            if (spawner != null) spawner.ClearAllLeaves();
        }

        /// <summary>重置到初始等待状态。</summary>
        public void ResetGame()
        {
            CollectedLeaves = 0;
            State = HarvestGameState.WaitingToStart;
            _completionFired = false;
            _returning = false;

            if (spawner != null)
            {
                spawner.StopSpawning();
                spawner.ClearAllLeaves();
            }
            if (player != null)
            {
                player.ResetPosition();
                player.SetControlEnabled(false);
            }
            if (basket != null) basket.UpdateBasketSprite(0);
            if (ui != null)
            {
                ui.HideComplete();
                ui.ShowIntro();
            }
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (ui != null) ui.UpdateProgress(CollectedLeaves, targetLeaves);
        }
    }
}
