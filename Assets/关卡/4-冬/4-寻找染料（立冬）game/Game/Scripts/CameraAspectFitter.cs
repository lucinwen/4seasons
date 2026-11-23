using UnityEngine;

namespace JiangnanDye
{
    /// <summary>固定 16:9 视野，窗口比例不符时用黑边填充，保证地图不被裁切。</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraAspectFitter : MonoBehaviour
    {
        public float targetAspect = 16f / 9f;

        private Camera cam;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            Apply();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) Apply();
        }

        private void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            if (lastWidth <= 0 || lastHeight <= 0) return;

            float windowAspect = (float)lastWidth / lastHeight;
            float scale = windowAspect / targetAspect;

            if (scale < 1f)
            {
                cam.rect = new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
            }
            else
            {
                float width = 1f / scale;
                cam.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
        }
    }
}
