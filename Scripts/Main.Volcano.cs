using Godot;

/// <summary>
/// VOLCANO ISLAND, the 7th world! (This is more of Main: "partial" means one class spread over several files.)
///
/// There's no ground here: just a river of glowing LAVA, with rocks floating on it. Bolt-E rides from rock to rock,
/// jumping over the gaps. When he lands on a rock it starts to SINK after a moment, so keep moving! Lava bubbles pop
/// up out of the gaps (a glowing spot warns first). Falling into the lava is a crash (HOT HOT HOT!).
/// Some rocks have a HOLE in them, with a sign showing how HARD it is (1, 2 or 3 flames). Ride into the hole, and
/// down you go into a LAVA CAVE (see Main.LavaCave.cs)! Or jump over it, if you'd rather stay up here.
/// VolcanoPlanner (in VolcanoThings.cs) puts the rocks in FAIR patterns, and VolcanoScenery.cs draws the world.
/// When a boss is coming, the rocks join together into one big SOLID ROCK floor, so every boss fights just like
/// everywhere else.
/// </summary>
public partial class Main
{
    // ---- VOLCANO ISLAND (try changing these!) ----
    public const float SinkDelay = 0.4f;        // seconds after Bolt-E lands on a rock before it starts to sink
    public const float SinkSpeed = 16f;         // how fast a rock sinks (pixels per second)
    public const float SinkDepth = 40f;         // how far a rock sinks before it's under the lava
    const float MegaSinkTimes = 1.6f;           // MEGA BOLT-E is heavy: rocks sink this much faster under him!
    public const float LavaSurfaceY = GroundY + 34f; // the lava's top (the rocks stick out of it 34 pixels)
    const float LavaEdgeForgiveness = 18f;      // riding this close past the edge of a rock is still safe (pixels)
    const float HotHopSeconds = 0.6f;           // the "HOT HOT HOT!" hop lasts this long, then KABOOM
    const float FirstHoleAt = 26f;              // seconds on VOLCANO ISLAND before the first hole comes...
    const float HoleEvery = 30f;                // ...and then about this many seconds between holes...
    const float HoleWobble = 5f;                // ...give or take this many
    const float HoleTrigger = 44f;              // riding this close to the middle of a hole drops you in
    const float NoHolesNearBoss = 60f;          // meters: no holes this close to a boss (or its warning)
    const float ShoreLength = 1500f;            // the safe rock you arrive on (pixels past Bolt-E)
    const float RescueLength = 1300f;           // the safe rock after a crash, a rocket ride or a cave
    const float BubbleLead = 0.35f;             // a bubble in front of Bolt-E is back in the lava this long before he gets there
    const float EruptEvery = 15f;               // seconds between the volcano's eruptions (far away: just to look at)
    const float EruptSeconds = 2.6f;            // how long an eruption lasts
    const float QuietRoadSign = 1e8f;           // (the self-test's "quiet road": nothing new comes)
    const string VolcanoTip = "Hop from rock to rock: they SINK!";
    const string HoleTip = "A HOLE! Ride in for a LAVA CAVE, or jump over!";

    /// <summary>How long a rock takes from Bolt-E landing on it to being under the lava (2.9 seconds).</summary>
    public static float SinkSeconds => SinkDelay + SinkDepth / SinkSpeed;

    static readonly Color LavaOrange = new(1f, 0.55f, 0.15f);
    static readonly Color HotYellow = new(1f, 0.85f, 0.35f);

    readonly List<LavaPlatform> lavaRocks = new();     // the rocks floating on the lava
    readonly List<LavaBubble> lavaBubbles = new();     // the lava bubbles in the gaps
    readonly VolcanoPlanner rockPlanner = new();       // puts the rocks in fair patterns (see VolcanoThings.cs)
    readonly List<PlannedRock> plannedRocks = new();   // the rocks of the pattern being made (reused every time)
    Random volcanoRandom = new(1);                     // the planner's own dice (so the other worlds' dice never change)
    float volcanoSeconds;      // seconds riding on VOLCANO ISLAND's surface (the first ones are easy)
    float nextHoleAt;          // when the next hole comes (in volcanoSeconds)
    bool holeTipShown;         // the tip about holes shows only once a game
    float lavaFallTimer;       // seconds left of the "HOT HOT HOT!" hop (then KABOOM)
    bool rescuedThisCrash;     // a safe rock was put under Bolt-E for this magic rebuild
    bool rescuedParachute;     // ...or under his parachute
    LavaPlatform? arena;       // the big solid rock floor for a boss fight (null = none)
    float eruptClock;          // seconds since the last eruption
    float eruptTime = -1;      // seconds into the eruption now (-1 = not erupting)
    float sinkSplashTimer;     // seconds until the next little lava splash at a sinking rock
    float lastBossDistance;    // (the boss counter waits while Bolt-E is in a cave: see PauseBossCounter)
    CanvasLayer curtainLayer = null!;
    CaveCurtain curtain = null!;

    // (The self-test reads and sets these)
    bool testRideOverHoles;    // true: holes don't drop Bolt-E in, so a test can watch them come by
    int bubblesLaunched;       // lava bubbles that popped up this game...
    int bubblesUnderBoltE;     // ...and how many of those popped up right under Bolt-E (it should never happen!)
    readonly List<(float Seconds, int Hardness)> holeLog = new(); // every hole planned this game, and when

    /// <summary>True on VOLCANO ISLAND (also while its colors are still coming in), on the surface or in a cave.</summary>
    bool InVolcano => toWorld.Scenery == Scenery.Volcano;

    /// <summary>True on VOLCANO ISLAND's surface: up on the rocks, not in a cave.</summary>
    bool OnVolcanoSurface => InVolcano && !InCave;

    /// <summary>True while something special happens that must not be interrupted (dropping in, blasting out, HOT HOT HOT!).</summary>
    bool VolcanoBusy => CaveBusy || lavaFallTimer > 0;

    /// <summary>Turns a spot on the screen into a "track" position (see ScreenX).</summary>
    float TrackOf(float screenX) => screenX + distance - RobotX;

    /// <summary>Makes the curtain for dropping into caves (called once, when the game starts).</summary>
    void SetUpVolcano()
    {
        curtainLayer = new CanvasLayer { Layer = 45 }; // over the game and its words, under the pause screen
        curtain = new CaveCurtain();
        curtainLayer.AddChild(curtain);
        AddChild(curtainLayer);
    }

    // ---------- Arriving and leaving ----------

    /// <summary>A new world. Into VOLCANO ISLAND: a safe rock to start on. Out of it: the rocks puff away.</summary>
    void VolcanoWorldChanged()
    {
        if (InVolcano && fromWorld.Scenery != Scenery.Volcano) ArriveInVolcano();
        else if (!InVolcano && fromWorld.Scenery == Scenery.Volcano) LeaveVolcanoNow();
    }

    /// <summary>
    /// Welcome to VOLCANO ISLAND! Bolt-E starts on a long safe rock, the first rocks are easy, and the tip says what to do.
    /// The volcano says hello with an eruption soon.
    /// </summary>
    void ArriveInVolcano()
    {
        volcanoSeconds = 0;
        nextHoleAt = FirstHoleAt;
        eruptClock = EruptEvery - 2.5f;
        CancelLavaFall();
        volcanoRandom = new Random(unchecked(runNumber * 6007 + (int)distance)); // (the same every time, for the self-test)
        PuffAway(lavaRocks, _ => true);
        PuffAway(lavaBubbles, _ => true);
        arena = null;
        AddRock(-300, RobotX + ShoreLength, steady: true);
        rockPlanner.Reset(TrackOf(RobotX + ShoreLength));
        hud.ShowHint(VolcanoTip);
    }

    /// <summary>Out of VOLCANO ISLAND (a new world, or WORLD WARP): the rocks puff away, and the ground is normal again.</summary>
    void LeaveVolcanoNow()
    {
        PuffAway(lavaRocks, _ => true);
        PuffAway(lavaBubbles, _ => true);
        arena = null;
        CancelLavaFall();
        robot.GroundY = GroundY;
        if (robot.OnGround && !robot.Crashed) robot.Position = new Vector2(robot.Position.X, GroundY);
    }

    /// <summary>A fresh start (a new game or the title screen): no rocks, no cave, normal ground, no curtain.</summary>
    void ClearVolcano()
    {
        foreach (var rock in lavaRocks) rock.QueueFree();
        foreach (var bubble in lavaBubbles) bubble.QueueFree();
        lavaRocks.Clear();
        lavaBubbles.Clear();
        ClearCaveThings();
        caveHardness = 0;
        caveStep = CaveStep.None;
        caveStepTime = 0;
        cavesCleared = 0;
        arena = null;
        CancelLavaFall();
        volcanoSeconds = 0;
        nextHoleAt = FirstHoleAt;
        holeTipShown = false;
        rescuedThisCrash = rescuedParachute = false;
        bubblesLaunched = bubblesUnderBoltE = 0;
        holeLog.Clear();
        eruptClock = 0;
        eruptTime = -1;
        robot.GroundY = GroundY;
        curtain.Reset();
    }

    // ---------- Every frame ----------

    /// <summary>
    /// Every frame, on every screen: the embers fall, the volcano erupts, the cave curtain closes and opens, and on
    /// VOLCANO ISLAND the rocks sink, Bolt-E rides up and down with them, and the lava bubbles pop.
    /// </summary>
    void UpdateVolcano(float dt)
    {
        UpdateEmbers(dt);
        UpdateEruption(dt);
        PauseBossCounter();
        UpdateCaveSteps(dt);
        UpdateLavaFall(dt); // (even if the world just changed: a hop in the lava always ends)
        if (!InVolcano) return;
        if (InCave)
        {
            UpdateCave(dt);
            return;
        }
        UpdateRocks(dt);
        UpdateBubbles();
        KeepSafeRocks();
        UpdateArena();
        FollowTheRocks(dt);
        if (state == GameState.Playing && !CaveBusy && !BossFightActive) volcanoSeconds += dt;
    }

    /// <summary>The rock under a spot (with a little forgiveness at the edges), or null if it's over the lava.</summary>
    LavaPlatform? RockUnder(float x)
    {
        foreach (var rock in lavaRocks)
            if (x >= rock.Left - LavaEdgeForgiveness && x <= rock.Right + LavaEdgeForgiveness) return rock;
        return null;
    }

    /// <summary>
    /// Bolt-E's feet go up and down with the rock he's on (it sinks!), and he lands on the rock under him. Landing on a
    /// rock makes it start to sink (unless it's a steady one).
    /// </summary>
    void FollowTheRocks(float dt)
    {
        if (robot.Crashed)
        {
            robot.GroundY = GroundY; // (he's rebuilt standing at rock height)
            return;
        }
        if (robot.HotFeet || robot.FallingIntoSpace || CaveBusy) return;
        var rock = RockUnder(robot.Position.X);
        if (rock is null)
        {
            robot.GroundY = GroundY;
            return;
        }
        if (robot.OnGround && state == GameState.Playing && !rock.Steady) rock.Touched = true;
        robot.RideOn(rock.Top, dt);
    }

    /// <summary>
    /// The rocks Bolt-E has touched sink: first they wait SinkDelay, then they go down SinkSpeed pixels every second
    /// (faster under heavy MEGA BOLT-E), with a rumble and little lava splashes.
    /// </summary>
    void UpdateRocks(float dt)
    {
        sinkSplashTimer -= dt;
        bool splash = sinkSplashTimer <= 0;
        if (splash) sinkSplashTimer = 0.12f;
        var under = robot.OnGround ? RockUnder(robot.Position.X) : null;
        foreach (var rock in lavaRocks)
        {
            if (!rock.Touched || rock.Steady) continue;
            rock.SinkClock += dt;
            if (rock.SinkClock < SinkDelay || rock.Sunk >= SinkDepth + 30) continue;
            if (rock.Sunk == 0) sounds.Play(Sfx.RockSink, Rand(0.9f, 1.1f));
            bool heavy = rock == under && robot.Size > 1.05f;
            rock.Sunk = Mathf.Min(SinkDepth + 30, rock.Sunk + SinkSpeed * (heavy ? MegaSinkTimes : 1) * dt);
            rock.Position = new Vector2(rock.Position.X, GroundY + rock.Sunk);
            if (splash && rock.Right > 0 && rock.Left < ScreenWidth)
            {
                float side = GD.Randf() < 0.5f ? rock.Left : rock.Right;
                LavaSplash(new Vector2(Mathf.Clamp(side, 10, ScreenWidth - 10), LavaSurfaceY), 3);
            }
        }
    }

    /// <summary>
    /// The lava bubbles: each one starts its glowing warning at the right moment, so its blob is back in the lava before
    /// Bolt-E gets to its gap (or, when the world is too fast to see it in front of him, it pops up just behind him).
    /// </summary>
    void UpdateBubbles()
    {
        float worldSpeed = speed * boost;
        const float halfBody = StationPlanner.HalfBody;
        foreach (var bubble in lavaBubbles)
        {
            if (bubble.Now == LavaBubble.Step.Waiting && state == GameState.Playing)
            {
                bool go = bubble.Behind
                    ? bubble.GapRight <= RobotX - halfBody - LavaBubble.Radius - bubble.DriftReach + worldSpeed * LavaBubble.WarnSeconds
                    : bubble.GapLeft - (RobotX + halfBody) <= worldSpeed * (LavaBubble.WarnSeconds + LavaBubble.FlightSeconds + BubbleLead);
                if (go)
                {
                    bubble.StartWarning();
                    sounds.Play(Sfx.LavaBubble, Rand(0.9f, 1.1f));
                }
            }
            if (bubble.JustLaunched)
            {
                bubble.JustLaunched = false;
                bubblesLaunched++;
                if (robot.Position.X + halfBody > bubble.GapLeft && robot.Position.X - halfBody < bubble.GapRight) bubblesUnderBoltE++;
                sounds.Play(Sfx.LavaPlop, Rand(0.9f, 1.15f));
                LavaSplash(bubble.Position, 8);
            }
        }
    }

    /// <summary>A little splash of lava drops (and their orange glow) at a spot on the lava.</summary>
    void LavaSplash(Vector2 at, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float life = Rand(0.25f, 0.55f);
            particles.Add(new Particle
            {
                Position = at + new Vector2(Rand(-10, 10), 0),
                Velocity = new Vector2(Rand(-90, 90), Rand(-320, -140)),
                Life = life,
                MaxLife = life,
                Size = Rand(2.5f, 5f),
                Color = i % 3 == 0 ? HotYellow : LavaOrange,
            });
        }
    }

    /// <summary>
    /// Safe rocks, so Bolt-E is never left over the lava: one under him when his pieces fly back together after a crash,
    /// and one under his parachute after the rocket board.
    /// </summary>
    void KeepSafeRocks()
    {
        if (state == GameState.Rebuilding && rebuildStarted && !rescuedThisCrash)
        {
            rescuedThisCrash = true;
            PlaceSafeRock();
        }
        if (state != GameState.Rebuilding) rescuedThisCrash = false;

        if (robot.Parachuting && !rescuedParachute)
        {
            rescuedParachute = true;
            PlaceSafeRock();
        }
        else if (robot.Parachuting && arena is null)
        {
            // The parachute floats down slowly: the safe rock under it grows longer, so it's still there when he lands
            var under = RockUnder(robot.Position.X);
            if (under is { Steady: true, Hole: 0 } && under.Right < robot.Position.X + 600) MakeRockLonger(under, robot.Position.X + RescueLength);
            else if (under is null || !under.Steady) PlaceSafeRock();
        }
        if (!robot.Parachuting && !robot.Rocketing) rescuedParachute = false;
    }

    /// <summary>A safe rock gets longer, out to "right" (anything in the way puffs away), and the next rocks come after it.</summary>
    void MakeRockLonger(LavaPlatform rock, float right)
    {
        float left = rock.Left;
        PuffAway(lavaRocks, other => other != rock && other.Right > left && other.Left < right + VolcanoPlanner.GapMin);
        PuffAway(lavaBubbles, bubble => bubble.GapLeft < right + 200);
        rock.Width = right - left;
        rock.Position = new Vector2((left + right) / 2, rock.Position.Y);
        rockPlanner.Reset(TrackOf(right));
    }

    /// <summary>
    /// A fresh, wide, steady rock right under Bolt-E (anything in its way puffs away), and the next rocks come after it.
    /// (In a boss fight the floor is already solid rock, so nothing changes.)
    /// </summary>
    void PlaceSafeRock()
    {
        if (arena is not null) return;
        float left = -300, right = RobotX + RescueLength;
        PuffAway(lavaRocks, rock => rock.Right > left - VolcanoPlanner.GapMin && rock.Left < right + VolcanoPlanner.GapMin);
        PuffAway(lavaBubbles, bubble => bubble.GapLeft < right + 200);
        AddRock(left, right, steady: true);
        rockPlanner.Reset(TrackOf(right));
    }

    /// <summary>
    /// A boss is coming: the rocks join together into one big SOLID ROCK floor ("SOLID GROUND!"), and the bubbles stop.
    /// After the fight (if no new world comes), the floor ends a little way ahead and the rocks come back.
    /// </summary>
    void UpdateArena()
    {
        if (arena is null && (bossWarningShown || boss is not null) && !CaveBusy && state is GameState.Playing or GameState.Rebuilding)
        {
            PuffAway(lavaRocks, _ => true);
            PuffAway(lavaBubbles, _ => true);
            arena = AddRock(-400, ScreenWidth + 9000, steady: true);
            arena.Arena = true;
            popups.Add("SOLID GROUND!", new Vector2(ScreenWidth / 2, 330), HotYellow, 46);
            sounds.Play(Sfx.RockSink, 0.7f);
            Rumble(0.3f, 0.4f, 0.3f);
            shake = Mathf.Max(shake, 0.15f);
        }
        else if (arena is not null && boss is null && !bossWarningShown && !newWorldComing && state == GameState.Playing)
        {
            // The fight is over and the world stays: the solid floor ends a little way ahead, then the rocks come back
            float right = Mathf.Max(robot.Position.X, RobotX) + 900;
            float left = arena.Left;
            arena.Width = right - left;
            arena.Position = new Vector2((left + right) / 2, arena.Position.Y);
            arena.Arena = false;
            rockPlanner.Reset(TrackOf(right));
            arena = null;
        }
    }

    // ---------- Lava: HOT HOT HOT! ----------

    /// <summary>
    /// Is Bolt-E riding over the lava (a gap, a rock that sank, or a pit in a cave's floor)? Then: HOT HOT HOT! A rock
    /// with a hole: riding into the hole drops him into a LAVA CAVE. MEGA BOLT-E is too big to fall in, nothing happens on
    /// the rocket board or the parachute, and while he blinks the lava can't hurt him. Returns true if something happened.
    /// </summary>
    bool CheckRocksUnderfoot()
    {
        if (state != GameState.Playing || CaveBusy || lavaFallTimer > 0) return false;
        if (!robot.OnGround || robot.Crashed || robot.FallingIntoSpace || robot.HotFeet) return false;
        bool canFall = robot.Size <= 1.05f && !RocketSafe;
        var rock = RockUnder(robot.Position.X);
        if (rock is { Hole: > 0 } && canFall && !testRideOverHoles && Mathf.Abs(robot.Position.X - rock.Position.X) < HoleTrigger)
        {
            DropIntoHole(rock); // WHEEEE! (see Main.LavaCave.cs)
            return true;
        }
        if (!canFall || robot.BlinkTime > 0) return false;
        if (rock is null || rock.IsUnder)
        {
            StartLavaFall();
            return true;
        }
        return false;
    }

    /// <summary>OUCH! Into the lava: a "HOT HOT HOT!" hop and a sizzle... and then KABOOM (see UpdateLavaFall).</summary>
    void StartLavaFall()
    {
        lavaFallTimer = HotHopSeconds;
        robot.HotHop(LavaSurfaceY + 18);
        popups.Add("HOT HOT HOT!", AboveBoltE(190), LavaOrange, 40);
        sounds.Play(Sfx.Sizzle);
        Rumble(0.5f, 0.3f, 0.4f);
        LavaSplash(new Vector2(robot.Position.X, LavaSurfaceY), 14);
        Burst(robot.Position + new Vector2(0, -20), 8, Colors.White, 160f); // steam!
    }

    /// <summary>The hop in the lava: steam and sparks... then KABOOM. (A crash, or a rebuild, ends it early.)</summary>
    void UpdateLavaFall(float dt)
    {
        if (lavaFallTimer <= 0) return;
        if (state != GameState.Playing || robot.Crashed)
        {
            CancelLavaFall();
            return;
        }
        lavaFallTimer -= dt;
        if (GD.Randf() < 0.5f) LavaSplash(new Vector2(robot.Position.X + Rand(-20, 20), LavaSurfaceY), 1);
        if (lavaFallTimer > 0) return;

        // KABOOM! (The lava itself: nothing can stop it... except blinking. If he started blinking during the hop, like
        // when a power ran out, he gets lucky: a safe rock pops up under him.)
        if (Bonk(null)) return;
        CancelLavaFall();
        if (InCave) PuffAway(cavePits, pit => Mathf.Abs(pit.Position.X - robot.Position.X) < pit.Width / 2 + 60);
        else if (InVolcano) PlaceSafeRock();
    }

    /// <summary>No more lava hop: his feet cool down, and the ground under him is normal again.</summary>
    void CancelLavaFall()
    {
        lavaFallTimer = 0;
        if (!robot.HotFeet) return;
        robot.CoolFeet();
        robot.GroundY = GroundY;
    }

    /// <summary>Every frame, while Bolt-E is on a gap's lava, a rock or a cave's floor: lava bubbles, rock spikes, stalactites... (see CheckBumps)</summary>
    bool CheckVolcanoBumps(Rect2 robotBox)
    {
        if (!InVolcano) return false;
        foreach (var bubble in lavaBubbles)
            if (bubble.Dangerous && robotBox.Intersects(bubble.Hitbox) && Bonk(bubble)) return true;
        foreach (var rock in caveRocks)
            if (rock.Dangerous && robotBox.Intersects(rock.Hitbox) && Bonk(rock)) return true;
        return InCave ? CheckCaveFloor() : CheckRocksUnderfoot();
    }

    // ---------- Putting rocks, bubbles and bolts on the lava ----------

    /// <summary>
    /// The rocks come in plenty of time: whenever the last planned rock ends less than a screen ahead, the planner makes
    /// the next one (it's fair: see VolcanoPlanner), and here it goes on the lava.
    /// </summary>
    void SpawnVolcanoThings()
    {
        if (spawnCountdown > QuietRoadSign || CaveBusy || arena is not null) return;
        float planSpeed = Mathf.Max(StartSpeed, speed); // (after a crash it's still speeding back up: plan for at least the start speed)
        float ahead = TrackOf(ScreenWidth + 400);
        if (rockPlanner.LastEnd < TrackOf(ScreenWidth - 100)) rockPlanner.Reset(TrackOf(ScreenWidth + 100)); // (just in case)
        for (int guard = 0; rockPlanner.LastEnd < ahead && guard < 8; guard++)
        {
            bool holeDue = volcanoSeconds >= nextHoleAt && HolesAllowedAt(rockPlanner.LastEnd);
            rockPlanner.Plan(planSpeed, volcanoSeconds, holeDue, volcanoRandom, plannedRocks);
            if (holeDue)
            {
                nextHoleAt = volcanoSeconds + HoleEvery + (volcanoRandom.NextSingle() * 2 - 1) * HoleWobble;
                foreach (var planned in plannedRocks) holeLog.Add((volcanoSeconds, planned.Hole));
            }
            float gapStart = float.NaN;
            foreach (var planned in plannedRocks) gapStart = PutRockOnLava(planned, gapStart, planSpeed);
        }
    }

    /// <summary>No holes close to a boss (or its warning), so a boss never comes while Bolt-E is in a cave.</summary>
    bool HolesAllowedAt(float track) => nextBossAt - track / PixelsPerMeter > NoHolesNearBoss;

    /// <summary>One planned rock goes on the lava, with the bubble and the bolts in the gap before it. Returns where it ends.</summary>
    float PutRockOnLava(PlannedRock planned, float gapStart, float planSpeed)
    {
        float left = ScreenX(planned.Left), right = ScreenX(planned.Right);
        float gapLeft = float.IsNaN(gapStart) ? left - FindGapBefore(planned) : gapStart;
        var rock = AddRock(left, right, hole: planned.Hole);

        float gapWidth = left - gapLeft;
        if (planned.Bubble && gapWidth > 0)
        {
            var bubble = new LavaBubble
            {
                GapWidth = gapWidth,
                Position = new Vector2((gapLeft + left) / 2, LavaSurfaceY),
                Drift = volcanoRandom.Next(2) == 0 ? -1 : 1,
                // In front of Bolt-E, if there's time to see it there; otherwise just behind him
                Behind = RobotX + StationPlanner.HalfBody + planSpeed * (LavaBubble.WarnSeconds + LavaBubble.FlightSeconds + BubbleLead) > ScreenWidth - 20,
            };
            AddChild(bubble);
            lavaBubbles.Add(bubble);
        }
        else if (gapWidth > 0 && volcanoRandom.NextSingle() < 0.55f)
        {
            // An arc of bolts over the gap: jump, and grab them all!
            float middle = (gapLeft + left) / 2;
            for (int i = -2; i <= 2; i++)
            {
                float u = i / 2f;
                AddBolt(middle + i * (gapWidth / 4 + 30), GroundY - 45 - 150 * (1 - u * u));
            }
        }

        if (planned.Hole > 0)
        {
            if (!holeTipShown)
            {
                holeTipShown = true;
                hud.ShowHint(HoleTip);
            }
        }
        else if (planned.Width >= VolcanoPlanner.JumpReach(planSpeed) + 80 && TimeForStationBox())
        {
            // A rainbow ? box over a wide rock: jump from the rock for it, and you land on the same rock again
            AddBox(left + VolcanoPlanner.JumpReach(planSpeed) / 2 + 40);
            nextBoxAt = Meters + Rand(PowerUps.BoxEveryMin, PowerUps.BoxEveryMax);
        }
        else if (planned.Width >= 500 && volcanoRandom.NextSingle() < 0.3f)
        {
            for (int i = 0; i < 4; i++) AddBolt(left + 120 + i * 55, GroundY - 40); // a line of bolts along the rock
        }
        return rock.Right;
    }

    /// <summary>Where the gap before a planned rock starts (the end of the rock before it), on the screen.</summary>
    float FindGapBefore(PlannedRock planned)
    {
        float left = ScreenX(planned.Left);
        float best = float.MaxValue;
        foreach (var rock in lavaRocks)
            if (rock.Right <= left + 1) best = Mathf.Min(best, left - rock.Right);
        return best == float.MaxValue ? 0 : best;
    }

    /// <summary>Puts a rock on the lava, from "left" to "right" on the screen. (Under everything else on the lava.)</summary>
    LavaPlatform AddRock(float left, float right, int hole = 0, bool steady = false)
    {
        var rock = new LavaPlatform { Width = right - left, Hole = hole, Steady = steady || hole > 0, Position = new Vector2((left + right) / 2, GroundY) };
        AddChild(rock);
        MoveChild(rock, 0); // (drawn first: bolts, bubbles and Bolt-E go in front of it)
        lavaRocks.Add(rock);
        return rock;
    }

    /// <summary>The rocks, the bubbles and the cave's things slide along with the world.</summary>
    void MoveVolcanoThings(float move)
    {
        MoveAlong(lavaRocks, move, rock => rock.Right < -200);
        MoveAlong(lavaBubbles, move, bubble => bubble.GapRight < -200);
        MoveAlong(cavePits, move, pit => pit.Right < -200);
        MoveAlong(caveRocks, move, rock => rock.Position.X < -200);
        if (geyser is not null) geyser.Position -= new Vector2(move, 0);
    }

    /// <summary>Slides every thing in a list to the left, and takes away the ones that are "gone".</summary>
    static void MoveAlong<T>(List<T> things, float move, Func<T, bool> gone) where T : Node2D
    {
        for (int i = things.Count - 1; i >= 0; i--)
        {
            things[i].Position -= new Vector2(move, 0);
            if (!gone(things[i])) continue;
            things[i].QueueFree();
            things.RemoveAt(i);
        }
    }

    // ---------- The volcano erupts (far away, just to look at) ----------

    /// <summary>Every EruptEvery seconds on the surface, the volcano ERUPTS: a fountain of lava blobs and a deep rumble.</summary>
    void UpdateEruption(float dt)
    {
        if (eruptTime >= 0)
        {
            eruptTime += dt;
            if (eruptTime > EruptSeconds) eruptTime = -1;
        }
        if (!OnVolcanoSurface || state != GameState.Playing || CaveBusy) return;
        eruptClock += dt;
        if (eruptClock < EruptEvery) return;
        eruptClock = 0;
        eruptTime = 0;
        sounds.Play(Sfx.Eruption);
        shake = Mathf.Max(shake, 0.12f);
        Rumble(0.15f, 0.35f, 0.6f);
    }

    /// <summary>The boss counter waits while Bolt-E is in a cave (and dropping in or blasting out), so bosses come on the surface.</summary>
    void PauseBossCounter()
    {
        if ((InCave || CaveBusy) && state is GameState.Playing or GameState.Rebuilding)
            nextBossAt += (distance - lastBossDistance) / PixelsPerMeter;
        lastBossDistance = distance;
    }
}
