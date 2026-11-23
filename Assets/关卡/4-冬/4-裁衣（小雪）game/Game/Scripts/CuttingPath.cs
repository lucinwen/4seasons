using System.Collections.Generic;
using UnityEngine;

namespace JiangnanDye.ClothCutting
{
    /// <summary>
    /// 单片衣片的裁剪路径。由一组有序的 Vector2 局部坐标点构成闭合路径。
    /// 路径点坐标系：相对于工作台 RectTransform，(0,0) 为左下角，单位为像素。
    /// </summary>
    [System.Serializable]
    public class CuttingPath
    {
        /// <summary>衣片标识名（前/后/左袖/右袖）。</summary>
        public string pieceId;

        /// <summary>路径点列表（相对于工作台左下角的像素坐标）。</summary>
        public List<Vector2> points = new List<Vector2>();

        /// <summary>允许的偏离容错距离（像素）。</summary>
        public float tolerance = 28f;

        /// <summary>累计路径长度（像素）。</summary>
        [System.NonSerialized] public float totalLength;

        /// <summary>每段的长度。</summary>
        [System.NonSerialized] public List<float> segmentLengths = new List<float>();

        /// <summary>当前已完成进度 0..1。</summary>
        [System.NonSerialized] public float progress;

        /// <summary>已推进到的最后一个点索引（已完成段数）。</summary>
        [System.NonSerialized] public int lastReachedIndex;

        /// <summary>是否已完成整圈裁剪。</summary>
        [System.NonSerialized] public bool completed;

        public void Initialize()
        {
            if (points.Count < 2)
            {
                segmentLengths = new List<float>();
                totalLength = 0f;
                progress = 0f;
                lastReachedIndex = 0;
                completed = false;
                return;
            }

            // 先在临时变量里算好，最后一次性替换 segmentLengths，避免 Clear() 后中间态被访问越界
            var lens = new List<float>(points.Count);
            float total = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                float seg = Vector2.Distance(points[i], points[next]);
                lens.Add(seg);
                total += seg;
            }

            segmentLengths = lens;
            totalLength = total;
            progress = 0f;
            lastReachedIndex = 0;
            completed = false;
        }

        public void Reset()
        {
            Initialize();
        }

        /// <summary>当前剪刀尖端位置在路径上的最近点坐标（供可视化裁剪效果用）。</summary>
        public Vector2 GetPointAtProgress(float p)
        {
            if (points.Count < 2) return Vector2.zero;
            p = Mathf.Clamp01(p) * totalLength;
            float acc = 0f;
            for (int i = 0; i < segmentLengths.Count; i++)
            {
                if (acc + segmentLengths[i] >= p || i == segmentLengths.Count - 1)
                {
                    float t = segmentLengths[i] > 0.0001f ? (p - acc) / segmentLengths[i] : 0f;
                    int next = (i + 1) % points.Count;
                    return Vector2.Lerp(points[i], points[next], t);
                }
                acc += segmentLengths[i];
            }
            return points[0];
        }

        /// <summary>检查某点是否落在起点附近。</summary>
        public bool IsNearStart(Vector2 point)
        {
            if (points.Count == 0) return false;
            return Vector2.Distance(point, points[0]) <= tolerance * 1.5f;
        }

        /// <summary>
        /// 尝试沿路径推进裁剪。返回值表示本次调用实际推进的进度增量（0 表示未推进）。
        /// 规则：只能从 lastReachedIndex 继续向前推进，不允许跳跃或回退。
        /// </summary>
        public float TryAdvance(Vector2 tipPos)
        {
            if (completed || points.Count < 2) return 0f;
            if (segmentLengths == null || segmentLengths.Count != points.Count) Initialize();
            if (segmentLengths.Count < points.Count || points.Count < 2) return 0f;

            if (lastReachedIndex >= points.Count) lastReachedIndex = 0;
            if (lastReachedIndex < 0) lastReachedIndex = 0;

            // 先检查剪刀尖端当前是否在任何一个"下一段"的有效范围内
            int segStart = lastReachedIndex;
            int segEnd = (segStart + 1) % points.Count;

            Vector2 a = points[segStart];
            Vector2 b = points[segEnd];

            float dist = DistancePointToSegment(tipPos, a, b);
            if (dist > tolerance)
            {
                // 超出容错范围，进度不增长
                return 0f;
            }

            // 计算剪刀沿该段已经推进了多少 t (0..1)
            float t = ProjectOntoSegment(tipPos, a, b);

            // 累计已覆盖长度 = 已完成段的总长 + 当前段已推进的长度
            float covered = 0f;
            for (int i = 0; i < segStart; i++) covered += segmentLengths[i];
            covered += segmentLengths[segStart] * t;

            float newProgress = Mathf.Clamp01(covered / totalLength);

            // 不允许回退
            if (newProgress <= progress) return 0f;

            float delta = newProgress - progress;
            progress = newProgress;

            // 判断是否推进到了段终点，更新 lastReachedIndex
            if (t >= 0.999f)
            {
                lastReachedIndex = segEnd;
                // 走完一整圈即完成
                if (lastReachedIndex == 0)
                {
                    completed = true;
                    progress = 1f;
                }
            }

            return delta;
        }

        /// <summary>剪刀尖端是否在路径附近（高亮用，不推进进度）。</summary>
        public bool IsNearPath(Vector2 tipPos)
        {
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                if (DistancePointToSegment(tipPos, points[i], points[next]) <= tolerance)
                    return true;
            }
            return false;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            float l2 = (b - a).sqrMagnitude;
            if (l2 < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / l2);
            Vector2 proj = a + t * (b - a);
            return Vector2.Distance(p, proj);
        }

        private static float ProjectOntoSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            float l2 = (b - a).sqrMagnitude;
            if (l2 < 0.0001f) return 0f;
            return Mathf.Clamp01(Vector2.Dot(p - a, b - a) / l2);
        }
    }
}
