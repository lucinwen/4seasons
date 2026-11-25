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
            Require(!WorkshopProgress.Has(WorkshopItem.Silkworm), "new game must not grant a feeding reward");
            Require(!WorkshopObservationView.HasSeasonOrderDeliveries(WorkshopSeason.Spring), "harvest is required");
            Require(!WorkshopObservationView.ObservationsFor(WorkshopSeason.Spring).Any(),
                "retired spring minigames are not listed");

            SpringSummerNpcDialogue.RecordHarvestedLeaves();
            Require(WorkshopProgress.Has(WorkshopItem.MulberryLeaf) && !WorkshopProgress.Has(WorkshopItem.Silkworm), "harvest grants leaves");
            Require(!WorkshopProgress.HasObservation("observation.spring.qingming"), "direct harvest only grants quest progress");
            WorkshopProgress.ResetSession();
            Require(!WorkshopProgress.Has(WorkshopItem.Silkworm), "reloading after harvest keeps feeding pending");
            Require(!SpringSummerNpcDialogue.CanWash, "bath preparation requires feeding");
            Require(WorkshopQuests.NextReady(WorkshopSeason.Spring)?.Id == nameof(WorkshopItem.Silkworm), "next quest is feeding");
            SpringSummerNpcDialogue.RecordHarvestedLeaves(true);
            Require(WorkshopProgress.Award(WorkshopItem.Silkworm), "first feeding grants one reward");
            Require(!WorkshopProgress.Award(WorkshopItem.Silkworm), "repeat feeding does not duplicate reward");
            Require(SpringSummerNpcDialogue.CanWash, "feeding unlocks bath preparation");
            Require(WorkshopQuests.NextReady(WorkshopSeason.Spring)?.Id == "EggPaper", "next quest is egg paper");
            Require(!WorkshopProgress.CanSubmit(WorkshopSeason.Spring), "egg still required");
            WorkshopProgress.MarkStep("spring.eggPaper");
            Require(WorkshopQuests.NextReady(WorkshopSeason.Spring)?.Id == nameof(WorkshopItem.SilkwormEgg), "bath remains playable");
            WorkshopProgress.Award(WorkshopItem.SilkwormEgg);
            Require(!WorkshopProgress.HasStep("spring.guyu.batchFed"), "no retired completion flag needed");
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Spring), "can deliver after feeding and washing");
            Require(WorkshopProgress.Submit(WorkshopSeason.Spring) && WorkshopProgress.IsUnlocked(WorkshopSeason.Summer), "summer unlock");

            foreach (string fixture in new[]
            {
                "{\"items\":1,\"seasonalOrder\":true}",
                "{\"steps\":[\"spring.qingming.harvested\"],\"seasonalOrder\":true}",
                "{\"observations\":[\"observation.spring.qingming\"],\"seasonalOrder\":true}"
            })
            {
                Load(fixture);
                Require(!WorkshopProgress.Has(WorkshopItem.Silkworm) && WorkshopProgress.Has(WorkshopItem.MulberryLeaf), "old harvest saves recover leaves");
                Require(WorkshopQuests.NextReady(WorkshopSeason.Spring)?.Id == nameof(WorkshopItem.Silkworm), "old harvest saves can feed");
                WorkshopProgress.ResetSession();
                Require(WorkshopProgress.Has(WorkshopItem.MulberryLeaf) && !WorkshopProgress.Has(WorkshopItem.Silkworm), "migration persists without skipping feeding");
            }
            Load("{\"items\":5,\"seasonalOrder\":true}");
            Require(WorkshopQuests.NextReady(WorkshopSeason.Spring)?.Id == "EggPaper", "previously earned silkworm continues to bath");
            Load("{\"items\":7,\"steps\":[\"spring.guyu.batchFed\"],\"observations\":[\"observation.spring.guyu\"],\"seasonalOrder\":true}");
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Spring), "old completed Guyu save can submit");
            Require(WorkshopProgress.HasObservation("observation.spring.guyu"), "legacy record preserved");
            Require(!WorkshopObservationView.ObservationsFor(WorkshopSeason.Spring).Any(), "legacy save adds no retired journal entry");
            Load("{}");
            Require(!WorkshopProgress.Has(WorkshopItem.Silkworm), "unplayed save gets no reward");
            Debug.Log("[SpringVerify] PASS: harvest, feeding reward, bath, handoff, reload and legacy saves.");
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
