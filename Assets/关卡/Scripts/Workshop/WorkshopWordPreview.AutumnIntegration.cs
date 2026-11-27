#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using JingweiWeave;
using JiangnanDye;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed partial class WorkshopWordPreview
{
    public static void BeginAutumnIntegration()
    {
        var host = new GameObject("Autumn Integration Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(ReviewAutumnIntegration()));
    }

    private static void CheckAutumn(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[AutumnIntegration] " + message);
    }

    private static IEnumerator DismissAutumnDialogue()
    {
        for (int i = 0; i < 45 && WorkshopDialoguePresenter.IsOpen; i++)
        {
            yield return new WaitForSecondsRealtime(.04f);
            InvokeNamedButton("Advance Dialogue");
        }
        CheckAutumn(!WorkshopDialoguePresenter.IsOpen, "Dialogue did not finish");
    }

    private static IEnumerator ReviewAutumnIntegration()
    {
        PlayerPrefs.SetString("ShiXu.Workshop.v1", "{\"items\":0,\"chapters\":3,\"solarTerms\":15,\"seasonalOrder\":true,\"steps\":[\"quest.guide.Autumn\",\"winter.hoe\"]}");
        WorkshopProgress.ResetSession();
        WinterLevelState.RestoreWorkshopProgress();
        CheckAutumn(WinterLevelState.HasHoe, "Legacy winter.hoe must remain usable");
        yield return SceneManager.LoadSceneAsync("Level3_Autumn");
        yield return new WaitForSecondsRealtime(.8f);
        var loom = GameObject.Find("3-破损的织机");
        CheckAutumn(loom != null && GameObject.Find("3-修机锤") != null, "Loom or independent hammer missing");
        var player = GameObject.Find("Player");
        player.transform.position = new Vector3(loom.transform.position.x - 1f, -2f, 0f);
        yield return new WaitForSecondsRealtime(.5f);
        CheckAutumn(GameObject.Find("DyeGardenClimbZone") != null && GameObject.Find("RitualClimbZone") != null,
            "Both new porch interactions must be installed");
        CheckAutumn(GameObject.Find("染料园触点").GetComponent<WinterDyeGardenTrigger>() != null,
            "Dye garden entrance missing");
        yield return Capture("40-autumn-integrated-scene");
        var npc = UnityEngine.Object.FindObjectOfType<AutumnNpcDialogue>();
        CheckAutumn(npc != null, "Autumn NPC missing");
        npc.Talk();
        CheckAutumn(AutumnLevelState.HasCake && !AutumnLevelState.HasHammer,
            "First conversation must grant cake independently of hammer");
        yield return DismissAutumnDialogue();
        CheckAutumn(AutumnItemClick.TryCollectHammer(), "Hammer pickup rule failed");
        WorkshopProgress.MarkStep("autumn.loom");
        WorkshopProgress.Award(WorkshopItem.Shuttle);
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level3_Autumn");
        yield return new WaitForSecondsRealtime(.6f);
        player = GameObject.Find("Player");
        CheckAutumn(AutumnLevelState.HasCake && AutumnLevelState.HasHammer && GameObject.Find("3-修机锤") == null,
            "Cake and hammer must survive reload; collected hammer must stay hidden");
        loom = GameObject.Find("3-破损的织机");
        loom.GetComponent<FloatingActionButton>().onClick.Invoke();
        yield return DismissAutumnDialogue();
        yield return new WaitForSecondsRealtime(1f);
        var weave = UnityEngine.Object.FindObjectOfType<WorkshopSimpleGames>();
        CheckAutumn(weave != null, "First loom game must be odd-even shuttle weaving");
        yield return Capture("41-autumn-integrated-weaving");
        for (int row = 0; row < WorkshopPlainWeave.Rows; row++)
        {
            InvokeWorktableButton("Shed " + row % 2);
            yield return null;
            InvokeWorktableButton(row % 2 == 0 ? "Pass Right" : "Pass Left");
            yield return null;
        }
        CheckAutumn(SimplePopup.IsOpen, "Weaving reward popup missing");
        UnityEngine.Object.FindObjectOfType<SimplePopup>().OnConfirm();
        yield return DismissAutumnDialogue();
        yield return new WaitForSecondsRealtime(.5f);
        CheckAutumn(WorkshopProgress.Has(WorkshopItem.GreigeCloth) && WorkshopProgress.HasStep(WorkshopWordRules.Woven),
            "Odd-even weaving must award permanent greige cloth and save woven step");
        CheckAutumn(!WorkshopProgress.Has(WorkshopItem.RawSilkCloth) && !WorkshopProgress.HasStep(WorkshopWordRules.Patterned),
            "Weaving must not grant patterned silk or pattern completion");

        foreach (var color in DyeColorUtil.AllColors)
        {
            WorkshopProgress.RecordRawDyes(new[] { WorkshopArt.RawDye(color, true) });
            WorkshopProgress.RecordGroundDye(color);
        }
        WinterLevelState.RestoreWorkshopProgress();
        WinterDyeingOverlay.Show();
        yield return null;
        var dye = UnityEngine.Object.FindObjectOfType<WinterDyeingOverlay>();
        CheckAutumn(!dye.CanDragSilk && !dye.TryDyeCloth(), "Unselected dye must not start a run");
        InvokeNamedButton("Dye_Purple");
        DragAutumnCloth(dye, false);
        CheckAutumn(!dye.IsDyeing && !WorkshopProgress.Has(WorkshopItem.DyedCloth),
            "A drop outside the vat must return the cloth without a reward");
        DragAutumnCloth(dye, true);
        CheckAutumn(dye.IsDyeing && !dye.TryDyeCloth(), "Dyeing must reject duplicate drops");
        WinterDyeingOverlay.CloseAny();
        yield return new WaitForSecondsRealtime(2.4f);
        CheckAutumn(!WorkshopProgress.Has(WorkshopItem.DyedCloth) && Time.timeScale == 1f,
            "Cancelling during dyeing must not grant cloth or leave the world paused");

        WinterDyeingOverlay.Show();
        yield return null;
        dye = UnityEngine.Object.FindObjectOfType<WinterDyeingOverlay>();
        InvokeNamedButton("Dye_Purple");
        var background = dye.transform.Find("Layout/Autumn Dye Workbench").GetComponent<Image>();
        CheckAutumn(background.sprite == Resources.Load<Sprite>("WordRevision/AutumnDyeWorkbench") &&
            !WorkshopObservationView.IsOpen && dye.GetComponentsInChildren<WinterDyeingSilk>().Length == 1,
            "Dyeing must use the supplied autumn background and exactly one cloth");
        CheckAutumn(dye.transform.Find("Layout/Workbench Instructions") == null &&
            dye.transform.Find("Layout/ConfirmButton") == null,
            "Retired cards and color confirmation must be absent");
        yield return Capture("45-autumn-dye-tabletop");
        DragAutumnCloth(dye, true);
        yield return new WaitForSecondsRealtime(.65f);
        CheckAutumn(dye.IsDyeing && !WorkshopProgress.Has(WorkshopItem.DyedCloth), "Cloth must not be awarded before the dip finishes");
        yield return Capture("46-autumn-dye-dipping");
        float dyeDeadline = Time.realtimeSinceStartup + 8f;
        while (!dye.HasFinishedCloth && Time.realtimeSinceStartup < dyeDeadline) yield return null;
        CheckAutumn(dye.HasFinishedCloth && WorkshopProgress.Has(WorkshopItem.DyedCloth) && !dye.CanDragSilk,
            "One completed dip must award one finished cloth and disable further dyeing");
        CheckAutumn(WorkshopProgress.DyedColor == DyeColorUtil.ChineseName(DyeColor.Purple), "Selected dye color was lost");
        yield return Capture("47-autumn-dye-finished");
        InvokeWorktableButton("Collect Dyed Cloth");
        yield return null;
        CheckAutumn(!WinterDyeingOverlay.IsActive && Time.timeScale == 1f &&
            player.GetComponent<LevelPlayerController>().enabled, "Collecting cloth must restore player controls");
        yield return new WaitForSecondsRealtime(.4f);

        loom.GetComponent<FloatingActionButton>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.7f);
        var pattern = UnityEngine.Object.FindObjectOfType<EmbroideryGameManager>();
        CheckAutumn(pattern != null && pattern.IsHanluPattern, "Dyed cloth must open Hanlu mode");
        CheckAutumn(pattern.uiRoot.transform.Find("Board/Dyed Cloth Base") != null, "Pattern board must show dyed cloth");
        yield return Capture("44-autumn-integrated-hanlu");
        yield return SolveAutumnBoard(pattern);
        CheckAutumn(WorkshopProgress.HasStep(WorkshopWordRules.Patterned), "Hanlu completion must be saved");
        if (SimplePopup.IsOpen) UnityEngine.Object.FindObjectOfType<SimplePopup>().OnConfirm();
        yield return new WaitForSecondsRealtime(.4f);
        WorkshopProgress.ResetSession();
        WinterLevelState.RestoreWorkshopProgress();
        CheckAutumn(WorkshopProgress.Has(WorkshopItem.Shuttle) && WorkshopProgress.Has(WorkshopItem.GreigeCloth) &&
            WorkshopProgress.Has(WorkshopItem.RawSilkCloth) && WorkshopProgress.Has(WorkshopItem.DyedCloth) &&
            WorkshopProgress.HasStep(WorkshopWordRules.Patterned),
            "Weaving, dye and pattern rewards must retain inventory after reload");
        var garden = GameObject.Find("染料园触点");
        player = GameObject.Find("Player");
        player.transform.position = garden.GetComponent<Collider2D>().bounds.center;
        yield return new WaitForSecondsRealtime(.4f);
        garden.GetComponent<FloatingActionButton>().onClick.Invoke();
        float gardenDeadline = Time.realtimeSinceStartup + 12f;
        while (JiangnanDye.GameManager.Instance == null && Time.realtimeSinceStartup < gardenDeadline) yield return null;
        var gardenGame = JiangnanDye.GameManager.Instance;
        CheckAutumn(gardenGame != null && gardenGame.gameObject.scene.name == "MainScene", "Garden entrance must open the dye map");
        yield return new WaitForSecondsRealtime(.6f);
        CheckAutumn(gardenGame.introUI != null && gardenGame.introUI.startButton != null, "Garden intro start control missing");
        if (gardenGame.introUI.gameObject.activeInHierarchy) gardenGame.introUI.startButton.onClick.Invoke();
        yield return new WaitForSecondsRealtime(.6f);
        CheckAutumn(gardenGame.allNodes.Length > 0 && gardenGame.player != null &&
            gardenGame.hudRoot.activeInHierarchy, "Garden exploration must start with resource nodes and player");
        yield return Capture("48-autumn-dye-garden");
        MiniGameOverlay.Close();
        gardenDeadline = Time.realtimeSinceStartup + 12f;
        while (MiniGameOverlay.IsActive && Time.realtimeSinceStartup < gardenDeadline) yield return null;
        CheckAutumn(!MiniGameOverlay.IsActive && SceneManager.GetActiveScene().name == "Level3_Autumn" &&
            player.GetComponent<LevelPlayerController>().enabled, "Garden exit must restore autumn controls");
        Debug.Log("[AutumnIntegration] PASS: actual scene, both stairways, first-talk cake, independent hammer, legacy hoe, odd-even weaving and Hanlu embroidery, user autumn background, one-cloth drag dyeing, wrong drop, cancellation, duplicate prevention, permanent inventory and reload.");
        File.WriteAllText("../WordRevision/autumn-integration-review-complete.txt",
            "Actual Unity 1920x1080 frames. Scene and gameplay callbacks verified with isolated fixtures; original save restored on exit. This is not a complete mouse-and-keyboard playthrough. No player build created.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    private static void DragAutumnCloth(WinterDyeingOverlay dye, bool intoVat)
    {
        var silk = dye.GetComponentInChildren<WinterDyeingSilk>();
        var rect = silk.GetComponent<RectTransform>();
        var target = dye.transform.Find("Layout/VatDropArea").GetComponent<RectTransform>();
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current) {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
        };
        silk.OnBeginDrag(pointer);
        pointer.position = intoVat
            ? RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center))
            : new Vector2(20, 200);
        silk.OnDrag(pointer);
        silk.OnEndDrag(pointer);
    }

    private static IEnumerator SolveAutumnBoard(EmbroideryGameManager game)
    {
        var remaining = (int[])game.colTargets.Clone();
        var masks = new int[EmbroideryGameManager.Rows];
        CheckAutumn(SolveAutumnRows(game, 0, remaining, masks), "Board has no solution");
        for (int row = 0; row < masks.Length; row++)
            for (int col = 0; col < remaining.Length; col++)
                if ((masks[row] & (1 << col)) != 0) game.OnCellClicked(row, col);
        CheckAutumn(game.CheckSuccess(), "Board solution failed counts");
        float deadline = Time.realtimeSinceStartup + 30f;
        while (!SimplePopup.IsOpen && Time.realtimeSinceStartup < deadline) yield return null;
        CheckAutumn(SimplePopup.IsOpen && !MiniGameCompletionAnimation.IsPlaying,
            "Board completion animation did not reach its reward popup");
    }

    private static bool SolveAutumnRows(EmbroideryGameManager game, int row, int[] left, int[] masks)
    {
        if (row == masks.Length) return Array.TrueForAll(left, value => value == 0);
        for (int mask = 0; mask < 1 << left.Length; mask++)
        {
            int count = 0;
            for (int col = 0; col < left.Length; col++) count += (mask >> col) & 1;
            if (count != game.rowTargets[row]) continue;
            bool valid = true;
            for (int col = 0; col < left.Length; col++)
            {
                int next = left[col] - ((mask >> col) & 1);
                if (next < 0 || next > masks.Length - row - 1) valid = false;
            }
            if (!valid) continue;
            for (int col = 0; col < left.Length; col++) left[col] -= (mask >> col) & 1;
            masks[row] = mask;
            if (SolveAutumnRows(game, row + 1, left, masks)) return true;
            for (int col = 0; col < left.Length; col++) left[col] += (mask >> col) & 1;
        }
        return false;
    }
}
#endif
