/*********************************************************************************************
 * COMPONENT OF: none (saved data, kept in PlayerPrefs)
 * REQUIRED DEPENDENCIES: PartCatalog, Stages
 * DESCRIPTION: Everything the game remembers between plays: money, which stage you are on,
 *              your best height in each stage, which parts you own, your upgrades, the rocket
 *              you built and the parts you already picked up in the yard.
 *              Tools > Rocketee Man > Reset Saved Progress (or Settings > Reset) clears it.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.1
 * VERSION 1.1: Stages (current stage, best height and milestones per stage), helmet level.
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedPart
{
    public int x;
    public int y;
    public PartType type;
}

[System.Serializable]
public class GameSave
{
    // A little money to start with
    public int money = 20;
    public int flights;
    // Level of every upgrade (index = UpgradeType)
    public int[] upgrades = new int[0];

    // Old saves kept these separately; they are moved into the upgrades list
    public int thrustLevel;
    public int fuelLevel;
    public int gridLevel;

    // Stage you are on now (0 = Earth to Moon, 1 = Moon to Mars, 2 = Mars to the Sun)
    public int stage;
    // How many stages you finished (3 = you reached the Sun)
    public int stagesCleared;
    // Best height and paid milestones for each stage
    public float[] stageBest = new float[0];
    public int[] stageMilestones = new int[0];

    // Old saves (before stages) used these; they are moved into the stage lists
    public float bestHeight;
    public int milestones;
    public bool reachedMoon;

    // How many of each part you own (index = PartType)
    public int[] owned = new int[0];
    // The rocket you built: grid position and part type
    public List<PlacedPart> layout = new List<PlacedPart>();
    // Starter parts in the yard that were already picked up
    public List<string> collected = new List<string>();

    private const string SaveKey = "RocketeeMan.Save";

    public static GameSave Load()
    {
        GameSave save = null;

        if (PlayerPrefs.HasKey(SaveKey))
        {
            try
            {
                save = JsonUtility.FromJson<GameSave>(PlayerPrefs.GetString(SaveKey));
            }
            catch (System.Exception)
            {
                save = null;
            }
        }

        if (save == null)
            save = new GameSave();

        save.FixUp();
        return save;
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }

    // Makes old or broken saves safe to use
    void FixUp()
    {
        owned = Resize(owned, PartCatalog.Count);
        stageMilestones = Resize(stageMilestones, Stages.Count);
        upgrades = Resize(upgrades, PartCatalog.Upgrades.Length);

        // Moves old upgrade levels into the upgrades list
        upgrades[(int)UpgradeType.Thrust] = Mathf.Max(upgrades[(int)UpgradeType.Thrust], thrustLevel);
        upgrades[(int)UpgradeType.Fuel] = Mathf.Max(upgrades[(int)UpgradeType.Fuel], fuelLevel);
        upgrades[(int)UpgradeType.Grid] = Mathf.Max(upgrades[(int)UpgradeType.Grid], gridLevel);
        thrustLevel = 0;
        fuelLevel = 0;
        gridLevel = 0;
        for (int i = 0; i < upgrades.Length; i++)
            upgrades[i] = Mathf.Clamp(upgrades[i], 0, PartCatalog.Upgrades[i].prices.Length);

        if (stageBest == null || stageBest.Length != Stages.Count)
        {
            float[] best = new float[Stages.Count];
            if (stageBest != null)
                System.Array.Copy(stageBest, best, Mathf.Min(stageBest.Length, best.Length));
            stageBest = best;
        }

        // Moves an old save (before stages) into stage 1
        if (bestHeight > stageBest[0])
            stageBest[0] = bestHeight;
        if (milestones > stageMilestones[0])
            stageMilestones[0] = milestones;
        if (reachedMoon && stagesCleared == 0)
        {
            stagesCleared = 1;
            stage = 1;
        }
        bestHeight = 0f;
        milestones = 0;
        reachedMoon = false;

        if (layout == null)
            layout = new List<PlacedPart>();
        if (collected == null)
            collected = new List<string>();

        layout.RemoveAll(p => p == null || (int)p.type < 0 || (int)p.type >= PartCatalog.Count);
        stage = Mathf.Clamp(stage, 0, Stages.Count - 1);
        stagesCleared = Mathf.Clamp(stagesCleared, 0, Stages.Count);
    }

    static int[] Resize(int[] values, int length)
    {
        if (values != null && values.Length == length)
            return values;
        int[] resized = new int[length];
        if (values != null)
            System.Array.Copy(values, resized, Mathf.Min(values.Length, length));
        return resized;
    }

    public StageInfo Stage => Stages.Get(stage);

    // Best height in the stage you are on now
    public float Best
    {
        get => stageBest[stage];
        set => stageBest[stage] = value;
    }

    public int Level(UpgradeType type)
    {
        return upgrades[(int)type];
    }

    public int GridSize => PartCatalog.GridSizes[Mathf.Clamp(Level(UpgradeType.Grid), 0, PartCatalog.GridSizes.Length - 1)];

    public int Owned(PartType type)
    {
        return owned[(int)type];
    }

    public int Placed(PartType type)
    {
        int count = 0;
        foreach (PlacedPart part in layout)
        {
            if (part.type == type)
                count++;
        }
        return count;
    }

    // Owned parts that are not on the rocket yet
    public int Available(PartType type)
    {
        return Owned(type) - Placed(type);
    }

    // Sold in the shop at this stage?
    public bool Unlocked(PartInfo info)
    {
        return info.unlockStage <= stage;
    }

    public PlacedPart At(int x, int y)
    {
        foreach (PlacedPart part in layout)
        {
            if (part.x == x && part.y == y)
                return part;
        }
        return null;
    }

    public bool Unlocked(UpgradeInfo info)
    {
        return info.unlockStage <= stage;
    }

    public float ThrustMultiplier => 1f + Level(UpgradeType.Thrust) * PartCatalog.ThrustPerLevel;
    public float FuelMultiplier => 1f + Level(UpgradeType.Fuel) * PartCatalog.FuelPerLevel;
    public float CoinMultiplier => 1f + Level(UpgradeType.LuckyCoins) * PartCatalog.CoinsPerLevel;
    public float SteeringMultiplier => 1f + Level(UpgradeType.Steering) * PartCatalog.SteeringPerLevel;
    public float MoneyPerMeterMultiplier => 1f + Level(UpgradeType.Sponsor) * PartCatalog.SponsorPerLevel;

    // Extra hits for the seat (helmet) and for crates (tougher crates)
    public int ExtraHits(PartInfo info)
    {
        if (info.type == PartType.Seat)
            return Level(UpgradeType.Helmet);
        return info.isCrate ? Level(UpgradeType.ToughCrates) : 0;
    }
}
