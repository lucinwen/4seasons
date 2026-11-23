using UnityEngine;
using UnityEngine.UI;

namespace JiangnanDye.ClothCutting
{
    /// <summary>
    /// 单片衣片的显示 + 分离动画 + 收集栏移动。
    /// 裁剪完成后生成对应成品 Sprite，先放大弹出，再滑入右侧收集栏。
    /// </summary>
    public class CutPieceView : MonoBehaviour
    {
        public enum PieceKind { Front, Back, Sleeve }

        [Header("配置")]
        public PieceKind kind;

        [Tooltip("该衣片的成品 Sprite（袖片可共用，通过 flipX 区分左右）。")]
        public Sprite pieceSprite;

        [Tooltip("是否水平翻转（用于右袖片与左袖片镜像）。")]
        public bool flipX;

        [Header("运行时")]
        [System.NonSerialized] public Image pieceImage;
        [System.NonSerialized] public bool collected;

        private RectTransform rt;

        public void Init()
        {
            rt = GetComponent<RectTransform>();
            pieceImage = GetComponent<Image>();
            if (pieceImage == null) pieceImage = gameObject.AddComponent<Image>();
            pieceImage.sprite = pieceSprite;
            pieceImage.color = Color.white;
            pieceImage.raycastTarget = false;
            pieceImage.enabled = false;
            pieceImage.preserveAspect = true; // 绝不拉伸！

            // 根据 sprite 原始比例设 sizeDelta（保持原图比例，允许不同尺寸）
            if (pieceSprite != null)
            {
                var texSize = pieceSprite.rect.size;
                float maxDim = 180f; // 最大边 180 像素
                float scale = maxDim / Mathf.Max(texSize.x, texSize.y);
                rt.sizeDelta = new Vector2(texSize.x * scale, texSize.y * scale);
            }
            else
            {
                rt.sizeDelta = new Vector2(180f, 180f);
            }
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            collected = false;
        }

        /// <summary>在工作台对应位置生成并播放分离动画。</summary>
        public void PlaySeparateAnim(Vector2 worldPos)
        {
            if (pieceImage == null) Init();
            pieceImage.enabled = true;
            collected = true;
            rt.anchoredPosition = worldPos;
            rt.localScale = Vector3.zero;
            if (flipX)
            {
                Vector3 s = rt.localScale;
                s.x = -s.x;
                rt.localScale = s;
            }
        }

        public void MoveToCollection(Vector2 targetPos)
        {
            StopAllCoroutines();
            StartCoroutine(MoveAnim(targetPos));
        }

        private System.Collections.IEnumerator MoveAnim(Vector2 target)
        {
            // 先放大弹出
            float t = 0f;
            Vector3 big = flipX ? new Vector3(-1.4f, 1.4f, 1f) : new Vector3(1.4f, 1.4f, 1f);
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / 0.25f);
                rt.localScale = Vector3.Lerp(Vector3.zero, big, k);
                yield return null;
            }
            rt.localScale = flipX ? new Vector3(-1f, 1f, 1f) : Vector3.one;

            // 滑入收集栏
            Vector2 start = rt.anchoredPosition;
            t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / 0.5f);
                rt.anchoredPosition = Vector2.Lerp(start, target, k);
                yield return null;
            }
            rt.anchoredPosition = target;
        }
    }
}
