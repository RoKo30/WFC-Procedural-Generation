using UnityEditor;
using UnityEngine;

namespace ProceduralGen.Wfc.Editor
{
    /// <summary>
    /// WFC needs the exact pixel colours of the input: readable, unfiltered, uncompressed and not rescaled.
    /// Textures inside any "WFC/Samples" folder get these settings automatically; others can be fixed from the inspector.
    /// </summary>
    public static class WfcTextureImport
    {
        public static bool NeedsFix(Texture2D texture)
        {
            var importer = GetImporter(texture);
            if (importer == null) return !texture.isReadable;
            return !importer.isReadable
                || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.npotScale != TextureImporterNPOTScale.None
                || importer.mipmapEnabled;
        }

        public static void Fix(Texture2D texture)
        {
            var importer = GetImporter(texture);
            if (importer == null) return;
            Apply(importer);
            importer.SaveAndReimport();
        }

        public static void Apply(TextureImporter importer)
        {
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
        }

        private static TextureImporter GetImporter(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            return string.IsNullOrEmpty(path) ? null : AssetImporter.GetAtPath(path) as TextureImporter;
        }
    }

    internal sealed class WfcSampleImportProcessor : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (assetPath.Replace('\\', '/').Contains("/WFC/Samples/"))
                WfcTextureImport.Apply((TextureImporter)assetImporter);
        }
    }
}
