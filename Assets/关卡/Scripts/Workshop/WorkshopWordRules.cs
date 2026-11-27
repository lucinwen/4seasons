using UnityEngine;

internal enum WorkshopWinterStage { Preparing, Cutting, Needle, EggSheet, Sewing, Complete }

/// <summary>The simplified, permanent-inventory chapter flow specified in the Word plan.</summary>
internal static class WorkshopWordRules
{
    internal const string Fed = "spring.guyu.fed";
    internal const string Reeled = "summer.xiaoman.reeled";
    internal const string Sorted = "summer.cocoon-sorted";
    internal const string Woven = "autumn.bailu.woven";
    internal const string Patterned = "autumn.hanlu.patterned";
    internal const string WinterPrepared = "winter.xiaoxue.prepared";

    internal static bool HasWinterPreparation => WorkshopProgress.HasStep(WinterPrepared) ||
        WorkshopProgress.Has(WorkshopItem.CutPieces);

    internal static WorkshopWinterStage WinterStage
    {
        get
        {
            if (WorkshopProgress.IsComplete(WorkshopSeason.Winter)) return WorkshopWinterStage.Complete;
            if (!HasWinterPreparation) return WorkshopWinterStage.Preparing;
            if (!WorkshopProgress.Has(WorkshopItem.CutPieces)) return WorkshopWinterStage.Cutting;
            if (!WorkshopProgress.Has(WorkshopItem.Needle)) return WorkshopWinterStage.Needle;
            return WorkshopProgress.Has(WorkshopItem.SilkwormEggSheet)
                ? WorkshopWinterStage.Sewing : WorkshopWinterStage.EggSheet;
        }
    }

    internal static bool HasDelivery(WorkshopSeason season)
    {
        switch (season)
        {
            case WorkshopSeason.Spring:
                return WorkshopProgress.HasStep(Fed) || WorkshopProgress.HasStep("spring.guyu.batchFed");
            case WorkshopSeason.Summer:
                return WorkshopProgress.HasStep(Sorted) && WorkshopProgress.HasStep(Reeled) &&
                    WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon);
            case WorkshopSeason.Autumn:
                return WorkshopProgress.Has(WorkshopItem.DyedCloth) && WorkshopProgress.HasStep(Patterned);
            case WorkshopSeason.Winter:
                return WorkshopProgress.Has(WorkshopItem.CutPieces);
            default: return false;
        }
    }

    internal static bool CanFeed => WorkshopProgress.Has(WorkshopItem.MulberryLeaf) &&
        WorkshopProgress.Has(WorkshopItem.SilkwormEgg);

    internal static bool CanPattern => WorkshopProgress.Has(WorkshopItem.DyedCloth) &&
        WorkshopProgress.Has(WorkshopItem.Shuttle);

    internal static void CompleteFeeding()
    {
        if (!CanFeed) return;
        WorkshopProgress.MarkStep(Fed);
        WorkshopProgress.Award(WorkshopItem.Silkworm);
        SpringPrompt.HasSilkwormBaby = true;
    }

    internal static void CompleteWeaving()
    {
        if (!AutumnLevelState.HasLoomRepaired || !WorkshopProgress.Has(WorkshopItem.Shuttle)) return;
        WorkshopProgress.MarkStep(Woven);
        WorkshopProgress.Award(WorkshopItem.RawSilkCloth);
    }

    internal static void CompletePattern()
    {
        if (CanPattern) WorkshopProgress.MarkStep(Patterned);
    }
}

/// <summary>Six alternating sheds and shuttle passes; wrong moves never destroy progress.</summary>
internal sealed class WorkshopPlainWeave
{
    internal const int Rows = 6;
    internal int WovenRows { get; private set; }
    internal int SelectedShed { get; private set; } = -1;
    internal int RequiredShed => WovenRows % 2;
    internal bool Complete => WovenRows == Rows;
    internal void Select(int shed) { if (!Complete && (shed == 0 || shed == 1)) SelectedShed = shed; }
    internal bool Pass(bool toRight)
    {
        if (Complete || SelectedShed != RequiredShed || toRight != (WovenRows % 2 == 0)) return false;
        WovenRows++;
        SelectedShed = -1;
        return true;
    }
}
