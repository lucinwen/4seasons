using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Qingming.Harvest
{
    /// <summary>
    /// UI 控制器：顶部进度数字、开场介绍、通关面板、按钮回调。
    /// </summary>
    public class HarvestUIController : MonoBehaviour
    {
        [Header("进度")]
        public Text progressText;

        [Header("开场面板")]
        public GameObject introPanel;
        public Button startButton;

        [Header("通关面板")]
        public GameObject completePanel;
        public Button continueButton;

        private Coroutine unsuitableFeedback;
        private Button sunnyRow;
        private Button shadedRow;
        private Button waitForDew;
        private Button waitInOrchard;
        private Text orchardStatus;
        private Text harvestRecord;
        private Text shadedRowNote;
        private Image shadedRowLeaf;
        private Sprite readyLeaf;
        private Sprite dewyLeaf;
        private RectTransform playingGuide;
        private RectTransform guideBasket;
        private RectTransform guideLeaf;
        private Image[] leftKeyFaces;
        private Image[] rightKeyFaces;
        private float guideOpenedAt;
        private bool movedBasket;

        private static readonly Vector2 GuideBasketCenter = new Vector2(352f, -8f);

        private void Start()
        {
            MinigameUiPresentation.Dialog(introPanel, "Body", "Title");
            MinigameUiPresentation.Dialog(completePanel, "Body", "Title");
            MinigameUiPresentation.Action(startButton);
            MinigameUiPresentation.Action(continueButton);
            BuildOrchardChoices();

            var progressBackground = progressText != null && progressText.transform.parent != null
                ? progressText.transform.parent.GetComponent<UnityEngine.UI.Image>()
                : null;
            MinigameUiPresentation.Surface(progressBackground, MinigameUiPresentation.Pine, true);
            MinigameUiPresentation.Label(progressText, MinigameUiPresentation.Paper, 28);
            if (progressBackground != null && progressBackground.name == "ProgressBg")
                progressBackground.rectTransform.sizeDelta = new Vector2(280f, 58f);

            // 用 Start 而非 Awake：覆盖层模式下 AddComponent 后立即触发 Awake，
            // 此时 startButton/continueButton 尚未赋值，监听器会注册失败。
            // Start 在下一帧执行，此时引用已由 HarvestOverlayManager 赋值完毕。
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        private void OnStartClicked()
        {
            var mgr = MulberryHarvestGameManager.Instance;
            if (mgr != null) mgr.StartGame();
        }

        private void OnContinueClicked()
        {
            var mgr = MulberryHarvestGameManager.Instance;
            if (mgr != null) mgr.ExitGame();
        }

        private void BuildOrchardChoices()
        {
            var body = MinigameUiPresentation.Find<RectTransform>(introPanel?.transform, "Body");
            if (body == null) return;
            body.sizeDelta = new Vector2(860, 620);
            readyLeaf = Resources.Load<Sprite>("WorkshopObservation/Spring_QingmingYoungMulberry");
            dewyLeaf = Resources.Load<Sprite>("WorkshopObservation/Spring_LeafWet");

            var title = MinigameUiPresentation.Find<Text>(body, "Title");
            PlaceIntroControl(title?.rectTransform, new Vector2(780, 65), new Vector2(0, 242));
            var description = MinigameUiPresentation.Find<Text>(body, "Desc");
            PlaceIntroControl(description?.rectTransform, new Vector2(780, 118), new Vector2(0, 137));
            if (description != null)
            {
                description.font = WorkshopArt.ReadingFont;
                description.fontSize = 28;
                description.lineSpacing = 1.16f;
                description.alignment = TextAnchor.MiddleCenter;
            }

            var orchard = MulberryHarvestGameManager.Instance;
            sunnyRow = AddOrchardChoice(body, "Sunny Mulberry Row", new Rect(40, 243, 380, 105),
                SpringSummerNpcDialogue.SunnyOrchardName, "嫩叶已展 · 叶面干爽", readyLeaf, () => orchard.SelectRow(false));
            shadedRow = AddOrchardChoice(body, "Shaded Mulberry Row", new Rect(440, 243, 380, 105),
                SpringSummerNpcDialogue.ShadedOrchardName, "有芽未展 · 叶面挂露", dewyLeaf, () => orchard.SelectRow(true));
            shadedRowNote = shadedRow.transform.Find("Row Condition").GetComponent<Text>();
            shadedRowLeaf = shadedRow.transform.Find("Row Leaf").GetComponent<Image>();

            orchardStatus = WorkshopUi.Label(body, "Orchard Condition", new Rect(41, 366, 778, 51), "",
                25, WorkshopUi.Ink, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
            waitForDew = WorkshopUi.Button(body, "Wait For Dew", new Rect(285, 426, 290, 60),
                "等露退、等叶舒展", orchard.WaitForDew, MinigameUiPresentation.Linen, size: 25);
            PlaceIntroControl(startButton?.GetComponent<RectTransform>(), new Vector2(286, 75), new Vector2(0, -236));
            if (startButton != null)
            {
                var play = WorkshopUi.Rect(startButton.transform, "Start Play Symbol", new Rect(31, 24, 26, 28));
                DrawGuideStroke(play, new Vector2(0, 0), new Vector2(21, 14), WorkshopUi.Cream);
                DrawGuideStroke(play, new Vector2(21, 14), new Vector2(0, 28), WorkshopUi.Cream);
                DrawGuideStroke(play, new Vector2(0, 28), new Vector2(0, 0), WorkshopUi.Cream);
            }
            harvestRecord = WorkshopUi.Label(body, "Harvest Record", new Rect(40, 585, 780, 29), "", 21,
                WorkshopUi.Ink, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);

            waitInOrchard = WorkshopUi.Button(introPanel.transform.parent, "Wait While Harvesting",
                new Rect(0, 0, 278, 60), "等露退、等叶舒展", orchard.WaitForDew, WorkshopUi.Green, size: 25);
            PlaceIntroControl(waitInOrchard.GetComponent<RectTransform>(), new Vector2(278, 60), new Vector2(0, 414));
            playingGuide = WorkshopUi.PixelCard(introPanel.transform.parent, "Harvest Playing Guide",
                new Rect(0, 0, 830, 94), WorkshopUi.Paper, WorkshopUi.Bronze);
            PlaceIntroControl(playingGuide, new Vector2(830, 94), new Vector2(0, -438));
            leftKeyFaces = new[] { DrawKeyHat(playingGuide, 50, "A"), DrawKeyHat(playingGuide, 127, "←") };
            rightKeyFaces = new[] { DrawKeyHat(playingGuide, 635, "→"), DrawKeyHat(playingGuide, 712, "D") };
            var basketSprite = WorkshopObservationArt.Current?.MulberryBasket ?? Resources.Load<Sprite>("WorkshopNoticeBoard/Notices/Basket");
            guideBasket = WorkshopUi.Picture(playingGuide, "Basket Movement Preview", new Rect(352, 8, 126, 80), basketSprite).rectTransform;
            guideLeaf = WorkshopUi.Picture(playingGuide, "Young Leaf Example", new Rect(397, 5, 36, 36), readyLeaf).rectTransform;
            DrawGuideArrow(playingGuide, new Vector2(276, 47), -1, WorkshopUi.Green);
            DrawGuideArrow(playingGuide, new Vector2(554, 47), 1, WorkshopUi.Green);
            UpdateOrchardChoices();
        }

        private static Image DrawKeyHat(Transform parent, float x, string key)
        {
            Rect bounds = new Rect(x, 16, 67, 62);
            var card = WorkshopUi.PixelCard(parent, key + " Key", bounds, WorkshopUi.Cream, WorkshopUi.Bronze);
            if (key == "←" || key == "→")
                DrawGuideArrow(card, bounds.size * .5f, key == "←" ? -1 : 1, WorkshopUi.Ink);
            else
                WorkshopUi.Label(card, "Key Symbol", new Rect(0, 0, bounds.width, bounds.height), key, 36,
                    WorkshopUi.Ink, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
            return card.Find("Surface").GetComponent<Image>();
        }

        private static void DrawGuideArrow(Transform parent, Vector2 center, int direction, Color color)
        {
            var tip = center + new Vector2(direction * 17, 0f);
            DrawGuideStroke(parent, center - new Vector2(direction * 17, 0f), tip, color);
            DrawGuideStroke(parent, tip, center + new Vector2(0f, -13f), color);
            DrawGuideStroke(parent, tip, center + new Vector2(0f, 13f), color);
        }

        private static void DrawGuideStroke(Transform parent, Vector2 from, Vector2 to, Color color)
        {
            Vector2 line = to - from;
            var rect = WorkshopUi.Fill(parent, "Direction Stroke", new Rect(from.x, from.y, line.magnitude, 5), color).rectTransform;
            rect.pivot = new Vector2(0f, .5f);
            rect.localEulerAngles = new Vector3(0, 0, -Mathf.Atan2(line.y, line.x) * Mathf.Rad2Deg);
        }

        private void Update()
        {
            if (playingGuide == null || !playingGuide.gameObject.activeInHierarchy) return;

            bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            if (left || right) movedBasket = true;

            float elapsed = Time.unscaledTime - guideOpenedAt;
            bool demonstrating = !movedBasket && elapsed < 3.2f;
            float horizontal = demonstrating ? Mathf.Sin(elapsed * Mathf.PI * 1.25f) : (right ? 1f : 0f) - (left ? 1f : 0f);
            var destination = GuideBasketCenter + Vector2.right * (horizontal * 40f);
            guideBasket.anchoredPosition = Vector2.MoveTowards(guideBasket.anchoredPosition, destination, Time.unscaledDeltaTime * 320f);
            float fall = demonstrating ? Mathf.Repeat(elapsed / .8f, 1f) : .5f;
            guideLeaf.anchoredPosition = new Vector2(guideBasket.anchoredPosition.x + 45f, -3f - 26f * fall);

            SetGuideKeys(leftKeyFaces, left || (demonstrating && horizontal < -.15f));
            SetGuideKeys(rightKeyFaces, right || (demonstrating && horizontal > .15f));
        }

        private static void SetGuideKeys(Image[] keys, bool pressed)
        {
            foreach (var face in keys)
                face.color = pressed ? new Color32(241, 208, 133, 255) : WorkshopUi.Cream;
        }

        private static Button AddOrchardChoice(Transform parent, string name, Rect bounds, string rowName,
            string condition, Sprite leaf, System.Action choose)
        {
            var button = WorkshopUi.Button(parent, name, bounds, "", choose);
            WorkshopUi.Picture(button.transform, "Row Leaf", new Rect(19, 12, 84, 81), leaf);
            WorkshopUi.Label(button.transform, "Row Name", new Rect(112, 5, 248, 44), rowName,
                28, WorkshopUi.Ink, TextAnchor.MiddleLeft, WorkshopArt.ReadingFont);
            WorkshopUi.Label(button.transform, "Row Condition", new Rect(112, 48, 248, 45), condition,
                23, WorkshopUi.Ink, TextAnchor.MiddleLeft, WorkshopArt.ReadingFont);
            var selected = WorkshopUi.Rect(button.transform, "Selected Row Mark", new Rect(334, 19, 30, 25));
            DrawGuideStroke(selected, new Vector2(0, 12), new Vector2(9, 21), WorkshopUi.Cream);
            DrawGuideStroke(selected, new Vector2(9, 21), new Vector2(28, 0), WorkshopUi.Cream);
            return button;
        }

        private static void PlaceIntroControl(RectTransform rect, Vector2 size, Vector2 position)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        internal void UpdateOrchardChoices()
        {
            var orchard = MulberryHarvestGameManager.Instance;
            if (orchard == null || orchardStatus == null) return;
            bool early = orchard.ShadedRow && !orchard.ShadeDewCleared;
            MinigameUiPresentation.Action(sunnyRow, !orchard.ShadedRow);
            MinigameUiPresentation.Action(shadedRow, orchard.ShadedRow);
            sunnyRow.transform.Find("Selected Row Mark").gameObject.SetActive(!orchard.ShadedRow);
            shadedRow.transform.Find("Selected Row Mark").gameObject.SetActive(orchard.ShadedRow);
            shadedRowNote.text = orchard.ShadeDewCleared ? "嫩叶已展 · 露水已退" : "有芽未展 · 叶面挂露";
            shadedRowLeaf.sprite = orchard.ShadeDewCleared ? readyLeaf : dewyLeaf;
            string rowName = orchard.ShadedRow ? SpringSummerNpcDialogue.ShadedOrchardName : SpringSummerNpcDialogue.SunnyOrchardName;
            orchardStatus.text = early ? $"已选{rowName} · 露未退，能接的嫩叶少"
                : $"已选{rowName} · 可以接干爽、展开的嫩叶";
            harvestRecord.text = WorkshopObservationView.HasRecord(WorkshopObservation.QingmingMulberry)
                ? "清明残页已收集 · 可再玩" : "首次完成可收集清明残页";
            waitForDew.gameObject.SetActive(early);
            waitInOrchard.gameObject.SetActive(early && orchard.State == HarvestGameState.Playing);
            if (playingGuide != null)
            {
                bool playing = orchard.State == HarvestGameState.Playing;
                if (playing && !playingGuide.gameObject.activeSelf)
                {
                    guideOpenedAt = Time.unscaledTime;
                    movedBasket = false;
                }
                playingGuide.gameObject.SetActive(playing);
            }
        }

        /// <summary>显示采桑数量与当前目标。</summary>
        public void UpdateProgress(int collected, int total)
        {
            if (unsuitableFeedback != null)
            {
                StopCoroutine(unsuitableFeedback);
                unsuitableFeedback = null;
            }
            if (progressText != null)
            {
                progressText.text = $"嫩叶  {collected} / {total}";
            }
        }

        internal void ShowHarvestNotice(string notice, int collected, int total)
        {
            if (progressText == null) return;
            if (unsuitableFeedback != null) StopCoroutine(unsuitableFeedback);
            progressText.text = notice;
            unsuitableFeedback = StartCoroutine(RestoreProgress(collected, total));
        }

        private IEnumerator RestoreProgress(int collected, int total)
        {
            yield return new WaitForSeconds(0.9f);
            unsuitableFeedback = null;
            UpdateProgress(collected, total);
        }

        public void ShowIntro()
        {
            if (introPanel != null)
            {
                var description = introPanel.transform.Find("Body/Desc")?.GetComponent<Text>();
                if (description != null)
                {
                    int goal = MulberryHarvestGameManager.Instance.targetLeaves;
                    description.text = "二十四节气 05/24 · 清明前后，桑芽开始舒展。\n" +
                        "先选叶已展开、露水已退的桑垄。\n" +
                        $"用 A/D 或 ←/→ 左右移篮，接满 {goal} 片干爽嫩叶。";
                    description.font = WorkshopArt.ReadingFont;
                }
                introPanel.SetActive(true);
            }
            UpdateOrchardChoices();
            if (completePanel != null) completePanel.SetActive(false);
        }

        public void HideIntro()
        {
            if (introPanel != null) introPanel.SetActive(false);
            UpdateOrchardChoices();
        }

        public void ShowComplete(int total, bool earnedPage)
        {
            if (completePanel == null) return;
            var description = completePanel.transform.Find("Body/Sub")?.GetComponent<Text>();
            if (description != null)
            {
                string status = earnedPage ? "获得嫩桑标本与清明残页" : "清明残页已收集";
                description.text = $"接到嫩叶 {total} 片 · {status}\n桑叶已收好";
                description.font = WorkshopArt.ReadingFont;
            }
            UpdateOrchardChoices();
            completePanel.SetActive(true);
        }

        public void HideComplete()
        {
            if (completePanel != null) completePanel.SetActive(false);
        }
    }
}
