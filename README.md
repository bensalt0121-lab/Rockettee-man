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

1. **Yard (2D platformer):** a simple flat desert yard (32 tiles wide): the shop on the left,
   the launch pad in the middle, the cacti on the right. Run with **A/D**, jump with **Space**.
   Walk into the free parts lying around (a seat, 2 colas, 2 crates) to pick them up.
2. **Shop (left side):** press **E** and click **Buy** for more parts and upgrades.
3. **Build (launch pad):** press **E**. Pick a part in the bar (or keys 1-7), click or drag
   it onto the grid, right click to remove. Parts must touch the seat (loose ones turn red).
   **Crates are full of spare cola:** every cola with a crate beside it or on top of it sprays
   longer (a green + shows on the cola for each crate).
   (Developer shortcut: while testing in the Unity editor, **B** auto-builds a balanced rocket.)
4. **Fly:** press **F** at the launch pad to sit in the seat. Hold **Space** to spray cola,
   **A/D** to tilt (let go and the rocket straightens itself). Meteors start at 150 m. Crates
   take hits for you; the seat can take one hit, the second one is a crash.
   On the way up you pass birds, hot air balloons, planes, satellites, planets and a UFO.
5. **Money:** every flight pays $1 per meter + the coins you fly through, plus a one-time bonus the
   first time you pass 100, 250, 500 and 750 m. After a flight, the results screen has quick
   buttons: **Fly again (F)**, **Build (B)**, **Shop (S)** - no walking back across the yard.
6. **Win:** reach the Moon, then Mars, then the Sun (see Stages).

Other keys: **Esc** pause (volume, back to menu), **R** give up a flight, **F12** twice in the
yard deletes all progress (for testing).

## Stages

Everything works the same in every stage (yard, shop, building, flying), but each trip is longer
and more dangerous, so you need better parts to protect you, your cola and your rocket.

| Stage | Trip | Danger | Protection |
| --- | --- | --- | --- |
| 1 | Earth -> the Moon (1000 m) | meteors | crates, metal crates |
| 2 | Moon -> Mars (2500 m) | meteor storm + space junk flying sideways | Titanium crate, Space Helmet, Bubble shield |
| 3 | Mars -> the Sun (4000 m) | fireballs + heat (colas boil, you can overheat) | Heat Shield, Cooler, Space Helmet |

The yard changes with the stage: grey moon ground with craters and a flag (low gravity, big
jumps, Earth in the sky), then red Mars ground. Reaching the goal shows a "stage cleared" screen
and takes you to the next stage.

## Parts

| Part | What it does |
| --- | --- |
| Seat | You sit here. Two hits on the seat = crash (Space Helmet adds hits) |
| Cola / 2L Cola | Engines + fuel (hold Space) |
| Mentos Boost | Huge push for 1 second |
| Crate / Metal Crate | Spare cola: colas touching it spray 1.2 s longer. Armor: blocks 1 / 3 hits |
| Cola Tank / 4L Tank | Spare cola for ALL colas (2 / 4 colas worth) once their own bottle is empty |
| Fin / Wing | Less spinning; wings also give a little lift |
| Nose Cone | Less air drag |
| Coin Magnet | Pulls coins into the rocket |
| Titanium (stage 2) | Blocks 6 hits, spare cola inside |
| Bubble (stage 2) | Blocks one hit, recharges in 6 s |
| Cooler (stage 3) | Colas and tanks touching it don't boil near the Sun |
| Heat Shield (stage 3) | The rocket heats up 22% slower |

Upgrades (8): **Shake It Harder** (+17% push), **Bigger Bottles** (+22% spray time), **Bigger
Launch Pad** (grid 5x5 up to 11x11), **Lucky Coins** (+30% coin money), **Better Steering**,
**Sponsor Deal** (+15% money per meter), and from stage 2 **Space Helmet** and **Tougher Crates**.
Parts get a little more expensive for every one you own. All numbers are in
`Assets/Scripts/PartCatalog.cs` and `Assets/Scripts/Stages.cs`; the balance was tested with a
flight simulator.

## Dev mode

Menu > Settings > **Dev Mode**, type the dev password and press Enter. In the game press **F10**
(or Pause > Dev Tools): add money, get 10 of every part, max all upgrades, god mode, infinite
cola, and jump to any stage. The password is not stored in plain text in the code.

## Tools menu (Tools > Rocketee Man)

| Menu item | What it does |
| --- | --- |
| **Build Everything** | Builds the whole game into the scenes in one click: Play gets a simple flat yard painted with my sand tiles, bigger cacti and desert decorations, Rocketee Game (settings in the Inspector), the shop, the launch pad, the starter parts and the player; Menu gets the Main Menu. Fixes sprite import and the scene list, then saves. Safe to run again (only adds what is missing). |
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
| FlightDirector | Meteors, fireballs, space junk, coins, clouds, the goal, end of the flight |
| Stages | The three stages: distances, dangers and how each one looks |
| Scenery | Sky gradient, stars, sun, growing Moon, planets, parallax mountains and dunes, birds/balloons/planes/satellites/UFO, tumbleweeds |
| YardBuilder | Makes the simple flat yard with my tiles, bigger cacti, decorations, where things go |
| GameUI / UIKit | Everything on screen |
| PartCatalog / GameSave / Settings | Part numbers, saved progress, menu settings |
| MusicPlayer / Sfx | One long chill song on repeat, realistic sound effects (plus wind when flying fast) |

## Credits

Art and sounds are from free online packs (mostly Kenney, CC0) plus pixel art drawn for this
project; the full list is in `Assets/Resources/CREDITS.txt`.
My own art: the sand tiles and the cacti.
