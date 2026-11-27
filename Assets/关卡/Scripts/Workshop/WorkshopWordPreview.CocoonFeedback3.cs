#if UNITY_EDITOR
using System.Collections;
using System.Linq;
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
        var board = GameObject.Find("Sorting Board")?.GetComponent<RectTransform>();
        var track = board?.Find("Sorting Track") as RectTransform;
        Feedback2Require(board != null && board.GetComponent<RectMask2D>() != null && track != null,
            "Sorting is missing its clipped scrolling track.");
        float readyY = track.anchoredPosition.y;
        InvokeWorktableButton("Start Cocoon Sorting");
        yield return new WaitForSecondsRealtime(.15f);
        Feedback2Require(track.anchoredPosition.y < readyY && Time.timeScale == 0f,
            "Sorting tiles did not move down while the workshop was paused.");
        var target = Feedback3SortCell(track, 0, true);
        var art = target?.transform.Find("Silkworm Cocoon")?.GetComponent<Image>();
        Feedback2Require(art != null && art.enabled && art.sprite != null && !art.raycastTarget,
            "The first sorting target is missing clickable tile art.");
        var images = target.transform.parent.GetComponentsInChildren<Image>();
        foreach (string name in new[] { "Torn Cocoon", "Damp Cocoon", "Large Dew Bead", "Small Dew Bead" })
            Feedback2Require(images.Any(image => image.name == name && image.enabled && image.sprite != null && !image.raycastTarget),
                "The first sorting row is missing defect art: " + name);
        var white = Feedback3SortCell(track, 0, false);
        Feedback2Require(white != null, "The first sorting row is missing a visible white tile.");
        white.onClick.Invoke();
        yield return null;
        Feedback2Require(Feedback3SortingStatus().Contains("点到白块了") &&
            !WorkshopProgress.HasStep(WorkshopWordRules.Sorted), "A white tile did not fail the sorting round.");
        yield return Capture("962-feedback3-cocoon-wrong-tile");
        InvokeWorktableButton("Retry Cocoon Sorting");
        yield return Capture("961-feedback3-cocoon-sorting-game");
        float missDeadline = Time.realtimeSinceStartup + 3f;
        while (!Feedback3SortingStatus().Contains("有蚕茧滑出底线") && Time.realtimeSinceStartup < missDeadline)
            yield return null;
        Feedback2Require(Feedback3SortingStatus().Contains("有蚕茧滑出底线") &&
            !WorkshopProgress.HasStep(WorkshopWordRules.Sorted), "An unattended cocoon did not fail the sorting round.");
        InvokeWorktableButton("Retry Cocoon Sorting");
        yield return null;
        var retriedTarget = Feedback3SortCell(track, 0, true);
        Feedback2Require(retriedTarget != null, "Retry did not begin a new sorting round.");
        retriedTarget.onClick.Invoke();
        InvokeWorktableButton("Leave Cocoon Sorting");
        yield return null;
        Feedback2Require(!WorkshopCocoonSorting.IsOpen && !WorkshopProgress.HasStep(WorkshopWordRules.Sorted) && Time.timeScale == 1f,
            "Leaving an incomplete sorting round changed progress or kept the world paused.");
        WorkshopCocoonSorting.Show();
        yield return null;
        yield return Feedback3SolveSorting();
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
        Debug.Log("[CocoonFeedback3] PASS: pause stays visible through tile travel and opens/resumes; sorting shows cocoon art, scrolls, fails on white tiles or missed cocoons, retries, cancels without reward, completes twenty rows and unlocks Lixia once; reload retains completion.");
    }

    private static string Feedback3SortingStatus()
    {
        var status = GameObject.Find("Sorting Progress")?.GetComponent<Text>();
        Feedback2Require(status != null, "Sorting status is missing.");
        return status.text;
    }

    private static IEnumerator Feedback3SolveSorting()
    {
        var track = GameObject.Find("Sorting Track")?.GetComponent<RectTransform>();
        Feedback2Require(track != null, "Sorting track is missing.");
        InvokeWorktableButton("Start Cocoon Sorting");
        for (int sequence = 0; sequence < WorkshopCocoonSorting.Target; sequence++)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            Button cell;
            while ((cell = Feedback3SortCell(track, sequence, true)) == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Feedback2Require(cell != null, "Sorting row did not become clickable: " + sequence);
            cell.onClick.Invoke();
            yield return null;
        }
    }

    private static Button Feedback3SortCell(RectTransform track, int sequence, bool cocoon)
    {
        var row = track.Find("Sort Row " + sequence) as RectTransform;
        if (row == null) return null;
        var board = (RectTransform)track.parent;
        float rowCenter = board.InverseTransformPoint(row.TransformPoint(row.rect.center)).y;
        if (rowCenter <= board.rect.yMin || rowCenter >= board.rect.yMax) return null;
        foreach (var button in row.GetComponentsInChildren<Button>())
            if (button.IsInteractable() && (button.transform.Find("Silkworm Cocoon") != null) == cocoon)
                return button;
        return null;
    }
}
#endif
