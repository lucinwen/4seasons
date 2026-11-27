using System;
using UnityEngine;
using UnityEngine.EventSystems;

internal enum WorkshopProcessCard
{
    Qingming, Guyu, Lixia, Xiaoman, Mangzhong, Liqiu, BailuWeaving, BailuRitual,
    Qiufen, Hanlu, Lidong, Xiaoxue, WinterEggs
}

/// <summary>Shows the supplied craft artwork before handing control to a worktable.</summary>
internal sealed class WorkshopProcessCardView : MonoBehaviour
{
    private static readonly Rect CardArea = new Rect(145, 106, 1630, 914);
    private static WorkshopProcessCardView current;
    private static int closedFrame = -1;

    private RectTransform frame;
    private GameObject previousSelection;
    private float previousTime;
    private Action play;

    internal static bool IsOpen => current != null;
    internal static bool BlocksWorldInput => IsOpen || closedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
        closedFrame = -1;
    }

    internal static void Show(WorkshopProcessCard card, Action play)
    {
        if (IsOpen) return;
        var art = Resources.Load<Sprite>("WorkshopProcessCards/" + card);
        if (art == null)
        {
            play?.Invoke();
            return;
        }

        var frame = WorkshopUi.NewCanvas("Workshop Process Card", 25100);
        WorkshopUi.Block(frame, new Color32(15, 23, 21, 218));
        var view = frame.gameObject.AddComponent<WorkshopProcessCardView>();
        current = view;
        view.frame = frame;
        view.play = play;
        view.previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        view.previousTime = Time.timeScale;
        Time.timeScale = 0f;
        view.Render(card, art);
    }

    private void Render(WorkshopProcessCard card, Sprite art)
    {
        var bounds = ArtworkBounds(card, art);
        WorkshopUi.Picture(frame, "Process Card Artwork", bounds, art);

        WorkshopUi.Button(frame, "Return From Process Card", new Rect(145, 31, 180, 59),
            "返回", Close, WorkshopUi.Paper, size: 28, font: WorkshopArt.ReadingFont);

        // The supplied card already prints its action. Bind it at the original position.
        var normalized = AcknowledgementBounds(card);
        var hitbox = new Rect(bounds.position + Vector2.Scale(bounds.size, normalized.position),
            Vector2.Scale(bounds.size, normalized.size));
        var accept = WorkshopUi.Button(frame, "Start Process Game", hitbox, "", Acknowledge, Color.clear);
        WorkshopKnowledgeView.MarkPrintedButtonFocus(accept);
        WorkshopCraftRecordView.KeepFocusIn(frame, accept);
    }

    private void Acknowledge()
    {
        if (current != this) return;
        var begin = play;
        Close();
        begin?.Invoke();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Close()
    {
        if (current != this) return;
        RestoreTime();
        closedFrame = Time.frameCount;
        WorkshopUi.CloseCanvas(frame);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy
                ? previousSelection : null);
    }

    private void OnDestroy()
    {
        if (current == this) RestoreTime();
    }

    private void RestoreTime()
    {
        current = null;
        Time.timeScale = previousTime;
    }

    private static Rect ArtworkBounds(WorkshopProcessCard card, Sprite art)
    {
        // The weaving PNG has unusually deep transparent margins above and below the illustration.
        var visible = card == WorkshopProcessCard.BailuWeaving
            ? new Rect(0, .0975f, 1, .744f)
            : new Rect(0, 0, 1, 1);
        var size = art.rect.size;
        float scale = Mathf.Min(CardArea.width / (size.x * visible.width), CardArea.height / (size.y * visible.height));
        size *= scale;
        return new Rect(CardArea.center - Vector2.Scale(size, visible.center), size);
    }

    private static Rect AcknowledgementBounds(WorkshopProcessCard card)
    {
        switch (card)
        {
            case WorkshopProcessCard.Qingming: return new Rect(.344f, .767f, .285f, .132f);
            case WorkshopProcessCard.Guyu: return new Rect(.356f, .776f, .289f, .143f);
            case WorkshopProcessCard.Lixia: return new Rect(.339f, .773f, .321f, .157f);
            case WorkshopProcessCard.Xiaoman: return new Rect(.368f, .783f, .276f, .147f);
            case WorkshopProcessCard.Mangzhong: return new Rect(.362f, .790f, .280f, .143f);
            case WorkshopProcessCard.Liqiu: return new Rect(.354f, .783f, .283f, .147f);
            case WorkshopProcessCard.BailuWeaving: return new Rect(.364f, .668f, .308f, .111f);
            case WorkshopProcessCard.BailuRitual: return new Rect(.350f, .804f, .301f, .154f);
            case WorkshopProcessCard.Qiufen: return new Rect(.317f, .780f, .337f, .161f);
            case WorkshopProcessCard.Hanlu: return new Rect(.501f, .772f, .316f, .152f);
            case WorkshopProcessCard.Lidong: return new Rect(.325f, .727f, .278f, .147f);
            case WorkshopProcessCard.Xiaoxue: return new Rect(.213f, .825f, .348f, .175f);
            case WorkshopProcessCard.WinterEggs: return new Rect(.366f, .801f, .268f, .129f);
            default: throw new ArgumentOutOfRangeException(nameof(card));
        }
    }
}
