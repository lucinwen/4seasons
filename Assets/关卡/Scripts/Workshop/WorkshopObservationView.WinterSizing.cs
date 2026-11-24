using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private static readonly Color SizingTapeColor = new Color32(244, 205, 121, 255);
    private static readonly Color SizingClothColor = new Color32(116, 151, 150, 255);
    private static readonly Rect SizingPatternTarget = new Rect(151, 294, 315, 219);
    private static readonly Rect SizingTryOnCoat = new Rect(217, 326, 181, 194);
    private static readonly Vector2 SizingTryOnShoulder = new Vector2(277, 357);
    private const float SizingTapeY = 350f;

    private int winterCutSize;
    private bool winterSizingEnabled;
    private bool winterSizingMeasured;
    private bool winterSizingGrabbed;
    private Image winterSizingTapeBody;
    private RectTransform winterSizingTapeTip;
    private RectTransform winterSizingCue;
    private RectTransform winterSizingGoalCue;
    private RectTransform winterSizingGripCue;
    private readonly Image[] winterSizingTickMarks = new Image[17];
    private Text winterSizingStatus;
    private int winterSizingPatternChoice;
    private int winterSizingDisplayedPattern;
    private readonly RectTransform[] winterSizingPatternHighlights = new RectTransform[2];
    private RectTransform winterSizingPatternGuide;
    private RectTransform winterSizingTryOn;
    private RectTransform winterSizingTrySleeve;
    private Image winterSizingReach;
    private Text winterSizingReachLabel;
    private Coroutine winterSizingSleeveMotion;

    private bool SizingThickLayers => ChallengeVariant == 1;
    private Vector2 SizingTapeStart => new Vector2(SizingThickLayers ? 229f : 249f, SizingTapeY);
    private Vector2 SizingTapeEnd => new Vector2(SizingThickLayers ? 369f : 349f, SizingTapeY);
    private static string WinterCutSizeLabel(int size) => size == 2 ? "加宽袖版" : "常规袖版";

    private void ResetWinterSizing()
    {
        winterSizingEnabled = ChallengeVariant == 1 || (WorkshopProgress.SeasonalOrderEnabled && winterCutOrderCloth);
        winterCutSize = winterSizingEnabled ? 0 : 1;
        winterSizingPatternChoice = 0;
        winterSizingMeasured = false;
        winterSizingGrabbed = false;
    }

    private void DrawWinterSizing()
    {
        var worktop = IllustratedGameCard(art, "Xiaoxue Size Workbench", new Rect(38, 40, 1084, 570),
            GameArtSprite("WoodDesk"), WinterWorktop);
        WorkshopUi.PixelCard(worktop, "Fitting Paper", new Rect(23, 17, 461, 532), LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(art, "Xiaoxue Sizing Coat Type", new Rect(63, 65, 458, 38),
            SizingThickLayers ? "加穿厚内搭" : "薄内搭量身", 25, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        WorkshopUi.Picture(art, "Xiaoxue Measuring Model", new Rect(142, 116, 314, 443),
            Resources.Load<Sprite>("WorkshopFinish/FittingHero"));
        if (SizingThickLayers)
            WorkshopUi.Picture(art, "Xiaoxue Thick Inner Jacket", new Rect(242, 339, 121, 116),
                WorkshopArt.CutPiece(0), SizingClothColor);

        winterSizingCue = winterSizingGoalCue = winterSizingGripCue = null;
        if (winterSizingMeasured)
        {
            DrawWinterSizingTryOn();
            DrawWinterSizingPatterns();
            Copy("纸样试身", "把纸样拖到人模，或点一下试穿。");
        }
        else
        {
            DrawWinterSizingTapeExercise();
            Copy("小雪 · 量体", "把金色软尺拉过衣身，量出这单的宽度。\n也可以点尺头，再点右侧刻度。");
        }
        DrawWinterCutMaterial();
    }

    private void DrawWinterSizingTapeExercise()
    {
        Vector2 start = SizingTapeStart;
        Vector2 end = SizingTapeEnd;
        for (float x = start.x + 3f; x < end.x; x += 15f)
            DrawCueStroke(art, new Vector2(x, start.y), new Vector2(x + 7, start.y), 3f, WorkshopUi.Cream);

        DrawSizingTape(start.x + 29f);
        var target = WorkshopUi.Button(art, "Xiaoxue Tape Target", new Rect(end.x - 37, end.y - 39, 74, 78),
            "", () => TryFinishWinterSizing(end), Color.clear);
        DrawCueStroke(target.transform, new Vector2(36, 18), new Vector2(36, 60), 5f, WorkshopUi.Green);
        DrawCueStroke(target.transform, new Vector2(24, 18), new Vector2(36, 18), 4f, WorkshopUi.Cream);
        DrawCueStroke(target.transform, new Vector2(24, 60), new Vector2(36, 60), 4f, WorkshopUi.Cream);
        RegisterButton(target);
        winterSizingGoalCue = VisualStep((RectTransform)target.transform, SizingTapeColor);
        winterSizingGoalCue.gameObject.SetActive(winterSizingGrabbed);

        var grip = WorkshopUi.Button(art, "Xiaoxue Tape Grip", new Rect(start.x - 12, start.y - 38, 78, 76),
            "", GrabWinterSizingTape, Color.clear);
        RegisterButton(grip);
        winterSizingGripCue = VisualStep((RectTransform)grip.transform, SizingTapeColor);
        winterSizingGripCue.gameObject.SetActive(!winterSizingGrabbed);
        var gestures = grip.gameObject.AddComponent<EventTrigger>();
        BindWinterEvent(gestures, EventTriggerType.BeginDrag, data =>
        {
            GrabWinterSizingTape();
            MoveWinterSizingTape((PointerEventData)data);
        });
        BindWinterEvent(gestures, EventTriggerType.Drag, data => MoveWinterSizingTape((PointerEventData)data));
        BindWinterEvent(gestures, EventTriggerType.EndDrag, data =>
        {
            var pointer = (PointerEventData)data;
            if (TryReadWinterSizingPointer(pointer, out Vector2 point)) TryFinishWinterSizing(point);
        });

        var miniature = WorkshopUi.PixelCard(art, "Measurement Legend", new Rect(573, 168, 468, 230),
            LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(miniature, "Measure this customer", new Rect(22, 12, 424, 53),
            SizingThickLayers ? "厚内搭 · 要留活动余量" : "量完这身，再挑纸样", 28, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        WorkshopUi.Picture(miniature, "Finished Jacket Preview", new Rect(142, 69, 184, 140), WorkshopArt.Garment("4_02"));
        winterSizingStatus = WorkshopUi.Label(art, "Xiaoxue Tape Feedback", new Rect(558, 449, 493, 89),
            winterSizingGrabbed ? "松手落在右边刻度" : "从金色尺头开始", 26, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        if (!winterSizingGrabbed) winterSizingCue = ShowGesture(art, start + new Vector2(30, 1), end, WorkGesture.Drag);
        preferredAction = winterSizingGrabbed ? target : grip;
    }

    private void DrawSizingTape(float endX)
    {
        Vector2 start = SizingTapeStart;
        winterSizingTapeBody = WorkshopUi.Fill(art, "Xiaoxue Soft Tape", new Rect(start.x, start.y - 12f, 27f, 24f), SizingTapeColor);
        WorkshopUi.ApplyPixelShape(winterSizingTapeBody);
        for (int i = 0; i < winterSizingTickMarks.Length; i++)
            winterSizingTickMarks[i] = WorkshopUi.Fill(art, "Tape Tick " + i,
                new Rect(start.x + 8f + i * 11f, start.y - 10f, 3f, i % 3 == 0 ? 11f : 6f), WorkshopUi.Clay);
        winterSizingTapeTip = WorkshopUi.PixelCard(art, "Xiaoxue Golden Tape End", new Rect(endX - 21f, start.y - 21f, 42f, 42f),
            SizingTapeColor, WorkshopUi.Ink);
        WorkshopUi.Fill(winterSizingTapeTip, "Raised Copper Grip", new Rect(15, 9, 12, 24), WorkshopUi.Clay);
        PaintWinterSizingTape(endX);
    }

    private void PaintWinterSizingTape(float endX)
    {
        Vector2 start = SizingTapeStart;
        float length = Mathf.Clamp(endX - start.x, 24f, SizingTapeEnd.x - start.x + 45f);
        winterSizingTapeBody.rectTransform.sizeDelta = new Vector2(length, 24f);
        winterSizingTapeTip.anchoredPosition = new Vector2(start.x + length - 21f, 21f - start.y);
        for (int i = 0; i < winterSizingTickMarks.Length; i++)
            winterSizingTickMarks[i].gameObject.SetActive(9f + i * 11f < length);
        bool near = Mathf.Abs(start.x + length - SizingTapeEnd.x) <= 38f;
        winterSizingTapeBody.color = near ? new Color32(169, 208, 145, 255) : SizingTapeColor;
    }

    private void GrabWinterSizingTape()
    {
        if (winterSizingMeasured) return;
        if (!winterSizingGrabbed) WorkshopAudioFeedback.Click();
        winterSizingGrabbed = true;
        winterSizingGripCue.gameObject.SetActive(false);
        winterSizingGoalCue.gameObject.SetActive(true);
        if (winterSizingCue != null) winterSizingCue.gameObject.SetActive(false);
        winterSizingStatus.text = "拉到右侧刻度，再松手";
        winterSizingStatus.color = WorkshopUi.Ink;
    }

    private bool TryReadWinterSizingPointer(PointerEventData pointer, out Vector2 point)
    {
        bool resolved = RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position,
            pointer.pressEventCamera, out var local);
        point = new Vector2(local.x, -local.y);
        return resolved;
    }

    private void MoveWinterSizingTape(PointerEventData pointer)
    {
        if (winterSizingGrabbed && TryReadWinterSizingPointer(pointer, out Vector2 point)) PaintWinterSizingTape(point.x);
    }

    private void TryFinishWinterSizing(Vector2 point)
    {
        if (winterSizingMeasured) return;
        if (!winterSizingGrabbed)
        {
            winterSizingStatus.text = "先抓住金色尺头";
            winterSizingGripCue.gameObject.SetActive(true);
            WorkshopAudioFeedback.Notice();
            return;
        }

        Vector2 target = SizingTapeEnd;
        if (Mathf.Abs(point.x - target.x) > 38f || Mathf.Abs(point.y - target.y) > 44f)
        {
            winterSizingStatus.text = "让尺头碰到右侧刻度，变绿就能松手";
            winterSizingStatus.color = ErrorInk;
            PaintWinterSizingTape(SizingTapeStart.x + 29f);
            WorkshopAudioFeedback.Notice();
            return;
        }

        winterSizingMeasured = true;
        WorkshopAudioFeedback.Place();
        Render();
    }

    private void DrawWinterSizingPatterns()
    {
        var fabric = GameArtSprite("CuttingCloth");
        Vector2 model = SizingPatternTarget.center;
        var target = WorkshopUi.Rect(art, "Xiaoxue Paper Pattern Target", SizingPatternTarget);
        var dropCue = VisualStep(target);
        dropCue.gameObject.SetActive(false);
        winterSizingPatternGuide = WorkshopUi.Rect(art, "Xiaoxue Pattern Tutorial", new Rect());
        if (winterSizingPatternChoice == 0)
            ShowGesture(winterSizingPatternGuide, new Vector2(673, 244), model, WorkGesture.Drag);
        var ghost = WorkshopUi.Picture(art, "Xiaoxue Paper Pattern in Hand", new Rect(0, 0, 120, 76),
            WorkshopArt.CutPiece(2), SizingTapeColor);
        ghost.gameObject.SetActive(false);

        for (int i = 0; i < 2; i++)
        {
            int size = i + 1;
            float x = 559f + i * 256f;
            Rect cardBounds = new Rect(x, 146, 228, 364);
            var card = WorkshopUi.PixelCard(art, "Xiaoxue Sleeve Sample " + size, cardBounds,
                LightSurface, WorkshopUi.Bronze);
            WorkshopUi.Label(card, "Sleeve Pattern Name", new Rect(10, 10, 208, 47),
                WinterCutSizeLabel(size), 24, WorkshopUi.Ink, TextAnchor.MiddleCenter);
            WorkshopUi.Picture(card, "Xiaoxue Sleeve Outline", new Rect(size == 2 ? 16 : 29, 63, size == 2 ? 196 : 170, 91),
                WorkshopArt.CutPiece(2), winterCutClothBase, true);
            Vector2Int[] tiles = size == 2 ? WideWinterSleeve : WinterPatternShapes[2];
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 3; col++)
                    WorkshopUi.Fill(card, "Preview Fabric Cell", new Rect(47 + col * 45, 172 + row * 43, 41, 40), InsetPaper);
            for (int tile = 0; tile < tiles.Length; tile++)
            {
                Vector2Int offset = tiles[tile];
                GameArtSurface(card, "Sleeve Shape Cell", new Rect(47 + offset.x * 45, 172 + offset.y * 43, 41, 40),
                    fabric, size == 2 && offset == new Vector2Int(2, 0) ? SizingClothColor : winterCutClothBase);
            }
            WorkshopUi.Label(card, "Pattern Trade-off", new Rect(12, 262, 204, 39),
                $"{tiles.Length} 格布", 22, WorkshopUi.Ink, TextAnchor.MiddleCenter);
            var confirm = WinterControl(card, "Xiaoxue Paper Pattern " + size, new Rect(16, 307, 196, 43),
                "按这版裁", () => ChooseWinterSizing(size), size == winterSizingPatternChoice, textSize: 21);
            var hit = WorkshopUi.Button(card, "Xiaoxue Sample Touch " + size, new Rect(6, 7, 216, 296),
                "", () => TryOnWinterSizingPattern(size), Color.clear);
            RegisterButton(hit);
            winterSizingPatternHighlights[i] = VisualStep((RectTransform)hit.transform);
            var events = hit.gameObject.AddComponent<EventTrigger>();
            BindWinterEvent(events, EventTriggerType.PointerEnter, _ => PaintWinterSizingPattern(size));
            BindWinterEvent(events, EventTriggerType.PointerExit, _ =>
            {
                if (!ghost.gameObject.activeSelf) PaintWinterSizingPattern(winterSizingPatternChoice);
            });
            BindWinterEvent(events, EventTriggerType.BeginDrag, data =>
            {
                MoveWinterSizingPatternGhost(size, (PointerEventData)data, ghost);
                winterSizingPatternGuide.gameObject.SetActive(false);
                dropCue.gameObject.SetActive(true);
                WorkshopAudioFeedback.Fabric();
            });
            BindWinterEvent(events, EventTriggerType.Drag, data => MoveWinterSizingPatternGhost(size, (PointerEventData)data, ghost));
            BindWinterEvent(events, EventTriggerType.EndDrag, data =>
            {
                ghost.gameObject.SetActive(false);
                dropCue.gameObject.SetActive(false);
                if (TryReadWinterSizingPointer((PointerEventData)data, out var point) && SizingPatternTarget.Contains(point))
                    TryOnWinterSizingPattern(size);
                else
                {
                    winterSizingPatternGuide.gameObject.SetActive(winterSizingPatternChoice == 0);
                    PaintWinterSizingPattern(winterSizingPatternChoice);
                }
            });
            if (size == winterSizingPatternChoice) preferredAction = confirm;
            else if (preferredAction == null) preferredAction = hit;
        }
        ghost.transform.SetAsLastSibling();
        winterSizingDisplayedPattern = -1;
        PaintWinterSizingPattern(winterSizingPatternChoice, false);
    }

    private void DrawWinterSizingTryOn()
    {
        winterSizingTryOn = WorkshopUi.Rect(art, "Xiaoxue Try-On Paper Coat", new Rect());
        var garment = WorkshopArt.Garment("4_02");
        DrawWinterSizingCoatPart(winterSizingTryOn, "Collar", garment, new Rect(0, 0, 1, .13f), Vector2.zero);
        DrawWinterSizingCoatPart(winterSizingTryOn, "Coat and Right Sleeve", garment, new Rect(.32f, .13f, .68f, .50f), Vector2.zero);
        DrawWinterSizingCoatPart(winterSizingTryOn, "Hem", garment, new Rect(0, .63f, 1, .37f), Vector2.zero);
        winterSizingTrySleeve = WorkshopUi.Rect(winterSizingTryOn, "Xiaoxue Moving Sleeve",
            new Rect(SizingTryOnShoulder.x, SizingTryOnShoulder.y, 1, 1));
        DrawWinterSizingCoatPart(winterSizingTrySleeve, "Raised Sleeve", garment,
            new Rect(0, .13f, .32f, .50f), -SizingTryOnShoulder);
        var reach = WorkshopUi.PixelCard(winterSizingTryOn, "Xiaoxue Sleeve Reach Gauge", new Rect(86, 472, 109, 66),
            LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Fill(reach, "Reach Scale", new Rect(11, 41, 87, 10), InsetPaper);
        winterSizingReach = WorkshopUi.Fill(reach, "Sleeve Lift Range", new Rect(11, 41, 87, 10), WorkshopUi.Green);
        float goal = SizingThickLayers ? 66 : 48;
        WorkshopUi.Fill(reach, "Minimum Lift Mark", new Rect(11 + goal, 36, 4, 20), WorkshopUi.Clay);
        winterSizingReachLabel = WorkshopUi.Label(reach, "Reach Result", new Rect(4, 6, 101, 28), "",
            20, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        DrawWinterMark(art, new Vector2(430, 139), true);
    }

    private static void DrawWinterSizingCoatPart(Transform parent, string name, Sprite garment, Rect crop, Vector2 origin)
    {
        var bounds = new Rect(SizingTryOnCoat.x + crop.x * SizingTryOnCoat.width + origin.x,
            SizingTryOnCoat.y + crop.y * SizingTryOnCoat.height + origin.y,
            crop.width * SizingTryOnCoat.width, crop.height * SizingTryOnCoat.height);
        DrawWorkbenchSpriteCrop(parent, name, bounds, garment, crop, Color.white);
    }

    private void MoveWinterSizingPatternGhost(int size, PointerEventData pointer, Image ghost)
    {
        if (!TryReadWinterSizingPointer(pointer, out var point)) return;
        ghost.gameObject.SetActive(true);
        float width = size == 2 ? 152f : 120f;
        ghost.rectTransform.sizeDelta = new Vector2(width, 76);
        ghost.rectTransform.anchoredPosition = new Vector2(point.x - width * .5f, 38 - point.y);
        PaintWinterSizingPattern(SizingPatternTarget.Contains(point) ? size : winterSizingPatternChoice);
    }

    private void TryOnWinterSizingPattern(int size)
    {
        winterSizingPatternChoice = size;
        winterSizingPatternGuide.gameObject.SetActive(false);
        PaintWinterSizingPattern(size);
        WorkshopAudioFeedback.Fabric();
    }

    private void PaintWinterSizingPattern(int size, bool animate = true)
    {
        for (int i = 0; i < winterSizingPatternHighlights.Length; i++)
            winterSizingPatternHighlights[i].gameObject.SetActive(i + 1 == size);
        winterSizingTryOn.gameObject.SetActive(size > 0);
        if (size == 0 || size == winterSizingDisplayedPattern) return;
        winterSizingDisplayedPattern = size;

        bool snug = size == 1 && SizingThickLayers;
        float reach = size == 2 ? 84f : snug ? 43f : 59f;
        winterSizingReach.rectTransform.sizeDelta = new Vector2(reach, 10);
        winterSizingReach.color = snug ? WorkshopUi.Clay : WorkshopUi.Green;
        winterSizingReachLabel.text = snug ? "偏紧" : "抬臂";
        winterSizingTrySleeve.localScale = new Vector3(size == 2 ? 1.12f : 1f, 1, 1);
        float angle = size == 2 ? -102f : snug ? -45f : -64f;
        if (winterSizingSleeveMotion != null) StopCoroutine(winterSizingSleeveMotion);
        if (animate)
            winterSizingSleeveMotion = StartCoroutine(AnimateWinterSizingSleeve(winterSizingTrySleeve, angle));
        else
            winterSizingTrySleeve.localEulerAngles = new Vector3(0, 0, angle);
    }

    private static IEnumerator AnimateWinterSizingSleeve(RectTransform sleeve, float angle)
    {
        float start = Mathf.DeltaAngle(0, sleeve.localEulerAngles.z);
        for (float t = 0; t < 1 && sleeve != null && sleeve.gameObject.activeInHierarchy; t += Time.unscaledDeltaTime / .24f)
        {
            sleeve.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(start, angle, Mathf.SmoothStep(0, 1, t)));
            yield return null;
        }
        if (sleeve != null) sleeve.localEulerAngles = new Vector3(0, 0, angle);
    }

    private void ChooseWinterSizing(int size)
    {
        if (!winterSizingMeasured || size < 1 || size > 2) return;
        winterSizingPatternChoice = size;
        winterCutSize = size;
        winterCutSelected = winterCutTurn = 0;
        winterCutTutorialUsed = false;
        winterCutHover = winterCutHintAnchor = winterCutTutorialAnchor = null;
        winterCutWarning = false;
        winterCutFeedback = WinterCutSizeLabel(size) + " · 按样排料，给里襟留下整布。";
        WorkshopAudioFeedback.Place();
        Render();
    }

    private void ReopenWinterSizing()
    {
        if (!winterSizingEnabled || WinterCutPlacedCount() != 0) return;
        winterCutSize = 0;
        Render();
    }
}
