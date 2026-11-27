using JiangnanDye;
using UnityEngine;

public sealed class WorkshopArtData : ScriptableObject
{
    public Font pixelFont;
    public Sprite workshopTaskBoard;
    public Sprite workshopChapterRail;
    public Sprite journalFrame;
    public Sprite rawSilk;
    public Sprite[] seasonPanels;
    public Sprite[] seasonButtons;
    public Sprite[] dialoguePanels;
    public Sprite dialoguePlayerPortrait;
    public Sprite dialogueStewardPortrait;
    public Sprite dialogueWorkshopIcon;
    public Sprite[] dialogueNpcPortraits;
    public Texture2D[] seasonBackgrounds;
    public Sprite[] itemCards;
    public Texture2D taskIconAtlas;
    public Sprite[] solarCards;
    public Sprite[] chapterProducts;
    public Sprite cuttingWorkbench;
    public Sprite[] cutPieces;
    public Sprite[] garments;
    public ItemData[] normalDyes;
    public ItemData[] damagedDyes;
}

public static class WorkshopArt
{
    private static WorkshopArtData data;
    private static Font readingFont;
    private static WorkshopArtData Data => data != null ? data : (data = Resources.Load<WorkshopArtData>("WorkshopArt"));

    private static readonly string[][] Terms =
    {
        new[] { "立春", "雨水", "惊蛰", "春分", "清明", "谷雨" },
        new[] { "立夏", "小满", "芒种", "夏至", "小暑", "大暑" },
        new[] { "立秋", "处暑", "白露", "秋分", "寒露", "霜降" },
        new[] { "立冬", "小雪", "大雪", "冬至", "小寒", "大寒" }
    };

    public static Font PixelFont => Data != null && Data.pixelFont != null ? Data.pixelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    public static Font WorkshopFont => PixelFont;
    internal static Font ReadingFont
    {
        get
        {
            if (readingFont != null) return readingFont;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var candidate = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei" }, 32);
            if (candidate != null && candidate.HasCharacter('织'))
            {
                candidate.hideFlags = HideFlags.HideAndDontSave;
                readingFont = candidate;
            }
#endif
            return readingFont != null ? readingFont : (readingFont = WorkshopFont);
        }
    }
    public static Sprite WorkshopTaskBoard => Data != null ? Data.workshopTaskBoard : null;
    public static Sprite WorkshopChapterRail => Data != null ? Data.workshopChapterRail : null;
    public static Sprite JournalFrame => Data != null ? Data.journalFrame : null;
    public static Sprite RawSilk => Data != null ? Data.rawSilk : null;
    public static Sprite ItemCard(WorkshopItem item) => RestoredItemCard(item) ?? Get(Data?.itemCards, (int)item);
    private static Sprite RestoredItemCard(WorkshopItem item) => item == WorkshopItem.Suoxu
        ? Resources.Load<Sprite>("WorkshopCardHD/Suoxu") : item == WorkshopItem.Spool
        ? Resources.Load<Sprite>("WorkshopCardHD/Spool") : item == WorkshopItem.Statue
        ? Resources.Load<Sprite>("WorkshopCardHD/Statue") : null;
    internal static bool ItemCardHasPrintedCopy(WorkshopItem item) =>
        item == WorkshopItem.Suoxu || item == WorkshopItem.Spool ||
        item == WorkshopItem.RawSilkCloth || item == WorkshopItem.SilkwormEggSheet;
    internal static bool TryIdentifyItemCard(Sprite sprite, out WorkshopItem item)
    {
        var cards = Data?.itemCards;
        int index = sprite != null && cards != null ? System.Array.IndexOf(cards, sprite) : -1;
        if (index < 0 && sprite != null && cards != null)
            for (int i = 0; i < cards.Length; i++)
                if (sprite == ItemCard((WorkshopItem)i)) { index = i; break; }
        item = (WorkshopItem)index;
        return index >= 0;
    }

    public static Texture2D TaskIconAtlas => Data != null ? Data.taskIconAtlas : null;

    public static Rect TaskIconUV(WorkshopItem item)
    {
        // Each item occupies one row-major tile in the 4 by 4 atlas.
        const int cells = 4;
        const float tileSize = 1f / cells;
        int index = (int)item;
        return new Rect((index % cells) * tileSize, (cells - 1 - index / cells) * tileSize,
            tileSize, tileSize);
    }
    public static Sprite SolarCard(WorkshopSeason season, int index) => index >= 0 && index < 6 ? Get(Data?.solarCards, (int)season * 6 + index) : null;
    public static string[] SolarNames(WorkshopSeason season) => (string[])Terms[(int)season].Clone();
    public static Sprite SeasonPanel(WorkshopSeason season) => Get(Data?.seasonPanels, (int)season);
    public static Sprite SeasonButton(WorkshopSeason season) => Get(Data?.seasonButtons, (int)season);
    internal static Sprite DialoguePanel(WorkshopSeason season) => Get(Data?.dialoguePanels, (int)season);
    internal static Sprite DialoguePlayerPortrait => Data?.dialoguePlayerPortrait;
    internal static Sprite DialogueStewardPortrait => Data?.dialogueStewardPortrait;
    internal static Sprite DialogueWorkshopIcon => Data?.dialogueWorkshopIcon;
    internal static Sprite DialogueNpcPortrait(WorkshopSeason season) => Get(Data?.dialogueNpcPortraits, (int)season);
    public static Texture2D SeasonBackground(WorkshopSeason season) => Get(Data?.seasonBackgrounds, (int)season);
    public static Sprite CuttingWorkbench => Data != null ? Data.cuttingWorkbench : null;
    public static Sprite CutPiece(int index) => Get(Data?.cutPieces, index);
    public static Sprite ChapterProduct(WorkshopSeason season)
    {
        var product = season switch
        {
            WorkshopSeason.Spring => Resources.Load<Sprite>("WorkshopFinish/Cocoon"),
            WorkshopSeason.Summer => Resources.Load<Sprite>("WorkshopFinish/RawSilk"),
            WorkshopSeason.Autumn => Resources.Load<Sprite>("WorkshopFinish/SilkFabric") ?? PlainSilk,
            _ => null
        };
        return product ?? Get(Data?.chapterProducts, (int)season);
    }

    internal static Sprite PlainSilk => Resources.Load<Sprite>("WorkshopFinish/PlainSilk") ??
        Get(Data?.chapterProducts, (int)WorkshopSeason.Autumn);

    public static ItemData RawDye(DyeColor color, bool healthy) => Get(healthy ? Data?.normalDyes : Data?.damagedDyes, (int)color);

    public static Sprite Garment(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length != 4 || id[1] != '_' || !int.TryParse(id.Substring(2), out int version)) return null;
        switch (id[0])
        {
            case '1': return version >= 1 && version <= 8 ? Get(Data?.garments, version - 1) : null;
            case '2': return version >= 1 && version <= 8 ? Get(Data?.garments, version + 7) : null;
            case '3': return version >= 1 && version <= 16 ? Get(Data?.garments, version + 15) : null;
            case '4': return version >= 1 && version <= 6 ? Get(Data?.garments, version + 31) : null;
            default: return null;
        }
    }

    private static T Get<T>(T[] items, int index) where T : UnityEngine.Object =>
        items != null && index >= 0 && index < items.Length ? items[index] : null;
}
