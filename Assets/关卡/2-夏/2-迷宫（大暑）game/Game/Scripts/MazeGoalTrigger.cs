using UnityEngine;

namespace JiangnanDye.MazeGame
{
    /// <summary>
    /// 终点触发器。玩家进入 CircleCollider2D (IsTrigger) 时通知 GameManager 通关。
    /// 线轴道具缩放为 0.1，场景里的半径在世界空间只有 0.05，玩家容易直接走过；
    /// 运行时按世界半径重设碰撞圈，保证能稳定触发。
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class MazeGoalTrigger : MonoBehaviour
    {
        [Tooltip("终点触发半径（世界单位）")]
        public float worldRadius = 1.2f;

        private MazeGameManager manager;

        private void Awake()
        {
            var circle = GetComponent<CircleCollider2D>();
            if (circle == null) return;

            circle.isTrigger = true;
            circle.enabled = true;

            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            if (scale > 0.0001f) circle.radius = worldRadius / scale;
        }

        public void BindManager(MazeGameManager mgr)
        {
            manager = mgr;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (manager == null) return;
            if (other.GetComponent<MazePlayerController>() != null)
                manager.OnPlayerReachGoal();
        }
    }
}
