/*********************************************************************************************
 * COMPONENT OF: Rocketee Game (added by the game)
 * REQUIRED DEPENDENCIES: sprite "spray" (a round dot is used if it is missing)
 * DESCRIPTION: Little flying dots: the cola spray under the rocket and the bits that fly off
 *              when something breaks. The dots are reused instead of made new each time,
 *              so it stays fast.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;

public class Effects : MonoBehaviour
{
    public int maxDots = 400;

    private class Dot
    {
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float life;
        public float maxLife;
        public float size;
        public float gravity;
        public Color color;
    }

    private readonly List<Dot> active = new List<Dot>();
    private readonly Stack<Dot> spare = new Stack<Dot>();
    private Sprite spraySprite;
    private Sprite plainSprite;
    private Transform holder;

    void Awake()
    {
        spraySprite = SpriteLib.Get("spray", new Color(0.75f, 0.55f, 0.35f), 8, 8);
        plainSprite = SpriteLib.Circle(8, Color.white);
        holder = new GameObject("Effect Dots").transform;
    }

    // One drop of cola spray
    public void Spray(Vector2 position, Vector2 velocity)
    {
        Add(position, velocity, spraySprite, Color.white, Random.Range(0.35f, 0.6f), Random.Range(0.18f, 0.3f), 2f);
    }

    // A puff of bits flying out in all directions
    public void Burst(Vector2 position, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = Random.insideUnitCircle.normalized * Random.Range(2f, 6f);
            Add(position, velocity, plainSprite, color, Random.Range(0.4f, 0.8f), Random.Range(0.12f, 0.25f), 6f);
        }
    }

    void Add(Vector2 position, Vector2 velocity, Sprite sprite, Color color, float life, float size, float gravity)
    {
        if (active.Count >= maxDots)
            return;

        Dot dot = spare.Count > 0 ? spare.Pop() : NewDot();
        dot.renderer.gameObject.SetActive(true);
        dot.renderer.sprite = sprite;
        dot.renderer.transform.position = position;
        dot.velocity = velocity;
        dot.life = life;
        dot.maxLife = life;
        dot.size = size;
        dot.gravity = gravity;
        dot.color = color;
        active.Add(dot);
    }

    Dot NewDot()
    {
        GameObject go = new GameObject("Dot");
        go.transform.SetParent(holder, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 8;
        return new Dot { renderer = renderer };
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            Dot dot = active[i];
            dot.life -= dt;

            if (dot.life <= 0f)
            {
                dot.renderer.gameObject.SetActive(false);
                active.RemoveAt(i);
                spare.Push(dot);
                continue;
            }

            dot.velocity += Vector2.down * dot.gravity * dt;
            Transform t = dot.renderer.transform;
            t.position += (Vector3)(dot.velocity * dt);

            float left = dot.life / dot.maxLife;
            float scale = dot.size * (0.5f + 0.5f * left);
            SpriteLib.Fit(dot.renderer, new Vector2(scale, scale));

            Color color = dot.color;
            color.a *= left;
            dot.renderer.color = color;
        }
    }
}
