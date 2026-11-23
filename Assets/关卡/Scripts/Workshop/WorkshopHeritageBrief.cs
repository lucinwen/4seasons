using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class WorkshopHeritageBrief : MonoBehaviour
{
    private const int CanvasOrder = 18700;
    private const string HangshanUrl = "https://www.news.cn/politics/20260406/dc54d3e3a63745c1a4af66a95f483bdf/c.html";
    private static WorkshopHeritageBrief active;
    private static int closedFrame = -1;

    private readonly struct Story
    {
        internal readonly string Tab, Title, Credit, Image, Body, Url, ExtraLabel, ExtraUrl;
        internal Story(string tab, string title, string credit, string image, string body, string url,
            string extraLabel = null, string extraUrl = null)
        {
            Tab = tab;
            Title = title;
            Credit = credit;
            Image = image;
            Body = body;
            Url = url;
            ExtraLabel = extraLabel;
            ExtraUrl = extraUrl;
        }
    }

    private static readonly Story[] Stories =
    {
        new Story("含山蚕花", "湖州南浔：蚕花绽春闹含山", "真实新闻 · 新华每日电讯 · 2026.04.06",
            "WorkshopHeritage/HangshanSilkwormFlower",
            "清明时节，浙江湖州南浔善琏镇含山迎来轧蚕花活动。巡游队伍走上古道，传承人在广场教游客制作蚕花。水乡的人为什么年年说起蚕花？一件丝衣又是怎样织成的？",
            HangshanUrl, "桑蚕丝织技艺", "https://www.ihchina.cn/sangcansi.html"),
        new Story("桐乡春俗", "桐乡河山镇的清明蚕俗", "真实新闻 · 桐乡市委宣传部 · 2025.03.29",
            "WorkshopHeritage/TongxiangSilkwormFlower",
            "桐乡八泉村在清明前后举行蚕俗活动。花船上的蚕花传递丰收祝愿，当地还把蚕桑知识做成研学课程。湖州含山和桐乡的现场活动各有来历。",
            "https://app.xinhuanet.com/news/article.html?articleId=6794d82568215815e31c08c2fdd07ea7",
            "春蚕饲养资料", "https://www.yancheng.gov.cn/art/2024/3/8/art_34252_17259.html"),
        new Story("南浔缫丝", "南浔古镇的辑里湖丝", "真实新闻 · 新华网 · 2024.04.28",
            "WorkshopHeritage/NanxunHandReeling",
            "南浔的代表性传承人顾明琪仍到古镇展示手工缫丝。煮茧后要找出丝头、稳住水温和卷绕速度。工坊的环节需要实际练习。初夏报道中的同地蚕户也可能在不同饲育批次。",
            "https://www.news.cn/culture/20240428/645654267e7744dcaabc374fcd02033e/c.html",
            "芒种地方蚕讯", "https://paper.xinmin.cn/html/home/2024-06-05/06/22494.html"),
        new Story("苏州宋锦", "苏州宋锦的手工展演", "真实新闻 · 经济参考报 · 2024.04.08",
            "WorkshopHeritage/SuzhouSongBrocade",
            "在苏州的手工展示中，挽花工和织花工配合操作织机。宋锦的经线与彩纬共同显花。留下织机、记录工序，并让学生练习操作，手艺才能继续使用。",
            "https://www.news.cn/20240408/fc60fb2ad7c24c398977b04fb092ec33/c.html",
            "宋锦工艺资料", "https://szfy.wglj.suzhou.com.cn/index.php/fy-project/49"),
        new Story("冬日工坊", "江南工坊的冬日备料", "工艺资料 · 盐城市农业农村局 · 2024.12.09",
            "WorkshopGuides/Winter_ChapterGuide",
            "本作的冬章在虚构江南工坊使用前几章备好的素绸。现实里的冬季桑园也要因地管理，为来年备蚕。小雪标示节气，游戏的雪景是场景演绎。",
            "https://snw.yancheng.gov.cn/art/2024/12/9/art_899_4262519.html",
            "二十四节气", "https://www.ihchina.cn/directory_details/12084")
    };

    private RectTransform layout;
    private RectTransform page;
    private RectTransform article;
    private GameObject previousSelection;
    private Action onClose;
    private Button[] tabs;
    private Button back;
    private Button original;
    private Button extra;
    private Button begin;
    private Selectable[] navigation;
    private bool closing;

    internal static bool IsOpen => active != null || Time.frameCount == closedFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        active = null;
        closedFrame = -1;
    }

    internal static void Show(Action onClose = null) => Open(0, null, onClose);

    internal static void ShowSeason(WorkshopSeason season, Action onClose = null) =>
        Open((int)season + 1, null, onClose);

    internal static void ShowOpening(Action beginApprenticeship, Action onClose) =>
        Open(0, beginApprenticeship, onClose);

    private static void Open(int story, Action start, Action onClose)
    {
        if (active != null) return;
        var layout = WorkshopUi.NewCanvas(start != null ? "Workshop Story Opening" : "Workshop Heritage", CanvasOrder);
        var view = layout.gameObject.AddComponent<WorkshopHeritageBrief>();
        active = view;
        view.layout = layout;
        view.onClose = onClose;
        view.previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        view.Build(story, start);
        WorkshopAudioFeedback.Notice();
    }

    private void Build(int story, Action start)
    {
        WorkshopUi.Block(layout, WorkshopUi.Veil);
        page = WorkshopUi.PixelCard(layout, "Heritage Page", new Rect(200, 104, 1520, 872), WorkshopUi.Paper, WorkshopUi.Bronze);
        bool opening = start != null;
        WorkshopUi.Label(page, "Heritage Heading", new Rect(48, 22, 1052, 69),
            opening ? "一根丝，从桑叶开始" : "新闻与来源", 50, font: WorkshopArt.ReadingFont);
        back = WorkshopUi.PixelButton(page, "Heritage Return", new Rect(1250, 30, 222, 54),
            "返回", () => Close(onClose), false, 29);

        if (opening)
        {
            begin = WorkshopUi.PixelButton(page, "Begin Apprenticeship", new Rect(1150, 755, 322, 65),
                "开始学徒任务", () => Close(start), true, 30);
            tabs = Array.Empty<Button>();
        }
        else
        {
            tabs = new Button[Stories.Length];
            for (int index = 0; index < Stories.Length; index++)
            {
                int choice = index;
                tabs[index] = WorkshopUi.Button(page, "Heritage Topic " + index,
                    new Rect(48 + index * 286, 115, 266, 58), Stories[index].Tab, () => Display(choice), WorkshopUi.Green,
                    size: 30, font: WorkshopArt.ReadingFont);
            }
        }

        Display(story);
        Select(opening ? begin : tabs[story]);
    }

    private void Display(int index)
    {
        if (article != null)
        {
            article.gameObject.SetActive(false);
            Destroy(article.gameObject);
        }

        var story = Stories[index];
        bool opening = begin != null;
        float top = opening ? 140 : 225;
        article = WorkshopUi.Rect(page, "Heritage Article", new Rect(0, 0, 1520, 872));
        var art = Resources.Load<Sprite>(story.Image);
        WorkshopUi.PixelCard(article, "Heritage Image Border", new Rect(48, top, 778, 440), WorkshopUi.Cream, WorkshopUi.Bronze);
        WorkshopUi.Picture(article, "Heritage Illustration", new Rect(55, top + 6, 764, 428), art);
        WorkshopUi.Label(article, "Heritage Image Caption", new Rect(49, top + 449, 778, 35),
            index == 4 ? "游戏场景 · 虚构江南工坊" : "游戏像素演绎", 23, WorkshopUi.Faded, font: WorkshopArt.ReadingFont);

        WorkshopUi.Label(article, "Publication Credit", new Rect(870, top + 1, 599, 45), story.Credit, 25, WorkshopUi.Faded,
            font: WorkshopArt.ReadingFont);
        Body(article, "Article Title", new Rect(870, top + 55, 599, 104), story.Title, 41);
        string description = story.Body;
        if (opening)
            description += "\n\n你是来学手艺的学徒。跟着桑园、蚕房和织机上的线索，从采桑养蚕做到裁衣，最后试穿亲手做出的丝衣。";
        Body(article, "Article Text", new Rect(873, top + 160, 591, 228), description, 28);

        original = WorkshopUi.PixelButton(article, "Read Primary Source", new Rect(870, opening ? 755 : 705, 255, 65),
            opening ? "阅读原报道" : "阅读原文", () => Application.OpenURL(story.Url), false, 29);
        if (!opening && !string.IsNullOrEmpty(story.ExtraUrl))
            extra = WorkshopUi.PixelButton(article, "Read Related Source", new Rect(1144, 705, 325, 65),
                story.ExtraLabel, () => Application.OpenURL(story.ExtraUrl), true, 28);

        foreach (var tab in tabs) tab.targetGraphic.color = WorkshopUi.Green;
        if (!opening) tabs[index].targetGraphic.color = WorkshopUi.Clay;
        navigation = opening
            ? new Selectable[] { begin, original, back }
            : new Selectable[] { tabs[0], tabs[1], tabs[2], tabs[3], tabs[4], original, extra, back };
        for (int i = 0; i < navigation.Length; i++)
        {
            Selectable previous = navigation[(i + navigation.Length - 1) % navigation.Length];
            Selectable next = navigation[(i + 1) % navigation.Length];
            navigation[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = previous, selectOnUp = previous, selectOnRight = next, selectOnDown = next
            };
        }
    }

    private static void Body(Transform parent, string id, Rect bounds, string value, int size)
    {
        var text = WorkshopUi.Label(parent, id, bounds, value, size, WorkshopUi.Ink, TextAnchor.UpperLeft, WorkshopArt.ReadingFont);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.lineSpacing = 1.14f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close(onClose);
        else if (Input.GetKeyDown(KeyCode.Tab) && EventSystem.current != null)
        {
            var current = EventSystem.current.currentSelectedGameObject;
            int index = Array.FindIndex(navigation, button => button.gameObject == current);
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Select(navigation[(index + navigation.Length + (reverse ? -1 : 1)) % navigation.Length]);
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

    private static void Select(Selectable button)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
