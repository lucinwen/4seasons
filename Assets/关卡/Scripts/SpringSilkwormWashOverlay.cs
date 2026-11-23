using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class SpringSilkwormWashOverlay : MonoBehaviour
{
    private enum WashPhase { Load, Rinse, Skim, Complete }

    private static readonly Rect Basin = new Rect(302, 182, 552, 193);
    private static readonly Vector2 TrayRest = new Vector2(137, 284);
    private static readonly Vector2[] RinsePositions =
    {
        new Vector2(369, 286), new Vector2(578, 214), new Vector2(787, 286), new Vector2(578, 346)
    };
    private static readonly Vector2[] DebrisPositions =
    {
        new Vector2(429, 245), new Vector2(521, 353), new Vector2(687, 245), new Vector2(731, 354)
    };
    private static readonly Color32 DeepWater = new Color32(34, 89, 95, 255);
    private static readonly Color32 Lit = new Color32(251, 223, 131, 255);
    private const string ArtPath = "WorkshopGameArt/";
    private const int RinseTarget = 8;

    private static SpringSilkwormWashOverlay current;
    private RectTransform frame;
    private RectTransform work;
    private RectTransform tray;
    private Image trayImage;
    private Text title;
    private Text hint;
    private Text goal;
    private Button action;
    private Image progress;
    private readonly Image[] rings = new Image[RinsePositions.Length];
    private readonly Text[] ringNumbers = new Text[RinsePositions.Length];
    private readonly GameObject[] scraps = new GameObject[DebrisPositions.Length];
    private Sprite cleanEggs;
    private Action<bool> onClose;
    private GameObject priorSelection;
    private float priorTimeScale;
    private WashPhase phase;
    private int rinsed;
    private int skimmed;
    private bool dragging;
    private Vector2 lastPoint;

    internal static bool IsOpen => current != null;

    internal static bool Show(Sprite bucket, Sprite eggs, Sprite cleaned, Action<bool> close)
    {
        if (IsOpen) return false;
        RectTransform canvas = WorkshopUi.NewCanvas("SpringSilkwormWashOverlay", 23000);
        WorkshopUi.Block(canvas, WorkshopUi.Veil);
        current = canvas.parent.gameObject.AddComponent<SpringSilkwormWashOverlay>();
        current.frame = canvas;
        current.onClose = close;
        current.priorTimeScale = Time.timeScale;
        current.priorSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        Time.timeScale = 0f;
        current.Build(bucket, eggs, cleaned);
        return true;
    }

    private void Build(Sprite bucket, Sprite eggs, Sprite cleaned)
    {
        cleanEggs = cleaned;
        var tubArt = Resources.Load<Sprite>(ArtPath + "BathTub");
        var sieveArt = Resources.Load<Sprite>(ArtPath + "BathSieve");
        var debrisArt = Resources.Load<Sprite>(ArtPath + "BathDebris");
        var rippleArt = Resources.Load<Sprite>(ArtPath + "WaterRipple");
        var page = WorkshopUi.WorkbenchCard(frame, "Bath Panel", new Rect(88, 77, 1744, 926),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        WorkshopUi.Label(page, "Bath Season", new Rect(29, 19, 440, 40), "春 · 下一批蚕种", 28, WorkshopUi.Green);
        WorkshopUi.Label(page, "Bath Title", new Rect(29, 60, 750, 77), "浴蚕桶 · 漂洗蚕种", 54);
        WorkshopUi.Fill(page, "Heading Rule", new Rect(28, 145, 1688, 2), WorkshopUi.Line);
        work = WorkshopUi.PixelCard(page, "Bath Worktable", new Rect(28, 163, 1172, 665),
            new Color32(117, 78, 51, 255), WorkshopUi.Bronze);
        WorkshopUi.Picture(work, "Spring Worktable", new Rect(5, 5, 1162, 655),
            Resources.Load<Sprite>("WorkshopObservation/Spring_LeafDesk"), stretch: true);
        WorkshopUi.Fill(work, "Table Shade", new Rect(5, 5, 1162, 655), new Color32(43, 34, 26, 58));
        WorkshopUi.Picture(work, "Bath Tub", new Rect(220, 96, 720, 505), tubArt ?? bucket, stretch: tubArt != null);

        for (int i = 0; i < RinsePositions.Length; i++)
        {
            var point = RinsePositions[i];
            var ring = DrawRipple(work, "Rinse Guide " + i, Around(point, 83, 70), rippleArt);
            ringNumbers[i] = WorkshopUi.Label(ring.transform, "Guide Number", new Rect(0, 0, 83, 68), (i + 1).ToString(),
                27, WorkshopUi.Cream, TextAnchor.MiddleCenter);
            rings[i] = ring;
        }

        var input = WorkshopUi.Fill(work, "Bath Gesture Surface", new Rect(5, 5, 1162, 655), Color.clear);
        input.raycastTarget = true;
        var trigger = input.gameObject.AddComponent<EventTrigger>();
        Register(trigger, EventTriggerType.BeginDrag, BeginDrag);
        Register(trigger, EventTriggerType.Drag, Drag);
        Register(trigger, EventTriggerType.EndDrag, EndDrag);

        tray = WorkshopUi.Rect(work, "Silkworm Egg Sieve", Around(TrayRest, 174, 125));
        WorkshopUi.Picture(tray, "Bamboo Sieve", new Rect(3, 12, 168, 104),
            sieveArt ?? WorkshopObservationArt.Current?.SilkwormTray, stretch: sieveArt != null);
        trayImage = WorkshopUi.Picture(tray, "Eggs on Sieve", new Rect(29, 11, 115, 85), eggs);

        for (int i = 0; i < DebrisPositions.Length; i++)
        {
            int scrap = i;
            var spot = WorkshopUi.Button(work, "Floating Debris " + i, Around(DebrisPositions[i], 70, 70),
                "", () => Skim(scrap), Color.clear);
            DrawRipple(spot.transform, "Debris Ripple", new Rect(6, 7, 58, 55), rippleArt);
            var leaf = WorkshopUi.Picture(spot.transform, "Debris", new Rect(16, 20, 38, 29),
                debrisArt ?? WorkshopObservationArt.Current?.Leaf);
            leaf.rectTransform.localEulerAngles = new Vector3(0, 0, i % 2 == 0 ? 29 : -23);
            scraps[i] = spot.gameObject;
        }

        var counter = WorkshopUi.PixelCard(work, "Bath Progress", new Rect(266, 551, 640, 72),
            WorkshopUi.Ink, WorkshopUi.Bronze);
        goal = WorkshopUi.Label(counter, "Bath Stage Progress", new Rect(22, 5, 592, 42), "", 27,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        WorkshopUi.Fill(counter, "Progress Track", new Rect(28, 53, 584, 7), DeepWater);
        progress = WorkshopUi.Fill(counter, "Progress Fill", new Rect(28, 53, 0, 7), Lit);

        var instructions = WorkshopUi.WorkbenchCard(page, "Bath Instructions", new Rect(1218, 163, 498, 665),
            WorkshopUi.Cream, WorkshopUi.Line);
        title = WorkshopUi.Label(instructions, "Bath Phase", new Rect(27, 28, 444, 62), "", 36, WorkshopUi.Green);
        hint = WorkshopUi.Label(instructions, "Bath Hint", new Rect(27, 112, 440, 225), "", 31,
            WorkshopUi.Ink, TextAnchor.UpperLeft);
        hint.lineSpacing = 1.20f;
        WorkshopUi.Fill(instructions, "Instruction Rule", new Rect(27, 341, 444, 2), WorkshopUi.Line);
        action = WorkshopUi.PixelButton(instructions, "Bath Action", new Rect(27, 386, 444, 64),
            "放入蚕种筛", () => EnterRinse(), true, 28);
        WorkshopUi.PixelButton(page, "Leave Bath", new Rect(1473, 848, 243, 58), "返回 · Esc", Close, false, 27);
        foreach (var label in page.GetComponentsInChildren<Text>(true))
            label.font = WorkshopArt.ReadingFont;
        RefreshPhase();
    }

    private static void Register(EventTrigger trigger, EventTriggerType type, Action<PointerEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => callback((PointerEventData)data));
        trigger.triggers.Add(entry);
    }

    private void BeginDrag(PointerEventData pointer)
    {
        if (pointer.button != PointerEventData.InputButton.Left || phase > WashPhase.Rinse) return;
        var start = WorkPoint(pointer.pressPosition, pointer.pressEventCamera);
        dragging = phase == WashPhase.Load ? Around(TrayRest, 204, 148).Contains(start) : OverWater(start);
        lastPoint = start;
    }

    private void Drag(PointerEventData pointer)
    {
        if (!dragging) return;
        Vector2 point = WorkPoint(pointer.position, pointer.pressEventCamera);
        if (phase == WashPhase.Load)
        {
            MoveTray(point);
        }
        else if (phase == WashPhase.Rinse && OverWater(point))
        {
            MoveTray(point);
            Vector2 target = RinsePositions[rinsed % RinsePositions.Length];
            Vector2 span = point - lastPoint;
            float t = Mathf.Clamp01(Vector2.Dot(target - lastPoint, span) / Mathf.Max(span.sqrMagnitude, 1f));
            if (OverWater(lastPoint) && span.sqrMagnitude >= 4f &&
                Vector2.Distance(target, lastPoint + span * t) < 49f)
                AdvanceRinse();
        }
        lastPoint = point;
    }

    private void EndDrag(PointerEventData pointer)
    {
        if (!dragging) return;
        dragging = false;
        if (phase != WashPhase.Load) return;
        Vector2 point = WorkPoint(pointer.position, pointer.pressEventCamera);
        if (OverWater(point)) EnterRinse();
        else MoveTray(TrayRest);
    }

    private static bool OverWater(Vector2 point)
    {
        Vector2 distance = point - Basin.center;
        distance.x /= Basin.width * 0.5f;
        distance.y /= Basin.height * 0.5f;
        return distance.sqrMagnitude <= 1f;
    }

    private Vector2 WorkPoint(Vector2 screen, Camera eventCamera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(work, screen, eventCamera, out Vector2 local);
        return new Vector2(local.x, -local.y);
    }

    private void MoveTray(Vector2 center)
    {
        tray.anchoredPosition = new Vector2(center.x - 87, -center.y + 62.5f);
    }

    private void EnterRinse()
    {
        if (phase != WashPhase.Load) return;
        phase = WashPhase.Rinse;
        MoveTray(Basin.center);
        WorkshopAudioFeedback.Click();
        RefreshPhase();
    }

    private void AdvanceRinse()
    {
        rinsed++;
        WorkshopAudioFeedback.Click();
        if (rinsed == RinseTarget)
        {
            phase = WashPhase.Skim;
            dragging = false;
            MoveTray(TrayRest);
        }
        RefreshPhase();
    }

    private void Skim(int index)
    {
        if (phase != WashPhase.Skim || !scraps[index].activeSelf) return;
        scraps[index].SetActive(false);
        skimmed++;
        if (skimmed == scraps.Length)
        {
            phase = WashPhase.Complete;
            if (cleanEggs != null)
            {
                trayImage.sprite = cleanEggs;
                trayImage.enabled = true;
            }
            WorkshopAudioFeedback.Confirm();
        }
        else WorkshopAudioFeedback.Click();
        RefreshPhase();
    }

    private void RefreshPhase()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            rings[i].gameObject.SetActive(phase == WashPhase.Rinse);
            bool next = i == rinsed % rings.Length;
            rings[i].color = next ? Color.white : new Color32(123, 179, 169, 255);
            ringNumbers[i].color = next ? Lit : WorkshopUi.Cream;
            ringNumbers[i].fontStyle = next ? FontStyle.Bold : FontStyle.Normal;
        }
        action.transform.parent.gameObject.SetActive(phase == WashPhase.Load || phase == WashPhase.Complete);
        switch (phase)
        {
            case WashPhase.Load:
                title.text = "把筛浸入水中";
                hint.text = "把左边的竹筛拖进水里，也可以点击下方按钮。";
                goal.text = "入水 → 漂洗 → 捞杂质";
                break;
            case WashPhase.Rinse:
                title.text = "沿水面轻轻漂洗";
                hint.text = "在桶里按住鼠标，带着筛子依次经过发亮的 1、2、3、4 号水圈。绕行两圈。";
                goal.text = $"漂洗 {rinsed} / {RinseTarget}";
                break;
            case WashPhase.Skim:
                title.text = "捞去浮起的杂质";
                hint.text = "竹筛先搁回桌边。点击水面上泛着金圈的碎屑，一件一件捞出来。";
                goal.text = $"捞出杂质 {skimmed} / {scraps.Length}";
                if (skimmed == 0)
                    foreach (GameObject scrap in scraps) scrap.SetActive(true);
                break;
            case WashPhase.Complete:
                title.text = "蚕种漂洗完成";
                hint.text = "下一批蚕种已经整理好。带着它和春季其余材料返回织坊交付。";
                goal.text = "筛出杂质 · 蚕种已备好";
                action.onClick.RemoveAllListeners();
                action.onClick.AddListener(Close);
                action.GetComponentInChildren<Text>().text = "收好蚕种";
                break;
        }
        progress.rectTransform.sizeDelta = new Vector2(584f * (rinsed + skimmed) / (RinseTarget + scraps.Length), 7);
        if (phase == WashPhase.Load)
            foreach (GameObject scrap in scraps) scrap.SetActive(false);
    }

    private static Image DrawRipple(Transform parent, string name, Rect bounds, Sprite art)
    {
        if (art != null) return WorkshopUi.Picture(parent, name, bounds, art, Lit, stretch: true);
        var marker = WorkshopUi.Fill(parent, name, bounds, Lit);
        WorkshopUi.ApplyPixelShape(marker);
        return marker;
    }

    private static Rect Around(Vector2 center, float width, float height) =>
        new Rect(center.x - width / 2, center.y - height / 2, width, height);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Close()
    {
        if (current != this) return;
        Restore();
        WorkshopUi.CloseCanvas(frame);
        onClose?.Invoke(phase == WashPhase.Complete);
    }

    private void Restore()
    {
        current = null;
        Time.timeScale = priorTimeScale;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(priorSelection != null && priorSelection.activeInHierarchy ? priorSelection : null);
    }

    private void OnDestroy()
    {
        if (current == this)
        {
            Restore();
            SilkwormWashingController.washingStarted = false;
        }
    }
}
