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
            new Entry(nameof(WorkshopItem.MulberryLeaf), "采摘桑叶", "带篮子到桑树前",
                "到桑树前点击「采摘桑叶」或按 O，收下桑叶。",
                () => SpringSummerNpcDialogue.HasHarvestedLeaves,
                () => SpringSummerNpcDialogue.HasMulberryBriefing && SpringSummerNpcDialogue.HasHarvestBasket),
            Item(WorkshopItem.Silkworm, "喂蚕宝宝", "带桑叶到蚕架前",
                "点击「喂蚕宝宝」或按 O，逐片把四片桑叶拖进蚕匾。",
                () => SpringSummerNpcDialogue.HasHarvestedLeaves),
            new Entry("EggPaper", "拾蚕种纸", "找发光的蚕种纸",
                "喂饱蚕宝宝后，点击并收下蚕种纸。",
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
            new Entry("MangzhongOrder", "芒种理线", "到蚕茧簇架按 O 打开丝路接接乐",
                "转动线段，把左侧丝头接到右侧丝轴。接通一张即可完成。",
                () => OrderReady(WorkshopObservation.MangzhongCocoon)),
            new Entry("Water", "到水源取水", "点击发光的水源", "点击水源，取一份水备着煮茧。",
                HasWater),
            new Entry("Firewood", "拾木柴", "找发光的木柴", "找到场景中的木柴，点击并收下，备着煮茧。",
                HasFirewood),
            Item(WorkshopItem.Spool, "寻找线轴", "从煮茧锅进入迷宫", "备好水和木柴，在煮茧锅旁进入迷宫，找到线轴。",
                () => HasWater() && HasFirewood()),
            Handoff(WorkshopSeason.Summer, "交付夏季成果", "带盐、索绪和线轴返回工坊，合成生丝。")
        },
        new[]
        {
            new Entry("Hammer", "向师傅借锤", "与秋季师傅对话", "与秋季师傅对话，点「收下锤子」领取修理工具。",
                HasHammer),
            new Entry("LoomRepair", "修复织机", "带锤子去织机旁", "带上锤子到织机旁，点击「修复织机」并完成修理。",
                LoomRepaired, HasHammer),
            Item(WorkshopItem.Shuttle, "取梭", "打开织机旁的工具箱", "织机修复后，点击工具箱收下梭子，再回织机推梭。",
                () => HasHammer() && LoomRepaired()),
            Item(WorkshopItem.RawSilkCloth, "寒露纺织", "到修好的织机旁开始纺织", "参照每行和每列的目标数字，用梭子织出图案，收下生坯绸，供后续染色和寒露攀花使用。",
                () => LoomRepaired() && Has(WorkshopItem.Shuttle)),
            new Entry("Offerings", "备齐贡品", "找酒和果篮，再找师傅", "找到蚕花酒和香蕉果篮，再与师傅对话领糕点。",
                OfferingsReady),
            Item(WorkshopItem.Statue, "酬蚕", "带贡品到供桌", "带三样贡品到供桌，开启酬蚕仪式，收下土地公。",
                OfferingsReady),
            new Entry("Hoe", "拾挖染料锄头", "找发光的锄头", "找到场景里的锄头，点击并收下。",
                HasHoe),
            new Entry("DyeGather", "采集染材", "带锄头去染料园", "带锄头进入染料园，采到染材后带回秋季场景。",
                HasDyeMaterials, HasHoe),
            new Entry("GrindingRod", "拾研磨棒", "找发光的研磨棒", "找到场景里的研磨棒，点击并收下。",
                HasGrindingRod),
            new Entry("Grind", "研磨染料", "带染材去研磨钵", "带染材和研磨棒到研磨钵，选择染材并至少研磨出一种染料。",
                HasGroundDye, () => HasDyeMaterials() && HasGrindingRod()),
            new Entry("LidongOrder", "染色", "到染缸选择已研磨的颜色",
                "确认心仪颜色后，调整清水与染料原液的比例，完成深浅两阶染色。",
                () => OrderReady(WorkshopObservation.LidongDye),
                () => HasGroundDye() && Has(WorkshopItem.RawSilkCloth)),
            Item(WorkshopItem.DyedCloth, "秋色染布", "到染缸给整幅生坯绸上色", "选择已研磨的颜色，调整清水与原液比例并完成染色，收下秋色染布。",
                () => HasGroundDye() && OrderReady(WorkshopObservation.LidongDye) && Has(WorkshopItem.RawSilkCloth)),
            Handoff(WorkshopSeason.Autumn, "交付秋季成果", "完成纺织、酬蚕和秋色染布，带梭子、生坯绸、土地公与染布返回工坊，在储物屋交付秋季成果。")
        },
        new[]
        {
            Item(WorkshopItem.CutPieces, "小雪裁衣", "带染布去裁衣台", "点「开始裁衣」，选中剪刀，沿样板裁出前片、后片和袖片。",
                () => Has(WorkshopItem.DyedCloth)),
            Item(WorkshopItem.Needle, "拾缝衣针", "找发光的缝衣针", "在冬季场景找到缝衣针，点击并收下。"),
            Handoff(WorkshopSeason.Winter, "缝制成衣", "使用秋季染布裁好衣片，带衣片和缝衣针返回主厅。大雪合缝翻衣，冬至试身交衣。")
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
    private static bool HasHoe() => Step("autumn.hoe");
    private static bool HasDyeMaterials() => Step("winter.dyes") || HasGroundDye();
    private static bool HasGrindingRod() => Step("winter.rod") || HasGroundDye();
    private static bool HasGroundDye() => Step("winter.ground") || Has(WorkshopItem.DyedCloth);
}
