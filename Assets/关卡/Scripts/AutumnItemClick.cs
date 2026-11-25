using UnityEngine;
using UnityEngine.EventSystems;
using Qingming.LevelBackground;

/// <summary>
/// 秋天关卡中可点击获取的道具（酒 / 水稻 / 香蕉 / 工具箱）。
/// 挂载后鼠标点击道具图像即可触发，不创建任何悬浮提示或标签。
/// 弹窗与春天场景共用 SimplePopup，尺寸/字体完全一致。
/// </summary>
public class AutumnItemClick : MonoBehaviour
{
    public enum ItemKind
    {
        Wine,       // 蚕花酒：知识卡片 + 获取弹窗
        Rice,       // 水稻：直接获取
        Toolbox,    // 工具箱：梭子知识卡片 + 获取
        Banana     // 香蕉果篮：领取供桌贡品
    }

    public ItemKind kind = ItemKind.Wine;

    private const string WineKnowledge =
        "蚕花酒是江南蚕乡的传统米酒，以新糯米与蚕花酿成。\n" +
        "清明前后祭祀蚕神时，人们以蚕花酒祈求蚕事顺遂、茧丝丰收。";

    private const string ShuttleKnowledge =
        "梭子是织机的引纬工具，内装纬线，在经线间往返穿梭，\n" +
        "把纬线织入经线，布面由此成形。";

    private SpriteRenderer _sr;
    private Collider2D[] _colliders;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _colliders = GetComponents<Collider2D>();
    }

    private void Start()
    {
        if (kind != ItemKind.Rice) WorkshopPickupGlow.Bind(gameObject, IsUncollectedTarget);
    }

    private bool IsUncollectedTarget()
    {
        switch (kind)
        {
            case ItemKind.Wine:
                return !AutumnLevelState.HasWine && !WorkshopProgress.Has(WorkshopItem.Statue);
            case ItemKind.Banana:
                return !AutumnLevelState.HasBanana && !WorkshopProgress.Has(WorkshopItem.Statue);
            case ItemKind.Toolbox:
                return !AutumnLevelState.HasShuttle && !WorkshopProgress.Has(WorkshopItem.Shuttle);
            default:
                return false;
        }
    }

    private void Update()
    {
        bool pressed = Input.GetMouseButtonDown(0);
        if (WorkshopQuestGuide.BlocksWorldInput || (!pressed && !WorkshopPointer.CanShowWorldHint)) return;

        // 小游戏/弹窗进行中不响应
        if (MiniGameOverlay.IsActive) return;
        if (HarvestOverlayManager.IsHarvestActive) return;
        if (FeedOverlayManager.Instance != null && FeedOverlayManager.Instance.IsActive) return;
        if (WorkshopKnowledgeView.IsOpen) return;

        // 点击 UI 时不响应
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        bool hasEnabledCollider = false;
        foreach (var col in _colliders)
        {
            if (col == null || !col.enabled) continue;
            hasEnabledCollider = true;
            if (col.OverlapPoint(world))
            {
                HandlePointerHit(pressed);
                return;
            }
        }

        // 香蕉果篮等陈设没有点击碰撞盒，直接使用精灵的局部范围。
        if (!hasEnabledCollider && _sr != null && _sr.enabled && _sr.sprite != null)
        {
            Vector3 local = _sr.transform.InverseTransformPoint(world);
            Bounds hitArea = _sr.sprite.bounds;
            if (kind == ItemKind.Banana) hitArea.Expand(new Vector3(1.2f, 1.2f, 0f));
            if (hitArea.Contains(new Vector3(local.x, local.y, 0f)))
                HandlePointerHit(pressed);
        }
    }

    private void HandlePointerHit(bool pressed)
    {
        if (FindObjectOfType<SimplePopup>() != null) return;
        if (kind != ItemKind.Rice || !AutumnLevelState.HasRice) WorkshopPointer.PointAtWorldTarget();
        if (pressed) OnClicked();
    }

    private void OnClicked()
    {
        switch (kind)
        {
            case ItemKind.Wine:
                if (AutumnLevelState.HasRitualDone) ShowWineCard();
                else AutumnNpcDialogue.IntroduceRitual(ShowWineCard);
                break;
            case ItemKind.Rice:
                AcquireRice();
                break;
            case ItemKind.Toolbox:
                AcquireShuttle();
                break;
            case ItemKind.Banana:
                if (AutumnLevelState.HasRitualDone) ShowBanana();
                else AutumnNpcDialogue.IntroduceRitual(ShowBanana);
                break;
        }
    }

    // ==================== 蚕花酒 ====================
    private void ShowWineCard()
    {
        var icon = _sr != null ? _sr.sprite : null;

        if (AutumnLevelState.HasWine)
        {
            SimplePopup.Show("蚕花酒", WineKnowledge, "知道了", null,
                false, "取消", null, icon);
            return;
        }

        SimplePopup.Show("蚕花酒", WineKnowledge, "获取",
            () =>
            {
                AutumnLevelState.HasWine = true;
                WorkshopProgress.MarkStep("autumn.wine");
                ShowGain("蚕花酒", icon);
            },
            true, "取消", null, icon);
    }

    // ==================== 水稻 ====================
    private void AcquireRice()
    {
        if (AutumnLevelState.HasRice) return;
        AutumnLevelState.HasRice = true;
        WorkshopProgress.MarkStep("autumn.rice");
        ShowGain("水稻", _sr != null ? _sr.sprite : null);
    }

    private static void ShowBanana()
    {
        Sprite icon = AutumnItemIcons.Load()?.banana;
        if (AutumnLevelState.HasBanana)
        {
            SimplePopup.Show("香蕉", "已收集香蕉贡品。", "知道了", null, icon: icon);
            return;
        }

        SimplePopup.Show("香蕉", "为酬蚕仪式准备一份香蕉。", "拾取", () =>
        {
            AutumnLevelState.HasBanana = true;
            WorkshopProgress.MarkStep("autumn.banana");
        }, true, "取消", null, icon);
    }

    // ==================== 工具箱 → 梭子 ====================
    private void AcquireShuttle()
    {
        Sprite icon = AutumnItemIcons.Load()?.shuttle;

        if (AutumnLevelState.HasShuttle)
        {
            SimplePopup.Show("梭子", ShuttleKnowledge, "知道了", null,
                false, "取消", null, icon);
            return;
        }

        SimplePopup.Show("梭子", ShuttleKnowledge, "获取",
            () =>
            {
                AutumnLevelState.HasShuttle = true;
                AutumnLevelState.HasToolbox = true;
                WorkshopProgress.Award(WorkshopItem.Shuttle);
                SimplePopup.ShowItem(WorkshopItem.Shuttle, "获得【梭子】×1", null, fallbackIcon: icon);
            },
            true, "取消", null, icon);
    }

    private static void ShowGain(string itemName, Sprite icon, System.Action onConfirm = null)
    {
        SimplePopup.Show("获得道具", "获得【" + itemName + "】×1", "确定", onConfirm,
            false, "取消", null, icon);
    }
}
