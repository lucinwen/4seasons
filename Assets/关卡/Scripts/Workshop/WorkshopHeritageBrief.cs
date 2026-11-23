using System;
using UnityEngine;
using UnityEngine.UI;

internal static class WorkshopHeritageBrief
{
    internal const string OpeningHook =
        "新华网曾报道，湖州南浔的顾明琪仍在古镇展示手工缫丝；清明时节，湖州也延续着祈盼蚕桑丰收的“轧蚕花”。你将作为织坊学徒，循节气观察桑叶和蚕的变化，亲手把一枚茧变成一件衣。";

    private static readonly Color PaleGold = new Color32(233, 215, 176, 255);
    private static readonly Color SourcePaper = new Color32(252, 243, 222, 255);

    private static readonly (string Publisher, string Title, string Url)[] Sources =
    {
        ("新华网 · 2024.04.28", "湖丝细线牵八钱 古镇里的非遗传承",
            "https://www.news.cn/culture/20240428/645654267e7744dcaabc374fcd02033e/c.html"),
        ("新华网 · 2025.04.05", "浙江湖州：“轧蚕花”民俗活动 展非遗文化",
            "https://www.news.cn/ci/20250405/f441ecc359be4748b96676418390d8ee/c.html"),
        ("中国丝绸博物馆", "丝语丨清明至 蚕事启",
            "https://www.chinasilkmuseum.com/cs/info_164.aspx?itemid=31631"),
        ("经济参考报 · 2024.04.08", "“国潮风”下的宋锦复兴",
            "https://www.news.cn/20240408/fc60fb2ad7c24c398977b04fb092ec33/c.html"),
        ("联合国教科文组织 · 2009年列入", "中国蚕桑丝织技艺 · 人类非遗代表作名录",
            "https://ich.unesco.org/en/RL/sericulture-and-silk-craftsmanship-of-china-00197")
    };

    internal static Selectable[] Open(Transform parent, Action onClose)
    {
        var page = WorkshopUi.PixelCard(parent, "Heritage Brief", new Rect(263f, 115f, 1394f, 850f),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        Label(page, "Brief Title", new Rect(64f, 46f, 1200f, 69f),
            "一件丝衣，为什么值得传下去？", 49, WorkshopUi.Ink);
        WorkshopUi.Fill(page, "Header Rule", new Rect(64f, 136f, 1266f, 2f), WorkshopUi.Bronze);

        var story = WorkshopUi.PixelCard(page, "Story and Safeguarding", new Rect(64f, 168f, 582f, 540f),
            SourcePaper, PaleGold);
        Label(story, "Story Heading", new Rect(28f, 24f, 526f, 40f), "从新闻走进织坊", 30, WorkshopUi.Green);
        Label(story, "Story Body", new Rect(28f, 78f, 526f, 155f), OpeningHook, 25, WorkshopUi.Ink);

        WorkshopUi.Fill(story, "Story Rule", new Rect(28f, 232f, 526f, 2f), PaleGold);
        Label(story, "Safeguarding Heading", new Rect(28f, 251f, 526f, 42f),
            "为什么要传承？", 30, WorkshopUi.Green);
        Label(story, "Safeguarding Body", new Rect(28f, 305f, 526f, 215f),
            "丝衣背后有一整套地方知识：桑叶怎样采，蚕怎样养，煮茧时怎样掌握水温，织机上如何安排经纬。蚕桑民俗也寄托着对丰收的期盼。\n\n苏州宋锦的古织机曾由传承人带学生复原。留下工具、记录操作，让新人跟着练习，工艺才有机会继续使用和发展。",
            25, WorkshopUi.Ink);

        Label(page, "Sources Heading", new Rect(692f, 171f, 638f, 44f),
            "新闻与资料来源", 32, WorkshopUi.Green);
        var back = WorkshopUi.PixelButton(page, "Return to cover", new Rect(1098f, 746f, 232f, 64f),
            "返回封面", onClose, true, 30);
        var navigation = new Selectable[Sources.Length + 1];
        navigation[0] = back;

        for (int index = 0; index < Sources.Length; index++)
        {
            var source = Sources[index];
            var bounds = new Rect(692f, 229f + 96f * index, 638f, 84f);
            var button = WorkshopUi.Button(page, "Source " + (index + 1), bounds, "",
                () => Application.OpenURL(source.Url), PaleGold);
            Label(button.transform, "Publisher", new Rect(17f, 9f, 470f, 24f),
                source.Publisher, 20, WorkshopUi.Faded);
            Label(button.transform, "Article", new Rect(17f, 37f, 535f, 34f),
                source.Title, 24, WorkshopUi.Ink);
            Label(button.transform, "Open Article", new Rect(557f, 40f, 66f, 31f),
                "原文", 23, WorkshopUi.Green);
            navigation[index + 1] = button;
        }

        Label(page, "Story Scope", new Rect(65f, 750f, 980f, 50f),
            "游戏按四季编排工序。各地蚕期随当地气候、桑叶和蚕的生长情况而不同。",
            23, WorkshopUi.Faded);
        return navigation;
    }

    private static void Label(Transform parent, string name, Rect bounds, string value, int size, Color color)
    {
        var label = WorkshopUi.Label(parent, name, bounds, value, size, color, TextAnchor.UpperLeft);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.lineSpacing = 1.12f;
    }
}
