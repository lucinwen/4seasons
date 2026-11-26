using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 冬季裁衣台按当前进度进入剪帛、纸样、缝衣或试衣，已完成工序不必重做。
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
        _floatBtn.Configure("立冬小雪 · 剪帛", buttonOffset, OnStartCuttingClicked);
        _floatBtn.SetOpacity(buttonOpacity);
        WorkshopPickupGlow.Bind(gameObject, () => WinterLevelState.HasDyedCloth &&
            WorkshopWordRules.WinterStage != WorkshopWinterStage.Complete);
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

        _floatBtn.SetText(ActionLabel);
        bool inRange = Vector2.Distance(_player.position, transform.position) <= triggerDistance;
        if (inRange == _shown) return;

        _shown = inRange;
        if (inRange)
        {
            _floatBtn.Show();
        }
        else _floatBtn.Hide();
    }

    private static string ActionLabel
    {
        get
        {
            switch (WorkshopWordRules.WinterStage)
            {
                case WorkshopWinterStage.Cutting: return "立冬小雪 · 剪帛";
                case WorkshopWinterStage.Fitting: return "小雪 · 量体裁衣";
                case WorkshopWinterStage.Needle: return "缝衣前先取针";
                case WorkshopWinterStage.Sewing: return "小雪 · 选款缝衣";
                default: return "锦衣堂 · 试衣";
            }
        }
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
        switch (WorkshopWordRules.WinterStage)
        {
            case WorkshopWinterStage.Cutting:
                WinterNpcDialogue.IntroduceTailoring(OpenXiaoxue);
                break;
            case WorkshopWinterStage.Fitting:
                WorkshopSimpleGames.Show(WorkshopSimpleGame.Fitting, ContinueToSewing);
                break;
            case WorkshopWinterStage.Needle:
                SimplePopup.Show("还差缝衣针", "版型已经确认，不用再拖纸样。收下缝素堂里发光的缝衣针，再来选款缝衣。", "知道了");
                break;
            case WorkshopWinterStage.Sewing:
                WorkshopTailor.Show(null, null);
                break;
            case WorkshopWinterStage.Complete:
                WorkshopWardrobe.Show();
                break;
        }
    }

    private static void ContinueToSewing()
    {
        if (WorkshopWordRules.WinterStage == WorkshopWinterStage.Sewing && WorkshopProgress.CanSubmit(WorkshopSeason.Winter))
            WorkshopTailor.Show(null, null);
    }

    private static void OpenXiaoxue() => MiniGameOverlay.Show("ClothCutting");

    private void OnDestroy()
    {
        if (_floatBtn != null) _floatBtn.Hide();
    }
}
