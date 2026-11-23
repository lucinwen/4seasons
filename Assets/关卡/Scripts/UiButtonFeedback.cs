using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Qingming.UI
{
    /// <summary>
    /// Shared hover wash and press feedback for standard UI buttons.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class UiButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        private static readonly Color Brass = new Color32(231, 194, 119, 255);
        private static readonly Color PressedBrass = new Color32(255, 216, 122, 255);
        private static readonly Color PressShade = new Color(0.17f, 0.12f, 0.08f, 0.14f);
        private const float HoverOpacity = 0.20f;
        private const float MinimumPressTime = 0.055f;
        private static Material overlayMaterial;

        private Selectable selectable;
        private Button soundButton;
        private Image surface;
        private Image hoverOverlay;
        private Graphic hitGraphic;
        private CanvasGroup cornerGroup;
        private CanvasGroup glintGroup;
        private Graphic[] cornerMarks;
        private RectTransform glintRoot;
        private Shadow graphicShadow;
        private bool ownsShadow;
        private Vector2 shadowRestDistance;
        private Vector3 restingScale;
        private Vector3 appliedScale;
        private Vector4 restingRaycastPadding;
        private float hoverBlend;
        private float pointerBlend;
        private float pressBlend;
        private float releaseTime;
        private float glintTime = 1f;
        private bool hovered;
        private bool pressed;
        private bool selected;
        private bool selectedByPointer;
        private bool hasScaleOffset;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            soundButton = selectable as Button;
            hitGraphic = GetComponent<Graphic>();
            BuildHoverOverlay();
            BuildCorners();
            ResetFeedback();
        }

        private void OnEnable()
        {
            if (soundButton == null) soundButton = GetComponent<Button>();
            if (soundButton != null)
                soundButton.onClick.AddListener(global::WorkshopAudioFeedback.Click);
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        }

        private void OnDisable()
        {
            if (soundButton != null)
                soundButton.onClick.RemoveListener(global::WorkshopAudioFeedback.Click);
            hovered = false;
            pressed = false;
            selected = false;
            selectedByPointer = false;
            ResetFeedback();
        }

        private void Update()
        {
            if (selectable == null) selectable = GetComponent<Selectable>();
            if (cornerGroup == null || glintRoot == null || glintGroup == null)
            {
                BuildCorners();
                ResetFeedback();
            }

            bool interactable = selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable();
            bool highlighted = interactable && (hovered || selected);
            if (!interactable)
            {
                CancelPress();
                pressBlend = 0f;
            }

            float delta = Time.unscaledDeltaTime;
            bool pressVisible = interactable && hovered && (pressed || Time.unscaledTime < releaseTime);

            hoverBlend = Mathf.MoveTowards(hoverBlend, highlighted ? 1f : 0f,
                delta * 10f);
            pointerBlend = Mathf.MoveTowards(pointerBlend, interactable && hovered ? 1f : 0f,
                delta * (hovered ? 14f : 10f));
            pressBlend = Mathf.MoveTowards(pressBlend, pressVisible ? 1f : 0f,
                delta * (pressVisible ? 34f : 9f));

            ApplyScale(Mathf.Lerp(1f, 0.956f, pressBlend));
            UpdateHoverOverlay();

            if (cornerGroup != null)
                cornerGroup.alpha = hoverBlend;

            Color accent = Color.Lerp(Brass, PressedBrass, pressBlend);
            if (cornerMarks != null)
            {
                foreach (Graphic mark in cornerMarks)
                    if (mark != null) mark.color = accent;
            }

            if (graphicShadow != null)
            {
                Vector2 liftedShadow = Vector2.Lerp(shadowRestDistance, new Vector2(2f, -3f), hoverBlend);
                Vector2 target = Vector2.Lerp(liftedShadow, shadowRestDistance * 0.35f, pressBlend);
                graphicShadow.effectDistance = Vector2.Lerp(graphicShadow.effectDistance, target,
                    1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            }

            if (glintTime < 1f)
            {
                glintTime = Mathf.Min(1f, glintTime + Time.unscaledDeltaTime / 0.24f);
                float pulse = 1f - Mathf.Pow(1f - glintTime, 3f);
                glintRoot.localScale = Vector3.one * Mathf.Lerp(1f, 1.055f, pulse);
                glintGroup.alpha = Mathf.Sin((1f - glintTime) * Mathf.PI) * 0.8f;
            }
            else
            {
                glintRoot.localScale = Vector3.one;
                glintGroup.alpha = 0f;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable())
                global::WorkshopAudioFeedback.Hover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            releaseTime = 0f;
            if (selectedByPointer && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);
            selectedByPointer = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left ||
                selectable == null || !selectable.isActiveAndEnabled || !selectable.IsInteractable()) return;
            hovered = true;
            pressed = true;
            releaseTime = Time.unscaledTime + MinimumPressTime;
            selectedByPointer = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            bool clicked = pressed && hovered;
            pressed = false;
            if (clicked && selectable != null && selectable.IsInteractable())
                glintTime = 0f;
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            CancelPress();
            selectedByPointer = false;
        }

        private void CancelPress()
        {
            pressed = false;
            releaseTime = 0f;
        }

        private void ApplyScale(float factor)
        {
            if (factor == 1f)
            {
                if (!hasScaleOffset) return;
                if (transform.localScale == appliedScale) transform.localScale = restingScale;
                if (hitGraphic != null) hitGraphic.raycastPadding = restingRaycastPadding;
                hasScaleOffset = false;
                return;
            }

            if (!hasScaleOffset && hitGraphic != null) restingRaycastPadding = hitGraphic.raycastPadding;
            if (!hasScaleOffset || transform.localScale != appliedScale) restingScale = transform.localScale;
            appliedScale = restingScale * factor;
            transform.localScale = appliedScale;
            hasScaleOffset = true;

            // Keep the root hit area steady as the button face compresses.
            if (hitGraphic != null)
            {
                Rect rect = hitGraphic.rectTransform.rect;
                var edges = new Vector4(rect.xMin, rect.yMin, -rect.xMax, -rect.yMax);
                hitGraphic.raycastPadding = (restingRaycastPadding + edges) / factor - edges;
            }
        }

        private void BuildHoverOverlay()
        {
            surface = selectable.targetGraphic as Image;
            if (surface == null || !surface.transform.IsChildOf(transform))
                surface = GetComponent<Image>();

            Transform parent = surface != null ? surface.transform : transform;
            hoverOverlay = NewImage(parent, "Button Hover Overlay", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            hoverOverlay.color = Color.clear;
            hoverOverlay.enabled = false;
            if (overlayMaterial == null)
            {
                var shader = Resources.Load<Shader>("WorkshopButtonOverlay");
                if (shader != null) overlayMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            if (overlayMaterial != null) hoverOverlay.material = overlayMaterial;
            if (parent.GetComponent<LayoutGroup>() != null)
                hoverOverlay.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        private void UpdateHoverOverlay()
        {
            if (hoverOverlay == null) return;
            hoverOverlay.enabled = (pointerBlend > 0f || pressBlend > 0f) &&
                (surface == null || surface.isActiveAndEnabled);
            if (!hoverOverlay.enabled) return;

            if (surface != null)
            {
                var rect = hoverOverlay.rectTransform;
                if (rect.pivot != surface.rectTransform.pivot)
                {
                    rect.pivot = surface.rectTransform.pivot;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }

                hoverOverlay.sprite = surface.overrideSprite;
                hoverOverlay.maskable = surface.maskable;
                hoverOverlay.preserveAspect = surface.preserveAspect;
                hoverOverlay.type = surface.type;
                hoverOverlay.pixelsPerUnitMultiplier = surface.pixelsPerUnitMultiplier;
                hoverOverlay.fillCenter = surface.fillCenter;
                hoverOverlay.useSpriteMesh = surface.useSpriteMesh;
                if (surface.type == Image.Type.Filled)
                {
                    hoverOverlay.fillMethod = surface.fillMethod;
                    hoverOverlay.fillOrigin = surface.fillOrigin;
                    hoverOverlay.fillClockwise = surface.fillClockwise;
                    hoverOverlay.fillAmount = surface.fillAmount;
                }
            }

            var wash = new Color(1f, 1f, 1f, HoverOpacity * pointerBlend);
            hoverOverlay.color = Color.Lerp(wash, PressShade, pressBlend);
        }

        private void BuildCorners()
        {
            var cornerRoot = NewRect(transform, "Button Corner Marks", Vector2.zero, Vector2.zero);
            cornerRoot.anchorMin = Vector2.zero;
            cornerRoot.anchorMax = Vector2.one;
            cornerRoot.offsetMin = cornerRoot.offsetMax = Vector2.zero;
            cornerGroup = cornerRoot.gameObject.AddComponent<CanvasGroup>();
            cornerGroup.interactable = false;
            cornerGroup.blocksRaycasts = false;
            cornerGroup.alpha = 0f;

            cornerMarks = new[]
            {
                AddRule(cornerRoot, "Top Left H", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(4f, -4f), new Vector2(17f, 2f)),
                AddRule(cornerRoot, "Top Left V", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(4f, -4f), new Vector2(2f, 14f)),
                AddRule(cornerRoot, "Top Right H", new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-4f, -4f), new Vector2(17f, 2f)),
                AddRule(cornerRoot, "Top Right V", new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-4f, -4f), new Vector2(2f, 14f)),
                AddRule(cornerRoot, "Bottom Left H", new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(4f, 4f), new Vector2(17f, 2f)),
                AddRule(cornerRoot, "Bottom Left V", new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(4f, 4f), new Vector2(2f, 14f)),
                AddRule(cornerRoot, "Bottom Right H", new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-4f, 4f), new Vector2(17f, 2f)),
                AddRule(cornerRoot, "Bottom Right V", new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-4f, 4f), new Vector2(2f, 14f))
            };

            glintRoot = NewRect(transform, "Button Click Glint", Vector2.zero, Vector2.zero);
            glintRoot.anchorMin = Vector2.zero;
            glintRoot.anchorMax = Vector2.one;
            glintRoot.offsetMin = new Vector2(-1f, -1f);
            glintRoot.offsetMax = new Vector2(1f, 1f);
            glintGroup = glintRoot.gameObject.AddComponent<CanvasGroup>();
            glintGroup.interactable = false;
            glintGroup.blocksRaycasts = false;
            glintGroup.alpha = 0f;
            AddGlintCorner("Top Left Glint", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(9f, 2f));
            AddGlintCorner("Top Right Glint", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-9f, 2f));
            AddGlintCorner("Bottom Left Glint", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(9f, -2f));
            AddGlintCorner("Bottom Right Glint", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-9f, -2f));

            Graphic target = selectable != null ? selectable.targetGraphic : null;
            if (target != null && target.GetComponent<Outline>() == null)
            {
                graphicShadow = target.GetComponent<Shadow>();
                if (graphicShadow == null)
                {
                    ownsShadow = true;
                    graphicShadow = target.gameObject.AddComponent<Shadow>();
                    graphicShadow.effectColor = new Color(0.13f, 0.08f, 0.04f, 0.36f);
                    shadowRestDistance = new Vector2(1f, -2f);
                    graphicShadow.effectDistance = shadowRestDistance;
                }
                else
                {
                    shadowRestDistance = graphicShadow.effectDistance;
                }
            }
        }

        private Image AddRule(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size)
        {
            var image = NewImage(parent, name, anchorMin, anchorMax, position, size);
            image.color = Brass;
            return image;
        }

        private void AddGlintCorner(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
        {
            var diamond = NewImage(glintRoot, name, anchorMin, anchorMax, position, new Vector2(5f, 5f));
            diamond.color = new Color32(255, 223, 144, 255);
            diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private static Image NewImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size)
        {
            var rect = NewRect(parent, name, position, size);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
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

        private void ResetFeedback()
        {
            hoverBlend = 0f;
            pointerBlend = 0f;
            pressBlend = 0f;
            releaseTime = 0f;
            glintTime = 1f;
            ApplyScale(1f);
            if (hoverOverlay != null) hoverOverlay.enabled = false;
            if (cornerGroup != null) cornerGroup.alpha = 0f;
            if (glintGroup != null) glintGroup.alpha = 0f;
            if (graphicShadow != null) graphicShadow.effectDistance = shadowRestDistance;
        }

        private void OnDestroy()
        {
            if (hoverOverlay != null) Destroy(hoverOverlay.gameObject);
            if (graphicShadow == null) return;
            if (ownsShadow) Destroy(graphicShadow);
            else graphicShadow.effectDistance = shadowRestDistance;
        }
    }

    /// <summary>Attaches feedback to runtime-built buttons and skips world action tags.</summary>
    internal sealed class UiButtonFeedbackInstaller : MonoBehaviour
    {
        private static UiButtonFeedbackInstaller instance;
        private float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var go = new GameObject("UI Button Feedback");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<UiButtonFeedbackInstaller>();
            AttachToAllButtons();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.2f;
            AttachToAllButtons();
        }

        private static void AttachToAllButtons()
        {
            foreach (Button button in FindObjectsOfType<Button>(true))
            {
                if (button == null ||
                    button is Qingming.LevelBackground.FloatingActionTagButton ||
                    button.GetComponent<UiButtonFeedback>() != null)
                    continue;
                button.gameObject.AddComponent<UiButtonFeedback>();
            }
        }
    }
}
