using System;
using System.Collections.Generic;
using JiangnanDye;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class WorkshopCraftRecordView : MonoBehaviour
{
    internal const int CanvasOrder = 18100;
    private static readonly Rect ChapterArtwork = new Rect(102, 225, 1008, 567.3f);
    private static readonly Rect[] PrintedInstructions =
    {
        new Rect(321, 550, 589, 87), new Rect(310, 510, 522, 80),
        new Rect(466, 571, 551, 83), new Rect(406, 530, 531, 83)
    };
    private static readonly Rect[] PrintedButtons =
    {
        new Rect(610, 705, 424, 82), new Rect(635, 785, 400, 78),
        new Rect(650, 695, 369, 78), new Rect(675, 691, 312, 83)
    };
    private static readonly int[] ChapterTerm = { 5, 5, 4, 1 };
    private static readonly string[][] ChapterSteps =
    {
        new[] { "桑叶", "养蚕", "结茧" }, new[] { "择茧", "缫丝", "生丝" },
        new[] { "引纬", "织坯", "素绸" }, new[] { "染布", "裁片", "成衣" }
    };
    private static readonly string[] ChapterNotes =
    {
        "春蚕在蚕匾里吃适合蚕龄的桑叶，长成后才会吐丝结茧。",
        "挑茧以后，先煮茧、找丝头，再把丝绪均匀绕到丝轴上。",
        "梭子引纬，纬线与经线交错成布。这里记录基础素绸，宋锦显花另见工艺资料。",
        "布料染好后按样板裁片，再按裁片的形状组装缝制。"
    };

    private static WorkshopCraftRecordView current;
    private static int closedFrame = -1;
    private RectTransform frame;
    private CanvasGroup controls;
    private GameObject previousSelection;
    private Action onClose;
    private int resumedFrame = -1;

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
            // These printed areas are contextual controls in the result screen after the reward has been granted.
            Rect note = FromArtwork(PrintedInstructions[index]);
            WorkshopUi.Fill(frame, "Updated Status Surface", note, WorkshopUi.Paper);
            Label(frame, "Reward Stored", note, index == 3 ? "成衣已经缝好。" : "本章成果已经入库。", 28);
            close = WorkshopUi.PixelButton(frame, "Continue from Result", FromArtwork(PrintedButtons[index]),
                "继续", Close, true, 32);
        }
        else
        {
            var result = WorkshopUi.PixelCard(frame, "Chapter Result", ChapterArtwork, WorkshopUi.Paper, WorkshopUi.Bronze);
            Label(result, "Chapter", new Rect(50, 60, 908, 85), WorkshopProgress.SeasonName(season) + "章完成", 54);
            var product = season == WorkshopSeason.Winter ? WorkshopArt.Garment(WorkshopProgress.GarmentId) : Product(index);
            WorkshopUi.Picture(result, "Result", new Rect(284, 157, 440, 240), product);
            Label(result, "Product", new Rect(50, 398, 908, 56), WorkshopProgress.ProductName(season), 35);
            close = WorkshopUi.PixelButton(result, "Continue from Result", new Rect(376, 476, 256, 66),
                "继续", Close, true, 32);
        }

        var side = WorkshopUi.PixelCard(frame, "Chapter Record", new Rect(1142, 157, 675, 765),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        Label(side, "Heading", new Rect(40, 30, 595, 57), "本章工序回看", 39);
        HeaderRule(side, 100);

        var process = WorkshopUi.Rect(side, "Process", new Rect(35, 179, 605, 233));
        DrawChapterSteps(process, season, assets != null ? assets.GreigeSilk : null);
        Label(process, "Craft Note", new Rect(10, 145, 585, 83), ChapterNotes[index], 25, TextAnchor.UpperLeft);
        process.gameObject.SetActive(false);
        var collapse = WorkshopUi.PixelButton(side, "Toggle Process", new Rect(41, 116, 247, 54), "展开工序", null, true, 28);
        var collapseLabel = collapse.GetComponentInChildren<UnityEngine.UI.Text>();
        collapse.onClick.AddListener(() =>
        {
            bool expanded = !process.gameObject.activeSelf;
            process.gameObject.SetActive(expanded);
            collapseLabel.text = expanded ? "收起工序" : "展开工序";
        });

        Label(side, "Observed Heading", new Rect(40, 400, 595, 48), "已记录的观察", 30, TextAnchor.MiddleLeft);
        int count = 0;
        foreach (var observation in WorkshopObservationView.ObservationsFor(season))
        {
            var id = observation;
            if (!WorkshopObservationView.HasRecord(id)) continue;
            WorkshopUi.PixelButton(side, "Observation " + id, new Rect(40, 453 + count * 53, 595, 46),
                WorkshopObservationView.RecordTitle(id), () => OpenChild(done => WorkshopObservationView.ShowRecord(id, done)),
                false, 26);
            count++;
        }
        if (count == 0)
            Label(side, "No Observation", new Rect(40, 457, 595, 57), "回访本章工位后，观察记录会在这里出现。", 25);

        int term = ChapterTerm[index];
        bool hasTerm = WorkshopProgress.HasSolarTerms(season) && WorkshopArt.SolarCard(season, term) != null;
        var solar = WorkshopUi.PixelButton(side, "Related Solar Card", new Rect(40, 681, 277, 55),
            WorkshopArt.SolarNames(season)[term] + "节气卡", () => OpenChild(done => WorkshopKnowledgeView.ShowTerm(season, term, done)),
            false, 27);
        solar.interactable = hasTerm;
        WorkshopUi.PixelButton(side, "Regional Sources", new Rect(336, 681, 299, 55),
            "地方工艺与来源", () => OpenChild(done => WorkshopHeritageBrief.ShowSeason(season, done)), true, 27);
        KeepFocusIn(frame, close);
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
                steps = new[] { observation?.Leaf, observation?.SilkwormTray, Product(0) };
                break;
            case WorkshopSeason.Summer:
                steps = new[] { Product(0), observation?.SilkReel, Product(1) };
                break;
            case WorkshopSeason.Autumn:
                steps = new[] { observation?.Shuttle, greigeSilk, Product(2) };
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
            return dedicated ?? config?.silk ?? Product(2);
        }

        fabricTint = clothTint = Color.white;
        return Product(2);
    }

    private void BuildProduction()
    {
        var bounds = new Rect(124, 69, 1672, 941);
        var board = Resources.Load<Sprite>("WorkshopDelivery/B07");
        if (board != null) WorkshopUi.Picture(frame, "Production Board", bounds, board);
        else WorkshopUi.PixelCard(frame, "Production Board", bounds, WorkshopUi.Paper, WorkshopUi.Bronze);
        var content = WorkshopUi.Rect(frame, "Production Journey", bounds);
        Label(content, "Title", new Rect(595, 131, 482, 68), "制作记录", 47);
        var terms = new[]
        {
            WorkshopObservation.QingmingMulberry, WorkshopObservation.GuyuLeaf,
            WorkshopObservation.MangzhongCocoon, WorkshopObservation.DashuReeling,
            WorkshopObservation.QiufenOffering, WorkshopObservation.HanluLoom,
            WorkshopObservation.LidongDye, WorkshopObservation.XiaoxueTailoring
        };
        for (int i = 0; i < terms.Length; i++)
        {
            var term = terms[i];
            bool recorded = WorkshopObservationView.HasRecord(term);
            var view = WorkshopUi.PixelButton(content, "Order Term " + term,
                new Rect(401 + i % 4 * 218, 211 + i / 4 * 54, 207, 47),
                WorkshopObservationView.RecordTerm(term) + (recorded ? " · 回看" : " · 待录"),
                () => OpenChild(done => WorkshopObservationView.ShowRecord(term, done)), recorded, 22);
            view.interactable = recorded;
        }
        Label(content, "Journey", new Rect(429, 329, 811, 55), "从一片桑叶到一件成衣", 30);

        string garmentId = WorkshopProgress.GarmentId;
        var garment = WorkshopArt.Garment(garmentId);
        Sprite[] stages = { WorkshopObservationArt.Current?.Leaf, Product(0), Product(1), Product(2), garment };
        string[] names = { "桑叶", "蚕茧", "生丝", "素绸", garment != null ? WorkshopTailor.GarmentName(garmentId) : "成衣图未记录" };
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

    private static void HeaderRule(Transform parent, float y) =>
        WorkshopUi.Fill(parent, "Heading Rule", new Rect(40, y, 595, 2), WorkshopUi.Bronze);

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

    private static Sprite Product(int index)
    {
        string item = index == 0 ? "Cocoon" : index == 1 ? "RawSilk" : "PlainSilk";
        return Resources.Load<Sprite>("WorkshopFinish/" + item) ?? WorkshopArt.ChapterProduct((WorkshopSeason)index);
    }
}
