#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class WorkshopWordPreview
{
    /// <summary>Standalone entry; the Editor review host owns restoring the user's save.</summary>
    public static void BeginSummerBridgePassage()
    {
        var host = new GameObject("Summer Bridge Physical Review");
        DontDestroyOnLoad(host);
        host.AddComponent<WorkshopWordPreview>().StartCoroutine(RunSafely(ReviewSummerBridgeAndExit()));
    }

    private static IEnumerator ReviewSummerBridgeAndExit()
    {
        yield return ReviewSummerBridgePassage();
        File.WriteAllText("../WordRevision/summer-bridge-review-complete.txt",
            "Actual dynamic Rigidbody2D movement using SetWalkTarget: left approach, eastbound bridge, westbound bridge, eastbound repeat. No test teleport or physics bypass.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    /// <summary>Can also be yielded by another review. No transform or rigidbody position is assigned.</summary>
    internal static IEnumerator ReviewSummerBridgePassage()
    {
        PlayerPrefs.SetString("ShiXu.Workshop.v1",
            "{\"items\":0,\"chapters\":1,\"seasonalOrder\":true,\"steps\":[\"quest.guide.Summer\"]}");
        WorkshopProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(.7f);

        var player = UnityEngine.Object.FindObjectOfType<LevelPlayerController>();
        BridgeRequire(player != null && player.gameObject.scene == SceneManager.GetActiveScene(), "Summer player is missing.");
        var body = player.GetComponent<Rigidbody2D>();
        BridgeRequire(body != null && body.simulated && body.bodyType == RigidbodyType2D.Dynamic,
            "Bridge test must use the live dynamic player body.");
        foreach (var npc in UnityEngine.Object.FindObjectsOfType<NpcTalkButton>())
            if (npc.gameObject.scene == player.gameObject.scene)
                foreach (var collider in npc.GetComponentsInChildren<Collider2D>())
                    BridgeRequire(!collider.enabled || collider.isTrigger,
                        "The guide must not block the bridge: " + collider.name);
        BridgeRequire(!WorkshopQuestGuide.BlocksWorldInput && !WorkshopQuestGuide.IsWorldOverlayActive,
            "An overlay is preventing the physical bridge test.");

        player.SetControlEnabled(true);
        player.clickToMoveEnabled = true;
        // Walk to the left approach from the authored spawn before traversing.
        yield return WalkAcrossSummerBridge(player, -3f, "left-approach", false);
        yield return Capture("90-summer-bridge-left-approach");
        yield return WalkAcrossSummerBridge(player, 7.5f, "eastbound", true);
        yield return Capture("91-summer-bridge-eastbound");
        yield return WalkAcrossSummerBridge(player, -3f, "westbound", true);
        yield return Capture("92-summer-bridge-westbound");
        yield return WalkAcrossSummerBridge(player, 7.5f, "eastbound-repeat", true);
        yield return Capture("93-summer-bridge-repeat");
        Debug.Log("[SummerBridge] PASS: SetWalkTarget moved the dynamic player across the original six bridge treads in both directions, then repeated eastbound. NPC collider and fall/teleport checks passed.");
    }

    private static IEnumerator WalkAcrossSummerBridge(LevelPlayerController player, float target, string phase, bool mustCrossCrest)
    {
        var body = player.GetComponent<Rigidbody2D>();
        var shape = player.GetComponentInChildren<Collider2D>();
        BridgeRequire(shape != null, "The player collider is missing.");
        Debug.Log("[SummerBridge] " + phase + " start body=" + body.position +
            " shapeCenter=" + shape.bounds.center + " shapeSize=" + shape.bounds.size +
            " stepHeight=" + player.stepHeight + " groundProbe=" + player.groundCheckDistance);
        float started = Time.realtimeSinceStartup;
        float lastMotion = started;
        float lastX = body.position.x;
        float furthestMotion = lastX;
        float peakFeet = shape.bounds.min.y;
        float minFeet = peakFeet;
        float direction = Mathf.Sign(target - lastX);
        float nextDiagnostic = started + 1f;
        player.SetWalkTarget(target);
        while (Mathf.Abs(body.position.x - target) > .2f)
        {
            yield return new WaitForFixedUpdate();
            float now = Time.realtimeSinceStartup;
            float x = body.position.x;
            float delta = Mathf.Abs(x - lastX);
            BridgeRequire(delta < player.moveSpeed * Time.fixedDeltaTime * 2f + .2f,
                phase + " moved without ordinary horizontal physics: " + delta);
            BridgeRequire(body.simulated && body.bodyType == RigidbodyType2D.Dynamic,
                phase + " bypassed dynamic physics.");
            BridgeRequire(player.transform.position.y > player.fallResetY + 1f,
                phase + " fell below the bridge.");
            peakFeet = Mathf.Max(peakFeet, shape.bounds.min.y);
            minFeet = Mathf.Min(minFeet, shape.bounds.min.y);
            if ((x - furthestMotion) * direction > .04f) { lastMotion = now; furthestMotion = x; }
            if (now >= nextDiagnostic)
            {
                Debug.Log("[SummerBridge] " + phase + " progress=" + body.position + " feet=" + shape.bounds.min.y +
                    " stepDiagnostic=" + player.StepUpDiagnostic);
                nextDiagnostic = now + 1f;
            }
            if (now - lastMotion > 1.4f || now - started > 8f)
            {
                var contacts = new ContactPoint2D[16];
                int count = body.GetContacts(contacts);
                string detail = "";
                for (int i = 0; i < count; i++)
                    detail += " [" + contacts[i].collider.name + " / " + contacts[i].otherCollider.name +
                        " point=" + contacts[i].point + " normal=" + contacts[i].normal + "]";
                throw new InvalidOperationException("[SummerBridge] " + phase + " stuck at " + body.position +
                    " feet=" + shape.bounds.min.y + " shapeCenter=" + shape.bounds.center +
                    " shapeSize=" + shape.bounds.size + " target=" + target +
                    " stepDiagnostic=" + player.StepUpDiagnostic + " contacts:" + detail);
            }
            lastX = x;
        }
        player.SetWalkTarget(target);
        yield return new WaitForSecondsRealtime(.18f);
        BridgeRequire(!mustCrossCrest || peakFeet > -2.5f,
            phase + " did not climb over the original bridge crest: peak feet=" + peakFeet);
        BridgeRequire(minFeet > -3.6f,
            phase + " passed beneath the bridge instead of walking over it: min feet=" + minFeet);
        Debug.Log("[SummerBridge] " + phase + " reached " + body.position + " in " +
            (Time.realtimeSinceStartup - started).ToString("0.00") + "s; peakFeet=" + peakFeet.ToString("0.000"));
    }

    private static void BridgeRequire(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[SummerBridge] " + message);
    }
}
#endif
