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
    private const string DryMulberry = "WorkshopObservation/Spring_LeafDry";
    private static readonly Color SpringLeafPaper = new Color32(239, 227, 187, 245);
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
    private readonly Image[] guyuMealLeafPictures = new Image[GuyuLeafCount];
    private int guyuClothLeft;
    private int guyuClothSpent;
    private int guyuFlips;
    private int activeGuyuLeaf = -1;
    private int guyuSelectedMealLeaf = -1;
    private bool? guyuSelectedBundle;
    private bool? guyuRejectedTray;
    private bool guyuNeedsMealPickup;
    private bool guyuGatheredLeaf;
    private bool guyuServing;
    private bool guyuCarriedQingming;
    private bool guyuSunnyWeather;
    private string guyuMessage;
    private RectTransform guyuWipeBoard;
    private Text guyuDewStatus;
    private Text guyuClothStatus;
    private RectTransform guyuGuideProgress;
    private RectTransform guyuWipeGesture;
    private RectTransform guyuDragGesture;
    private Vector2? formerDewPointer;

    private void ResetGuyuOrder() => ResetGuyuWeather(false);

    private void ResetGuyuWeather(bool sunny)
    {
        guyuSunnyWeather = sunny;
        guyuBasket.Clear();
        guyuFed.Clear();
        guyuCarriedQingming = ChallengeVariant == 0 && HasGuyuOrchard;
        if (guyuCarriedQingming && !WorkshopProgress.Has(WorkshopItem.Silkworm))
        {
            guyuBasket.Add(0);
            guyuBasket.Add(1);
        }
        Array.Clear(guyuClothCharged, 0, guyuClothCharged.Length);
        for (int leaf = 0; leaf < guyuDew.Length; leaf++)
        {
            int cost = sunny ? 0 : GuyuDewCosts[ChallengeVariant][leaf];
            guyuDew[leaf] = cost == 0 ? 0 : (1 << (cost + 2)) - 1;
        }
        guyuClothLeft = GuyuClothCapacity;
        guyuClothSpent = 0;
        guyuFlips = 0;
        activeGuyuLeaf = -1;
        guyuSelectedMealLeaf = -1;
        guyuSelectedBundle = null;
        guyuRejectedTray = null;
        guyuNeedsMealPickup = false;
        guyuGatheredLeaf = false;
        guyuServing = false;
        guyuMessage = null;
    }

    private int GuyuTenderTarget => ChallengeVariant == 0 ? 3 : 2;
    private int GuyuMatureTarget => GuyuMealSize - GuyuTenderTarget;
    private bool IsGuyuTender(int leaf) => guyuCarriedQingming ? IsQingmingGuyuLeaf(leaf) : GuyuLeafKinds[ChallengeVariant][leaf] == 't';
    private bool IsQingmingGuyuLeaf(int leaf) => guyuCarriedQingming && (leaf == 0 || leaf == 1 || leaf == 3);
    private bool CanAwardGuyuStory => SceneManager.GetActiveScene().name == WorkshopProgress.SceneName(WorkshopSeason.Spring)
        && SpringSummerNpcDialogue.HasMulberryBriefing;
    private bool HasGuyuOrchard => CanAwardGuyuStory && WorkshopProgress.HasStep("spring.qingming.harvested");

    private void DrawGuyuChallenge()
    {
        Layer("Spring_LeafDesk");
        guyuWipeBoard = null;
        guyuWipeGesture = null;
        guyuDragGesture = null;
        guyuGuideProgress = null;
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
        DrawGuyuQuota(new Rect(57, 36, 337, 66), true, tender, GuyuTenderTarget);
        DrawGuyuQuota(new Rect(417, 36, 337, 66), false, mature, GuyuMatureTarget);
        string orchard = HasGuyuOrchard ? $"{SpringSummerNpcDialogue.HarvestOrchardName}今天新采。" : "桑园今天新采。";
        Copy(guyuSunnyWeather ? "放晴 · 干叶直接入篮" : "谷雨 · 雨后先去水",
            guyuSunnyWeather ? $"{orchard}\n点干叶就能收进篮里。\n凑齐两类叶，再去喂蚕。"
            : $"{orchard}\n点干叶入篮；点湿叶擦水。\n嫩叶和适熟叶各收够份数。");
        Tag("Guyu Weather", guyuSunnyWeather ? "放晴 · 叶面已干" : "雨刚停 · 叶面有水",
            new Rect(818, 36, 298, 61), guyuSunnyWeather);
        DrawGuyuCloth();
        DrawGuyuGuide("① 理桑入篮", GuyuCollectionAction(), (float)guyuBasket.Count / GuyuMealSize);
        DrawGuyuMessage();
        if (tender == GuyuTenderTarget && mature == GuyuMatureTarget)
        {
            Act("Guyu Bring This Basket", "桑叶齐了 · 去喂蚕 →", StartGuyuServing, true);
            var basket = WorkshopUi.Button(art, "Serve Full Mulberry Basket", GuyuBasketBounds, "",
                StartGuyuServing, Color.clear);
            RegisterButton(basket);
            WorkshopUi.Picture(art, "Guyu Basket to Silkworms", new Rect(938, 104, 160, 112),
                SpringSummerArt("YoungSilkwormTray") ?? WorkshopObservationArt.Current?.SilkwormTray);
            VisualStep(basket.GetComponent<RectTransform>());
            Vector2 target = GuyuBasketBounds.center + new Vector2(0f, 30f);
            ShowGesture(art, target, target, WorkGesture.Tap);
        }
        else if (guyuBasket.Count == GuyuMealSize)
            Act("Guyu Check Meal", "核一下两匾份数", CheckGuyuMeal, true);
        if (guyuClothLeft < GuyuClothCapacity)
            Act("Guyu Flip Cloth", "换干布 · 叶片进度保留", FlipGuyuCloth);
        if (guyuClothSpent > 0 || guyuBasket.Count > 0)
            Act("Guyu Retry Order", "重新整理这一单", () => { ResetGuyuWeather(guyuSunnyWeather); Render(); });
        if (CanAwardGuyuStory && HasRecord(observation) && SpringSummerNpcDialogue.HasPreparedLeaves &&
            !WorkshopProgress.Has(WorkshopItem.Silkworm))
        {
            Act("Guyu Resume Old Basket", "继续喂上一篮", ResumeOldGuyuBasket);
        }
        if ((!CanAwardGuyuStory || WorkshopProgress.Has(WorkshopItem.Silkworm)) && guyuBasket.Count == 0 && guyuClothSpent == 0)
            Act("Guyu Compare Weather", guyuSunnyWeather ? "对照 · 再试雨后" : "对照 · 试试放晴",
                () => { ResetGuyuWeather(!guyuSunnyWeather); Render(); });
        if (!guyuGatheredLeaf && (tender != GuyuTenderTarget || mature != GuyuMatureTarget))
            ShowGuyuCollectionGesture();
    }

    private static Rect GuyuLeafTile(int leaf) => new Rect(32 + leaf % 4 * 187, 124 + leaf / 4 * 205, 176, 199);

    private void DrawGuyuQuota(Rect bounds, bool tender, int count, int target)
    {
        for (int i = 0; i < target; i++)
        {
            float size = tender ? 40 : 55;
            var slot = new Rect(bounds.x + i * 62 + (60 - size) / 2f, bounds.y + (66 - size) / 2f, size, size);
            var tint = tender ? new Color(1f, 1f, .79f) : new Color(.72f, .85f, .66f);
            tint.a = i < count ? 1f : .24f;
            WorkshopUi.Picture(art, "Guyu Leaf Quota", slot, SpringSprite(DryMulberry), tint);
        }
        WorkshopUi.Label(art, tender ? "Guyu Tender Quota Count" : "Guyu Mature Quota Count",
            new Rect(bounds.x + 255, bounds.y, 82, bounds.height), $"{count}/{target}", 27, WorkshopUi.Green,
            TextAnchor.MiddleCenter);
    }

    private void ShowGuyuCollectionGesture()
    {
        int sample = -1;
        for (int leaf = 0; leaf < GuyuLeafCount; leaf++)
            if (!guyuBasket.Contains(leaf) && GuyuNeedsLeaf(leaf) && guyuDew[leaf] == 0) { sample = leaf; break; }
        if (sample < 0) return;
        var card = art.Find("Guyu Leaf Card " + sample) as RectTransform;
        if (card != null) VisualStep(card);
        Vector2 target = GuyuLeafTile(sample).center - new Vector2(0f, 34f);
        ShowGesture(art, target, target, WorkGesture.Tap);
    }

    private void DrawGuyuLeaf(int leaf)
    {
        Rect tile = GuyuLeafTile(leaf);
        bool wet = guyuDew[leaf] != 0;
        bool stored = guyuBasket.Contains(leaf);
        bool needed = GuyuNeedsLeaf(leaf);
        WorkshopUi.PixelCard(art, "Guyu Leaf Card " + leaf, tile,
            stored || !needed ? LightSurface : SpringLeafPaper, needed && !stored ? WorkshopUi.Green : WorkshopUi.Bronze);
        Rect leafBounds = GuyuLeafSize(leaf, new Rect(tile.x + 15, tile.y + 12, tile.width - 30, 116));
        string title = $"{GuyuLeafLetter(leaf)} · {GuyuLeafName(leaf)}";
        string detail = stored ? "✓ 已入篮 · 点可放回" : !needed ? "这类够了 · 先收另一类"
            : wet ? $"有水珠 · 耗布 {GuyuClothCost(leaf)}" : "干爽 · 点一下收好";
        if (IsQingmingGuyuLeaf(leaf))
            title = $"{GuyuLeafLetter(leaf)} · 今日鲜嫩叶";
        WorkshopUi.Label(art, "Guyu Leaf Identity " + leaf, new Rect(tile.x, tile.y + 128, tile.width, 32),
            title, 23, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        WorkshopUi.Label(art, "Guyu Leaf State " + leaf, new Rect(tile.x, tile.y + 157, tile.width, 36),
            detail, 19, wet ? WorkshopUi.Green : WorkshopUi.Faded, TextAnchor.MiddleCenter);

        if (stored || wet)
        {
            DrawGuyuLeafPicture(art, leaf, leafBounds, stored || !needed ? 0.38f : 1f);
            if (wet)
                for (int drop = 0; drop < Mathf.Max(1, GuyuClothCost(leaf)); drop++)
                    WorkshopUi.Picture(art, "Guyu Wet Glint " + leaf + ":" + drop,
                        new Rect(tile.x + 91 + drop * 23, tile.y + 11, 24, 31), GuyuDewSprite());
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
        if (!GuyuNeedsLeaf(leaf))
        {
            guyuMessage = $"{GuyuLeafName(leaf)}已经够了，看看另一类桑叶。";
            WorkshopAudioFeedback.Notice();
            Render();
            return;
        }
        int reserve = GuyuClothCost(leaf);
        activeGuyuLeaf = leaf;
        if (reserve > guyuClothLeft)
        {
            guyuMessage = "布已经湿了，点右边「换干布」就能继续擦。";
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
        Rect basket = new Rect(873, 223, 226, 275);
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

        bool dry = guyuDew[leaf] == 0;
        Copy(dry ? "擦干了 · 收进篮里" : $"{GuyuLeafLetter(leaf)} · 擦掉水珠", dry
            ? "这片叶已经干爽。\n点下面的「收进篮里」，\n也可以直接拖到右边篮子。"
            : "把鼠标移到大叶片上，\n来回划过蓝色水珠。\n不用按住鼠标。");
        guyuDewStatus = WorkshopUi.Label(infoPanel, "Remaining Dew", new Rect(27, 212, 444, 44),
            GuyuDewLabel(leaf), 24, WorkshopUi.Green);
        DrawGuyuCloth();
        DrawGuyuGuide(dry ? "① 理桑入篮" : "擦露中", dry ? "✓ 水珠全擦净了，点「收进篮里」继续。"
            : "鼠标划过蓝色水珠就能擦掉，试着左右来回擦。", GuyuWipeProgress(leaf));
        DrawGuyuMessage();
        if (dry)
        {
            Act("Guyu Basket Wiped Leaf", "✓ 擦干了 · 收进篮里", () => BasketGuyuLeaf(leaf), true);
            var target = WorkshopUi.Button(art, "Basket This Dry Mulberry", basket, "", () => { BasketGuyuLeaf(leaf); }, Color.clear);
            RegisterButton(target);
            VisualStep(target.GetComponent<RectTransform>());
            guyuDragGesture = ShowGesture(art, leafBounds.center + new Vector2(0f, 56f), basket.center, WorkGesture.Drag);
        }
        else if (GuyuClothCost(leaf) > guyuClothLeft)
        {
            Act("Guyu Wipe Fresh Cloth", "换干布 · 继续擦这片", FlipGuyuCloth, true);
            var fresh = infoPanel.Find("Guyu Wipe Fresh Cloth") as RectTransform;
            if (fresh != null)
            {
                var point = new Vector2(fresh.rect.width * .8f, fresh.rect.height * .5f);
                WorkshopUi.Picture(fresh, "Dry Cloth Symbol", new Rect(10, 7, 49, 47), SpringSummerArt("CuttingCloth"));
                VisualStep(fresh);
                ShowGesture(fresh, point, point, WorkGesture.Tap);
            }
        }
        else
            ShowGuyuWipeGesture(leaf);
        Act("Back To Leaf Table", "回到桑叶盘", () => { activeGuyuLeaf = -1; guyuMessage = null; Render(); });
    }

    private void ShowGuyuWipeGesture(int leaf)
    {
        if (guyuWipeGesture != null)
        {
            guyuWipeGesture.gameObject.SetActive(false);
            Destroy(guyuWipeGesture.gameObject);
            guyuWipeGesture = null;
        }
        for (int drop = 0; drop < DewCenters.Length; drop++)
        {
            if (!HasSpringBit(guyuDew[leaf], drop)) continue;
            Vector2 point = GuyuDewCenter(leaf, drop);
            guyuWipeGesture = ShowGesture(guyuWipeBoard,
                point - new Vector2(76f, 9f), point + new Vector2(76f, 9f), WorkGesture.Move);
            return;
        }
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

    private string GuyuClothLabel() => guyuSunnyWeather ? "叶面已干 · 本单无需擦拭"
        : $"干布剩 {guyuClothLeft}/{GuyuClothCapacity} 格 · 本单已用 {guyuClothSpent} 格";

    private void DrawGuyuGuide(string step, string action, float progress)
    {
        var guide = WorkshopUi.PixelCard(art, "Guyu Current Action", new Rect(30, 540, 1088, 65),
            LightSurface, WorkshopUi.Green);
        WorkshopUi.Label(guide, "Step", new Rect(16, 3, 1048, 25), step, 21, WorkshopUi.Green);
        WorkshopUi.Label(guide, "Next Action", new Rect(16, 28, 1048, 31), action, 24, WorkshopUi.Ink);
        WorkshopUi.Fill(guide, "Progress Track", new Rect(16, 60, 1056, 3), WorkshopUi.Line);
        guyuGuideProgress = WorkshopUi.Fill(guide, "Progress", new Rect(16, 60, 1056 * Mathf.Clamp01(progress), 3),
            WorkshopUi.Green).rectTransform;
    }

    private string GuyuCollectionAction()
    {
        int tender = GuyuTenderTarget - CountGuyuLeaves(guyuBasket, true);
        int mature = GuyuMatureTarget - CountGuyuLeaves(guyuBasket, false);
        if (tender == 0 && mature == 0) return "✓ 两类桑叶都齐了，点右边「去喂蚕」。";
        if (tender == 0) return $"嫩叶够了！再收 {mature} 片适熟叶。干叶点一下，湿叶先擦。";
        if (mature == 0) return $"适熟叶够了！再收 {tender} 片嫩叶。干叶点一下，湿叶先擦。";
        return $"再收嫩叶 {tender} 片、适熟叶 {mature} 片。点干叶入篮，点湿叶擦水。";
    }

    private bool GuyuNeedsLeaf(int leaf) => CountGuyuLeaves(guyuBasket, IsGuyuTender(leaf))
        < (IsGuyuTender(leaf) ? GuyuTenderTarget : GuyuMatureTarget);

    private void DrawGuyuMessage(float top = 610)
    {
        if (guyuMessage == null) return;
        WorkshopUi.PixelCard(art, "Guyu Action Feedback", new Rect(36, top, 1088, 37), LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(art, "Guyu Feedback Text", new Rect(48, top + 1, 1064, 35), guyuMessage, 22, ErrorInk,
            TextAnchor.MiddleCenter);
    }

    private bool BasketGuyuLeaf(int leaf)
    {
        if (guyuBasket.Contains(leaf) || guyuDew[leaf] != 0) return false;
        if (!GuyuNeedsLeaf(leaf))
        {
            guyuMessage = $"{GuyuLeafName(leaf)}已经够了，先收另一类桑叶。";
            WorkshopAudioFeedback.Notice();
            Render();
            return false;
        }
        if (guyuBasket.Count >= GuyuMealSize)
        {
            guyuMessage = "篮子已经满了，先放回一片多带的叶。";
            WorkshopAudioFeedback.Notice();
            Render();
            return false;
        }
        guyuBasket.Add(leaf);
        guyuGatheredLeaf = true;
        activeGuyuLeaf = -1;
        guyuMessage = $"✓ {GuyuLeafLetter(leaf)} 号{GuyuLeafName(leaf)}已入篮。";
        WorkshopAudioFeedback.Place();
        Render();
        var filledBasket = art.Find("Guyu Mulberry Leaf " + GuyuLeafLetter(leaf));
        if (filledBasket != null)
            AnimateSpringSummerSprite(filledBasket.GetComponent<Image>(), new Vector2(-22f, 28f), 1.18f, .23f, 7f);
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
        if (guyuClothLeft == GuyuClothCapacity) return;
        guyuFlips++;
        guyuClothLeft = GuyuClothCapacity;
        guyuMessage = "换好干布了，继续擦水珠，已擦掉的不会回来。";
        WorkshopAudioFeedback.Place();
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
        guyuSelectedBundle = null;
        guyuRejectedTray = null;
        guyuNeedsMealPickup = false;
        guyuMessage = null;
        WorkshopAudioFeedback.Milestone();
        Render();
    }

    private void DrawGuyuMeal()
    {
        Array.Clear(guyuMealLeafPictures, 0, guyuMealLeafPictures.Length);
        if (GameComplete)
            Copy("两只蚕匾都喂好了", $"本单用布 {guyuClothSpent} 格。\n小蚕继续长大、上蔟结茧。\n芒种验收成熟的春茧。");
        else
        {
            string heading = "给两匾分桑叶";
            string instruction = "拖一把叶到蚕匾，也可先点选再投喂。";
            if (guyuSelectedBundle.HasValue)
            {
                heading = guyuSelectedBundle.Value ? "已选整把嫩叶" : "已选整把适熟叶";
                instruction = "对照蚕匾上的叶片图样，点一个蚕匾。";
            }
            else if (guyuSelectedMealLeaf >= 0)
            {
                bool tender = IsGuyuTender(guyuSelectedMealLeaf);
                heading = $"已选 {GuyuLeafLetter(guyuSelectedMealLeaf)} 号{GuyuLeafName(guyuSelectedMealLeaf)}";
                instruction = $"点{(tender ? "左" : "右")}边亮起的蚕匾。";
            }
            Copy(heading, instruction);
            if (guyuFed.Count == 0)
                Act("Guyu Back To Basket", "回去整理篮中桑叶", () =>
                {
                    guyuServing = false;
                    guyuSelectedMealLeaf = -1;
                    guyuSelectedBundle = null;
                    guyuRejectedTray = null;
                    guyuNeedsMealPickup = false;
                    guyuMessage = null;
                    Render();
                });
        }

        DrawGuyuTray(true, TenderTray);
        DrawGuyuTray(false, MatureTray);
        if (!GameComplete)
        {
            DrawGuyuMealBundle(false);
            DrawGuyuMealBundle(true);
            string next = guyuSelectedBundle.HasValue || guyuSelectedMealLeaf >= 0 ? "点选蚕匾投喂。" : "先抓下方一把叶，再送到蚕匾。";
            DrawGuyuGuide($"② 分叶喂蚕 · {guyuFed.Count}/{GuyuMealSize}", next, (float)guyuFed.Count / GuyuMealSize);
            if (guyuSelectedMealLeaf >= 0)
            {
                Rect tray = IsGuyuTender(guyuSelectedMealLeaf) ? TenderTray : MatureTray;
                Vector2 target = new Vector2(tray.center.x, tray.y + 146f);
                guyuDragGesture = ShowGesture(art, target, target, WorkGesture.Tap);
            }
            else if (GuyuShowsFirstFeed)
                ShowGuyuBundleDrag();
        }
        DrawGuyuMessage();
    }

    private static Rect GuyuBundleBounds(bool tender) => new Rect(tender ? 610 : 61, 428, 489, 103);

    private bool GuyuShowsFirstFeed => guyuFed.Count == 0 && guyuSelectedMealLeaf < 0
        && !guyuSelectedBundle.HasValue && !guyuRejectedTray.HasValue;

    private void ShowGuyuBundleDrag()
    {
        Rect grip = GuyuBundleGrip(true);
        Vector2 from = new Vector2(grip.x + 45f, grip.y + 30f);
        Vector2 to = new Vector2(TenderTray.center.x + 45f, TenderTray.y + 148f);
        guyuDragGesture = ShowGesture(art, from, to, WorkGesture.Drag);
        var pointerPress = guyuDragGesture.Find("Mouse Pointer/Mouse Press");
        if (pointerPress == null) return;
        int stack = 0;
        foreach (int leaf in guyuBasket)
        {
            if (!IsGuyuTender(leaf)) continue;
            var ghostLeaf = DrawGuyuLeafPicture(pointerPress, leaf, new Rect(-78 + stack++ * 12, -24, 45, 58), .72f);
            ghostLeaf.transform.SetAsFirstSibling();
        }
    }

    private static Rect GuyuBundleGrip(bool tender)
    {
        Rect row = GuyuBundleBounds(tender);
        return new Rect(row.x + 8, row.y + 7, 188, row.height - 14);
    }

    private void DrawGuyuMealBundle(bool tender)
    {
        Rect row = GuyuBundleBounds(tender);
        Rect grip = GuyuBundleGrip(tender);
        int remaining = CountGuyuLeaves(guyuBasket, tender) - CountGuyuLeaves(guyuFed, tender);
        bool selected = guyuSelectedBundle.HasValue && guyuSelectedBundle.Value == tender;
        string kind = tender ? "嫩叶" : "适熟叶";
        WorkshopUi.PixelCard(art, "Guyu Meal Bundle " + kind, row,
            remaining == 0 ? LightSurface : SpringLeafPaper, selected ? WorkshopUi.Green : WorkshopUi.Bronze);
        if (remaining == 0)
        {
            WorkshopUi.Label(art, "Guyu Bundle Served", row, $"✓ {kind}已投喂", 25,
                WorkshopUi.Green, TextAnchor.MiddleCenter);
            return;
        }

        string name = tender ? "Serve Guyu Tender Bundle" : "Serve Guyu Mature Bundle";
        var whole = WorkshopUi.Button(art, name, grip, "", () =>
        {
            guyuSelectedBundle = tender;
            guyuSelectedMealLeaf = -1;
            guyuRejectedTray = null;
            guyuNeedsMealPickup = false;
            guyuMessage = null;
            WorkshopAudioFeedback.Click();
            Render();
        }, Color.clear);
        WorkshopUi.PixelCard(whole.transform, "Guyu Bundle Grip Backing", new Rect(0, 0, grip.width, grip.height),
            selected ? new Color32(218, 236, 183, 255) : LightSurface, selected ? WorkshopUi.Green : WorkshopUi.Bronze);

        int index = 0;
        foreach (int leaf in guyuBasket)
        {
            if (guyuFed.Contains(leaf) || IsGuyuTender(leaf) != tender) continue;
            int identity = leaf;
            DrawGuyuLeafPicture(whole.transform, leaf, new Rect(8 + index * 8, 8, 38, 51), 1f);
            float width = (row.width - 213f) / remaining;
            Rect card = new Rect(row.x + 204 + index++ * width, row.y + 7, width - 4, row.height - 14);
            WorkshopUi.PixelCard(art, "Guyu Basket Leaf Card " + leaf, card, SpringLeafPaper,
                guyuSelectedMealLeaf == leaf ? WorkshopUi.Green : WorkshopUi.Bronze);
            WorkshopUi.Label(art, "Guyu Leaf Identity " + leaf, new Rect(card.x, card.y + 60, card.width, 26),
                GuyuLeafLetter(leaf).ToString(), 20, WorkshopUi.Ink, TextAnchor.MiddleCenter);
            var picture = GuyuLeafSize(leaf, new Rect(card.center.x - 38, card.y + 1, 76, 63));
            BindGuyuLeafDrag(leaf, art, picture, card, "Serve Guyu Leaf ",
                point => TryDropGuyuMeal(point, target => FeedGuyuLeaf(identity, target)),
                () =>
                {
                    guyuSelectedMealLeaf = identity;
                    guyuSelectedBundle = null;
                    guyuRejectedTray = null;
                    guyuNeedsMealPickup = false;
                    guyuMessage = null;
                    WorkshopAudioFeedback.Click();
                    Render();
                });
        }
        WorkshopUi.Label(whole.transform, "Guyu Bundle Kind", new Rect(72, 8, 111, 41), $"{kind} ×{remaining}", 22,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
        bool showSource = GuyuShowsFirstFeed ? tender : guyuNeedsMealPickup;
        WorkshopUi.Label(whole.transform, "Guyu Bundle Action", new Rect(9, 54, grip.width - 18, 32),
            selected ? "已选整把" : showSource ? "先抓这一把" : "整把投喂", 22, WorkshopUi.Green, TextAnchor.MiddleCenter);
        BindGuyuDrag(whole, art, point => TryDropGuyuMeal(point, target => FeedGuyuBundle(tender, target)));
        if (selected || showSource)
            VisualStep((RectTransform)whole.transform);
    }

    private static bool TryDropGuyuMeal(Vector2 point, Func<bool, bool> feed)
    {
        if (TenderTray.Contains(point)) return feed(true);
        return MatureTray.Contains(point) && feed(false);
    }

    private void DrawGuyuTray(bool tender, Rect bounds)
    {
        int count = CountGuyuLeaves(guyuFed, tender);
        int target = tender ? GuyuTenderTarget : GuyuMatureTarget;
        string tray = tender ? "幼蚕匾" : "饲育匾";
        bool receiving = !GameComplete && guyuSelectedMealLeaf >= 0 && IsGuyuTender(guyuSelectedMealLeaf) == tender;
        bool choosing = !GameComplete && count < target && guyuSelectedBundle.HasValue;
        bool rejected = guyuRejectedTray == tender;
        WorkshopUi.PixelCard(art, tender ? "Guyu Tender Serving Tray" : "Guyu Mature Serving Tray", bounds,
            receiving || choosing ? WorkshopUi.Cream : SpringLeafPaper,
            rejected ? ErrorInk : receiving || count == target ? WorkshopUi.Green : WorkshopUi.Bronze);
        WorkshopUi.Label(art, "Guyu Tray Action " + tray, new Rect(bounds.x, bounds.y - 44, bounds.width, 39),
            receiving ? $"↓ 点这里喂 {GuyuLeafLetter(guyuSelectedMealLeaf)} 号{GuyuLeafName(guyuSelectedMealLeaf)}"
                : count == target ? "✓ 这匾已喂好" : rejected ? "× 这匾不吃这把叶" : tray,
            26, rejected ? ErrorInk : WorkshopUi.Green, TextAnchor.MiddleCenter);
        var trayArt = SpringSummerArt(tender ? "YoungSilkwormTray" : "MatureSilkwormTray")
            ?? WorkshopObservationArt.Current?.SilkwormTray;
        WorkshopUi.Picture(art, "Silkworm Tray " + tray,
            new Rect(bounds.x + 26, bounds.y + 25, bounds.width - 52, 230), trayArt);
        DrawGuyuQuota(new Rect(bounds.x + 57, bounds.y + 214, 337, 66), tender, count, target);
        int row = 0;
        Rect lastLeaf = default;
        foreach (int leaf in guyuFed)
        {
            if (IsGuyuTender(leaf) != tender) continue;
            Rect served = new Rect(bounds.x + 86 + row++ * 71, bounds.y + 127, 74, 88);
            guyuMealLeafPictures[leaf] = DrawGuyuLeafPicture(art, leaf, GuyuLeafSize(leaf, served), 1f);
            lastLeaf = served;
            WorkshopUi.Label(art, "Fed Guyu Leaf " + leaf, new Rect(served.x, served.yMax - 8, 74, 29),
                GuyuLeafLetter(leaf).ToString(), 20, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        }
        guyuFeedingWorms[tender ? 0 : 1] = count == 0 ? null : WorkshopUi.Picture(art, "Guyu Feeding Worm " + tray,
            new Rect(lastLeaf.x - 3, lastLeaf.y - 3, tender ? 42 : 57, tender ? 31 : 42), SpringSummerArt("SilkwormFeeding"));
        WorkshopUi.Label(art, "Guyu Tray Need " + tray, new Rect(bounds.x + 4, bounds.y + 272, bounds.width - 8, 41),
            $"{tray} · {(tender ? "嫩叶" : "适熟叶")} {count} / {target}", 30, WorkshopUi.Green, TextAnchor.MiddleCenter);
        if (GameComplete) return;
        var control = WorkshopUi.Button(art, tender ? "Feed Tender Guyu Tray" : "Feed Mature Guyu Tray", bounds, "", () =>
        {
            if (guyuSelectedBundle.HasValue)
                FeedGuyuBundle(guyuSelectedBundle.Value, tender);
            else if (guyuSelectedMealLeaf >= 0)
                FeedGuyuLeaf(guyuSelectedMealLeaf, tender);
            else
            {
                guyuNeedsMealPickup = true;
                guyuRejectedTray = null;
                guyuMessage = "匾上还没叶。先点或拖亮框里的桑叶。";
                WorkshopAudioFeedback.Notice();
                Render();
            }
        }, Color.clear);
        RegisterButton(control);
        if (receiving || choosing || rejected)
            VisualStep((RectTransform)control.transform, rejected ? (Color?)ErrorInk : null);
    }

    private bool FeedGuyuLeaf(int leaf, bool toTenderTray)
    {
        if (!guyuServing || !guyuBasket.Contains(leaf) || guyuFed.Contains(leaf)) return false;
        bool tender = IsGuyuTender(leaf);
        if (toTenderTray != tender)
            return RejectGuyuFeed(toTenderTray, tender, $"{GuyuLeafLetter(leaf)} 号是{GuyuLeafName(leaf)}");
        int first = guyuFed.Count;
        guyuFed.Add(leaf);
        CompleteGuyuFeeding(toTenderTray, first, $"已送入 {GuyuLeafLetter(leaf)} 号桑叶。");
        return true;
    }

    private bool FeedGuyuBundle(bool tender, bool toTenderTray)
    {
        if (!guyuServing || GameComplete) return false;
        string kind = tender ? "嫩叶" : "适熟叶";
        if (tender != toTenderTray)
            return RejectGuyuFeed(toTenderTray, tender, $"这是{kind}");

        int first = guyuFed.Count;
        foreach (int leaf in guyuBasket)
            if (IsGuyuTender(leaf) == tender && !guyuFed.Contains(leaf)) guyuFed.Add(leaf);
        if (guyuFed.Count == first) return false;
        WorkshopAudioFeedback.Confirm();
        CompleteGuyuFeeding(toTenderTray, first, $"✓ {guyuFed.Count - first} 片{kind}已投喂！");
        return true;
    }

    private bool RejectGuyuFeed(bool wrongTray, bool tender, string reason)
    {
        guyuNeedsMealPickup = false;
        guyuRejectedTray = wrongTray;
        guyuMessage = $"{reason}，该送往{(tender ? "左边幼蚕匾" : "右边饲育匾")}。";
        WorkshopAudioFeedback.Notice();
        Render();
        return false;
    }

    private void CompleteGuyuFeeding(bool toTenderTray, int first, string feedback)
    {
        guyuSelectedMealLeaf = -1;
        guyuSelectedBundle = null;
        guyuRejectedTray = null;
        guyuNeedsMealPickup = false;
        guyuMessage = feedback;
        WorkshopAudioFeedback.Fabric();
        if (guyuFed.Count != GuyuMealSize)
        {
            Render();
            AnimateGuyuFeeding(toTenderTray, first);
            return;
        }

        guyuMessage = null;
        if (CanAwardGuyuStory)
        {
            if (!WorkshopProgress.Has(WorkshopItem.Silkworm))
                WorkshopProgress.MarkStep("spring.guyu.batchFed");
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
        AnimateGuyuFeeding(toTenderTray, first);
    }

    private void AnimateGuyuFeeding(bool tender, int first)
    {
        for (int i = first; i < guyuFed.Count; i++)
        {
            var leaf = guyuMealLeafPictures[guyuFed[i]];
            float delay = (i - first) * 4f;
            AnimateSpringSummerSprite(leaf, new Vector2(tender ? 14f : -14f, -27f - delay), 1.16f, 0.27f + delay / 60f, 7f);
        }
        AnimateSpringSummerSprite(guyuFeedingWorms[tender ? 0 : 1], new Vector2(-16f, 16f), 1.08f, 0.25f, 5f);
    }

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
        DrawGuyuLeafPicture(hit.transform, leaf, new Rect(leafBounds.position - hitBounds.position, leafBounds.size), 1f);
        BindGuyuDrag(hit, parent, dropped);
    }

    private void BindGuyuDrag(Button hit, RectTransform parent, Func<Vector2, bool> dropped)
    {
        var events = hit.gameObject.AddComponent<EventTrigger>();
        var content = hit.gameObject.AddComponent<CanvasGroup>();
        var transform = hit.GetComponent<RectTransform>();
        Vector2 start = transform.anchoredPosition;
        Vector2 offset = Vector2.zero;
        CanvasGroup demonstration = null;
        AttachGuyuPointer(events, EventTriggerType.BeginDrag, pointer =>
        {
            if (!TryGuyuPointer(parent, pointer.position, out var position)) return;
            demonstration = guyuDragGesture != null ? guyuDragGesture.GetComponent<CanvasGroup>() : null;
            if (demonstration != null) demonstration.alpha = 0f;
            pointer.eligibleForClick = false;
            transform.SetAsLastSibling();
            offset = start - new Vector2(position.x, -position.y);
            content.alpha = 0.84f;
        });
        AttachGuyuPointer(events, EventTriggerType.Drag, pointer =>
        {
            if (TryGuyuPointer(parent, pointer.position, out var position))
                transform.anchoredPosition = new Vector2(position.x, -position.y) + offset;
        });
        AttachGuyuPointer(events, EventTriggerType.EndDrag, pointer =>
        {
            if (demonstration != null) demonstration.alpha = 1f;
            if (TryGuyuPointer(parent, pointer.position, out var position) && dropped(position)) return;
            transform.anchoredPosition = start;
            content.alpha = 1f;
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
            if ((center - (from + along * motion)).sqrMagnitude <= 38f * 38f)
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
        ShowGuyuWipeGesture(leaf);
        if (guyuGuideProgress != null)
            guyuGuideProgress.sizeDelta = new Vector2(1056 * GuyuWipeProgress(leaf), 3);
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
        return drops > 0 ? $"还有 {drops} 滴 · 鼠标来回擦" : "✓ 水珠擦净了，可以收进篮。";
    }

    private float GuyuWipeProgress(int leaf) => 1f - (float)CountSpringBits(guyuDew[leaf])
        / (GuyuDewCosts[ChallengeVariant][leaf] + 2);

    private Sprite GuyuDewSprite() => SpringSummerArt("DewBead");
}
