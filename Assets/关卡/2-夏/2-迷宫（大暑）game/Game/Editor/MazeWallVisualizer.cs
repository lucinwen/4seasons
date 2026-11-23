#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JiangnanDye.MazeGame.EditorTools
{
    /// <summary>
    /// 在 Scene 视图里把 MazeWalls 碰撞体可视化：
    ///   边界 → 红色实心 + 白色粗边框
    ///   树篱 → 黄色实心 + 白色粗边框
    ///   每个碰撞体中心标注 world 坐标 (x, y)
    ///
    /// 菜单：Tools/江南迷宫/显示碰撞体可视化
    /// 勾选后 Scene 视图即时渲染。
    /// </summary>
    [InitializeOnLoad]
    public static class MazeWallVisualizer
    {
        private static bool _enabled;

        [MenuItem("Tools/江南迷宫/显示碰撞体可视化")]
        public static void Toggle()
        {
            _enabled = !_enabled;
            SceneView.duringSceneGui -= OnSceneGUI;
            if (_enabled)
            {
                SceneView.duringSceneGui += OnSceneGUI;
                Debug.Log("[MazeWallVis] 已开启 — 在 Scene 视图查看所有碰撞体的彩色覆盖");
            }
            else
            {
                Debug.Log("[MazeWallVis] 已关闭");
            }
        }

        private static void OnSceneGUI(SceneView sv)
        {
            var walls = GameObject.Find("MazeWalls");
            if (walls == null) return;

            // 先画一层浅青色背景网格辅助对齐
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
            Handles.BeginGUI(); // placeholder

            int idx = 0;
            foreach (Transform child in walls.transform)
            {
                var col = child.GetComponent<BoxCollider2D>();
                if (col == null) continue;

                Vector2 p = child.position;
                Vector2 s = col.size;
                float x = p.x - s.x * 0.5f;
                float y = p.y - s.y * 0.5f;

                bool isBoundary = child.name.StartsWith("Wall_");
                // 实心填充（EditorGUI 里用 Handles.DrawSolidRectangleWithOutline）
                Color fill = isBoundary
                    ? new Color(1f, 0.2f, 0.1f, 0.45f)   // 边界=红
                    : new Color(1f, 0.85f, 0.1f, 0.5f);  // 树篱=黄
                Color border = new Color(1f, 1f, 1f, 0.9f);
                Handles.DrawSolidRectangleWithOutline(
                    new Rect(x, y, s.x, s.y), fill, border);

                // 中心世界坐标标注
                Handles.Label(p, $"{idx}\n({p.x:F1},{p.y:F1})\n{s.x:F1}x{s.y:F1}");
                idx++;
            }

            // 背景边界辅助线（地图四角）
            Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
            Vector2 bl = new Vector2(-8.36f, -4.705f);
            Vector2 tr = new Vector2(8.36f, 4.705f);
            Handles.DrawSolidRectangleWithOutline(
                new Rect(bl.x, bl.y, tr.x - bl.x, tr.y - bl.y),
                new Color(0.3f, 0.8f, 1f, 0f),
                new Color(0.3f, 0.8f, 1f, 0.8f));
            Handles.Label(new Vector2(tr.x, tr.y), "  地图范围 (×,y)");
        }
    }
}
#endif
