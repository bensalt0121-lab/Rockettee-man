/*********************************************************************************************
 * COMPONENT OF: none (editor only, runs when Unity imports a picture)
 * REQUIRED DEPENDENCIES: pictures in Assets/Resources/Sprites
 * DESCRIPTION: Every picture put in Assets/Resources/Sprites is set up as sharp pixel art
 *              automatically: Sprite, 32 Pixels Per Unit, Point filter (no blur), no
 *              compression. Same settings as your sand tiles.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEditor;
using UnityEngine;

public class RocketeeSpriteImport : AssetPostprocessor
{
    public const string SpriteFolder = "Assets/Resources/Sprites/";

    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').StartsWith(SpriteFolder))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
}
