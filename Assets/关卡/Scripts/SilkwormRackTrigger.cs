using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 蚕簇架触发器：玩家靠近时在上方显示悬浮"前往喂蚕"按钮。
/// 进入条件：已获得桑叶（SpringPrompt.HasMulberryLeaves）且已完成蚕种清洗（SilkwormWashingController.washingCompleted）。
/// 条件不满足时按钮置灰，点击提示缺少的物品。
/// 挂载到蚕簇架 GameObject 上，需要同物体有 Collider2D (Is Trigger = true)。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SilkwormRackTrigger : MonoBehaviour
{
    [Header("悬浮按钮")]
    public string buttonText = "前往喂蚕";
    public Vector3 buttonOffset = new Vector3(0f, 2.4f, 0f);

    [Tooltip("按钮整体不透明度（与进入桑园按钮一致）")]
    [Range(0f, 1f)]
    public float buttonOpacity = 0.9f;

    [Header("提示文案")]
    public string missingLeavesTitle = "缺少桑叶";
    [TextArea] public string missingLeavesMsg = "请先去桑园采桑，获得桑叶后再来喂蚕。";
    public string missingEggsTitle = "缺少蚕种";
    [TextArea] public string missingEggsMsg = "请先完成蚕种清洗，再来喂蚕。";

    [Header("目标场景（后续喂蚕小游戏）")]
    public string targetSceneName = "";

    private FloatingActionButton _floatBtn;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure(buttonText, buttonOffset, OnRackClicked);
        _floatBtn.SetOpacity(buttonOpacity);
        WorkshopPickupGlow.Bind(gameObject, () => SpringPrompt.HasMulberryLeaves &&
            SilkwormWashingController.washingCompleted && !WorkshopProgress.Has(WorkshopItem.Silkworm));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (HarvestOverlayManager.IsHarvestActive) return; // 采桑进行中不弹按钮
        // 外观与“进入桑园”按钮完全一致（不使用置灰态），前置条件在点击时校验
        if (_floatBtn != null) _floatBtn.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (_floatBtn != null) _floatBtn.Hide();
    }

    private void OnRackClicked()
    {
        // 已经喂过蚕，不再重复进入
        if (SpringPrompt.HasSilkwormBaby)
        {
            SimplePopup.Show("已经喂过了", "蚕宝宝已经吃得饱饱的啦，不用再喂了。", "知道了", null);
            return;
        }

        bool hasLeaves = SpringPrompt.HasMulberryLeaves;
        bool hasEggs = SilkwormWashingController.washingCompleted;

        // 缺少什么条件就显示什么；两个都缺则一起显示
        if (!hasLeaves || !hasEggs)
        {
            string title;
            string msg;
            if (!hasLeaves && !hasEggs)
            {
                title = "条件未满足";
                msg = missingLeavesMsg + "\n\n" + missingEggsMsg;
            }
            else if (!hasLeaves)
            {
                title = missingLeavesTitle;
                msg = missingLeavesMsg;
            }
            else
            {
                title = missingEggsTitle;
                msg = missingEggsMsg;
            }

            SimplePopup.Show(title, msg, "知道了", null);
            return;
        }

        // 条件满足：进入喂蚕小游戏
        if (_floatBtn != null) _floatBtn.Hide();

        if (FeedOverlayManager.Instance != null)
        {
            Debug.Log("[SilkwormRackTrigger] 条件满足，进入喂蚕小游戏");
            FeedOverlayManager.Instance.ShowFeed();
        }
        else if (!string.IsNullOrEmpty(targetSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("[SilkwormRackTrigger] 未找到 FeedOverlayManager，无法开始喂蚕");
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") ||
               other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
    }
}
