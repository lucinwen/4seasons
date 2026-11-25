using UnityEngine;
using Qingming.LevelBackground;

/// <summary>浴蚕桶打开近景工位，漂洗完成后更新场景中的蚕种。</summary>
public class SilkwormWashingController : MonoBehaviour
{
    public enum RoleType { Egg, Bucket }

    [Header("角色")]
    public RoleType role = RoleType.Egg;
    public static bool washingStarted;
    public static bool washingCompleted;

    [Header("场景")]
    public bool useTrigger = true;
    public Sprite cleanedSprite;
    [HideInInspector] public float completeDistance = 3.5f; // 兼容旧场景和场景恢复工具。

    [Header("浴蚕桶介绍")]
    public Sprite introImage;
    [TextArea]
    public string introText = "这只木桶用来处理下一批蚕种。把蚕种筛浸入水中，轻轻漂洗，再捞去浮起的杂质。";
    public string startWashButtonText = "开始清洗";

    [Header("浴蚕桶悬浮按钮")]
    public string bucketButtonText = "开始清洗";
    public Vector3 bucketButtonOffset = new Vector3(0f, 2.2f, 0f);
    [Range(0f, 1f)] public float bucketButtonOpacity = 0.9f;

    private SpriteRenderer eggRenderer;
    private Sprite unwashedSprite;
    private FloatingActionButton bucketButton;
    private bool playerInRange;

    private void Start()
    {
        if (role == RoleType.Egg)
        {
            eggRenderer = GetComponent<SpriteRenderer>();
            unwashedSprite = eggRenderer != null ? eggRenderer.sprite : null;
            if (washingCompleted) ShowCleanedEgg();
        }
        else
        {
            WorkshopPickupGlow.Bind(gameObject, () => !washingCompleted &&
                SpringSummerNpcDialogue.CanWash && SpringPrompt.HasEggPaper && !washingStarted);
            bucketButton = gameObject.AddComponent<FloatingActionButton>();
            bucketButton.Configure(washingCompleted ? "已漂洗 · 再练" : bucketButtonText,
                bucketButtonOffset, OnBucketClicked);
            bucketButton.SetOpacity(bucketButtonOpacity);
        }

        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.isTrigger = true;
    }

    private static bool IsPlayer(Collider2D other) =>
        other.CompareTag("Player") || other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (bucketButton == null || !IsPlayer(other)) return;
        playerInRange = true;
        if (!washingStarted) bucketButton.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (bucketButton == null || !IsPlayer(other)) return;
        playerInRange = false;
        bucketButton.Hide();
    }

    private void OnBucketClicked()
    {
        if (role != RoleType.Bucket || WorkshopQuestGuide.BlocksWorldInput || washingStarted) return;
        if (!SpringSummerNpcDialogue.CanWash)
        {
            SimplePopup.Show("先去采桑", "先按师傅的交代完成接桑叶，再准备下一批蚕种。",
                "知道了", null);
            return;
        }

        if (!SpringPrompt.HasEggPaper && !washingCompleted)
        {
            SimplePopup.Show("缺少蚕种纸", "请先拾取下一批的蚕种纸，再到浴蚕桶漂洗。", "知道了", null);
            return;
        }

        SpringSummerNpcDialogue.BeforeBath(ShowIntroPanel);
    }

    private void ShowIntroPanel()
    {
        const string instructions = "把下一批的蚕种筛浸入木桶，沿桶口轻晃漂洗，把漂起来的杂质捞出去。";
        SimplePopup.Show("浴蚕桶", washingCompleted ? "已完成蚕种漂洗。可以再练习一遍。" : instructions,
            washingCompleted ? "再练一次" : startWashButtonText, BeginWashing, true, "返回", null, introImage);
    }

    private void BeginWashing()
    {
        SilkwormWashingController eggs = null;
        foreach (var candidate in FindObjectsOfType<SilkwormWashingController>())
        {
            if (candidate.role != RoleType.Egg || candidate.gameObject.scene != gameObject.scene) continue;
            eggs = candidate;
            break;
        }

        var bucketRenderer = GetComponent<SpriteRenderer>();
        Sprite bucket = bucketRenderer != null ? bucketRenderer.sprite : introImage;
        Sprite before = eggs != null ? eggs.unwashedSprite : null;
        Sprite after = eggs != null ? eggs.cleanedSprite : null;
        if (!SpringSilkwormWashOverlay.Show(bucket, before, after, success => EndWashing(eggs, success))) return;

        washingStarted = true;
        if (bucketButton != null) bucketButton.Hide();
    }

    private void EndWashing(SilkwormWashingController eggs, bool success)
    {
        washingStarted = false;
        if (success && !washingCompleted)
        {
            washingCompleted = true;
            foreach (var candidate in FindObjectsOfType<SilkwormWashingController>())
                if (candidate.role == RoleType.Egg && candidate.gameObject.scene == gameObject.scene)
                    candidate.ShowCleanedEgg();
            WorkshopProgress.Award(WorkshopItem.SilkwormEgg);
            if (bucketButton != null) bucketButton.SetText("已漂洗 · 再练");

            MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.SilkwormEgg, () =>
                WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.SpringBathComplete, () =>
                    SimplePopup.ShowItem(WorkshopItem.SilkwormEgg, "蚕种已漂洗完毕。",
                        null, fallbackIcon: eggs != null ? eggs.cleanedSprite : null)));
        }
        if (playerInRange && bucketButton != null) bucketButton.Show();
    }

    private void ShowCleanedEgg()
    {
        if (eggRenderer == null || cleanedSprite == null) return;
        eggRenderer.sprite = cleanedSprite;
        gameObject.name = gameObject.name.Replace("清洗前", "清洗后");
    }
}
