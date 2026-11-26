using UnityEngine;

public sealed partial class WorkshopKnowledgeView
{
    private readonly struct ItemCardCopy
    {
        internal readonly Vector2 TitleCenter;
        internal readonly Vector2 BodyTopLeft;
        internal readonly string Body;

        internal ItemCardCopy(float titleX, float titleY, float bodyX, float bodyY, string body)
        {
            TitleCenter = new Vector2(titleX, titleY);
            BodyTopLeft = new Vector2(bodyX, bodyY);
            Body = body;
        }
    }

    private static readonly ItemCardCopy[] ItemCopy =
    {
        new ItemCardCopy(528, 195, 253, 821,
            "桑叶是家蚕的主要\n食物，不同生长阶\n段的蚕宝宝对桑叶\n的鲜嫩程度和大小\n有不同需求。"),
        new ItemCardCopy(444, 193, 194, 824,
            "蚕种是蚕宝宝生命\n的起点，经过适宜\n的温湿度孵化后，\n蚁蚕便会破壳而出\n，开始生长。"),
        new ItemCardCopy(447, 195, 192, 820,
            "蚕宝宝会经历不同\n的生长阶段，随着\n不断蜕皮逐渐长大\n，最终吐丝结茧，\n完成生命蜕变。"),
        new ItemCardCopy(403, 168, 202, 805,
            "盐是传统蚕丝加工\n中的辅助材料之一\n，了解盐的储存与\n使用，认识古代工\n坊的物资管理。"),
        new ItemCardCopy(405, 168, 214, 807,
            "索绪是从煮熟的蚕\n茧中寻找并抽出丝\n头的完整过程，为\n后续连续缫丝做好\n准备。"),
        new ItemCardCopy(416, 166, 217, 807,
            "线轴用于缠绕和收\n纳缫出的蚕丝，方\n便丝线整理、运输\n与后续织造，避免\n丝线缠绕打结。"),
        new ItemCardCopy(427, 184, 163, 900,
            "梭子用于携带纬线穿过\n经线之间，配合织机往\n复运动，使经纬线交织\n形成完整布面。"),
        new ItemCardCopy(424, 183, 172, 899,
            "生坯绸是织造完成后尚\n未经过精练、染整的原\n始绸料，保留着丝织物\n最初的质感。"),
        new ItemCardCopy(417, 184, 151, 899,
            "土地公是乡间守护神，\n寄托着蚕农祈求桑树繁\n茂、丰收、四时风调雨\n顺的美好愿望。"),
        new ItemCardCopy(428, 194, 207, 867,
            "染布是通过染料与\n织物接触，使布料\n呈现不同色彩的工\n艺，可结合浸染、\n晾晒等步骤完成。"),
        new ItemCardCopy(409, 195, 197, 867,
            "衣片是根据服装结\n构裁剪出的布料部\n件，经过拼接、缝\n合与整理，最终组\n合成完整服饰。"),
        new ItemCardCopy(420, 194, 203, 863,
            "缝衣针是传统成衣\n制作的重要工具，\n通过穿引丝线，将\n裁剪好的衣片缝合\n成完整衣物。"),
    };

    internal static void DrawItemCard(Transform parent, Rect bounds, WorkshopItem item, Sprite art)
    {
        float scale = Mathf.Min(bounds.width / art.rect.width, bounds.height / art.rect.height);
        Vector2 size = art.rect.size * scale;
        var card = WorkshopUi.Picture(parent, "Item Card Illustration",
            new Rect(bounds.center - size * 0.5f, size), art, stretch: true).rectTransform;

        if (WorkshopArt.ItemCardHasPrintedCopy(item)) return; // Reviewed HD/Word cards already carry their original copy.
        var copy = ItemCopy[(int)item];
        var season = (WorkshopSeason)((int)item / 3);
        bool lightTitle = season == WorkshopSeason.Spring || season == WorkshopSeason.Autumn;
        var titleBox = new Rect((copy.TitleCenter - new Vector2(210, 60)) * scale, new Vector2(420, 120) * scale);
        var title = WorkshopUi.Label(card, "Item Card Title", titleBox,
            item == WorkshopItem.Statue ? "土地公" : WorkshopProgress.ItemName(item),
            Mathf.RoundToInt((lightTitle ? 75 : 60) * scale),
            lightTitle ? WorkshopUi.Cream : WorkshopUi.Ink, TextAnchor.MiddleCenter,
            WorkshopArt.ReadingFont);

        var bodyBox = new Rect(copy.BodyTopLeft * scale,
            new Vector2(520, 310) * scale);
        var body = WorkshopUi.Label(card, "Item Card Description", bodyBox, copy.Body,
            Mathf.RoundToInt(44 * scale), WorkshopUi.Ink, TextAnchor.UpperLeft,
            WorkshopArt.ReadingFont);
        body.horizontalOverflow = HorizontalWrapMode.Overflow;
        body.verticalOverflow = VerticalWrapMode.Truncate;
        body.lineSpacing = 1f;
        // Reading fonts have taller Chinese line metrics than the old pixel font.
        // Fit the unchanged original copy into the original printed text area.
        body.resizeTextForBestFit = true;
        body.resizeTextMinSize = 20;
        body.resizeTextMaxSize = body.fontSize;
        FitCardLettering(body, 20, body.fontSize);
    }

    private static void FitCardLettering(UnityEngine.UI.Text label, int minimum, int maximum)
    {
        // Unity's built-in best fit uses line metrics; some Chinese glyphs extend
        // below those metrics. Measure the complete glyph geometry as well.
        Rect rect = label.rectTransform.rect;
        var settings = label.GetGenerationSettings(rect.size);
        settings.resizeTextForBestFit = false;
        settings.verticalOverflow = VerticalWrapMode.Overflow;
        settings.generateOutOfBounds = true;
        float unit = 1f / label.pixelsPerUnit;
        using (var generator = new TextGenerator())
        {
            for (int size = maximum; size >= minimum; size--)
            {
                settings.fontSize = size;
                label.font.RequestCharactersInTexture(label.text, size, label.fontStyle);
                bool fits = generator.Populate(label.text, settings);
                var vertices = generator.verts;
                for (int start = 0; fits && start + 3 < vertices.Count; start += 4)
                {
                    if (vertices[start].position == vertices[start + 1].position &&
                        vertices[start].position == vertices[start + 2].position &&
                        vertices[start].position == vertices[start + 3].position) continue;
                    for (int corner = 0; corner < 4; corner++)
                    {
                        var point = vertices[start + corner].position * unit;
                        const float pixelRounding = 2f;
                        if (point.x < rect.xMin - pixelRounding || point.x > rect.xMax + pixelRounding ||
                            point.y < rect.yMin - pixelRounding || point.y > rect.yMax + pixelRounding) { fits = false; break; }
                    }
                }
                if (!fits && size != minimum) continue;
                label.resizeTextForBestFit = false;
                label.fontSize = size;
                return;
            }
        }
    }
}
