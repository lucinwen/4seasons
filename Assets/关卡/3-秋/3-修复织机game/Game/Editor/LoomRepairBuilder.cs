#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JiangnanDye.LoomRepair.EditorTools
{
    /// <summary>
    /// 一键构建修复织机小游戏。
    /// 菜单：Tools/江南织机/一键构建修复织机小游戏
    /// </summary>
    public static class LoomRepairBuilder
    {
        private const string RootDir = "Assets/关卡/3-秋/3-修复织机game";
        private const string ScriptsDir = RootDir + "/Game/Scripts";
        private const string ScenePath = RootDir + "/Game/Scenes/LoomRepair.unity";

        private static readonly Color Paper = new Color(0.96f, 0.89f, 0.75f);
        private static readonly Color Brick = new Color(0.66f, 0.27f, 0.18f);
        private static readonly Color Gold = new Color(0.75f, 0.6f, 0.33f);
        private static readonly Color DarkBg = new Color(0.18f, 0.14f, 0.1f);
        private static readonly Color DimBlack = new Color(0, 0, 0, 0.65f);

        [MenuItem("Tools/江南织机/一键构建修复织机小游戏")]
        public static void Build()
        {
            Debug.Log("[LoomRepairBuilder] 开始构建修复织机小游戏...");

            EnsureFolder(RootDir + "/Game");
            EnsureFolder(RootDir + "/Game/Scenes");
            ConfigureTextures();
            AssetDatabase.Refresh();

            // 加载所有素材 sprite
            Sprite loomSprite = LoadSprite("3-织机.png");
            Sprite progFrameSprite = LoadSprite("3-进度框.png");
            Sprite progBarSprite = LoadSprite("3-进度条.png");
            Sprite panelSprite = LoadSprite("3-修复面板.png");
            Sprite hitZoneSprite = LoadSprite("3-修复面板的打击区域.png");
            Sprite pointerSprite = LoadSprite("3-打击指针.png");

            // 获取字体（优先用像素体）
            Font pixelFont = WorkshopArt.PixelFont;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // === 相机 ===
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = DarkBg;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // === EventSystem ===
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // === Canvas ===
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // === 背景 ===
            var bg = CreateUI("Background", canvas.transform);
            PlaceFull(bg);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = DarkBg;
            bgImg.raycastTarget = false;

            // === 中央织机 ===
            float loomW = 750f, loomH = 650f;
            var loomGo = CreateUI("Loom", canvas.transform);
            PlaceCenter(loomGo, new Vector2(-241f, -2f), new Vector2(loomW, loomH));
            var loomImg = loomGo.AddComponent<Image>();
            loomImg.sprite = loomSprite;
            loomImg.preserveAspect = true;
            loomImg.color = Color.white;
            loomImg.raycastTarget = false;

            // === 顶部进度框（加大加粗） ===
            float progFrameW = 1000f, progFrameH = 180f;
            var progFrameGo = CreateUI("ProgressFrame", canvas.transform);
            PlaceCenter(progFrameGo, new Vector2(-245f, 380f), new Vector2(progFrameW, progFrameH));
            var progFrameImg = progFrameGo.AddComponent<Image>();
            progFrameImg.sprite = progFrameSprite;
            progFrameImg.preserveAspect = true;
            progFrameImg.color = Color.white;
            progFrameImg.raycastTarget = false;

            // 进度填充（加粗 — 扩大上下 anchor 范围）
            var progFillGo = CreateUI("ProgressFill", progFrameGo.transform);
            var progFillRt = progFillGo.GetComponent<RectTransform>();
            progFillRt.anchorMin = new Vector2(0.04f, 0.2f);
            progFillRt.anchorMax = new Vector2(0.96f, 0.8f);
            progFillRt.offsetMin = Vector2.zero;
            progFillRt.offsetMax = Vector2.zero;
            var progFillImg = progFillGo.AddComponent<Image>();
            progFillImg.sprite = progBarSprite;
            progFillImg.type = Image.Type.Filled;
            progFillImg.fillMethod = Image.FillMethod.Horizontal;
            progFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            progFillImg.fillAmount = 0f;
            progFillImg.preserveAspect = false;
            progFillImg.color = Color.white;
            progFillImg.raycastTarget = false;

            // 进度文字（字号同步加大）
            var progTextGo = CreateUI("ProgressText", progFrameGo.transform);
            PlaceCenter(progTextGo, Vector2.zero, new Vector2(800, 90));
            var progText = progTextGo.AddComponent<Text>();
            progText.font = pixelFont;
            progText.fontSize = 48;
            progText.color = Paper;
            progText.alignment = TextAnchor.MiddleCenter;
            progText.text = "0%";
            progText.raycastTarget = false;

            // === 校准面板（织机前方） ===
            var calibRootGo = CreateUI("CalibrationPanel", canvas.transform);
            PlaceCenter(calibRootGo, new Vector2(331f, 120f), new Vector2(500f, 500f));
            calibRootGo.SetActive(false); // 默认隐藏

            // 校准面板底图（半圆）
            var panelBodyGo = CreateUI("PanelBody", calibRootGo.transform);
            PlaceCenter(panelBodyGo, Vector2.zero, new Vector2(500f, 500f));
            var panelBodyImg = panelBodyGo.AddComponent<Image>();
            panelBodyImg.sprite = panelSprite;
            panelBodyImg.preserveAspect = true;
            panelBodyImg.color = Color.white;
            panelBodyImg.raycastTarget = true; // 让面板可以接收点击

            // 指针（pivot 设为底部中心，绕底部圆心旋转；加大加粗）
            var pointerGo = CreateUI("Pointer", calibRootGo.transform);
            var pointerRt = pointerGo.GetComponent<RectTransform>();
            pointerRt.anchorMin = new Vector2(0.5f, 0.5f);
            pointerRt.anchorMax = new Vector2(0.5f, 0.5f);
            pointerRt.pivot = new Vector2(0.5f, 0f); // 关键：pivot 在底部中心 = 旋转中心
            pointerRt.sizeDelta = new Vector2(320f, 1600f); // 用户调后：80×400 的 4 倍
            // 面板 anchoredPosition=(0,0)，sizeDelta=(500,500)，底部 y = -250
            // 指针 pivot.y=0（底部），所以 anchoredPosition.y=-250 让指针底部对齐面板底部圆心
            pointerRt.anchoredPosition = new Vector2(0f, -250f);

            var pointerImg = pointerGo.AddComponent<Image>();
            pointerImg.sprite = pointerSprite;
            pointerImg.preserveAspect = true;
            pointerImg.color = Color.white;
            pointerImg.raycastTarget = false;
            pointerRt.localEulerAngles = new Vector3(0, 0, -75f); // 初始角度 = minAngle(15)-90 = -75

            // === 开始提示 ===
            var startHintGo = CreateUI("StartHint", canvas.transform);
            PlaceCenter(startHintGo, new Vector2(0f, -338f), new Vector2(600, 80));
            var startHintText = startHintGo.AddComponent<Text>();
            startHintText.font = pixelFont;
            startHintText.fontSize = 36;
            startHintText.color = new Color(Paper.r, Paper.g, Paper.b, 0.9f);
            startHintText.alignment = TextAnchor.MiddleCenter;
            startHintText.text = "点击 或 按空格 开始织造";
            startHintText.raycastTarget = false;

            // === 结果反馈文字 ===
            var resultTextGo = CreateUI("ResultText", canvas.transform);
            PlaceCenter(resultTextGo, new Vector2(331f, 296f), new Vector2(500, 100));
            var resultText = resultTextGo.AddComponent<Text>();
            resultText.font = pixelFont;
            resultText.fontSize = 48;
            resultText.color = Color.white;
            resultText.alignment = TextAnchor.MiddleCenter;
            resultText.text = "";
            resultText.raycastTarget = false;
            resultTextGo.SetActive(false);

            // === 底部按钮区 ===
            var btnRow = CreateUI("Buttons", canvas.transform);
            PlaceCenter(btnRow, new Vector2(0f, -450f), new Vector2(460, 80));

            var resetBtnGo = CreateUI("ResetBtn", btnRow.transform);
            PlaceCenter(resetBtnGo, new Vector2(-115, 0), new Vector2(200, 70));
            var resetBtnBg = resetBtnGo.AddComponent<Image>();
            resetBtnBg.color = new Color(0.5f, 0.35f, 0.22f);
            resetBtnBg.raycastTarget = true;
            var resetBtn = resetBtnGo.AddComponent<Button>();
            var resetLabelGo = CreateUI("Label", resetBtnGo.transform);
            PlaceFull(resetLabelGo);
            var resetLabel = resetLabelGo.AddComponent<Text>();
            resetLabel.font = pixelFont;
            resetLabel.fontSize = 30;
            resetLabel.color = Paper;
            resetLabel.alignment = TextAnchor.MiddleCenter;
            resetLabel.text = "重置";

            var closeBtnGo = CreateUI("CloseBtn", btnRow.transform);
            PlaceCenter(closeBtnGo, new Vector2(115, 0), new Vector2(200, 70));
            var closeBtnBg = closeBtnGo.AddComponent<Image>();
            closeBtnBg.color = new Color(0.5f, 0.35f, 0.22f);
            closeBtnBg.raycastTarget = true;
            var closeBtn = closeBtnGo.AddComponent<Button>();
            var closeLabelGo = CreateUI("Label", closeBtnGo.transform);
            PlaceFull(closeLabelGo);
            var closeLabel = closeLabelGo.AddComponent<Text>();
            closeLabel.font = pixelFont;
            closeLabel.fontSize = 30;
            closeLabel.color = Paper;
            closeLabel.alignment = TextAnchor.MiddleCenter;
            closeLabel.text = "关闭";

            // === 通关弹窗 ===
            var completePanelGo = CreateUI("CompletePanel", canvas.transform);
            PlaceFull(completePanelGo);
            var completePanelRt = completePanelGo.GetComponent<RectTransform>();
            var dimGo = CreateUI("Dim", completePanelGo.transform);
            PlaceFull(dimGo);
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = DimBlack;
            dimImg.raycastTarget = true;

            var completeBodyGo = CreateUI("Body", completePanelGo.transform);
            PlaceCenter(completeBodyGo, Vector2.zero, new Vector2(560, 320));
            var completeBodyImg = completeBodyGo.AddComponent<Image>();
            completeBodyImg.color = Paper;
            completeBodyImg.raycastTarget = false;

            var completeTextGo = CreateUI("Text", completeBodyGo.transform);
            PlaceCenter(completeTextGo, new Vector2(0, 50), new Vector2(500, 70));
            var completeText = completeTextGo.AddComponent<Text>();
            completeText.font = pixelFont;
            completeText.fontSize = 48;
            completeText.color = Brick;
            completeText.alignment = TextAnchor.MiddleCenter;
            completeText.text = "织造完成！";
            completeText.raycastTarget = false;

            var continueBtnGo = CreateUI("ContinueBtn", completeBodyGo.transform);
            PlaceCenter(continueBtnGo, new Vector2(0, -60), new Vector2(220, 70));
            var continueBtnBg = continueBtnGo.AddComponent<Image>();
            continueBtnBg.color = Brick;
            continueBtnBg.raycastTarget = true;
            var continueBtn = continueBtnGo.AddComponent<Button>();
            var continueLabelGo = CreateUI("Label", continueBtnGo.transform);
            PlaceFull(continueLabelGo);
            var continueLabel = continueLabelGo.AddComponent<Text>();
            continueLabel.font = pixelFont;
            continueLabel.fontSize = 28;
            continueLabel.color = Paper;
            continueLabel.alignment = TextAnchor.MiddleCenter;
            continueLabel.text = "继续";

            completePanelGo.SetActive(false);

            // === 管理器 GameObject ===
            var mgrGo = new GameObject("GameManager");
            var mgr = mgrGo.AddComponent<LoomRepairGameManager>();
            mgr.autoProgressPerSecond = 4f; // 加快自动织造速度

            // === 校准面板控制器 ===
            var calibController = calibRootGo.AddComponent<CalibrationPanelController>();
            calibController.panelImage = panelBodyImg;
            calibController.hitZoneImage = null; // 不再显示打击区域图片，但判定逻辑仍在
            calibController.pointerRect = pointerRt;
            calibController.swingSpeed = 209f; // 用户调后：220 × 0.95
            // 默认参数已经在 Inspector 上配置好，可以按需调整

            // === UI 控制器 ===
            var ui = canvasGo.AddComponent<LoomRepairUI>();
            ui.progressFill = progFillImg;
            ui.progressText = progText;
            ui.startHint = startHintGo;
            ui.resultText = resultText;
            ui.completePanel = completePanelGo;
            ui.continueButton = continueBtn;
            ui.completeText = completeText;
            ui.loomImage = loomImg;

            // === 绑定引用 ===
            mgr.calibrationPanel = calibController;
            mgr.ui = ui;

            // === 按钮事件 ===
            resetBtn.onClick.AddListener(() => mgr.ResetGame());
            closeBtn.onClick.AddListener(() => mgr.CloseGame());
            continueBtn.onClick.AddListener(() => mgr.CloseGame());

            // 开始提示区域也可点击开始 — 先移除原 Text，重建为可点击区域
            var oldText = startHintGo.GetComponent<Text>();
            if (oldText != null) Object.DestroyImmediate(oldText);
            var startHintImg = startHintGo.AddComponent<Image>();
            startHintImg.color = new Color(1f, 1f, 1f, 0f); // 透明底接收点击
            startHintImg.raycastTarget = true;
            var startBtnClick = startHintGo.AddComponent<Button>();
            startBtnClick.targetGraphic = startHintImg;
            startBtnClick.onClick.AddListener(() => mgr.StartGame());
            // 在 Button 下重建文字作为子物体
            var hintLabelGo = CreateUI("HintLabel", startHintGo.transform);
            PlaceFull(hintLabelGo);
            var hintLabel = hintLabelGo.AddComponent<Text>();
            hintLabel.font = pixelFont;
            hintLabel.fontSize = 36;
            hintLabel.color = new Color(Paper.r, Paper.g, Paper.b, 0.9f);
            hintLabel.alignment = TextAnchor.MiddleCenter;
            hintLabel.text = "点击 或 按空格 开始织造";
            hintLabel.raycastTarget = false;

            // === 保存场景 ===
            EditorSceneManager.SaveScene(scene, ScenePath);

            // === 设置所有 Text 使用像素字体 ===
            var allTexts = Object.FindObjectsOfType<Text>(true);
            foreach (var t in allTexts)
            {
                t.font = pixelFont;
                EditorUtility.SetDirty(t);
            }

            // === 设置所有 Image 使用 Point filter (Canvas 会自动处理) ===
            var allImgs = Object.FindObjectsOfType<Image>(true);
            foreach (var img in allImgs)
            {
                EditorUtility.SetDirty(img);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LoomRepairBuilder] ✅ 构建完成：" + ScenePath);
        }

        // ============================================================
        // 工具函数
        // ============================================================

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + "/" + name);
        }

        private static void ConfigureTextures()
        {
            string[] files = Directory.GetFiles(RootDir, "*.png");
            foreach (string f in files)
            {
                string path = f.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                var s = new TextureImporterSettings();
                importer.ReadTextureSettings(s);
                s.spriteMeshType = SpriteMeshType.FullRect;
                s.spritePivot = new Vector2(0.5f, 0.5f);
                importer.SetTextureSettings(s);
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'),
                    Path.GetFileName(path));
            }
        }

        private static GameObject CreateUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void PlaceFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static RectTransform PlaceCenter(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }
    }
}
#endif
