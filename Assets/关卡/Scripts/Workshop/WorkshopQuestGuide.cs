using System.Collections;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class WorkshopQuestGuide : MonoBehaviour
{
    private static readonly Vector2 CardCenter = new Vector2(960, -540);
    private static readonly Rect NoticeButtonBounds = new Rect(1714, 36, 170, 58);
    private static readonly Vector2 NoticeButtonCenter = new Vector2(NoticeButtonBounds.center.x, -NoticeButtonBounds.center.y);
    private static readonly Color Shade = WorkshopNoticeBoard.ModalShade;
    private static readonly Color MissingTint = new Color(0.76f, 0.73f, 0.68f, 0.70f);

    private static WorkshopQuestGuide current;
    private static int lastDismissalFrame = -1;
    internal static bool IsOpen => current != null && current.card != null && current.card.gameObject.activeSelf;
    internal static bool BlocksWorldInput => WorkshopMinigameMenu.BlocksGameplayInput || IsOpen || lastDismissalFrame == Time.frameCount ||
        WorkshopDialoguePresenter.BlocksWorldInput || MiniGameCompletionAnimation.IsPlaying ||
        AutumnOfferingView.BlocksWorldInput || WorkshopObservationView.BlocksWorldInput ||
        WorkshopCraftRecordView.BlocksWorldInput || WorkshopHeritageBrief.IsOpen || SpringSilkwormWashOverlay.IsOpen;

    internal static bool IsWorldOverlayActive
    {
        get
        {
            return MiniGameCompletionAnimation.IsPlaying || MiniGameOverlay.IsActive || HarvestOverlayManager.IsHarvestActive ||
                WinterDyeingOverlay.IsActive || WinterGrindingOverlay.IsActive || SpringSilkwormWashOverlay.IsOpen ||
                WorkshopKnowledgeView.IsOpen ||
                WorkshopNarrativeView.IsOpen || SimplePopup.IsOpen || WorkshopDialoguePresenter.IsOpen ||
                AutumnOfferingView.IsOpen || WorkshopObservationView.IsOpen ||
                WorkshopCraftRecordView.IsOpen || WorkshopHeritageBrief.IsOpen;
        }
    }

    private RectTransform card;
    private RectTransform itemHud;
    private GameObject noticeEntry;
    private UnityEngine.UI.Image shield;
    private CanvasGroup cardGroup;
    private UnityEngine.UI.Button acknowledge;
    private GoalItem[] goalItems;
    private WorkshopSeason season;
    private bool needsOpening;
    private bool collapsing;

    private sealed class GoalItem
    {
        internal WorkshopItem Item;
        internal UnityEngine.UI.RawImage Icon;
        internal GameObject CollectedMark;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        current = null;
        lastDismissalFrame = -1;
    }

    internal static void Attach(WorkshopSeason season)
    {
        var frame = WorkshopUi.NewCanvas("Level Quest Guide", 510);
        current = frame.gameObject.AddComponent<WorkshopQuestGuide>();
        current.Build(frame, season);
    }

    private string ReadStep => "quest.guide." + season;

    private void Build(RectTransform frame, WorkshopSeason level)
    {
        season = level;
        needsOpening = !WorkshopProgress.IsComplete(season) && !WorkshopProgress.HasStep(ReadStep);

        shield = WorkshopUi.Fill(frame.parent, "Quest Modal Shield", new Rect(), Shade);
        var mask = shield.rectTransform;
        mask.anchorMin = Vector2.zero;
        mask.anchorMax = Vector2.one;
        mask.offsetMin = mask.offsetMax = Vector2.zero;
        mask.SetAsFirstSibling();
        shield.raycastTarget = true;
        shield.gameObject.SetActive(false);

        BuildItemHud(frame);
        var boardButton = WorkshopUi.PixelButton(frame, "Notice Board Entry", NoticeButtonBounds,
            "公告栏", OpenCard, primary: true, size: 29);
        noticeEntry = boardButton.transform.parent.gameObject;
        BuildOpeningCard(frame);
        card.gameObject.SetActive(false);
        RefreshProgress();
        WorkshopProgress.Changed += RefreshProgress;
        if (needsOpening && !IsWorldOverlayActive) OpenCard();
        SetHudVisible(!IsOpen && !IsWorldOverlayActive);
    }

    private void BuildItemHud(RectTransform frame)
    {
        const float width = 124f;
        const float height = 110f;
        const float gap = 9f;
        var items = WorkshopProgress.Items(season);
        itemHud = WorkshopUi.Rect(frame, "Season Goal Items",
            new Rect(36f, 36f, items.Length * (width + gap) - gap, height));
        goalItems = new GoalItem[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            WorkshopItem item = items[i];
            var tile = WorkshopUi.PixelCard(itemHud, item.ToString(), new Rect(i * (width + gap), 0, width, height),
                WorkshopUi.Paper, WorkshopUi.Bronze);
            var icon = WorkshopUi.Rect(tile, "Item Icon", new Rect(28, 4, 68, 68))
                .gameObject.AddComponent<UnityEngine.UI.RawImage>();
            icon.texture = WorkshopArt.TaskIconAtlas;
            icon.uvRect = WorkshopArt.TaskIconUV(item);
            icon.raycastTarget = false;
            icon.enabled = icon.texture != null;
            WorkshopUi.Label(tile, "Item Name", new Rect(5, 73, width - 10, 29), WorkshopProgress.ItemName(item),
                26, WorkshopUi.Ink, TextAnchor.MiddleCenter);

            var check = WorkshopUi.PixelCard(tile, "Collected Mark", new Rect(94, 8, 22, 22),
                WorkshopUi.Green, WorkshopUi.Bronze);
            var checkStart = WorkshopUi.Fill(check, "Check Start", new Rect(6, 11, 6, 3), WorkshopUi.Cream);
            checkStart.rectTransform.localEulerAngles = new Vector3(0, 0, -45);
            var checkEnd = WorkshopUi.Fill(check, "Check End", new Rect(9, 15, 11, 3), WorkshopUi.Cream);
            checkEnd.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            goalItems[i] = new GoalItem { Item = item, Icon = icon, CollectedMark = check.gameObject };
        }
    }

    private void BuildOpeningCard(RectTransform frame)
    {
        card = WorkshopUi.Rect(frame, "Quest Opening Card", new Rect(0, 0, 1920, 1080));
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = CardCenter;
        cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        var noticeBoard = WorkshopNoticeBoard.Build(card, season, DismissCard);
        acknowledge = noticeBoard.CloseAction;
    }

    private void RefreshProgress()
    {
        foreach (var goal in goalItems)
        {
            bool collected = WorkshopProgress.Has(goal.Item);
            goal.Icon.color = collected ? Color.white : MissingTint;
            goal.CollectedMark.SetActive(collected);
        }
    }

    private void SetHudVisible(bool visible)
    {
        if (itemHud.gameObject.activeSelf != visible) itemHud.gameObject.SetActive(visible);
        if (noticeEntry.activeSelf != visible) noticeEntry.SetActive(visible);
    }

    private void Update()
    {
        bool blocked = IsWorldOverlayActive;
        if (needsOpening && !blocked && !IsOpen) OpenCard();
        SetHudVisible(!IsOpen && !blocked);
        if (IsOpen && !collapsing && !blocked && Input.GetKeyDown(KeyCode.Escape)) DismissCard();
    }

    private void OpenCard()
    {
        if (card.gameObject.activeSelf || IsWorldOverlayActive) return;
        card.anchoredPosition = CardCenter;
        card.localScale = Vector3.one;
        cardGroup.alpha = 1f;
        cardGroup.interactable = true;
        card.gameObject.SetActive(true);
        shield.gameObject.SetActive(true);
        shield.color = Shade;
        SetHudVisible(false);
        acknowledge.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(acknowledge.gameObject);
    }

    private void DismissCard()
    {
        if (!card.gameObject.activeSelf || collapsing) return;
        needsOpening = false;
        WorkshopProgress.MarkStep(ReadStep);
        collapsing = true;
        cardGroup.interactable = false;
        acknowledge.interactable = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(Collapse());
    }

    private IEnumerator Collapse()
    {
        const float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            card.anchoredPosition = Vector2.Lerp(CardCenter, NoticeButtonCenter, blend);
            card.localScale = Vector3.one * Mathf.Lerp(1f, 0.17f, blend);
            cardGroup.alpha = 1f - blend;
            shield.color = new Color(Shade.r, Shade.g, Shade.b, Shade.a * (1f - blend));
            yield return null;
        }

        card.gameObject.SetActive(false);
        shield.gameObject.SetActive(false);
        lastDismissalFrame = Time.frameCount;
        collapsing = false;
        SetHudVisible(!IsWorldOverlayActive);
    }

    private void OnDestroy()
    {
        WorkshopProgress.Changed -= RefreshProgress;
        if (current == this) current = null;
    }
}
