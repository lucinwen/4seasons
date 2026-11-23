using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Qingming.LevelBackground;

namespace JiangnanDye.ClothCutting
{
    /// <summary>
    /// 裁衣小游戏主管理器。三片衣片：前衣片、后衣片、袖片。
    /// 玩家激活剪刀按钮后在识别区 Image 内滑动即可匀速走进度。
    /// </summary>
    public class ClothCuttingGameManager : MonoBehaviour
    {
        public enum PieceId { Front, Back, Sleeve }

        private const float SnipInterval = 0.24f;

        [Header("工作台")]
        public RectTransform workbenchRect;
        public Vector2 workbenchSize = new Vector2(845f, 543f);

        [Header("裁剪路径（供渲染已裁剪金色实线用）")]
        public CuttingPath frontPath = new CuttingPath();
        public CuttingPath backPath = new CuttingPath();
        public CuttingPath sleevePath = new CuttingPath();

        [Header("裁剪识别区（由 Builder 自动绑定）")]
        public RectTransform frontAreaRect;
        public RectTransform backAreaRect;
        public RectTransform sleeveAreaRect;

        [Header("衣片成品 Sprite")]
        public Sprite frontSprite;
        public Sprite backSprite;
        public Sprite sleeveSprite;

        [Header("组件引用")]
        public ScissorsController scissors;
        public ClothCuttingUI ui;

        [Header("裁剪速度：每秒走完一片衣片的进度")]
        [Range(0.1f, 3f)] public float progressPerSecond = 1.5f;

        [Header("字体（由 Builder 自动注入）")]
        public Font pixelFont;

        [Header("事件")]
        public UnityEvent OnCuttingCompleted;
        public UnityEvent OnReset;
        public UnityEvent OnClose;

        private int doneCount;
        private bool gameOver;
        private float nextSnipTime;
        public PieceId? activePiece;

        private CutPieceView frontView, backView, sleeveView;
        private Color _clothTint = Color.white;
        private Material _clothMaterial;

        public bool IsGameOver => gameOver;
        public int DoneCount => doneCount;
        internal bool HasRunProgress => frontPath.progress > 0f || backPath.progress > 0f || sleevePath.progress > 0f;

        private void Awake()
        {
            frontPath.pieceId = "Front";
            backPath.pieceId = "Back";
            sleevePath.pieceId = "Sleeve";
        }

        private void Start()
        {
            StartGame();
            if (MiniGameOverlay.IsActive && WorkshopProgress.Has(WorkshopItem.DyedCloth) &&
                !WorkshopProgress.Has(WorkshopItem.CutPieces))
                WorkshopDialoguePresenter.PlayOnce("dialogue.winter.cutting", WorkshopDialogueScripts.WinterCuttingIntro);
        }

        public void StartGame()
        {
            InitializePaths();
            _clothTint = ResolveClothTint();
            _clothMaterial = _clothTint != Color.white ? Resources.Load<Material>("WorkshopClothDye") : null;
            doneCount = 0;
            gameOver = false;
            activePiece = null;
            if (scissors != null) scissors.BindManager(this);
            if (ui != null) ui.UpdateProgress(0f, 0);
            ClearPieceViews();
            CreatePieceViews();
        }

        /// <summary>根据染色时选定的颜色，给裁出的衣片染上对应颜色。</summary>
        private static Color ResolveClothTint()
        {
            string name = WinterLevelState.ChosenClothColor;
            if (string.IsNullOrEmpty(name)) return Color.white;

            var config = Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
            if (config == null) return Color.white;

            for (int i = 0; i < 5; i++)
            {
                var color = (JiangnanDye.DyeColor)i;
                if (JiangnanDye.DyeColorUtil.ChineseName(color) + "布" == name)
                    return config.GetClothTint(color);
            }
            return Color.white;
        }

        private void InitializePaths()
        {
            frontPath.Initialize();
            backPath.Initialize();
            sleevePath.Initialize();
        }

        private void ClearPieceViews()
        {
            DestroyPieceView(frontView); frontView = null;
            DestroyPieceView(backView); backView = null;
            DestroyPieceView(sleeveView); sleeveView = null;
        }
        private static void DestroyPieceView(CutPieceView v) { if (v != null && v.gameObject != null) Destroy(v.gameObject); }

        private void CreatePieceViews()
        {
            Transform parent = workbenchRect != null ? workbenchRect.parent : transform;
            frontView = CreateView("Piece_Front", parent, PieceSlot.Front, frontSprite, false);
            backView = CreateView("Piece_Back", parent, PieceSlot.Back, backSprite, false);
            sleeveView = CreateView("Piece_Sleeve", parent, PieceSlot.Sleeve, sleeveSprite, false);
        }

        private CutPieceView CreateView(string name, Transform parent, PieceSlot slot, Sprite sprite, bool flipX)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            CutPieceView view = go.AddComponent<CutPieceView>();
            view.kind = (CutPieceView.PieceKind)System.Enum.Parse(typeof(CutPieceView.PieceKind), slot.ToString());
            view.pieceSprite = sprite;
            view.flipX = flipX;
            view.Init();
            if (view.pieceImage != null)
            {
                view.pieceImage.color = _clothTint;
                if (_clothMaterial != null) view.pieceImage.material = _clothMaterial;
            }
            view.transform.SetAsLastSibling();
            return view;
        }

        public bool IsPointInWorkArea(Vector2 p)
        {
            if (workbenchRect == null) return false;
            Vector2 wbLocal = CanvasToWorkbenchLocal(p);
            float w = workbenchRect.rect.width;
            float h = workbenchRect.rect.height;
            return wbLocal.x >= 0 && wbLocal.x <= w && wbLocal.y >= 0 && wbLocal.y <= h;
        }

        public Vector2 CanvasToWorkbenchLocal(Vector2 canvasLocalPos)
        {
            if (workbenchRect == null) return canvasLocalPos;
            Vector2 wbAnchored = canvasLocalPos - workbenchRect.anchoredPosition;
            return wbAnchored + new Vector2(workbenchRect.rect.width * 0.5f, workbenchRect.rect.height * 0.5f);
        }

        public void OnCutStart(Vector2 tipCanvasLocal)
        {
            if (gameOver) return;
            activePiece = FindPieceContaining(CanvasToWorkbenchLocal(tipCanvasLocal));
        }

        private float diagLogTimer;

        public void OnCutDrag(Vector2 tipCanvasLocal)
        {
            if (gameOver) return;
            Vector2 tipLocal = CanvasToWorkbenchLocal(tipCanvasLocal);

            // 每秒打印一次诊断（避免刷屏）
            diagLogTimer += Time.deltaTime;
            if (diagLogTimer >= 1f)
            {
                diagLogTimer = 0f;
                UnityEngine.Debug.Log($"[OnCutDrag] tipCanvas={tipCanvasLocal} tipRect={tipLocal} activePiece={activePiece}");
                UnityEngine.Debug.Log($"  wbRect=[0,{workbenchRect.rect.width:F0}]x[0,{workbenchRect.rect.height:F0}] InWorkArea={IsPointInWorkArea(tipCanvasLocal)}");
                foreach (var p in new PieceId[] { PieceId.Front, PieceId.Back, PieceId.Sleeve })
                {
                    var dArea = GetAreaRect(p);
                    bool cont = dArea != null && AreaContains(dArea, tipLocal);
                    var dPath = GetPath(p);
                    UnityEngine.Debug.Log($"  {p}: areaContain={cont} progress={dPath.progress:F2} completed={dPath.completed}");
                }
            }

            if (activePiece == null)
            {
                activePiece = FindPieceContaining(tipLocal);
                if (activePiece == null) return;
            }

            CuttingPath path = GetPath(activePiece.Value);
            if (path == null) { activePiece = null; return; }

            if (path.completed)
            {
                activePiece = FindPieceContaining(tipLocal);
                if (activePiece == null) return;
                path = GetPath(activePiece.Value);
                if (path == null || path.completed) return;
            }

            RectTransform area = GetAreaRect(activePiece.Value);
            if (area == null) return;

            // 去掉 AreaContains 硬检查：已确定 activePiece 后，直接推进进度
            // （FindPieceContaining 已经验证过包含）

            float previousProgress = path.progress;
            path.progress = Mathf.Clamp01(previousProgress + progressPerSecond * Time.deltaTime);
            if (path.progress > previousProgress && Time.unscaledTime >= nextSnipTime)
            {
                nextSnipTime = Time.unscaledTime + SnipInterval;
                WorkshopAudioFeedback.Snip();
            }

            if (path.progress >= 0.999f)
            {
                path.progress = 1f;
                path.completed = true;
                HandlePieceCompleted(activePiece.Value, tipCanvasLocal);
            }

            UpdateUI();
        }

        public void OnCutEnd() { }

        private PieceId? FindPieceContaining(Vector2 tipLocal)
        {
            if (!frontPath.completed && AreaContains(frontAreaRect, tipLocal)) return PieceId.Front;
            if (!backPath.completed && AreaContains(backAreaRect, tipLocal)) return PieceId.Back;
            if (!sleevePath.completed && AreaContains(sleeveAreaRect, tipLocal)) return PieceId.Sleeve;
            return null;
        }

        private bool AreaContains(RectTransform area, Vector2 tipLocal)
        {
            if (area == null || workbenchRect == null) return false;
            Vector2 areaCenterRect = area.anchoredPosition
                + new Vector2(workbenchRect.rect.width * 0.5f, workbenchRect.rect.height * 0.5f);
            Vector2 half = area.rect.size * 0.5f;
            if (area.localScale.x < 0) half.x = -half.x;

            float minX = areaCenterRect.x - Mathf.Abs(half.x);
            float maxX = areaCenterRect.x + Mathf.Abs(half.x);
            float minY = areaCenterRect.y - Mathf.Abs(half.y);
            float maxY = areaCenterRect.y + Mathf.Abs(half.y);

            return tipLocal.x >= minX && tipLocal.x <= maxX
                && tipLocal.y >= minY && tipLocal.y <= maxY;
        }

        private RectTransform GetAreaRect(PieceId id)
        {
            switch (id)
            {
                case PieceId.Front: return frontAreaRect;
                case PieceId.Back: return backAreaRect;
                case PieceId.Sleeve: return sleeveAreaRect;
            }
            return null;
        }

        private CuttingPath GetPath(PieceId id)
        {
            switch (id)
            {
                case PieceId.Front: return frontPath;
                case PieceId.Back: return backPath;
                case PieceId.Sleeve: return sleevePath;
            }
            return null;
        }

        private void HandlePieceCompleted(PieceId id, Vector2 tipCanvasLocal)
        {
            doneCount++;
            CutPieceView view = GetView(id);
            if (view != null)
            {
                // 1. 起点：工作台位置 → Canvas 中心坐标系
                Vector2 workbenchLocal = CanvasToWorkbenchLocal(tipCanvasLocal);
                Vector2 viewCanvasPos = workbenchLocal - new Vector2(workbenchRect.rect.width * 0.5f, workbenchRect.rect.height * 0.5f)
                    + workbenchRect.anchoredPosition;
                view.PlaySeparateAnim(viewCanvasPos);

                // 2. 终点：槽位中心 → Canvas 中心坐标系
                PieceSlot slot = IdToSlot(id);
                RectTransform slotRt = ui != null ? ui.GetSlot(slot) : null;
                if (slotRt != null)
                {
                    // Piece 的 parent 是 Canvas，slotRt 的 parent 链是 Canvas → Collection → Slot
                    // 用 RectTransformUtility 把 slot 的世界坐标转换成 Canvas 下的 anchoredPosition
                    var viewParentRt = view.GetComponent<RectTransform>().parent as RectTransform;
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, slotRt.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        viewParentRt, screenPos, null, out Vector2 targetPos);
                    UnityEngine.Debug.Log($"[HandlePieceCompleted] {id}: start={viewCanvasPos} target={targetPos}");
                    view.MoveToCollection(targetPos);
                }
            }

            activePiece = null;
            UpdateUI();
            CheckGameOver();
        }

        private CutPieceView GetView(PieceId id)
        {
            switch (id)
            {
                case PieceId.Front: return frontView;
                case PieceId.Back: return backView;
                case PieceId.Sleeve: return sleeveView;
            }
            return null;
        }

        private static PieceSlot IdToSlot(PieceId id)
        {
            switch (id)
            {
                case PieceId.Front: return PieceSlot.Front;
                case PieceId.Back: return PieceSlot.Back;
                case PieceId.Sleeve: return PieceSlot.Sleeve;
            }
            return PieceSlot.Front;
        }

        private void UpdateUI()
        {
            if (ui == null) return;
            float total = (frontPath.progress + backPath.progress + sleevePath.progress) / 3f;
            ui.UpdateProgress(total, doneCount);
        }

        private void CheckGameOver()
        {
            if (frontPath.completed && backPath.completed && sleevePath.completed)
            {
                gameOver = true;
                if (scissors != null) scissors.Hide();

                MiniGameCompletionAnimation.PlayThen(MiniGameCompletionId.ClothesCutting, ShowCuttingCompletion);
            }
        }

        private void ShowCuttingCompletion()
        {
            // 裁衣结束：弹出获得衣片道具的弹窗，确认后关闭叠加层
            SimplePopup.ShowItem(WorkshopItem.CutPieces, "获得【衣片】× 3",
                () =>
                {
                    if (MiniGameOverlay.IsActive && WorkshopProgress.Has(WorkshopItem.DyedCloth))
                        WorkshopProgress.Award(WorkshopItem.CutPieces);
                    OnClose?.Invoke();
                }, fallbackIcon: frontSprite, fallbackTint: _clothTint, fallbackMaterial: _clothMaterial);

            OnCuttingCompleted?.Invoke();
        }

        public void ResetGame()
        {
            frontPath.Reset();
            backPath.Reset();
            sleevePath.Reset();
            doneCount = 0;
            gameOver = false;
            activePiece = null;
            ClearPieceViews();
            CreatePieceViews();
            if (scissors != null)
            {
                scissors.Show();
                scissors.cuttingMode = false;
            }
            UpdateUI();
            OnReset?.Invoke();
        }

        public void CloseGame()
        {
            gameOver = true;
            OnClose?.Invoke();
        }
    }
}
