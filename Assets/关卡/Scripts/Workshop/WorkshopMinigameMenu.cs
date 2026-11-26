using System;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Shared pause and return controls for workshop minigame runs.</summary>
[DefaultExecutionOrder(-900)]
internal sealed class WorkshopMinigameMenu : MonoBehaviour
{
    private static WorkshopMinigameMenu pausedMenu;
    private static readonly Rect PanelBounds = new Rect(650, 315, 620, 450);

    private RectTransform launcher;
    private RectTransform pauseFrame;
    private RectTransform panel;
    private Action restart;
    private Action leave;
    private Func<bool> hasRunProgress;
    private Func<bool> canOpen;
    private Action pendingAction;
    private float previousTimeScale;
    private GameObject previousSelection;

    internal static bool IsPaused => pausedMenu != null;

    internal static WorkshopMinigameMenu Attach(Transform owner, Action restart, Action leave,
        Func<bool> hasRunProgress, Func<bool> canOpen = null)
    {
        var menu = owner.gameObject.AddComponent<WorkshopMinigameMenu>();
        menu.restart = restart;
        menu.leave = leave;
        menu.hasRunProgress = hasRunProgress;
        menu.canOpen = canOpen;
        menu.launcher = WorkshopUi.NewCanvas("Minigame Controls", 8000);
        WorkshopImmersive.Button(menu.launcher, "Pause Minigame", new Rect(1742, 25, 148, 52),
            "暂停 · Esc", menu.OpenMenu);
        menu.launcher.gameObject.SetActive(false);
        return menu;
    }

    private bool CanPauseHere => !SimplePopup.IsOpen && !WorkshopDialoguePresenter.BlocksWorldInput &&
        !MiniGameCompletionAnimation.IsPlaying && (canOpen?.Invoke() ?? true);

    private void Update()
    {
        if (pausedMenu == this)
        {
            if (!CanPauseHere)
            {
                CloseMenu(false);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                WorkshopAudioFeedback.Click();
                if (pendingAction != null) ShowOptions();
                else CloseMenu(true);
            }
            return;
        }

        bool available = !IsPaused && Time.timeScale > 0f && CanPauseHere;
        if (launcher.gameObject.activeSelf != available) launcher.gameObject.SetActive(available);
        if (available && Input.GetKeyDown(KeyCode.Escape)) OpenMenu();
    }

    private void OpenMenu()
    {
        if (IsPaused || Time.timeScale <= 0f || !CanPauseHere) return;

        previousTimeScale = Time.timeScale;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        launcher.gameObject.SetActive(false);
        pausedMenu = this;
        Time.timeScale = 0f;
        WorkshopAudioFeedback.Click();

        pauseFrame = WorkshopUi.NewCanvas("Minigame Pause", 8100);
        WorkshopUi.Block(pauseFrame, new Color32(21, 32, 29, 178));
        ShowOptions();
    }

    private void ShowOptions()
    {
        pendingAction = null;
        MakePanel(WorkshopTitle.GameName + " · 暂停");
        var resumeButton = WorkshopUi.PixelButton(panel, "Resume Minigame", new Rect(125, 120, 370, 76),
            "继续游戏", () => CloseMenu(true), true, 32);
        WorkshopUi.PixelButton(panel, "Restart Minigame", new Rect(125, 219, 370, 69),
            "重试小游戏", () => RequestAction(restart, true), false, 29);
        WorkshopUi.PixelButton(panel, "Leave Minigame", new Rect(125, 313, 370, 69),
            "返回原工位", () => RequestAction(leave, false), false, 29);
        Select(resumeButton.gameObject);
    }

    private void RequestAction(Action action, bool retry)
    {
        if (pausedMenu != this) return;
        if (!(hasRunProgress?.Invoke() ?? false))
        {
            FinishAction(action);
            return;
        }

        pendingAction = action;
        MakePanel(retry ? "重新开始本轮？" : "返回原工位？");
        WorkshopUi.Label(panel, "Progress Notice", new Rect(62, 126, 496, 104),
            "本轮进度会清空。\n已记入工坊的成果会保留。", 29, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        var back = WorkshopUi.PixelButton(panel, "Cancel Minigame Action", new Rect(61, 297, 226, 70),
            "取消", ShowOptions, false, 30);
        WorkshopUi.PixelButton(panel, "Confirm Minigame Action", new Rect(333, 297, 226, 70),
            retry ? "确认重试" : "确认返回", () => FinishAction(pendingAction), true, 30, WorkshopUi.Clay);
        Select(back.gameObject);
    }

    private void MakePanel(string title)
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
            Destroy(panel.gameObject);
        }
        panel = WorkshopUi.PixelCard(pauseFrame, "Pause Options", PanelBounds, WorkshopUi.Paper, WorkshopUi.Bronze);
        WorkshopUi.Label(panel, "Pause Title", new Rect(47, 32, 526, 58), title, 38,
            WorkshopUi.Green, TextAnchor.MiddleCenter);
    }

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
        Time.timeScale = previousTimeScale;
        WorkshopUi.CloseCanvas(pauseFrame);
        pauseFrame = null;
        panel = null;
        pendingAction = null;
        Select(restoreSelection ? previousSelection : null);
        previousSelection = null;
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
