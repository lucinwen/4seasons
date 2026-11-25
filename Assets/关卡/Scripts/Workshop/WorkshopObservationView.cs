using System;
using System.Collections;
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
    XiaoxueTailoring,
    DaxueSewing,
    DongzhiFitting,
    MangzhongCocoonHarvest
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
        new Definition(WorkshopSeason.Spring, "spring.qingming", "清明", "桑园里的新叶", "桑园"),
        new Definition(WorkshopSeason.Summer, "summer.mangzhong.thread", "芒种", "簇架上的丝路", "络纬整经", "丝路接接乐",
            "芒种时节，簇架上的蚕茧已收好。转动线段，把丝头接到丝轴。", "蚕茧簇架 → 接通丝路 → 整理丝样",
            "点击线段，连接左侧丝头与右侧丝轴。接通一张即可完成。"),
        new Definition(WorkshopSeason.Summer, "summer.dashu.vent", "大暑", "蔟房的通风窗", "暑热观察"),
        new Definition(WorkshopSeason.Autumn, "autumn.qiufen", "秋分", "窗光下的丝样", "借光验丝", "借光验丝",
            "秋分后白昼渐短。织坊在夜织前，用剩下的窗光检查生丝的粗节和结头。", "夏季丝样 → 对光验丝 → 寒露上机",
            "① 点后排空盘放丝轴。\n② 切到侧窗，挪开挡光的酒坛。\n③ 点有接头的丝段，再挂验收签。"),
        new Definition(WorkshopSeason.Autumn, "autumn.hanlu", "寒露", "织造", "织机"),
        new Definition(WorkshopSeason.Winter, "winter.lidong", "立冬", "染坊里的色样", "看色样", "两单一缸",
            "立冬这班收到浙南十一月一批栀子果料。熟果的黄色素更适合这张工单。", "寒露素绸 → 栀黄试色与染布 → 小雪裁衣",
            "① 点红黄色熟果，再点石臼研料。\n② 短蘸上色，晾看干样。\n③ 对照左侧色样，完成深浅两块。"),
        new Definition(WorkshopSeason.Winter, "winter.xiaoxue", "小雪", "按样裁衣", "裁衣台"),
        new Definition(WorkshopSeason.Winter, "winter.daxue.sewing", "大雪", "拼缝夹衣", "主厅针线台", "合缝翻衣",
            "大雪后天气更冷，夹衣要能叠穿。表里缝牢，袖下也要留出抬手的余量。", "小雪衣片＋素里绢 → 合缝翻面 → 冬至验衣",
            "① 找到成对的布边记号。\n② 直边走长针，弯处收短针。\n③ 留口翻衣，翻正后补针。"),
        new Definition(WorkshopSeason.Winter, "winter.dongzhi.fitting", "冬至", "冬至交衣", "主厅试衣台", "试衣改合身",
            "冬至贺冬要穿着出门见客。衣襟得合得严，抬手行礼时袖根也不能勒。", "大雪夹衣 → 叠穿试衣与返修 → 新衣上架",
            "① 拖衣袖向上，试试能抬多高。\n② 绷紧处可松一点缝份，再抬手。\n③ 扣好衣襟，交付新衣。"),
        new Definition(WorkshopSeason.Summer, "summer.mangzhong.harvest", "芒种", "春茧收存记录", "采收干茧", "落茧快收",
            "芒种前后有江南产区采收春茧。挑选成熟、干爽且完整的茧，分批收存。", "四轨收茧 → 干茧配批",
            "① 开始后蚕茧会沿四轨落下。\n② 从最下方一排开始，点深色篮或按轨道键。\n③ 避开浅色篮，在金线前收齐好茧。")
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
    private Action onGarmentDelivery;
    private GameObject priorSelection;
    private float priorTimeScale;
    private bool readOnly;
    private bool showBriefing;
    private bool resumeFromBriefing;
    private string briefingCheckpoint;
    private Button preferredAction;
    private int navigationStamp;
    private bool earnedRewardThisRun;
    private int stage;
    private int rows;

    internal static bool IsOpen => current != null;
    internal static bool BlocksWorldInput => IsOpen || lastClosedFrame == Time.frameCount;
    internal static WorkshopObservation? ActiveGame => current != null && !current.readOnly &&
        IsSeasonGame(current.observation) ? current.observation : (WorkshopObservation?)null;
    internal static bool CanPauseGameplay => ActiveGame.HasValue && !current.showBriefing;

    internal static WorkshopSeason Season(WorkshopObservation id) => Definitions[(int)id].Season;
    internal static string RecordTitle(WorkshopObservation id) => Definitions[(int)id].Title;
    internal static string StationLabel(WorkshopObservation id) => Definitions[(int)id].Station;
    internal static string SeasonGameTitle(WorkshopObservation id) => Definitions[(int)id].ChallengeName;
    internal static bool IsSeasonGame(WorkshopObservation id) => !string.IsNullOrEmpty(SeasonGameTitle(id));
    internal static bool HasRecord(WorkshopObservation id) => WorkshopProgress.HasObservation(Definitions[(int)id].Id);
    private static bool IsRetiredObservation(WorkshopObservation id) =>
        id == WorkshopObservation.QingmingMulberry || id == WorkshopObservation.HanluLoom ||
        id == WorkshopObservation.XiaoxueTailoring;

    internal static bool IsGarmentWorkshop(WorkshopObservation id) =>
        id == WorkshopObservation.DaxueSewing || id == WorkshopObservation.DongzhiFitting;

    internal static bool CanPracticeGarmentWorkshop(WorkshopObservation id) =>
        IsGarmentWorkshop(id) && WorkshopProgress.IsComplete(WorkshopSeason.Winter);

    internal static void OpenRecordOrGarmentPractice(WorkshopObservation id, Action onClosed = null)
    {
        if (!IsRetiredObservation(id) && HasRecord(id)) ShowRecord(id, onClosed);
        else if (CanPracticeGarmentWorkshop(id)) Show(id, onClosed);
        else onClosed?.Invoke();
    }

    private bool IsFormalGarmentOrder => onGarmentDelivery != null;

    internal static bool HasGarmentWorkshopDeliveries() =>
        HasOrderWork(WorkshopObservation.DaxueSewing) && HasOrderWork(WorkshopObservation.DongzhiFitting);

    internal static bool ShowGarmentOrder(WorkshopObservation id, Action back, Action deliver)
    {
        if (!IsGarmentWorkshop(id) || IsOpen || deliver == null || !WorkshopProgress.CanSubmit(WorkshopSeason.Winter)) return false;
        if (id == WorkshopObservation.DongzhiFitting && (!HasOrderWork(WorkshopObservation.DaxueSewing) || !WorkshopProgress.GarmentSewn))
            return false;
        Open(id, back, false, deliver);
        return true;
    }

    private void SaveGarmentSewingResult(int sleeveAllowance, int turningOpening)
    {
        if (IsFormalGarmentOrder && observation == WorkshopObservation.DaxueSewing && !HasOrderWork(observation))
            WorkshopProgress.SaveGarmentSewing(sleeveAllowance, turningOpening);
    }

    private void SaveGarmentFittingResult(int alterations, int claspStyle)
    {
        if (IsFormalGarmentOrder && observation == WorkshopObservation.DongzhiFitting && !HasOrderWork(observation))
            WorkshopProgress.SaveGarmentFitting(alterations, claspStyle);
    }

    internal static IEnumerable<WorkshopObservation> ObservationsFor(WorkshopSeason season)
    {
        for (int i = 0; i < Definitions.Length; i++)
        {
            var id = (WorkshopObservation)i;
            if (Definitions[i].Season == season && !IsRetiredObservation(id))
                yield return id;
        }
    }

    internal static void Show(WorkshopObservation id, Action onClosed = null)
    {
        if (IsRetiredObservation(id) || IsOpen) return;
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

    internal static bool HasSeasonOrderDeliveries(WorkshopSeason season)
    {
        switch (season)
        {
            case WorkshopSeason.Spring: return SpringSummerNpcDialogue.HasHarvestedLeaves;
            case WorkshopSeason.Summer: return HasOrderWork(WorkshopObservation.MangzhongCocoon);
            case WorkshopSeason.Autumn: return HasOrderWork(WorkshopObservation.QiufenOffering);
            case WorkshopSeason.Winter: return HasOrderWork(WorkshopObservation.LidongDye);
            default: return true;
        }
    }

    private static bool IsAtOwnWorkshop(WorkshopObservation id) =>
        SceneManager.GetActiveScene().name == WorkshopProgress.SceneName(Season(id));

    private static string MissingOrderInput(WorkshopObservation id)
    {
        switch (id)
        {
            case WorkshopObservation.QiufenOffering when !HasOrderWork(WorkshopObservation.MangzhongCocoon):
                return "先到夏季蚕茧簇架接通丝路，再带丝样到秋分验丝。";
            case WorkshopObservation.LidongDye when !WorkshopProgress.Has(WorkshopItem.RawSilkCloth):
                return "先在秋季织成生坯绸，再到立冬染坊试色。";
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
        if (!IsRetiredObservation(id) && HasRecord(id))
            Open(id, onClosed, true);
    }

    private static void Open(WorkshopObservation id, Action onClosed, bool readOnly, Action garmentDelivery = null)
    {
        if (IsOpen) return;

        var selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var root = WorkshopUi.NewCanvas("Workshop Observation", 23000);
        WorkshopUi.Block(root, new Color32(21, 34, 29, 219));
        current = root.gameObject.AddComponent<WorkshopObservationView>();
        current.frame = root;
        current.onClosed = onClosed;
        current.onGarmentDelivery = garmentDelivery;
        current.readOnly = readOnly;
        current.priorSelection = selection;
        current.priorTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        current.PrepareActivity(id);
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
        if (WorkshopMinigameMenu.BlocksGameplayInput) return;
        if (!CanPauseGameplay && Input.GetKeyDown(KeyCode.Escape))
        {
            if (showBriefing && resumeFromBriefing) ResumeWorkbench();
            else Close();
            return;
        }

        if (showBriefing) return;
        TickCocoonRun();
        if (!Input.GetKeyDown(KeyCode.Tab) || EventSystem.current == null || navigation.Count == 0) return;
        var available = navigation.FindAll(button => button != null && button.IsActive() && button.IsInteractable());
        if (available.Count == 0) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        int index = available.FindIndex(button => button.gameObject == selected);
        int step = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1;
        if (index < 0)
            index = step == 1 ? available.Count - 1 : 0;
        Select(available[(index + step + available.Count) % available.Count].gameObject);
    }

    private void Close()
    {
        if (current != this) return;
        if (observation == WorkshopObservation.MangzhongCocoon) ClearXiazhiHint();
        RestoreTime();
        WorkshopUi.CloseCanvas(frame);
        Select(priorSelection);
        onClosed?.Invoke();
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
        preferredAction = null;
        var definition = Definitions[(int)observation];
        Color accent = Accents[(int)definition.Season];
        page = DrawWorkshopPaper(frame, "Observation Panel", new Rect(88, 77, 1744, 926),
            WorkshopUi.Paper, WorkshopUi.Bronze);

        WorkshopUi.Label(page, "Season and Solar Term", new Rect(28, 19, 560, 40),
            WorkshopProgress.SeasonName(definition.Season) + " · " + definition.Term, 28, accent);
        if (IsSeasonGame(observation)) DrawSeasonalHandoff(definition, accent);
        WorkshopUi.Label(page, "Observation Title", new Rect(28, 61, 1320, 78),
            ChallengeIsActive ? definition.ChallengeName : definition.Title, 54, WorkshopUi.Ink);
        if (readOnly)
            WorkshopUi.Label(page, "Record Mode", new Rect(1294, 68, 412, 39),
                IsSeasonGame(observation) ? "已通关 · 残页已收集" : "织录 · 已观察", 31, accent, TextAnchor.MiddleRight);
        else if (ChallengeIsActive)
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
        if (ChallengeIsActive &&
            (observation == WorkshopObservation.MangzhongCocoon || observation == WorkshopObservation.MangzhongCocoonHarvest))
            DrawSummerGameSwitch(accent);

        bool summerGame = ChallengeIsActive &&
            (observation == WorkshopObservation.MangzhongCocoon || observation == WorkshopObservation.MangzhongCocoonHarvest);
        string exitLabel = summerGame ? "返回夏季" : onClosed == null && !readOnly && !ChallengeIsActive ? "返回关卡" : "返回";
        if (!CanPauseGameplay) exitLabel += " · Esc";
        var exit = WorkshopUi.PixelButton(page, "Return From Observation", new Rect(1473, 848, 243, 58),
            exitLabel, Close, false, 27);
        Button replay = ChallengeIsActive ? DrawChallengeControls() : null;
        if (ChallengeIsActive && !GameComplete && !readOnly)
        {
            var help = WorkshopUi.PixelButton(page, "Workbench Help", new Rect(1015, 848, 185, 58),
                "节气工单", ReviewWorkbenchBriefing, false, 26);
            RegisterButton(help);
        }
        RegisterButton(exit);
        if (ChallengeIsActive)
            ApplyWorkbenchReadingFont();
        var prior = previousFocus != null ? page.Find(previousFocus) : null;
        BindNavigation(replay ?? preferredAction ?? prior?.GetComponent<Button>());
    }

    private void DrawSummerGameSwitch(Color accent)
    {
        bool threadGame = observation == WorkshopObservation.MangzhongCocoon;
        var button = WorkshopUi.PixelButton(page, "Switch Mangzhong Game", new Rect(1402, 19, 314, 44),
            threadGame ? "落茧快收 · 试玩" : "返回芒种理线", () =>
            {
                if (WorkshopMinigameMenu.BlocksGameplayInput) return;
                WorkshopAudioFeedback.Click();
                if (threadGame) ClearXiazhiHint();
                Link(threadGame ? WorkshopObservation.MangzhongCocoonHarvest : WorkshopObservation.MangzhongCocoon);
            }, false, 25, accent);
        RegisterButton(button);
    }

    private void ReviewWorkbenchBriefing()
    {
        var heading = infoPanel.Find("Instruction Heading")?.GetComponent<Text>();
        briefingCheckpoint = heading != null ? heading.text : null;
        resumeFromBriefing = true;
        showBriefing = true;
        WorkshopAudioFeedback.Click();
        Render();
    }

    private void ResumeWorkbench()
    {
        WorkshopAudioFeedback.Click();
        showBriefing = false;
        resumeFromBriefing = false;
        Render();
    }

    private void DrawSeasonalHandoff(Definition definition, Color accent)
    {
        var terms = WorkshopArt.SolarNames(definition.Season);
        int position = Array.IndexOf(terms, definition.Term);
        if (position >= 0)
            WorkshopUi.Label(page, "Solar Year Position", new Rect(161, 20, 242, 39),
                $"二十四节气 {((int)definition.Season * terms.Length + position + 1):00}/24", 21, accent);
        WorkshopUi.Label(page, "Production Handoff", new Rect(621, 111, 1086, 31), definition.MaterialRoute, 22,
            accent, TextAnchor.MiddleRight, WorkshopArt.ReadingFont);
    }

    private void DrawWorkbenchBriefing(Definition definition, Color accent)
    {
        var bounds = new Rect(28, 163, 1688, 660);
        var scene = DrawWorkshopPaper(page, "Season Work Briefing", bounds, accent, WorkshopUi.Bronze);
        string backdrop = "WorkshopGuides/" + definition.Season + "_ChapterGuide";
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
        if (resumeFromBriefing && !string.IsNullOrEmpty(briefingCheckpoint))
            WorkshopUi.Label(card, "Season Work Checkpoint", new Rect(42, 449, 689, 37),
                "当前进度 · " + briefingCheckpoint, 24, accent, TextAnchor.MiddleLeft, font);
        var begin = WorkshopUi.PixelButton(card, "Start Season Work", new Rect(42, 493, 691, 68),
            resumeFromBriefing ? "继续刚才的步骤" : "开工", ResumeWorkbench, true, 33, accent);
        var leave = WorkshopUi.PixelButton(page, "Return From Observation", new Rect(1473, 848, 243, 58),
            resumeFromBriefing ? "继续游戏 · Esc" : "返回 · Esc",
            resumeFromBriefing ? (Action)ResumeWorkbench : Close, false, 27);
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
        var available = navigation.FindAll(button => button != null && button.IsActive() && button.IsInteractable());
        for (int i = 0; i < available.Count; i++)
        {
            Button previous = available[(i + available.Count - 1) % available.Count];
            Button next = available[(i + 1) % available.Count];
            available[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = previous,
                selectOnLeft = previous,
                selectOnDown = next,
                selectOnRight = next
            };
        }
        Button focus = initial != null && available.Contains(initial) ? initial : available.Find(button => button.IsInteractable());
        Select(focus != null ? focus.gameObject : null);
    }

    private void LateUpdate()
    {
        if (WorkshopMinigameMenu.BlocksGameplayInput || EventSystem.current == null) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        int stamp = 17;
        foreach (var button in navigation)
            stamp = unchecked(stamp * 31 + (button.IsActive() && button.IsInteractable() ? 1 : 0));
        if (stamp != navigationStamp)
        {
            navigationStamp = stamp;
            BindNavigation(selected != null ? selected.GetComponent<Button>() : preferredAction);
            selected = EventSystem.current.currentSelectedGameObject;
        }
        for (int i = 0; i < navigation.Count; i++)
        {
            bool visible = navigation[i].IsInteractable() && (navigation[i].gameObject == selected ||
                RectTransformUtility.RectangleContainsScreenPoint(navigation[i].transform as RectTransform, Input.mousePosition));
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
        label.lineSpacing = 1.12f;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 24;
        label.resizeTextMaxSize = 30;
        if (ChallengeIsActive && !GameComplete && !warning) label.color = WorkshopUi.Faded;
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
        if (primary && preferredAction == null) preferredAction = button;
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

    private enum WorkGesture { Tap, Move, Drag, Hold }

    private static readonly Color GuideGold = new Color32(255, 214, 105, 255);
    private static readonly Color GuideShadow = new Color32(34, 32, 24, 240);

    private RectTransform VisualStep(RectTransform target, Color? color = null)
    {
        Vector2 size = target.rect.size;
        var border = WorkshopUi.Rect(target, "Current Action Highlight", new Rect(0, 0, size.x, size.y));
        float corner = Mathf.Clamp(Mathf.Min(size.x, size.y) * .27f, 16f, 39f);
        for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            {
                float left = x == 0 ? -4f : size.x - corner + 4f;
                float top = y == 0 ? -4f : size.y - corner + 4f;
                float innerX = x == 0 ? left : size.x - 4f;
                float innerY = y == 0 ? top : size.y - 4f;
                WorkshopUi.Fill(border, "Corner Shadow", new Rect(left - 2, innerY - 2, corner + 4, 12), GuideShadow);
                WorkshopUi.Fill(border, "Corner Shadow", new Rect(innerX - 2, top - 2, 12, corner + 4), GuideShadow);
                WorkshopUi.Fill(border, "Corner Edge", new Rect(left, innerY, corner, 8), color ?? GuideGold);
                WorkshopUi.Fill(border, "Corner Edge", new Rect(innerX, top, 8, corner), color ?? GuideGold);
            }
        var fade = border.gameObject.AddComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        fade.interactable = false;
        StartCoroutine(PulseWorkbenchCue(fade));
        return border;
    }

    private static IEnumerator PulseWorkbenchCue(CanvasGroup fade)
    {
        while (fade != null && fade.gameObject.activeInHierarchy)
        {
            fade.alpha = .77f + .23f * Mathf.Cos(Time.unscaledTime * 4f);
            yield return null;
        }
        // Callers can show the same marker again without recreating it.
        if (fade != null) fade.alpha = 1f;
    }

    private RectTransform ShowGesture(RectTransform parent, Vector2 from, Vector2 to, WorkGesture gesture = WorkGesture.Move)
    {
        var cue = WorkshopUi.Rect(parent, "Action Demonstration " + gesture, new Rect());
        var surface = cue.gameObject.AddComponent<CanvasGroup>();
        surface.blocksRaycasts = false;
        surface.interactable = false;
        bool moving = gesture == WorkGesture.Move || gesture == WorkGesture.Drag;
        if (moving && (to - from).sqrMagnitude > 900f)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 start = from + direction * 28f;
            Vector2 tip = to - direction * 25f;
            float distance = Vector2.Distance(start, tip);
            int dots = Mathf.Clamp(Mathf.RoundToInt(distance / 35f), 1, 12);
            for (int i = 0; i < dots; i++)
            {
                Vector2 point = Vector2.Lerp(start, tip, i / (float)dots);
                DrawCueStroke(cue, point, point + direction * 13f, 6f, GuideGold);
            }
            Vector2 cross = new Vector2(-direction.y, direction.x);
            DrawCueStroke(cue, tip - direction * 26f + cross * 15f, tip, 8f, GuideGold);
            DrawCueStroke(cue, tip - direction * 26f - cross * 15f, tip, 8f, GuideGold);
        }

        var mouse = WorkshopUi.Rect(cue, "Mouse Pointer", new Rect());
        DrawCueStroke(mouse, new Vector2(0, 0), new Vector2(3, 34), 7f, WorkshopUi.Cream);
        DrawCueStroke(mouse, new Vector2(0, 0), new Vector2(26, 18), 7f, WorkshopUi.Cream);
        DrawCueStroke(mouse, new Vector2(26, 18), new Vector2(14, 21), 7f, WorkshopUi.Cream);
        DrawCueStroke(mouse, new Vector2(14, 21), new Vector2(3, 34), 7f, WorkshopUi.Cream);
        var press = WorkshopUi.Rect(mouse, "Mouse Press", new Rect());
        DrawCueStroke(press, new Vector2(2, -8), new Vector2(2, -25), 6f, GuideGold);
        DrawCueStroke(press, new Vector2(-7, -5), new Vector2(-20, -17), 6f, GuideGold);
        DrawCueStroke(press, new Vector2(-9, 5), new Vector2(-26, 5), 6f, GuideGold);
        var holdMarks = new Image[gesture == WorkGesture.Hold ? 10 : 0];
        for (int i = 0; i < holdMarks.Length; i++)
        {
            float angle = -Mathf.PI * .5f + i * 2f * Mathf.PI / holdMarks.Length;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 start = direction * 46f;
            var block = WorkshopUi.Fill(mouse, "Hold Progress Notch", new Rect(start.x - 5, start.y - 5, 11, 11), GuideGold);
            WorkshopUi.ApplyPixelShape(block);
            holdMarks[i] = block;
        }
        StartCoroutine(AnimateWorkbenchGesture(cue, mouse, press, holdMarks, from, to, gesture));
        return cue;
    }

    private static void DrawCueStroke(Transform parent, Vector2 a, Vector2 b, float width, Color color)
    {
        float length = Vector2.Distance(a, b);
        Vector2 delta = b - a;
        float angle = -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        var edge = WorkshopUi.Fill(parent, "Gesture Outline", new Rect(a.x, a.y, length, width + 5f), GuideShadow).rectTransform;
        var line = WorkshopUi.Fill(parent, "Gesture Mark", new Rect(a.x, a.y, length, width), color).rectTransform;
        edge.pivot = line.pivot = new Vector2(0f, .5f);
        edge.localEulerAngles = line.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    private static IEnumerator AnimateWorkbenchGesture(RectTransform cue, RectTransform mouse, RectTransform press,
        Image[] holdMarks, Vector2 from, Vector2 to, WorkGesture gesture)
    {
        float elapsed = 0f;
        while (cue != null && cue.gameObject.activeInHierarchy)
        {
            float t = Mathf.Repeat(elapsed, 2.15f);
            float movement = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .27f) / 1.1f));
            if (gesture == WorkGesture.Move && t > 1.37f)
                movement = 1f - Mathf.SmoothStep(0f, 1f, (t - 1.37f) / .78f);
            bool moving = gesture == WorkGesture.Move || gesture == WorkGesture.Drag;
            Vector2 at = moving ? Vector2.Lerp(from, to, movement) : from;
            bool pressed = gesture == WorkGesture.Tap ? t < .32f :
                gesture == WorkGesture.Drag || gesture == WorkGesture.Hold ? t < 1.36f : false;
            if (!pressed && !moving) at += new Vector2(7, 8);
            mouse.anchoredPosition = new Vector2(at.x, -at.y);
            mouse.localScale = Vector3.one * (pressed ? .91f : 1f);
            press.gameObject.SetActive(pressed);
            for (int i = 0; i < holdMarks.Length; i++)
                holdMarks[i].color = pressed && t >= i * .13f ? GuideGold : GuideShadow;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static Vector2 WorkbenchPoint(RectTransform parent, RectTransform target)
    {
        Vector3 local = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
        return new Vector2(local.x - parent.rect.xMin, parent.rect.yMax - local.y);
    }

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
