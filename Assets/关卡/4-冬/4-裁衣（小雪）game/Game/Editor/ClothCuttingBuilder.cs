#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JiangnanDye.ClothCutting.EditorTools
{
    /// <summary>
    /// 一键构建裁衣小游戏。
    /// 菜单：Tools/江南裁衣/一键构建裁衣小游戏
    /// </summary>
    public static class ClothCuttingBuilder
    {
        private const string RootDir = "Assets/4-裁衣game";
        private const string ArtDir = RootDir;
        private const string ScriptsDir = RootDir + "/Game/Scripts";
        private const string ScenePath = RootDir + "/Game/Scenes/ClothCutting.unity";
        private const string GlowPath = "Assets/4-寻找染料game/Game/Art/UI/Soft_Glow.png";

        // 工作台图片原始尺寸（像素）
        private const float WorkbenchW = 845f;
        private const float WorkbenchH = 543f;

        private static readonly Color Dark = new Color(0.23f, 0.17f, 0.12f);
        private static readonly Color Paper = new Color(0.96f, 0.89f, 0.75f);
        private static readonly Color Brick = new Color(0.66f, 0.27f, 0.18f);
        private static readonly Color Gold = new Color(0.75f, 0.6f, 0.33f);
        private static readonly Color Ink = new Color(0.2f, 0.15f, 0.1f);

        [MenuItem("Tools/江南裁衣/一键构建裁衣小游戏")]
        public static void Build()
        {
            Debug.Log("[ClothCuttingBuilder] 开始构建...");

            EnsureFolder("Assets/4-裁衣game/Game");
            EnsureFolder("Assets/4-裁衣game/Game/Scenes");
            ConfigureTextures();
            AssetDatabase.Refresh();

            Sprite workbench = LoadSprite("4-裁衣工作台.png");
            Sprite scissors = LoadSprite("4-剪刀.png");
            Sprite collection = LoadSprite("4-已裁衣片.png");
            Sprite progress = LoadSprite("4-裁剪进度.png");
            Sprite sleeve = LoadSprite("4-袖片.png");
            Sprite front = LoadSprite("4-衣片-前.png");
            Sprite back = LoadSprite("4-衣片-后.png");
            Font pixelFont = WorkshopArt.PixelFont;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 相机
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.08f, 0.06f, 0.05f);
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // EventSystem
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // Canvas — 用 ScreenSpaceOverlay（与梭子/拨子场景保持一致）
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // ------ 背景 ------
            var bg = CreateUI("Background", canvas.transform);
            PlaceFull(bg);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.14f, 0.1f);
            bgImg.raycastTarget = false;

            // ------ 工作台（中央）放大 1.2×，让裁剪更顺手但不溢出 ------
            const float WbScale = 1.2f;
            float wbW = WorkbenchW * WbScale;  // 1014
            float wbH = WorkbenchH * WbScale;  // 652
            var wbGo = CreateUI("Workbench", canvas.transform);
            PlaceCenter(wbGo, new Vector2(169f, -49f), new Vector2(wbW, wbH));
            var wbImg = wbGo.AddComponent<Image>();
            wbImg.sprite = workbench;
            wbImg.preserveAspect = true;
            wbImg.color = Color.white;
            wbImg.raycastTarget = true; // 必须能接收点击（WorkbenchCuttingHandler 需要）

            var wbRt = wbGo.GetComponent<RectTransform>();

            // 裁剪交互处理器（用 UGUI 事件系统代替有 bug 的 Input.mousePosition）
            var wbHandler = wbGo.AddComponent<WorkbenchCuttingHandler>();

            // ------ 3 个裁剪识别区（用户手动调整后的位置，反推 rectCoord） ------
            // 反推：anchoredPos = rectCoord - wbCenter(507,325.8)
            //   Front anchoredPos=(-155, 96) → rectCoord=(352, 421.8)
            //   Back  anchoredPos=(159, 91)  → rectCoord=(666, 416.8)
            //   Sleeve anchoredPos=(1, -98)  → rectCoord=(508, 227.8)
            const float AreaSize = 280f;
            Image areaFront  = CreateAreaImage(wbRt, "Area_Front",  front,  new Vector2(352f, 422f), AreaSize, flip: false);
            Image areaBack   = CreateAreaImage(wbRt, "Area_Back",   back,   new Vector2(666f, 417f), AreaSize, flip: false);
            Image areaSleeve = CreateAreaImage(wbRt, "Area_Sleeve", sleeve, new Vector2(508f, 228f), AreaSize, flip: false);

            // ------ 裁剪轨迹渲染层（叠在工作台之上、剪刀之下） ------
            var trailGo = CreateUI("CuttingTrail", canvas.transform);
            // 让 trail 覆盖整个 Canvas 区域，Render 内部自己做坐标转换
            var trailRt = trailGo.GetComponent<RectTransform>();
            trailRt.anchorMin = Vector2.zero;
            trailRt.anchorMax = Vector2.one;
            trailRt.offsetMin = Vector2.zero;
            trailRt.offsetMax = Vector2.zero;
            var trailRenderer = trailGo.AddComponent<CuttingPathRenderer>();
            trailRenderer.raycastTarget = false; // 覆盖全屏的 Graphic 必须禁 raycast，否则吃掉所有点击
            trailGo.SetActive(false); // 禁用裁剪线显示（用户不需要黄色线）

            // ------ 右侧收集栏（同步缩小到 1.2×） ------
            float colW = 331 * WbScale;   // 397
            float colH = 533 * WbScale;   // 640
            var colGo = CreateUI("Collection", canvas.transform);
            PlaceCenter(colGo, new Vector2(-477f, 1f), new Vector2(colW, colH));
            var colImg = colGo.AddComponent<Image>();
            colImg.sprite = collection;
            colImg.preserveAspect = true;
            colImg.color = Color.white;
            colImg.raycastTarget = false;
            var colRt = colGo.GetComponent<RectTransform>();

            // 在收集栏内放 3 个槽位（用户手动调整后的位置）
            float slotSizeW = 120f;
            float slotSizeH = 84f;
            var slotFront = CreateUI("Slot_Front", colRt);
            PlaceCenter(slotFront, new Vector2(3f, 116f), new Vector2(slotSizeW, slotSizeH));
            var slotBack = CreateUI("Slot_Back", colRt);
            PlaceCenter(slotBack, new Vector2(3f, -47f), new Vector2(slotSizeW, slotSizeH));
            var slotSleeve = CreateUI("Slot_Sleeve", colRt);
            PlaceCenter(slotSleeve, new Vector2(0f, -196f), new Vector2(slotSizeW, slotSizeH));

            // ------ 底部进度条 ------
            var progBgGo = CreateUI("ProgressBg", canvas.transform);
            PlaceCenter(progBgGo, new Vector2(167f, 291f), new Vector2(691, 117));
            var progBgImg = progBgGo.AddComponent<Image>();
            progBgImg.sprite = progress;
            progBgImg.preserveAspect = true;
            progBgImg.color = Color.white;
            progBgImg.raycastTarget = false;

            // 进度条内的填充层（在底图之上）
            var progFillGo = CreateUI("ProgressFill", progBgGo.transform);
            var progFillRt = progFillGo.GetComponent<RectTransform>();
            progFillRt.anchorMin = new Vector2(0.08f, 0.3f);
            progFillRt.anchorMax = new Vector2(0.92f, 0.7f);
            progFillRt.offsetMin = new Vector2(89f, 5f);
            progFillRt.offsetMax = new Vector2(89f, 5f);
            var progFillImg = progFillGo.AddComponent<Image>();
            progFillImg.color = new Color(0.76f, 0.3f, 0.2f, 0.9f);
            progFillImg.type = Image.Type.Filled;
            progFillImg.fillMethod = Image.FillMethod.Horizontal;
            progFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            progFillImg.fillAmount = 0f;
            progFillImg.raycastTarget = false;

            // 进度文字
            var progTextGo = CreateUI("ProgressText", progBgGo.transform);
            PlaceCenter(progTextGo, new Vector2(91f, 4f), new Vector2(600, 50));
            var progText = progTextGo.AddComponent<Text>();
            progText.fontSize = 32;
            progText.color = Color.white;
            progText.alignment = TextAnchor.MiddleCenter;
            progText.text = "0%";
            progText.raycastTarget = false;

            // ------ 剪刀按钮（仿梭子/拨子：透明底 + 选中时金色光晕，位置固定在收集栏下方） ------
            var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath);
            var scGo = CreateUI("ScissorsButton", canvas.transform);
            var scRt = scGo.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.5f, 0.5f);
            scRt.anchorMax = new Vector2(0.5f, 0.5f);
            scRt.pivot = new Vector2(0.5f, 0.5f);
            scRt.anchoredPosition = new Vector2(669f, -258f);
            scRt.sizeDelta = new Vector2(200f, 200f);

            // 透明点击区（无底色卡片，仿梭子/拨子的 CreateToolButton）
            var hitImg = scGo.AddComponent<Image>();
            hitImg.color = new Color(1f, 1f, 1f, 0f);

            // 选中态金色光晕（仿梭子/拨子的 Highlight，默认隐藏）
            var hlGo = CreateUI("Highlight", scRt);
            var hlRt = hlGo.GetComponent<RectTransform>();
            hlRt.anchorMin = new Vector2(0.5f, 0.5f);
            hlRt.anchorMax = new Vector2(0.5f, 0.5f);
            hlRt.pivot = new Vector2(0.5f, 0.5f);
            hlRt.anchoredPosition = Vector2.zero;
            hlRt.sizeDelta = new Vector2(260f, 260f); // 父级 200 + 向外扩 30×2
            var hlImg = hlGo.AddComponent<Image>();
            hlImg.sprite = glowSprite;
            hlImg.color = new Color(1f, 0.93f, 0.72f, 0.62f);
            hlImg.preserveAspect = true;
            hlImg.raycastTarget = false;
            hlGo.SetActive(false); // 默认隐藏，激活裁剪模式才显示

            // 剪刀图标（透明底 PNG 原样显示，居中）
            var scImgGo = CreateUI("Icon", scRt);
            var scImgRt = scImgGo.GetComponent<RectTransform>();
            scImgRt.anchorMin = new Vector2(0.5f, 0.5f);
            scImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            scImgRt.pivot = new Vector2(0.5f, 0.5f);
            scImgRt.sizeDelta = new Vector2(150f, 140f);
            scImgRt.anchoredPosition = Vector2.zero;
            var scImg = scImgGo.AddComponent<Image>();
            scImg.sprite = scissors;
            scImg.preserveAspect = true;
            scImg.color = Color.white;
            scImg.raycastTarget = false;

            // Button + 控制器
            var btn = scGo.AddComponent<Button>();
            btn.targetGraphic = hitImg;
            var scissorsCtrl = scGo.AddComponent<ScissorsController>();
            scissorsCtrl.scissorsImage = scImg;
            scissorsCtrl.highlightImage = hlImg;
            scissorsCtrl.canvas = canvas;
            scissorsCtrl.tipOffset = new Vector2(30f, -20f);
            scissorsCtrl.buttonAnchoredPos = new Vector2(669f, -258f);
            scissorsCtrl.buttonRt = scRt;

            // ------ 飞行剪刀光标（Canvas 直接子级，默认隐藏，跟随鼠标） ------
            var flyGo = CreateUI("FlyingScissors", canvas.transform);
            var flyRt = flyGo.GetComponent<RectTransform>();
            flyRt.anchorMin = new Vector2(0.5f, 0.5f);
            flyRt.anchorMax = new Vector2(0.5f, 0.5f);
            flyRt.pivot = new Vector2(0.5f, 0.5f);
            flyRt.sizeDelta = new Vector2(150f, 140f);
            flyRt.anchoredPosition = scissorsCtrl.buttonAnchoredPos;
            var flyImg = flyGo.AddComponent<Image>();
            flyImg.sprite = scissors;
            flyImg.preserveAspect = true;
            flyImg.color = Color.white;
            flyImg.raycastTarget = false;
            flyGo.SetActive(false); // 默认隐藏
            flyGo.transform.SetAsLastSibling(); // 让光标在最上层

            scissorsCtrl.scissorsCursorRt = flyRt;
            scissorsCtrl.scissorsCursorImage = flyImg;

            // ------ 顶部标题 ------
            var titleGo = CreateUI("Title", canvas.transform);
            PlaceCenter(titleGo, new Vector2(64f, 449f), new Vector2(600, 70));
            var titleText = titleGo.AddComponent<Text>();
            titleText.fontSize = 52;
            titleText.color = Paper;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "裁 衣";
            titleText.raycastTarget = false;

            // ------ 按钮区 ------
            var btnRow = CreateUI("Buttons", canvas.transform);
            PlaceCenter(btnRow, new Vector2(64f, -430f), new Vector2(360, 80));

            var resetBtnGo = CreateUI("ResetBtn", btnRow.transform);
            PlaceCenter(resetBtnGo, new Vector2(-90, 0), new Vector2(160, 70));
            var resetBtnBg = resetBtnGo.AddComponent<Image>();
            resetBtnBg.color = new Color(0.5f, 0.35f, 0.22f);
            resetBtnBg.raycastTarget = true;
            var resetBtn = resetBtnGo.AddComponent<Button>();
            var resetLabelGo = CreateUI("Label", resetBtnGo.transform);
            PlaceFull(resetLabelGo);
            var resetLabel = resetLabelGo.AddComponent<Text>();
            resetLabel.fontSize = 30;
            resetLabel.color = Paper;
            resetLabel.alignment = TextAnchor.MiddleCenter;
            resetLabel.text = "重置";

            var closeBtnGo = CreateUI("CloseBtn", btnRow.transform);
            PlaceCenter(closeBtnGo, new Vector2(90, 0), new Vector2(160, 70));
            var closeBtnBg = closeBtnGo.AddComponent<Image>();
            closeBtnBg.color = new Color(0.5f, 0.35f, 0.22f);
            closeBtnBg.raycastTarget = true;
            var closeBtn = closeBtnGo.AddComponent<Button>();
            var closeLabelGo = CreateUI("Label", closeBtnGo.transform);
            PlaceFull(closeLabelGo);
            var closeLabel = closeLabelGo.AddComponent<Text>();
            closeLabel.fontSize = 30;
            closeLabel.color = Paper;
            closeLabel.alignment = TextAnchor.MiddleCenter;
            closeLabel.text = "关闭";

            // ------ 通关弹窗 ------
            var completePanelGo = CreateUI("CompletePanel", canvas.transform);
            PlaceFull(completePanelGo);
            var completePanelRt = completePanelGo.GetComponent<RectTransform>();
            var dimGo = CreateUI("Dim", completePanelGo.transform);
            PlaceFull(dimGo);
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.6f);
            dimImg.raycastTarget = true;

            var panelBody = CreateUI("Body", completePanelGo.transform);
            PlaceCenter(panelBody, Vector2.zero, new Vector2(500, 300));
            var panelBodyImg = panelBody.AddComponent<Image>();
            panelBodyImg.color = Paper;
            panelBodyImg.raycastTarget = false;

            var completeTextGo = CreateUI("Text", panelBody.transform);
            PlaceCenter(completeTextGo, new Vector2(0, 40), new Vector2(460, 60));
            var completeText = completeTextGo.AddComponent<Text>();
            completeText.fontSize = 40;
            completeText.color = Brick;
            completeText.alignment = TextAnchor.MiddleCenter;
            completeText.text = "裁剪完成！";
            completeText.raycastTarget = false;

            var continueBtnGo = CreateUI("ContinueBtn", panelBody.transform);
            PlaceCenter(continueBtnGo, new Vector2(0, -60), new Vector2(200, 60));
            var continueBtnBg = continueBtnGo.AddComponent<Image>();
            continueBtnBg.color = new Color(0.66f, 0.27f, 0.18f);
            continueBtnBg.raycastTarget = true;
            var continueBtn = continueBtnGo.AddComponent<Button>();
            var continueLabelGo = CreateUI("Label", continueBtnGo.transform);
            PlaceFull(continueLabelGo);
            var continueLabel = continueLabelGo.AddComponent<Text>();
            continueLabel.fontSize = 26;
            continueLabel.color = Paper;
            continueLabel.alignment = TextAnchor.MiddleCenter;
            continueLabel.text = "继续";

            completePanelGo.SetActive(false);

            // ------ 管理器 ------
            var mgrGo = new GameObject("GameManager");
            var mgr = mgrGo.AddComponent<ClothCuttingGameManager>();
            mgr.workbenchRect = wbRt;
            mgr.workbenchSize = new Vector2(WorkbenchW, WorkbenchH);
            mgr.frontSprite = front;
            mgr.backSprite = back;
            mgr.sleeveSprite = sleeve;
            mgr.scissors = scissorsCtrl;

            // 绑定 3 个裁剪识别区 RectTransform
            mgr.frontAreaRect = areaFront.GetComponent<RectTransform>();
            mgr.backAreaRect = areaBack.GetComponent<RectTransform>();
            mgr.sleeveAreaRect = areaSleeve.GetComponent<RectTransform>();

            // 路径点初始化（已自动 ×1.5 与放大后的工作台对齐）
            SetupDefaultPaths(mgr);

            // 给 WorkbenchCuttingHandler 注入依赖
            wbHandler.manager = mgr;
            wbHandler.scissorsController = scissorsCtrl;
            wbHandler.canvas = canvas;

            // 给 ScissorsController 也绑定 manager（保持兼容）
            scissorsCtrl.BindManager(mgr);

            // ------ UI 组件 ------
            var ui = canvasGo.AddComponent<ClothCuttingUI>();
            ui.progressBarBg = progBgImg;
            ui.progressBarFill = progFillImg;
            ui.progressText = progText;
            ui.collectionBg = colImg;
            ui.collectionFrontSlot = slotFront.GetComponent<RectTransform>();
            ui.collectionBackSlot = slotBack.GetComponent<RectTransform>();
            ui.collectionSleeveSlot = slotSleeve.GetComponent<RectTransform>();
            ui.resetButton = resetBtn;
            ui.closeButton = closeBtn;
            ui.completePanel = completePanelGo;
            ui.continueButton = continueBtn;
            ui.completeText = completeText;

            mgr.ui = ui;
            mgr.pixelFont = pixelFont; // 注入字体字段供运行时 ApplyPixelFont 使用
            trailRenderer.manager = mgr;

            // 按钮事件
            resetBtn.onClick.AddListener(() => mgr.ResetGame());
            closeBtn.onClick.AddListener(() => mgr.CloseGame());

            // 统一像素字体（同步赋值 + SetDirty + 立刻保存）
            if (pixelFont != null)
            {
                var allTexts = Object.FindObjectsOfType<UnityEngine.UI.Text>(true);
                foreach (var t in allTexts)
                {
                    t.font = pixelFont;
                    t.fontStyle = FontStyle.Normal;
                    EditorUtility.SetDirty(t);
                }
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[ClothCuttingBuilder] 构建完成：" + ScenePath);
        }

        private static readonly float PathScale = 1.2f; // 工作台放大 1.2×，路径点同步放大

        /// <summary>
        /// 初始化四条裁剪路径点。
        /// 坐标系：工作台 RectTransform.rect 左下角为 (0,0)，单位为像素。
        /// 路径点基于原始 845×543 轮廓估算，统一 ×1.5 与放大后的工作台对齐。
        /// </summary>
        private static void SetupDefaultPaths(ClothCuttingGameManager mgr)
        {
            mgr.frontPath.tolerance = 45f;
            mgr.backPath.tolerance = 45f;
            mgr.sleevePath.tolerance = 45f;

            mgr.frontPath.points = ScalePoints(new[]
            {
                new Vector2(285, 440), new Vector2(345, 430), new Vector2(370, 400),
                new Vector2(350, 375), new Vector2(378, 290), new Vector2(355, 200),
                new Vector2(225, 200), new Vector2(200, 290), new Vector2(190, 378),
                new Vector2(170, 408), new Vector2(200, 432), new Vector2(235, 440),
                new Vector2(255, 420), new Vector2(285, 420),
            });

            mgr.backPath.points = ScalePoints(new[]
            {
                new Vector2(580, 455), new Vector2(650, 445), new Vector2(720, 428),
                new Vector2(755, 400), new Vector2(745, 372), new Vector2(770, 300),
                new Vector2(750, 210), new Vector2(615, 210), new Vector2(590, 298),
                new Vector2(560, 375), new Vector2(528, 402), new Vector2(545, 432),
            });

            // 单片袖片（合并左右轮廓取中心位置）
            mgr.sleevePath.points = ScalePoints(new[]
            {
                new Vector2(345, 360), new Vector2(505, 362), new Vector2(525, 310),
                new Vector2(535, 240), new Vector2(505, 145), new Vector2(400, 90),
                new Vector2(300, 145), new Vector2(285, 240), new Vector2(320, 310),
            });

            mgr.frontPath.Initialize();
            mgr.backPath.Initialize();
            mgr.sleevePath.Initialize();
        }

        /// <summary>在父级下创建一个半透明 Image 作为裁剪识别区。
        /// 统一 sizeDelta + preserveAspect=true，保证绝不拉伸。</summary>
        private static Image CreateAreaImage(Transform parent, string name, Sprite sprite,
            Vector2 centerRectCoord, float size, bool flip)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            Vector2 wbCenter = new Vector2(WorkbenchW * PathScale * 0.5f, WorkbenchH * PathScale * 0.5f);
            rt.anchoredPosition = centerRectCoord - wbCenter;
            rt.sizeDelta = new Vector2(size, size);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true; // 关键：绝不拉伸
            img.color = new Color(1f, 1f, 1f, 0.2f);
            img.raycastTarget = false;
            if (flip)
            {
                Vector3 s = rt.localScale;
                s.x = -Mathf.Abs(s.x);
                rt.localScale = s;
            }
            return img;
        }

        /// <summary>动态生成一个圆形 sprite 作为光圈背景。</summary>
        private static Sprite CreateCircleSprite()
        {
            const int res = 128;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float cx = res * 0.5f, cy = res * 0.5f;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float edge = 2f;
                float a = Mathf.Clamp01(res * 0.5f - d);
                float inside = Mathf.Clamp01(d - (res * 0.5f - edge));
                tex.SetPixel(x, y, new Color(1, 1, 1, inside * a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        }

        private static List<Vector2> ScalePoints(Vector2[] raw)
        {
            var list = new List<Vector2>(raw.Length);
            for (int i = 0; i < raw.Length; i++) list.Add(raw[i] * PathScale);
            return list;
        }

        /// <summary>在父级下创建一个半透明 Image 作为裁剪识别区。</summary>
        private static Image CreateAreaImage(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, bool flip)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // parent 是 wbRt，rect 左下角 (0,0)；wbRt.anchoredPosition 已经 (-250,20)
            // 子级 anchoredPosition 是相对于 wbRt 的中心 (size/2, size/2) 的偏移
            // 我们传入的 center 是 rect 坐标（左下 0,0），需要转成子级 anchoredPosition
            // 子级 parent 的中心在 rect 的 (wbSize/2, wbSize/2)，所以：
            // child.anchoredPosition = center - (wbSize/2, wbSize/2)
            Vector2 wbCenter = new Vector2(WorkbenchW * PathScale * 0.5f, WorkbenchH * PathScale * 0.5f);
            rt.anchoredPosition = center - wbCenter;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = new Color(1f, 1f, 1f, 0.2f); // 半透明参考底图
            img.raycastTarget = false;
            if (flip)
            {
                Vector3 s = rt.localScale;
                s.x = -Mathf.Abs(s.x);
                rt.localScale = s;
            }
            return img;
        }

        // ------------------------------------------------------------------
        // 工具函数
        // ------------------------------------------------------------------
        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + name);
        }

        private static void ConfigureTextures()
        {
            string[] files = System.IO.Directory.GetFiles(ArtDir, "*.png");
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
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'),
                    System.IO.Path.GetFileName(path));
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
