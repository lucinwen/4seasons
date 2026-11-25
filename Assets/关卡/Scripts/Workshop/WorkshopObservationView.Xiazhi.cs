using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private XiazhiPuzzle xiazhiPuzzle;
    private readonly Button[] xiazhiTiles = new Button[XiazhiPuzzle.CellCount];
    private readonly XiazhiThreadGraphic[] xiazhiThreads = new XiazhiThreadGraphic[XiazhiPuzzle.CellCount];
    private Button xiazhiUndo, xiazhiHint;
    private Text xiazhiTally, xiazhiFeedback;
    private int xiazhiHintIndex = -1, xiazhiGeneration;
    private Image xiazhiHintMarker;

    private void ResetXiazhiPuzzle()
    {
        xiazhiGeneration++;
        xiazhiHintIndex = -1;
        xiazhiPuzzle = new XiazhiPuzzle(ChallengeVariant == 0 ? 0 : 1);
    }

    private void DrawXiazhiPuzzle()
    {
        Layer("Summer_CocoonBench");
        WorkshopUi.Fill(art, "Xiazhi Workbench Shade", ArtBounds, new Color32(29,35,25,75));
        if (GameComplete)
        {
            DrawXiazhiCompleted();
            return;
        }
        if (xiazhiPuzzle == null) ResetXiazhiPuzzle();
        var board = WorkshopUi.PixelCard(art, "Xiazhi Silk Board", new Rect(275,65,610,486),
            new Color32(156,112,64,255), new Color32(105,76,48,255));
        WorkshopUi.Label(art, "Xiazhi Board Title", new Rect(35,9,1090,45),
            xiazhiPuzzle.Title, 28, WorkshopUi.Cream, TextAnchor.MiddleCenter);
        for (int i = 0; i < XiazhiPuzzle.CellCount; i++)
        {
            int index = i;
            float left = 86 + i % 4 * 110, top = 23 + i / 4 * 110;
            var tile = WorkshopUi.Button(board, "Xiazhi Tile " + i, new Rect(left,top,104,104), "",
                () => RotateXiazhiTile(index), new Color32(222,192,138,255));
            xiazhiTiles[i] = tile;
            if (xiazhiPuzzle.Mask(i) != 0 && !xiazhiPuzzle.IsLocked(i)) RegisterButton(tile);
            xiazhiThreads[i] = WorkshopUi.Rect(tile.transform, "Pixel Silk", new Rect(2,2,100,100))
                .gameObject.AddComponent<XiazhiThreadGraphic>();
            if (xiazhiPuzzle.IsLocked(i))
                WorkshopUi.Fill(tile.transform, "Fixed Pin", new Rect(91,6,6,6), SummerJade);
        }
        DrawXiazhiEndpoint(board, "Silk Entrance", 11, 23 + xiazhiPuzzle.Start / 4 * 110, "丝头",
            SpringSprite("WorkshopObservation/Summer_CocoonIntact"));
        DrawXiazhiEndpoint(board, "Silk Exit", 535, 23 + xiazhiPuzzle.End / 4 * 110, "丝轴",
            SpringSummerArt("SilkBobbin"));
        xiazhiUndo = XiazhiAction("Xiazhi Undo", "撤回一步", new Rect(244,575,210,56), () =>
        {
            if (!xiazhiPuzzle.Undo()) return;
            ClearXiazhiHint();
            RefreshXiazhiBoard("退回上一步，换个方向再试试。");
        });
        xiazhiHint = XiazhiAction("Xiazhi Hint", "给点提示", new Rect(475,575,210,56), HintXiazhi);
        XiazhiAction("Xiazhi Restart", "重新理线", new Rect(706,575,210,56), () => LoadXiazhiBoard(xiazhiPuzzle.LevelIndex));
        Copy("接通丝路", "点击线段，顺时针转动一格，把左侧丝头接到右侧丝轴。\n接通一张即可完成工单。不计时，不限步数。");
        var wheel = WorkshopUi.Picture(infoPanel, "Xiazhi Reeling Wheel", new Rect(169,322,160,160),
            SpringSummerArt("ReelingWheel"));
        xiazhiTally = WorkshopUi.Label(infoPanel, "Xiazhi Tally", new Rect(20,490,458,52), "", 28,
            SummerDark, TextAnchor.MiddleCenter);
        xiazhiFeedback = WorkshopUi.Label(infoPanel, "Xiazhi Feedback", new Rect(27,552,444,84), "", 25,
            SummerDark, TextAnchor.UpperLeft);
        xiazhiFeedback.lineSpacing = 1.2f;
        RefreshXiazhiBoard("让相邻线段的开口彼此对齐。想再挑战？试试" + xiazhiPuzzle.Target + "次以内接通。");
        if (wheel != null) wheel.raycastTarget = false;
    }

    private void DrawXiazhiEndpoint(Transform parent, string name, float x, float y, string caption, Sprite sprite)
    {
        WorkshopUi.Label(parent, name + " Label", new Rect(x,y+4,62,28), caption, 21,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        WorkshopUi.Picture(parent, name + " Icon", new Rect(x+5,y+33,52,48), sprite);
        WorkshopUi.Label(parent, name + " Arrow", new Rect(x+18,y+78,30,24), "→", 24,
            SummerGold, TextAnchor.MiddleCenter);
    }

    private Button XiazhiAction(string name, string caption, Rect bounds, Action action, bool primary = false)
    {
        var button = WorkshopUi.PixelButton(art, name, bounds, caption, () =>
        {
            if (WorkshopMinigameMenu.BlocksGameplayInput) return;
            WorkshopAudioFeedback.Click();
            action();
        }, primary, 27, SummerJade);
        RegisterButton(button);
        return button;
    }

    private void RefreshXiazhiBoard(string message)
    {
        if (GameComplete || xiazhiPuzzle == null) return;
        for (int i = 0; i < XiazhiPuzzle.CellCount; i++)
        {
            Button tile = xiazhiTiles[i];
            if (tile == null) continue;
            bool empty = xiazhiPuzzle.Mask(i) == 0;
            tile.interactable = !empty && !xiazhiPuzzle.IsLocked(i) && !xiazhiPuzzle.Solved;
            ((Image)tile.targetGraphic).color = empty ? new Color32(173,134,87,255) :
                xiazhiPuzzle.IsLocked(i) ? new Color32(204,206,156,255) :
                xiazhiPuzzle.IsConnected(i) ? new Color32(224,211,154,255) : new Color32(222,192,138,255);
            xiazhiThreads[i].SetThread(xiazhiPuzzle.Mask(i), xiazhiPuzzle.IsConnected(i));
        }
        xiazhiUndo.interactable = xiazhiPuzzle.CanUndo;
        xiazhiHint.interactable = !xiazhiPuzzle.Solved;
        xiazhiTally.text = "转动 " + xiazhiPuzzle.Moves + " 次　已接 " + xiazhiPuzzle.ConnectedCount + " 段";
        xiazhiFeedback.text = message;
    }

    private void RotateXiazhiTile(int index)
    {
        if (WorkshopMinigameMenu.BlocksGameplayInput || GameComplete || readOnly || !xiazhiPuzzle.Rotate(index)) return;
        WorkshopAudioFeedback.Click();
        ClearXiazhiHint();
        if (xiazhiPuzzle.Solved)
        {
            int score = Mathf.Max(1, 1000 - Mathf.Max(0, xiazhiPuzzle.Moves - xiazhiPuzzle.Target) * 5);
            FinishChallenge(score, "丝路接通 · 转动" + xiazhiPuzzle.Moves + "次 · 提示" + xiazhiPuzzle.Hints + "次",
                CaptureThreadSample());
            return;
        }
        RefreshXiazhiBoard(xiazhiPuzzle.ConnectedCount > 1 ? "丝头又往前走了一段，继续接下去。" : "试着转动弯线，让两个开口彼此对齐。");
    }

    // Three-band silk sample, including one join, consumed by autumn light inspection.
    private string CaptureThreadSample()
    {
        int width = ChallengeVariant == 0 ? 10 : 12;
        string warp = new string('1', width);
        var joins = new string('9', width).ToCharArray();
        joins[4] = '4';
        return warp + "/" + new string(joins) + "/" + warp;
    }

    private void ClearXiazhiHint()
    {
        xiazhiGeneration++; xiazhiHintIndex = -1;
        if (xiazhiHintMarker != null) Destroy(xiazhiHintMarker.gameObject);
        xiazhiHintMarker = null;
    }

    private void HintXiazhi()
    {
        if (!xiazhiPuzzle.Hint(out int index, out int turns)) return;
        ClearXiazhiHint(); xiazhiHintIndex = index;
        xiazhiHintMarker = WorkshopUi.Fill(xiazhiTiles[index].transform, "Hint Edge", new Rect(4,4,96,5), SummerGold);
        RefreshXiazhiBoard("试试发亮的这一格：再顺时针转" + turns + "次。");
        StartCoroutine(FadeXiazhiHint(xiazhiGeneration, xiazhiHintMarker));
    }

    private IEnumerator FadeXiazhiHint(int generation, Image marker)
    {
        float elapsed = 0;
        while (generation == xiazhiGeneration && marker != null && elapsed < 5)
        {
            if (WorkshopMinigameMenu.BlocksGameplayInput)
            {
                yield return null;
                continue;
            }
            elapsed += Time.unscaledDeltaTime;
            marker.color = Mathf.FloorToInt(elapsed * 3) % 2 == 0 ? SummerGold : WorkshopUi.Cream;
            yield return null;
        }
        if (generation == xiazhiGeneration) ClearXiazhiHint();
    }

    private void LoadXiazhiBoard(int level)
    {
        ClearXiazhiHint();
        // The legacy save format has practice/challenge orders. All optional challenge
        // boards continue writing order 1 rather than creating incompatible order IDs.
        PrepareChallenge(level == 0 ? 0 : 1);
        xiazhiPuzzle.Load(level);
        Render();
    }

    private void DrawXiazhiCompleted()
    {
        var card = WorkshopUi.PixelCard(art, "Xiazhi Completed Work", new Rect(125,106,910,441),
            SummerNeutral, SummerGold);
        WorkshopUi.Label(card, "Xiazhi Complete Heading", new Rect(25,34,860,65), "丝路已接通", 38,
            SummerDark, TextAnchor.MiddleCenter);
        var wheel = WorkshopUi.Picture(card, "Xiazhi Winding Wheel", new Rect(201,144,186,186), SpringSummerArt("ReelingWheel"));
        WorkshopUi.Picture(card, "Xiazhi Reward Silk", new Rect(526,150,174,174), RewardIcon(observation));
        Copy("接下来", "回夏季择茧、找线轴。集齐盐、索绪和线轴后，回织坊合成生丝。");
        Act("Xiazhi Return To Summer", "返回夏季", Close, true);
        Act("Xiazhi Next Order", "下一张工单", () => LoadXiazhiBoard((xiazhiPuzzle.LevelIndex + 1) % XiazhiPuzzle.LevelCount));
        Act("Xiazhi Replay Order", "再理一回", () => LoadXiazhiBoard(xiazhiPuzzle.LevelIndex));
        StartCoroutine(AnimateXiazhiWheel(wheel));
    }

    private IEnumerator AnimateXiazhiWheel(Image wheel)
    {
        float elapsed = 0;
        while (wheel != null && wheel.gameObject.activeInHierarchy && elapsed < .85f)
        {
            if (WorkshopMinigameMenu.BlocksGameplayInput)
            {
                yield return null;
                continue;
            }
            elapsed += Time.unscaledDeltaTime;
            wheel.rectTransform.localRotation = Quaternion.Euler(0,0,-Mathf.Floor(elapsed * 24) * 15);
            yield return null;
        }
        if (wheel != null) wheel.rectTransform.localRotation = Quaternion.identity;
    }

    private void DrawXiazhiOrderSelectors()
    {
        for (int i = 0; i < XiazhiPuzzle.LevelCount; i++)
        {
            int level = i;
            var button = WorkshopUi.PixelButton(page, "Xiazhi Select " + i, new Rect(28+i*198,848,184,58),
                i == 0 ? "初试" : i == 1 ? "熟手" : "巧匠", () => LoadXiazhiBoard(level),
                xiazhiPuzzle.LevelIndex == i, 26, SummerJade);
            RegisterButton(button);
        }
        WorkshopUi.Label(page, "Xiazhi Optional Note", new Rect(640,848,560,58),
            "接通任一工单即可完成芒种", 25, SummerDark, TextAnchor.MiddleCenter);
    }
}
