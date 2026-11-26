#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class WorkshopWordPreview
{
    public static IEnumerator ReviewCocoonFeedback3()
    {
        Feedback2Save("{\"items\":0,\"chapters\":1,\"steps\":[\"quest.guide.Summer\"]}");
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return new WaitForSecondsRealtime(.5f);
        MiniGameOverlay.Show("CocoonMatch");
        yield return new WaitForSecondsRealtime(1.1f);
        var game = UnityEngine.Object.FindObjectOfType<ShiXu.CocoonMatch.CocoonMatchController>();
        Feedback2Require(game != null && game.CanPause, "Cocoon matching failed to start.");
        foreach (var tile in game.tiles)
            if (tile.button.interactable) { tile.button.onClick.Invoke(); break; }
        Feedback2Require(game.IsAnimating, "Pause stability fixture did not begin a tile animation.");
        float until = Time.realtimeSinceStartup + .55f;
        while (Time.realtimeSinceStartup < until)
        {
            var launcher = GameObject.Find("Pause Minigame");
            Feedback2Require(game.CanPause && launcher != null && launcher.activeInHierarchy,
                "Pause control disappeared during cocoon travel.");
            yield return null;
        }
        yield return Capture("960-feedback3-stable-pause-control");
        InvokeNamedButton("Pause Minigame");
        yield return null;
        Feedback2Require(WorkshopMinigameMenu.IsPaused && Time.timeScale == 0f, "Pause did not open.");
        InvokeWorktableButton("Resume Minigame");
        MiniGameOverlay.Close();
        yield return new WaitForSecondsRealtime(.6f);

        Feedback2Save("{\"items\":16,\"chapters\":1,\"steps\":[\"quest.guide.Summer\",\"reward.item.4\",\"reward.craft.cocoon\"]}");
        Feedback2Require(!WorkshopRewardCards.HasTerm(WorkshopSeason.Summer, 0), "Lixia was granted before sorting.");
        WorkshopCocoonSorting.Show();
        yield return null;
        Feedback2Require(WorkshopCocoonSorting.IsOpen && Time.timeScale == 0f && WorkshopQuestGuide.IsWorldOverlayActive,
            "Sorting did not open as a blocking worktable.");
        yield return Capture("961-feedback3-cocoon-sorting-game");
        Feedback3SortCell(false).onClick.Invoke();
        yield return null;
        Feedback2Require(!WorkshopProgress.HasStep(WorkshopWordRules.Sorted), "A white tile awarded sorting progress.");
        yield return Capture("962-feedback3-cocoon-wrong-tile");
        InvokeWorktableButton("Retry Cocoon Sorting");
        yield return null;
        Feedback3SortCell(true).onClick.Invoke();
        InvokeWorktableButton("Leave Cocoon Sorting");
        yield return null;
        Feedback2Require(!WorkshopCocoonSorting.IsOpen && !WorkshopProgress.HasStep(WorkshopWordRules.Sorted) && Time.timeScale == 1f,
            "Leaving an incomplete sorting round changed progress or kept the world paused.");
        WorkshopCocoonSorting.Show();
        yield return null;
        for (int i = 0; i < WorkshopCocoonSorting.Target; i++)
        {
            Feedback3SortCell(true).onClick.Invoke();
            yield return null;
        }
        yield return Capture("963-feedback3-cocoon-sorting-finished");
        InvokeWorktableButton("Finish Cocoon Sorting");
        yield return new WaitForSecondsRealtime(.65f);
        Feedback2Require(WorkshopProgress.HasStep(WorkshopWordRules.Sorted) &&
            WorkshopRewardCards.HasTerm(WorkshopSeason.Summer, 0) && WorkshopKnowledgeView.IsOpen,
            "Successful sorting did not unlock Lixia after returning.");
        InvokeNamedButton("Close Solar Detail");
        yield return new WaitForSecondsRealtime(.4f);
        WorkshopCocoonSorting.Show();
        yield return null;
        InvokeWorktableButton("Leave Cocoon Sorting");
        WorkshopProgress.ResetSession();
        yield return new WaitForSecondsRealtime(.5f);
        Feedback2Require(WorkshopProgress.HasStep(WorkshopWordRules.Sorted) && !WorkshopRewardCards.IsBusy,
            "Replaying sorting lost the saved completion or duplicated the card.");
        Debug.Log("[CocoonFeedback3] PASS: pause stays visible through tile travel and opens/resumes; sorting rejects white tiles, retries, cancels without reward, completes twenty rows and unlocks Lixia once; reload retains completion.");
    }

    private static Button Feedback3SortCell(bool cocoon)
    {
        foreach (var button in UnityEngine.Object.FindObjectsOfType<Button>())
        {
            if (!button.name.StartsWith("Sort Cell 3 ") || !button.interactable) continue;
            var label = button.GetComponentInChildren<Text>();
            if ((label != null && label.text == "蚕茧") == cocoon) return button;
        }
        throw new System.InvalidOperationException("Sorting row has no requested active tile.");
    }
}
#endif
