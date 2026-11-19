/*********************************************************************************************
 * COMPONENT OF: base class for things you can use in the yard (Shop Stand, Launch Pad)
 * REQUIRED DEPENDENCIES: YardPlayer (it finds the nearest one and shows its prompt)
 * DESCRIPTION: Anything the player can walk up to and use. E calls Interact(), F calls
 *              AltInteract(). Prompt is the text shown at the bottom of the screen.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> All = new List<Interactable>();

    // How close (in units) the player must be
    public float range = 1.8f;

    protected virtual void OnEnable()
    {
        All.Add(this);
    }

    protected virtual void OnDisable()
    {
        All.Remove(this);
    }

    public virtual Vector2 InteractPoint => transform.position;

    public abstract string Prompt { get; }

    public abstract void Interact();

    public virtual void AltInteract()
    {
    }

    // The closest one in range, or null
    public static Interactable Nearest(Vector2 from)
    {
        Interactable best = null;
        float bestDistance = float.MaxValue;

        foreach (Interactable item in All)
        {
            float distance = Vector2.Distance(from, item.InteractPoint);
            if (distance <= item.range && distance < bestDistance)
            {
                best = item;
                bestDistance = distance;
            }
        }
        return best;
    }
}
