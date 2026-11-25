using UnityEngine;

internal sealed class WorkshopSeasonNarrative
{
    public readonly string Issue;
    public readonly string TimeLabel;
    public readonly string AnnouncementTitle;
    public readonly string AnnouncementBody;
    public readonly string Craft;
    public readonly string Area;
    public readonly string ClosingTitle;
    public readonly string ClosingBody;
    public readonly string TransitionLabel;

    public WorkshopSeasonNarrative(string issue, string timeLabel, string announcementTitle,
        string announcementBody, string craft, string area, string closingTitle,
        string closingBody, string transitionLabel)
    {
        Issue = issue;
        TimeLabel = timeLabel;
        AnnouncementTitle = announcementTitle;
        AnnouncementBody = announcementBody;
        Craft = craft;
        Area = area;
        ClosingTitle = closingTitle;
        ClosingBody = closingBody;
        TransitionLabel = transitionLabel;
    }
}

internal sealed class WorkshopSeasonFieldNote
{
    internal readonly string Region;
    internal readonly string Scene;
    internal readonly string Condition;
    internal readonly string Response;
    internal readonly string Question;
    internal readonly string[] Choices;
    internal readonly int CorrectChoice;
    internal readonly string Feedback;
    internal readonly string Source;
    internal readonly string SourceUrl;

    internal WorkshopSeasonFieldNote(string region, string scene, string condition, string response,
        string question, string firstChoice, string secondChoice, int correctChoice, string feedback,
        string source, string sourceUrl)
    {
        Region = region;
        Scene = scene;
        Condition = condition;
        Response = response;
        Question = question;
        Choices = new[] { firstChoice, secondChoice };
        CorrectChoice = correctChoice;
        Feedback = feedback;
        Source = source;
        SourceUrl = sourceUrl;
    }
}

internal static class WorkshopNarrativeData
{
    private static readonly WorkshopSeasonNarrative[] Seasons =
    {
        new WorkshopSeasonNarrative(
            "春字第一号",
            "清明 · 谷雨",
            "清明已至 · 采桑给蚕",
            "清明前后回暖，桑芽舒展。拿篮接满掉落的嫩叶，小蚕交给师傅照料，再为下一批备好蚕种。",
            "接桑叶、备蚕种",
            "春 · 桑园与蚕房",
            "谷雨将尽 · 新茧入夏",
            "这批蚕渐渐长大，上蔟结茧。到夏季的蚕茧簇架择茧，再把丝头接到丝轴。",
            "清明 · 谷雨  →  芒种 · 夏至"),
        new WorkshopSeasonNarrative(
            "夏字第一号",
            "芒种 · 夏至",
            "芒种收茧 · 理顺丝路",
            "在蚕茧簇架择茧、转动线段接通丝路。收好盐和索绪，备水拾柴后去煮茧锅寻找线轴。",
            "择茧、接通丝路、寻找线轴",
            "夏 · 晾素轩与缫丝处",
            "夏季交付 · 生丝入库",
            "绕好的丝已经干妥，带着批签入库。秋分织坊会把这一轴丝摊开检查，再给寒露的织机备料。",
            "芒种 · 夏至  →  秋分 · 寒露"),
        new WorkshopSeasonNarrative(
            "秋字第一号",
            "秋分 · 寒露",
            "秋分借光 · 验丝备织",
            "秋分后白昼渐短，为夜织备丝时趁窗光看粗节和接头。寒露早凉，窗边沾露的丝先整备，合适的丝上机。",
            "验丝、整丝、理经织绸",
            "秋 · 灵桑阙与织造工坊",
            "寒露收梭 · 素绸入冬",
            "织好的整幅素绸和纹样记录一同入箧。立冬的染坊会从这匹绸留样头试色，再染正式衣料。",
            "秋分 · 寒露  →  立冬 · 小雪"),
        new WorkshopSeasonNarrative(
            "冬字第一号",
            "立冬 · 小雪",
            "立冬 · 栀子熟料进染坊",
            "立冬这班收到浙南第二批栀子染材。用成熟果料试栀黄，再染整绸。小雪遇冷雨，先选晾架，排裁夹衣。",
            "染布、裁衣、缝制",
            "冬 · 染料园与青染轩",
            "小雪衣片 · 冬至交衣",
            "小雪按客人的身量裁好前后片和袖片。大雪把这些衣片与素里绢合好，冬至试过抬手和合襟后交衣。",
            "小雪量裁  →  大雪缝衣  →  冬至试衣")
    };

    private static readonly WorkshopSeasonFieldNote[] FieldNotes =
    {
        new WorkshopSeasonFieldNote(
            "浙北桐乡", "桑园与蚕房", "清明前后渐暖，桑树抽嫩叶。谷雨前后的春雨也会打湿叶面。",
            "桑园接新叶，蚕房备蚕种。小蚕交给师傅照料。",
            "刚下过一场春雨，要给小蚕喂桑，先做什么？", "等叶面干，再采嫩叶", "趁叶上有雨水，马上采喂", 0,
            "嫩叶适合小蚕。让叶面保持干爽，再把新鲜桑叶带进蚕房。",
            "中国丝绸博物馆《清明至 蚕事启》", "https://www.chinasilkmuseum.com/cs/info_164.aspx?itemid=31631"),
        new WorkshopSeasonFieldNote(
            "浙江湖州南浔", "蚕蔟架与缫丝坊", "芒种前后江南春茧陆续收验，夏至前后可能遇梅雨。",
            "先按茧况分批。南浔辑里湖丝有煮茧、索绪、绕轴和烘丝的工序。",
            "梅雨天，刚绕好的丝还湿，可以直接堆进入库箱吗？", "直接放进箱子", "先避雨干丝，再入库", 1,
            "生丝要干妥后再收存。了解梅雨时节如何保存丝料。",
            "中国非遗网《辑里湖丝手工制作技艺》", "https://www.ihchina.cn/art/detail/id/14472.html"),
        new WorkshopSeasonFieldNote(
            "江苏苏州", "验丝窗台与织机", "秋分后白昼渐短，本坊趁亮验丝。寒露这一班窗沿见冷露。",
            "看经线是否齐整、松紧是否均匀，再穿梭引纬。本关用素绸练基础，苏州宋锦还能用经纬显花。",
            "上机后经线有的松、有的紧，应先做哪一步？", "校准织机，理齐经线", "多穿几趟梭子盖住松线", 0,
            "先把经线排齐并调匀松紧，梭子穿过时才更容易织出齐整布面。",
            "中国非遗网《宋锦织造技艺》", "https://www.ihchina.cn/art/detail/id/19995.html"),
        new WorkshopSeasonFieldNote(
            "江南工坊", "染坊与裁衣间", "温栀子在十一月有第二批采果，小雪冷空气活动增多。本坊从熟果试色，赶做夹衣。",
            "选成熟栀子染材，验干样再染整绸。冷雨天找避雨通风处晾布，排衣片和里襟。",
            "这班遇冷雨，染绸该在哪里晾？", "露天晾架", "避雨通风的晾架", 1,
            "先晾干再下剪，夹衣工单还要为里襟留整料。",
            "中国丝绸博物馆植物染料研究", "https://www.chinasilkmuseum.com/kyjd/info_30.aspx?itemid=5473")
    };

    public static WorkshopSeasonNarrative For(WorkshopSeason season)
    {
        return Seasons[Mathf.Clamp((int)season, 0, Seasons.Length - 1)];
    }

    internal static WorkshopSeasonFieldNote FieldNote(WorkshopSeason season) => FieldNotes[(int)season];

    public static string SeasonShort(WorkshopSeason season)
    {
        switch (season)
        {
            case WorkshopSeason.Spring: return "春";
            case WorkshopSeason.Summer: return "夏";
            case WorkshopSeason.Autumn: return "秋";
            default: return "冬";
        }
    }

    public static string SeasonTime(WorkshopSeason season) => For(season).TimeLabel.Replace(" · ", "\n");
}
