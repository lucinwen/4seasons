using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class WorkshopChapterGuide : MonoBehaviour
{
    internal static readonly string[] Titles =
    {
        "春季·采桑育蚕",
        "夏季·寻轴缫丝",
        "秋季·织染攀花",
        "冬季·剪帛成衣"
    };

    private static readonly string[] Process =
    {
        "清明采桑 · 谷雨浴蚕与喂蚕",
        "立夏下簇 · 小满缫丝 · 芒种夏至备料",
        "立秋采材 · 白露织造与秋社 · 秋分染色 · 寒露攀花",
        "立冬、小雪剪帛 · 量体裁衣 · 小雪成衣"
    };

    private static readonly string[] Tips =
    {
        "春气渐暖，桑芽初展。拾篮采桑、清洗蚕种，\n再用桑叶喂养蚕宝宝，开启桑织之旅。",
        "到择茧草堂下簇收茧，备水拾柴后穿过迷宫寻得线轴。\n控制松紧完成缫丝，再接通水管备料。",
        "采集天然染材，拾槌修机、取梭织坯，再研磨染色。\n首次交谈收下糕点，备齐贡品参加秋社，最后织出攀花纹样。",
        "带上秋季染布，沿虚线剪出衣片，拖放衣样纸确认版型。\n找到缝衣针后选择款式、拼片缝衣，最后试衣留念。"
    };

    private static readonly string[] Delivery =
    {
        "本章交付：蚕种、桑叶、蚕宝宝　｜　完成奖励：蚕茧",
        "本章交付：盐、线轴、索绪　｜　完成奖励：丝束",
        "本章交付：土地公雕像、梭子、生坯绸　｜　完成奖励：素绸",
        "本章交付：缝衣针、染布、衣片　｜　完成奖励：成衣"
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
        WorkshopUi.Block(layout, new Color32(24, 18, 13, 172));
        int index = (int)season;
        var page = WorkshopUi.Rect(layout, "Chapter Guide", new Rect(70, 39, 1780, 1001.25f));
        var frame = Resources.Load<Sprite>("WorkshopChapterFrames/Blank/" + season + "_Frame");
        WorkshopUi.Picture(page, "Seasonal Wood Frame", new Rect(0, 0, 1780, 1001.25f), frame);
        // The chapter name is metadata only. The illustrated plaque and all UI copy stay blank.
        page.gameObject.name = "Chapter Guide · " + Titles[index];
        // Preserve the three native buttons for the team's later typography pass.

        var source = ChapterButton(page, "Chapter Sources", new Rect(440, 618, 330, 70),
            string.Empty, () => WorkshopHeritageBrief.ShowSeason(season), false);
        var back = ChapterButton(page, "Close Chapter Guide", new Rect(800, 618, 200, 70),
            string.Empty, () => Close(dismissed), false);
        var enter = ChapterButton(page, "Enter Chapter", new Rect(1030, 618, 310, 70),
            string.Empty, () => Close(onEnter), true);
        enter.interactable = onEnter != null;
        actions = onEnter != null ? new[] { enter, back, source } : new[] { back, source };

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

    private static Button ChapterButton(Transform parent, string name, Rect bounds, string text, Action clicked, bool primary)
    {
        var button = WorkshopUi.PixelButton(parent, name, bounds, text, clicked, primary, 35,
            new Color32(130, 57, 27, 255));
        var colors = button.colors;
        colors.highlightedColor = new Color32(255, 235, 195, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color32(224, 186, 122, 255);
        button.colors = colors;
        if (!primary) button.GetComponentInChildren<Text>().color = new Color32(84, 43, 24, 255);
        return button;
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
