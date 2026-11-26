using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 秋天关卡祭祀供台。玩家靠近时可查看贡品，集齐后提交。
/// 前置条件：蚕花酒、香蕉和糕点。
/// 按钮与其它场景共用 FloatingActionButton，尺寸/字体一致。
/// 触发点在运行时创建，不修改场景文件。
/// </summary>
public class AutumnRitualTrigger : MonoBehaviour
{
    [Tooltip("玩家离仪式点多近时显示悬浮按钮（世界单位）")]
    public float triggerDistance = 3f;

    private FloatingActionButton _floatBtn;
    private Transform _player;
    private Collider2D _triggerCollider;
    private string _label;
    private bool _shown;

    private Vector2 TriggerCenter => _triggerCollider != null
        ? (Vector2)_triggerCollider.bounds.center
        : (Vector2)transform.position;

    private Vector3 ActionOffset =>
        (Vector3)(TriggerCenter - (Vector2)transform.position) + new Vector3(0.5f, 2f, 0f);

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider2D>();
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _label = OfferingLabel();
        _floatBtn.Configure(_label, ActionOffset, OnRitualClicked);
        _floatBtn.SetOpacity(0.9f);
    }

    private void Start()
    {
        var go = GameObject.Find("Player");
        if (go != null) _player = go.transform;
    }

    private void Update()
    {
        if (_player == null) return;

        _floatBtn.offset = ActionOffset;
        bool inRange = !WorkshopQuestGuide.BlocksWorldInput && !WorkshopQuestGuide.IsWorldOverlayActive &&
            Vector2.Distance(_player.position, TriggerCenter) <= triggerDistance;

        if (inRange)
        {
            string label = OfferingLabel();
            if (label != _label)
            {
                _label = label;
                _floatBtn.SetText(label);
            }
        }
        if (inRange == _shown) return;

        _shown = inRange;
        if (inRange) _floatBtn.Show();
        else _floatBtn.Hide();
    }

    private void OnRitualClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive) return;
        AutumnOfferingView.Show(() => AutumnNpcDialogue.IntroduceRitual(SubmitOfferings));
    }

    private static string OfferingLabel()
    {
        if (AutumnLevelState.HasRitualDone) return "查看祭祀供台";
        int count = AutumnLevelState.OfferingCount;
        return count == 3 ? "提交贡品" : $"查看贡品 {count}/3";
    }

    private static void SubmitOfferings()
    {
        if (AutumnLevelState.HasRitualDone || AutumnLevelState.OfferingCount != 3) return;

        AutumnLevelState.HasRitualDone = true;
        AutumnLevelState.HasStatue = true;
        WorkshopProgress.Award(WorkshopItem.Statue);
        MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.ThanksgivingRitual, () =>
            SimplePopup.ShowItem(WorkshopItem.Statue,
                "土地公是乡间守护神，寄托着蚕农祈求桑树繁茂、丰收与四时风调雨顺的美好愿望。",
                null, fallbackIcon: AutumnItemIcons.Load()?.statue));
    }

    private void OnDestroy()
    {
        if (_floatBtn != null) _floatBtn.Hide();
    }
}
