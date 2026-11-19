/*********************************************************************************************
 * COMPONENT OF: meteors (made by FlightDirector during a flight)
 * REQUIRED DEPENDENCIES: Rigidbody 2D (kinematic), Circle Collider 2D, RocketPart,
 *                        sprites meteor_small, meteor_med, meteor_big
 * DESCRIPTION: A space rock that slowly falls and spins. If it hits the rocket it breaks the
 *              part it touched (a big meteor hits twice as hard) and bumps the rocket.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public class Meteor : MonoBehaviour
{
    // 1 = small, 2 = medium, 3 = big
    public int size = 2;

    private Rigidbody2D body;

    public static Meteor Spawn(Vector2 position, int size)
    {
        string spriteName = size == 1 ? "meteor_small" : size == 2 ? "meteor_med" : "meteor_big";
        int pixels = size == 1 ? 16 : size == 2 ? 32 : 64;
        float diameter = size == 1 ? 0.6f : size == 2 ? 1.1f : 2f;

        GameObject go = new GameObject("Meteor");
        go.transform.position = position;
        SpriteLib.Show(go.transform, "Look", SpriteLib.Get(spriteName, new Color(0.5f, 0.4f, 0.32f), pixels, pixels), Vector2.one * diameter, 7);

        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.radius = diameter * 0.42f;

        Meteor meteor = go.AddComponent<Meteor>();
        meteor.size = size;

        meteor.body = go.AddComponent<Rigidbody2D>();
        meteor.body.bodyType = RigidbodyType2D.Kinematic;
        meteor.body.linearVelocity = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(-3f, -0.5f));
        meteor.body.angularVelocity = Random.Range(-90f, 90f);
        return meteor;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        RocketPart part = collision.collider.GetComponent<RocketPart>();
        if (part == null)
            return;

        part.TakeHit(size >= 3 ? 2 : 1);

        if (RocketeeGame.Instance != null)
            RocketeeGame.Instance.Effects.Burst(transform.position, new Color(0.55f, 0.45f, 0.35f), 10);
        Sfx.Play("hit");
        Destroy(gameObject);
    }

    void Update()
    {
        // Cleans up meteors far below the camera
        Camera cam = Camera.main;
        if (cam != null && transform.position.y < cam.transform.position.y - 40f)
            Destroy(gameObject);
    }
}
