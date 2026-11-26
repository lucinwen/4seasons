using System.Collections;
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

    private IEnumerator Start()
    {
        // Winter's scene installer turns the authored trigger floors solid in
        // Start. Wait for every scene Start (and the first sprite pose) before
        // finding the supporting floor, independent of component start order.
        yield return null;
        MoveNearEntrance();
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

    /// <summary>Keep the guide within a short walk of the authored entrance.</summary>
    private void MoveNearEntrance()
    {
        var player = FindScenePlayer(gameObject.scene);
        if (player == null) return;
        _player = player;
        const float entranceDistance = 4.2f;
        float horizontalGap = transform.position.x - player.position.x;
        if (Mathf.Abs(horizontalGap) <= entranceDistance) return;

        Vector3 position = transform.position;
        position.x = player.position.x + Mathf.Sign(horizontalGap) * entranceDistance;
        var sprite = GetComponent<SpriteRenderer>();
        float footOffset = sprite != null ? sprite.bounds.min.y - transform.position.y : -1f;
        // Floor colliders have no renderer; decorated props and the player must not
        // become an accidental perch for a relocated guide.
        Physics2D.SyncTransforms();
        var hits = Physics2D.RaycastAll(new Vector2(position.x, player.position.y + 3f), Vector2.down, 30f);
        float floor = float.NegativeInfinity;
        foreach (var hit in hits)
        {
            var collider = hit.collider;
            if (collider == null || collider.isTrigger || collider.gameObject.scene != gameObject.scene ||
                collider.attachedRigidbody != null || collider.GetComponent<Renderer>() != null ||
                hit.normal.y < .45f || hit.point.y > player.position.y + 2f) continue;
            floor = Mathf.Max(floor, hit.point.y);
        }
        if (!float.IsNegativeInfinity(floor)) position.y = floor - footOffset + .03f;
        else
        {
            var playerSprite = player.GetComponentInChildren<SpriteRenderer>();
            float playerFeet = playerSprite != null ? playerSprite.bounds.min.y : player.position.y - .5f;
            position.y = playerFeet - footOffset;
        }
        transform.position = position;
    }

    private static Transform FindScenePlayer(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var player = root.GetComponentInChildren<LevelPlayerController>(true);
            if (player != null) return player.transform;
        }
        return null;
    }

    private void Update()
    {
        if (_floatBtn == null) return;

        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive ||
            WorkshopDialoguePresenter.IsOpen || MiniGameOverlay.IsActive || HarvestOverlayManager.IsHarvestActive ||
            WinterDyeingOverlay.IsActive || WinterGrindingOverlay.IsActive || WorkshopKnowledgeView.IsOpen ||
            AutumnOfferingView.IsOpen)
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
        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive) return;
        if (!WorkshopProgress.TryGetSeason(gameObject.scene.name, out var season)) return;
        bool first = !WorkshopProgress.HasSolarTerms(season);
        WorkshopProgress.UnlockSolarTerms(season);
        ConfigureSolarButton();
        WorkshopKnowledgeView.Show(season, first);
    }

    private void OnTalkClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput) return;

        // 秋天关卡首次交谈即送糕点；修机锤在场景中单独拾取
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
