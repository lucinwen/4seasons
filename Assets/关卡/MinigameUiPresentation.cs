using UnityEngine;
using UnityEngine.SceneManagement;

internal static class MinigameUiPresentation
{
    internal static readonly Color Pine = new Color32(25, 51, 45, 255);
    internal static readonly Color Paper = new Color32(245, 236, 217, 255);
    internal static readonly Color Linen = new Color32(235, 223, 197, 255);
    internal static readonly Color MutedInk = new Color32(78, 89, 77, 255);
    internal static readonly Color Bronze = new Color32(182, 139, 75, 255);
    internal const float BackgroundVeilAlpha = 0.75f;
    internal static readonly Color BackgroundVeil = new Color(0f, 0f, 0f, BackgroundVeilAlpha);
    internal static readonly Color Veil = new Color32(12, 20, 19, 158);
    private static Sprite veilSprite;

    internal static SpriteRenderer WorldVeil(Transform parent, string name, int order,
        float alpha = BackgroundVeilAlpha)
    {
        if (veilSprite == null)
        {
            var texture = Texture2D.whiteTexture;
            veilSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), texture.width, 0, SpriteMeshType.FullRect);
            veilSprite.hideFlags = HideFlags.HideAndDontSave;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var veil = go.AddComponent<SpriteRenderer>();
        veil.sprite = veilSprite;
        veil.sortingOrder = order;
        Color tint = BackgroundVeil;
        tint.a = alpha;
        veil.color = tint;
        return veil;
    }

    internal static T Find<T>(Transform root, string path) where T : Component
    {
        return root != null ? root.Find(path)?.GetComponent<T>() : null;
    }

    internal static void Dialog(GameObject overlay, string cardPath, string titlePath)
    {
        if (overlay == null) return;
        var card = Find<RectTransform>(overlay.transform, cardPath);
        Dialog(overlay.transform, card, Find<UnityEngine.UI.Text>(card, titlePath));
    }

    internal static void Dialog(Transform overlay, RectTransform card, UnityEngine.UI.Text title)
    {
        if (overlay != null)
        {
            var veil = Find<UnityEngine.UI.Image>(overlay, "Dim") ?? overlay.GetComponent<UnityEngine.UI.Image>();
            if (veil != null) veil.color = Veil;
        }

        if (card == null) return;
        WorkshopImmersive.StripSurface(card);

        foreach (var text in card.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            if (text.GetComponentInParent<UnityEngine.UI.Button>(true) == null)
                WorkshopImmersive.QuietText(text);
        }

        var divider = Find<UnityEngine.UI.Image>(card, "Divider");
        if (divider != null) divider.gameObject.SetActive(false);
    }

    internal static void Surface(UnityEngine.UI.Image image, Color color, bool shadow = false)
    {
        if (image == null) return;
        WorkshopUi.ApplyPixelShape(image);
        image.color = color;
        if (!shadow) return;

        var shade = image.GetComponent<UnityEngine.UI.Shadow>();
        if (shade == null) shade = image.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        shade.effectColor = new Color32(10, 24, 20, 125);
        shade.effectDistance = new Vector2(1f, -5f);
    }

    internal static void Action(UnityEngine.UI.Button button, bool primary = true)
    {
        if (button == null) return;
        var image = button.targetGraphic as UnityEngine.UI.Image ?? button.GetComponent<UnityEngine.UI.Image>();
        if (image == null) return;

        image.color = new Color(0,0,0,.002f);
        var shadow = image.GetComponent<UnityEngine.UI.Shadow>();
        if (shadow != null) shadow.enabled = false;
        button.targetGraphic = image;
        button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = colors.selectedColor = new Color32(255, 244, 219, 255);
        colors.pressedColor = new Color32(195, 205, 188, 255);
        colors.disabledColor = new Color32(195, 191, 182, 140);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        foreach (var text in button.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            WorkshopImmersive.QuietText(text);
    }

    /// <summary>Keep the playable objects; remove decorative HUD sheets from legacy scenes.</summary>
    internal static void Immerse(Scene scene, WorkshopSeason season)
    {
        if (scene.name == "CocoonMatch")
        {
            RestoreCocoonBackground(scene);
            return;
        }
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
            {
                switch (part.name)
                {
                    case "CocoonOrnatePanel": case "QuietPaperGlow": case "PanelShadow":
                    case "TitleDivider": case "SolarIntroduction": case "SolarTerm":
                    case "Subtitle": case "Rules": case "CapacityHint":
                        part.gameObject.SetActive(false); break;
                }
            }
        }
    }

    internal static void RestoreCocoonBackground(Scene scene)
    {
        Transform backdrop = null;
        Transform glow = null;
        // Earlier presentation imports saved these original layers inactive in the scene.
        foreach (var root in scene.GetRootGameObjects())
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
                switch (part.name)
                {
                    case "TealBackdrop":
                        backdrop = part;
                        part.gameObject.SetActive(true);
                        break;
                    case "QuietPaperGlow":
                        glow = part;
                        part.gameObject.SetActive(true);
                        break;
                    case "CocoonOrnatePanel":
                    case "PanelShadow":
                        part.gameObject.SetActive(true);
                        break;
                }
        if (glow != null) glow.SetAsFirstSibling();
        if (backdrop != null) backdrop.SetAsFirstSibling();
    }

    internal static void Label(UnityEngine.UI.Text text, Color color, int size = 0)
    {
        if (text == null) return;
        text.font = WorkshopArt.WorkshopFont;
        text.color = color;
        if (size > 0) text.fontSize = size;
        text.raycastTarget = false;
    }

    internal static UnityEngine.UI.Image InsertFill(Transform parent, string name, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image))
            .GetComponent<UnityEngine.UI.Image>();
        var rect = image.rectTransform;
        rect.SetParent(parent, false);
        rect.SetAsFirstSibling();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.raycastTarget = false;
        image.color = color;
        return image;
    }

    private static void AddTitleRule(RectTransform title)
    {
        var line = InsertFill(title, "Bronze Rule", Bronze).rectTransform;
        line.anchorMin = line.anchorMax = new Vector2(0.5f, 0f);
        line.sizeDelta = new Vector2(88f, 1f);
        line.anchoredPosition = Vector2.zero;

        var stitch = InsertFill(line, "Center Stitch", Bronze).rectTransform;
        stitch.anchorMin = stitch.anchorMax = new Vector2(0.5f, 0.5f);
        stitch.sizeDelta = new Vector2(6f, 6f);
        stitch.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }
}
