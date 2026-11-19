/*********************************************************************************************
 * COMPONENT OF: none (helper used by the other scripts)
 * REQUIRED DEPENDENCIES: sounds in Assets/Resources/Audio (any missing sound is just skipped)
 * DESCRIPTION: Plays a short sound by name, for example Sfx.Play("jump").
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;

public static class Sfx
{
    private static AudioSource source;
    private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    public static void Play(string name, float volume = 1f)
    {
        AudioClip clip = Get(name);
        if (clip == null)
            return;

        if (source == null)
        {
            GameObject go = new GameObject("Sound Effects");
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            // Button clicks still play in the pause menu
            source.ignoreListenerPause = true;
        }

        source.PlayOneShot(clip, volume * Settings.SoundVolume);
    }

    public static AudioClip Get(string name)
    {
        if (clips.TryGetValue(name, out AudioClip clip) && clip != null)
            return clip;

        clip = Resources.Load<AudioClip>("Audio/" + name);
        clips[name] = clip;
        return clip;
    }
}
