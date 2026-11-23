using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 春天场景道具触发器：角色穿过时弹出对应的文字提示，离开时隐藏。
/// 挂载到道具 GameObject 上，需要同物体有 BoxCollider2D (Is Trigger = true)。
/// 玩家需要有 Rigidbody2D + Collider2D 才能触发。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpringItemTrigger : MonoBehaviour
{
    [Header("提示内容")]
    [TextArea]
    public string promptText = "这是一个道具";

    [Header("UI 配置")]
    [Tooltip("场景中提示 UI 的 GameObject 名称")]
    public string promptUIName = "MissionPromptUI";

    [Tooltip("触发后多少秒自动隐藏 (0 = 玩家离开才隐藏)")]
    public float autoHideSeconds = 0f;

    [Header("调试")]
    public bool showDebugLog = true;

    private static SpringItemTrigger _currentShowing;
    private GameObject _promptUI;
    private Text _promptText;
    private Coroutine _autoHideCoroutine;

    private void Reset()
    {
        // 自动把 Collider2D 设为 Is Trigger
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // 尝试根据 GameObject 名字生成默认提示
        promptText = GenerateDefaultPrompt(gameObject.name);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase))
            return;

        ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.gameObject.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase))
            return;

        if (autoHideSeconds <= 0f)
            HidePrompt();
    }

    private void ShowPrompt()
    {
        // 如果有其他道具正在显示，先隐藏它
        if (_currentShowing != null && _currentShowing != this)
            _currentShowing.HidePromptImmediate();
        _currentShowing = this;

        if (_promptUI == null)
        {
            _promptUI = GameObject.Find(promptUIName);
            if (_promptUI == null)
            {
                if (showDebugLog) Debug.LogWarning($"[SpringItemTrigger] 没找到 UI '{promptUIName}'！");
                return;
            }
        }

        _promptUI.SetActive(true);
        if (_promptText == null) _promptText = _promptUI.GetComponentInChildren<Text>();
        if (_promptText != null) _promptText.text = promptText;

        if (showDebugLog) Debug.Log($"[SpringItemTrigger] 📝 显示: {promptText}");

        // 自动隐藏协程
        if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
        if (autoHideSeconds > 0f)
            _autoHideCoroutine = StartCoroutine(AutoHide());
    }

    private void HidePrompt()
    {
        if (_currentShowing == this) _currentShowing = null;
        HidePromptImmediate();
    }

    private void HidePromptImmediate()
    {
        if (_autoHideCoroutine != null) { StopCoroutine(_autoHideCoroutine); _autoHideCoroutine = null; }
        if (_promptUI != null)
        {
            _promptUI.SetActive(false);
            if (showDebugLog) Debug.Log("[SpringItemTrigger] 📝 隐藏");
        }
    }

    private System.Collections.IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHideSeconds);
        if (_currentShowing == this) HidePrompt();
    }

    /// <summary>
    /// 根据道具名字生成默认的中文提示。
    /// </summary>
    public static string GenerateDefaultPrompt(string objName)
    {
        // 去掉末尾的 "(1)" "(2)" 等编号
        string cleanName = System.Text.RegularExpressions.Regex.Replace(objName, @"\s*\(\d+\)\s*$", "");

        // 去掉前缀编号如 "1-"
        if (cleanName.Length > 2 && cleanName[1] == '-')
            cleanName = cleanName.Substring(2);

        // 关键字映射
        if (cleanName.Contains("桑树")) return "桑树：桑叶是蚕的主要食物";
        if (cleanName.Contains("采桑篮")) return "采桑篮：用来采集桑叶的篮子";
        if (cleanName.Contains("浴蚕桶")) return "浴蚕桶：用于清洗蚕种的木桶";
        if (cleanName.Contains("蚕种清洗前")) return "蚕种：需要清洗消毒后才能孵化";
        if (cleanName.Contains("蚕种清洗后")) return "清洗后的蚕种：已消毒，可以孵化";
        if (cleanName.Contains("蚕种纸")) return "蚕种纸：蚕卵产在纸上";
        if (cleanName.Contains("蚕匾架")) return "蚕匾架：用来放置蚕匾的架子";
        if (cleanName.Contains("蚕匾")) return "蚕匾：饲养蚕的竹制器具";
        if (cleanName.Contains("蚕簇架")) return "蚕簇架：供蚕结茧的架子";
        if (cleanName.Contains("蚕簇")) return "蚕簇：供蚕吐丝结茧的结构";
        if (cleanName.Contains("桑叶")) return "桑叶：蚕最喜欢的食物";

        return $"这是 {cleanName}";
    }
}
