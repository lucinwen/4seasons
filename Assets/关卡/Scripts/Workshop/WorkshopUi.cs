using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal static class WorkshopUi
{
    internal static readonly Color Ink = new Color32(25, 51, 45, 255);
    internal static readonly Color Paper = new Color32(245, 236, 217, 255);
    internal static readonly Color Cream = new Color32(255, 248, 230, 255);
    internal static readonly Color Clay = new Color32(132, 67, 54, 255);
    internal static readonly Color Faded = new Color32(101, 91, 73, 255);
    internal static readonly Color Green = new Color32(46, 82, 69, 255);
    internal static readonly Color Bronze = new Color32(182, 139, 75, 255);
    internal static readonly Color Line = new Color32(174, 151, 112, 148);
    internal static readonly Color Veil = new Color32(16, 33, 30, 216);

    internal static readonly Color PixelPaper = Paper;
    internal static readonly Color PixelInk = Ink;
    internal static readonly Color PixelSeam = Bronze;
    private static readonly Color CardShadow = new Color32(13, 27, 24, 126);
    private static Sprite pixelSurface;

    internal static void ApplyPixelShape(UnityEngine.UI.Image image)
    {
        if (pixelSurface == null)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Workshop Pixel Corners",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int edgeX = Mathf.Min(x, size - 1 - x);
                    int edgeY = Mathf.Min(y, size - 1 - y);
                    bool inside = edgeX + edgeY >= 4;
                    pixels[y * size + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            pixelSurface = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(5, 5, 5, 5));
            pixelSurface.hideFlags = HideFlags.HideAndDontSave;
        }
        image.sprite = pixelSurface;
        image.type = UnityEngine.UI.Image.Type.Sliced;
    }

    internal static RectTransform Card(Transform parent, string name, Rect bounds, Color surface, Color? edge = null)
    {
        var frame = Rect(parent, name, bounds);
        var shadow = Fill(frame, "Shadow", new Rect(2, 5, bounds.width, bounds.height), CardShadow);
        var border = Fill(frame, "Edge", new Rect(0, 0, bounds.width, bounds.height), edge ?? Line);
        var face = Fill(frame, "Surface", new Rect(2, 2, bounds.width - 4, bounds.height - 4), surface);
        ApplyPixelShape(shadow);
        ApplyPixelShape(border);
        ApplyPixelShape(face);
        return frame;
    }

    internal static RectTransform PixelCard(Transform parent, string name, Rect bounds, Color surface, Color edge)
        => Card(parent, name, bounds, surface, edge);

    internal static RectTransform WorkbenchCard(Transform parent, string name, Rect bounds, Color surface, Color edge)
    {
        var card = PixelCard(parent, name, bounds, surface, edge);
        var paper = Resources.Load<Sprite>("WorkshopGameArt/PaperFrame");
        if (paper != null)
        {
            var layer = Picture(card, "Mulberry Paper", new Rect(0, 0, bounds.width, bounds.height), paper, stretch: true);
            layer.type = Image.Type.Sliced;
        }
        return card;
    }

    internal static UnityEngine.UI.Button PixelButton(Transform parent, string name, Rect bounds,
        string label, Action clicked, bool primary, int size, Color? primaryColor = null)
    {
        Color surface = primary ? primaryColor ?? PixelInk : PixelPaper;
        var card = PixelCard(parent, name, bounds, surface, PixelSeam);
        var button = Button(card, "Action", new Rect(2, 2, bounds.width - 4, bounds.height - 4),
            "", clicked, surface);
        Label(button.transform, "Label", new Rect(6, 0, bounds.width - 16, bounds.height - 4), label, size,
            OnSurface(surface), TextAnchor.MiddleCenter, WorkshopArt.WorkshopFont);
        return button;
    }

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

    internal static Image Block(RectTransform frame, Color color)
    {
        var backdrop = Fill(frame.parent, "Backdrop", new Rect(), color);
        var rect = backdrop.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        backdrop.raycastTarget = true;
        return backdrop;
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
        label.font = font ?? WorkshopArt.WorkshopFont;
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
        Color surface = background ?? Green;
        var image = Fill(parent, name, bounds, surface);
        if (surface.a > 0.05f) ApplyPixelShape(image);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.interactable = enabled;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.94f, 0.98f, 0.94f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.78f, 0.86f, 0.82f);
        colors.disabledColor = new Color(0.84f, 0.85f, 0.82f, 0.75f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        if (clicked != null) button.onClick.AddListener(() => clicked());
        if (!string.IsNullOrEmpty(label))
            Label(image.transform, "Label", new Rect(8f, 0f, bounds.width - 16f, bounds.height),
                label, size, OnSurface(surface), TextAnchor.MiddleCenter, font);
        return button;
    }

    private static Color OnSurface(Color surface)
    {
        float luminance = surface.r * 0.2126f + surface.g * 0.7152f + surface.b * 0.0722f;
        return luminance > 0.59f ? Ink : Cream;
    }
}
