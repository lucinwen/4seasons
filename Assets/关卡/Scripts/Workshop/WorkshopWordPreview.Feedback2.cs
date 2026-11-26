#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Regression fixtures for the second playable-build feedback round.</summary>
public sealed partial class WorkshopWordPreview
{
    public static void BeginFeedback2()
    {
        var host = new GameObject("Workshop Feedback Two Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(ReviewFeedback2()));
    }

    private static void Feedback2Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("[Feedback2Review] " + message);
    }

    private static void Feedback2Save(string json)
    {
        PlayerPrefs.SetString("ShiXu.Workshop.v1", json);
        WorkshopProgress.ResetSession();
        WorkshopRewardCards.Rebase();
    }

    private static LevelPlayerController Feedback2Player()
    {
        foreach (var player in UnityEngine.Object.FindObjectsOfType<LevelPlayerController>())
            if (player.gameObject.scene == SceneManager.GetActiveScene()) return player;
        throw new InvalidOperationException("[Feedback2Review] Missing scene player.");
    }

    private static void Feedback2Npc()
    {
        var player = Feedback2Player();
        NpcTalkButton guide = null;
        foreach (var npc in UnityEngine.Object.FindObjectsOfType<NpcTalkButton>())
            if (npc.gameObject.scene == player.gameObject.scene) { guide = npc; break; }
        Feedback2Require(guide != null, "Missing seasonal NPC.");
        Feedback2Require(Mathf.Abs(guide.transform.position.x - player.transform.position.x) <= 4.3f,
            "NPC is farther than a short walk from the entrance: " + player.gameObject.scene.name);
        var sprite = guide.GetComponent<SpriteRenderer>();
        Feedback2Require(sprite != null, "NPC sprite missing.");
        float feet = sprite.bounds.min.y;
        bool supported = false;
        foreach (var hit in Physics2D.RaycastAll(new Vector2(guide.transform.position.x, feet + .35f), Vector2.down, .8f))
        {
            var collider = hit.collider;
            if (collider == null || collider.isTrigger || collider.attachedRigidbody != null ||
                collider.gameObject.scene != guide.gameObject.scene || collider.GetComponent<Renderer>() != null || hit.normal.y < .45f) continue;
            if (Mathf.Abs(feet - hit.point.y) <= .25f) supported = true;
        }
        Feedback2Require(supported, "Relocated NPC feet are not supported by the floor: " + guide.gameObject.scene.name);
        Debug.Log("[Feedback2Review] NPC supported near entrance: " + guide.gameObject.scene.name + " " + guide.transform.position);
    }

    private static IEnumerator ReviewFeedback2()
    {
        yield return ReviewOriginalCardLegibility();
        yield return ReviewWinterSceneRevision();
        Feedback2Save("{\"items\":0,\"chapters\":7,\"seasonalOrder\":true,\"steps\":[\"quest.guide.Spring\",\"quest.guide.Summer\",\"quest.guide.Autumn\",\"quest.guide.Winter\"]}");
        string[] levels = { "Level1_Spring", "Level2_Summer", "Level3_Autumn", "Level4_Winter" };
        for (int i = 0; i < levels.Length; i++)
        {
            yield return SceneManager.LoadSceneAsync(levels[i]);
            yield return new WaitForSecondsRealtime(.7f);
            Feedback2Npc();
            yield return Capture("80" + i + "-feedback2-entrance-" + levels[i]);
            if (i == 2) yield return Feedback2Offering();
        }

        yield return ReviewSummerBridgePassage();
        yield return ReviewHarvestKnowledgeCard();
        yield return ReviewWashSensitivity();

        // Use the actual bucket action, wipe callbacks, collect control, and completion popup.
        Feedback2Save("{\"items\":1,\"chapters\":0,\"seasonalOrder\":true,\"steps\":[\"quest.guide.Spring\",\"spring.eggPaper\"]}");
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(.65f);
        SilkwormWashingController bucket = null;
        foreach (var controller in UnityEngine.Object.FindObjectsOfType<SilkwormWashingController>())
            if (controller.role == SilkwormWashingController.RoleType.Bucket) { bucket = controller; break; }
        Feedback2Require(bucket != null && SpringSummerNpcDialogue.CanWash && SpringPrompt.HasEggPaper,
            "The bucket fixture is not ready.");
        var bucketAction = bucket.GetComponent<FloatingActionButton>();
        Feedback2Require(bucketAction != null, "The bucket action is missing.");
        bucketAction.onClick.Invoke();
        yield return null;
        var wash = UnityEngine.Object.FindObjectOfType<SpringSilkwormWashOverlay>();
        Feedback2Require(wash != null && SpringSilkwormWashOverlay.IsOpen, "The bucket did not start the washing worktable.");
        var washFrame = (RectTransform)wash.transform;
        wash.BeginDrag(TablePointer(washFrame, new Vector2(1590, 800)));
        for (int pass = 0; pass < 8 && !wash.IsComplete; pass++)
        {
            wash.Drag(TablePointer(washFrame, new Vector2(240, 760)));
            for (int row = 0; row < 7; row++)
                for (int column = 0; column < 15; column++)
                    wash.Drag(TablePointer(washFrame, new Vector2(460 + (row % 2 == 0 ? column : 14 - column) * 70, 430 + row * 65)));
        }
        Feedback2Require(wash.IsComplete, "The wet cloth could not finish cleaning.");
        wash.EndDrag(TablePointer(washFrame, new Vector2(1440, 820)));
        InvokeNamedButton("Collect Clean Eggs");
        yield return null;
        Feedback2Require(SilkwormWashingController.washingCompleted && WorkshopProgress.Has(WorkshopItem.SilkwormEgg) &&
            SimplePopup.IsOpen && Feedback2HasText("浴蚕完成"), "Washing did not show its completion confirmation.");
        yield return Capture("805-feedback2-washing-completion");
        WorkshopRewardCards.Rebase();
        UnityEngine.Object.FindObjectOfType<SimplePopup>().OnConfirm();
        yield return null;
        yield return Feedback2Feeding();
        yield return Feedback2Summer();
        yield return ReviewFeedback2Cards();
        yield return ReviewChapterCompletionLayout();
        Debug.Log("[Feedback2Review] PASS: four grounded entrance NPCs; expanded offering hit target; high-rack feeding and bamboo tray; washing and feeding completion confirmations; original cocoon background; automatic summer synthesis; reward cards; dynamic bridge passage; original card copy and HD assets; chapter completion controls; revised winter courtyard.");
        File.WriteAllText("../WordRevision/feedback2-review-complete.txt",
            "Actual Unity Editor frames, 1920x1080. Isolated scene and UI callback fixtures passed; original save restored by Editor review host.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    private static bool Feedback2HasText(string value)
    {
        foreach (var text in UnityEngine.Object.FindObjectsOfType<Text>())
            if (text.text.Contains(value)) return true;
        return false;
    }

    private static IEnumerator Feedback2Offering()
    {
        var ritual = UnityEngine.Object.FindObjectOfType<AutumnRitualTrigger>();
        Feedback2Require(ritual != null, "Autumn offering trigger missing.");
        var collider = ritual.GetComponent<Collider2D>();
        Vector2 center = collider != null
            ? (collider.enabled ? (Vector2)collider.bounds.center : (Vector2)collider.transform.TransformPoint(collider.offset))
            : (Vector2)ritual.transform.position;
        var player = Feedback2Player();
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.velocity = Vector2.zero; body.simulated = false; }
        player.transform.position = new Vector3(center.x + 4.4f, center.y, player.transform.position.z);
        yield return new WaitForSecondsRealtime(.4f);
        var action = ritual.GetComponent<FloatingActionButton>();
        Feedback2Require(action != null && action.IsVisible && action.buttonSize.x >= 300 && action.buttonSize.y >= 84,
            "Offering action should be large and available 4.4 units from its actual center.");
        Button clickable = null;
        foreach (var button in UnityEngine.Object.FindObjectsOfType<Button>())
        {
            var label = button.GetComponentInChildren<Text>();
            if (button.name == "Action Tag" && label != null && label.text == action.buttonText) { clickable = button; break; }
        }
        Feedback2Require(clickable != null && ((RectTransform)clickable.transform).rect.width >= 360 &&
            ((RectTransform)clickable.transform).rect.height >= 124, "Offering click area did not include the added padding.");
        yield return Capture("804-feedback2-offering-large-action");
        clickable.onClick.Invoke();
        yield return null;
        Feedback2Require(AutumnOfferingView.IsOpen, "The expanded offering control does not open the offering table.");
        yield return Capture("804b-feedback2-offering-table");
        InvokeWorktableButton("Return From Offering Table");
        yield return null;
    }

    private static IEnumerator Feedback2Feeding()
    {
        var station = UnityEngine.Object.FindObjectOfType<WorkshopWordStation>();
        Feedback2Require(station != null && (station.name == "1-蚕簇架1 (1)" || station.name == "1-蚕簇架1"),
            "Feeding entry is not attached to the tall rack by the steps.");
        var player = Feedback2Player();
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.velocity = Vector2.zero; body.simulated = false; }
        player.transform.position = station.transform.position + new Vector3(-1.5f, 0, 0);
        yield return new WaitForSecondsRealtime(.45f);
        var action = station.GetComponent<FloatingActionButton>();
        Feedback2Require(action != null && action.IsVisible && action.buttonText == "谷雨 · 喂蚕",
            "The tall rack does not present the feeding action.");
        yield return Capture("806-feedback2-feeding-rack-entry");
        action.onClick.Invoke();
        yield return null;
        var table = UnityEngine.Object.FindObjectOfType<WorkshopSimpleGames>();
        Feedback2Require(table != null, "The rack did not open feeding.");
        var workspace = table.transform.Find("Worktable/Workspace");
        Feedback2Require(workspace != null, "Feeding workspace missing.");
        var bamboo = workspace.Find("Mulberry Bamboo Tray");
        Feedback2Require(bamboo != null && bamboo.GetComponent<Image>().sprite == Resources.Load<Sprite>("WordRevision/FeedingLeafTray"),
            "Leaves do not have the bamboo tray artwork.");
        var tray = (RectTransform)workspace.Find("Silkworm Tray");
        Feedback2Require(tray != null, "Silkworm feeding target missing.");
        yield return Capture("807-feedback2-feeding-bamboo-tray");
        for (int i = 0; i < 3; i++)
        {
            var leaf = workspace.Find("Mulberry Portion " + i);
            Feedback2Require(leaf != null && leaf.gameObject.activeSelf, "A feeding portion is missing.");
            var drag = leaf.GetComponent<WorkshopWordDrag>();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)leaf).TransformPoint(((RectTransform)leaf).rect.center)) };
            drag.OnBeginDrag(pointer);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, tray.TransformPoint(tray.rect.center));
            drag.OnDrag(pointer);
            drag.OnEndDrag(pointer);
            yield return null;
        }
        Feedback2Require(WorkshopProgress.HasStep(WorkshopWordRules.Fed) && WorkshopProgress.Has(WorkshopItem.Silkworm) &&
            SimplePopup.IsOpen && Feedback2HasText("谷雨喂蚕完成"), "Three leaf drops did not show the feeding completion popup.");
        yield return Capture("808-feedback2-feeding-completion");
        WorkshopRewardCards.Rebase();
        UnityEngine.Object.FindObjectOfType<SimplePopup>().OnConfirm();
        yield return null;
        Feedback2Require(!WorkshopSimpleGames.IsOpen && Time.timeScale == 1f, "Confirming feeding did not return to the spring world.");
    }

    private static IEnumerator Feedback2Summer()
    {
        Feedback2Save("{\"items\":63,\"chapters\":1,\"seasonalOrder\":true,\"steps\":[\"quest.guide.Summer\",\"spring.guyu.fed\",\"summer.xiaoman.reeled\",\"dialogue.review.Summer\"]}");
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return new WaitForSecondsRealtime(.6f);
        MiniGameOverlay.Show("CocoonMatch");
        yield return new WaitForSecondsRealtime(1.2f);
        var game = UnityEngine.Object.FindObjectOfType<ShiXu.CocoonMatch.CocoonMatchController>();
        Feedback2Require(game != null && game.gameSurface.activeInHierarchy && game.Model != null, "Cocoon match did not start.");
        foreach (string part in new[] { "CocoonOrnatePanel", "PanelShadow", "TealBackdrop" })
        {
            var node = GameObject.Find(part);
            Feedback2Require(node != null && node.activeInHierarchy, "Original cocoon background part missing: " + part);
        }
        Feedback2Require(GameObject.Find("Cocoon Workshop Scenery") == null, "Cocoon game still uses the replacement scenery.");
        yield return Capture("809-feedback2-cocoon-original-background");
        MiniGameOverlay.Close();
        yield return new WaitForSecondsRealtime(.5f);
        Feedback2Require(!WorkshopStorageView.IsOpen, "Summer synthesis opened before all work was complete.");
        WorkshopProgress.RecordObservation("observation.summer.mangzhong.thread");
        WorkshopRewardCards.Rebase();
        float deadline = Time.realtimeSinceStartup + 4;
        while (!WorkshopStorageView.IsOpen && Time.realtimeSinceStartup < deadline) yield return null;
        Feedback2Require(WorkshopStorageView.IsOpen, "Finishing summer did not automatically open the storage workbench.");
        Button combine = null;
        foreach (var button in UnityEngine.Object.FindObjectsOfType<Button>())
            if (button.name == "Synthesis Combine" || (button.name == "Action" && button.transform.parent.name == "Synthesis Combine")) { combine = button; break; }
        Feedback2Require(combine != null && combine.interactable && Feedback2HasText("开始合成"),
            "The summer storage workbench is not ready for immediate synthesis.");
        yield return Capture("810-feedback2-summer-storage-ready");
        combine.onClick.Invoke();
        yield return new WaitForSecondsRealtime(1.4f);
        Feedback2Require(WorkshopProgress.IsComplete(WorkshopSeason.Summer), "The summer synthesis action failed.");
        if (WorkshopCraftRecordView.IsOpen) InvokeWorktableButton("Continue from Result");
        yield return null;
        yield return Capture("811-feedback2-summer-synthesized");
        InvokeWorktableButton("Storage Close");
        yield return null;
        Feedback2Require(!WorkshopStorageView.IsOpen && SceneManager.GetActiveScene().name == "Level2_Summer",
            "Leaving summer storage did not return to the summer scene.");
    }
}
#endif
