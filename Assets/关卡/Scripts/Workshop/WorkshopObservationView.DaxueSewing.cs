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
    private const float SewingNeedleEyeRadius = 31f;
    private static readonly string[] SewingAlignmentNames = { "移到左位", "移到中位", "移到右位" };
    private static readonly string[] SewingSeamNames = { "肩缝 · 直边", "袖根 · 弯边", "里侧缝 · 转角" };
    private static readonly string[] SewingOpeningNames = { "袖口", "里布侧缝", "里布下摆" };
    private static readonly string[] SewingTurnNames = { "衣身", "里襟", "袖子" };
    private static readonly int[][] SewingTurnOrders = { new[] { 2, 1, 0 }, new[] { 1, 2, 0 }, new[] { 0, 1, 2 } };
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
            DrawSewingGarment(false);
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
            WorkshopUi.Label(samples, "Practice Stock", new Rect(0, 0, 239, 43), "织坊备用裁片", 23, WorkshopUi.Cream, TextAnchor.MiddleRight);
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

    private static void DrawSewingKnot(Transform parent, Vector2 spot)
    {
        const float size = 17;
        var mark = WorkshopUi.Fill(parent, "Realized Needle Stitch", new Rect(spot.x - size / 2f, spot.y - size / 2f, size, size), SewingThread);
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
            ShowSewingFeedback("布边都合上了，留一处能把衣身翻出来的口。");
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
        int[] widths = { 1, 2, 3 };
        int needed = SewingOpeningNeed + 1;
        for (int i = 0; i < widths.Length; i++)
        {
            int index = i;
            var card = WorkshopUi.WorkbenchCard(art, "Opening Option " + i, new Rect(66 + i * 365, 128, 301, 393),
                LightSurface, WorkshopUi.Bronze);
            WorkshopUi.Picture(card, "Layered Cloth", new Rect(33, 28, 235, 152), WorkshopArt.CutPiece(i == 0 ? 2 : 0), SewingLining);
            for (int dot = 0; dot < widths[i]; dot++)
                WorkshopUi.Fill(card, "Open Space", new Rect(75 + dot * 55, 176, 45, 22), SewingSeamInk);
            WorkshopUi.Label(card, "Opening Width", new Rect(18, 217, 265, 49), $"开口宽 {widths[i]} 格", 28,
                SewingSeamInk, TextAnchor.MiddleCenter);
            SewingButton(card, "Daxue Opening " + i, new Rect(20, 293, 263, 70), "留" + SewingOpeningNames[i],
                () => SelectDaxueOpening(index), false);
        }
        Copy("留一个翻口", $"这件衣身至少要过 {needed} 格宽的口。口小了翻不出来，口大了要多补几针。");
    }

    private void SelectDaxueOpening(int index)
    {
        if (sewingPhase != SewingPhase.Opening || GameComplete || readOnly) return;
        sewingOpening = index;
        sewingPhase = SewingPhase.Turn;
        sewingTurned = 0;
        WorkshopAudioFeedback.Place();
        ShowSewingFeedback("翻口留在" + SewingOpeningNames[index] + "，试着把露出的布角拉出来。");
    }

    private void DrawSewingTurnBoard()
    {
        DrawSewingGarment(true);
        var dock = WorkshopUi.WorkbenchCard(art, "Daxue Turn Opening", new Rect(61, 315, 643, 235),
            LightSurface, WorkshopUi.Bronze);
        int next = SewingTurnOrders[sewingOpening][sewingTurned];
        var origin = new Vector2(61f, 315f);
        var destination = new Vector2(840f, 242f);
        WorkshopUi.Label(dock, "Opening Name", new Rect(15, 8, 606, 46), SewingOpeningNames[sewingOpening] + "翻口", 28, SewingSeamInk);
        for (int depth = 0; depth < SewingTurnNames.Length; depth++)
        {
            int part = SewingTurnOrders[sewingOpening][depth];
            Rect bounds = new Rect(18 + depth * 207, 76 + depth * 8, 193, 132 - depth * 8);
            if (depth < sewingTurned)
            {
                WorkshopUi.Label(dock, "Turned Section " + part, bounds, SewingTurnNames[part] + "已翻", 24, SewingSeamInk, TextAnchor.MiddleCenter);
                continue;
            }
            var handle = SewingButton(dock, "Daxue Turn Cloth " + part, bounds, SewingTurnNames[part], () => TurnDaxueCloth(part),
                part == next && ChallengeVariant == 0);
            var picture = WorkshopUi.Picture(handle.transform, "Exposed Cloth Fold", new Rect(48, 4, 94, 78),
                WorkshopArt.CutPiece(part == 2 ? 2 : part), SewingLining);
            handle.transform.Find("Label").GetComponent<RectTransform>().anchoredPosition = new Vector2(6, -45);
            BindSewingTurnDrag(handle, picture.sprite, part);
            if (part != next || ChallengeVariant != 0 || sewingTurned > 0 || sewingWarning) continue;
            ShowGesture(art, origin + bounds.center, destination, WorkGesture.Drag);
        }
        if (sewingWarning && sewingOpening < SewingOpeningNeed)
        {
            var jam = WorkshopUi.PixelCard(art, "Turn Jam", new Rect(336, 213, 302, 66), new Color32(252, 224, 204, 255), ErrorInk);
            WorkshopUi.Label(jam, "Too Narrow", new Rect(12, 1, 278, 64), "翻口太窄 · 卡住了", 27, ErrorInk, TextAnchor.MiddleCenter);
        }
        Copy("把夹衣翻到正面", "抓住露出来的布角向右拉，也可以点它。卡住就松开，核对翻口和布边。", sewingWarning);
        if (sewingOpening < SewingOpeningNeed)
            Act("Daxue Reopen", "改留更宽的翻口", ReopenDaxueSewing, true);
    }

    private void BindSewingTurnDrag(Button handle, Sprite sprite, int part)
    {
        var events = handle.gameObject.AddComponent<EventTrigger>();
        Image moving = null;
        Vector2 start = Vector2.zero;
        BindWinterEvent(events, EventTriggerType.BeginDrag, input =>
        {
            start = SewingLocal(art, (PointerEventData)input);
            moving = WorkshopUi.Picture(art, "Pulled Cloth", new Rect(start.x - 55, start.y - 48, 110, 96), sprite, sewingCloth);
        });
        BindWinterEvent(events, EventTriggerType.Drag, input =>
        {
            if (moving == null) return;
            Vector2 point = SewingLocal(art, (PointerEventData)input);
            moving.rectTransform.anchoredPosition = new Vector2(point.x - 55, -point.y + 48);
        });
        BindWinterEvent(events, EventTriggerType.EndDrag, input =>
        {
            if (moving != null) Destroy(moving.gameObject);
            Vector2 to = SewingLocal(art, (PointerEventData)input);
            if (to.x - start.x >= 70f) TurnDaxueCloth(part);
        });
    }

    private void TurnDaxueCloth(int part)
    {
        if (sewingPhase != SewingPhase.Turn || GameComplete || readOnly) return;
        if (sewingOpening < SewingOpeningNeed)
        {
            ShowSewingFeedback("衣身卡在翻口里。拆这一小段，换个宽些的口。", true);
            return;
        }
        int next = SewingTurnOrders[sewingOpening][sewingTurned];
        if (part != next)
        {
            ShowSewingFeedback("这块还被里面压着。先拉离翻口最近的" + SewingTurnNames[next] + "。", true);
            return;
        }

        sewingTurned++;
        if (sewingTurned == SewingTurnNames.Length) sewingPhase = SewingPhase.Close;
        sewingAnimate = true;
        WorkshopAudioFeedback.Collect();
        ShowSewingFeedback(sewingPhase == SewingPhase.Close ? "表层翻出来了，现在补好翻口。" : SewingTurnNames[part] + "翻到正面了。");
    }

    private void ReopenDaxueSewing()
    {
        if (sewingPhase != SewingPhase.Turn || sewingTurned > 0 || GameComplete || readOnly) return;
        sewingPhase = SewingPhase.Opening;
        sewingOpening = -1;
        WorkshopAudioFeedback.Fabric();
        ShowSewingFeedback("里布的翻口拆开了，别的缝没有松。");
    }

    private void DrawSewingGarment(bool turning)
    {
        Rect rect = turning ? new Rect(697, 94, 399, 453) : new Rect(355, 85, 444, 468);
        var garment = WorkshopUi.WorkbenchCard(art, "Daxue Garment", rect, LightSurface, WorkshopUi.Bronze);
        bool front = !turning || sewingTurned == SewingTurnNames.Length;
        if (front)
        {
            var sprite = WorkshopArt.Garment("4_02");
            var picture = WorkshopUi.Picture(garment, "Garment Right Side", new Rect(34, 21, rect.width - 68, rect.height - 75), sprite);
            if (sewingAnimate) AnimateSpringSummerSprite(picture, new Vector2(25, -20), .87f, .31f, 24f);
        }
        else
        {
            for (int i = 0; i < SewingTurnNames.Length; i++)
            {
                Color color = Array.IndexOf(SewingTurnOrders[sewingOpening], i) < sewingTurned ? sewingCloth : SewingLining;
                var part = WorkshopUi.Picture(garment, "Turning Piece " + i, new Rect(40 + (i % 2) * 116, 39 + i * 91, 224, 155),
                    WorkshopArt.CutPiece(i == 2 ? 2 : i), color);
                if (sewingAnimate && sewingTurned > 0 && SewingTurnOrders[sewingOpening][sewingTurned - 1] == i)
                    AnimateSpringSummerSprite(part, new Vector2(105, -39), .76f, .31f, 24f);
            }
        }
        string caption = GameComplete || readOnly ? "表里已合 · 可去试衣" : !front ? $"已翻正 {sewingTurned} / 3" : "正面朝外";
        WorkshopUi.Label(garment, "Garment State", new Rect(14, rect.height - 55, rect.width - 28, 47), caption,
            28, SewingSeamInk, TextAnchor.MiddleCenter);
    }

    private void DrawSewingCloseBoard()
    {
        DrawSewingGarment(false);
        int need = sewingOpening;
        var card = WorkshopUi.PixelCard(art, "Closing the Turning Opening", new Rect(815, 184, 295, 294),
            LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(card, "Closing Progress", new Rect(13, 17, 269, 70), $"翻口收针 {sewingClosed} / {need}", 29,
            SewingSeamInk, TextAnchor.MiddleCenter);
        for (int i = 0; i < need; i++)
        {
            Color ink = sewingClosed > i ? SewingSeamInk : WorkshopUi.Bronze;
            DrawCueStroke(card, new Vector2(79 + i * 68, 119), new Vector2(115 + i * 68, 119), 8, ink);
        }
        var close = SewingButton(card, "Daxue Close Stitch", new Rect(21, 188, 253, 70), "补一针收口", CloseDaxueOpening, true);
        VisualStep((RectTransform)close.transform);
        Copy("翻面后收口", "把翻口边折进里面，再补针。先前留的口越长，要收的针越多。");
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
