#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using JiangnanDye;
using JingweiWeave;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Rendered fixtures exercise runtime callbacks with an isolated save.</summary>
public sealed partial class WorkshopWordPreview
{
    public static void BeginImmersive()
    {
        var host = new GameObject("Immersive Workshop Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(ReviewImmersive()));
    }

    public static void BeginCocoonRelease()
    {
        var host=new GameObject("Cocoon Release Review");DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(ReviewCocoonRelease()));
    }

    private static IEnumerator ReviewCocoonRelease()
    {
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return new WaitForSecondsRealtime(.5f);
        MiniGameOverlay.Show("CocoonMatch");yield return new WaitForSecondsRealtime(1.3f);
        var game=UnityEngine.Object.FindObjectOfType<ShiXu.CocoonMatch.CocoonMatchController>();
        RequireImmersive(game!=null && game.gameSurface.activeInHierarchy && game.Model!=null,"Cocoon board is hidden");
        int active=0;
        foreach(var tile in game.tiles)if(tile.gameObject.activeInHierarchy && tile.icon.sprite!=null)active++;
        RequireImmersive(active==game.tiles.Count && active>0,"Cocoon tiles not visible");
        yield return Capture("59-summer-cocoons-immersive");
        foreach(var tile in game.tiles)
            if(tile.button.interactable){tile.button.onClick.Invoke();break;}
        yield return new WaitForSecondsRealtime(.7f);
        RequireImmersive(game.Model.Tray.Count>0,"Cocoon tile could not be collected");
        yield return Capture("59b-summer-cocoon-collected");
        MiniGameOverlay.Close();yield return new WaitForSecondsRealtime(.4f);
        Debug.Log("[CocoonRelease] PASS: visible tiles, direct start, collection, exit.");
        File.WriteAllText("../WordRevision/immersive-review-complete.txt","Full immersive review passed before final cocoon hierarchy fix; focused cocoon visible/collect/exit regression passed. Original save restored on exit.\n");
        UnityEditor.EditorApplication.isPlaying=false;
    }

    private static void RequireImmersive(bool condition, string detail)
    {
        if (!condition) throw new InvalidOperationException("[ImmersiveReview] " + detail);
    }

    private static PointerEventData TablePointer(RectTransform frame, Vector2 point)
    {
        return new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, frame.TransformPoint(new Vector3(point.x-960,540-point.y,0))) };
    }

    private static IEnumerator ReviewImmersive()
    {
        PlayerPrefs.SetString("ShiXu.Workshop.v1", "{\"items\":4095,\"chapters\":7,\"solarTerms\":15,\"dyedColor\":\"紫色\",\"seasonalOrder\":true,\"steps\":[\"quest.guide.Spring\",\"quest.guide.Summer\",\"quest.guide.Autumn\",\"quest.guide.Winter\",\"autumn.loom\",\"winter.hoe\",\"winter.rod\",\"autumn.mortar\"]}");
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(.7f);
        var eggSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/关卡/1-春/道具/1-蚕种清洗后.png");
        bool washed = false;
        SpringSilkwormWashOverlay.Show(null,null,eggSprite,ok=>washed=ok);
        yield return null;
        var wash = UnityEngine.Object.FindObjectOfType<SpringSilkwormWashOverlay>();
        var washFrame = (RectTransform)wash.transform;
        wash.BeginDrag(TablePointer(washFrame,new Vector2(1590,800)));
        wash.Drag(TablePointer(washFrame,new Vector2(510,550)));
        wash.Drag(TablePointer(washFrame,new Vector2(1350,550)));
        RequireImmersive(wash.CleanFraction == 0 && !wash.IsComplete,"Dry cloth cleaned seeds");
        wash.EndDrag(TablePointer(washFrame,new Vector2(1350,550)));
        yield return Capture("50-spring-wash-dirty");
        wash.BeginDrag(TablePointer(washFrame,new Vector2(1590,800)));
        wash.Drag(TablePointer(washFrame,new Vector2(240,760)));
        for (int n=0;n<5;n++) wash.Drag(TablePointer(washFrame,new Vector2(n%2==0?540:830,555)));
        RequireImmersive(wash.CleanFraction>.04f && wash.CleanFraction<.85f && !wash.IsComplete,
            "Cleaning must be local with untouched seeds still dirty");
        wash.EndDrag(TablePointer(washFrame,new Vector2(830,555)));
        yield return Capture("51-spring-wash-partly-clean");
        wash.BeginDrag(TablePointer(washFrame,new Vector2(1590,800)));
        for (int pass=0;pass<8 && !wash.IsComplete;pass++)
        {
            wash.Drag(TablePointer(washFrame,new Vector2(240,760)));
            for(int row=0;row<7;row++)
                for(int column=0;column<15;column++)
                    wash.Drag(TablePointer(washFrame,new Vector2(460+(row%2==0?column:14-column)*70,430+row*65)));
        }
        RequireImmersive(wash.IsComplete && wash.CleanFraction>.99f,"Wet cloth could not fully clean seeds");
        yield return Capture("52-spring-wash-clean");
        InvokeNamedButton("Collect Clean Eggs");
        yield return null;
        RequireImmersive(washed && !SpringSilkwormWashOverlay.IsOpen && Time.timeScale==1,"Wash return did not restore world");
        SpringSilkwormWashOverlay.Show(null,null,eggSprite,ok=>washed=ok);
        InvokeNamedButton("Leave Bath");
        yield return null;
        RequireImmersive(!washed && Time.timeScale==1,"Cancelled washing granted completion");
        WorkshopSimpleGames.Show(WorkshopSimpleGame.Feeding);
        yield return Capture("53-spring-feeding-immersive");
        CloseWorktable();
        yield return null;
        HarvestOverlayManager.Instance.ShowHarvest();
        yield return Capture("54-spring-harvest-immersive");
        HarvestOverlayManager.Instance.HideHarvest();

        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return new WaitForSecondsRealtime(.6f);
        WorkshopSimpleGames.Show(WorkshopSimpleGame.Reeling);
        yield return null;
        var reel=UnityEngine.Object.FindObjectOfType<WorkshopReelingWork>();
        RequireImmersive(reel!=null && reel.GetComponentsInChildren<Image>().Length>0,"Missing reeling workbench");
        foreach(string part in new[]{"Reeling Machine Support","Reeling Machine Wheel"})
            RequireImmersive(reel.transform.Find(part).GetComponent<Image>().sprite!=null,"Machine sprite not imported: "+part);
        yield return Capture("55-summer-reeling-immersive");
        var rail=(RectTransform)reel.transform.Find("Guide Rail");
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        Action<int> place = band =>
        {
            var wrap = (RectTransform)reel.transform.Find("Silk Band " + band);
            var position = rail.InverseTransformPoint(wrap.TransformPoint(wrap.rect.center));
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, rail.TransformPoint(new Vector3(position.x, -38f, 0f)));
        };
        place(0);reel.OnBeginDrag(pointer);
        float initialAngle=reel.WheelAngle;
        yield return new WaitForSecondsRealtime(2.5f);
        RequireImmersive(reel.Progress>0 && reel.Progress<.1f && !reel.Complete,"Stationary guide completed more than one band");
        RequireImmersive(Mathf.Abs(Mathf.DeltaAngle(initialAngle,reel.WheelAngle))>.5f,"Machine wheel did not turn");
        reel.OnEndDrag(pointer);
        yield return Capture("56-summer-reeling-one-band");
        CloseWorktable();yield return null;
        RequireImmersive(!WorkshopProgress.HasStep(WorkshopWordRules.Reeled) && Time.timeScale==1,"Cancelled winding granted completion");
        WorkshopSimpleGames.Show(WorkshopSimpleGame.Reeling);yield return null;
        reel=UnityEngine.Object.FindObjectOfType<WorkshopReelingWork>();rail=(RectTransform)reel.transform.Find("Guide Rail");
        place(11);reel.OnBeginDrag(pointer);
        int[] windingOrder = { 11, 9, 7, 5, 3, 1, 10, 8, 4, 2, 0 };
        foreach (int band in windingOrder)
        {
            place(band);reel.OnDrag(pointer);
            yield return new WaitForSecondsRealtime(1.9f);
            if(band==1)yield return Capture("57-summer-reeling-half-full");
        }
        RequireImmersive(!reel.Complete && Mathf.Approximately(reel.Progress, 11f / 12f), "Skipped winding band should still need filling");
        var marker = (RectTransform)rail.Find("Next Winding Band");
        place(6);
        RequireImmersive(marker.gameObject.activeInHierarchy &&
            Mathf.Abs(RectTransformUtility.WorldToScreenPoint(null, marker.TransformPoint(marker.rect.center)).x - pointer.position.x) < 1f,
            "Winding marker did not point to the skipped band");
        reel.OnDrag(pointer);
        yield return new WaitForSecondsRealtime(1.9f);
        reel.OnEndDrag(pointer);
        RequireImmersive(reel.Complete && WorkshopProgress.HasStep(WorkshopWordRules.Reeled),"Out-of-order winding did not complete and save");
        yield return Capture("58-summer-reeling-complete");
        CloseWorktable();yield return null;
        MiniGameOverlay.Show("CocoonMatch");yield return new WaitForSecondsRealtime(1.3f);
        yield return Capture("59-summer-cocoons-immersive");
        MiniGameOverlay.Close();yield return new WaitForSecondsRealtime(.7f);
        MiniGameOverlay.ShowMap("Maze_Autumn");yield return new WaitForSecondsRealtime(1.3f);
        var maze=UnityEngine.Object.FindObjectOfType<JiangnanDye.MazeGame.MazeGameManager>();
        RequireImmersive(maze!=null && maze.state==JiangnanDye.MazeGame.MazeState.Playing && !maze.instructionPanel.activeInHierarchy,"Maze still waits on intro card");
        yield return Capture("60-summer-maze-direct");
        MiniGameOverlay.Close();yield return new WaitForSecondsRealtime(.7f);

        yield return SceneManager.LoadSceneAsync("Level3_Autumn");yield return new WaitForSecondsRealtime(.7f);
        MiniGameOverlay.Show("LoomRepair");yield return new WaitForSecondsRealtime(1.1f);
        yield return Capture("61-autumn-repair-immersive");
        MiniGameOverlay.Close();yield return new WaitForSecondsRealtime(.6f);
        EmbroideryGameManager.SetNextMode(EmbroideryPlayMode.Weaving);
        MiniGameOverlay.Show("EmbroideryScene");yield return new WaitForSecondsRealtime(1.1f);
        RequireImmersive(GameObject.Find("Weaving Instructions")==null,"Weaving instruction card remains");
        yield return Capture("62-autumn-weaving-immersive");
        MiniGameOverlay.Close();yield return new WaitForSecondsRealtime(.7f);
        WorkshopProgress.RecordRawDyes(new[]{WorkshopArt.RawDye(DyeColor.Purple,true)});
        WinterLevelState.RestoreWorkshopProgress();
        WinterGrindingOverlay.Show();yield return null;
        yield return Capture("63-autumn-grinding-immersive");
        WinterGrindingOverlay.Close();yield return null;
        WorkshopProgress.RecordGroundDye(DyeColor.Purple);WinterLevelState.RestoreWorkshopProgress();
        WinterDyeingOverlay.Show();yield return null;
        yield return Capture("64-autumn-single-cloth-dye");
        WinterDyeingOverlay.CloseAny();yield return null;
        MiniGameOverlay.ShowMap("MainScene");yield return new WaitForSecondsRealtime(1.4f);
        var garden=UnityEngine.Object.FindObjectOfType<JiangnanDye.GameManager>();
        RequireImmersive(garden!=null && garden.hudRoot.activeInHierarchy && !garden.introUI.gameObject.activeInHierarchy,"Garden still waits on intro card");
        yield return Capture("65-autumn-garden-direct");
        MiniGameOverlay.Close();yield return new WaitForSecondsRealtime(.7f);

        yield return SceneManager.LoadSceneAsync("Level4_Winter");yield return new WaitForSecondsRealtime(.7f);
        MiniGameOverlay.Show("ClothCutting");yield return new WaitForSecondsRealtime(1.2f);
        yield return Capture("66-winter-cutting-immersive");
        MiniGameOverlay.Close();yield return new WaitForSecondsRealtime(.7f);
        WorkshopSimpleGames.Show(WorkshopSimpleGame.Fitting);
        yield return Capture("67-winter-fitting-immersive");CloseWorktable();yield return null;
        WorkshopProgress.MarkStep(WorkshopWordRules.Fitted);
        WorkshopTailor.Show(null,null);yield return Capture("68-winter-style-immersive");
        InvokeNamedButton("ConfirmStyle");yield return Capture("69-winter-sewing-immersive");
        Debug.Log("[ImmersiveReview] PASS: dry/wet/local/complete/cancel wash; stationary/even/cancel/save reeling; sprites and turning wheel; direct maze and garden; all active workshop interfaces rendered.");
        File.WriteAllText("../WordRevision/immersive-review-complete.txt","Actual Unity Editor 1920x1080 frames; isolated callback fixtures, not a complete physical-input playthrough. Original player save restored on exit. No player build or remote upload.\n");
        UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
