/*********************************************************************************************
 * COMPONENT OF: Rocketee Game (added by the game)
 * REQUIRED DEPENDENCIES: RocketeeGame, Rocket, Meteor, SkyCoin, sprites cloud_1-3 and moon
 * DESCRIPTION: Everything that happens in the sky. While you fly it puts meteors and coins
 *              above you (more meteors the higher you go), changes the sky from blue to
 *              black space with stars, and decides when the flight is over: when you crash,
 *              when you run out of cola and start falling, or when you reach the Moon.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlightDirector : MonoBehaviour
{
    [Header("Meteors")]
    // Meters between meteors where they start, and near the Moon
    public float meteorGapLow = 14f;
    public float meteorGapHigh = 4f;
    // How far left and right of the rocket meteors appear
    public float spawnWidth = 9f;

    [Header("Coins")]
    public float coinGap = 18f;

    [Header("Sky colors")]
    public Color groundSky = new Color(0.55f, 0.8f, 0.98f);
    public Color highSky = new Color(0.22f, 0.4f, 0.8f);
    public Color spaceSky = new Color(0.02f, 0.02f, 0.06f);

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
    private Transform stars;
    private readonly List<SpriteRenderer> starDots = new List<SpriteRenderer>();

    void Start()
    {
        game = RocketeeGame.Instance;
        MakeClouds();
        MakeMoon();
        MakeStars();
        UpdateSky(0f);
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
        nextMeteor = game.meteorStartHeight;
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
        {
            UpdateSky(0f);
            return;
        }

        flightTime += Time.deltaTime;
        Height = rocket.Height;
        MaxHeight = Mathf.Max(MaxHeight, Height);
        UpdateSky(Height);

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
            float t = Mathf.InverseLerp(game.meteorStartHeight, game.moonHeight, nextMeteor);
            int size = Random.value < Mathf.Lerp(0.1f, 0.4f, t) ? 3 : Random.value < 0.5f ? 2 : 1;
            Vector2 spot = new Vector2(x + Random.Range(-spawnWidth, spawnWidth), rocket.LaunchHeight + nextMeteor);
            spawned.Add(Meteor.Spawn(spot, size).gameObject);
            nextMeteor += Mathf.Lerp(meteorGapLow, meteorGapHigh, t) * Random.Range(0.6f, 1.4f);
        }

        while (nextCoin < lookAhead)
        {
            int value = 5 + Mathf.FloorToInt(nextCoin / 100f) * 2;
            int count = Random.Range(1, 4);
            float startX = x + Random.Range(-5f, 5f);
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

    // ---------- Sky ----------

    void UpdateSky(float height)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Color sky;
        if (height < 200f)
            sky = Color.Lerp(groundSky, highSky, height / 200f);
        else
            sky = Color.Lerp(highSky, spaceSky, Mathf.InverseLerp(200f, 450f, height));

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = sky;

        float starAlpha = Mathf.InverseLerp(250f, 450f, height);
        if (stars != null)
        {
            stars.gameObject.SetActive(starAlpha > 0f);
            foreach (SpriteRenderer dot in starDots)
                dot.color = new Color(1f, 1f, 1f, starAlpha);
        }
    }

    // Stars move with the camera, so they always fill the screen in space
    void MakeStars()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        stars = new GameObject("Stars").transform;
        stars.SetParent(cam.transform, false);
        stars.localPosition = new Vector3(0f, 0f, 9f);

        Sprite dot = SpriteLib.Circle(4, Color.white);
        for (int i = 0; i < 90; i++)
        {
            SpriteRenderer star = SpriteLib.Show(stars, "Star", dot, Vector2.one * Random.Range(0.06f, 0.14f), -60);
            star.transform.localPosition = new Vector3(Random.Range(-16f, 16f), Random.Range(-10f, 10f), 0f);
            starDots.Add(star);
        }
        stars.gameObject.SetActive(false);
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
            cloud.transform.position = new Vector3(centerX + Random.Range(-16f, 16f), game.GroundY + height, 0f);
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
