using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Qingming.LevelBackground;

/// <summary>
/// 采桑游戏触发点：
/// - 玩家进入触发范围 → 在触发点上方显示悬浮"进入桑园"按钮（不打断游戏进程）
/// - 点击按钮：已拾取采桑篮 → 进入采桑游戏；未拾取 → 提示需要采桑篮
/// 挂载到触发点 GameObject 上，需要同物体有 Collider2D (Is Trigger = true)。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpringPrompt : MonoBehaviour
{
    [Header("全局状态")]
    [Tooltip("是否已拾取采桑篮（PickupItem 拾取时自动置 true）")]
    public static bool HasPickedBasket = false;

    [Tooltip("是否已获得桑叶（采桑小游戏通关后置 true）")]
    public static bool HasMulberryLeaves = false;

    [Tooltip("是否已拾取蚕种纸（浴蚕清洗的前置条件）")]
    public static bool HasEggPaper = false;

    [Tooltip("是否已获得蚕宝宝（喂蚕小游戏通关后置 true）")]
    public static bool HasSilkwormBaby = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterWorkshopProgress()
    {
        SceneManager.sceneLoaded -= OnSpringLoaded;
        SceneManager.sceneLoaded += OnSpringLoaded;
        RestoreWorkshopProgress();
    }

    private static void OnSpringLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single && scene.name == "Level1_Spring")
            RestoreWorkshopProgress();
    }

    private static void RestoreWorkshopProgress()
    {
        HasPickedBasket = WorkshopProgress.HasStep("spring.basket");
        HasEggPaper = WorkshopProgress.HasStep("spring.eggPaper");
        HasMulberryLeaves = WorkshopProgress.Has(WorkshopItem.MulberryLeaf);
        HasSilkwormBaby = WorkshopProgress.Has(WorkshopItem.Silkworm);
        SilkwormWashingController.washingStarted = false;
        SilkwormWashingController.washingCompleted = WorkshopProgress.Has(WorkshopItem.SilkwormEgg);
    }

    [Header("悬浮按钮")]
    [Tooltip("按钮文字")]
    public string buttonText = "进入桑园";

    [Tooltip("按钮相对触发点的偏移（Y 与蚕簇架“前往喂蚕”按钮同高）")]
    public Vector3 buttonOffset = new Vector3(0f, 3.33f, 0f);

    [Tooltip("按钮整体不透明度")]
    [Range(0f, 1f)]
    public float buttonOpacity = 0.9f;

    [Header("未拾取提示")]
    [Tooltip("未拾取采桑篮时的提示标题")]
    public string noBasketTitle = "需要采桑篮";

    [Tooltip("未拾取采桑篮时的提示正文")]
    [TextArea]
    public string noBasketMessage = "请先拾取采桑篮，再进入桑园完成采桑。";

    [Header("采桑游戏场景（兜底）")]
    [Tooltip("覆盖层不可用时的回退场景名")]
    public string gameSceneName = "MulberryHarvest";

    private FloatingActionButton _floatBtn;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure(buttonText, buttonOffset, OnEnterButtonClicked);
        _floatBtn.SetOpacity(buttonOpacity);
    }

    private void Start()
    {
        var mulberryTree = GameObject.Find("1-桑树1");
        WorkshopPickupGlow.Bind(mulberryTree, () => HasPickedBasket &&
            !WorkshopProgress.Has(WorkshopItem.MulberryLeaf), 0.58f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (HarvestOverlayManager.IsHarvestActive) return; // 采桑进行中不弹按钮
        if (_floatBtn != null) _floatBtn.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (_floatBtn != null) _floatBtn.Hide();
    }

    /// <summary>
    /// 点击"进入桑园"按钮：
    /// - 已拾取采桑篮 → 进入采桑游戏
    /// - 未拾取 → 提示需要采桑篮
    /// </summary>
    private void OnEnterButtonClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || !SpringSummerNpcDialogue.RequireMulberryBriefing()) return;

        if (HasPickedBasket)
        {
            LoadMulberryGame();
        }
        else
        {
            SimplePopup.Show(noBasketTitle, noBasketMessage, "知道了", null);
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") ||
               other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
    }

    private void LoadMulberryGame()
    {
        Debug.Log("[SpringPrompt] 🌿 以覆盖层方式启动采桑游戏");
        if (HarvestOverlayManager.Instance != null)
        {
            HarvestOverlayManager.Instance.ShowHarvest();
        }
        else
        {
            Debug.LogWarning("[SpringPrompt] 未找到 HarvestOverlayManager，回退到场景加载");
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
