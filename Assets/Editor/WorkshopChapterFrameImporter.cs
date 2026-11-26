using UnityEditor;
using UnityEngine;

/// <summary>Keep the chapter illustrations crisp and preserve the transparent frame silhouette.</summary>
public sealed class WorkshopChapterFrameImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/WorkshopChapterFrames/")) return;
        var texture = (TextureImporter)assetImporter;
        texture.textureType = TextureImporterType.Sprite;
        texture.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings();
        texture.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        texture.SetTextureSettings(settings);
        texture.alphaIsTransparency = true;
        texture.mipmapEnabled = false;
        texture.npotScale = TextureImporterNPOTScale.None;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.maxTextureSize = 2048;
        texture.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
