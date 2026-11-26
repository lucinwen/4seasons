using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 蚕茧簇架交互（运行时自动挂到“2-蚕茧簇架”上，不改场景）：
/// - 玩家进入范围 → 未完成显示“开始择茧”，获得索绪后显示“再玩一次”
/// - 点击按钮 → 以叠加层形式打开“蔟上收茧”小游戏（不切换场景）
/// 按钮样式/字号与春天场景的悬浮按钮完全一致。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SummerCocoonRackTrigger : MonoBehaviour
{
    [Header("择茧小游戏场景名")]
    public string cocoonSceneName = "CocoonMatch";

    [Header("悬浮按钮")]
    public string buttonText = "开始择茧";
    public Vector3 buttonOffset = new Vector3(0f, 2.4f, 0f);
    [Range(0f, 1f)]
    public float buttonOpacity = 0.9f;

    private FloatingActionButton _floatBtn;
    private bool _playerInRange;

    private void Start()
    {
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure(ActionLabel, buttonOffset, OnButtonClicked);
        _floatBtn.SetOpacity(buttonOpacity);
        WorkshopPickupGlow.Bind(gameObject, () => !WorkshopProgress.HasStep(WorkshopWordRules.Sorted));
    }

    private string ActionLabel => !WorkshopProgress.Has(WorkshopItem.Suoxu) ? "下簇收茧"
        : WorkshopProgress.HasStep(WorkshopWordRules.Sorted) ? "重玩择茧" : "择茧 · 别踩白块";

    private void Update()
    {
        if (_floatBtn == null) return;
        if (_floatBtn.buttonText != ActionLabel) _floatBtn.SetText(ActionLabel);
        bool visible = _playerInRange && !WorkshopQuestGuide.BlocksWorldInput &&
            !WorkshopQuestGuide.IsWorldOverlayActive && !WorkshopChapterGuide.IsOpen;
        if (visible && !_floatBtn.IsVisible) _floatBtn.Show();
        else if (!visible && _floatBtn.IsVisible) _floatBtn.Hide();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _playerInRange = true;
        if (_floatBtn != null) _floatBtn.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _playerInRange = false;
        if (_floatBtn != null) _floatBtn.Hide();
    }

    private void OnButtonClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput) return;
        if (WorkshopProgress.Has(WorkshopItem.Suoxu)) { WorkshopCocoonSorting.Show(); return; }
        SpringSummerNpcDialogue.BeforeCocoon(OpenCocoonGame);
    }

    private void OpenCocoonGame()
    {
        if (_floatBtn != null) _floatBtn.Hide();
        Debug.Log("[SummerCocoonRackTrigger] 叠加打开择茧小游戏 " + cocoonSceneName);
        MiniGameOverlay.Show(cocoonSceneName);
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
