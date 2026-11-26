#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using JiangnanDye;

public static class WorkshopRewardChecks
{
    private const string Key = "ShiXu.Workshop.v1";
    private static void Require(bool value, string detail)
    { if (!value) throw new InvalidOperationException("[CardRewards] " + detail); }
    private static void Load(string data)
    { PlayerPrefs.SetString(Key, data); WorkshopProgress.ResetSession(); }

    public static void Verify()
    {
        bool existed = PlayerPrefs.HasKey(Key);
        string prior = PlayerPrefs.GetString(Key, "");
        try
        {
            Load("{}");
            Require(WorkshopRewardCards.CardCount == 0 && !WorkshopRewardCards.IsBusy, "new save starts without cards");
            WorkshopWordRules.CompleteFeeding();
            Require(WorkshopRewardCards.CardCount == 0, "failed or unprepared game cannot award a card");
            WorkshopProgress.Award(WorkshopItem.MulberryLeaf);
            Require(WorkshopRewardCards.CardCount == 1 && WorkshopRewardCards.IsBusy &&
                WorkshopProgress.HasStep("reward.craft.harvest") && WorkshopRewardCards.HasTerm(WorkshopSeason.Spring, 4),
                "successful harvest queues and persists its craft and Qingming cards");
            Require(!WorkshopRewardCards.HasTerm(WorkshopSeason.Spring, 5), "one term does not unlock the whole season");
            Require(!WorkshopProgress.Award(WorkshopItem.MulberryLeaf) && WorkshopRewardCards.CardCount == 1,
                "repeat completion does not duplicate collection");
            WorkshopProgress.ResetSession();
            Require(WorkshopRewardCards.CardCount == 1 && !WorkshopRewardCards.IsBusy,
                "earned cards survive reload without replaying old reward popups");

            // Legacy progress is adopted silently, including the previous batch-feeding id.
            Load("{\"items\":7,\"steps\":[\"spring.guyu.batchFed\"]}");
            Require(WorkshopRewardCards.CardCount == 3 && !WorkshopRewardCards.IsBusy &&
                WorkshopProgress.HasStep("reward.craft.feed"), "legacy spring tasks backfill three craft cards");
            WorkshopProgress.UnlockSolarTerms(WorkshopSeason.Winter);
            Require(Enumerable.Range(0, 6).All(i => WorkshopRewardCards.HasTerm(WorkshopSeason.Winter, i)),
                "all six NPC term cards remain accessible");

            Load("{}");
            Require(WorkshopRewardCards.CardCount == 0 && WorkshopRewardCards.TryBeginItemCard(WorkshopItem.Suoxu) &&
                !WorkshopRewardCards.TryBeginItemCard(WorkshopItem.Suoxu), "one item card may be in flight only once");
            WorkshopRewardCards.FinishItemCard(WorkshopItem.Suoxu);
            WorkshopProgress.Award(WorkshopItem.Suoxu);
            Require(WorkshopRewardCards.HasReceivedItemCard(WorkshopItem.Suoxu) &&
                !WorkshopRewardCards.HasTerm(WorkshopSeason.Summer, 0), "matching alone must not grant Lixia before cocoon sorting");
            WorkshopProgress.MarkStep("summer.cocoon-sorted");
            Require(WorkshopRewardCards.HasTerm(WorkshopSeason.Summer, 0), "the second summer sorting game grants Lixia");
            WorkshopProgress.ResetSession();
            Require(WorkshopRewardCards.HasReceivedItemCard(WorkshopItem.Suoxu) &&
                !WorkshopRewardCards.TryBeginItemCard(WorkshopItem.Suoxu), "accepted item receipt survives reload");

            Load("{\"items\":4095,\"chapters\":7,\"dyedColor\":\"紫色\",\"steps\":[\"spring.guyu.fed\",\"summer.cocoon-sorted\",\"summer.xiaoman.reeled\",\"autumn.loom\",\"autumn.bailu.woven\",\"winter.dyes\",\"winter.ground\",\"autumn.hanlu.patterned\",\"winter.xiaoxue.fitted\"],\"observations\":[\"observation.summer.mangzhong.thread\"]}");
            Require(WorkshopRewardCards.CardCount == 16 && !WorkshopRewardCards.IsBusy,
                "all pre-sewing minigames have distinct collected cards");
            Require(WorkshopProgress.CompleteWinter("1_04"), "winter sewing fixture completes");
            Require(WorkshopRewardCards.CardCount == 17 && WorkshopProgress.HasStep("reward.craft.sew"),
                "sewing has its own card independent of fitting");
            WorkshopProgress.MarkStep("winter.tryOn");
            Require(WorkshopRewardCards.CardCount == 18 && WorkshopRewardCards.Definitions.All(WorkshopRewardCards.Earned) &&
                WorkshopRewardCards.HasTerm(WorkshopSeason.Winter, 3), "try-on grants the final craft and Dongzhi cards");
            Debug.Log("[CardRewards] PASS: 18 distinct crafts, success-only gains, term mapping, repeat protection, reload and silent legacy migration.");
        }
        finally
        {
            if (existed) PlayerPrefs.SetString(Key, prior); else PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save(); WorkshopProgress.ResetSession();
        }
    }
}
#endif
