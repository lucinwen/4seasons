using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private const int InspectionBudget = 3;
    private const int CocoonsPerRow = 3;
    private const int InspectionCocoonCount = 9;
    private const int SpringCocoonReadyFromDay = 6;
    private const char RoundCocoonInk = '4';
    private const char LongCocoonInk = '5';
    private static readonly Color SummerJade = new Color32(55, 105, 91, 255);
    private static readonly Color SummerGold = new Color32(253, 217, 139, 255);
    private static readonly Color SummerDark = new Color32(31, 61, 52, 255);
    private static readonly Color SummerNeutral = new Color32(247, 236, 206, 255);
    private static readonly Color CocoonTilePaper = new Color32(255, 246, 224, 192);
    private static readonly Color SummerWarning = new Color32(154, 74, 56, 255);
    private static readonly Color SummerUrgent = new Color32(242, 134, 98, 255);
    private static readonly Color SummerReady = new Color32(153, 227, 181, 255);
    private static readonly Rect DryingShelterRack = new Rect(34, 99, 484, 345);
    private static readonly Rect DryingWindowRack = new Rect(550, 99, 484, 345);

    private enum CocoonEvidence { Hidden, Intact, Cracked, Damp, Immature, NeedsReview }
    private enum CocoonTray { Pending, Delivery, Review, Mount }

    private readonly struct CocoonSample
    {
        internal readonly bool Long;
        internal readonly bool Covered;
        internal readonly int MountDays;
        private readonly CocoonEvidence shellCondition;
        internal CocoonEvidence Condition => MountDays < SpringCocoonReadyFromDay ? CocoonEvidence.Immature : shellCondition;

        internal CocoonSample(bool longShape, bool covered, CocoonEvidence shellCondition = CocoonEvidence.Intact, int mountDays = 7)
        {
            Long = longShape;
            Covered = covered;
            MountDays = mountDays;
            this.shellCondition = shellCondition;
        }
    }

    private sealed class InspectionOrder
    {
        internal readonly string Name;
        internal readonly int RoundCount;
        internal readonly int LongCount;
        internal readonly CocoonSample[] Samples;

        internal InspectionOrder(string name, int roundCount, int longCount, params CocoonSample[] samples)
        {
            Name = name;
            RoundCount = roundCount;
            LongCount = longCount;
            Samples = samples;
        }
    }

    private static readonly InspectionOrder[] InspectionOrders =
    {
        new InspectionOrder("织坊首单", 3, 2,
            new CocoonSample(false, false), new CocoonSample(true, false, CocoonEvidence.Cracked), new CocoonSample(false, true),
            new CocoonSample(false, true, CocoonEvidence.Damp), new CocoonSample(true, true), new CocoonSample(false, false),
            new CocoonSample(true, false, mountDays: 3), new CocoonSample(false, true), new CocoonSample(true, true)),
        new InspectionOrder("细丝加单", 2, 4,
            new CocoonSample(true, false), new CocoonSample(false, true), new CocoonSample(false, true, CocoonEvidence.Cracked),
            new CocoonSample(true, true), new CocoonSample(false, true, CocoonEvidence.Damp), new CocoonSample(true, false),
            new CocoonSample(true, true), new CocoonSample(false, true), new CocoonSample(true, false, mountDays: 3))
    };

    private readonly bool[] cocoonTurned = new bool[InspectionCocoonCount];
    private readonly CocoonTray[] cocoonTrays = new CocoonTray[InspectionCocoonCount];
    private readonly Image[] cocoonTileFaces = new Image[InspectionCocoonCount];
    private readonly Image[] cocoonTileImages = new Image[InspectionCocoonCount];
    private readonly Image[] cocoonTileClues = new Image[InspectionCocoonCount];
    private readonly Text[] cocoonTileLabels = new Text[InspectionCocoonCount];
    private readonly RectTransform[] cocoonTurnTargets = new RectTransform[InspectionCocoonCount];
    private readonly Image[] deliveryCocoons = new Image[6];
    private readonly Image[] reviewCocoons = new Image[2];
    private Image mountedCocoon;
    private RectTransform inspectionGesture;
    private int inspectionGestureStep = -1;
    private bool inspectionDragging;
    private static readonly Rect CocoonDeliveryArea = new Rect(700, 118, 385, 166);
    private static readonly Rect CocoonReviewArea = new Rect(700, 306, 385, 166);
    private static readonly Rect CocoonMountArea = new Rect(700, 476, 385, 48);
    private int inspectedCocoonCount;
    private int selectedCocoon;
    private Text cocoonSelectedText;
    private Text cocoonNote;
    private Text cocoonDeliveryText;
    private Text cocoonReviewText;
    private Text cocoonMountText;
    private Text cocoonBudgetText;
    private Button cocoonFlipButton;
    private Button cocoonSubmitButton;
    private Button cocoonMountButton;
    private readonly Button[] cocoonRouteButtons = new Button[2];
    private RectTransform cocoonDragChip;
    private Image cocoonDragSprite;
    private Text cocoonDragLabel;
    private InspectionOrder ActiveInspection => InspectionOrders[ChallengeVariant];

    private static readonly float[][] DashuLeads =
    {
        new[] { 6f, 13.5f, 12f },
        new[] { 6.5f, 11.5f, 8f, 12f }
    };
    private const float DashuReadyToSplice = 2.8f;
    private const float DashuSilkRunningOut = 0.85f;
    private const float DashuSafeStart = 0.34f;
    private const float DashuSafeEnd = 0.77f;
    private const float DashuMaxSpliceSpeed = 0.19f;
    private const float DashuGaugeWidth = 716f;
    private const int DashuReserveNotches = 12;
    private const int FreshSilkMoisture = 3;
    private enum SilkDryingPlace { None, Shelter, Window }
    private readonly bool[] dashuSilkJoins = new bool[12];
    private readonly Image[] dashuReserveCells = new Image[DashuReserveNotches];
    private readonly RectTransform[] dashuJoinTicks = new RectTransform[12];
    private float dashuSpeed;
    private float dashuStrain;
    private float dashuWrapped;
    private float dashuRemaining;
    private float dashuTension;
    private int dashuLead;
    private int dashuSplices;
    private int dashuCleanSplices;
    private int dashuBreaks;
    private bool dashuCocoonsPrepared;
    private bool dashuHoldingPointer;
    private bool dashuSlowGear;
    private bool dashuThreadAttached;
    private bool dashuDrying;
    private int dashuMoisture;
    private SilkDryingPlace dashuDryingPlace;
    private float dashuNoticeTime;
    private string dashuNote;
    private Text dashuNoteText;
    private Text dashuWrappedText;
    private Text dashuGaugeInputText;
    private Text dashuSupplyText;
    private Button dashuPrepareButton;
    private Text dashuPrepareLabel;
    private Text dashuGearText;
    private RectTransform dashuGearControl;
    private Text dashuActionText;
    private Button dashuWindingButton;
    private Button dashuSpliceButton;
    private Image dashuLengthBar;
    private RectTransform dashuSpliceThreshold;
    private Text dashuSpliceText;
    private Image dashuNeedle;
    private Image dashuSafeTrack;
    private Image dashuStrainBar;
    private Image dashuThread;
    private Image dashuWheel;
    private RectTransform dashuPotTarget;
    private RectTransform dashuWheelTarget;
    private RectTransform dashuSpliceTarget;
    private RectTransform dashuReleaseSign;
    private RectTransform dashuGaugeReleaseMark;
    private RectTransform dashuGauge;
    private RectTransform dashuGuide;
    private RectTransform dashuReelHighlight;
    private int dashuGuideStep;
    private bool dashuTriedWinding;
    private bool dashuWarnedAboutReserve;
    private bool dashuDraggingSilk;
    private float dashuWheelAngle;
    private float DashuLengthGoal => ChallengeVariant == 0 ? 14f : 24f;
    private bool DashuRainy => ChallengeVariant == 0;

    private void ResetSummerChallenge()
    {
        if (observation == WorkshopObservation.MangzhongCocoon)
            ResetInspection();
        if (observation != WorkshopObservation.DashuReeling) return;

        dashuSpeed = dashuStrain = dashuWrapped = dashuTension = 0f;
        dashuLead = dashuSplices = dashuCleanSplices = dashuBreaks = 0;
        dashuRemaining = DashuLeads[ChallengeVariant][0];
        dashuCocoonsPrepared = dashuHoldingPointer = dashuThreadAttached = dashuDrying = false;
        dashuTriedWinding = false;
        dashuWarnedAboutReserve = false;
        dashuDraggingSilk = false;
        dashuSlowGear = ChallengeVariant == 0;
        dashuDryingPlace = SilkDryingPlace.None;
        dashuMoisture = FreshSilkMoisture;
        dashuWheelAngle = 0f;
        dashuNote = null;
        dashuNoticeTime = 0f;
        Array.Clear(dashuSilkJoins, 0, dashuSilkJoins.Length);
    }

    private void ResetInspection()
    {
        Array.Clear(cocoonTurned, 0, cocoonTurned.Length);
        Array.Clear(cocoonTrays, 0, cocoonTrays.Length);
        inspectedCocoonCount = 0;
        selectedCocoon = -1;
        inspectionDragging = false;
    }

    private static Vector2 CocoonSampleCenter(int index) =>
        new Vector2(53 + 75 + index % CocoonsPerRow * 175 + 80, 112 + 30 + index / CocoonsPerRow * 152 + 51);

    private void DrawMangzhongChallenge()
    {
        if (showBatches)
        {
            DrawSummerBatches();
            return;
        }

        Layer("Summer_CocoonBench");
        WorkshopUi.Fill(art, "Summer Worktop Shade", ArtBounds, new Color32(30, 32, 24, 81));
        if (GameComplete)
        {
            DrawInspectionResult();
            return;
        }
        inspectionGesture = null;
        inspectionGestureStep = -1;
        inspectionDragging = false;

        var top = WorkshopUi.PixelCard(art, "Inspection Work Order", new Rect(53, 27, 1034, 70), SummerDark, SummerGold);
        WorkshopUi.Label(top, "Inspection Order Requirements", new Rect(17, 2, 660, 66),
            $"{ActiveInspection.Name}　圆茧 {ActiveInspection.RoundCount} 枚 · 长茧 {ActiveInspection.LongCount} 枚",
            29, WorkshopUi.Cream);
        cocoonBudgetText = WorkshopUi.Label(top, "Inspection Budget", new Rect(690, 2, 326, 66), "", 28, SummerGold,
            TextAnchor.MiddleRight);

        var tray = SummerArtworkPanel("Cocoon Inspection Tray", "CocoonTray", new Rect(53, 112, 616, 497), SummerNeutral);
        Sprite initialSprite = Resources.Load<Sprite>("WorkshopObservation/Summer_CocoonIntact");
        for (int i = 0; i < InspectionCocoonCount; i++)
        {
            int index = i;
            int row = i / CocoonsPerRow;
            float x = 75 + i % CocoonsPerRow * 175;
            float y = 30 + row * 152;
            if (i % CocoonsPerRow == 0)
                WorkshopUi.Label(tray, "Cocoon Row " + row, new Rect(13, y + 27, 53, 55),
                    (row + 1) + "排", 23, SummerDark, TextAnchor.MiddleCenter);
            var tile = WorkshopUi.Button(tray, "Cocoon Sample " + (i + 1), new Rect(x, y, 161, 134), "",
                () => SelectInspectionCocoon(index), CocoonTilePaper);
            RegisterButton(tile);
            cocoonTileFaces[i] = (Image)tile.targetGraphic;
            WorkshopUi.Label(tile.transform, "Cocoon Identifier", new Rect(9, 4, 41, 26), (i + 1).ToString(), 18, SummerDark);
            cocoonTileImages[i] = WorkshopUi.Picture(tile.transform, "Sample Sprite", new Rect(17, 12, 126, 80),
                initialSprite, stretch: ActiveInspection.Samples[i].Long);
            cocoonTileClues[i] = WorkshopUi.Picture(tile.transform, "Shell Condition Symbol", new Rect(116, 84, 31, 32), null);
            var sample = ActiveInspection.Samples[i];
            bool stillOnMount = sample.Condition == CocoonEvidence.Immature;
            WorkshopUi.Label(tile.transform, "Cocoon Mount Date", new Rect(76, 4, 79, 26),
                $"蔟龄 {sample.MountDays} 日", 17, stillOnMount ? SummerWarning : SummerDark, TextAnchor.MiddleRight);
            cocoonTileLabels[i] = WorkshopUi.Label(tile.transform, "Cocoon Evidence", new Rect(4, 89, 152, 42), "", 21,
                SummerDark, TextAnchor.MiddleCenter);
            var turn = WorkshopUi.Button(tile.transform, "Turn Sample In Place", new Rect(93, 38, 62, 52), "",
                TurnSelectedCocoon, SummerDark);
            WorkshopUi.Picture(turn.transform, "Turned Shell Silhouette", new Rect(11, 9, 40, 30), initialSprite)
                .rectTransform.localEulerAngles = new Vector3(0, 0, -33);
            var firstTurnArrow = WorkshopUi.Fill(turn.transform, "Turn Arrow Top", new Rect(34, 4, 20, 4), SummerGold);
            firstTurnArrow.rectTransform.localEulerAngles = new Vector3(0, 0, -30);
            var secondTurnArrow = WorkshopUi.Fill(turn.transform, "Turn Arrow Bottom", new Rect(34, 4, 15, 4), SummerGold);
            secondTurnArrow.rectTransform.localEulerAngles = new Vector3(0, 0, 43);
            cocoonTurnTargets[i] = (RectTransform)turn.transform;
            cocoonTurnTargets[i].gameObject.SetActive(false);
            var gestures = tile.gameObject.AddComponent<EventTrigger>();
            SummerPointer(gestures, EventTriggerType.BeginDrag, pointer => StartInspectionDrag(index, pointer));
            SummerPointer(gestures, EventTriggerType.Drag, MoveInspectionDrag);
            SummerPointer(gestures, EventTriggerType.EndDrag, EndInspectionDrag);
        }

        cocoonDeliveryText = DrawInspectionTray(CocoonDeliveryArea, "交货盘", "Cocoon Delivery Tray", CocoonTray.Delivery, deliveryCocoons);
        cocoonReviewText = DrawInspectionTray(CocoonReviewArea, "复查盘", "Cocoon Review Tray", CocoonTray.Review, reviewCocoons);
        var mount = WorkshopUi.PixelButton(art, "Route Cocoon To Mount", CocoonMountArea, "",
            () => RouteInspection(CocoonTray.Mount), false, 23);
        cocoonMountButton = mount;
        cocoonMountText = mount.GetComponentInChildren<Text>();
        WorkshopUi.Picture(mount.transform, "Mount Branch", new Rect(11, 4, 80, 35), SpringSummerArt("MulberryBranch"));
        mountedCocoon = WorkshopUi.Picture(mount.transform, "Mount Young Cocoon Slot", new Rect(19, 6, 48, 32),
            initialSprite, new Color32(183, 192, 109, 154), stretch: true);
        RegisterButton(mount);
        var ship = WorkshopUi.PixelButton(art, "Submit Inspection Order", new Rect(700, 535, 385, 58),
            "交货验收", SubmitInspectionOrder, true, 31, SummerJade);
        cocoonSubmitButton = ship;
        WorkshopUi.Picture(ship.transform, "Order Seal", new Rect(24, 4, 51, 47), SpringSummerArt("SuccessSeal"));
        RegisterButton(ship);

        cocoonDragChip = WorkshopUi.Rect(art, "Cocoon In Hand", new Rect(0, 0, 125, 105));
        cocoonDragSprite = WorkshopUi.Picture(cocoonDragChip, "Dragged Cocoon Shell", new Rect(0, 0, 120, 74), initialSprite);
        var dragCaption = WorkshopUi.PixelCard(cocoonDragChip, "Cocoon In Hand Caption", new Rect(6, 74, 112, 31), SummerDark, SummerGold);
        cocoonDragLabel = WorkshopUi.Label(dragCaption, "Selected Cocoon Drag Label", new Rect(3, 0, 106, 30), "", 18,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        cocoonDragChip.gameObject.SetActive(false);

        Copy("芒种", "足日且干爽才能交货。\n每排有一枚问题茧。");
        cocoonSelectedText = WorkshopUi.Label(infoPanel, "Selected Cocoon Detail", new Rect(27, 198, 444, 60), "", 25, SummerDark);
        cocoonNote = WorkshopUi.Label(infoPanel, "Inspection Feedback", new Rect(27, 258, 444, 48), "", 22, ErrorInk);
        Act("Turn Over Selected Cocoon", "翻面检查", TurnSelectedCocoon, true);
        cocoonFlipButton = infoPanel.Find("Turn Over Selected Cocoon").GetComponentInChildren<Button>();
        Act("Put Selected Cocoon Back", "放回待检盘", () => RouteInspection(CocoonTray.Pending));
        Act("Reset Cocoon Inspection", "重开", () =>
        {
            ResetInspection();
            RefreshInspection("翻面机会已复原。先看同排线索。");
        });
        var supply = WorkshopProgress.IsComplete(WorkshopSeason.Spring)
            ? "从春蚕养护记录接过来，先看蔟龄。" : "工坊备了春茧，先看上蔟记录。";
        RefreshInspection(supply + " 点左边一枚茧开始。");
    }

    private Text DrawInspectionTray(Rect bounds, string heading, string name, CocoonTray destination, Image[] slots)
    {
        var tray = SummerArtworkPanel(name, "CocoonBin", bounds, SummerNeutral);
        var label = WorkshopUi.PixelCard(tray, "Tray Label Paper", new Rect(39, 8, bounds.width - 78, 64), LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(label, "Tray Heading", new Rect(5, 2, bounds.width - 88, 31), heading, 26, SummerDark,
            TextAnchor.MiddleCenter);
        var count = WorkshopUi.Label(label, "Tray Contents", new Rect(5, 30, bounds.width - 88, 30), "", 23,
            SummerDark, TextAnchor.MiddleCenter);
        bool delivery = destination == CocoonTray.Delivery;
        Sprite clean = SpringSprite("WorkshopObservation/Summer_CocoonIntact");
        Sprite cracked = SpringSprite("WorkshopObservation/Summer_CocoonReview");
        WorkshopUi.Picture(label, "Matching Cocoon Condition", new Rect(3, 8, 51, 43), delivery ? clean : cracked);
        if (!delivery)
            WorkshopUi.Picture(label, "Review Moisture Sample", new Rect(bounds.width - 138, 17, 24, 28),
                SpringSummerArt("DewBead"));
        int totalSlots = delivery ? ActiveInspection.RoundCount + ActiveInspection.LongCount : slots.Length;
        for (int i = 0; i < slots.Length; i++)
        {
            float x = (bounds.width - totalSlots * 46) * 0.5f + i * 46;
            if (i < totalSlots)
            {
                bool longShape = delivery && i >= ActiveInspection.RoundCount;
                var empty = WorkshopUi.Picture(tray, "Cocoon Shaped Space " + i,
                    new Rect(x + (longShape ? 0 : 6), 72, longShape ? 41 : 29, 34), clean,
                    new Color32(133, 112, 77, 122), stretch: true);
                empty.preserveAspect = false;
            }
            slots[i] = WorkshopUi.Picture(tray, "Stored Cocoon " + i, new Rect(x, 72, 41, 34), null, stretch: true);
        }
        var route = WorkshopUi.PixelButton(tray, "Route Cocoon To " + destination, new Rect(28, 111, bounds.width - 56, 43),
            destination == CocoonTray.Delivery ? "干爽好茧放这里" : "破口、过潮放这里", () => RouteInspection(destination), false, 23);
        cocoonRouteButtons[destination == CocoonTray.Delivery ? 0 : 1] = route;
        RegisterButton(route);
        // The whole illustrated tray is a target, including its heading and empty slots.
        var hit = WorkshopUi.Button(tray, "Whole Tray Target", new Rect(0, 0, bounds.width, 108), "",
            () => RouteInspection(destination), Color.clear);
        hit.transform.SetAsLastSibling();
        return count;
    }

    private RectTransform SummerArtworkPanel(string name, string graphic, Rect bounds, Color fallback)
    {
        var sprite = SpringSummerArt(graphic);
        if (sprite == null) return WorkshopUi.PixelCard(art, name, bounds, fallback, WorkshopUi.Bronze);
        var panel = WorkshopUi.Rect(art, name, bounds);
        WorkshopUi.Picture(panel, name + " Artwork", new Rect(0, 0, bounds.width, bounds.height), sprite, stretch: true);
        return panel;
    }

    private void DrawInspectionResult()
    {
        var result = SummerArtworkPanel("Completed Cocoon Delivery", "CocoonBin", new Rect(239, 115, 682, 386), SummerNeutral);
        WorkshopUi.Label(result, "Cocoon Order Finished", new Rect(36, 30, 610, 56), "合格春茧已干存", 42, SummerDark,
            TextAnchor.MiddleCenter);
        Sprite cocoon = Resources.Load<Sprite>("WorkshopObservation/Summer_CocoonIntact");
        int position = 0;
        for (int i = 0; i < cocoonTrays.Length; i++)
        {
            if (cocoonTrays[i] != CocoonTray.Delivery) continue;
            float left = 53 + position++ * 96;
            float width = ActiveInspection.Samples[i].Long ? 67f : 83f;
            WorkshopUi.Picture(result, "Delivered Cocoon " + i, new Rect(left, 103, width, 126), cocoon, stretch: true);
            WorkshopUi.Label(result, "Delivered Cocoon Number " + i, new Rect(left, 227, width, 35),
                $"{i + 1} 号", 22, SummerDark, TextAnchor.MiddleCenter);
        }
        WorkshopUi.Label(result, "Inspection Result", new Rect(25, 271, 632, 62), InspectionResult(), 28, SummerDark,
            TextAnchor.MiddleCenter);
        Copy("这批好茧交给夏至", "茧足日才能收。合格茧已干存，夏至重新煮茧取丝。未熟茧继续留蔟，问题茧单独复查。");
        Act("Compare Nanxun Batches", "看南浔的两批蚕", ToggleSummerBatches, true);
    }

    private string InspectionResult() =>
        $"交货 {ActiveInspection.RoundCount + ActiveInspection.LongCount} 枚 · 留蔟 1 枚 · 翻面 {inspectedCocoonCount}/{InspectionBudget} 次";

    private string CaptureDeliveredCocoons()
    {
        var slots = new char[InspectionCocoonCount];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = '0';
            if (cocoonTrays[i] == CocoonTray.Delivery)
                slots[i] = ActiveInspection.Samples[i].Long ? LongCocoonInk : RoundCocoonInk;
        }
        return new string(slots);
    }

    private static WorkshopProgress.ChallengeWork WorkshopUpstreamWork(WorkshopObservation previous, int order)
    {
        var delivered = OrderWork(previous);
        if (!string.IsNullOrEmpty(delivered?.sample)) return delivered;
        var matching = BestWork(previous, order);
        if (!string.IsNullOrEmpty(matching?.sample)) return matching;
        var alternate = BestWork(previous, 1 - order);
        return !string.IsNullOrEmpty(alternate?.sample) ? alternate : matching ?? alternate;
    }

    private CocoonEvidence InspectEvidence(int index)
    {
        var samples = ActiveInspection.Samples;
        if (!samples[index].Covered || cocoonTurned[index])
            return samples[index].Condition;

        int rowStart = index / CocoonsPerRow * CocoonsPerRow;
        int hiddenCount = 0;
        for (int i = rowStart; i < rowStart + CocoonsPerRow; i++)
        {
            if (samples[i].Covered && !cocoonTurned[i]) hiddenCount++;
            else if (samples[i].Condition != CocoonEvidence.Intact) return CocoonEvidence.Intact;
        }
        return hiddenCount == 1 ? CocoonEvidence.NeedsReview : CocoonEvidence.Hidden;
    }

    private static string CocoonEvidenceLabel(CocoonEvidence evidence)
    {
        switch (evidence)
        {
            case CocoonEvidence.Intact: return "完好干爽";
            case CocoonEvidence.Cracked: return "有破口";
            case CocoonEvidence.Damp: return "过潮";
            case CocoonEvidence.Immature: return "未熟";
            case CocoonEvidence.NeedsReview: return "应复查";
            default: return "待翻面";
        }
    }

    private static CocoonTray ExpectedCocoonTray(CocoonEvidence evidence)
    {
        switch (evidence)
        {
            case CocoonEvidence.Intact: return CocoonTray.Delivery;
            case CocoonEvidence.Immature: return CocoonTray.Mount;
            case CocoonEvidence.Hidden: return CocoonTray.Pending;
            default: return CocoonTray.Review;
        }
    }

    private void RefreshInspection(string feedback)
    {
        Sprite intact = Resources.Load<Sprite>("WorkshopObservation/Summer_CocoonIntact");
        Sprite cracked = Resources.Load<Sprite>("WorkshopObservation/Summer_CocoonReview");
        int round = 0;
        int longCount = 0;
        int review = 0;
        int stayOnMount = 0;
        foreach (Image icon in deliveryCocoons) icon.enabled = false;
        foreach (Image icon in reviewCocoons) icon.enabled = false;
        for (int i = 0; i < InspectionCocoonCount; i++)
        {
            CocoonSample cocoon = ActiveInspection.Samples[i];
            CocoonEvidence evidence = InspectEvidence(i);
            cocoonTileImages[i].sprite = evidence == CocoonEvidence.Cracked ? cracked : intact;
            Image state = cocoonTileClues[i];
            state.sprite = cocoonTrays[i] != CocoonTray.Pending ? SpringSummerArt("SuccessSeal") :
                evidence == CocoonEvidence.Damp ? SpringSummerArt("DewBead") :
                evidence == CocoonEvidence.Immature ? SpringSummerArt("MulberryBranch") :
                evidence == CocoonEvidence.NeedsReview ? SpringSummerArt("RecoverySeal") : null;
            state.enabled = state.sprite != null;
            bool showTurn = selectedCocoon == i && evidence == CocoonEvidence.Hidden && inspectedCocoonCount < InspectionBudget;
            cocoonTurnTargets[i].gameObject.SetActive(showTurn);
            switch (evidence)
            {
                case CocoonEvidence.Damp: cocoonTileImages[i].color = new Color32(145, 187, 191, 255); break;
                case CocoonEvidence.Hidden: cocoonTileImages[i].color = new Color32(201, 190, 157, 255); break;
                case CocoonEvidence.Immature: cocoonTileImages[i].color = new Color32(229, 227, 171, 255); break;
                default: cocoonTileImages[i].color = Color.white; break;
            }
            cocoonTileFaces[i].color = selectedCocoon == i ? SummerGold :
                cocoonTrays[i] == CocoonTray.Delivery ? new Color32(179, 223, 181, 255) :
                cocoonTrays[i] == CocoonTray.Review ? new Color32(240, 184, 156, 255) : CocoonTilePaper;
            string destination = cocoonTrays[i] == CocoonTray.Delivery ? "交货" :
                cocoonTrays[i] == CocoonTray.Review ? "复查" :
                cocoonTrays[i] == CocoonTray.Mount ? "留蔟" : CocoonEvidenceLabel(evidence);
            cocoonTileLabels[i].text = $"{(cocoon.Long ? "长茧" : "圆茧")} · {destination}";
            if (cocoonTrays[i] == CocoonTray.Delivery)
            {
                ShowStoredCocoon(deliveryCocoons, cocoon.Long ? ActiveInspection.RoundCount + longCount : round,
                    cocoon, cocoonTileImages[i]);
                if (cocoon.Long) longCount++;
                else round++;
            }
            if (cocoonTrays[i] == CocoonTray.Review)
                ShowStoredCocoon(reviewCocoons, review++, cocoon, cocoonTileImages[i]);
            if (cocoonTrays[i] == CocoonTray.Mount) stayOnMount++;
        }
        cocoonDeliveryText.text = $"圆 {round}/{ActiveInspection.RoundCount}　长 {longCount}/{ActiveInspection.LongCount}";
        cocoonReviewText.text = $"{review}/{reviewCocoons.Length}";
        cocoonMountText.text = $"留蔟 · {stayOnMount}/1";
        mountedCocoon.color = stayOnMount > 0 ? new Color32(221, 236, 153, 255) : new Color32(183, 192, 109, 154);
        bool ready = InspectionReady();
        int arranged = round + longCount + review + stayOnMount;
        cocoonBudgetText.text = $"分好 {arranged}/{ActiveInspection.RoundCount + ActiveInspection.LongCount + 3} · 翻面余 {InspectionBudget - inspectedCocoonCount}";
        string selected = ready ? "已配齐，点右侧「交货验收」" : "① 点左边一枚茧";
        if (selectedCocoon >= 0)
        {
            CocoonEvidence evidence = InspectEvidence(selectedCocoon);
            string known = evidence == CocoonEvidence.Hidden ? "背面被遮" : CocoonEvidenceLabel(evidence);
            selected = $"{selectedCocoon + 1} 号 · {known}\n" + (evidence == CocoonEvidence.Hidden
                ? "② 点「翻面检查」看背面"
                : evidence == CocoonEvidence.Intact ? "② 点右侧交货盘"
                : evidence == CocoonEvidence.Immature ? "② 点右侧「未熟留蔟」"
                : "② 点右侧复查盘");
        }
        cocoonSelectedText.text = selected;
        cocoonNote.text = feedback;
        bool needsFlip = selectedCocoon >= 0 && InspectEvidence(selectedCocoon) == CocoonEvidence.Hidden;
        cocoonFlipButton.interactable = needsFlip && inspectedCocoonCount < InspectionBudget;
        cocoonFlipButton.transform.parent.gameObject.SetActive(needsFlip);
        cocoonFlipButton.GetComponentInChildren<Text>().text = selectedCocoon < 0 ? "先点左边一枚茧" :
            needsFlip ? "翻面检查 · 看背面" : "茧况已明，无需翻面";
        cocoonSubmitButton.interactable = ready;
        cocoonSubmitButton.transform.parent.gameObject.SetActive(ready);
        CocoonTray expected = selectedCocoon < 0 ? CocoonTray.Pending : ExpectedCocoonTray(InspectEvidence(selectedCocoon));
        bool firstPlacement = ChallengeVariant == 0 && arranged == 0;
        SetSummerAction(cocoonRouteButtons[0], firstPlacement && expected == CocoonTray.Delivery);
        SetSummerAction(cocoonRouteButtons[1], firstPlacement && expected == CocoonTray.Review);
        SetSummerAction(cocoonMountButton, firstPlacement && expected == CocoonTray.Mount);
        SetSummerAction(cocoonSubmitButton, ready);
        RefreshInspectionGesture(ready, firstPlacement, needsFlip && inspectedCocoonCount < InspectionBudget);
    }

    private void RefreshInspectionGesture(bool ready, bool firstPlacement, bool needsFlip)
    {
        int next = -1;
        if (!inspectionDragging)
        {
            if (ready) next = 0;
            else if (needsFlip) next = 10 + selectedCocoon;
            else if (firstPlacement && selectedCocoon < 0) next = 1;
            else if (firstPlacement) next = 20 + selectedCocoon;
        }
        if (next == inspectionGestureStep) return;
        if (inspectionGesture != null)
        {
            inspectionGesture.gameObject.SetActive(false);
            Destroy(inspectionGesture.gameObject);
        }
        inspectionGestureStep = next;
        if (next == 0)
        {
            var seal = new Vector2(750, 565);
            inspectionGesture = ShowGesture(art, seal, seal, WorkGesture.Tap);
        }
        else if (next == 1)
            inspectionGesture = ShowSummerCarryCue(art, CocoonSampleCenter(0), new Vector2(805, 205),
                SpringSprite("WorkshopObservation/Summer_CocoonIntact"));
        else if (next >= 20)
        {
            CocoonTray tray = ExpectedCocoonTray(InspectEvidence(selectedCocoon));
            Vector2 target = tray == CocoonTray.Delivery ? CocoonDeliveryArea.center :
                tray == CocoonTray.Review ? CocoonReviewArea.center : CocoonMountArea.center;
            inspectionGesture = ShowGesture(art, target, target, WorkGesture.Tap);
        }
        else if (next >= 10)
        {
            Vector2 turn = CocoonSampleCenter(selectedCocoon) + new Vector2(43, 12);
            inspectionGesture = ShowGesture(art, turn, turn, WorkGesture.Tap);
        }
    }

    private RectTransform ShowSummerCarryCue(RectTransform parent, Vector2 from, Vector2 to, Sprite material)
    {
        var cue = ShowGesture(parent, from, to, WorkGesture.Drag);
        var cursor = cue.Find("Mouse Pointer");
        if (cursor != null)
        {
            var carried = WorkshopUi.Picture(cursor, "Example Carried Material", new Rect(-57, -47, 63, 54), material,
                new Color32(255, 252, 222, 176));
            carried.transform.SetAsFirstSibling();
        }
        return cue;
    }

    private bool InspectionReady()
    {
        int round = 0, elongated = 0, review = 0, mount = 0;
        for (int i = 0; i < cocoonTrays.Length; i++)
        {
            if (cocoonTrays[i] == CocoonTray.Pending) continue;
            if (cocoonTrays[i] != ExpectedCocoonTray(InspectEvidence(i))) return false;
            if (cocoonTrays[i] == CocoonTray.Delivery)
            {
                if (ActiveInspection.Samples[i].Long) elongated++;
                else round++;
            }
            else if (cocoonTrays[i] == CocoonTray.Review) review++;
            else if (cocoonTrays[i] == CocoonTray.Mount) mount++;
        }
        return round == ActiveInspection.RoundCount && elongated == ActiveInspection.LongCount && review == 2 && mount == 1;
    }

    private static void SetSummerAction(Button button, bool current)
    {
        if (button == null) return;
        button.targetGraphic.color = current ? SummerGold : WorkshopUi.Paper;
        var label = button.GetComponentInChildren<Text>();
        if (label != null) label.color = SummerDark;
    }

    private static void ShowStoredCocoon(Image[] slots, int index, CocoonSample cocoon, Image source)
    {
        if (index >= slots.Length) return;
        Image icon = slots[index];
        icon.enabled = true;
        icon.sprite = source.sprite;
        icon.color = source.color;
        icon.rectTransform.sizeDelta = new Vector2(cocoon.Long ? 41f : 29f, 34f);
    }

    private void SelectInspectionCocoon(int index)
    {
        selectedCocoon = index;
        WorkshopAudioFeedback.Click();
        RefreshInspection(InspectEvidence(index) == CocoonEvidence.Hidden ? "也可以先翻同排另一枚。" : "已确认茧况，可安排去向。");
    }

    private void TurnSelectedCocoon()
    {
        if (selectedCocoon < 0)
        {
            RefreshInspection("先在左侧选一枚茧。");
            return;
        }

        if (InspectEvidence(selectedCocoon) != CocoonEvidence.Hidden)
        {
            RefreshInspection("从外观和同排线索已经能确认，翻面次数保留。");
            return;
        }

        if (inspectedCocoonCount >= InspectionBudget)
        {
            RefreshInspection("翻面机会用完，可依据线索或重开。");
            return;
        }

        cocoonTurned[selectedCocoon] = true;
        inspectedCocoonCount++;
        WorkshopAudioFeedback.Notice();
        var condition = ActiveInspection.Samples[selectedCocoon].Condition;
        RefreshInspection(condition == CocoonEvidence.Intact ? "茧壳完好干爽，再核对同排。"
            : "发现" + CocoonEvidenceLabel(condition) + "，同排其余茧可交。");
        StartCoroutine(RevealCocoonBack(cocoonTileImages[selectedCocoon]));
    }

    private static IEnumerator RevealCocoonBack(Image picture)
    {
        var edge = picture.rectTransform;
        for (float elapsed = 0f; elapsed < .23f; elapsed += Time.unscaledDeltaTime)
        {
            if (edge == null || !edge.gameObject.activeInHierarchy) yield break;
            edge.localScale = new Vector3(Mathf.SmoothStep(.13f, 1f, elapsed / .23f), 1f, 1f);
            yield return null;
        }
        if (edge != null) edge.localScale = Vector3.one;
    }

    private void RouteInspection(CocoonTray destination)
    {
        if (selectedCocoon < 0)
        {
            RefreshInspection("先在左侧选一枚茧。");
            return;
        }

        var evidence = InspectEvidence(selectedCocoon);
        if (destination != CocoonTray.Pending && (evidence == CocoonEvidence.Hidden || destination != ExpectedCocoonTray(evidence)))
        {
            WorkshopAudioFeedback.Notice();
            RefreshInspection(evidence == CocoonEvidence.Hidden ? "背面没看清，先翻面再分。" :
                evidence == CocoonEvidence.Immature ? "这枚只有三日，留在蔟上继续成熟。" :
                evidence == CocoonEvidence.Intact ? "这枚干爽完好，放交货盘。" : "问题茧放复查盘，和好茧分开。");
            AnimateSpringSummerSprite(cocoonTileImages[selectedCocoon], new Vector2(9, 0), 0.95f, 0.15f);
            DrawCocoonRejection(selectedCocoon);
            return;
        }
        if (destination == CocoonTray.Delivery && cocoonTrays[selectedCocoon] != CocoonTray.Delivery)
        {
            bool longShape = ActiveInspection.Samples[selectedCocoon].Long;
            int count = 0;
            for (int i = 0; i < cocoonTrays.Length; i++)
                if (cocoonTrays[i] == CocoonTray.Delivery && ActiveInspection.Samples[i].Long == longShape) count++;
            if (count >= (longShape ? ActiveInspection.LongCount : ActiveInspection.RoundCount))
            {
                WorkshopAudioFeedback.Notice();
                RefreshInspection((longShape ? "长茧" : "圆茧") + "已够，余下好茧留作备用。");
                DrawCocoonRejection(selectedCocoon);
                return;
            }
        }
        int placed = selectedCocoon;
        if (cocoonTrays[placed] == destination)
        {
            RefreshInspection(destination == CocoonTray.Pending ? "这枚茧还在原盘。" : "这枚茧已经分好。");
            return;
        }
        cocoonTrays[placed] = destination;
        WorkshopAudioFeedback.Place();
        if (destination != CocoonTray.Pending) selectedCocoon = -1;
        RefreshInspection(destination == CocoonTray.Pending ? "已放回待检盘。" : $"{placed + 1} 号分好了！继续点下一枚。");
        if (destination != CocoonTray.Pending)
        {
            PlayCocoonLanding(placed, destination);
            if (InspectionReady()) WorkshopAudioFeedback.Collect();
        }
    }

    private void PlayCocoonLanding(int placed, CocoonTray tray)
    {
        bool longShape = ActiveInspection.Samples[placed].Long;
        int slot = tray == CocoonTray.Delivery && longShape ? ActiveInspection.RoundCount : 0;
        for (int i = 0; i < placed; i++)
        {
            if (cocoonTrays[i] != tray) continue;
            if (tray != CocoonTray.Delivery || ActiveInspection.Samples[i].Long == longShape) slot++;
        }
        Image receiver = tray == CocoonTray.Mount ? mountedCocoon :
            tray == CocoonTray.Delivery ? deliveryCocoons[slot] : reviewCocoons[slot];
        Vector2 source = CocoonSampleCenter(placed);
        Vector2 target = WorkbenchPoint(art, receiver.rectTransform);
        Image sample = cocoonTileImages[placed];
        var flight = WorkshopUi.Picture(art, "Cocoon Sliding Into Its Slot", new Rect(source.x, source.y, 66, 50),
            sample.sprite, sample.color, stretch: longShape);
        flight.rectTransform.pivot = new Vector2(.5f, .5f);
        StartCoroutine(AnimateCocoonLanding(flight.rectTransform, source, target));
    }

    private static IEnumerator AnimateCocoonLanding(RectTransform flight, Vector2 source, Vector2 target)
    {
        for (float elapsed = 0f; elapsed < .24f; elapsed += Time.unscaledDeltaTime)
        {
            if (flight == null || !flight.gameObject.activeInHierarchy) yield break;
            float t = elapsed / .24f;
            Vector2 point = Vector2.Lerp(source, target, Mathf.SmoothStep(0f, 1f, t));
            point.y -= Mathf.Sin(t * Mathf.PI) * 24f;
            flight.anchoredPosition = new Vector2(point.x, -point.y);
            flight.localScale = Vector3.one * Mathf.Lerp(1f, .47f, t);
            yield return null;
        }
        if (flight != null) Destroy(flight.gameObject);
    }

    private void DrawCocoonRejection(int index)
    {
        Vector2 at = CocoonSampleCenter(index) + new Vector2(-64, -38);
        var stamp = WorkshopUi.PixelCard(art, "Wrong Tray Mark", new Rect(at.x, at.y, 49, 49), SummerWarning, SummerGold);
        DrawCueStroke(stamp, new Vector2(14, 14), new Vector2(35, 35), 5, WorkshopUi.Cream);
        DrawCueStroke(stamp, new Vector2(35, 14), new Vector2(14, 35), 5, WorkshopUi.Cream);
        StartCoroutine(ClearCocoonRejection(stamp));
    }

    private static IEnumerator ClearCocoonRejection(RectTransform stamp)
    {
        yield return new WaitForSecondsRealtime(.7f);
        if (stamp != null) Destroy(stamp.gameObject);
    }

    private void SubmitInspectionOrder()
    {
        int round = 0;
        int longCount = 0;
        int review = 0;
        int stayOnMount = 0;
        for (int i = 0; i < InspectionCocoonCount; i++)
        {
            CocoonEvidence evidence = InspectEvidence(i);
            if (cocoonTrays[i] == CocoonTray.Pending) continue;
            if (evidence == CocoonEvidence.Hidden)
            {
                selectedCocoon = i;
                RefreshInspection($"{i + 1} 号背面还未确认，先检查或换回。");
                return;
            }
            if (cocoonTrays[i] != ExpectedCocoonTray(evidence))
            {
                selectedCocoon = i;
                RefreshInspection($"{i + 1} 号放错盘 · {CocoonEvidenceLabel(evidence)}。");
                return;
            }
            if (cocoonTrays[i] == CocoonTray.Review) review++;
            else if (cocoonTrays[i] == CocoonTray.Mount) stayOnMount++;
            else if (ActiveInspection.Samples[i].Long) longCount++;
            else round++;
        }

        if (round != ActiveInspection.RoundCount || longCount != ActiveInspection.LongCount ||
            review != reviewCocoons.Length || stayOnMount != 1)
        {
            RefreshInspection("配齐交货茧、两枚复查和一枚留蔟。");
            return;
        }

        WorkshopAudioFeedback.Collect();
        FinishChallenge(100 + InspectionBudget - inspectedCocoonCount, InspectionResult(), CaptureDeliveredCocoons());
    }

    private void StartInspectionDrag(int index, PointerEventData pointer)
    {
        pointer.eligibleForClick = false;
        selectedCocoon = index;
        inspectionDragging = true;
        RefreshInspection("把这枚茧拖到右边的盘。");
        Image sample = cocoonTileImages[index];
        cocoonDragSprite.sprite = sample.sprite;
        cocoonDragSprite.color = sample.color;
        cocoonDragSprite.preserveAspect = sample.preserveAspect;
        cocoonDragLabel.text = $"{index + 1} 号{(ActiveInspection.Samples[index].Long ? "长茧" : "圆茧")}";
        cocoonDragChip.gameObject.SetActive(true);
        MoveInspectionDrag(pointer);
    }

    private void MoveInspectionDrag(PointerEventData pointer)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera,
                out Vector2 point))
            cocoonDragChip.anchoredPosition = point + new Vector2(-60, 50);
    }

    private void EndInspectionDrag(PointerEventData pointer)
    {
        inspectionDragging = false;
        cocoonDragChip.gameObject.SetActive(false);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera,
                out Vector2 point)) return;
        point.y = -point.y;
        if (CocoonDeliveryArea.Contains(point)) RouteInspection(CocoonTray.Delivery);
        else if (CocoonReviewArea.Contains(point)) RouteInspection(CocoonTray.Review);
        else if (CocoonMountArea.Contains(point)) RouteInspection(CocoonTray.Mount);
        else RefreshInspection("先把茧放进一个盘。");
    }

    private static void SummerPointer(EventTrigger trigger, EventTriggerType eventType, Action<PointerEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(data =>
        {
            if (data is PointerEventData pointer && pointer.button == PointerEventData.InputButton.Left) callback(pointer);
        });
        trigger.triggers.Add(entry);
    }

    private void DrawDashuChallenge()
    {
        Layer("Summer_CocoonBench");
        if (!DashuRainy)
            Art("Xiazhi Open Window", "WorkshopObservation/Summer_OpenWindow", ArtBounds, true);
        WorkshopUi.Fill(art, "Dashu Workbench Shade", ArtBounds, new Color32(29, 35, 25, 86));
        if (GameComplete)
        {
            var finished = WorkshopUi.PixelCard(art, "Wound Silk", new Rect(300, 126, 560, 393), SummerNeutral, SummerGold);
            WorkshopUi.Picture(finished, "Hand Reeled Silk", new Rect(145, 54, 270, 228),
                SpringSummerArt("SilkBobbin") ?? Resources.Load<Sprite>("WorkshopFinish/RawSilk"));
            WorkshopUi.Label(finished, "Silk Work Result", new Rect(20, 276, 520, 78), DashuWorkResult(), 33,
                SummerDark, TextAnchor.MiddleCenter);
            Copy("干妥的生丝送去秋分", "干茧经复煮、索绪、添绪和绕轴，最后干丝入库。秋分会拿这一轴验丝。");
            return;
        }

        if (dashuDrying)
        {
            DrawDashuDrying();
            return;
        }

        dashuGuide = null;
        dashuGuideStep = -1;
        var workshop = WorkshopUi.PixelCard(art, "Dashu Reel Closeup", new Rect(65, 26, 1030, 352), SummerNeutral, SummerGold);
        var illustration = new Rect(82, 79, 996, 259);
        DrawDashuReelingTools(illustration);
        dashuThread = WorkshopUi.Picture(art, "Active Silk Thread", illustration,
            SpringSprite("WorkshopObservation/Summer_ReelingThreadPath"), stretch: true);
        var threadPivot = new Vector2(0.62f, 0.42f);
        dashuThread.rectTransform.pivot = threadPivot;
        dashuThread.rectTransform.anchoredPosition = new Vector2(illustration.x + illustration.width * threadPivot.x,
            -illustration.y - illustration.height * (1f - threadPivot.y));
        DrawDashuDirectActions(illustration);
        DrawDashuCocoonSupply(workshop);
        dashuSupplyText = WorkshopUi.Label(workshop, "Current Silk Lead", new Rect(687, 4, 323, 44), "", 26, SummerDark,
            TextAnchor.MiddleRight);
        for (int i = 0; i < dashuReserveCells.Length; i++)
            dashuReserveCells[i] = WorkshopUi.Fill(workshop, "Thread Reserve " + (i + 1),
                new Rect(698 + 25f * i, 47, 21, 13), SummerJade);
        dashuSpliceThreshold = WorkshopUi.Fill(workshop, "Spare Thread Threshold",
            new Rect(698, 43, 3, 21), SummerGold).rectTransform;
        dashuNoteText = WorkshopUi.Label(workshop, "Dashu Live Status", new Rect(18, 308, 995, 40), "", 26, SummerDark,
            TextAnchor.MiddleCenter);

        var gauge = SummerArtworkPanel("Dashu Tension Gauge", "ReelingGauge", new Rect(65, 396, 1030, 220), SummerDark);
        dashuGauge = gauge;
        dashuWrappedText = WorkshopUi.Label(gauge, "Dashu Wound Length", new Rect(19, 6, 991, 58), "", 30, WorkshopUi.Cream,
            TextAnchor.MiddleCenter);
        float trackX = 156;
        WorkshopUi.Fill(gauge, "Wound Length Track", new Rect(trackX, 61, DashuGaugeWidth, 16), WorkshopUi.Line);
        dashuLengthBar = WorkshopUi.Fill(gauge, "Wound Length Progress", new Rect(trackX, 61, 0, 16), SummerGold);
        for (int i = 0; i < dashuJoinTicks.Length; i++)
        {
            float x = trackX + (2f * i + 1f) / DashuLengthGoal * DashuGaugeWidth;
            var stitch = WorkshopUi.Rect(gauge, "Wound Silk Join " + (i + 1), new Rect(x, 64, 1, 1));
            DrawCueStroke(stitch, new Vector2(-7f, 0f), new Vector2(7f, 11f), 3f, SummerReady);
            DrawCueStroke(stitch, new Vector2(7f, 0f), new Vector2(-7f, 11f), 3f, SummerReady);
            dashuJoinTicks[i] = stitch;
        }
        WorkshopUi.Picture(gauge, "Wound Silk Goal", new Rect(trackX + DashuGaugeWidth + 15, 41, 76, 52), SpringSummerArt("SilkBobbin"));
        WorkshopUi.Fill(gauge, "Tension Track", new Rect(trackX, 91, DashuGaugeWidth, 42), SummerWarning);
        dashuSafeTrack = WorkshopUi.Fill(gauge, "Safe Tension", new Rect(trackX + DashuSafeStart * DashuGaugeWidth, 91,
            (DashuSafeEnd - DashuSafeStart) * DashuGaugeWidth, 42), SummerJade);
        for (int i = 0; i < 2; i++)
            WorkshopUi.Fill(gauge, "Safe Zone End " + i,
                new Rect(trackX + (i == 0 ? DashuSafeStart : DashuSafeEnd) * DashuGaugeWidth - 2, 91, 4, 42), SummerGold);
        dashuNeedle = WorkshopUi.Fill(gauge, "Dashu Tension Needle", new Rect(trackX - 6, 83, 12, 60), SummerGold);
        dashuGaugeReleaseMark = WorkshopUi.Rect(gauge, "Ease Toward Lower Tension", new Rect(trackX, 116, 1, 1));
        DrawCueStroke(dashuGaugeReleaseMark, new Vector2(-19, 0), new Vector2(-68, 0), 5, SummerGold);
        DrawCueStroke(dashuGaugeReleaseMark, new Vector2(-52, -10), new Vector2(-68, 0), 5, SummerGold);
        DrawCueStroke(dashuGaugeReleaseMark, new Vector2(-52, 10), new Vector2(-68, 0), 5, SummerGold);
        WorkshopUi.Label(gauge, "Low Tension Label", new Rect(12, 91, 130, 42), "松 · 打圈", 22, WorkshopUi.Cream,
            TextAnchor.MiddleRight);
        WorkshopUi.Label(gauge, "High Tension Label", new Rect(887, 91, 129, 42), "紧 · 易断", 22, WorkshopUi.Cream);
        dashuGaugeInputText = WorkshopUi.Label(gauge, "Safe Tension Instruction", new Rect(trackX, 134, DashuGaugeWidth, 28),
            "张力", 20, SummerGold, TextAnchor.MiddleCenter);
        WorkshopUi.Fill(gauge, "Strain Track", new Rect(trackX, 170, DashuGaugeWidth, 12), new Color32(78, 82, 68, 255));
        dashuStrainBar = WorkshopUi.Fill(gauge, "Accumulating Strain", new Rect(trackX, 170, 0, 12), SummerGold);
        WorkshopUi.Label(gauge, "Dashu Risk Label", new Rect(12, 153, 130, 42), "断丝风险", 22, WorkshopUi.Cream,
            TextAnchor.MiddleRight);

        Copy(DashuRainy ? "夏至｜梅雨" : "夏至｜雨歇", "张力平稳，丝才绕得匀。");
        dashuActionText = WorkshopUi.Label(infoPanel, "Dashu Next Action", new Rect(27, 213, 444, 86), "", 28,
            SummerDark, TextAnchor.UpperLeft);
        Act("Dashu Prepare Cocoons", "煮茧、找丝头", PrepareDashuCocoons, true);
        dashuPrepareButton = infoPanel.Find("Dashu Prepare Cocoons").GetComponentInChildren<Button>();
        dashuPrepareLabel = dashuPrepareButton.GetComponentInChildren<Text>();
        var windingBounds = new Rect(27, 338 + rows * 73f, 444, 62);
        var winding = WorkshopUi.PixelButton(infoPanel, "Hold Dashu Reel", windingBounds,
            "按住摇轮 · 空格", null, true, 29, SummerJade);
        dashuWindingButton = winding;
        RegisterButton(winding);
        var events = winding.gameObject.AddComponent<EventTrigger>();
        SummerPointer(events, EventTriggerType.PointerDown, _ =>
        {
            dashuHoldingPointer = true;
            if (dashuThreadAttached && dashuSpeed < 0.08f) WorkshopAudioFeedback.Click();
        });
        SummerPointer(events, EventTriggerType.PointerUp, _ => dashuHoldingPointer = false);
        SummerPointer(events, EventTriggerType.PointerExit, _ => dashuHoldingPointer = false);
        rows++;

        Act("Dashu Splice Silk", "", SpliceDashuSilk);
        dashuSpliceButton = infoPanel.Find("Dashu Splice Silk").GetComponentInChildren<Button>();
        dashuSpliceText = dashuSpliceButton.GetComponentInChildren<Text>();
        Act("Dashu Change Gear", "", () =>
        {
            dashuSlowGear = !dashuSlowGear;
            ShowDashuNote(dashuSlowGear ? "换到慢摇，丝轮更稳。" : "换到快摇，留意张力上升。", 1.4f);
        });
        dashuGearText = infoPanel.Find("Dashu Change Gear").GetComponentInChildren<Text>();
        dashuGearControl = (RectTransform)infoPanel.Find("Dashu Change Gear");
        RefreshDashuWorktop();
        StartCoroutine(RunDashuReel(workshop));
    }

    private void PrepareDashuCocoons()
    {
        if (dashuCocoonsPrepared) return;
        dashuCocoonsPrepared = dashuThreadAttached = true;
        WorkshopAudioFeedback.Place();
        ShowDashuNote("煮软后已找出丝头。按住摇轮，把它绕上轴。", 2.6f);
        RefreshDashuWorktop();
    }

    private void DrawDashuDirectActions(Rect illustration)
    {
        float potWidth = illustration.width / 3f;
        var pot = WorkshopUi.Button(art, "Tap the Cocoon Pot",
            new Rect(illustration.x + 5, illustration.y + 5, potWidth - 10, illustration.height - 10), "",
            PrepareDashuCocoons, Color.clear);
        dashuPotTarget = (RectTransform)pot.transform;
        VisualStep(dashuPotTarget);

        var wheel = WorkshopUi.Button(art, "Hold the Reeling Wheel",
            new Rect(illustration.x + potWidth * 2f + 23, illustration.y + 17, 237, 213), "", null, Color.clear);
        dashuWheelTarget = (RectTransform)wheel.transform;
        dashuReelHighlight = VisualStep(dashuWheelTarget);
        var wheelInput = wheel.gameObject.AddComponent<EventTrigger>();
        SummerPointer(wheelInput, EventTriggerType.PointerDown, _ => dashuHoldingPointer = true);
        SummerPointer(wheelInput, EventTriggerType.PointerUp, _ => dashuHoldingPointer = false);

        var splice = WorkshopUi.Button(art, "Tap the Spare Silk End",
            new Rect(illustration.x + potWidth + 60, illustration.y + 46, 211, 149), "", SpliceDashuSilk, SummerNeutral);
        dashuSpliceTarget = (RectTransform)splice.transform;
        WorkshopUi.Picture(dashuSpliceTarget, "Spare Silk Bobbin", new Rect(52, 10, 118, 91), SpringSummerArt("SilkBobbin"));
        WorkshopUi.Fill(dashuSpliceTarget, "Join Mark Horizontal", new Rect(83, 111, 45, 7), SummerJade);
        WorkshopUi.Fill(dashuSpliceTarget, "Join Mark Vertical", new Rect(102, 92, 7, 45), SummerJade);
        VisualStep(dashuSpliceTarget, SummerReady);

        var lift = WorkshopUi.PixelCard(art, "Release the Wheel Sign",
            new Rect(990, 88, 71, 73), SummerDark, SummerGold);
        DrawCueStroke(lift, new Vector2(35, 38), new Vector2(35, 13), 5, SummerGold);
        DrawCueStroke(lift, new Vector2(23, 25), new Vector2(35, 13), 5, SummerGold);
        DrawCueStroke(lift, new Vector2(47, 25), new Vector2(35, 13), 5, SummerGold);
        WorkshopUi.Fill(lift, "Pause Bar Left", new Rect(25, 47, 7, 14), SummerGold);
        WorkshopUi.Fill(lift, "Pause Bar Right", new Rect(39, 47, 7, 14), SummerGold);
        dashuReleaseSign = lift;
    }

    private void DrawDashuDrying()
    {
        dashuDraggingSilk = false;
        var desk = WorkshopUi.WorkbenchCard(art, "Xiazhi Drying Board", new Rect(45, 41, 1070, 571),
            SummerNeutral, SummerGold);
        string weather = DashuRainy ? "梅雨未停 · 窗沿仍潮" : "雨已歇 · 窗边有风";
        WorkshopUi.Label(desk, "Xiazhi Drying Weather", new Rect(40, 25, 990, 47), weather, 32,
            SummerDark, TextAnchor.MiddleCenter);
        DrawDashuDryingSpot(desk, SilkDryingPlace.Shelter, DryingShelterRack,
            "避雨干丝架", "能控湿 · 远离潮窗");
        DrawDashuDryingSpot(desk, SilkDryingPlace.Window, DryingWindowRack,
            "通风窗架", DashuRainy ? "湿风夹雨 · 此处易返潮" : "雨歇有风 · 此处干得快");
        Rect silkOrigin = new Rect(93, 462, 120, 86);
        WorkshopUi.Picture(desk, "Freshly Reeled Silk", silkOrigin, SpringSummerArt("SilkBobbin"),
            dashuDryingPlace == SilkDryingPlace.None ? Color.white : new Color32(220, 210, 170, 85));
        for (int drop = 0; drop < dashuMoisture; drop++)
            WorkshopUi.Picture(desk, "Silk Moisture Drop " + drop, new Rect(207 + drop * 28, 475, 26, 31),
                SpringSummerArt("DewBead"));
        WorkshopUi.Label(desk, "Xiazhi Silk Moisture", new Rect(298, 453, 662, 49),
            dashuMoisture > 0 ? $"丝轴湿标 {dashuMoisture}/{FreshSilkMoisture}" : "丝轴已干妥，可以入库",
            28, dashuMoisture > 0 ? SummerWarning : SummerDark);
        WorkshopUi.Label(desk, "Xiazhi Drying Feedback", new Rect(298, 508, 510, 45),
            dashuNote ?? "选晾架", 24, SummerDark);
        Rect bobbinBounds = dashuDryingPlace == SilkDryingPlace.None ? silkOrigin :
            DashuRackBobbin(dashuDryingPlace == SilkDryingPlace.Shelter ? DryingShelterRack : DryingWindowRack);
        BindDashuDryingDrag(desk, bobbinBounds);
        if (dashuMoisture == 0)
        {
            var store = WorkshopUi.Button(desk, "Store the Dry Bobbin",
                new Rect(852, 452, 176, 86), "", SubmitDashuSilk, SummerJade);
            WorkshopUi.Picture(store.transform, "Silk Receiving Basket", new Rect(18, 11, 112, 64), SpringSummerArt("CocoonBin"));
            WorkshopUi.Picture(store.transform, "Dry Silk Delivery Seal", new Rect(111, 14, 48, 49), SpringSummerArt("SuccessSeal"));
            ShowGesture(desk, new Vector2(934, 494), new Vector2(934, 494), WorkGesture.Tap);
        }
        else if (DashuRainy && dashuDryingPlace != SilkDryingPlace.Shelter)
            ShowSummerCarryCue(desk, bobbinBounds.center, DashuRackBobbin(DryingShelterRack).center, SpringSummerArt("SilkBobbin"));
        else if (dashuDryingPlace == SilkDryingPlace.None)
            ShowSummerCarryCue(desk, bobbinBounds.center, DashuRackBobbin(DryingWindowRack).center, SpringSummerArt("SilkBobbin"));

        Copy("晾丝", "");
        Act("Xiazhi Check Drying", "轻拂", AdvanceDashuDrying, false);
        var speedDry = infoPanel.Find("Xiazhi Check Drying").GetComponentInChildren<Button>();
        speedDry.interactable = dashuMoisture > 0 && dashuDryingPlace != SilkDryingPlace.None;
        speedDry.transform.parent.gameObject.SetActive(speedDry.interactable);
        Act("Xiazhi Store Silk", "干丝入库", SubmitDashuSilk, dashuMoisture == 0);
        var storeDry = infoPanel.Find("Xiazhi Store Silk").GetComponentInChildren<Button>();
        storeDry.interactable = dashuMoisture == 0;
        storeDry.transform.parent.gameObject.SetActive(storeDry.interactable);
        StartCoroutine(RunDashuDrying(desk));
    }

    private static Rect DashuRackBobbin(Rect rack)
    {
        return new Rect(rack.center.x - 48, rack.y + 107, 96, 85);
    }

    private void BindDashuDryingDrag(RectTransform desk, Rect silkBounds)
    {
        var hit = WorkshopUi.Button(desk, "Drag the Wet Bobbin", silkBounds, "", null, Color.clear);
        var ghost = WorkshopUi.Picture(desk, "Carried Silk Bobbin", new Rect(0, 0, 98, 75), SpringSummerArt("SilkBobbin"));
        ghost.gameObject.SetActive(false);
        var events = hit.gameObject.AddComponent<EventTrigger>();
        SummerPointer(events, EventTriggerType.BeginDrag, pointer =>
        {
            if (WorkshopMinigameMenu.BlocksGameplayInput) return;
            pointer.eligibleForClick = false;
            dashuDraggingSilk = true;
            ghost.gameObject.SetActive(true);
            SetDashuDryingDragPosition(desk, ghost.rectTransform, pointer);
        });
        SummerPointer(events, EventTriggerType.Drag, pointer =>
        {
            if (dashuDraggingSilk && !WorkshopMinigameMenu.BlocksGameplayInput)
                SetDashuDryingDragPosition(desk, ghost.rectTransform, pointer);
        });
        SummerPointer(events, EventTriggerType.EndDrag, pointer =>
        {
            if (!dashuDraggingSilk) return;
            dashuDraggingSilk = false;
            ghost.gameObject.SetActive(false);
            if (WorkshopMinigameMenu.BlocksGameplayInput) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(desk, pointer.position, pointer.pressEventCamera, out var at))
                return;
            var drop = new Vector2(at.x, -at.y);
            if (DryingShelterRack.Contains(drop)) MoveDashuSilk(SilkDryingPlace.Shelter);
            else if (DryingWindowRack.Contains(drop)) MoveDashuSilk(SilkDryingPlace.Window);
        });
    }

    private static void SetDashuDryingDragPosition(RectTransform desk, RectTransform ghost, PointerEventData pointer)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(desk, pointer.position, pointer.pressEventCamera, out var at))
            ghost.anchoredPosition = at + new Vector2(-49, 38);
    }

    private void DrawDashuDryingSpot(Transform desk, SilkDryingPlace place, Rect bounds, string name, string hint)
    {
        bool chosen = dashuDryingPlace == place;
        string spotId = place == SilkDryingPlace.Shelter ? "Shelter" : "Window";
        var card = WorkshopUi.PixelCard(desk, "Xiazhi " + spotId + " Drying Spot", bounds,
            chosen ? new Color32(211, 231, 191, 255) : LightSurface, chosen ? SummerJade : WorkshopUi.Bronze);
        WorkshopUi.Label(card, "Rack Name", new Rect(16, 14, bounds.width - 32, 47), name, 29,
            SummerDark, TextAnchor.MiddleCenter);
        WorkshopUi.Picture(card, "Silk Drying Rack", new Rect(30, 61, bounds.width - 60, 176),
            SpringSummerArt("DyeDryRack"));
        if (place == SilkDryingPlace.Shelter)
            WorkshopUi.Picture(card, "Shelter Beam", new Rect(103, 57, 277, 42), SpringSummerArt("WoodenRiser"), stretch: true);
        else if (DashuRainy)
            for (int drop = 0; drop < 3; drop++)
                WorkshopUi.Picture(card, "Window Rain " + drop, new Rect(324 + 26 * drop, 69 + 26 * (drop % 2), 23, 31), SpringSummerArt("DewBead"));
        else
            WorkshopUi.Picture(card, "Open Window Sunshine", new Rect(293, 59, 148, 119), SpringSummerArt("OfferingSunbeam"));
        if (chosen)
        {
            WorkshopUi.Picture(card, "Placed Silk Bobbin", new Rect(bounds.width * 0.5f - 48, 107, 96, 85),
                SpringSummerArt("SilkBobbin"));
            for (int drop = 0; drop < dashuMoisture; drop++)
                WorkshopUi.Picture(card, "Rack Silk Moisture " + drop, new Rect(283 + 24 * drop, 149, 19, 25), SpringSummerArt("DewBead"));
        }
        WorkshopUi.Label(card, "Rack Weather", new Rect(18, 230, bounds.width - 36, 40), hint, 23,
            place == SilkDryingPlace.Window && DashuRainy ? SummerWarning : SummerDark, TextAnchor.MiddleCenter);
        var choose = WorkshopUi.PixelButton(card, "Xiazhi Choose " + spotId, new Rect(50, 277, bounds.width - 100, 52),
            chosen ? "丝轴已放此处" : "把丝轴放这里", () => MoveDashuSilk(place), chosen, 24, SummerJade);
        RegisterButton(choose);
        var rackTarget = WorkshopUi.Button(card, "Whole Drying Rack Target", new Rect(0, 0, bounds.width, 276), "",
            () => MoveDashuSilk(place), Color.clear);
        rackTarget.transform.SetAsLastSibling();
    }

    private IEnumerator RunDashuDrying(RectTransform desk)
    {
        if (dashuMoisture == 0 || dashuDryingPlace == SilkDryingPlace.None ||
            (dashuDryingPlace == SilkDryingPlace.Window && DashuRainy)) yield break;
        float elapsed = 0f;
        while (desk != null && desk.gameObject.activeInHierarchy &&
            (elapsed < 1.6f || dashuDraggingSilk || WorkshopMinigameMenu.BlocksGameplayInput))
        {
            if (!WorkshopMinigameMenu.BlocksGameplayInput) elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (desk != null && desk.gameObject.activeInHierarchy && dashuDrying && !GameComplete)
            AdvanceDashuDrying();
    }

    private void MoveDashuSilk(SilkDryingPlace place)
    {
        if (place == dashuDryingPlace) return;
        dashuDryingPlace = place;
        if (place == SilkDryingPlace.Window && DashuRainy)
        {
            dashuMoisture = Mathf.Min(FreshSilkMoisture, dashuMoisture + 1);
            dashuNote = "窗沿返潮，换到避雨干丝架。";
        }
        else
            dashuNote = "丝轴开始变干，留意水珠慢慢减少。";
        WorkshopAudioFeedback.Place();
        Render();
    }

    private void AdvanceDashuDrying()
    {
        if (dashuDryingPlace == SilkDryingPlace.None)
            dashuNote = "先选一处放丝轴。";
        else if (dashuDryingPlace == SilkDryingPlace.Window && DashuRainy)
            dashuNote = "湿标没降。梅雨未停，移到避雨干丝架。";
        else
        {
            int dryingRate = DashuRainy || dashuDryingPlace == SilkDryingPlace.Window ? 2 : 1;
            dashuMoisture = Mathf.Max(0, dashuMoisture - dryingRate);
            dashuNote = dashuMoisture == 0 ? "水珠全消失了！点「干丝入库」。" : "水珠变少了，正在继续干燥。";
            if (dashuMoisture == 0) WorkshopAudioFeedback.Place();
        }
        Render();
    }

    private void SubmitDashuSilk()
    {
        if (dashuMoisture > 0)
        {
            dashuNote = "湿丝入库容易粘连，先把湿标降到零。";
            WorkshopAudioFeedback.Notice();
            Render();
            return;
        }
        WorkshopAudioFeedback.Collect();
        FinishChallenge(Mathf.Max(1, 1000 + dashuCleanSplices * 8 - dashuBreaks * 25 - dashuSplices),
            DashuWorkResult(), CaptureWoundSilk());
    }

    private void DrawDashuCocoonSupply(Transform workshop)
    {
        string batch = WorkshopUpstreamWork(WorkshopObservation.MangzhongCocoon, ChallengeVariant)?.sample;
        bool fromInspection = !string.IsNullOrEmpty(batch) && batch.Length == InspectionCocoonCount;
        WorkshopUi.Label(workshop, "Dashu Ready Workbench", new Rect(19, 4, 350, 44),
            fromInspection ? "芒种验收批次 · 出库干茧" : "工坊备茧 · 出库干茧", 25, SummerDark);
        if (!fromInspection) return;

        var cocoon = SpringSprite("WorkshopObservation/Summer_CocoonIntact");
        int shown = 0;
        for (int i = 0; i < batch.Length; i++)
        {
            bool longShape = batch[i] == LongCocoonInk;
            if (!longShape && batch[i] != RoundCocoonInk) continue;
            float left = 358 + 49 * shown++;
            WorkshopUi.Picture(workshop, "Dashu Received Cocoon " + i, new Rect(left, 4, longShape ? 37 : 28, longShape ? 21 : 28),
                cocoon, stretch: longShape);
            WorkshopUi.Label(workshop, "Dashu Cocoon Number " + i, new Rect(left + 19, 21, 23, 23),
                (i + 1).ToString(), 16, SummerDark, TextAnchor.MiddleCenter);
        }
    }

    private void DrawDashuReelingTools(Rect illustration)
    {
        const string stepsResource = "WorkshopObservation/Summer_ReelingSteps";
        dashuWheel = null;
        var wheel = SpringSummerArt("ReelingWheel");
        var steps = SpringSprite(stepsResource);
        if (wheel == null || steps == null)
        {
            Art("Dashu Reeling Reference", stepsResource, illustration, true);
            return;
        }

        float stepsWidth = illustration.width * 2f / 3f;
        var instructions = WorkshopUi.Rect(art, "Dashu Reeling Reference",
            new Rect(illustration.x, illustration.y, stepsWidth, illustration.height)).gameObject.AddComponent<RawImage>();
        instructions.texture = steps.texture;
        instructions.uvRect = new Rect(0f, 0f, 2f / 3f, 1f);
        instructions.raycastTarget = false;

        var worktop = WorkshopUi.WorkbenchCard(art, "Dashu Reeling Stand",
            new Rect(illustration.x + stepsWidth + 3f, illustration.y + 3f, illustration.width - stepsWidth - 6f,
                illustration.height - 6f), SummerNeutral, SummerGold);
        WorkshopUi.Picture(worktop, "Dashu Wheel Foot", new Rect(18, 186, 238, 54), SpringSummerArt("WoodenRiser"));
        WorkshopUi.Picture(worktop, "Mounted Silk Bobbin", new Rect(222, 156, 93, 83), SpringSummerArt("SilkBobbin"));
        var wheelBounds = new Rect(21, 14, 231, 205);
        dashuWheel = WorkshopUi.Picture(worktop, "Dashu Moving Reel", wheelBounds, wheel);
        dashuWheel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        dashuWheel.rectTransform.anchoredPosition = new Vector2(wheelBounds.center.x, -wheelBounds.center.y);
        WorkshopUi.Label(worktop, "Wheel Direct Control", new Rect(16, 218, 205, 31), "按住丝轮", 18,
            SummerDark, TextAnchor.MiddleCenter);
    }

    private IEnumerator RunDashuReel(RectTransform worktop)
    {
        while (worktop != null && worktop.gameObject.activeInHierarchy && !showBriefing && !GameComplete && !dashuDrying)
        {
            if (dashuHoldingPointer && !Input.GetMouseButton(0)) dashuHoldingPointer = false;
            if (WorkshopMinigameMenu.BlocksGameplayInput)
            {
                yield return null;
                continue;
            }
            bool held = Application.isFocused && (dashuHoldingPointer || Input.GetKey(KeyCode.Space));
            if (!Application.isFocused) dashuHoldingPointer = false;
            if (Application.isFocused && Input.GetKeyDown(KeyCode.E)) SpliceDashuSilk();
            AdvanceDashuReel(Mathf.Min(0.05f, Time.unscaledDeltaTime), held);
            if (GameComplete || dashuDrying) yield break;
            RefreshDashuWorktop();
            yield return null;
        }
        dashuHoldingPointer = false;
    }

    private void AdvanceDashuReel(float delta, bool held)
    {
        if (GameComplete || dashuDrying) return;
        if (dashuNoticeTime > 0f) dashuNoticeTime -= delta;
        if (!dashuThreadAttached) return;

        float gearSpeed = dashuSlowGear ? 0.54f : 1f;
        dashuSpeed = Mathf.MoveTowards(dashuSpeed, held ? gearSpeed : 0f, (held ? 0.73f : 1.18f) * delta);
        dashuWheelAngle = Mathf.Repeat(dashuWheelAngle + dashuSpeed * 240f * delta, 360f);
        float irregularity = dashuLead % 2 == 1 ? 0.03f : 0f;
        float shortLeadTug = dashuRemaining < DashuReadyToSplice ? 0.08f : 0f;
        float pull = dashuSpeed * (0.74f + shortLeadTug + irregularity);
        dashuTension = Mathf.MoveTowards(dashuTension, Mathf.Clamp01(dashuSpeed * 0.16f + pull), 1.45f * delta);

        if (dashuTension > DashuSafeEnd)
            dashuStrain = Mathf.Clamp01(dashuStrain + (0.80f + (dashuTension - DashuSafeEnd) * 3.8f) * delta);
        else
            dashuStrain = Mathf.MoveTowards(dashuStrain, 0f, 0.65f * delta);
        if (dashuStrain >= 1f)
        {
            BreakDashuSilk("牵得太紧，丝断了。停轮后可以补接。");
            return;
        }

        if (dashuTension < DashuSafeStart || dashuTension > DashuSafeEnd) return;
        dashuTriedWinding = true;
        float windingRate = ChallengeVariant == 0 ? 4.2f : 2.1f;
        float wound = Mathf.Min(dashuRemaining, dashuSpeed * windingRate * delta);
        dashuWrapped += wound;
        dashuRemaining -= wound;
        if (dashuWrapped >= DashuLengthGoal && dashuSplices > 0)
        {
            dashuWrapped = DashuLengthGoal;
            dashuSpeed = 0f;
            dashuHoldingPointer = false;
            dashuDrying = true;
            dashuNote = null;
            WorkshopAudioFeedback.Collect();
            Render();
            return;
        }
        if (dashuRemaining <= 0.001f)
            BreakDashuSilk("这一股丝用尽了。已经绕好的部分仍在轴上。");
        else if (DashuWantsSplice() && !dashuWarnedAboutReserve)
        {
            dashuWarnedAboutReserve = true;
            dashuNoticeTime = 0f;
            WorkshopAudioFeedback.Notice();
        }
    }

    private bool DashuWantsSplice() => dashuCocoonsPrepared && (!dashuThreadAttached || dashuRemaining <= DashuReadyToSplice);

    private string CaptureWoundSilk()
    {
        int width = Mathf.Clamp(Mathf.CeilToInt(dashuWrapped / 2f), 1, dashuSilkJoins.Length);
        var knots = new char[width];
        for (int i = 0; i < width; i++) knots[i] = dashuSilkJoins[i] ? '4' : '9';
        string warp = new string('1', width);
        return warp + "/" + new string(knots) + "/" + warp;
    }

    private void BreakDashuSilk(string note)
    {
        dashuBreaks++;
        dashuSpeed = dashuStrain = dashuTension = 0f;
        dashuThreadAttached = dashuHoldingPointer = false;
        WorkshopAudioFeedback.Notice();
        ShowDashuNote(note, 3.7f);
    }

    private void SpliceDashuSilk()
    {
        if (!dashuCocoonsPrepared)
        {
            ShowDashuNote("先煮茧、找丝头。", 2.2f);
            return;
        }
        if (dashuSpeed > DashuMaxSpliceSpeed)
        {
            ShowDashuNote("先松手，让轮子慢下来，再接备用丝头。", 2.2f);
            return;
        }
        if (dashuThreadAttached && dashuRemaining > DashuReadyToSplice)
        {
            ShowDashuNote("这一股还能继续绕，留到快用完再续丝。", 2.2f);
            return;
        }

        dashuLead++;
        dashuSplices++;
        bool clean = dashuThreadAttached && dashuRemaining >= DashuSilkRunningOut;
        if (clean) dashuCleanSplices++;
        int join = Mathf.Clamp(Mathf.FloorToInt(dashuWrapped / 2f), 0, dashuSilkJoins.Length - 1);
        dashuSilkJoins[join] = true;
        var supplies = DashuLeads[ChallengeVariant];
        dashuRemaining = supplies[dashuLead % supplies.Length];
        dashuThreadAttached = true;
        dashuTriedWinding = false;
        dashuWarnedAboutReserve = false;
        if (clean) WorkshopAudioFeedback.Collect();
        else WorkshopAudioFeedback.Place();
        ShowDashuNote(clean ? "接得正好！继续摇轮。" : "接好了，继续摇轮。", 1.4f);
        RefreshDashuWorktop();
    }

    private void ShowDashuNote(string note, float duration)
    {
        dashuNote = note;
        dashuNoticeTime = duration;
    }

    private string DashuWorkResult() =>
        $"成丝 {DashuLengthGoal:0} · 顺接 {dashuCleanSplices}/{dashuSplices} · 断丝 {dashuBreaks}";

    private void RefreshDashuWorktop()
    {
        bool spliceNeeded = DashuWantsSplice();
        bool spliceReady = spliceNeeded && dashuSpeed <= DashuMaxSpliceSpeed;
        bool easing = (spliceNeeded && !spliceReady) || dashuTension > DashuSafeEnd;
        bool inBand = dashuTension >= DashuSafeStart && dashuTension <= DashuSafeEnd;
        string action = !dashuCocoonsPrepared ? "点锅煮茧" : spliceReady ? "点备用丝头" :
            easing ? "松手减速" : inBand ? "按住继续绕" : "按住丝轮";
        dashuWrappedText.text = $"成丝 {dashuWrapped:0.0}/{DashuLengthGoal:0}　续丝 {dashuSplices}　断丝 {dashuBreaks}";
        dashuSupplyText.text = $"第 {dashuLead + 1} 股　余丝 {dashuRemaining:0.0}";
        dashuNoteText.text = dashuNoticeTime > 0f ? dashuNote : action;
        dashuPrepareButton.interactable = !dashuCocoonsPrepared;
        dashuPrepareLabel.text = dashuCocoonsPrepared ? "茧已煮，丝头已找到" : "煮茧、找丝头";
        dashuGearText.text = dashuSlowGear ? "换快摇 · 目前慢摇" : "换慢摇 · 目前快摇";
        dashuWindingButton.interactable = dashuThreadAttached;
        dashuSpliceButton.interactable = spliceReady;
        dashuSpliceText.text = !dashuCocoonsPrepared ? "备好丝头后才能续丝" : !spliceNeeded ? "余丝充足 · 暂不需续丝" :
            !spliceReady ? "松开摇轮，减速后续丝" : "点这里续丝 · E";
        dashuWindingButton.GetComponentInChildren<Text>().text = easing ? "松手减速" :
            inBand ? "按住继续绕" : "按住摇轮 · 空格";
        dashuActionText.text = action;
        dashuGaugeInputText.text = !easing && !spliceReady && inBand ? "绿区 · 按住继续绕" : action;
        dashuGaugeInputText.color = !easing && (inBand || spliceReady) ? SummerReady : SummerGold;
        SetSummerAction(dashuPrepareButton, !dashuCocoonsPrepared);
        SetSummerAction(dashuWindingButton, dashuThreadAttached && !spliceNeeded);
        SetSummerAction(dashuSpliceButton, spliceReady);
        dashuLengthBar.rectTransform.sizeDelta = new Vector2(DashuGaugeWidth * Mathf.Clamp01(dashuWrapped / DashuLengthGoal), 16);
        dashuLengthBar.color = inBand ? SummerReady : SummerGold;
        for (int i = 0; i < dashuJoinTicks.Length; i++)
            dashuJoinTicks[i].gameObject.SetActive(dashuSilkJoins[i]);
        var supplies = DashuLeads[ChallengeVariant];
        float supplyLength = supplies[dashuLead % supplies.Length];
        RefreshDashuReserveMeter(supplyLength, spliceNeeded, spliceReady);
        var needlePosition = dashuNeedle.rectTransform.anchoredPosition;
        needlePosition.x = 150 + DashuGaugeWidth * dashuTension;
        dashuNeedle.rectTransform.anchoredPosition = needlePosition;
        dashuNeedle.color = easing ? SummerUrgent : inBand ? SummerReady : SummerGold;
        dashuSafeTrack.color = inBand ? new Color32(66, 133, 103, 255) : SummerJade;
        dashuGaugeReleaseMark.gameObject.SetActive(easing);
        dashuGaugeReleaseMark.anchoredPosition = new Vector2(Mathf.Max(234f, needlePosition.x), -116f);
        var strainSize = dashuStrainBar.rectTransform.sizeDelta;
        strainSize.x = DashuGaugeWidth * dashuStrain;
        dashuStrainBar.rectTransform.sizeDelta = strainSize;
        dashuThread.enabled = dashuThreadAttached && dashuThread.sprite != null;
        if (dashuWheel != null) dashuWheel.rectTransform.localRotation = Quaternion.Euler(0, 0, -dashuWheelAngle);
        float wave = Mathf.Sin(Time.unscaledTime * 24f) * Mathf.Max(0, dashuTension - 0.65f) * 15f;
        dashuThread.rectTransform.localRotation = Quaternion.Euler(0, 0, wave);
        dashuThread.color = dashuTension > DashuSafeEnd ? new Color32(255, 153, 91, 255) : Color.white;
        RefreshDashuDirectActions(spliceNeeded, spliceReady);
    }

    private void RefreshDashuReserveMeter(float supplyLength, bool spliceNeeded, bool spliceReady)
    {
        float fill = dashuThreadAttached ? dashuRemaining / supplyLength : 0f;
        for (int i = 0; i < dashuReserveCells.Length; i++)
        {
            bool filled = (i + .5f) / dashuReserveCells.Length <= fill;
            dashuReserveCells[i].color = !filled ? WorkshopUi.Line : !spliceNeeded ? SummerJade :
                dashuRemaining < DashuSilkRunningOut ? SummerUrgent : SummerGold;
        }
        float threshold = 698f + 300f * DashuReadyToSplice / supplyLength;
        dashuSpliceThreshold.anchoredPosition = new Vector2(threshold, -43f);
        dashuSpliceThreshold.localScale = new Vector3(spliceReady ? 2f : 1f, 1f, 1f);
    }

    private void RefreshDashuDirectActions(bool spliceNeeded, bool spliceReady)
    {
        bool canWind = dashuThreadAttached && !spliceReady;
        bool shouldRelease = (spliceNeeded && !spliceReady) || dashuTension > DashuSafeEnd;
        dashuPotTarget.gameObject.SetActive(!dashuCocoonsPrepared);
        dashuWheelTarget.gameObject.SetActive(canWind);
        dashuReelHighlight.gameObject.SetActive(canWind && !dashuTriedWinding && !shouldRelease);
        dashuSpliceTarget.gameObject.SetActive(spliceReady);
        dashuReleaseSign.gameObject.SetActive(shouldRelease);
        dashuGauge.gameObject.SetActive(dashuCocoonsPrepared);
        dashuPrepareButton.transform.parent.gameObject.SetActive(!dashuCocoonsPrepared);
        dashuWindingButton.transform.parent.gameObject.SetActive(canWind);
        dashuSpliceButton.transform.parent.gameObject.SetActive(spliceReady);
        dashuGearControl.gameObject.SetActive(dashuCocoonsPrepared && ChallengeVariant != 0);

        int step = !dashuCocoonsPrepared ? 1 : spliceReady ? 3 : shouldRelease ? 4 : !dashuTriedWinding ? 2 : 0;
        if (step == dashuGuideStep) return;
        if (dashuGuide != null)
        {
            dashuGuide.gameObject.SetActive(false);
            Destroy(dashuGuide.gameObject);
        }
        dashuGuideStep = step;
        if (step == 1)
            dashuGuide = ShowGesture(art, new Vector2(248, 208), new Vector2(248, 208), WorkGesture.Tap);
        else if (step == 2)
            dashuGuide = ShowGesture(art, new Vector2(883, 202), new Vector2(883, 202), WorkGesture.Hold);
        else if (step == 3)
            dashuGuide = ShowGesture(art, new Vector2(582, 205), new Vector2(582, 205), WorkGesture.Tap);
    }
}
