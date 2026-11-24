using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 煮茧锅交互（运行时自动挂到“2-煮茧锅”上，不改场景）：
/// - 玩家进入范围 → 显示悬浮按钮“寻找线轴”
/// - 点击按钮：已取水 + 已拾取木柴 → 进入迷宫小游戏；缺什么提示什么
/// 按钮样式/字号与春天场景的悬浮按钮完全一致。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SummerCookerTrigger : MonoBehaviour
{
    [Header("迷宫场景名")]
    public string mazeSceneName = "Maze_Autumn";

    [Header("悬浮按钮")]
    public string buttonText = "寻找线轴";
    public Vector3 buttonOffset = new Vector3(0f, 1.9f, 0f);
    [Range(0f, 1f)]
    public float buttonOpacity = 0.9f;

    [Header("提示文案")]
    public string missingBothTitle = "条件未满足";
    public string missingWaterTitle = "缺少水";
    [TextArea] public string missingWaterMsg = "请先到“水源”处取水，再来煮茧。";
    public string missingWoodTitle = "缺少木柴";
    [TextArea] public string missingWoodMsg = "请先拾取“木柴”，再来煮茧。";

    private FloatingActionButton _floatBtn;

    private void Start()
    {
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure(buttonText, buttonOffset, OnButtonClicked);
        _floatBtn.SetOpacity(buttonOpacity);
        WorkshopPickupGlow.Bind(gameObject, () => SummerItemInteraction.WoodCollected &&
            SummerItemInteraction.WaterCollected && !WorkshopProgress.Has(WorkshopItem.Spool));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (_floatBtn != null) _floatBtn.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (_floatBtn != null) _floatBtn.Hide();
    }

    private void OnButtonClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput) return;
        SpringSummerNpcDialogue.BeforeBoiling(ContinueAtCooker);
    }

    private void ContinueAtCooker()
    {
        bool hasWater = SummerItemInteraction.WaterCollected;
        bool hasWood = SummerItemInteraction.WoodCollected;

        if (!hasWater || !hasWood)
        {
            string title;
            string msg;
            if (!hasWater && !hasWood)
            {
                title = missingBothTitle;
                msg = missingWaterMsg + "\n\n" + missingWoodMsg;
            }
            else if (!hasWater)
            {
                title = missingWaterTitle;
                msg = missingWaterMsg;
            }
            else
            {
                title = missingWoodTitle;
                msg = missingWoodMsg;
            }

            SimplePopup.Show(title, msg, "知道了", null);
            return;
        }

        if (WorkshopProgress.SeasonalOrderEnabled && !WorkshopObservationView.HasOrderWork(WorkshopObservation.DashuReeling))
        {
            WorkshopObservationView.Show(WorkshopObservation.DashuReeling);
            return;
        }
        SpringSummerNpcDialogue.BeforeFindingSpool(OpenMaze);
    }

    private void OpenMaze()
    {
        // 迷宫结束后卸载顶层，原关卡角色仍在煮茧锅旁。
        if (_floatBtn != null) _floatBtn.Hide();
        MiniGameOverlay.ShowMap(mazeSceneName);
    }

    private static bool IsPlayer(Collider2D other)
    {
        // 玩家碰撞盒可能挂在 Player 的子物体（如 Visual）上，按刚体/根物体判断
        var rb = other.attachedRigidbody;
        var owner = rb != null ? rb.gameObject : other.gameObject;
        if (owner.CompareTag("Player")) return true;
        if (owner.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase)) return true;

        var root = other.transform.root;
        return root.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
    }
}
