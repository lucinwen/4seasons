using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye.ClothCutting
{
    /// <summary>
    /// 裁衣小游戏 UI：进度条填充、收集栏衣片显示、按钮、通关提示。
    /// 三片衣片：前衣片、后衣片、袖片。
    /// </summary>
    public class ClothCuttingUI : MonoBehaviour
    {
        [Header("进度条")]
        public Image progressBarBg;
        public Image progressBarFill;
        public Text progressText;

        [Header("收集栏")]
        public Image collectionBg;
        public RectTransform collectionFrontSlot;
        public RectTransform collectionBackSlot;
        public RectTransform collectionSleeveSlot;
        public Text collectionCountText;

        [Header("按钮")]
        public Button resetButton;
        public Button closeButton;

        [Header("通关弹窗")]
        public GameObject completePanel;
        public Button continueButton;
        public Text completeText;

        private static Sprite progressFillSprite;

        private void Start()
        {
            MinigameUiPresentation.Dialog(completePanel, "Body", "Text");
            MinigameUiPresentation.Action(continueButton);
            MinigameUiPresentation.Action(resetButton, false);
            MinigameUiPresentation.Action(closeButton, false);
            MinigameUiPresentation.Label(progressText, MinigameUiPresentation.Paper, 28);
            if (progressBarFill != null)
            {
                progressBarFill.color = new Color32(47, 100, 98, 255);
                // uGUI renders an untextured Image as a quad even with Type.Filled.
                if (progressBarFill.sprite == null)
                {
                    if (progressFillSprite == null)
                    {
                        var white = Texture2D.whiteTexture;
                        progressFillSprite = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height),
                            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                        progressFillSprite.name = "Cutting Progress Fill";
                        progressFillSprite.hideFlags = HideFlags.HideAndDontSave;
                    }
                    progressBarFill.sprite = progressFillSprite;
                }
                progressBarFill.type = Image.Type.Filled;
                progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            }

            var canvas = progressBarBg != null ? progressBarBg.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                var background = MinigameUiPresentation.Find<UnityEngine.UI.Image>(canvas.transform, "Background");
                if (background != null) background.color = new Color32(26, 50, 48, 255);
                MinigameUiPresentation.Label(MinigameUiPresentation.Find<UnityEngine.UI.Text>(canvas.transform, "Title"),
                    MinigameUiPresentation.Paper, 48);
                var title = canvas.transform.Find("Title");
                if (title != null) title.gameObject.SetActive(false);
                if (collectionBg != null) collectionBg.color = Color.white;
                WorkshopImmersive.QuietText(progressText);
                WorkshopImmersive.QuietText(collectionCountText);
            }

            if (completePanel != null) completePanel.SetActive(false);
        }

        public void UpdateProgress(float totalProgress, int doneCount)
        {
            if (progressBarFill != null)
                progressBarFill.fillAmount = Mathf.Clamp01(totalProgress);
            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(totalProgress * 100)}%";
            if (collectionCountText != null)
                collectionCountText.text = $"{doneCount} / 3";
        }

        public RectTransform GetSlot(PieceSlot slot)
        {
            switch (slot)
            {
                case PieceSlot.Front: return collectionFrontSlot;
                case PieceSlot.Back: return collectionBackSlot;
                case PieceSlot.Sleeve: return collectionSleeveSlot;
                default: return null;
            }
        }

        public void ShowComplete(System.Action onContinue)
        {
            if (completePanel != null) completePanel.SetActive(true);
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() =>
                {
                    onContinue?.Invoke();
                    if (completePanel != null) completePanel.SetActive(false);
                });
            }
            if (completeText != null) completeText.text = "裁剪完成！";
        }
    }

    public enum PieceSlot { Front, Back, Sleeve }
}
