/*********************************************************************************************
 * COMPONENT OF: none (editor only, runs every time Unity loads the scripts)
 * REQUIRED DEPENDENCIES: Assets/Scenes/Menu.unity and Assets/Scenes/Play.unity
 * DESCRIPTION: Makes sure File > Build Profiles has the scenes in the right order:
 *              0 = Menu (the game starts here), 1 = Play. Needed so the Play button in
 *              the menu can open the Play scene. Also re-imports any picture in
 *              Assets/Resources/Sprites that is not sharp pixel art yet.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class RocketeeBuildSettings
{
    public const string MenuPath = "Assets/Scenes/Menu.unity";
    public const string PlayPath = "Assets/Scenes/Play.unity";

    static RocketeeBuildSettings()
    {
        // Waits until Unity has finished loading
        EditorApplication.delayCall += FixSceneList;
        EditorApplication.delayCall += FixBlurrySprites;
    }

    // Pictures imported before the pixel art importer existed can be blurry; this fixes them once
    public static void FixBlurrySprites()
    {
        string folder = RocketeeSpriteImport.SpriteFolder.TrimEnd('/');
        if (!AssetDatabase.IsValidFolder(folder))
            return;

        int fixedCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            bool sharp = importer.textureType == TextureImporterType.Sprite
                && importer.filterMode == UnityEngine.FilterMode.Point
                && importer.spritePixelsPerUnit == 32f
                && importer.textureCompression == TextureImporterCompression.Uncompressed;
            if (sharp)
                continue;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            fixedCount++;
        }

        if (fixedCount > 0)
            UnityEngine.Debug.Log("[Rocketee Man] Made " + fixedCount + " sprites sharp pixel art.");
    }

    public static void FixSceneList()
    {
        if (!File.Exists(MenuPath) || !File.Exists(PlayPath))
            return;

        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        if (current.Length >= 2 && current[0].path == MenuPath && current[1].path == PlayPath && current[0].enabled && current[1].enabled)
            return;

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuPath, true),
            new EditorBuildSettingsScene(PlayPath, true),
        };

        // Keeps any other scenes you added, after these two
        foreach (EditorBuildSettingsScene scene in current)
        {
            if (scene.path != MenuPath && scene.path != PlayPath && File.Exists(scene.path))
                scenes.Add(scene);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        UnityEngine.Debug.Log("[Rocketee Man] Build scenes set: 0 = Menu, 1 = Play.");
    }
}
