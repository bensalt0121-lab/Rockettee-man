# Rocketee Man

A 2D pixel art game made in Unity for my game design class (theme: **Sprite Fly**).
Instead of just flying a plane, you build your own rocket out of cola bottles, crates and a
seat, spray the cola to fly up, dodge meteors and try to reach the Moon.

Made by Ben Xiao, with help from Claude (AI coding assistant).

## Scenes

| Scene | What it is |
| --- | --- |
| `Assets/Scenes/Menu.unity` | Start screen: Play, Character (pick your character and color), Settings (music, sounds, fullscreen, reset progress) |
| `Assets/Scenes/Play.unity` | The game: the desert yard, the shop, the launch pad and the sky up to the Moon |

Open **Menu** and press Play. (You can also press Play in the Play scene to skip the menu.)
The menu, the shop, the launch pad, the starter parts, the UI and everything in the sky are
made by the scripts when the game starts, so the scenes only hold the level art.

## How to play

1. **Yard (2D platformer):** run with **A/D**, jump with **Space**. Walk into the free parts
   lying around (a seat, 2 colas, 2 crates) to pick them up.
2. **Shop (left side):** press **E** and click **Buy** for more parts and upgrades.
3. **Build (launch pad):** press **E**. Pick a part in the bar (or keys 1-7), click or drag
   it onto the grid, right click to remove. Parts must touch the seat (loose ones turn red).
   (Developer shortcut: while testing in the Unity editor, **B** auto-builds a balanced rocket.)
4. **Fly:** press **F** at the launch pad to sit in the seat. Hold **Space** to spray cola,
   **A/D** to tilt. Meteors start at 120 m. Crates take hits for you; if the seat is hit you crash.
5. **Money:** every flight pays $1 per meter + the coins you fly through. Buy more and fly higher.
6. **Win:** reach the Moon at 1000 m.

Other keys: **Esc** pause (volume, back to menu), **R** give up a flight, **F12** twice in the
yard deletes all progress (for testing).

## Parts

| Part | What it does |
| --- | --- |
| Seat | You sit here. A meteor hit on the seat = crash |
| Cola | Engine + fuel (hold Space) |
| 2L Cola | Bigger engine, more push, sprays longer |
| Crate / Metal Crate | Armor: takes 1 / 3 meteor hits |
| Fin | Less spinning |
| Nose Cone | Less air drag |

Upgrades: **Shake It Harder** (+15% push), **Bigger Bottles** (+20% spray time),
**Bigger Launch Pad** (build grid 5x5 -> 7x7 -> 9x9). Parts get a bit more expensive for
every one you own. All numbers are in `Assets/Scripts/PartCatalog.cs`; the balance was
tested with a flight simulator (first rocket about 50 m, around 15-20 flights to the Moon).

## Tools menu (Tools > Rocketee Man)

| Menu item | What it does |
| --- | --- |
| **Build Everything** | Builds the whole game into the scenes in one click: Play gets Rocketee Game (settings in the Inspector), the shop, the launch pad, the starter parts and the player; Menu gets the Main Menu. Fixes sprite import and the scene list, then saves. Safe to run again (only adds what is missing). |
| **Build Web Game** | Builds the whole game for the web in one click with the class settings (Company San Domenico School, Product Rocketee Man, Version 1.0, Gzip, Decompression Fallback, scenes Menu + Play) into `Builds/RocketeeMan_BenXiao`, checks it is under 2 GB and opens the folder. Upload that folder to Google Drive. |
| Build and Run Web Game | Same, then opens the game in the browser (localhost) to test it |
| Open Builds Folder | Opens the `Builds` folder |
| Add Yard Objects To Scene | Puts the shop, launch pad and starter parts into the open scene so you can move them by hand |
| Fix Pixel Art Import | Makes every picture in `Assets/Resources/Sprites` sharp pixel art again (32 PPU, Point filter) |
| Reset Saved Progress | Deletes money, parts and best height |

Pictures put in `Assets/Resources/Sprites` are set up as pixel art automatically.
The build scene list (0 = Menu, 1 = Play) is also set automatically.

## Scripts (`Assets/Scripts`)

| Script | What it does |
| --- | --- |
| SceneBoot | Adds the menu or the game when a scene loads |
| MainMenu | Start screen, character picker, settings |
| RocketeeGame | The boss: yard setup, money, saving, switching between yard/shop/build/flight |
| YardPlayer | Platformer controls |
| BuildMode | The build grid and Auto Build |
| Rocket / RocketPart | The rocket's physics, spraying, getting hit |
| FlightDirector | Meteors, coins, sky color, stars, end of the flight |
| GameUI / UIKit | Everything on screen |
| PartCatalog / GameSave / Settings | Part numbers, saved progress, menu settings |
| MusicPlayer / Sfx | One long chill song on repeat, sound effects |

## Credits

Art and sounds are from free online packs (mostly Kenney, CC0) plus pixel art drawn for this
project; the full list is in `Assets/Resources/CREDITS.txt`.
My own art: the sand tiles and the cacti.
