using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static WorkshopWardrobe;

internal sealed class WorkshopWardrobeMirror : MonoBehaviour
{
    private enum CameraMode { Character, Starting, Calibrating, Tracking }

    private static readonly Rect PageBounds = new Rect(0f, 0f, 1920f, 1080f);
    private static readonly Rect ViewBounds = new Rect(0f, 0f, 1160f, 750f);
    private static readonly Rect DetailBounds = new Rect(24f, 61f, 508f, 252f);
    private static readonly Rect CharacterBounds = new Rect(431f, 37f, 298f, 620f);
    private static readonly Color Night = new Color32(16, 28, 48, 255);
    private static readonly Color Ink = new Color32(28, 46, 68, 255);
    private static readonly Color Ivory = new Color32(255, 245, 222, 255);
    private static readonly Color Mist = new Color32(194, 211, 233, 255);
    private static readonly Color Gold = new Color32(247, 208, 142, 255);
    private static readonly Color Brass = new Color32(170, 142, 103, 255);
    private static readonly Color Rose = new Color32(239, 153, 134, 255);
    private const float CameraStartTimeout = 6f;
    private const float CameraDropTimeout = 1.7f;
    private const float CalibrationTime = 3f;
    private const float TrackingInterval = 1f / 18f;
    private const int StableFrames = 3;

    private readonly WorkshopWardrobeCameraTracking tracking = new WorkshopWardrobeCameraTracking();
    private readonly WorkshopWardrobeMirrorGesture gesture = new WorkshopWardrobeMirrorGesture();
    private RectTransform avatar;
    private RectTransform avatarClip;
    private RectTransform cameraContent;
    private RectTransform garmentOrigin;
    private RectTransform cameraClip;
    private RectTransform detailCard;
    private RectTransform detailClip;
    private UnityEngine.UI.Image avatarImage;
    private UnityEngine.UI.Image cameraImage;
    private UnityEngine.UI.Image detailImage;
    private RawImage feed;
    private Text outfitName;
    private Text outfitVersion;
    private Text detailCaption;
    private Text materialButtonLabel;
    private Text cameraButtonLabel;
    private Text cameraStatus;
    private Text instruction;
    private UnityEngine.UI.Button materialButton;
    private UnityEngine.UI.Button calibrationButton;
    private Action<int> cycle;
    private Action onClose;
    private Action<RectTransform, UnityEngine.UI.Image, float> drawLiveOutfit;
    private CameraMode mode;
    private WebCamTexture webcam;
    private Coroutine startup;
    private Color32[] pixels;
    private Rect cameraRect;
    private int cameraWidth;
    private int cameraHeight;
    private int cameraRotation;
    private bool verticallyMirrored;
    private int stableFrames;
    private int calibrationSeconds;
    private float lastFrame;
    private float calibrationEnd;
    private float nextSample;
    private float shoulderWidth;
    private Vector2 shoulderMidpoint;
    private float shoulderAngle;
    private bool closing;

    internal static WorkshopWardrobeMirror Show(RectTransform parent, Action<int> cycleStyle, Action close,
        Action toggleMaterial, Action openPhoto, bool startCamera = true)
    {
        var page = WorkshopUi.Rect(parent, "Wardrobe Mirror Page", PageBounds);
        var mirror = page.gameObject.AddComponent<WorkshopWardrobeMirror>();
        mirror.cycle = cycleStyle;
        mirror.onClose = close;
        mirror.Draw(page, toggleMaterial, openPhoto);
        if (startCamera) mirror.StartCamera();
        return mirror;
    }

    private void Draw(RectTransform page, Action toggleMaterial, Action openPhoto)
    {
        var background = WorkshopUi.Fill(page, "Mirror Backdrop", PageBounds, Night);
        background.raycastTarget = true;
        Label(page, "Mirror Heading", new Rect(84f, 28f, 870f, 88f), "AR 镜前试衣", 61, Ivory);
        var back = WardrobeButton(page, "Leave Wardrobe Mirror", new Rect(1578f, 49f, 260f, 55f), "← 返回展架", Close);
        PixelFill(page, "Mirror Stage Frame", new Rect(80f, 134f, 1172f, 762f), Brass);
        var stage = WorkshopUi.Rect(page, "Mirror Stage", new Rect(86f, 140f, ViewBounds.width, ViewBounds.height));
        WorkshopUi.Fill(stage, "Mirror Stage Backing", ViewBounds, Ink);
        avatar = WorkshopUi.Rect(stage, "Character Fitting", ViewBounds);
        WorkshopUi.Picture(avatar, "Character Fitting Court", ViewBounds,
            Resources.Load<Sprite>("WorkshopFinish/WardrobeWinterCourtyard"), stretch: true);
        WorkshopUi.Picture(avatar, "Mirror Fitting Hero", CharacterBounds,
            Resources.Load<Sprite>("WorkshopFinish/FittingHero"), stretch: true);
        avatarClip = Clip(avatar, "Mirror Character Garment Crop");
        avatarImage = WorkshopUi.Picture(avatarClip, "Mirror Character Garment", new Rect(), null, stretch: true);

        cameraContent = WorkshopUi.Rect(stage, "Live Camera Content", ViewBounds);
        cameraContent.gameObject.AddComponent<RectMask2D>();
        var feedRect = WorkshopUi.Rect(cameraContent, "Live Mirror Feed", ViewBounds);
        feedRect.pivot = Vector2.one * 0.5f;
        feed = feedRect.gameObject.AddComponent<RawImage>();
        feed.raycastTarget = false;
        garmentOrigin = WorkshopUi.Rect(cameraContent, "Tracked Shoulders", new Rect());
        cameraClip = WorkshopUi.Rect(garmentOrigin, "Live Garment Crop", new Rect());
        // The source crop rotates with the shoulder line, so clip it with the rotated stencil.
        cameraClip.gameObject.AddComponent<UnityEngine.UI.Image>().raycastTarget = false;
        cameraClip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        cameraImage = WorkshopUi.Picture(cameraClip, "Tracked Garment", new Rect(), null, stretch: true);
        garmentOrigin.gameObject.SetActive(false);
        cameraContent.gameObject.SetActive(false);

        var swipe = WorkshopUi.Fill(stage, "Mirror Swipe Area", ViewBounds, Color.clear);
        swipe.raycastTarget = true;
        swipe.gameObject.AddComponent<WorkshopWardrobeSwipe>().Configure(cycle);
        WardrobeButton(page, "Mirror Previous Garment", new Rect(83f, 922f, 229f, 55f),
            "← 上一款", () => cycle(-1));
        WardrobeButton(page, "Mirror Next Garment", new Rect(1018f, 922f, 229f, 55f),
            "下一款 →", () => cycle(1));
        Label(page, "Mirror Swap Help", new Rect(328f, 921f, 669f, 56f),
            "← / → 键、拖动或按钮换款", 27, Mist, TextAnchor.MiddleCenter);

        var info = WorkshopUi.PixelCard(page, "Mirror Controls",
            new Rect(1275f, 134f, 562f, 842f), Night, Brass);
        outfitName = Label(info, "Mirror Outfit Name", new Rect(26f, 15f, 510f, 68f), "", 48, Ivory);
        outfitVersion = Label(info, "Mirror Outfit Version", new Rect(27f, 79f, 510f, 38f), "", 27, Gold);
        materialButton = WardrobeButton(info, "Mirror Toggle Material", new Rect(26f, 129f, 241f, 56f),
            "看丝绸版", toggleMaterial, primary: true);
        materialButtonLabel = materialButton.GetComponentInChildren<Text>();
        materialButton.transform.parent.gameObject.SetActive(false);
        WardrobeButton(info, "Mirror Photo Entry", new Rect(282f, 129f, 253f, 56f),
            "拍照打卡", openPhoto);
        var cameraButton = WardrobeButton(info, "Mirror Switch Camera", new Rect(26f, 202f, 241f, 55f),
            "使用摄像头", ToggleCamera);
        cameraButtonLabel = cameraButton.GetComponentInChildren<Text>();
        calibrationButton = WardrobeButton(info, "Mirror Recalibrate", new Rect(282f, 202f, 253f, 55f),
            "重新校准", BeginCalibration);
        calibrationButton.interactable = false;
        cameraStatus = Label(info, "Mirror Camera Status", new Rect(27f, 278f, 508f, 43f), "", 28, Gold);
        instruction = Label(info, "Mirror Camera Instruction", new Rect(27f, 326f, 508f, 96f),
            "", 25, Mist, TextAnchor.UpperLeft);

        detailCard = WorkshopUi.PixelCard(info, "Mirror Fabric Card", new Rect(3f, 468f, 556f, 365f), Ink, Brass);
        Label(detailCard, "Mirror Fabric Title", new Rect(24f, 11f, 508f, 45f), "布料特写", 30, Ivory);
        WorkshopUi.Fill(detailCard, "Mirror Fabric Backing", DetailBounds, Night);
        detailClip = Clip(detailCard, "Mirror Fabric Crop");
        detailImage = WorkshopUi.Picture(detailClip, "Mirror Fabric Artwork", new Rect(), null, stretch: true);
        detailCaption = Label(detailCard, "Mirror Fabric Note", new Rect(24f, 320f, 508f, 34f), "", 23, Mist);
        detailCard.gameObject.SetActive(false);
        ShowCharacter("角色试衣", "使用摄像头可开启镜面。选中的衣样和材质会带入拍照。");
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(back.gameObject);
    }

    internal void SetOutfit(string name, string version, string materialNote,
        Action<RectTransform, UnityEngine.UI.Image, Rect> drawPreview,
        Action<RectTransform, UnityEngine.UI.Image, float> drawCamera,
        Action<RectTransform, UnityEngine.UI.Image, Rect> drawDetail, string materialLabel)
    {
        outfitName.text = name;
        outfitVersion.text = version;
        detailCaption.text = materialNote ?? "";
        materialButton.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(materialLabel));
        materialButtonLabel.text = materialLabel;
        drawPreview(avatarClip, avatarImage, CharacterBounds);
        drawLiveOutfit = drawCamera;
        if (garmentOrigin.gameObject.activeSelf)
            drawLiveOutfit(cameraClip, cameraImage, shoulderWidth);
        detailCard.gameObject.SetActive(drawDetail != null);
        drawDetail?.Invoke(detailClip, detailImage, DetailBounds);
    }

    private void ToggleCamera()
    {
        if (mode == CameraMode.Character) StartCamera();
        else ShowCharacter("角色试衣", "摄像头已关闭。选中的衣样和材质仍可试穿。");
    }

    private void StartCamera()
    {
        if (closing || mode != CameraMode.Character) return;
        mode = CameraMode.Starting;
        cameraButtonLabel.text = "使用角色试衣";
        SetStatus("正在打开摄像头…", "若系统弹出权限提示，可在允许后继续。");
        lastFrame = Time.realtimeSinceStartup;
        startup = StartCoroutine(OpenCamera());
    }

    private IEnumerator OpenCamera()
    {
        yield return null;
        if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.WebGLPlayer)
        {
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                startup = null;
                ShowCharacter("摄像头权限未开启", "可继续角色试衣，或允许摄像头后重试。");
                yield break;
            }
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            bool answered = false;
            var request = new UnityEngine.Android.PermissionCallbacks();
            request.PermissionGranted += _ => answered = true;
            request.PermissionDenied += _ => answered = true;
            request.PermissionDeniedAndDontAskAgain += _ => answered = true;
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, request);
            float permissionWaitEnds = Time.realtimeSinceStartup + 30f;
            while (!answered && Time.realtimeSinceStartup < permissionWaitEnds) yield return null;
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                startup = null;
                ShowCharacter("摄像头权限未开启", "可继续角色试衣，或允许摄像头后重试。");
                yield break;
            }
        }
#endif
        startup = null;
        try
        {
            var cameras = WebCamTexture.devices;
            if (cameras.Length == 0)
            {
                ShowCharacter("找不到摄像头", "可直接角色试衣，或连接摄像头后重试。");
                yield break;
            }

            int preferred = Array.FindIndex(cameras, device => device.isFrontFacing);
            webcam = new WebCamTexture(cameras[Mathf.Max(0, preferred)].name, 640, 480, 30);
            feed.texture = webcam;
            webcam.Play();
            lastFrame = Time.realtimeSinceStartup;
        }
        catch (Exception)
        {
            ShowCharacter("摄像头无法开启", "可直接角色试衣，或检查设备和系统摄像头权限后重试。");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) cycle(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) cycle(1);
        if (webcam == null) return;

        float now = Time.realtimeSinceStartup;
        if (!webcam.isPlaying || !webcam.didUpdateThisFrame || webcam.width < 32 || webcam.height < 32)
        {
            bool starting = mode == CameraMode.Starting;
            if (now - lastFrame >= (starting ? CameraStartTimeout : CameraDropTimeout))
                ShowCharacter(starting ? "未收到摄像头画面" : "摄像头画面已中断",
                    "可直接角色试衣，或检查设备和系统摄像头权限后重试。");
            return;
        }

        lastFrame = now;
        int rotation = ((Mathf.RoundToInt(webcam.videoRotationAngle / 90f) % 4) + 4) % 4;
        if (mode == CameraMode.Starting || webcam.width != cameraWidth || webcam.height != cameraHeight ||
            rotation != cameraRotation || webcam.videoVerticallyMirrored != verticallyMirrored)
        {
            cameraWidth = webcam.width;
            cameraHeight = webcam.height;
            cameraRotation = rotation;
            verticallyMirrored = webcam.videoVerticallyMirrored;
            ConfigureFeed();
            BeginCalibration();
        }

        if (mode == CameraMode.Calibrating)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(calibrationEnd - now));
            if (seconds != calibrationSeconds && seconds > 0)
            {
                calibrationSeconds = seconds;
                SetStatus("校准空画面 · " + seconds, "请暂时让出画面。校准后再回到镜头中央。");
            }
            if (now < calibrationEnd || !TryReadFrame()) return;
            tracking.CaptureBackground();
            mode = CameraMode.Tracking;
            SetStatus("请站到镜头中央", "正对镜头，让双肩进入画面。");
            return;
        }

        if (mode == CameraMode.Tracking && now >= nextSample)
        {
            nextSample = now + TrackingInterval;
            if (TryReadFrame()) UpdatePose(now);
        }
    }

    private void ConfigureFeed()
    {
        bool swapped = (cameraRotation & 1) != 0;
        float aspect = swapped ? (float)cameraHeight / cameraWidth : (float)cameraWidth / cameraHeight;
        float width = Mathf.Min(ViewBounds.width, ViewBounds.height * aspect);
        float height = width / aspect;
        cameraRect = new Rect(ViewBounds.center - new Vector2(width, height) * 0.5f, new Vector2(width, height));
        var rect = feed.rectTransform;
        rect.anchoredPosition = new Vector2(cameraRect.center.x, -cameraRect.center.y);
        rect.sizeDelta = swapped ? new Vector2(height, width) : new Vector2(width, height);
        rect.localEulerAngles = new Vector3(0f, 0f, -90f * cameraRotation);
        bool flipX = !swapped;
        bool flipY = verticallyMirrored ^ swapped;
        feed.uvRect = new Rect(flipX ? 1f : 0f, flipY ? 1f : 0f, flipX ? -1f : 1f, flipY ? -1f : 1f);
        cameraContent.gameObject.SetActive(true);
        avatar.gameObject.SetActive(false);
    }

    private void BeginCalibration()
    {
        if (webcam == null || !cameraContent.gameObject.activeSelf) return;
        mode = CameraMode.Calibrating;
        calibrationButton.interactable = true;
        calibrationEnd = Time.realtimeSinceStartup + CalibrationTime;
        calibrationSeconds = -1;
        stableFrames = 0;
        gesture.Reset();
        garmentOrigin.gameObject.SetActive(false);
        SetStatus("校准空画面 · 3", "请暂时让出画面。校准后再回到镜头中央。");
    }

    private bool TryReadFrame()
    {
        try
        {
            if (pixels == null || pixels.Length != cameraWidth * cameraHeight)
                pixels = new Color32[cameraWidth * cameraHeight];
            pixels = webcam.GetPixels32(pixels);
            tracking.Sample(pixels, cameraWidth, cameraHeight, verticallyMirrored, cameraRotation);
            return true;
        }
        catch (Exception)
        {
            ShowCharacter("摄像头画面读取失败", "可继续角色试衣，或检查摄像头后重试。");
            return false;
        }
    }

    private void UpdatePose(float now)
    {
        if (!tracking.TryEstimate(out WorkshopWardrobeCameraTracking.Pose pose))
        {
            stableFrames = 0;
            gesture.Sample(false, 0f, now);
            garmentOrigin.gameObject.SetActive(false);
            SetStatus("请站到镜头中央", "正对镜头，让双肩进入画面，识别稳定后会恢复衣片。");
            return;
        }

        stableFrames = Mathf.Min(stableFrames + 1, StableFrames);
        if (stableFrames < StableFrames) return;
        Vector2 center = cameraRect.position + Vector2.Scale(pose.ShoulderMidpoint, cameraRect.size);
        float width = Mathf.Clamp(pose.ShoulderWidth * cameraRect.width, 82f, cameraRect.width * 0.58f);
        float angle = Mathf.Clamp(Mathf.Atan2(pose.ShoulderSlope * cameraRect.height, width) * Mathf.Rad2Deg, -16f, 16f);
        bool wasVisible = garmentOrigin.gameObject.activeSelf;
        shoulderMidpoint = wasVisible ? Vector2.Lerp(shoulderMidpoint, center, 0.32f) : center;
        shoulderWidth = wasVisible ? Mathf.Lerp(shoulderWidth, width, 0.24f) : width;
        shoulderAngle = wasVisible ? Mathf.Lerp(shoulderAngle, angle, 0.24f) : angle;
        garmentOrigin.anchoredPosition = new Vector2(shoulderMidpoint.x, -shoulderMidpoint.y);
        garmentOrigin.localEulerAngles = new Vector3(0f, 0f, -shoulderAngle);
        drawLiveOutfit?.Invoke(cameraClip, cameraImage, shoulderWidth);
        garmentOrigin.gameObject.SetActive(true);

        int direction = gesture.Sample(pose.HandRaised, pose.HandOffset, now);
        if (direction != 0) cycle(direction);
        if (gesture.IsReady)
            SetStatus("手势已就绪", "沿镜中方向挥手。← 上一款，下一款 →");
        else if (gesture.WaitingForRelease)
            SetStatus("已换一款", "放下手后可继续挥手换款。");
        else
            SetStatus("已对准肩线", "抬起一只手至肩侧，出现就绪提示后再向左或右挥。");
    }

    private void ShowCharacter(string title, string hint)
    {
        StopCamera();
        mode = CameraMode.Character;
        cameraButtonLabel.text = "使用摄像头";
        calibrationButton.interactable = false;
        cameraContent.gameObject.SetActive(false);
        avatar.gameObject.SetActive(true);
        garmentOrigin.gameObject.SetActive(false);
        SetStatus(title, hint, title == "角色试衣" ? Gold : Rose);
    }

    private void SetStatus(string title, string hint, Color? color = null)
    {
        if (cameraStatus.text != title) cameraStatus.text = title;
        if (instruction.text != hint) instruction.text = hint;
        cameraStatus.color = color ?? Gold;
    }

    private void StopCamera()
    {
        if (startup != null)
        {
            StopCoroutine(startup);
            startup = null;
        }
        if (webcam == null) return;
        feed.texture = null;
        var camera = webcam;
        webcam = null;
        try
        {
            if (camera.isPlaying) camera.Stop();
        }
        catch (UnityException)
        {
            // The native device may already have disconnected.
        }
        finally
        {
            Destroy(camera);
        }
        pixels = null;
        gesture.Reset();
    }

    internal void Close()
    {
        if (closing) return;
        closing = true;
        StopCamera();
        gameObject.SetActive(false);
        Destroy(gameObject);
        onClose?.Invoke();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && mode != CameraMode.Character)
            ShowCharacter("摄像头已暂停", "可继续角色试衣，也可重新打开摄像头。");
    }

    private void OnDisable() => StopCamera();
}

internal sealed class WorkshopWardrobeMirrorGesture
{
    private const int ReadyFrames = 3;
    private const int ReleaseFrames = 5;
    private const float MinimumTravel = 0.083f;
    private const float Cooldown = 0.65f;
    private float origin;
    private float nextSwipe;
    private int raisedFrames;
    private int loweredFrames;

    internal bool IsReady { get; private set; }
    internal bool WaitingForRelease { get; private set; }

    internal int Sample(bool raised, float offset, float now)
    {
        if (!raised)
        {
            loweredFrames++;
            if (loweredFrames >= ReleaseFrames)
            {
                WaitingForRelease = false;
                IsReady = false;
                raisedFrames = 0;
            }
            return 0;
        }

        loweredFrames = 0;
        if (WaitingForRelease || now < nextSwipe) return 0;
        if (!IsReady)
        {
            if (++raisedFrames < ReadyFrames) return 0;
            origin = offset;
            IsReady = true;
            return 0;
        }
        float travel = offset - origin;
        if (Mathf.Abs(travel) < MinimumTravel) return 0;
        nextSwipe = now + Cooldown;
        IsReady = false;
        WaitingForRelease = true;
        return travel < 0f ? -1 : 1;
    }

    internal void Reset()
    {
        IsReady = false;
        WaitingForRelease = false;
        raisedFrames = 0;
        loweredFrames = 0;
        nextSwipe = 0f;
    }
}
