/*********************************************************************************************
 * COMPONENT OF: none (shared data used by the whole game)
 * REQUIRED DEPENDENCIES: sprites in Assets/Resources/Sprites (colored boxes are used if missing)
 * DESCRIPTION: Every rocket part you can build with (seat, colas, crates, fins, nose cone) and
 *              every upgrade in the shop: price, weight, push, fuel and how many meteor hits a
 *              part can take. Change the numbers here to balance the game.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

// The order here must match the order of PartCatalog.Parts
public enum PartType { Seat, Cola, BigCola, Crate, MetalCrate, Fin, Nose }

public class PartInfo
{
    public PartType type;
    public string name;
    // File name in Assets/Resources/Sprites (without .png)
    public string sprite;
    // Color of the box that is drawn if the sprite is missing
    public Color color;
    public int price;
    // Every one you already own makes the next one cost this much more (1.2 = 20% more)
    public float priceGrowth = 1.1f;
    public float mass;
    // Push while spraying (only cola bottles have this)
    public float thrust;
    // Seconds of spraying in one bottle
    public float fuel;
    // How many meteor hits the part can take
    public int hits = 1;
    // Less spinning (fins)
    public float stability;
    // Less air drag, 0 to 1 (nose cone)
    public float dragCut;
    // Crates are full of spare cola: every cola touching this part (on its left, right or
    // top) sprays this many seconds longer
    public float fuelBonus;
    public string description;

    public bool IsEngine => thrust > 0f;
}

public class UpgradeInfo
{
    public string name;
    public string description;
    // One price per level
    public int[] prices;
}

public static class PartCatalog
{
    public static readonly PartInfo[] Parts =
    {
        new PartInfo { type = PartType.Seat, name = "Seat", sprite = "seat", color = new Color(0.75f, 0.25f, 0.2f),
            price = 10, priceGrowth = 1f, mass = 1.5f, hits = 2, description = "You sit here. Two meteor hits and you crash!" },
        new PartInfo { type = PartType.Cola, name = "Cola", sprite = "cola", color = new Color(0.85f, 0.1f, 0.1f),
            price = 22, priceGrowth = 1.18f, mass = 0.6f, thrust = 33f, fuel = 3.4f, description = "Engine. Hold Space to spray it." },
        new PartInfo { type = PartType.BigCola, name = "2L Cola", sprite = "cola_big", color = new Color(0.55f, 0.05f, 0.08f),
            price = 100, priceGrowth = 1.18f, mass = 1.1f, thrust = 52f, fuel = 6.5f, description = "Bigger engine: more push, sprays longer." },
        new PartInfo { type = PartType.Crate, name = "Crate", sprite = "crate", color = new Color(0.65f, 0.45f, 0.2f),
            price = 8, priceGrowth = 1.06f, mass = 0.25f, fuelBonus = 1.2f, description = "Spare cola: colas touching it spray longer. Blocks 1 meteor." },
        new PartInfo { type = PartType.MetalCrate, name = "Metal Crate", sprite = "crate_metal", color = new Color(0.55f, 0.6f, 0.65f),
            price = 30, priceGrowth = 1.06f, mass = 0.6f, hits = 3, fuelBonus = 1.2f, description = "Spare cola too, and strong: blocks 3 meteors." },
        new PartInfo { type = PartType.Fin, name = "Fin", sprite = "fin", color = new Color(0.9f, 0.3f, 0.2f),
            price = 15, mass = 0.3f, stability = 2f, description = "Stops your rocket from spinning." },
        new PartInfo { type = PartType.Nose, name = "Nose Cone", sprite = "nose", color = new Color(0.95f, 0.95f, 0.95f),
            price = 40, priceGrowth = 1.5f, mass = 0.4f, dragCut = 0.4f, description = "Cuts through the air: less drag." },
    };

    // Balance tested with a flight simulator: the first rocket (seat, 2 colas, 2 crates next to
    // them) reaches about 100 m and a perfect player needs about 5 flights to reach the Moon
    // (most players about 7-9). Crates help: the same rocket without crates reaches about 92 m.
    public static readonly UpgradeInfo ThrustUpgrade = new UpgradeInfo
    {
        name = "Shake It Harder", description = "+17% push for every cola", prices = new[] { 90, 200, 400, 750, 1200 }
    };

    public static readonly UpgradeInfo FuelUpgrade = new UpgradeInfo
    {
        name = "Bigger Bottles", description = "+22% spray time for every cola", prices = new[] { 80, 180, 360, 700, 1100 }
    };

    public static readonly UpgradeInfo GridUpgrade = new UpgradeInfo
    {
        name = "Bigger Launch Pad", description = "More room to build", prices = new[] { 150, 450 }
    };

    // Build grid width and height for each Bigger Launch Pad level
    public static readonly int[] GridSizes = { 5, 7, 9 };

    public const float ThrustPerLevel = 0.17f;
    public const float FuelPerLevel = 0.22f;

    public static int Count => Parts.Length;

    public static PartInfo Get(PartType type)
    {
        return Parts[(int)type];
    }
}
