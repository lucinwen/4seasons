using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class WorkshopKnowledgeView : MonoBehaviour
{
    private static readonly Color Jade = WorkshopUi.Green;
    private static readonly Color Cream = WorkshopUi.Cream;
    private static readonly Rect AlmanacBounds = new Rect(160, 58, 1600, 900);
    private static readonly float[] SolarColumns = { 524, 663, 801, 939, 1077, 1216 };
    private static readonly float[] SolarRows = { 245, 382, 519, 656 };

    private static WorkshopKnowledgeView current;
    private static int closedFrame = -1;
    private RectTransform frame;
    private RectTransform page;
    private WorkshopSeason season;
    private Action escape;
    private Action onClose;
    private WorkshopKnowledgeView previousView;
    private GameObject previousSelection;
    private int sortingOrder;
    private int resumedFrame = -1;

    public static bool IsOpen => current != null;
    internal static bool BlocksWorldInput => IsOpen || closedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
        closedFrame = -1;
    }

    public static void Show(WorkshopSeason season, bool justReceived = false)
    {
        var view = Open(season);
        view.RenderSolarGallery(view.Close, justReceived);
    }

    public static void ShowJournal(WorkshopSeason season)
    {
        var view = Open(season);
        view.RenderItems();
    }

    internal static void ShowTerm(WorkshopSeason season, int index, Action onClose = null)
    {
        var art = WorkshopProgress.HasSolarTerms(season) ? WorkshopArt.SolarCard(season, index) : null;
        if (art == null)
        {
            onClose?.Invoke();
            return;
        }

        var view = Open(season, WorkshopCraftRecordView.CanvasOrder + 100, preserveExisting: true);
        view.onClose = onClose;
        view.RenderSolarDetail(art, season, index, WorkshopArt.SolarNames(season)[index], view.Close);
    }

    public static void ShowItem(WorkshopItem item)
    {
        var art = WorkshopArt.ItemCard(item);
        if (art == null) return;
        var view = Open((WorkshopSeason)((int)item / 3));
        view.RenderDetail(art, view.Close, "收下卡片");
    }

    public static void ShowCard(Sprite art)
    {
        if (art == null) return;
        var view = Open(WorkshopSeason.Spring);
        view.RenderDetail(art, view.Close, "关闭");
    }

    private static WorkshopKnowledgeView Open(WorkshopSeason season, int order = 12000, bool preserveExisting = false)
    {
        if (!preserveExisting && current != null) current.Close();
        var previous = preserveExisting ? current : null;
        var selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var frame = WorkshopUi.NewCanvas("Workshop Knowledge", order);
        WorkshopUi.Block(frame, WorkshopUi.Veil);
        current = frame.gameObject.AddComponent<WorkshopKnowledgeView>();
        current.frame = frame;
        current.season = season;
        current.sortingOrder = order;
        current.previousView = previous;
        current.previousSelection = selection;
        return current;
    }

    private void Update()
    {
        if (current != this || resumedFrame == Time.frameCount || WorkshopObservationView.BlocksWorldInput ||
            WorkshopDialoguePresenter.BlocksWorldInput ||
            WorkshopHeritageBrief.IsOpen || WorkshopChapterGuide.IsOpen ||
            (sortingOrder < WorkshopCraftRecordView.CanvasOrder && WorkshopCraftRecordView.BlocksWorldInput)) return;
        if (Input.GetKeyDown(KeyCode.Escape)) escape?.Invoke();
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private void Close()
    {
        if (current != this) return;
        closedFrame = Time.frameCount;
        current = previousView;
        if (previousView != null) previousView.resumedFrame = Time.frameCount;
        WorkshopUi.CloseCanvas(frame);
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        onClose?.Invoke();
    }

    private void StartPage(Action goBack)
    {
        escape = goBack;
        if (page != null)
        {
            page.gameObject.SetActive(false);
            Destroy(page.gameObject);
        }

        page = WorkshopUi.Rect(frame, "Page", new Rect(0, 0, 1920, 1080));
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void RenderItems()
    {
        StartPage(Close);
        var journal = WorkshopUi.Panel(page, "Journal Frame", new Rect(76, 17, 1768, 1046),
            WorkshopArt.JournalFrame ?? WorkshopArt.WorkshopTaskBoard).rectTransform;
        Label(journal, "Title", new Rect(589, 112, 631, 89), "四时织录", 54,
            Jade, TextAnchor.MiddleCenter);
        Label(journal, "Item Hint", new Rect(291, 288, 876, 49),
            "获得关键道具后可点击卡片阅读。", 25, WorkshopUi.Faded);
        var back = WorkshopUi.PixelButton(journal, "Close", new Rect(1337, 283, 160, 56),
            "返回", Close, false, 28);

        for (int i = 0; i < 4; i++)
        {
            var tab = (WorkshopSeason)i;
            WorkshopUi.PixelButton(journal, "Season " + tab, new Rect(291 + i * 162, 352, 146, 54),
                WorkshopProgress.SeasonName(tab) + "季", () => SelectSeason(tab),
                tab == season, 27);
        }
        WorkshopUi.PixelButton(journal, "Solar Cards", new Rect(1255, 352, 242, 54), "二十四节气",
            () => RenderSolarGallery(RenderItems), true, 28, WorkshopUi.Green);
        WorkshopUi.PixelButton(journal, "Observation Records", new Rect(988, 352, 243, 54), "四季观察",
            RenderObservationGallery, true, 28, WorkshopUi.Green);
        WorkshopUi.Fill(journal, "Rule", new Rect(291, 422, 1206, 2), WorkshopUi.Line);

        var items = WorkshopProgress.Items(season);
        for (int i = 0; i < items.Length; i++)
            RenderItem(journal, items[i], new Rect(293 + i * 402, 434, 378, 428));
        WorkshopCraftRecordView.KeepFocusIn(page, back);
    }

    private void SelectSeason(WorkshopSeason tab)
    {
        season = tab;
        RenderItems();
    }

    private void RenderItem(Transform parent, WorkshopItem item, Rect rect)
    {
        var sprite = WorkshopArt.ItemCard(item);
        string title = WorkshopProgress.ItemName(item);
        bool acquired = WorkshopProgress.Has(item) && sprite != null;
        var surface = WorkshopUi.Button(parent, "Item Card " + item, rect, "",
            () => RenderDetail(sprite, RenderItems, "返回图鉴"), Color.clear, acquired).transform;
        if (acquired)
            DrawItemCard(surface, new Rect(14, 7, 350, 374), item, sprite);
        else
        {
            WorkshopUi.Panel(surface, "Uncollected Card Frame", new Rect(48, 61, 282, 306),
                WorkshopArt.SeasonPanel(season));
            var icon = WorkshopUi.Rect(surface, "Locked Material Art", new Rect(134, 104, 110, 110))
                .gameObject.AddComponent<UnityEngine.UI.RawImage>();
            icon.texture = WorkshopArt.TaskIconAtlas;
            icon.uvRect = WorkshopArt.TaskIconUV(item);
            icon.color = new Color(0.38f, 0.41f, 0.35f, 0.46f);
            icon.raycastTarget = false;
            icon.enabled = icon.texture != null;
            Label(surface, "Locked Item Name", new Rect(56, 222, 266, 58), title, 36,
                WorkshopUi.Ink, TextAnchor.MiddleCenter);
            Label(surface, "Locked Item Status", new Rect(56, 283, 266, 45), "尚未收录", 25,
                WorkshopUi.Faded, TextAnchor.MiddleCenter);
        }
    }

    private void RenderSolarGallery(Action goBack, bool justReceived = false)
    {
        StartPage(goBack);
        var ledger = Resources.Load<Sprite>("WorkshopDelivery/P09");
        if (ledger != null)
        {
            WorkshopUi.Picture(page, "Solar Almanac Art", AlmanacBounds, ledger, stretch: true);
            DrawSolarAlmanacLabels(page, AlmanacBounds, ledger.texture);
        }
        else
        {
            WorkshopUi.Panel(page, "Solar Almanac Art", AlmanacBounds, WorkshopArt.WorkshopTaskBoard);
            Label(page, "Solar Title", new Rect(580, 112, 760, 76), "二十四节气", 58, Jade, TextAnchor.MiddleCenter);
        }

        for (int row = 0; row < SolarRows.Length; row++)
        {
            var currentSeason = (WorkshopSeason)row;
            var names = WorkshopArt.SolarNames(currentSeason);
            bool acquired = WorkshopProgress.HasSolarTerms(currentSeason);
            if (ledger == null)
                Label(page, "Solar Season", FromAlmanac(345, SolarRows[row], 150, 122),
                    WorkshopProgress.SeasonName(currentSeason) + "季", 38, Jade, TextAnchor.MiddleCenter);
            for (int i = 0; i < names.Length; i++)
                RenderTerm(currentSeason, i, names[i], acquired, goBack);
        }

        var back = WorkshopUi.Button(page, "Solar Back", FromAlmanac(1152, 809, 272, 92),
            "", goBack, ledger != null ? new Color(0f, 0f, 0f, 0f) : Jade);
        if (ledger == null)
            Label(back.transform, "Solar Back Label", new Rect(0, 0, 260, 82),
                "返回", 32, Cream, TextAnchor.MiddleCenter);
        else
            MarkPrintedButtonFocus(back);
        string guide = justReceived
            ? "已收录" + WorkshopProgress.SeasonName(season) + "季节气卡。"
            : "向各季师傅领取节气卡。";
        Label(page, "Solar Guide", new Rect(210, 966, 1500, 54), guide, 29, Cream, TextAnchor.MiddleCenter);
        WorkshopCraftRecordView.KeepFocusIn(page, back);
    }

    private void RenderTerm(WorkshopSeason currentSeason, int index, string name, bool acquired, Action goBack)
    {
        Sprite art = acquired ? WorkshopArt.SolarCard(currentSeason, index) : null;
        var rect = FromAlmanac(SolarColumns[index] + 7, SolarRows[(int)currentSeason] + 15, 86, 96);
        if (art == null)
        {
            Label(page, "Locked " + currentSeason + " " + index, rect, name, 27,
                WorkshopUi.Faded, TextAnchor.MiddleCenter);
            return;
        }

        var button = WorkshopUi.Button(page, "Solar Card " + currentSeason + " " + index,
            rect, "", () => RenderSolarDetail(art, currentSeason, index, name, () => RenderSolarGallery(goBack)),
            new Color32(46, 95, 74, 246));
        Label(button.transform, "Solar Name", new Rect(1, 2, rect.width - 2, rect.height - 5),
            name, 28, Cream, TextAnchor.MiddleCenter);
    }

    private static Rect FromAlmanac(float x, float y, float width, float height) =>
        new Rect(AlmanacBounds.x + x * (AlmanacBounds.width / 1672f),
            AlmanacBounds.y + y * (AlmanacBounds.height / 941f),
            width * (AlmanacBounds.width / 1672f), height * (AlmanacBounds.height / 941f));

    private void RenderDetail(Sprite artwork, Action goBack, string label)
    {
        StartPage(goBack);
        bool landscape = artwork.rect.width >= artwork.rect.height;
        var bounds = landscape ? new Rect(195, 74, 1530, 862) : new Rect(658, 30, 604, 892);
        if (WorkshopArt.TryIdentifyItemCard(artwork, out var item))
            DrawItemCard(page, bounds, item, artwork);
        else
            CenterArt(page, "Full Size Card", bounds, artwork);
        Button(page, "Close Detail", new Rect(811, 943, 298, 69), label, goBack);
    }

    private static void CenterArt(Transform parent, string name, Rect rect, Sprite art)
    {
        var image = WorkshopUi.Picture(parent, name, rect, art);
        image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        image.rectTransform.anchoredPosition = new Vector2(rect.center.x, -rect.center.y);
    }

    private static void MarkPrintedButtonFocus(UnityEngine.UI.Button button)
    {
        Rect bounds = ((RectTransform)button.transform).rect;
        button.targetGraphic = WorkshopUi.Fill(button.transform, "Keyboard Focus Line",
            new Rect(bounds.width * 0.18f, bounds.height * 0.82f, bounds.width * 0.64f, 4f), WorkshopUi.Bronze);
        var colors = button.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = colors.selectedColor = Color.white;
        button.colors = colors;
    }

    private static UnityEngine.UI.Text Label(Transform parent, string name, Rect rect, string text,
        int size, Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var label = WorkshopUi.Label(parent, name, rect, text, size, color, alignment, WorkshopArt.WorkshopFont);
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, Rect rect, string text,
        Action click, Color? color = null) =>
        WorkshopUi.Button(parent, name, rect, text, click, color ?? Jade, size: 29, font: WorkshopArt.WorkshopFont);
}
