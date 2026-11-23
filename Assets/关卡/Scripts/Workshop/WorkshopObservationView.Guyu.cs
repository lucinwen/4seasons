using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private const int GuyuLeafCount = 8;
    private const int GuyuMealSize = 6;
    private const int GuyuClothCapacity = 5;
    private static readonly string[] GuyuLeafKinds = { "ttmttmmm", "tmtmmtmm" };
    private static readonly int[][] GuyuDewCosts =
    {
        new[] { 0, 0, 0, 1, 3, 1, 2, 3 },
        new[] { 2, 0, 0, 3, 1, 1, 0, 2 }
    };
    private static readonly Vector2[] DewCenters =
    {
        new Vector2(221, 166), new Vector2(270, 146), new Vector2(303, 200),
        new Vector2(231, 247), new Vector2(273, 303)
    };
    private static readonly Rect WipeBounds = new Rect(270, 44, 534, 526);
    private static readonly Rect GuyuBasketBounds = new Rect(804, 104, 312, 394);
    private static readonly Rect TenderTray = new Rect(91, 76, 449, 323);
    private static readonly Rect MatureTray = new Rect(617, 76, 449, 323);

    private readonly int[] guyuDew = new int[GuyuLeafCount];
    private readonly bool[] guyuClothCharged = new bool[GuyuLeafCount];
    private readonly List<int> guyuBasket = new List<int>(GuyuMealSize);
    private readonly List<int> guyuFed = new List<int>(GuyuMealSize);
    private readonly Image[] guyuDewMarks = new Image[DewCenters.Length];
    private readonly Image[] guyuFeedingWorms = new Image[2];
    private int guyuClothLeft;
    private int guyuClothSpent;
    private int guyuFlips;
    private int activeGuyuLeaf = -1;
    private int guyuSelectedMealLeaf = -1;
    private bool guyuServing;
    private string guyuMessage;
    private RectTransform guyuWipeBoard;
    private Text guyuDewStatus;
    private Text guyuClothStatus;
    private Vector2? formerDewPointer;

    private void ResetGuyuOrder()
    {
        guyuBasket.Clear();
        guyuFed.Clear();
        Array.Clear(guyuClothCharged, 0, guyuClothCharged.Length);
        for (int leaf = 0; leaf < guyuDew.Length; leaf++)
        {
            int cost = GuyuDewCosts[ChallengeVariant][leaf];
            guyuDew[leaf] = cost == 0 ? 0 : (1 << (cost + 2)) - 1;
        }
        guyuClothLeft = GuyuClothCapacity;
        guyuClothSpent = 0;
        guyuFlips = 0;
        activeGuyuLeaf = -1;
        guyuSelectedMealLeaf = -1;
        guyuServing = false;
        guyuMessage = null;
    }

    private int GuyuTenderTarget => ChallengeVariant == 0 ? 3 : 2;
    private int GuyuMatureTarget => GuyuMealSize - GuyuTenderTarget;
    private bool IsGuyuTender(int leaf) => GuyuLeafKinds[ChallengeVariant][leaf] == 't';
    private bool CanAwardGuyuStory => SceneManager.GetActiveScene().name == WorkshopProgress.SceneName(WorkshopSeason.Spring)
        && SpringSummerNpcDialogue.HasMulberryBriefing;

    private void DrawGuyuChallenge()
    {
        Layer("Spring_LeafDesk");
        guyuWipeBoard = null;
        formerDewPointer = null;
        if (GameComplete || guyuServing)
        {
            DrawGuyuMeal();
            return;
        }
        if (activeGuyuLeaf >= 0)
        {
            DrawGuyuWipeTable(activeGuyuLeaf);
            return;
        }

        DrawGuyuBasket(GuyuBasketBounds);
        for (int leaf = 0; leaf < GuyuLeafCount; leaf++)
            DrawGuyuLeaf(leaf);
        int tender = CountGuyuLeaves(guyuBasket, true);
        int mature = guyuBasket.Count - tender;
        WorkshopUi.PixelCard(art, "Guyu Meal Order", new Rect(30, 34, 757, 72), SpringLeafPaper, WorkshopUi.Bronze);
        WorkshopUi.Label(art, "Guyu Meal Needs", new Rect(47, 39, 722, 60),
            $"幼蚕匾 · 嫩叶 {tender}/{GuyuTenderTarget}     饲育匾 · 适熟叶 {mature}/{GuyuMatureTarget}", 26,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
        Copy("给两匾小蚕备桑叶", "干叶直接拖入篮。\n点开湿叶，移鼠标擦露再入篮。\n点击篮内叶片可以放回。");
        DrawGuyuCloth();
        DrawGuyuMessage();
        if (tender == GuyuTenderTarget && mature == GuyuMatureTarget)
            Act("Guyu Bring This Basket", "把这篮桑叶送到两只蚕匾", StartGuyuServing, true);
        else if (guyuBasket.Count == GuyuMealSize)
            Act("Guyu Check Meal", "核一下两匾份数", CheckGuyuMeal, true);
        if (ChallengeVariant == 0 && guyuClothLeft < GuyuClothCapacity)
            Act("Guyu Flip Cloth", "翻到干布的另一面", FlipGuyuCloth);
        if (guyuClothSpent > 0 || guyuBasket.Count > 0)
            Act("Guyu Retry Order", "重新整理这一单", () => { ResetGuyuOrder(); Render(); });
        if (CanAwardGuyuStory && HasRecord(observation) && SpringSummerNpcDialogue.HasPreparedLeaves &&
            !WorkshopProgress.Has(WorkshopItem.Silkworm))
        {
            Act("Guyu Resume Old Basket", "继续喂上一篮", ResumeOldGuyuBasket);
        }
    }

    private void DrawGuyuLeaf(int leaf)
    {
        Rect tile = new Rect(32 + leaf % 4 * 187, 124 + leaf / 4 * 205, 176, 199);
        bool wet = guyuDew[leaf] != 0;
        bool stored = guyuBasket.Contains(leaf);
        WorkshopUi.PixelCard(art, "Guyu Leaf Card " + leaf, tile, SpringLeafPaper, WorkshopUi.Bronze);
        Rect leafBounds = GuyuLeafSize(leaf, new Rect(tile.x + 15, tile.y + 12, tile.width - 30, 116));
        string title = $"{GuyuLeafLetter(leaf)} · {GuyuLeafName(leaf)}";
        string detail = stored ? "已入篮 · 点此放回" : wet ? $"湿叶 · 用布 {GuyuClothCost(leaf)} 格" : "干叶 · 拖入篮";
        WorkshopUi.Label(art, "Guyu Leaf Identity " + leaf, new Rect(tile.x, tile.y + 128, tile.width, 32),
            title, 23, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        WorkshopUi.Label(art, "Guyu Leaf State " + leaf, new Rect(tile.x, tile.y + 157, tile.width, 36),
            detail, 19, wet ? WorkshopUi.Green : WorkshopUi.Faded, TextAnchor.MiddleCenter);

        if (stored || wet)
        {
            DrawGuyuLeafPicture(art, leaf, leafBounds, stored ? 0.38f : 1f);
            if (wet)
                WorkshopUi.Picture(art, "Guyu Wet Glint " + leaf,
                    new Rect(leafBounds.x + leafBounds.width * 0.42f, leafBounds.y + 10, 37, 44), GuyuDewSprite());
            var button = WorkshopUi.Button(art, (stored ? "Return Guyu Leaf " : "Inspect Wet Leaf ") + leaf, tile, "",
                stored ? (Action)(() => ReturnGuyuLeaf(leaf)) : () => OpenGuyuWipe(leaf), Color.clear);
            RegisterButton(button);
            return;
        }

        BindGuyuLeafDrag(leaf, art, leafBounds, tile, "Drag Dry Leaf ", point => GuyuBasketBounds.Contains(point)
            && BasketGuyuLeaf(leaf), () => BasketGuyuLeaf(leaf));
    }

    private void OpenGuyuWipe(int leaf)
    {
        int reserve = GuyuClothCost(leaf);
        if (reserve > guyuClothLeft)
        {
            guyuMessage = $"这片叶要 {reserve} 格，干布只剩 {guyuClothLeft} 格。";
            WorkshopAudioFeedback.Notice();
        }
        else
        {
            activeGuyuLeaf = leaf;
            guyuMessage = null;
            WorkshopAudioFeedback.Click();
        }
        Render();
    }

    private void DrawGuyuWipeTable(int leaf)
    {
        WorkshopUi.Picture(art, "Dew Wiping Table", WipeBounds, SpringSummerArt("WipeBoard"), stretch: true);
        var leafBounds = new Rect(WipeBounds.x + 10, WipeBounds.y + 15, WipeBounds.width - 20, WipeBounds.height - 30);
        Rect basket = new Rect(873, 223, 226, 295);
        DrawGuyuBasket(basket);
        if (guyuDew[leaf] == 0)
            BindGuyuLeafDrag(leaf, art, leafBounds, leafBounds, "Drag Wiped Leaf ",
                point => basket.Contains(point) && BasketGuyuLeaf(leaf), () => BasketGuyuLeaf(leaf));
        else
            DrawGuyuLeafPicture(art, leaf, leafBounds, 1f);

        guyuWipeBoard = WorkshopUi.Rect(art, "Wipe Gesture Surface", WipeBounds);
        for (int drop = 0; drop < DewCenters.Length; drop++)
        {
            Vector2 position = GuyuDewCenter(leaf, drop);
            guyuDewMarks[drop] = WorkshopUi.Picture(guyuWipeBoard, "Dew Drop " + (drop + 1),
                new Rect(position.x - 20, position.y - 22, 40, 44), GuyuDewSprite());
            guyuDewMarks[drop].enabled = HasSpringBit(guyuDew[leaf], drop);
        }

        Copy($"{GuyuLeafLetter(leaf)} · {GuyuLeafName(leaf)}近景", "让鼠标划过有水珠的地方，连着划过几滴也可以。擦干以后把这片拖入篮中。");
        guyuDewStatus = WorkshopUi.Label(infoPanel, "Remaining Dew", new Rect(27, 212, 444, 44),
            GuyuDewLabel(leaf), 24, WorkshopUi.Green);
        DrawGuyuCloth();
        DrawGuyuMessage();
        Act("Back To Leaf Table", "回到桑叶盘", () => { activeGuyuLeaf = -1; guyuMessage = null; Render(); });
    }

    private void DrawGuyuBasket(Rect bounds)
    {
        WorkshopUi.Picture(art, "Mulberry Collection Basket", bounds, WorkshopObservationArt.Current?.MulberryBasket);
        for (int i = 0; i < guyuBasket.Count; i++)
        {
            Rect leaflet = new Rect(bounds.x + bounds.width * (0.12f + 0.23f * (i % 3)),
                bounds.y + bounds.height * (0.50f - 0.10f * (i / 3)), bounds.width * 0.37f, bounds.height * 0.21f);
            DrawGuyuLeafPicture(art, guyuBasket[i], leaflet, 1f);
        }
        Tag("Basket Drop Target", $"篮内 {guyuBasket.Count} / {GuyuMealSize}", new Rect(bounds.x, bounds.yMax - 11, bounds.width, 48));
    }

    private void DrawGuyuCloth()
    {
        guyuClothStatus = WorkshopUi.Label(infoPanel, "Guyu Cloth", new Rect(27, 257, 444, 46),
            GuyuClothLabel(), 23, WorkshopUi.Green);
    }

    private string GuyuClothLabel() => $"干布剩 {guyuClothLeft}/{GuyuClothCapacity} 格 · 本单已用 {guyuClothSpent} 格";

    private void DrawGuyuMessage(float top = 570)
    {
        if (guyuMessage == null) return;
        WorkshopUi.PixelCard(art, "Guyu Action Feedback", new Rect(36, top, 1088, 43), LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(art, "Guyu Feedback Text", new Rect(48, top + 2, 1064, 39), guyuMessage, 24, ErrorInk,
            TextAnchor.MiddleCenter);
    }

    private bool BasketGuyuLeaf(int leaf)
    {
        if (guyuBasket.Contains(leaf) || guyuDew[leaf] != 0) return false;
        if (guyuBasket.Count >= GuyuMealSize)
        {
            guyuMessage = "篮子已经满了，先放回一片多带的叶。";
            WorkshopAudioFeedback.Notice();
            Render();
            return false;
        }
        guyuBasket.Add(leaf);
        activeGuyuLeaf = -1;
        guyuMessage = null;
        WorkshopAudioFeedback.Place();
        Render();
        return true;
    }

    private void ReturnGuyuLeaf(int leaf)
    {
        guyuBasket.Remove(leaf);
        guyuMessage = null;
        WorkshopAudioFeedback.Click();
        Render();
    }

    private void FlipGuyuCloth()
    {
        if (ChallengeVariant != 0 || guyuClothLeft == GuyuClothCapacity) return;
        guyuFlips++;
        guyuClothLeft = GuyuClothCapacity;
        guyuMessage = "布翻到干的一面了。";
        Render();
    }

    private void CheckGuyuMeal()
    {
        int missing = GuyuTenderTarget - CountGuyuLeaves(guyuBasket, true);
        if (missing != 0)
        {
            guyuMessage = missing > 0
                ? $"幼蚕匾还缺 {missing} 片嫩叶，先放回同样数量的适熟叶。"
                : $"饲育匾还缺 {-missing} 片适熟叶，先放回同样数量的嫩叶。";
            WorkshopAudioFeedback.Notice();
        }
        Render();
    }

    private void StartGuyuServing()
    {
        if (guyuBasket.Count != GuyuMealSize || CountGuyuLeaves(guyuBasket, true) != GuyuTenderTarget)
            return;
        guyuServing = true;
        guyuMessage = null;
        WorkshopAudioFeedback.Milestone();
        Render();
    }

    private void DrawGuyuMeal()
    {
        if (GameComplete)
            Copy("两只蚕匾都喂好了", $"这篮桑叶已经喂完。\n嫩叶 {GuyuTenderTarget} 片，适熟叶 {GuyuMatureTarget} 片。\n本单用布 {guyuClothSpent} 格。");
        else
        {
            Copy("把篮里的桑叶喂进蚕匾", "嫩叶拖进左边幼蚕匾，适熟叶拖进右边饲育匾。送错匾会提示，已送进去的叶片继续留在匾里。");
            if (guyuFed.Count == 0)
                Act("Guyu Back To Basket", "回去整理篮中桑叶", () =>
                {
                    guyuServing = false;
                    guyuSelectedMealLeaf = -1;
                    guyuMessage = null;
                    Render();
                });
        }

        DrawGuyuTray(true, TenderTray);
        DrawGuyuTray(false, MatureTray);
        int index = 0;
        foreach (int leaf in guyuBasket)
        {
            if (guyuFed.Contains(leaf)) continue;
            int identity = leaf;
            Rect card = new Rect(62 + index++ * 174, 452, 159, 112);
            WorkshopUi.PixelCard(art, "Guyu Basket Leaf Card " + leaf, card, SpringLeafPaper,
                guyuSelectedMealLeaf == leaf ? WorkshopUi.Green : WorkshopUi.Bronze);
            WorkshopUi.Label(art, "Guyu Leaf Identity " + leaf, new Rect(card.x, card.y + 76, card.width, 32),
                $"{GuyuLeafLetter(leaf)} · {GuyuLeafName(leaf)}", 21, WorkshopUi.Ink, TextAnchor.MiddleCenter);
            var picture = GuyuLeafSize(leaf, new Rect(card.x + 16, card.y + 4, 127, 72));
            BindGuyuLeafDrag(leaf, art, picture, card, "Serve Guyu Leaf ",
                point => TenderTray.Contains(point) ? FeedGuyuLeaf(identity, true) :
                    MatureTray.Contains(point) && FeedGuyuLeaf(identity, false),
                () => { guyuSelectedMealLeaf = identity; guyuMessage = null; Render(); });
        }
        DrawGuyuMessage(579);
    }

    private void DrawGuyuTray(bool tender, Rect bounds)
    {
        int count = CountGuyuLeaves(guyuFed, tender);
        int target = tender ? GuyuTenderTarget : GuyuMatureTarget;
        string tray = tender ? "幼蚕匾" : "饲育匾";
        WorkshopUi.PixelCard(art, tender ? "Guyu Tender Serving Tray" : "Guyu Mature Serving Tray", bounds,
            SpringLeafPaper, count == target ? WorkshopUi.Green : WorkshopUi.Bronze);
        var trayArt = SpringSummerArt(tender ? "YoungSilkwormTray" : "MatureSilkwormTray")
            ?? WorkshopObservationArt.Current?.SilkwormTray;
        WorkshopUi.Picture(art, "Silkworm Tray " + tray,
            new Rect(bounds.x + 26, bounds.y + 25, bounds.width - 52, 230), trayArt);
        int row = 0;
        Rect lastLeaf = default;
        foreach (int leaf in guyuFed)
        {
            if (IsGuyuTender(leaf) != tender) continue;
            Rect served = new Rect(bounds.x + 86 + row++ * 71, bounds.y + 127, 74, 88);
            DrawGuyuLeafPicture(art, leaf, GuyuLeafSize(leaf, served), 1f);
            lastLeaf = served;
            WorkshopUi.Label(art, "Fed Guyu Leaf " + leaf, new Rect(served.x, served.yMax - 8, 74, 29),
                GuyuLeafLetter(leaf).ToString(), 20, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        }
        guyuFeedingWorms[tender ? 0 : 1] = count == 0 ? null : WorkshopUi.Picture(art, "Guyu Feeding Worm " + tray,
            new Rect(lastLeaf.x - 3, lastLeaf.y - 3, tender ? 42 : 57, tender ? 31 : 42), SpringSummerArt("SilkwormFeeding"));
        WorkshopUi.Label(art, "Guyu Tray Need " + tray, new Rect(bounds.x + 4, bounds.y + 260, bounds.width - 8, 49),
            $"{tray} · {(tender ? "嫩叶" : "适熟叶")} {count} / {target}", 30, WorkshopUi.Green, TextAnchor.MiddleCenter);
        if (GameComplete) return;
        var control = WorkshopUi.Button(art, tender ? "Feed Tender Guyu Tray" : "Feed Mature Guyu Tray", bounds, "", () =>
        {
            if (guyuSelectedMealLeaf >= 0) FeedGuyuLeaf(guyuSelectedMealLeaf, tender);
        }, Color.clear);
        RegisterButton(control);
    }

    private bool FeedGuyuLeaf(int leaf, bool toTenderTray)
    {
        if (!guyuServing || !guyuBasket.Contains(leaf) || guyuFed.Contains(leaf)) return false;
        if (toTenderTray != IsGuyuTender(leaf))
        {
            guyuMessage = $"{GuyuLeafLetter(leaf)} 号是{GuyuLeafName(leaf)}，该送往{(IsGuyuTender(leaf) ? "左边幼蚕匾" : "右边饲育匾")}。";
            WorkshopAudioFeedback.Notice();
            Render();
            return false;
        }
        guyuFed.Add(leaf);
        guyuSelectedMealLeaf = -1;
        guyuMessage = $"已送入 {GuyuLeafLetter(leaf)} 号桑叶。";
        WorkshopAudioFeedback.Fabric();
        if (guyuFed.Count != GuyuMealSize)
        {
            Render();
            AnimateGuyuFeeding(toTenderTray);
            return true;
        }

        guyuMessage = null;
        if (CanAwardGuyuStory)
        {
            SpringPrompt.HasMulberryLeaves = true;
            SpringPrompt.HasSilkwormBaby = true;
            WorkshopProgress.Award(WorkshopItem.MulberryLeaf);
            SpringSummerNpcDialogue.RecordPreparedLeaves();
            WorkshopProgress.Award(WorkshopItem.Silkworm);
        }
        var row = new StringBuilder();
        foreach (int identity in guyuBasket)
            row.Append(IsGuyuTender(identity) ? '2' : '4');
        string leaves = string.Join("、", guyuBasket.ConvertAll(item => GuyuLeafLetter(item).ToString()));
        int score = Mathf.Max(0, 100 - guyuClothSpent * 8 - guyuFlips * 4);
        FinishChallenge(score, $"喂了 {leaves} · 用布 {guyuClothSpent} 格", row.ToString());
        AnimateGuyuFeeding(toTenderTray);
        return true;
    }

    private void AnimateGuyuFeeding(bool tender) =>
        AnimateSpringSummerSprite(guyuFeedingWorms[tender ? 0 : 1], new Vector2(-16f, 16f), 1.08f, 0.25f, 5f);

    private void ResumeOldGuyuBasket()
    {
        var feeder = FeedOverlayManager.Instance;
        Close();
        if (feeder != null)
            feeder.ShowFeed();
        else
            SimplePopup.Show("蚕匾暂不可用", "进入春章后再试一次。", "知道了", null);
    }

    private int CountGuyuLeaves(List<int> leaves, bool tender)
    {
        int count = 0;
        foreach (int id in leaves)
            if (IsGuyuTender(id) == tender) count++;
        return count;
    }

    private static char GuyuLeafLetter(int leaf) => (char)('A' + leaf);
    private string GuyuLeafName(int leaf) => IsGuyuTender(leaf) ? "嫩叶" : "适熟叶";
    private int GuyuClothCost(int leaf) => guyuClothCharged[leaf] ? 0 : GuyuDewCosts[ChallengeVariant][leaf];

    private Rect GuyuLeafSize(int leaf, Rect frame)
    {
        if (!IsGuyuTender(leaf)) return frame;
        return new Rect(frame.x + frame.width * 0.10f, frame.y + frame.height * 0.10f, frame.width * 0.80f, frame.height * 0.80f);
    }

    private Image DrawGuyuLeafPicture(Transform parent, int leaf, Rect bounds, float opacity)
    {
        Color tone = IsGuyuTender(leaf) ? new Color(1.00f, 1.00f, 0.79f, opacity) : new Color(0.72f, 0.85f, 0.66f, opacity);
        return WorkshopUi.Picture(parent, "Guyu Mulberry Leaf " + GuyuLeafLetter(leaf), bounds,
            SpringSprite(DryMulberry), tone);
    }

    private void BindGuyuLeafDrag(int leaf, RectTransform parent, Rect leafBounds, Rect hitBounds, string prefix,
        Func<Vector2, bool> dropped, Action select)
    {
        var hit = WorkshopUi.Button(parent, prefix + leaf, hitBounds, "", select, Color.clear);
        var image = DrawGuyuLeafPicture(hit.transform, leaf,
            new Rect(leafBounds.position - hitBounds.position, leafBounds.size), 1f);
        var events = hit.gameObject.AddComponent<EventTrigger>();
        var transform = hit.GetComponent<RectTransform>();
        Vector2 start = transform.anchoredPosition;
        Vector2 offset = Vector2.zero;
        Color color = image.color;
        AttachGuyuPointer(events, EventTriggerType.BeginDrag, pointer =>
        {
            if (!TryGuyuPointer(parent, pointer.position, out var position)) return;
            pointer.eligibleForClick = false;
            transform.SetAsLastSibling();
            offset = start - new Vector2(position.x, -position.y);
            image.color = color * new Color(1f, 1f, 1f, 0.84f);
        });
        AttachGuyuPointer(events, EventTriggerType.Drag, pointer =>
        {
            if (TryGuyuPointer(parent, pointer.position, out var position))
                transform.anchoredPosition = new Vector2(position.x, -position.y) + offset;
        });
        AttachGuyuPointer(events, EventTriggerType.EndDrag, pointer =>
        {
            if (TryGuyuPointer(parent, pointer.position, out var position) && dropped(position)) return;
            transform.anchoredPosition = start;
            image.color = color;
        });
        RegisterButton(hit);
    }

    private static void AttachGuyuPointer(EventTrigger target, EventTriggerType kind, Action<PointerEventData> onPointer)
    {
        var entry = new EventTrigger.Entry { eventID = kind };
        entry.callback.AddListener(data =>
        {
            if (data is PointerEventData pointer && pointer.button == PointerEventData.InputButton.Left)
                onPointer(pointer);
        });
        target.triggers.Add(entry);
    }

    private static bool TryGuyuPointer(RectTransform target, Vector2 screen, out Vector2 position)
    {
        bool found = RectTransformUtility.ScreenPointToLocalPointInRectangle(target, screen, null, out var local);
        position = new Vector2(local.x, -local.y);
        return found;
    }

    private Vector2 GuyuDewCenter(int leaf, int drop) => DewCenters[drop] +
        new Vector2(((leaf + drop * 2 + ChallengeVariant) % 5 - 2) * 10, ((leaf * 2 + drop) % 5 - 2) * 7);

    private void TickGuyuWiping()
    {
        if (guyuWipeBoard == null || activeGuyuLeaf < 0 || guyuDew[activeGuyuLeaf] == 0) return;
        if (!TryGuyuPointer(guyuWipeBoard, Input.mousePosition, out var position)
            || !new Rect(Vector2.zero, WipeBounds.size).Contains(position))
        {
            formerDewPointer = null;
            return;
        }
        Vector2? previous = formerDewPointer;
        formerDewPointer = position;
        if (previous.HasValue)
            RemoveGuyuDew(activeGuyuLeaf, previous.Value, position);
    }

    private void RemoveGuyuDew(int leaf, Vector2 from, Vector2 to)
    {
        var motion = to - from;
        if (motion.sqrMagnitude < 4f) return;
        int crossed = 0;
        for (int drop = 0; drop < DewCenters.Length; drop++)
        {
            if (!HasSpringBit(guyuDew[leaf], drop)) continue;
            Vector2 center = GuyuDewCenter(leaf, drop);
            float along = Mathf.Clamp01(Vector2.Dot(center - from, motion) / motion.sqrMagnitude);
            if ((center - (from + along * motion)).sqrMagnitude <= 25f * 25f)
                crossed |= 1 << drop;
        }
        if (crossed == 0) return;
        int charged = GuyuClothCost(leaf);
        if (charged > guyuClothLeft) return;
        guyuClothCharged[leaf] = true;
        guyuClothLeft -= charged;
        guyuClothSpent += charged;
        guyuDew[leaf] &= ~crossed;
        for (int drop = 0; drop < DewCenters.Length; drop++)
            if (HasSpringBit(crossed, drop)) guyuDewMarks[drop].enabled = false;
        if (guyuDew[leaf] == 0)
        {
            WorkshopAudioFeedback.Confirm();
            Render();
            AnimateClearedGuyuDew(leaf, crossed);
            return;
        }
        guyuDewStatus.text = GuyuDewLabel(leaf);
        guyuClothStatus.text = GuyuClothLabel();
        WorkshopAudioFeedback.Click();
        AnimateClearedGuyuDew(leaf, crossed);
    }

    private void AnimateClearedGuyuDew(int leaf, int crossed)
    {
        var rippleArt = SpringSummerArt("WaterRipple");
        if (rippleArt == null || guyuWipeBoard == null) return;
        for (int drop = 0; drop < DewCenters.Length; drop++)
        {
            if (!HasSpringBit(crossed, drop)) continue;
            Vector2 point = GuyuDewCenter(leaf, drop);
            var splash = WorkshopUi.Picture(guyuWipeBoard, "Guyu Cleared Dew Ripple",
                new Rect(point.x - 25f, point.y - 12f, 50f, 24f), rippleArt, stretch: true);
            splash.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            splash.rectTransform.anchoredPosition = new Vector2(point.x, -point.y);
            StartCoroutine(FadeGuyuRipple(splash));
        }
    }

    private static IEnumerator FadeGuyuRipple(Image splash)
    {
        const float duration = 0.20f;
        for (float elapsed = 0f; splash != null && splash.gameObject.activeInHierarchy && elapsed < duration;
             elapsed += Time.unscaledDeltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            splash.color = new Color(1f, 1f, 1f, 1f - progress);
            splash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.35f, progress);
            yield return null;
        }
        if (splash != null) Destroy(splash.gameObject);
    }

    private string GuyuDewLabel(int leaf)
    {
        int drops = CountSpringBits(guyuDew[leaf]);
        return drops > 0 ? $"剩余 {drops} 滴 · 还需用布 {GuyuClothCost(leaf)} 格" : "水珠擦净了，拖进篮里。";
    }

    private Sprite GuyuDewSprite() => SpringSummerArt("DewBead");
}
