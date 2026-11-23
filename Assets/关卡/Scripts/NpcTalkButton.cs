using UnityEngine;
using UnityEngine.SceneManagement;
using Qingming.LevelBackground;

/// <summary>
/// NPC 对话悬浮按钮（运行时自动挂到四季场景的 NPC 上，不修改场景）：
/// - 玩家靠近 NPC → NPC 头顶显示当前对话或领取动作
/// - 玩家离开 → 隐藏
/// - 点击 → 根据季节和任务阶段播放对话
/// 按钮尺寸/字号与其它悬浮按钮完全一致。
/// </summary>
public class NpcTalkButton : MonoBehaviour
{
    [Header("按钮")]
    public string buttonText = "对话";
    [Range(0f, 1f)]
    public float buttonOpacity = 0.96f;

    [Tooltip("玩家与 NPC 的距离小于该值时显示按钮")]
    public float talkDistance = 3.5f;

    [Tooltip("按钮相对 NPC 头顶再往上留出的距离")]
    public float headGap = 0.75f;

    private FloatingActionButton _floatBtn;
    private FloatingActionButton _solarBtn;
    private Transform _player;
    private Vector3 _solarOffset;
    private bool _near;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Install(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install(scene);
    }

    private static string NpcObjectNameFor(string sceneName)
    {
        switch (sceneName)
        {
            case "Level1_Spring":
            case "Level3_Autumn":
                return "待机关键帧_01";
            case "Level2_Summer":
                return "idle_frame_01";
            case "Level4_Winter":
                return "idle_blink_frame_01";
            default:
                return null;
        }
    }

    private static void Install(Scene scene)
    {
        string npcName = NpcObjectNameFor(scene.name);
        if (string.IsNullOrEmpty(npcName)) return;

        var npc = GameObject.Find(npcName);
        if (npc == null) return;
        if (npc.GetComponent<NpcTalkButton>() != null) return;

        npc.AddComponent<NpcTalkButton>();
    }

    private void Start()
    {
        float offsetY = 1.8f;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float spriteH = sr.sprite.bounds.size.y * Mathf.Abs(transform.lossyScale.y);
            float pivotN = sr.sprite.pivot.y / Mathf.Max(1f, sr.sprite.rect.height);
            offsetY = spriteH * (1f - pivotN) + headGap;
        }

        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure(buttonText, new Vector3(0f, offsetY, 0f), OnTalkClicked,
            FloatingActionButton.VisualStyle.InkPlaque);
        _floatBtn.SetOpacity(buttonOpacity);
        if (WorkshopProgress.TryGetSeason(gameObject.scene.name, out var season) && season == WorkshopSeason.Autumn)
        {
            RefreshAutumnAction();
            WorkshopProgress.Changed += RefreshAutumnAction;
        }

        _solarBtn = gameObject.AddComponent<FloatingActionButton>();
        _solarOffset = new Vector3(0f, offsetY - 0.78f, 0f);
        ConfigureSolarButton();
        _solarBtn.SetOpacity(buttonOpacity);
    }

    private void Update()
    {
        if (_floatBtn == null) return;

        if (WorkshopDialoguePresenter.IsOpen || MiniGameOverlay.IsActive || HarvestOverlayManager.IsHarvestActive ||
            WinterDyeingOverlay.IsActive || WinterGrindingOverlay.IsActive || WorkshopKnowledgeView.IsOpen ||
            AutumnOfferingView.IsOpen ||
            (FeedOverlayManager.Instance != null && FeedOverlayManager.Instance.IsActive))
        {
            if (_near)
            {
                _near = false;
                _floatBtn.Hide();
                _solarBtn.Hide();
            }
            return;
        }

        if (_player == null)
        {
            var playerGo = GameObject.Find("Player");
            if (playerGo != null) _player = playerGo.transform;
        }
        if (_player == null) return;

        bool near = Vector3.Distance(_player.position, transform.position) <= talkDistance;
        if (near == _near) return;

        _near = near;
        if (near)
        {
            _floatBtn.Show();
            _solarBtn.Show();
        }
        else
        {
            _floatBtn.Hide();
            _solarBtn.Hide();
        }
    }

    private void RefreshAutumnAction()
    {
        string label = AutumnNpcDialogue.ActionLabel ?? buttonText;
        if (_floatBtn != null && _floatBtn.buttonText != label) _floatBtn.SetText(label);
    }

    private void OnDestroy() => WorkshopProgress.Changed -= RefreshAutumnAction;

    private void ConfigureSolarButton()
    {
        if (!WorkshopProgress.TryGetSeason(gameObject.scene.name, out var season)) return;
        _solarBtn.Configure(WorkshopProgress.HasSolarTerms(season) ? "读节气卡" : "领节气卡",
            _solarOffset, OnSolarClicked, FloatingActionButton.VisualStyle.PaperTag);
    }

    private void OnSolarClicked()
    {
        if (!WorkshopProgress.TryGetSeason(gameObject.scene.name, out var season)) return;
        bool first = !WorkshopProgress.HasSolarTerms(season);
        WorkshopProgress.UnlockSolarTerms(season);
        ConfigureSolarButton();
        WorkshopKnowledgeView.Show(season, first);
    }

    private void OnTalkClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput) return;

        // 秋天关卡使用正式对话：锤子、糕点通过对话获得
        var autumnDialogue = GetComponent<AutumnNpcDialogue>();
        if (autumnDialogue != null)
        {
            autumnDialogue.Talk();
            return;
        }

        // 冬天关卡使用正式对话：研磨之法 → 寻找研磨棒和钵体
        var winterDialogue = GetComponent<WinterNpcDialogue>();
        if (winterDialogue != null)
        {
            winterDialogue.Talk();
            return;
        }

        if (WorkshopProgress.TryGetSeason(gameObject.scene.name, out var season))
            SpringSummerNpcDialogue.Talk(season);
    }
}
