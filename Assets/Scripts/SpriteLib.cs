/*********************************************************************************************
 * COMPONENT OF: none (helper used by the other scripts)
 * REQUIRED DEPENDENCIES: sprites in Assets/Resources/Sprites (32 pixels = 1 unit)
 * DESCRIPTION: Loads the pixel art sprites by name. If a sprite is missing, it makes a simple
 *              colored box instead so the game still works. Also sizes sprites in units.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;

public static class SpriteLib
{
    public const float PixelsPerUnit = 32f;

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // A sprite from Assets/Resources/Sprites, or a colored box w x h pixels if it is missing
    public static Sprite Get(string name, Color fallback, int width = 32, int height = 32)
    {
        if (cache.TryGetValue(name, out Sprite cached) && cached != null)
            return cached;

        Sprite sprite = Resources.Load<Sprite>("Sprites/" + name);

        // Works even if the picture was not imported as a Sprite
        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>("Sprites/" + name);
            if (texture != null)
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        if (sprite == null)
            sprite = Box(width, height, fallback);

        cache[name] = sprite;
        return sprite;
    }

    // Size of a sprite in units, counting 32 pixels as 1 unit
    public static Vector2 UnitSize(Sprite sprite)
    {
        return new Vector2(sprite.rect.width, sprite.rect.height) / PixelsPerUnit;
    }

    // A filled box with a darker 1 pixel border
    public static Sprite Box(int width, int height, Color color)
    {
        Texture2D texture = NewTexture(width, height);
        Color border = Color.Lerp(color, Color.black, 0.45f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool edge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                texture.SetPixel(x, y, edge ? border : color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // An empty box with a thin border (used for the build grid)
    public static Sprite Outline(int size, Color color)
    {
        Texture2D texture = NewTexture(size, size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                texture.SetPixel(x, y, edge ? color : new Color(color.r, color.g, color.b, color.a * 0.12f));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // A filled circle
    public static Sprite Circle(int size, Color color)
    {
        Texture2D texture = NewTexture(size, size);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                texture.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? color : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // Adds a child object that shows a sprite, sized to 'size' units
    public static SpriteRenderer Show(Transform parent, string objectName, Sprite sprite, Vector2 size, int order)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        Fit(renderer, size);
        return renderer;
    }

    // Scales a sprite so it is exactly 'size' units big
    public static void Fit(SpriteRenderer renderer, Vector2 size)
    {
        if (renderer.sprite == null)
            return;

        Vector3 bounds = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(size.x / Mathf.Max(0.0001f, bounds.x), size.y / Mathf.Max(0.0001f, bounds.y), 1f);
    }

    static Texture2D NewTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }
}
