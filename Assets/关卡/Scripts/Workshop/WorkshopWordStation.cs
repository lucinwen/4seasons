using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Qingming.LevelBackground;

/// <summary>Restore the spring feeding entry without the retired multi-stage Guyu game.</summary>
internal sealed class WorkshopWordStation : MonoBehaviour
{
    private FloatingActionButton button;
    private Transform player;
    private bool shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= Attach;
        SceneManager.sceneLoaded += Attach;
        Attach(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void Attach(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || scene.name != "Level1_Spring") return;
        // The tall feeding rack sits beside the stone steps; the short racks on
        // the raised platform belong to washing and must not host this action.
        var rack = GameObject.Find("1-蚕簇架1 (1)") ?? GameObject.Find("1-蚕簇架1");
        if (rack != null && rack.GetComponent<WorkshopWordStation>() == null) rack.AddComponent<WorkshopWordStation>();
    }

    private void Start()
    {
        button = gameObject.AddComponent<FloatingActionButton>();
        button.Configure("谷雨 · 喂蚕", new Vector3(0, 2.6f, 0), OpenFeeding);
        var hit = GetComponent<Collider2D>();
        if (hit != null) { hit.enabled = true; hit.isTrigger = true; }
        WorkshopPickupGlow.Bind(gameObject, () => WorkshopWordRules.CanFeed && !WorkshopProgress.HasStep(WorkshopWordRules.Fed));
    }

    private void OnMouseDown()
    {
        if (!shown || WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        OpenFeeding();
    }
    private void Update()
    {
        if (player == null) { var go = GameObject.Find("Player"); if (go != null) player = go.transform; }
        bool nearby = player != null && Vector2.Distance(player.position, transform.position) < 3.4f &&
            !WorkshopQuestGuide.BlocksWorldInput && !WorkshopQuestGuide.IsWorldOverlayActive;
        if (nearby != shown) { shown = nearby; if (shown) button.Show(); else button.Hide(); }
        if (nearby && Input.GetKeyDown(KeyCode.O)) OpenFeeding();
    }

    private static void OpenFeeding()
    {
        if (WorkshopWordRules.HasDelivery(WorkshopSeason.Spring))
            SimplePopup.Show("喂蚕已完成", "蚕宝宝已经养好，不用再喂一轮。带蚕种、桑叶和蚕宝宝回工坊提交春季成果。", "知道了");
        else WorkshopSimpleGames.Show(WorkshopSimpleGame.Feeding);
    }
}
