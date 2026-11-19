/*********************************************************************************************
 * COMPONENT OF: none (shared data used by the whole game)
 * REQUIRED DEPENDENCIES: sprites named below in Assets/Resources/Sprites (anything missing is
 *                        left out or drawn as a colored box)
 * DESCRIPTION: The three stages. Everything works the same in every stage (yard, shop,
 *              building, flying), but each trip is longer and more dangerous:
 *              1. Earth to the Moon (1000 m): meteors
 *              2. Moon to Mars (2500 m): a meteor storm and fast space junk
 *              3. Mars to the Sun (4000 m): fireballs, and heat that makes colas boil and can
 *                 cook you (heat shields and coolers protect you)
 *              Also how each stage looks: ground tiles, background hills, sky colors and
 *              decorations.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public class StageInfo
{
    public string name;
    public string from;
    public string goalName;
    // Sprite of what you fly to (and how big it is in units)
    public string goalSprite;
    public float goalSize = 8f;
    public float goalHeight;

    // Physics
    public float gravity = 1f;
    // Gravity for jumping around in the yard (low on the Moon!)
    public float yardGravity = 1f;
    public float air = 1f;

    // Hazards
    public float firstMeteor;
    public float meteorGapLow;
    public float meteorGapHigh;
    public float bigMeteorChance;
    // Share of meteors that are fireballs instead (hit twice as hard)
    public float fireballChance;
    // Space junk flying sideways (meters between pieces, 0 = none)
    public float junkGap;
    // Heat near the Sun
    public bool heat;

    // Looks
    public string topTile;
    public string deepTile;
    public string hills;
    public Color hillsTint = Color.white;
    public bool dunes;
    public bool clouds;
    public bool earthCritters;
    public bool showCacti;
    public string skyPlanet;
    public bool starsFromGround;
    public Color farTop;
    public Color farDeep;
    public Color[] skyBottom;
    public Color[] skyTop;
    public (string sprite, float x, int w, int h, float scale)[] decorations;

    // What is new in this stage (shown when you get here)
    public string news;
}

public static class Stages
{
    // Sky color stops: at 0%, 15%, 35% and 60% of the way to the goal
    public static readonly float[] SkyStops = { 0f, 0.15f, 0.35f, 0.6f };

    public static readonly StageInfo[] All =
    {
        new StageInfo
        {
            name = "Earth to the Moon", from = "Earth", goalName = "the Moon", goalSprite = "moon", goalHeight = 1000f,
            firstMeteor = 150f, meteorGapLow = 18f, meteorGapHigh = 6f, bigMeteorChance = 0.3f,
            hills = "bg_mountains", dunes = true, clouds = true, earthCritters = true, showCacti = true,
            farTop = new Color(136f / 255f, 91f / 255f, 0f), farDeep = new Color(97f / 255f, 65f / 255f, 0f),
            skyBottom = new[] { new Color(1f, 0.86f, 0.62f), new Color(0.62f, 0.82f, 0.98f), new Color(0.2f, 0.25f, 0.55f), new Color(0.05f, 0.05f, 0.14f) },
            skyTop = new[] { new Color(0.38f, 0.68f, 0.98f), new Color(0.22f, 0.45f, 0.88f), new Color(0.05f, 0.06f, 0.2f), new Color(0.01f, 0.01f, 0.04f) },
            decorations = new[]
            {
                ("rock_1", -15.1f, 32, 16, 1.5f), ("sign_shop", -9.8f, 32, 32, 1.5f), ("sign_launch", -0.6f, 32, 32, 1.5f),
                ("rock_2", 5f, 16, 16, 1.5f), ("bush", 13.4f, 32, 24, 1.75f), ("small_cactus", 15f, 16, 32, 2f),
            },
            news = "",
        },
        new StageInfo
        {
            name = "Moon to Mars", from = "the Moon", goalName = "Mars", goalSprite = "mars", goalHeight = 2500f, goalSize = 10f,
            gravity = 0.9f, air = 0.8f, yardGravity = 0.45f,
            firstMeteor = 80f, meteorGapLow = 10f, meteorGapHigh = 4f, bigMeteorChance = 0.45f, junkGap = 70f,
            topTile = "moon_top", deepTile = "moon_deep", hills = "bg_moon_hills", skyPlanet = "earth", starsFromGround = true,
            farTop = new Color(0.62f, 0.62f, 0.66f), farDeep = new Color(0.42f, 0.42f, 0.46f),
            skyBottom = new[] { new Color(0.08f, 0.08f, 0.14f), new Color(0.05f, 0.05f, 0.12f), new Color(0.04f, 0.03f, 0.1f), new Color(0.06f, 0.02f, 0.05f) },
            skyTop = new[] { new Color(0.01f, 0.01f, 0.04f), new Color(0.01f, 0.01f, 0.04f), new Color(0.02f, 0.01f, 0.05f), new Color(0.03f, 0.01f, 0.03f) },
            decorations = new[]
            {
                ("moon_rock", -15.1f, 32, 16, 1.5f), ("sign_shop", -9.8f, 32, 32, 1.5f), ("sign_launch", -0.6f, 32, 32, 1.5f),
                ("crater", 6.5f, 48, 16, 1.5f), ("flag", 10.5f, 32, 48, 1.5f), ("moon_rock", 13.6f, 32, 16, 1.5f),
            },
            news = "Watch out for the meteor storm and space junk flying sideways!\nNew in the shop: Titanium crates and the Space Helmet.",
        },
        new StageInfo
        {
            name = "Mars to the Sun", from = "Mars", goalName = "the Sun", goalSprite = "sun_big", goalHeight = 4000f, goalSize = 14f,
            gravity = 1.1f, air = 0.7f, yardGravity = 0.6f,
            firstMeteor = 100f, meteorGapLow = 12f, meteorGapHigh = 5f, bigMeteorChance = 0.4f, fireballChance = 0.5f, junkGap = 110f, heat = true,
            topTile = "mars_top", deepTile = "mars_deep", hills = "bg_mars_hills", clouds = true,
            farTop = new Color(0.62f, 0.27f, 0.14f), farDeep = new Color(0.42f, 0.17f, 0.09f),
            skyBottom = new[] { new Color(0.95f, 0.66f, 0.45f), new Color(0.75f, 0.4f, 0.3f), new Color(0.35f, 0.12f, 0.1f), new Color(0.5f, 0.2f, 0.05f) },
            skyTop = new[] { new Color(0.78f, 0.5f, 0.38f), new Color(0.4f, 0.18f, 0.15f), new Color(0.15f, 0.05f, 0.06f), new Color(0.9f, 0.55f, 0.15f) },
            decorations = new[]
            {
                ("mars_rock", -15.1f, 32, 16, 1.5f), ("sign_shop", -9.8f, 32, 32, 1.5f), ("sign_launch", -0.6f, 32, 32, 1.5f),
                ("mars_crystal", 6.5f, 16, 24, 2f), ("mars_rock", 10.5f, 32, 16, 1.5f), ("mars_crystal", 14.2f, 16, 24, 2f),
            },
            news = "It gets HOT near the Sun: colas boil and you can overheat!\nNew in the shop: Heat Shields and Coolers. Fireballs hit twice as hard.",
        },
    };

    public static int Count => All.Length;

    public static StageInfo Get(int index)
    {
        return All[Mathf.Clamp(index, 0, All.Length - 1)];
    }
}
