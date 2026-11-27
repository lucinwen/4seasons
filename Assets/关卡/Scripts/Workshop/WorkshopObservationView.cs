using System;
using JiangnanDye;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Qingming.LevelBackground;

internal enum WorkshopObservation
{
    QingmingMulberry,
    MangzhongCocoon,
    DashuVentilation,
    QiufenOffering,
    HanluLoom,
    LidongDye,
    XiaoxueTailoring
}

internal sealed partial class WorkshopObservationView : MonoBehaviour
{
    private sealed class Definition
    {
        internal readonly WorkshopSeason Season;
        internal readonly string Id;
        internal readonly string Term;
        internal readonly string Title;
        internal readonly string Station;
        internal readonly string ChallengeName;
        internal readonly string SeasonCue;
        internal readonly string MaterialRoute;
        internal readonly string FirstMove;

        internal Definition(WorkshopSeason season, string id, string term, string title, string station,
            string challengeName = null, string seasonCue = null, string materialRoute = null, string firstMove = null)
        {
            Season = season;
            Id = "observation." + id;
            Term = term;
            Title = title;
            Station = station;
            ChallengeName = challengeName;
            SeasonCue = seasonCue;
            MaterialRoute = materialRoute;
            FirstMove = firstMove;
        }
    }

    private static readonly Definition[] Definitions =
    {
        new Definition(WorkshopSeason.Spring, "spring.qingming", "清明", "桑园里的新叶", "清明采桑", "移篮接桑",
            "清明前后桑树陆续展叶。舒展开、叶面干的嫩叶适合取用。", "移篮接桑 → 师傅照料小蚕"),
        new Definition(WorkshopSeason.Summer, "summer.mangzhong.thread", "芒种夏至", "练丝坊的备料丝路", "绕丝上轴", "丝路接接乐",
            "芒种、夏至时蚕事转入治丝备料。转动管段，把丝头接到丝轴。", "练丝坊缫丝 → 接水管 → 络丝理经备料",
            "点击线段，连接左侧丝头与右侧丝轴。连续接通三关，完成后点击返回。"),
        new Definition(WorkshopSeason.Summer, "summer.dashu.vent", "大暑", "蔟房的通风窗", "暑热观察"),
        new Definition(WorkshopSeason.Autumn, "autumn.qiufen", "秋分", "窗光下的丝样", "借光验丝", "借光验丝",
            "秋分后白昼渐短。织坊在夜织前，用剩下的窗光检查生丝的粗节和结头。", "夏季丝样 → 对光验丝 → 寒露上机",
            "把真实丝样放到后排，再切侧窗找光。天暗时点灯补光。"),
        new Definition(WorkshopSeason.Autumn, "autumn.hanlu", "寒露", "看经线", "看经线", "双色走梭",
            "寒露晨凉，窗沿可能结露。丝轴沾了露，要整备妥当才能上机。", "验过的丝 → 理经走梭织绸 → 立冬试染",
            "先取状态合适的丝轴，再看样带抬经线、走梭。"),
        new Definition(WorkshopSeason.Autumn, "winter.lidong", "秋分", "染坊里的色样", "染色", "两阶染色",
            "先采集天然染材并研磨，选好染料，将缬拖入染缸。", "缬 → 研磨与染色 → 寒露攀花",
            "调节染缸浓度，轻蘸或浸染，对上两阶颜色后确认。"),
        new Definition(WorkshopSeason.Winter, "winter.xiaoxue", "小雪", "从试染到成衣", "看成衣工序", "排布",
            "小雪时冷空气更活跃。逢冷雨先避雨晾布，做夹衣还得留里襟料。", "晾布 → 排料 → 剪帛",
            "先找避雨又通风的晾架。布干后再顺纹摆衣样，给里襟留一块整布。")
    };

    private static readonly Color[] Accents =
    {
        new Color32(47, 86, 61, 255),
        new Color32(43, 84, 80, 255),
        new Color32(126, 73, 51, 255),
        new Color32(54, 75, 101, 255)
    };

    private const float ArtWidth = 1160f;
    private const float ArtHeight = 653f;
    private static readonly Rect ArtBounds = new Rect(0f, 0f, ArtWidth, ArtHeight);
    private static readonly Color InsetPaper = new Color32(239, 225, 194, 255);
    private static readonly Color LightSurface = new Color32(252, 243, 222, 255);
    private static readonly Color ErrorInk = new Color32(128, 60, 48, 255);
    private static readonly Color HotspotSurface = new Color32(255, 243, 190, 255);
    private static readonly Color FocusInk = new Color32(224, 170, 65, 255);

    private static WorkshopObservationView current;
    private static int lastClosedFrame = -1;

    private readonly List<Button> navigation = new List<Button>();
    private readonly List<GameObject> focusMarks = new List<GameObject>();
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private RectTransform frame;
    private RectTransform page;
    private RectTransform art;
    private RectTransform infoPanel;
    private WorkshopObservation observation;
    private Action onClosed;
    private Action onWinterPrepared;
    private bool IsWinterPreparation => !readOnly && observation == WorkshopObservation.XiaoxueTailoring;
    private GameObject priorSelection;
    private float priorTimeScale;
    private bool readOnly;
    private bool showBriefing;
    private bool showSongJin;
    private bool earnedRewardThisRun;
    private int stage;
    private int rows;

    internal static bool IsOpen => current != null;
    internal static bool BlocksWorldInput => IsOpen || lastClosedFrame == Time.frameCount;
    internal static WorkshopObservation? ActiveGame => current != null && !current.readOnly &&
        (IsSeasonGame(current.observation) || current.IsWinterPreparation) ? current.observation : (WorkshopObservation?)null;

    internal static WorkshopSeason Season(WorkshopObservation id) => Definitions[(int)id].Season;
    internal static string RecordTitle(WorkshopObservation id) => Definitions[(int)id].Title;
    internal static string StationLabel(WorkshopObservation id) => Definitions[(int)id].Station;
    internal static string SeasonGameTitle(WorkshopObservation id) => Definitions[(int)id].ChallengeName;
    internal static bool IsSeasonGame(WorkshopObservation id) => id == WorkshopObservation.QingmingMulberry || id == WorkshopObservation.MangzhongCocoon;
    internal static bool HasRecord(WorkshopObservation id) => WorkshopProgress.HasObservation(Definitions[(int)id].Id);

    internal static IEnumerable<WorkshopObservation> ObservationsFor(WorkshopSeason season)
    {
        for (int i = 0; i < Definitions.Length; i++)
            if (Definitions[i].Season == season)
                yield return (WorkshopObservation)i;
    }

    internal static void Show(WorkshopObservation id, Action onClosed = null)
    {
        if (id != WorkshopObservation.MangzhongCocoon || IsOpen) return;
        if (!WorkshopProgress.Has(WorkshopItem.Spool))
        {
            SimplePopup.Show("先找到线轴", "先备水拾柴，到煮茧锅寻找线轴，再回右侧缫丝车绕丝上轴。", "知道了");
            return;
        }
        if (WorkshopProgress.SeasonalOrderEnabled && IsAtOwnWorkshop(id) && MissingOrderInput(id) is string guide)
        {
            SimplePopup.Show("先备齐这道工序", guide, "知道了");
            return;
        }
        Open(id, onClosed, false);
    }

    internal static WorkshopProgress.ChallengeWork OrderWork(WorkshopObservation id) =>
        WorkshopProgress.SeasonalOrderWork(Definitions[(int)id].Id);

    internal static bool HasOrderWork(WorkshopObservation id) => OrderWork(id) != null;

    internal static bool HasSeasonOrderDeliveries(WorkshopSeason season) => WorkshopWordRules.HasDelivery(season);

    private static bool IsAtOwnWorkshop(WorkshopObservation id) =>
        SceneManager.GetActiveScene().name == WorkshopProgress.SceneName(Season(id));

    private static string MissingOrderInput(WorkshopObservation id)
    {
        switch (id)
        {
            case WorkshopObservation.QiufenOffering when !HasOrderWork(WorkshopObservation.MangzhongCocoon):
                return "先到夏季练丝坊右侧缫丝车接通丝路，再带丝样到秋分验丝。";
            case WorkshopObservation.HanluLoom when !HasOrderWork(WorkshopObservation.QiufenOffering):
                return "先到秋分窗台验生丝，把验过的丝轴带回织机。";
            case WorkshopObservation.HanluLoom when !AutumnLevelState.HasLoomRepaired || !AutumnLevelState.HasShuttle:
                return "先修好织机并取得梭子，再把寒露整理过的丝织成绸。";
            case WorkshopObservation.LidongDye when !HasOrderWork(WorkshopObservation.HanluLoom) || !WorkshopProgress.Has(WorkshopItem.RawSilkCloth):
                return "寒露织样和正式整绸都备好后，才能在立冬试色。";
            case WorkshopObservation.XiaoxueTailoring when !WorkshopProgress.Has(WorkshopItem.DyedCloth):
                return "先完成立冬试色和正式染整幅，再到小雪裁衣台。";
            default:
                return null;
        }
    }

    internal static bool RecordQingmingHarvest(int leaves)
    {
        const WorkshopObservation harvest = WorkshopObservation.QingmingMulberry;
        string id = Definitions[(int)harvest].Id;
        string result = $"{SpringSummerNpcDialogue.HarvestOrchardName} · 接到嫩叶 {leaves} 片";
        if (WorkshopProgress.SeasonalOrderEnabled && IsAtOwnWorkshop(harvest))
            WorkshopProgress.RecordSeasonalOrderWork(id, 0, 100, result, null);
        return WorkshopProgress.RecordChallenge(id, 0, 100, result, null);
    }


    internal static void ShowRecord(WorkshopObservation id, Action onClosed = null)
    {
        if (HasRecord(id))
            Open(id, onClosed, true);
    }

    internal static void ShowWinterPreparation(Action onPrepared = null)
    {
        if (!WorkshopProgress.Has(WorkshopItem.DyedCloth)) return;
        if (onPrepared != null && WorkshopWordRules.WinterStage != WorkshopWinterStage.Preparing) return;
        if (onPrepared == null && !WorkshopProgress.Has(WorkshopItem.CutPieces)) return;
        Open(WorkshopObservation.XiaoxueTailoring, null, false, onPrepared);
    }

    private static void Open(WorkshopObservation id, Action onClosed, bool readOnly, Action onWinterPrepared = null,
        DyeColor? directColor = null, DyeRun directRun = null)
    {
        if (IsOpen) return;

        var selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var root = WorkshopUi.NewCanvas("Workshop Observation", 23000);
        WorkshopUi.Block(root, id == WorkshopObservation.MangzhongCocoon && !readOnly
            ? new Color32(0,0,0,168) : new Color32(21,34,29,219));
        current = root.gameObject.AddComponent<WorkshopObservationView>();
        current.frame = root;
        current.onClosed = onClosed;
        current.onWinterPrepared = onWinterPrepared;
        current.directDyeColor = directColor;
        current.directDyeRun = directRun;
        current.readOnly = readOnly;
        current.priorSelection = selection;
        current.priorTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        current.PrepareActivity(id);
        current.showBriefing = !current.IsDirectDyeing && !readOnly && id != WorkshopObservation.MangzhongCocoon && IsSeasonGame(id) && !HasRecord(id);
        current.Render();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        current = null;
        lastClosedFrame = -1;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Tab) || EventSystem.current == null || navigation.Count == 0) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        int index = navigation.FindIndex(button => button.gameObject == selected);
        int step = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1;
        if (index < 0)
            index = step == 1 ? navigation.Count - 1 : 0;
        Select(navigation[(index + step + navigation.Count) % navigation.Count].gameObject);
    }

    private void Close()
    {
        if (current != this) return;
        if (observation == WorkshopObservation.MangzhongCocoon) CancelXiazhiRound();
        bool direct = IsDirectDyeing;
        var completedColor = directDyeSucceeded ? directDyeColor : null;
        RestoreTime();
        WorkshopUi.CloseCanvas(frame);
        Select(priorSelection);
        if (direct) onDirectDyeClosed?.Invoke(completedColor);
        else onClosed?.Invoke();
    }

    private void OnDestroy()
    {
        if (current == this) RestoreTime();
    }

    private void RestoreTime()
    {
        current = null;
        lastClosedFrame = Time.frameCount;
        Time.timeScale = priorTimeScale;
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target != null && target.activeInHierarchy ? target : null);
    }

    private void Render()
    {
        string previousFocus = FocusPath();
        if (page != null)
        {
            Select(null);
            page.gameObject.SetActive(false);
            Destroy(page.gameObject);
        }

        navigation.Clear();
        focusMarks.Clear();
        rows = 0;
        if (observation == WorkshopObservation.MangzhongCocoon && !readOnly)
        {
            DrawImmersiveXiazhi();
            return;
        }
        var definition = Definitions[(int)observation];
        Color accent = Accents[(int)definition.Season];
        page = DrawWorkshopPaper(frame, "Observation Panel", new Rect(88, 77, 1744, 926),
            WorkshopUi.Paper, WorkshopUi.Bronze);

        WorkshopUi.Label(page, "Season and Solar Term", new Rect(28, 19, 560, 40),
            WorkshopProgress.SeasonName(definition.Season) + " · " + definition.Term, 28, accent);
        if (IsSeasonGame(observation)) DrawSeasonalHandoff(definition, accent);
        WorkshopUi.Label(page, "Observation Title", new Rect(28, 61, 1320, 78),
            IsDirectDyeing ? "染色" : ChallengeIsActive ? definition.ChallengeName : definition.Title, 54, WorkshopUi.Ink);
        if (readOnly)
            WorkshopUi.Label(page, "Record Mode", new Rect(1294, 68, 412, 39),
                IsSeasonGame(observation) ? "已通关 · 残页已收集" : "织录 · 已观察", 31, accent, TextAnchor.MiddleRight);
        else if (ChallengeIsActive && !IsWinterPreparation)
            DrawChallengeStatus(accent);
        WorkshopUi.Fill(page, "Heading Rule", new Rect(28, 144, 1688, 2), WorkshopUi.Line);
        if (showBriefing)
        {
            DrawWorkbenchBriefing(definition, accent);
            return;
        }

        var frameBevel = WorkshopUi.PixelCard(page, "Illustration Frame", new Rect(28, 163, 1172, 665),
            accent, WorkshopUi.Bronze);
        art = WorkshopUi.Rect(frameBevel, "Observation Illustration", new Rect(6, 6, ArtWidth, ArtHeight));
        WorkshopUi.Fill(art, "Illustration Surface", ArtBounds, InsetPaper);
        infoPanel = DrawWorkshopPaper(page, "Observation Instructions", new Rect(1218, 163, 498, 665),
            LightSurface, WorkshopUi.Line);

        DrawActivity();
        if (IsSeasonGame(observation)) DrawChallengeReward();

        bool xiazhiGame = observation == WorkshopObservation.MangzhongCocoon && ChallengeIsActive;
        var exit = WorkshopUi.PixelButton(page, "Return From Observation", new Rect(1473, 848, 243, 58),
            xiazhiGame ? "返回夏季 · Esc" : ChallengeIsActive ? "退出小游戏 · Esc" : onClosed == null && !readOnly ? "返回关卡 · Esc" : "返回 · Esc",
            Close, false, 27);
        Button replay = ChallengeIsActive ? DrawChallengeControls() : null;
        RegisterButton(exit);
        if (ChallengeIsActive)
            ApplyWorkbenchReadingFont();
        var prior = previousFocus != null ? page.Find(previousFocus) : null;
        BindNavigation(replay ?? prior?.GetComponent<Button>());
    }

    private void DrawSeasonalHandoff(Definition definition, Color accent)
    {
        var terms = WorkshopArt.SolarNames(definition.Season);
        int position = Array.IndexOf(terms, definition.Term);
        if (position >= 0)
            WorkshopUi.Label(page, "Solar Year Position", new Rect(161, 20, 242, 39),
                $"二十四节气 {((int)definition.Season * terms.Length + position + 1):00}/24", 21, accent);
        if (!showBriefing)
            WorkshopUi.Label(page, "Seasonal Work Cue", new Rect(414, 19, 1293, 41), definition.SeasonCue, 23,
                WorkshopUi.Ink, TextAnchor.MiddleRight, WorkshopArt.ReadingFont);
        WorkshopUi.Label(page, "Production Handoff", new Rect(621, 111, 1086, 31), definition.MaterialRoute, 22,
            accent, TextAnchor.MiddleRight, WorkshopArt.ReadingFont);
    }

    private void DrawWorkbenchBriefing(Definition definition, Color accent)
    {
        var bounds = new Rect(28, 163, 1688, 660);
        var scene = DrawWorkshopPaper(page, "Season Work Briefing", bounds, accent, WorkshopUi.Bronze);
        string backdrop = observation == WorkshopObservation.XiaoxueTailoring
            ? "WorkshopGameArt/XiaoxueRainCourtyard"
            : "WorkshopGuides/" + definition.Season + "_ChapterGuide";
        var place = Resources.Load<Sprite>(backdrop);
        WorkshopUi.Picture(scene, "Workshop Region", new Rect(8, 8, 854, 644), place, stretch: true);
        var card = DrawWorkshopPaper(scene, "Season Work Card", new Rect(878, 26, 782, 602),
            LightSurface, WorkshopUi.Bronze);
        var font = WorkshopArt.ReadingFont;
        WorkshopUi.Label(card, "Season Work Reason Heading", new Rect(42, 40, 689, 54), "为什么是" + definition.Term,
            39, accent, TextAnchor.MiddleLeft, font);
        WorkshopUi.Label(card, "Season Work Reason", new Rect(42, 111, 689, 154), definition.SeasonCue,
            32, WorkshopUi.Ink, TextAnchor.UpperLeft, font);
        WorkshopUi.Fill(card, "Work Card Rule", new Rect(42, 278, 691, 2), WorkshopUi.Line);
        WorkshopUi.Label(card, "Season Work First Move", new Rect(42, 308, 689, 142), definition.FirstMove,
            30, WorkshopUi.Ink, TextAnchor.UpperLeft, font);
        var begin = WorkshopUi.PixelButton(card, "Start Season Work", new Rect(42, 493, 691, 68),
            "开工", () => { showBriefing = false; Render(); }, true, 33, accent);
        var leave = WorkshopUi.PixelButton(page, "Return From Observation", new Rect(1473, 848, 243, 58),
            "返回 · Esc", Close, false, 27);
        RegisterButton(begin);
        RegisterButton(leave);
        ApplyWorkbenchReadingFont();
        BindNavigation(begin);
    }

    private void ApplyWorkbenchReadingFont()
    {
        var readingFont = observation == WorkshopObservation.MangzhongCocoon ? WorkshopArt.PixelFont : WorkshopArt.ReadingFont;
        foreach (var label in page.GetComponentsInChildren<Text>(true))
        {
            label.font = readingFont;
            if (label.name == "Observation Title") label.fontStyle = FontStyle.Bold;
        }
    }

    private string FocusPath()
    {
        if (page == null || EventSystem.current == null) return null;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || selected.transform == page || !selected.transform.IsChildOf(page)) return null;
        Transform item = selected.transform;
        string path = item.name;
        while (item.parent != page)
        {
            item = item.parent;
            path = item.name + "/" + path;
        }
        return path;
    }

    private void BindNavigation(Button initial = null)
    {
        for (int i = 0; i < navigation.Count; i++)
        {
            Button previous = navigation[(i + navigation.Count - 1) % navigation.Count];
            Button next = navigation[(i + 1) % navigation.Count];
            navigation[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = previous,
                selectOnLeft = previous,
                selectOnDown = next,
                selectOnRight = next
            };
        }
        Button focus = initial != null && initial.IsInteractable() ? initial : navigation.Find(button => button.IsInteractable());
        Select(focus != null ? focus.gameObject : null);
    }

    private void LateUpdate()
    {
        if (EventSystem.current == null) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        for (int i = 0; i < navigation.Count; i++)
        {
            bool visible = navigation[i].gameObject == selected;
            if (focusMarks[i].activeSelf != visible)
                focusMarks[i].SetActive(visible);
        }
    }

    private void Copy(string heading, string body, bool warning = false)
    {
        var recovery = warning && ChallengeIsActive ? Resources.Load<Sprite>("WorkshopGameArt/RecoverySeal") : null;
        if (recovery != null)
            WorkshopUi.Picture(infoPanel, "Recovery Cue", new Rect(420, 20, 47, 53), recovery);
        WorkshopUi.Label(infoPanel, "Instruction Heading", new Rect(27, 28, recovery == null ? 444 : 384, 62),
            heading, 35, warning ? ErrorInk : Accents[(int)Season(observation)], TextAnchor.UpperLeft);
        var label = WorkshopUi.Label(infoPanel, "Instruction Body", new Rect(27, 102, 444, 195),
            body, 30, WorkshopUi.Ink, TextAnchor.UpperLeft);
        label.lineSpacing = 1.20f;
        WorkshopUi.Fill(infoPanel, "Instruction Rule", new Rect(27, 309, 444, 2), WorkshopUi.Line);
    }

    private void Act(string name, string label, Action invoke, bool primary = false)
    {
        var button = WorkshopUi.PixelButton(infoPanel, name, new Rect(27, 338 + rows * 73f, 444, 62),
            label, () =>
            {
                WorkshopAudioFeedback.Click();
                invoke();
            }, primary, 28, Accents[(int)Season(observation)]);
        rows++;
        RegisterButton(button);
    }

    private void RegisterButton(Button button)
    {
        var marker = WorkshopUi.Fill(button.transform, "Keyboard Focus", new Rect(7, 12, 5, 32), FocusInk);
        marker.gameObject.SetActive(false);
        navigation.Add(button);
        focusMarks.Add(marker.gameObject);
    }

    private void Hotspot(string name, Rect bounds, Action action)
    {
        if (readOnly) return;
        var button = WorkshopUi.Button(art, name, bounds, "", () =>
        {
            WorkshopAudioFeedback.Click();
            action();
        }, Color.clear);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.targetGraphic.color = HotspotSurface;
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.20f);
        colors.selectedColor = colors.normalColor;
        colors.pressedColor = new Color(1f, 1f, 1f, 0.38f);
        button.colors = colors;
    }

    private void Art(string name, string resource, Rect bounds, bool stretch = false)
    {
        if (!sprites.TryGetValue(resource, out var sprite))
        {
            sprite = Resources.Load<Sprite>(resource);
            sprites.Add(resource, sprite);
        }
        WorkshopUi.Picture(art, name, bounds, sprite, stretch: stretch);
    }

    private void Layer(string resource) => Art(resource, "WorkshopObservation/" + resource, ArtBounds, true);

    private void Prop(string resource, Rect bounds) => Art(resource, "WorkshopObservation/" + resource, bounds);

    private void Texture(string name, string resource, Rect bounds)
    {
        var raw = WorkshopUi.Rect(art, name, bounds).gameObject.AddComponent<RawImage>();
        raw.texture = Resources.Load<Texture2D>(resource);
        raw.enabled = raw.texture != null;
        raw.raycastTarget = false;
    }

    private void Tag(string name, string label, Rect bounds, bool warning = false)
    {
        var tag = WorkshopUi.PixelCard(art, name, bounds, warning ? ErrorInk : WorkshopUi.Ink, WorkshopUi.Bronze);
        WorkshopUi.Label(tag, "Tag Text", new Rect(6, 0, bounds.width - 12, bounds.height), label, 25,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
    }

    private static Rect At(float cx, float cy, float width, float height) =>
        new Rect((cx - width * 0.5f) * ArtWidth, (cy - height * 0.5f) * ArtHeight,
            width * ArtWidth, height * ArtHeight);

    private void Step(int value)
    {
        stage = value;
        Render();
    }

    private void Finish()
    {
        if (readOnly || ChallengeIsActive) return;
        WorkshopProgress.RecordObservation(Definitions[(int)observation].Id);
        Step(CompletionStage(observation));
    }

    private void Link(WorkshopObservation target)
    {
        PrepareActivity(target);
        Render();
    }

    private static int CompletionStage(WorkshopObservation id)
    {
        switch (id)
        {
            case WorkshopObservation.MangzhongCocoon:
            case WorkshopObservation.LidongDye:
            case WorkshopObservation.XiaoxueTailoring:
                return 3;
            default:
                return 2;
        }
    }

    private void Restart()
    {
        if (!readOnly && !ChallengeIsActive)
            Act("Retry Observation", "再看一次", () => Step(0));
    }
}
