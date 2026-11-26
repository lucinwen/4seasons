using UnityEngine;
using UnityEngine.UI;
using Qingming.LevelBackground;

/// <summary>
/// 冬季研磨台。将原料放入钵体，再拖动研磨棒研磨。
/// </summary>
public class WinterGrindingOverlay : MonoBehaviour
{
    public static bool IsActive { get; private set; }

    private const float GrindDistanceRequired = 1600f;
    private const float ProgressWidth = 400f;
    internal const string PlaceMaterialHint = "把左侧的原料拖到钵口。";
    internal const string UseRodHint = "把研磨棒拖到钵口，来回滑动。";

    private static WinterGrindingOverlay _runner;

    private WinterGrindingConfig _config;
    private RectTransform _frame;
    private RectTransform _mortarDropArea;
    private Outline _mortarHighlight;
    private Image _groundDye;
    private Image _rawInBowl;
    private Text _statusText;
    private Text _progressLabel;
    private RectTransform _progressFill;
    private WinterGrindingRod _rod;

    private WinterGrindingRawCard _selectedCard;
    private WinterDyeMaterial _selectedMaterial;
    private float _grindProgress;
    private bool _grindingComplete;

    private LevelPlayerController _player;
    private bool _playerWasEnabled;

    public bool HasMaterial
    {
        get { return _selectedMaterial != null && !_grindingComplete; }
    }

    public bool CanAcceptMaterial
    {
        get { return _selectedMaterial == null && !_grindingComplete; }
    }

    public static void Show()
    {
        if (IsActive) return;

        // 同一时间只保留一个界面，避免染色界面残留在研磨画面上
        WinterDyeingOverlay.CloseAny();

        WinterLevelState.RefreshDyes();
        if (WinterLevelState.CollectedDyes.Count == 0)
        {
            // 区分两种情况：染材都已研磨过（直接去染色），或还没有采集染材
            string message = WinterLevelState.ProducedDyeColors.Count > 0
                ? "已采集的染材都研磨过了，可以直接到清水染缸选色染色。"
                : "先进入染料园采集染材，再回来研磨。";
            SimplePopup.Show("没有待研磨的染材", message, "知道了", null);
            return;
        }

        var config = Resources.Load<WinterGrindingConfig>("WinterGrindingConfig");
        if (config == null || config.mortar == null || config.rod == null)
        {
            SimplePopup.Show("研磨", "研磨界面素材缺失，请检查 Resources/WinterGrindingConfig。", "知道了", null);
            return;
        }

        var frame = WorkshopUi.NewCanvas("WinterGrindingOverlay", 600);
        _runner = frame.parent.gameObject.AddComponent<WinterGrindingOverlay>();
        _runner.Build(frame, config);
    }

    public static void Close()
    {
        if (_runner != null) _runner.CloseInternal();
    }

    private void Build(RectTransform frame, WinterGrindingConfig config)
    {
        _frame = frame;
        _config = config;
        IsActive = true;
        FreezePlayer();

        WinterWorktableUi.Build(_frame, "研磨染料", 244, CloseInternal);
        CreateRawMaterialList();
        CreateMortar();
        CreateRod();
        CreateProgress();
        var footer = WinterWorktableUi.Footer(_frame);
        _statusText = WorkshopUi.Label(footer, "Worktable Status", new Rect(25, 4, 1776, 71),
            PlaceMaterialHint, 29, WorkshopUi.Cream);
    }

    private void CreateRawMaterialList()
    {
        var materials = WinterLevelState.CollectedDyes;
        WinterWorktableUi.Nameplate(_frame, "Raw Materials Heading", new Rect(274, 477, 226, 41), "待研磨原料", 24);
        for (int i = 0; i < materials.Count; i++)
        {
            var material = materials[i];
            var slot = WorkshopUi.Fill(_frame, "Raw_" + material.Color,
                WinterWorktableUi.MaterialBounds(i, materials.Count), Color.clear);
            slot.raycastTarget = true;
            var icon = WorkshopUi.Picture(slot.transform, "Raw Ingredient", new Rect(11, 0, 152, 120), material.Icon);
            icon.gameObject.AddComponent<UnityEngine.UI.Shadow>().effectDistance = new Vector2(4, -5);
            WinterWorktableUi.Nameplate(slot.transform, "Ingredient Name", new Rect(8, 124, 158, 34), material.Name, 22);
            var card = slot.gameObject.AddComponent<WinterGrindingRawCard>();
            card.Init(this, material);
        }
    }

    private void CreateMortar()
    {
        var bounds = new Rect(714, 423, 492, 384);
        var mortar = WorkshopUi.Picture(_frame, "Mortar", bounds, _config.mortar, stretch: true);
        mortar.gameObject.AddComponent<UnityEngine.UI.Shadow>().effectDistance = new Vector2(6, -7);
        _mortarHighlight = WinterWorktableUi.FocusOutline(mortar);
        _mortarDropArea = WorkshopUi.Rect(_frame, "MortarDropArea", new Rect(779, 463, 362, 142));
        _groundDye = WorkshopUi.Picture(_frame, "Ground Dye", bounds, null, Color.clear, stretch: true);
        _rawInBowl = WorkshopUi.Picture(_frame, "RawInBowl", new Rect(878, 465, 164, 123), null);
        _rawInBowl.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rawInBowl.rectTransform.anchoredPosition = new Vector2(960, -526.5f);
        WinterWorktableUi.Nameplate(_frame, "Mortar Label", new Rect(858, 362, 204, 46), "研磨钵", 25);
    }

    private void CreateRod()
    {
        var image = WorkshopUi.Picture(_frame, "Rod", new Rect(0, 0, 330, 330), _config.rod);
        image.raycastTarget = true;
        var rect = image.rectTransform;
        rect.pivot = new Vector2(0.30f, 0.20f); // The grindstone contacts the bowl under the pointer.
        rect.anchoredPosition = new Vector2(1450, -691);
        rect.localRotation = Quaternion.Euler(0f, 0f, -25f);
        image.gameObject.AddComponent<UnityEngine.UI.Shadow>().effectDistance = new Vector2(4, -5);
        _rod = image.gameObject.AddComponent<WinterGrindingRod>();
        _rod.Init(this);
        WinterWorktableUi.Nameplate(_frame, "Rod Label", new Rect(1487, 812, 132, 40), "研磨棒", 24);
    }

    private void CreateProgress()
    {
        _progressLabel = WinterWorktableUi.Nameplate(_frame, "Grinding Progress Plate", new Rect(740, 830, 440, 62),
            "研磨进度 0%", 25);
        _progressLabel.name = "Grinding Progress Label";
        _progressLabel.rectTransform.sizeDelta = new Vector2(440, 44);
        var track = WorkshopUi.Fill(_progressLabel.transform.parent, "Grinding Track", new Rect(20, 46, ProgressWidth, 7),
            WorkshopUi.Faded);
        _progressFill = WorkshopUi.Fill(track.transform, "Grinding Progress", new Rect(0, 0, 0, 7),
            WinterWorktableUi.FocusGold).rectTransform;
    }

    public bool IsOverMortar(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(_mortarDropArea, screenPosition, null);
    }

    internal bool TryGetMortarPoint(Vector2 screenPosition, Camera eventCamera, out Vector2 position)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_mortarDropArea, screenPosition,
            eventCamera, out position) && _mortarDropArea.rect.Contains(position);
    }

    internal void HighlightMortar(bool highlighted)
    {
        _mortarHighlight.enabled = highlighted;
    }

    /// <summary>
    /// 创建一个跟随鼠标的透明拖拽图标：只显示染料图片、无底板，
    /// 并放在画布最后一个子节点，保证层级高于钵体。
    /// </summary>
    public RectTransform CreateDragIcon(Sprite sprite)
    {
        var icon = WorkshopUi.Picture(_frame, "RawDragIcon", new Rect(0, 0, 128, 128), sprite);
        icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return icon.rectTransform;
    }

    public void SetStatus(string text)
    {
        if (_statusText != null) _statusText.text = text;
    }

    public bool AcceptMaterial(WinterGrindingRawCard card)
    {
        if (!CanAcceptMaterial || card == null || card.Material == null) return false;

        _selectedCard = card;
        _selectedMaterial = card.Material;
        card.gameObject.SetActive(false);

        _rawInBowl.sprite = _selectedMaterial.Icon;
        _rawInBowl.enabled = _rawInBowl.sprite != null;
        _groundDye.sprite = _config.GetDye(_selectedMaterial.Color);
        _groundDye.enabled = _groundDye.sprite != null;
        WorkshopAudioFeedback.DyeIngredient();
        SetStatus(UseRodHint);
        return true;
    }

    public void AddGrind(float localDistance)
    {
        if (!HasMaterial) return;

        _grindProgress += localDistance;
        ShowProgress(Mathf.Clamp01(_grindProgress / GrindDistanceRequired));
        if (_grindProgress >= GrindDistanceRequired) CompleteGrinding();
    }

    private void ShowProgress(float completion)
    {
        int percent = Mathf.FloorToInt(completion * 100f);
        _progressLabel.text = "研磨进度 " + percent + "%";
        _progressFill.sizeDelta = new Vector2(ProgressWidth * completion, _progressFill.sizeDelta.y);
        _rawInBowl.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, completion);
        _rawInBowl.color = new Color(1f, 1f, 1f, 1f - completion);
        _groundDye.color = new Color(1f, 1f, 1f, completion);
    }

    private void CompleteGrinding()
    {
        if (_grindingComplete || _selectedMaterial == null) return;

        _grindingComplete = true;
        var material = _selectedMaterial;
        var product = _config.GetDye(material.Color);

        bool hasGroundDye = WorkshopProgress.HasStep("winter.ground");
        bool producedDye = WinterLevelState.MarkDyeProduced(material.Color);

        HighlightMortar(false);
        _statusText.text = "研磨完成！获得【" + material.Name + "】";
        SimplePopup.Show("研磨完成", "获得【" + material.Name + "】", "收下", () =>
        {
            ResetBowl();
            if (!hasGroundDye && producedDye)
                WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.WinterGrindingComplete);
        }, false, "取消", null, product);
    }

    private void ResetBowl()
    {
        if (_selectedCard != null) Destroy(_selectedCard.gameObject);
        _selectedCard = null;
        _selectedMaterial = null;
        _grindProgress = 0f;
        _grindingComplete = false;
        _rawInBowl.enabled = false;
        _groundDye.enabled = false;
        ShowProgress(0f);
        _rod.ResetPosition();

        if (WinterLevelState.CollectedDyes.Count == 0)
        {
            _statusText.text = "全部染料都研磨完成了";
            Invoke(nameof(CloseInternal), 0.8f);
        }
        else
        {
            _statusText.text = PlaceMaterialHint;
        }
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
