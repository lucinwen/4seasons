using System;
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

    private void DrawAutumnSilkHandoff(string name, Rect bounds, string caption, string sample)
    {
        var strip = WorkshopUi.PixelCard(art, name, bounds, WorkshopUi.Ink, WorkshopUi.Bronze);
        WorkshopUi.Label(strip, "Silk Handoff Caption", new Rect(15, 0, bounds.width - 168, bounds.height), caption,
            21, WorkshopUi.Cream, TextAnchor.MiddleLeft);
        DrawWorkSample(strip, new Rect(bounds.width - 151, 3, 136, bounds.height - 6), sample);
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
