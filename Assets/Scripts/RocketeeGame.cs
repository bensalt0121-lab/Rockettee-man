/*********************************************************************************************
 * COMPONENT OF: Rocketee Game (made automatically by SceneBoot in the Play scene)
 * REQUIRED DEPENDENCIES: a Tilemap with a Tilemap Collider 2D as the ground (a plain ground is
 *                        made if there is none), all the other Rocketee Man scripts
 * DESCRIPTION: The boss of the game. Sets up the yard (shop on the left, launch pad, free
 *              starter parts, the player), keeps the money and saved progress, and switches
 *              between walking in the yard, the shop, building, flying and the results.
 *              Game loop: build a rocket -> fly -> earn $1 per meter + coins -> buy more parts
 *              and upgrades -> fly higher -> reach the Moon.
 *              Esc pauses. F12 twice in the yard deletes all progress (for testing).
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public enum GameState { Yard, Build, Shop, Flight, Results, Won }

public class RocketeeGame : MonoBehaviour
{
    public static RocketeeGame Instance { get; private set; }

    [Header("Goal")]
    public float moonHeight = 1000f;
    public float meteorStartHeight = 120f;

    [Header("Money")]
    public int moneyPerMeter = 1;
    public int recordBonus = 20;
    public int moonBonus = 500;

    // Free parts lying in the yard: id, part, distance from the left edge of the yard
    public static readonly (string id, PartType type, float fromLeft)[] StarterParts =
    {
        ("starter_crate_1", PartType.Crate, 5.3f),
        ("starter_seat", PartType.Seat, 6.5f),
        ("starter_cola_1", PartType.Cola, 7.8f),
        ("starter_cola_2", PartType.Cola, 12.6f),
        ("starter_crate_2", PartType.Crate, 13.5f),
    };

    public GameSave Data { get; private set; }
    public GameState State { get; private set; } = GameState.Yard;
    // Frame when the state last changed. A key press that changed the state (like E) is
    // ignored by the next screen in the same frame, so it does not close right away.
    public int StateFrame { get; private set; }
    public bool JustChanged => Time.frameCount <= StateFrame;
    public float GroundY { get; private set; }
    public float YardLeft { get; private set; }
    public float YardRight { get; private set; }

    public YardPlayer Player { get; private set; }
    public LaunchPad Pad { get; private set; }
    public Rocket Rocket { get; private set; }
    public BuildMode Build { get; private set; }
    public FlightDirector Flight { get; private set; }
    public GameUI UI { get; private set; }
    public CameraRig Cam { get; private set; }
    public Effects Effects { get; private set; }

    public bool Paused { get; private set; }

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
        MusicPlayer.Play();
        UI = GameUI.Create(this);

        Time.timeScale = 1f;
        SetState(GameState.Yard);
        Cam.Snap();

        string who = Settings.CharacterName(Settings.Character);
        if (Data.flights == 0)
            UI.Say("Welcome, " + who + "! Grab the parts lying around,\nthen press E at the launch pad to build a rocket.", 7f);
        else
            UI.Say("Welcome back, " + who + "! Best flight: " + Mathf.RoundToInt(Data.bestHeight) + " m. The Moon is at " + Mathf.RoundToInt(moonHeight) + " m.", 5f);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || Paused || State != GameState.Yard || !keyboard.f12Key.wasPressedThisFrame)
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

        YardGround.Span(out float left, out float right);
        YardLeft = left;
        YardRight = right;
        GroundY = YardGround.TopAt((left + right) * 0.5f);

        MakeWall(left - 0.5f);
        MakeWall(right + 0.5f);

        // Uses the ones you placed in the scene, or makes new ones
        Pad = FindAnyObjectByType<LaunchPad>();
        if (Pad == null)
            Pad = LaunchPad.Create(left + (right - left) * 0.55f);

        if (FindAnyObjectByType<ShopStand>() == null)
            ShopStand.Create(left + 2.2f);

        if (FindAnyObjectByType<PartPickup>() == null)
            MakeStarterParts(left, right);

        Player = FindAnyObjectByType<YardPlayer>();
        if (Player == null)
            Player = YardPlayer.Create(left + 4.5f);
    }

    public static void MakeStarterParts(float left, float right)
    {
        foreach (var starter in StarterParts)
            PartPickup.Create(starter.id, starter.type, Mathf.Min(left + starter.fromLeft, right - 0.8f));
    }

    // Invisible walls so you can't walk off the edge of the yard
    void MakeWall(float x)
    {
        GameObject wall = new GameObject("Yard Wall");
        wall.transform.position = new Vector3(x, GroundY + 5f, 0f);
        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 14f);
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

        if (Data.collected.Count >= StarterParts.Length)
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

        bool record = height > Data.bestHeight + 0.5f;
        int earned = Mathf.FloorToInt(height) * moneyPerMeter + coinMoney + (record ? recordBonus : 0);

        Data.money += earned;
        Data.flights++;
        if (record)
            Data.bestHeight = height;
        Data.Save();

        SetState(GameState.Results);
        UI.ShowResults(height, coinCount, coinMoney, earned, record, crashed);
        Sfx.Play("buy");
    }

    public void ReachMoon()
    {
        if (State != GameState.Flight)
            return;

        Rocket.Stop();
        Data.money += Mathf.FloorToInt(moonHeight) * moneyPerMeter + Flight.CoinMoney + moonBonus;
        Data.flights++;
        Data.bestHeight = Mathf.Max(Data.bestHeight, moonHeight);
        Data.reachedMoon = true;
        Data.Save();

        SetState(GameState.Won);
        UI.ShowWin();
        Sfx.Play("win");
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

    // 0 = Shake It Harder, 1 = Bigger Bottles, 2 = Bigger Launch Pad
    public void BuyUpgrade(int which)
    {
        UpgradeInfo upgrade = which == 0 ? PartCatalog.ThrustUpgrade : which == 1 ? PartCatalog.FuelUpgrade : PartCatalog.GridUpgrade;
        int level = which == 0 ? Data.thrustLevel : which == 1 ? Data.fuelLevel : Data.gridLevel;
        if (level >= upgrade.prices.Length)
            return;

        int price = upgrade.prices[level];
        if (Data.money < price)
        {
            Sfx.Play("error");
            return;
        }

        Data.money -= price;
        if (which == 0)
            Data.thrustLevel++;
        else if (which == 1)
            Data.fuelLevel++;
        else
            Data.gridLevel++;
        Data.Save();

        Sfx.Play("buy");
        UI.Say(upgrade.name + " is now level " + (level + 1) + "!", 2f);
        UI.RefreshShop();
    }

    // ---------- Testing ----------

    void ResetProgress()
    {
        GameSave.Delete();
        Data = GameSave.Load();
        Rocket.Rebuild(Data, false);

        // Puts all the free starter parts back in the yard
        foreach (PartPickup pickup in FindObjectsByType<PartPickup>())
            Destroy(pickup.gameObject);
        MakeStarterParts(YardLeft, YardRight);

        UI.Say("All progress deleted. Starting over!", 3f);
    }
}
