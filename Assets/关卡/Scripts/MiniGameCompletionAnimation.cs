using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum MiniGameCompletionId
{
    MulberryHarvest,
    SilkwormEgg,
    SilkwormFeeding,
    MazeSpool,
    CocoonSorting,
    ThanksgivingRitual,
    LoomRepair,
    WeavingPuzzle,
    DyeCollection,
    ClothesCutting
}

/// <summary>Plays a minigame's result animation before continuing its reward or return flow.</summary>
public sealed class MiniGameCompletionAnimation : MonoBehaviour
{
    public const string ResourceRoot = "MiniGameCompletion/";

    [Serializable]
    public sealed class ClipInfo
    {
        public const int Columns = 5;
        public const int Rows = 5;
        public const int FramesPerSheet = Columns * Rows;

        public int frameCount;
        public float frameRate;
        public int frameWidth;
        public int frameHeight;

        public bool IsValid => frameCount > 0 && frameRate > 0f && frameRate <= 60f &&
            frameWidth > 0 && frameHeight > 0;
        public int SheetCount => (frameCount - 1) / FramesPerSheet + 1;
        public int SheetWidth => frameWidth * Columns;
        public int SheetHeight => frameHeight * Rows;
        public float Duration => frameCount / frameRate;
    }

    private const float FadeInDuration = 0.18f;
    private const float FadeOutDuration = 0.2f;
    private const float FinalHoldDuration = 0.35f;

    private static MiniGameCompletionAnimation _active;

    private MiniGameCompletionId _id;
    private Action _onCompleted;
    private CanvasGroup _canvasGroup;
    private RawImage _image;
    private AudioSource _audioSource;
    private ResourceRequest _sheetRequest;
    private ResourceRequest _audioRequest;
    private Texture2D _sheet;
    private AudioClip _audioClip;

    public static bool IsPlaying => _active != null;

    public static void PlayThen(MiniGameCompletionId id, Action onCompleted)
    {
        if (_active != null)
        {
            _active._onCompleted += _active._id == id ? onCompleted : () => PlayThen(id, onCompleted);
            return;
        }

        var go = new GameObject("[MiniGameCompletionAnimation]", typeof(RectTransform));
        _active = go.AddComponent<MiniGameCompletionAnimation>();
        _active._id = id;
        _active._onCompleted = onCompleted;
        _active.BuildInterface();
        _active.StartCoroutine(_active.PlaySequence());
    }

    private void BuildInterface()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        gameObject.AddComponent<GraphicRaycaster>();

        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        dim.rectTransform.SetParent(transform, false);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        dim.color = MinigameUiPresentation.Veil;

        var frame = new GameObject("Animation Bounds", typeof(RectTransform)).GetComponent<RectTransform>();
        frame.SetParent(transform, false);
        frame.anchorMin = new Vector2(0.08f, 0.11f);
        frame.anchorMax = new Vector2(0.92f, 0.89f);
        frame.offsetMin = frame.offsetMax = Vector2.zero;

        _image = new GameObject("Animation", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        _image.rectTransform.SetParent(frame, false);
        _image.raycastTarget = false;
        _image.enabled = false;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;

        EventSystem.current?.SetSelectedGameObject(null);
    }

    private IEnumerator PlaySequence()
    {
        string path = ResourceRoot + _id + "/";
        var manifest = Resources.Load<TextAsset>(path + "Clip");
        ClipInfo clip = null;
        if (manifest != null)
        {
            try
            {
                clip = JsonUtility.FromJson<ClipInfo>(manifest.text);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"[MiniGameCompletionAnimation] {path}Clip: {exception.Message}");
            }
            Resources.UnloadAsset(manifest);
        }

        if (clip == null || !clip.IsValid)
        {
            Debug.LogWarning($"[MiniGameCompletionAnimation] Missing or invalid clip data at {path}Clip.");
            Finish();
            yield break;
        }

        _sheetRequest = LoadSheet(path, 0);
        _audioRequest = Resources.LoadAsync<AudioClip>(path + "Audio");
        yield return _sheetRequest;
        if (!UseLoadedSheet(clip, path, 0))
        {
            Finish();
            yield break;
        }
        if (clip.SheetCount > 1) _sheetRequest = LoadSheet(path, 1);

        yield return _audioRequest;
        _audioClip = _audioRequest.asset as AudioClip;
        _audioRequest = null;

        var fit = _image.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = (float)clip.frameWidth / clip.frameHeight;
        ShowFrame(0);
        _image.enabled = true;
        yield return Fade(0f, 1f, FadeInDuration);

        if (_audioClip != null)
        {
            _audioSource.clip = _audioClip;
            _audioSource.Play();
        }

        // Keep the current atlas and prefetch only the next one.
        int sheetIndex = 0;
        int displayedFrame = 0;
        double startedAt = Time.unscaledTimeAsDouble;
        int lastFrame = clip.frameCount - 1;
        while (true)
        {
            double elapsed = Time.unscaledTimeAsDouble - startedAt;
            int frame = Mathf.Min(lastFrame, (int)(elapsed * clip.frameRate));
            while (frame / ClipInfo.FramesPerSheet > sheetIndex && _sheetRequest.isDone)
            {
                sheetIndex++;
                if (!UseLoadedSheet(clip, path, sheetIndex))
                {
                    Finish();
                    yield break;
                }
                if (sheetIndex + 1 < clip.SheetCount) _sheetRequest = LoadSheet(path, sheetIndex + 1);
            }

            frame = Mathf.Min(frame, (sheetIndex + 1) * ClipInfo.FramesPerSheet - 1);
            if (frame != displayedFrame)
            {
                ShowFrame(frame);
                displayedFrame = frame;
            }
            if (elapsed >= clip.Duration && displayedFrame == lastFrame) break;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(FinalHoldDuration);
        yield return Fade(1f, 0f, FadeOutDuration);
        Finish();
    }

    private static ResourceRequest LoadSheet(string path, int index)
    {
        return Resources.LoadAsync<Texture2D>(path + index.ToString("00"));
    }

    private bool UseLoadedSheet(ClipInfo clip, string path, int index)
    {
        var incoming = _sheetRequest.asset as Texture2D;
        _sheetRequest = null;
        if (incoming == null || incoming.width != clip.SheetWidth || incoming.height != clip.SheetHeight)
        {
            Unload(incoming);
            Debug.LogWarning($"[MiniGameCompletionAnimation] Missing or invalid atlas at {path}{index:00}.");
            return false;
        }

        _image.texture = incoming;
        Unload(_sheet);
        _sheet = incoming;
        return true;
    }

    private void ShowFrame(int frame)
    {
        int cell = frame % ClipInfo.FramesPerSheet;
        const float width = 1f / ClipInfo.Columns;
        const float height = 1f / ClipInfo.Rows;
        _image.uvRect = new Rect(
            (cell % ClipInfo.Columns) * width,
            (ClipInfo.Rows - 1 - cell / ClipInfo.Columns) * height,
            width, height);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }

    private void Finish()
    {
        Action callback = _onCompleted;
        _onCompleted = null;
        _active = null;
        ReleaseAssets();
        gameObject.SetActive(false);
        Destroy(gameObject);
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (_active == this) _active = null;
        ReleaseAssets();
    }

    private void ReleaseAssets()
    {
        if (_image != null) _image.texture = null;
        if (_audioSource != null)
        {
            _audioSource.Stop();
            _audioSource.clip = null;
        }

        Unload(_sheet);
        _sheet = null;
        Unload(_audioClip);
        _audioClip = null;
        ReleaseRequest(_sheetRequest);
        _sheetRequest = null;
        ReleaseRequest(_audioRequest);
        _audioRequest = null;
    }

    private static void ReleaseRequest(ResourceRequest request)
    {
        if (request == null) return;
        if (request.isDone) Unload(request.asset);
        else request.completed += operation => Unload(((ResourceRequest)operation).asset);
    }

    private static void Unload(UnityEngine.Object asset)
    {
        if (asset != null) Resources.UnloadAsset(asset);
    }
}
