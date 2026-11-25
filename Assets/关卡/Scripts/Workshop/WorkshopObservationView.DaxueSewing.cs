using System;
using JiangnanDye;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private enum SewingPhase { Align, SleeveRoom, Stitch, Opening, Turn, Close }

    private static readonly Color SewingLining = new Color32(210, 227, 220, 255);
    private static readonly Color SewingSeamInk = new Color32(50, 91, 78, 255);
    private static readonly Color SewingThread = new Color32(255, 232, 169, 255);
    private static readonly Color SewingOffset = new Color32(221, 191, 154, 255);
    private static readonly Color TurnedLiningTint = new Color32(84, 126, 137, 255);
    private static readonly Color SewingJamHighlight = new Color32(255, 164, 129, 255);
    private const float SewingNeedleEyeRadius = 31f;
    private static readonly string[] SewingAlignmentNames = { "移到左位", "移到中位", "移到右位" };
    private static readonly string[] SewingSeamNames = { "肩缝 · 直边", "袖根 · 弯边", "里侧缝 · 转角" };
    private static readonly string[] SewingOpeningNames = { "袖口", "里布侧缝", "里布下摆" };
    private static readonly string[] SewingTurnNames = { "衣身", "衣襟", "袖子" };
    private static readonly int[][] SewingTurnOrders = { new[] { 2, 1, 0 }, new[] { 1, 2, 0 }, new[] { 0, 1, 2 } };
    private static readonly Rect SewingTurnCoat = new Rect(342, 100, 383, 441);
    private static readonly Rect SewingCoatTexture = new Rect(0, 0, 1, 1);
    private static readonly Vector2 TurningOpeningEdge = new Vector2(-.58f, 1f).normalized;
    private const float TurningOpeningIncrement = 22f;
    private static readonly Rect SewingCloseupBounds = new Rect(58, 88, 1034, 506);
    private static readonly Rect SewingCloseupCoat = new Rect(45, 64, 257, 296);
    private static readonly Vector2 SewingCloseupOrigin = new Vector2(559, 347);
    private static readonly Vector2 SewingCloseupPull = new Vector2(1f, -.16f).normalized;
    private const float SewingCloseupReach = 322f;
    private const float SewingCloseupGripOffset = 63f;
    private static readonly Vector2[] SewingOpeningPositions =
    {
        new Vector2(683, 312), new Vector2(627, 381), new Vector2(535, 511)
    };
    private static readonly Vector2[][] SewingRoutes =
    {
        new[] { new Vector2(169, 291), new Vector2(302, 291), new Vector2(435, 291), new Vector2(568, 291),
            new Vector2(701, 291), new Vector2(834, 291), new Vector2(967, 291) },
        new[] { new Vector2(192, 333), new Vector2(339, 312), new Vector2(461, 260), new Vector2(564, 231),
            new Vector2(667, 260), new Vector2(795, 312), new Vector2(942, 333) },
        new[] { new Vector2(179, 314), new Vector2(312, 314), new Vector2(445, 314), new Vector2(566, 314),
            new Vector2(661, 281), new Vector2(762, 247), new Vector2(908, 247) }
    };

    private SewingPhase sewingPhase;
    private int sewingSeam;
    private int sewingClothMark;
    private int sewingNeedlePoint;
    private int sewingWrinklePoint;
    private readonly bool[] sewingNeedleHoles = new bool[7];
    private int sewingTotalStitches;
    private int sewingCorrections;
    private int sewingOpening;
    private int sewingTurned;
    private int sewingClosed;
    private int sewingSleeveRoom;
    private string sewingFeedback;
    private bool sewingWarning;
    private bool sewingAnimate;
    private Color sewingCloth;
    private Text sewingFeedbackText;
    private bool sewingNeedleDragging;
    private int sewingNeedlePointer;
    private Vector2 sewingNeedleCursor;
    private readonly Button[] sewingEyeButtons = new Button[7];
    private RectTransform sewingPlacedStitches;
    private RectTransform sewingNeedleTool;
    private RectTransform sewingNeedleHint;
    private RectTransform sewingNeedleHintBorder;
    private RectTransform sewingNeedleGripCue;
    private RectTransform sewingNeedleWrinkle;
    private Image sewingLiveThread;
    private Image sewingLiveThreadShadow;

    private int SewingSeams => ChallengeVariant == 0 ? 2 : 3;
    private int SewingMarkTarget => (sewingSeam + ChallengeVariant + (WorkshopProgress.GarmentSize == 2 ? 1 : 0)) % 3;
    private int SewingOpeningNeed => ChallengeVariant != 0 || WorkshopProgress.GarmentSize == 2 ? 2 : 1;

    private void ResetSewingChallenge()
    {
        sewingPhase = SewingPhase.Align;
        sewingSeam = sewingNeedlePoint = sewingTotalStitches = sewingCorrections = sewingTurned = sewingClosed = 0;
        sewingClothMark = (SewingMarkTarget + 1) % 3;
        sewingWrinklePoint = -1;
        sewingOpening = -1;
        sewingSleeveRoom = 0;
        sewingFeedback = null;
        sewingWarning = sewingAnimate = false;
        sewingNeedleDragging = false;
        Array.Clear(sewingNeedleHoles, 0, sewingNeedleHoles.Length);
    }

    private void DrawDaxueSewing()
    {
        sewingFeedbackText = null;
        Layer("Winter_DyeSampleDesk");
        Art("Daxue Sewing Table", "WorkshopGameArt/DaxueSewingTable", ArtBounds, true);
        sewingCloth = DaxueClothTint();
        DrawSewingRibbon();
        if (GameComplete || readOnly)
        {
            DrawFinishedSewingGarment();
            Copy("夹衣缝好了", "表布已经翻正，衬里藏在里面。冬至还要让客人合襟、抬臂，检查能不能穿着活动。");
            return;
        }

        switch (sewingPhase)
        {
            case SewingPhase.Align: DrawSewingAlignment(); break;
            case SewingPhase.SleeveRoom: DrawSewingSleeveChoice(); break;
            case SewingPhase.Stitch: DrawSewingNeedleBoard(); break;
            case SewingPhase.Opening: DrawSewingOpeningChoice(); break;
            case SewingPhase.Turn: DrawSewingTurnBoard(); break;
            case SewingPhase.Close: DrawSewingCloseBoard(); break;
        }
        if (!string.IsNullOrEmpty(sewingFeedback))
        {
            var status = WorkshopUi.PixelCard(art, "Daxue Feedback", new Rect(45, 590, 1070, 48),
                sewingWarning ? new Color32(252, 224, 204, 255) : LightSurface, WorkshopUi.Bronze);
            sewingFeedbackText = WorkshopUi.Label(status, "Sewing Feedback", new Rect(14, 1, 1042, 46), sewingFeedback, 24,
                sewingWarning ? ErrorInk : SewingSeamInk, TextAnchor.MiddleCenter);
        }
        sewingAnimate = false;
    }

    private Color DaxueClothTint()
    {
        var tint = new Color32(219, 167, 89, 255);
        foreach (var color in DyeColorUtil.AllColors)
        {
            string name = DyeColorUtil.ChineseName(color);
            if (WorkshopProgress.DyedColor != name && WorkshopProgress.DyedColor != name + "布") continue;
            var config = Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
            return Color.Lerp(UndyedLinen, config != null ? config.GetClothTint(color) : DyeColorUtil.Tint(color), .65f);
        }
        return tint;
    }

    private void DrawSewingRibbon()
    {
        var ribbon = WorkshopUi.PixelCard(art, "Daxue Work Stages", new Rect(45, 19, 1070, 59), WorkshopUi.Ink, WorkshopUi.Bronze);
        int done = GameComplete || readOnly ? 3 : sewingPhase == SewingPhase.Close ? 2 :
            sewingPhase == SewingPhase.Opening || sewingPhase == SewingPhase.Turn ? 1 : 0;
        string[] stages = { "缝两层", "留口翻面", "收口" };
        for (int i = 0; i < stages.Length; i++)
        {
            Color tint = i == done ? SewingThread : i < done ? new Color32(199, 231, 181, 255) : new Color32(225, 217, 192, 255);
            WorkshopUi.Label(ribbon, "Stage " + i, new Rect(22 + i * 230, 0, 213, 57),
                (i < done ? "✓  " : i == done ? "▶  " : "○  ") + stages[i], 27, tint);
        }
        var samples = WorkshopUi.Rect(ribbon, "Small Snow Material Record", new Rect(800, 8, 242, 43));
        if (DrawWorkSample(samples, new Rect(153, 0, 86, 43), WorkshopProgress.GarmentCutSample))
            WorkshopUi.Label(samples, "Cut Batch", new Rect(0, 0, 144, 43), "小雪裁片", 23, WorkshopUi.Cream);
        else
            WorkshopUi.Label(samples, "Cut Batch", new Rect(0, 0, 239, 43),
                IsFormalGarmentOrder ? "小雪裁片" : "织坊备用裁片", 23, WorkshopUi.Cream, TextAnchor.MiddleRight);
    }

    private void DrawSewingAlignment()
    {
        int target = SewingMarkTarget;
        int offset = (sewingClothMark - target) * 91;
        var board = WorkshopUi.WorkbenchCard(art, "Daxue Alignment Board", new Rect(61, 99, 1038, 469), LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(board, "Seam Name", new Rect(22, 9, 434, 48), SewingSeamNames[sewingSeam], 30, SewingSeamInk);
        var cloth = GameArtSprite("CuttingCloth");
        var top = GameArtSurface(board, "Outer Cloth", new Rect(126, 81, 781, 120), cloth, sewingCloth);
        var bottom = GameArtSurface(board, "Lining Cloth", new Rect(126 + offset, 209, 781, 120), cloth, SewingLining);
        WorkshopUi.Label(top.transform, "Outer Edge Label", new Rect(32, 22, 190, 42), "表布", 29, WorkshopUi.Ink);
        WorkshopUi.Label(bottom.transform, "Lining Edge Label", new Rect(32, 51, 180, 43), "里布", 29, WorkshopUi.Ink);
        Vector2 topMark = new Vector2(519, 201);
        Vector2 lowerMark = new Vector2(519 + offset, 209);
        DrawCueStroke(board, topMark + new Vector2(-12, -24), topMark, 6, SewingThread);
        DrawCueStroke(board, topMark + new Vector2(12, -24), topMark, 6, SewingThread);
        Vector2 clothMark = new Vector2(393, 0);
        DrawCueStroke(bottom.transform, clothMark, clothMark + new Vector2(-12, 24), 6, SewingThread);
        DrawCueStroke(bottom.transform, clothMark, clothMark + new Vector2(12, 24), 6, SewingThread);
        var clip = GameArtSprite("BambooClip");
        if (clip != null)
            WorkshopUi.Picture(bottom.transform, "Bamboo Alignment Clip", new Rect(410, -20, 62, 61), clip);

        var grab = WorkshopUi.Fill(board, "Daxue Drag Lining", new Rect(104, 198, 829, 133), Color.clear);
        grab.raycastTarget = true;
        var drag = grab.gameObject.AddComponent<EventTrigger>();
        Vector2 begin = Vector2.zero;
        BindWinterEvent(drag, EventTriggerType.BeginDrag, input => begin = SewingLocal(board, (PointerEventData)input));
        BindWinterEvent(drag, EventTriggerType.Drag, input =>
        {
            float dx = Mathf.Clamp(SewingLocal(board, (PointerEventData)input).x - begin.x, -200, 200);
            bottom.rectTransform.anchoredPosition = new Vector2(126 + offset + dx, -209);
        });
        BindWinterEvent(drag, EventTriggerType.EndDrag, input =>
        {
            float dx = SewingLocal(board, (PointerEventData)input).x - begin.x;
            int choice = Mathf.Clamp(sewingClothMark + Mathf.RoundToInt(dx / 91f), 0, 2);
            SetSewingAlignment(choice);
        });

        for (int i = 0; i < 3; i++)
        {
            int slot = i;
            bool current = sewingClothMark == i;
            bool suggested = ChallengeVariant == 0 && i == target;
            var position = SewingButton(board, "Daxue Align " + i, new Rect(155 + i * 249, 352, 225, 73),
                current ? "当前位置" : SewingAlignmentNames[i], () => SetSewingAlignment(slot), current || suggested,
                current ? SewingOffset : SewingSeamInk);
            if (suggested) VisualStep((RectTransform)position.transform);
        }
        Copy("先夹齐布边", "拖里布，让两个缺口对上。也可以点下方位置。对上时夹子会扣住。", sewingWarning);
        if (sewingSeam == 0 && ChallengeVariant == 0 && sewingCorrections == 0)
            ShowGesture(board, new Vector2(lowerMark.x, 262), new Vector2(topMark.x, 262), WorkGesture.Drag);
    }

    private void SetSewingAlignment(int slot)
    {
        if (sewingPhase != SewingPhase.Align || GameComplete || readOnly) return;
        sewingClothMark = Mathf.Clamp(slot, 0, 2);
        if (slot != SewingMarkTarget)
        {
            float sign = slot > SewingMarkTarget ? -1f : 1f;
            ShowSewingFeedback(sign < 0 ? "下面的缺口偏右，再往左挪。" : "下面的缺口偏左，再往右挪。", true);
            return;
        }

        sewingPhase = sewingSeam == 1 ? SewingPhase.SleeveRoom : SewingPhase.Stitch;
        sewingNeedlePoint = 0;
        Array.Clear(sewingNeedleHoles, 0, sewingNeedleHoles.Length);
        sewingNeedleHoles[0] = true;
        sewingAnimate = true;
        WorkshopAudioFeedback.Place();
        ShowSewingFeedback("两层布已对齐，夹子固定好了。");
    }

    private void DrawSewingSleeveChoice()
    {
        int basis = WorkshopProgress.GarmentSize == 2 ? 1 : 0;
        var choices = new[] { basis, Mathf.Min(2, basis + 1) };
        for (int i = 0; i < choices.Length; i++)
        {
            var rect = new Rect(112 + i * 478, 143, 452, 381);
            var card = WorkshopUi.WorkbenchCard(art, "Sleeve Curve " + i, rect, LightSurface, WorkshopUi.Bronze);
            WorkshopUi.Picture(card, "Sleeve Sample", new Rect(102, 33, 245, 218), WorkshopArt.CutPiece(2), sewingCloth);
            int room = choices[i];
            Vector2 from = new Vector2(166, 213);
            Vector2 mid = new Vector2(226, 183 - room * 16);
            Vector2 to = new Vector2(288, 213);
            DrawCueStroke(card, from, mid, 6, SewingSeamInk);
            DrawCueStroke(card, mid, to, 6, SewingSeamInk);
            WorkshopUi.Label(card, "Sleeve Room Label", new Rect(25, 240, 402, 41), $"活动余量 · {room} 格", 25,
                SewingSeamInk, TextAnchor.MiddleCenter);
            SewingButton(card, "Daxue Sleeve Line " + i, new Rect(31, 292, 390, 64),
                i == 0 ? "按原线缝" : "袖根放松一格", () => ChooseSewingSleeveRoom(room), i == 1);
        }
        Copy("袖根选一道线", "保留活动余量，厚内搭时更容易抬手。沿原线缝则更贴身。");
    }

    private void ChooseSewingSleeveRoom(int room)
    {
        if (sewingPhase != SewingPhase.SleeveRoom || GameComplete || readOnly) return;
        sewingSleeveRoom = Mathf.Clamp(room, 0, 2);
        sewingPhase = SewingPhase.Stitch;
        WorkshopAudioFeedback.Place();
        ShowSewingFeedback("缝线定好了，沿这道袖弧落针。");
    }

    private void DrawSewingNeedleBoard()
    {
        var route = CurrentSewingRoute();
        bool showFirstStitch = sewingNeedlePoint == 0 && sewingSeam == 0 && ChallengeVariant == 0 && sewingWrinklePoint < 0;
        sewingNeedleDragging = false;
        sewingNeedleHint = sewingNeedleHintBorder = sewingNeedleGripCue = null;
        Array.Clear(sewingEyeButtons, 0, sewingEyeButtons.Length);
        var board = WorkshopUi.WorkbenchCard(art, "Daxue Needle Board", new Rect(61, 101, 1038, 462), LightSurface, WorkshopUi.Bronze);
        var cloth = GameArtSprite("CuttingCloth");
        var outer = GameArtSurface(art, "Aligned Outer Cloth", new Rect(121, 177, 917, 161), cloth, sewingCloth);
        var lining = GameArtSurface(art, "Aligned Lining Cloth", new Rect(121, 228, 917, 224), cloth, SewingLining);
        if (sewingAnimate && sewingNeedlePoint == 0)
            AnimateSpringSummerSprite(lining, new Vector2(0, -15), 1.02f, .21f);
        WorkshopUi.Label(board, "Seam Name", new Rect(20, 10, 536, 46), SewingSeamNames[sewingSeam], 29, SewingSeamInk);
        WorkshopUi.Label(board, "Seams Progress", new Rect(732, 10, 270, 46), $"{sewingSeam + 1} / {SewingSeams} 道缝", 25,
            SewingSeamInk, TextAnchor.MiddleRight);
        WorkshopUi.Picture(outer.transform, "Seam Clip", new Rect(18, 49, 70, 81), GameArtSprite("BambooClip"));
        for (int i = 1; i < route.Length; i++)
        {
            bool bend = SewingCrossesBend(i - 1, i);
            DrawCueStroke(art, route[i - 1], route[i], 4, bend ? new Color32(153, 77, 58, 255) : SewingSeamInk);
        }
        sewingPlacedStitches = WorkshopUi.Rect(art, "Stitches in the Cloth", new Rect());
        DrawSewingKnot(sewingPlacedStitches, route[0]);
        int previous = 0;
        for (int i = 1; i <= sewingNeedlePoint; i++)
        {
            if (!sewingNeedleHoles[i]) continue;
            DrawDaxueMadeStitch(route[previous], route[i]);
            previous = i;
        }

        for (int i = sewingNeedlePoint + 1; i < route.Length; i++)
        {
            int destination = i;
            Vector2 spot = route[i];
            Color point = showFirstStitch && i == 1 ? SewingThread : LightSurface;
            var mark = WorkshopUi.Button(art, "Daxue Stitch " + i, new Rect(spot.x - 28, spot.y - 27, 56, 54),
                "•", () => SewAtDaxueNeedle(destination), point, size: 36);
            sewingEyeButtons[i] = mark;
            RegisterButton(mark);
            if (showFirstStitch && i == 1) sewingNeedleHintBorder = VisualStep((RectTransform)mark.transform);
        }

        DrawDaxueNeedleTool(route, showFirstStitch);
        Copy("抓针沿布边缝", "按住针，滑过针眼。也可以直接点针眼。", sewingWarning);
        if (sewingNeedlePoint > 0) Act("Daxue Undo Stitch", "拆最近一针", UndoDaxueStitch);
        if (showFirstStitch) sewingNeedleHint = ShowGesture(art, route[0], route[1], WorkGesture.Drag);
        WorkshopUi.Label(board, "Needle Style Legend", new Rect(109, 357, 824, 70),
            sewingSeam == 0 ? "长针少走几步　·　一针最多跨 3 格" : "深红线是弯边　·　弯边一针走 1 格", 27,
            SewingSeamInk, TextAnchor.MiddleCenter);
    }

    private void DrawDaxueNeedleTool(Vector2[] route, bool showFirstStitch)
    {
        var anchor = route[sewingNeedlePoint];
        sewingNeedleWrinkle = WorkshopUi.Rect(art, "Pulled Cloth Wrinkle", new Rect());
        for (int i = -1; i <= 1; i++)
            DrawCueStroke(sewingNeedleWrinkle, new Vector2(i * 19, -32), new Vector2(i * 32, -70), 5, ErrorInk);

        sewingLiveThreadShadow = WorkshopUi.Fill(art, "Trailing Thread Shadow", new Rect(), GuideShadow);
        sewingLiveThread = WorkshopUi.Fill(art, "Trailing Thread", new Rect(), SewingThread);
        sewingNeedleTool = WorkshopUi.Rect(art, "Sewing Needle in Hand", new Rect());
        DrawCueStroke(sewingNeedleTool, new Vector2(-59, 0), new Vector2(-6, 0), 8, new Color32(205, 219, 219, 255));
        DrawCueStroke(sewingNeedleTool, new Vector2(-13, 0), Vector2.zero, 3, WorkshopUi.Cream);
        var eye = WorkshopUi.Fill(sewingNeedleTool, "Threaded Needle Eye", new Rect(-63, -6, 14, 12), GuideGold);
        WorkshopUi.ApplyPixelShape(eye);

        var grip = WorkshopUi.Fill(art, "Daxue Grab Needle", new Rect(anchor.x - 67, anchor.y - 64, 106, 103), Color.clear);
        grip.raycastTarget = true;
        if (showFirstStitch) sewingNeedleGripCue = VisualStep(grip.rectTransform);
        var drag = grip.gameObject.AddComponent<EventTrigger>();
        BindWinterEvent(drag, EventTriggerType.InitializePotentialDrag, input => ((PointerEventData)input).useDragThreshold = false);
        BindWinterEvent(drag, EventTriggerType.PointerDown, input => BeginDaxueNeedleDrag((PointerEventData)input, route));
        BindWinterEvent(drag, EventTriggerType.Drag, input => MoveDaxueNeedle((PointerEventData)input, route));
        BindWinterEvent(drag, EventTriggerType.PointerUp, input => EndDaxueNeedleDrag((PointerEventData)input, route));
        BindWinterEvent(drag, EventTriggerType.EndDrag, input => EndDaxueNeedleDrag((PointerEventData)input, route));
        UpdateDaxueNeedleVisuals(route, anchor);
    }

    private void BeginDaxueNeedleDrag(PointerEventData pointer, Vector2[] route)
    {
        if (pointer.button != PointerEventData.InputButton.Left || sewingNeedleDragging || sewingPhase != SewingPhase.Stitch) return;
        sewingNeedlePointer = pointer.pointerId;
        sewingNeedleDragging = true;
        sewingNeedleCursor = SewingLocal(art, pointer);
        if (sewingNeedleHint != null) sewingNeedleHint.gameObject.SetActive(false);
        if (sewingNeedleHintBorder != null) sewingNeedleHintBorder.gameObject.SetActive(false);
        if (sewingNeedleGripCue != null) sewingNeedleGripCue.gameObject.SetActive(false);
        UpdateDaxueNeedleVisuals(route, sewingNeedleCursor);
    }

    private void MoveDaxueNeedle(PointerEventData pointer, Vector2[] route)
    {
        if (!sewingNeedleDragging || pointer.pointerId != sewingNeedlePointer) return;
        var cursor = SewingLocal(art, pointer);
        cursor.x = Mathf.Clamp(cursor.x, 4, ArtWidth - 4);
        cursor.y = Mathf.Clamp(cursor.y, 4, ArtHeight - 4);
        Vector2 delta = cursor - sewingNeedleCursor;
        if (delta.sqrMagnitude > .01f)
        {
            bool returning = delta.x < 0;
            int step = returning ? -1 : 1;
            int first = returning ? route.Length - 1 : sewingNeedlePoint + 1;
            float sweepStart = Mathf.Min(sewingNeedleCursor.x, cursor.x) - SewingNeedleEyeRadius;
            float sweepEnd = Mathf.Max(sewingNeedleCursor.x, cursor.x) + SewingNeedleEyeRadius;
            for (int i = first; i > sewingNeedlePoint && i < route.Length; i += step)
            {
                var spot = route[i];
                if (spot.x < sweepStart || spot.x > sweepEnd) continue;
                if (returning && i - sewingNeedlePoint > DaxueNeedleSpanLimit(i)) continue;
                if (!DaxueNeedleGrazesEye(spot, sewingNeedleCursor, delta)) continue;
                if (SewAtDaxueNeedle(i, route)) return;
            }
        }
        sewingNeedleCursor = cursor;
        UpdateDaxueNeedleVisuals(route, cursor);
    }

    private static bool DaxueNeedleGrazesEye(Vector2 spot, Vector2 start, Vector2 delta)
    {
        float t = Mathf.Clamp01(Vector2.Dot(spot - start, delta) / delta.sqrMagnitude);
        return (spot - start - t * delta).sqrMagnitude <= SewingNeedleEyeRadius * SewingNeedleEyeRadius;
    }

    private void EndDaxueNeedleDrag(PointerEventData pointer, Vector2[] route)
    {
        if (!sewingNeedleDragging || pointer.pointerId != sewingNeedlePointer) return;
        MoveDaxueNeedle(pointer, route);
        if (!sewingNeedleDragging) return;
        sewingNeedleDragging = false;
        Render();
    }

    private void UpdateDaxueNeedleVisuals(Vector2[] route, Vector2 cursor)
    {
        Vector2 anchor = route[sewingNeedlePoint];
        Vector2 direction = cursor - anchor;
        if (!sewingNeedleDragging || direction.sqrMagnitude < 12 * 12) direction = new Vector2(3, 2);
        float angle = -Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        sewingNeedleTool.anchoredPosition = new Vector2(cursor.x, -cursor.y);
        sewingNeedleTool.localEulerAngles = new Vector3(0, 0, angle);
        sewingNeedleWrinkle.gameObject.SetActive(sewingWrinklePoint >= 0);
        if (sewingWrinklePoint >= 0)
        {
            var wrinkle = route[sewingWrinklePoint];
            sewingNeedleWrinkle.anchoredPosition = new Vector2(wrinkle.x, -wrinkle.y);
        }

        Vector2 tail = cursor - direction.normalized * 53;
        sewingLiveThread.color = sewingWarning ? ErrorInk : SewingThread;
        SetDaxueThreadLength(sewingLiveThreadShadow, anchor, tail, 11);
        SetDaxueThreadLength(sewingLiveThread, anchor, tail, 5);
    }

    private void SetDaxueThreadLength(Image thread, Vector2 anchor, Vector2 tail, float width)
    {
        thread.enabled = sewingNeedleDragging;
        var rect = thread.rectTransform;
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(anchor.x, -anchor.y);
        Vector2 line = tail - anchor;
        rect.sizeDelta = new Vector2(line.magnitude, width);
        rect.localEulerAngles = new Vector3(0, 0, -Mathf.Atan2(line.y, line.x) * Mathf.Rad2Deg);
    }

    private Vector2[] CurrentSewingRoute()
    {
        if (sewingSeam != 1 || sewingSleeveRoom == 0) return SewingRoutes[sewingSeam];
        var route = (Vector2[])SewingRoutes[sewingSeam].Clone();
        for (int i = 1; i < route.Length - 1; i++) route[i].y -= (3 - Mathf.Abs(3 - i)) * sewingSleeveRoom * 4;
        return route;
    }

    private bool SewingCrossesBend(int from, int to)
    {
        if (sewingSeam == 1) return from < 4 && to > 2;
        return sewingSeam == 2 && from < 5 && to > 3;
    }

    private int DaxueNeedleSpanLimit(int target) => SewingCrossesBend(sewingNeedlePoint, target) ? 1 : sewingSeam == 0 ? 3 : 2;

    private void DrawDaxueMadeStitch(Vector2 from, Vector2 to)
    {
        DrawCueStroke(sewingPlacedStitches, from, to, 8, SewingThread);
        DrawSewingKnot(sewingPlacedStitches, to);
    }

    private static void DrawSewingKnot(Transform parent, Vector2 spot, Color? color = null)
    {
        const float size = 17;
        var mark = WorkshopUi.Fill(parent, "Realized Needle Stitch", new Rect(spot.x - size / 2f, spot.y - size / 2f, size, size), color ?? SewingThread);
        WorkshopUi.ApplyPixelShape(mark);
    }

    // Report whether this stitch finished the board and rebuilt its controls.
    private bool SewAtDaxueNeedle(int target, Vector2[] draggedRoute = null)
    {
        bool duringDrag = draggedRoute != null;
        if (sewingPhase != SewingPhase.Stitch || GameComplete || readOnly || duringDrag != sewingNeedleDragging ||
            target <= sewingNeedlePoint || target >= sewingNeedleHoles.Length) return false;
        int span = target - sewingNeedlePoint;
        int limit = DaxueNeedleSpanLimit(target);
        if (span > limit)
        {
            if (duringDrag && sewingWrinklePoint == target) return false;
            sewingWrinklePoint = target;
            ShowSewingFeedback(limit == 1 ? "弯边拉皱了，回到近一点的针眼。" : "针距太长，回到近一点的针眼。", true, !duringDrag);
            return false;
        }

        sewingWrinklePoint = -1;
        int previous = sewingNeedlePoint;
        sewingNeedlePoint = target;
        sewingNeedleHoles[target] = true;
        sewingTotalStitches++;
        if (duringDrag)
        {
            DrawDaxueMadeStitch(draggedRoute[previous], draggedRoute[target]);
            for (int i = previous + 1; i <= target; i++)
                if (sewingEyeButtons[i] != null) sewingEyeButtons[i].gameObject.SetActive(false);
        }
        if (target < sewingNeedleHoles.Length - 1)
        {
            WorkshopAudioFeedback.Fabric();
            ShowSewingFeedback(span > 1 ? "长针稳住了这段直边。" : "针脚合拢了一小段。", false, !duringDrag);
            return false;
        }

        sewingNeedleDragging = false;
        sewingSeam++;
        if (sewingSeam >= SewingSeams)
        {
            sewingPhase = SewingPhase.Opening;
            WorkshopAudioFeedback.Milestone();
            ShowSewingFeedback("里布现在朝外。留个口，把金色外布翻出来。");
        }
        else
        {
            sewingPhase = SewingPhase.Align;
            sewingClothMark = (SewingMarkTarget + 2) % 3;
            WorkshopAudioFeedback.Collect();
            ShowSewingFeedback("这一边缝妥了，再夹齐下一道缝口。");
        }
        return true;
    }

    private void UndoDaxueStitch()
    {
        if (sewingPhase != SewingPhase.Stitch || sewingNeedlePoint == 0 || GameComplete || readOnly || sewingNeedleDragging) return;
        sewingNeedleHoles[sewingNeedlePoint] = false;
        sewingWrinklePoint = -1;
        while (sewingNeedlePoint > 0 && !sewingNeedleHoles[sewingNeedlePoint]) sewingNeedlePoint--;
        sewingTotalStitches--;
        WorkshopAudioFeedback.Fabric();
        ShowSewingFeedback("拆了最近一针，其他针还在。");
    }

    private void DrawSewingOpeningChoice()
    {
        DrawTurningCoat();
        for (int i = 0; i < SewingOpeningPositions.Length; i++)
        {
            int index = i;
            Vector2 at = SewingOpeningPositions[i];
            DrawTurningMouth(art, at, i);
            var hit = WorkshopUi.Button(art, "Daxue Opening " + i, new Rect(at.x - 36, at.y - 31, 127, 62), "",
                () => SelectDaxueOpening(index), Color.clear);
            RegisterButton(hit);
            if (i == SewingOpeningNeed)
            {
                preferredAction = hit;
                VisualStep((RectTransform)hit.transform, SewingThread);
            }
            var pin = WorkshopUi.PixelCard(art, "Opening Length " + i,
                new Rect(at.x + 38, at.y - 16, 53, 33), LightSurface, WorkshopUi.Bronze);
            WorkshopUi.Label(pin, "Length", new Rect(0, 0, 53, 33), (i + 1).ToString(), 23, SewingSeamInk,
                TextAnchor.MiddleCenter);
            Act("Daxue Opening Alternative " + i, SewingOpeningNames[i] + " · " + (i + 1) + " 格",
                () => SelectDaxueOpening(index));
        }
        var gauge = WorkshopUi.WorkbenchCard(art, "Fold Width Sample", new Rect(46, 195, 242, 209),
            LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(gauge, "Fold Sample Caption", new Rect(12, 18, 218, 43), "衣身需要的宽度", 27, SewingSeamInk,
            TextAnchor.MiddleCenter);
        int needed = SewingOpeningNeed + 1;
        float width = 31f * needed;
        GameArtSurface(gauge, "Folded Fabric Thickness", new Rect(121f - width * .5f, 77, width, 51),
            GameArtSprite("CuttingCloth"), sewingCloth);
        for (int i = 0; i < needed; i++)
        {
            float x = 121f - width * .5f + (i + .5f) * 31f;
            DrawCueStroke(gauge, new Vector2(x - 9f, 147), new Vector2(x + 9f, 147), 5f, SewingSeamInk);
        }
        Copy("留翻口", "选一个够衣身通过的开口。");
        DrawDaxueTurnGoal();
    }

    private void SelectDaxueOpening(int index)
    {
        if (sewingPhase != SewingPhase.Opening || GameComplete || readOnly || index < 0 || index >= SewingOpeningPositions.Length) return;
        sewingOpening = index;
        sewingPhase = SewingPhase.Turn;
        sewingTurned = 0;
        WorkshopAudioFeedback.Place();
        ShowSewingFeedback("抓住露出的金色布头，顺着箭头拉。");
    }

    private bool DaxueFoldCanPass => sewingTurned == 0 || sewingOpening >= SewingOpeningNeed;

    private void DrawSewingTurnBoard()
    {
        Vector2 origin = SewingCloseupOrigin;
        Vector2 direction = SewingCloseupPull;
        Vector2 start = origin + direction * SewingCloseupGripOffset;
        Vector2 destination = start + direction * SewingCloseupReach;
        int part = SewingTurnOrders[sewingOpening][sewingTurned];
        bool canPass = DaxueFoldCanPass;
        bool stuck = sewingWarning && !canPass;
        DrawSewingCloseup(false, out var faceMask, out var outsideDetail, out var coatState);
        var back = DrawDaxueOpeningLayer("Daxue Opening Back Layer", "DaxueOpeningBack");
        var foldArt = GameArtSprite("DaxueTurnFold");
        var fold = WorkshopUi.Picture(art, "Fabric at the Opening", new Rect(), foldArt ?? WorkshopArt.CutPiece(part),
            Color.Lerp(sewingCloth, Color.white, foldArt != null ? .83f : .62f), stretch: true);
        var nearLip = DrawDaxueOpeningLayer("Daxue Opening Front Lip", "DaxueOpeningLip");
        FitDaxueOpening(back, nearLip, stuck ? -1f : 0f);
        DrawDaxueOpeningTag(origin, stuck);
        float completed = sewingTurned / (float)SewingTurnNames.Length;
        PaintTurningFace(faceMask, outsideDetail, coatState, completed);
        PlaceDaxueFold(fold.rectTransform, origin, direction, stuck ? .18f : 0f);
        var target = WorkshopUi.Rect(art, "Daxue Turn Destination", SewingTouchArea(destination, 80, 73));
        if (!stuck) VisualStep(target, SewingThread);
        var grip = WorkshopUi.Button(art, "Daxue Turn Cloth " + part, SewingTouchArea(start, 74, 63),
            "", TurnDaxueCloth, Color.clear, enabled: !stuck);
        RectTransform tipCue = null;
        if (!stuck)
        {
            RegisterButton(grip);
            preferredAction = grip;
            var tip = WorkshopUi.Rect(art, "Daxue Cloth Tip Highlight", SewingTouchArea(start + direction * 26f, 46, 43));
            tipCue = VisualStep(tip, SewingThread);
        }
        else
        {
            DrawCueStroke(art, origin + new Vector2(10, -65), origin + new Vector2(41, -86), 7f, SewingJamHighlight);
            DrawCueStroke(art, origin + new Vector2(5, 59), origin + new Vector2(38, 77), 7f, SewingJamHighlight);
        }
        var gesture = !stuck ? ShowGesture(art, start, destination, WorkGesture.Drag).GetComponent<CanvasGroup>() : null;

        var events = grip.gameObject.AddComponent<EventTrigger>();
        BindWinterEvent(events, EventTriggerType.BeginDrag, input =>
        {
            var pointer = (PointerEventData)input;
            if (stuck || pointer.button != PointerEventData.InputButton.Left) return;
            pointer.eligibleForClick = false;
            if (tipCue != null) tipCue.gameObject.SetActive(false);
            if (gesture != null) gesture.alpha = 0f;
        });
        BindWinterEvent(events, EventTriggerType.Drag, input =>
        {
            if (stuck || ((PointerEventData)input).button != PointerEventData.InputButton.Left) return;
            float pull = DaxuePullDistance((PointerEventData)input, direction);
            float tension = Mathf.Clamp01(pull / SewingCloseupReach);
            if (!canPass)
                pull = Mathf.Min(pull, 37f + Mathf.Max(0f, pull - 37f) * .08f);
            float fraction = Mathf.Clamp01(pull / SewingCloseupReach);
            FitDaxueOpening(back, nearLip, canPass ? fraction : -tension);
            PlaceDaxueFold(fold.rectTransform, origin, direction, fraction);
            PaintTurningFace(faceMask, outsideDetail, coatState, completed + fraction * .88f / SewingTurnNames.Length);
        });
        BindWinterEvent(events, EventTriggerType.EndDrag, input =>
        {
            if (stuck || ((PointerEventData)input).button != PointerEventData.InputButton.Left) return;
            if (DaxuePullDistance((PointerEventData)input, direction) >= SewingCloseupReach * (canPass ? .75f : .33f))
            {
                TurnDaxueCloth();
                return;
            }
            PlaceDaxueFold(fold.rectTransform, origin, direction, 0f);
            FitDaxueOpening(back, nearLip, 0f);
            PaintTurningFace(faceMask, outsideDetail, coatState, completed);
            if (tipCue != null) tipCue.gameObject.SetActive(true);
            if (gesture != null) gesture.alpha = 1f;
        });

        var status = WorkshopUi.PixelCard(art, "Daxue Turn Steps", new Rect(828, 482, 250, 72), WorkshopUi.Ink, WorkshopUi.Bronze);
        for (int i = 0; i < SewingTurnNames.Length; i++)
            DrawSewingKnot(status, new Vector2(89 + i * 36, 22), i < sewingTurned ? SewingThread : WorkshopUi.Faded);
        WorkshopUi.Label(status, "Turning Progress", new Rect(0, 33, 250, 32), $"衣面翻正 {sewingTurned} / {SewingTurnNames.Length}", 24,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        Copy(stuck ? "翻口太窄" : "把金色外布拉出来", stuck ? "衣身过不去，换个宽一点的口。" : "抓住金色布头，朝箭头拖动。", stuck);
        DrawDaxueTurnGoal();
        if (!stuck) Act("Daxue Pull Exposed Fold", "拉出金色布头", TurnDaxueCloth);
        if (!stuck) return;
        var reopen = SewingButton(art, "Daxue Reopen", new Rect(660, 405, 206, 68),
            "拆开，换宽口", ReopenDaxueSewing, true);
        preferredAction = reopen;
        VisualStep((RectTransform)reopen.transform, SewingThread);
        Act("Daxue Reopen Alternative", "拆开，换宽口", ReopenDaxueSewing);
    }

    private float DaxuePullDistance(PointerEventData pointer, Vector2 direction)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.pressPosition, pointer.pressEventCamera, out var local);
        var start = new Vector2(local.x - art.rect.xMin, art.rect.yMax - local.y);
        return Mathf.Max(0, Vector2.Dot(SewingLocal(art, pointer) - start, direction));
    }

    private static void PlaceDaxueFold(RectTransform rect, Vector2 origin, Vector2 direction, float fraction)
    {
        Vector2 tail = origin - direction * 8f;
        float gripDistance = SewingCloseupGripOffset + fraction * SewingCloseupReach;
        rect.pivot = new Vector2(0f, .5f);
        rect.sizeDelta = new Vector2(gripDistance + 69f, Mathf.Lerp(104f, 177f, fraction));
        rect.anchoredPosition = new Vector2(tail.x, -tail.y);
        rect.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private void TurnDaxueCloth()
    {
        if (sewingPhase != SewingPhase.Turn || GameComplete || readOnly) return;
        if (!DaxueFoldCanPass)
        {
            if (!sewingWarning)
                ShowSewingFeedback("翻口比衣身窄，金色外布卡住了。", true);
            return;
        }
        int part = SewingTurnOrders[sewingOpening][sewingTurned];
        sewingTurned++;
        if (sewingTurned == SewingTurnNames.Length) sewingPhase = SewingPhase.Close;
        sewingAnimate = true;
        if (sewingPhase == SewingPhase.Close) WorkshopAudioFeedback.Milestone();
        else WorkshopAudioFeedback.Collect();
        ShowSewingFeedback(sewingPhase == SewingPhase.Close ? "金色外布已经朝外，把最后的开口缝上。"
            : SewingTurnNames[part] + "的金色外布翻出来了，接着拉。");
    }

    private void ReopenDaxueSewing()
    {
        if (sewingPhase != SewingPhase.Turn || GameComplete || readOnly) return;
        sewingPhase = SewingPhase.Opening;
        sewingOpening = -1;
        sewingTurned = 0;
        WorkshopAudioFeedback.Fabric();
        ShowSewingFeedback("翻口拆开了，可以在更宽的位置留口。");
    }

    private void DrawTurningCoat(bool turned = false)
    {
        DrawDaxueWholeCoat(art, SewingTurnCoat, turned);
        if (!turned)
            DrawDaxueFabricBadge(art, "Inside Out Badge", new Rect(365, 539, 329, 45), "里布在外", SewingSeamInk);
    }

    private static void DrawDaxueWholeCoat(Transform parent, Rect bounds, bool outside)
    {
        var garment = WorkshopArt.Garment("4_02");
        var lining = outside ? null : GameArtSprite("DaxueLiningCoat");
        Color tint = outside || lining != null ? Color.white : TurnedLiningTint;
        DrawWorkbenchSpriteCrop(parent, "Coat on the Table", bounds, lining ?? garment, SewingCoatTexture, tint);
    }

    private void DrawDaxueTurnGoal()
    {
        var goal = WorkshopUi.PixelCard(infoPanel, "Daxue Finished Coat Goal", new Rect(27, 165, 444, 132),
            LightSurface, WorkshopUi.Bronze);
        DrawDaxueWholeCoat(goal, new Rect(25, 7, 101, 116), true);
        WorkshopUi.Label(goal, "Daxue Goal Caption", new Rect(149, 13, 262, 43),
            "翻好以后", 25, SewingSeamInk);
        WorkshopUi.Label(goal, "Daxue Goal Result", new Rect(149, 57, 262, 52),
            "金色外布朝外", 30, WorkshopUi.Ink);
    }

    private static void DrawDaxueFabricBadge(Transform parent, string name, Rect bounds, string text, Color background)
    {
        var plate = WorkshopUi.PixelCard(parent, name, bounds, background, WorkshopUi.Bronze);
        WorkshopUi.Label(plate, name + " Text", new Rect(Vector2.zero, bounds.size),
            text, 26, WorkshopUi.Cream, TextAnchor.MiddleCenter);
    }

    private void DrawDaxueOpeningTag(Vector2 opening, bool stuck)
    {
        Vector2 end = opening + new Vector2(-38, 84);
        Color color = stuck ? SewingJamHighlight : SewingThread;
        DrawCueStroke(art, opening + new Vector2(-12, 41), end, 4, color);
        DrawDaxueFabricBadge(art, "Daxue Opening Tag", new Rect(end.x - 59, end.y - 2, 118, 44),
            stuck ? "卡在这里" : "翻口", stuck ? ErrorInk : WorkshopUi.Ink);
    }

    private RectTransform DrawDaxueOpeningLayer(string name, string sprite)
    {
        var layer = WorkshopUi.Picture(art, name, new Rect(), GameArtSprite(sprite), stretch: true);
        layer.rectTransform.pivot = new Vector2(.5f, .5f);
        Vector2 tangent = -TurningOpeningEdge;
        layer.rectTransform.localEulerAngles = new Vector3(0f, 0f,
            -Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
        return layer.rectTransform;
    }

    private void FitDaxueOpening(RectTransform back, RectTransform nearLip, float pull)
    {
        float closed = sewingPhase == SewingPhase.Close ? sewingClosed / (float)sewingOpening : 0f;
        float length = 113f + 46f * sewingOpening;
        Vector2 at = SewingCloseupOrigin + TurningOpeningEdge * (length * .31f * closed);
        float depth = (72f + 12f * sewingOpening) * (1f + .19f * pull - .58f * closed);
        var size = new Vector2(length * (1f - .62f * closed), depth);
        Vector2 normal = new Vector2(TurningOpeningEdge.y, -TurningOpeningEdge.x);
        Vector2 front = at + normal * (depth * .055f * (1f + Mathf.Min(0f, pull)) * (1f - closed));
        back.anchoredPosition = new Vector2(at.x, -at.y);
        nearLip.anchoredPosition = new Vector2(front.x, -front.y);
        back.sizeDelta = nearLip.sizeDelta = size;
    }

    private void DrawSewingCloseup(bool turned, out CanvasGroup faceVisibility, out CanvasGroup outsideDetail, out Text coatState)
    {
        WorkshopUi.PixelCard(art, "Daxue Close-up Frame", new Rect(52, 82, 1046, 513), WorkshopUi.Ink, WorkshopUi.Bronze);
        Art("Daxue Close-up Fabric", "WorkshopGameArt/DaxueTurnCloseup", SewingCloseupBounds, true);
        var goldDetail = GameArtSprite("DaxueTurnedCloseup");
        outsideDetail = goldDetail != null
            ? WorkshopUi.Picture(art, "Daxue Golden Fabric Emerging", SewingCloseupBounds, goldDetail, stretch: true)
                .gameObject.AddComponent<CanvasGroup>()
            : null;
        if (outsideDetail != null) outsideDetail.blocksRaycasts = false;

        var map = WorkshopUi.PixelCard(art, "Daxue Full Coat Map", new Rect(75, 106, 347, 450), LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(map, "Whole Coat Caption", new Rect(0, 11, 347, 43),
            "手上的夹衣", 27, SewingSeamInk, TextAnchor.MiddleCenter);
        if (!turned) DrawDaxueWholeCoat(map, SewingCloseupCoat, false);
        var faceMask = WorkshopUi.Rect(map, "Coat Surface Emerging", SewingCloseupCoat);
        faceVisibility = faceMask.gameObject.AddComponent<CanvasGroup>();
        faceVisibility.blocksRaycasts = false;
        faceMask.gameObject.AddComponent<RectMask2D>();
        DrawDaxueWholeCoat(faceMask, new Rect(Vector2.zero, SewingCloseupCoat.size), true);

        Vector2 offset = SewingOpeningPositions[sewingOpening] - SewingTurnCoat.position;
        Vector2 mark = SewingCloseupCoat.position + offset * (SewingCloseupCoat.width / SewingTurnCoat.width);
        DrawSewingKnot(map, mark, SewingSeamInk);
        DrawSewingKnot(map, mark + new Vector2(-1, -1), SewingThread);
        var state = WorkshopUi.PixelCard(map, "Daxue Facing Badge", new Rect(45, 370, 257, 58), WorkshopUi.Ink, WorkshopUi.Bronze);
        coatState = WorkshopUi.Label(state, "Daxue Garment Facing", new Rect(6, 0, 245, 58),
            "", 30, SewingThread, TextAnchor.MiddleCenter);
    }

    private static void PaintTurningFace(CanvasGroup face, CanvasGroup outsideDetail, Text coatState, float fraction)
    {
        face.alpha = fraction > .001f ? 1f : 0f;
        var mask = (RectTransform)face.transform;
        mask.sizeDelta = new Vector2(SewingCloseupCoat.width, SewingCloseupCoat.height * fraction);
        if (outsideDetail != null) outsideDetail.alpha = fraction;
        coatState.text = fraction <= .001f ? "里布在外" : fraction >= 1f ? "✓ 金色外布在外" : "金色外布翻出中";
    }

    private static void DrawTurningMouth(Transform parent, Vector2 at, int opening)
    {
        Vector2 edge = TurningOpeningEdge;
        float length = TurningOpeningIncrement * (opening + 1);
        var start = at - edge * length * .5f;
        var end = at + edge * length * .5f;
        Vector2 rim = new Vector2(edge.y, -edge.x) * 6.5f;
        DrawCueStroke(parent, start, end, 20f, WorkshopUi.PixelInk);
        DrawCueStroke(parent, start - rim, end - rim, 2.8f, SewingThread);
        DrawCueStroke(parent, start + rim, end + rim, 2.8f, SewingThread);
        for (int i = 0; i <= opening; i++)
        {
            Vector2 middle = start + edge * ((i + .5f) * TurningOpeningIncrement);
            DrawCueStroke(parent, middle - rim * 1.3f, middle - rim * .7f, 2.8f, SewingThread);
        }
        DrawSewingKnot(parent, start, SewingThread);
        DrawSewingKnot(parent, end, SewingThread);
    }

    private static Rect SewingTouchArea(Vector2 at, float x, float y) => new Rect(at.x - x, at.y - y, x * 2f, y * 2f);

    private void DrawFinishedSewingGarment()
    {
        DrawTurningCoat(true);
        WorkshopUi.Label(art, "Garment State", new Rect(363, 552, 340, 47), "表里已合 · 可去试衣", 29,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
    }

    private void DrawSewingCloseBoard()
    {
        DrawSewingCloseup(true, out var face, out var outsideDetail, out var coatState);
        PaintTurningFace(face, outsideDetail, coatState, 1f);
        int need = sewingOpening;
        Vector2 at = SewingCloseupOrigin;
        Vector2 edge = TurningOpeningEdge;
        var back = DrawDaxueOpeningLayer("Daxue Opening Back Layer", "DaxueOpeningBack");
        var nearLip = DrawDaxueOpeningLayer("Daxue Opening Front Lip", "DaxueOpeningLip");
        FitDaxueOpening(back, nearLip, 0f);
        DrawDaxueOpeningTag(at, false);
        for (int i = 0; i < need; i++)
        {
            Vector2 spot = at + edge * (i - (need - 1) * .5f) * 68f;
            if (i < sewingClosed)
            {
                DrawCueStroke(art, spot - new Vector2(28f, 16f), spot + new Vector2(28f, 16f), 12f, SewingThread);
                continue;
            }
            DrawSewingKnot(art, spot, i == sewingClosed ? SewingThread : WorkshopUi.Faded);
            if (i != sewingClosed) continue;
            var close = WorkshopUi.Button(art, "Daxue Close Stitch", SewingTouchArea(spot, 68, 54), "",
                CloseDaxueOpening, Color.clear);
            RegisterButton(close);
            preferredAction = close;
            VisualStep((RectTransform)close.transform, SewingThread);
            if (sewingClosed == 0)
                ShowGesture(art, spot, spot, WorkGesture.Tap);
        }
        var status = WorkshopUi.PixelCard(art, "Daxue Closing Steps", new Rect(828, 482, 250, 72), WorkshopUi.Ink, WorkshopUi.Bronze);
        WorkshopUi.Label(status, "Closing Progress", new Rect(0, 0, 250, 72), $"翻口收针 {sewingClosed} / {need}", 27,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        Copy("翻衣完成", "点击亮起的针眼补针收口。");
        Act("Daxue Close from Rail", "补一针收口", CloseDaxueOpening);
    }

    private void CloseDaxueOpening()
    {
        if (sewingPhase != SewingPhase.Close || GameComplete || readOnly) return;
        sewingClosed++;
        if (sewingClosed < sewingOpening)
        {
            WorkshopAudioFeedback.Fabric();
            ShowSewingFeedback("翻口折边合上了一部分。");
            return;
        }
        SaveGarmentSewingResult(sewingSleeveRoom, sewingOpening);
        int economicalStitches = SewingSeams == 2 ? 6 : 11;
        int score = Mathf.Max(1, 100 - sewingCorrections * 4 - Mathf.Max(0, sewingTotalStitches - economicalStitches)
            - (sewingOpening - SewingOpeningNeed));
        string stitches = "044440/044440/000000";
        if (sewingSleeveRoom > 0) stitches = "044440/042240/022220";
        FinishChallenge(score, $"表里缝合 · 袖根余量 {sewingSleeveRoom} 格 · 针脚 {sewingTotalStitches + sewingClosed}", stitches);
    }

    private Button SewingButton(Transform parent, string name, Rect bounds, string label, Action click, bool primary, Color? primaryColor = null)
    {
        var button = WorkshopUi.PixelButton(parent, name, bounds, label, click, primary, 24, primaryColor ?? SewingSeamInk);
        RegisterButton(button);
        return button;
    }

    private void ShowSewingFeedback(string text, bool warning = false, bool refresh = true)
    {
        sewingFeedback = text;
        sewingWarning = warning;
        if (warning)
        {
            sewingCorrections++;
            WorkshopAudioFeedback.Notice();
        }
        if (refresh) Render();
        else if (sewingFeedbackText != null)
        {
            sewingFeedbackText.text = text;
            sewingFeedbackText.color = warning ? ErrorInk : SewingSeamInk;
        }
    }

    private static Vector2 SewingLocal(RectTransform parent, PointerEventData pointer)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pointer.position, pointer.pressEventCamera, out var point);
        return new Vector2(point.x - parent.rect.xMin, parent.rect.yMax - point.y);
    }
}
