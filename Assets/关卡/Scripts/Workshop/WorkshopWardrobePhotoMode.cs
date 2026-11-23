using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static WorkshopWardrobe;

internal sealed class WorkshopWardrobePhotoMode : MonoBehaviour
{
    private sealed class Scenery
    {
        internal readonly string Name;
        internal readonly Texture2D Texture;
        internal readonly Rect View;
        internal readonly Vector2 Feet;

        internal Scenery(string name, Texture2D texture, Vector2 feet, Rect? crop = null)
        {
            Name = name;
            Texture = texture;
            Feet = feet;
            View = crop ?? new Rect(0f, 0f, 1f, 1f);
            if (texture == null) return;

            float sourceAspect = texture.width * View.width / (texture.height * View.height);
            const float targetAspect = PhotoWidth / PhotoHeight;
            Vector2 size = View.size;
            if (sourceAspect > targetAspect) size.x *= targetAspect / sourceAspect;
            else size.y *= sourceAspect / targetAspect;
            View = new Rect(View.center - size * 0.5f, size);
        }
    }

    private const float PhotoWidth = 1920f;
    private const float PhotoHeight = 1080f;
    private const float PortraitHeight = 588f;
    private const float PortraitWidth = PortraitHeight * 260f / 540f;
    private const string PhotoFormat = "1920 × 1080 PNG";
    private const string HallBackdropName = "四时工坊";
    private static readonly Rect PhotoBounds = new Rect(0f, 0f, PhotoWidth, PhotoHeight);
    private static readonly Color Night = new Color32(16, 28, 48, 255);
    private static readonly Color Ink = new Color32(28, 46, 68, 255);
    private static readonly Color IndigoEdge = new Color32(91, 113, 144, 255);
    private static readonly Color Ivory = new Color32(255, 245, 222, 255);
    private static readonly Color Mist = new Color32(194, 211, 233, 255);
    private static readonly Color Gold = new Color32(247, 208, 142, 255);
    private static readonly Color Brass = new Color32(170, 142, 103, 255);

    private List<Scenery> scenes;
    private readonly List<UnityEngine.UI.Image> sceneryEdges = new List<UnityEngine.UI.Image>();
    private RectTransform photoContent;
    private RectTransform portrait;
    private RawImage scenery;
    private RectTransform hallSigns;
    private Text placeName;
    private Text date;
    private Text saveStatus;
    private UnityEngine.UI.Button shootButton;
    private GameObject folderControl;
    private Action<int> onClose;
    private string lastPhoto;
    private int activeScene;
    private bool saving;

    internal static WorkshopWardrobePhotoMode Show(RectTransform parent, string outfitName, int backdrop,
        Action<RectTransform, Rect> drawPortrait, Action<int> onClose)
    {
        var entries = AvailableScenery();
        if (entries.Count == 0) return null;

        var page = WorkshopUi.Rect(parent, "Wardrobe Photo Mode", PhotoBounds);
        var mode = page.gameObject.AddComponent<WorkshopWardrobePhotoMode>();
        mode.scenes = entries;
        mode.onClose = onClose;
        mode.Draw(page, outfitName, backdrop, drawPortrait);
        return mode;
    }

    private static List<Scenery> AvailableScenery()
    {
        var winter = Resources.Load<Sprite>("WorkshopFinish/WardrobeWinterCourtyard");
        var entries = new List<Scenery>
        {
            new Scenery("雪夜庭院", winter != null ? winter.texture : null, new Vector2(0.32f, 0.91f)),
            new Scenery("春日石桥", WorkshopArt.SeasonBackground(WorkshopSeason.Spring), new Vector2(0.39f, 0.754f)),
            new Scenery("夏日缫丝", WorkshopArt.SeasonBackground(WorkshopSeason.Summer), new Vector2(0.51f, 0.745f)),
            new Scenery("秋日金岸", WorkshopArt.SeasonBackground(WorkshopSeason.Autumn), new Vector2(0.70f, 0.823f)),
            new Scenery("冬日雪村", WorkshopArt.SeasonBackground(WorkshopSeason.Winter), new Vector2(0.60f, 0.858f)),
            new Scenery(HallBackdropName, Resources.Load<Texture2D>(WorkshopHallSignage.TexturePath),
                new Vector2(0.66f, 0.815f), new Rect(0.38f, 0f, 0.62f, 1f))
        };
        entries.RemoveAll(entry => entry.Texture == null);
        return entries;
    }

    private void Draw(RectTransform page, string outfitName, int backdrop, Action<RectTransform, Rect> drawPortrait)
    {
        WorkshopUi.Fill(page, "Photo Mode Backdrop", PhotoBounds, Night);
        Label(page, "Photo Title", new Rect(80f, 38f, 335f, 76f), "拍照打卡", 59, Ivory);
        Label(page, "Photo Outfit", new Rect(415f, 57f, 850f, 48f), "当前造型 · " + outfitName, 29, Mist);
        WardrobeButton(page, "Leave Wardrobe Photo", new Rect(1590f, 47f, 250f, 55f), "← 返回换装", Close);
        DrawComposition(page, drawPortrait);
        DrawSceneryPicker(page);

        shootButton = WorkshopUi.Button(page, "Take Wardrobe Photo", new Rect(1374f, 766f, 466f, 79f),
            "拍照并保存", TakePhoto, Gold, size: 34);
        saveStatus = Label(page, "Photo Save Status", new Rect(82f, 873f, 919f, 58f), PhotoFormat, 25, Mist);
        var folderButton = WardrobeButton(page, "Open Wardrobe Photo Folder", new Rect(1066f, 873f, 268f, 58f),
            "打开照片文件夹", OpenFolder);
        folderControl = folderButton.transform.parent.gameObject;
        folderControl.SetActive(false);

        SelectScenery(Mathf.Clamp(backdrop, 0, scenes.Count - 1));
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(shootButton.gameObject);
    }

    private void DrawComposition(RectTransform page, Action<RectTransform, Rect> drawPortrait)
    {
        const float previewWidth = 1248f;
        const float previewScale = previewWidth / PhotoWidth;
        const float previewHeight = PhotoHeight * previewScale;
        PixelFill(page, "Photo Preview Frame", new Rect(80f, 137f, previewWidth + 8f, previewHeight + 8f), Brass);
        photoContent = WorkshopUi.Rect(page, "Wardrobe Photo Content", new Rect(84f, 141f, PhotoWidth, PhotoHeight));
        photoContent.localScale = Vector3.one * previewScale;
        scenery = PictureScenery(photoContent, "Photo Scenery", PhotoBounds);
        var hall = scenes.Find(entry => entry.Name == HallBackdropName);
        if (hall != null)
            hallSigns = WorkshopHallSignage.Draw(scenery.transform, PhotoBounds, hall.View);
        portrait = WorkshopUi.Rect(photoContent, "Photo Portrait", new Rect(0f, 0f, PortraitWidth, PortraitHeight));
        PixelFill(portrait, "Photo Foot Shadow", new Rect((PortraitWidth - 136f) * 0.5f, PortraitHeight - 21f, 136f, 20f),
            new Color32(16, 28, 40, 78));
        drawPortrait(portrait, new Rect(0f, 0f, PortraitWidth, PortraitHeight));

        var stamp = PixelFill(photoContent, "Photo Stamp", new Rect(56f, 872f, 388f, 150f), new Color32(16, 28, 48, 225));
        WorkshopUi.Fill(stamp.transform, "Stamp Rule", new Rect(26f, 26f, 3f, 98f), Gold);
        placeName = Label(stamp.transform, "Photo Place", new Rect(51f, 17f, 309f, 68f), "", 51, Ivory);
        date = Label(stamp.transform, "Photo Date", new Rect(52f, 85f, 299f, 39f), Today(), 29, Gold);
    }

    private void DrawSceneryPicker(RectTransform page)
    {
        var picker = WorkshopUi.Rect(page, "Wardrobe Photo Backgrounds", new Rect(1374f, 137f, 466f, 585f));
        Label(picker, "Backgrounds Title", new Rect(0f, 0f, 466f, 53f), "选择背景", 42, Ivory);
        for (int i = 0; i < scenes.Count; i++)
        {
            int index = i;
            Scenery entry = scenes[i];
            var edge = PixelFill(picker, "Photo Backdrop Border", new Rect(i % 2 * 240f, 71f + i / 2 * 172f, 226f, 159f), IndigoEdge);
            sceneryEdges.Add(edge);
            var button = WorkshopUi.Button(edge.transform, "Photo Backdrop " + i, new Rect(3f, 3f, 220f, 153f), "",
                () => SelectScenery(index), Ink);
            var preview = PictureScenery(button.transform, "Backdrop Thumbnail", new Rect(0f, 0f, 220f, 123.75f));
            preview.texture = entry.Texture;
            preview.uvRect = entry.View;
            if (entry.Name == HallBackdropName)
                WorkshopHallSignage.Draw(preview.transform, new Rect(0f, 0f, 220f, 123.75f), entry.View);
            Label(button.transform, "Backdrop Name", new Rect(4f, 123.75f, 212f, 29f), entry.Name, 24, Ivory, TextAnchor.MiddleCenter);
        }
    }

    private static RawImage PictureScenery(Transform parent, string name, Rect bounds)
    {
        var image = WorkshopUi.Rect(parent, name, bounds).gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        return image;
    }

    private void SelectScenery(int index)
    {
        if (saving) return;
        if (index != activeScene) saveStatus.text = PhotoFormat;
        activeScene = index;
        var entry = scenes[index];
        scenery.texture = entry.Texture;
        scenery.uvRect = entry.View;
        if (hallSigns != null) hallSigns.gameObject.SetActive(entry.Name == HallBackdropName);
        portrait.anchoredPosition = new Vector2(entry.Feet.x * PhotoWidth - PortraitWidth * 0.5f,
            PortraitHeight - entry.Feet.y * PhotoHeight);
        placeName.text = entry.Name;
        for (int i = 0; i < sceneryEdges.Count; i++)
            sceneryEdges[i].color = i == index ? Gold : IndigoEdge;
    }

    private static string Today() => DateTime.Now.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);

    private void TakePhoto()
    {
        if (saving) return;
        saving = true;
        shootButton.interactable = false;
        saveStatus.text = "正在保存照片…";
        date.text = Today();
        StartCoroutine(WorkshopWardrobePhotoCapture.Save(photoContent, path =>
        {
            lastPhoto = path;
            folderControl.SetActive(true);
            FinishCapture("照片已保存");
        }, error =>
        {
            Debug.LogWarning("[成衣展架] 无法保存照片。" + error);
            FinishCapture("保存照片失败，请重试");
        }));
    }

    private void FinishCapture(string message)
    {
        saveStatus.text = message;
        saving = false;
        shootButton.interactable = true;
    }

    private void OpenFolder()
    {
        string folder = Path.GetDirectoryName(lastPhoto);
        if (Directory.Exists(folder)) Application.OpenURL(new Uri(folder).AbsoluteUri);
        else saveStatus.text = "找不到照片文件夹";
    }

    internal void Close()
    {
        if (saving) return;
        gameObject.SetActive(false);
        Destroy(gameObject);
        onClose?.Invoke(activeScene);
    }
}
