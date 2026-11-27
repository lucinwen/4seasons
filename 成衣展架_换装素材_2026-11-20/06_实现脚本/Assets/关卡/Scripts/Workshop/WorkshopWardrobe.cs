using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>The workshop's fitting room. Previewing a style never changes the sewn garment.</summary>
public sealed class WorkshopWardrobe : MonoBehaviour
{
    private sealed class Style
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Rect? ArtworkBounds;
        public readonly float CollarOffset;
        public readonly float Length;

        public Style(string id, string name = null, Rect? artworkBounds = null, float collarOffset = 0f, float length = 274f)
        {
            Id = id;
            Name = name ?? WorkshopTailor.GarmentName(id);
            ArtworkBounds = artworkBounds;
            CollarOffset = collarOffset;
            Length = length;
        }
    }

    private sealed class Card
    {
        public Style Style;
        public UnityEngine.UI.Image Border;
        public UnityEngine.UI.Image Surface;
    }

    private static readonly string[] CategoryNames = { "红粉", "明黄", "青绿", "霁蓝", "烟紫" };
    private static readonly Color32[] CategoryColors =
    {
        new Color32(149, 62, 62, 255), new Color32(193, 142, 62, 255),
        new Color32(83, 121, 85, 255), new Color32(83, 112, 161, 255), new Color32(130, 88, 151, 255)
    };
    private static readonly Style[][] Catalog =
    {
        new[]
        {
            new Style("1_01"), new Style("3_01"), new Style("3_12", artworkBounds: new Rect(0, 0, 309, 293)),
            new Style("3_16"), new Style("1_06", "粉绫齐胸裙", collarOffset: 9f, length: 266f), new Style("3_08", "桃花长衣")
        },
        new[] { new Style("1_05"), new Style("3_07"), new Style("3_10", "霞云交领", new Rect(21, 0, 276, 293)) },
        new[]
        {
            new Style("1_03"), new Style("3_03", "竹影对襟", length: 254f), new Style("3_06", length: 246f),
            new Style("3_13"), new Style("1_07", "白鹤交领"), new Style("3_11", "青鹤云纹", new Rect(0, 0, 295, 293))
        },
        new[]
        {
            new Style("1_02", artworkBounds: new Rect(24, 0, 350, 416)), new Style("3_02"),
            new Style("3_05"), new Style("3_09", artworkBounds: new Rect(0, 0, 307, 290)),
            new Style("1_08", "霜纹披风", new Rect(7, 0, 369, 421)), new Style("3_15", "金龙长衣", new Rect(0, 0, 276, 302))
        },
        new[]
        {
            new Style("1_04", artworkBounds: new Rect(7, 0, 369, 408)),
            new Style("3_04", collarOffset: 13f, length: 263f), new Style("3_14")
        }
    };

    private static readonly Color32 Gold = new Color32(238, 196, 119, 255);
    private static readonly Color32 Brass = new Color32(152, 101, 65, 255);
    private static readonly Color32 DarkWood = new Color32(59, 37, 29, 255);
    private static readonly Color32 Jade = new Color32(39, 74, 61, 255);
    private static readonly Rect PanoramaBounds = new Rect(0f, 0f, 1920f, 1080f);
    private static readonly Rect HeroBounds = new Rect(320f, 307f, 260f, 540f);
    private const float NeckX = 462f;
    private const float NeckY = 572f;
    private const float FittingScale = 540f / 620f;

    private static WorkshopWardrobe active;
    private readonly List<Card> cards = new List<Card>();
    private readonly UnityEngine.UI.Image[] tabs = new UnityEngine.UI.Image[CategoryNames.Length];
    private readonly UnityEngine.UI.Image[] tabBorders = new UnityEngine.UI.Image[CategoryNames.Length];
    private RectTransform frame;
    private RectTransform catalog;
    private RectTransform wornClip;
    private UnityEngine.UI.Image worn;
    private Text styleName;
    private Text previewStatus;
    private string sewnId;
    private int category;
    private Style selected;
    private Action onClose;

    public static bool IsOpen => active != null;

    public static void Show(Action onClose = null)
    {
        if (active != null) return;
        var frame = WorkshopUi.NewCanvas("WorkshopWardrobe", 11000);
        active = frame.gameObject.AddComponent<WorkshopWardrobe>();
        active.Open(frame, onClose);
    }

    private void Open(RectTransform layout, Action close)
    {
        frame = layout;
        onClose = close;
        sewnId = CanonicalId(WorkshopProgress.GarmentId);
        WorkshopUi.Block(frame, new Color32(34, 24, 21, 255));
        DrawBackdrop();

        var title = Label(frame, "Wardrobe Title", new Rect(76f, 34f, 570f, 75f), "成衣展架", 64, WorkshopUi.Cream);
        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(31, 33, 27, 255);
        shadow.effectDistance = new Vector2(2f, -4f);
        var back = FramedButton(frame, "Close Wardrobe", new Rect(1615f, 43f, 215f, 62f), "返回工坊", Close, DarkWood);
        WorkshopUi.Picture(frame, "Fitting Hero", HeroBounds, Resources.Load<Sprite>("WorkshopFinish/FittingHero"), stretch: true);
        wornClip = Clip(frame, "Fitted Artwork");
        worn = WorkshopUi.Picture(wornClip, "Worn Garment", new Rect(), null, stretch: true);

        var plaque = WorkshopUi.Fill(frame, "Fitting Plaque", new Rect(170f, 933f, 574f, 99f), Brass).transform;
        WorkshopUi.Fill(plaque, "Plaque Inset", new Rect(2f, 2f, 570f, 95f), DarkWood);
        styleName = Label(plaque, "Fitting Name", new Rect(30f, 3f, 514f, 55f), "", 42,
            WorkshopUi.Cream, TextAnchor.MiddleCenter);
        previewStatus = Label(plaque, "Fitting Status", new Rect(30f, 57f, 514f, 35f), "", 25,
            Gold, TextAnchor.MiddleCenter);

        Label(frame, "Catalog Title", new Rect(1043f, 204f, 572f, 58f), "试穿衣样", 46, Jade);

        for (int i = 0; i < CategoryNames.Length; i++)
        {
            int index = i;
            var bounds = new Rect(915f + 175.5f * i, 278f, 164f, 56f);
            tabBorders[i] = WorkshopUi.Fill(frame, "Wardrobe Tab Frame", bounds, Brass);
            var button = WorkshopUi.Button(tabBorders[i].transform, "Wardrobe Category " + i,
                new Rect(2f, 2f, 160f, 52f), "", () => Browse(index), DarkWood);
            tabs[i] = button.GetComponent<UnityEngine.UI.Image>();
            WorkshopUi.Fill(button.transform, "Dye Swatch", new Rect(17f, 17f, 17f, 17f), CategoryColors[i]);
            Label(button.transform, "Dye Category", new Rect(44f, 2f, 98f, 46f), CategoryNames[i], 29, WorkshopUi.Cream);
        }

        int initialCategory = 0;
        if (FindStyle(sewnId, out int savedCategory, out _))
        {
            initialCategory = savedCategory;
            FramedButton(frame, "Restore Sewn Garment", new Rect(1246f, 848f, 331f, 61f), "穿回我的成衣", RestoreSewn, Jade);
        }

        Browse(initialCategory, sewnId);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(back.gameObject);
    }

    private void DrawBackdrop()
    {
        Texture2D winter = WorkshopArt.SeasonBackground(WorkshopSeason.Winter);
        if (winter != null)
        {
            var panorama = WorkshopUi.Rect(frame, "Fitting Winter Panorama", PanoramaBounds).gameObject.AddComponent<RawImage>();
            panorama.texture = winter;
            panorama.raycastTarget = false;
            float ratio = (float)winter.width * PanoramaBounds.height / (winter.height * PanoramaBounds.width);
            // Put the open snowy river behind the fitting stage and the distant workshop behind the wardrobe panel.
            if (ratio > 1f) panorama.uvRect = new Rect((1f + 1f / ratio) * 0.5f, 0f, -1f / ratio, 1f);
            else panorama.uvRect = new Rect(1f, (1f - ratio) * 0.5f, -1f, ratio);
        }
        WorkshopUi.Fill(frame, "Fitting Winter Shade", PanoramaBounds, new Color32(28, 43, 44, 112));
        WorkshopUi.Picture(frame, "Fitting Stage", new Rect(90f, 106f, 735f, 817f),
            Resources.Load<Sprite>("WorkshopFinish/FittingStage"), stretch: true);
        WorkshopUi.Panel(frame, "Wardrobe Cabinet", new Rect(834f, 160f, 1000f, 817f), WorkshopArt.WorkshopTaskBoard);
    }

    private Color CardColor(bool fitted)
    {
        var paper = fitted ? new Color32(246, 216, 158, 255) : new Color32(209, 188, 146, 255);
        return Color.Lerp(paper, CategoryColors[category], fitted ? 0.28f : 0.42f);
    }

    private void Browse(int index, string requestedId = null)
    {
        if (catalog != null)
        {
            catalog.gameObject.SetActive(false);
            Destroy(catalog.gameObject);
        }
        cards.Clear();
        category = index;
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].color = i == category ? Jade : DarkWood;
            tabBorders[i].color = i == category ? Gold : Brass;
        }

        catalog = WorkshopUi.Rect(frame, "Wardrobe Catalog", new Rect(915f, 364f, 870f, 459f));
        var styles = Catalog[category];
        foreach (Style style in styles)
        {
            Sprite sprite = WorkshopArt.Garment(style.Id);
            if (sprite == null) continue;
            float verticalOffset = styles.Length <= 3 ? 100f : 0f;
            var bounds = new Rect(cards.Count % 3 * 298f, verticalOffset + cards.Count / 3 * 224f, 270f, 203f);
            var border = WorkshopUi.Fill(catalog, "Garment Card Frame", bounds, Brass);
            var button = WorkshopUi.Button(border.transform, "Try Garment " + style.Id,
                new Rect(2f, 2f, 266f, 199f), "", () => TryOn(style), CardColor(false));
            var surface = button.GetComponent<UnityEngine.UI.Image>();
            WorkshopUi.Fill(button.transform, "Garment Tag", new Rect(0f, 155f, 266f, 44f), DarkWood);
            Label(button.transform, "Garment Card Name", new Rect(5f, 157f, 256f, 38f), style.Name, 25,
                WorkshopUi.Cream, TextAnchor.MiddleCenter);

            var clip = Clip(button.transform, "Card Artwork");
            var picture = WorkshopUi.Picture(clip, "Garment Illustration", new Rect(), sprite, stretch: true);
            PlaceInBox(clip, picture, sprite, SourceBounds(style, sprite), new Rect(39f, 7f, 188f, 144f));

            if (style.Id == sewnId)
            {
                var badge = WorkshopUi.Fill(button.transform, "Made Garment", new Rect(5f, 6f, 66f, 32f), Jade);
                Label(badge.transform, "Made Garment Label", new Rect(0f, 0f, 66f, 31f), "已制成",
                    19, WorkshopUi.Cream, TextAnchor.MiddleCenter);
            }
            cards.Add(new Card { Style = style, Border = border, Surface = surface });
        }

        // Browsing a color leaves the chosen clothing on the stage until a card is clicked.
        Style requested = null;
        foreach (Card card in cards)
            if (card.Style.Id == requestedId) requested = card.Style;
        if (requested != null) TryOn(requested);
        else if (selected == null && cards.Count != 0) TryOn(cards[0].Style);
        else RefreshSelection();
    }

    private void TryOn(Style style)
    {
        Sprite sprite = WorkshopArt.Garment(style.Id);
        if (sprite == null) return;

        Rect source = SourceBounds(style, sprite);
        float length = style.Length * FittingScale;
        float width = length * source.width / source.height;
        float neckCenter = style.Id == "3_05" || style.Id == "3_09" ? 0.524f : 0.5f;
        var placement = new Rect(NeckX - width * neckCenter, NeckY + style.CollarOffset * FittingScale, width, length);
        PlaceArtwork(wornClip, worn, sprite, source, placement);
        selected = style;
        styleName.text = style.Name;
        previewStatus.text = style.Id == sewnId ? "我的成衣 · 已制成" : "试穿预览";
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (Card card in cards)
        {
            bool isFitted = selected == card.Style;
            card.Border.color = isFitted ? Gold : Brass;
            card.Surface.color = CardColor(isFitted);
        }
    }

    private void RestoreSewn()
    {
        if (FindStyle(sewnId, out int savedCategory, out var style))
        {
            if (savedCategory == category) TryOn(style);
            else Browse(savedCategory, style.Id);
        }
    }

    private static bool FindStyle(string id, out int foundCategory, out Style result)
    {
        for (int i = 0; i < Catalog.Length; i++)
        {
            foreach (Style style in Catalog[i])
            {
                if (style.Id != id || WorkshopArt.Garment(id) == null) continue;
                foundCategory = i;
                result = style;
                return true;
            }
        }
        foundCategory = 0;
        result = null;
        return false;
    }

    private static string CanonicalId(string id)
    {
        // The two source sheets of eight costumes contain identical pairs.
        return !string.IsNullOrEmpty(id) && id[0] == '2' ? "1" + id.Substring(1) : id;
    }

    private static Rect SourceBounds(Style style, Sprite sprite) =>
        style?.ArtworkBounds ?? new Rect(Vector2.zero, sprite.rect.size);

    internal static RectTransform Clip(Transform parent, string name)
    {
        var clip = WorkshopUi.Rect(parent, name, new Rect());
        clip.gameObject.AddComponent<RectMask2D>();
        return clip;
    }

    internal static void PlaceInBox(RectTransform clip, UnityEngine.UI.Image image, string garmentId, Rect window)
    {
        var sprite = WorkshopArt.Garment(garmentId);
        if (sprite == null) return;
        FindStyle(CanonicalId(garmentId), out _, out var style);
        PlaceInBox(clip, image, sprite, SourceBounds(style, sprite), window);
    }

    private static void PlaceInBox(RectTransform clip, UnityEngine.UI.Image image, Sprite sprite, Rect source, Rect window)
    {
        float scale = Mathf.Min(window.width / source.width, window.height / source.height);
        Vector2 display = source.size * scale;
        PlaceArtwork(clip, image, sprite, source, new Rect(window.center - display * 0.5f, display));
    }

    private static void PlaceArtwork(RectTransform clip, UnityEngine.UI.Image image, Sprite sprite, Rect source, Rect destination)
    {
        // Clip disconnected sheet fragments while retaining the original source sprite and its colors.
        clip.anchoredPosition = new Vector2(destination.x, -destination.y);
        clip.sizeDelta = destination.size;
        float scale = destination.width / source.width;
        image.rectTransform.anchoredPosition = new Vector2(-source.x * scale, source.y * scale);
        image.rectTransform.sizeDelta = sprite.rect.size * scale;
        image.sprite = sprite;
        image.enabled = true;
    }

    private static UnityEngine.UI.Button FramedButton(Transform parent, string name, Rect bounds, string label,
        Action action, Color fill)
    {
        var border = WorkshopUi.Fill(parent, name + " Border", bounds, Brass);
        var button = WorkshopUi.Button(border.transform, name, new Rect(2f, 2f, bounds.width - 4f, bounds.height - 4f),
            "", action, fill);
        Label(button.transform, "Label", new Rect(6f, 1f, bounds.width - 16f, bounds.height - 7f),
            label, 32, WorkshopUi.Cream, TextAnchor.MiddleCenter);
        return button;
    }

    private static Text Label(Transform parent, string name, Rect bounds, string value, int size,
        Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var label = WorkshopUi.Label(parent, name, bounds, value, size, color, alignment, WorkshopArt.WorkshopFont);
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Close()
    {
        if (active != this) return;
        active = null;
        WorkshopUi.CloseCanvas(frame);
        onClose?.Invoke();
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }
}
