# Robot Dash

An endless runner starring **Bolt-E**, a tiny cute robot with headphones riding a hoverboard (designed from `hero-robot.png`). Made with Godot 4.7.2 (.NET / C#) and SQLite.

## How to play
- **Space**, **Up arrow**, **W**, or **click**: jump
- **Xbox controller** (or any game controller): **A, B, X, Y** or **D-pad up** to jump, **START** to start or play again.
  The controller rumbles when Bolt-E blows up, and buzzes a tiny bit for each bolt.
- Press jump again in the air to **double jump** (Bolt-E does a flip!)
- Let go early for a small hop
- Grab golden bolts for +10 points each
- Jump over crates and cones. Ride **under** the grumpy purple drones!
- **Stomp bad robots** by landing on their heads (like Mario!). Bumping into them from the side is a crash.
  - **Pup-Bot** (from `enemy-1.png`) zooms toward you on its wheels.
  - **Egg-Bot** (from `enemy-2.png`) hops up and down, so time your stomp!
  - Stomps in a row without landing score 25, 50, 100, then 200. Hold jump while landing to bounce higher.
- **Boss: BIG RUSTY** (from `enemy-boss-who-can-shoot.png`) shows up at 400 m, then every 700 m after you beat him.
  The world stops and it's an **arena fight**: ride left and right with the **arrow keys**, **A / D**,
  or the controller's **D-pad / left stick**. He has 6 health and gets angrier as he gets hurt:
  - **Phase 1:** shoots bouncing fireballs (jump over them), then does a **GROUND POUND**. His shadow follows you,
    turns red, and he slams down. Ride out of the way and jump the shockwaves!
  - **Phase 2, ANGRY** (steaming ears): more fireballs, and he drops **Pup-Bot helpers** that chase you.
  - **Phase 3, FURIOUS** (red eyes, sparks): lots of fireballs, a **giant laser** at head height
    (stay on the ground!), then **two** ground pounds in a row.
  - After a ground pound he's **dizzy** (spinning eyes, stars). That's when you **jump on his head**!
  - Six stomps and he shakes, pops, and blows apart (+250 points and a rainbow of bolts).
- **Enter** also starts or restarts the game
- **M** (or **BACK** on a controller): music on/off (the game remembers your choice)

## Running it
Open `project.godot` in the Godot 4.7.2 **.NET** editor
(`C:\my-coding-projects\godotPalaceRoomViewer\.tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`)
and press **F5** (Run). Or from a terminal: `dotnet build`, then run Godot with `--path .`.

## Making one exe to share
```powershell
.\build.ps1
```
This makes `dist\RobotDash.exe`, one file you can copy anywhere and double-click.
Inside is the exported Godot game. The first time it runs, it unpacks to `%LOCALAPPDATA%\RobotDash`.
Godot's logs are in `build\export.log` if something goes wrong.

## Where the code lives
| File | What it does |
|---|---|
| `Scripts/Main.cs` | The game boss: draws the world, speeds up, spawns obstacles and bolts, checks for bumps, saves scores |
| `Scripts/Robot.cs` | Bolt-E: jumping, gravity, blinking, music notes, and drawing the robot |
| `Scripts/RobotExplosion.cs` | The crash: Bolt-E bursts into bouncing pieces, sparks, smoke, and dizzy stars |
| `Scripts/Enemy.cs` | Pup-Bot and Egg-Bot: how they move, look, and get squashed |
| `Scripts/Boss.cs` | Big Rusty: his size, his three phases, every attack, getting dizzy, blowing up |
| `Scripts/Blast.cs` | Big Rusty's bouncing fireballs |
| `Scripts/Shockwave.cs` | The waves that roll out from his ground pound |
| `Scripts/Laser.cs` | His giant laser (warning line, then the beam) |
| `Scripts/ScorePopups.cs` | The floating "+25" numbers |
| `Scripts/Obstacle.cs` | Crates, cones, crate stacks, and drones |
| `Scripts/BoltPickup.cs` | The spinning golden bolts |
| `Scripts/Hud.cs` | Score text, title screen, game-over screen |
| `Scripts/ScoreDatabase.cs` | All the SQLite code |
| `Scripts/Audio/Song.cs` | The theme song, written as notes you can change |
| `Scripts/Audio/SoundEffects.cs` | Every sound effect (jump, bolt, explosion, jingles...) |
| `Scripts/Audio/Synth.cs` | The tiny synthesizer that turns notes into sound with math |
| `Scripts/Audio/SoundBoard.cs` | Plays the music and sound effects |
| `Scripts/Shapes.cs` | Drawing helpers (rounded rectangles, happy eyes, X eyes) |

## The database
Saved at `%APPDATA%\Godot\app_userdata\Robot Dash\robotdash.db`. Open it with [DB Browser for SQLite](https://sqlitebrowser.org/) to look inside.
- `runs`: every game played (name, score, meters, bolts, enemies stomped, bosses beaten, seconds, date and time)
- `settings`: remembers things like the last player name and whether the music is on

To test without touching your real scores, start the game with a different database:
`Godot_v4.7.2-stable_mono_win64.exe --path . -- --db=C:\temp\test.db`

## Sounds
There are no sound files. Every sound is made with math by `Scripts/Audio/Synth.cs` when the game starts, like old game consoles did.
To write your own song, change the notes in `Scripts/Audio/Song.cs`: `"C5"` is a note, `"-"` holds it longer, `"."` is silence.

## Fun things to try changing
- Robot colors: the `Color` lines at the top of `Robot.cs`
- Jump height: `JumpSpeed` in `Robot.cs`
- Triple jump: set `MaxJumps = 3`
- How fast the game gets: `StartSpeed`, `MaxSpeed`, `SpeedUpPerSecond` in `Main.cs`
- Bolt points: `PointsPerBolt` in `Main.cs`
- When the boss comes: `FirstBossAt` and `MetersBetweenBosses` in `Main.cs`
- How big the boss is: `Size` in `Boss.cs` (1.5 now; try 2!)
- How tough the boss is: `MaxHealth` in `Boss.cs`, and which attacks he uses in `AttackPlan`
- How long he stays dizzy: `DizzyTime` in `Boss.cs`
- How high Egg-Bot hops: the `80f` in `HopHeight` in `Enemy.cs`
