using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 秋天关卡交互引导：场景加载后自动为手动摆放的道具挂上交互组件，
/// 无需修改场景文件（组件在运行时动态添加）。
/// </summary>
public class AutumnLevelInteractions : MonoBehaviour
{
    private const string SceneName = "Level3_Autumn";
    private const string DyeGardenPointName = "染料园触点";
    private const string HoeObjectName = "4-挖掘染料锄头";
    private const string GrindingBowlName = "4-染料空";
    private const string GrindingRodName = "4-研磨棒";
    private const string DyeVatName = "4-染料空缸";

    private static readonly string[] WineNames = { "3-酒", "3-酒 (1)", "3-酒 (2)", "3-酒 (3)" };
    private static readonly string[] RiceNames = { "3-水稻贡品", "3-水稻贡品 (1)", "3-水稻贡品2", "3-水稻贡品2 (1)" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName) return;

        // 从小游戏返回时保留状态；从工坊或存档进入时载入已领取的道具。
        if (AutumnLevelState.ReturningFromLoom) AutumnLevelState.ReturningFromLoom = false;
        else AutumnLevelState.RestoreProgress();

        if (FindObjectOfType<AutumnLevelInteractions>() != null) return;
        new GameObject("[AutumnLevelInteractions]").AddComponent<AutumnLevelInteractions>();
    }

    private void Start()
    {
        WinterLevelState.RestoreWorkshopProgress();
        AttachItems(WineNames, AutumnItemClick.ItemKind.Wine);
        AttachItems(RiceNames, AutumnItemClick.ItemKind.Rice);
        AttachItem("3-香蕉果篮", AutumnItemClick.ItemKind.Banana);
        AttachItem("3-工具箱", AutumnItemClick.ItemKind.Toolbox);
        AttachLoom();
        CreateHammerPickup();
        AttachDyeGardenTrigger();
        AttachHoePickup();
        AttachGrindingBowl();
        AttachGrindingRod();
        AttachDyeVat();
        FixBlockingPropColliders();
        AutumnPorchClimb.Setup();
        AttachNpcDialogue();
        CreateRitualPoint();
        StartCoroutine(ShowPendingGrindingPromptNextFrame());
    }

    internal static GameObject CreateHammerPickup()
    {
        const string name = "3-修机锤";
        var existing = GameObject.Find(name);
        if (existing != null) return existing;
        if (WorkshopProgress.HasStep("autumn.hammer")) return null;
        var loom = GameObject.Find("3-破损的织机") ?? GameObject.Find("3-织机");
        var sprite = AutumnItemIcons.Load()?.hammer;
        if (loom == null || sprite == null) return null;

        var pickup = new GameObject(name);
        SceneManager.MoveGameObjectToScene(pickup, loom.scene);
        float scale = 0.9f / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        pickup.transform.localScale = Vector3.one * scale;
        pickup.transform.position = new Vector3(loom.transform.position.x - 2f,
            -2.6f - sprite.bounds.min.y * scale, loom.transform.position.z);
        var renderer = pickup.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        var loomRenderer = loom.GetComponent<SpriteRenderer>();
        if (loomRenderer != null)
        {
            renderer.sortingLayerID = loomRenderer.sortingLayerID;
            renderer.sortingOrder = loomRenderer.sortingOrder + 1;
        }
        var collider = pickup.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.offset = sprite.bounds.center;
        collider.size = (Vector2)sprite.bounds.size + Vector2.one * (0.18f / scale);
        pickup.AddComponent<AutumnItemClick>().kind = AutumnItemClick.ItemKind.Hammer;
        return pickup;
    }

    private static void AttachNpcDialogue()
    {
        var npc = GameObject.Find("待机关键帧_01");
        if (npc != null && npc.GetComponent<AutumnNpcDialogue>() == null)
            npc.AddComponent<AutumnNpcDialogue>();
    }

    private static void CreateRitualPoint()
    {
        // 优先使用场景里的“祭祀触发点”，同步它的位置；它的碰撞盒是实体，会挡住玩家，运行时关闭
        var scenePoint = GameObject.Find("祭祀触发点");
        if (scenePoint != null)
        {
            foreach (var col in scenePoint.GetComponents<Collider2D>())
            {
                if (!col.isTrigger) col.enabled = false;
            }

            if (scenePoint.GetComponent<AutumnRitualTrigger>() == null)
                scenePoint.AddComponent<AutumnRitualTrigger>();
            return;
        }

        if (GameObject.Find("AutumnRitualPoint") != null) return;

        var go = new GameObject("AutumnRitualPoint");
        go.transform.position = new Vector3(-5.2f, -2f, 0f);
        go.AddComponent<AutumnRitualTrigger>();
    }

    /// <summary>
    /// 这些道具在场景里挂了整张图大小的“实体”碰撞盒，
    /// 会横在玩家头顶高度形成看不见的墙（3-树 还会把出生点的玩家挤进地面）。
    /// 它们都是装饰/交互道具，实体碰撞盒在运行时关闭；Trigger 碰撞盒保留。
    /// 不修改场景文件。
    /// </summary>
    private static readonly string[] BlockingPropNames =
    {
        "3-树",
        "3-酒",
        "3-酒 (1)",
        "3-酒 (2)",
        "3-酒 (3)",
        "3-丝束",
        "3-丝束 (1)",
        "3-苹果贡品",
        "3-橘子贡品",
        "3-水稻贡品",
        "3-水稻贡品 (1)",
        "3-水稻贡品2",
        "3-水稻贡品2 (1)",
        "3-织好的白布+架子",
        "3-织好的白布+架子 (1)",
        "3-织好的白布+架子 (2)",
        "3-破损的织机",
    };

    private static void FixBlockingPropColliders()
    {
        foreach (var name in BlockingPropNames)
        {
            var go = GameObject.Find(name);
            if (go == null) continue;

            foreach (var col in go.GetComponents<BoxCollider2D>())
            {
                if (col.isTrigger) continue;
                col.enabled = false;
            }
        }
    }

    private static void AttachDyeGardenTrigger()
    {
        var point = GameObject.Find(DyeGardenPointName);
        if (point == null)
        {
            Debug.LogWarning("[AutumnLevelInteractions] 未找到染料园触点：" + DyeGardenPointName);
            return;
        }

        foreach (var col in point.GetComponents<Collider2D>())
        {
            if (!col.isTrigger) col.enabled = false;
        }

        if (point.GetComponent<WinterDyeGardenTrigger>() == null)
            point.AddComponent<WinterDyeGardenTrigger>();
    }

    private static void AttachHoePickup()
    {
        var hoe = GameObject.Find(HoeObjectName);
        if (WinterLevelState.HasHoe)
        {
            if (hoe != null) hoe.SetActive(false);
            return;
        }
        if (hoe == null)
        {
            Debug.LogWarning("[AutumnLevelInteractions] 未找到锄头：" + HoeObjectName);
            return;
        }
        if (hoe.GetComponent<PickupItem>() != null) return;

        var sr = hoe.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            var col = hoe.GetComponent<BoxCollider2D>();
            if (col == null) col = hoe.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = sr.sprite.bounds.size * 1.6f;
            col.offset = Vector2.zero;
        }

        var pickup = hoe.AddComponent<PickupItem>();
        pickup.itemName = "锄头";
        pickup.triggerDistance = 2.5f;
        pickup.labelHeight = 3f;
        pickup.pickupTitle = "锄头";
        pickup.pickupMessage = "带去染料园挖取染材。";
        pickup.pickupButtonText = "拾取";
    }

    private static void AttachGrindingBowl()
    {
        var bowl = GameObject.Find(GrindingBowlName);
        if (bowl == null)
        {
            Debug.LogWarning("[AutumnLevelInteractions] 未找到钵体：" + GrindingBowlName);
            return;
        }

        var col = bowl.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        if (bowl.GetComponent<WinterGrindingClickTarget>() == null)
            bowl.AddComponent<WinterGrindingClickTarget>();
    }

    private static void AttachGrindingRod()
    {
        var rod = GameObject.Find(GrindingRodName);
        if (rod == null)
        {
            Debug.LogWarning("[AutumnLevelInteractions] 未找到研磨棒：" + GrindingRodName);
            return;
        }

        if (WinterLevelState.HasGrindingRod)
        {
            rod.SetActive(false);
            return;
        }

        var col = rod.GetComponent<Collider2D>();
        if (col == null)
        {
            var box = rod.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(20f, 20f);
            box.offset = Vector2.zero;
        }
        else
        {
            col.enabled = true;
        }

        if (rod.GetComponent<WinterRodClickTarget>() == null)
            rod.AddComponent<WinterRodClickTarget>();
    }

    private static void AttachDyeVat()
    {
        var vat = GameObject.Find(DyeVatName);
        if (vat == null)
        {
            Debug.LogWarning("[AutumnLevelInteractions] 未找到清水缸：" + DyeVatName);
            return;
        }

        var col = vat.GetComponent<Collider2D>();
        if (col == null)
        {
            var box = vat.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(8f, 8f);
            box.offset = new Vector2(0f, 0.5f);
        }
        else
        {
            col.enabled = true;
        }

        if (vat.GetComponent<WinterDyeVatClickTarget>() == null)
            vat.AddComponent<WinterDyeVatClickTarget>();
    }

    private static IEnumerator ShowPendingGrindingPromptNextFrame()
    {
        yield return null;
        ShowPendingGrindingPrompt();
    }

    internal static void ShowPendingGrindingPrompt()
    {
        if (!WinterLevelState.PendingGrindingPrompt) return;
        WinterLevelState.PendingGrindingPrompt = false;
        if (WinterLevelState.CollectedDyes.Count > 0)
            WorkshopDialoguePresenter.PlayOnce("dialogue.winter.materialsComplete",
                WorkshopDialogueScripts.WinterDyeMaterialsComplete);
    }

    private static void AttachItems(string[] names, AutumnItemClick.ItemKind kind)
    {
        foreach (var n in names) AttachItem(n, kind);
    }

    private static void AttachItem(string name, AutumnItemClick.ItemKind kind)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogWarning($"[AutumnLevelInteractions] 未找到道具：{name}");
            return;
        }
        if (go.GetComponent<AutumnItemClick>() != null) return;

        var click = go.AddComponent<AutumnItemClick>();
        click.kind = kind;
    }

    private static void AttachLoom()
    {
        var loom = GameObject.Find("3-破损的织机");
        if (loom == null) loom = GameObject.Find("3-织机");
        if (loom == null)
        {
            Debug.LogWarning("[AutumnLevelInteractions] 未找到织机");
            return;
        }
        if (loom.GetComponent<AutumnLoomTrigger>() != null) return;

        loom.AddComponent<AutumnLoomTrigger>();
    }
}
