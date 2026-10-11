using Godot;

/// <summary>
/// Grown-up testing code for VOLCANO ISLAND and its LAVA CAVES, the 7th world (more of the self-test: see Main.SelfTest.cs).
/// To run only these checks, add --selftest-volcano after --selftest.
/// </summary>
public partial class Main
{
    /// <summary>The tests that --selftest-volcano runs.</summary>
    static readonly string[] VolcanoTestNames =
    {
        nameof(VolcanoIsSeventh), nameof(VolcanoPickable), nameof(PlatformsSink), nameof(LavaDangers), nameof(VolcanoFairness),
        nameof(HolesHaveHardness), nameof(CaveByHardness), nameof(CaveExit), nameof(CaveRebuildAndGameOver), nameof(VolcanoBoss),
        nameof(VolcanoPowersAndPause), nameof(VolcanoDraws),
    };

    /// <summary>A new game that starts on VOLCANO ISLAND (picked on the title screen), on its safe rock, with nothing else coming.</summary>
    async Task StartVolcanoGame(string name)
    {
        PlayAs(name);
        startWorld = Worlds.VolcanoIndex;
        StartGame();
        startWorld = 0;
        QuietRoad();
        await NextFrame();
    }

    /// <summary>Takes away every rock and bubble, right away (a test then puts its own rocks on the lava).</summary>
    void OnlyLava()
    {
        ClearList(lavaRocks);
        ClearList(lavaBubbles);
    }

    /// <summary>Drops Bolt-E into the LAVA CAVE of this hardness, through a hole right under him. True when he's in it.</summary>
    async Task<bool> GoIntoCave(int hardness)
    {
        OnlyLava();
        AddRock(RobotX - 300, RobotX + 300, hole: hardness);
        return await WaitUntil(() => InCave && !CaveBusy && robot.OnGround, 3);
    }

    static float TextWidth(string text, int size) => ThemeDB.FallbackFont.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;

    // ---------- 1. The 7th world ----------

    /// <summary>
    /// VOLCANO ISLAND is the 7th world, after UPSIDE-DOWN STATION: Station -> Volcano -> Sunny Hills is "all the way
    /// around". The results card's words are right for 7 and 8 worlds, the words fit, and old saved numbers mean what they
    /// meant (6 is still the Station, and the Moon look still needs the Moon).
    /// </summary>
    async Task VolcanoIsSeventh()
    {
        var volcano = Worlds.All[6];
        Check(Worlds.All.Length == 7 && volcano.Name == "VOLCANO ISLAND" && Worlds.VolcanoIndex == 6 && volcano.Scenery == Scenery.Volcano,
              $"the 7th world is {volcano.Name}");
        Check(volcano.Dark && Mathf.IsEqualApprox(volcano.MusicSpeed, 1.08f) && volcano.Gravity == 1 && volcano.RustyOutfit == BossOutfit.FireHelmet &&
              volcano.Weather == Weather.Embers,
              "it's a dark world (with the glow), with embers, its music is a bit faster (1.08), and Big Rusty wears a fire helmet there");

        PlayAs("TVolcanoSeventh");
        StartGame();
        QuietRoad();
        ChangeWorld(5);
        ChangeWorld(6);
        Check(CurrentWorld == volcano && hud.BannerText == "WELCOME TO VOLCANO ISLAND!" && hud.HintText == VolcanoTip && worldsReached == 7,
              $"after the Station: '{hud.BannerText}', '{hud.HintText}' ({worldsReached} worlds)");
        Check(lavaRocks.Count == 1 && RockUnder(robot.Position.X) is { Steady: true } shore && shore.Right > ScreenWidth,
              "Bolt-E arrives on a long, safe rock over the lava");
        await Wait(0.5);
        Check(state == GameState.Playing && robot.OnGround && robot.Position.Y == GroundY, "riding along on it");
        ChangeWorld(7);
        Check(CurrentWorld.Name == "SUNNY HILLS" && hud.BannerText == "YOU WENT ALL THE WAY AROUND!" && worldsReached == 8,
              $"after the Volcano: '{hud.BannerText}' in {CurrentWorld.Name} ({worldsReached} worlds)");
        Check(lavaRocks.Count == 0 && robot.GroundY == GroundY && robot.Position.Y == GroundY, "back in Sunny Hills: the rocks are gone, solid ground again");

        Check(Worlds.WorldLine(6) == "You rode all the way to UPSIDE-DOWN STATION!" && Worlds.WorldLine(7) == "You rode all the way to VOLCANO ISLAND!" &&
              Worlds.WorldLine(8) == "You went all the way around the world!",
              $"the results card: 6 = '{Worlds.WorldLine(6)}', 7 = '{Worlds.WorldLine(7)}', 8 = '{Worlds.WorldLine(8)}'");
        float line = TextWidth(Worlds.WorldLine(7), 22), banner = TextWidth("WELCOME TO VOLCANO ISLAND!", 60), caveBanner = TextWidth("LAVA CAVE - MEDIUM!", 60);
        float tips = Mathf.Max(TextWidth(VolcanoTip, 28), Mathf.Max(TextWidth(HoleTip, 28), TextWidth("Crystal bolts are worth x3! Find the GEYSER!", 28)));
        Check(line <= 800 && banner <= 1280 && caveBanner <= 1280 && tips <= 1240 && TextWidth(FindTheWayOut, 30) <= 1000,
              $"the words fit (results line {line:0} of 800 px, banners {banner:0} and {caveBanner:0} of 1280, the longest tip {tips:0})");

        // Old saved numbers keep their meaning: 6 is still the Station (the Volcano's postcard is a '?'), 7 is the Volcano
        var astronaut = Looks.All.First(look => look.Id == "astronaut");
        string station = FreshName("TOLDSTATION"), all = FreshName("TOLDVOLCANO");
        SeedRun(station, bolts: 0, world: 6);
        SeedRun(all, bolts: 0, world: 7);
        GoToTitle();
        hud.SetPlayerName(station);
        await NextFrame();
        await NextFrame();
        Check(Unlocked(astronaut) && hud.Postcards.Reached == 6 && hud.Postcards.QuestionMarksDrawn == 1,
              $"a player who reached the Station: 6 postcards, the Volcano's is a '?' ({hud.Postcards.Reached}, {hud.Postcards.QuestionMarksDrawn})");
        hud.SetPlayerName(all);
        await NextFrame();
        await NextFrame();
        Check(hud.Postcards.Reached == 7 && hud.Postcards.QuestionMarksDrawn == 0 && hud.Postcards.PicturesDrawn == 7,
              "a player who rode to the Volcano: all 7 postcards");
    }

    // ---------- 2. Picking it ----------

    /// <summary>
    /// DOWN on the title screen offers VOLCANO ISLAND to everyone (like the Station), 7 postcards fit in the strip, a game
    /// started there is fair (no postcard or look won), and the pause screen's WORLD WARP can go there too.
    /// </summary>
    async Task VolcanoPickable()
    {
        try
        {
            string name = FreshName("TPICKVOLC");
            hud.SetPlayerName(name);
            await Wait(0.2);
            var postcards = hud.Postcards;
            var steps = new List<int>();
            string line = "";
            float cardX = 0;
            for (int i = 0; i < 3; i++)
            {
                await PressButton(JoyButton.DpadDown);
                steps.Add(startWorld);
                await NextFrame();
                if (startWorld != Worlds.VolcanoIndex) continue;
                line = postcards.StartLine;
                cardX = postcards.PickedCard.Position.X;
            }
            Check(string.Join(" ", steps) == "5 6 0" && state == GameState.Title, $"a new player steps through worlds {string.Join(" ", steps)}: the Volcano is always allowed");
            Check(line == "START: VOLCANO ISLAND" && cardX > 250, $"'{line}', and its postcard (the 7th) glows (at x {cardX:0})");
            await NextFrame();
            Check(postcards.DrawnRight <= postcards.Size.X, $"7 postcards and the words stay inside their box (to {postcards.DrawnRight:0} of {postcards.Size.X:0})");
            CheckPostcardsBesideCard(hud.TitleCardRect, "title card");
            CheckCardFits(hud.TitleCardRect, "title card");

            // Starting there: the welcome, the tip, a safe rock, and its music. Fair: nothing is won.
            startWorld = Worlds.VolcanoIndex;
            ShowStartWorld();
            int looksBefore = Looks.All.Count(Unlocked);
            StartGame();
            QuietRoad();
            Check(CurrentWorld.Name == "VOLCANO ISLAND" && hud.BannerText == "WELCOME TO VOLCANO ISLAND!" && hud.HintText == VolcanoTip,
                  $"the game starts on VOLCANO ISLAND: '{hud.BannerText}', '{hud.HintText}'");
            await NextFrame();
            Check(RockUnder(robot.Position.X) is { Steady: true } && robot.OnGround && robot.Position.Y == GroundY, "on a safe rock");
            Check(Mathf.Abs(sounds.MusicPitch - 1.08f) < 0.01f, $"the Volcano's music speed ({sounds.MusicPitch:0.00})");
            LetBoltECrash();
            Check(Bonk(null) && state == GameState.GameOver, "game over");
            Check(LastRun(name, "world_reached") == 1 && (database?.FarthestWorld(name) ?? 0) == 1, "just starting there saves 1 world: its postcard isn't won");
            await Wait(2);
            Check(hud.ResultsWorldLine == "World: VOLCANO ISLAND" && postcards.Reached == 1 && postcards.RibbonsDrawn == 0 && unlockQueue.Count == 0,
                  $"the results card says '{hud.ResultsWorldLine}', with no new postcards or looks");
            Check(Looks.All.Count(Unlocked) == looksBefore, "no look was won");

            // WORLD WARP on the pause screen can go there too, and 7 big postcards fit
            GoToTitle();
            startWorld = 0;
            await StartQuietGame(name);
            PauseGame();
            await NextFrame();
            Check(pauseMenu.CanWarpTo.Contains(Worlds.VolcanoIndex) && pauseMenu.PicturesDrawn == 3 && pauseMenu.QuestionMarksDrawn == 4,
                  $"WORLD WARP offers Sunny Hills, the Station and the Volcano ({pauseMenu.PicturesDrawn} pictures, {pauseMenu.QuestionMarksDrawn} '?')");
            var first = PauseMenu.CardRect(0);
            var last = PauseMenu.CardRect(Worlds.All.Length - 1);
            Check(first.Position.X >= 110 && last.End.X <= 1170 && new Rect2(0, 0, 1280, 720).Encloses(pauseMenu.DrawnArea),
                  $"the 7 big postcards fit in the WORLD WARP row (x {first.Position.X:0} to {last.End.X:0}, of 110 to 1170)");
            float here = TextWidth("YOU ARE HERE", 16), newWord = TextWidth("NEW!", 12);
            Check(here <= first.Size.X - 16 && newWord <= 40 - 6, $"the words on the (now smaller) postcards still fit (YOU ARE HERE {here:0} px, NEW! {newWord:0} px)");
            pauseMenu.Row = WarpRow;
            StepWarpWorld(1);
            StepWarpWorld(1);
            await NextFrame();
            Check(pauseMenu.Picked == Worlds.VolcanoIndex && pauseMenu.NameLine == "VOLCANO ISLAND", $"RIGHT, RIGHT: {pauseMenu.NameLine}");
            PickPauseRow();
            Check(!paused && CurrentWorld.Name == "VOLCANO ISLAND" && worldsReached == 1 && RockUnder(robot.Position.X) is { Steady: true },
                  "WARP! onto a safe rock on VOLCANO ISLAND (warping wins no worlds)");
            await Wait(0.5);
            Check(state == GameState.Playing && robot.OnGround, "riding on");
        }
        finally
        {
            startWorld = 0;
        }
    }

    // ---------- 3. The rocks sink ----------

    /// <summary>
    /// Landing on a rock makes it sink after SinkDelay (Bolt-E goes down with it). Staying on it too long: it goes under,
    /// HOT HOT HOT!, KABOOM. Jumping onto the next rock in time is fine. A rock he never touched never sinks.
    /// </summary>
    async Task PlatformsSink()
    {
        await StartVolcanoGame("TSink");
        LetBoltECrash();
        OnlyLava();
        var under = AddRock(-300, RobotX + 3000);
        var later = AddRock(RobotX + 3200, RobotX + 3800);
        await NextFrame();
        await NextFrame();
        float touchedAt = playTime;
        Check(under.Touched && !under.Steady && under.Sunk == 0, "Bolt-E is on a rock: it knows he's there");
        await Wait(SinkDelay - 0.15f);
        Check(under.Sunk == 0, $"it waits a moment first ({SinkDelay} s)");
        int rumbles = sounds.TimesPlayed(Sfx.RockSink);
        await Wait(0.45);
        Check(under.Sunk > 0 && robot.OnGround && robot.Position.Y > GroundY + 1 && sounds.TimesPlayed(Sfx.RockSink) > rumbles - 1,
              $"then it sinks (with a rumble), and Bolt-E goes down with it (feet {robot.Position.Y - GroundY:0} px down)");
        Check(await WaitUntil(() => under.IsUnder, SinkSeconds + 1), "standing on it too long: it goes under the lava");
        float sinkTime = playTime - touchedAt;
        GD.Print($"  (VOLCANO ISLAND: a rock goes under {sinkTime:0.00} s after Bolt-E lands on it: {SinkDelay} s wait + {SinkDepth} px at {SinkSpeed} px/s)");
        Check(Mathf.Abs(sinkTime - SinkSeconds) < 0.1f, $"...after {sinkTime:0.00} s (SinkSeconds = {SinkSeconds:0.00})");
        int sizzles = sounds.TimesPlayed(Sfx.Sizzle);
        Check(await WaitUntil(() => robot.HotFeet, 0.5) && popups.Texts.Contains("HOT HOT HOT!"), "he's in the lava: HOT HOT HOT!");
        Check(await WaitUntil(() => state == GameState.GameOver, 1.5) && robot.Crashed && sounds.TimesPlayed(Sfx.Sizzle) >= sizzles,
              "...then KABOOM (no spare: game over)");
        Check(!later.Touched && later.Sunk == 0, "the rock he never touched didn't sink");

        // Keep moving! Jump from a sinking rock onto the next one in time
        GoToTitle();
        await StartVolcanoGame("TSink2");
        LetBoltECrash();
        OnlyLava();
        var a = AddRock(-300, RobotX + 350);
        var b = AddRock(RobotX + 480, RobotX + 3000);
        Check(await WaitUntil(() => a.Right - robot.Position.X <= speed * 0.25f, 3), "riding to the end of a sinking rock");
        await Press("jump");
        await Wait(0.1);
        await Release("jump");
        Check(await WaitUntil(() => robot.OnGround, 2) && state == GameState.Playing && RockUnder(robot.Position.X) == b,
              "a jump over the gap, onto the next rock: fine");
        await NextFrame();
        Check(a.Sunk > 0 && b.Touched, $"the rock he left was sinking ({a.Sunk:0} px), and now the new one knows he's there");
        await Wait(1);
        Check(state == GameState.Playing, "still riding");
    }

    // ---------- 4. Lava ----------

    /// <summary>
    /// Riding off a rock into the lava: HOT HOT HOT! (a hop and a sizzle), then KABOOM. A lava bubble popping up into
    /// Bolt-E over a gap: KABOOM. And on a long ride, bubbles never pop up under him.
    /// </summary>
    async Task LavaDangers()
    {
        await StartVolcanoGame("TLavaGap");
        LetBoltECrash();
        OnlyLava();
        AddRock(-300, RobotX + 150, steady: true);
        float rideY = robot.Position.Y;
        Check(await WaitUntil(() => robot.HotFeet, 2) && popups.Texts.Contains("HOT HOT HOT!") && sounds.IsPlaying(Sfx.Sizzle),
              "riding off a rock into the lava: HOT HOT HOT! (and a sizzle)");
        float highest = rideY;
        for (int i = 0; i < 60 && state == GameState.Playing; i++)
        {
            highest = Mathf.Min(highest, robot.Position.Y);
            await NextFrame();
        }
        Check(highest < rideY - 50, $"a hop first ({rideY - highest:0} px up)");
        Check(await WaitUntil(() => state == GameState.GameOver, 1) && robot.Crashed, "then KABOOM");

        // If he starts blinking during the hop (like when a power runs out), he's lucky: a safe rock pops up under him
        GoToTitle();
        await StartVolcanoGame("TLavaLucky");
        LetBoltECrash();
        OnlyLava();
        Check(await WaitUntil(() => robot.HotFeet, 1), "into the lava again...");
        robot.BlinkTime = 1;
        Check(await WaitUntil(() => !robot.HotFeet, 1.5) && state == GameState.Playing && RockUnder(robot.Position.X) is { Steady: true },
              "...but he started blinking: no KABOOM, a safe rock pops up under him");
        Check(await WaitUntil(() => robot.OnGround && robot.Position.Y == GroundY, 2) && state == GameState.Playing, "and he rides on, on top of it");

        // A lava bubble pops up into Bolt-E while he jumps over a gap
        GoToTitle();
        await StartVolcanoGame("TLavaBubble");
        LetBoltECrash();
        OnlyLava();
        float meet = speed * 0.72f; // (where the blob and Bolt-E meet: he's coming down, it's still up)
        AddRock(-300, RobotX + 60, steady: true);
        AddRock(RobotX + meet + 120, RobotX + 3000, steady: true);
        var bubble = new LavaBubble { GapWidth = meet + 60, Position = new Vector2(RobotX + meet, LavaSurfaceY), Drift = 0 };
        AddChild(bubble);
        lavaBubbles.Add(bubble);
        robot.Jump();
        bubble.LaunchNow();
        Check(await WaitUntil(() => state != GameState.Playing, 1.2) && state == GameState.GameOver && robot.Crashed,
              "a lava bubble pops up into Bolt-E over a gap: KABOOM");
        Check(!popups.Texts.Contains("HOT HOT HOT!") && bubble.Now == LavaBubble.Step.Flying, "(it was the bubble, not the lava)");

        // A long ride with lots of bubbles: they only pop up in gaps, never under Bolt-E
        GoToTitle();
        await StartVolcanoGame("TLavaRide");
        robot.BlinkTime = 1e9f; // (he can't fall in: he just watches)
        testRideOverHoles = true;
        volcanoSeconds = VolcanoPlanner.EasySeconds + 1;
        spawnCountdown = 0;
        int inGaps = 0, checkedBubbles = 0;
        bool done = await FastForwardUntil(() =>
        {
            foreach (var b in lavaBubbles)
            {
                if (b.Now != LavaBubble.Step.Flying) continue;
                checkedBubbles++;
                if (!lavaRocks.Any(r => r.Left < b.GapRight - 1 && r.Right > b.GapLeft + 1)) inGaps++;
            }
            return bubblesLaunched >= 8;
        }, 60);
        GD.Print($"  (VOLCANO ISLAND: {bubblesLaunched} lava bubbles popped up, {bubblesUnderBoltE} under Bolt-E; speed now {speed:0})");
        Check(done && bubblesLaunched >= 8, $"lots of lava bubbles pop up on a long ride ({bubblesLaunched})");
        Check(bubblesUnderBoltE == 0, "a bubble never pops up under Bolt-E (only in front of him, done before he gets there, or behind him)");
        Check(checkedBubbles > 0 && inGaps == checkedBubbles, $"every flying blob is in a gap, never under a rock ({inGaps} of {checkedBubbles} looks)");
    }

    // ---------- 5. Fair rocks ----------

    /// <summary>
    /// Thousands of planned rocks (always the same dice): every gap is jumpable with ONE jump at the speed it was planned
    /// for, with lots to spare (a double jump is never needed), every sinking rock can be crossed before it sinks, the first
    /// 20 seconds are easy (wide rocks, small gaps, no bubbles), bubbles stay inside their gaps, and holes have a hardness.
    /// Then a real ride checks the first 20 seconds really are easy.
    /// </summary>
    async Task VolcanoFairness()
    {
        await NextFrame();
        var test = new VolcanoPlanner();
        var dice = new Random(2026);
        var one = new List<PlannedRock>();
        float seconds = 0, nextHole = FirstHoleAt, previousRight = 0;
        test.Reset(0);
        int rocks = 0, gapsOk = 0, widthsOk = 0, easyRocks = 0, easyOk = 0, bubbles = 0, bubblesOk = 0, holes = 0, holesOk = 0, groups = 0, bigGroups = 0;
        float leastSpare = float.MaxValue, biggestGap = 0, leastCross = float.MaxValue;
        for (int i = 0; i < 4000; i++)
        {
            float speedNow = Mathf.Min(MaxSpeed, StartSpeed + SpeedUpPerSecond * seconds);
            bool holeDue = seconds >= nextHole;
            if (holeDue) nextHole = seconds + HoleEvery;
            test.Plan(speedNow, seconds, holeDue, dice, one);
            if (holeDue)
            {
                groups++;
                if (one.Count >= 2) bigGroups++;
                if (one.Select(r => r.Hole).Distinct().Count() == one.Count && one.All(r => r.Hole is >= 1 and <= 3)) holesOk += one.Count;
            }
            foreach (var rock in one)
            {
                rocks++;
                bool easy = rock.Seconds < VolcanoPlanner.EasySeconds;
                float gap = rock.Left - previousRight, reach = VolcanoPlanner.JumpReach(rock.Speed);
                biggestGap = Mathf.Max(biggestGap, gap);
                leastSpare = Mathf.Min(leastSpare, (reach - gap) / reach);
                if (gap >= VolcanoPlanner.GapMin - 0.5f && gap <= VolcanoPlanner.GapShare * reach + 0.5f) gapsOk++;
                if (rock.Hole > 0)
                {
                    holes++;
                    widthsOk++;
                }
                else
                {
                    float cross = SinkSeconds - rock.Width / rock.Speed; // seconds to spare when riding across it
                    leastCross = Mathf.Min(leastCross, cross);
                    if (rock.Width <= VolcanoPlanner.WidestRock(rock.Speed) + 0.5f && rock.Width >= VolcanoPlanner.WidthMin - 0.5f) widthsOk++;
                }
                if (easy)
                {
                    easyRocks++;
                    if (rock.Width >= VolcanoPlanner.EasyWidthMin - 0.5f && gap <= VolcanoPlanner.EasyGapShare * reach + 0.5f && !rock.Bubble && rock.Hole == 0)
                        easyOk++;
                }
                if (rock.Bubble)
                {
                    bubbles++;
                    // The blob stays inside its gap: never over a rock (where Bolt-E lands)
                    float reachOut = gap * 0.16f + LavaBubble.Radius;
                    if (gap >= VolcanoPlanner.BubbleGapMin && reachOut <= gap / 2) bubblesOk++;
                }
                previousRight = rock.Right;
                seconds += (rock.Right - rock.Left + gap) / rock.Speed;
            }
        }
        GD.Print($"  (VOLCANO ISLAND: {rocks} rocks over {seconds:0} s; the biggest gap is {biggestGap:0} px; every gap leaves at least {leastSpare * 100:0}% of a jump to spare; " +
                 $"every sinking rock is crossed with at least {leastCross:0.00} s to spare)");
        Check(rocks >= 4000 && seconds > 300, $"{rocks} rocks planned, over {seconds:0} seconds of riding");
        Check(gapsOk == rocks && leastSpare >= 0.5f - 0.001f,
              $"every gap is at most half a single jump at its speed ({gapsOk} of {rocks}, at least {leastSpare * 100:0}% to spare): a double jump is never needed");
        Check(widthsOk == rocks && leastCross > 0.5f, $"every sinking rock can be crossed before it sinks, with time to spare ({leastCross:0.00} s)");
        Check(easyRocks >= 5 && easyOk == easyRocks, $"the first {VolcanoPlanner.EasySeconds:0} seconds are easy: {easyOk} of {easyRocks} rocks wide, small gaps, no bubbles");
        Check(bubbles > 100 && bubblesOk == bubbles, $"lava bubbles only in wide gaps, and the blob stays inside the gap ({bubblesOk} of {bubbles})");
        Check(holes > 10 && holesOk == holes && bigGroups > 0, $"holes come with a hardness of 1 to 3, sometimes 2 or 3 close together to choose ({holes} holes in {groups} groups)");

        // A real ride: the first 20 seconds are easy (wide rocks, small gaps, no bubbles)
        await StartVolcanoGame("TVolcanoFair");
        robot.BlinkTime = 1e9f;
        testRideOverHoles = true;
        spawnCountdown = 0;
        int narrow = 0, wideGaps = 0, seen = 0;
        var widths = new HashSet<LavaPlatform>();
        await FastForwardUntil(() =>
        {
            foreach (var rock in lavaRocks)
            {
                if (rock.Steady || !widths.Add(rock)) continue;
                seen++;
                if (rock.Width < VolcanoPlanner.EasyWidthMin - 0.5f) narrow++;
                var before = lavaRocks.Where(r => r.Right <= rock.Left + 1).OrderByDescending(r => r.Right).FirstOrDefault();
                if (before is not null && rock.Left - before.Right > VolcanoPlanner.EasyGapShare * VolcanoPlanner.JumpReach(speed) + 1) wideGaps++;
            }
            return volcanoSeconds >= VolcanoPlanner.EasySeconds - 4;
        }, 30);
        Check(seen >= 3 && narrow == 0 && wideGaps == 0 && bubblesLaunched == 0 && lavaBubbles.Count == 0,
              $"a real ride: the first rocks are wide ({seen} rocks), with small gaps and no lava bubbles");
    }

    // ---------- 6. Holes ----------

    /// <summary>
    /// Holes come about every HoleEvery seconds, each with a hardness of 1 to 3 shown on its sign. Jumping over a hole
    /// doesn't drop Bolt-E in; riding into one does: WHEEEE!, the screen closes like an eye, and he's in the LAVA CAVE of
    /// that hardness, with its banner and flames.
    /// </summary>
    async Task HolesHaveHardness()
    {
        await StartVolcanoGame("THoles");
        robot.BlinkTime = 1e9f;
        testRideOverHoles = true; // (he rides over them, so the test can watch them come)
        spawnCountdown = 0;
        int signs = 0;
        await FastForwardUntil(() =>
        {
            foreach (var rock in lavaRocks)
                if (rock.Hole > 0 && rock.SignsDrawn > 0 && rock.Position.X < ScreenWidth) signs = Mathf.Max(signs, 1);
            return holeLog.Select(h => h.Seconds).Distinct().Count() >= 3;
        }, 110);
        var times = holeLog.Select(h => h.Seconds).Distinct().OrderBy(t => t).ToList();
        var gaps = times.Zip(times.Skip(1), (a, b) => b - a).ToList();
        GD.Print($"  (VOLCANO ISLAND: holes planned at {string.Join(", ", times.Select(t => t.ToString("0.0")))} s, hardness {string.Join(" ", holeLog.Select(h => h.Hardness))})");
        Check(times.Count >= 3 && times[0] >= VolcanoPlanner.EasySeconds && Mathf.Abs(times[0] - FirstHoleAt) < 1.5f,
              $"the first hole comes after the easy part (at {(times.Count > 0 ? times[0] : 0):0.0} s)");
        Check(gaps.Count >= 2 && gaps.All(g => g >= HoleEvery - HoleWobble - 0.5f && g <= HoleEvery + HoleWobble + 1.5f),
              $"then about every {HoleEvery:0} seconds ({string.Join(", ", gaps.Select(g => g.ToString("0.0")))} s apart)");
        Check(holeLog.All(h => h.Hardness is >= 1 and <= 3) && signs > 0, "every hole has a hardness of 1, 2 or 3, shown on its sign");
        Check(VolcanoArt.HardnessName(1) == "EASY" && VolcanoArt.HardnessName(2) == "MEDIUM" && VolcanoArt.HardnessName(3) == "HARD" &&
              VolcanoArt.HardnessColor(1).G > 0.8f && VolcanoArt.HardnessColor(3).R > 0.9f && VolcanoArt.HardnessColor(3).G < 0.3f,
              "1 flame = EASY (green), 2 = MEDIUM (orange), 3 = HARD (red)");

        // Jumping over a hole: he stays up here. Riding into the next one: WHEEEE! down into the HARD cave.
        GoToTitle();
        await StartVolcanoGame("THoleJump");
        robot.BlinkTime = 0;
        testRideOverHoles = false; // (holes drop him in again)
        OnlyLava();
        var medium = AddRock(-300, RobotX + 900, hole: 2);
        var hard = AddRock(medium.Right, medium.Right + 600, hole: 3);
        Check(await WaitUntil(() => medium.Position.X - robot.Position.X <= speed * 0.35f, 2), "a MEDIUM hole comes");
        robot.Jump();
        Check(await WaitUntil(() => robot.OnGround, 2) && !InCave && !CaveBusy && state == GameState.Playing && RockUnder(robot.Position.X) == medium,
              "jumping over the hole: he stays up here");
        int wheees = sounds.TimesPlayed(Sfx.Wheee);
        Check(await WaitUntil(() => CaveBusy, 3) && popups.Texts.Contains("WHEEEE!") && sounds.TimesPlayed(Sfx.Wheee) == wheees + 1,
              "riding into the HARD hole: WHEEEE!");
        await NextFrame();
        Check(curtain.Closed > 0 && curtain.IrisDrawn > 0, "the screen closes like an eye");
        Check(await WaitUntil(() => InCave && !CaveBusy, 3) && caveHardness == 3 && hud.BannerText == "LAVA CAVE - HARD!",
              $"down in the HARD cave: '{hud.BannerText}'");
        await NextFrame();
        await NextFrame();
        Check(curtain.Flames == 3 && curtain.FlamesDrawn > 0 && curtain.Closed == 0, "with 3 flame pictures under the banner, and the screen open again");
    }

    // ---------- 7. The caves, by hardness ----------

    /// <summary>
    /// In each cave (1, 2, 3) the world goes faster by its number (1.0, 1.15, 1.3), and so does the music. The planner's
    /// numbers go up with the hardness (more dangers, stalactites and bats), the pits are always single-jumpable, crystal
    /// bolts are worth x1, x2 or x3, HARD has bats, and stalactites shake before they fall (and land before Bolt-E gets there).
    /// </summary>
    async Task CaveByHardness()
    {
        // The planner, for each hardness (always the same dice)
        var perTen = new float[3];
        var batShare = new float[3];
        var stalactiteShare = new float[3];
        for (int h = 1; h <= 3; h++)
        {
            var test = new CavePlanner();
            var dice = new Random(7 + h);
            var one = new List<CaveDanger>();
            test.Reset(0);
            int count = 0, bats = 0, stalactites = 0, pitsOk = 0, pits = 0, roomOk = 0;
            float start = 0, riding = 0, widest = 0;
            CaveDanger? previous = null;
            for (int i = 0; i < 1500; i++)
            {
                float speedNow = Mathf.Min(MaxSpeed, StartSpeed + SpeedUpPerSecond * riding) * CavePlanner.SpeedTimes[h - 1];
                test.Plan(speedNow, h, dice, one);
                var d = one[0];
                count++;
                if (d.Kind == CaveDangerKind.Bat) bats++;
                if (d.Kind == CaveDangerKind.Stalactite) stalactites++;
                if (d.Kind == CaveDangerKind.Pit)
                {
                    pits++;
                    widest = Mathf.Max(widest, d.Right - d.Left);
                    if (d.Right - d.Left <= CavePlanner.PitShare * VolcanoPlanner.JumpReach(d.Speed) + 0.5f) pitsOk++;
                }
                if (previous is { } p)
                {
                    bool bat = p.Kind == CaveDangerKind.Bat || d.Kind == CaveDangerKind.Bat;
                    if (d.Left - p.Left >= CavePlanner.LeastRoom(d.Speed, h, bat) - 0.5f && d.Left >= p.Right + 79.5f) roomOk++;
        
                }
                else
                {
                    roomOk++;
                    start = d.Left;
                }
                riding = (d.Right - start) / speedNow;
                previous = d;
            }
            perTen[h - 1] = count / riding * 10;
            batShare[h - 1] = bats / (float)count;
            stalactiteShare[h - 1] = stalactites / (float)count;
            string name = VolcanoArt.HardnessName(h);
            GD.Print($"  (LAVA CAVE {name}: {perTen[h - 1]:0.0} dangers every 10 s, {batShare[h - 1] * 100:0}% bats, {stalactiteShare[h - 1] * 100:0}% stalactites, " +
                     $"widest pit {widest:0} px)");
            Check(pits > 100 && pitsOk == pits, $"{name}: every lava pit is at most half a single jump ({pitsOk} of {pits}, the widest {widest:0} px)");
            Check(roomOk == count, $"{name}: after every danger there's room to land and see the next one ({roomOk} of {count})");
        }
        Check(batShare[0] == 0 && batShare[1] > 0.1f && batShare[2] > batShare[1], $"no bats in EASY caves, more in HARD ones ({batShare[1] * 100:0}%, {batShare[2] * 100:0}%)");
        Check(stalactiteShare[2] > stalactiteShare[1] && stalactiteShare[1] > stalactiteShare[0], "more stalactites in harder caves");
        Check(perTen[2] > perTen[1] && perTen[1] > perTen[0], $"more dangers in harder caves ({perTen[0]:0.0}, {perTen[1]:0.0}, {perTen[2]:0.0} every 10 s)");
        Check(CavePlanner.SpeedTimes.SequenceEqual(new[] { 1f, 1.15f, 1.3f }), "the caves go 1.0, 1.15 and 1.3 times as fast");

        // Each cave for real
        for (int h = 1; h <= 3; h++)
        {
            string name = VolcanoArt.HardnessName(h);
            GoToTitle();
            await StartVolcanoGame($"TCave{h}");
            robot.BlinkTime = 1e9f;
            Check(await GoIntoCave(h) && caveHardness == h && hud.BannerText == $"LAVA CAVE - {name}!", $"{name}: into the cave ('{hud.BannerText}')");
            await Wait(0.5);
            float wantSpeed = CavePlanner.SpeedTimes[h - 1];
            Check(Mathf.Abs(boost - wantSpeed) < 0.01f && Mathf.Abs(sounds.MusicPitch - 1.08f * CaveMusicTimes[h - 1]) < 0.01f,
                  $"{name}: the world goes {boost:0.00} times as fast, and the music plays at {sounds.MusicPitch:0.00}");

            spawnCountdown = 0;
            var crystals = new HashSet<BoltPickup>();
            int plainBolts = 0;
            await FastForwardUntil(() =>
            {
                foreach (var bolt in bolts)
                    if (bolt.Crystal) crystals.Add(bolt);
                    else plainBolts++;
                return caveSeconds > 8;
            }, 12);
            float worldSpeed = speed * boost;
            float biggestPit = Mathf.Min(CavePlanner.PitMax[h - 1], CavePlanner.PitShare * VolcanoPlanner.JumpReach(worldSpeed));
            Check(cavePits.All(p => p.Width <= biggestPit + 0.5f), $"{name}: the pits on the track are single-jumpable ({cavePits.Count} pits)");
            Check(crystals.Count > 0 && plainBolts == 0 && crystals.All(b => b.Worth == h), $"{name}: the bolts are glowing CRYSTAL BOLTS worth x{h} ({crystals.Count})");
            ClearList(bolts); // (only the one below can be grabbed now)
            int got = boltsCollected;
            AddCrystalBolt(robot.Position.X + 40, robot.Position.Y - 60);
            Check(await WaitUntil(() => boltsCollected > got, 1) && boltsCollected == got + h, $"{name}: grabbing one counts {boltsCollected - got} bolts");

            // A stalactite shakes before it falls, and has landed before Bolt-E gets there
            var stalactite = new CaveRock { Kind = CaveRockKind.Stalactite, Position = new Vector2(ScreenWidth + 300, CaveFloorY) };
            AddChild(stalactite);
            caveRocks.Add(stalactite);
            int cracks = sounds.TimesPlayed(Sfx.StalactiteCrack);
            Check(await WaitUntil(() => stalactite.Now == CaveRock.Step.Shaking, 3) && sounds.TimesPlayed(Sfx.StalactiteCrack) > cracks,
                  $"{name}: a stalactite shakes first (CRACK!) when it's {stalactite.Position.X - RobotX:0} px ahead");
            Check(await WaitUntil(() => stalactite.Now == CaveRock.Step.Landed, 2), $"{name}: then it falls");
            float landedAhead = stalactite.Position.X - robot.Position.X;
            Check(landedAhead > speed * boost * 0.3f, $"{name}: it lands {landedAhead:0} px in front of Bolt-E (before he gets there)");

            if (h == 3)
            {
                // HARD has fire bats: they swoop up and down, and fly at Bolt-E
                pendingBats.Add((TrackOf(ScreenWidth + 50), 0f));
                Check(await WaitUntil(() => enemies.Any(e => e.Kind == EnemyKind.FireBat), 1), "HARD: a fire bat flies in");
                var bat = enemies.First(e => e.Kind == EnemyKind.FireBat);
                float low = bat.Position.Y, high = bat.Position.Y;
                for (int i = 0; i < 60; i++)
                {
                    await NextFrame();
                    low = Mathf.Max(low, bat.Position.Y);
                    high = Mathf.Min(high, bat.Position.Y);
                }
                Check(low - high > Enemy.BatSwoop, $"HARD: it swoops up and down ({low - high:0} px)");
            }
        }
    }

    // ---------- 8. The way out ----------

    /// <summary>
    /// At the end of a cave (about CaveLength seconds), the GEYSER: CAVE CLEAR!, the bonus for its hardness, a fanfare,
    /// WHOOSH, and Bolt-E is back on the surface on a fresh, wide, safe rock, blinking. The boss counter waited.
    /// </summary>
    async Task CaveExit()
    {
        await StartVolcanoGame("TCaveExit");
        robot.BlinkTime = 1e9f;
        nextBossAt = Meters + 600;
        Check(await GoIntoCave(2), "into the MEDIUM cave");
        float bossAhead = nextBossAt - distance / PixelsPerMeter;
        int bonusBefore = bonusPoints, fanfares = sounds.TimesPlayed(Sfx.CaveClear), whooshes = sounds.TimesPlayed(Sfx.GeyserWhoosh);
        spawnCountdown = 0;
        Check(await FastForwardUntil(() => caveStep == CaveStep.BlastOut, 45), $"he reaches the GEYSER ({caveSeconds:0.0} s in the cave)");
        float length = caveSeconds;
        GD.Print($"  (LAVA CAVE MEDIUM: it lasted {length:0.0} s, until the geyser)");
        Check(Mathf.Abs(length - CaveLength) < 3, $"a cave lasts about {CaveLength:0} seconds ({length:0.0})");
        Check(hud.BannerText == "CAVE CLEAR! +100" && bonusPoints == bonusBefore + 100 && cavesCleared == 1,
              $"'{hud.BannerText}': the MEDIUM bonus (+{bonusPoints - bonusBefore})");
        Check(sounds.TimesPlayed(Sfx.CaveClear) == fanfares + 1 && sounds.TimesPlayed(Sfx.GeyserWhoosh) == whooshes + 1 && geyser!.Fired,
              "a fanfare, and the geyser goes WHOOSH");
        robot.BlinkTime = 0;
        Check(await WaitUntil(() => !InCave && !CaveBusy, 3) && CurrentWorld.Name == "VOLCANO ISLAND", "back up on VOLCANO ISLAND");
        float bossAfter = nextBossAt - distance / PixelsPerMeter;
        var rock = RockUnder(robot.Position.X);
        Check(rock is { Steady: true } && rock.Width >= RescueLength && robot.BlinkTime > 1, $"over a fresh, wide, safe rock, blinking ({robot.BlinkTime:0.0} s)");
        Check(await WaitUntil(() => robot.OnGround, 2) && robot.Position.Y == GroundY && state == GameState.Playing, "he lands on it");
        Check(Mathf.Abs(bossAfter - bossAhead) < 1, $"the boss counter waited in the cave ({bossAhead:0} m before, {bossAfter:0} m after)");
        await Wait(0.5);
        Check(Mathf.Abs(boost - 1) < 0.01f && Mathf.Abs(sounds.MusicPitch - 1.08f) < 0.01f && cavePits.Count == 0 && caveRocks.Count == 0 && geyser is null,
              "normal speed and music again, and the cave is gone");
    }

    // ---------- 9. Crashing in a cave ----------

    /// <summary>
    /// A crash in a cave with a spare battery: the magic rebuild happens IN the cave, on its solid floor (the pit puffs
    /// away). With no spare: a normal game over, the score saved, and the next game is normal (no cave, solid ground).
    /// </summary>
    async Task CaveRebuildAndGameOver()
    {
        string name = FreshName("TCAVEBOOM");
        await StartVolcanoGame(name);
        Check(await GoIntoCave(1), "into the EASY cave");
        spareBatteries = 1;
        bestScore = 1_000_000;
        robot.BlinkTime = 0;
        var pit = new CavePit { Width = 220, Position = new Vector2(RobotX + 50, CaveFloorY) };
        AddChild(pit);
        cavePits.Add(pit);
        Check(await WaitUntil(() => robot.HotFeet, 0.5) && popups.Texts.Contains("HOT HOT HOT!"), "riding onto a lava pit: HOT HOT HOT!");
        Check(await WaitUntil(() => state == GameState.Rebuilding, 1.5) && InCave, "KABOOM, with a spare: the magic rebuild, still in the cave");
        Check(await WaitUntil(() => state == GameState.Playing, 5) && InCave && caveHardness == 1, "rebuilt in the cave");
        Check(robot.OnGround && robot.Position.Y == CaveFloorY && cavePits.Count == 0, "on the solid floor, and the pit puffed away");
        await Wait(0.5);
        Check(state == GameState.Playing && drawnWorldThings.Contains("cave"), "riding on in the cave");

        // No spare: game over, saved
        int games = database?.StatsFor(name).GamesPlayed ?? 0;
        int score = Score;
        spareBatteries = 0;
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.GameOver, "with no spare: game over, in the cave");
        Check((database?.StatsFor(name).GamesPlayed ?? 0) == games + 1 && LastRun(name, "score") == score, $"the game is saved (score {score})");
        await Wait(2);
        Check(hud.ResultsShowing, "the results card shows");
        PlayPressed();
        QuietRoad();
        await Wait(0.3);
        Check(state == GameState.Playing && !InCave && !CaveBusy && CurrentWorld.Name == "SUNNY HILLS" && lavaRocks.Count == 0 && cavePits.Count == 0 &&
              caveRocks.Count == 0 && geyser is null, "the next game is in Sunny Hills: no cave, no rocks");
        Check(robot.GroundY == GroundY && robot.Position.Y == GroundY && Mathf.Abs(boost - 1) < 0.01f && curtain.Closed == 0,
              "solid ground, normal speed, and the screen is open");
        var (height, _) = await MeasureJump();
        Check(Mathf.Abs(height - 212) < 15, $"a normal jump ({height:0} px)");
    }

    // ---------- 10. Bosses ----------

    /// <summary>
    /// Near a boss no holes come. The boss warning: the rocks join into one SOLID ROCK floor ("SOLID GROUND!"), the fight
    /// works like everywhere else, and after a win the World Tour goes on to Sunny Hills. Big Rusty wears his fire helmet.
    /// A fight that ends without a new world: the floor ends, and the rocks come back.
    /// </summary>
    async Task VolcanoBoss()
    {
        await StartVolcanoGame("TVolcanoBoss");
        robot.BlinkTime = 1e9f;
        volcanoSeconds = 40;
        nextHoleAt = 0;      // (a hole is due...)
        nextBossAt = Meters + 50; // (...but a boss is near)
        spawnCountdown = 0;
        await Wait(1.5);
        Check(holeLog.Count == 0 && lavaRocks.All(r => r.Hole == 0) && lavaRocks.Count > 1, "a boss is near: rocks come, but no holes");

        nextBossAt = Meters + 20;
        await NextFrame();
        await NextFrame();
        Check(hud.BannerText == "WARNING!  BOSS AHEAD!" && popups.Texts.Contains("SOLID GROUND!"), "WARNING! BOSS AHEAD!... SOLID GROUND!");
        Check(arena is not null && lavaRocks.Count == 1 && lavaBubbles.Count == 0 && RockUnder(robot.Position.X) == arena,
              "the rocks join into one big solid rock floor");
        Check(await WaitUntil(() => boss is not null, 4), $"the boss comes ({boss?.DisplayName})");
        await Wait(0.5);
        testMoveAxis = -1;
        await Wait(0.8);
        testMoveAxis = 1;
        await Wait(1.6);
        testMoveAxis = 0;
        Check(state == GameState.Playing && RockUnder(robot.Position.X) == arena && robot.Position.Y <= GroundY,
              $"riding left and right on solid rock (at x {robot.Position.X:0})");
        var (height, _) = await MeasureJump();
        Check(Mathf.Abs(height - 212) < 15, $"a normal jump in the fight ({height:0} px)");
        var theBoss = boss!;
        theBoss.LoseHealth(BossBase.MaxHealth);
        Check(await WaitUntil(() => theBoss.HasBlownUp, 6), "the boss blows up");
        Check(await WaitUntil(() => worldNumber == 7, 4) && CurrentWorld.Name == "SUNNY HILLS" && hud.BannerText == "YOU WENT ALL THE WAY AROUND!",
              $"then on to Sunny Hills: '{hud.BannerText}'");
        await Wait(1);
        Check(lavaRocks.Count == 0 && arena is null && robot.GroundY == GroundY && robot.OnGround && robot.Position.Y == GroundY && state == GameState.Playing,
              "solid ground in Sunny Hills");

        // Big Rusty's outfit here
        GoToTitle();
        var rusty = StartBossFight("TVolcRusty", world: Worlds.VolcanoIndex);
        await Wait(1);
        Check(rusty.Outfit == BossOutfit.FireHelmet && rusty.OutfitDrawn && arena is not null, "Big Rusty wears a red fire helmet on VOLCANO ISLAND (and the floor is solid)");

        // A fight that ends without a new world: the floor ends, and the rocks come back
        boss!.QueueFree();
        boss = null;
        bossWarningShown = false;
        nextBossAt = 1e9f;
        spawnCountdown = 0;
        Check(await WaitUntil(() => arena is null, 2) && await WaitUntil(() => lavaRocks.Count > 1, 4),
              "no new world: the solid floor ends a little way ahead, and the rocks come back");
    }

    // ---------- 11. Powers and the pause screen ----------

    /// <summary>
    /// MEGA BOLT-E strides over lava gaps and rocks sink faster under him, the MAGNET works, and the ROCKET's parachute
    /// lands on a fresh safe rock. Pausing freezes the sinking. In a cave WORLD WARP is greyed ("Find the way out first!"),
    /// and START while dropping in waits until he's in. Warping out of the Volcano: solid ground again.
    /// </summary>
    async Task VolcanoPowersAndPause()
    {
        await StartVolcanoGame("TVolcPowers");
        LetBoltECrash();

        // MEGA
        StartPowerUp(PowerUp.Mega);
        await Wait(1);
        OnlyLava();
        AddRock(-300, RobotX + 100);
        var b = AddRock(RobotX + 300, RobotX + 4000);
        Check(await WaitUntil(() => RockUnder(robot.Position.X) == b && robot.OnGround, 2) && state == GameState.Playing && !robot.HotFeet,
              "MEGA BOLT-E strides right over a lava gap (too big to fall in)");
        Check(await WaitUntil(() => b.SinkClock >= SinkDelay + 1, 3), "on the next rock");
        Check(b.Sunk > SinkSpeed * 1.3f, $"MEGA is heavy: it sinks {b.Sunk:0} px in 1 s (normally {SinkSpeed:0})");
        EndPowerUp(quiet: true);
        robot.BlinkTime = 0;

        // MAGNET
        OnlyLava();
        AddRock(-300, RobotX + 4000, steady: true);
        StartPowerUp(PowerUp.Magnet);
        int got = boltsCollected;
        AddBolt(robot.Position.X + 250, GroundY - 140);
        Check(await WaitUntil(() => boltsCollected > got, 2), "the MAGNET pulls a bolt in");
        EndPowerUp(quiet: true);

        // ROCKET: over the lava, and the parachute lands on a fresh safe rock
        StartPowerUp(PowerUp.Rocket);
        await Wait(1);
        OnlyLava(); // (nothing but lava under him!)
        await Wait(0.3);
        Check(robot.Rocketing && state == GameState.Playing, "the ROCKET flies over the lava");
        EndPowerUp();
        await NextFrame();
        await NextFrame();
        var pad = RockUnder(robot.Position.X);
        Check(robot.Parachuting && pad is { Steady: true } && pad.Width >= RescueLength, "the parachute pops open, and a fresh, wide, safe rock comes under him");
        Check(await WaitUntil(() => robot.OnGround, 5) && robot.Position.Y == GroundY && state == GameState.Playing && !robot.HotFeet &&
              RockUnder(robot.Position.X) is { Steady: true }, "he lands on it (it's still there, however long the parachute took)");
        OnlyLava();
        AddRock(-300, RobotX + 4000, steady: true); // (no new rocks come in this test: a long one to ride on)
        await Wait(0.5);
        Check(state == GameState.Playing, "riding on");

        // Pausing freezes the sinking
        OnlyLava();
        var c = AddRock(-300, RobotX + 3000);
        await Wait(SinkDelay + 0.3f);
        float sunk = c.Sunk;
        PauseGame();
        await Wait(0.5);
        Check(sunk > 0 && c.Sunk == sunk && paused, $"paused: the rock stops sinking ({sunk:0.0} px)");
        ResumeGame();
        await Wait(0.3);
        Check(c.Sunk > sunk, "back to the game: it sinks again");

        // In a cave, WORLD WARP is greyed
        robot.BlinkTime = 1e9f;
        Check(await GoIntoCave(1), "into a cave");
        PauseGame();
        await NextFrame();
        Check(pauseMenu.WarpBlocked == FindTheWayOut && pauseMenu.WarpGreyed && pauseMenu.NameLine == "Find the way out first!",
              $"WORLD WARP is greyed: '{pauseMenu.NameLine}'");
        pauseMenu.Row = WarpRow;
        pauseMenu.Picked = 0;
        PickPauseRow();
        Check(InCave && CurrentWorld.Name == "VOLCANO ISLAND", "...and picking it does nothing");
        ResumeGame();

        // START while dropping into a hole waits until he's in the cave
        GoToTitle();
        await StartVolcanoGame("TVolcPause");
        OnlyLava();
        AddRock(RobotX - 300, RobotX + 300, hole: 1);
        Check(await WaitUntil(() => CaveBusy, 1), "dropping into a hole");
        AskToPause();
        Check(!paused && pauseWanted, "START while he drops in: the pause waits...");
        Check(await WaitUntil(() => paused, 3) && InCave, "...and pauses once he's in the cave");
        ResumeGame();

        // Warping out of the Volcano: solid ground, normal speed
        GoToTitle();
        await StartVolcanoGame("TVolcWarp");
        PauseGame();
        pauseMenu.Row = WarpRow;
        pauseMenu.Picked = 0;
        PickPauseRow();
        Check(CurrentWorld.Name == "SUNNY HILLS" && lavaRocks.Count == 0 && lavaBubbles.Count == 0 && robot.GroundY == GroundY,
              "WORLD WARP to Sunny Hills: the rocks are gone");
        await Wait(0.5);
        Check(state == GameState.Playing && robot.OnGround && robot.Position.Y == GroundY && Mathf.Abs(boost - 1) < 0.01f, "riding on solid ground again");
    }

    // ---------- 12. Drawing ----------

    /// <summary>
    /// VOLCANO ISLAND and all 3 caves draw (with every danger and every look), Bolt-E is never upside-down here, the colors
    /// keep him easy to see, its postcard draws, and there are no engine errors.
    /// </summary>
    async Task VolcanoDraws()
    {
        int errors = SelfTestLogger.Errors;
        var volcano = Worlds.All[Worlds.VolcanoIndex];
        float far = Worlds.Brightness(volcano.FarHills), near = Worlds.Brightness(volcano.NearHills);
        Check(far >= 0.38f && near >= 0.38f && volcano.Dark && Worlds.Brightness(volcano.SkyTop) < 0.2f,
              $"the hills behind Bolt-E are bright enough ({far:0.00}, {near:0.00}; his body is 0.24), and it's a dark world with the glow");

        await StartVolcanoGame("TVolcDraw");
        robot.BlinkTime = 1e9f;
        testRideOverHoles = true;
        OnlyLava();
        AddRock(-300, RobotX + 400);
        var holes = new[]
        {
            AddRock(RobotX + 460, RobotX + 1060, hole: 1), AddRock(RobotX + 1180, RobotX + 1780, hole: 2), AddRock(RobotX + 1900, RobotX + 2500, hole: 3),
        };
        var warning = new LavaBubble { GapWidth = 120, Position = new Vector2(RobotX + 430, LavaSurfaceY) };
        var flying = new LavaBubble { GapWidth = 120, Position = new Vector2(RobotX + 1120, LavaSurfaceY) };
        foreach (var bubble in new[] { warning, flying })
        {
            AddChild(bubble);
            lavaBubbles.Add(bubble);
        }
        warning.StartWarning();
        flying.LaunchNow();
        eruptTime = 0;
        await Wait(0.3);
        var expected = new[] { "sun", "clouds", "volcano", "smoke", "eruption", "rock spires", "lava river", "embers", "night glow" };
        var missing = expected.Where(thing => !drawnWorldThings.Contains(thing)).ToList();
        Check(missing.Count == 0, $"VOLCANO ISLAND draws its {string.Join(", ", expected)}{(missing.Count > 0 ? " (missing: " + string.Join(", ", missing) + ")" : "")}");
        Check(holes.All(h => h.SignsDrawn > 0), "the holes draw, with their signs (1, 2 and 3 flames)");

        bool upsideDown = false;
        for (int i = 0; i < Looks.All.Length; i++)
        {
            Looks.Apply(robot, Looks.All[i]);
            await NextFrame();
            upsideDown |= robot.UpsideDown || robot.FlipMode;
        }
        Looks.Apply(robot, Looks.All[lookIndex]);
        Check(!upsideDown, $"all {Looks.All.Length} looks ride the rocks, never upside-down");

        // A crash into the lava: his pieces bounce on the lava, and he's rebuilt on a safe rock
        OnlyLava();
        robot.BlinkTime = 0;
        spareBatteries = 1;
        Check(await WaitUntil(() => state == GameState.Rebuilding, 2), "a crash into the lava (with a spare)");
        await Wait(0.8);
        Check(await WaitUntil(() => state == GameState.Playing, 5) && RockUnder(robot.Position.X) is { Steady: true } && robot.Position.Y == GroundY,
              "he's rebuilt on a fresh, safe rock");

        // The 3 caves, with every danger
        testRideOverHoles = false;
        for (int h = 1; h <= 3; h++)
        {
            GoToTitle();
            await StartVolcanoGame($"TVolcDraw{h}");
            robot.BlinkTime = 1e9f;
            Check(await GoIntoCave(h), $"into the {VolcanoArt.HardnessName(h)} cave");
            var pit = new CavePit { Width = 160, Position = new Vector2(700, CaveFloorY) };
            AddChild(pit);
            cavePits.Add(pit);
            var spikes = new CaveRock { Kind = CaveRockKind.Spikes, Position = new Vector2(880, CaveFloorY) };
            var stalactite = new CaveRock { Kind = CaveRockKind.Stalactite, Position = new Vector2(1000, CaveFloorY) };
            foreach (var rock in new[] { spikes, stalactite })
            {
                AddChild(rock);
                caveRocks.Add(rock);
            }
            stalactite.StartShaking();
            pendingBats.Add((TrackOf(1100), 1f));
            PlaceGeyser(TrackOf(1180));
            AddCrystalBolt(600, GroundY - 120);
            await Wait(0.4);
            var caveThings = new List<string> { "cave", "lavafalls", "crystals", "stalactites", "cave floor", "bats", "night glow" };
            if (h == 3) caveThings.Add("hard cave");
            var caveMissing = caveThings.Where(thing => !drawnWorldThings.Contains(thing)).ToList();
            Check(caveMissing.Count == 0 && enemies.Any(e => e.Kind == EnemyKind.FireBat) && !drawnWorldThings.Contains("lava river"),
                  $"the {VolcanoArt.HardnessName(h)} cave draws its {string.Join(", ", caveThings)}, a fire bat, a pit, spikes, a stalactite and the geyser" +
                  (caveMissing.Count > 0 ? " (missing: " + string.Join(", ", caveMissing) + ")" : ""));
            for (int i = 0; i < Looks.All.Length; i++)
            {
                Looks.Apply(robot, Looks.All[i]);
                await NextFrame();
                upsideDown |= robot.UpsideDown || robot.FlipMode;
            }
            Looks.Apply(robot, Looks.All[lookIndex]);
            Check(!upsideDown, $"every look draws in the {VolcanoArt.HardnessName(h)} cave, never upside-down");
        }

        GoToTitle();
        hud.ShowPostcards(7);
        await NextFrame();
        await NextFrame();
        Check(hud.Postcards.PicturesDrawn == 7, "VOLCANO ISLAND's own postcard draws (a volcano, lava and a rock)");
        Check(SelfTestLogger.Errors == errors, "no engine errors");
    }
}
