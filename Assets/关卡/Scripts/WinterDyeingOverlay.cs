using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JiangnanDye;
using Qingming.LevelBackground;

/// <summary>
/// 冬季染色台。选定已研磨的染料后，将素绸拖进染缸。
/// </summary>
public class WinterDyeingOverlay : MonoBehaviour
{
    public static bool IsActive { get; private set; }

    private static WinterDyeingOverlay _runner;
    private static readonly Color ConfirmDisabled = new Color32(106, 112, 94, 255);
    private static readonly Rect VatBounds = new Rect(714, 311, 506, 480);
    private static readonly Rect SilkBounds = new Rect(1384, 511, 338, 250);

    private WinterDyeingConfig _config;
    private WinterGrindingConfig _grindingConfig;
    private RectTransform _frame;
    private RectTransform _vatDropArea;
    private Image _vatImage;
    private Outline _vatHighlight;
    private Text _vatLabel;
    private Text _silkLabel;
    private Text _statusText;
    private Button _confirmButton;
    private Text _confirmLabel;
    private GameObject _silkGo;

    private readonly Dictionary<DyeColor, Outline> _dyeSelections = new Dictionary<DyeColor, Outline>();
    private DyeColor? _selectedColor;
    private bool _colorConfirmed;
    private bool _dyeingComplete;

    private LevelPlayerController _player;
    private bool _playerWasEnabled;

    public bool CanDragSilk
    {
        get { return _selectedColor.HasValue && _colorConfirmed && !_dyeingComplete; }
    }

    public static void Show()
    {
        if (IsActive) return;

        // 同一时间只保留一个界面，避免研磨界面上残留染色的「关闭」按钮
        WinterGrindingOverlay.Close();

        var config = Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
        if (config == null || config.clearWaterVat == null || config.silk == null)
        {
            SimplePopup.Show("染色", "染色界面素材缺失，请检查 Resources/WinterDyeingConfig。", "知道了", null);
            return;
        }

        var frame = WorkshopUi.NewCanvas("WinterDyeingOverlay", 600);
        _runner = frame.parent.gameObject.AddComponent<WinterDyeingOverlay>();
        _runner.Build(frame, config);
    }

    private void Build(RectTransform frame, WinterDyeingConfig config)
    {
        _frame = frame;
        _config = config;
        _grindingConfig = Resources.Load<WinterGrindingConfig>("WinterGrindingConfig");
        IsActive = true;
        FreezePlayer();

        WinterWorktableUi.Build(_frame, "染色", 186, CloseInternal);

        CreateDyeList();
        CreateVat();
        CreateSilk();

        var footer = WinterWorktableUi.Footer(_frame);
        _statusText = WorkshopUi.Label(footer, "Worktable Status", new Rect(25, 4, 1470, 71),
            "选择左侧已研磨的染料。", 29, WorkshopUi.Cream);
        CreateConfirmButton(footer);
    }

    private void CreateDyeList()
    {
        var colors = new List<DyeColor>(WinterLevelState.ProducedDyeColors);
        colors.Sort();
        WinterWorktableUi.Nameplate(_frame, "Dyes Heading", new Rect(274, 477, 226, 41), "已研磨染料", 24);

        for (int i = 0; i < colors.Count; i++)
        {
            var color = colors[i];
            var icon = _grindingConfig != null ? _grindingConfig.GetDye(color) : null;
            icon = icon ?? _config.GetVat(color) ?? _config.clearWaterVat;

            var button = WorkshopUi.Button(_frame, "Dye_" + color, WinterWorktableUi.MaterialBounds(i, colors.Count),
                "", () => SelectColor(color), Color.clear);
            var bowl = WorkshopUi.Picture(button.transform, "Dye Bowl", new Rect(0, 0, 174, 122), icon);
            bowl.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -5);
            _dyeSelections[color] = WinterWorktableUi.FocusOutline(bowl);
            WinterWorktableUi.Nameplate(button.transform, "Dye Name", new Rect(34, 124, 106, 34),
                DyeColorUtil.ChineseName(color), 22);
            button.targetGraphic = bowl;
            var tones = button.colors;
            tones.normalColor = new Color32(240, 226, 206, 255);
            tones.highlightedColor = tones.selectedColor = Color.white;
            tones.pressedColor = new Color32(204, 183, 153, 255);
            tones.disabledColor = tones.normalColor;
            button.colors = tones;
        }

        if (colors.Count == 0)
            WinterWorktableUi.Nameplate(_frame, "Empty", new Rect(274, 600, 226, 52), "还没有染料", 24);
    }

    private void CreateVat()
    {
        _vatImage = WorkshopUi.Picture(_frame, "Vat", VatBounds, _config.clearWaterVat);
        _vatImage.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6, -7);
        _vatHighlight = WinterWorktableUi.FocusOutline(_vatImage);
        _vatDropArea = WorkshopUi.Rect(_frame, "VatDropArea", new Rect(774, 378, 390, 166));
        _vatLabel = WinterWorktableUi.Nameplate(_frame, "VatLabel", new Rect(852, 252, 230, 46), "清水缸", 25);
    }

    private void CreateConfirmButton(RectTransform footer)
    {
        _confirmButton = WorkshopUi.PixelButton(footer, "ConfirmButton", new Rect(1569, 10, 245, 61),
            "确定颜色", ConfirmColor, true, 27);
        _confirmLabel = _confirmButton.GetComponentInChildren<UnityEngine.UI.Text>();
        SetConfirmAvailable(false);
    }

    private void SetConfirmAvailable(bool available)
    {
        _confirmButton.interactable = available;
        _confirmButton.targetGraphic.color = available ? WorkshopUi.PixelInk : ConfirmDisabled;
    }

    private void CreateSilk()
    {
        var image = WorkshopUi.Picture(_frame, "Silk", SilkBounds, _config.silk);
        image.raycastTarget = true;
        _silkGo = image.gameObject;

        var silk = _silkGo.AddComponent<WinterDyeingSilk>();
        silk.Init(this);
        _silkLabel = WinterWorktableUi.Nameplate(_frame, "Silk Label", new Rect(1487, 812, 132, 40), "素绸", 24);
    }

    private void SelectColor(DyeColor color)
    {
        if (_colorConfirmed || _dyeingComplete) return;

        _selectedColor = color;
        _vatImage.sprite = _config.GetVat(color) ?? _config.clearWaterVat;
        _vatLabel.text = DyeColorUtil.ChineseName(color) + "染缸";

        foreach (var selection in _dyeSelections)
            selection.Value.enabled = selection.Key == color;

        SetConfirmAvailable(true);
        SetStatus("已选" + DyeColorUtil.ChineseName(color) + "。确定后，把素绸拖入缸口。");
    }

    private void ConfirmColor()
    {
        if (!_selectedColor.HasValue || _colorConfirmed) return;

        _colorConfirmed = true;
        SetConfirmAvailable(false);
        _confirmLabel.text = "已确定";
        foreach (var selection in _dyeSelections.Values)
            selection.GetComponentInParent<Button>().interactable = false;
        _vatLabel.text = "把素绸拖到这里";
        SetStatus("把右侧的素绸拖到缸口，松手染色。");
    }

    public bool IsOverVat(Vector2 screenPosition)
    {
        return _vatDropArea != null &&
               RectTransformUtility.RectangleContainsScreenPoint(_vatDropArea, screenPosition, null);
    }

    internal void HighlightDropTarget(bool highlighted)
    {
        if (_vatHighlight != null) _vatHighlight.enabled = highlighted;
    }

    public void SetStatus(string text)
    {
        if (_statusText != null) _statusText.text = text;
    }

    public bool TryDyeCloth()
    {
        if (!CanDragSilk) return false;

        _dyeingComplete = true;
        var color = _selectedColor.Value;
        var clothName = DyeColorUtil.ChineseName(color) + "布";

        var dedicated = _config.GetCloth(color);
        var clothSprite = dedicated != null ? dedicated : _config.silk;
        var tint = dedicated != null ? Color.white : _config.GetClothTint(color);

        if (_silkGo != null) _silkGo.SetActive(false);
        WorkshopUi.Picture(_frame, "DyedCloth", SilkBounds, clothSprite, tint);
        _vatLabel.text = DyeColorUtil.ChineseName(color) + "染缸";
        _silkLabel.text = clothName;

        _statusText.text = "染色完成！获得【" + clothName + "】";
        SimplePopup.ShowItem(WorkshopItem.DyedCloth, "获得【" + clothName + "】", () =>
        {
            WinterLevelState.SetDyedCloth(clothName);
            bool firstDyeing = WorkshopProgress.RecordDyedCloth(color);
            CloseInternal();
            if (firstDyeing) WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.WinterDyeingComplete);
        }, fallbackIcon: clothSprite, fallbackTint: tint, playSound: WorkshopAudioFeedback.DyeDip);
        return true;
    }

    /// <summary>关闭当前打开的染色界面（用于打开研磨界面前清理）。</summary>
    public static void CloseAny()
    {
        if (_runner != null) _runner.CloseInternal();
    }

    private void CloseInternal()
    {
        if (_runner != this) return;

        IsActive = false;
        RestorePlayer();
        _runner = null;
        WorkshopUi.CloseCanvas(_frame);
    }

    private void OnDestroy()
    {
        if (_runner != this) return;
        _runner = null;
        IsActive = false;
        RestorePlayer();
    }

    private void FreezePlayer()
    {
        var playerGo = GameObject.Find("Player");
        if (playerGo == null) return;

        _player = playerGo.GetComponent<LevelPlayerController>();
        if (_player != null)
        {
            _playerWasEnabled = _player.enabled;
            _player.enabled = false;
        }

        var rb = playerGo.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }

    private void RestorePlayer()
    {
        if (_player != null)
        {
            _player.enabled = _playerWasEnabled;
            _player = null;
        }
    }
}
