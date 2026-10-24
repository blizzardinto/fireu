using UnityEditor;
using UnityEngine;

namespace GalaxyStrike.Editor
{
    public class GalaxyStrikeArtImporter : AssetPostprocessor
    {
        private const string ArtPath = "Assets/GalaxyStrike/Resources/GalaxyStrikeArt/";

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(ArtPath))
            {
                var importer = (TextureImporter)assetImporter;
                Configure(importer);
                if (assetPath.Contains("/Sprites/"))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100;
                }
            }
        }

        private static void Configure(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }

        [InitializeOnLoadMethod]
        private static void EnsureImports()
        {
            EditorApplication.delayCall += () =>
            {
                foreach (var name in new[] { "CartoonEntities", "CartoonEffects" })
                {
                    var importer = AssetImporter.GetAtPath(ArtPath + name + ".png") as TextureImporter;
                    if (importer == null) continue;
                    if (importer.npotScale == TextureImporterNPOTScale.None && !importer.mipmapEnabled
                        && importer.alphaIsTransparency && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                    Configure(importer);
                    importer.SaveAndReimport();
                }
            };
        }
    }
}
