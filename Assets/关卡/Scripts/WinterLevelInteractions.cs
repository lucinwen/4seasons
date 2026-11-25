using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Qingming.LevelBackground;

/// <summary>
/// 冬天关卡交互引导：场景加载后自动为手动摆放的「染料园触点」「锄头」「裁衣台」「NPC」
/// 挂上交互组件，无需修改场景文件（组件在运行时动态添加）。
/// </summary>
public class WinterLevelInteractions : MonoBehaviour
{
    private const string SceneName = "Level4_Winter";
    private const string DyeGardenPointName = "染料园触点";
    private const string HoeObjectName = "4-挖掘染料锄头";
    private const string ClothTableName = "4-裁剪台";
    private const string NpcName = "idle_blink_frame_01";
    private const string GrindingBowlName = "4-染料空";
    private const string GrindingRodName = "4-研磨棒";
    private const string DyeVatName = "4-染料空缸";
    private const string NeedleName = "4-裁衣工具针";
    private const string CollisionRootName = "碰撞盒";

    // 这些是摆在走道上的装饰道具，图片自带的实体碰撞盒会在场景中形成隐形墙，
    // 导致玩家从一侧能走过、从另一侧被挡住。运行时关闭其实体碰撞（保留 Trigger）。
    private static readonly string[] BlockingPropNames =
    {
        "4-提花机",
        "4-裁剪台",
    };

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
        if (FindObjectOfType<WinterLevelInteractions>() != null) return;
        new GameObject("[WinterLevelInteractions]").AddComponent<WinterLevelInteractions>();
    }

    private void Start()
    {
        WinterLevelState.RestoreWorkshopProgress();
        AttachDyeGardenTrigger();
        AttachHoePickup();
        AttachClothCuttingTrigger();
        AttachNeedlePickup();
        AttachNpcDialogue();
        AttachGrindingBowl();
        AttachGrindingRod();
        AttachDyeVat();
        ConfigureWinterPlayerStep();
        MakeGroundCollisionSolid();
        FixBlockingPropColliders();
        RestoreReturnPosition();
        StartCoroutine(ShowPendingGrindingPromptNextFrame());
    }

    /// <summary>
    /// 从染料园返回冬天场景时，把玩家放回进入小游戏前的位置。
    /// </summary>
    private static void RestoreReturnPosition()
    {
        if (!WinterLevelState.HasReturnPosition) return;
        WinterLevelState.HasReturnPosition = false;

        var player = GameObject.Find("Player");
        if (player == null) return;

        Vector3 pos = WinterLevelState.ReturnPosition;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = new Vector2(pos.x, pos.y);
        else player.transform.position = pos;

        // 掉落重置的出生点也跟着改，避免玩家在染料园入口附近掉下去后被送回关卡起点
        var controller = player.GetComponent<LevelPlayerController>();
        if (controller != null)
            controller.spawnPoint = new Vector3(pos.x, pos.y, controller.spawnPoint.z);
    }

    private static void AttachNpcDialogue()
    {
        var npc = GameObject.Find(NpcName);
        if (npc == null)
        {
            Debug.LogWarning("[WinterLevelInteractions] 未找到 NPC：" + NpcName);
            return;
        }

        if (npc.GetComponent<WinterNpcDialogue>() == null)
            npc.AddComponent<WinterNpcDialogue>();
    }

    private static void AttachGrindingBowl()
    {
        var bowl = GameObject.Find(GrindingBowlName);
        if (bowl == null)
        {
            Debug.LogWarning("[WinterLevelInteractions] 未找到钵体：" + GrindingBowlName);
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
            Debug.LogWarning("[WinterLevelInteractions] 未找到研磨棒：" + GrindingRodName);
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

    /// <summary>
    /// 冬天场景由多块地面碰撞盒拼成，木廊/台阶之间有约 0.75 的高度差。
    /// 放宽该场景的自动迈步高度，玩家往左/往右走时不会被隐形台阶卡住。
    /// </summary>
    private static void ConfigureWinterPlayerStep()
    {
        var player = GameObject.Find("Player");
        if (player == null) return;

        var controller = player.GetComponent<LevelPlayerController>();
        if (controller != null) controller.stepHeight = 0.8f;
    }

    private static void AttachDyeVat()
    {
        var vat = GameObject.Find(DyeVatName);
        if (vat == null)
        {
            Debug.LogWarning("[WinterLevelInteractions] 未找到清水缸：" + DyeVatName);
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

    /// <summary>从寻找染料小游戏带回原料后，衔接研磨对话。</summary>
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

    private static void AttachClothCuttingTrigger()
    {
        var table = GameObject.Find(ClothTableName);
        if (table == null)
        {
            Debug.LogWarning("[WinterLevelInteractions] 未找到裁衣台：" + ClothTableName);
            return;
        }

        if (table.GetComponent<WinterClothCuttingTrigger>() == null)
            table.AddComponent<WinterClothCuttingTrigger>();
    }

    /// <summary>
    /// 「4-裁衣工具针」：玩家点击道具本体后弹出获得提示，不显示任何悬浮提示/名字。
    /// </summary>
    private static void AttachNeedlePickup()
    {
        var needle = GameObject.Find(NeedleName);
        if (needle == null)
        {
            Debug.LogWarning("[WinterLevelInteractions] 未找到裁衣工具针：" + NeedleName);
            return;
        }

        if (WinterLevelState.HasSewingNeedle)
        {
            needle.SetActive(false);
            return;
        }

        var col = needle.GetComponent<Collider2D>();
        if (col == null)
        {
            // 针的图片很小，补一个略大的 Trigger 碰撞盒方便鼠标点中
            var sr = needle.GetComponent<SpriteRenderer>();
            Vector2 worldSize = sr != null ? sr.bounds.size * 1.8f : new Vector2(1f, 1f);
            Vector3 scale = needle.transform.lossyScale;
            var box = needle.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(
                worldSize.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                worldSize.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)));
            box.offset = Vector2.zero;
        }
        else
        {
            col.enabled = true;
        }

        if (needle.GetComponent<WinterNeedleClickTarget>() == null)
            needle.AddComponent<WinterNeedleClickTarget>();
    }

    /// <summary>
    /// 提花机 / 裁剪台等装饰道具的图片自带实体 BoxCollider2D，
    /// 摆在走道上会形成隐形墙。运行时关闭这些实体碰撞，避免挡住玩家。
    /// </summary>
    private static void FixBlockingPropColliders()
    {
        int count = 0;
        foreach (var name in BlockingPropNames)
        {
            var go = GameObject.Find(name);
            if (go == null) continue;

            foreach (var col in go.GetComponents<BoxCollider2D>())
            {
                if (col.isTrigger) continue;
                col.enabled = false;
                count++;
            }
        }

        Debug.Log($"[WinterLevelInteractions] 已关闭 {count} 个装饰道具的实体碰撞，避免隐形墙");
    }

    /// <summary>
    /// 场景里的「碰撞盒」子物体目前全部勾选了 Is Trigger，
    /// Trigger 不参与物理阻挡，玩家会直接穿过地面下落。
    /// 这里在运行时把它们改为实体碰撞（不修改场景文件）。
    /// </summary>
    private static void MakeGroundCollisionSolid()
    {
        var root = GameObject.Find(CollisionRootName);
        if (root == null)
        {
            Debug.LogWarning("[WinterLevelInteractions] 未找到碰撞盒：" + CollisionRootName);
            return;
        }

        int count = 0;
        foreach (var col in root.GetComponentsInChildren<BoxCollider2D>(true))
        {
            if (!col.isTrigger) continue;
            col.isTrigger = false;
            count++;
        }

        Debug.Log($"[WinterLevelInteractions] 已将 {count} 个碰撞盒改为实体碰撞，防止玩家下落");
    }

    private static void AttachDyeGardenTrigger()
    {
        var point = GameObject.Find(DyeGardenPointName);
        if (point == null)
        {
            Debug.LogWarning("[WinterLevelInteractions] 未找到染料园触点：" + DyeGardenPointName);
            return;
        }

        // 触点的实体碰撞盒只作为位置标记，运行时关闭，避免形成隐形墙
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
            Debug.LogWarning("[WinterLevelInteractions] 未找到锄头：" + HoeObjectName);
            return;
        }
        if (hoe.GetComponent<PickupItem>() != null) return;

        // 锄头图片较小，补一个略大的 Trigger 碰撞盒方便鼠标点中
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
        pickup.labelHeight = 3f; // 锄头缩放较小，按局部比例抬高名字
        pickup.pickupTitle = "锄头";
        pickup.pickupMessage = "带去染料园挖取染材。";
        pickup.pickupButtonText = "拾取";
    }
}
