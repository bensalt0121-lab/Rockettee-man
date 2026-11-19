/*********************************************************************************************
 * COMPONENT OF: Game UI (made by the game in the Play scene)
 * REQUIRED DEPENDENCIES: RocketeeGame, UIKit, Settings, Stages, uGUI, Input System package
 * DESCRIPTION: Everything on the screen in the Play scene: money and best height, messages,
 *              the "Press E" prompt, the shop (a scroll list: click Buy), the build bar with
 *              your parts, the height, cola and heat meters while flying, the results and
 *              stage clear screens, the pause menu (Esc) and the dev tools (F10, dev mode).
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.2
 * VERSION 1.1: Quick buttons after a flight, milestones.
 * VERSION 1.2: Stages, scrolling shop, two-row build bar, heat meter, dev tools.
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    static readonly Color RowColor = new Color(1f, 1f, 1f, 0.07f);
    static readonly Color SelectedColor = new Color(1f, 0.85f, 0.3f);
    static readonly Color NewColor = new Color(0.4f, 1f, 0.5f);

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
    private GameObject heatMeter;
    private Image heatFill;
    private Text heatLabel;

    private GameObject buildPanel;
    private Text buildAdvice;
    private readonly List<(PartType type, Image background, Text count)> buildButtons = new List<(PartType, Image, Text)>();
    private readonly List<PartType> buildBarParts = new List<PartType>();

    private GameObject shopPanel;
    private RectTransform shopList;
    private ScrollRect shopScroll;
    private Text shopMoney;

    private GameObject resultsPanel;
    private Text resultsText;
    private GameObject winPanel;
    private Text winTitle;
    private Text winText;
    private Button winButton;

    private GameObject pausePanel;
    private Text musicText;
    private Text soundText;
    private GameObject devPanel;

    // The parts in the build bar, in order (number keys 1-9 pick these)
    public IReadOnlyList<PartType> BuildBarParts => buildBarParts;

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

        StageInfo stage = game.Data.Stage;
        switch (state)
        {
            case GameState.Yard:
                hintText.text = "Stage " + (game.Data.stage + 1) + ": " + stage.name + " (" + Mathf.RoundToInt(stage.goalHeight) + " m)"
                    + "     A/D: run   Space: jump   E: use   F: get in the rocket   Esc: pause";
                break;
            case GameState.Build: hintText.text = "Left click (or drag): place    Right click: remove    1-9: pick a part    E: done"; break;
            case GameState.Shop: hintText.text = "Click Buy    Mouse wheel: scroll    E or Esc: close the shop"; break;
            case GameState.Flight: hintText.text = "Hold SPACE: spray cola    A/D: tilt    R: give up    Esc: pause"; break;
            default: hintText.text = ""; break;
        }

        if (state == GameState.Build)
            RefreshBuildBar();
        if (state == GameState.Shop)
        {
            RefreshShop();
            shopScroll.verticalNormalizedPosition = 1f;
        }
        if (state == GameState.Flight)
            heatMeter.SetActive(stage.heat);
    }

    public void ShowPause(bool paused)
    {
        pausePanel.SetActive(paused);
        if (paused)
            devPanel.SetActive(false);
        RefreshVolumes();
    }

    public void ToggleDevPanel()
    {
        devPanel.SetActive(!devPanel.activeSelf);
    }

    public void ShowResults(float height, int coinCount, int coinMoney, int earned, bool record, string crashReason, string milestones)
    {
        resultsText.text =
            (crashReason != null ? "CRASH! " + crashReason + "\n" : "Out of cola!\n") +
            "Height: " + Mathf.RoundToInt(height) + " m" + (record ? "   NEW RECORD!" : "") + "\n" +
            "Coins: " + coinCount + "  (+$" + coinMoney + ")\n" +
            milestones +
            "You earned: $" + earned + "\n" +
            "Best: " + Mathf.RoundToInt(game.Data.Best) + " m    " + game.Data.Stage.goalName + ": " + Mathf.RoundToInt(game.GoalHeight) + " m";
    }

    public void ShowWin()
    {
        StageInfo stage = game.Data.Stage;
        string who = Settings.CharacterName(Settings.Character);

        if (game.OnLastStage)
        {
            winTitle.text = "YOU REACHED THE SUN!";
            winText.text =
                who + " flew from Earth to the Moon, to Mars and all the way to the Sun on cola power!\n\n" +
                "You beat Rocketee Man in " + game.Data.flights + " flights.\n\n" +
                "You can keep playing and build an even crazier rocket.";
            UIKit.SetButtonText(winButton, "Keep playing (Enter)");
        }
        else
        {
            StageInfo next = Stages.Get(game.Data.stage + 1);
            winTitle.text = "YOU REACHED " + stage.goalName.ToUpper() + "!";
            winText.text =
                "Stage " + (game.Data.stage + 1) + " cleared!\n\n" +
                "Next: Stage " + (game.Data.stage + 2) + " - " + next.name + " (" + Mathf.RoundToInt(next.goalHeight) + " m)\n" +
                next.news;
            UIKit.SetButtonText(winButton, "Go to stage " + (game.Data.stage + 2) + " (Enter)");
        }
    }

    // ---------- Every frame ----------

    void Update()
    {
        moneyText.text = "$" + game.Data.money;
        bestText.text = "Best: " + Mathf.RoundToInt(game.Data.Best) + " m" + (Settings.DevMode ? "   [DEV: F10]" : "");

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
        else if (game.State == GameState.Results && keyboard.enterKey.wasPressedThisFrame)
            game.BackToYard();
        else if (game.State == GameState.Won && keyboard.enterKey.wasPressedThisFrame)
            game.NextStage();
        else if (game.State == GameState.Results && keyboard.fKey.wasPressedThisFrame)
            game.FlyAgain();
        else if (game.State == GameState.Results && keyboard.bKey.wasPressedThisFrame)
            game.BuildFromResults();
        else if (game.State == GameState.Results && keyboard.sKey.wasPressedThisFrame)
            game.ShopFromResults();
    }

    void UpdateFlightHud()
    {
        Rocket rocket = game.Rocket;
        float height = game.Flight.Height;
        heightText.text = Mathf.RoundToInt(height) + " m";
        fuelFill.rectTransform.anchorMax = new Vector2(rocket != null ? rocket.FuelFraction : 0f, 1f);

        float now = Mathf.Clamp01(height / game.GoalHeight);
        float best = Mathf.Clamp01(game.Data.Best / game.GoalHeight);
        heightMarker.anchorMin = heightMarker.anchorMax = new Vector2(0.5f, now);
        bestMarker.anchorMin = new Vector2(0f, best);
        bestMarker.anchorMax = new Vector2(1f, best);

        if (heatMeter.activeSelf && rocket != null)
        {
            float heat = rocket.Heat;
            heatFill.rectTransform.anchorMax = new Vector2(heat, 1f);
            heatFill.color = Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(1f, 0.15f, 0.1f), heat);
            bool flash = heat > 0.75f && Mathf.FloorToInt(Time.time * 4f) % 2 == 0;
            heatLabel.text = flash ? "TOO HOT!" : "HEAT";
        }
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

        GameSave save = game.Data;
        shopMoney.text = "You have $" + save.money;

        AddHeader("PARTS");
        foreach (PartInfo info in PartCatalog.Parts)
        {
            if (!save.Unlocked(info))
                continue;
            // You only need one seat (and there is a free one lying in the yard)
            if (info.type == PartType.Seat && (save.Owned(PartType.Seat) > 0 || !save.collected.Contains("starter_seat")))
                continue;

            PartType type = info.type;
            int price = game.PriceOf(type);
            bool isNew = info.unlockStage > 0 && info.unlockStage == save.stage;
            AddShopRow(SpriteLib.Get(info.sprite, info.color), (isNew ? "NEW! " : "") + info.name, info.description,
                "You have " + save.Owned(type), "$" + price, save.money >= price, () => game.BuyPart(type), isNew);
        }

        AddHeader("UPGRADES");
        foreach (UpgradeInfo upgrade in PartCatalog.Upgrades)
        {
            if (!save.Unlocked(upgrade))
                continue;

            UpgradeType type = upgrade.type;
            int level = save.Level(type);
            bool maxed = level >= upgrade.prices.Length;
            int price = maxed ? 0 : upgrade.prices[level];
            bool isNew = upgrade.unlockStage > 0 && upgrade.unlockStage == save.stage;
            AddShopRow(null, (isNew ? "NEW! " : "") + upgrade.name, upgrade.description, "Level " + level + "/" + upgrade.prices.Length,
                maxed ? "MAX" : "$" + price, !maxed && save.money >= price, () => game.BuyUpgrade(type), isNew);
        }
    }

    void AddHeader(string title)
    {
        Text header = UIKit.Label(shopList, title, 20, TextAnchor.LowerLeft, UIKit.Gold);
        LayoutElement layout = header.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 30f;
    }

    void AddShopRow(Sprite icon, string title, string description, string owned, string price, bool canBuy, System.Action buy, bool isNew)
    {
        RectTransform row = UIKit.Panel(shopList, "Row", isNew ? new Color(0.3f, 1f, 0.4f, 0.12f) : RowColor);
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 50f;

        if (icon != null)
        {
            Image image = UIKit.Image(row, "Icon", Color.white);
            image.sprite = icon;
            image.preserveAspect = true;
            UIKit.Place(image.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(8f, 6f), new Vector2(54f, -6f));
        }

        Text name = UIKit.Label(row, title + "\n<size=14>" + description + "</size>", 18, TextAnchor.MiddleLeft, isNew ? NewColor : Color.white);
        UIKit.Place(name.rectTransform, new Vector2(0f, 0f), new Vector2(0.62f, 1f), new Vector2(64f, 0f), Vector2.zero);

        Text ownedText = UIKit.Label(row, owned, 15, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f));
        UIKit.Place(ownedText.rectTransform, new Vector2(0.62f, 0f), new Vector2(0.78f, 1f), Vector2.zero, Vector2.zero);

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
        UIKit.Place(bestText.rectTransform, new Vector2(0.55f, 1f), new Vector2(1f, 1f), new Vector2(0f, -50f), new Vector2(-20f, -14f));
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
        BuildDevPanel();
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

        // Heat meter above it (only near the Sun)
        RectTransform heatBack = UIKit.Panel(hud, "Heat Meter", new Color(0f, 0f, 0f, 0.6f));
        UIKit.Place(heatBack, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 72f), new Vector2(200f, 94f));
        heatMeter = heatBack.gameObject;
        heatFill = UIKit.Image(heatBack, "Heat", new Color(1f, 0.6f, 0.1f));
        UIKit.Place(heatFill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        heatFill.rectTransform.anchorMax = new Vector2(0f, 1f);
        heatLabel = UIKit.Label(heatBack, "HEAT", 14, TextAnchor.MiddleCenter, Color.white);
        UIKit.Place(heatLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        heatMeter.SetActive(false);

        // Height bar on the right with the goal (Moon, Mars or Sun) at the top
        RectTransform bar = UIKit.Panel(hud, "Height Bar", new Color(0f, 0f, 0f, 0.5f));
        UIKit.Place(bar, new Vector2(1f, 0.15f), new Vector2(1f, 0.8f), new Vector2(-46f, 0f), new Vector2(-30f, 0f));
        StageInfo stage = game.Data.Stage;
        Image goalIcon = UIKit.Image(hud, "Goal Icon", Color.white);
        goalIcon.sprite = SpriteLib.Get(stage.goalSprite, new Color(0.8f, 0.8f, 0.78f), 256, 256);
        UIKit.Place(goalIcon.rectTransform, new Vector2(1f, 0.8f), new Vector2(1f, 0.8f), new Vector2(-62f, 4f), new Vector2(-14f, 52f));
        bestMarker = UIKit.Image(bar, "Best", UIKit.Gold).rectTransform;
        UIKit.Place(bestMarker, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-6f, -1f), new Vector2(6f, 1f));
        heightMarker = UIKit.Image(bar, "You", Color.white).rectTransform;
        heightMarker.sizeDelta = new Vector2(22f, 8f);

        flightHud.SetActive(false);
    }

    void BuildBuildPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Build Bar", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 36f), new Vector2(0f, 216f));
        buildPanel = panel.gameObject;

        // Parts in two rows
        RectTransform grid = new GameObject("Parts", typeof(RectTransform)).GetComponent<RectTransform>();
        grid.SetParent(panel, false);
        UIKit.Place(grid, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 34f), new Vector2(-150f, -6f));
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(100f, 66f);
        layout.spacing = new Vector2(6f, 6f);
        layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        layout.constraintCount = 2;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.MiddleCenter;

        GameSave save = game.Data;
        foreach (PartInfo info in PartCatalog.Parts)
        {
            // Parts sold in this stage, or ones you already have
            if (!save.Unlocked(info) && save.Owned(info.type) == 0)
                continue;

            PartType type = info.type;
            buildBarParts.Add(type);
            int keyNumber = buildBarParts.Count;

            Button button = UIKit.Button(grid, "", Color.white, () => game.Build.Select(type));
            Image background = button.GetComponent<Image>();

            Image icon = UIKit.Image(button.transform, "Icon", Color.white);
            icon.sprite = SpriteLib.Get(info.sprite, info.color);
            icon.preserveAspect = true;
            UIKit.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-16f, -34f), new Vector2(16f, -3f));

            Text count = UIKit.Label(button.transform, info.name, 12, TextAnchor.LowerCenter, Color.black, false);
            UIKit.Place(count.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -34f));

            if (keyNumber <= 9)
            {
                Text key = UIKit.Label(button.transform, keyNumber.ToString(), 12, TextAnchor.UpperLeft, new Color(0f, 0f, 0f, 0.6f), false);
                UIKit.Place(key.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(0f, -2f));
            }

            buildButtons.Add((type, background, count));
        }

        Button done = UIKit.Button(panel, "Done (E)", UIKit.ButtonColor, () => game.ExitBuild());
        UIKit.Place((RectTransform)done.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140f, -80f), new Vector2(-10f, -8f));
        Button clear = UIKit.Button(panel, "Clear all", new Color(0.7f, 0.3f, 0.3f), () => game.Build.ClearAll());
        UIKit.Place((RectTransform)clear.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140f, -150f), new Vector2(-10f, -88f));

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

        // A scroll list: the visible window (viewport) and the list inside it (content)
        RectTransform viewport = UIKit.Panel(panel, "Viewport", new Color(0f, 0f, 0f, 0.2f));
        UIKit.Place(viewport, Vector2.zero, Vector2.one, new Vector2(16f, 64f), new Vector2(-34f, -62f));
        viewport.gameObject.AddComponent<RectMask2D>();

        shopList = new GameObject("Items", typeof(RectTransform)).GetComponent<RectTransform>();
        shopList.SetParent(viewport, false);
        shopList.anchorMin = new Vector2(0f, 1f);
        shopList.anchorMax = new Vector2(1f, 1f);
        shopList.pivot = new Vector2(0.5f, 1f);
        shopList.offsetMin = new Vector2(4f, 0f);
        shopList.offsetMax = new Vector2(-4f, 0f);
        VerticalLayoutGroup list = shopList.gameObject.AddComponent<VerticalLayoutGroup>();
        list.spacing = 4f;
        list.padding = new RectOffset(0, 0, 4, 8);
        list.childControlWidth = true;
        list.childControlHeight = true;
        list.childForceExpandWidth = true;
        list.childForceExpandHeight = false;
        ContentSizeFitter fitter = shopList.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Scroll bar on the right
        RectTransform track = UIKit.Panel(panel, "Scrollbar", new Color(1f, 1f, 1f, 0.1f));
        UIKit.Place(track, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-28f, 64f), new Vector2(-16f, -62f));
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        RectTransform handle = UIKit.Panel(track, "Handle", UIKit.ButtonColor);
        UIKit.Place(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();

        shopScroll = panel.gameObject.AddComponent<ScrollRect>();
        shopScroll.viewport = viewport;
        shopScroll.content = shopList;
        shopScroll.horizontal = false;
        shopScroll.vertical = true;
        shopScroll.movementType = ScrollRect.MovementType.Clamped;
        shopScroll.scrollSensitivity = 30f;
        shopScroll.verticalScrollbar = scrollbar;
        shopScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        Button close = UIKit.Button(panel, "Close (E)", UIKit.ButtonColor, () => game.CloseShop());
        UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-90f, 14f), new Vector2(90f, 54f));

        shopPanel.SetActive(false);
    }

    void BuildResultsPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Results", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -215f), new Vector2(330f, 215f));
        resultsPanel = panel.gameObject;

        Text title = UIKit.Label(panel, "FLIGHT OVER", 34, TextAnchor.UpperCenter, UIKit.Gold);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(0f, -12f));
        resultsText = UIKit.Label(panel, "", 22, TextAnchor.UpperCenter, Color.white);
        UIKit.Place(resultsText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 80f), new Vector2(-20f, -66f));

        // Quick buttons so you don't have to walk back across the yard every time
        string[] labels = { "Fly again (F)", "Build (B)", "Shop (S)", "Yard (Enter)" };
        Color[] colors = { new Color(0.25f, 0.6f, 0.35f), UIKit.ButtonColor, new Color(0.3f, 0.55f, 0.85f), new Color(0.45f, 0.45f, 0.5f) };
        System.Action[] actions = { game.FlyAgain, game.BuildFromResults, game.ShopFromResults, game.BackToYard };
        for (int i = 0; i < labels.Length; i++)
        {
            Button button = UIKit.Button(panel, labels[i], colors[i], actions[i], 17);
            float x = -310f + i * 155f;
            UIKit.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 16f), new Vector2(x + 145f, 62f));
        }

        resultsPanel.SetActive(false);
    }

    void BuildWinPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Goal Reached", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-340f, -220f), new Vector2(340f, 220f));
        winPanel = panel.gameObject;

        winTitle = UIKit.Label(panel, "", 36, TextAnchor.UpperCenter, UIKit.Gold);
        UIKit.Place(winTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), new Vector2(0f, -12f));
        winText = UIKit.Label(panel, "", 21, TextAnchor.UpperCenter, Color.white);
        UIKit.Place(winText.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 70f), new Vector2(-24f, -76f));

        winButton = UIKit.Button(panel, "Next (Enter)", UIKit.ButtonColor, () => game.NextStage());
        UIKit.Place((RectTransform)winButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 16f), new Vector2(160f, 58f));

        winPanel.SetActive(false);
    }

    void BuildPausePanel()
    {
        RectTransform panel = UIKit.Panel(root, "Pause", UIKit.PanelColor);
        UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, -220f), new Vector2(220f, 220f));
        pausePanel = panel.gameObject;

        Text title = UIKit.Label(panel, "PAUSED", 36, TextAnchor.UpperCenter, UIKit.Gold);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), new Vector2(0f, -12f));

        Button resume = UIKit.Button(panel, "Resume (Esc)", UIKit.ButtonColor, () => game.SetPaused(false));
        UIKit.Place((RectTransform)resume.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-140f, -124f), new Vector2(140f, -80f));

        musicText = VolumeRow(panel, -150f, () => Settings.MusicLevel--, () => Settings.MusicLevel++);
        soundText = VolumeRow(panel, -204f, () => Settings.SoundLevel--, () => Settings.SoundLevel++);

        if (Settings.DevMode)
        {
            Button dev = UIKit.Button(panel, "Dev Tools", new Color(0.5f, 0.3f, 0.7f), () => { game.SetPaused(false); devPanel.SetActive(true); });
            UIKit.Place((RectTransform)dev.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 74f), new Vector2(140f, 114f));
        }

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

    // Dev tools (dev mode only): F10 or Pause > Dev Tools
    void BuildDevPanel()
    {
        RectTransform panel = UIKit.Panel(root, "Dev Tools", new Color(0.15f, 0.08f, 0.22f, 0.95f));
        UIKit.Place(panel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, -230f), new Vector2(276f, 230f));
        devPanel = panel.gameObject;

        Text title = UIKit.Label(panel, "DEV TOOLS (F10)", 22, TextAnchor.UpperCenter, new Color(0.85f, 0.6f, 1f));
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), new Vector2(0f, -8f));

        (string label, System.Action action)[] tools =
        {
            ("+ $1,000", () => game.DevAddMoney(1000)),
            ("+ $10,000", () => game.DevAddMoney(10000)),
            ("+10 of every part", game.DevGiveParts),
            ("Max all upgrades", game.DevMaxUpgrades),
            ("God mode on/off", game.DevToggleGod),
            ("Infinite cola on/off", game.DevToggleInfiniteCola),
            ("Go to stage 1 (Moon)", () => game.DevSetStage(0)),
            ("Go to stage 2 (Mars)", () => game.DevSetStage(1)),
            ("Go to stage 3 (Sun)", () => game.DevSetStage(2)),
            ("Close", () => devPanel.SetActive(false)),
        };

        float top = -48f;
        foreach (var tool in tools)
        {
            Button button = UIKit.Button(panel, tool.label, new Color(0.45f, 0.28f, 0.6f), tool.action, 16);
            UIKit.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, top - 36f), new Vector2(-14f, top));
            top -= 40f;
        }

        devPanel.SetActive(false);
    }
}
