using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MiniGameCompletionAssetValidator
{
    public static void Validate()
    {
        var errors = new List<string>();
        foreach (MiniGameCompletionId id in Enum.GetValues(typeof(MiniGameCompletionId)))
        {
            string path = MiniGameCompletionAnimation.ResourceRoot + id + "/";
            var manifest = Resources.Load<TextAsset>(path + "Clip");
            var clip = manifest != null
                ? JsonUtility.FromJson<MiniGameCompletionAnimation.ClipInfo>(manifest.text)
                : null;
            if (manifest != null) Resources.UnloadAsset(manifest);
            if (clip == null || !clip.IsValid)
            {
                errors.Add("Missing or invalid clip data: " + path);
                continue;
            }

            for (int i = 0; i < clip.SheetCount; i++)
                ValidateSheet(path + i.ToString("00"), clip, errors);

            var audio = Resources.Load<AudioClip>(path + "Audio");
            if (audio == null)
                errors.Add("Missing audio: " + path);
            else
            {
                if (Mathf.Abs(audio.length - clip.Duration) > 0.25f)
                    errors.Add($"Audio duration differs from the animation: {path} ({audio.length:F2}s vs {clip.Duration:F2}s)");
                Resources.UnloadAsset(audio);
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));

        Debug.Log("[MiniGameCompletionAssetValidator] PASS: all result animation atlases and audio.");
    }

    private static void ValidateSheet(
        string path, MiniGameCompletionAnimation.ClipInfo clip, List<string> errors)
    {
        var texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            errors.Add("Missing imported atlas: " + path);
            return;
        }

        if (texture.width != clip.SheetWidth || texture.height != clip.SheetHeight)
            errors.Add($"Unexpected atlas size: {path} ({texture.width}x{texture.height})");
        Resources.UnloadAsset(texture);

        var importer = AssetImporter.GetAtPath("Assets/Resources/" + path + ".png") as TextureImporter;
        if (importer == null || !importer.DoesSourceTextureHaveAlpha() ||
            importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
            importer.textureCompression != TextureImporterCompression.Uncompressed)
            errors.Add("Expected transparent, uncompressed point-filtered atlas: " + path);
    }
}
