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
        new[]
        {
            new Entry("SpringBriefing", "领采桑任务", "与蚕房师傅交谈",
                "到蚕房找师傅，领清明的采桑任务，听他讲幼蚕需要什么叶。",
                () => SpringSummerNpcDialogue.HasMulberryBriefing),
            new Entry("Basket", "拾采桑篮", "找发光的采桑篮", "收下篮子，带到桑树前采桑。",
                () => SpringSummerNpcDialogue.HasHarvestBasket, () => SpringSummerNpcDialogue.HasMulberryBriefing),
            new Entry(nameof(WorkshopItem.MulberryLeaf), "清明采桑", "带篮子到桑树前",
                "先选叶展、露退的桑垄，再左右移动篮子接鲜叶。把桑垄记下来，谷雨当天另采鲜叶。",
                () => SpringSummerNpcDialogue.HasHarvestedLeaves,
                () => SpringSummerNpcDialogue.HasMulberryBriefing && SpringSummerNpcDialogue.HasHarvestBasket),
            Item(WorkshopItem.Silkworm, "谷雨给桑", "到谷雨蚕架取当天新采的桑叶",
                "春雨后先擦干湿叶，再按两只蚕匾的需要配一篮。嫩叶喂幼蚕，适熟叶喂饲育匾。",
                () => SpringSummerNpcDialogue.HasHarvestedLeaves),
            new Entry("EggPaper", "拾蚕种纸", "找发光的蚕种纸",
                "喂完眼前这批小蚕，点击并收下蚕种纸，为下一批小蚕做准备。",
                HasEggPaper, () => SpringSummerNpcDialogue.CanWash),
            Item(WorkshopItem.SilkwormEgg, "浴蚕", "带蚕种纸去浴蚕桶",
                "把蚕种筛放入水面，沿桶口轻晃漂洗，捞起杂质。",
                () => SpringSummerNpcDialogue.CanWash && HasEggPaper()),
            Handoff(WorkshopSeason.Spring, "登记春蚕批次", "带春章记录回工坊，登记这批蚕后续长大、上蔟和结茧的过程。")
        },
        new[]
        {
            Item(WorkshopItem.Salt, "取盐", "点击盐窖", "点击盐窖，选择「获取盐」，再点击「收下」。"),
            Item(WorkshopItem.Suoxu, "择茧", "到蚕茧簇架", "到蚕茧簇架开始择茧，凑齐三张相同图案，领取索绪。"),
            new Entry("MangzhongOrder", "芒种验茧", "到蔟架按 O 打开翻茧配批",
                "查上蔟记录并翻验，成熟完好的交给夏至，未熟的留蔟，过潮或破损的另放。",
                () => OrderReady(WorkshopObservation.MangzhongCocoon)),
            new Entry("Water", "到水源取水", "点击发光的水源", "点击水源，取一份水备着煮茧。",
                HasWater),
            new Entry("Firewood", "拾木柴", "找发光的木柴", "找到场景中的木柴，点击并收下，备着煮茧。",
                HasFirewood),
            new Entry("XiazhiOrder", "夏至缫丝", "带水和木柴到煮茧锅",
                "煮合格茧，找丝头、绕轴、续丝。梅雨天把丝放到合适的干丝位，干妥再入库。",
                () => OrderReady(WorkshopObservation.DashuReeling),
                () => HasWater() && HasFirewood() && OrderReady(WorkshopObservation.MangzhongCocoon)),
            Item(WorkshopItem.Spool, "寻找线轴", "从煮茧锅进入迷宫", "备好水和木柴，在煮茧锅旁进入迷宫，找到线轴。",
                () => HasWater() && HasFirewood() && OrderReady(WorkshopObservation.DashuReeling)),
            Handoff(WorkshopSeason.Summer, "交付夏季成果", "带盐、索绪和线轴返回工坊，合成生丝。")
        },
        new[]
        {
            Item(WorkshopItem.Shuttle, "取梭", "打开织机旁的工具箱", "点击织机附近的工具箱，打开后收下梭子。"),
            new Entry("Hammer", "向师傅借锤", "与秋季师傅对话", "与秋季师傅对话，点「收下锤子」领取修理工具。",
                HasHammer),
            new Entry("LoomRepair", "修复织机", "带锤子去织机旁", "带上锤子到织机旁，点击「修复织机」并完成修理。",
                LoomRepaired, HasHammer),
            new Entry("QiufenOrder", "秋分验丝", "到秋分窗台按 O",
                "把夏至的丝轴摆进窗光，找出接头。天暗时点灯，验过的丝留给寒露。",
                () => OrderReady(WorkshopObservation.QiufenOffering)),
            Item(WorkshopItem.RawSilkCloth, "寒露织绸", "到修好的织机旁开始织造", "先看丝轴有没有沾露，再照着样带理经、走梭，织出素绸。",
                () => LoomRepaired() && Has(WorkshopItem.Shuttle) && OrderReady(WorkshopObservation.QiufenOffering)),
            new Entry("Offerings", "备齐贡品", "找酒和果篮，再找师傅", "找到蚕花酒和香蕉果篮，再与师傅对话领糕点。",
                OfferingsReady),
            Item(WorkshopItem.Statue, "酬蚕", "带贡品到供桌", "带三样贡品到供桌，开启酬蚕仪式，收下嫘祖雕像。",
                OfferingsReady),
            Handoff(WorkshopSeason.Autumn, "交付秋季成果", "带梭子、生坯绸和嫘祖雕像返回工坊，合成素绸。")
        },
        new[]
        {
            new Entry("Hoe", "拾挖染料锄头", "找发光的锄头", "找到场景里的锄头，点击并收下。",
                HasHoe),
            new Entry("DyeGather", "采集染材", "带锄头去染料园", "带锄头进入染料园，采到染材后带回冬季场景。",
                HasDyeMaterials, HasHoe),
            new Entry("GrindingRod", "拾研磨棒", "找发光的研磨棒", "找到场景里的研磨棒，点击并收下。",
                HasGrindingRod),
            new Entry("Grind", "研磨染材", "带染材去研磨钵", "带染材和研磨棒到钵体，至少研磨出一种染料。",
                HasGroundDye, () => HasDyeMaterials() && HasGrindingRod()),
            new Entry("LidongOrder", "立冬试栀黄", "到染缸或在研磨处按 O",
                "挑这一班的成熟栀子，用寒露织绸的样头试出深浅栀黄。",
                () => OrderReady(WorkshopObservation.LidongDye), HasGroundDye),
            Item(WorkshopItem.DyedCloth, "染布", "到染缸给整幅素绸上色", "用验过的色样染整幅素绸。",
                () => HasGroundDye() && OrderReady(WorkshopObservation.LidongDye)),
            Item(WorkshopItem.CutPieces, "小雪量体裁衣", "带染布去裁衣台", "晾干后量体选纸样，顺纹排面片，为里襟留一块整布。",
                () => Has(WorkshopItem.DyedCloth)),
            Item(WorkshopItem.Needle, "拾缝衣针", "找发光的缝衣针", "在冬季场景找到缝衣针，点击并收下。"),
            Handoff(WorkshopSeason.Winter, "缝制成衣", "带染布、衣片和缝衣针返回主厅。大雪合缝翻衣，冬至试身交衣。")
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
    private static bool HasHoe() => Step("winter.hoe") || HasDyeMaterials();
    private static bool HasDyeMaterials() => Step("winter.dyes") || HasGroundDye();
    private static bool HasGrindingRod() => Step("winter.rod") || HasGroundDye();
    private static bool HasGroundDye() => Step("winter.ground") || Has(WorkshopItem.DyedCloth);
}
