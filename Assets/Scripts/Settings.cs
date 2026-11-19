/*********************************************************************************************
 * COMPONENT OF: none (settings shared by the Menu and Play scenes, kept in PlayerPrefs)
 * REQUIRED DEPENDENCIES: characters.txt in Assets/Resources (optional, for character names)
 * DESCRIPTION: The player's choices from the menu: music and sound volume, fullscreen, which
 *              character to play as and its color. Also the names of the two scenes.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public static class Settings
{
    public const string MenuScene = "Menu";
    public const string PlayScene = "Play";

    // Sprite name start for each character: player_idle, player2_idle, ...
    public static readonly string[] CharacterPrefixes = { "player", "player2", "player3", "player4" };

    public static readonly string[] ColorNames = { "Normal", "Red", "Blue", "Green", "Gold", "Purple" };
    public static readonly Color[] Colors =
    {
        Color.white,
        new Color(1f, 0.6f, 0.6f),
        new Color(0.6f, 0.75f, 1f),
        new Color(0.6f, 1f, 0.65f),
        new Color(1f, 0.9f, 0.5f),
        new Color(0.85f, 0.65f, 1f),
    };

    private const string MusicKey = "RocketeeMan.MusicVolume";
    private const string SoundKey = "RocketeeMan.SoundVolume";
    private const string CharacterKey = "RocketeeMan.Character";
    private const string ColorKey = "RocketeeMan.Color";

    // 0 to 10
    public static int MusicLevel
    {
        get => PlayerPrefs.GetInt(MusicKey, 7);
        set { PlayerPrefs.SetInt(MusicKey, Mathf.Clamp(value, 0, 10)); PlayerPrefs.Save(); }
    }

    // 0 to 10
    public static int SoundLevel
    {
        get => PlayerPrefs.GetInt(SoundKey, 8);
        set { PlayerPrefs.SetInt(SoundKey, Mathf.Clamp(value, 0, 10)); PlayerPrefs.Save(); }
    }

    public static float MusicVolume => MusicLevel / 10f;
    public static float SoundVolume => SoundLevel / 10f;

    public static int Character
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(CharacterKey, 0), 0, CharacterPrefixes.Length - 1);
        set { PlayerPrefs.SetInt(CharacterKey, (value % CharacterPrefixes.Length + CharacterPrefixes.Length) % CharacterPrefixes.Length); PlayerPrefs.Save(); }
    }

    public static int ColorIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(ColorKey, 0), 0, Colors.Length - 1);
        set { PlayerPrefs.SetInt(ColorKey, Mathf.Clamp(value, 0, Colors.Length - 1)); PlayerPrefs.Save(); }
    }

    public static Color PlayerColor => Colors[ColorIndex];

    // One sprite of the chosen character, for example PlayerSprite("idle").
    // Uses the first character if the chosen one has no picture for this pose.
    public static Sprite PlayerSprite(string pose)
    {
        string name = CharacterPrefixes[Character] + "_" + pose;
        if (Character != 0 && Resources.Load<Texture2D>("Sprites/" + name) == null)
            name = CharacterPrefixes[0] + "_" + pose;
        return SpriteLib.Get(name, new Color(0.2f, 0.5f, 0.9f), 32, 48);
    }

    // Display name of a character, from Assets/Resources/characters.txt
    public static string CharacterName(int index)
    {
        TextAsset names = Resources.Load<TextAsset>("characters");
        if (names != null)
        {
            string[] lines = names.text.Replace("\r", "").Split('\n');
            if (index < lines.Length && lines[index].Trim().Length > 0)
                return lines[index].Trim();
        }
        return index == 0 ? "Rocketee Man" : "Character " + (index + 1);
    }
}
