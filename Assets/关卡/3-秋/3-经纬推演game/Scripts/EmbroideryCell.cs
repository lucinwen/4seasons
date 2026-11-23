using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JingweiWeave
{
    /// <summary>
    /// 单个格子：负责 X 形刺绣的显示、悬停预览与点击上报。
    /// </summary>
    public class EmbroideryCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("坐标")]
        public int row;
        public int col;

        [Header("显示引用")]
        public Image xImage;          // 已织入的白色 X
        public Image previewImage;    // 悬停时的半透明预览 X

        private bool filled;

        private void Start()
        {
            // cells 数组不参与序列化，运行时必须由每格自行注册，
            // 否则 GameManager 拿不到引用，X 图案无法点亮
            if (EmbroideryGameManager.Instance != null)
                EmbroideryGameManager.Instance.RegisterCell(this, row, col);

            // onClick 必须在运行时绑定：Editor 阶段 AddListener 的监听器不会序列化保存
            Button btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnClick);

            SetFilled(false);
            if (previewImage != null) previewImage.gameObject.SetActive(false);
        }

        public void SetFilled(bool value)
        {
            filled = value;
            if (xImage != null) xImage.gameObject.SetActive(value);
        }

        /// <summary>由 Button.onClick 调用。</summary>
        public void OnClick()
        {
            if (EmbroideryGameManager.Instance != null)
                EmbroideryGameManager.Instance.OnCellClicked(row, col);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EmbroideryGameManager gm = EmbroideryGameManager.Instance;
            if (gm == null || gm.isLocked) return;

            // 梭子模式下，空白格显示半透明 X 预览
            if (gm.currentTool == EmbroideryTool.Shuttle && !filled && previewImage != null)
                previewImage.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (previewImage != null) previewImage.gameObject.SetActive(false);
        }

        /// <summary>通关时的轻微闪光。</summary>
        public void Flash()
        {
            if (gameObject.activeInHierarchy) StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            if (xImage == null) yield break;
            Color original = xImage.color;
            Color glow = new Color(1f, 0.93f, 0.55f, 1f);

            for (int i = 0; i < 3; i++)
            {
                xImage.color = glow;
                yield return new WaitForSeconds(0.12f);
                xImage.color = original;
                yield return new WaitForSeconds(0.12f);
            }
        }
    }
}
