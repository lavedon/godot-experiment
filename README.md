# Robot Dash

An endless runner starring **Bolt-E**, a tiny cute robot with headphones riding a hoverboard (designed from `hero-robot.png`). Made with Godot 4.7.2 (.NET / C#) and SQLite.

## How to play
- **Space**, **Up arrow**, **W**, or **click**: jump
- **Xbox controller** (or any game controller): **A, B, X, Y** or **D-pad up** to jump, **START** to start or play again.
  The controller rumbles when Bolt-E blows up, and buzzes a tiny bit for each bolt.
  (On the menus, D-pad up never starts a game, so pressing the D-pad a bit crooked while picking a look is fine.)
  The **first controller plugged in is Bolt-E's**. A second controller never moves Bolt-E: it can only drive Big Rusty
  (see [Dad drives Big Rusty](#dad-drives-big-rusty) below), and only Bolt-E's controller buzzes for bolts and crashes.
- Press jump again in the air to **double jump** (Bolt-E does a flip!)
- Let go early for a small hop
- Grab golden bolts for +10 points each
- **Spare batteries** (the little green batteries under "Bolts"): Bolt-E starts with 1. Crash with a spare and he
  puts himself back together instead of game over! Every 100 bolts charges a new one (**1-UP!**), up to 3.
  See [Spare batteries](#spare-batteries) below.
- **Rainbow ? boxes**: sometimes a rainbow **?** box floats over the road. **Jump into it!** A slot machine over
  Bolt-E's head spins and lands on a power: **MEGA BOLT-E**, the **BOLT MAGNET** or the **ROCKET BOARD**.
  See [Mystery ? boxes](#mystery--boxes) below.
- **World Tour**: every time you beat a boss, Bolt-E rides into a new world: **Candy Land**, **Night City**,
  **Snowy Peaks**, then **THE MOON** (with floaty moon jumps!), and all the way around again.
  See [World Tour](#world-tour) below.
- **Bolt Bank & looks**: every bolt you ever grab goes into Bolt-E's **Bolt Bank** (top-left of the menus). It never
  goes down, and it wins Bolt-E new **looks**: Ninja, Fire Truck, Pirate, Hot Dog, King Bolt-E and more (17 in all).
  On the menus, press **LEFT / RIGHT** (arrow keys, the D-pad or the stick), or click the arrows under him,
  to change his look. See [Bolt Bank & looks](#bolt-bank--looks) below.
- **Two new robots: COZMOS and VECKTOR!** On the menus, press **LEFT** from CLASSIC: first comes **VECKTOR**, and
  LEFT again gives **COZMOS**. Everyone has them from the start. See [COZMOS and VECKTOR](#cozmos-and-vecktor) below.
- **Family race**: on the menus, press **LB / RB** on the controller (or **TAB**, or click the LB / RB buttons) to pick
  **who's playing**. Press **TAB** past the last name (or click the name) to type a **new player**.
  **Flags on the road** show where everyone's best ride ended: zoom past DAD's flag for **YOU PASSED DAD!**, and beat
  the Best score for **NEW HIGH SCORE!** right away. See [Family race](#family-race) below.
- **Dad drives Big Rusty**: normally the computer drives Big Rusty. Plug in a **second controller**, and in a Big Rusty
  fight Dad (or Mom) can press its buttons to pick Big Rusty's attacks, while the kid keeps playing on the first
  controller. See [Dad drives Big Rusty](#dad-drives-big-rusty) below.
- Jump over crates and cones. Ride **under** the grumpy purple drones!
- **Stomp bad robots** by landing on their heads (like Mario!). Bumping into them from the side is a crash.
  - **Pup-Bot** (from `enemy-1.png`) zooms toward you on its wheels.
  - **Egg-Bot** (from `enemy-2.png`) hops up and down, so time your stomp!
  - Stomps in a row without landing score 25, 50, 100, then 200. Hold jump while landing to bounce higher.
- **Bosses** show up at 400 m, then every 700 m after you beat one (**WARNING!  BOSS AHEAD!**). There are four,
  and they **take turns**: **BIG RUSTY**, **VROOMBA**, the giant robot vacuum (see [VROOMBA](#vroomba-the-second-boss)
  below), **DRONEZILLA**, the giant attack drone (see [DRONEZILLA](#dronezilla-the-third-boss) below), and **OPTI-MAX**,
  the humanoid robot (see [OPTI-MAX](#opti-max-the-fourth-boss) below). The world stops and it's an **arena fight**: ride left and right with the **arrow keys**, **A / D**,
  or the controller's **D-pad / left stick**. Every boss has 6 health and gets angrier as he gets hurt.
- **Boss: BIG RUSTY** (from `enemy-boss-who-can-shoot.png`):
  - **Phase 1:** shoots bouncing fireballs (jump over them), then does a **GROUND POUND**. His shadow follows you,
    turns red, and he slams down. Ride out of the way and jump the shockwaves!
  - **Phase 2, ANGRY** (steaming ears): more fireballs, and he drops **Pup-Bot helpers** that chase you.
  - **Phase 3, FURIOUS** (red eyes, sparks): lots of fireballs, a **giant laser** at head height
    (stay on the ground!), then **two** ground pounds in a row.
  - After a ground pound he's **dizzy** (spinning eyes, stars). That's when you **jump on his head**!
  - Six stomps and he shakes, pops, and blows apart (+250 points and a rainbow of bolts).
- **Boss: VROOMBA**, a giant robot vacuum (drawn in code): he **ZOOMS** across the ground (jump over him!), **BONKS**
  into the wall and lands upside down, dizzy: that's when you **jump on him**! His vacuum **SUCKS** (ride away!) and
  gobbles up the bolts, and when he's ANGRY he spits out **dust bunnies**. Stomp him and the bolts fly back out!
- **Boss: DRONEZILLA**, a giant attack drone (drawn in code): he flies high and attacks from the air: **BOMBS**
  (ride out of the red rings!), a low **SWOOP** (jump over him!), and when he's ANGRY a **ZAP BEAM** and **mini-drones**.
  After every attack his battery runs low and he **LANDS**: that's when you **jump on him**!
- **Boss: OPTI-MAX**, a tall white-and-black humanoid robot (drawn in code): he **throws boxes**, does a **SUPER STOMP**
  (jump the teal shockwaves!), and when he's ANGRY a **SOCCER KICK** and a **ROBOT RUN**. After every attack he stops
  for a **SOFTWARE UPDATE** and kneels down: that's when you **jump on him**!
- **Enter** also starts or restarts the game
- **M** (or **BACK** on a controller): music on/off (the game remembers your choice)
- BIG RUSTY and VROOMBA battles play **Titan Circuit**, an original 32-bar chiptune with a charging minor-key riff, galloping bass,
  heavy drums, a half-time bridge and a soaring final section. It loops smoothly, speeds up as the boss gets angry,
  and crossfades back to the running theme after victory. M / BACK and battery rebuilds work with both themes.
- **F11**: switch between full screen (the game always starts full screen) and a window
- **F9**: move the game to the other screen. With a TV or second monitor plugged in, the game starts on the
  TV by itself (so the laptop screen stays free). F9 swaps, and the game remembers your choice for next time.

## Spare batteries
- Bolt-E starts every game with **1 spare battery** and can carry up to **3**. They're the green batteries under "Bolts".
- The dark battery next to them is **charging**: it fills up with yellow as you grab bolts. Every **100 bolts** it pops
  into a new green spare with a **1-UP!** jingle (and a buzz on the controller).
  With 3 spares already, every 100 bolts gives **FULL! +100** points instead.
- **Crash with a spare** and there's still a big KABOOM, slow motion and rumble, but the music only wobbles down. Then:
  a battery zooms over from the corner, every piece flies back into place (the head goes last), the gears get sucked in,
  there's a bright flash, and the face screen boots up with three loading dots before the eyes open.
  **BOLT-E IS BACK!** He flips and keeps riding. (As COZMOS or VECKTOR it says **COZMOS IS BACK!** or **VECKTOR IS BACK!**)
- For 2.5 seconds he **blinks**: he can't crash, but he can still stomp bad robots and grab bolts.
  Dangerous things close by puff away, and the world speeds back up to 85% of the speed it had.
- **In a Big Rusty fight**, Big Rusty laughs "HA HA HA!" and floats back to his spot. His fireballs, shockwaves and
  lasers vanish, and his health stays where it was. He waits until Bolt-E is back before attacking again.
  (If he was dizzy, he stays dizzy, so you might still get to stomp him!)
- **In a VROOMBA fight**, VROOMBA does a happy little **BEEP BOOP!** spin, rolls to his spot, and also waits until
  Bolt-E is back. Dust bunnies close by puff away. (If he was dizzy, he stays dizzy.)
- **In a DRONEZILLA fight**, DRONEZILLA does a happy loop-the-loop (**BZZT BZZT!**) and waits until Bolt-E is back.
  Bombs already falling still land, but no new ones come. His bombs, zap beam and mini-drones puff away.
  (If he had landed, he stays landed, so you can still stomp him!)
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
- Every game starts in **Sunny Hills**. Beat a boss and **YOU BEAT BIG RUSTY!** (or **YOU BEAT VROOMBA!**, or
  **YOU BEAT DRONEZILLA!**) shows first.
  About 2 seconds later **WELCOME TO CANDY LAND!** pops up with a sparkly jingle, and the sky, sun, clouds, hills and
  ground smoothly change color over 3 seconds. Every boss win (Big Rusty, VROOMBA or DRONEZILLA) takes you one world further:
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
- A new game always starts in Sunny Hills again. Reaching the Moon takes 4 boss wins in one game:
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
  **Need 120 more bolts!**, **Beat 3 bosses (1/3)** or **Ride to THE MOON!**
- **After a game** the Bolt Bank counts up with ticking sounds (higher and higher). Passing a prize means a party:
  **NEW LOOK!**, confetti, a jingle, and Bolt-E puts himself back together wearing it.
  If you start the next game too fast, the party simply happens on the next menu instead: **it's never lost**.
- **Old games count!** The first time the game opens for a player who already has games, the looks they already won
  are all announced at once, like **5 NEW LOOKS! Your old games count!** This note is never lost either: if a game
  starts (or LB / RB picks somebody else) before it has been showing for 2.5 seconds, it comes back, on the results
  card after that game (with that game's new looks in it too) or the next time that player is picked.
- Each player (each name) has their own Bolt Bank and their own look. "Sam" and "sam" are the same player.
- The 17 looks (bolts = bolts in the Bolt Bank):

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
  | RUSTY JR. | beat 3 bosses: Big Rusty, VROOMBA or DRONEZILLA, all games together | dressed like Big Rusty! |
  | ASTRONAUT | ride all the way to THE MOON | a space helmet, and he rides a cloud |
  | COZMOS | everyone has him | a whole new robot: white, on tank treads, with blue eyes |
  | VECKTOR | everyone has him | a whole new robot: shiny black, on tank treads, with green eyes |
- When Bolt-E blows up, his hat flies off as its own bouncing piece (and flies back on last, with his head).
- Light-colored looks (Bubblegum, Gold, King, Rusty Jr., Astronaut) get a thin dark outline, so Bolt-E is still easy
  to see on snow and candy.
- If the score database can't open, Bolt-E wears the classic look and nothing is saved (like the scores).
  COZMOS and VECKTOR can still be picked (everyone has them), but the game won't remember the pick.

## COZMOS and VECKTOR
Two new robots to play as, picked from photos of two little real-life robots! Everything is drawn in code, like Bolt-E.
- **How to pick them:** on the title screen or the results screen, press **LEFT** from CLASSIC (or click the left
  arrow): **VECKTOR** (17/17) comes first, then **COZMOS** (16/17). They're the last two looks, so RIGHT from
  ASTRONAUT gets there too. Everyone has them from the very start (0 bolts), so there's no unlock party for them, and
  the game remembers your pick like any other look.
- **COZMOS** is a little white and light-grey robot on **tank treads**, with a big rounded head, a black screen face and
  **blue eyes**, a **red disc** on the side of his head, white wheels with red dots, and a **forklift arm** that reaches
  from the side of his head down to a flat lift plate in front.
- **VECKTOR** is his shiny black cousin: black and gunmetal, a glossy black head, a **gold panel** on the side of his
  head, dark grey wheels with spokes, **green eyes**, and the same forklift arm.
- They're just as big as Bolt-E and play exactly the same: jump, double jump with a flip, stomp, grab bolts, go MEGA,
  fly the rocket board (the flames come out of the back of the treads) and float down on the parachute. The treads
  roll along with the road, and their eyes do every face Bolt-E's do: blinking, happy squints (VECKTOR's eyes stay
  rounded squares), X eyes in a crash, hearts, stars and the booting dots.
- When they crash they blow apart into their **own pieces** (head, body, both treads and the lift arm, plus gears and
  screws), and with a spare battery they zip back together, head last: **COZMOS IS BACK!**
- Their hoverboard style is ignored (they have treads). A hat would sit on their heads too, if you give them one in
  `Looks.cs`.

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

## VROOMBA (the second boss)
VROOMBA is a giant robot vacuum: a big, flat, dark dome on wheels, with a shiny silver rim, a glowing blue **CLEAN**
button on top, two big sensor eyes on his front bumper, and two spinning side brushes. He's only 75 pixels tall, so
Bolt-E can **jump right over him**.
- **The bosses take turns** (see [OPTI-MAX](#opti-max-the-fourth-boss) below for the whole rule): BIG RUSTY, VROOMBA,
  DRONEZILLA, OPTI-MAX, BIG RUSTY... and each game starts one boss further along.
- He rolls in from the right, says **BEEP BOOP!**, and then it's the same kind of fight as Big Rusty's: 6 health, his
  name on the health bar, **VROOMBA IS ANGRY!** and **VROOMBA IS FURIOUS!**, +250 and the rainbow of bolts when you beat
  him (**YOU BEAT VROOMBA!**), and then on to the next world of the World Tour.
- **His moves:**
  - **ZOOM!** He revs up (a big red **!** over him, a rising engine sound, dust flying), then dashes across the arena
    along the ground and **BONKS** into the wall. **Jump over him!** Bumping into his side or front while he's revving
    or zooming is a crash. (Tip: ride toward him and jump, then he whooshes by underneath quicker. Riding away from him
    makes him take longer to pass under you: then double jump!)
  - **BONK, then DIZZY.** Every zoom ends with a BONK: a thud, the screen shakes, he bounces back a little and flips
    **upside down**, wheels in the air, spinning slowly, with dizzy stars. **That's when you jump on him!** Touching him
    while he's dizzy never hurts. Stomped, he blinks, flips back over and rolls to the side far from Bolt-E (hopping
    right over Bolt-E if he's in the way), then attacks again.
  - **SUCK!** He turns on his vacuum: wind lines swirl into his mouth with a whooshing hum for 3 seconds. Bolt-E gets
    pulled toward him: **ride away!** (The pull is slower than riding.) Touching him is a crash. Every loose bolt in
    the arena flies into his mouth: **YUM!** But they're not lost: **stomp him and half of them fountain back out**
    (grab them!), and when he's beaten all the rest come out. After the SUCK comes a ZOOM, unless jump dust distracts him.
  - **JUMP DUST!** Every jump (including the double jump) leaves a dust puff during his fight. Ride away from it!
    Before a zoom, or while sucking or spitting, he spots the dust (**DUST!**) and chases that puff instead of Bolt-E.
    He rides slower than you, but touching him on the way still hurts. When he stops to vacuum it, he's harmless.
    His filter clogs: **CLOGGED! STOMP!** He flips upside down for 3.2 seconds: jump on him to deal the usual damage!
    Dust fades after 8 seconds. He ignores it during a zoom, while dizzy or recovering, and briefly after a clog,
    so his normal attack pattern still matters. Dust and its vacuum sound clear when the fight ends or Bolt-E crashes.
  - **DUST BUNNIES** (when he's ANGRY): he spits out 2 fluffy grey dust bunnies with big eyes (3 when he's FURIOUS).
    They hop after Bolt-E: **stomp them** (like Egg-Bots, for the stomp combo), but bumping into one is a crash.
    Then he zooms.
  - **TURBO** (when he's FURIOUS): his zooms are faster, and every other time he zooms there and back first. The last
    zoom always bonks, so he always ends up dizzy (a bit shorter when he's FURIOUS, but never under 1.8 seconds).
- **His plan:** first a ZOOM, then SUCK + ZOOM, again and again. ANGRY: DUST BUNNIES + ZOOM, ZOOM, SUCK + ZOOM. FURIOUS:
  DUST BUNNIES + ZOOM, SUCK + ZOOM, ZOOM, all with TURBO zooms. Without a dust distraction, every attack ends with a
  BONK, so you always get your chance to stomp him.
- His eyes glow blue, then orange (ANGRY) and red (FURIOUS), and the CLEAN button turns red too. Beaten, he blows apart
  into his lid, the button, the bumper, the brushes and the wheels, in a big cloud of dust.
- **Nobody can drive VROOMBA** (only Big Rusty, see below): in his fights the second controller does nothing.
- If Bolt-E runs out of batteries, VROOMBA rolls away happily.

## DRONEZILLA (the third boss)
DRONEZILLA is a giant attack drone, seen from the side: a chunky gunmetal body with a yellow-and-black hazard stripe,
two arms with a spinning rotor on top of each, a big round red camera eye underneath, a dark visor with two little LED
eyes, blinking red and green lights, an antenna, a battery on his side, and two landing skids. His shadow on the ground
gets bigger as he comes lower, so you can always see where he is.
- **The bosses take turns** (BIG RUSTY, VROOMBA, DRONEZILLA, OPTI-MAX): see [OPTI-MAX](#opti-max-the-fourth-boss) below
  for the whole rule.
- He flies in from the top-right with a rising buzz, and then it's the same kind of fight as the others: 6 health, his
  name on the health bar, **DRONEZILLA IS ANGRY!** and **DRONEZILLA IS FURIOUS!**, +250 and the rainbow of bolts when
  you beat him (**YOU BEAT DRONEZILLA!**), and then on to the next world of the World Tour.
- **Up in the air he's safe to touch.** He cruises way up high, higher than a double jump. Bumping into him up there
  never hurts (not even with the Moon's floaty jumps), but you can't stomp him up there either.
- **His moves:**
  - **BOMBS AWAY!** He flies over Bolt-E and drops 3 bombs, one after another (5 when he's FURIOUS). Each one whistles,
    and a **blinking red ring** on the ground shows where it will land: **ride out of the ring!** A bomb (or its
    BOOM) hitting Bolt-E is a crash.
  - **SWOOP!** A big red **!** flashes, then he dives across the arena in a big U, down low: **jump over him!** Touching
    him during the swoop is a crash. Then he climbs back up at the other side. FURIOUS: there AND back.
  - **ZAP BEAM!** (when he's ANGRY) A red circle on the ground follows Bolt-E... then it **LOCKS** (it stops and glows):
    **ride out of it!** Then a zap beam fires straight down on it. Being in the beam is a crash.
  - **MINI-DRONES!** (when he's ANGRY) 2 little buzzing drones (3 when he's FURIOUS) with one rotor and one eye. They
    hover at head height and drift slowly after Bolt-E: **stomp them** (for the stomp combo), but bumping into one is a
    crash.
  - **LOW BATTERY!** After every attack his battery runs low: beep beep booop! His battery blinks red, a yellow ring on
    the ground shows where he'll land (never close to Bolt-E), and he glides down and **LANDS**. His rotors spin down,
    and a bouncing yellow arrow shows over him: **JUMP ON HIM!** (He sits there for 2.6 seconds, 1.9 when he's FURIOUS.)
    Touching him while he's landed never hurts. Stomped, he blinks, his rotors sputter back on, and he wobbles back up.
    If you miss him, his battery charges up and he takes off by himself.
- **His plan:** BOMBS, land; SWOOP, land; again and again. ANGRY: MINI-DRONES + BOMBS, land; ZAP BEAM + SWOOP, land.
  FURIOUS: the same, with 5 bombs and double swoops. Every attack ends with a landing, so you always get your chance.
- **How he looks:** his LED eyes are round and green when he's calm, squinty and mischievous when he attacks, sleepy and
  half shut when his battery is low, spinning spirals when he's stomped, happy when he loops the loop, and X eyes when
  he's beaten. They turn orange when he's ANGRY and red when he's FURIOUS (then he crackles with sparks, too). His red
  camera eye always looks at Bolt-E. He leans into the way he flies, and bobs gently when he hovers. His rotors are a
  see-through blur when they're fast (even faster in a swoop) and stop when he lands.
- Beaten, every bomb and mini-drone puffs away. He sputters, smokes, spins down out of the sky, **crashes**, and blows
  apart into his body, his 4 rotor blades, his 2 arms, his camera and his skids.
- **Nobody can drive DRONEZILLA** (only Big Rusty): in his fights the second controller does nothing.
- If Bolt-E runs out of batteries, DRONEZILLA zooms up and away, off the top of the screen.

## OPTI-MAX (the fourth boss)
OPTI-MAX is a tall, slim humanoid robot (a factory helper!), seen from the side and a little from the front: a glossy
black egg-shaped head with a thin glowing **teal light strip** around his face and two soft glowing eyes, a black neck,
a white chest plate and white shoulder caps, white arms and legs with black joints (shoulders, elbows, hips and knees),
black hands with fingers, and black feet. He's about 290 pixels tall, but his bump box stays under 300 pixels, so a
**double jump always clears him**. He moves like a person: his legs swing when he walks, his arms swing the other way,
and his knees bend when he crouches or lands. (He's built like a little skeleton of angles: see `Pose` in `OptiMax.cs`.)
- **The bosses take turns:** BIG RUSTY, then VROOMBA, then DRONEZILLA, then OPTI-MAX, then Big Rusty again... and every
  game starts one boss further along: game 1 is RUSTY, VROOMBA, DRONEZILLA, OPTI-MAX...; game 2 is VROOMBA, DRONEZILLA,
  OPTI-MAX, RUSTY...; game 3 starts with DRONEZILLA; game 4 starts with OPTI-MAX; and game 5 is like game 1 (counting
  from when the game was opened). So you meet every boss, even if you only ever reach the first fight.
- He walks in from the right with heavy clanky footsteps and does a little wave, and then it's the same kind of fight
  as the others: 6 health, his name on the health bar, **OPTI-MAX IS ANGRY!** and **OPTI-MAX IS FURIOUS!**, +250 and
  the rainbow of bolts when you beat him (**YOU BEAT OPTI-MAX!**), and then on to the next world of the World Tour.
- **His moves:**
  - **BOX TOSS!** There's a little stack of cardboard boxes at each end of the arena. He bends down, picks up the top
    box, lifts it over his head and throws it in a high arc at Bolt-E (whoosh!). A **shadow** on the ground shows where
    it will land: **ride away!** It lands (crunch!), bounces once more the same way (jump it!) and breaks into bits.
    2 boxes (3 when he's ANGRY, 4 and faster when he's FURIOUS). Touching a box is a crash.
  - **SUPER STOMP!** He crouches, jumps up high and lands with a STOMP: the screen shakes and two low glowing teal
    **shockwaves** run along the ground, away from him both ways. **Jump over them!** FURIOUS: two stomps in a row.
  - **SOCCER KICK!** (when he's ANGRY) He drops a ball and kicks it toward Bolt-E. It bounces in big hops (boing!):
    **jump over it, or ride under it** while it's high up. It bounces off the far wall once, then pops. FURIOUS: 2 balls.
  - **ROBOT RUN!** (when he's ANGRY) He sprints across the arena with pumping arms, so the fight moves around. Touching
    him is a crash (double jump over him!), but he **never squashes Bolt-E against a wall**: he always stops at least
    200 pixels before him.
  - **SOFTWARE UPDATE: STOMP HIM!** Every attack ends with him **freezing** in the middle of what he was doing:
    **UPDATING...** Then he **kneels on one knee** with his head bowed (the top of his head is only about 125 pixels up)
    and a progress bar fills over his head, with little computer chirps, while a yellow arrow bounces over his back.
    **Jump on his back!** (The update takes 2.6 seconds, 2.0 when he's FURIOUS, never less than 1.8.) He never kneels
    on Bolt-E: if Bolt-E is close, he walks a few steps away first. Touching him while he updates never hurts.
    Stomped: sparks fly from his head, an error buzz, the bar says **ERROR!** in red, and he wobbles back up. Missed
    him? The bar reaches 100%, **DING!**, and he stands up.
- **His plan:** BOX TOSS, update; SUPER STOMP, update; again and again. ANGRY: SOCCER KICK + BOX TOSS, update; ROBOT RUN
  + SUPER STOMP, update. FURIOUS: the same, faster, with 4 boxes, 2 balls and double stomps. Every attack ends with an
  update, so you always get your chance.
- **How he looks:** his eyes are calm soft glows, narrow when he attacks, spinning loading circles while he updates,
  happy arcs when he waves or dances, glitchy red flickers when he's stomped, and X eyes when he's beaten. His eyes
  and face strip are teal, then orange (ANGRY), then red (FURIOUS, with crackly sparks).
- **If Bolt-E crashes but has a spare battery**, he does a funny robot dance (**BEEP BOP DANCE!**) and waits until
  Bolt-E is back. Boxes and balls already flying finish, but no new ones come, and the rebuild clears them. He never
  dances in the middle of an update, so he never steals your stomp chance.
- Beaten, every box, ball and wave (and his box stacks) puffs away. He sparks and stumbles, **falls over backward**
  with a CLANK, and blows apart into his head, chest, arms, legs, hands and feet.
- **Nobody can drive OPTI-MAX** (only Big Rusty): in his fights the second controller does nothing.
- If Bolt-E runs out of batteries, OPTI-MAX waves goodbye and jogs off the screen.

## Dad drives Big Rusty
- **With one controller (or none), the computer drives Big Rusty**, exactly like the original fight.
- **Plug in a second controller** and Dad (or Mom) can drive him, while the kid keeps playing on the first controller
  exactly like always. The title card says so: **2 controllers! The 1st one plays Bolt-E, and the 2nd one can drive
  BIG RUSTY!**
- **The first controller plugged in is Bolt-E's; plug in a second one to drive Big Rusty.** If they're swapped (the
  kid's buttons drive Big Rusty and Dad's move Bolt-E), swap the controllers, or unplug both and plug Bolt-E's in first.
  (Some keyboards, mice and steering wheels pretend to be a controller too. They never steal Bolt-E, because a real
  game controller always comes first. They can't drive Big Rusty, and the title card doesn't count them.)
- Bolt-E's controller is always the real controller with the lowest number, and that's on purpose: if the kid's
  controller goes away for a moment (flat batteries, or it fell asleep), Dad's controller plays Bolt-E meanwhile, and
  when the kid's comes back, Godot gives it the lowest free number again, so Bolt-E goes back to it all by himself.
- If the title says 2 controllers but only one is plugged in (Steam with controller support can make a pretend copy), close Steam or turn off Steam Input for Robot Dash.
- When **BIG RUSTY** shows up, press one of Dad's buttons on the second controller to take over: **DAD IS DRIVING BIG
  RUSTY!** shows, Rusty laughs and puts on a little **blue cap with a star**, his bar says **BIG RUSTY (DAD)**, and a tip
  shows Dad's buttons. If nobody touches the second controller, the computer keeps driving, so a spare controller lying
  around changes nothing.
- Dad's buttons (on the second controller):

  | Button | Big Rusty does |
  |---|---|
  | **X** | bouncing fireballs |
  | **Y** | drops Pup-Bot helpers (once he's ANGRY) |
  | **A** | a GROUND POUND (two in a row when he's FURIOUS) |
  | **B** | the giant laser (only when he's FURIOUS) |
  | **LB** or **RB** | "HA HA HA!" |
- **Thinking dots:** when Rusty is ready for an order, three white dots (with a dark ring, so you can see them in front
  of the sun and in the snow) bob over his head. That's the moment to press a button!
  While he's busy (shooting, pounding, dizzy...) a button just says **Wait...**, and a move he isn't angry enough for yet
  says **Not yet!** (Pressing again and again doesn't stack up the words, and they never float up over his health bar.)
- **Fair-play rules**, so a 6-year-old can win:
  - After **2 attacks** Rusty is **TIRED**: only the ground pound works (**TIRED! Press A!**). Laughing is always
    allowed, but only once every 2 seconds, and **only 2 laughs in a row**: then LB / RB says **Wait...** until Rusty has
    done a real move (Dad's pick, or his own brain's after 3 seconds). So laughing over and over can never stop the fight.
  - After **every ground pound** he's dizzy, like always, so Bolt-E gets his chance to stomp him. His shadow still
    follows Bolt-E and turns red before he slams down. When Bolt-E stomps him, Dad's controller gives a little buzz too.
  - If Dad presses nothing for **3 seconds**, Rusty's own brain picks his next move, so the fight never stops.
  - When Bolt-E crashes, Rusty still laughs and waits until Bolt-E is back together.
- After the giant laser Rusty is down low: he floats back up to his spot smoothly, even if Dad picks his next move
  right away.
- Beat him and it's **YOU BEAT DAD'S BIG RUSTY!** with the usual +250 and the rainbow of bolts (and Dad's cap flies off
  when he blows up!). Lose the last battery while Dad drives him, and the results card says **DAD GOT YOU!**
- Every new Big Rusty starts out driving himself: press a button on the second controller again to take over.
- **VROOMBA, DRONEZILLA and OPTI-MAX can't be driven.** In their fights the second controller's buttons do nothing (no banner, no "Wait...").
- **The second controller never moves Bolt-E**: its buttons don't jump, its stick and D-pad don't ride, and on the
  menus it doesn't start games, switch the music, change looks or switch players. It only drives Big Rusty, only in a
  boss fight, and only with the buttons above (its other buttons do nothing). The keyboard can't drive Big Rusty.
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
        --headless --path . --fixed-fps 60 --quit-after 120000 --log-file "$scratch\godot.log" `
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
  --headless --path . --fixed-fps 60 --quit-after 120000 --log-file "$S\godot.log" -- "--db=$S\selftest.db" --selftest
```
During the self-test the boss that comes by itself is always Big Rusty (so the older tests always meet the boss they
expect); the VROOMBA tests ask for VROOMBA, and one test checks the taking-turns rule like a real game. A few of the
VROOMBA tests run the game 3 times as fast while they wait for his moves, so the self-test stays quick.
To run only the jump-dust checks and VROOMBA drawing coverage, add `--selftest-dust` after `--selftest` in the same command.
To run only DRONEZILLA's checks (and the taking-turns rule), add `--selftest-drone` instead. (The DRONEZILLA tests ask for
him too, and some of them also run the game 3 times as fast.)
To run only OPTI-MAX's checks (and the taking-turns rules), add `--selftest-optimax` instead.
Exit codes: **0** = everything passed, **1** = something failed, **2** = refused to start (bad `--db`),
**3** = it took too long. A good run has no lines with `FAIL:`, `ERROR:`, `SCRIPT ERROR` or `Unhandled exception`
in the output or in `godot.log`. The tests are in `Scripts/Main.SelfTest.cs`, and new features add theirs there.
Just before the DONE line it also prints how many seconds of game time the tests took (the watchdog stops them at 1500,
so the commands above use `--quit-after 120000`).
(They expect the normal tuning numbers, so if you change a number like `StartingBatteries` or `MegaSeconds`,
a world in `Worlds.cs`, a look in `Looks.cs`, a family race number in `Main.Family.cs`, Big Rusty's
`AttacksBeforeHeMustPound`, `DriverWaitBeforeAI` or `LaughsInARow`, VROOMBA's numbers in `Vroomba.cs`, DRONEZILLA's
numbers in `Dronezilla.cs`, OPTI-MAX's numbers in `OptiMax.cs`, or Dad's
`DriveButtons` (the words on the screen
follow your buttons, but the tests press the normal ones), some tests will say FAIL. Changing `DriverName`
to MOM, or a flag's `PoleHeight`, is fine: the tests use whatever is there.)
The self-test only pretends to press buttons: its pretend Bolt-E controller is device 99 and its pretend second controller
(the one that drives Big Rusty) is device 98. It never touches a real keyboard or controller, and while it runs, real
controllers being plugged in or out don't change anything. (One test pretends that controllers get plugged in and out,
a phantom one too, to check that Bolt-E's controls follow the plugs.)
Start each run with a fresh `selftest.db` (the commands above delete the old one). Test players' names start with T,
like `TCOW`; if a test name was already used in that database, the test adds a number (`TCOW2`). (One test also saves a
game called `Player` in the scratch database, to check that games without a name count as a person.)

## Where the code lives

The boss score is in `Scripts/Audio/BossSong.cs`: each melody line is one bar of eight eighth-note steps (`-` holds
the previous note, `.` rests). `Chords` supplies the harmony; `BeatsPerMinute` is 144 before world and boss-phase
speed changes. The flying drone's score, **Rotor Rush**, is in `Scripts/Audio/DroneSong.cs`: 32 bars in E minor at
152 BPM (about 50.5 seconds), with a pulsing rotor arpeggio, climbing melody, hovering bridge, and diving final phrase.
The humanoid robot's score, **Iron Shadow**, is in `Scripts/Audio/HumanoidSong.cs`: 32 bars at 132 BPM
(about 58.2 seconds), with a D-minor/Phrygian melody, a lurching bass pulse, metallic FM percussion, a sparse
stalking passage, and a tightening final pursuit. It is ready for the humanoid boss to select at spawn.
`Scripts/Audio/SoundBoard.Music.cs` caches all four synthesized themes and crossfades between them.
The self-test writes listenable `titan-circuit.wav`, `rotor-rush.wav`, and `iron-shadow.wav` files beside its scratch database.
Add `--selftest-music` after `--selftest` to run just the music playback and audio checks.

`SpawnBoss` automatically selects **Rotor Rush** for DRONEZILLA and **Titan Circuit** for BIG RUSTY and VROOMBA.
The score continues while DRONEZILLA lands to recharge, follows the world's speed and his angry phases, and dips
during a rebuild. Victory returns to the runner theme; game over powers the music down normally.
The optional third argument to `SetBossMusic` selects `BossMusicTheme.RotorRush`, `BossMusicTheme.IronShadow`,
or `BossMusicTheme.TitanCircuit` (the default).
Phase changes use `SetMusicSpeed`, and victory calls `SetBossMusic(false, CurrentWorld.MusicSpeed)`.

To connect the new humanoid boss, select `BossMusicTheme.IronShadow` in its `SpawnBoss` theme selection:
`sounds.SetBossMusic(true, 1.1f * CurrentWorld.MusicSpeed, BossMusicTheme.IronShadow)`.
The score already supports phase changes, muting, rebuilding, victory crossfades, game over, and restarting.

| File | What it does |
|---|---|
| `Scripts/Main.cs` | The game boss: draws the world (in the colors of the world you're in), speeds up, spawns obstacles, bolts and the bosses (`SpawnBoss`), checks for bumps (every crash goes through `Bonk`), saves scores, and the controls: which controller is Bolt-E's (the first one plugged in, `PickControllerOne`), tying his controls to it (`BindControllerOne`), sending a second controller's presses to Big Rusty, and telling real controllers from phantom ones (`PadIsKnown`) |
| `Scripts/Main.Batteries.cs` | Spare batteries: charging with bolts (1-UP!), the magic rebuild after a crash, blinking, puffing away dangers |
| `Scripts/Main.PowerUps.cs` | Rainbow ? boxes: when a box comes, opening it, starting and ending powers, SMASH! and SPLAT!, the magnet pull, the rocket landing |
| `Scripts/Main.Worlds.cs` | The World Tour: changing worlds after a Big Rusty win, the smooth color change, and drawing each world's special things (stars, moon, Earth, skyscrapers, mountains, lollipops, pine trees, sprinkles, road dashes, craters, snow, the night glow) |
| `Scripts/Worlds.cs` | Every world on the World Tour: its colors, sky thing, scenery, weather, gravity, music speed and Big Rusty's outfit. **Make your own world here!** |
| `Scripts/WorldPostcards.cs` | The World Tour postcards on the menus (pictures, grey "?" cards and gold NEW! ribbons) |
| `Scripts/Looks.cs` | All 17 looks: their colors, hats, boards, trails, which robot wears them (Bolt-E, COZMOS or VECKTOR) and what it takes to win them. **Invent your own look here!** |
| `Scripts/Main.Looks.cs` | The Bolt Bank and the looks: loading a player's bank and look, changing looks on the menus, the count-up after a game, and the unlock parties and the first-launch "N NEW LOOKS!" note (which are never lost) |
| `Scripts/Main.Family.cs` | The family race: who's playing (LB / RB, TAB), "GO, MAX!" and "DAD'S TURN!", the flags on the road and passing them, NEW HIGH SCORE! in the middle of a ride, fireworks, who's champ today, rounds, and "So close!" |
| `Scripts/RecordFlag.cs` | A flag on the road: the pole, the waving cloth, the star or X eyes, the words, and tipping over |
| `Scripts/Main.Vroomba.cs` | VROOMBA's side of the game: the bosses taking turns (`BossForFight`), his sounds and dust bunnies, jump dust lures, his vacuum pulling Bolt-E and gobbling up loose bolts (YUM!), and bolts fountaining back out when stomped |
| `Scripts/JumpDust.cs` | Jump dust: a visible puff that settles, fades, and shrinks as VROOMBA cleans it |
| `Scripts/Main.VroombaDustTests.cs` | Self-test checks for jump dust, luring and stomping, arena edges, and encounter cleanup |
| `Scripts/Main.Dronezilla.cs` | DRONEZILLA's side of the game: his sounds (and the rotor buzz, which never keeps playing after the fight), his bombs, zap beam and mini-drones, checking if they bumped into Bolt-E, and puffing them away after a rebuild or when he's beaten |
| `Scripts/Main.DronezillaTests.cs` | Self-test checks for DRONEZILLA (and the three bosses taking turns) |
| `Scripts/Main.OptiMax.cs` | OPTI-MAX's side of the game: his sounds (none of them keep playing after the fight), his box stacks, thrown boxes, soccer balls and teal shockwaves, checking if they bumped into Bolt-E, and puffing them away after a rebuild or when he's beaten |
| `Scripts/Main.OptiMaxTests.cs` | Self-test checks for OPTI-MAX (and the four bosses taking turns) |
| `Scripts/Main.Driver.cs` | Dad drives Big Rusty with a second controller (only a real one: phantom controllers can't): its buttons (X, Y, A, B, LB and RB in `DriveButtons`), taking over ("DAD IS DRIVING BIG RUSTY!", with a tip made from `DriveButtons`), giving orders ("Wait...", "TIRED! Press A!", "Not yet!", which never pile up or float over his health bar), Dad's buzz when Bolt-E stomps him, and who's driving (`DriverName`) |
| `Scripts/BankBox.cs` | The Bolt Bank box in the top-left corner of the menus (the number, the gold hexagon and the bar to the next prize) |
| `Scripts/LookPicker.cs` | The look picker under Bolt-E on the menus (the arrows, the look's name, the padlock and what it takes to win it) |
| `Scripts/Main.SelfTest.cs` | Grown-up testing code: the headless self-test and all its checks |
| `Scripts/PowerUps.cs` | The three powers: how long they last, how big, how high (the "try changing these!" numbers), their names and little icons |
| `Scripts/PowerBox.cs` | The rainbow ? box with its two orbiting stars |
| `Scripts/PowerRoulette.cs` | The slot machine over Bolt-E's head (tick-tick-tick!) |
| `Scripts/Robot.cs` | Bolt-E: jumping, gravity (and floaty Moon gravity, even when a new world comes in the middle of a jump), blinking, music notes, the booting face, growing giant (MEGA), the rocket board and parachute, the magnet, heart eyes, star eyes, drawing the robot, and his looks (hats, boards, trails, rainbow eyes and the dark outline) |
| `Scripts/RobotBodies.cs` | COZMOS and VECKTOR, the two tank-tread robots: their "try changing these!" shapes, drawing the rolling treads and wheels, the box body, the big head with its screen face (and the red disc or gold panel), the forklift arm, and the pieces they blow apart into |
| `Scripts/RobotExplosion.cs` | The crash: Bolt-E bursts into bouncing pieces, sparks, smoke, and dizzy stars (and flies back together in the magic rebuild). Smashed crates, cones and drones use it too |
| `Scripts/Enemy.cs` | Pup-Bot, Egg-Bot, VROOMBA's dust bunnies and DRONEZILLA's mini-drones: how they move, look, and get squashed |
| `Scripts/BossBase.cs` | What every boss has: 6 health and the ANGRY and FURIOUS phases, stomping, when bumping into him hurts, flying away, the laugh when Bolt-E crashes, blowing up, and being driven (only Big Rusty can be) |
| `Scripts/Boss.cs` | Big Rusty: his size, his three phases, every attack, getting dizzy, blowing up, his World Tour outfits, and being driven by Dad (waiting for orders with thinking dots, the fair-play rules like TIRED and only 2 laughs in a row, and Dad's blue cap) |
| `Scripts/Vroomba.cs` | VROOMBA, the giant robot vacuum: his moves (ZOOM, BONK, dizzy upside down, SUCK, dust bunnies, TURBO), his attack plan, his "try changing these!" numbers, and drawing him (lid, CLEAN button, bumper, sensor eyes, spinning brushes, wheels, dust, wind, and blowing apart) |
| `Scripts/Dronezilla.cs` | DRONEZILLA, the giant attack drone: his moves (BOMBS AWAY, SWOOP, ZAP BEAM, MINI-DRONES, LOW BATTERY and landing, the loop-the-loop, crashing down), his attack plan, his "try changing these!" numbers, and drawing him (body, hazard stripes, arms, spinning rotors, camera eye, LED eyes, nav lights, antenna, battery, skids, shadow, and blowing apart) |
| `Scripts/OptiMax.cs` | OPTI-MAX, the humanoid robot: his moves (BOX TOSS, SUPER STOMP, SOCCER KICK, ROBOT RUN, the SOFTWARE UPDATE and kneeling, the BEEP BOP DANCE, falling over), his attack plan, his "try changing these!" numbers, his skeleton of angles and poses (walking, running, kneeling...), and drawing him (glossy head with the teal face strip, eyes, white plates, black joints, hands and feet, the progress bar, and blowing apart) |
| `Scripts/ThrownBox.cs` | OPTI-MAX's cardboard boxes: the high arc, the shadow where they'll land, one bounce, and breaking into bits |
| `Scripts/KickBall.cs` | OPTI-MAX's soccer ball: big bouncy hops, one bounce off the wall, then POP |
| `Scripts/StompWave.cs` | The low glowing teal shockwaves from OPTI-MAX's SUPER STOMP |
| `Scripts/BoxStack.cs` | The little stacks of boxes at the ends of OPTI-MAX's arena (a new top box pops back on after he takes one) |
| `Scripts/DroneBomb.cs` | DRONEZILLA's bombs: falling with a red target ring underneath, and the BOOM |
| `Scripts/ZapBeam.cs` | DRONEZILLA's zap beam: the red circle that follows Bolt-E, locks, then the beam zapping straight down |
| `Scripts/Blast.cs` | Big Rusty's bouncing fireballs |
| `Scripts/Shockwave.cs` | The waves that roll out from his ground pound |
| `Scripts/Laser.cs` | His giant laser (warning line, then the beam) |
| `Scripts/ScorePopups.cs` | The floating "+25", "1-UP!", "SMASH! +25", "HA HA HA!" and "Wait..." words (how long they stay, how fast they float up) |
| `Scripts/Obstacle.cs` | Crates, cones, crate stacks, and drones (and smashing them into pieces) |
| `Scripts/BoltPickup.cs` | The spinning golden bolts (and bolts flying out of VROOMBA like a fountain) |
| `Scripts/Hud.cs` | Score text, title screen (with "Who's playing?", Family Champions, and the controller line, which says when a 2nd controller can drive Big Rusty), game-over screen (results card, with the Today and So close lines and LB / RB around the play button), the flying spare battery, the power bar, the boss's health bar (it says "VROOMBA", "DRONEZILLA" or "OPTI-MAX", or "BIG RUSTY (DAD)" while Dad drives Big Rusty), the Best number flashing gold, the menu strip on the left (the Bolt Bank, the postcards, the NEW LOOK! words and the look picker) |
| `Scripts/BatteryRow.cs` | The spare batteries under "Bolts" (green spares and the yellow charging one) |
| `Scripts/ScoreDatabase.cs` | All the SQLite code (including everyone who plays, their bests, and today's scores) |
| `Scripts/Audio/Song.cs` | The theme song, written as notes you can change |
| `Scripts/Audio/SoundEffects.cs` | Every sound effect (jump, bolt, explosion, jingles, power-ups, SMASH, the rocket, the new-world jingle, the NEW LOOK! jingle, the "ta-da-DA!" for passing a flag, VROOMBA's rev-up, zoom, wall bonk, vacuum hum, BEEP BOOP and "ptoo!", and DRONEZILLA's rotor buzz, bomb whistle, LOW BATTERY beeps, power-down, take-off whir and BZZT BZZT, and OPTI-MAX's clanky footsteps, box whoosh and crunch, ball boing and kick, UPDATING chirps, DING!, error buzz and dance beat...) |
| `Scripts/Audio/Synth.cs` | The tiny synthesizer that turns notes into sound with math (with a buzzy saw wave for VROOMBA) |
| `Scripts/Audio/SoundBoard.cs` | Plays the music and sound effects (and remembers how fast the music should play, so it wobbles down while Bolt-E is in pieces, even if you switch it off and on) |
| `Scripts/Shapes.cs` | Drawing helpers (rounded rectangles, ovals, stars, happy eyes, X eyes, heart eyes) |

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
- COZMOS and VECKTOR's colors are in `Looks.cs` (try a pink COZMOS!), their names in `Looks.RobotName`, and their
  shapes at the top of `RobotBodies.cs`: how many cleats the treads have (`CleatCount`), how big the wheels are
  (`HubRadius`), and where the eyes sit on the screen
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
- How big Big Rusty is: `Size` in `Boss.cs` (1.5 now; try 2!)
- How tough the bosses are: `MaxHealth` in `BossBase.cs`, and which attacks Big Rusty uses in `AttackPlan` in `Boss.cs`
- How long Big Rusty stays dizzy: `DizzyTime` in `Boss.cs`
- VROOMBA, all in `Vroomba.cs`: `ZoomSpeed` and `TurboSpeed` (how fast he zooms. Funny: faster is EASIER to jump
  over, because he's under Bolt-E for a shorter time! Below about 400 a single jump standing still can't clear him),
  `TurboZooms` (how many times a TURBO zoom goes across), `RevSeconds` (how long he revs up before a zoom),
  `SuckSeconds` and `SuckPull` (how long the vacuum is on, and how hard it pulls Bolt-E: 0.55 = 55% of his riding
  speed; at 1 or more he can't ride away!), `DizzySeconds` and `FuriousDizzySeconds` (how long he's dizzy;
  `ShortestDizzy` keeps it at least 1.8 seconds), `BunniesWhenAngry` and `BunniesWhenFurious`, his attack lists
  (`PhaseOnePlan`, `AngryPlan`, `FuriousPlan`), and his colors (`VroombaBlue` and friends). In `Main.Vroomba.cs`:
  `SuckBoltSpeed` (how fast bolts fly into his mouth) and `DustBunnyChase` (how fast dust bunnies hop after Bolt-E)
- Jump dust: `Lifetime` in `JumpDust.cs`, `MaxJumpDust` in `Main.Vroomba.cs`, and in `Vroomba.cs`: `DustChaseSpeed`,
  `DustNoticeSeconds`, `DustChaseSeconds`, `DustVacuumSeconds`, `DustDizzySeconds` and `DustLureCooldown`
- DRONEZILLA, all in `Dronezilla.cs`: `CruiseHeight` (how high he flies: under about 365 a double jump reaches him,
  but that's still harmless), `BombsWhenCalm` and `BombsWhenFurious` (how many bombs), `BombEvery` (time between
  bombs), `BombChaseSpeed` (how fast he follows Bolt-E while bombing: faster than 430 and you can't get away!),
  `SwoopSeconds` (how long a swoop takes: shorter is faster), `SwoopLow` (how low he swoops: try 120 and you can ride
  under him!), `ZapTrackSeconds`, `ZapLockSeconds` and `ZapFireSeconds` (the zap beam's timing), `MinisWhenAngry` and
  `MinisWhenFurious`, `LandedSeconds` (how long he sits on the ground: your stomp time! `ShortestLanded` keeps it at
  least 1.8 seconds), `LandAway` (how far from Bolt-E he lands), his attack plans (`PhaseOnePlan`, `AngryPlan`,
  `FuriousPlan`), and his colors (`DroneYellow` and friends). In `Main.Dronezilla.cs`: `MiniDroneChase` (how fast
  mini-drones drift after Bolt-E). In `DroneBomb.cs`: `Gravity` (how fast bombs fall) and `BlastHalfWidth`. In
  `Enemy.cs`: `MiniDroneHover` (how high mini-drones hover)
- OPTI-MAX, all in `OptiMax.cs`: `BoxesWhenCalm`, `BoxesWhenAngry` and `BoxesWhenFurious` (how many boxes),
  `BoxFlightSeconds` (how long a box flies: shorter is harder), `StompJumpHeight` (how high he jumps), `BallsWhenAngry`
  and `BallsWhenFurious`, `WalkSpeed` and `RunSpeed`, `RunStopShort` (how far before Bolt-E a run stops), `UpdateSeconds`
  and `FuriousUpdateSeconds` (your stomp time! `ShortestUpdate` keeps it at least 1.8 seconds), `KneelAway` (how far
  from Bolt-E he kneels), `FuriousSpeedUp`, his attack plans (`PhaseOnePlan`, `AngryPlan`, `FuriousPlan`), his poses
  (`StandPose`, `KneelPose`, `CrouchPose`... every angle is in radians: try making him lean way back!), his bone lengths
  (`ThighLength`, `TorsoLength`...) and his colors (`Teal` and friends). In `ThrownBox.cs`: `Gravity` and `BounceUp`.
  In `KickBall.cs`: `KickSpeed` and `HopSpeed` (how high the ball bounces). In `StompWave.cs`: `Speed` and `Height`
- Which boss comes when: `BossForFight` in `Main.Vroomba.cs` (make it always OPTI-MAX: `BossKind.OptiMax`!)
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
- Dad drives Big Rusty: who's driving, `DriverName` in `Main.Driver.cs` (try MOM or GRANDMA!), which button does what,
  `DriveButtons` in `Main.Driver.cs` (try swapping them around, or giving LB and RB different moves: Dad's tip and the
  "TIRED! Press A!" words follow along, with the button names from `ButtonNames`), and in `Boss.cs`
  `AttacksBeforeHeMustPound` (how many attacks before he's TIRED: try 1 for an easier fight), `DriverWaitBeforeAI`
  (how long he waits for Dad's order before his own brain picks), `LaughsInARow` (how many laughs Dad gets before a
  real move has to come) and `CapBlue` (the color of Dad's cap)
