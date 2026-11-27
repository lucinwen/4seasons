using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Qingming.LevelBackground
{
    public class SimplePopup : MonoBehaviour
    {
        private static readonly Color Dim = new Color32(17, 32, 28, 174);
        private static int openCount;
        internal static bool IsOpen => openCount > 0;
        private Action _onConfirm;
        private Action _onCancel;
        private WorkshopItem? _rewardItem;

        public static SimplePopup ShowItem(WorkshopItem item, string fallbackMessage, Action onConfirm,
            bool showCancel = false, string cancelText = "取消", Action onCancel = null,
            Sprite fallbackIcon = null, Color? fallbackTint = null, Material fallbackMaterial = null,
            Action playSound = null)
        {
            if (!WorkshopRewardCards.TryBeginItemCard(item))
            {
                WorkshopRewardCards.ContinueAfterItemCard(item, onConfirm);
                return null;
            }
            var art = WorkshopArt.ItemCard(item);
            var popup = art != null
                ? ShowCard(item, art, "收下", onConfirm, showCancel, cancelText, onCancel, playSound)
                : ShowMessage("获得" + WorkshopProgress.ItemName(item), fallbackMessage, "收下",
                    onConfirm, showCancel, cancelText, onCancel, fallbackIcon, fallbackTint, fallbackMaterial,
                    playSound);
            popup._rewardItem = item;
            return popup;
        }

        public static SimplePopup Show(string title, string message,
            string confirmText = "确定", Action onConfirm = null,
            bool showCancel = false, string cancelText = "取消", Action onCancel = null,
            Sprite icon = null, Color? iconColor = null, Material iconMaterial = null)
        {
            if (WorkshopArt.TryIdentifyItemCard(icon, out var item))
                return ShowItem(item, message, onConfirm, showCancel, cancelText, onCancel,
                    icon, iconColor, iconMaterial);
            if (HarvestOverlayManager.IsHarvestActive) return null;
            return ShowMessage(title, message, confirmText, onConfirm, showCancel, cancelText,
                onCancel, icon, iconColor, iconMaterial);
        }

        private static SimplePopup ShowMessage(string title, string message, string confirmText,
            Action onConfirm, bool showCancel, string cancelText, Action onCancel,
            Sprite icon, Color? iconColor, Material iconMaterial, Action playSound = null)
        {
            var popup = Create(out var frame, onConfirm, onCancel);
            const float pad = 86f;
            const float bodyTop = 148f;
            const float itemSize = 164f;
            bool hasIcon = icon != null;
            float width = hasIcon ? 840f : 782f;
            float textTop = hasIcon ? bodyTop + itemSize + 21f : bodyTop;
            float textWidth = width - pad * 2f;
            var panel = WorkshopUi.Rect(frame, "Popup Content", new Rect(0, 0, width, 0));
            var copy = BodyLabel(panel, "Message", new Rect(pad, textTop, textWidth, 0), message);
            float contentHeight = Mathf.Max(45f, copy.preferredHeight + 4f);
            float footer = textTop + contentHeight + 27f;
            float height = footer + 122f;
            var season = CurrentSeason();
            var card = CreateCard(frame, panel, title, season, width, height, footer);
            copy.rectTransform.sizeDelta = new Vector2(textWidth, contentHeight);

            if (hasIcon)
            {
                var item = CenteredPicture(panel, "Item",
                    new Rect((width - itemSize) * 0.5f, bodyTop, itemSize, itemSize), icon, iconColor);
                if (iconMaterial != null) item.material = iconMaterial;
            }

            return AddActions(popup, card, width * 0.5f, footer + 20f, season,
                confirmText, showCancel, cancelText, playSound);
        }

        public static SimplePopup ShowRelic(string title, string obtainLine, string knowledge,
            Sprite icon, string confirmText = "收下", Action onConfirm = null)
        {
            return Show(title, obtainLine + "\n\n" + knowledge, confirmText, onConfirm, icon: icon);
        }

        private static SimplePopup ShowCard(WorkshopItem item, Sprite art, string confirmText,
            Action onConfirm, bool showCancel, string cancelText, Action onCancel, Action playSound = null)
        {
            var popup = Create(out var frame, onConfirm, onCancel);
            WorkshopKnowledgeView.DrawItemCard(frame, new Rect(224, 31, 1472, 871), item, art);
            var season = WorkshopProgress.ItemSeason(item);
            return AddActions(popup, frame, 960, 945, season, confirmText, showCancel, cancelText, playSound);
        }

        private static UnityEngine.UI.Image CenteredPicture(Transform parent, string name,
            Rect bounds, Sprite art, Color? tint = null)
        {
            var image = WorkshopUi.Picture(parent, name, bounds, art, tint);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = new Vector2(bounds.center.x, -bounds.center.y);
            return image;
        }

        private static SimplePopup AddActions(SimplePopup popup, RectTransform parent, float centerX,
            float top, WorkshopSeason season, string confirmText, bool showCancel, string cancelText,
            Action playSound)
        {
            const float buttonWidth = 240f;
            const float buttonHeight = 68f;
            if (showCancel)
                WorkshopUi.PixelButton(parent, "Cancel", new Rect(centerX - 232, top, 200, buttonHeight),
                    cancelText, popup.OnCancel, false, 25);
            float left = showCancel ? centerX - 8f : centerX - buttonWidth * 0.5f;
            var confirm = WorkshopUi.Button(parent, "Confirm", new Rect(left, top, buttonWidth, buttonHeight),
                confirmText, popup.OnConfirm, size: 29);
            var buttonArt = WorkshopArt.SeasonButton(season);
            if (buttonArt != null)
            {
                var image = (UnityEngine.UI.Image)confirm.targetGraphic;
                image.sprite = buttonArt;
                image.type = buttonArt.border.sqrMagnitude > 0f
                    ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
                image.color = Color.white;
            }
            (playSound ?? WorkshopAudioFeedback.Notice)();
            return popup;
        }

        private static WorkshopSeason CurrentSeason() =>
            WorkshopProgress.TryGetSeason(SceneManager.GetActiveScene().name, out var season)
                ? season : WorkshopSeason.Spring;

        private static SimplePopup Create(out RectTransform frame, Action onConfirm, Action onCancel)
        {
            frame = WorkshopUi.NewCanvas("SimplePopupCanvas", 9999);
            WorkshopUi.Block(frame, Dim);
            var popup = frame.parent.gameObject.AddComponent<SimplePopup>();
            popup._onConfirm = onConfirm;
            popup._onCancel = onCancel;
            return popup;
        }

        private static RectTransform CreateCard(RectTransform frame, RectTransform panel, string title,
            WorkshopSeason season, float width, float height, float footer)
        {
            var card = WorkshopUi.Panel(frame, "Popup Frame",
                new Rect((1920 - width) * 0.5f, (1080 - height) * 0.5f, width, height),
                WorkshopArt.SeasonPanel(season) ?? WorkshopArt.WorkshopTaskBoard).rectTransform;
            WorkshopUi.Label(card, "Title", new Rect(86, 72, width - 172, 63), title,
                40, WorkshopUi.Ink, TextAnchor.MiddleCenter);
            WorkshopUi.Fill(card, "Footer Rule", new Rect(86, footer, width - 172, 1), WorkshopUi.Line);
            panel.SetParent(card, false);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(width, height);
            return card;
        }

        private static UnityEngine.UI.Text BodyLabel(Transform parent, string name, Rect bounds, string value)
        {
            var label = WorkshopUi.Label(parent, name, bounds, value, 27, WorkshopUi.Ink,
                TextAnchor.UpperCenter, WorkshopArt.WorkshopFont);
            label.lineSpacing = 1.28f;
            return label;
        }

        private void OnEnable() => openCount++;
        private void OnDisable() => openCount--;

        public void OnConfirm()
        {
            if (_rewardItem.HasValue)
            {
                var item = _rewardItem.Value;
                _rewardItem = null;
                WorkshopRewardCards.FinishItemCard(item, () => Close(_onConfirm));
                return;
            }
            Close(_onConfirm);
        }

        public void OnCancel()
        {
            ReleaseReward();
            Close(_onCancel);
        }

        private void ReleaseReward()
        {
            if (_rewardItem.HasValue) WorkshopRewardCards.ReleaseItemCard(_rewardItem.Value);
            _rewardItem = null;
        }

        private void OnDestroy() => ReleaseReward();

        private void Close(Action action)
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            Destroy(gameObject);
            action?.Invoke();
        }
    }
}
