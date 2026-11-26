using System;
using System.Collections;
using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 秋天关卡 NPC 对话：第一次交谈即送糕点，修机锤由玩家在场景中拾取。
/// 由 AutumnLevelInteractions 在运行时挂到 NPC 上，不改场景。
/// </summary>
public class AutumnNpcDialogue : MonoBehaviour
{
    private RectTransform _cakeNotice;

    internal static string ActionLabel => CanOfferCake ? "领取祭祀糕点" : null;

    private static bool CanOfferCake => !AutumnLevelState.HasCake && !AutumnLevelState.HasRitualDone;

    public void Talk()
    {
        WorkshopProgress.UnlockSolarTerms(WorkshopSeason.Autumn);
        bool receivedCake = TryGiveCake();
        if (receivedCake) ShowCakeNotice();
        if (!WorkshopProgress.HasStep("dialogue.autumn.dyeIntro"))
        {
            WorkshopDialoguePresenter.PlayOnce("dialogue.autumn.dyeIntro", new[] {
                new WorkshopDialogueLine("织造师傅", receivedCake
                    ? "这份糕点先送给你。收集香蕉与蚕花酒后，带到灵桑阙参加秋社。"
                    : "立秋先到染材园采集天然染材。锄头在入口旁，研磨棒和研钵在青染轩。"),
                new WorkshopDialogueLine("织造师傅", "修机锤放在织机左侧的地上，自己拾起就能修机。织出生坯绸后去青染轩研磨染色，最后用染布攀花。道具一直保留。") });
            return;
        }

        if (receivedCake)
        {
            WorkshopDialoguePresenter.Play(new[] {
                new WorkshopDialogueLine("织造师傅", "糕点送给你了。再收集香蕉与蚕花酒，就能去灵桑阙参加秋社。修机锤在织机左侧的地上，记得自己拾取。") });
            return;
        }

        var next = WorkshopQuests.NextReady(WorkshopSeason.Autumn);
        WorkshopDialoguePresenter.Play(new[] { new WorkshopDialogueLine("织造师傅", next != null
            ? next.Title + "：" + next.Instructions
            : "秋季成果已登记。染布一直保留，可以带入冬季剪帛制衣。") });
    }

    internal static void IntroduceRitual(Action onFinished)
    {
        WorkshopDialoguePresenter.PlayOnce("dialogue.autumn.ritual", WorkshopDialogueScripts.AutumnRitualIntro,
            onFinished);
    }

    internal static void IntroduceWeaving(Action onFinished)
    {
        WorkshopDialoguePresenter.PlayOnce("dialogue.autumn.weaving", WorkshopDialogueScripts.AutumnWeavingIntro,
            onFinished);
    }

    internal static bool TryGiveCake()
    {
        if (!CanOfferCake || WorkshopProgress.HasStep("autumn.cake")) return false;
        AutumnLevelState.HasCake = true;
        WorkshopProgress.MarkStep("autumn.cake");
        return true;
    }

    private void ShowCakeNotice()
    {
        CloseCakeNotice();
        string next;
        if (AutumnLevelState.HasWine && AutumnLevelState.HasBanana) next = "回祭祀供台提交贡品";
        else if (!AutumnLevelState.HasWine && !AutumnLevelState.HasBanana) next = "还差蚕花酒和香蕉";
        else next = AutumnLevelState.HasWine ? "还差香蕉" : "还差蚕花酒";

        _cakeNotice = WorkshopUi.NewCanvas("Autumn Offering Feedback", 1600);
        var card = WorkshopUi.PixelCard(_cakeNotice, "Cake Collected", new Rect(663, 916, 594, 104),
            WorkshopUi.Ink, WorkshopUi.Bronze);
        WorkshopUi.Label(card, "Offering Count", new Rect(20, 12, 554, 42),
            $"糕点已收下 · 贡品 {AutumnLevelState.OfferingCount}/3", 31, WorkshopUi.Cream,
            TextAnchor.MiddleCenter);
        WorkshopUi.Label(card, "Next Offering Step", new Rect(20, 56, 554, 34), next, 24,
            WorkshopUi.Paper, TextAnchor.MiddleCenter);
        StartCoroutine(DismissCakeNotice());
    }

    private IEnumerator DismissCakeNotice()
    {
        yield return new WaitForSecondsRealtime(3.2f);
        CloseCakeNotice();
    }

    private void OnDisable() => CloseCakeNotice();

    private void CloseCakeNotice()
    {
        WorkshopUi.CloseCanvas(_cakeNotice);
        _cakeNotice = null;
    }
}
