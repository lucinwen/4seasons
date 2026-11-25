using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Qingming.LevelBackground;
using JiangnanDye.ClothCutting;
using JiangnanDye.LoomRepair;
using JiangnanDye.MazeGame;
using JingweiWeave;
using ShiXu.CocoonMatch;

/// <summary>
/// 将小游戏作为独立场景叠加在关卡上，保留关卡相机与事件系统。
/// </summary>
[DefaultExecutionOrder(1000)]
public class MiniGameOverlay : MonoBehaviour
{
    private const int BackdropOrder = 740;
    private const int ContentOrder = 750;

    public static bool IsActive { get; private set; }

    private static MiniGameOverlay _runner;
    private LevelPlayerController _player;
    private Rigidbody2D _playerBody;
    private RigidbodyConstraints2D _playerConstraints;
    private bool _playerWasEnabled;
    private CameraFollow _cameraFollow;
    private bool _cameraFollowWasEnabled;
    private EventSystem _hostEventSystem;
    private bool _closing;
    private string _sceneName;
    private bool _usesPlayfield;
    private Scene _loadedScene;
    private PhysicsScene2D _physicsScene;
    private RectTransform _backdrop;
    private WorkshopMinigameMenu _pauseMenu;
    private Camera _playfieldCamera;
    private RenderTexture _playfieldTexture;
    private int _playfieldMask;
    private int _stageLayer;
    private int _materialsLayer;
    private TransparencySortMode _hostSortMode;
    private Vector3 _hostSortAxis;
    private readonly Dictionary<Camera, (int Culling, int Events)> _cameraMasks =
        new Dictionary<Camera, (int, int)>();

    public static void Show(string sceneName) => Open(sceneName, false);

    public static void ShowMap(string sceneName) => Open(sceneName, true);

    private static void Open(string sceneName, bool usesPlayfield)
    {
        if (IsActive || string.IsNullOrEmpty(sceneName)) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[MiniGameOverlay] 无法加载场景 " + sceneName + "，请检查 Build Settings。");
            return;
        }

        int stageLayer = LayerMask.NameToLayer("MinigameStage");
        if (usesPlayfield && stageLayer < 0)
        {
            Debug.LogError("[MiniGameOverlay] 未配置 MinigameStage 渲染层。");
            return;
        }

        var go = new GameObject("MiniGameOverlay");
        _runner = go.AddComponent<MiniGameOverlay>();
        _runner._sceneName = sceneName;
        _runner._usesPlayfield = usesPlayfield;
        _runner._stageLayer = stageLayer;
        _runner._materialsLayer = LayerMask.NameToLayer("Gatherable");
        _runner.StartCoroutine(_runner.LoadAndShow());
    }

    public static void Close()
    {
        if (_runner != null) CloseIfActive(_runner._loadedScene);
    }

    public static bool CloseIfActive(Scene scene, Action onClosed = null)
    {
        if (_runner == null || !scene.IsValid() || _runner._loadedScene != scene) return false;
        if (!_runner._closing) _runner.StartCoroutine(_runner.UnloadAndRestore(onClosed));
        return true;
    }

    private IEnumerator LoadAndShow()
    {
        IsActive = true;
        FreezePlayer();
        _backdrop = WorkshopUi.NewCanvas("Minigame Backdrop", BackdropOrder);
        WorkshopUi.Block(_backdrop, MinigameUiPresentation.BackgroundVeil);
        _hostEventSystem = EventSystem.current;
        if (_hostEventSystem != null)
        {
            _hostEventSystem.SetSelectedGameObject(null);
            _hostEventSystem.enabled = false;
        }
        _hostSortMode = GraphicsSettings.transparencySortMode;
        _hostSortAxis = GraphicsSettings.transparencySortAxis;

        if (_usesPlayfield)
        {
            _playfieldMask = 1 << _stageLayer;
            if (_materialsLayer >= 0) _playfieldMask |= 1 << _materialsLayer;
        }
        foreach (var camera in Camera.allCameras)
        {
            _cameraMasks[camera] = (camera.cullingMask, camera.eventMask);
            camera.cullingMask &= ~_playfieldMask;
            camera.eventMask = 0;
        }

        SceneManager.sceneLoaded += PrepareLoadedScene;
        var layout = new LoadSceneParameters(LoadSceneMode.Additive,
            _usesPlayfield ? LocalPhysicsMode.Physics2D : LocalPhysicsMode.None);
        var op = SceneManager.LoadSceneAsync(_sceneName, layout);
        if (op != null) yield return op;
        SceneManager.sceneLoaded -= PrepareLoadedScene;
        if (!_loadedScene.IsValid()) Destroy(gameObject);
    }

    private void PrepareLoadedScene(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Additive || scene.name != _sceneName) return;
        _loadedScene = scene;
        if (_usesPlayfield) _physicsScene = scene.GetPhysicsScene2D();

        Canvas canvas = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (_usesPlayfield)
            {
                foreach (var part in root.GetComponentsInChildren<Transform>(true))
                    if (!(part is RectTransform) && part.gameObject.layer != _materialsLayer)
                        part.gameObject.layer = _stageLayer;
            }

            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (_usesPlayfield && _playfieldCamera == null && camera.isActiveAndEnabled)
                    AttachPlayfieldCamera(camera);
                else
                    camera.enabled = false;
            }

            foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = false;

            foreach (var eventSystem in root.GetComponentsInChildren<EventSystem>(true))
                eventSystem.gameObject.SetActive(false);

            foreach (var c in root.GetComponentsInChildren<Canvas>(true))
            {
                if (!c.isRootCanvas) continue;
                canvas = c;
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.worldCamera = null;
                c.sortingOrder += ContentOrder;
            }
        }
        RestoreEventSystem();

        if (_sceneName == "LoomRepair")
        {
            HideCanvasChild(canvas, "Background");
            if (canvas != null) ShiftLoomContentDown(canvas, 40f);
            SetLoomHint(scene);
            HideRoundButtons(scene);
            ReplaceLoomPointer(scene);
            WireContinueButton(scene);
        }

        if (_sceneName == "ClothCutting")
        {
            HideCanvasChild(canvas, "Background");
            HideRoundButtons(scene);
            WireClothCuttingClose(scene);
        }

        if (_sceneName == "CocoonMatch")
        {
            HideCanvasChild(canvas, "TealBackdrop");
            StartCoroutine(PrepareCocoonControls(scene));
        }

        AttachPauseMenu(scene);
    }

    private void AttachPauseMenu(Scene scene)
    {
        switch (scene.name)
        {
            case "LoomRepair":
                var loom = FindSceneComponent<LoomRepairGameManager>(scene);
                if (loom != null) AttachPauseMenu(() => loom.IsPlaying, () => !loom.IsCompleted);
                break;
            case "EmbroideryScene":
                var embroidery = FindSceneComponent<EmbroideryGameManager>(scene);
                var embroideryUi = FindSceneComponent<EmbroideryUIController>(scene);
                if (embroideryUi != null && embroideryUi.resetButton != null)
                    embroideryUi.resetButton.gameObject.SetActive(false);
                if (embroidery != null) AttachPauseMenu(() => embroidery.HasRunProgress, () => !embroidery.isLocked);
                break;
            case "ClothCutting":
                var cutting = FindSceneComponent<ClothCuttingGameManager>(scene);
                if (cutting != null) AttachPauseMenu(() => cutting.HasRunProgress, () => !cutting.IsGameOver);
                break;
            case "CocoonMatch":
                var cocoon = FindSceneComponent<CocoonMatchController>(scene);
                if (cocoon != null)
                    AttachPauseMenu(() => cocoon.Model.BoardCount < cocoon.level.placements.Count,
                        () => cocoon.CanPause);
                break;
            case "Maze_Autumn":
                var maze = FindSceneComponent<MazeGameManager>(scene);
                if (maze != null) AttachPauseMenu(() => maze.HasRunProgress, () => maze.state != MazeState.Completed);
                break;
            case "MainScene":
                var garden = FindSceneComponent<JiangnanDye.GameManager>(scene);
                if (garden != null)
                    AttachPauseMenu(() => garden.HasRunProgress, () => garden.State == JiangnanDye.GameState.Exploring);
                break;
        }
    }

    private void AttachPauseMenu(Func<bool> hasProgress, Func<bool> canPause)
    {
        _pauseMenu = WorkshopMinigameMenu.Attach(transform, Restart, Close, hasProgress, canPause);
    }

    private void Restart()
    {
        string name = _sceneName;
        bool usesPlayfield = _usesPlayfield;
        CloseIfActive(_loadedScene, () => Open(name, usesPlayfield));
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    private void AttachPlayfieldCamera(Camera camera)
    {
        _playfieldCamera = camera;
        _playfieldTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32)
        {
            name = "Minigame Playfield",
            filterMode = FilterMode.Bilinear
        };
        camera.gameObject.tag = "Untagged";
        var aspectFitter = camera.GetComponent<JiangnanDye.CameraAspectFitter>();
        if (aspectFitter != null) aspectFitter.enabled = false;
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.aspect = 16f / 9f;
        camera.cullingMask = _playfieldMask;
        camera.eventMask = 0;
        camera.targetTexture = _playfieldTexture;

        var bounds = new Rect(192f, 108f, 1536f, 864f);
        var view = WorkshopUi.Rect(_backdrop, "Minigame Map", bounds)
            .gameObject.AddComponent<UnityEngine.UI.RawImage>();
        view.texture = _playfieldTexture;
        view.raycastTarget = false;
    }

    private static void HideCanvasChild(Canvas canvas, string name)
    {
        if (canvas == null) return;
        var child = canvas.transform.Find(name);
        if (child != null) child.gameObject.SetActive(false);
    }

    private static IEnumerator PrepareCocoonControls(Scene scene)
    {
        // 控制器先初始化自己的面板，再绑定开始、结算和返回。
        yield return null;
        if (scene.isLoaded) WireCocoonMatch(scene);
    }

    private void FixedUpdate()
    {
        if (_usesPlayfield && !_closing && _loadedScene.isLoaded && _physicsScene.IsValid())
            _physicsScene.Simulate(Time.fixedDeltaTime);
    }

    private static void ShiftLoomContentDown(Canvas canvas, float delta)
    {
        foreach (Transform child in canvas.transform)
        {
            var rt = child as RectTransform;
            if (rt == null) continue;
            rt.anchoredPosition += new Vector2(0f, -delta);
        }
    }

    private static void SetLoomHint(Scene scene)
    {
        const string hint = "点击或按空格开工，指针进入黄区时点击锤子校准";
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "HintLabel" && t.name != "StartHint") continue;
                foreach (var text in t.GetComponentsInChildren<Text>(true))
                    text.text = hint;

                // 提示词变长，放宽起始提示的点击区域，保证单行显示
                if (t.name == "StartHint")
                {
                    var rt = t as RectTransform;
                    if (rt != null)
                    {
                        var size = rt.sizeDelta;
                        size.x = Mathf.Max(size.x, 1100f);
                        rt.sizeDelta = size;
                    }
                }
            }
        }
    }

    private static void HideRoundButtons(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "ResetBtn" || t.name == "CloseBtn")
                    t.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>完成面板的「继续」按钮绑定为关闭叠加层，返回秋天场景。</summary>
    private static void WireContinueButton(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var btn in root.GetComponentsInChildren<Button>(true))
            {
                if (btn.gameObject.name != "ContinueBtn") continue;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Close);
            }
        }
    }

    /// <summary>把校准面板的指针换成锤子图标，仍绕底部手柄旋转摆动。</summary>
    private static void ReplaceLoomPointer(Scene scene)
    {
        var hammer = AutumnItemIcons.Load()?.hammer;
        if (hammer == null) return;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.name != "Pointer") continue;

                img.sprite = hammer;
                img.preserveAspect = true;
                var rt = img.rectTransform;
                // 保持 pivot 底部不动，只收窄宽度让锤子大小适合表盘
                rt.sizeDelta = new Vector2(130f, rt.sizeDelta.y);
            }
        }
    }

    /// <summary>裁衣通关后返回季节场景，进行中使用共享暂停入口。</summary>
    private static void WireClothCuttingClose(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var mgr = root.GetComponentInChildren<ClothCuttingGameManager>(true);
            if (mgr == null) continue;

            mgr.OnClose.RemoveAllListeners();
            mgr.OnClose.AddListener(Close);
        }
    }

    /// <summary>
    /// 蔟上收茧加在夏季关卡上时，使用工坊弹窗处理开始、退出和领奖。
    /// </summary>
    private static void WireCocoonMatch(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var mgr = root.GetComponentInChildren<CocoonMatchController>(true);
            if (mgr == null) continue;

            mgr.exitSceneName = "";
            mgr.onLeaveWorkshop.RemoveAllListeners();
            mgr.onLeaveWorkshop.AddListener(Close);

            mgr.useExternalResultPopup = true;
            mgr.GameEnded += won => ShowCocoonResult(mgr, won);

            // 不再使用场景自带的开始/结果/离开面板
            if (mgr.restartButton != null) mgr.restartButton.gameObject.SetActive(false);
            if (mgr.startPanel != null) mgr.startPanel.SetActive(false);
            if (mgr.resultPanel != null) mgr.resultPanel.SetActive(false);
            if (mgr.closedPanel != null) mgr.closedPanel.SetActive(false);

            KeepPopupInScene(scene, SimplePopup.Show("蔟上收茧",
                "点击亮起的蚕茧，凑齐三个相同图案。",
                "开始游戏", mgr.BeginGame, true, "离开", Close));
            return;
        }
    }

    private static void ShowCocoonResult(CocoonMatchController mgr, bool won)
    {
        if (won)
        {
            void ShowReward()
            {
                KeepPopupInScene(mgr.gameObject.scene, SimplePopup.ShowItem(WorkshopItem.Suoxu, "可与盐、线轴合成生丝。",
                    () =>
                    {
                        mgr.ClaimReward();
                        Close();
                    }, fallbackIcon: SummerSuoxuConfig.RewardSprite));
            }

            if (WorkshopProgress.Has(WorkshopItem.Suoxu))
                ShowReward();
            else
                WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.SummerCocoonComplete, ShowReward);
            return;
        }

        KeepPopupInScene(mgr.gameObject.scene, SimplePopup.Show("收集槽已满",
            "收集槽有七格，凑齐三个同类即可腾出位置。",
            "重新开始", mgr.RestartGame, true, "离开", Close));
    }

    private static void KeepPopupInScene(Scene scene, SimplePopup popup)
    {
        if (popup != null) SceneManager.MoveGameObjectToScene(popup.gameObject, scene);
    }

    private IEnumerator UnloadAndRestore(Action onClosed)
    {
        _closing = true;
        if (_pauseMenu != null) _pauseMenu.enabled = false;
        var op = SceneManager.UnloadSceneAsync(_loadedScene);
        if (op != null) yield return op;

        RestoreHost();
        _runner = null;
        IsActive = false;
        Destroy(gameObject);
        onClosed?.Invoke();
    }

    private void FreezePlayer()
    {
        var camera = Camera.main;
        _cameraFollow = camera != null ? camera.GetComponent<CameraFollow>() : null;
        if (_cameraFollow != null)
        {
            _cameraFollowWasEnabled = _cameraFollow.enabled;
            _cameraFollow.enabled = false;
        }

        _player = FindObjectOfType<LevelPlayerController>();
        if (_player == null) return;
        _playerWasEnabled = _player.enabled;
        _player.enabled = false;
        _playerBody = _player.GetComponent<Rigidbody2D>();
        if (_playerBody == null) return;
        _playerConstraints = _playerBody.constraints;
        _playerBody.velocity = Vector2.zero;
        _playerBody.constraints |= RigidbodyConstraints2D.FreezePosition;
    }

    private void RestoreHost()
    {
        RestoreEventSystem();
        if (_player != null) _player.enabled = _playerWasEnabled;
        if (_playerBody != null) _playerBody.constraints = _playerConstraints;
        if (_cameraFollow != null) _cameraFollow.enabled = _cameraFollowWasEnabled;
        foreach (var pair in _cameraMasks)
        {
            if (pair.Key == null) continue;
            pair.Key.cullingMask = pair.Value.Culling;
            pair.Key.eventMask = pair.Value.Events;
        }
        _cameraMasks.Clear();
        if (_usesPlayfield)
        {
            GraphicsSettings.transparencySortMode = _hostSortMode;
            GraphicsSettings.transparencySortAxis = _hostSortAxis;
        }

        WorkshopUi.CloseCanvas(_backdrop);
        if (_playfieldTexture != null)
        {
            if (_playfieldCamera != null) _playfieldCamera.targetTexture = null;
            _playfieldTexture.Release();
            Destroy(_playfieldTexture);
        }
    }

    private void RestoreEventSystem()
    {
        if (_hostEventSystem != null) _hostEventSystem.enabled = true;
        _hostEventSystem = null;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= PrepareLoadedScene;
        if (_runner != this) return;
        RestoreHost();
        _runner = null;
        IsActive = false;
    }
}
