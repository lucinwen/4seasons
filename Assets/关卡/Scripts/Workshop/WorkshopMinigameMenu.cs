using System;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Pause controls shared by the workshop, chapters, and minigames.</summary>
[DefaultExecutionOrder(-900)]
internal sealed class WorkshopMinigameMenu : MonoBehaviour
{
    private static WorkshopMinigameMenu pausedMenu;
    private static int lastCloseFrame = -1;
    private const float PanelWidth = 660f;

    private RectTransform launcher;
    private RectTransform pauseFrame;
    private RectTransform panel;
    private Action restart;
    private Action leave;
    private Func<bool> hasRunProgress;
    private Func<bool> canOpen;
    private Action pendingAction;
    private Selectable[] navigation;
    private float previousTimeScale;
    private GameObject previousSelection;

    internal static bool IsPaused => pausedMenu != null;
    internal static bool BlocksGameplayInput => IsPaused || lastCloseFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        pausedMenu = null;
        lastCloseFrame = -1;
        SceneManager.sceneLoaded -= InstallSceneMenu;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += InstallSceneMenu;
        InstallSceneMenu(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void InstallSceneMenu(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || scene.buildIndex < 0 || scene.name == WorkshopTitle.SceneName) return;
        new GameObject("Game Pause Menu").AddComponent<WorkshopMinigameMenu>();
    }

    internal static WorkshopMinigameMenu Attach(Transform owner, Action restart, Action leave,
        Func<bool> hasRunProgress, Func<bool> canOpen = null)
    {
        var menu = owner.gameObject.AddComponent<WorkshopMinigameMenu>();
        menu.restart = restart;
        menu.leave = leave;
        menu.hasRunProgress = hasRunProgress;
        menu.canOpen = canOpen;
        menu.launcher = WorkshopUi.NewCanvas("Minigame Controls", 8000);
        WorkshopUi.PixelButton(menu.launcher, "Pause Minigame", new Rect(1742, 25, 148, 52),
            "暂停 · Esc", menu.OpenMenu, false, 25);
        menu.launcher.gameObject.SetActive(false);
        return menu;
    }

    private bool CanPauseHere => !SimplePopup.IsOpen && !WorkshopDialoguePresenter.BlocksWorldInput &&
        !MiniGameCompletionAnimation.IsPlaying && (canOpen?.Invoke() ?? true);

    private static bool CanPauseScene()
    {
        if (WorkshopObservationView.IsOpen) return WorkshopObservationView.CanPauseGameplay;
        if (SpringSilkwormWashOverlay.IsOpen || WinterDyeingOverlay.IsActive || WinterGrindingOverlay.IsActive) return true;

        return Time.timeScale > 0f && !WorkshopQuestGuide.BlocksWorldInput && !WorkshopQuestGuide.IsWorldOverlayActive &&
            !WorkshopChapterGuide.IsOpen && !WorkshopStorageView.IsOpen && !WorkshopTailor.IsOpen && !WorkshopWardrobe.IsOpen;
    }

    private bool CanOpenMenu => !BlocksGameplayInput && CanPauseHere &&
        (launcher == null ? CanPauseScene() : Time.timeScale > 0f);

    private void Update()
    {
        if (pausedMenu == this)
        {
            if (!CanPauseHere)
            {
                CloseMenu(false);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
            else if (Input.GetKeyDown(KeyCode.Tab)) SelectNext();
            return;
        }

        if (launcher != null)
        {
            bool available = CanOpenMenu;
            if (launcher.gameObject.activeSelf != available) launcher.gameObject.SetActive(available);
        }
        if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
    }

    private void HandleEscape()
    {
        if (pausedMenu != this)
        {
            OpenMenu();
            return;
        }

        WorkshopAudioFeedback.Click();
        if (pendingAction != null) ShowOptions();
        else CloseMenu(true);
    }

    private void OpenMenu()
    {
        if (!CanOpenMenu) return;

        previousTimeScale = Time.timeScale;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (launcher != null) launcher.gameObject.SetActive(false);
        pausedMenu = this;
        Time.timeScale = 0f;
        WorkshopAudioFeedback.Click();

        pauseFrame = WorkshopUi.NewCanvas("Game Pause", 30000);
        WorkshopUi.Block(pauseFrame, new Color32(21, 32, 29, 178));
        ShowOptions();
    }

    private void ShowOptions()
    {
        pendingAction = null;
        bool hasMinigameActions = launcher != null;
        MakePanel("游戏暂停", hasMinigameActions ? 470f : 386f);
        WorkshopUi.Label(panel, "Master Volume Label", new Rect(60, 126, 118, 46),
            "总音量", 28, WorkshopUi.Ink, TextAnchor.MiddleLeft, WorkshopArt.ReadingFont);
        var readout = WorkshopUi.Label(panel, "Master Volume Value", new Rect(534, 126, 66, 46),
            "", 26, WorkshopUi.Ink, TextAnchor.MiddleRight, WorkshopArt.ReadingFont);
        var volume = WorkshopTitle.CreateVolumeSlider(panel, new Rect(184, 127, 334, 44), readout);

        var resume = WorkshopUi.PixelButton(panel, "Resume Game", new Rect(60, 198, 540, 72),
            "继续游戏", () => CloseMenu(true), true, 32);
        float exitRow = hasMinigameActions ? 360f : 282f;
        var mainMenu = WorkshopUi.PixelButton(panel, "Return to Main Menu", new Rect(60, exitRow, 258, 64),
            "返回主菜单", () => ShowConfirmation("返回主菜单？", "确认返回", ReturnToTitle), false, 28);
        var quit = WorkshopUi.PixelButton(panel, "Quit Game", new Rect(342, exitRow, 258, 64),
            "退出游戏", () => ShowConfirmation("退出游戏？", "确认退出", WorkshopTitle.QuitApplication), false, 28);

        if (hasMinigameActions)
        {
            var retry = WorkshopUi.PixelButton(panel, "Restart Minigame", new Rect(60, 282, 258, 64),
                "重试小游戏", () => RequestMinigameAction(restart, "重新开始本轮？", "确认重试"), false, 28);
            var station = WorkshopUi.PixelButton(panel, "Leave Minigame", new Rect(342, 282, 258, 64),
                "返回原工位", () => RequestMinigameAction(leave, "返回原工位？", "确认返回"), false, 28);
            SetNavigation(resume, retry, station, mainMenu, quit, volume);
            LinkHorizontal(retry, station);
        }
        else SetNavigation(resume, mainMenu, quit, volume);
        LinkHorizontal(mainMenu, quit);
    }

    private void RequestMinigameAction(Action action, string title, string label)
    {
        if (pausedMenu != this) return;
        if (!(hasRunProgress?.Invoke() ?? false))
        {
            FinishAction(action);
            return;
        }

        ShowConfirmation(title, label, action, "本轮进度会清空。\n已记入工坊的成果会保留。");
    }

    private void ShowConfirmation(string title, string label, Action action, string notice = "已保存的进度会保留。")
    {
        pendingAction = action;
        MakePanel(title, 334f);
        WorkshopUi.Label(panel, "Progress Notice", new Rect(60, 130, 540, 72),
            notice, 28, WorkshopUi.Ink, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
        var back = WorkshopUi.PixelButton(panel, "Cancel Pause Action", new Rect(60, 222, 258, 68),
            "取消", ShowOptions, false, 30);
        var confirm = WorkshopUi.PixelButton(panel, "Confirm Pause Action", new Rect(342, 222, 258, 68),
            label, () => FinishAction(pendingAction), true, 30, WorkshopUi.Clay);
        SetNavigation(back, confirm);
        LinkHorizontal(back, confirm);
    }

    private void MakePanel(string title, float height)
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
            Destroy(panel.gameObject);
        }
        var bounds = new Rect((1920f - PanelWidth) / 2f, (1080f - height) / 2f, PanelWidth, height);
        panel = WorkshopUi.PixelCard(pauseFrame, "Pause Options", bounds, WorkshopUi.Paper, WorkshopUi.Bronze);
        WorkshopUi.Label(panel, "Pause Title", new Rect(60, 35, 540, 58), title, 40,
            WorkshopUi.Green, TextAnchor.MiddleCenter);
        WorkshopUi.Fill(panel, "Divider", new Rect(60, 108, 540, 2), WorkshopUi.Bronze);
    }

    private static void ReturnToTitle() => SceneManager.LoadScene(WorkshopTitle.SceneName);

    private void FinishAction(Action action)
    {
        if (pausedMenu != this) return;
        CloseMenu(false);
        action?.Invoke();
    }

    private void CloseMenu(bool restoreSelection)
    {
        if (pausedMenu != this) return;
        pausedMenu = null;
        lastCloseFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
        PlayerPrefs.Save();
        WorkshopUi.CloseCanvas(pauseFrame);
        pauseFrame = null;
        panel = null;
        pendingAction = null;
        navigation = null;
        Select(restoreSelection ? previousSelection : null);
        previousSelection = null;
    }

    private void SetNavigation(params Selectable[] options)
    {
        navigation = options;
        for (int i = 0; i < options.Length; i++)
        {
            options[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = options[(i + options.Length - 1) % options.Length],
                selectOnDown = options[(i + 1) % options.Length]
            };
        }
        Select(options[0].gameObject);
    }

    private static void LinkHorizontal(Selectable left, Selectable right)
    {
        var leftNavigation = left.navigation;
        var rightNavigation = right.navigation;
        leftNavigation.selectOnRight = right;
        rightNavigation.selectOnLeft = left;
        left.navigation = leftNavigation;
        right.navigation = rightNavigation;
    }

    private void SelectNext()
    {
        if (EventSystem.current == null) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        int index = Array.FindIndex(navigation, option => option.gameObject == selected);
        bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (index < 0) index = reverse ? 0 : navigation.Length - 1;
        int next = (index + (reverse ? -1 : 1) + navigation.Length) % navigation.Length;
        Select(navigation[next].gameObject);
    }

    private static void Select(GameObject target)
    {
        var events = EventSystem.current;
        if (events != null) events.SetSelectedGameObject(target != null && target.activeInHierarchy ? target : null);
    }

    private void OnDisable()
    {
        CloseMenu(false);
        if (launcher != null) launcher.gameObject.SetActive(false);
    }

    private void OnDestroy() => WorkshopUi.CloseCanvas(launcher);
}
