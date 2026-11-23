using System;
using System.Collections;
using System.IO;
using System.Security;
using UnityEngine;

internal static class WorkshopWardrobePhotoCapture
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const float WorldUnitsPerPixel = 0.01f;
    private const string AlbumName = "打卡相册";
    private static readonly Color ClearColor = new Color32(16, 28, 48, 255);

    internal static IEnumerator Save(RectTransform photoContent, Action<string> onSaved, Action<string> onError)
    {
        yield return null;

        string path;
        try
        {
            path = SavePng(CapturePng(photoContent));
        }
        catch (Exception exception)
        {
            onError?.Invoke(exception.Message);
            yield break;
        }

        onSaved?.Invoke(path);
    }

    private static byte[] CapturePng(RectTransform photoContent)
    {
        if (photoContent == null || !photoContent.gameObject.activeInHierarchy)
            throw new InvalidOperationException("拍照画面已关闭。");

        Vector2 size = photoContent.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            throw new InvalidOperationException("拍照画面尚未就绪。");

        GameObject rig = null;
        Camera camera = null;
        RenderTexture target = null;
        Texture2D image = null;
        RenderTexture previousTarget = RenderTexture.active;

        try
        {
            // Render just the photo composition behind the live scene, using a separate camera.
            rig = new GameObject("Wardrobe Photo Render");
            rig.transform.position = Vector3.back * 4096f;
            var ui = new GameObject("Photo Canvas", typeof(RectTransform), typeof(Canvas));
            ui.layer = LayerMask.NameToLayer("UI");
            var canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)ui.transform;
            canvasRect.SetParent(rig.transform, false);
            canvasRect.sizeDelta = size;
            canvasRect.localScale = Vector3.one * WorldUnitsPerPixel;

            var composition = UnityEngine.Object.Instantiate(photoContent, canvasRect, false);
            composition.anchorMin = composition.anchorMax = composition.pivot = Vector2.one * 0.5f;
            composition.sizeDelta = size;
            composition.localPosition = Vector3.zero;
            composition.localRotation = Quaternion.identity;
            composition.localScale = Vector3.one;
            foreach (Transform part in composition.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = ui.layer;

            var cameraObject = new GameObject("Photo Camera", typeof(Camera));
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.transform.localPosition = Vector3.back * 6f;
            camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ClearColor;
            camera.cullingMask = 1 << ui.layer;
            camera.orthographic = true;
            camera.aspect = (float)Width / Height;
            camera.orthographicSize = Mathf.Max(size.y, size.x / camera.aspect) * WorldUnitsPerPixel * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 12f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            canvas.worldCamera = camera;

            target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture.active = target;
            image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0, false);
            return image.EncodeToPNG();
        }
        finally
        {
            RenderTexture.active = previousTarget;
            if (camera != null) camera.targetTexture = null;
            if (rig != null)
            {
                rig.SetActive(false);
                UnityEngine.Object.Destroy(rig);
            }
            if (target != null) RenderTexture.ReleaseTemporary(target);
            if (image != null) UnityEngine.Object.Destroy(image);
        }
    }

    private static string SavePng(byte[] image)
    {
        string fileName = $"{WorkshopTitle.GameName}_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.png";
        try
        {
            string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (!string.IsNullOrWhiteSpace(pictures))
                return WritePng(Path.Combine(pictures, WorkshopTitle.GameName, AlbumName), fileName, image);
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            // The platform's Pictures folder may be inaccessible. Use the game's writable folder.
        }

        return WritePng(Path.Combine(Application.persistentDataPath, AlbumName), fileName, image);
    }

    private static string WritePng(string folder, string fileName, byte[] image)
    {
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, fileName);
        string pendingPath = path + ".tmp";
        try
        {
            using (var stream = new FileStream(pendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(image, 0, image.Length);
            File.Move(pendingPath, path);
            return path;
        }
        finally
        {
            if (File.Exists(pendingPath)) File.Delete(pendingPath);
        }
    }

    private static bool IsFileError(Exception exception) =>
        exception is IOException || exception is UnauthorizedAccessException ||
        exception is SecurityException || exception is NotSupportedException;
}
