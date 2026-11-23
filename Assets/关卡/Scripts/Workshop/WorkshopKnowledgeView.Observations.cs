using UnityEngine;

public sealed partial class WorkshopKnowledgeView
{
    private static readonly string[] ObservationCoverPaths =
    {
        "WorkshopObservationCards/Spring_ObservationCard", "WorkshopObservationCards/Summer_ObservationCard",
        "WorkshopObservationCards/Autumn_ObservationCard", "WorkshopObservationCards/Winter_ObservationCard"
    };

    private void RenderObservationGallery()
    {
        StartPage(RenderItems);
        var panel = WorkshopUi.PixelCard(page, "Observation Gallery", new Rect(107, 123, 1706, 833),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        Label(panel, "Title", new Rect(52, 42, 1200, 80), "节气小游戏与观察", 51, Jade);
        Label(panel, "Hint", new Rect(55, 123, 1500, 42),
            "每季有两款节气小游戏。亲临工位开局，完成后可在这里回看。", 28, WorkshopUi.Ink);
        var back = WorkshopUi.PixelButton(panel, "Back", new Rect(1456, 63, 196, 58), "返回织录", RenderItems, false, 28);

        for (int i = 0; i < ObservationCoverPaths.Length; i++)
        {
            var chapter = (WorkshopSeason)i;
            float x = 52 + i * 405f;
            var card = WorkshopUi.PixelCard(panel, "Season Observation Card " + chapter, new Rect(x, 213, 384, 504),
                WorkshopUi.Cream, WorkshopUi.Bronze);
            var cover = Resources.Load<Sprite>(ObservationCoverPaths[i]);
            bool unlocked = WorkshopProgress.IsUnlocked(chapter);
            WorkshopUi.Picture(card, "Cover", new Rect(16, 16, 352, 264), cover,
                unlocked ? Color.white : new Color32(137, 143, 127, 186));
            Label(card, "Season", new Rect(16, 301, 352, 49), WorkshopProgress.SeasonName(chapter) + "章", 38,
                Jade, TextAnchor.MiddleCenter);
            int recorded = 0;
            int total = 0;
            foreach (var id in WorkshopObservationView.ObservationsFor(chapter))
            {
                if (!WorkshopObservationView.IsSeasonGame(id)) continue;
                total++;
                if (WorkshopObservationView.HasRecord(id)) recorded++;
            }
            Label(card, "Count", new Rect(16, 359, 352, 51),
                unlocked ? $"节气残页 {recorded} / {total}" : "本章尚未开放", 27, WorkshopUi.Ink, TextAnchor.MiddleCenter);
            var read = WorkshopUi.PixelButton(card, "Open season " + chapter, new Rect(63, 426, 258, 58), "查看记录",
                () => RenderSeasonObservations(chapter), true, 28, Jade);
            read.interactable = unlocked;
        }

        WorkshopCraftRecordView.KeepFocusIn(page, back);
    }

    private void RenderSeasonObservations(WorkshopSeason chapter)
    {
        StartPage(RenderObservationGallery);
        var panel = WorkshopUi.PixelCard(page, "Season Observations", new Rect(147, 109, 1626, 862),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        string title = WorkshopProgress.SeasonName(chapter) + "章 · 节气记录";
        Label(panel, "Title", new Rect(61, 43, 1200, 75), title, 49, Jade);
        var back = WorkshopUi.PixelButton(panel, "Back", new Rect(1363, 47, 204, 61), "返回", RenderObservationGallery,
            false, 29);

        var cover = Resources.Load<Sprite>(ObservationCoverPaths[(int)chapter]);
        WorkshopUi.Picture(panel, "Cover", new Rect(57, 154, 712, 534), cover);
        Label(panel, "Craft", new Rect(57, 709, 712, 68), WorkshopNarrativeData.FieldNote(chapter).Region + " · " +
            WorkshopNarrativeData.FieldNote(chapter).Scene, 29, Jade, TextAnchor.MiddleCenter);
        WorkshopUi.Fill(panel, "Divider", new Rect(797, 153, 2, 639), WorkshopUi.Bronze);

        int index = 0;
        foreach (var item in WorkshopObservationView.ObservationsFor(chapter))
        {
            var id = item;
            bool recorded = WorkshopObservationView.HasRecord(id);
            bool game = WorkshopObservationView.IsSeasonGame(id);
            if (!game && !recorded) continue;
            float y = 158 + index * 127f;
            string status = game ? (recorded ? "已通关 · 残页已收集" : "未通关 · 前往关卡工位") : "已记录 · 工艺观察";
            Label(panel, "Record Status " + id, new Rect(835, y, 690, 37), status,
                24, WorkshopUi.Ink);
            var read = WorkshopUi.PixelButton(panel, "Read " + id, new Rect(828, y + 44, 702, 62),
                game ? WorkshopObservationView.SeasonGameTitle(id) : WorkshopObservationView.RecordTitle(id),
                () => WorkshopObservationView.ShowRecord(id), true, 29, Jade);
            read.interactable = recorded;
            index++;
        }

        WorkshopUi.PixelButton(panel, "Regional Context", new Rect(828, 620, 702, 62), "地方工艺与来源",
            () => WorkshopHeritageBrief.ShowSeason(chapter), true, 29, Jade);
        var summary = WorkshopUi.PixelButton(panel, "Chapter Result", new Rect(828, 702, 702, 62), "查看本章成果",
            () => WorkshopCraftRecordView.ShowChapterRecord(chapter), false, 29);
        summary.interactable = WorkshopProgress.IsComplete(chapter);
        WorkshopCraftRecordView.KeepFocusIn(page, back);
    }
}
