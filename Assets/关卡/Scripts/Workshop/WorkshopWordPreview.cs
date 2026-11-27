#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Qingming.LevelBackground;
using Qingming.Harvest;

/// <summary>Uses the actual runtime UI and scenes for local screenshot review, with a temporary save.</summary>
public sealed partial class WorkshopWordPreview : MonoBehaviour
{
    public static void BeginSummer()
    {
        var host = new GameObject("Summer Station Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(CaptureSummer()));
    }

    private static IEnumerator CaptureSummer()
    {
        PlayerPrefs.SetString("ShiXu.Workshop.v1", "{\"items\":8,\"chapters\":1,\"seasonalOrder\":true,\"steps\":[\"quest.guide.Summer\",\"summer.water\",\"summer.wood\"]}");
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return new WaitForSecondsRealtime(0.6f);
        var rack = GameObject.Find(SummerItemInteraction.CocoonRackObjectName);
        var wheel = GameObject.Find(SummerItemInteraction.WindingWheelObjectName);
        if (rack == null || wheel == null || wheel.GetComponent<SummerWindingTrigger>() == null)
            throw new System.InvalidOperationException("Summer wheel interaction was not installed.");
        MoveSummerPlayer(rack.transform.position.x);
        yield return new WaitForSecondsRealtime(0.8f);
        var rackAction = rack.GetComponent<FloatingActionButton>();
        if (rackAction == null || !rackAction.IsVisible || rackAction.buttonText != "开始择茧")
            throw new System.InvalidOperationException("First cocoon visit must offer the original start action.");
        WorkshopProgress.Award(WorkshopItem.Suoxu);
        yield return null;
        if (rackAction.buttonText != "再玩一次")
            throw new System.InvalidOperationException("Cocoon completion did not refresh the replay label.");
        yield return Capture("29-summer-cocoon-replay");
        rackAction.onClick.Invoke();
        float deadline = Time.realtimeSinceStartup + 10f;
        while (!SceneManager.GetSceneByName("CocoonMatch").isLoaded && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (!MiniGameOverlay.IsActive || !SceneManager.GetSceneByName("CocoonMatch").isLoaded)
            throw new System.InvalidOperationException("Cocoon replay did not open the original game.");
        yield return new WaitForSecondsRealtime(0.4f);
        MiniGameOverlay.Close();
        deadline = Time.realtimeSinceStartup + 10f;
        while (MiniGameOverlay.IsActive && Time.realtimeSinceStartup < deadline) yield return null;
        if (MiniGameOverlay.IsActive || !WorkshopProgress.Has(WorkshopItem.Suoxu))
            throw new System.InvalidOperationException("Cocoon replay lost progress or failed to return.");

        MoveSummerPlayer(wheel.transform.position.x);
        yield return new WaitForSecondsRealtime(0.8f);
        var wheelAction = wheel.GetComponent<FloatingActionButton>();
        if (wheelAction == null || !wheelAction.IsVisible || wheelAction.buttonText != "绕丝上轴")
            throw new System.InvalidOperationException("Touching the wheel did not show the winding action.");
        wheelAction.onClick.Invoke();
        yield return null;
        if (WorkshopObservationView.IsOpen || !SimplePopup.IsOpen)
            throw new System.InvalidOperationException("Winding must explain the missing spool.");
        Object.FindObjectOfType<SimplePopup>().OnConfirm();
        yield return null;
        WorkshopProgress.Award(WorkshopItem.Spool);
        yield return new WaitForSecondsRealtime(0.3f);
        if (WorkshopProgress.HasStep(WorkshopWordRules.Reeled))
            throw new System.InvalidOperationException("Summer review unexpectedly completed tension reeling.");
        yield return Capture("30-summer-wheel-winding-action");
        wheelAction.onClick.Invoke();
        yield return null;
        if (WorkshopObservationView.ActiveGame != WorkshopObservation.MangzhongCocoon)
            throw new System.InvalidOperationException("The wheel must open the silk connection puzzle immediately after finding the spool.");
        VerifyXiazhiScene(0, false);
        // Leaving during the short success animation must not grant a three-round completion.
        yield return SolveXiazhiReviewBoard(0);
        Object.FindObjectOfType<WorkshopObservationView>().ExitXiazhiReview();
        yield return new WaitForSecondsRealtime(1f);
        if (WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon) ||
            WorkshopObservationView.HasOrderWork(WorkshopObservation.MangzhongCocoon) || Time.timeScale != 1f)
            throw new System.InvalidOperationException("An interrupted round granted completion or left the world paused.");
        wheelAction.onClick.Invoke();
        yield return null;
        VerifyXiazhiScene(0, false);
        yield return Capture("33-silk-summer-overlay-round-1");
        int[] sceneShots = { 33, 37, 38 };
        int previousPopulated = 0;
        for (int level = 0; level < XiazhiPuzzle.LevelCount; level++)
        {
            var view = Object.FindObjectOfType<WorkshopObservationView>();
            if (view.XiazhiReviewLevel != level || view.XiazhiReviewProgress != level)
                throw new System.InvalidOperationException("Silk rounds did not automatically advance in increasing order.");
            VerifyXiazhiScene(level, false);
            var model = new XiazhiPuzzle(level);
            int populated = 0;
            for (int cell = 0; cell < XiazhiPuzzle.CellCount; cell++) if (model.Mask(cell) != 0) populated++;
            if (populated <= previousPopulated) throw new System.InvalidOperationException("Silk boards did not increase in complexity.");
            previousPopulated = populated;
            if (level > 0) yield return Capture(sceneShots[level] + "-silk-summer-overlay-round-" + (level+1));
            yield return SolveXiazhiReviewBoard(level);
            if (!view.XiazhiReviewAdvancing || view.XiazhiReviewProgress != level+1 || Time.timeScale != 0f)
                throw new System.InvalidOperationException("Connected silk did not enter its unscaled success transition.");
            if (view.XiazhiReviewCanReturn || view.transform.Find("Xiazhi Immersive Scene/Xiazhi Return To Summer") != null)
                throw new System.InvalidOperationException("Return must remain unavailable during every round's wheel feedback, including the third.");
            if (WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon))
                throw new System.InvalidOperationException("Silk completion was saved before all three rounds and feedback finished.");
            if (level == 0) yield return CaptureXiazhiWheelSpin();
            deadline = Time.realtimeSinceStartup+4f;
            while (view.XiazhiReviewAdvancing && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            if (view.XiazhiReviewAdvancing) throw new System.TimeoutException("Silk round advance stalled while the world was paused.");
            if (level < XiazhiPuzzle.LevelCount-1 && WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon))
                throw new System.InvalidOperationException("Silk activity completed before the third round.");
        }
        if (!WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon) || WorkshopProgress.CanSubmit(WorkshopSeason.Summer))
            throw new System.InvalidOperationException("The three-round result must preserve the separate tension reeling requirement.");
        VerifyXiazhiScene(3, true);
        yield return Capture("39-silk-summer-overlay-success-hint");
        yield return new WaitForSecondsRealtime(1.1f);
        if (!WorkshopObservationView.IsOpen || Time.timeScale != 0f)
            throw new System.InvalidOperationException("Silk completion returned automatically instead of waiting for the player.");
        InvokeWorktableButton("Xiazhi Return To Summer");
        yield return new WaitForSecondsRealtime(0.3f);
        if (!wheelAction.IsVisible || !WorkshopProgress.Has(WorkshopItem.Spool) || WorkshopProgress.HasStep(WorkshopWordRules.Reeled))
            throw new System.InvalidOperationException("Returning from winding changed inventory, reeling progress or hid the wheel action.");
        wheelAction.onClick.Invoke();
        yield return null;
        if (WorkshopObservationView.ActiveGame != WorkshopObservation.MangzhongCocoon)
            throw new System.InvalidOperationException("The wheel puzzle cannot be replayed after completion.");
        VerifyXiazhiScene(0, false);
        yield return SolveXiazhiReviewBoard(0);
        Object.FindObjectOfType<WorkshopObservationView>().ExitXiazhiReview();
        yield return new WaitForSecondsRealtime(1f);
        if (WorkshopObservationView.IsOpen || Time.timeScale != 1f ||
            !WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon))
            throw new System.InvalidOperationException("Leaving a replay lost the saved result or left a round transition running.");

        var cooker = GameObject.Find(SummerItemInteraction.CookerObjectName);
        MoveSummerPlayer(cooker.transform.position.x);
        yield return new WaitForSecondsRealtime(0.5f);
        cooker.GetComponent<FloatingActionButton>().onClick.Invoke();
        yield return null;
        if (!WorkshopSimpleGames.IsOpen || WorkshopObservationView.IsOpen)
            throw new System.InvalidOperationException("The cooker must retain its separate tension reeling activity.");
        CloseWorktable();
        yield return null;
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return new WaitForSecondsRealtime(0.6f);
        MoveSummerPlayer(GameObject.Find(SummerItemInteraction.CocoonRackObjectName).transform.position.x);
        yield return new WaitForSecondsRealtime(0.5f);
        if (GameObject.Find(SummerItemInteraction.CocoonRackObjectName).GetComponent<FloatingActionButton>().buttonText != "再玩一次" ||
            !WorkshopProgress.Has(WorkshopItem.Spool) || !WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon))
            throw new System.InvalidOperationException("Summer replay state did not survive scene reload.");
        Debug.Log("[WordRevision] SUMMER OVERLAY SILK PASS: no illustration layer or scene-sized shadow; actual summer scene dimmed with a black input-blocking veil; named cocoon, bobbin and wider wheel display with contrasting jade surfaces and soft board shadows; three increasing boards via tile callbacks, no early return, final explicit return click, cancellation, replay, separate cooker and saved inventory.");
        File.WriteAllText("../WordRevision/summer-overlay-silk-review-complete.txt", "Actual Unity 1920x1080 frames. Transparent game overlay over black-dimmed summer scene, no intermediate illustration; names, hint, contrasting surfaces, wider wheel frame, three rounds and final-only return verified; original save restored on exit. Screenshot confirmation required before any player build.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    private static void VerifyXiazhiScene(int completed, bool finished)
    {
        Canvas.ForceUpdateCanvases();
        var view = Object.FindObjectOfType<WorkshopObservationView>();
        if (view == null || view.XiazhiReviewProgress != completed ||
            view.transform.Find("Xiazhi Immersive Scene") == null ||
            view.GetComponentsInChildren<Button>().Length != (finished ? 17 : 16))
            throw new System.InvalidOperationException("The immersive silk scene has incorrect progress or extra controls.");
        if (view.transform.Find("Observation Panel") != null)
            throw new System.InvalidOperationException("The legacy paper frame is still present.");
        var threads = view.GetComponentsInChildren<XiazhiThreadGraphic>();
        if (threads.Length != XiazhiPuzzle.CellCount)
            throw new System.InvalidOperationException("The silk board must render all sixteen cells.");
        foreach (var thread in threads)
            if (thread.GetComponent<CanvasRenderer>() == null || thread.canvasRenderer.cull)
                throw new System.InvalidOperationException("A silk segment is missing its renderer or is culled.");
        var scene = (RectTransform)view.transform.Find("Xiazhi Immersive Scene");
        if (scene.Find("Xiazhi Scene Shadow") != null || scene.Find("Xiazhi Workshop Background") != null ||
            scene.Find("Xiazhi Silk Board/Board Shadow/Feather") == null || scene.GetComponent<Image>() != null)
            throw new System.InvalidOperationException("The game must show only the softly shadowed board and objects over the real scene, without an intermediate background.");
        var veil = view.GetComponentInParent<Canvas>().transform.Find("Backdrop").GetComponent<Image>();
        if (SceneManager.GetActiveScene().name != "Level2_Summer" || Time.timeScale != 0f ||
            !veil.raycastTarget || veil.color != (Color)new Color32(0,0,0,168))
            throw new System.InvalidOperationException("The real summer scene must remain paused and darkened by a black veil that blocks world input.");
        var wheel = scene.Find("Xiazhi Reeling Wheel").GetComponent<Image>();
        if (wheel.sprite == null || wheel.rectTransform.anchoredPosition.x < scene.rect.width*.8f)
            throw new System.InvalidOperationException("The lower-right wheel is incorrect.");
        var wheelFrame = scene.Find("Xiazhi Reeling Wheel Frame").GetComponent<RectTransform>();
        if (wheelFrame.rect.size != new Vector2(288,288) ||
            wheelFrame.GetSiblingIndex() >= wheel.transform.GetSiblingIndex() ||
            wheel.rectTransform.anchoredPosition != wheelFrame.anchoredPosition+new Vector2(144,-148))
            throw new System.InvalidOperationException("The rotating wheel must fit within its wider stationary display frame.");
        VerifyXiazhiLabel(wheelFrame.Find("Xiazhi Wheel Name").GetComponent<Text>(), "缫丝转盘");
        VerifyXiazhiLabel(wheelFrame.Find("Xiazhi Wheel Hint").GetComponent<Text>(), "转动即成功接通");
        VerifyXiazhiDisplaySurface(wheelFrame);
        var model = new XiazhiPuzzle(view.XiazhiReviewLevel);
        VerifyXiazhiEndpoint(scene, "Silk Entrance", model.Start, true);
        VerifyXiazhiEndpoint(scene, "Silk Exit", model.End, false);
        var returnControl = scene.Find("Xiazhi Return To Summer");
        if (view.XiazhiReviewCanReturn != finished || (returnControl != null) != finished ||
            (finished && !returnControl.Find("Action").GetComponent<Button>().interactable))
            throw new System.InvalidOperationException("Return to summer must only appear and respond after all three rounds finish.");
        var hud = scene.Find("Xiazhi Progress");
        if (hud.GetComponent<RectTransform>().anchoredPosition.x > 80 ||
            hud.Find("Progress Count").GetComponent<Text>().text != completed + " / 3")
            throw new System.InvalidOperationException("The upper-left progress display is incorrect.");
        for (int slot = 0; slot < XiazhiPuzzle.LevelCount; slot++)
        {
            var fill = hud.Find("Xiazhi Progress Slot " + slot + "/Fill").GetComponent<Image>();
            bool filled = fill.color.r > .8f;
            if (filled != (slot < completed)) throw new System.InvalidOperationException("Silk progress fill is incorrect.");
        }
        foreach (var button in view.GetComponentsInChildren<Button>())
            if (finished ? button.name.StartsWith("Xiazhi Tile ") && button.interactable : !button.name.StartsWith("Xiazhi Tile "))
                throw new System.InvalidOperationException("A silk scene control is active at the wrong phase.");
    }

    private static void VerifyXiazhiEndpoint(RectTransform scene, string name, int cell, bool entrance)
    {
        var display = scene.Find(name + " Frame").GetComponent<RectTransform>();
        var icon = display.Find(name + " Icon").GetComponent<Image>();
        var lead = scene.Find(name + " Thread").GetComponent<RectTransform>();
        var tile = scene.Find("Xiazhi Silk Board/Xiazhi Tile " + cell).GetComponent<RectTransform>();
        if (display.rect.size != new Vector2(144,144) || icon.rectTransform.rect.size != new Vector2(120,120) ||
            icon.sprite == null || icon.raycastTarget ||
            Mathf.Abs(display.position.y-display.rect.height*.5f*display.lossyScale.y-tile.position.y+tile.rect.height*.5f*tile.lossyScale.y) > .1f ||
            lead.rect.size != new Vector2(38,6) ||
            lead.anchoredPosition.x != display.anchoredPosition.x+(entrance ? 144 : -38))
            throw new System.InvalidOperationException("Silk endpoint must be enlarged in a square wooden frame aligned to its fixed cell: " + name);
        VerifyXiazhiLabel(display.Find(name + " Nameplate/Object Name").GetComponent<Text>(), entrance ? "蚕茧" : "丝轴");
        VerifyXiazhiDisplaySurface(display);
    }

    private static void VerifyXiazhiLabel(Text label, string expected)
    {
        if (label.text != expected || label.raycastTarget ||
            label.preferredWidth > label.rectTransform.rect.width+.1f ||
            label.preferredHeight > label.rectTransform.rect.height+.1f)
            throw new System.InvalidOperationException("Silk object text must be visible, fit its space and allow pointer input through: " + expected);
    }

    private static void VerifyXiazhiDisplaySurface(RectTransform display)
    {
        Color surface = display.Find("Inset Surface").GetComponent<Image>().color;
        if (surface.g <= surface.r || surface.g <= surface.b || surface.r >= .4f)
            throw new System.InvalidOperationException("Silk display needs a contrasting muted jade surface: " + display.name);
    }

    private static IEnumerator SolveXiazhiReviewBoard(int level)
    {
        int[] solution = new XiazhiPuzzle(level).Solve();
        if (solution == null) throw new System.InvalidOperationException("The silk board has no solution: " + level);
        for (int index = 0; index < solution.Length; index++)
            for (int turn = 0; turn < solution[index]; turn++)
            {
                InvokeNamedButton("Xiazhi Tile " + index);
                yield return null;
            }
    }

    private static IEnumerator CaptureXiazhiWheelSpin()
    {
        var wheel = Object.FindObjectOfType<WorkshopObservationView>().transform
            .Find("Xiazhi Immersive Scene/Xiazhi Reeling Wheel").GetComponent<Image>();
        Quaternion previous = wheel.rectTransform.localRotation;
        int changes = 0;
        for (int shot = 0; shot < 3; shot++)
        {
            yield return new WaitForSecondsRealtime(.09f);
            if (Quaternion.Angle(previous,wheel.rectTransform.localRotation) > 2f) changes++;
            previous = wheel.rectTransform.localRotation;
            yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath("../WordRevision/Screenshots/" + (34+shot) + "-silk-summer-overlay-wheel-spin-" + (shot+1) + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[WordRevision] WHEEL FRAME " + shot + " angle=" + wheel.rectTransform.localEulerAngles.z);
        }
        if (changes < 2) throw new System.InvalidOperationException("The silk wheel did not visibly rotate across paused-world frames.");
    }

    private static void MoveSummerPlayer(float x)
    {
        var player = GameObject.Find("Player");
        if (player == null) throw new System.InvalidOperationException("Summer player is missing.");
        player.transform.position = new Vector3(x, -2.05f, player.transform.position.z);
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = new Vector2(x, -2.05f);
            body.velocity = Vector2.zero;
        }
    }

    public static void BeginChapters()
    {
        var host = new GameObject("Seasonal Chapter Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(CaptureChapters()));
    }

    private static IEnumerator CaptureChapters()
    {
        const string completionPath = "../WordRevision/chapter-guide-review-complete.txt";
        File.Delete(completionPath);
        PlayerPrefs.SetString("ShiXu.Workshop.v1", "{\"chapters\":7,\"seasonalOrder\":true,\"steps\":[\"quest.guide.Spring\",\"quest.guide.Summer\",\"quest.guide.Autumn\",\"quest.guide.Winter\"]}");
        WorkshopProgress.ResetSession();
        string[] scenes = { "Level1_Spring", "Level2_Summer", "Level3_Autumn", "Level4_Winter" };
        for (int index = 0; index < scenes.Length; index++)
        {
            var season = (WorkshopSeason)index;
            yield return SceneManager.LoadSceneAsync(scenes[index]);
            yield return new WaitForSecondsRealtime(0.6f);
            bool entered = false, returned = false;
            WorkshopChapterGuide.Show(season, () => entered = true, () => returned = true);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var guide = Object.FindObjectOfType<WorkshopChapterGuide>();
            if (guide == null || guide.GetComponentsInChildren<Button>().Length != 3)
                throw new System.InvalidOperationException("Chapter must retain exactly three buttons: " + season);
            string title = WorkshopChapterGuide.Titles[index];
            var page = guide.transform.Find("Chapter Guide · " + title);
            var art = page?.Find("Seasonal Wood Frame");
            if (art == null || art.GetComponent<Image>().sprite == null)
                throw new System.InvalidOperationException("Missing seasonal chapter frame: " + season);

            var expectedLabels = new (string path, string text)[]
            {
                ("Chapter Title", title),
                ("Chapter Process", null),
                ("Chapter Tips", null),
                ("Chapter Delivery", null),
                ("Chapter Sources/Action/Label", "阅读相关资料"),
                ("Close Chapter Guide/Action/Label", "返回"),
                ("Enter Chapter/Action/Label", "进入关卡")
            };
            if (page.GetComponentsInChildren<Text>().Length != expectedLabels.Length)
                throw new System.InvalidOperationException("Unexpected chapter guide label count: " + season);
            foreach (var expected in expectedLabels)
            {
                var label = page.Find(expected.path)?.GetComponent<Text>();
                if (label == null || !label.isActiveAndEnabled || label.font != WorkshopArt.WorkshopFont || label.color.a <= 0f ||
                    string.IsNullOrWhiteSpace(label.text) || (expected.text != null && label.text != expected.text) ||
                    label.preferredWidth > label.rectTransform.rect.width + 1f ||
                    label.preferredHeight > label.rectTransform.rect.height + 1f)
                    throw new System.InvalidOperationException("Chapter guide label is missing or does not fit: " + season + " / " + expected.path);
            }
            yield return Capture((24 + index).ToString() + "-chapter-" + season.ToString().ToLowerInvariant() + "-guide");

            InvokeWorktableButton("Chapter Sources");
            yield return null;
            if (!WorkshopHeritageBrief.IsOpen || !WorkshopChapterGuide.IsOpen || entered || returned)
                throw new System.InvalidOperationException("Chapter source button lost its callback: " + season);
            InvokeWorktableButton("Heritage Return");
            yield return null;
            InvokeWorktableButton("Close Chapter Guide");
            yield return null;
            if (!returned || entered || WorkshopChapterGuide.IsOpen)
                throw new System.InvalidOperationException("Chapter return callback is incorrect: " + season);
            WorkshopChapterGuide.Show(season, () => entered = true);
            yield return null;
            InvokeWorktableButton("Enter Chapter");
            yield return null;
            if (!entered || WorkshopChapterGuide.IsOpen)
                throw new System.InvalidOperationException("Chapter entry callback is incorrect: " + season);
        }
        Debug.Log("[WordRevision] CHAPTER GUIDE UI PASS: all labels use the workshop pixel font and fit; all three button callbacks passed for every season.");
        File.WriteAllText(completionPath, "Actual Unity frames, 1920x1080. All four guides use the workshop pixel font and fit; sources, return and entry callbacks passed.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    public static void BeginAutumn()
    {
        var host = new GameObject("Autumn Pickup Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(CaptureAutumn()));
    }

    private static IEnumerator RunSafely(IEnumerator routine)
    {
        var stack = new System.Collections.Generic.Stack<IEnumerator>();
        stack.Push(routine);
        while (stack.Count > 0)
        {
            object yielded = null;
            bool moved = false;
            System.Exception error = null;
            try { moved = stack.Peek().MoveNext(); if (moved) yielded = stack.Peek().Current; }
            catch (System.Exception exception) { error = exception; }
            if (error != null)
            {
                Debug.LogException(error);
                UnityEditor.EditorApplication.isPlaying = false;
                yield break;
            }
            if (!moved) { stack.Pop(); continue; }
            if (yielded is IEnumerator nested) stack.Push(nested);
            else yield return yielded;
        }
    }

    private static IEnumerator CaptureAutumn()
    {
        PlayerPrefs.SetString("ShiXu.Workshop.v1", "{\"items\":0,\"chapters\":3,\"solarTerms\":15,\"seasonalOrder\":true,\"steps\":[\"quest.guide.Autumn\"]}");
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level3_Autumn");
        yield return new WaitForSecondsRealtime(0.6f);
        MoveToAutumnLoom();
        yield return new WaitForSecondsRealtime(0.8f);
        if (GameObject.Find("3-修机锤") == null || AutumnLevelState.HasHammer || AutumnLevelState.HasCake)
            throw new System.InvalidOperationException("Autumn pickup initial state is incorrect.");
        yield return Capture("15-autumn-hammer-in-scene");
        Debug.Log("[WordRevision] AUTUMN READY: click the NPC conversation once, then collect the hammer on the ground.");
        float deadline = Time.realtimeSinceStartup + 240f;
        while (!AutumnLevelState.HasCake && Time.realtimeSinceStartup < deadline) yield return null;
        if (!AutumnLevelState.HasCake || AutumnLevelState.HasHammer)
            throw new System.InvalidOperationException("First conversation did not independently grant cake.");
        yield return Capture("16-autumn-first-talk-cake");
        while ((!AutumnLevelState.HasHammer || WorkshopDialoguePresenter.IsOpen || SimplePopup.IsOpen) &&
               Time.realtimeSinceStartup < deadline) yield return null;
        if (!AutumnLevelState.HasHammer || GameObject.Find("3-修机锤") != null)
            throw new System.InvalidOperationException("Scene hammer was not collected and hidden.");
        yield return Capture("17-autumn-hammer-collected");
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level3_Autumn");
        yield return new WaitForSecondsRealtime(0.6f);
        MoveToAutumnLoom();
        yield return new WaitForSecondsRealtime(0.8f);
        if (!AutumnLevelState.HasCake || !AutumnLevelState.HasHammer || GameObject.Find("3-修机锤") != null)
            throw new System.InvalidOperationException("Autumn cake or hammer failed to persist after scene reload.");
        yield return Capture("18-autumn-pickups-reloaded");
        Debug.Log("[WordRevision] AUTUMN UI PASS: first conversation grants only cake; separate world click collects hammer; scene reload preserves both.");
        File.WriteAllText("../WordRevision/autumn-review-complete.txt", "First dialogue, world pickup and scene reload verified in actual 1920x1080 Editor frames.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    private static void MoveToAutumnLoom()
    {
        var player = GameObject.Find("Player");
        if (player == null) throw new System.InvalidOperationException("Autumn player is missing.");
        player.transform.position = new Vector3(16.7f, -2f, player.transform.position.z);
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.velocity = Vector2.zero;
    }

    public static void BeginFeedback()
    {
        var host = new GameObject("Spring Feedback Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(CaptureFeedback()));
    }

    private static IEnumerator CaptureFeedback()
    {
        PlayerPrefs.SetString("ShiXu.Workshop.v1", "{\"items\":3,\"seasonalOrder\":true,\"steps\":[\"dialogue.spring.mulberryIntro\",\"spring.basket\",\"quest.guide.Spring\"]}");
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(0.6f);
        WorkshopSimpleGames.Show(WorkshopSimpleGame.Feeding);
        yield return Capture("08-feeding-before");
        Debug.Log("[WordRevision] FEEDING READY: drag the three leaves into the tray using the game UI.");
        float deadline = Time.realtimeSinceStartup + 240f;
        while (CountFeedingHearts() == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        if (CountFeedingHearts() == 0) throw new System.TimeoutException("No feeding response observed.");
        yield return Capture("09-feeding-hearts-early");
        yield return Capture("10-feeding-hearts-rising");
        yield return Capture("11-feeding-hearts-fading");
        while (!WorkshopProgress.Has(WorkshopItem.Silkworm) && Time.realtimeSinceStartup < deadline) yield return null;
        if (!WorkshopProgress.Has(WorkshopItem.Silkworm)) throw new System.TimeoutException("Feeding did not complete.");
        yield return new WaitForSecondsRealtime(1.8f);
        if (CountFeedingHearts() != 0) throw new System.InvalidOperationException("Feeding hearts did not clean up.");
        yield return Capture("12-feeding-complete");
        CloseWorktable();
        yield return null;
        HarvestOverlayManager.Instance.ShowHarvest();
        yield return new WaitForSecondsRealtime(0.3f);
        VerifyHarvestStarted();
        yield return Capture("13-harvest-direct-entry");
        MulberryHarvestGameManager.Instance.ResetGame();
        yield return null;
        VerifyHarvestStarted();
        HarvestOverlayManager.Instance.HideHarvest();
        yield return null;
        if (Time.timeScale != 1f || GameObject.Find("Player") == null)
            throw new System.InvalidOperationException("Spring world was not restored after harvest.");
        yield return SceneManager.LoadSceneAsync("MulberryHarvest");
        yield return new WaitForSecondsRealtime(0.3f);
        VerifyHarvestStarted();
        yield return Capture("14-harvest-standalone-entry");
        Debug.Log("[WordRevision] FEEDBACK UI PASS: three real leaf drops, unscaled hearts and cleanup; harvest overlay, restart and standalone begin without selection.");
        File.WriteAllText("../WordRevision/feedback-review-complete.txt", "Feeding and direct harvest verified in actual 1920x1080 Editor frames.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    private static int CountFeedingHearts()
    {
        int count = 0;
        foreach (var image in Object.FindObjectsOfType<Image>()) if (image.name == "Feeding Heart") count++;
        return count;
    }

    private static void VerifyHarvestStarted()
    {
        var manager = MulberryHarvestGameManager.Instance;
        if (manager == null || manager.State != HarvestGameState.Playing || manager.CollectedLeaves != 0 ||
            manager.ui == null || (manager.ui.introPanel != null && manager.ui.introPanel.activeInHierarchy))
            throw new System.InvalidOperationException("Harvest is waiting for a selection or confirmation.");
    }

    public static void Begin()
    {
        var host = new GameObject("Word Interface Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(CaptureAll());
    }

    private static IEnumerator CaptureAll()
    {
        PlayerPrefs.SetString("ShiXu.Workshop.v1", "{\"items\":4095,\"chapters\":7,\"solarTerms\":15,\"dyedColor\":\"紫色\",\"seasonalOrder\":true,\"steps\":[\"quest.guide.Spring\",\"quest.guide.Summer\",\"quest.guide.Autumn\",\"quest.guide.Winter\",\"autumn.loom\",\"winter.hoe\",\"winter.rod\",\"autumn.mortar\"]}");
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(0.6f);
        WorkshopSimpleGames.Show(WorkshopSimpleGame.Feeding);
        yield return Capture("01-spring-feeding");
        CloseWorktable();

        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return new WaitForSecondsRealtime(0.6f);
        WorkshopSimpleGames.Show(WorkshopSimpleGame.Reeling);
        yield return Capture("02-summer-reeling");
        CloseWorktable();

        yield return SceneManager.LoadSceneAsync("Level3_Autumn");
        yield return new WaitForSecondsRealtime(0.6f);
        AutumnLevelState.HasLoomRepaired = true;
        WorkshopSimpleGames.Show(WorkshopSimpleGame.Weaving);
        for (int row = 0; row < 3; row++)
        {
            InvokeWorktableButton("Shed " + row % 2);
            yield return null;
            InvokeWorktableButton(row % 2 == 0 ? "Pass Right" : "Pass Left");
            yield return null;
        }
        yield return Capture("03-autumn-weaving");
        CloseWorktable();
        var player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(9.3f, -2f, player.transform.position.z);
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
        }
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Capture("04-autumn-stations");

        yield return SceneManager.LoadSceneAsync("Level4_Winter");
        yield return new WaitForSecondsRealtime(0.6f);
        WorkshopSimpleGames.Show(WorkshopSimpleGame.Fitting);
        yield return Capture("05-winter-fitting");
        CloseWorktable();
        WorkshopProgress.MarkStep(WorkshopWordRules.Fitted);
        WorkshopProgress.MarkStep(WorkshopObservationView.XiaoxueLiningStep);
        WorkshopTailor.Show(null, null);
        yield return Capture("06-winter-style");
        InvokeNamedButton("ConfirmStyle");
        yield return Capture("07-winter-sewing");
        foreach (var item in Object.FindObjectsOfType<Button>())
            if (item.name == "XiaoxueFitLining") throw new System.InvalidOperationException("Retired lining prerequisite is still present.");
        Debug.Log("[WordRevision] UI PASS: style chooser and three-piece sewing open; no legacy lining requirement.");
        File.WriteAllText("../WordRevision/visual-review-complete.txt", "Actual Unity Editor frames, 1920x1080; no player build.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    private static IEnumerator Capture(string name)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        string path = Path.GetFullPath("../WordRevision/Screenshots/" + name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForSecondsRealtime(0.35f);
        Debug.Log("[WordRevision] SCREENSHOT " + path);
    }

    private static void CloseWorktable()
    {
        foreach (var button in Object.FindObjectsOfType<Button>())
            if (button.name == "Close" || (button.name == "Action" && button.transform.parent.name == "Close"))
            { button.onClick.Invoke(); break; }
        Time.timeScale = 1;
    }

    private static void InvokeWorktableButton(string parentName)
    {
        foreach (var button in Object.FindObjectsOfType<Button>())
            if (button.name == parentName || (button.name == "Action" && button.transform.parent.name == parentName))
            { button.onClick.Invoke(); return; }
        throw new System.InvalidOperationException("Missing worktable control: " + parentName);
    }

    private static void InvokeNamedButton(string name)
    {
        foreach (var button in Object.FindObjectsOfType<Button>())
            if (button.name == name) { button.onClick.Invoke(); return; }
        throw new System.InvalidOperationException("Missing interface button: " + name);
    }
}
#endif
