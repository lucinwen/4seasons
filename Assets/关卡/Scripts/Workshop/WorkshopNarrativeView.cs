using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI-only narrative layer for the workshop notices, chapter transitions and finale.
/// </summary>
public sealed class WorkshopNarrativeView : MonoBehaviour
{
    private static readonly Color NightSoft = WorkshopUi.Ink;
    private static readonly Color Jade = WorkshopUi.Green;
    private static readonly Color Gold = new Color32(214, 174, 103, 255);
    private static readonly Color Cream = new Color32(255, 246, 218, 255);
    private static readonly Color Muted = new Color32(187, 201, 170, 255);
    private static readonly Color Rust = WorkshopUi.Clay;

    private static WorkshopNarrativeView active;
    private RectTransform frame;
    private RectTransform page;
    private CanvasGroup group;
    private CanvasGroup contentGroup;
    private Action onClose;
    private bool closing;
    private bool ready = true;

    public static bool IsOpen => active != null;
    internal static bool HasSeenFinaleDialogue => WorkshopProgress.HasStep("dialogue.finale.ending");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active = null;
    }

    public static void ShowAnnouncement(WorkshopSeason season, Action onClosed = null)
    {
        var view = Open("Workshop Notice", 17500, () => PlaySeasonBriefing(season, onClosed));
        view.BuildAnnouncement(season);
    }

    internal static void PlaySeasonBriefing(WorkshopSeason season, Action onFinished)
    {
        if (!WorkshopProgress.IsUnlocked(season) || WorkshopProgress.IsComplete(season))
        {
            onFinished?.Invoke();
            return;
        }

        WorkshopDialogueLine[] script;
        switch (season)
        {
            case WorkshopSeason.Spring: script = WorkshopDialogueScripts.SpringBriefing; break;
            case WorkshopSeason.Summer: script = WorkshopDialogueScripts.SummerBriefing; break;
            case WorkshopSeason.Autumn: script = WorkshopDialogueScripts.AutumnBriefing; break;
            default: script = WorkshopDialogueScripts.WinterBriefing; break;
        }
        WorkshopDialoguePresenter.PlayOnce("dialogue.briefing." + season, script, onFinished, season);
    }

    public static void ShowChapterInterlude(WorkshopSeason completed, WorkshopSeason next,
        Action onEnter, Action onStay)
    {
        PlaySeasonReview(completed, () =>
        {
            if (completed == WorkshopSeason.Spring && next == WorkshopSeason.Summer)
            {
                onEnter?.Invoke();
                return;
            }

            var view = Open("Workshop Time Passage", 17500, onStay);
            view.BuildInterlude(completed, next, onEnter);
        });
    }

    internal static void PlaySeasonReview(WorkshopSeason season, Action onFinished = null)
    {
        WorkshopDialogueLine[] review;
        switch (season)
        {
            case WorkshopSeason.Spring: review = WorkshopDialogueScripts.SpringReview; break;
            case WorkshopSeason.Summer: review = WorkshopDialogueScripts.SummerReview; break;
            case WorkshopSeason.Autumn: review = WorkshopDialogueScripts.AutumnReview; break;
            default: review = WorkshopDialogueScripts.WinterReview; break;
        }
        WorkshopDialoguePresenter.PlayOnce("dialogue.review." + season, review, onFinished, season);
    }

    public static void ShowFinale(Action onFinished)
    {
        PlaySeasonReview(WorkshopSeason.Winter, () =>
            WorkshopDialoguePresenter.PlayOnce("dialogue.finale.display", WorkshopDialogueScripts.FinaleBeforeDisplay,
                () => WorkshopCraftRecordView.ShowChapterRecord(WorkshopSeason.Winter, () => ShowFinaleRecap(onFinished)),
                WorkshopSeason.Winter));
    }

    private static void ShowFinaleRecap(Action onFinished)
    {
        var view = Open("Workshop Finale", 17500, () =>
            WorkshopDialoguePresenter.PlayOnce("dialogue.finale.ending", WorkshopDialogueScripts.FinaleAfterDisplay, () =>
            {
                WorkshopProgress.MarkStep("finale.seen");
                onFinished?.Invoke();
            }, WorkshopSeason.Winter));
        view.BuildFinale();
    }

    private static WorkshopNarrativeView Open(string name, int order, Action closed)
    {
        if (active != null)
        {
            active.DestroyOverlay();
        }

        var frame = WorkshopUi.NewCanvas(name, order);
        active = frame.gameObject.AddComponent<WorkshopNarrativeView>();
        active.frame = frame;
        active.onClose = closed;
        active.group = frame.gameObject.AddComponent<CanvasGroup>();
        active.group.alpha = 0f;
        active.group.interactable = false;
        WorkshopAudioFeedback.Notice();
        return active;
    }

    private void BuildBase()
    {
        WorkshopUi.Block(frame, WorkshopUi.Veil);
        page = WorkshopUi.Card(frame, "Narrative Page", new Rect(250, 104, 1420, 872),
            NightSoft, WorkshopUi.Bronze);

        contentGroup = page.gameObject.AddComponent<CanvasGroup>();
        contentGroup.alpha = 0f;
    }

    private void BuildAnnouncement(WorkshopSeason season)
    {
        WorkshopUi.Block(frame, WorkshopNoticeBoard.ModalShade);
        page = WorkshopUi.Rect(frame, "Workshop Announcement Page", new Rect(0, 0, 1920, 1080));
        contentGroup = page.gameObject.AddComponent<CanvasGroup>();
        contentGroup.alpha = 0f;
        var board = WorkshopNoticeBoard.Build(page, season, () => BeginClose(onClose));
        StartCoroutine(Reveal(board.CloseAction));
    }

    private void BuildInterlude(WorkshopSeason completed, WorkshopSeason next, Action onEnter)
    {
        BuildBase();
        var closed = WorkshopNarrativeData.For(completed);
        var opened = WorkshopNarrativeData.For(next);
        Label(page, "Interlude Chapter", new Rect(98, 56, 620, 40),
            WorkshopProgress.SeasonName(completed) + "章完成", 30, Gold);
        WorkshopUi.Fill(page, "Interlude Line", new Rect(98, 112, 1220, 2), new Color32(214, 174, 103, 160));

        Label(page, "Interlude Title", new Rect(96, 164, 1190, 80),
            closed.ClosingTitle, 54, Cream);
        Label(page, "Interlude Body", new Rect(100, 253, 1178, 128),
            closed.ClosingBody, 30, Cream);

        WorkshopUi.Fill(page, "Season Progress Rail", new Rect(100, 470, 402, 3),
            new Color32(143, 155, 132, 80));
        var nextRail = WorkshopUi.Fill(page, "Season Next Rail", new Rect(502, 470, 402, 3),
            new Color32(143, 155, 132, 80));
        var fillRail = WorkshopUi.Fill(page, "Season Active Rail", new Rect(100, 470, 0, 3), Gold);

        for (int i = 0; i < 4; i++)
        {
            float center = 100f + i * 402f;
            Color fill = i == (int)next ? Gold : i <= (int)completed ? Jade : new Color32(52, 64, 55, 255);
            Color text = i <= (int)next ? Cream : Muted;
            var node = WorkshopUi.Card(page, "Season Node " + i, new Rect(center - 37, 433, 74, 74),
                NightSoft, fill);
            Label(node, "Node Season", new Rect(0, 5, 74, 64),
                WorkshopNarrativeData.SeasonShort((WorkshopSeason)i), 34, text, TextAnchor.MiddleCenter);
            Label(page, "Season Node Label " + i, new Rect(center - 88, 520, 176, 88),
                WorkshopNarrativeData.SeasonTime((WorkshopSeason)i), 24, text, TextAnchor.UpperCenter);
        }

        WorkshopUi.Fill(page, "Interlude Next Rule", new Rect(100, 637, 1218, 2),
            new Color32(214, 174, 103, 130));
        Label(page, "Next Announcement", new Rect(100, 665, 1218, 70),
            "下一章 · " + opened.AnnouncementTitle, 30, Cream, TextAnchor.MiddleCenter);

        ready = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        var stay = WorkshopUi.Button(page, "Interlude Stay", new Rect(778, 786, 220, 58),
            "留在主厅", () => BeginClose(onClose), Jade, enabled: false, size: 30, font: WorkshopArt.WorkshopFont);
        var proceed = WorkshopUi.Button(page, "Interlude Continue", new Rect(1028, 786, 292, 58),
            "进入" + WorkshopProgress.SeasonName(next) + "季工坊", () => BeginClose(onEnter),
            Rust, enabled: false, size: 30, font: WorkshopArt.WorkshopFont);
        var review = WorkshopUi.Button(page, "Interlude Chapter Record", new Rect(98, 786, 224, 58),
            "回看本章成果", () => WorkshopCraftRecordView.ShowChapterRecord(completed), Jade,
            enabled: WorkshopProgress.IsComplete(completed), size: 28, font: WorkshopArt.WorkshopFont);
        review.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = stay };
        stay.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = proceed, selectOnLeft = review };
        proceed.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = stay };
        proceed.GetComponentInChildren<Text>().verticalOverflow = VerticalWrapMode.Overflow;
        StartCoroutine(AnimateInterlude(fillRail.rectTransform, nextRail.rectTransform, proceed, stay));
    }

    private IEnumerator AnimateInterlude(RectTransform fillRail, RectTransform nextRail,
        UnityEngine.UI.Button proceed, UnityEngine.UI.Button stay)
    {
        yield return Fade(group, 0f, 1f, 0.3f);
        yield return Fade(contentGroup, 0f, 1f, 0.28f);

        Vector2 targetSize = nextRail.sizeDelta;
        float elapsed = 0f;
        const float duration = 0.9f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            fillRail.sizeDelta = new Vector2(targetSize.x * blend, targetSize.y);
            yield return null;
        }

        fillRail.sizeDelta = targetSize;
        WorkshopAudioFeedback.Progress();
        yield return new WaitForSecondsRealtime(0.18f);
        proceed.interactable = true;
        stay.interactable = true;
        group.interactable = true;
        ready = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(proceed.gameObject);
    }

    private void BuildFinale()
    {
        BuildBase();
        Label(page, "Finale Title", new Rect(96, 78, 1228, 88),
            "冬至 · 贺冬交衣", 57, Cream, TextAnchor.MiddleCenter);
        WorkshopUi.Fill(page, "Finale Line", new Rect(98, 184, 1220, 2), new Color32(214, 174, 103, 160));
        Label(page, "Finale Body", new Rect(147, 233, 1126, 250),
            "清明采桑喂蚕，备下蚕种。芒种择茧理丝，合成生丝。秋分借光验丝，寒露织绸。立冬试色染绸，小雪按样裁衣，大雪合缝翻衣。\n\n冬至试衣合身，师傅把贺冬的新衣挂上展架。",
            30, Cream, TextAnchor.MiddleCenter);

        for (int i = 0; i < 4; i++)
        {
            float center = 284f + i * 282f;
            var tag = WorkshopUi.Fill(page, "Finale Season " + i, new Rect(center - 96, 544, 192, 76),
                i == 3 ? Rust : Jade);
            Label(tag.transform, "Finale Season Label", new Rect(0, 0, 192, 76),
                WorkshopNarrativeData.SeasonShort((WorkshopSeason)i) + " · " +
                WorkshopProgress.ProductName((WorkshopSeason)i), 26, Cream, TextAnchor.MiddleCenter);
        }

        WorkshopUi.Fill(page, "Finale Divider", new Rect(100, 657, 1218, 2),
            new Color32(214, 174, 103, 130));
        string garment = WorkshopTailor.GarmentName(WorkshopProgress.GarmentId);
        Label(page, "Finale Garment", new Rect(100, 688, 1218, 70),
            "已完成 · " + garment + "\n工坊展架已经开放，可随时回来试穿。",
            30, Gold, TextAnchor.UpperCenter);

        var finish = WorkshopUi.Button(page, "Finale Continue", new Rect(1028, 786, 292, 58),
            "继续", () => BeginClose(onClose), Rust, size: 30, font: WorkshopArt.WorkshopFont);
        WorkshopUi.Button(page, "Finale Production Record", new Rect(98, 786, 251, 58), "看制作记录",
            () => WorkshopCraftRecordView.ShowProductionRecord(), Jade, size: 28, font: WorkshopArt.WorkshopFont);
        WorkshopUi.Button(page, "Finale Winter Record", new Rect(371, 786, 245, 58), "回看冬章成果",
            () => WorkshopCraftRecordView.ShowChapterRecord(WorkshopSeason.Winter), Jade, size: 28,
            font: WorkshopArt.WorkshopFont);
        finish.GetComponentInChildren<Text>().verticalOverflow = VerticalWrapMode.Overflow;
        StartCoroutine(Reveal(finish));
    }

    private IEnumerator Reveal(UnityEngine.UI.Button action)
    {
        ready = false;
        action.interactable = false;
        yield return Fade(group, 0f, 1f, 0.32f);
        yield return Fade(contentGroup, 0f, 1f, 0.42f);
        action.interactable = true;
        group.interactable = true;
        ready = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(action.gameObject);
    }

    private static IEnumerator Fade(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void Update()
    {
        if (closing || !ready || WorkshopCraftRecordView.BlocksWorldInput || WorkshopHeritageBrief.IsOpen ||
            WorkshopChapterGuide.IsOpen || !Input.GetKeyDown(KeyCode.Escape)) return;
        BeginClose(onClose);
    }

    private void BeginClose(Action callback)
    {
        if (closing) return;
        closing = true;
        if (contentGroup != null) contentGroup.interactable = false;
        if (group != null) group.interactable = false;
        StartCoroutine(CloseAfterFade(callback));
    }

    private IEnumerator CloseAfterFade(Action callback)
    {
        yield return Fade(contentGroup, contentGroup.alpha, 0f, 0.18f);
        yield return Fade(group, group.alpha, 0f, 0.24f);
        DestroyOverlay();
        callback?.Invoke();
    }

    private void DestroyOverlay()
    {
        if (frame == null) return;
        if (active == this) active = null;
        var canvas = frame.GetComponentInParent<Canvas>();
        if (canvas != null) canvas.gameObject.SetActive(false);
        Destroy(canvas != null ? canvas.gameObject : frame.gameObject);
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }

    private static Text Label(Transform parent, string name, Rect bounds, string value,
        int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        var label = WorkshopUi.Label(parent, name, bounds, value, size, color, anchor,
            WorkshopArt.WorkshopFont);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.lineSpacing = 1.18f;
        return label;
    }
}
