using Godot;

/// <summary>
/// Grown-up testing code for OPTI-MAX, the fourth boss (more of the self-test: see Main.SelfTest.cs).
/// To run only these checks (and the taking-turns rules), add --selftest-optimax after --selftest.
/// </summary>
public partial class Main
{
    /// <summary>The tests that --selftest-optimax runs.</summary>
    static readonly string[] OptiMaxTestNames =
    {
        nameof(BossesTakeTurns), nameof(ThreeBossesTakeTurns), nameof(FourBossesTakeTurns), nameof(OptiMaxIsJumpable),
        nameof(BoxToss), nameof(SuperStompWaves), nameof(OptiMaxUpdatesAndStomp), nameof(SoccerAndRun),
        nameof(OptiMaxTauntOnRebuild), nameof(BeatOptiMax), nameof(OptiMaxCantBeDriven), nameof(OptiMaxDrawsEverywhere),
    };

    /// <summary>
    /// A new game with nothing on the road, and OPTI-MAX walks in right away (in world number "world": 1 = Candy Land).
    /// Nothing in the fight can crash Bolt-E, unless a test lets it (robot.BlinkTime = 0).
    /// </summary>
    OptiMax StartOptiMaxFight(string name, int world = 0)
    {
        PlayAs(name);
        StartGame();
        QuietRoad();
        if (world > 0) ChangeWorld(world);
        robot.BlinkTime = 1e9f;
        SpawnBoss(BossKind.OptiMax);
        return (OptiMax)boss!;
    }

    /// <summary>From now on Bolt-E can really crash: no spare battery, not blinking (and no NEW HIGH SCORE! words).</summary>
    void LetBoltECrash()
    {
        spareBatteries = 0;
        bestScore = 1_000_000; // (so it isn't a new high score)
        robot.BlinkTime = 0;
    }

    /// <summary>Bolt-E jumps straight up next to OPTI-MAX's back, and comes down right on top of him. True if he got stomped.</summary>
    async Task<bool> StompOptiMax(OptiMax opti)
    {
        int health = opti.Health;
        await WaitUntil(() => robot.OnGround, 2);
        robot.Position = new Vector2(opti.Hitbox.GetCenter().X, GroundY);
        robot.Jump();
        return await WaitUntil(() => opti.Health < health, 2);
    }

    /// <summary>How high the top of OPTI-MAX's bump box is above the ground.</summary>
    float BumpTop(OptiMax opti) => GroundY - opti.Top;

    /// <summary>
    /// The FOUR bosses take turns: BIG RUSTY, VROOMBA, DRONEZILLA, OPTI-MAX, BIG RUSTY... and every game starts one boss
    /// further along (game 5 is like game 1). Like a real game, four fights in a row bring all four bosses, each with his
    /// name on the health bar, and OPTI-MAX with his own tip (and it fits on the screen).
    /// </summary>
    async Task FourBossesTakeTurns()
    {
        await NextFrame();
        var order = new[] { BossKind.BigRusty, BossKind.Vroomba, BossKind.Dronezilla, BossKind.OptiMax };
        for (int game = 1; game <= 5; game++)
        {
            for (int fight = 1; fight <= 5; fight++)
            {
                var comes = BossForFight(game, fight);
                Check(comes == order[(game - 1 + fight - 1) % 4], $"game {game}, fight {fight}: {comes}");
            }
        }
        Check(BossForFight(4, 1) == BossKind.OptiMax && BossForFight(5, 1) == BossKind.BigRusty,
              "game 4 starts with OPTI-MAX, and game 5 is like game 1");

        testTakeTurns = true;
        try
        {
            PlayAs("TTurns4");
            StartGame();
            QuietRoad();
            robot.BlinkTime = 1e9f;
            var seen = new List<BossKind>();
            for (int fight = 1; fight <= 4; fight++)
            {
                SpawnBoss();
                var kind = KindOf(boss);
                seen.Add(kind);
                Check(kind == BossForFight(gameNumber, fight) && hud.BossBarName == boss!.DisplayName,
                      $"game {gameNumber}, fight {fight} follows the rule: {kind} ('{hud.BossBarName}' on his bar)");
                if (kind == BossKind.OptiMax)
                {
                    float tipWidth = ThemeDB.FallbackFont.GetStringSize(OptiMaxHint, HorizontalAlignment.Left, -1, 28).X;
                    Check(hud.HintShowing && hud.HintText == OptiMaxHint && tipWidth < ScreenWidth - 40,
                          $"OPTI-MAX's tip: '{hud.HintText}' ({tipWidth:0} px wide)");
                }
                boss!.QueueFree();
                boss = null;
                await NextFrame();
            }
            Check(seen.Distinct().Count() == 4, $"four fights in a row bring all four bosses ({string.Join(", ", seen)})");
        }
        finally
        {
            testTakeTurns = false;
        }
    }

    /// <summary>
    /// OPTI-MAX walks in from the right (clank, clank), waves hello, and stands about 290 px tall. Standing, walking and
    /// running, the top of his bump box stays below 300 px, so a DOUBLE JUMP always clears him. He can't be stomped
    /// unless he's updating, and touching him while he walks or runs is a crash.
    /// </summary>
    async Task OptiMaxIsJumpable()
    {
        var opti = StartOptiMaxFight("TOptiJump");
        Check(opti.CurrentMove == "Entering" && opti.Position.X > ScreenWidth && opti.Position.Y == GroundY,
              $"he walks in from the right ({opti.Position.X:0})");
        Check(hud.BossBarShowing && hud.BossBarName == OptiMax.OptiMaxName && opti.Health == BossBase.MaxHealth,
              $"his health bar says '{hud.BossBarName}' (6 health)");
        int steps = sounds.TimesPlayed(Sfx.RobotStep);
        Check(await WaitUntil(() => sounds.TimesPlayed(Sfx.RobotStep) >= steps + 2, 1.5) && !opti.IsDangerous && !opti.CanBeStomped,
              "clank, clank: heavy footsteps (and walking in, he's harmless)");
        Check(await WaitUntil(() => opti.CurrentMove == "Hello", 3) && Mathf.Abs(opti.Position.X - (OptiMax.StackRightX - OptiMax.StackReach)) < 1,
              $"he stops next to his box stack and waves hello ({opti.Position.X:0})");
        Check(boxStacks.Count == 2 && boxStacks.All(stack => stack.TopBoxThere), "a little stack of boxes stands at each end of the arena");

        await Wait(0.4);
        float tall = opti.HeadTopHeight, top = BumpTop(opti);
        Check(tall >= 275 && tall <= 305, $"he's tall and slim: the top of his head is {tall:0} px up");
        Check(top <= 300 && top < DoubleJumpReach - 40 && opti.Hitbox.Size.X <= 64,
              $"standing, his bump box is {top:0} px tall and {opti.Hitbox.Size.X:0} px wide (a double jump reaches {DoubleJumpReach:0})");
        Check(!opti.CanBeStomped && !opti.IsDangerous, "waving hello, he can't be stomped (and doesn't hurt)");

        // Walking and running, he's always double-jumpable
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 2), "then he gets ready");
        opti.DoAttack(OptiAttack.Run);
        Check(opti.CurrentMove == "Running", $"ROBOT RUN! ({opti.CurrentMove})");
        float highest = 0, startX = opti.Position.X, fastest = 0, lastX = startX;
        bool dangerous = true, stompable = false;
        for (int frame = 0; frame < 200 && opti.CurrentMove == "Running"; frame++)
        {
            await NextFrame();
            highest = Mathf.Max(highest, BumpTop(opti));
            fastest = Mathf.Max(fastest, Mathf.Abs(opti.Position.X - lastX) * 60);
            lastX = opti.Position.X;
            dangerous &= opti.IsDangerous;
            stompable |= opti.CanBeStomped;
        }
        Check(Mathf.Abs(opti.Position.X - startX) > 500 && fastest > 500,
              $"he sprints across the arena ({startX:0} -> {opti.Position.X:0}, up to {fastest:0} px a second)");
        Check(highest <= 300 && highest < DoubleJumpReach - 40, $"running, his bump box is at most {highest:0} px tall: still double-jumpable");
        Check(dangerous && !stompable, "running, touching him is a crash (and he can't be stomped)");

        // Touching him while he walks is a crash
        GoToTitle();
        opti = StartOptiMaxFight("TOptiWalkHit");
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 4), "a new fight");
        opti.Position = new Vector2(640, GroundY);
        opti.DoAttack(OptiAttack.Boxes);
        await NextFrame();
        Check(opti.CurrentMove == "Walking" && opti.IsDangerous && !opti.CanBeStomped, $"he walks to his box stack ({opti.CurrentMove})");
        LetBoltECrash();
        robot.Position = new Vector2(opti.Position.X, GroundY);
        Check(await WaitUntil(() => state == GameState.GameOver, 1), "Bolt-E bumping into him while he walks crashes (Bonk!)");
        Check(opti.CurrentMove == "FlyingAway", $"out of batteries: OPTI-MAX waves goodbye and leaves ({opti.CurrentMove})");
        Check(await WaitUntil(() => boss is null, 5), "...jogging off the screen");

        // Touching him while he runs is a crash
        GoToTitle();
        opti = StartOptiMaxFight("TOptiRunHit");
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 4), "a new fight");
        opti.DoAttack(OptiAttack.Run);
        await Wait(0.3);
        LetBoltECrash();
        robot.Position = new Vector2(opti.Position.X, GroundY);
        Check(await WaitUntil(() => state == GameState.GameOver, 1), "Bolt-E bumping into him while he runs crashes (Bonk!)");
    }

    /// <summary>
    /// BOX TOSS! He bends down, picks up the top box from his stack, lifts it over his head and throws it in a high arc
    /// toward Bolt-E (whoosh!). A shadow on the ground shows where it will land. It lands near where Bolt-E was (crunch!),
    /// bounces once more the same way, and breaks into bits. 2 boxes (4 when FURIOUS, flying faster). A box hitting
    /// Bolt-E is a crash.
    /// </summary>
    async Task BoxToss()
    {
        var opti = StartOptiMaxFight("TOptiBoxes");
        int thrown = 0, picked = 0, whooshes = sounds.TimesPlayed(Sfx.BoxWhoosh), crunches = sounds.TimesPlayed(Sfx.BoxCrunch);
        float boltEAtThrow = 0;
        opti.BoxThrown += (_, _, _) =>
        {
            thrown++;
            boltEAtThrow = robot.Position.X;
        };
        opti.BoxPickedUp += _ => picked++;
        Check(await WaitUntil(() => opti.CurrentMove == "PickingUp", 5), "phase 1: his first attack is BOX TOSS (he bends down to his stack)");
        Check(await WaitUntil(() => picked == 1, 1) && !boxStacks.All(stack => stack.TopBoxThere), "he takes the top box off the stack");
        Check(await WaitUntil(() => opti.CurrentMove == "Lifting", 1) && Mathf.Sign(robot.Position.X - opti.Position.X) == opti.Facing,
              "he lifts it over his head, turned toward Bolt-E");
        Check(await WaitUntil(() => thrownBoxes.Count == 1, 2) && opti.CurrentMove == "Throwing", "and THROWS it!");
        var box = thrownBoxes[0];
        float from = box.Position.Y;
        await NextFrame();
        Check(box.Flying && box.ShadowDrawn && sounds.TimesPlayed(Sfx.BoxWhoosh) == whooshes + 1, "whoosh! a shadow on the ground shows where it will land");
        Check(Mathf.Abs(box.LandX - boltEAtThrow) < 40, $"aimed at Bolt-E (it lands at x {box.LandX:0}, Bolt-E was at {boltEAtThrow:0})");
        float highest = from;
        while (IsInstanceValid(box) && box.Flying)
        {
            highest = Mathf.Min(highest, box.Position.Y);
            await NextFrame();
        }
        Check(from - highest > 40, $"it flies in a high arc (up {from - highest:0} px over his hands, {GroundY - highest:0} px over the ground)");
        Check(box.Bouncing && Mathf.Abs(box.Position.X - box.LandX) < 30 && sounds.TimesPlayed(Sfx.BoxCrunch) == crunches + 1,
              $"crunch! it lands near where Bolt-E was (x {box.Position.X:0})");
        float landedX = box.Position.X;
        bool dangerousHop = true;
        while (IsInstanceValid(box) && box.Bouncing)
        {
            dangerousHop &= box.Dangerous;
            await NextFrame();
        }
        Check(dangerousHop && Mathf.Sign(box.Position.X - landedX) == Mathf.Sign(landedX - opti.Position.X),
              $"it bounces once more the same way ({landedX:0} -> {box.Position.X:0}), and touching it is still a crash");
        Check(box.Finished && sounds.TimesPlayed(Sfx.BoxCrunch) == crunches + 2, "then it breaks into a puff of cardboard bits");
        await NextFrame();
        Check(!thrownBoxes.Contains(box), "(and it's gone)");
        Check(await WaitUntil(() => opti.IsUpdating, 4) && thrown == OptiMax.BoxesWhenCalm, $"{thrown} boxes, then he stops to update");

        // A box hitting Bolt-E (no spare battery, not blinking): a crash
        GoToTitle();
        opti = StartOptiMaxFight("TOptiBoxHit");
        opti.SetProcess(false); // (he stays out of it)
        LetBoltECrash();
        AddThrownBox(new Vector2(robot.Position.X + 300, GroundY - 330), robot.Position.X, OptiMax.BoxFlightSeconds);
        Check(await WaitUntil(() => state == GameState.GameOver, 2), "a box landing on Bolt-E is a crash (Bonk!)");

        // Away from it: no crash
        GoToTitle();
        opti = StartOptiMaxFight("TOptiBoxMiss");
        opti.SetProcess(false);
        LetBoltECrash();
        AddThrownBox(new Vector2(robot.Position.X + 600, GroundY - 330), robot.Position.X + 300, OptiMax.BoxFlightSeconds);
        var miss = thrownBoxes[^1];
        Check(await WaitUntil(() => !IsInstanceValid(miss) || miss.Finished, 3), "the box lands, bounces and breaks");
        Check(state == GameState.Playing && !robot.Crashed, "Bolt-E wasn't under it: no crash");

        // FURIOUS: 4 boxes, flying faster
        GoToTitle();
        opti = StartOptiMaxFight("TOptiBoxes4");
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 4), "OPTI-MAX walked in");
        opti.LoseHealth(4);
        Check(await FastForwardUntil(() => opti.CurrentMove == "GettingAngry", 3), "he's FURIOUS");
        int furiousThrows = 0;
        float seconds = 0;
        opti.BoxThrown += (_, _, flight) =>
        {
            furiousThrows++;
            seconds = flight;
        };
        opti.DoAttack(OptiAttack.Boxes);
        Check(await FastForwardUntil(() => opti.IsUpdating, 10) && furiousThrows == OptiMax.BoxesWhenFurious,
              $"FURIOUS: {furiousThrows} boxes");
        Check(Mathf.IsEqualApprox(seconds, OptiMax.FuriousBoxFlightSeconds) && seconds < OptiMax.BoxFlightSeconds,
              $"...flying faster ({seconds:0.00} s in the air)");
    }

    /// <summary>
    /// SUPER STOMP! He crouches (knees bent), jumps up high, and lands with a STOMP (a boom and a shake). Two low teal
    /// shockwaves run along the ground away from him, both ways. Bolt-E standing in a wave's way crashes; jumping over
    /// it, he's fine. FURIOUS, he stomps twice in a row.
    /// </summary>
    async Task SuperStompWaves()
    {
        var opti = StartOptiMaxFight("TOptiStomp");
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 4), "OPTI-MAX walked in");
        float standing = opti.HeadTopHeight;
        int booms = sounds.TimesPlayed(Sfx.Slam), stomped = 0;
        opti.Stomped += _ => stomped++;
        opti.DoAttack(OptiAttack.Stomp);
        Check(opti.CurrentMove == "Crouching" && opti.IsDangerous, "SUPER STOMP! First he crouches...");
        await Wait(0.35);
        Check(opti.HeadTopHeight < standing - 40, $"...knees bent ({standing:0} -> {opti.HeadTopHeight:0} px tall)");
        Check(await WaitUntil(() => opti.CurrentMove == "Jumping", 1), "then he jumps...");
        float highest = 0;
        while (opti.CurrentMove == "Jumping")
        {
            highest = Mathf.Max(highest, opti.HeadTopHeight);
            await NextFrame();
        }
        Check(highest > standing + 120, $"...up high (his head gets {highest:0} px up)");
        Check(stomped == 1 && sounds.TimesPlayed(Sfx.Slam) == booms + 1 && shake > 0.2f, "...and lands: STOMP! (a boom and a screen shake)");
        Check(stompWaves.Count == 2 && stompWaves.Any(wave => wave.Direction < 0) && stompWaves.Any(wave => wave.Direction > 0),
              "two shockwaves, one each way");
        var left = stompWaves.First(wave => wave.Direction < 0);
        var right = stompWaves.First(wave => wave.Direction > 0);
        float leftX = left.Position.X, rightX = right.Position.X;
        await Wait(0.25);
        Check(left.Position.X < leftX - 50 && right.Position.X > rightX + 50, "they run away from him along the ground");
        float waveHeight = left.Hitbox.Size.Y + (GroundY - left.Hitbox.End.Y);
        Check(waveHeight >= 30 && waveHeight <= 45 && left.Hitbox.End.Y >= GroundY - 1, $"low glowing waves, {waveHeight:0} px high (jump over them!)");

        // Bolt-E standing in a wave's way crashes
        GoToTitle();
        opti = StartOptiMaxFight("TOptiWaveHit");
        opti.SetProcess(false);
        LetBoltECrash();
        AddStompWave(robot.Position.X + 200, -1);
        Check(await WaitUntil(() => state == GameState.GameOver, 1.5), "a wave running into Bolt-E on the ground is a crash");

        // Jumping over it: no crash
        GoToTitle();
        opti = StartOptiMaxFight("TOptiWaveJump");
        opti.SetProcess(false);
        LetBoltECrash();
        await WaitUntil(() => robot.OnGround, 1);
        AddStompWave(robot.Position.X + 200, -1);
        var wave = stompWaves[^1];
        Check(robot.Jump(), "Bolt-E jumps...");
        Check(await WaitUntil(() => wave.Position.X < robot.Position.X - 80, 2) && state == GameState.Playing && !robot.Crashed,
              "...and the wave runs right under him: no crash");

        // FURIOUS: two stomps in a row
        GoToTitle();
        opti = StartOptiMaxFight("TOptiStomp2");
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 4), "a new fight");
        opti.LoseHealth(4);
        Check(await FastForwardUntil(() => opti.CurrentMove == "GettingAngry", 3), "he's FURIOUS");
        int furiousStomps = 0;
        opti.Stomped += _ => furiousStomps++;
        opti.DoAttack(OptiAttack.Stomp);
        Check(await FastForwardUntil(() => opti.IsUpdating, 8) && furiousStomps == 2, $"FURIOUS: {furiousStomps} stomps in a row, then he updates");
    }

    /// <summary>
    /// SOFTWARE UPDATE: every attack ends with him freezing mid-pose ("UPDATING..."), then he kneels on one knee with his
    /// head bowed, so the top of his head is only about 120 px up, and a progress bar fills over his head (with computer
    /// chirps). He kneels at least 250 px from Bolt-E. Touching him then never hurts. A stomp takes 1 health: sparks, an
    /// error buzz and a red "ERROR!", and he stands back up. Left alone, the bar fills to 100%, DING!, and he stands up.
    /// </summary>
    async Task OptiMaxUpdatesAndStomp()
    {
        var opti = StartOptiMaxFight("TOptiUpdate");
        int chirps = sounds.TimesPlayed(Sfx.Chirp);
        Check(await FastForwardUntil(() => opti.CurrentMove == "Freezing", 12), "after his attack he freezes...");
        Check(popups.Texts.Contains("UPDATING...") && sounds.TimesPlayed(Sfx.Chirp) > chirps && opti.IsUpdating && !opti.IsDangerous,
              "...'UPDATING...' (with a computer chirp), and he's harmless");
        Check(!opti.CanBeStomped, "(not stompable until he's down on his knee)");
        Check(await WaitUntil(() => opti.CurrentMove == "Updating", 1.5) && opti.CanBeStomped, "he kneels down: UPDATING, stomp him now!");
        await Wait(0.3);
        float head = opti.HeadTopHeight, top = BumpTop(opti), away = Mathf.Abs(opti.Position.X - robot.Position.X);
        Check(head >= 110 && head <= 130, $"kneeling with his head bowed, the top of his head is only {head:0} px up");
        Check(top <= 140, $"(his bump box is {top:0} px tall: easy to jump on)");
        Check(away >= 250, $"he kneels {away:0} px from Bolt-E");
        Check(opti.BarLabel == "UPDATING..." && opti.UpdateProgress > 0.05f && opti.UpdateProgress < 0.5f,
              $"a progress bar fills over his head ({opti.UpdateProgress * 100:0}%)");
        chirps = sounds.TimesPlayed(Sfx.Chirp);
        await Wait(0.7);
        Check(sounds.TimesPlayed(Sfx.Chirp) >= chirps + 2, "chirp, chirp: he's busy updating");

        // Touching him while he's updating never hurts
        LetBoltECrash();
        spareBatteries = 1;
        robot.Position = new Vector2(opti.Hitbox.GetCenter().X, GroundY);
        await Wait(0.2);
        Check(state == GameState.Playing && !robot.Crashed && opti.CanBeStomped && robot.Hitbox.Intersects(opti.Hitbox),
              "touching him while he updates never hurts");
        robot.BlinkTime = 1e9f;

        // STOMP!
        int bonus = bonusPoints, clangs = sounds.TimesPlayed(Sfx.BossHit), buzzes = sounds.TimesPlayed(Sfx.ErrorBuzz);
        Check(await StompOptiMax(opti), "Bolt-E comes down on his back: STOMP!");
        Check(opti.Health == BossBase.MaxHealth - 1 && opti.CurrentMove == "Hurt" && !opti.CanBeStomped && !opti.IsDangerous,
              $"health {opti.Health}, and he's {opti.CurrentMove} (harmless)");
        Check(bonusPoints == bonus + PointsPerBossHit && sounds.TimesPlayed(Sfx.BossHit) == clangs + 1, $"+{bonusPoints - bonus} and a CLANG");
        await NextFrame();
        Check(opti.BarLabel == "ERROR!" && sounds.TimesPlayed(Sfx.ErrorBuzz) == buzzes + 1, $"sparks, an error buzz, and the bar says '{opti.BarLabel}'");
        Check(await WaitUntil(() => opti.CurrentMove != "Hurt", 2) && await WaitUntil(() => opti.HeadTopHeight > 250, 1.5),
              $"he wobbles and stands back up ({opti.CurrentMove})");

        // Not stomped: the bar fills to 100%, DING!, and he stands up
        Check(await FastForwardUntil(() => opti.CurrentMove == "Updating", 12), "his next attack ends with an update too");
        int dings = sounds.TimesPlayed(Sfx.Ding);
        Check(await FastForwardUntil(() => opti.CurrentMove == "StandingUp", OptiMax.UpdateSeconds + 0.5),
              "if he isn't stomped, the bar fills up...");
        Check(opti.UpdateProgress >= 1 && sounds.TimesPlayed(Sfx.Ding) == dings + 1 && popups.Texts.Contains("DING!"), "...100%, DING!");
        Check(await WaitUntil(() => opti.HeadTopHeight > 250, 1.5) && opti.Health == BossBase.MaxHealth - 1,
              "and he stands up (no health lost)");

        // Bolt-E right next to him when his attack ends: he walks away a bit first, then kneels
        GoToTitle();
        opti = StartOptiMaxFight("TOptiKneelAway");
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 4), "a new fight");
        opti.Position = new Vector2(760, GroundY);
        robot.Position = new Vector2(640, GroundY);
        opti.DoAttack(OptiAttack.Stomp);
        Check(await FastForwardUntil(() => opti.CurrentMove == "Positioning", 4), "Bolt-E is close: he walks a few steps away...");
        Check(await FastForwardUntil(() => opti.CanBeStomped, 4), "...then kneels");
        away = Mathf.Abs(opti.Position.X - robot.Position.X);
        Check(away >= 250 && !robot.Hitbox.Intersects(opti.Hitbox), $"{away:0} px from Bolt-E (never on him)");
    }

    /// <summary>
    /// ANGRY OPTI-MAX does the SOCCER KICK: he drops a ball and kicks it toward Bolt-E. It bounces in big hops (boing!),
    /// high enough to ride under, bounces off the far wall once and then pops. A ball hitting Bolt-E is a crash.
    /// His ROBOT RUN never squashes Bolt-E against a wall: it always stops at least 200 px before him.
    /// </summary>
    async Task SoccerAndRun()
    {
        var opti = StartOptiMaxFight("TOptiSoccer");
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 4), "OPTI-MAX walked in");
        opti.LoseHealth(2); // ANGRY (as if he was stomped twice)
        Check(await FastForwardUntil(() => opti.CurrentMove == "GettingAngry", 3) && hud.BannerText == $"{OptiMax.OptiMaxName} IS ANGRY!",
              $"he gets ANGRY: '{hud.BannerText}'");
        int kicks = sounds.TimesPlayed(Sfx.Kick), boings = sounds.TimesPlayed(Sfx.Boing);
        Check(await WaitUntil(() => opti.CurrentMove == "HoldingBall", 3), "ANGRY: his first attack is the SOCCER KICK (he holds a ball)");
        Check(await WaitUntil(() => kickBalls.Count == 1, 2) && opti.CurrentMove == "Kicking" && sounds.TimesPlayed(Sfx.Kick) == kicks + 1,
              "he drops it and KICKS it!");
        var ball = kickBalls[0];
        Check(Mathf.Sign(ball.Velocity.X) == Mathf.Sign(robot.Position.X - ball.Position.X), "toward Bolt-E");
        float highest = 0;
        for (int frame = 0; frame < 90; frame++)
        {
            highest = Mathf.Max(highest, GroundY - ball.Position.Y - KickBall.Radius);
            await NextFrame();
        }
        Check(sounds.TimesPlayed(Sfx.Boing) > boings && highest > 140,
              $"boing! it bounces along in big hops ({highest:0} px high: ride under it!)");
        Check(await FastForwardUntil(() => !IsInstanceValid(ball) || ball.BouncedOffWall, 8), "it bounces off the far wall...");
        Check(await FastForwardUntil(() => !kickBalls.Contains(ball), 3), "...and then pops");

        // A ball bouncing into Bolt-E is a crash
        GoToTitle();
        opti = StartOptiMaxFight("TOptiBallHit");
        opti.SetProcess(false);
        LetBoltECrash();
        AddKickBall(new Vector2(robot.Position.X + 120, GroundY - KickBall.Radius), -1);
        Check(await WaitUntil(() => state == GameState.GameOver, 2), "a ball bouncing into Bolt-E is a crash (Bonk!)");

        // ROBOT RUN: Bolt-E against the left wall...
        GoToTitle();
        opti = StartOptiMaxFight("TOptiRunWall");
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 4), "a new fight");
        opti.LoseHealth(2);
        Check(await FastForwardUntil(() => opti.CurrentMove == "GettingAngry", 3), "he's ANGRY");
        robot.Position = new Vector2(Robot.LeftEdge, GroundY);
        opti.TargetX = robot.Position.X;
        opti.DoAttack(OptiAttack.Run);
        Check(opti.CurrentMove == "Running" && opti.RunGoal < opti.Position.X, $"ROBOT RUN! toward Bolt-E, who is against the wall ({opti.CurrentMove})");
        float closest = 10000;
        while (opti.CurrentMove == "Running")
        {
            closest = Mathf.Min(closest, Mathf.Abs(opti.Position.X - robot.Position.X));
            await NextFrame();
        }
        Check(closest >= 200 && opti.CurrentMove == "Braking", $"he stops {closest:0} px before Bolt-E (he never squashes him on the wall)");

        // ...and against the right wall
        await Wait(0.5);
        opti.Position = new Vector2(OptiMax.StackLeftX + OptiMax.StackReach, GroundY);
        robot.Position = new Vector2(Robot.RightEdge, GroundY);
        opti.TargetX = robot.Position.X;
        opti.DoAttack(OptiAttack.Run);
        Check(opti.CurrentMove == "Running" && opti.RunGoal > opti.Position.X, "the other way, Bolt-E against the right wall");
        closest = 10000;
        while (opti.CurrentMove == "Running")
        {
            closest = Mathf.Min(closest, Mathf.Abs(opti.Position.X - robot.Position.X));
            await NextFrame();
        }
        Check(closest >= 200, $"he stops {closest:0} px before Bolt-E");
    }

    /// <summary>
    /// Bolt-E crashes (with a spare) while OPTI-MAX throws boxes: a funny robot dance with "BEEP BOP DANCE!". The box
    /// that's already flying keeps flying, but no new ones come; the rebuild clears his boxes, balls and waves, and he
    /// waits a moment before attacking again. A crash while he's UPDATING never steals the stomp chance: no dance.
    /// </summary>
    async Task OptiMaxTauntOnRebuild()
    {
        var opti = StartOptiMaxFight("TOptiTaunt");
        Check(await WaitUntil(() => thrownBoxes.Count > 0, 6), "OPTI-MAX throws a box");
        AddKickBall(new Vector2(robot.Position.X + 500, GroundY - 200), 1);
        AddStompWave(robot.Position.X + 500, 1);
        int dances = sounds.TimesPlayed(Sfx.DanceBeat), throwsAfterCrash = 0;
        opti.BoxThrown += (_, _, _) => throwsAfterCrash++;
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.Rebuilding, "Bolt-E crashes, with a spare battery");
        Check(opti.CurrentMove == "Dancing" && sounds.TimesPlayed(Sfx.DanceBeat) == dances + 1 && popups.Texts.Contains("BEEP BOP DANCE!"),
              $"BEEP BOP DANCE! a funny robot dance ({opti.CurrentMove})");
        Check(thrownBoxes.Count > 0 && thrownBoxes[0].Dangerous, "the box that was already flying keeps flying");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        Check(thrownBoxes.Count == 0 && kickBalls.Count == 0 && stompWaves.Count == 0, "the rebuild cleared his boxes, balls and waves");
        robot.BlinkTime = 1e9f;
        await Wait(0.3);
        Check(opti.CurrentMove == "Idle" && throwsAfterCrash == 0, $"he still holds his attacks a moment after Bolt-E is back ({opti.CurrentMove}, {throwsAfterCrash} new boxes)");
        Check(await WaitUntil(() => opti.CurrentMove != "Idle", 2.5) && opti.IsDangerous, $"then he attacks again ({opti.CurrentMove})");

        // No dance while he's updating (he never steals a stomp window)
        Check(await FastForwardUntil(() => opti.CanBeStomped, 14), "his attack ends and he updates");
        spareBatteries = 1;
        robot.BlinkTime = 0;
        dances = sounds.TimesPlayed(Sfx.DanceBeat);
        Check(Bonk(null) && state == GameState.Rebuilding, "Bolt-E crashes while OPTI-MAX updates");
        Check(opti.CurrentMove == "Updating" && opti.CanBeStomped && sounds.TimesPlayed(Sfx.DanceBeat) == dances,
              $"no dance: he keeps updating, so Bolt-E can still stomp him ({opti.CurrentMove})");
    }

    /// <summary>
    /// Beating OPTI-MAX: every box, ball and wave (and his box stacks) puffs away, he sparks and stumbles, falls over
    /// backward with a CLANK, and blows apart into his head, chest, arms, legs, hands and feet. "YOU BEAT OPTI-MAX!",
    /// +250, the rainbow of bolts, and it counts as a boss beaten; then the World Tour moves on.
    /// </summary>
    async Task BeatOptiMax()
    {
        var opti = StartOptiMaxFight("TOptiBeat");
        bestScore = 1_000_000; // (so a NEW HIGH SCORE! banner can't cover the victory words)
        Check(await WaitUntil(() => opti.CurrentMove == "Idle", 4), "OPTI-MAX walked in");
        AddThrownBox(new Vector2(900, GroundY - 330), 700, OptiMax.BoxFlightSeconds);
        AddKickBall(new Vector2(800, GroundY - 100), -1);
        AddStompWave(900, 1);
        int bonus = bonusPoints, beaten = bossesBeaten, loose = bolts.Count + boltsCollected;
        int jingles = sounds.TimesPlayed(Sfx.NewWorld), errors = SelfTestLogger.Errors, clanks = sounds.TimesPlayed(Sfx.Slam);
        opti.LoseHealth(BossBase.MaxHealth); // (as if Bolt-E stomped him 6 times)
        Check(opti.IsBeaten && opti.CurrentMove == "Exploding" && !opti.IsDangerous && !opti.CanBeStomped, "he sparks and stumbles");
        await NextFrame();
        await NextFrame();
        Check(thrownBoxes.Count == 0 && kickBalls.Count == 0 && stompWaves.Count == 0, "every box, ball and wave puffs away");
        Check(boxStacks.Count == 0, "(and so do his box stacks)");
        Check(await WaitUntil(() => sounds.TimesPlayed(Sfx.Slam) > clanks, 3) && !opti.HasBlownUp,
              "he falls over backward: CLANK!");
        Check(await WaitUntil(() => opti.HasBlownUp, 2), "then he blows up");
        Check(hud.BannerText == $"YOU BEAT {OptiMax.OptiMaxName}!", $"'{hud.BannerText}'");
        Check(bossesBeaten == beaten + 1 && bonusPoints == bonus + PointsForBeatingBoss, $"+{bonusPoints - bonus}, and it counts as a boss beaten");
        Check(bolts.Count + boltsCollected >= loose + 12, $"the rainbow of bolts ({bolts.Count + boltsCollected - loose} new bolts)");
        foreach (string piece in new[] { "head", "chest", "arm", "leg", "hand", "foot" })
            Check(opti.HasPiece(piece), $"his {piece} flies off");
        await Wait(1);
        Check(worldNumber == 0 && hud.BannerText == $"YOU BEAT {OptiMax.OptiMaxName}!", $"a second later it's still Sunny Hills, so the victory words show first (world {worldNumber}, '{hud.BannerText}')");
        Check(await WaitUntil(() => worldNumber == 1, 2) && hud.BannerText == "WELCOME TO CANDY LAND!" && sounds.TimesPlayed(Sfx.NewWorld) == jingles + 1,
              $"then on to the next world, like after a Big Rusty win: '{hud.BannerText}'");
        Check(await WaitUntil(() => boss is null, 4), "his pieces are cleared away");
        Check(SelfTestLogger.Errors == errors, "no engine errors while his pieces fly");

        LetBoltECrash();
        Check(Bonk(null) && state == GameState.GameOver, "game over");
        Check(await WaitUntil(() => hud.ResultsShowing, 3) &&
              AskDatabase(selfTestDbPath, "SELECT bosses_defeated FROM runs WHERE player_name = $name COLLATE NOCASE ORDER BY id DESC LIMIT 1;",
                          ("$name", hud.PlayerName)) == 1,
              "the saved game counts OPTI-MAX as a boss beaten");
    }

    /// <summary>Nobody can drive OPTI-MAX: the second controller's buttons do nothing in his fight (no banner, no "Wait...").</summary>
    async Task OptiMaxCantBeDriven()
    {
        var opti = StartOptiMaxFight("TOptiDrive");
        int ticks = sounds.TimesPlayed(Sfx.Tick), laughs = sounds.TimesPlayed(Sfx.Laugh);
        await SecondPadButton(JoyButton.A); // (while he walks in)
        Check(await WaitUntil(() => opti.CurrentMove != "Entering", 3), "OPTI-MAX walked in");
        foreach (var button in new[] { JoyButton.A, JoyButton.X, JoyButton.LeftShoulder, JoyButton.Y, JoyButton.B, JoyButton.RightShoulder })
            await SecondPadButton(button);
        Check(!opti.CanBeDriven && !opti.DriverControlled && opti.Request(BossMove.Pound) == DriveAnswer.Busy,
              "OPTI-MAX can't be driven: the second controller's buttons do nothing");
        Check(hud.BossBarName == OptiMax.OptiMaxName && !hud.BannerText.Contains("DRIVING"), $"no takeover banner, and his bar says '{hud.BossBarName}'");
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks && sounds.TimesPlayed(Sfx.Laugh) == laughs && !popups.Texts.Contains("Wait...") &&
              driverPad == -1 && !DriverGotYou, "no tick, no laugh, no 'Wait...'");
    }

    /// <summary>
    /// OPTI-MAX draws without engine errors in every move and every phase (a whole fight, sped up), in every world (with
    /// the Moon's low gravity too), and leaving. None of his sounds keep playing after the fight or on GoToTitle.
    /// </summary>
    async Task OptiMaxDrawsEverywhere()
    {
        int errors = SelfTestLogger.Errors;
        var drawn = new HashSet<string>();

        // A whole fight, 3 times as fast
        var opti = StartOptiMaxFight("TOptiDraw");
        Engine.TimeScale = 3;
        try
        {
            Check(await WaitUntil(() => opti.CanBeStomped, 4), "phase 1: box toss, then UPDATING");
            opti.LoseHealth(2);
            Check(await WaitUntil(() => opti.CanBeStomped, 7) && opti.Phase == 2, "ANGRY: soccer kick, box toss, UPDATING");
            Check(await WaitUntil(() => opti.CurrentMove == "StandingUp", 2), "not stomped: he stands up");
            opti.LoseHealth(2);
            Check(await WaitUntil(() => opti.CurrentMove == "HoldingBall", 4) && opti.Phase == 3, "FURIOUS: soccer kick");
            opti.DoSequence(OptiAttack.Run, OptiAttack.Stomp);
            Check(await WaitUntil(() => opti.CurrentMove == "Jumping", 3), "a robot run, then a super stomp");
            opti.Taunt(0.5f);
            Check(opti.CurrentMove == "Dancing", "a BEEP BOP DANCE");
            Check(await WaitUntil(() => opti.CurrentMove == "Idle", 2), "then he stands still");
        }
        finally
        {
            Engine.TimeScale = 1;
        }
        opti.LoseHealth(BossBase.MaxHealth);
        Check(await WaitUntil(() => opti.HasBlownUp, 3), "he blows up");
        await Wait(0.5);
        drawn.UnionWith(opti.MovesDrawn);

        // Every world (on the Moon with its low gravity), and leaving when Bolt-E is out of batteries
        for (int world = 0; world < Worlds.All.Length; world++)
        {
            GoToTitle();
            var inWorld = StartOptiMaxFight("TOptiWorlds", world);
            await Wait(0.3);
            inWorld.Position = new Vector2(640, GroundY);
            inWorld.DoAttack(OptiAttack.Boxes); // (he walks to his stack)
            await Wait(0.4);
            inWorld.DoAttack(OptiAttack.Kick);
            await Wait(0.7);
            robot.Position = new Vector2(inWorld.Position.X - 100, GroundY);
            inWorld.DoAttack(OptiAttack.Stomp);
            await Wait(1.9); // (Bolt-E is close, so he walks away a bit before he updates)
            inWorld.DoAttack(OptiAttack.Run);
            await Wait(0.5);
            inWorld.FlyAway();
            await Wait(0.4);
            drawn.UnionWith(inWorld.MovesDrawn);
            string gravity = robot.GravityScale < 1 ? " (with its low gravity)" : "";
            Check(SelfTestLogger.Errors == errors, $"OPTI-MAX in {CurrentWorld.Name}{gravity} draws without errors");
        }

        var missing = OptiMax.EveryMove.Where(move => !drawn.Contains(move)).ToList();
        Check(missing.Count == 0, $"every move was drawn ({drawn.Count} of {OptiMax.EveryMove.Count()}){(missing.Count > 0 ? ": missing " + string.Join(", ", missing) : "")}");

        // None of his sounds keep playing after the fight: game over...
        Sfx[] hisSounds = { Sfx.RobotStep, Sfx.BoxWhoosh, Sfx.BoxCrunch, Sfx.Boing, Sfx.Kick, Sfx.Chirp, Sfx.Ding, Sfx.ErrorBuzz, Sfx.DanceBeat };
        GoToTitle();
        var dancer = StartOptiMaxFight("TOptiSounds");
        Check(await WaitUntil(() => dancer.CurrentMove == "Idle", 4), "a new fight");
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.Rebuilding && sounds.IsPlaying(Sfx.DanceBeat), "a crash: BEEP BOP DANCE!");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        LetBoltECrash();
        Check(Bonk(null) && state == GameState.GameOver, "game over");
        Check(await WaitUntil(() => !hisSounds.Any(sounds.IsPlaying), 2) && dancer.CurrentMove == "FlyingAway",
              $"his sounds stop after the fight (and he waves goodbye) (still playing: {string.Join(", ", hisSounds.Where(sounds.IsPlaying))}; {dancer.CurrentMove})");

        // ...and going back to the title screen in the middle of a dance
        GoToTitle();
        dancer = StartOptiMaxFight("TOptiSounds2");
        Check(await WaitUntil(() => dancer.CurrentMove == "Idle", 4), "a new fight");
        robot.BlinkTime = 0;
        Check(Bonk(null) && sounds.IsPlaying(Sfx.DanceBeat), "dancing again");
        GoToTitle();
        await NextFrame();
        Check(!hisSounds.Any(sounds.IsPlaying) && boxStacks.Count == 0,
              $"GoToTitle stops his sounds (and his box stacks are gone) (still playing: {string.Join(", ", hisSounds.Where(sounds.IsPlaying))}; {boxStacks.Count} stacks)");
        Check(SelfTestLogger.Errors == errors, "no engine errors");
    }
}
