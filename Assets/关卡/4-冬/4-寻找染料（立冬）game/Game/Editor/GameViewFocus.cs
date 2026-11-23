using UnityEditor;
using UnityEditorInternal;

namespace JiangnanDye.EditorTools
{
    /// <summary>
    /// 进入 Play 时自动把焦点切到 Game 视图，避免按键被其他编辑器窗口吃掉导致人物不能移动。
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewFocus
    {
        private static bool wasActive;

        static GameViewFocus()
        {
            wasActive = InternalEditorUtility.isApplicationActive;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnUpdate;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            FocusGameView();
        }

        /// <summary>从其它窗口切回 Unity 时，如果正在运行游戏就把焦点交还 Game 视图。</summary>
        private static void OnUpdate()
        {
            bool active = InternalEditorUtility.isApplicationActive;
            if (active && !wasActive && EditorApplication.isPlaying)
            {
                FocusGameView();
            }
            wasActive = active;
        }

        private static void FocusGameView()
        {
            System.Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;

            EditorWindow window = EditorWindow.GetWindow(gameViewType);
            if (window != null) window.Focus();
        }
    }
}
