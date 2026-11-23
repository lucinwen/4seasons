#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JingweiWeave.EditorTools
{
    /// <summary>
    /// 一键构建「经纬织纹」：配置素材导入 → 生成 Canvas / 棋盘 / 工具 / 按钮 → 保存场景。
    /// 菜单：Tools/经纬织纹/一键构建小游戏场景
    /// </summary>
    public static class EmbroideryBuilder
    {
        private const string RootDir = "Assets/3-经纬推演game";
        private const string ScenePath = RootDir + "/Scenes/EmbroideryScene.unity";
        private const string GlowPath = "Assets/4-寻找染料game/Game/Art/UI/Soft_Glow.png";
        private const string ButtonPath = "Assets/4-寻找染料game/Game/Art/UI/Button_Paper.png";

        // 底板原图尺寸
        private const float BoardW = 1672f;
        private const float BoardH = 941f;
        // UI 中棋盘显示宽度（高度按比例）
        private const float BoardUiWidth = 1300f;
        private static float Scale { get { return BoardUiWidth / BoardW; } }

        // ---- 以下为对 3-经纬底板.png 实测得到的网格像素坐标（原点为图片左上）----
        private static readonly float[] ColLines = { 368f, 518f, 669f, 818f, 969f, 1120f, 1271f, 1420f, 1571f };
        private static readonly float[] RowLines = { 168f, 306f, 442f, 577f, 712f, 852f };

        private static readonly Color Ink = new Color32(92, 56, 30, 255);            // 深棕
        private static readonly Color Gold = new Color32(214, 158, 60, 255);         // 选中金
        private static readonly Color PanelPaper = new Color32(244, 231, 206, 255);

        private static Font gameFont;
        private static Sprite boardSprite;
        private static Sprite xSprite;
        private static Sprite shuttleSprite;
        private static Sprite beaterSprite;
        private static Sprite glowSprite;
        private static Sprite buttonSprite;

        [MenuItem("Tools/经纬织纹/一键构建小游戏场景")]
        public static void Build()
        {
            Debug.Log("[EmbroideryBuilder] 开始构建经纬织纹...");

            ConfigureTextures();
            AssetDatabase.Refresh();
            LoadAssets();
            BuildScene();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EmbroideryBuilder] 构建完成：" + ScenePath);
        }

        // ------------------------------------------------------------------
        // 素材导入
        // ------------------------------------------------------------------
        private static void ConfigureTextures()
        {
            string[] files = { "3-经纬底板.png", "3-经纬单体.png", "3-梭子.png", "3-拨子.png" };
            foreach (string file in files)
            {
                string path = RootDir + "/" + file;
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("[EmbroideryBuilder] 未找到素材：" + path);
                    continue;
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Point;          // 像素风，避免模糊
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static void LoadAssets()
        {
            gameFont = WorkshopArt.PixelFont;
            if (gameFont == null) Debug.LogWarning("[EmbroideryBuilder] 未找到字体，UI 将使用默认字体");

            boardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + "/3-经纬底板.png");
            xSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + "/3-经纬单体.png");
            shuttleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + "/3-梭子.png");
            beaterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + "/3-拨子.png");
            glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath);
            buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
        }

        // ------------------------------------------------------------------
        // 场景搭建
        // ------------------------------------------------------------------
        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 相机（纯色背景即可，UI 用 Overlay）
            GameObject camGo = new GameObject("Main Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(46, 32, 24, 255);
            cam.orthographic = true;
            camGo.tag = "MainCamera";

            // EventSystem
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // Canvas
            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // UI 根节点（OpenGame / CloseGame 控制显隐）
            GameObject uiRoot = CreateUI("UIRoot", canvasGo.transform);
            Stretch(uiRoot);

            // 棋盘
            GameObject board = CreateUI("Board", uiRoot.transform);
            Image boardImg = board.AddComponent<Image>();
            boardImg.sprite = boardSprite;
            boardImg.raycastTarget = false;
            RectTransform boardRt = board.GetComponent<RectTransform>();
            boardRt.sizeDelta = new Vector2(BoardUiWidth, BoardUiWidth * BoardH / BoardW);
            // 木框本体包围盒为 (300,111)-(1637,918)，中心为 (968.5,514.5)。
            // 排除透明边距、顶部列数字和左侧行数字，按棋盘本体视觉居中。
            boardRt.anchoredPosition = new Vector2(
                (BoardW * 0.5f - 968.5f) * Scale,
                (514.5f - BoardH * 0.5f) * Scale);

            // 管理器
            GameObject gmGo = new GameObject("GameManager");
            EmbroideryGameManager gm = gmGo.AddComponent<EmbroideryGameManager>();
            gm.uiRoot = uiRoot;
            EmbroideryToolController toolCtrl = gmGo.AddComponent<EmbroideryToolController>();
            EmbroideryUIController uiCtrl = gmGo.AddComponent<EmbroideryUIController>();

            BuildCells(board.transform, gm);
            BuildTools(uiRoot.transform, toolCtrl);
            BuildButtons(uiRoot.transform, uiCtrl);
            BuildSuccessPanel(uiRoot.transform, uiCtrl);

            Directory.CreateDirectory(RootDir + "/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        /// <summary>把图片像素坐标（左上原点）转成 Board 内的 UI 局部坐标。</summary>
        private static Vector2 ImgToUi(float imgX, float imgY)
        {
            return new Vector2((imgX - BoardW * 0.5f) * Scale, (BoardH * 0.5f - imgY) * Scale);
        }

        private static void BuildCells(Transform board, EmbroideryGameManager gm)
        {
            for (int r = 0; r < EmbroideryGameManager.Rows; r++)
            {
                for (int c = 0; c < EmbroideryGameManager.Cols; c++)
                {
                    float x0 = ColLines[c];
                    float x1 = ColLines[c + 1];
                    float y0 = RowLines[r];
                    float y1 = RowLines[r + 1];
                    float cx = (x0 + x1) * 0.5f;
                    float cy = (y0 + y1) * 0.5f;
                    float cellW = (x1 - x0) * Scale;
                    float cellH = (y1 - y0) * Scale;

                    GameObject cellGo = CreateUI("Cell_" + r + "_" + c, board);
                    RectTransform rt = cellGo.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(cellW, cellH);
                    rt.anchoredPosition = ImgToUi(cx, cy);

                    // 透明点击区域
                    Image hitImg = cellGo.AddComponent<Image>();
                    hitImg.color = new Color(1f, 1f, 1f, 0f);
                    Button btn = cellGo.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                    btn.targetGraphic = hitImg;

                    EmbroideryCell cell = cellGo.AddComponent<EmbroideryCell>();
                    cell.row = r;
                    cell.col = c;

                    // 已织入的 X（居中，约为格子的 72%）
                    GameObject xGo = CreateUI("X", cellGo.transform);
                    RectTransform xRt = xGo.GetComponent<RectTransform>();
                    float xSize = Mathf.Min(cellW, cellH) * 0.72f;
                    xRt.sizeDelta = new Vector2(xSize, xSize);
                    xRt.anchoredPosition = Vector2.zero;
                    Image xImg = xGo.AddComponent<Image>();
                    xImg.sprite = xSprite;
                    xImg.raycastTarget = false;
                    cell.xImage = xImg;

                    // 悬停半透明预览 X
                    GameObject pvGo = CreateUI("Preview", cellGo.transform);
                    RectTransform pvRt = pvGo.GetComponent<RectTransform>();
                    pvRt.sizeDelta = new Vector2(xSize, xSize);
                    pvRt.anchoredPosition = Vector2.zero;
                    Image pvImg = pvGo.AddComponent<Image>();
                    pvImg.sprite = xSprite;
                    pvImg.color = new Color(1f, 1f, 1f, 0.4f);
                    pvImg.raycastTarget = false;
                    cell.previewImage = pvImg;

                    gm.RegisterCell(cell, r, c);
                }
            }
        }

        private static void BuildTools(Transform parent, EmbroideryToolController toolCtrl)
        {
            // 透明底图标，置于板子右侧偏下
            Button shuttle = CreateToolButton(parent, "ShuttleButton", new Vector2(790f, -60f),
                shuttleSprite, new Vector2(200f, 143f), out GameObject shuttleHl);
            Button beater = CreateToolButton(parent, "BeaterButton", new Vector2(790f, -260f),
                beaterSprite, new Vector2(200f, 121f), out GameObject beaterHl);

            toolCtrl.shuttleButton = shuttle;
            toolCtrl.beaterButton = beater;
            toolCtrl.shuttleHighlight = shuttleHl;
            toolCtrl.beaterHighlight = beaterHl;
            shuttleHl.SetActive(true);
            beaterHl.SetActive(false);
        }

        private static Button CreateToolButton(Transform parent, string name, Vector2 pos,
            Sprite icon, Vector2 iconSize, out GameObject highlight)
        {
            GameObject go = CreateUI(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            Vector2 size = iconSize + new Vector2(30f, 30f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            // 透明点击区（无底色卡片）
            Image hitArea = go.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            // 选中态：圆心渐亮的金色光晕，未选中时隐藏
            highlight = CreateUI("Highlight", go.transform);
            RectTransform hlRt = highlight.GetComponent<RectTransform>();
            float glowSize = Mathf.Max(iconSize.x, iconSize.y) + 60f;
            hlRt.sizeDelta = new Vector2(glowSize, glowSize);
            hlRt.anchoredPosition = Vector2.zero;
            Image glowImg = highlight.AddComponent<Image>();
            glowImg.sprite = glowSprite;
            glowImg.color = new Color(1f, 0.93f, 0.72f, 0.62f);
            glowImg.preserveAspect = true;
            glowImg.raycastTarget = false;

            // 工具图标（透明底 PNG 原样显示）
            GameObject iconGo = CreateUI("Icon", go.transform);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.sizeDelta = iconSize;
            iconRt.anchoredPosition = Vector2.zero;
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = hitArea;
            return btn;
        }

        private static void BuildButtons(Transform parent, EmbroideryUIController uiCtrl)
        {
            uiCtrl.resetButton = CreateTextButton(parent, "ResetButton", "重置", new Vector2(200f, -430f));
            uiCtrl.closeButton = CreateTextButton(parent, "CloseButton", "关闭", new Vector2(-200f, -430f));
        }

        private static Button CreateTextButton(Transform parent, string name, string caption, Vector2 pos)
        {
            GameObject go = CreateUI(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 70f);
            rt.anchoredPosition = pos;

            Image bg = go.AddComponent<Image>();
            bg.sprite = buttonSprite;
            bg.type = Image.Type.Sliced;   // 九宫格拉伸，保留像素圆角
            bg.color = Color.white;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;

            GameObject textGo = CreateUI("Text", go.transform);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.sizeDelta = new Vector2(180f, 70f);
            trt.anchoredPosition = Vector2.zero;
            Text text = textGo.AddComponent<Text>();
            text.font = gameFont;
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Ink;
            text.text = caption;
            text.raycastTarget = false;
            return btn;
        }

        private static void BuildSuccessPanel(Transform parent, EmbroideryUIController uiCtrl)
        {
            GameObject panel = CreateUI("SuccessPanel", parent);
            Stretch(panel);

            // 半透明遮罩
            Image mask = panel.AddComponent<Image>();
            mask.color = new Color(0f, 0f, 0f, 0.55f);

            // 中央卡片
            GameObject card = CreateUI("Card", panel.transform);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(700f, 360f);
            cardRt.anchoredPosition = Vector2.zero;
            Image cardBg = card.AddComponent<Image>();
            cardBg.color = PanelPaper;

            GameObject titleGo = CreateUI("Title", card.transform);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(660f, 120f);
            titleRt.anchoredPosition = new Vector2(0f, 70f);
            Text title = titleGo.AddComponent<Text>();
            title.font = gameFont;
            title.fontSize = 72;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Ink;
            title.text = "织造完成！";
            title.raycastTarget = false;

            GameObject btnGo = CreateUI("ConfirmButton", card.transform);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(240f, 84f);
            btnRt.anchoredPosition = new Vector2(0f, -100f);
            Image btnBg = btnGo.AddComponent<Image>();
            btnBg.color = Gold;
            Button confirm = btnGo.AddComponent<Button>();
            confirm.targetGraphic = btnBg;

            GameObject btnTextGo = CreateUI("Text", btnGo.transform);
            RectTransform btnTextRt = btnTextGo.GetComponent<RectTransform>();
            btnTextRt.sizeDelta = new Vector2(240f, 84f);
            btnTextRt.anchoredPosition = Vector2.zero;
            Text btnText = btnTextGo.AddComponent<Text>();
            btnText.font = gameFont;
            btnText.fontSize = 38;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Ink;
            btnText.text = "确认";
            btnText.raycastTarget = false;

            uiCtrl.successPanel = panel;
            uiCtrl.successConfirmButton = confirm;
            panel.SetActive(false);
        }

        private static void ConfigureBuildSettings()
        {
            // 追加而非覆盖，避免影响主游戏已有场景
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath))
                list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            PlayerSettings.runInBackground = true;
        }

        // ------------------------------------------------------------------
        // UI 工具
        // ------------------------------------------------------------------
        private static GameObject CreateUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
