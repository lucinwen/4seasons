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
    private UnityEngine.UI.Text sewingStatus;
    private UnityEngine.UI.Button sewButton;

    internal static bool IsOpen => active != null;

    internal static void Show(Action back, Action completed)
    {
        if (active != null) return;
        if (!WorkshopProgress.CanSubmit(WorkshopSeason.Winter))
        {
            if (SimplePopup.Show("制作成衣", "先剪出衣片并拖纸样确认版型，再备好染布和缝衣针，就可以选款缝衣。",
                    "返回", back) == null)
                back?.Invoke();
            return;
        }

        if (!TryResolveDye(WorkshopProgress.DyedColor, out DyeColor dye))
        {
            if (SimplePopup.Show("制作成衣", "请先在秋季青染轩确认染布颜色，再回来选款。",
                    "返回", back) == null)
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
            if (SimplePopup.Show("制作成衣", "衣片或衣样素材无法读取。", "返回", Close) == null)
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

        panel = WorkshopUi.Rect(frame, title, new Rect(0, 0, 1920, 1080));
        WorkshopImmersive.Background(panel,WorkshopSeason.Winter);
        WorkshopImmersive.Text(panel,"Prompt",new Rect(45,35,1450,62),prompt);
        if(back!=null)WorkshopImmersive.Button(panel,"Back",new Rect(1680,35,195,62),backLabel,back);
    }

    private static UnityEngine.UI.Text Label(Transform parent, string name, Rect bounds, string text, int size,
        Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var label = WorkshopUi.Label(parent, name, bounds, text, size, color, anchor, WorkshopArt.WorkshopFont);
        label.verticalOverflow = VerticalWrapMode.Overflow;
        WorkshopImmersive.QuietText(label);
        return label;
    }

    private void BuildChooser()
    {
        BuildPanel("选衣样","挑一件喜欢的衣样。","返回工位",Close);
        var previewClip=WorkshopWardrobe.Clip(panel,"Garment Preview Cutout");
        var preview=WorkshopUi.Picture(previewClip,"GarmentPreview",new Rect(),null,stretch:true);
        var previewName=WorkshopImmersive.Text(panel,"GarmentName",new Rect(1180,880,570,48),"",30,TextAnchor.MiddleCenter);
        var next=WorkshopImmersive.Button(panel,"ConfirmStyle",new Rect(1280,955,370,64),"开始缝制",BuildSewing);
        next.interactable=false;
        var buttons=new UnityEngine.UI.Button[available.Count];
        var marks=new UnityEngine.UI.Text[available.Count];
        for(int i=0;i<available.Count;i++)
        {
            int index=i;var style=available[i];
            float x=115+(i%3)*320,y=290+(i/3)*330;
            var button=WorkshopUi.Button(panel,"Option-"+style.Id,new Rect(x,y,270,300),"",null,new Color(0,0,0,.002f));
            var clip=WorkshopWardrobe.Clip(button.transform,"Garment Cutout");
            var image=WorkshopUi.Picture(clip,"Garment",new Rect(),null,stretch:true);
            WorkshopWardrobe.PlaceInBox(clip,image,style.Id,new Rect(10,0,250,245));
            marks[i]=WorkshopImmersive.Text(button.transform,"Style",new Rect(0,250,270,46),style.Name,24,TextAnchor.MiddleCenter);
            buttons[i]=button;
            button.onClick.AddListener(()=>
            {
                selected=style;next.interactable=true;
                WorkshopWardrobe.PlaceInBox(previewClip,preview,style.Id,new Rect(1170,270,600,590));
                previewName.text=style.Name;
                for(int j=0;j<marks.Length;j++)if(marks[j]!=null)marks[j].color=j==index?new Color32(255,218,132,255):WorkshopUi.Cream;
            });
        }
        buttons[Mathf.Max(0,available.IndexOf(selected))].onClick.Invoke();
    }

    private void BuildSewing()
    {
        BuildPanel("缝素堂 · 拼合衣片", "把三块衣片拼合在布面上。",
            "重新选款", BuildChooser);
        submitting = false;
        piecesPlaced = 0;
        WorkshopUi.Picture(panel,"Workbench",new Rect(575,400,900,470),Resources.Load<Sprite>("WorkshopGameArt/CuttingCloth"),fabricTint,true);
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

        var progress = WorkshopUi.Fill(panel, "Sewing Progress Plaque", new Rect(682, 789, 546, 46), Color.clear);
        sewingStatus = Label(progress.transform, "Progress", new Rect(12, 0, 522, 46),
            "衣片归位 0 / 3", 25, progress.sprite == null ? Accent : WorkshopUi.Cream,
            TextAnchor.MiddleCenter);
        WorkshopImmersive.Button(panel, "ResetPieces", new Rect(684, 900, 215, 59), "重新摆放", BuildSewing);
        sewButton = WorkshopImmersive.Button(panel, "FinishGarment", new Rect(927, 900, 360, 59), "缝制成衣", Finish);
        sewButton.interactable = false;

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
            // Loose cut pieces rest directly on the worktable.
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

            sewButton.interactable = true;
            sewingStatus.text = "三片已归位 · 点击缝制成衣";
        }
        else
        {
            WorkshopAudioFeedback.Notice();
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
            if (SimplePopup.Show("制作成衣", "任务材料或布色已变更，请返回重新选择。",
                    "返回", Close) == null)
                Close();
            return;
        }

        var finished = onCompleted;
        CloseFrame();
        WorkshopNarrativeView.ShowFinale(() => WorkshopWardrobe.Show(finished));
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
