/*********************************************************************************************
 * COMPONENT OF: Main Menu (made automatically by SceneBoot in the Menu scene)
 * REQUIRED DEPENDENCIES: UIKit, Settings, GameSave, MusicPlayer, the Play scene in Build Profiles
 * DESCRIPTION: The start screen. PLAY opens the Play scene. CHARACTER lets you pick which
 *              character you play as and its color (with a walking preview). SETTINGS has
 *              music and sound volume, fullscreen and a reset-progress button. QUIT closes
 *              the game (not shown in the web version). Enter = Play, Esc = back.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    static readonly Color SkyColor = new Color(0.55f, 0.8f, 0.98f);

    private RectTransform root;
    private GameObject mainPanel;
    private GameObject characterPanel;
    private GameObject settingsPanel;

    private Image preview;
    private Text characterName;
    private Text musicText;
    private Text soundText;
    private Button fullscreenButton;
    private Button resetButton;
    private float resetConfirmUntil;
    private Sprite walk1;
    private Sprite walk2;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
        }

        MusicPlayer.Play();
        root = UIKit.MakeCanvas(gameObject, 10);

        BuildDecorations();
        BuildMainPanel();
        BuildCharacterPanel();
        BuildSettingsPanel();
        Show(mainPanel);
    }

    void Update()
    {
        // Walking preview of the chosen character
        if (preview != null && characterPanel.activeSelf)
            preview.sprite = Mathf.FloorToInt(Time.unscaledTime / 0.2f) % 2 == 0 ? walk1 : walk2;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (mainPanel.activeSelf && keyboard.enterKey.wasPressedThisFrame)
            Play();
        else if (!mainPanel.activeSelf && keyboard.escapeKey.wasPressedThisFrame)
            Show(mainPanel);
    }

    void Play()
    {
        Sfx.Play("click");
        SceneManager.LoadScene(Settings.PlayScene);
    }

    void Show(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        characterPanel.SetActive(panel == characterPanel);
        settingsPanel.SetActive(panel == settingsPanel);
        RefreshCharacter();
        RefreshSettings();
    }

    // ---------- Main ----------

    void BuildDecorations()
    {
        Image moon = UIKit.Image(root, "Moon", Color.white);
        moon.sprite = SpriteLib.Get("moon", new Color(0.8f, 0.8f, 0.78f), 256, 256);
        moon.preserveAspect = true;
        UIKit.Place(moon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-260f, -260f), new Vector2(-40f, -40f));

        string[] clouds = { "cloud_1", "cloud_2", "cloud_3" };
        Vector2[] spots = { new Vector2(0.12f, 0.82f), new Vector2(0.4f, 0.9f), new Vector2(0.75f, 0.55f), new Vector2(0.2f, 0.35f) };
        for (int i = 0; i < spots.Length; i++)
        {
            Image cloud = UIKit.Image(root, "Cloud", new Color(1f, 1f, 1f, 0.85f));
            cloud.sprite = SpriteLib.Get(clouds[i % clouds.Length], Color.white, 64, 32);
            cloud.preserveAspect = true;
            UIKit.Place(cloud.rectTransform, spots[i], spots[i], new Vector2(-90f, -40f), new Vector2(90f, 40f));
        }

        Image rocket = UIKit.Image(root, "Cola Rocket", Color.white);
        rocket.sprite = SpriteLib.Get("cola", new Color(0.85f, 0.1f, 0.1f));
        rocket.preserveAspect = true;
        rocket.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
        UIKit.Place(rocket.rectTransform, new Vector2(0.15f, 0.55f), new Vector2(0.15f, 0.55f), new Vector2(-48f, -48f), new Vector2(48f, 48f));
    }

    void BuildMainPanel()
    {
        mainPanel = new GameObject("Main", typeof(RectTransform));
        RectTransform panel = (RectTransform)mainPanel.transform;
        panel.SetParent(root, false);
        UIKit.Place(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Text title = UIKit.Label(panel, "ROCKETEE MAN", 84, TextAnchor.MiddleCenter, UIKit.Gold);
        UIKit.Place(title.rectTransform, new Vector2(0f, 0.7f), new Vector2(1f, 0.88f), Vector2.zero, Vector2.zero);
        Text subtitle = UIKit.Label(panel, "Build a cola rocket. Dodge the meteors. Fly to the Moon!", 26, TextAnchor.MiddleCenter, Color.white);
        UIKit.Place(subtitle.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.7f), Vector2.zero, Vector2.zero);

        float y = -40f;
        MenuButton(panel, "PLAY", ref y, UIKit.ButtonColor, Play);
        MenuButton(panel, "CHARACTER", ref y, new Color(0.3f, 0.55f, 0.85f), () => Show(characterPanel));
        MenuButton(panel, "SETTINGS", ref y, new Color(0.45f, 0.45f, 0.5f), () => Show(settingsPanel));
#if !UNITY_WEBGL
        MenuButton(panel, "QUIT", ref y, new Color(0.7f, 0.3f, 0.3f), Application.Quit);
#endif

        GameSave save = GameSave.Load();
        string progress = save.flights == 0
            ? "Enter: play"
            : "Best flight: " + Mathf.RoundToInt(save.bestHeight) + " m    Flights: " + save.flights + (save.reachedMoon ? "    You reached the Moon!" : "");
        Text info = UIKit.Label(panel, progress, 20, TextAnchor.MiddleCenter, Color.white);
        UIKit.Place(info.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 20f), new Vector2(0f, 60f));
    }

    void MenuButton(RectTransform panel, string label, ref float y, Color color, System.Action onClick)
    {
        Button button = UIKit.Button(panel, label, color, onClick, 26);
        UIKit.Place((RectTransform)button.transform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(-150f, y - 50f), new Vector2(150f, y));
        y -= 62f;
    }

    // ---------- Character ----------

    void BuildCharacterPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Character", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -280f), new Vector2(330f, 280f));
        characterPanel = panel.gameObject;

        Text title = UIKit.Label(panel, "CHARACTER", 36, TextAnchor.UpperCenter, UIKit.Gold);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -62f), new Vector2(0f, -12f));

        preview = UIKit.Image(panel, "Preview", Color.white);
        preview.preserveAspect = true;
        UIKit.Place(preview.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-80f, -310f), new Vector2(80f, -70f));

        Button previous = UIKit.Button(panel, "<", UIKit.ButtonColor, () => { Settings.Character--; RefreshCharacter(); }, 32);
        UIKit.Place((RectTransform)previous.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-200f, -220f), new Vector2(-140f, -160f));
        Button next = UIKit.Button(panel, ">", UIKit.ButtonColor, () => { Settings.Character++; RefreshCharacter(); }, 32);
        UIKit.Place((RectTransform)next.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(140f, -220f), new Vector2(200f, -160f));

        characterName = UIKit.Label(panel, "", 28, TextAnchor.MiddleCenter, Color.white);
        UIKit.Place(characterName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -360f), new Vector2(0f, -318f));

        Text colorTitle = UIKit.Label(panel, "Color", 20, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
        UIKit.Place(colorTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -394f), new Vector2(0f, -366f));

        float width = 64f;
        float start = -(Settings.Colors.Length * width) * 0.5f;
        for (int i = 0; i < Settings.Colors.Length; i++)
        {
            int index = i;
            Button swatch = UIKit.Button(panel, "", Settings.Colors[i], () => { Settings.ColorIndex = index; RefreshCharacter(); });
            float x = start + i * width;
            UIKit.Place((RectTransform)swatch.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x + 6f, -450f), new Vector2(x + width - 6f, -400f));
        }

        Button back = UIKit.Button(panel, "Back (Esc)", UIKit.ButtonColor, () => Show(mainPanel));
        UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-110f, 18f), new Vector2(110f, 64f));
    }

    void RefreshCharacter()
    {
        if (preview == null)
            return;

        walk1 = Settings.PlayerSprite("walk1");
        walk2 = Settings.PlayerSprite("walk2");
        preview.sprite = walk1;
        preview.color = Settings.PlayerColor;
        characterName.text = Settings.CharacterName(Settings.Character) + "  (" + Settings.ColorNames[Settings.ColorIndex] + ")";
    }

    // ---------- Settings ----------

    void BuildSettingsPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Settings", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300f, -250f), new Vector2(300f, 250f));
        settingsPanel = panel.gameObject;

        Text title = UIKit.Label(panel, "SETTINGS", 36, TextAnchor.UpperCenter, UIKit.Gold);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -62f), new Vector2(0f, -12f));

        musicText = VolumeRow(panel, -90f, () => Settings.MusicLevel--, () => Settings.MusicLevel++);
        soundText = VolumeRow(panel, -150f, () => { Settings.SoundLevel--; Sfx.Play("coin"); }, () => { Settings.SoundLevel++; Sfx.Play("coin"); });

#if !UNITY_WEBGL
        fullscreenButton = UIKit.Button(panel, "", new Color(0.45f, 0.45f, 0.5f), () => { Screen.fullScreen = !Screen.fullScreen; Invoke(nameof(RefreshSettings), 0.1f); }, 22);
        UIKit.Place((RectTransform)fullscreenButton.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-170f, -264f), new Vector2(170f, -218f));
#endif

        resetButton = UIKit.Button(panel, "", new Color(0.7f, 0.3f, 0.3f), ResetProgress, 22);
        UIKit.Place((RectTransform)resetButton.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-170f, -330f), new Vector2(170f, -284f));

        Button back = UIKit.Button(panel, "Back (Esc)", UIKit.ButtonColor, () => Show(mainPanel));
        UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-110f, 18f), new Vector2(110f, 64f));
    }

    Text VolumeRow(RectTransform panel, float top, System.Action down, System.Action up)
    {
        Button minus = UIKit.Button(panel, "-", UIKit.ButtonColor, () => { down(); RefreshSettings(); }, 28);
        UIKit.Place((RectTransform)minus.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-170f, top - 48f), new Vector2(-122f, top));
        Button plus = UIKit.Button(panel, "+", UIKit.ButtonColor, () => { up(); RefreshSettings(); }, 28);
        UIKit.Place((RectTransform)plus.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(122f, top - 48f), new Vector2(170f, top));

        Text text = UIKit.Label(panel, "", 24, TextAnchor.MiddleCenter, Color.white);
        UIKit.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-118f, top - 48f), new Vector2(118f, top));
        return text;
    }

    void RefreshSettings()
    {
        if (musicText == null)
            return;

        musicText.text = "Music: " + Settings.MusicLevel + " / 10";
        soundText.text = "Sounds: " + Settings.SoundLevel + " / 10";
        if (fullscreenButton != null)
            UIKit.SetButtonText(fullscreenButton, "Fullscreen: " + (Screen.fullScreen ? "ON" : "OFF"));
        UIKit.SetButtonText(resetButton, Time.unscaledTime < resetConfirmUntil ? "Click again to delete everything!" : "Reset progress");
    }

    // Needs two clicks so it is not pressed by accident
    void ResetProgress()
    {
        if (Time.unscaledTime < resetConfirmUntil)
        {
            GameSave.Delete();
            resetConfirmUntil = 0f;
            UIKit.SetButtonText(resetButton, "Progress deleted");
            Sfx.Play("remove");
            return;
        }

        resetConfirmUntil = Time.unscaledTime + 3f;
        RefreshSettings();
        Invoke(nameof(RefreshSettings), 3.05f);
    }
}
