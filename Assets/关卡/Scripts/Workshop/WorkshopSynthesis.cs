using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class WorkshopSynthesis : MonoBehaviour
{
    private const int SlotCount = 3;
    private static readonly string[] ProductAssets = { "Cocoon", "RawSilk", "PlainSilk" };
    private static readonly Rect OutputBounds = new Rect(1380, 378, 354, 346);
    private static readonly Rect ProductBounds = new Rect(1464, 421, 186, 186);
    private static readonly Color WellLip = new Color32(84, 98, 64, 255);
    private static readonly Color EmptyCell = new Color32(132, 145, 94, 255);
    private static readonly Color HoverCell = new Color32(212, 221, 160, 255);
    private static readonly Color FilledCell = new Color32(187, 202, 133, 255);
    private static readonly Color BlockedCell = new Color32(177, 102, 78, 255);
    private static readonly Color Preview = new Color32(234, 219, 183, 123);

    private RectTransform table;
    private RectTransform inventory;
    private WorkshopSeason season;
    private WorkshopItem[] items;
    private Rect[] materialSources;
    private Rect[] slotIcons;
    private UnityEngine.UI.Image[] slots;
    private UnityEngine.UI.RawImage[] pieces;
    private UnityEngine.UI.RawImage[] storedPieces;
    private int[] assignments;
    private int dragging = -1;
    private bool unlocked;
    private bool completed;
    private UnityEngine.UI.Button action;
    private UnityEngine.UI.Text actionText;
    private UnityEngine.UI.Text status;
    private UnityEngine.UI.Text tally;
    private UnityEngine.UI.Image product;
    private RectTransform chapterResult;
    private Sprite outputTray;
    private Action<WorkshopSeason> advance;
    private Action tailor;

    internal bool Busy { get; private set; }

    internal void Build(RectTransform page, WorkshopSeason selected, RectTransform bag, Rect[] sources,
        Sprite artwork, Action<WorkshopSeason> onAdvance, Action onTailor)
    {
        table = page;
        inventory = bag;
        season = selected;
        materialSources = sources;
        advance = onAdvance;
        tailor = onTailor;
        unlocked = WorkshopProgress.IsUnlocked(season);
        completed = WorkshopProgress.IsComplete(season);

        items = WorkshopProgress.Items(season);
        slots = new UnityEngine.UI.Image[SlotCount];
        slotIcons = new Rect[slots.Length];
        assignments = new int[items.Length];
        Array.Fill(assignments, -1);
        pieces = new UnityEngine.UI.RawImage[items.Length];
        storedPieces = new UnityEngine.UI.RawImage[items.Length];

        Color ink = WorkshopStorageView.WorkbenchInk;
        string recipe = string.Join(" · ", Array.ConvertAll(items, WorkshopProgress.ItemName));
        WorkshopStorageView.Label(table, "Workbench Recipe", new Rect(964, 352, 327, 33),
            recipe, 19, ink);
        tally = WorkshopStorageView.Label(table, "Materials Placed", new Rect(1286, 352, 64, 33),
            "", 20, ink, TextAnchor.MiddleRight);
        for (int i = 0; i < slots.Length; i++) DrawSlot(i);
        for (int i = 0; i < items.Length; i++) DrawMaterial(i);

        DrawArrow();
        DrawOutputTray(artwork);
        WorkshopStorageView.Label(table, "Output Heading", new Rect(1380, 348, 354, 29),
            "成品", 24, ink, TextAnchor.MiddleCenter);
        WorkshopStorageView.Label(table, "Product Name", new Rect(1407, 627, 300, 55),
            WorkshopProgress.ProductName(season), 34, artwork != null ? WorkshopUi.Cream : ink, TextAnchor.MiddleCenter);
        var productArt = season == WorkshopSeason.Winter
            ? completed ? WorkshopArt.Garment(WorkshopProgress.GarmentId) :
                WorkshopProgress.SeasonalOrderEnabled ? WorkshopArt.Garment(WorkshopProgress.SeasonalGarmentId) :
                    WorkshopTailor.PreviewForDye(WorkshopProgress.DyedColor)
            : Resources.Load<Sprite>("WorkshopFinish/" + ProductAssets[(int)season]);
        if (productArt == null) productArt = WorkshopArt.ChapterProduct(season);
        product = WorkshopUi.Picture(table, "Synthesis Product", ProductBounds, productArt);
        MoveIcon(product.rectTransform, ProductBounds);
        action = WorkshopUi.PixelButton(table, "Synthesis Combine", new Rect(1427, 731, 260, 49), "",
            CombineOrContinue, true, 29);
        actionText = action.GetComponentInChildren<UnityEngine.UI.Text>();
        var statusCard = WorkshopUi.PixelCard(table, "Synthesis Status Card", new Rect(1379, 786, 356, 39),
            new Color32(251, 234, 195, 255), new Color32(137, 94, 60, 255));
        status = WorkshopStorageView.Label(statusCard, "Synthesis Status", new Rect(9, 1, 338, 37),
            "", 24, ink, TextAnchor.MiddleCenter);
        status.verticalOverflow = VerticalWrapMode.Truncate;
        chapterResult = WorkshopUi.Rect(table, "Chapter Result", new Rect(1427, 834, 260, 49));
        WorkshopUi.PixelButton(chapterResult, "Open Chapter Result", new Rect(0, 0, 260, 49),
            "回看本章成果", () => WorkshopCraftRecordView.ShowChapterRecord(season), false, 25);
        Refresh();
    }

    private void DrawOutputTray(Sprite artwork)
    {
        if (artwork == null)
        {
            WorkshopUi.Panel(table, "Output Slot", OutputBounds, WorkshopArt.SeasonPanel(season));
            return;
        }

        // Sample the tray from StorageCraftTable, clipping its corners to the painted outline.
        var texture = artwork.texture;
        var scale = new Vector2(texture.width / 1671f, texture.height / 941f);
        var crop = new Rect(1275 * scale.x, 330 * scale.y, 266 * scale.x, 261 * scale.y);
        outputTray = Sprite.Create(texture, crop, Vector2.one * 0.5f, artwork.pixelsPerUnit);
        var outline = new[]
        {
            new Vector2(35, 261), new Vector2(227, 261), new Vector2(266, 221), new Vector2(266, 41),
            new Vector2(225, 0), new Vector2(37, 0), new Vector2(0, 38), new Vector2(0, 224)
        };
        for (int i = 0; i < outline.Length; i++) outline[i] = Vector2.Scale(outline[i], scale);
        outputTray.OverrideGeometry(outline, new ushort[] { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 6, 0, 6, 7 });
        WorkshopUi.Picture(table, "Output Slot", OutputBounds, outputTray, stretch: true).useSpriteMesh = true;
    }

    private void OnDestroy()
    {
        if (outputTray != null) Destroy(outputTray);
    }

    private void DrawSlot(int index)
    {
        const float size = 144;
        const float gap = 8;
        const float left = 1084;
        float top = 386 + index * (size + gap);
        var rim = WorkshopUi.Fill(table, "Synthesis Slot " + index, new Rect(left, top, size, size), WellLip);
        WorkshopUi.ApplyPixelShape(rim);
        slots[index] = WorkshopStorageView.DrawPocket(rim.transform, size, EmptyCell);
        slotIcons[index] = new Rect(left + 11, top + 11, size - 22, size - 22);
    }

    private void DrawMaterial(int index)
    {
        if (materialSources[index].width <= 0f) return;
        if (completed)
            assignments[index] = index;
        pieces[index] = MaterialIcon(table, "Synthesis Material " + index, materialSources[index],
            items[index], Color.white);
        if (!completed && unlocked)
            pieces[index].gameObject.AddComponent<WorkshopSynthesisMaterial>().Bind(this, table, pieces[index], index);
    }

    private void DrawArrow()
    {
        var arrow = WorkshopUi.Rect(table, "Craft Direction", new Rect(1299, 550, 68, 33));
        WorkshopUi.Fill(arrow, "Shaft", new Rect(1, 13, 43, 7), WorkshopUi.Green);
        WorkshopUi.Fill(arrow, "Head Base", new Rect(37, 2, 8, 29), WorkshopUi.Green);
        WorkshopUi.Fill(arrow, "Head Middle", new Rect(45, 7, 8, 19), WorkshopUi.Green);
        WorkshopUi.Fill(arrow, "Head Tip", new Rect(53, 12, 8, 9), WorkshopUi.Green);
    }

    internal static UnityEngine.UI.RawImage MaterialIcon(Transform parent, string name, Rect bounds,
        WorkshopItem item, Color tint)
    {
        var rect = WorkshopUi.Rect(parent, name, bounds);
        MoveIcon(rect, bounds);
        var icon = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        icon.texture = WorkshopArt.TaskIconAtlas;
        icon.uvRect = WorkshopArt.TaskIconUV(item);
        icon.color = tint;
        icon.raycastTarget = false;
        icon.enabled = icon.texture != null;
        return icon;
    }

    private static void MoveIcon(RectTransform rect, Rect area)
    {
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = area.size;
        rect.anchoredPosition = new Vector2(area.center.x, -area.center.y);
    }

    internal bool BeginMaterialDrag(int index)
    {
        if (Busy || completed || !unlocked || dragging >= 0 || pieces[index] == null) return false;
        dragging = index;
        Refresh();
        return true;
    }

    internal void HoverMaterial(int index, Vector2 position, Camera camera)
    {
        if (dragging != index) return;
        int hovered = FindSlot(position, camera);
        for (int i = 0; i < slots.Length; i++)
        {
            int occupant = Array.IndexOf(assignments, i);
            slots[i].color = hovered == i
                ? occupant >= 0 && occupant != index ? BlockedCell : HoverCell
                : occupant >= 0 ? FilledCell : EmptyCell;
        }
    }

    internal void DropMaterial(int index, Vector2 position, Camera camera)
    {
        if (dragging != index) return;
        dragging = -1;
        int slot = FindSlot(position, camera);
        int occupant = slot >= 0 ? Array.IndexOf(assignments, slot) : -1;
        bool accepted = slot >= 0 && (occupant < 0 || occupant == index);
        bool returned = RectTransformUtility.RectangleContainsScreenPoint(inventory, position, camera);
        int previousSlot = assignments[index];
        if (accepted) assignments[index] = slot;
        else if (returned) assignments[index] = -1;

        if (assignments[index] != previousSlot) WorkshopAudioFeedback.Place();
        else if (!accepted && !returned) WorkshopAudioFeedback.Notice();
        Refresh();
        if (!accepted && !returned)
            status.text = occupant >= 0 ? "这个格子已有材料。" : "请拖入合成格或放回背包。";
    }

    private int FindSlot(Vector2 position, Camera camera)
    {
        for (int i = 0; i < slots.Length; i++)
            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)slots[i].transform.parent, position, camera))
                return i;
        return -1;
    }

    internal void CancelMaterialDrag(int index)
    {
        if (dragging != index) return;
        dragging = -1;
        Refresh();
    }

    internal void ReadMaterial(int index)
    {
        if (!Busy && dragging < 0) WorkshopKnowledgeView.ShowItem(items[index]);
    }

    internal void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].color = Array.IndexOf(assignments, i) >= 0 ? FilledCell : EmptyCell;

        int count = 0;
        for (int i = 0; i < items.Length; i++)
        {
            int slot = assignments[i];
            if (slot >= 0) count++;
            if (pieces[i] == null) continue;

            var piece = pieces[i];
            if (dragging != i)
            {
                MoveIcon(piece.rectTransform, slot >= 0 ? slotIcons[slot] : materialSources[i]);
                piece.rectTransform.localScale = Vector3.one;
                piece.color = Color.white;
            }
            piece.raycastTarget = !Busy && !completed && unlocked && (dragging < 0 || dragging == i);
            if (completed && slot >= 0 && storedPieces[i] == null)
                storedPieces[i] = MaterialIcon(table, "Stored Material " + i, materialSources[i], items[i], Color.white);
        }

        tally.text = $"{count} / {items.Length}";
        if (completed) status.text = "本季成果已完成。";
        else if (!unlocked) status.text = "完成上一季后开放。";
        else if (count == items.Length) status.text = "材料已就位。";
        else
        {
            var missing = new List<string>(items.Length);
            foreach (WorkshopItem item in items)
                if (!WorkshopProgress.Has(item)) missing.Add(WorkshopProgress.ItemName(item));

            status.text = missing.Count > 0 ? "缺少：" + string.Join("、", missing) : "拖动金边材料入格。";
        }

        product.color = completed || count == items.Length ? Color.white : Preview;
        chapterResult.gameObject.SetActive(completed && !Busy);
        action.interactable = !Busy && dragging < 0 &&
            (completed ? season != WorkshopSeason.Winter : count == items.Length && WorkshopProgress.CanSubmit(season));
        actionText.text = ActionLabel();
    }

    private string ActionLabel()
    {
        if (Busy) return "合成中";
        if (season != WorkshopSeason.Winter)
            return completed ? "前往" + WorkshopProgress.SeasonName(season + 1) + "季" : "开始合成";
        if (completed) return "已交衣";
        return WorkshopTailor.NextOrderActionLabel();
    }

    private void CombineOrContinue()
    {
        if (!action.interactable || Busy) return;
        if (completed)
        {
            advance(season + 1);
            return;
        }
        if (!WorkshopProgress.CanSubmit(season) || Array.IndexOf(assignments, -1) >= 0) return;

        WorkshopAudioFeedback.Confirm();
        if (season == WorkshopSeason.Winter) tailor();
        else StartCoroutine(AnimateSynthesis());
    }

    private IEnumerator AnimateSynthesis()
    {
        Busy = true;
        Refresh();
        status.text = "正在合成" + WorkshopProgress.ProductName(season) + "……";
        Vector2 destination = new Vector2(ProductBounds.center.x, -ProductBounds.center.y);
        const float duration = 0.66f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < pieces.Length; i++)
            {
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration * 1.3f - i * 0.11f));
                Rect slot = slotIcons[assignments[i]];
                Vector2 start = new Vector2(slot.center.x, -slot.center.y);
                var icon = pieces[i];
                icon.rectTransform.anchoredPosition = Vector2.Lerp(start, destination, progress) +
                    Vector2.up * (Mathf.Sin(progress * Mathf.PI) * (60f - i * 12f));
                icon.rectTransform.localScale = Vector3.one * (1f - progress * 0.65f);
                icon.color = new Color(1f, 1f, 1f, 1f - progress);
            }
            product.color = Color.Lerp(Color.white, Color.clear, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        bool submitted = WorkshopProgress.Submit(season);
        completed = submitted || WorkshopProgress.IsComplete(season);
        if (completed)
        {
            elapsed = 0f;
            const float reveal = 0.28f;
            while (elapsed < reveal)
            {
                elapsed += Time.unscaledDeltaTime;
                product.color = new Color(1f, 1f, 1f, Mathf.Clamp01(elapsed / reveal));
                yield return null;
            }
        }

        Busy = false;
        Refresh();
        status.text = completed ? "已获得" + WorkshopProgress.ProductName(season) + "。" : "材料状态已变化，请重新查看。";
        if (submitted)
            WorkshopNarrativeView.PlaySeasonReview(season, () => WorkshopCraftRecordView.ShowChapterRecord(season));
        else if (completed)
            WorkshopNarrativeView.PlaySeasonReview(season);
    }
}
