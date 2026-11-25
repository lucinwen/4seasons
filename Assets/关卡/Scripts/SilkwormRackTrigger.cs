using Qingming.LevelBackground;
using UnityEngine;

/// <summary>Starts the feeding minigame at the central silkworm rack.</summary>
[RequireComponent(typeof(Collider2D))]
public class SilkwormRackTrigger : MonoBehaviour
{
    [SerializeField] private Vector3 buttonOffset = new Vector3(0f, 2.4f, 0f);
    [SerializeField, Range(0f, 1f)] private float buttonOpacity = 0.9f;

    private FloatingActionButton station;
    private bool playerNearby;

    private void Awake()
    {
        station = gameObject.AddComponent<FloatingActionButton>();
        station.Configure("喂蚕宝宝 · O", buttonOffset, OnRackClicked, FloatingActionButton.VisualStyle.PaperTag);
        station.SetOpacity(buttonOpacity);
        WorkshopPickupGlow.Bind(gameObject, () => SpringSummerNpcDialogue.HasHarvestedLeaves &&
            !WorkshopProgress.Has(WorkshopItem.Silkworm));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other)) playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other)) playerNearby = false;
    }

    private void Update()
    {
        bool canInteract = playerNearby && !WorkshopQuestGuide.BlocksWorldInput &&
            !WorkshopQuestGuide.IsWorldOverlayActive;
        if (canInteract != station.IsVisible)
        {
            if (canInteract) station.Show();
            else station.Hide();
        }

        if (canInteract && Input.GetKeyDown(KeyCode.O)) OnRackClicked();
    }

    private void OnDisable()
    {
        playerNearby = false;
        station?.Hide();
    }

    private void OnRackClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive) return;
        if (!SpringSummerNpcDialogue.HasHarvestedLeaves)
        {
            SimplePopup.Show("需要桑叶", "先去桑树前采摘桑叶，再回来喂蚕宝宝。", "知道了");
            return;
        }

        FeedOverlayManager.Instance?.ShowFeed();
    }

    private static bool IsPlayer(Collider2D other) =>
        other.CompareTag("Player") || other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
}
