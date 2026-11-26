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
        WorkshopPickupGlow.Bind(gameObject, () => WorkshopProgress.Has(WorkshopItem.RawSilkCloth) &&
            WinterLevelState.ProducedDyeColors.Count > 0 &&
            !WorkshopProgress.Has(WorkshopItem.DyedCloth));
    }

    private void Update()
    {
        if (WinterDyeingOverlay.IsActive) return;
        bool pressed = Input.GetMouseButtonDown(0);
        if (WorkshopQuestGuide.BlocksWorldInput || !pressed) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null || _collider == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        if (!_collider.OverlapPoint(world)) return;

        if (!pressed) return;

        TryOpen();
    }

    private void TryOpen()
    {
        if (WorkshopProgress.Has(WorkshopItem.CutPieces))
        {
            SimplePopup.Show("染色", "布料已经裁成衣片，请回工坊制衣。", "知道了", null);
            return;
        }
        if (!WorkshopProgress.Has(WorkshopItem.RawSilkCloth))
        {
            SimplePopup.Show("缺少素绸", "先在秋季织成生坯绸，再来染色。", "知道了", null);
            return;
        }
        if (WinterLevelState.ProducedDyeColors.Count == 0)
        {
            SimplePopup.Show("染料未研磨", "点击研钵研磨染料。", "知道了", null);
            return;
        }

        WinterDyeingOverlay.Show();
    }
}
