using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Readable craft rewards and a paged collection of completed workshop games.</summary>
internal sealed class WorkshopRewardCardView : MonoBehaviour
{
    private const int PageSize = 6;
    private static WorkshopRewardCardView current;
    private static int closedFrame = -1;
    private RectTransform frame;
    private RectTransform page;
    private CanvasGroup visibility;
    private WorkshopSeason season;
    private int pageIndex;
    private Action onClose;
    private Action back;
    private GameObject previousSelection;
    private bool termOpen;
    private bool notified;
    private int resumedFrame = -1;
    private WorkshopItem? automaticItem;

    internal static bool IsOpen => current != null;
    internal static bool BlocksWorldInput => IsOpen || closedFrame == Time.frameCount;
    internal static bool HasTermOpen => current != null && current.termOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
        closedFrame = -1;
    }

    internal static void ShowReward(WorkshopRewardCards.Card card, Action closed)
    {
        if (card == null || !WorkshopRewardCards.AutomaticItemCard(card))
        {
            closed?.Invoke();
            return;
        }
        if (current != null)
        {
            if (current.automaticItem == card.Item)
                WorkshopRewardCards.ContinueAfterItemCard(card.Item, closed);
            else closed?.Invoke();
            return;
        }
        if (!WorkshopRewardCards.TryBeginItemCard(card.Item))
        {
            WorkshopRewardCards.ContinueAfterItemCard(card.Item, closed);
            return;
        }
        var view = Open(card.Season, closed);
        view.automaticItem = card.Item;
        view.RenderCard(card, true, view.Close);
    }

    internal static void ShowCollection(WorkshopSeason season, Action closed = null)
    {
        if (current != null) return;
        var view = Open(season, closed);
        view.RenderCollection();
    }

    private static WorkshopRewardCardView Open(WorkshopSeason season, Action closed)
    {
        var frame = WorkshopUi.NewCanvas("Workshop Craft Cards", 25000);
        WorkshopUi.Block(frame, new Color32(0, 0, 0, 170));
        current = frame.gameObject.AddComponent<WorkshopRewardCardView>();
        current.frame = frame;
        current.visibility = frame.GetComponentInParent<Canvas>().gameObject.AddComponent<CanvasGroup>();
        current.season = season;
        current.onClose = closed;
        current.previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return current;
    }

    private void StartPage(Action goBack)
    {
        back = goBack;
        if (page != null)
        {
            page.gameObject.SetActive(false);
            Destroy(page.gameObject);
        }
        page = WorkshopUi.Rect(frame, "Craft Card Page", new Rect(0, 0, 1920, 1080));
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void RenderCard(WorkshopRewardCards.Card card, bool received, Action goBack)
    {
        if (!received && (card.Id == "maze" || card.Id == "reeling" || card.Id == "thread"))
        {
            // These crafts are represented by their solar craft card. Keep the
            // collection underneath so closing the detail returns to this page.
            OpenTerm(WorkshopSeason.Summer, card.Id == "thread" ? 2 : 1);
            return;
        }
        StartPage(goBack);
        var art = WorkshopArt.ItemCard(card.Item);
        if (art != null)
        {
            WorkshopKnowledgeView.DrawItemCard(page, new Rect(658, 30, 604, 892), card.Item, art);
        }
        else
            DrawItemArt(page, card.Item, new Rect(658, 30, 604, 892), true);
        var done = ActionButton(page, "Accept Craft Card", new Rect(800, 943, 320, 76),
            "收下卡片", goBack, true, 31);
        WorkshopCraftRecordView.KeepFocusIn(page, done);
    }

    private void RenderCollection()
    {
        StartPage(Close);
        var board = WorkshopUi.PixelCard(page, "Craft Collection Board", new Rect(120, 50, 1680, 980),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        Label(board, "Collection Title", new Rect(60, 30, 790, 70), "工艺卡片图鉴", 46);
        int collected = 0;
        foreach (var definition in WorkshopRewardCards.Definitions)
            if (WorkshopRewardCards.Earned(definition)) collected++;
        Label(board, "Collection Progress", new Rect(980, 40, 355, 50),
            "已收录 " + collected + " / " + WorkshopRewardCards.Definitions.Length, 27,
            WorkshopUi.Green, TextAnchor.MiddleRight);
        var close = ActionButton(board, "Close Craft Collection", new Rect(1410, 35, 210, 64), "返回织录", Close, false, 29);
        for (int i = 0; i < 4; i++)
        {
            var tab = (WorkshopSeason)i;
            ActionButton(board, "Craft Season " + tab, new Rect(60 + i * 224, 123, 208, 58),
                WorkshopProgress.SeasonName(tab) + "季工艺", () => SelectSeason(tab), tab == season, 28);
        }
        Label(board, "Collection Hint", new Rect(980, 129, 640, 47), "完成小游戏后获得卡片；已收录卡片可点开回看。",
            24, WorkshopUi.Faded, TextAnchor.MiddleRight);
        WorkshopUi.Fill(board, "Collection Rule", new Rect(60, 199, 1560, 2), WorkshopUi.Bronze);

        var cards = new List<WorkshopRewardCards.Card>();
        foreach (var definition in WorkshopRewardCards.Definitions)
            if (definition.Season == season) cards.Add(definition);
        int pages = Mathf.Max(1, (cards.Count + PageSize - 1) / PageSize);
        pageIndex = Mathf.Clamp(pageIndex, 0, pages - 1);
        for (int i = 0; i < PageSize && pageIndex * PageSize + i < cards.Count; i++)
        {
            var card = cards[pageIndex * PageSize + i];
            DrawCollectionCard(board, card, new Rect(60 + i % 3 * 526, 226 + i / 3 * 286, 508, 264));
        }
        if (cards.Count == 0)
            Label(board, "Empty Collection", new Rect(340, 360, 1000, 170), "本季工艺卡片尚未开放。", 34,
                WorkshopUi.Faded, TextAnchor.MiddleCenter);
        var previous = ActionButton(board, "Previous Craft Page", new Rect(460, 855, 220, 61),
            "上一页", () => ChangePage(-1), false, 28);
        previous.interactable = pageIndex > 0;
        Label(board, "Craft Page Number", new Rect(700, 855, 280, 61),
            (pageIndex + 1) + " / " + pages, 29, WorkshopUi.Green, TextAnchor.MiddleCenter);
        var next = ActionButton(board, "Next Craft Page", new Rect(1000, 855, 220, 61),
            "下一页", () => ChangePage(1), false, 28);
        next.interactable = pageIndex + 1 < pages;
        WorkshopCraftRecordView.KeepFocusIn(page, close);
    }

    private void DrawCollectionCard(Transform parent, WorkshopRewardCards.Card card, Rect bounds)
    {
        bool earned = WorkshopRewardCards.Earned(card);
        var surface = WorkshopUi.PixelCard(parent, "Craft Entry " + card.Id, bounds,
            earned ? WorkshopUi.Cream : new Color32(226, 222, 208, 255), WorkshopUi.Line);
        DrawItemArt(surface, card.Item, new Rect(18, 22, 140, 194), earned);
        var title = Label(surface, "Craft Entry Title", new Rect(175, 24, 310, 70), card.Title,
            29, earned ? WorkshopUi.Ink : WorkshopUi.Faded);
        Fit(title, 24, 29);
        var description = Label(surface, "Craft Entry Description", new Rect(175, 101, 310, 76),
            earned ? card.Description : "完成对应小游戏后收录工艺知识与节气卡片。", 23, WorkshopUi.Faded, TextAnchor.UpperLeft);
        Fit(description, 20, 23);
        Label(surface, "Craft Entry Status", new Rect(175, 195, 310, 43),
            earned ? "已收录 · 点击回看" : "尚未收录", 25, earned ? WorkshopUi.Green : WorkshopUi.Faded);
        var open = WorkshopUi.Button(surface, "Open Craft " + card.Id, new Rect(0, 0, bounds.width, bounds.height),
            "", () => RenderCard(card, false, RenderCollection), Color.clear, earned);
        open.interactable = earned;
    }

    private static void DrawItemArt(Transform parent, WorkshopItem item, Rect bounds, bool earned)
    {
        if (earned && WorkshopArt.ItemCard(item) != null)
        {
            WorkshopKnowledgeView.DrawItemCard(parent, bounds, item, WorkshopArt.ItemCard(item));
            return;
        }
        var icon = WorkshopUi.Rect(parent, "Craft Material Icon", bounds).gameObject.AddComponent<RawImage>();
        icon.texture = WorkshopArt.TaskIconAtlas;
        icon.uvRect = WorkshopArt.TaskIconUV(item);
        icon.color = earned ? Color.white : new Color32(105, 112, 99, 98);
        icon.raycastTarget = false;
        icon.enabled = icon.texture != null;
    }

    private void OpenTerm(WorkshopSeason selected, int term)
    {
        if (termOpen || !WorkshopRewardCards.HasTerm(selected, term)) return;
        termOpen = true;
        var selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        visibility.alpha = 0;
        visibility.interactable = false;
        visibility.blocksRaycasts = false;
        WorkshopKnowledgeView.ShowTerm(selected, term, () =>
        {
            if (current != this) return;
            termOpen = false;
            resumedFrame = Time.frameCount;
            visibility.alpha = 1;
            visibility.interactable = true;
            visibility.blocksRaycasts = true;
            if (EventSystem.current != null && selection != null && selection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(selection);
        });
    }

    private static List<int> ValidTerms(int[] terms)
    {
        var result = new List<int>();
        if (terms != null)
            foreach (int term in terms)
                if (term >= 0 && term < 6 && !result.Contains(term)) result.Add(term);
        return result;
    }

    private void SelectSeason(WorkshopSeason selected)
    {
        season = selected;
        pageIndex = 0;
        RenderCollection();
    }

    private void ChangePage(int delta)
    {
        pageIndex += delta;
        RenderCollection();
    }

    private void Update()
    {
        if (current != this || termOpen || resumedFrame == Time.frameCount) return;
        if (Input.GetKeyDown(KeyCode.Escape)) back?.Invoke();
    }

    private void Close()
    {
        if (current != this || termOpen) return;
        if (automaticItem.HasValue)
        {
            var item = automaticItem.Value;
            automaticItem = null;
            WorkshopRewardCards.FinishItemCard(item, CloseAccepted);
            return;
        }
        CloseAccepted();
    }

    private void CloseAccepted()
    {
        current = null;
        closedFrame = Time.frameCount;
        WorkshopUi.CloseCanvas(frame);
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        NotifyClosed();
    }

    private void OnDestroy()
    {
        if (automaticItem.HasValue) WorkshopRewardCards.ReleaseItemCard(automaticItem.Value);
        automaticItem = null;
        if (current == this) current = null;
        NotifyClosed();
    }

    private void NotifyClosed()
    {
        if (notified) return;
        notified = true;
        onClose?.Invoke();
    }

    private static Text Label(Transform parent, string name, Rect bounds, string value, int size,
        Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var label = WorkshopUi.Label(parent, name, bounds, value, size, color ?? WorkshopUi.Ink,
            alignment, WorkshopArt.ReadingFont);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    private static void Fit(Text text, int min, int max)
    {
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = min;
        text.resizeTextMaxSize = max;
    }

    private static Button ActionButton(Transform parent, string name, Rect bounds, string value,
        Action clicked, bool primary, int size)
    {
        var button = WorkshopUi.Button(parent, name, bounds, value, clicked,
            primary ? WorkshopUi.Green : new Color32(232, 220, 193, 255), size: size, font: WorkshopArt.ReadingFont);
        return button;
    }
}
