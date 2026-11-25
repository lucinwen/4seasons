#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;

/// <summary>Batch-only progression regression checks. Restores the prior Editor save in finally.</summary>
public static class SpringCollectionChecks
{
    private const string Key = "ShiXu.Workshop.v1";
    private static void Require(bool result, string message)
    {
        if (!result) throw new Exception("[SpringVerify] " + message);
    }

    private static void Load(string fixture)
    {
        PlayerPrefs.SetString(Key, fixture);
        WorkshopProgress.ResetSession();
    }

    public static void Verify()
    {
        string previous = PlayerPrefs.GetString(Key, "");
        try
        {
            WorkshopProgress.StartNewGame();
            Require(!WorkshopProgress.Has(WorkshopItem.Silkworm), "new game must not grant a harvesting reward");
            Require(!WorkshopObservationView.HasSeasonOrderDeliveries(WorkshopSeason.Spring), "harvest is required");
            Require(WorkshopObservationView.ObservationsFor(WorkshopSeason.Spring).SequenceEqual(
                new[] { WorkshopObservation.QingmingMulberry }), "only harvest remains in spring collection");

            SpringSummerNpcDialogue.RecordHarvestedLeaves(false);
            WorkshopObservationView.RecordQingmingHarvest(8);
            Require(WorkshopProgress.Has(WorkshopItem.MulberryLeaf) && WorkshopProgress.Has(WorkshopItem.Silkworm), "harvest grants both rewards");
            Require(SpringSummerNpcDialogue.CanWash, "harvest unlocks bath preparation");
            Require(WorkshopQuests.NextReady(WorkshopSeason.Spring)?.Id == "EggPaper", "next quest is egg paper");
            Require(!WorkshopProgress.CanSubmit(WorkshopSeason.Spring), "egg still required");
            SpringSummerNpcDialogue.RecordHarvestedLeaves(true);
            Require(!WorkshopProgress.Award(WorkshopItem.Silkworm), "replay does not duplicate reward");
            WorkshopProgress.MarkStep("spring.eggPaper");
            Require(WorkshopQuests.NextReady(WorkshopSeason.Spring)?.Id == nameof(WorkshopItem.SilkwormEgg), "bath remains playable");
            WorkshopProgress.Award(WorkshopItem.SilkwormEgg);
            Require(!WorkshopProgress.HasStep("spring.guyu.batchFed"), "no retired completion flag needed");
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Spring), "can deliver without Guyu");
            Require(WorkshopProgress.Submit(WorkshopSeason.Spring) && WorkshopProgress.IsUnlocked(WorkshopSeason.Summer), "summer unlock");

            foreach (string fixture in new[]
            {
                "{\"items\":1,\"seasonalOrder\":true}",
                "{\"steps\":[\"spring.qingming.harvested\"],\"seasonalOrder\":true}",
                "{\"observations\":[\"observation.spring.qingming\"],\"seasonalOrder\":true}"
            })
            {
                Load(fixture);
                Require(WorkshopProgress.Has(WorkshopItem.Silkworm) && WorkshopProgress.Has(WorkshopItem.MulberryLeaf), "old harvest save migration");
                Require(WorkshopQuests.NextReady(WorkshopSeason.Spring)?.Id == "EggPaper", "old save can continue");
                WorkshopProgress.ResetSession();
                Require(WorkshopProgress.Has(WorkshopItem.Silkworm), "migration persisted across reload");
            }
            Load("{\"items\":7,\"steps\":[\"spring.guyu.batchFed\"],\"observations\":[\"observation.spring.guyu\"],\"seasonalOrder\":true}");
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Spring), "old completed Guyu save can submit");
            Require(WorkshopProgress.HasObservation("observation.spring.guyu"), "legacy record preserved");
            Require(WorkshopObservationView.ObservationsFor(WorkshopSeason.Spring).Count() == 1, "legacy save adds no retired journal entry");
            Load("{}");
            Require(!WorkshopProgress.Has(WorkshopItem.Silkworm), "unplayed save gets no reward");
            Debug.Log("[SpringVerify] PASS: new game, harvest rewards, replay, bath, handoff, summer unlock, 3 legacy migrations, reload and retired entries.");
        }
        finally
        {
            PlayerPrefs.SetString(Key, previous);
            PlayerPrefs.Save();
            WorkshopProgress.ResetSession();
        }
    }
}
#endif
