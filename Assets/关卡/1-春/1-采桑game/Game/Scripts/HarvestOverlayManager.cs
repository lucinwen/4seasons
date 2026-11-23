using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Qingming.Harvest;
using Qingming.LevelBackground;

/// <summary>
/// 采桑游戏覆盖层管理器：不切换场景，直接在当前场景上方构建采桑小游戏，
/// 并用半透明黑色遮罩盖住原场景背景。
/// 对象/UI 的尺寸、位置、字体等全部对齐原始 HarvestBuilder 构建的场景。
/// </summary>
public class HarvestOverlayManager : MonoBehaviour
{
    // ---- 配色（对齐原始采桑小游戏）----
    private static readonly Color Paper = new Color(0.96f, 0.89f, 0.75f);
    private static readonly Color Ink   = new Color(0.20f, 0.15f, 0.10f);
    private static readonly Color Brick = new Color(0.66f, 0.27f, 0.18f);

    [Header("素材引用")]
    public Sprite pathSprite;
    public Sprite[] basketSprites = new Sprite[4];
    public Sprite[] leafSprites = new Sprite[7];
    public RuntimeAnimatorController playerAnimatorCtrl;

    [Header("遮罩参数")]
    [Range(0f, 1f)]
    public float overlayAlpha = MinigameUiPresentation.BackgroundVeilAlpha;

    [Header("采桑摄像机参数")]
    public float harvestOrthoSize = 4.5f;
    public Vector3 harvestCameraPos = new Vector3(0f, 0f, -10f);

    // ---- 运行时状态 ----
    private GameObject _overlayRoot;

    // 采桑进度文字（世界空间，跟随采桑篮）
    private Text _progressLabel;
    private Transform _progressLabelCanvas;
    private Transform _playerForLabel;
    private Camera _mainCam;
    private CameraFollow _cameraFollow;
    private bool _camFollowWasEnabled;
    private Vector3 _savedCamPos;
    private float _savedOrthoSize;
    private GameObject _mainPlayer;
    private bool _mainPlayerWasActive;

    // 春天场景触发器禁用状态（采桑期间禁用，避免采桑玩家移动触发弹窗）
    private Dictionary<MonoBehaviour, bool> _springTriggerStates;

    // 春天场景悬浮按钮显示状态（采桑期间隐藏，结束后恢复）
    private Dictionary<FloatingActionButton, bool> _floatingButtonStates;

    // 春天场景所有 Canvas 的激活状态（采桑期间全部隐藏，结束后恢复）
    private Dictionary<GameObject, bool> _springCanvasStates;

    public static HarvestOverlayManager Instance { get; private set; }

    /// <summary>
    /// 采桑小游戏是否正在进行中。
    /// 悬浮按钮/弹窗在采桑期间一律禁止显示，避免移动角色时被原场景触发器唤起。
    /// </summary>
    public static bool IsHarvestActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        IsHarvestActive = false;
    }

    /// <summary>显示采桑小游戏覆盖层</summary>
    public void ShowHarvest()
    {
        Debug.Log("[HarvestOverlay] ShowHarvest 开始");
        if (_overlayRoot != null)
        {
            Debug.LogWarning("[HarvestOverlay] _overlayRoot 已存在，跳过");
            return;
        }

        // 1. 保存并接管主摄像机（保持相机位置/orthoSize 不变，仅禁用跟随）
        _mainCam = Camera.main;
        if (_mainCam == null)
        {
            Debug.LogError("[HarvestOverlay] 找不到主摄像机");
            return;
        }
        Debug.Log($"[HarvestOverlay] 主摄像机: {_mainCam.name}, pos={_mainCam.transform.position}, orthoSize={_mainCam.orthographicSize}");
        _cameraFollow = _mainCam.GetComponent<CameraFollow>();
        _camFollowWasEnabled = _cameraFollow != null && _cameraFollow.enabled;
        if (_cameraFollow != null) _cameraFollow.enabled = false;
        _savedCamPos = _mainCam.transform.position;
        _savedOrthoSize = _mainCam.orthographicSize;

        // 不移动摄像机、不修改 orthoSize，保持春天场景的视角不变
        Debug.Log($"[HarvestOverlay] 相机保持原位: pos={_savedCamPos}, orthoSize={_savedOrthoSize}");

        // 2. 停用主场景玩家（避免与采桑玩家同名冲突）
        _mainPlayer = GameObject.Find("Player");
        if (_mainPlayer != null)
        {
            _mainPlayerWasActive = _mainPlayer.activeSelf;
            _mainPlayer.SetActive(false);
            Debug.Log($"[HarvestOverlay] 已停用主场景玩家: {_mainPlayer.name}");
        }
        else
        {
            Debug.LogWarning("[HarvestOverlay] 未找到主场景 Player");
        }

        // 3. 禁用春天场景的交互触发器，防止采桑玩家移动时触发弹窗
        IsHarvestActive = true;
        DisableSpringTriggers();

        // 3.5 隐藏所有悬浮按钮与弹窗，避免干扰采桑小游戏
        HideSpringFloatingUI();

        // 4. 构建采桑游戏
        Debug.Log("[HarvestOverlay] 开始构建覆盖层...");
        BuildHarvestOverlay();
        Debug.Log("[HarvestOverlay] 覆盖层构建完成");
    }

    /// <summary>隐藏采桑小游戏覆盖层，恢复原场景</summary>
    public void HideHarvest()
    {
        if (_overlayRoot == null) return;

        IsHarvestActive = false;
        var manager = _overlayRoot.GetComponentInChildren<MulberryHarvestGameManager>();
        if (manager != null && manager.spawner != null)
        {
            manager.spawner.StopSpawning();
            manager.spawner.ClearAllLeaves();
        }
        _overlayRoot.SetActive(false);
        Destroy(_overlayRoot);
        _overlayRoot = null;
        if (MulberryHarvestGameManager.Instance == manager)
            MulberryHarvestGameManager.Instance = null;

        // 恢复主场景玩家
        if (_mainPlayer != null)
        {
            _mainPlayer.SetActive(_mainPlayerWasActive);
            _mainPlayer = null;
        }

        // 恢复春天场景的交互触发器
        RestoreSpringTriggers();

        // 恢复春天场景的悬浮按钮显示状态
        RestoreSpringFloatingUI();

        // 恢复主摄像机
        if (_mainCam != null)
        {
            _mainCam.transform.position = _savedCamPos;
            _mainCam.orthographicSize = _savedOrthoSize;
            if (_cameraFollow != null) _cameraFollow.enabled = _camFollowWasEnabled;
        }
    }

    // ============================================================
    //  春天场景触发器控制
    // ============================================================

    /// <summary>
    /// 禁用春天场景所有交互触发器（SpringPrompt / PickupItem / SpringItemTrigger），
    /// 防止采桑玩家移动时触发原场景弹窗。仅禁用组件，不隐藏物体。
    /// </summary>
    private void DisableSpringTriggers()
    {
        _springTriggerStates = new Dictionary<MonoBehaviour, bool>();

        // SpringPrompt：采桑入口触发器
        foreach (var sp in FindObjectsOfType<SpringPrompt>(true))
            RegisterAndDisable(sp);

        // PickupItem：物品拾取（含采桑篮）
        foreach (var pi in FindObjectsOfType<PickupItem>(true))
            RegisterAndDisable(pi);

        // SpringItemTrigger：其它物品交互触发器
        foreach (var sit in FindObjectsOfType<SpringItemTrigger>(true))
            RegisterAndDisable(sit);

        // SilkwormWashingController：洗蚕相关触发器
        foreach (var swc in FindObjectsOfType<SilkwormWashingController>(true))
            RegisterAndDisable(swc);

        Debug.Log($"[HarvestOverlay] 已禁用 {_springTriggerStates.Count} 个春天场景触发器");
    }

    private void RegisterAndDisable(MonoBehaviour comp)
    {
        if (comp == null) return;
        _springTriggerStates[comp] = comp.enabled;
        comp.enabled = false;
    }

    /// <summary>恢复春天场景触发器的原始启用状态</summary>
    private void RestoreSpringTriggers()
    {
        if (_springTriggerStates == null) return;
        foreach (var kvp in _springTriggerStates)
        {
            if (kvp.Key != null) kvp.Key.enabled = kvp.Value;
        }
        _springTriggerStates = null;
        Debug.Log("[HarvestOverlay] 已恢复春天场景触发器");
    }

    /// <summary>
    /// 隐藏春天场景所有悬浮按钮，并关闭当前所有弹窗，
    /// 避免采桑小游戏进行时弹出 UI 干扰操作。
    /// </summary>
    private void HideSpringFloatingUI()
    {
        _floatingButtonStates = new Dictionary<FloatingActionButton, bool>();
        foreach (var fb in FindObjectsOfType<FloatingActionButton>(true))
        {
            if (fb == null) continue;
            _floatingButtonStates[fb] = fb.IsVisible;
            fb.Hide();
        }

        // 关闭所有已打开的弹窗（SimplePopup 一次性弹窗，直接销毁）
        foreach (var popup in FindObjectsOfType<SimplePopup>(true))
        {
            if (popup != null) Destroy(popup.gameObject);
        }

        // 隐藏春天场景所有 Canvas（任务提示/关卡名/气泡等），避免盖在黑色遮罩与采桑 UI 之上
        _springCanvasStates = new Dictionary<GameObject, bool>();
        foreach (var canvas in FindObjectsOfType<Canvas>(true))
        {
            if (canvas == null) continue;
            if (_overlayRoot != null && canvas.transform.IsChildOf(_overlayRoot.transform)) continue;
            var go = canvas.gameObject;
            if (_springCanvasStates.ContainsKey(go)) continue;
            _springCanvasStates[go] = go.activeSelf;
            go.SetActive(false);
        }

        Debug.Log($"[HarvestOverlay] 已隐藏 {_floatingButtonStates.Count} 个悬浮按钮、" +
                  $"关闭弹窗、隐藏 {_springCanvasStates.Count} 个 Canvas");
    }

    /// <summary>恢复采桑开始前处于显示状态的悬浮按钮与 Canvas</summary>
    private void RestoreSpringFloatingUI()
    {
        // 先恢复 Canvas 原始激活状态
        if (_springCanvasStates != null)
        {
            foreach (var kvp in _springCanvasStates)
            {
                if (kvp.Key != null) kvp.Key.SetActive(kvp.Value);
            }
            _springCanvasStates = null;
        }

        // 再恢复采桑前处于显示状态的悬浮按钮（其 Canvas 会由 Show 重新激活）
        if (_floatingButtonStates != null)
        {
            foreach (var kvp in _floatingButtonStates)
            {
                if (kvp.Key != null && kvp.Value) kvp.Key.Show();
            }
            _floatingButtonStates = null;
        }

        Debug.Log("[HarvestOverlay] 已恢复春天场景悬浮按钮与 Canvas");
    }

    // ============================================================
    //  构建采桑游戏对象（对齐 HarvestBuilder）
    // ============================================================

    private void BuildHarvestOverlay()
    {
        Debug.Log("[HarvestOverlay] BuildHarvestOverlay 开始");
        _overlayRoot = new GameObject("HarvestOverlayRoot");

        // 相机位置不变，将整个覆盖层根节点偏移到相机当前 X/Y，
        // 使采桑游戏对象居中显示在屏幕上。
        if (_mainCam != null)
        {
            Vector3 cp = _mainCam.transform.position;
            _overlayRoot.transform.position = new Vector3(cp.x, cp.y, 0f);
            Debug.Log($"[HarvestOverlay] 覆盖层根节点偏移到 ({cp.x:F2}, {cp.y:F2})");
        }

        // 黑色半透明遮罩
        CreateBlackOverlay(_overlayRoot.transform);
        Debug.Log("[HarvestOverlay] 黑色遮罩已创建");

        // 小路
        CreatePath(_overlayRoot.transform);
        Debug.Log("[HarvestOverlay] 小路已创建");

        // 玩家
        var playerGo = CreateHarvestPlayer();
        Debug.Log($"[HarvestOverlay] 玩家已创建: {playerGo.name}");

        // 采桑篮
        var basketGo = CreateBasket();
        Debug.Log($"[HarvestOverlay] 采桑篮已创建: {basketGo.name}");

        // 采桑进度文字（世界空间，跟随角色头顶，浅色无底框）
        _progressLabel = CreateProgressLabel(playerGo);
        Debug.Log("[HarvestOverlay] 采桑进度文字已创建（角色头顶上方）");

        // 桑叶生成器
        var spawnerGo = new GameObject("MulberryLeafSpawner");
        spawnerGo.transform.SetParent(_overlayRoot.transform, false);
        var spawner = spawnerGo.AddComponent<MulberryLeafSpawner>();
        spawner.leafSprites = leafSprites;
        spawner.leafSortingOrder = 55; // 高于黑色遮罩
        spawner.targetWorldHeight = 1f; // 对齐原始场景的桑叶尺寸
        // 相机位置不变，根据相机实际 Y/orthoSize 设置桑叶生成/销毁的世界 Y 范围
        if (_mainCam != null)
        {
            float ortho = _mainCam.orthographicSize;
            float camY = _mainCam.transform.position.y;
            spawner.spawnY = camY + ortho + 0.5f;   // 视野顶部外侧
            spawner.destroyY = camY - ortho - 0.5f; // 视野底部外侧
        }
        Debug.Log("[HarvestOverlay] 桑叶生成器已创建");

        // Canvas + UI
        var canvas = CreateCanvas();
        Debug.Log("[HarvestOverlay] Canvas已创建");
        var uiController = CreateUI(canvas.transform);
        Debug.Log($"[HarvestOverlay] UI已创建, uiController={uiController != null}");

        // 游戏管理器
        var mgrGo = new GameObject("HarvestGameManager");
        mgrGo.transform.SetParent(_overlayRoot.transform, false);
        var mgr = mgrGo.AddComponent<MulberryHarvestGameManager>();
        mgr.targetLeaves = 8;
        mgr.player = playerGo.GetComponent<HarvestPlayerController>();
        mgr.basket = basketGo.GetComponent<HarvestBasketController>();
        mgr.spawner = spawner;
        mgr.ui = uiController;
        WorkshopMinigameMenu.Attach(_overlayRoot.transform, () =>
        {
            mgr.ResetGame();
            mgr.StartGame();
        }, HideHarvest, () => mgr.CollectedLeaves > 0,
            () => mgr.State != HarvestGameState.Completed);
        Debug.Log($"[HarvestOverlay] 游戏管理器已创建, player={mgr.player != null}, basket={mgr.basket != null}, spawner={mgr.spawner != null}, ui={mgr.ui != null}");
    }

    private void CreateBlackOverlay(Transform parent)
    {
        var veil = MinigameUiPresentation.WorldVeil(parent, "Harvest Veil", 50, overlayAlpha);

        // 缩放到覆盖摄像机视野（用相机实际 orthoSize 和 aspect，留 20% 余量）
        float ortho = _mainCam != null ? _mainCam.orthographicSize : harvestOrthoSize;
        float aspect = _mainCam != null ? _mainCam.aspect : (16f / 9f);
        float halfH = ortho * 1.2f;
        float halfW = halfH * aspect;
        veil.transform.localScale = new Vector3(halfW * 2f, halfH * 2f, 1f);
    }

    private void CreatePath(Transform parent)
    {
        var go = new GameObject("Path");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -1.45f, 0f);
        go.transform.localScale = new Vector3(0.57f, 0.57f, 0.77f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pathSprite;
        sr.sortingOrder = 52;
    }

    private GameObject CreateHarvestPlayer()
    {
        var go = new GameObject("Player");
        go.transform.SetParent(_overlayRoot.transform, false);
        go.transform.localPosition = new Vector3(0f, -2.5f, 0f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // 玩家身体碰撞器
        var cap = go.AddComponent<CapsuleCollider2D>();
        cap.isTrigger = true;
        cap.direction = CapsuleDirection2D.Vertical;
        cap.size = new Vector2(1.0f, 1.5f);
        cap.offset = new Vector2(0f, 0.75f);

        // LeafCollector 触发器（覆盖身体+篮子区域）
        var collectorGo = new GameObject("LeafCollector");
        collectorGo.transform.SetParent(go.transform, false);
        var collectorCol = collectorGo.AddComponent<CapsuleCollider2D>();
        collectorCol.isTrigger = true;
        collectorCol.direction = CapsuleDirection2D.Vertical;
        collectorCol.size = new Vector2(2.2f, 2.2f);
        collectorCol.offset = new Vector2(0.5f, 1.0f);
        collectorGo.AddComponent<LeafCollector>();

        // Visual 子物体（对齐原始场景：localPosition (0,-0.18)，缩放 0.1103/0.1103/0.6386）
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        visual.transform.localScale = new Vector3(0.110266f, 0.110266f, 0.638622f);

        var vsr = visual.AddComponent<SpriteRenderer>();
        vsr.sortingOrder = 54;

        // Animator 挂在 Player 上（对齐原始场景）
        Animator anim = null;
        if (playerAnimatorCtrl != null)
        {
            anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = playerAnimatorCtrl;
        }

        // 控制器
        var ctrl = go.AddComponent<HarvestPlayerController>();
        ctrl.moveSpeed = 5f;
        ctrl.visualTransform = visual.transform;
        ctrl.animator = anim;
        // AddComponent 时 Awake 已用默认值运行过，这里显式重置到覆盖层坐标系下的出生点与边界
        // （原始场景：玩家 (0,-2.5)，左右边界 ±7）
        Vector3 rootPos = _overlayRoot.transform.position;
        ctrl.Setup(rootPos + new Vector3(0f, -2.5f, 0f), rootPos.x - 7f, rootPos.x + 7f);

        return go;
    }

    private GameObject CreateBasket()    {
        var go = new GameObject("HarvestBasket");
        go.transform.SetParent(_overlayRoot.transform, false);
        // 原始场景中篮子初始位置为 (0.97, -2.72)，与玩家保持相对偏移，靠此值快照跟随偏移
        go.transform.localPosition = new Vector3(0.97f, -2.72f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 53;
        if (basketSprites != null && basketSprites.Length > 0 && basketSprites[0] != null)
            sr.sprite = basketSprites[0];

        go.transform.localScale = new Vector3(0.16f, 0.16f, 1f);

        var bc = go.AddComponent<HarvestBasketController>();
        bc.basketSprites = basketSprites;
        return go;
    }

    /// <summary>
    /// 在角色头顶创建世界空间的进度文字（浅色、无背景框），并随角色移动。
    /// </summary>
    private Text CreateProgressLabel(GameObject player)
    {
        var canvasGo = new GameObject("ProgressLabelCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(_overlayRoot.transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 56; // 高于桑叶(55)，位于黑色遮罩之上
        if (_mainCam != null) canvas.worldCamera = _mainCam;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220f, 48f);
        canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        var badge = canvasGo.AddComponent<UnityEngine.UI.Image>();
        badge.raycastTarget = false;
        MinigameUiPresentation.Surface(badge, MinigameUiPresentation.Pine);

        var textGo = CreateUIObj("Text", canvasGo.transform);
        FullRect(textGo);
        var text = textGo.AddComponent<Text>();
        text.font = WorkshopArt.PixelFont;
        text.fontSize = 26;
        text.text = "桑叶  0 / 8";
        text.color = Paper;  // 浅色字体
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        // 记录采桑篮，位置每帧按“精灵顶部”重新计算
        _progressLabelCanvas = canvasGo.transform;
        _playerForLabel = player.transform;
        UpdateProgressLabelPosition();

        return text;
    }

    private void UpdateProgressLabelPosition()
    {
        if (_progressLabelCanvas == null || _playerForLabel == null) return;

        // 用角色精灵自身尺寸 + 轴心计算头顶位置（SpriteRenderer.bounds 当帧可能尚未刷新，不能用）
        var visual = _playerForLabel.Find("Visual");
        var sr = (visual != null ? visual : _playerForLabel).GetComponentInChildren<SpriteRenderer>();
        float topY = _playerForLabel.position.y + 0.4f;
        if (sr != null && sr.sprite != null)
        {
            float spriteH = sr.sprite.bounds.size.y * Mathf.Abs(sr.transform.lossyScale.y);
            // 轴心比例 pivotN：0=底边，1=顶边；精灵顶边到原点的距离 = (1 - pivotN) * 高度
            float pivotN = sr.sprite.pivot.y / Mathf.Max(1f, sr.sprite.rect.height);
            topY = sr.transform.position.y + spriteH * (1f - pivotN);
        }

        Vector3 p = _playerForLabel.position;
        _progressLabelCanvas.position = new Vector3(p.x, topY + 0.34f, 0f);
    }

    private void LateUpdate()
    {
        UpdateProgressLabelPosition();
    }

    private Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(_overlayRoot.transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private HarvestUIController CreateUI(Transform canvas)
    {
        // ------- HUD 容器（进度文字已移到角色头顶，见 CreateProgressLabel） -------
        var hud = CreateUIObj("HUD", canvas);
        FullRect(hud);

        // ------- 开场面板 -------
        var intro = CreateUIObj("IntroPanel", canvas);
        FullRect(intro);

        var introDim = CreateUIObj("Dim", intro.transform);
        FullRect(introDim);
        var introDimImg = introDim.AddComponent<Image>();
        introDimImg.color = new Color(0f, 0f, 0f, 0.55f);
        introDimImg.raycastTarget = true;

        var introBody = CreateUIObj("Body", intro.transform);
        PlaceCenter(introBody, Vector2.zero, new Vector2(720f, 480f));
        var introBg = introBody.AddComponent<Image>();
        introBg.color = Paper;

        var title = CreateUIObj("Title", introBody.transform);
        PlaceCenter(title, new Vector2(0f, 140f), new Vector2(640f, 60f));
        var titleText = title.AddComponent<Text>();
        titleText.font = WorkshopArt.PixelFont;
        titleText.fontSize = 40;
        titleText.text = "清明采桑";
        titleText.color = Brick;
        titleText.alignment = TextAnchor.MiddleCenter;

        var desc = CreateUIObj("Desc", introBody.transform);
        PlaceCenter(desc, new Vector2(0f, 20f), new Vector2(640f, 240f));
        var descText = desc.AddComponent<Text>();
        descText.font = WorkshopArt.PixelFont;
        descText.fontSize = 26;
        descText.text = "使用 A/D 或方向键左右移动\n接住飘落的桑叶，收集 8 片即可完成采桑";
        descText.color = Ink;
        descText.alignment = TextAnchor.MiddleCenter;
        descText.lineSpacing = 1.4f;

        var startBtnGo = CreateUIObj("StartButton", introBody.transform);
        PlaceCenter(startBtnGo, new Vector2(0f, -170f), new Vector2(240f, 80f));
        var startBtnBg = startBtnGo.AddComponent<Image>();
        startBtnBg.color = Brick;
        var startBtn = startBtnGo.AddComponent<Button>();
        var startLabelGo = CreateUIObj("Label", startBtnGo.transform);
        FullRect(startLabelGo);
        var startLabel = startLabelGo.AddComponent<Text>();
        startLabel.font = WorkshopArt.PixelFont;
        startLabel.fontSize = 28;
        startLabel.text = "开始采桑";
        startLabel.color = Paper;
        startLabel.alignment = TextAnchor.MiddleCenter;

        // ------- 通关面板 -------
        var complete = CreateUIObj("CompletePanel", canvas);
        FullRect(complete);
        complete.SetActive(false);

        var completeDim = CreateUIObj("Dim", complete.transform);
        FullRect(completeDim);
        var completeDimImg = completeDim.AddComponent<Image>();
        completeDimImg.color = new Color(0f, 0f, 0f, 0.55f);
        completeDimImg.raycastTarget = true;

        var completeBody = CreateUIObj("Body", complete.transform);
        PlaceCenter(completeBody, Vector2.zero, new Vector2(600f, 440f));
        var completeBg = completeBody.AddComponent<Image>();
        completeBg.color = Paper;

        var completeTitle = CreateUIObj("Title", completeBody.transform);
        PlaceCenter(completeTitle, new Vector2(0f, 110f), new Vector2(540f, 80f));
        var completeTitleText = completeTitle.AddComponent<Text>();
        completeTitleText.font = WorkshopArt.PixelFont;
        completeTitleText.fontSize = 44;
        completeTitleText.text = "采桑完成！";
        completeTitleText.color = Brick;
        completeTitleText.alignment = TextAnchor.MiddleCenter;

        // 桑叶奖励图像
        var leafImgGo = CreateUIObj("RewardLeaf", completeBody.transform);
        PlaceCenter(leafImgGo, new Vector2(0f, 10f), new Vector2(110f, 110f));
        var leafImg = leafImgGo.AddComponent<Image>();
        leafImg.preserveAspect = true;
        if (leafSprites != null && leafSprites.Length > 0 && leafSprites[0] != null)
            leafImg.sprite = leafSprites[0];

        var completeSub = CreateUIObj("Sub", completeBody.transform);
        PlaceCenter(completeSub, new Vector2(0f, -65f), new Vector2(540f, 60f));
        var completeSubText = completeSub.AddComponent<Text>();
        completeSubText.font = WorkshopArt.PixelFont;
        completeSubText.fontSize = 22;
        completeSubText.text = "已收集桑叶 ×8";
        completeSubText.color = Ink;
        completeSubText.alignment = TextAnchor.MiddleCenter;

        var contBtnGo = CreateUIObj("ContinueButton", completeBody.transform);
        PlaceCenter(contBtnGo, new Vector2(0f, -140f), new Vector2(200f, 70f));
        var contBtnBg = contBtnGo.AddComponent<Image>();
        contBtnBg.color = Brick;
        var contBtn = contBtnGo.AddComponent<Button>();
        var contLabelGo = CreateUIObj("Label", contBtnGo.transform);
        FullRect(contLabelGo);
        var contLabel = contLabelGo.AddComponent<Text>();
        contLabel.font = WorkshopArt.PixelFont;
        contLabel.fontSize = 26;
        contLabel.text = "继续";
        contLabel.color = Paper;
        contLabel.alignment = TextAnchor.MiddleCenter;

        // HarvestUIController 挂在 HUD 上（对齐原始场景）
        var uiController = hud.AddComponent<HarvestUIController>();
        uiController.progressText = _progressLabel; // 挂在采桑篮上方的世界空间文字
        uiController.introPanel = intro;
        uiController.startButton = startBtn;
        uiController.completePanel = complete;
        uiController.continueButton = contBtn;

        return uiController;
    }

    // ==============================================================
    // UI 工具方法（对齐 HarvestBuilder）
    // ==============================================================
    private static GameObject CreateUIObj(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform FullRect(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    private static RectTransform PlaceCenter(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform PlaceTopLeft(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }
}
