/*********************************************************************************************
 * COMPONENT OF: Rocketee Game (added by the game)
 * REQUIRED DEPENDENCIES: RocketeeGame, CameraRig, Stages, sprites (bg_mountains, bg_dunes,
 *                        bg_moon_hills, bg_mars_hills, sun, moon, mars, sun_big, earth,
 *                        planet_1, planet_2, bird_1, bird_2, balloon, plane, satellite, ufo,
 *                        tumbleweed) - anything missing is just left out
 * DESCRIPTION: Makes the view more interesting, different for every stage:
 *              - a sky that changes color as you climb (desert blue on Earth, black on the
 *                Moon, dusty orange on Mars, glowing near the Sun)
 *              - hills (and dunes on Earth) behind the yard that move slower than the camera
 *                (parallax), plus ground that goes on far past the yard
 *              - the sun, the goal (Moon, Mars or Sun) in the sky that grows as you get closer,
 *                Earth seen from the Moon, planets and twinkling stars in space
 *              - things to fly past: birds, balloons and planes on Earth, satellites and a UFO
 *                higher up, tumbleweeds rolling through the desert yard
 *              Only for looks: none of it can be hit.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.1
 * VERSION 1.1: Stages (Moon and Mars look), far ground.
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;

// Runs after the camera has moved, so the background never shakes
[DefaultExecutionOrder(100)]
public class Scenery : MonoBehaviour
{
    [Header("Parallax (1 = moves with the camera, 0 = stays still)")]
    public float hillsFollow = 0.85f;
    public float dunesFollow = 0.6f;

    // Things that fly past. low/high are parts of the way to the goal (0.07 = 7%).
    class CritterKind
    {
        public string name;
        public string spriteA;
        public string spriteB;
        public float low;
        public float high;
        public int max;
        public float slowest;
        public float fastest;
        public Vector2 size;
        public bool earthOnly;
    }

    static readonly CritterKind[] Kinds =
    {
        new CritterKind { name = "Bird", spriteA = "bird_1", spriteB = "bird_2", low = 0.003f, high = 0.07f, max = 3, slowest = 2f, fastest = 3.5f, size = new Vector2(0.5f, 0.5f), earthOnly = true },
        new CritterKind { name = "Balloon", spriteA = "balloon", low = 0.025f, high = 0.14f, max = 2, slowest = 0.3f, fastest = 0.8f, size = new Vector2(1f, 1.5f), earthOnly = true },
        new CritterKind { name = "Plane", spriteA = "plane", low = 0.07f, high = 0.23f, max = 1, slowest = 5f, fastest = 7f, size = new Vector2(1.5f, 0.75f), earthOnly = true },
        new CritterKind { name = "Satellite", spriteA = "satellite", low = 0.35f, high = 0.76f, max = 2, slowest = 0.4f, fastest = 1f, size = Vector2.one },
        new CritterKind { name = "UFO", spriteA = "ufo", low = 0.65f, high = 0.98f, max = 1, slowest = 2.5f, fastest = 3.5f, size = new Vector2(1f, 0.5f) },
    };

    class Critter
    {
        public CritterKind kind;
        public SpriteRenderer look;
        public Sprite a;
        public Sprite b;
        public float speed;
        public float baseY;
        public float phase;
    }

    private RocketeeGame game;
    private StageInfo stage;
    private Camera cam;
    private Transform camTransform;
    private float startCameraY;

    private SpriteRenderer sky;
    private Texture2D skyTexture;
    private float lastSkyHeight = -999f;

    private Transform starHolder;
    private readonly List<(SpriteRenderer dot, float phase)> stars = new List<(SpriteRenderer, float)>();
    private SpriteRenderer sun;
    private SpriteRenderer skyGoal;
    private SpriteRenderer skyPlanet;
    private readonly List<(SpriteRenderer look, Vector2 spot)> planets = new List<(SpriteRenderer, Vector2)>();

    private SpriteRenderer hills;
    private SpriteRenderer dunes;

    private readonly List<Critter> critters = new List<Critter>();
    private Transform critterHolder;
    private Sprite tumbleweedSprite;
    private float tumbleweedTimer = 4f;

    void Start()
    {
        game = RocketeeGame.Instance;
        stage = game.Data.Stage;
        cam = Camera.main;
        if (cam == null)
            return;

        camTransform = cam.transform;
        startCameraY = game.GroundY + game.Cam.yardHeight;
        critterHolder = new GameObject("Flying Things").transform;

        MakeSky();
        MakeStars();
        MakeSunGoalAndPlanets();
        MakeHills();
        MakeFarGround();

        if (stage.earthCritters && SpriteLib.Has("tumbleweed"))
            tumbleweedSprite = SpriteLib.Get("tumbleweed", Color.gray, 24, 24);
    }

    // How high the camera is above where it starts in the yard
    float ViewHeight => Mathf.Max(0f, camTransform.position.y - startCameraY);
    float HalfHeight => cam.orthographicSize;
    float HalfWidth => cam.orthographicSize * cam.aspect;

    void LateUpdate()
    {
        if (cam == null)
            return;

        float height = ViewHeight;
        UpdateSky(height);
        UpdateSpace(height);
        UpdateHills();
        UpdateCritters(height);
    }

    // ---------- Sky ----------

    void MakeSky()
    {
        skyTexture = new Texture2D(1, 32, TextureFormat.RGBA32, false);
        skyTexture.wrapMode = TextureWrapMode.Clamp;
        skyTexture.filterMode = FilterMode.Bilinear;

        Sprite sprite = Sprite.Create(skyTexture, new Rect(0, 0, 1, 32), new Vector2(0.5f, 0.5f), 1f);
        sky = SpriteLib.Show(camTransform, "Sky", sprite, Vector2.one, -100);
        sky.transform.localPosition = new Vector3(0f, 0f, 30f);
    }

    void UpdateSky(float height)
    {
        SpriteLib.Fit(sky, new Vector2(HalfWidth * 2f + 2f, HalfHeight * 2f + 2f));

        if (Mathf.Abs(height - lastSkyHeight) < 0.5f)
            return;
        lastSkyHeight = height;

        float way = height / stage.goalHeight;
        Color bottom = SkyColor(stage.skyBottom, way);
        Color top = SkyColor(stage.skyTop, way);
        for (int y = 0; y < 32; y++)
            skyTexture.SetPixel(0, y, Color.Lerp(bottom, top, y / 31f));
        skyTexture.Apply();

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = top;
    }

    static Color SkyColor(Color[] colors, float way)
    {
        float[] stops = Stages.SkyStops;
        for (int i = 0; i < stops.Length - 1; i++)
        {
            if (way <= stops[i + 1])
                return Color.Lerp(colors[i], colors[i + 1], Mathf.InverseLerp(stops[i], stops[i + 1], way));
        }
        return colors[colors.Length - 1];
    }

    // ---------- Stars, sun, goal, planets ----------

    void MakeStars()
    {
        starHolder = new GameObject("Stars").transform;
        starHolder.SetParent(camTransform, false);
        starHolder.localPosition = new Vector3(0f, 0f, 25f);

        Sprite dot = SpriteLib.Circle(4, Color.white);
        for (int i = 0; i < 110; i++)
        {
            SpriteRenderer star = SpriteLib.Show(starHolder, "Star", dot, Vector2.one * Random.Range(0.06f, 0.16f), -90);
            star.transform.localPosition = new Vector3(Random.Range(-18f, 18f), Random.Range(-11f, 11f), 0f);
            stars.Add((star, Random.value * 10f));
        }
    }

    void MakeSunGoalAndPlanets()
    {
        // The small sun in the corner (not in the stage that flies to the Sun itself)
        if (SpriteLib.Has("sun") && stage.goalSprite != "sun_big")
            sun = SpriteLib.Show(camTransform, "Sun", SpriteLib.Get("sun", Color.yellow, 48, 48), Vector2.one * 1.6f, -85);

        skyGoal = SpriteLib.Show(camTransform, "Sky Goal", SpriteLib.Get(stage.goalSprite, new Color(0.8f, 0.8f, 0.78f), 256, 256), Vector2.one, -82);

        // Earth seen from the Moon
        if (stage.skyPlanet != null && SpriteLib.Has(stage.skyPlanet))
            skyPlanet = SpriteLib.Show(camTransform, "Sky Planet", SpriteLib.Get(stage.skyPlanet, Color.blue, 64, 64), Vector2.one * 1.8f, -83);

        if (SpriteLib.Has("planet_1"))
            planets.Add((SpriteLib.Show(camTransform, "Planet", SpriteLib.Get("planet_1", Color.gray, 64, 64), Vector2.one * 2.6f, -80), new Vector2(0.45f, 0.25f)));
        if (SpriteLib.Has("planet_2"))
            planets.Add((SpriteLib.Show(camTransform, "Planet", SpriteLib.Get("planet_2", Color.gray, 48, 48), Vector2.one * 1.6f, -80), new Vector2(-0.55f, -0.15f)));
    }

    void UpdateSpace(float height)
    {
        float halfW = HalfWidth;
        float halfH = HalfHeight;
        float way = height / stage.goalHeight;

        // Stars: always on the Moon, fading in higher up elsewhere; they twinkle
        float starAlpha = stage.starsFromGround ? 1f : Mathf.InverseLerp(0.25f, 0.45f, way);
        starHolder.gameObject.SetActive(starAlpha > 0f);
        if (starAlpha > 0f)
        {
            foreach (var star in stars)
                star.dot.color = new Color(1f, 1f, 1f, starAlpha * (0.65f + 0.35f * Mathf.Sin(Time.time * 2f + star.phase)));
        }

        if (sun != null)
            sun.transform.localPosition = new Vector3(-halfW + 2.2f, halfH - 1.8f, 28f);

        // The goal grows as you get closer, until the real one comes into view
        float progress = Mathf.Clamp01(way);
        skyGoal.gameObject.SetActive(height < stage.goalHeight - 25f);
        SpriteLib.Fit(skyGoal, Vector2.one * Mathf.Lerp(0.7f, 3.4f, Mathf.Pow(progress, 1.6f)));
        skyGoal.transform.localPosition = new Vector3(halfW - 2.6f, halfH - 2.3f, 27f);

        // Earth sinks out of view as you fly away
        if (skyPlanet != null)
            skyPlanet.transform.localPosition = new Vector3(-halfW + 3f, halfH - 2.5f - way * halfH * 1.5f, 27f);

        // Planets show up in space and drift down slowly as you climb
        float planetAlpha = stage.starsFromGround ? Mathf.InverseLerp(0.1f, 0.3f, way) : Mathf.InverseLerp(0.38f, 0.62f, way);
        foreach (var planet in planets)
        {
            planet.look.gameObject.SetActive(planetAlpha > 0f);
            planet.look.color = new Color(1f, 1f, 1f, planetAlpha);
            float drift = (way - 0.5f) * 4f;
            planet.look.transform.localPosition = new Vector3(planet.spot.x * halfW, planet.spot.y * halfH - drift, 26f);
        }
    }

    // ---------- Background hills (parallax) and far ground ----------

    void MakeHills()
    {
        hills = MakeTiledLayer(stage.hills, "Far Hills", -48);
        if (hills != null)
            hills.color = stage.hillsTint;
        if (stage.dunes)
            dunes = MakeTiledLayer("bg_dunes", "Dunes", -46);
    }

    SpriteRenderer MakeTiledLayer(string spriteName, string objectName, int order)
    {
        if (spriteName == null || !SpriteLib.Has(spriteName))
            return null;

        Sprite sprite = SpriteLib.Get(spriteName, Color.gray);
        GameObject go = new GameObject(objectName);
        SpriteRenderer layer = go.AddComponent<SpriteRenderer>();
        layer.sprite = sprite;
        layer.sortingOrder = order;
        layer.drawMode = SpriteDrawMode.Tiled;
        layer.tileMode = SpriteTileMode.Continuous;

        // The sprite repeats side by side across a very wide strip
        Vector2 unit = SpriteLib.UnitSize(sprite);
        Vector3 scale = new Vector3(unit.x / sprite.bounds.size.x, unit.y / sprite.bounds.size.y, 1f);
        go.transform.localScale = scale;
        layer.size = new Vector2(800f / scale.x, sprite.bounds.size.y);
        return layer;
    }

    // Ground that goes on far past the yard (same colors as the ground tiles), so when the
    // camera zooms out in flight the yard does not look like a floating island
    void MakeFarGround()
    {
        Transform holder = new GameObject("Far Ground").transform;

        SpriteRenderer top = SpriteLib.Show(holder, "Top", SpriteLib.Solid(stage.farTop), new Vector2(2000f, 1f), -44);
        top.transform.position = new Vector3(0f, game.GroundY - 0.5f, 0f);

        SpriteRenderer deep = SpriteLib.Show(holder, "Deep", SpriteLib.Solid(stage.farDeep), new Vector2(2000f, 60f), -44);
        deep.transform.position = new Vector3(0f, game.GroundY - 1f - 30f, 0f);
    }

    void UpdateHills()
    {
        float rise = camTransform.position.y - startCameraY;
        PlaceLayer(hills, hillsFollow, game.GroundY + 0.3f + rise * 0.92f);
        PlaceLayer(dunes, dunesFollow, game.GroundY - 1.2f + rise * 0.8f);
    }

    void PlaceLayer(SpriteRenderer layer, float follow, float bottom)
    {
        if (layer == null)
            return;

        float halfHeight = SpriteLib.UnitSize(layer.sprite).y * 0.5f;
        layer.transform.position = new Vector3(camTransform.position.x * follow, bottom + halfHeight, 0f);
    }

    // ---------- Things flying past ----------

    void UpdateCritters(float height)
    {
        float camX = camTransform.position.x;
        float camY = camTransform.position.y;
        float halfW = HalfWidth;
        float halfH = HalfHeight;
        float goal = stage.goalHeight;

        // Sometimes a new one flies in from the side
        foreach (CritterKind kind in Kinds)
        {
            if (kind.earthOnly && !stage.earthCritters)
                continue;
            if (Random.value > Time.deltaTime * 0.6f)
                continue;
            if (Count(kind) >= kind.max || !SpriteLib.Has(kind.spriteA))
                continue;

            float low = Mathf.Max(game.GroundY + kind.low * goal, camY - halfH);
            float high = Mathf.Min(game.GroundY + kind.high * goal, camY + halfH);
            if (low >= high)
                continue;

            Spawn(kind, Random.value < 0.5f ? -1f : 1f, camX, halfW, Random.Range(low, high));
        }

        // A tumbleweed rolls through the desert yard now and then
        tumbleweedTimer -= Time.deltaTime;
        if (tumbleweedTimer <= 0f)
        {
            tumbleweedTimer = Random.Range(6f, 14f);
            if (tumbleweedSprite != null && height < 4f)
            {
                CritterKind roll = new CritterKind { name = "Tumbleweed", slowest = 2.5f, fastest = 4f, size = Vector2.one * 0.75f };
                Spawn(roll, Random.value < 0.5f ? -1f : 1f, camX, halfW, 0f, tumbleweedSprite);
            }
        }

        for (int i = critters.Count - 1; i >= 0; i--)
        {
            Critter critter = critters[i];
            Transform t = critter.look.transform;
            Vector3 position = t.position + Vector3.right * critter.speed * Time.deltaTime;
            critter.phase += Time.deltaTime;

            if (critter.kind.name == "Tumbleweed")
            {
                // Rolls along the ground and bounces a little
                position.y = YardGround.TopAt(position.x) + 0.38f + Mathf.Abs(Mathf.Sin(critter.phase * 4f)) * 0.35f;
                t.Rotate(0f, 0f, -critter.speed * 120f * Time.deltaTime);
            }
            else if (critter.kind.name == "Balloon" || critter.kind.name == "UFO")
            {
                position.y = critter.baseY + Mathf.Sin(critter.phase * 1.5f) * 0.3f;
            }
            else if (critter.kind.name == "Satellite")
            {
                t.Rotate(0f, 0f, 10f * Time.deltaTime);
            }

            // Birds flap their wings
            if (critter.b != null)
                critter.look.sprite = Mathf.FloorToInt(critter.phase / 0.2f) % 2 == 0 ? critter.a : critter.b;

            t.position = position;

            bool gone = Mathf.Abs(position.x - camX) > halfW + 8f || Mathf.Abs(position.y - camY) > halfH * 3f;
            if (gone)
            {
                Destroy(t.gameObject);
                critters.RemoveAt(i);
            }
        }
    }

    int Count(CritterKind kind)
    {
        int count = 0;
        foreach (Critter critter in critters)
        {
            if (critter.kind == kind)
                count++;
        }
        return count;
    }

    // Starts just outside the left or right edge of the screen and moves across
    Critter Spawn(CritterKind kind, float direction, float camX, float halfW, float y, Sprite spriteOverride = null)
    {
        Sprite a = spriteOverride != null ? spriteOverride : SpriteLib.Get(kind.spriteA, Color.gray);
        Sprite b = kind.spriteB != null && SpriteLib.Has(kind.spriteB) ? SpriteLib.Get(kind.spriteB, Color.gray) : null;

        SpriteRenderer look = SpriteLib.Show(critterHolder, kind.name, a, kind.size, -40);
        float startX = camX - direction * (halfW + 2f);
        look.transform.position = new Vector3(startX, y, 0f);
        // The pictures face right; flip the ones going left
        look.flipX = direction < 0f;

        Critter critter = new Critter
        {
            kind = kind,
            look = look,
            a = a,
            b = b,
            speed = direction * Random.Range(kind.slowest, kind.fastest),
            baseY = y,
            phase = Random.value * 5f,
        };
        critters.Add(critter);
        return critter;
    }
}
