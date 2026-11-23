using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye.ClothCutting
{
    /// <summary>
    /// 在工作台上层实时绘制四条裁剪路径的轨迹。
    /// 直接继承 Graphic，用 OnPopulateMesh 绘制折线。
    /// </summary>
    public class CuttingPathRenderer : Graphic
    {
        public ClothCuttingGameManager manager;

        [Header("样式")]
        public Color completedColor = new Color(1f, 0.85f, 0.3f, 1f);
        public Color pendingColor = new Color(1f, 1f, 1f, 0.35f);
        public float lineWidth = 6f;

        protected override void Awake()
        {
            // Graphic 子类需要 CanvasRenderer 才能渲染
            if (GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();
            // 禁用裁剪线显示（用户不需要黄色线）
            gameObject.SetActive(false);
            base.Awake();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (manager == null) return;

            // 四条路径
            CuttingPath[] paths = new CuttingPath[]
            {
                manager.frontPath,
                manager.backPath,
                manager.sleevePath,
            };
            var activeId = manager.activePiece;

            for (int i = 0; i < paths.Length; i++)
            {
                CuttingPath path = paths[i];
                if (path == null || path.points.Count < 2) continue;
                if (path.segmentLengths == null || path.segmentLengths.Count != path.points.Count)
                    path.Initialize();

                DrawProgressSegment(vh, path, path.progress, completedColor, path.completed);
                // 不再绘制 pending 虚线（工作台底图已经自带虚线轮廓）
            }
        }

        private bool IsActivePath(CuttingPath p, ClothCuttingGameManager.PieceId? activeId)
        {
            if (activeId == null || manager == null) return false;
            if (p == manager.frontPath && activeId == ClothCuttingGameManager.PieceId.Front) return true;
            if (p == manager.backPath && activeId == ClothCuttingGameManager.PieceId.Back) return true;
            if (p == manager.sleevePath && activeId == ClothCuttingGameManager.PieceId.Sleeve) return true;
            return false;
        }

        private void LateUpdate()
        {
            SetVerticesDirty();
        }

        private void DrawProgressSegment(VertexHelper vh, CuttingPath path, float progress, Color color, bool fullyCompleted)
        {
            if (progress <= 0.0001f) return;

            int pts = path.points.Count;
            float covered = progress * path.totalLength;

            // 找到当前覆盖到的最后一个整段
            int lastSeg = 0;
            float segCovered = 0f;
            for (int i = 0; i < path.segmentLengths.Count; i++)
            {
                if (covered <= segCovered + path.segmentLengths[i])
                {
                    lastSeg = i;
                    break;
                }
                segCovered += path.segmentLengths[i];
                lastSeg = i;
            }

            for (int i = 0; i <= lastSeg; i++)
            {
                Vector2 a = WbToCanvas(path.points[i]);
                Vector2 b = WbToCanvas(path.points[(i + 1) % pts]);

                if (i == lastSeg && !fullyCompleted)
                {
                    float segLen = path.segmentLengths[i];
                    float remain = covered - segCovered;
                    float t = segLen > 0.001f ? Mathf.Clamp01(remain / segLen) : 1f;
                    b = Vector2.Lerp(a, b, t);
                }

                AddLine(vh, a, b, color, lineWidth);
            }
        }

        private void DrawPendingSegment(VertexHelper vh, CuttingPath path, float progress, Color color)
        {
            if (progress >= 0.999f) return;

            int pts = path.points.Count;
            float covered = progress * path.totalLength;

            int startSeg = 0;
            float segAcc = 0f;
            for (int i = 0; i < path.segmentLengths.Count; i++)
            {
                if (covered <= segAcc + path.segmentLengths[i])
                {
                    startSeg = i;
                    break;
                }
                segAcc += path.segmentLengths[i];
                startSeg = i;
            }

            for (int i = startSeg; i < pts; i++)
            {
                Vector2 a = WbToCanvas(path.points[i]);
                Vector2 b = WbToCanvas(path.points[(i + 1) % pts]);

                if (i == startSeg)
                {
                    float segLen = path.segmentLengths[i];
                    float remain = covered - segAcc;
                    float t = segLen > 0.001f ? Mathf.Clamp01(remain / segLen) : 0f;
                    a = Vector2.Lerp(a, b, t);
                }

                AddDashedLine(vh, a, b, color, lineWidth * 0.85f, 14f, 10f);
            }
        }

        /// <summary>工作台 rect 局部坐标 -> Canvas 下的局部坐标。</summary>
        private Vector2 WbToCanvas(Vector2 rectPt)
        {
            if (manager == null || manager.workbenchRect == null) return rectPt;
            var wbRt = manager.workbenchRect;
            Vector2 size = wbRt.rect.size;
            Vector2 anchor = wbRt.anchoredPosition;
            return rectPt - new Vector2(size.x * 0.5f, size.y * 0.5f) + anchor;
        }

        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 dir = b - a;
            if (dir.sqrMagnitude < 0.0001f) return;
            Vector2 n = new Vector2(-dir.y, dir.x).normalized * width * 0.5f;

            Vector2 p1 = a + n;
            Vector2 p2 = a - n;
            Vector2 p3 = b - n;
            Vector2 p4 = b + n;

            int idx = vh.currentVertCount;
            vh.AddVert(p1, color, new Vector2(0, 1));
            vh.AddVert(p2, color, new Vector2(0, 0));
            vh.AddVert(p3, color, new Vector2(1, 0));
            vh.AddVert(p4, color, new Vector2(1, 1));

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        private static void AddDashedLine(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width, float dashLen, float gapLen)
        {
            Vector2 dir = b - a;
            float total = dir.magnitude;
            if (total < 0.0001f) return;
            Vector2 unit = dir / total;

            float acc = 0f;
            bool drawing = true;
            while (acc < total)
            {
                float segLen = drawing ? dashLen : gapLen;
                float next = Mathf.Min(acc + segLen, total);

                if (drawing)
                {
                    Vector2 pA = a + unit * acc;
                    Vector2 pB = a + unit * next;
                    AddLine(vh, pA, pB, color, width);
                }

                acc = next;
                drawing = !drawing;
            }
        }
    }
}
