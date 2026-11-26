using System;
using System.Collections.Generic;
using Qingming.LevelBackground;
using UnityEngine;

/// <summary>First successful completions enter the collection and wait for the worktable to close.</summary>
internal static class WorkshopRewardCards
{
    internal sealed class Card
    {
        internal readonly string Id, Title, Description;
        internal readonly WorkshopSeason Season;
        internal readonly WorkshopItem Item;
        internal readonly int[] Terms;
        internal readonly Func<bool> Completed;

        internal Card(string id, string title, string description, WorkshopSeason season,
            WorkshopItem item, Func<bool> completed, params int[] terms)
        {
            Id = id; Title = title; Description = description; Season = season;
            Item = item; Completed = completed; Terms = terms;
        }
    }

    internal static readonly Card[] Definitions =
    {
        new Card("harvest", "清明 · 采桑", "择取舒展、洁净的嫩桑叶，装入竹篮，为春蚕备下新鲜饲料。", WorkshopSeason.Spring,
            WorkshopItem.MulberryLeaf, () => WorkshopProgress.Has(WorkshopItem.MulberryLeaf), 4),
        new Card("wash", "谷雨 · 浴蚕", "先把布蘸湿，再逐处轻拭蚕种。清洁完成，才能把蚕种带去照料。", WorkshopSeason.Spring,
            WorkshopItem.SilkwormEgg, () => WorkshopProgress.Has(WorkshopItem.SilkwormEgg), 5),
        new Card("feed", "谷雨 · 喂蚕", "把竹匾里的桑叶分次喂给蚕宝宝。采桑、浴蚕与喂蚕衔接成春季蚕事。", WorkshopSeason.Spring,
            WorkshopItem.Silkworm, () => WorkshopProgress.HasStep(WorkshopWordRules.Fed) || WorkshopProgress.HasStep("spring.guyu.batchFed"), 5),
        new Card("cocoon", "立夏 · 择茧", "从蚕茧中辨认、配对并收取合适的茧，为后续索绪和缫丝准备材料。", WorkshopSeason.Summer,
            WorkshopItem.Suoxu, () => WorkshopProgress.Has(WorkshopItem.Suoxu), 0),
        new Card("maze", "小满 · 备料寻轴", "备水拾柴，找到线轴。煮茧、索绪和收丝各有工具，备齐后再进行治丝。", WorkshopSeason.Summer,
            WorkshopItem.Spool, () => WorkshopProgress.Has(WorkshopItem.Spool), 1),
        new Card("reeling", "小满 · 匀丝排线", "按顺序把丝束排入线轴槽位，让丝路整齐、收卷有序。", WorkshopSeason.Summer,
            WorkshopItem.Spool, () => WorkshopProgress.HasStep(WorkshopWordRules.Reeled), 1),
        new Card("thread", "芒种夏至 · 理经卷纬", "转动丝路线段，让丝头连续接到丝轴。接通三轮，完成下一道织造工序的备丝。", WorkshopSeason.Summer,
            WorkshopItem.Suoxu, () => WorkshopProgress.HasObservation("observation.summer.mangzhong.thread"), 2),
        new Card("dyeGather", "立秋 · 采染材", "辨认并采集天然染材。不同染材提供不同色泽，采集后还要研磨处理。", WorkshopSeason.Autumn,
            WorkshopItem.DyedCloth, () => WorkshopProgress.HasStep("winter.dyes"), 0),
        new Card("repair", "白露 · 修整织机", "用木槌修整织机，检查工位并备好梭子，再开始织造。", WorkshopSeason.Autumn,
            WorkshopItem.Shuttle, () => WorkshopProgress.HasStep("autumn.loom"), 2),
        new Card("weave", "白露 · 交替走梭", "交替抬起经线、来回投梭，让经纬相交，逐行织成生坯绸。", WorkshopSeason.Autumn,
            WorkshopItem.RawSilkCloth, () => WorkshopProgress.HasStep(WorkshopWordRules.Woven), 2),
        new Card("grind", "秋分 · 研磨染料", "将采来的染材研磨成可使用的染料，再把染色工位准备妥当。", WorkshopSeason.Autumn,
            WorkshopItem.DyedCloth, () => WorkshopProgress.HasStep("winter.ground"), 3),
        new Card("dye", "秋分 · 染绸", "选择研磨好的染料，把素绸送入染缸，让整幅布获得同一种颜色。", WorkshopSeason.Autumn,
            WorkshopItem.DyedCloth, () => WorkshopProgress.Has(WorkshopItem.DyedCloth), 3),
        new Card("ritual", "白露秋分 · 秋社", "备齐供品并完成秋社陈设，以土地公雕像记录这一季的劳作与祈愿。", WorkshopSeason.Autumn,
            WorkshopItem.Statue, () => WorkshopProgress.Has(WorkshopItem.Statue), 2, 3),
        new Card("pattern", "寒露 · 攀花", "在染好的绸面上完成纹样，让染整后的布进入装饰工序。", WorkshopSeason.Autumn,
            WorkshopItem.RawSilkCloth, () => WorkshopProgress.HasStep(WorkshopWordRules.Patterned), 4),
        new Card("cut", "立冬小雪 · 剪帛", "沿衣样裁出前衣、后衣与衣袖。先确定轮廓，再保留完整衣片供后续排样。", WorkshopSeason.Winter,
            WorkshopItem.CutPieces, () => WorkshopProgress.Has(WorkshopItem.CutPieces), 0, 1),
        new Card("fit", "小雪 · 排样", "把衣片摆到对应位置，辨清前后与袖片，完成缝衣前的排样。", WorkshopSeason.Winter,
            WorkshopItem.CutPieces, () => WorkshopProgress.HasStep(WorkshopWordRules.Fitted), 1),
        new Card("sew", "小雪 · 成衣", "备齐染布、衣片和缝衣针，选定衣形，把四季的蚕桑织染成果合成衣裳。", WorkshopSeason.Winter,
            WorkshopItem.Needle, () => WorkshopProgress.IsComplete(WorkshopSeason.Winter), 1),
        new Card("tryOn", "冬至 · 试衣", "试穿完成的衣裳，查看布色与衣形。四季工序在一件成衣上汇合。", WorkshopSeason.Winter,
            WorkshopItem.Needle, () => WorkshopProgress.HasStep("winter.tryOn"), 3)
    };

    private readonly struct Reward
    {
        internal readonly Card Craft;
        internal readonly WorkshopSeason Season;
        internal readonly int Term;
        internal Reward(Card craft) { Craft = craft; Season = craft.Season; Term = -1; }
        internal Reward(WorkshopSeason season, int term) { Craft = null; Season = season; Term = term; }
    }

    private static readonly Queue<Reward> pending = new Queue<Reward>();
    private static readonly HashSet<WorkshopItem> presentingItems = new HashSet<WorkshopItem>();
    private static readonly HashSet<string> presentingTerms = new HashSet<string>();
    private static bool initialized;
    private static Runner runner;
    private static bool termPresented;
    internal static bool IsOpen => WorkshopRewardCardView.IsOpen || termPresented;
    internal static bool IsBusy => IsOpen || pending.Count > 0;
    internal static int CardCount
    {
        get { int count = 0; foreach (var card in Definitions) if (Earned(card)) count++; return count; }
    }
    internal static bool Earned(Card card) => card != null && card.Completed();
    internal static bool HasTerm(WorkshopSeason season, int index) => index >= 0 && index < 6 &&
        (WorkshopProgress.HasSolarTerms(season) || WorkshopProgress.HasStep(TermId(season, index)));
    private static string Receipt(Card card) => "reward.craft." + card.Id;
    private static string TermId(WorkshopSeason season, int index) => "reward.term." + ((int)season * 6 + index);
    private static string ItemReceipt(WorkshopItem item) => "reward.item." + (int)item;
    private static string TermReceipt(WorkshopSeason season, int index) => "reward.term.seen." + ((int)season * 6 + index);

    internal static bool HasReceivedItemCard(WorkshopItem item) => WorkshopProgress.HasStep(ItemReceipt(item));

    // Automatic rewards and the older pickup popups share one receipt. Explicit
    // journal/collection reads deliberately bypass it.
    internal static bool TryBeginItemCard(WorkshopItem item)
    {
        if (HasReceivedItemCard(item) || !presentingItems.Add(item)) return false;
        RemovePendingItem(item);
        return true;
    }

    internal static void FinishItemCard(WorkshopItem item)
    {
        presentingItems.Remove(item);
        WorkshopProgress.RecordRewardReceipts(new[] { ItemReceipt(item) });
        RemovePendingItem(item);
    }

    internal static void ReleaseItemCard(WorkshopItem item) => presentingItems.Remove(item);

    private static void RemovePendingItem(WorkshopItem item)
    {
        for (int remaining = pending.Count; remaining > 0; remaining--)
        {
            var reward = pending.Dequeue();
            if (reward.Craft == null || reward.Craft.Item != item) pending.Enqueue(reward);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    internal static void Reset()
    {
        if (runner != null) UnityEngine.Object.Destroy(runner.gameObject);
        pending.Clear(); presentingItems.Clear(); presentingTerms.Clear();
        initialized = false; runner = null; termPresented = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // Read existing progress before play; older saves are collected without a popup backlog.
        Rebase();
        EnsureRunner();
    }

    internal static void Rebase()
    {
        pending.Clear(); initialized = true;
        var receipts = new List<string>();
        foreach (var card in Definitions)
            if (Earned(card))
            {
                AddReceipts(card, receipts);
                foreach (int term in EligibleTerms(card)) receipts.Add(TermReceipt(card.Season, term));
            }
        foreach (WorkshopItem item in Enum.GetValues(typeof(WorkshopItem)))
            if (WorkshopProgress.Has(item)) receipts.Add(ItemReceipt(item));
        WorkshopProgress.RecordRewardReceipts(receipts);
    }

    private static void AddReceipts(Card card, List<string> receipts)
    {
        receipts.Add(Receipt(card));
        foreach (int term in EligibleTerms(card)) receipts.Add(TermId(card.Season, term));
    }

    private static IEnumerable<int> EligibleTerms(Card card)
    {
        // Sorting the matched cocoons is a separate playable step. Lixia belongs
        // after that step, even though the earlier matching game already gave Suoxu.
        if (card.Id == "cocoon" && !WorkshopProgress.HasStep("summer.cocoon-sorted")) yield break;
        foreach (int term in card.Terms) yield return term;
    }

    private static bool AutomaticItemCard(Card card) => card.Id != "maze" && card.Id != "thread" &&
        card.Id != "pattern" && WorkshopProgress.Has(card.Item);

    private static bool ItemAlreadyQueued(WorkshopItem item)
    {
        foreach (var reward in pending) if (reward.Craft != null && reward.Craft.Item == item) return true;
        return false;
    }

    private static bool TermAlreadyQueued(WorkshopSeason season, int term)
    {
        foreach (var reward in pending)
            if (reward.Craft == null && reward.Season == season && reward.Term == term) return true;
        return false;
    }

    private static bool ShouldPresentTerm(WorkshopSeason season, int term) =>
        season == WorkshopSeason.Summer && term <= 2
            ? !WorkshopProgress.HasStep(TermReceipt(season, term))
            : !HasTerm(season, term);

    internal static void Synchronize()
    {
        if (!initialized) { Rebase(); return; }
        var receipts = new List<string>();
        foreach (var card in Definitions)
        {
            if (!Earned(card)) continue;
            if (!WorkshopProgress.HasStep(Receipt(card)) && AutomaticItemCard(card) &&
                !HasReceivedItemCard(card.Item) && !presentingItems.Contains(card.Item) && !ItemAlreadyQueued(card.Item))
                pending.Enqueue(new Reward(card));
            foreach (int term in EligibleTerms(card))
                if (ShouldPresentTerm(card.Season, term) && !TermAlreadyQueued(card.Season, term) &&
                    !presentingTerms.Contains(TermReceipt(card.Season, term)))
                    pending.Enqueue(new Reward(card.Season, term));
            AddReceipts(card, receipts);
        }
        WorkshopProgress.RecordRewardReceipts(receipts);
        if (pending.Count > 0) EnsureRunner();
    }

    private static void EnsureRunner()
    {
        if (!Application.isPlaying || runner != null) return;
        var host = new GameObject("Workshop Card Rewards");
        UnityEngine.Object.DontDestroyOnLoad(host);
        runner = host.AddComponent<Runner>();
    }

    internal static void ShowCollection(WorkshopSeason season, Action closed = null) =>
        WorkshopRewardCardView.ShowCollection(season, closed);

    internal static void ShowCompletionCard(string craftId, Action closed)
    {
        foreach (var card in Definitions)
        {
            if (card.Id != craftId || !Earned(card)) continue;
            if (!AutomaticItemCard(card)) { closed?.Invoke(); return; }
            // The game's completion card also presents its queued craft reward.
            // Keep newly earned solar cards in order for after returning to the scene.
            for (int remaining = pending.Count; remaining > 0; remaining--)
            {
                var reward = pending.Dequeue();
                if (reward.Craft != card) pending.Enqueue(reward);
            }
            WorkshopRewardCardView.ShowReward(card, closed);
            return;
        }
        closed?.Invoke();
    }

    internal static void ShowTermReward(WorkshopSeason season, int term, Action closed = null)
    {
        string id = TermReceipt(season, term);
        if (WorkshopProgress.HasStep(id) || !presentingTerms.Add(id)) { closed?.Invoke(); return; }
        for (int remaining = pending.Count; remaining > 0; remaining--)
        {
            var reward = pending.Dequeue();
            if (reward.Craft != null || reward.Season != season || reward.Term != term) pending.Enqueue(reward);
        }
        WorkshopProgress.RecordRewardReceipts(new[] { TermId(season, term) });
        termPresented = true;
        WorkshopKnowledgeView.ShowTerm(season, term, () =>
        {
            WorkshopProgress.RecordRewardReceipts(new[] { id });
            presentingTerms.Remove(id);
            termPresented = presentingTerms.Count > 0;
            closed?.Invoke();
        });
    }

    private static bool CanPresent => !IsOpen && !WorkshopQuestGuide.IsOpen &&
        !WorkshopQuestGuide.IsWorldOverlayActive && !WorkshopStorageView.IsOpen &&
        !WorkshopChapterGuide.IsOpen && !SimplePopup.IsOpen;

    private sealed class Runner : MonoBehaviour
    {
        private float quietTime;
        private void Update()
        {
            if (pending.Count == 0 || !CanPresent) { quietTime = 0f; return; }
            quietTime += Time.unscaledDeltaTime;
            if (quietTime < .35f) return;
            quietTime = 0f;
            var reward = pending.Dequeue();
            if (reward.Craft != null)
                WorkshopRewardCardView.ShowReward(reward.Craft, null);
            else
                ShowTermReward(reward.Season, reward.Term);
        }
    }
}
