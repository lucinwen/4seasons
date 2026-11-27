using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye
{
    /// <summary>收下 / 丢弃弹窗。</summary>
    public class ItemRewardPopup : MonoBehaviour
    {
        [Header("引用")]
        public GameManager gameManager;

        [Header("显示元素")]
        public CanvasGroup group;
        public RectTransform panel;
        public Image icon;
        public Text titleText;
        public Text nameText;
        public Text plantText;
        public Text descText;
        public Text stateText;
        public Image stateChip;
        public Button acceptButton;
        public Button discardButton;

        private bool visible;

        public bool Visible
        {
            get { return visible; }
        }

        private void Awake()
        {
            MinigameUiPresentation.Dialog(transform, panel, titleText);
            MinigameUiPresentation.Action(acceptButton);
            MinigameUiPresentation.Action(discardButton, false);
            MinigameUiPresentation.Label(plantText, MinigameUiPresentation.Pine, 52);
            MinigameUiPresentation.Label(stateText, MinigameUiPresentation.Pine, 30);
            MinigameUiPresentation.Surface(stateChip, MinigameUiPresentation.Pine);
            AddContentBackground();
            ColorButtons();
            HideImmediate();
            if (acceptButton != null) acceptButton.onClick.AddListener(OnAccept);
            if (discardButton != null) discardButton.onClick.AddListener(OnDiscard);
        }

        private void AddContentBackground()
        {
            if (panel == null) return;

            // 在 panel 最底层插入米色背景，覆盖整个弹窗内容区
            var bg = new GameObject("ContentBackground", typeof(RectTransform), typeof(Image))
                .GetComponent<Image>();
            var bgRect = bg.rectTransform;
            bgRect.SetParent(panel, false);
            bgRect.SetAsFirstSibling();

            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            bg.raycastTarget = false;
            bg.color = MinigameUiPresentation.Paper;

            // 文本颜色
            SetTextBlack(titleText);
            SetTextBlack(nameText);
            SetTextBlack(plantText);
            SetTextBlack(descText);
            if (stateText != null) stateText.color = Color.white;
        }

        private static void SetTextBlack(Text text)
        {
            if (text != null) text.color = Color.black;
        }

        private void ColorButtons()
        {
            SetButtonColor(acceptButton, MinigameUiPresentation.Pine);
            SetButtonColor(discardButton, new Color32(136, 69, 49, 255));
        }

        private static void SetButtonColor(Button button, Color color)
        {
            if (button == null) return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null) return;
            image.color = color;
            // 让文字可读
            foreach (var text in button.GetComponentsInChildren<Text>(true))
                text.color = MinigameUiPresentation.Paper;
        }

        private void OnDestroy()
        {
            if (acceptButton != null) acceptButton.onClick.RemoveListener(OnAccept);
            if (discardButton != null) discardButton.onClick.RemoveListener(OnDiscard);
        }

        public void Show(ItemData item)
        {
            if (item == null) return;
            visible = true;

            if (titleText != null) titleText.text = "采集成功";
            if (nameText != null) nameText.text = item.chineseName;
            if (plantText != null) plantText.text = item.plantName;
            if (descText != null) descText.text = item.description;
            if (stateText != null) stateText.text = "状态：" + item.stateText;
            if (icon != null)
            {
                icon.sprite = item.icon;
                icon.preserveAspect = true;
                icon.enabled = item.icon != null;
            }
            if (stateChip != null)
            {
                stateChip.color = item.condition == DyeCondition.Normal
                    ? new Color32(64, 99, 70, 255)
                    : new Color32(136, 69, 49, 255);
            }

            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            if (panel != null) panel.localScale = Vector3.one * 0.92f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!visible) return;
            if (group != null) group.alpha = Mathf.Lerp(group.alpha, 1f, Time.deltaTime * 14f);
            if (panel != null) panel.localScale = Vector3.Lerp(panel.localScale, Vector3.one, Time.deltaTime * 14f);
        }

        public void OnAccept()
        {
            if (!visible) return;
            if (gameManager != null) gameManager.AcceptPending();
        }

        public void OnDiscard()
        {
            if (!visible) return;
            if (gameManager != null) gameManager.DiscardPending();
        }

        public void Hide()
        {
            HideImmediate();
        }

        private void HideImmediate()
        {
            visible = false;
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }
    }
}
