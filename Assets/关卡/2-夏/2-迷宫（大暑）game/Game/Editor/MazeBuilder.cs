#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JiangnanDye.MazeGame.EditorTools
{
    /// <summary>
    /// 一键构建秋日寻线迷宫。
    /// 菜单：Tools/江南迷宫/一键构建秋日迷宫
    /// </summary>
    public static class MazeBuilder
    {
        private const string RootDir = "Assets/3-迷宫（秋分）game";
        private const string ScriptsDir = RootDir + "/Game/Scripts";
        private const string ScenePath = RootDir + "/Game/Scenes/Maze_Autumn.unity";

        // 纹理尺寸（来自 TextureImporter 读取）
        private const float BgW = 1672f;
        private const float BgH = 941f;
        private const float PPU = 100f;

        // 世界大小
        private static readonly Vector2 WorldSize = new Vector2(BgW / PPU, BgH / PPU); // 16.72 x 9.41
        private static readonly Vector2 WorldCenter = Vector2.zero;

        // 相机 orthographicSize：让整张地图 (9.41) 完整显示
        private const float CamSize = 4.75f;

        // 颜色
        private static readonly Color Paper = new Color(0.96f, 0.89f, 0.75f);
        private static readonly Color Brick = new Color(0.66f, 0.27f, 0.18f);
        private static readonly Color Dark = new Color(0.23f, 0.17f, 0.12f);
        private static readonly Color Ink = new Color(0.2f, 0.15f, 0.1f);

        [MenuItem("Tools/江南迷宫/一键构建秋日迷宫")]
        public static void Build()
        {
            Debug.Log("[MazeBuilder] 开始构建秋日迷宫...");

            EnsureFolder(RootDir + "/Game");
            EnsureFolder(RootDir + "/Game/Scenes");
            EnsureFolder(RootDir + "/Game/Scripts");
            ConfigureTextures();
            AssetDatabase.Refresh();

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + "/background.png");
            Sprite goalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RootDir + "/3-成功道具.png");
            Font pixelFont = WorkshopArt.PixelFont;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ============== Main Camera ==============
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.backgroundColor = new Color(0.1f, 0.08f, 0.06f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0, 0, -10);

            // EventSystem
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // ============== Background ==============
            var bgGo = new GameObject("Background");
            bgGo.transform.position = new Vector3(WorldCenter.x, WorldCenter.y, 0);
            bgGo.transform.localScale = Vector3.one;
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = bgSprite;
            bgSr.sortingLayerName = "Background";
            bgSr.sortingOrder = 0;

            // ============== MazeWalls（碰撞体根节点） ==============
            var wallsRoot = new GameObject("MazeWalls");
            wallsRoot.transform.position = WorldCenter;

            // ------ 外围边界碰撞（挡住地图外） ------
            BuildBoundaryWalls(wallsRoot);

            // 内部树篱碰撞：自动分析背景图生成
            BuildInteriorWallsFromPixelMap(wallsRoot);

            // ============== Player ==============
            var playerGo = CreatePlayer(pixelFont);
            var playerCtrl = playerGo.GetComponent<MazePlayerController>();

            // ============== Goal（白色线头） ==============
            var goalGo = new GameObject("Goal_WhiteThread");
            // 终点位置：地图右侧空地（自动像素分析找到 hedgeRatio<15% 的最靠右位置）
            var goalPos = new Vector2(5.50f, 2.50f);
            goalGo.transform.position = new Vector3(goalPos.x, goalPos.y, 0);
            var goalSr = goalGo.AddComponent<SpriteRenderer>();
            goalSr.sprite = goalSprite;
            goalSr.sortingOrder = 5;
            // 线头 sprite 1254x1254 @ PPU=100，worldH=12.54，缩到 worldH≈0.7
            goalSr.transform.localScale = new Vector3(0.10f, 0.10f, 1f);

            var goalRb = goalGo.AddComponent<Rigidbody2D>();
            goalRb.bodyType = RigidbodyType2D.Kinematic;
            goalRb.gravityScale = 0f;
            goalRb.freezeRotation = true;

            var goalCol = goalGo.AddComponent<CircleCollider2D>();
            goalCol.isTrigger = true;
            goalCol.radius = 0.5f;

            var goalTrigger = goalGo.AddComponent<MazeGoalTrigger>();

            // ============== Canvas ==============
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // ------ InstructionPanel（操作提示） ------
            var instrRoot = CreateUI("InstructionPanel", canvas.transform);
            PlaceFull(instrRoot);
            // 半透明遮罩
            var dimGo = CreateUI("Dim", instrRoot.transform);
            PlaceFull(dimGo);
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.5f);
            dimImg.raycastTarget = true;

            // 面板主体
            var instrBody = CreateUI("Body", instrRoot.transform);
            PlaceCenter(instrBody, Vector2.zero, new Vector2(620, 380));
            var instrBodyImg = instrBody.AddComponent<Image>();
            instrBodyImg.color = Paper;
            instrBodyImg.raycastTarget = false;

            // 标题
            var titleGo = CreateUI("Title", instrBody.transform);
            PlaceCenter(titleGo, new Vector2(0, 110), new Vector2(560, 60));
            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text = "寻找线轴";
            titleTxt.fontSize = 40;
            titleTxt.color = Brick;
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // 说明
            var descGo = CreateUI("Desc", instrBody.transform);
            PlaceCenter(descGo, new Vector2(0, 20), new Vector2(540, 120));
            var descTxt = descGo.AddComponent<Text>();
            descTxt.text = "使用 WASD 或方向键移动\n找到迷宫尽头的线轴";
            descTxt.fontSize = 26;
            descTxt.color = Ink;
            descTxt.alignment = TextAnchor.MiddleCenter;

            // 开始按钮
            var startBtnGo = CreateUI("StartBtn", instrBody.transform);
            PlaceCenter(startBtnGo, new Vector2(0, -100), new Vector2(200, 70));
            var startBtnBg = startBtnGo.AddComponent<Image>();
            startBtnBg.color = Brick;
            var startBtn = startBtnGo.AddComponent<Button>();
            var startLabelGo = CreateUI("Label", startBtnGo.transform);
            PlaceFull(startLabelGo);
            var startLabel = startLabelGo.AddComponent<Text>();
            startLabel.text = "开 始";
            startLabel.fontSize = 28;
            startLabel.color = Paper;
            startLabel.alignment = TextAnchor.MiddleCenter;

            // ------ RewardPopup（奖励弹窗，默认隐藏） ------
            var rewardRoot = CreateUI("RewardPopup", canvas.transform);
            PlaceFull(rewardRoot);
            var rewardDimGo = CreateUI("Dim", rewardRoot.transform);
            PlaceFull(rewardDimGo);
            var rewardDimImg = rewardDimGo.AddComponent<Image>();
            rewardDimImg.color = new Color(0, 0, 0, 0.5f);
            rewardDimImg.raycastTarget = true;

            var rewardBody = CreateUI("Body", rewardRoot.transform);
            PlaceCenter(rewardBody, Vector2.zero, new Vector2(500, 400));
            var rewardBodyImg = rewardBody.AddComponent<Image>();
            rewardBodyImg.color = Paper;
            rewardBodyImg.raycastTarget = false;

            var rewardTitleGo = CreateUI("Title", rewardBody.transform);
            PlaceCenter(rewardTitleGo, new Vector2(0, 140), new Vector2(460, 50));
            var rewardTitleTxt = rewardTitleGo.AddComponent<Text>();
            rewardTitleTxt.text = "获得物品";
            rewardTitleTxt.fontSize = 34;
            rewardTitleTxt.color = Brick;
            rewardTitleTxt.alignment = TextAnchor.MiddleCenter;

            // 白色线头图标（UI Image 用 Canvas 坐标）
            var rewardIconGo = CreateUI("ItemIcon", rewardBody.transform);
            PlaceCenter(rewardIconGo, new Vector2(0, 30), new Vector2(150, 150));
            var rewardIconImg = rewardIconGo.AddComponent<Image>();
            rewardIconImg.sprite = goalSprite;
            rewardIconImg.preserveAspect = true;

            var rewardNameGo = CreateUI("ItemName", rewardBody.transform);
            PlaceCenter(rewardNameGo, new Vector2(0, -55), new Vector2(460, 40));
            var rewardNameTxt = rewardNameGo.AddComponent<Text>();
            rewardNameTxt.text = "线轴 × 1";
            rewardNameTxt.fontSize = 26;
            rewardNameTxt.color = Ink;
            rewardNameTxt.alignment = TextAnchor.MiddleCenter;

            var rewardDescGo = CreateUI("ItemDesc", rewardBody.transform);
            PlaceCenter(rewardDescGo, new Vector2(0, -100), new Vector2(460, 50));
            var rewardDescTxt = rewardDescGo.AddComponent<Text>();
            rewardDescTxt.text = "洁白柔韧的丝线";
            rewardDescTxt.fontSize = 18;
            rewardDescTxt.color = new Color(0.4f, 0.3f, 0.2f);
            rewardDescTxt.alignment = TextAnchor.MiddleCenter;

            var finishBtnGo = CreateUI("FinishBtn", rewardBody.transform);
            PlaceCenter(finishBtnGo, new Vector2(0, -160), new Vector2(180, 60));
            var finishBtnBg = finishBtnGo.AddComponent<Image>();
            finishBtnBg.color = Brick;
            var finishBtn = finishBtnGo.AddComponent<Button>();
            var finishLabelGo = CreateUI("Label", finishBtnGo.transform);
            PlaceFull(finishLabelGo);
            var finishLabel = finishLabelGo.AddComponent<Text>();
            finishLabel.text = "完 成";
            finishLabel.fontSize = 26;
            finishLabel.color = Paper;
            finishLabel.alignment = TextAnchor.MiddleCenter;

            // RewardPopup 本体保持 active，只隐藏 Body/Dim 子物体（StartCoroutine 要求 GO active）
            rewardBody.SetActive(false);
            rewardDimGo.SetActive(false);

            // ============== MazeGameManager ==============
            var mgrGo = new GameObject("MazeGameManager");
            var mgr = mgrGo.AddComponent<MazeGameManager>();
            mgr.player = playerCtrl;
            mgr.goal = goalTrigger;
            mgr.instructionPanel = instrRoot;
            mgr.startButton = startBtn;

            // RewardPopup 组件
            var rewardPopup = rewardRoot.AddComponent<MazeRewardPopup>();
            rewardPopup.rootPanel = rewardBody;   // 绑 Body 子物体，不是 RewardPopup 本体
            rewardPopup.itemIconImage = rewardIconImg;
            rewardPopup.finishButton = finishBtn;
            mgr.rewardPopup = rewardPopup;

            // Finish 按钮事件：完成后关闭弹窗 + 触发 manager 完成
            finishBtn.onClick.AddListener(() =>
            {
                rewardPopup.Hide();
                // 这里可以加 LoadScene 回到主菜单等
            });

            // 统一像素字体
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
            Debug.Log("[MazeBuilder] 构建完成：" + ScenePath);
        }

        // ------------------------------------------------------------------
        // 玩家创建
        // 结构：Player (Rigidbody2D + BoxCollider2D(脚底) + MazePlayerController)
        //        └── Visual (SpriteRenderer + Animator) — 人物图像
        // Collider 直接在根节点，只有脚底 0.55×0.30 一块，上半身视觉可穿过树篱
        // ------------------------------------------------------------------
        private static GameObject CreatePlayer(Font pixelFont)
        {
            var go = new GameObject("Player");
            // 出生点（硬编码，与当前场景完全一致）
            go.transform.position = new Vector3(-7.41f, -3.71f, 0);

            // Rigidbody2D（根节点挂）
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // BoxCollider2D（根节点挂，只有脚底一小块）
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.61f, 0.44f);
            col.offset = new Vector2(1.19f, -0.07f);

            // ---- Visual 子物体 ----
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = Vector3.zero;

            // SpriteRenderer：用 a-idle 第一帧做初始显示
            Sprite idleFirst = LoadFirstFrame("Assets/a-idle");
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = idleFirst;
            sr.sortingOrder = 3;
            sr.drawMode = SpriteDrawMode.Simple;

            // 人物缩放（硬编码，与当前场景完全一致）
            visual.transform.localScale = new Vector3(0.121f, 0.121f, 1f);

            // Animator（挂在 Player 根节点，Visual 是子物体 — 与 4-寻找染料 完全一致）
            var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                RootDir + "/Game/Art/Animations/MazePlayer.controller");
            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrl;
            anim.updateMode = AnimatorUpdateMode.Normal;

            // Controller（根节点挂，自动查找 Visual 子物体引用）
            var ctrl = go.AddComponent<MazePlayerController>();
            return go;
        }

        private static Sprite LoadFirstFrame(string folder)
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            if (guids.Length == 0) return null;
            // 自然排序后取第一帧
            Sprite[] frames = new Sprite[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
            System.Array.Sort(frames, (a, b) =>
            {
                var rgx = new System.Text.RegularExpressions.Regex(@"\d+");
                var ma = rgx.Match(a.name); var mb = rgx.Match(b.name);
                if (ma.Success && mb.Success)
                {
                    int c = int.Parse(ma.Value).CompareTo(int.Parse(mb.Value));
                    if (c != 0) return c;
                }
                return string.Compare(a.name, b.name, System.StringComparison.Ordinal);
            });
            return frames[0];
        }

        // ------------------------------------------------------------------
        // 边界碰撞（围住地图四周，留出入口和终点的缺口）
        // ------------------------------------------------------------------
        // ==========================================================================
        // 边界墙 — 硬编码（与用户手动调整的场景数据完全一致）
        // ==========================================================================
        private static void BuildBoundaryWalls(GameObject wallsRoot)
        {
            // 用户手动布置的 4 个边界墙（Wall_Right 已删除）
            CreateBox(wallsRoot, "Wall_Top",         new Vector2( 0.00f,  4.03f), new Vector2(17.92f,  0.60f));
            CreateBox(wallsRoot, "Wall_BottomLeft",  new Vector2( 4.00f, -0.20f), new Vector2( 0.10f,  0.60f));
            CreateBox(wallsRoot, "Wall_BottomRight", new Vector2( 2.63f, -1.64f), new Vector2(14.82f,  0.60f));
            CreateBox(wallsRoot, "Wall_Left",        new Vector2(-3.72f,  0.11f), new Vector2( 0.60f, 10.61f));
        }

        // ==========================================================================
        // 内部树篱 — 31 个（与用户手动调整的场景完全一致）
        // 数据来源：Scene dump 2026-11-16 22:00
        // ==========================================================================
        private static void BuildInteriorWallsFromPixelMap(GameObject wallsRoot)
        {
            CreateBox(wallsRoot, "Hedge_000",        new Vector2(-7.86f, -0.11f), new Vector2( 1.00f,  2.00f));
            CreateBox(wallsRoot, "Hedge_001",        new Vector2( 2.02f, -4.10f), new Vector2(11.50f,  0.50f));
            CreateBox(wallsRoot, "Hedge_002",        new Vector2( 7.59f, -0.09f), new Vector2( 1.00f,  5.00f));
            CreateBox(wallsRoot, "Hedge_004",        new Vector2(-1.54f, -3.35f), new Vector2(10.50f,  1.50f));
            CreateBox(wallsRoot, "Hedge_005",        new Vector2( 3.70f, -3.03f), new Vector2( 1.00f,  8.50f));
            CreateBox(wallsRoot, "Hedge_006",        new Vector2( 2.64f,  1.09f), new Vector2( 1.00f,  8.00f));
            CreateBox(wallsRoot, "Hedge_007",        new Vector2( 4.09f,  3.32f), new Vector2( 0.50f,  8.00f));
            CreateBox(wallsRoot, "Hedge_008",        new Vector2( 7.39f, -1.71f), new Vector2( 0.50f,  4.00f));
            CreateBox(wallsRoot, "Hedge_009",        new Vector2( 2.64f,  3.07f), new Vector2( 0.50f,  7.50f));
            CreateBox(wallsRoot, "Hedge_012",        new Vector2( 1.17f,  1.64f), new Vector2( 4.00f,  7.00f));
            CreateBox(wallsRoot, "Hedge_013",        new Vector2(-0.40f,  0.44f), new Vector2( 0.50f,  6.50f));
            CreateBox(wallsRoot, "Hedge_014",        new Vector2(-3.85f, -2.78f), new Vector2( 1.00f,  2.50f));
            CreateBox(wallsRoot, "Hedge_015",        new Vector2(-0.84f, -1.61f), new Vector2( 0.50f,  4.00f));
            CreateBox(wallsRoot, "Hedge_015 (1)",    new Vector2( 1.06f, -0.27f), new Vector2( 0.50f,  4.00f));
            CreateBox(wallsRoot, "Hedge_015 (2)",    new Vector2( 3.46f,  2.39f), new Vector2( 0.50f,  4.00f));
            CreateBox(wallsRoot, "Hedge_015 (3)",    new Vector2(-3.10f, -2.24f), new Vector2( 0.50f,  4.00f));
            CreateBox(wallsRoot, "Hedge_015 (4)",    new Vector2( 6.30f,  1.40f), new Vector2( 0.50f,  4.00f));
            CreateBox(wallsRoot, "Hedge_016",        new Vector2(-4.34f,  1.69f), new Vector2( 1.00f,  3.50f));
            CreateBox(wallsRoot, "Hedge_016 (1)",    new Vector2(-1.02f, -2.32f), new Vector2( 1.00f,  3.50f));
            CreateBox(wallsRoot, "Hedge_016 (2)",    new Vector2( 0.53f, -3.00f), new Vector2( 1.00f,  3.50f));
            CreateBox(wallsRoot, "Hedge_017",        new Vector2(-2.15f, -1.17f), new Vector2( 0.50f,  3.00f));
            CreateBox(wallsRoot, "Hedge_018",        new Vector2(-0.67f,  2.44f), new Vector2( 3.00f,  0.50f));
            CreateBox(wallsRoot, "Hedge_019",        new Vector2(-2.02f,  1.07f), new Vector2( 2.50f,  3.00f));
            CreateBox(wallsRoot, "Hedge_020",        new Vector2( 5.49f, -0.66f), new Vector2( 0.50f,  2.50f));
            CreateBox(wallsRoot, "Hedge_021",        new Vector2(-4.47f,  2.42f), new Vector2( 3.00f,  2.50f));
            CreateBox(wallsRoot, "Hedge_021 (1)",    new Vector2(-4.61f, -0.93f), new Vector2( 3.00f,  2.50f));
            CreateBox(wallsRoot, "Hedge_022",        new Vector2(-5.18f,  1.46f), new Vector2( 0.50f,  2.00f));
            CreateBox(wallsRoot, "Hedge_022 (1)",    new Vector2(-5.43f, -1.73f), new Vector2( 0.50f,  2.00f));
            CreateBox(wallsRoot, "Hedge_022 (2)",    new Vector2(-6.96f, -0.69f), new Vector2( 0.50f,  2.00f));
            CreateBox(wallsRoot, "Hedge_022 (3)",    new Vector2(-6.68f,  1.50f), new Vector2( 0.50f,  2.00f));
            CreateBox(wallsRoot, "Hedge_023",        new Vector2( 7.89f,  4.05f), new Vector2( 0.50f,  0.50f));

            Debug.Log("[MazeBuilder] 内部树篱 31 个（硬编码，与当前场景完全一致）");
        }

        private static BoxCollider2D CreateBox(GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            return col;
        }

        // ------------------------------------------------------------------
        // 纹理配置
        // ------------------------------------------------------------------
        private static void ConfigureTextures()
        {
            string[] files = System.IO.Directory.GetFiles(RootDir, "*.png");
            foreach (string f in files)
            {
                string path = f.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PPU;
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
