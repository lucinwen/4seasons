using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye
{
    /// <summary>游戏开场介绍：展示五种染料外观 + 玩法说明，点击开始进入探索。</summary>
    public class IntroUI : MonoBehaviour
    {
        public GameManager gameManager;
        public CanvasGroup group;
        public RectTransform panel;
        public Text titleText;
        public Text introText;

        public Image[] dyeIcons;
        public Text[] dyeNames;

        public Button startButton;

        private void Awake()
        {
            StylePresentation();
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            HideImmediate();
        }

        private void StylePresentation()
        {
            MinigameUiPresentation.Dialog(transform, panel, titleText);
            MinigameUiPresentation.Action(startButton);
            if (introText != null)
            {
                introText.text = "秋日染坊需要五份健康的天然染料。\n\n"
                    + "用 WASD 或方向键走到植物旁，点击右下角的锄头采集。\n"
                    + "坏损染料可以丢弃，收下五件健康染料即可完成。";
                introText.lineSpacing = 1.25f;
            }

            if (panel != null) panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 890f);
            var dyeRow = MinigameUiPresentation.Find<RectTransform>(panel, "DyeRow");
            if (dyeRow != null) dyeRow.anchoredPosition = new Vector2(0f, -426f);

            if (startButton != null && startButton.transform is RectTransform actionRect)
            {
                actionRect.anchoredPosition = new Vector2(0f, -740f);
                actionRect.sizeDelta = new Vector2(320f, 90f);
            }

            if (dyeNames == null) return;
            foreach (var dyeName in dyeNames)
            {
                if (dyeName == null || dyeName.transform.parent == null) continue;
                MinigameUiPresentation.Surface(dyeName.transform.parent.GetComponent<UnityEngine.UI.Image>(),
                    MinigameUiPresentation.Pine);
                MinigameUiPresentation.Label(dyeName, MinigameUiPresentation.Paper);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStart);
        }

        public void Show()
        {
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            gameObject.SetActive(false);
        }

        private void OnStart()
        {
            Hide();
            if (gameManager != null) gameManager.StartRound();
        }
    }
}
