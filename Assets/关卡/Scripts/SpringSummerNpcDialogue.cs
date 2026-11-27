using System;
using Qingming.LevelBackground;

internal static class SpringSummerNpcDialogue
{
    private const string MulberryIntroStep = "dialogue.spring.mulberryIntro";
    private const string HarvestedLeavesStep = "spring.qingming.harvested";
    private const string SunnyOrchardStep = "spring.qingming.orchard.sunny";
    private const string ShadedOrchardStep = "spring.qingming.orchard.shaded";
    internal const string SunnyOrchardName = "向阳桑垄";
    internal const string ShadedOrchardName = "背阴桑垄";
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

    internal static bool HasMulberryBriefing => WorkshopProgress.HasStep(MulberryIntroStep) || HasHarvestedLeaves;

    internal static bool HasHarvestBasket => WorkshopProgress.HasStep("spring.basket") || HasHarvestedLeaves;

    internal static bool HasHarvestedLeaves => WorkshopProgress.HasStep(HarvestedLeavesStep)
        || WorkshopProgress.Has(WorkshopItem.MulberryLeaf) || WorkshopProgress.Has(WorkshopItem.Silkworm)
        || WorkshopObservationView.HasRecord(WorkshopObservation.QingmingMulberry)
        || WorkshopProgress.IsComplete(WorkshopSeason.Spring);

    internal static bool CanWash => HasHarvestedLeaves
        || WorkshopProgress.Has(WorkshopItem.SilkwormEgg);

    internal static string HarvestOrchardName => WorkshopProgress.HasStep(ShadedOrchardStep) ? ShadedOrchardName
        : WorkshopProgress.HasStep(SunnyOrchardStep) ? SunnyOrchardName : "桑园";

    internal static void RecordHarvestedLeaves(bool shaded)
    {
        if (!WorkshopProgress.HasStep(SunnyOrchardStep) && !WorkshopProgress.HasStep(ShadedOrchardStep) &&
            !HasHarvestedLeaves)
            WorkshopProgress.MarkStep(shaded ? ShadedOrchardStep : SunnyOrchardStep);
        WorkshopProgress.MarkStep(HarvestedLeavesStep);
        WorkshopProgress.Award(WorkshopItem.MulberryLeaf);
        SpringPrompt.HasMulberryLeaves = true;
    }

    internal static bool RequireMulberryBriefing()
    {
        if (HasMulberryBriefing) return true;
        SimplePopup.Show("先去找蚕桑师傅", "请先与春章的蚕桑师傅交谈，领到今天的采桑任务。", "知道了", null);
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
        else if (!HasHarvestedLeaves)
            WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.SpringInProgress);
        else if (!WorkshopProgress.Has(WorkshopItem.SilkwormEgg) && !WorkshopProgress.HasStep(BathIntroStep))
            BeforeBath();
        else
            ShowNextTask(WorkshopSeason.Spring, "蚕桑师傅");
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
            ShowNextTask(WorkshopSeason.Summer, "缫丝师傅");
    }

    private static void ShowNextTask(WorkshopSeason season, string speaker)
    {
        var next = WorkshopQuests.NextReady(season);
        WorkshopDialoguePresenter.Play(new[] { new WorkshopDialogueLine(speaker, next != null
            ? next.Title + "：" + next.Instructions
            : "本季成果已登记，可以从工坊前往下一季。已经获得的道具会一直保留。") });
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
