using UnityEngine;
using Qingming.LevelBackground;
using JingweiWeave;

/// <summary>
/// 秋天关卡「3-织机」触发器：玩家靠近时显示悬浮按钮。
/// 未修复时显示「修复织机」，前置条件：拾取织机左侧地上的锤子。
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
    private SpriteRenderer _loomRenderer;
    private Sprite _repairedLoomSprite;

    private void Awake()
    {
        _loomRenderer = GetComponent<SpriteRenderer>();
        _repairedLoomSprite = AutumnItemIcons.Load()?.repairedLoom;
        UpdateLoomAppearance();
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure("修复织机", new Vector3(0f, 2.4f, 0f), OnLoomClicked);
        _floatBtn.SetOpacity(0.9f);
        WorkshopPickupGlow.Bind(gameObject, () => !WorkshopProgress.Has(WorkshopItem.GreigeCloth) &&
            (AutumnLevelState.HasLoomRepaired ? AutumnLevelState.HasShuttle : AutumnLevelState.HasHammer), 0.72f);
    }

    private void Start()
    {
        var go = GameObject.Find("Player");
        if (go != null) _player = go.transform;
    }

    private void Update()
    {
        UpdateLoomAppearance();
        // 修复完成后按钮文字切换为「开始织造」
        string desired = WorkshopProgress.HasStep(WorkshopWordRules.Patterned) ? "织造攀花已完成" :
            !AutumnLevelState.HasLoomRepaired ? "修复织机" :
            !WorkshopProgress.Has(WorkshopItem.GreigeCloth) ? "白露 · 奇偶推梭" : "寒露 · 经纬攀花";
        if (_floatBtn.buttonText != desired) _floatBtn.SetText(desired);

        if (_player == null) return;

        bool inRange = !WorkshopQuestGuide.BlocksWorldInput && !WorkshopQuestGuide.IsWorldOverlayActive &&
            Vector2.Distance(_player.position, transform.position) <= triggerDistance;
        if (inRange == _shown) return;

        _shown = inRange;
        if (inRange) _floatBtn.Show();
        else _floatBtn.Hide();
    }

    private void OnLoomClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive) return;
        if (AutumnLevelState.HasLoomRepaired && WorkshopProgress.Has(WorkshopItem.GreigeCloth))
            OpenLoom();
        else
            AutumnNpcDialogue.IntroduceWeaving(OpenLoom);
    }

    private void OpenLoom()
    {
        if (WorkshopProgress.HasStep(WorkshopWordRules.Patterned))
        {
            SimplePopup.Show("攀花已完成", "纹样已经织好，梭子、缬与染布都还在。收齐土地公雕像后回工坊交付秋季成果。", "知道了");
            return;
        }
        if (!AutumnLevelState.HasLoomRepaired)
        {
            if (!AutumnLevelState.HasHammer)
            {
                SimplePopup.Show("缺少修复工具", "修机锤在织机左侧的地上，点击拾取后再来修复织机。", "知道了", null);
                return;
            }

            _floatBtn.Hide();
            _shown = false; // 关闭叠加层返回后重新显示按钮
            WorkshopProcessCardView.Show(WorkshopProcessCard.BailuWeaving, () => MiniGameOverlay.Show("LoomRepair"));
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
        if (!WorkshopProgress.Has(WorkshopItem.GreigeCloth))
        {
            WorkshopProcessCardView.Show(WorkshopProcessCard.BailuWeaving,
                () => WorkshopSimpleGames.Show(WorkshopSimpleGame.Weaving));
            return;
        }
        if (!WorkshopWordRules.CanPattern)
        {
            SimplePopup.Show("先染好布", "已获得生坯绸。先到青染轩研磨染材、选择颜色染布，再带梭子回来攀花。道具不会消耗。", "知道了");
            return;
        }
        ShowEmbroidery(WorkshopProcessCard.Hanlu, EmbroideryPlayMode.HanluPattern);
    }

    private static void ShowEmbroidery(WorkshopProcessCard card, EmbroideryPlayMode mode)
    {
        WorkshopProcessCardView.Show(card, () =>
        {
            EmbroideryGameManager.SetNextMode(mode);
            MiniGameOverlay.Show("EmbroideryScene");
        });
    }

    private void UpdateLoomAppearance()
    {
        if (!AutumnLevelState.HasLoomRepaired || _loomRenderer == null || _repairedLoomSprite == null) return;
        if (_loomRenderer.sprite != _repairedLoomSprite) _loomRenderer.sprite = _repairedLoomSprite;
    }

    private void OnDestroy()
    {
        if (_floatBtn != null) _floatBtn.Hide();
    }
}
