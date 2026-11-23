#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Qingming.Harvest.EditorTools
{
    /// <summary>
    /// 一键构建清明采桑小游戏。
    /// 菜单：Tools/清明采桑/一键构建采桑小游戏
    /// </summary>
    public static class HarvestBuilder
    {
        private const string RootDir = "Assets/关卡/1-春/1-采桑game";
        private const string GameDir = RootDir + "/Game";
        private const string ScriptsDir = GameDir + "/Scripts";
        private const string EditorDir = GameDir + "/Editor";
        private const string ScenesDir = GameDir + "/Scenes";
        private const string ScenePath = ScenesDir + "/MulberryHarvest.unity";

        private const float PPU = 100f;
        private const float CamSize = 4.5f; // 适配 16:9: orthoSize 4.5 → 屏幕高 9 单位 → 宽 16 单位

        private static readonly Color Paper = new Color(0.96f, 0.89f, 0.75f);
        private static readonly Color Ink = new Color(0.2f, 0.15f, 0.1f);
        private static readonly Color Brick = new Color(0.66f, 0.27f, 0.18f);

        private static Font _pixelFont;
        private static Sprite _pathSprite;
        private static Sprite[] _leafSprites;
        private static Sprite[] _basketSprites;
        private static RuntimeAnimatorController _playerAnimatorCtrl;

        [MenuItem("Tools/清明采桑/一键构建采桑小游戏")]
        public static void Build()
        {
            Debug.Log("[HarvestBuilder] 开始构建清明采桑小游戏...");

            _playerAnimatorCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(GameDir + "/HarvestPlayer.controller");
            if (_playerAnimatorCtrl == null)
            {
                Debug.LogError("[HarvestBuilder] 缺少采桑玩家动画控制器，已取消构建。");
                return;
            }

            EnsureFolders();
            ConfigureTextures();
            AssetDatabase.Refresh();
            LoadAssets();
            BuildScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HarvestBuilder] 构建完成：" + ScenePath);
        }

        // ==============================================================
        // 基础
        // ==============================================================
        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(GameDir)) AssetDatabase.CreateFolder(RootDir, "Game");
            if (!AssetDatabase.IsValidFolder(ScriptsDir)) AssetDatabase.CreateFolder(GameDir, "Scripts");
            if (!AssetDatabase.IsValidFolder(EditorDir)) AssetDatabase.CreateFolder(GameDir, "Editor");
            if (!AssetDatabase.IsValidFolder(ScenesDir)) AssetDatabase.CreateFolder(GameDir, "Scenes");
        }

        private static void ConfigureTextures()
        {
            // 场景图
            ConfigureOne(RootDir + "/1-采桑小路.png", PPU, new Vector2(0.5f, 0.5f));
            // 篮子（底部中心锚点，避免形态切换时跳动）
            for (int i = 1; i <= 4; i++)
                ConfigureOne(RootDir + $"/1-采桑篮{i}.png", PPU, new Vector2(0.5f, 0f));
            // 桑叶（中心锚点，方便旋转）
            for (int i = 1; i <= 7; i++)
                ConfigureOne(RootDir + $"/1-桑叶{i}.png", PPU, new Vector2(0.5f, 0.5f));
        }

        private static void ConfigureOne(string path, float ppu, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void LoadAssets()
        {
            _pixelFont = WorkshopArt.PixelFont;

            _pathSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + "/1-采桑小路.png");

            _basketSprites = new Sprite[4];
            for (int i = 0; i < 4; i++)
                _basketSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + $"/1-采桑篮{i + 1}.png");

            _leafSprites = new Sprite[7];
            for (int i = 0; i < 7; i++)
                _leafSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + $"/1-桑叶{i + 1}.png");

        }

        // ==============================================================
        // 场景构建
        // ==============================================================
        private static void BuildScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ------- 摄像机 -------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.backgroundColor = new Color(0.78f, 0.88f, 0.75f); // 淡春绿
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // ------- EventSystem -------
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // ------- 前景（采桑小路） -------
            var pathGo = new GameObject("Path");
            var pathSr = pathGo.AddComponent<SpriteRenderer>();
            pathSr.sprite = _pathSprite;
            pathSr.sortingOrder = 0;
            pathGo.transform.localScale = new Vector3(0.57f, 0.57f, 0.77f);
            pathGo.transform.position = new Vector3(0f, -1.45f, 0f);

            // ------- 玩家 -------
            var playerGo = CreatePlayer();

            // ------- 篮子（玩家子物体） -------
            var basketGo = CreateBasket(playerGo.transform);

            // ------- 桑叶生成器 -------
            var spawnerGo = new GameObject("MulberryLeafSpawner");
            var spawner = spawnerGo.AddComponent<MulberryLeafSpawner>();
            spawner.leafSprites = _leafSprites;

            // ------- Canvas + UI -------
            var canvas = CreateCanvas(cam);
            var uiController = CreateUI(canvas.transform);

            // ------- GameManager -------
            var mgrGo = new GameObject("HarvestGameManager");
            var mgr = mgrGo.AddComponent<MulberryHarvestGameManager>();
            mgr.targetLeaves = 8;
            mgr.player = playerGo.GetComponent<HarvestPlayerController>();
            mgr.basket = basketGo.GetComponent<HarvestBasketController>();
            mgr.spawner = spawner;
            mgr.ui = uiController;

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // --------------------------------------------------------------
        private static GameObject CreatePlayer()
        {
            var go = new GameObject("Player");
            // 出生位置：小路中央，地面上
            go.transform.position = new Vector3(0f, -2.5f, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            // Collider（玩家身体，用于 LeafCollector 触发）
            CapsuleCollider2D cap = go.AddComponent<CapsuleCollider2D>();
            cap.isTrigger = true;
            cap.direction = CapsuleDirection2D.Vertical;
            cap.size = new Vector2(1.0f, 1.5f);
            cap.offset = new Vector2(0f, 0.75f);

            // LeafCollector 触发器（稍大，覆盖身体+篮子区域）
            var collectorGo = new GameObject("LeafCollector");
            collectorGo.transform.SetParent(go.transform, false);
            var collectorCol = collectorGo.AddComponent<CapsuleCollider2D>();
            collectorCol.isTrigger = true;
            collectorCol.direction = CapsuleDirection2D.Vertical;
            collectorCol.size = new Vector2(2.2f, 2.2f);
            collectorCol.offset = new Vector2(0.5f, 1.0f); // 稍微偏右（篮子在右侧）
            collectorGo.AddComponent<LeafCollector>();

            // ---- Visual 子物体 ----
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.12f, 0f);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/animation/a-idle/待机_00001.png");
            sr.sortingOrder = 3;

            // 人物缩放（用户手动微调值）
            visual.transform.localScale = new Vector3(0.11f, 0.11f, 0.64f);

            // Animator
            Animator anim = null;
            if (_playerAnimatorCtrl != null)
            {
                anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = _playerAnimatorCtrl;
            }

            // Controller
            var ctrl = go.AddComponent<HarvestPlayerController>();
            ctrl.moveSpeed = 5f;
            ctrl.minX = -7f;
            ctrl.maxX = 7f;
            ctrl.spawnPosition = new Vector3(0f, -2.5f, 0f);
            ctrl.visualTransform = visual.transform;
            ctrl.animator = anim;

            return go;
        }

        private static GameObject CreateBasket(Transform playerTransform)
        {
            var go = new GameObject("HarvestBasket");
            go.transform.SetParent(null, false); // 篮子独立跟随，避免玩家旋转/翻转影响

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2; // 在玩家后面一点

            // 初始显示空篮
            if (_basketSprites != null && _basketSprites.Length > 0)
                sr.sprite = _basketSprites[0];

            // 缩放（与 HarvestBasketController 自身 LateUpdate 配合）
            go.transform.localScale = new Vector3(0.16f, 0.16f, 1f);

            var basketCtrl = go.AddComponent<HarvestBasketController>();
            basketCtrl.basketSprites = _basketSprites;

            return go;
        }

        private static Canvas CreateCanvas(Camera cam)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static HarvestUIController CreateUI(Transform canvas)
        {
            // ------- HUD：顶部进度 -------
            var hud = CreateUIObj("HUD", canvas);
            FullRect(hud);

            var progressBg = CreateUIObj("ProgressBg", hud.transform);
            PlaceTopLeft(progressBg, new Vector2(24f, -24f), new Vector2(420f, 72f));
            var bgImg = progressBg.AddComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0.7f);

            var progressTextGo = CreateUIObj("ProgressText", progressBg.transform);
            FullRect(progressTextGo);
            var progressText = progressTextGo.AddComponent<Text>();
            progressText.font = _pixelFont;
            progressText.fontSize = 36;
            progressText.text = "采桑进度：0/8";
            progressText.color = Ink;
            progressText.alignment = TextAnchor.MiddleCenter;

            // ------- 开场面板 -------
            var intro = CreateUIObj("IntroPanel", canvas);
            FullRect(intro);
            var introDim = CreateUIObj("Dim", intro.transform);
            FullRect(introDim);
            var introDimImg = introDim.AddComponent<Image>();
            introDimImg.color = new Color(0f, 0f, 0f, 0.55f);
            introDimImg.raycastTarget = true;

            var introBody = CreateUIObj("Body", intro.transform);
            PlaceCenter(introBody, Vector2.zero, new Vector2(720f, 480f));
            var introBg = introBody.AddComponent<Image>();
            introBg.color = Paper;

            var title = CreateUIObj("Title", introBody.transform);
            PlaceCenter(title, new Vector2(0f, 140f), new Vector2(640f, 60f));
            var titleText = title.AddComponent<Text>();
            titleText.font = _pixelFont;
            titleText.fontSize = 40;
            titleText.text = "清明采桑";
            titleText.color = Brick;
            titleText.alignment = TextAnchor.MiddleCenter;

            var desc = CreateUIObj("Desc", introBody.transform);
            PlaceCenter(desc, new Vector2(0f, 20f), new Vector2(640f, 240f));
            var descText = desc.AddComponent<Text>();
            descText.font = _pixelFont;
            descText.fontSize = 22;
            descText.text = "使用 A/D 或方向键左右移动\n接住飘落的桑叶，收集 8 片即可完成采桑";
            descText.color = Ink;
            descText.alignment = TextAnchor.MiddleCenter;
            descText.lineSpacing = 1.4f;

            var startBtnGo = CreateUIObj("StartButton", introBody.transform);
            PlaceCenter(startBtnGo, new Vector2(0f, -170f), new Vector2(240f, 80f));
            var startBtnBg = startBtnGo.AddComponent<Image>();
            startBtnBg.color = Brick;
            var startBtn = startBtnGo.AddComponent<Button>();
            var startLabelGo = CreateUIObj("Label", startBtnGo.transform);
            FullRect(startLabelGo);
            var startLabel = startLabelGo.AddComponent<Text>();
            startLabel.font = _pixelFont;
            startLabel.fontSize = 28;
            startLabel.text = "开 始";
            startLabel.color = Paper;
            startLabel.alignment = TextAnchor.MiddleCenter;

            // ------- 通关面板 -------
            var complete = CreateUIObj("CompletePanel", canvas);
            FullRect(complete);
            complete.SetActive(false);

            var completeDim = CreateUIObj("Dim", complete.transform);
            FullRect(completeDim);
            var completeDimImg = completeDim.AddComponent<Image>();
            completeDimImg.color = new Color(0f, 0f, 0f, 0.55f);
            completeDimImg.raycastTarget = true;

            var completeBody = CreateUIObj("Body", complete.transform);
            PlaceCenter(completeBody, Vector2.zero, new Vector2(600f, 360f));
            var completeBg = completeBody.AddComponent<Image>();
            completeBg.color = Paper;

            var completeTitle = CreateUIObj("Title", completeBody.transform);
            PlaceCenter(completeTitle, new Vector2(0f, 60f), new Vector2(540f, 80f));
            var completeTitleText = completeTitle.AddComponent<Text>();
            completeTitleText.font = _pixelFont;
            completeTitleText.fontSize = 44;
            completeTitleText.text = "采桑完成！";
            completeTitleText.color = Brick;
            completeTitleText.alignment = TextAnchor.MiddleCenter;

            var completeSub = CreateUIObj("Sub", completeBody.transform);
            PlaceCenter(completeSub, new Vector2(0f, -20f), new Vector2(540f, 60f));
            var completeSubText = completeSub.AddComponent<Text>();
            completeSubText.font = _pixelFont;
            completeSubText.fontSize = 22;
            completeSubText.text = "你已采满 8 片桑叶，可以回去缫丝了~";
            completeSubText.color = Ink;
            completeSubText.alignment = TextAnchor.MiddleCenter;

            var contBtnGo = CreateUIObj("ContinueButton", completeBody.transform);
            PlaceCenter(contBtnGo, new Vector2(0f, -110f), new Vector2(200f, 70f));
            var contBtnBg = contBtnGo.AddComponent<Image>();
            contBtnBg.color = Brick;
            var contBtn = contBtnGo.AddComponent<Button>();
            var contLabelGo = CreateUIObj("Label", contBtnGo.transform);
            FullRect(contLabelGo);
            var contLabel = contLabelGo.AddComponent<Text>();
            contLabel.font = _pixelFont;
            contLabel.fontSize = 26;
            contLabel.text = "继 续";
            contLabel.color = Paper;
            contLabel.alignment = TextAnchor.MiddleCenter;

            var uiController = hud.gameObject.AddComponent<HarvestUIController>();
            uiController.progressText = progressText;
            uiController.introPanel = intro;
            uiController.startButton = startBtn;
            uiController.completePanel = complete;
            uiController.continueButton = contBtn;

            return uiController;
        }

        // ==============================================================
        // UI 工具
        // ==============================================================
        private static GameObject CreateUIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform FullRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
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

        private static RectTransform PlaceTopLeft(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }
    }
}
#endif
