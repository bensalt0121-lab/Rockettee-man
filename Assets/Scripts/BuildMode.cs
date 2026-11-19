/*********************************************************************************************
 * COMPONENT OF: Rocketee Game (added by the game)
 * REQUIRED DEPENDENCIES: RocketeeGame, Rocket, LaunchPad, GameUI, Input System package
 * DESCRIPTION: Building the rocket like Lego (or the pig's cart in Bad Piggies). A grid sits on
 *              the launch pad. Pick a part in the bar at the bottom (or press 1-7), then
 *              left click (or hold and drag) on the grid to place it, right click to take it
 *              off. Every part must touch the seat; loose parts turn red and stay behind.
 *              While testing in the Unity editor, B auto-builds a balanced rocket from all
 *              your parts (a developer shortcut, not in the real game).
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildMode : MonoBehaviour
{
    public bool Active { get; private set; }
    public PartType Selected { get; private set; } = PartType.Seat;

    private RocketeeGame game;
    private Transform gridRoot;
    private SpriteRenderer ghost;
    private readonly List<SpriteRenderer> cells = new List<SpriteRenderer>();

    void Awake()
    {
        game = GetComponent<RocketeeGame>();
    }

    public void Begin()
    {
        Active = true;
        MakeGrid();

        // Start with the seat if it is not placed yet, otherwise the first part you have
        if (game.Data.Available(PartType.Seat) > 0)
            Select(PartType.Seat);
        else
            SelectFirstAvailable();

        game.Rocket.Rebuild(game.Data, false);
    }

    public void End()
    {
        Active = false;
        if (gridRoot != null)
            Destroy(gridRoot.gameObject);
        cells.Clear();
        ghost = null;
    }

    public void Select(PartType type)
    {
        Selected = type;
        if (ghost != null)
        {
            PartInfo info = PartCatalog.Get(type);
            ghost.sprite = SpriteLib.Get(info.sprite, info.color);
            SpriteLib.Fit(ghost, Vector2.one);
        }
        if (game.UI != null)
            game.UI.RefreshBuildBar();
    }

    void SelectFirstAvailable()
    {
        foreach (PartInfo info in PartCatalog.Parts)
        {
            if (game.Data.Available(info.type) > 0)
            {
                Select(info.type);
                return;
            }
        }
        Select(PartType.Cola);
    }

    void MakeGrid()
    {
        gridRoot = new GameObject("Build Grid").transform;
        gridRoot.position = game.Pad.GridOrigin;

        Sprite outline = SpriteLib.Outline(32, new Color(1f, 1f, 1f, 0.55f));
        int half = game.Data.GridSize / 2;

        for (int y = 0; y < game.Data.GridSize; y++)
        {
            for (int x = -half; x <= half; x++)
            {
                SpriteRenderer cell = SpriteLib.Show(gridRoot, "Cell", outline, Vector2.one, 15);
                cell.transform.localPosition = Rocket.CellToLocal(new Vector2Int(x, y));
                cells.Add(cell);
            }
        }

        PartInfo info = PartCatalog.Get(Selected);
        ghost = SpriteLib.Show(gridRoot, "Ghost", SpriteLib.Get(info.sprite, info.color), Vector2.one, 20);
    }

    void Update()
    {
        if (!Active)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null)
        {
            for (int i = 0; i < PartCatalog.Count && i < 9; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                    Select((PartType)i);
            }

#if UNITY_EDITOR
            // Testing shortcut for the game developer only (not in the real game)
            if (keyboard.bKey.wasPressedThisFrame)
                AutoBuild();
#endif

            bool done = keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
            if (done && !game.JustChanged)
            {
                game.ExitBuild();
                return;
            }
        }

        if (mouse == null || ghost == null)
            return;

        bool overButtons = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        Vector2Int cell = MouseCell(mouse);
        bool inside = InGrid(cell) && !overButtons;

        ghost.gameObject.SetActive(inside);
        if (!inside)
            return;

        ghost.transform.localPosition = Rocket.CellToLocal(cell);
        bool canPlace = CanPlace(cell, Selected);
        ghost.color = canPlace ? new Color(1f, 1f, 1f, 0.6f) : new Color(1f, 0.3f, 0.3f, 0.35f);

        // Hold and drag to place many parts, like painting
        if (mouse.leftButton.isPressed && canPlace)
            Place(cell);
        else if (mouse.rightButton.isPressed && game.Data.At(cell.x, cell.y) != null)
            Remove(cell);
    }

    Vector2Int MouseCell(Mouse mouse)
    {
        Camera cam = Camera.main;
        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 local = (Vector2)world - game.Pad.GridOrigin;
        return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.FloorToInt(local.y));
    }

    bool InGrid(Vector2Int cell)
    {
        int half = game.Data.GridSize / 2;
        return cell.x >= -half && cell.x <= half && cell.y >= 0 && cell.y < game.Data.GridSize;
    }

    bool CanPlace(Vector2Int cell, PartType type)
    {
        return game.Data.At(cell.x, cell.y) == null && game.Data.Available(type) > 0;
    }

    void Place(Vector2Int cell)
    {
        game.Data.layout.Add(new PlacedPart { x = cell.x, y = cell.y, type = Selected });
        game.Rocket.Rebuild(game.Data, false);
        Sfx.Play("place", 0.6f);

        // Out of this part? Switch to one you still have
        if (game.Data.Available(Selected) <= 0)
            SelectFirstAvailable();
        else
            game.UI.RefreshBuildBar();
    }

    void Remove(Vector2Int cell)
    {
        PlacedPart part = game.Data.At(cell.x, cell.y);
        game.Data.layout.Remove(part);
        game.Rocket.Rebuild(game.Data, false);
        Sfx.Play("remove", 0.6f);
        game.UI.RefreshBuildBar();
    }

    // Builds a balanced rocket from every part you own. Left and right always match so it
    // flies straight: seat at the bottom middle, colas in pairs, crates on top as armor,
    // fins on the sides and the nose cone on the very top. You can change it afterwards.
    public void AutoBuild()
    {
        GameSave save = game.Data;
        save.layout.Clear();

        int size = save.GridSize;
        int half = size / 2;
        int[] left = new int[PartCatalog.Count];
        for (int i = 0; i < left.Length; i++)
            left[i] = save.Owned((PartType)i);

        bool Free(int x, int y) => x >= -half && x <= half && y >= 0 && y < size && save.At(x, y) == null;
        bool Filled(int x, int y) => save.At(x, y) != null;
        void Put(int x, int y, PartType type)
        {
            save.layout.Add(new PlacedPart { x = x, y = y, type = type });
            left[(int)type]--;
        }
        int Top(int x)
        {
            for (int y = size - 1; y >= 0; y--)
            {
                if (Filled(x, y))
                    return y;
            }
            return -1;
        }

        if (left[(int)PartType.Seat] <= 0)
        {
            game.Rocket.Rebuild(save, false);
            game.UI.Say("You need a seat first. Buy one in the shop!", 3f);
            Sfx.Play("error");
            return;
        }
        Put(0, 0, PartType.Seat);

        // Colas in matching pairs: next to the seat first, then rows on top of them
        int widthBelow = half;
        for (int y = 0; y < size; y++)
        {
            int width = 0;
            for (int c = 1; c <= Mathf.Min(half, widthBelow); c++)
            {
                PartType engine = left[(int)PartType.BigCola] >= 2 ? PartType.BigCola : PartType.Cola;
                if (left[(int)engine] < 2 || !Free(-c, y) || !Free(c, y))
                    break;
                Put(-c, y, engine);
                Put(c, y, engine);
                width = c;
            }
            if (width == 0)
                break;
            widthBelow = width;
        }

        // An odd cola goes right above the seat, in the middle, so it still pushes straight
        foreach (PartType engine in new[] { PartType.BigCola, PartType.Cola })
        {
            while (left[(int)engine] > 0)
            {
                int y = Top(0) + 1;
                if (!Free(0, y))
                    break;
                Put(0, y, engine);
            }
        }

        // Fins on the outside of the bottom row
        int edge = 0;
        while (Filled(edge + 1, 0) && Filled(-(edge + 1), 0))
            edge++;
        if (left[(int)PartType.Fin] >= 2 && Free(-(edge + 1), 0) && Free(edge + 1, 0))
        {
            Put(-(edge + 1), 0, PartType.Fin);
            Put(edge + 1, 0, PartType.Fin);
        }

        // Crates stacked on top of every column as armor (metal ones first)
        bool placedSomething = true;
        while (placedSomething)
        {
            placedSomething = false;
            foreach (PartType crate in new[] { PartType.MetalCrate, PartType.Crate })
            {
                if (left[(int)crate] >= 1 && Free(0, Top(0) + 1))
                {
                    Put(0, Top(0) + 1, crate);
                    placedSomething = true;
                }

                for (int c = 1; c <= half; c++)
                {
                    int topLeft = Top(-c);
                    int topRight = Top(c);
                    if (left[(int)crate] < 2 || topLeft < 0 || topLeft != topRight)
                        continue;
                    if (!Free(-c, topLeft + 1) || !Free(c, topRight + 1))
                        continue;
                    Put(-c, topLeft + 1, crate);
                    Put(c, topRight + 1, crate);
                    placedSomething = true;
                }
            }
        }

        // Nose cone on the very top
        if (left[(int)PartType.Nose] > 0 && Free(0, Top(0) + 1))
            Put(0, Top(0) + 1, PartType.Nose);

        // Safety: anything not touching the seat is taken off again
        HashSet<Vector2Int> connected = Rocket.ConnectedCells(save.layout);
        save.layout.RemoveAll(p => !connected.Contains(new Vector2Int(p.x, p.y)));

        game.Rocket.Rebuild(save, false);
        SelectFirstAvailable();
        Sfx.Play("place");

        string problem = Rocket.ProblemWith(save);
        game.UI.Say(problem ?? "Auto built a rocket with " + save.layout.Count + " parts!", 3f);
    }

    // Takes every part off the rocket
    public void ClearAll()
    {
        game.Data.layout.Clear();
        game.Rocket.Rebuild(game.Data, false);
        Select(PartType.Seat);
        Sfx.Play("remove");
    }

    // Text under the build bar telling you what to fix
    public string Advice()
    {
        GameSave save = game.Data;
        if (save.Placed(PartType.Seat) == 0)
            return save.Owned(PartType.Seat) > 0 ? "Put your seat on the grid first." : "Buy a seat in the shop first.";

        HashSet<Vector2Int> connected = Rocket.ConnectedCells(save.layout);
        if (connected.Count < save.layout.Count)
            return "Red parts don't touch the seat - they will stay behind!";

        string problem = Rocket.ProblemWith(save);
        if (problem != null)
            return "Add a cola next to the seat so it can fly.";

        return "Looks good! Press E when you are done, then F at the pad to fly.";
    }
}
