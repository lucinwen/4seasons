using UnityEngine;
using UnityEngine.UI;

namespace Qingming.Harvest
{
    /// <summary>
    /// UI 控制器：顶部进度数字、开场介绍、通关面板、按钮回调。
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

        private void Start()
        {
            MinigameUiPresentation.Dialog(introPanel, "Body", "Title");
            MinigameUiPresentation.Dialog(completePanel, "Body", "Title");
            MinigameUiPresentation.Action(startButton);
            MinigameUiPresentation.Action(continueButton);

            var progressBackground = progressText != null && progressText.transform.parent != null
                ? progressText.transform.parent.GetComponent<UnityEngine.UI.Image>()
                : null;
            MinigameUiPresentation.Surface(progressBackground, MinigameUiPresentation.Pine, true);
            MinigameUiPresentation.Label(progressText, MinigameUiPresentation.Paper, 28);
            if (progressBackground != null && progressBackground.name == "ProgressBg")
                progressBackground.rectTransform.sizeDelta = new Vector2(280f, 58f);

            // 用 Start 而非 Awake：覆盖层模式下 AddComponent 后立即触发 Awake，
            // 此时 startButton/continueButton 尚未赋值，监听器会注册失败。
            // Start 在下一帧执行，此时引用已由 HarvestOverlayManager 赋值完毕。
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        private void OnStartClicked()
        {
            var mgr = MulberryHarvestGameManager.Instance;
            if (mgr != null) mgr.StartGame();
        }

        private void OnContinueClicked()
        {
            var mgr = MulberryHarvestGameManager.Instance;
            if (mgr != null) mgr.ExitGame();
        }

        /// <summary>显示采桑数量与当前目标。</summary>
        public void UpdateProgress(int collected, int total)
        {
            if (progressText != null)
            {
                progressText.text = $"桑叶  {collected} / {total}";
            }
        }

        public void ShowIntro()
        {
            if (introPanel != null) introPanel.SetActive(true);
            if (completePanel != null) completePanel.SetActive(false);
        }

        public void HideIntro()
        {
            if (introPanel != null) introPanel.SetActive(false);
        }

        public void ShowComplete()
        {
            if (completePanel != null) completePanel.SetActive(true);
        }

        public void HideComplete()
        {
            if (completePanel != null) completePanel.SetActive(false);
        }
    }
}
