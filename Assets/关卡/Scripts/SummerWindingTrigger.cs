using UnityEngine;
using Qingming.LevelBackground;

/// <summary>Use the existing reeling wheel's trigger to open the silk connection puzzle.</summary>
[RequireComponent(typeof(Collider2D))]
public sealed class SummerWindingTrigger : MonoBehaviour
{
    private FloatingActionButton action;
    private bool playerInRange;

    private void Start()
    {
        action = gameObject.AddComponent<FloatingActionButton>();
        action.Configure("绕丝上轴", new Vector3(0f, 2.6f, 0f), OpenWinding);
        action.SetOpacity(0.9f);
        WorkshopPickupGlow.Bind(gameObject, () => WorkshopProgress.Has(WorkshopItem.Spool) &&
            WorkshopProgress.HasStep(WorkshopWordRules.Reeled) &&
            !WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon));
    }

    private void Update()
    {
        if (action == null) return;
        bool visible = playerInRange && !WorkshopQuestGuide.BlocksWorldInput &&
            !WorkshopQuestGuide.IsWorldOverlayActive && !WorkshopChapterGuide.IsOpen;
        if (visible && !action.IsVisible) action.Show();
        else if (!visible && action.IsVisible) action.Hide();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other)) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other)) playerInRange = false;
    }

    private void OpenWinding()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive) return;
        action.Hide();
        if (WorkshopProgress.Has(WorkshopItem.Spool) &&
            WorkshopProgress.HasStep(WorkshopWordRules.Reeled))
            WorkshopProcessCardView.Show(WorkshopProcessCard.Mangzhong,
                () => WorkshopObservationView.Show(WorkshopObservation.MangzhongCocoon));
        else
            WorkshopObservationView.Show(WorkshopObservation.MangzhongCocoon);
    }

    private static bool IsPlayer(Collider2D other)
    {
        var body = other.attachedRigidbody;
        var owner = body != null ? body.gameObject : other.gameObject;
        return owner.CompareTag("Player") ||
            owner.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase) ||
            other.transform.root.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
    }
}
