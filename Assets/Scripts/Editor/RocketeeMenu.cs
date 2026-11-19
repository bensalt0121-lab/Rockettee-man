/*********************************************************************************************
 * COMPONENT OF: none (editor only: the Tools > Rocketee Man menu)
 * REQUIRED DEPENDENCIES: RocketeeGame, MainMenu, ShopStand, LaunchPad, PartPickup, YardPlayer,
 *                        GameSave, RocketeeBuildSettings, the Menu and Play scenes
 * DESCRIPTION: Helpers for building the game in the editor:
 *              - Build Everything: builds the whole game into the scenes in one click.
 *                Play scene: Rocketee Game (its settings show in the Inspector), the shop, the
 *                launch pad, the starter parts and the player, as real objects you can see
 *                and move. Menu scene: the Main Menu. Also fixes the sprite import and the
 *                build scene list, then saves both scenes. Safe to run again: it only adds
 *                what is missing, so things you moved stay where you put them.
 *              - Add Yard Objects To Scene: the same yard objects, only in the open scene.
 *              - Fix Pixel Art Import: makes all pictures in Resources/Sprites sharp again.
 *              - Reset Saved Progress: deletes money, parts and best height.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.1
 * VERSION 1.1: Added Build Everything.
 *********************************************************************************************/
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RocketeeMenu
{
    [MenuItem("Tools/Rocketee Man/Build Everything", false, -10)]
    static void BuildEverything()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Rocketee Man", "Stop playing first, then build.", "OK");
            return;
        }

        // Asks to save the scene you have open if it has changes
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        RocketeeBuildSettings.FixSceneList();
        RocketeeBuildSettings.FixBlurrySprites();

        // Menu scene
        Scene menu = EditorSceneManager.OpenScene(RocketeeBuildSettings.MenuPath, OpenSceneMode.Single);
        int menuAdded = 0;
        if (Object.FindAnyObjectByType<MainMenu>() == null)
        {
            new GameObject("Main Menu").AddComponent<MainMenu>();
            menuAdded++;
        }
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);

        // Play scene (left open so you can see the yard)
        Scene play = EditorSceneManager.OpenScene(RocketeeBuildSettings.PlayPath, OpenSceneMode.Single);

        // Simple flat ground (32 tiles wide) with your sand tiles
        UnityEngine.Tilemaps.Tilemap ground = YardBuilder.Ground();
        if (ground != null)
            Undo.RegisterCompleteObjectUndo(ground, "Simple Yard");
        int tiles = YardBuilder.MakeSimpleGround(ground);

        int playAdded = AddYardObjects(true);

        // The yard changed: move the shop, pad, cacti, parts and player to their spots
        YardGround.Span(out float left, out float right);
        if (tiles > 0)
            YardBuilder.PlaceYardObjects(left, right);

        ShopStand shop = Object.FindAnyObjectByType<ShopStand>();
        if (shop != null)
            shop.FixSize();
        LaunchPad pad = Object.FindAnyObjectByType<LaunchPad>();
        if (pad != null)
            pad.FixSize();

        YardBuilder.Decorate(left, right);

        EditorSceneManager.MarkSceneDirty(play);
        EditorSceneManager.SaveScene(play);

        Debug.Log("[Rocketee Man] Build Everything done: " + tiles + " ground tiles changed, " + playAdded + " objects in Play, " + menuAdded + " in Menu.");
        EditorUtility.DisplayDialog("Rocketee Man",
            "Everything is built!\n\n" +
            "Play scene: simple flat yard with your sand tiles (" + tiles + " tiles changed), bigger cacti and " +
            "decorations, Rocketee Game, Shop, Launch Pad, starter parts and the player (added " + playAdded + ").\n" +
            "Menu scene: Main Menu (added " + menuAdded + ").\n\n" +
            "You can move things in the Scene view and save. To start from the menu, open the Menu scene and press Play.",
            "OK");
    }

    [MenuItem("Tools/Rocketee Man/Add Yard Objects To Scene", false, 20)]
    static void AddYardObjectsMenu()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[Rocketee Man] Stop playing first.");
            return;
        }

        int added = AddYardObjects(false);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Rocketee Man] Added " + added + " yard objects. Move them where you like, then save the scene.");
    }

    // Adds whatever is missing in the open scene; returns how many objects were added
    static int AddYardObjects(bool withGameAndPlayer)
    {
        int added = 0;
        YardGround.Span(out float left, out float right);

        if (withGameAndPlayer && Object.FindAnyObjectByType<RocketeeGame>() == null)
        {
            Undo.RegisterCreatedObjectUndo(new GameObject("Rocketee Game", typeof(RocketeeGame)), "Add Rocketee Game");
            added++;
        }

        if (Object.FindAnyObjectByType<ShopStand>() == null)
        {
            Undo.RegisterCreatedObjectUndo(ShopStand.Create(Mathf.Clamp(YardBuilder.ShopX, left + 1.5f, right - 1.5f)).gameObject, "Add Shop");
            added++;
        }

        if (Object.FindAnyObjectByType<LaunchPad>() == null)
        {
            Undo.RegisterCreatedObjectUndo(LaunchPad.Create(Mathf.Clamp(YardBuilder.PadX, left + 2f, right - 2f)).gameObject, "Add Launch Pad");
            added++;
        }

        if (Object.FindAnyObjectByType<PartPickup>() == null)
        {
            RocketeeGame.MakeStarterParts(left, right);
            foreach (PartPickup pickup in Object.FindObjectsByType<PartPickup>())
            {
                Undo.RegisterCreatedObjectUndo(pickup.gameObject, "Add Starter Parts");
                added++;
            }
        }

        if (withGameAndPlayer && Object.FindAnyObjectByType<YardPlayer>() == null)
        {
            Undo.RegisterCreatedObjectUndo(YardPlayer.Create(Mathf.Clamp(YardBuilder.PlayerX, left + 1f, right - 1f)).gameObject, "Add Player");
            added++;
        }

        return added;
    }

    [MenuItem("Tools/Rocketee Man/Fix Pixel Art Import", false, 40)]
    static void FixPixelArt()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { RocketeeSpriteImport.SpriteFolder.TrimEnd('/') });
        foreach (string guid in guids)
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        Debug.Log("[Rocketee Man] Re-imported " + guids.Length + " sprites as sharp pixel art.");
    }

    [MenuItem("Tools/Rocketee Man/Reset Saved Progress", false, 41)]
    static void ResetProgress()
    {
        GameSave.Delete();
        Debug.Log("[Rocketee Man] Saved progress deleted.");
    }
}
