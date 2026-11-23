using System;
using System.Collections;
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
        internal readonly int WetBag;
        internal readonly int Water;
        internal readonly int Concentrate;
        internal readonly bool InOrder;

        internal DyeRun(string[] names, Vector2Int[] ranges, Color ink, int wetBag, int water, int concentrate, bool inOrder)
        {
            Names = names;
            Ranges = ranges;
            Ink = ink;
            WetBag = wetBag;
            Water = water;
            Concentrate = concentrate;
            InOrder = inOrder;
        }
    }

    private static readonly DyeRun[] WinterDyeRuns =
    {
        new DyeRun(new[] { "深靛", "雾蓝" }, new[] { new Vector2Int(4, 5), new Vector2Int(1, 2) },
            new Color32(49, 76, 125, 255), 1, 2, 1, false),
        new DyeRun(new[] { "浅茶", "深赭" }, new[] { new Vector2Int(1, 2), new Vector2Int(5, 6) },
            new Color32(116, 56, 48, 255), 2, 1, 1, true)
    };
    private const int StartingVatDepth = 5;
    private const int MaxVatDepth = 6;
    private static readonly Color UndyedLinen = new Color32(237, 221, 188, 255);
    private static readonly Color WinterVatRim = new Color32(80, 69, 67, 255);
    private static readonly Color WinterWorktop = new Color32(244, 232, 207, 240);
    private readonly int[] winterDyeDepth = new int[2];
    private readonly bool[] winterDyeAccepted = new bool[2];
    private readonly Text[] winterDyeOrderLabels = new Text[2];
    private readonly Image[] winterDyeOrderCloth = new Image[2];
    private readonly Button[] winterDyeOrderButtons = new Button[2];
    private bool winterDyeSorted;
    private int winterDyeOrder;
    private int winterVatDepth;
    private int winterDyeWaterLeft;
    private int winterDyeConcentrateLeft;
    private int winterDyeVatChanges;
    private int winterDyeReplacements;
    private int winterDyeRenewals;
    private bool winterDyeWarning;
    private string winterDyeFeedback;
    private Text winterDyeStatus;
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
                winterDyeSorted = false;
                winterDyeOrder = winterDyeVatChanges = winterDyeReplacements = winterDyeRenewals = 0;
                winterDyeWarning = false;
                winterDyeFeedback = null;
                RestoreWinterVat();
                break;
            case WorkshopObservation.XiaoxueTailoring:
                ResetWinterCutting();
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

        DrawWinterDyeDesk();
    }

    private void DrawWinterDyeBags()
    {
        var trayArt = GameArtSprite("OfferingTray");
        for (int i = 0; i < 3; i++)
        {
            int bag = i;
            Rect bounds = new Rect(75 + i * 350, 146, 310, 332);
            IllustratedGameCard(art, "Dye Sorting Tray " + i, bounds, trayArt, WinterWorktop);
            Prop(i == CurrentDyeRun.WetBag ? "Winter_DyeSampleDamp" : "Winter_DyeSampleDry",
                new Rect(bounds.x + 27, bounds.y + 24, 256, 210));
            WinterControl(art, "Lidong Bag " + i, new Rect(bounds.x + 32, bounds.y + 247, 246, 59),
                $"分查 {i + 1} 号染袋", () => CheckWinterDyeBag(bag));
        }

        Copy(winterDyeWarning ? "这包外布还干爽" : "挑出受潮的染袋",
            "先看袋面水印，再调同一缸染液，做深浅两块小样。", winterDyeWarning);
    }

    private void CheckWinterDyeBag(int index)
    {
        if (GameComplete || winterDyeSorted) return;

        if (index == CurrentDyeRun.WetBag)
        {
            winterDyeSorted = true;
            winterDyeWarning = false;
            winterDyeFeedback = CurrentDyeRun.InOrder ? "先交浅样。留好原液，深样随后。" : "两单共用这一缸。先试一块布。";
            WorkshopAudioFeedback.Place();
        }
        else
        {
            winterDyeWarning = true;
            WorkshopAudioFeedback.Notice();
        }
        Render();
    }

    private void DrawWinterDyeDesk()
    {
        var deskArt = GameArtSprite("WoodDesk");
        var desk = IllustratedGameCard(art, "Lidong Dye Desk", new Rect(36, 44, 1088, 527), deskArt, WinterWorktop);
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
        Sprite vat = ChallengeVariant == 0 ? winterDyeSprites?.blueVat : winterDyeSprites?.redVat;
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
        WorkshopUi.Label(desk, "Vat Direction", new Rect(321, 420, 394, 82),
            "染缸会保留当前浓淡，下一块布也用这缸。", 26, WorkshopUi.Faded, TextAnchor.UpperCenter);

        var hanger = WorkshopUi.PixelCard(desk, "Dye Hanger", new Rect(751, 79, 310, 416), LightSurface, WorkshopUi.Line);
        WorkshopUi.Label(hanger, "Hanger Heading", new Rect(16, 13, 278, 47), "手上的布头", 28, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        var dryRack = GameArtSprite("DyeDryRack");
        if (dryRack != null)
            WorkshopUi.Picture(hanger, "Dye Dry Rack", new Rect(28, 56, 254, 153), dryRack, stretch: true);
        var sampleBounds = new Rect(66, 71, 178, 135);
        winterDyeSwatch = DrawWinterSwatch(hanger, "Lidong Sample", sampleBounds, 0);
        var dragTarget = WorkshopUi.Fill(hanger, "Lidong Swatch Grip", sampleBounds, Color.clear);
        dragTarget.raycastTarget = true;
        BindWinterDyeDrag(dragTarget.gameObject);
        winterDyeSampleLabel = WorkshopUi.Label(hanger, "Swatch Depth", new Rect(16, 212, 278, 55), "", 25,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
        WinterControl(hanger, "Lidong Dip", new Rect(20, 278, 127, 52), "短蘸", DipWinterDye, textSize: 23);
        WinterControl(hanger, "Lidong Hang", new Rect(160, 278, 130, 52), "晾看", HangWinterDye, true, 23);
        WinterControl(hanger, "Lidong New Swatch", new Rect(20, 343, 270, 51), "只换这块布头", ReplaceWinterSwatch, textSize: 24);

        var status = WorkshopUi.PixelCard(art, "Dye Feedback Plate", new Rect(36, 588, 1088, 49), LightSurface, WorkshopUi.Bronze);
        winterDyeStatus = WorkshopUi.Label(status, "Lidong Feedback", new Rect(18, 0, 1052, 49), "", 26,
            WorkshopUi.Green, TextAnchor.MiddleCenter);
        winterDyeDrop = DrawWinterSwatch(art, "Lidong Dragged Swatch", new Rect(0, 0, 110, 83), 0);
        winterDyeDrop.gameObject.SetActive(false);

        Copy("两块布共用一缸染液", "拖布头入缸短蘸，也可以点「短蘸」。选「晾看」和订单样比较。浅了能再蘸，过深可只换这块。");
        Act("Lidong Refill Vat", "换一缸并保留已交小样", RenewWinterVat);
        RefreshWinterDye();
    }

    private void DrawWinterDyeOrder(RectTransform shelf, int index)
    {
        var order = WorkshopUi.PixelCard(shelf, "Lidong Order " + index, new Rect(24, 77 + index * 212, 262, 191),
            LightSurface, WorkshopUi.Line);
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
        Copy("深浅两块都染成了", $"调缸 {winterDyeVatChanges} 次，换布 {winterDyeReplacements} 片，换缸 {winterDyeRenewals} 次。小样试出合适的色泽后，才能安排正式染布。");
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
            string rank = winterDyeAccepted[i] ? "已验" : i == winterDyeOrder ? "正在试" : "待试";
            var range = CurrentDyeRun.Ranges[i];
            winterDyeOrderLabels[i].text = $"{rank} · 需 {range.x} 至 {range.y} 阶";
            winterDyeOrderCloth[i].color = WinterDyeColor(winterDyeDepth[i]);
            winterDyeOrderButtons[i].interactable = !winterDyeAccepted[i] && (!CurrentDyeRun.InOrder || i == winterDyeOrder);
        }
        winterDyeLiquid.color = winterDyeLiquid.sprite == null
            ? Color.Lerp(WinterVatRim, WinterDyeColor(winterVatDepth), .64f)
            : new Color(1f, 1f, 1f, winterVatDepth / (float)MaxVatDepth);
        winterDyeVatLabel.text = $"染劲 {winterVatDepth} 阶 · 初蘸上 {DyeDipAmount(winterVatDepth)} 阶";
        winterDyeWaterLabel.text = $"清水 {winterDyeWaterLeft} 勺";
        winterDyeConcentrateLabel.text = $"原液 {winterDyeConcentrateLeft} 勺";
        winterDyeSwatch.color = WinterDyeColor(winterDyeDepth[winterDyeOrder]);
        winterDyeSampleLabel.text = $"{CurrentDyeRun.Names[winterDyeOrder]} · {winterDyeDepth[winterDyeOrder]} 阶";
        winterDyeStatus.text = winterDyeFeedback;
        winterDyeStatus.color = winterDyeWarning ? ErrorInk : WorkshopUi.Green;
    }

    private void SelectWinterDyeOrder(int order)
    {
        if (winterDyeAccepted[order] || (CurrentDyeRun.InOrder && order != winterDyeOrder)) return;

        winterDyeOrder = order;
        WinterDyeMessage("拿起「" + CurrentDyeRun.Names[order] + "」的布头。", WorkshopAudioFeedback.Fabric);
    }

    private static int DyeDipAmount(int vatDepth) => (vatDepth + 1) / 2;

    private void DipWinterDye()
    {
        if (GameComplete || !winterDyeSorted) return;

        int lastDepth = winterDyeDepth[winterDyeOrder];
        int colored = Mathf.Min(winterVatDepth, lastDepth + DyeDipAmount(winterVatDepth));
        if (colored <= lastDepth)
        {
            WinterDyeMessage("这缸已无法把这块布染深。可以晾看，或添一点原液。", WorkshopAudioFeedback.Notice, true);
            return;
        }

        winterDyeDepth[winterDyeOrder] = colored;
        StartWinterDyeRipple();
        WinterDyeMessage(lastDepth == 0 ? "布头已上色。晾看后和订单比一比。" : "这一蘸使布头更深了。", WorkshopAudioFeedback.DyeDip);
    }

    private void HangWinterDye()
    {
        if (GameComplete || !winterDyeSorted) return;

        var range = CurrentDyeRun.Ranges[winterDyeOrder];
        int depth = winterDyeDepth[winterDyeOrder];
        if (depth < range.x)
        {
            WinterDyeMessage("干样偏浅，原布继续蘸。染劲不够时先添原液。", WorkshopAudioFeedback.Notice, true);
            return;
        }
        if (depth > range.y)
        {
            WinterDyeMessage("干样偏深。换这一块布，先把染缸调淡一些。", WorkshopAudioFeedback.Notice, true);
            return;
        }

        winterDyeAccepted[winterDyeOrder] = true;
        if (winterDyeAccepted[0] && winterDyeAccepted[1])
        {
            int score = Mathf.Max(1, 100 - 5 * winterDyeVatChanges - 9 * winterDyeReplacements - 18 * winterDyeRenewals);
            string palette = ChallengeVariant == 0 ? "1955588" : "1974433";
            string swatches = new string(palette[winterDyeDepth[0]], 3) + "0" + new string(palette[winterDyeDepth[1]], 3);
            FinishChallenge(score, $"双色齐 · 调缸 {winterDyeVatChanges} 次 · 换布 {winterDyeReplacements} 片", $"{swatches}/{swatches}/{swatches}");
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
