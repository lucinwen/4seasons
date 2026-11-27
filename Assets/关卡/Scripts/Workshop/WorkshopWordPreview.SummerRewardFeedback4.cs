#if UNITY_EDITOR
using System.Collections;
using JiangnanDye.MazeGame;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class WorkshopWordPreview
{
    public static IEnumerator ReviewSummerRewardFeedback4()
    {
        Feedback2Save("{\"items\":8,\"chapters\":1,\"steps\":[\"quest.guide.Summer\",\"summer.water\",\"summer.wood\"]}");
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(.5f);

        int accepted = 0;
        var first = SimplePopup.ShowItem(WorkshopItem.Suoxu, "收茧完成", () =>
        { accepted++; WorkshopProgress.Award(WorkshopItem.Suoxu); });
        var duplicate = SimplePopup.Show("索绪", "收茧完成", "收下", () => accepted += 10,
            icon: WorkshopArt.ItemCard(WorkshopItem.Suoxu));
        Feedback2Require(first != null && duplicate == null && accepted == 0 &&
            !WorkshopProgress.Has(WorkshopItem.Suoxu), "A duplicate callback continued before accepting the first Suoxu card.");
        yield return Capture("980-feedback4-one-suoxu-card");
        first.OnConfirm();
        yield return new WaitForSecondsRealtime(.6f);
        Feedback2Require(accepted == 11 && WorkshopProgress.Has(WorkshopItem.Suoxu) &&
            WorkshopRewardCards.HasReceivedItemCard(WorkshopItem.Suoxu) && !SimplePopup.IsOpen &&
            !WorkshopRewardCards.IsBusy, "Confirming Suoxu did not finish both callbacks exactly once.");
        SimplePopup.Show("索绪", "再次收茧", "收下", () => accepted++, icon: WorkshopArt.ItemCard(WorkshopItem.Suoxu));
        Feedback2Require(accepted == 12 && !SimplePopup.IsOpen, "The generic popup repeated an accepted Suoxu card.");

        WorkshopCocoonSorting.Show();
        yield return null;
        yield return Feedback3SolveSorting();
        InvokeWorktableButton("Finish Cocoon Sorting");
        yield return new WaitForSecondsRealtime(.65f);
        RewardFeedback3TermOnly(null, "real cocoon sorting completion");
        InvokeNamedButton("Close Solar Detail");
        yield return new WaitForSecondsRealtime(.4f);

        var mazeHost = new GameObject("Real Maze Reward Callback");
        mazeHost.AddComponent<MazeRewardPopup>().Show();
        yield return null;
        RewardFeedback3TermOnly("WorkshopCraftSolar/Xiaoman", "real maze completion");
        yield return Capture("981-feedback4-maze-only-xiaoman");
        InvokeNamedButton("Close Solar Detail");
        yield return new WaitForSecondsRealtime(.9f);
        Feedback2Require(WorkshopProgress.Has(WorkshopItem.Spool) && WorkshopRewardCards.HasReceivedItemCard(WorkshopItem.Spool) &&
            !SimplePopup.IsOpen && !WorkshopRewardCards.IsBusy, "Returning from the maze repeated Spool.");

        WorkshopProgress.MarkStep(WorkshopWordRules.Reeled);
        yield return new WaitForSecondsRealtime(.5f);
        Feedback2Require(!WorkshopRewardCards.IsBusy && !SimplePopup.IsOpen, "Reeling repeated the already-received Spool card.");
        for (int replay = 0; replay < 2; replay++)
        {
            var wheel = GameObject.Find(SummerItemInteraction.WindingWheelObjectName);
            wheel.GetComponent<FloatingActionButton>().onClick.Invoke();
            yield return null;
            var view = Object.FindObjectOfType<WorkshopObservationView>();
            Feedback2Require(view != null && WorkshopObservationView.ActiveGame == WorkshopObservation.MangzhongCocoon,
                "The actual winding-wheel entry did not open thread connection.");
            for (int level = 0; level < XiazhiPuzzle.LevelCount; level++)
            {
                Feedback2Require(view.XiazhiReviewLevel == level, "Thread rounds advanced out of order.");
                yield return SolveXiazhiReviewBoard(level);
                float deadline = Time.realtimeSinceStartup + 4f;
                while (view.XiazhiReviewAdvancing && Time.realtimeSinceStartup < deadline) yield return null;
                Feedback2Require(!view.XiazhiReviewAdvancing, "Thread completion animation stalled.");
            }
            Feedback2Require(view.XiazhiReviewCanReturn && !SimplePopup.IsOpen && !WorkshopRewardCardView.IsOpen,
                "Thread completion displayed an item card before returning.");
            InvokeWorktableButton("Xiazhi Return To Summer");
            yield return new WaitForSecondsRealtime(.7f);
            if (replay == 0)
            {
                RewardFeedback3TermOnly("WorkshopCraftSolar/Mangzhong", "real three-round thread completion");
                yield return Capture("982-feedback4-thread-only-mangzhong");
                InvokeNamedButton("Close Solar Detail");
                yield return new WaitForSecondsRealtime(.6f);
            }
            Feedback2Require(!WorkshopKnowledgeView.IsOpen && !WorkshopRewardCards.IsBusy && !SimplePopup.IsOpen,
                "Thread completion or replay queued Suoxu, Spool, or another card.");
        }

        WorkshopRewardCards.ShowCollection(WorkshopSeason.Summer);
        yield return null;
        foreach (string craft in new[] { "maze", "reeling", "thread" })
        {
            InvokeNamedButton("Open Craft " + craft);
            yield return null;
            var detail = Object.FindObjectOfType<WorkshopKnowledgeView>();
            var expected = Resources.Load<Sprite>("WorkshopCraftSolar/" + (craft == "thread" ? "Mangzhong" : "Xiaoman"));
            bool correctArt = false;
            if (detail != null)
                foreach (var art in detail.GetComponentsInChildren<UnityEngine.UI.Image>())
                    if (art.sprite == expected && expected != null) correctArt = true;
            Feedback2Require(WorkshopKnowledgeView.IsOpen && correctArt,
                "The craft collection displayed the old material card for " + craft);
            InvokeNamedButton("Close Solar Detail");
            yield return null;
        }
        InvokeNamedButton("Close Craft Collection");
        yield return new WaitForSecondsRealtime(.4f);
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return new WaitForSecondsRealtime(.7f);
        Feedback2Require(WorkshopRewardCards.HasReceivedItemCard(WorkshopItem.Suoxu) &&
            WorkshopRewardCards.HasReceivedItemCard(WorkshopItem.Spool) && !WorkshopRewardCards.IsBusy,
            "Reloading summer forgot accepted material cards.");
        Debug.Log("[SummerRewardFeedback4] PASS: generic and explicit Suoxu callbacks merge until acceptance; sorting and maze completion callbacks; three actual thread rounds plus full replay; no repeated material cards; correct craft collection details and reload.");
    }
}
#endif
