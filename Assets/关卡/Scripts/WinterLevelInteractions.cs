using UnityEngine;
using UnityEngine.SceneManagement;
using Qingming.LevelBackground;

/// <summary>
/// 冬天关卡交互引导：场景加载后自动为手动摆放的「裁衣台」「缝衣针」「NPC」
/// 挂上交互组件，无需修改场景文件（组件在运行时动态添加）。
/// </summary>
public class WinterLevelInteractions : MonoBehaviour
{
    private const string SceneName = "Level4_Winter";
    private const string ClothTableName = "4-裁剪台";
    private const string NpcName = "idle_blink_frame_01";
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
        AttachClothCuttingTrigger();
        AttachNeedlePickup();
        AttachNpcDialogue();
        ConfigureWinterPlayerStep();
        MakeGroundCollisionSolid();
        FixBlockingPropColliders();
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

}
