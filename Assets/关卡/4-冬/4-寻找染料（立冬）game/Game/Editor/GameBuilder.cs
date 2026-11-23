#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JiangnanDye.EditorTools
{
    /// <summary>
    /// 一键构建：导入像素素材 → 生成染料 / 物品数据 → 生成 Prefab → 搭建 MainScene 与 HUD。
    /// 菜单：Tools/江南染料/一键构建游戏场景
    /// </summary>
    public static class GameBuilder
    {
        private const string RootDir = "Assets/4-寻找染料game/Game";
        private const string ArtDir = RootDir + "/Art";
        private const string DataDir = RootDir + "/Data";
        private const string PrefabDir = RootDir + "/Prefabs";
        private const string ScenePath = RootDir + "/Scenes/MainScene.unity";
        private const float PPU = 64f;
        private const int MapWidth = 1671;
        private const int MapHeight = 941;

        private static readonly Color Ink = Hex("#3B2C1F");
        private static readonly Color InkSoft = new Color(0.23f, 0.17f, 0.12f, 0.62f);
        private static readonly Color Paper = Hex("#F4E7CE");
        private static readonly Color Brick = Hex("#A8452F");
        private static readonly Color Gold = Hex("#C09A55");
        private static readonly Color Moss = Hex("#6E7F45");

        private static Font gameFont;
        private static int gatherableLayer;

        private static Sprite mapSprite;
        private static Sprite playerSprite;
        private static Sprite playerWalkFrame1;
        private static RuntimeAnimatorController playerAnimatorCtrl;
        private static Sprite hoeSprite;
        private static Sprite basketSprite;
        private static Sprite highlightSprite;
        private static Sprite sparkleSprite;
        private static Sprite glowSprite;
        private static Sprite shadowSprite;

        // 可走范围（世界坐标）。编辑器里调整 PlayerController.worldMin/worldMax 时同步改这里
        private static readonly Vector2 worldMin = new Vector2(-12.6f, -6.85f);
        private static readonly Vector2 worldMax = new Vector2(12.6f, 4.95f);

        // 调试用可视化开关：可走区蓝色框默认关；障碍物红色块也默认关（用户调完障碍坐标后关闭）
        private static readonly bool SHOW_WALK_BOUNDS = false;
        private static readonly bool SHOW_OBSTACLE_VIS = false;

        private static Sprite panelSprite;
        private static Sprite innerSprite;
        private static Sprite buttonSprite;
        private static Sprite chipSprite;
        private static Sprite circleSprite;
        private static Sprite ringSprite;
        private static Sprite dotSprite;

        private static GameObject plantPrefab;
        private static GameObject stonePrefab;
        private static GameObject playerPrefab;

        private static readonly Dictionary<DyeColor, DyeDefinition> dyes = new Dictionary<DyeColor, DyeDefinition>();

        [MenuItem("Tools/江南染料/一键构建游戏场景")]
        public static void Build()
        {
            Debug.Log("[GameBuilder] 开始构建江南染料小游戏...");

            gatherableLayer = EnsureLayer("Gatherable");
            ConfigureTextures();
            AssetDatabase.Refresh();

            LoadSprites();
            CreateDataAssets();
            CreatePrefabs();
            BuildScene();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameBuilder] 构建完成：" + ScenePath);
        }

        // ------------------------------------------------------------------
        // 基础工具
        // ------------------------------------------------------------------
        private static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.white;
        }

        private static int EnsureLayer(string layerName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return 8;

            SerializedObject so = new SerializedObject(assets[0]);
            SerializedProperty layers = so.FindProperty("layers");
            for (int i = 8; i < 32; i++)
            {
                SerializedProperty sp = layers.GetArrayElementAtIndex(i);
                if (sp.stringValue == layerName) return i;
            }
            for (int i = 8; i < 32; i++)
            {
                SerializedProperty sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    so.ApplyModifiedProperties();
                    return i;
                }
            }
            return 8;
        }

        // ------------------------------------------------------------------
        // 素材导入
        // ------------------------------------------------------------------
        private static void ConfigureTextures()
        {
            string[] files = System.IO.Directory.GetFiles(ArtDir, "*.png", System.IO.SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string path = file.Replace('\\', '/');
                bool isUi = path.Contains("/Art/UI/");
                bool bottomPivot = path.Contains("/Art/Plants/") || path.Contains("/Art/Character/")
                                   || path.Contains("/Art/Stones/");
                Vector4 border = Vector4.zero;
                if (path.EndsWith("Panel_Paper.png") || path.EndsWith("Panel_Inner.png")
                    || path.EndsWith("Button_Paper.png") || path.EndsWith("Chip_Dark.png"))
                {
                    border = new Vector4(30f, 30f, 30f, 30f);
                }

                // 植物 / 人物 / 石头按实际不透明内容的底部设置轴心，避免悬空
                float pivotY = bottomPivot ? ComputeBottomPivot(file) : 0.5f;
                ImportAsSprite(path, isUi ? 100f : PPU, bottomPivot, border, pivotY);
            }
        }

        /// <summary>读取 PNG 透明通道，返回不透明内容底边对应的归一化 Y（从底部算起）。</summary>
        private static float ComputeBottomPivot(string filePath)
        {
            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes);
                int w = tex.width;
                int h = tex.height;
                Color32[] pixels = tex.GetPixels32();
                for (int y = h - 1; y >= 0; y--)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[y * w + x].a > 8)
                        {
                            Object.DestroyImmediate(tex);
                            return Mathf.Clamp01((y + 1f) / h);
                        }
                    }
                }
                Object.DestroyImmediate(tex);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GameBuilder] 计算轴心失败 " + filePath + " : " + e.Message);
            }
            return 0.5f;
        }

        private static void ImportAsSprite(string path, float ppu, bool bottomPivot, Vector4 border, float pivotY)
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
            importer.maxTextureSize = 4096;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)(bottomPivot ? SpriteAlignment.Custom : SpriteAlignment.Center);
            settings.spritePivot = bottomPivot ? new Vector2(0.5f, pivotY) : new Vector2(0.5f, 0.5f);
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string relative)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + relative);
        }

        private static void LoadSprites()
        {
            gameFont = WorkshopArt.PixelFont;

            mapSprite = LoadSprite("Background/Map_Background_Clean.png");
            playerSprite = LoadSprite("Character/Player_4Dir.png");
            // 新人物：9 张走帧 + AnimatorController（由 PlayerAnimatorBuilder 生成）
            playerWalkFrame1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a-walk/走路关键帧_01_三次修正.png");
            playerAnimatorCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ArtDir + "/Animations/Player.controller");
            hoeSprite = LoadSprite("Tools/Hoe_Icon.png");
            basketSprite = LoadSprite("UI/Basket.png");
            highlightSprite = LoadSprite("Effects/Collect_Highlight.png");
            sparkleSprite = LoadSprite("Effects/Collect_Sparkle.png");
            shadowSprite = LoadSprite("Effects/Ground_Shadow.png");
            glowSprite = LoadSprite("UI/Soft_Glow.png");

            panelSprite = LoadSprite("UI/Panel_Paper.png");
            innerSprite = LoadSprite("UI/Panel_Inner.png");
            buttonSprite = LoadSprite("UI/Button_Paper.png");
            chipSprite = LoadSprite("UI/Chip_Dark.png");
            circleSprite = LoadSprite("UI/Circle.png");
            ringSprite = LoadSprite("UI/Ring.png");
            dotSprite = LoadSprite("UI/Dot.png");
        }

        // ------------------------------------------------------------------
        // 数据资源
        // ------------------------------------------------------------------
        private static ItemData CreateItem(DyeColor color, DyeCondition condition, Sprite sprite)
        {
            string suffix = condition == DyeCondition.Normal ? "Normal" : "Damaged";
            string path = DataDir + "/Items/Item_Dye_" + color + "_" + suffix + ".asset";
            ItemData asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.itemId = "DYE_" + color.ToString().ToUpperInvariant() + "_" + suffix.ToUpperInvariant();
            asset.dyeColor = color;
            asset.condition = condition;
            asset.chineseName = DyeColorUtil.ChineseName(color) + "染料";
            asset.stateText = DyeColorUtil.ConditionText(condition);
            string plantName;
            string description;
            GetPlantInfo(color, out plantName, out description);
            asset.plantName = plantName;
            asset.description = description;
            asset.icon = sprite;
            asset.worldSprite = sprite;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>五色染料的原料植物名与简介。</summary>
        private static void GetPlantInfo(DyeColor color, out string plantName, out string description)
        {
            switch (color)
            {
                case DyeColor.Red:
                    plantName = "红花";
                    description = "花瓣可提取红色染料，为丝织物染出鲜亮的红色。";
                    break;
                case DyeColor.Yellow:
                    plantName = "金盏花";
                    description = "花朵可提取黄色色素，为丝织物染出明亮暖黄色。";
                    break;
                case DyeColor.Green:
                    plantName = "冻绿";
                    description = "叶片等部位可作染材，经处理可染出自然的绿色。";
                    break;
                case DyeColor.Blue:
                    plantName = "马蓝";
                    description = "叶片经浸泡发酵制成蓝靛，可为织物染出靛蓝色。";
                    break;
                default:
                    plantName = "贝紫";
                    description = "海螺腺体分泌物经氧化，可制成珍贵的紫色染料。";
                    break;
            }
        }

        private static void CreateDataAssets()
        {
            dyes.Clear();
            for (int i = 0; i < DyeColorUtil.AllColors.Length; i++)
            {
                DyeColor color = DyeColorUtil.AllColors[i];
                string colorEn = color.ToString();

                Sprite normal = LoadSprite("Plants/Plant_" + colorEn + "_Normal.png");
                Sprite damaged = LoadSprite("Plants/Plant_" + colorEn + "_Damaged.png");

                ItemData normalItem = CreateItem(color, DyeCondition.Normal, normal);
                ItemData damagedItem = CreateItem(color, DyeCondition.Damaged, damaged);

                string path = DataDir + "/Dyes/Dye_" + colorEn + ".asset";
                DyeDefinition def = AssetDatabase.LoadAssetAtPath<DyeDefinition>(path);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<DyeDefinition>();
                    AssetDatabase.CreateAsset(def, path);
                }

                def.color = color;
                def.chineseName = DyeColorUtil.ChineseName(color) + "染料";
                def.normalSprite = normal;
                def.damagedSprite = damaged;
                def.normalIcon = normal;
                def.damagedIcon = damaged;
                def.normalItemData = normalItem;
                def.damagedItemData = damagedItem;
                EditorUtility.SetDirty(def);
                dyes[color] = def;
            }
        }

        // ------------------------------------------------------------------
        // Prefab
        // ------------------------------------------------------------------
        private static void CreatePrefabs()
        {
            // 植物
            GameObject plant = new GameObject("GatherablePlant");
            SpriteRenderer plantSr = plant.AddComponent<SpriteRenderer>();
            plantSr.sortingOrder = 0;
            plant.AddComponent<CircleCollider2D>();
            GatherableNode plantNode = plant.AddComponent<GatherableNode>();

            GameObject hl = new GameObject("Highlight");
            hl.transform.SetParent(plant.transform, false);
            SpriteRenderer hlSr = hl.AddComponent<SpriteRenderer>();
            hlSr.sprite = highlightSprite;
            hlSr.sortingOrder = -20;
            hlSr.enabled = false;

            plantNode.spriteRenderer = plantSr;
            plantNode.highlight = hlSr;
            AddPlantGlow(plantNode, glowSprite);

            plantPrefab = PrefabUtility.SaveAsPrefabAsset(plant, PrefabDir + "/Gatherables/GatherablePlant.prefab");
            Object.DestroyImmediate(plant);

            // 石头
            GameObject stone = new GameObject("GatherableStone");
            SpriteRenderer stoneSr = stone.AddComponent<SpriteRenderer>();
            stoneSr.sortingOrder = 0;
            stone.AddComponent<CircleCollider2D>();
            GatherableNode stoneNode = stone.AddComponent<GatherableNode>();

            GameObject stoneHl = new GameObject("Highlight");
            stoneHl.transform.SetParent(stone.transform, false);
            SpriteRenderer stoneHlSr = stoneHl.AddComponent<SpriteRenderer>();
            stoneHlSr.sprite = highlightSprite;
            stoneHlSr.sortingOrder = -20;
            stoneHlSr.enabled = false;

            stoneNode.spriteRenderer = stoneSr;
            stoneNode.highlight = stoneHlSr;
            stoneNode.resourceType = ResourceType.Stone;
            stoneNode.displayName = "普通山石";

            stonePrefab = PrefabUtility.SaveAsPrefabAsset(stone, PrefabDir + "/Gatherables/GatherableStone.prefab");
            Object.DestroyImmediate(stone);

            // 人物
            GameObject player = new GameObject("Player");
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            SpriteRenderer playerSr = visual.AddComponent<SpriteRenderer>();
            playerSr.sprite = playerWalkFrame1 != null ? playerWalkFrame1 : playerSprite;
            playerSr.sortingOrder = 0;

            float playerHeight = 3.25f;
            Sprite scaleRef = playerWalkFrame1 != null ? playerWalkFrame1 : playerSprite;
            float playerScale = playerHeight / (scaleRef.rect.height / scaleRef.pixelsPerUnit);
            visual.transform.localScale = new Vector3(playerScale, playerScale, 1f);

            Animator anim = player.AddComponent<Animator>();
            anim.runtimeAnimatorController = playerAnimatorCtrl;

            // Collider 必须在 InteractionDetector（Require Collider2D）之前加
            CapsuleCollider2D cap = player.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Vertical;
            Bounds visualBounds = playerSr.bounds;
            cap.offset = new Vector2(visualBounds.center.x - player.transform.position.x,
                                     visualBounds.center.y - player.transform.position.y);
            cap.size = new Vector2(Mathf.Max(0.6f, visualBounds.size.x),
                                   Mathf.Max(1.6f, visualBounds.size.y * 0.8f));

            PlayerController pc = player.AddComponent<PlayerController>();
            pc.moveSpeed = 3.2f;
            pc.worldMin = new Vector2(-12.6f, -6.85f);
            pc.worldMax = new Vector2(12.6f, 4.95f);
            pc.visual = visual.transform;
            pc.spriteRenderer = playerSr;
            pc.animator = anim;

            InteractionDetector detector = player.AddComponent<InteractionDetector>();
            detector.detectionRadius = 2.0f;
            detector.gatherableMask = 1 << gatherableLayer;

            GameObject shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(player.transform, false);
            SpriteRenderer shadowSr = shadowGo.AddComponent<SpriteRenderer>();
            shadowSr.sprite = shadowSprite;
            shadowSr.sortingOrder = -5;
            shadowSr.color = new Color(1f, 1f, 1f, 0.55f);
            shadowGo.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
            shadowGo.transform.localPosition = new Vector3(0f, 0.03f, 0f);

            playerPrefab = PrefabUtility.SaveAsPrefabAsset(player, PrefabDir + "/Player/Player.prefab");
            Object.DestroyImmediate(player);

            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------
        // 场景
        // ------------------------------------------------------------------
        private class PlantSpec
        {
            public string id;
            public DyeColor color;
            public DyeCondition condition;
            public float px;
            public float py;
            public float height;
            public float ringWidth;
        }

        // 地图是一张整图。每种颜色的提示圈固定在对应资源的根部。
        private static readonly PlantSpec[] PlantAnchors =
        {
            new PlantSpec { id = "PLANT_RED", color = DyeColor.Red, px = 1240, py = 510, height = 3.25f, ringWidth = 154f },
            new PlantSpec { id = "PLANT_YELLOW", color = DyeColor.Yellow, px = 1010, py = 288, height = 2.82f, ringWidth = 116f },
            new PlantSpec { id = "PLANT_GREEN", color = DyeColor.Green, px = 347, py = 780, height = 2.15f, ringWidth = 158f },
            new PlantSpec { id = "PLANT_BLUE", color = DyeColor.Blue, px = 946, py = 683, height = 2.90f, ringWidth = 136f },
            new PlantSpec { id = "PLANT_PURPLE", color = DyeColor.Purple, px = 470, py = 355, height = 2.85f, ringWidth = 132f },
        };

        private static readonly Vector2[] PlantPointOffsets =
        {
            Vector2.zero, new Vector2(14f, -8f), new Vector2(-12f, 9f)
        };

        private static Vector3 MapPos(float px, float py)
        {
            return new Vector3((px - MapWidth * 0.5f) / PPU, (MapHeight * 0.5f - py) / PPU, 0f);
        }

        private static void BuildScene()
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 相机
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = (MapHeight / PPU) * 0.5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#15120F");
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            CameraAspectFitter fitter = camGo.AddComponent<CameraAspectFitter>();
            // 黑边比例跟随地图实际宽高，保证整张地图完整显示且不露黑缝
            fitter.targetAspect = MapWidth / (float)MapHeight;

            // 背景
            GameObject background = new GameObject("Background");
            SpriteRenderer bgSr = background.AddComponent<SpriteRenderer>();
            bgSr.sprite = mapSprite;
            bgSr.sortingOrder = -1000;
            background.transform.position = Vector3.zero;

            // 可走范围可视化（半透明淡蓝色矩形）
            if (SHOW_WALK_BOUNDS) BuildWalkBounds(worldMin, worldMax);

            // 环境碰撞
            GameObject envRoot = new GameObject("EnvironmentCollision");
            AddBox(envRoot.transform, "Building_Workshop", -67, 0, 413, 430);
            AddBox(envRoot.transform, "Wall_North", 413, 0, 1605, 150);
            AddBox(envRoot.transform, "Rock_Center", 622, 132, 862, 422);
            AddBox(envRoot.transform, "Rock_Left", 21, 494, 261, 624);
            AddBox(envRoot.transform, "Rock_BottomLeft", -15, 776, 315, 926);
            AddBox(envRoot.transform, "Rock_BottomCenter", 716, 611, 886, 771);
            AddBox(envRoot.transform, "Rock_RightUpper", 1440, 150, 1585, 310);
            AddBox(envRoot.transform, "Rock_RightLower", 1514, 549, 1669, 704);
            AddBox(envRoot.transform, "Pond", 1223, 739, 1600, 975);
            AddBox(envRoot.transform, "Bamboo_Left", 449, -26, 532, 254);
            AddBox(envRoot.transform, "Bamboo_Center", 915, -21, 1007, 259);
            AddBox(envRoot.transform, "Bamboo_Right", 1467, 297, 1587, 507);

            // 资源点
            GameObject resources = new GameObject("ResourceNodes");
            // 每种颜色 3 个相近点位（同一时间只激活一个，采 3 次后该颜色失效）
            foreach (PlantSpec spec in PlantAnchors)
            {
                for (int k = 0; k < PlantPointOffsets.Length; k++)
                {
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(plantPrefab);
                    go.name = "Plant_" + spec.color + "_Point" + (k + 1);
                    go.transform.SetParent(resources.transform, false);
                    go.layer = gatherableLayer;
                    ConfigurePlant(go, spec, k);
                }
            }

            // 石头触发点直接落在地图太湖石的下沿（Rock_Left / Rock_BottomCenter / Rock_RightUpper）
            float[][] stones =
            {
                new float[] { 220f, 630f },
                new float[] { 810f, 690f },
                new float[] { 1427f, 310f },
            };
            for (int i = 0; i < stones.Length; i++)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(stonePrefab);
                go.name = "Stone_0" + (i + 1);
                go.transform.SetParent(resources.transform, false);
                go.transform.position = MapPos(stones[i][0], stones[i][1]);
                go.layer = gatherableLayer;
                ConfigureStone(go, i + 1);
            }

            // 人物
            GameObject playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerGo.name = "Player";
            playerGo.transform.position = MapPos(550f, 640f);

            // 画布
            Canvas canvas = CreateCanvas(cam);
            GameManager gm = CreateSystems(playerGo);
            BuildHud(canvas.transform, gm, playerGo);
            BuildIntro(canvas.transform, gm);
            BuildRewardPopup(canvas.transform, gm);
            BuildSettlement(canvas.transform, gm);
            BuildToast(canvas.transform, gm);

            // EventSystem
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            gizmoValidate(gm);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void gizmoValidate(GameManager gm)
        {
            if (gm.player == null) Debug.LogWarning("[GameBuilder] Player 未绑定");
            if (gm.detector == null) Debug.LogWarning("[GameBuilder] InteractionDetector 未绑定");
        }

        // ---------- 可走范围可视化 ----------
        private static void BuildWalkBounds(Vector2 min, Vector2 max)
        {
            // 1×1 白色 Texture + Sprite，用 SpriteRenderer 缩放出矩形和边框
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1),
                                          new Vector2(0.5f, 0.5f), /*ppu*/ 100f);

            Vector2 size = max - min;
            Vector2 center = (min + max) * 0.5f;

            // 半透明填充
            GameObject fill = new GameObject("WalkBoundsFill");
            SpriteRenderer fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = sprite;
            fillSr.sortingOrder = -999;
            fillSr.color = new Color(0.2f, 0.55f, 0.95f, 0.12f);
            fill.transform.position = new Vector3(center.x, center.y, 0f);
            fill.transform.localScale = new Vector3(size.x * 100f, size.y * 100f, 1f);

            // 4 根细边框
            Color edgeColor = new Color(0.2f, 0.55f, 0.95f, 0.85f);
            float edgeThickness = 0.08f;  // 世界单位（≈ 5px @ PPU=64）
            float sx = size.x, sy = size.y;

            MakeEdge(sprite, "Top",    edgeColor, center + new Vector2(0,  sy * 0.5f), new Vector2(sx, edgeThickness));
            MakeEdge(sprite, "Bottom", edgeColor, center + new Vector2(0, -sy * 0.5f), new Vector2(sx, edgeThickness));
            MakeEdge(sprite, "Left",   edgeColor, center + new Vector2(-sx * 0.5f, 0), new Vector2(edgeThickness, sy));
            MakeEdge(sprite, "Right",  edgeColor, center + new Vector2( sx * 0.5f, 0), new Vector2(edgeThickness, sy));
        }

        private static void MakeEdge(Sprite sprite, string name, Color color, Vector2 pos, Vector2 sizeWorld)
        {
            GameObject go = new GameObject("WalkBounds_" + name);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -998;
            sr.color = color;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            // sprite 1×1 px @ ppu=100 → scale = sizeWorld * 100
            go.transform.localScale = new Vector3(sizeWorld.x * 100f, sizeWorld.y * 100f, 1f);
        }

        private static void ConfigurePlant(GameObject go, PlantSpec spec, int pointIndex)
        {
            DyeDefinition dye = dyes[spec.color];
            ItemData item = dye.GetItem(spec.condition);
            Sprite sprite = dye.GetSprite(spec.condition);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;

            float scale = spec.height / (sprite.rect.height / PPU);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            GatherableNode node = go.GetComponent<GatherableNode>();
            node.resourceId = spec.id + "_POINT_0" + (pointIndex + 1);
            node.resourceType = ResourceType.Plant;
            node.dyeColor = spec.color;
            node.condition = spec.condition;
            node.itemData = item;
            node.dyeDefinition = dye;
            node.displayName = DyeColorUtil.ChineseName(spec.color) + "染料";
            node.originalSprite = sprite;
            node.spriteRenderer = sr;
            // 植物点位在地图上不显示（空物体 + 隐形触发器），视觉由地图本身的植物承担
            node.hideVisual = true;
            sr.enabled = false;

            CircleCollider2D col = go.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f / scale;
            col.offset = new Vector2(0f, 0.3f / scale);

            PlacePlant(node, pointIndex);
        }

        internal static void AddPlantGlow(GatherableNode node, Sprite sprite)
        {
            SpriteRenderer ring = node.highlight;
            SpriteRenderer glow = new GameObject("Plant Glow").AddComponent<SpriteRenderer>();
            glow.gameObject.layer = ring.gameObject.layer;
            glow.transform.SetParent(ring.transform, false);
            Vector3 ringSize = ring.sprite.bounds.size;
            Vector3 glowSize = sprite.bounds.size;
            glow.transform.localPosition = new Vector3(0f, ringSize.y * 0.48f, 0f);
            glow.transform.localScale = new Vector3(ringSize.x * 1.2f / glowSize.x,
                ringSize.y * 1.9f / glowSize.y, 1f);
            glow.sprite = sprite;
            glow.sortingLayerID = ring.sortingLayerID;
            glow.sortingOrder = ring.sortingOrder - 1;
            glow.color = new Color(1f, 0.84f, 0.32f, 0.55f);
            glow.enabled = false;
            node.highlightGlow = glow;
            node.interactionRadius = GatherableNode.PlantInteractionRadius;
        }

        internal static void PlacePlant(GatherableNode node, int pointIndex)
        {
            PlantSpec spec = PlantAnchors[(int)node.dyeColor];
            Vector2 offset = PlantPointOffsets[pointIndex];
            node.transform.position = MapPos(spec.px + offset.x, spec.py + offset.y);

            Transform ring = node.highlight.transform;
            ring.position = MapPos(spec.px, spec.py);
            Vector3 size = node.highlight.sprite.bounds.size;
            Vector3 scale = node.transform.lossyScale;
            float width = spec.ringWidth / PPU;
            ring.localScale = new Vector3(width / (size.x * scale.x),
                width * 0.44f / (size.y * scale.y), 1f);
        }

        private static void ConfigureStone(GameObject go, int index)
        {
            Sprite sprite = LoadSprite("Stones/Stone_Mineable.png");
            Sprite depleted = LoadSprite("Stones/Stone_Depleted.png");

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;

            float scale = 0.95f;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            GatherableNode node = go.GetComponent<GatherableNode>();
            node.resourceId = "STONE_0" + index;
            node.resourceType = ResourceType.Stone;
            node.displayName = "普通山石";
            node.itemData = null;
            node.originalSprite = sprite;
            node.depletedSprite = depleted;
            node.interactionRadius = 1.2f;
            node.spriteRenderer = sr;
            // 石头点位直接落在场景太湖石上：隐藏自带小石头视觉，仅保留隐形触发器
            node.hideVisual = true;
            node.autoSnapToSceneRock = false;
            sr.enabled = false;

            CircleCollider2D col = go.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.42f / scale;
            col.offset = new Vector2(0f, 0.26f / scale);

            Transform hl = go.transform.Find("Highlight");
            if (hl != null)
            {
                hl.localScale = new Vector3(1.9f / 3f / scale, 1.0f / 1.5f / scale, 1f);
                hl.localPosition = new Vector3(0f, 0.2f / scale, 0f);
            }
        }

        private static void AddBox(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Vector3 center = MapPos((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
            go.transform.position = center;
            Vector2 sizeWorld = new Vector2((x1 - x0) / PPU, (y1 - y0) / PPU);
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = sizeWorld;

            // 红色半透明可视化（调试障碍位置用，用户调完后关掉）
            if (SHOW_OBSTACLE_VIS)
            {
                Texture2D redTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                redTex.SetPixel(0, 0, Color.white);
                redTex.Apply();
                Sprite redSprite = Sprite.Create(redTex, new Rect(0, 0, 1, 1),
                                                  new Vector2(0.5f, 0.5f), /*ppu*/ 100f);
                SpriteRenderer visSr = go.AddComponent<SpriteRenderer>();
                visSr.sprite = redSprite;
                visSr.sortingOrder = -900;
                visSr.color = new Color(1f, 0.3f, 0.25f, 0.35f);
                go.transform.localScale = new Vector3(sizeWorld.x * 100f, sizeWorld.y * 100f, 1f);
            }
        }

        // ------------------------------------------------------------------
        // UI 基础
        // ------------------------------------------------------------------
        private static GameObject CreateUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Place(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform PlaceCenter(GameObject go, Vector2 position, Vector2 size)
        {
            return Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        }

        private static RectTransform PlaceTopCenter(GameObject go, Vector2 position, Vector2 size)
        {
            return Place(go, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size);
        }

        private static RectTransform PlaceTopLeft(GameObject go, Vector2 position, Vector2 size)
        {
            return Place(go, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        }

        private static RectTransform PlaceBottomCenter(GameObject go, Vector2 position, Vector2 size)
        {
            return Place(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, size);
        }

        private static Image AddImage(GameObject go, Sprite sprite, Color color, Image.Type type = Image.Type.Simple)
        {
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            img.raycastTarget = false;
            return img;
        }

        private static Text AddText(Transform parent, string name, string content, int fontSize, Color color,
            TextAnchor anchor, Vector2 position, Vector2 size, bool topCenter = true)
        {
            GameObject go = CreateUI(name, parent);
            if (topCenter) PlaceTopCenter(go, position, size);
            else PlaceCenter(go, position, size);

            Text text = go.AddComponent<Text>();
            text.font = gameFont;
            text.fontSize = fontSize;
            text.text = content;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        private static Button AddButton(Transform parent, string name, Vector2 position, Vector2 size,
            string label, Color labelColor, Sprite sprite)
        {
            GameObject go = CreateUI(name, parent);
            PlaceTopCenter(go, position, size);

            Image bg = go.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            go.AddComponent<Button>();

            Text text = AddText(go.transform, "Label", label, 32, labelColor, TextAnchor.MiddleCenter,
                Vector2.zero, size);
            RectTransform tr = text.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = Vector2.zero;
            tr.sizeDelta = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private static Canvas CreateCanvas(Camera cam)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            // 使用相机模式，使 UI 与 16:9 游戏区域完全对齐（黑边区域不绘制 UI）
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        // ------------------------------------------------------------------
        // 系统对象
        // ------------------------------------------------------------------
        private static GameManager CreateSystems(GameObject playerGo)
        {
            GameObject systems = new GameObject("GameSystems");

            CollectionInventory inventory = systems.AddComponent<CollectionInventory>();
            DyeCollectionManager history = systems.AddComponent<DyeCollectionManager>();
            ItemRewardManager rewardManager = systems.AddComponent<ItemRewardManager>();
            CollectEffect effect = systems.AddComponent<CollectEffect>();
            effect.sparkleSprite = sparkleSprite;
            effect.glowSprite = glowSprite;

            GameManager gm = systems.AddComponent<GameManager>();
            gm.player = playerGo.GetComponent<PlayerController>();
            gm.detector = playerGo.GetComponent<InteractionDetector>();
            gm.inventory = inventory;
            gm.dyeHistory = history;
            gm.rewardManager = rewardManager;
            gm.collectEffect = effect;

            GameSettlementManager settlementManager = systems.AddComponent<GameSettlementManager>();
            settlementManager.inventory = inventory;
            settlementManager.gameManager = gm;

            return gm;
        }

        // ------------------------------------------------------------------
        // HUD
        // ------------------------------------------------------------------
        private static void BuildHud(Transform canvas, GameManager gm, GameObject playerGo)
        {
            GameObject hud = CreateUI("HUD", canvas);
            RectTransform hudRt = hud.GetComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.offsetMin = Vector2.zero;
            hudRt.offsetMax = Vector2.zero;

            // 收集面板
            GameObject panel = CreateUI("CollectPanel", hud.transform);
            PlaceTopLeft(panel, new Vector2(34f, -30f), new Vector2(470f, 196f));
            AddImage(panel, panelSprite, Color.white, Image.Type.Sliced);

            GameObject basket = CreateUI("BasketIcon", panel.transform);
            PlaceTopLeft(basket, new Vector2(6f, -16f), new Vector2(246f, 170f));
            Image basketImg = AddImage(basket, basketSprite, Color.white);
            basketImg.preserveAspect = true;

            Text count = AddText(panel.transform, "Count", "0 / 5", 58, Brick,
                TextAnchor.MiddleLeft, new Vector2(272f, -46f), new Vector2(200f, 80f));
            count.fontStyle = FontStyle.Bold;
            // 像素字体没有粗体字面，用四向描边模拟加粗
            Outline countOutline = count.gameObject.AddComponent<Outline>();
            countOutline.effectColor = Brick;
            countOutline.effectDistance = new Vector2(0.7f, -0.7f);
            countOutline.useGraphicAlpha = false;
            // AddText 默认水平居中锚点，这里改成左上锚点，避免数字被推到面板外
            RectTransform countRt = count.rectTransform;
            countRt.anchorMin = new Vector2(0f, 1f);
            countRt.anchorMax = new Vector2(0f, 1f);
            countRt.pivot = new Vector2(0f, 1f);
            countRt.anchoredPosition = new Vector2(272f, -34f);
            countRt.sizeDelta = new Vector2(200f, 104f);

            CollectionProgressUI progress = hud.AddComponent<CollectionProgressUI>();
            progress.countText = count;

            Image[] dots = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                GameObject dot = CreateUI("Dot_" + (i + 1), panel.transform);
                PlaceTopLeft(dot, new Vector2(272f + i * 36f, -152f), new Vector2(26f, 26f));
                dots[i] = AddImage(dot, dotSprite, new Color(0.48f, 0.40f, 0.31f, 0.32f));
            }
            progress.dots = dots;

            // 不做文字提示，靠近目标时只用资源点上的黄色光圈标记


            // 锄头按钮
            GameObject hoe = CreateUI("HoeButton", canvas);
            RectTransform hoeRt = Place(hoe, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-40f, 40f), new Vector2(176f, 176f));
            CanvasGroup hoeGroup = hoe.AddComponent<CanvasGroup>();

            Image glow = AddImage(CreateUI("Glow", hoe.transform), glowSprite, new Color(1f, 0.86f, 0.55f, 0.28f));
            PlaceCenter(glow.gameObject, Vector2.zero, new Vector2(230f, 230f));

            Image bg = AddImage(hoe, circleSprite, Paper);
            bg.raycastTarget = true;

            Image ring = AddImage(CreateUI("Ring", hoe.transform), ringSprite, new Color(0.75f, 0.55f, 0.25f, 0.75f));
            PlaceCenter(ring.gameObject, Vector2.zero, new Vector2(176f, 176f));

            GameObject swing = CreateUI("SwingRoot", hoe.transform);
            PlaceCenter(swing, Vector2.zero, new Vector2(176f, 176f));
            GameObject iconGo = CreateUI("Icon", swing.transform);
            PlaceCenter(iconGo, new Vector2(0f, 4f), new Vector2(152f, 78f));
            Image icon = AddImage(iconGo, hoeSprite, Color.white);
            icon.preserveAspect = true;
            iconGo.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);   // 顺时针倾斜

            Button hoeButtonComponent = hoe.AddComponent<Button>();
            hoeButtonComponent.targetGraphic = bg;
            hoeButtonComponent.transition = Selectable.Transition.None;   // 状态切换不改变外观，保证锄头始终可见

            HoeButtonUI hoeUi = hoe.AddComponent<HoeButtonUI>();
            hoeUi.gameManager = gm;
            hoeUi.detector = playerGo.GetComponent<InteractionDetector>();
            hoeUi.button = hoeButtonComponent;
            hoeUi.background = bg;
            hoeUi.ring = ring;
            hoeUi.icon = icon;
            hoeUi.group = hoeGroup;
            hoeUi.swingRoot = swing.GetComponent<RectTransform>();
            gm.hoeRoot = hoe;

            gm.progressUI = progress;
            gm.hudRoot = hud;
        }

        // ------------------------------------------------------------------
        // 开场介绍弹窗
        // ------------------------------------------------------------------
        private static void BuildIntro(Transform canvas, GameManager gm)
        {
            GameObject root = CreateUI("Intro", canvas);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            GameObject dim = CreateUI("Dim", root.transform);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            Image dimImg = AddImage(dim, null, new Color(0.05f, 0.04f, 0.03f, 0.62f));
            dimImg.raycastTarget = true;

            GameObject panel = CreateUI("Panel", root.transform);
            PlaceCenter(panel, Vector2.zero, new Vector2(1440f, 980f));
            AddImage(panel, panelSprite, Paper, Image.Type.Sliced);

            Text title = AddText(panel.transform, "Title", "江南初冬 · 天然染料采集", 58, Ink,
                TextAnchor.MiddleCenter, new Vector2(0f, -44f), new Vector2(1000f, 72f));
            title.fontStyle = FontStyle.Bold;

            GameObject divider = CreateUI("Divider", panel.transform);
            PlaceTopCenter(divider, new Vector2(0f, -126f), new Vector2(980f, 2f));
            AddImage(divider, null, new Color(0.23f, 0.17f, 0.12f, 0.25f));

            string introBody =
                "冬天来了，江南水乡染上了一层淡淡的灰蓝。\n" +
                "你是一位走街串巷的染坊学徒，今日的任务——\n" +
                "在工坊周围的山野之间，采集 5 份天然染料。\n\n" +
                "· 踩方向键移动人物，走到植物旁\n" +
                "· 点击右下角锄头按钮采集\n" +
                "· 采到坏损染料可以丢弃，采到 5 件健康染料即可完成本次探索";

            Text intro = AddText(panel.transform, "IntroText", introBody, 28,
                new Color(0.23f, 0.17f, 0.12f, 0.92f),
                TextAnchor.UpperCenter, new Vector2(0f, -170f), new Vector2(1160f, 230f));
            intro.horizontalOverflow = HorizontalWrapMode.Wrap;
            intro.verticalOverflow = VerticalWrapMode.Truncate;
            intro.lineSpacing = 1.25f;

            GameObject dyeRow = CreateUI("DyeRow", panel.transform);
            PlaceTopCenter(dyeRow, new Vector2(0f, -460f), new Vector2(1320f, 260f));

            Image[] dyeIcons = new Image[5];
            Text[] dyeNames = new Text[5];

            DyeColor[] ordered = { DyeColor.Red, DyeColor.Yellow, DyeColor.Green, DyeColor.Blue, DyeColor.Purple };
            for (int i = 0; i < ordered.Length; i++)
            {
                DyeDefinition dye = dyes[ordered[i]];

                GameObject slot = CreateUI("DyeSlot_" + (i + 1), dyeRow.transform);
                PlaceTopCenter(slot, new Vector2(-500f + i * 250f, 0f), new Vector2(230f, 250f));

                GameObject frame = CreateUI("Frame", slot.transform);
                PlaceTopCenter(frame, new Vector2(0f, -10f), new Vector2(200f, 180f));
                AddImage(frame, innerSprite, new Color(1f, 1f, 1f, 0.85f), Image.Type.Sliced);

                GameObject iconGo = CreateUI("Icon", frame.transform);
                PlaceCenter(iconGo, Vector2.zero, new Vector2(170f, 170f));
                Image icon = AddImage(iconGo, dye != null ? dye.normalSprite : null, Color.white);
                icon.preserveAspect = true;
                dyeIcons[i] = icon;

                GameObject chip = CreateUI("Chip", slot.transform);
                PlaceTopCenter(chip, new Vector2(0f, -206f), new Vector2(216f, 42f));
                AddImage(chip, chipSprite, new Color(0.32f, 0.24f, 0.16f, 0.92f), Image.Type.Sliced);

                Text name = AddText(chip.transform, "Name", dye != null ? dye.chineseName : "", 24, Paper,
                    TextAnchor.MiddleCenter, Vector2.zero, new Vector2(210f, 36f), false);
                dyeNames[i] = name;
            }

            Button start = AddButton(panel.transform, "StartButton", new Vector2(0f, -820f),
                new Vector2(360f, 100f), "开始探索", Ink, buttonSprite);

            IntroUI ui = root.AddComponent<IntroUI>();
            ui.gameManager = gm;
            ui.group = group;
            ui.panel = panel.GetComponent<RectTransform>();
            ui.titleText = title;
            ui.introText = intro;
            ui.dyeIcons = dyeIcons;
            ui.dyeNames = dyeNames;
            ui.startButton = start;

            gm.introUI = ui;
        }

        // ------------------------------------------------------------------
        // 收下 / 丢弃弹窗
        // ------------------------------------------------------------------
        private static void BuildRewardPopup(Transform canvas, GameManager gm)
        {
            GameObject root = CreateUI("RewardPopup", canvas);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            GameObject dim = CreateUI("Dim", root.transform);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            Image dimImg = AddImage(dim, null, new Color(0.05f, 0.04f, 0.03f, 0.55f));
            dimImg.raycastTarget = true;

            GameObject panel = CreateUI("Panel", root.transform);
            PlaceCenter(panel, Vector2.zero, new Vector2(760f, 830f));
            AddImage(panel, panelSprite, Paper, Image.Type.Sliced);

            Text title = AddText(panel.transform, "Title", "采集成功", 44, Ink,
                TextAnchor.MiddleCenter, new Vector2(0f, -32f), new Vector2(620f, 60f));

            GameObject divider = CreateUI("Divider", panel.transform);
            PlaceTopCenter(divider, new Vector2(0f, -100f), new Vector2(620f, 2f));
            AddImage(divider, null, new Color(0.23f, 0.17f, 0.12f, 0.25f));

            GameObject iconFrame = CreateUI("IconFrame", panel.transform);
            PlaceTopCenter(iconFrame, new Vector2(0f, -116f), new Vector2(230f, 230f));
            AddImage(iconFrame, innerSprite, new Color(1f, 1f, 1f, 0.85f), Image.Type.Sliced);

            GameObject iconGo = CreateUI("Icon", iconFrame.transform);
            PlaceCenter(iconGo, Vector2.zero, new Vector2(204f, 204f));
            Image icon = AddImage(iconGo, null, Color.white);
            icon.preserveAspect = true;

            Text nameText = AddText(panel.transform, "Name", "红色染料", 34, Ink,
                TextAnchor.MiddleCenter, new Vector2(0f, -436f), new Vector2(660f, 48f));

            Text plantText = AddText(panel.transform, "PlantName", "红花", 58, Ink,
                TextAnchor.MiddleCenter, new Vector2(0f, -358f), new Vector2(680f, 72f));

            Text descText = AddText(panel.transform, "Description",
                "花瓣可提取红色染料，为丝织物染出鲜亮的红色。", 32,
                new Color(0.23f, 0.17f, 0.12f, 0.92f),
                TextAnchor.UpperCenter, new Vector2(0f, -498f), new Vector2(470f, 110f));
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.verticalOverflow = VerticalWrapMode.Truncate;
            descText.lineSpacing = 1.1f;

            GameObject chip = CreateUI("StateChip", panel.transform);
            PlaceTopCenter(chip, new Vector2(0f, -612f), new Vector2(304f, 74f));
            Image chipImg = AddImage(chip, chipSprite, new Color(0.42f, 0.56f, 0.32f, 0.92f), Image.Type.Sliced);

            Text stateText = AddText(chip.transform, "StateText", "状态：正常", 34, Paper,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(292f, 62f), false);

            Button accept = AddButton(panel.transform, "AcceptButton", new Vector2(-132f, -706f),
                new Vector2(240f, 86f), "收下", Ink, buttonSprite);
            Button discard = AddButton(panel.transform, "DiscardButton", new Vector2(132f, -706f),
                new Vector2(240f, 86f), "丢弃", Ink, buttonSprite);

            ItemRewardPopup popup = root.AddComponent<ItemRewardPopup>();
            popup.gameManager = gm;
            popup.group = group;
            popup.panel = panel.GetComponent<RectTransform>();
            popup.icon = icon;
            popup.titleText = title;
            popup.nameText = nameText;
            popup.plantText = plantText;
            popup.descText = descText;
            popup.stateText = stateText;
            popup.stateChip = chipImg;
            popup.acceptButton = accept;
            popup.discardButton = discard;

            gm.rewardPopup = popup;
            gm.rewardManager.popup = popup;
        }

        // ------------------------------------------------------------------
        // 结算界面
        // ------------------------------------------------------------------
        private static void BuildSettlement(Transform canvas, GameManager gm)
        {
            GameObject root = CreateUI("Settlement", canvas);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            GameObject dim = CreateUI("Dim", root.transform);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            Image dimImg = AddImage(dim, null, new Color(0.05f, 0.04f, 0.03f, 0.68f));
            dimImg.raycastTarget = true;

            GameObject panel = CreateUI("Panel", root.transform);
            PlaceCenter(panel, Vector2.zero, new Vector2(1560f, 920f));
            AddImage(panel, panelSprite, Paper, Image.Type.Sliced);

            Text title = AddText(panel.transform, "Title", "本次采集成果", 58, Ink,
                TextAnchor.MiddleCenter, new Vector2(0f, -38f), new Vector2(900f, 72f));

            Text subtitle = AddText(panel.transform, "Subtitle", "本次共收集到 5 件天然染料", 30, InkSoft,
                TextAnchor.MiddleCenter, new Vector2(0f, -112f), new Vector2(900f, 44f));

            Text summary = AddText(panel.transform, "Summary", "共采集 0 次　·　收下 0 件　·　丢弃 0 次", 23,
                new Color(0.42f, 0.34f, 0.25f, 0.75f), TextAnchor.MiddleCenter,
                new Vector2(0f, -154f), new Vector2(1000f, 36f));

            SettlementUI ui = root.AddComponent<SettlementUI>();
            ui.gameManager = gm;
            ui.group = group;
            ui.panel = panel.GetComponent<RectTransform>();
            ui.titleText = title;
            ui.subtitleText = subtitle;
            ui.summaryText = summary;

            ui.itemCards = new GameObject[5];
            ui.itemIcons = new Image[5];
            ui.itemNames = new Text[5];
            ui.itemStates = new Text[5];

            for (int i = 0; i < 5; i++)
            {
                GameObject card = CreateUI("ItemCard_" + (i + 1), panel.transform);
                PlaceTopCenter(card, new Vector2(-560f + i * 280f, -200f), new Vector2(250f, 316f));
                AddImage(card, innerSprite, new Color(1f, 1f, 1f, 0.7f), Image.Type.Sliced);

                GameObject cardIcon = CreateUI("Icon", card.transform);
                PlaceTopCenter(cardIcon, new Vector2(0f, -14f), new Vector2(170f, 170f));
                Image cardIconImg = AddImage(cardIcon, null, Color.white);
                cardIconImg.preserveAspect = true;

                Text cardName = AddText(card.transform, "Name", "1. 红色染料", 26, Ink,
                    TextAnchor.MiddleCenter, new Vector2(0f, -200f), new Vector2(230f, 40f));

                Text cardState = AddText(card.transform, "State", "正常", 24, Moss,
                    TextAnchor.MiddleCenter, new Vector2(0f, -242f), new Vector2(230f, 36f));

                ui.itemCards[i] = card;
                ui.itemIcons[i] = cardIconImg;
                ui.itemNames[i] = cardName;
                ui.itemStates[i] = cardState;
            }

            ui.statTexts = new Text[5];
            for (int i = 0; i < 5; i++)
            {
                Text stat = AddText(panel.transform, "Stat_" + (i + 1), "红色：正常 0 件，坏损 0 件", 26, Ink,
                    TextAnchor.MiddleCenter, new Vector2(0f, -546f - i * 46f), new Vector2(900f, 42f));
                stat.horizontalOverflow = HorizontalWrapMode.Overflow;
                ui.statTexts[i] = stat;
            }

            Button restart = AddButton(panel.transform, "RestartButton", new Vector2(0f, -790f),
                new Vector2(320f, 88f), "前往研磨", Ink, buttonSprite);
            ui.restartButton = restart;

            gm.settlementUI = ui;
        }

        // ------------------------------------------------------------------
        // 提示条
        // ------------------------------------------------------------------
        private static void BuildToast(Transform canvas, GameManager gm)
        {
            GameObject root = CreateUI("Toast", canvas);
            PlaceBottomCenter(root, new Vector2(0f, 266f), new Vector2(760f, 68f));
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            AddImage(root, chipSprite, new Color(0.16f, 0.12f, 0.09f, 0.82f), Image.Type.Sliced);

            Text text = AddText(root.transform, "Text", "", 27, Paper, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(720f, 56f), false);

            ToastUI toast = root.AddComponent<ToastUI>();
            toast.group = group;
            toast.text = text;
            gm.toast = toast;
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.companyName = "Jiangnan Studio";
            PlayerSettings.productName = "江南初冬·天然染料采集";
            // 编辑器失去焦点时也保持运行，避免测试 / 后台调试时游戏循环停住
            PlayerSettings.runInBackground = true;
        }
    }
}
#endif
