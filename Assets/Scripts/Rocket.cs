/*********************************************************************************************
 * COMPONENT OF: Rocket (made by the game)
 * REQUIRED DEPENDENCIES: Rigidbody 2D, RocketPart, RocketeeGame, LaunchPad, Input System package
 * DESCRIPTION: The rocket you built on the grid. Every part gets its own collider and weight,
 *              so the rocket balances like a real object: put the colas on one side and it
 *              spins. In flight, hold Space to spray all colas that still have fuel, A/D to
 *              tilt. Fins stop spinning, a nose cone cuts air drag, and high up the air gets
 *              thin and gravity gets weaker (space!).
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Rocket : MonoBehaviour
{
    [Header("Flying")]
    // Turning push for A/D (times the rocket's weight)
    public float tiltTorque = 2.5f;
    // Air drag: the rocket has a top speed
    public float airDrag = 0.4f;
    public float spinDrag = 2f;

    [Header("Space")]
    // From this height the air starts to get thinner
    public float thinAirStart = 150f;
    public float thinAirEnd = 500f;
    // How much drag is left in thin air (0.5 = half)
    public float thinAirDrag = 0.5f;
    // From this height gravity starts to get weaker
    public float spaceStart = 450f;
    public float spaceEnd = 900f;
    public float spaceGravity = 0.7f;

    public Rigidbody2D Body { get; private set; }
    public readonly List<RocketPart> Parts = new List<RocketPart>();
    public RocketPart Seat { get; private set; }
    public bool Flying { get; private set; }
    public bool Crashed { get; private set; }
    public bool Spraying { get; private set; }
    public float LaunchHeight { get; private set; }

    private SpriteRenderer pilot;
    private AudioSource sprayLoop;
    private float tiltInput;
    private bool sprayHeld;

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

        sprayLoop = gameObject.AddComponent<AudioSource>();
        sprayLoop.clip = Sfx.Get("spray_loop");
        sprayLoop.loop = true;
        sprayLoop.playOnAwake = false;
        sprayLoop.volume = 0.6f;
    }

    // ---------- Building ----------

    // Puts the rocket back on the launch pad, standing up, not moving
    public void PlaceOnPad(LaunchPad pad)
    {
        Flying = false;
        Crashed = false;
        Spraying = false;
        Body.bodyType = RigidbodyType2D.Kinematic;
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0f;
        transform.SetPositionAndRotation(pad.GridOrigin, Quaternion.identity);
        if (sprayLoop.isPlaying)
            sprayLoop.Stop();
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

        // Fins on the right side face the other way
        if (info.type == PartType.Fin && cell.x > SeatColumn(save))
            look.flipX = true;

        RocketPart part = go.AddComponent<RocketPart>();
        part.Setup(this, info, cell, look, save.FuelMultiplier);
        return part;
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
        RocketeeGame game = RocketeeGame.Instance;
        if (game == null || game.Player == null || game.Player.Collider == null)
            return;

        foreach (RocketPart part in Parts)
        {
            Collider2D collider = part.GetComponent<Collider2D>();
            if (collider != null)
                Physics2D.IgnoreCollision(collider, game.Player.Collider, true);
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
        Flying = true;
        Crashed = false;
        Body.bodyType = RigidbodyType2D.Dynamic;
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0f;
        UpdateAir();

        if (pilot != null)
            pilot.enabled = true;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        RocketeeGame game = RocketeeGame.Instance;
        bool control = Flying && !Crashed && keyboard != null && game != null && game.State == GameState.Flight && !game.Paused;
        sprayLoop.volume = 0.6f * Settings.SoundVolume;

        sprayHeld = control && keyboard.spaceKey.isPressed;
        tiltInput = 0f;
        if (control)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                tiltInput += 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                tiltInput -= 1f;
        }

        bool sound = Spraying && Flying;
        if (sound && !sprayLoop.isPlaying && sprayLoop.clip != null)
            sprayLoop.Play();
        else if (!sound && sprayLoop.isPlaying)
            sprayLoop.Stop();
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
        {
            float push = RocketeeGame.Instance != null ? RocketeeGame.Instance.Data.ThrustMultiplier : 1f;
            Effects effects = RocketeeGame.Instance != null ? RocketeeGame.Instance.Effects : null;

            foreach (RocketPart part in Parts)
            {
                if (part == null || !part.HasFuel)
                    continue;

                part.Fuel = Mathf.Max(0f, part.Fuel - Time.fixedDeltaTime);
                Body.AddForceAtPosition((Vector2)transform.up * part.Info.thrust * push, part.Nozzle);
                Spraying = true;

                if (effects != null)
                {
                    Vector2 spray = -(Vector2)transform.up * Random.Range(5f, 8f) + Random.insideUnitCircle * 1.2f;
                    effects.Spray(part.Nozzle, spray + Body.linearVelocity * 0.5f);
                }
            }
        }

        if (tiltInput != 0f)
            Body.AddTorque(tiltInput * tiltTorque * Body.mass);
    }

    // Thinner air and weaker gravity the higher you go; fins and nose cone help
    void UpdateAir()
    {
        float height = Height;
        float thinAir = Mathf.Lerp(1f, thinAirDrag, Mathf.InverseLerp(thinAirStart, thinAirEnd, height));
        float space = Mathf.InverseLerp(spaceStart, spaceEnd, height);

        float dragCut = 0f;
        float stability = 0f;
        foreach (RocketPart part in Parts)
        {
            if (part == null)
                continue;
            dragCut = Mathf.Max(dragCut, part.Info.dragCut);
            stability += part.Info.stability;
        }

        Body.gravityScale = Mathf.Lerp(1f, spaceGravity, space);
        Body.linearDamping = airDrag * (1f - dragCut) * thinAir;
        Body.angularDamping = spinDrag + stability;
    }

    // Height above where the rocket started, in units (meters)
    public float Height => Mathf.Max(0f, transform.position.y - LaunchHeight);

    public bool HasFuel
    {
        get
        {
            foreach (RocketPart part in Parts)
            {
                if (part != null && part.HasFuel)
                    return true;
            }
            return false;
        }
    }

    // 0 to 1, for the fuel bar
    public float FuelFraction
    {
        get
        {
            float left = 0f;
            float max = 0f;
            foreach (RocketPart part in Parts)
            {
                if (part == null || !part.Info.IsEngine)
                    continue;
                left += part.Fuel;
                max += part.MaxFuel;
            }
            return max > 0f ? left / max : 0f;
        }
    }

    // Stops the rocket where it is (you reached the Moon)
    public void Stop()
    {
        Flying = false;
        Spraying = false;
        Body.bodyType = RigidbodyType2D.Kinematic;
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0f;
    }

    // A part was destroyed by a meteor
    public void LosePart(RocketPart part)
    {
        Parts.Remove(part);
        Destroy(part.gameObject);
    }

    // The seat was hit: the pilot flies out and the rocket stops spraying
    public void Crash()
    {
        if (Crashed)
            return;

        Crashed = true;
        Spraying = false;
        Sfx.Play("explosion");
        Sfx.Play("fall", 0.8f);

        if (RocketeeGame.Instance != null && Seat != null)
            RocketeeGame.Instance.Effects.Burst(Seat.transform.position, new Color(1f, 0.6f, 0.2f), 24);

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
