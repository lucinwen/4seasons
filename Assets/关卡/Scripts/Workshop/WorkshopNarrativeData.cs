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
            "清明已至 · 护种备育",
            "清明前后回暖，桑芽舒展，小蚕有了合适的嫩叶。谷雨前后若遇春雨，等叶面干了再采，备好蚕具再喂蚕。",
            "采桑、浴蚕、喂蚕",
            "春 · 桑园与蚕房",
            "谷雨将尽 · 新茧入夏",
            "桑叶与净种都已归档，春蚕在匾中安稳生长。日影渐长，蚕房把这一季的记录交到下一位师傅手中，暑夏的忙碌正要开始。",
            "清明 · 谷雨  →  小暑 · 大暑"),
        new WorkshopSeasonNarrative(
            "夏字第一号",
            "小暑 · 大暑",
            "暑热防潮 · 蔟上收茧",
            "暑热中要查看蔟房是否闷湿。等蚕茧成熟后再收茧，按品相分拣，准备煮茧与缫丝。",
            "择茧、煮茧、寻轴",
            "夏 · 晾素轩与缫丝处",
            "大暑转秋 · 生丝入库",
            "择茧与寻轴告一段落，散乱的丝绪重新归拢成束。暑热渐退，织造工坊的灯已经亮起，下一段经纬正在等候。",
            "小暑 · 大暑  →  秋分 · 寒露"),
        new WorkshopSeasonNarrative(
            "秋字第一号",
            "秋分 · 寒露",
            "秋分将至 · 酬蚕议事",
            "秋分昼夜近等，寒露早晚渐凉。趁白天验丝、校机，保持丝料干爽，再穿梭织出素绸。酬蚕礼收进本章，记下蚕乡的谢意。",
            "修机、织造、酬蚕",
            "秋 · 灵桑阙与织造工坊",
            "寒露收梭 · 素绸入冬",
            "酬谢过一季蚕事，织机重新发出均匀的声响。素绸叠放入箧，风里已带上霜意，染坊的清水和染缸正等待新布。",
            "秋分 · 寒露  →  立冬 · 小雪"),
        new WorkshopSeasonNarrative(
            "冬字第一号",
            "立冬 · 小雪",
            "立冬霜寒 · 染坊启门",
            "天气冷下来，桑园逐渐转入冬管。工坊利用前几章备好的素绸，检查染材、研磨染布，晾干后用样板裁衣。",
            "染布、裁衣、缝制",
            "冬 · 染料园与青染轩",
            "小雪将近 · 衣片成行",
            "染料在钵中渐渐化开，素绸也有了新的颜色。裁衣台已经铺平样板，只待三片布合成一件可以穿在身上的成衣。",
            "立冬 · 小雪  →  一件桑蚕成衣")
    };

    private static readonly WorkshopSeasonFieldNote[] FieldNotes =
    {
        new WorkshopSeasonFieldNote(
            "浙北桐乡", "桑园与蚕房", "清明前后渐暖，桑树抽嫩叶。谷雨前后的春雨也会打湿叶面。",
            "桑园看叶芽，蚕房看小蚕。等叶面干再采鲜嫩桑叶，按蚕龄给叶。",
            "刚下过一场春雨，要给小蚕喂桑，先做什么？", "等叶面干，再采嫩叶", "趁叶上有雨水，马上采喂", 0,
            "嫩叶适合小蚕。先等叶面干，再把新鲜桑叶带进蚕房。",
            "中国丝绸博物馆《清明至 蚕事启》", "https://www.chinasilkmuseum.com/cs/info_164.aspx?itemid=31631"),
        new WorkshopSeasonFieldNote(
            "浙江湖州南浔", "蚕蔟架与缫丝坊", "小暑、大暑天气炎热，遇上高湿，熟蚕所在的蔟房容易闷热。",
            "先通风排湿，确认茧熟再收。南浔辑里湖丝的手工工序包括煮茧、索绪和绕丝轴。",
            "大暑午后，蔟房闷热又潮，怎样照料更合适？", "关紧门窗，立刻收全部茧", "先通风排湿，再检查茧况", 1,
            "蔟房要通风，避免强风直吹。收茧要看茧的成熟情况。",
            "中国非遗网《辑里湖丝手工制作技艺》", "https://www.ihchina.cn/art/detail/id/14472.html"),
        new WorkshopSeasonFieldNote(
            "江苏苏州", "织造工坊与酬蚕台", "本章排在秋分、寒露。寒露前后早晚转凉，可在白天检查丝料和织机。",
            "看经线是否齐整、松紧是否均匀，再穿梭引纬。本关用素绸练基础，苏州宋锦还能用经纬显花。",
            "上机后经线有的松、有的紧，应先做哪一步？", "校准织机，理齐经线", "多穿几趟梭子盖住松线", 0,
            "先把经线排齐并调匀松紧，梭子穿过时才更容易织出齐整布面。",
            "中国非遗网《宋锦织造技艺》", "https://www.ihchina.cn/art/detail/id/19995.html"),
        new WorkshopSeasonFieldNote(
            "江南工坊", "染料园、染坊与裁衣间", "立冬、小雪前后渐冷。桑园逐渐转入冬管，清园修枝是为来年桑叶做准备。",
            "本章把已有的素绸送进染坊。先检查备下的染材，试色、染布，晾平后再依样裁衣。",
            "一份储存的染材受潮变质，如何处理？", "和好染材混在一起磨", "挑出变质染材，用完好材料试色", 1,
            "先剔除变质材料，再用完好的染材试出需要的色样。",
            "安康市农业农村局《冬季桑园管理技术要点》", "https://nyj.ankang.gov.cn/Content-2868508.html")
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
