using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JiangnanDye
{
    /// <summary>
    /// 在当前物理场景中检测可采集资源，优先选择最近的目标。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractionDetector : MonoBehaviour
    {
        [Header("检测参数")]
        [Tooltip("资源点没有指定交互半径时使用。")]
        public float detectionRadius = 1.2f;
        public LayerMask gatherableMask;

        [Header("目标切换")]
        [Tooltip("当前目标具有少量优先权，避免站在相邻资源之间时高亮来回跳动。")]
        public float targetSwitchMargin = 0.18f;

        private const int QueryBufferSize = 32;
        private readonly Collider2D[] results = new Collider2D[QueryBufferSize];
        private GatherableNode currentTarget;
        private Collider2D playerCollider;
        private PhysicsScene2D physicsScene;

        public GatherableNode CurrentTarget
        {
            get { return currentTarget; }
        }

        public bool HasTarget
        {
            get { return currentTarget != null && currentTarget.CanHarvest; }
        }

        public event Action<GatherableNode> TargetChanged;

        private void Awake()
        {
            playerCollider = GetComponent<Collider2D>();
            physicsScene = gameObject.scene.GetPhysicsScene2D();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Exploring)
            {
                SetTarget(null);
                return;
            }
            Detect();
        }

        private void Detect()
        {
            Vector2 feet = transform.position;
            Vector2 bodyCenter = playerCollider != null ? (Vector2)playerCollider.bounds.center : feet;
            // 搜索范围须覆盖两个判定中心，以及植物点位与固定黄圈之间的小幅偏移。
            float searchRadius = Mathf.Max(detectionRadius, GatherableNode.PlantInteractionRadius)
                + Vector2.Distance(feet, bodyCenter);

            var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
            filter.SetLayerMask(gatherableMask);
            int count = physicsScene.OverlapCircle(feet, searchRadius, filter, results);
            GatherableNode best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D col = results[i];
                if (col == null) continue;
                GatherableNode node = col.GetComponent<GatherableNode>();
                if (node == null) node = col.GetComponentInParent<GatherableNode>();
                if (node == null || !node.CanHarvest) continue;

                Vector2 anchorPoint = node.resourceType == ResourceType.Plant ? feet : bodyCenter;
                float distance = Vector2.Distance(anchorPoint, node.InteractionPoint);
                float effectiveRadius = node.interactionRadius > 0f ? node.interactionRadius : detectionRadius;
                if (distance > effectiveRadius) continue;

                // 当前目标有少量优先权，只有另一个目标明显更近时才切换。
                float score = node == currentTarget
                    ? Mathf.Max(0f, distance - targetSwitchMargin)
                    : distance;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = node;
                }
            }

            SetTarget(best);
        }

        public void SetTarget(GatherableNode node)
        {
            if (currentTarget == node) return;
            if (currentTarget != null) currentTarget.SetHighlighted(false);
            currentTarget = node;
            if (currentTarget != null) currentTarget.SetHighlighted(true);
            if (TargetChanged != null) TargetChanged(currentTarget);
        }

        public void ClearTarget()
        {
            SetTarget(null);
        }
    }
}
