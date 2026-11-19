/*********************************************************************************************
 * COMPONENT OF: Game UI (made by the game in the Play scene)
 * REQUIRED DEPENDENCIES: RocketeeGame, UIKit, Settings, uGUI, Input System package
 * DESCRIPTION: Everything on the screen in the Play scene: money and best height, messages,
 *              the "Press E" prompt, the shop (click Buy), the build bar with your parts,
 *              the height and cola meters while flying, the results and Moon screens, and
 *              the pause menu (Esc) with volume and a button back to the main menu.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    static readonly Color RowColor = new Color(1f, 1f, 1f, 0.07f);
    static readonly Color SelectedColor = new Color(1f, 0.85f, 0.3f);

    private RocketeeGame game;
    private RectTransform root;

    private Text moneyText;
    private Text bestText;
    private Text messageText;
    private Text promptText;
    private Text hintText;
    private float messageUntil;
    private int promptFrame = -10;

    private GameObject flightHud;
    private Text heightText;
    private Image fuelFill;
    private RectTransform heightMarker;
    private RectTransform bestMarker;

    private GameObject buildPanel;
    private RectTransform buildRow;
    private Text buildAdvice;
    private readonly List<(PartType type, Image background, Text count)> buildButtons = new List<(PartType, Image, Text)>();

    private GameObject shopPanel;
    private RectTransform shopList;
    private Text shopMoney;

    private GameObject resultsPanel;
    private Text resultsText;
    private GameObject winPanel;
    private Text winText;

    private GameObject pausePanel;
    private Text musicText;
    private Text soundText;

    public static GameUI Create(RocketeeGame game)
    {
        GameObject go = new GameObject("Game UI");
        GameUI ui = go.AddComponent<GameUI>();
        ui.game = game;
        ui.Build();
        return ui;
    }

    // ---------- Called by the game ----------

    public void Say(string message, float seconds = 3f)
    {
        messageText.text = message;
        messageUntil = Time.unscaledTime + seconds;
    }

    // Call every frame while the prompt should show
    public void ShowPrompt(string text)
    {
        promptText.text = text;
        promptFrame = Time.frameCount;
    }

    public void ShowFor(GameState state)
    {
        flightHud.SetActive(state == GameState.Flight);
        buildPanel.SetActive(state == GameState.Build);
        shopPanel.SetActive(state == GameState.Shop);
        resultsPanel.SetActive(state == GameState.Results);
        winPanel.SetActive(state == GameState.Won);

        switch (state)
        {
            case GameState.Yard: hintText.text = "A/D: run    Space: jump    E: use    F: get in the rocket    Esc: pause    Goal: the Moon (" + Mathf.RoundToInt(game.moonHeight) + " m)"; break;
            case GameState.Build: hintText.text = "Left click (or drag): place    Right click: remove    1-7: pick a part    E: done"; break;
            case GameState.Shop: hintText.text = "Click Buy    E or Esc: close the shop"; break;
            case GameState.Flight: hintText.text = "Hold SPACE: spray cola    A/D: tilt    R: give up    Esc: pause"; break;
            default: hintText.text = ""; break;
        }

        if (state == GameState.Build)
            RefreshBuildBar();
        if (state == GameState.Shop)
            RefreshShop();
    }

    public void ShowPause(bool paused)
    {
        pausePanel.SetActive(paused);
        RefreshVolumes();
    }

    public void ShowResults(float height, int coinCount, int coinMoney, int earned, bool record, bool crashed)
    {
        resultsText.text =
            (crashed ? "CRASH! A meteor hit your seat.\n\n" : "Out of cola!\n\n") +
            "Height: " + Mathf.RoundToInt(height) + " m" + (record ? "   NEW RECORD!" : "") + "\n" +
            "Coins: " + coinCount + "  (+$" + coinMoney + ")\n" +
            "You earned: $" + earned + "\n\n" +
            "Best: " + Mathf.RoundToInt(game.Data.bestHeight) + " m    Moon: " + Mathf.RoundToInt(game.moonHeight) + " m";
    }

    public void ShowWin()
    {
        winText.text =
            "You flew to the Moon in " + game.Data.flights + " flights!\n\n" +
            Settings.CharacterName(Settings.Character) + " is the first person to reach the Moon on cola power.\n\n" +
            "You can keep playing and build an even crazier rocket.";
    }

    // ---------- Every frame ----------

    void Update()
    {
        moneyText.text = "$" + game.Data.money;
        bestText.text = "Best: " + Mathf.RoundToInt(game.Data.bestHeight) + " m";

        if (Time.unscaledTime > messageUntil)
            messageText.text = "";
        promptText.gameObject.SetActive(promptFrame >= Time.frameCount - 1 && game.State == GameState.Yard && !game.Paused);

        if (game.State == GameState.Flight)
            UpdateFlightHud();
        if (game.State == GameState.Build && buildAdvice != null)
            buildAdvice.text = game.Build.Advice();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || game.JustChanged)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame && (game.State == GameState.Yard || game.State == GameState.Flight))
            game.SetPaused(!game.Paused);
        else if (game.State == GameState.Shop && (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            game.CloseShop();
        // Only Enter (not Space), so holding Space at the end of a flight doesn't skip the results
        else if ((game.State == GameState.Results || game.State == GameState.Won) && keyboard.enterKey.wasPressedThisFrame)
            game.BackToYard();
    }

    void UpdateFlightHud()
    {
        Rocket rocket = game.Rocket;
        float height = game.Flight.Height;
        heightText.text = Mathf.RoundToInt(height) + " m";
        fuelFill.rectTransform.anchorMax = new Vector2(rocket != null ? rocket.FuelFraction : 0f, 1f);

        float now = Mathf.Clamp01(height / game.moonHeight);
        float best = Mathf.Clamp01(game.Data.bestHeight / game.moonHeight);
        heightMarker.anchorMin = heightMarker.anchorMax = new Vector2(0.5f, now);
        bestMarker.anchorMin = new Vector2(0f, best);
        bestMarker.anchorMax = new Vector2(1f, best);
    }

    void RefreshVolumes()
    {
        if (musicText != null)
            musicText.text = "Music: " + Settings.MusicLevel;
        if (soundText != null)
            soundText.text = "Sounds: " + Settings.SoundLevel;
    }

    // ---------- Build bar ----------

    public void RefreshBuildBar()
    {
        if (buildRow == null)
            return;

        foreach (var button in buildButtons)
        {
            int left = game.Data.Available(button.type);
            button.count.text = PartCatalog.Get(button.type).name + "\n" + left + " left";
            button.background.color = button.type == game.Build.Selected ? SelectedColor : (left > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f));
        }
    }

    // ---------- Shop ----------

    public void RefreshShop()
    {
        foreach (Transform child in shopList)
        {
            // Hidden right away so the list does not show old and new rows for a frame
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        shopMoney.text = "You have $" + game.Data.money;

        foreach (PartInfo info in PartCatalog.Parts)
        {
            // You only need one seat
            if (info.type == PartType.Seat && game.Data.Owned(PartType.Seat) > 0)
                continue;

            PartType type = info.type;
            int price = game.PriceOf(type);
            AddShopRow(SpriteLib.Get(info.sprite, info.color), info.name, info.description,
                "You have " + game.Data.Owned(type), "$" + price, game.Data.money >= price, () => game.BuyPart(type));
        }

        AddUpgradeRow(PartCatalog.ThrustUpgrade, game.Data.thrustLevel, 0);
        AddUpgradeRow(PartCatalog.FuelUpgrade, game.Data.fuelLevel, 1);
        AddUpgradeRow(PartCatalog.GridUpgrade, game.Data.gridLevel, 2);
    }

    void AddUpgradeRow(UpgradeInfo upgrade, int level, int which)
    {
        bool maxed = level >= upgrade.prices.Length;
        int price = maxed ? 0 : upgrade.prices[level];
        AddShopRow(null, upgrade.name, upgrade.description, "Level " + level + "/" + upgrade.prices.Length,
            maxed ? "MAX" : "$" + price, !maxed && game.Data.money >= price, () => game.BuyUpgrade(which));
    }

    void AddShopRow(Sprite icon, string title, string description, string owned, string price, bool canBuy, System.Action buy)
    {
        RectTransform row = UIKit.Panel(shopList, "Row", RowColor);
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 50f;

        if (icon != null)
        {
            Image image = UIKit.Image(row, "Icon", Color.white);
            image.sprite = icon;
            image.preserveAspect = true;
            UIKit.Place(image.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(8f, 6f), new Vector2(54f, -6f));
        }

        Text name = UIKit.Label(row, title + "\n<size=14>" + description + "</size>", 18, TextAnchor.MiddleLeft, Color.white);
        UIKit.Place(name.rectTransform, new Vector2(0f, 0f), new Vector2(0.6f, 1f), new Vector2(64f, 0f), Vector2.zero);

        Text ownedText = UIKit.Label(row, owned, 15, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f));
        UIKit.Place(ownedText.rectTransform, new Vector2(0.6f, 0f), new Vector2(0.78f, 1f), Vector2.zero, Vector2.zero);

        Button button = UIKit.Button(row, "Buy " + price, canBuy ? UIKit.ButtonColor : new Color(0.4f, 0.4f, 0.4f), buy);
        button.interactable = canBuy;
        UIKit.Place((RectTransform)button.transform, new Vector2(0.79f, 0f), new Vector2(1f, 1f), new Vector2(0f, 8f), new Vector2(-8f, -8f));
    }

    // ---------- Making the UI ----------

    void Build()
    {
        root = UIKit.MakeCanvas(gameObject, 50);

        // Always on screen
        moneyText = UIKit.Label(root, "$0", 30, TextAnchor.UpperLeft, UIKit.Gold);
        UIKit.Place(moneyText.rectTransform, new Vector2(0f, 1f), new Vector2(0.4f, 1f), new Vector2(20f, -60f), new Vector2(0f, -12f));
        bestText = UIKit.Label(root, "Best: 0 m", 22, TextAnchor.UpperRight, Color.white);
        UIKit.Place(bestText.rectTransform, new Vector2(0.6f, 1f), new Vector2(1f, 1f), new Vector2(0f, -50f), new Vector2(-20f, -14f));
        messageText = UIKit.Label(root, "", 26, TextAnchor.MiddleCenter, Color.white);
        UIKit.Place(messageText.rectTransform, new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.8f), Vector2.zero, Vector2.zero);
        promptText = UIKit.Label(root, "", 24, TextAnchor.MiddleCenter, UIKit.Gold);
        UIKit.Place(promptText.rectTransform, new Vector2(0.1f, 0f), new Vector2(0.9f, 0f), new Vector2(0f, 60f), new Vector2(0f, 100f));
        hintText = UIKit.Label(root, "", 16, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.85f));
        UIKit.Place(hintText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 8f), new Vector2(-10f, 34f));

        BuildFlightHud();
        BuildBuildPanel();
        BuildShopPanel();
        BuildResultsPanel();
        BuildWinPanel();
        BuildPausePanel();
    }

    void BuildFlightHud()
    {
        flightHud = new GameObject("Flight HUD", typeof(RectTransform));
        RectTransform hud = (RectTransform)flightHud.transform;
        hud.SetParent(root, false);
        UIKit.Place(hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        heightText = UIKit.Label(hud, "0 m", 48, TextAnchor.UpperCenter, Color.white);
        UIKit.Place(heightText.rectTransform, new Vector2(0.3f, 1f), new Vector2(0.7f, 1f), new Vector2(0f, -80f), new Vector2(0f, -10f));

        // Cola meter at the bottom
        RectTransform fuelBack = UIKit.Panel(hud, "Cola Meter", new Color(0f, 0f, 0f, 0.6f));
        UIKit.Place(fuelBack, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 44f), new Vector2(200f, 66f));
        fuelFill = UIKit.Image(fuelBack, "Cola", new Color(0.55f, 0.3f, 0.12f));
        UIKit.Place(fuelFill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Text fuelLabel = UIKit.Label(fuelBack, "COLA", 14, TextAnchor.MiddleCenter, Color.white);
        UIKit.Place(fuelLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Height bar on the right with the Moon at the top
        RectTransform bar = UIKit.Panel(hud, "Height Bar", new Color(0f, 0f, 0f, 0.5f));
        UIKit.Place(bar, new Vector2(1f, 0.15f), new Vector2(1f, 0.8f), new Vector2(-46f, 0f), new Vector2(-30f, 0f));
        Image moon = UIKit.Image(hud, "Moon Icon", Color.white);
        moon.sprite = SpriteLib.Get("moon", new Color(0.8f, 0.8f, 0.78f), 256, 256);
        UIKit.Place(moon.rectTransform, new Vector2(1f, 0.8f), new Vector2(1f, 0.8f), new Vector2(-62f, 4f), new Vector2(-14f, 52f));
        bestMarker = UIKit.Image(bar, "Best", UIKit.Gold).rectTransform;
        UIKit.Place(bestMarker, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-6f, -1f), new Vector2(6f, 1f));
        heightMarker = UIKit.Image(bar, "You", Color.white).rectTransform;
        heightMarker.sizeDelta = new Vector2(22f, 8f);

        flightHud.SetActive(false);
    }

    void BuildBuildPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Build Bar", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 36f), new Vector2(0f, 180f));
        buildPanel = panel.gameObject;

        buildRow = new GameObject("Parts", typeof(RectTransform)).GetComponent<RectTransform>();
        buildRow.SetParent(panel, false);
        UIKit.Place(buildRow, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 34f), new Vector2(-150f, -8f));
        HorizontalLayoutGroup row = buildRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 8f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;

        for (int i = 0; i < PartCatalog.Count; i++)
        {
            PartInfo info = PartCatalog.Parts[i];
            PartType type = info.type;

            Button button = UIKit.Button(buildRow, "", Color.white, () => game.Build.Select(type));
            Image background = button.GetComponent<Image>();

            Image icon = UIKit.Image(button.transform, "Icon", Color.white);
            icon.sprite = SpriteLib.Get(info.sprite, info.color);
            icon.preserveAspect = true;
            UIKit.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-20f, -46f), new Vector2(20f, -6f));

            Text count = UIKit.Label(button.transform, info.name, 14, TextAnchor.LowerCenter, Color.black, false);
            UIKit.Place(count.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 4f), new Vector2(-2f, -48f));

            Text key = UIKit.Label(button.transform, (i + 1).ToString(), 13, TextAnchor.UpperLeft, new Color(0f, 0f, 0f, 0.6f), false);
            UIKit.Place(key.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 0f), new Vector2(0f, -3f));

            buildButtons.Add((type, background, count));
        }

        Button done = UIKit.Button(panel, "Done (E)", UIKit.ButtonColor, () => game.ExitBuild());
        UIKit.Place((RectTransform)done.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140f, -70f), new Vector2(-10f, -8f));
        Button clear = UIKit.Button(panel, "Clear all", new Color(0.7f, 0.3f, 0.3f), () => game.Build.ClearAll());
        UIKit.Place((RectTransform)clear.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140f, -136f), new Vector2(-10f, -78f));

        buildAdvice = UIKit.Label(panel, "", 16, TextAnchor.MiddleCenter, UIKit.Gold);
        UIKit.Place(buildAdvice.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 4f), new Vector2(-150f, 32f));

        buildPanel.SetActive(false);
    }

    void BuildShopPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Shop", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450f, -330f), new Vector2(450f, 330f));
        shopPanel = panel.gameObject;

        Text title = UIKit.Label(panel, "SHOP", 34, TextAnchor.UpperLeft, UIKit.Gold);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -56f), new Vector2(0f, -10f));
        shopMoney = UIKit.Label(panel, "", 22, TextAnchor.UpperRight, Color.white);
        UIKit.Place(shopMoney.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, -50f), new Vector2(-20f, -16f));

        shopList = new GameObject("Items", typeof(RectTransform)).GetComponent<RectTransform>();
        shopList.SetParent(panel, false);
        UIKit.Place(shopList, Vector2.zero, Vector2.one, new Vector2(16f, 64f), new Vector2(-16f, -62f));
        VerticalLayoutGroup list = shopList.gameObject.AddComponent<VerticalLayoutGroup>();
        list.spacing = 4f;
        list.childControlWidth = true;
        list.childControlHeight = true;
        list.childForceExpandWidth = true;
        list.childForceExpandHeight = false;

        Button close = UIKit.Button(panel, "Close (E)", UIKit.ButtonColor, () => game.CloseShop());
        UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-90f, 14f), new Vector2(90f, 54f));

        shopPanel.SetActive(false);
    }

    void BuildResultsPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Results", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300f, -200f), new Vector2(300f, 200f));
        resultsPanel = panel.gameObject;

        Text title = UIKit.Label(panel, "FLIGHT OVER", 34, TextAnchor.UpperCenter, UIKit.Gold);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(0f, -12f));
        resultsText = UIKit.Label(panel, "", 22, TextAnchor.UpperCenter, Color.white);
        UIKit.Place(resultsText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 70f), new Vector2(-20f, -70f));

        Button back = UIKit.Button(panel, "Back to the yard (Enter)", UIKit.ButtonColor, () => game.BackToYard());
        UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 16f), new Vector2(150f, 58f));

        resultsPanel.SetActive(false);
    }

    void BuildWinPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Moon", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -210f), new Vector2(320f, 210f));
        winPanel = panel.gameObject;

        Text title = UIKit.Label(panel, "YOU REACHED THE MOON!", 36, TextAnchor.UpperCenter, UIKit.Gold);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), new Vector2(0f, -12f));
        winText = UIKit.Label(panel, "", 22, TextAnchor.UpperCenter, Color.white);
        UIKit.Place(winText.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 70f), new Vector2(-24f, -76f));

        Button back = UIKit.Button(panel, "Keep playing (Enter)", UIKit.ButtonColor, () => game.BackToYard());
        UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 16f), new Vector2(150f, 58f));

        winPanel.SetActive(false);
    }

    void BuildPausePanel()
    {
        RectTransform panel = UIKit.Panel(root, "Pause", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, -200f), new Vector2(220f, 200f));
        pausePanel = panel.gameObject;

        Text title = UIKit.Label(panel, "PAUSED", 36, TextAnchor.UpperCenter, UIKit.Gold);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), new Vector2(0f, -12f));

        Button resume = UIKit.Button(panel, "Resume (Esc)", UIKit.ButtonColor, () => game.SetPaused(false));
        UIKit.Place((RectTransform)resume.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-140f, -124f), new Vector2(140f, -80f));

        musicText = VolumeRow(panel, -150f, () => Settings.MusicLevel--, () => Settings.MusicLevel++);
        soundText = VolumeRow(panel, -204f, () => Settings.SoundLevel--, () => Settings.SoundLevel++);

        Button menu = UIKit.Button(panel, "Main Menu", new Color(0.7f, 0.3f, 0.3f), () => game.GoToMenu());
        UIKit.Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 20f), new Vector2(140f, 64f));

        pausePanel.SetActive(false);
    }

    // "-  Music: 7  +" row; returns the text in the middle
    Text VolumeRow(RectTransform panel, float top, System.Action down, System.Action up)
    {
        Button minus = UIKit.Button(panel, "-", UIKit.ButtonColor, () => { down(); RefreshVolumes(); }, 26);
        UIKit.Place((RectTransform)minus.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-140f, top - 44f), new Vector2(-96f, top));
        Button plus = UIKit.Button(panel, "+", UIKit.ButtonColor, () => { up(); RefreshVolumes(); }, 26);
        UIKit.Place((RectTransform)plus.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(96f, top - 44f), new Vector2(140f, top));

        Text text = UIKit.Label(panel, "", 22, TextAnchor.MiddleCenter, Color.white);
        UIKit.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-92f, top - 44f), new Vector2(92f, top));
        return text;
    }
}
