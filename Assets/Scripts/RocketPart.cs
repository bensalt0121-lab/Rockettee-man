/*********************************************************************************************
 * COMPONENT OF: every part of the rocket (made by Rocket.Rebuild)
 * REQUIRED DEPENDENCIES: Box Collider 2D, Rocket on the parent
 * DESCRIPTION: One block of the rocket: a seat, cola, crate, fin or nose cone. Remembers how
 *              much spray is left in a cola and how many meteor hits it can still take.
 *              When it breaks it falls off; if the seat breaks the rocket crashes.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public class RocketPart : MonoBehaviour
{
    public PartInfo Info { get; private set; }
    public Vector2Int Cell { get; private set; }
    public Rocket Rocket { get; private set; }
    public int HitsLeft { get; private set; }
    // Seconds of spraying left (colas only)
    public float Fuel { get; set; }
    public float MaxFuel { get; private set; }
    public SpriteRenderer Look { get; private set; }

    private float flashTimer;
    private Color normalColor = Color.white;

    public void Setup(Rocket rocket, PartInfo info, Vector2Int cell, SpriteRenderer look, float fuelMultiplier)
    {
        Rocket = rocket;
        Info = info;
        Cell = cell;
        Look = look;
        HitsLeft = info.hits;
        MaxFuel = info.fuel * fuelMultiplier;
        Fuel = MaxFuel;
    }

    // Extra spray time from crates touching this cola
    public void AddFuel(float seconds)
    {
        MaxFuel += seconds;
        Fuel += seconds;
    }

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
        if (Rocket == null || Rocket.Crashed)
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

        // Warning: the seat can take one hit, the next one is a crash
        if (Info.type == PartType.Seat && RocketeeGame.Instance != null)
            RocketeeGame.Instance.UI.Say("Your seat got hit! One more hit and you crash!", 2.5f);
    }

    void Break()
    {
        if (Info.type == PartType.Seat)
        {
            Rocket.Crash();
            return;
        }

        // Wood smashes, colas burst
        bool wooden = Info.type == PartType.Crate || Info.type == PartType.MetalCrate || Info.type == PartType.Fin || Info.type == PartType.Nose;
        Sfx.Play(wooden ? "crate_break" : "explosion", 0.7f);
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
