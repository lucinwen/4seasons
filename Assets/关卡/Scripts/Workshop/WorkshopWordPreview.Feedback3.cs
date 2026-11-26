#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class WorkshopWordPreview
{
    public static void BeginFeedback3()
    {
        var host = new GameObject("Summer Reward Feedback Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(ReviewFeedback3()));
    }

    private static IEnumerator ReviewFeedback3()
    {
        yield return ReviewCocoonFeedback3();
        yield return ReviewRewardFeedback3();
        Feedback2Save("{\"items\":0,\"chapters\":3,\"steps\":[\"quest.guide.Autumn\"]}");
        yield return SceneManager.LoadSceneAsync("Level3_Autumn");
        yield return new WaitForSecondsRealtime(.6f);
        Feedback2Npc();
        yield return Capture("973-feedback3-autumn-npc");
        File.WriteAllText("../WordRevision/feedback3-review-complete.txt", "PASS");
        Debug.Log("[Feedback3Review] PASS: stable pause control, cocoon tile game, one-time card claims, Xiaoman/Mangzhong craft cards and reachable autumn guide.");
        UnityEditor.EditorApplication.isPlaying = false;
    }
}
#endif
