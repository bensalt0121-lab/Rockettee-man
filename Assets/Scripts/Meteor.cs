/*********************************************************************************************
 * COMPONENT OF: meteors, fireballs and space junk (made by FlightDirector during a flight)
 * REQUIRED DEPENDENCIES: Rigidbody 2D (kinematic), Circle Collider 2D, RocketPart,
 *                        sprites meteor_small/med/big, fireball, debris
 * DESCRIPTION: Something dangerous flying around in the sky:
 *              - meteors fall slowly and spin (a big one hits twice as hard)
 *              - fireballs (near the Sun) fall faster and hit twice as hard
 *              - space junk (Moon to Mars and later) flies fast sideways and hits twice as hard
 *              If it touches the rocket it damages the part it hit and bumps the rocket.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.1
 * VERSION 1.1: Fireballs and space junk.
 *********************************************************************************************/
using UnityEngine;

public class Meteor : MonoBehaviour
{
    public int damage = 1;
    public string hitSound = "hit";
    public Color burstColor = new Color(0.55f, 0.45f, 0.35f);

    private Rigidbody2D body;

    // A falling space rock: 1 = small, 2 = medium, 3 = big
    public static Meteor Spawn(Vector2 position, int size)
    {
        string spriteName = size == 1 ? "meteor_small" : size == 2 ? "meteor_med" : "meteor_big";
        int pixels = size == 1 ? 16 : size == 2 ? 32 : 64;
        float diameter = size == 1 ? 0.6f : size == 2 ? 1.1f : 2f;

        // Slow enough to see coming and steer around
        Vector2 velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-2f, -0.4f));
        Meteor meteor = Make(position, spriteName, pixels, diameter, velocity, size >= 3 ? 2 : 1, new Color(0.5f, 0.4f, 0.32f));
        meteor.body.angularVelocity = Random.Range(-90f, 90f);
        return meteor;
    }

    // A solar fireball: faster and hits twice as hard
    public static Meteor SpawnFireball(Vector2 position)
    {
        Vector2 velocity = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(-4f, -2f));
        Meteor fireball = Make(position, "fireball", 32, 1.2f, velocity, 2, new Color(1f, 0.6f, 0.1f));
        fireball.burstColor = new Color(1f, 0.55f, 0.1f);
        fireball.hitSound = "explosion";
        return fireball;
    }

    // Space junk flying sideways (direction -1 = to the left, 1 = to the right)
    public static Meteor SpawnJunk(Vector2 position, float direction)
    {
        Vector2 velocity = new Vector2(direction * Random.Range(6f, 9f), Random.Range(-0.5f, 0.5f));
        Meteor junk = Make(position, "debris", 32, 1.1f, velocity, 2, new Color(0.6f, 0.62f, 0.66f));
        junk.body.angularVelocity = Random.Range(-200f, 200f);
        junk.burstColor = new Color(0.7f, 0.72f, 0.75f);
        junk.hitSound = Sfx.Get("metal_hit") != null ? "metal_hit" : "hit";
        return junk;
    }

    static Meteor Make(Vector2 position, string spriteName, int pixels, float diameter, Vector2 velocity, int damage, Color fallback)
    {
        GameObject go = new GameObject(spriteName);
        go.transform.position = position;
        SpriteLib.Show(go.transform, "Look", SpriteLib.Get(spriteName, fallback, pixels, pixels), Vector2.one * diameter, 7);

        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.radius = diameter * 0.42f;

        Meteor meteor = go.AddComponent<Meteor>();
        meteor.damage = damage;
        meteor.body = go.AddComponent<Rigidbody2D>();
        meteor.body.bodyType = RigidbodyType2D.Kinematic;
        meteor.body.linearVelocity = velocity;
        return meteor;
    }

    private bool used;

    void OnCollisionEnter2D(Collision2D collision)
    {
        RocketPart part = collision.collider.GetComponent<RocketPart>();
        if (part == null || used)
            return;

        // Only hits one part, even if it touches two at the same time
        used = true;
        GetComponent<Collider2D>().enabled = false;
        part.TakeHit(damage);

        if (RocketeeGame.Instance != null)
            RocketeeGame.Instance.Effects.Burst(transform.position, burstColor, 10);
        Sfx.Play(hitSound);
        Destroy(gameObject);
    }

    void Update()
    {
        // Cleans up things far away from the camera
        Camera cam = Camera.main;
        if (cam == null)
            return;
        Vector3 offset = transform.position - cam.transform.position;
        if (offset.y < -40f || Mathf.Abs(offset.x) > 60f)
            Destroy(gameObject);
    }
}
