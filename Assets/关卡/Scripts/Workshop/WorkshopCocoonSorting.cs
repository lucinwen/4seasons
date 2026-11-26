using UnityEngine;
using UnityEngine.UI;

/// <summary>Sort a short run of cocoons by following the dark tiles, from the bottom row.</summary>
internal sealed class WorkshopCocoonSorting : MonoBehaviour
{
    internal const int Target = 20;
    private static WorkshopCocoonSorting current;
    internal static bool IsOpen => current != null;
    private readonly int[] columns = new int[Target + 4];
    private RectTransform frame, board;
    private Text status;
    private float previousTime;
    private int collected;
    private bool failed, completed;
    private GameObject previousSelection;

    internal static void Show()
    {
        if (IsOpen || MiniGameOverlay.IsActive || !WorkshopProgress.Has(WorkshopItem.Suoxu)) return;
        var canvas = WorkshopUi.NewCanvas("Cocoon Sorting Tiles", 24000);
        current = canvas.gameObject.AddComponent<WorkshopCocoonSorting>();
        current.frame = canvas;
        current.previousTime = Time.timeScale;
        if (UnityEngine.EventSystems.EventSystem.current != null)
            current.previousSelection = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        Time.timeScale = 0f;
        current.Build();
    }

    private void Build()
    {
        WorkshopImmersive.Background(frame, WorkshopSeason.Summer);
        WorkshopUi.Block(frame, new Color32(16, 33, 27, 65));
        var title = WorkshopUi.PixelCard(frame, "Sorting Instructions", new Rect(390, 55, 1140, 171), WorkshopUi.Paper, WorkshopUi.Bronze);
        WorkshopUi.Label(title, "Sorting Title", new Rect(35, 16, 1070, 52), "立夏择茧 · 别踩白块", 39,
            WorkshopUi.Green, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
        WorkshopUi.Label(title, "Sorting Explanation", new Rect(35, 77, 1070, 65),
            "择茧是将蚕茧分类的重要步骤。\n从最下行开始，依次点击深色茧块，避开白块。", 27,
            WorkshopUi.Ink, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
        board = WorkshopUi.Rect(frame, "Sorting Board", new Rect(510, 254, 900, 616));
        status = WorkshopImmersive.Text(frame, "Sorting Progress", new Rect(510, 889, 900, 55), "", 31, TextAnchor.MiddleCenter);
        WorkshopUi.PixelButton(frame, "Leave Cocoon Sorting", new Rect(1635, 45, 240, 65), "返回工位", Close, false, 27);
        WorkshopUi.PixelButton(frame, "Retry Cocoon Sorting", new Rect(800, 968, 320, 68), "重新择茧", Restart, false, 29);
        Restart();
        foreach (var label in frame.GetComponentsInChildren<Text>(true)) label.font = WorkshopArt.ReadingFont;
    }

    private void Restart()
    {
        collected = 0; failed = completed = false;
        for (int i = 0; i < columns.Length; i++) columns[i] = Random.Range(0, 4);
        Draw();
    }

    private void Draw()
    {
        for (int i = board.childCount - 1; i >= 0; i--)
        {
            var child = board.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
        for (int row = 0; row < 4; row++)
        {
            int sequence = collected + 3 - row;
            for (int col = 0; col < 4; col++)
            {
                int r = row, c = col;
                bool cocoon = sequence < Target && columns[sequence] == col;
                var cell = WorkshopUi.Button(board, "Sort Cell " + row + " " + col,
                    new Rect(col * 225 + 4, row * 154 + 4, 217, 146),
                    cocoon ? "蚕茧" : "", () => Tap(r, c),
                    cocoon ? WorkshopUi.Green : new Color32(249, 239, 213, 255),
                    size: 34, font: WorkshopArt.ReadingFont);
                foreach (var text in cell.GetComponentsInChildren<Text>()) text.color = cocoon ? WorkshopUi.Cream : WorkshopUi.Ink;
                cell.interactable = !failed && !completed && sequence < Target;
            }
        }
        status.text = failed ? "踩到白块了，点「重新择茧」再试一次。"
            : completed ? "择茧完成！二十枚蚕茧已分类，点击收取立夏卡。"
            : "已择 " + collected + " / " + Target + " 枚 · 从最下行开始";
        if (completed)
        {
            var finish = WorkshopUi.PixelButton(board, "Finish Cocoon Sorting", new Rect(165, 232, 570, 100), "完成择茧 · 收取立夏卡", Finish, true, 32);
            foreach (var label in finish.GetComponentsInChildren<Text>()) label.font = WorkshopArt.ReadingFont;
        }
    }

    private void Tap(int row, int column)
    {
        if (failed || completed || row != 3) return;
        if (columns[collected] != column)
        {
            failed = true;
            Draw();
            return;
        }
        WorkshopAudioFeedback.Progress();
        collected++;
        completed = collected >= Target;
        Draw();
    }

    private void Finish()
    {
        if (!completed) return;
        WorkshopProgress.MarkStep(WorkshopWordRules.Sorted);
        Close();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Close()
    {
        if (current != this) return;
        current = null;
        Time.timeScale = previousTime;
        WorkshopUi.CloseCanvas(frame);
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null && previousSelection != null && previousSelection.activeInHierarchy)
            events.SetSelectedGameObject(previousSelection);
    }

    private void OnDestroy()
    {
        if (current != this) return;
        current = null;
        Time.timeScale = previousTime;
    }
}
