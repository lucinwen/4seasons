#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 春天场景道具批量配置工具：
/// 1. 给场景里所有道具添加 BoxCollider2D (Is Trigger) + SpringItemTrigger
/// 2. 给 Player 添加 Rigidbody2D (Kinematic) + BoxCollider2D
/// 菜单: Tools -> Spring Setup -> 批量配置道具触发器
/// </summary>
public static class SpringTriggerSetup
{
    [MenuItem("Tools/Spring Setup/批量配置道具触发器")]
    public static void SetupAll()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[SpringTriggerSetup] 请先打开春天场景 Level1_Spring.unity");
            return;
        }

        int itemCount = 0;
        int playerCount = 0;
        var skipped = new List<string>();

        // 遍历场景所有根物体
        var allRoots = scene.GetRootGameObjects();
        foreach (var root in allRoots)
        {
            // Player 单独处理
            if (root.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase))
            {
                SetupPlayer(root);
                playerCount++;
                continue;
            }

            // 跳过系统物体
            if (root.name.StartsWith("Main Camera") ||
                root.name.StartsWith("EventSystem") ||
                root.name.StartsWith("Canvas") ||
                root.name.StartsWith("Background") ||
                root.name.StartsWith("BG") ||
                root.name.StartsWith("Light") ||
                root.name.StartsWith("Directional Light"))
                continue;

            // 跳过光圈相关
            if (root.name.Contains("光圈")) continue;

            // 有 SpriteRenderer 的非 UI 物体视为道具
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            if (sr.sortingLayerName == "UI" || sr.sortingOrder >= 100) continue;

            SetupItem(root);
            itemCount++;
        }

        // 如果 Player 没在根层级，也找一下子物体
        if (playerCount == 0)
        {
            var player = GameObject.Find("Player");
            if (player != null)
            {
                SetupPlayer(player);
                playerCount++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[SpringTriggerSetup] ✅ 完成！道具: {itemCount} 个, Player: {playerCount} 个");
        if (skipped.Count > 0)
            Debug.LogWarning("[SpringTriggerSetup] ⚠️ 跳过: " + string.Join(", ", skipped));
    }

    [MenuItem("Tools/Spring Setup/仅配置 Player (添加 Rigidbody2D + Collider2D)")]
    public static void SetupPlayerOnly()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[SpringTriggerSetup] 没找到 Player"); return; }
        SetupPlayer(player);
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[SpringTriggerSetup] ✅ Player 配置完成");
    }

    [MenuItem("Tools/Spring Setup/仅配置选中物体")]
    public static void SetupSelected()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            SetupItem(go);
            count++;
        }
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[SpringTriggerSetup] ✅ 配置了 {count} 个选中物体");
    }

    private static void SetupPlayer(GameObject player)
    {
        // 确保有 Rigidbody2D (Kinematic)
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = Undo.AddComponent<Rigidbody2D>(player);
            Debug.Log($"[SpringTriggerSetup] Player 添加 Rigidbody2D");
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 确保有 BoxCollider2D
        var col = player.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = Undo.AddComponent<BoxCollider2D>(player);
            Debug.Log($"[SpringTriggerSetup] Player 添加 BoxCollider2D");
        }

        // 根据 SpriteRenderer 的 bounds 自动适配 Collider 大小
        var sr = player.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            var sprite = sr.sprite;
            var texSize = sprite.textureRect.size;
            var pixelsPerUnit = sprite.pixelsPerUnit;
            // Visual 有 localScale，要乘上
            var visualScale = sr.transform.lossyScale;
            float w = (texSize.x / pixelsPerUnit) * Mathf.Abs(visualScale.x);
            float h = (texSize.y / pixelsPerUnit) * Mathf.Abs(visualScale.y);

            col.size = new Vector2(Mathf.Max(0.5f, w * 0.6f), Mathf.Max(0.5f, h * 0.8f));
            col.offset = new Vector2(0, 0); // 居中
        }
        else
        {
            col.size = new Vector2(0.8f, 1.5f);
        }

        col.isTrigger = false; // Player 的 Collider 不是 trigger，用来触发道具的 trigger
    }

    private static void SetupItem(GameObject item)
    {
        // 确保有 BoxCollider2D (Is Trigger)
        var col = item.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = Undo.AddComponent<BoxCollider2D>(item);
            Debug.Log($"[SpringTriggerSetup] {item.name} 添加 BoxCollider2D");
        }
        col.isTrigger = true;

        // 根据 SpriteRenderer 的 bounds 自动适配 Collider 大小
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            var bounds = sr.bounds;
            var localScale = item.transform.lossyScale;

            col.size = new Vector2(
                Mathf.Max(0.3f, bounds.size.x / Mathf.Abs(localScale.x) * 0.9f),
                Mathf.Max(0.3f, bounds.size.y / Mathf.Abs(localScale.y) * 0.9f));
            col.offset = Vector2.zero;
        }

        // 确保有 SpringItemTrigger
        var trigger = item.GetComponent<SpringItemTrigger>();
        if (trigger == null)
        {
            trigger = Undo.AddComponent<SpringItemTrigger>(item);
            trigger.promptText = SpringItemTrigger.GenerateDefaultPrompt(item.name);
            Debug.Log($"[SpringTriggerSetup] {item.name} 添加 SpringItemTrigger: {trigger.promptText}");
        }
    }
}
#endif
