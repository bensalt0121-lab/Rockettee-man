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

        // The coin magnet pulls coins to the rocket
        RocketeeGame game = RocketeeGame.Instance;
        Rocket rocket = game != null ? game.Rocket : null;
        if (rocket != null && rocket.Flying && !rocket.Crashed && rocket.MagnetRange > 0f && rocket.Seat != null)
        {
            Vector3 target = rocket.Seat.transform.position;
            if (Vector2.Distance(transform.position, target) < rocket.MagnetRange)
                transform.position = Vector3.MoveTowards(transform.position, target, 14f * Time.deltaTime);
        }

        Camera cam = Camera.main;
        if (cam != null && transform.position.y < cam.transform.position.y - 40f)
            Destroy(gameObject);
    }

    private bool taken;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || other.GetComponent<RocketPart>() == null)
            return;

        // Counted only once, even if it touches two parts
        taken = true;
        GetComponent<Collider2D>().enabled = false;

        RocketeeGame game = RocketeeGame.Instance;
        if (game != null)
        {
            // Lucky Coins upgrade makes every coin worth more
            game.Flight.AddCoin(Mathf.RoundToInt(value * game.Data.CoinMultiplier));
            game.Effects.Burst(transform.position, new Color(1f, 0.85f, 0.2f), 6);
        }

        Sfx.Play("coin");
        Destroy(gameObject);
    }
}
