using System;
using UnityEngine;

internal static class WinterWorktableUi
{
    private static readonly Color Ink = new Color32(39, 31, 26, 240);
    internal static readonly Color FocusGold = new Color32(255, 218, 132, 255);

    internal static void Build(RectTransform frame, string title, float titleWidth, Action close)
    {
        WorkshopUi.Block(frame, MinigameUiPresentation.BackgroundVeil);
        WorkshopUi.Picture(frame, "Dye Workbench Artwork", new Rect(0, 0, 1920, 1080),
            Resources.Load<Sprite>("WorkshopFinish/DyeWorkbench"), stretch: true);
        Nameplate(frame, "Heading", new Rect(46, 40, titleWidth, 70), title, 39);
        WorkshopUi.PixelButton(frame, "CloseButton", new Rect(1718, 45, 154, 60),
            "关闭", close, true, 27, Ink);
    }

    internal static RectTransform Footer(RectTransform frame)
    {
        return WorkshopUi.PixelCard(frame, "Workbench Instructions", new Rect(46, 958, 1828, 81),
            Ink, WorkshopUi.PixelSeam);
    }

    internal static UnityEngine.UI.Text Nameplate(Transform parent, string name, Rect bounds, string label, int size)
    {
        var plate = WorkshopUi.Fill(parent, name, bounds, Ink);
        WorkshopUi.ApplyPixelShape(plate);
        return WorkshopUi.Label(plate.transform, "Label", new Rect(0, 0, bounds.width, bounds.height),
            label, size, WorkshopUi.Cream, TextAnchor.MiddleCenter);
    }

    internal static Rect MaterialBounds(int index, int count)
    {
        int firstRowCount = Mathf.Min(count, count > 3 ? 3 : 2);
        bool firstRow = index < firstRowCount;
        int column = firstRow ? index : index - firstRowCount;
        int columns = firstRow ? firstRowCount : count - firstRowCount;
        float x = 387 + (column - (columns - 1) * 0.5f) * 190 - 87;
        return new Rect(x, firstRow ? 535 : 706, 174, 161);
    }

    internal static UnityEngine.UI.Outline FocusOutline(UnityEngine.UI.Image image)
    {
        var outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = FocusGold;
        outline.effectDistance = new Vector2(3, -3);
        outline.enabled = false;
        return outline;
    }
}
