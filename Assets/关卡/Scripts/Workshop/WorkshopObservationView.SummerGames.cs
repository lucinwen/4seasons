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
    private static readonly Color SummerJade = new Color32(55, 105, 91, 255);
    private static readonly Color SummerGold = new Color32(253, 217, 139, 255);
    private static readonly Color SummerDark = new Color32(31, 61, 52, 255);
    private static readonly Color SummerNeutral = new Color32(247, 236, 206, 255);
    private static readonly Color CocoonTilePaper = new Color32(255, 246, 224, 192);
    private static readonly Color SummerWarning = new Color32(154, 74, 56, 255);

    private enum CocoonEvidence { Hidden, Intact, Cracked }
    private enum CocoonTray { Pending, Delivery, Review }

    private readonly struct CocoonSample
    {
        internal readonly bool Long;
        internal readonly bool Cracked;
        internal readonly bool Covered;

        internal CocoonSample(bool longShape, bool cracked, bool covered)
        {
            Long = longShape;
            Cracked = cracked;
            Covered = covered;
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
            new CocoonSample(false, false, false), new CocoonSample(true, true, false), new CocoonSample(false, false, true),
            new CocoonSample(false, true, true), new CocoonSample(true, false, true), new CocoonSample(false, false, false),
            new CocoonSample(true, true, true), new CocoonSample(false, false, true), new CocoonSample(true, false, true)),
        new InspectionOrder("细丝加单", 2, 4,
            new CocoonSample(true, false, false), new CocoonSample(false, false, true), new CocoonSample(false, true, true),
            new CocoonSample(true, false, true), new CocoonSample(false, true, true), new CocoonSample(true, false, false),
            new CocoonSample(true, false, true), new CocoonSample(false, false, true), new CocoonSample(true, true, true))
    };

    private readonly bool[] cocoonTurned = new bool[InspectionCocoonCount];
    private readonly CocoonTray[] cocoonTrays = new CocoonTray[InspectionCocoonCount];
    private readonly Image[] cocoonTileFaces = new Image[InspectionCocoonCount];
    private readonly Image[] cocoonTileImages = new Image[InspectionCocoonCount];
    private readonly Text[] cocoonTileLabels = new Text[InspectionCocoonCount];
    private readonly Image[] deliveryCocoons = new Image[6];
    private readonly Image[] reviewCocoons = new Image[3];
    private static readonly Rect CocoonDeliveryArea = new Rect(700, 118, 385, 166);
    private static readonly Rect CocoonReviewArea = new Rect(700, 306, 385, 166);
    private int inspectedCocoonCount;
    private int selectedCocoon;
    private Text cocoonSelectedText;
    private Text cocoonNote;
    private Text cocoonDeliveryText;
    private Text cocoonReviewText;
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
    private float dashuSpeed;
    private float dashuStrain;
    private float dashuWrapped;
    private float dashuRemaining;
    private float dashuTension;
    private int dashuLead;
    private int dashuSplices;
    private int dashuBreaks;
    private bool dashuVentOpen;
    private bool dashuHoldingPointer;
    private bool dashuSlowGear;
    private bool dashuThreadAttached;
    private float dashuNoticeTime;
    private string dashuNote;
    private Text dashuNoteText;
    private Text dashuWrappedText;
    private Text dashuSupplyText;
    private Text dashuWindowText;
    private Text dashuGearText;
    private Text dashuSpliceText;
    private Image dashuNeedle;
    private Image dashuStrainBar;
    private Image dashuThread;
    private Image dashuVentArt;
    private Image dashuWheel;
    private float dashuWheelAngle;
    private float DashuLengthGoal => ChallengeVariant == 0 ? 20f : 24f;

    private void ResetSummerChallenge()
    {
        if (observation == WorkshopObservation.MangzhongCocoon)
            ResetInspection();
        if (observation != WorkshopObservation.DashuReeling) return;

        dashuSpeed = dashuStrain = dashuWrapped = dashuTension = 0f;
        dashuLead = dashuSplices = dashuBreaks = 0;
        dashuRemaining = DashuLeads[ChallengeVariant][0];
        dashuVentOpen = dashuHoldingPointer = false;
        dashuSlowGear = ChallengeVariant == 0;
        dashuThreadAttached = true;
        dashuWheelAngle = 0f;
        dashuNote = null;
        dashuNoticeTime = 0f;
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
            cocoonTileLabels[i] = WorkshopUi.Label(tile.transform, "Cocoon Evidence", new Rect(4, 89, 152, 42), "", 21,
                SummerDark, TextAnchor.MiddleCenter);
            var gestures = tile.gameObject.AddComponent<EventTrigger>();
            SummerPointer(gestures, EventTriggerType.BeginDrag, pointer => StartInspectionDrag(index, pointer));
            SummerPointer(gestures, EventTriggerType.Drag, MoveInspectionDrag);
            SummerPointer(gestures, EventTriggerType.EndDrag, EndInspectionDrag);
        }

        cocoonDeliveryText = DrawInspectionTray(CocoonDeliveryArea, "交货盘", "Cocoon Delivery Tray", CocoonTray.Delivery, deliveryCocoons);
        cocoonReviewText = DrawInspectionTray(CocoonReviewArea, "复查盘", "Cocoon Review Tray", CocoonTray.Review, reviewCocoons);
        var ship = WorkshopUi.PixelButton(art, "Submit Inspection Order", new Rect(700, 503, 385, 66),
            "交货验收", SubmitInspectionOrder, true, 31, SummerJade);
        RegisterButton(ship);
        WorkshopUi.Label(art, "Cocoon Drag Help", new Rect(698, 578, 391, 36), "也可拖茧到右边的盘", 23,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);

        cocoonDragChip = WorkshopUi.Rect(art, "Cocoon In Hand", new Rect(0, 0, 125, 105));
        cocoonDragSprite = WorkshopUi.Picture(cocoonDragChip, "Dragged Cocoon Shell", new Rect(0, 0, 120, 74), initialSprite);
        var dragCaption = WorkshopUi.PixelCard(cocoonDragChip, "Cocoon In Hand Caption", new Rect(6, 74, 112, 31), SummerDark, SummerGold);
        cocoonDragLabel = WorkshopUi.Label(dragCaption, "Selected Cocoon Drag Label", new Rect(3, 0, 106, 30), "", 18,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        cocoonDragChip.gameObject.SetActive(false);

        Copy("每排恰有一枚破口", "同排的茧能互证。拖完整茧进交货盘，破口茧进复查盘。");
        cocoonSelectedText = WorkshopUi.Label(infoPanel, "Selected Cocoon Detail", new Rect(27, 211, 444, 45), "", 24, SummerDark);
        cocoonNote = WorkshopUi.Label(infoPanel, "Inspection Feedback", new Rect(27, 255, 444, 50), "", 23, ErrorInk);
        Act("Turn Over Selected Cocoon", "翻面检查", TurnSelectedCocoon, true);
        Act("Put Selected Cocoon Back", "放回待检盘", () => RouteInspection(CocoonTray.Pending));
        Act("Reset Cocoon Inspection", "重开这一单", () =>
        {
            ResetInspection();
            RefreshInspection("翻面机会已复原。先看同排线索。");
        });
        RefreshInspection("可以先处理肉眼能确认的茧。");
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
        WorkshopUi.Label(result, "Cocoon Order Finished", new Rect(36, 30, 610, 56), "配齐的蚕茧已交货", 42, SummerDark,
            TextAnchor.MiddleCenter);
        Sprite cocoon = Resources.Load<Sprite>("WorkshopObservation/Summer_CocoonIntact");
        for (int i = 0; i < ActiveInspection.RoundCount + ActiveInspection.LongCount; i++)
        {
            float width = i < ActiveInspection.RoundCount ? 83f : 67f;
            WorkshopUi.Picture(result, "Delivered Cocoon " + i, new Rect(53 + i * 96, 121, width, 126), cocoon, stretch: true);
        }
        WorkshopUi.Label(result, "Inspection Result", new Rect(25, 271, 632, 62), InspectionResult(), 28, SummerDark,
            TextAnchor.MiddleCenter);
        Copy("完整茧入批，破口另查", "同排线索只用于这次工坊抽检。实际分批时以逐枚查见的茧况为准。");
        Act("Compare Nanxun Batches", "看南浔的两批蚕", ToggleSummerBatches, true);
    }

    private string InspectionResult() =>
        $"交货 {ActiveInspection.RoundCount + ActiveInspection.LongCount} 枚 · 翻面 {inspectedCocoonCount}/{InspectionBudget} 次";

    private CocoonEvidence InspectEvidence(int index)
    {
        var samples = ActiveInspection.Samples;
        if (!samples[index].Covered || cocoonTurned[index])
            return samples[index].Cracked ? CocoonEvidence.Cracked : CocoonEvidence.Intact;

        int rowStart = index / CocoonsPerRow * CocoonsPerRow;
        int hiddenCount = 0;
        for (int i = rowStart; i < rowStart + CocoonsPerRow; i++)
        {
            if (samples[i].Covered && !cocoonTurned[i]) hiddenCount++;
            else if (samples[i].Cracked) return CocoonEvidence.Intact;
        }
        return hiddenCount == 1 ? CocoonEvidence.Cracked : CocoonEvidence.Hidden;
    }

    private void RefreshInspection(string feedback)
    {
        Sprite intact = Resources.Load<Sprite>("WorkshopObservation/Summer_CocoonIntact");
        Sprite cracked = Resources.Load<Sprite>("WorkshopObservation/Summer_CocoonReview");
        int round = 0;
        int longCount = 0;
        int review = 0;
        int unsettled = 0;
        foreach (Image icon in deliveryCocoons) icon.enabled = false;
        foreach (Image icon in reviewCocoons) icon.enabled = false;
        for (int i = 0; i < InspectionCocoonCount; i++)
        {
            CocoonSample cocoon = ActiveInspection.Samples[i];
            CocoonEvidence evidence = InspectEvidence(i);
            string label = evidence == CocoonEvidence.Hidden ? "待确认" : evidence == CocoonEvidence.Cracked ? "破口" : "完整";
            cocoonTileImages[i].sprite = evidence == CocoonEvidence.Cracked ? cracked : intact;
            cocoonTileImages[i].color = evidence == CocoonEvidence.Hidden ? new Color32(201, 190, 157, 255) : Color.white;
            cocoonTileFaces[i].color = selectedCocoon == i ? SummerGold :
                cocoonTrays[i] == CocoonTray.Delivery ? new Color32(179, 223, 181, 255) :
                cocoonTrays[i] == CocoonTray.Review ? new Color32(240, 184, 156, 255) : CocoonTilePaper;
            string destination = cocoonTrays[i] == CocoonTray.Delivery ? "交货" :
                cocoonTrays[i] == CocoonTray.Review ? "复查" : label;
            cocoonTileLabels[i].text = $"{(cocoon.Long ? "长茧" : "圆茧")} · {destination}";
            if (cocoonTrays[i] == CocoonTray.Delivery)
            {
                ShowStoredCocoon(deliveryCocoons, round + longCount, cocoon, cocoonTileImages[i]);
                if (cocoon.Long) longCount++;
                else round++;
            }
            if (cocoonTrays[i] == CocoonTray.Review)
                ShowStoredCocoon(reviewCocoons, review++, cocoon, cocoonTileImages[i]);
            if (evidence == CocoonEvidence.Hidden) unsettled++;
        }
        cocoonDeliveryText.text = $"圆 {round}/{ActiveInspection.RoundCount}　长 {longCount}/{ActiveInspection.LongCount}";
        cocoonReviewText.text = $"入盘 {review}/3 枚　待确认 {unsettled} 枚";
        cocoonBudgetText.text = $"翻面余 {InspectionBudget - inspectedCocoonCount} 次";
        string selected = "当前未拿茧";
        if (selectedCocoon >= 0)
        {
            CocoonEvidence evidence = InspectEvidence(selectedCocoon);
            string known = evidence == CocoonEvidence.Hidden ? "背面被遮" :
                evidence == CocoonEvidence.Cracked ? "有破口" : "完整";
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
        RefreshInspection(ActiveInspection.Samples[selectedCocoon].Cracked ? "发现破口，同排其余茧完整。" : "翻面完整，接着核对同排。");
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
            if (cocoonTrays[i] == CocoonTray.Review ? evidence != CocoonEvidence.Cracked : evidence != CocoonEvidence.Intact)
            {
                selectedCocoon = i;
                RefreshInspection($"{i + 1} 号放错盘，选中后可以直接换盘。");
                return;
            }
            if (cocoonTrays[i] == CocoonTray.Review) review++;
            else if (ActiveInspection.Samples[i].Long) longCount++;
            else round++;
        }

        if (round != ActiveInspection.RoundCount || longCount != ActiveInspection.LongCount || review != 3)
        {
            RefreshInspection("按工单配茧，复查盘收齐三枚破口。");
            return;
        }

        WorkshopAudioFeedback.Collect();
        FinishChallenge(100 + InspectionBudget - inspectedCocoonCount, InspectionResult());
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
        Sprite ventilation = Resources.Load<Sprite>("WorkshopObservation/Summer_OpenWindow");
        dashuVentArt = WorkshopUi.Picture(art, "Dashu Open Window", ArtBounds, ventilation, stretch: true);
        dashuVentArt.enabled = dashuVentOpen;
        WorkshopUi.Fill(art, "Dashu Workbench Shade", ArtBounds, new Color32(29, 35, 25, 86));
        if (GameComplete)
        {
            var finished = WorkshopUi.PixelCard(art, "Wound Silk", new Rect(300, 126, 560, 393), SummerNeutral, SummerGold);
            WorkshopUi.Picture(finished, "Hand Reeled Silk", new Rect(145, 54, 270, 228),
                SpringSummerArt("SilkBobbin") ?? Resources.Load<Sprite>("WorkshopFinish/RawSilk"));
            WorkshopUi.Label(finished, "Silk Work Result", new Rect(20, 276, 520, 78), DashuWorkResult(), 33,
                SummerDark, TextAnchor.MiddleCenter);
            Copy("丝头续上了，丝束已经绕好", "稳丝时要留意茧况和手上的拉力。换丝头前可以先减速，断线也可在原处重新续上。");
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
        WorkshopUi.Label(workshop, "Dashu Ready Workbench", new Rect(19, 4, 548, 44), "煮过的茧已备好 · 丝头可接入丝轴", 26, SummerDark);
        dashuSupplyText = WorkshopUi.Label(workshop, "Current Silk Lead", new Rect(546, 4, 464, 44), "", 26, SummerDark,
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

        Copy("控制丝轮，收满一轴", "按住摇轮加速，松手让轮子慢下来。丝头快用完时减速续丝。开窗能减轻闷湿带来的抖动。");
        var winding = WorkshopUi.PixelButton(infoPanel, "Hold Dashu Reel", new Rect(27, 338, 444, 62),
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
        Act("Dashu Window Lever", "", ToggleDashuVent);
        dashuWindowText = infoPanel.Find("Dashu Window Lever").GetComponentInChildren<Text>();
        Act("Dashu Change Gear", "", () =>
        {
            dashuSlowGear = !dashuSlowGear;
            ShowDashuNote(dashuSlowGear ? "换到慢摇，丝轮更稳。" : "换到快摇，留意张力上升。", 1.4f);
        });
        dashuGearText = infoPanel.Find("Dashu Change Gear").GetComponentInChildren<Text>();
        RefreshDashuWorktop();
        StartCoroutine(RunDashuReel(workshop));
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
        while (worktop != null && worktop.gameObject.activeInHierarchy && !GameComplete)
        {
            bool held = Application.isFocused && (dashuHoldingPointer || Input.GetKey(KeyCode.Space));
            if (!Application.isFocused) dashuHoldingPointer = false;
            AdvanceDashuReel(Mathf.Min(0.05f, Time.unscaledDeltaTime), held);
            if (GameComplete) yield break;
            RefreshDashuWorktop();
            yield return null;
        }
    }

    private void AdvanceDashuReel(float delta, bool held)
    {
        if (GameComplete) return;
        if (dashuNoticeTime > 0f) dashuNoticeTime -= delta;
        if (!dashuThreadAttached) return;

        float gearSpeed = dashuSlowGear ? 0.54f : 1f;
        dashuSpeed = Mathf.MoveTowards(dashuSpeed, held ? gearSpeed : 0f, (held ? 0.73f : 1.18f) * delta);
        dashuWheelAngle = Mathf.Repeat(dashuWheelAngle + dashuSpeed * 240f * delta, 360f);
        float irregularity = dashuLead % 2 == 1 ? 0.03f : 0f;
        float damp = dashuVentOpen ? 0.02f : (ChallengeVariant == 0 ? 0.14f : 0.18f);
        float shortLeadTug = dashuRemaining < DashuReadyToSplice ? 0.08f : 0f;
        float pull = dashuSpeed * (0.72f + damp + shortLeadTug + irregularity);
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
            WorkshopAudioFeedback.Collect();
            FinishChallenge(Mathf.Max(1, 1000 - dashuBreaks * 25 - dashuSplices), DashuWorkResult());
            return;
        }
        if (dashuRemaining <= 0.001f)
            BreakDashuSilk("这一股丝用尽了。已经绕好的部分仍在轴上。");
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
        var supplies = DashuLeads[ChallengeVariant];
        dashuRemaining = supplies[dashuLead % supplies.Length];
        dashuThreadAttached = true;
        WorkshopAudioFeedback.Place();
        ShowDashuNote("新的丝头接住了。按住可继续绕。", 2.2f);
    }

    private void ToggleDashuVent()
    {
        dashuVentOpen = !dashuVentOpen;
        ShowDashuNote(dashuVentOpen ? "窗开了，湿气散开，丝线更稳。" : "关窗后气流变弱，拉紧时容易抖。", 2.2f);
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
        dashuNoteText.text = dashuNoticeTime > 0f ? dashuNote : !dashuThreadAttached ? "丝已断，选择「接备用丝头」补救。" :
            dashuRemaining <= DashuReadyToSplice ? "余丝快用完了。松手减速，准备接备用丝头。" :
            dashuTension > DashuSafeEnd ? "丝线颤动，松手减速。" :
            dashuSpeed > 0.1f ? "丝轮在转。调整手上的速度，把张力留在中段。" : "按住摇轮开始。慢摇也能绕满。";
        dashuVentArt.enabled = dashuVentOpen;
        dashuWindowText.text = dashuVentOpen ? "关窗 · 目前通风" : "开窗 · 目前闷湿";
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
