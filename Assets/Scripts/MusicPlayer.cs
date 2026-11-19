/*********************************************************************************************
 * COMPONENT OF: Rocketee Game and Main Menu (added automatically)
 * REQUIRED DEPENDENCIES: Assets/Resources/Audio/music (one long chill song), Settings
 * DESCRIPTION: Plays the background song on repeat. Only one song plays, even when you go
 *              from the menu to the game, so it never restarts. The volume comes from the
 *              Music setting in the menu.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    // How loud the song is at full Music volume
    public float maxVolume = 0.45f;

    private static MusicPlayer instance;
    private AudioSource song;

    // Starts the song if it is not already playing
    public static void Play()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("Music");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MusicPlayer>();
    }

    void Awake()
    {
        AudioClip clip = Sfx.Get("music");
        if (clip == null)
            return;

        song = gameObject.AddComponent<AudioSource>();
        song.clip = clip;
        song.loop = true;
        // Keeps playing in the pause menu
        song.ignoreListenerPause = true;
        song.volume = maxVolume * Settings.MusicVolume;
        song.Play();
    }

    void Update()
    {
        if (song != null)
            song.volume = maxVolume * Settings.MusicVolume;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
