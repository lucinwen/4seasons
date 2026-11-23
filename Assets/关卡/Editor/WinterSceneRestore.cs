#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 冬天场景 Level4_Winter.unity 一键恢复工具。
/// 所有位置 / 缩放 / 旋转 / sortingOrder / 碰撞盒尺寸与 Trigger 状态
/// 均从当前手动搭建的冬天场景同步而来（2026-11-19），
/// 仅在场景损坏需要重建时使用，不会自动修改现有场景。
/// 菜单: Tools/Winter Setup/恢复冬天场景 (Level4_Winter)
/// </summary>
public static class WinterSceneRestore
{
    private const string BgPath = "Assets/关卡/4-冬/第四关-冬.jpg";
    private const string PlayerAnimCtrl = "Assets/animation/player.controller";
    private const string PlayerIdle = "Assets/animation/a-idle/待机_00001.png";
    private const string NpcSprite = "Assets/关卡/4-冬/4-NPC动画关键帧/idle_blink_frame_01.png";
    private const string NpcAnimCtrl = "Assets/关卡/4-冬/4-NPC动画关键帧/idle_blink_frame_01.controller";
    private const string ItemDir = "Assets/关卡/4-冬/道具/";

    [MenuItem("Tools/Winter Setup/恢复冬天场景 (Level4_Winter)")]
    public static void Restore()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogError("[WinterSceneRestore] 没有打开的场景"); return; }

        Debug.Log("========== Winter Scene Restore Start ==========");

        CreatePlayer();
        var cam = CreateMainCamera();
        CreateBackground();
        CreateCanvas(cam);
        CreateEventSystem();
        CreateNpc();
        CreateWinterItems();
        CreateCollisionBoxes();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("========== Winter Scene Restore Done ==========");
    }

    [MenuItem("Tools/Winter Setup/仅恢复核心物体 (Player+Camera+BG)")]
    public static void RestoreCoreOnly()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        CreatePlayer();
        var cam = CreateMainCamera();
        CreateBackground();
        CreateCanvas(cam);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[WinterSceneRestore] ✅ 核心物体恢复完成");
    }

    // ==================== Player ====================
    private static GameObject CreatePlayer()
    {
        var existing = GameObject.Find("Player");
        if (existing != null)
        {
            Debug.Log("[WinterSceneRestore] Player 已存在，跳过");
            return existing;
        }

        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, -3.8f, 0f);
        player.transform.localScale = Vector3.one;

        // Visual 子物体（位置 / 缩放 / 排序同步自场景）
        var visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = new Vector3(-0.03f, 0.24f, 0f);
        visual.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);

        var visualSr = visual.AddComponent<SpriteRenderer>();
        visualSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerIdle);
        visualSr.sortingOrder = 5;

        // Visual 上的实体碰撞盒（尺寸 / 偏移同步自场景）
        var visualCol = visual.AddComponent<BoxCollider2D>();
        visualCol.isTrigger = false;
        visualCol.offset = new Vector2(0f, 8.34f);
        visualCol.size = new Vector2(13.03125f, 17.375f);

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
            Debug.LogWarning("[WinterSceneRestore] 找不到 player.controller，Animator 没配");
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
            Debug.Log("[WinterSceneRestore] Main Camera 已存在，跳过");
            return existing.GetComponent<Camera>();
        }

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.85f, 0.9f, 0.95f, 1f);

        camGo.AddComponent<AudioListener>();

        var follow = camGo.AddComponent<Qingming.LevelBackground.CameraFollow>();
        follow.target = null;
        follow.offset = new Vector2(0f, 1.5f);
        follow.smoothSpeedX = 8f;
        follow.smoothSpeedY = 5f;
        follow.clampX = true;
        follow.minX = -20f;
        follow.maxX = 20f;

        Debug.Log("  ✅ Main Camera 创建完成");
        return cam;
    }

    // ==================== Background ====================
    private static void CreateBackground()
    {
        if (GameObject.Find("Background") != null)
        {
            Debug.Log("[WinterSceneRestore] Background 已存在，跳过");
            return;
        }

        var bgGo = new GameObject("Background");
        bgGo.transform.position = new Vector3(0f, 0f, 0f);
        bgGo.transform.localScale = new Vector3(0.89473814f, 0.89473814f, 0.50503f);

        var bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
        bgSr.sortingOrder = -10;

        Debug.Log("  ✅ Background 创建完成");
    }

    // ==================== Canvas + LevelLabel ====================
    private static void CreateCanvas(Camera cam)
    {
        if (GameObject.Find("Canvas") != null)
        {
            Debug.Log("[WinterSceneRestore] Canvas 已存在，跳过");
            return;
        }

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

    }

    // ==================== EventSystem ====================
    private static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            Debug.Log("[WinterSceneRestore] EventSystem 已存在，跳过");
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Debug.Log("  ✅ EventSystem 创建完成");
    }

    // ==================== NPC ====================
    private static void CreateNpc()
    {
        const string npcName = "idle_blink_frame_01";
        if (GameObject.Find(npcName) != null)
        {
            Debug.Log("[WinterSceneRestore] NPC 已存在，跳过");
            return;
        }

        var npc = new GameObject(npcName);
        npc.transform.position = new Vector3(-15.08f, -1.46f, 0f);
        npc.transform.localScale = new Vector3(0.46378f, 0.46378f, 0.46378f);

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
            Debug.LogWarning($"[WinterSceneRestore] 找不到 NPC 动画控制器: {NpcAnimCtrl}");
        }

        Debug.Log("  ✅ NPC 创建完成");
    }

    // ==================== 冬天道具 ====================
    // 每个道具的位置 / 缩放 / 旋转 / sortingOrder / 碰撞盒尺寸与 Trigger 状态
    // 均从手动搭建的冬天场景同步而来
    private enum WinterColliderKind
    {
        None,
        Trigger,
        Solid
    }

    private struct WinterItem
    {
        public string Name;
        public string SpritePath;          // null = 没有 SpriteRenderer
        public Vector3 Pos;
        public Vector3 Scale;
        public float EulerZ;
        public int SortingOrder;
        public WinterColliderKind ColliderKind;
        public Vector2 ColliderSize;
        public Vector2 ColliderOffset;

        public WinterItem(string name, string spritePath, Vector3 pos, Vector3 scale,
                          float eulerZ, int sortingOrder, WinterColliderKind colliderKind,
                          Vector2 colliderSize, Vector2 colliderOffset)
        {
            Name = name;
            SpritePath = spritePath;
            Pos = pos;
            Scale = scale;
            EulerZ = eulerZ;
            SortingOrder = sortingOrder;
            ColliderKind = colliderKind;
            ColliderSize = colliderSize;
            ColliderOffset = colliderOffset;
        }
    }

    private static readonly WinterItem[] WinterItems = new WinterItem[]
    {
        // (名字, 图片路径, 位置, 缩放, Z旋转, sortingOrder, 碰撞盒类型, 碰撞盒尺寸, 碰撞盒偏移)
        new WinterItem("4-晾晒架子", ItemDir + "4-晾晒架子.png",
            new Vector3(-6.68f, -3.19f, 0f), new Vector3(0.16351217f, 0.16351217f, 0.16351217f),
            0f, 10, WinterColliderKind.None, Vector2.zero, Vector2.zero),
        new WinterItem("4-染料蓝布料", ItemDir + "4-染料蓝布料.png",
            new Vector3(-2.898f, -1.622f, 0f), new Vector3(0.13379f, 0.13379f, 0.13379f),
            0f, 1, WinterColliderKind.None, Vector2.zero, Vector2.zero),
        new WinterItem("4-裁衣工具针", ItemDir + "4-裁衣工具针.png",
            new Vector3(17.281f, -2.254f, 0f), new Vector3(0.113982692f, 0.113982692f, 0.113982692f),
            0f, 0, WinterColliderKind.None, Vector2.zero, Vector2.zero),
        new WinterItem("4-裁衣工具尺", ItemDir + "4-裁衣工具尺.png",
            new Vector3(17.32f, -2.85f, 0f), new Vector3(0.13379f, 0.13379f, 0.13379f),
            0f, 0, WinterColliderKind.None, Vector2.zero, Vector2.zero),
        new WinterItem("4-裁剪台", ItemDir + "4-裁剪台.png",
            new Vector3(17.26f, -2.882f, 0f), new Vector3(0.20514001f, 0.2104f, 0.2104f),
            0f, 0, WinterColliderKind.Solid, new Vector2(15.36f, 10.24f), Vector2.zero),
        new WinterItem("4-石头3", ItemDir + "4-石头3.png",
            new Vector3(6.77f, -2.7f, 0f), new Vector3(0.27244157f, 0.27244157f, 0.27244157f),
            0f, 10, WinterColliderKind.None, Vector2.zero, Vector2.zero),
        new WinterItem("4-石头3 (1)", ItemDir + "4-石头3.png",
            new Vector3(-7.98f, 0.14f, 0f), new Vector3(0.27244157f, 0.27244157f, 0.27244157f),
            0f, 10, WinterColliderKind.None, Vector2.zero, Vector2.zero),
        new WinterItem("4-染料空", ItemDir + "4-染料钵体.png",
            new Vector3(14.28f, -1.5f, 0f), new Vector3(0.15102404f, 0.15102404f, 0.15102404f),
            0f, 0, WinterColliderKind.Trigger, new Vector2(4.51f, 3.52f), Vector2.zero),
        new WinterItem("4-提花机", ItemDir + "4-提花机.png",
            new Vector3(-1.7f, -1.24f, 0f), new Vector3(0.22166072f, 0.22166072f, 0.22166072f),
            0f, 0, WinterColliderKind.Trigger, new Vector2(17.74f, 8.87f), Vector2.zero),
        new WinterItem("4-研磨棒", ItemDir + "4-研磨棒.png",
            new Vector3(1.14f, -1.75f, 0f), new Vector3(0.06846758f, 0.06846758f, 0.06846758f),
            0f, 0, WinterColliderKind.None, Vector2.zero, Vector2.zero),
        new WinterItem("4-染料空缸", ItemDir + "4-染料空缸.png",
            new Vector3(-5.34f, -1.75f, 0f), new Vector3(0.20442495f, 0.20442495f, 0.20442495f),
            0f, 0, WinterColliderKind.None, Vector2.zero, Vector2.zero),
        new WinterItem("4-挖掘染料锄头", ItemDir + "4-挖掘染料锄头.png",
            new Vector3(13.056f, -1.67f, 0f), new Vector3(0.18664856f, 0.18664856f, 0.18664856f),
            -26.587f, 0, WinterColliderKind.Trigger, new Vector2(4.59f, 2.28f), Vector2.zero),
        new WinterItem("4-挖掘染料背篓", ItemDir + "4-挖掘染料背篓.png",
            new Vector3(-12.59f, -0.52f, 0f), new Vector3(0.110615909f, 0.110615909f, 0.110615909f),
            0f, 0, WinterColliderKind.None, Vector2.zero, Vector2.zero),
        new WinterItem("染料园触点", null,
            new Vector3(-11.47f, -1.62f, -5.027085f), Vector3.one,
            0f, 0, WinterColliderKind.Solid, new Vector2(2.5958767f, 0.84556043f),
            new Vector2(0.79793835f, -0.35177916f)),
    };

    private static void CreateWinterItems()
    {
        int created = 0;

        foreach (var item in WinterItems)
        {
            if (GameObject.Find(item.Name) != null)
            {
                Debug.Log($"  跳过（已存在）: {item.Name}");
                continue;
            }

            var go = new GameObject(item.Name);
            go.transform.position = item.Pos;
            go.transform.localScale = item.Scale;
            go.transform.localEulerAngles = new Vector3(0f, 0f, item.EulerZ);

            if (!string.IsNullOrEmpty(item.SpritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(item.SpritePath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[WinterSceneRestore] 找不到图片: {item.SpritePath}");
                    continue;
                }

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = item.SortingOrder;
            }

            if (item.ColliderKind != WinterColliderKind.None)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = item.ColliderKind == WinterColliderKind.Trigger;
                col.size = item.ColliderSize;
                col.offset = item.ColliderOffset;
            }

            created++;
        }

        Debug.Log($"  ✅ 创建了 {created} 个冬天道具");
    }

    // ==================== 碰撞盒 ====================
    private struct WinterSceneCollider
    {
        public string Name;
        public Vector2 Size;
        public Vector2 Offset;

        public WinterSceneCollider(string name, Vector2 size, Vector2 offset)
        {
            Name = name;
            Size = size;
            Offset = offset;
        }
    }

    // 冬天场景中的「碰撞盒」子物体全部为 Trigger
    private static readonly WinterSceneCollider[] CollisionBoxes = new WinterSceneCollider[]
    {
        new WinterSceneCollider("1",     new Vector2(16.384624f, 1.4206238f), new Vector2(4.964632f, -3.1674232f)),
        new WinterSceneCollider("1 (1)", new Vector2(6.4379444f, 1.0440378f), new Vector2(19.841747f, -3.3557162f)),
        new WinterSceneCollider("1 (2)", new Vector2(0.20676422f, 5.1014504f), new Vector2(22.957336f, -1.3270099f)),
        new WinterSceneCollider("1 (3)", new Vector2(10.812014f, 2.1733103f), new Vector2(-8.466838f, -2.79108f)),
        new WinterSceneCollider("1 (4)", new Vector2(2.6695251f, 0.7537012f), new Vector2(14.162167f, -3.5008845f)),
        new WinterSceneCollider("1 (5)", new Vector2(0.34608078f, 0.8460908f), new Vector2(15.673508f, -3.4546897f)),
        new WinterSceneCollider("1 (6)", new Vector2(0.32219696f, 1.0113244f), new Vector2(16.027775f, -3.372073f)),
        new WinterSceneCollider("1 (7)", new Vector2(0.4400406f, 1.0786877f), new Vector2(16.39746f, -3.3383913f)),
    };

    private static void CreateCollisionBoxes()
    {
        var parent = GameObject.Find("碰撞盒");
        if (parent == null)
        {
            parent = new GameObject("碰撞盒");
            parent.transform.position = new Vector3(-4.5948462f, -0.6327649f, -5.006914f);
            parent.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.Log("[WinterSceneRestore] 碰撞盒 已存在，仅补齐缺失的碰撞块");
        }

        int created = 0;
        foreach (var box in CollisionBoxes)
        {
            if (parent.transform.Find(box.Name) != null) continue;

            var child = new GameObject(box.Name);
            child.transform.SetParent(parent.transform, false);

            var col = child.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = box.Size;
            col.offset = box.Offset;
            created++;
        }

        Debug.Log($"  ✅ 碰撞盒补齐 {created} 个");
    }
}
#endif
