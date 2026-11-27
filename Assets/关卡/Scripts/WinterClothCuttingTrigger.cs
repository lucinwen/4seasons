using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 冬季裁衣台先排布再剪帛，之后按进度进入缝衣或试衣。
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
    private FloatingActionButton _replayButton;
    private Transform _player;
    private bool _shown;

    private void Awake()
    {
        _floatBtn = gameObject.AddComponent<FloatingActionButton>();
        _floatBtn.Configure(ActionLabel, buttonOffset, OnStartCuttingClicked);
        _floatBtn.SetOpacity(buttonOpacity);
        _replayButton = gameObject.AddComponent<FloatingActionButton>();
        _replayButton.Configure("重玩晾布排料", buttonOffset + Vector3.up * .85f, OnReplayPreparationClicked);
        _replayButton.SetOpacity(buttonOpacity);
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
            HideActions();
            return;
        }

        _floatBtn.SetText(ActionLabel);
        bool inRange = Vector2.Distance(_player.position, transform.position) <= triggerDistance;
        bool canReplay = inRange && WorkshopProgress.Has(WorkshopItem.DyedCloth) && WorkshopProgress.Has(WorkshopItem.CutPieces);
        if (canReplay && !_replayButton.IsVisible) _replayButton.Show();
        else if (!canReplay && _replayButton.IsVisible) _replayButton.Hide();
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
                case WorkshopWinterStage.Preparing: return "小雪 · 排布";
                case WorkshopWinterStage.Cutting: return "立冬小雪 · 剪帛";
                case WorkshopWinterStage.Needle: return "缝衣前先取针";
                case WorkshopWinterStage.EggSheet: return "缝衣前先领蚕连";
                case WorkshopWinterStage.Sewing: return "小雪 · 选款缝衣";
                default: return "锦衣堂 · 试衣";
            }
        }
    }

    private void OnStartCuttingClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive) return;
        if (!WorkshopProgress.Has(WorkshopItem.DyedCloth))
        {
            SimplePopup.Show(noClothTitle, noClothMessage, "知道了", null);
            return;
        }

        HideActions();
        switch (WorkshopWordRules.WinterStage)
        {
            case WorkshopWinterStage.Preparing:
                WinterNpcDialogue.IntroduceTailoring(() => WorkshopProcessCardView.Show(WorkshopProcessCard.Lidong,
                    () => WorkshopObservationView.ShowWinterPreparation(OpenCutting)));
                break;
            case WorkshopWinterStage.Cutting:
                OpenCutting();
                break;
            case WorkshopWinterStage.Needle:
                SimplePopup.Show("还差缝衣针", "三块衣片已经剪好。收下缝素堂里发光的缝衣针。", "知道了");
                break;
            case WorkshopWinterStage.EggSheet:
                SimplePopup.Show("还差蚕连", "与裁衣师傅对话，收下蚕连，再来选款缝衣。", "知道了");
                break;
            case WorkshopWinterStage.Sewing:
                WorkshopProcessCardView.Show(WorkshopProcessCard.Xiaoxue, () => WorkshopTailor.Show(null, null));
                break;
            case WorkshopWinterStage.Complete:
                WorkshopWardrobe.Show();
                break;
        }
    }

    private static void OpenCutting() =>
        MiniGameOverlay.Show("ClothCutting");

    private void OnReplayPreparationClicked()
    {
        if (WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive) return;
        HideActions();
        WorkshopProcessCardView.Show(WorkshopProcessCard.Lidong,
            () => WorkshopObservationView.ShowWinterPreparation());
    }

    private void HideActions()
    {
        if (_shown) _floatBtn.Hide();
        if (_replayButton.IsVisible) _replayButton.Hide();
        _shown = false;
    }

    private void OnDestroy()
    {
        if (_floatBtn != null) _floatBtn.Hide();
        if (_replayButton != null) _replayButton.Hide();
    }
}
