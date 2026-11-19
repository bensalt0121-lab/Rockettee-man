/*********************************************************************************************
 * COMPONENT OF: Rocketee Game (made automatically by SceneBoot in the Play scene)
 * REQUIRED DEPENDENCIES: a Tilemap with a Tilemap Collider 2D as the ground (a plain ground is
 *                        made if there is none), all the other Rocketee Man scripts
 * DESCRIPTION: The boss of the game. Sets up the yard for the current stage (shop on the left,
 *              launch pad, free starter parts, the player), keeps the money and saved
 *              progress, and switches between walking in the yard, the shop, building, flying
 *              and the results.
 *              Game loop: build a rocket -> fly -> earn money for every meter + coins -> buy
 *              more parts and upgrades -> fly higher -> reach the goal -> next stage
 *              (Earth -> Moon -> Mars -> the Sun).
 *              Esc pauses. F12 twice in the yard deletes all progress (for testing).
 *              Dev mode (password in the menu settings): F10 opens the dev tools.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.2
 * VERSION 1.1: Height milestones, quick buttons after a flight.
 * VERSION 1.2: Three stages, more upgrades, dev tools.
 *********************************************************************************************/
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public enum GameState { Yard, Build, Shop, Flight, Results, Won }

public class RocketeeGame : MonoBehaviour
{
    public static RocketeeGame Instance { get; private set; }

    [Header("Money")]
    public float moneyPerMeter = 1f;
    public int recordBonus = 20;
    public int goalBonus = 500;

    // One-time bonus the first time you pass these parts of the way to the goal (times the stage number)
    public static readonly (float part, int bonus)[] Milestones = { (0.1f, 30), (0.25f, 60), (0.5f, 100), (0.75f, 150) };

    public GameSave Data { get; private set; }
    public GameState State { get; private set; } = GameState.Yard;
    // Frame when the state last changed. A key press that changed the state (like E) is
    // ignored by the next screen in the same frame, so it does not close right away.
    public int StateFrame { get; private set; }
    public bool JustChanged => Time.frameCount <= StateFrame;
    public float GroundY { get; private set; }
    public float YardLeft { get; private set; }
    public float YardRight { get; private set; }
    public float GoalHeight => Data.Stage.goalHeight;

    public YardPlayer Player { get; private set; }
    public LaunchPad Pad { get; private set; }
    public Rocket Rocket { get; private set; }
    public BuildMode Build { get; private set; }
    public FlightDirector Flight { get; private set; }
    public GameUI UI { get; private set; }
    public CameraRig Cam { get; private set; }
    public Effects Effects { get; private set; }

    public bool Paused { get; private set; }

    // Dev tools (only with dev mode on)
    public bool GodMode { get; private set; }
    public bool InfiniteCola { get; private set; }

    private float resetConfirmUntil;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Data = GameSave.Load();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Start()
    {
        Effects = gameObject.AddComponent<Effects>();
        SetUpYard();
        Cam = CameraRig.Attach();

        Rocket = Rocket.Create();
        Rocket.PlaceOnPad(Pad);
        Rocket.Rebuild(Data, false);

        Build = gameObject.AddComponent<BuildMode>();
        Flight = gameObject.AddComponent<FlightDirector>();
        gameObject.AddComponent<Scenery>();
        MusicPlayer.Play();
        UI = GameUI.Create(this);

        Time.timeScale = 1f;
        SetState(GameState.Yard);
        Cam.Snap();

        StageInfo stage = Data.Stage;
        string who = Settings.CharacterName(Settings.Character);
        if (Data.flights == 0)
            UI.Say("Welcome, " + who + "! Grab the parts lying around,\nthen press E at the launch pad to build a rocket.", 7f);
        else
            UI.Say("Stage " + (Data.stage + 1) + ": " + stage.name + ". Best flight: " + Mathf.RoundToInt(Data.Best) + " m, "
                + stage.goalName + " is at " + Mathf.RoundToInt(stage.goalHeight) + " m.", 5f);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || Paused)
            return;

        // Dev tools
        if (keyboard.f10Key.wasPressedThisFrame && Settings.DevMode && (State == GameState.Yard || State == GameState.Flight))
            UI.ToggleDevPanel();

        if (State != GameState.Yard || !keyboard.f12Key.wasPressedThisFrame)
            return;

        if (Time.unscaledTime < resetConfirmUntil)
            ResetProgress();
        else
        {
            resetConfirmUntil = Time.unscaledTime + 3f;
            UI.Say("Press F12 again to delete ALL progress.", 3f);
        }
    }

    // ---------- The yard ----------

    void SetUpYard()
    {
        if (!YardGround.HasGround())
            MakePlainGround();
        MakeLightIfMissing();

        // Makes the simple flat yard with your sand tiles if Build Everything has not done it yet
        bool changed = YardBuilder.MakeSimpleGround(YardBuilder.Ground()) > 0;

        YardGround.Span(out float left, out float right);
        YardLeft = left;
        YardRight = right;
        GroundY = YardGround.TopAt(YardBuilder.PadX);

        MakeWall(left - 0.5f);
        MakeWall(right + 0.5f);
        MakeFarGround();

        // Uses the ones you placed in the scene, or makes new ones
        Pad = FindAnyObjectByType<LaunchPad>();
        if (Pad == null)
            Pad = LaunchPad.Create(Mathf.Clamp(YardBuilder.PadX, left + 3f, right - 3f));
        else
            Pad.FixSize();

        ShopStand shop = FindAnyObjectByType<ShopStand>();
        if (shop == null)
            ShopStand.Create(Mathf.Clamp(YardBuilder.ShopX, left + 1.5f, right - 1.5f));
        else
            shop.FixSize();

        if (FindAnyObjectByType<PartPickup>() == null)
            MakeStarterParts(left, right);

        Player = FindAnyObjectByType<YardPlayer>();
        if (Player == null)
            Player = YardPlayer.Create(Mathf.Clamp(YardBuilder.PlayerX, left + 1f, right - 1f));

        // The yard just changed: move everything to its spot
        if (changed)
            YardBuilder.PlaceYardObjects(left, right);

        // Moon or Mars ground, decorations of this stage
        YardBuilder.ApplyStage(Data.Stage, left, right);
    }

    public static void MakeStarterParts(float left, float right)
    {
        foreach (var starter in YardBuilder.StarterParts)
            PartPickup.Create(starter.id, starter.type, Mathf.Clamp(starter.x, left + 1f, right - 1f));
    }

    // Invisible walls so you can't walk off the edge of the yard
    void MakeWall(float x)
    {
        GameObject wall = new GameObject("Yard Wall");
        wall.transform.position = new Vector3(x, GroundY + 5f, 0f);
        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 14f);
    }

    // Solid ground far past the yard, so a rocket that drifts sideways lands instead of
    // falling forever (the sand you see there is drawn by Scenery)
    void MakeFarGround()
    {
        GameObject ground = new GameObject("Far Ground (solid)");
        ground.transform.position = new Vector3(0f, GroundY - 1f, 0f);
        BoxCollider2D box = ground.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2000f, 2f);
    }

    // Only used if the scene has no ground tiles
    void MakePlainGround()
    {
        GameObject ground = new GameObject("Plain Ground");
        ground.transform.position = new Vector3(0f, YardGround.DefaultGround - 1f, 0f);
        BoxCollider2D box = ground.AddComponent<BoxCollider2D>();
        box.size = new Vector2(YardGround.DefaultRight - YardGround.DefaultLeft, 2f);
        SpriteLib.Show(ground.transform, "Look", SpriteLib.Box(32, 32, new Color(0.86f, 0.72f, 0.45f)), box.size, -1);
    }

    // 2D sprites need a light to be seen; your scene has a Global Light 2D, a new scene might not
    void MakeLightIfMissing()
    {
        if (FindAnyObjectByType<Light2D>() != null)
            return;

        Light2D light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    public void CollectStarterPart(string id, PartType type)
    {
        if (Data.collected.Contains(id))
            return;

        Data.collected.Add(id);
        Data.owned[(int)type]++;
        Data.Save();
        Sfx.Play("pickup");

        if (Data.collected.Count >= YardBuilder.StarterParts.Length)
            UI.Say("You have everything for your first rocket!\nGo to the launch pad and press E to build.", 5f);
        else
            UI.Say("Picked up: " + PartCatalog.Get(type).name + "!", 2f);
    }

    // ---------- Switching between parts of the game ----------

    public void SetState(GameState state)
    {
        State = state;
        StateFrame = Time.frameCount;
        UI.ShowFor(state);
    }

    public void OpenShop()
    {
        if (State != GameState.Yard)
            return;
        SetState(GameState.Shop);
        Sfx.Play("click");
    }

    public void CloseShop()
    {
        if (State == GameState.Shop)
            SetState(GameState.Yard);
    }

    public void EnterBuild()
    {
        if (State != GameState.Yard)
            return;
        Build.Begin();
        SetState(GameState.Build);
    }

    public void ExitBuild()
    {
        if (State != GameState.Build)
            return;

        Build.End();
        Data.Save();
        Rocket.Rebuild(Data, false);
        SetState(GameState.Yard);

        string problem = Rocket.ProblemWith(Data);
        UI.Say(problem ?? "Rocket ready! Press F at the launch pad to get in.", 4f);
    }

    public void BoardRocket()
    {
        if (State != GameState.Yard)
            return;

        string problem = Rocket.ProblemWith(Data);
        if (problem != null)
        {
            UI.Say(problem, 4f);
            Sfx.Play("error");
            return;
        }

        Player.SitDown();
        Rocket.Launch(Data);
        Flight.Begin();
        SetState(GameState.Flight);
        Sfx.Play("launch");
        UI.Say("Hold SPACE to spray the cola!", 3f);
    }

    public void EndFlight(float height, int coinMoney, int coinCount, bool crashed)
    {
        if (State != GameState.Flight)
            return;

        bool record = height > Data.Best + 0.5f;
        int milestoneMoney = PayMilestones(height, out string milestoneText);
        int earned = MeterMoney(height) + coinMoney + (record ? recordBonus : 0) + milestoneMoney;

        Data.money += earned;
        Data.flights++;
        if (record)
            Data.Best = height;
        Data.Save();

        SetState(GameState.Results);
        UI.ShowResults(height, coinCount, coinMoney, earned, record, crashed ? Rocket.CrashReason : null, milestoneText);
        Sfx.Play("buy");
    }

    // Money for the height you reached (the Sponsor Deal upgrade adds more)
    int MeterMoney(float height)
    {
        return Mathf.FloorToInt(height * moneyPerMeter * Data.MoneyPerMeterMultiplier);
    }

    int PayMilestones(float height, out string text)
    {
        int money = 0;
        text = "";
        int stageNumber = Data.stage + 1;
        while (Data.stageMilestones[Data.stage] < Milestones.Length)
        {
            var milestone = Milestones[Data.stageMilestones[Data.stage]];
            float milestoneHeight = Mathf.Round(GoalHeight * milestone.part);
            if (height < milestoneHeight)
                break;

            int bonus = milestone.bonus * stageNumber;
            money += bonus;
            text += "First time past " + milestoneHeight + " m: +$" + bonus + "\n";
            Data.stageMilestones[Data.stage]++;
        }
        return money;
    }

    // ---------- Quick buttons on the results screen (no walking back across the yard) ----------

    public void FlyAgain()
    {
        BackToYard();
        BoardRocket();
    }

    public void BuildFromResults()
    {
        BackToYard();
        EnterBuild();
    }

    public void ShopFromResults()
    {
        BackToYard();
        OpenShop();
    }

    // ---------- Reaching the goal ----------

    public bool OnLastStage => Data.stage >= Stages.Count - 1;

    public void ReachGoal()
    {
        if (State != GameState.Flight)
            return;

        Rocket.Stop();
        Data.money += PayMilestones(GoalHeight, out _);
        Data.money += MeterMoney(GoalHeight) + Flight.CoinMoney + goalBonus * (Data.stage + 1);
        Data.flights++;
        Data.Best = Mathf.Max(Data.Best, GoalHeight);
        Data.stagesCleared = Mathf.Max(Data.stagesCleared, Data.stage + 1);
        Data.Save();

        SetState(GameState.Won);
        UI.ShowWin();
        Sfx.Play("win");
    }

    // Goes on to the next stage: the Play scene loads again with the new ground and sky
    public void NextStage()
    {
        if (OnLastStage)
        {
            BackToYard();
            return;
        }
        GoToStage(Data.stage + 1);
    }

    void GoToStage(int stage)
    {
        Data.stage = Mathf.Clamp(stage, 0, Stages.Count - 1);
        Data.Save();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToYard()
    {
        if (State != GameState.Results && State != GameState.Won)
            return;

        Flight.Clear();
        Rocket.PlaceOnPad(Pad);
        Rocket.Rebuild(Data, false);

        Vector2 padPosition = Pad.transform.position;
        Player.StandUp(new Vector2(padPosition.x - LaunchPad.Width * 0.5f - 1f, padPosition.y + 0.8f));

        SetState(GameState.Yard);
        Cam.Snap();
    }

    // ---------- Pause and menu ----------

    public void SetPaused(bool paused)
    {
        Paused = paused;
        Time.timeScale = paused ? 0f : 1f;
        UI.ShowPause(paused);
        AudioListener.pause = paused;
    }

    public void GoToMenu()
    {
        Data.Save();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(Settings.MenuScene);
    }

    // ---------- Shop ----------

    // Each part costs a bit more for every one you already own
    public int PriceOf(PartType type)
    {
        PartInfo info = PartCatalog.Get(type);
        return Mathf.RoundToInt(info.price * Mathf.Pow(info.priceGrowth, Data.Owned(type)));
    }

    public void BuyPart(PartType type)
    {
        PartInfo info = PartCatalog.Get(type);
        int price = PriceOf(type);
        if (Data.money < price)
        {
            Sfx.Play("error");
            return;
        }

        Data.money -= price;
        Data.owned[(int)type]++;
        Data.Save();
        Sfx.Play("buy");
        UI.Say("Bought: " + info.name, 1.5f);
        UI.RefreshShop();
    }

    public void BuyUpgrade(UpgradeType type)
    {
        UpgradeInfo upgrade = PartCatalog.Get(type);
        int level = Data.Level(type);
        if (level >= upgrade.prices.Length)
            return;

        int price = upgrade.prices[level];
        if (Data.money < price)
        {
            Sfx.Play("error");
            return;
        }

        Data.money -= price;
        Data.upgrades[(int)type]++;
        Data.Save();

        Sfx.Play("buy");
        UI.Say(upgrade.name + " is now level " + (level + 1) + "!", 2f);
        UI.RefreshShop();

        // Some upgrades change the rocket on the pad (tougher crates, bigger pad)
        if (State == GameState.Shop || State == GameState.Yard)
            Rocket.Rebuild(Data, false);
    }

    // ---------- Dev tools (dev mode only) ----------

    public void DevAddMoney(int amount)
    {
        Data.money += amount;
        Data.Save();
        UI.Say("DEV: +$" + amount, 1.5f);
    }

    public void DevGiveParts()
    {
        for (int i = 0; i < Data.owned.Length; i++)
            Data.owned[i] += i == (int)PartType.Seat ? (Data.owned[i] == 0 ? 1 : 0) : 10;
        Data.Save();
        UI.Say("DEV: +10 of every part", 1.5f);
        if (State == GameState.Build)
            UI.RefreshBuildBar();
    }

    public void DevMaxUpgrades()
    {
        for (int i = 0; i < Data.upgrades.Length; i++)
            Data.upgrades[i] = PartCatalog.Upgrades[i].prices.Length;
        Data.Save();
        if (State == GameState.Yard)
            Rocket.Rebuild(Data, false);
        UI.Say("DEV: all upgrades maxed", 1.5f);
    }

    public void DevSetStage(int stage)
    {
        Data.stagesCleared = Mathf.Max(Data.stagesCleared, stage);
        GoToStage(stage);
    }

    public void DevToggleGod()
    {
        GodMode = !GodMode;
        UI.Say("DEV: god mode " + (GodMode ? "ON" : "OFF"), 1.5f);
    }

    public void DevToggleInfiniteCola()
    {
        InfiniteCola = !InfiniteCola;
        UI.Say("DEV: infinite cola " + (InfiniteCola ? "ON" : "OFF"), 1.5f);
    }

    // ---------- Testing ----------

    void ResetProgress()
    {
        GameSave.Delete();
        Data = GameSave.Load();
        // Back to stage 1 with a fresh yard
        GoToStage(0);
    }
}
