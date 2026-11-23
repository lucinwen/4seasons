using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WorkshopHub : MonoBehaviour
{
    private static readonly Color Backdrop = new Color32(30, 43, 35, 255);
    private static readonly Color Rule = new Color32(180, 150, 101, 210);
    private static readonly Color Brass = new Color32(223, 185, 107, 255);
    private static readonly Color[] SeasonColors =
    {
        new Color32(43, 83, 66, 255),
        new Color32(58, 91, 83, 255),
        new Color32(125, 65, 48, 255),
        new Color32(54, 75, 105, 255),
    };

    private static WorkshopSeason? _returnFrom;
    private static WorkshopItem? _trackedItem;
    private static WorkshopHub _installed;
    private WorkshopSeason _season;
    private RectTransform _frame;
    private RectTransform _page;
    private RawImage _backdrop;
    private GameObject _returnButton;
    private bool _returnOnly;
    private bool _finishModalOpen;
    private bool _dirty;
    private bool _leaving;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntime()
    {
        _returnFrom = null;
        _trackedItem = null;
        _installed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive || (_installed != null && _installed.gameObject.scene == scene))
            return;

        bool isHub = scene.name == WorkshopProgress.HubScene;
        if (isHub)
        {
            var returned = _returnFrom;
            _returnFrom = null;
            WorkshopMainHallWorld.Build(returned ?? RecommendedSeason());
            return;
        }
        if (!WorkshopProgress.TryGetSeason(scene.name, out var season)) return;

        var frame = WorkshopUi.NewCanvas("Workshop Return", 90);
        var hub = frame.gameObject.AddComponent<WorkshopHub>();
        _installed = hub;
        hub._frame = frame;
        hub._returnOnly = true;
        hub._season = season;
        hub._returnButton = WorkshopUi.Button(frame, "Return to Workshop", new Rect(46f, 978f, 228f, 64f),
            "返回工坊", hub.ReturnToWorkshop, WorkshopUi.Ink, size: 28,
            font: WorkshopArt.WorkshopFont).gameObject;
        WorkshopQuestGuide.Attach(season);
    }

    private static WorkshopSeason RecommendedSeason()
    {
        for (int i = 3; i >= 0; i--)
            if (WorkshopProgress.IsUnlocked((WorkshopSeason)i)) return (WorkshopSeason)i;
        return WorkshopSeason.Spring;
    }

    private void OnEnable() => WorkshopProgress.Changed += OnProgressChanged;
    private void OnDisable() => WorkshopProgress.Changed -= OnProgressChanged;
    private void OnProgressChanged() => _dirty = !_returnOnly;

    private void Update()
    {
        if (_returnOnly)
        {
            bool available = !_leaving && !WorkshopQuestGuide.IsWorldOverlayActive &&
                !WorkshopQuestGuide.BlocksWorldInput;
            if (_returnButton != null && _returnButton.activeSelf != available) _returnButton.SetActive(available);
            return;
        }

        bool availableInHub = UpdateHubInput();
        if (_dirty && availableInHub) RenderWorkshop();
    }

    private bool UpdateHubInput()
    {
        bool available = !_leaving && !_finishModalOpen &&
            !WorkshopKnowledgeView.IsOpen && !WorkshopStorageView.IsOpen &&
            !WorkshopNarrativeView.IsOpen && !WorkshopDialoguePresenter.BlocksWorldInput &&
            !WorkshopChapterGuide.IsOpen && !WorkshopHeritageBrief.IsOpen && !WorkshopCraftRecordView.BlocksWorldInput;
        return available;
    }

    private void ReturnToWorkshop()
    {
        if (_leaving) return;
        _leaving = true;
        _returnFrom = _season;
        _trackedItem = null;
        SceneManager.LoadScene(WorkshopProgress.HubScene);
    }

    private void ReturnToCover()
    {
        if (!UpdateHubInput()) return;
        _leaving = true;
        _trackedItem = null;
        _returnFrom = null;
        SceneManager.LoadScene(WorkshopTitle.SceneName);
    }

    private void RenderWorkshop()
    {
        _dirty = false;
        if (_page != null)
        {
            _page.gameObject.SetActive(false);
            Destroy(_page.gameObject);
        }

        DrawBackground();
        _page = WorkshopUi.Rect(_frame, "Workshop Screen", new Rect(0f, 0f, 1920f, 1080f));
        WorkshopUi.Fill(_page, "Main Hall Top", new Rect(0, 0, 1920, 170), new Color32(19, 43, 36, 250));
        WorkshopUi.Fill(_page, "Main Hall Bottom", new Rect(0, 810, 1920, 270), new Color32(20, 45, 38, 252));
        WorkshopUi.Fill(_page, "Top Gold Rule", new Rect(0, 166, 1920, 4), new Color32(213, 174, 103, 210));
        WorkshopUi.Fill(_page, "Bottom Gold Rule", new Rect(0, 810, 1920, 4), new Color32(213, 174, 103, 210));

        var title = Label(_page, "Game Title", new Rect(52f, 20f, 350f, 78f), "四时桑织", 56, WorkshopUi.Cream);
        title.verticalOverflow = VerticalWrapMode.Overflow;
        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(17, 30, 23, 220);
        shadow.effectDistance = new Vector2(2f, -3f);
        Label(_page, "Workshop Name", new Rect(57f, 94f, 350f, 40f),
            "四时合作社 · 中央主厅", 25, new Color32(223, 211, 173, 255));
        DrawAnnouncement();
        DrawTopActions();
        DrawMainHallHotspots();
        DrawSeasonDock();
    }

    private void DrawAnnouncement()
    {
        var story = WorkshopNarrativeData.For(_season);
        bool fresh = !WorkshopProgress.HasStep("announcement." + _season);
        var banner = WorkshopUi.Button(_page, "Workshop Announcement",
            new Rect(420f, 28f, 700f, 66f), "", OpenAnnouncement,
            new Color32(35, 71, 55, 245)).transform;
        WorkshopUi.Fill(banner, "Announcement Accent", new Rect(0, 0, 7, 66), fresh ? Brass : Rule);
        Label(banner, "Announcement Kicker", new Rect(20, 3, 158, 60),
            "合作社公告", 24, new Color32(246, 215, 151, 255));
        Label(banner, "Announcement Headline", new Rect(180, 3, 350, 60),
            story.AnnouncementTitle, 25, WorkshopUi.Cream);
        Label(banner, "Announcement Time", new Rect(522, 3, 98, 60),
            story.TimeLabel, 20, new Color32(221, 213, 185, 255), TextAnchor.MiddleRight);
        Label(banner, "Announcement State", new Rect(624, 3, 62, 60),
            fresh ? "新" : "查看", 23,
            fresh ? new Color32(255, 197, 113, 255) : WorkshopUi.Cream, TextAnchor.MiddleRight);
    }

    private void OpenAnnouncement()
    {
        WorkshopProgress.MarkStep("announcement." + _season);
        WorkshopNarrativeView.ShowAnnouncement(_season);
    }

    private void DrawBackground()
    {
        if (_backdrop == null)
        {
            var bounds = new Rect(0f, 170f, 1920f, 640f);
            _backdrop = WorkshopUi.Rect(_frame, "Main Hall Panorama", bounds).gameObject.AddComponent<RawImage>();
            _backdrop.raycastTarget = false;
        }

        _backdrop.rectTransform.anchoredPosition = new Vector2(0f, -170f);
        _backdrop.rectTransform.sizeDelta = new Vector2(1920f, 640f);
        var texture = Resources.Load<Texture2D>(WorkshopHallSignage.TexturePath);
        if (texture != null && _backdrop.transform.childCount == 0)
            WorkshopHallSignage.Draw(_backdrop.transform, WorkshopHallSignage.Panorama, WorkshopHallSignage.FullView);
        if (texture == null) texture = WorkshopArt.SeasonBackground(WorkshopSeason.Spring);
        _backdrop.texture = texture;
        _backdrop.enabled = texture != null;
        if (texture == null) return;
        _backdrop.uvRect = new Rect(0f, 0f, 1f, 1f);
    }

    private void DrawTopActions()
    {
        DrawTopButton(new Rect(1172, 28, 164, 56), "储物屋",
            OpenStorage, new Color32(53, 91, 70, 255), $"{AllItemsCount()} / 12");
        DrawTopButton(new Rect(1350, 28, 164, 56), "知识图鉴",
            () => WorkshopKnowledgeView.ShowJournal(_season), new Color32(63, 91, 76, 255));
        DrawTopButton(new Rect(1528, 28, 164, 56), WorkshopHallSignage.WardrobeName,
            ShowGarment, WorkshopUi.Clay, null, WorkshopProgress.IsComplete(WorkshopSeason.Winter));
        DrawTopButton(new Rect(1706, 28, 164, 56), "返回封面",
            ReturnToCover, new Color32(79, 69, 54, 255));
    }

    private void DrawTopButton(Rect bounds, string text, System.Action action,
        Color color, string badge = null, bool enabled = true)
    {
        var button = WorkshopUi.Button(_page, "Top " + text, bounds, text, action, color,
            enabled, size: 24, font: WorkshopArt.WorkshopFont);
        if (string.IsNullOrEmpty(badge)) return;
        Label(button.transform, "Top Button Badge", new Rect(bounds.width - 66, 4, 58, 22),
            badge, 16, new Color32(255, 222, 154, 255), TextAnchor.MiddleRight);
    }

    private void DrawMainHallHotspots()
    {
        int stored = AllItemsCount();
        DrawBuildingHotspot(new Rect(360, 390, 360, 258), "四时织录",
            "知识卡与节气", new Color32(45, 85, 65, 245),
            () => WorkshopKnowledgeView.ShowJournal(_season), $"{stored} / 12");
        DrawBuildingHotspot(new Rect(790, 333, 322, 96), "新闻公告",
            "合作社新消息", new Color32(132, 70, 47, 245), OpenAnnouncement,
            WorkshopProgress.HasStep("announcement." + _season) ? "查看" : "新");
        DrawBuildingHotspot(new Rect(887, 430, 318, 215),
            WorkshopProgress.SeasonName(_season) + "季工坊", ChapterCraft(_season),
            SeasonColors[(int)_season], EnterChapter, SeasonStatus(_season));
        DrawBuildingHotspot(new Rect(1365, 390, 300, 258), "储物屋",
            "查看所有材料", new Color32(67, 83, 63, 248), OpenStorage, $"{stored} / 12");

        Label(_page, "Main Hall Hint", new Rect(1460, 717, 390, 56),
            "点选建筑进入功能 · 点选季节切换任务", 22,
            new Color32(222, 238, 210, 245), TextAnchor.MiddleRight);
    }

    private void DrawBuildingHotspot(Rect bounds, string title, string detail,
        Color tint, System.Action action, string badge)
    {
        var hotspot = WorkshopUi.Button(_page, "Building " + title, bounds, "", action,
            new Color32(255, 255, 255, 1), size: 24, font: WorkshopArt.WorkshopFont);
        float plaqueWidth = Mathf.Min(bounds.width - 22f, 260f);
        var plaque = WorkshopUi.Fill(hotspot.transform, "Building Plaque",
            new Rect((bounds.width - plaqueWidth) * 0.5f, bounds.height - 68f,
                plaqueWidth, 56f), tint).transform;
        Label(plaque, "Building Title", new Rect(14, 2, plaqueWidth - 80f, 31),
            title, 25, WorkshopUi.Cream);
        Label(plaque, "Building Detail", new Rect(14, 29, plaqueWidth - 28f, 24),
            detail, 18, new Color32(242, 222, 178, 255));
        Label(plaque, "Building Badge", new Rect(plaqueWidth - 66f, 4, 54, 25),
            badge, 17, new Color32(255, 222, 154, 255), TextAnchor.MiddleRight);
    }

    private void DrawSeasonDock()
    {
        Color accent = SeasonColors[(int)_season];
        Label(_page, "Dock Kicker", new Rect(52, 818, 330, 40),
            "四时章节 · 当前进度", 24, new Color32(220, 194, 132, 255));
        Label(_page, "Dock Season", new Rect(378, 812, 650, 52),
            WorkshopProgress.SeasonName(_season) + "季 · " + ChapterCraft(_season), 31,
            WorkshopUi.Cream);

        for (int i = 0; i < 4; i++)
        {
            var tabSeason = (WorkshopSeason)i;
            bool unlocked = WorkshopProgress.IsUnlocked(tabSeason);
            bool selected = tabSeason == _season;
            var tab = WorkshopUi.Button(_page, "Dock Season " + tabSeason,
                new Rect(52 + i * 216, 864, 202, 64), "", () => SelectSeason(tabSeason),
                selected ? SeasonColors[i] : new Color32(39, 65, 53, 235), unlocked);
            Label(tab.transform, "Dock Season Name", new Rect(16, 3, 170, 34),
                WorkshopProgress.SeasonName(tabSeason) + "季", 27,
                unlocked ? WorkshopUi.Cream : new Color32(149, 151, 132, 255));
            Label(tab.transform, "Dock Season State", new Rect(16, 34, 170, 25),
                SeasonStatus(tabSeason), 17,
                unlocked ? new Color32(230, 210, 170, 255) : new Color32(120, 125, 111, 255));
        }

        var items = WorkshopProgress.Items(_season);
        for (int i = 0; i < items.Length; i++)
            DrawDockTask(items[i], i, new Rect(52 + i * 345, 956, 326, 68), accent);

        bool unlockedSeason = WorkshopProgress.IsUnlocked(_season);
        var enter = WorkshopUi.Button(_page, "Dock Enter Chapter",
            new Rect(1112, 864, 250, 68),
            WorkshopProgress.IsComplete(_season) ? "回访本章" : HasStarted(CountMaterials(items)) ? "继续章节" : "进入章节",
            EnterChapter, accent, unlockedSeason, size: 28, font: WorkshopArt.WorkshopFont);
        var buttonArt = WorkshopArt.SeasonButton(_season);
        if (buttonArt != null)
        {
            var image = enter.GetComponent<Image>();
            image.sprite = buttonArt;
            image.color = Color.white;
            if (buttonArt.border.sqrMagnitude > 0f) image.type = Image.Type.Sliced;
        }

        bool complete = WorkshopProgress.IsComplete(_season);
        bool ready = WorkshopProgress.CanSubmit(_season);
        string action = complete ? (_season == WorkshopSeason.Winter ? "试穿成衣" : "查看合成成果") : "开始合成";
        System.Action finish = _season == WorkshopSeason.Winter && complete
            ? (System.Action)ShowGarment : OpenSynthesis;
        WorkshopUi.Button(_page, "Dock Submit Chapter", new Rect(1380, 864, 250, 68),
            action, finish, WorkshopUi.Clay, ready || complete, size: 28,
            font: WorkshopArt.WorkshopFont);
        Label(_page, "Dock Product", new Rect(1112, 952, 518, 62),
            "本季成果 · " + WorkshopProgress.ProductName(_season) + "\n" +
            ChapterGuidance(unlockedSeason, complete, ready, _season == WorkshopSeason.Winter),
            20, new Color32(226, 214, 180, 255), TextAnchor.UpperRight);
    }

    private void DrawDockTask(WorkshopItem item, int index, Rect bounds, Color accent)
    {
        bool owned = WorkshopProgress.Has(item);
        bool unlocked = WorkshopProgress.IsUnlocked(_season);
        var button = WorkshopUi.Button(_page, "Dock Task " + item, bounds, "",
            owned ? () => WorkshopKnowledgeView.ShowItem(item) : () => EnterChapter(item),
            owned ? new Color32(61, 90, 69, 245) : new Color32(37, 66, 54, 235),
            unlocked, size: 20, font: WorkshopArt.WorkshopFont);
        WorkshopUi.Fill(button.transform, "Dock Task Accent", new Rect(0, 0, 4, 68),
            owned ? new Color32(202, 180, 118, 255) : accent);
        Label(button.transform, "Dock Task Number", new Rect(14, 4, 30, 30),
            (index + 1).ToString(), 20, new Color32(225, 193, 115, 255));
        Label(button.transform, "Dock Task Name", new Rect(46, 4, 160, 30),
            WorkshopProgress.ItemName(item), 22, WorkshopUi.Cream);
        Label(button.transform, "Dock Task State", new Rect(46, 34, 250, 25),
            owned ? "已入库 · 查看" : unlocked ? "点击前往收集" : "待开放", 17,
            new Color32(204, 201, 173, 255));
    }

    private void OpenStorage()
    {
        OpenSynthesis();
    }

    private static int AllItemsCount()
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
            foreach (var item in WorkshopProgress.Items((WorkshopSeason)i))
                if (WorkshopProgress.Has(item)) count++;
        return count;
    }

    private void DrawSeasons()
    {
        var bounds = new Rect(84f, 225f, 397f, 761f);
        var art = WorkshopArt.WorkshopChapterRail;
        var nav = (art != null
            ? WorkshopUi.Picture(_page, "Chapters", bounds, art, stretch: true)
            : WorkshopUi.Fill(_page, "Chapters", bounds, Backdrop)).transform;
        Label(nav, "Chapters Title", new Rect(51f, 44f, 295f, 65f), "四季章节", 45, WorkshopUi.Cream,
            TextAnchor.MiddleCenter);
        WorkshopUi.Fill(nav, "Chapter Rule", new Rect(68f, 115f, 261f, 1f), Brass);

        for (int i = 0; i < 4; i++)
        {
            var season = (WorkshopSeason)i;
            Color tileColor = season == _season ? SeasonColors[i] : (Color)new Color32(32, 59, 47, 169);
            var card = WorkshopUi.Button(nav, "Select " + season, new Rect(45f, 135f + i * 137f, 307f, 113f),
                "", () => SelectSeason(season), tileColor).transform;
            if (season == _season)
                WorkshopUi.Fill(card, "Selection", new Rect(0f, 0f, 4f, 113f), Brass);

            string name = WorkshopProgress.SeasonName(season);
            string status = SeasonStatus(season);
            Label(card, "Season", new Rect(20f, 12f, 270f, 54f), name + " · " + ChapterCraft(season),
                36, WorkshopUi.Cream);
            Label(card, "Status", new Rect(21f, 63f, 266f, 39f), status, 26,
                new Color32(252, 222, 170, 255));
        }
    }

    private void DrawTasks()
    {
        var bounds = new Rect(511f, 225f, 1325f, 761f);
        var art = WorkshopArt.WorkshopTaskBoard;
        var content = (art != null
            ? WorkshopUi.Picture(_page, "Task Board", bounds, art, stretch: true)
            : WorkshopUi.Panel(_page, "Task Board", bounds, WorkshopArt.SeasonPanel(_season))).transform;
        Color accent = SeasonColors[(int)_season];
        Label(content, "Tasks Title", new Rect(215f, 51f, 610f, 76f),
            WorkshopProgress.SeasonName(_season) + "季任务", 58, accent);

        var items = WorkshopProgress.Items(_season);
        int obtained = CountMaterials(items);
        Label(content, "Count", new Rect(810f, 66f, 286f, 48f),
            $"关键道具  {obtained} / {items.Length}", 31, accent, TextAnchor.MiddleRight);
        WorkshopUi.Fill(content, "Header Rule", new Rect(193f, 132f, 937f, 2f), Rule);

        for (int i = 0; i < items.Length; i++) DrawTask(content, items[i], i, accent);

        bool complete = WorkshopProgress.IsComplete(_season);
        bool unlocked = WorkshopProgress.IsUnlocked(_season);
        bool ready = WorkshopProgress.CanSubmit(_season);
        bool winter = _season == WorkshopSeason.Winter;
        WorkshopUi.Fill(content, "Footer Rule", new Rect(93f, 493f, 1139f, 2f), Rule);
        var day = new Rect(93f, 507f, 1139f, 50f);
        string product = WorkshopProgress.ProductName(_season);
        Label(content, "Product", day, complete ? $"已提交 · {product}" : $"章节成果 · {product}",
            36, accent);
        Label(content, "Task Guidance", new Rect(93f, 556f, 1139f, 43f),
            ChapterGuidance(unlocked, complete, ready, winter), 29, WorkshopUi.Faded);

        var enter = WorkshopUi.Button(content, "Enter Chapter", new Rect(216f, 619f, 300f, 66f),
            complete ? "回访本章" : HasStarted(obtained) ? "继续章节" : "进入章节", EnterChapter, accent, unlocked,
            font: WorkshopArt.WorkshopFont);
        var buttonArt = WorkshopArt.SeasonButton(_season);
        if (buttonArt != null)
        {
            var image = enter.GetComponent<Image>();
            image.sprite = buttonArt;
            image.color = Color.white;
            if (buttonArt.border.sqrMagnitude > 0f) image.type = Image.Type.Sliced;
        }

        string action = complete ? (winter ? "试穿成衣" : "查看合成成果") : "开始合成";
        System.Action finish = winter && complete ? (System.Action)ShowGarment : OpenSynthesis;
        var finishButton = WorkshopUi.Button(content, "Submit Chapter", new Rect(793f, 619f, 316f, 66f),
            action, finish, WorkshopUi.Clay, ready || complete, font: WorkshopArt.WorkshopFont);
        finishButton.GetComponentInChildren<Text>().verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void DrawTask(Transform parent, WorkshopItem item, int index, Color accent)
    {
        bool collected = WorkshopProgress.Has(item);
        bool canStart = !collected && WorkshopProgress.IsUnlocked(_season);
        var bounds = new Rect(93f, 153f + index * 111f, 1139f, 100f);
        var rowColor = new Color32(234, 220, 185, 127);
        var row = canStart
            ? WorkshopUi.Button(parent, "Start Task " + item, bounds, "", () => EnterChapter(item), rowColor).transform
            : WorkshopUi.Fill(parent, "Task " + item, bounds, rowColor).transform;
        WorkshopUi.Fill(row, "Icon Background", new Rect(13f, 11f, 78f, 78f), new Color32(218, 199, 156, 162));
        var icon = WorkshopUi.Rect(row, "Task Material", new Rect(13f, 11f, 78f, 78f)).gameObject.AddComponent<RawImage>();
        icon.texture = WorkshopArt.TaskIconAtlas;
        icon.uvRect = WorkshopArt.TaskIconUV(item);
        icon.raycastTarget = false;
        icon.enabled = icon.texture != null;

        WorkshopObjectives.Describe(item, out string title, out string guide);
        string itemName = WorkshopProgress.ItemName(item);
        string rowTitle = title == itemName ? title : title + " · " + itemName;
        Label(row, "Task", new Rect(109f, 7f, 800f, 46f), rowTitle, 35);
        Label(row, "Hint", new Rect(110f, 50f, 799f, 40f), guide, 28, WorkshopUi.Faded);

        if (collected)
        {
            WorkshopUi.Button(row, "Read " + item, new Rect(915f, 25f, 201f, 50f), "已获得 · 查看",
                () => WorkshopKnowledgeView.ShowItem(item), accent, size: 26, font: WorkshopArt.WorkshopFont);
        }
        else
        {
            if (canStart)
                WorkshopUi.Fill(row, "Start Badge", new Rect(915f, 25f, 201f, 50f), accent);
            Label(row, "Status", new Rect(915f, 25f, 201f, 50f), canStart ? "前往任务" : "待开放", 28,
                canStart ? WorkshopUi.Cream : WorkshopUi.Faded, TextAnchor.MiddleCenter);
        }
    }

    private static Text Label(Transform parent, string name, Rect bounds, string value, int size,
        Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        return WorkshopUi.Label(parent, name, bounds, value, size, color, anchor, WorkshopArt.WorkshopFont);
    }

    private bool HasStarted(int items)
    {
        if (items > 0) return true;
        switch (_season)
        {
            case WorkshopSeason.Spring: return WorkshopProgress.HasStep("spring.basket") || WorkshopProgress.HasStep("spring.eggPaper");
            case WorkshopSeason.Autumn: return WorkshopProgress.HasStep("autumn.hammer") || WorkshopProgress.HasStep("autumn.wine") ||
                WorkshopProgress.HasStep("autumn.banana") || WorkshopProgress.HasStep("autumn.cake");
            case WorkshopSeason.Winter: return WorkshopProgress.HasStep("winter.hoe") || WorkshopProgress.HasStep("winter.dyes");
            default: return false;
        }
    }

    private string ChapterGuidance(bool unlocked, bool complete, bool ready, bool winter)
    {
        if (!unlocked)
            return $"完成{WorkshopProgress.SeasonName((WorkshopSeason)((int)_season - 1))}季并提交成果后开放。";
        if (complete) return winter ? "可在工坊展架试穿成衣。" : "成果已入库，可以回看合成。";
        if (ready) return "三件材料已备齐，打开合成台开始合成。";
        return "点选一项任务，前往本章收集关键道具。";
    }

    private static int CountMaterials(WorkshopItem[] items)
    {
        int acquired = 0;
        foreach (var item in items)
            if (WorkshopProgress.Has(item)) acquired++;
        return acquired;
    }

    private static string SeasonStatus(WorkshopSeason season)
    {
        if (WorkshopProgress.IsComplete(season)) return "已完成";
        if (!WorkshopProgress.IsUnlocked(season)) return "待开放";
        if (WorkshopProgress.CanSubmit(season)) return "可合成";
        int count = CountMaterials(WorkshopProgress.Items(season));
        return $"已开放 · {count} / 3";
    }

    private void SelectSeason(WorkshopSeason season)
    {
        if (_season == season) return;
        _season = season;
        RenderWorkshop();
    }

    private void EnterChapter() => EnterChapter(null);

    private void EnterChapter(WorkshopItem? item)
    {
        if (_leaving || !WorkshopProgress.IsUnlocked(_season)) return;
        WorkshopSeason season = _season;
        WorkshopChapterGuide.Show(season, () => WorkshopNarrativeView.PlaySeasonBriefing(season, () =>
        {
            _trackedItem = item;
            _leaving = true;
            SceneManager.LoadScene(WorkshopProgress.SceneName(season));
        }), CloseFinishModal);
    }

    private void OpenSynthesis()
    {
        if (_finishModalOpen || _leaving || WorkshopStorageView.IsOpen) return;
        _finishModalOpen = true;
        UpdateHubInput();
        WorkshopStorageView.Show(_season, CloseFinishModal, AdvanceAfterChapter);
    }

    private void CloseFinishModal()
    {
        _finishModalOpen = false;
        _dirty = true;
        UpdateHubInput();
    }

    private void AdvanceAfterChapter(WorkshopSeason completed, WorkshopSeason next)
    {
        if (_leaving) return;
        _finishModalOpen = true;
        UpdateHubInput();
        WorkshopNarrativeView.ShowChapterInterlude(completed, next,
            () =>
            {
                _season = next;
                EnterChapter();
            },
            () =>
            {
                _finishModalOpen = false;
                _season = next;
                _dirty = true;
                RenderWorkshop();
            });
    }

    private void ShowGarment()
    {
        if (_finishModalOpen || _leaving || !WorkshopProgress.IsComplete(WorkshopSeason.Winter))
            return;

        _finishModalOpen = true;
        UpdateHubInput();
        if (WorkshopNarrativeView.HasSeenFinaleDialogue)
            WorkshopWardrobe.Show(CloseFinishModal);
        else
            WorkshopNarrativeView.ShowFinale(() => WorkshopWardrobe.Show(CloseFinishModal));
    }

    private static string ChapterCraft(WorkshopSeason season)
    {
        switch (season)
        {
            case WorkshopSeason.Spring: return "养蚕";
            case WorkshopSeason.Summer: return "缫丝";
            case WorkshopSeason.Autumn: return "织造";
            default: return "染织制衣";
        }
    }
}
