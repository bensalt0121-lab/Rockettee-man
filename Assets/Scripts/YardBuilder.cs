/*********************************************************************************************
 * COMPONENT OF: none (helper used by RocketeeGame and Tools > Rocketee Man > Build Everything)
 * REQUIRED DEPENDENCIES: the ground Tilemap (with a Tilemap Collider 2D) painted with your
 *                        sand tiles, YardGround, ShopStand, LaunchPad, PartPickup, YardPlayer
 * DESCRIPTION: Makes the yard a simple flat strip of ground 32 tiles wide with your own
 *              tiles: the top row uses the tiles you painted on top (sand tile 1 and 2), the
 *              rows below use the deeper tiles (deep sand 1 and 2), mixed the same way you
 *              mixed them. Anything floating above the ground is removed. Then it places the
 *              shop (left), launch pad (middle), your cacti (right, twice as big), the starter
 *              parts and a few desert decorations.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.1
 * VERSION 1.1: Smaller flat yard (no floating platforms), bigger cacti and decorations.
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class YardBuilder
{
    // Ground cells from Left to Right - 1 (32 tiles wide)
    public const int Left = -16;
    public const int Right = 16;
    // Rows of ground under the top row
    public const int DeepRows = 4;

    // Where things go (world x)
    public const float ShopX = -12.5f;
    public const float PlayerX = -8f;
    public const float PadX = 2.5f;

    // Your cacti go on the right, this much bigger
    public static readonly float[] CactusX = { 9.4f, 11.4f };
    public const float CactusScale = 2f;

    public static readonly (string id, PartType type, float x)[] StarterParts =
    {
        ("starter_crate_1", PartType.Crate, -6.5f),
        ("starter_seat", PartType.Seat, -4.5f),
        ("starter_cola_1", PartType.Cola, -2.5f),
        ("starter_cola_2", PartType.Cola, 6.3f),
        ("starter_crate_2", PartType.Crate, 7.8f),
    };

    // Decorations: sprite, world x, size in pixels, how much bigger to draw it
    static readonly (string sprite, float x, int w, int h, float scale)[] Decorations =
    {
        ("rock_1", -15.1f, 32, 16, 1.5f),
        ("sign_shop", -9.8f, 32, 32, 1.5f),
        ("sign_launch", -0.6f, 32, 32, 1.5f),
        ("rock_2", 5f, 16, 16, 1.5f),
        ("bush", 13.4f, 32, 24, 1.75f),
        ("small_cactus", 15f, 16, 32, 2f),
    };

    public const string DecorationsName = "Yard Decorations";

    // The ground tilemap (the first one with a Tilemap Collider 2D)
    public static Tilemap Ground()
    {
        TilemapCollider2D collider = Object.FindAnyObjectByType<TilemapCollider2D>();
        return collider != null ? collider.GetComponent<Tilemap>() : null;
    }

    // Makes the flat ground. Returns how many tiles were added or removed (0 = it was already right).
    public static int MakeSimpleGround(Tilemap map)
    {
        if (map == null)
            return 0;

        // The top row of the ground is the highest row that is mostly full
        // (rows with only a few tiles are floating platforms)
        BoundsInt bounds = map.cellBounds;
        Dictionary<int, int> tilesInRow = new Dictionary<int, int>();
        int fullest = 0;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!map.HasTile(cell))
                continue;
            tilesInRow.TryGetValue(cell.y, out int count);
            tilesInRow[cell.y] = count + 1;
            fullest = Mathf.Max(fullest, count + 1);
        }
        if (fullest == 0)
            return 0;

        int topRow = int.MinValue;
        foreach (var row in tilesInRow)
        {
            if (row.Value * 2 >= fullest)
                topRow = Mathf.Max(topRow, row.Key);
        }

        // Which tiles you used on top and underneath (each as often as you used it, so the mix stays)
        List<TileBase> topTiles = new List<TileBase>();
        List<TileBase> deepTiles = new List<TileBase>();
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = map.GetTile(cell);
            if (tile == null || cell.y > topRow)
                continue;
            if (cell.y == topRow)
                topTiles.Add(tile);
            else
                deepTiles.Add(tile);
        }
        if (deepTiles.Count == 0)
            deepTiles.AddRange(topTiles);

        int changed = 0;

        // Removes anything floating above the ground, and ground outside the yard
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!map.HasTile(cell))
                continue;
            bool floating = cell.y > topRow;
            bool outside = (cell.x < Left || cell.x >= Right) && cell.y >= topRow - DeepRows;
            if (floating || outside)
            {
                map.SetTile(cell, null);
                changed++;
            }
        }

        // Fills the flat ground
        for (int x = Left; x < Right; x++)
        {
            for (int y = topRow - DeepRows; y <= topRow; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (map.HasTile(cell))
                    continue;
                map.SetTile(cell, Pick(y == topRow ? topTiles : deepTiles, x, y));
                changed++;
            }
        }

        if (changed > 0)
            map.CompressBounds();

        // Updates the ground collider right away (when playing)
        TilemapCollider2D collider = map.GetComponent<TilemapCollider2D>();
        if (changed > 0 && collider != null && Application.isPlaying)
            collider.ProcessTilemapChanges();

        return changed;
    }

    // Same tile for the same cell every time, but mixed up like hand painting
    static TileBase Pick(List<TileBase> tiles, int x, int y)
    {
        int hash = (x * 73856093) ^ (y * 19349663);
        return tiles[Mathf.Abs(hash) % tiles.Count];
    }

    // Puts the shop, launch pad, cacti, starter parts and player at their spots
    public static void PlaceYardObjects(float left, float right)
    {
        ShopStand shop = Object.FindAnyObjectByType<ShopStand>();
        if (shop != null)
            OnGround(shop.transform, Clamp(ShopX, left, right), 0f);

        LaunchPad pad = Object.FindAnyObjectByType<LaunchPad>();
        if (pad != null)
            OnGround(pad.transform, Clamp(PadX, left, right), 0f);

        foreach (PartPickup pickup in Object.FindObjectsByType<PartPickup>())
        {
            foreach (var starter in StarterParts)
            {
                if (starter.id == pickup.id)
                    OnGround(pickup.transform, Clamp(starter.x, left, right), 0f);
            }
        }

        YardPlayer player = Object.FindAnyObjectByType<YardPlayer>();
        if (player != null)
            OnGround(player.transform, Clamp(PlayerX, left, right), 0.8f);

        ArrangeCacti(left, right);
    }

    // Your cacti (objects named "cactus...") go on the right, twice as big, standing on the ground
    public static void ArrangeCacti(float left, float right)
    {
        List<SpriteRenderer> cacti = new List<SpriteRenderer>();
        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>())
        {
            if (renderer.gameObject.name.ToLower().StartsWith("cactus") && renderer.transform.parent == null)
                cacti.Add(renderer);
        }
        cacti.Sort((a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));

        for (int i = 0; i < cacti.Count && i < CactusX.Length; i++)
        {
            Transform cactus = cacti[i].transform;
            if (Mathf.Abs(cactus.localScale.y) < CactusScale - 0.01f)
                cactus.localScale = new Vector3(CactusScale, CactusScale, 1f);

            float x = Clamp(CactusX[i], left, right);
            cactus.position = new Vector3(x, cactus.position.y, cactus.position.z);
            // Bottom of the picture on the ground
            cactus.position += Vector3.up * (YardGround.TopAt(x) - cacti[i].bounds.min.y);
        }
    }

    // Rocks, a bush, a small cactus and signs (made again each time so they match the yard)
    public static GameObject Decorate(float left, float right)
    {
        GameObject old = GameObject.Find(DecorationsName);
        if (old != null)
        {
            if (Application.isPlaying)
                Object.Destroy(old);
            else
                Object.DestroyImmediate(old);
        }

        GameObject holder = new GameObject(DecorationsName);
        foreach (var decoration in Decorations)
        {
            if (!SpriteLib.Has(decoration.sprite))
                continue;

            float x = Clamp(decoration.x, left, right);
            Sprite sprite = SpriteLib.Get(decoration.sprite, Color.gray, decoration.w, decoration.h);
            Vector2 size = SpriteLib.UnitSize(sprite) * decoration.scale;
            SpriteRenderer look = SpriteLib.Show(holder.transform, decoration.sprite, sprite, size, -1);
            look.transform.position = new Vector3(x, YardGround.TopAt(x) + size.y * 0.5f, 0f);
        }
        return holder;
    }

    static float Clamp(float x, float left, float right)
    {
        return Mathf.Clamp(x, left + 1f, right - 1f);
    }

    static void OnGround(Transform thing, float x, float lift)
    {
        thing.position = new Vector3(x, YardGround.TopAt(x) + lift, thing.position.z);
    }
}
