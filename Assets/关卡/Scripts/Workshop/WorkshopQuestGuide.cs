using System.Collections;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class WorkshopQuestGuide : MonoBehaviour
{
    private static readonly Vector2 CardCenter = new Vector2(960, -540);
    private static readonly Rect NoticeButtonBounds = new Rect(1714, 36, 170, 58);
    private static readonly Vector2 NoticeButtonCenter = new Vector2(NoticeButtonBounds.center.x, -NoticeButtonBounds.center.y);
    private static readonly Color Shade = WorkshopNoticeBoard.ModalShade;
    private static readonly Color MissingTint = new Color(0.76f, 0.73f, 0.68f, 0.70f);

    private static WorkshopQuestGuide current;
    private static int lastDismissalFrame = -1;
    internal static bool IsOpen => current != null && current.card != null && current.card.gameObject.activeSelf;
    private static bool IsFeeding => FeedOverlayManager.Instance != null && FeedOverlayManager.Instance.IsActive;
    internal static bool BlocksWorldInput => WorkshopMinigameMenu.BlocksGameplayInput || IsOpen || lastDismissalFrame == Time.frameCount ||
        WorkshopDialoguePresenter.BlocksWorldInput || MiniGameCompletionAnimation.IsPlaying ||
        AutumnOfferingView.BlocksWorldInput || WorkshopObservationView.BlocksWorldInput ||
        WorkshopCraftRecordView.BlocksWorldInput || WorkshopHeritageBrief.IsOpen || SpringSilkwormWashOverlay.IsOpen || IsFeeding;

    internal static bool IsWorldOverlayActive
    {
        get
        {
            return MiniGameCompletionAnimation.IsPlaying || MiniGameOverlay.IsActive || HarvestOverlayManager.IsHarvestActive ||
                WinterDyeingOverlay.IsActive || WinterGrindingOverlay.IsActive || SpringSilkwormWashOverlay.IsOpen || IsFeeding ||
                WorkshopKnowledgeView.IsOpen ||
                WorkshopNarrativeView.IsOpen || SimplePopup.IsOpen || WorkshopDialoguePresenter.IsOpen ||
                AutumnOfferingView.IsOpen || WorkshopObservationView.IsOpen ||
                WorkshopCraftRecordView.IsOpen || WorkshopHeritageBrief.IsOpen;
        }
    }

    private RectTransform card;
    private RectTransform itemHud;
    private GameObject noticeEntry;
    private UnityEngine.UI.Image shield;
    private CanvasGroup cardGroup;
    private UnityEngine.UI.Button acknowledge;
    private GoalItem[] goalItems;
    private WorkshopSeason season;
    private bool needsOpening;
    private bool collapsing;

    private sealed class GoalItem
    {
        internal WorkshopItem Item;
        internal UnityEngine.UI.RawImage Icon;
        internal GameObject CollectedMark;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        current = null;
        lastDismissalFrame = -1;
    }

    internal static void Attach(WorkshopSeason season)
    {
        var frame = WorkshopUi.NewCanvas("Level Quest Guide", 510);
        current = frame.gameObject.AddComponent<WorkshopQuestGuide>();
        current.Build(frame, season);
    }

    private string ReadStep => "quest.guide." + season;

    private void Build(RectTransform frame, WorkshopSeason level)
    {
        season = level;
        needsOpening = !WorkshopProgress.IsComplete(season) && !WorkshopProgress.HasStep(ReadStep);

        shield = WorkshopUi.Fill(frame.parent, "Quest Modal Shield", new Rect(), Shade);
        var mask = shield.rectTransform;
        mask.anchorMin = Vector2.zero;
        mask.anchorMax = Vector2.one;
        mask.offsetMin = mask.offsetMax = Vector2.zero;
        mask.SetAsFirstSibling();
        shield.raycastTarget = true;
        shield.gameObject.SetActive(false);

        BuildItemHud(frame);
        var boardButton = WorkshopUi.PixelButton(frame, "Notice Board Entry", NoticeButtonBounds,
            "公告栏", OpenCard, primary: true, size: 29);
        noticeEntry = boardButton.transform.parent.gameObject;
        BuildOpeningCard(frame);
        card.gameObject.SetActive(false);
        RefreshProgress();
        WorkshopProgress.Changed += RefreshProgress;
        if (needsOpening && !IsWorldOverlayActive) OpenCard();
        SetHudVisible(!IsOpen && !IsWorldOverlayActive);
    }

    private void BuildItemHud(RectTransform frame)
    {
        const float width = 124f;
        const float height = 110f;
        const float gap = 9f;
        var items = WorkshopProgress.Items(season);
        itemHud = WorkshopUi.Rect(frame, "Season Goal Items",
            new Rect(36f, 36f, items.Length * (width + gap) - gap, height));
        goalItems = new GoalItem[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            WorkshopItem item = items[i];
            var tile = WorkshopUi.PixelCard(itemHud, item.ToString(), new Rect(i * (width + gap), 0, width, height),
                WorkshopUi.Paper, WorkshopUi.Bronze);
            var icon = WorkshopUi.Rect(tile, "Item Icon", new Rect(28, 4, 68, 68))
                .gameObject.AddComponent<UnityEngine.UI.RawImage>();
            icon.texture = WorkshopArt.TaskIconAtlas;
            icon.uvRect = WorkshopArt.TaskIconUV(item);
            icon.raycastTarget = false;
            icon.enabled = icon.texture != null;
            WorkshopUi.Label(tile, "Item Name", new Rect(5, 73, width - 10, 29), WorkshopProgress.ItemName(item),
                26, WorkshopUi.Ink, TextAnchor.MiddleCenter);

            var check = WorkshopUi.PixelCard(tile, "Collected Mark", new Rect(94, 8, 22, 22),
                WorkshopUi.Green, WorkshopUi.Bronze);
            var checkStart = WorkshopUi.Fill(check, "Check Start", new Rect(6, 11, 6, 3), WorkshopUi.Cream);
            checkStart.rectTransform.localEulerAngles = new Vector3(0, 0, -45);
            var checkEnd = WorkshopUi.Fill(check, "Check End", new Rect(9, 15, 11, 3), WorkshopUi.Cream);
            checkEnd.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            goalItems[i] = new GoalItem { Item = item, Icon = icon, CollectedMark = check.gameObject };
        }
    }

    private void BuildOpeningCard(RectTransform frame)
    {
        card = WorkshopUi.Rect(frame, "Quest Opening Card", new Rect(0, 0, 1920, 1080));
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = CardCenter;
        cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        var noticeBoard = WorkshopNoticeBoard.Build(card, season, DismissCard);
        acknowledge = noticeBoard.CloseAction;
    }

    private void RefreshProgress()
    {
        foreach (var goal in goalItems)
        {
            bool collected = WorkshopProgress.Has(goal.Item);
            goal.Icon.color = collected ? Color.white : MissingTint;
            goal.CollectedMark.SetActive(collected);
        }
    }

    private void SetHudVisible(bool visible)
    {
        if (itemHud.gameObject.activeSelf != visible) itemHud.gameObject.SetActive(visible);
        if (noticeEntry.activeSelf != visible) noticeEntry.SetActive(visible);
    }

    private void Update()
    {
        bool blocked = IsWorldOverlayActive;
        if (needsOpening && !blocked && !IsOpen) OpenCard();
        SetHudVisible(!IsOpen && !blocked);
        if (IsOpen && !collapsing && !blocked && Input.GetKeyDown(KeyCode.Escape)) DismissCard();
    }

    private void OpenCard()
    {
        if (card.gameObject.activeSelf || IsWorldOverlayActive) return;
        card.anchoredPosition = CardCenter;
        card.localScale = Vector3.one;
        cardGroup.alpha = 1f;
        cardGroup.interactable = true;
        card.gameObject.SetActive(true);
        shield.gameObject.SetActive(true);
        shield.color = Shade;
        SetHudVisible(false);
        acknowledge.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(acknowledge.gameObject);
    }

    private void DismissCard()
    {
        if (!card.gameObject.activeSelf || collapsing) return;
        needsOpening = false;
        WorkshopProgress.MarkStep(ReadStep);
        collapsing = true;
        cardGroup.interactable = false;
        acknowledge.interactable = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(Collapse());
    }

    private IEnumerator Collapse()
    {
        const float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            card.anchoredPosition = Vector2.Lerp(CardCenter, NoticeButtonCenter, blend);
            card.localScale = Vector3.one * Mathf.Lerp(1f, 0.17f, blend);
            cardGroup.alpha = 1f - blend;
            shield.color = new Color(Shade.r, Shade.g, Shade.b, Shade.a * (1f - blend));
            yield return null;
        }

        card.gameObject.SetActive(false);
        shield.gameObject.SetActive(false);
        lastDismissalFrame = Time.frameCount;
        collapsing = false;
        SetHudVisible(!IsWorldOverlayActive);
    }

    private void OnDestroy()
    {
        WorkshopProgress.Changed -= RefreshProgress;
        if (current == this) current = null;
    }
}

/// <summary>角色移动时，在角色身旁指向当前任务目标的世界箭头。</summary>
internal sealed class WorkshopQuestArrow : MonoBehaviour
{
    private const float TargetRefreshInterval = 0.12f;
    private static Sprite arrowSprite;
    private static Sprite pinSprite;

    private Transform player;
    private WorkshopSeason? season;
    private System.Func<Transform> mainHallTarget;
    private System.Func<bool> mainHallInputBlocked;
    private SpriteRenderer arrowRenderer;
    private SpriteRenderer highlightRenderer;
    private SpriteRenderer pinRenderer;
    private Transform highlightedTarget;
    private Transform target;
    private Camera cachedCamera;
    private float pinTopY;
    private float refreshTimer;
    private bool wasVisible;

    internal static void Attach(WorkshopSeason level)
    {
        var playerObject = GameObject.Find("Player");
        if (playerObject == null) return;
        Attach(playerObject.transform).season = level;
    }

    internal static void AttachMainHall(Transform playerTransform, System.Func<Transform> targetProvider,
        System.Func<bool> inputBlocked)
    {
        if (playerTransform == null) return;
        var arrow = Attach(playerTransform);
        arrow.mainHallTarget = targetProvider;
        arrow.mainHallInputBlocked = inputBlocked;
    }

    private static WorkshopQuestArrow Attach(Transform playerTransform)
    {
        var arrow = playerTransform.GetComponent<WorkshopQuestArrow>();
        if (arrow == null) arrow = playerTransform.gameObject.AddComponent<WorkshopQuestArrow>();
        arrow.player = playerTransform;
        if (arrow.arrowRenderer == null) arrow.BuildVisual();
        return arrow;
    }

    private void BuildVisual()
    {
        var visual = new GameObject("Current Quest Arrow", typeof(SpriteRenderer));
        // 不挂到玩家子物体下，避免随玩家跳跃/移动而抖动；位置每帧由本组件按屏幕坐标设定。
        visual.transform.SetParent(null, false);
        visual.transform.localScale = Vector3.one * 0.82f;
        arrowRenderer = visual.GetComponent<SpriteRenderer>();
        arrowRenderer.sprite = GetArrowSprite();
        arrowRenderer.sortingOrder = 75;
        arrowRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (player == null || arrowRenderer == null)
        {
            Hide();
            return;
        }

        bool blocked = WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive ||
            (mainHallInputBlocked != null && mainHallInputBlocked());
        if (Time.timeScale <= 0f || blocked)
        {
            Hide();
            return;
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            target = mainHallTarget != null ? mainHallTarget() : ResolveSeasonTarget();
            refreshTimer = TargetRefreshInterval;
        }

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        Camera cam = GetCamera();
        if (cam == null)
        {
            Hide();
            return;
        }

        // 目标出现在屏幕内 → 隐藏屏幕两侧箭头，改由目标边缘高亮 + 上方倒三角定位符指引。
        Vector3 targetViewport = cam.WorldToViewportPoint(target.position);
        bool targetOnScreen = targetViewport.x >= 0.02f && targetViewport.x <= 0.98f &&
            targetViewport.y >= 0.02f && targetViewport.y <= 0.98f;
        if (targetOnScreen)
        {
            arrowRenderer.enabled = false;
            wasVisible = false;
            UpdateTargetMarkers(true);
            return;
        }

        HideMarkers();

        float direction = Mathf.Sign(target.position.x - player.position.x);
        // 箭头固定在屏幕左右两侧，高度与人物对齐：目标在右 → 右缘箭头朝右；目标在左 → 左缘箭头朝左。
        float viewportX = direction > 0f ? 0.90f : 0.10f;
        float viewportY = 1f / 3f; // 固定在屏幕靠下方 1/3 处
        Vector3 desired = cam.ViewportToWorldPoint(new Vector3(viewportX, viewportY, -cam.transform.position.z));
        arrowRenderer.transform.position = wasVisible
            ? Vector3.Lerp(arrowRenderer.transform.position, desired, 1f - Mathf.Exp(-18f * Time.deltaTime))
            : desired;
        arrowRenderer.transform.rotation = Quaternion.Euler(0f, 0f, direction > 0f ? 0f : 180f);

        // 光标提示：轻微脉动，明确“该往哪走”。
        float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
        arrowRenderer.transform.localScale = Vector3.one * 0.82f * pulse;
        arrowRenderer.color = new Color(1f, 1f, 1f, 0.85f + Mathf.Sin(Time.time * 6f) * 0.15f);
        arrowRenderer.enabled = true;
        wasVisible = true;
    }

    private void Hide()
    {
        if (arrowRenderer != null) arrowRenderer.enabled = false;
        wasVisible = false;
        HideMarkers();
    }

    private void OnDisable() => Hide();

    private void OnDestroy()
    {
        if (arrowRenderer != null && arrowRenderer.gameObject != null)
            Destroy(arrowRenderer.gameObject);
        if (highlightRenderer != null && highlightRenderer.gameObject != null)
            Destroy(highlightRenderer.gameObject);
        if (pinRenderer != null && pinRenderer.gameObject != null)
            Destroy(pinRenderer.gameObject);
    }

    private void UpdateTargetMarkers(bool show)
    {
        if (target == null)
        {
            ClearMarkers();
            return;
        }

        if (highlightedTarget != target)
        {
            ClearMarkers();
            highlightedTarget = target;
            highlightRenderer = BuildHighlight(target);
            pinRenderer = BuildPin(target, out pinTopY);
        }

        float pulse = 0.5f + Mathf.Sin(Time.time * 5f) * 0.5f;

        if (highlightRenderer != null)
        {
            Color color = highlightRenderer.color;
            color.a = show ? (0.28f + pulse * 0.22f) : 0f;
            highlightRenderer.color = color;
            highlightRenderer.enabled = show;
        }

        if (pinRenderer != null)
        {
            pinRenderer.enabled = show;
            if (show)
            {
                // 用世界坐标固定在目标正上方，倒三角大小统一（不受目标缩放影响）。
                float bob = Mathf.Sin(Time.time * 4f) * 0.10f;
                pinRenderer.transform.position = new Vector3(target.position.x, pinTopY + 0.35f + bob, target.position.z);
                Color color = pinRenderer.color;
                color.a = 0.70f + pulse * 0.30f;
                pinRenderer.color = color;
            }
        }
    }

    private void HideMarkers()
    {
        if (highlightRenderer != null) highlightRenderer.enabled = false;
        if (pinRenderer != null) pinRenderer.enabled = false;
    }

    private void ClearMarkers()
    {
        if (highlightRenderer != null)
        {
            if (highlightRenderer.gameObject != null) Destroy(highlightRenderer.gameObject);
            highlightRenderer = null;
        }
        if (pinRenderer != null)
        {
            if (pinRenderer.gameObject != null) Destroy(pinRenderer.gameObject);
            pinRenderer = null;
        }
        highlightedTarget = null;
    }

    private SpriteRenderer BuildHighlight(Transform targetTransform)
    {
        var source = GetMainSpriteRenderer(targetTransform);
        if (source == null) return null;

        var child = new GameObject("Quest Target Highlight", typeof(SpriteRenderer));
        child.transform.SetParent(targetTransform, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one * 1.06f;

        var renderer = child.GetComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.sortingOrder = source.sortingOrder - 1;
        renderer.color = new Color(1f, 0.86f, 0.42f, 0.4f);
        return renderer;
    }

    private SpriteRenderer BuildPin(Transform targetTransform, out float topY)
    {
        topY = GetTargetTop(targetTransform);

        // 独立物体，避免继承目标物体的缩放导致倒三角大小不一致。
        var pin = new GameObject("Quest Target Pin", typeof(SpriteRenderer));
        pin.transform.SetParent(null, false);
        pin.transform.localScale = Vector3.one * 0.5f;

        var renderer = pin.GetComponent<SpriteRenderer>();
        renderer.sprite = GetPinSprite();
        renderer.sortingOrder = 100;
        renderer.color = new Color(1f, 0.82f, 0.30f, 0.9f);
        return renderer;
    }

    private static float GetTargetTop(Transform targetTransform)
    {
        var source = GetMainSpriteRenderer(targetTransform);
        if (source != null) return source.bounds.max.y;
        return targetTransform.position.y + 1.5f;
    }

    private static SpriteRenderer GetMainSpriteRenderer(Transform root)
    {
        var own = root.GetComponent<SpriteRenderer>();
        if (own != null && own.sprite != null) return own;

        SpriteRenderer best = null;
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sprite == null) continue;
            if (best == null || renderer.sortingOrder > best.sortingOrder) best = renderer;
        }
        return best;
    }

    private Camera GetCamera()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        if (cachedCamera == null) cachedCamera = FindObjectOfType<Camera>();
        return cachedCamera;
    }

    private Transform ResolveSeasonTarget()
    {
        if (!season.HasValue) return null;
        var quest = WorkshopQuests.NextReady(season.Value);
        if (quest == null) return null;

        switch (quest.Id)
        {
            case "SpringBriefing":
                return FindComponent<NpcTalkButton>() ?? FindObject("待机关键帧_01");
            case "Basket":
                return FindObject("1-采桑篮1", "1-采桑篮");
            case "MulberryLeaf":
                return FindComponent<SpringPrompt>() ?? FindObject("采桑游戏触发点位", "1-桑树1");
            case "EggPaper":
                return FindObject("1-蚕种纸1", "1-蚕种纸");
            case "SilkwormEgg":
                return FindWashingBucket() ?? FindObject("1-浴蚕桶");

            case "Salt":
                return FindObject(SummerItemInteraction.SaltObjectName);
            case "Suoxu":
            case "MangzhongOrder":
                return FindComponent<SummerCocoonRackTrigger>() ?? FindObject(SummerItemInteraction.CocoonRackObjectName);
            case "Water":
                return FindObject(SummerItemInteraction.WaterObjectName);
            case "Firewood":
                return FindObject(SummerItemInteraction.WoodObjectName);
            case "Spool":
                return FindComponent<SummerCookerTrigger>() ?? FindObject(SummerItemInteraction.CookerObjectName);

            case "Shuttle":
                return FindAutumnItem(AutumnItemClick.ItemKind.Toolbox) ?? FindObject("3-工具箱");
            case "Hammer":
                return FindComponent<AutumnNpcDialogue>() ?? FindObject("待机关键帧_01");
            case "LoomRepair":
            case "RawSilkCloth":
                return FindComponent<AutumnLoomTrigger>() ?? FindObject("3-破损的织机", "3-织机");
            case "QiufenOrder":
            case "Statue":
                return FindComponent<AutumnRitualTrigger>() ?? FindObject("祭祀触发点", "AutumnRitualPoint");
            case "Offerings":
                return NextAutumnOffering();

            case "Hoe":
                return FindObject("4-挖掘染料锄头");
            case "DyeGather":
                return FindComponent<WinterDyeGardenTrigger>() ?? FindObject("染料园触点");
            case "GrindingRod":
                return FindComponent<WinterRodClickTarget>() ?? FindObject("4-研磨棒");
            case "Grind":
            case "LidongOrder":
                return FindComponent<WinterGrindingClickTarget>() ?? FindObject("4-染料空");
            case "DyedCloth":
                return FindComponent<WinterDyeVatClickTarget>() ?? FindObject("4-染料空缸");
            case "CutPieces":
                return FindComponent<WinterClothCuttingTrigger>() ?? FindObject("4-裁剪台");
            case "Needle":
                return FindComponent<WinterNeedleClickTarget>() ?? FindObject("4-裁衣工具针");
            default:
                return null;
        }
    }

    private Transform NextAutumnOffering()
    {
        if (!WorkshopProgress.HasStep("autumn.wine"))
            return FindAutumnItem(AutumnItemClick.ItemKind.Wine) ??
                FindObject("3-酒", "3-酒 (1)", "3-酒 (2)", "3-酒 (3)");
        if (!WorkshopProgress.HasStep("autumn.banana"))
            return FindAutumnItem(AutumnItemClick.ItemKind.Banana) ?? FindObject("3-香蕉果篮");
        if (!WorkshopProgress.HasStep("autumn.cake"))
            return FindComponent<AutumnNpcDialogue>() ?? FindObject("待机关键帧_01");
        return null;
    }

    private Transform FindAutumnItem(AutumnItemClick.ItemKind kind)
    {
        AutumnItemClick nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (var item in FindObjectsOfType<AutumnItemClick>())
        {
            if (item.kind != kind || !item.gameObject.activeInHierarchy) continue;
            float distance = (item.transform.position - player.position).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearest = item;
            nearestDistance = distance;
        }
        return nearest != null ? nearest.transform : null;
    }

    private static Transform FindWashingBucket()
    {
        foreach (var station in FindObjectsOfType<SilkwormWashingController>())
            if (station.role == SilkwormWashingController.RoleType.Bucket) return station.transform;
        return null;
    }

    private static Transform FindComponent<T>() where T : Component
    {
        var component = FindObjectOfType<T>();
        return component != null ? component.transform : null;
    }

    private static Transform FindObject(params string[] names)
    {
        foreach (string name in names)
        {
            var found = GameObject.Find(name);
            if (found != null) return found.transform;
        }
        return null;
    }

    private static Sprite GetArrowSprite()
    {
        if (arrowSprite != null) return arrowSprite;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Current Quest Arrow Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var pixels = new Color32[size * size];
        var fill = new Color32(255, 207, 92, 255);
        var outline = new Color32(76, 55, 31, 255);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (!InsideArrow(x, y)) continue;
                bool edge = false;
                for (int offsetY = -2; offsetY <= 2 && !edge; offsetY++)
                    for (int offsetX = -2; offsetX <= 2; offsetX++)
                        if (!InsideArrow(x + offsetX, y + offsetY)) { edge = true; break; }
                pixels[y * size + x] = edge ? outline : fill;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        arrowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            size, 0, SpriteMeshType.FullRect);
        arrowSprite.name = "Current Quest Arrow";
        arrowSprite.hideFlags = HideFlags.HideAndDontSave;
        return arrowSprite;
    }

    private static bool InsideArrow(int x, int y)
    {
        bool shaft = x >= 5 && x <= 37 && y >= 26 && y <= 38;
        bool head = x >= 32 && x <= 58 && Mathf.Abs(y - 32) <= (58 - x) * 0.62f;
        return shaft || head;
    }

    private static Sprite GetPinSprite()
    {
        if (pinSprite != null) return pinSprite;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Quest Target Pin Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var pixels = new Color32[size * size];
        var fill = new Color32(255, 207, 92, 255);
        var outline = new Color32(76, 55, 31, 255);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (!InsideDownTriangle(x, y)) continue;
                bool edge = false;
                for (int offsetY = -2; offsetY <= 2 && !edge; offsetY++)
                    for (int offsetX = -2; offsetX <= 2; offsetX++)
                        if (!InsideDownTriangle(x + offsetX, y + offsetY)) { edge = true; break; }
                pixels[y * size + x] = edge ? outline : fill;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        pinSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            size, 0, SpriteMeshType.FullRect);
        pinSprite.name = "Quest Target Pin";
        pinSprite.hideFlags = HideFlags.HideAndDontSave;
        return pinSprite;
    }

    private static bool InsideDownTriangle(int x, int y)
    {
        // 倒三角：尖朝下，顶点 (32,12)，底边 y=54，半宽 20。
        if (y < 12 || y > 54) return false;
        float halfWidth = (y - 12) * (20f / 42f);
        return Mathf.Abs(x - 32) <= halfWidth;
    }
}
