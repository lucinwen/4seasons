using UnityEngine;
using UnityEngine.EventSystems;
using Qingming.LevelBackground;

/// <summary>
/// 冬天场景「4-研磨棒」的点击拾取：点击后弹出获得道具提示，并记录已获得研磨棒。
/// 由 WinterLevelInteractions 运行时挂载，不改场景。
/// </summary>
public class WinterRodClickTarget : MonoBehaviour
{
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        WorkshopPickupGlow.Bind(gameObject, () => !WinterLevelState.HasGrindingRod && !WorkshopProgress.HasStep("winter.rod") &&
            !WorkshopProgress.Has(WorkshopItem.DyedCloth));
    }

    private void Update()
    {
        if (WinterLevelState.HasGrindingRod) return;
        if (WinterGrindingOverlay.IsActive || WinterDyeingOverlay.IsActive) return;
        bool pressed = Input.GetMouseButtonDown(0);
        if (WorkshopQuestGuide.BlocksWorldInput || (!pressed && !WorkshopPointer.CanShowWorldHint)) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null || _collider == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        if (!_collider.OverlapPoint(world)) return;

        WorkshopPointer.PointAtWorldTarget();
        if (!pressed) return;

        PickUp();
    }

    private void PickUp()
    {
        var config = Resources.Load<WinterGrindingConfig>("WinterGrindingConfig");
        var icon = config != null ? config.rod : null;

        SimplePopup.Show("获得道具", "获得【研磨棒】", "收下", () =>
        {
            WinterLevelState.HasGrindingRod = true;
            WorkshopProgress.MarkStep("winter.rod");
            gameObject.SetActive(false);
            Debug.Log("[WinterRodClickTarget] ✅ 已获得研磨棒");
        }, false, "取消", null, icon);
    }
}
