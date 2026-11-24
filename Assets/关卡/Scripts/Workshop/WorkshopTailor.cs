using System;
using System.Collections.Generic;
using JiangnanDye;
using Qingming.LevelBackground;
using UnityEngine;

/// <summary>Winter's final task, from choosing a garment to sewing the three cut pieces.</summary>
public sealed class WorkshopTailor : MonoBehaviour
{
    private sealed class GarmentStyle
    {
        public readonly string Id;
        public readonly string Name;

        public GarmentStyle(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    private static readonly Dictionary<DyeColor, GarmentStyle[]> Styles = new Dictionary<DyeColor, GarmentStyle[]>
    {
        { DyeColor.Red, new[]
            {
                new GarmentStyle("1_01", "花纹交领"),
                new GarmentStyle("3_01", "鹤纹长衣"),
                new GarmentStyle("3_12", "金纹披风"),
                new GarmentStyle("3_16", "梅枝长衣")
            }
        },
        { DyeColor.Yellow, new[]
            {
                new GarmentStyle("1_05", "菱纹交领"),
                new GarmentStyle("3_07", "莲纹短襦"),
                new GarmentStyle("4_01", "桂枝交领裙"),
                new GarmentStyle("4_02", "金云盘扣袄裙"),
                new GarmentStyle("4_03", "杏枝披肩裙")
            }
        },
        { DyeColor.Green, new[]
            {
                new GarmentStyle("1_03", "花纹交领"),
                new GarmentStyle("3_06", "花枝对襟"),
                new GarmentStyle("3_13", "竹叶长衣")
            }
        },
        { DyeColor.Blue, new[]
            {
                new GarmentStyle("1_02", "花纹交领"),
                new GarmentStyle("3_02", "云浪纹长衣"),
                new GarmentStyle("3_05", "雪花纹长衣"),
                new GarmentStyle("3_09", "雪花纹披风")
            }
        },
        { DyeColor.Purple, new[]
            {
                new GarmentStyle("1_04", "花纹交领"),
                new GarmentStyle("3_04", "花纹齐胸裙"),
                new GarmentStyle("3_14", "莲纹对襟"),
                new GarmentStyle("4_04", "紫藤交领裙"),
                new GarmentStyle("4_05", "金蕊盘扣袄裙"),
                new GarmentStyle("4_06", "紫兰披肩裙")
            }
        }
    };

    private static readonly string[] PieceNames = { "前衣片", "后衣片", "袖片" };
    private static readonly Color Accent = new Color32(107, 57, 80, 255);
    private static readonly Color Unselected = new Color32(223, 218, 225, 255);

    private static WorkshopTailor active;
    private readonly List<GarmentStyle> available = new List<GarmentStyle>();
    private readonly Sprite[] pieceSprites = new Sprite[PieceNames.Length];
    private Action onBack;
    private Action onCompleted;
    private Action backAction;
    private RectTransform frame;
    private RectTransform panel;
    private GarmentStyle selected;
    private DyeColor dye;
    private Color fabricTint;
    private Material fabricMaterial;
    private int piecesPlaced;
    private bool submitting;
    private bool liningFitted;
    private UnityEngine.UI.Text sewingStatus;
    private UnityEngine.UI.Button sewButton;
    private UnityEngine.UI.Button liningButton;

    private bool UsesLining => WorkshopProgress.HasStep(WorkshopObservationView.XiaoxueLiningStep);

    internal static bool IsOpen => active != null;

    internal static void Show(Action back, Action completed)
    {
        if (active != null) return;
        if (!WorkshopProgress.CanSubmit(WorkshopSeason.Winter))
        {
            if (SimplePopup.Show("制作成衣", "准备好染布、三块衣片和缝衣针后，就可以制作成衣。",
                    "返回合成台", back) == null)
                back?.Invoke();
            return;
        }

        if (!TryResolveDye(WorkshopProgress.DyedColor, out DyeColor dye))
        {
            if (SimplePopup.Show("制作成衣", "请在冬季染坊确认布料颜色，再回来选款。",
                    "返回合成台", back) == null)
                back?.Invoke();
            return;
        }

        RectTransform frame = WorkshopUi.NewCanvas("WorkshopTailor", 4500);
        WorkshopUi.Block(frame, MinigameUiPresentation.BackgroundVeil);
        active = frame.gameObject.AddComponent<WorkshopTailor>();
        active.Open(frame, dye, back, completed);
    }

    public static bool IsGarmentAvailable(string id, string color)
    {
        if (!TryResolveDye(color, out DyeColor dye)) return false;
        foreach (GarmentStyle style in Styles[dye])
        {
            if (style.Id == id) return WorkshopArt.Garment(id) != null;
        }
        return false;
    }

    internal static Sprite PreviewForDye(string color)
    {
        if (!TryResolveDye(color, out var dye)) return null;
        foreach (var style in Styles[dye])
        {
            var sprite = WorkshopArt.Garment(style.Id);
            if (sprite != null) return sprite;
        }
        return null;
    }

    public static string GarmentName(string id)
    {
        foreach (GarmentStyle[] group in Styles.Values)
        {
            foreach (GarmentStyle style in group)
            {
                if (style.Id == id) return style.Name;
            }
        }
        return "桑蚕成衣";
    }

    private static bool TryResolveDye(string name, out DyeColor dye)
    {
        foreach (DyeColor color in DyeColorUtil.AllColors)
        {
            string colorName = DyeColorUtil.ChineseName(color);
            if (name != colorName && name != colorName + "布") continue;
            dye = color;
            return true;
        }

        dye = default;
        return false;
    }

    private void Open(RectTransform canvasFrame, DyeColor clothColor, Action back, Action completed)
    {
        frame = canvasFrame;
        dye = clothColor;
        onBack = back;
        onCompleted = completed;
        var config = Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
        fabricTint = config != null ? config.GetClothTint(dye) : DyeColorUtil.Tint(dye);
        fabricMaterial = Resources.Load<Material>("WorkshopClothDye");

        bool haveAllPieces = true;
        for (int i = 0; i < pieceSprites.Length; i++)
        {
            pieceSprites[i] = WorkshopArt.CutPiece(i);
            haveAllPieces &= pieceSprites[i] != null;
        }

        foreach (GarmentStyle style in Styles[dye])
        {
            if (WorkshopArt.Garment(style.Id) != null) available.Add(style);
        }

        if (!haveAllPieces || available.Count == 0)
        {
            if (SimplePopup.Show("制作成衣", "衣片或衣样素材无法读取。", "返回合成台", Close) == null)
                Close();
            return;
        }

        BuildChooser();
    }

    private void BuildPanel(string title, string prompt, string backLabel, Action back)
    {
        backAction = back;
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
            Destroy(panel.gameObject);
        }

        panel = WorkshopUi.Rect(frame, title, new Rect(46, 30, 1828, 1020));
        WorkshopUi.Panel(panel, "Embroidered Tailor Frame", new Rect(0, 0, 1828, 1020),
            Resources.Load<Sprite>("WorkshopDelivery/B04") ?? WorkshopArt.WorkshopTaskBoard);
        Label(panel, "Title", new Rect(689, 93, 450, 85), title, 45, Accent, TextAnchor.MiddleCenter);
        Label(panel, "Prompt", new Rect(252, 323, 1085, 49), prompt, 31, Accent);
        if (back != null)
            WorkshopUi.PixelButton(panel, "Back", new Rect(1114, 262, 187, 56), backLabel, back, true, 27, Accent);
    }

    private static UnityEngine.UI.Text Label(Transform parent, string name, Rect bounds, string text, int size,
        Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var label = WorkshopUi.Label(parent, name, bounds, text, size, color, anchor, WorkshopArt.WorkshopFont);
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private void BuildChooser()
    {
        BuildPanel(UsesLining ? "小雪 · 夹衣选样" : "缝素堂 · 选衣样",
            UsesLining ? "小雪转凉，用裁好的外片和里襟配衣样。" : "选择一件衣样，使用已染好的布料缝制。",
            "返回合成台", Close);
        Sprite winterFrame = WorkshopArt.SeasonPanel(WorkshopSeason.Winter);
        WorkshopUi.Panel(panel, "PreviewPanel", new Rect(866, 377, 454, 363), winterFrame);
        var swatch = WorkshopUi.Fill(panel, "FabricSwatch", new Rect(906, 410, 25, 25), fabricTint);
        WorkshopUi.ApplyPixelShape(swatch);
        string cloth = dye == DyeColor.Yellow && WorkshopObservationView.HasOrderWork(WorkshopObservation.LidongDye)
            ? "立冬栀黄染绸" : DyeColorUtil.ChineseName(dye) + "染布";
        Label(panel, "FabricColor", new Rect(943, 399, 305, 43), cloth, 27, Accent);

        var previewClip = WorkshopWardrobe.Clip(panel, "Garment Preview Cutout");
        var preview = WorkshopUi.Picture(previewClip, "GarmentPreview", new Rect(), null, stretch: true);
        var previewName = Label(panel, "GarmentName", new Rect(887, 672, 410, 51), "", 32,
            Accent, TextAnchor.MiddleCenter);
        var next = WorkshopUi.Button(panel, "ConfirmStyle", new Rect(906, 833, 378, 61),
            "确认款式 · 开始缝制", BuildSewing, Accent, enabled: false, size: 32, font: WorkshopArt.WorkshopFont);

        var optionSurfaces = new UnityEngine.UI.Image[available.Count];
        var optionSelections = new UnityEngine.UI.Image[available.Count];
        var buttons = new UnityEngine.UI.Button[available.Count];
        bool threeRows = available.Count > 4;
        float cardHeight = threeRows ? 126f : 161f;
        float artworkTop = threeRows ? 9f : 17f;
        float artworkHeight = threeRows ? 81f : 102f;
        float captionTop = threeRows ? 88f : 116f;
        for (int i = 0; i < available.Count; i++)
        {
            int index = i;
            GarmentStyle style = available[i];
            var bounds = new Rect(253 + 299 * (i % 2), 378 + (cardHeight + 13f) * (i / 2), 278, cardHeight);
            var button = WorkshopUi.Button(panel, "Option-" + style.Id, bounds, "", null, Color.clear);
            var face = (UnityEngine.UI.Image)button.targetGraphic;
            face.sprite = winterFrame;
            face.color = winterFrame == null ? WorkshopUi.Cream : Unselected;
            var selectedMark = WorkshopUi.Fill(button.transform, "Selected ribbon", new Rect(28, 16, 18, 20),
                Accent);
            WorkshopUi.ApplyPixelShape(selectedMark);
            selectedMark.enabled = false;
            var clip = WorkshopWardrobe.Clip(button.transform, "Garment Cutout");
            var image = WorkshopUi.Picture(clip, "Garment", new Rect(), null, stretch: true);
            WorkshopWardrobe.PlaceInBox(clip, image, style.Id, new Rect(26, artworkTop, 226, artworkHeight));
            Label(button.transform, "Style", new Rect(14, captionTop, 250, 37), style.Name, 26,
                Accent, TextAnchor.MiddleCenter);
            optionSurfaces[i] = face;
            optionSelections[i] = selectedMark;
            buttons[i] = button;
            button.onClick.AddListener(() =>
            {
                selected = style;
                WorkshopWardrobe.PlaceInBox(previewClip, preview, style.Id, new Rect(915, 448, 370, 221));
                previewName.text = style.Name;
                next.interactable = true;
                for (int j = 0; j < optionSurfaces.Length; j++)
                {
                    optionSurfaces[j].color = j == index ? Color.white : Unselected;
                    optionSelections[j].enabled = j == index;
                }
            });
        }

        int current = available.IndexOf(selected);
        if (current < 0 && UsesLining && dye == DyeColor.Yellow)
            current = available.FindIndex(style => style.Id == "4_02");
        buttons[Mathf.Max(0, current)].onClick.Invoke();
    }

    private void BuildSewing()
    {
        bool winterLining = UsesLining;
        BuildPanel(winterLining ? "小雪 · 拼缝夹衣" : "缝素堂 · 拼合衣片",
            winterLining ? "冷天要夹衣。先对齐三片面料，再合上裁台特意留下的里襟。" : "把衣片拖到对应的样板上，三片归位后即可缝制。",
            "重新选款", BuildChooser);
        submitting = false;
        liningFitted = false;
        liningButton = null;
        piecesPlaced = 0;
        Sprite winterFrame = WorkshopArt.SeasonPanel(WorkshopSeason.Winter);
        WorkshopUi.Panel(panel, "Tray", new Rect(251, 378, 325, 356), winterFrame);
        WorkshopUi.Panel(panel, "Workbench", new Rect(563, 378, 786, 455),
            WorkshopArt.CuttingWorkbench ?? winterFrame);
        Label(panel, "TrayTitle", new Rect(281, 398, 265, 43), "已裁衣片", 29, Accent);
        Label(panel, "PatternTitle", new Rect(640, 382, 615, 42), selected.Name + "样板", 30, Accent,
            TextAnchor.MiddleCenter);

        var targetCells = new[]
        {
            new Rect(980, 455, 252, 122),
            new Rect(675, 455, 252, 122),
            new Rect(823, 587, 267, 128)
        };
        var sourceCells = new[]
        {
            new Rect(282, 443, 262, 82),
            new Rect(282, 530, 262, 82),
            new Rect(282, 617, 262, 82)
        };

        var progress = WorkshopUi.Panel(panel, "Sewing Progress Plaque", new Rect(682, 789, 546, 46),
            WorkshopArt.SeasonButton(WorkshopSeason.Winter));
        sewingStatus = Label(progress.transform, "Progress", new Rect(12, 0, 522, 46),
            "衣片归位 0 / 3", 25, progress.sprite == null ? Accent : WorkshopUi.Cream,
            TextAnchor.MiddleCenter);
        var resetBounds = winterLining ? new Rect(317, 755, 215, 59) : new Rect(684, 840, 215, 59);
        WorkshopUi.PixelButton(panel, "ResetPieces", resetBounds,
            "重新摆放", BuildSewing, false, 26);
        if (winterLining)
        {
            WorkshopUi.Picture(panel, "Xiaoxue Reserved Lining", new Rect(588, 728, 73, 52),
                WorkshopArt.CutPiece(0), Color.Lerp(fabricTint, Color.white, .40f));
            Label(panel, "Xiaoxue Lining Caption", new Rect(672, 736, 469, 43), "里襟 · 小雪排料时所留整布", 22, WorkshopUi.Ink);
            liningButton = WorkshopUi.Button(panel, "XiaoxueFitLining", new Rect(684, 840, 215, 59),
                "贴合里襟", FitLining, Accent, enabled: false, size: 25, font: WorkshopArt.WorkshopFont);
        }
        sewButton = WorkshopUi.Button(panel, "FinishGarment", new Rect(927, 840, 360, 59), "缝制成衣", Finish,
            Accent, enabled: false, size: 30, font: WorkshopArt.WorkshopFont);

        for (int i = 0; i < PieceNames.Length; i++)
        {
            Rect cell = targetCells[i];
            Rect targetBounds = new Rect(cell.x + 8, cell.y, cell.width - 16, cell.height - 25);
            var target = WorkshopUi.Picture(panel, PieceNames[i] + "-Target", targetBounds, pieceSprites[i],
                new Color(1f, 0.96f, 0.9f, 0.44f));
            Label(panel, PieceNames[i] + "-TargetLabel",
                new Rect(cell.x, cell.yMax - 26, cell.width, 32), PieceNames[i], 25,
                WorkshopUi.Cream, TextAnchor.MiddleCenter);

            cell = sourceCells[i];
            WorkshopUi.Panel(panel, PieceNames[i] + "-Tray", cell, winterFrame);
            Label(panel, PieceNames[i] + "-TrayLabel",
                new Rect(cell.x + 138, cell.y, 108, cell.height), PieceNames[i], 26, Accent, TextAnchor.MiddleCenter);
            var piece = WorkshopUi.Picture(panel, PieceNames[i] + "-Piece",
                new Rect(cell.x + 15, cell.y + 5, 113, cell.height - 10), pieceSprites[i], fabricTint);
            if (fabricMaterial != null) piece.material = fabricMaterial;
            piece.raycastTarget = true;
            piece.gameObject.AddComponent<WorkshopTailorPiece>().Bind(target.rectTransform, OnPieceDrop);
        }
    }

    private void OnPieceDrop(bool placed)
    {
        if (placed)
        {
            piecesPlaced++;
            WorkshopAudioFeedback.Place();
            sewingStatus.text = $"衣片归位 {piecesPlaced} / {PieceNames.Length}";
            if (piecesPlaced != PieceNames.Length) return;

            if (liningButton != null)
            {
                sewingStatus.text = "外片已对齐 · 合上里襟再缝";
                liningButton.interactable = true;
            }
            else
            {
                sewButton.interactable = true;
                WorkshopDialoguePresenter.PlayOnce("dialogue.winter.piecesFitted", WorkshopDialogueScripts.WinterCuttingComplete);
            }
        }
        else
        {
            WorkshopAudioFeedback.Notice();
            sewingStatus.text = $"衣片归位 {piecesPlaced} / {PieceNames.Length}   请拖到对应样板";
        }
    }

    private void FitLining()
    {
        if (liningButton == null || piecesPlaced != PieceNames.Length || liningFitted) return;

        liningFitted = true;
        liningButton.interactable = false;
        var lining = panel.Find("Xiaoxue Reserved Lining") as RectTransform;
        if (lining != null) lining.anchoredPosition += new Vector2(209, 38);
        var caption = panel.Find("Xiaoxue Lining Caption")?.GetComponent<UnityEngine.UI.Text>();
        if (caption != null) caption.text = "里襟已对齐 · 可缝外缘";
        sewingStatus.text = "外片和里襟已对齐";
        sewButton.interactable = true;
        WorkshopAudioFeedback.Fabric();
    }

    private void Finish()
    {
        if (submitting || piecesPlaced != PieceNames.Length || (UsesLining && !liningFitted)) return;
        submitting = true;
        sewButton.interactable = false;
        if (!WorkshopProgress.CompleteWinter(selected.Id))
        {
            if (SimplePopup.Show("制作成衣", "任务材料或布色已变更，请返回合成台重新选择。",
                    "返回合成台", Close) == null)
                Close();
            return;
        }

        var finished = onCompleted;
        CloseFrame();
        WorkshopDialoguePresenter.PlayOnce("dialogue.winter.garmentSewn", WorkshopDialogueScripts.WinterTailoringComplete,
            () => WorkshopNarrativeView.ShowFinale(() => WorkshopWardrobe.Show(finished)));
    }

    private void Update()
    {
        if (active != this || submitting || SimplePopup.IsOpen || WorkshopDialoguePresenter.BlocksWorldInput) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        WorkshopAudioFeedback.Click();
        backAction?.Invoke();
    }

    private void Close()
    {
        if (active != this) return;
        CloseFrame();
        onBack?.Invoke();
    }

    private void CloseFrame()
    {
        active = null;
        WorkshopUi.CloseCanvas(frame);
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }
}
