using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Qingming.LevelBackground;

namespace JiangnanDye.MazeGame
{
    /// <summary>
    /// 白色线头奖励弹窗。负责显示/隐藏。
    /// 本体 RewardPopup 永远 active，只切换 Body 子物体的显隐。
    /// </summary>
    public class MazeRewardPopup : MonoBehaviour
    {
        [Header("引用")]
        public GameObject rootPanel;    // 绑定 RewardPopup 下的 Body 子物体
        public Image itemIconImage;
        public Button finishButton;

        [Header("配置")]
        public float animDuration = 0.2f;

        private RectTransform rootRt;
        private Transform dim;

        private void Awake()
        {
            if (rootPanel != null) rootRt = rootPanel.GetComponent<RectTransform>();
            dim = transform.Find("Dim");
            MinigameUiPresentation.Dialog(transform, rootRt,
                MinigameUiPresentation.Find<UnityEngine.UI.Text>(rootRt, "Title"));
            MinigameUiPresentation.Action(finishButton);
            if (finishButton != null)
                finishButton.onClick.AddListener(OnFinishClicked);

            // 默认隐藏 Body（RewardPopup 本体保持 active 不关闭）
            Hide();
        }

        public void Show()
        {
            WorkshopProgress.Award(WorkshopItem.Spool);
            // Finding the tool now ends with its corresponding Xiaoman craft card.
            // Keep Spool in the inventory without a second item-knowledge reward.
            WorkshopRewardCards.FinishItemCard(WorkshopItem.Spool);
            WorkshopRewardCards.ShowTermReward(WorkshopSeason.Summer, 1, OnFinishClicked);
        }

        public void Hide()
        {
            StopAllCoroutines();
            if (dim != null) dim.gameObject.SetActive(false);
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        private System.Collections.IEnumerator ScaleIn()
        {
            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animDuration);
                float s = Mathf.Lerp(0.9f, 1.0f, t);
                rootRt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rootRt.localScale = Vector3.one;
        }

        private void OnFinishClicked()
        {
            Hide();
            WorkshopProgress.Award(WorkshopItem.Spool);
            if (MiniGameOverlay.CloseIfActive(gameObject.scene)) return;
            SceneManager.LoadScene("Level2_Summer");
        }
    }
}
