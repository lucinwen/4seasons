using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JiangnanDye.ClothCutting
{
    /// <summary>
    /// 剪刀按钮（固定不动）+ 飞行剪刀光标（跟随鼠标）。
    /// 仿梭子/拨子风格：默认按钮固定显示，点击激活裁剪后按钮仍固定、另一个光标飞向鼠标。
    /// </summary>
    [RequireComponent(typeof(Image), typeof(Button))]
    public class ScissorsController : MonoBehaviour
    {
        [Header("按钮自身引用")]
        [Tooltip("按钮上的剪刀图标 Image（4-剪刀.png）。按钮永远固定显示这张图。")]
        public Image scissorsImage;

        [Tooltip("按钮上的选中金色光晕 Highlight。")]
        public Image highlightImage;

        [Tooltip("按钮自身 RectTransform（Builder 自动注入）。按钮永远固定在 buttonAnchoredPos。")]
        public RectTransform buttonRt;

        [Header("飞行光标（跟随鼠标的那把剪刀）")]
        [Tooltip("Builder 在 Canvas 下额外创建的 FlyingScissors，默认隐藏，激活裁剪模式才显示。")]
        public RectTransform scissorsCursorRt;

        [Tooltip("飞行光标上的 Image 组件。")]
        public Image scissorsCursorImage;

        [Header("Canvas")]
        public Canvas canvas;

        [Tooltip("剪刀尖端相对于光标中心的偏移（像素）。")]
        public Vector2 tipOffset = new Vector2(30f, -20f);

        [Header("按钮固定位置（Canvas 中心坐标系）")]
        public Vector2 buttonAnchoredPos = new Vector2(669f, -258f);

        [Header("运行时状态")]
        public bool cuttingMode;
        public bool isCutting;
        public bool IsInCuttingArea { get; private set; }
        public Vector2 tipLocalPos;

        private ClothCuttingGameManager manager;
        private CanvasScaler scaler;

        private void Awake()
        {
            if (buttonRt == null) buttonRt = GetComponent<RectTransform>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas != null) scaler = canvas.GetComponent<CanvasScaler>();

            // 按钮永远固定在 buttonAnchoredPos
            buttonRt.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRt.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRt.pivot = new Vector2(0.5f, 0.5f);
            buttonRt.anchoredPosition = buttonAnchoredPos;

            // 飞行光标默认隐藏
            if (scissorsCursorRt != null)
                scissorsCursorRt.gameObject.SetActive(false);

            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnButtonClick);

            UpdateHighlight();
            Debug.Log("[ScissorsController] Awake OK buttonRt=" + (buttonRt != null) + " cursor=" + (scissorsCursorRt != null) + " canvas=" + (canvas != null) + " worldCamera=" + (canvas != null && canvas.worldCamera != null));
        }

        public void BindManager(ClothCuttingGameManager mgr) { manager = mgr; Debug.Log("[ScissorsController] BindManager called, mgr=" + (mgr != null)); }

        private void OnButtonClick()
        {
            if (WorkshopMinigameMenu.IsPaused) return;
            Debug.Log("[ScissorsController] OnButtonClick triggered, current cuttingMode=" + cuttingMode + " manager=" + (manager != null));
            if (manager != null && manager.IsGameOver) { Debug.Log("  -> game over, ignored"); return; }
            cuttingMode = !cuttingMode;
            if (!cuttingMode)
            {
                isCutting = false;
                manager?.OnCutEnd();
                Cursor.visible = true;
                if (scissorsCursorRt != null) scissorsCursorRt.gameObject.SetActive(false);
            }
            else
            {
                if (scissorsCursorRt != null) scissorsCursorRt.gameObject.SetActive(true);
                Debug.Log("  -> cuttingMode ON, cursor active");
            }
            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            if (highlightImage != null)
                highlightImage.gameObject.SetActive(cuttingMode);
        }

        public void Show()
        {
            if (scissorsImage != null) scissorsImage.enabled = true;
            if (buttonRt != null) buttonRt.gameObject.SetActive(true);
            if (scissorsCursorRt != null && cuttingMode)
                scissorsCursorRt.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (scissorsImage != null) scissorsImage.enabled = false;
            if (scissorsCursorRt != null) scissorsCursorRt.gameObject.SetActive(false);
            Cursor.visible = true;
        }

        private float debugTimer;

        /// <summary>
        /// Update 只负责两件事：
        /// 1. 飞行剪刀光标跟随鼠标（用手动坐标计算）
        /// 2. 在裁剪区域内隐藏系统光标
        /// 裁剪进度逻辑全交给 WorkbenchCuttingHandler（用 UGUI 事件系统，不用 Input.mousePosition）。
        /// </summary>
        private void Update()
        {
            if (canvas == null) return;
            if (manager != null && manager.IsGameOver) return;
            if (WorkshopMinigameMenu.IsPaused)
            {
                if (scissorsCursorRt != null) scissorsCursorRt.gameObject.SetActive(false);
                Cursor.visible = true;
                return;
            }

            if (cuttingMode)
            {
                // ===== 手动计算鼠标在 referenceResolution 坐标系的位置 =====
                float refW = 1920f;
                float refH = 1080f;
                float scaleX = Screen.width / refW;
                float scaleY = Screen.height / refH;
                float scale = Mathf.Min(scaleX, scaleY);
                var canvasCenterScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 mouseScreen = Input.mousePosition;
                Vector2 localPos = (mouseScreen - canvasCenterScreen) / scale;
                Vector2 tipLocal = localPos + tipOffset;

                if (manager != null)
                    IsInCuttingArea = manager.IsPointInWorkArea(tipLocal);

                // 飞行光标跟随鼠标
                if (scissorsCursorRt != null)
                {
                    if (!scissorsCursorRt.gameObject.activeSelf) scissorsCursorRt.gameObject.SetActive(true);
                    scissorsCursorRt.anchoredPosition = localPos;
                }

                // 在裁剪区域内隐藏系统光标
                if (IsInCuttingArea) Cursor.visible = false;
                else Cursor.visible = true;

                // 调试日志（每秒）
                debugTimer += Time.deltaTime;
                if (debugTimer >= 1f)
                {
                    debugTimer = 0f;
                    Debug.Log($"[Scissors] cutting=ON inArea={IsInCuttingArea} pos=({localPos.x:F0},{localPos.y:F0}) isCutting={isCutting}");
                }
            }
            else
            {
                if (scissorsCursorRt != null) scissorsCursorRt.gameObject.SetActive(false);
                Cursor.visible = true;
                isCutting = false;
            }
        }

        private void OnDisable()
        {
            Cursor.visible = true;
            isCutting = false;
            cuttingMode = false;
            if (scissorsCursorRt != null) scissorsCursorRt.gameObject.SetActive(false);
        }
    }
}
