using System;
using System.Collections;
using JiangnanDye;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private sealed class DyeRun
    {
        internal readonly string[] Names;
        internal readonly Vector2Int[] Ranges;
        internal readonly Color Ink;
        internal readonly int RipeBatch;
        internal readonly int Water;
        internal readonly int Concentrate;
        internal readonly bool InOrder;

        internal DyeRun(string[] names, Vector2Int[] ranges, Color ink, int ripeBatch, int water, int concentrate, bool inOrder)
        {
            Names = names;
            Ranges = ranges;
            Ink = ink;
            RipeBatch = ripeBatch;
            Water = water;
            Concentrate = concentrate;
            InOrder = inOrder;
        }
    }

    private static readonly DyeRun[] WinterDyeRuns =
    {
        new DyeRun(new[] { "深栀黄", "浅栀黄" }, new[] { new Vector2Int(4, 5), new Vector2Int(1, 2) },
            new Color32(194, 127, 38, 255), 1, 2, 1, false),
        new DyeRun(new[] { "浅栀黄", "蜜栀黄" }, new[] { new Vector2Int(1, 2), new Vector2Int(5, 6) },
            new Color32(170, 108, 37, 255), 2, 1, 1, true)
    };
    private const int StartingVatDepth = 5;
    private const int MaxVatDepth = 6;
    private const string WinterDyeSteps = "轻蘸最多加深一阶，浸染可加深更多。对照预览，晾干比色。";
    private static readonly Color UndyedLinen = new Color32(237, 221, 188, 255);
    private static readonly Color WinterVatRim = new Color32(80, 69, 67, 255);
    private static readonly Color WinterWorktop = new Color32(244, 232, 207, 240);
    private readonly int[] winterDyeDepth = new int[2];
    private readonly int[] winterDyePreviousDepth = new int[2];
    private readonly bool[] winterDyeAccepted = new bool[2];
    private readonly Text[] winterDyeOrderLabels = new Text[2];
    private readonly Image[] winterDyeOrderCloth = new Image[2];
    private readonly Button[] winterDyeOrderButtons = new Button[2];
    private readonly Image[] winterDyeOrderEdges = new Image[2];
    private bool winterDyeSorted;
    private bool winterDyeGround;
    private int winterDyeRejectedBatch;
    private int winterDyeOrder;
    private int winterVatDepth;
    private int winterDyeWaterLeft;
    private int winterDyeConcentrateLeft;
    private int winterDyeVatChanges;
    private int winterDyeReplacements;
    private int winterDyeRenewals;
    private int winterDyeDips;
    private int winterDyeBlots;
    private bool winterDyeWarning;
    private string winterDyeFeedback;
    private Text winterDyeStatus;
    private Text winterDyeGuide;
    private RectTransform winterDyeDesk;
    private RectTransform winterDyeRackAction;
    private RectTransform winterDyeMotionCue;
    private RectTransform winterDyeFocus;
    private RectTransform winterDyeCurrentTarget;
    private Button winterDyeDipButton;
    private Button winterDyeFineButton;
    private Button winterDyeHangButton;
    private Button winterDyeReplaceButton;
    private Button winterDyeBlotButton;
    private RectTransform winterDyeRefill;
    private Image winterDyeDipPreview;
    private Image winterDyeFinePreview;
    private readonly Image[] winterDyeDepthMarks = new Image[MaxVatDepth];
    private Text winterDyeVatLabel;
    private Text winterDyeSampleLabel;
    private Text winterDyeWaterLabel;
    private Text winterDyeConcentrateLabel;
    private Image winterDyeLiquid;
    private Image winterDyeSwatch;
    private Image winterDyeDrop;
    private Image winterDyeRipple;
    private Coroutine winterDyeRippleMotion;
    private RectTransform winterDyeVatTarget;
    private WinterDyeingConfig winterDyeSprites;

    private DyeRun CurrentDyeRun => WinterDyeRuns[ChallengeVariant];

    private void ResetWinterChallenge()
    {
        switch (observation)
        {
            case WorkshopObservation.LidongDye:
                Array.Clear(winterDyeDepth, 0, winterDyeDepth.Length);
                Array.Clear(winterDyeAccepted, 0, winterDyeAccepted.Length);
                for (int i = 0; i < winterDyePreviousDepth.Length; i++) winterDyePreviousDepth[i] = -1;
                winterDyeSorted = false;
                winterDyeGround = false;
                winterDyeRejectedBatch = -1;
                winterDyeOrder = winterDyeVatChanges = winterDyeReplacements = winterDyeRenewals = 0;
                winterDyeDips = winterDyeBlots = 0;
                winterDyeWarning = false;
                winterDyeFeedback = null;
                RestoreWinterVat();
                break;
        }
    }

    private void RestoreWinterVat()
    {
        winterVatDepth = StartingVatDepth;
        winterDyeWaterLeft = CurrentDyeRun.Water;
        winterDyeConcentrateLeft = CurrentDyeRun.Concentrate;
    }

    private void DrawWinterDye()
    {
        if (winterDyeSprites == null) winterDyeSprites = Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
        Layer("Winter_DyeSampleDesk");
        if (GameComplete)
        {
            DrawWinterDyeFinish();
            return;
        }

        if (!winterDyeSorted)
        {
            DrawWinterDyeBags();
            return;
        }

        if (!winterDyeGround)
        {
            DrawWinterGardeniaGrinding();
            return;
        }

        DrawWinterDyeDesk();
    }

    private void DrawWinterDyeBags()
    {
        var trayArt = GameArtSprite("OfferingTray");
        int ripeBatch = CurrentDyeRun.RipeBatch;
        for (int i = 0; i < 3; i++)
        {
            int bag = i;
            bool ripe = i == ripeBatch;
            bool bruised = i == (ripeBatch + 1) % 3;
            string source = ripe ? "新到批 · 红黄熟果" : bruised ? "分检批 · 裂果潮包" : "早采批 · 青绿果";
            Rect bounds = new Rect(75 + i * 350, 146, 310, 332);
            IllustratedGameCard(art, "Dye Sorting Tray " + i, bounds, trayArt, WinterWorktop);
            WorkshopUi.Picture(art, "Gardenia Batch " + i, new Rect(bounds.x + 46, bounds.y + 20, 218, 170),
                GameArtSprite(ripe ? "GardeniaRipe" : bruised ? "GardeniaDamaged" : "GardeniaGreen"));
            Tag("Lidong Batch Tag " + i, source, new Rect(bounds.x + 18, bounds.y + 185, 274, 45), bruised);
            WinterControl(art, "Lidong Bag " + i, new Rect(bounds.x + 32, bounds.y + 249, 246, 59),
                $"选这包果料", () => CheckWinterDyeBag(bag));
            var choose = WorkshopUi.Button(art, "Lidong Fruit Choice " + i,
                new Rect(bounds.x + 10, bounds.y + 10, 290, 176), "", () => CheckWinterDyeBag(bag), Color.clear);
            RegisterButton(choose);
            if (i == winterDyeRejectedBatch)
                DrawWinterMark(art, new Vector2(bounds.x + 245, bounds.y + 51), false);
            if (ripe && ChallengeVariant == 0)
            {
                VisualStep((RectTransform)choose.transform);
                ShowGesture(art, new Vector2(bounds.center.x + 42, bounds.y + 117), bounds.center, WorkGesture.Tap);
            }
        }

        Copy(winterDyeWarning ? "这批果料先留复查" : "挑选熟果",
            winterDyeWarning ? winterDyeFeedback :
            "选一包红黄熟果。", winterDyeWarning);
        if (winterDyeRejectedBatch >= 0)
        {
            bool damaged = winterDyeRejectedBatch == (ripeBatch + 1) % 3;
            DrawWinterSwatch(infoPanel, "Rejected Gardenia Test", new Rect(28, 354, 187, 96), damaged ? 3 : 1);
            WorkshopUi.Label(infoPanel, "Rejected Gardenia Result", new Rect(228, 354, 249, 96),
                damaged ? "裂果潮包\n样色不匀，留待复查" : "青果抽样\n色浅，够不上工单", 22, ErrorInk);
        }
        DrawWinterLoomSample();
    }

    private void CheckWinterDyeBag(int index)
    {
        if (GameComplete || winterDyeSorted) return;

        if (index == CurrentDyeRun.RipeBatch)
        {
            winterDyeSorted = true;
            winterDyeWarning = false;
            winterDyeFeedback = CurrentDyeRun.InOrder
                ? "红黄熟果已取料。先交浅栀黄，留好原液。"
                : "红黄熟果已取料。深浅栀黄共用这一缸。";
            WorkshopAudioFeedback.Place();
        }
        else
        {
            winterDyeWarning = true;
            winterDyeRejectedBatch = index;
            winterDyeFeedback = index == (CurrentDyeRun.RipeBatch + 1) % 3
                ? "这一包有裂果和水迹。换新批，找红黄熟果。"
                : "早采青果的试样偏浅。换成红黄熟果再试。";
            WorkshopAudioFeedback.Notice();
        }
        Render();
    }

    private void DrawWinterGardeniaGrinding()
    {
        var tools = Resources.Load<WinterGrindingConfig>("WinterGrindingConfig");
        IllustratedGameCard(art, "Lidong Grinding Table", new Rect(181, 77, 798, 475), GameArtSprite("WoodDesk"), WinterWorktop);
        WorkshopUi.Picture(art, "Lidong Gardenia Mortar", new Rect(329, 118, 423, 316), tools?.mortar);
        WorkshopUi.Picture(art, "Lidong Gardenia Rod", new Rect(643, 104, 194, 213), tools?.rod);
        WorkshopUi.Picture(art, "Ripe Gardenia for Grinding", new Rect(487, 189, 134, 121),
            GameArtSprite("GardeniaRipe"));
        var grind = WorkshopUi.Button(art, "Lidong Mortar Action", new Rect(329, 118, 423, 316), "",
            GrindWinterGardenia, Color.clear);
        RegisterButton(grind);
        VisualStep((RectTransform)grind.transform);
        ShowGesture(art, new Vector2(560, 263), new Vector2(560, 263), WorkGesture.Tap);
        WinterControl(art, "Lidong Grind", new Rect(432, 468, 296, 58), "点击石臼 · 研细果料", GrindWinterGardenia, true, 26);
        Copy("红黄熟果已选好", null);
        DrawWinterLoomSample();
    }

    private void GrindWinterGardenia()
    {
        if (!winterDyeSorted || winterDyeGround || GameComplete) return;
        winterDyeGround = true;
        WorkshopAudioFeedback.DyeIngredient();
        Render();
    }

    private void DrawWinterDyeDesk()
    {
        var deskArt = GameArtSprite("WoodDesk");
        var desk = IllustratedGameCard(art, "Lidong Dye Desk", new Rect(36, 44, 1088, 527), deskArt, WinterWorktop);
        winterDyeDesk = desk;
        winterDyeMotionCue = winterDyeFocus = winterDyeCurrentTarget = null;
        if (deskArt != null)
        {
            // Worktop labels remain on paper so the artwork can stay free of lettering.
            WorkshopUi.Fill(desk, "Order Heading Paper", new Rect(24, 20, 262, 47), WinterWorktop);
            WorkshopUi.Fill(desk, "Vat Heading Paper", new Rect(320, 20, 396, 47), WinterWorktop);
            WorkshopUi.Fill(desk, "Vat Strength Paper", new Rect(325, 294, 386, 43), WinterWorktop);
            WorkshopUi.Fill(desk, "Vat Note Paper", new Rect(321, 414, 394, 80), WinterWorktop);
        }
        WorkshopUi.Label(desk, "Orders Heading", new Rect(29, 20, 256, 47), "工单 · 干样色泽", 29);
        for (int i = 0; i < 2; i++) DrawWinterDyeOrder(desk, i);

        WorkshopUi.Label(desk, "Vat Heading", new Rect(320, 20, 396, 47), "共用染缸", 32, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        winterDyeVatTarget = WorkshopUi.Rect(desk, "Lidong Vat Drop", new Rect(326, 77, 384, 216));
        Rect vatArt = new Rect(64, 0, 257, 216);
        WorkshopUi.Picture(winterDyeVatTarget, "Water Vat", vatArt, winterDyeSprites?.clearWaterVat);
        Sprite vat = winterDyeSprites?.yellowVat;
        winterDyeLiquid = vat == null
            ? WorkshopUi.Fill(winterDyeVatTarget, "Vat Liquid", new Rect(65, 28, 254, 158), WinterVatRim)
            : WorkshopUi.Picture(winterDyeVatTarget, "Vat Liquid", vatArt, vat);
        var rippleArt = GameArtSprite("WaterRipple");
        Rect rippleBounds = rippleArt != null ? new Rect(112, 51, 158, 44) : new Rect(118, 66, 151, 5);
        winterDyeRipple = GameArtSurface(winterDyeVatTarget, "Dye Ripple", rippleBounds, rippleArt, Color.clear);
        winterDyeRipple.rectTransform.pivot = new Vector2(.5f, .5f);
        winterDyeRipple.rectTransform.anchoredPosition = new Vector2(rippleBounds.center.x, -rippleBounds.center.y);
        winterDyeVatLabel = WorkshopUi.Label(desk, "Vat Strength", new Rect(325, 294, 386, 43),
            "", 26, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        winterDyeWaterLabel = WinterControl(desk, "Lidong Dilute", new Rect(321, 346, 189, 62), "加清水", () => AdjustWinterVat(false), textSize: 24)
            .transform.Find("Label").GetComponent<Text>();
        winterDyeConcentrateLabel = WinterControl(desk, "Lidong Concentrate", new Rect(526, 346, 189, 62), "添原液", () => AdjustWinterVat(true), textSize: 24)
            .transform.Find("Label").GetComponent<Text>();
        WorkshopUi.Picture(desk, "Water Ingredient Symbol", new Rect(365, 406, 108, 68), SpringSummerArt("DewBead"));
        WorkshopUi.Picture(desk, "Concentrate Ingredient Symbol", new Rect(560, 405, 114, 76), GameArtSprite("GardeniaRipe"));
        WorkshopUi.Button(desk, "Pour Water Icon", new Rect(365, 406, 108, 68), "", () => AdjustWinterVat(false), Color.clear);
        WorkshopUi.Button(desk, "Pour Concentrate Icon", new Rect(560, 405, 114, 76), "", () => AdjustWinterVat(true), Color.clear);

        var hanger = WorkshopUi.PixelCard(desk, "Dye Hanger", new Rect(751, 79, 310, 416), LightSurface, WorkshopUi.Line);
        WorkshopUi.Label(hanger, "Hanger Heading", new Rect(16, 3, 278, 34), "手上的布头", 24, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        var dryRack = GameArtSprite("DyeDryRack");
        if (dryRack != null)
            WorkshopUi.Picture(hanger, "Dye Dry Rack", new Rect(28, 56, 254, 153), dryRack, stretch: true);
        var sampleBounds = new Rect(66, 71, 178, 135);
        winterDyeSwatch = DrawWinterSwatch(hanger, "Lidong Sample", sampleBounds, 0);
        var dragTarget = WorkshopUi.Fill(hanger, "Lidong Swatch Grip", sampleBounds, Color.clear);
        dragTarget.raycastTarget = true;
        BindWinterDyeDrag(dragTarget.gameObject);
        winterDyeSampleLabel = WorkshopUi.Label(hanger, "Swatch Depth", new Rect(16, 205, 278, 39), "", 23,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
        for (int i = 0; i < MaxVatDepth; i++)
            winterDyeDepthMarks[i] = WorkshopUi.Fill(hanger, "Dye Depth " + (i + 1),
                new Rect(29 + i * 42, 248, 36, 14), InsetPaper);
        winterDyeFineButton = WinterControl(hanger, "Lidong Fine Dip", new Rect(19, 270, 129, 63), "轻蘸", FineDipWinterDye, textSize: 23);
        winterDyeDipButton = WinterControl(hanger, "Lidong Dip", new Rect(162, 270, 129, 63), "浸染", DipWinterDye, textSize: 23);
        winterDyeFinePreview = DrawDyeActionPreview(hanger, winterDyeFineButton, 19);
        winterDyeDipPreview = DrawDyeActionPreview(hanger, winterDyeDipButton, 162);
        winterDyeHangButton = WinterControl(hanger, "Lidong Hang", new Rect(19, 343, 272, 57), "晾看", HangWinterDye, true, 25);
        winterDyeRackAction = WorkshopUi.Rect(hanger, "Dry Rack Action Area", new Rect(54, 37, 202, 54));
        WorkshopUi.Picture(winterDyeRackAction, "Hanging Rail", new Rect(0, 0, 202, 54), dryRack, stretch: true);
        var rackAction = WorkshopUi.Button(winterDyeRackAction, "Dry This Swatch", new Rect(0, 0, 202, 54), "", HangWinterDye, Color.clear);
        RegisterButton(rackAction);
        var dipVat = WorkshopUi.Button(winterDyeVatTarget, "Click Vat To Dip", new Rect(0, 0, 384, 216), "",
            DipWinterDye, Color.clear);
        RegisterButton(dipVat);

        var status = WorkshopUi.PixelCard(art, "Dye Feedback Plate", new Rect(36, 588, 1088, 49), LightSurface, WorkshopUi.Bronze);
        winterDyeStatus = WorkshopUi.Label(status, "Lidong Feedback", new Rect(18, 0, 1052, 49), "", 26,
            WorkshopUi.Green, TextAnchor.MiddleCenter);
        winterDyeDrop = DrawWinterSwatch(art, "Lidong Dragged Swatch", new Rect(0, 0, 110, 83), 0);
        winterDyeDrop.gameObject.SetActive(false);

        Copy("栀黄试色", WinterDyeSteps);
        winterDyeGuide = infoPanel.Find("Instruction Body").GetComponent<Text>();
        Act("Lidong Blot", "吸回上一次染色", BlotWinterDye);
        winterDyeBlotButton = infoPanel.Find("Lidong Blot").GetComponentInChildren<Button>();
        Act("Lidong New Swatch", "换这块布头", ReplaceWinterSwatch);
        winterDyeReplaceButton = infoPanel.Find("Lidong New Swatch").GetComponentInChildren<Button>();
        Act("Lidong Refill Vat", "补齐清水和原液", RenewWinterVat);
        winterDyeRefill = (RectTransform)infoPanel.Find("Lidong Refill Vat");
        DrawWinterLoomSample();
        RefreshWinterDye();
    }

    private Image DrawDyeActionPreview(RectTransform hanger, Button button, float x)
    {
        var label = button.transform.Find("Label").GetComponent<Text>();
        label.rectTransform.anchoredPosition = new Vector2(3, 0);
        label.rectTransform.sizeDelta = new Vector2(72, 59);
        WorkshopUi.PixelCard(hanger, "Result Swatch Border", new Rect(x + 75, 282, 42, 39), WorkshopUi.Cream, WorkshopUi.Bronze);
        return DrawWinterSwatch(hanger, "Predicted Dip Color", new Rect(x + 79, 286, 34, 30), 0);
    }

    private void DrawWinterLoomSample()
    {
        var work = OrderWork(WorkshopObservation.HanluLoom)
            ?? BestWork(WorkshopObservation.HanluLoom, ChallengeVariant)
            ?? BestWork(WorkshopObservation.HanluLoom, 1 - ChallengeVariant);
        if (!DrawWorkSample(infoPanel, new Rect(27, 512, 168, 86), work?.sample)) return;
        WorkshopUi.Label(infoPanel, "Earlier Loom Sample", new Rect(214, 512, 250, 86),
            "从寒露织好的布留样\n试色通过再染整幅", 23, WorkshopUi.Ink);
    }

    private void DrawWinterDyeOrder(RectTransform shelf, int index)
    {
        var order = WorkshopUi.PixelCard(shelf, "Lidong Order " + index, new Rect(24, 77 + index * 212, 262, 191),
            LightSurface, WorkshopUi.Line);
        winterDyeOrderEdges[index] = WorkshopUi.Fill(order, "Active Dye Order", new Rect(10, 5, 242, 7), GuideGold);
        var range = CurrentDyeRun.Ranges[index];
        DrawWinterSwatch(order, "Client Dye Sample", new Rect(15, 18, 110, 89), range.y);
        winterDyeOrderCloth[index] = DrawWinterSwatch(order, "Your Dye Sample", new Rect(137, 18, 110, 89), 0);
        winterDyeOrderLabels[index] = WorkshopUi.Label(order, "Dye Order Progress", new Rect(10, 109, 242, 39),
            "", 22, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        winterDyeOrderButtons[index] = WinterControl(order, "Lidong Choose Order " + index, new Rect(23, 149, 216, 36),
            "选「" + CurrentDyeRun.Names[index] + "」", () => SelectWinterDyeOrder(index), textSize: 20);
    }

    private void DrawWinterDyeFinish()
    {
        var rack = GameArtSprite("DyeDryRack");
        var linen = GameArtSprite("CuttingCloth");
        if (rack != null)
            WorkshopUi.Picture(art, "Finished Swatch Dry Rack", new Rect(142, 80, 876, 450), rack, stretch: true);
        for (int i = 0; i < 2; i++)
        {
            var bounds = new Rect(105 + i * 493, 135, 455, 384);
            var sample = rack != null
                ? WorkshopUi.Rect(art, "Lidong Finished Swatch " + i, bounds)
                : WorkshopUi.PixelCard(art, "Lidong Finished Swatch " + i, bounds, WinterWorktop, WorkshopUi.Bronze);
            Rect swatch = rack != null ? new Rect(i == 0 ? 150 : 61, -9, 215, 291) : new Rect(66, 42, 323, 242);
            var cloth = rack != null && linen != null
                ? WorkshopUi.Picture(sample, "Finished Dyed Cloth", swatch, linen, WinterDyeColor(winterDyeDepth[i]), stretch: true)
                : DrawWinterSwatch(sample, "Finished Dyed Cloth", swatch, winterDyeDepth[i]);
            if (rack != null) StartCoroutine(SettleWinterDyeSwatch(cloth));
            Rect label = rack != null ? new Rect(swatch.x + 18, 319, swatch.width - 36, 57) : new Rect(10, 293, 435, 57);
            if (rack != null) WorkshopUi.PixelCard(sample, "Dry Swatch Plaque", label, WinterWorktop, WorkshopUi.Bronze);
            WorkshopUi.Label(sample, "Dry Swatch Name", label,
                CurrentDyeRun.Names[i] + " · 已验", 33, WorkshopUi.Green, TextAnchor.MiddleCenter);
        }
        Copy("深浅两块布样已验", "试色小样会留给小雪排料参考。选好色后，还要用染缸给整匹素绸染色，再送到裁衣台。");
    }

    private Image DrawWinterSwatch(RectTransform parent, string name, Rect bounds, int depth)
    {
        if (winterDyeSprites != null && winterDyeSprites.silk != null)
            return WorkshopUi.Picture(parent, name, bounds, winterDyeSprites.silk, WinterDyeColor(depth));
        var clothArt = WorkshopObservationArt.Current?.DyedSwatch ?? GameArtSprite("CuttingCloth");
        return GameArtSurface(parent, name, bounds, clothArt, WinterDyeColor(depth));
    }

    private Color WinterDyeColor(int depth) => Color.Lerp(Color.white, CurrentDyeRun.Ink, depth / (float)MaxVatDepth);

    private void RefreshWinterDye()
    {
        for (int i = 0; i < 2; i++)
        {
            winterDyeOrderLabels[i].text = winterDyeAccepted[i] ? "已交 ✓" : "订单色     你的布";
            winterDyeOrderEdges[i].color = winterDyeAccepted[i] ? WorkshopUi.Green :
                i == winterDyeOrder ? GuideGold : WorkshopUi.Line;
            winterDyeOrderCloth[i].color = WinterDyeColor(winterDyeDepth[i]);
            winterDyeOrderButtons[i].interactable = !winterDyeAccepted[i] && (!CurrentDyeRun.InOrder || i == winterDyeOrder);
            var orderCard = (RectTransform)winterDyeOrderButtons[i].transform.parent.parent;
            if (winterDyeAccepted[i] && orderCard.Find("Swatch Accepted") == null)
                DrawWinterMark(orderCard, new Vector2(223, 36), true).name = "Swatch Accepted";
        }
        winterDyeLiquid.color = winterDyeLiquid.sprite == null
            ? Color.Lerp(WinterVatRim, WinterDyeColor(winterVatDepth), .64f)
            : new Color(1f, 1f, 1f, winterVatDepth / (float)MaxVatDepth);
        winterDyeVatLabel.text = $"染缸浓度 {winterVatDepth} / {MaxVatDepth}";
        winterDyeWaterLabel.text = $"调浅 · 水 {winterDyeWaterLeft}";
        winterDyeConcentrateLabel.text = $"调深 · 液 {winterDyeConcentrateLeft}";
        winterDyeWaterLabel.GetComponentInParent<Button>().interactable = winterDyeWaterLeft > 0 && winterVatDepth > 1;
        winterDyeConcentrateLabel.GetComponentInParent<Button>().interactable = winterDyeConcentrateLeft > 0 && winterVatDepth < MaxVatDepth;
        winterDyeSwatch.color = WinterDyeColor(winterDyeDepth[winterDyeOrder]);
        int depth = winterDyeDepth[winterDyeOrder];
        var target = CurrentDyeRun.Ranges[winterDyeOrder];
        int next = WinterDyeAfterDip(depth, false);
        int fine = WinterDyeAfterDip(depth, true);
        bool ready = depth >= target.x && depth <= target.y;
        winterDyeSampleLabel.text = $"当前 {depth} 阶 / 目标 {target.x}–{target.y} 阶";
        for (int i = 0; i < MaxVatDepth; i++)
        {
            bool within = i + 1 >= target.x && i + 1 <= target.y;
            winterDyeDepthMarks[i].color = i < depth ? (ready ? WorkshopUi.Green : WorkshopUi.Bronze) :
                within ? new Color32(151, 189, 150, 255) : InsetPaper;
        }
        winterDyeDipButton.interactable = next > depth && depth < target.y;
        winterDyeFineButton.interactable = fine > depth && depth < target.y;
        winterDyeDipPreview.color = WinterDyeColor(next);
        winterDyeFinePreview.color = WinterDyeColor(fine);
        winterDyeHangButton.interactable = depth > 0;
        winterDyeHangButton.transform.parent.gameObject.SetActive(depth > 0);
        winterDyeRefill.gameObject.SetActive(winterDyeWaterLeft == 0 || winterDyeConcentrateLeft == 0);
        bool canBlot = winterDyePreviousDepth[winterDyeOrder] >= 0;
        winterDyeBlotButton.interactable = canBlot;
        winterDyeBlotButton.transform.parent.gameObject.SetActive(canBlot);
        winterDyeReplaceButton.interactable = depth > 0;
        winterDyeReplaceButton.transform.parent.gameObject.SetActive(depth > target.y && !canBlot);
        winterDyeHangButton.transform.Find("Label").GetComponent<Text>().text = ready ? "晾干交样 ✓" : "晾看";
        winterDyeGuide.text = $"目标：{CurrentDyeRun.Names[winterDyeOrder]}\n{WinterDyeSteps}";
        var dryObject = winterDyeRackAction.Find("Dry This Swatch").GetComponent<Button>();
        dryObject.interactable = depth > 0;
        UpdateWinterDyeVisualGuide(depth, next, fine, target, ready);
        winterDyeStatus.text = winterDyeFeedback;
        winterDyeStatus.color = winterDyeWarning ? ErrorInk : WorkshopUi.Green;
    }

    private void UpdateWinterDyeVisualGuide(int depth, int next, int fine, Vector2Int target, bool ready)
    {
        RectTransform focus = null;
        bool dip = false;
        if (ready) focus = winterDyeRackAction;
        else if (depth > target.y) focus = (RectTransform)(winterDyePreviousDepth[winterDyeOrder] >= 0
            ? winterDyeBlotButton.transform : winterDyeReplaceButton.transform);
        else if (fine == depth) focus = (RectTransform)(winterDyeConcentrateLeft > 0
            ? winterDyeDesk.Find("Pour Concentrate Icon") : winterDyeRefill);
        else if (ChallengeVariant == 0 && winterDyeDips == 0 && next <= target.y)
        {
            focus = winterDyeVatTarget;
            dip = true;
        }
        if (winterDyeCurrentTarget == focus) return;
        winterDyeCurrentTarget = focus;
        HideWinterDyeCue(winterDyeFocus);
        HideWinterDyeCue(winterDyeMotionCue);
        if (focus == null) return;
        winterDyeFocus = VisualStep(focus);
        Vector2 end = WorkbenchPoint(art, focus);
        Vector2 start = dip ? WorkbenchPoint(art, winterDyeSwatch.rectTransform) : end;
        bool onSidebar = focus.IsChildOf(infoPanel);
        winterDyeMotionCue = onSidebar
            ? ShowGesture(infoPanel, WorkbenchPoint(infoPanel, focus), WorkbenchPoint(infoPanel, focus), WorkGesture.Tap)
            : ShowGesture(art, start, end, dip ? WorkGesture.Drag : WorkGesture.Tap);
    }

    private void HideWinterDyeCue(RectTransform cue)
    {
        if (cue == null) return;
        cue.gameObject.SetActive(false);
        Destroy(cue.gameObject);
    }

    private static RectTransform DrawWinterMark(RectTransform parent, Vector2 at, bool success)
    {
        var icon = WorkshopUi.Rect(parent, "Action Result Symbol", new Rect(at.x, at.y, 1, 1));
        Color ink = success ? new Color32(194, 240, 162, 255) : new Color32(255, 146, 125, 255);
        if (success)
        {
            DrawCueStroke(icon, new Vector2(-17, 0), new Vector2(-5, 13), 10, ink);
            DrawCueStroke(icon, new Vector2(-5, 13), new Vector2(21, -15), 10, ink);
        }
        else
        {
            DrawCueStroke(icon, new Vector2(-13, -13), new Vector2(13, 13), 9, ink);
            DrawCueStroke(icon, new Vector2(-13, 13), new Vector2(13, -13), 9, ink);
        }
        return icon;
    }

    private void SelectWinterDyeOrder(int order)
    {
        if (winterDyeAccepted[order] || (CurrentDyeRun.InOrder && order != winterDyeOrder)) return;

        winterDyeOrder = order;
        WinterDyeMessage("拿起「" + CurrentDyeRun.Names[order] + "」的布头。", WorkshopAudioFeedback.Fabric);
    }

    private static int DyeDipAmount(int vatDepth) => (vatDepth + 1) / 2;

    private int WinterDyeAfterDip(int depth, bool fine)
    {
        int gain = fine ? 1 : DyeDipAmount(winterVatDepth);
        return Mathf.Max(depth, Mathf.Min(winterVatDepth, depth + gain));
    }

    private void FineDipWinterDye() => ApplyWinterDye(true);

    private void DipWinterDye() => ApplyWinterDye(false);

    private void ApplyWinterDye(bool fine)
    {
        if (GameComplete || !winterDyeGround) return;

        int lastDepth = winterDyeDepth[winterDyeOrder];
        var target = CurrentDyeRun.Ranges[winterDyeOrder];
        if (lastDepth >= target.y)
        {
            WinterDyeMessage(lastDepth == target.y ? "颜色已经到位，点「晾干交样」收好这块。"
                : "这块颜色已过深。吸回最后一次染色，再试浅一些。", WorkshopAudioFeedback.Notice);
            return;
        }
        int colored = WinterDyeAfterDip(lastDepth, fine);
        if (colored <= lastDepth)
        {
            WinterDyeMessage("这缸已无法把这块布染深。可以晾看，或添一点原液。", WorkshopAudioFeedback.Notice, true);
            return;
        }

        winterDyePreviousDepth[winterDyeOrder] = lastDepth;
        winterDyeDepth[winterDyeOrder] = colored;
        winterDyeDips++;
        StartWinterDyeRipple();
        AnimateSpringSummerSprite(winterDyeSwatch, Vector2.zero, 1.07f, .19f);
        if (colored > target.y)
            WinterDyeMessage("颜色深了。吸回这一蘸，再选浅一点的预览色。", WorkshopAudioFeedback.Notice, true);
        else if (colored >= target.x)
            WinterDyeMessage("颜色对上了。把这块小样晾干交单。", WorkshopAudioFeedback.Place);
        else WinterDyeMessage("布头吃进了新颜色。再和左边的订单色比一比。", WorkshopAudioFeedback.DyeDip);
    }

    private void BlotWinterDye()
    {
        if (GameComplete || !winterDyeGround) return;
        int previous = winterDyePreviousDepth[winterDyeOrder];
        if (previous < 0) return;
        winterDyeDepth[winterDyeOrder] = previous;
        winterDyePreviousDepth[winterDyeOrder] = -1;
        winterDyeBlots++;
        WinterDyeMessage("吸掉了刚上去的颜色。其他小样和染缸保留。", WorkshopAudioFeedback.Fabric);
    }

    private void HangWinterDye()
    {
        if (GameComplete || !winterDyeGround) return;

        var range = CurrentDyeRun.Ranges[winterDyeOrder];
        int depth = winterDyeDepth[winterDyeOrder];
        if (depth < range.x)
        {
            WinterDyeMessage("干样偏浅，原布继续蘸。染劲不够时先添原液。", WorkshopAudioFeedback.Notice, true);
            return;
        }
        if (depth > range.y)
        {
            WinterDyeMessage("干样偏深。先吸回上一次染色，或换这块布头。", WorkshopAudioFeedback.Notice, true);
            return;
        }

        winterDyeAccepted[winterDyeOrder] = true;
        winterDyePreviousDepth[winterDyeOrder] = -1;
        if (winterDyeAccepted[0] && winterDyeAccepted[1])
        {
            int score = Mathf.Max(1, 100 - 3 * Mathf.Max(0, winterDyeDips - 3) - 3 * winterDyeBlots
                - 5 * winterDyeVatChanges - 9 * winterDyeReplacements - 18 * winterDyeRenewals);
            const string palette = "1994444";
            string swatches = new string(palette[winterDyeDepth[0]], 3) + "0" + new string(palette[winterDyeDepth[1]], 3);
            FinishChallenge(score, $"两色齐 · 上色 {winterDyeDips} 次 · 调缸 {winterDyeVatChanges} 次", $"{swatches}/{swatches}/{swatches}");
            if (HasOrderWork(WorkshopObservation.LidongDye))
                WinterLevelState.MarkDyeProduced(DyeColor.Yellow);
            return;
        }

        StartCoroutine(SettleWinterDyeSwatch(winterDyeOrderCloth[winterDyeOrder]));
        winterDyeOrder = 1 - winterDyeOrder;
        WinterDyeMessage("首块已验。取下一块布，使用同一缸染液。", WorkshopAudioFeedback.Progress);
    }

    private void AdjustWinterVat(bool strengthen)
    {
        if (GameComplete || !winterDyeSorted) return;

        if (strengthen && winterDyeConcentrateLeft > 0 && winterVatDepth < MaxVatDepth)
        {
            winterDyeConcentrateLeft--;
            winterVatDepth++;
        }
        else if (!strengthen && winterDyeWaterLeft > 0 && winterVatDepth > 1)
        {
            winterDyeWaterLeft--;
            winterVatDepth--;
        }
        else
        {
            WinterDyeMessage(strengthen ? "原液已用完，或者染劲已经最高。" : "清水已用完，或者染劲已经最低。",
                WorkshopAudioFeedback.Notice, true);
            return;
        }

        winterDyeVatChanges++;
        StartWinterDyeRipple();
        WinterDyeMessage(strengthen ? "染缸浓了一阶，接下来上色会深些。" : "染缸淡了一阶，接下来上色会浅些。", WorkshopAudioFeedback.DyeIngredient);
    }

    private void ReplaceWinterSwatch()
    {
        if (GameComplete || winterDyeDepth[winterDyeOrder] == 0) return;

        winterDyeDepth[winterDyeOrder] = 0;
        winterDyePreviousDepth[winterDyeOrder] = -1;
        winterDyeReplacements++;
        WinterDyeMessage("换上干净布头。上一块已验的小样留在工单上。", WorkshopAudioFeedback.Fabric);
    }

    private void RenewWinterVat()
    {
        if (GameComplete) return;

        winterDyeRenewals++;
        RestoreWinterVat();
        WinterDyeMessage("染缸和备用料已补齐。已验和正在试的布都留在架上。", WorkshopAudioFeedback.Fabric);
    }

    private void WinterDyeMessage(string message, Action sound, bool warning = false)
    {
        winterDyeFeedback = message;
        winterDyeWarning = warning;
        sound();
        RefreshWinterDye();
    }

    private void BindWinterDyeDrag(GameObject grip)
    {
        var events = grip.AddComponent<EventTrigger>();
        BindWinterEvent(events, EventTriggerType.PointerClick, data =>
        {
            if (data is PointerEventData pointer && pointer.button == PointerEventData.InputButton.Left && !pointer.dragging)
                DipWinterDye();
        });
        BindWinterEvent(events, EventTriggerType.BeginDrag, data =>
        {
            winterDyeDrop.color = winterDyeSwatch.color;
            winterDyeDrop.gameObject.SetActive(true);
            MoveWinterDyeDrag((PointerEventData)data);
        });
        BindWinterEvent(events, EventTriggerType.Drag, data => MoveWinterDyeDrag((PointerEventData)data));
        BindWinterEvent(events, EventTriggerType.EndDrag, data =>
        {
            var pointer = (PointerEventData)data;
            winterDyeDrop.gameObject.SetActive(false);
            if (RectTransformUtility.RectangleContainsScreenPoint(winterDyeVatTarget, pointer.position, pointer.pressEventCamera))
                DipWinterDye();
        });
    }

    private void MoveWinterDyeDrag(PointerEventData pointer)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera, out Vector2 local))
            winterDyeDrop.rectTransform.anchoredPosition = local - new Vector2(55f, -41f);
    }

    private void StartWinterDyeRipple()
    {
        if (winterDyeRippleMotion != null) StopCoroutine(winterDyeRippleMotion);
        winterDyeRippleMotion = StartCoroutine(AnimateWinterDyeRipple(winterDyeRipple));
    }

    private IEnumerator AnimateWinterDyeRipple(Image ripple)
    {
        const float duration = .37f;
        for (float elapsed = 0f; ripple != null && elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            ripple.color = new Color(1f, 1f, 1f, (1f - progress) * .88f);
            ripple.rectTransform.localScale = new Vector3(.46f + .54f * progress, 1f, 1f);
            yield return null;
        }
        if (ripple != null) ripple.color = Color.clear;
        winterDyeRippleMotion = null;
    }

    private IEnumerator SettleWinterDyeSwatch(Image cloth)
    {
        const float duration = .32f;
        var rect = cloth.rectTransform;
        Vector2 resting = rect.anchoredPosition;
        for (float elapsed = 0f; cloth != null && cloth.gameObject.activeInHierarchy && elapsed < duration;
             elapsed += Time.unscaledDeltaTime)
        {
            float remaining = 1f - elapsed / duration;
            rect.anchoredPosition = resting + new Vector2(0f, remaining * remaining * 13f);
            yield return null;
        }
        if (cloth != null) rect.anchoredPosition = resting;
    }

    private Button WinterControl(Transform parent, string name, Rect bounds, string label, Action action, bool primary = false, int textSize = 26)
    {
        var button = WorkshopUi.PixelButton(parent, name, bounds, label, action, primary, textSize, Accents[(int)WorkshopSeason.Winter]);
        RegisterButton(button);
        return button;
    }

    private static void BindWinterEvent(EventTrigger events, EventTriggerType type, Action<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => action(data));
        events.triggers.Add(entry);
    }
}
