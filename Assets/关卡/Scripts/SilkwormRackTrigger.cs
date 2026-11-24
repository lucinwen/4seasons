using UnityEngine;
using Qingming.LevelBackground;

/// <summary>Opens the shared leaf-preparation and feeding station at the designated spring rack.</summary>
[RequireComponent(typeof(Collider2D))]
public class SilkwormRackTrigger : MonoBehaviour
{
    public bool guyuStation;
    public Vector3 buttonOffset = new Vector3(0f, 2.4f, 0f);
    [Range(0f, 1f)] public float buttonOpacity = 0.9f;

    private FloatingActionButton station;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (!guyuStation) return;
        station = gameObject.AddComponent<FloatingActionButton>();
        station.Configure(StationLabel(), buttonOffset, OnRackClicked);
        station.SetOpacity(buttonOpacity);
        WorkshopPickupGlow.Bind(gameObject, () => SpringSummerNpcDialogue.HasHarvestedLeaves
            && !WorkshopProgress.Has(WorkshopItem.Silkworm));
    }

    private static string StationLabel()
    {
        string title = WorkshopObservationView.SeasonGameTitle(WorkshopObservation.GuyuLeaf);
        return WorkshopObservationView.HasRecord(WorkshopObservation.GuyuLeaf) ? title + " · 已收残页" : title;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (station == null || !IsPlayer(other) || HarvestOverlayManager.IsHarvestActive) return;
        station.SetText(StationLabel());
        station.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (station != null && IsPlayer(other)) station.Hide();
    }

    private void OnRackClicked()
    {
        if (station == null || WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive
            || !SpringSummerNpcDialogue.RequireMulberryBriefing()) return;
        if (!SpringSummerNpcDialogue.HasHarvestedLeaves)
        {
            SimplePopup.Show("先到桑园试采", "带篮到桑树前完成清明采桑，记下适合取嫩叶的桑垄。谷雨当天再去同垄采新鲜桑叶。", "知道了", null);
            return;
        }

        WorkshopObservationView.Show(WorkshopObservation.GuyuLeaf, () => station.SetText(StationLabel()));
    }

    private static bool IsPlayer(Collider2D other) =>
        other.CompareTag("Player") || other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
}
