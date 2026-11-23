using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye
{
    /// <summary>右下角圆形锄头按钮：始终常亮显示，有目标时高亮呼吸，点击播放挥锄动画。</summary>
    public class HoeButtonUI : MonoBehaviour
    {
        [Header("引用")]
        public GameManager gameManager;
        public InteractionDetector detector;
        public Button button;
        public Image background;
        public Image ring;
        public Image icon;
        public CanvasGroup group;
        public RectTransform swingRoot;

        [Header("动画")]
        public float pulseSpeed = 3.2f;

        private bool swinging;
        private float pulseTime;
        private Coroutine swingRoutine;

        private void Start()
        {
            if (background != null) background.color = MinigameUiPresentation.Paper;
            if (ring != null) ring.color = MinigameUiPresentation.Bronze;
            if (button != null) button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        private void Update()
        {
            bool canHarvest = !WorkshopMinigameMenu.IsPaused
                              && gameManager != null
                              && gameManager.State == GameState.Exploring
                              && detector != null
                              && detector.HasTarget;

            if (button != null) button.interactable = canHarvest && !swinging;
            if (group != null) group.alpha = 1f;   // 锄头始终显示，不做淡出

            // 回车键 = 采集快捷键（等价于点一下锄头）
            if (canHarvest && !swinging
                && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                OnClick();
            }

            if (ring != null)
            {
                pulseTime += Time.deltaTime * pulseSpeed;
                float pulse = canHarvest ? 0.75f + Mathf.Sin(pulseTime) * 0.25f : 0.45f;
                Color rc = ring.color;
                rc.a = pulse;
                ring.color = rc;
            }
        }

        public void OnClick()
        {
            if (swinging || WorkshopMinigameMenu.IsPaused) return;
            if (gameManager == null || gameManager.State != GameState.Exploring) return;
            if (detector == null || detector.CurrentTarget == null) return;
            if (swingRoutine != null) StopCoroutine(swingRoutine);
            swingRoutine = StartCoroutine(Swing());
        }

        private IEnumerator Swing()
        {
            swinging = true;
            if (swingRoot != null)
            {
                float t = 0f;
                while (t < 0.12f)
                {
                    t += Time.deltaTime;
                    swingRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -34f, t / 0.12f));
                    yield return null;
                }

                while (WorkshopMinigameMenu.IsPaused) yield return null;
                gameManager.HarvestCurrent();

                t = 0f;
                while (t < 0.26f)
                {
                    t += Time.deltaTime;
                    swingRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-34f, 0f, t / 0.26f));
                    yield return null;
                }
                swingRoot.localRotation = Quaternion.identity;
            }
            else
            {
                gameManager.HarvestCurrent();
            }

            swinging = false;
        }
    }
}
