using UnityEngine;

/// <summary>
/// Sound layer for workshop UI and gameplay events.
/// Clips are prepared once and played through a persistent 2D AudioSource.
/// </summary>
public sealed class WorkshopAudioFeedback : MonoBehaviour
{
    private static WorkshopAudioFeedback instance;
    private AudioSource source;
    private AudioClip hover;
    private AudioClip click;
    private AudioClip confirm;
    private AudioClip progress;
    private AudioClip collect;
    private AudioClip milestone;
    private AudioClip notice;
    private AudioClip placement;
    private AudioClip fabric;
    private AudioClip[] grindStrokes;
    private AudioClip dyeIngredient;
    private AudioClip dyeDip;
    private AudioClip clothSnip;
    private int nextGrindStroke;
    private int lastClickFrame = -1;
    private float lastHoverTime = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        var host = new GameObject("Workshop Audio Feedback");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<WorkshopAudioFeedback>();
        instance.Initialize();
    }

    private void Initialize()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.ignoreListenerPause = true;

        hover = LoadSound("UiHover");
        click = LoadSound("UiClick");
        confirm = LoadSound("Confirm");
        progress = LoadSound("Progress");
        collect = LoadSound("Collect");
        milestone = LoadSound("Milestone");
        notice = LoadSound("Notice");
        placement = LoadSound("Placement");
        fabric = LoadSound("Fabric");
        grindStrokes = new[] { LoadSound("DyeGrind1"), LoadSound("DyeGrind2"), LoadSound("DyeGrind3") };
        dyeIngredient = LoadSound("DyeIngredient");
        dyeDip = LoadSound("DyeDip");
        clothSnip = LoadSound("ClothSnip");
    }

    public static void Click()
    {
        // A button and its action handler can both report the same press.
        if (instance == null || instance.lastClickFrame == Time.frameCount) return;
        instance.lastClickFrame = Time.frameCount;
        instance.Play(instance.click, 0.16f);
    }

    public static void Hover()
    {
        if (instance == null || Time.unscaledTime - instance.lastHoverTime < 0.07f) return;
        instance.lastHoverTime = Time.unscaledTime;
        instance.Play(instance.hover, 0.08f);
    }

    public static void Confirm()
    {
        instance?.Play(instance.confirm, 0.3f);
    }

    public static void Progress()
    {
        instance?.Play(instance.progress, 0.26f);
    }

    public static void Collect()
    {
        instance?.Play(instance.collect, 0.32f);
    }

    public static void Milestone()
    {
        instance?.Play(instance.milestone, 0.36f);
    }

    public static void Notice()
    {
        instance?.Play(instance.notice, 0.24f);
    }

    public static void Place()
    {
        instance?.Play(instance.placement, 0.23f);
    }

    public static void Fabric()
    {
        instance?.Play(instance.fabric, 0.28f);
    }

    public static void Grind()
    {
        if (instance == null) return;
        instance.Play(instance.grindStrokes[instance.nextGrindStroke], 0.36f);
        instance.nextGrindStroke = (instance.nextGrindStroke + 1) % instance.grindStrokes.Length;
    }

    public static void DyeIngredient()
    {
        instance?.Play(instance.dyeIngredient, 0.36f);
    }

    public static void DyeDip()
    {
        instance?.Play(instance.dyeDip, 0.38f);
    }

    public static void Snip()
    {
        instance?.Play(instance.clothSnip, 0.24f);
    }

    private static AudioClip LoadSound(string name)
    {
        return Resources.Load<AudioClip>("WorkshopSfx/" + name);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (source == null || clip == null || clip.length <= 0f) return;
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
