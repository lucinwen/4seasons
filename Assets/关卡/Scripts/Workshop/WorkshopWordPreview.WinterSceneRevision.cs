#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Qingming.LevelBackground;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class WorkshopWordPreview
{
    /// <summary>Standalone entry; the existing Editor review host restores the user's save.</summary>
    public static void BeginWinterSceneRevision()
    {
        var host = new GameObject("Winter Courtyard Physical Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(WinterSceneRevisionAndExit()));
    }

    private static IEnumerator WinterSceneRevisionAndExit()
    {
        yield return ReviewWinterSceneRevision();
        File.WriteAllText("../WordRevision/winter-scene-review-complete.txt",
            "Actual dynamic Rigidbody2D walking through the revised winter courtyard, with native world actions and screenshots. No test teleport, disabled simulation or injected jump velocity.\n");
        EditorApplication.isPlaying = false;
    }

    /// <summary>Uses the authored spawn and ordinary walking throughout; no physics/body/position bypass.</summary>
    internal static IEnumerator ReviewWinterSceneRevision()
    {
        Feedback2Save("{\"items\":2047,\"chapters\":7,\"solarTerms\":15,\"dyedColor\":\"紫色\",\"steps\":[\"quest.guide.Spring\",\"quest.guide.Summer\",\"quest.guide.Autumn\",\"quest.guide.Winter\",\"dialogue.winter.tailoring\",\"spring.guyu.fed\",\"summer.xiaoman.reeled\",\"autumn.loom\",\"autumn.bailu.woven\",\"winter.dyes\",\"winter.ground\",\"autumn.hanlu.patterned\"]}");
        WinterLevelState.HasReturnPosition = false;
        WinterLevelState.PendingGrindingPrompt = false;
        yield return SceneManager.LoadSceneAsync("Level4_Winter");
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(.7f);

        var player = Feedback2Player();
        var body = player.GetComponent<Rigidbody2D>();
        WinterSceneRequire(body != null && body.simulated && body.bodyType == RigidbodyType2D.Dynamic,
            "Winter review requires the live dynamic player.");
        WinterSceneRequire(Mathf.Abs(player.transform.position.x - player.spawnPoint.x) < .25f,
            "Winter must begin at its authored spawn, rather than a saved return teleport.");
        WinterSceneRequire(!WorkshopQuestGuide.BlocksWorldInput && !WorkshopQuestGuide.IsWorldOverlayActive,
            "An overlay is blocking the winter walk.");
        player.clickToMoveEnabled = true;
        var backdrop = GameObject.Find("Background")?.GetComponent<SpriteRenderer>();
        WinterSceneBackground(backdrop);
        WinterSceneTerrain(backdrop);
        var npc = GameObject.Find("idle_blink_frame_01");
        WinterSceneRequire(npc != null && npc.GetComponent<NpcTalkButton>() != null &&
            npc.GetComponent<WinterNpcDialogue>() != null,
            "The winter guide lost its dialogue components.");
        WinterSceneRequire(Mathf.Abs(npc.transform.position.x - player.spawnPoint.x) <= 4.3f,
            "The winter guide is not a short walk from spawn.");
        WinterSceneSupported(npc.GetComponent<SpriteRenderer>());
        foreach (var collider in npc.GetComponentsInChildren<Collider2D>())
            WinterSceneRequire(!collider.enabled || collider.isTrigger, "The guide blocks the main route.");
        yield return Capture("940-winter-revision-spawn");

        yield return WinterSceneWalk(player, npc.transform.position.x - 1.2f, "guide");
        FloatingActionButton talk = null;
        foreach (var action in npc.GetComponents<FloatingActionButton>())
            if (action.IsVisible && action.buttonText != "读节气卡" && action.buttonText != "领节气卡") talk = action;
        WinterSceneRequire(talk != null, "Walking from spawn did not reveal the guide's action.");
        WinterSceneClickTag(talk);
        yield return null;
        WinterSceneRequire(WorkshopDialoguePresenter.IsOpen, "The relocated guide's visible talk action did not open dialogue.");
        yield return Capture("941-winter-revision-guide-dialogue");
        for (int line = 0; line < 16 && WorkshopDialoguePresenter.IsOpen; line++)
        {
            InvokeNamedButton("Advance Dialogue");
            yield return null;
        }
        WinterSceneRequire(!WorkshopDialoguePresenter.IsOpen, "The guide dialogue could not be dismissed.");
        yield return null;

        var table = GameObject.Find("4-裁剪台");
        WinterSceneRequire(table != null && table.GetComponent<WinterClothCuttingTrigger>() != null,
            "The central workshop lost its tailoring entry.");
        yield return WinterSceneWalk(player, table.transform.position.x, "central-workshop");
        yield return Capture("942-winter-revision-central-workshop");
        WinterSceneNeedleTarget(table.transform.position.x);
        var cut = table.GetComponent<FloatingActionButton>();
        WinterSceneRequire(cut != null && cut.IsVisible, "The relocated cutting table has no reachable action.");
        WinterSceneClickTag(cut);
        yield return null;
        WinterSceneRequire(WorkshopSimpleGames.IsOpen && WorkshopWordRules.WinterStage == WorkshopWinterStage.Fitting,
            "The cutting table did not open the saved fitting stage.");
        yield return Capture("943-winter-revision-tailoring-entry");
        CloseWorktable();
        yield return null;
        WinterSceneRequire(!WorkshopSimpleGames.IsOpen && !WorkshopRewardCards.IsBusy,
            "Leaving the fitting worktable must resume the courtyard without a reward.");
        yield return null;

        yield return WinterSceneWalk(player, 1.5f, "east-workshop-porch");
        yield return Capture("944-winter-revision-east-porch");
        yield return WinterSceneWalk(player, 9.0f, "stone-crossing");
        yield return Capture("945-winter-revision-stone-crossing");
        var storageEntrance = GameObject.Find("Winter Storage Entrance");
        WinterSceneRequire(storageEntrance != null && storageEntrance.GetComponent<WinterStorageEntrance>() != null,
            "The right-hand storehouse lost its native entry.");
        yield return WinterSceneWalk(player, storageEntrance.transform.position.x, "right-storage");
        yield return Capture("946-winter-revision-right-storage");
        var storageAction = storageEntrance.GetComponent<FloatingActionButton>();
        WinterSceneRequire(storageAction != null && storageAction.IsVisible,
            "Walking to the right-hand storehouse did not reveal its material-room action.");
        Vector2 storagePosition = body.position;
        WinterSceneClickTag(storageAction);
        yield return null;
        WinterSceneRequire(WorkshopStorageView.IsOpen && SceneManager.GetActiveScene().name == "Level4_Winter",
            "The storehouse action did not open the shared material room above the winter courtyard.");
        yield return Capture("947-winter-revision-storage-open");
        var storage = UnityEngine.Object.FindObjectOfType<WorkshopStorageView>();
        WinterSceneRequire(storage != null, "The winter material room has no live view.");
        Button storageClose = null;
        foreach (var button in storage.GetComponentsInChildren<Button>())
            if (button.name == "Storage Close" ||
                (button.name == "Action" && button.transform.parent.name == "Storage Close"))
            { storageClose = button; break; }
        WinterSceneClickButton(storageClose, "Storage Close");
        yield return null;
        WinterSceneRequire(!WorkshopStorageView.IsOpen && SceneManager.GetActiveScene().name == "Level4_Winter" &&
            Feedback2Player() == player && Vector2.Distance(body.position, storagePosition) < .3f,
            "Closing winter storage must return to the same player at the reached storehouse.");
        WinterSceneRequire(body.simulated && body.bodyType == RigidbodyType2D.Dynamic &&
            !WorkshopQuestGuide.BlocksWorldInput && !WorkshopQuestGuide.IsWorldOverlayActive,
            "The winter route did not end in a live, controllable courtyard.");
        Debug.Log("[WinterSceneRevision] PASS: full cropped background without encoded black bands; grounded nearby guide; artwork-aligned main route; real dynamic walking from spawn through the main workshop and stone crossing to the right storage; guide, tailoring and storehouse actions; storage opens and returns to the same courtyard/player; reachable needle hitbox. No assigned player positions, disabled simulation or injected jump velocity.");
    }

    private static void WinterSceneBackground(SpriteRenderer renderer)
    {
        WinterSceneRequire(renderer != null && renderer.sprite != null, "Winter background is missing.");
        var sprite = renderer.sprite;
        WinterSceneRequire(Mathf.Abs(renderer.bounds.min.x + 20f) < .05f &&
            Mathf.Abs(renderer.bounds.max.x - 20f) < .05f,
            "The revised winter background must cover the complete forty-unit courtyard.");
        WinterSceneRequire(Mathf.Abs(sprite.rect.x) < .01f && Mathf.Abs(sprite.rect.y) < .01f &&
            Mathf.Abs(sprite.rect.width - sprite.texture.width) < 1f &&
            Mathf.Abs(sprite.rect.height - sprite.texture.height) < 1f,
            "The winter sprite metadata crops or offsets the imported image.");
        string path = AssetDatabase.GetAssetPath(sprite);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        WinterSceneRequire(path == "Assets/Resources/WorkshopWinterRevision/WinterCourtyard.png" &&
            AssetDatabase.AssetPathToGUID(path) == "27e7106b9ccf43e39f81fc8012148818" &&
            importer != null && importer.spriteImportMode == SpriteImportMode.Single &&
            importer.maxTextureSize == 4096 && importer.textureCompression == TextureImporterCompression.Uncompressed,
            "Winter background must import the complete revised bitmap without downscaling or compression.");
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        WinterSceneRequire(settings.spriteMeshType == SpriteMeshType.FullRect &&
            Mathf.Abs(renderer.bounds.min.y + 5.353299f) < .02f &&
            Mathf.Abs(renderer.bounds.max.y - 5.353299f) < .02f,
            "Winter background mesh or vertical bounds crop the revised courtyard.");
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            WinterSceneRequire(source.LoadImage(File.ReadAllBytes(path)), "Could not inspect the actual winter bitmap.");
            WinterSceneRequire(source.width == 2137 && source.height == 572,
                "The winter replacement should preserve its full width and remove the source screenshot's black bands.");
            foreach (int row in new[] { 1, source.height - 2 })
            {
                int dark = 0, samples = 0;
                for (int column = 0; column < source.width; column += 16)
                {
                    Color pixel = source.GetPixel(column, row);
                    if (Mathf.Max(pixel.r, pixel.g, pixel.b) < .045f) dark++;
                    samples++;
                }
                WinterSceneRequire(dark < samples * .1f,
                    "The actual winter bitmap retains a black top/bottom border: row=" + row);
            }
        }
        finally { UnityEngine.Object.Destroy(source); }
        var camera = Camera.main;
        WinterSceneRequire(camera != null && camera.orthographic &&
            camera.transform.position.y + camera.orthographicSize <= renderer.bounds.max.y + .02f &&
            camera.transform.position.y - camera.orthographicSize >= renderer.bounds.min.y - .02f,
            "The winter camera exposes a horizontal band outside the background.");
    }

    private static void WinterSceneTerrain(SpriteRenderer background)
    {
        // Image-space surfaces from WordRevision/WinterRevision/final-map.json.
        var points = new[]
        {
            new Vector2(-15f, -3.855873f), new Vector2(-10f, -3.855873f),
            new Vector2(-2f, -4.043051f), new Vector2(1.5f, -4.043051f),
            new Vector2(2.4f, -3.743566f), new Vector2(3.4f, -3.518952f),
            new Vector2(5.25f, -3.500234f), new Vector2(7.1f, -3.462798f),
            new Vector2(10f, -4.080487f), new Vector2(15f, -4.080487f)
        };
        foreach (var point in points)
        {
            RaycastHit2D support = default;
            foreach (var hit in Physics2D.RaycastAll(point + Vector2.up * .8f, Vector2.down, 1.6f))
            {
                var collider = hit.collider;
                if (collider == null || collider.isTrigger || collider.attachedRigidbody != null ||
                    collider.gameObject.scene != background.gameObject.scene || collider.GetComponent<Renderer>() != null ||
                    hit.normal.y < .45f) continue;
                support = hit; break;
            }
            WinterSceneRequire(support.collider != null && Mathf.Abs(support.point.y - point.y) <= .12f,
                "Winter ground/stone top does not match its drawn surface near " + point + " / actual " + support.point);
        }
    }

    private static void WinterSceneSupported(SpriteRenderer sprite)
    {
        WinterSceneRequire(sprite != null, "Winter guide has no visible sprite.");
        var feet = new Vector2(sprite.bounds.center.x, sprite.bounds.min.y);
        bool grounded = false;
        foreach (var hit in Physics2D.RaycastAll(feet + Vector2.up * .25f, Vector2.down, .55f))
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.attachedRigidbody == null &&
                hit.collider.GetComponent<Renderer>() == null && hit.normal.y > .45f &&
                Mathf.Abs(hit.point.y - feet.y) <= .25f) grounded = true;
        WinterSceneRequire(grounded, "The nearby winter guide is floating above or inside the courtyard.");
    }

    private static IEnumerator WinterSceneWalk(LevelPlayerController player, float target, string phase)
    {
        var body = player.GetComponent<Rigidbody2D>();
        var shape = player.GetComponentInChildren<Collider2D>();
        WinterSceneRequire(shape != null, "Winter player has no collider.");
        float began = Time.realtimeSinceStartup, lastProgress = began;
        float lastX = body.position.x, furthest = lastX, direction = Mathf.Sign(target - lastX);
        float minFeet = shape.bounds.min.y;
        player.SetWalkTarget(target);
        while (Mathf.Abs(body.position.x - target) > .2f)
        {
            yield return new WaitForFixedUpdate();
            float now = Time.realtimeSinceStartup, x = body.position.x;
            WinterSceneRequire(body.simulated && body.bodyType == RigidbodyType2D.Dynamic,
                "Winter movement disabled dynamic physics: " + phase);
            WinterSceneRequire(Mathf.Abs(x - lastX) < player.moveSpeed * Time.fixedDeltaTime * 2f + .2f,
                "Winter movement jumped horizontally instead of walking: " + phase);
            minFeet = Mathf.Min(minFeet, shape.bounds.min.y);
            WinterSceneRequire(player.transform.position.y > player.fallResetY + 1f && minFeet > -4.65f,
                "Winter movement fell under the drawn main route: " + phase + " at " + body.position);
            if ((x - furthest) * direction > .04f) { furthest = x; lastProgress = now; }
            WinterSceneRequire(now - lastProgress < 1.5f && now - began < 10f,
                "Winter walk is blocked at " + body.position + " during " + phase + "; target=" + target +
                "; feet=" + shape.bounds.min.y + "; " + player.StepUpDiagnostic);
            lastX = x;
        }
        yield return new WaitForSecondsRealtime(.2f);
        Debug.Log("[WinterSceneRevision] walked " + phase + " to " + body.position + "; minFeet=" + minFeet);
    }

    private static void WinterSceneNeedleTarget(float tableX)
    {
        var needle = GameObject.Find("4-裁衣工具针");
        var sprite = needle != null ? needle.GetComponent<SpriteRenderer>() : null;
        var collider = needle != null ? needle.GetComponent<Collider2D>() : null;
        WinterSceneRequire(needle != null && needle.GetComponent<WinterNeedleClickTarget>() != null &&
            sprite != null && sprite.enabled && sprite.sprite != null && collider != null && collider.enabled &&
            collider.isTrigger && collider.OverlapPoint(sprite.bounds.center),
            "The sewing needle lost its visible, clickable pickup target.");
        WinterSceneRequire(Mathf.Abs(sprite.bounds.center.x - tableX) < 3f,
            "The needle is no longer beside the central tailoring worktable.");
        var screen = Camera.main.WorldToViewportPoint(sprite.bounds.center);
        WinterSceneRequire(screen.z > 0 && screen.x > .03f && screen.x < .97f && screen.y > .03f && screen.y < .97f,
            "The needle cannot be seen/clicked from the reached central workshop.");
    }

    private static void WinterSceneClickTag(FloatingActionButton action)
    {
        Button target = null;
        foreach (var button in UnityEngine.Object.FindObjectsOfType<Button>())
        {
            if (button.name != "Action Tag") continue;
            foreach (var text in button.GetComponentsInChildren<Text>())
                if (text.name == "Action Label" && text.text == action.buttonText) { target = button; break; }
        }
        WinterSceneClickButton(target, action.buttonText);
    }

    private static void WinterSceneClickButton(Button target, string label)
    {
        WinterSceneRequire(target != null && target.IsInteractable(), "Missing visible action: " + label);
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)target.transform;
        var pointer = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        WinterSceneRequire(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == target,
            "The visible action is obscured: " + label);
        ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static void WinterSceneRequire(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("[WinterSceneRevision] " + message);
    }
}
#endif
