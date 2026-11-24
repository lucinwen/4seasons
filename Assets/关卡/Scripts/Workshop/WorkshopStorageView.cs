using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>The shared material inventory and seasonal synthesis workbench.</summary>
public sealed class WorkshopStorageView : MonoBehaviour
{
    private static readonly Rect FrameBounds = new Rect(50, 26, 1820, 1025);
    private static readonly Rect InventoryBounds = new Rect(162, 303, 737, 599);
    private static readonly Rect InventoryGrid = new Rect(174, 360, 713, 531);
    private const int InventoryColumns = 4;
    private const float InventoryGutter = 7f;
    private static readonly Color SlotLip = new Color32(107, 119, 72, 255);
    private static readonly Color SelectedLip = new Color32(62, 100, 62, 255);
    private static readonly Color DraggableLip = new Color32(186, 134, 53, 255);
    private static readonly Color EmptyPocket = new Color32(212, 214, 173, 255);
    private static readonly Color FilledPocket = new Color32(252, 242, 213, 255);
    private static readonly Color DraggablePocket = new Color32(255, 228, 168, 255);
    internal static readonly Color WorkbenchInk = new Color32(55, 35, 24, 255);
    private static readonly Color[] SeasonAccents =
    {
        new Color32(45, 95, 63, 255), new Color32(42, 95, 92, 255),
        new Color32(139, 75, 50, 255), new Color32(57, 77, 125, 255)
    };

    private static WorkshopStorageView current;
    private RectTransform frame;
    private RectTransform page;
    private CanvasGroup visibility;
    private CanvasGroup storageControls;
    private WorkshopSynthesis workbench;
    private RectTransform collectionsPage;
    private WorkshopSeason season;
    private Action onClose;
    private Action<WorkshopSeason, WorkshopSeason> onContinue;
    private Action onTailoringComplete;
    private int resumedFrame = -1;

    public static bool IsOpen => current != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => current = null;

    public static void Show(WorkshopSeason selected) => Show(selected, null, null);

    internal static void Show(WorkshopSeason selected, Action closed,
        Action<WorkshopSeason, WorkshopSeason> continued, Action tailoringComplete = null)
    {
        if (current != null) return;

        var frame = WorkshopUi.NewCanvas("Workshop Storage", 9000);
        WorkshopUi.Block(frame, new Color32(20, 23, 19, 142));
        current = frame.gameObject.AddComponent<WorkshopStorageView>();
        current.frame = frame;
        current.visibility = frame.parent.gameObject.AddComponent<CanvasGroup>();
        current.season = selected;
        current.onClose = closed;
        current.onContinue = continued;
        current.onTailoringComplete = tailoringComplete;
        current.Render();
        WorkshopAudioFeedback.Notice();
    }

    private void Render()
    {
        if (page != null)
        {
            page.gameObject.SetActive(false);
            Destroy(page.gameObject);
        }

        page = WorkshopUi.Rect(frame, "Storage Page", new Rect(0, 0, 1920, 1080));
        storageControls = page.gameObject.AddComponent<CanvasGroup>();
        var artwork = Resources.Load<Sprite>("WorkshopFinish/StorageCraftTable");
        WorkshopUi.Panel(page, "Unified Storage Frame", FrameBounds, artwork ?? WorkshopArt.WorkshopTaskBoard);
        var title = Label(page, "Storage Title", new Rect(704, 139, 440, 69), "储物屋", 48,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;
        WorkshopUi.PixelButton(page, "Storage Close", new Rect(1020, 139, 202, 69), "返回工坊", Close, true, 34);
        WorkshopUi.PixelButton(page, "Open Challenge Collections", new Rect(1240, 139, 224, 69),
            "节气收藏", ShowCollections, false, 30);

        var inventory = DrawInventory(out Rect[] sources);
        Label(page, "Workbench Title", new Rect(961, 302, 217, 48), "合成台", 40, WorkbenchInk);
        for (int i = 0; i < SeasonAccents.Length; i++)
        {
            var tab = (WorkshopSeason)i;
            var button = ArtButton(page, "Workbench Season " + tab, new Rect(1240 + i * 116, 303, 108, 45),
                WorkshopProgress.SeasonName(tab) + "季", () => SelectSeason(tab), tab, 22);
            if (tab == season)
            {
                WorkshopUi.Fill(button.transform, "Selected Season", new Rect(20, 36, 68, 3),
                    SeasonAccents[i]);
            }
        }

        workbench = page.gameObject.AddComponent<WorkshopSynthesis>();
        workbench.Build(page, season, inventory, sources, artwork, NextSeason, Tailor);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void ShowCollections()
    {
        if (collectionsPage != null || workbench.Busy) return;

        storageControls.interactable = false;
        storageControls.blocksRaycasts = false;
        collectionsPage = WorkshopUi.Rect(frame, "Challenge Collections", new Rect(0, 0, 1920, 1080));
        WorkshopUi.Button(collectionsPage, "Collections Input Shield", new Rect(0, 0, 1920, 1080),
            "", null, WorkshopUi.Veil);
        var board = WorkshopUi.PixelCard(collectionsPage, "Collection Board", new Rect(162, 101, 1596, 878),
            WorkshopUi.Paper, WorkshopUi.Bronze);

        int count = 0;
        int total = 0;
        foreach (WorkshopSeason chapter in Enum.GetValues(typeof(WorkshopSeason)))
        {
            foreach (var id in WorkshopObservationView.ObservationsFor(chapter))
            {
                if (!WorkshopObservationView.IsSeasonGame(id)) continue;

                var challenge = id;
                bool earned = WorkshopObservationView.HasRecord(id);
                if (earned) count++;
                float x = 39 + total % 5 * 305;
                float y = 169 + total / 5 * 323;
                total++;
                var card = WorkshopUi.PixelCard(board, "Collected Item " + id, new Rect(x, y, 298, 307),
                    earned ? FilledPocket : EmptyPocket, SeasonAccents[(int)chapter]);
                string term = WorkshopObservationView.RecordTerm(id);
                Label(card, "Term", new Rect(8, 10, 282, 47), term + " · " + WorkshopObservationView.SeasonGameTitle(id),
                    24, WorkshopUi.Green, TextAnchor.MiddleCenter);
                var best = WorkshopObservationView.BestWork(id, 1) ?? WorkshopObservationView.BestWork(id, 0);
                var preview = new Rect(79, 61, 140, best != null ? 90 : 112);
                if (!WorkshopObservationView.DrawWorkSample(card, preview, best?.sample))
                    WorkshopUi.Picture(card, "Collectible", preview, WorkshopObservationView.RewardIcon(id),
                        earned ? Color.white : new Color32(74, 89, 58, 97));
                Label(card, "Item", new Rect(6, best != null ? 149 : 175, 286, 35), WorkshopObservationView.RewardName(id) + (earned ? " ×1" : ""),
                    24, WorkshopUi.Ink, TextAnchor.MiddleCenter);
                if (best != null)
                {
                    var note = Label(card, "Best Work", new Rect(9, 182, 280, 47), best.description, 20,
                        WorkshopUi.Green, TextAnchor.MiddleCenter);
                    note.verticalOverflow = UnityEngine.VerticalWrapMode.Truncate;
                }
                var record = WorkshopUi.PixelButton(card, "Read collected page " + id, new Rect(17, 233, 264, 57),
                    earned ? term + "残页 ×1 · 查看" : "待通关 · 残页未收集", () => WorkshopObservationView.ShowRecord(challenge), earned, 23);
                record.interactable = earned;
            }
        }

        Label(board, "Collection Heading", new Rect(55, 45, 688, 67), $"节气收藏　{count} / {total}", 47, WorkshopUi.Green);
        if (WorkshopProgress.HasStep("autumn.loom"))
            Label(board, "Loom Repair Ticket", new Rect(811, 50, 458, 61), "工坊凭证 · 修机工票 ×1", 27,
                WorkshopUi.Green, TextAnchor.MiddleRight);
        var back = WorkshopUi.PixelButton(board, "Close Collections", new Rect(1303, 48, 238, 64),
            "返回背包", CloseCollections, true, 31);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(back.gameObject);
    }

    private void CloseCollections()
    {
        if (collectionsPage == null) return;
        collectionsPage.gameObject.SetActive(false);
        Destroy(collectionsPage.gameObject);
        collectionsPage = null;
        storageControls.interactable = true;
        storageControls.blocksRaycasts = true;
    }

    private RectTransform DrawInventory(out Rect[] sources)
    {
        var inventory = WorkshopUi.Rect(page, "Illustrated Backpack", InventoryBounds);
        Label(page, "Inventory Title", new Rect(176, 313, 156, 42), "背包", 34, WorkshopUi.Ink);
        bool canArrange = WorkshopProgress.IsUnlocked(season) && !WorkshopProgress.IsComplete(season);
        string help = canArrange ? "拖动金边材料 · 点击看图鉴" : "点击材料查看图鉴";
        Label(page, "Inventory Help", new Rect(315, 317, 368, 38), help, 24, WorkshopUi.Ink);

        int total = 0;
        int ownedCount = 0;
        var materials = new WorkshopItem[SeasonAccents.Length][];
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = WorkshopProgress.Items((WorkshopSeason)i);
            total += materials[i].Length;
            foreach (var item in materials[i])
                if (WorkshopProgress.Has(item)) ownedCount++;
        }
        Label(page, "Storage Count", new Rect(704, 313, 181, 40), $"已入库 {ownedCount} / {total}", 24,
            WorkshopUi.Ink, TextAnchor.MiddleRight);

        int rows = (total + InventoryColumns - 1) / InventoryColumns;
        float cell = Mathf.Floor(Mathf.Min(
            (InventoryGrid.width - InventoryGutter * (InventoryColumns - 1)) / InventoryColumns,
            (InventoryGrid.height - InventoryGutter * (rows - 1)) / rows));
        float step = cell + InventoryGutter;
        float left = InventoryGrid.center.x - (step * InventoryColumns - InventoryGutter) * 0.5f;
        float top = InventoryGrid.center.y - (step * rows - InventoryGutter) * 0.5f;
        sources = new Rect[materials[(int)season].Length];
        int index = 0;
        for (int i = 0; i < SeasonAccents.Length; i++)
        {
            var rowSeason = (WorkshopSeason)i;
            bool selected = rowSeason == season;
            var items = materials[i];
            for (int j = 0; j < items.Length; j++)
            {
                var item = items[j];
                bool owned = WorkshopProgress.Has(item);
                bool draggable = owned && selected && canArrange;
                var bounds = new Rect(left + index % InventoryColumns * step,
                    top + index / InventoryColumns * step, cell, cell);
                index++;
                var slot = WorkshopUi.Button(page, "Storage Item " + item, bounds, "",
                    owned ? () => WorkshopKnowledgeView.ShowItem(item) : null,
                    draggable ? DraggableLip : owned && selected ? SelectedLip : SlotLip, owned);
                DrawPocket(slot.transform, bounds.width, draggable ? DraggablePocket : owned ? FilledPocket : EmptyPocket);
                var colors = slot.colors;
                colors.disabledColor = Color.white;
                slot.colors = colors;
                Label(slot.transform, "Material Name", new Rect(5, cell - 44, cell - 10, 32),
                    WorkshopProgress.ItemName(item), 24, owned ? WorkshopUi.Ink : new Color32(67, 76, 46, 255),
                    TextAnchor.MiddleCenter);

                float iconSize = Mathf.Floor(cell * 0.66f);
                var iconBounds = new Rect(bounds.center.x - iconSize * 0.5f, bounds.y + 10, iconSize, iconSize);
                if (selected && owned) sources[j] = iconBounds;
                else WorkshopSynthesis.MaterialIcon(page, "Inventory " + item, iconBounds, item,
                    owned ? Color.white : new Color32(72, 91, 63, 119));
            }
        }
        return inventory;
    }

    internal static UnityEngine.UI.Image DrawPocket(Transform parent, float size, Color color)
    {
        var surface = WorkshopUi.Fill(parent, "Recess", new Rect(4, 4, size - 8, size - 8), color);
        WorkshopUi.ApplyPixelShape(surface);
        WorkshopUi.Fill(parent, "Upper Bevel", new Rect(5, 5, size - 10, 4), new Color32(48, 52, 32, 111));
        WorkshopUi.Fill(parent, "Lower Bevel", new Rect(5, size - 8, size - 10, 3), new Color32(251, 237, 183, 133));
        return surface;
    }

    internal static UnityEngine.UI.Button ArtButton(Transform parent, string name, Rect bounds, string title,
        Action action, WorkshopSeason season, int size, bool enabled = true)
    {
        var button = WorkshopUi.Button(parent, name, bounds, "", action, WorkshopUi.Paper, enabled);
        ApplyArtwork((UnityEngine.UI.Image)button.targetGraphic, WorkshopArt.SeasonPanel(season));
        Label(button.transform, "Label", new Rect(0, 0, bounds.width, bounds.height), title, size,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
        return button;
    }

    private static void ApplyArtwork(UnityEngine.UI.Image image, Sprite sprite)
    {
        if (sprite == null) return;
        image.sprite = sprite;
        image.type = sprite.border.sqrMagnitude > 0f ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        image.color = Color.white;
    }

    private void SelectSeason(WorkshopSeason selected)
    {
        if (selected == season || workbench.Busy) return;
        season = selected;
        Render();
    }

    private void NextSeason(WorkshopSeason next)
    {
        Close();
        if (onContinue != null) onContinue(season, next);
        else WorkshopNarrativeView.ShowChapterInterlude(season, next,
            () => WorkshopNarrativeView.PlaySeasonBriefing(next,
                () => SceneManager.LoadScene(WorkshopProgress.SceneName(next))),
            () =>
            {
                if (SceneManager.GetActiveScene().name != WorkshopProgress.HubScene)
                    SceneManager.LoadScene(WorkshopProgress.HubScene);
            });
    }

    private void Tailor()
    {
        if (WorkshopTailor.IsOpen) return;

        var events = EventSystem.current;
        var selected = events != null ? events.currentSelectedGameObject : null;
        if (events != null) events.SetSelectedGameObject(null);
        SetWorkbenchVisible(false);

        WorkshopTailor.Show(() =>
        {
            if (current != this) return;
            workbench.Refresh();
            SetWorkbenchVisible(true);
            resumedFrame = Time.frameCount;
            if (events != null && selected != null) events.SetSelectedGameObject(selected);
        }, () =>
        {
            if (current != this) return;
            Close();
            if (onTailoringComplete != null) onTailoringComplete();
            else if (onClose == null) SceneManager.LoadScene(WorkshopProgress.HubScene);
        });
    }

    private void SetWorkbenchVisible(bool visible)
    {
        visibility.alpha = visible ? 1f : 0f;
        visibility.interactable = visible;
        visibility.blocksRaycasts = visible;
    }

    private void Update()
    {
        if (!visibility.interactable || resumedFrame == Time.frameCount) return;
        if (WorkshopKnowledgeView.BlocksWorldInput || WorkshopNarrativeView.IsOpen || WorkshopDialoguePresenter.BlocksWorldInput ||
            WorkshopCraftRecordView.BlocksWorldInput || WorkshopHeritageBrief.IsOpen || WorkshopChapterGuide.IsOpen ||
            WorkshopObservationView.BlocksWorldInput) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (collectionsPage != null) CloseCollections();
        else Close();
    }

    private void Close()
    {
        if (current != this || (workbench != null && workbench.Busy)) return;
        current = null;
        WorkshopUi.CloseCanvas(frame);
        onClose?.Invoke();
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    internal static UnityEngine.UI.Text Label(Transform parent, string name, Rect bounds, string value,
        int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var label = WorkshopUi.Label(parent, name, bounds, value, size, color, anchor, WorkshopArt.WorkshopFont);
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }
}
