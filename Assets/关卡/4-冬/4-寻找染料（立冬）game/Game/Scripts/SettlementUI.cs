using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye
{
    /// <summary>最终结算界面：五件实际收下的物品 + 五种颜色统计。</summary>
    public class SettlementUI : MonoBehaviour
    {
        public GameManager gameManager;
        public CanvasGroup group;
        public RectTransform panel;
        public Text titleText;
        public Text subtitleText;
        public Text summaryText;

        public GameObject[] itemCards;
        public Image[] itemIcons;
        public Text[] itemNames;
        public Text[] itemStates;

        public Text[] statTexts;
        public Button restartButton;

        private bool visible;

        private void Awake()
        {
            StylePresentation();
            if (restartButton != null) restartButton.onClick.AddListener(OnGoToGrinding);
            HideImmediate();
        }

        private void StylePresentation()
        {
            MinigameUiPresentation.Dialog(transform, panel, titleText);
            MinigameUiPresentation.Action(restartButton);

            if (panel != null) panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 710f);
            if (restartButton != null && restartButton.transform is RectTransform actionRect)
                actionRect.anchoredPosition = new Vector2(0f, -572f);

            if (itemCards == null) return;
            foreach (var itemCard in itemCards)
            {
                if (itemCard == null) continue;
                MinigameUiPresentation.Surface(itemCard.GetComponent<UnityEngine.UI.Image>(),
                    MinigameUiPresentation.Linen);
            }
        }

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(OnGoToGrinding);
        }

        private void Update()
        {
            if (!visible) return;
            if (group != null) group.alpha = Mathf.Lerp(group.alpha, 1f, Time.deltaTime * 8f);
            if (panel != null) panel.localScale = Vector3.Lerp(panel.localScale, Vector3.one, Time.deltaTime * 8f);
        }

        public void Show(IReadOnlyList<ItemData> items, DyeCollectionManager history)
        {
            visible = true;
            if (titleText != null) titleText.text = "本次采集成果";
            if (subtitleText != null)
            {
                bool showSubtitle = history == null || summaryText == null;
                subtitleText.gameObject.SetActive(showSubtitle);
                if (showSubtitle) subtitleText.text = $"收下 {items?.Count ?? 0} 件染料";
            }

            if (summaryText != null && history != null)
            {
                summaryText.text = "共采集 " + history.TotalHarvests + " 次　·　收下 " + history.AcceptedCount
                                   + " 件　·　丢弃 " + history.DiscardedCount + " 次";
            }

            for (int i = 0; i < 5; i++)
            {
                bool has = items != null && i < items.Count;
                if (itemCards != null && i < itemCards.Length && itemCards[i] != null)
                {
                    itemCards[i].SetActive(has);
                }
                if (!has) continue;

                ItemData item = items[i];
                if (itemIcons != null && i < itemIcons.Length && itemIcons[i] != null)
                {
                    itemIcons[i].sprite = item.icon;
                    itemIcons[i].enabled = item.icon != null;
                }
                if (itemNames != null && i < itemNames.Length && itemNames[i] != null)
                {
                    itemNames[i].text = item.chineseName;
                }
                if (itemStates != null && i < itemStates.Length && itemStates[i] != null)
                {
                    itemStates[i].text = item.stateText;
                    itemStates[i].color = item.condition == DyeCondition.Normal
                        ? new Color32(45, 91, 56, 255)
                        : new Color32(125, 65, 45, 255);
                }
            }

            // 隐藏五种颜色好坏统计文字
            if (statTexts != null)
            {
                for (int i = 0; i < statTexts.Length; i++)
                {
                    if (statTexts[i] != null) statTexts[i].gameObject.SetActive(false);
                }
            }

            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            if (panel != null) panel.localScale = Vector3.one * 0.94f;
            gameObject.SetActive(true);
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

        private void OnGoToGrinding()
        {
            if (gameManager != null) gameManager.GoToGrinding();
        }
    }
}
