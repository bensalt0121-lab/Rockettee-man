/*********************************************************************************************
 * COMPONENT OF: none (starts automatically when the game runs)
 * REQUIRED DEPENDENCIES: MainMenu, RocketeeGame, Settings (scene names)
 * DESCRIPTION: Every time a scene is loaded it adds what that scene needs: the Menu scene
 *              gets the main menu, the Play scene gets the game. So you never have to set
 *              anything up by hand, just press Play.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Listen()
    {
        // Removed first so it is never added twice
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        if (scene.name == Settings.MenuScene)
        {
            if (Object.FindAnyObjectByType<MainMenu>() == null)
                new GameObject("Main Menu").AddComponent<MainMenu>();
        }
        else if (Object.FindAnyObjectByType<RocketeeGame>() == null)
        {
            new GameObject("Rocketee Game").AddComponent<RocketeeGame>();
        }
    }
}
