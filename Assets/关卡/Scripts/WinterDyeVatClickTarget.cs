using UnityEngine;
using UnityEngine.EventSystems;
using Qingming.LevelBackground;

/// <summary>
/// 挂在冬天场景的「4-染料空缸」（清水缸）上：拥有染料后点击打开染色界面。
/// </summary>
public class WinterDyeVatClickTarget : MonoBehaviour
{
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        WorkshopPickupGlow.Bind(gameObject, () => WorkshopProgress.IsComplete(WorkshopSeason.Autumn) &&
            (WorkshopProgress.SeasonalOrderEnabled || WinterLevelState.ProducedDyeColors.Count > 0) &&
            !WorkshopProgress.Has(WorkshopItem.DyedCloth));
    }

    private void Update()
    {
        if (WinterDyeingOverlay.IsActive) return;
        if (WorkshopQuestGuide.BlocksWorldInput || !Input.GetMouseButtonDown(0)) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null || _collider == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        if (!_collider.OverlapPoint(world)) return;

        TryOpen();
    }

    private void TryOpen()
    {
        if (WorkshopProgress.Has(WorkshopItem.CutPieces))
        {
            SimplePopup.Show("染色", "布料已经裁成衣片，请回工坊制衣。", "知道了", null);
            return;
        }
        if (!WorkshopProgress.IsComplete(WorkshopSeason.Autumn))
        {
            SimplePopup.Show("缺少素绸", "提交秋季任务后，可把做好的素绸带来染色。", "知道了", null);
            return;
        }
        if (!WorkshopProgress.SeasonalOrderEnabled && WinterLevelState.ProducedDyeColors.Count == 0)
        {
            SimplePopup.Show("染色", "还没有研磨好的染料，先去研磨染料吧。", "知道了", null);
            return;
        }

        WinterDyeingOverlay.Show();
    }
}
