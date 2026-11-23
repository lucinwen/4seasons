#if UNITY_EDITOR
using UnityEngine;

namespace JiangnanDye.ClothCutting
{
    /// <summary>
    /// 在 Scene 视图中绘制裁剪路径点，方便调试与对齐。
    /// 运行时也会绘制，但主要用于 Editor 可视化。
    /// </summary>
    public class CuttingPathGizmo : MonoBehaviour
    {
        public ClothCuttingGameManager manager;
        public bool drawPathLines = true;
        public bool drawPoints = true;
        public bool drawPointLabels = false;

        private void OnDrawGizmosSelected()
        {
            if (manager == null) manager = GetComponent<ClothCuttingGameManager>();
            if (manager == null || manager.workbenchRect == null) return;

            // 工作台 RectTransform.rect 左下角 = (0,0)
            // 转换为世界坐标：
            var wbRt = manager.workbenchRect;
            Vector3 wbWorldPos = wbRt.position;
            Vector3 wbScale = wbRt.lossyScale;

            void DrawPath(CuttingPath path, Color c)
            {
                if (path == null || path.points.Count < 2) return;
                Gizmos.color = c;

                // 把 rect local (0,0 在左下) 转成 world position
                Vector2 rectSize = wbRt.rect.size;
                Vector2 pivotOffset = new Vector2(rectSize.x * 0.5f, rectSize.y * 0.5f);

                Vector3 ToWorld(Vector2 local)
                {
                    // local: 相对于 rect 左下角的像素
                    // wbRt.position 是 Canvas 下 anchoredPosition + Canvas 中心偏移
                    // 简单做法：用 RectTransformUtility
                    return wbWorldPos + new Vector3(
                        (local.x - rectSize.x * 0.5f) * wbScale.x,
                        (local.y - rectSize.y * 0.5f) * wbScale.y,
                        0);
                }

                if (drawPathLines)
                {
                    for (int i = 0; i < path.points.Count; i++)
                    {
                        Vector3 a = ToWorld(path.points[i]);
                        Vector3 b = ToWorld(path.points[(i + 1) % path.points.Count]);
                        Gizmos.DrawLine(a, b);
                    }
                }

                if (drawPoints)
                {
                    for (int i = 0; i < path.points.Count; i++)
                    {
                        Vector3 p = ToWorld(path.points[i]);
                        Gizmos.DrawSphere(p, 4f * wbScale.x);
                    }
                }
            }

            DrawPath(manager.frontPath, new Color(1f, 0.6f, 0.2f));
            DrawPath(manager.backPath, new Color(0.2f, 1f, 0.6f));
            DrawPath(manager.sleevePath, new Color(0.6f, 0.2f, 1f));
        }
    }
}
#endif
