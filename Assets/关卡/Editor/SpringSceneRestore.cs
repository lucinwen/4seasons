#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 春天场景 Level1_Spring.unity 一键恢复工具。
/// 场景被编译错误/Undo 操作搞坏时跑这个，重建：Player + Camera + Background + Canvas + 所有道具 + Trigger。
/// 菜单: Tools/Spring Setup/恢复春天场景 (Level1_Spring)
/// </summary>
public static class SpringSceneRestore
{
    private const string BgPath = "Assets/关卡/1-春/第一关-春.png";
    private const string PlayerAnimCtrl = "Assets/animation/player.controller";
    private const string PlayerIdle = "Assets/animation/a-idle/待机_00001.png";

    [MenuItem("Tools/Spring Setup/恢复春天场景 (Level1_Spring)")]
    public static void Restore()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogError("[SpringSceneRestore] 没有打开的场景"); return; }

        // 1. 先清空现有物体（保留光圈/MissionPromptUI，它们不冲突）
        // 不删 EventSystem，避免引用丢失

        Debug.Log("========== Spring Scene Restore Start ==========");

        // ---- 2. 创建 Player ----
        var player = CreatePlayer();

        // ---- 3. 创建 Main Camera ----
        CreateMainCamera(player.transform);

        // ---- 4. 创建 Background ----
        CreateBackground();

        // ---- 5. 批量创建春天道具 ----
        CreateSpringItems();

        // ---- 7. 台阶碰撞盒 ----
        CreateStepColliders();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("========== Spring Scene Restore Done ==========");
    }

    [MenuItem("Tools/Spring Setup/仅恢复核心物体 (Player+Camera+BG)")]
    public static void RestoreCoreOnly()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        var player = CreatePlayer();
        CreateMainCamera(player.transform);
        CreateBackground();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[SpringSceneRestore] ✅ 核心物体恢复完成");
    }

    // ==================== Player ====================
    private static GameObject CreatePlayer()
    {
        var existing = GameObject.Find("Player");
        if (existing != null)
        {
            Debug.Log("[SpringSceneRestore] Player 已存在，跳过");
            return existing;
        }

        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, -1.0f, 0);
        player.transform.localScale = Vector3.one;

        // Visual 子物体
        var visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.562f, 0f);
        visual.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
        var visualSr = visual.AddComponent<SpriteRenderer>();
        visualSr.sortingOrder = 5;
        // 尝试加载 Player sprite
        var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerIdle);
        if (playerSprite == null)
        {
            // 用 Resources 里的 default
            playerSprite = Resources.GetBuiltinResource<Sprite>("LegacyRuntime.png");
        }
        visualSr.sprite = playerSprite;

        // LevelPlayerController
        var pc = player.AddComponent<Qingming.LevelBackground.LevelPlayerController>();
        pc.jumpHeight = 2.5f;
        pc.visualTransform = visual.transform;

        // Animator
        var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimCtrl);
        if (animCtrl != null)
        {
            var anim = player.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrl;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            pc.animator = anim;
        }
        else
        {
            Debug.LogWarning("[SpringSceneRestore] 找不到 player.controller，Animator 没配");
        }

        // Rigidbody2D (Dynamic，参与物理碰撞，可跳跃上楼梯)
        var rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // BoxCollider2D
        var col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 1.2f);
        col.offset = new Vector2(0, 0);
        col.isTrigger = false;

        Debug.Log($"  ✅ Player 创建完成, pos={player.transform.position}");
        return player;
    }

    // ==================== Main Camera ====================
    private static void CreateMainCamera(Transform target)
    {
        var existing = GameObject.Find("Main Camera");
        if (existing != null) { Debug.Log("[SpringSceneRestore] Main Camera 已存在，跳过"); return; }

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 2f, -10f);

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.5f, 0.7f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        camGo.AddComponent<AudioListener>();

        var cf = camGo.AddComponent<Qingming.LevelBackground.CameraFollow>();
        cf.target = target;
        cf.offset = new Vector2(0, 1.5f);
        cf.clampX = true;

        Debug.Log("  ✅ Main Camera 创建完成");
    }

    // ==================== Background ====================
    private static void CreateBackground()
    {
        var bgExisting = GameObject.Find("第一关-春");
        if (bgExisting != null) { Debug.Log("[SpringSceneRestore] 第一关-春 已存在，跳过"); return; }

        var bgGo = new GameObject("第一关-春");
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
        if (bgSprite == null)
        {
            var springBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/关卡/1-春/第一关-春.png");
            if (springBg != null) bgSprite = springBg;
        }
        var bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = bgSprite;
        bgSr.sortingOrder = -10;
        bgGo.transform.position = new Vector3(4.47f, 2.1041f, 0f);
        bgGo.transform.localScale = Vector3.one;

        Debug.Log($"  ✅ Background 创建完成 (sprite={(bgSprite != null ? bgSprite.name : "NULL")})");
    }

    // ==================== 春天道具 ====================
    // 每个道具的位置 / 缩放 / sortingOrder / 碰撞盒尺寸 / 交互组件均从手动搭建的春天场景同步而来
    private enum SpringItemKind
    {
        Plain,          // 纯装饰
        Pickup,         // PickupItem
        SilkwormRack,   // 春季簇架仅作场景陈设
        WashingEgg,     // SilkwormWashingController (role = Egg)
        WashingBucket,  // SilkwormWashingController (role = Bucket)
        Prompt          // SpringPrompt（采桑游戏触发点位）
    }

    private struct SpringItem
    {
        public string Name;
        public string SpritePath;          // null = 没有 SpriteRenderer
        public Vector3 Pos;
        public Vector3 Scale;
        public int SortingOrder;
        public Vector2? ColliderSize;      // null = 没有碰撞盒
        public Vector2 ColliderOffset;
        public bool ColliderIsTrigger;
        public SpringItemKind Kind;

        public string ItemName;            // PickupItem
        public string PickupTitle;         // PickupItem
        public string PickupMessage;       // PickupItem
        public string CleanedSpritePath;   // WashingEgg
        public string IntroImagePath;      // WashingBucket
    }

    private static readonly string ItemDir = "Assets/关卡/1-春/道具/";

    private static readonly SpringItem[] SpringItems = new SpringItem[]
    {
        new SpringItem { Name = "1-桑树2", SpritePath = ItemDir + "1-桑树2.png",
            Pos = new Vector3(-14.77f, -0.48f, 0f), Scale = new Vector3(0.78197f, 0.78197f, 0.78197f),
            SortingOrder = 10, Kind = SpringItemKind.Plain },
        new SpringItem { Name = "1-蚕匾架", SpritePath = ItemDir + "1-蚕匾架.png",
            Pos = new Vector3(20.28f, 2.35f, 0f), Scale = new Vector3(0.31646f, 0.31646f, 0.31646f),
            SortingOrder = 0, Kind = SpringItemKind.Plain },
        new SpringItem { Name = "1-蚕匾架 (1)", SpritePath = ItemDir + "1-蚕匾架.png",
            Pos = new Vector3(21.74f, 2.35f, 0f), Scale = new Vector3(0.31646f, 0.31646f, 0.31646f),
            SortingOrder = 0, Kind = SpringItemKind.Plain },
        new SpringItem { Name = "1-采桑篮1", SpritePath = ItemDir + "1-采桑篮1.png",
            Pos = new Vector3(-10.25f, -0.72f, 0f), Scale = new Vector3(0.21598493f, 0.21598493f, 0.21598493f),
            SortingOrder = 0, ColliderSize = new Vector2(5.26f, 7.48f), ColliderIsTrigger = true,
            Kind = SpringItemKind.Pickup,
            ItemName = "采桑篮", PickupTitle = "采桑篮", PickupMessage = "采桑篮：用来采集桑叶的篮子" },
        new SpringItem { Name = "1-桑树1", SpritePath = ItemDir + "1-桑树1.png",
            Pos = new Vector3(-3.19f, -0.7f, 0f), Scale = new Vector3(0.71127f, 0.71127f, 0.71127f),
            SortingOrder = 10, Kind = SpringItemKind.Plain },
        new SpringItem { Name = "1-蚕簇架2", SpritePath = ItemDir + "1-蚕簇架2.png",
            Pos = new Vector3(-1.77f, 0.15f, 0f), Scale = new Vector3(0.58163f, 0.58163f, 0.58163f),
            SortingOrder = 0, ColliderSize = new Vector2(4.65f, 4.22f), ColliderIsTrigger = true,
            Kind = SpringItemKind.SilkwormRack },
        new SpringItem { Name = "1-蚕种纸1", SpritePath = ItemDir + "1-蚕种纸1.png",
            Pos = new Vector3(21.896f, 2.892f, 0f), Scale = new Vector3(0.14530623f, 0.14530623f, 0.14530623f),
            SortingOrder = 1, ColliderSize = new Vector2(4.02f, 3.8f), ColliderIsTrigger = true,
            Kind = SpringItemKind.Pickup,
            ItemName = "蚕种纸", PickupTitle = "蚕种纸",
            PickupMessage = "蚕种纸：写着蚕种的纸，拿到浴蚕桶清洗后即可孵化。" },
        new SpringItem { Name = "1-蚕簇架1", SpritePath = ItemDir + "1-蚕簇架1.png",
            Pos = new Vector3(-12.94f, -0.05f, 0f), Scale = new Vector3(0.55806f, 0.55806f, 0.55806f),
            SortingOrder = 0, ColliderSize = new Vector2(2.84f, 4.79f), ColliderIsTrigger = true,
            Kind = SpringItemKind.SilkwormRack },
        new SpringItem { Name = "1-蚕种清洗前 (2)", SpritePath = ItemDir + "1-蚕种清洗前.png",
            Pos = new Vector3(21.592f, 2.732f, 0f), Scale = new Vector3(0.0632283f, 0.0632283f, 0.0632283f),
            SortingOrder = 2, ColliderSize = new Vector2(7.36f, 5.77f), ColliderIsTrigger = true,
            Kind = SpringItemKind.WashingEgg, CleanedSpritePath = ItemDir + "1-蚕种清洗后.png" },
        new SpringItem { Name = "1-浴蚕桶", SpritePath = ItemDir + "1-浴蚕桶.png",
            Pos = new Vector3(23.38f, 1.99f, 0f), Scale = new Vector3(0.35771f, 0.35771f, 0.35771f),
            SortingOrder = 0, ColliderSize = new Vector2(5.032009f, 5.032009f), ColliderIsTrigger = true,
            Kind = SpringItemKind.WashingBucket, IntroImagePath = ItemDir + "1-浴蚕桶.png" },
        new SpringItem { Name = "1-蚕种清洗前 (1)", SpritePath = ItemDir + "1-蚕种清洗前.png",
            Pos = new Vector3(20.495f, 2.732f, 0f), Scale = new Vector3(0.0632283f, 0.0632283f, 0.0632283f),
            SortingOrder = 2, ColliderSize = new Vector2(7.36f, 5.77f), ColliderIsTrigger = true,
            Kind = SpringItemKind.WashingEgg, CleanedSpritePath = ItemDir + "1-蚕种清洗后.png" },
        new SpringItem { Name = "采桑游戏触发点位",
            Pos = new Vector3(-7.1198936f, -0.78f, -0.049523696f), Scale = Vector3.one,
            ColliderSize = new Vector2(2.7100024f, 1.8613346f),
            ColliderOffset = new Vector2(0.8550012f, -0.43066728f), ColliderIsTrigger = true,
            Kind = SpringItemKind.Prompt },
        new SpringItem { Name = "1-蚕簇架1 (1)", SpritePath = ItemDir + "1-蚕簇架1.png",
            Pos = new Vector3(12.54f, 0.15f, 0f), Scale = new Vector3(-0.4531355f, 0.4988666f, 0.4988666f),
            SortingOrder = 0, ColliderSize = new Vector2(2.84f, 4.79f), ColliderIsTrigger = true,
            Kind = SpringItemKind.SilkwormRack },
        new SpringItem { Name = "1-蚕种清洗前", SpritePath = ItemDir + "1-蚕种清洗前.png",
            Pos = new Vector3(20.138f, 2.801f, 0f), Scale = new Vector3(0.0632283f, 0.0632283f, 0.0632283f),
            SortingOrder = 2, ColliderSize = new Vector2(18.978844f, 18.978844f), ColliderIsTrigger = true,
            Kind = SpringItemKind.WashingEgg, CleanedSpritePath = ItemDir + "1-蚕种清洗后.png" },
    };

    private static void CreateSpringItems()
    {
        int created = 0;

        foreach (var item in SpringItems)
        {
            if (GameObject.Find(item.Name) != null)
            {
                Debug.Log($"  跳过（已存在）: {item.Name}");
                continue;
            }

            Sprite sprite = null;
            if (!string.IsNullOrEmpty(item.SpritePath))
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(item.SpritePath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[SpringSceneRestore] 找不到图片: {item.SpritePath}");
                    continue;
                }
            }

            var go = new GameObject(item.Name);
            go.transform.position = item.Pos;
            go.transform.localScale = item.Scale;

            if (sprite != null)
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = item.SortingOrder;
            }

            if (item.ColliderSize.HasValue)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = item.ColliderIsTrigger;
                col.size = item.ColliderSize.Value;
                col.offset = item.ColliderOffset;
            }

            switch (item.Kind)
            {
                case SpringItemKind.Pickup:
                    var pickup = go.AddComponent<PickupItem>();
                    pickup.itemName = item.ItemName;
                    pickup.pickupTitle = item.PickupTitle;
                    pickup.pickupMessage = item.PickupMessage;
                    pickup.pickupButtonText = "拾取";
                    break;

                case SpringItemKind.SilkwormRack:
                    break;

                case SpringItemKind.WashingEgg:
                    var egg = go.AddComponent<SilkwormWashingController>();
                    egg.role = SilkwormWashingController.RoleType.Egg;
                    egg.cleanedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(item.CleanedSpritePath);
                    egg.completeDistance = 2.5f;
                    break;

                case SpringItemKind.WashingBucket:
                    var bucket = go.AddComponent<SilkwormWashingController>();
                    bucket.role = SilkwormWashingController.RoleType.Bucket;
                    bucket.introImage = AssetDatabase.LoadAssetAtPath<Sprite>(item.IntroImagePath);
                    break;

                case SpringItemKind.Prompt:
                    go.AddComponent<SpringPrompt>();
                    break;
            }

            created++;
        }

        Debug.Log($"  ✅ 创建了 {created} 个春天道具");
    }

    // ==================== 台阶碰撞盒 ====================
    private struct StepCollider
    {
        public string Name;
        public Vector3 LocalPos;
        public Vector2 Size;
        public Vector2 Offset;

        public StepCollider(string name, Vector3 localPos, Vector2 size, Vector2 offset)
        {
            Name = name;
            LocalPos = localPos;
            Size = size;
            Offset = offset;
        }
    }

    private static readonly StepCollider[] StepColliders = new StepCollider[]
    {
        new StepCollider("1",      new Vector3(-2.27f, -2.02f, 0f),      new Vector2(1f, 0.5695584f),         new Vector2(0f, -0.21522081f)),
        new StepCollider("1 (1)",  new Vector3(-1.62f, -1.81f, 0f),      new Vector2(1.0500517f, 0.8798766f), new Vector2(0.26527166f, -0.25025666f)),
        new StepCollider("1 (2)",  new Vector3(-0.85f, -1.72f, 0f),      new Vector2(1.1401443f, 1.1301335f), new Vector2(0.22022533f, -0.12512821f)),
        new StepCollider("1 (3)",  new Vector3(0.08f, -1.33f, 0f),       new Vector2(0.7397337f, 0.6296201f), new Vector2(-0.11011267f, 0.12512851f)),
        new StepCollider("1 (4)",  new Vector3(0.6f, -0.81f, 0f),        new Vector2(0.8998985f, 0.5995894f), new Vector2(0.2802868f, -0.040041f)),
        new StepCollider("1 (5)",  new Vector3(1.59f, -0.36f, 0f),       new Vector2(1.8108311f, 0.8098051f), new Vector2(0.4154253f, -0.14514884f)),
        new StepCollider("1 (6)",  new Vector3(5.25f, 0.38f, 0f),        new Vector2(4.8270664f, 1.040041f),  new Vector2(1.3929987f, -0.030030847f)),
        new StepCollider("1 (7)",  new Vector3(3.17f, 0.16f, 0f),        new Vector2(1.0800838f, 0.23921949f),new Vector2(-0.28028774f, -0.07007179f)),
        new StepCollider("1 (8)",  new Vector3(3.69f, 0.68f, 0f),        new Vector2(0.67967415f, 0.22480226f),new Vector2(-0.6106272f, -0.22743458f)),
        new StepCollider("1 (9)",  new Vector3(3.69f, 0.68f, 0f),        new Vector2(1.030035f, 0.46945608f), new Vector2(-0.015015602f, -0.105107665f)),
        new StepCollider("1 (10)", new Vector3(-2.27f, -2.02f, 0f),      new Vector2(30.765032f, 1.3592983f), new Vector2(-14.746356f, -0.555626f)),
        new StepCollider("1 (11)", new Vector3(-2.27f, -2.02f, 0f),      new Vector2(4.7587433f, 0.56345904f),new Vector2(-11.695564f, -0.18579465f)),
        new StepCollider("1 (12)", new Vector3(-2.27f, -2.02f, 0f),      new Vector2(4.464651f, 0.31308436f), new Vector2(-11.66575f, 0.054167747f)),
        new StepCollider("1 (13)", new Vector3(-2.27f, -2.02f, 0f),      new Vector2(4.1233397f, 0.4416746f), new Vector2(-11.659697f, 0.11846286f)),
        new StepCollider("1 (14)", new Vector3(-2.27f, -2.02f, 0f),      new Vector2(3.3449764f, 0.16297066f),new Vector2(-11.715592f, 0.5565868f)),
        new StepCollider("1 (15)", new Vector3(-2.27f, -2.02f, 0f),      new Vector2(2.8935623f, 0.14650798f),new Vector2(-11.657719f, 0.7253622f)),
        new StepCollider("1 (17)", new Vector3(-2.27f, -0.94f, 0f),      new Vector2(3.7237167f, 0.11992681f),new Vector2(-11.7019005f, -0.66953546f)),
        new StepCollider("1 (18)", new Vector3(-2.27f, -0.94f, 0f),      new Vector2(1.6426249f, 0.093405604f),new Vector2(-11.661526f, -0.055671632f)),
        new StepCollider("1 (19)", new Vector3(-2.27f, -0.94f, 0f),      new Vector2(2.1842537f, 0.17130071f),new Vector2(-11.695606f, -0.18993232f)),
        new StepCollider("1 (20)", new Vector3(-2.27f, -0.94f, 0f),      new Vector2(1.1187763f, 0.13142693f),new Vector2(-11.733344f, 0.026707888f)),
        new StepCollider("木桩 01", new Vector3(-9.456943f, -1.95286865f, 0f), new Vector2(0.48f, 0.95f), Vector2.zero),
        new StepCollider("木桩 02", new Vector3(-8.596943f, -2.12786865f, 0f), new Vector2(0.49f, 0.60f), Vector2.zero),
        new StepCollider("木桩 03", new Vector3(-7.506943f, -2.09286865f, 0f), new Vector2(0.48f, 0.69f), Vector2.zero),
        new StepCollider("木桩 04", new Vector3(-6.646943f, -1.82786865f, 0f), new Vector2(0.48f, 0.74f), Vector2.zero),
        new StepCollider("木桩 05", new Vector3(-5.516943f, -2.10786865f, 0f), new Vector2(0.49f, 0.66f), Vector2.zero),
        new StepCollider("木桩 06", new Vector3(-4.436943f, -1.95286865f, 0f), new Vector2(0.48f, 0.95f), Vector2.zero),
        new StepCollider("木桩 07", new Vector3(-3.486943f, -2.11286865f, 0f), new Vector2(0.49f, 0.65f), Vector2.zero),
    };

    private static void CreateStepColliders()
    {
        var parent = GameObject.Find("台阶碰撞盒");
        if (parent == null)
        {
            parent = new GameObject("台阶碰撞盒");
            parent.transform.position = new Vector3(16.246943f, 0.23786865f, -0.03742251f);
            parent.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.Log("[SpringSceneRestore] 台阶碰撞盒 已存在，仅补齐缺失的碰撞块");
        }

        int created = 0;
        foreach (var step in StepColliders)
        {
            if (parent.transform.Find(step.Name) != null) continue;

            var child = new GameObject(step.Name);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = step.LocalPos;

            var col = child.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            col.size = step.Size;
            col.offset = step.Offset;
            created++;
        }

        Debug.Log($"  ✅ 台阶碰撞盒补齐 {created} 个");
    }
}
#endif
