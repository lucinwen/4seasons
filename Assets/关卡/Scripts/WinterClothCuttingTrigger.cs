using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 冬季裁衣台。用染好的布沿样板裁出衣片。
/// </summary>
public class WinterClothCuttingTrigger : MonoBehaviour
{
    [Header("显示范围")]
    [Tooltip("玩家离裁衣台多近时显示悬浮按钮（世界单位，忽略 Z 轴）")]
    public float triggerDistance = 3.5f;

    [Header("悬浮按钮")]
    public Vector3 buttonOffset = new Vector3(0f, 2.2f, 0f);
    [Range(0f, 1f)]
    public float buttonOpacity = 0.9f;

    [Header("未拥有布匹的提示")]
    public string noClothTitle = "还没有染好的布匹";
    [TextArea]
    public string noClothMessage = "请先完成染色，并选好自己喜欢的衣服颜色。";

    private FloatingActionButton _floatBtn;
    private Transform _player;
    private bool _shown;

    private void Awake()
    {
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure("开始裁衣", buttonOffset, OnStartCuttingClicked);
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

        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive)
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
        if (inRange)
        {
            _floatBtn.SetText(WorkshopProgress.Has(WorkshopItem.CutPieces) ? "小雪裁衣 · 再玩" : "开始裁衣");
            _floatBtn.Show();
        }
        else _floatBtn.Hide();
    }

    private void OnStartCuttingClicked()
    {
        if (!WorkshopProgress.Has(WorkshopItem.DyedCloth))
        {
            SimplePopup.Show(noClothTitle, noClothMessage, "知道了", null);
            return;
        }

        _floatBtn.Hide();
        _shown = false;
        if (WorkshopProgress.Has(WorkshopItem.CutPieces)) OpenCutting();
        else WinterNpcDialogue.IntroduceTailoring(OpenCutting);
    }

    private static void OpenCutting() => MiniGameOverlay.Show("ClothCutting");

    private void OnDestroy()
    {
        if (_floatBtn != null) _floatBtn.Hide();
    }
}
