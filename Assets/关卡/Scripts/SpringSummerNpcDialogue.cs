using System;

internal static class SpringSummerNpcDialogue
{
    private const string MulberryIntroStep = "dialogue.spring.mulberryIntro";
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

    internal static void BeforeMulberry(Action continueToActivity = null) =>
        Introduce(WorkshopItem.MulberryLeaf, MulberryIntroStep,
            WorkshopDialogueScripts.SpringMulberryIntro, continueToActivity);

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
        bool hasLeaves = WorkshopProgress.Has(WorkshopItem.MulberryLeaf);
        if (!hasLeaves && !WorkshopProgress.HasStep(MulberryIntroStep))
            BeforeMulberry();
        else if (hasLeaves && !WorkshopProgress.Has(WorkshopItem.SilkwormEgg) &&
                 !WorkshopProgress.HasStep(BathIntroStep))
            BeforeBath();
        else
            WorkshopDialoguePresenter.Play(hasLeaves ? WorkshopDialogueScripts.SpringDone :
                WorkshopDialogueScripts.SpringInProgress);
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
