using UnityEngine;
using UnityEngine.UI;

public sealed partial class WorkshopKnowledgeView
{
    internal const string XiaomanCraftKnowledge = "缫丝（《清嘉录》）：小满乍来，蚕妇卖茧，治车缫丝，昼夜操作。小满时节，蚕茧收成，缫丝工作逐渐繁忙。蚕农将蚕茧放入热水中煮制，寻得丝头，再借助缫丝车抽取细丝，缠绕成线。";
    internal const string MangzhongCraftKnowledge = "理经卷纬（《四民月令》）：五月，乃可以丝、治絮、曝缣。夏至时节，蚕丝逐渐完成整理。工匠将丝线按照织造要求排列经线、卷绕纬线，整理好丝轴，为织机上的经纬交织做好准备。";

    internal static bool DrawCraftSolar(Transform parent, Rect bounds, int ordinal,
        string acknowledgement = "收下卡片")
    {
        if (ordinal != 7 && ordinal != 8) return false;
        Sprite art = Resources.Load<Sprite>(ordinal == 7
            ? "WorkshopCraftSolar/Xiaoman" : "WorkshopCraftSolar/Mangzhong");
        if (art == null) return false;
        float scale = Mathf.Min(bounds.width / art.rect.width, bounds.height / art.rect.height);
        Vector2 size = art.rect.size * scale;
        var image = WorkshopUi.Picture(parent, "Full Size Solar Card",
            new Rect(bounds.center - size * .5f, size), art, stretch: true);
        Rect source = ordinal == 7 ? new Rect(294, 406, 872, 222) : new Rect(260, 361, 832, 225);
        var label = WorkshopUi.Label(image.transform, "Craft Solar Knowledge",
            new Rect(source.position * scale, source.size * scale),
            ordinal == 7 ? XiaomanCraftKnowledge : MangzhongCraftKnowledge,
            Mathf.RoundToInt(36 * scale), new Color32(68, 43, 28, 255),
            TextAnchor.MiddleLeft, WorkshopArt.ReadingFont);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 26;
        label.resizeTextMaxSize = label.fontSize;
        FitCardLettering(label, 26, label.fontSize);
        Rect button = ordinal == 7 ? new Rect(605, 669, 262, 72) : new Rect(550, 616, 242, 69);
        var done = WorkshopUi.Label(image.transform, "Craft Solar Acknowledgement",
            new Rect(button.position * scale, button.size * scale), acknowledgement,
            Mathf.RoundToInt(43 * scale), WorkshopUi.Cream, TextAnchor.MiddleCenter,
            WorkshopArt.ReadingFont);
        FitCardLettering(done, 25, done.fontSize);
        return true;
    }

    private static Rect CraftSolarCloseBounds(Rect bounds, int ordinal)
    {
        Vector2 sourceSize = ordinal == 7 ? new Vector2(1458, 821) : new Vector2(1339, 754);
        float scale = Mathf.Min(bounds.width / sourceSize.x, bounds.height / sourceSize.y);
        Vector2 origin = bounds.center - sourceSize * scale * .5f;
        Rect button = ordinal == 7 ? new Rect(560, 650, 360, 110) : new Rect(510, 597, 320, 106);
        return new Rect(origin + button.position * scale, button.size * scale);
    }
}
