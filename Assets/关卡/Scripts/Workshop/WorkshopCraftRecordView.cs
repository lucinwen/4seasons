using System;
using System.Collections.Generic;
using JiangnanDye;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class WorkshopCraftRecordView : MonoBehaviour
{
    internal const int CanvasOrder = 18100;
    private static readonly Rect ChapterArtwork = new Rect(330, 120, 1260, 709);
    private static readonly Rect[] PrintedButtons =
    {
        new Rect(610, 705, 424, 82), new Rect(635, 785, 400, 78),
        new Rect(650, 695, 369, 78), new Rect(675, 691, 312, 83)
    };
    private static readonly string[][] ChapterSteps =
    {
        new[] { "桑叶", "养蚕", "结茧" }, new[] { "择茧", "缫丝", "丝束" },
        new[] { "引纬", "缬", WorkshopProgress.ProductName(WorkshopSeason.Autumn) }, new[] { "染布", "裁片", "成衣" }
    };
    private static readonly string[] ChapterNotes =
    {
        "春蚕在蚕匾里吃适合蚕龄的桑叶，长成后才会吐丝结茧。",
        "挑茧以后，先煮茧、找丝头，再把丝绪均匀绕到丝轴上。",
        $"白露奇偶推梭获得生坯绸，染色后完成寒露经纬攀花获得缬。交付秋季成果后获得{WorkshopProgress.ProductName(WorkshopSeason.Autumn)}。",
        "布料染好后按样板裁片，再按裁片的形状组装缝制。"
    };

    private static WorkshopCraftRecordView current;
    private static int closedFrame = -1;
    private RectTransform frame;
    private CanvasGroup controls;
    private GameObject previousSelection;
    private Action onClose;
    private int resumedFrame = -1;
    private RectTransform processFrame;
    private GameObject processSelection;

    internal static bool IsOpen => current != null;
    internal static bool BlocksWorldInput => IsOpen || closedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
        closedFrame = -1;
    }

    internal static void ShowChapterRecord(WorkshopSeason season, Action onClose = null)
    {
        if (!WorkshopProgress.IsComplete(season) || IsOpen)
        {
            onClose?.Invoke();
            return;
        }

        Open("Workshop Chapter Record", onClose).BuildChapter(season);
    }

    internal static void ShowProductionRecord(Action onClose = null)
    {
        if (!WorkshopProgress.IsComplete(WorkshopSeason.Winter) || IsOpen)
        {
            onClose?.Invoke();
            return;
        }

        Open("Workshop Production Record", onClose).BuildProduction();
    }

    private static WorkshopCraftRecordView Open(string name, Action onClose)
    {
        var frame = WorkshopUi.NewCanvas(name, CanvasOrder);
        WorkshopUi.Block(frame, WorkshopUi.Veil);
        var view = frame.gameObject.AddComponent<WorkshopCraftRecordView>();
        view.frame = frame;
        view.controls = frame.gameObject.AddComponent<CanvasGroup>();
        view.previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        view.onClose = onClose;
        current = view;
        WorkshopAudioFeedback.Notice();
        return view;
    }

    private void BuildChapter(WorkshopSeason season)
    {
        int index = (int)season;
        var assets = Resources.Load<WorkshopCraftRecordArt>("WorkshopCraftRecordArt");
        var cover = assets != null ? assets.ChapterCover(season) : null;
        UnityEngine.UI.Button close;
        if (cover != null)
        {
            WorkshopUi.Picture(frame, "Original Chapter Completion", ChapterArtwork, cover);
            if (season == WorkshopSeason.Autumn) DrawAutumnResult(cover);
            close = WorkshopUi.PixelButton(frame, "Continue from Result", FromArtwork(PrintedButtons[index]),
                "继续", Close, true, 32);
        }
        else
        {
            var result = WorkshopUi.PixelCard(frame, "Chapter Result", ChapterArtwork, WorkshopUi.Paper, WorkshopUi.Bronze);
            Label(result, "Chapter", new Rect(50, 60, 908, 85), WorkshopProgress.SeasonName(season) + "章完成", 54);
            var product = season == WorkshopSeason.Winter ? WorkshopArt.Garment(WorkshopProgress.GarmentId) :
                WorkshopArt.ChapterProduct(season);
            WorkshopUi.Picture(result, "Result", new Rect(284, 157, 440, 240), product);
            Label(result, "Product", new Rect(50, 398, 908, 56), WorkshopProgress.ProductName(season), 35);
            close = WorkshopUi.PixelButton(result, "Continue from Result", new Rect(376, 476, 256, 66),
                "继续", Close, true, 32);
        }

        var observations = new List<WorkshopObservation>();
        foreach (var observation in WorkshopObservationView.ObservationsFor(season))
            if (WorkshopObservationView.HasRecord(observation)) observations.Add(observation);

        int actions = observations.Count + 2;
        float width = (ChapterArtwork.width - (actions - 1) * 16) / actions;
        int action = 0;
        SecondaryButton("Toggle Process", action++, width, "展开工序",
            () => OpenChapterProcess(season, assets != null ? assets.GreigeSilk : null));
        foreach (var observation in observations)
        {
            var id = observation;
            SecondaryButton("Observation " + id, action++, width, WorkshopObservationView.RecordTitle(id),
                () => OpenChild(done => WorkshopObservationView.ShowRecord(id, done)));
        }
        SecondaryButton("Regional Sources", action, width, "地方工艺与来源",
            () => OpenChild(done => WorkshopHeritageBrief.ShowSeason(season, done)));
        KeepFocusIn(frame, close);
    }

    private void DrawAutumnResult(Sprite cover)
    {
        // The autumn cover has its original product name and illustration printed on the paper.
        var nameBounds = new Rect(757, 446, 142, 65);
        DrawParchment(cover, "Product Name Parchment", nameBounds, new Rect(757, 514, 142, 18));
        var name = WorkshopUi.Label(frame, "Product", FromArtwork(nameBounds),
            WorkshopProgress.ProductName(WorkshopSeason.Autumn), 40, new Color32(53, 29, 13, 255), TextAnchor.MiddleCenter);
        name.fontStyle = FontStyle.Bold;

        var paper = new Rect(544, 559, 420, 109);
        DrawParchment(cover, "Product Parchment", new Rect(987, 371, 431, 256), paper);
        DrawParchment(cover, "Product Parchment Top", new Rect(1126, 331, 292, 40), paper);
        WorkshopUi.Picture(frame, "Result", FromArtwork(new Rect(938, 309, 514, 343)),
            WorkshopArt.ChapterProduct(WorkshopSeason.Autumn));
    }

    private void DrawParchment(Sprite cover, string name, Rect bounds, Rect paper)
    {
        var image = WorkshopUi.Rect(frame, name, FromArtwork(bounds)).gameObject.AddComponent<RawImage>();
        image.texture = cover.texture;
        image.uvRect = new Rect(paper.x / 1672f, 1f - paper.yMax / 941f, paper.width / 1672f, paper.height / 941f);
        image.raycastTarget = false;
    }

    private void SecondaryButton(string name, int column, float width, string text, Action clicked)
    {
        var button = WorkshopUi.PixelButton(frame, name,
            new Rect(ChapterArtwork.x + column * (width + 16), 850, width, 56), text, clicked, false, 26);
        var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 20;
        label.resizeTextMaxSize = 26;
    }

    private void OpenChapterProcess(WorkshopSeason season, Sprite greigeSilk)
    {
        if (processFrame != null || !controls.interactable) return;
        processSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        controls.alpha = 0;
        controls.interactable = false;
        controls.blocksRaycasts = false;
        processFrame = WorkshopUi.NewCanvas("Workshop Chapter Process", CanvasOrder + 50);
        var board = WorkshopUi.PixelCard(processFrame, "Chapter Process Detail", new Rect(550, 290, 820, 500),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        Label(board, "Process Title", new Rect(70, 34, 680, 62), WorkshopProgress.SeasonName(season) + "季工序", 40);
        var process = WorkshopUi.Rect(board, "Process", new Rect(94, 127, 632, 233));
        DrawChapterSteps(process, season, greigeSilk);
        Label(process, "Craft Note", new Rect(10, 145, 612, 83), ChapterNotes[(int)season], 25, TextAnchor.UpperLeft);
        var back = WorkshopUi.PixelButton(board, "Close Chapter Process", new Rect(285, 400, 250, 62),
            "返回", CloseChapterProcess, false, 28);
        KeepFocusIn(processFrame, back);
    }

    private void CloseChapterProcess()
    {
        if (processFrame == null) return;
        WorkshopUi.CloseCanvas(processFrame);
        processFrame = null;
        controls.alpha = 1;
        controls.interactable = true;
        controls.blocksRaycasts = true;
        resumedFrame = Time.frameCount;
        Select(processSelection);
    }

    private static void DrawChapterSteps(Transform parent, WorkshopSeason season, Sprite greigeSilk)
    {
        const float width = 150f;
        const float gap = 70f;
        var observation = WorkshopObservationArt.Current;
        Sprite[] steps;
        Color fabricTint = Color.white;
        Color clothTint = Color.white;
        switch (season)
        {
            case WorkshopSeason.Spring:
                steps = new[] { observation?.Leaf, observation?.SilkwormTray, WorkshopArt.ChapterProduct(season) };
                break;
            case WorkshopSeason.Summer:
                steps = new[] { WorkshopArt.ChapterProduct(WorkshopSeason.Spring), observation?.SilkReel,
                    WorkshopArt.ChapterProduct(season) };
                break;
            case WorkshopSeason.Autumn:
                steps = new[] { observation?.Shuttle, greigeSilk, WorkshopArt.ChapterProduct(season) };
                break;
            default:
                steps = new[] { DyedCloth(out fabricTint, out clothTint), null, WorkshopArt.Garment(WorkshopProgress.GarmentId) };
                break;
        }

        for (int i = 0; i < steps.Length; i++)
        {
            float x = 8 + i * (width + gap);
            if (season == WorkshopSeason.Winter && i == 1)
                DrawCutPieces(parent, x + 5, fabricTint);
            else
                WorkshopUi.Picture(parent, "Step " + i, new Rect(x, 0, width, 96), steps[i],
                    i == 0 ? clothTint : Color.white);
            Label(parent, "Step Caption " + i, new Rect(x, 102, width, 41), ChapterSteps[(int)season][i], 28);
            if (i < steps.Length - 1) DrawArrow(parent, x + width + 17, 47);
        }
    }

    private static void DrawCutPieces(Transform parent, float x, Color tint)
    {
        WorkshopUi.Picture(parent, "Front Piece", new Rect(x, 4, 54, 73), WorkshopArt.CutPiece(0), tint);
        WorkshopUi.Picture(parent, "Back Piece", new Rect(x + 43, 4, 54, 73), WorkshopArt.CutPiece(1), tint);
        WorkshopUi.Picture(parent, "Sleeve Pieces", new Rect(x + 93, 31, 47, 52), WorkshopArt.CutPiece(2), tint);
    }

    private static Sprite DyedCloth(out Color fabricTint, out Color clothTint)
    {
        var config = Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
        string savedColor = WorkshopProgress.DyedColor;
        foreach (DyeColor color in DyeColorUtil.AllColors)
        {
            string name = DyeColorUtil.ChineseName(color);
            if (savedColor != name && savedColor != name + "布") continue;
            fabricTint = config != null ? config.GetClothTint(color) : DyeColorUtil.Tint(color);
            var dedicated = config?.GetCloth(color);
            clothTint = dedicated != null ? Color.white : fabricTint;
            return dedicated ?? config?.silk ?? WorkshopArt.PlainSilk;
        }

        fabricTint = clothTint = Color.white;
        return WorkshopArt.PlainSilk;
    }

    private void BuildProduction()
    {
        var bounds = new Rect(124, 69, 1672, 941);
        var board = Resources.Load<Sprite>("WorkshopDelivery/B07");
        if (board != null) WorkshopUi.Picture(frame, "Production Board", bounds, board);
        else WorkshopUi.PixelCard(frame, "Production Board", bounds, WorkshopUi.Paper, WorkshopUi.Bronze);
        var content = WorkshopUi.Rect(frame, "Production Journey", bounds);
        Label(content, "Title", new Rect(595, 131, 482, 68), "制作记录", 47);
        string[] processes = { "春 · 采桑浴蚕与喂蚕", "夏 · 下簇缫丝与备料", "秋 · 织造染色与攀花", "冬 · 排布剪帛与缝衣" };
        for (int i = 0; i < processes.Length; i++)
        {
            var season = (WorkshopSeason)i;
            bool recorded = WorkshopProgress.IsComplete(season);
            Label(content, "Chapter Process " + season,
                new Rect(401 + i % 2 * 436, 211 + i / 2 * 54, 425, 47),
                processes[i] + (recorded ? " · 已完成" : " · 待完成"),
                24);
        }
        Label(content, "Journey", new Rect(429, 329, 811, 55), "从一片桑叶到一件成衣", 30);

        string garmentId = WorkshopProgress.GarmentId;
        var garment = WorkshopArt.Garment(garmentId);
        Sprite[] stages = { WorkshopObservationArt.Current?.Leaf, WorkshopArt.ChapterProduct(WorkshopSeason.Spring),
            WorkshopArt.ChapterProduct(WorkshopSeason.Summer), WorkshopArt.ChapterProduct(WorkshopSeason.Autumn), garment };
        string[] names = { "桑叶", "蚕茧", "丝束", WorkshopProgress.ProductName(WorkshopSeason.Autumn),
            garment != null ? WorkshopTailor.GarmentName(garmentId) : "成衣图未记录" };
        for (int i = 0; i < stages.Length; i++)
        {
            float x = 398 + i * 183;
            WorkshopUi.Picture(content, "Production Step " + i, new Rect(x, 393, 144, 143), stages[i]);
            Label(content, "Production Caption " + i, new Rect(x - 9, 545, 162, 54), names[i], 26);
            if (i < stages.Length - 1) DrawArrow(content, x + 145, 461);
        }

        WorkshopUi.PixelButton(content, "Heritage Context", new Rect(509, 646, 282, 62), "回看南浔缫丝",
            () => OpenChild(done => WorkshopHeritageBrief.ShowSeason(WorkshopSeason.Summer, done)), true, 28);
        var close = WorkshopUi.PixelButton(content, "Return from Production", new Rect(881, 646, 282, 62),
            "返回", Close, false, 28);
        KeepFocusIn(frame, close);
    }

    private void OpenChild(Action<Action> open)
    {
        if (!controls.interactable || WorkshopObservationView.BlocksWorldInput || WorkshopHeritageBrief.IsOpen) return;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        controls.interactable = false;
        controls.blocksRaycasts = false;
        open(() =>
        {
            if (current != this) return;
            resumedFrame = Time.frameCount;
            controls.interactable = true;
            controls.blocksRaycasts = true;
            Select(selected);
        });
    }

    private void Update()
    {
        if (current == this && processFrame != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseChapterProcess();
            return;
        }
        if (current != this || !controls.interactable || resumedFrame == Time.frameCount ||
            WorkshopDialoguePresenter.BlocksWorldInput || WorkshopObservationView.BlocksWorldInput || WorkshopHeritageBrief.IsOpen)
            return;
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Close()
    {
        if (current != this) return;
        current = null;
        closedFrame = Time.frameCount;
        WorkshopUi.CloseCanvas(frame);
        Select(previousSelection);
        onClose?.Invoke();
    }

    private void OnDestroy()
    {
        if (processFrame != null) WorkshopUi.CloseCanvas(processFrame);
        if (current == this) current = null;
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target != null && target.activeInHierarchy ? target : null);
    }

    internal static void KeepFocusIn(Transform content, UnityEngine.UI.Selectable initial)
    {
        var options = new List<UnityEngine.UI.Selectable>();
        foreach (var selectable in content.GetComponentsInChildren<UnityEngine.UI.Selectable>())
            if (selectable.IsInteractable()) options.Add(selectable);

        for (int i = 0; i < options.Count; i++)
        {
            var before = options[(i + options.Count - 1) % options.Count];
            var after = options[(i + 1) % options.Count];
            options[i].navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = before,
                selectOnLeft = before,
                selectOnDown = after,
                selectOnRight = after
            };
        }

        if (initial != null) Select(initial.gameObject);
    }

    private static void DrawArrow(Transform parent, float x, float y)
    {
        WorkshopUi.Fill(parent, "Arrow Stem", new Rect(x, y - 2, 19, 4), WorkshopUi.Bronze);
        WorkshopUi.Fill(parent, "Arrow Head", new Rect(x + 17, y - 8, 5, 16), WorkshopUi.Bronze);
        WorkshopUi.Fill(parent, "Arrow Tip", new Rect(x + 22, y - 4, 5, 8), WorkshopUi.Bronze);
    }

    private static void Label(Transform parent, string name, Rect bounds, string value, int size,
        TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        WorkshopUi.Label(parent, name, bounds, value, size, WorkshopUi.Ink, alignment);
    }

    private static Rect FromArtwork(Rect source) => new Rect(
        ChapterArtwork.x + source.x * (ChapterArtwork.width / 1672f),
        ChapterArtwork.y + source.y * (ChapterArtwork.height / 941f),
        source.width * (ChapterArtwork.width / 1672f), source.height * (ChapterArtwork.height / 941f));

}
