#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 秋天场景 Level3_Autumn.unity 一键恢复工具。
/// 所有位置 / 缩放 / sortingOrder / 碰撞盒尺寸均从当前手动搭建的秋天场景同步而来，
/// 仅在场景损坏需要重建时使用，不会自动修改现有场景。
/// 菜单: Tools/Autumn Setup/恢复秋天场景 (Level3_Autumn)
/// </summary>
public static class AutumnSceneRestore
{
    private const string BgPath = "Assets/关卡/3-秋/第三关-秋.png";
    private const string PlayerAnimCtrl = "Assets/animation/player.controller";
    private const string PlayerIdle = "Assets/animation/a-idle/待机_00001.png";
    private const string NpcSprite = "Assets/关卡/3-秋/3-NPC动画关键帧/待机关键帧_01.png";
    private const string NpcAnimCtrl = "Assets/关卡/3-秋/3-NPC动画关键帧/待机关键帧_01.controller";

    [MenuItem("Tools/Autumn Setup/恢复秋天场景 (Level3_Autumn)")]
    public static void Restore()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogError("[AutumnSceneRestore] 没有打开的场景"); return; }

        Debug.Log("========== Autumn Scene Restore Start ==========");

        CreatePlayer();
        var cam = CreateMainCamera();
        CreateBackground();
        CreateCanvasAndLabel(cam);
        CreateEventSystem();
        CreateNpc();
        CreateAutumnItems();
        CreateCollisionBoxes();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("========== Autumn Scene Restore Done ==========");
    }

    [MenuItem("Tools/Autumn Setup/仅恢复核心物体 (Player+Camera+BG)")]
    public static void RestoreCoreOnly()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        CreatePlayer();
        var cam = CreateMainCamera();
        CreateBackground();
        CreateCanvasAndLabel(cam);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[AutumnSceneRestore] ✅ 核心物体恢复完成");
    }

    // ==================== Player ====================
    private static GameObject CreatePlayer()
    {
        var existing = GameObject.Find("Player");
        if (existing != null)
        {
            Debug.Log("[AutumnSceneRestore] Player 已存在，跳过");
            return existing;
        }

        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, -3.8f, 0f);
        player.transform.localScale = Vector3.one;

        // Visual 子物体（位置/缩放/排序同步自场景）
        var visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.68f, 0f);
        visual.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);

        var visualSr = visual.AddComponent<SpriteRenderer>();
        visualSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerIdle);
        visualSr.sortingOrder = 5;

        // Animator
        var anim = player.AddComponent<Animator>();
        var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimCtrl);
        if (animCtrl != null)
        {
            anim.runtimeAnimatorController = animCtrl;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        else
        {
            Debug.LogWarning("[AutumnSceneRestore] 找不到 player.controller，Animator 没配");
        }

        // LevelPlayerController（参数同步自场景；Rigidbody2D 由控制器在运行时补齐）
        var pc = player.AddComponent<Qingming.LevelBackground.LevelPlayerController>();
        pc.moveSpeed = 5f;
        pc.runMultiplier = 1.6f;
        pc.jumpHeight = 3.5f;
        pc.visualTransform = visual.transform;
        pc.animator = anim;
        pc.minX = -100f;
        pc.maxX = 100f;
        pc.clampHorizontal = false;
        pc.groundCheckDistance = 0.3f;
        pc.fallResetY = -10f;
        pc.spawnPoint = new Vector3(0f, -1f, 0f);

        Debug.Log($"  ✅ Player 创建完成, pos={player.transform.position}");
        return player;
    }

    // ==================== Main Camera ====================
    private static Camera CreateMainCamera()
    {
        var existing = GameObject.Find("Main Camera");
        if (existing != null)
        {
            Debug.Log("[AutumnSceneRestore] Main Camera 已存在，跳过");
            return existing.GetComponent<Camera>();
        }

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.backgroundColor = new Color(0.95f, 0.78f, 0.55f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        camGo.AddComponent<AudioListener>();

        var cf = camGo.AddComponent<Qingming.LevelBackground.CameraFollow>();
        cf.target = null; // 留空，运行时自动找 Player
        cf.offset = new Vector2(0f, 1.5f);
        cf.smoothSpeedX = 8f;
        cf.smoothSpeedY = 5f;
        cf.clampX = true;
        cf.minX = -20f;
        cf.maxX = 20f;

        Debug.Log("  ✅ Main Camera 创建完成");
        return cam;
    }

    // ==================== Background ====================
    private static void CreateBackground()
    {
        var existing = GameObject.Find("Background");
        if (existing != null) { Debug.Log("[AutumnSceneRestore] Background 已存在，跳过"); return; }

        var bgGo = new GameObject("Background");
        bgGo.transform.position = new Vector3(4.88f, 0f, 0f);
        bgGo.transform.localScale = new Vector3(0.8789192f, 0.8789192f, 0.49610105f);

        var sr = bgGo.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
        sr.sortingOrder = -10;

        Debug.Log($"  ✅ Background 创建完成 (sprite={(sr.sprite != null ? sr.sprite.name : "NULL")})");
    }

    // ==================== Canvas + 关卡标题 ====================
    private static void CreateCanvasAndLabel(Camera cam)
    {
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
        }

    }

    // ==================== EventSystem ====================
    private static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            Debug.Log("[AutumnSceneRestore] EventSystem 已存在，跳过");
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Debug.Log("  ✅ EventSystem 创建完成");
    }

    // ==================== NPC ====================
    private static void CreateNpc()
    {
        const string npcName = "织染师傅";
        if (GameObject.Find(npcName) != null)
        {
            Debug.Log("[AutumnSceneRestore] NPC 已存在，跳过");
            return;
        }

        var npc = new GameObject(npcName);
        npc.transform.position = new Vector3(8.19f, -2.29f, 0f);
        npc.transform.localScale = new Vector3(-0.45938486f, 0.46324372f, 0.46324372f);

        var sr = npc.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(NpcSprite);
        sr.sortingOrder = 0;

        var anim = npc.AddComponent<Animator>();
        var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(NpcAnimCtrl);
        if (animCtrl != null)
        {
            anim.runtimeAnimatorController = animCtrl;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        else
        {
            Debug.LogWarning($"[AutumnSceneRestore] 找不到 NPC 动画控制器: {NpcAnimCtrl}");
        }

        Debug.Log("  ✅ NPC 创建完成");
    }

    // ==================== 秋天道具 ====================
    // 每个道具的位置 / 缩放 / sortingOrder / 碰撞盒尺寸均从手动搭建的秋天场景同步而来
    // 道具可同时拥有一个 Trigger 碰撞盒（交互）和一个实体碰撞盒（阻挡）
    private struct AutumnItem
    {
        public string Name;
        public string SpritePath;
        public Vector3 Pos;
        public Vector3 Scale;
        public int SortingOrder;
        public Vector2? TriggerColliderSize;   // null = 没有 Trigger 碰撞盒
        public Vector2? SolidColliderSize;     // null = 没有实体碰撞盒

        public AutumnItem(string name, string spritePath, Vector3 pos, Vector3 scale,
                          int sortingOrder, Vector2? triggerColliderSize, Vector2? solidColliderSize)
        {
            Name = name;
            SpritePath = spritePath;
            Pos = pos;
            Scale = scale;
            SortingOrder = sortingOrder;
            TriggerColliderSize = triggerColliderSize;
            SolidColliderSize = solidColliderSize;
        }
    }

    private static readonly string ItemDir = "Assets/关卡/3-秋/道具/";

    private static readonly AutumnItem[] AutumnItems = new AutumnItem[]
    {
        // (名字, 图片路径, 位置, 缩放, sortingOrder, Trigger碰撞盒(null=不创建), 实体碰撞盒(null=不创建))
        new AutumnItem("3-酒 (1)", ItemDir + "3-酒.png",
            new Vector3(-11.53f, -0.79f, 0f), new Vector3(0.260889f, 0.260889f, 0.260889f),
            0, new Vector2(5.01f, 5.08f), new Vector2(5.01f, 5.08f)),
        new AutumnItem("3-酒", ItemDir + "3-酒.png",
            new Vector3(-6.913f, -1.28f, 0f), new Vector3(0.23796226f, 0.23796226f, 0.23796226f),
            0, new Vector2(5.01f, 5.08f), new Vector2(5.01f, 5.08f)),
        new AutumnItem("3-酒 (2)", ItemDir + "3-酒.png",
            new Vector3(19.93f, -3.9f, 0f), new Vector3(0.21629f, 0.21629f, 0.21629f),
            0, new Vector2(5.01f, 5.08f), new Vector2(5.01f, 5.08f)),
        new AutumnItem("3-酒 (3)", ItemDir + "3-酒.png",
            new Vector3(19.05f, -4.04f, 0f), new Vector3(0.21629f, 0.21629f, 0.21629f),
            0, new Vector2(5.01f, 5.08f), new Vector2(5.01f, 5.08f)),
        new AutumnItem("3-香蕉果篮", ItemDir + "3-香蕉果篮.png",
            new Vector3(2.45f, -2.16f, 0f), new Vector3(0.14003818f, 0.14003818f, 0.14003818f),
            2, null, null),
        new AutumnItem("3-树", ItemDir + "3-树.png",
            new Vector3(-0.34f, -0.48f, 0f), new Vector3(0.36570826f, 0.36570826f, 0.36570826f),
            1, null, new Vector2(13.12f, 11.99f)),
        new AutumnItem("3-苹果贡品", ItemDir + "3-苹果贡品.png",
            new Vector3(-5.21f, -1.14f, 0f), new Vector3(0.17637672f, 0.17637672f, 0.17637672f),
            0, new Vector2(2.92f, 3.35f), null),
        new AutumnItem("3-橘子贡品", ItemDir + "3-橘子贡品.png",
            new Vector3(-4.784f, -1.161f, 0f), new Vector3(0.15108564f, 0.15108564f, 0.15108564f),
            0, new Vector2(2.91f, 3.34f), null),
        new AutumnItem("3-水稻贡品", ItemDir + "3-水稻贡品.png",
            new Vector3(-7.514f, -1.13f, 0f), new Vector3(0.31088853f, 0.31088853f, 0.31088853f),
            0, new Vector2(3.73f, 4.91f), null),
        new AutumnItem("3-水稻贡品 (1)", ItemDir + "3-水稻贡品.png",
            new Vector3(-2.89f, -1.22f, 0f), new Vector3(0.2909172f, 0.2909172f, 0.2909172f),
            0, new Vector2(3.73f, 4.91f), null),
        new AutumnItem("3-水稻贡品2", ItemDir + "3-水稻贡品2.png",
            new Vector3(-5.66f, -0.9f, 0f), new Vector3(0.25904113f, 0.25904113f, 0.25904113f),
            0, new Vector2(4.53f, 4.04f), null),
        new AutumnItem("3-水稻贡品2 (1)", ItemDir + "3-水稻贡品2.png",
            new Vector3(10f, -1.47f, 0f), new Vector3(0.23649323f, 0.23649323f, 0.23649323f),
            2, new Vector2(4.53f, 4.04f), null),
        new AutumnItem("3-丝束", ItemDir + "3-丝束.png",
            new Vector3(-4.257f, -1.03f, 0f), new Vector3(0.22959508f, 0.22959508f, 0.22959508f),
            0, new Vector2(3.26f, 3.52f), new Vector2(3.26f, 3.52f)),
        new AutumnItem("3-丝束 (1)", ItemDir + "3-丝束.png",
            new Vector3(-3.897f, -1.03f, 0f), new Vector3(0.22959508f, 0.22959508f, 0.22959508f),
            0, new Vector2(3.26f, 3.52f), new Vector2(3.26f, 3.52f)),
        new AutumnItem("3-破损的织机", ItemDir + "3-破损的织机.png",
            new Vector3(16.87f, -0.82f, 0f), new Vector3(0.2104f, 0.2104f, 0.2104f),
            0, new Vector2(13.11f, 12f), new Vector2(13.11f, 12f)),
        new AutumnItem("3-织好的白布+架子", ItemDir + "3-织好的白布+架子.png",
            new Vector3(10.87f, -0.94f, 0f), new Vector3(0.17579988f, 0.17579988f, 0.17579988f),
            0, new Vector2(15.36f, 10.24f), new Vector2(15.36f, 10.24f)),
        new AutumnItem("3-织好的白布+架子 (1)", ItemDir + "3-织好的白布+架子.png",
            new Vector3(13.38f, -0.94f, 0f), new Vector3(0.17579988f, 0.17579988f, 0.17579988f),
            0, new Vector2(15.36f, 10.24f), new Vector2(15.36f, 10.24f)),
        new AutumnItem("3-织好的白布+架子 (2)", ItemDir + "3-织好的白布+架子.png",
            new Vector3(17.97f, -3.86f, 0f), new Vector3(0.14368652f, 0.14368652f, 0.14368652f),
            0, new Vector2(15.36f, 10.24f), new Vector2(15.36f, 10.24f)),
        new AutumnItem("3-工具箱", ItemDir + "3-工具箱.png",
            new Vector3(19.97f, -2.28f, 0f), new Vector3(0.095239796f, 0.095239796f, 0.095239796f),
            0, new Vector2(15.36f, 10.24f), new Vector2(15.36f, 10.24f)),
    };

    private static void CreateAutumnItems()
    {
        int created = 0;

        foreach (var item in AutumnItems)
        {
            if (GameObject.Find(item.Name) != null)
            {
                Debug.Log($"  跳过（已存在）: {item.Name}");
                continue;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(item.SpritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[AutumnSceneRestore] 找不到图片: {item.SpritePath}");
                continue;
            }

            var go = new GameObject(item.Name);
            go.transform.position = item.Pos;
            go.transform.localScale = item.Scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = item.SortingOrder;

            if (item.TriggerColliderSize.HasValue)
            {
                var trigger = go.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = item.TriggerColliderSize.Value;
                trigger.offset = Vector2.zero;
            }

            if (item.SolidColliderSize.HasValue)
            {
                var solid = go.AddComponent<BoxCollider2D>();
                solid.isTrigger = false;
                solid.size = item.SolidColliderSize.Value;
                solid.offset = Vector2.zero;
            }

            created++;
        }

        Debug.Log($"  ✅ 创建了 {created} 个秋天道具");
    }

    // ==================== 碰撞盒 ====================
    private struct SceneCollider
    {
        public string Name;
        public Vector2 Size;
        public Vector2 Offset;

        public SceneCollider(string name, Vector2 size, Vector2 offset)
        {
            Name = name;
            Size = size;
            Offset = offset;
        }
    }

    private static readonly SceneCollider[] CollisionBoxes = new SceneCollider[]
    {
        new SceneCollider("1",      new Vector2(9.671436f, 1.709274f),   new Vector2(0.8579893f, -3.6264443f)),
        new SceneCollider("1 (1)",  new Vector2(12.714445f, 1.3660774f), new Vector2(-10.387422f, -3.7980428f)),
        new SceneCollider("1 (2)",  new Vector2(3.3086128f, 1.7684658f), new Vector2(-18.145975f, -3.5968485f)),
        new SceneCollider("1 (3)",  new Vector2(3.0068207f, 2.0325332f), new Vector2(-18.296871f, -3.464815f)),
        new SceneCollider("1 (4)",  new Vector2(2.6924515f, 2.271451f),  new Vector2(-18.454056f, -3.345356f)),
        new SceneCollider("1 (5)",  new Vector2(2.3277855f, 2.5355186f), new Vector2(-18.636389f, -3.2133222f)),
        new SceneCollider("1 (6)",  new Vector2(0.16770363f, 8.63422f),  new Vector2(-19.71643f, -0.16397142f)),
        new SceneCollider("1 (7)",  new Vector2(12.2392845f, 1.7092743f),new Vector2(10.979727f, -3.6264443f)),
        new SceneCollider("1 (8)",  new Vector2(6.7570066f, 1.9770865f), new Vector2(13.720865f, -3.4925382f)),
        new SceneCollider("1 (9)",  new Vector2(2.2987175f, 5.253851f),  new Vector2(15.95001f, -1.854156f)),
    };

    private static void CreateCollisionBoxes()
    {
        var parent = GameObject.Find("碰撞盒");
        if (parent == null)
        {
            parent = new GameObject("碰撞盒");
            parent.transform.position = new Vector3(6.2194204f, -0.088456675f, -5.070173f);
            parent.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.Log("[AutumnSceneRestore] 碰撞盒 已存在，仅补齐缺失的碰撞块");
        }

        int created = 0;
        foreach (var box in CollisionBoxes)
        {
            if (parent.transform.Find(box.Name) != null) continue;

            var child = new GameObject(box.Name);
            child.transform.SetParent(parent.transform, false);

            var col = child.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            col.size = box.Size;
            col.offset = box.Offset;
            created++;
        }

        Debug.Log($"  ✅ 碰撞盒补齐 {created} 个");
    }
}
#endif
