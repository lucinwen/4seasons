using UnityEngine;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private static void SetAutumnActionVisibility(Button button, bool visible, bool available = true)
    {
        var root = button.transform.parent.gameObject;
        var group = root.GetComponent<CanvasGroup>();
        if (group == null) group = root.AddComponent<CanvasGroup>();
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        button.interactable = visible && available;
    }

    private static Sprite GameArtSprite(string name) => Resources.Load<Sprite>("WorkshopGameArt/" + name);

    private static Image GameArtSurface(Transform parent, string name, Rect bounds, Sprite sprite, Color tint)
    {
        return sprite != null
            ? WorkshopUi.Picture(parent, name, bounds, sprite, tint, stretch: true)
            : WorkshopUi.Fill(parent, name, bounds, tint);
    }

    private static RectTransform IllustratedGameCard(Transform parent, string name, Rect bounds, Sprite art,
        Color fallback, Color? tint = null)
    {
        if (art == null) return WorkshopUi.PixelCard(parent, name, bounds, fallback, WorkshopUi.Bronze);

        var card = WorkshopUi.Rect(parent, name, bounds);
        WorkshopUi.Picture(card, "Illustrated Surface", new Rect(0, 0, bounds.width, bounds.height), art, tint, stretch: true);
        return card;
    }
}
