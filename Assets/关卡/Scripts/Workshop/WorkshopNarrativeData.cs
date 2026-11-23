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

internal static class WorkshopNarrativeData
{
    private static readonly WorkshopSeasonNarrative[] Seasons =
    {
        new WorkshopSeasonNarrative(
            "春字第一号",
            "清明 · 谷雨",
            "清明已至 · 护种备育",
            "桑芽初展，本社春蚕生产进入护种备育阶段。请各位小学徒先备采桑篮，沿桑园采收嫩叶；再取得蚕种纸，到蚕房完成浴蚕与喂养。春事有序，先从细处着手。",
            "采桑、浴蚕、喂蚕",
            "春 · 桑园与蚕房",
            "谷雨将尽 · 新茧入夏",
            "桑叶与净种都已归档，春蚕在匾中安稳生长。日影渐长，蚕房把这一季的记录交到下一位师傅手中，暑夏的忙碌正要开始。",
            "清明 · 谷雨  →  立夏 · 小暑"),
        new WorkshopSeasonNarrative(
            "夏字第一号",
            "芒种 · 大暑",
            "立夏巡蚕 · 蔟上收茧",
            "立夏前后，日暖渐长，蚕房照料进入要紧时段。请分批巡视，辨明食桑、眠起与熟蚕状态，及时给叶、扩座，并为熟蚕备好蚕蔟。",
            "择茧、煮茧、寻轴",
            "夏 · 晾素轩与缫丝处",
            "大暑转秋 · 生丝入库",
            "择茧与寻轴告一段落，散乱的丝绪重新归拢成束。暑热渐退，织造工坊的灯已经亮起，下一段经纬正在等候。",
            "芒种 · 大暑  →  秋分 · 寒露"),
        new WorkshopSeasonNarrative(
            "秋字第一号",
            "秋分 · 寒露",
            "秋分将至 · 酬蚕议事",
            "秋茧收成，先参与酬蚕礼，再细心理顺丝线。寒露转凉后，排布经线、校准织机，为素绸入库做好准备。",
            "酬蚕、修机、织造",
            "秋 · 灵桑阙与织造工坊",
            "寒露收梭 · 素绸入冬",
            "酬谢过一季蚕事，织机重新发出均匀的声响。素绸叠放入箧，风里已带上霜意，染坊的清水和染缸正等待新布。",
            "秋分 · 寒露  →  立冬 · 小雪"),
        new WorkshopSeasonNarrative(
            "冬字第一号",
            "立冬 · 小雪",
            "立冬霜寒 · 染坊启门",
            "立冬之后，素绸送入染坊。架上各色染材与色样齐备，请辨认可用原料，研磨染材并完成染色。待小雪，再以样板裁制衣片。",
            "取染、研磨、染衣",
            "冬 · 染料园与青染轩",
            "小雪将近 · 衣片成行",
            "染料在钵中渐渐化开，素绸也有了新的颜色。裁衣台已经铺平样板，只待三片布合成一件可以穿在身上的成衣。",
            "立冬 · 小雪  →  一件桑蚕成衣")
    };

    public static WorkshopSeasonNarrative For(WorkshopSeason season)
    {
        return Seasons[Mathf.Clamp((int)season, 0, Seasons.Length - 1)];
    }

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

    public static string SeasonTime(WorkshopSeason season)
    {
        switch (season)
        {
            case WorkshopSeason.Spring: return "清明\n谷雨";
            case WorkshopSeason.Summer: return "芒种\n大暑";
            case WorkshopSeason.Autumn: return "秋分\n寒露";
            default: return "立冬\n小雪";
        }
    }
}
