using Godot;

/// <summary>
/// Grown-up testing code for DRONEZILLA, the third boss (more of the self-test: see Main.SelfTest.cs).
/// To run only these checks (and the taking-turns rule), add --selftest-drone after --selftest.
/// </summary>
public partial class Main
{
    /// <summary>The tests that --selftest-drone runs.</summary>
    static readonly string[] DroneTestNames =
    {
        nameof(BossesTakeTurns), nameof(ThreeBossesTakeTurns), nameof(DroneFliesHighAndHarmless), nameof(DroneBombs),
        nameof(DroneSwoop), nameof(DroneLandsAndStomp), nameof(DroneZapAndMinis), nameof(DroneTauntOnRebuild),
        nameof(BeatDronezilla), nameof(DroneCantBeDriven), nameof(DroneDrawsEverywhere),
    };

    /// <summary>Which kind of boss this is (Big Rusty, VROOMBA, DRONEZILLA or OPTI-MAX).</summary>
    static BossKind KindOf(BossBase? someBoss) => someBoss switch
    {
        Vroomba => BossKind.Vroomba,
        Dronezilla => BossKind.Dronezilla,
        OptiMax => BossKind.OptiMax,
        _ => BossKind.BigRusty,
    };

    /// <summary>How high Bolt-E's feet get with a jump and a double jump on a normal world (about 365 pixels).</summary>
    static float DoubleJumpReach =>
        Robot.JumpSpeed * Robot.JumpSpeed / (2 * Robot.Gravity) + Mathf.Pow(0.85f * Robot.JumpSpeed, 2) / (2 * Robot.Gravity);

    /// <summary>
    /// A new game with nothing on the road, and DRONEZILLA flies in right away (in world number "world": 1 = Candy Land).
    /// Nothing in the fight can crash Bolt-E, unless a test lets it (robot.BlinkTime = 0).
    /// </summary>
    Dronezilla StartDronezillaFight(string name, int world = 0)
    {
        PlayAs(name);
        StartGame();
        QuietRoad();
        if (world > 0) ChangeWorld(world);
        robot.BlinkTime = 1e9f;
        SpawnBoss(BossKind.Dronezilla);
        return (Dronezilla)boss!;
    }

    /// <summary>Bolt-E jumps straight up from where DRONEZILLA landed, and comes down right on top of him. True if he got stomped.</summary>
    async Task<bool> StompDrone(Dronezilla drone)
    {
        int health = drone.Health;
        await WaitUntil(() => robot.OnGround, 2);
        robot.Position = new Vector2(drone.Position.X, GroundY);
        robot.Jump();
        return await WaitUntil(() => drone.Health < health, 2);
    }

    /// <summary>
    /// The bosses take turns: BIG RUSTY, VROOMBA, DRONEZILLA, (now also OPTI-MAX,) BIG RUSTY... and every game starts one
    /// boss further along. Games 1 and 2 still start like before. Like a real game, three fights in a row bring three
    /// different bosses, each with his name on the health bar (and DRONEZILLA with his own tip).
    /// (Since OPTI-MAX came along there are four bosses: see FourBossesTakeTurns in Main.OptiMaxTests.cs.)
    /// </summary>
    async Task ThreeBossesTakeTurns()
    {
        await NextFrame();
        var order = new[] { BossKind.BigRusty, BossKind.Vroomba, BossKind.Dronezilla, BossKind.OptiMax };
        for (int game = 1; game <= 4; game++)
        {
            for (int fight = 1; fight <= 4; fight++)
            {
                var comes = BossForFight(game, fight);
                Check(comes == order[(game - 1 + fight - 1) % 4], $"game {game}, fight {fight}: {comes}");
            }
        }
        Check(BossForFight(1, 1) == BossKind.BigRusty && BossForFight(2, 1) == BossKind.Vroomba,
              "games 1 and 2 still start like before (BIG RUSTY, then VROOMBA)");

        testTakeTurns = true;
        try
        {
            PlayAs("TTurns3");
            StartGame();
            QuietRoad();
            robot.BlinkTime = 1e9f;
            var seen = new List<BossKind>();
            for (int fight = 1; fight <= 3; fight++)
            {
                SpawnBoss();
                var kind = KindOf(boss);
                seen.Add(kind);
                Check(kind == BossForFight(gameNumber, fight) && hud.BossBarName == boss!.DisplayName,
                      $"game {gameNumber}, fight {fight} follows the rule: {kind} ('{hud.BossBarName}' on his bar)");
                if (kind == BossKind.Dronezilla)
                    Check(hud.HintShowing && hud.HintText == DronezillaHint, $"DRONEZILLA's tip: '{hud.HintText}'");
                boss!.QueueFree();
                boss = null;
                await NextFrame();
            }
            Check(seen.Distinct().Count() == 3, $"three fights in a row bring three different bosses ({string.Join(", ", seen)})");
        }
        finally
        {
            testTakeTurns = false;
        }
    }

    /// <summary>
    /// DRONEZILLA flies in from the top-right (rotors buzzing) and cruises up high: his bump box is above the reach of a
    /// double jump. Up there, touching him never hurts and he can't be stomped. (On the Moon, floaty jumps reach him too:
    /// it still doesn't hurt.) His tip fits on the screen.
    /// </summary>
    async Task DroneFliesHighAndHarmless()
    {
        var drone = StartDronezillaFight("TDroneHigh");
        Check(drone.CurrentMove == "Entering" && drone.Position.X > ScreenWidth && drone.Position.Y < 0,
              $"he flies in from the top-right ({drone.Position.X:0}, {drone.Position.Y:0})");
        Check(hud.BossBarShowing && hud.BossBarName == Dronezilla.DronezillaName && drone.Health == BossBase.MaxHealth,
              $"his health bar says '{hud.BossBarName}' (6 health)");
        float tipWidth = ThemeDB.FallbackFont.GetStringSize(DronezillaHint, HorizontalAlignment.Left, -1, 28).X;
        Check(hud.HintText == DronezillaHint && tipWidth < ScreenWidth - 40, $"his tip: '{hud.HintText}' ({tipWidth:0} px wide)");
        await NextFrame();
        await NextFrame();
        Check(sounds.IsPlaying(Sfx.RotorBuzz) && sounds.TimesPlayed(Sfx.TakeOff) >= 1, "his rotors buzz (and whir as he flies in)");
        Check(!drone.IsDangerous && !drone.CanBeStomped, "flying in he's harmless");
        Check(await WaitUntil(() => drone.CurrentMove == "Hovering", 3), "he hovers");

        drone.SetProcess(false); // (he holds still for a moment)
        float bottom = GroundY - drone.Hitbox.End.Y;
        Check(bottom > DoubleJumpReach && bottom >= 330 && bottom <= 380,
              $"he cruises up high: his bump box starts {bottom:0} px up, and a double jump only reaches {DoubleJumpReach:0}");
        spareBatteries = 0;
        bestScore = 1_000_000; // (so it isn't a new high score)
        robot.BlinkTime = 0;
        bool touched = false;
        for (int frame = 0; frame < 20; frame++)
        {
            robot.Position = new Vector2(drone.Position.X, drone.Hitbox.End.Y + 60); // (as if a floaty Moon jump reached him)
            await NextFrame();
            touched |= robot.Hitbox.Intersects(drone.Hitbox);
        }
        Check(touched && state == GameState.Playing && !robot.Crashed, "touching him while he cruises doesn't crash Bolt-E");
        Check(!drone.CanBeStomped && !drone.IsDangerous, "...and he can't be stomped up there");
        robot.BlinkTime = 1e9f;
        drone.SetProcess(true);
        Check(await WaitUntil(() => drone.CurrentMove == "Bombing", 2) && !drone.IsDangerous && !drone.CanBeStomped,
              "dropping bombs, he's still harmless to touch (only the bombs hurt)");
    }

    /// <summary>
    /// BOMBS AWAY! He flies over Bolt-E and drops 3 bombs one after another (5 when FURIOUS), each with a whistle and a
    /// red target ring on the ground where it will land. A bomb landing on Bolt-E (no spare, not blinking) is a crash; one
    /// that lands away from him just goes BOOM.
    /// </summary>
    async Task DroneBombs()
    {
        var drone = StartDronezillaFight("TDroneBombs");
        int dropped = 0, booms = 0;
        drone.BombDropped += _ => dropped++;
        int whistles = sounds.TimesPlayed(Sfx.BombWhistle);
        Check(await WaitUntil(() => drone.CurrentMove == "Bombing", 4), "phase 1: his first attack is BOMBS AWAY!");
        Check(await WaitUntil(() => bombs.Count > 0, 3), "a bomb drops from his belly");
        var bomb = bombs[0];
        bomb.Boomed += _ => booms++;
        await NextFrame();
        Check(bomb.Falling && bomb.RingDrawn, "a blinking red target ring on the ground shows where it will land");
        Check(Mathf.Abs(bomb.Position.X - robot.Position.X) < 60 && sounds.TimesPlayed(Sfx.BombWhistle) == whistles + 1,
              $"aimed at Bolt-E (it falls at x {bomb.Position.X:0}, Bolt-E is at {robot.Position.X:0}), with a whistle");
        Check(await WaitUntil(() => drone.CurrentMove != "Bombing", 4) && dropped == Dronezilla.BombsWhenCalm,
              $"{dropped} bombs, one after another");
        Check(booms == 1 && state == GameState.Playing, "the first bomb went BOOM (Bolt-E is blinking, so it didn't hurt him)");
        Check(Mathf.IsEqualApprox(DroneBomb.BlastHalfWidth * 2, 90) && Mathf.IsEqualApprox(DroneBomb.BoomSeconds, 0.25f),
              "the blast is 90 px wide and lasts a quarter of a second");

        // A bomb landing on Bolt-E (no spare battery, not blinking): a crash
        GoToTitle();
        drone = StartDronezillaFight("TDroneBombHit");
        drone.SetProcess(false); // (he stays out of it)
        spareBatteries = 0;
        bestScore = 1_000_000;
        robot.BlinkTime = 0;
        AddBomb(new Vector2(robot.Position.X, GroundY - 380));
        Check(await WaitUntil(() => state == GameState.GameOver, 2), "a bomb landing on Bolt-E is a crash (Bonk!)");

        // Away from the ring: no crash
        GoToTitle();
        drone = StartDronezillaFight("TDroneBombMiss");
        drone.SetProcess(false);
        spareBatteries = 0;
        bestScore = 1_000_000;
        robot.BlinkTime = 0;
        AddBomb(new Vector2(robot.Position.X + 200, GroundY - 380));
        var miss = bombs[^1];
        bool boomed = false;
        miss.Boomed += _ => boomed = true;
        Check(await WaitUntil(() => !IsInstanceValid(miss) || miss.Finished, 3) && boomed, "the bomb lands and goes BOOM");
        Check(state == GameState.Playing && !robot.Crashed, "Bolt-E was out of the ring: no crash");

        // FURIOUS: 5 bombs
        GoToTitle();
        drone = StartDronezillaFight("TDroneBombs5");
        Check(await WaitUntil(() => drone.CurrentMove == "Hovering", 3), "DRONEZILLA flew in");
        drone.LoseHealth(4);
        int furiousDrops = 0;
        drone.BombDropped += _ => furiousDrops++;
        Check(await FastForwardUntil(() => drone.CurrentMove == "Bombing", 8), "FURIOUS: mini-drones, then BOMBS AWAY!");
        Check(await FastForwardUntil(() => drone.CurrentMove != "Bombing", 8) && furiousDrops == Dronezilla.BombsWhenFurious,
              $"FURIOUS: {furiousDrops} bombs");
    }

    /// <summary>
    /// SWOOP! A warning sound and a flashing "!", then he dives across the arena in a big U: low enough that his bump
    /// box is less than 130 px off the ground (jump over him!), and touching him then is a crash. Then he climbs back up
    /// at the far side. FURIOUS, he swoops there AND back.
    /// </summary>
    async Task DroneSwoop()
    {
        var drone = StartDronezillaFight("TDroneSwoop");
        Check(await WaitUntil(() => drone.CurrentMove == "Hovering", 3), "DRONEZILLA flew in");
        int alarms = sounds.TimesPlayed(Sfx.BossAlarm), whooshes = sounds.TimesPlayed(Sfx.Zoom);
        drone.DoAttack(DroneAttack.Swoop);
        await NextFrame();
        Check(drone.CurrentMove == "SwoopWarning" && sounds.TimesPlayed(Sfx.BossAlarm) == alarms + 1 && !drone.IsDangerous,
              "SWOOP! First a warning sound (he's still harmless)...");
        Check(await WaitUntil(() => drone.WarningDrawn, 0.5), "...and a flashing '!' over him");
        Check(await WaitUntil(() => drone.CurrentMove == "Swooping", 2) && sounds.TimesPlayed(Sfx.Zoom) == whooshes + 1, "then he dives: whoosh!");
        float startX = drone.Position.X, lowest = 1000;
        bool alwaysDangerous = true;
        for (int frame = 0; frame < 300 && drone.CurrentMove == "Swooping"; frame++)
        {
            lowest = Mathf.Min(lowest, GroundY - drone.Hitbox.End.Y);
            alwaysDangerous &= drone.IsDangerous;
            await NextFrame();
        }
        Check(lowest < 130 && lowest > 40, $"he swoops low: his bump box gets down to {lowest:0} px off the ground (jump over him!)");
        Check(alwaysDangerous, "during the whole swoop, touching him is a crash");
        Check(Mathf.Abs(drone.Position.X - startX) > 900, $"all the way across the arena ({startX:0} -> {drone.Position.X:0})");
        Check(GroundY - drone.Hitbox.End.Y > 300 && !drone.IsDangerous, "he climbs back up at the far side, harmless again");

        // Bolt-E standing in his way (no spare battery, not blinking) crashes
        GoToTitle();
        drone = StartDronezillaFight("TDroneSwoopHit");
        Check(await WaitUntil(() => drone.CurrentMove == "Hovering", 3), "a new fight");
        spareBatteries = 0;
        bestScore = 1_000_000;
        robot.BlinkTime = 0;
        robot.Position = new Vector2(640, GroundY);
        drone.DoAttack(DroneAttack.Swoop);
        Check(await WaitUntil(() => state == GameState.GameOver, 4), "Bolt-E standing in the middle crashes into him (Bonk!)");
        Check(drone.CurrentMove == "FlyingAway", $"out of batteries: DRONEZILLA zooms away ({drone.CurrentMove})");
        Check(await WaitUntil(() => boss is null, 4), "...up and off the top of the screen");

        // FURIOUS: there and back
        GoToTitle();
        drone = StartDronezillaFight("TDroneSwoop2");
        Check(await WaitUntil(() => drone.CurrentMove == "Hovering", 3), "a new fight");
        drone.LoseHealth(4);
        Check(await FastForwardUntil(() => drone.CurrentMove == "DroppingMinis", 6), "he's FURIOUS");
        int dives = 0;
        drone.Swooped += () => dives++;
        drone.DoAttack(DroneAttack.Swoop);
        Check(await FastForwardUntil(() => drone.CurrentMove == "LowBattery", 8) && dives == 2,
              $"FURIOUS: he swoops there AND back ({dives} dives), then his battery runs low");
    }

    /// <summary>
    /// After his attack his battery runs low ("LOW BATTERY!" and beeps). He picks a landing spot at least 260 px from
    /// Bolt-E (it shows on the ground), glides down and LANDS: rotors spin down, the buzz stops, and the top of his body
    /// is low enough to jump on. Touching him then never hurts. A stomp takes 1 health, and he wobbles back up into the
    /// air (Hurt). If he isn't stomped, his battery charges up and he takes off by himself.
    /// </summary>
    async Task DroneLandsAndStomp()
    {
        var drone = StartDronezillaFight("TDroneLand");
        int beeps = sounds.TimesPlayed(Sfx.LowBattery), whines = sounds.TimesPlayed(Sfx.PowerDown);
        Check(await FastForwardUntil(() => drone.CurrentMove == "LowBattery", 12), "after his attack his battery runs low");
        Check(popups.Texts.Contains("LOW BATTERY!") && sounds.TimesPlayed(Sfx.LowBattery) == beeps + 1 && drone.Battery < 0.3f,
              "'LOW BATTERY!' with beeps (and his battery icon blinks red)");
        float landX = drone.LandingSpotX, away = Mathf.Abs(landX - robot.Position.X);
        Check(away >= 260 && landX >= Dronezilla.LandLeft && landX <= Dronezilla.LandRight,
              $"he picks a landing spot {away:0} px from Bolt-E, inside the arena (x {landX:0})");
        await NextFrame();
        Check(drone.LandingSpotDrawn, "the landing spot shows on the ground first");
        Check(await WaitUntil(() => drone.CurrentMove == "Landed", 3) && drone.CanBeStomped, "he glides down and LANDS: stomp him now!");
        Check(Mathf.Abs(drone.Position.X - landX) < 1 && drone.Position.Y == GroundY, "on his skids, right on the spot");
        float top = GroundY - drone.Top;
        Check(top >= 85 && top <= 100, $"the top of his body is {top:0} px up: low enough to jump on");
        await NextFrame();
        Check(sounds.TimesPlayed(Sfx.PowerDown) == whines + 1 && !sounds.IsPlaying(Sfx.RotorBuzz), "a power-down whine, and the rotor buzz stops");
        float spin = drone.RotorSpeed;
        await Wait(0.4);
        Check(drone.RotorSpeed < spin - 5, $"his rotors spin down ({spin:0} -> {drone.RotorSpeed:0})");

        // Touching him while he's landed never hurts
        spareBatteries = 0;
        robot.BlinkTime = 0;
        robot.Position = new Vector2(drone.Position.X, GroundY);
        await Wait(0.3);
        Check(state == GameState.Playing && !robot.Crashed && drone.CanBeStomped && robot.Hitbox.Intersects(drone.Hitbox),
              "touching him while he's landed never hurts");
        robot.BlinkTime = 1e9f;
        spareBatteries = 1;

        // STOMP!
        int bonus = bonusPoints, clangs = sounds.TimesPlayed(Sfx.BossHit);
        Check(await StompDrone(drone), "Bolt-E comes down on top of him: STOMP!");
        Check(drone.Health == BossBase.MaxHealth - 1 && drone.CurrentMove == "Hurt" && !drone.CanBeStomped && !drone.IsDangerous,
              $"health {drone.Health}, and he's {drone.CurrentMove} (harmless)");
        Check(bonusPoints == bonus + PointsPerBossHit && sounds.TimesPlayed(Sfx.BossHit) == clangs + 1, $"+{bonusPoints - bonus} and a CLANG");
        await NextFrame();
        Check(sounds.IsPlaying(Sfx.RotorBuzz), "his rotors sputter back on");
        Check(await WaitUntil(() => drone.CurrentMove != "Hurt", 2) && GroundY - drone.Position.Y > Dronezilla.CruiseHeight - 5,
              $"he wobbles back up to cruising height ({drone.CurrentMove})");

        // Not stomped: he takes off by himself
        Check(await FastForwardUntil(() => drone.CurrentMove == "Landed", 12), "his next attack ends with a landing too");
        int takeoffs = sounds.TimesPlayed(Sfx.TakeOff);
        Check(await FastForwardUntil(() => drone.CurrentMove == "TakingOff", Dronezilla.LandedSeconds + 0.5) &&
              sounds.TimesPlayed(Sfx.TakeOff) == takeoffs + 1, "if he isn't stomped, his battery charges up and he takes off (whirr!)");
        Check(drone.Health == BossBase.MaxHealth - 1 && drone.Battery > 0.95f, "(fully charged, and no health lost)");
    }

    /// <summary>
    /// ANGRY DRONEZILLA drops 2 mini-drones that hover at head height and drift after Bolt-E: one can be stomped (a normal
    /// +25 stomp), and one bumping into Bolt-E is a crash. His ZAP BEAM: a red circle follows Bolt-E, then LOCKS (it stops
    /// and glows), then the beam fires down on it, and Bolt-E standing in it crashes.
    /// </summary>
    async Task DroneZapAndMinis()
    {
        var drone = StartDronezillaFight("TDroneZap");
        Check(await WaitUntil(() => drone.CurrentMove == "Hovering", 3), "DRONEZILLA flew in");
        drone.LoseHealth(2); // ANGRY (as if he was stomped twice)
        Check(await WaitUntil(() => drone.CurrentMove == "GettingAngry", 3) && hud.BannerText == $"{Dronezilla.DronezillaName} IS ANGRY!",
              $"he gets ANGRY: '{hud.BannerText}'");
        Check(await WaitUntil(() => drone.CurrentMove == "DroppingMinis", 3), "ANGRY: his first attack is MINI-DRONES");
        Check(await WaitUntil(() => drone.CurrentMove != "DroppingMinis", 3), "...then the rest of his attack");
        var minis = enemies.Where(enemy => enemy.Kind == EnemyKind.MiniDrone).ToList();
        Check(minis.Count == Dronezilla.MinisWhenAngry && minis.All(mini => mini.Chaser), $"{minis.Count} buzzing mini-drones dropped out");
        await Wait(1.2);
        Check(minis.All(mini => GroundY - mini.Position.Y >= 60 && GroundY - mini.Position.Y <= 90),
              $"they hover at head height ({string.Join(", ", minis.Select(mini => (GroundY - mini.Position.Y).ToString("0")))} px up)");
        var mini = minis[0];
        float gap = Mathf.Abs(mini.Position.X - robot.Position.X);
        await Wait(0.5);
        float closer = gap - Mathf.Abs(mini.Position.X - robot.Position.X), expected = MiniDroneChase * 0.5f;
        Check(closer > expected * 0.7f && closer < expected * 1.3f, $"and drift slowly after Bolt-E ({closer:0} px closer in 0.5 s)");

        // Bolt-E jumps, and comes down on a mini-drone
        minis[1].Position = new Vector2(robot.Position.X > 640 ? 100 : 1180, minis[1].Position.Y); // (the other one is far away)
        int stompsBefore = stomps, bonus = bonusPoints;
        await WaitUntil(() => robot.OnGround, 2);
        Check(robot.Jump() && await WaitUntil(() => robot.IsFalling, 1), "Bolt-E jumps");
        mini.Position = new Vector2(robot.Position.X, mini.Position.Y);
        Check(await WaitUntil(() => mini.Squashed, 1.5), "and lands on the mini-drone: squashed!");
        Check(stomps == stompsBefore + 1 && bonusPoints == bonus + PointsPerStomp, $"a normal stomp: +{bonusPoints - bonus}");

        // A mini-drone bumping into Bolt-E is a crash (and the rebuild clears them)
        PuffAway(enemies, _ => true);
        PuffAway(bombs, _ => true);
        await WaitUntil(() => robot.OnGround, 3);
        AddMiniDrone(new Vector2(robot.Position.X + 10, GroundY - Enemy.MiniDroneHover));
        robot.BlinkTime = 0;
        Check(await WaitUntil(() => state == GameState.Rebuilding, 1), "a mini-drone bumping into Bolt-E is a crash");
        Check(await WaitUntil(() => state == GameState.Playing, 4) && !enemies.Any(enemy => enemy.Kind == EnemyKind.MiniDrone),
              "Bolt-E is back, and the mini-drones puffed away");
        robot.BlinkTime = 1e9f;

        // ZAP BEAM!
        PuffAway(bombs, _ => true);
        int charges = sounds.TimesPlayed(Sfx.LaserCharge), zaps = sounds.TimesPlayed(Sfx.LaserBeam);
        drone.DoAttack(DroneAttack.Zap);
        Check(await WaitUntil(() => zapBeams.Count == 1, 0.5) && sounds.TimesPlayed(Sfx.LaserCharge) == charges + 1,
              "ZAP BEAM! A red circle under him (and a charging sound)");
        var beam = zapBeams[0];
        bool fired = false;
        beam.StartedFiring += () => fired = true;
        Check(beam.Tracking && !beam.Firing, "the circle follows Bolt-E...");
        robot.Position = new Vector2(drone.Position.X < 640 ? 1000 : 280, GroundY); // Bolt-E rides far away
        float from = Mathf.Abs(beam.Position.X - robot.Position.X);
        await Wait(0.6);
        float now = Mathf.Abs(beam.Position.X - robot.Position.X);
        Check(from - now > 120, $"...it follows him ({from:0} -> {now:0} px away)");
        Check(await WaitUntil(() => beam.IsLocked, 1.2), "then it LOCKS");
        float lockedX = beam.Position.X;
        robot.Position = new Vector2(Mathf.Clamp(lockedX + 250, Robot.LeftEdge, Robot.RightEdge), GroundY); // ride out!
        await Wait(0.15);
        Check(beam.Position.X == lockedX && beam.LockedDrawn && !beam.Firing, "it stops (Bolt-E rode away) and glows brighter");

        // Standing in the beam is a crash
        spareBatteries = 0;
        bestScore = 1_000_000;
        robot.BlinkTime = 0;
        robot.Position = new Vector2(lockedX, GroundY);
        Check(await WaitUntil(() => state == GameState.GameOver, 1.5) && fired, "ZAP! Bolt-E standing in the beam crashes");
        Check(sounds.TimesPlayed(Sfx.LaserBeam) == zaps + 1, "with the laser's BZZZT");
    }

    /// <summary>
    /// Bolt-E crashes (with a spare) while DRONEZILLA bombs him: a happy loop-the-loop with "BZZT BZZT!". The bomb that's
    /// already falling still falls, but no new ones come; the rebuild clears his bombs and mini-drones, and he waits a
    /// moment before attacking again. A crash while he's LANDED never steals the stomp chance: no BZZT BZZT.
    /// </summary>
    async Task DroneTauntOnRebuild()
    {
        var drone = StartDronezillaFight("TDroneTaunt");
        Check(await WaitUntil(() => drone.CurrentMove == "Bombing" && bombs.Count > 0, 6), "DRONEZILLA drops bombs");
        AddMiniDrone(robot.Position + new Vector2(150, -250));
        AddMiniDrone(robot.Position + new Vector2(-120, -250));
        int bzzts = sounds.TimesPlayed(Sfx.Bzzt), dropsAfterCrash = 0;
        drone.BombDropped += _ => dropsAfterCrash++;
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.Rebuilding, "Bolt-E crashes, with a spare battery");
        Check(drone.CurrentMove == "Taunting" && sounds.TimesPlayed(Sfx.Bzzt) == bzzts + 1 && popups.Texts.Contains("BZZT BZZT!"),
              $"BZZT BZZT! a happy loop-the-loop ({drone.CurrentMove})");
        Check(bombs.Count > 0 && bombs[0].Falling, "the bomb that was already falling keeps falling");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        Check(bombs.Count == 0 && !enemies.Any(enemy => enemy.Kind == EnemyKind.MiniDrone), "the rebuild cleared his bombs and mini-drones");
        robot.BlinkTime = 1e9f;
        await Wait(0.3);
        Check(drone.CurrentMove == "Hovering" && dropsAfterCrash == 0 && zapBeams.Count == 0,
              $"he still holds his attacks a moment after Bolt-E is back ({drone.CurrentMove}, {dropsAfterCrash} new bombs)");
        Check(await WaitUntil(() => drone.CurrentMove != "Hovering", 2.5) && drone.Attacking, $"then he attacks again ({drone.CurrentMove})");

        // No taunt while he's landed (he never steals a stomp window)
        Check(await FastForwardUntil(() => drone.CanBeStomped, 12), "his battery runs low and he lands");
        spareBatteries = 1;
        robot.BlinkTime = 0;
        bzzts = sounds.TimesPlayed(Sfx.Bzzt);
        Check(Bonk(null) && state == GameState.Rebuilding, "Bolt-E crashes while DRONEZILLA is landed");
        Check(drone.CurrentMove == "Landed" && drone.CanBeStomped && sounds.TimesPlayed(Sfx.Bzzt) == bzzts,
              $"no BZZT BZZT: he stays landed, so Bolt-E can still stomp him ({drone.CurrentMove})");
    }

    /// <summary>
    /// Beating DRONEZILLA: every bomb and mini-drone puffs away, his buzz stops, he sputters, smokes and spins down out of
    /// the sky, CRASHES, and blows apart into his body, blades, arms, camera and skids. "YOU BEAT DRONEZILLA!", +250, the
    /// rainbow of bolts, and it counts as a boss beaten; then the World Tour moves on, like after a Big Rusty win.
    /// </summary>
    async Task BeatDronezilla()
    {
        var drone = StartDronezillaFight("TDroneBeat");
        bestScore = 1_000_000; // (so a NEW HIGH SCORE! banner can't cover the victory words)
        Check(await WaitUntil(() => drone.CurrentMove == "Hovering", 3), "DRONEZILLA flew in");
        AddMiniDrone(new Vector2(800, GroundY - 200));
        AddBomb(new Vector2(900, GroundY - 380));
        int bonus = bonusPoints, beaten = bossesBeaten, loose = bolts.Count + boltsCollected;
        int jingles = sounds.TimesPlayed(Sfx.NewWorld), errors = SelfTestLogger.Errors, crashes = sounds.TimesPlayed(Sfx.Slam);
        drone.LoseHealth(BossBase.MaxHealth); // (as if Bolt-E stomped him 6 times)
        Check(drone.IsBeaten && drone.CurrentMove == "Exploding" && !drone.IsDangerous && !drone.CanBeStomped, "he sputters and smokes");
        await NextFrame();
        await NextFrame();
        Check(bombs.Count == 0 && zapBeams.Count == 0 && !enemies.Any(enemy => enemy.Kind == EnemyKind.MiniDrone),
              "every mini-drone and bomb puffs away");
        Check(!sounds.IsPlaying(Sfx.RotorBuzz), "his rotor buzz stops");
        Check(await WaitUntil(() => sounds.TimesPlayed(Sfx.Slam) > crashes, 3) && drone.Position.Y == GroundY && !drone.HasBlownUp,
              "he spins down out of the sky and CRASHES into the ground");
        Check(await WaitUntil(() => drone.HasBlownUp, 2), "then he blows up");
        Check(hud.BannerText == $"YOU BEAT {Dronezilla.DronezillaName}!", $"'{hud.BannerText}'");
        Check(bossesBeaten == beaten + 1 && bonusPoints == bonus + PointsForBeatingBoss, $"+{bonusPoints - bonus}, and it counts as a boss beaten");
        Check(bolts.Count + boltsCollected >= loose + 12, $"the rainbow of bolts ({bolts.Count + boltsCollected - loose} new bolts)");
        foreach (string piece in new[] { "body", "blade", "arm", "camera", "skids" })
            Check(drone.HasPiece(piece), $"his {piece} flies off");
        await Wait(1);
        Check(worldNumber == 0 && hud.BannerText == $"YOU BEAT {Dronezilla.DronezillaName}!", $"a second later it's still Sunny Hills, so the victory words show first (world {worldNumber}, '{hud.BannerText}')");
        Check(await WaitUntil(() => worldNumber == 1, 2) && hud.BannerText == "WELCOME TO CANDY LAND!" && sounds.TimesPlayed(Sfx.NewWorld) == jingles + 1,
              $"then on to the next world, like after a Big Rusty win: '{hud.BannerText}'");
        Check(await WaitUntil(() => boss is null, 4), "his pieces are cleared away");
        Check(SelfTestLogger.Errors == errors && !sounds.IsPlaying(Sfx.RotorBuzz), "no engine errors while his pieces fly, and no buzz");

        spareBatteries = 0;
        bestScore = 1_000_000;
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.GameOver, "game over");
        Check(await WaitUntil(() => hud.ResultsShowing, 3) &&
              AskDatabase(selfTestDbPath, "SELECT bosses_defeated FROM runs WHERE player_name = $name COLLATE NOCASE ORDER BY id DESC LIMIT 1;",
                          ("$name", hud.PlayerName)) == 1,
              "the saved game counts DRONEZILLA as a boss beaten");
    }

    /// <summary>Nobody can drive DRONEZILLA: the second controller's buttons do nothing in his fight (no banner, no "Wait...").</summary>
    async Task DroneCantBeDriven()
    {
        var drone = StartDronezillaFight("TDroneDrive");
        int ticks = sounds.TimesPlayed(Sfx.Tick), laughs = sounds.TimesPlayed(Sfx.Laugh);
        await SecondPadButton(JoyButton.A); // (while he flies in)
        Check(await WaitUntil(() => drone.CurrentMove != "Entering", 3), "DRONEZILLA flew in");
        foreach (var button in new[] { JoyButton.A, JoyButton.X, JoyButton.LeftShoulder, JoyButton.Y, JoyButton.B, JoyButton.RightShoulder })
            await SecondPadButton(button);
        Check(!drone.CanBeDriven && !drone.DriverControlled && drone.Request(BossMove.Pound) == DriveAnswer.Busy,
              "DRONEZILLA can't be driven: the second controller's buttons do nothing");
        Check(hud.BossBarName == Dronezilla.DronezillaName && !hud.BannerText.Contains("DRIVING"), $"no takeover banner, and his bar says '{hud.BossBarName}'");
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks && sounds.TimesPlayed(Sfx.Laugh) == laughs && !popups.Texts.Contains("Wait...") &&
              driverPad == -1 && !DriverGotYou, "no tick, no laugh, no 'Wait...'");
    }

    /// <summary>
    /// DRONEZILLA draws without engine errors in every move and every phase (a whole fight, sped up), in every world
    /// (with the Moon's low gravity too), and flying away. His rotor buzz stops on game over and on GoToTitle.
    /// </summary>
    async Task DroneDrawsEverywhere()
    {
        int errors = SelfTestLogger.Errors;
        var drawn = new HashSet<string>();

        // A whole fight, 3 times as fast
        var drone = StartDronezillaFight("TDroneDraw");
        Engine.TimeScale = 3;
        try
        {
            Check(await WaitUntil(() => drone.CanBeStomped, 4), "phase 1: bombs, LOW BATTERY, landed");
            drone.LoseHealth(2);
            Check(await WaitUntil(() => drone.CanBeStomped, 6) && drone.Phase == 2, "ANGRY: mini-drones, bombs, landed");
            Check(await WaitUntil(() => drone.CurrentMove == "TakingOff", 2), "not stomped: he takes off");
            drone.LoseHealth(2);
            Check(await WaitUntil(() => drone.CurrentMove == "DroppingMinis", 4) && drone.Phase == 3, "FURIOUS: mini-drones");
            drone.DoSequence(DroneAttack.Zap, DroneAttack.Swoop);
            Check(await WaitUntil(() => drone.CurrentMove == "Swooping", 3), "a zap, then a swoop");
            drone.Taunt(0.5f);
            Check(drone.CurrentMove == "Taunting", "a BZZT BZZT loop-the-loop");
            Check(await WaitUntil(() => drone.CurrentMove == "Hovering", 2), "then he hovers");
        }
        finally
        {
            Engine.TimeScale = 1;
        }
        drone.LoseHealth(BossBase.MaxHealth);
        Check(await WaitUntil(() => drone.HasBlownUp, 3), "he blows up");
        await Wait(0.5);
        drawn.UnionWith(drone.MovesDrawn);

        // Every world (on the Moon with its low gravity), and flying away when Bolt-E is out of batteries
        for (int world = 0; world < Worlds.All.Length; world++)
        {
            GoToTitle();
            var inWorld = StartDronezillaFight("TDroneWorlds", world);
            await Wait(0.3);
            inWorld.DoAttack(DroneAttack.Bombs);
            await Wait(0.3);
            inWorld.DoAttack(DroneAttack.MiniDrones);
            await Wait(0.5);
            inWorld.DoAttack(DroneAttack.Zap);
            await Wait(0.4);
            inWorld.DoAttack(DroneAttack.Swoop);
            await Wait(1.2);
            inWorld.FlyAway();
            await Wait(0.3);
            drawn.UnionWith(inWorld.MovesDrawn);
            string gravity = robot.GravityScale < 1 ? " (with its low gravity)" : "";
            Check(SelfTestLogger.Errors == errors, $"DRONEZILLA in {CurrentWorld.Name}{gravity} draws without errors");
        }

        var missing = Dronezilla.EveryMove.Where(move => !drawn.Contains(move)).ToList();
        Check(missing.Count == 0, $"every move was drawn ({drawn.Count} of {Dronezilla.EveryMove.Count()}){(missing.Count > 0 ? ": missing " + string.Join(", ", missing) : "")}");

        // The rotor buzz never keeps playing after the fight: game over...
        GoToTitle();
        var buzzing = StartDronezillaFight("TDroneBuzz");
        Check(await WaitUntil(() => sounds.IsPlaying(Sfx.RotorBuzz), 1), "his rotors buzz while he flies");
        spareBatteries = 0;
        bestScore = 1_000_000;
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.GameOver, "game over");
        await NextFrame();
        await NextFrame();
        Check(!sounds.IsPlaying(Sfx.RotorBuzz) && buzzing.CurrentMove == "FlyingAway", "the buzz stops on game over (and he zooms away)");
        await Wait(0.5);
        Check(!sounds.IsPlaying(Sfx.RotorBuzz), "...and it stays stopped");

        // ...and going back to the title screen
        GoToTitle();
        StartDronezillaFight("TDroneBuzz2");
        Check(await WaitUntil(() => sounds.IsPlaying(Sfx.RotorBuzz), 1), "buzzing again in a new fight");
        GoToTitle();
        await NextFrame();
        Check(!sounds.IsPlaying(Sfx.RotorBuzz), "the buzz stops on GoToTitle");
        Check(SelfTestLogger.Errors == errors, "no engine errors");
    }
}
