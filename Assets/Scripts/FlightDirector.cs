/*********************************************************************************************
 * COMPONENT OF: Rocketee Game (added by the game)
 * REQUIRED DEPENDENCIES: RocketeeGame, Rocket, Meteor, SkyCoin, sprites cloud_1-3 and moon
 * DESCRIPTION: Everything that happens during a flight. It puts meteors and coins above you
 *              (more meteors the higher you go), places the clouds and the Moon, and decides
 *              when the flight is over: when you crash, when you run out of cola and start
 *              falling, or when you reach the Moon. (The sky colors, stars, sun, planets and
 *              background hills are in Scenery.)
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.1
 * VERSION 1.1: Easier: meteors start higher and are farther apart, coins are worth more.
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlightDirector : MonoBehaviour
{
    [Header("Meteors")]
    // Meters between meteors where they start, and near the Moon
    public float meteorGapLow = 18f;
    public float meteorGapHigh = 6f;
    // How far left and right of the rocket meteors appear
    public float spawnWidth = 9f;

    [Header("Coins")]
    public float coinGap = 16f;

    public float Height { get; private set; }
    public float MaxHeight { get; private set; }
    public int CoinMoney { get; private set; }
    public int CoinCount { get; private set; }
    public bool Active { get; private set; }

    private RocketeeGame game;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private float nextMeteor;
    private float nextCoin;
    private float endTimer;
    private float flightTime;

    void Start()
    {
        game = RocketeeGame.Instance;
        MakeClouds();
        MakeMoon();
    }

    public void Begin()
    {
        Clear();
        Active = true;
        Height = 0f;
        MaxHeight = 0f;
        CoinMoney = 0;
        CoinCount = 0;
        endTimer = 0f;
        flightTime = 0f;
        nextMeteor = game.firstMeteorHeight;
        nextCoin = 12f;
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

        if (Height >= game.moonHeight)
        {
            Active = false;
            game.ReachMoon();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && !game.Paused)
        {
            Finish(false);
            return;
        }

        CheckEnd(rocket);
    }

    // Puts meteors and coins a little above the rocket before you get there
    void SpawnAhead(Rocket rocket)
    {
        float lookAhead = Height + 16f;
        float x = rocket.transform.position.x;

        while (nextMeteor < lookAhead && nextMeteor < game.moonHeight - 10f)
        {
            float t = Mathf.InverseLerp(game.firstMeteorHeight, game.moonHeight, nextMeteor);
            int size = Random.value < Mathf.Lerp(0.05f, 0.3f, t) ? 3 : Random.value < 0.5f ? 2 : 1;
            Vector2 spot = new Vector2(x + Random.Range(-spawnWidth, spawnWidth), rocket.LaunchHeight + nextMeteor);
            spawned.Add(Meteor.Spawn(spot, size).gameObject);
            nextMeteor += Mathf.Lerp(meteorGapLow, meteorGapHigh, t) * Random.Range(0.7f, 1.3f);
        }

        while (nextCoin < lookAhead)
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

        for (float height = 6f; height < 260f; height += Random.Range(5f, 11f))
        {
            int pick = Random.Range(0, names.Length);
            Sprite sprite = SpriteLib.Get(names[pick], Color.white, widths[pick], heights[pick]);
            SpriteRenderer cloud = SpriteLib.Show(holder, "Cloud", sprite, SpriteLib.UnitSize(sprite), -50);
            cloud.transform.position = new Vector3(centerX + Random.Range(-25f, 25f), game.GroundY + height, 0f);
            cloud.color = new Color(1f, 1f, 1f, 0.9f);
        }
    }

    void MakeMoon()
    {
        float centerX = game.Pad != null ? game.Pad.transform.position.x : 0f;
        Sprite sprite = SpriteLib.Get("moon", new Color(0.8f, 0.8f, 0.78f), 256, 256);
        SpriteRenderer moon = SpriteLib.Show(null, "Moon", sprite, Vector2.one * 8f, -40);
        moon.transform.position = new Vector3(centerX, game.GroundY + game.moonHeight + 5f, 0f);
    }
}
