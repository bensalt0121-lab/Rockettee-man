/*********************************************************************************************
 * COMPONENT OF: Rocketee Game (added by the game)
 * REQUIRED DEPENDENCIES: RocketeeGame, Rocket, Meteor, SkyCoin, Stages, sprites cloud_1-3 and
 *                        the goal sprite of each stage (moon, mars, sun_big)
 * DESCRIPTION: Everything that happens during a flight in the current stage. It puts meteors
 *              (and fireballs near the Sun), space junk and coins in your way, places the
 *              clouds and the goal (Moon, Mars or Sun), and decides when the flight is over:
 *              when you crash, when you run out of cola and start falling, or when you reach
 *              the goal. (Sky colors, stars, sun, planets and hills are in Scenery.)
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.2
 * VERSION 1.1: Easier: meteors start higher and are farther apart, coins are worth more.
 * VERSION 1.2: Stages: each stage has its own goal, meteor storm, space junk and fireballs.
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlightDirector : MonoBehaviour
{
    // How far left and right of the rocket meteors appear
    public float spawnWidth = 9f;
    public float coinGap = 16f;

    public float Height { get; private set; }
    public float MaxHeight { get; private set; }
    public int CoinMoney { get; private set; }
    public int CoinCount { get; private set; }
    public bool Active { get; private set; }

    private RocketeeGame game;
    private StageInfo stage;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private float nextMeteor;
    private float nextCoin;
    private float nextJunk;
    private float endTimer;
    private float flightTime;

    void Start()
    {
        game = RocketeeGame.Instance;
        stage = game.Data.Stage;
        if (stage.clouds)
            MakeClouds();
        MakeGoal();
    }

    public void Begin()
    {
        Clear();
        stage = game.Data.Stage;
        Active = true;
        Height = 0f;
        MaxHeight = 0f;
        CoinMoney = 0;
        CoinCount = 0;
        endTimer = 0f;
        flightTime = 0f;
        nextMeteor = stage.firstMeteor;
        nextCoin = 12f;
        nextJunk = stage.junkGap > 0f ? stage.firstMeteor + stage.junkGap : float.MaxValue;
    }

    // Removes this flight's meteors and coins
    public void Clear()
    {
        foreach (GameObject go in spawned)
        {
            if (go != null)
                Destroy(go);
        }
        spawned.Clear();
        Active = false;
    }

    public void AddCoin(int value)
    {
        CoinMoney += value;
        CoinCount++;
    }

    void Update()
    {
        Rocket rocket = game != null ? game.Rocket : null;
        if (!Active || rocket == null)
            return;

        flightTime += Time.deltaTime;
        Height = rocket.Height;
        MaxHeight = Mathf.Max(MaxHeight, Height);

        SpawnAhead(rocket);

        // A crashed rocket that coasts past the goal does not count
        if (Height >= stage.goalHeight && !rocket.Crashed)
        {
            Active = false;
            game.ReachGoal();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && !game.Paused)
        {
            Finish(false);
            return;
        }

        CheckEnd(rocket);
    }

    // Puts meteors, junk and coins a little above the rocket before you get there
    void SpawnAhead(Rocket rocket)
    {
        float lookAhead = Height + 16f;
        float x = rocket.transform.position.x;
        float goal = stage.goalHeight;

        while (nextMeteor < lookAhead && nextMeteor < goal - 10f)
        {
            float t = Mathf.InverseLerp(stage.firstMeteor, goal, nextMeteor);
            Vector2 spot = new Vector2(x + Random.Range(-spawnWidth, spawnWidth), rocket.LaunchHeight + nextMeteor);

            if (Random.value < stage.fireballChance)
            {
                spawned.Add(Meteor.SpawnFireball(spot).gameObject);
            }
            else
            {
                int size = Random.value < Mathf.Lerp(0.05f, stage.bigMeteorChance, t) ? 3 : Random.value < 0.5f ? 2 : 1;
                spawned.Add(Meteor.Spawn(spot, size).gameObject);
            }
            nextMeteor += Mathf.Lerp(stage.meteorGapLow, stage.meteorGapHigh, t) * Random.Range(0.7f, 1.3f);
        }

        // Space junk comes in from the side of the screen
        while (nextJunk < lookAhead && nextJunk < goal - 10f)
        {
            float direction = Random.value < 0.5f ? -1f : 1f;
            Vector2 spot = new Vector2(x - direction * 14f, rocket.LaunchHeight + nextJunk);
            spawned.Add(Meteor.SpawnJunk(spot, direction).gameObject);
            nextJunk += stage.junkGap * Random.Range(0.6f, 1.4f);
        }

        while (nextCoin < lookAhead && nextCoin < goal)
        {
            // Coins are worth more the higher they are
            int value = 6 + Mathf.FloorToInt(nextCoin / 100f) * 3;
            int count = Random.Range(2, 5);
            float startX = x + Random.Range(-4f, 4f);
            for (int i = 0; i < count; i++)
            {
                Vector2 spot = new Vector2(startX + i * 0.9f, rocket.LaunchHeight + nextCoin);
                spawned.Add(SkyCoin.Spawn(spot, value).gameObject);
            }
            nextCoin += coinGap * Random.Range(0.7f, 1.3f);
        }
    }

    void CheckEnd(Rocket rocket)
    {
        bool over = false;
        float wait = 1.5f;

        if (rocket.Crashed)
        {
            over = true;
            wait = 2.5f;
        }
        else if (rocket.transform.position.y < game.GroundY - 5f)
        {
            // Fell below the ground somehow
            over = true;
        }
        else if (!rocket.HasFuel && rocket.Body.linearVelocity.y < -0.5f)
        {
            // Out of cola and falling back down
            over = true;
        }
        else if (!rocket.HasFuel && flightTime > 2f && rocket.Body.linearVelocity.magnitude < 0.1f)
        {
            // Out of cola and not moving (landed or never took off)
            over = true;
        }

        if (!over)
        {
            endTimer = 0f;
            return;
        }

        endTimer += Time.deltaTime;
        if (endTimer >= wait)
            Finish(rocket.Crashed);
    }

    void Finish(bool crashed)
    {
        Active = false;
        game.EndFlight(MaxHeight, CoinMoney, CoinCount, crashed);
    }

    void MakeClouds()
    {
        Transform holder = new GameObject("Clouds").transform;
        float centerX = game.Pad != null ? game.Pad.transform.position.x : 0f;
        string[] names = { "cloud_1", "cloud_2", "cloud_3" };
        int[] widths = { 64, 96, 48 };
        int[] heights = { 32, 40, 24 };
        // Mars has thin orange dust clouds
        Color tint = game.Data.stage == 0 ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 0.75f, 0.55f, 0.6f);

        for (float height = 6f; height < 260f; height += Random.Range(5f, 11f))
        {
            int pick = Random.Range(0, names.Length);
            Sprite sprite = SpriteLib.Get(names[pick], Color.white, widths[pick], heights[pick]);
            SpriteRenderer cloud = SpriteLib.Show(holder, "Cloud", sprite, SpriteLib.UnitSize(sprite), -50);
            cloud.transform.position = new Vector3(centerX + Random.Range(-25f, 25f), game.GroundY + height, 0f);
            cloud.color = tint;
        }
    }

    // The Moon, Mars or the Sun, waiting at the top
    void MakeGoal()
    {
        float centerX = game.Pad != null ? game.Pad.transform.position.x : 0f;
        Sprite sprite = SpriteLib.Get(stage.goalSprite, new Color(0.8f, 0.8f, 0.78f), 256, 256);
        SpriteRenderer goal = SpriteLib.Show(null, "Goal - " + stage.goalName, sprite, Vector2.one * stage.goalSize, -40);
        goal.transform.position = new Vector3(centerX, game.GroundY + stage.goalHeight + stage.goalSize * 0.6f, 0f);
    }
}
