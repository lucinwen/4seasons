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

    public WorkshopSeasonNarrative(string issue, string timeLabel, string announcementTitle,
        string announcementBody, string craft, string area, string closingTitle,
        string closingBody)
    {
        Issue = issue;
        TimeLabel = timeLabel;
        AnnouncementTitle = announcementTitle;
        AnnouncementBody = announcementBody;
        Craft = craft;
        Area = area;
        ClosingTitle = closingTitle;
        ClosingBody = closingBody;
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
            "清明前后回暖，桑芽舒展。带篮到桑树前采摘嫩叶，喂饱小蚕，再为下一批备好蚕种。",
            "采桑喂蚕、备蚕种",
            "春 · 桑园与蚕房",
            "谷雨将尽 · 新茧入夏",
            "这批蚕渐渐长大，上蔟结茧。到夏季的蚕茧簇架择茧，再把丝头接到丝轴。"),
        new WorkshopSeasonNarrative(
            "夏字第一号",
            "芒种 · 夏至",
            "芒种收茧 · 理顺丝路",
            "在蚕茧簇架择茧、转动线段接通丝路。收好盐和索绪，备水拾柴后去煮茧锅寻找线轴。",
            "择茧、接通丝路、寻找线轴",
            "夏 · 晾素轩与缫丝处",
            "夏季交付 · 生丝入库",
            "绕好的丝已经干妥，带着批签入库，给寒露的织机备料。"),
        new WorkshopSeasonNarrative(
            "秋字第一号",
            "寒露 · 立冬",
            "寒露纺织 · 秋色染布",
            "修好织机，寒露纺织生坯绸。再领取糕点、备齐贡品酬蚕；之后拾锄采集染材、研磨染料并完成秋色染布，回工坊交付秋季成果。",
            "纺织、采集、研磨、染色、染布",
            "秋 · 灵桑阙、织造工坊与染料园",
            "秋色染布 · 交付秋季成果",
            "纺织、采染、研磨、染色与酬蚕均已完成。带梭子、生坯绸、土地公和秋色染布回主厅交付，再把染布送往冬季裁衣。"),
        new WorkshopSeasonNarrative(
            "冬字第一号",
            "小雪 · 冬至",
            "小雪裁衣 · 缝制冬衣",
            "秋季染好的秋色布已经备妥。到裁衣台按样板裁出前后片和袖片，拾缝衣针，再回工坊缝制成衣。",
            "裁衣、缝制",
            "冬 · 青染轩与裁衣间",
            "小雪衣片 · 冬至交衣",
            "小雪按样板裁好前后片和袖片。大雪把这些衣片与素里绢合好，冬至试过抬手和合襟后交衣。")
    };

    private static readonly WorkshopSeasonFieldNote[] FieldNotes =
    {
        new WorkshopSeasonFieldNote(
            "浙北桐乡", "桑园与蚕房", "清明前后渐暖，桑树抽嫩叶。谷雨前后的春雨也会打湿叶面。",
            "桑园采嫩叶，带回蚕房喂小蚕，再备蚕种。",
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
            "江苏苏州", "验丝窗台与织机", "秋分后白昼渐短，本坊趁亮验丝。",
            "看经线是否齐整、松紧是否均匀，再穿梭引纬。本关用素绸练基础，苏州宋锦还能用经纬显花。",
            "上机后经线有的松、有的紧，应先做哪一步？", "校准织机，理齐经线", "多穿几趟梭子盖住松线", 0,
            "先把经线排齐并调匀松紧，梭子穿过时才更容易织出齐整布面。",
            "中国非遗网《宋锦织造技艺》", "https://www.ihchina.cn/art/detail/id/19995.html"),
        new WorkshopSeasonFieldNote(
            "江南工坊", "染坊与裁衣间", "温栀子在十一月有第二批采果，小雪冷空气活动增多。本坊从熟果试色，赶做夹衣。",
            "选成熟栀子染材，验干样再染整绸。裁衣前先看清样板的位置。",
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
