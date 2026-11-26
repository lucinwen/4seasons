using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JiangnanDye;
using Qingming.LevelBackground;

/// <summary>秋季染色台：选染料，将一块素绸拖入染缸，染好后收下。</summary>
public class WinterDyeingOverlay : MonoBehaviour
{
    public static bool IsActive { get; private set; }
    private static WinterDyeingOverlay _runner;
    private static readonly Rect VatBounds = new Rect(714, 335, 506, 470);
    private static readonly Rect SilkBounds = new Rect(1380, 560, 338, 250);

    private WinterDyeingConfig _config;
    private RectTransform _frame;
    private RectTransform _vatDropArea;
    private Image _vatImage;
    private Image _silkImage;
    private Outline _vatHighlight;
    private Text _statusText;
    private Text _silkLabel;
    private Button _collectButton;
    private readonly Dictionary<DyeColor, Outline> _dyeSelections = new Dictionary<DyeColor, Outline>();
    private readonly List<Button> _dyeButtons = new List<Button>();
    private DyeColor? _selectedColor;
    private bool _dyeing;
    private bool _finished;
    private float _priorTimeScale;
    private bool _timeCaptured;
    private LevelPlayerController _player;
    private bool _playerWasEnabled;

    public bool CanDragSilk => _selectedColor.HasValue && !_dyeing && !_finished && !WorkshopMinigameMenu.IsPaused;
    internal bool IsDyeing => _dyeing;
    internal bool HasFinishedCloth => _finished;

    public static void Show()
    {
        if (IsActive) return;
        WinterGrindingOverlay.Close();
        var config = Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
        if (config == null || config.clearWaterVat == null || config.silk == null)
        {
            SimplePopup.Show("染色", "染色界面素材缺失，请检查 Resources/WinterDyeingConfig。", "知道了");
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
        IsActive = true;
        FreezePlayer();
        WorkshopUi.Block(frame, Color.black);
        WorkshopUi.Picture(frame, "Autumn Dye Workbench", new Rect(0, 0, 1920, 1080),
            Resources.Load<Sprite>("WordRevision/AutumnDyeWorkbench"), stretch: true);
        _statusText = TableLabel(frame, "Dye Hint", new Rect(60, 48, 1300, 55),
            "选染料，把素绸拖入染缸。", 29, TextAnchor.MiddleLeft);
        WorkshopUi.PixelButton(frame, "CloseButton", new Rect(1720, 45, 150, 58),
            "返回", CloseInternal, true, 26);
        CreateDyeList();
        CreateVat();
        CreateSilk();
        _collectButton = WorkshopUi.PixelButton(frame, "Collect Dyed Cloth", new Rect(1435, 875, 230, 62),
            "收好染布", CollectFinishedCloth, true, 27);
        _collectButton.transform.parent.gameObject.SetActive(false);
    }

    private static Text TableLabel(Transform parent, string name, Rect bounds, string text, int size,
        TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var label = WorkshopUi.Label(parent, name, bounds, text, size, WorkshopUi.Cream, alignment);
        var shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(48, 28, 17, 230);
        shadow.effectDistance = new Vector2(2, -2);
        return label;
    }

    private void CreateDyeList()
    {
        var colors = new List<DyeColor>(WinterLevelState.ProducedDyeColors);
        colors.Sort();
        var grinding = Resources.Load<WinterGrindingConfig>("WinterGrindingConfig");
        int firstRow = Mathf.Min(colors.Count, 3);
        for (int i = 0; i < colors.Count; i++)
        {
            DyeColor color = colors[i];
            bool top = i < firstRow;
            int column = top ? i : i - firstRow;
            int columns = top ? firstRow : colors.Count - firstRow;
            float x = 390f + (column - (columns - 1) * .5f) * 185f - 87f;
            var button = WorkshopUi.Button(_frame, "Dye_" + color,
                new Rect(x, top ? 560 : 755, 174, 160), "", () => SelectColor(color), Color.clear);
            var icon = grinding != null ? grinding.GetDye(color) : null;
            var bowl = WorkshopUi.Picture(button.transform, "Dye Bowl", new Rect(0, 0, 174, 122),
                icon ?? _config.GetVat(color) ?? _config.clearWaterVat);
            bowl.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -5);
            _dyeSelections[color] = WinterWorktableUi.FocusOutline(bowl);
            TableLabel(button.transform, "Dye Name", new Rect(0, 124, 174, 34), DyeColorUtil.ChineseName(color), 24);
            button.targetGraphic = bowl;
            _dyeButtons.Add(button);
        }
        if (colors.Count == 0) SetStatus("先把采来的染材研磨成染料。");
    }

    private void CreateVat()
    {
        _vatImage = WorkshopUi.Picture(_frame, "Vat", VatBounds, _config.clearWaterVat);
        _vatImage.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6, -7);
        _vatHighlight = WinterWorktableUi.FocusOutline(_vatImage);
        _vatDropArea = WorkshopUi.Rect(_frame, "VatDropArea", new Rect(770, 385, 390, 180));
    }

    private void CreateSilk()
    {
        _silkImage = WorkshopUi.Picture(_frame, "Silk", SilkBounds, _config.silk);
        _silkImage.raycastTarget = true;
        _silkImage.gameObject.AddComponent<WinterDyeingSilk>().Init(this);
        _silkLabel = TableLabel(_frame, "Silk Name", new Rect(1420, 810, 260, 42), "素绸", 25);
    }

    private void SelectColor(DyeColor color)
    {
        if (_dyeing || _finished) return;
        _selectedColor = color;
        _vatImage.sprite = _config.GetVat(color) ?? _config.clearWaterVat;
        foreach (var selection in _dyeSelections) selection.Value.enabled = selection.Key == color;
        SetStatus("把右侧素绸拖入染缸，染成" + DyeColorUtil.ChineseName(color) + "。");
    }

    public bool IsOverVat(Vector2 screenPosition)
    {
        return _vatDropArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_vatDropArea, screenPosition, null);
    }

    internal void HighlightDropTarget(bool highlighted)
    {
        if (_vatHighlight != null) _vatHighlight.enabled = highlighted && CanDragSilk;
    }

    public void SetStatus(string text)
    {
        if (_statusText != null) _statusText.text = text;
    }

    public bool TryDyeCloth()
    {
        if (!CanDragSilk || !WorkshopProgress.Has(WorkshopItem.RawSilkCloth)) return false;
        _dyeing = true;
        _silkImage.raycastTarget = false;
        _silkLabel.text = "";
        foreach (var button in _dyeButtons) button.interactable = false;
        SetStatus("素绸正在上色……");
        WorkshopAudioFeedback.DyeDip();
        StartCoroutine(DyeOneCloth(_selectedColor.Value));
        return true;
    }

    private IEnumerator DyeOneCloth(DyeColor color)
    {
        var rect = _silkImage.rectTransform;
        Vector2 from = rect.anchoredPosition;
        Vector2 dip = new Vector2(796, -370);
        Vector2 home = new Vector2(SilkBounds.x, -SilkBounds.y);
        Color dyedTint = _config.GetClothTint(color);
        float elapsed = 0f;
        const float duration = 2.2f;
        while (elapsed < duration)
        {
            if (WorkshopMinigameMenu.IsPaused) { yield return null; continue; }
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float enter = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .24f));
            float lift = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .67f) / .33f));
            rect.anchoredPosition = Vector2.Lerp(Vector2.Lerp(from, dip, enter), home, lift);
            rect.localScale = Vector3.one * Mathf.Lerp(Mathf.Lerp(1, .65f, enter), 1, lift);
            _silkImage.color = Color.Lerp(Color.white, dyedTint, Mathf.Clamp01((t - .18f) / .48f));
            yield return null;
        }
        rect.anchoredPosition = home;
        rect.localScale = Vector3.one;
        var dedicated = _config.GetCloth(color);
        _silkImage.sprite = dedicated != null ? dedicated : _config.silk;
        _silkImage.color = dedicated != null ? Color.white : dyedTint;
        _silkImage.raycastTarget = true;
        _silkLabel.text = DyeColorUtil.ChineseName(color) + "布";
        WinterLevelState.SetDyedCloth(_silkLabel.text);
        WorkshopProgress.RecordDyedCloth(color);
        _dyeing = false;
        _finished = true;
        _collectButton.transform.parent.gameObject.SetActive(true);
        SetStatus("染好了，收好这块布。");
    }

    internal void CollectFinishedCloth()
    {
        if (!_finished || WorkshopMinigameMenu.IsPaused) return;
        CloseInternal();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !WorkshopMinigameMenu.IsPaused) CloseInternal();
    }

    public static void CloseAny()
    {
        if (_runner != null) _runner.CloseInternal();
    }

    private void CloseInternal()
    {
        if (_runner != this) return;
        StopAllCoroutines();
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
        _priorTimeScale = Time.timeScale;
        _timeCaptured = true;
        Time.timeScale = 0f;
        var playerGo = GameObject.Find("Player");
        if (playerGo == null) return;
        _player = playerGo.GetComponent<LevelPlayerController>();
        if (_player != null) { _playerWasEnabled = _player.enabled; _player.enabled = false; }
        var rb = playerGo.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }

    private void RestorePlayer()
    {
        if (_timeCaptured) { Time.timeScale = _priorTimeScale; _timeCaptured = false; }
        if (_player != null) { _player.enabled = _playerWasEnabled; _player = null; }
    }
}
