using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Qingming.LevelBackground
{
    /// <summary>
    /// 春季关卡玩家视觉保护：
    /// 打开场景、进入播放模式前自动检查 Player/Visual 是否与碰撞盒水平对齐，
    /// 一旦发现偏移立即校正，避免"运行时碰撞盒与角色图片错位"的问题再次出现。
    /// 只作用于 Level1_Spring 场景，不影响其他关卡与物体。
    /// </summary>
    [InitializeOnLoad]
    public static class SpringPlayerVisualGuard
    {
        private const string SpringScenePath = "Assets/关卡/1-春/Level1_Spring.unity";

        static SpringPlayerVisualGuard()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // 编译完成时如果春季场景已打开，也立刻校验一次
            Enforce(SceneManager.GetActiveScene());
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Enforce(scene);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 进入播放前强制对齐，保证运行时的角色图片与碰撞盒一致
            if (state == PlayModeStateChange.ExitingEditMode)
                Enforce(SceneManager.GetActiveScene());
        }

        private static void Enforce(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            if (scene.path != SpringScenePath) return;

            var player = GameObject.Find("Player");
            if (player == null) return;

            var visual = player.transform.Find("Visual");
            if (visual == null) return;

            var col = player.GetComponent<Collider2D>();
            float targetX = col != null ? col.offset.x : 0f;

            Vector3 pos = visual.localPosition;
            if (Mathf.Abs(pos.x - targetX) < 0.001f) return;

            pos.x = targetX;
            visual.localPosition = pos;
            EditorUtility.SetDirty(visual);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.LogWarning($"[SpringPlayerVisualGuard] 检测到 Player/Visual 水平偏移，已自动校正 " +
                             $"X: {pos.x:F3} → {targetX:F3}，确保与碰撞盒对齐。");
        }
    }
}
