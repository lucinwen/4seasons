using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private static readonly Color SizingTapeColor = new Color32(244, 205, 121, 255);
    private static readonly Color SizingTapeReady = new Color32(187, 222, 151, 255);
    private static readonly Color SizingClothColor = new Color32(116, 151, 150, 255);
    private static readonly Color SizingCloseupPaper = new Color32(229, 211, 178, 255);
    private static readonly Rect SizingTorsoBounds = new Rect(80, 93, 742, 432);
    private static readonly Rect SizingTorsoCrop = new Rect(.126f, .052f, .748f, .438f);
    private static readonly Rect SizingPatternTarget = new Rect(151, 294, 315, 219);
    private static readonly Rect SizingTryOnCoat = new Rect(217, 326, 181, 194);
    private static readonly Vector2 SizingTryOnShoulder = new Vector2(277, 357);
    private const float SizingTapeY = 354f;
    private const float SizingTapeInitialLength = 61f;
    private const float SizingTapeHeight = 39f;
    private const float SizingTapeTipSize = 64f;
    private const float SizingTapeTickSpacing = 18f;
    private const float SizingTapeSnapX = 41f;
    private const float SizingTapeSnapY = 55f;

    private int winterCutSize;
    private bool winterSizingEnabled;
    private bool winterSizingMeasured;
    private bool winterSizingGrabbed;
    private RectTransform winterSizingTape;
    private Image winterSizingTapeEdge;
    private Image winterSizingTapeBody;
    private RectTransform winterSizingTapeTip;
    private CanvasGroup winterSizingCue;
    private RectTransform winterSizingGoalCue;
    private RectTransform winterSizingGripCue;
    private RectTransform winterSizingTapeSnapped;
    private readonly Image[] winterSizingTickMarks = new Image[30];
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
    private Vector2 SizingTapeStart => new Vector2(SizingThickLayers ? 230f : 270f, SizingTapeY);
    private Vector2 SizingTapeEnd => new Vector2(SizingThickLayers ? 657f : 625f, SizingTapeY);
    private Vector2 SizingTapeGrip => SizingTapeStart + Vector2.right * SizingTapeInitialLength;
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
        var hero = Resources.Load<Sprite>("WorkshopFinish/FittingHero");
        winterSizingCue = null;
        if (winterSizingMeasured)
        {
            WorkshopUi.PixelCard(worktop, "Fitting Paper", new Rect(23, 17, 461, 532),
                LightSurface, WorkshopUi.Bronze);
            WorkshopUi.Label(art, "Xiaoxue Sizing Coat Type", new Rect(63, 65, 458, 38),
                SizingThickLayers ? "厚内搭 · 已量好" : "薄内搭 · 已量好", 25, WorkshopUi.Ink, TextAnchor.MiddleCenter);
            WorkshopUi.Picture(art, "Xiaoxue Measuring Model", new Rect(142, 116, 314, 443), hero);
            if (SizingThickLayers)
                WorkshopUi.Picture(art, "Xiaoxue Thick Inner Jacket", new Rect(242, 339, 121, 116),
                    WorkshopArt.CutPiece(0), SizingClothColor);
            DrawWinterSizingTryOn();
            DrawWinterSizingPatterns();
            Copy("纸样试身", "把纸样拖到人模，或点一下试穿。");
        }
        else
        {
            DrawWinterSizingCloseup(hero);
            DrawWinterSizingTapeExercise();
            Copy("小雪 · 量体", "拉金色尺头到右侧刻度。\n也可以先点尺头，再点刻度。");
        }
        DrawWinterCutMaterial();
    }

    private void DrawWinterSizingCloseup(Sprite hero)
    {
        WorkshopUi.PixelCard(art, "Xiaoxue Measuring Close-Up Backing", new Rect(62, 62, 778, 525),
            SizingCloseupPaper, WorkshopUi.Bronze);
        var blouse = Resources.Load<Sprite>(SizingThickLayers
            ? "WorkshopFinish/Closeups/XiaoxueTorsoThick"
            : "WorkshopFinish/Closeups/XiaoxueTorsoThin");
        if (blouse != null)
            DrawWorkbenchSpriteCrop(art, "Xiaoxue Inner Layer Close-Up", SizingTorsoBounds, blouse, SizingTorsoCrop, Color.white);
        else
            DrawWorkbenchSpriteCrop(art, "Xiaoxue Inner Layer Close-Up", new Rect(231, 135, 440, 360), hero,
                new Rect(.20f, .46f, .60f, .37f), SizingThickLayers ? SizingClothColor : Color.white);

        var locator = WorkshopUi.PixelCard(art, "Xiaoxue Full Figure Locator", new Rect(866, 140, 223, 365),
            LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(locator, "Xiaoxue Sizing Coat Type", new Rect(8, 10, 207, 45),
            SizingThickLayers ? "厚内搭" : "薄内搭", 27, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        WorkshopUi.Picture(locator, "Xiaoxue Measuring Model", new Rect(25, 50, 173, 290), hero);
        if (SizingThickLayers)
            WorkshopUi.Picture(locator, "Padded Inner Layer Locator", new Rect(74, 190, 89, 89), blouse);
        var chest = new Rect(85, 188, 67, 60);
        WorkshopUi.Fill(locator, "Measured Area", chest, new Color32(238, 171, 71, 50));
        DrawCueStroke(locator, new Vector2(chest.xMin, chest.yMin), new Vector2(chest.xMax, chest.yMin), 4f, SizingTapeColor);
        DrawCueStroke(locator, new Vector2(chest.xMin, chest.yMax), new Vector2(chest.xMax, chest.yMax), 4f, SizingTapeColor);
        DrawCueStroke(locator, new Vector2(chest.xMin, chest.yMin), new Vector2(chest.xMin, chest.yMax), 4f, SizingTapeColor);
        DrawCueStroke(locator, new Vector2(chest.xMax, chest.yMin), new Vector2(chest.xMax, chest.yMax), 4f, SizingTapeColor);
    }

    private void DrawWinterSizingTapeExercise()
    {
        Vector2 at = SizingTapeGrip;
        Vector2 end = SizingTapeEnd;

        var target = WorkshopUi.Button(art, "Xiaoxue Tape Target",
            new Rect(end.x - SizingTapeSnapX, end.y - SizingTapeSnapY, SizingTapeSnapX * 2f, SizingTapeSnapY * 2f),
            "", () => TryFinishWinterSizing(end), Color.clear);
        DrawCueStroke(target.transform, new Vector2(41, 20), new Vector2(41, 90), 7f, WorkshopUi.Cream);
        DrawCueStroke(target.transform, new Vector2(23, 20), new Vector2(41, 20), 6f, WorkshopUi.Cream);
        DrawCueStroke(target.transform, new Vector2(23, 90), new Vector2(41, 90), 6f, WorkshopUi.Cream);
        RegisterButton(target);
        winterSizingGoalCue = VisualStep((RectTransform)target.transform, SizingTapeColor);
        winterSizingGoalCue.gameObject.SetActive(winterSizingGrabbed);

        DrawSizingTape();
        winterSizingTapeSnapped = DrawWinterMark(art, end + new Vector2(66f, 0), true);
        winterSizingTapeSnapped.name = "Xiaoxue Tape Docked";
        PaintWinterSizingTape(at);

        var grip = WorkshopUi.Button(art, "Xiaoxue Tape Grip", new Rect(at.x - 49, at.y - 49, 98, 98),
            "", GrabWinterSizingTape, Color.clear);
        RegisterButton(grip);
        winterSizingGripCue = VisualStep((RectTransform)grip.transform, SizingTapeColor);
        winterSizingGripCue.gameObject.SetActive(!winterSizingGrabbed);
        var gestures = grip.gameObject.AddComponent<EventTrigger>();
        BindWinterEvent(gestures, EventTriggerType.BeginDrag, data =>
        {
            var pointer = (PointerEventData)data;
            pointer.eligibleForClick = false;
            GrabWinterSizingTape();
            MoveWinterSizingTape(pointer);
        });
        BindWinterEvent(gestures, EventTriggerType.Drag, data => MoveWinterSizingTape((PointerEventData)data));
        BindWinterEvent(gestures, EventTriggerType.EndDrag, data =>
        {
            var pointer = (PointerEventData)data;
            if (TryReadWinterSizingPointer(pointer, out Vector2 point)) TryFinishWinterSizing(point);
            else ResetWinterSizingTape();
        });

        var feedback = WorkshopUi.PixelCard(art, "Xiaoxue Measuring Feedback Slip", new Rect(102, 525, 698, 45),
            LightSurface, WorkshopUi.Bronze);
        winterSizingStatus = WorkshopUi.Label(feedback, "Xiaoxue Tape Feedback", new Rect(8, 0, 682, 45),
            winterSizingGrabbed ? "拉到右侧刻度，再松手" : "拉住金色尺头", 26, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        winterSizingCue = ShowGesture(art, at, end, WorkGesture.Drag).GetComponent<CanvasGroup>();
        winterSizingCue.alpha = winterSizingGrabbed ? 0f : 1f;
        preferredAction = winterSizingGrabbed ? target : grip;
    }

    private void DrawSizingTape()
    {
        Vector2 start = SizingTapeStart;
        winterSizingTape = WorkshopUi.Rect(art, "Xiaoxue Measuring Tape Root", new Rect(start.x, start.y, 0f, 0f));
        winterSizingTapeEdge = WorkshopUi.Fill(winterSizingTape, "Soft Tape Outline",
            new Rect(-3f, -SizingTapeHeight * .5f - 3f, SizingTapeInitialLength + 6f, SizingTapeHeight + 6f), GuideShadow);
        WorkshopUi.ApplyPixelShape(winterSizingTapeEdge);
        winterSizingTapeBody = WorkshopUi.Fill(winterSizingTape, "Xiaoxue Soft Tape",
            new Rect(0f, -SizingTapeHeight * .5f, SizingTapeInitialLength, SizingTapeHeight), SizingTapeColor);
        WorkshopUi.ApplyPixelShape(winterSizingTapeBody);
        for (int i = 0; i < winterSizingTickMarks.Length; i++)
            winterSizingTickMarks[i] = WorkshopUi.Fill(winterSizingTape, "Tape Tick " + i,
                new Rect(10f + i * SizingTapeTickSpacing, -SizingTapeHeight * .5f + 4f, 3f, i % 3 == 0 ? 20f : 10f),
                WorkshopUi.Clay);
        float halfTip = SizingTapeTipSize * .5f;
        winterSizingTapeTip = WorkshopUi.PixelCard(winterSizingTape, "Xiaoxue Golden Tape End",
            new Rect(SizingTapeInitialLength - halfTip, -halfTip, SizingTapeTipSize, SizingTapeTipSize),
            SizingTapeColor, WorkshopUi.Ink);
        WorkshopUi.Fill(winterSizingTapeTip, "Copper Grip Inset", new Rect(11, 11, 42, 42), WorkshopUi.Clay);
        for (int i = 0; i < 3; i++)
            WorkshopUi.Fill(winterSizingTapeTip, "Grip Rib " + i, new Rect(20 + i * 10, 19, 4, 26), SizingTapeColor);
    }

    private bool WinterSizingTapeAtGoal(Vector2 point)
        => Mathf.Abs(point.x - SizingTapeEnd.x) <= SizingTapeSnapX && Mathf.Abs(point.y - SizingTapeEnd.y) <= SizingTapeSnapY;

    private void PaintWinterSizingTape(Vector2 point)
    {
        Vector2 start = SizingTapeStart;
        bool near = winterSizingGrabbed && WinterSizingTapeAtGoal(point);
        if (near) point = SizingTapeEnd;
        Vector2 extension = new Vector2(
            Mathf.Clamp(point.x - start.x, SizingTapeInitialLength, SizingTapeEnd.x - start.x + 48f),
            Mathf.Clamp(point.y - start.y, -48f, 48f));
        float length = extension.magnitude;
        winterSizingTape.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(extension.y, extension.x) * Mathf.Rad2Deg);
        winterSizingTapeEdge.rectTransform.sizeDelta = new Vector2(length + 6f, SizingTapeHeight + 6f);
        winterSizingTapeBody.rectTransform.sizeDelta = new Vector2(length, SizingTapeHeight);
        winterSizingTapeTip.anchoredPosition = new Vector2(length - SizingTapeTipSize * .5f, SizingTapeTipSize * .5f);
        for (int i = 0; i < winterSizingTickMarks.Length; i++)
            winterSizingTickMarks[i].gameObject.SetActive(12f + i * SizingTapeTickSpacing < length);
        winterSizingTapeBody.color = near ? SizingTapeReady : SizingTapeColor;
        winterSizingTapeSnapped.gameObject.SetActive(near);
    }

    private void GrabWinterSizingTape()
    {
        if (winterSizingMeasured) return;
        if (!winterSizingGrabbed) WorkshopAudioFeedback.Click();
        winterSizingGrabbed = true;
        winterSizingGripCue.gameObject.SetActive(false);
        winterSizingGoalCue.gameObject.SetActive(true);
        winterSizingCue.alpha = 0f;
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
        if (!winterSizingGrabbed || !TryReadWinterSizingPointer(pointer, out Vector2 point)) return;
        PaintWinterSizingTape(point);
        bool near = WinterSizingTapeAtGoal(point);
        winterSizingStatus.text = near ? "松手量好" : "拉到右侧刻度，再松手";
        winterSizingStatus.color = near ? WorkshopUi.Green : WorkshopUi.Ink;
    }

    private void ResetWinterSizingTape()
    {
        winterSizingGrabbed = false;
        winterSizingGoalCue.gameObject.SetActive(false);
        winterSizingGripCue.gameObject.SetActive(true);
        winterSizingCue.alpha = 1f;
        PaintWinterSizingTape(SizingTapeGrip);
        winterSizingStatus.text = "把尺头拉到右侧刻度";
        winterSizingStatus.color = ErrorInk;
        WorkshopAudioFeedback.Notice();
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

        if (!WinterSizingTapeAtGoal(point))
        {
            ResetWinterSizingTape();
            return;
        }

        winterSizingMeasured = true;
        winterSizingGrabbed = false;
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
