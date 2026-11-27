using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

internal readonly struct WorkshopDialogueLine
{
    internal readonly string Speaker;
    internal readonly string Text;

    internal WorkshopDialogueLine(string speaker, string text)
    {
        Speaker = speaker;
        Text = text;
    }
}

internal sealed class WorkshopDialoguePresenter : MonoBehaviour
{
    private const float PanelWidth = 1560f;
    private const float PanelHeight = 328f;
    private const float PortraitSize = 174f;
    private static readonly Color PlayerAccent = new Color32(132, 66, 52, 255);
    private static readonly Color WorkshopAccent = new Color32(109, 76, 43, 255);
    private static readonly Color[] SeasonAccents =
    {
        new Color32(46, 83, 59, 255),
        new Color32(35, 83, 81, 255),
        new Color32(122, 67, 52, 255),
        new Color32(58, 74, 110, 255),
    };
    private static readonly Rect PlayerCrop = CropFromTop(0, 0, 324, 360, 324, 673);
    private static readonly Rect StewardCrop = CropFromTop(53, 0, 820, 820, 927, 1696);
    // Crop the existing character sprites to their faces and shoulders.
    private static readonly Rect[] NpcCrops =
    {
        CropFromTop(176, 243, 590, 590, 941, 1672),
        CropFromTop(108, 110, 710, 710, 941, 1672),
        CropFromTop(117, 0, 220, 220, 444, 444),
        CropFromTop(102, 0, 225, 225, 443, 443),
    };

    private static WorkshopDialoguePresenter active;
    private static int lastClosedFrame = -1;

    private readonly Queue<(WorkshopDialogueLine[] lines, Action finished, WorkshopSeason season)> pending =
        new Queue<(WorkshopDialogueLine[], Action, WorkshopSeason)>();
    private RectTransform frame;
    private UnityEngine.UI.Image panelArt;
    private UnityEngine.UI.Image portraitPaper;
    private UnityEngine.UI.Image namePlate;
    private UnityEngine.UI.RawImage portrait;
    private UnityEngine.UI.Text speaker;
    private UnityEngine.UI.Text body;
    private UnityEngine.UI.Text progress;
    private UnityEngine.UI.Button advanceButton;
    private UnityEngine.UI.Text buttonLabel;
    private WorkshopDialogueLine[] lines;
    private Action finished;
    private int index;
    private int lastAdvanceFrame;

    internal static bool IsOpen => active != null;
    internal static bool BlocksWorldInput => IsOpen || lastClosedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        active = null;
        lastClosedFrame = -1;
    }

    internal static void Play(WorkshopDialogueLine[] script, Action onFinished = null, WorkshopSeason? theme = null)
    {
        if (script == null || script.Length == 0)
        {
            onFinished?.Invoke();
            return;
        }
        WorkshopSeason season = theme ?? SceneTheme(script);
        if (active != null)
        {
            active.pending.Enqueue((script, onFinished, season));
            return;
        }

        var canvas = WorkshopUi.NewCanvas("Workshop Dialogue", 19000);
        active = canvas.gameObject.AddComponent<WorkshopDialoguePresenter>();
        active.Build(canvas);
        active.Begin(script, onFinished, season);
    }

    internal static void PlayOnce(string step, WorkshopDialogueLine[] script, Action onFinished = null,
        WorkshopSeason? theme = null)
    {
        if (WorkshopProgress.HasStep(step))
        {
            onFinished?.Invoke();
            return;
        }
        Play(script, () =>
        {
            WorkshopProgress.MarkStep(step);
            onFinished?.Invoke();
        }, theme);
    }

    private void Build(RectTransform canvas)
    {
        frame = canvas;
        WorkshopUi.Block(frame, new Color32(17, 32, 28, 74));
        var panel = WorkshopUi.Rect(frame.parent, "Dialogue Panel", new Rect(0, 0, PanelWidth, PanelHeight));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 48f);
        var shadow = WorkshopUi.Fill(panel, "Shadow", new Rect(3, 8, PanelWidth, PanelHeight),
            new Color32(22, 27, 23, 105));
        WorkshopUi.ApplyPixelShape(shadow);
        panelArt = WorkshopUi.Fill(panel, "Season Artwork", new Rect(0, 0, PanelWidth, PanelHeight), WorkshopUi.Paper);

        var portraitFrame = WorkshopUi.Card(panel, "Portrait Frame", new Rect(113, 64, 196, 196),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        portraitPaper = WorkshopUi.Fill(portraitFrame, "Portrait Paper", new Rect(6, 6, 184, 184), WorkshopUi.Paper);
        WorkshopUi.ApplyPixelShape(portraitPaper);
        portrait = WorkshopUi.Rect(portraitFrame, "Portrait", new Rect()).gameObject.AddComponent<UnityEngine.UI.RawImage>();
        portrait.rectTransform.anchorMin = portrait.rectTransform.anchorMax = portrait.rectTransform.pivot =
            new Vector2(0.5f, 0.5f);
        portrait.rectTransform.anchoredPosition = Vector2.zero;
        portrait.raycastTarget = false;

        namePlate = WorkshopUi.Fill(panel, "Name Plate", new Rect(338, 59, 202, 43), WorkshopUi.Ink);
        WorkshopUi.ApplyPixelShape(namePlate);
        speaker = WorkshopUi.Label(namePlate.transform, "Speaker", new Rect(12, 0, 178, 43), "", 31,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        WorkshopUi.Fill(panel, "Speaker Rule", new Rect(556, 80, 872, 2), WorkshopUi.Line);
        body = WorkshopUi.Label(panel, "Spoken Line", new Rect(340, 113, 1088, 110), "", 33,
            WorkshopUi.Ink, TextAnchor.UpperLeft);
        body.supportRichText = false;
        body.lineSpacing = 1.13f;
        body.resizeTextForBestFit = true;
        body.resizeTextMinSize = 30;
        body.resizeTextMaxSize = 33;

        progress = WorkshopUi.Label(panel, "Dialogue Progress", new Rect(340, 230, 220, 43), "", 24, WorkshopUi.Faded);
        WorkshopUi.Label(panel, "Keyboard Shortcut", new Rect(1035, 230, 193, 43),
            "空格 / 回车", 22, WorkshopUi.Faded, TextAnchor.MiddleRight);
        advanceButton = WorkshopUi.Button(panel, "Advance Dialogue", new Rect(1244, 226, 184, 49),
            "", Advance, WorkshopUi.Ink);
        var navigation = advanceButton.navigation;
        navigation.mode = UnityEngine.UI.Navigation.Mode.None;
        advanceButton.navigation = navigation;
        buttonLabel = WorkshopUi.Label(advanceButton.transform, "Label", new Rect(0, 0, 184, 49), "", 28,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(advanceButton.gameObject);
    }

    private void Begin(WorkshopDialogueLine[] script, Action onFinished, WorkshopSeason season)
    {
        lines = script;
        finished = onFinished;
        index = 0;
        lastAdvanceFrame = Time.frameCount;
        panelArt.sprite = WorkshopArt.DialoguePanel(season);
        panelArt.color = panelArt.sprite == null ? WorkshopUi.Paper : Color.white;
        advanceButton.image.color = SeasonAccents[(int)season];
        DisplayLine();
        WorkshopAudioFeedback.Notice();
    }

    private void DisplayLine()
    {
        var line = lines[index];
        if (speaker.text != line.Speaker)
        {
            speaker.text = line.Speaker;
            UpdatePortrait(line.Speaker);
        }
        body.text = line.Text;
        progress.text = lines.Length > 1 ? $"{index + 1} / {lines.Length}" : "";
        buttonLabel.text = index + 1 < lines.Length ? "下一句" : "继续";
    }

    private void UpdatePortrait(string name)
    {
        var npcSeason = GetNpcSeason(name);
        bool playerSpeaking = name == "玩家";
        var stewardPortrait = name == "工坊掌事" ? WorkshopArt.DialogueStewardPortrait : null;
        bool hasStewardPortrait = stewardPortrait != null;
        var art = playerSpeaking ? WorkshopArt.DialoguePlayerPortrait :
            npcSeason.HasValue ? WorkshopArt.DialogueNpcPortrait(npcSeason.Value) :
            hasStewardPortrait ? stewardPortrait : WorkshopArt.DialogueWorkshopIcon;
        Color accent = playerSpeaking ? PlayerAccent :
            npcSeason.HasValue ? SeasonAccents[(int)npcSeason.Value] : WorkshopAccent;
        namePlate.color = accent;
        portraitPaper.color = hasStewardPortrait ? Color.white : Color.Lerp(WorkshopUi.Paper, accent, 0.14f);
        portrait.enabled = art != null;
        if (art == null) return;

        var crop = playerSpeaking ? PlayerCrop :
            npcSeason.HasValue ? NpcCrops[(int)npcSeason.Value] :
            hasStewardPortrait ? StewardCrop : new Rect(0, 0, 1, 1);
        var source = art.rect;
        var texture = art.texture;
        var cropSize = Vector2.Scale(source.size, crop.size);
        portrait.texture = texture;
        portrait.uvRect = new Rect((source.x + source.width * crop.x) / texture.width,
            (source.y + source.height * crop.y) / texture.height, cropSize.x / texture.width, cropSize.y / texture.height);
        float size = playerSpeaking || npcSeason.HasValue || hasStewardPortrait ? PortraitSize : 126f;
        portrait.rectTransform.sizeDelta = cropSize * (size / Mathf.Max(cropSize.x, cropSize.y));
    }

    private static WorkshopSeason SceneTheme(WorkshopDialogueLine[] script)
    {
        foreach (var line in script)
        {
            var season = GetNpcSeason(line.Speaker);
            if (season.HasValue) return season.Value;
        }
        return WorkshopProgress.TryGetSeason(SceneManager.GetActiveScene().name, out var sceneSeason)
            ? sceneSeason : WorkshopSeason.Spring;
    }

    private static WorkshopSeason? GetNpcSeason(string name)
    {
        switch (name)
        {
            case "蚕桑师傅": return WorkshopSeason.Spring;
            case "缫丝师傅": return WorkshopSeason.Summer;
            case "织染师傅": return WorkshopSeason.Autumn;
            case "裁衣师傅": return WorkshopSeason.Winter;
            default: return null;
        }
    }

    private static Rect CropFromTop(float left, float top, float width, float height, float sourceWidth, float sourceHeight)
    {
        return new Rect(left / sourceWidth, 1f - (top + height) / sourceHeight,
            width / sourceWidth, height / sourceHeight);
    }

    private void Update()
    {
        var events = EventSystem.current;
        if (events != null && !events.alreadySelecting && events.currentSelectedGameObject == null)
            events.SetSelectedGameObject(advanceButton.gameObject);
    }

    private void Advance()
    {
        if (lastAdvanceFrame == Time.frameCount) return;
        lastAdvanceFrame = Time.frameCount;
        if (++index < lines.Length)
        {
            DisplayLine();
            return;
        }

        Action callback = finished;
        if (pending.Count > 0)
        {
            var next = pending.Dequeue();
            Begin(next.lines, next.finished, next.season);
        }
        else
        {
            active = null;
            lastClosedFrame = Time.frameCount;
            WorkshopUi.CloseCanvas(frame);
        }
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }
}
