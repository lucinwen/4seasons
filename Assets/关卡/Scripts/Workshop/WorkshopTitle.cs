using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class WorkshopTitle : MonoBehaviour
{
    public const string SceneName = "FourSeasonsTitle";
    public const string GameName = "四时织坊";

    private const string VolumeKey = "ShiXu.Settings.MasterVolume";
    private const string FullscreenKey = "ShiXu.Settings.Fullscreen";
    private const string CoverResource = "WorkshopFinish/TitleCover";
    private const int PostOfficeButtonIndex = 3;
    private const int HeritageButtonIndex = 4;
    private const int QuitButtonIndex = 5;

    private static readonly Color Jade = new Color32(41, 83, 68, 255);
    private static readonly Color Red = new Color32(156, 78, 66, 255);
    private static readonly Color Gold = new Color32(197, 159, 92, 255);

    private static readonly float[] ButtonTops = { 537f, 629f, 722f, 810f, 810f, 878f };
    private readonly UnityEngine.UI.Button[] menuButtons = new UnityEngine.UI.Button[ButtonTops.Length];
    private readonly RectTransform[] focusMarks = new RectTransform[ButtonTops.Length];
    private RectTransform frame;
    private RectTransform modal;
    private CanvasGroup menuGroup;
    private UnityEngine.UI.Button modalReturn;
    private UnityEngine.UI.Selectable[] modalNavigation;
    private bool postOfficeOpen;
    private bool leaving;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedSettings()
    {
        WorkshopLegacyPreferences.Import();
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        if (PlayerPrefs.HasKey(FullscreenKey))
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) != 0;
    }

    private void Start()
    {
        frame = WorkshopUi.NewCanvas("Four Seasons Title", 300);
        WorkshopUi.Block(frame, new Color32(34, 63, 56, 255));

        var coverBounds = new Rect(0f, 0f, 1920f, 1080f);
        var cover = Resources.Load<Texture2D>(CoverResource);
        if (cover != null)
        {
            var art = WorkshopUi.Rect(frame, "Title Cover Artwork", coverBounds).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            art.texture = cover;
            art.raycastTarget = false;
            var wordmark = Resources.Load<Sprite>("WorkshopFinish/TitleWordmark");
            if (wordmark != null)
                WorkshopUi.Picture(frame, "Title Wordmark", CoverRect(253f, 228f, 212f, 170f), wordmark);
            else
            {
                Label(frame, "Title First Line", CoverRect(166f, 222f, 394f, 88f), "四时", 110, Jade);
                Label(frame, "Title Second Line", CoverRect(166f, 307f, 394f, 98f), "织坊", 123, Jade);
            }
        }
        else
        {
            WorkshopUi.Panel(frame, "Title Frame", coverBounds, WorkshopArt.WorkshopTaskBoard);
            Label(frame, "Fallback Title", new Rect(172f, 190f, 580f, 145f), GameName, 94, Jade);
        }

        RectTransform menu = WorkshopUi.Rect(frame, "Title Menu", coverBounds);
        menuGroup = menu.gameObject.AddComponent<CanvasGroup>();
        bool canContinue = WorkshopProgress.HasSavedGame;
        menuButtons[0] = AddMenuButton(menu, 0, "Title Start Game", "开始游戏", StartGame, true, cover != null);
        menuButtons[1] = AddMenuButton(menu, 1, "Title Continue Game", "继续游戏", EnterWorkshop, canContinue, cover != null);
        menuButtons[2] = AddMenuButton(menu, 2, "Title Settings", "游戏设置", ShowSettings, true, cover != null);
        var postOfficeButton = AddMenuButton(menu, PostOfficeButtonIndex, "Title Send Postcard", "寄明信片", ShowPostOffice, true, cover != null);
        menuButtons[PostOfficeButtonIndex] = postOfficeButton;
        var heritageButton = AddMenuButton(menu, HeritageButtonIndex, "Title Heritage Sources", "新闻与来源",
            ShowHeritage, true, cover != null);
        menuButtons[HeritageButtonIndex] = heritageButton;
        var quitButton = AddMenuButton(menu, QuitButtonIndex, "Title Quit Game", "退出游戏",
            ShowQuitConfirmation, true, cover != null);
        menuButtons[QuitButtonIndex] = quitButton;
        if (!canContinue && cover != null)
        {
            var tint = WorkshopUi.Fill(menuButtons[1].transform, "Continue Unavailable",
                new Rect(30f, 12f, 313f, 60f), new Color32(230, 224, 207, 132));
            tint.raycastTarget = false;
        }

        SetNavigation(canContinue
            ? menuButtons
            : new UnityEngine.UI.Selectable[] { menuButtons[0], menuButtons[2], postOfficeButton, heritageButton, quitButton });
        Select(menuButtons[0]);
    }

    private UnityEngine.UI.Button AddMenuButton(RectTransform menu, int index, string name, string label,
        Action action, bool enabled, bool hasCoverArt)
    {
        bool isShortcut = index >= PostOfficeButtonIndex;
        // Keep the first three targets aligned to the cover's painted buttons.
        float left = index switch
        {
            HeritageButtonIndex => 318f,
            QuitButtonIndex => 237f,
            _ => 156f
        };
        float height = index == QuitButtonIndex ? 46f : isShortcut ? 60f : 75f;
        var bounds = CoverRect(left, ButtonTops[index], isShortcut ? 156f : 325f, height);
        bool useCoverButton = hasCoverArt && !isShortcut;
        var button = WorkshopUi.Button(menu, name, bounds, "", action, useCoverButton ? Color.clear : Jade, enabled);
        if (useCoverButton)
        {
            var face = WorkshopUi.Fill(button.transform, "Cover Button Face",
                new Rect(7f, 3f, bounds.width - 14f, bounds.height - 6f), Color.clear);
            WorkshopUi.ApplyPixelShape(face);
            face.pixelsPerUnitMultiplier = 0.20f;
            button.targetGraphic = face;
        }
        if (isShortcut)
        {
            var trim = WorkshopUi.Fill(button.transform, "Gold Trim", new Rect(4f, 4f, bounds.width - 8f, bounds.height - 8f), Gold);
            WorkshopUi.ApplyPixelShape(trim);
            var face = WorkshopUi.Fill(button.transform, "Paper Face", new Rect(7f, 7f, bounds.width - 14f, bounds.height - 14f),
                new Color32(248, 228, 193, 255));
            WorkshopUi.ApplyPixelShape(face);
            button.targetGraphic = face;
        }
        var labelColor = isShortcut || (useCoverButton && index > 0) ? Jade : WorkshopUi.Cream;
        Label(button.transform, "Label", new Rect(0f, 0f, bounds.width, bounds.height), label, isShortcut ? 26 : 39, labelColor);
        button.transition = UnityEngine.UI.Selectable.Transition.None;

        var mark = WorkshopUi.Rect(button.transform, name + " Focus", new Rect(0f, 0f, bounds.width, bounds.height));
        focusMarks[index] = mark;
        var shine = new Color32(255, 239, 184, 240);
        WorkshopUi.Fill(mark, "Top Focus Seam", new Rect(34f, 6f, bounds.width - 68f, 4f), shine);
        WorkshopUi.Fill(mark, "Bottom Focus Seam", new Rect(34f, bounds.height - 10f, bounds.width - 68f, 4f), shine);
        mark.gameObject.SetActive(false);
        return button;
    }

    private void Update()
    {
        if (leaving || menuGroup == null || WorkshopHeritageBrief.IsOpen) return;
        if (!postOfficeOpen && modal != null && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseModal();
            return;
        }

        if (!postOfficeOpen && Input.GetKeyDown(KeyCode.Tab))
        {
            if (modal != null) SelectNext(modalNavigation);
            else SelectNext(menuButtons);
        }

        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        Vector2 mouse = Input.mousePosition;
        for (int index = 0; index < menuButtons.Length; index++)
        {
            var button = menuButtons[index];
            bool focused = modal == null && button.IsInteractable() &&
                (selected == button.gameObject ||
                 RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform, mouse));
            if (focusMarks[index].gameObject.activeSelf != focused)
                focusMarks[index].gameObject.SetActive(focused);
        }
    }

    private void StartGame()
    {
        if (!WorkshopProgress.HasSavedGame)
        {
            ShowOpening();
            return;
        }

        RectTransform panel = OpenModal("Title Restart Confirmation", "重新开始游戏", menuButtons[0]);
        Label(panel, "Restart Message", new Rect(120f, 164f, 740f, 110f),
            "重新开始将清空现有的四季任务、图鉴与成衣进度。", 30, WorkshopUi.Ink);
        var cancel = DialogButton(panel, "Title Cancel Restart", new Rect(130f, 365f, 310f, 72f), "返回封面", CloseModal, Jade);
        var confirm = DialogButton(panel, "Title Confirm Restart", new Rect(524f, 365f, 310f, 72f), "确认重新开始", () =>
        {
            CloseModal();
            ShowOpening();
        }, Red);
        SetConfirmationNavigation(cancel, confirm);
    }

    private void ShowQuitConfirmation()
    {
        RectTransform panel = OpenModal("Title Quit Confirmation", "退出游戏？", menuButtons[QuitButtonIndex], 326f);
        var cancel = DialogButton(panel, "Title Cancel Quit", new Rect(130f, 192f, 310f, 72f), "取消", CloseModal, Jade);
        var confirm = DialogButton(panel, "Title Confirm Quit", new Rect(524f, 192f, 310f, 72f), "确认退出", QuitGame, Red);
        SetConfirmationNavigation(cancel, confirm);
    }

    private void QuitGame()
    {
        if (leaving) return;
        leaving = true;
        QuitApplication();
    }

    internal static void QuitApplication()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowOpening()
    {
        BlockMenu(menuButtons[0]);
        WorkshopHeritageBrief.ShowOpening(StartNewJourney, RestoreTitleMenu);
    }

    private void ShowHeritage()
    {
        BlockMenu(menuButtons[HeritageButtonIndex]);
        WorkshopHeritageBrief.Show(RestoreTitleMenu);
    }

    private void StartNewJourney()
    {
        if (leaving) return;
        WorkshopProgress.StartNewGame();
        EnterWorkshop();
    }

    private void EnterWorkshop()
    {
        if (leaving) return;
        leaving = true;
        SceneManager.LoadScene(WorkshopProgress.HubScene);
    }

    private void ShowPostOffice()
    {
        if (leaving || modal != null) return;
        BlockMenu(menuButtons[PostOfficeButtonIndex]);
        postOfficeOpen = true;

        var shade = WorkshopUi.Fill(frame.parent, "Title Post Office Shade", new Rect(), new Color32(29, 46, 40, 190));
        shade.raycastTarget = true;
        modal = shade.rectTransform;
        modal.anchorMin = Vector2.zero;
        modal.anchorMax = Vector2.one;
        modal.offsetMin = modal.offsetMax = Vector2.zero;

        var layout = WorkshopUi.Rect(modal, "Title Post Office", new Rect(0f, 0f, 1920f, 1080f));
        layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(0.5f, 0.5f);
        layout.anchoredPosition = Vector2.zero;
        SeasonalPostOffice.Open(layout, CloseModal);
    }

    private void ShowSettings()
    {
        RectTransform panel = OpenModal("Title Settings Dialog", "游戏设置", menuButtons[2]);
        Label(panel, "Master Volume Label", new Rect(125f, 150f, 285f, 67f), "总音量", 32, Jade);
        var readout = Label(panel, "Master Volume Value", new Rect(737f, 150f, 99f, 67f), "", 30, Jade);
        var slider = CreateVolumeSlider(panel, new Rect(380f, 162f, 345f, 44f), readout);

        Label(panel, "Fullscreen Label", new Rect(125f, 242f, 285f, 67f), "显示模式", 32, Jade);
        UnityEngine.UI.Button mode = null;
        mode = DialogButton(panel, "Title Fullscreen", new Rect(430f, 243f, 385f, 65f),
            FullscreenLabel(), () =>
            {
                bool fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 0;
                Screen.fullScreen = fullScreen;
                PlayerPrefs.SetInt(FullscreenKey, fullScreen ? 1 : 0);
                PlayerPrefs.Save();
                mode.GetComponentInChildren<UnityEngine.UI.Text>().text = FullscreenLabel();
            }, Jade);
        var back = DialogButton(panel, "Title Settings Back", new Rect(323f, 365f, 310f, 72f), "返回封面", CloseModal, Jade);
        SetModalNavigation(slider, mode, back);
    }

    internal static UnityEngine.UI.Slider CreateVolumeSlider(RectTransform panel, Rect bounds, UnityEngine.UI.Text readout)
    {
        const float trackInset = 8f;
        const float trackHeight = 10f;
        const float handleHeight = 30f;
        float trackWidth = bounds.width - 2f * trackInset;

        var sliderHit = WorkshopUi.Fill(panel, "Master Volume Slider", bounds,
            new Color32(255, 255, 255, 1));
        sliderHit.raycastTarget = true;
        RectTransform sliderArea = sliderHit.rectTransform;
        var track = WorkshopUi.Fill(sliderArea, "Volume Track",
            new Rect(trackInset, (bounds.height - trackHeight) * 0.5f, trackWidth, trackHeight),
            new Color32(174, 157, 115, 255)).rectTransform;
        var level = WorkshopUi.Fill(track, "Volume Level", new Rect(0f, 0f, trackWidth, trackHeight), Jade).rectTransform;
        level.anchorMin = Vector2.zero;
        level.anchorMax = Vector2.one;
        level.offsetMin = level.offsetMax = Vector2.zero;
        var handleTrack = WorkshopUi.Rect(sliderArea, "Handle Track", new Rect(trackInset, 0f, trackWidth, bounds.height));
        var handle = WorkshopUi.Fill(handleTrack, "Volume Handle", new Rect(0f, 0f, 18f, handleHeight), Jade);
        handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        handle.rectTransform.anchorMin = Vector2.zero;
        handle.rectTransform.anchorMax = Vector2.up;
        handle.rectTransform.sizeDelta = new Vector2(18f, handleHeight - bounds.height);
        handle.rectTransform.anchoredPosition = Vector2.zero;
        handle.raycastTarget = true;
        var slider = sliderArea.gameObject.AddComponent<UnityEngine.UI.Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = level;
        slider.handleRect = handle.rectTransform;
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.wholeNumbers = true;
        slider.SetValueWithoutNotify(Mathf.RoundToInt(AudioListener.volume * 100f));
        readout.text = $"{slider.value:0}%";
        slider.onValueChanged.AddListener(value =>
        {
            AudioListener.volume = value / 100f;
            readout.text = $"{value:0}%";
            PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        });
        return slider;
    }

    private static string FullscreenLabel() =>
        PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0 ? "全屏" : "窗口";

    private RectTransform OpenModal(string name, string title, UnityEngine.UI.Button origin, float height = 516f)
    {
        BlockMenu(origin);
        modal = WorkshopUi.Rect(frame, name, new Rect(0f, 0f, 1920f, 1080f));
        var shade = WorkshopUi.Fill(modal, "Title Modal Shade", new Rect(0f, 0f, 1920f, 1080f), new Color32(29, 46, 40, 175));
        shade.raycastTarget = true;
        var panel = WorkshopUi.Panel(modal, "Title Modal Frame", new Rect(480f, (1080f - height) * 0.5f, 960f, height),
            WorkshopArt.WorkshopTaskBoard).rectTransform;
        Label(panel, "Title Modal Heading", new Rect(130f, 66f, 700f, 68f), title, 49, Jade);
        WorkshopUi.Fill(panel, "Title Modal Rule", new Rect(118f, 140f, 724f, 2f), Gold);
        return panel;
    }

    private void BlockMenu(UnityEngine.UI.Button origin)
    {
        modalReturn = origin;
        menuGroup.interactable = false;
        menuGroup.blocksRaycasts = false;
    }

    private void CloseModal()
    {
        if (modal == null) return;
        modal.gameObject.SetActive(false);
        Destroy(modal.gameObject);
        modal = null;
        modalNavigation = null;
        postOfficeOpen = false;
        PlayerPrefs.Save();
        RestoreTitleMenu();
    }

    private void RestoreTitleMenu()
    {
        menuGroup.interactable = true;
        menuGroup.blocksRaycasts = true;
        if (modalReturn != null) Select(modalReturn);
        modalReturn = null;
    }

    private void SetModalNavigation(params UnityEngine.UI.Selectable[] options)
    {
        modalNavigation = options;
        SetNavigation(options);
        Select(options[0]);
    }

    private void SetConfirmationNavigation(UnityEngine.UI.Button cancel, UnityEngine.UI.Button confirm)
    {
        SetModalNavigation(cancel, confirm);
        var cancelNav = cancel.navigation;
        var confirmNav = confirm.navigation;
        cancelNav.selectOnLeft = cancelNav.selectOnRight = confirm;
        confirmNav.selectOnLeft = confirmNav.selectOnRight = cancel;
        cancel.navigation = cancelNav;
        confirm.navigation = confirmNav;
    }

    private static void SetNavigation(UnityEngine.UI.Selectable[] options)
    {
        for (int i = 0; i < options.Length; i++)
        {
            options[i].navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = options[(i + options.Length - 1) % options.Length],
                selectOnDown = options[(i + 1) % options.Length]
            };
        }
    }

    private static void SelectNext(UnityEngine.UI.Selectable[] options)
    {
        if (options == null || options.Length == 0 || EventSystem.current == null) return;
        int current = Array.FindIndex(options, option => option.gameObject == EventSystem.current.currentSelectedGameObject);
        bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        int direction = reverse ? -1 : 1;
        if (current < 0) current = reverse ? 0 : options.Length - 1;
        for (int offset = 1; offset <= options.Length; offset++)
        {
            var candidate = options[(current + offset * direction + options.Length) % options.Length];
            if (!candidate.IsInteractable()) continue;
            Select(candidate);
            return;
        }
    }

    private static void Select(UnityEngine.UI.Selectable option)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(option.gameObject);
    }

    private static UnityEngine.UI.Button DialogButton(Transform parent, string name, Rect bounds, string text,
        Action onClick, Color color)
    {
        var button = WorkshopUi.Button(parent, name, bounds, text, onClick, color, size: 31, font: WorkshopArt.ReadingFont);
        button.GetComponentInChildren<UnityEngine.UI.Text>().verticalOverflow = VerticalWrapMode.Overflow;
        return button;
    }

    private static UnityEngine.UI.Text Label(Transform parent, string name, Rect bounds, string text, int size, Color color)
    {
        var label = WorkshopUi.Label(parent, name, bounds, text, size, color, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private static Rect CoverRect(float x, float y, float width, float height)
    {
        const float scaleX = 1920f / 1672f;
        const float scaleY = 1080f / 941f;
        return new Rect(x * scaleX, y * scaleY, width * scaleX, height * scaleY);
    }
}
