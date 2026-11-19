/*********************************************************************************************
 * COMPONENT OF: Rocketee Man (the player, made by the game if it is not in the scene)
 * REQUIRED DEPENDENCIES: Rigidbody 2D, Capsule Collider 2D, RocketeeGame, Input System package,
 *                        sprites player_idle, player_walk1, player_walk2, player_jump
 * DESCRIPTION: 2D platformer controls in the yard. A/D (or arrows) run, Space jumps, E uses the
 *              shop or the launch pad, F climbs into the rocket. When you sit in the rocket
 *              the player is hidden and the pilot is drawn in the seat instead.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class YardPlayer : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    // A bit higher than before so you can reach the floating platforms
    public float jumpPower = 12.5f;
    public float gravityScale = 3f;
    // You can still jump this long after running off an edge
    public float coyoteTime = 0.1f;

    [Header("Size (units)")]
    public Vector2 bodySize = new Vector2(0.6f, 1.4f);
    public Vector2 spriteSize = new Vector2(1f, 1.5f);

    public bool Hidden { get; private set; }

    private Rigidbody2D body;
    private CapsuleCollider2D capsule;
    private SpriteRenderer look;
    private Sprite idle, walk1, walk2, jump;
    private float moveInput;
    private float coyoteTimer;
    private float animTimer;
    private bool grounded;
    private readonly List<Collider2D> hits = new List<Collider2D>();
    private ContactFilter2D solidOnly;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = gravityScale;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        capsule = GetComponent<CapsuleCollider2D>();
        if (capsule == null)
            capsule = gameObject.AddComponent<CapsuleCollider2D>();
        capsule.size = bodySize;

        // The character and color picked in the menu
        idle = Settings.PlayerSprite("idle");
        walk1 = Settings.PlayerSprite("walk1");
        walk2 = Settings.PlayerSprite("walk2");
        jump = Settings.PlayerSprite("jump");

        look = GetComponentInChildren<SpriteRenderer>();
        if (look == null)
            look = SpriteLib.Show(transform, "Look", idle, spriteSize, 10);
        look.color = Settings.PlayerColor;
        // The picture is a bit taller than the collider, with the feet at the bottom
        look.transform.localPosition = new Vector3(0f, (spriteSize.y - bodySize.y) * 0.5f, 0f);

        solidOnly = new ContactFilter2D();
        solidOnly.useTriggers = false;
    }

    void Update()
    {
        RocketeeGame game = RocketeeGame.Instance;
        bool canControl = game != null && game.State == GameState.Yard && !Hidden && !game.Paused;
        Keyboard keyboard = Keyboard.current;

        moveInput = 0f;
        if (canControl && keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                moveInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                moveInput += 1f;
        }

        CheckGround();

        if (canControl && keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) && coyoteTimer > 0f)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);
            coyoteTimer = 0f;
            Sfx.Play("jump", 0.6f);
        }

        if (canControl && !game.JustChanged && game.UI != null)
            CheckInteract(game, keyboard);

        Animate();
    }

    void FixedUpdate()
    {
        if (Hidden)
            return;
        body.linearVelocity = new Vector2(moveInput * moveSpeed, body.linearVelocity.y);
    }

    // Standing on something solid that is not the player?
    void CheckGround()
    {
        Vector2 feet = (Vector2)transform.position + capsule.offset + Vector2.down * (bodySize.y * 0.5f);
        int count = Physics2D.OverlapBox(feet, new Vector2(bodySize.x * 0.8f, 0.1f), 0f, solidOnly, hits);

        grounded = false;
        for (int i = 0; i < count; i++)
        {
            // The rocket does not count: the player walks through it in the yard
            if (hits[i] != capsule && hits[i].attachedRigidbody != body && hits[i].GetComponent<RocketPart>() == null)
            {
                grounded = true;
                break;
            }
        }

        if (grounded && body.linearVelocity.y <= 0.1f)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;
    }

    void CheckInteract(RocketeeGame game, Keyboard keyboard)
    {
        Interactable nearest = Interactable.Nearest(transform.position);
        if (nearest == null)
            return;

        game.UI.ShowPrompt(nearest.Prompt);

        if (keyboard == null)
            return;
        if (keyboard.eKey.wasPressedThisFrame)
            nearest.Interact();
        else if (keyboard.fKey.wasPressedThisFrame)
            nearest.AltInteract();
    }

    void Animate()
    {
        if (look == null)
            return;

        if (moveInput != 0f)
            look.flipX = moveInput < 0f;

        Sprite frame = idle;
        if (!grounded)
        {
            frame = jump;
        }
        else if (moveInput != 0f)
        {
            animTimer += Time.deltaTime;
            frame = Mathf.FloorToInt(animTimer / 0.15f) % 2 == 0 ? walk1 : walk2;
        }

        if (look.sprite != frame)
        {
            look.sprite = frame;
            SpriteLib.Fit(look, spriteSize);
        }
    }

    // Hides the player while they sit in the rocket
    public void SitDown()
    {
        Hidden = true;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        look.enabled = false;
    }

    // Puts the player back in the yard after a flight
    public void StandUp(Vector2 position)
    {
        Hidden = false;
        transform.position = position;
        body.simulated = true;
        body.linearVelocity = Vector2.zero;
        look.enabled = true;
    }

    public Collider2D Collider => capsule;

    // Makes the player standing on the ground at x. The body, collider and picture are added
    // here too, so the player also shows up in the Scene view (Tools > Rocketee Man > Build Everything).
    public static YardPlayer Create(float x)
    {
        GameObject go = new GameObject("Rocketee Man");
        go.transform.position = new Vector3(x, YardGround.TopAt(x) + 0.8f, 0f);

        Vector2 body = new Vector2(0.6f, 1.4f);
        Vector2 picture = new Vector2(1f, 1.5f);

        Rigidbody2D rigidbody = go.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = 3f;
        rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

        CapsuleCollider2D capsule = go.AddComponent<CapsuleCollider2D>();
        capsule.size = body;

        SpriteRenderer look = SpriteLib.Show(go.transform, "Look", Settings.PlayerSprite("idle"), picture, 10);
        look.transform.localPosition = new Vector3(0f, (picture.y - body.y) * 0.5f, 0f);

        return go.AddComponent<YardPlayer>();
    }
}
