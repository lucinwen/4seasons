using System.Collections.Generic;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds a playable side-scrolling main hall from the supplied panoramic artwork.
/// The player walks between the visible workshop buildings and interacts from close range.
/// </summary>
public sealed class WorkshopMainHallWorld : MonoBehaviour
{
    private const float HallHalfWidth = 19.2f;
    private const float LeftWalkLimit = -12.25f;
    private const float RightWalkLimit = 18.15f;
    private const float CameraLookAhead = 5.5f;
    private const float PlayerSpriteHeight = 2.08f;
    private const float PlayerColliderHeight = 1.58f;
    private const float DestinationMarkerHeight = 0.36f;
    private const float SignPixelsPerUnit = 125f;
    private const float SignCanvasHalfHeight = 5.12f;
    private static readonly Color Gold = new Color32(225, 193, 133, 255);
    private static readonly Color ProgressTrack = new Color32(202, 191, 160, 255);
    private static readonly Color SignPine = new Color32(25, 51, 45, 236);
    private static readonly Color[] SeasonAccents =
    {
        new Color32(64, 114, 75, 255),
        new Color32(47, 109, 108, 255),
        new Color32(157, 85, 50, 255),
        new Color32(87, 105, 151, 255)
    };
    // Contact line through the dock, paving and center of the painted stone bridge deck.
    private static readonly Vector2[] WalkwaySurface =
    {
        new Vector2(-13f, -3.57f), new Vector2(-11.35f, -3.73f), new Vector2(-9.95f, -3.87f),
        new Vector2(-4.45f, -3.87f), new Vector2(-4f, -3.72f), new Vector2(-3.60f, -3.51f),
        new Vector2(-3.10f, -3.28f), new Vector2(-2.60f, -3.13f), new Vector2(-2.10f, -3.08f),
        new Vector2(-1.60f, -3.11f), new Vector2(-1.10f, -3.20f), new Vector2(-0.60f, -3.35f),
        new Vector2(-0.10f, -3.55f), new Vector2(0.35f, -3.74f), new Vector2(0.75f, -3.91f),
        new Vector2(1.40f, -3.99f), new Vector2(HallHalfWidth, -3.99f)
    };

    private static WorkshopMainHallWorld active;
    private static float lastPlayerX = -10.6f;

    private readonly Dictionary<WorkshopHallInteractable, RectTransform> destinationSigns =
        new Dictionary<WorkshopHallInteractable, RectTransform>();
    private readonly Dictionary<WorkshopItem, UnityEngine.UI.Image> materialMarks =
        new Dictionary<WorkshopItem, UnityEngine.UI.Image>();
    private readonly WorkshopHallInteractable[] seasonDestinations = new WorkshopHallInteractable[SeasonAccents.Length];
    private Sprite destinationMarkerArtwork;
    private WorkshopHallInteractable storageDestination;
    private WorkshopHallInteractable wardrobeDestination;
    private WorkshopHallInteractable recommendedDestination;
    private WorkshopSeason focusSeason;
    private Transform player;
    private LevelPlayerController playerController;
    private Camera worldCamera;
    private RectTransform signCanvas;
    private UnityEngine.UI.Text statusText;
    private UnityEngine.UI.Text seasonText;
    private UnityEngine.UI.Text nextTaskText;
    private UnityEngine.UI.Text directionText;
    private UnityEngine.UI.Image seasonSeal;
    private RectTransform nextStopTag;
    private bool modalOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active = null;
        lastPlayerX = -10.6f;
    }

    public static void Build(WorkshopSeason season)
    {
        if (active != null) return;
        var root = new GameObject("Playable Four Seasons Main Hall");
        active = root.AddComponent<WorkshopMainHallWorld>();
        active.Initialize(season);
    }

    private void Initialize(WorkshopSeason season)
    {
        focusSeason = season;
        lastPlayerX = Mathf.Clamp(lastPlayerX, LeftWalkLimit, RightWalkLimit);
        BuildWorldBackdrop();
        BuildGroundAndBounds();
        BuildPlayer();
        BuildHud();
        BuildInteractions();
        RefreshHud();

        WorkshopProgress.Changed += RefreshHud;
        if (!WorkshopProgress.HasStep("announcement.Spring") &&
            !WorkshopProgress.HasStep("quest.guide.Spring") &&
            !WorkshopProgress.IsComplete(WorkshopSeason.Spring) &&
            !WorkshopProgress.CanSubmit(WorkshopSeason.Spring))
        {
            WorkshopDialoguePresenter.PlayOnce("dialogue.prologue", WorkshopDialogueScripts.Prologue);
        }
        if (WorkshopProgress.CanSubmit(focusSeason))
            OpenStorage(focusSeason);
    }

    private void BuildWorldBackdrop()
    {
        worldCamera = Camera.main;
        if (worldCamera == null)
        {
            var cameraGo = new GameObject("Main Hall Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            worldCamera = cameraGo.GetComponent<Camera>();
        }

        worldCamera.orthographic = true;
        worldCamera.orthographicSize = 5.1f;
        worldCamera.clearFlags = CameraClearFlags.SolidColor;
        worldCamera.backgroundColor = new Color32(25, 50, 43, 255);
        worldCamera.transform.position = new Vector3(CameraCenterX(lastPlayerX), 0f, -10f);

        Texture2D texture = Resources.Load<Texture2D>(WorkshopHallSignage.TexturePath);
        if (texture == null) return;

        float worldWidth = HallHalfWidth * 2f;
        float pixelsPerUnit = texture.width / worldWidth;
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = "Four Seasons Main Hall Panorama";

        var background = new GameObject("MainHallBackground", typeof(SpriteRenderer));
        background.transform.SetParent(transform, false);
        background.transform.position = Vector3.zero;
        var renderer = background.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -100;

        var signage = new GameObject("Main Hall Painted Signs", typeof(RectTransform), typeof(Canvas));
        var signFrame = (RectTransform)signage.transform;
        signFrame.SetParent(background.transform, false);
        signFrame.sizeDelta = WorkshopHallSignage.Panorama.size;
        signFrame.localScale = Vector3.one * (worldWidth / signFrame.sizeDelta.x);
        var canvas = signage.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = worldCamera;
        canvas.sortingOrder = -90;
        WorkshopHallSignage.Draw(signFrame, WorkshopHallSignage.Panorama, WorkshopHallSignage.FullView);
    }

    private void BuildGroundAndBounds()
    {
        var ground = new GameObject("Main Hall Walkway", typeof(PolygonCollider2D));
        var floor = ground.GetComponent<PolygonCollider2D>();
        var outline = new Vector2[WalkwaySurface.Length + 2];
        WalkwaySurface.CopyTo(outline, 0);
        outline[WalkwaySurface.Length] = new Vector2(WalkwaySurface[WalkwaySurface.Length - 1].x, -5.8f);
        outline[WalkwaySurface.Length + 1] = new Vector2(WalkwaySurface[0].x, -5.8f);
        floor.SetPath(0, outline);

        CreateWall("Main Hall Left Bound", LeftWalkLimit - 0.55f);
        CreateWall("Main Hall Right Bound", 18.55f);
    }

    private static float GroundHeightAt(float x)
    {
        for (int i = 1; i < WalkwaySurface.Length; i++)
        {
            Vector2 end = WalkwaySurface[i];
            if (x > end.x) continue;
            Vector2 start = WalkwaySurface[i - 1];
            return Mathf.Lerp(start.y, end.y, Mathf.InverseLerp(start.x, end.x, x));
        }
        return WalkwaySurface[WalkwaySurface.Length - 1].y;
    }

    private static void CreateWall(string name, float x)
    {
        var wall = new GameObject(name, typeof(BoxCollider2D));
        wall.transform.position = new Vector3(x, -1f, 0f);
        wall.GetComponent<BoxCollider2D>().size = new Vector2(0.3f, 8f);
    }

    private void BuildPlayer()
    {
        var playerGo = new GameObject("Player");
        playerGo.SetActive(false);

        var body = playerGo.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = playerGo.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Vertical;
        collider.size = new Vector2(0.55f, PlayerColliderHeight);
        collider.offset = new Vector2(0f, PlayerColliderHeight * 0.5f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(playerGo.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        var visualAnimator = visual.AddComponent<WorkshopHallPlayerVisual>();
        visualAnimator.Initialize(PlayerSpriteHeight);

        playerController = playerGo.AddComponent<LevelPlayerController>();
        playerController.visualTransform = visual.transform;
        playerController.animator = null;
        playerController.clickToMoveEnabled = true;
        playerController.moveSpeed = 4.25f;
        playerController.runMultiplier = 1.55f;
        playerController.jumpHeight = 1.75f;
        playerController.stepHeight = 0f;
        playerController.clampHorizontal = true;
        playerController.minX = LeftWalkLimit;
        playerController.maxX = RightWalkLimit;
        playerController.fallResetY = -6.2f;
        playerController.spawnPoint = new Vector3(lastPlayerX, GroundHeightAt(lastPlayerX) + 0.09f, 0f);

        playerGo.transform.position = playerController.spawnPoint;
        playerGo.SetActive(true);
        player = playerGo.transform;
        renderer.sortingOrder = 40;
    }

    private void BuildHud()
    {
        var frame = WorkshopUi.NewCanvas("Main Hall HUD", 80);
        var location = WorkshopUi.Card(frame, "Location", new Rect(36, 32, 188, 58), WorkshopUi.Paper);
        Label(location, "Hall Name", new Rect(0, 0, 188, 58), "工坊主厅", 30,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);

        var status = WorkshopUi.Card(frame, "Hall Progress", new Rect(1184, 32, 520, 128), WorkshopUi.Paper);
        seasonSeal = WorkshopUi.Fill(status, "Season Accent", new Rect(20, 13, 81, 38), SeasonAccents[0]);
        WorkshopUi.ApplyPixelShape(seasonSeal);
        seasonText = Label(seasonSeal.transform, "Season", new Rect(0, 0, 81, 38), "", 23,
            WorkshopUi.Paper, TextAnchor.MiddleCenter);
        statusText = Label(status, "Material Count", new Rect(281, 11, 219, 40), "", 25, WorkshopUi.Ink, TextAnchor.MiddleRight);
        BuildMaterialMarks(status);
        WorkshopUi.Fill(status, "Progress Divider", new Rect(20, 67, 480, 2), WorkshopUi.Line);
        nextTaskText = Label(status, "Next Task", new Rect(20, 73, 400, 43), "", 28, WorkshopUi.Ink);
        directionText = Label(status, "Offscreen Direction", new Rect(420, 73, 80, 43), "", 24,
            WorkshopUi.Green, TextAnchor.MiddleRight);

        WorkshopUi.PixelButton(frame, "Return To Title", new Rect(1722, 32, 162, 58),
            "返回封面", ReturnToTitle, false, 24);

        var controls = WorkshopUi.Card(frame, "Hall Controls", new Rect(36, 990, 714, 54),
            WorkshopUi.Ink, WorkshopUi.Bronze);
        DrawControl(controls, 16, 58, "A / D", "移动", 52);
        DrawControl(controls, 143, 62, "Shift", "跑步", 50);
        DrawControl(controls, 274, 76, "Space", "跳跃", 50);
        DrawControl(controls, 422, 30, "E", "交互", 50);
        DrawControl(controls, 525, 58, "鼠标", "点击移动", 109);
    }

    private void BuildMaterialMarks(Transform parent)
    {
        float left = 119f;
        for (int i = 0; i < SeasonAccents.Length; i++)
        {
            foreach (var item in WorkshopProgress.Items((WorkshopSeason)i))
            {
                var mark = WorkshopUi.Fill(parent, "Material " + item, new Rect(left, 29f, 9f, 8f), ProgressTrack);
                materialMarks.Add(item, mark);
                left += 12f;
            }
            left += 6f;
        }
    }

    private static void DrawControl(Transform parent, int x, int keyWidth, string key, string action, int actionWidth)
    {
        var keycap = WorkshopUi.Fill(parent, "Key " + key, new Rect(x, 12, keyWidth, 30),
            new Color32(58, 82, 66, 255));
        WorkshopUi.ApplyPixelShape(keycap);
        Label(keycap.transform, "Key", new Rect(0, 0, keyWidth, 30), key, 19,
            Gold, TextAnchor.MiddleCenter);
        Label(parent, "Action", new Rect(x + keyWidth + 8, 0, actionWidth, 54), action, 21, WorkshopUi.Paper);
    }

    private void BuildInteractions()
    {
        destinationMarkerArtwork = Resources.Load<Sprite>("WorkshopGameArt/HallDestinationMarker");
        var signs = new GameObject("Hall Wayfinding", typeof(RectTransform), typeof(Canvas));
        signs.transform.SetParent(transform, false);
        signCanvas = signs.GetComponent<RectTransform>();
        signCanvas.pivot = new Vector2(0.5f, 0.5f);
        signCanvas.sizeDelta = new Vector2(HallHalfWidth * 2f, SignCanvasHalfHeight * 2f) * SignPixelsPerUnit;
        signCanvas.localScale = Vector3.one / SignPixelsPerUnit;
        var canvas = signs.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = worldCamera;
        canvas.sortingOrder = 21;

        CreateSeasonInteraction(WorkshopSeason.Spring, -10.4f);
        CreateSeasonInteraction(WorkshopSeason.Summer, -6.15f);
        CreateSeasonInteraction(WorkshopSeason.Autumn, 1.45f);
        CreateSeasonInteraction(WorkshopSeason.Winter, 9.7f);

        CreateInteraction("NewsBoard", 13.45f, "公告栏", "查看工坊公告栏",
            () =>
            {
                WorkshopSeason season = WorkshopMainHallWorld.RecommendedSeason();
                WorkshopProgress.MarkStep("announcement." + season);
                WorkshopNarrativeView.ShowAnnouncement(season);
            }, true, new Color32(205, 143, 78, 255));

        CreateInteraction("HallJournal", -12.1f, "四时织录", "打开四时织录",
            () => WorkshopKnowledgeView.ShowJournal(RecommendedSeason()), true,
            new Color32(107, 174, 126, 255));

        storageDestination = CreateInteraction("StorageHouse", 5.8f, "储物屋", "进入储物屋",
            () => OpenStorage(RecommendedSeason()), true,
            new Color32(205, 177, 96, 255));

        nextStopTag = WorkshopUi.Card(signCanvas, "Next Stop Tag", new Rect(0, 0, 112, 36), SignPine, Gold);
        Label(nextStopTag, "Next Stop", new Rect(0, 0, 112, 36), "下一站", 23, Gold, TextAnchor.MiddleCenter);
        nextStopTag.gameObject.SetActive(false);
    }

    private void CreateSeasonInteraction(WorkshopSeason season, float x)
    {
        bool unlocked = WorkshopProgress.IsUnlocked(season);
        bool complete = WorkshopProgress.IsComplete(season);
        string destination = WorkshopProgress.SeasonName(season) + "季工坊";

        Color markerColor = !unlocked ? new Color32(174, 173, 152, 230) :
            complete ? Gold : Color.Lerp(SeasonAccents[(int)season], WorkshopUi.Paper, 0.45f);
        seasonDestinations[(int)season] = CreateInteraction("Season" + season, x, destination, SeasonActionLabel(season),
            () => UseSeasonDestination(season), unlocked, markerColor);
    }

    private static string SeasonActionLabel(WorkshopSeason season)
    {
        string name = WorkshopProgress.SeasonName(season);
        if (!WorkshopProgress.IsUnlocked(season)) return name + "季工坊 · 待开放";
        if (WorkshopProgress.IsComplete(season)) return "回访" + name + "季工坊";
        if (WorkshopProgress.CanSubmit(season)) return "提交" + name + "季成果";
        return "进入" + name + "季工坊";
    }

    private void UseSeasonDestination(WorkshopSeason season)
    {
        if (!WorkshopProgress.IsUnlocked(season)) return;
        if (WorkshopProgress.CanSubmit(season)) OpenStorage(season);
        else EnterSeason(season);
    }

    private WorkshopHallInteractable CreateInteraction(string name, float x, string destination, string label,
        System.Action action, bool available, Color markerColor)
    {
        var anchor = new GameObject(name);
        anchor.transform.position = new Vector3(x, GroundHeightAt(x) + 0.18f, 0f);
        var interactable = anchor.AddComponent<WorkshopHallInteractable>();
        interactable.Configure(label, action, available);

        if (destinationMarkerArtwork != null)
        {
            var marker = new GameObject("Destination Marker", typeof(SpriteRenderer));
            marker.transform.SetParent(anchor.transform, false);
            marker.transform.position = new Vector3(x, -1.15f, 0f);
            marker.transform.localScale = Vector3.one * (DestinationMarkerHeight / destinationMarkerArtwork.bounds.size.y);
            var renderer = marker.GetComponent<SpriteRenderer>();
            renderer.sprite = destinationMarkerArtwork;
            marker.AddComponent<WorkshopHallMarker>().Initialize(renderer, markerColor, x * 1.37f);
        }
        destinationSigns.Add(interactable, AddDestinationSign(x, destination, available, markerColor));
        return interactable;
    }

    private RectTransform AddDestinationSign(float x, string destination, bool available, Color edge)
    {
        float width = destination.Length * 24f + 28f;
        float left = (x + HallHalfWidth) * SignPixelsPerUnit - width * 0.5f;
        float top = (SignCanvasHalfHeight + 1.33f) * SignPixelsPerUnit;
        var sign = WorkshopUi.Card(signCanvas, destination, new Rect(left, top, width, 37f), SignPine, edge);
        Label(sign, "Destination", new Rect(6f, 0f, width - 12f, 37f), destination, 24,
            available ? WorkshopUi.Paper : new Color32(214, 211, 191, 255), TextAnchor.MiddleCenter);
        return sign;
    }

    private void Update()
    {
        if (player == null) return;
        bool blocked = InputBlocked;
        WorkshopHallInteractable nearest = null;
        float nearestDistance = 2.15f;

        foreach (WorkshopHallInteractable item in destinationSigns.Keys)
        {
            if (item == null) continue;
            float distance = Vector2.Distance(player.position, item.transform.position);
            if (distance > nearestDistance) continue;
            nearestDistance = distance;
            nearest = item;
        }

        foreach (var destination in destinationSigns)
        {
            var item = destination.Key;
            if (item == null) continue;
            item.SetNearest(item == nearest, blocked);
            var sign = destination.Value.gameObject;
            if (sign.activeSelf == item.isNearest)
                sign.SetActive(!item.isNearest);
        }

        bool showNextStop = recommendedDestination != null && recommendedDestination != nearest && !blocked;
        if (nextStopTag.gameObject.activeSelf != showNextStop) nextStopTag.gameObject.SetActive(showNextStop);

        if (nearest != null && !blocked)
        {
            if (Input.GetKeyDown(KeyCode.E)) nearest.Invoke();
        }
    }

    private void LateUpdate()
    {
        if (worldCamera == null || player == null) return;
        float targetX = CameraCenterX(player.position.x);
        Vector3 position = worldCamera.transform.position;
        position.x = Mathf.Lerp(position.x, targetX, 6f * Time.deltaTime);
        worldCamera.transform.position = position;

        if (recommendedDestination == null) return;
        float targetInView = worldCamera.WorldToViewportPoint(recommendedDestination.transform.position).x;
        string direction = targetInView < 0.03f ? "向左走" : targetInView > 0.97f ? "向右走" : "";
        if (directionText.text != direction) directionText.text = direction;
    }

    private float CameraCenterX(float playerX)
    {
        float travel = Mathf.Max(0f, HallHalfWidth - worldCamera.orthographicSize * worldCamera.aspect);
        return Mathf.Clamp(playerX + CameraLookAhead, -travel, travel);
    }

    private bool InputBlocked =>
        modalOpen || WorkshopMinigameMenu.BlocksGameplayInput || WorkshopQuestGuide.IsWorldOverlayActive || WorkshopDialoguePresenter.BlocksWorldInput ||
        WorkshopKnowledgeView.IsOpen || WorkshopStorageView.IsOpen ||
        WorkshopNarrativeView.IsOpen || WorkshopChapterGuide.IsOpen || WorkshopHeritageBrief.IsOpen ||
        WorkshopCraftRecordView.BlocksWorldInput;

    private void EnterSeason(WorkshopSeason season)
    {
        modalOpen = true;
        playerController?.SetControlEnabled(false);
        WorkshopChapterGuide.Show(season, () => WorkshopNarrativeView.PlaySeasonBriefing(season, () =>
        {
            if (player != null) lastPlayerX = player.position.x;
            SceneManager.LoadScene(WorkshopProgress.SceneName(season));
        }), ResumeHall);
    }

    private void OpenStorage(WorkshopSeason season)
    {
        if (modalOpen || WorkshopStorageView.IsOpen) return;
        modalOpen = true;
        playerController?.SetControlEnabled(false);

        WorkshopStorageView.Show(season, ResumeHall, ShowChapterHandoff,
            () => ReloadHall(WorkshopSeason.Winter));
    }

    private void ShowChapterHandoff(WorkshopSeason completed, WorkshopSeason next)
    {
        modalOpen = true;
        playerController?.SetControlEnabled(false);
        WorkshopNarrativeView.ShowChapterInterlude(completed, next, () => EnterSeason(next), ResumeHall);
    }

    private void ResumeHall()
    {
        modalOpen = false;
        playerController?.SetControlEnabled(true);
    }

    private void ShowWardrobe()
    {
        if (modalOpen) return;
        modalOpen = true;
        playerController?.SetControlEnabled(false);
        System.Action openWardrobe = () => WorkshopWardrobe.Show(() =>
        {
            modalOpen = false;
            playerController?.SetControlEnabled(true);
        });
        if (WorkshopNarrativeView.HasSeenFinaleDialogue)
            openWardrobe();
        else
            WorkshopNarrativeView.ShowFinale(openWardrobe);
    }

    private void ReloadHall(WorkshopSeason season)
    {
        if (player != null) lastPlayerX = player.position.x;
        focusSeason = season;
        SceneManager.LoadScene(WorkshopProgress.HubScene);
    }

    private void ReturnToTitle()
    {
        if (modalOpen) return;
        lastPlayerX = player != null ? player.position.x : lastPlayerX;
        SceneManager.LoadScene(WorkshopTitle.SceneName);
    }

    private void RefreshHud()
    {
        if (statusText == null) return;
        focusSeason = RecommendedSeason();
        int stored = 0;
        for (int i = 0; i < SeasonAccents.Length; i++)
        {
            var season = (WorkshopSeason)i;
            var destination = seasonDestinations[i];
            string actionLabel = SeasonActionLabel(season);
            if (destination.actionLabel != actionLabel)
            {
                bool unlocked = WorkshopProgress.IsUnlocked(season);
                destination.Configure(actionLabel, () => UseSeasonDestination(season), unlocked);
                if (unlocked)
                    destinationSigns[destination].GetComponentInChildren<UnityEngine.UI.Text>(true).color = WorkshopUi.Paper;
            }

            foreach (var item in WorkshopProgress.Items(season))
            {
                bool collected = WorkshopProgress.Has(item);
                materialMarks[item].color = collected ? SeasonAccents[i] : ProgressTrack;
                if (collected) stored++;
            }
        }
        seasonSeal.color = SeasonAccents[(int)focusSeason];
        seasonText.text = WorkshopProgress.SeasonName(focusSeason) + "季";
        statusText.text = $"材料  {stored} / {materialMarks.Count}";

        if (WorkshopProgress.IsComplete(WorkshopSeason.Winter))
        {
            if (wardrobeDestination == null)
                wardrobeDestination = CreateInteraction("GarmentDisplay", 17.5f, WorkshopHallSignage.WardrobeName,
                    "进入" + WorkshopHallSignage.WardrobeName,
                    ShowWardrobe, true, new Color32(201, 111, 103, 255));
            recommendedDestination = wardrobeDestination;
            nextTaskText.text = "去" + WorkshopHallSignage.WardrobeName + "试穿";
        }
        else if (WorkshopProgress.CanSubmit(focusSeason))
        {
            recommendedDestination = storageDestination;
            nextTaskText.text = focusSeason == WorkshopSeason.Winter ? "去储物屋 · " + WorkshopTailor.NextOrderActionLabel() :
                "去储物屋 · 合成" + WorkshopProgress.ProductName(focusSeason);
        }
        else
        {
            recommendedDestination = seasonDestinations[(int)focusSeason];
            var task = WorkshopQuests.NextReady(focusSeason);
            nextTaskText.text = "去" + WorkshopProgress.SeasonName(focusSeason) + "季工坊" +
                (task == null ? "" : " · " + task.Title);
        }

        float tagX = (recommendedDestination.transform.position.x + HallHalfWidth) * SignPixelsPerUnit;
        nextStopTag.anchoredPosition = new Vector2(tagX - nextStopTag.rect.width * 0.5f,
            -(SignCanvasHalfHeight + 0.53f) * SignPixelsPerUnit);
    }

    private static WorkshopSeason RecommendedSeason()
    {
        for (int i = 3; i >= 0; i--)
            if (WorkshopProgress.IsUnlocked((WorkshopSeason)i)) return (WorkshopSeason)i;
        return WorkshopSeason.Spring;
    }

    private static UnityEngine.UI.Text Label(Transform parent, string name, Rect bounds, string text,
        int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var label = WorkshopUi.Label(parent, name, bounds, text, size, color, anchor,
            WorkshopArt.WorkshopFont);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = size - 4;
        label.resizeTextMaxSize = size;
        return label;
    }

    private void OnDestroy()
    {
        WorkshopProgress.Changed -= RefreshHud;
        if (active == this) active = null;
    }
}
