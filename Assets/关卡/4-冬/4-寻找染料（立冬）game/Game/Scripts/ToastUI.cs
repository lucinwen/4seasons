using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye
{
    /// <summary>轻量提示条（例如采集到普通石头时）。</summary>
    public class ToastUI : MonoBehaviour
    {
        public CanvasGroup group;
        public Text text;
        public float fadeDuration = 0.25f;

        private Coroutine routine;

        private void Awake()
        {
            MinigameUiPresentation.Surface(GetComponent<UnityEngine.UI.Image>(), MinigameUiPresentation.Pine, true);
            MinigameUiPresentation.Label(text, MinigameUiPresentation.Paper);
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        public void Show(string message, float duration = 2.4f)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(message, duration));
        }

        /// <summary>立即隐藏（例如弹出物品收取窗口时避免遮挡按钮）。</summary>
        public void Hide()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            if (group != null) group.alpha = 0f;
        }

        private IEnumerator Run(string message, float duration)
        {
            if (text != null) text.text = message;

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                if (group != null) group.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
                yield return null;
            }
            if (group != null) group.alpha = 1f;

            yield return new WaitForSeconds(duration);

            t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                if (group != null) group.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }
            if (group != null) group.alpha = 0f;
            routine = null;
        }
    }
}
