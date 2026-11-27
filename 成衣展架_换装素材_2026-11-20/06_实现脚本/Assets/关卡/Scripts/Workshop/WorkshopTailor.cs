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
                new GarmentStyle("3_07", "莲纹短襦")
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
                new GarmentStyle("3_14", "莲纹对襟")
            }
        }
    };

    private static readonly string[] PieceNames = { "前衣片", "后衣片", "袖片" };
    private static readonly Color Accent = new Color32(43, 78, 65, 255);
    private static readonly Color Border = new Color32(153, 129, 90, 255);
    private static readonly Color BoardPaper = new Color32(202, 214, 195, 255);
    private static readonly Color PatternPaper = new Color32(233, 234, 217, 255);

    private static WorkshopTailor active;
    private readonly List<GarmentStyle> available = new List<GarmentStyle>();
    private readonly Sprite[] pieceSprites = new Sprite[PieceNames.Length];
    private Action onClose;
    private RectTransform frame;
    private RectTransform panel;
    private GarmentStyle selected;
    private DyeColor dye;
    private Color fabricTint;
    private Material fabricMaterial;
    private int piecesPlaced;
    private bool submitting;
    private UnityEngine.UI.Text sewingStatus;
    private UnityEngine.UI.Button sewButton;

    public static void Show(Action onClose)
    {
        if (active != null) return;
        if (!WorkshopProgress.CanSubmit(WorkshopSeason.Winter))
        {
            if (SimplePopup.Show("制作成衣", "准备好染布、三块衣片和缝衣针后，就可以制作成衣。",
                    "返回任务", onClose) == null)
                onClose?.Invoke();
            return;
        }

        if (!TryResolveDye(WorkshopProgress.DyedColor, out DyeColor dye))
        {
            if (SimplePopup.Show("制作成衣", "请在冬季染坊确认布料颜色，再回来选款。",
                    "返回任务", onClose) == null)
                onClose?.Invoke();
            return;
        }

        RectTransform frame = WorkshopUi.NewCanvas("WorkshopTailor", 4500);
        WorkshopUi.Block(frame, new Color32(32, 43, 43, 179));
        active = frame.gameObject.AddComponent<WorkshopTailor>();
        active.Open(frame, dye, onClose);
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

    private void Open(RectTransform canvasFrame, DyeColor clothColor, Action closed)
    {
        frame = canvasFrame;
        dye = clothColor;
        onClose = closed;
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
            Close();
            SimplePopup.Show("制作成衣", "衣片或衣样素材无法读取，请返回任务面板。");
            return;
        }

        BuildChooser();
    }

    private void BuildPanel(string title, string prompt, string backLabel, Action back)
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
            Destroy(panel.gameObject);
        }

        panel = WorkshopUi.Rect(frame, title, new Rect(198, 86, 1524, 908));
        WorkshopUi.Panel(panel, "Panel", new Rect(0, 0, 1524, 908), WorkshopArt.WorkshopTaskBoard);

        Label(panel, "Title", new Rect(242, 39, 820, 74), title, 51, Accent);
        Label(panel, "Prompt", new Rect(242, 117, 1120, 42), prompt, 29);
        if (back != null)
            WorkshopUi.Button(panel, "Back", new Rect(1149, 50, 190, 58), backLabel, back, Accent, size: 30, font: WorkshopArt.WorkshopFont);
        WorkshopUi.Fill(panel, "Divider", new Rect(100, 171, 1325, 2), Border);
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
        BuildPanel("缝素堂 · 选衣样", "选择一件衣样，使用已染好的布料缝制。", "返回任务", Close);
        WorkshopUi.Fill(panel, "PreviewPanel", new Rect(850, 200, 585, 548), BoardPaper);
        WorkshopUi.Fill(panel, "FabricSwatch", new Rect(895, 236, 26, 26), fabricTint);
        Label(panel, "FabricColor", new Rect(935, 223, 440, 51), DyeColorUtil.ChineseName(dye) + "染布", 28);

        var previewClip = WorkshopWardrobe.Clip(panel, "Garment Preview Cutout");
        var preview = WorkshopUi.Picture(previewClip, "GarmentPreview", new Rect(), null, stretch: true);
        var previewName = Label(panel, "GarmentName", new Rect(890, 656, 500, 53), "", 35,
            Accent, TextAnchor.MiddleCenter);
        var next = WorkshopUi.Button(panel, "ConfirmStyle", new Rect(973, 784, 393, 72),
            "确认款式 · 开始缝制", BuildSewing, Accent, enabled: false, size: 32, font: WorkshopArt.WorkshopFont);

        var optionBorders = new UnityEngine.UI.Image[available.Count];
        var buttons = new UnityEngine.UI.Button[available.Count];
        for (int i = 0; i < available.Count; i++)
        {
            int index = i;
            GarmentStyle style = available[i];
            var bounds = new Rect(83 + 376 * (i % 2), 200 + 272 * (i / 2), 348, 250);
            var border = WorkshopUi.Fill(panel, "Option-" + style.Id, bounds, Border);
            var card = WorkshopUi.Fill(border.transform, "Card", new Rect(3, 3, 342, 244), PatternPaper);
            card.raycastTarget = true;
            var button = card.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = card;
            var clip = WorkshopWardrobe.Clip(card.transform, "Garment Cutout");
            var image = WorkshopUi.Picture(clip, "Garment", new Rect(), null, stretch: true);
            WorkshopWardrobe.PlaceInBox(clip, image, style.Id, new Rect(50, 12, 242, 181));
            Label(card.transform, "Style", new Rect(16, 192, 310, 42), style.Name, 28,
                anchor: TextAnchor.MiddleCenter);
            optionBorders[i] = border;
            buttons[i] = button;
            button.onClick.AddListener(() =>
            {
                selected = style;
                WorkshopWardrobe.PlaceInBox(previewClip, preview, style.Id, new Rect(950, 276, 380, 362));
                previewName.text = style.Name;
                next.interactable = true;
                for (int j = 0; j < optionBorders.Length; j++)
                    optionBorders[j].color = j == index ? Accent : Border;
            });
        }

        int current = available.IndexOf(selected);
        buttons[Mathf.Max(0, current)].onClick.Invoke();
    }

    private void BuildSewing()
    {
        BuildPanel("缝素堂 · 拼合衣片", "把衣片拖到对应的样板上，三片归位后即可缝制。", "重新选款", BuildChooser);
        submitting = false;
        piecesPlaced = 0;
        WorkshopUi.Fill(panel, "Tray", new Rect(83, 196, 328, 563), BoardPaper);
        WorkshopUi.Fill(panel, "Workbench", new Rect(435, 196, 1000, 563), BoardPaper);
        Label(panel, "TrayTitle", new Rect(112, 208, 265, 45), "已裁衣片", 28, Accent);
        Label(panel, "PatternTitle", new Rect(473, 208, 620, 45), selected.Name + "样板", 31, Accent);

        var targetCells = new[]
        {
            new Rect(943, 266, 320, 220),
            new Rect(595, 266, 320, 220),
            new Rect(756, 503, 345, 220)
        };
        var sourceCells = new[]
        {
            new Rect(113, 256, 268, 150),
            new Rect(113, 423, 268, 150),
            new Rect(113, 590, 268, 150)
        };

        sewingStatus = Label(panel, "Progress", new Rect(85, 787, 645, 68), "衣片归位 0 / 3", 30, Accent);
        WorkshopUi.Button(panel, "ResetPieces", new Rect(750, 786, 258, 68), "重新摆放", BuildSewing, Accent, size: 30, font: WorkshopArt.WorkshopFont);
        sewButton = WorkshopUi.Button(panel, "FinishGarment", new Rect(1033, 786, 403, 68), "缝制成衣", Finish,
            Accent, enabled: false, size: 32, font: WorkshopArt.WorkshopFont);

        for (int i = 0; i < PieceNames.Length; i++)
        {
            Rect cell = targetCells[i];
            WorkshopUi.Fill(panel, PieceNames[i] + "-Pattern", cell, PatternPaper);
            Rect targetBounds = new Rect(cell.x + 24, cell.y + 8, cell.width - 48, cell.height - 60);
            var target = WorkshopUi.Picture(panel, PieceNames[i] + "-Target", targetBounds, pieceSprites[i],
                new Color(0.69f, 0.58f, 0.56f, 0.38f));
            Label(panel, PieceNames[i] + "-TargetLabel",
                new Rect(cell.x, cell.yMax - 47, cell.width, 39), PieceNames[i], 25, Accent, TextAnchor.MiddleCenter);

            cell = sourceCells[i];
            WorkshopUi.Fill(panel, PieceNames[i] + "-Tray", cell, PatternPaper);
            Label(panel, PieceNames[i] + "-TrayLabel",
                new Rect(cell.x, cell.yMax - 38, cell.width, 32), PieceNames[i], 24, Accent, TextAnchor.MiddleCenter);
            var piece = WorkshopUi.Picture(panel, PieceNames[i] + "-Piece",
                new Rect(cell.x + 26, cell.y + 4, cell.width - 52, cell.height - 46), pieceSprites[i], fabricTint);
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
            sewingStatus.text = $"衣片归位 {piecesPlaced} / {PieceNames.Length}";
            sewButton.interactable = piecesPlaced == PieceNames.Length;
        }
        else
        {
            sewingStatus.text = $"衣片归位 {piecesPlaced} / {PieceNames.Length}   请拖到对应样板";
        }
    }

    private void Finish()
    {
        if (submitting || piecesPlaced != PieceNames.Length) return;
        submitting = true;
        sewButton.interactable = false;
        if (!WorkshopProgress.CompleteWinter(selected.Id))
        {
            SimplePopup.Show("制作成衣", "任务材料或布色已变更，请返回任务面板重新选择。", "返回任务", Close);
            return;
        }

        var finished = onClose;
        CloseFrame();
        WorkshopWardrobe.Show(finished);
    }

    private void Close()
    {
        if (active != this) return;
        CloseFrame();
        onClose?.Invoke();
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
