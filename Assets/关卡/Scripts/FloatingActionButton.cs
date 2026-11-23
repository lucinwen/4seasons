using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Qingming.LevelBackground
{
    /// <summary>
    /// Compact diegetic action tag shown above world objects. It uses the same
    /// ink, paper and brass language as the rest of the seasonal HUD.
    /// </summary>
    public class FloatingActionButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        public enum VisualStyle
        {
            InkPlaque,
            PaperTag
        }

        [Header("Button")]
        public string buttonText = "交互";
        public Vector3 offset = new Vector3(0f, 2.2f, 0f);
        public Vector2 buttonSize = new Vector2(248f, 58f);
        public int fontSize = 25;
        public VisualStyle visualStyle = VisualStyle.InkPlaque;

        [Header("Legacy colors")]
        public Color bgColor = new Color32(39, 76, 59, 255);
        public Color textColor = new Color32(255, 244, 211, 255);
        public Color disabledBgColor = new Color32(105, 101, 91, 255);
        public Color disabledTextColor = new Color32(220, 214, 190, 255);

        [Range(0f, 1f)]
        public float opacity = 1f;

        public System.Action onClick;
        public bool IsVisible => _visible;

        private static readonly Color Brass = new Color32(184, 143, 82, 255);
        private static readonly Color DeepBrass = new Color32(109, 77, 42, 255);
        private static readonly Color Paper = new Color32(245, 236, 217, 255);
        private static readonly Color Ink = new Color32(25, 51, 45, 255);
        private static readonly Color Jade = new Color32(29, 58, 51, 255);
        private static readonly Color Cream = new Color32(250, 244, 226, 255);
        private static readonly Color Shadow = new Color32(17, 27, 22, 119);
        private const float HoverOpacity = 0.20f;
        private const float MinimumPressTime = 0.055f;

        private GameObject _canvasGo;
        private RectTransform _buttonRt;
        private RectTransform _visualRoot;
        private CanvasGroup _canvasGroup;
        private Canvas _canvas;
        private Button _button;
        private Image _hitImage;
        private Image _shadowImage;
        private Image _outlineImage;
        private Image _surfaceImage;
        private UnityEngine.UI.Image _hoverOverlay;
        private Image _iconPlate;
        private Image _iconImage;
        private Image _chevron;
        private Text _label;
        private Text _caption;
        private RectTransform _captionRt;

        private bool _visible;
        private bool _interactable = true;
        private bool _hovered;
        private bool _pressed;
        private bool _selected;
        private float _hoverBlend;
        private float _pressBlend;
        private float _pressUntil;
        private float _reveal = 1f;
        private float _pulseTime = 1f;
        private static Sprite _talkIcon;
        private static Sprite _cardIcon;
        private static Sprite _actionIcon;
        private static Sprite _chevronIcon;

        private void Awake()
        {
            CreateButton();
            ApplyStyle();
            Hide();
        }

        private void CreateButton()
        {
            _canvasGo = new GameObject("Action Tag Canvas");
            _canvasGo.transform.SetParent(null, false);

            _canvas = _canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            _canvas.pixelPerfect = true;

            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _canvasGo.AddComponent<GraphicRaycaster>();

            var buttonGo = new GameObject("Action Tag", typeof(RectTransform), typeof(Image),
                typeof(FloatingActionTagButton));
            buttonGo.transform.SetParent(_canvasGo.transform, false);
            _buttonRt = buttonGo.GetComponent<RectTransform>();
            _buttonRt.anchorMin = _buttonRt.anchorMax = _buttonRt.pivot = new Vector2(0.5f, 0.5f);
            _buttonRt.sizeDelta = buttonSize;
            _canvasGroup = buttonGo.AddComponent<CanvasGroup>();

            _hitImage = buttonGo.GetComponent<Image>();
            _hitImage.color = new Color(1f, 1f, 1f, 0.002f);
            _hitImage.raycastTarget = true;
            _button = buttonGo.GetComponent<Button>();
            _button.targetGraphic = _hitImage;
            _button.transition = Selectable.Transition.None;
            _button.navigation = new Navigation { mode = Navigation.Mode.None };
            _button.onClick.AddListener(InvokeAction);

            var events = buttonGo.AddComponent<EventTrigger>();
            Listen(events, EventTriggerType.PointerEnter, e => OnPointerEnter((PointerEventData)e));
            Listen(events, EventTriggerType.PointerExit, e => OnPointerExit((PointerEventData)e));
            Listen(events, EventTriggerType.PointerDown, e => OnPointerDown((PointerEventData)e));
            Listen(events, EventTriggerType.PointerUp, e => OnPointerUp((PointerEventData)e));
            Listen(events, EventTriggerType.Select, OnSelect);
            Listen(events, EventTriggerType.Deselect, OnDeselect);

            _visualRoot = NewRect(buttonGo.transform, "Tag Visual", Vector2.zero, buttonSize);

            _shadowImage = NewImage(_visualRoot, "Tag Shadow", Vector2.zero, buttonSize);
            WorkshopUi.ApplyPixelShape(_shadowImage);
            _shadowImage.color = Shadow;
            StretchToParent(_shadowImage.rectTransform, -1f);

            _outlineImage = NewImage(_visualRoot, "Tag Edge", Vector2.zero, buttonSize);
            WorkshopUi.ApplyPixelShape(_outlineImage);
            StretchToParent(_outlineImage.rectTransform, 0f);

            _surfaceImage = NewImage(_visualRoot, "Tag Surface", Vector2.zero, buttonSize);
            WorkshopUi.ApplyPixelShape(_surfaceImage);
            StretchToParent(_surfaceImage.rectTransform, 1.5f);

            _hoverOverlay = NewImage(_visualRoot, "Tag Hover Overlay", Vector2.zero, buttonSize);
            WorkshopUi.ApplyPixelShape(_hoverOverlay);
            StretchToParent(_hoverOverlay.rectTransform, 1.5f);
            _hoverOverlay.color = new Color(1f, 1f, 1f, 0f);

            _iconPlate = NewImage(_visualRoot, "Icon Plate", new Vector2(34f - buttonSize.x * 0.5f, 0f),
                new Vector2(32f, 32f));
            WorkshopUi.ApplyPixelShape(_iconPlate);

            _iconImage = NewImage(_iconPlate.transform, "Action Icon", Vector2.zero, new Vector2(20f, 20f));
            _iconImage.preserveAspect = true;

            _label = NewText(_visualRoot, "Action Label", new Rect(14f, 0f, buttonSize.x - 92f, 50f), fontSize);
            _label.alignment = TextAnchor.MiddleLeft;

            _chevron = NewImage(_visualRoot, "Action Chevron", new Vector2(buttonSize.x * 0.5f - 24f, 0f),
                new Vector2(12f, 16f));
            _chevron.sprite = ChevronIcon;
            _chevron.preserveAspect = true;

            _captionRt = NewRect(_visualRoot, "Action Caption", new Vector2(14f, 13f),
                new Vector2(buttonSize.x - 92f, 17f));
            _caption = _captionRt.gameObject.AddComponent<Text>();
            _caption.font = WorkshopArt.WorkshopFont;
            _caption.fontSize = 17;
            _caption.alignment = TextAnchor.MiddleLeft;
            _caption.color = new Color32(209, 168, 90, 255);
            _caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            _caption.verticalOverflow = VerticalWrapMode.Overflow;
            _caption.raycastTarget = false;

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(null, false);
            }
        }

        private static void Listen(EventTrigger events, EventTriggerType type,
            UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            events.triggers.Add(entry);
        }

        private void Update()
        {
            if (_visualRoot == null) return;

            bool canHover = _visible && _interactable && (_hovered || _selected);
            bool pressing = _visible && _interactable && (_pressed || Time.unscaledTime < _pressUntil);
            _hoverBlend = Mathf.MoveTowards(_hoverBlend, canHover ? 1f : 0f,
                Time.unscaledDeltaTime * 9f);
            _pressBlend = Mathf.MoveTowards(_pressBlend, pressing ? 1f : 0f,
                Time.unscaledDeltaTime * (pressing ? 42f : 13f));
            _reveal = Mathf.MoveTowards(_reveal, _visible ? 1f : 0f, Time.unscaledDeltaTime * 8f);

            float revealEase = 1f - Mathf.Pow(1f - _reveal, 3f);
            float targetScale = Mathf.Lerp(0.94f, 1f, revealEase);
            targetScale *= 1f + 0.012f * _hoverBlend - 0.04f * _pressBlend;
            _visualRoot.localScale = Vector3.one * targetScale;
            _visualRoot.anchoredPosition = new Vector2(0f, _hoverBlend * 2f - _pressBlend * 5f);

            if (_canvasGroup != null)
                _canvasGroup.alpha = opacity * revealEase;

            _shadowImage.rectTransform.anchoredPosition = Vector2.Lerp(
                new Vector2(1f, -4f - _hoverBlend), new Vector2(0f, -1f), _pressBlend);
            Color shadow = Color.Lerp(Shadow, new Color32(20, 29, 23, 157), _hoverBlend);
            _shadowImage.color = Color.Lerp(shadow, new Color32(17, 27, 22, 90), _pressBlend);

            Color outline = visualStyle == VisualStyle.PaperTag
                ? Color.Lerp(DeepBrass, Brass, _hoverBlend)
                : Color.Lerp(Brass, new Color32(223, 182, 113, 255), _hoverBlend);
            if (!_interactable) outline = disabledBgColor;
            _outlineImage.color = outline;

            Color surface = visualStyle == VisualStyle.PaperTag ? Paper : Jade;
            surface = Color.Lerp(surface, new Color(surface.r * 0.88f, surface.g * 0.88f, surface.b * 0.88f, surface.a), _pressBlend);
            if (!_interactable) surface = disabledBgColor;
            _surfaceImage.color = surface;
            float hoverAlpha = HoverOpacity * _hoverBlend * Mathf.Lerp(1f, 0.4f, _pressBlend);
            _hoverOverlay.color = new Color(1f, 1f, 1f, hoverAlpha);

            Color foreground = visualStyle == VisualStyle.PaperTag ? Ink : Cream;
            if (!_interactable) foreground = disabledTextColor;
            _label.color = foreground;
            _iconPlate.color = _interactable
                ? (visualStyle == VisualStyle.PaperTag ? new Color32(230, 220, 195, 255) : new Color32(65, 91, 70, 255))
                : disabledTextColor;
            _iconImage.color = _interactable && visualStyle == VisualStyle.InkPlaque ? Cream : Ink;
            _caption.color = visualStyle == VisualStyle.PaperTag ? DeepBrass : new Color32(231, 196, 139, 255);
            _chevron.color = _interactable ? Brass : disabledTextColor;

            _chevron.rectTransform.anchoredPosition = new Vector2(buttonSize.x * 0.5f - 24f + _hoverBlend * 3f, 0f);

            _pulseTime = Mathf.Min(1f, _pulseTime + Time.unscaledDeltaTime / 0.28f);
            float pulse = 1f - _pulseTime;
            _iconImage.rectTransform.localScale = Vector3.one *
                Mathf.Lerp(1f, 1.14f, Mathf.Sin(pulse * Mathf.PI));
        }

        private void LateUpdate()
        {
            if (_canvasGo == null || _buttonRt == null) return;
            if (HarvestOverlayManager.IsHarvestActive)
            {
                if (_canvasGo.activeSelf) _canvasGo.SetActive(false);
                return;
            }

            if (!_visible)
            {
                if (_canvasGo.activeSelf) _canvasGo.SetActive(false);
                return;
            }

            if (!_canvasGo.activeSelf) _canvasGo.SetActive(true);
            var camera = Camera.main;
            if (camera == null) return;

            Vector3 screen = camera.WorldToScreenPoint(transform.position + offset);
            bool onScreen = screen.z >= 0f;
            if (_buttonRt.gameObject.activeSelf != onScreen)
                _buttonRt.gameObject.SetActive(onScreen);
            if (!onScreen) return;
            Vector2 halfSize = _buttonRt.rect.size * _canvas.scaleFactor * 0.5f;
            float x = Mathf.Clamp(screen.x, halfSize.x + 12f, Screen.width - halfSize.x - 12f);
            float y = Mathf.Clamp(screen.y, halfSize.y + 12f, Screen.height - halfSize.y - 12f);
            _buttonRt.position = new Vector3(x, y, 0f);
        }

        /// <summary>Backward-compatible configuration used by existing level triggers.</summary>
        public void Configure(string text, Vector3 buttonOffset, System.Action callback)
        {
            Configure(text, buttonOffset, callback,
                text.Contains("节气") ? VisualStyle.PaperTag : VisualStyle.InkPlaque);
        }

        public void Configure(string text, Vector3 buttonOffset, System.Action callback, VisualStyle style)
        {
            buttonText = text;
            offset = buttonOffset;
            onClick = callback;
            visualStyle = style;
            ApplyStyle();
        }

        public void Show()
        {
            if (HarvestOverlayManager.IsHarvestActive)
            {
                Hide();
                return;
            }

            bool wasVisible = _visible;
            _visible = true;
            if (_canvasGo != null) _canvasGo.SetActive(true);
            if (_buttonRt != null) _buttonRt.gameObject.SetActive(true);
            if (!wasVisible) _reveal = 0f;
        }

        public void Hide()
        {
            _visible = false;
            _hovered = false;
            _pressed = false;
            _selected = false;
            _pressUntil = 0f;
            _pressBlend = 0f;
            _hoverBlend = 0f;
            if (_canvasGo != null) _canvasGo.SetActive(false);
        }

        public void SetText(string text)
        {
            buttonText = text;
            ApplyStyle();
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (_button != null) _button.interactable = interactable;
            if (!interactable)
            {
                _hovered = false;
                _pressed = false;
                _pressUntil = 0f;
            }
        }

        public void SetOpacity(float alpha)
        {
            opacity = Mathf.Clamp01(alpha);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable) return;
            _hovered = true;
            WorkshopAudioFeedback.Hover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            _pressUntil = 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !_interactable) return;
            _pressed = true;
            _pressBlend = Mathf.Max(_pressBlend, 0.5f);
            _pressUntil = Time.unscaledTime + MinimumPressTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _pressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            _pressed = false;
            _pressUntil = 0f;
        }

        private void InvokeAction()
        {
            if (!_interactable) return;
            _pulseTime = 0f;
            WorkshopAudioFeedback.Click();
            onClick?.Invoke();
        }

        private void ApplyStyle()
        {
            if (_label == null) return;

            bool solarCard = buttonText.Contains("节气");
            _label.text = solarCard ? "节气卡" : buttonText;
            _label.fontSize = fontSize;
            _label.resizeTextMinSize = Mathf.Min(17, fontSize);
            _label.resizeTextMaxSize = fontSize;
            _iconImage.sprite = solarCard ? CardIcon : buttonText.Contains("对话") ? TalkIcon : ActionIcon;

            bool useCaption = solarCard && (buttonText.StartsWith("领取") || buttonText.StartsWith("读"));
            _caption.gameObject.SetActive(useCaption);
            _caption.text = buttonText.StartsWith("领取") ? "解锁" : "阅览";
            if (useCaption)
            {
                _label.rectTransform.anchoredPosition = new Vector2(14f, -8f);
                _label.rectTransform.sizeDelta = new Vector2(buttonSize.x - 92f, 30f);
                _captionRt.anchoredPosition = new Vector2(14f, 13f);
            }
            else
            {
                _label.rectTransform.anchoredPosition = new Vector2(14f, 0f);
                _label.rectTransform.sizeDelta = new Vector2(buttonSize.x - 92f, 50f);
            }
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            _selected = false;
            _pressUntil = 0f;
            _pressBlend = 0f;
            _hoverBlend = 0f;
        }

        private void OnDestroy()
        {
            if (_canvasGo != null) Destroy(_canvasGo);
        }

        private static RectTransform NewRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image NewImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = NewRect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Text NewText(Transform parent, string name, Rect bounds, int size)
        {
            var rect = NewRect(parent, name, new Vector2(14f, 0f), bounds.size);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(bounds.x, bounds.y);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = WorkshopArt.WorkshopFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.resizeTextForBestFit = true;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void StretchToParent(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static Sprite TalkIcon
        {
            get
            {
                if (_talkIcon != null) return _talkIcon;
                _talkIcon = CreateMaskSprite("Talk Icon", 24, delegate(int x, int y)
                {
                    bool bubble = x >= 3 && x <= 20 && y >= 5 && y <= 18;
                    bool tail = x >= 7 && x <= 11 && y >= 2 && y < 5 && x - 7 >= 5 - y;
                    bool dot = (x == 8 || x == 12 || x == 16) && y >= 11 && y <= 13;
                    return (bubble || tail) && !dot;
                });
                return _talkIcon;
            }
        }

        private static Sprite CardIcon
        {
            get
            {
                if (_cardIcon != null) return _cardIcon;
                _cardIcon = CreateMaskSprite("Solar Card Icon", 24, delegate(int x, int y)
                {
                    bool card = x >= 5 && x <= 18 && y >= 2 && y <= 21;
                    bool borderCut = x >= 7 && x <= 16 && y >= 5 && y <= 18;
                    bool sun = (x - 11) * (x - 11) + (y - 13) * (y - 13) <= 9;
                    bool titleLine = y <= 4 && x >= 8 && x <= 15;
                    return card && (!borderCut || sun || titleLine);
                });
                return _cardIcon;
            }
        }

        private static Sprite ActionIcon
        {
            get
            {
                if (_actionIcon != null) return _actionIcon;
                _actionIcon = CreateMaskSprite("Action Icon", 24, delegate(int x, int y)
                {
                    int cx = x - 11;
                    int cy = y - 11;
                    return Mathf.Abs(cx) + Mathf.Abs(cy) <= 8 &&
                        !(Mathf.Abs(cx) <= 2 && Mathf.Abs(cy) <= 2);
                });
                return _actionIcon;
            }
        }

        private static Sprite ChevronIcon
        {
            get
            {
                if (_chevronIcon != null) return _chevronIcon;
                _chevronIcon = CreateMaskSprite("Chevron Icon", 16, delegate(int x, int y)
                {
                    return (x - 4) == Mathf.Abs(y - 7) / 2 && x >= 4 && x <= 9;
                });
                return _chevronIcon;
            }
        }

        private static Sprite CreateMaskSprite(string name, int size, System.Func<int, int, bool> include)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = include(x, y)
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    // Action tags already animate their own visuals while their button keeps a fixed hit area.
    internal sealed class FloatingActionTagButton : UnityEngine.UI.Button { }
}
