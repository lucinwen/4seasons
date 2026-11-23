using UnityEditor;
using UnityEngine;

/// <summary>让所有结算动画图集使用无损 RGBA、点采样和 4096 最大尺寸。</summary>
public sealed class MiniGameCompletionAssetImporter : AssetPostprocessor
{
    private const string TextureFolder = "/Resources/MiniGameCompletion/";

    private void OnPreprocessTexture()
    {
        if (assetPath.Replace('\\', '/').IndexOf(TextureFolder, System.StringComparison.Ordinal) < 0)
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.npotScale = TextureImporterNPOTScale.None;
    }

    private void OnPreprocessAudio()
    {
        if (assetPath.Replace('\\', '/').IndexOf(TextureFolder, System.StringComparison.Ordinal) < 0)
        {
            return;
        }

        var importer = (AudioImporter)assetImporter;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.8f;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
    }
}
