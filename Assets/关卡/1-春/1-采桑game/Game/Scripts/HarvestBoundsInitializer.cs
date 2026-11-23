using UnityEngine;

namespace Qingming.Harvest
{
    /// <summary>
    /// Play mode 开始时自动从 Path bounds 设置 Player 边界和 Spawner 生成范围。
    /// 用 RuntimeInitializeOnLoadMethod 确保在任何 MonoBehaviour 之前执行。
    /// </summary>
    public static class HarvestBoundsInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            UnityEngine.Debug.Log("[HarvestBoundsInitializer] Init() called!");
            
            var pathGo = GameObject.Find("Path");
            UnityEngine.Debug.Log("[HarvestBoundsInitializer] Path found: " + (pathGo != null));
            if (pathGo == null) return;
            var sr = pathGo.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            float left = sr.bounds.min.x;
            float right = sr.bounds.max.x;
            UnityEngine.Debug.Log("[HarvestBoundsInitializer] Path bounds: " + left + " ~ " + right);

            // Player
            var playerGo = GameObject.Find("Player");
            if (playerGo != null)
            {
                var pctrl = playerGo.GetComponent<HarvestPlayerController>();
                if (pctrl != null)
                {
                    pctrl.minX = left + 0.3f;
                    pctrl.maxX = right - 0.3f;
                }
            }

            // Spawner
            var spawnerGo = GameObject.Find("MulberryLeafSpawner");
            if (spawnerGo != null)
            {
                var sp = spawnerGo.GetComponent<MulberryLeafSpawner>();
                if (sp != null)
                {
                    sp.spawnXMin = left + 0.3f + sp.swayAmplitude;
                    sp.spawnXMax = right - 0.3f - sp.swayAmplitude;
                }
            }
        }
    }
}
