using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class WorkshopChapterGuide : MonoBehaviour
{
    private static readonly string[] Places =
    {
        "春｜桐乡桑园与蚕房",
        "夏｜南浔缫丝坊",
        "秋｜苏州织造工坊",
        "冬｜江南织坊"
    };

    private static readonly string[] Tips =
    {
        "先向蚕房师傅领任务，带篮采桑，再到蚕架喂蚕。喂饱后收下蚕种纸，前往浴蚕桶。",
        "到蚕茧簇架择茧、接通丝路。备好水和木柴，再从煮茧锅进入迷宫寻找线轴。",
        "秋分后天黑渐早，先借光验丝。修好织机、拿到梭子后，就能织出生坯绸。",
        "立冬用浙南入库的熟栀子试染，合样才染整绸。带染好的布到小雪裁衣台，沿样板裁出衣片。"
    };

    private static readonly string[] RegionalBoards = { "B01", "B02", "B03" };
    private static readonly Vector2[] RegionalBoardTitles =
    {
        new Vector2(0.49f, 0.53f), new Vector2(0.37f, 0.54f), new Vector2(0.51f, 0.52f)
    };

    private static WorkshopChapterGuide active;
    private static int closedFrame = -1;
    private RectTransform layout;
    private GameObject previousSelection;
    private Action dismissed;
    private Button[] actions;
    private bool closing;

    internal static bool IsOpen => active != null || Time.frameCount == closedFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        active = null;
        closedFrame = -1;
    }

    internal static void Show(WorkshopSeason season, Action onEnter = null, Action onClose = null)
    {
        if (active != null) return;
        var frame = WorkshopUi.NewCanvas("Chapter Location Guide", 18300);
        active = frame.gameObject.AddComponent<WorkshopChapterGuide>();
        active.layout = frame;
        active.dismissed = onClose;
        active.previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        active.Build(season, WorkshopProgress.IsUnlocked(season) ? onEnter : null);
        WorkshopAudioFeedback.Notice();
    }

    private void Build(WorkshopSeason season, Action onEnter)
    {
        const float contentTop = 174f;
        WorkshopUi.Block(layout, WorkshopUi.Veil);
        var page = WorkshopUi.PixelCard(layout, "Chapter Guide", new Rect(185, 105, 1550, 870),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        int index = (int)season;
        WorkshopUi.Label(page, "Chapter Location", new Rect(48, 32, 1340, 70), Places[index], 49, font: WorkshopArt.ReadingFont);
        WorkshopUi.Fill(page, "Guide Rule", new Rect(48, 112, 1455, 2), WorkshopUi.Bronze);
        var guide = Resources.Load<Sprite>("WorkshopGuides/" + season + "_ChapterGuide");
        WorkshopUi.PixelCard(page, "Chapter Artwork Border", new Rect(48, contentTop, 919, 520), WorkshopUi.Cream, WorkshopUi.Bronze);
        WorkshopUi.Picture(page, "Chapter Artwork", new Rect(54, contentTop + 6, 907, 508), guide);

        var field = WorkshopNarrativeData.FieldNote(season);
        var board = index < RegionalBoards.Length ? Resources.Load<Sprite>("WorkshopDelivery/" + RegionalBoards[index]) : null;
        float textTop = contentTop;
        if (board != null)
        {
            var boardBounds = new Rect(1016, contentTop, 474, 267);
            var boardTitle = RegionalBoardTitles[index];
            var center = boardBounds.position + Vector2.Scale(boardBounds.size, boardTitle);
            WorkshopUi.Picture(page, "Illustrated Regional Board", boardBounds, board);
            WorkshopUi.Label(page, "Chapter Region", new Rect(center.x - 118, center.y - 53, 236, 38),
                "地点原型", 24, WorkshopUi.Green, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
            WorkshopUi.Label(page, "Illustrated Region Name", new Rect(center.x - 118, center.y - 13, 236, 57),
                field.Region, 37, WorkshopUi.Green, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
            textTop = boardBounds.yMax + 11;
        }
        else if (season != WorkshopSeason.Winter)
        {
            WorkshopUi.Label(page, "Chapter Region", new Rect(1016, 210, 474, 49),
                "地点原型 · " + field.Region, 31, WorkshopUi.Green);
            textTop = 281;
        }

        var textBounds = new Rect(1016, textTop, 470, board != null ? 197 : 265);
        var overview = WorkshopUi.Label(page, "Chapter Context", textBounds, Tips[index], board != null ? 28 : 32,
            WorkshopUi.Ink, TextAnchor.UpperLeft, WorkshopArt.ReadingFont);
        overview.horizontalOverflow = HorizontalWrapMode.Wrap;
        overview.lineSpacing = 1.2f;

        var source = WorkshopUi.PixelButton(page, "Chapter Sources", new Rect(1016, textBounds.yMax + 12, 272, 61),
            "阅读相关资料", () => WorkshopHeritageBrief.ShowSeason(season), false, 28);
        var back = WorkshopUi.PixelButton(page, "Close Chapter Guide", new Rect(1016, 752, 199, 62),
            "返回", () => Close(dismissed), false, 29);
        if (onEnter != null)
        {
            var enter = WorkshopUi.PixelButton(page, "Enter Chapter", new Rect(1230, 752, 272, 62),
                "进入关卡", () => Close(onEnter), true, 29);
            actions = new[] { enter, back, source };
        }
        else actions = new[] { back, source };

        foreach (var label in page.GetComponentsInChildren<Text>(true)) label.font = WorkshopArt.ReadingFont;

        for (int i = 0; i < actions.Length; i++)
        {
            var prior = actions[(i + actions.Length - 1) % actions.Length];
            var next = actions[(i + 1) % actions.Length];
            actions[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit, selectOnUp = prior, selectOnLeft = prior,
                selectOnDown = next, selectOnRight = next
            };
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(actions[0].gameObject);
    }

    private void Update()
    {
        if (WorkshopHeritageBrief.IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Close(dismissed);
        else if (Input.GetKeyDown(KeyCode.Tab) && EventSystem.current != null)
        {
            var selection = EventSystem.current.currentSelectedGameObject;
            int index = Array.FindIndex(actions, action => action.gameObject == selection);
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            EventSystem.current.SetSelectedGameObject(
                actions[(index + actions.Length + (reverse ? -1 : 1)) % actions.Length].gameObject);
        }
    }

    private void Close(Action callback)
    {
        if (closing) return;
        closing = true;
        active = null;
        closedFrame = Time.frameCount;
        WorkshopUi.CloseCanvas(layout);
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }
}
