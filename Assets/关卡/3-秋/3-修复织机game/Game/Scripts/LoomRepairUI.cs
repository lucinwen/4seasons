using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye.LoomRepair
{
    /// <summary>
    /// 修复织机 UI 控制器：进度条、开始提示、结果文字、完成面板。
    /// </summary>
    public class LoomRepairUI : MonoBehaviour
    {
        [Header("进度条")]
        [Tooltip("总进度填充 Image — type=Filled, FillMethod=Horizontal")]
        public Image progressFill;

        [Tooltip("进度百分比文字")]
        public Text progressText;

        [Header("开始提示")]
        [Tooltip("游戏未开始时显示的提示文字（点击开始或按空格）")]
        public GameObject startHint;

        [Header("结果反馈")]
        [Tooltip("成功/完美/失败文字（动画渐显渐隐）")]
        public Text resultText;

        [Header("完成面板")]
        [Tooltip("织造完成面板（根 GameObject）")]
        public GameObject completePanel;

        [Tooltip("完成面板上的继续按钮")]
        public Button continueButton;

        [Tooltip("完成面板标题文字")]
        public Text completeText;

        [Header("织机引用（可选，用于动画）")]
        public Image loomImage;

        [Tooltip("三阶段样式：3-织机-1、3-织机-2、3-织机-3")]
        public Sprite[] loomStages = new Sprite[3];

        private void Awake()
        {
            StylePresentation();
            // 初始状态
            if (startHint != null) startHint.SetActive(true);
            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
                resultText.color = new Color(1f, 1f, 1f, 0f);
            }
            if (completePanel != null) completePanel.SetActive(false);
        }

        private void StylePresentation()
        {
            MinigameUiPresentation.Dialog(completePanel, "Body", "Text");
            MinigameUiPresentation.Action(continueButton);
            if (progressText != null)
            {
                var readout = progressText.rectTransform;
                readout.anchoredPosition = new Vector2(0f, 66f);
                readout.sizeDelta = new Vector2(236f, 42f);
                MinigameUiPresentation.Label(progressText, MinigameUiPresentation.Paper, 27);
                WorkshopImmersive.QuietText(progressText);
            }

            if (startHint != null)
                MinigameUiPresentation.Action(startHint.GetComponent<UnityEngine.UI.Button>());

            var canvas = progressText != null ? progressText.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return;
            MinigameUiPresentation.Action(MinigameUiPresentation.Find<UnityEngine.UI.Button>(canvas.transform, "Buttons/ResetBtn"), false);
            MinigameUiPresentation.Action(MinigameUiPresentation.Find<UnityEngine.UI.Button>(canvas.transform, "Buttons/CloseBtn"), false);
        }

        /// <summary>更新进度条（百分比 0-100）</summary>
        public void UpdateProgress(float percent)
        {
            float clamped = Mathf.Clamp01(percent / 100f);

            if (progressFill != null)
            {
                progressFill.fillAmount = clamped;
            }
            if (progressText != null)
            {
                progressText.text = $"织机进度  {Mathf.RoundToInt(percent)}%";
            }
        }

        /// <summary>切换织机形态：0=织机-1，1=织机-2，2 及以上=织机-3。</summary>
        public void SetLoomStage(int stage)
        {
            if (loomImage == null || loomStages == null || loomStages.Length == 0) return;

            int index = Mathf.Clamp(stage, 0, loomStages.Length - 1);
            var sprite = loomStages[index];
            if (sprite != null) loomImage.sprite = sprite;
        }

        /// <summary>显示开始提示</summary>
        public void ShowStartHint()
        {
            if (startHint != null) startHint.SetActive(true);
        }

        /// <summary>隐藏开始提示</summary>
        public void HideStartHint()
        {
            if (startHint != null) startHint.SetActive(false);
        }

        /// <summary>显示结果文字（带渐显）</summary>
        public void ShowResultText(string text, Color color)
        {
            if (resultText == null) return;
            StopAllCoroutines();
            StartCoroutine(FadeResultText(text, color, 1f));
        }

        /// <summary>隐藏结果文字</summary>
        public void HideResultText()
        {
            if (resultText == null) return;
            StopAllCoroutines();
            resultText.color = new Color(resultText.color.r, resultText.color.g, resultText.color.b, 0f);
        }

        private System.Collections.IEnumerator FadeResultText(string text, Color color, float duration)
        {
            resultText.text = text;
            resultText.color = new Color(color.r, color.g, color.b, 0f);
            resultText.gameObject.SetActive(true);

            // 渐入
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / 0.2f);
                resultText.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }

            // 停留
            yield return new WaitForSeconds(0.8f);

            // 渐出
            t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(t / 0.3f);
                resultText.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }

            resultText.color = new Color(color.r, color.g, color.b, 0f);
            resultText.gameObject.SetActive(false);
        }

        /// <summary>显示完成面板</summary>
        public void ShowCompletePanel()
        {
            if (completePanel != null) completePanel.SetActive(true);
        }

        /// <summary>隐藏完成面板</summary>
        public void HideCompletePanel()
        {
            if (completePanel != null) completePanel.SetActive(false);
        }
    }
}
