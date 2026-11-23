using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private const int MulberryClipLimit = 2;
    private const int MulberryMoveLimit = 7;
    private const int MulberryLeafGoal = 4;
    private static readonly string[] BranchNames = { "甲", "乙", "丙", "丁" };
    private static readonly string[] MulberryLeafKinds = { "tbtmmtbt", "btmtbttm" };
    private static readonly int[][] MulberryCoverBranches =
    {
        new[] { 0, 1, 3, 2, 0, 6, 8, 12 },
        new[] { 0, 5, 1, 10, 2, 9, 0, 4 }
    };
    private static readonly Vector2[] OrchardLeaves =
    {
        new Vector2(148, 148), new Vector2(118, 332), new Vector2(366, 124), new Vector2(353, 308),
        new Vector2(558, 177), new Vector2(578, 353), new Vector2(794, 137), new Vector2(810, 297)
    };
    private static readonly Color32[] BranchInks =
    {
        new Color32(162, 116, 57, 255), new Color32(84, 123, 78, 255),
        new Color32(75, 122, 133, 255), new Color32(136, 100, 126, 255)
    };
    private const string YoungMulberry = "WorkshopObservation/Spring_QingmingYoungMulberry";
    private const string MatureMulberry = "WorkshopObservation/Spring_QingmingMatureMulberry";
    private const string DryMulberry = "WorkshopObservation/Spring_LeafDry";
    private static readonly Color SpringLeafPaper = new Color32(239, 227, 187, 245);
    private static readonly Rect QingmingBasketBounds = new Rect(939, 164, 174, 247);

    private readonly Stack<(int clips, int leaves, int moves)> orchardUndo =
        new Stack<(int clips, int leaves, int moves)>();
    private int mulberryClips;
    private int mulberryPicked;
    private int mulberryMoves;
    private int mulberryInspect = -1;
    private string mulberryMessage;

    private void ResetSpringChallenge()
    {
        if (observation == WorkshopObservation.QingmingMulberry)
            ResetMulberryOrchard();
        else if (observation == WorkshopObservation.GuyuLeaf)
            ResetGuyuOrder();
    }

    private void ResetMulberryOrchard()
    {
        orchardUndo.Clear();
        mulberryClips = 0;
        mulberryPicked = 0;
        mulberryMoves = 0;
        mulberryInspect = -1;
        mulberryMessage = null;
    }

    private void DrawQingmingChallenge()
    {
        Art("Mulberry Garden", "WorkshopGuides/Spring_ChapterGuide", ArtBounds, true);
        WorkshopUi.Fill(art, "Canopy Shade", ArtBounds, new Color32(30, 54, 32, 85));
        DrawMulberryBasket(QingmingBasketBounds);
        if (GameComplete)
        {
            Copy("四片嫩叶入篮", "叶子已经舒展，叶面柔嫩。未展的芽留在枝头，还能继续长大。");
            return;
        }

        for (int branch = 0; branch < BranchNames.Length; branch++)
        {
            int clip = branch;
            bool on = HasSpringBit(mulberryClips, branch);
            float root = 123 + 223 * branch;
            Vector2 from = new Vector2(root, 548);
            Vector2 fork = from + new Vector2(on ? 82 : -15, -112);
            DrawBranchLine(from, fork, BranchInks[branch], 15);
            DrawBranchLine(fork, OrchardLeaves[branch * 2] + new Vector2(0, 23), BranchInks[branch], 9);
            DrawBranchLine(fork, OrchardLeaves[branch * 2 + 1], BranchInks[branch], 7);
            if (on)
            {
                var fastener = WorkshopUi.Picture(art, "Attached Bamboo Clip " + branch,
                    new Rect(fork.x - 28, fork.y + 3, 56, 72), SpringSummerArt("BambooClip"));
                fastener.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            }
            var toggle = WorkshopUi.PixelButton(art, "Toggle Mulberry Clip " + branch, new Rect(root - 96, 550, 194, 54),
                BranchNames[branch] + (on ? "枝 · 取下竹扣" : "枝 · 扣住拨开"), () => ToggleMulberryClip(clip),
                on, 20, BranchInks[branch]);
            RegisterButton(toggle);
        }

        for (int leaf = 0; leaf < OrchardLeaves.Length; leaf++)
            DrawMulberryCanopyLeaf(leaf);

        string budget = ChallengeVariant == 0
            ? $"竹扣上枝 {CountSpringBits(mulberryClips)} / {MulberryClipLimit}"
            : $"竹扣 {CountSpringBits(mulberryClips)} / {MulberryClipLimit}　动作 {mulberryMoves} / {MulberryMoveLimit}";
        Tag("Mulberry Clip Status", budget, new Rect(36, 36, 686, 46));
        Copy("拨开交叠的枝条", "用竹扣拨开挡在前面的枝。写着「甲、乙」的叶片要同时拨开两枝才看得见。挑已舒展的嫩叶，留下未展的芽。", mulberryInspect >= 0);
        if (mulberryMessage != null)
            WorkshopUi.Label(infoPanel, "Mulberry Action Feedback", new Rect(27, 264, 444, 42),
                mulberryMessage, 22, mulberryInspect >= 0 ? ErrorInk : WorkshopUi.Green);
        if (orchardUndo.Count > 0)
        {
            Act("Undo Mulberry Move", "撤回上一步", UndoMulberryMove);
            Act("Reset Mulberry Canopy", "放回竹扣，重采这一枝", () => { ResetMulberryOrchard(); Render(); });
        }
    }

    private void DrawBranchLine(Vector2 from, Vector2 to, Color ink, float width)
    {
        Vector2 delta = to - from;
        var stem = WorkshopUi.Picture(art, "Mulberry Branch", new Rect(from.x, from.y, delta.magnitude + 7, width * 2.75f),
            SpringSummerArt("MulberryBranch"), Color.Lerp(Color.white, ink, 0.24f), stretch: true);
        stem.rectTransform.pivot = new Vector2(0f, 0.5f);
        stem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private void DrawMulberryCanopyLeaf(int leaf)
    {
        Vector2 point = OrchardLeaves[leaf];
        Rect card = new Rect(point.x - 82, point.y - 50, 164, 127);
        int blockers = MulberryCoverBranches[ChallengeVariant][leaf] & ~mulberryClips;
        char kind = MulberryLeafKinds[ChallengeVariant][leaf];
        bool picked = HasSpringBit(mulberryPicked, leaf);
        if (picked || blockers != 0)
        {
            var hint = WorkshopUi.PixelCard(art, "Mulberry Leaf Shade " + leaf, card,
                picked ? new Color32(214, 225, 163, 245) : new Color32(111, 137, 83, 255), WorkshopUi.Bronze);
            if (!picked)
                WorkshopUi.Picture(hint, "Overlapping Branch Leaves", new Rect(24, 4, 116, 60),
                    SpringSprite(MatureMulberry), new Color32(192, 218, 140, 235));
            WorkshopUi.Label(hint, "Covering Branches", new Rect(4, picked ? 34 : 59, 156, 52),
                picked ? "已入篮" : $"{BranchLabel(blockers)}枝遮挡", 21, picked ? WorkshopUi.Green : WorkshopUi.Cream,
                TextAnchor.MiddleCenter);
            return;
        }

        int target = leaf;
        var button = WorkshopUi.PixelButton(art, "Gather Mulberry Leaf " + leaf, card, "",
            () => GatherMulberryLeaf(target), false, 22);
        bool inspecting = mulberryInspect == leaf;
        var imageBounds = kind == 'b' ? new Rect(inspecting ? 46 : 55, 3, inspecting ? 70 : 50, 70)
            : new Rect(inspecting ? 3 : 9, 1, inspecting ? 155 : 145, 88);
        WorkshopUi.Picture(button.transform, "Visible Leaf", imageBounds,
            SpringSprite(kind == 'm' ? MatureMulberry : YoungMulberry));
        string cue = kind == 't' ? "已舒展 · 细叶脉" : kind == 'b' ? "芽尖还卷着" : "叶面较厚";
        WorkshopUi.Label(button.transform, "Leaf Clue", new Rect(3, 83, 154, 34), cue, 20,
            inspecting ? ErrorInk : kind == 't' ? WorkshopUi.Green : WorkshopUi.Faded, TextAnchor.MiddleCenter);
        RegisterButton(button);
    }

    private void DrawMulberryBasket(Rect bounds)
    {
        WorkshopUi.Picture(art, "Qingming Mulberry Basket", bounds, WorkshopObservationArt.Current?.MulberryBasket);
        int gathered = CountSpringBits(mulberryPicked);
        for (int leaf = 0; leaf < gathered; leaf++)
            WorkshopUi.Picture(art, "Gathered Tender Leaf " + leaf,
                new Rect(bounds.x + 12 + leaf * 28, bounds.y + 55 - leaf % 2 * 19, 66, 75), SpringSprite(DryMulberry));
        Tag("Qingming Leaf Goal", $"嫩叶 {gathered} / {MulberryLeafGoal}", new Rect(bounds.x - 4, bounds.yMax + 14, 181, 45));
    }

    private static string BranchLabel(int mask)
    {
        var names = new StringBuilder();
        for (int i = 0; i < BranchNames.Length; i++)
        {
            if (!HasSpringBit(mask, i)) continue;
            if (names.Length > 0) names.Append('、');
            names.Append(BranchNames[i]);
        }
        return names.ToString();
    }

    private void RememberMulberryMove()
    {
        orchardUndo.Push((mulberryClips, mulberryPicked, mulberryMoves));
    }

    private void ToggleMulberryClip(int branch)
    {
        mulberryInspect = -1;
        bool clipped = HasSpringBit(mulberryClips, branch);
        bool changed = false;
        if (!clipped && CountSpringBits(mulberryClips) >= MulberryClipLimit)
            mulberryMessage = "两个竹扣都在枝上，先取下一个。";
        else if (ChallengeVariant > 0 && mulberryMoves >= MulberryMoveLimit)
            mulberryMessage = "拨枝动作已用完，撤回后再试。";
        else
        {
            RememberMulberryMove();
            mulberryClips ^= 1 << branch;
            mulberryMoves++;
            mulberryMessage = clipped ? "竹扣已取回，枝条回到原处。" : "枝条拨开了，看看露出了哪些叶。";
            changed = true;
            WorkshopAudioFeedback.Click();
        }
        Render();
        if (changed && !clipped)
            AnimateSpringSummerSprite(art.Find("Attached Bamboo Clip " + branch)?.GetComponent<Image>(),
                new Vector2(-11, 18), 1.12f, 0.20f);
    }

    private void GatherMulberryLeaf(int leaf)
    {
        if (HasSpringBit(mulberryPicked, leaf) || (MulberryCoverBranches[ChallengeVariant][leaf] & ~mulberryClips) != 0)
            return;
        char kind = MulberryLeafKinds[ChallengeVariant][leaf];
        if (kind != 't')
        {
            mulberryInspect = leaf;
            mulberryMessage = kind == 'b' ? "芽尖还没有舒展，先让它留在枝上。" : "这片叶质稍厚，找叶脉更细的嫩叶。";
            WorkshopAudioFeedback.Notice();
            Render();
            return;
        }
        RememberMulberryMove();
        mulberryInspect = -1;
        mulberryPicked |= 1 << leaf;
        WorkshopAudioFeedback.Collect();
        if (CountSpringBits(mulberryPicked) >= MulberryLeafGoal)
        {
            FinishChallenge(Mathf.Max(0, 100 - mulberryMoves * 8), $"采得四片嫩叶 · 拨枝 {mulberryMoves} 次");
        }
        else
        {
            mulberryMessage = "嫩叶入篮了。";
            Render();
        }
        var gathered = art.Find("Gathered Tender Leaf " + (CountSpringBits(mulberryPicked) - 1))?.GetComponent<Image>();
        if (gathered != null)
        {
            var rect = gathered.rectTransform;
            rect.SetAsLastSibling();
            Vector2 origin = OrchardLeaves[leaf];
            Vector2 topLeft = new Vector2(origin.x - rect.rect.width * 0.5f, -origin.y + rect.rect.height * 0.5f);
            AnimateSpringSummerSprite(gathered, topLeft - rect.anchoredPosition, 1.25f, 0.34f, 48f);
        }
    }

    private void UndoMulberryMove()
    {
        if (orchardUndo.Count == 0) return;
        (mulberryClips, mulberryPicked, mulberryMoves) = orchardUndo.Pop();
        mulberryInspect = -1;
        mulberryMessage = "已撤回上一步。";
        Render();
    }

    private static bool HasSpringBit(int mask, int bit) => (mask & (1 << bit)) != 0;

    private static int CountSpringBits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            mask &= mask - 1;
            count++;
        }
        return count;
    }

    private Sprite SpringSprite(string resource)
    {
        if (!sprites.TryGetValue(resource, out var sprite))
        {
            sprite = Resources.Load<Sprite>(resource);
            sprites.Add(resource, sprite);
        }
        return sprite;
    }

    private Sprite SpringSummerArt(string asset) => SpringSprite("WorkshopGameArt/" + asset);

    private void AnimateSpringSummerSprite(Image picture, Vector2 offset, float scale, float duration, float lift = 0f)
    {
        if (picture != null && picture.sprite != null)
            StartCoroutine(SpringSummerSpriteMotion(picture.rectTransform, offset, scale, duration, lift));
    }

    private static IEnumerator SpringSummerSpriteMotion(RectTransform picture, Vector2 offset, float scale,
        float duration, float lift)
    {
        Vector2 resting = picture.anchoredPosition;
        for (float time = 0f; picture != null && picture.gameObject.activeInHierarchy && time < duration;
             time += Time.unscaledDeltaTime)
        {
            float progress = time / duration;
            float remaining = 1f - Mathf.SmoothStep(0f, 1f, progress);
            picture.anchoredPosition = resting + offset * remaining + Vector2.up * (Mathf.Sin(progress * Mathf.PI) * lift);
            picture.localScale = Vector3.one * Mathf.Lerp(1f, scale, remaining);
            yield return null;
        }
        if (picture == null) yield break;
        picture.anchoredPosition = resting;
        picture.localScale = Vector3.one;
    }
}
