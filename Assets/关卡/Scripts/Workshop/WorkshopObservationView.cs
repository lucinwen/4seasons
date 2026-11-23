using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal enum WorkshopObservation
{
    QingmingMulberry,
    GuyuLeaf,
    MangzhongCocoon,
    DashuVentilation,
    DashuReeling,
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

        internal Definition(WorkshopSeason season, string id, string term, string title, string station,
            string challengeName = null)
        {
            Season = season;
            Id = "observation." + id;
            Term = term;
            Title = title;
            Station = station;
            ChallengeName = challengeName;
        }
    }

    private static readonly Definition[] Definitions =
    {
        new Definition(WorkshopSeason.Spring, "spring.qingming", "清明", "桑园里的新叶", "看桑芽", "拨枝采桑"),
        new Definition(WorkshopSeason.Spring, "spring.guyu", "谷雨", "雨后的桑叶", "观察桑叶", "擦露配桑"),
        new Definition(WorkshopSeason.Summer, "summer.mangzhong", "芒种", "蔟架上的蚕茧", "看茧况", "翻茧配批"),
        new Definition(WorkshopSeason.Summer, "summer.dashu.vent", "大暑", "蔟房的通风窗", "暑热观察"),
        new Definition(WorkshopSeason.Summer, "summer.dashu.reeling", "大暑", "缫丝三步", "缫丝三步", "稳丝续绪"),
        new Definition(WorkshopSeason.Autumn, "autumn.qiufen", "秋分", "织坊的致谢", "读织坊致谢", "供台见光"),
        new Definition(WorkshopSeason.Autumn, "autumn.hanlu", "寒露", "看经线", "看经线", "双色走梭"),
        new Definition(WorkshopSeason.Winter, "winter.lidong", "立冬", "染坊里的色样", "看色样", "两单一缸"),
        new Definition(WorkshopSeason.Winter, "winter.xiaoxue", "小雪", "从试染到成衣", "看成衣工序", "排料留布")
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
    private GameObject priorSelection;
    private float priorTimeScale;
    private bool readOnly;
    private bool showSongJin;
    private bool showBatches;
    private bool earnedRewardThisRun;
    private int stage;
    private int rows;

    internal static bool IsOpen => current != null;
    internal static bool BlocksWorldInput => IsOpen || lastClosedFrame == Time.frameCount;
    internal static WorkshopObservation? ActiveGame => current != null && !current.readOnly &&
        IsSeasonGame(current.observation) ? current.observation : (WorkshopObservation?)null;

    internal static WorkshopSeason Season(WorkshopObservation id) => Definitions[(int)id].Season;
    internal static string RecordTitle(WorkshopObservation id) => Definitions[(int)id].Title;
    internal static string StationLabel(WorkshopObservation id) => Definitions[(int)id].Station;
    internal static string SeasonGameTitle(WorkshopObservation id) => Definitions[(int)id].ChallengeName;
    internal static bool IsSeasonGame(WorkshopObservation id) => !string.IsNullOrEmpty(SeasonGameTitle(id));
    internal static bool HasRecord(WorkshopObservation id) => WorkshopProgress.HasObservation(Definitions[(int)id].Id);

    internal static IEnumerable<WorkshopObservation> ObservationsFor(WorkshopSeason season)
    {
        for (int i = 0; i < Definitions.Length; i++)
            if (Definitions[i].Season == season)
                yield return (WorkshopObservation)i;
    }

    internal static void Show(WorkshopObservation id, Action onClosed = null) => Open(id, onClosed, false);

    internal static void ShowRecord(WorkshopObservation id, Action onClosed = null)
    {
        if (HasRecord(id))
            Open(id, onClosed, true);
    }

    private static void Open(WorkshopObservation id, Action onClosed, bool readOnly)
    {
        if (IsOpen) return;

        var selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var root = WorkshopUi.NewCanvas("Workshop Observation", 23000);
        WorkshopUi.Block(root, new Color32(21, 34, 29, 219));
        current = root.gameObject.AddComponent<WorkshopObservationView>();
        current.frame = root;
        current.onClosed = onClosed;
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        TickGuyuWiping();

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
        var definition = Definitions[(int)observation];
        Color accent = Accents[(int)definition.Season];
        page = DrawWorkshopPaper(frame, "Observation Panel", new Rect(88, 77, 1744, 926),
            WorkshopUi.Paper, WorkshopUi.Bronze);

        WorkshopUi.Label(page, "Season and Solar Term", new Rect(28, 19, 560, 40),
            WorkshopProgress.SeasonName(definition.Season) + " · " + definition.Term, 28, accent);
        WorkshopUi.Label(page, "Observation Title", new Rect(28, 61, 1320, 78),
            ChallengeIsActive ? definition.ChallengeName : definition.Title, 54, WorkshopUi.Ink);
        if (readOnly)
            WorkshopUi.Label(page, "Record Mode", new Rect(1294, 71, 390, 58),
                IsSeasonGame(observation) ? "已通关 · 残页已收集" : "织录 · 已观察", 31, accent, TextAnchor.MiddleRight);
        else if (ChallengeIsActive)
            DrawChallengeStatus(accent);
        WorkshopUi.Fill(page, "Heading Rule", new Rect(28, 144, 1688, 2), WorkshopUi.Line);

        var frameBevel = WorkshopUi.PixelCard(page, "Illustration Frame", new Rect(28, 163, 1172, 665),
            accent, WorkshopUi.Bronze);
        art = WorkshopUi.Rect(frameBevel, "Observation Illustration", new Rect(6, 6, ArtWidth, ArtHeight));
        WorkshopUi.Fill(art, "Illustration Surface", ArtBounds, InsetPaper);
        infoPanel = DrawWorkshopPaper(page, "Observation Instructions", new Rect(1218, 163, 498, 665),
            LightSurface, WorkshopUi.Line);

        DrawActivity();
        if (IsSeasonGame(observation)) DrawChallengeReward();

        var exit = WorkshopUi.PixelButton(page, "Return From Observation", new Rect(1473, 848, 243, 58),
            ChallengeIsActive ? "退出小游戏 · Esc" : onClosed == null && !readOnly ? "返回关卡 · Esc" : "返回 · Esc",
            Close, false, 27);
        Button replay = ChallengeIsActive ? DrawChallengeControls() : null;
        RegisterButton(exit);
        if (ChallengeIsActive)
        {
            // Instruction text and small labels must stay readable at the 720p workbench size.
            var readingFont = WorkshopArt.ReadingFont;
            foreach (var label in page.GetComponentsInChildren<Text>(true))
            {
                label.font = readingFont;
                if (label.name == "Observation Title") label.fontStyle = FontStyle.Bold;
            }
        }
        var prior = previousFocus != null ? page.Find(previousFocus) : null;
        BindNavigation(replay ?? prior?.GetComponent<Button>());
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
            case WorkshopObservation.DashuReeling:
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
