/*********************************************************************************************
 * COMPONENT OF: Shop (made by the game, or by Tools > Rocketee Man > Add Yard Objects To Scene)
 * REQUIRED DEPENDENCIES: RocketeeGame, sprite "shop"
 * DESCRIPTION: The market stall in the yard. Press E next to it to buy parts and upgrades.
 *              You can move it anywhere on the ground in the Scene view.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public class ShopStand : Interactable
{
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

        Sprite sprite = SpriteLib.Get("shop", new Color(0.9f, 0.55f, 0.2f), 64, 64);
        Vector2 size = SpriteLib.UnitSize(sprite);
        SpriteRenderer look = SpriteLib.Show(go.transform, "Look", sprite, size, 1);
        look.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);

        ShopStand shop = go.AddComponent<ShopStand>();
        shop.range = 2f;
        return shop;
    }
}
