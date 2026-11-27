using System;
using System.Collections.Generic;

internal static class WorkshopQuests
{
    internal sealed class Entry
    {
        internal readonly string Id;
        internal readonly string Title;
        internal readonly string LocationHint;
        internal readonly string Instructions;
        private readonly Func<bool> completed;
        private readonly Func<bool> prerequisite;

        internal Entry(string id, string title, string locationHint, string instructions, Func<bool> completed,
            Func<bool> prerequisite = null)
        {
            Id = id;
            Title = title;
            LocationHint = locationHint;
            Instructions = instructions;
            this.completed = completed;
            this.prerequisite = prerequisite;
        }

        internal bool IsComplete => completed();
        internal bool IsReady => prerequisite == null || prerequisite();
    }

    private static readonly Entry[][] Chapters =
    {
        new[] {
            new Entry("SpringBriefing", "领采桑任务", "蚕房师傅", "与师傅交谈，了解清明采桑、谷雨浴蚕与喂蚕。", () => SpringSummerNpcDialogue.HasMulberryBriefing),
            new Entry("Basket", "拾采桑篮", "发光的采桑篮", "收下篮子，再进桑园。", () => SpringSummerNpcDialogue.HasHarvestBasket, () => SpringSummerNpcDialogue.HasMulberryBriefing),
            Item(WorkshopItem.MulberryLeaf, "清明采桑", "春日桑园", "左右移动接住桑叶。通关仅获得桑叶。", () => SpringSummerNpcDialogue.HasHarvestBasket),
            new Entry("EggPaper", "拾蚕种纸", "浴蚕平台旁", "采桑后收下蚕种纸。", HasEggPaper, () => SpringSummerNpcDialogue.CanWash),
            Item(WorkshopItem.SilkwormEgg, "谷雨浴蚕", "浴蚕平台", "拿起擦拭布蘸水，逐处擦净中央的蚕种，收好干净蚕种。", HasEggPaper),
            new Entry("Feed", "谷雨喂蚕", "养蚕区蚕匾架", "备好桑叶与干净蚕种，将三份叶子拖入蚕匾，获得蚕宝宝。", () => WorkshopWordRules.HasDelivery(WorkshopSeason.Spring), () => WorkshopWordRules.CanFeed),
            Handoff(WorkshopSeason.Spring, "交付春季成果", "提交蚕种、桑叶、蚕宝宝，获得蚕茧，开启夏章。")
        },
        new[] {
            Item(WorkshopItem.Salt, "取盐", "盐窖", "获取并收下盐。"),
            Item(WorkshopItem.Suoxu, "立夏下簇", "择茧草堂", "完成原有蚕茧合成消除小游戏，领取索绪。"),
            new Entry("WhiteTiles", "立夏择茧", "蚕茧簇架", "取得索绪后，回到簇架，从最下行开始点选深色茧块，避开白块。完成择茧领取立夏卡。", () => Step(WorkshopWordRules.Sorted), () => Has(WorkshopItem.Suoxu)),
            new Entry("Water", "取水", "水源", "为煮茧备水。", HasWater),
            new Entry("Firewood", "拾木材", "发光木柴", "收下木材，带到练丝坊。", HasFirewood),
            Item(WorkshopItem.Spool, "小满寻找线轴", "练丝坊煮茧锅", "备水拾柴后走通原有迷宫，找到线轴。", () => HasWater() && HasFirewood()),
            new Entry("Reeling", "小满缫丝", "找到线轴后再点煮茧锅", "左右拖动导丝环，把丝均匀绕满线轴；绕厚的地方换到薄处。", () => Step(WorkshopWordRules.Reeled), () => Has(WorkshopItem.Spool)),
            new Entry("MangzhongOrder", "芒种夏至绕丝上轴", "练丝坊右侧缫丝车", "找到线轴后，走近缫丝车点「绕丝上轴」。在丝路接接乐中转动线段，把左侧丝头接到右侧丝轴。", () => WorkshopObservationView.HasRecord(WorkshopObservation.MangzhongCocoon), () => Has(WorkshopItem.Spool)),
            Handoff(WorkshopSeason.Summer, "交付夏季成果", "提交盐、线轴、索绪，获得丝束，开启秋章。")
        },
        new[] {
            new Entry("Hoe", "立秋取锄头", "染材园门口", "向师傅了解天然染材，拾起锄头。", HasHoe),
            new Entry("DyeGather", "立秋采集染材", "染材园", "挥锄采集，选择收下或丢弃正常、损坏染材。", HasDyeMaterials, HasHoe),
            new Entry("Hammer", "白露拾木槌", "织机左侧地面", "点击发光的修机木槌拾取。", HasHammer),
            new Entry("LoomRepair", "白露修复织机", "织造坊", "在黄色区域内完成三次击打。", LoomRepaired, HasHammer),
            Item(WorkshopItem.Shuttle, "取得梭子", "织机附近工具箱", "修机后打开工具箱，收下梭子。", LoomRepaired),
            Item(WorkshopItem.RawSilkCloth, "白露经纬织造", "修好的织机", "用梭子织入、拔子取消，满足每行每列的目标数字，获得生坯绸。", () => LoomRepaired() && Has(WorkshopItem.Shuttle)),
            new Entry("GrindingRod", "寻找研磨棒与研钵", "青染轩", "拾研磨棒，点击研钵收下，准备研磨。", () => HasGrindingRod() && Step("autumn.mortar")),
            new Entry("Grind", "秋分研磨染材", "青染轩研钵", "把天然染材研磨成染料。", HasGroundDye, () => HasDyeMaterials() && HasGrindingRod() && Step("autumn.mortar")),
            Item(WorkshopItem.DyedCloth, "秋分染色", "青染轩染缸", "选择已研磨的染料，把一块素绸拖入染缸，染好后收下。道具不消耗。", () => HasGroundDye() && Has(WorkshopItem.RawSilkCloth)),
            new Entry("Offerings", "备齐秋社贡品", "灵桑阙与师傅处", "收集香蕉、蚕花酒；第一次与师傅交谈即获得糕点。", OfferingsReady),
            Item(WorkshopItem.Statue, "白露秋分秋社", "灵桑阙", "登上灵桑阙提交贡品，领取土地公雕像。", OfferingsReady),
            new Entry("Panhua", "寒露攀花", "染好布后回到织机", "用梭子和拔子，按行列数字推演纹样。染布保留。", () => Step(WorkshopWordRules.Patterned), () => WorkshopWordRules.CanPattern),
            Handoff(WorkshopSeason.Autumn, "交付秋季成果", "提交土地公雕像、梭子、生坯绸，获得素绸。染布带入冬章。")
        },
        new[] {
            new Entry("WinterPreparation", "小雪晾布排料", "缝素堂裁衣台", "晾干染布，摆齐前片、后片和袖片，留一块完整的余布。",
                () => WorkshopWordRules.HasWinterPreparation, () => Has(WorkshopItem.DyedCloth)),
            Item(WorkshopItem.CutPieces, "立冬小雪剪帛", "缝素堂裁衣台", "排料完成后，沿虚线剪出前衣、后衣和衣袖。",
                () => Has(WorkshopItem.DyedCloth) && WorkshopWordRules.HasWinterPreparation),
            new Entry("Fitting", "小雪量体裁衣", "剪帛后再点裁衣台", "把三张衣样纸拖到右侧布面的同名轮廓。", () => Step(WorkshopWordRules.Fitted), () => Has(WorkshopItem.CutPieces)),
            Item(WorkshopItem.Needle, "取得缝衣针", "缝素堂", "点击并收下缝衣针。"),
            new Entry("WinterHandoff", "小雪选款缝衣", "缝素堂裁衣台", "版型确认、缝衣针备好后，在裁衣台选款，拼合三块衣片并缝制。道具保留，也可回工坊交件。", () => WorkshopProgress.IsComplete(WorkshopSeason.Winter), () => WorkshopProgress.CanSubmit(WorkshopSeason.Winter)),
            new Entry("TryOn", "锦衣堂试衣", "成衣完成后直接进入", "穿上刚缝好的衣服，试衣留念。", () => Step("winter.tryOn"), () => WorkshopProgress.IsComplete(WorkshopSeason.Winter))
        }
    };

    internal static int SeasonCount => Chapters.Length;
    internal static IReadOnlyList<Entry> ForSeason(WorkshopSeason season) => Chapters[(int)season];

    internal static Entry NextReady(WorkshopSeason season)
    {
        if (!WorkshopProgress.IsUnlocked(season) || WorkshopProgress.IsComplete(season)) return null;
        foreach (var quest in ForSeason(season))
            if (quest.IsReady && !quest.IsComplete) return quest;
        return null;
    }

    private static Entry Item(WorkshopItem item, string title, string locationHint, string instructions,
        Func<bool> prerequisite = null) =>
        new Entry(item.ToString(), title, locationHint, instructions, () => Has(item), prerequisite);

    private static Entry Handoff(WorkshopSeason season, string title, string instructions) =>
        new Entry(season + "Handoff", title, "返回工坊，前往储物屋", instructions,
            () => WorkshopProgress.IsComplete(season),
            () => WorkshopProgress.CanSubmit(season));

    private static bool Has(WorkshopItem item) => WorkshopProgress.Has(item);
    private static bool Step(string id) => WorkshopProgress.HasStep(id);
    private static bool OrderReady(WorkshopObservation id) =>
        !WorkshopProgress.SeasonalOrderEnabled || WorkshopObservationView.HasOrderWork(id);

    // A reward can imply its prerequisites in saves made before those steps were recorded.
    private static bool HasEggPaper() => Step("spring.eggPaper") || Has(WorkshopItem.SilkwormEgg);
    private static bool HasWater() => Step("summer.water") || Has(WorkshopItem.Spool);
    private static bool HasFirewood() => Step("summer.wood") || Has(WorkshopItem.Spool);
    private static bool HasHammer() => Step("autumn.hammer") || LoomRepaired();
    private static bool LoomRepaired() => Step("autumn.loom") || Has(WorkshopItem.RawSilkCloth);
    private static bool OfferingsReady() => Has(WorkshopItem.Statue) ||
        (Step("autumn.wine") && Step("autumn.banana") && Step("autumn.cake"));
    private static bool HasHoe() => Step("autumn.hoe") || Step("winter.hoe") || HasDyeMaterials();
    private static bool HasDyeMaterials() => Step("winter.dyes") || HasGroundDye();
    private static bool HasGrindingRod() => Step("winter.rod") || HasGroundDye();
    private static bool HasGroundDye() => Step("winter.ground") || Has(WorkshopItem.DyedCloth);
}
