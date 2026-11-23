using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Qingming.LevelBackground;

/// <summary>
/// 夏天场景道具点击交互（纯代码安装，不修改场景）：
/// - 点击“2-盐窖”图像 → 盐窖知识卡片 → 获取盐
/// - 点击“2-木柴”图像 → 拾取木柴（获得后道具消失）
/// - 点击“2-水源”图像 → 水源知识卡片 → 取水
/// 这些道具都不显示任何悬浮提示/悬浮按钮。
/// 弹窗与悬浮按钮的尺寸、字体统一使用 SimplePopup / FloatingActionButton，与春天场景完全一致。
/// </summary>
public class SummerItemInteraction : MonoBehaviour
{
    public const string SaltObjectName = "2-盐窖";
    public const string WoodObjectName = "2-木柴";
    public const string WaterObjectName = "2-水源";
    public const string CookerObjectName = "2-煮茧锅";
    public const string CocoonRackObjectName = "2-蚕茧簇架";

    /// <summary>是否已获取盐</summary>
    public static bool SaltCollected { get; private set; }

    /// <summary>是否已获取木柴</summary>
    public static bool WoodCollected { get; private set; }

    /// <summary>是否已取水</summary>
    public static bool WaterCollected { get; private set; }

    /// <summary>是否已完成合成蚕茧并获得索绪</summary>
    public static bool SuoxuCollected { get; private set; }

    public static void CollectSuoxu()
    {
        SuoxuCollected = true;
    }

    /// <summary>从迷宫返回时玩家应出现的位置（进入迷宫时记录）</summary>
    public static bool HasReturnPosition { get; private set; }
    public static Vector3 ReturnPosition { get; private set; }

    public static void SetReturnPosition(Vector3 position)
    {
        ReturnPosition = position;
        HasReturnPosition = true;
    }

    private const string SaltKnowledge =
        "盐窖：储存粗盐的地窖。煮茧时加入少量盐，能让蚕丝更容易舒解，缫丝更顺滑。";

    private const string WoodKnowledge = "木柴：用来生火煮茧的燃料。";

    private const string WaterKnowledge = "水源：清澈的井水，煮茧与缫丝都离不开它。";

    private Collider2D _saltCol;
    private Collider2D _woodCol;
    private Collider2D _waterCol;
    private Sprite _saltSprite;
    private Sprite _woodSprite;
    private Sprite _waterSprite;
    private Camera _cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstall(scene);
    }

    private static void TryInstall(Scene scene)
    {
        if (scene.name != "Level2_Summer") return;

        // 只检查当前加载的场景，避免把上一个场景尚未销毁的实例误判为已安装
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<SummerItemInteraction>(true) != null) return;
        }

        var go = new GameObject("SummerItemInteraction");
        go.AddComponent<SummerItemInteraction>();

        // 煮茧锅：运行时挂上悬浮按钮触发器（不改场景）
        var cooker = GameObject.Find(CookerObjectName);
        if (cooker != null && cooker.GetComponent<SummerCookerTrigger>() == null)
        {
            cooker.AddComponent<SummerCookerTrigger>();
        }

        // 蚕茧簇架：运行时挂上“开始择茧”悬浮按钮触发器（不改场景）
        var rack = GameObject.Find(CocoonRackObjectName);
        if (rack != null && rack.GetComponent<SummerCocoonRackTrigger>() == null)
        {
            rack.AddComponent<SummerCocoonRackTrigger>();
        }
    }

    private void Start()
    {
        SaltCollected = WorkshopProgress.Has(WorkshopItem.Salt);
        SuoxuCollected = WorkshopProgress.Has(WorkshopItem.Suoxu);
        WoodCollected = WorkshopProgress.HasStep("summer.wood");
        WaterCollected = WorkshopProgress.HasStep("summer.water");
        _cam = Camera.main;
        ResolveItems();
        if (WoodCollected && _woodCol != null)
        {
            Destroy(_woodCol.gameObject);
            _woodCol = null;
        }
        RestoreReturnPosition();
    }

    /// <summary>从迷宫返回时把玩家放回进入迷宫前的位置。</summary>
    private static void RestoreReturnPosition()
    {
        if (!HasReturnPosition) return;
        HasReturnPosition = false;

        var player = GameObject.Find("Player");
        if (player == null) return;

        var controller = player.GetComponent<LevelPlayerController>();
        if (controller != null) controller.spawnPoint = ReturnPosition;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = ReturnPosition;
        else player.transform.position = ReturnPosition;
    }

    private void ResolveItems()
    {
        var salt = GameObject.Find(SaltObjectName);
        if (salt != null)
        {
            _saltCol = salt.GetComponent<Collider2D>();
            var sr = salt.GetComponent<SpriteRenderer>();
            if (sr != null) _saltSprite = sr.sprite;
            WorkshopPickupGlow.Bind(salt, () => !SaltCollected && !WorkshopProgress.Has(WorkshopItem.Salt));
        }

        var wood = GameObject.Find(WoodObjectName);
        if (wood != null)
        {
            _woodCol = wood.GetComponent<Collider2D>();
            var sr = wood.GetComponent<SpriteRenderer>();
            if (sr != null) _woodSprite = sr.sprite;
            WorkshopPickupGlow.Bind(wood, () => !WoodCollected && !WorkshopProgress.HasStep("summer.wood") &&
                !WorkshopProgress.Has(WorkshopItem.Spool));
        }

        var water = GameObject.Find(WaterObjectName);
        if (water != null)
        {
            _waterCol = water.GetComponent<Collider2D>();
            var sr = water.GetComponent<SpriteRenderer>();
            if (sr != null) _waterSprite = sr.sprite;
            WorkshopPickupGlow.Bind(water, () => !WaterCollected && !WorkshopProgress.HasStep("summer.water") &&
                !WorkshopProgress.Has(WorkshopItem.Spool));
        }
    }

    private void Update()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || !Input.GetMouseButtonDown(0)) return;

        // 小游戏/其它弹窗进行中不响应
        if (HarvestOverlayManager.IsHarvestActive) return;
        if (FeedOverlayManager.Instance != null && FeedOverlayManager.Instance.IsActive) return;
        if (FindObjectOfType<SimplePopup>() != null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector3 world = _cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(world.x, world.y);

        if (_saltCol != null && _saltCol.OverlapPoint(point))
        {
            OnSaltClicked();
            return;
        }

        if (_woodCol != null && _woodCol.OverlapPoint(point))
        {
            OnWoodClicked();
            return;
        }

        if (_waterCol != null && _waterCol.OverlapPoint(point))
        {
            OnWaterClicked();
        }
    }

    private void OnSaltClicked()
    {
        if (_saltSprite == null) ResolveItems();

        // 已获取过盐：只显示知识卡片
        if (SaltCollected)
        {
            WorkshopKnowledgeView.ShowItem(WorkshopItem.Salt);
            return;
        }

        SimplePopup.Show("盐窖", SaltKnowledge, "获取盐", () =>
        {
            SaltCollected = true;
            WorkshopProgress.Award(WorkshopItem.Salt);
            SimplePopup.ShowItem(WorkshopItem.Salt, "获得【盐】×1", null, fallbackIcon: _saltSprite);
        }, true, "取消", null, _saltSprite);
    }

    private void OnWoodClicked()
    {
        if (_woodSprite == null) ResolveItems();

        SimplePopup.Show("木柴", WoodKnowledge, "拾取", () =>
        {
            WoodCollected = true;
            WorkshopProgress.MarkStep("summer.wood");

            var wood = GameObject.Find(WoodObjectName);
            if (wood != null)
            {
                Destroy(wood);
                _woodCol = null;
                _woodSprite = null;
            }
        }, false, "取消", null, _woodSprite);
    }

    private void OnWaterClicked()
    {
        if (_waterSprite == null) ResolveItems();

        if (WaterCollected)
        {
            SimplePopup.Show("水源", WaterKnowledge, "知道了", null, false, "取消", null, _waterSprite);
            return;
        }

        SimplePopup.Show("水源", WaterKnowledge, "取水", () =>
        {
            WaterCollected = true;
            WorkshopProgress.MarkStep("summer.water");
            SimplePopup.Show("获得道具", "获得【水】×1", "确定", null, false, "取消", null, _waterSprite);
        }, true, "取消", null, _waterSprite);
    }
}
