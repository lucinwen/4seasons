using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Qingming.Harvest
{
    /// <summary>
    /// UI 控制器：采桑操作提示、进度和通关面板。保留旧场景的开场引用以便隐藏。
    /// </summary>
    public class HarvestUIController : MonoBehaviour
    {
        [Header("进度")]
        public Text progressText;

        [Header("开场面板")]
        public GameObject introPanel;
        public Button startButton;

        [Header("通关面板")]
        public GameObject completePanel;
        public Button continueButton;

        private Coroutine unsuitableFeedback;
        private Text controlsHint;
        private bool completionCardShown;

        private void Start()
        {
            HideIntro();
            MinigameUiPresentation.Dialog(completePanel, "Body", "Title");
            MinigameUiPresentation.Action(continueButton);
            BuildControlsHint();

            var progressBackground = progressText != null && progressText.transform.parent != null
                ? progressText.transform.parent.GetComponent<UnityEngine.UI.Image>()
                : null;
            if (progressBackground != null) progressBackground.color = Color.clear;
            WorkshopImmersive.QuietText(progressText);
            MinigameUiPresentation.Label(progressText, MinigameUiPresentation.Paper, 28);
            if (progressBackground != null && progressBackground.name == "ProgressBg")
                progressBackground.rectTransform.sizeDelta = new Vector2(280f, 58f);

            // 用 Start 而非 Awake：覆盖层模式下 AddComponent 后立即触发 Awake，
            // 此时 continueButton 尚未赋值，监听器会注册失败。
            // Start 在下一帧执行，此时引用已由 HarvestOverlayManager 赋值完毕。
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDestroy()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        private void OnContinueClicked()
        {
            var mgr = MulberryHarvestGameManager.Instance;
            if (mgr != null) mgr.ExitGame();
        }

        private void BuildControlsHint()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            controlsHint = WorkshopUi.Label(canvas.transform, "Harvest Controls", new Rect(0, 0, 1180, 54),
                "A / D 或 ← / → 左右移篮 · 接满 8 片嫩叶", 28, MinigameUiPresentation.Paper,
                TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
            var rect = controlsHint.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -42f);
        }

        /// <summary>显示采桑数量与当前目标。</summary>
        public void UpdateProgress(int collected, int total)
        {
            if (unsuitableFeedback != null)
            {
                StopCoroutine(unsuitableFeedback);
                unsuitableFeedback = null;
            }
            if (progressText != null)
            {
                progressText.text = $"嫩叶  {collected} / {total}";
            }
        }

        internal void ShowHarvestNotice(string notice, int collected, int total)
        {
            if (progressText == null) return;
            if (unsuitableFeedback != null) StopCoroutine(unsuitableFeedback);
            progressText.text = notice;
            unsuitableFeedback = StartCoroutine(RestoreProgress(collected, total));
        }

        private IEnumerator RestoreProgress(int collected, int total)
        {
            yield return new WaitForSeconds(0.9f);
            unsuitableFeedback = null;
            UpdateProgress(collected, total);
        }

        public void HideIntro()
        {
            if (introPanel != null) introPanel.SetActive(false);
        }

        public void ShowComplete(int total, bool earnedPage)
        {
            if (completionCardShown) return;
            completionCardShown = true;
            if (completePanel != null) completePanel.SetActive(false);
            if (controlsHint != null) controlsHint.gameObject.SetActive(false);
            if (progressText != null) progressText.gameObject.SetActive(false);
            WorkshopRewardCards.ShowCompletionCard("harvest", OnContinueClicked);
        }

        public void HideComplete()
        {
            completionCardShown = false;
            if (completePanel != null) completePanel.SetActive(false);
            if (controlsHint != null) controlsHint.gameObject.SetActive(true);
            if (progressText != null) progressText.gameObject.SetActive(true);
        }
    }
}
