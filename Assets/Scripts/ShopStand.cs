/*********************************************************************************************
 * COMPONENT OF: Shop (made by the game, or by Tools > Rocketee Man > Build Everything)
 * REQUIRED DEPENDENCIES: RocketeeGame, sprite "shop"
 * DESCRIPTION: The market stall in the yard. Press E next to it to buy parts and upgrades.
 *              You can move it anywhere on the ground in the Scene view.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.1
 * VERSION 1.1: 1.5 times bigger.
 *********************************************************************************************/
using UnityEngine;

public class ShopStand : Interactable
{
    // How much bigger than the 64x64 pixel picture (1 = 2x2 units)
    public const float Scale = 1.5f;

    public override Vector2 InteractPoint => (Vector2)transform.position + Vector2.up * 0.8f;

    public override string Prompt => "E: Open the shop";

    public override void Interact()
    {
        if (RocketeeGame.Instance != null)
            RocketeeGame.Instance.OpenShop();
    }

    // Makes a shop standing on the ground at x
    public static ShopStand Create(float x)
    {
        GameObject go = new GameObject("Shop");
        go.transform.position = new Vector3(x, YardGround.TopAt(x), 0f);
        ShopStand shop = go.AddComponent<ShopStand>();
        shop.FixSize();
        return shop;
    }

    // Makes the picture the right size (also fixes shops made before it got bigger)
    public void FixSize()
    {
        Sprite sprite = SpriteLib.Get("shop", new Color(0.9f, 0.55f, 0.2f), 64, 64);
        Vector2 size = SpriteLib.UnitSize(sprite) * Scale;

        Transform lookTransform = transform.Find("Look");
        SpriteRenderer look = lookTransform != null ? lookTransform.GetComponent<SpriteRenderer>() : null;
        if (look == null)
            look = SpriteLib.Show(transform, "Look", sprite, size, 1);

        SpriteLib.Fit(look, size);
        look.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
        range = 2.4f;
    }
}
