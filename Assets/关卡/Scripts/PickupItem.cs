using UnityEngine;
using UnityEngine.UI;
using Qingming.LevelBackground;

/// <summary>
/// 可拾取道具：靠近玩家时头顶显示名字，按 E 拾取后销毁并通知游戏。
/// 挂载到道具 GameObject 上。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PickupItem : MonoBehaviour
{
    [Header("显示")]
    [Tooltip("显示在头顶的名字")]
    public string itemName = "道具";

    [Tooltip("靠近多远显示名字（世界单位）")]
    public float triggerDistance = 2.5f;

    [Tooltip("名字浮在道具上方多少距离")]
    public float labelHeight = 0.8f;

    [Header("拾取")]
    public bool destroyOnPickup = true;

    [Tooltip("拾取弹窗的标题")]
    public string pickupTitle = "采桑篮";

    [Tooltip("拾取弹窗的正文")]
    public string pickupMessage = "采桑篮：用来采集桑叶的篮子";

    [Tooltip("拾取弹窗的按钮文字")]
    public string pickupButtonText = "拾取";

    private SimplePopup _pickupPopup;

    private GameObject _labelCanvas;
    private Transform _player;
    private bool _picked;

    private void Awake()
    {
        string step = WorkshopStep();
        if (step != null && WorkshopProgress.HasStep(step))
        {
            _picked = true;
            gameObject.SetActive(false);
            return;
        }
    }

    private void Start()
    {
        if (_picked) return;
        CreateLabel();
        string step = WorkshopStep();
        if (step != null) WorkshopPickupGlow.Bind(gameObject, () => !_picked && !WorkshopProgress.HasStep(step));
    }

    private string WorkshopStep()
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        if (itemName.Contains("采桑篮")) return "spring.basket";
        if (itemName.Contains("蚕种纸")) return "spring.eggPaper";
        if (itemName.Contains("锄头")) return "winter.hoe";
        return null;
    }

    private void CreateLabel()
    {
        _labelCanvas = new GameObject("LabelCanvas_" + itemName, typeof(RectTransform), typeof(Canvas));
        _labelCanvas.transform.SetParent(transform, false);
        _labelCanvas.transform.localPosition = new Vector3(0, labelHeight, 0);
        var spriteBounds = GetComponent<SpriteRenderer>().bounds;
        var position = _labelCanvas.transform.position;
        position.y = Mathf.Max(position.y, spriteBounds.max.y + 0.22f);
        _labelCanvas.transform.position = position;
        var scale = transform.lossyScale;
        const float unitsPerPixel = 0.010f;
        _labelCanvas.transform.localScale = new Vector3(
            unitsPerPixel / Mathf.Max(Mathf.Abs(scale.x), 0.001f),
            unitsPerPixel / Mathf.Max(Mathf.Abs(scale.y), 0.001f), 1f);

        var canvas = _labelCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 8;
        var cam = Camera.main;
        if (cam != null) canvas.worldCamera = cam;

        var rt = canvas.GetComponent<RectTransform>();
        float width = Mathf.Max(80f, (itemName?.Length ?? 0) * 22f + 34f);
        const float height = 38f;
        rt.sizeDelta = new Vector2(width, height);

        var scaler = _labelCanvas.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 3f;

        var tag = WorkshopUi.Card(rt, "Item Tag", new Rect(0f, 0f, width, height),
            WorkshopUi.PixelInk, WorkshopUi.Bronze);
        WorkshopUi.Label(tag, "Name", new Rect(7f, 0f, width - 14f, height), itemName, 21,
            WorkshopUi.Cream, TextAnchor.MiddleCenter, WorkshopArt.WorkshopFont);

        _labelCanvas.SetActive(false);
    }

    private void Update()
    {
        if (_picked) return;

        if (WorkshopQuestGuide.BlocksWorldInput)
        {
            if (_labelCanvas != null && _labelCanvas.activeSelf) _labelCanvas.SetActive(false);
            return;
        }

        // 采桑小游戏进行中，不响应拾取点击，避免拖拽操作误触发拾取弹窗
        if (HarvestOverlayManager.IsHarvestActive)
        {
            if (_labelCanvas != null && _labelCanvas.activeSelf) _labelCanvas.SetActive(false);
            return;
        }

        if (_player == null)
        {
            var go = GameObject.Find("Player");
            if (go != null) _player = go.transform;
            else
            {
                if (Time.frameCount % 60 == 0)
                    Debug.Log($"[PickupItem:{itemName}] Player 没找到...");
                return;
            }
        }

        float dist = Vector3.Distance(transform.position, _player.position);
        bool inRange = dist <= triggerDistance;

        if (_labelCanvas != null)
            _labelCanvas.SetActive(inRange);

        // 弹窗只能由玩家鼠标点击物品本身触发（不自动弹出、也不显示悬浮按钮）
        if (inRange && !_picked && _pickupPopup == null &&
            Input.GetMouseButtonDown(0) && !IsPointerOverUI() && IsMouseOverSelf())
        {
            string step = WorkshopStep();
            if (step == "spring.basket" && !SpringSummerNpcDialogue.RequireMulberryBriefing()) return;
            if (step == "spring.eggPaper")
            {
                if (!SpringSummerNpcDialogue.CanWash)
                {
                    SimplePopup.Show("先去采桑", "先完成接桑叶，再回来准备蚕种。", "知道了", null);
                    return;
                }
                SpringSummerNpcDialogue.BeforeBath(ShowPickupPopup);
                return;
            }
            ShowPickupPopup();
        }
    }

    /// <summary>鼠标是否点在物品自身的碰撞盒上</summary>
    private bool IsMouseOverSelf()
    {
        var cam = Camera.main;
        if (cam == null) return false;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        var col = GetComponent<Collider2D>();
        if (col != null) return col.OverlapPoint(world);

        // 没有碰撞盒时退化为按 SpriteRenderer 包围盒判定
        var sr = GetComponent<SpriteRenderer>();
        return sr != null && sr.bounds.Contains(new Vector3(world.x, world.y, sr.bounds.center.z));
    }

    private static bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    private void ShowPickupPopup()
    {
        if (_picked || _pickupPopup != null) return;

        // 采桑篮/蚕种纸/锄头的拾取弹窗显示物品图像
        Sprite icon = null;
        if (!string.IsNullOrEmpty(itemName) &&
            (itemName.Contains("采桑篮") || itemName.Contains("蚕种纸") || itemName.Contains("锄头")))
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) icon = sr.sprite;
        }

        _pickupPopup = SimplePopup.Show(
            pickupTitle,
            pickupMessage,
            pickupButtonText,
            () => Pickup(),
            false, "取消", null,
            icon);
    }

    private void Pickup()
    {
        _picked = true;
        WorkshopProgress.MarkStep(WorkshopStep());
        Debug.Log($"[PickupItem] 拾取了: {itemName}");

        // 如果拾取的是采桑篮，通知触发点已可进入采桑游戏
        if (itemName.Contains("采桑篮"))
        {
            SpringPrompt.HasPickedBasket = true;
            Debug.Log("[PickupItem] ✅ 已拾取采桑篮，采桑游戏已解锁");
        }

        // 如果拾取的是蚕种纸，解锁浴蚕清洗
        if (itemName.Contains("蚕种纸"))
        {
            SpringPrompt.HasEggPaper = true;
            Debug.Log("[PickupItem] ✅ 已拾取蚕种纸，浴蚕清洗已解锁");
            WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.SpringEggPaperCollected);
        }

        // 如果拾取的是锄头，解锁染料园
        if (itemName.Contains("锄头"))
        {
            WorkshopProgress.MarkStep("autumn.hoe");
            WinterLevelState.HasHoe = true;
            Debug.Log("[PickupItem] ✅ 已在秋季拾取锄头，染料园已解锁");
        }

        if (destroyOnPickup) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
