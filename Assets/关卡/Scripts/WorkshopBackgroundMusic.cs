using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Plays the score for the current scene or foreground workshop activity.</summary>
internal sealed class WorkshopBackgroundMusic : MonoBehaviour
{
    private const float MusicVolume = 0.48f;
    private const float RewardVolume = 0.18f;
    private const float FadeSeconds = 0.65f;
    private const string FieldworkTrack = "Fieldwork";
    private const string HandcraftTrack = "Handcraft";
    private static readonly string[] SeasonTracks = { "Spring", "Summer", "Autumn", "Winter" };

    private AudioSource first;
    private AudioSource second;
    private AudioSource foreground;
    private AudioListener fallbackListener;
    private string sceneTrack;
    private string currentTrack;
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

        float level = MiniGameCompletionAnimation.IsPlaying ? RewardVolume : MusicVolume;
        float delta = Time.unscaledDeltaTime * MusicVolume / FadeSeconds;
        if (level == RewardVolume) delta *= 2f;
        FadeSource(first, first == foreground ? level : 0f, delta);
        FadeSource(second, second == foreground ? level : 0f, delta);
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
        foreground = null;
        if (track == null) return;

        var clip = Resources.Load<AudioClip>("WorkshopMusic/" + track);
        if (clip == null) return;

        if (first.clip == clip) foreground = first;
        else if (second.clip == clip) foreground = second;
        else
        {
            foreground = first.volume <= second.volume ? first : second;
            foreground.Stop();
            foreground.volume = 0f;
            foreground.clip = clip;
        }

        if (!foreground.isPlaying) foreground.Play();
    }

    private void FadeSource(AudioSource source, float target, float delta)
    {
        source.volume = Mathf.MoveTowards(source.volume, target, delta);
        if (source != foreground && source.volume == 0f && source.isPlaying)
            source.Stop();
    }

    private static string ActivityTrack()
    {
        switch (WorkshopObservationView.ActiveGame)
        {
            case WorkshopObservation.MangzhongCocoonHarvest:
                return FieldworkTrack;
            case WorkshopObservation.MangzhongCocoon:
            case WorkshopObservation.QiufenOffering:
            case WorkshopObservation.LidongDye:
            case WorkshopObservation.DaxueSewing:
                return HandcraftTrack;
            case WorkshopObservation.DongzhiFitting:
                return "Winter";
        }

        if (SpringSilkwormWashOverlay.IsOpen) return HandcraftTrack;
        if (HarvestOverlayManager.IsHarvestActive) return FieldworkTrack;
        if (AutumnOfferingView.IsOpen || WinterGrindingOverlay.IsActive ||
            WinterDyeingOverlay.IsActive || WorkshopTailor.IsOpen)
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
                case "MulberryHarvest":
                case "CocoonMatch":
                case "Maze_Autumn":
                case "MainScene":
                    return FieldworkTrack;
                case "LoomRepair":
                case "EmbroideryScene":
                case "ClothCutting":
                    return HandcraftTrack;
            }
        }

        string active = SceneManager.GetActiveScene().name;
        if (active == WorkshopTitle.SceneName) return "Title";
        if (active == WorkshopProgress.HubScene) return "Hall";
        return WorkshopProgress.TryGetSeason(active, out var season) ? SeasonTracks[(int)season] : null;
    }
}
