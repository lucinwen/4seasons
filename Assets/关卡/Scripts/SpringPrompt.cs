using UnityEngine;
using UnityEngine.SceneManagement;
using Qingming.LevelBackground;

/// <summary>桑树采摘点。鼠标和 O 键使用同一入口。</summary>
[RequireComponent(typeof(Collider2D))]
public class SpringPrompt : MonoBehaviour
{
    [Header("全局状态")]
    [Tooltip("是否已拾取采桑篮（PickupItem 拾取时自动置 true）")]
    public static bool HasPickedBasket = false;

    [Tooltip("是否已获得桑叶")]
    public static bool HasMulberryLeaves = false;

    [Tooltip("是否已拾取蚕种纸（浴蚕清洗的前置条件）")]
    public static bool HasEggPaper = false;

    [Tooltip("是否已获得蚕宝宝")]
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
        _floatBtn.Configure("采摘桑叶 · O", buttonOffset, CollectLeaves, FloatingActionButton.VisualStyle.PaperTag);
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
        if (!SpringSummerNpcDialogue.HasHarvestedLeaves) _floatBtn?.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playerNearby = false;
        if (_floatBtn != null) _floatBtn.Hide();
    }

    private void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.O)) CollectLeaves();
    }

    private void OnDisable()
    {
        playerNearby = false;
        _floatBtn?.Hide();
    }

    private void CollectLeaves()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive ||
            SpringSummerNpcDialogue.HasHarvestedLeaves ||
            !SpringSummerNpcDialogue.RequireMulberryBriefing()) return;

        if (!SpringSummerNpcDialogue.HasHarvestBasket)
        {
            SimplePopup.Show("需要采桑篮", "先收下采桑篮，再回到桑树前采摘桑叶。", "知道了");
            return;
        }

        SpringSummerNpcDialogue.RecordHarvestedLeaves();
        _floatBtn.Hide();
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") ||
               other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
    }
}
