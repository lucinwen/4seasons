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
        public readonly string ArtworkPath;
        public readonly string Material;
        public readonly string SampleId;
        public readonly Rect? SampleBounds;

        private Sprite exhibitionArtwork;
        private bool exhibitionLoaded;
        private Sprite silkArtwork;
        private bool silkLoaded;

        public Style(string id, string name = null, Rect? artworkBounds = null, float collarOffset = 0f,
            float length = 274f, string artworkPath = null, string material = null,
            string sampleId = null, Rect? sampleBounds = null)
        {
            Id = id;
            Name = name ?? WorkshopTailor.GarmentName(id);
            ArtworkBounds = artworkBounds;
            CollarOffset = collarOffset;
            Length = length;
            ArtworkPath = artworkPath;
            Material = material;
            SampleId = sampleId;
            SampleBounds = sampleBounds;
        }

        public Sprite ExhibitionArtwork
        {
            get
            {
                if (!exhibitionLoaded && ArtworkPath != null)
                {
                    exhibitionArtwork = Resources.Load<Sprite>(ArtworkPath);
                    exhibitionLoaded = true;
                }
                return exhibitionArtwork;
            }
        }

        public Sprite Artwork => ArtworkPath == null
            ? WorkshopArt.Garment(Id)
            : ExhibitionArtwork ?? WorkshopArt.Garment(SampleId);

        public bool UsesSample => ArtworkPath != null && ExhibitionArtwork == null;
        public WorkshopWardrobeArtIndex.Item ArtIndex => WorkshopWardrobeArtIndex.Find(SampleId ?? Id);

        public Sprite SilkArtwork
        {
            get
            {
                if (!silkLoaded && ArtworkPath == null)
                {
                    silkArtwork = Resources.Load<Sprite>(ArtIndex?.silkResource ?? ("WorkshopFinish/SilkVariants/" + Id));
                    silkLoaded = true;
                }
                return silkArtwork;
            }
        }

        public Sprite MaterialDetail => ArtworkPath == null || UsesSample
            ? null
            : Resources.Load<Sprite>(ArtworkPath + "Detail");
    }

    private sealed class Card
    {
        public Style Style;
        public UnityEngine.UI.Image Border;
        public UnityEngine.UI.Image Surface;
        public UnityEngine.UI.Image Caption;
    }

    private static readonly string[] CategoryNames = { "四时丝衣", "红粉", "明黄", "青绿", "霁蓝", "烟紫" };
    private static readonly Color32[] CategoryColors =
    {
        new Color32(250, 219, 160, 255), new Color32(218, 125, 132, 255), new Color32(231, 186, 103, 255),
        new Color32(150, 192, 157, 255), new Color32(142, 179, 222, 255), new Color32(194, 159, 213, 255)
    };
    private static readonly Style[][] Catalog =
    {
        new[]
        {
            new Style("silk_spring", "桑芽春衫", length: 283f, artworkPath: "WorkshopFinish/Silk/Spring",
                material: "薄绫轻软，细密斜纹在光下微微发亮。", sampleId: "1_01"),
            new Style("silk_summer", "水碧夏衣", length: 266f, artworkPath: "WorkshopFinish/Silk/Summer",
                material: "轻纱透光，叠合处的颜色会更深。", sampleId: "3_07"),
            new Style("silk_autumn", "桂金秋袍", length: 283f, artworkPath: "WorkshopFinish/Silk/Autumn",
                material: "纹样随经纬织出，花与地各有光泽。", sampleId: "3_03"),
            new Style("silk_winter", "霁雪冬氅", length: 290f, artworkPath: "WorkshopFinish/Silk/Winter",
                material: "厚缎垂坠，亮面沿褶痕收成柔和的细光。", sampleId: "3_09",
                sampleBounds: new Rect(0, 0, 307, 290))
        },
        new[]
        {
            new Style("1_01", material: "交领与袖面沿褶线泛起柔光。"),
            new Style("3_01"), new Style("3_12", artworkBounds: new Rect(0, 0, 309, 293)),
            new Style("3_16"), new Style("1_06", "粉绫齐胸裙", collarOffset: 9f, length: 266f), new Style("3_08", "桃花长衣")
        },
        new[]
        {
            new Style("1_05"), new Style("3_07"), new Style("3_10", "霞云交领", new Rect(21, 0, 276, 293)),
            new Style("4_01"), new Style("4_02"), new Style("4_03")
        },
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
            new Style("3_04", collarOffset: 13f, length: 263f), new Style("3_14"),
            new Style("4_04"), new Style("4_05"), new Style("4_06")
        }
    };

    private static readonly Color Night = new Color32(16, 28, 48, 255);
    private static readonly Color Ink = new Color32(28, 46, 68, 255);
    private static readonly Color ActiveInk = new Color32(53, 72, 96, 255);
    private static readonly Color IndigoEdge = new Color32(91, 113, 144, 255);
    private static readonly Color Ivory = new Color32(255, 245, 222, 255);
    private static readonly Color Mist = new Color32(194, 211, 233, 255);
    private static readonly Color Gold = new Color32(247, 208, 142, 255);
    private static readonly Color Brass = new Color32(170, 142, 103, 255);
    private static readonly Color CardEdge = new Color32(140, 136, 125, 255);
    private static readonly Color SilkTint = new Color32(243, 244, 248, 255);
    private static readonly Color SelectedCaption = new Color32(91, 65, 41, 255);
    private static readonly Rect PanoramaBounds = new Rect(0f, 0f, 1920f, 1080f);
    private const float StageX = 103f;
    private const float StageY = 166f;
    private const float StageScale = 0.916f;
    private static readonly Rect HeroBounds = StageRect(202f, 108f, 260f * 630f / 540f, 630f);
    private static readonly Rect DetailWindow = new Rect(42f, 142f, 820f, 492f);

    private static WorkshopWardrobe active;
    private readonly List<Card> cards = new List<Card>();
    private readonly UnityEngine.UI.Image[] tabs = new UnityEngine.UI.Image[CategoryNames.Length];
    private readonly UnityEngine.UI.Image[] tabBorders = new UnityEngine.UI.Image[CategoryNames.Length];
    private RectTransform layout;
    private RectTransform frame;
    private RectTransform catalogPane;
    private RectTransform catalog;
    private RectTransform wornClip;
    private UnityEngine.UI.Image worn;
    private Sprite silkBacking;
    private Text styleName;
    private Text previewStatus;
    private Text materialToggleLabel;
    private UnityEngine.UI.Button materialToggle;
    private UnityEngine.UI.Button detailEntry;
    private RectTransform detailPanel;
    private RectTransform detailClip;
    private UnityEngine.UI.Image detailImage;
    private Text detailVersion;
    private Text detailToggleLabel;
    private UnityEngine.UI.Button arEntry;
    private UnityEngine.UI.Button photoEntry;
    private UnityEngine.UI.Button productionEntry;
    private WorkshopWardrobePhotoMode photoMode;
    private WorkshopWardrobeMirror mirror;
    private string sewnId;
    private int category;
    private int photoBackdrop;
    private Style selected;
    private Sprite originalGarment;
    private Sprite silkGarment;
    private bool showSilk;
    private float outfitPulseUntil;
    private Action onClose;

    public static bool IsOpen => active != null;

    private bool CanChangeMaterial => silkGarment != null;
    private Sprite PreviewGarment => showSilk ? silkGarment : originalGarment;
    private string MaterialToggleText => showSilk ? "看原版" : "看丝绸版";
    private string PreviewVersion => selected.ArtworkPath != null
        ? (selected.UsesSample ? "样衣示意" : "四时丝衣")
        : (showSilk ? "丝绸版" : "原版");
    private bool HasMaterialDetail => CanChangeMaterial || selected.MaterialDetail != null;

    public static void Show(Action onClose = null)
    {
        if (active != null) return;
        var frame = WorkshopUi.NewCanvas("WorkshopWardrobe", 11000);
        active = frame.gameObject.AddComponent<WorkshopWardrobe>();
        active.Open(frame, onClose);
    }

    private void Open(RectTransform layout, Action close)
    {
        this.layout = layout;
        frame = WorkshopUi.Rect(layout, "Wardrobe Page", PanoramaBounds);
        onClose = close;
        sewnId = CanonicalId(WorkshopProgress.GarmentId);
        WorkshopUi.Block(layout, Night);
        WorkshopUi.Picture(frame, "Fitting Winter Courtyard", PanoramaBounds,
            Resources.Load<Sprite>("WorkshopFinish/WardrobeWinterCourtyard"), stretch: true);
        DrawFittingArea();
        DrawCatalog();

        if (FindStyle(sewnId, out _, out _))
            productionEntry = WardrobeButton(frame, "Open Wardrobe Production Record", new Rect(897f, 48f, 240f, 55f),
                "看制作记录", OpenProductionRecord);
        arEntry = WardrobeButton(frame, "Open Wardrobe Mirror", new Rect(1160f, 48f, 230f, 55f), "AR 镜前试衣", OpenMirror, primary: true);
        arEntry.interactable = selected != null;
        photoEntry = WardrobeButton(frame, "Open Wardrobe Photo", new Rect(1410f, 48f, 211f, 55f), "拍照打卡", OpenPhoto);
        photoEntry.interactable = selected != null;
        var back = WardrobeButton(frame, "Close Wardrobe", new Rect(1645f, 48f, 191f, 55f), "← 返回工坊", Close);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(back.gameObject);
    }

    private void OpenProductionRecord()
    {
        frame.gameObject.SetActive(false);
        WorkshopCraftRecordView.ShowProductionRecord(() =>
        {
            if (this == null || frame == null) return;
            frame.gameObject.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(productionEntry.gameObject);
        });
    }

    private void OpenPhoto()
    {
        if (selected == null || photoMode != null) return;

        Style outfit = selected;
        Sprite garment = PreviewGarment;
        photoMode = WorkshopWardrobePhotoMode.Show(layout, outfit.Name + " · " + PreviewVersion, photoBackdrop,
            (parent, bounds) =>
            {
                WorkshopUi.Picture(parent, "Photo Hero", bounds, Resources.Load<Sprite>("WorkshopFinish/FittingHero"), stretch: true);
                var clip = Clip(parent, "Photo Fitted Artwork");
                var artwork = WorkshopUi.Picture(clip, "Photo Worn Garment", new Rect(), null, stretch: true);
                PlaceFittedGarment(clip, artwork, outfit, garment, bounds);
            }, index =>
            {
                photoMode = null;
                photoBackdrop = index;
                frame.gameObject.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(photoEntry.gameObject);
            });
        if (photoMode != null) frame.gameObject.SetActive(false);
    }

    private void OpenMirror()
    {
        if (selected == null || mirror != null) return;

        mirror = WorkshopWardrobeMirror.Show(layout, CycleStyle, () =>
        {
            mirror = null;
            frame.gameObject.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(arEntry.gameObject);
        }, ToggleMaterial, OpenPhotoFromMirror);
        RefreshMirror();
        frame.gameObject.SetActive(false);
    }

    private void OpenPhotoFromMirror()
    {
        mirror.Close();
        OpenPhoto();
    }

    private void RefreshMirror()
    {
        if (mirror == null || selected == null) return;
        Style style = selected;
        Sprite sprite = PreviewGarment;
        bool materialVersion = showSilk;
        Action<RectTransform, UnityEngine.UI.Image, Rect> drawDetail = null;
        if (HasMaterialDetail)
            drawDetail = (clip, image, window) => PlaceMaterial(clip, image, style, sprite, window, materialVersion);
        mirror.SetOutfit(style.Name, PreviewVersion,
            HasMaterialDetail ? (style.Material ?? style.ArtIndex?.note) : null,
            (clip, image, hero) => PlaceFittedGarment(clip, image, style, sprite, hero),
            (clip, image, shoulderWidth) => PlaceCameraGarment(clip, image, style, sprite, shoulderWidth),
            drawDetail,
            CanChangeMaterial ? MaterialToggleText : null);
    }

    private static Rect StageRect(float x, float y, float width, float height) =>
        new Rect(StageX + x * StageScale, StageY + y * StageScale, width * StageScale, height * StageScale);

    private void DrawFittingArea()
    {
        Label(frame, "Wardrobe Title", new Rect(133f, 23f, 601f, 89f), WorkshopHallSignage.WardrobeName, 70, Ivory, TextAnchor.MiddleCenter);
        WorkshopUi.Fill(frame, "Title Rule", new Rect(367f, 123f, 134f, 2f), Brass);
        WorkshopUi.Fill(frame, "Fitting Screen Backing", StageRect(64f, 160f, 594f, 516f),
            new Color32(211, 214, 198, 255));
        WorkshopUi.Picture(frame, "Fitting Stage", StageRect(0f, 0f, 722f, 785f),
            Resources.Load<Sprite>("WorkshopFinish/FittingStageLanterns"), stretch: true);
        PixelFill(frame, "Fitting Floor Shadow", StageRect(309f, 723f, 104f, 15f), new Color32(24, 40, 40, 65));
        WorkshopUi.Picture(frame, "Fitting Hero", HeroBounds,
            Resources.Load<Sprite>("WorkshopFinish/FittingHero"), stretch: true);
        wornClip = Clip(frame, "Fitted Artwork");
        worn = WorkshopUi.Picture(wornClip, "Worn Garment", new Rect(), null, stretch: true);
        var swipeArea = WorkshopUi.Fill(frame, "Wardrobe Swipe Area", StageRect(76f, 175f, 572f, 505f), Color.clear);
        swipeArea.raycastTarget = true;
        swipeArea.gameObject.AddComponent<WorkshopWardrobeSwipe>().Configure(CycleStyle);

        WardrobeButton(frame, "Previous Garment", new Rect(133f, 907f, 69f, 61f), "←", () => CycleStyle(-1));
        WardrobeButton(frame, "Next Garment", new Rect(667f, 907f, 69f, 61f), "→", () => CycleStyle(1));
        materialToggle = WardrobeButton(frame, "Compare Garment Material", new Rect(488f, 170f, 240f, 48f),
            "看丝绸版", ToggleMaterial, primary: true);
        materialToggleLabel = materialToggle.GetComponentInChildren<Text>();
        materialToggle.transform.parent.gameObject.SetActive(false);
        detailEntry = WardrobeButton(frame, "Open Fabric Detail", new Rect(488f, 229f, 240f, 48f),
            "布料特写", OpenMaterialDetail);
        detailEntry.transform.parent.gameObject.SetActive(false);
        var hint = WorkshopUi.PixelCard(frame, "Fitting Hint", new Rect(162f, 1003f, 542f, 42f), Night, IndigoEdge);
        Label(hint, "Preview Controls", new Rect(12f, 0f, 518f, 42f),
            "滑动或按 ← / → 换衣", 24, Mist, TextAnchor.MiddleCenter);

        var plaque = WorkshopUi.PixelCard(frame, "Fitting Plaque", new Rect(220f, 884f, 428f, 110f), Night, Brass);
        styleName = Label(plaque, "Fitting Name", new Rect(26f, 10f, 376f, 58f), "", 48, Ivory, TextAnchor.MiddleCenter);
        previewStatus = Label(plaque, "Fitting Status", new Rect(26f, 69f, 376f, 31f), "", 24, Gold, TextAnchor.MiddleCenter);
    }

    private void DrawCatalog()
    {
        silkBacking = Resources.Load<Sprite>("WorkshopFinish/WardrobeSilkBacking");
        catalogPane = WorkshopUi.Rect(frame, "Wardrobe Catalog Area", new Rect(843f, 146f, 993f, 825f));
        Label(catalogPane, "Catalog Title", new Rect(0f, 0f, 230f, 64f), "试穿衣样", 49, Ivory);

        const float tabGap = 11f;
        float tabWidth = (993f - (CategoryNames.Length - 1) * tabGap) / CategoryNames.Length;
        for (int i = 0; i < CategoryNames.Length; i++)
        {
            int index = i;
            var bounds = new Rect((tabWidth + tabGap) * i, 80f, tabWidth, 55f);
            tabBorders[i] = PixelFill(catalogPane, "Wardrobe Tab Frame", bounds, IndigoEdge);
            var button = WorkshopUi.Button(tabBorders[i].transform, "Wardrobe Category " + i,
                new Rect(2f, 2f, tabWidth - 4f, 51f), "", () => Browse(index, index == 0 ? Catalog[0][0].Id : null), Ink);
            tabs[i] = button.GetComponent<UnityEngine.UI.Image>();
            PixelFill(button.transform, "Dye Swatch", new Rect(14f, 17f, 17f, 17f), CategoryColors[i]);
            Label(button.transform, "Dye Category", new Rect(40f, 0f, tabWidth - 47f, 51f), CategoryNames[i], 25, Ivory);
        }

        int initialCategory = 1;
        if (FindStyle(sewnId, out int savedCategory, out _))
        {
            initialCategory = savedCategory;
            WardrobeButton(catalogPane, "Restore Sewn Garment", new Rect(741f, 7f, 252f, 50f), "穿回我的成衣", RestoreSewn, primary: true);
        }

        Browse(initialCategory, sewnId);
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
            tabs[i].color = i == category ? ActiveInk : Ink;
            tabBorders[i].color = i == category ? Gold : IndigoEdge;
        }

        catalog = WorkshopUi.Rect(catalogPane, "Wardrobe Catalog", new Rect(0f, 159f, 993f, 664f));
        var styles = Catalog[category];
        bool singleRow = styles.Length <= 3;
        float cardHeight = singleRow ? 490f : 321f;
        float artworkHeight = singleRow ? 348f : 208f;
        foreach (Style style in styles)
        {
            Sprite sprite = style.Artwork;
            if (sprite == null) continue;
            var bounds = new Rect(cards.Count % 3 * 338f, cards.Count / 3 * 343f, 317f, cardHeight);
            var border = PixelFill(catalog, "Garment Card Frame", bounds, CardEdge);
            var button = WorkshopUi.Button(border.transform, "Try Garment " + style.Id,
                new Rect(3f, 3f, 311f, cardHeight - 6f), "", () => TryOn(style, playCue: true), Ivory);
            var surface = button.GetComponent<UnityEngine.UI.Image>();
            if (silkBacking != null)
            {
                surface.sprite = silkBacking;
                surface.type = UnityEngine.UI.Image.Type.Simple;
            }

            float captionY = cardHeight - 65f;
            var caption = WorkshopUi.Fill(button.transform, "Garment Caption", new Rect(0f, captionY, 311f, 59f), Ink);
            Label(caption.transform, "Garment Card Name", new Rect(7f, 0f, 297f, 59f), style.Name, 31,
                Ivory, TextAnchor.MiddleCenter);

            if (style.UsesSample)
            {
                var sample = PixelFill(button.transform, "Exhibition Sample", new Rect(12f, 11f, 124f, 32f), Ink);
                Label(sample.transform, "Exhibition Sample Label", new Rect(0f, 0f, 124f, 32f),
                    "样衣示意", 21, Ivory, TextAnchor.MiddleCenter);
            }

            var clip = Clip(button.transform, "Card Artwork");
            var picture = WorkshopUi.Picture(clip, "Garment Illustration", new Rect(), sprite, stretch: true);
            PlaceInBox(clip, picture, sprite, SourceBounds(style, sprite), new Rect(24f, 44f, 263f, artworkHeight));

            if (style.Id == sewnId)
            {
                var badge = PixelFill(button.transform, "Made Garment", new Rect(12f, 11f, 86f, 32f), Ink);
                Label(badge.transform, "Made Garment Label", new Rect(0f, 0f, 86f, 32f), "已制成", 21, Ivory, TextAnchor.MiddleCenter);
            }
            cards.Add(new Card { Style = style, Border = border, Surface = surface, Caption = caption });
        }

        // Browsing a color leaves the chosen clothing on the stage until a card is clicked.
        Style requested = null;
        foreach (Card card in cards)
            if (card.Style.Id == requestedId) requested = card.Style;
        if (requested != null) TryOn(requested);
        else if (selected == null && cards.Count != 0) TryOn(cards[0].Style);
        else RefreshSelection();
    }

    private void TryOn(Style style, bool playCue = false)
    {
        Sprite sprite = style.Artwork;
        if (sprite == null) return;

        bool changed = selected != style;
        selected = style;
        originalGarment = sprite;
        silkGarment = style.SilkArtwork;
        showSilk = false;
        styleName.text = style.Name;
        if (playCue && changed) outfitPulseUntil = Time.unscaledTime + 0.22f;
        RefreshPreview();
        if (arEntry != null) arEntry.interactable = true;
        if (photoEntry != null) photoEntry.interactable = true;
        RefreshSelection();
        if (playCue && changed) WorkshopAudioFeedback.Fabric();
    }

    private void CycleStyle(int direction)
    {
        if (cards.Count == 0) return;
        int index = cards.FindIndex(card => card.Style == selected);
        if (index < 0) index = direction > 0 ? -1 : 0;
        TryOn(cards[(index + direction + cards.Count) % cards.Count].Style, playCue: true);
    }

    private void ToggleMaterial()
    {
        if (!CanChangeMaterial) return;
        showSilk = !showSilk;
        RefreshPreview();
        WorkshopAudioFeedback.Fabric();
    }

    private void RefreshPreview()
    {
        PlaceFittedGarment(wornClip, worn, selected, PreviewGarment, HeroBounds);
        materialToggle.transform.parent.gameObject.SetActive(CanChangeMaterial);
        materialToggleLabel.text = MaterialToggleText;
        detailEntry.transform.parent.gameObject.SetActive(HasMaterialDetail);
        previewStatus.text = selected.Id == sewnId ? PreviewVersion + " · 我的成衣" : PreviewVersion;
        RefreshMaterialDetail();
        RefreshMirror();
    }

    private void OpenMaterialDetail()
    {
        if (!HasMaterialDetail || detailPanel != null) return;

        detailPanel = WorkshopUi.Rect(layout, "Wardrobe Material Detail", PanoramaBounds);
        WorkshopUi.Fill(detailPanel, "Fabric Detail Backdrop", PanoramaBounds, new Color32(16, 28, 48, 225))
            .raycastTarget = true;
        var dialog = WorkshopUi.PixelCard(detailPanel, "Fabric Detail Card", new Rect(508f, 172f, 904f, 736f), Night, Brass);
        Label(dialog, "Fabric Detail Title", new Rect(42f, 26f, 782f, 64f),
            selected.Name + " · 布料特写", 42, Ivory);
        detailVersion = Label(dialog, "Fabric Version", new Rect(42f, 94f, 820f, 34f), "", 27, Gold);
        WorkshopUi.Fill(dialog, "Fabric Detail Surface", DetailWindow, ActiveInk);
        detailClip = Clip(dialog, "Fabric Detail Crop");
        detailImage = WorkshopUi.Picture(detailClip, "Fabric Detail Artwork", new Rect(), null, stretch: true);
        var back = WardrobeButton(dialog, "Close Fabric Detail", new Rect(684f, 656f, 178f, 51f), "返回试衣", CloseMaterialDetail);
        if (CanChangeMaterial)
        {
            var toggle = WardrobeButton(dialog, "Toggle Fabric Detail Material", new Rect(42f, 656f, 226f, 51f),
                MaterialToggleText, ToggleMaterial, primary: true);
            detailToggleLabel = toggle.GetComponentInChildren<Text>();
        }
        frame.gameObject.SetActive(false);
        RefreshMaterialDetail();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(back.gameObject);
    }

    private void RefreshMaterialDetail()
    {
        if (detailPanel == null) return;
        detailVersion.text = PreviewVersion;
        if (detailToggleLabel != null) detailToggleLabel.text = MaterialToggleText;
        PlaceMaterial(detailClip, detailImage, selected, PreviewGarment, DetailWindow, showSilk);
    }

    private void CloseMaterialDetail()
    {
        detailPanel.gameObject.SetActive(false);
        Destroy(detailPanel.gameObject);
        detailPanel = null;
        detailToggleLabel = null;
        frame.gameObject.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(detailEntry.gameObject);
    }

    private static void PlaceFittedGarment(RectTransform clip, UnityEngine.UI.Image image, Style style, Sprite sprite, Rect heroBounds)
    {
        float fittingScale = heroBounds.height / 620f;
        Vector2 neck = heroBounds.position + new Vector2(142f, 265f) * (heroBounds.height / 540f);
        PlaceAtNeck(clip, image, style, sprite, neck + Vector2.up * style.CollarOffset * fittingScale,
            style.Length * fittingScale);
    }

    private static void PlaceAtNeck(RectTransform clip, UnityEngine.UI.Image image, Style style, Sprite sprite,
        Vector2 neck, float length)
    {
        Rect source = SourceBounds(style, sprite);
        float width = length * source.width / source.height;
        float neckCenter = style.Id == "3_05" || style.Id == "3_09" ? 0.524f : 0.5f;
        var placement = new Rect(neck.x - width * neckCenter, neck.y, width, length);
        PlaceArtwork(clip, image, sprite, source, placement);
    }

    private static void PlaceCameraGarment(RectTransform clip, UnityEngine.UI.Image image, Style style,
        Sprite sprite, float shoulderWidth)
    {
        Rect source = SourceBounds(style, sprite);
        var anchors = style.ArtworkPath == null || style.UsesSample ? style.ArtIndex?.arInitialAnchors : null;
        if (anchors != null && anchors.TryGet(out Vector2 midpoint, out float artworkShoulders))
        {
            float scale = shoulderWidth / artworkShoulders;
            PlaceArtwork(clip, image, sprite, source,
                new Rect((source.position - midpoint) * scale, source.size * scale));
            return;
        }

        float length = shoulderWidth * 1.95f;
        PlaceAtNeck(clip, image, style, sprite, new Vector2(0f, -length * 0.14f), length);
    }

    private static void PlaceMaterial(RectTransform clip, UnityEngine.UI.Image image, Style style, Sprite sprite,
        Rect window, bool isSilk)
    {
        var detail = style.MaterialDetail;
        if (style.ArtworkPath == null)
        {
            var art = style.ArtIndex;
            string detailResource = isSilk ? art?.silkDetailResource : art?.originalDetailResource;
            if (!string.IsNullOrEmpty(detailResource))
                detail = Resources.Load<Sprite>(detailResource);
            if (detail == null && art != null && art.detailCrop.width > 0f && art.detailCrop.height > 0f)
            {
                PlaceInBox(clip, image, sprite, art.detailCrop.Bounds, window);
                return;
            }
        }

        if (detail != null)
        {
            PlaceInBox(clip, image, detail, SourceBounds(null, detail), window);
            return;
        }

        Rect source = SourceBounds(style, sprite);
        Vector2 cropSize = source.size * 0.42f;
        PlaceInBox(clip, image, sprite, new Rect(source.center - cropSize * 0.5f, cropSize), window);
    }

    private void RefreshSelection()
    {
        foreach (Card card in cards)
        {
            bool isFitted = selected == card.Style;
            card.Border.color = isFitted ? Gold : CardEdge;
            card.Surface.color = isFitted ? Color.white : SilkTint;
            card.Caption.color = isFitted ? SelectedCaption : Ink;
        }
    }

    private void RestoreSewn()
    {
        if (FindStyle(sewnId, out int savedCategory, out var style))
        {
            bool changed = selected != style;
            if (savedCategory == category) TryOn(style);
            else Browse(savedCategory, style.Id);
            if (changed) WorkshopAudioFeedback.Fabric();
        }
    }

    private static bool FindStyle(string id, out int foundCategory, out Style result)
    {
        for (int i = 0; i < Catalog.Length; i++)
        {
            foreach (Style style in Catalog[i])
            {
                if (style.Id != id || style.Artwork == null) continue;
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
        (style != null && style.UsesSample ? style.SampleBounds : style?.ArtworkBounds) ??
        new Rect(Vector2.zero, sprite.rect.size);

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

    internal static UnityEngine.UI.Image PixelFill(Transform parent, string name, Rect bounds, Color color)
    {
        var surface = WorkshopUi.Fill(parent, name, bounds, color);
        WorkshopUi.ApplyPixelShape(surface);
        return surface;
    }

    internal static UnityEngine.UI.Button WardrobeButton(Transform parent, string name, Rect bounds, string label,
        Action action, bool primary = false)
    {
        var border = PixelFill(parent, name + " Frame", bounds, primary ? Brass : IndigoEdge);
        var button = WorkshopUi.Button(border.transform, name, new Rect(2f, 2f, bounds.width - 4f, bounds.height - 4f),
            "", action, primary ? ActiveInk : Night);
        Label(button.transform, "Label", new Rect(4f, 0f, bounds.width - 12f, bounds.height - 4f), label, 28,
            primary ? Ivory : Mist, TextAnchor.MiddleCenter);
        return button;
    }

    internal static Text Label(Transform parent, string name, Rect bounds, string value, int size,
        Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var label = WorkshopUi.Label(parent, name, bounds, value, size, color, alignment, WorkshopArt.WorkshopFont);
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private void Update()
    {
        if (WorkshopCraftRecordView.BlocksWorldInput) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mirror != null) mirror.Close();
            else if (photoMode != null) photoMode.Close();
            else if (detailPanel != null) CloseMaterialDetail();
            else Close();
            return;
        }

        if (photoMode != null || mirror != null || detailPanel != null) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) CycleStyle(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) CycleStyle(1);
        if (outfitPulseUntil > 0f)
        {
            float remaining = Mathf.Clamp01((outfitPulseUntil - Time.unscaledTime) / 0.22f);
            worn.color = new Color(1f, 1f, 1f, 1f - 0.32f * remaining);
            if (remaining == 0f) outfitPulseUntil = 0f;
        }
    }

    private void Close()
    {
        if (active != this) return;
        active = null;
        WorkshopUi.CloseCanvas(layout);
        onClose?.Invoke();
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }
}
