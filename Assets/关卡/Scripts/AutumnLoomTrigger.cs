using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 秋天关卡「3-织机」触发器：玩家靠近时显示悬浮按钮。
/// 未修复时显示「修复织机」，前置条件：与 NPC 对话获得锤子。
/// 修复完成后显示「开始织造」，前置条件：点击工具箱获得梭子。
/// 两个小游戏均以叠加层方式打开，不切换场景。
/// </summary>
public class AutumnLoomTrigger : MonoBehaviour
{
    [Tooltip("玩家离织机多近时显示悬浮按钮（世界单位）")]
    public float triggerDistance = 3f;

    private FloatingActionButton _floatBtn;
    private Transform _player;
    private bool _shown;

    private void Awake()
    {
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure("修复织机", new Vector3(0f, 2.4f, 0f), OnLoomClicked);
        _floatBtn.SetOpacity(0.9f);
        WorkshopPickupGlow.Bind(gameObject, () => !WorkshopProgress.Has(WorkshopItem.RawSilkCloth) &&
            (AutumnLevelState.HasLoomRepaired ? AutumnLevelState.HasShuttle : AutumnLevelState.HasHammer), 0.72f);
    }

    private void Start()
    {
        var go = GameObject.Find("Player");
        if (go != null) _player = go.transform;
    }

    private void Update()
    {
        // 修复完成后按钮文字切换为「开始织造」
        string desired = AutumnLevelState.HasLoomRepaired ? "开始织造" : "修复织机";
        if (_floatBtn.buttonText != desired) _floatBtn.SetText(desired);

        if (_player == null) return;

        bool inRange = Vector3.Distance(_player.position, transform.position) <= triggerDistance;
        if (inRange == _shown) return;

        _shown = inRange;
        if (inRange) _floatBtn.Show();
        else _floatBtn.Hide();
    }

    private void OnLoomClicked()
    {
        if (AutumnLevelState.HasLoomRepaired && WorkshopProgress.Has(WorkshopItem.RawSilkCloth))
            OpenLoom();
        else
            AutumnNpcDialogue.IntroduceWeaving(OpenLoom);
    }

    private void OpenLoom()
    {
        if (!AutumnLevelState.HasLoomRepaired)
        {
            if (!AutumnLevelState.HasHammer)
            {
                SimplePopup.Show("缺少修复工具", "请先与 NPC 对话，拿到锤子后再来修复织机。", "知道了", null);
                return;
            }

            _floatBtn.Hide();
            _shown = false; // 关闭叠加层返回后重新显示按钮
            MiniGameOverlay.Show("LoomRepair");
            return;
        }

        // 修复完成后的织造阶段：需要梭子
        if (!AutumnLevelState.HasShuttle)
        {
            SimplePopup.Show("缺少梭子", "请先点击工具箱，获取梭子后再开始织造。", "知道了", null);
            return;
        }

        _floatBtn.Hide();
        _shown = false;
        MiniGameOverlay.Show("EmbroideryScene");
    }

    private void OnDestroy()
    {
        if (_floatBtn != null) _floatBtn.Hide();
    }
}
