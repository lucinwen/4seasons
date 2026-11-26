#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Qingming.LevelBackground;

/// <summary>Only in development builds, in a separate QA product/save namespace.</summary>
public sealed class XiazhiQaFixture : MonoBehaviour
{
    private float nextSnapshot;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Application.productName != "四时织坊_春夏测试" ||
            Array.IndexOf(Environment.GetCommandLineArgs(), "--xiazhi-qa") < 0) return;
        var host = new GameObject("Xiazhi QA Fixture");
        DontDestroyOnLoad(host);
        host.AddComponent<XiazhiQaFixture>();
    }
    private IEnumerator Start()
    {
        WorkshopProgress.StartNewGame();
        WorkshopProgress.Award(WorkshopItem.MulberryLeaf);
        WorkshopProgress.Award(WorkshopItem.Silkworm);
        WorkshopProgress.Award(WorkshopItem.SilkwormEgg);
        WorkshopProgress.Submit(WorkshopSeason.Spring);
        WorkshopProgress.Award(WorkshopItem.Salt);
        WorkshopProgress.Award(WorkshopItem.Suoxu);
        WorkshopProgress.MarkStep("summer.water");
        WorkshopProgress.MarkStep("summer.wood");
        WorkshopProgress.MarkStep("quest.guide.Summer");
        yield return SceneManager.LoadSceneAsync("Level2_Summer");
        yield return null;
        var player = GameObject.Find("Player");
        var rack = FindObjectOfType<SummerCocoonRackTrigger>();
        if (player != null && rack != null)
        {
            Vector3 pos = player.transform.position;
            pos.x = rack.transform.position.x - 1.0f;
            player.transform.position = pos;
        }
        Debug.Log("[MangzhongQA] Fixture ready: original summer scene, player at rack, Spool absent, thread puzzle incomplete.");
        WriteSnapshot();
    }
    private void Update()
    {
        if (Time.unscaledTime < nextSnapshot) return;
        nextSnapshot = Time.unscaledTime + .5f;
        WriteSnapshot();
    }
    [Serializable] private sealed class Snapshot
    {
        public string scene, sample, quest;
        public bool overlay, xiazhiRecord, xiazhiOrder, salt, suoxu, spool, canSubmit, playerEnabled;
        public float timeScale, playerX, playerY;
        public int orderCount;
    }
    private static void WriteSnapshot()
    {
        var work = WorkshopProgress.SeasonalOrderWork("observation.summer.mangzhong.thread");
        var controller = FindObjectOfType<LevelPlayerController>();
        var quest = WorkshopQuests.NextReady(WorkshopSeason.Summer);
        var data = new Snapshot
        {
            scene = SceneManager.GetActiveScene().name, overlay = WorkshopObservationView.IsOpen,
            xiazhiRecord = WorkshopProgress.HasObservation("observation.summer.mangzhong.thread"),
            xiazhiOrder = work != null, sample = work?.sample, salt = WorkshopProgress.Has(WorkshopItem.Salt),
            suoxu = WorkshopProgress.Has(WorkshopItem.Suoxu), spool = WorkshopProgress.Has(WorkshopItem.Spool),
            canSubmit = WorkshopProgress.CanSubmit(WorkshopSeason.Summer), timeScale = Time.timeScale,
            playerEnabled = controller != null && controller.enabled,
            playerX = controller != null ? controller.transform.position.x : 0,
            playerY = controller != null ? controller.transform.position.y : 0,
            quest = quest?.Id,
            orderCount = work == null ? 0 : 1
        };
        File.WriteAllText(Path.Combine(Application.dataPath,"../xiazhi-qa-state.json"), JsonUtility.ToJson(data,true));
    }
}
#endif
