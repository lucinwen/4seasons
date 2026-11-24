using System;
using System.Collections.Generic;
using System.Text;
using JiangnanDye;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private const int WinterClothColumns = 6;
    private const int WinterClothRows = 5;
    private const int WinterClothCells = WinterClothColumns * WinterClothRows;
    private const float WinterClothTile = 93f;
    private static readonly Vector2 WinterClothOrigin = new Vector2(73, 113);
    private static readonly string[] WinterPatternNames = { "前片", "后片", "袖片" };
    private static readonly Color[] WinterPatternColors =
    {
        new Color32(167, 83, 73, 255), new Color32(91, 127, 150, 255), new Color32(199, 153, 78, 255)
    };
    private static readonly Color WinterRemnantColor = new Color32(146, 187, 204, 255);
    private static readonly Color WinterRemnantEdge = new Color32(57, 114, 142, 255);
    private static readonly Color WinterStainColor = new Color32(113, 81, 70, 255);
    private static readonly Color WinterEmptyPattern = new Color32(222, 207, 175, 255);
    private static readonly Vector2Int[][] WinterPatternShapes =
    {
        new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(1, 2) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) },
        new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }
    };
    private static readonly Vector2Int[] WideWinterSleeve =
    {
        new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)
    };
    private static readonly string[][] WinterClothBoards =
    {
        new[] { "...##.", "....#.", "#.#...", "..#...", "..#..." },
        new[] { "....#.", "...#..", "..#...", "......", "#...#." }
    };

    private readonly struct WinterCutMove
    {
        internal readonly int Piece;
        internal readonly Vector2Int Anchor;
        internal readonly int Angle;
        internal readonly int Mask;

        internal WinterCutMove(int piece, Vector2Int anchor, int angle, int mask)
        {
            Piece = piece;
            Anchor = anchor;
            Angle = angle;
            Mask = mask;
        }
    }

    private readonly List<WinterCutMove> winterCutHistory = new List<WinterCutMove>();
    private readonly int[] winterCutMasks = new int[3];
    private readonly Vector2Int[] winterCutAnchors = new Vector2Int[3];
    private readonly int[] winterCutAngles = new int[3];
    private readonly Image[] winterCutSurface = new Image[WinterClothCells];
    private readonly Image[] winterCutPreview = new Image[WinterClothCells];
    private readonly Image[] winterCutPatternGlyphs = new Image[27];
    private readonly Text[] winterCutCardLabels = new Text[3];
    private Text winterCutStatus;
    private Text winterCutSummary;
    private Text winterCutTurnLabel;
    private Text winterCutGuide;
    private Button winterCutSnip;
    private Button winterCutUndo;
    private Button winterCutChangeSize;
    private Vector2Int? winterCutHintAnchor;
    private Vector2Int? winterCutTutorialAnchor;
    private RectTransform winterCutDemo;
    private RectTransform winterCutShearDemo;
    private RectTransform winterCutUndoCue;
    private RectTransform winterCutGoalOkay;
    private RectTransform winterCutGoalMissed;
    private RectTransform winterCutRemnantFrame;
    private bool winterCutTutorialUsed;
    private int winterCutLastRack;
    private int winterCutSelected;
    private int winterCutTurn;
    private int winterCutStains;
    private bool winterCutOrderCloth;
    private bool winterCutColdRain;
    private int winterCutMoisture;
    private RectInt winterCutRemnant;
    private Color winterCutClothBase;
    private string winterCutDyeSample;
    private string winterCutDyedColor;
    private string[] winterCutWeaveRows;
    private bool winterCutHasStainArt;
    private bool winterCutWarning;
    private string winterCutFeedback;
    private Vector2Int? winterCutHover;

    internal const string XiaoxueLiningStep = "winter.xiaoxue.lining";

    private static bool CanCutOrderCloth => SceneManager.GetActiveScene().name == WorkshopProgress.SceneName(WorkshopSeason.Winter) &&
        WorkshopProgress.Has(WorkshopItem.DyedCloth) && !WorkshopProgress.Has(WorkshopItem.CutPieces);

    private bool PreservesWinterGrain(int piece) => (winterCutOrderCloth || ChallengeVariant == 1) && piece < 2;
    private int WinterRemnantLongSide => ChallengeVariant == 1 ? 3 : 2;

    private void ResetWinterCutting()
    {
        Array.Clear(winterCutMasks, 0, winterCutMasks.Length);
        Array.Clear(winterCutAnchors, 0, winterCutAnchors.Length);
        Array.Clear(winterCutAngles, 0, winterCutAngles.Length);
        winterCutHistory.Clear();
        winterCutSelected = winterCutTurn = winterCutStains = 0;
        winterCutHover = winterCutHintAnchor = null;
        winterCutTutorialAnchor = null;
        winterCutTutorialUsed = false;
        winterCutLastRack = -1;
        winterCutWarning = false;
        winterCutOrderCloth = CanCutOrderCloth;
        ResetWinterSizing();
        winterCutColdRain = winterCutOrderCloth || ChallengeVariant == 0;
        winterCutMoisture = winterCutColdRain ? 2 : 1;
        winterCutFeedback = "染绸还有潮气。先选一处晾架。";
        LoadWinterCutMaterial();
        var board = WinterClothBoards[ChallengeVariant];
        for (int y = 0; y < WinterClothRows; y++)
            for (int x = 0; x < WinterClothColumns; x++)
                if (board[y][x] == '#') winterCutStains |= WinterCutBit(x, y);
    }

    private void LoadWinterCutMaterial()
    {
        winterCutClothBase = UndyedLinen;
        winterCutDyedColor = null;
        winterCutWeaveRows = OrderWork(WorkshopObservation.HanluLoom)?.sample?.Split('/');
        var work = OrderWork(WorkshopObservation.LidongDye)
            ?? BestWork(WorkshopObservation.LidongDye, ChallengeVariant)
            ?? BestWork(WorkshopObservation.LidongDye, 1 - ChallengeVariant);
        winterCutDyeSample = work?.sample;

        if (WorkshopProgress.Has(WorkshopItem.DyedCloth))
        {
            string savedColor = WorkshopProgress.DyedColor;
            foreach (DyeColor color in DyeColorUtil.AllColors)
            {
                string name = DyeColorUtil.ChineseName(color);
                if (savedColor != name && savedColor != name + "布") continue;
                var config = Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
                var tint = config != null ? config.GetClothTint(color) : DyeColorUtil.Tint(color);
                winterCutClothBase = Color.Lerp(UndyedLinen, tint, .4f);
                winterCutDyedColor = name;
                return;
            }
        }

        // Each dye order occupies three columns, with one uncolored column between them.
        if (string.IsNullOrEmpty(winterCutDyeSample) || winterCutDyeSample.Length < 7 || winterCutDyeSample[3] != '0') return;
        var ranges = WinterDyeRuns[work.order].Ranges;
        int shallowColumn = ranges[0].x <= ranges[1].x ? 0 : 4;
        int swatch = winterCutDyeSample[shallowColumn] - '0';
        if (swatch > 0 && swatch < SampleColors.Length)
            winterCutClothBase = Color.Lerp(UndyedLinen, SampleColors[swatch], .4f);
    }

    private void DrawWinterCutting()
    {
        Art("Winter Cutting Table", "WorkshopGameArt/XiaoxueRainCourtyard", ArtBounds, true);
        if (winterCutMoisture > 0)
        {
            DrawWinterDryingRacks();
            return;
        }

        if (winterCutSize == 0)
        {
            DrawWinterSizing();
            return;
        }

        var worktopArt = GameArtSprite("WoodDesk");
        IllustratedGameCard(art, "Xiaoxue Plan Board", new Rect(38, 36, 1084, 580), worktopArt, WinterWorktop);
        if (worktopArt != null)
        {
            WorkshopUi.Fill(art, "Board Title Paper", new Rect(63, 54, 577, 53), WinterWorktop);
            WorkshopUi.Fill(art, "Shelf Title Paper", new Rect(669, 54, 420, 53), WinterWorktop);
            WorkshopUi.Fill(art, "Remnant Note Paper", new Rect(64, 581, 565, 33), WinterWorktop);
        }
        WorkshopUi.Label(art, "Xiaoxue Board Title", new Rect(73, 55, 574, 52),
            "布面", 28);
        DrawWinterCutShelf();
        DrawWinterCutBoard();

        winterCutSummary = WorkshopUi.Label(art, "Xiaoxue Remnant", new Rect(71, 582, 565, 33), "", 22);
        var strip = WorkshopUi.PixelCard(art, "Cut Feedback Strip", new Rect(41, 618, 1078, 33), LightSurface, WorkshopUi.Line);
        winterCutStatus = WorkshopUi.Label(strip, "Xiaoxue Feedback", new Rect(10, 0, 1058, 33), "", 22,
            WorkshopUi.Green, TextAnchor.MiddleCenter);

        if (GameComplete)
        {
            string next = WorkshopProgress.Has(WorkshopItem.CutPieces)
                ? "前后片、袖片和里襟贴边料齐了。带上缝衣针，去主厅的大雪缝案。"
                : "排样已记录。等有正式染布后，将整幅布带回小雪裁衣台。";
            Copy("三片裁齐，留出余布", next);
        }
        else
        {
            Copy("小雪排料", $"三片衣样 · 留 {2}×{WinterRemnantLongSide} 里料");
            winterCutGuide = infoPanel.Find("Instruction Body").GetComponent<Text>();
            Act("Xiaoxue Snip", "确认下剪", CutWinterCloth, true);
            winterCutSnip = infoPanel.Find("Xiaoxue Snip").GetComponentInChildren<Button>();
            var shears = GameArtSprite("TailorShears");
            if (shears != null)
                WorkshopUi.Picture(infoPanel.Find("Xiaoxue Snip"), "Tailor Shears", new Rect(21, 7, 76, 47), shears);
            Act("Xiaoxue Undo", "撤回上一步", UndoWinterCut);
            winterCutUndo = infoPanel.Find("Xiaoxue Undo").GetComponentInChildren<Button>();
            winterCutUndoCue = VisualStep((RectTransform)winterCutUndo.transform);
            var remnantGoal = WorkshopUi.Rect(infoPanel, "Lining Shape Goal", new Rect(32, 178, 205, 89));
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < WinterRemnantLongSide; x++)
                    GameArtSurface(remnantGoal, "Lining Goal Cell", new Rect(x * 43, y * 43, 40, 40),
                        GameArtSprite("CuttingCloth"), WinterRemnantColor);
            winterCutGoalOkay = DrawWinterMark(remnantGoal, new Vector2(160, 43), true);
            winterCutGoalMissed = DrawWinterMark(remnantGoal, new Vector2(160, 43), false);
            winterCutChangeSize = winterSizingEnabled
                ? WinterControl(infoPanel, "Xiaoxue Change Fit", new Rect(239, 228, 231, 61), "改选纸样", ReopenWinterSizing)
                : null;
        }
        DrawWinterCutMaterial();
        winterCutDemo = winterCutShearDemo = null;
        DrawWinterCutDemonstration();
        RefreshWinterCutting();
    }

    private void DrawWinterDryingRacks()
    {
        Sprite rackArt = GameArtSprite("DyeDryRack");
        Sprite cloth = GameArtSprite("CuttingCloth");
        var drops = GuyuDewSprite();
        var dampCloth = GameArtSurface(art, "Xiaoxue Cloth In Hand", new Rect(486, 29, 188, 105),
            cloth, Color.Lerp(winterCutClothBase, new Color32(128, 147, 154, 255), winterCutMoisture * .21f));
        for (int i = 0; i < winterCutMoisture; i++)
            WorkshopUi.Picture(dampCloth.transform, "Remaining Moisture", new Rect(35 + i * 43, 31, 34, 46), drops);
        var grip = WorkshopUi.Fill(dampCloth.transform, "Grip Wet Cloth", new Rect(0, 0, 188, 105), Color.clear);
        grip.raycastTarget = true;
        var drag = grip.gameObject.AddComponent<EventTrigger>();
        var dragCloth = GameArtSurface(art, "Moving Wet Cloth", new Rect(0, 0, 130, 84), cloth, dampCloth.color);
        dragCloth.gameObject.SetActive(false);
        BindWinterEvent(drag, EventTriggerType.BeginDrag, data => MoveWinterRackCloth(dragCloth, (PointerEventData)data));
        BindWinterEvent(drag, EventTriggerType.Drag, data => MoveWinterRackCloth(dragCloth, (PointerEventData)data));
        BindWinterEvent(drag, EventTriggerType.EndDrag, data =>
        {
            dragCloth.gameObject.SetActive(false);
            var pointer = (PointerEventData)data;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera, out var local);
            for (int rack = 0; rack < 3; rack++)
                if (WinterRackBounds(rack).Contains(new Vector2(local.x, -local.y))) { TryDryWinterCloth(rack); return; }
        });
        string[] names = { "临院透风", "背雨 · 闷角", "背雨 · 通风" };
        string[] conditions = { winterCutColdRain ? "雨能打进来" : "天晴 · 有风", "屋檐遮雨，空气滞留", "远离雨脚，有过风" };
        for (int i = 0; i < names.Length; i++)
        {
            int rack = i;
            var slot = WinterRackBounds(i);
            var station = IllustratedGameCard(art, "Xiaoxue Drying Station " + i, slot, GameArtSprite("WoodDesk"), WinterWorktop);
            WorkshopUi.Picture(art, "Xiaoxue Rack " + i, new Rect(slot.x + 30, slot.y + 31, 285, 144), rackArt);
            bool rain = i == 0 && winterCutColdRain;
            if (rain)
                for (int j = 0; j < 4; j++)
                    WorkshopUi.Picture(station, "Rain Across Rack", new Rect(86 + j * 44, 78 + j % 2 * 32, 30, 42), drops);
            if (!rain && i != 1)
                for (int j = 0; j < 3; j++)
                {
                    Vector2 left = new Vector2(91 + j * 30, 89 + j * 22);
                    DrawCueStroke(station, left, left + new Vector2(108, 0), 6, new Color32(204, 240, 245, 255));
                    DrawCueStroke(station, left + new Vector2(89, -11), left + new Vector2(108, 0), 6, new Color32(204, 240, 245, 255));
                }
            if (i == winterCutLastRack)
                DrawWinterMark(station, new Vector2(172, 152), !winterCutWarning);
            Tag("Xiaoxue Rack Conditions " + i, conditions[i], new Rect(slot.x + 12, slot.y + 185, 323, 43),
                i == 0 && winterCutColdRain);
            WinterControl(art, "Xiaoxue Rack Choice " + i, new Rect(slot.x + 29, slot.y + 248, 288, 51),
                names[i] + " · 放布", () => TryDryWinterCloth(rack));
            var choose = WorkshopUi.Button(art, "Xiaoxue Rack Touch " + i,
                new Rect(slot.x + 12, slot.y + 18, 323, 164), "", () => TryDryWinterCloth(rack), Color.clear);
            RegisterButton(choose);
        }

        if (ChallengeVariant == 0)
        {
            int chosen = winterCutColdRain ? 2 : 0;
            var target = (RectTransform)art.Find("Xiaoxue Drying Station " + chosen);
            VisualStep(target);
            ShowGesture(art, new Vector2(580, 80), WinterRackBounds(chosen).center - new Vector2(0, 76), WorkGesture.Drag);
        }
        dragCloth.transform.SetAsLastSibling();

        Copy(winterCutColdRain ? "小雪 · 冷雨晾布" : "小雪 · 晴间晾布",
            winterCutWarning ? winterCutFeedback : "潮湿染绸 → 晾架", winterCutWarning);
        DrawWinterCutMaterial();
    }

    private void TryDryWinterCloth(int rack)
    {
        if (winterCutMoisture == 0 || rack < 0 || rack > 2) return;
        winterCutLastRack = rack;
        if (rack == 0 && winterCutColdRain)
        {
            winterCutMoisture = Mathf.Min(3, winterCutMoisture + 1);
            winterCutFeedback = "临院又被雨点打湿，换个架位。";
            winterCutWarning = true;
            WorkshopAudioFeedback.Notice();
        }
        else
        {
            winterCutMoisture = Mathf.Max(0, winterCutMoisture - (rack == 1 ? 1 : 2));
            winterCutFeedback = winterCutMoisture == 0 ? "布已干平，可以按经纹排料。"
                : rack == 1 ? "闷角干得慢，可以等一轮或移到通风架。" : "潮气散掉不少。再晾一轮。";
            winterCutWarning = false;
            WorkshopAudioFeedback.Fabric();
        }
        Render();
    }

    private static Rect WinterRackBounds(int rack) => new Rect(47 + rack * 360, 178, 347, 325);

    private void MoveWinterRackCloth(Image cloth, PointerEventData pointer)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera, out var local)) return;
        cloth.gameObject.SetActive(true);
        cloth.rectTransform.anchoredPosition = local - new Vector2(65, -42);
    }

    private void DrawWinterCutMaterial()
    {
        var preview = new Rect(27, 510, 168, 86);
        string description;
        if (winterCutDyedColor != null)
        {
            GameArtSurface(infoPanel, "Dyed Cloth Reference", preview, GameArtSprite("CuttingCloth"), winterCutClothBase);
            description = winterCutDyedColor + "染布\n" + (winterCutMoisture > 0 ? "先把整幅晾干" : "顺经纹排料");
            if (winterCutWeaveRows != null && DrawWorkSample(infoPanel, new Rect(34, 517, 154, 71), string.Join("/", winterCutWeaveRows)))
                description = winterCutDyedColor + "染布 · 寒露布纹\n" + (winterCutMoisture > 0 ? "先把整幅晾干" : "经向 ↑ 顺纹排料");
        }
        else if (DrawWorkSample(infoPanel, preview, winterCutDyeSample))
        {
            description = "立冬布样 · 排料参考\n按浅色样预览布面";
        }
        else
        {
            GameArtSurface(infoPanel, "Plain Cloth Reference", preview, GameArtSprite("CuttingCloth"), UndyedLinen);
            description = "小雪渐冷，织坊备冬衣\n先用素绸练习排样";
        }
        WorkshopUi.Label(infoPanel, "Cloth Reference Caption", new Rect(214, 510, 250, 86),
            description, 23, WorkshopUi.Ink);
    }

    private void DrawWinterCutShelf()
    {
        WorkshopUi.Label(art, "Pattern Shelf Heading", new Rect(679, 54, 408, 52), "衣样纸 · 可挪放", 30);
        var weaveArt = GameArtSprite("CuttingCloth");
        for (int i = 0; i < WinterPatternNames.Length; i++)
        {
            int piece = i;
            var bounds = new Rect(674, 116 + i * 125, 411, 109);
            var card = WorkshopUi.PixelCard(art, "Cloth Pattern " + i, bounds, LightSurface, WorkshopUi.Bronze);
            Rect silhouette = i == 2 && winterCutSize == 2 ? new Rect(9, 9, 126, 89) : new Rect(13, 9, 118, 89);
            WorkshopUi.Picture(card, "Pattern Silhouette", silhouette, WorkshopArt.CutPiece(i), WinterPatternColors[i]);
            winterCutCardLabels[i] = WorkshopUi.Label(card, "Pattern State", new Rect(134, 8, 159, 91), "", 25);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    winterCutPatternGlyphs[i * 9 + y * 3 + x] = GameArtSurface(card, "Pattern Footprint",
                        new Rect(305 + x * 27, 15 + y * 27, 25, 25), weaveArt, WinterEmptyPattern);
            if (GameComplete) continue;

            var button = WorkshopUi.Button(card, "Xiaoxue Piece " + i, new Rect(0, 0, bounds.width, bounds.height), "",
                () => SelectWinterPattern(piece), Color.clear);
            RegisterButton(button);
            BindWinterPatternDrag(button.gameObject, piece);
        }

        if (!GameComplete)
        {
            var button = WinterControl(art, "Xiaoxue Rotate", new Rect(674, 486, 411, 52), "旋转衣样", RotateWinterPattern, true, 26);
            winterCutTurnLabel = button.transform.Find("Label").GetComponent<Text>();
            WinterControl(art, "Xiaoxue Placement Hint", new Rect(674, 547, 411, 52), "没思路？提示一片位置", ShowWinterCutHint, textSize: 25);
        }
        else
        {
            var shears = GameArtSprite("TailorShears");
            if (shears != null)
                WorkshopUi.Picture(art, "Finished Cut Tailor Shears", new Rect(790, 485, 180, 103), shears);
        }
    }

    private void DrawWinterCutBoard()
    {
        var clothArt = GameArtSprite("CuttingCloth");
        var flawArt = GameArtSprite("ClothStain");
        winterCutHasStainArt = flawArt != null;
        for (int y = 0; y < WinterClothRows; y++)
        {
            for (int x = 0; x < WinterClothColumns; x++)
            {
                int index = y * WinterClothColumns + x;
                var cell = new Vector2Int(x, y);
                Rect bounds = new Rect(WinterClothOrigin.x + x * WinterClothTile, WinterClothOrigin.y + y * WinterClothTile,
                    WinterClothTile - 2, WinterClothTile - 2);
                var surface = GameArtSurface(art, "Cloth Cell " + index, bounds, clothArt, WinterCutWeaveTint(x, y));
                winterCutSurface[index] = surface;
                if ((winterCutStains & WinterCutBit(x, y)) != 0)
                {
                    if (winterCutHasStainArt)
                        WorkshopUi.Picture(surface.rectTransform, "Woven Fabric Flaw", new Rect(0, 0, bounds.width, bounds.height), flawArt, stretch: true);
                    WorkshopUi.Label(surface.rectTransform, "Cloth Flaw", new Rect(37, 54, 49, 29), "疵", 23,
                        winterCutHasStainArt ? WorkshopUi.Ink : WorkshopUi.Cream, TextAnchor.MiddleCenter);
                }
                winterCutPreview[index] = WorkshopUi.Fill(art, "Preview Cell " + index, bounds, Color.clear);

                if (GameComplete) continue;
                var button = WorkshopUi.Button(art, $"Xiaoxue Cell {x}:{y}", bounds, "", () => PlaceWinterPattern(cell), Color.clear);
                RegisterButton(button);
                var events = button.gameObject.AddComponent<EventTrigger>();
                BindWinterEvent(events, EventTriggerType.PointerEnter, _ => SetWinterPreview(cell));
                BindWinterEvent(events, EventTriggerType.Select, _ => SetWinterPreview(cell));
                BindWinterEvent(events, EventTriggerType.PointerExit, _ => SetWinterPreview(null));
                BindWinterEvent(events, EventTriggerType.Deselect, _ => SetWinterPreview(null));
            }
        }

        winterCutRemnantFrame = WorkshopUi.Rect(art, "Continuous Lining Outline", new Rect());
        var top = WorkshopUi.Fill(winterCutRemnantFrame, "Top", new Rect(0, 0, 0, 5), WinterRemnantEdge).rectTransform;
        top.anchorMax = Vector2.one;
        var bottom = WorkshopUi.Fill(winterCutRemnantFrame, "Bottom", new Rect(0, -5, 0, 5), WinterRemnantEdge).rectTransform;
        bottom.anchorMin = Vector2.zero;
        bottom.anchorMax = Vector2.right;
        var left = WorkshopUi.Fill(winterCutRemnantFrame, "Left", new Rect(0, 0, 5, 0), WinterRemnantEdge).rectTransform;
        left.anchorMin = Vector2.zero;
        var right = WorkshopUi.Fill(winterCutRemnantFrame, "Right", new Rect(-5, 0, 5, 0), WinterRemnantEdge).rectTransform;
        right.anchorMin = Vector2.right;
        right.anchorMax = Vector2.one;
    }

    private Color WinterCutWeaveTint(int x, int y)
    {
        if (winterCutWeaveRows == null || winterCutWeaveRows.Length == 0) return winterCutClothBase;
        string row = winterCutWeaveRows[y % winterCutWeaveRows.Length];
        if (row.Length == 0 || row[x % row.Length] <= '1') return winterCutClothBase;
        return Color.Lerp(winterCutClothBase, WorkshopUi.Clay, .14f);
    }

    private void SelectWinterPattern(int piece)
    {
        if (piece != winterCutSelected)
        {
            ClearWinterCutDemonstration();
            winterCutTurn = winterCutAngles[piece];
        }
        winterCutSelected = piece;
        winterCutHintAnchor = null;
        SetWinterCutMessage(winterCutMasks[piece] == 0 ? "移动到布上预览，点击落样。" : "原位置仍在，选到可用新位置后再移过去。",
            WorkshopAudioFeedback.Click);
    }

    private void RotateWinterPattern()
    {
        ClearWinterCutDemonstration();
        winterCutHintAnchor = null;
        int step = PreservesWinterGrain(winterCutSelected) ? 2 : 1;
        winterCutTurn = (winterCutTurn + step) % 4;
        SetWinterCutMessage($"{WinterPatternNames[winterCutSelected]}转到 {winterCutTurn * 90}°，绿色预览即可落样。", WorkshopAudioFeedback.Fabric);
    }

    private void PlaceWinterPattern(Vector2Int anchor)
    {
        if (GameComplete) return;

        int piece = winterCutSelected;
        int footprint = WinterPatternMask(piece, anchor, winterCutTurn);
        if (footprint == 0 || (footprint & OccupiedWinterCloth(piece)) != 0)
        {
            SetWinterCutMessage("红色部分压到了布疵、布边或其他衣样。可以挪位或旋转。", WorkshopAudioFeedback.Notice, true);
            return;
        }
        ClearWinterCutDemonstration();

        if (winterCutMasks[piece] == footprint) return;

        winterCutHistory.Add(new WinterCutMove(piece, winterCutAnchors[piece], winterCutAngles[piece], winterCutMasks[piece]));
        bool moving = winterCutMasks[piece] != 0;
        winterCutMasks[piece] = footprint;
        winterCutAnchors[piece] = anchor;
        winterCutAngles[piece] = winterCutTurn;
        winterCutHover = winterCutHintAnchor = null;

        if (!moving && WinterCutPlacedCount() < WinterPatternNames.Length)
        {
            for (int i = 1; i < WinterPatternNames.Length; i++)
            {
                int next = (piece + i) % WinterPatternNames.Length;
                if (winterCutMasks[next] != 0) continue;

                winterCutSelected = next;
                winterCutTurn = winterCutAngles[next];
                break;
            }
        }

        SetWinterCutMessage(moving ? "衣样已换位，看看留出的整布。" : "衣样放好了，青格会标出当前整块余料。", WorkshopAudioFeedback.Place);
    }

    private void UndoWinterCut()
    {
        if (GameComplete) return;
        if (winterCutHistory.Count == 0)
        {
            SetWinterCutMessage("落样后才有位置可以撤回。", WorkshopAudioFeedback.Notice);
            return;
        }

        int last = winterCutHistory.Count - 1;
        WinterCutMove move = winterCutHistory[last];
        winterCutHistory.RemoveAt(last);
        winterCutSelected = move.Piece;
        winterCutTurn = winterCutAngles[move.Piece] = move.Angle;
        winterCutMasks[move.Piece] = move.Mask;
        winterCutAnchors[move.Piece] = move.Anchor;
        winterCutHover = winterCutHintAnchor = null;
        SetWinterCutMessage("已撤回这一步，其他衣样仍在原位。", WorkshopAudioFeedback.Fabric);
    }

    private void CutWinterCloth()
    {
        if (GameComplete || winterCutMoisture > 0 || winterCutSize == 0) return;
        if (WinterCutPlacedCount() != WinterPatternNames.Length)
        {
            SetWinterCutMessage("先摆齐前片、后片和袖片，再下剪。", WorkshopAudioFeedback.Notice, true);
            return;
        }

        RectInt usable = FindWinterRemnant(OccupiedWinterCloth(), 2, WinterRemnantLongSide);
        if (usable.width == 0)
        {
            SetWinterCutMessage($"衣片已齐，整布还不到 2×{WinterRemnantLongSide}。试着挪一片留出空位。", WorkshopAudioFeedback.Notice, true);
            return;
        }

        winterCutFeedback = $"下剪完成 · 净布留下 {usable.width}×{usable.height}";
        winterCutWarning = false;
        winterCutHover = null;
        WorkshopAudioFeedback.Snip();
        string sample = WinterCutSample(usable);
        if (CanCutOrderCloth)
        {
            if (winterSizingEnabled)
                WorkshopProgress.SaveGarmentCut(winterCutSize, sample, usable.width * usable.height >= 6 ? 2 : 1);
            WorkshopProgress.MarkStep(XiaoxueLiningStep);
            WorkshopProgress.Award(WorkshopItem.CutPieces);
        }
        string fit = winterSizingEnabled ? WinterCutSizeLabel(winterCutSize) + " · " : "";
        FinishChallenge(usable.width * usable.height, $"{fit}三片齐 · 里襟留料 {usable.width}×{usable.height}", sample);
    }

    private void SetWinterCutMessage(string message, Action sound, bool warning = false)
    {
        winterCutFeedback = message;
        winterCutWarning = warning;
        sound();
        RefreshWinterCutting();
    }

    private void RefreshWinterCutting()
    {
        int occupied = OccupiedWinterCloth();
        RectInt usable = FindWinterRemnant(occupied, 2, WinterRemnantLongSide);
        winterCutRemnant = usable.width > 0 ? usable : FindWinterRemnant(occupied);

        for (int i = 0; i < WinterPatternNames.Length; i++)
        {
            bool selected = !GameComplete && i == winterCutSelected;
            string state = winterCutMasks[i] != 0 ? "已摆 · 可移" : "待摆";
            if (GameComplete) state = "已裁";
            else if (selected) state = "已选 · " + winterCutTurn * 90 + "°";
            winterCutCardLabels[i].text = WinterPatternNames[i] + "\n" + state;
            winterCutCardLabels[i].color = selected ? WorkshopUi.Green : WorkshopUi.Ink;

            for (int cell = 0; cell < 9; cell++) winterCutPatternGlyphs[i * 9 + cell].color = WinterEmptyPattern;
            int turns = selected ? winterCutTurn : winterCutAngles[i];
            foreach (var offset in RotatedWinterFootprint(i, turns))
                winterCutPatternGlyphs[i * 9 + offset.y * 3 + offset.x].color = WinterPatternColors[i];
        }

        if (!GameComplete)
        {
            int placed = WinterCutPlacedCount();
            winterCutSnip.interactable = placed == WinterPatternNames.Length && usable.width > 0;
            winterCutUndo.interactable = winterCutHistory.Count > 0;
            winterCutTurnLabel.text = PreservesWinterGrain(winterCutSelected) ? "顺纹掉头 180°" : "旋转衣样 90°";
            winterCutSnip.transform.parent.gameObject.SetActive(winterCutSnip.interactable);
            winterCutUndo.transform.parent.gameObject.SetActive(winterCutUndo.interactable);
            winterCutUndoCue.gameObject.SetActive(placed == WinterPatternNames.Length && usable.width == 0);
            if (winterCutChangeSize != null)
            {
                winterCutChangeSize.interactable = placed == 0;
                winterCutChangeSize.transform.parent.gameObject.SetActive(placed == 0);
            }
            string fit = winterSizingEnabled ? WinterCutSizeLabel(winterCutSize) + "\n" : "";
            winterCutGuide.text = $"{fit}{placed} / 3 衣片　·　里料 2×{WinterRemnantLongSide}";
            if (winterCutSnip.interactable && winterCutShearDemo == null)
            {
                VisualStep((RectTransform)winterCutSnip.transform);
                Vector2 at = WorkbenchPoint(infoPanel, (RectTransform)winterCutSnip.transform);
                winterCutShearDemo = ShowGesture(infoPanel, at, at, WorkGesture.Tap);
            }
            else if (!winterCutSnip.interactable && winterCutShearDemo != null)
            {
                Destroy(winterCutShearDemo.gameObject);
                winterCutShearDemo = null;
            }
        }
        winterCutStatus.text = winterCutFeedback;
        winterCutStatus.color = winterCutWarning ? ErrorInk : WorkshopUi.Green;
        SetWinterPreview(winterCutHover);
    }

    private void PaintWinterCutBoard(RectInt remnant, int liftingPiece = -1)
    {
        bool fitsLining = Mathf.Min(remnant.width, remnant.height) >= 2 &&
            Mathf.Max(remnant.width, remnant.height) >= WinterRemnantLongSide;
        winterCutRemnantFrame.gameObject.SetActive(fitsLining);
        if (fitsLining)
        {
            var left = WinterClothOrigin.x + remnant.x * WinterClothTile;
            var top = WinterClothOrigin.y + remnant.y * WinterClothTile;
            winterCutRemnantFrame.anchoredPosition = new Vector2(left, -top);
            winterCutRemnantFrame.sizeDelta = new Vector2(remnant.width, remnant.height) * WinterClothTile - Vector2.one * 2;
        }
        for (int y = 0; y < WinterClothRows; y++)
            for (int x = 0; x < WinterClothColumns; x++)
            {
                int bit = WinterCutBit(x, y);
                int piece = WinterPieceAt(bit);
                Color color = WinterCutWeaveTint(x, y);
                if ((bit & winterCutStains) != 0 && !winterCutHasStainArt) color = WinterStainColor;
                else if (piece >= 0 && piece != liftingPiece) color = WinterPatternColors[piece];
                else if (fitsLining && remnant.Contains(new Vector2Int(x, y))) color = WinterRemnantColor;
                winterCutSurface[y * WinterClothColumns + x].color = color;
            }
    }

    private void SetWinterPreview(Vector2Int? target)
    {
        winterCutHover = target;
        foreach (var image in winterCutPreview)
            if (image != null) image.color = Color.clear;

        target = target ?? winterCutHintAnchor ?? winterCutTutorialAnchor;
        if (!target.HasValue || GameComplete)
        {
            PaintWinterCutBoard(winterCutRemnant);
            UpdateWinterCutSummary(winterCutRemnant, "整布");
            if (!GameComplete) ShowWinterRemnantStatus(OccupiedWinterCloth(), false);
            return;
        }
        Vector2Int anchor = target.Value;
        int footprint = WinterPatternMask(winterCutSelected, anchor, winterCutTurn);
        int occupied = OccupiedWinterCloth(winterCutSelected);
        bool legal = footprint != 0 && (footprint & occupied) == 0;
        bool keepsLining = false;
        if (legal)
        {
            RectInt forecast = ShowWinterRemnantStatus(occupied | footprint, true);
            keepsLining = forecast.width > 0;
            PaintWinterCutBoard(forecast, winterCutSelected);
            UpdateWinterCutSummary(forecast, "预计里料");
        }
        else
        {
            PaintWinterCutBoard(winterCutRemnant);
            UpdateWinterCutSummary(winterCutRemnant, "整布");
            ShowWinterRemnantStatus(OccupiedWinterCloth(), false);
        }
        Color color = !legal ? new Color32(213, 76, 59, 179) : keepsLining
            ? new Color32(204, 245, 176, 176) : new Color32(241, 184, 102, 192);
        foreach (var offset in RotatedWinterFootprint(winterCutSelected, winterCutTurn))
        {
            var cell = anchor + offset;
            if (InsideWinterCloth(cell)) winterCutPreview[cell.y * WinterClothColumns + cell.x].color = color;
        }
    }

    private void UpdateWinterCutSummary(RectInt remnant, string caption)
    {
        string size = remnant.width > 0 ? $"{remnant.width}×{remnant.height}" : "不足";
        winterCutSummary.text = $"衣片 {WinterCutPlacedCount()}/3 · {caption} {size} · 工单 ≥ 2×{WinterRemnantLongSide}";
    }

    private RectInt ShowWinterRemnantStatus(int occupied, bool preview)
    {
        RectInt usable = FindWinterRemnant(occupied, 2, WinterRemnantLongSide);
        bool liningFits = usable.width > 0;
        winterCutGoalOkay.gameObject.SetActive(liningFits);
        winterCutGoalMissed.gameObject.SetActive(!liningFits && (preview || WinterCutPlacedCount() == WinterPatternNames.Length));
        winterCutStatus.text = preview && winterCutHover.HasValue
            ? liningFits ? "这里放得下，蓝色区域还能留作里襟。" : "这里放得下，但整块里料会被切碎。"
            : winterCutFeedback;
        winterCutStatus.color = (preview && !liningFits) || (!preview && winterCutWarning) ? ErrorInk : WorkshopUi.Green;
        return usable;
    }

    private void BindWinterPatternDrag(GameObject source, int piece)
    {
        var events = source.AddComponent<EventTrigger>();
        BindWinterEvent(events, EventTriggerType.BeginDrag, data =>
        {
            SelectWinterPattern(piece);
            PreviewDraggedWinterPattern((PointerEventData)data);
        });
        BindWinterEvent(events, EventTriggerType.Drag, data => PreviewDraggedWinterPattern((PointerEventData)data));
        BindWinterEvent(events, EventTriggerType.EndDrag, data =>
        {
            var cell = WinterCutPointer((PointerEventData)data);
            if (InsideWinterCloth(cell)) PlaceWinterPattern(cell);
            else SetWinterPreview(null);
        });
    }

    private Vector2Int WinterCutPointer(PointerEventData pointer)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera, out var local);
        return new Vector2Int(Mathf.FloorToInt((local.x - WinterClothOrigin.x) / WinterClothTile),
            Mathf.FloorToInt((-local.y - WinterClothOrigin.y) / WinterClothTile));
    }

    private void PreviewDraggedWinterPattern(PointerEventData pointer)
    {
        var cell = WinterCutPointer(pointer);
        SetWinterPreview(InsideWinterCloth(cell) ? cell : (Vector2Int?)null);
    }

    private void ShowWinterCutHint()
    {
        if (GameComplete) return;
        ClearWinterCutDemonstration();
        int piece = -1;
        for (int i = 0; i < WinterPatternNames.Length; i++)
            if (winterCutMasks[i] == 0) { piece = i; break; }
        if (piece < 0)
        {
            SetWinterCutMessage("衣样都在布上。若里襟不够，撤回一片后再提示。", WorkshopAudioFeedback.Notice);
            return;
        }
        int[] angles = new int[3];
        Vector2Int[] anchors = new Vector2Int[3];
        if (!FindWinterCutCompletion(0, OccupiedWinterCloth(), angles, anchors))
        {
            SetWinterCutMessage("现有摆位占住了后续衣片或里襟。撤回上一步后再试提示。", WorkshopAudioFeedback.Notice, true);
            return;
        }
        winterCutSelected = piece;
        winterCutTurn = angles[piece];
        winterCutHintAnchor = anchors[piece];
        winterCutHover = null;
        SetWinterCutMessage($"{WinterPatternNames[piece]}可放这里。点第 {anchors[piece].y + 1} 行、第 {anchors[piece].x + 1} 列的绿格左上角。",
            WorkshopAudioFeedback.Click);
    }

    private void DrawWinterCutDemonstration()
    {
        if (GameComplete || ChallengeVariant != 0 || winterCutTutorialUsed || WinterCutPlacedCount() != 0) return;
        int occupied = OccupiedWinterCloth();
        var nextAngles = new int[3];
        var nextAnchors = new Vector2Int[3];
        for (int y = 0; y < WinterClothRows; y++)
            for (int x = 0; x < WinterClothColumns; x++)
            {
                var cell = new Vector2Int(x, y);
                int mask = WinterPatternMask(0, cell, 0);
                if (mask == 0 || (mask & occupied) != 0 || !FindWinterCutCompletion(1, occupied | mask, nextAngles, nextAnchors)) continue;
                winterCutTutorialAnchor = cell;
                var piece = (RectTransform)art.Find("Cloth Pattern 0");
                winterCutDemo = WorkshopUi.Rect(art, "First Pattern Demonstration", new Rect());
                var border = VisualStep(piece);
                border.SetParent(winterCutDemo, true);
                Vector2 start = WorkbenchPoint(art, piece) - new Vector2(115, 0);
                Vector2 end = WinterClothOrigin + new Vector2(cell.x + .48f, cell.y + .48f) * WinterClothTile;
                ShowGesture(winterCutDemo, start, end, WorkGesture.Drag);
                return;
            }
    }

    private void ClearWinterCutDemonstration()
    {
        winterCutTutorialUsed = true;
        winterCutTutorialAnchor = null;
        if (winterCutDemo == null) return;
        winterCutDemo.gameObject.SetActive(false);
        Destroy(winterCutDemo.gameObject);
        winterCutDemo = null;
    }

    private bool FindWinterCutCompletion(int piece, int occupied, int[] angles, Vector2Int[] anchors)
    {
        if (piece == WinterPatternNames.Length)
            return FindWinterRemnant(occupied, 2, WinterRemnantLongSide).width > 0;
        if (winterCutMasks[piece] != 0) return FindWinterCutCompletion(piece + 1, occupied, angles, anchors);
        int step = PreservesWinterGrain(piece) ? 2 : 1;
        for (int turn = 0; turn < 4; turn += step)
            for (int y = 0; y < WinterClothRows; y++)
                for (int x = 0; x < WinterClothColumns; x++)
                {
                    var anchor = new Vector2Int(x, y);
                    int mask = WinterPatternMask(piece, anchor, turn);
                    if (mask == 0 || (mask & occupied) != 0) continue;
                    angles[piece] = turn;
                    anchors[piece] = anchor;
                    if (FindWinterCutCompletion(piece + 1, occupied | mask, angles, anchors)) return true;
                }
        return false;
    }

    private int WinterCutPlacedCount()
    {
        int count = 0;
        foreach (int mask in winterCutMasks)
            if (mask != 0) count++;
        return count;
    }

    private int OccupiedWinterCloth(int ignorePiece = -1)
    {
        int occupied = winterCutStains;
        for (int i = 0; i < winterCutMasks.Length; i++)
            if (i != ignorePiece) occupied |= winterCutMasks[i];
        return occupied;
    }

    private int WinterPieceAt(int bit)
    {
        for (int i = 0; i < winterCutMasks.Length; i++)
            if ((winterCutMasks[i] & bit) != 0) return i;
        return -1;
    }

    private Vector2Int[] RotatedWinterFootprint(int piece, int turns)
    {
        Vector2Int[] source = piece == 2 && winterCutSize == 2 ? WideWinterSleeve : WinterPatternShapes[piece];
        var tiles = new Vector2Int[source.Length];
        var min = new Vector2Int(int.MaxValue, int.MaxValue);
        for (int i = 0; i < source.Length; i++)
        {
            var position = source[i];
            for (int turn = 0; turn < turns; turn++) position = new Vector2Int(-position.y, position.x);
            tiles[i] = position;
            min = Vector2Int.Min(min, position);
        }
        for (int i = 0; i < tiles.Length; i++) tiles[i] -= min;
        return tiles;
    }

    private static bool InsideWinterCloth(Vector2Int cell) =>
        cell.x >= 0 && cell.x < WinterClothColumns && cell.y >= 0 && cell.y < WinterClothRows;

    private static int WinterCutBit(int x, int y) => 1 << (y * WinterClothColumns + x);

    private int WinterPatternMask(int piece, Vector2Int anchor, int turns)
    {
        int mask = 0;
        foreach (var offset in RotatedWinterFootprint(piece, turns))
        {
            var cell = anchor + offset;
            if (!InsideWinterCloth(cell)) return 0;
            mask |= WinterCutBit(cell.x, cell.y);
        }
        return mask;
    }

    private static RectInt FindWinterRemnant(int occupied, int minShort = 1, int minLong = 1)
    {
        RectInt best = default;
        for (int y = 0; y < WinterClothRows; y++)
        {
            for (int x = 0; x < WinterClothColumns; x++)
            {
                for (int width = minShort; width <= WinterClothColumns - x; width++)
                {
                    int strip = ((1 << width) - 1) << (y * WinterClothColumns + x);
                    for (int height = 1; height <= WinterClothRows - y; height++, strip <<= WinterClothColumns)
                    {
                        if ((strip & occupied) != 0) break;
                        if (height < minShort || Mathf.Max(height, width) < minLong) continue;

                        int area = width * height;
                        int bestArea = best.width * best.height;
                        if (area > bestArea || (area == bestArea && Mathf.Min(width, height) > Mathf.Min(best.width, best.height)))
                            best = new RectInt(x, y, width, height);
                    }
                }
            }
        }
        return best;
    }

    private string WinterCutSample(RectInt remnant)
    {
        var result = new StringBuilder();
        const string pieceColors = "354";
        for (int y = 0; y < WinterClothRows; y++)
        {
            if (y > 0) result.Append('/');
            for (int x = 0; x < WinterClothColumns; x++)
            {
                int bit = WinterCutBit(x, y);
                int piece = WinterPieceAt(bit);
                char color = '1';
                if ((winterCutStains & bit) != 0) color = '8';
                else if (piece >= 0) color = pieceColors[piece];
                else if (remnant.Contains(new Vector2Int(x, y))) color = '2';
                result.Append(color);
            }
        }
        return result.ToString();
    }
}
