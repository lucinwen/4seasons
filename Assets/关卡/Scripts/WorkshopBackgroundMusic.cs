using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Plays the score for the current scene or foreground workshop activity.</summary>
internal sealed class WorkshopBackgroundMusic : MonoBehaviour
{
    private const float MusicVolume = 0.42f;
    private const float RewardVolume = 0.15f;
    private const float FadeSeconds = 1.1f;
    private const string FieldworkTrack = "Harvest";
    private const string HandcraftTrack = "Weave";
    private static readonly string[] SeasonTracks = { "Spring", "Summer", "Autumn", "Winter" };

    private AudioSource first;
    private AudioSource second;
    private AudioSource foreground;
    private AudioListener fallbackListener;
    private string sceneTrack;
    private string currentTrack;
    private float firstMix;
    private float secondMix;
    private float musicGain = MusicVolume;
    private bool sceneChanged = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var host = new GameObject("Workshop Background Music");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopBackgroundMusic>();
    }

    private void Awake()
    {
        first = AddMusicSource();
        second = AddMusicSource();

        var listeningPost = new GameObject("Standalone Minigame Listener");
        listeningPost.SetActive(false);
        listeningPost.transform.SetParent(transform, false);
        fallbackListener = listeningPost.AddComponent<AudioListener>();

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private AudioSource AddMusicSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.volume = 0f;
        return source;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => sceneChanged = true;
    private void OnSceneUnloaded(Scene scene) => sceneChanged = true;

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        fallbackListener.gameObject.SetActive(false);
        sceneChanged = true;
    }

    private void LateUpdate()
    {
        if (sceneChanged)
        {
            sceneChanged = false;
            sceneTrack = SceneTrack();
            UpdateListener();
        }

        string nextTrack = ActivityTrack() ?? sceneTrack;
        if (currentTrack != nextTrack)
            ChangeTrack(nextTrack);

        bool rewardPlaying = MiniGameCompletionAnimation.IsPlaying;
        float duckTime = rewardPlaying ? 0.18f : 0.55f;
        musicGain = Mathf.MoveTowards(musicGain, rewardPlaying ? RewardVolume : MusicVolume,
            Time.unscaledDeltaTime * (MusicVolume - RewardVolume) / duckTime);

        float mixStep = Time.unscaledDeltaTime / FadeSeconds;
        FadeSource(first, ref firstMix, mixStep);
        FadeSource(second, ref secondMix, mixStep);
    }

    private void UpdateListener()
    {
        // Some of the older minigame scenes were designed to borrow their host scene's listener.
        bool needsFallback = sceneTrack != null;
        foreach (var listener in FindObjectsOfType<AudioListener>())
        {
            if (listener == fallbackListener || !listener.isActiveAndEnabled) continue;
            needsFallback = false;
            break;
        }

        fallbackListener.gameObject.SetActive(needsFallback);
    }

    private void ChangeTrack(string track)
    {
        currentTrack = track;
        if (track == null)
        {
            foreground = null;
            return;
        }

        var clip = Resources.Load<AudioClip>("WorkshopMusic/" + track);
        if (clip == null) return;

        if (first.clip == clip) foreground = first;
        else if (second.clip == clip) foreground = second;
        else
        {
            foreground = firstMix <= secondMix ? first : second;
            foreground.Stop();
            foreground.volume = 0f;
            if (foreground == first) firstMix = 0f;
            else secondMix = 0f;
            foreground.clip = clip;
        }

        if (foreground.isPlaying) return;
        if (foreground.timeSamples > 0) foreground.UnPause();
        else foreground.Play();
    }

    private void FadeSource(AudioSource source, ref float mix, float step)
    {
        mix = Mathf.MoveTowards(mix, source == foreground ? 1f : 0f, step);
        source.volume = Mathf.Sin(mix * Mathf.PI * 0.5f) * musicGain;

        // Keep the outgoing cue's position so closing an activity can resume the chapter theme.
        if (source != foreground && mix == 0f && source.isPlaying)
            source.Pause();
    }

    private static string ActivityTrack()
    {
        if (AutumnOfferingView.IsOpen) return "Offering";

        var activity = WorkshopObservationView.ActiveGame;
        if (HarvestOverlayManager.IsHarvestActive || activity == WorkshopObservation.QingmingMulberry ||
            activity == WorkshopObservation.MangzhongCocoon)
            return FieldworkTrack;
        if (WinterGrindingOverlay.IsActive || WinterDyeingOverlay.IsActive || WorkshopTailor.IsOpen ||
            activity == WorkshopObservation.LidongDye || activity == WorkshopObservation.XiaoxueTailoring)
            return HandcraftTrack;

        return null;
    }

    private static string SceneTrack()
    {
        // A minigame scene may be reached directly or loaded on top of a seasonal map.
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            switch (scene.name)
            {
                case "Maze_Autumn": return "Maze";
                case "MulberryHarvest":
                case "CocoonMatch":
                case "MainScene": return FieldworkTrack;
                case "LoomRepair":
                case "EmbroideryScene":
                case "ClothCutting": return HandcraftTrack;
            }
        }

        string active = SceneManager.GetActiveScene().name;
        if (active == WorkshopTitle.SceneName) return "Title";
        if (active == WorkshopProgress.HubScene) return "Hall";
        return WorkshopProgress.TryGetSeason(active, out var season) ? SeasonTracks[(int)season] : null;
    }
}
