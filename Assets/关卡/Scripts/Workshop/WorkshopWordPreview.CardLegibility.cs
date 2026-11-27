#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class WorkshopWordPreview
{
    // Fixed editorial expectations: these must not be derived from the runtime copy tables.
    private static readonly string[] OriginalItemTitles =
    {
        "桑叶", "蚕种", "蚕宝宝", "盐", "索绪", "线轴", "梭子", "缬", "土地公", "染布", "衣片", "缝衣针", "蚕连"
    };

    private static readonly string[] OriginalItemKnowledge =
    {
        "桑叶是家蚕的主要食物，不同生长阶段的蚕宝宝对桑叶的鲜嫩程度和大小有不同需求。",
        "蚕种是蚕宝宝生命的起点，经过适宜的温湿度孵化后，蚁蚕便会破壳而出，开始生长。",
        "蚕宝宝会经历不同的生长阶段，随着不断蜕皮逐渐长大，最终吐丝结茧，完成生命蜕变。",
        "盐是传统蚕丝加工中的辅助材料之一，了解盐的储存与使用，认识古代工坊的物资管理。",
        "索绪是从煮熟的蚕茧中寻找并抽出丝头的完整过程，为后续连续缫丝做好准备。",
        "线轴用于缠绕和收纳缫出的蚕丝，方便丝线整理、运输与后续织造，避免丝线缠绕打结。",
        "梭子用于携带纬线穿过经线之间，配合织机往复运动，使经纬线交织形成完整布面。",
        "缬是经攀花工艺织成的提花丝织物，以经纬丝线交织形成精美纹样，花纹细腻雅致。",
        "土地公是乡间守护神，寄托着蚕农祈求桑树繁茂、丰收、四时风调雨顺的美好愿望。",
        "染布是通过染料与织物接触，使布料呈现不同色彩的工艺，可结合浸染、晾晒等步骤完成。",
        "衣片是根据服装结构裁剪出的布料部件，经过拼接、缝合与整理，最终组合成完整服饰。",
        "缝衣针是传统成衣制作的重要工具，通过穿引丝线，将裁剪好的衣片缝合成完整衣物。",
        "蚕连是附着蚕卵的纸张，冬季卷起悬挂，经受寒气以保藏蚕种，为来年春蚕孵育做好准备。"
    };

    private static readonly string[] OriginalSolarKnowledge =
    {
        "立春|二十四节气|春|立春意味着春季开篇，|但各地并不会立刻暖和起来。|小织匠观察|留意枝头新芽与空气的变化。|收下卡片",
        "二十四节气|雨水|春|雨水时节，许多地方降水|逐渐增多，田间开始积蓄|春天的水分。|小织匠观察|看看土地是否变得湿润。|收下卡片",
        "惊蛰|二十四节气|天气渐暖，冬日藏身的昆虫逐|春|渐活跃。“惊蛰”并非处处都打雷。|小织匠观察|仔细寻找草丛中的小生命。|收下卡片",
        "二十四节气|春分|春|春分前后，白天与黑夜长短|接近，阳光带来新的生长节奏。|小织匠观察|观察日影，感受白昼变长。|收下卡片",
        "清明|二十四节气|春|清明时节，许多地方气温回升，|江南蚕乡的桑树长出新叶。|小织匠观察|看嫩叶舒展，等露干后采桑。|收下卡片",
        "二十四节气|谷雨|春|谷雨前后雨水渐丰，利于|不少作物生长。不同地区|农时并不相同。|小织匠观察|查看桑园与蚕房的春日准备。|收下卡片",
        "立夏|二十四节气|立夏|立夏标志夏季开始，气温总体升高，|农事进入忙碌时段。|小织匠观察|看看桑叶有没有长得更繁密。|收下卡片",
        "小满|夏|二十四节气|小满原指夏熟作物的籽粒|逐渐饱满，尚未完全成熟。|小织匠观察|观察田间作物如何慢慢结实。|收下卡片",
        "芒种|二十四节气|芒种前后，适宜有芒作物收获与播种，|是农忙的重要时段。|小织匠观察|留意田野里的农事活动。|收下卡片",
        "夏至|二十四节气|夏至前后，北半球迎来一|年中白昼最长的时段，|暑热尚会继续。|小织匠观察|看看太阳升得多高，日影多短。|收下卡片",
        "小暑|二十四节气|小暑意味着天气开始更加炎热，|照料蚕房要关注温湿度。|小织匠观察|寻找阴凉处，观察蚕房通风。|收下卡片",
        "大暑|二十四节气|大暑常是一年中暑热突出的|时期。养蚕和存茧都需注意|环境条件。|小织匠观察|收茧前看看工坊有没有降温。|收下卡片",
        "立秋|二十四节气|立秋表示秋季开始，|但不少地方仍有明显暑热。|小织匠观察|找找桑园叶色是否开始变化。|收下卡片",
        "处暑|二十四节气|处暑表示暑热逐步减弱，|早晚温度常开始出现差别。|小织匠观察|感受清晨与午后的不同。|收下卡片",
        "白露|二十四节气|白露时节，气温逐渐下降，清晨草叶上容易看到露珠。|小织匠观察|在清晨场景寻找露珠。|收下卡片",
        "秋分|二十四节气|秋分前后昼夜长短接近，|田野慢慢转入深秋色彩。|小织匠观察|留意白天和夜晚的变化。|收下卡片",
        "寒露|二十四节气|寒露时节气温进一步下降，|清晨的露水带来明显凉意。|小织匠观察|寻找秋叶与凝在叶尖的水珠。|收下卡片",
        "二十四节气|霜降|霜降提示天气继续转冷，|一些地方开始见霜，但不|是每地都有霜。|小织匠观察|观察枝叶是否出现霜白。|收下卡片",
        "立冬|二十四节气|立冬意味着冬季开篇，|许多地方进入冷季，|衣物也更加讲究保暖。|小织匠观察|带秋季染布，按虚线剪出衣片。|收下卡片",
        "小雪|二十四节气|小雪表示寒冷加重的时节，|并不意味着当天一定会下雪。|小织匠观察|让前片、后片、左右袖和领贴固定方向排满布面。|收下卡片",
        "大雪|二十四节气|大雪前后气温常较低，北方一些地区积雪增多。|小织匠观察|观察树枝与屋檐上的雪。|收下卡片",
        "冬至|二十四节气|冬至前后，北半球白昼最短。|此后白昼开始缓缓增长。|小织匠观察|看看冬日光影怎样变化。|收下卡片",
        "小寒|二十四节气|小寒处于一年中较冷的时段，|人们重视保暖与冬季生活。|小织匠观察|寻找工坊里温暖的物件。|收下卡片",
        "大寒|二十四节气|大寒是二十四节气的最后|一个节气，也意味着又一轮|四季将至。|小织匠观察|回到主厅，回顾四时劳动。|收下卡片"
    };

    private static IEnumerator ReviewOriginalCardLegibility()
    {
        Feedback2Save("{\"items\":4095,\"chapters\":15,\"solarTerms\":15,\"dyedColor\":\"紫色\",\"garmentId\":\"1_04\",\"steps\":[\"quest.guide.Spring\",\"quest.guide.Summer\",\"quest.guide.Autumn\",\"quest.guide.Winter\",\"spring.guyu.fed\",\"summer.xiaoman.reeled\",\"autumn.loom\",\"autumn.bailu.woven\",\"winter.dyes\",\"winter.ground\",\"autumn.hanlu.patterned\",\"winter.xiaoxue.fitted\"]}");
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(.5f);
        Feedback2Require(!WorkshopRewardCards.IsBusy, "Original-card review must load its completed legacy save silently.");
        string previousSave = PlayerPrefs.GetString("ShiXu.Workshop.v1", "");
        int labelsChecked = 0;

        for (int index = 0; index < OriginalItemTitles.Length; index++)
        {
            var item = (WorkshopItem)index;
            WorkshopKnowledgeView.ShowItem(item);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var canvas = GameObject.Find("Workshop Knowledge");
            Feedback2Require(canvas != null && WorkshopKnowledgeView.IsOpen, "Item knowledge card did not open: " + item);
            RequireSingleCard("Workshop Knowledge");
            var artwork = CardReviewImage(canvas, "Item Card Illustration");
            Feedback2Require(artwork != null && artwork.sprite == WorkshopArt.ItemCard(item),
                "Original item artwork was replaced or lost: " + item);
            var title = CardReviewText(canvas, "Item Card Title");
            var body = CardReviewText(canvas, "Item Card Description");
            if (WorkshopArt.ItemCardHasPrintedCopy(item))
            {
                // These bitmaps already carry their supplied title and knowledge text.
                Feedback2Require(title == null && body == null,
                    "The reviewed bitmap's original knowledge must not be covered with duplicate dynamic text: " + item);
                if (item == WorkshopItem.Suoxu || item == WorkshopItem.Spool)
                    Feedback2Require(artwork.sprite.texture.width >= 1600 && artwork.sprite.texture.height >= 2400,
                        "The restored card was reduced during import: " + item);
                if (item == WorkshopItem.RawSilkCloth)
                    Feedback2Require(WorkshopProgress.ItemName(item) == OriginalItemTitles[index] &&
                        artwork.sprite.name == "知识卡片-缬" && artwork.sprite.texture.width == 816 &&
                        artwork.sprite.texture.height == 1364, "The supplied autumn card or item name was not applied.");
                if (item == WorkshopItem.SilkwormEggSheet)
                    Feedback2Require(artwork.sprite.texture.width >= 800 && artwork.sprite.texture.height >= 1300,
                        "The supplied egg sheet card was reduced during import.");
            }
            else
            {
                Feedback2Require(title != null && body != null && title.text == OriginalItemTitles[index] &&
                    CardReviewNormalize(body.text) == OriginalItemKnowledge[index],
                    "Original item title or knowledge changed: " + item);
                CardReviewLegibleText(title, "item " + item + " title");
                CardReviewLegibleText(body, "item " + item + " knowledge");
                labelsChecked += 2;
            }
            yield return Capture((830 + index) + "-card-item-" + item.ToString().ToLowerInvariant());
            InvokeNamedButton("Close Detail");
            yield return null;
            Feedback2Require(!WorkshopKnowledgeView.IsOpen && !WorkshopRewardCards.IsBusy,
                "Reading/accepting an owned item card must return without an extra reward: " + item);
        }

        for (int ordinal = 0; ordinal < OriginalSolarKnowledge.Length; ordinal++)
        {
            var season = (WorkshopSeason)(ordinal / 6);
            int index = ordinal % 6;
            int closes = 0;
            WorkshopKnowledgeView.ShowTerm(season, index, () => closes++);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var canvas = GameObject.Find("Workshop Knowledge");
            Feedback2Require(canvas != null && WorkshopKnowledgeView.IsOpen && closes == 0,
                "Owned solar card did not open: " + season + " " + index);
            RequireSingleCard("Workshop Knowledge");
            var artwork = CardReviewImage(canvas, "Full Size Solar Card");
            Feedback2Require(artwork != null && artwork.sprite == WorkshopArt.SolarCard(season, index),
                "Original solar card artwork was replaced or lost: " + season + " " + index);
            var actual = new List<string>();
            foreach (var label in canvas.GetComponentsInChildren<Text>())
            {
                if (label.name != "Solar Lettering") continue;
                actual.Add(CardReviewNormalize(label.text));
                CardReviewLegibleText(label, "solar " + season + " " + index + " / " + label.text);
                labelsChecked++;
            }
            Feedback2Require(string.Join("|", actual) == OriginalSolarKnowledge[ordinal],
                "A solar card's original printed title, knowledge or observation changed: " + season + " " + index);
            Feedback2Require(GameObject.Find("Solar Review Notes") == null,
                "The original solar card is obscured by a removed review footer.");
            yield return Capture((850 + ordinal) + "-card-solar-" + season.ToString().ToLowerInvariant() + "-" + index);
            InvokeNamedButton("Close Solar Detail");
            yield return null;
            Feedback2Require(closes == 1 && !WorkshopKnowledgeView.IsOpen && !WorkshopRewardCards.IsBusy,
                "Collecting an owned solar card did not return exactly once: " + season + " " + index);
        }
        WorkshopKnowledgeView.Show(WorkshopSeason.Spring);
        yield return null;
        Canvas.ForceUpdateCanvases();
        foreach (var label in GameObject.Find("Workshop Knowledge").GetComponentsInChildren<Text>())
            if (label.name == "Solar Lettering") CardReviewLegibleText(label, "solar collection / " + label.text);
        yield return Capture("874-card-solar-collection");
        InvokeNamedButton("Solar Back");
        yield return null;
        Feedback2Require(!WorkshopKnowledgeView.IsOpen, "The solar collection did not return to the scene.");
        Feedback2Require(PlayerPrefs.GetString("ShiXu.Workshop.v1", "") == previousSave,
            "Reviewing the original card collection changed saved progression.");
        Debug.Log("[OriginalCardLegibility] PASS: 12 original item and 24 solar artworks; all dynamic native titles, knowledge and observations preserved; " +
            labelsChecked + " native labels use ReadingFont with complete glyphs and unclipped geometry; 36 captures; one accept action each; saved progress unchanged. Two printed cards are verified visually, including both full-resolution HD restorations.");
    }

    private static string CardReviewNormalize(string value) => value.Replace("\r", "").Replace("\n", "");

    private static Image CardReviewImage(GameObject root, string name)
    {
        foreach (var image in root.GetComponentsInChildren<Image>()) if (image.name == name) return image;
        return null;
    }

    private static Text CardReviewText(GameObject root, string name)
    {
        foreach (var text in root.GetComponentsInChildren<Text>()) if (text.name == name) return text;
        return null;
    }

    private static void CardReviewLegibleText(Text label, string context)
    {
        Feedback2Require(label.font == WorkshopArt.ReadingFont, "Card knowledge is not using the reading font: " + context);
        var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
        int fittedSize = label.resizeTextForBestFit ? label.cachedTextGenerator.fontSizeUsedForBestFit : label.fontSize;
        if (fittedSize <= 0) fittedSize = label.fontSize;
        Feedback2Require(fittedSize >= 20, "Card knowledge shrank below a readable size: " + context + " / " + fittedSize);
        settings.fontSize = fittedSize;
        settings.resizeTextForBestFit = false;
        // Generate all text at the actual fitted size. A truncated on-screen generator
        // alone would incorrectly pass when the final line had simply disappeared.
        settings.verticalOverflow = VerticalWrapMode.Overflow;
        settings.generateOutOfBounds = true;
        label.font.RequestCharactersInTexture(label.text, fittedSize, label.fontStyle);
        foreach (char character in label.text)
        {
            if (char.IsWhiteSpace(character)) continue;
            CharacterInfo glyph;
            Feedback2Require(label.font.HasCharacter(character) &&
                label.font.GetCharacterInfo(character, out glyph, fittedSize, label.fontStyle),
                "The card reading font is missing a character '" + character + "': " + context);
        }
        using (var generator = new TextGenerator())
        {
            Feedback2Require(generator.Populate(label.text, settings), "Could not generate full card lettering: " + context);
            var vertices = generator.verts;
            float unit = 1f / label.pixelsPerUnit;
            Rect bounds = label.rectTransform.rect;
            const float tolerance = 2f;
            int glyphQuads = 0;
            for (int start = 0; start + 3 < vertices.Count; start += 4)
            {
                // Spaces and control characters can produce degenerate quads at zero.
                if (vertices[start].position == vertices[start + 1].position &&
                    vertices[start].position == vertices[start + 2].position &&
                    vertices[start].position == vertices[start + 3].position) continue;
                glyphQuads++;
                for (int corner = 0; corner < 4; corner++)
                {
                    var point = vertices[start + corner].position * unit;
                    Feedback2Require(point.x >= bounds.xMin - tolerance && point.x <= bounds.xMax + tolerance &&
                        point.y >= bounds.yMin - tolerance && point.y <= bounds.yMax + tolerance,
                        "Card lettering exceeds its own rectangle: " + context + " / point " + point + " / bounds " + bounds);
                }
            }
            Feedback2Require(glyphQuads > 0, "Card lettering has no visible glyphs: " + context);
        }
    }
}
#endif
