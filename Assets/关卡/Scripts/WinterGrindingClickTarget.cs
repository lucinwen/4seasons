using UnityEngine;
using UnityEngine.EventSystems;
using Qingming.LevelBackground;

/// <summary>
/// 挂在研磨钵（钵体）上：点击打开研磨界面。
/// 前置条件：已拾取研磨棒，且采集到染材。
/// </summary>
public class WinterGrindingClickTarget : MonoBehaviour
{
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        WorkshopPickupGlow.Bind(gameObject, () => WinterLevelState.HasGrindingRod &&
            WinterLevelState.CollectedDyes.Count > 0 && !WorkshopProgress.Has(WorkshopItem.DyedCloth));
    }

    private void Update()
    {
        if (WinterGrindingOverlay.IsActive) return;
        bool pressed = Input.GetMouseButtonDown(0);
        if (WorkshopQuestGuide.BlocksWorldInput || (!pressed && !WorkshopPointer.CanShowWorldHint)) return;

        // 点在 UI 上时不触发世界点击
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null || _collider == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        if (!_collider.OverlapPoint(world)) return;

        WorkshopPointer.PointAtWorldTarget();
        if (!pressed) return;

        TryOpen();
    }

    private void TryOpen()
    {
        if (!WinterLevelState.HasGrindingRod)
        {
            SimplePopup.Show("缺少研磨棒", "在染坊里点击研磨棒拾取。", "知道了", null);
            return;
        }

        if (WinterLevelState.CollectedDyes.Count > 0)
            WinterNpcDialogue.IntroduceGrinding(WinterGrindingOverlay.Show);
        else
            WinterGrindingOverlay.Show(); // 内部会提示先采集染材
    }
}
