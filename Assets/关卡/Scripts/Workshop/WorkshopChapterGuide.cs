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

    private static readonly string[] Process =
    {
        "清明采桑 · 谷雨蚕事",
        "小暑 · 大暑｜择茧、煮茧、缫丝",
        "秋分酬蚕 · 寒露织造",
        "立冬试色 · 小雪裁衣"
    };

    private static readonly string[] Tips =
    {
        "先在桑园看看桑芽和叶面，再把合适的桑叶带进蚕房。遇到雨后湿叶，先晾干再给小蚕。",
        "到蔟架看茧品相，再到煮茧锅找丝头。当地初夏的蚕讯记录了不同批次，织坊会在暑热时使用备好的蚕茧。",
        "苏州织造有不同工序分工。开机前看经线是否齐整，再穿梭引纬。章内先练基础素绸，宋锦另有工艺资料。",
        "本章取景于虚构的江南工坊。先检查染材和样布，再用前几章备好的素绸染布、裁片和缝衣。"
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
        WorkshopUi.Block(layout, WorkshopUi.Veil);
        var page = WorkshopUi.PixelCard(layout, "Chapter Guide", new Rect(185, 105, 1550, 870),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        int index = (int)season;
        WorkshopUi.Label(page, "Chapter Location", new Rect(48, 32, 1340, 70), Places[index], 49, font: WorkshopArt.ReadingFont);
        WorkshopUi.Fill(page, "Guide Rule", new Rect(48, 112, 1455, 2), WorkshopUi.Bronze);
        WorkshopUi.Label(page, "Chapter Process", new Rect(50, 137, 930, 46), Process[index], 35, WorkshopUi.Green);
        var guide = Resources.Load<Sprite>("WorkshopGuides/" + season + "_ChapterGuide");
        WorkshopUi.PixelCard(page, "Chapter Artwork Border", new Rect(48, 202, 919, 520), WorkshopUi.Cream, WorkshopUi.Bronze);
        WorkshopUi.Picture(page, "Chapter Artwork", new Rect(54, 208, 907, 508), guide);
        WorkshopUi.Label(page, "Map Credit", new Rect(49, 738, 800, 40), "关卡取景 · 游戏像素演绎", 25, WorkshopUi.Faded);

        var field = WorkshopNarrativeData.FieldNote(season);
        WorkshopUi.Label(page, "Chapter Region", new Rect(1016, 210, 474, 49),
            season == WorkshopSeason.Winter ? "织坊故事 · 江南" : "地点原型 · " + field.Region, 31, WorkshopUi.Green);
        var overview = WorkshopUi.Label(page, "Chapter Context", new Rect(1016, 281, 470, 265), Tips[index], 32,
            WorkshopUi.Ink, TextAnchor.UpperLeft, WorkshopArt.ReadingFont);
        overview.horizontalOverflow = HorizontalWrapMode.Wrap;
        overview.lineSpacing = 1.2f;

        var source = WorkshopUi.PixelButton(page, "Chapter Sources", new Rect(1016, 580, 272, 61),
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
