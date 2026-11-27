#if UNITY_EDITOR
using System.Collections;
using JiangnanDye.MazeGame;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class WorkshopWordPreview
{
    /// <summary>Public completion/pickup callbacks, visible accepts and persisted duplicate protection.</summary>
    public static IEnumerator ReviewRewardFeedback3()
    {
        const string quietSave = "{\"items\":0,\"chapters\":15,\"solarTerms\":15,\"steps\":[\"quest.guide.Spring\",\"quest.guide.Summer\",\"quest.guide.Autumn\",\"quest.guide.Winter\",\"reward.term.seen.6\",\"reward.term.seen.7\",\"reward.term.seen.8\"]}";
        Feedback2Save(quietSave);
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(.4f);

        var items = new[] { WorkshopItem.Suoxu, WorkshopItem.Spool, WorkshopItem.Statue,
            WorkshopItem.Shuttle, WorkshopItem.RawSilkCloth, WorkshopItem.DyedCloth };
        foreach (var item in items)
        {
            Feedback2Save(quietSave);
            yield return null;
            int callbacks = 0;
            var first = SimplePopup.ShowItem(item, "领取", () => { WorkshopProgress.Award(item); callbacks++; });
            Feedback2Require(first != null && SimplePopup.IsOpen, "First pickup card is missing: " + item);
            var duplicate = SimplePopup.ShowItem(item, "重复领取", () => callbacks++);
            Feedback2Require(duplicate == null && UnityEngine.Object.FindObjectsOfType<SimplePopup>().Length == 1,
                "An in-flight item card opened twice: " + item);
            if (item == WorkshopItem.Statue) yield return Capture("970-feedback3-single-statue-reward");
            InvokeNamedButton("Confirm");
            yield return null;
            if (item == WorkshopItem.Shuttle) WorkshopProgress.MarkStep("autumn.loom");
            if (item == WorkshopItem.GreigeCloth) WorkshopProgress.MarkStep(WorkshopWordRules.Woven);
            if (item == WorkshopItem.RawSilkCloth) WorkshopProgress.MarkStep(WorkshopWordRules.Patterned);
            if (item == WorkshopItem.DyedCloth)
            {
                WorkshopProgress.MarkStep("winter.dyes");
                WorkshopProgress.MarkStep("winter.ground");
            }
            yield return new WaitForSecondsRealtime(.6f);
            Feedback2Require(callbacks == 2 && WorkshopProgress.Has(item) && WorkshopRewardCards.HasReceivedItemCard(item) &&
                !SimplePopup.IsOpen && !WorkshopRewardCards.IsBusy, "Pickup and automatic craft repeated the same card: " + item);
            Feedback2Require(SimplePopup.ShowItem(item, "再次领取", () => callbacks++) == null && callbacks == 3,
                "An accepted item card repeated or swallowed its continuation: " + item);
            WorkshopProgress.ResetSession();
            Feedback2Require(WorkshopProgress.Has(item) && WorkshopRewardCards.HasReceivedItemCard(item) &&
                SimplePopup.ShowItem(item, "重新加载", () => callbacks++) == null && callbacks == 4,
                "The item-card receipt did not survive reload: " + item);
            WorkshopKnowledgeView.ShowItem(item);
            yield return null;
            Feedback2Require(WorkshopKnowledgeView.IsOpen, "An accepted card can no longer be read in the journal: " + item);
            InvokeNamedButton("Close Detail");
            yield return null;
        }

        // Also cover Award-before-popup: it leaves an automatic craft queued first.
        Feedback2Save(quietSave);
        WorkshopProgress.Award(WorkshopItem.Statue);
        Feedback2Require(WorkshopRewardCards.IsBusy, "The competing automatic-reward fixture did not queue.");
        var pickup = SimplePopup.ShowItem(WorkshopItem.Statue, "领取土地公", null);
        Feedback2Require(pickup != null, "A queued craft swallowed the actual pickup card.");
        InvokeNamedButton("Confirm");
        yield return new WaitForSecondsRealtime(.6f);
        Feedback2Require(!WorkshopRewardCards.IsBusy && !SimplePopup.IsOpen, "Award-before-popup left a second item card.");

        Feedback2Save("{\"items\":0,\"chapters\":1,\"steps\":[\"quest.guide.Summer\"]}");
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return new WaitForSecondsRealtime(.4f);
        WorkshopRewardCards.FinishItemCard(WorkshopItem.Suoxu);
        WorkshopProgress.Award(WorkshopItem.Suoxu);
        Feedback2Require(!WorkshopRewardCards.HasTerm(WorkshopSeason.Summer, 0) && !WorkshopRewardCards.IsBusy,
            "Lixia appeared after matching rather than the second sorting game.");
        WorkshopProgress.MarkStep("summer.cocoon-sorted");
        yield return new WaitForSecondsRealtime(.7f);
        RewardFeedback3TermOnly(null, "Lixia after cocoon sorting");
        yield return Capture("971-feedback3-lixia-after-sorting");
        InvokeNamedButton("Close Solar Detail");
        yield return new WaitForSecondsRealtime(.4f);

        var mazeHost = new GameObject("Maze Completion Reward Review");
        var maze = mazeHost.AddComponent<MazeRewardPopup>();
        maze.Show();
        yield return null;
        RewardFeedback3TermOnly("WorkshopCraftSolar/Xiaoman", "maze Xiaoman");
        Feedback2Require(WorkshopProgress.Has(WorkshopItem.Spool), "Maze reward lost its inventory spool.");
        yield return Capture("972-feedback3-xiaoman-maze-card");
        InvokeNamedButton("Close Solar Detail");
        yield return new WaitForSecondsRealtime(.6f);
        int returned = 0;
        WorkshopRewardCards.ShowTermReward(WorkshopSeason.Summer, 1, () => returned++);
        Feedback2Require(returned == 1 && !WorkshopRewardCards.IsBusy && !WorkshopKnowledgeView.IsOpen,
            "Maze Xiaoman repeated after it was accepted.");

        WorkshopProgress.RecordObservation("observation.summer.mangzhong.thread");
        yield return new WaitForSecondsRealtime(.7f);
        RewardFeedback3TermOnly("WorkshopCraftSolar/Mangzhong", "thread Mangzhong");
        yield return Capture("973-feedback3-mangzhong-thread-card");
        InvokeNamedButton("Close Solar Detail");
        yield return new WaitForSecondsRealtime(.6f);
        WorkshopProgress.RecordObservation("observation.summer.mangzhong.thread");
        yield return new WaitForSecondsRealtime(.4f);
        Feedback2Require(!WorkshopRewardCards.IsBusy && !WorkshopRewardCards.HasTerm(WorkshopSeason.Summer, 3),
            "The thread puzzle repeated its material knowledge or added an unrelated Xiazhi card.");

        Feedback2Save("{\"items\":4800,\"chapters\":3,\"steps\":[\"quest.guide.Autumn\",\"autumn.loom\",\"autumn.bailu.woven\",\"winter.dyes\",\"winter.ground\"]}");
        yield return SceneManager.LoadSceneAsync("Level3_Autumn");
        yield return new WaitForSecondsRealtime(.4f);
        WorkshopWordRules.CompletePattern();
        yield return new WaitForSecondsRealtime(.7f);
        RewardFeedback3TermOnly(null, "Hanlu pattern completion");
        yield return Capture("974-feedback3-pattern-term-only");
        InvokeNamedButton("Close Solar Detail");
        yield return new WaitForSecondsRealtime(.6f);
        Feedback2Require(!WorkshopRewardCards.IsBusy && !SimplePopup.IsOpen,
            "Pattern completion issued a RawSilkCloth knowledge card again.");
        Debug.Log("[RewardFeedback3] PASS: six pickup items accept once across in-flight calls, competing automatic rewards and reload; explicit journal reads remain available; Lixia waits for sorting; maze presents only supplied Xiaoman, thread only supplied Mangzhong; Hanlu pattern never repeats greige-cloth knowledge.");
    }

    private static void RewardFeedback3TermOnly(string resource, string context)
    {
        Feedback2Require(WorkshopKnowledgeView.IsOpen && !WorkshopRewardCardView.IsOpen && !SimplePopup.IsOpen,
            "Expected one term card, without a second item reward: " + context);
        RequireSingleCard("Workshop Knowledge");
        Feedback2Require(GameObject.Find("Item Card Illustration") == null, "Unexpected item-knowledge card: " + context);
        if (resource == null) return;
        var expected = Resources.Load<Sprite>(resource);
        bool found = false;
        foreach (var image in GameObject.Find("Workshop Knowledge").GetComponentsInChildren<Image>())
            if (image.sprite == expected && expected != null) found = true;
        Feedback2Require(found, "The term reward is not the supplied craft artwork: " + resource);
    }
}
#endif
