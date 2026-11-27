using System;
using System.Collections.Generic;
using JiangnanDye;
using UnityEngine;
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
    private static readonly Color WinterRemnantColor = new Color32(106, 152, 111, 255);
    private static readonly Color WinterStainColor = new Color32(113, 81, 70, 255);
    private static readonly Color WinterEmptyPattern = new Color32(222, 207, 175, 255);
    private static readonly Vector2Int[][] WinterPatternShapes =
    {
        new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(1, 2) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) },
        new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }
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
    private int winterCutSelected;
    private int winterCutTurn;
    private int winterCutStains;
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

    private bool PreservesWinterGrain(int piece) => (IsWinterPreparation || ChallengeVariant == 1) && piece < 2;
    private int WinterRemnantLongSide => ChallengeVariant == 1 ? 3 : 2;

    private void ResetWinterCutting()
    {
        Array.Clear(winterCutMasks, 0, winterCutMasks.Length);
        Array.Clear(winterCutAnchors, 0, winterCutAnchors.Length);
        Array.Clear(winterCutAngles, 0, winterCutAngles.Length);
        winterCutHistory.Clear();
        winterCutSelected = winterCutTurn = winterCutStains = 0;
        winterCutHover = null;
        winterCutWarning = false;
        winterCutColdRain = IsWinterPreparation || ChallengeVariant == 0;
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

        var worktopArt = GameArtSprite("WoodDesk");
        IllustratedGameCard(art, "Xiaoxue Plan Board", new Rect(38, 36, 1084, 580), worktopArt, WinterWorktop);
        if (worktopArt != null)
        {
            WorkshopUi.Fill(art, "Board Title Paper", new Rect(63, 54, 577, 53), WinterWorktop);
            WorkshopUi.Fill(art, "Shelf Title Paper", new Rect(669, 54, 420, 53), WinterWorktop);
            WorkshopUi.Fill(art, "Remnant Note Paper", new Rect(64, 581, 565, 33), WinterWorktop);
        }
        WorkshopUi.Label(art, "Xiaoxue Board Title", new Rect(73, 55, 574, 52),
            "试摆衣样 · 青格为整块余料", 31);
        DrawWinterCutShelf();
        DrawWinterCutBoard();

        winterCutSummary = WorkshopUi.Label(art, "Xiaoxue Remnant", new Rect(71, 582, 565, 33), "", 22);
        var strip = WorkshopUi.PixelCard(art, "Cut Feedback Strip", new Rect(41, 618, 1078, 33), LightSurface, WorkshopUi.Line);
        winterCutStatus = WorkshopUi.Label(strip, "Xiaoxue Feedback", new Rect(10, 0, 1058, 33), "", 22,
            WorkshopUi.Green, TextAnchor.MiddleCenter);

        string goal = $"避开布疵，给里襟留至少 2×{WinterRemnantLongSide} 格整布。";
        if (PreservesWinterGrain(0)) goal += "前后片只能顺纹掉头。";
        Copy("小雪 · 排布", "拖放三片衣样，或选衣样再点格子。" + goal);
        string confirm = onWinterPrepared != null ? "确认排布，去剪帛" : "确认排布";
        const string confirmControl = "Xiaoxue Confirm Layout";
        Act(confirmControl, confirm, ConfirmWinterLayout, true);
        var shears = GameArtSprite("TailorShears");
        if (onWinterPrepared != null && shears != null)
            WorkshopUi.Picture(infoPanel.Find(confirmControl), "Tailor Shears", new Rect(21, 7, 76, 47), shears);
        Act("Xiaoxue Undo", "撤回上一步", UndoWinterCut);
        DrawWinterCutMaterial();
        RefreshWinterCutting();
    }

    private void DrawWinterDryingRacks()
    {
        Sprite rackArt = GameArtSprite("DyeDryRack");
        Sprite cloth = GameArtSprite("CuttingCloth");
        Color dampTint = Color.Lerp(winterCutClothBase, new Color32(128, 147, 154, 255), winterCutMoisture * .21f);
        string[] names = { "临院透风", "背雨 · 闷角", "背雨 · 通风" };
        string[] conditions = { winterCutColdRain ? "雨能打进来" : "天晴 · 有风", "屋檐遮雨，空气滞留", "远离雨脚，有过风" };
        for (int i = 0; i < names.Length; i++)
        {
            int rack = i;
            var slot = WinterRackBounds(i);
            IllustratedGameCard(art, "Xiaoxue Drying Station " + i, slot, GameArtSprite("WoodDesk"), WinterWorktop);
            WorkshopUi.Picture(art, "Xiaoxue Rack " + i, new Rect(slot.x + 30, slot.y + 31, 285, 144), rackArt);
            GameArtSurface(art, "Xiaoxue Damp Cloth " + i, new Rect(slot.x + 106, slot.y + 85, 134, 93),
                cloth, dampTint);
            Tag("Xiaoxue Rack Conditions " + i, conditions[i], new Rect(slot.x + 12, slot.y + 185, 323, 43),
                i == 0 && winterCutColdRain);
            WinterControl(art, "Xiaoxue Rack Choice " + i, new Rect(slot.x + 29, slot.y + 248, 288, 51),
                names[i], () => TryDryWinterCloth(rack));
        }

        DrawWinterRackDrag(cloth, dampTint, names.Length);
        Copy(winterCutColdRain ? "小雪 · 冷雨晾布" : "小雪 · 晴间晾布",
            (winterCutColdRain ? "冷雨会打湿布，找个避雨通风的架子。" : "小雪刚放晴，挑个通风的架子。") +
            $"\n把湿布拖上晾架，也可点选。\n潮气 {winterCutMoisture} 格。{winterCutFeedback}", winterCutWarning);
        DrawWinterCutMaterial();
    }

    private static Rect WinterRackBounds(int rack) => new Rect(47 + rack * 360, 178, 347, 325);

    private void DrawWinterRackDrag(Sprite cloth, Color tint, int rackCount)
    {
        var source = GameArtSurface(art, "Xiaoxue Cloth In Hand", new Rect(486, 29, 188, 105), cloth, tint);
        source.raycastTarget = true;
        WorkshopUi.Label(source.transform, "Wet Cloth Label", new Rect(0, 0, 188, 105), "湿布", 25,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
        var ghost = GameArtSurface(art, "Moving Wet Cloth", new Rect(0, 0, 130, 84), cloth, tint);
        ghost.gameObject.SetActive(false);
        var events = source.gameObject.AddComponent<EventTrigger>();

        void MoveCloth(BaseEventData data)
        {
            var pointer = (PointerEventData)data;
            if (pointer.button != PointerEventData.InputButton.Left || !TryReadWinterCutPointer(pointer, out var position)) return;

            ghost.rectTransform.anchoredPosition = new Vector2(position.x - 65, 42 - position.y);
            ghost.gameObject.SetActive(true);
        }

        BindWinterEvent(events, EventTriggerType.BeginDrag, MoveCloth);
        BindWinterEvent(events, EventTriggerType.Drag, MoveCloth);
        BindWinterEvent(events, EventTriggerType.EndDrag, data =>
        {
            var pointer = (PointerEventData)data;
            if (pointer.button != PointerEventData.InputButton.Left) return;

            ghost.gameObject.SetActive(false);
            if (!TryReadWinterCutPointer(pointer, out var position)) return;

            for (int rack = 0; rack < rackCount; rack++)
            {
                if (!WinterRackBounds(rack).Contains(position)) continue;

                TryDryWinterCloth(rack);
                break;
            }
        });
    }

    private void TryDryWinterCloth(int rack)
    {
        if (winterCutMoisture == 0 || rack < 0 || rack > 2) return;
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
            WorkshopUi.Picture(card, "Pattern Silhouette", new Rect(13, 9, 118, 89), WorkshopArt.CutPiece(i), WinterPatternColors[i]);
            winterCutCardLabels[i] = WorkshopUi.Label(card, "Pattern State", new Rect(134, 8, 159, 91), "", 25);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    winterCutPatternGlyphs[i * 9 + y * 3 + x] = GameArtSurface(card, "Pattern Footprint",
                        new Rect(305 + x * 27, 15 + y * 27, 25, 25), weaveArt, WinterEmptyPattern);
            var button = WorkshopUi.Button(card, "Xiaoxue Piece " + i, new Rect(0, 0, bounds.width, bounds.height), "",
                () => SelectWinterPattern(piece), Color.clear);
            RegisterButton(button);
            BindWinterPatternDrag(button.gameObject, piece);
        }

        var rotate = WinterControl(art, "Xiaoxue Rotate", new Rect(674, 504, 411, 65), "旋转衣样", RotateWinterPattern, true, 27);
        winterCutTurnLabel = rotate.transform.Find("Label").GetComponent<Text>();
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

                var button = WorkshopUi.Button(art, $"Xiaoxue Cell {x}:{y}", bounds, "", () => PlaceWinterPattern(cell), Color.clear);
                RegisterButton(button);
                var events = button.gameObject.AddComponent<EventTrigger>();
                BindWinterEvent(events, EventTriggerType.PointerEnter, _ => SetWinterPreview(cell));
                BindWinterEvent(events, EventTriggerType.Select, _ => SetWinterPreview(cell));
                BindWinterEvent(events, EventTriggerType.PointerExit, _ => SetWinterPreview(null));
                BindWinterEvent(events, EventTriggerType.Deselect, _ => SetWinterPreview(null));
            }
        }
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
        winterCutSelected = piece;
        winterCutTurn = winterCutAngles[piece];
        SetWinterCutMessage(winterCutMasks[piece] == 0 ? "拖到布面摆放，也可以点一下格子。" : "原位置仍在，选到可用新位置后再移过去。",
            WorkshopAudioFeedback.Click);
    }

    private void BindWinterPatternDrag(GameObject source, int piece)
    {
        var events = source.AddComponent<EventTrigger>();
        BindWinterEvent(events, EventTriggerType.BeginDrag, data =>
        {
            var pointer = (PointerEventData)data;
            if (pointer.button != PointerEventData.InputButton.Left) return;

            pointer.eligibleForClick = false;
            if (winterCutSelected != piece) SelectWinterPattern(piece);
            SetWinterPreview(WinterCutCellAt(pointer));
        });
        BindWinterEvent(events, EventTriggerType.Drag, data =>
        {
            var pointer = (PointerEventData)data;
            if (pointer.button == PointerEventData.InputButton.Left)
                SetWinterPreview(WinterCutCellAt(pointer));
        });
        BindWinterEvent(events, EventTriggerType.EndDrag, data =>
        {
            var pointer = (PointerEventData)data;
            if (pointer.button != PointerEventData.InputButton.Left) return;

            Vector2Int? cell = WinterCutCellAt(pointer);
            SetWinterPreview(cell);
            if (cell.HasValue) PlaceWinterPattern(cell.Value);
        });
    }

    private Vector2Int? WinterCutCellAt(PointerEventData pointer)
    {
        if (!TryReadWinterCutPointer(pointer, out var position)) return null;

        Vector2 offset = (position - WinterClothOrigin) / WinterClothTile;
        var cell = new Vector2Int(Mathf.FloorToInt(offset.x), Mathf.FloorToInt(offset.y));
        return InsideWinterCloth(cell) ? cell : (Vector2Int?)null;
    }

    private bool TryReadWinterCutPointer(PointerEventData pointer, out Vector2 position)
    {
        bool found = RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera, out position);
        position.y = -position.y;
        return found;
    }

    private void RotateWinterPattern()
    {
        int step = PreservesWinterGrain(winterCutSelected) ? 2 : 1;
        winterCutTurn = (winterCutTurn + step) % 4;
        SetWinterCutMessage($"{WinterPatternNames[winterCutSelected]}转到 {winterCutTurn * 90}°，绿色预览即可落样。", WorkshopAudioFeedback.Fabric);
    }

    private void PlaceWinterPattern(Vector2Int anchor)
    {
        int piece = winterCutSelected;
        int footprint = WinterPatternMask(piece, anchor, winterCutTurn);
        if (footprint == 0 || (footprint & OccupiedWinterCloth(piece)) != 0)
        {
            SetWinterCutMessage("红色部分压到了布疵、布边或其他衣样。可以挪位或旋转。", WorkshopAudioFeedback.Notice, true);
            return;
        }

        if (winterCutMasks[piece] == footprint) return;

        winterCutHistory.Add(new WinterCutMove(piece, winterCutAnchors[piece], winterCutAngles[piece], winterCutMasks[piece]));
        bool moving = winterCutMasks[piece] != 0;
        winterCutMasks[piece] = footprint;
        winterCutAnchors[piece] = anchor;
        winterCutAngles[piece] = winterCutTurn;
        winterCutHover = null;

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
        winterCutHover = null;
        SetWinterCutMessage("已撤回这一步，其他衣样仍在原位。", WorkshopAudioFeedback.Fabric);
    }

    private void ConfirmWinterLayout()
    {
        if (current != this || winterCutMoisture > 0) return;
        if (WinterCutPlacedCount() != WinterPatternNames.Length)
        {
            SetWinterCutMessage("先摆齐前片、后片和袖片。", WorkshopAudioFeedback.Notice, true);
            return;
        }

        RectInt usable = FindWinterRemnant(OccupiedWinterCloth(), 2, WinterRemnantLongSide);
        if (usable.width == 0)
        {
            SetWinterCutMessage($"衣样已齐，整布还不到 2×{WinterRemnantLongSide}。试着挪一片留出空位。", WorkshopAudioFeedback.Notice, true);
            return;
        }

        var openCutting = onWinterPrepared;
        if (openCutting != null) WorkshopProgress.MarkStep(WorkshopWordRules.WinterPrepared);
        WorkshopAudioFeedback.Confirm();
        Close();
        openCutting?.Invoke();
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
        for (int y = 0; y < WinterClothRows; y++)
        {
            for (int x = 0; x < WinterClothColumns; x++)
            {
                int bit = WinterCutBit(x, y);
                int piece = WinterPieceAt(bit);
                Color color = WinterCutWeaveTint(x, y);
                if ((bit & winterCutStains) != 0 && !winterCutHasStainArt) color = WinterStainColor;
                else if (piece >= 0) color = WinterPatternColors[piece];
                else if (winterCutRemnant.Contains(new Vector2Int(x, y))) color = WinterRemnantColor;
                winterCutSurface[y * WinterClothColumns + x].color = color;
            }
        }

        for (int i = 0; i < WinterPatternNames.Length; i++)
        {
            bool selected = i == winterCutSelected;
            string state = winterCutMasks[i] != 0 ? "已摆 · 可移" : "待摆";
            if (selected) state = "已选 · " + winterCutTurn * 90 + "°";
            winterCutCardLabels[i].text = WinterPatternNames[i] + "\n" + state;
            winterCutCardLabels[i].color = selected ? WorkshopUi.Green : WorkshopUi.Ink;

            for (int cell = 0; cell < 9; cell++) winterCutPatternGlyphs[i * 9 + cell].color = WinterEmptyPattern;
            int turns = selected ? winterCutTurn : winterCutAngles[i];
            foreach (var offset in RotatedWinterFootprint(i, turns))
                winterCutPatternGlyphs[i * 9 + offset.y * 3 + offset.x].color = WinterPatternColors[i];
        }

        winterCutTurnLabel.text = PreservesWinterGrain(winterCutSelected) ? "顺纹掉头 180°" : "旋转衣样 90°";
        string remnant = winterCutRemnant.width > 0 ? $"{winterCutRemnant.width}×{winterCutRemnant.height}" : "无";
        winterCutSummary.text = $"衣样 {WinterCutPlacedCount()}/3 · 整布 {remnant} · 工单 ≥ 2×{WinterRemnantLongSide}";
        winterCutStatus.text = winterCutFeedback;
        winterCutStatus.color = winterCutWarning ? ErrorInk : WorkshopUi.Green;
        SetWinterPreview(winterCutHover);
    }

    private void SetWinterPreview(Vector2Int? target)
    {
        winterCutHover = target;
        foreach (var image in winterCutPreview)
            if (image != null) image.color = Color.clear;

        if (!target.HasValue) return;
        Vector2Int anchor = target.Value;
        int footprint = WinterPatternMask(winterCutSelected, anchor, winterCutTurn);
        bool legal = footprint != 0 && (footprint & OccupiedWinterCloth(winterCutSelected)) == 0;
        Color color = legal ? new Color32(204, 245, 176, 176) : new Color32(213, 76, 59, 179);
        foreach (var offset in RotatedWinterFootprint(winterCutSelected, winterCutTurn))
        {
            var cell = anchor + offset;
            if (InsideWinterCloth(cell)) winterCutPreview[cell.y * WinterClothColumns + cell.x].color = color;
        }
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

    private static Vector2Int[] RotatedWinterFootprint(int piece, int turns)
    {
        Vector2Int[] source = WinterPatternShapes[piece];
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

    private static int WinterPatternMask(int piece, Vector2Int anchor, int turns)
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

}
