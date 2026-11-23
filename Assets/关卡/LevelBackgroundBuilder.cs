#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Qingming.LevelBackground.EditorTools
{
    /// <summary>
    /// 一键构建四季关卡场景（1-4 关背景）。
    /// 菜单：Tools/四季关卡/一键构建关卡背景场景
    /// </summary>
    public static class LevelBackgroundBuilder
    {
        private const string RootDir = "Assets/关卡";

        private const float PPU = 100f;
        private const float CamSize = 4.5f; // 16:9 orthoSize 4.5 → 屏幕高 9 单位 → 宽 16 单位

        // 四关的配置：文件夹 / 序号 / 季节 / 背景文件名 / 场景名 / 摄像机背景色 / 背景位置 / 背景缩放 / Visual 位置偏移
        private static readonly LevelConfig[] Levels = new[]
        {
            new LevelConfig("1-春", 1, "春", "第一关-春", "Level1_Spring", new Color(0.78f, 0.88f, 0.75f),
                            new Vector3(5.53f, 0f, 0f), new Vector3(0.8947f, 0.8947f, 0.505f),
                            new Vector3(0f, 0.62f, 0f)),
            new LevelConfig("2-夏", 2, "夏", "第二关-夏", "Level2_Summer", new Color(0.55f, 0.75f, 0.9f),
                            new Vector3(2.68f, 0f, 0f), new Vector3(0.8947f, 0.8947f, 0.505f),
                            new Vector3(-5.06f, 0.48f, 0f)),
            new LevelConfig("3-秋", 3, "秋", "第三关-秋", "Level3_Autumn", new Color(0.95f, 0.78f, 0.55f),
                            new Vector3(4.88f, 0f, 0f), new Vector3(0.8947f, 0.8947f, 0.505f),
                            new Vector3(0f, 0.68f, 0f)),
            new LevelConfig("4-冬", 4, "冬", "第四关-冬", "Level4_Winter", new Color(0.85f, 0.9f, 0.95f),
                            new Vector3(0f, 0f, 0f), new Vector3(0.8947f, 0.8947f, 0.505f),
                            new Vector3(-3.15f, 0.24f, 0f)),
        };

        [MenuItem("Tools/四季关卡/一键构建关卡背景场景")]
        public static void BuildAll()
        {
            Debug.Log("[LevelBackgroundBuilder] 开始构建四季关卡场景...");

            EnsureFolders();
            ConfigureTextures();
            AssetDatabase.Refresh();

            int success = 0;
            foreach (var cfg in Levels)
            {
                if (BuildOne(cfg)) success++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LevelBackgroundBuilder] 构建完成：{success}/{Levels.Length} 个场景");
        }

        // ==============================================================
        // 基础
        // ==============================================================
        private static void EnsureFolders()
        {
            foreach (var cfg in Levels)
            {
                string dir = RootDir + "/" + cfg.FolderName;
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder(RootDir, cfg.FolderName);
            }
        }

        private static void ConfigureTextures()
        {
            foreach (var cfg in Levels)
            {
                string folder = RootDir + "/" + cfg.FolderName;
                string pngPath = folder + "/" + cfg.FileName + ".png";
                string jpgPath = folder + "/" + cfg.FileName + ".jpeg";
                string path = File.Exists(pngPath) ? pngPath : (File.Exists(jpgPath) ? jpgPath : null);

                if (path == null)
                {
                    Debug.LogWarning($"[LevelBackgroundBuilder] 找不到背景文件：{cfg.FolderName}/{cfg.FileName}");
                    continue;
                }

                ConfigureOne(path, PPU, new Vector2(0.5f, 0.5f));
            }
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
            importer.maxTextureSize = 0; // 不自动缩小，保持原始分辨率

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // ==============================================================
        // 单个场景构建
        // ==============================================================
        private static bool BuildOne(LevelConfig cfg)
        {
            string folder = RootDir + "/" + cfg.FolderName;
            string scenePath = folder + "/" + cfg.SceneName + ".unity";

            // 场景已存在 → 跳过，保护手动调的数值不被覆盖
            if (File.Exists(scenePath))
            {
                Debug.Log($"[LevelBackgroundBuilder] 场景已存在，跳过（手动调的数值会被保护）：{cfg.SceneName}");
                return true;
            }

            string pngPath = folder + "/" + cfg.FileName + ".png";
            string jpgPath = folder + "/" + cfg.FileName + ".jpeg";
            string path = File.Exists(pngPath) ? pngPath : jpgPath;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[LevelBackgroundBuilder] 加载 Sprite 失败：{path}");
                return false;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ------- 摄像机 -------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.backgroundColor = cfg.CamBg;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // AudioListener（否则 Unity 会报警告）
            camGo.AddComponent<AudioListener>();

            // 摄像机跟随组件
            var camFollow = camGo.AddComponent<Qingming.LevelBackground.CameraFollow>();
            camFollow.offset = new Vector2(0f, 1.5f);
            camFollow.smoothSpeedX = 8f;
            camFollow.smoothSpeedY = 5f;

            // ------- EventSystem -------
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // ------- 背景 Sprite -------
            var bgGo = new GameObject("Background");
            bgGo.transform.position = cfg.BackgroundPos;
            bgGo.transform.localScale = cfg.BackgroundScale;
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = sprite;
            bgSr.sortingOrder = -10; // 最底层

            // ------- 主人公（复用采桑游戏的人物动画） -------
            CreatePlayerOnGround(cfg);

            // ------- Canvas（全屏 UI 层，方便后续加按钮/提示） -------
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

            // ------- 保存到各自的文件夹 -------
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[LevelBackgroundBuilder] 已创建：{cfg.SceneName} ({cfg.Season}) → {scenePath}");
            return true;
        }

        // ==============================================================
        // 主人公
        // ==============================================================
        private static void CreatePlayerOnGround(LevelConfig cfg)
        {
            var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/animation/player.controller");
            if (animCtrl == null)
            {
                Debug.LogWarning("[LevelBackgroundBuilder] 找不到 player.controller，跳过人物创建");
                return;
            }

            // 取 idle 第一帧作为默认显示
            Sprite idleFirst = null;
            var idleGuids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/animation/a-idle" });
            if (idleGuids.Length > 0)
                idleFirst = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(idleGuids[0]));

            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, -3.8f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(playerGo.transform, false);
            visual.transform.localPosition = cfg.PlayerVisualPos;

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = idleFirst;
            sr.sortingOrder = 5; // 在背景之上

            // 与采桑游戏一致：帧图 1112 像素高 → world=11.12 单位 → * 0.11 ≈ 1.22 单位显示高度
            visual.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);

            var anim = playerGo.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrl;

            // 玩家移动控制器（A/D 移动，Shift 跑，Space 跳）
            var ctrl = playerGo.AddComponent<Qingming.LevelBackground.LevelPlayerController>();
            ctrl.visualTransform = visual.transform;
            ctrl.animator = anim;
            ctrl.jumpHeight = 2.5f;
            ctrl.spawnPoint = playerGo.transform.position;
            ctrl.clampHorizontal = false;
        }

        private struct LevelConfig
        {
            public readonly string FolderName;
            public readonly int Index;
            public readonly string Season;
            public readonly string FileName;
            public readonly string SceneName;
            public readonly Color CamBg;
            public readonly Vector3 BackgroundPos;
            public readonly Vector3 BackgroundScale;
            public readonly Vector3 PlayerVisualPos;

            public LevelConfig(string folder, int idx, string season, string file, string scene, Color camBg,
                               Vector3 bgPos, Vector3 bgScale, Vector3 visualPos)
            {
                FolderName = folder;
                Index = idx;
                Season = season;
                FileName = file;
                SceneName = scene;
                CamBg = camBg;
                BackgroundPos = bgPos;
                BackgroundScale = bgScale;
                PlayerVisualPos = visualPos;
            }
        }
    }
}
#endif
