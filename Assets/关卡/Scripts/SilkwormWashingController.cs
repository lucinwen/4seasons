using System.Collections.Generic;
using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 蚕种清洗交互控制器。
/// 挂在"1-蚕种清洗前"上（role=Egg）和"1-浴蚕桶"上（role=Bucket）。
/// 流程：
///   1. 玩家碰到蚕种 → 弹出清洗任务框
///   2. 玩家碰到浴蚕桶 → 弹出浴蚕桶知识介绍（配图）+ 开始清洗按钮
///   3. 点击"开始清洗" → 蚕种可拖拽
///   4. 玩家把蚕种拖到浴蚕桶处 → 替换为"1-蚕种清洗后"
/// </summary>
public class SilkwormWashingController : MonoBehaviour
{
    public enum RoleType { Egg, Bucket }

    [Header("角色")]
    public RoleType role = RoleType.Egg;

    [Header("共用状态")]
    public static bool washingStarted = false;   // 是否已开始清洗
    public static bool washingCompleted = false; // 是否已完成清洗

    [Header("触发检测")]
    [Tooltip("玩家进入碰撞盒时触发（需要 Is Trigger = true）")]
    public bool useTrigger = true;

    [Header("Egg 配置（仅 role=Egg 生效）")]
    [Tooltip("清洗后的蚕种图片")]
    public Sprite cleanedSprite;

    [Tooltip("拖拽到浴蚕桶多近算完成清洗")]
    public float completeDistance = 3.5f;

    [Header("Bucket 配置（仅 role=Bucket 生效）")]
    [Tooltip("浴蚕桶知识介绍配图")]
    public Sprite introImage;

    [Tooltip("浴蚕桶知识介绍文字")]
    [TextArea]
    public string introText = "浴蚕桶：用于清洗蚕种的木桶。将蚕种放入桶中清洗消毒，去除杂质后即可孵化。";

    [Header("按钮文字")]
    public string startWashButtonText = "开始清洗";

    [Header("浴蚕桶悬浮按钮（仅 role=Bucket 生效）")]
    [Tooltip("靠近浴蚕桶时显示的悬浮按钮文字")]
    public string bucketButtonText = "开始清洗";
    public Vector3 bucketButtonOffset = new Vector3(0f, 2.2f, 0f);
    [Range(0f, 1f)]
    public float bucketButtonOpacity = 0.9f;

    // 运行时引用
    private Camera _cam;
    private bool _dragging;
    private Vector3 _dragOffset;
    private SpriteRenderer _sr;
    private FloatingActionButton _bucketBtn;
    private static SilkwormWashingController activeStage;
    private SpriteRenderer stageVeil;
    private readonly Dictionary<SpriteRenderer, int> stageSpriteOrders =
        new Dictionary<SpriteRenderer, int>();

    private void Start()
    {
        _cam = Camera.main;
        WorkshopPickupGlow.Bind(gameObject, () => !washingCompleted &&
            (role == RoleType.Bucket ? SpringPrompt.HasEggPaper && !washingStarted : washingStarted));
        if (role == RoleType.Egg)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (washingCompleted) ShowCleanedEgg();
        }

        // 确保碰撞体为 Trigger
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        // 浴蚕桶：靠近时显示悬浮按钮（与其他悬浮按钮同样式）
        if (role == RoleType.Bucket)
        {
            _bucketBtn = gameObject.AddComponent<FloatingActionButton>();
            _bucketBtn.Configure(bucketButtonText, bucketButtonOffset, OnBucketClicked);
            _bucketBtn.SetOpacity(bucketButtonOpacity);
        }
    }

    // ============ 碰撞盒触发 ============

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        OnPlayerEnter();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (role == RoleType.Bucket && _bucketBtn != null) _bucketBtn.Hide();
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") ||
               other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
    }

    private void OnPlayerEnter()
    {
        // 蚕种（Egg）：经过时完全不触发任何 UI，只能由浴蚕桶按钮开启清洗后拖拽
        if (role == RoleType.Egg) return;

        // Bucket：进入范围只显示悬浮按钮，不自动弹窗
        if (washingStarted || washingCompleted) return;
        if (_bucketBtn != null) _bucketBtn.Show();
    }

    /// <summary>点击浴蚕桶悬浮按钮：先校验蚕种纸，再弹出清洗介绍</summary>
    private void OnBucketClicked()
    {
        if (role != RoleType.Bucket || WorkshopQuestGuide.BlocksWorldInput) return;
        if (washingStarted || washingCompleted) return;

        if (!SpringPrompt.HasEggPaper)
        {
            SimplePopup.Show("缺少蚕种纸", "请先拾取蚕种纸，再进行蚕种清洗。", "知道了", null);
            return;
        }

        SpringSummerNpcDialogue.BeforeBath(ShowIntroPanel);
    }

    // ============ 弹窗 ============

    private void ShowIntroPanel()
    {
        // 配图由 SimplePopup 统一排版（图文不重叠）
        SimplePopup.Show("浴蚕桶", introText, startWashButtonText, BeginWashing,
            false, "取消", null, introImage);
    }

    private void BeginWashing()
    {
        washingStarted = true;
        if (_bucketBtn != null) _bucketBtn.Hide();
        if (_cam == null) return;

        activeStage = this;
        stageVeil = MinigameUiPresentation.WorldVeil(_cam.transform, "Washing Veil", 1000);
        stageVeil.transform.localPosition = Vector3.forward * Mathf.Abs(_cam.transform.position.z);
        RaiseSprites(transform, 1001);
        foreach (var candidate in FindObjectsOfType<SilkwormWashingController>())
        {
            if (candidate.role != RoleType.Egg || candidate.gameObject.scene != gameObject.scene) continue;
            RaiseSprites(candidate.transform, 1002);
        }
        ResizeVeil();
    }

    private void RaiseSprites(Transform root, int order)
    {
        foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            stageSpriteOrders[sprite] = sprite.sortingOrder;
            sprite.sortingOrder = order;
        }
    }

    private void LateUpdate()
    {
        if (activeStage == this) ResizeVeil();
    }

    private void ResizeVeil()
    {
        if (stageVeil == null || _cam == null) return;
        float height = _cam.orthographicSize * 2f;
        stageVeil.transform.localScale = new Vector3(height * _cam.aspect + 1f, height + 1f, 1f);
    }

    private void EndWashing()
    {
        if (activeStage != this) return;
        activeStage = null;
        foreach (var pair in stageSpriteOrders)
            if (pair.Key != null) pair.Key.sortingOrder = pair.Value;
        stageSpriteOrders.Clear();

        if (stageVeil != null) Destroy(stageVeil.gameObject);
        stageVeil = null;
    }

    private void OnDestroy()
    {
        EndWashing();
    }

    // ============ 拖拽（仅 Egg） ============

    private void OnMouseDown()
    {
        if (role != RoleType.Egg || WorkshopQuestGuide.BlocksWorldInput) return;
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null && events.IsPointerOverGameObject()) return;
        if (!washingStarted || washingCompleted) return;

        _dragging = true;
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        _dragOffset = transform.position - mouseWorld;
    }

    private void OnMouseDrag()
    {
        if (!_dragging) return;

        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        transform.position = mouseWorld + _dragOffset;
    }

    private void OnMouseUp()
    {
        if (!_dragging) return;
        _dragging = false;

        // 检查是否拖到浴蚕桶附近
        foreach (var candidate in FindObjectsOfType<SilkwormWashingController>())
        {
            if (candidate.role != RoleType.Bucket) continue;
            if (Vector3.Distance(transform.position, candidate.transform.position) <= completeDistance)
                CompleteWashing();
            break;
        }
    }

    private void CompleteWashing()
    {
        washingCompleted = true;
        Debug.Log("[SilkwormWashing] ✅ 蚕种清洗完成！");
        ShowCleanedEgg();
        if (activeStage != null) activeStage.EndWashing();

        MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.SilkwormEgg, () =>
            WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.SpringBathComplete, () =>
                SimplePopup.ShowItem(WorkshopItem.SilkwormEgg, "蚕种已清洗消毒完毕，可以孵化了。",
                    () => WorkshopProgress.Award(WorkshopItem.SilkwormEgg), fallbackIcon: cleanedSprite)));
    }

    private void ShowCleanedEgg()
    {
        if (_sr != null && cleanedSprite != null)
        {
            _sr.sprite = cleanedSprite;
            gameObject.name = gameObject.name.Replace("清洗前", "清洗后");
        }
    }
}
