# Robot Dash

An endless runner starring **Bolt-E**, a tiny cute robot with headphones riding a hoverboard (designed from `hero-robot.png`). Made with Godot 4.7.2 (.NET / C#) and SQLite.

## How to play
- **Space**, **Up arrow**, **W**, or **click**: jump
- **Xbox controller** (or any game controller): **A, B, X, Y** or **D-pad up** to jump, **START** to start or play again.
  The controller rumbles when Bolt-E blows up, and buzzes a tiny bit for each bolt.
  (On the menus, D-pad up never starts a game, so pressing the D-pad a bit crooked while picking a look is fine.)
- Press jump again in the air to **double jump** (Bolt-E does a flip!)
- Let go early for a small hop
- Grab golden bolts for +10 points each
- **Spare batteries** (the little green batteries under "Bolts"): Bolt-E starts with 1. Crash with a spare and he
  puts himself back together instead of game over! Every 100 bolts charges a new one (**1-UP!**), up to 3.
  See [Spare batteries](#spare-batteries) below.
- **Rainbow ? boxes**: sometimes a rainbow **?** box floats over the road. **Jump into it!** A slot machine over
  Bolt-E's head spins and lands on a power: **MEGA BOLT-E**, the **BOLT MAGNET** or the **ROCKET BOARD**.
  See [Mystery ? boxes](#mystery--boxes) below.
- **World Tour**: every time you beat Big Rusty, Bolt-E rides into a new world: **Candy Land**, **Night City**,
  **Snowy Peaks**, then **THE MOON** (with floaty moon jumps!), and all the way around again.
  See [World Tour](#world-tour) below.
- **Bolt Bank & looks**: every bolt you ever grab goes into Bolt-E's **Bolt Bank** (top-left of the menus). It never
  goes down, and it wins Bolt-E new **looks**: Ninja, Fire Truck, Pirate, Hot Dog, King Bolt-E and more (15 in all).
  On the menus, press **LEFT / RIGHT** (arrow keys, the D-pad or the stick), or click the arrows under him,
  to change his look. See [Bolt Bank & looks](#bolt-bank--looks) below.
- **Family race**: on the menus, press **LB / RB** on the controller (or **TAB**, or click the LB / RB buttons) to pick
  **who's playing**. Press **TAB** past the last name (or click the name) to type a **new player**.
  **Flags on the road** show where everyone's best ride ended: zoom past DAD's flag for **YOU PASSED DAD!**, and beat
  the Best score for **NEW HIGH SCORE!** right away. See [Family race](#family-race) below.
- **Dad drives Big Rusty**: in a Big Rusty fight, Dad (or Mom) can press **1 to 4** or **H** on the keyboard to pick
  Big Rusty's attacks, while the kid keeps playing on the controller. See [Dad drives Big Rusty](#dad-drives-big-rusty) below.
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

## Spare batteries
- Bolt-E starts every game with **1 spare battery** and can carry up to **3**. They're the green batteries under "Bolts".
- The dark battery next to them is **charging**: it fills up with yellow as you grab bolts. Every **100 bolts** it pops
  into a new green spare with a **1-UP!** jingle (and a buzz on the controller).
  With 3 spares already, every 100 bolts gives **FULL! +100** points instead.
- **Crash with a spare** and there's still a big KABOOM, slow motion and rumble, but the music only wobbles down. Then:
  a battery zooms over from the corner, every piece flies back into place (the head goes last), the gears get sucked in,
  there's a bright flash, and the face screen boots up with three loading dots before the eyes open.
  **BOLT-E IS BACK!** He flips and keeps riding.
- For 2.5 seconds he **blinks**: he can't crash, but he can still stomp bad robots and grab bolts.
  Dangerous things close by puff away, and the world speeds back up to 85% of the speed it had.
- **In a Big Rusty fight**, Big Rusty laughs "HA HA HA!" and floats back to his spot. His fireballs, shockwaves and
  lasers vanish, and his health stays where it was. He waits until Bolt-E is back before attacking again.
  (If he was dizzy, he stays dizzy, so you might still get to stomp him!)
- While Bolt-E is in pieces, jump and START do nothing (so mashing buttons can't skip the rebuild). M / BACK still work.
- **No spare left?** Then a crash is game over, like always. If you used batteries, the results card says
  **Out of batteries!** (unless it's a **NEW HIGH SCORE!**)
- Scores now run **higher** than the old Top Scores (spare batteries make games longer).
  Want the classic game? Set `StartingBatteries = 0` in `Scripts/Main.Batteries.cs`.

## Mystery ? boxes
- Every 300 to 450 meters, a rainbow **?** box floats over the road, in a big gap between obstacles. The first one
  comes after 100 m, almost always before the first Big Rusty fight. It floats high, so riding along never opens it:
  **jump into it** (from any side).
- A slot machine pops up over Bolt-E's head and goes **tick-tick-tick**, slowing down, until it lands on a power.
  The power's name shows big, and a bar at the top of the screen shows how long it lasts.
- **MEGA BOLT-E** (6 seconds): he grows giant in steps, like Mario (**bwip-bwip-BWIIIP!**), and the music speeds up.
  Every landing shakes the screen. Crates, cones and drones burst into bouncing pieces (**SMASH! +25**, and 2 bolts pop out),
  and bad robots get flattened from any side (**SPLAT! +25**). At the end he shrinks back with a "pfffft".
- **BOLT MAGNET** (8 seconds): a red magnet floats over his head and his eyes turn into pink hearts. Every bolt nearby
  zooms over to him, and extra bolts sprinkle in. Grab bolts quickly and the notes climb **two whole octaves**!
- **ROCKET BOARD** (5 seconds): flames burst out of the board, he zooms up over everything, the world rushes by faster,
  and a line of bolts waits in the sky. Nothing can reach him up there (and he can't jump). Then a tiny red-and-white
  **parachute** pops open and he floats down safely. Anything dangerous close to where he lands puffs away.
- When a power is almost over (the last 1.5 seconds), Bolt-E **blinks** and the clock goes **tick... tick... tick**.
  After a power ends he blinks for 1 more second, and he's safe while he blinks.
- A box **never gives the same power twice in a row**.
- There are **no boxes near or during a Big Rusty fight** (none in the last 150 m before he comes), and a power ends
  as soon as the boss warning sounds. A crash also ends a power right away.
- Scores run higher with powers (more bolts, SMASH! and SPLAT! points, and more meters on the rocket board).

## World Tour
- Every game starts in **Sunny Hills**. Beat Big Rusty and **YOU BEAT BIG RUSTY!** shows first. About 2 seconds later
  **WELCOME TO CANDY LAND!** pops up with a sparkly jingle, and the sky, sun, clouds, hills and ground smoothly change
  color over 3 seconds. Every Big Rusty win takes you one world further:
  1. **Sunny Hills**: the green hills Robot Dash always had.
  2. **Candy Land**: a pink sky, giant lollipops, and chocolate ground with pink frosting and rainbow sprinkles.
  3. **Night City**: twinkling stars, a moon, skyscrapers with glowing windows, and a road with yellow dashes.
  4. **Snowy Peaks**: snowy mountains, pine trees and falling snow.
  5. **The Moon**: the Earth in a black starry sky, grey ground full of craters, and **FLOATY moon jumps**:
     Bolt-E jumps just as high, but hangs in the air longer (obstacles come a bit farther apart to match).
- After the Moon you're back in Sunny Hills: **YOU WENT ALL THE WAY AROUND!**
- If a new world comes while Bolt-E is in the middle of a jump (kids love jumping for the prize bolts!), the jump
  still goes just as high, on the Moon and back in Sunny Hills.
- **Big Rusty dresses up** for every world: a party hat in Candy Land, cool sunglasses in Night City (he pushes them
  up when he winds up a ground pound, gets hurt or is dizzy, so you can still see his eyes), a bobble hat in
  Snowy Peaks and a fishbowl space helmet on the Moon. When he blows up, his hat flies off too!
- In the dark worlds (Night City and the Moon) a soft glow around Bolt-E keeps him easy to see.
- Each world has its own **music speed** (a bit faster in Candy Land and Snowy Peaks, slower at night and on the Moon).
  Sunny Hills' speed plays on the title screen and from the very start of every game.
- The results card says how far you got, like **You rode all the way to NIGHT CITY!**
- **Postcards** on the menus (on the left, next to the cards) show every world you have reached. Worlds you reached
  for the very first time get a gold **NEW!** ribbon, and worlds you haven't reached yet are grey with a **?**.
- A new game always starts in Sunny Hills again. Reaching the Moon takes 4 Big Rusty wins in one game:
  spare batteries help!

## Bolt Bank & looks
- **The Bolt Bank** (top-left of the title screen and the results screen) holds every bolt this player has EVER grabbed,
  in every game. It never goes down, so no bolt is wasted! The bar under it fills up on the way to the next prize,
  like **Next: COWBOY in 57**.
- **Picking a look:** on the menus Bolt-E stands in the corner on the left. Press **LEFT / RIGHT** (arrow keys,
  the D-pad or the stick: one push is one step) or click the white arrows under him, and he changes looks right away.
  If he's still in pieces after a crash, he zips back together wearing the new look. The game remembers each
  player's look. (The A and D keys don't change looks on the menus: there, "(A)" means the controller's A button.
  In a Big Rusty fight A / D still ride left and right.)
- A look you haven't won yet shows as a **dark shadow** of Bolt-E, with a padlock and what it takes:
  **Need 120 more bolts!**, **Beat BIG RUSTY 3 times (1/3)** or **Ride to THE MOON!**
- **After a game** the Bolt Bank counts up with ticking sounds (higher and higher). Passing a prize means a party:
  **NEW LOOK!**, confetti, a jingle, and Bolt-E puts himself back together wearing it.
  If you start the next game too fast, the party simply happens on the next menu instead: **it's never lost**.
- **Old games count!** The first time the game opens for a player who already has games, the looks they already won
  are all announced at once, like **5 NEW LOOKS! Your old games count!** This note is never lost either: if a game
  starts (or LB / RB picks somebody else) before it has been showing for 2.5 seconds, it comes back, on the results
  card after that game (with that game's new looks in it too) or the next time that player is picked.
- Each player (each name) has their own Bolt Bank and their own look. "Sam" and "sam" are the same player.
- The 15 looks (bolts = bolts in the Bolt Bank):

  | Look | How to win it | Special |
  |---|---|---|
  | CLASSIC | everyone has it | the Bolt-E you know |
  | PARTY BOLT-E | 100 bolts | a party hat |
  | BUBBLEGUM | 250 bolts | pink, with a trail of hearts |
  | FIRE TRUCK | 450 bolts | red and silver |
  | COWBOY | 700 bolts | a cowboy hat, and he rides a skateboard |
  | NINJA | 1,000 bolts | black with glowing red eyes |
  | OCEAN | 1,400 bolts | deep blue, with a trail of sparkles |
  | HOT DOG | 1,900 bolts | a propeller cap, and he rides a HOT DOG |
  | PIRATE | 2,500 bolts | a pirate hat |
  | GALAXY | 3,200 bolts | space purple, with a trail of sparkles |
  | GOLD BOLT-E | 4,000 bolts | all gold |
  | KING BOLT-E | 5,500 bolts | gold, a crown, a rainbow board and a rainbow trail |
  | RAINBOW | 7,500 bolts | rainbow eyes, a rainbow board and a rainbow trail |
  | RUSTY JR. | beat Big Rusty 3 times (all games together) | dressed like Big Rusty! |
  | ASTRONAUT | ride all the way to THE MOON | a space helmet, and he rides a cloud |
- When Bolt-E blows up, his hat flies off as its own bouncing piece (and flies back on last, with his head).
- Light-colored looks (Bubblegum, Gold, King, Rusty Jr., Astronaut) get a thin dark outline, so Bolt-E is still easy
  to see on snow and candy.
- If the score database can't open, Bolt-E wears the classic look and nothing is saved (like the scores).

## Family race
- **Who's playing?** The title card says `Who's playing?  [LB]  MAX  [RB]`. Press **LB / RB** on the controller (or
  click the LB / RB buttons) to switch between everyone who has played. Bolt-E's look, the Bolt Bank and the World Tour
  postcards switch too. The results card has the same buttons around the play button: `[LB]  DAD's turn!  (A)  [RB]`.
- **A new player:** press **TAB** past the last name (or click the name) and the name box opens: type the new name and
  press **Enter** to play (or **Escape** to stop typing). On the results card, TAB past the last name makes the button
  say **New player?  (Enter)**, and it takes you to the name box. The controller's LB / RB only switch between real
  names, so a controller can never get stuck in the name box (LB / RB, the D-pad and A still work while Dad types).
  If the kid presses **A** (or START) when Dad has typed only 1 letter, the game starts as the last player picked, and
  no 1-letter person is saved. (Enter always keeps what Dad typed.)
- Every game starts with **GO, MAX!**. When it's somebody else's turn it says **DAD'S TURN!**, and if somebody else has a
  better score today, a tip says who to beat: **Beat MAX's 1520 from today!**
- **Flags on the road** stand where the best rides ended (rides under 50 m don't get one):
  - a gold **YOUR RECORD 640 m** flag with a white star (the farthest this player ever rode),
  - a small silver **LAST TIME** flag (where the last ride ended, when it's not right next to the record),
  - and a colored flag for up to 4 other people, like **DAD 812 m** (everyone always gets the same color).
- Zoom past **DAD's flag** and it tips over with X eyes, rainbow confetti flies, the controller buzzes, Bolt-E gets star
  eyes and the screen cheers **YOU PASSED DAD!** Passing **LAST TIME** gives a little **BEAT LAST TIME!**. Passing
  **YOUR RECORD** gives **NEW RECORD!**, fireworks, star eyes and a flip.
- When the score beats the **Best** number in the top-right corner, **NEW HIGH SCORE!** plays right then, in the middle
  of the ride: the jingle, fireworks, and the Best number flashes gold. (Two different records: RECORD is your flag,
  which is about meters; HIGH SCORE is the Best number, which is about the score.)
- Crash within 150 m of a flag and the results card says **So close! Only 42 m to DAD's flag!**
- The results card also says **who's champ today**: `Today: MAX 1520 (champ!)  -  DAD 980`. Taking turns: when
  somebody else played the game before, the higher score wins the round: `Round to MAX!`
- The title card's Top Scores is now **Family Champions**: one row per person, with their best score ever.
- Games played without a name count as a person called **Player**. Was that really MAX? Fix it in DB Browser (with the
  game closed) with the SQL in [The database](#the-database) below, then "Write Changes".
- "Sam" and "sam" are the same person, but a typo makes a new person ("Sma"). The same SQL fixes that too.

## Dad drives Big Rusty
- When **BIG RUSTY** shows up, Dad (or Mom) can grab the **keyboard** and drive him, while the kid keeps playing on the
  controller exactly like always. Press one of Dad's keys to take over: **DAD IS DRIVING BIG RUSTY!** shows, Rusty laughs
  and puts on a little **blue cap with a star**, his bar says **BIG RUSTY (DAD)**, and a tip shows Dad's keys.
- Dad's keys (the number keys on top of the keyboard, or on the number pad):

  | Key | Big Rusty does |
  |---|---|
  | **1** | bouncing fireballs |
  | **2** | drops Pup-Bot helpers (once he's ANGRY) |
  | **3** | a GROUND POUND (two in a row when he's FURIOUS) |
  | **4** | the giant laser (only when he's FURIOUS) |
  | **H** | "HA HA HA!" |
- **Thinking dots:** when Rusty is ready for an order, three white dots (with a dark ring, so you can see them in front
  of the sun and in the snow) bob over his head. That's the moment to press a key!
  While he's busy (shooting, pounding, dizzy...) a key just says **Wait...**, and a move he isn't angry enough for yet
  says **Not yet!** (Pressing again and again doesn't stack up the words, and they never float up over his health bar.)
- **Fair-play rules**, so a 6-year-old can win:
  - After **2 attacks** Rusty is **TIRED**: only the ground pound works (**TIRED! Press 3!**). Laughing is always
    allowed, but only once every 2 seconds, and **only 2 laughs in a row**: then H says **Wait...** until Rusty has done
    a real move (Dad's pick, or his own brain's after 3 seconds). So pressing H over and over can never stop the fight.
  - After **every ground pound** he's dizzy, like always, so Bolt-E gets his chance to stomp him. His shadow still
    follows Bolt-E and turns red before he slams down.
  - If Dad presses nothing for **3 seconds**, Rusty's own brain picks his next move, so the fight never stops.
  - When Bolt-E crashes, Rusty still laughs and waits until Bolt-E is back together.
- After the giant laser Rusty is down low: he floats back up to his spot smoothly, even if Dad picks his next move
  right away.
- Beat him and it's **YOU BEAT DAD'S BIG RUSTY!** with the usual +250 and the rainbow of bolts (and Dad's cap flies off
  when he blows up!). Lose the last battery while Dad drives him, and the results card says **DAD GOT YOU!**
- Every new Big Rusty starts out driving himself: press a key again to take over.
- Only these keys drive him. A controller can never drive Big Rusty, and the kid's buttons and keys work just like before.
- Want MOM or GRANDMA to drive? Change `DriverName` in `Scripts/Main.Driver.cs`.

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

## Self-test (grown-ups)
The game can test itself. It plays by itself, crashing Bolt-E on purpose, grabbing hundreds of bolts, fighting
Big Rusty and checking that the cards fit on the screen. Every check prints `PASS` or `FAIL`, and at the end it prints
`SELFTEST DONE: N passed, M failed`. It never opens a window. It never touches the real scores: it refuses to start
(exit code 2) unless `--db` is a full path to a scratch file outside the save folder and the project folder.
It ignores real controllers and never makes them rumble, so it's safe to run while someone is playing.

Run it from the project folder in PowerShell. It builds the game first (running Godot from a terminal doesn't build
the C# code, so without `dotnet build` it would test the last build, not the numbers you just changed). Then it points
APPDATA and LOCALAPPDATA at a scratch folder so even Godot's own files land there:
```powershell
dotnet build
$scratch = 'C:\scratch\rd'
New-Item -ItemType Directory -Force $scratch | Out-Null
Remove-Item "$scratch\selftest.db*" -ErrorAction SilentlyContinue
$oldAppData = $env:APPDATA; $oldLocalAppData = $env:LOCALAPPDATA
try {
    $env:APPDATA = "$scratch\appdata"; $env:LOCALAPPDATA = "$scratch\localappdata"
    & 'C:\my-coding-projects\godotPalaceRoomViewer\.tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' `
        --headless --path . --fixed-fps 60 --quit-after 72000 --log-file "$scratch\godot.log" `
        -- "--db=$scratch\selftest.db" --selftest
    "Exit code: $LASTEXITCODE"
} finally {
    $env:APPDATA = $oldAppData; $env:LOCALAPPDATA = $oldLocalAppData
}
```
Or in Git Bash:
```bash
dotnet build
S='C:\scratch\rd'; mkdir -p "$S"; rm -f "$S"/selftest.db*
APPDATA="$S\appdata" LOCALAPPDATA="$S\localappdata" \
  'C:\my-coding-projects\godotPalaceRoomViewer\.tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' \
  --headless --path . --fixed-fps 60 --quit-after 72000 --log-file "$S\godot.log" -- "--db=$S\selftest.db" --selftest
```
Exit codes: **0** = everything passed, **1** = something failed, **2** = refused to start (bad `--db`),
**3** = it took too long. A good run has no lines with `FAIL:`, `ERROR:`, `SCRIPT ERROR` or `Unhandled exception`
in the output or in `godot.log`. The tests are in `Scripts/Main.SelfTest.cs`, and new features add theirs there.
Just before the DONE line it also prints how many seconds of game time the tests took (the watchdog stops them at 900).
(They expect the normal tuning numbers, so if you change a number like `StartingBatteries` or `MegaSeconds`,
a world in `Worlds.cs`, a look in `Looks.cs`, a family race number in `Main.Family.cs`, or Big Rusty's
`AttacksBeforeHeMustPound`, `DriverWaitBeforeAI` or `LaughsInARow`, some tests will say FAIL. Changing `DriverName`
to MOM, or a flag's `PoleHeight`, is fine: the tests use whatever is there.)
The self-test only pretends to press Dad's keys: it never touches a real keyboard or controller.
Start each run with a fresh `selftest.db` (the commands above delete the old one). Test players' names start with T,
like `TCOW`; if a test name was already used in that database, the test adds a number (`TCOW2`). (One test also saves a
game called `Player` in the scratch database, to check that games without a name count as a person.)

## Where the code lives
| File | What it does |
|---|---|
| `Scripts/Main.cs` | The game boss: draws the world (in the colors of the world you're in), speeds up, spawns obstacles and bolts, checks for bumps (every crash goes through `Bonk`), saves scores |
| `Scripts/Main.Batteries.cs` | Spare batteries: charging with bolts (1-UP!), the magic rebuild after a crash, blinking, puffing away dangers |
| `Scripts/Main.PowerUps.cs` | Rainbow ? boxes: when a box comes, opening it, starting and ending powers, SMASH! and SPLAT!, the magnet pull, the rocket landing |
| `Scripts/Main.Worlds.cs` | The World Tour: changing worlds after a Big Rusty win, the smooth color change, and drawing each world's special things (stars, moon, Earth, skyscrapers, mountains, lollipops, pine trees, sprinkles, road dashes, craters, snow, the night glow) |
| `Scripts/Worlds.cs` | Every world on the World Tour: its colors, sky thing, scenery, weather, gravity, music speed and Big Rusty's outfit. **Make your own world here!** |
| `Scripts/WorldPostcards.cs` | The World Tour postcards on the menus (pictures, grey "?" cards and gold NEW! ribbons) |
| `Scripts/Looks.cs` | All 15 of Bolt-E's looks: their colors, hats, boards, trails and what it takes to win them. **Invent your own look here!** |
| `Scripts/Main.Looks.cs` | The Bolt Bank and the looks: loading a player's bank and look, changing looks on the menus, the count-up after a game, and the unlock parties and the first-launch "N NEW LOOKS!" note (which are never lost) |
| `Scripts/Main.Family.cs` | The family race: who's playing (LB / RB, TAB), "GO, MAX!" and "DAD'S TURN!", the flags on the road and passing them, NEW HIGH SCORE! in the middle of a ride, fireworks, who's champ today, rounds, and "So close!" |
| `Scripts/RecordFlag.cs` | A flag on the road: the pole, the waving cloth, the star or X eyes, the words, and tipping over |
| `Scripts/Main.Driver.cs` | Dad drives Big Rusty: the keys 1 to 4 and H, taking over ("DAD IS DRIVING BIG RUSTY!"), giving orders ("Wait...", "TIRED! Press 3!", "Not yet!", which never pile up or float over his health bar), and who's driving (`DriverName`) |
| `Scripts/BankBox.cs` | The Bolt Bank box in the top-left corner of the menus (the number, the gold hexagon and the bar to the next prize) |
| `Scripts/LookPicker.cs` | The look picker under Bolt-E on the menus (the arrows, the look's name, the padlock and what it takes to win it) |
| `Scripts/Main.SelfTest.cs` | Grown-up testing code: the headless self-test and all its checks |
| `Scripts/PowerUps.cs` | The three powers: how long they last, how big, how high (the "try changing these!" numbers), their names and little icons |
| `Scripts/PowerBox.cs` | The rainbow ? box with its two orbiting stars |
| `Scripts/PowerRoulette.cs` | The slot machine over Bolt-E's head (tick-tick-tick!) |
| `Scripts/Robot.cs` | Bolt-E: jumping, gravity (and floaty Moon gravity, even when a new world comes in the middle of a jump), blinking, music notes, the booting face, growing giant (MEGA), the rocket board and parachute, the magnet, heart eyes, star eyes, drawing the robot, and his looks (hats, boards, trails, rainbow eyes and the dark outline) |
| `Scripts/RobotExplosion.cs` | The crash: Bolt-E bursts into bouncing pieces, sparks, smoke, and dizzy stars (and flies back together in the magic rebuild). Smashed crates, cones and drones use it too |
| `Scripts/Enemy.cs` | Pup-Bot and Egg-Bot: how they move, look, and get squashed |
| `Scripts/Boss.cs` | Big Rusty: his size, his three phases, every attack, getting dizzy, blowing up, his World Tour outfits, and being driven by Dad (waiting for orders with thinking dots, the fair-play rules like TIRED and only 2 laughs in a row, and Dad's blue cap) |
| `Scripts/Blast.cs` | Big Rusty's bouncing fireballs |
| `Scripts/Shockwave.cs` | The waves that roll out from his ground pound |
| `Scripts/Laser.cs` | His giant laser (warning line, then the beam) |
| `Scripts/ScorePopups.cs` | The floating "+25", "1-UP!", "SMASH! +25", "HA HA HA!" and "Wait..." words (how long they stay, how fast they float up) |
| `Scripts/Obstacle.cs` | Crates, cones, crate stacks, and drones (and smashing them into pieces) |
| `Scripts/BoltPickup.cs` | The spinning golden bolts |
| `Scripts/Hud.cs` | Score text, title screen (with "Who's playing?" and Family Champions), game-over screen (results card, with the Today and So close lines and LB / RB around the play button), the flying spare battery, the power bar, Big Rusty's health bar (it says "BIG RUSTY (DAD)" while Dad drives him), the Best number flashing gold, the menu strip on the left (the Bolt Bank, the postcards, the NEW LOOK! words and the look picker) |
| `Scripts/BatteryRow.cs` | The spare batteries under "Bolts" (green spares and the yellow charging one) |
| `Scripts/ScoreDatabase.cs` | All the SQLite code (including everyone who plays, their bests, and today's scores) |
| `Scripts/Audio/Song.cs` | The theme song, written as notes you can change |
| `Scripts/Audio/SoundEffects.cs` | Every sound effect (jump, bolt, explosion, jingles, power-ups, SMASH, the rocket, the new-world jingle, the NEW LOOK! jingle, the "ta-da-DA!" for passing a flag...) |
| `Scripts/Audio/Synth.cs` | The tiny synthesizer that turns notes into sound with math |
| `Scripts/Audio/SoundBoard.cs` | Plays the music and sound effects (and remembers how fast the music should play, so it wobbles down while Bolt-E is in pieces, even if you switch it off and on) |
| `Scripts/Shapes.cs` | Drawing helpers (rounded rectangles, ovals, happy eyes, X eyes, heart eyes) |

## The database
Saved at `%APPDATA%\Godot\app_userdata\Robot Dash\robotdash.db`. Open it with [DB Browser for SQLite](https://sqlitebrowser.org/) to look inside.
- `runs`: every game played (name, score, meters, bolts, enemies stomped, bosses beaten, spare batteries used,
  `world_reached`, seconds, date and time). Older databases get the new columns added by themselves (old games count as 0).
  `world_reached` is how many worlds of the World Tour that game rode through: 1 = only Sunny Hills, 3 = all the way to
  Night City, 6 or more = all the way around. Old games count as 1 (Sunny Hills), so postcards are earned by really going there.
- `settings`: remembers things like the last player picked (`player_name`) and whether the music is on, and for each player
  (the name in small letters): `look:sam` = the look Sam wears, and `looks_seen:sam` = the looks Sam already had an
  unlock party for (like `classic,party,cowboy`). A look only joins `looks_seen` once its party has really happened,
  which is why a party is never lost.
- **The Bolt Bank** isn't stored anywhere by itself: it's all the bolts in the `runs` table for that name added up
  (old games count too!). To see everyone's Bolt Bank in DB Browser, run this in "Execute SQL":
  `SELECT player_name, SUM(bolts) FROM runs GROUP BY player_name COLLATE NOCASE;`
  Then you can pick good `Need` numbers in `Scripts/Looks.cs` (for example so the first new look comes soon).
- **The family race** only reads the `runs` table (nothing new is stored): everyone who plays, their best score, their
  farthest ride (for the flags), their last ride, and today's scores. "Sam" and "sam" are one person, and the name is
  written the way it was typed last. Rounds won are only remembered while the game is open.
- **Games played without a name** are saved as `Player`. To give them to the right person (here MAX), close the game,
  then run this in DB Browser's "Execute SQL" and press "Write Changes". The first line moves the games, the second
  makes MAX the player the game opens with, and the last three move Player's look and seen looks to MAX (the keys
  use the name in small letters; if MAX already has his own, his stay and Player's are thrown away):
  ```sql
  UPDATE runs SET player_name='MAX' WHERE player_name='Player';
  UPDATE settings SET value='MAX' WHERE key='player_name' AND value='Player';
  UPDATE OR IGNORE settings SET key='look:max' WHERE key='look:player';
  UPDATE OR IGNORE settings SET key='looks_seen:max' WHERE key='looks_seen:player';
  DELETE FROM settings WHERE key IN ('look:player', 'looks_seen:player');
  ```
  The same SQL fixes a typo too (put the typo instead of `Player`, and its small-letter key instead of `player`).

To test without touching your real scores, start the game with a different database:
`Godot_v4.7.2-stable_mono_win64.exe --path . -- --db=C:\temp\test.db`

## Sounds
There are no sound files. Every sound is made with math by `Scripts/Audio/Synth.cs` when the game starts, like old game consoles did.
To write your own song, change the notes in `Scripts/Audio/Song.cs`: `"C5"` is a note, `"-"` holds it longer, `"."` is silence.

## Fun things to try changing
- **Invent your own look!** Every look is a few lines in `Looks.cs`: 7 colors (body, the ring around the screen,
  screen, eyes, headphones, stripes and lights, board), a hat (party, cowboy, pirate, propeller, crown or space helmet),
  a board (classic, skateboard, hot dog, rainbow or cloud), a trail (sparkles, hearts or rainbow), rainbow eyes, and
  what it takes to win it (`Need`). Keep the eyes bright and the screen dark (the self-test checks it).
- How many bolts each look costs: the `Need` numbers in `Looks.cs` (try 10 for PARTY BOLT-E!)
- The unlock parties and the count-up: `PartyEvery`, `CountUpSeconds`, `CountUpSlowest`, `FirstNoteReadSeconds`
  (how long "5 NEW LOOKS!" has to show before it counts as seen) and `MenuHomeX` (where Bolt-E stands on the menus)
  in `Main.Looks.cs`
- The trails: `TrailEvery` (how often a sparkle, heart or rainbow bubble comes out) and `TrailLife` (how long they last)
  in `Robot.cs`
- Jump height: `JumpSpeed` in `Robot.cs`
- Triple jump: set `MaxJumps = 3`
- How fast the game gets: `StartSpeed`, `MaxSpeed`, `SpeedUpPerSecond` in `Main.cs`
- Bolt points: `PointsPerBolt` in `Main.cs`
- When the boss comes: `FirstBossAt` and `MetersBetweenBosses` in `Main.cs`
- How big the boss is: `Size` in `Boss.cs` (1.5 now; try 2!)
- How tough the boss is: `MaxHealth` in `Boss.cs`, and which attacks he uses in `AttackPlan`
- How long he stays dizzy: `DizzyTime` in `Boss.cs`
- How high Egg-Bot hops: the `80f` in `HopHeight` in `Enemy.cs`
- Spare batteries, all in `Main.Batteries.cs`: `StartingBatteries` (0 = the classic game, 3 = super easy),
  `MaxBatteries` (how many he can carry), `BoltsPerBattery` (try 50 for lots of 1-UPs!)
- The magic rebuild: `RebuildSeconds` (how fast the pieces fly back), `SafeBlinkSeconds` (how long he blinks),
  `SpeedAfterRebuild` (0.85 = 85% of the old speed) and `ClearAround` (how far away dangers puff away), all in `Main.Batteries.cs`
- Mystery ? boxes, all in `PowerUps.cs`: `MegaSeconds`, `MagnetSeconds` and `RocketSeconds` (how long each power lasts),
  `MegaSize` (how giant MEGA BOLT-E gets: try 2.5!), `MagnetReach` (how far the magnet pulls bolts from),
  `RocketHeight` and `RocketBoost` (how high and how fast the rocket board flies), `ParachuteFallSpeed`,
  `FirstBoxAt`, `BoxEveryMin` and `BoxEveryMax` (when boxes come: try 50 and 100 for LOTS of boxes!),
  and `NoBoxesNearBoss` (how close to Big Rusty boxes stop coming)
- The slot machine: `FlickDelays` in `PowerRoulette.cs` (how fast it flicks and slows down; you can add or take away
  numbers too, and it still lands on the power it gives)
- The floating words like "+25": `LifeSeconds` (how long they stay) and `FloatSpeed` (how fast they float up) in
  `ScorePopups.cs`
- **Make your own world!** Every world is a few lines in `Worlds.cs`: change the sky, hill and ground colors (red, green,
  blue from 0 to 1), the `Scenery` (lollipops, city, mountains or craters), the `Weather` (stars or snow), the `Gravity`
  (try 0.3 for SUPER floaty jumps! Keep it between 0.2 and 2: Bolt-E never feels less than 0.1, so even 0 can't break
  the game, but it would be very slow), the `MusicSpeed` and Big Rusty's outfit. Keep the hills bright enough to see
  Bolt-E (the self-test checks it).
- How long the colors take to change: `WorldChangeSeconds` in `Main.Worlds.cs`, and `NewWorldDelay`
  (how long "YOU BEAT BIG RUSTY!" shows before the new world comes)
- Big Rusty's outfit colors: `PartyPink`, `BobbleRed` and friends in `Boss.cs`
- The bolt song: `ScaleSteps` in `Main.cs` (the notes bolts play when you grab them quickly one after another)
- The family race, all in `Main.Family.cs`: `MinFlagMeters` (rides shorter than this get no flag), `MaxOtherFlags`
  (how many other people get a flag: try 1 for just the best one), `SoCloseMeters` (how close counts as "So close!"),
  `KnownPlayersShown` (how many people LB / RB switch between), `ChampionsShown` (rows in Family Champions),
  `FireworksEvery`, and `FlagColors` (everybody's flag colors)
- The flags themselves, in `RecordFlag.cs`: `PoleHeight` (how tall: the cloth, the star, the words and the confetti all
  go up with it), `LastTimeSize` (how small the LAST TIME flag is),
  `FallenOver` and `FallSeconds` (how far and how fast a passed flag tips over), and `LabelSize` (how big the words are)
- The "ta-da-DA!" cheer for passing a flag: `PassFlag` in `Scripts/Audio/SoundEffects.cs`
- Dad drives Big Rusty: who's driving, `DriverName` in `Main.Driver.cs` (try MOM or GRANDMA!), and in `Boss.cs`
  `AttacksBeforeHeMustPound` (how many attacks before he's TIRED: try 1 for an easier fight), `DriverWaitBeforeAI`
  (how long he waits for Dad's order before his own brain picks), `LaughsInARow` (how many laughs Dad gets before a
  real move has to come) and `CapBlue` (the color of Dad's cap)
