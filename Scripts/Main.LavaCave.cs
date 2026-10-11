using Godot;

/// <summary>
/// THE LAVA CAVES under VOLCANO ISLAND! (This is more of Main: "partial" means one class spread over several files.)
///
/// Some rocks on VOLCANO ISLAND have a HOLE. Ride into one and WHEEEE! Bolt-E drops down into a LAVA CAVE: a level of
/// its own, with a solid floor (no sinking rocks down here), glowing crystals, lavafalls, and cave dangers: rock spikes,
/// lava pits, stalactites that fall from the ceiling (they shake first!) and, in the harder caves, fire bats.
/// Each hole has a HARDNESS (1, 2 or 3 flames): harder caves go faster, have more dangers, and their CRYSTAL BOLTS are
/// worth more (x1, x2, x3). At the end there's a GEYSER: ride onto it and WHOOSH! "CAVE CLEAR!", a bonus, and Bolt-E
/// blasts back up to the surface, onto a safe rock.
/// In a cave the boss counter waits (bosses only come on the surface), and WORLD WARP has to wait too.
/// CavePlanner (in VolcanoThings.cs) puts the dangers in fair patterns, and VolcanoScenery.cs draws the caves.
/// </summary>
public partial class Main
{
    // ---- The LAVA CAVES (try changing these!) ----
    const float CaveLength = 25f;             // about how many seconds a cave lasts, until the geyser
    const float IrisSeconds = 0.45f;          // how long the screen takes to close (or open), like an eye
    const float IrisHold = 0.12f;             // how long it stays black in the middle
    const float CaveDropY = 250f;             // Bolt-E drops into the cave from up here
    const float GeyserLaunch = 2200f;         // how hard the geyser blasts him up (pixels per second)
    const float GeyserBlinkSeconds = 2.2f;    // how long he blinks (safe) when he's back on the surface
    const float StalactiteLead = 0.45f;       // a stalactite has landed this long before Bolt-E gets to it
    public const float CaveCeilingY = 110f;   // the bottom of the cave's ceiling
    public const float CaveFloorY = GroundY;  // the cave's floor (solid rock!)
    static readonly int[] CaveClearBonus = { 50, 100, 250 };        // the CAVE CLEAR! bonus: EASY, MEDIUM, HARD
    static readonly float[] CaveMusicTimes = { 1f, 1.04f, 1.08f };  // the music plays a little faster in harder caves
    const string FindTheWayOut = "Find the way out first!";

    /// <summary>The steps of dropping into a cave and blasting back out (the screen closes and opens like an eye).</summary>
    enum CaveStep { None, DiveIn, OpenIn, BlastOut, OpenOut }

    CaveStep caveStep;         // what's happening now (None = just riding)
    float caveStepTime;        // seconds into that step
    int caveHardness;          // 0 = not in a cave, or 1 (EASY), 2 (MEDIUM), 3 (HARD)
    int droppingInto;          // the hardness of the hole Bolt-E is dropping into
    float caveSeconds;         // seconds in this cave
    int cavesCleared;          // caves cleared this game (the self-test reads this)
    Geyser? geyser;            // the way out (null until the end of the cave comes)
    readonly List<CavePit> cavePits = new();
    readonly List<CaveRock> caveRocks = new();
    readonly List<(float Track, float Phase)> pendingBats = new(); // fire bats waiting to fly in (where, and their swoop)
    readonly CavePlanner cavePlanner = new();
    readonly List<CaveDanger> plannedDangers = new();
    Random caveRandom = new(1);

    /// <summary>True in a LAVA CAVE.</summary>
    bool InCave => caveHardness > 0;

    /// <summary>True while Bolt-E drops into a cave or blasts out of one (nothing can crash him then).</summary>
    bool CaveBusy => caveStep != CaveStep.None;

    /// <summary>How much faster the world goes right now: 1 on the surface, more in the harder caves.</summary>
    float CaveSpeedNow => InCave ? CavePlanner.SpeedTimes[caveHardness - 1] : 1f;

    // ---------- Dropping in ----------

    /// <summary>WHEEEE! Bolt-E rode into a hole: he tumbles down into it, and the screen closes like an eye around him.</summary>
    void DropIntoHole(LavaPlatform rock)
    {
        caveStep = CaveStep.DiveIn;
        caveStepTime = 0;
        droppingInto = rock.Hole;
        rock.ZIndex = robot.ZIndex + 1; // (the rock's front covers him as he goes down the hole)
        robot.FallIntoSpace();
        sounds.Play(Sfx.Wheee);
        popups.Add("WHEEEE!", AboveBoltE(170), Colors.White, 40);
        Rumble(0.3f, 0.2f, 0.3f);
    }

    /// <summary>
    /// Every frame: the screen closes (black in the middle: the world changes!) and opens again, when Bolt-E drops into a
    /// cave or blasts out of one.
    /// </summary>
    void UpdateCaveSteps(float dt)
    {
        if (caveStep == CaveStep.None) return;
        if (state is GameState.Title or GameState.GameOver)
        {
            caveStep = CaveStep.None; // (the game ended: never mind)
            curtain.Reset();
            return;
        }
        caveStepTime += dt;
        float t = caveStepTime / IrisSeconds;
        switch (caveStep)
        {
            case CaveStep.DiveIn:
                curtain.Center = new Vector2(RobotX, Mathf.Min(robot.Position.Y - 40, GroundY + 40));
                curtain.Closed = Mathf.Min(1, t);
                if (caveStepTime >= IrisSeconds + IrisHold)
                {
                    EnterCave(droppingInto);
                    caveStep = CaveStep.OpenIn;
                    caveStepTime = 0;
                }
                break;
            case CaveStep.OpenIn:
                curtain.Center = new Vector2(RobotX, Mathf.Clamp(robot.Position.Y - 60, 100, GroundY));
                curtain.Closed = 1 - Mathf.Min(1, t);
                if (t >= 1) FinishCaveStep(showCaveBanner: true);
                break;
            case CaveStep.BlastOut:
                curtain.Center = new Vector2(RobotX, Mathf.Clamp(robot.Position.Y - 60, 60, GroundY));
                curtain.Closed = Mathf.Min(1, t);
                if (caveStepTime >= IrisSeconds + IrisHold)
                {
                    ExitCave();
                    caveStep = CaveStep.OpenOut;
                    caveStepTime = 0;
                }
                break;
            case CaveStep.OpenOut:
                curtain.Center = new Vector2(RobotX, Mathf.Clamp(robot.Position.Y - 60, 60, GroundY));
                curtain.Closed = 1 - Mathf.Min(1, t);
                if (t >= 1) FinishCaveStep(showCaveBanner: false);
                break;
        }
    }

    /// <summary>The screen is open again. In a new cave: its banner, with the flame pictures, and a tip.</summary>
    void FinishCaveStep(bool showCaveBanner)
    {
        caveStep = CaveStep.None;
        curtain.Closed = 0;
        if (!showCaveBanner) return;
        var color = VolcanoArt.HardnessColor(caveHardness);
        hud.ShowBanner($"LAVA CAVE - {VolcanoArt.HardnessName(caveHardness)}!", color);
        hud.ShowHint($"Crystal bolts are worth x{caveHardness}! Find the GEYSER!");
        curtain.Flames = caveHardness;
        curtain.FlamesLeft = 3;
    }

    /// <summary>
    /// The screen is black: now Bolt-E is in the LAVA CAVE of this hardness! The surface's rocks are gone, he drops down
    /// onto the cave's solid floor, the world goes faster (in harder caves), and so does the music.
    /// </summary>
    void EnterCave(int hardness)
    {
        caveHardness = Math.Clamp(hardness, 1, 3);
        caveSeconds = 0;
        ClearList(lavaRocks);
        ClearList(lavaBubbles);
        ClearList(obstacles);
        ClearList(enemies);
        ClearList(boxes);
        ClearList(bolts);
        CancelLavaFall();
        robot.GroundY = CaveFloorY;
        robot.DropInFrom(CaveDropY);
        cavePlanner.Reset(TrackOf(ScreenWidth + 250)); // (a little safe floor first)
        caveRandom = new Random(unchecked(runNumber * 4099 + (int)distance + caveHardness)); // (the same every time, for the self-test)
        sounds.SetMusicSpeed(CurrentWorld.MusicSpeed * CaveMusicTimes[caveHardness - 1]);
        sounds.Play(Sfx.Land, 0.7f);
    }

    /// <summary>Takes away every thing in a list, right away (the screen is black, so no puffs are needed).</summary>
    static void ClearList<T>(List<T> things) where T : Node2D
    {
        foreach (var thing in things) thing.QueueFree();
        things.Clear();
    }

    /// <summary>The cave's things all go away (a new game, or blasting out of the cave).</summary>
    void ClearCaveThings()
    {
        ClearList(cavePits);
        ClearList(caveRocks);
        pendingBats.Clear();
        geyser?.QueueFree();
        geyser = null;
    }

    // ---------- Every frame in a cave ----------

    /// <summary>
    /// Every frame in a cave: the floor is solid, stalactites start shaking when Bolt-E comes close (so they've landed
    /// before he gets there), and fire bats fly in.
    /// </summary>
    void UpdateCave(float dt)
    {
        if (state == GameState.Playing && !CaveBusy) caveSeconds += dt;
        if (!robot.HotFeet && !robot.FallingIntoSpace) robot.GroundY = CaveFloorY;

        float worldSpeed = speed * boost;
        float warnAhead = worldSpeed * (CaveRock.ShakeSeconds + CaveRock.FallSeconds + StalactiteLead) + StationPlanner.HalfBody;
        foreach (var rock in caveRocks)
        {
            if (rock.Kind == CaveRockKind.Stalactite && rock.Now == CaveRock.Step.Hanging && rock.Position.X - RobotX <= warnAhead)
            {
                rock.StartShaking();
                sounds.Play(Sfx.StalactiteCrack, Rand(1.1f, 1.3f));
                shake = Mathf.Max(shake, 0.06f);
            }
            if (rock.JustLanded)
            {
                rock.JustLanded = false;
                sounds.Play(Sfx.StalactiteCrack, 0.7f);
                Burst(rock.Position, 12, new Color(0.7f, 0.6f, 0.55f), 220f);
                shake = Mathf.Max(shake, 0.12f);
            }
        }

        // Fire bats fly in from the right side when their turn comes
        for (int i = pendingBats.Count - 1; i >= 0; i--)
        {
            float x = ScreenX(pendingBats[i].Track);
            if (x > ScreenWidth + 120) continue;
            var bat = new Enemy { Kind = EnemyKind.FireBat, GroundY = CaveFloorY, BatPhase = pendingBats[i].Phase };
            bat.Position = new Vector2(x, Enemy.BatMiddleY + Enemy.BatSwoop * Mathf.Sin(bat.BatPhase));
            AddChild(bat);
            enemies.Add(bat);
            pendingBats.RemoveAt(i);
            sounds.Play(Sfx.BatFlap, Rand(0.9f, 1.2f));
            for (int k = 0; k < 3; k++) AddCrystalBolt(x + k * 50, CaveFloorY - 300); // (way up high: stomp the bat and BOUNCE up to them!)
        }
    }

    /// <summary>
    /// Is Bolt-E riding onto a lava pit? HOT HOT HOT! (Not MEGA, not on the rocket or the parachute, not while blinking.)
    /// And reaching the GEYSER: WHOOSH! (Even in the middle of a jump.) Returns true if something happened.
    /// </summary>
    bool CheckCaveFloor()
    {
        if (geyser is { Fired: false } && robot.Position.X >= geyser.Left + 20 && state == GameState.Playing &&
            !CaveBusy && !robot.Crashed && !robot.HotFeet)
        {
            BlastOff();
            return true;
        }
        if (state != GameState.Playing || CaveBusy || lavaFallTimer > 0) return false;
        if (!robot.OnGround || robot.Crashed || robot.HotFeet || robot.FallingIntoSpace) return false;
        if (robot.Size > 1.05f || RocketSafe || robot.BlinkTime > 0) return false;
        foreach (var pit in cavePits)
        {
            if (Mathf.Abs(robot.Position.X - pit.Position.X) < pit.Width / 2 - LavaEdgeForgiveness)
            {
                StartLavaFall();
                return true;
            }
        }
        return false;
    }

    // ---------- Putting the cave's dangers on the track ----------

    /// <summary>
    /// The cave's dangers come in plenty of time (the planner makes them fair: see CavePlanner). When the cave has lasted
    /// about CaveLength seconds, the GEYSER comes instead.
    /// </summary>
    void SpawnCaveThings()
    {
        if (CaveBusy || !InCave || geyser is not null || spawnCountdown > QuietRoadSign) return;
        float planSpeed = Mathf.Max(StartSpeed, speed) * CaveSpeedNow;
        float ahead = TrackOf(ScreenWidth + 400);
        for (int guard = 0; cavePlanner.LastEnd < ahead && guard < 6; guard++)
        {
            // When would Bolt-E get to the next danger? Too close to the end of the cave: here comes the geyser instead!
            float from = Mathf.Max(cavePlanner.LastEnd, TrackOf(ScreenWidth + 100));
            if (caveSeconds + (from - distance) / planSpeed >= CaveLength - 1)
            {
                PlaceGeyser(from + planSpeed * 0.8f + 120);
                return;
            }
            cavePlanner.Plan(planSpeed, caveHardness, caveRandom, plannedDangers);
            foreach (var danger in plannedDangers) AddCaveDanger(danger);
        }
    }

    /// <summary>Puts one planned danger in the cave, with crystal bolts over it (sometimes).</summary>
    void AddCaveDanger(CaveDanger danger)
    {
        float x = ScreenX(danger.Middle);
        switch (danger.Kind)
        {
            case CaveDangerKind.Pit:
                var pit = new CavePit { Width = danger.Right - danger.Left, Position = new Vector2(x, CaveFloorY) };
                AddChild(pit);
                cavePits.Add(pit);
                break;
            case CaveDangerKind.Bat:
                pendingBats.Add((danger.Left, caveRandom.NextSingle() * Mathf.Tau)); // (it flies in when its turn comes)
                return;
            default:
                var kind = danger.Kind == CaveDangerKind.Stalactite ? CaveRockKind.Stalactite : CaveRockKind.Spikes;
                var rock = new CaveRock { Kind = kind, Position = new Vector2(x, CaveFloorY) };
                AddChild(rock);
                caveRocks.Add(rock);
                break;
        }

        // An arc of crystal bolts over it (jump, and grab them all!), or a line of them on the floor after it
        if (caveRandom.NextSingle() < 0.6f)
        {
            for (int i = -2; i <= 2; i++)
            {
                float u = i / 2f;
                AddCrystalBolt(x + i * 55, CaveFloorY - 50 - 150 * (1 - u * u));
            }
        }
        else
        {
            for (int i = 0; i < 3; i++) AddCrystalBolt(x + danger.Speed * 0.9f + i * 50, CaveFloorY - 40);
        }
    }

    /// <summary>A glowing CRYSTAL BOLT, worth as many bolts as the cave is hard (see BoltPickup).</summary>
    void AddCrystalBolt(float x, float y)
    {
        var bolt = new BoltPickup { Position = new Vector2(x, y), Crystal = true, Worth = caveHardness };
        AddChild(bolt);
        bolts.Add(bolt);
    }

    /// <summary>The way out: the GEYSER, at "track" (its left side).</summary>
    void PlaceGeyser(float track)
    {
        geyser = new Geyser { Position = new Vector2(ScreenX(track) + Geyser.Width / 2, CaveFloorY) };
        AddChild(geyser);
        MoveChild(geyser, 0); // (Bolt-E rides in front of it)
    }

    // ---------- Blasting out ----------

    /// <summary>
    /// Bolt-E reached the GEYSER: CAVE CLEAR! A bonus (bigger for harder caves), a fanfare, and WHOOSH! the geyser
    /// blasts him up, out of the cave, while the screen closes like an eye.
    /// </summary>
    void BlastOff()
    {
        int bonus = CaveClearBonus[caveHardness - 1];
        bonusPoints += bonus;
        cavesCleared++;
        if (power == PowerUp.Rocket) EndPowerUp(quiet: true); // (off the rocket board: the geyser does the flying now!)
        geyser!.Fire();
        hud.ShowBanner($"CAVE CLEAR! +{bonus}", new Color(1f, 0.85f, 0.2f));
        popups.Add($"+{bonus}", AboveBoltE(200), new Color(1f, 0.85f, 0.2f), 46);
        sounds.Play(Sfx.CaveClear);
        sounds.Play(Sfx.GeyserWhoosh);
        robot.LaunchUp(GeyserLaunch);
        shake = Mathf.Max(shake, 0.3f);
        Rumble(0.6f, 0.6f, 0.5f);
        LavaSplash(new Vector2(robot.Position.X, CaveFloorY), 20);
        caveStep = CaveStep.BlastOut;
        caveStepTime = 0;
    }

    /// <summary>
    /// The screen is black: back on VOLCANO ISLAND's surface! The cave is gone, the world goes at its normal speed again,
    /// and Bolt-E drops down from the sky onto a fresh, wide, safe rock, blinking (safe) for a moment.
    /// </summary>
    void ExitCave()
    {
        caveHardness = 0;
        ClearCaveThings();
        ClearList(obstacles);
        ClearList(enemies);
        ClearList(boxes);
        ClearList(bolts);
        CancelLavaFall();
        sounds.SetMusicSpeed(CurrentWorld.MusicSpeed);
        PlaceSafeRock();
        robot.GroundY = GroundY;
        robot.DropInFrom(-90);
        robot.BlinkTime = GeyserBlinkSeconds;
        nextHoleAt = volcanoSeconds + HoleEvery + (volcanoRandom.NextSingle() * 2 - 1) * HoleWobble;
    }
}
