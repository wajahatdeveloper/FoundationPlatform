#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// Writes a rendered icon to the owning package's <c>Editor/Icons</c> folder and applies the
    /// import settings a 64px editor icon needs (uncompressed, no mips, straight alpha).
    /// </summary>
    internal static class DesignerIconWriter
    {
        internal static void WriteFile(DesignerIconEntry entry)
        {
            var texture = DesignerIconRenderer.Render(entry, DesignerIconRenderer.DefaultSize);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            string absolutePath = Path.GetFullPath(entry.IconAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, png);
        }

        /// <summary>Runs after the import that <see cref="WriteFile"/>'s output triggers; the importer only exists once the asset is in the database.</summary>
        internal static void ApplyImportSettings(string assetPath)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.GUI;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = DesignerIconRenderer.DefaultSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
#endif
