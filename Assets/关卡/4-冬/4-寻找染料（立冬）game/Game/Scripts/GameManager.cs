using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace JiangnanDye
{
    public enum GameState
    {
        Exploring = 0,
        RewardPrompt = 1,
        Settling = 2
    }

    /// <summary>
    /// 游戏整体状态机：探索 / 收下丢弃弹窗 / 结算。
    /// 只负责流程串联，具体功能由各系统脚本承担。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("场景引用")]
        public PlayerController player;
        public InteractionDetector detector;
        public CollectionInventory inventory;
        public DyeCollectionManager dyeHistory;
        public ItemRewardManager rewardManager;
        public ItemRewardPopup rewardPopup;
        public CollectionProgressUI progressUI;
        public SettlementUI settlementUI;
        public IntroUI introUI;
        public CollectEffect collectEffect;
        public ToastUI toast;
        public GatherableNode[] allNodes;

        [Header("界面显隐")]
        public GameObject hudRoot;
        public GameObject hoeRoot;

        public GameState State { get; private set; } = GameState.Exploring;

        internal bool HasRunProgress => allNodes != null &&
            System.Array.Exists(allNodes, node => node != null && node.isCollected);

        public event System.Action<GameState> StateChanged;

        // 每种颜色 3 个相近点位，同一时间只激活一个；累计采集 3 次后该颜色点位失效。
        private const int POINTS_PER_COLOR = 3;
        private readonly System.Collections.Generic.List<GatherableNode>[] colorPoints =
            new System.Collections.Generic.List<GatherableNode>[5];
        private readonly int[] colorHarvested = new int[5];

        private void Awake()
        {
            Instance = this;
            GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);
        }

        private void Start()
        {
            // 每次开局重新收集场景中的全部资源点，避免新增点位漏掉
            allNodes = System.Array.FindAll(FindObjectsOfType<GatherableNode>(),
                node => node.gameObject.scene == gameObject.scene);

            // 先禁用控制和 HUD，展示开场介绍弹窗
            if (player != null) player.SetControlEnabled(false);
            if (hudRoot != null) hudRoot.SetActive(false);
            if (hoeRoot != null) hoeRoot.SetActive(false);
            SetState(GameState.Exploring);

            if (introUI != null) introUI.Show();
            else StartRound();
        }

        /// <summary>开场介绍的"开始探索"按钮回调，正式进入游戏。</summary>
        public void StartRound()
        {
            ResetRound();
        }

        private void ResetRound()
        {
            if (rewardPopup != null) rewardPopup.Hide();
            if (rewardManager != null) rewardManager.Clear();
            if (settlementUI != null) settlementUI.Hide();
            if (inventory != null) inventory.Clear();
            if (dyeHistory != null) dyeHistory.ResetStats();

            if (allNodes != null)
            {
                for (int i = 0; i < allNodes.Length; i++)
                {
                    if (allNodes[i] != null) allNodes[i].ResetNode();
                }
            }

            SetupDyePoints();

            if (detector != null) detector.ClearTarget();
            if (progressUI != null) progressUI.Refresh(0, CollectionInventory.MAX_CAPACITY, null);
            if (player != null) player.SetControlEnabled(true);
            if (hudRoot != null) hudRoot.SetActive(true);
            if (hoeRoot != null) hoeRoot.SetActive(true);
            SetState(GameState.Exploring);
        }

        /// <summary>
        /// 每种颜色准备 3 个相近点位：随机排序后依次激活，同一时间只亮一个；
        /// 三次品质随机且保证至少一次为健康染料。采集 3 次后该颜色全部失效。
        /// </summary>
        private void SetupDyePoints()
        {
            for (int c = 0; c < colorPoints.Length; c++)
            {
                colorPoints[c] = new System.Collections.Generic.List<GatherableNode>();
                colorHarvested[c] = 0;
            }

            if (allNodes != null)
            {
                for (int i = 0; i < allNodes.Length; i++)
                {
                    GatherableNode node = allNodes[i];
                    if (node == null || node.resourceType != ResourceType.Plant) continue;
                    int colorIndex = (int)node.dyeColor;
                    if (colorIndex >= 0 && colorIndex < colorPoints.Length)
                    {
                        colorPoints[colorIndex].Add(node);
                    }
                }
            }

            for (int c = 0; c < colorPoints.Length; c++)
            {
                System.Collections.Generic.List<GatherableNode> points = colorPoints[c];
                if (points.Count == 0) continue;

                // 随机点位顺序（“随机出现”的位置差来自三个相近点位）
                for (int i = points.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    GatherableNode temp = points[i];
                    points[i] = points[j];
                    points[j] = temp;
                }

                // 三次品质随机，但至少有一次健康
                DyeCondition[] conditions = new DyeCondition[points.Count];
                bool hasNormal = false;
                for (int i = 0; i < conditions.Length; i++)
                {
                    conditions[i] = Random.value < 0.5f ? DyeCondition.Normal : DyeCondition.Damaged;
                    if (conditions[i] == DyeCondition.Normal) hasNormal = true;
                }
                if (!hasNormal)
                {
                    conditions[Random.Range(0, conditions.Length)] = DyeCondition.Normal;
                }

                for (int i = 0; i < points.Count; i++)
                {
                    GatherableNode point = points[i];
                    ItemData item = point.dyeDefinition != null
                        ? point.dyeDefinition.GetItem(conditions[i])
                        : point.itemData;
                    point.ApplyRoundState(conditions[i], item);
                    point.SetInteractable(i == 0);
                }
            }
        }

        private void AdvanceDyePoint(GatherableNode harvested)
        {
            int colorIndex = (int)harvested.dyeColor;
            if (colorIndex < 0 || colorIndex >= colorPoints.Length) return;
            System.Collections.Generic.List<GatherableNode> points = colorPoints[colorIndex];
            if (points == null) return;

            colorHarvested[colorIndex]++;
            int next = colorHarvested[colorIndex];
            if (next < points.Count && !points[next].isCollected)
            {
                points[next].SetInteractable(true);
            }
            else
            {
                for (int i = 0; i < points.Count; i++) points[i].SetInteractable(false);
            }
        }

        /// <summary>锄头按钮点击后由 HoeButtonUI 调用。</summary>
        public void HarvestCurrent()
        {
            if (State != GameState.Exploring) return;
            if (detector == null) return;
            Harvest(detector.CurrentTarget);
        }

        public void Harvest(GatherableNode node)
        {
            if (State != GameState.Exploring || WorkshopMinigameMenu.IsPaused) return;
            if (node == null || !node.CanHarvest) return;

            if (node.resourceType == ResourceType.Stone)
            {
                node.Collect();
                if (detector != null) detector.ClearTarget();
                if (collectEffect != null)
                {
                    collectEffect.Play(node.transform.position + new Vector3(0f, 0.3f, 0f),
                        new Color(0.78f, 0.77f, 0.72f));
                }
                if (toast != null) toast.Show("只是普通山石，里面没有染料", 1.8f);
                return;
            }

            ItemData item = node.itemData;
            node.Collect();
            AdvanceDyePoint(node);
            if (detector != null) detector.ClearTarget();

            if (dyeHistory != null) dyeHistory.RecordHarvest(item);
            if (collectEffect != null)
            {
                collectEffect.Play(node.transform.position + new Vector3(0f, 0.4f, 0f),
                    DyeColorUtil.Tint(node.dyeColor));
            }
            if (rewardManager != null) rewardManager.Show(item);
            if (toast != null) toast.Hide();
            if (player != null) player.SetControlEnabled(false);
            SetState(GameState.RewardPrompt);
        }

        public void AcceptPending()
        {
            if (State != GameState.RewardPrompt) return;
            if (rewardManager == null || rewardManager.PendingItem == null) return;

            ItemData item = rewardManager.PendingItem;
            rewardManager.Clear();
            if (rewardPopup != null) rewardPopup.Hide();

            if (dyeHistory != null) dyeHistory.RecordAccept(item);
            if (inventory != null) inventory.TryAdd(item);
            if (progressUI != null)
            {
                progressUI.Refresh(inventory != null ? inventory.Count : 0,
                    CollectionInventory.MAX_CAPACITY,
                    inventory != null ? inventory.Items : null);
            }

            if (State == GameState.RewardPrompt)
            {
                SetState(GameState.Exploring);
                if (player != null) player.SetControlEnabled(true);
            }
        }

        public void DiscardPending()
        {
            if (State != GameState.RewardPrompt) return;
            if (rewardManager == null || rewardManager.PendingItem == null) return;

            if (rewardPopup != null) rewardPopup.Hide();
            rewardManager.Clear();

            if (dyeHistory != null) dyeHistory.RecordDiscard(null);
            SetState(GameState.Exploring);
            if (player != null) player.SetControlEnabled(true);
        }

        public void EnterSettlement()
        {
            if (State == GameState.Settling) return;
            SetState(GameState.Settling);

            if (player != null) player.SetControlEnabled(false);
            if (detector != null) detector.ClearTarget();
            if (rewardPopup != null) rewardPopup.Hide();
            if (hudRoot != null) hudRoot.SetActive(false);
            if (hoeRoot != null) hoeRoot.SetActive(false);

            MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.DyeCollection, ShowSettlement);
        }

        private void ShowSettlement()
        {
            if (settlementUI != null && inventory != null)
            {
                settlementUI.Show(inventory.Items, dyeHistory);
            }
        }

        /// <summary>结算界面的"前往研磨"按钮：结束小游戏，返回冬天场景并提示玩家询问 NPC 研磨之法。</summary>
        public void GoToGrinding()
        {
            Time.timeScale = 1f;
            if (inventory != null) WinterLevelState.SetCollectedDyes(inventory.Items);
            WinterLevelState.PendingGrindingPrompt = true;
            if (MiniGameOverlay.CloseIfActive(gameObject.scene,
                WinterLevelInteractions.ShowPendingGrindingPrompt)) return;
            SceneManager.LoadScene("Level4_Winter");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            if (StateChanged != null) StateChanged(next);
        }
    }
}
