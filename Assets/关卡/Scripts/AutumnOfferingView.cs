using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>The autumn altar and the live status of its three offerings.</summary>
internal sealed class AutumnOfferingView : MonoBehaviour
{
    private static readonly Color FrameEdge = new Color32(141, 77, 46, 255);
    private static readonly Color IllustrationPaper = new Color32(232, 206, 158, 255);
    private static readonly Color RowPaper = new Color32(252, 240, 211, 255);
    private static readonly Color ReadyPaper = new Color32(226, 232, 198, 255);
    private static readonly Rect PanelBounds = new Rect(330, 210, 1260, 660);

    private static AutumnOfferingView current;
    private static int lastClosedFrame = -1;
    private RectTransform frame;

    internal static bool IsOpen => current != null;
    internal static bool BlocksWorldInput => IsOpen || lastClosedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        current = null;
        lastClosedFrame = -1;
    }

    internal static void Show(Action submit)
    {
        if (IsOpen) return;

        var frame = WorkshopUi.NewCanvas("Autumn Offering Table", 11000);
        WorkshopUi.Block(frame, new Color32(20, 16, 12, 178));
        current = frame.gameObject.AddComponent<AutumnOfferingView>();
        current.frame = frame;
        current.Build(submit);
        WorkshopAudioFeedback.Notice();
    }

    private void Build(Action submit)
    {
        bool completed = AutumnLevelState.HasRitualDone;
        int collected = completed ? 3 : AutumnLevelState.OfferingCount;
        var table = WorkshopUi.PixelCard(frame, "Offering Table Panel", PanelBounds, WorkshopUi.Paper, FrameEdge);
        WorkshopUi.Label(table, "Offering Table Title", new Rect(37, 23, 530, 63),
            "祭祀供台", 47, WorkshopUi.Ink);
        WorkshopUi.Label(table, "Ritual Offering Count", new Rect(852, 29, 365, 50),
            completed ? "酬蚕仪式已完成" : $"贡品 {collected}/3", 32, FrameEdge, TextAnchor.MiddleRight);
        WorkshopUi.Fill(table, "Header Rule", new Rect(34, 92, 1192, 2), WorkshopUi.Line);

        DrawIllustration(table);
        var icons = AutumnItemIcons.Load();
        DrawOffering(table, 108, "蚕花酒", FindWineIcon(), AutumnLevelState.HasWine, completed, "点击附近的蚕花酒罐");
        DrawOffering(table, 259, "香蕉", icons?.banana, AutumnLevelState.HasBanana, completed, "点击香蕉果篮领取");
        DrawOffering(table, 410, "糕点", icons?.cake, AutumnLevelState.HasCake, completed,
            AutumnLevelState.HasHammer ? "向织造师傅领取" : "先向师傅借锤子，再与他对话领糕点");

        bool canSubmit = collected == 3 && !completed;
        var back = WorkshopUi.PixelButton(table, "Return From Offering Table", new Rect(34, 576, 192, 58),
            "返回", Close, false, 27);
        var selected = back;
        if (canSubmit)
            selected = WorkshopUi.PixelButton(table, "Submit Offerings", new Rect(965, 576, 262, 58),
                "提交贡品", () =>
                {
                    if (current != this || AutumnLevelState.OfferingCount != 3 || AutumnLevelState.HasRitualDone) return;
                    Close();
                    submit?.Invoke();
                }, true, 30, FrameEdge);

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected.gameObject);
    }

    private static void DrawIllustration(RectTransform table)
    {
        var art = WorkshopUi.PixelCard(table, "Altar Illustration Frame", new Rect(34, 108, 590, 441),
            IllustrationPaper, WorkshopUi.Bronze);
        var texture = Resources.Load<Texture2D>("WorkshopFinish/AutumnOfferingTable");
        if (texture == null) return;

        var bounds = new Rect(18, 12, 554, 417);
        float scale = Mathf.Min(bounds.width / texture.width, bounds.height / texture.height);
        var size = new Vector2(texture.width, texture.height) * scale;
        var illustration = WorkshopUi.Rect(art, "Altar Illustration", new Rect(bounds.center - size * 0.5f, size))
            .gameObject.AddComponent<UnityEngine.UI.RawImage>();
        illustration.texture = texture;
        illustration.raycastTarget = false;
    }

    private static void DrawOffering(RectTransform parent, float top, string name, Sprite icon,
        bool collected, bool completed, string source)
    {
        bool ready = completed || collected;
        var row = WorkshopUi.PixelCard(parent, name + " Offering", new Rect(648, top, 579, 139),
            ready ? ReadyPaper : RowPaper, ready ? WorkshopUi.Green : WorkshopUi.Bronze);
        WorkshopUi.Picture(row, "Offering Icon", new Rect(20, 25, 88, 88), icon,
            ready ? Color.white : new Color(1f, 1f, 1f, 0.73f));
        float labelTop = ready ? 48 : 19;
        WorkshopUi.Label(row, "Offering Name", new Rect(124, labelTop, 240, 43), name, 32);
        if (!completed)
            WorkshopUi.Label(row, "Offering Status", new Rect(390, labelTop, 164, 43),
                ready ? "已备齐" : "待收集", 26, ready ? WorkshopUi.Green : FrameEdge, TextAnchor.MiddleRight);
        if (!ready)
            WorkshopUi.Label(row, "Offering Source", new Rect(124, 68, 426, 61), source, 27, WorkshopUi.Ink,
                TextAnchor.UpperLeft);
    }

    private static Sprite FindWineIcon()
    {
        foreach (var prop in FindObjectsOfType<AutumnItemClick>())
        {
            if (prop.kind != AutumnItemClick.ItemKind.Wine) continue;
            var renderer = prop.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) return renderer.sprite;
        }
        return null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Close()
    {
        if (current != this) return;
        current = null;
        lastClosedFrame = Time.frameCount;
        WorkshopUi.CloseCanvas(frame);
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }
}
