using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye
{
    /// <summary>左上角收集进度：0/5 文本 + 五个进度圆点。</summary>
    public class CollectionProgressUI : MonoBehaviour
    {
        public Text countText;
        public Text labelText;
        public Image[] dots;

        private void Awake()
        {
            var panel = MinigameUiPresentation.Find<RectTransform>(transform, "CollectPanel");
            if (panel != null) panel.sizeDelta = new Vector2(328f, 130f);
            if (panel != null)
                MinigameUiPresentation.Surface(panel.GetComponent<UnityEngine.UI.Image>(), MinigameUiPresentation.Paper, true);

            var basket = MinigameUiPresentation.Find<RectTransform>(panel, "BasketIcon");
            if (basket != null)
            {
                basket.anchoredPosition = new Vector2(8f, -10f);
                basket.sizeDelta = new Vector2(140f, 110f);
            }

            if (countText != null)
            {
                countText.rectTransform.anchoredPosition = new Vector2(150f, -30f);
                countText.rectTransform.sizeDelta = new Vector2(154f, 48f);
                MinigameUiPresentation.Label(countText, MinigameUiPresentation.Pine, 36);
            }

            if (dots == null) return;
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null) continue;
                dots[i].rectTransform.anchoredPosition = new Vector2(158f + i * 29f, -87f);
                dots[i].rectTransform.sizeDelta = new Vector2(21f, 21f);
            }
        }

        public void Refresh(int count, int max, IReadOnlyList<ItemData> items)
        {
            if (countText != null) countText.text = count + " / " + max;
            if (labelText != null) labelText.text = "已收集";

            if (dots == null) return;
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null) continue;
                if (i < count && items != null && i < items.Count)
                {
                    Color c = DyeColorUtil.Tint(items[i].dyeColor);
                    c.a = 1f;
                    dots[i].color = c;
                    dots[i].transform.localScale = Vector3.one;
                }
                else
                {
                    dots[i].color = new Color32(160, 136, 104, 122);
                    dots[i].transform.localScale = Vector3.one * 0.86f;
                }
            }
        }
    }
}
