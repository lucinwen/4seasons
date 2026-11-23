using UnityEngine;

internal static class WorkshopHallSignage
{
    internal const string TexturePath = "WorkshopFinish/MainHall";
    internal const string WardrobeName = "锦衣阁";
    internal static readonly Rect Panorama = new Rect(0f, 0f, 1920f, 640f);
    internal static readonly Rect FullView = new Rect(0f, 0f, 1f, 1f);
    private static readonly Color RoofSignInk = new Color32(92, 40, 22, 255);
    private static readonly Color DirectionSignInk = new Color32(74, 46, 26, 255);

    internal static RectTransform Draw(Transform parent, Rect bounds, Rect view)
    {
        var signage = WorkshopUi.Rect(parent, "Panorama Signage", bounds);
        DrawLabel(signage, view, new Rect(846f, 239f, 228f, 44f), "四时合作社工坊", 32, RoofSignInk);
        DrawLabel(signage, view, new Rect(1690f, 304f, 74f, 26f), "公告栏", 18, DirectionSignInk);
        DrawLabel(signage, view, new Rect(1690f, 336f, 74f, 26f), "储物屋", 18, DirectionSignInk);
        DrawLabel(signage, view, new Rect(1690f, 368f, 74f, 26f), WardrobeName, 18, DirectionSignInk);
        return signage;
    }

    private static void DrawLabel(RectTransform parent, Rect view, Rect plaque, string text, int size, Color ink)
    {
        var crop = new Rect(view.x * Panorama.width, (1f - view.yMax) * Panorama.height,
            view.width * Panorama.width, view.height * Panorama.height);
        if (plaque.xMin < crop.xMin || plaque.xMax > crop.xMax ||
            plaque.yMin < crop.yMin || plaque.yMax > crop.yMax) return;

        float scaleX = parent.rect.width / crop.width;
        float scaleY = parent.rect.height / crop.height;
        var bounds = new Rect((plaque.x - crop.x) * scaleX, (plaque.y - crop.y) * scaleY,
            plaque.width * scaleX, plaque.height * scaleY);
        WorkshopUi.Label(parent, text, bounds, text, Mathf.Max(1, Mathf.RoundToInt(size * scaleY)),
            ink, TextAnchor.MiddleCenter, WorkshopArt.WorkshopFont);
    }
}
