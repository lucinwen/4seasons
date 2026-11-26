using System;
using System.Collections;
using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 秋天关卡 NPC 对话：锤子与糕点通过对话获得。
/// 由 AutumnLevelInteractions 在运行时挂到 NPC 上，不改场景。
/// </summary>
public class AutumnNpcDialogue : MonoBehaviour
{
    private RectTransform _cakeNotice;

    internal static string ActionLabel => !AutumnLevelState.HasHammer ? "领取修机锤" :
        CanOfferCake ? "领取祭祀糕点" : null;

    private static bool CanOfferCake => WorkshopProgress.Has(WorkshopItem.RawSilkCloth) &&
        !AutumnLevelState.HasCake && !AutumnLevelState.HasRitualDone;

    public void Talk()
    {
        if (!AutumnLevelState.HasHammer)
        {
            IntroduceWeaving(OfferHammer);
            return;
        }

        // 保留秋季原顺序：先修机、取梭并完成推梭，再领取糕点。
        if (!WorkshopProgress.Has(WorkshopItem.RawSilkCloth))
        {
            WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.AutumnInProgress);
            return;
        }

        if (CanOfferCake)
        {
            IntroduceRitual(OfferCake);
            return;
        }

        if (WinterLevelState.CollectedDyes.Count > 0 && !WinterLevelState.HasDyedCloth &&
            !WorkshopProgress.HasStep("dialogue.winter.grinding"))
        {
            WinterNpcDialogue.IntroduceGrinding();
            return;
        }

        if (!WorkshopProgress.HasStep("winter.dyes") && !WorkshopProgress.HasStep("winter.ground") &&
            !WinterLevelState.HasDyedCloth && !WorkshopProgress.HasStep("dialogue.winter.materials"))
        {
            WinterNpcDialogue.IntroduceDyeMaterials();
            return;
        }

        WorkshopDialoguePresenter.Play(WorkshopProgress.Has(WorkshopItem.DyedCloth)
            ? WorkshopDialogueScripts.AutumnDone
            : WorkshopDialogueScripts.AutumnInProgress);
    }

    internal static void IntroduceRitual(Action onFinished)
    {
        WorkshopDialoguePresenter.PlayOnce("dialogue.autumn.ritual", WorkshopDialogueScripts.AutumnRitualIntro,
            onFinished);
    }

    internal static void IntroduceWeaving(Action onFinished)
    {
        // 织造开场对话每次都播放，保证织机前的剧情与引导不因存档而消失
        WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.AutumnWeavingIntro, onFinished);
    }

    private static void OfferHammer()
    {
        var icon = AutumnItemIcons.Load()?.hammer;
        SimplePopup.Show("领取修机工具", "锤子 ×1", "收下锤子", () =>
        {
            AutumnLevelState.HasHammer = true;
            WorkshopProgress.MarkStep("autumn.hammer");
            SimplePopup.Show("获得道具", "获得【锤子】×1", "确定", null, icon: icon);
        }, true, "取消", null, icon);
    }

    private void OfferCake()
    {
        var icon = AutumnItemIcons.Load()?.cake;
        SimplePopup.Show("领取祭祀糕点", "糕点 ×1\n带到祭祀供台，和蚕花酒、香蕉一起提交。", "收下糕点", () =>
        {
            AutumnLevelState.HasCake = true;
            WorkshopProgress.MarkStep("autumn.cake");
            ShowCakeNotice();
        }, true, "取消", null, icon);
    }

    private void ShowCakeNotice()
    {
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
