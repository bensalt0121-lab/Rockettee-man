/*********************************************************************************************
 * COMPONENT OF: none (shared data used by the whole game)
 * REQUIRED DEPENDENCIES: sprites in Assets/Resources/Sprites (colored boxes are used if missing)
 * DESCRIPTION: Every rocket part you can build with and every upgrade in the shop: price,
 *              weight, push, fuel, how many hits a part can take, and from which stage it is
 *              sold (stage 1 = Earth to Moon, 2 = Moon to Mars, 3 = Mars to the Sun).
 *              Change the numbers here to balance the game.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.2
 * VERSION 1.1: Crates are full of spare cola (colas touching them spray longer).
 * VERSION 1.2: More parts (tanks, Mentos booster, wing, coin magnet, titanium crate, bubble
 *              shield, cooler, heat shield) and more upgrades (8 in total).
 *********************************************************************************************/
using UnityEngine;

// New parts are always added at the END so old saves still work
public enum PartType
{
    Seat, Cola, BigCola, Crate, MetalCrate, Fin, Nose,
    Tank, BigTank, TitaniumCrate, Cooler, HeatShield, Mentos, Wing, Magnet, BubbleShield
}

// New upgrades are always added at the END so old saves still work
public enum UpgradeType { Thrust, Fuel, Grid, Helmet, LuckyCoins, Steering, Sponsor, ToughCrates }

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
    // Push while spraying (colas and the Mentos booster)
    public float thrust;
    // Seconds of spraying in one bottle
    public float fuel;
    // How many hits the part can take
    public int hits = 1;
    // Less spinning (fins, wings)
    public float stability;
    // Less air drag, 0 to 1 (nose cone)
    public float dragCut;
    // Less gravity while flying, 0 to 1 (wings)
    public float lift;
    // Crates are full of spare cola: every cola touching this part (on its left, right or
    // top) sprays this many seconds longer
    public float fuelBonus;
    // Tanks: spare cola (in seconds of spraying) that all colas can use when their own runs out
    public float tankFuel;
    // Heat shields: how much slower the rocket heats up near the Sun (0.22 = 22%)
    public float heatCut;
    // Coolers: colas and tanks touching it don't boil near the Sun
    public bool cools;
    // Coin magnet: pulls coins from this far away (units)
    public float magnetRange;
    // Bubble shield: blocks one hit, then needs this many seconds to come back
    public float shieldRecharge;
    // Counts as a crate for the Tougher Crates upgrade
    public bool isCrate;
    // First stage this part is sold in (0 = stage 1)
    public int unlockStage;
    public string description;

    public bool IsEngine => thrust > 0f;
    public bool HoldsCola => IsEngine || tankFuel > 0f;
}

public class UpgradeInfo
{
    public UpgradeType type;
    public string name;
    public string description;
    // One price per level
    public int[] prices;
    // First stage this upgrade is sold in (0 = stage 1)
    public int unlockStage;
}

public static class PartCatalog
{
    // Same order as PartType
    public static readonly PartInfo[] Parts =
    {
        new PartInfo { type = PartType.Seat, name = "Seat", sprite = "seat", color = new Color(0.75f, 0.25f, 0.2f),
            price = 10, priceGrowth = 1f, mass = 1.5f, hits = 2, description = "You sit here. Two hits and you crash!" },
        new PartInfo { type = PartType.Cola, name = "Cola", sprite = "cola", color = new Color(0.85f, 0.1f, 0.1f),
            price = 22, priceGrowth = 1.12f, mass = 0.6f, thrust = 33f, fuel = 3.4f, description = "Engine. Hold Space to spray it." },
        new PartInfo { type = PartType.BigCola, name = "2L Cola", sprite = "cola_big", color = new Color(0.55f, 0.05f, 0.08f),
            price = 100, priceGrowth = 1.12f, mass = 1.1f, thrust = 52f, fuel = 6.5f, description = "Bigger engine: more push, sprays longer." },
        new PartInfo { type = PartType.Crate, name = "Crate", sprite = "crate", color = new Color(0.65f, 0.45f, 0.2f),
            price = 8, priceGrowth = 1.04f, mass = 0.25f, fuelBonus = 1.2f, isCrate = true, description = "Spare cola: colas touching it spray longer. Blocks 1 hit." },
        new PartInfo { type = PartType.MetalCrate, name = "Metal Crate", sprite = "crate_metal", color = new Color(0.55f, 0.6f, 0.65f),
            price = 30, priceGrowth = 1.04f, mass = 0.6f, hits = 3, fuelBonus = 1.2f, isCrate = true, description = "Spare cola too, and strong: blocks 3 hits." },
        new PartInfo { type = PartType.Fin, name = "Fin", sprite = "fin", color = new Color(0.9f, 0.3f, 0.2f),
            price = 15, mass = 0.3f, stability = 2f, description = "Stops your rocket from spinning." },
        new PartInfo { type = PartType.Nose, name = "Nose Cone", sprite = "nose", color = new Color(0.95f, 0.95f, 0.95f),
            price = 40, priceGrowth = 1.3f, mass = 0.4f, dragCut = 0.4f, description = "Cuts through the air: less drag." },

        // Tanks feed every cola through pipes once the cola's own bottle is empty
        new PartInfo { type = PartType.Tank, name = "Cola Tank", sprite = "tank", color = new Color(0.8f, 0.15f, 0.15f),
            price = 120, priceGrowth = 1.1f, mass = 0.5f, tankFuel = 6.8f, description = "Holds 2 colas of spare cola for all your colas." },
        new PartInfo { type = PartType.BigTank, name = "4L Tank", sprite = "tank_big", color = new Color(0.5f, 0.06f, 0.08f),
            price = 300, priceGrowth = 1.1f, mass = 0.9f, hits = 2, tankFuel = 13.6f, description = "Holds 4 colas of spare cola. Takes 2 hits." },

        // Stage 2 (Moon to Mars): space junk flies around
        new PartInfo { type = PartType.TitaniumCrate, name = "Titanium", sprite = "crate_titanium", color = new Color(0.45f, 0.55f, 0.7f),
            price = 250, priceGrowth = 1.05f, mass = 0.5f, hits = 6, fuelBonus = 1.2f, isCrate = true, unlockStage = 1, description = "Space armor: blocks 6 hits. Spare cola inside too." },

        // Stage 3 (Mars to the Sun): heat
        new PartInfo { type = PartType.Cooler, name = "Cooler", sprite = "cooler", color = new Color(0.3f, 0.6f, 0.95f),
            price = 200, priceGrowth = 1.06f, mass = 0.4f, cools = true, unlockStage = 2, description = "Colas and tanks touching it don't boil near the Sun." },
        new PartInfo { type = PartType.HeatShield, name = "Heat Shield", sprite = "heat_shield", color = new Color(1f, 0.6f, 0.15f),
            price = 300, priceGrowth = 1.08f, mass = 0.6f, hits = 2, heatCut = 0.22f, unlockStage = 2, description = "Your rocket heats up 22% slower near the Sun." },

        // Extra fun parts
        new PartInfo { type = PartType.Mentos, name = "Mentos Boost", sprite = "mentos", color = new Color(0.95f, 0.95f, 0.85f),
            price = 60, priceGrowth = 1.1f, mass = 0.4f, thrust = 90f, fuel = 1f, description = "Huge push for 1 second. Great for take-off!" },
        new PartInfo { type = PartType.Wing, name = "Wing", sprite = "wing", color = new Color(0.85f, 0.85f, 0.9f),
            price = 50, priceGrowth = 1.08f, mass = 0.3f, stability = 1f, lift = 0.05f, description = "Less spinning and a little lift (5% less gravity)." },
        new PartInfo { type = PartType.Magnet, name = "Coin Magnet", sprite = "magnet", color = new Color(0.85f, 0.2f, 0.25f),
            price = 80, priceGrowth = 1.2f, mass = 0.4f, magnetRange = 6f, description = "Pulls coins into your rocket from far away." },
        new PartInfo { type = PartType.BubbleShield, name = "Bubble", sprite = "bubble_shield", color = new Color(0.5f, 0.85f, 1f),
            price = 400, priceGrowth = 1.2f, mass = 0.5f, shieldRecharge = 6f, unlockStage = 1, description = "Blocks one hit, then recharges in 6 seconds." },
    };

    // Balance tested with a flight simulator: the first rocket (seat, 2 colas, 2 crates next to
    // them) reaches about 100 m and a perfect player needs about 5 flights to reach the Moon.
    // The rocket that reaches the Moon only gets about 1500 m towards Mars (2500 m), so you
    // need tanks and upgrades; the Sun (4000 m) needs even more, plus shields and coolers.
    // Same order as UpgradeType
    public static readonly UpgradeInfo[] Upgrades =
    {
        new UpgradeInfo { type = UpgradeType.Thrust, name = "Shake It Harder", description = "+17% push for every cola",
            prices = new[] { 80, 170, 340, 640, 1000, 1700, 2500, 3800 } },
        new UpgradeInfo { type = UpgradeType.Fuel, name = "Bigger Bottles", description = "+22% spray time for colas and tanks",
            prices = new[] { 70, 150, 300, 600, 950, 1500, 2400, 3600 } },
        new UpgradeInfo { type = UpgradeType.Grid, name = "Bigger Launch Pad", description = "More room to build",
            prices = new[] { 130, 380, 1250 } },
        new UpgradeInfo { type = UpgradeType.Helmet, name = "Space Helmet", description = "Your seat survives one more hit",
            prices = new[] { 340, 1000 }, unlockStage = 1 },
        new UpgradeInfo { type = UpgradeType.LuckyCoins, name = "Lucky Coins", description = "+30% money from every coin",
            prices = new[] { 85, 210, 500 } },
        new UpgradeInfo { type = UpgradeType.Steering, name = "Better Steering", description = "+25% faster turning with A/D",
            prices = new[] { 70, 170, 380 } },
        new UpgradeInfo { type = UpgradeType.Sponsor, name = "Sponsor Deal", description = "+15% money for every meter",
            prices = new[] { 130, 340, 760, 1700 } },
        new UpgradeInfo { type = UpgradeType.ToughCrates, name = "Tougher Crates", description = "Every crate takes 1 more hit",
            prices = new[] { 250, 760 }, unlockStage = 1 },
    };

    // Build grid width and height for each Bigger Launch Pad level
    public static readonly int[] GridSizes = { 5, 7, 9, 11 };

    public const float ThrustPerLevel = 0.17f;
    public const float FuelPerLevel = 0.22f;
    public const float CoinsPerLevel = 0.3f;
    public const float SteeringPerLevel = 0.25f;
    public const float SponsorPerLevel = 0.15f;

    public static int Count => Parts.Length;

    public static PartInfo Get(PartType type)
    {
        return Parts[(int)type];
    }

    public static UpgradeInfo Get(UpgradeType type)
    {
        return Upgrades[(int)type];
    }
}
