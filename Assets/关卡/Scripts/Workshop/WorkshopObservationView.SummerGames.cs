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
    private readonly Text[] cocoonTileLabels = new Text[InspectionCocoonCount];
    private readonly Image[] deliveryCocoons = new Image[6];
    private readonly Image[] reviewCocoons = new Image[2];
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
    private RectTransform cocoonDragChip;
    private Image cocoonDragSprite;
    private Text cocoonDragLabel;
    private InspectionOrder ActiveInspection => InspectionOrders[ChallengeVariant];

    private static readonly float[][] DashuLeads =
    {
        new[] { 8f, 9.5f, 12f },
        new[] { 6.5f, 11.5f, 8f, 12f }
    };
    private const float DashuReadyToSplice = 2.8f;
    private const float DashuSafeStart = 0.34f;
    private const float DashuSafeEnd = 0.77f;
    private const float DashuMaxSpliceSpeed = 0.19f;
    private const float DashuGaugeWidth = 716f;
    private const int FreshSilkMoisture = 3;
    private enum SilkDryingPlace { None, Shelter, Window }
    private readonly bool[] dashuSilkJoins = new bool[12];
    private float dashuSpeed;
    private float dashuStrain;
    private float dashuWrapped;
    private float dashuRemaining;
    private float dashuTension;
    private int dashuLead;
    private int dashuSplices;
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
    private Text dashuSupplyText;
    private Button dashuPrepareButton;
    private Text dashuPrepareLabel;
    private Text dashuGearText;
    private Text dashuSpliceText;
    private Image dashuNeedle;
    private Image dashuStrainBar;
    private Image dashuThread;
    private Image dashuWheel;
    private float dashuWheelAngle;
    private float DashuLengthGoal => ChallengeVariant == 0 ? 20f : 24f;
    private bool DashuRainy => ChallengeVariant == 0;

    private void ResetSummerChallenge()
    {
        if (observation == WorkshopObservation.MangzhongCocoon)
            ResetInspection();
        if (observation != WorkshopObservation.DashuReeling) return;

        dashuSpeed = dashuStrain = dashuWrapped = dashuTension = 0f;
        dashuLead = dashuSplices = dashuBreaks = 0;
        dashuRemaining = DashuLeads[ChallengeVariant][0];
        dashuCocoonsPrepared = dashuHoldingPointer = dashuThreadAttached = dashuDrying = false;
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
    }

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
            var sample = ActiveInspection.Samples[i];
            bool stillOnMount = sample.Condition == CocoonEvidence.Immature;
            WorkshopUi.Label(tile.transform, "Cocoon Mount Date", new Rect(76, 4, 79, 26),
                $"蔟龄 {sample.MountDays} 日", 17, stillOnMount ? SummerWarning : SummerDark, TextAnchor.MiddleRight);
            cocoonTileLabels[i] = WorkshopUi.Label(tile.transform, "Cocoon Evidence", new Rect(4, 89, 152, 42), "", 21,
                SummerDark, TextAnchor.MiddleCenter);
            var gestures = tile.gameObject.AddComponent<EventTrigger>();
            SummerPointer(gestures, EventTriggerType.BeginDrag, pointer => StartInspectionDrag(index, pointer));
            SummerPointer(gestures, EventTriggerType.Drag, MoveInspectionDrag);
            SummerPointer(gestures, EventTriggerType.EndDrag, EndInspectionDrag);
        }

        cocoonDeliveryText = DrawInspectionTray(CocoonDeliveryArea, "交货盘", "Cocoon Delivery Tray", CocoonTray.Delivery, deliveryCocoons);
        cocoonReviewText = DrawInspectionTray(CocoonReviewArea, "复查盘", "Cocoon Review Tray", CocoonTray.Review, reviewCocoons);
        var mount = WorkshopUi.PixelButton(art, "Route Cocoon To Mount", CocoonMountArea, "",
            () => RouteInspection(CocoonTray.Mount), false, 23);
        cocoonMountText = mount.GetComponentInChildren<Text>();
        RegisterButton(mount);
        var ship = WorkshopUi.PixelButton(art, "Submit Inspection Order", new Rect(700, 535, 385, 58),
            "交货验收", SubmitInspectionOrder, true, 31, SummerJade);
        RegisterButton(ship);
        WorkshopUi.Label(art, "Cocoon Drag Help", new Rect(698, 593, 391, 35), "也可拖到右侧对应位置", 23,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);

        cocoonDragChip = WorkshopUi.Rect(art, "Cocoon In Hand", new Rect(0, 0, 125, 105));
        cocoonDragSprite = WorkshopUi.Picture(cocoonDragChip, "Dragged Cocoon Shell", new Rect(0, 0, 120, 74), initialSprite);
        var dragCaption = WorkshopUi.PixelCard(cocoonDragChip, "Cocoon In Hand Caption", new Rect(6, 74, 112, 31), SummerDark, SummerGold);
        cocoonDragLabel = WorkshopUi.Label(dragCaption, "Selected Cocoon Drag Label", new Rect(3, 0, 106, 30), "", 18,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        cocoonDragChip.gameObject.SetActive(false);

        Copy("芒种｜春茧陆续到站", "每排有一枚需要处理。\n足日且完好干爽，才交货。\n未熟留蔟，过潮或破口复查。");
        cocoonSelectedText = WorkshopUi.Label(infoPanel, "Selected Cocoon Detail", new Rect(27, 235, 444, 40), "", 23, SummerDark);
        cocoonNote = WorkshopUi.Label(infoPanel, "Inspection Feedback", new Rect(27, 277, 444, 46), "", 21, ErrorInk);
        Act("Turn Over Selected Cocoon", "翻面检查", TurnSelectedCocoon, true);
        Act("Put Selected Cocoon Back", "放回待检盘", () => RouteInspection(CocoonTray.Pending));
        Act("Reset Cocoon Inspection", "重开这一单", () =>
        {
            ResetInspection();
            RefreshInspection("翻面机会已复原。先看同排线索。");
        });
        var supply = WorkshopProgress.IsComplete(WorkshopSeason.Spring)
            ? "从春蚕养护记录接过来，先看蔟龄。" : "工坊备了春茧，先看上蔟记录。";
        RefreshInspection(supply);
    }

    private Text DrawInspectionTray(Rect bounds, string heading, string name, CocoonTray destination, Image[] slots)
    {
        var tray = SummerArtworkPanel(name, "CocoonBin", bounds, SummerNeutral);
        var label = WorkshopUi.PixelCard(tray, "Tray Label Paper", new Rect(39, 8, bounds.width - 78, 64), LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(label, "Tray Heading", new Rect(5, 2, bounds.width - 88, 31), heading, 26, SummerDark,
            TextAnchor.MiddleCenter);
        var count = WorkshopUi.Label(label, "Tray Contents", new Rect(5, 30, bounds.width - 88, 30), "", 23,
            SummerDark, TextAnchor.MiddleCenter);
        for (int i = 0; i < slots.Length; i++)
        {
            float x = (bounds.width - slots.Length * 46) * 0.5f + i * 46;
            slots[i] = WorkshopUi.Picture(tray, "Stored Cocoon " + i, new Rect(x, 72, 41, 34), null, stretch: true);
        }
        var route = WorkshopUi.PixelButton(tray, "Route Cocoon To " + destination, new Rect(28, 111, bounds.width - 56, 43),
            destination == CocoonTray.Delivery ? "装入所选茧" : "送入所选茧", () => RouteInspection(destination), false, 23);
        RegisterButton(route);
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
            default: return "待确认";
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
        int unsettled = 0;
        foreach (Image icon in deliveryCocoons) icon.enabled = false;
        foreach (Image icon in reviewCocoons) icon.enabled = false;
        for (int i = 0; i < InspectionCocoonCount; i++)
        {
            CocoonSample cocoon = ActiveInspection.Samples[i];
            CocoonEvidence evidence = InspectEvidence(i);
            cocoonTileImages[i].sprite = evidence == CocoonEvidence.Cracked ? cracked : intact;
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
                ShowStoredCocoon(deliveryCocoons, round + longCount, cocoon, cocoonTileImages[i]);
                if (cocoon.Long) longCount++;
                else round++;
            }
            if (cocoonTrays[i] == CocoonTray.Review)
                ShowStoredCocoon(reviewCocoons, review++, cocoon, cocoonTileImages[i]);
            if (cocoonTrays[i] == CocoonTray.Mount) stayOnMount++;
            if (evidence == CocoonEvidence.Hidden) unsettled++;
        }
        cocoonDeliveryText.text = $"圆 {round}/{ActiveInspection.RoundCount}　长 {longCount}/{ActiveInspection.LongCount}";
        cocoonReviewText.text = $"入盘 {review}/{reviewCocoons.Length} 枚　待确认 {unsettled} 枚";
        cocoonMountText.text = $"未熟留蔟 · {stayOnMount}/1 枚";
        cocoonBudgetText.text = $"翻面余 {InspectionBudget - inspectedCocoonCount} 次";
        string selected = "当前未拿茧";
        if (selectedCocoon >= 0)
        {
            CocoonEvidence evidence = InspectEvidence(selectedCocoon);
            string known = evidence == CocoonEvidence.Hidden ? "背面被遮" : CocoonEvidenceLabel(evidence);
            selected = $"选中 {selectedCocoon + 1} 号 · {known}";
        }
        cocoonSelectedText.text = selected;
        cocoonNote.text = feedback;
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

        if (!ActiveInspection.Samples[selectedCocoon].Covered || cocoonTurned[selectedCocoon])
        {
            RefreshInspection("这一面的茧况已经能看清。");
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
        AnimateSpringSummerSprite(cocoonTileImages[selectedCocoon], new Vector2(4, 11), 0.82f, 0.19f);
    }

    private void RouteInspection(CocoonTray destination)
    {
        if (selectedCocoon < 0)
        {
            RefreshInspection("先在左侧选一枚茧。");
            return;
        }

        cocoonTrays[selectedCocoon] = destination;
        WorkshopAudioFeedback.Place();
        RefreshInspection(destination == CocoonTray.Pending ? "已放回待检盘。" : "已入盘，交货前仍可以换回。");
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
        cocoonDragChip.gameObject.SetActive(false);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera,
                out Vector2 point)) return;
        point.y = -point.y;
        if (CocoonDeliveryArea.Contains(point)) RouteInspection(CocoonTray.Delivery);
        else if (CocoonReviewArea.Contains(point)) RouteInspection(CocoonTray.Review);
        else if (CocoonMountArea.Contains(point)) RouteInspection(CocoonTray.Mount);
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

        var workshop = WorkshopUi.PixelCard(art, "Dashu Reel Closeup", new Rect(65, 26, 1030, 352), SummerNeutral, SummerGold);
        var illustration = new Rect(82, 79, 996, 259);
        DrawDashuReelingTools(illustration);
        dashuThread = WorkshopUi.Picture(art, "Active Silk Thread", illustration,
            SpringSprite("WorkshopObservation/Summer_ReelingThreadPath"), stretch: true);
        var threadPivot = new Vector2(0.62f, 0.42f);
        dashuThread.rectTransform.pivot = threadPivot;
        dashuThread.rectTransform.anchoredPosition = new Vector2(illustration.x + illustration.width * threadPivot.x,
            -illustration.y - illustration.height * (1f - threadPivot.y));
        DrawDashuCocoonSupply(workshop);
        dashuSupplyText = WorkshopUi.Label(workshop, "Current Silk Lead", new Rect(687, 4, 323, 44), "", 26, SummerDark,
            TextAnchor.MiddleRight);
        dashuNoteText = WorkshopUi.Label(workshop, "Dashu Live Status", new Rect(18, 308, 995, 40), "", 26, SummerDark,
            TextAnchor.MiddleCenter);

        var gauge = SummerArtworkPanel("Dashu Tension Gauge", "ReelingGauge", new Rect(65, 396, 1030, 220), SummerDark);
        dashuWrappedText = WorkshopUi.Label(gauge, "Dashu Wound Length", new Rect(19, 6, 991, 58), "", 30, WorkshopUi.Cream,
            TextAnchor.MiddleCenter);
        float trackX = 156;
        WorkshopUi.Fill(gauge, "Tension Track", new Rect(trackX, 91, DashuGaugeWidth, 42), SummerWarning);
        WorkshopUi.Fill(gauge, "Safe Tension", new Rect(trackX + DashuSafeStart * DashuGaugeWidth, 91,
            (DashuSafeEnd - DashuSafeStart) * DashuGaugeWidth, 42), SummerJade);
        dashuNeedle = WorkshopUi.Fill(gauge, "Dashu Tension Needle", new Rect(trackX - 6, 83, 12, 60), SummerGold);
        WorkshopUi.Label(gauge, "Low Tension Label", new Rect(12, 91, 130, 42), "松 · 打圈", 22, WorkshopUi.Cream,
            TextAnchor.MiddleRight);
        WorkshopUi.Label(gauge, "High Tension Label", new Rect(887, 91, 129, 42), "紧 · 易断", 22, WorkshopUi.Cream);
        WorkshopUi.Fill(gauge, "Strain Track", new Rect(trackX, 170, DashuGaugeWidth, 12), new Color32(78, 82, 68, 255));
        dashuStrainBar = WorkshopUi.Fill(gauge, "Accumulating Strain", new Rect(trackX, 170, 0, 12), SummerGold);
        WorkshopUi.Label(gauge, "Dashu Risk Label", new Rect(12, 153, 130, 42), "断丝风险", 22, WorkshopUi.Cream,
            TextAnchor.MiddleRight);

        Copy(DashuRainy ? "夏至｜梅雨缫丝" : "夏至｜雨歇缫丝",
            "先煮茧找丝头。按住摇轮，松手减速续丝。\n绕好的生丝带水，干妥才可入库。");
        Act("Dashu Prepare Cocoons", "煮茧、找丝头", PrepareDashuCocoons, true);
        dashuPrepareButton = infoPanel.Find("Dashu Prepare Cocoons").GetComponentInChildren<Button>();
        dashuPrepareLabel = dashuPrepareButton.GetComponentInChildren<Text>();
        var windingBounds = new Rect(27, 338 + rows * 73f, 444, 62);
        var winding = WorkshopUi.PixelButton(infoPanel, "Hold Dashu Reel", windingBounds,
            "按住摇轮 · 空格", null, true, 29, SummerJade);
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
        dashuSpliceText = infoPanel.Find("Dashu Splice Silk").GetComponentInChildren<Text>();
        Act("Dashu Change Gear", "", () =>
        {
            dashuSlowGear = !dashuSlowGear;
            ShowDashuNote(dashuSlowGear ? "换到慢摇，丝轮更稳。" : "换到快摇，留意张力上升。", 1.4f);
        });
        dashuGearText = infoPanel.Find("Dashu Change Gear").GetComponentInChildren<Text>();
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

    private void DrawDashuDrying()
    {
        var desk = WorkshopUi.WorkbenchCard(art, "Xiazhi Drying Board", new Rect(45, 41, 1070, 571),
            SummerNeutral, SummerGold);
        string weather = DashuRainy ? "梅雨未停 · 窗沿仍潮" : "雨已歇 · 窗边有风";
        WorkshopUi.Label(desk, "Xiazhi Drying Weather", new Rect(40, 25, 990, 47), weather, 32,
            SummerDark, TextAnchor.MiddleCenter);
        DrawDashuDryingSpot(desk, SilkDryingPlace.Shelter, new Rect(34, 99, 484, 345),
            "避雨干丝架", "能控湿 · 远离潮窗");
        DrawDashuDryingSpot(desk, SilkDryingPlace.Window, new Rect(550, 99, 484, 345),
            "通风窗架", DashuRainy ? "湿风夹雨 · 此处易返潮" : "雨歇有风 · 此处干得快");
        WorkshopUi.Picture(desk, "Freshly Reeled Silk", new Rect(93, 462, 120, 86), SpringSummerArt("SilkBobbin"));
        for (int drop = 0; drop < dashuMoisture; drop++)
            WorkshopUi.Picture(desk, "Silk Moisture Drop " + drop, new Rect(207 + drop * 28, 475, 26, 31),
                SpringSummerArt("DewBead"));
        WorkshopUi.Label(desk, "Xiazhi Silk Moisture", new Rect(298, 453, 662, 49),
            dashuMoisture > 0 ? $"丝轴湿标 {dashuMoisture}/{FreshSilkMoisture}" : "丝轴已干妥，可以入库",
            28, dashuMoisture > 0 ? SummerWarning : SummerDark);
        WorkshopUi.Label(desk, "Xiazhi Drying Feedback", new Rect(298, 508, 690, 45),
            dashuNote ?? "选一处放丝轴，查看湿标变化。", 24, SummerDark);

        Copy("等湿丝干妥再入库", DashuRainy
            ? "生丝绕好仍带水。梅雨天窗沿潮，放到避雨干丝架，再查看湿标。"
            : "这回雨歇，窗边通风比避雨架干得快。等湿标清零再入库。");
        Act("Xiazhi Check Drying", "查看湿标变化", AdvanceDashuDrying, true);
        Act("Xiazhi Store Silk", "干丝入库", SubmitDashuSilk);
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
        if (chosen)
            WorkshopUi.Picture(card, "Placed Silk Bobbin", new Rect(bounds.width * 0.5f - 48, 107, 96, 85),
                SpringSummerArt("SilkBobbin"));
        WorkshopUi.Label(card, "Rack Weather", new Rect(18, 230, bounds.width - 36, 40), hint, 23,
            place == SilkDryingPlace.Window && DashuRainy ? SummerWarning : SummerDark, TextAnchor.MiddleCenter);
        var choose = WorkshopUi.PixelButton(card, "Xiazhi Choose " + spotId, new Rect(50, 277, bounds.width - 100, 52),
            chosen ? "丝轴已放此处" : "把丝轴放这里", () => MoveDashuSilk(place), chosen, 24, SummerJade);
        RegisterButton(choose);
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
            dashuNote = "丝轴已放好，查看湿标变化。";
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
            dashuNote = dashuMoisture == 0 ? "湿标清零，可以把这段生丝入库。" : "湿标降了，再查看一次。";
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
        FinishChallenge(Mathf.Max(1, 1000 - dashuBreaks * 25 - dashuSplices), DashuWorkResult(), CaptureWoundSilk());
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
    }

    private IEnumerator RunDashuReel(RectTransform worktop)
    {
        while (worktop != null && worktop.gameObject.activeInHierarchy && !GameComplete && !dashuDrying)
        {
            bool held = Application.isFocused && (dashuHoldingPointer || Input.GetKey(KeyCode.Space));
            if (!Application.isFocused) dashuHoldingPointer = false;
            AdvanceDashuReel(Mathf.Min(0.05f, Time.unscaledDeltaTime), held);
            if (GameComplete || dashuDrying) yield break;
            RefreshDashuWorktop();
            yield return null;
        }
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
        float wound = Mathf.Min(dashuRemaining, dashuSpeed * 1.72f * delta);
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
    }

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
        int join = Mathf.Clamp(Mathf.FloorToInt(dashuWrapped / 2f), 0, dashuSilkJoins.Length - 1);
        dashuSilkJoins[join] = true;
        var supplies = DashuLeads[ChallengeVariant];
        dashuRemaining = supplies[dashuLead % supplies.Length];
        dashuThreadAttached = true;
        WorkshopAudioFeedback.Place();
        ShowDashuNote("新的丝头接住了。按住可继续绕。", 2.2f);
    }

    private void ShowDashuNote(string note, float duration)
    {
        dashuNote = note;
        dashuNoticeTime = duration;
    }

    private string DashuWorkResult() => $"成丝 {DashuLengthGoal:0} · 续丝 {dashuSplices} 次 · 断丝 {dashuBreaks} 次";

    private void RefreshDashuWorktop()
    {
        dashuWrappedText.text = $"已绕 {dashuWrapped:0.0}/{DashuLengthGoal:0}　断丝 {dashuBreaks} 次　续丝 {dashuSplices} 次";
        dashuSupplyText.text = $"第 {dashuLead + 1} 股　余 {dashuRemaining:0.0}";
        dashuNoteText.text = dashuNoticeTime > 0f ? dashuNote : !dashuCocoonsPrepared ? "选「煮茧、找丝头」开始处理干茧。" :
            !dashuThreadAttached ? "丝已断，选择「接备用丝头」补救。" :
            dashuRemaining <= DashuReadyToSplice ? "余丝快用完了。松手减速，准备接备用丝头。" :
            dashuTension > DashuSafeEnd ? "丝线颤动，松手减速。" :
            dashuSpeed > 0.1f ? "丝轮在转。调整手上的速度，把张力留在中段。" : "按住摇轮开始。慢摇也能绕满。";
        dashuPrepareButton.interactable = !dashuCocoonsPrepared;
        dashuPrepareLabel.text = dashuCocoonsPrepared ? "茧已煮，丝头已找到" : "煮茧、找丝头";
        dashuGearText.text = dashuSlowGear ? "换快摇 · 目前慢摇" : "换慢摇 · 目前快摇";
        dashuSpliceText.text = dashuThreadAttached ? "续上备用丝头" : "接备用丝头 · 补救";
        var needlePosition = dashuNeedle.rectTransform.anchoredPosition;
        needlePosition.x = 150 + DashuGaugeWidth * dashuTension;
        dashuNeedle.rectTransform.anchoredPosition = needlePosition;
        var strainSize = dashuStrainBar.rectTransform.sizeDelta;
        strainSize.x = DashuGaugeWidth * dashuStrain;
        dashuStrainBar.rectTransform.sizeDelta = strainSize;
        dashuThread.enabled = dashuThreadAttached && dashuThread.sprite != null;
        if (dashuWheel != null) dashuWheel.rectTransform.localRotation = Quaternion.Euler(0, 0, -dashuWheelAngle);
        float wave = Mathf.Sin(Time.unscaledTime * 24f) * Mathf.Max(0, dashuTension - 0.65f) * 15f;
        dashuThread.rectTransform.localRotation = Quaternion.Euler(0, 0, wave);
        dashuThread.color = dashuTension > DashuSafeEnd ? new Color32(255, 153, 91, 255) : Color.white;
    }
}
