using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 春季节点关卡道具生成 Editor 工具。
/// </summary>
public static class SpringLevelSetup
{
    // Unity 编译完成后自动执行一次，确保光圈/提示 100% 就位
    [InitializeOnLoadMethod]
    private static void AutoRun()
    {
        EditorApplication.delayCall += () =>
        {
            // A delayed editor callback can land after Play Mode has started.
            // Scene rebuilding is edit-mode-only and would otherwise log a red error.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;
            if (!scene.path.EndsWith("Level1_Spring.unity")) return;

            Debug.Log("[SpringLevelSetup] 🔄 自动校准光圈/位置/脚本...");
            OneClickAll();
        };
    }

    [MenuItem("Tools/四季关卡/春/生成采桑关卡道具")]
    public static void Generate()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[SpringLevelSetup] 当前没有打开任何场景！");
            return;
        }

        // ---- 1. 采桑篮 Sprite ----
        string basketPath = "Assets/关卡/1-春/1-采桑game/1-采桑篮1.png";
        var basketSprite = AssetDatabase.LoadAssetAtPath<Sprite>(basketPath);
        if (basketSprite == null)
        {
            Debug.LogError($"[SpringLevelSetup] 找不到采桑篮图片: {basketPath}");
            return;
        }

        var basketGo = new GameObject("1-采桑篮1");
        basketGo.transform.position = new Vector3(0, -3.5f, 0); // 默认位置，你手动调
        basketGo.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
        var basketSr = basketGo.AddComponent<SpriteRenderer>();
        basketSr.sprite = basketSprite;
        basketSr.sortingOrder = 6;
        var pickup = basketGo.AddComponent<PickupItem>();
        pickup.itemName = "采桑篮";
        pickup.triggerDistance = 2.5f;
        pickup.labelHeight = 1.2f;
        pickup.destroyOnPickup = true;
        Undo.RegisterCreatedObjectUndo(basketGo, "Create Basket");

        // ---- 2. 光圈触发点 ----
        var triggerGo = new GameObject("采桑游戏触发点位");
        triggerGo.transform.position = new Vector3(-7.12f, -0.78f, 0); // 手动调整后的位置
        triggerGo.transform.localScale = Vector3.one;
        var triggerCol = triggerGo.AddComponent<BoxCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector2(2.71f, 1.86f);
        triggerCol.offset = new Vector2(0.86f, -0.43f);
        triggerGo.AddComponent<SpringPrompt>();

        // 光圈视觉：一个圆形 SpriteRenderer（用 Unity 内置的 circle 或者简单的淡色方块）
        // 用一个半透明的大圆圈做视觉标记
        var ringGo = new GameObject("光圈视觉");
        ringGo.transform.SetParent(triggerGo.transform, false);
        ringGo.transform.localPosition = Vector3.zero;
        ringGo.transform.localScale = new Vector3(0.08f, 0.04f, 1f);
        var ringSr = ringGo.AddComponent<SpriteRenderer>();
        // 用 white texture + color tint
        ringSr.sprite = Sprite.Create(
            new Texture2D(4, 4), new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        ringSr.color = new Color(1f, 0.85f, 0.4f, 0.6f); // 金黄色半透明
        ringSr.sortingOrder = 4;
        ringGo.SetActive(false);

        Undo.RegisterCreatedObjectUndo(triggerGo, "Create Trigger");

        // ---- 3. 提示 UI Canvas ----
        var canvasGo = new GameObject("MissionPromptUI");
        canvasGo.transform.position = new Vector3(0, 2f, 0); // 屏幕上方
        canvasGo.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 120);

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        // Text
        var textGo = new GameObject("PromptText");
        textGo.transform.SetParent(canvasGo.transform, false);
        var text = textGo.AddComponent<Text>();
        text.text = "拾取采桑篮后，进入桑园完成采桑";
        text.font = WorkshopArt.PixelFont;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var shadow = textGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.85f);
        shadow.effectDistance = new Vector2(2, -2);

        canvasGo.SetActive(false); // 默认隐藏
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create PromptUI");

        // 如果场景里没有 EventSystem，加一个
        if (GameObject.Find("EventSystem") == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        Debug.Log($"[SpringLevelSetup] ✅ 生成完成！\n  🧺 采桑篮: {basketGo.name}\n  ⭕ 光圈触发: {triggerGo.name}\n  📝 提示 UI: {canvasGo.name}\n  ⚠️ 位置是默认值，请在 Scene 视图里手动拖动调整！");
    }

    /// <summary>
    /// 只给已放置好的采桑篮补挂脚本，并生成光圈 + 提示 UI。
    /// 不动任何已有 GameObject 的位置。
    /// </summary>
    [MenuItem("Tools/四季关卡/春/补挂脚本+生成光圈提示")]
    public static void SetupPromptOnly()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[SpringLevelSetup] 当前没有打开任何场景！");
            return;
        }

        // ---- 1. 给场景里所有叫 1-采桑篮1 的 GameObject 补挂 PickupItem ----
        int basketsUpdated = 0;
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name.StartsWith("1-采桑篮1"))
            {
                var pickup = go.GetComponent<PickupItem>();
                if (pickup == null)
                {
                    pickup = go.AddComponent<PickupItem>();
                    Undo.RegisterCreatedObjectUndo(pickup, "Add PickupItem");
                    Debug.Log($"  ✅ 给 {go.name} 补挂了 PickupItem");
                }
                pickup.itemName = "采桑篮";
                basketsUpdated++;
            }
        }
        if (basketsUpdated == 0)
            Debug.LogWarning("[SpringLevelSetup] 场景里没找到采桑篮，请确认 GameObject 名字是 '1-采桑篮1'");

        // 如果已经有触发点了，跳过
        if (GameObject.Find("光圈触发点") != null || GameObject.Find("采桑游戏触发点位") != null)
        {
            Debug.Log("[SpringLevelSetup] ⚠️ 触发点已存在，跳过生成");
            return;
        }

        // ---- 2. 光圈触发点 ----
        var triggerGo = new GameObject("采桑游戏触发点位");
        triggerGo.transform.position = new Vector3(-7.12f, -0.78f, 0); // 手动调整后的位置
        triggerGo.transform.localScale = Vector3.one;
        var triggerCol = triggerGo.AddComponent<BoxCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector2(2.71f, 1.86f);
        triggerCol.offset = new Vector2(0.86f, -0.43f);
        triggerGo.AddComponent<SpringPrompt>();

        // 光圈视觉（半透明白方块当光圈，sortingOrder 在背景之上）
        var ringGo = new GameObject("光圈视觉");
        ringGo.transform.SetParent(triggerGo.transform, false);
        ringGo.transform.localPosition = Vector3.zero;
        ringGo.transform.localScale = new Vector3(0.08f, 0.04f, 1f);
        var ringSr = ringGo.AddComponent<SpriteRenderer>();
        ringSr.sprite = Sprite.Create(
            new Texture2D(4, 4), new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        ringSr.color = new Color(1f, 0.85f, 0.4f, 0.6f); // 金黄色半透明
        ringSr.sortingOrder = 4;
        ringGo.SetActive(false);

        Undo.RegisterCreatedObjectUndo(triggerGo, "Create Trigger");

        // ---- 3. 提示 UI Canvas ----
        var canvasGo = new GameObject("MissionPromptUI");
        canvasGo.transform.position = new Vector3(0, 2f, 0);
        canvasGo.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 120);
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        var textGo = new GameObject("PromptText");
        textGo.transform.SetParent(canvasGo.transform, false);
        var text = textGo.AddComponent<Text>();
        text.text = "拾取采桑篮后，进入桑园完成采桑";
        text.font = WorkshopArt.PixelFont;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var shadow = textGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.85f);
        shadow.effectDistance = new Vector2(2, -2);

        canvasGo.SetActive(false);
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create PromptUI");

        // EventSystem
        if (GameObject.Find("EventSystem") == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        Debug.Log($"[SpringLevelSetup] ✅ 脚本补挂 + 光圈提示 生成完成！\n  ⭕ 光圈触发点（请手动拖位置）\n  📝 提示 UI（请手动拖位置）\n  ⚠️ 光圈默认位置 (-3, -2.5, 0)，请到 Scene 视图拖到桑树林入口位置");
    }

    /// <summary>
    /// 一键全部搞定：补挂脚本 + 生成光圈 + 自动调位置 + 保存场景。
    /// 设计给 batchmode 调用。
    /// </summary>
    [MenuItem("Tools/四季关卡/春/一键全部搞定(batchmode)")]
    public static void OneClickAll()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[SpringLevelSetup] 当前没有打开任何场景！");
            return;
        }

        // ---- 0. 先跑 SetupPromptOnly（补挂脚本+生成光圈提示） ----
        SetupPromptOnly();

        // ---- 1. 找 Player 位置，以它为参考 ----
        var player = GameObject.Find("Player");
        Vector3 playerPos = player != null ? player.transform.position : new Vector3(-5f, -3.8f, 0);
        Debug.Log($"[SpringLevelSetup] Player 位置: {playerPos}");

        // ---- 2. 触发点（保留用户手动设置的位置，不覆盖） ----
        var trigger = GameObject.Find("光圈触发点");
        if (trigger == null) trigger = GameObject.Find("采桑游戏触发点位");
        if (trigger != null)
        {
            Debug.Log($"[SpringLevelSetup] 触发点位置保留: {trigger.transform.position}");
        }

        // ---- 3. 调整 MissionPromptUI 位置（光圈上方） ----
        var promptUI = GameObject.Find("MissionPromptUI");
        if (promptUI != null)
        {
            promptUI.transform.position = new Vector3(0f, 2f, 0);
            Debug.Log($"[SpringLevelSetup] 提示 UI → (0, 2, 0)");
        }

        // ---- 4. 把采桑篮移到光圈旁边（近到能触发，远到光圈入口） ----
        var allBaskets = new System.Collections.Generic.List<GameObject>();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name.StartsWith("1-采桑篮1") || go.name.StartsWith("1-\u91C7\u6851\u7BEE1"))
                allBaskets.Add(go);
        }

        if (allBaskets.Count > 0)
        {
            // 保留用户手动调整的采桑篮位置，不覆盖
            Debug.Log($"[SpringLevelSetup] 采桑篮 {allBaskets[0].name} 位置保留: {allBaskets[0].transform.position}");

            // 其他副本隐藏
            for (int i = 1; i < allBaskets.Count; i++)
            {
                allBaskets[i].SetActive(false);
                Debug.Log($"[SpringLevelSetup] 隐藏重复采桑篮 {allBaskets[i].name}");
            }
        }

        // ---- 5. 保存场景 ----
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[SpringLevelSetup] ✅ 场景已保存！");
    }
}
