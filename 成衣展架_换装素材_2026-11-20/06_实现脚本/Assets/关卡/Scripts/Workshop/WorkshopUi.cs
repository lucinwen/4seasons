using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal static class WorkshopUi
{
    internal static readonly Color Ink = new Color32(54, 44, 36, 255);
    internal static readonly Color Paper = new Color32(255, 241, 203, 255);
    internal static readonly Color Cream = new Color32(255, 250, 230, 255);
    internal static readonly Color Clay = new Color32(144, 63, 45, 255);
    internal static readonly Color Faded = new Color32(115, 97, 76, 255);
    internal static readonly Color Green = new Color32(43, 83, 66, 255);

    internal static RectTransform NewCanvas(string name, int order)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        canvas.pixelPerfect = true;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("Workshop Event System", typeof(EventSystem), typeof(StandaloneInputModule));

        var frame = new GameObject("Layout", typeof(RectTransform)).GetComponent<RectTransform>();
        frame.SetParent(root.transform, false);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = scaler.referenceResolution;
        frame.anchoredPosition = Vector2.zero;
        return frame;
    }

    internal static void CloseCanvas(RectTransform frame)
    {
        if (frame == null) return;
        var canvas = frame.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvas.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(canvas.gameObject);
    }

    internal static void Block(RectTransform frame, Color color)
    {
        var backdrop = Fill(frame.parent, "Backdrop", new Rect(), color);
        var rect = backdrop.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        backdrop.raycastTarget = true;
    }

    internal static RectTransform Rect(Transform parent, string name, Rect bounds)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
        rect.sizeDelta = bounds.size;
        return rect;
    }

    internal static Image Picture(Transform parent, string name, Rect bounds, Sprite sprite,
        Color? tint = null, bool stretch = false)
    {
        var image = Rect(parent, name, bounds).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = tint ?? Color.white;
        image.preserveAspect = !stretch;
        image.raycastTarget = false;
        image.enabled = sprite != null;
        return image;
    }

    internal static Image Fill(Transform parent, string name, Rect bounds, Color color)
    {
        var image = Picture(parent, name, bounds, null, color, true);
        image.enabled = true;
        return image;
    }

    internal static Image Panel(Transform parent, string name, Rect bounds, Sprite sprite)
    {
        if (sprite == null) return Fill(parent, name, bounds, Paper);
        var image = Picture(parent, name, bounds, sprite, null, true);
        if (sprite.border.sqrMagnitude > 0f) image.type = Image.Type.Sliced;
        return image;
    }

    internal static Text Label(Transform parent, string name, Rect bounds, string value, int size,
        Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft, Font font = null)
    {
        var label = Rect(parent, name, bounds).gameObject.AddComponent<Text>();
        label.font = font ?? WorkshopArt.PixelFont;
        label.fontSize = size;
        label.alignment = anchor;
        label.color = color ?? Ink;
        label.text = value;
        label.raycastTarget = false;
        return label;
    }

    internal static UnityEngine.UI.Button Button(Transform parent, string name, Rect bounds, string label,
        Action clicked, Color? background = null, bool enabled = true, int size = 32, Font font = null)
    {
        var image = Fill(parent, name, bounds, background ?? Green);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.interactable = enabled;
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.89f, 0.7f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.79f, 0.69f, 0.55f);
        colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.68f);
        button.colors = colors;
        if (clicked != null) button.onClick.AddListener(() => clicked());
        if (!string.IsNullOrEmpty(label))
            Label(image.transform, "Label", new Rect(8f, 0f, bounds.width - 16f, bounds.height),
                label, size, Cream, TextAnchor.MiddleCenter, font);
        return button;
    }
}
