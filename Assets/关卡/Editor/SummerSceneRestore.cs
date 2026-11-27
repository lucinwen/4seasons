#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 夏天场景 Level2_Summer.unity 一键恢复工具。
/// 所有位置 / 缩放 / sortingOrder / 碰撞盒尺寸均从当前手动搭建的夏天场景同步而来，
/// 仅在场景损坏需要重建时使用，不会自动修改现有场景。
/// 菜单: Tools/Summer Setup/恢复夏天场景 (Level2_Summer)
/// </summary>
public static class SummerSceneRestore
{
    private const string BgPath = "Assets/关卡/2-夏/第二关-夏.png";
    private const string PlayerAnimCtrl = "Assets/animation/player.controller";
    private const string PlayerIdle = "Assets/animation/a-idle/待机_00001.png";
    private const string NpcSprite = "Assets/关卡/2-夏/2-NPC动画关键帧/idle_frame_01.png";
    private const string NpcAnimCtrl = "Assets/关卡/2-夏/2-NPC动画关键帧/idle_frame_01.controller";

    [MenuItem("Tools/Summer Setup/恢复夏天场景 (Level2_Summer)")]
    public static void Restore()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogError("[SummerSceneRestore] 没有打开的场景"); return; }

        Debug.Log("========== Summer Scene Restore Start ==========");

        CreatePlayer();
        var cam = CreateMainCamera();
        CreateBackground();
        CreateCanvasAndLabel(cam);
        CreateEventSystem();
        CreateSummerItems();
        CreateNpc();
        CreateStairColliders();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("========== Summer Scene Restore Done ==========");
    }

    [MenuItem("Tools/Summer Setup/仅恢复核心物体 (Player+Camera+BG)")]
    public static void RestoreCoreOnly()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        CreatePlayer();
        var cam = CreateMainCamera();
        CreateBackground();
        CreateCanvasAndLabel(cam);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[SummerSceneRestore] ✅ 核心物体恢复完成");
    }

    // ==================== Player ====================
    private static GameObject CreatePlayer()
    {
        var existing = GameObject.Find("Player");
        if (existing != null)
        {
            Debug.Log("[SummerSceneRestore] Player 已存在，跳过");
            return existing;
        }

        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, -3.8f, 0f);
        player.transform.localScale = Vector3.one;

        // Visual 子物体（位置/缩放/排序同步自场景）
        var visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = new Vector3(-0.88f, 0.52f, 0f);
        visual.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);

        var visualSr = visual.AddComponent<SpriteRenderer>();
        visualSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerIdle);
        visualSr.sortingOrder = 5;

        // 玩家碰撞盒（挂在 Visual 上，同步自场景）
        var visualCol = visual.AddComponent<BoxCollider2D>();
        visualCol.size = new Vector2(13.03125f, 17.375f);
        visualCol.offset = new Vector2(0f, 8.34f);
        visualCol.isTrigger = false;

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
            Debug.LogWarning("[SummerSceneRestore] 找不到 player.controller，Animator 没配");
        }

        // Rigidbody2D（挂在 Player 根物体上，Visual 上的碰撞盒作为复合碰撞体参与物理）
        var rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // LevelPlayerController（参数同步自场景）
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
            Debug.Log("[SummerSceneRestore] Main Camera 已存在，跳过");
            return existing.GetComponent<Camera>();
        }

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.backgroundColor = new Color(0.55f, 0.75f, 0.9f);
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
        if (existing != null) { Debug.Log("[SummerSceneRestore] Background 已存在，跳过"); return; }

        var bgGo = new GameObject("Background");
        bgGo.transform.position = new Vector3(2.68f, 0f, 0f);
        bgGo.transform.localScale = new Vector3(0.8947205f, 0.8947205f, 0.50502f);

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
            Debug.Log("[SummerSceneRestore] EventSystem 已存在，跳过");
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Debug.Log("  ✅ EventSystem 创建完成");
    }

    // ==================== 夏天道具 ====================
    // 每个道具的位置 / 缩放 / sortingOrder / 碰撞盒尺寸均从手动搭建的夏天场景同步而来
    private struct SummerItem
    {
        public string Name;
        public string SpritePath;
        public Vector3 Pos;
        public Vector3 Scale;
        public int SortingOrder;
        public Vector2? ColliderSize;   // null = 该道具没有碰撞盒
        public bool ColliderIsTrigger;

        public SummerItem(string name, string spritePath, Vector3 pos, Vector3 scale,
                          int sortingOrder, Vector2? colliderSize, bool colliderIsTrigger = true)
        {
            Name = name;
            SpritePath = spritePath;
            Pos = pos;
            Scale = scale;
            SortingOrder = sortingOrder;
            ColliderSize = colliderSize;
            ColliderIsTrigger = colliderIsTrigger;
        }
    }

    private static readonly string ItemDir = "Assets/关卡/2-夏/道具/";

    private static readonly SummerItem[] SummerItems = new SummerItem[]
    {
        // (名字, 图片路径, 位置, 缩放, sortingOrder, 碰撞盒大小(null=不创建), 是否Trigger)
        new SummerItem("2-木柴", ItemDir + "2-木柴.png",
            new Vector3(-14.52f, -2.7f, 0f), new Vector3(0.32089198f, 0.32089198f, 0.32089198f),
            0, new Vector2(3.08f, 2.29f)),
        // 2-水源 在当前场景中使用的就是这张图（按场景实际引用同步，未替换）
        new SummerItem("2-水源", "Assets/关卡/1-春/道具/1-浴蚕桶.png",
            new Vector3(6.04f, -2.12f, 0f), new Vector3(0.32236f, 0.32236f, 0.32236f),
            0, new Vector2(3.17f, 3.3f)),
        new SummerItem("2-煮茧锅", ItemDir + "2-煮茧锅.png",
            new Vector3(11.93f, -2.01f, 0f), new Vector3(0.37268615f, 0.37268615f, 0.37268615f),
            0, new Vector2(4.32f, 4.91f)),
        new SummerItem("2-盐窖", ItemDir + "2-盐窖.png",
            new Vector3(-2.62f, -2.26f, 0f), new Vector3(0.25165f, 0.25165f, 0.25165f),
            0, new Vector2(3.85f, 4.88f)),
        new SummerItem("2-缫丝车", ItemDir + "2-缫丝车.png",
            new Vector3(14.26f, -2.12f, 0f), new Vector3(0.16353293f, 0.17521448f, 0.17521448f),
            0, new Vector2(15.36f, 10.24f)),
        new SummerItem("2-蚕茧簇架", ItemDir + "2-蚕茧簇架.png",
            new Vector3(-11.2f, -2.12f, 0f), new Vector3(-0.44261885f, 0.45788f, 0.45788f),
            0, new Vector2(4.84f, 4.42f)),
        new SummerItem("2-蚕茧匾", ItemDir + "2-蚕茧匾.png",
            new Vector3(-7.5f, -2.68f, 0f), new Vector3(0.30417982f, 0.30417982f, 0.30417982f),
            0, null),
        new SummerItem("2-蚕茧篓", ItemDir + "2-蚕茧篓.png",
            new Vector3(-5.72f, -2.33f, 0f), new Vector3(0.23397f, 0.23397f, 0.23397f),
            0, null),
        new SummerItem("2-蚕茧篓 (1)", ItemDir + "2-蚕茧篓.png",
            new Vector3(-4.63f, -2.33f, 0f), new Vector3(0.23397f, 0.23397f, 0.23397f),
            0, null),
    };

    private static void CreateSummerItems()
    {
        int created = 0;

        foreach (var item in SummerItems)
        {
            if (GameObject.Find(item.Name) != null)
            {
                Debug.Log($"  跳过（已存在）: {item.Name}");
                continue;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(item.SpritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[SummerSceneRestore] 找不到图片: {item.SpritePath}");
                continue;
            }

            var go = new GameObject(item.Name);
            go.transform.position = item.Pos;
            go.transform.localScale = item.Scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = item.SortingOrder;

            if (item.ColliderSize.HasValue)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = item.ColliderIsTrigger;
                col.size = item.ColliderSize.Value;
                col.offset = Vector2.zero;
            }

            created++;
        }

        Debug.Log($"  ✅ 创建了 {created} 个夏天道具");
    }

    // ==================== 台阶碰撞盒 ====================
    private struct StairCollider
    {
        public string Name;
        public Vector2 Size;
        public Vector2 Offset;

        public StairCollider(string name, Vector2 size, Vector2 offset)
        {
            Name = name;
            Size = size;
            Offset = offset;
        }
    }

    private static readonly StairCollider[] StairColliders = new StairCollider[]
    {
        new StairCollider("1",      new Vector2(12.567939f, 1.3423042f), new Vector2(0.09092188f, -3.9004564f)),
        new StairCollider("1 (1)",  new Vector2(8.9634695f, 1.1621447f), new Vector2(-10.683262f, -3.9905362f)),
        new StairCollider("1 (2)",  new Vector2(10.686308f, 1.1621447f), new Vector2(11.713513f, -3.9905362f)),
        new StairCollider("1 (3)",  new Vector2(4.922327f, 4.5781593f),  new Vector2(19.384167f, -2.2825289f)),
        // 场景新增：右侧台阶与左侧边界
        new StairCollider("1 (4)",  new Vector2(4.2194230f, 0.1935654f), new Vector2(3.4631180f, -3.1303630f)),
        new StairCollider("1 (5)",  new Vector2(3.5807560f, 0.1985168f), new Vector2(3.4061830f, -2.9793600f)),
        new StairCollider("1 (6)",  new Vector2(2.9123820f, 0.1539583f), new Vector2(3.4284620f, -2.8283570f)),
        new StairCollider("1 (7)",  new Vector2(2.3430270f, 0.1292038f), new Vector2(3.4754950f, -2.6724030f)),
        new StairCollider("1 (8)",  new Vector2(1.5954380f, 0.0945473f), new Vector2(3.4532160f, -2.5362530f)),
        new StairCollider("1 (9)",  new Vector2(0.6795192f, 0.0598908f), new Vector2(3.4309370f, -2.4595140f)),
        new StairCollider("1 (10)", new Vector2(0.1294117f, 4.8738490f), new Vector2(-15.0502700f, -2.1847070f)),
    };

    // ==================== NPC ====================
    private static void CreateNpc()
    {
        const string npcName = "缫丝师傅";
        if (GameObject.Find(npcName) != null)
        {
            Debug.Log("[SummerSceneRestore] NPC 已存在，跳过");
            return;
        }

        var npc = new GameObject(npcName);
        npc.transform.position = new Vector3(15.85f, -2.38f, 0f);
        npc.transform.localScale = new Vector3(-0.14779168f, 0.14779168f, 0.15977481f);

        var sr = npc.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(NpcSprite);
        sr.sortingOrder = 10;

        var anim = npc.AddComponent<Animator>();
        var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(NpcAnimCtrl);
        if (animCtrl != null)
        {
            anim.runtimeAnimatorController = animCtrl;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        else
        {
            Debug.LogWarning($"[SummerSceneRestore] 找不到 NPC 动画控制器: {NpcAnimCtrl}");
        }

        Debug.Log("  ✅ NPC 创建完成");
    }

    private static void CreateStairColliders()
    {
        var parent = GameObject.Find("台阶碰撞盒");
        if (parent == null)
        {
            parent = new GameObject("台阶碰撞盒");
            parent.transform.position = new Vector3(-0.6798855f, 0.06777373f, 0.0006464261f);
            parent.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.Log("[SummerSceneRestore] 台阶碰撞盒 已存在，仅补齐缺失的碰撞块");
        }

        int created = 0;
        foreach (var stair in StairColliders)
        {
            if (parent.transform.Find(stair.Name) != null) continue;

            var child = new GameObject(stair.Name);
            child.transform.SetParent(parent.transform, false);

            var col = child.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            col.size = stair.Size;
            col.offset = stair.Offset;
            created++;
        }

        Debug.Log($"  ✅ 台阶碰撞盒补齐 {created} 个");
    }
}
#endif
