using UnityEngine;
using UnityEngine.EventSystems;
using Qingming.LevelBackground;

/// <summary>
/// 冬天场景「蚕连架」的点击拾取：玩家点击道具后弹出获得蚕连提示。
/// 不显示任何悬浮提示/名字，由 WinterLevelInteractions 运行时挂载，不改场景。
/// </summary>
public class WinterEggSheetRackClickTarget : MonoBehaviour
{
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        WorkshopPickupGlow.Bind(gameObject, () => !WorkshopProgress.Has(WorkshopItem.SilkwormEggSheet));
    }

    private void Update()
    {
        if (WorkshopProgress.Has(WorkshopItem.SilkwormEggSheet)) return;
        if (WinterGrindingOverlay.IsActive || WinterDyeingOverlay.IsActive) return;
        if (WorkshopQuestGuide.BlocksWorldInput || !Input.GetMouseButtonDown(0)) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null || _collider == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        if (!_collider.OverlapPoint(world)) return;

        PickUp();
    }

    private void PickUp()
    {
        var sr = GetComponent<SpriteRenderer>();
        var icon = sr != null ? sr.sprite : null;

        SimplePopup.ShowItem(WorkshopItem.SilkwormEggSheet, "收好这卷蚕连，留存来年春蚕的蚕种。", () =>
        {
            WorkshopProgress.Award(WorkshopItem.SilkwormEggSheet);
            WorkshopProcessCardView.Show(WorkshopProcessCard.WinterEggs, null);
        }, fallbackIcon: icon);
    }
}
