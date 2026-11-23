using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private const int OfferingColumns = 5;
    private const int OfferingSeatCount = 10;
    private const int SilkOffering = 3;
    private static readonly string[] ExhibitionNames = { "蚕花酒", "果篮", "糕点", "丝束", "桑叶" };
    private static readonly int[] ExhibitionHeights = { 3, 2, 1, 1, 1 };
    private static readonly string[] ExhibitionInks = { "3", "4", "7", "9", "2" };
    private static readonly int[] ExhibitionOpenFront = { 0b00101, 0b10100 };
    private static readonly int[] ExhibitionRisers = { (1 << 6) | (1 << 8), 1 << 7 };
    private readonly int[] exhibition = new int[OfferingSeatCount];
    private readonly Image[] exhibitionObjects = new Image[OfferingSeatCount];
    private readonly Image[] exhibitionEdges = new Image[OfferingSeatCount];
    private readonly Text[] exhibitionSeatLabels = new Text[OfferingSeatCount];
    private readonly Text[] exhibitionSupplyLabels = new Text[ExhibitionNames.Length];
    private Sprite[] exhibitionSprites;
    private int exhibitionHeld = -1;
    private bool exhibitionSide;
    private Image exhibitionGhost;
    private Text exhibitionHeading;
    private Text exhibitionStatus;
    private Text exhibitionLight;
    private Image exhibitionSunbeam;
    private Button exhibitionTurn;

    private const int WeavingRows = 6;
    private const int WeavingColumns = 6;
    private const int UndyedPick = 3;
    private static readonly int[] HeddleGroups = { 0b101010, 0b010101, 0b100001 };
    private static readonly string[] HeddleNames = { "奇数经线", "偶数经线", "两侧经线" };
    private static readonly int[][] WeftColors = { new[] { 1, 3, 2, 1, 3, 2 }, new[] { 2, 3, 1, 2, 3, 1 } };
    private static readonly int[][] WeftPatterns = { new[] { 1, 2, 5, 1, 2, 6 }, new[] { 5, 1, 6, 2, 5, 1 } };
    private static readonly string[] WeftNames = { "赭梭", "青梭" };
    private static readonly Color32[] WeftInks = { new Color32(167, 83, 73, 255), new Color32(66, 104, 133, 255) };
    private readonly Image[,] weavingCells = new Image[WeavingRows, WeavingColumns];
    private readonly int[] wovenColors = new int[WeavingRows];
    private readonly int[] wovenPatterns = new int[WeavingRows];
    private readonly Text[] weftHints = new Text[2];
    private readonly Image[,] weftHintPegs = new Image[2, WeavingColumns];
    private readonly Text[] shuttleCards = new Text[2];
    private readonly Image[] shuttlePictures = new Image[2];
    private readonly Text[] heddleCards = new Text[3];
    private readonly Image[] heddlePictures = new Image[3];
    private readonly Image[] liveHeddlePegs = new Image[WeavingColumns];
    private int wovenCount;
    private int shuttlesRight;
    private int heddles;
    private int emptyRuns;
    private int weavingCheckpointSides;
    private int weavingCheckpointHeddles;
    private int weavingCheckpointRuns;
    private Text weavingStatus;
    private Image movingShuttle;

    private void ResetAutumnChallenge()
    {
        switch (observation)
        {
            case WorkshopObservation.QiufenOffering:
                ResetExhibition();
                exhibitionSide = false;
                break;
            case WorkshopObservation.HanluLoom:
                wovenCount = emptyRuns = heddles = 0;
                shuttlesRight = ChallengeVariant == 0 ? 2 : 1;
                SaveWeavingCheckpoint();
                break;
        }
    }

    private void ResetExhibition()
    {
        for (int i = 0; i < exhibition.Length; i++) exhibition[i] = -1;
        exhibitionHeld = -1;
    }

    private void DrawOfferingWorkshop()
    {
        Texture("Offering Table and Tools", "WorkshopFinish/AutumnOfferingTable", ArtBounds);
        var props = AutumnItemIcons.Load();
        exhibitionSprites = new[] { WorkshopObservationArt.Current?.OfferingWine, props?.banana, props?.cake,
            Resources.Load<Sprite>("WorkshopFinish/RawSilk"), WorkshopObservationArt.Current?.Leaf };
        var trayArt = GameArtSprite("OfferingTray");
        var riserArt = GameArtSprite("WoodenRiser");

        var header = WorkshopUi.PixelCard(art, "Exhibition View", new Rect(53, 24, 1054, 53), WorkshopUi.Ink, WorkshopUi.Bronze);
        exhibitionLight = WorkshopUi.Label(header, "Exhibition Light", new Rect(12, 0, 1030, 53), "", 27,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);

        for (int seat = 0; seat < exhibition.Length; seat++)
        {
            int i = seat;
            Rect bounds = ExhibitionSeatBounds(seat);
            bool open = CanUseExhibitionSeat(seat);
            var card = IllustratedGameCard(art, "Offering Display Seat " + seat, bounds, trayArt,
                open ? LightSurface : InsetPaper, open ? Color.white : new Color32(161, 143, 118, 193));
            exhibitionEdges[seat] = WorkshopUi.Fill(card, "Seat Status", new Rect(8, 5, bounds.width - 16, 6), WorkshopUi.Line);
            bool lifted = IsRaisedExhibitionSeat(seat);
            if (lifted) GameArtSurface(card, "Wooden Riser", new Rect(37, 60, 130, 47), riserArt, new Color32(150, 104, 64, 255));
            exhibitionObjects[seat] = WorkshopUi.Picture(card, "Displayed Offering", new Rect(29, lifted ? 3 : 12, 140, lifted ? 79 : 93), null);
            if (trayArt != null)
                WorkshopUi.Fill(card, "Caption Paper", new Rect(16, 102, 173, 26), new Color32(251, 234, 196, 218));
            exhibitionSeatLabels[seat] = WorkshopUi.Label(card, "Seat Caption", new Rect(8, 96, 189, 37), "", 21,
                WorkshopUi.Ink, TextAnchor.MiddleCenter);
            if (GameComplete || !open) continue;

            var target = WorkshopUi.Button(card, "Place", new Rect(1, 1, bounds.width - 2, bounds.height - 2), "",
                () => ClickExhibitionSeat(i), Color.clear);
            RegisterButton(target);
            BindOfferingDrag(target.gameObject, () => exhibition[i]);
        }

        exhibitionSunbeam = WorkshopUi.Picture(art, "Offering Sunbeam", new Rect(0, 101, 1, 112),
            GameArtSprite("OfferingSunbeam"), stretch: true);
        exhibitionSunbeam.raycastTarget = false;

        if (GameComplete)
        {
            Copy("三牌可见，丝束见光", "供台沿用四时织坊的致谢故事。这里陈列桑叶和丝束，记下采桑、养蚕和缫丝经过的几双手。");
            Act("Read Autumn Heritage", "看苏州织造资料", OpenAutumnHeritage, true);
            RefreshExhibition();
            return;
        }

        for (int item = 0; item < ExhibitionNames.Length; item++)
        {
            int i = item;
            var bounds = new Rect(53 + item * 212, 478, 205, 142);
            var tray = IllustratedGameCard(art, "Offering Supply " + item, bounds, trayArt, LightSurface);
            WorkshopUi.Picture(tray, "Supply Item", new Rect(45, 11, 114, 77), exhibitionSprites[item]);
            if (trayArt != null)
                WorkshopUi.Fill(tray, "Caption Paper", new Rect(16, 84, 173, 26), new Color32(251, 234, 196, 218));
            exhibitionSupplyLabels[item] = WorkshopUi.Label(tray, "Supply Caption", new Rect(8, 78, 189, 37), "", 21,
                WorkshopUi.Ink, TextAnchor.MiddleCenter);
            var take = WorkshopUi.Button(tray, "Take", new Rect(1, 1, 203, 140), "", () => PickExhibitionItem(i), Color.clear);
            RegisterButton(take);
            BindOfferingDrag(take.gameObject, () => i);
        }

        exhibitionGhost = WorkshopUi.Picture(art, "Offering In Hand", new Rect(0, 0, 150, 104), null);
        exhibitionGhost.color = new Color(1f, 1f, 1f, 0.9f);
        Copy("摆供品，留出视线", "拖供品到桌上。前排高物会遮挡后方名牌。切到侧窗查看丝束，后排高物会挡住照进来的光。");
        exhibitionHeading = infoPanel.Find("Instruction Heading").GetComponent<Text>();
        Act("Submit Offering Display", "请来客看供台", SubmitExhibition, true);
        Act("Offering View Toggle", "切到侧窗", () => { exhibitionSide = !exhibitionSide; RefreshExhibition(); });
        exhibitionTurn = infoPanel.Find("Offering View Toggle").GetComponentInChildren<Button>();
        Act("Reset Offering Display", "收回供品", () => { ResetExhibition(); RefreshExhibition(); });
        exhibitionStatus = WorkshopUi.Label(infoPanel, "Offering Order Progress", new Rect(26, 563, 448, 88), "", 24,
            WorkshopUi.Green, TextAnchor.UpperLeft);
        RefreshExhibition();
    }

    private static Rect ExhibitionSeatBounds(int seat) =>
        new Rect(53 + seat % OfferingColumns * 212, seat < OfferingColumns ? 281 : 101, 205, 158);

    private bool CanUseExhibitionSeat(int seat) => seat >= OfferingColumns ||
        (ExhibitionOpenFront[ChallengeVariant] & (1 << seat)) != 0;

    private bool IsRaisedExhibitionSeat(int seat) => (ExhibitionRisers[ChallengeVariant] & (1 << seat)) != 0;

    private bool OfferingPlaqueVisible(int seat)
    {
        if (seat < 0) return false;
        if (seat < OfferingColumns) return true;
        int ahead = exhibition[seat - OfferingColumns];
        return ahead < 0 || ExhibitionHeights[ahead] < (IsRaisedExhibitionSeat(seat) ? 3 : 2);
    }

    private int WindowBlocker(int silkSeat)
    {
        if (silkSeat < OfferingColumns) return -1;
        int from = ChallengeVariant == 0 ? OfferingColumns : OfferingSeatCount - 1;
        int direction = ChallengeVariant == 0 ? 1 : -1;
        for (int seat = from; seat != silkSeat; seat += direction)
        {
            int prop = exhibition[seat];
            if (prop >= 0 && ExhibitionHeights[prop] + (IsRaisedExhibitionSeat(seat) ? 1 : 0) >= 2) return seat;
        }
        return -1;
    }

    private int VisibleOfferingPlaques()
    {
        int seen = 0;
        for (int item = 0; item < SilkOffering; item++)
            if (OfferingPlaqueVisible(Array.IndexOf(exhibition, item))) seen++;
        return seen;
    }

    private void RefreshExhibition(string message = null)
    {
        int silk = Array.IndexOf(exhibition, SilkOffering);
        int blocker = WindowBlocker(silk);
        bool lit = silk >= OfferingColumns && blocker < 0;
        int count = VisibleOfferingPlaques();
        string window = ChallengeVariant == 0 ? "左窗向右照" : "右窗向左照";
        exhibitionLight.text = exhibitionSide ? $"侧窗 · {window} · {(lit ? "丝束已见光" : "看丝束与挡光物")}" : "正面 · 后排低位容易被前排的高供品遮挡";
        ShowExhibitionSunbeam(silk, blocker);

        for (int seat = 0; seat < exhibition.Length; seat++)
        {
            int item = exhibition[seat];
            bool raised = IsRaisedExhibitionSeat(seat);
            bool visible = item < 0 || item >= SilkOffering || OfferingPlaqueVisible(seat);
            var picture = exhibitionObjects[seat];
            picture.sprite = item >= 0 ? exhibitionSprites[item] : null;
            picture.enabled = picture.sprite != null;
            bool warning = exhibitionSide ? seat == blocker || (seat == silk && !lit) : !visible;
            bool featured = seat == silk && exhibitionSide && lit;
            exhibitionEdges[seat].color = warning ? ErrorInk : featured ? new Color32(212, 166, 65, 255) : item >= 0 && item == exhibitionHeld ? FocusInk : WorkshopUi.Line;
            picture.color = warning ? new Color32(155, 130, 115, 210) : Color.white;
            string location = seat < OfferingColumns ? "前排" : raised ? "后排高台" : "后排";
            string caption = CanUseExhibitionSeat(seat) ? location : "留白";
            if (item >= 0)
            {
                string cue = featured ? "见光" : location;
                if (warning) cue = !exhibitionSide ? "牌被挡" : seat == blocker ? "挡住窗光" : "未照到";
                caption = ExhibitionNames[item] + " · " + cue;
            }
            exhibitionSeatLabels[seat].text = caption;
        }

        if (GameComplete) return;
        for (int item = 0; item < ExhibitionNames.Length; item++)
        {
            bool placed = Array.IndexOf(exhibition, item) >= 0;
            exhibitionSupplyLabels[item].text = ExhibitionNames[item] + (item == exhibitionHeld ? " · 拿起" : placed ? " · 在桌上" : " · 待摆");
        }
        exhibitionHeading.text = exhibitionHeld >= 0 ? "在摆" + ExhibitionNames[exhibitionHeld] : "摆供品，留出视线";
        exhibitionTurn.transform.Find("Label").GetComponent<Text>().text = exhibitionSide ? "切回正面" : "切到侧窗";
        exhibitionStatus.text = message ?? $"看得见的供品名牌 {count} / 3\n丝束{(lit ? "已见光" : "需要后排来光")}";
    }

    private void ShowExhibitionSunbeam(int silk, int blocker)
    {
        exhibitionSunbeam.enabled = exhibitionSide && exhibitionSunbeam.sprite != null;
        if (!exhibitionSunbeam.enabled) return;

        int direction = ChallengeVariant == 0 ? 1 : -1;
        float source = direction > 0 ? 52f : 1107f;
        int endSeat = blocker >= 0 ? blocker : silk;
        float end = endSeat >= OfferingColumns
            ? ExhibitionSeatBounds(endSeat).center.x + direction * (blocker >= 0 ? -27f : 43f)
            : direction > 0 ? 1107f : 52f;

        var beam = exhibitionSunbeam.rectTransform;
        beam.pivot = new Vector2(.5f, 1f);
        beam.anchoredPosition = new Vector2((source + end) * .5f, -101f);
        beam.sizeDelta = new Vector2(Mathf.Abs(end - source), 112f);
        beam.localScale = new Vector3(direction, 1f, 1f);
        exhibitionSunbeam.color = new Color(1f, 1f, 1f, blocker >= 0 ? .75f : .92f);
    }

    private void PickExhibitionItem(int item)
    {
        exhibitionHeld = exhibitionHeld == item ? -1 : item;
        WorkshopAudioFeedback.Click();
        RefreshExhibition();
    }

    private void ClickExhibitionSeat(int seat)
    {
        if (exhibitionHeld < 0)
        {
            if (exhibition[seat] >= 0) PickExhibitionItem(exhibition[seat]);
            return;
        }
        PutOffering(exhibitionHeld, seat);
    }

    private void PutOffering(int item, int seat)
    {
        if (item < 0 || seat < 0 || !CanUseExhibitionSeat(seat)) return;
        int old = Array.IndexOf(exhibition, item);
        if (old >= 0) exhibition[old] = exhibition[seat];
        exhibition[seat] = item;
        exhibitionHeld = -1;
        WorkshopAudioFeedback.Place();
        RefreshExhibition();
    }

    private void BindOfferingDrag(GameObject target, Func<int> itemAtSource)
    {
        var trigger = target.AddComponent<EventTrigger>();
        int dragged = -1;
        void OnDrag(EventTriggerType kind, Action<PointerEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = kind };
            entry.callback.AddListener(data =>
            {
                if (data is PointerEventData pointer && pointer.button == PointerEventData.InputButton.Left) callback(pointer);
            });
            trigger.triggers.Add(entry);
        }
        OnDrag(EventTriggerType.BeginDrag, pointer =>
        {
            dragged = itemAtSource();
            if (dragged < 0) return;
            exhibitionGhost.sprite = exhibitionSprites[dragged];
            exhibitionGhost.enabled = exhibitionGhost.sprite != null;
            MoveOfferingGhost(pointer);
        });
        OnDrag(EventTriggerType.Drag, pointer => { if (dragged >= 0) MoveOfferingGhost(pointer); });
        OnDrag(EventTriggerType.EndDrag, pointer =>
        {
            exhibitionGhost.enabled = false;
            Vector2 at = OfferingPointer(pointer);
            for (int seat = 0; seat < OfferingSeatCount; seat++)
                if (ExhibitionSeatBounds(seat).Contains(at)) { PutOffering(dragged, seat); break; }
            dragged = -1;
        });
    }

    private void MoveOfferingGhost(PointerEventData pointer)
    {
        Vector2 at = OfferingPointer(pointer);
        exhibitionGhost.rectTransform.anchoredPosition = new Vector2(at.x - 75, -at.y + 52);
    }

    private Vector2 OfferingPointer(PointerEventData pointer)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(art, pointer.position, pointer.pressEventCamera, out var at);
        return new Vector2(at.x, -at.y);
    }

    private void SubmitExhibition()
    {
        if (Array.IndexOf(exhibition, 4) < 0) { RefreshExhibition("先把桑叶样本摆到桌面上"); return; }
        if (VisibleOfferingPlaques() < 3) { exhibitionSide = false; RefreshExhibition("正面还有名牌被遮住或没摆上"); return; }
        int silk = Array.IndexOf(exhibition, SilkOffering);
        if (silk < OfferingColumns || WindowBlocker(silk) >= 0)
        {
            exhibitionSide = true;
            RefreshExhibition("让后排丝束前的来光路保持畅通");
            return;
        }

        int standsUsed = 0;
        string pattern = "";
        for (int i = 0; i < exhibition.Length; i++)
        {
            if (i == OfferingColumns) pattern += "/";
            var item = exhibition[i];
            if (item >= 0 && IsRaisedExhibitionSeat(i)) standsUsed++;
            pattern += item >= 0 ? ExhibitionInks[item] : "0";
        }
        exhibitionSide = true;
        FinishChallenge(100 + (2 - standsUsed) * 5, $"供台陈设 · 三牌可见 · 高台 {standsUsed}", pattern);
    }

    private void DrawShuttleWorkshop()
    {
        if (showSongJin) { DrawLoom(); return; }
        Layer("Autumn_LoomCloseup");
        Layer("Autumn_WarpAligned");
        var weftArt = GameArtSprite("WeftTile");
        var shuttleArt = GameArtSprite("Shuttle") ?? WorkshopObservationArt.Current?.Shuttle;
        var heddleArt = GameArtSprite("LoomHeddle");
        var cloth = IllustratedGameCard(art, "Weaving Ribbon", new Rect(64, 196, 1032, 240),
            GameArtSprite("WoodDesk"), LightSurface);
        for (int row = 0; row < WeavingRows; row++)
            for (int col = 0; col < WeavingColumns; col++)
                weavingCells[row, col] = GameArtSurface(cloth, $"Weft {row},{col}",
                    new Rect(23 + 165 * col, 19 + 34 * row, 161, 31), weftArt, InsetPaper);

        if (GameComplete)
        {
            Copy("织完一条双色纹带", "梭子横穿带入纬线，抬起不同的经线就显出不同纹样。织成的纹带可以在储物屋收藏中回看。");
            Act("Look At Song Brocade", "看宋锦显花示意", ToggleSongJin, true);
            PaintWovenRows();
            return;
        }

        for (int peek = 0; peek < 2; peek++)
        {
            var preview = WorkshopUi.PixelCard(art, "Pick Preview " + peek, new Rect(64 + 520 * peek, 45, 512, 132), LightSurface, WorkshopUi.Bronze);
            weftHints[peek] = WorkshopUi.Label(preview, "Scheduled Pick", new Rect(9, 5, 494, 48), "", 26,
                WorkshopUi.Ink, TextAnchor.MiddleCenter);
            for (int col = 0; col < WeavingColumns; col++)
                weftHintPegs[peek, col] = GameArtSurface(preview, "Pattern Peg " + col,
                    new Rect(33 + 75 * col, 63, 68, 48), weftArt, InsetPaper);
        }

        for (int i = 0; i < HeddleGroups.Length; i++)
        {
            int group = i;
            heddleCards[i] = AutumnControl("Heddle Group " + i, new Rect(64 + i * 237, 458, 222, 57),
                () => ToggleHeddle(group), heddleArt);
            heddlePictures[i] = heddleCards[i].transform.parent.Find("Tool")?.GetComponent<Image>();
        }
        for (int col = 0; col < WeavingColumns; col++)
            liveHeddlePegs[col] = GameArtSurface(art, "Live Heddle " + col,
                new Rect(790 + col * 51, 466, 44, 44), weftArt, InsetPaper);
        for (int i = 0; i < WeftNames.Length; i++)
        {
            int shuttle = i;
            shuttleCards[i] = AutumnControl("Send Shuttle " + i, new Rect(64 + i * 520, 541, 512, 72),
                () => WeavePick(shuttle), shuttleArt);
            shuttlePictures[i] = shuttleCards[i].transform.parent.Find("Tool")?.GetComponent<Image>();
        }

        Copy("两侧换梭，织六梭", "把抬起的经线排成当前示样，再从箭头起点送所需颜色的梭。梭到达另一侧后，留意下一梭从哪边出发。");
        Act("Move Red Shuttle", "空送赭梭到另一侧", () => MoveEmptyShuttle(0));
        Act("Move Blue Shuttle", "空送青梭到另一侧", () => MoveEmptyShuttle(1));
        Act("Reset Weaving Segment", "退回这一段开头", RestoreWeavingCheckpoint);
        weavingStatus = WorkshopUi.Label(infoPanel, "Weaving Status", new Rect(27, 565, 444, 90), "", 24,
            WorkshopUi.Green, TextAnchor.UpperLeft);
        UpdateWeaving();
    }

    private Text AutumnControl(string name, Rect bounds, Action input, Sprite tool = null)
    {
        var card = WorkshopUi.PixelCard(art, name, bounds, LightSurface, WorkshopUi.Bronze);
        var button = WorkshopUi.Button(card, "Action", new Rect(1, 1, bounds.width - 2, bounds.height - 2), "", input, Color.clear);
        RegisterButton(button);
        bool compact = bounds.width < 250f;
        float toolWidth = compact ? 56f : 116f;
        if (tool != null)
            WorkshopUi.Picture(card, "Tool", new Rect(13, 9, toolWidth, bounds.height - 18), tool);
        float captionLeft = tool != null ? toolWidth + 15f : 6f;
        return WorkshopUi.Label(card, "Caption", new Rect(captionLeft, 1, bounds.width - captionLeft - 6, bounds.height - 2), "", compact ? 21 : 27,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
    }

    private static int WeavingThreadMask(int groups)
    {
        int threads = 0;
        for (int i = 0; i < HeddleGroups.Length; i++)
            if ((groups & (1 << i)) != 0) threads ^= HeddleGroups[i];
        return threads;
    }

    private static bool RaisedWeavingThread(int mask, int col) => (mask & (1 << (WeavingColumns - 1 - col))) != 0;
    private int ShuttleSide(int shuttle) => (shuttlesRight >> shuttle) & 1;
    private int WeavingMoveBudget => ChallengeVariant == 0 ? 2 : 1;
    private static string WeavingDirection(int pick) => pick % 2 == 0 ? "左 → 右" : "右 → 左";

    private void UpdateWeaving(string message = null)
    {
        PaintWovenRows();
        int threads = WeavingThreadMask(heddles);
        for (int col = 0; col < WeavingColumns; col++)
            liveHeddlePegs[col].color = RaisedWeavingThread(threads, col) ? WorkshopUi.Bronze : InsetPaper;
        for (int i = 0; i < HeddleGroups.Length; i++)
        {
            bool raised = ((heddles >> i) & 1) == 1;
            heddleCards[i].text = HeddleNames[i] + (raised ? " · 起" : " · 落");
            if (heddlePictures[i] == null) continue;
            heddlePictures[i].color = raised ? Color.white : new Color32(159, 139, 117, 255);
            heddlePictures[i].rectTransform.anchoredPosition = new Vector2(13f, raised ? -5f : -11f);
        }
        for (int i = 0; i < WeftNames.Length; i++)
        {
            shuttleCards[i].text = $"{WeftNames[i]} · 在{(ShuttleSide(i) == 0 ? "左" : "右")}侧送梭";
            shuttleCards[i].color = WeftInks[i];
            if (shuttlePictures[i] != null) shuttlePictures[i].color = Color.Lerp(Color.white, WeftInks[i], .68f);
        }

        for (int peek = 0; peek < weftHints.Length; peek++)
        {
            int row = wovenCount + peek;
            bool valid = row < WeavingRows;
            int shade = valid ? WeftColors[ChallengeVariant][row] : UndyedPick;
            string name = shade == UndyedPick ? "任一梭" : WeftNames[shade == 1 ? 0 : 1];
            weftHints[peek].text = valid ? $"第 {row + 1} 梭 · {WeavingDirection(row)} · {name}" : "已经是最后一梭";
            int pattern = valid ? WeavingThreadMask(WeftPatterns[ChallengeVariant][row]) : 0;
            for (int col = 0; col < WeavingColumns; col++)
                weftHintPegs[peek, col].color = RaisedWeavingThread(pattern, col) ? WorkshopUi.Bronze : InsetPaper;
        }
        weavingStatus.text = message ?? $"已织 {wovenCount} / {WeavingRows} 梭\n剩余空送 {WeavingMoveBudget - emptyRuns} 次";
    }

    private void PaintWovenRows()
    {
        for (int row = 0; row < WeavingRows; row++)
            for (int col = 0; col < WeavingColumns; col++)
                weavingCells[row, col].color = row >= wovenCount ? InsetPaper : RaisedWeavingThread(wovenPatterns[row], col)
                    ? (Color)WeftInks[wovenColors[row]] : LightSurface;
    }

    private void ToggleHeddle(int group)
    {
        heddles ^= 1 << group;
        WorkshopAudioFeedback.Click();
        UpdateWeaving();
    }

    private void MoveEmptyShuttle(int shuttle)
    {
        if (emptyRuns >= WeavingMoveBudget) { UpdateWeaving("空送已用完，可重排这一段"); return; }
        int fromSide = ShuttleSide(shuttle);
        emptyRuns++;
        shuttlesRight ^= 1 << shuttle;
        WorkshopAudioFeedback.Place();
        UpdateWeaving();
        ShowShuttlePass(shuttle, fromSide, wovenCount);
    }

    private void WeavePick(int shuttle)
    {
        if (ShuttleSide(shuttle) != wovenCount % 2) { UpdateWeaving("这把梭在另一侧，先留着或空送"); return; }
        if ((WeftColors[ChallengeVariant][wovenCount] & (1 << shuttle)) == 0) { UpdateWeaving("这一梭需要另一种颜色"); return; }
        int threadMask = WeavingThreadMask(heddles);
        if (threadMask != WeavingThreadMask(WeftPatterns[ChallengeVariant][wovenCount]))
        {
            UpdateWeaving("梭口的经线还没排成定样");
            return;
        }

        int fromSide = ShuttleSide(shuttle);
        int nextSide = shuttlesRight ^ (1 << shuttle);
        if (wovenCount + 1 == WeavingRows / 2 && !CanFinishRibbon(wovenCount + 1, nextSide, WeavingMoveBudget - emptyRuns))
        {
            UpdateWeaving("空送余量不足以织后半段，试试另一种摆法");
            return;
        }

        wovenPatterns[wovenCount] = threadMask;
        wovenColors[wovenCount] = shuttle;
        wovenCount++;
        shuttlesRight = nextSide;
        if (wovenCount == WeavingRows)
        {
            string ribbon = "";
            for (int row = 0; row < WeavingRows; row++)
            {
                if (row != 0) ribbon += "/";
                for (int col = 0; col < WeavingColumns; col++)
                    ribbon += RaisedWeavingThread(wovenPatterns[row], col) ? wovenColors[row] == 0 ? "3" : "5" : "1";
            }
            FinishChallenge(100 + (WeavingMoveBudget - emptyRuns) * 10, $"双色织带 · 六梭完成 · 空送 {emptyRuns} 次", ribbon);
            ShowShuttlePass(shuttle, fromSide, wovenCount - 1);
            return;
        }

        if (wovenCount == WeavingRows / 2) SaveWeavingCheckpoint();
        WorkshopAudioFeedback.Progress();
        UpdateWeaving();
        ShowShuttlePass(shuttle, fromSide, wovenCount - 1);
    }

    private void ShowShuttlePass(int shuttle, int fromSide, int row)
    {
        var sprite = GameArtSprite("Shuttle") ?? WorkshopObservationArt.Current?.Shuttle;
        if (sprite == null) return;

        if (movingShuttle != null) movingShuttle.gameObject.SetActive(false);
        movingShuttle = WorkshopUi.Picture(art, "Shuttle In Motion", new Rect(0, 0, 145, 45), sprite,
            Color.Lerp(Color.white, WeftInks[shuttle], .62f));
        StartCoroutine(AnimateShuttlePass(movingShuttle, fromSide, row));
    }

    private IEnumerator AnimateShuttlePass(Image shuttle, int fromSide, int row)
    {
        const float duration = .28f;
        float start = fromSide == 0 ? 55f : 966f;
        float finish = fromSide == 0 ? 966f : 55f;
        float y = -(208f + row * 34f);
        for (float elapsed = 0f; shuttle != null && shuttle.gameObject.activeInHierarchy && elapsed < duration;
             elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            shuttle.rectTransform.anchoredPosition = new Vector2(Mathf.SmoothStep(start, finish, progress), y);
            yield return null;
        }
        if (shuttle != null) Destroy(shuttle.gameObject);
    }

    private bool CanFinishRibbon(int row, int sides, int emptyMoves)
    {
        if (row >= WeavingRows) return true;
        int acceptedColors = WeftColors[ChallengeVariant][row];
        for (int shuttle = 0; shuttle < WeftNames.Length; shuttle++)
        {
            int bit = 1 << shuttle;
            if ((acceptedColors & bit) == 0) continue;
            bool needsEmptyMove = ((sides >> shuttle) & 1) != row % 2;
            if (needsEmptyMove && emptyMoves == 0) continue;
            if (CanFinishRibbon(row + 1, needsEmptyMove ? sides : sides ^ bit, emptyMoves - (needsEmptyMove ? 1 : 0)))
                return true;
        }
        return false;
    }

    private void SaveWeavingCheckpoint()
    {
        weavingCheckpointSides = shuttlesRight;
        weavingCheckpointHeddles = heddles;
        weavingCheckpointRuns = emptyRuns;
    }

    private void RestoreWeavingCheckpoint()
    {
        if (movingShuttle != null) movingShuttle.gameObject.SetActive(false);
        wovenCount = wovenCount < WeavingRows / 2 ? 0 : WeavingRows / 2;
        shuttlesRight = weavingCheckpointSides;
        heddles = weavingCheckpointHeddles;
        emptyRuns = weavingCheckpointRuns;
        UpdateWeaving("已退回本段起点");
    }

    private static Sprite GameArtSprite(string name) => Resources.Load<Sprite>("WorkshopGameArt/" + name);

    private static Image GameArtSurface(Transform parent, string name, Rect bounds, Sprite sprite, Color tint)
    {
        return sprite != null
            ? WorkshopUi.Picture(parent, name, bounds, sprite, tint, stretch: true)
            : WorkshopUi.Fill(parent, name, bounds, tint);
    }

    private static RectTransform IllustratedGameCard(Transform parent, string name, Rect bounds, Sprite art,
        Color fallback, Color? tint = null)
    {
        if (art == null) return WorkshopUi.PixelCard(parent, name, bounds, fallback, WorkshopUi.Bronze);

        var card = WorkshopUi.Rect(parent, name, bounds);
        WorkshopUi.Picture(card, "Illustrated Surface", new Rect(0, 0, bounds.width, bounds.height), art, tint, stretch: true);
        return card;
    }
}
