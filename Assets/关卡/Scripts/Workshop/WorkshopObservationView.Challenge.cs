using System.Collections;
using UnityEngine;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private enum ChallengePhase { None, Playing, Complete }

    private ChallengePhase challengePhase;
    private bool ChallengeIsActive => challengePhase != ChallengePhase.None;
    private bool GameComplete => challengePhase == ChallengePhase.Complete;
    private int ChallengeVariant { get; set; }
    private string runResult;
    private bool stampCurrentResult;

    private static readonly (string name, string icon)[] ChallengeRewards =
    {
        ("嫩桑标本", "WorkshopObservation/Spring_QingmingYoungMulberry"),
        ("整经丝束", "WorkshopFinish/RawSilk"),
        (null, null),
        ("生丝验收签", "WorkshopGameArt/SilkBobbin"),
        ("平纹织样", "WorkshopFinish/PlainSilk"),
        ("双染试色布头", "WorkshopObservation/Winter_DyedSwatch"),
        ("节用裁衣纸样", null),
        ("夹衣缝样", "WorkshopGameArt/WeftTile"),
        ("冬至验衣签", "WorkshopGameArt/SuccessSeal"),
        ("完整蚕茧", "WorkshopObservation/Summer_CocoonIntact")
    };

    internal static string RewardName(WorkshopObservation id) => ChallengeRewards[(int)id].name;
    internal static Sprite RewardIcon(WorkshopObservation id)
    {
        if (id == WorkshopObservation.XiaoxueTailoring) return WorkshopArt.CutPiece(0);
        return Resources.Load<Sprite>(ChallengeRewards[(int)id].icon);
    }
    internal static string RecordTerm(WorkshopObservation id) => Definitions[(int)id].Term;
    internal static WorkshopProgress.ChallengeWork BestWork(WorkshopObservation id, int order) =>
        WorkshopProgress.BestChallengeWork(Definitions[(int)id].Id, order);
    internal static string OrderTitle(int order) => order == 0 ? "练习单" : "挑战单";

    private static readonly Color32[] SampleColors =
    {
        new Color32(0, 0, 0, 0), new Color32(223, 201, 157, 255), new Color32(84, 130, 91, 255),
        new Color32(167, 83, 73, 255), new Color32(199, 153, 78, 255), new Color32(91, 127, 150, 255),
        new Color32(101, 82, 119, 255), new Color32(230, 187, 170, 255), new Color32(51, 69, 99, 255),
        new Color32(246, 235, 221, 255)
    };

    private RectTransform DrawWorkshopPaper(Transform parent, string name, Rect bounds, Color paper, Color edge)
        => ChallengeIsActive ? WorkshopUi.WorkbenchCard(parent, name, bounds, paper, edge)
            : WorkshopUi.PixelCard(parent, name, bounds, paper, edge);

    internal static bool DrawWorkSample(Transform parent, Rect area, string sample)
    {
        if (string.IsNullOrEmpty(sample)) return false;
        string[] lines = sample.Split('/');
        int columns = lines[0].Length;
        if (columns < 1 || columns > 16 || lines.Length > 12) return false;
        foreach (string line in lines)
        {
            if (line.Length != columns) return false;
            foreach (char cell in line)
                if (cell < '0' || cell > '9') return false;
        }

        float edge = Mathf.Floor(Mathf.Min(area.width / columns, area.height / lines.Length));
        float left = Mathf.Round(area.center.x - columns * edge * 0.5f);
        float top = Mathf.Round(area.center.y - lines.Length * edge * 0.5f);
        var fabric = Resources.Load<Sprite>("WorkshopGameArt/CuttingCloth");
        for (int row = 0; row < lines.Length; row++)
            for (int col = 0; col < columns; col++)
            {
                int ink = lines[row][col] - '0';
                if (ink == 0) continue;
                var bounds = new Rect(left + edge * col, top + edge * row, edge, edge);
                if (fabric != null) WorkshopUi.Picture(parent, $"Sample {row},{col}", bounds, fabric, SampleColors[ink], true);
                else WorkshopUi.Fill(parent, $"Sample {row},{col}", bounds, SampleColors[ink]);
            }
        return true;
    }

    private void PrepareActivity(WorkshopObservation id)
    {
        observation = id;
        challengePhase = ChallengePhase.None;
        stage = readOnly ? CompletionStage(id) : 0;
        runResult = null;
        earnedRewardThisRun = false;
        stampCurrentResult = false;
        if (!readOnly && IsSeasonGame(id))
        {
            bool hasFirstOrder = WorkshopProgress.SeasonalOrderEnabled && id != WorkshopObservation.MangzhongCocoonHarvest
                ? HasOrderWork(id) : BestWork(id, 0) != null;
            PrepareChallenge(!IsFormalGarmentOrder && hasFirstOrder ? 1 : 0);
        }
    }

    private void PrepareChallenge(int order)
    {
        ChallengeVariant = order;
        challengePhase = ChallengePhase.Playing;
        stage = 0;
        earnedRewardThisRun = false;
        runResult = null;
        stampCurrentResult = false;
        ResetSummerChallenge();
        ResetWinterChallenge();
        if (observation == WorkshopObservation.DaxueSewing) ResetSewingChallenge();
        if (observation == WorkshopObservation.DongzhiFitting) ResetFittingChallenge();
    }

    private void DrawChallengeStatus(Color accent)
    {
        string suffix = GameComplete ? "已完成" : HasRecord(observation) ? " · 残页已收集" : " · 残页待收集";
        WorkshopUi.Label(page, GameComplete ? "Challenge Completed" : "Challenge Playing", new Rect(1140, 68, 566, 39),
            ChallengeOrderLabel() + suffix, 30, accent, TextAnchor.MiddleRight);
    }

    private bool IsGarmentProductionRun => IsFormalGarmentOrder;

    private string ChallengeOrderLabel() => IsDirectDyeing ? "比例染色" :
        IsGarmentProductionRun ? "贺冬工单" : OrderTitle(ChallengeVariant);

    private void DrawChallengeReward()
    {
        if (challengePhase == ChallengePhase.Playing) return;

        if (readOnly)
        {
            var delivery = OrderWork(observation);
            var first = delivery ?? BestWork(observation, 0);
            var extra = BestWork(observation, 1);
            if (first != null || extra != null)
            {
                DrawArchivedOrder(0, first, delivery != null ? "主线工单" : null);
                DrawArchivedOrder(1, extra);
                return;
            }
        }

        var reward = OrderReceipt("Challenge Reward", new Rect(28, 848, 1172, 60));
        WorkshopUi.Picture(reward, "Reward Item", new Rect(14, 5, 66, 50), RewardIcon(observation));
        string prefix = earnedRewardThisRun ? "首次获得" : "已收藏";
        string award = $"{prefix} · {RewardName(observation)} ×1 ＋ {RecordTerm(observation)}残页 ×1";
        var best = BestWork(observation, 1) ?? BestWork(observation, 0);
        string result = GameComplete ? runResult : best != null ? "佳作 · " + best.description : null;
        string contents = earnedRewardThisRun || string.IsNullOrEmpty(result) ? award : result;
        if (!GameComplete)
        {
            WorkshopUi.Label(reward, "Reward Contents", new Rect(90, 0, 1055, 60), contents, 26, WorkshopUi.Green);
            return;
        }

        if (earnedRewardThisRun)
            WorkshopUi.Label(reward, "Work Order Result", new Rect(94, 1, 945, 28), result, 23, WorkshopUi.Ink);
        else
            contents += " · 残页已收藏";
        WorkshopUi.Label(reward, "Reward Contents", new Rect(94, earnedRewardThisRun ? 28 : 0, 945, earnedRewardThisRun ? 30 : 60),
            contents, 23, WorkshopUi.Green);
        var seal = WorkshopUi.Picture(reward, "Workshop Success Seal", new Rect(1095, 6, 48, 48),
            Resources.Load<Sprite>("WorkshopGameArt/SuccessSeal"));
        var stamp = seal.rectTransform;
        stamp.pivot = new Vector2(0.5f, 0.5f);
        stamp.anchoredPosition = new Vector2(1119f, -30f);
        stamp.localEulerAngles = new Vector3(0, 0, -9f);
        if (stampCurrentResult && seal.enabled) StartCoroutine(StampReceipt(seal));
        stampCurrentResult = false;
    }

    private RectTransform OrderReceipt(string name, Rect bounds)
    {
        var paper = WorkshopUi.PixelCard(page, name, bounds, LightSurface, WorkshopUi.Bronze);
        var artwork = Resources.Load<Sprite>("WorkshopGameArt/ResultSlip");
        if (artwork != null)
        {
            var layer = WorkshopUi.Picture(paper, "Work Order Paper", new Rect(0, 0, bounds.width, bounds.height), artwork, stretch: true);
            layer.type = Image.Type.Sliced;
        }
        return paper;
    }

    private static IEnumerator StampReceipt(Image seal)
    {
        const float duration = 0.23f;
        float elapsed = 0f;
        Color ink = seal.color;
        var pivot = seal.rectTransform;
        while (seal != null && seal.gameObject.activeInHierarchy && elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float settle = 1f - Mathf.Pow(1f - t, 3);
            pivot.localScale = Vector3.one * Mathf.Lerp(1.22f, 1f, settle);
            seal.color = new Color(ink.r, ink.g, ink.b, Mathf.Lerp(0.35f, ink.a, settle));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (seal != null)
        {
            pivot.localScale = Vector3.one;
            seal.color = ink;
        }
    }

    private void DrawArchivedOrder(int order, WorkshopProgress.ChallengeWork work, string caption = null)
    {
        var card = OrderReceipt("Archived Order " + order, new Rect(28 + order * 591, 848, 581, 60));
        if (!DrawWorkSample(card, new Rect(11, 6, 48, 48), work?.sample) && work != null)
            WorkshopUi.Picture(card, "Work Icon", new Rect(11, 6, 48, 48), RewardIcon(observation));
        WorkshopUi.Label(card, "Archived Work", new Rect(69, 4, 503, 51),
            (caption ?? OrderTitle(order)) + " · " + (work == null ? "待完成" : work.description), 21, WorkshopUi.Green);
    }

    private Button DrawChallengeControls()
    {
        if (GameComplete && IsFormalGarmentOrder)
        {
            var deliver = WorkshopUi.PixelButton(page, "Deliver Garment Stage", new Rect(1218, 848, 237, 58),
                observation == WorkshopObservation.DaxueSewing ? "去试衣 →" : "交付成衣 →", () =>
                {
                    var nextStage = onGarmentDelivery;
                    onClosed = null;
                    Close();
                    nextStage?.Invoke();
                }, true, 27, Accents[(int)Season(observation)]);
            RegisterButton(deliver);
            return deliver;
        }
        if (observation == WorkshopObservation.MangzhongCocoon)
        {
            if (!GameComplete) DrawXiazhiOrderSelectors();
            return null;
        }
        int next = 1 - ChallengeVariant;
        bool canSwitch = !IsDirectDyeing && (!IsGarmentProductionRun || GameComplete) && BestWork(observation, 0) != null;
        if (GameComplete)
            return DrawOrderAction("Challenge Replay", canSwitch ? "去玩" + OrderTitle(next) : "重做本单",
                new Rect(1218, 848, 237, 58), canSwitch ? next : ChallengeVariant, true);

        DrawOrderAction("Challenge Restart", "重做本单", new Rect(28, 848, 220, 58), ChallengeVariant, false);
        if (canSwitch)
            DrawOrderAction("Challenge Switch", "切换到" + OrderTitle(next), new Rect(264, 848, 236, 58), next, false);
        return null;
    }

    private Button DrawOrderAction(string name, string caption, Rect bounds, int order, bool primary)
    {
        var button = WorkshopUi.PixelButton(page, name, bounds, caption, () =>
            {
                WorkshopAudioFeedback.Click();
                PrepareChallenge(order);
                Render();
            }, primary, 26, Accents[(int)Season(observation)]);
        RegisterButton(button);
        return button;
    }

    private void FinishChallenge(int score, string result, string sample = null)
    {
        if (challengePhase != ChallengePhase.Playing || readOnly) return;

        runResult = result;
        challengePhase = ChallengePhase.Complete;
        stampCurrentResult = true;
        earnedRewardThisRun = WorkshopProgress.RecordChallenge(Definitions[(int)observation].Id,
            ChallengeVariant, score, result, sample);
        if (observation != WorkshopObservation.MangzhongCocoonHarvest && WorkshopProgress.SeasonalOrderEnabled &&
            (IsGarmentWorkshop(observation) ? IsFormalGarmentOrder : IsAtOwnWorkshop(observation)))
            WorkshopProgress.RecordSeasonalOrderWork(Definitions[(int)observation].Id, ChallengeVariant, score, result, sample);
        WorkshopAudioFeedback.Confirm();
        Step(CompletionStage(observation));
    }
}
