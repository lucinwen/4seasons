using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Collect intact cocoons from continuously falling rows.</summary>
internal sealed class WorkshopCocoonSorting : MonoBehaviour
{
    internal const int Target = 20;
    private const int LaneCount = 4;
    private const float LaneWidth = 228f;
    private const float BoardWidth = LaneCount * LaneWidth;
    private const float BoardHeight = 620f;
    private const float TileHeight = 142f;
    private const float RowSpacing = 152f;
    private const float FirstRowTop = 403f;
    private const float StartingSpeed = 230f;
    private const float FinalSpeed = 355f;
    private static readonly Color DampTint = new Color32(156, 174, 174, 255);
    private static readonly Color CollectedTile = new Color32(90, 130, 101, 255);

    private enum State { Ready, Running, Failed, Completed }

    private sealed class Row
    {
        internal int Sequence, TargetLane;
        internal RectTransform View;
        internal Image Cocoon;
        internal readonly Button[] Tiles = new Button[LaneCount];
        internal bool Collected;

        internal void SetInput(bool enabled)
        {
            foreach (var tile in Tiles) tile.interactable = enabled;
        }
    }

    private static WorkshopCocoonSorting current;
    internal static bool IsOpen => current != null;
    private readonly Queue<Row> rows = new Queue<Row>();
    private RectTransform frame, track, progressFill;
    private Text status;
    private GameObject startButton, retryButton, finishButton, previousSelection;
    private Sprite intactCocoon, tornCocoon, dewBead;
    private float previousTime, distance;
    private int collected, nextSequence;
    private State state;

    internal static void Show()
    {
        if (IsOpen || MiniGameOverlay.IsActive || !WorkshopProgress.Has(WorkshopItem.Suoxu)) return;
        var canvas = WorkshopUi.NewCanvas("Cocoon Sorting Tiles", 24000);
        current = canvas.gameObject.AddComponent<WorkshopCocoonSorting>();
        current.frame = canvas;
        current.previousTime = Time.timeScale;
        var events = EventSystem.current;
        if (events != null)
        {
            current.previousSelection = events.currentSelectedGameObject;
            events.SetSelectedGameObject(null);
        }
        Time.timeScale = 0f;
        current.Build();
    }

    private void Build()
    {
        intactCocoon = Resources.Load<Sprite>("WorkshopObservation/Summer_CocoonIntact");
        tornCocoon = Resources.Load<Sprite>("WorkshopObservation/Summer_CocoonReview");
        dewBead = Resources.Load<Sprite>("WorkshopGameArt/DewBead");

        WorkshopImmersive.Background(frame, WorkshopSeason.Summer);
        WorkshopUi.Fill(frame, "Sorting Veil", new Rect(0, 0, 1920, 1080), new Color32(16, 33, 27, 84));
        var title = WorkshopUi.PixelCard(frame, "Sorting Instructions", new Rect(394, 42, 1132, 152), WorkshopUi.Paper, WorkshopUi.Bronze);
        WorkshopUi.Label(title, "Sorting Title", new Rect(24, 12, 1084, 50), "立夏择茧 · 别踩白块", 39,
            WorkshopUi.Green, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
        WorkshopUi.Label(title, "Sorting Explanation", new Rect(24, 66, 1084, 68),
            "在蚕茧滑出底线前，点中深色块里的完好茧。\n避开浅色块里的破口茧和受潮茧。", 27,
            WorkshopUi.Ink, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);

        var tray = WorkshopUi.PixelCard(frame, "Sorting Tray", new Rect(492, 214, BoardWidth + 24, BoardHeight + 24),
            new Color32(19, 44, 36, 255), WorkshopUi.Bronze);
        var board = WorkshopUi.Rect(tray, "Sorting Board", new Rect(12, 12, BoardWidth, BoardHeight));
        board.gameObject.AddComponent<RectMask2D>();
        track = WorkshopUi.Rect(board, "Sorting Track", new Rect(0, 0, BoardWidth, BoardHeight));
        WorkshopUi.Fill(tray, "Sorting Deadline", new Rect(12, BoardHeight + 10, BoardWidth, 4), WorkshopUi.Bronze);

        var progress = WorkshopUi.PixelCard(frame, "Sorting Status", new Rect(504, 865, BoardWidth, 75), WorkshopUi.Paper, WorkshopUi.Bronze);
        status = WorkshopUi.Label(progress, "Sorting Progress", new Rect(20, 4, BoardWidth - 40, 55), "", 29,
            WorkshopUi.Ink, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
        WorkshopUi.Fill(progress, "Progress Track", new Rect(28, 61, BoardWidth - 56, 4), WorkshopUi.Line);
        progressFill = WorkshopUi.Fill(progress, "Progress Fill", new Rect(28, 61, 0, 4), WorkshopUi.Green).rectTransform;

        WorkshopUi.PixelButton(frame, "Leave Cocoon Sorting", new Rect(1635, 45, 240, 65), "返回工位", Close, false, 27);
        startButton = WorkshopUi.PixelButton(frame, "Start Cocoon Sorting", new Rect(780, 962, 360, 70),
            "开始择茧", StartRound, true, 29).transform.parent.gameObject;
        retryButton = WorkshopUi.PixelButton(frame, "Retry Cocoon Sorting", new Rect(780, 962, 360, 70),
            "重新择茧", StartRound, true, 29).transform.parent.gameObject;
        finishButton = WorkshopUi.PixelButton(frame, "Finish Cocoon Sorting", new Rect(715, 962, 490, 70),
            "完成择茧 · 收取立夏卡", Finish, true, 29).transform.parent.gameObject;
        foreach (var label in frame.GetComponentsInChildren<Text>()) label.font = WorkshopArt.ReadingFont;

        FillTrack();
        SetState(State.Ready, "点「开始择茧」后，方块会自动下落。");
    }

    private void StartRound()
    {
        if (state != State.Ready && state != State.Failed) return;
        if (state == State.Failed)
        {
            while (rows.Count > 0) RemoveFirstRow();
            distance = 0f;
            nextSequence = collected = 0;
            track.anchoredPosition = Vector2.zero;
            FillTrack();
        }
        SetState(State.Running);
    }

    private void FillTrack()
    {
        while (nextSequence < Target && RowTop(nextSequence) + TileHeight > 0f)
        {
            int sequence = nextSequence++;
            var row = new Row
            {
                Sequence = sequence,
                TargetLane = Random.Range(0, LaneCount),
                View = WorkshopUi.Rect(track, "Sort Row " + sequence,
                    new Rect(0, FirstRowTop - sequence * RowSpacing, BoardWidth, TileHeight))
            };
            bool nextDefectIsDamp = Random.value < .5f;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                int column = lane;
                bool intact = lane == row.TargetLane;
                var tile = WorkshopUi.Button(row.View, "Sort Cell " + sequence + " " + lane,
                    new Rect(lane * LaneWidth + 5f, 0, LaneWidth - 10f, TileHeight), "", () => Tap(row, column),
                    intact ? WorkshopUi.Green : WorkshopUi.Cream);
                tile.transition = Selectable.Transition.None;
                tile.navigation = new Navigation { mode = Navigation.Mode.None };
                tile.interactable = state == State.Running;
                row.Tiles[lane] = tile;

                if (intact)
                {
                    row.Cocoon = WorkshopUi.Picture(tile.transform, "Silkworm Cocoon", new Rect(38, 18, 142, 106), intactCocoon);
                    continue;
                }

                bool damp = nextDefectIsDamp;
                nextDefectIsDamp = !nextDefectIsDamp;
                WorkshopUi.Picture(tile.transform, damp ? "Damp Cocoon" : "Torn Cocoon", new Rect(54, 9, 110, 83),
                    damp ? intactCocoon : tornCocoon, damp ? DampTint : Color.white);
                if (damp)
                {
                    WorkshopUi.Picture(tile.transform, "Large Dew Bead", new Rect(126, 22, 25, 33), dewBead);
                    WorkshopUi.Picture(tile.transform, "Small Dew Bead", new Rect(79, 48, 19, 25), dewBead);
                }
                WorkshopUi.Label(tile.transform, "Cocoon Defect", new Rect(0, 96, LaneWidth - 10f, 36),
                    damp ? "受潮茧" : "破口茧", 24, WorkshopUi.Faded, TextAnchor.MiddleCenter, WorkshopArt.ReadingFont);
            }
            rows.Enqueue(row);
        }
    }

    private float RowTop(int sequence) => FirstRowTop - sequence * RowSpacing + distance;

    private void RemoveFirstRow()
    {
        var view = rows.Dequeue().View;
        view.gameObject.SetActive(false);
        view.SetParent(frame, false);
        Destroy(view.gameObject);
    }

    private void Advance(float deltaTime)
    {
        float speed = Mathf.Lerp(StartingSpeed, FinalSpeed, Mathf.InverseLerp(0f, (Target - 1) * RowSpacing, distance));
        distance += speed * deltaTime;
        while (rows.Count > 0 && RowTop(rows.Peek().Sequence) >= BoardHeight)
        {
            var row = rows.Peek();
            if (!row.Collected)
            {
                distance = BoardHeight - FirstRowTop + row.Sequence * RowSpacing;
                track.anchoredPosition = new Vector2(0, -distance);
                Fail("有蚕茧滑出底线了。");
                return;
            }
            RemoveFirstRow();
        }
        track.anchoredPosition = new Vector2(0, -distance);
        FillTrack();
    }

    private void Tap(Row row, int lane)
    {
        if (state != State.Running || row.Collected || !row.View.gameObject.activeInHierarchy) return;
        if (lane != row.TargetLane)
        {
            row.Tiles[lane].image.color = new Color32(235, 183, 159, 255);
            Fail("点到白块了。");
            return;
        }

        row.Collected = true;
        row.SetInput(false);
        row.Tiles[lane].image.color = CollectedTile;
        row.Cocoon.color = new Color(1f, 1f, 1f, .42f);
        collected++;
        WorkshopAudioFeedback.Progress();
        if (collected == Target)
            SetState(State.Completed, "择茧完成！");
        else
            UpdateProgress();
    }

    private void Fail(string message)
    {
        WorkshopAudioFeedback.Notice();
        SetState(State.Failed, message);
    }

    private void SetState(State next, string message = null)
    {
        state = next;
        foreach (var row in rows) row.SetInput(state == State.Running && !row.Collected);
        startButton.SetActive(state == State.Ready);
        retryButton.SetActive(state == State.Failed);
        finishButton.SetActive(state == State.Completed);
        status.color = state == State.Failed ? WorkshopUi.Clay : WorkshopUi.Ink;
        UpdateProgress(message);
    }

    private void UpdateProgress(string message = null)
    {
        status.text = "已择 " + collected + " / " + Target + " 枚";
        if (message != null) status.text += "  ·  " + message;
        progressFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (BoardWidth - 56f) * collected / Target);
    }

    private void Finish()
    {
        if (state != State.Completed) return;
        WorkshopProgress.MarkStep(WorkshopWordRules.Sorted);
        Close();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
        else if (state == State.Running) Advance(Mathf.Min(Time.unscaledDeltaTime, .1f));
    }

    private void Close()
    {
        if (current != this) return;
        current = null;
        Time.timeScale = previousTime;
        WorkshopUi.CloseCanvas(frame);
        var events = EventSystem.current;
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
