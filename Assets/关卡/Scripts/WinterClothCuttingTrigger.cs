using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 冬天关卡「裁衣台」触发点：玩家靠近时显示悬浮按钮「开始裁衣」。
/// 前置条件：已完成染色并选好衣服颜色，即已拥有染好颜色的布匹。
/// - 已拥有布匹 → 进入裁衣小游戏（ClothCutting）
/// - 未拥有布匹 → 提示先完成染色并选颜色
/// 按钮与其它场景共用 FloatingActionButton，尺寸/字体一致。
/// </summary>
public class WinterClothCuttingTrigger : MonoBehaviour
{
    [Header("显示范围")]
    [Tooltip("玩家离裁衣台多近时显示悬浮按钮（世界单位，忽略 Z 轴）")]
    public float triggerDistance = 3.5f;

    [Header("悬浮按钮")]
    public string buttonText = "开始裁衣";
    public Vector3 buttonOffset = new Vector3(0f, 2.2f, 0f);
    [Range(0f, 1f)]
    public float buttonOpacity = 0.9f;

    [Header("未拥有布匹的提示")]
    public string noClothTitle = "还没有染好的布匹";
    [TextArea]
    public string noClothMessage = "请先完成染色，并选好自己喜欢的衣服颜色。";

    [Header("裁衣小游戏场景名")]
    public string clothCuttingSceneName = "ClothCutting";

    private FloatingActionButton _floatBtn;
    private Transform _player;
    private bool _shown;

    private void Awake()
    {
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure(buttonText, buttonOffset, OnStartCuttingClicked);
        _floatBtn.SetOpacity(buttonOpacity);
        WorkshopPickupGlow.Bind(gameObject, () => WinterLevelState.HasDyedCloth &&
            !WorkshopProgress.Has(WorkshopItem.CutPieces));
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

        bool inRange = Vector2.Distance(_player.position, transform.position) <= triggerDistance;
        if (inRange == _shown) return;

        _shown = inRange;
        if (inRange) _floatBtn.Show();
        else _floatBtn.Hide();
    }

    private void OnStartCuttingClicked()
    {
        if (!WinterLevelState.HasDyedCloth)
        {
            SimplePopup.Show(noClothTitle, noClothMessage, "知道了", null);
            return;
        }

        _floatBtn.Hide();
        _shown = false;
        Debug.Log("[WinterClothCuttingTrigger] 已拥有染好颜色的布匹，叠加打开裁衣小游戏 " + clothCuttingSceneName);
        if (WorkshopProgress.Has(WorkshopItem.CutPieces)) MiniGameOverlay.Show(clothCuttingSceneName);
        else WinterNpcDialogue.IntroduceTailoring(() => MiniGameOverlay.Show(clothCuttingSceneName));
    }

    private void OnDestroy()
    {
        if (_floatBtn != null) _floatBtn.Hide();
    }
}
