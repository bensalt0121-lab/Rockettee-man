/*********************************************************************************************
 * COMPONENT OF: Launch Pad (made by the game, or by Tools > Rocketee Man > Add Yard Objects To Scene)
 * REQUIRED DEPENDENCIES: RocketeeGame, sprite "launch_pad"
 * DESCRIPTION: The metal platform where you build your rocket. E = build mode,
 *              F = climb into the seat and fly. The build grid sits on top of the pad.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public class LaunchPad : Interactable
{
    public const float Width = 3f;
    public const float Height = 0.5f;

    // Middle of the top of the pad: the bottom middle of the build grid
    public Vector2 GridOrigin => (Vector2)transform.position + Vector2.up * Height;

    public override Vector2 InteractPoint => (Vector2)transform.position + Vector2.up * 0.8f;

    public override string Prompt
    {
        get
        {
            RocketeeGame game = RocketeeGame.Instance;
            if (game != null && game.Data.Placed(PartType.Seat) > 0)
                return "E: Build your rocket     F: Get in and fly!";
            return "E: Build your rocket";
        }
    }

    public override void Interact()
    {
        if (RocketeeGame.Instance != null)
            RocketeeGame.Instance.EnterBuild();
    }

    public override void AltInteract()
    {
        if (RocketeeGame.Instance != null)
            RocketeeGame.Instance.BoardRocket();
    }

    // Makes a launch pad standing on the ground at x
    public static LaunchPad Create(float x)
    {
        GameObject go = new GameObject("Launch Pad");
        go.transform.position = new Vector3(x, YardGround.TopAt(x), 0f);

        Sprite sprite = SpriteLib.Get("launch_pad", new Color(0.5f, 0.52f, 0.55f), 96, 16);
        SpriteRenderer look = SpriteLib.Show(go.transform, "Look", sprite, new Vector2(Width, Height), 2);
        look.transform.localPosition = new Vector3(0f, Height * 0.5f, 0f);

        BoxCollider2D solid = go.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(Width, Height);
        solid.offset = new Vector2(0f, Height * 0.5f);

        LaunchPad pad = go.AddComponent<LaunchPad>();
        pad.range = 2.4f;
        return pad;
    }
}
