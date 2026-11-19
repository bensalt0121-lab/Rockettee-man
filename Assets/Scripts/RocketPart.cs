/*********************************************************************************************
 * COMPONENT OF: every part of the rocket (made by Rocket.Rebuild)
 * REQUIRED DEPENDENCIES: Box Collider 2D, Rocket on the parent
 * DESCRIPTION: One block of the rocket. Remembers how much cola is left (colas, Mentos and
 *              tanks), how many hits it can still take, and whether a cooler is touching it.
 *              When it breaks it falls off; if the seat breaks the rocket crashes.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.1
 * VERSION 1.1: Tanks, coolers, extra hits from the Space Helmet and Tougher Crates upgrades,
 *              the bubble shield can block a hit.
 *********************************************************************************************/
using UnityEngine;

public class RocketPart : MonoBehaviour
{
    public PartInfo Info { get; private set; }
    public Vector2Int Cell { get; private set; }
    public Rocket Rocket { get; private set; }
    public int HitsLeft { get; private set; }
    // Seconds of spraying left (colas and Mentos), or spare cola left (tanks)
    public float Fuel { get; set; }
    public float MaxFuel { get; private set; }
    // A cooler is touching it, so it does not boil near the Sun
    public bool Cooled { get; set; }
    public SpriteRenderer Look { get; private set; }

    private float flashTimer;
    private Color normalColor = Color.white;

    public void Setup(Rocket rocket, PartInfo info, Vector2Int cell, SpriteRenderer look, GameSave save)
    {
        Rocket = rocket;
        Info = info;
        Cell = cell;
        Look = look;
        HitsLeft = info.hits + save.ExtraHits(info);
        MaxFuel = (info.fuel + info.tankFuel) * save.FuelMultiplier;
        Fuel = MaxFuel;
    }

    // Extra spray time from crates touching this cola
    public void AddFuel(float seconds)
    {
        MaxFuel += seconds;
        Fuel += seconds;
    }

    // Real cola bottles (they get crate bonuses and can drink from tanks; Mentos can't)
    public bool IsCola => Info.type == PartType.Cola || Info.type == PartType.BigCola;
    public bool IsTank => Info.tankFuel > 0f;

    // Bottom of the part: where the cola sprays out
    public Vector2 Nozzle => transform.TransformPoint(new Vector3(0f, -0.5f, 0f));

    public bool HasFuel => Info.IsEngine && Fuel > 0f;

    // Red tint for parts that are not connected to the seat (they stay behind)
    public void SetLoose(bool loose)
    {
        normalColor = loose ? new Color(1f, 0.35f, 0.35f, 0.65f) : Color.white;
        if (Look != null)
            Look.color = normalColor;
    }

    public void TakeHit(int damage)
    {
        // Only during a flight (not on the results screen while the rocket falls back down)
        RocketeeGame game = RocketeeGame.Instance;
        if (Rocket == null || Rocket.Crashed || !Rocket.Flying || game == null || game.State != GameState.Flight)
            return;

        // The bubble shield takes the hit instead
        if (Rocket.TryBlockHit(transform.position))
            return;

        HitsLeft -= damage;
        flashTimer = 0.15f;
        CameraRig.Shake(0.3f);

        if (HitsLeft <= 0)
        {
            Break();
            return;
        }

        Sfx.Play("hit", 0.8f);

        // Warning: tells you how many more hits your seat can take
        if (Info.type == PartType.Seat && RocketeeGame.Instance != null)
            RocketeeGame.Instance.UI.Say(HitsLeft == 1 ? "Your seat got hit! One more hit and you crash!" : "Your seat got hit! " + HitsLeft + " hits left.", 2.5f);
    }

    void Break()
    {
        if (Info.type == PartType.Seat)
        {
            Rocket.Crash("A hit broke your seat.");
            return;
        }

        // Wood smashes, metal clangs, colas burst
        bool wooden = Info.type == PartType.Crate || Info.type == PartType.Fin || Info.type == PartType.Nose || Info.type == PartType.Wing;
        bool metal = Info.type == PartType.MetalCrate || Info.type == PartType.TitaniumCrate || Info.type == PartType.HeatShield
            || Info.type == PartType.Magnet || Info.type == PartType.BubbleShield || Info.type == PartType.Cooler;
        Sfx.Play(wooden ? "crate_break" : metal && Sfx.Get("metal_hit") != null ? "metal_hit" : "explosion", 0.7f);
        if (RocketeeGame.Instance != null)
            RocketeeGame.Instance.Effects.Burst(transform.position, Info.color, 14);

        Rocket.LosePart(this);
    }

    void Update()
    {
        if (Look == null)
            return;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            Look.color = flashTimer > 0f ? new Color(1f, 0.6f, 0.6f) : normalColor;
        }
    }
}
