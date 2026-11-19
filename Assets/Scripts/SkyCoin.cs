/*********************************************************************************************
 * COMPONENT OF: coins in the sky (made by FlightDirector during a flight)
 * REQUIRED DEPENDENCIES: Circle Collider 2D (Is Trigger), RocketPart, sprite "coin"
 * DESCRIPTION: A spinning coin floating in the sky. Fly the rocket through it to get extra
 *              money at the end of the flight. Coins higher up are worth more.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public class SkyCoin : MonoBehaviour
{
    public int value = 5;

    private Transform look;

    public static SkyCoin Spawn(Vector2 position, int value)
    {
        GameObject go = new GameObject("Coin");
        go.transform.position = position;
        SpriteRenderer renderer = SpriteLib.Show(go.transform, "Look", SpriteLib.Get("coin", new Color(1f, 0.82f, 0.1f), 16, 16), Vector2.one * 0.6f, 7);

        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.45f;

        SkyCoin coin = go.AddComponent<SkyCoin>();
        coin.value = value;
        coin.look = renderer.transform;
        return coin;
    }

    void Update()
    {
        // Spins like a flipping coin
        if (look != null)
        {
            Vector3 scale = look.localScale;
            scale.x = Mathf.Abs(scale.y) * Mathf.Max(0.15f, Mathf.Abs(Mathf.Cos(Time.time * 4f)));
            look.localScale = scale;
        }

        Camera cam = Camera.main;
        if (cam != null && transform.position.y < cam.transform.position.y - 40f)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<RocketPart>() == null)
            return;

        RocketeeGame game = RocketeeGame.Instance;
        if (game != null)
        {
            game.Flight.AddCoin(value);
            game.Effects.Burst(transform.position, new Color(1f, 0.85f, 0.2f), 6);
        }

        Sfx.Play("coin");
        Destroy(gameObject);
    }
}
