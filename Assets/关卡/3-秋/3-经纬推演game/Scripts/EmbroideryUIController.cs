using UnityEngine;
using UnityEngine.UI;
using Qingming.LevelBackground;
using JiangnanDye;

namespace JingweiWeave
{
    /// <summary>
    /// 界面控制：行列数字提示的三态变色、重置 / 关闭按钮、成功弹窗。
    /// </summary>
    public class EmbroideryUIController : MonoBehaviour
    {
        [Header("数字提示（显示 当前/目标，随状态变色）")]
        public Text[] colHintTexts = new Text[8];
        public Text[] rowHintTexts = new Text[5];

        [Header("操作按钮")]
        public Button resetButton;
        public Button closeButton;

        [Header("成功弹窗")]
        public GameObject successPanel;
        public Button successConfirmButton;

        private static readonly Color NormalColor = MinigameUiPresentation.Paper;
        private static readonly Color DoneColor = new Color32(183, 228, 168, 255);
        private static readonly Color OverColor = new Color32(255, 158, 133, 255);

        private EmbroideryGameManager gm;

        private void Start()
        {
            gm = EmbroideryGameManager.Instance;
            gm.OnBoardChanged += RefreshHints;
            gm.OnSuccess += ShowSuccess;

            if (resetButton != null) resetButton.onClick.AddListener(OnReset);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
            if (successConfirmButton != null) successConfirmButton.onClick.AddListener(OnSuccessConfirm);
            if (successPanel != null) successPanel.SetActive(false);

            MinigameUiPresentation.Action(resetButton, false);
            MinigameUiPresentation.Dialog(successPanel, "Card", "Title");
            MinigameUiPresentation.Action(successConfirmButton);

            ApplyLayoutTweaks();
            ApplyModePresentation();
            CreateIntroText();
            RefreshHints();
        }

        /// <summary>去掉关闭按钮、重置按钮移到右上角、棋盘与工具整体左下移。</summary>
        private void ApplyLayoutTweaks()
        {
            if (closeButton != null) closeButton.gameObject.SetActive(false);

            if (resetButton != null)
            {
                var rt = resetButton.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-40f, -40f);
                }
            }

            if (gm == null || gm.uiRoot == null) return;

            // 棋盘连同左右两侧工具一起向左、向下移动
            var shift = new Vector2(-40f, -80f);
            string[] playParts = { "Board", "ShuttleButton", "BeaterButton" };
            foreach (var part in playParts)
            {
                var child = gm.uiRoot.transform.Find(part) as RectTransform;
                if (child != null) child.anchoredPosition += shift;
            }
        }

        private void ApplyModePresentation()
        {
            if (gm == null || !gm.IsHanluPattern || gm.uiRoot == null) return;
            var board = gm.uiRoot.transform.Find("Board") as RectTransform;
            if (board == null) return;

            const float scale = 1300f / 1672f;
            Color tint = DyedClothTint();
            tint.a = 0.52f;
            var cloth = WorkshopUi.Fill(board, "Dyed Cloth Base",
                new Rect(368f * scale, 168f * scale, (1571f - 368f) * scale, (852f - 168f) * scale), tint);
            cloth.raycastTarget = false;
            cloth.transform.SetSiblingIndex(0);
        }

        private static Color DyedClothTint()
        {
            string saved = WorkshopProgress.DyedColor;
            var config = Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
            foreach (var color in DyeColorUtil.AllColors)
            {
                string name = DyeColorUtil.ChineseName(color);
                if (saved != name && saved != name + "布") continue;
                return config != null ? config.GetClothTint(color) : DyeColorUtil.Tint(color);
            }
            return new Color32(214, 190, 150, 255);
        }

        /// <summary>在棋盘顶部添加玩法说明。</summary>
        private void CreateIntroText()
        {
            Transform parent = gm.uiRoot != null ? gm.uiRoot.transform : null;
            if (parent == null)
            {
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    var c = root.GetComponentInChildren<Canvas>(true);
                    if (c != null && c.isRootCanvas) { parent = c.transform; break; }
                }
            }
            if (parent == null) return;

            var go = new GameObject("Weaving Instructions", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(960f, 64f);
            rt.anchoredPosition = new Vector2(-70f, 472f);

            var plate = go.GetComponent<UnityEngine.UI.Image>();
            plate.raycastTarget = false;
            MinigameUiPresentation.Surface(plate, MinigameUiPresentation.Pine, true);
            if (gm.IsHanluPattern)
            {
                rt.sizeDelta = new Vector2(1080f, 100f);
                rt.anchoredPosition = new Vector2(-70f, 452f);
                WorkshopUi.Label(rt, "Mode Title", new Rect(22f, 4f, 1036f, 40f),
                    "寒露攀花", 31, MinigameUiPresentation.Paper, TextAnchor.MiddleCenter, WorkshopArt.WorkshopFont);
                WorkshopUi.Label(rt, "Instruction", new Rect(22f, 46f, 1036f, 46f),
                    "参照每行和每列的目标数字，用梭子在染布上织出花纹", 25,
                    MinigameUiPresentation.Paper, TextAnchor.MiddleCenter, WorkshopArt.WorkshopFont);
            }
            else
            {
                WorkshopUi.Label(rt, "Instruction", new Rect(22f, 0f, 916f, 64f),
                    "参照每行和每列的目标数字，用梭子织出图案", 27,
                    MinigameUiPresentation.Paper, TextAnchor.MiddleCenter, WorkshopArt.WorkshopFont);
            }
        }

        /// <summary>重新统计并刷新 13 个数字提示的颜色。</summary>
        public void RefreshHints()
        {
            if (gm == null) return;

            for (int c = 0; c < EmbroideryGameManager.Cols; c++)
            {
                if (colHintTexts[c] == null) continue;
                int cur = gm.GetColCount(c);
                int target = gm.colTargets[c];
                colHintTexts[c].text = cur + "/" + target;
                colHintTexts[c].color = EvaluateColor(cur, target);
            }

            for (int r = 0; r < EmbroideryGameManager.Rows; r++)
            {
                if (rowHintTexts[r] == null) continue;
                int cur = gm.GetRowCount(r);
                int target = gm.rowTargets[r];
                rowHintTexts[r].text = cur + "/" + target;
                rowHintTexts[r].color = EvaluateColor(cur, target);
            }
        }

        private static Color EvaluateColor(int current, int target)
        {
            if (current > target) return OverColor;
            if (current == target) return DoneColor;
            return NormalColor;
        }

        private void OnReset()
        {
            gm.ResetBoard();
            if (successPanel != null) successPanel.SetActive(false);
        }

        private void OnClose()
        {
            gm.CloseGame();
        }

        private void ShowSuccess()
        {
            if (successPanel != null) successPanel.SetActive(false);
            MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.WeavingPuzzle, ShowSuccessPopup);
        }

        private void ShowSuccessPopup()
        {
            if (gm.IsHanluPattern)
            {
                WorkshopProgress.MarkStep("autumn.hanlu.patterned");
                SimplePopup.Show("寒露攀花", "攀花完成，染布上的花纹已经织好。", "完成", gm.CloseGame);
                return;
            }

            // 通关奖励：生坯绸道具弹窗，收下后关闭小游戏叠加层
            SimplePopup.ShowItem(
                WorkshopItem.RawSilkCloth,
                "生坯绸是未经染整加工的丝织坯布，质地柔韧、色泽自然，\n还需经过精练、染色等工序，才能成为成品绸缎。",
                () =>
                {
                    if (WorkshopProgress.Award(WorkshopItem.RawSilkCloth))
                        WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.AutumnWarpComplete, gm.CloseGame);
                    else
                        gm.CloseGame();
                }, fallbackIcon: WorkshopArt.RawSilk);
        }

        private void OnSuccessConfirm()
        {
            if (successPanel != null) successPanel.SetActive(false);
            gm.CloseGame();
        }
    }
}
