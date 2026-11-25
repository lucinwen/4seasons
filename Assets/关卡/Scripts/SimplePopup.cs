using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Qingming.LevelBackground
{
    public class SimplePopup : MonoBehaviour
    {
        private static readonly Color Dim = new Color32(17, 32, 28, 174);
        private static readonly Color[] ChapterAccents =
        {
            new Color32(43, 72, 43, 255),
            new Color32(27, 78, 78, 255),
            new Color32(125, 52, 38, 255),
            new Color32(45, 61, 96, 255)
        };
        private static int openCount;
        internal static bool IsOpen => openCount > 0;
        private Action _onConfirm;
        private Action _onCancel;

        public static SimplePopup ShowItem(WorkshopItem item, string fallbackMessage, Action onConfirm,
            bool showCancel = false, string cancelText = "取消", Action onCancel = null,
            Sprite fallbackIcon = null, Color? fallbackTint = null, Material fallbackMaterial = null,
            Action playSound = null)
        {
            var art = WorkshopArt.ItemCard(item);
            return art != null
                ? ShowCard(item, art, "收下", onConfirm, showCancel, cancelText, onCancel, playSound)
                : ShowMessage("获得" + WorkshopProgress.ItemName(item), fallbackMessage, "收下",
                    onConfirm, showCancel, cancelText, onCancel, fallbackIcon, fallbackTint, fallbackMaterial,
                    playSound);
        }

        public static SimplePopup Show(string title, string message,
            string confirmText = "确定", Action onConfirm = null,
            bool showCancel = false, string cancelText = "取消", Action onCancel = null,
            Sprite icon = null, Color? iconColor = null, Material iconMaterial = null)
        {
            if (WorkshopArt.TryIdentifyItemCard(icon, out var item))
                return ShowCard(item, icon, confirmText, onConfirm,
                    showCancel, cancelText, onCancel);
            if (HarvestOverlayManager.IsHarvestActive) return null;
            return ShowMessage(title, message, confirmText, onConfirm, showCancel, cancelText,
                onCancel, icon, iconColor, iconMaterial);
        }

        internal static SimplePopup ShowChapterCompletion(WorkshopSeason season, Action onConfirm)
        {
            if (HarvestOverlayManager.IsHarvestActive) return null;

            const string title = "章节任务达成";
            const string message = "本章节的所有任务已达成。";
            const string confirmText = "去合成";
            const string cancelText = "稍后";
            var atlas = Resources.Load<Texture2D>("WorkshopFinish/ChapterCompletionPixelCards");
            if (atlas == null)
                return Show(WorkshopProgress.SeasonName(season) + "季" + title, message, confirmText, onConfirm,
                    showCancel: true, cancelText: cancelText);

            var popup = Create(out var frame, onConfirm, null);
            int index = (int)season;
            var accent = ChapterAccents[index];
            var card = WorkshopUi.Rect(frame, "Popup Frame", new Rect(288, 162, 1344, 756));
            var illustration = WorkshopUi.Rect(card, "Chapter Completion Illustration", new Rect(0, 0, 1344, 756))
                .gameObject.AddComponent<UnityEngine.UI.RawImage>();
            illustration.texture = atlas;
            illustration.raycastTarget = false;
            // The atlas reads from top left, while texture coordinates start at the bottom left.
            var tile = new Rect(index % 2 * 0.5f, (1 - index / 2) * 0.5f, 0.5f, 0.5f);
            float insetX = 1f / atlas.width;
            float insetY = 1f / atlas.height;
            illustration.uvRect = new Rect(tile.x + insetX, tile.y + insetY,
                tile.width - insetX * 2, tile.height - insetY * 2);

            WorkshopUi.Label(card, "Title", new Rect(490, 91, 364, 86),
                title, 46, WorkshopUi.Cream, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            WorkshopUi.Label(card, "Chapter Season", new Rect(1230, 296, 64, 105),
                WorkshopProgress.SeasonName(season), 47, accent, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            WorkshopUi.Label(card, "Message", new Rect(165, 324, 480, 58),
                message, 32, WorkshopUi.Ink, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            WorkshopUi.Label(card, "Chapter Product Label", new Rect(191, 405, 209, 56),
                "合成成果", 28, WorkshopUi.Faded, TextAnchor.MiddleRight).fontStyle = FontStyle.Bold;
            WorkshopUi.Fill(card, "Chapter Product Rule", new Rect(418, 418, 2, 30), WorkshopUi.Bronze);
            WorkshopUi.Label(card, "Chapter Product", new Rect(439, 405, 150, 56),
                WorkshopProgress.ProductName(season), 34, accent).fontStyle = FontStyle.Bold;

            var cancel = WorkshopUi.PixelButton(card, "Cancel", new Rect(181, 484, 152, 67),
                cancelText, popup.OnCancel, primary: false, size: 29);
            var confirm = WorkshopUi.Button(card, "Confirm", new Rect(349, 484, 280, 67),
                confirmText, popup.OnConfirm, accent, size: 33);
            ApplySeasonButtonArt(confirm, season);
            cancel.GetComponentInChildren<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;
            confirm.GetComponentInChildren<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;
            cancel.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnRight = confirm
            };
            confirm.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnLeft = cancel
            };
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(confirm.gameObject);
            WorkshopAudioFeedback.Notice();
            return popup;
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
            var season = (WorkshopSeason)((int)item / 3);
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
            ApplySeasonButtonArt(confirm, season);
            (playSound ?? WorkshopAudioFeedback.Notice)();
            return popup;
        }

        private static void ApplySeasonButtonArt(UnityEngine.UI.Button button, WorkshopSeason season)
        {
            var buttonArt = WorkshopArt.SeasonButton(season);
            if (buttonArt == null) return;
            var image = (UnityEngine.UI.Image)button.targetGraphic;
            image.sprite = buttonArt;
            image.type = buttonArt.border.sqrMagnitude > 0f
                ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = Color.white;
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

        public void OnConfirm() => Close(_onConfirm);

        public void OnCancel() => Close(_onCancel);

        private void Close(Action action)
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            Destroy(gameObject);
            action?.Invoke();
        }
    }
}
