using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Qingming.LevelBackground;

internal enum WorkshopSimpleGame { Feeding, Reeling, Weaving }

/// <summary>Small worktable interactions sharing the established paper-and-wood UI.</summary>
internal sealed class WorkshopSimpleGames : MonoBehaviour
{
    private static WorkshopSimpleGames current;
    internal static bool IsOpen => current != null;
    private RectTransform frame, work;
    private WorkshopSimpleGame game;
    private float previousTime;
    private bool finished;
    private int placed;
    private Text status;
    private Text closeLabel;
    private WorkshopPlainWeave weave;

    internal static void Show(WorkshopSimpleGame game)
    {
        if (IsOpen || MiniGameOverlay.IsActive) return;
        string missing = null;
        switch (game)
        {
            case WorkshopSimpleGame.Feeding when !WorkshopWordRules.CanFeed:
                missing = "先完成清明采桑和谷雨浴蚕，备好桑叶与干净蚕种。"; break;
            case WorkshopSimpleGame.Reeling when !WorkshopProgress.Has(WorkshopItem.Spool):
                missing = "先备水、拾柴，在迷宫里找到线轴。"; break;
            case WorkshopSimpleGame.Weaving when !AutumnLevelState.HasLoomRepaired || !WorkshopProgress.Has(WorkshopItem.Shuttle):
                missing = "先拾起织机左侧地上的木槌修好织机，再从工具箱取得梭子。"; break;
        }
        if (missing != null) { SimplePopup.Show("先备好材料", missing, "知道了"); return; }
        var root = WorkshopUi.NewCanvas("Simplified " + game, 24000);
        current = root.gameObject.AddComponent<WorkshopSimpleGames>();
        current.frame = root;
        current.game = game;
        current.previousTime = Time.timeScale;
        Time.timeScale = 0f;
        current.Build();
    }

    private void Build()
    {
        var season=game==WorkshopSimpleGame.Feeding?WorkshopSeason.Spring:game==WorkshopSimpleGame.Reeling?WorkshopSeason.Summer:WorkshopSeason.Autumn;
        WorkshopImmersive.Background(frame,season);
        var paper=WorkshopUi.Rect(frame,"Worktable",new Rect(0,0,1920,1080));
        var close=WorkshopImmersive.Button(paper,"Close",new Rect(1680,35,195,62),"返回工位",Close);
        closeLabel=close.GetComponentInChildren<Text>();
        work=WorkshopUi.Rect(paper,"Workspace",new Rect(150,345,1530,615));
        status=WorkshopImmersive.Text(paper,"Status",new Rect(45,35,1550,66),"");
        switch(game)
        {
            case WorkshopSimpleGame.Feeding:BuildFeeding();break;
            case WorkshopSimpleGame.Reeling:BuildReeling();break;
            case WorkshopSimpleGame.Weaving:weave=new WorkshopPlainWeave();DrawWeave();break;
        }
        ApplyReadingFont();
        foreach(var label in frame.GetComponentsInChildren<Text>(true))WorkshopImmersive.QuietText(label);
    }

    private void ApplyReadingFont()
    {
        foreach (var label in frame.GetComponentsInChildren<Text>(true)) label.font = WorkshopArt.ReadingFont;
    }

    private static Sprite Art(string name) => Resources.Load<Sprite>("WorkshopGameArt/" + name);

    private void BuildFeeding()
    {
        WorkshopUi.Label(work, "Instructions", new Rect(30, 0, 1470, 70),
            "把桑叶喂给小蚕。", 31, WorkshopUi.Ink);
        WorkshopUi.Picture(work, "Mulberry Bamboo Tray", new Rect(25, 120, 715, 430),
            Resources.Load<Sprite>("WordRevision/FeedingLeafTray"));
        var target = WorkshopUi.Rect(work, "Silkworm Tray", new Rect(820, 135, 570, 390));
        var tray = WorkshopUi.Picture(target, "Tray Art", new Rect(0, 0, 570, 350), Art("MatureSilkwormTray"));
        var reaction = target.gameObject.AddComponent<WorkshopFeedingReaction>();
        reaction.Bind(tray.rectTransform);
        WorkshopUi.Label(target, "Tray Label", new Rect(0, 350, 570, 40), "蚕匾", 28, WorkshopUi.Green, TextAnchor.MiddleCenter);
        for (int i = 0; i < 3; i++)
        {
            int portion = i;
            var leaf = WorkshopUi.Picture(work, "Mulberry Portion " + i, new Rect(100 + i * 170, 235, 145, 145), WorkshopObservationArt.Current?.Leaf);
            leaf.raycastTarget = true;
            var drag = leaf.gameObject.AddComponent<WorkshopWordDrag>();
            drag.Bind(frame, target, () =>
            {
                placed++;
                WorkshopUi.Picture(target, "Fed Mulberry " + portion,
                    new Rect(110 + portion * 115, 140 + (portion % 2) * 25, 100, 100),
                    WorkshopObservationArt.Current?.Leaf);
                reaction.Play();
                status.text = $"已喂 {placed} / 3 份桑叶。";
                if (placed == 3)
                {
                    WorkshopWordRules.CompleteFeeding();
                    Finish("小蚕吃饱了。");
                }
            });
        }
        status.text = "把三份桑叶拖到蚕匾里。";
    }

    private void BuildReeling()
    {
        var reeling=frame.gameObject.AddComponent<WorkshopReelingWork>();
        reeling.Build(frame,status,()=>
        {
            WorkshopProgress.MarkStep(WorkshopWordRules.Reeled);
            Finish("丝线绕匀了，收好线轴。");
        });
    }

    private void DrawWeave()
    {
        foreach (Transform child in work) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        WorkshopUi.Label(work, "Instructions", new Rect(45, 25, 400, 190),
            "奇数经与偶数经轮流抬起，\n梭子左右交替穿过。\n织满六行，获得生坯绸。", 28, WorkshopUi.Ink, TextAnchor.MiddleCenter);
        for (int row = 0; row < 6; row++)
            for (int col = 0; col < 6; col++)
                WorkshopUi.Fill(work, $"Weave {row},{col}", new Rect(565 + col * 62, 110 + row * 62, 58, 58),
                    row < weave.WovenRows ? ((row + col) % 2 == 0 ? new Color32(213, 173, 115, 255) : new Color32(245, 225, 185, 255))
                        : new Color32(191, 179, 156, 255));
        for (int i = 0; i < 2; i++)
        {
            int shed = i;
            WorkshopUi.PixelButton(work, "Shed " + i, new Rect(65, 250 + i * 140, 360, 85),
                (i == 0 ? "抬起奇数经" : "抬起偶数经") + (weave.SelectedShed == i ? " · 已选" : ""),
                () => { weave.Select(shed); DrawWeave(); }, weave.SelectedShed == i, 31);
        }
        WorkshopUi.Picture(work, "Shuttle Art", new Rect(1110, 110, 250, 130), Art("Shuttle"));
        WorkshopUi.PixelButton(work, "Pass Right", new Rect(1090, 290, 355, 78), "向右穿梭 →", () => Pass(true), true, 30);
        WorkshopUi.PixelButton(work, "Pass Left", new Rect(1090, 400, 355, 78), "← 向左穿梭", () => Pass(false), true, 30);
        status.text = weave.Complete ? "六行平纹已织好。" : $"第 {weave.WovenRows + 1} / 6 行：抬起{(weave.RequiredShed == 0 ? "奇数" : "偶数")}经，再向{(weave.RequiredShed == 0 ? "右" : "左")}穿梭。";
        ApplyReadingFont();
    }

    private void Pass(bool right)
    {
        if (finished) return;
        if (!weave.Pass(right)) { status.text = "经线或方向还没对上，请按本行提示调整后再穿梭。"; return; }
        WorkshopAudioFeedback.Progress();
        DrawWeave();
        if (weave.Complete)
        {
            WorkshopWordRules.CompleteWeaving();
            Finish("奇偶推梭完成，获得生坯绸。接下来到青染轩研磨染材、选择喜欢的颜色染布。");
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))Close();
    }

    private void Finish(string message)
    {
        finished = true;
        status.text = message;
        if (closeLabel != null) closeLabel.text = "完成并返回";
        WorkshopAudioFeedback.Milestone();
        if (game == WorkshopSimpleGame.Feeding)
        {
            var popup = SimplePopup.Show("谷雨喂蚕完成", "三份桑叶已喂好，小蚕吃饱了。\n获得蚕宝宝，可以带着春季成果回工坊。", "完成并返回", Close);
            // This worktable uses a high overlay order; keep its completion
            // confirmation above the table until the player returns.
            if (popup != null) popup.GetComponent<Canvas>().sortingOrder = 24100;
        }
        else if (game == WorkshopSimpleGame.Weaving)
        {
            var popup = SimplePopup.ShowItem(WorkshopItem.GreigeCloth,
                "奇偶推梭完成，获得生坯绸。接下来去青染轩染色。", Close, fallbackIcon: WorkshopArt.RawSilk);
            if (popup != null) popup.GetComponent<Canvas>().sortingOrder = 24100;
        }
    }

    private void Close()
    {
        if (current != this) return;
        current = null;
        Time.timeScale = previousTime;
        WorkshopUi.CloseCanvas(frame);
    }
    private void OnDestroy() { if (current == this) { current = null; Time.timeScale = previousTime; } }
}

/// <summary>Small, unscaled feeding response that never intercepts dragging.</summary>
internal sealed class WorkshopFeedingReaction : MonoBehaviour
{
    private static Sprite heartSprite;
    private RectTransform tray;
    private Vector3 restingScale;
    private Coroutine pulse;

    internal void Bind(RectTransform art)
    {
        tray = art;
        // Keep the artwork in place while allowing it to respond around its centre.
        tray.pivot = new Vector2(0.5f, 0.5f);
        tray.anchoredPosition = new Vector2(285f, -175f);
        restingScale = tray.localScale;
    }

    internal void Play()
    {
        if (pulse != null) StopCoroutine(pulse);
        pulse = StartCoroutine(Respond());
        Vector2[] positions = { new Vector2(128f, 140f), new Vector2(320f, 78f), new Vector2(452f, 235f) };
        for (int i = 0; i < positions.Length; i++)
        {
            float size = i == 1 ? 58f : 46f;
            var heart = WorkshopUi.Picture(transform, "Feeding Heart", new Rect(0, 0, size, size), HeartSprite());
            heart.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            heart.rectTransform.anchoredPosition = new Vector2(positions[i].x, -positions[i].y);
            heart.color = Color.clear;
            StartCoroutine(FloatHeart(heart, i));
        }
    }

    private IEnumerator Respond()
    {
        float elapsed = 0f;
        const float duration = 0.55f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            tray.localScale = restingScale * (1f + Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * 0.025f);
            yield return null;
        }
        tray.localScale = restingScale;
        pulse = null;
    }

    private IEnumerator FloatHeart(Image heart, int index)
    {
        yield return new WaitForSecondsRealtime(index * 0.1f);
        var rect = heart.rectTransform;
        Vector2 start = rect.anchoredPosition;
        float elapsed = 0f;
        const float duration = 1.5f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float appear = Mathf.Clamp01(t / 0.12f);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, t));
            heart.color = new Color(1f, 1f, 1f, appear * fade);
            rect.anchoredPosition = start + new Vector2(Mathf.Sin(t * Mathf.PI) * (index - 1) * 22f, t * (100f + index * 13f));
            rect.localScale = Vector3.one * (0.55f + Mathf.SmoothStep(0f, 1f, appear) * 0.45f);
            yield return null;
        }
        Destroy(heart.gameObject);
    }

    private static Sprite HeartSprite()
    {
        if (heartSprite != null) return heartSprite;
        string[] rows = {
            "00111011100", "01222122210", "12322222221", "12222222221", "01222222210",
            "00122222100", "00012221000", "00001210000", "00000100000"
        };
        var texture = new Texture2D(11, 9, TextureFormat.RGBA32, false) {
            name = "Feeding Pixel Heart", filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
        };
        var pixels = new Color32[99];
        Color32[] palette = { new Color32(0, 0, 0, 0), new Color32(151, 54, 59, 255),
            new Color32(234, 112, 124, 255), new Color32(255, 213, 193, 255) };
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < 11; x++) pixels[(8 - y) * 11 + x] = palette[rows[y][x] - '0'];
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        heartSprite = Sprite.Create(texture, new Rect(0, 0, 11, 9), new Vector2(0.5f, 0.5f), 11f);
        heartSprite.hideFlags = HideFlags.HideAndDontSave;
        return heartSprite;
    }
}

internal sealed class WorkshopWordDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform frame, target, rect;
    private Vector2 start, grabOffset;
    private Action accepted;
    private bool placed;
    internal void Bind(RectTransform frame, RectTransform target, Action accepted)
    {
        this.frame = frame; this.target = target; this.accepted = accepted;
        rect = (RectTransform)transform;
        start = rect.anchoredPosition;
    }
    public void OnBeginDrag(PointerEventData data)
    {
        if (placed) return;
        rect.SetAsLastSibling();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect.parent as RectTransform,
            data.position, data.pressEventCamera, out var point))
            grabOffset = point - (Vector2)rect.localPosition;
    }
    public void OnDrag(PointerEventData data)
    {
        if (placed) return;
        var parent = rect.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position, data.pressEventCamera, out var point))
            rect.localPosition = new Vector3(point.x - grabOffset.x, point.y - grabOffset.y, rect.localPosition.z);
    }
    public void OnEndDrag(PointerEventData data)
    {
        if (placed) return;
        if (RectTransformUtility.RectangleContainsScreenPoint(target, data.position, data.pressEventCamera))
        {
            placed = true;
            gameObject.SetActive(false);
            accepted?.Invoke();
            WorkshopAudioFeedback.Progress();
        }
        else rect.anchoredPosition = start;
    }
}
