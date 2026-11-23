using System;
using UnityEngine;

public sealed partial class WorkshopKnowledgeView
{
    private static readonly Rect SolarDetailBounds = new Rect(195, 74, 1530, 862);

    private readonly struct SolarPrint
    {
        internal readonly Rect Bounds;
        internal readonly string Text;
        internal readonly Color32 Background;
        internal readonly Color32 Ink;
        internal readonly bool Centered;

        internal SolarPrint(int x, int y, int width, int height, string text, uint background, uint ink,
            bool centered = false)
        {
            Bounds = new Rect(x, y, width, height);
            Text = text;
            Background = Rgb(background);
            Ink = Rgb(ink);
            Centered = centered;
        }

        private static Color32 Rgb(uint value) => new Color32(
            (byte)(value >> 16), (byte)(value >> 8), (byte)value, 255);
    }

    private void RenderSolarDetail(Sprite artwork, WorkshopSeason termSeason, int index, string title, Action goBack)
    {
        StartPage(goBack);
        CenterArt(page, "Full Size Solar Card", SolarDetailBounds, artwork);
        var printed = SolarPrints[(int)termSeason * 6 + index];
        foreach (var region in printed)
        {
            bool acknowledgement = region.Text == "知道啦";
            DrawSolarPrint(page, SolarDetailBounds, region, region.Text == null ? title : region.Text);
            if (acknowledgement)
            {
                Rect hitbox = region.Bounds;
                hitbox.xMin -= 40;
                hitbox.xMax += 40;
                hitbox.yMin -= 14;
                hitbox.yMax += 14;
                WorkshopUi.Button(page, "Close Solar Detail", ScaleSolarRect(SolarDetailBounds, hitbox),
                    "", goBack, Color.clear);
            }
        }
    }

    private static void DrawSolarAlmanacLabels(Transform parent, Rect bounds, Texture2D artwork)
    {
        foreach (var region in AlmanacPrints)
        {
            if (region.Text == "返回")
                DrawSolarPrint(parent, bounds, region, region.Text, artwork, new Rect(1216, 828, 20, 52));
            else
                DrawSolarPrint(parent, bounds, region, region.Text);
        }
    }

    private static void DrawSolarPrint(Transform parent, Rect bounds, SolarPrint region, string content,
        Texture2D artwork = null, Rect sample = default)
    {
        Rect line = ScaleSolarRect(bounds, region.Bounds);
        RectTransform backing;
        if (artwork != null)
        {
            backing = WorkshopUi.Rect(parent, "Solar Lettering Surface", line);
            var paper = backing.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            paper.texture = artwork;
            paper.uvRect = new Rect(sample.x / artwork.width, 1f - sample.yMax / artwork.height,
                sample.width / artwork.width, sample.height / artwork.height);
            paper.raycastTarget = false;
        }
        else
        {
            var paper = WorkshopUi.Fill(parent, "Solar Lettering Surface", line, region.Background);
            paper.raycastTarget = false;
            backing = paper.rectTransform;
        }

        var labelBounds = new Rect(1f, -2f, line.width - 2f, line.height + 4f);
        bool vertical = content.Contains("\n");
        int size = Mathf.RoundToInt(Mathf.Min(line.width, line.height / (vertical ? 2 : 1)) * 0.96f);
        var label = Label(backing, "Solar Lettering", labelBounds, content, size, region.Ink,
            region.Centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(12, size * 3 / 5);
        label.resizeTextMaxSize = size;
        label.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private static Rect ScaleSolarRect(Rect art, Rect source) => new Rect(
        art.x + source.x * art.width / 1672f, art.y + source.y * art.height / 941f,
        source.width * art.width / 1672f, source.height * art.height / 941f);

    // Coordinates follow the lettering in the delivered solar card artwork.
    private static readonly SolarPrint[][] SolarPrints =
    {
        new[] // 立春
        {
            new SolarPrint(715, 60, 254, 112, null, 0x476041, 0xFCE9D1, true),
            new SolarPrint(717, 247, 250, 56, "二十四节气", 0xFCE8D0, 0x462C1A, true),
            new SolarPrint(91, 340, 94, 113, "春", 0xFCE8D0, 0x435538, true),
            new SolarPrint(774, 351, 561, 59, "立春意味着春季开篇，", 0xFCEAD4, 0x462C1A),
            new SolarPrint(777, 421, 719, 58, "但各地并不会立刻暖和起来。", 0xFCEAD4, 0x452B1A),
            new SolarPrint(823, 530, 279, 59, "小织匠观察", 0xB84A3A, 0xFCEACF, true),
            new SolarPrint(773, 606, 677, 60, "留意枝头新芽与空气的变化。", 0xFCE9D4, 0x442A19),
            new SolarPrint(709, 763, 263, 78, "知道啦", 0xC1513D, 0xFDEBD2, true),
        },
        new[] // 雨水
        {
            new SolarPrint(665, 61, 350, 71, "二十四节气", 0x405945, 0xF9E9CA, true),
            new SolarPrint(719, 240, 230, 98, null, 0xFBE5C8, 0x3B4D34, true),
            new SolarPrint(54, 334, 114, 125, "春", 0xFBE5C9, 0x3E5036, true),
            new SolarPrint(756, 359, 570, 55, "雨水时节，许多地方降水", 0xFCE7CB, 0x593A25),
            new SolarPrint(756, 414, 572, 57, "逐渐增多，田间开始积蓄", 0xFCE6CA, 0x583924),
            new SolarPrint(761, 471, 286, 51, "春天的水分。", 0xFCE7CB, 0x5A3B25),
            new SolarPrint(845, 553, 229, 48, "小织匠观察", 0x4A5E42, 0xFAEBC9, true),
            new SolarPrint(911, 637, 418, 47, "看看土地是否变得湿润。", 0xFCE7CC, 0x573722),
            new SolarPrint(709, 780, 252, 71, "知道啦", 0xC85238, 0xFDEFD4, true),
        },
        new[] // 惊蛰
        {
            new SolarPrint(733, 70, 221, 102, null, 0x3B4F2F, 0xFBE6C2, true),
            new SolarPrint(719, 176, 252, 53, "二十四节气", 0xFCE9C9, 0x4B2C17, true),
            new SolarPrint(740, 337, 670, 58, "天气渐暖，冬日藏身的昆虫逐", 0xFDE8C9, 0x422410),
            new SolarPrint(86, 374, 103, 125, "春", 0xFCE8C7, 0x3B502D, true),
            new SolarPrint(740, 405, 689, 54, "渐活跃。“惊蛰”并非处处都打雷。", 0xFDE7C9, 0x41220F),
            new SolarPrint(849, 542, 253, 52, "小织匠观察", 0xA83629, 0xFDEFD2, true),
            new SolarPrint(853, 625, 513, 47, "仔细寻找草丛中的小生命。", 0xFDE7C6, 0x412310),
            new SolarPrint(714, 763, 256, 77, "知道啦", 0xBC382E, 0xFCE9C9, true),
        },
        new[] // 春分
        {
            new SolarPrint(671, 111, 341, 70, "二十四节气", 0x325230, 0xFBF2D7, true),
            new SolarPrint(1002, 284, 183, 70, null, 0x365736, 0xF9ECCD, true),
            new SolarPrint(84, 324, 95, 110, "春", 0xFAE4C2, 0x2F502F, true),
            new SolarPrint(774, 374, 623, 57, "春分前后，白天与黑夜长短", 0xFCE6C6, 0x452612),
            new SolarPrint(774, 436, 657, 52, "接近，阳光带来新的生长节奏。", 0xFBE5C6, 0x432411),
            new SolarPrint(848, 555, 237, 55, "小织匠观察", 0x335632, 0xF8F0D0, true),
            new SolarPrint(819, 627, 530, 51, "观察日影，感受白昼变长。", 0xFCE7C7, 0x462714),
            new SolarPrint(719, 773, 234, 70, "知道啦", 0xBA462D, 0xFDF2CF, true),
        },
        new[] // 清明
        {
            new SolarPrint(694, 75, 279, 126, null, 0x4C6244, 0xFBECD0, true),
            new SolarPrint(396, 276, 238, 45, "二十四节气", 0xAD3F30, 0xFBEED0, true),
            new SolarPrint(1509, 333, 93, 109, "春", 0xFCE9CC, 0x42563B, true),
            new SolarPrint(219, 356, 597, 48, "清明时节，许多地方气温回升，", 0xFBE5C5, 0x422A1B),
            new SolarPrint(218, 409, 619, 50, "草木转绿，是观察桑芽的好时候。", 0xFCE5C5, 0x40291A),
            new SolarPrint(335, 521, 236, 52, "小织匠观察", 0x465B40, 0xFAF1D4, true),
            new SolarPrint(224, 607, 583, 48, "在桑园寻找健康、柔嫩的叶片。", 0xFCE8CA, 0x402A1B),
            new SolarPrint(700, 745, 249, 71, "知道啦", 0xBF4832, 0xFDF2D6, true),
        },
        new[] // 谷雨
        {
            new SolarPrint(657, 71, 357, 76, "二十四节气", 0x485B3A, 0xFBECCE, true),
            new SolarPrint(967, 253, 177, 76, null, 0xFBDDBA, 0x39492C, true),
            new SolarPrint(124, 305, 106, 121, "春", 0xFBE4C0, 0x394A2D, true),
            new SolarPrint(787, 349, 518, 51, "谷雨前后雨水渐丰，利于", 0xFCE5C1, 0x502F17),
            new SolarPrint(788, 404, 525, 47, "不少作物生长。不同地区", 0xFCE5C1, 0x4E2E17),
            new SolarPrint(787, 454, 322, 52, "农时并不相同。", 0xFCE4C1, 0x512F17),
            new SolarPrint(848, 534, 231, 53, "小织匠观察", 0x475738, 0xF8E9C4, true),
            new SolarPrint(807, 606, 515, 48, "查看桑园与蚕房的春日准备。", 0xFDE4BF, 0x4F2F18),
            new SolarPrint(722, 749, 237, 77, "知道啦", 0xC34B33, 0xFDF5DE, true),
        },
        new[] // 立夏
        {
            new SolarPrint(721, 69, 232, 106, null, 0xF8D5AA, 0x512715, true),
            new SolarPrint(724, 186, 227, 45, "二十四节气", 0x415753, 0xF8E2BF, true),
            new SolarPrint(91, 244, 80, 145, "立\n夏", 0xFBD6A7, 0x532B17, true),
            new SolarPrint(430, 446, 880, 58, "立夏标志夏季开始，气温总体升高，", 0xFBDDBA, 0x4A2513),
            new SolarPrint(429, 505, 502, 61, "农事进入忙碌时段。", 0xFBDDBA, 0x4D2815),
            new SolarPrint(362, 631, 248, 60, "小织匠观察", 0x9B3D2F, 0xFBE4BF, true),
            new SolarPrint(710, 633, 654, 54, "看看桑叶有没有长得更繁密。", 0xFBDEBB, 0x4C2714),
            new SolarPrint(718, 768, 242, 71, "知道啦", 0xBE452D, 0xFCE4C0, true),
        },
        new[] // 小满
        {
            new SolarPrint(327, 184, 256, 113, null, 0xFCDDB4, 0x401F10, true),
            new SolarPrint(73, 220, 103, 111, "夏", 0xFCDEB2, 0x402010, true),
            new SolarPrint(759, 230, 227, 50, "二十四节气", 0xAE3A28, 0xFAE8CA, true),
            new SolarPrint(255, 363, 656, 60, "小满原指夏熟作物的籽粒", 0xFCDFB9, 0x3C1D0C),
            new SolarPrint(253, 429, 634, 60, "逐渐饱满，尚未完全成熟。", 0xFCDFB9, 0x3D1E0D),
            new SolarPrint(326, 563, 228, 49, "小织匠观察", 0xA83726, 0xFDEFD5, true),
            new SolarPrint(265, 627, 701, 59, "观察田间作物如何慢慢结实。", 0xFCDEB7, 0x3B1D0C),
            new SolarPrint(703, 764, 262, 77, "知道啦", 0xBE3D29, 0xFCEACD, true),
        },
        new[] // 芒种
        {
            new SolarPrint(705, 70, 269, 108, null, 0xFADDB0, 0x512814, true),
            new SolarPrint(714, 194, 279, 45, "二十四节气", 0xA83728, 0xFCE4BB, true),
            new SolarPrint(296, 494, 941, 58, "芒种前后，适宜有芒作物收获与播种，", 0xFCE3BD, 0x401F0A),
            new SolarPrint(296, 561, 505, 57, "是农忙的重要时段。", 0xFCE3BD, 0x40210B),
            new SolarPrint(381, 676, 251, 59, "小织匠观察", 0xA43825, 0xFCE5BA, true),
            new SolarPrint(739, 676, 542, 58, "留意田野里的农事活动。", 0xFCE3BD, 0x3E1E09),
            new SolarPrint(706, 789, 267, 80, "知道啦", 0xBE3C2B, 0xFCE5BB, true),
        },
        new[] // 夏至
        {
            new SolarPrint(376, 123, 227, 123, null, 0xFCE1B7, 0x5F2D17, true),
            new SolarPrint(763, 194, 248, 48, "二十四节气", 0xB03528, 0xFDEDC7, true),
            new SolarPrint(223, 306, 599, 58, "夏至前后，北半球迎来一", 0xFCE7C6, 0x5E3018),
            new SolarPrint(223, 368, 520, 58, "年中白昼最长的时段，", 0xFCE7C6, 0x5B2D15),
            new SolarPrint(223, 428, 371, 61, "暑热尚会继续。", 0xFCE7C7, 0x5A2B15),
            new SolarPrint(303, 545, 245, 55, "小织匠观察", 0xAC3B29, 0xFBEAC8, true),
            new SolarPrint(237, 623, 638, 54, "看看太阳升得多高，日影多短。", 0xFCE7C6, 0x5E2F17),
            new SolarPrint(726, 753, 220, 68, "知道啦", 0xC4432D, 0xFDF0CF, true),
        },
        new[] // 小暑
        {
            new SolarPrint(724, 87, 232, 114, null, 0xFCD9A4, 0x5A2915, true),
            new SolarPrint(1174, 161, 251, 53, "二十四节气", 0xFDDFB3, 0x5B2E18, true),
            new SolarPrint(704, 304, 732, 65, "小暑意味着天气开始更加炎热，", 0xFCE3BF, 0x522712),
            new SolarPrint(705, 379, 578, 58, "照料蚕房要关注温湿度。", 0xFCE4BF, 0x502611),
            new SolarPrint(768, 483, 246, 58, "小织匠观察", 0xA43927, 0xFCEDCD, true),
            new SolarPrint(724, 567, 614, 58, "寻找阴凉处，观察蚕房通风。", 0xFCE3BE, 0x532712),
            new SolarPrint(955, 738, 229, 75, "知道啦", 0xD14A31, 0xFDEBC4, true),
        },
        new[] // 大暑
        {
            new SolarPrint(711, 71, 252, 117, null, 0xFCE4B9, 0x572A18, true),
            new SolarPrint(716, 229, 227, 46, "二十四节气", 0xAF382C, 0xFBECC1, true),
            new SolarPrint(787, 330, 675, 54, "大暑常是一年中暑热突出的", 0xFCE7C1, 0x43200F),
            new SolarPrint(784, 392, 678, 55, "时期。养蚕和存茧都需注意", 0xFCE7C1, 0x421F0F),
            new SolarPrint(783, 453, 263, 60, "环境条件。", 0xFCE7C1, 0x442112),
            new SolarPrint(841, 554, 247, 55, "小织匠观察", 0xFCD9A2, 0x791E15, true),
            new SolarPrint(786, 630, 666, 54, "收茧前看看工坊有没有降温。", 0xFCE6C1, 0x401D0D),
            new SolarPrint(726, 776, 229, 71, "知道啦", 0xCF4A36, 0xFDEDC5, true),
        },
        new[] // 立秋
        {
            new SolarPrint(723, 57, 234, 94, null, 0x133736, 0xFAE1BF, true),
            new SolarPrint(693, 167, 286, 61, "二十四节气", 0xFADDB9, 0x4A250A, true),
            new SolarPrint(209, 306, 489, 63, "立秋表示秋季开始，", 0xFCE5C7, 0x41210B),
            new SolarPrint(210, 381, 622, 58, "但不少地方仍有明显暑热。", 0xFCE5C7, 0x3D1D06),
            new SolarPrint(281, 483, 275, 60, "小织匠观察", 0x1F403B, 0xF8E5C3, true),
            new SolarPrint(208, 569, 614, 54, "找找桑园叶色是否开始变化。", 0xFCE5C7, 0x3C1C06),
            new SolarPrint(479, 705, 215, 64, "知道啦", 0xBB4926, 0xFDECCB, true),
        },
        new[] // 处暑
        {
            new SolarPrint(732, 47, 204, 86, null, 0x193F40, 0xF9EFD8, true),
            new SolarPrint(713, 227, 244, 53, "二十四节气", 0xFCE5BD, 0x412412, true),
            new SolarPrint(493, 321, 668, 62, "处暑表示暑热逐步减弱，", 0xFCEACE, 0x3B1F0D),
            new SolarPrint(477, 398, 725, 61, "早晚温度常开始出现差别。", 0xFBEACD, 0x3B1E0C),
            new SolarPrint(698, 509, 276, 56, "小织匠观察", 0xFCE2B9, 0x3C1D0C, true),
            new SolarPrint(536, 595, 596, 61, "感受清晨与午后的不同。", 0xFCEACD, 0x3A1E0C),
            new SolarPrint(716, 733, 240, 74, "知道啦", 0x1B4345, 0xFBF1D9, true),
        },
        new[] // 白露
        {
            new SolarPrint(735, 45, 212, 91, null, 0x293F3B, 0xF9E6CA, true),
            new SolarPrint(709, 144, 250, 52, "二十四节气", 0xFBE4C3, 0x43230F, true),
            new SolarPrint(234, 436, 1229, 58, "白露时节，气温逐渐下降，清晨草叶上容易看到露珠。", 0xFCE7CA, 0x3D1E0D),
            new SolarPrint(709, 563, 261, 58, "小织匠观察", 0x314E4A, 0xF8E7CB, true),
            new SolarPrint(573, 647, 520, 55, "在清晨场景寻找露珠。", 0xFBE7CB, 0x3D1F0E),
            new SolarPrint(715, 774, 253, 74, "知道啦", 0xBC4626, 0xFCEBCE, true),
        },
        new[] // 秋分
        {
            new SolarPrint(349, 86, 256, 110, null, 0x213C3E, 0xFBE8CC, true),
            new SolarPrint(780, 159, 255, 53, "二十四节气", 0xFAE4C5, 0x672214, true),
            new SolarPrint(244, 309, 623, 62, "秋分前后昼夜长短接近，", 0xFCE7CD, 0x371B08),
            new SolarPrint(244, 384, 627, 62, "田野慢慢转入深秋色彩。", 0xFCE7CC, 0x381B08),
            new SolarPrint(244, 482, 307, 62, "小织匠观察", 0xFCE6CB, 0x3B1F0C, true),
            new SolarPrint(244, 555, 626, 64, "留意白天和夜晚的变化。", 0xFCE7CC, 0x381D0A),
            new SolarPrint(650, 693, 249, 77, "知道啦", 0xA43621, 0xFCEDCC, true),
        },
        new[] // 寒露
        {
            new SolarPrint(828, 50, 240, 108, null, 0x153C45, 0xFAE7CA, true),
            new SolarPrint(813, 179, 270, 60, "二十四节气", 0xFCE0BB, 0x43220E, true),
            new SolarPrint(591, 324, 746, 64, "寒露时节气温进一步下降，", 0xFCE7CF, 0x3B1C0A),
            new SolarPrint(591, 399, 754, 69, "清晨的露水带来明显凉意。", 0xFCE7CF, 0x3A1B09),
            new SolarPrint(852, 518, 309, 62, "小织匠观察", 0x194047, 0xFAEACF, true),
            new SolarPrint(631, 615, 784, 61, "寻找秋叶与凝在叶尖的水珠。", 0xFCE8D0, 0x3A1C0B),
            new SolarPrint(865, 753, 235, 71, "知道啦", 0xB23F2A, 0xFDEDD0, true),
        },
        new[] // 霜降
        {
            new SolarPrint(623, 54, 386, 65, "二十四节气", 0x223F3F, 0xF5E3C6, true),
            new SolarPrint(338, 168, 230, 120, null, 0xFCE2C1, 0x40200F, true),
            new SolarPrint(824, 257, 576, 57, "霜降提示天气继续转冷，", 0xFBE4C9, 0x3F210D),
            new SolarPrint(824, 321, 603, 55, "一些地方开始见霜，但不", 0xFBE5CA, 0x3E1F0C),
            new SolarPrint(825, 386, 355, 56, "是每地都有霜。", 0xFBE6CB, 0x3F210D),
            new SolarPrint(866, 486, 298, 55, "小织匠观察", 0xB7482A, 0xFDE7B9, true),
            new SolarPrint(824, 571, 574, 55, "观察枝叶是否出现霜白。", 0xFBE6CC, 0x40210E),
            new SolarPrint(1044, 707, 232, 71, "知道啦", 0x2F5A51, 0xFAEBCC, true),
        },
        new[] // 立冬
        {
            new SolarPrint(740, 114, 198, 100, null, 0xFBDDBD, 0x4F282C, true),
            new SolarPrint(512, 251, 198, 46, "二十四节气", 0xFADDBC, 0x492231, true),
            new SolarPrint(354, 336, 471, 53, "立冬意味着冬季开篇，", 0xFCECD8, 0x452A1D),
            new SolarPrint(354, 395, 439, 51, "许多地方进入冷季，", 0xFCEBD7, 0x462B1E),
            new SolarPrint(354, 451, 480, 54, "衣物也更加讲究保暖。", 0xFCEBD7, 0x42281C),
            new SolarPrint(421, 544, 213, 52, "小织匠观察", 0xFAD3A9, 0x4F2631, true),
            new SolarPrint(335, 613, 582, 61, "想想丝绸怎样变成可穿的衣裳。", 0xFCEBD7, 0x442A1E),
            new SolarPrint(510, 730, 206, 67, "知道啦", 0xC24B43, 0xFDE9C8, true),
        },
        new[] // 小雪
        {
            new SolarPrint(737, 87, 209, 95, null, 0xFBE4CC, 0x4C1E2A, true),
            new SolarPrint(727, 220, 231, 53, "二十四节气", 0xFCDBB0, 0x4C1F2A, true),
            new SolarPrint(500, 307, 664, 52, "小雪表示寒冷加重的时节，", 0xFCEAD7, 0x481B23),
            new SolarPrint(469, 365, 715, 55, "并不意味着当天一定会下雪。", 0xFCEBD8, 0x4A1C25),
            new SolarPrint(714, 451, 271, 49, "小织匠观察", 0xFCD8A8, 0x481B27, true),
            new SolarPrint(515, 519, 641, 48, "看看工坊如何保存染料与布料。", 0xFCEBD9, 0x471B24),
            new SolarPrint(726, 774, 215, 63, "知道啦", 0xC04A44, 0xFCEACF, true),
        },
        new[] // 大雪
        {
            new SolarPrint(745, 96, 181, 88, null, 0xFCE6CB, 0x431C15, true),
            new SolarPrint(726, 199, 218, 48, "二十四节气", 0xFCE1BD, 0x411D14, true),
            new SolarPrint(341, 487, 1003, 51, "大雪前后气温常较低，北方一些地区积雪增多。", 0xFCEAD4, 0x3B180F),
            new SolarPrint(374, 576, 250, 53, "小织匠观察", 0xFCDBB1, 0x3F1C14, true),
            new SolarPrint(352, 647, 493, 58, "观察树枝与屋檐上的雪。", 0xFCEAD5, 0x36150E),
            new SolarPrint(732, 764, 216, 67, "知道啦", 0xBE443F, 0xFDECCD, true),
        },
        new[] // 冬至
        {
            new SolarPrint(314, 97, 228, 107, null, 0xFCE8CF, 0x4F2428, true),
            new SolarPrint(709, 126, 242, 52, "二十四节气", 0xFCE4C7, 0x45241F, true),
            new SolarPrint(250, 312, 624, 55, "冬至前后，北半球白昼最短。", 0xFCE9D2, 0x43241A),
            new SolarPrint(253, 378, 541, 52, "此后白昼开始缓缓增长。", 0xFCE9D3, 0x422319),
            new SolarPrint(356, 491, 248, 55, "小织匠观察", 0xFCD6A7, 0x4A1D2D, true),
            new SolarPrint(294, 578, 525, 52, "看看冬日光影怎样变化。", 0xFCEAD3, 0x402219),
            new SolarPrint(675, 731, 226, 70, "知道啦", 0xB8423F, 0xFDE9C7, true),
        },
        new[] // 小寒
        {
            new SolarPrint(752, 61, 224, 102, null, 0xFCE2C3, 0x460C21, true),
            new SolarPrint(1171, 123, 239, 52, "二十四节气", 0xFCDDBA, 0x420B1F, true),
            new SolarPrint(623, 283, 781, 67, "小寒处于一年中较冷的时段，", 0xFCE8D3, 0x462517),
            new SolarPrint(626, 361, 726, 62, "人们重视保暖与冬季生活。", 0xFCE9D4, 0x432316),
            new SolarPrint(733, 479, 286, 58, "小织匠观察", 0xFAD6AD, 0x460F24, true),
            new SolarPrint(676, 575, 607, 55, "寻找工坊里温暖的物件。", 0xFCE9D5, 0x422215),
            new SolarPrint(828, 704, 223, 69, "知道啦", 0xBF4D46, 0xFDE9C7, true),
        },
        new[] // 大寒
        {
            new SolarPrint(747, 72, 247, 107, null, 0x41426D, 0xFCE7C9, true),
            new SolarPrint(758, 189, 217, 45, "二十四节气", 0xFCE5C6, 0x451228, true),
            new SolarPrint(796, 321, 579, 57, "大寒是二十四节气的最后", 0xFCECD9, 0x3D1F0E),
            new SolarPrint(800, 386, 590, 52, "一个节气，也意味着又一轮", 0xFCECD9, 0x402312),
            new SolarPrint(799, 443, 236, 60, "四季将至。", 0xFCECDA, 0x3F200E),
            new SolarPrint(879, 534, 238, 55, "小织匠观察", 0xF8D6B0, 0x441028, true),
            new SolarPrint(803, 608, 528, 54, "回到主厅，回顾四时劳动。", 0xFCECD9, 0x412312),
            new SolarPrint(882, 715, 203, 61, "知道啦", 0xBA443C, 0xFDEBC8, true),
        },
    };

    private static readonly SolarPrint[] AlmanacPrints =
    {
        new SolarPrint(663, 87, 356, 75, "二十四节气", 0xF8DFBF, 0x402513, true),
        new SolarPrint(364, 277, 61, 65, "春", 0xF9E4C7, 0x422612, true),
        new SolarPrint(366, 410, 55, 61, "夏", 0xF9E5CA, 0x422410, true),
        new SolarPrint(363, 550, 57, 54, "秋", 0xF6E1C2, 0x482C19, true),
        new SolarPrint(363, 686, 57, 55, "冬", 0xF8E4C8, 0x452B18, true),
        new SolarPrint(324, 830, 128, 53, "已收录", 0xF9E2C7, 0x361C0D, true),
        new SolarPrint(536, 827, 134, 57, "待发现", 0xF8E1C6, 0x3D2213, true),
        new SolarPrint(1239, 828, 103, 52, "返回", 0xFCDD88, 0x391E0C, true),
    };
}
