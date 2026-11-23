using System;
using UnityEngine;

/// <summary>
/// Sound layer for workshop UI and gameplay events.
/// Clips are prepared once and played through a persistent 2D AudioSource.
/// </summary>
public sealed class WorkshopAudioFeedback : MonoBehaviour
{
    private const int SampleRate = 44100;

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

        hover = BuildHover();
        click = BuildClick();
        confirm = BuildTwoNote("Workshop Confirm", 523.25f, 659.25f, 0.16f);
        progress = BuildTwoNote("Workshop Progress", 392f, 493.88f, 0.18f);
        collect = BuildGlide("Workshop Collect", 880f, 1320f, 0.13f);
        milestone = BuildMilestone();
        notice = BuildTwoNote("Workshop Notice", 659.25f, 523.25f, 0.2f);
        placement = BuildPlacement();
        fabric = BuildFabric();
        grindStrokes = new[] { LoadSound("DyeGrind1"), LoadSound("DyeGrind2"), LoadSound("DyeGrind3") };
        dyeIngredient = LoadSound("DyeIngredient");
        dyeDip = LoadSound("DyeDip");
        clothSnip = LoadSound("ClothSnip");
    }

    public static void Click()
    {
        instance?.Play(instance.click, 0.16f);
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

    private static AudioClip BuildClick()
    {
        float duration = 0.045f;
        var random = new System.Random(2211);
        return CreateClip("Workshop Click", duration, (time, normalised) =>
        {
            float envelope = Mathf.Pow(1f - normalised, 3.2f);
            float noise = (float)(random.NextDouble() * 2d - 1d);
            float tone = Mathf.Sin(time * Mathf.PI * 2f * 720f) * 0.2f;
            return (tone + noise * 0.32f) * envelope;
        });
    }

    private static AudioClip BuildHover()
    {
        const float duration = 0.024f;
        return CreateClip("Workshop Hover", duration, (time, normalised) =>
        {
            float envelope = Mathf.Sin(normalised * Mathf.PI) * (1f - normalised * 0.45f);
            float tone = Mathf.Sin(time * Mathf.PI * 2f * 1180f);
            float overtone = Mathf.Sin(time * Mathf.PI * 2f * 1770f) * 0.18f;
            return (tone + overtone) * envelope * 0.24f;
        });
    }

    private static AudioClip BuildPlacement()
    {
        return CreateClip("Workshop Placement", 0.09f, (time, normalised) =>
        {
            float decay = Mathf.Pow(1f - normalised, 3f);
            float body = Mathf.Sin(time * Mathf.PI * 2f * 315f) * 0.35f;
            float edge = Mathf.Sin(time * Mathf.PI * 2f * 548f) * 0.13f * decay;
            return (body + edge) * decay;
        });
    }

    private static AudioClip BuildFabric()
    {
        var random = new System.Random(3059);
        float filteredNoise = 0f;
        return CreateClip("Workshop Fabric Rustle", 0.17f, (time, normalised) =>
        {
            float noise = (float)(random.NextDouble() * 2d - 1d);
            filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.24f);
            float envelope = Mathf.Sin(normalised * Mathf.PI);
            float folds = 0.76f + 0.24f * Mathf.Sin(normalised * Mathf.PI * 4f);
            return filteredNoise * envelope * folds * 0.8f;
        });
    }

    private static AudioClip BuildTwoNote(string name, float first, float second, float duration)
    {
        return CreateClip(name, duration, (time, normalised) =>
        {
            const float noteLength = 0.12f;
            float firstLocal = time / noteLength;
            float secondLocal = (time - 0.055f) / noteLength;
            float value = 0f;
            if (firstLocal >= 0f && firstLocal <= 1f)
                value += Note(first, time, firstLocal, 0.3f);
            if (secondLocal >= 0f && secondLocal <= 1f)
                value += Note(second, time, secondLocal, 0.36f);
            return value;
        });
    }

    private static float Note(float frequency, float time, float local, float volume)
    {
        float attack = Mathf.Clamp01(local / 0.08f);
        float release = Mathf.Clamp01((1f - local) / 0.34f);
        float envelope = attack * release;
        float fundamental = Mathf.Sin(time * Mathf.PI * 2f * frequency);
        float overtone = Mathf.Sin(time * Mathf.PI * 2f * frequency * 2f) * 0.12f;
        return (fundamental + overtone) * envelope * volume;
    }

    private static AudioClip BuildGlide(string name, float startFrequency, float endFrequency, float duration)
    {
        float phase = 0f;
        return CreateClip(name, duration, (time, normalised) =>
        {
            float frequency = Mathf.Lerp(startFrequency, endFrequency, Mathf.SmoothStep(0f, 1f, normalised));
            phase += frequency / SampleRate;
            if (phase > 1f) phase -= 1f;
            float envelope = Mathf.Sin(normalised * Mathf.PI) * Mathf.Pow(1f - normalised, 0.25f);
            return Mathf.Sin(phase * Mathf.PI * 2f) * envelope * 0.42f;
        });
    }

    private static AudioClip BuildMilestone()
    {
        float duration = 0.58f;
        float[] notes = { 523.25f, 659.25f, 783.99f };
        return CreateClip("Workshop Milestone", duration, (time, normalised) =>
        {
            float value = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float start = i * 0.14f;
                float local = (time - start) / 0.3f;
                if (local < 0f || local > 1f) continue;
                float envelope = Mathf.Sin(local * Mathf.PI) * Mathf.Pow(1f - local, 0.55f);
                float fundamental = Mathf.Sin(time * Mathf.PI * 2f * notes[i]);
                float overtone = Mathf.Sin(time * Mathf.PI * 2f * notes[i] * 2f) * 0.1f;
                value += (fundamental * 0.28f + overtone) * envelope;
            }

            float tail = Mathf.Clamp01((1f - normalised) / 0.08f);
            return value * tail;
        });
    }

    private static AudioClip CreateClip(string name, float duration,
        Func<float, float, float> sample)
    {
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
        var data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float normalised = i / (float)Mathf.Max(1, sampleCount - 1);
            float value = Mathf.Clamp(sample(i / (float)SampleRate, normalised), -0.85f, 0.85f);
            float edgeFade = Mathf.Clamp01(Mathf.Min(normalised / 0.03f, (1f - normalised) / 0.03f));
            data[i] = value * edgeFade;
        }

        var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
