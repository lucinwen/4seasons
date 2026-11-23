using UnityEngine;
using UnityEngine.UI;

namespace JingweiWeave
{
    /// <summary>
    /// 工具切换：梭子 / 拨子互斥选中，并刷新高亮表现。
    /// </summary>
    public class EmbroideryToolController : MonoBehaviour
    {
        [Header("工具按钮")]
        public Button shuttleButton;
        public Button beaterButton;

        [Header("选中高亮（圆心渐变光晕容器）")]
        public GameObject shuttleHighlight;
        public GameObject beaterHighlight;

        private UnityEngine.UI.Image shuttleCaption;
        private UnityEngine.UI.Image beaterCaption;

        private void Start()
        {
            shuttleCaption = CreateCaption(shuttleButton, "梭子");
            beaterCaption = CreateCaption(beaterButton, "拨子");
            if (shuttleButton != null) shuttleButton.onClick.AddListener(SelectShuttle);
            if (beaterButton != null) beaterButton.onClick.AddListener(SelectBeater);
            Refresh();
        }

        public void SelectShuttle()
        {
            EmbroideryGameManager.Instance.SetTool(EmbroideryTool.Shuttle);
            Refresh();
        }

        public void SelectBeater()
        {
            EmbroideryGameManager.Instance.SetTool(EmbroideryTool.Beater);
            Refresh();
        }

        /// <summary>刷新两个工具的高亮状态，保证只有一个被选中。</summary>
        public void Refresh()
        {
            EmbroideryGameManager gm = EmbroideryGameManager.Instance;
            if (gm == null) return;

            bool shuttle = gm.currentTool == EmbroideryTool.Shuttle;
            if (shuttleHighlight != null) shuttleHighlight.SetActive(shuttle);
            if (beaterHighlight != null) beaterHighlight.SetActive(!shuttle);
            if (shuttleCaption != null) shuttleCaption.color = ToolCaptionColor(shuttle);
            if (beaterCaption != null) beaterCaption.color = ToolCaptionColor(!shuttle);
        }

        private static UnityEngine.UI.Image CreateCaption(UnityEngine.UI.Button button, string label)
        {
            if (button == null) return null;
            var caption = WorkshopUi.Fill(button.transform, "Tool Caption", new Rect(), MinigameUiPresentation.Pine);
            var rect = caption.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(104f, 37f);
            WorkshopUi.ApplyPixelShape(caption);
            WorkshopUi.Label(rect, "Label", new Rect(0f, 0f, 104f, 37f), label, 22,
                MinigameUiPresentation.Paper, TextAnchor.MiddleCenter, WorkshopArt.WorkshopFont);
            return caption;
        }

        private static Color ToolCaptionColor(bool selected)
        {
            return selected ? new Color32(68, 102, 78, 255) : MinigameUiPresentation.Pine;
        }
    }
}
