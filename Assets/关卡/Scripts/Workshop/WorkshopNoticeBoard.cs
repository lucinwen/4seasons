using UnityEngine;

internal sealed class WorkshopNoticeBoard : MonoBehaviour
{
    private const float PaperWidth = 1092f;
    private const float PaperHeight = 128f;
    private const float RowSpacing = 116f;
    private const float ViewWidth = 1130f;
    private const float ViewHeight = 496f;
    private const float ProgressWidth = 79f;
    private const string ArtPath = "WorkshopNoticeBoard/";
    private const string NoticeNumbers = "一二三四五六七八";
    private static readonly float[] LeftOffsets = { 9f, 31f, 15f, 23f };
    private static readonly float[] Angles = { 0.55f, -0.67f, 0.37f, -0.46f };
    private static readonly Color Ink = new Color32(43, 34, 26, 255);
    private static readonly Color Leaf = new Color32(69, 102, 78, 255);
    private static readonly Color TabPaper = new Color32(242, 221, 182, 255);
    private static readonly Color[] SeasonDyes =
    {
        new Color32(202, 218, 174, 255),
        new Color32(180, 213, 201, 255),
        new Color32(234, 202, 168, 255),
        new Color32(194, 215, 224, 255)
    };
    private static readonly Color[] SeasonInks =
    {
        new Color32(70, 101, 73, 255),
        new Color32(57, 101, 97, 255),
        new Color32(128, 75, 55, 255),
        new Color32(66, 93, 121, 255)
    };

    internal static readonly Color ModalShade = new Color(0f, 0f, 0f, 0.88f);
    internal static readonly Rect PosterIllustration =
        new Rect(505f / 660f, 8f / 85f, 136f / 660f, 69f / 85f);

    private sealed class Notice
    {
        internal WorkshopQuests.Entry Quest;
        internal RectTransform Paper;
        internal RectTransform Face;
        internal CanvasGroup Group;
        internal UnityEngine.UI.Image Flag;
        internal UnityEngine.UI.Text FlagLabel;
        internal Vector2 Target;
        internal Vector2 PeelOrigin;
        internal float Angle;
        internal float PeelTime = -1f;
    }

    private WorkshopSeason season;
    private Notice[] notices;
    private UnityEngine.UI.Button[] tabs;
    private UnityEngine.UI.Text[] tabLabels;
    private RectTransform content;
    private RectTransform peelLayer;
    private UnityEngine.UI.ScrollRect scroll;
    private UnityEngine.UI.Scrollbar scrollbar;
    private UnityEngine.UI.Text chapter;
    private UnityEngine.UI.Text progress;
    private UnityEngine.UI.Image progressFill;
    private UnityEngine.UI.Text scrollHint;
    private UnityEngine.UI.Text emptyMessage;
    private float pendingHeight;
    private float reflowPauseUntil;
    private int pendingCount;

    internal UnityEngine.UI.Button CloseAction { get; private set; }

    internal static Font NoticeFont => WorkshopArt.PixelFont;

    internal static WorkshopNoticeBoard Build(Transform parent, WorkshopSeason season, System.Action closed)
    {
        var root = WorkshopUi.Rect(parent, "Workshop Notice Board", new Rect(0, 0, 1920, 1080));
        var board = root.gameObject.AddComponent<WorkshopNoticeBoard>();
        board.BuildFrame(closed);
        board.SelectSeason(season);
        return board;
    }

    internal static void DrawBoard(Transform parent, Rect bounds)
    {
        if (DrawTexture(parent, "Timber Notice Board", bounds, Resources.Load<Texture2D>(ArtPath + "Board")) != null)
            return;

        WorkshopUi.PixelCard(parent, "Timber Frame", bounds,
            new Color32(198, 156, 108, 255), new Color32(108, 83, 55, 255));
    }

    internal static void DrawTitle(Transform parent, Rect bounds)
    {
        Label(parent, "Notice Board Title", bounds, "工坊公告栏", Mathf.RoundToInt(bounds.height * 0.78f),
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
    }

    internal static RectTransform DrawSmallNotice(Transform parent, string name, Rect bounds)
    {
        var paper = DrawTexture(parent, name, bounds, Resources.Load<Texture2D>(ArtPath + "SidePaper"));
        return paper != null ? paper.rectTransform : WorkshopUi.PixelCard(parent, name, bounds,
            WorkshopUi.Paper, WorkshopUi.Bronze);
    }

    internal static Texture2D LoadPoster(WorkshopQuests.Entry quest) =>
        Resources.Load<Texture2D>(ArtPath + "Notices/" + quest.Id);

    internal static UnityEngine.UI.RawImage DrawTexture(Transform parent, string name, Rect bounds,
        Texture2D texture)
    {
        if (texture == null) return null;
        var picture = WorkshopUi.Rect(parent, name, bounds).gameObject.AddComponent<UnityEngine.UI.RawImage>();
        picture.texture = texture;
        picture.raycastTarget = false;
        return picture;
    }

    private void BuildFrame(System.Action closed)
    {
        DrawBoard(transform, new Rect(180, 28, 1560, 1024));
        DrawTitle(transform, new Rect(805, 209, 310, 69));

        tabs = new UnityEngine.UI.Button[WorkshopQuests.SeasonCount];
        tabLabels = new UnityEngine.UI.Text[tabs.Length];
        for (int i = 0; i < tabs.Length; i++)
        {
            var tabSeason = (WorkshopSeason)i;
            tabs[i] = WorkshopUi.Button(transform, "Season " + tabSeason,
                new Rect(409 + i * 98, 309, 88, 43), "", () => SelectSeason(tabSeason), TabPaper);
            tabLabels[i] = Label(tabs[i].transform, "Season Name", new Rect(0, 0, 88, 43),
                WorkshopProgress.SeasonName(tabSeason) + "季", 26, Ink, TextAnchor.MiddleCenter);
        }

        CloseAction = WorkshopUi.Button(transform, "Close Notice Board", new Rect(1404, 219, 98, 45),
            "收起", closed, Leaf, size: 24, font: NoticeFont);

        chapter = Label(transform, "Chapter Status", new Rect(817, 307, 366, 45), "", 25, Ink);
        progress = Label(transform, "Quest Progress", new Rect(1175, 307, 228, 45), "", 24, Ink,
            TextAnchor.MiddleRight);
        var gauge = WorkshopUi.Fill(transform, "Progress Track", new Rect(1420, 327, ProgressWidth, 6),
            new Color32(118, 83, 53, 160));
        progressFill = WorkshopUi.Fill(gauge.transform, "Progress Fill", new Rect(0, 0, 0, 6), Leaf);
        WorkshopUi.Fill(transform, "Header Seam", new Rect(409, 358, 1090, 2), new Color32(115, 84, 54, 100));
        BuildScroll();

        WorkshopUi.Button(transform, "Notice Location Guide", new Rect(409, 868, 176, 40),
            "地点导览", () => WorkshopChapterGuide.Show(season), Leaf, size: 24, font: NoticeFont);
        WorkshopUi.Button(transform, "Notice Heritage", new Rect(599, 868, 195, 40),
            "新闻与来源", () => WorkshopHeritageBrief.Show(), Leaf, size: 24, font: NoticeFont);
        emptyMessage = Label(transform, "Empty Notice Board", new Rect(445, 544, 1030, 104),
            "本季告示已全部取下", 40, Ink, TextAnchor.MiddleCenter);
        scrollHint = Label(transform, "Scroll Hint", new Rect(1095, 868, 369, 26),
            "滚动或拖动查看", 20, Ink, TextAnchor.MiddleRight);
    }

    private void BuildScroll()
    {
        var area = WorkshopUi.Rect(transform, "Notice Scroll Area", new Rect(389, 367, 1145, ViewHeight));
        var hitArea = WorkshopUi.Fill(area, "Viewport", new Rect(0, 0, ViewWidth, ViewHeight), Color.clear);
        hitArea.raycastTarget = true;
        var viewport = hitArea.rectTransform;
        viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        content = WorkshopUi.Rect(viewport, "Pinned Notices", new Rect(0, 0, ViewWidth, ViewHeight));

        scroll = area.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 66f;

        var rail = WorkshopUi.Fill(area, "Scrollbar", new Rect(ViewWidth + 2, 9, 10, ViewHeight - 18),
            new Color32(117, 89, 61, 152));
        rail.raycastTarget = true;
        var track = WorkshopUi.Rect(rail.transform, "Sliding Area", new Rect());
        track.anchorMin = Vector2.zero;
        track.anchorMax = Vector2.one;
        track.offsetMin = new Vector2(2, 2);
        track.offsetMax = new Vector2(-2, -2);
        var thumb = WorkshopUi.Fill(track, "Scroll Handle", new Rect(), new Color32(67, 99, 77, 255));
        thumb.raycastTarget = true;
        scrollbar = rail.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
        scrollbar.handleRect = thumb.rectTransform;
        scrollbar.targetGraphic = thumb;
        scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        scrollbar.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        scroll.verticalScrollbar = scrollbar;
        peelLayer = WorkshopUi.Rect(area, "Lifted Notices", new Rect(0, 0, ViewWidth, ViewHeight));
    }

    private void SelectSeason(WorkshopSeason selected)
    {
        if (notices != null)
        {
            if (season == selected) return;
            foreach (var notice in notices)
            {
                notice.Paper.gameObject.SetActive(false);
                Destroy(notice.Paper.gameObject);
            }
        }

        season = selected;
        var quests = WorkshopQuests.ForSeason(season);
        notices = new Notice[quests.Count];
        for (int i = 0; i < quests.Count; i++)
            notices[i] = BuildNotice(quests[i], i);

        scroll.StopMovement();
        reflowPauseUntil = 0f;
        content.anchoredPosition = Vector2.zero;
        Refresh(true);
        scroll.verticalNormalizedPosition = 1f;
    }

    private Notice BuildNotice(WorkshopQuests.Entry quest, int index)
    {
        var paper = WorkshopUi.Rect(content, "Notice " + quest.Id, new Rect(0, 0, PaperWidth, PaperHeight));
        var face = WorkshopUi.Rect(paper, "Paper Face", new Rect(0, 0, PaperWidth, PaperHeight));
        face.pivot = new Vector2(0.5f, 0.5f);
        face.anchoredPosition = new Vector2(PaperWidth / 2, -PaperHeight / 2);
        float angle = Angles[index % Angles.Length];
        face.localRotation = Quaternion.Euler(0, 0, angle);

        var bounds = new Rect(0, 0, PaperWidth, PaperHeight);
        if (DrawTexture(face, "Illustrated Notice", bounds, LoadPoster(quest)) == null)
        {
            WorkshopUi.PixelCard(face, "Pinned Paper", bounds, WorkshopUi.PixelPaper,
                new Color32(137, 84, 46, 255));
        }
        string order = index < NoticeNumbers.Length ? NoticeNumbers[index].ToString() : (index + 1).ToString();
        Label(face, "Order", new Rect(46, 47, 59, 42), order, 33,
            new Color32(163, 84, 67, 255), TextAnchor.MiddleCenter);
        Label(face, "Task Title", new Rect(146, 15, 548, 45), quest.Title, 37, SeasonInks[(int)season]);
        var instructions = Label(face, "Instructions", new Rect(147, 60, 666, 44), quest.Instructions, 27, Ink);
        instructions.resizeTextForBestFit = true;
        instructions.resizeTextMinSize = 22;
        instructions.resizeTextMaxSize = 27;

        var flag = WorkshopUi.Fill(face, "Task Status", new Rect(709, 24, 108, 30), Leaf);
        WorkshopUi.ApplyPixelShape(flag);
        var flagLabel = Label(flag.transform, "Status Label", new Rect(0, 0, 108, 30), "", 21,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        var group = paper.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        return new Notice
        {
            Quest = quest, Paper = paper, Face = face, Group = group, Flag = flag, FlagLabel = flagLabel, Angle = angle
        };
    }

    private void OnEnable()
    {
        WorkshopProgress.Changed += OnProgressChanged;
        if (notices != null) Refresh(false);
    }

    private void OnDisable() => WorkshopProgress.Changed -= OnProgressChanged;

    private void OnProgressChanged() => Refresh(false);

    private void Refresh(bool immediate)
    {
        bool unlocked = WorkshopProgress.IsUnlocked(season);
        bool complete = WorkshopProgress.IsComplete(season);
        chapter.text = unlocked ? "" : "完成上一季后开放";
        for (int i = 0; i < tabs.Length; i++)
        {
            var image = (UnityEngine.UI.Image)tabs[i].targetGraphic;
            bool selected = i == (int)season;
            image.color = selected ? SeasonDyes[i] : TabPaper;
            tabLabels[i].color = selected ? SeasonInks[i] : Ink;
        }

        int row = 0;
        bool hasCurrent = false;
        bool peeling = false;
        foreach (var notice in notices)
        {
            if (complete || notice.Quest.IsComplete)
            {
                if (immediate) notice.Paper.gameObject.SetActive(false);
                else if (notice.Paper.gameObject.activeSelf && notice.PeelTime < 0f) BeginPeel(notice);

                peeling |= notice.Paper.gameObject.activeSelf;
                continue;
            }

            bool ready = unlocked && notice.Quest.IsReady;
            bool current = ready && !hasCurrent;
            hasCurrent |= current;
            notice.Flag.gameObject.SetActive(current || (unlocked && !ready));
            notice.Flag.color = current ? Leaf : new Color32(130, 91, 57, 255);
            notice.FlagLabel.text = current ? "下一项" : "待前置";

            notice.Target = new Vector2(LeftOffsets[row % LeftOffsets.Length], -8f - row * RowSpacing);
            if (immediate || !notice.Paper.gameObject.activeSelf || notice.PeelTime >= 0f)
            {
                if (notice.Paper.parent != content) notice.Paper.SetParent(content, false);
                notice.Paper.anchoredPosition = notice.Target;
                notice.Face.localRotation = Quaternion.Euler(0, 0, notice.Angle);
                notice.Group.alpha = 1f;
                notice.Paper.gameObject.SetActive(true);
            }

            notice.PeelTime = -1f;
            row++;
        }

        // The upper sheets cover the bottom edges of the papers below them.
        for (int i = notices.Length - 1; i >= 0; i--)
            if (notices[i].Paper.parent == content) notices[i].Paper.SetAsLastSibling();

        pendingCount = row;
        pendingHeight = Mathf.Max(ViewHeight, 14f + PaperHeight + (row - 1) * RowSpacing);
        int completed = notices.Length - row;
        progress.text = $"已完成  {completed} / {notices.Length}";
        progressFill.color = SeasonInks[(int)season];
        progressFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            ProgressWidth * completed / notices.Length);
        if (!peeling || pendingHeight > content.sizeDelta.y) FitScroll();
        emptyMessage.gameObject.SetActive(row == 0 && !peeling);
    }

    private void BeginPeel(Notice notice)
    {
        float top = notice.Paper.anchoredPosition.y + content.anchoredPosition.y;
        if (top > 0f || top - PaperHeight < -ViewHeight)
        {
            notice.Paper.gameObject.SetActive(false);
            return;
        }

        notice.Paper.SetParent(peelLayer, true);
        notice.PeelOrigin = notice.Paper.anchoredPosition;
        notice.PeelTime = 0f;
        notice.Flag.color = Leaf;
        notice.FlagLabel.text = "已完成";
        notice.Flag.gameObject.SetActive(true);
        reflowPauseUntil = Time.unscaledTime + 0.16f;
    }

    private void Update()
    {
        if (notices == null) return;
        bool peeling = false;
        float delta = Time.unscaledDeltaTime;
        float move = 1f - Mathf.Exp(-15f * delta);
        foreach (var notice in notices)
        {
            if (!notice.Paper.gameObject.activeSelf) continue;
            if (notice.PeelTime < 0f)
            {
                if (Time.unscaledTime < reflowPauseUntil) continue;
                notice.Paper.anchoredPosition = Vector2.Lerp(notice.Paper.anchoredPosition, notice.Target, move);
                if ((notice.Paper.anchoredPosition - notice.Target).sqrMagnitude < 0.25f)
                    notice.Paper.anchoredPosition = notice.Target;
                continue;
            }

            notice.PeelTime += delta;
            float t = Mathf.Clamp01(notice.PeelTime / 0.48f);
            float lift = Mathf.SmoothStep(0f, 1f, t * 2f);
            notice.Paper.anchoredPosition = notice.PeelOrigin + new Vector2(242f * t, 26f * lift - 110f * t * t);
            notice.Face.localRotation = Quaternion.Euler(0, 0, notice.Angle - 12f * t);
            notice.Group.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.32f, 1f, t));
            if (t < 1f)
            {
                peeling = true;
                continue;
            }

            notice.Paper.gameObject.SetActive(false);
            notice.Paper.SetParent(content, false);
        }

        if (peeling) return;
        if (!Mathf.Approximately(content.sizeDelta.y, pendingHeight)) FitScroll();
        if (pendingCount == 0 && !emptyMessage.gameObject.activeSelf) emptyMessage.gameObject.SetActive(true);
    }

    private void FitScroll()
    {
        float range = pendingHeight - ViewHeight;
        bool overflow = range > 0f;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pendingHeight);
        var position = content.anchoredPosition;
        content.anchoredPosition = new Vector2(0, Mathf.Clamp(position.y, 0, range));
        scrollbar.gameObject.SetActive(overflow);
        scrollHint.gameObject.SetActive(overflow);
    }

    private static UnityEngine.UI.Text Label(Transform parent, string name, Rect bounds, string text,
        int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft) =>
        WorkshopUi.Label(parent, name, bounds, text, size, color, alignment, NoticeFont);
}
