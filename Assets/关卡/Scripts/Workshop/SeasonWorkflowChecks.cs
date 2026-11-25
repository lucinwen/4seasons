#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;

public static class SeasonWorkflowChecks
{
    private const string Key = "ShiXu.Workshop.v1";
    private const string ThreadId = "observation.summer.mangzhong.thread";
    private static void Require(bool result, string message)
    {
        if (!result) throw new Exception("[WorkflowVerify] " + message);
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
            SpringCollectionChecks.Verify();
            Load("{\"chapters\":1,\"items\":63,\"seasonalOrder\":true}");
            Require(!WorkshopProgress.CanSubmit(WorkshopSeason.Summer), "summer delivery requires thread puzzle");
            Require(WorkshopObservationView.ObservationsFor(WorkshopSeason.Summer)
                .Where(WorkshopObservationView.IsSeasonGame).SequenceEqual(new[] { WorkshopObservation.MangzhongCocoon }),
                "exactly one summer puzzle, at Mangzhong");
            Require(WorkshopObservationView.RecordTerm(WorkshopObservation.MangzhongCocoon) == "芒种", "new puzzle term");
            Require(WorkshopObservationView.StationLabel(WorkshopObservation.MangzhongCocoon) == "络纬整经", "rack entry caption");
            var quests = WorkshopQuests.ForSeason(WorkshopSeason.Summer);
            Require(quests.All(quest => quest.Id != "XiazhiOrder"), "no duplicate cooker puzzle quest");
            Require(quests.Any(quest => quest.Id == nameof(WorkshopItem.Suoxu)) &&
                quests.Any(quest => quest.Id == nameof(WorkshopItem.Spool)), "match and maze rewards remain");
            Require(WorkshopProgress.RecordChallenge(ThreadId, 0, 1000, "丝路接通", "111/949/111"), "first puzzle reward");
            WorkshopProgress.RecordSeasonalOrderWork(ThreadId, 0, 1000, "丝路接通", "111/949/111");
            Require(!WorkshopProgress.RecordChallenge(ThreadId, 0, 1000, "丝路接通", "111/949/111"), "repeat gives no duplicate reward");
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Summer), "new rack puzzle completes summer delivery");
            Require(WorkshopProgress.Submit(WorkshopSeason.Summer) && WorkshopProgress.IsUnlocked(WorkshopSeason.Autumn), "autumn unlock");
            WorkshopProgress.ResetSession();
            Require(WorkshopObservationView.OrderWork(WorkshopObservation.MangzhongCocoon)?.sample == "111/949/111", "autumn input survives reload");

            Load("{\"chapters\":1,\"items\":63,\"seasonalOrder\":true,\"observations\":[\"observation.summer.mangzhong\"],\"orderWorks\":[{\"id\":\"observation.summer.mangzhong\",\"score\":100,\"sample\":\"454\"}]}");
            Require(!WorkshopObservationView.HasOrderWork(WorkshopObservation.MangzhongCocoon) &&
                !WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon), "old inspection is not a completed thread puzzle");
            Require(!WorkshopProgress.CanSubmit(WorkshopSeason.Summer), "old inspection cannot skip puzzle");

            Load("{\"chapters\":1,\"items\":63,\"seasonalOrder\":true,\"observations\":[\"observation.summer.dashu.reeling\"],\"challengeWorks\":[{\"id\":\"observation.summer.dashu.reeling\",\"order\":1,\"score\":900,\"sample\":\"111/949/111\"}],\"orderWorks\":[{\"id\":\"observation.summer.dashu.reeling\",\"order\":0,\"score\":1000,\"sample\":\"111/949/111\"}]}");
            Require(WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon), "legacy puzzle page migrates");
            Require(WorkshopObservationView.BestWork(WorkshopObservation.MangzhongCocoon, 1)?.score == 900, "legacy best score migrates");
            Require(WorkshopObservationView.OrderWork(WorkshopObservation.MangzhongCocoon)?.sample == "111/949/111", "legacy sample migrates");
            Require(WorkshopProgress.CanSubmit(WorkshopSeason.Summer), "legacy puzzle completion can deliver");
            Require(!WorkshopProgress.RecordChallenge(ThreadId, 0, 1000, "丝路接通", "111/949/111"), "migrated completion does not reaward page");
            WorkshopProgress.ResetSession();
            Require(WorkshopObservationView.HasOrderWork(WorkshopObservation.MangzhongCocoon), "migration persists");
            Require(JsonUtility.FromJson<Counts>(PlayerPrefs.GetString(Key)).orderWorks.Length == 2, "migration idempotent");
            Debug.Log("[WorkflowVerify] PASS: spring cleanup, Mangzhong routing, summer delivery, one-time rewards, autumn input, legacy migration and reload.");
        }
        finally
        {
            PlayerPrefs.SetString(Key, previous);
            PlayerPrefs.Save();
            WorkshopProgress.ResetSession();
        }
    }

    [Serializable] private sealed class Counts { public WorkshopProgress.ChallengeWork[] orderWorks; }
}
#endif
