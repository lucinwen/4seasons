using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor-only rendered review; never creates a player build.</summary>
[InitializeOnLoad]
public static class WordRevisionVisualReview
{
    private const string Active = "WordRevision.VisualReview.Active";
    private const string SaveKey = "ShiXu.Workshop.v1";
    private const string Prior = "WordRevision.VisualReview.Save";
    private const string Existed = "WordRevision.VisualReview.Existed";
    private const string Feedback = "WordRevision.VisualReview.Feedback";
    private const string Autumn = "WordRevision.VisualReview.Autumn";
    private const string Chapters = "WordRevision.VisualReview.Chapters";
    private const string Summer = "WordRevision.VisualReview.Summer";
    private const string AutumnIntegration = "WordRevision.VisualReview.AutumnIntegration";
    private const string Immersive = "WordRevision.VisualReview.Immersive";
    private const string CocoonOnly = "WordRevision.VisualReview.CocoonOnly";
    private const string Feedback2 = "WordRevision.VisualReview.Feedback2";
    private const string Feedback3 = "WordRevision.VisualReview.Feedback3";
    static WordRevisionVisualReview()
    {
        EditorApplication.playModeStateChanged += OnState;
        EditorApplication.update += CheckCaptureRequest;
    }

    private static void CheckCaptureRequest()
    {
        const string stopPath = "../WordRevision/stop-play-request.txt";
        if (File.Exists(stopPath) && !EditorApplication.isCompiling)
        {
            File.Move(stopPath, "../WordRevision/stop-play-received-" + DateTime.UtcNow.Ticks + ".txt");
            Debug.Log("[WordRevision] Requested normal exit from play mode; scene edits are preserved by Unity.");
            EditorApplication.isPlaying = false;
            return;
        }
        const string buildPath = "../WordRevision/build-request.txt";
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && File.Exists(buildPath))
        {
            File.Move(buildPath, "../WordRevision/build-received-" + DateTime.UtcNow.Ticks + ".txt");
            EditorApplication.delayCall += WordRevisionMaintenance.ValidateAndBuild;
            return;
        }
        const string path = "../WordRevision/capture-request.txt";
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists(path)) return;
        string mode = File.ReadAllText(path).Trim();
        File.Move(path, "../WordRevision/capture-received-" + DateTime.UtcNow.Ticks + ".txt");
        if (mode == "feedback2") EditorApplication.delayCall += StartFeedback2Captures;
        else if (mode == "immersive") EditorApplication.delayCall += StartImmersiveCaptures;
        else if (mode == "summer") EditorApplication.delayCall += StartSummerCaptures;
        else if (mode == "chapters") EditorApplication.delayCall += StartChapterCaptures;
        else if (mode == "autumn") EditorApplication.delayCall += StartAutumnCaptures;
        else if (mode == "feedback") EditorApplication.delayCall += StartFeedbackCaptures;
        else EditorApplication.delayCall += StartCaptures;
    }

    [MenuItem("Tools/Word Revision/Capture Actual Interfaces")]
    public static void StartCaptures()
    {
        SessionState.SetBool(Chapters, false);
        SessionState.SetBool(Feedback, false);
        SessionState.SetBool(Autumn, false);
        StartReview(false);
    }

    [MenuItem("Tools/Word Revision/Review Spring Feedback")]
    public static void StartFeedbackCaptures()
    {
        SessionState.SetBool(Chapters, false);
        SessionState.SetBool(Feedback, true);
        SessionState.SetBool(Autumn, false);
        StartReview(true);
    }

    [MenuItem("Tools/Word Revision/Review Autumn Pickups")]
    public static void StartAutumnCaptures()
    {
        SessionState.SetBool(Chapters, false);
        SessionState.SetBool(Feedback, false);
        SessionState.SetBool(Autumn, true);
        StartReview(true);
    }

    [MenuItem("Tools/Word Revision/Review Seasonal Chapter Frames")]
    public static void StartChapterCaptures()
    {
        SessionState.SetBool(Feedback, false);
        SessionState.SetBool(Autumn, false);
        SessionState.SetBool(Chapters, true);
        StartReview(true);
    }

    [MenuItem("Tools/Word Revision/Review Summer Station Entrances")]
    public static void StartSummerCaptures()
    {
        SessionState.SetBool(Chapters, false);
        SessionState.SetBool(Feedback, false);
        SessionState.SetBool(Autumn, false);
        StartReview(true, true);
    }

    public static void StartAutumnIntegrationCaptures()
    {
        SessionState.SetBool(Chapters, false);
        SessionState.SetBool(Feedback, false);
        SessionState.SetBool(Autumn, false);
        SessionState.SetBool(AutumnIntegration, true);
        StartReview(true);
    }

    public static void StartImmersiveCaptures()
    {
        SessionState.SetBool(CocoonOnly, false);
        if(File.Exists("../WordRevision/immersive-review-complete.txt")) File.Delete("../WordRevision/immersive-review-complete.txt");
        SessionState.SetBool(Chapters, false);
        SessionState.SetBool(Feedback, false);
        SessionState.SetBool(Autumn, false);
        SessionState.SetBool(AutumnIntegration, false);
        SessionState.SetBool(Immersive, true);
        foreach (string name in new[] { "ReelingMachine", "ReelingMachineSupport", "ReelingMachineWheel", "SpringEggHeap", "SilkWrap" })
        {
            string path = "Assets/Resources/WordRevision/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing machine art: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        StartReview(true);
    }

    public static void StartCocoonReleaseCaptures()
    {
        File.Delete("../WordRevision/immersive-review-complete.txt");
        SessionState.SetBool(CocoonOnly, true);
        SessionState.SetBool(Immersive, false);
        StartReview(true);
    }

    [MenuItem("Tools/Word Revision/Review Latest Player Feedback")]
    public static void StartFeedback2Captures()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // The winter layout was repaired offline while this project had no Editor.
        // Round-trip that scene through the native serializer before scene validation.
        AssetDatabase.ForceReserializeAssets(new[] { "Assets/关卡/4-冬/Level4_Winter.unity" },
            ForceReserializeAssetsOptions.ReserializeAssets);
        File.Delete("../WordRevision/feedback2-review-complete.txt");
        SessionState.SetBool(Feedback2, true);
        SessionState.SetBool(CocoonOnly, false);
        SessionState.SetBool(Immersive, false);
        StartReview(true);
    }

    [MenuItem("Tools/Word Revision/Review Summer Reward Feedback")]
    public static void StartFeedback3Captures()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.ForceReserializeAssets(new[] { "Assets/关卡/3-秋/Level3_Autumn.unity" },
            ForceReserializeAssetsOptions.ReserializeAssets);
        File.Delete("../WordRevision/feedback3-review-complete.txt");
        SessionState.SetBool(Feedback3, true);
        StartReview(true);
    }

    private static void StartReview(bool feedbackOnly, bool summer = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (feedbackOnly) WordRevisionMaintenance.Validate();
        else WordRevisionMaintenance.PrepareArtAndValidate();
        SessionState.SetBool(Summer, summer);
        SessionState.SetString(Prior, PlayerPrefs.GetString(SaveKey, ""));
        SessionState.SetBool(Existed, PlayerPrefs.HasKey(SaveKey));
        SessionState.SetBool(Active, true);
        Directory.CreateDirectory("../WordRevision/Screenshots");
        File.WriteAllText("../WordRevision/visual-review-save-backup.json", JsonUtility.ToJson(new SaveBackup {
            existed = PlayerPrefs.HasKey(SaveKey), value = PlayerPrefs.GetString(SaveKey, "") }));
        ConfigureGameView();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        EditorApplication.isPlaying = true;
    }

    [Serializable] private sealed class SaveBackup { public bool existed; public string value; }

    private static void OnState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Active, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            if (SessionState.GetBool(Feedback3, false)) WorkshopWordPreview.BeginFeedback3();
            else if (SessionState.GetBool(Feedback2, false)) WorkshopWordPreview.BeginFeedback2();
            else if (SessionState.GetBool(CocoonOnly, false)) WorkshopWordPreview.BeginCocoonRelease();
            else if (SessionState.GetBool(Immersive, false)) WorkshopWordPreview.BeginImmersive();
            else if (SessionState.GetBool(AutumnIntegration, false)) WorkshopWordPreview.BeginAutumnIntegration();
            else if (SessionState.GetBool(Summer, false)) WorkshopWordPreview.BeginSummer();
            else if (SessionState.GetBool(Chapters, false)) WorkshopWordPreview.BeginChapters();
            else if (SessionState.GetBool(Autumn, false)) WorkshopWordPreview.BeginAutumn();
            else if (SessionState.GetBool(Feedback, false)) WorkshopWordPreview.BeginFeedback();
            else WorkshopWordPreview.Begin();
        }
        if (state != PlayModeStateChange.EnteredEditMode) return;
        if (SessionState.GetBool(Existed, false)) PlayerPrefs.SetString(SaveKey, SessionState.GetString(Prior, ""));
        else PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        bool latestFeedback = SessionState.GetBool(Feedback2, false);
        bool summerRewardFeedback = SessionState.GetBool(Feedback3, false);
        SessionState.SetBool(Active, false);
        SessionState.SetBool(Feedback2, false);
        SessionState.SetBool(Feedback3, false);
        SessionState.SetBool(AutumnIntegration, false);
        SessionState.SetBool(Immersive, false);
        SessionState.SetBool(CocoonOnly, false);
        bool quit = Array.IndexOf(Environment.GetCommandLineArgs(), "-quitAfterWordReview") >= 0;
        if (!quit) EditorSceneManager.OpenScene("Assets/关卡/3-秋/Level3_Autumn.unity");
        Debug.Log("[WordRevision] Visual review finished; original player save restored.");
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-buildAfterWordReview") >= 0)
        {
            string marker = summerRewardFeedback ? "../WordRevision/feedback3-review-complete.txt"
                : latestFeedback ? "../WordRevision/feedback2-review-complete.txt" : "../WordRevision/immersive-review-complete.txt";
            if (!File.Exists(marker))
            {
                Debug.LogError("Rendered review failed; build withheld.");
                if (quit) EditorApplication.delayCall += () => EditorApplication.Exit(1);
                return;
            }
            WordRevisionMaintenance.ValidateAndBuild();
        }
        if (quit)
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }

    private static void ConfigureGameView()
    {
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var kind = assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(kind, "FixedResolution"), (object)1920, 1080, "Word Review 1920 x 1080" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        var viewType = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, count - 1);
        view.Show();
        view.Focus();
    }
}
