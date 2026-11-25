using UnityEngine;
using UnityEngine.EventSystems;
using Qingming.LevelBackground;

/// <summary>
/// 冬天场景「4-裁衣工具针」的点击拾取：玩家点击道具后弹出获得提示。
/// 不显示任何悬浮提示/名字，由 WinterLevelInteractions 运行时挂载，不改场景。
/// </summary>
public class WinterNeedleClickTarget : MonoBehaviour
{
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        WorkshopPickupGlow.Bind(gameObject, () => !WinterLevelState.HasSewingNeedle && !WorkshopProgress.Has(WorkshopItem.Needle));
    }

    private void Update()
    {
        if (WinterLevelState.HasSewingNeedle) return;
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

        if (WinterLevelState.HasDyedCloth && !WorkshopProgress.Has(WorkshopItem.CutPieces))
            WinterNpcDialogue.IntroduceTailoring(PickUp);
        else
            PickUp();
    }

    private void PickUp()
    {
        var sr = GetComponent<SpriteRenderer>();
        var icon = sr != null ? sr.sprite : null;

        SimplePopup.ShowItem(WorkshopItem.Needle, "获得【缝衣针】", () =>
        {
            WinterLevelState.HasSewingNeedle = true;
            WorkshopProgress.Award(WorkshopItem.Needle);
            gameObject.SetActive(false);
        }, fallbackIcon: icon);
    }
}
