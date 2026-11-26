using UnityEngine;
using UnityEngine.SceneManagement;
using Qingming.LevelBackground;

/// <summary>清明采桑工位。鼠标和 O 键使用同一入口。</summary>
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

    [Tooltip("是否已获得蚕宝宝（采桑小游戏通关后置 true）")]
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
    [Tooltip("按钮相对触发点的偏移")]
    public Vector3 buttonOffset = new Vector3(0f, 3.33f, 0f);

    [Tooltip("按钮整体不透明度")]
    [Range(0f, 1f)]
    public float buttonOpacity = 0.9f;

    [Header("未拾取提示")]
    [Tooltip("未拾取采桑篮时的提示标题")]
    public string noBasketTitle = "需要采桑篮";

    [Tooltip("未拾取采桑篮时的提示正文")]
    [TextArea]
    public string noBasketMessage = "先在春章收下采桑篮，再回到桑树前接桑叶。";

    [Header("备用采桑场景")]
    [Tooltip("采桑覆盖层不可用时加载此场景")]
    public string gameSceneName = "MulberryHarvest";

    private FloatingActionButton _floatBtn;
    private bool playerNearby;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure(StationLabel(), buttonOffset, OnEnterButtonClicked, FloatingActionButton.VisualStyle.PaperTag);
        _floatBtn.SetOpacity(buttonOpacity);
    }

    private void Start()
    {
        var mulberryTree = GameObject.Find("1-桑树1");
        WorkshopPickupGlow.Bind(mulberryTree, () => SpringSummerNpcDialogue.HasMulberryBriefing &&
            SpringSummerNpcDialogue.HasHarvestBasket && !SpringSummerNpcDialogue.HasHarvestedLeaves, 0.58f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playerNearby = true;
        if (HarvestOverlayManager.IsHarvestActive || _floatBtn == null) return;
        _floatBtn.SetText(StationLabel());
        _floatBtn.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playerNearby = false;
        if (_floatBtn != null) _floatBtn.Hide();
    }

    private void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.O)) OnEnterButtonClicked();
    }

    private void OnDisable()
    {
        playerNearby = false;
        if (!HarvestOverlayManager.IsHarvestActive && _floatBtn != null) _floatBtn.Hide();
    }

    private void OnEnable() => _floatBtn?.SetText(StationLabel());

    private void OnEnterButtonClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive ||
            !SpringSummerNpcDialogue.RequireMulberryBriefing()) return;

        if (!SpringSummerNpcDialogue.HasHarvestBasket)
        {
            SimplePopup.Show(noBasketTitle, noBasketMessage, "知道了", null);
            return;
        }

        var harvest = HarvestOverlayManager.Instance;
        if (harvest != null) harvest.ShowHarvest();
        else SceneManager.LoadScene(gameSceneName);
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") ||
               other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
    }

    private static string StationLabel()
    {
        return WorkshopObservationView.HasRecord(WorkshopObservation.QingmingMulberry) ? "清明采桑 · 再玩 · O" : "清明采桑 · O";
    }
}
