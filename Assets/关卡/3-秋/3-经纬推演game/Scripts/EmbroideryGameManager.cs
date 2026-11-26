using System;
using System.Collections;
using UnityEngine;

namespace JingweiWeave
{
    /// <summary>工具类型：梭子=织入，拔子=取消。</summary>
    public enum EmbroideryTool { Shuttle, Beater }

    public enum EmbroideryPlayMode { Weaving, HanluPattern }

    /// <summary>
    /// 经纬织纹 · 游戏主逻辑。
    /// 负责棋盘数据、工具切换、行列计数与多解法通关判定。
    /// </summary>
    public class EmbroideryGameManager : MonoBehaviour
    {
        public static EmbroideryGameManager Instance { get; private set; }
        private static EmbroideryPlayMode nextMode = EmbroideryPlayMode.Weaving;

        public EmbroideryPlayMode PlayMode { get; private set; }
        public bool IsHanluPattern => PlayMode == EmbroideryPlayMode.HanluPattern;

        public static void SetNextMode(EmbroideryPlayMode mode)
        {
            nextMode = mode;
        }

        public const int Rows = 5;
        public const int Cols = 8;

        [Header("关卡目标（可在 Inspector 直接修改以更换关卡）")]
        public int[] rowTargets = { 6, 2, 4, 2, 6 };
        public int[] colTargets = { 2, 3, 2, 3, 3, 2, 3, 2 };

        [Header("运行状态")]
        public EmbroideryTool currentTool = EmbroideryTool.Shuttle;
        public bool isLocked;

        [Header("UI 根节点（打开 / 关闭小游戏时切换显隐）")]
        public GameObject uiRoot;

        private bool[,] grid = new bool[Rows, Cols];
        private EmbroideryCell[,] cells = new EmbroideryCell[Rows, Cols];
        private bool successFired;

        internal bool HasRunProgress
        {
            get
            {
                foreach (bool filled in grid)
                    if (filled) return true;
                return false;
            }
        }

        /// <summary>棋盘内容发生变化（用于刷新行列数字提示）。</summary>
        public event Action OnBoardChanged;
        /// <summary>通关动画播放完毕后触发（用于弹成功提示）。</summary>
        public event Action OnSuccess;
        /// <summary>小游戏被关闭时触发（供主游戏接管后续流程）。</summary>
        public event Action OnClosed;

        private void Awake()
        {
            PlayMode = nextMode;
            Instance = this;
        }

        public void RegisterCell(EmbroideryCell cell, int r, int c)
        {
            cells[r, c] = cell;
        }

        public void SetTool(EmbroideryTool tool)
        {
            currentTool = tool;
        }

        public bool IsFilled(int r, int c)
        {
            return grid[r, c];
        }

        /// <summary>格子被点击：按当前工具织入或取消。</summary>
        public void OnCellClicked(int r, int c)
        {
            if (isLocked) return;

            if (currentTool == EmbroideryTool.Shuttle)
            {
                if (grid[r, c]) return;          // 已有刺绣，不重复放置
                grid[r, c] = true;
                if (cells[r, c] != null) cells[r, c].SetFilled(true);
            }
            else
            {
                if (!grid[r, c]) return;         // 空白格，不产生变化
                grid[r, c] = false;
                if (cells[r, c] != null) cells[r, c].SetFilled(false);
            }

            NotifyChanged();

            if (CheckSuccess()) TriggerSuccess();
        }

        public int GetRowCount(int r)
        {
            int n = 0;
            for (int c = 0; c < Cols; c++) if (grid[r, c]) n++;
            return n;
        }

        public int GetColCount(int c)
        {
            int n = 0;
            for (int r = 0; r < Rows; r++) if (grid[r, c]) n++;
            return n;
        }

        /// <summary>
        /// 多解法通关判定：仅校验每行 / 每列的刺绣数量是否等于目标，
        /// 不比对任何预设图案，任何满足全部 13 个约束的布局均判成功。
        /// </summary>
        public bool CheckSuccess()
        {
            for (int i = 0; i < Rows; i++)
                if (GetRowCount(i) != rowTargets[i]) return false;

            for (int j = 0; j < Cols; j++)
                if (GetColCount(j) != colTargets[j]) return false;

            return true;
        }

        private void TriggerSuccess()
        {
            if (successFired) return;            // 防止重复触发通关
            successFired = true;
            isLocked = true;                     // 成功后锁定棋盘
            StartCoroutine(SuccessSequence());
        }

        private IEnumerator SuccessSequence()
        {
            // 全部 X 形刺绣按行依次闪光
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (grid[r, c] && cells[r, c] != null)
                        cells[r, c].Flash();
                }
                yield return new WaitForSeconds(0.12f);
            }
            yield return new WaitForSeconds(0.5f);

            if (OnSuccess != null) OnSuccess();
        }

        /// <summary>重置：清空 40 格、计数归零、清除完成状态、恢复默认工具，目标数字不变。</summary>
        public void ResetBoard()
        {
            isLocked = false;
            successFired = false;

            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    grid[r, c] = false;
                    if (cells[r, c] != null) cells[r, c].SetFilled(false);
                }

            currentTool = EmbroideryTool.Shuttle;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            if (OnBoardChanged != null) OnBoardChanged();
        }

        // ------------------------------------------------------------------
        // 供主游戏调用的接口
        // ------------------------------------------------------------------

        /// <summary>打开小游戏（主游戏在合适时机调用，例如与织机交互时）。</summary>
        public void OpenGame()
        {
            if (uiRoot != null) uiRoot.SetActive(true);
            ResetBoard();
        }

        /// <summary>关闭小游戏并广播 OnClosed，不会退出整个程序。</summary>
        public void CloseGame()
        {
            // 以叠加层方式打开时，先卸载小游戏场景
            if (MiniGameOverlay.IsActive)
            {
                MiniGameOverlay.Close();
                return;
            }

            if (OnClosed != null) OnClosed();
            if (uiRoot != null) uiRoot.SetActive(false);
        }
    }
}
