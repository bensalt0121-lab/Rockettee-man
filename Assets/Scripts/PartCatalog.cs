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
            price = 10, priceGrowth = 1f, mass = 1.5f, description = "You sit here. If a meteor hits it, you crash!" },
        new PartInfo { type = PartType.Cola, name = "Cola", sprite = "cola", color = new Color(0.85f, 0.1f, 0.1f),
            price = 25, priceGrowth = 1.2f, mass = 0.6f, thrust = 30f, fuel = 4f, description = "Engine. Hold Space to spray it." },
        new PartInfo { type = PartType.BigCola, name = "2L Cola", sprite = "cola_big", color = new Color(0.55f, 0.05f, 0.08f),
            price = 110, priceGrowth = 1.2f, mass = 1.1f, thrust = 50f, fuel = 6f, description = "Bigger engine: more push, sprays longer." },
        new PartInfo { type = PartType.Crate, name = "Crate", sprite = "crate", color = new Color(0.65f, 0.45f, 0.2f),
            price = 8, mass = 0.5f, description = "Armor. Takes one meteor hit." },
        new PartInfo { type = PartType.MetalCrate, name = "Metal Crate", sprite = "crate_metal", color = new Color(0.55f, 0.6f, 0.65f),
            price = 30, mass = 1f, hits = 3, description = "Heavy armor. Takes three meteor hits." },
        new PartInfo { type = PartType.Fin, name = "Fin", sprite = "fin", color = new Color(0.9f, 0.3f, 0.2f),
            price = 15, mass = 0.3f, stability = 2f, description = "Stops your rocket from spinning." },
        new PartInfo { type = PartType.Nose, name = "Nose Cone", sprite = "nose", color = new Color(0.95f, 0.95f, 0.95f),
            price = 40, priceGrowth = 1.5f, mass = 0.4f, dragCut = 0.4f, description = "Cuts through the air: less drag." },
    };

    // Balance tested with a flight simulator: the first rocket reaches about 50 m and a
    // perfect player needs about 9 flights to reach the Moon (most players 15-20).
    public static readonly UpgradeInfo ThrustUpgrade = new UpgradeInfo
    {
        name = "Shake It Harder", description = "+15% push for every cola", prices = new[] { 100, 220, 450, 850, 1400 }
    };

    public static readonly UpgradeInfo FuelUpgrade = new UpgradeInfo
    {
        name = "Bigger Bottles", description = "+20% spray time for every cola", prices = new[] { 90, 200, 400, 800, 1300 }
    };

    public static readonly UpgradeInfo GridUpgrade = new UpgradeInfo
    {
        name = "Bigger Launch Pad", description = "More room to build", prices = new[] { 150, 450 }
    };

    // Build grid width and height for each Bigger Launch Pad level
    public static readonly int[] GridSizes = { 5, 7, 9 };

    public const float ThrustPerLevel = 0.15f;
    public const float FuelPerLevel = 0.2f;

    public static int Count => Parts.Length;

    public static PartInfo Get(PartType type)
    {
        return Parts[(int)type];
    }
}
