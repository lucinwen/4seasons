using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class WorkshopKnowledgeView
{
    private const string ProcessCardPath = "WorkshopProcessCards/";
    private static readonly Color ProcessCardVeil = new Color32(14, 25, 22, 244);
    private static readonly Rect ProcessCardBounds = new Rect(654, 30, 612, 1020);
    private static readonly Rect ProcessOverviewBounds = new Rect(420, 36, 1080, 1008);

    [Serializable]
    private sealed class ProcessCardAlbum
    {
        public ProcessCard[] cards = Array.Empty<ProcessCard>();
    }

    [Serializable]
    private sealed class ProcessCard
    {
        public int season = 0;
    }

    private ProcessCard[] processCards;
    private Action<int> flipProcessCard;
    private UnityEngine.UI.Selectable processKeyboardEntry;

    private void RenderProcessCardGallery(WorkshopSeason chapter)
    {
        if (processCards == null)
        {
            var source = Resources.Load<TextAsset>(ProcessCardPath + "Cards");
            var album = source != null ? JsonUtility.FromJson<ProcessCardAlbum>(source.text) : null;
            processCards = album?.cards ?? Array.Empty<ProcessCard>();
        }

        int first = Array.FindIndex(processCards, card => card.season == (int)chapter);
        RenderProcessCard(processCards.Length == 0 ? -1 : Mathf.Max(0, first));
    }

    private void RenderProcessCard(int index)
    {
        StartPage(RenderItems, ProcessCardVeil);
        var gallery = WorkshopUi.Rect(page, "Process Card Gallery", new Rect(0, 0, 1920, 1080));
        var artwork = index >= 0 ? Resources.Load<Sprite>(ProcessCardPath + "Card" + ProcessCardNumber(index)) : null;
        WorkshopUi.Picture(gallery, "Card Artwork", ProcessCardBounds, artwork);
        if (artwork == null)
            WorkshopUi.Label(gallery, "Missing Card Artwork", ProcessCardBounds, "工序卡暂不可用", 28, Cream,
                TextAnchor.MiddleCenter);

        ProcessControl(gallery, "Back", new Rect(42, 28, 108, 56), "返回", 22, RenderItems);
        processKeyboardEntry = ProcessControl(gallery, "Overview", new Rect(1738, 28, 140, 56), "四季总览", 22,
            () => RenderProcessOverview(index));

        if (processCards.Length > 1)
        {
            void Flip(int direction) =>
                RenderProcessCard((index + direction + processCards.Length) % processCards.Length);

            flipProcessCard = Flip;
            ProcessControl(gallery, "Previous Card", new Rect(561, 504, 76, 72), "‹", 42, () => Flip(-1));
            ProcessControl(gallery, "Next Card", new Rect(1283, 504, 76, 72), "›", 42, () => Flip(1));
        }

        WorkshopCraftRecordView.KeepFocusIn(page, null);
    }

    private void RenderProcessOverview(int index)
    {
        void ReturnToCard() => RenderProcessCard(index);

        StartPage(ReturnToCard, ProcessCardVeil);
        var overview = WorkshopUi.Rect(page, "Process Overview", new Rect(0, 0, 1920, 1080));
        var artwork = Resources.Load<Sprite>(ProcessCardPath + "Overview");
        WorkshopUi.Picture(overview, "Overview Artwork", ProcessOverviewBounds, artwork);
        if (artwork != null)
            DrawOverviewLinks(overview);
        else
            WorkshopUi.Label(overview, "Missing Overview Artwork", ProcessOverviewBounds, "四季总览暂不可用", 28, Cream,
                TextAnchor.MiddleCenter);

        processKeyboardEntry = ProcessControl(overview, "Back", new Rect(42, 28, 108, 56), "返回", 22, ReturnToCard);
        WorkshopCraftRecordView.KeepFocusIn(page, null);
    }

    private void DrawOverviewLinks(Transform overview)
    {
        float scale = ProcessOverviewBounds.width / 1500f;
        var columns = new int[4];
        for (int i = 0; i < processCards.Length; i++)
        {
            int index = i;
            int row = processCards[index].season;
            int column = columns[row]++;
            // The original overview artwork has one row per season.
            var bounds = new Rect(ProcessOverviewBounds.x + (14 + column * 210) * scale,
                ProcessOverviewBounds.y + (2 + row * 350) * scale, 206 * scale, 346 * scale);
            var button = WorkshopUi.Button(overview, "Process Card " + ProcessCardNumber(index), bounds, "",
                () => RenderProcessCard(index), Color.clear);
            MarkPrintedButtonFocus(button);
        }
    }

    private void ReadProcessCardInput()
    {
        if (processKeyboardEntry == null) return;
        if (Input.GetKeyDown(KeyCode.Tab) && EventSystem.current != null)
        {
            var selected = EventSystem.current.currentSelectedGameObject?.GetComponent<UnityEngine.UI.Selectable>();
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var next = selected == null || !selected.transform.IsChildOf(page) ? processKeyboardEntry :
                reverse ? selected.FindSelectableOnLeft() : selected.FindSelectableOnRight();
            if (next != null) EventSystem.current.SetSelectedGameObject(next.gameObject);
            return;
        }

        if (flipProcessCard == null) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || scroll > 0f)
            flipProcessCard(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || scroll < 0f)
            flipProcessCard(1);
    }

    private static UnityEngine.UI.Button ProcessControl(Transform parent, string name, Rect bounds, string text,
        int size, Action click)
    {
        var button = WorkshopUi.Button(parent, name, bounds, "", click, Color.clear);
        var label = WorkshopUi.Label(button.transform, "Label", new Rect(0, 0, bounds.width, bounds.height),
            text, size, Cream, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.38f);
        colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(1f, 1f, 1f, 0.66f);
        button.colors = colors;
        label.canvasRenderer.SetColor(colors.normalColor);
        button.targetGraphic = label;
        return button;
    }

    private static string ProcessCardNumber(int index) => (index + 1).ToString("00", CultureInfo.InvariantCulture);
}
