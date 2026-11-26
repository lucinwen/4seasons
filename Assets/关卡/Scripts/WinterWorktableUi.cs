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
            Resources.Load<Sprite>("WordRevision/AutumnDyeWorkbench") ??
            Resources.Load<Sprite>("WorkshopFinish/DyeWorkbench"), stretch: true);
        WorkshopImmersive.Button(frame, "CloseButton", new Rect(1718, 35, 154, 60), "返回", close);
    }

    internal static RectTransform Footer(RectTransform frame)
    {
        return WorkshopUi.Rect(frame, "Workbench Hint", new Rect(46, 35, 1550, 64));
    }

    internal static UnityEngine.UI.Text Nameplate(Transform parent, string name, Rect bounds, string label, int size)
    {
        var plate = WorkshopUi.Rect(parent, name, bounds);
        return WorkshopImmersive.Text(plate, "Label", new Rect(0, 0, bounds.width, bounds.height),
            label, size, TextAnchor.MiddleCenter);
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
