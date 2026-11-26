using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 冬天关卡「染料园」触发点：玩家靠近时显示悬浮按钮「进入染料园」。
/// 前置条件：已拾取锄头。
/// - 已拥有锄头 → 直接进入寻找染料小游戏（MainScene）
/// - 未拥有锄头 → 提示需要锄头
/// 按钮与其它场景共用 FloatingActionButton，尺寸/字体一致。
/// </summary>
public class WinterDyeGardenTrigger : MonoBehaviour
{
    [Header("显示范围")]
    [Tooltip("玩家离触发点多近时显示悬浮按钮（世界单位，忽略 Z 轴）")]
    public float triggerDistance = 3.5f;

    [Header("悬浮按钮")]
    public string buttonText = "进入染料园";
    public Vector3 buttonOffset = new Vector3(0f, 2.2f, 0f);
    [Range(0f, 1f)]
    public float buttonOpacity = 0.9f;

    [Header("未获得锄头的提示")]
    public string noHoeTitle = "需要锄头";
    [TextArea]
    public string noHoeMessage = "请先拾取锄头，再进入染料园挖掘染料。";

    [Header("寻找染料小游戏场景名")]
    public string dyeGameSceneName = "MainScene";

    private FloatingActionButton _floatBtn;
    private Transform _player;
    private Collider2D _triggerCollider;
    private bool _shown;

    private Vector2 TriggerCenter => _triggerCollider != null
        ? (Vector2)_triggerCollider.bounds.center
        : (Vector2)transform.position;

    private Vector3 ActionOffset => buttonOffset + (Vector3)(TriggerCenter - (Vector2)transform.position);

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider2D>();
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure(buttonText, ActionOffset, OnEnterClicked);
        _floatBtn.SetOpacity(buttonOpacity);
    }

    private void Update()
    {
        if (_player == null)
        {
            var go = GameObject.Find("Player");
            if (go == null) return;
            _player = go.transform;
        }

        // 小游戏叠加层打开期间不显示场景悬浮按钮
        if (MiniGameOverlay.IsActive)
        {
            if (_shown)
            {
                _shown = false;
                _floatBtn.Hide();
            }
            return;
        }

        _floatBtn.offset = ActionOffset;
        bool inRange = Vector2.Distance(_player.position, TriggerCenter) <= triggerDistance;
        if (inRange == _shown) return;

        _shown = inRange;
        if (inRange) _floatBtn.Show();
        else _floatBtn.Hide();
    }

    private void OnEnterClicked()
    {
        if (!WinterLevelState.HasHoe && !WorkshopProgress.HasStep("autumn.hoe"))
        {
            SimplePopup.Show(noHoeTitle, noHoeMessage, "知道了", null);
            return;
        }

        WinterLevelState.HasHoe = true;
        _floatBtn.Hide();
        _shown = false;

        if (WorkshopProgress.HasStep("winter.dyes") || WinterLevelState.HasDyedCloth)
            MiniGameOverlay.ShowMap(dyeGameSceneName);
        else
            WinterNpcDialogue.IntroduceDyeMaterials(() => MiniGameOverlay.ShowMap(dyeGameSceneName));
    }

    private void OnDestroy()
    {
        if (_floatBtn != null) _floatBtn.Hide();
    }
}
