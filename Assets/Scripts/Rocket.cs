/*********************************************************************************************
 * COMPONENT OF: Rocket (made by the game)
 * REQUIRED DEPENDENCIES: Rigidbody 2D, RocketPart, RocketeeGame, LaunchPad, Stages,
 *                        Input System package
 * DESCRIPTION: The rocket you built on the grid. Every part gets its own collider and weight,
 *              so the rocket balances like a real object: put the colas on one side and it
 *              spins. In flight, hold Space to spray all colas (and Mentos) that still have
 *              fuel, A/D to tilt; let go of A/D and it straightens itself.
 *              - Crates next to a cola give it spare cola; tanks feed all colas when their
 *                own bottle is empty.
 *              - Fins and wings stop spinning, wings add lift, a nose cone cuts air drag.
 *              - The bubble shield blocks one hit and recharges; the coin magnet pulls coins.
 *              - High up the air gets thin and gravity weaker. Each stage has its own gravity.
 *              - Near the Sun the rocket heats up: heat shields slow it down, colas and tanks
 *                boil away unless a cooler touches them, and at full heat you crash.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.2
 * VERSION 1.1: Easier: self-straightening, tilting feels the same for big and small rockets,
 *              wind sound.
 * VERSION 1.2: Crate fuel bonus, tanks, heat, coolers, bubble shield, coin magnet, wings,
 *              Mentos, stage gravity, steering upgrade, god mode and infinite cola (dev mode).
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Rocket : MonoBehaviour
{
    [Header("Flying")]
    // How fast A/D turns the rocket
    public float tiltPower = 4f;
    // How strongly it turns back upright when you let go of A/D (0 = not at all)
    public float selfRight = 5f;
    public float selfRightDamping = 4.5f;
    // Air drag: the rocket has a top speed
    public float airDrag = 0.4f;
    public float spinDrag = 2f;

    [Header("Space (parts of the way to the goal)")]
    // The air gets thinner between these parts of the way (0.15 = 15%)
    public float thinAirStart = 0.15f;
    public float thinAirEnd = 0.5f;
    // How much drag is left in thin air (0.5 = half)
    public float thinAirDrag = 0.5f;
    // Gravity gets weaker between these parts of the way
    public float spaceStart = 0.42f;
    public float spaceEnd = 0.85f;
    public float spaceGravity = 0.68f;

    [Header("Heat (stage 3)")]
    // Heat gained per second near the start and near the Sun (1 = cooked)
    public float heatLow = 0.04f;
    public float heatHigh = 0.12f;
    // Heat at which colas start to boil
    public float boilHeat = 0.4f;
    // Seconds of cola lost per second at full heat when no cooler touches it
    public float boilRate = 0.8f;

    public Rigidbody2D Body { get; private set; }
    public readonly List<RocketPart> Parts = new List<RocketPart>();
    public RocketPart Seat { get; private set; }
    public bool Flying { get; private set; }
    public bool Crashed { get; private set; }
    public string CrashReason { get; private set; } = "";
    public bool Spraying { get; private set; }
    public float LaunchHeight { get; private set; }
    // 0 to 1 (stage 3 only)
    public float Heat { get; private set; }
    public bool ShieldReady => shieldParts > 0 && shieldTimer <= 0f;
    public float MagnetRange { get; private set; }

    private SpriteRenderer pilot;
    private SpriteRenderer bubble;
    private AudioSource sprayLoop;
    private AudioSource windLoop;
    private AudioSource fireLoop;
    private float tiltInput;
    private bool sprayHeld;
    private int shieldParts;
    private float shieldRecharge;
    private float shieldTimer;
    private bool alarmPlayed;

    public static Rocket Create()
    {
        GameObject go = new GameObject("Rocket");
        return go.AddComponent<Rocket>();
    }

    void Awake()
    {
        Body = gameObject.AddComponent<Rigidbody2D>();
        Body.bodyType = RigidbodyType2D.Kinematic;
        Body.useAutoMass = true;
        Body.interpolation = RigidbodyInterpolation2D.Interpolate;
        Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        sprayLoop = MakeLoop("spray_loop");
        windLoop = MakeLoop("wind_loop");
        fireLoop = MakeLoop("fire_loop");
    }

    AudioSource MakeLoop(string clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = Sfx.Get(clip);
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        return source;
    }

    static RocketeeGame Game => RocketeeGame.Instance;

    // ---------- Building ----------

    // Puts the rocket back on the launch pad, standing up, not moving
    public void PlaceOnPad(LaunchPad pad)
    {
        Flying = false;
        Crashed = false;
        CrashReason = "";
        Spraying = false;
        Heat = 0f;
        Body.bodyType = RigidbodyType2D.Kinematic;
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0f;
        // Moves the physics body too, so smoothing can't put it back where it was
        Body.position = pad.GridOrigin;
        Body.rotation = 0f;
        transform.SetPositionAndRotation(pad.GridOrigin, Quaternion.identity);
        StopSounds();
    }

    // Makes the parts again from the saved layout.
    // flyingOnly = true leaves out parts that do not touch the seat.
    public void Rebuild(GameSave save, bool flyingOnly)
    {
        foreach (RocketPart part in Parts)
        {
            if (part == null)
                continue;
            // Turned off right away so its collider and weight are gone this frame
            part.gameObject.SetActive(false);
            Destroy(part.gameObject);
        }
        Parts.Clear();
        Seat = null;
        pilot = null;
        if (bubble != null)
            Destroy(bubble.gameObject);
        bubble = null;

        HashSet<Vector2Int> connected = ConnectedCells(save.layout);

        foreach (PlacedPart placed in save.layout)
        {
            Vector2Int cell = new Vector2Int(placed.x, placed.y);
            bool loose = !connected.Contains(cell);
            if (loose && flyingOnly)
                continue;

            RocketPart part = MakePart(PartCatalog.Get(placed.type), cell, save);
            part.SetLoose(loose);
            Parts.Add(part);

            if (placed.type == PartType.Seat)
                Seat = part;
        }

        if (Seat != null)
        {
            // Small enough that the pilot's head stays inside the seat's square
            pilot = SpriteLib.Show(Seat.transform, "Pilot", Settings.PlayerSprite("sit"), new Vector2(0.75f, 1.125f), 7);
            pilot.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            pilot.color = Settings.PlayerColor;
            pilot.enabled = false;
        }

        LinkNeighbors(save, !flyingOnly);
        SetUpGadgets();
        IgnorePlayer();
    }

    RocketPart MakePart(PartInfo info, Vector2Int cell, GameSave save)
    {
        GameObject go = new GameObject("Part - " + info.name + " " + cell.x + "," + cell.y);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = CellToLocal(cell);

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.96f, 0.96f);
        // With Use Auto Mass, the rocket's weight comes from the parts
        box.density = info.mass;

        SpriteRenderer look = SpriteLib.Show(go.transform, "Look", SpriteLib.Get(info.sprite, info.color), Vector2.one, 5);

        // Fins and wings on the right side face the other way
        if ((info.type == PartType.Fin || info.type == PartType.Wing) && cell.x > SeatColumn(save))
            look.flipX = true;

        RocketPart part = go.AddComponent<RocketPart>();
        part.Setup(this, info, cell, look, save);
        return part;
    }

    // Crates give spare cola to the colas they touch (left, right or top), and coolers keep
    // the colas and tanks they touch from boiling. In the yard and build mode a green + shows
    // each crate bonus.
    void LinkNeighbors(GameSave save, bool showBadges)
    {
        Dictionary<Vector2Int, RocketPart> byCell = new Dictionary<Vector2Int, RocketPart>();
        foreach (RocketPart part in Parts)
            byCell[part.Cell] = part;

        Vector2Int[] crateSides = { Vector2Int.left, Vector2Int.right, Vector2Int.up };
        Vector2Int[] allSides = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        Sprite plus = showBadges ? SpriteLib.Plus(new Color(0.3f, 1f, 0.35f)) : null;

        foreach (RocketPart part in Parts)
        {
            if (part.IsCola)
            {
                int badges = 0;
                foreach (Vector2Int side in crateSides)
                {
                    if (!byCell.TryGetValue(part.Cell + side, out RocketPart neighbor) || neighbor.Info.fuelBonus <= 0f)
                        continue;

                    part.AddFuel(neighbor.Info.fuelBonus * save.FuelMultiplier);
                    if (plus != null)
                    {
                        SpriteRenderer badge = SpriteLib.Show(part.transform, "Crate Bonus", plus, Vector2.one * 0.3f, 6);
                        badge.transform.localPosition = new Vector3(0.32f - badges * 0.3f, 0.32f, 0f);
                        badges++;
                    }
                }
            }

            if (part.Info.HoldsCola)
            {
                foreach (Vector2Int side in allSides)
                {
                    if (byCell.TryGetValue(part.Cell + side, out RocketPart neighbor) && neighbor.Info.cools)
                        part.Cooled = true;
                }
            }
        }
    }

    // Bubble shield and coin magnet
    void SetUpGadgets()
    {
        shieldParts = 0;
        shieldRecharge = 0f;
        MagnetRange = 0f;
        foreach (RocketPart part in Parts)
        {
            if (part.Info.shieldRecharge > 0f)
            {
                shieldParts++;
                shieldRecharge = part.Info.shieldRecharge;
            }
            // Every extra magnet reaches a bit farther
            if (part.Info.magnetRange > 0f)
                MagnetRange = MagnetRange <= 0f ? part.Info.magnetRange : MagnetRange + 2f;
        }
        shieldTimer = 0f;

        if (shieldParts > 0)
        {
            Bounds bounds = PartBounds();
            float size = Mathf.Max(bounds.size.x, bounds.size.y) + 1.2f;
            bubble = SpriteLib.Show(transform, "Bubble Shield", SpriteLib.Circle(64, new Color(0.55f, 0.9f, 1f, 0.25f)), Vector2.one * size, 9);
            bubble.transform.position = bounds.center;
            bubble.enabled = false;
        }
    }

    Bounds PartBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool first = true;
        foreach (RocketPart part in Parts)
        {
            Bounds partBounds = new Bounds(part.transform.position, Vector3.one);
            if (first)
                bounds = partBounds;
            else
                bounds.Encapsulate(partBounds);
            first = false;
        }
        return bounds;
    }

    static int SeatColumn(GameSave save)
    {
        foreach (PlacedPart placed in save.layout)
        {
            if (placed.type == PartType.Seat)
                return placed.x;
        }
        return 0;
    }

    // Grid cell (x = left/right from the middle, y = rows up from the pad) to rocket position
    public static Vector2 CellToLocal(Vector2Int cell)
    {
        return new Vector2(cell.x, cell.y + 0.5f);
    }

    // Every cell that is joined to the seat through touching parts
    public static HashSet<Vector2Int> ConnectedCells(List<PlacedPart> layout)
    {
        HashSet<Vector2Int> filled = new HashSet<Vector2Int>();
        Vector2Int? seat = null;

        foreach (PlacedPart placed in layout)
        {
            Vector2Int cell = new Vector2Int(placed.x, placed.y);
            filled.Add(cell);
            if (placed.type == PartType.Seat)
                seat = cell;
        }

        HashSet<Vector2Int> connected = new HashSet<Vector2Int>();
        if (seat == null)
            return connected;

        Queue<Vector2Int> toVisit = new Queue<Vector2Int>();
        toVisit.Enqueue(seat.Value);
        connected.Add(seat.Value);

        Vector2Int[] sides = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        while (toVisit.Count > 0)
        {
            Vector2Int cell = toVisit.Dequeue();
            foreach (Vector2Int side in sides)
            {
                Vector2Int next = cell + side;
                if (filled.Contains(next) && connected.Add(next))
                    toVisit.Enqueue(next);
            }
        }
        return connected;
    }

    // The player walks through the rocket in the yard instead of bumping into it
    void IgnorePlayer()
    {
        if (Game == null || Game.Player == null || Game.Player.Collider == null)
            return;

        foreach (RocketPart part in Parts)
        {
            Collider2D collider = part.GetComponent<Collider2D>();
            if (collider != null)
                Physics2D.IgnoreCollision(collider, Game.Player.Collider, true);
        }
    }

    // ---------- Flying ----------

    // Why the rocket can't fly yet, or null if it can
    public static string ProblemWith(GameSave save)
    {
        if (save.Placed(PartType.Seat) == 0)
            return "Your rocket needs a seat! Press E at the launch pad to build.";

        HashSet<Vector2Int> connected = ConnectedCells(save.layout);
        foreach (PlacedPart placed in save.layout)
        {
            if (PartCatalog.Get(placed.type).IsEngine && connected.Contains(new Vector2Int(placed.x, placed.y)))
                return null;
        }
        return "Your rocket needs a cola touching the seat to fly!";
    }

    public void Launch(GameSave save)
    {
        Rebuild(save, true);
        LaunchHeight = transform.position.y;
        CameraRig.Shake(0.15f);
        Flying = true;
        Crashed = false;
        CrashReason = "";
        Heat = 0f;
        alarmPlayed = false;
        Body.bodyType = RigidbodyType2D.Dynamic;
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0f;
        UpdateAir();

        if (pilot != null)
            pilot.enabled = true;
        if (bubble != null)
            bubble.enabled = true;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        bool control = Flying && !Crashed && keyboard != null && Game != null && Game.State == GameState.Flight && !Game.Paused;

        sprayHeld = control && keyboard.spaceKey.isPressed;
        tiltInput = 0f;
        if (control)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                tiltInput += 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                tiltInput -= 1f;
        }

        // Bubble shield coming back
        if (shieldTimer > 0f)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f && bubble != null && Flying && !Crashed)
            {
                bubble.enabled = true;
                Sfx.Play("pickup", 0.6f);
            }
        }

        UpdateSounds();
    }

    void UpdateSounds()
    {
        float sound = Settings.SoundVolume;
        SetLoop(sprayLoop, Spraying && Flying ? 0.6f * sound : 0f, 10f);
        // Wind gets louder the faster you go
        SetLoop(windLoop, Flying ? Mathf.Clamp01(Body.linearVelocity.magnitude / 35f) * sound : 0f, 0.8f);
        // Fire crackles louder the hotter it gets
        SetLoop(fireLoop, Flying && !Crashed ? Heat * 0.8f * sound : 0f, 1f);
    }

    static void SetLoop(AudioSource source, float target, float speed)
    {
        source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime * speed);
        if (source.volume > 0.01f && !source.isPlaying && source.clip != null)
            source.Play();
        else if (source.volume <= 0.01f && source.isPlaying)
            source.Stop();
    }

    void StopSounds()
    {
        foreach (AudioSource source in new[] { sprayLoop, windLoop, fireLoop })
        {
            source.volume = 0f;
            if (source.isPlaying)
                source.Stop();
        }
    }

    void FixedUpdate()
    {
        if (!Flying)
            return;

        UpdateAir();
        Spraying = false;

        if (Crashed)
            return;

        if (sprayHeld)
            Spray();

        UpdateHeat();

        // A/D turns the rocket. With no A/D it slowly turns back upright on its own.
        // Times the rocket's inertia, so big and small rockets turn at the same speed.
        float steering = Game != null ? Game.Data.SteeringMultiplier : 1f;
        float angle = Mathf.DeltaAngle(0f, Body.rotation) * Mathf.Deg2Rad;
        float spin = Body.angularVelocity * Mathf.Deg2Rad;
        float turn = tiltInput != 0f ? tiltInput * tiltPower * steering : -angle * selfRight - spin * selfRightDamping;
        Body.AddTorque(turn * Body.inertia);
    }

    void Spray()
    {
        float push = Game != null ? Game.Data.ThrustMultiplier : 1f;
        bool infinite = Game != null && Game.InfiniteCola;
        Effects effects = Game != null ? Game.Effects : null;
        float dt = Time.fixedDeltaTime;

        foreach (RocketPart part in Parts)
        {
            if (part == null || !part.Info.IsEngine)
                continue;

            // Its own bottle first, then spare cola from a tank (Mentos can't use tanks)
            if (part.Fuel > 0f)
            {
                if (!infinite)
                    part.Fuel = Mathf.Max(0f, part.Fuel - dt);
            }
            else if (!part.IsCola || !DrinkFromTank(dt, infinite))
            {
                continue;
            }

            Body.AddForceAtPosition((Vector2)transform.up * part.Info.thrust * push, part.Nozzle);
            Spraying = true;

            if (effects != null)
            {
                Vector2 spray = -(Vector2)transform.up * Random.Range(5f, 8f) + Random.insideUnitCircle * 1.2f;
                effects.Spray(part.Nozzle, spray + Body.linearVelocity * 0.5f);
            }
        }
    }

    bool DrinkFromTank(float amount, bool infinite)
    {
        foreach (RocketPart tank in Parts)
        {
            if (tank == null || !tank.IsTank || tank.Fuel <= 0f)
                continue;
            if (!infinite)
                tank.Fuel = Mathf.Max(0f, tank.Fuel - amount);
            return true;
        }
        return false;
    }

    // Near the Sun: heat goes up, colas boil, and at full heat you are cooked
    void UpdateHeat()
    {
        StageInfo stage = Game != null ? Game.Data.Stage : null;
        if (stage == null || !stage.heat)
            return;

        float shieldCut = 0f;
        foreach (RocketPart part in Parts)
            shieldCut += part.Info.heatCut;
        shieldCut = Mathf.Min(0.85f, shieldCut);

        float dt = Time.fixedDeltaTime;
        float progress = Mathf.Clamp01(Height / stage.goalHeight);
        Heat = Mathf.Min(1f, Heat + Mathf.Lerp(heatLow, heatHigh, progress) * (1f - shieldCut) * dt);

        if (Heat >= 0.75f && !alarmPlayed)
        {
            alarmPlayed = true;
            Sfx.Play("alarm");
            Game.UI.Say("TOO HOT! Add heat shields and coolers!", 2.5f);
        }

        // Colas and tanks without a cooler boil away
        if (Heat > boilHeat && !Game.InfiniteCola)
        {
            float boil = boilRate * Mathf.InverseLerp(boilHeat, 1f, Heat) * dt;
            foreach (RocketPart part in Parts)
            {
                if (part == null || !part.Info.HoldsCola || part.Cooled || part.Fuel <= 0f)
                    continue;
                part.Fuel = Mathf.Max(0f, part.Fuel - boil);
                if (Random.value < 0.05f)
                    Game.Effects.Spray(part.transform.position + Vector3.up * 0.4f, new Vector2(Random.Range(-0.5f, 0.5f), 2f));
            }
        }

        if (Heat >= 1f && !Game.GodMode)
            Crash("You got too hot near the Sun!");
    }

    // Thinner air and weaker gravity the higher you go (measured as part of the way to the
    // goal, so it works the same in every stage). Fins, wings and the nose cone help.
    void UpdateAir()
    {
        StageInfo stage = Game != null ? Game.Data.Stage : Stages.Get(0);
        float way = Height / stage.goalHeight;
        float thinAir = Mathf.Lerp(1f, thinAirDrag, Mathf.InverseLerp(thinAirStart, thinAirEnd, way));
        float space = Mathf.InverseLerp(spaceStart, spaceEnd, way);

        float dragCut = 0f;
        float stability = 0f;
        float lift = 0f;
        foreach (RocketPart part in Parts)
        {
            if (part == null)
                continue;
            dragCut = Mathf.Max(dragCut, part.Info.dragCut);
            stability += part.Info.stability;
            lift += part.Info.lift;
        }

        Body.gravityScale = stage.gravity * Mathf.Lerp(1f, spaceGravity, space) * (1f - Mathf.Min(0.3f, lift));
        Body.linearDamping = airDrag * stage.air * (1f - dragCut) * thinAir;
        Body.angularDamping = spinDrag + stability;
    }

    // Height above where the rocket started, in units (meters)
    public float Height => Mathf.Max(0f, transform.position.y - LaunchHeight);

    public bool HasFuel
    {
        get
        {
            bool hasColaEngine = false;
            bool tankLeft = false;
            foreach (RocketPart part in Parts)
            {
                if (part == null)
                    continue;
                if (part.HasFuel)
                    return true;
                hasColaEngine |= part.IsCola;
                tankLeft |= part.IsTank && part.Fuel > 0f;
            }
            return hasColaEngine && tankLeft;
        }
    }

    // 0 to 1, for the cola meter (bottles and tanks together)
    public float FuelFraction
    {
        get
        {
            float left = 0f;
            float max = 0f;
            foreach (RocketPart part in Parts)
            {
                if (part == null || !part.Info.HoldsCola)
                    continue;
                left += part.Fuel;
                max += part.MaxFuel;
            }
            return max > 0f ? left / max : 0f;
        }
    }

    // Blocks a hit if the bubble shield is up
    public bool TryBlockHit(Vector3 where)
    {
        if (Game != null && Game.GodMode)
            return true;
        if (!ShieldReady || !Flying)
            return false;

        shieldTimer = shieldRecharge / shieldParts;
        if (bubble != null)
            bubble.enabled = false;
        Sfx.Play("hit", 0.5f);
        if (Game != null)
            Game.Effects.Burst(where, new Color(0.55f, 0.9f, 1f), 12);
        return true;
    }

    // Stops the rocket where it is (you reached the goal)
    public void Stop()
    {
        Flying = false;
        Spraying = false;
        Body.bodyType = RigidbodyType2D.Kinematic;
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0f;
        StopSounds();
    }

    // A part was destroyed
    public void LosePart(RocketPart part)
    {
        Parts.Remove(part);
        // Turned off right away so it can't be hit again this frame
        part.gameObject.SetActive(false);
        Destroy(part.gameObject);
    }

    // The pilot flies out and the rocket stops spraying
    public void Crash(string reason)
    {
        if (Crashed)
            return;

        Crashed = true;
        CrashReason = reason;
        Spraying = false;
        CameraRig.Shake(0.6f);
        Sfx.Play("explosion");
        Sfx.Play("fall", 0.8f);

        if (Game != null && Seat != null)
            Game.Effects.Burst(Seat.transform.position, new Color(1f, 0.6f, 0.2f), 24);
        if (bubble != null)
            bubble.enabled = false;

        if (pilot != null)
        {
            pilot.enabled = false;
            ThrowPilot(pilot.transform.position);
        }
    }

    // A little pilot that tumbles away after a crash
    void ThrowPilot(Vector3 position)
    {
        GameObject go = new GameObject("Falling Pilot");
        go.transform.position = position;
        SpriteRenderer look = SpriteLib.Show(go.transform, "Look", Settings.PlayerSprite("jump"), new Vector2(1f, 1.5f), 9);
        look.color = Settings.PlayerColor;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.linearVelocity = Body.linearVelocity + new Vector2(Random.Range(-3f, 3f), 7f);
        body.angularVelocity = Random.Range(-400f, 400f);
        Destroy(go, 5f);
    }
}
