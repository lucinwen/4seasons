#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

public sealed partial class WorkshopWordPreview
{
    public static void BeginFeedback4()
    {
        var host = new GameObject("Summer Reward Callback Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(ReviewFeedback4()));
    }

    private static IEnumerator ReviewFeedback4()
    {
        yield return ReviewSummerRewardFeedback4();
        yield return ReviewRewardFeedback3();
        File.WriteAllText("../WordRevision/feedback4-review-complete.txt", "PASS");
        Debug.Log("[Feedback4Review] PASS: real summer completion callbacks, one accepted item card, deferred duplicate continuations, thread-only Mangzhong reward and saved receipts.");
        UnityEditor.EditorApplication.isPlaying = false;
    }
}
#endif
