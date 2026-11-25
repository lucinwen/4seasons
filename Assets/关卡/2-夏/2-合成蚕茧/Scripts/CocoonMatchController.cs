using System.Collections;
using System.Collections.Generic;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace ShiXu.CocoonMatch
{
    public sealed class CocoonMatchController : MonoBehaviour
    {
        public CocoonMatchLevel level;
        public RectTransform boardRoot;
        public RectTransform animationRoot;
        public RectTransform[] slots;
        public Image[] slotHighlights;
        public List<CocoonTileView> tiles = new List<CocoonTileView>();
        public Text remainingText;
        public Text clearedText;
        public Text trayText;
        public Text feedbackText;
        public Button restartButton;
        public GameObject resultPanel;
        public Text resultTitle;
        public Text resultMessage;
        public Button resultRestartButton;
        public GameObject gameSurface;
        public GameObject startPanel;
        public Button startButton;
        public GameObject closedPanel;
        public Button returnButton;
        public Text harvestText;
        public CanvasGroup harvestGroup;
        [Tooltip("后续填写组员场景名称或路径，并将场景加入 Build Settings。留空则关闭小游戏并显示离开页面。")]
        public string exitSceneName;
        public UnityEvent onLeaveWorkshop = new UnityEvent();
        [Tooltip("由叠加层接管结果弹窗时勾选：不再显示场景自带的结果面板。")]
        public bool useExternalResultPopup;
        public event System.Action<bool> GameEnded;
        [Range(.1f, 1f)] public float travelDuration = .32f;
        [Tooltip("收集槽内的图块尺寸，可与场上的大图块分别调整。")]
        public float trayTileSize = 72f;
        private CocoonMatchModel model;
        private bool busy;
        private bool started;
        private bool leftWorkshop;
        private Coroutine notice;
        public CocoonMatchModel Model => model;
        public bool IsAnimating => busy;
        internal bool CanPause => started && !busy && model?.State == CocoonGameState.Playing;

        private void Start()
        {
            StylePresentation();
            restartButton.onClick.AddListener(RestartGame);
            resultRestartButton.onClick.AddListener(LeaveWorkshop);
            startButton.onClick.AddListener(BeginGame);
            returnButton.onClick.AddListener(ShowStartScreen);
            ShowStartScreen();
        }

        private void StylePresentation()
        {
            MinigameUiPresentation.Dialog(startPanel, "StartCard", "StartTitle");
            MinigameUiPresentation.Dialog(resultPanel, "ResultCard", "ResultTitle");
            MinigameUiPresentation.Dialog(closedPanel, "ClosedCard", "ClosedTitle");
            MinigameUiPresentation.Action(startButton);
            MinigameUiPresentation.Action(restartButton, false);
            MinigameUiPresentation.Action(resultRestartButton);
            MinigameUiPresentation.Action(returnButton);

            if (gameSurface != null)
            {
                var surface = gameSurface.transform;
                MinigameUiPresentation.Label(MinigameUiPresentation.Find<Text>(surface, "Title"), MinigameUiPresentation.Pine);
                MinigameUiPresentation.Label(MinigameUiPresentation.Find<Text>(surface, "SolarTerm"), MinigameUiPresentation.Pine);
                MinigameUiPresentation.Label(MinigameUiPresentation.Find<Text>(surface, "SolarIntroduction"), MinigameUiPresentation.MutedInk);
            }
            MinigameUiPresentation.Label(remainingText, MinigameUiPresentation.Pine, 20);
            MinigameUiPresentation.Label(clearedText, MinigameUiPresentation.MutedInk, 19);
            MinigameUiPresentation.Label(trayText, MinigameUiPresentation.Pine);

            // 收茧反馈靠近画面底边，避免盖住标题与可点击图块。
            if (harvestText != null)
            {
                harvestText.rectTransform.anchoredPosition = new Vector2(0f, -466f);
                MinigameUiPresentation.Label(harvestText, MinigameUiPresentation.Paper, 23);
            }
        }

        public void ShowStartScreen()
        {
            RestartGame();
            started = false;
            startPanel.SetActive(true);
            RefreshViews();
        }

        public void BeginGame()
        {
            if (leftWorkshop || model == null || started) return;
            started = true;
            startPanel.SetActive(false);
            RefreshViews();
        }

        public void LeaveWorkshop()
        {
            if (leftWorkshop) return;
            StopAllCoroutines();
            notice = null;
            busy = started = false;
            leftWorkshop = true;
            harvestGroup.alpha = 0;
            resultPanel.SetActive(false);
            startPanel.SetActive(false);
            gameSurface.SetActive(false);
            closedPanel.SetActive(true);
            string destination = exitSceneName;
            onLeaveWorkshop.Invoke();
            if (!string.IsNullOrWhiteSpace(destination) && Application.CanStreamedLevelBeLoaded(destination))
                SceneManager.LoadSceneAsync(destination);
        }

        internal bool ClaimReward()
        {
            if (model == null || model.State != CocoonGameState.Won) return false;
            SummerItemInteraction.CollectSuoxu();
            return WorkshopProgress.Award(WorkshopItem.Suoxu);
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            notice = null;
            busy = false;
            started = true;
            leftWorkshop = false;
            gameSurface.SetActive(true);
            startPanel.SetActive(false);
            closedPanel.SetActive(false);
            harvestGroup.alpha = 0;
            harvestText.text = "";
            level.ValidateData();
            if (slots.Length != CocoonMatchModel.Capacity || tiles.Count != level.placements.Count)
            {
                Debug.LogError("蔟上收茧：场景图块或收集槽配置不完整。", this);
                enabled = false;
                return;
            }
            model = new CocoonMatchModel(level);
            resultPanel.SetActive(false);
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                tile.gameObject.SetActive(true);
                tile.Rect.SetParent(boardRoot, false);
                tile.Rect.anchoredPosition = level.placements[i].position;
                tile.Rect.sizeDelta = Vector2.one * level.tileSize;
                tile.Rect.SetAsLastSibling();
                tile.Configure(i, level.patterns[level.placements[i].pattern], OnTileClicked);
            }
            RefreshViews();
        }

        private void OnTileClicked(int id)
        {
            if (!started || leftWorkshop || busy || model == null || !model.TryCollect(id, out var move)) return;
            busy = true;
            WorkshopAudioFeedback.Place();
            RefreshViews();
            StartCoroutine(PlayMove(move));
        }

        private IEnumerator PlayMove(CocoonMove move)
        {
            foreach (int id in move.trayBeforeMatch)
            {
                tiles[id].Rect.SetParent(animationRoot, true);
                tiles[id].transform.localScale = Vector3.one;
                tiles[id].SetState(true, true, true);
            }
            yield return Arrange(move.trayBeforeMatch, travelDuration, move.tileId);
            if (move.matched.Length == 3)
            {
                WorkshopAudioFeedback.Confirm();
                ShowHarvestNotice(level.placements[move.matched[0]].pattern);
                float elapsed = 0;
                while (elapsed < .22f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / .22f);
                    foreach (int id in move.matched)
                    {
                        tiles[id].transform.localScale = Vector3.one * Mathf.Lerp(1, .15f, t);
                        tiles[id].group.alpha = 1 - t;
                    }
                    yield return null;
                }
                foreach (int id in move.matched) tiles[id].gameObject.SetActive(false);
                yield return Arrange(move.trayAfterMatch, .18f, -1);
            }
            busy = false;
            RefreshViews();
            if (model.State != CocoonGameState.Playing)
            {
                bool won = model.State == CocoonGameState.Won;
                if (won) yield return new WaitForSecondsRealtime(.35f);
                resultTitle.text = won ? "收茧完成" : "收集槽已满";
                resultMessage.text = won ? "蚕茧和工具已收好。" : "收集槽有七格，凑齐三个同类即可腾出位置。";
                if (won)
                {
                    MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.CocoonSorting, () => PresentEnd(won));
                }
                else
                {
                    PresentEnd(won);
                }
            }
        }

        private void PresentEnd(bool won)
        {
            if (!useExternalResultPopup)
            {
                if (won)
                {
                    SimplePopup.ShowItem(WorkshopItem.Suoxu, "可与盐、线轴合成生丝。",
                        () =>
                        {
                            ClaimReward();
                            LeaveWorkshop();
                        }, fallbackIcon: SummerSuoxuConfig.RewardSprite);
                }
                else resultPanel.SetActive(true);
            }
            GameEnded?.Invoke(won);
        }

        private void ShowHarvestNotice(int pattern)
        {
            if (notice != null) StopCoroutine(notice);
            harvestText.text = level.patterns[pattern].harvestMessage;
            notice = StartCoroutine(AnimateHarvestNotice());
        }

        private IEnumerator AnimateHarvestNotice()
        {
            harvestGroup.alpha = 1;
            float elapsed = 0;
            while (elapsed < 4f)
            {
                elapsed += Time.unscaledDeltaTime;
                harvestGroup.alpha = Mathf.Min(Mathf.Clamp01(elapsed / .12f), Mathf.Clamp01((4f - elapsed) / .45f));
                yield return null;
            }
            harvestGroup.alpha = 0;
            notice = null;
        }

        private IEnumerator Arrange(int[] ids, float duration, int flyingId)
        {
            var starts = new Vector3[ids.Length];
            var startSizes = new Vector2[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                starts[i] = tiles[ids[i]].Rect.position;
                startSizes[i] = tiles[ids[i]].Rect.sizeDelta;
            }
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1 - Mathf.Pow(1 - t, 3);
                for (int i = 0; i < ids.Length; i++)
                {
                    Vector3 position = Vector3.LerpUnclamped(starts[i], slots[i].position, eased);
                    if (ids[i] == flyingId) position += animationRoot.up * (Mathf.Sin(t * Mathf.PI) * 26 * animationRoot.lossyScale.y);
                    tiles[ids[i]].Rect.position = position;
                    tiles[ids[i]].Rect.sizeDelta = Vector2.Lerp(startSizes[i], Vector2.one * trayTileSize, eased);
                }
                yield return null;
            }
            for (int i = 0; i < ids.Length; i++)
            {
                tiles[ids[i]].Rect.position = slots[i].position;
                tiles[ids[i]].Rect.sizeDelta = Vector2.one * trayTileSize;
            }
        }

        private void RefreshViews()
        {
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i].gameObject.activeSelf)
                    tiles[i].SetState(model.IsExposed(i), !model.IsOnBoard(i), busy || !started || leftWorkshop || model.State != CocoonGameState.Playing);
            remainingText.text = $"场上剩余\n<size=36>{model.BoardCount}</size>";
            clearedText.text = $"已收好\n<size=27>{model.ClearedCount} / {tiles.Count}</size>";
            for (int i = 0; i < slotHighlights.Length; i++)
                slotHighlights[i].color = i < model.Tray.Count ? new Color(.38f, .52f, .37f, .7f) : new Color(.54f, .58f, .4f, .22f);
        }
    }
}
