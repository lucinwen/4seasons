using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 秋天关卡交互引导：场景加载后自动为手动摆放的道具挂上交互组件，
/// 无需修改场景文件（组件在运行时动态添加）。
/// </summary>
public class AutumnLevelInteractions : MonoBehaviour
{
    private const string SceneName = "Level3_Autumn";

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
        AttachItems(WineNames, AutumnItemClick.ItemKind.Wine);
        AttachItems(RiceNames, AutumnItemClick.ItemKind.Rice);
        AttachItem("3-香蕉果篮", AutumnItemClick.ItemKind.Banana);
        AttachItem("3-工具箱", AutumnItemClick.ItemKind.Toolbox);
        AttachLoom();
        FixBlockingPropColliders();
        AutumnPorchClimb.Setup();
        AttachNpcDialogue();
        CreateRitualPoint();
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
