using System.Collections;
using UnityEngine;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private const float XiazhiSceneWidth = 1584f, XiazhiSceneHeight = 892f;
    private const float XiazhiBoardLeft = 484f, XiazhiBoardTop = 136f;
    private const float XiazhiCellPitch = 148f, XiazhiCellSize = 144f;
    private XiazhiPuzzle xiazhiPuzzle;
    private readonly Button[] xiazhiTiles = new Button[XiazhiPuzzle.CellCount];
    private readonly XiazhiThreadGraphic[] xiazhiThreads = new XiazhiThreadGraphic[XiazhiPuzzle.CellCount];
    private readonly Image[] xiazhiProgress = new Image[XiazhiPuzzle.LevelCount];
    private Image xiazhiWheel;
    private Text xiazhiProgressCount;
    private int xiazhiCompletedRounds, xiazhiTotalMoves, xiazhiTotalTarget, xiazhiGeneration;
    private bool xiazhiAdvancing;
    private Coroutine xiazhiRoundRoutine;
    private bool CanReturnFromXiazhi => GameComplete &&
        xiazhiCompletedRounds == XiazhiPuzzle.LevelCount && !xiazhiAdvancing;

    private void ResetXiazhiPuzzle()
    {
        CancelXiazhiRound();
        xiazhiCompletedRounds = xiazhiTotalMoves = xiazhiTotalTarget = 0;
        // Every visit starts with the simplest board, including visits from older completed saves.
        xiazhiPuzzle = new XiazhiPuzzle(0);
    }

    private void CancelXiazhiRound()
    {
        xiazhiGeneration++;
        if (xiazhiRoundRoutine != null) StopCoroutine(xiazhiRoundRoutine);
        xiazhiRoundRoutine = null;
        xiazhiAdvancing = false;
    }

    private void DrawImmersiveXiazhi()
    {
        page = WorkshopUi.Rect(frame, "Xiazhi Immersive Scene", new Rect(168,94,XiazhiSceneWidth,XiazhiSceneHeight));
        art = page;
        infoPanel = null;
        // Keep this container transparent: the dimmed, paused summer scene is the only backdrop.
        DrawXiazhiProgress();
        DrawXiazhiPuzzle();

        var wheelFrame = DrawXiazhiDisplayFrame("Xiazhi Reeling Wheel Frame", new Rect(1280,584,288,288));
        WorkshopUi.Label(wheelFrame, "Xiazhi Wheel Name", new Rect(16,10,256,32), "缫丝转盘", 28,
            WorkshopUi.Cream, TextAnchor.MiddleCenter, WorkshopArt.PixelFont);
        WorkshopUi.Label(wheelFrame, "Xiazhi Wheel Hint", new Rect(16,256,256,24), "转动即成功接通", 22,
            WorkshopUi.Cream, TextAnchor.MiddleCenter, WorkshopArt.PixelFont);
        xiazhiWheel = WorkshopUi.Picture(page, "Xiazhi Reeling Wheel", new Rect(1318,626,212,212),
            SpringSummerArt("ReelingWheel"));
        // Rotate around the axle rather than the top-left corner used by layout rectangles.
        xiazhiWheel.rectTransform.pivot = new Vector2(.5f,.5f);
        xiazhiWheel.rectTransform.anchoredPosition = new Vector2(1424,-732);

        if (CanReturnFromXiazhi)
        {
            var leave = WorkshopUi.PixelButton(page, "Xiazhi Return To Summer", new Rect(638,796,308,64),
                "返回夏季", ReturnFromXiazhi, true, 30, SummerJade);
            RegisterButton(leave);
            BindNavigation(leave);
        }
        else
        {
            BindNavigation();
            // Keep the board visually quiet until the player uses keyboard navigation.
            Select(null);
        }
    }

    private void ReturnFromXiazhi()
    {
        if (!CanReturnFromXiazhi) return;
        WorkshopAudioFeedback.Click();
        Close();
    }

    private void DrawXiazhiProgress()
    {
        var hud = WorkshopUi.Rect(page, "Xiazhi Progress", new Rect(36,28,336,94));
        WorkshopUi.Fill(hud, "Progress Shade", new Rect(0,0,336,94), new Color32(46,32,21,211));
        WorkshopUi.Picture(hud, "Progress Silk", new Rect(16,22,48,48), SpringSummerArt("SilkBobbin"));
        WorkshopUi.Label(hud, "Progress Label", new Rect(80,8,156,30), "接丝进度", 24, WorkshopUi.Cream,
            TextAnchor.MiddleLeft, WorkshopArt.PixelFont);
        xiazhiProgressCount = WorkshopUi.Label(hud, "Progress Count", new Rect(245,8,76,30), "", 24,
            WorkshopUi.Cream, TextAnchor.MiddleRight, WorkshopArt.PixelFont);
        for (int i = 0; i < xiazhiProgress.Length; i++)
        {
            var slot = WorkshopUi.Rect(hud, "Xiazhi Progress Slot " + i, new Rect(80+i*80,48,70,26));
            WorkshopUi.Fill(slot, "Slot", new Rect(0,0,70,26), new Color32(149,118,74,255));
            xiazhiProgress[i] = WorkshopUi.Fill(slot, "Fill", new Rect(3,3,64,20), Color.clear);
        }
        RefreshXiazhiProgress();
    }

    private void RefreshXiazhiProgress()
    {
        for (int i = 0; i < xiazhiProgress.Length; i++)
            if (xiazhiProgress[i] != null)
                xiazhiProgress[i].color = i < xiazhiCompletedRounds ? SummerGold : new Color32(63,48,32,255);
        if (xiazhiProgressCount != null) xiazhiProgressCount.text = xiazhiCompletedRounds + " / 3";
    }

    private void DrawXiazhiPuzzle()
    {
        if (xiazhiPuzzle == null) ResetXiazhiPuzzle();
        // A restrained wooden rim and soft shadow lift the board above the actual summer scene.
        var board = WorkshopUi.Rect(art, "Xiazhi Silk Board", new Rect(XiazhiBoardLeft,XiazhiBoardTop,616,616));
        DrawXiazhiSoftShadow(board, "Board Shadow", new Rect(0,0,616,616));
        WorkshopUi.Fill(board, "Board Wood", new Rect(0,0,616,616), new Color32(93,64,41,255));
        WorkshopUi.Fill(board, "Board Upper Rim", new Rect(5,3,606,3), new Color32(186,151,98,110));
        for (int i = 0; i < XiazhiPuzzle.CellCount; i++)
        {
            int index = i;
            var tile = WorkshopUi.Button(board, "Xiazhi Tile " + i,
                new Rect(14+i%4*XiazhiCellPitch,14+i/4*XiazhiCellPitch,XiazhiCellSize,XiazhiCellSize), "",
                () => RotateXiazhiTile(index), new Color32(222,192,138,255));
            xiazhiTiles[i] = tile;
            var colors = tile.colors;
            colors.disabledColor = Color.white;
            tile.colors = colors;
            if (xiazhiPuzzle.Mask(i) != 0 && !xiazhiPuzzle.IsLocked(i) && !GameComplete) RegisterButton(tile);
            xiazhiThreads[i] = WorkshopUi.Rect(tile.transform, "Pixel Silk", new Rect(0,0,XiazhiCellSize,XiazhiCellSize))
                .gameObject.AddComponent<XiazhiThreadGraphic>();
            if (xiazhiPuzzle.IsLocked(i))
                WorkshopUi.Fill(tile.transform, "Fixed Pin", new Rect(130,8,6,6), SummerJade);
        }
        DrawXiazhiEndpoint("Silk Entrance", XiazhiBoardLeft-168,
            XiazhiBoardTop+14+xiazhiPuzzle.Start/4*XiazhiCellPitch,
            SpringSprite("WorkshopObservation/Summer_CocoonIntact"), true);
        DrawXiazhiEndpoint("Silk Exit", XiazhiBoardLeft+640,
            XiazhiBoardTop+14+xiazhiPuzzle.End/4*XiazhiCellPitch, SpringSummerArt("SilkBobbin"), false);
        RefreshXiazhiBoard();
    }

    private RectTransform DrawXiazhiSoftShadow(Transform parent, string name, Rect bounds)
    {
        var shadow = WorkshopUi.Rect(parent, name, bounds);
        var ambient = WorkshopUi.Fill(shadow, "Ambient", new Rect(-6,-3,bounds.width+16,bounds.height+18),
            new Color32(43,28,18,18));
        var feather = WorkshopUi.Fill(shadow, "Feather", new Rect(-2,2,bounds.width+12,bounds.height+14),
            new Color32(43,28,18,26));
        var contact = WorkshopUi.Fill(shadow, "Contact", new Rect(3,7,bounds.width+4,bounds.height+4),
            new Color32(43,28,18,58));
        WorkshopUi.ApplyPixelShape(ambient);
        WorkshopUi.ApplyPixelShape(feather);
        WorkshopUi.ApplyPixelShape(contact);
        return shadow;
    }

    private RectTransform DrawXiazhiDisplayFrame(string name, Rect bounds)
    {
        var display = WorkshopUi.PixelCard(page, name, bounds,
            new Color32(145,101,56,255), new Color32(79,51,29,255));
        display.Find("Shadow").GetComponent<Image>().color = new Color32(43,28,18,58);
        DrawXiazhiSoftShadow(display, "Display Soft Shadow", new Rect(0,0,bounds.width,bounds.height))
            .SetAsFirstSibling();
        WorkshopUi.Fill(display, "Inset Border", new Rect(7,7,bounds.width-14,bounds.height-14),
            new Color32(94,62,35,255));
        WorkshopUi.Fill(display, "Inset Surface", new Rect(11,11,bounds.width-22,bounds.height-22),
            new Color32(72,101,84,255));
        return display;
    }

    private void DrawXiazhiEndpoint(string name, float x, float y, Sprite sprite, bool entrance)
    {
        // The short lead meets the corresponding fixed cell at its horizontal centre.
        WorkshopUi.Fill(page, name + " Thread", new Rect(entrance ? x+144 : x-38,y+69,38,6), SummerGold);
        var display = DrawXiazhiDisplayFrame(name + " Frame", new Rect(x,y,144,144));
        WorkshopUi.Picture(display, name + " Icon", new Rect(12,12,120,120), sprite);
        var nameplate = WorkshopUi.PixelCard(display, name + " Nameplate", new Rect(0,144,144,38),
            new Color32(42,65,51,255), new Color32(145,101,56,255));
        WorkshopUi.Label(nameplate, "Object Name", new Rect(8,2,128,34), entrance ? "蚕茧" : "丝轴", 28,
            WorkshopUi.Cream, TextAnchor.MiddleCenter, WorkshopArt.PixelFont);
    }

    private void RefreshXiazhiBoard()
    {
        if (xiazhiPuzzle == null) return;
        for (int i = 0; i < XiazhiPuzzle.CellCount; i++)
        {
            Button tile = xiazhiTiles[i];
            if (tile == null) continue;
            bool empty = xiazhiPuzzle.Mask(i) == 0;
            tile.interactable = !GameComplete && !xiazhiAdvancing && !empty &&
                !xiazhiPuzzle.IsLocked(i) && !xiazhiPuzzle.Solved;
            ((Image)tile.targetGraphic).color = empty ? new Color32(143,109,69,255) :
                xiazhiPuzzle.IsLocked(i) ? new Color32(184,191,142,255) :
                xiazhiPuzzle.IsConnected(i) ? new Color32(224,211,154,255) : new Color32(222,192,138,255);
            xiazhiThreads[i].SetThread(xiazhiPuzzle.Mask(i), xiazhiPuzzle.IsConnected(i));
        }
    }

    private void RotateXiazhiTile(int index)
    {
        if (GameComplete || readOnly || xiazhiAdvancing || xiazhiPuzzle == null || !xiazhiPuzzle.Rotate(index)) return;
        WorkshopAudioFeedback.Click();
        RefreshXiazhiBoard();
        if (!xiazhiPuzzle.Solved) return;
        xiazhiAdvancing = true;
        xiazhiCompletedRounds++;
        xiazhiTotalMoves += xiazhiPuzzle.Moves;
        xiazhiTotalTarget += xiazhiPuzzle.Target;
        RefreshXiazhiProgress();
        Select(null);
        xiazhiRoundRoutine = StartCoroutine(AdvanceXiazhiRound(xiazhiGeneration));
    }

    private IEnumerator AdvanceXiazhiRound(int generation)
    {
        WorkshopAudioFeedback.Confirm();
        const float duration = .68f;
        float elapsed = 0f;
        while (current == this && generation == xiazhiGeneration && xiazhiWheel != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed/duration);
            float eased = 1f-Mathf.Pow(1f-t,3f);
            xiazhiWheel.rectTransform.localRotation = Quaternion.Euler(0,0,-720f*eased);
            xiazhiWheel.rectTransform.localScale = Vector3.one*(1f+.045f*Mathf.Sin(t*Mathf.PI));
            yield return null;
        }
        if (current != this || generation != xiazhiGeneration) yield break;
        if (xiazhiWheel != null)
        {
            xiazhiWheel.rectTransform.localRotation = Quaternion.identity;
            xiazhiWheel.rectTransform.localScale = Vector3.one;
        }
        yield return new WaitForSecondsRealtime(.18f);
        if (current != this || generation != xiazhiGeneration) yield break;
        xiazhiRoundRoutine = null;
        xiazhiAdvancing = false;
        if (xiazhiCompletedRounds < XiazhiPuzzle.LevelCount)
        {
            xiazhiPuzzle.Load(xiazhiCompletedRounds);
            Render();
            yield break;
        }
        // Keep the legacy record ID/order and sample format; never stamp the whole activity after just one round.
        int score = Mathf.Max(1,1000-Mathf.Max(0,xiazhiTotalMoves-xiazhiTotalTarget)*5);
        FinishChallenge(score,"三关接丝完成 · 转动" + xiazhiTotalMoves + "次",CaptureThreadSample());
        // FinishChallenge renders the completed scene with one explicit return button.
    }

    // Three-band silk sample, including one join, consumed by autumn light inspection.
    private string CaptureThreadSample()
    {
        int width = ChallengeVariant == 0 ? 10 : 12;
        string warp = new string('1',width);
        var joins = new string('9',width).ToCharArray();
        joins[4] = '4';
        return warp + "/" + new string(joins) + "/" + warp;
    }

#if UNITY_EDITOR
    internal int XiazhiReviewLevel => xiazhiPuzzle != null ? xiazhiPuzzle.LevelIndex : -1;
    internal int XiazhiReviewProgress => xiazhiCompletedRounds;
    internal bool XiazhiReviewAdvancing => xiazhiAdvancing;
    internal bool XiazhiReviewCanReturn => CanReturnFromXiazhi;
    internal void ExitXiazhiReview() => Close();
#endif
}
