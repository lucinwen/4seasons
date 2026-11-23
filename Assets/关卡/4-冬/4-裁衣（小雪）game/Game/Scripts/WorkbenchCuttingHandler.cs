using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JiangnanDye.ClothCutting
{
    /// <summary>
    /// 挂在 Workbench 上的裁剪交互处理器。
    /// 用 UGUI 事件系统（PointerEventData.position）代替有 bug 的 Input.mousePosition。
    /// 流程：在裁剪模式激活时 → 鼠标在工作台按下 → 拖动 → 松开。
    /// </summary>
    public class WorkbenchCuttingHandler : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public ClothCuttingGameManager manager;
        public ScissorsController scissorsController;
        public Canvas canvas;

        private bool isDragging;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (WorkshopMinigameMenu.IsPaused) return;
            UnityEngine.Debug.Log("[WorkbenchHandler] OnPointerDown called, pos=" + eventData.position);
            // 注意：同一鼠标点击事件可能先触发 WorkbenchHandler.OnPointerDown
            // 再触发 Button.onClick（ScissorsController.OnButtonClick）。
            // 所以这里不能依赖 scissorsController.cuttingMode —— 如果用户"刚点完剪刀按钮马上点工作台"，
            // 那时 cuttingMode 可能还没来得及变成 true。
            // 替代方案：如果 cuttingMode 还是 false，那这次点击就是点剪刀按钮（按钮自己会处理），
            // 直接放弃；否则才接受。
            if (scissorsController == null) return;
            if (!scissorsController.cuttingMode)
            {
                UnityEngine.Debug.Log("  ignored: scissors not cuttingMode yet (maybe this click was for the button)");
                return;
            }
            if (manager != null && manager.IsGameOver) return;

            Vector2 canvasLocal = ScreenToCanvasLocal(eventData.position);
            Vector2 tipLocal = canvasLocal + scissorsController.tipOffset;
            UnityEngine.Debug.Log("  canvasLocal=" + canvasLocal + " tipLocal=" + tipLocal);
            if (!manager.IsPointInWorkArea(tipLocal))
            {
                UnityEngine.Debug.Log("  NOT in workbench area, ignored");
                return;
            }

            isDragging = true;
            scissorsController.isCutting = true;
            manager.OnCutStart(tipLocal);
            UnityEngine.Debug.Log("  -> START CUTTING! activePiece=" + manager.activePiece);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || WorkshopMinigameMenu.IsPaused) return;
            UnityEngine.Debug.Log("[WorkbenchHandler] OnDrag called, isDragging=true");

            Vector2 canvasLocal = ScreenToCanvasLocal(eventData.position);
            Vector2 tipLocal = canvasLocal + scissorsController.tipOffset;
            manager?.OnCutDrag(tipLocal);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDragging) return;
            UnityEngine.Debug.Log("[WorkbenchHandler] OnPointerUp called");
            isDragging = false;
            if (scissorsController != null) scissorsController.isCutting = false;
            manager?.OnCutEnd();
        }

        private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
        {
            // ScreenSpaceOverlay 模式 + referenceResolution 1920x1080 + match=0.5
            // Canvas 中心在屏幕中心
            if (canvas == null) return screenPos;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return screenPos;

            float refW = scaler.referenceResolution.x;
            float refH = scaler.referenceResolution.y;
            float scaleX = Screen.width / refW;
            float scaleY = Screen.height / refH;
            float scale = Mathf.Min(scaleX, scaleY);

            var canvasCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return (screenPos - canvasCenter) / scale;
        }
    }
}
