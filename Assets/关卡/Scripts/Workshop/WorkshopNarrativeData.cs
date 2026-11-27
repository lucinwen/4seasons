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
            "清明已至 谷雨将临",
            "春气渐暖，桑芽初展。向蚕房师傅领任务，拾篮采桑，查验蚕种并完成浴蚕，再把桑叶拖给小蚕。",
            "采桑、浴蚕、喂蚕",
            "春日桑园 · 浴蚕平台 · 养蚕区",
            "春季交付 新茧入夏",
            "蚕种、桑叶和蚕宝宝已登记，获得蚕茧。道具保留，下一章学习下簇和缫丝。",
            "清明谷雨 → 立夏小满"),
        new WorkshopSeasonNarrative(
            "夏字第一号",
            "立夏 · 小满 · 芒种 · 夏至",
            "下簇缫丝 络丝备料",
            "先在择茧草堂完成下簇。备好木材与水源，穿行迷宫寻找线轴，再移动导丝环均匀绕轴，接通丝路备料。",
            "下簇、寻轴、缫丝、备料",
            "择茧草堂 · 练丝坊",
            "夏季交付 丝束入库",
            "提交盐、线轴和索绪，获得丝束。秋季将用它学习织造、染色与攀花。",
            "立夏小满芒种夏至 → 立秋白露"),
        new WorkshopSeasonNarrative(
            "秋字第一号",
            "立秋 · 白露 · 秋分 · 寒露",
            "采材织造 秋染攀花",
            "立秋采收天然染材。白露拾槌修机、取梭织造，完成后收下缬；秋分研磨染色，参加秋社；寒露再在染布上攀花。所有道具一直保留。",
            "采材、修机织造、研磨染色、秋社、攀花",
            "染材园 · 织造坊 · 青染轩 · 灵桑阙",
            "秋季交付 染布入冬",
            $"提交土地公雕像、梭子和缬，获得{WorkshopProgress.ProductName(WorkshopSeason.Autumn)}。染布与其他道具仍在，带入冬章裁制成衣。",
            "立秋白露秋分寒露 → 立冬小雪"),
        new WorkshopSeasonNarrative(
            "冬字第一号",
            "立冬 · 小雪",
            "剪帛制衣 试衣留念",
            "带秋季染好的布到缝素堂，向师傅领取蚕连。晾干布后排好衣样，再沿虚线剪帛领衣片。备齐缝衣针、蚕连和衣片后选款缝衣。",
            "排布、剪帛、缝衣、试衣",
            "缝素堂 · 锦衣堂",
            "小雪成衣 晋升大织将",
            "缝衣针、蚕连和衣片已登记，成衣缝制完成。到锦衣堂试衣留念，回看从桑叶到成衣的四季旅程。",
            "小雪成衣 → 锦衣堂试衣"),
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
