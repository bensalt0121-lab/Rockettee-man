/*********************************************************************************************
 * COMPONENT OF: Pickup objects in the yard (the free starter parts)
 * REQUIRED DEPENDENCIES: RocketeeGame, YardPlayer, a Circle Collider 2D set to Is Trigger
 * DESCRIPTION: A rocket part lying on the ground. Walk into it to pick it up. Each pickup has
 *              an id so it does not come back after you picked it up once.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public class PartPickup : MonoBehaviour
{
    // Must be different for every pickup
    public string id = "starter";
    public PartType type = PartType.Crate;

    private Transform look;
    private float bobOffset;

    void Start()
    {
        RocketeeGame game = RocketeeGame.Instance;
        if (game != null && game.Data.collected.Contains(id))
        {
            Destroy(gameObject);
            return;
        }

        look = transform.childCount > 0 ? transform.GetChild(0) : null;
        bobOffset = Random.value * 6f;
    }

    void Update()
    {
        // Floats up and down a little so you notice it
        if (look != null)
            look.localPosition = new Vector3(0f, 0.55f + Mathf.Sin(Time.time * 3f + bobOffset) * 0.08f, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<YardPlayer>() == null)
            return;

        RocketeeGame game = RocketeeGame.Instance;
        if (game == null)
            return;

        game.CollectStarterPart(id, type);
        Destroy(gameObject);
    }

    // Makes a pickup lying on the ground at x
    public static PartPickup Create(string id, PartType type, float x)
    {
        PartInfo info = PartCatalog.Get(type);

        GameObject go = new GameObject("Pickup - " + info.name);
        go.transform.position = new Vector3(x, YardGround.TopAt(x), 0f);

        SpriteRenderer look = SpriteLib.Show(go.transform, "Look", SpriteLib.Get(info.sprite, info.color), new Vector2(0.9f, 0.9f), 4);
        look.transform.localPosition = new Vector3(0f, 0.55f, 0f);

        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.55f;
        trigger.offset = new Vector2(0f, 0.55f);

        PartPickup pickup = go.AddComponent<PartPickup>();
        pickup.id = id;
        pickup.type = type;
        return pickup;
    }
}
