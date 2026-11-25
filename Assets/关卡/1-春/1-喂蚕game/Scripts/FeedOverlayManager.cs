using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Qingming.LevelBackground;

/// <summary>
/// 喂蚕小游戏覆盖层：中央蚕匾、右侧桑叶及喂养进度。
/// 玩家把桑叶逐片拖拽到蚕匾上（每片冒出小爱心），全部拖完即可获得蚕宝宝道具。
/// 不切换场景，直接叠加在当前场景之上。
/// </summary>
public class FeedOverlayManager : MonoBehaviour
{
    [Header("素材引用")]
    public Sprite traySprite;          // 1-蚕匾+蚕
    public Sprite[] leafSprites;       // 1-桑叶1..7
    public Sprite heartSprite;         // 1-小爱心
    public Sprite silkwormBabySprite;  // 1-蚕宝宝

    [Header("遮罩参数")]
    [Range(0f, 1f)]
    public float overlayAlpha = MinigameUiPresentation.BackgroundVeilAlpha;

    [Header("玩法参数")]
    [Tooltip("需要拖拽的桑叶数量")]
    public int leafCount = 4;

    public static FeedOverlayManager Instance { get; private set; }

    private GameObject _root;
    private Camera _cam;
    private CameraFollow _camFollow;
    private bool _camFollowWasEnabled;
    private GameObject _springPlayer;
    private bool _playerWasActive;

    private Vector3 _trayCenter;
    private float _trayRadius;
    private int _fedCount;
    private bool _finishing;
    private FeedLeafDraggable _dragging;
    private Vector3 _dragOffset;
    private UnityEngine.UI.Text _feedProgress;
    private UnityEngine.UI.Image _feedProgressFill;

    private readonly List<FeedLeafDraggable> _leaves = new List<FeedLeafDraggable>();

    // 喂蚕期间被隐藏的场景 Canvas（结束后恢复）
    private readonly Dictionary<GameObject, bool> _canvasStates = new Dictionary<GameObject, bool>();

    public bool IsActive { get { return _root != null; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>开始喂蚕小游戏</summary>
    public void ShowFeed()
    {
        if (_root != null) return;

        _cam = Camera.main;
        if (_cam == null) { Debug.LogError("[FeedGame] 找不到主摄像机"); return; }
        if (traySprite == null) { Debug.LogError("[FeedGame] 未配置蚕匾素材，无法开始喂蚕"); return; }

        // 相机保持不动，仅禁用跟随
        _camFollow = _cam.GetComponent<CameraFollow>();
        _camFollowWasEnabled = _camFollow != null && _camFollow.enabled;
        if (_camFollow != null) _camFollow.enabled = false;

        // 停用春天场景玩家，避免移动与触发
        _springPlayer = GameObject.Find("Player");
        if (_springPlayer != null)
        {
            _playerWasActive = _springPlayer.activeSelf;
            _springPlayer.SetActive(false);
        }

        // 隐藏所有悬浮按钮，避免叠在遮罩之上
        foreach (var b in FindObjectsOfType<FloatingActionButton>(true))
        {
            if (b.IsVisible) b.Hide();
        }

        // 隐藏场景中已有的 Canvas（物品标签、关卡名等），避免它们盖在黑色遮罩之上
        HideOtherCanvases();

        BuildOverlay();
        Debug.Log("[FeedGame] 喂蚕小游戏开始（桑叶 " + leafCount + " 片）");
    }

    /// <summary>结束喂蚕小游戏并恢复原场景</summary>
    public void CloseFeed()
    {
        if (_root == null) return;

        DestroyOverlay();
        _dragging = null;
        _leaves.Clear();
        _fedCount = 0;
        _finishing = false;
        _feedProgress = null;
        _feedProgressFill = null;

        if (_springPlayer != null && _playerWasActive) _springPlayer.SetActive(true);
        if (_camFollow != null) _camFollow.enabled = _camFollowWasEnabled;

        RestoreOtherCanvases();

        Debug.Log("[FeedGame] 喂蚕小游戏结束");
    }

    private void RestartFeed()
    {
        if (_root == null || _finishing) return;

        DestroyOverlay();
        BuildOverlay();
    }

    private void DestroyOverlay()
    {
        _root.SetActive(false);
        Destroy(_root);
        _root = null;
    }

    private void HideOtherCanvases()
    {
        _canvasStates.Clear();
        foreach (var c in FindObjectsOfType<Canvas>(true))
        {
            var go = c.gameObject;
            if (_canvasStates.ContainsKey(go)) continue;
            _canvasStates[go] = go.activeSelf;
            if (go.activeSelf) go.SetActive(false);
        }
    }

    private void RestoreOtherCanvases()
    {
        foreach (var kv in _canvasStates)
        {
            if (kv.Key != null && kv.Value) kv.Key.SetActive(true);
        }
        _canvasStates.Clear();
    }

    private void BuildOverlay()
    {
        _fedCount = 0;
        _finishing = false;
        _dragging = null;
        _leaves.Clear();

        _root = new GameObject("FeedOverlayRoot");
        _root.transform.position = new Vector3(_cam.transform.position.x, _cam.transform.position.y, 0f);

        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;

        // ---------- 舞台底色 ----------
        var mask = MinigameUiPresentation.WorldVeil(_root.transform, "Feeding Veil", 1000, overlayAlpha);
        mask.transform.localScale = new Vector3(halfW * 2f + 1f, halfH * 2f + 1f, 1f);

        // ---------- 中央蚕匾+蚕 ----------
        var tray = NewSpriteObject("Tray", traySprite, _root.transform);
        var traySr = tray.GetComponent<SpriteRenderer>();
        traySr.sortingOrder = 1001;
        float trayScale = (halfH * 0.82f) / Mathf.Max(0.01f, traySprite.bounds.size.y);
        tray.transform.localPosition = Vector3.zero;
        tray.transform.localScale = Vector3.one * trayScale;

        // 精灵视觉中心（轴心可能不在正中）
        float px = traySprite.pivot.x / Mathf.Max(1f, traySprite.rect.width);
        float py = traySprite.pivot.y / Mathf.Max(1f, traySprite.rect.height);
        _trayCenter = tray.transform.position + new Vector3(
            (0.5f - px) * traySprite.bounds.size.x * trayScale,
            (0.5f - py) * traySprite.bounds.size.y * trayScale, 0f);
        _trayRadius = Mathf.Min(traySprite.bounds.size.x, traySprite.bounds.size.y) * trayScale * 0.45f;

        // ---------- 右侧一列桑叶 ----------
        int n = Mathf.Max(1, leafCount);
        float leafH = halfH * 0.34f;
        float spacing = halfH * 0.46f;
        float leafX = halfW * 0.66f;
        float startY = (n - 1) * spacing * 0.5f;

        for (int i = 0; i < n; i++)
        {
            Sprite sp = (leafSprites != null && leafSprites.Length > 0)
                ? leafSprites[i % leafSprites.Length]
                : null;
            if (sp == null) continue;

            var go = NewSpriteObject("Leaf" + i, sp, _root.transform);
            go.GetComponent<SpriteRenderer>().sortingOrder = 1002;

            float s = leafH / Mathf.Max(0.01f, sp.bounds.size.y);
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = new Vector3(leafX, startY - i * spacing, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = sp.bounds.size;
            col.isTrigger = true;

            var leaf = go.AddComponent<FeedLeafDraggable>();
            leaf.homePosition = go.transform.position;
            _leaves.Add(leaf);
        }

        BuildFeedGuide();
        WorkshopMinigameMenu.Attach(_root.transform, RestartFeed, CloseFeed, () => _fedCount > 0,
            () => !_finishing);
    }

    private void BuildFeedGuide()
    {
        var frame = WorkshopUi.NewCanvas("Feed Guide", 1001);
        frame.parent.SetParent(_root.transform, false);
        // Place the guide above the stage veil (1000) and below the leaf sprites (1002+).
        var canvas = frame.GetComponentInParent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = _cam;
        canvas.planeDistance = Mathf.Max(0.3f, _cam.nearClipPlane + 0.01f);
        var guide = WorkshopUi.PixelCard(frame, "Feeding Guide", new Rect(70f, 69f, 344f, 208f),
            MinigameUiPresentation.Paper, MinigameUiPresentation.Bronze);
        WorkshopUi.Label(guide, "Title", new Rect(24f, 20f, 296f, 45f), "谷雨喂蚕", 34,
            MinigameUiPresentation.Pine, TextAnchor.MiddleLeft, WorkshopArt.WorkshopFont);
        WorkshopUi.Fill(guide, "Divider", new Rect(24f, 72f, 48f, 2f), MinigameUiPresentation.Bronze);
        WorkshopUi.Label(guide, "Instruction", new Rect(24f, 83f, 296f, 36f), "把桑叶拖进蚕匾", 22,
            MinigameUiPresentation.MutedInk, TextAnchor.MiddleLeft, WorkshopArt.WorkshopFont);
        _feedProgress = WorkshopUi.Label(guide, "Progress", new Rect(24f, 128f, 296f, 35f), "", 24,
            MinigameUiPresentation.Pine, TextAnchor.MiddleLeft, WorkshopArt.WorkshopFont);
        WorkshopUi.Fill(guide, "Progress Track", new Rect(24f, 176f, 296f, 6f), MinigameUiPresentation.Linen);
        _feedProgressFill = WorkshopUi.Fill(guide, "Progress Fill", new Rect(24f, 176f, 0f, 6f),
            MinigameUiPresentation.Pine);
        UpdateFeedGuide();
    }

    private void UpdateFeedGuide()
    {
        if (_feedProgress == null || _feedProgressFill == null) return;
        int target = Mathf.Max(1, leafCount);
        _feedProgress.text = $"已喂养  {_fedCount} / {target}";
        _feedProgressFill.rectTransform.sizeDelta = new Vector2(296f * Mathf.Clamp01((float)_fedCount / target), 6f);
    }

    private void Update()
    {
        if (_root == null) return;
        if (WorkshopMinigameMenu.BlocksGameplayInput)
        {
            if (_dragging != null)
            {
                _dragging.Sr.sortingOrder = 1002;
                _dragging.transform.position = _dragging.homePosition;
                _dragging = null;
            }
            return;
        }

        // ---------- 拖拽中 ----------
        if (_dragging != null)
        {
            Vector3 world = ScreenToWorld(Input.mousePosition);
            _dragging.transform.position = new Vector3(world.x + _dragOffset.x, world.y + _dragOffset.y, 0f);

            if (!Input.GetMouseButton(0))
            {
                var leaf = _dragging;
                _dragging = null;
                leaf.Sr.sortingOrder = 1002;
                TryDropLeaf(leaf); // 未落在蚕匾上时由下方自动飘回原位
            }
            else WorkshopPointer.GrabWorldTarget();
        }
        // ---------- 按下拾取 ----------
        else if (!_finishing && (Input.GetMouseButtonDown(0) || WorkshopPointer.CanShowWorldHint) && !IsPointerOverUI())
        {
            bool pressed = Input.GetMouseButtonDown(0);
            Vector2 point = ScreenToWorld(Input.mousePosition);
            for (int i = _leaves.Count - 1; i >= 0; i--)
            {
                var leaf = _leaves[i];
                if (leaf == null) { _leaves.RemoveAt(i); continue; }

                var col = leaf.GetComponent<Collider2D>();
                if (col == null || !col.OverlapPoint(point)) continue;

                WorkshopPointer.PointAtWorldTarget();
                if (!pressed) break;
                _dragging = leaf;
                leaf.Sr.sortingOrder = 1010; // 拖拽时置于最前
                _dragOffset = leaf.transform.position - (Vector3)point;
                WorkshopPointer.GrabWorldTarget();
                break;
            }
        }

        // ---------- 未拖拽的桑叶平滑回到原位 ----------
        for (int i = 0; i < _leaves.Count; i++)
        {
            var leaf = _leaves[i];
            if (leaf == null || leaf == _dragging) continue;
            leaf.transform.position = Vector3.Lerp(leaf.transform.position, leaf.homePosition, Time.deltaTime * 12f);
        }
    }

    /// <summary>尝试把桑叶放到蚕匾上</summary>
    private bool TryDropLeaf(FeedLeafDraggable leaf)
    {
        if (leaf == null || leaf.consumed) return false;

        Vector2 d = (Vector2)leaf.transform.position - (Vector2)_trayCenter;
        if (d.magnitude > _trayRadius) return false;

        leaf.consumed = true;
        _leaves.Remove(leaf);
        SpawnHeart(_trayCenter + (Vector3)(Random.insideUnitCircle * (_trayRadius * 0.65f)));
        Destroy(leaf.gameObject);

        _fedCount++;
        WorkshopAudioFeedback.Fabric();
        UpdateFeedGuide();
        Debug.Log("[FeedGame] 已喂 " + _fedCount + "/" + leafCount);

        if (_fedCount >= leafCount && !_finishing) StartCoroutine(FinishRoutine());
        return true;
    }

    private IEnumerator FinishRoutine()
    {
        _finishing = true;
        yield return new WaitForSeconds(0.7f);

        bool alreadyFed = WorkshopProgress.Has(WorkshopItem.Silkworm);
        if (alreadyFed)
        {
            SimplePopup.Show("喂蚕完成", "蚕宝宝又吃饱啦！", "返回蚕房", CloseFeed);
            yield break;
        }

        MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.SilkwormFeeding, () =>
            SimplePopup.ShowItem(
                WorkshopItem.Silkworm,
                "蚕宝宝吃得饱饱的！获得道具【蚕宝宝】。",
                () =>
                {
                    SpringPrompt.HasSilkwormBaby = true;
                    WorkshopProgress.Award(WorkshopItem.Silkworm);
                    CloseFeed();
                }, fallbackIcon: silkwormBabySprite));
    }

    private void SpawnHeart(Vector3 worldPos)
    {
        if (heartSprite == null) return;

        var go = NewSpriteObject("Heart", heartSprite, _root.transform);
        go.GetComponent<SpriteRenderer>().sortingOrder = 1003;

        float h = _cam.orthographicSize * 0.15f;
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * (h / Mathf.Max(0.01f, heartSprite.bounds.size.y));
        go.AddComponent<FeedHeartEffect>();
    }

    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        var w = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
        return new Vector3(w.x, w.y, 0f);
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static GameObject NewSpriteObject(string name, Sprite sprite, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        return go;
    }

}
