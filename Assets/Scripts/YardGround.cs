/*********************************************************************************************
 * COMPONENT OF: none (helper used by the other scripts)
 * REQUIRED DEPENDENCIES: a Tilemap with a Tilemap Collider 2D (the ground you painted)
 * DESCRIPTION: Finds how high the ground is and where the yard starts and ends, by looking at
 *              the tiles you painted. So the shop, launch pad and parts always sit on the
 *              ground, even if you change the level.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class YardGround
{
    // Used when no ground tiles are found
    public const float DefaultGround = -3f;
    public const float DefaultLeft = -9f;
    public const float DefaultRight = 9f;

    // Tilemaps the player can stand on (the ones with a Tilemap Collider 2D)
    static List<Tilemap> SolidTilemaps()
    {
        List<Tilemap> maps = new List<Tilemap>();
        foreach (TilemapCollider2D collider in Object.FindObjectsByType<TilemapCollider2D>())
        {
            Tilemap map = collider.GetComponent<Tilemap>();
            if (map != null)
                maps.Add(map);
        }
        return maps;
    }

    // Height of the top of the highest ground tile at this x position
    public static float TopAt(float worldX)
    {
        float best = float.NegativeInfinity;

        foreach (Tilemap map in SolidTilemaps())
        {
            BoundsInt bounds = map.cellBounds;
            int column = map.WorldToCell(new Vector3(worldX, map.transform.position.y, 0f)).x;

            for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
            {
                if (map.HasTile(new Vector3Int(column, y, 0)))
                {
                    best = Mathf.Max(best, map.CellToWorld(new Vector3Int(column, y + 1, 0)).y);
                    break;
                }
            }
        }

        return float.IsNegativeInfinity(best) ? DefaultGround : best;
    }

    // Left and right edge of the painted ground
    public static void Span(out float left, out float right)
    {
        left = float.PositiveInfinity;
        right = float.NegativeInfinity;

        foreach (Tilemap map in SolidTilemaps())
        {
            BoundsInt bounds = map.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    if (!map.HasTile(new Vector3Int(x, y, 0)))
                        continue;

                    left = Mathf.Min(left, map.CellToWorld(new Vector3Int(x, y, 0)).x);
                    right = Mathf.Max(right, map.CellToWorld(new Vector3Int(x + 1, y, 0)).x);
                    break;
                }
            }
        }

        if (float.IsPositiveInfinity(left))
        {
            left = DefaultLeft;
            right = DefaultRight;
        }
    }

    // True if any ground tiles were painted
    public static bool HasGround()
    {
        foreach (Tilemap map in SolidTilemaps())
        {
            BoundsInt bounds = map.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (map.HasTile(cell))
                    return true;
            }
        }
        return false;
    }
}
