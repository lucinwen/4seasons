#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using JiangnanDye;

public static class WorkshopWordChecks
{
    private const string SaveKey = "ShiXu.Workshop.v1";
    private static void Require(bool value, string message) { if (!value) throw new Exception("[WordRevision] " + message); }
    private static void Load(string value) { PlayerPrefs.SetString(SaveKey, value); WorkshopProgress.ResetSession(); }
    public static void Verify()
    {
        bool existed = PlayerPrefs.HasKey(SaveKey);
        string prior = PlayerPrefs.GetString(SaveKey, "");
        try
        {
            Load("{}");
            AutumnLevelState.RestoreProgress();
            Require(AutumnNpcDialogue.TryGiveCake(), "first autumn conversation immediately grants cake");
            Require(AutumnLevelState.HasCake && !AutumnLevelState.HasHammer, "conversation grants cake without hammer");
            Require(!AutumnNpcDialogue.TryGiveCake(), "repeat conversation does not duplicate cake");
            WorkshopProgress.ResetSession();
            AutumnLevelState.RestoreProgress();
            Require(AutumnLevelState.HasCake && !AutumnLevelState.HasHammer, "conversation reward survives reload independently");
            Require(AutumnItemClick.TryCollectHammer(), "scene hammer can be picked up after conversation");
            Require(!AutumnItemClick.TryCollectHammer(), "scene hammer cannot be picked up twice");
            WorkshopProgress.ResetSession();
            AutumnLevelState.RestoreProgress();
            Require(AutumnLevelState.HasCake && AutumnLevelState.HasHammer, "cake and collected hammer survive reload");

            Load("{}");
            AutumnLevelState.RestoreProgress();
            Require(AutumnItemClick.TryCollectHammer() && !AutumnLevelState.HasCake,
                "hammer pickup does not require conversation or grant cake");
            Load("{\"steps\":[\"autumn.hammer\"]}");
            AutumnLevelState.RestoreProgress();
            Require(AutumnLevelState.HasHammer && !AutumnItemClick.TryCollectHammer(), "legacy hammer record remains valid");
            Require(AutumnNpcDialogue.TryGiveCake() && AutumnLevelState.HasCake, "legacy hammer does not block first cake reward");

            Load("{}");
            AutumnLevelState.RestoreProgress();
            WorkshopProgress.StartNewGame();
            Require(WorkshopWordRules.WinterStage == WorkshopWinterStage.Preparing, "winter starts at cloth preparation");
            SpringSummerNpcDialogue.RecordHarvestedLeaves(false);
            Require(WorkshopProgress.Has(WorkshopItem.MulberryLeaf) && !WorkshopProgress.Has(WorkshopItem.Silkworm), "harvest grants only leaves");
            Require(SpringSummerNpcDialogue.CanWash, "harvest unlocks bath without silkworm circular dependency");
            WorkshopWordRules.CompleteFeeding();
            Require(!WorkshopProgress.Has(WorkshopItem.Silkworm), "feeding requires cleaned eggs");
            WorkshopProgress.Award(WorkshopItem.SilkwormEgg);
            WorkshopWordRules.CompleteFeeding();
            Require(WorkshopProgress.Has(WorkshopItem.Silkworm) && WorkshopProgress.CanSubmit(WorkshopSeason.Spring), "feeding enables spring handoff");
            Require(WorkshopProgress.Submit(WorkshopSeason.Spring), "spring handoff");
            Require(WorkshopProgress.Items(WorkshopSeason.Spring).All(WorkshopProgress.Has), "spring handoff preserves items");

            foreach (var item in WorkshopProgress.Items(WorkshopSeason.Summer)) WorkshopProgress.Award(item);
            Require(WorkshopQuests.ForSeason(WorkshopSeason.Summer).Single(q => q.Id == "MangzhongOrder").IsReady,
                "finding the spool enables the wheel puzzle before tension reeling");
            Require(!WorkshopProgress.CanSubmit(WorkshopSeason.Summer), "cannot skip reeling and preparation");
            WorkshopProgress.RecordObservation("observation.summer.mangzhong.thread");
            Require(!WorkshopProgress.CanSubmit(WorkshopSeason.Summer), "puzzle alone does not skip reeling");
            WorkshopProgress.MarkStep(WorkshopWordRules.Reeled);
            Require(!WorkshopProgress.CanSubmit(WorkshopSeason.Summer), "summer also requires the cocoon sorting game");
            WorkshopProgress.MarkStep(WorkshopWordRules.Sorted);
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Summer) && WorkshopProgress.Submit(WorkshopSeason.Summer), "summer delivery");

            AutumnLevelState.HasLoomRepaired = true;
            WorkshopProgress.Award(WorkshopItem.Shuttle);
            var weave = new WorkshopPlainWeave();
            Require(!weave.Pass(true), "must select shed");
            weave.Select(1); Require(!weave.Pass(true), "reject wrong first shed");
            for (int row = 0; row < WorkshopPlainWeave.Rows; row++)
            {
                weave.Select(row % 2);
                Require(!weave.Pass(row % 2 != 0), "reject wrong shuttle direction");
                Require(weave.Pass(row % 2 == 0), "alternating weave pass");
            }
            Require(weave.Complete && !weave.Pass(true), "completed weave locks");
            WorkshopWordRules.CompleteWeaving();
            Require(WorkshopProgress.Has(WorkshopItem.RawSilkCloth), "basic weave awards greige cloth");
            WorkshopWordRules.CompletePattern();
            Require(!WorkshopProgress.HasStep(WorkshopWordRules.Patterned), "pattern requires dyed cloth");
            WorkshopProgress.RecordRawDyes(new[] { WorkshopArt.RawDye(DyeColor.Purple, true) });
            WorkshopProgress.RecordGroundDye(DyeColor.Purple);
            WorkshopProgress.RecordDyedCloth(DyeColor.Purple);
            WorkshopProgress.Award(WorkshopItem.Statue);
            Require(!WorkshopProgress.CanSubmit(WorkshopSeason.Autumn), "autumn requires pattern");
            WorkshopWordRules.CompletePattern();
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Autumn) && WorkshopProgress.Submit(WorkshopSeason.Autumn), "autumn dye-pattern handoff");
            Require(WorkshopProgress.Has(WorkshopItem.RawSilkCloth) && WorkshopProgress.Has(WorkshopItem.Shuttle) && WorkshopProgress.Has(WorkshopItem.DyedCloth), "autumn interactions and handoff preserve cloth and shuttle");

            WorkshopProgress.MarkStep(WorkshopWordRules.WinterPrepared);
            WorkshopProgress.ResetSession();
            Require(WorkshopWordRules.WinterStage == WorkshopWinterStage.Cutting && !WorkshopProgress.Has(WorkshopItem.CutPieces),
                "cloth preparation survives reload and leads to scissors without awarding pieces");
            WorkshopProgress.Award(WorkshopItem.CutPieces);
            Require(WorkshopWordRules.WinterStage == WorkshopWinterStage.Needle, "cut pieces lead to needle if missing");
            Require(!WorkshopProgress.CanSubmit(WorkshopSeason.Winter), "needle required after cutting");
            WorkshopProgress.Award(WorkshopItem.Needle);
            Require(WorkshopProgress.Items(WorkshopSeason.Winter).SequenceEqual(new[] {
                WorkshopItem.Needle, WorkshopItem.SilkwormEggSheet, WorkshopItem.CutPieces }),
                "winter task board lists needle, egg sheet and cut pieces in order");
            Require(WorkshopWordRules.WinterStage == WorkshopWinterStage.EggSheet &&
                !WorkshopProgress.CanSubmit(WorkshopSeason.Winter) && !WorkshopProgress.CompleteWinter("1_04"),
                "dyed cloth cannot substitute for the egg sheet at settlement");
            Require(WorkshopProgress.Award(WorkshopItem.SilkwormEggSheet) &&
                WorkshopWordRules.WinterStage == WorkshopWinterStage.Sewing &&
                WorkshopProgress.CanSubmit(WorkshopSeason.Winter), "all three delivery items open sewing");
            WorkshopProgress.MarkStep(WorkshopObservationView.XiaoxueLiningStep);
            WorkshopProgress.ResetSession();
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Winter) && WorkshopProgress.DyedColor == DyeColorUtil.ChineseName(DyeColor.Purple), "permanent inventory and winter delivery survive reload");
            Require(WorkshopProgress.CompleteWinter("1_04"), "legacy lining record does not add a sewing prerequisite");
            Require(WorkshopWordRules.WinterStage == WorkshopWinterStage.Complete, "finished table leads to wardrobe");
            Require(WorkshopProgress.Items(WorkshopSeason.Winter).All(WorkshopProgress.Has), "winter sewing preserves all items");

            Load("{\"items\":1,\"seasonalOrder\":true}");
            Require(!WorkshopProgress.Has(WorkshopItem.Silkworm), "old harvest save is not auto-fed");
            Load("{\"items\":7,\"steps\":[\"spring.guyu.batchFed\"]}");
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Spring), "old real feeding completion remains valid");
            Load("{\"items\":1536}");
            Require(WorkshopWordRules.WinterStage == WorkshopWinterStage.Needle,
                "legacy cut pieces allow the player to proceed to the needle");
            Load("{\"items\":3584,\"chapters\":7}");
            Require(WorkshopWordRules.WinterStage == WorkshopWinterStage.EggSheet && !WorkshopProgress.CanSubmit(WorkshopSeason.Winter),
                "unfinished legacy saves retain their materials and can collect the new item");
            Load("{\"items\":4095,\"chapters\":15,\"dyedColor\":\"紫色\"}");
            Require(Enum.GetValues(typeof(WorkshopSeason)).Cast<WorkshopSeason>().All(WorkshopProgress.IsComplete), "completed legacy chapters remain complete");
            Require(WorkshopProgress.Has(WorkshopItem.DyedCloth) &&
                WorkshopProgress.Items(WorkshopSeason.Winter).All(WorkshopProgress.Has),
                "completed legacy saves retain dyed cloth and display the full updated delivery");
            Require(WorkshopQuests.ForSeason(WorkshopSeason.Summer).Any(q => q.Id == "WhiteTiles"), "cocoon sorting is present in the summer route");
            int entered = 0;
            for (int season = 0; season < 3; season++)
                WorkshopNarrativeView.ShowChapterInterlude((WorkshopSeason)season, (WorkshopSeason)(season + 1),
                    () => entered++, () => { throw new Exception("Unexpected stay in chapter transition."); });
            Require(entered == 3 && !WorkshopNarrativeView.IsOpen, "all chapter transitions continue without a narrative page");
            WorkshopNarrativeView.ShowFinale(() => entered++);
            Require(entered == 4 && !WorkshopNarrativeView.IsOpen && WorkshopProgress.HasStep("finale.seen"),
                "finale goes directly to the wardrobe callback");
            Debug.Log("[WordRevision] RULES PASS: first-conversation cake, independent scene hammer, new game, four-season delivery, permanent inventory, feeding, weaving, dye-pattern gates, layout-cutting-needle-sewing-wardrobe routing, legacy compatibility and save reload.");
        }
        finally
        {
            if (existed) PlayerPrefs.SetString(SaveKey, prior); else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save(); WorkshopProgress.ResetSession();
            AutumnLevelState.RestoreProgress(); WinterLevelState.RestoreWorkshopProgress();
        }
    }
}
#endif
