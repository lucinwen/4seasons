using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private const int OfferingColumns = 5;
    private const int OfferingSeatCount = 10;
    private const int SilkOffering = 3;
    private static readonly string[] ExhibitionNames = { "高酒坛", "果篮", "糕点", "待验丝轴", "桑叶" };
    private static readonly int[] ExhibitionHeights = { 3, 2, 1, 1, 1 };
    private static readonly int[] ExhibitionOpenFront = { 0b00101, 0b10100 };
    private static readonly int[] ExhibitionRisers = { (1 << 6) | (1 << 8), 1 << 7 };
    private readonly int[] exhibition = new int[OfferingSeatCount];
    private readonly Image[] exhibitionObjects = new Image[OfferingSeatCount];
    private readonly Image[] exhibitionEdges = new Image[OfferingSeatCount];
    private readonly Text[] exhibitionSeatLabels = new Text[OfferingSeatCount];
    private readonly Text[] exhibitionSupplyLabels = new Text[ExhibitionNames.Length];
    private readonly Button[] inspectionSegments = new Button[3];
    private readonly RectTransform[] exhibitionSeatCues = new RectTransform[OfferingSeatCount];
    private readonly RectTransform[] inspectionCues = new RectTransform[3];
    private readonly Image[] exhibitionSampleThreads = new Image[3];
    private readonly Image[] exhibitionSampleKnots = new Image[3];
    private readonly Image[] exhibitionSampleBases = new Image[3];
    private readonly Image[] exhibitionDropGhosts = new Image[OfferingSeatCount];
    private Sprite[] exhibitionSprites;
    private int exhibitionHeld = -1;
    private bool exhibitionSide;
    private bool exhibitionLamp;
    private bool exhibitionKnotFound;
    private int lastEvenSilkSegment = -1;
    private string exhibitionSilk;
    private int exhibitionKnot;
    private Image exhibitionGhost;
    private Text exhibitionHeading;
    private Text exhibitionGuide;
    private Button exhibitionSubmit;
    private Button exhibitionLampButton;
    private Text exhibitionStatus;
    private Text exhibitionLight;
    private Image exhibitionSunbeam;
    private Button exhibitionTurn;
    private RectTransform exhibitionSilkCue;
    private RectTransform exhibitionTurnCue;
    private RectTransform exhibitionLampCue;
    private RectTransform exhibitionSubmitCue;
    private RectTransform exhibitionGesture;
    private int exhibitionGestureKey;
    private const int NoExhibitionGesture = -1;
    private static readonly Rect ExhibitionTurnBounds = new Rect(912, 24, 92, 53);
    private static readonly Rect ExhibitionLampBounds = new Rect(1015, 24, 92, 53);
    private static readonly Rect ExhibitionSubmitBounds = new Rect(901, 532, 206, 100);

    private const int WeavingRows = 6;
    private const int WeavingColumns = 6;
    private static readonly int[] HeddleGroups = { 0b101010, 0b010101, 0b100001 };
    private static readonly string[] HeddleNames = { "奇数经线", "偶数经线", "两侧经线" };
    private static readonly int[][] WeftColors = { new[] { 1, 3, 2, 1, 3, 2 }, new[] { 2, 3, 1, 2, 3, 1 } };
    private static readonly int[][] WeftPatterns = { new[] { 1, 2, 5, 1, 2, 6 }, new[] { 5, 1, 6, 2, 5, 1 } };
    private static readonly string[] WeftNames = { "赭梭", "青梭" };
    private static readonly Color32[] WeftInks = { new Color32(167, 83, 73, 255), new Color32(66, 104, 133, 255) };
    private readonly Image[,] weavingCells = new Image[WeavingRows, WeavingColumns];
    private readonly int[] wovenColors = new int[WeavingRows];
    private readonly int[] wovenPatterns = new int[WeavingRows];
    private readonly Text[] weftHints = new Text[2];
    private readonly Image[,] weftHintPegs = new Image[2, WeavingColumns];
    private readonly Image[,] weftColorChips = new Image[2, 2];
    private readonly RectTransform[,] weftDirections = new RectTransform[2, 2];
    private readonly Text[] shuttleCards = new Text[2];
    private readonly Image[] shuttlePictures = new Image[2];
    private readonly RectTransform[] shuttleCues = new RectTransform[2];
    private readonly RectTransform[,] shuttleSends = new RectTransform[2, 2];
    private readonly RectTransform[,] shuttleEmptySends = new RectTransform[2, 2];
    private readonly RectTransform[] currentWeavingDirection = new RectTransform[2];
    private readonly Text[] heddleCards = new Text[3];
    private readonly Image[] heddlePictures = new Image[3];
    private readonly RectTransform[] heddleCues = new RectTransform[3];
    private readonly RectTransform[] weavingRowCues = new RectTransform[WeavingRows];
    private readonly Image[] liveHeddlePegs = new Image[WeavingColumns];
    private readonly Image[] alignedPegMarks = new Image[WeavingColumns];
    private readonly Image[] mismatchedPegMarks = new Image[WeavingColumns];
    private readonly Image[] emptyMoveMarks = new Image[2];
    private Button redEmptyShuttle;
    private Button blueEmptyShuttle;
    private Button undoEmptyShuttle;
    private Button restartWeavingSegment;
    private RectTransform weavingGesture;
    private int weavingGestureKey;
    private int wovenCount;
    private int shuttlesRight;
    private int heddles;
    private int emptyRuns;
    private int weavingCheckpointSides;
    private int weavingCheckpointHeddles;
    private int weavingCheckpointRuns;
    private int lastEmptyShuttle = -1;
    private Text weavingGuide;
    private Text weavingStatus;
    private Image movingShuttle;
    private int animatingWeaveRow = -1;
    private int revealedWeaveThreads;
    private bool weavingSilkReady;
    private bool weavingWindowWet;
    private string weavingPrepHint;

    private void ResetAutumnChallenge()
    {
        switch (observation)
        {
            case WorkshopObservation.QiufenOffering:
                ResetExhibition();
                exhibitionSide = exhibitionLamp = exhibitionKnotFound = false;
                lastEvenSilkSegment = -1;
                string reeled = WorkshopUpstreamWork(WorkshopObservation.MangzhongCocoon, 0)?.sample;
                var bands = reeled?.Split('/');
                exhibitionSilk = bands != null && bands.Length == 3 && bands[1].Contains("4") ? reeled : "111/949/111";
                exhibitionKnot = exhibitionSilk.Split('/')[1].IndexOf('4');
                break;
            case WorkshopObservation.HanluLoom:
                wovenCount = emptyRuns = heddles = 0;
                lastEmptyShuttle = -1;
                animatingWeaveRow = -1;
                weavingSilkReady = false;
                weavingWindowWet = ChallengeVariant == 0;
                weavingPrepHint = null;
                shuttlesRight = ChallengeVariant == 0 ? 2 : 1;
                SaveWeavingCheckpoint();
                break;
        }
    }

    private void ResetExhibition()
    {
        for (int i = 0; i < exhibition.Length; i++) exhibition[i] = -1;
        exhibition[7] = 0;
        exhibitionHeld = -1;
    }

    private void DrawOfferingWorkshop()
    {
        exhibitionGesture = null;
        exhibitionGestureKey = NoExhibitionGesture;
        Texture("Offering Table and Tools", "WorkshopFinish/AutumnOfferingTable", ArtBounds);
        var props = AutumnItemIcons.Load();
        var silk = SpringSummerArt("SilkBobbin");
        exhibitionSprites = new[] { WorkshopObservationArt.Current?.OfferingWine, props?.banana, props?.cake,
            silk ?? Resources.Load<Sprite>("WorkshopFinish/RawSilk"), WorkshopObservationArt.Current?.Leaf };
        var trayArt = GameArtSprite("OfferingTray");
        var riserArt = GameArtSprite("WoodenRiser");

        var header = WorkshopUi.PixelCard(art, "Exhibition View", new Rect(53, 24, 848, 53), WorkshopUi.Ink, WorkshopUi.Bronze);
        exhibitionLight = WorkshopUi.Label(header, "Exhibition Light", new Rect(12, 0, 824, 53), "", 27,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);

        for (int seat = 0; seat < exhibition.Length; seat++)
        {
            int i = seat;
            Rect bounds = ExhibitionSeatBounds(seat);
            bool open = CanUseExhibitionSeat(seat);
            var card = IllustratedGameCard(art, "Offering Display Seat " + seat, bounds, trayArt,
                open ? LightSurface : InsetPaper, open ? Color.white : new Color32(161, 143, 118, 193));
            exhibitionEdges[seat] = WorkshopUi.Fill(card, "Seat Status", new Rect(8, 5, bounds.width - 16, 6), WorkshopUi.Line);
            bool lifted = IsRaisedExhibitionSeat(seat);
            if (lifted) GameArtSurface(card, "Wooden Riser", new Rect(37, 60, 130, 47), riserArt, new Color32(150, 104, 64, 255));
            exhibitionObjects[seat] = WorkshopUi.Picture(card, "Displayed Offering", new Rect(29, lifted ? 3 : 12, 140, lifted ? 79 : 93), null);
            if (trayArt != null)
                WorkshopUi.Fill(card, "Caption Paper", new Rect(16, 102, 173, 26), new Color32(251, 234, 196, 218));
            exhibitionSeatLabels[seat] = WorkshopUi.Label(card, "Seat Caption", new Rect(8, 96, 189, 37), "", 21,
                WorkshopUi.Ink, TextAnchor.MiddleCenter);
            if (GameComplete || !open) continue;

            var target = WorkshopUi.Button(card, "Place", new Rect(1, 1, bounds.width - 2, bounds.height - 2), "",
                () => ClickExhibitionSeat(i), Color.clear);
            RegisterButton(target);
            BindOfferingDrag(target.gameObject, () => exhibition[i]);
            exhibitionDropGhosts[i] = WorkshopUi.Picture(card, "Suggested Drop Ghost", new Rect(47, 25, 108, 62), null,
                new Color(1, 1, 1, .45f));
            exhibitionSeatCues[i] = VisualStep(card);
            exhibitionSeatCues[i].gameObject.SetActive(false);
        }

        exhibitionSunbeam = WorkshopUi.Picture(art, "Offering Sunbeam", new Rect(0, 101, 1, 112),
            GameArtSprite("OfferingSunbeam"), stretch: true);
        exhibitionSunbeam.raycastTarget = false;
        string silkCaption = WorkshopUpstreamWork(WorkshopObservation.MangzhongCocoon, 0) == null
            ? "工坊存丝 · 对照样" : "夏季丝样已入库 · 本班验丝";
        DrawAutumnSilkHandoff("Offering Silk Handoff", new Rect(53, 439, 1054, 31), silkCaption,
            GameComplete ? AcceptedExhibitionSilk() : null);

        if (GameComplete)
        {
            Copy("验收完成 · 挂上丝签", "光下看清了接头，这轴丝已登记。收进柜子，寒露开机时从这里领丝。");
            Act("Read Autumn Heritage", "看苏州织造资料", OpenAutumnHeritage, true);
            RefreshExhibition();
            return;
        }

        for (int segment = 0; segment < inspectionSegments.Length; segment++)
        {
            int index = segment;
            inspectionSegments[segment] = WorkshopUi.PixelButton(art, "Silk Inspection Segment " + segment,
                new Rect(53 + segment * 354, 477, 346, 47), "", () => InspectExhibitionSilk(index), false, 22);
            var segmentButton = inspectionSegments[segment];
            RegisterButton(segmentButton);
            var segmentTarget = (RectTransform)segmentButton.transform.parent;
            exhibitionSampleBases[segment] = segmentButton.targetGraphic as Image;
            exhibitionSampleThreads[segment] = WorkshopUi.Fill(segmentTarget, "Silk Strand", new Rect(39, 21, 266, 4), WorkshopUi.Bronze);
            exhibitionSampleKnots[segment] = WorkshopUi.Fill(segmentTarget, "Silk Knot", new Rect(162, 13, 20, 20), ErrorInk);
            exhibitionSampleKnots[segment].rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            inspectionCues[segment] = VisualStep(segmentTarget);
        }

        for (int item = 0; item < ExhibitionNames.Length; item++)
        {
            int i = item;
            var bounds = new Rect(53 + item * 212, 532, 205, 100);
            var tray = IllustratedGameCard(art, "Offering Supply " + item, bounds, trayArt, LightSurface);
            WorkshopUi.Picture(tray, "Supply Item", new Rect(47, 5, 110, 59), exhibitionSprites[item]);
            var fade = tray.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = item == SilkOffering ? 1f : .42f;
            if (trayArt != null)
                WorkshopUi.Fill(tray, "Caption Paper", new Rect(16, 66, 173, 26), new Color32(251, 234, 196, 218));
            exhibitionSupplyLabels[item] = WorkshopUi.Label(tray, "Supply Caption", new Rect(8, 61, 189, 36), "", 21,
                WorkshopUi.Ink, TextAnchor.MiddleCenter);
            var take = WorkshopUi.Button(tray, "Take", new Rect(1, 1, 203, 98), "", () => PickExhibitionItem(i), Color.clear);
            bool useful = item == SilkOffering || item == 0;
            take.interactable = useful;
            if (useful)
            {
                RegisterButton(take);
                BindOfferingDrag(take.gameObject, () => i);
            }
            if (item == SilkOffering) exhibitionSilkCue = VisualStep(tray);
        }

        exhibitionGhost = WorkshopUi.Picture(art, "Offering In Hand", new Rect(0, 0, 150, 104), null);
        exhibitionGhost.color = new Color(1f, 1f, 1f, 0.9f);
        Copy("先放丝轴", "秋分后天黑渐早，趁窗边有光验丝。");
        exhibitionHeading = infoPanel.Find("Instruction Heading").GetComponent<Text>();
        exhibitionGuide = infoPanel.Find("Instruction Body").GetComponent<Text>();
        exhibitionTurn = AutumnToolButton("Offering View Toggle", ExhibitionTurnBounds, () =>
        {
            exhibitionSide = !exhibitionSide;
            WorkshopAudioFeedback.Click();
            RefreshExhibition();
        });
        var turnIcon = (RectTransform)exhibitionTurn.transform.parent;
        DrawAutumnArrow(turnIcon, new Vector2(23, 26), new Vector2(67, 26), WorkshopUi.Ink);
        exhibitionTurnCue = VisualStep(turnIcon);
        exhibitionLampButton = AutumnToolButton("Offering Inspection Lamp", ExhibitionLampBounds, LightExhibitionLamp);
        var lampIcon = (RectTransform)exhibitionLampButton.transform.parent;
        WorkshopUi.Fill(lampIcon, "Lamp Flame", new Rect(39, 11, 14, 16), WorkshopUi.Bronze);
        WorkshopUi.Fill(lampIcon, "Lamp Bowl", new Rect(28, 27, 36, 7), WorkshopUi.Ink);
        WorkshopUi.Fill(lampIcon, "Lamp Stem", new Rect(43, 31, 6, 8), WorkshopUi.Ink);
        WorkshopUi.Fill(lampIcon, "Lamp Foot", new Rect(34, 39, 24, 4), WorkshopUi.Ink);
        DrawAutumnLine(lampIcon, new Vector2(26, 18), new Vector2(33, 21), WorkshopUi.Bronze, 3);
        DrawAutumnLine(lampIcon, new Vector2(66, 18), new Vector2(59, 21), WorkshopUi.Bronze, 3);
        exhibitionLampCue = VisualStep(lampIcon);
        exhibitionSubmit = WorkshopUi.PixelButton(art, "Submit Offering Display", ExhibitionSubmitBounds, "",
            SubmitExhibition, true, 26, WorkshopUi.Green);
        RegisterButton(exhibitionSubmit);
        var submitIcon = (RectTransform)exhibitionSubmit.transform.parent;
        WorkshopUi.Picture(submitIcon, "Accepted Silk Tag", new Rect(10, 19, 68, 62), GameArtSprite("SuccessSeal"));
        WorkshopUi.Label(submitIcon, "Accept Tag Caption", new Rect(76, 26, 120, 48), "挂验收签", 24, WorkshopUi.Cream, TextAnchor.MiddleCenter);
        exhibitionSubmitCue = VisualStep(submitIcon);
        exhibitionStatus = WorkshopUi.Label(infoPanel, "Offering Order Progress", new Rect(26, 338, 448, 96), "", 24,
            WorkshopUi.Green, TextAnchor.UpperLeft);
        RefreshExhibition();
    }

    private static Rect ExhibitionSeatBounds(int seat) =>
        new Rect(53 + seat % OfferingColumns * 212, seat < OfferingColumns ? 281 : 101, 205, 158);

    private Button AutumnToolButton(string name, Rect bounds, Action click)
    {
        var button = WorkshopUi.PixelButton(art, name, bounds, "", click, false, 23);
        RegisterButton(button);
        return button;
    }

    private bool CanUseExhibitionSeat(int seat) => seat >= OfferingColumns ||
        (ExhibitionOpenFront[ChallengeVariant] & (1 << seat)) != 0;

    private bool IsRaisedExhibitionSeat(int seat) => (ExhibitionRisers[ChallengeVariant] & (1 << seat)) != 0;

    private int WindowBlocker(int silkSeat)
    {
        if (silkSeat < OfferingColumns) return -1;
        int direction = ExhibitionLightDirection;
        int from = direction > 0 ? OfferingColumns : OfferingSeatCount - 1;
        for (int seat = from; seat != silkSeat; seat += direction)
        {
            int prop = exhibition[seat];
            if (prop >= 0 && ExhibitionHeights[prop] + (IsRaisedExhibitionSeat(seat) ? 1 : 0) >= 2) return seat;
        }
        return -1;
    }

    private int ExhibitionLightDirection => exhibitionLamp ? -1 : 1;

    private bool ExhibitionCanInspect(int silk, int blocker) =>
        exhibitionSide && silk >= OfferingColumns && blocker < 0 && (ChallengeVariant == 0 || exhibitionLamp);

    private void RefreshExhibition(string message = null)
    {
        int silk = Array.IndexOf(exhibition, SilkOffering);
        int blocker = WindowBlocker(silk);
        bool lit = ExhibitionCanInspect(silk, blocker);
        bool needsSpool = silk < OfferingColumns;
        bool needsSideView = !needsSpool && !exhibitionSide;
        bool needsLamp = !needsSpool && exhibitionSide && ChallengeVariant != 0 && !exhibitionLamp;
        bool needsSpace = !needsSpool && exhibitionSide && !needsLamp && blocker >= 0;
        int suggestedSeat = needsSpool ? Array.FindLastIndex(exhibition, item => item < 0)
            : needsSpace ? FirstEmptyFrontSeat() : -1;
        string window = exhibitionLamp ? "右侧验丝灯已亮" : ChallengeVariant == 0 ? "白昼 · 左侧窗光" : "傍晚 · 窗光已弱";
        exhibitionLight.text = exhibitionSide ? $"侧面 · {window} · {(lit ? "可以验丝" : "查看丝轴和光线")}"
            : $"正面 · {window} · 后排是验丝位";
        ShowExhibitionSunbeam(silk, blocker);

        for (int seat = 0; seat < exhibition.Length; seat++)
        {
            int item = exhibition[seat];
            bool raised = IsRaisedExhibitionSeat(seat);
            var picture = exhibitionObjects[seat];
            picture.sprite = item >= 0 ? exhibitionSprites[item] : null;
            picture.enabled = picture.sprite != null;
            bool warning = exhibitionSide && (seat == blocker || (seat == silk && !lit));
            bool featured = seat == silk && exhibitionSide && lit;
            exhibitionEdges[seat].color = warning ? ErrorInk : featured ? new Color32(212, 166, 65, 255) : item >= 0 && item == exhibitionHeld ? FocusInk : WorkshopUi.Line;
            picture.color = warning ? new Color32(155, 130, 115, 210) : Color.white;
            string location = seat < OfferingColumns ? "前排" : raised ? "后排高台" : "后排";
            string caption = CanUseExhibitionSeat(seat) ? location : "留白";
            bool source = needsSpace && seat == blocker;
            bool destination = item < 0 && ((needsSpool && seat >= OfferingColumns) || (needsSpace && seat == suggestedSeat));
            if (destination && seat == suggestedSeat) caption = needsSpool ? "放丝轴" : "放酒坛";
            if (item >= 0)
            {
                string cue = featured ? "见光" : location;
                if (warning) cue = seat == blocker ? "挡光" : "未照到";
                caption = ExhibitionNames[item] + " · " + cue;
            }
            exhibitionSeatLabels[seat].text = caption;
            if (!GameComplete && exhibitionSeatCues[seat] != null)
            {
                exhibitionSeatCues[seat].gameObject.SetActive(source || destination && seat == suggestedSeat);
                var drop = exhibitionDropGhosts[seat];
                drop.enabled = destination && seat == suggestedSeat;
                if (drop.enabled) drop.sprite = exhibitionSprites[needsSpool ? SilkOffering : 0];
            }
        }

        if (GameComplete) return;
        for (int item = 0; item < ExhibitionNames.Length; item++)
        {
            bool placed = Array.IndexOf(exhibition, item) >= 0;
            exhibitionSupplyLabels[item].text = item != SilkOffering && item != 0 ? ExhibitionNames[item] + " · 陈设"
                : ExhibitionNames[item] + (item == exhibitionHeld ? " · 已拿起" : placed ? " · 可挪动" : " · 点选");
        }
        exhibitionHeading.text = exhibitionKnotFound && lit ? "验收入库" : lit ? "找接头"
            : needsSpool ? "先放丝轴" : "给丝轴找光";
        exhibitionGuide.text = exhibitionKnotFound && lit ? "接头已标记，挂签留存。"
            : needsSpool ? "把丝轴放到后排的空盘。"
            : needsSideView ? "切到侧窗看光路。"
            : needsLamp ? "傍晚窗光变暗，点灯补光。"
            : needsSpace ? "酒坛挡光，把它挪到前排。"
            : "点带接头的丝段。";
        SetAutumnActionVisibility(exhibitionSubmit, lit && exhibitionKnotFound);
        SetAutumnActionVisibility(exhibitionLampButton, !needsSpool && exhibitionSide && !exhibitionLamp);
        if (!needsLamp && exhibitionLampButton.interactable)
            exhibitionLampButton.transform.parent.GetComponent<CanvasGroup>().alpha = .65f;
        SetAutumnActionVisibility(exhibitionTurn, !needsSpool);
        exhibitionSilkCue.gameObject.SetActive(needsSpool);
        exhibitionTurnCue.gameObject.SetActive(needsSideView);
        exhibitionLampCue.gameObject.SetActive(needsLamp);
        exhibitionSubmitCue.gameObject.SetActive(lit && exhibitionKnotFound);
        for (int i = 0; i < inspectionSegments.Length; i++)
        {
            bool hasKnot = i == exhibitionKnot % inspectionSegments.Length;
            SetAutumnActionVisibility(inspectionSegments[i], lit, !exhibitionKnotFound);
            inspectionCues[i].gameObject.SetActive(lit && (!exhibitionKnotFound || hasKnot));
            exhibitionSampleBases[i].color = exhibitionKnotFound && hasKnot ? new Color32(223, 232, 197, 255)
                : lastEvenSilkSegment == i ? InsetPaper : LightSurface;
            exhibitionSampleThreads[i].color = hasKnot ? WorkshopUi.Ink : WorkshopUi.Bronze;
            exhibitionSampleKnots[i].enabled = hasKnot;
        }
        exhibitionStatus.text = message ?? (exhibitionKnotFound && lit ? "已标记，可以挂签。" : needsLamp ? "窗暗了，点灯补光。" : "");

        if (needsSpool && suggestedSeat >= OfferingColumns)
        {
            var from = silk >= 0 ? ExhibitionSeatBounds(silk).center : new Vector2(791, 580);
            var destination = ExhibitionSeatBounds(suggestedSeat).center;
            bool held = exhibitionHeld == SilkOffering;
            SetExhibitionGesture(suggestedSeat + (held ? 10 : 0), held ? destination : from, destination, held ? WorkGesture.Tap : WorkGesture.Drag);
        }
        else if (needsSideView)
            SetExhibitionGesture(20, ExhibitionTurnBounds.center, ExhibitionTurnBounds.center, WorkGesture.Tap);
        else if (needsLamp)
            SetExhibitionGesture(21, ExhibitionLampBounds.center, ExhibitionLampBounds.center, WorkGesture.Tap);
        else if (needsSpace && suggestedSeat >= 0)
        {
            var destination = ExhibitionSeatBounds(suggestedSeat).center;
            bool held = exhibitionHeld == 0;
            SetExhibitionGesture(30 + blocker * OfferingSeatCount + suggestedSeat + (held ? 150 : 0),
                held ? destination : ExhibitionSeatBounds(blocker).center, destination, held ? WorkGesture.Tap : WorkGesture.Drag);
        }
        else if (lit && exhibitionKnotFound)
            SetExhibitionGesture(22, ExhibitionSubmitBounds.center, ExhibitionSubmitBounds.center, WorkGesture.Tap);
        else SetExhibitionGesture(NoExhibitionGesture, Vector2.zero, Vector2.zero, WorkGesture.Tap);
    }

    private int FirstEmptyFrontSeat()
    {
        for (int seat = 0; seat < OfferingColumns; seat++)
            if (CanUseExhibitionSeat(seat) && exhibition[seat] < 0) return seat;
        return -1;
    }

    private void SetExhibitionGesture(int key, Vector2 from, Vector2 to, WorkGesture gesture)
    {
        if (key == exhibitionGestureKey) return;
        if (exhibitionGesture != null)
        {
            exhibitionGesture.gameObject.SetActive(false);
            Destroy(exhibitionGesture.gameObject);
        }
        exhibitionGestureKey = key;
        exhibitionGesture = key == NoExhibitionGesture ? null : ShowGesture(art, from, to, gesture);
    }

    private static void SetAutumnActionVisibility(Button button, bool visible, bool available = true)
    {
        var root = button.transform.parent.gameObject;
        var group = root.GetComponent<CanvasGroup>();
        if (group == null) group = root.AddComponent<CanvasGroup>();
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        button.interactable = visible && available;
    }

    private static void DrawAutumnLine(RectTransform parent, Vector2 from, Vector2 to, Color color, float width)
    {
        var delta = to - from;
        var segment = WorkshopUi.Fill(parent, "Direction Stroke", new Rect(from.x, from.y, delta.magnitude, width), color);
        segment.rectTransform.pivot = new Vector2(0, .5f);
        segment.rectTransform.anchoredPosition = new Vector2(from.x, -from.y);
        segment.rectTransform.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private static void DrawAutumnArrow(RectTransform parent, Vector2 from, Vector2 to, Color color)
    {
        var tangent = (to - from).normalized;
        var cross = new Vector2(-tangent.y, tangent.x);
        DrawAutumnLine(parent, from, to, color, 5);
        DrawAutumnLine(parent, to, to - tangent * 13 + cross * 10, color, 5);
        DrawAutumnLine(parent, to, to - tangent * 13 - cross * 10, color, 5);
    }

    private static void DrawAutumnDottedArrow(RectTransform parent, float from, float to, float y, Color color)
    {
        for (int i = 0; i < 4; i++)
        {
            float start = Mathf.Lerp(from, to, i * .22f);
            float end = Mathf.Lerp(from, to, i * .22f + .10f);
            DrawAutumnLine(parent, new Vector2(start, y), new Vector2(end, y), color, 4);
        }
        float back = Mathf.Sign(to - from) * 10;
        DrawAutumnLine(parent, new Vector2(to - back, y - 8), new Vector2(to, y), color, 4);
        DrawAutumnLine(parent, new Vector2(to - back, y + 8), new Vector2(to, y), color, 4);
    }

    private void ShowExhibitionSunbeam(int silk, int blocker)
    {
        exhibitionSunbeam.enabled = exhibitionSide && exhibitionSunbeam.sprite != null && (ChallengeVariant == 0 || exhibitionLamp);
        if (!exhibitionSunbeam.enabled) return;

        int direction = ExhibitionLightDirection;
        float source = direction > 0 ? 52f : 1107f;
        int endSeat = blocker >= 0 ? blocker : silk;
        float end = endSeat >= OfferingColumns
            ? ExhibitionSeatBounds(endSeat).center.x + direction * (blocker >= 0 ? -27f : 43f)
            : direction > 0 ? 1107f : 52f;

        var beam = exhibitionSunbeam.rectTransform;
        beam.pivot = new Vector2(.5f, 1f);
        beam.anchoredPosition = new Vector2((source + end) * .5f, -101f);
        beam.sizeDelta = new Vector2(Mathf.Abs(end - source), 112f);
        beam.localScale = new Vector3(direction, 1f, 1f);
        exhibitionSunbeam.color = new Color(1f, 1f, 1f, blocker >= 0 ? .75f : .92f);
    }

    private void PickExhibitionItem(int item)
    {
        exhibitionHeld = exhibitionHeld == item ? -1 : item;
        WorkshopAudioFeedback.Click();
        RefreshExhibition();
    }

    private void ClickExhibitionSeat(int seat)
    {
        if (exhibitionHeld < 0)
        {
            if (exhibition[seat] >= 0) PickExhibitionItem(exhibition[seat]);
            else if (seat >= OfferingColumns && Array.IndexOf(exhibition, SilkOffering) < 0)
                PutOffering(SilkOffering, seat);
            else RefreshExhibition("先点丝轴或酒坛拿起，再点这里放下");
            return;
        }
        PutOffering(exhibitionHeld, seat);
    }

    private void PutOffering(int item, int seat)
    {
        if (item < 0 || seat < 0 || !CanUseExhibitionSeat(seat)) return;
        int old = Array.IndexOf(exhibition, item);
        if (old >= 0) exhibition[old] = exhibition[seat];
        exhibition[seat] = item;
        exhibitionHeld = -1;
        WorkshopAudioFeedback.Place();
        RefreshExhibition();
    }

    private void BindOfferingDrag(GameObject target, Func<int> itemAtSource)
    {
        var trigger = target.AddComponent<EventTrigger>();
        int dragged = -1;
        void OnDrag(EventTriggerType kind, Action<PointerEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = kind };
            entry.callback.AddListener(data =>
            {
                if (data is PointerEventData pointer && pointer.button == PointerEventData.InputButton.Left) callback(pointer);
            });
            trigger.triggers.Add(entry);
        }
        OnDrag(EventTriggerType.BeginDrag, pointer =>
        {
            dragged = itemAtSource();
            if (dragged < 0) return;
            exhibitionGhost.sprite = exhibitionSprites[dragged];
            exhibitionGhost.enabled = exhibitionGhost.sprite != null;
            MoveOfferingGhost(pointer);
        });
        OnDrag(EventTriggerType.Drag, pointer => { if (dragged >= 0) MoveOfferingGhost(pointer); });
        OnDrag(EventTriggerType.EndDrag, pointer =>
        {
            exhibitionGhost.enabled = false;
            Vector2 at = OfferingPointer(pointer);
            for (int seat = 0; seat < OfferingSeatCount; seat++)
                if (ExhibitionSeatBounds(seat).Contains(at)) { PutOffering(dragged, seat); break; }
            dragged = -1;
        });
    }

    private void MoveOfferingGhost(PointerEventData pointer)
    {
        Vector2 at = OfferingPointer(pointer);
        exhibitionGhost.rectTransform.anchoredPosition = new Vector2(at.x - 75, -at.y + 52);
    }

    private Vector2 OfferingPointer(PointerEventData pointer)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera, out var at);
        return new Vector2(at.x, -at.y);
    }

    private void SubmitExhibition()
    {
        int silk = Array.IndexOf(exhibition, SilkOffering);
        if (!ExhibitionCanInspect(silk, WindowBlocker(silk)))
        {
            exhibitionSide = true;
            RefreshExhibition("先让光照到后排丝轴");
            return;
        }
        if (!exhibitionKnotFound)
        {
            RefreshExhibition("先在照亮的三段里找到接头");
            return;
        }
        exhibitionSide = true;
        FinishChallenge(100, "窗光验丝 · 已标记接头 · 入柜备织", AcceptedExhibitionSilk());
    }

    private void LightExhibitionLamp()
    {
        exhibitionSide = true;
        exhibitionLamp = true;
        RefreshExhibition("右灯已亮，留出通向丝轴的光路");
    }

    private void InspectExhibitionSilk(int segment)
    {
        int silk = Array.IndexOf(exhibition, SilkOffering);
        if (!ExhibitionCanInspect(silk, WindowBlocker(silk))) return;
        if (segment != exhibitionKnot % inspectionSegments.Length)
        {
            lastEvenSilkSegment = segment;
            WorkshopAudioFeedback.Notice();
            RefreshExhibition("这一段线身均匀，再找有粗结的一段");
            return;
        }
        exhibitionKnotFound = true;
        WorkshopAudioFeedback.Confirm();
        RefreshExhibition();
    }

    private string AcceptedExhibitionSilk()
    {
        var bands = exhibitionSilk.Split('/');
        var middle = bands[1].ToCharArray();
        middle[exhibitionKnot] = '2';
        return bands[0] + "/" + new string(middle) + "/" + bands[2];
    }

    private void DrawShuttleWorkshop()
    {
        if (showSongJin) { DrawLoom(); return; }
        weavingGesture = null;
        weavingGestureKey = -1;
        animatingWeaveRow = -1;
        movingShuttle = null;
        Layer("Autumn_LoomCloseup");
        Layer("Autumn_WarpAligned");
        string inspected = WorkshopUpstreamWork(WorkshopObservation.QiufenOffering, 0)?.sample;
        bool verified = !string.IsNullOrEmpty(inspected) && inspected.Split('/').Length == 3;
        string silkSample = verified ? inspected : WorkshopUpstreamWork(WorkshopObservation.MangzhongCocoon, 0)?.sample;
        string silkCaption = verified ? "秋分验收丝轴 · 接头已登记"
            : string.IsNullOrEmpty(silkSample) ? "工坊备丝 · 先验干湿" : "夏季丝样 · 先验干湿";
        DrawAutumnSilkHandoff("Loom Silk Handoff", new Rect(64, 7, 1032, 32), silkCaption, silkSample);
        if (!weavingSilkReady && !GameComplete)
        {
            DrawHanluSilkPreparation();
            return;
        }
        var weftArt = GameArtSprite("WeftTile");
        var shuttleArt = GameArtSprite("Shuttle") ?? WorkshopObservationArt.Current?.Shuttle;
        var heddleArt = GameArtSprite("LoomHeddle");
        var cloth = IllustratedGameCard(art, "Weaving Ribbon", new Rect(64, 196, 1032, 240),
            GameArtSprite("WoodDesk"), LightSurface);
        for (int row = 0; row < WeavingRows; row++)
            for (int col = 0; col < WeavingColumns; col++)
                weavingCells[row, col] = GameArtSurface(cloth, $"Weft {row},{col}",
                    new Rect(23 + 165 * col, 19 + 34 * row, 161, 31), weftArt, InsetPaper);

        if (GameComplete)
        {
            Copy("把六梭织样带去立冬染坊", "不同的经线起落织出不同纹样。留下这条纹带作花纹参照，再带同批素绸布头去立冬染缸试色。");
            Act("Look At Song Brocade", "看宋锦显花示意", ToggleSongJin, true);
            PaintWovenRows();
            return;
        }

        for (int row = 0; row < WeavingRows; row++)
        {
            var rowBounds = WorkshopUi.Rect(cloth, "Pick Highlight " + row, new Rect(21, 17 + 34 * row, 993, 35));
            weavingRowCues[row] = VisualStep(rowBounds);
        }

        for (int peek = 0; peek < 2; peek++)
        {
            bool current = peek == 0;
            var preview = WorkshopUi.PixelCard(art, "Pick Preview " + peek,
                current ? new Rect(64, 45, 660, 133) : new Rect(735, 45, 361, 133),
                current ? LightSurface : InsetPaper, WorkshopUi.Bronze);
            weftHints[peek] = WorkshopUi.Label(preview, "Scheduled Pick", new Rect(12, 0, current ? 201 : 190, 39), "", 23,
                WorkshopUi.Ink, TextAnchor.MiddleLeft);
            float arrowStart = current ? 241 : 30;
            float arrowY = current ? 18 : 98;
            for (int side = 0; side < 2; side++)
            {
                var direction = WorkshopUi.Rect(preview, "Pick Direction " + side, new Rect(0, 0, 1, 1));
                DrawAutumnArrow(direction, new Vector2(arrowStart + side * 99, arrowY),
                    new Vector2(arrowStart + (1 - side) * 99, arrowY), WorkshopUi.Ink);
                weftDirections[peek, side] = direction;
            }
            for (int shade = 0; shade < WeftInks.Length; shade++)
                weftColorChips[peek, shade] = GameArtSurface(preview, "Allowed Shuttle " + shade,
                    new Rect((current ? 396 : 179) + shade * 64, current ? 5 : 83, 51, 26), shuttleArt, WeftInks[shade]);
            for (int col = 0; col < WeavingColumns; col++)
            {
                weftHintPegs[peek, col] = GameArtSurface(preview, "Pattern Peg " + col,
                    current ? new Rect(109 + 87 * col, 44, 76, 30) : new Rect(20 + 54 * col, 41, 48, 30), weftArt, InsetPaper);
                if (!current) continue;
                liveHeddlePegs[col] = GameArtSurface(preview, "Live Heddle " + col,
                    new Rect(109 + 87 * col, 89, 76, 30), weftArt, InsetPaper);
                alignedPegMarks[col] = WorkshopUi.Fill(preview, "Shaft Alignment " + col,
                    new Rect(111 + 87 * col, 78, 70, 5), WorkshopUi.Green);
                mismatchedPegMarks[col] = WorkshopUi.Fill(preview, "Shaft Mismatch " + col,
                    new Rect(111 + 87 * col, 78, 70, 5), ErrorInk);
            }
            if (current)
            {
                WorkshopUi.Label(preview, "Pattern Row", new Rect(12, 41, 88, 34), "样纹", 21, WorkshopUi.Ink);
                WorkshopUi.Label(preview, "Actual Row", new Rect(12, 87, 88, 34), "梭口", 21, WorkshopUi.Ink);
                // Matching marks and the paired rows also make the comparison legible with captions hidden.
                DrawAutumnArrow(preview, new Vector2(77, 48), new Vector2(77, 112), WorkshopUi.Bronze);
            }
        }

        for (int i = 0; i < HeddleGroups.Length; i++)
        {
            int group = i;
            heddleCards[i] = AutumnControl("Heddle Group " + i, new Rect(64 + i * 346, 450, 338, 72),
                () => ToggleHeddle(group), heddleArt);
            heddlePictures[i] = heddleCards[i].transform.parent.Find("Tool")?.GetComponent<Image>();
            var lever = (RectTransform)heddleCards[i].transform.parent;
            heddleCards[i].rectTransform.anchoredPosition = new Vector2(122, -2);
            heddleCards[i].rectTransform.sizeDelta = new Vector2(210, 38);
            heddleCues[i] = VisualStep(lever);
            for (int col = 0; col < WeavingColumns; col++)
            {
                Color peg = RaisedWeavingThread(HeddleGroups[i], col) ? WorkshopUi.Bronze : InsetPaper;
                WorkshopUi.Fill(lever, "Raised Group " + col, new Rect(125 + col * 30, 45, 22, 12), peg);
            }
        }
        WorkshopUi.PixelCard(art, "Shuttle Dock Left", new Rect(64, 535, 429, 86), InsetPaper, WorkshopUi.Bronze);
        WorkshopUi.PixelCard(art, "Shuttle Dock Right", new Rect(667, 535, 429, 86), InsetPaper, WorkshopUi.Bronze);
        WorkshopUi.Label(art, "Empty Move Budget", new Rect(505, 607, 150, 32), "空送", 19, WorkshopUi.Ink);
        for (int i = 0; i < emptyMoveMarks.Length; i++)
            emptyMoveMarks[i] = WorkshopUi.Fill(art, "Empty Move Token " + i, new Rect(572 + 31 * i, 616, 24, 14), WorkshopUi.Bronze);
        for (int i = 0; i < WeftNames.Length; i++)
        {
            int shuttle = i;
            shuttleCards[i] = AutumnControl("Send Shuttle " + i, new Rect(0, 543, 200, 70),
                () => ClickWeavingShuttle(shuttle), shuttleArt);
            var tool = (RectTransform)shuttleCards[i].transform.parent;
            shuttlePictures[i] = tool.Find("Tool")?.GetComponent<Image>();
            if (shuttlePictures[i] != null)
            {
                shuttlePictures[i].rectTransform.anchoredPosition = new Vector2(12, -6);
                shuttlePictures[i].rectTransform.sizeDelta = new Vector2(100, 39);
            }
            shuttleCards[i].rectTransform.anchoredPosition = new Vector2(5, -43);
            shuttleCards[i].rectTransform.sizeDelta = new Vector2(190, 23);
            shuttleCards[i].fontSize = 19;
            shuttleCues[i] = VisualStep(tool);
            for (int side = 0; side < 2; side++)
            {
                float from = side == 0 ? 126 : 175, to = side == 0 ? 175 : 126;
                var send = WorkshopUi.Rect(tool, "Weave Arrow " + side, new Rect(0, 0, 1, 1));
                DrawAutumnArrow(send, new Vector2(from, 28), new Vector2(to, 28), WeftInks[i]);
                shuttleSends[i, side] = send;
                var emptySend = WorkshopUi.Rect(tool, "Empty Move Arrow " + side, new Rect(0, 0, 1, 1));
                DrawAutumnDottedArrow(emptySend, from, to, 28, WeftInks[i]);
                shuttleEmptySends[i, side] = emptySend;
            }
        }

        Copy("对准样纹，送梭织入", "");
        weavingGuide = infoPanel.Find("Instruction Body").GetComponent<Text>();
        Act("Move Red Shuttle", "空送赭梭到另一侧", () => MoveEmptyShuttle(0));
        redEmptyShuttle = infoPanel.Find("Move Red Shuttle").GetComponentInChildren<Button>();
        Act("Move Blue Shuttle", "空送青梭到另一侧", () => MoveEmptyShuttle(1));
        blueEmptyShuttle = infoPanel.Find("Move Blue Shuttle").GetComponentInChildren<Button>();
        Act("Reset Weaving Segment", "退回这一段开头", RestoreWeavingCheckpoint);
        restartWeavingSegment = infoPanel.Find("Reset Weaving Segment").GetComponentInChildren<Button>();
        var undo = WorkshopUi.PixelButton(infoPanel, "Undo Last Empty Move", new Rect(27, 253, 444, 48),
            "撤回这次空送", UndoLastEmptyShuttle, false, 23, WorkshopUi.Green);
        undoEmptyShuttle = undo;
        RegisterButton(undo);
        for (int side = 0; side < 2; side++)
        {
            var direction = WorkshopUi.Rect(art, "Current Direction " + side, new Rect(0, 0, 1, 1));
            DrawAutumnArrow(direction, new Vector2(513 + side * 134, 577), new Vector2(513 + (1 - side) * 134, 577), WorkshopUi.Bronze);
            currentWeavingDirection[side] = direction;
        }
        weavingStatus = WorkshopUi.Label(infoPanel, "Weaving Status", new Rect(27, 565, 444, 90), "", 24,
            WorkshopUi.Green, TextAnchor.UpperLeft);
        UpdateWeaving();
    }

    private void DrawHanluSilkPreparation()
    {
        var tray = GameArtSprite("WoodDesk");
        var silk = SpringSummerArt("SilkBobbin") ?? Resources.Load<Sprite>("WorkshopFinish/RawSilk");
        var dew = GameArtSprite("DewBead");
        for (int i = 0; i < 2; i++)
        {
            bool window = i == 0;
            var seat = IllustratedGameCard(art, "Hanlu Silk Location " + i,
                new Rect(76 + 510 * i, 188, 480, 305), tray, LightSurface);
            WorkshopUi.Picture(seat, "Silk Bobbin", new Rect(112, 40, 256, 174), silk);
            var choose = WorkshopUi.Button(seat, window ? "Hanlu Window Spool" : "Hanlu Cabinet Spool", new Rect(1, 1, 478, 216), "",
                () => SelectHanluSpool(window), Color.clear);
            RegisterButton(choose);
            if (window && weavingWindowWet)
            {
                WorkshopUi.Picture(seat, "Visible Cold Dew", new Rect(315, 56, 62, 79), dew);
                DrawAutumnLine(seat, new Vector2(385, 172), new Vector2(421, 208), ErrorInk, 7);
                DrawAutumnLine(seat, new Vector2(421, 172), new Vector2(385, 208), ErrorInk, 7);
            }
            else
            {
                DrawAutumnLine(seat, new Vector2(379, 186), new Vector2(392, 202), WorkshopUi.Green, 7);
                DrawAutumnLine(seat, new Vector2(392, 202), new Vector2(423, 170), WorkshopUi.Green, 7);
                VisualStep(seat);
            }
            WorkshopUi.Label(seat, "Silk State", new Rect(19, 225, 444, 66), window ?
                weavingWindowWet ? "窗沿丝轴 · 有露" : "窗沿丝轴 · 干爽"
                : "柜中丝轴 · 干爽", 28, WorkshopUi.Ink, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
        }

        Copy("先选干爽丝轴", weavingWindowWet ? "寒露清晨，窗沿凝露。" : "本单是干爽的午后。");
        if (weavingWindowWet)
        {
            DrawAutumnArrow(art, new Vector2(1081, 365), new Vector2(970, 365), WorkshopUi.Bronze);
            ShowGesture(art, new Vector2(834, 309), new Vector2(834, 309), WorkGesture.Tap);
        }
        if (weavingWindowWet)
            Act("Hanlu Condition Spool", "把湿轴移到整理架", () =>
            {
                weavingWindowWet = false;
                weavingPrepHint = "上架整理后，窗边丝轴也可上机。";
                Render();
            });
        if (!string.IsNullOrEmpty(weavingPrepHint))
            WorkshopUi.Label(infoPanel, "Hanlu Dew Feedback", new Rect(27, 567, 444, 60),
                weavingPrepHint, 23, WorkshopUi.Green, TextAnchor.UpperLeft);
    }

    private void SelectHanluSpool(bool window)
    {
        if (window && weavingWindowWet)
        {
            weavingPrepHint = "丝轴上还有冷露。可先移至整理架。";
            Render();
            return;
        }
        weavingSilkReady = true;
        Render();
    }

    private void DrawAutumnSilkHandoff(string name, Rect bounds, string caption, string sample)
    {
        var strip = WorkshopUi.PixelCard(art, name, bounds, WorkshopUi.Ink, WorkshopUi.Bronze);
        WorkshopUi.Label(strip, "Silk Handoff Caption", new Rect(15, 0, bounds.width - 168, bounds.height), caption,
            21, WorkshopUi.Cream, TextAnchor.MiddleLeft);
        DrawWorkSample(strip, new Rect(bounds.width - 151, 3, 136, bounds.height - 6), sample);
    }

    private Text AutumnControl(string name, Rect bounds, Action input, Sprite tool = null)
    {
        var card = WorkshopUi.PixelCard(art, name, bounds, LightSurface, WorkshopUi.Bronze);
        var button = WorkshopUi.Button(card, "Action", new Rect(1, 1, bounds.width - 2, bounds.height - 2), "", input, Color.clear);
        RegisterButton(button);
        bool compact = bounds.width < 250f;
        float toolWidth = compact ? 56f : 116f;
        if (tool != null)
            WorkshopUi.Picture(card, "Tool", new Rect(13, 9, toolWidth, bounds.height - 18), tool);
        float captionLeft = tool != null ? toolWidth + 15f : 6f;
        return WorkshopUi.Label(card, "Caption", new Rect(captionLeft, 1, bounds.width - captionLeft - 6, bounds.height - 2), "", compact ? 21 : 27,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
    }

    private static int WeavingThreadMask(int groups)
    {
        int threads = 0;
        for (int i = 0; i < HeddleGroups.Length; i++)
            if ((groups & (1 << i)) != 0) threads ^= HeddleGroups[i];
        return threads;
    }

    private static bool RaisedWeavingThread(int mask, int col) => (mask & (1 << (WeavingColumns - 1 - col))) != 0;
    private int ShuttleSide(int shuttle) => (shuttlesRight >> shuttle) & 1;
    private int WeavingMoveBudget => ChallengeVariant == 0 ? 2 : 1;

    private void UpdateWeaving(string message = null)
    {
        PaintWovenRows();
        if (GameComplete || wovenCount >= WeavingRows) return;
        int requiredGroups = WeftPatterns[ChallengeVariant][wovenCount];
        int difference = heddles ^ requiredGroups;
        int suggestedShuttle = SuggestedWeavingShuttle();
        bool firstPick = ChallengeVariant == 0 && wovenCount == 0;
        bool practiceHalf = ChallengeVariant == 0 && wovenCount < WeavingRows / 2;
        bool canContinue = suggestedShuttle >= 0;
        int teachingHeddle = -1;
        if (firstPick && canContinue)
            for (int group = 0; group < HeddleGroups.Length; group++)
                if ((difference & (1 << group)) != 0) { teachingHeddle = group; break; }
        bool teachShuttle = firstPick && canContinue && difference == 0;
        int colors = WeftColors[ChallengeVariant][wovenCount];
        weavingGuide.text = !canContinue ? "空送不够了，退回这一段开头。"
            : difference != 0 ? "点经线，把上下两排对齐。\n红线标出还不同的位置。"
            : colors == 3 ? "两种颜色都能织。看下一梭，再选从哪边送。"
            : ShuttleSide(suggestedShuttle) != wovenCount % 2 ? "先点虚线箭头把梭移过来，再点实线织入。"
            : "两排已经对齐，点带实线箭头的对应色梭。";
        int threads = WeavingThreadMask(heddles);
        int requiredThreads = WeavingThreadMask(requiredGroups);
        for (int col = 0; col < WeavingColumns; col++)
        {
            bool up = RaisedWeavingThread(threads, col);
            bool aligned = up == RaisedWeavingThread(requiredThreads, col);
            liveHeddlePegs[col].color = up ? WorkshopUi.Bronze : InsetPaper;
            alignedPegMarks[col].enabled = aligned;
            mismatchedPegMarks[col].enabled = !aligned;
        }
        for (int i = 0; i < HeddleGroups.Length; i++)
        {
            bool raised = ((heddles >> i) & 1) == 1;
            bool needsChange = practiceHalf && (difference & (1 << i)) != 0;
            string action = raised ? " ↓" : " ↑";
            string state = raised ? " · 已抬" : " · 已落";
            heddleCards[i].text = HeddleNames[i] + (needsChange ? action : state);
            heddleCards[i].color = needsChange || raised ? WorkshopUi.Green : WorkshopUi.Faded;
            heddleCues[i].gameObject.SetActive(canContinue && (firstPick ? teachingHeddle == i : needsChange));
            var face = heddleCards[i].transform.parent.Find("Surface").GetComponent<Image>();
            face.color = raised ? new Color32(223, 232, 197, 255) : LightSurface;
            if (heddlePictures[i] == null) continue;
            heddlePictures[i].color = raised ? Color.white : new Color32(159, 139, 117, 255);
            heddlePictures[i].rectTransform.anchoredPosition = new Vector2(13f, raised ? -5f : -11f);
        }
        for (int i = 0; i < WeftNames.Length; i++)
        {
            int side = ShuttleSide(i);
            bool fromCorrectSide = side == wovenCount % 2;
            bool matchesColor = (colors & (1 << i)) != 0;
            bool ready = canContinue && difference == 0 && matchesColor;
            bool needsEmptyMove = ready && !fromCorrectSide && emptyRuns < WeavingMoveBudget;
            bool canWeave = ready && fromCorrectSide;
            var shuttleCard = (RectTransform)shuttleCards[i].transform.parent;
            shuttleCard.anchoredPosition = new Vector2((side == 0 ? 69 : 672) + i * 215, -543);
            string action = !matchesColor ? "本梭不用"
                : !canContinue || ready && !canWeave && !needsEmptyMove ? "待调位"
                : difference != 0 ? fromCorrectSide ? "待对齐" : "对齐后空送"
                : needsEmptyMove ? "先空送" : "织入";
            shuttleCards[i].text = WeftNames[i] + " · " + action;
            // Single-colour orders can point to the required one; the two-colour orders leave the route choice to the player.
            bool emphasized = teachShuttle && ready || colors != 3 && (needsEmptyMove || practiceHalf && canWeave);
            shuttleCues[i].gameObject.SetActive(emphasized);
            for (int direction = 0; direction < 2; direction++)
            {
                shuttleSends[i, direction].gameObject.SetActive(canWeave && direction == side);
                shuttleEmptySends[i, direction].gameObject.SetActive(needsEmptyMove && direction == side);
            }
            var card = shuttleCards[i].transform.parent.Find("Surface")?.GetComponent<Image>();
            if (card != null) card.color = canWeave ? new Color32(223, 232, 197, 255)
                : needsEmptyMove ? new Color32(244, 223, 185, 255) : InsetPaper;
            shuttleCards[i].color = canWeave || needsEmptyMove ? WeftInks[i] : WorkshopUi.Faded;
            if (shuttlePictures[i] != null)
            {
                Color tint = Color.Lerp(Color.white, WeftInks[i], .68f);
                tint.a = canWeave || needsEmptyMove ? 1f : .56f;
                shuttlePictures[i].color = tint;
            }
        }

        for (int peek = 0; peek < weftHints.Length; peek++)
        {
            int row = wovenCount + peek;
            bool valid = row < WeavingRows;
            int shade = valid ? WeftColors[ChallengeVariant][row] : 0;
            weftHints[peek].text = peek == 0 ? $"第 {row + 1} / {WeavingRows} 梭" : valid ? "下一梭" : "最后一梭";
            for (int side = 0; side < 2; side++)
                weftDirections[peek, side].gameObject.SetActive(valid && row % 2 == side);
            for (int ink = 0; ink < WeftInks.Length; ink++)
                weftColorChips[peek, ink].enabled = (shade & (1 << ink)) != 0;
            int pattern = valid ? WeavingThreadMask(WeftPatterns[ChallengeVariant][row]) : 0;
            for (int col = 0; col < WeavingColumns; col++)
                weftHintPegs[peek, col].color = RaisedWeavingThread(pattern, col) ? WorkshopUi.Bronze : InsetPaper;
        }
        for (int i = 0; i < currentWeavingDirection.Length; i++)
            currentWeavingDirection[i].gameObject.SetActive(i == wovenCount % 2);
        for (int i = 0; i < weavingRowCues.Length; i++)
            weavingRowCues[i].gameObject.SetActive(i == wovenCount);
        for (int i = 0; i < emptyMoveMarks.Length; i++)
        {
            emptyMoveMarks[i].enabled = i < WeavingMoveBudget;
            emptyMoveMarks[i].color = i < WeavingMoveBudget - emptyRuns ? WorkshopUi.Bronze : WorkshopUi.Line;
        }
        EnableEmptyMoves(!firstPick && emptyRuns < WeavingMoveBudget);
        SetAutumnActionVisibility(undoEmptyShuttle, lastEmptyShuttle >= 0);
        SetAutumnActionVisibility(restartWeavingSegment, !canContinue || wovenCount > 0 || emptyRuns > 0);
        if (teachingHeddle >= 0)
            SetWeavingGesture(teachingHeddle, new Vector2(222 + 346 * teachingHeddle, 472));
        else if (teachShuttle)
            SetWeavingGesture(3 + suggestedShuttle, new Vector2((ShuttleSide(suggestedShuttle) == 0 ? 131 : 734) + suggestedShuttle * 215, 563));
        else SetWeavingGesture(-1, Vector2.zero);
        weavingStatus.text = message ?? $"已织 {wovenCount} / {WeavingRows} 梭\n剩余空送 {WeavingMoveBudget - emptyRuns} 次";
    }

    private void EnableEmptyMoves(bool available)
    {
        SetAutumnActionVisibility(redEmptyShuttle, available);
        SetAutumnActionVisibility(blueEmptyShuttle, available);
    }

    private void SetWeavingGesture(int key, Vector2 position)
    {
        if (key == weavingGestureKey) return;
        if (weavingGesture != null)
        {
            weavingGesture.gameObject.SetActive(false);
            Destroy(weavingGesture.gameObject);
        }
        weavingGestureKey = key;
        weavingGesture = key < 0 ? null : ShowGesture(art, position, position, WorkGesture.Tap);
    }

    private int SuggestedWeavingShuttle()
    {
        int colors = WeftColors[ChallengeVariant][wovenCount];
        for (int pass = 0; pass < 2; pass++)
            for (int shuttle = 0; shuttle < 2; shuttle++)
            {
                if ((colors & (1 << shuttle)) == 0) continue;
                bool empty = ShuttleSide(shuttle) != wovenCount % 2;
                if (empty != (pass == 1) || emptyRuns + (empty ? 1 : 0) > WeavingMoveBudget) continue;
                int nextSides = empty ? shuttlesRight : shuttlesRight ^ (1 << shuttle);
                if (CanFinishRibbon(wovenCount + 1, nextSides, WeavingMoveBudget - emptyRuns - (empty ? 1 : 0)))
                    return shuttle;
            }
        return -1;
    }

    private void ClickWeavingShuttle(int shuttle)
    {
        if (GameComplete) return;
        if (ShuttleSide(shuttle) != wovenCount % 2)
        {
            if ((WeftColors[ChallengeVariant][wovenCount] & (1 << shuttle)) == 0)
            {
                RejectShuttle("这一梭需要另一种颜色。");
                return;
            }
            MoveEmptyShuttle(shuttle);
        }
        else WeavePick(shuttle);
    }

    private void PaintWovenRows()
    {
        int draft = WeavingThreadMask(heddles);
        for (int row = 0; row < WeavingRows; row++)
        {
            bool hasDraft = !GameComplete && row == wovenCount;
            for (int col = 0; col < WeavingColumns; col++)
            {
                int distance = row % 2 == 0 ? col : WeavingColumns - 1 - col;
                if (row < wovenCount && (row != animatingWeaveRow || distance < revealedWeaveThreads))
                    weavingCells[row, col].color = RaisedWeavingThread(wovenPatterns[row], col)
                        ? (Color)WeftInks[wovenColors[row]] : LightSurface;
                else
                    weavingCells[row, col].color = hasDraft && RaisedWeavingThread(draft, col)
                        ? Color.Lerp(InsetPaper, WorkshopUi.Bronze, .42f) : InsetPaper;
            }
        }
    }

    private void ToggleHeddle(int group)
    {
        heddles ^= 1 << group;
        WorkshopAudioFeedback.Click();
        UpdateWeaving();
    }

    private void MoveEmptyShuttle(int shuttle)
    {
        if (GameComplete) return;
        if (emptyRuns >= WeavingMoveBudget) { RejectShuttle("空送已用完。可撤回一次空送，或重排本段。"); return; }
        int fromSide = ShuttleSide(shuttle);
        lastEmptyShuttle = shuttle;
        emptyRuns++;
        shuttlesRight ^= 1 << shuttle;
        WorkshopAudioFeedback.Place();
        UpdateWeaving(WeftNames[shuttle] + "已换边 · 布纹不变");
        ShowShuttlePass(shuttle, fromSide, wovenCount, true);
    }

    private void UndoLastEmptyShuttle()
    {
        if (GameComplete || lastEmptyShuttle < 0) return;
        int shuttle = lastEmptyShuttle;
        int fromSide = ShuttleSide(shuttle);
        lastEmptyShuttle = -1;
        emptyRuns--;
        shuttlesRight ^= 1 << shuttle;
        WorkshopAudioFeedback.Place();
        UpdateWeaving("梭子回到原位 · 补回一次空送");
        ShowShuttlePass(shuttle, fromSide, wovenCount, true);
    }

    private void RejectShuttle(string message)
    {
        WorkshopAudioFeedback.Notice();
        UpdateWeaving(message);
    }

    private void WeavePick(int shuttle)
    {
        if (GameComplete) return;
        if (ShuttleSide(shuttle) != wovenCount % 2) { RejectShuttle("这把梭在另一侧，先留着或空送。"); return; }
        if ((WeftColors[ChallengeVariant][wovenCount] & (1 << shuttle)) == 0) { RejectShuttle("这一梭需要另一种颜色。"); return; }
        int threadMask = WeavingThreadMask(heddles);
        if (threadMask != WeavingThreadMask(WeftPatterns[ChallengeVariant][wovenCount]))
        {
            RejectShuttle("这一梭还没织。先对齐红线标出的经线。");
            return;
        }

        int fromSide = ShuttleSide(shuttle);
        int nextSide = shuttlesRight ^ (1 << shuttle);
        if (!CanFinishRibbon(wovenCount + 1, nextSide, WeavingMoveBudget - emptyRuns))
        {
            RejectShuttle("这样送梭，后面会少一次空送。换一把梭，或撤回刚才的空送。");
            return;
        }

        lastEmptyShuttle = -1;
        wovenPatterns[wovenCount] = threadMask;
        wovenColors[wovenCount] = shuttle;
        wovenCount++;
        shuttlesRight = nextSide;
        if (wovenCount == WeavingRows)
        {
            string ribbon = "";
            for (int row = 0; row < WeavingRows; row++)
            {
                if (row != 0) ribbon += "/";
                for (int col = 0; col < WeavingColumns; col++)
                    ribbon += RaisedWeavingThread(wovenPatterns[row], col) ? wovenColors[row] == 0 ? "3" : "5" : "1";
            }
            FinishChallenge(100 + (WeavingMoveBudget - emptyRuns) * 10, $"双色织带 · 六梭完成 · 空送 {emptyRuns} 次", ribbon);
            if (WorkshopProgress.SeasonalOrderEnabled && IsAtOwnWorkshop(observation))
                WorkshopProgress.Award(WorkshopItem.RawSilkCloth);
            ShowShuttlePass(shuttle, fromSide, wovenCount - 1);
            return;
        }

        if (wovenCount == WeavingRows / 2) SaveWeavingCheckpoint();
        WorkshopAudioFeedback.Progress();
        UpdateWeaving($"第 {wovenCount} 梭织入 · 下一个梭口已就位");
        ShowShuttlePass(shuttle, fromSide, wovenCount - 1);
    }

    private void ShowShuttlePass(int shuttle, int fromSide, int row, bool empty = false)
    {
        var sprite = GameArtSprite("Shuttle") ?? WorkshopObservationArt.Current?.Shuttle;
        if (sprite == null) return;

        StopShuttlePass();
        if (!empty)
        {
            animatingWeaveRow = row;
            revealedWeaveThreads = 0;
        }
        PaintWovenRows();
        Color tint = Color.Lerp(Color.white, WeftInks[shuttle], .62f);
        if (empty) tint.a = .7f;
        movingShuttle = WorkshopUi.Picture(art, empty ? "Shuttle Crossing Empty" : "Shuttle In Motion",
            new Rect(0, 0, 145, 45), sprite, tint);
        StartCoroutine(AnimateShuttlePass(movingShuttle, fromSide, row, empty));
    }

    private IEnumerator AnimateShuttlePass(Image shuttle, int fromSide, int row, bool empty)
    {
        float duration = empty ? .23f : .34f;
        float start = fromSide == 0 ? 55f : 966f;
        float finish = fromSide == 0 ? 966f : 55f;
        float y = empty ? -542f : -(208f + row * 34f);
        for (float elapsed = 0f; shuttle != null && shuttle.gameObject.activeInHierarchy && elapsed < duration;
             elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            float travel = Mathf.SmoothStep(0f, 1f, progress);
            shuttle.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(start, finish, travel),
                y + (empty ? 24f * Mathf.Sin(Mathf.PI * progress) : 0f));
            if (!empty)
            {
                int revealed = Mathf.FloorToInt(travel * (WeavingColumns + 1));
                if (revealedWeaveThreads != revealed)
                {
                    revealedWeaveThreads = revealed;
                    PaintWovenRows();
                }
            }
            yield return null;
        }
        if (movingShuttle != shuttle || shuttle == null) yield break;
        StopShuttlePass();
        PaintWovenRows();
    }

    private void StopShuttlePass()
    {
        animatingWeaveRow = -1;
        if (movingShuttle == null) return;
        movingShuttle.gameObject.SetActive(false);
        Destroy(movingShuttle.gameObject);
        movingShuttle = null;
    }

    private bool CanFinishRibbon(int row, int sides, int emptyMoves)
    {
        if (row >= WeavingRows) return true;
        int acceptedColors = WeftColors[ChallengeVariant][row];
        for (int shuttle = 0; shuttle < WeftNames.Length; shuttle++)
        {
            int bit = 1 << shuttle;
            if ((acceptedColors & bit) == 0) continue;
            bool needsEmptyMove = ((sides >> shuttle) & 1) != row % 2;
            if (needsEmptyMove && emptyMoves == 0) continue;
            if (CanFinishRibbon(row + 1, needsEmptyMove ? sides : sides ^ bit, emptyMoves - (needsEmptyMove ? 1 : 0)))
                return true;
        }
        return false;
    }

    private void SaveWeavingCheckpoint()
    {
        weavingCheckpointSides = shuttlesRight;
        weavingCheckpointHeddles = heddles;
        weavingCheckpointRuns = emptyRuns;
    }

    private void RestoreWeavingCheckpoint()
    {
        StopShuttlePass();
        lastEmptyShuttle = -1;
        wovenCount = wovenCount < WeavingRows / 2 ? 0 : WeavingRows / 2;
        shuttlesRight = weavingCheckpointSides;
        heddles = weavingCheckpointHeddles;
        emptyRuns = weavingCheckpointRuns;
        UpdateWeaving("已退回本段起点");
    }

    private static Sprite GameArtSprite(string name) => Resources.Load<Sprite>("WorkshopGameArt/" + name);

    private static Image GameArtSurface(Transform parent, string name, Rect bounds, Sprite sprite, Color tint)
    {
        return sprite != null
            ? WorkshopUi.Picture(parent, name, bounds, sprite, tint, stretch: true)
            : WorkshopUi.Fill(parent, name, bounds, tint);
    }

    private static RectTransform IllustratedGameCard(Transform parent, string name, Rect bounds, Sprite art,
        Color fallback, Color? tint = null)
    {
        if (art == null) return WorkshopUi.PixelCard(parent, name, bounds, fallback, WorkshopUi.Bronze);

        var card = WorkshopUi.Rect(parent, name, bounds);
        WorkshopUi.Picture(card, "Illustrated Surface", new Rect(0, 0, bounds.width, bounds.height), art, tint, stretch: true);
        return card;
    }
}
