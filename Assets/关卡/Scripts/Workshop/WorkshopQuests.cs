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
                "先去找春章的蚕房师傅，听他交代这一批小蚕需要哪些桑叶。",
                () => SpringSummerNpcDialogue.HasMulberryBriefing),
            new Entry("Basket", "拾采桑篮", "找发光的采桑篮", "点击篮子并收下，带到桑树旁的谷雨蚕架。",
                HasBasket, () => SpringSummerNpcDialogue.HasMulberryBriefing),
            new Entry(nameof(WorkshopItem.MulberryLeaf), "理桑叶", "带篮子到谷雨蚕架",
                "按两只蚕匾的需求选叶。干叶直接拖入篮，湿叶用鼠标擦去露水后再入篮。",
                () => SpringSummerNpcDialogue.HasPreparedLeaves,
                () => SpringSummerNpcDialogue.HasMulberryBriefing && HasBasket()),
            Item(WorkshopItem.Silkworm, "喂蚕", "在谷雨蚕架喂桑叶",
                "同一篮桑叶配齐后送到蚕匾。嫩叶给左边幼蚕，适熟叶给右边饲育匾。",
                () => SpringSummerNpcDialogue.HasMulberryBriefing && HasBasket()),
            new Entry("EggPaper", "拾蚕种纸", "找发光的蚕种纸",
                "喂完眼前这批小蚕，点击并收下蚕种纸，为下一批小蚕做准备。",
                HasEggPaper, () => SpringSummerNpcDialogue.CanWash),
            Item(WorkshopItem.SilkwormEgg, "浴蚕", "带蚕种纸去浴蚕桶",
                "把蚕种筛放入水面，沿桶口轻晃漂洗，捞起杂质。",
                () => SpringSummerNpcDialogue.CanWash && HasEggPaper()),
            Handoff(WorkshopSeason.Spring, "交付春季成果", "带桑叶、蚕种和蚕宝宝返回工坊，合成蚕茧。")
        },
        new[]
        {
            Item(WorkshopItem.Salt, "取盐", "点击盐窖", "点击盐窖，选择「获取盐」，再点击「收下」。"),
            Item(WorkshopItem.Suoxu, "择茧", "到蚕茧簇架", "到蚕茧簇架开始择茧，凑齐三张相同图案，领取索绪。"),
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
            Item(WorkshopItem.Shuttle, "取梭", "打开织机旁的工具箱", "点击织机附近的工具箱，打开后收下梭子。"),
            new Entry("Hammer", "向师傅借锤", "与秋季师傅对话", "与秋季师傅对话，点「收下锤子」领取修理工具。",
                HasHammer),
            new Entry("LoomRepair", "修复织机", "带锤子去织机旁", "带上锤子到织机旁，点击「修复织机」并完成修理。",
                LoomRepaired, HasHammer),
            Item(WorkshopItem.RawSilkCloth, "织造", "到织机旁开始织造", "修好织机并拿到梭子后，点击「开始织造」，织出生坯绸。",
                () => LoomRepaired() && Has(WorkshopItem.Shuttle)),
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
            Item(WorkshopItem.DyedCloth, "染布", "到染缸给素绸上色", "在染缸选择已研磨的染料，把素绸拖入缸中染色。",
                HasGroundDye),
            Item(WorkshopItem.CutPieces, "裁衣", "带染布去裁衣台", "拿染好的布到裁衣台，用剪刀沿线剪出三片衣料。",
                () => Has(WorkshopItem.DyedCloth)),
            Item(WorkshopItem.Needle, "拾缝衣针", "找发光的缝衣针", "在冬季场景找到缝衣针，点击并收下。"),
            Handoff(WorkshopSeason.Winter, "缝制成衣", "带染布、衣片和缝衣针返回工坊，选款并缝制成衣。")
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

    // A reward can imply its prerequisites in saves made before those steps were recorded.
    private static bool HasBasket() => Step("spring.basket") || Has(WorkshopItem.MulberryLeaf);
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
