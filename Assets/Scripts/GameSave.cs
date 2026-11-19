/*********************************************************************************************
 * COMPONENT OF: none (saved data, kept in PlayerPrefs)
 * REQUIRED DEPENDENCIES: PartCatalog
 * DESCRIPTION: Everything the game remembers between plays: money, best height, which parts
 *              you own, your upgrades, the rocket you built and the parts you already picked
 *              up in the yard. Tools > Rocketee Man > Reset Saved Progress clears it.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
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
    // How many height milestones (100, 250, 500, 750 m) already paid their bonus
    public int milestones;
    public float bestHeight;
    public int flights;
    public bool reachedMoon;
    public int thrustLevel;
    public int fuelLevel;
    public int gridLevel;
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
        if (owned == null || owned.Length != PartCatalog.Count)
        {
            int[] fixedOwned = new int[PartCatalog.Count];
            if (owned != null)
                System.Array.Copy(owned, fixedOwned, Mathf.Min(owned.Length, fixedOwned.Length));
            owned = fixedOwned;
        }

        if (layout == null)
            layout = new List<PlacedPart>();
        if (collected == null)
            collected = new List<string>();

        layout.RemoveAll(p => p == null || (int)p.type < 0 || (int)p.type >= PartCatalog.Count);
        gridLevel = Mathf.Clamp(gridLevel, 0, PartCatalog.GridSizes.Length - 1);
    }

    public int GridSize => PartCatalog.GridSizes[Mathf.Clamp(gridLevel, 0, PartCatalog.GridSizes.Length - 1)];

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

    public PlacedPart At(int x, int y)
    {
        foreach (PlacedPart part in layout)
        {
            if (part.x == x && part.y == y)
                return part;
        }
        return null;
    }

    public float ThrustMultiplier => 1f + thrustLevel * PartCatalog.ThrustPerLevel;
    public float FuelMultiplier => 1f + fuelLevel * PartCatalog.FuelPerLevel;
}
