using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private static readonly Color SummerWarning = new Color32(154, 74, 56, 255);
    private static readonly Color SummerUrgent = new Color32(242, 134, 98, 255);

    private const int CocoonLaneCount = 4;
    private const int CocoonBatchSlotCount = 9;
    private const char RoundCocoonInk = '4';
    private const char LongCocoonInk = '5';
    private const float CocoonLaneWidth = 266f;
    private const float CocoonGridHeight = 472f;
    private const float CocoonTileHeight = 104f;
    private const float CocoonRowSpacing = 112f;
    private const float CocoonFirstRowTop = 228f;
    private const float CocoonCatchLimit = 428f;
    private const float CocoonFeedbackDuration = .28f;
    private static readonly Color CocoonHarvestBasketTint = new Color32(173, 129, 92, 255);
    private static readonly Color CocoonHarvestWeaveTint = new Color32(105, 83, 66, 255);
    private static readonly Color CocoonTrack = new Color32(32, 59, 51, 255);
    private static readonly Color CocoonTrackAlternate = new Color32(38, 69, 59, 255);
    private static readonly Color CocoonLacquer = new Color32(23, 43, 36, 255);
    private static readonly Color CocoonBrass = new Color32(170, 128, 66, 255);
    private static readonly Color CocoonMutedGold = new Color32(225, 191, 133, 255);
    private static readonly Color CocoonPaper = new Color32(246, 230, 191, 255);
    private static readonly KeyCode[] CocoonLaneKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };
    private static readonly string[] CocoonProblemNames = { "过潮", "有破口", "未熟" };

    private sealed class CocoonOrder
    {
        internal readonly string Name;
        internal readonly int RoundCount;
        internal readonly int LongCount;
        internal readonly (int Lane, bool Long)[] Rows;

        internal CocoonOrder(string name, int roundCount, int longCount, params (int Lane, bool Long)[] rows)
        {
            Name = name;
            RoundCount = roundCount;
            LongCount = longCount;
            Rows = rows;
        }
    }

    private static readonly CocoonOrder[] CocoonOrders =
    {
        new CocoonOrder("织坊首单", 3, 2, (1, false), (3, true), (0, false), (2, true), (1, false),
            (0, true), (3, false), (2, true), (0, false), (3, true)),
        new CocoonOrder("细丝加单", 2, 4, (2, true), (0, false), (3, true), (1, true), (3, false),
            (0, true), (2, false), (1, true), (0, false), (3, true), (2, true), (0, false), (1, true), (3, false))
    };

    private bool showCocoonBatches;
    private bool cocoonRunStarted;
    private bool cocoonRunFailed;
    private int cocoonRowsCollected;
    private float cocoonTravel;
    private int cocoonLastLane;
    private float cocoonHitFlash;
    private string cocoonRunFeedback;
    private RectTransform[] cocoonFallingRows;
    private Image[] cocoonActiveEdges;
    private Image[] cocoonProgressMarks;
    private RectTransform cocoonGrid;
    private Sprite cocoonDrySprite;
    private readonly Image[] cocoonLaneButtons = new Image[CocoonLaneCount];
    private Image cocoonCatchLine;
    private Text cocoonRunProgress;
    private CocoonOrder ActiveCocoonOrder => CocoonOrders[ChallengeVariant];

    private void ResetCocoonRun()
    {
        cocoonRunStarted = false;
        cocoonRunFailed = false;
        cocoonRowsCollected = 0;
        cocoonTravel = 0f;
        cocoonLastLane = -1;
        cocoonHitFlash = 0f;
        cocoonRunFeedback = null;
    }

    private void DrawCocoonHarvestChallenge()
    {
        if (showCocoonBatches)
        {
            DrawSummerBatches();
            return;
        }

        Layer("Summer_CocoonBench");
        WorkshopUi.Fill(art, "Summer Worktop Shade", ArtBounds, new Color32(30, 32, 24, 72));
        if (GameComplete)
        {
            DrawCocoonRunResult();
            return;
        }

        var order = ActiveCocoonOrder;
        const float left = 48f;
        const float width = CocoonLaneCount * CocoonLaneWidth;
        cocoonDrySprite = SpringSprite("WorkshopObservation/Summer_CocoonIntact");
        DrawCocoonOrderBanner(left, width);
        DrawCocoonTracks(left, width, SpringSummerArt("CuttingCloth"));
        var harvestBasket = SpringSummerArt("CocoonBin");
        var reviewBasket = SpringSummerArt("CocoonTray");
        var clues = new[] { SpringSummerArt("DewBead"), SpringSprite("WorkshopObservation/Summer_CocoonReview"), SpringSummerArt("MulberryBranch") };
        cocoonFallingRows = new RectTransform[order.Rows.Length];
        cocoonActiveEdges = new Image[order.Rows.Length];
        for (int row = 0; row < order.Rows.Length; row++)
        {
            var strip = WorkshopUi.Rect(cocoonGrid, "Cocoon Row " + row, new Rect(0, 0, width, CocoonTileHeight));
            cocoonFallingRows[row] = strip;
            for (int lane = 0; lane < CocoonLaneCount; lane++)
                DrawFallingCocoon(strip, row, lane, harvestBasket, reviewBasket, clues);
        }

        if (cocoonRunFailed)
        {
            WorkshopUi.Fill(cocoonGrid, "Cocoon End Shade", new Rect(0, 0, width, CocoonGridHeight), new Color32(12, 28, 23, 126));
            var notice = WorkshopUi.WorkbenchCard(art, "Cocoon Run Failed", new Rect(258, 229, 644, 178), CocoonPaper, CocoonBrass);
            WorkshopUi.Picture(notice, "Cocoon Recovery Seal", new Rect(55, 23, 56, 52), SpringSummerArt("RecoverySeal"));
            WorkshopUi.Label(notice, "Cocoon Run Ended", new Rect(22, 17, 600, 61), "本轮结束", 42, SummerDark,
                TextAnchor.MiddleCenter);
            WorkshopUi.Fill(notice, "Cocoon Result Rule", new Rect(105, 81, 434, 2), WorkshopUi.Line);
            WorkshopUi.Label(notice, "Cocoon Retry Summary", new Rect(22, 91, 600, 63), cocoonRunFeedback, 26,
                SummerWarning, TextAnchor.MiddleCenter);
        }

        DrawCocoonInstructions(clues);
        RefreshCocoonRun();
    }

    private void DrawCocoonOrderBanner(float left, float width)
    {
        var order = ActiveCocoonOrder;
        var ribbon = WorkshopUi.PixelCard(art, "Cocoon Order Ribbon", new Rect(left, 18, width, 59), CocoonLacquer, CocoonBrass);
        WorkshopUi.Picture(ribbon, "Order Cocoon", new Rect(14, 10, 49, 40), cocoonDrySprite);
        var name = WorkshopUi.Label(ribbon, "Cocoon Order Name", new Rect(71, 0, 157, 59), order.Name, 29, CocoonPaper);
        name.fontStyle = FontStyle.Bold;
        WorkshopUi.Fill(ribbon, "Cocoon Order Divider", new Rect(223, 15, 1, 29), CocoonBrass);
        WorkshopUi.Label(ribbon, "Cocoon Order Requirements", new Rect(246, 0, 481, 59),
            $"交货　圆茧 {order.RoundCount} 枚 · 长茧 {order.LongCount} 枚", 25, CocoonPaper);
        cocoonRunProgress = WorkshopUi.Label(ribbon, "Cocoon Run Progress", new Rect(748, 3, 290, 36), "", 26,
            SummerGold, TextAnchor.MiddleRight);
        int count = order.Rows.Length;
        cocoonProgressMarks = new Image[count];
        const float progressWidth = 261f;
        float markStep = progressWidth / count;
        for (int i = 0; i < count; i++)
            cocoonProgressMarks[i] = WorkshopUi.Fill(ribbon, "Collected Cocoon Mark " + i,
                new Rect(777 + i * markStep, 44, markStep - 5f, 5f), CocoonTrackAlternate);
        UpdateCocoonRunProgress();
    }

    private void DrawCocoonTracks(float left, float width, Sprite cloth)
    {
        var bounds = new Rect(left - 14, 81, width + 28, CocoonGridHeight + 25);
        WorkshopUi.PixelCard(art, "Cocoon Track Frame", bounds, CocoonLacquer, CocoonBrass);
        WorkshopUi.Picture(art, "Mulberry Wood Track Frame", bounds, SpringSummerArt("WoodDesk"),
            new Color32(189, 158, 119, 255), stretch: true);
        cocoonGrid = WorkshopUi.Rect(art, "Cocoon Sorting Grid", new Rect(left, 93, width, CocoonGridHeight));
        cocoonGrid.gameObject.AddComponent<RectMask2D>();
        WorkshopUi.Fill(cocoonGrid, "Cocoon Track Lining", new Rect(0, 0, width, CocoonGridHeight), CocoonLacquer);
        for (int lane = 0; lane < CocoonLaneCount; lane++)
        {
            float x = lane * CocoonLaneWidth;
            var tone = lane % 2 == 0 ? CocoonTrack : CocoonTrackAlternate;
            var laneBounds = new Rect(x + 4, 0, CocoonLaneWidth - 8, CocoonGridHeight);
            WorkshopUi.Fill(cocoonGrid, "Cocoon Track " + lane, laneBounds, tone);
            WorkshopUi.Picture(cocoonGrid, "Silk Track Weave " + lane, laneBounds, cloth,
                new Color(tone.r * 1.48f, tone.g * 1.43f, tone.b * 1.56f, .74f), stretch: true).type = Image.Type.Tiled;
            for (int seam = 0; seam < 2; seam++)
            {
                float railX = x + (seam == 0 ? 11 : CocoonLaneWidth - 13);
                WorkshopUi.Fill(cocoonGrid, "Silk Track Selvedge", new Rect(railX, 0, 1, CocoonGridHeight),
                    new Color32(223, 191, 130, 64));
                for (int y = 15; y < CocoonCatchLimit; y += 32)
                    WorkshopUi.Fill(cocoonGrid, "Silk Track Stitch", new Rect(railX + 2, y, 2, 13), new Color32(233, 206, 153, 43));
            }
        }

        WorkshopUi.Fill(cocoonGrid, "Cocoon Catch Band", new Rect(0, CocoonCatchLimit, width, CocoonGridHeight - CocoonCatchLimit),
            new Color32(16, 34, 28, 180));
        cocoonCatchLine = WorkshopUi.Fill(cocoonGrid, "Cocoon Catch Line", new Rect(0, CocoonCatchLimit, width, 3), SummerGold);
        WorkshopUi.Label(cocoonGrid, "Cocoon Catch Limit Caption", new Rect(19, CocoonCatchLimit + 6, 160, 33),
            "收茧线", 21, CocoonMutedGold);

        var controls = WorkshopUi.PixelCard(art, "Cocoon Controls", new Rect(left, 580, width, 52), CocoonLacquer, CocoonBrass);
        for (int lane = 0; lane < CocoonLaneCount; lane++)
        {
            float x = lane * CocoonLaneWidth;
            if (lane > 0)
                WorkshopUi.Fill(controls, "Cocoon Lane Key Divider", new Rect(x, 12, 1, 29), CocoonBrass);
            var key = WorkshopUi.PixelCard(controls, "Cocoon Lane Key " + lane,
                new Rect(x + (CocoonLaneWidth - 56) * .5f, 7, 56, 37), CocoonPaper, CocoonBrass);
            cocoonLaneButtons[lane] = WorkshopUi.Fill(key, "Lane Key Feedback", new Rect(3, 3, 50, 30), CocoonPaper);
            var label = WorkshopUi.Label(key, "Lane Key", new Rect(0, 0, 56, 37), CocoonLaneKeys[lane].ToString(), 27,
                CocoonLacquer, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
        }
    }

    private void DrawFallingCocoon(RectTransform strip, int row, int lane, Sprite harvestBasket, Sprite reviewBasket, Sprite[] clues)
    {
        var entry = ActiveCocoonOrder.Rows[row];
        bool good = lane == entry.Lane;
        var tile = WorkshopUi.Rect(strip, $"Cocoon Tile {row},{lane}",
            new Rect(lane * CocoonLaneWidth + 8, 0, CocoonLaneWidth - 16, CocoonTileHeight));
        var tray = WorkshopUi.Rect(tile, "Cocoon Basket", new Rect(13, 4, 224, 96));
        var basketBounds = new Rect(0, 0, 224, 96);
        var basket = WorkshopUi.Picture(tray, "Cocoon Tile Basket", basketBounds, good ? harvestBasket : reviewBasket,
            good ? CocoonHarvestBasketTint : Color.white, stretch: true);
        basket.raycastTarget = true;
        var pick = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pick.callback.AddListener(data =>
        {
            if (data is PointerEventData pointer && pointer.button == PointerEventData.InputButton.Left)
                TapCocoonTile(row, lane);
        });
        basket.gameObject.AddComponent<EventTrigger>().triggers.Add(pick);

        if (good)
        {
            DrawCocoonBasketInlay(tray, basketBounds, harvestBasket);
            cocoonActiveEdges[row] = WorkshopUi.Fill(tray, "Next Cocoon Gold Edge", new Rect(48, 6, 128, 3), SummerGold);
            var number = WorkshopUi.PixelCard(tray, "Cocoon Order Badge", new Rect(12, 9, 32, 25), CocoonLacquer, SummerGold);
            WorkshopUi.Label(number, "Collection Sequence", new Rect(0, 0, 33, 26), (row + 1).ToString("00"), 18,
                SummerGold, TextAnchor.MiddleCenter);
            float shellWidth = entry.Long ? 84 : 66;
            WorkshopUi.Picture(tray, "Dry Cocoon", new Rect(29 + (84 - shellWidth) * .5f, 18, shellWidth, 60),
                cocoonDrySprite, stretch: entry.Long);
            var name = WorkshopUi.Label(tray, "Cocoon Condition", new Rect(104, 0, 94, 96), entry.Long ? "干爽长茧" : "干爽圆茧",
                21, WorkshopUi.Cream, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Bold;
            return;
        }

        int problem = (row + ChallengeVariant + (lane < entry.Lane ? lane : lane - 1)) % clues.Length;
        Color tint = problem == 0 ? new Color32(153, 194, 193, 255) :
            problem == 2 ? new Color32(191, 195, 133, 255) : new Color32(211, 194, 174, 255);
        WorkshopUi.Picture(tray, "Problem Cocoon", new Rect(33, 16, 83, 64), problem == 1 ? clues[problem] : cocoonDrySprite, tint);
        if (problem != 1)
            WorkshopUi.Picture(tray, "Problem Mark", new Rect(89, 47, 26, 23), clues[problem]);
        WorkshopUi.Label(tray, "Cocoon Condition", new Rect(104, 0, 94, 96), CocoonProblemNames[problem],
            22, CocoonLacquer, TextAnchor.MiddleCenter);
    }

    private static void DrawCocoonBasketInlay(RectTransform tray, Rect bounds, Sprite basket)
    {
        var inset = new Rect(29, 18, 166, 60);
        var well = WorkshopUi.Rect(tray, "Dark Basket Well", inset);
        well.gameObject.AddComponent<RectMask2D>();
        WorkshopUi.Picture(well, "Dark Basket Weave", new Rect(-inset.x, -inset.y, bounds.width, bounds.height),
            basket, CocoonHarvestWeaveTint, stretch: true);
        WorkshopUi.Fill(tray, "Basket Gold Lower Trim", new Rect(40, 78, 144, 2), CocoonMutedGold);
        for (int side = 0; side < 2; side++)
        {
            var inlay = WorkshopUi.Fill(tray, "Basket Gold Side Inlay", new Rect(side == 0 ? 23 : 201, 43, 8, 8), SummerGold);
            inlay.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
        }
    }

    private void DrawCocoonInstructions(Sprite[] clues)
    {
        WorkshopUi.Fill(infoPanel, "Cocoon Instruction Gold Rule", new Rect(28, 40, 5, 36), WorkshopUi.Bronze);
        var heading = WorkshopUi.Label(infoPanel, "Instruction Heading", new Rect(48, 27, 421, 60),
            cocoonRunFailed ? "再试一轮" : "收茧要领", 39, SummerDark);
        heading.fontStyle = FontStyle.Bold;

        var directions = WorkshopUi.PixelCard(infoPanel, "Cocoon Directions", new Rect(28, 102, 442, 196),
            LightSurface, WorkshopUi.Line);
        DrawCocoonInstruction(directions, 1, "从最下方一排开始");
        DrawCocoonInstruction(directions, 2, "点深色篮，或按轨道键");
        DrawCocoonInstruction(directions, 3, "点错或好茧过线即结束");

        DrawCocoonConditionGuide(clues);
        if (cocoonRunStarted && !cocoonRunFailed) return;

        var start = WorkshopUi.PixelButton(infoPanel, "Start Cocoon Run", new Rect(28, 542, 442, 76),
            cocoonRunFailed ? "再试一次" : "开始收茧", () =>
            {
                WorkshopAudioFeedback.Click();
                StartCocoonRun();
            }, true, 32, SummerDark);
        RegisterButton(start);
        preferredAction = start;
    }

    private static void DrawCocoonInstruction(Transform parent, int number, string instruction)
    {
        float y = 14 + (number - 1) * 56f;
        var marker = WorkshopUi.PixelCard(parent, "Cocoon Step " + number, new Rect(16, y + 6, 36, 36),
            SummerDark, WorkshopUi.Bronze);
        WorkshopUi.Label(marker, "Step Number", new Rect(0, 0, 36, 36), number.ToString(), 23,
            SummerGold, TextAnchor.MiddleCenter);
        WorkshopUi.Label(parent, "Step Instruction " + number, new Rect(67, y, 355, 48),
            instruction, 27, SummerDark);
    }

    private void DrawCocoonConditionGuide(Sprite[] clues)
    {
        var title = WorkshopUi.Label(infoPanel, "Cocoon Condition Legend", new Rect(29, 321, 441, 38),
            "浅色篮里的问题茧 · 跳过", 26, SummerDark);
        title.fontStyle = FontStyle.Bold;
        var guide = WorkshopUi.Rect(infoPanel, "Cocoon Condition Guide", new Rect(28, 371, 442, 148));
        WorkshopUi.Picture(guide, "Bamboo Condition Tray", new Rect(0, 0, 442, 148),
            SpringSummerArt("CocoonTray"), stretch: true);
        for (int i = 0; i < clues.Length; i++)
        {
            float x = 62 + i * 106f;
            WorkshopUi.Picture(guide, "Cocoon Condition Mark " + i, new Rect(x + 26, 34, 54, 48), clues[i]);
            WorkshopUi.Label(guide, "Cocoon Condition Hint " + i, new Rect(x, 84, 106, 34), CocoonProblemNames[i], 25,
                SummerDark, TextAnchor.MiddleCenter);
        }
    }

    private void StartCocoonRun()
    {
        if (WorkshopMinigameMenu.BlocksGameplayInput || GameComplete || showBriefing || readOnly ||
            (cocoonRunStarted && !cocoonRunFailed)) return;
        ResetCocoonRun();
        cocoonRunStarted = true;
        Render();
    }

    private float CocoonRowTop(int row) => CocoonFirstRowTop + cocoonTravel - row * CocoonRowSpacing;

    private void TickCocoonRun()
    {
        if (observation != WorkshopObservation.MangzhongCocoonHarvest || !cocoonRunStarted ||
            cocoonRunFailed || GameComplete) return;
        for (int lane = 0; lane < CocoonLaneKeys.Length; lane++)
        {
            if (!Input.GetKeyDown(CocoonLaneKeys[lane])) continue;
            TapCocoonTile(cocoonRowsCollected, lane);
            break;
        }
        AdvanceCocoonRun(Mathf.Min(Time.unscaledDeltaTime, .05f));
    }

    private void AdvanceCocoonRun(float seconds)
    {
        if (!cocoonRunStarted || cocoonRunFailed || GameComplete || showBriefing) return;
        float speed = (ChallengeVariant == 0 ? 80f : 104f) + cocoonRowsCollected * 4f;
        cocoonTravel += seconds * speed;
        cocoonHitFlash = Mathf.Max(0f, cocoonHitFlash - seconds);
        float overshoot = CocoonRowTop(cocoonRowsCollected) - CocoonCatchLimit;
        if (overshoot >= 0f)
        {
            cocoonTravel -= overshoot;
            FailCocoonRun("漏接了一枚好茧，赶在金线前收取。");
            return;
        }
        RefreshCocoonRun();
    }

    private void TapCocoonTile(int row, int lane)
    {
        if (WorkshopMinigameMenu.BlocksGameplayInput || !cocoonRunStarted || cocoonRunFailed || GameComplete ||
            showBriefing || row < cocoonRowsCollected || CocoonRowTop(row) + CocoonTileHeight < 30f) return;
        if (row != cocoonRowsCollected)
        {
            FailCocoonRun("请先收最下方深色篮里的好茧。");
            return;
        }
        var order = ActiveCocoonOrder;
        if (order.Rows[row].Lane != lane)
        {
            FailCocoonRun("点到了浅色篮里的问题茧。");
            return;
        }

        cocoonRowsCollected++;
        if (cocoonRowsCollected == order.Rows.Length)
        {
            int shipped = order.RoundCount + order.LongCount;
            FinishChallenge(110 + cocoonRowsCollected,
                $"连收 {cocoonRowsCollected} 枚 · 交货 {shipped} 枚 · 备存 {cocoonRowsCollected - shipped} 枚", CaptureDeliveredCocoons());
            return;
        }

        WorkshopAudioFeedback.Place();
        cocoonLastLane = lane;
        cocoonHitFlash = CocoonFeedbackDuration;
        ShowCocoonPickup(lane, CocoonRowTop(row));
        UpdateCocoonRunProgress();
        RefreshCocoonRun();
    }

    private void ShowCocoonPickup(int lane, float top)
    {
        float left = (lane + .5f) * CocoonLaneWidth - 36f;
        float startY = Mathf.Clamp(top + 22f, 4f, CocoonCatchLimit - 34f);
        var pickup = WorkshopUi.Rect(cocoonGrid, "Collected Cocoon Effect", new Rect(left, startY, 72, 57));
        var fade = pickup.gameObject.AddComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        WorkshopUi.Picture(pickup, "Collected Cocoon Shell", new Rect(9, 2, 54, 53), cocoonDrySprite);
        WorkshopUi.Fill(pickup, "Pickup Glint", new Rect(57, 1, 4, 16), SummerGold);
        WorkshopUi.Fill(pickup, "Pickup Glint Cross", new Rect(51, 7, 16, 4), SummerGold);
        StartCoroutine(AnimateCocoonPickup(pickup, fade, left, startY));
    }

    private static IEnumerator AnimateCocoonPickup(RectTransform pickup, CanvasGroup fade, float left, float startY)
    {
        for (float time = 0f; time < CocoonFeedbackDuration;
             time += WorkshopMinigameMenu.BlocksGameplayInput ? 0f : Time.unscaledDeltaTime)
        {
            if (pickup == null || !pickup.gameObject.activeInHierarchy) yield break;
            float fraction = time / CocoonFeedbackDuration;
            float y = Mathf.Lerp(startY, CocoonCatchLimit - 15f, fraction * fraction);
            pickup.anchoredPosition = new Vector2(left + 20f * fraction, -y);
            pickup.localScale = Vector3.one * Mathf.Lerp(1f, .45f, fraction);
            fade.alpha = 1f - fraction;
            yield return null;
        }
        if (pickup != null) Destroy(pickup.gameObject);
    }

    private void FailCocoonRun(string feedback)
    {
        cocoonRunFailed = true;
        cocoonRunFeedback = feedback;
        WorkshopAudioFeedback.Notice();
        Render();
    }

    private void RefreshCocoonRun()
    {
        for (int row = 0; row < cocoonFallingRows.Length; row++)
        {
            var strip = cocoonFallingRows[row];
            float top = CocoonRowTop(row);
            bool visible = row >= cocoonRowsCollected && top > -CocoonTileHeight;
            if (strip.gameObject.activeSelf != visible) strip.gameObject.SetActive(visible);
            if (visible) strip.anchoredPosition = new Vector2(0, -top);
            cocoonActiveEdges[row].enabled = row == cocoonRowsCollected;
        }
        float warning = Mathf.InverseLerp(CocoonCatchLimit - 75f, CocoonCatchLimit, CocoonRowTop(cocoonRowsCollected));
        cocoonCatchLine.color = Color.Lerp(SummerGold, SummerUrgent, warning);
        for (int lane = 0; lane < CocoonLaneCount; lane++)
            cocoonLaneButtons[lane].color = lane == cocoonLastLane
                ? Color.Lerp(CocoonPaper, SummerGold, cocoonHitFlash / CocoonFeedbackDuration) : CocoonPaper;
    }

    private void UpdateCocoonRunProgress()
    {
        cocoonRunProgress.text = $"已收 {cocoonRowsCollected} / {ActiveCocoonOrder.Rows.Length}";
        for (int i = 0; i < cocoonProgressMarks.Length; i++)
            cocoonProgressMarks[i].color = i < cocoonRowsCollected ? SummerGold : CocoonTrackAlternate;
    }

    private string CaptureDeliveredCocoons()
    {
        var order = ActiveCocoonOrder;
        return new string(RoundCocoonInk, order.RoundCount) + new string(LongCocoonInk, order.LongCount) +
            new string('0', CocoonBatchSlotCount - order.RoundCount - order.LongCount);
    }

    private void DrawCocoonRunResult()
    {
        DrawCocoonBatchResult(CaptureDeliveredCocoons(), runResult);
        Copy("合格春茧已收好", "干爽的成熟春茧已经配批，余下的留作备用。本次练习已记入织录。");
        Act("Compare Nanxun Batches", "看南浔的两批蚕", ToggleSummerBatches, true);
    }

    private bool DrawCocoonBatchResult(string batch, string summary)
    {
        if (batch == null || batch.Length != CocoonBatchSlotCount) return false;
        int count = 0;
        foreach (char ink in batch)
            if (ink == RoundCocoonInk || ink == LongCocoonInk) count++;
        if (count == 0) return false;

        var result = WorkshopUi.WorkbenchCard(art, "Completed Cocoon Delivery", new Rect(225, 140, 710, 359),
            CocoonPaper, CocoonBrass);
        WorkshopUi.Picture(result, "Cocoon Harvest Seal", new Rect(78, 29, 56, 55), SpringSummerArt("SuccessSeal"));
        WorkshopUi.Label(result, "Cocoon Order Finished", new Rect(30, 23, 650, 67), "合格春茧已干存", 44, SummerDark,
            TextAnchor.MiddleCenter);
        WorkshopUi.Picture(result, "Woven Delivery Tray", new Rect(49, 97, 612, 176), SpringSummerArt("CocoonBin"), stretch: true);
        var cocoon = SpringSprite("WorkshopObservation/Summer_CocoonIntact");
        float slotWidth = Mathf.Min(99f, 620f / count);
        int shown = 0;
        foreach (char ink in batch)
        {
            if (ink != RoundCocoonInk && ink != LongCocoonInk) continue;
            bool longShape = ink == LongCocoonInk;
            float left = (710f - count * slotWidth) * .5f + shown * slotWidth;
            float shellWidth = slotWidth * (longShape ? .92f : .69f);
            WorkshopUi.Picture(result, "Delivered Cocoon " + shown,
                new Rect(left + (slotWidth - shellWidth) * .5f, 133, shellWidth, 72), cocoon, stretch: true);
            var tag = WorkshopUi.PixelCard(result, "Cocoon Shape Paper " + shown,
                new Rect(left + (slotWidth - 67f) * .5f, 214, 67, 29), CocoonPaper, CocoonBrass);
            WorkshopUi.Label(tag, "Delivered Cocoon Shape " + shown, new Rect(0, 0, 67, 29), longShape ? "长茧" : "圆茧",
                24, SummerDark, TextAnchor.MiddleCenter);
            shown++;
        }
        WorkshopUi.Label(result, "Cocoon Run Result", new Rect(28, 281, 654, 52), summary, 30, SummerDark, TextAnchor.MiddleCenter);
        return true;
    }

    private void DrawCocoonHarvestRecord()
    {
        if (showCocoonBatches)
        {
            DrawSummerBatches();
            return;
        }

        Layer("Summer_CocoonBench");
        var work = BestWork(WorkshopObservation.MangzhongCocoonHarvest, 1) ??
            BestWork(WorkshopObservation.MangzhongCocoonHarvest, 0);
        DrawCocoonBatchResult(work?.sample, work?.description);
        Copy("春茧收存记录", "合格干茧按圆茧和长茧配批。也可以看看南浔的两批春蚕。");
        Act("Compare Nanxun Batches", "看南浔的两批蚕", ToggleSummerBatches, true);
    }

    private void DrawSummerBatches()
    {
        Layer("Summer_CocoonBench");
        var first = new Rect(89, 111, 445, 410);
        var second = new Rect(625, 111, 445, 410);
        WorkshopUi.PixelCard(art, "Early Batch Frame", first, LightSurface, WorkshopUi.Bronze);
        WorkshopUi.PixelCard(art, "Later Batch Frame", second, LightSurface, WorkshopUi.Bronze);
        Art("First Batch Cocoons", "WorkshopFinish/Cocoon", new Rect(184, 169, 255, 260));
        Prop("Spring_LeafOnTray", new Rect(691, 173, 313, 246));
        Tag("Early Batch Caption", "第一批 · 吐丝结茧", new Rect(118, 454, 387, 50));
        Tag("Later Batch Caption", "第二批 · 开始养殖", new Rect(654, 454, 387, 50));
        Copy("同在南浔，进度各有不同", "六月初的报道记录了当地具体蚕农的批次。两张画面示意同期的两种进度。");
        Act("Return To Cocoon Comparison", "返回收茧结果", ToggleSummerBatches, true);
        Act("Read Nanxun Reporting", "阅读南浔地方蚕讯", () => Application.OpenURL(
            "https://paper.xinmin.cn/html/home/2024-06/05/06/22494.html"));
    }

    private void ToggleSummerBatches()
    {
        showCocoonBatches = !showCocoonBatches;
        Render();
    }

}
