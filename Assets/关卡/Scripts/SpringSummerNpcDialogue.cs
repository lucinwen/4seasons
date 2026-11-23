using System;
using Qingming.LevelBackground;

internal static class SpringSummerNpcDialogue
{
    private const string MulberryIntroStep = "dialogue.spring.mulberryIntro";
    private const string PreparedLeavesStep = "spring.guyu.prepared";
    private const string BathIntroStep = "dialogue.spring.bathIntro";
    private const string CocoonIntroStep = "dialogue.summer.cocoonIntro";
    private const string BoilingIntroStep = "dialogue.summer.boilingIntro";
    private const string BoilingCompleteStep = "dialogue.summer.boilingComplete";

    internal static void Talk(WorkshopSeason season)
    {
        switch (season)
        {
            case WorkshopSeason.Spring:
                TalkSpring();
                break;
            case WorkshopSeason.Summer:
                TalkSummer();
                break;
        }
    }

    internal static bool HasMulberryBriefing => WorkshopProgress.HasStep(MulberryIntroStep)
        || WorkshopProgress.Has(WorkshopItem.MulberryLeaf) || WorkshopProgress.Has(WorkshopItem.Silkworm);

    internal static bool HasPreparedLeaves => WorkshopProgress.HasStep(PreparedLeavesStep)
        || WorkshopProgress.Has(WorkshopItem.Silkworm);

    internal static bool CanWash => WorkshopProgress.Has(WorkshopItem.Silkworm)
        || WorkshopProgress.Has(WorkshopItem.SilkwormEgg);

    internal static void RecordPreparedLeaves() => WorkshopProgress.MarkStep(PreparedLeavesStep);

    internal static bool RequireMulberryBriefing()
    {
        if (HasMulberryBriefing) return true;
        SimplePopup.Show("先去找蚕房师傅", "请先与春章的蚕房师傅交谈，领到今天的采桑任务。", "知道了", null);
        return false;
    }

    internal static void BeforeBath(Action continueToActivity = null) =>
        Introduce(WorkshopItem.SilkwormEgg, BathIntroStep,
            WorkshopDialogueScripts.SpringBathIntro, continueToActivity);

    internal static void BeforeCocoon(Action continueToActivity = null) =>
        Introduce(WorkshopItem.Suoxu, CocoonIntroStep,
            WorkshopDialogueScripts.SummerCocoonIntro, continueToActivity);

    internal static void BeforeBoiling(Action continueToActivity = null) =>
        Introduce(WorkshopItem.Spool, BoilingIntroStep,
            WorkshopDialogueScripts.SummerBoilingIntro, continueToActivity);

    internal static void BeforeFindingSpool(Action openMaze) =>
        Introduce(WorkshopItem.Spool, BoilingCompleteStep,
            WorkshopDialogueScripts.SummerBoilingComplete, openMaze);

    private static void TalkSpring()
    {
        if (!HasMulberryBriefing)
            WorkshopDialoguePresenter.PlayOnce(MulberryIntroStep, WorkshopDialogueScripts.SpringMulberryIntro);
        else if (!HasPreparedLeaves)
            WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.SpringInProgress);
        else if (!WorkshopProgress.Has(WorkshopItem.Silkworm))
            WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.SpringReadyToFeed);
        else if (!WorkshopProgress.Has(WorkshopItem.SilkwormEgg) && !WorkshopProgress.HasStep(BathIntroStep))
            BeforeBath();
        else
            WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.SpringDone);
    }

    private static void TalkSummer()
    {
        bool hasSuoxu = WorkshopProgress.Has(WorkshopItem.Suoxu);
        bool hasSpool = WorkshopProgress.Has(WorkshopItem.Spool);
        if (!hasSuoxu && !WorkshopProgress.HasStep(CocoonIntroStep))
            BeforeCocoon();
        else if (hasSuoxu && !hasSpool && !WorkshopProgress.HasStep(BoilingIntroStep))
            BeforeBoiling();
        else
            WorkshopDialoguePresenter.Play(hasSuoxu && hasSpool ? WorkshopDialogueScripts.SummerDone :
                WorkshopDialogueScripts.SummerInProgress);
    }

    private static void Introduce(WorkshopItem reward, string step, WorkshopDialogueLine[] script,
        Action continueToActivity)
    {
        if (WorkshopProgress.Has(reward))
            continueToActivity?.Invoke();
        else
            WorkshopDialoguePresenter.PlayOnce(step, script, continueToActivity);
    }
}
