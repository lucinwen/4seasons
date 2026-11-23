using System;
using System.Collections.Generic;
using System.Text;
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
    private RectInt winterCutRemnant;
    private bool winterCutHasStainArt;
    private bool winterCutWarning;
    private string winterCutFeedback;
    private Vector2Int? winterCutHover;

    private bool PreservesWinterGrain(int piece) => ChallengeVariant == 1 && piece < 2;
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
        winterCutFeedback = "选一张衣样，移到布上试摆。";
        var board = WinterClothBoards[ChallengeVariant];
        for (int y = 0; y < WinterClothRows; y++)
            for (int x = 0; x < WinterClothColumns; x++)
                if (board[y][x] == '#') winterCutStains |= WinterCutBit(x, y);
    }

    private void DrawWinterCutting()
    {
        Art("Winter Cutting Table", "WorkshopGuides/Winter_ChapterGuide", ArtBounds, true);
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

        if (GameComplete)
            Copy("三片裁齐，整块料留下了", "余下的整块布能用来做里衬或修补。按衣样错开落料，可以给下一道工序留出更整齐的布。");
        else
        {
            string goal = $"摆齐前片、后片和袖片，再留至少 2×{WinterRemnantLongSide} 的整块净布。";
            if (ChallengeVariant == 1) goal += "前后片要顺纹，只能掉头。";
            Copy("先排料，再下剪", goal + " 随时可以挪已摆的衣样，或撤回上一步。");
            Act("Xiaoxue Snip", "确认下剪", CutWinterCloth, true);
            var shears = GameArtSprite("TailorShears");
            if (shears != null)
                WorkshopUi.Picture(infoPanel.Find("Xiaoxue Snip"), "Tailor Shears", new Rect(21, 7, 76, 47), shears);
            Act("Xiaoxue Undo", "撤回上一步", UndoWinterCut);
        }
        RefreshWinterCutting();
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
            if (GameComplete) continue;

            var button = WorkshopUi.Button(card, "Xiaoxue Piece " + i, new Rect(0, 0, bounds.width, bounds.height), "",
                () => SelectWinterPattern(piece), Color.clear);
            RegisterButton(button);
        }

        if (!GameComplete)
        {
            var button = WinterControl(art, "Xiaoxue Rotate", new Rect(674, 504, 411, 65), "旋转衣样", RotateWinterPattern, true, 27);
            winterCutTurnLabel = button.transform.Find("Label").GetComponent<Text>();
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
                var surface = GameArtSurface(art, "Cloth Cell " + index, bounds, clothArt, UndyedLinen);
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
    }

    private void SelectWinterPattern(int piece)
    {
        winterCutSelected = piece;
        winterCutTurn = winterCutAngles[piece];
        SetWinterCutMessage(winterCutMasks[piece] == 0 ? "移动到布上预览，点击落样。" : "原位置仍在，选到可用新位置后再移过去。",
            WorkshopAudioFeedback.Click);
    }

    private void RotateWinterPattern()
    {
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
        winterCutHover = null;
        SetWinterCutMessage("已撤回这一步，其他衣样仍在原位。", WorkshopAudioFeedback.Fabric);
    }

    private void CutWinterCloth()
    {
        if (GameComplete) return;
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
        FinishChallenge(usable.width * usable.height, $"三片齐 · 完整余料 {usable.width}×{usable.height}", WinterCutSample(usable));
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
                Color color = UndyedLinen;
                if ((bit & winterCutStains) != 0) color = winterCutHasStainArt ? UndyedLinen : WinterStainColor;
                else if (piece >= 0) color = WinterPatternColors[piece];
                else if (winterCutRemnant.Contains(new Vector2Int(x, y))) color = WinterRemnantColor;
                winterCutSurface[y * WinterClothColumns + x].color = color;
            }
        }

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
            winterCutTurnLabel.text = PreservesWinterGrain(winterCutSelected) ? "顺纹掉头 180°" : "旋转衣样 90°";
        string remnant = winterCutRemnant.width > 0 ? $"{winterCutRemnant.width}×{winterCutRemnant.height}" : "无";
        winterCutSummary.text = $"衣片 {WinterCutPlacedCount()}/3 · 整布 {remnant} · 工单 ≥ 2×{WinterRemnantLongSide}";
        winterCutStatus.text = winterCutFeedback;
        winterCutStatus.color = winterCutWarning ? ErrorInk : WorkshopUi.Green;
        SetWinterPreview(winterCutHover);
    }

    private void SetWinterPreview(Vector2Int? target)
    {
        winterCutHover = target;
        foreach (var image in winterCutPreview)
            if (image != null) image.color = Color.clear;

        if (!target.HasValue || GameComplete) return;
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
