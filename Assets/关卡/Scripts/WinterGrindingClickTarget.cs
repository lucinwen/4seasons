using UnityEngine;
using UnityEngine.EventSystems;
using Qingming.LevelBackground;

/// <summary>
/// 挂在冬天场景的「4-染料空」（钵体）上：点击钵体打开研磨界面。
/// 只要已经获得研磨棒即可研磨。
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
        if (WorkshopQuestGuide.BlocksWorldInput || !Input.GetMouseButtonDown(0)) return;

        // 点在 UI 上时不触发世界点击
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null || _collider == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        if (!_collider.OverlapPoint(world)) return;

        TryOpen();
    }

    private void TryOpen()
    {
        if (!WinterLevelState.HasGrindingRod)
        {
            SimplePopup.Show("研磨", "还没有研磨棒，先去场景里找到并点击研磨棒。", "知道了", null);
            return;
        }

        if (WinterLevelState.CollectedDyes.Count > 0)
            WinterNpcDialogue.IntroduceGrinding(WinterGrindingOverlay.Show);
        else
            WinterGrindingOverlay.Show();
    }
}
