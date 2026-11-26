#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class WorkshopWordPreview
{
    private static IEnumerator ReviewFeedback2Cards()
    {
        const string key = "ShiXu.Workshop.v1";
        PlayerPrefs.SetString(key, "{\"items\":3,\"steps\":[\"quest.guide.Spring\"]}");
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(.6f);
        RequireImmersive(WorkshopRewardCards.CardCount == 2 && !WorkshopRewardCards.IsBusy,
            "old earned cards must load silently");

        WorkshopSimpleGames.Show(WorkshopSimpleGame.Feeding);
        yield return new WaitForSecondsRealtime(.2f);
        CloseWorktable();
        yield return new WaitForSecondsRealtime(.6f);
        RequireImmersive(!WorkshopRewardCards.IsBusy && WorkshopRewardCards.CardCount == 2,
            "cancelled feeding granted a card");

        WorkshopWordRules.CompleteFeeding();
        yield return new WaitForSecondsRealtime(.8f);
        RequireImmersive(WorkshopRewardCards.IsOpen && WorkshopRewardCards.CardCount == 3 &&
            WorkshopQuestGuide.BlocksWorldInput, "completed feeding did not display/block input for reward");
        RequireImmersive(GameObject.Find("Item Card Title") != null && GameObject.Find("Item Card Description") != null,
            "craft illustration is missing its native card labels");
        RequireSingleCard("Workshop Craft Cards");
        RequireImmersive(GameObject.Find("Craft Reward Board") == null &&
            GameObject.Find("Item Card Title").GetComponent<Text>().text == "蚕宝宝" &&
            GameObject.Find("Item Card Description").GetComponent<Text>().text.Replace("\n", "").Replace("\r", "") ==
                "蚕宝宝会经历不同的生长阶段，随着不断蜕皮逐渐长大，最终吐丝结茧，完成生命蜕变。",
            "craft reward must preserve the original Silkworm knowledge, rather than replace it with a craft summary");
        yield return Capture("77-feeding-craft-reward");
        InvokeNamedButton("Accept Craft Card");
        yield return new WaitForSecondsRealtime(.6f);
        RequireImmersive(!WorkshopRewardCards.IsBusy, "previously owned Guyu should not be awarded again after feeding");

        // A newly acquired term follows its craft as a separate, single-card popup.
        Feedback2Save("{\"items\":1,\"steps\":[\"quest.guide.Spring\"]}");
        WorkshopProgress.Award(WorkshopItem.SilkwormEgg);
        yield return new WaitForSecondsRealtime(.8f);
        RequireImmersive(WorkshopRewardCards.IsOpen, "new washing craft was not presented");
        RequireSingleCard("Workshop Craft Cards");
        InvokeNamedButton("Accept Craft Card");
        yield return new WaitForSecondsRealtime(.8f);
        RequireImmersive(WorkshopKnowledgeView.IsOpen && WorkshopRewardCards.IsOpen &&
            !WorkshopRewardCardView.IsOpen, "new Guyu should follow craft as a standalone term card");
        RequireSingleCard("Workshop Knowledge");
        RequireImmersive(GameObject.Find("Solar Review Notes") == null, "solar reward still has an external footer");
        yield return Capture("78-guyu-solar-reward");
        InvokeNamedButton("Close Solar Detail");
        yield return new WaitForSecondsRealtime(.6f);
        RequireImmersive(!WorkshopRewardCards.IsBusy && !WorkshopKnowledgeView.IsOpen,
            "collecting the last standalone card did not return to the scene");
        WorkshopWordRules.CompleteFeeding();
        yield return new WaitForSecondsRealtime(.8f);
        RequireImmersive(WorkshopRewardCardView.IsOpen && WorkshopRewardCards.CardCount == 3,
            "new feeding craft should remain distinct from washing");
        InvokeNamedButton("Accept Craft Card");
        yield return new WaitForSecondsRealtime(.6f);
        WorkshopWordRules.CompleteFeeding();
        yield return new WaitForSecondsRealtime(.6f);
        RequireImmersive(!WorkshopRewardCards.IsBusy && WorkshopRewardCards.CardCount == 3,
            "repeated completion duplicated the craft reward");

        WorkshopKnowledgeView.ShowJournal(WorkshopSeason.Spring);
        yield return null;
        InvokeNamedButton("Item Card SilkwormEgg");
        yield return null;
        RequireSingleCard("Workshop Knowledge");
        yield return Capture("78b-egg-knowledge-cutout");
        InvokeNamedButton("Close Detail");
        yield return null;
        InvokeWorktableButton("Craft Reward Cards");
        yield return null;
        RequireImmersive(WorkshopRewardCards.IsOpen, "journal craft collection entry is unavailable");
        InvokeNamedButton("Open Craft feed");
        yield return null;
        InvokeNamedButton("Accept Craft Card");
        yield return null;
        InvokeNamedButton("Craft Season Autumn");
        yield return null;
        var locked = GameObject.Find("Open Craft dyeGather")?.GetComponent<Button>();
        RequireImmersive(locked != null && !locked.interactable, "unfinished crafts must stay locked");
        yield return Capture("79-craft-card-collection");
        InvokeNamedButton("Next Craft Page");
        yield return null;
        RequireImmersive(GameObject.Find("Open Craft pattern") != null,
            "autumn collection second page is missing");
        InvokeNamedButton("Close Craft Collection");
        yield return null;
        // Close the underlying journal through its own navigation action.
        InvokeWorktableButton("Close");
        yield return null;
        Debug.Log("[Feedback2Cards] PASS: cutout craft and solar cards, one accept button, sequential new term reward, cancellation, journal entry, pagination, locks and repeat protection.");
    }

    private static void RequireSingleCard(string canvasName)
    {
        var canvas = GameObject.Find(canvasName);
        RequireImmersive(canvas != null, "missing card canvas: " + canvasName);
        var buttons = canvas.GetComponentsInChildren<Button>();
        RequireImmersive(buttons.Length == 1 && buttons[0].interactable,
            "card popup must have exactly one working accept button: " + canvasName);
        bool accept = false;
        foreach (var text in canvas.GetComponentsInChildren<Text>())
            if (text.text == "收下卡片") accept = true;
        RequireImmersive(accept, "card popup is missing its accept label: " + canvasName);
    }
}
#endif
