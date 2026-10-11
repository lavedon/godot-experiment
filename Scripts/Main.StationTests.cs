using Godot;

/// <summary>
/// Grown-up testing code for UPSIDE-DOWN STATION, the 6th world (more of the self-test: see Main.SelfTest.cs).
/// To run only these checks, add --selftest-station after --selftest.
/// </summary>
public partial class Main
{
    /// <summary>The tests that --selftest-station runs.</summary>
    static readonly string[] StationTestNames =
    {
        nameof(StationIsSixth), nameof(StartWorldPicker), nameof(FlipGravity), nameof(StationDangers), nameof(StationFairness),
        nameof(StationRebuild), nameof(StationBoss), nameof(StationPowers), nameof(StationResets), nameof(StationDraws),
        nameof(StationMusicLevel),
    };

    /// <summary>
    /// A new game that starts in UPSIDE-DOWN STATION (picked on the title screen), with nothing on the track yet.
    /// Waits until flipping is on. (The next games start in Sunny Hills again.)
    /// </summary>
    async Task StartStationGame(string name)
    {
        PlayAs(name);
        startWorld = Worlds.StationIndex;
        StartGame();
        startWorld = 0;
        QuietRoad();
        await WaitUntil(() => robot.FlipMode, 1);
    }

    /// <summary>Flips (like pressing jump) and waits until Bolt-E lands. Returns how long the trip took (seconds), or -1.</summary>
    async Task<float> FlipAndLand()
    {
        await NextFrame();
        if (!robot.FlipMode || !robot.OnGround) return -1;
        ulong start = Engine.GetProcessFrames();
        FlipPressed();
        if (robot.OnGround) return -1;
        if (!await WaitUntil(() => robot.OnGround, 2)) return -1;
        return (Engine.GetProcessFrames() - start) / 60f;
    }

    /// <summary>Puts a station danger on the track, "ahead" pixels in front of Bolt-E (see AddStationDanger).</summary>
    void PutDanger(StationDangerKind kind, StationSide side, float ahead, float width = 0)
    {
        if (width <= 0) width = kind switch
        {
            StationDangerKind.Spikes => Obstacle.SpikeHalfWidth * 2,
            StationDangerKind.Laser => Obstacle.LaserHalfWidth * 2,
            StationDangerKind.Junk => Obstacle.JunkHalfSize * 2,
            _ => 200,
        };
        float middle = distance + robot.Position.X - RobotX + ahead; // (a track position: see ScreenX)
        AddStationDanger(new StationDanger(kind, side, middle - width / 2, middle + width / 2, 0));
    }

    /// <summary>How many of UPSIDE-DOWN STATION's spikes, laser gates and junk are on the track.</summary>
    int StationObstacles => obstacles.Count(o => o.Kind is ObstacleKind.SpikeStrip or ObstacleKind.LaserGate or ObstacleKind.SpaceJunk);

    // ---------- 1. The 6th world ----------

    /// <summary>
    /// UPSIDE-DOWN STATION is the 6th world, after the Moon: Moon -> Station -> Sunny Hills is "all the way around". The
    /// results card's words are right for 6 and 7 worlds, and old saved numbers mean what they meant (the Moon look still
    /// needs the Moon).
    /// </summary>
    async Task StationIsSixth()
    {
        var station = Worlds.All[5];
        Check(Worlds.All.Length == 7 && station.Name == "UPSIDE-DOWN STATION" && Worlds.StationIndex == 5 && station.Scenery == Scenery.Station,
              $"the 6th world is {station.Name}");
        Check(station.Dark && Mathf.IsEqualApprox(station.MusicSpeed, 1.1f) && station.Gravity == 1 && station.RustyOutfit == BossOutfit.NeonGoggles,
              "it's a dark world (with the glow), its music is a bit faster (1.1), and Big Rusty wears neon goggles there");

        PlayAs("TStationSixth");
        StartGame();
        QuietRoad();
        ChangeWorld(4);
        ChangeWorld(5);
        Check(CurrentWorld == station && hud.BannerText == "WELCOME TO UPSIDE-DOWN STATION!" && hud.HintText == FlipTip && worldsReached == 6,
              $"after the Moon: '{hud.BannerText}', '{hud.HintText}' ({worldsReached} worlds)");
        await Wait(1.2);
        Check(robot.FlipMode && ceilingOpen == 0, "the ceiling slides shut, and flipping is on");
        ChangeWorld(6);
        Check(CurrentWorld.Name == "VOLCANO ISLAND" && hud.BannerText == "WELCOME TO VOLCANO ISLAND!" && worldsReached == 7,
              $"after the Station (now the 7th world comes next): '{hud.BannerText}' in {CurrentWorld.Name} ({worldsReached} worlds)");
        await Wait(1.2);
        Check(!robot.FlipMode && robot.CeilingY == Robot.NoCeiling && ceilingOpen == 1, "out of the Station: no ceiling, no flipping");

        Check(Worlds.WorldLine(5) == "You rode all the way to THE MOON!" && Worlds.WorldLine(6) == "You rode all the way to UPSIDE-DOWN STATION!" &&
              Worlds.WorldLine(8) == "You went all the way around the world!",
              $"the results card: 5 = '{Worlds.WorldLine(5)}', 6 = '{Worlds.WorldLine(6)}', 8 = '{Worlds.WorldLine(8)}'");
        float Wide(string text, int size) => ThemeDB.FallbackFont.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        float line = Wide(Worlds.WorldLine(6), 22), banner = Wide("WELCOME TO UPSIDE-DOWN STATION!", 60), tip = Wide(FlipTip, 28);
        Check(line <= 800 && banner <= 1280 && tip <= 1280, $"the words fit (results line {line:0} of 800 px, banner {banner:0} and tip {tip:0} of 1280 px)");

        // Old saved numbers keep their meaning: 5 is still the Moon, and the ASTRONAUT look still needs the Moon
        var astronaut = Looks.All.First(look => look.Id == "astronaut");
        string snowy = FreshName("TOLDSNOWY"), moon = FreshName("TOLDMOON");
        SeedRun(snowy, bolts: 0, world: 4);
        SeedRun(moon, bolts: 0, world: 5);
        GoToTitle(); // (players are picked on the menus)
        hud.SetPlayerName(snowy);
        await NextFrame();
        Check(!Unlocked(astronaut) && Goal(astronaut) == "Ride to THE MOON!", $"a player who reached Snowy Peaks: ASTRONAUT still says '{Goal(astronaut)}'");
        hud.SetPlayerName(moon);
        await NextFrame();
        await NextFrame();
        Check(Unlocked(astronaut) && hud.Postcards.Reached == 5 && hud.Postcards.QuestionMarksDrawn == 2,
              "a player who reached the Moon has ASTRONAUT, and 5 postcards (the Station's is still a '?')");
    }

    // ---------- 2. Picking the world to start in ----------

    /// <summary>
    /// DOWN on Bolt-E's controller (the D-pad or the stick, or the Down arrow or S) on the title screen steps through the
    /// worlds this player may start in: Sunny Hills, the worlds they reached, and always UPSIDE-DOWN STATION. Dad's DOWN
    /// does nothing, and Y starts the game again (like A, B and X). Starting in the Station: welcome, tip, flipping.
    /// A picked start never wins a look or a postcard, and never makes a saved record smaller.
    /// </summary>
    async Task StartWorldPicker()
    {
        try
        {
            string name = FreshName("TPICKER");
            hud.SetPlayerName(name);
            await Wait(0.2);
            var postcards = hud.Postcards;
            Check(startWorld == 0 && postcards.Picked == 0 && postcards.StartLine == "START: SUNNY HILLS" && postcards.HintDrawn,
                  $"a new player starts in Sunny Hills ('{postcards.StartLine}', and the 'DOWN = pick a world' tip)");

            await PressButton(JoyButton.DpadDown);
            Check(state == GameState.Title && startWorld == Worlds.StationIndex && postcards.StartLine == "START: UPSIDE-DOWN STATION",
                  $"D-pad DOWN: the Station, the brand-new world, is always allowed ('{postcards.StartLine}', still on the title screen)");
            Check(postcards.PickedCard.Position.X > 200, $"...and its postcard (the 6th) glows (at x {postcards.PickedCard.Position.X:0})");
            await PressButton(JoyButton.DpadDown);
            bool volcanoNext = startWorld == Worlds.VolcanoIndex; // (VOLCANO ISLAND, the newest world, is always allowed too)
            await PressButton(JoyButton.DpadDown);
            Check(volcanoNext && startWorld == 0, "DOWN again: VOLCANO ISLAND, and again: back to Sunny Hills (only those three for a new player)");
            await TapKey(Key.S);
            Check(state == GameState.Title && startWorld == Worlds.StationIndex, "S on the keyboard picks too (and doesn't start a game)");
            await SecondPadButton(JoyButton.DpadDown);
            await SecondPadButton(JoyButton.DpadDown);
            await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0.9f }, SelfTestSecondPad);
            await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0 }, SelfTestSecondPad);
            Check(state == GameState.Title && startWorld == Worlds.StationIndex, "Dad's controller's DOWN (and his stick) does nothing on the title screen");

            // The Down arrow picks too, and the stick: one push down is one step (wiggling a little more doesn't step again)
            await TapKey(Key.Down);
            bool arrowPicked = startWorld == Worlds.VolcanoIndex;
            await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0.7f });
            await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0.9f });
            await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 1f });
            await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0 });
            Check(state == GameState.Title && arrowPicked && startWorld == 0,
                  $"the Down arrow picks, and one push of the stick is one step (now world {startWorld})");
            await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0.4f });
            await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0 });
            await PressButton(JoyButton.DpadUp);
            Check(state == GameState.Title && startWorld == 0, "a small stick wobble and D-pad UP do nothing on the title screen");

            // It all fits in the strip, next to the title card
            await NextFrame();
            Check(postcards.DrawnRight <= postcards.Size.X, $"the postcards and the words stay inside their box (to {postcards.DrawnRight:0} of {postcards.Size.X:0})");
            CheckPostcardsBesideCard(hud.TitleCardRect, "title card");
            CheckCardFits(hud.TitleCardRect, "title card");

            // A player who reached Night City may start in Sunny Hills, Candy Land, Night City or the Station
            string farther = FreshName("TPICKFAR");
            SeedRun(farther, bolts: 0, world: 3);
            hud.SetPlayerName(farther);
            startWorld = 0;
            ShowStartWorld();
            await NextFrame();
            var steps = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                await PressButton(JoyButton.DpadDown);
                steps.Add(startWorld);
            }
            Check(string.Join(" ", steps) == "1 2 5 6 0", $"a player who reached Night City steps through worlds {string.Join(" ", steps)}");

            // Y starts the game again, like A, B and X (it used to pick the world: that got mixed up with A)
            await PressButton(JoyButton.Y);
            Check(state == GameState.Playing, "Y on Bolt-E's controller starts a game from the title screen again");
            GoToTitle();
            await Wait(0.2);

            // Starting in the Station: the welcome, the tip, the ceiling shut, and flipping right away
            hud.SetPlayerName(name);
            await NextFrame();
            startWorld = Worlds.StationIndex;
            int looksBefore = Looks.All.Count(Unlocked);
            StartGame();
            QuietRoad();
            Check(CurrentWorld.Name == "UPSIDE-DOWN STATION" && hud.BannerText == "WELCOME TO UPSIDE-DOWN STATION!" && hud.HintText == FlipTip,
                  $"the game starts in the Station: '{hud.BannerText}', '{hud.HintText}'");
            Check(await WaitUntil(() => robot.FlipMode, 0.5) && ceilingOpen == 0 && robot.CeilingY == CeilingSurfaceY, "the ceiling is shut, and flipping is on");
            Check(Mathf.Abs(sounds.MusicPitch - 1.1f) < 0.01f, $"the Station's music speed ({sounds.MusicPitch:0.00})");
            spareBatteries = 0;
            bestScore = 1_000_000;
            Check(Bonk(null) && state == GameState.GameOver, "game over");
            Check(AskDatabase(selfTestDbPath, "SELECT world_reached FROM runs WHERE player_name = $name COLLATE NOCASE ORDER BY id DESC LIMIT 1;",
                              ("$name", name)) == 1 && (database?.FarthestWorld(name) ?? 0) == 1,
                  "just starting in the Station saves 1 world: its postcard isn't won");
            await Wait(2);
            Check(hud.ResultsWorldLine == "World: UPSIDE-DOWN STATION" && postcards.Reached == 1 && postcards.RibbonsDrawn == 0 && unlockQueue.Count == 0,
                  $"the results card says '{hud.ResultsWorldLine}', with no new postcards or looks");
            Check(Looks.All.Count(Unlocked) == looksBefore, "no look was won");

            // Riding on from the Station by beating bosses: VOLCANO ISLAND, Sunny Hills, then Candy Land (that one he really
            // rode into)
            StartGame(); // (still picked: the Station)
            QuietRoad();
            ChangeWorld(6); // (VOLCANO ISLAND)
            ChangeWorld(7); // (Sunny Hills)
            Check(worldsReached == 1, $"beating bosses in the Station rides into VOLCANO ISLAND, then Sunny Hills ({worldsReached} world)");
            ChangeWorld(8);
            Check(worldsReached == 2, $"...and the next one into Candy Land: really reached, so 2 worlds ({worldsReached})");
            spareBatteries = 0;
            Check(Bonk(null) && (database?.FarthestWorld(name) ?? 0) == 2, $"saved: 2 worlds ({database?.FarthestWorld(name)})");
            await Wait(2);
            Check(hud.ResultsWorldLine == "You rode from UPSIDE-DOWN STATION to CANDY LAND!", $"'{hud.ResultsWorldLine}'");

            // From the Moon (reached before) into the Station: that's riding there for real, so its postcard is won
            string moon = FreshName("TPICKMOON");
            SeedRun(moon, bolts: 0, world: 5);
            GoToTitle();
            hud.SetPlayerName(moon);
            startWorld = 4;
            ShowStartWorld();
            StartGame();
            QuietRoad();
            Check(CurrentWorld.Name == "THE MOON" && robot.GravityScale < 1, "a game picked to start on the Moon (floaty!)");
            ChangeWorld(5);
            Check(worldsReached == 6, $"riding from the Moon into the Station: 6 worlds ({worldsReached})");

            // A picked start never makes a saved record smaller
            string champ = FreshName("TPICKCHAMP");
            SeedRun(champ, bolts: 0, world: 4);
            GoToTitle();
            hud.SetPlayerName(champ);
            startWorld = Worlds.StationIndex;
            StartGame();
            QuietRoad();
            spareBatteries = 0;
            Check(Bonk(null) && (database?.FarthestWorld(champ) ?? 0) == 4, $"a quick game from the Station keeps the record of 4 worlds ({database?.FarthestWorld(champ)})");
            await Wait(2);
            Check(postcards.Reached == 4 && postcards.RibbonsDrawn == 0, $"...and the postcards still show 4 worlds, none NEW ({postcards.Reached})");
        }
        finally
        {
            startWorld = 0;
        }
    }

    // ---------- 3. Flipping gravity ----------

    /// <summary>
    /// In the Station, jump FLIPS GRAVITY: Bolt-E travels up and lands on the ceiling in about 0.37 seconds (with a
    /// half-roll), rides there upside-down, and flips back down. One flip in the air works, a second one doesn't, and
    /// holding the button doesn't repeat.
    /// </summary>
    async Task FlipGravity()
    {
        await StartStationGame("TFlip");
        robot.BlinkTime = 1e9f;
        Check(robot.FlipMode && robot.OnGround && robot.Position.Y == GroundY && !robot.UpsideDown, "flipping is on, Bolt-E rides on the floor");
        Check(hud.GravityArrow.Visible && hud.GravityArrow.Angle == 0, "the gravity arrow in the corner points DOWN");

        // The real button (held down)
        int whooshes = sounds.TimesPlayed(Sfx.FlipUp);
        await Press("jump");
        Check(robot.UpsideDown && !robot.OnGround && sounds.TimesPlayed(Sfx.FlipUp) == whooshes + 1, "jump: gravity flips UP (with a whoosh)");
        Check(robot.RollAngle > 0.05f && robot.RollAngle < Mathf.Pi - 0.05f, $"...and he does a half-roll ({robot.RollAngle:0.00} of {Mathf.Pi:0.00})");
        Check(await WaitUntil(() => robot.OnGround, 1) && robot.Position.Y == CeilingSurfaceY && robot.UpsideDown,
              $"he lands on the ceiling (feet at y {robot.Position.Y:0})");
        await Wait(0.5); // (still holding the button)
        Check(robot.OnGround && robot.UpsideDown && robot.RollAngle == Mathf.Pi && robot.Position.Y == CeilingSurfaceY,
              "he rides along the ceiling upside-down (holding the button doesn't flip him back)");
        var box = robot.Hitbox;
        Check(box.Position.Y >= CeilingSurfaceY && box.End.Y < CorridorMiddle, $"his bump box hangs under the ceiling (y {box.Position.Y:0} to {box.End.Y:0})");
        Check(hud.GravityArrow.Angle == Mathf.Pi, "the gravity arrow points UP");
        await Release("jump");

        // The trip, measured both ways
        float down = await FlipAndLand();
        Check(down > 0.3f && down < 0.45f && robot.Position.Y == GroundY && !robot.UpsideDown && robot.RollAngle == 0,
              $"flip back down: {down:0.00} seconds across, right way up on the floor");
        float up = await FlipAndLand();
        GD.Print($"  (UPSIDE-DOWN STATION: corridor {GroundY - CeilingSurfaceY:0} px, flip up {up:0.000} s, flip down {down:0.000} s)");
        Check(up > 0.3f && up < 0.45f && robot.Position.Y == CeilingSurfaceY, $"flip up: {up:0.00} seconds across");
        await FlipAndLand();

        // One flip in the air (a second chance), but not two
        await NextFrame();
        FlipPressed();
        await Wait(0.1);
        Check(!robot.OnGround && robot.UpsideDown && robot.FlipsLeft == 1, "flipped up, in the air");
        FlipPressed();
        Check(!robot.UpsideDown && robot.LastFlipInAir && robot.FlipsLeft == 0, "a flip in the air turns him back (the second chance)");
        await NextFrame();
        Check(!robot.SwapGravity() && !robot.UpsideDown, "a second flip in the air doesn't work");
        Check(await WaitUntil(() => robot.OnGround, 1) && robot.Position.Y == GroundY && robot.FlipsLeft == Robot.MaxFlips,
              "he lands back on the floor, and touching it gives his flips back");
        Check(sounds.TimesPlayed(Sfx.FlipThud) >= 4, "every landing goes 'tunk!'");
    }

    // ---------- 4. Dangers ----------

    /// <summary>
    /// A ceiling spike crashes Bolt-E on the ceiling, but not on the floor. A floor gap makes a floor rider fall out into
    /// space (then KABOOM). Space junk in the middle crashes him in the middle of a flip, but not riding by.
    /// </summary>
    async Task StationDangers()
    {
        await StartStationGame("TDangers");
        LetBoltECrash();
        PutDanger(StationDangerKind.Spikes, StationSide.Ceiling, 250);
        PutDanger(StationDangerKind.Laser, StationSide.Ceiling, 450);
        PutDanger(StationDangerKind.Gap, StationSide.Ceiling, 650);
        PutDanger(StationDangerKind.Junk, StationSide.Middle, 850);
        await Wait(3.6);
        Check(state == GameState.Playing && StationObstacles == 0 && stationGaps.Count == 0,
              "on the floor: a ceiling spike, a ceiling laser, a ceiling gap and space junk all go by safely");

        // On the ceiling, a ceiling spike: crash (no spare, not blinking: game over)
        Check(await FlipAndLand() > 0 && robot.UpsideDown, "up on the ceiling");
        PutDanger(StationDangerKind.Spikes, StationSide.Ceiling, 250);
        Check(await WaitUntil(() => state != GameState.Playing, 2) && state == GameState.GameOver && robot.Crashed,
              "on the ceiling, a ceiling spike: KABOOM! (game over)");
        Check(!robot.UpsideDown, "his pieces fall down (gravity is normal again)");

        // A floor gap under a floor rider: WHOOOA... KABOOM
        GoToTitle();
        await StartStationGame("TDangers2");
        LetBoltECrash();
        PutDanger(StationDangerKind.Gap, StationSide.Floor, 300, 200);
        Check(await WaitUntil(() => robot.FallingIntoSpace, 2) && popups.Texts.Contains("WHOOOA!") && sounds.IsPlaying(Sfx.SpaceFall),
              "riding onto a floor gap: he falls out into space (WHOOOA!)");
        float fallFrom = robot.Position.Y;
        await Wait(0.2);
        Check(robot.Position.Y > fallFrom + 20 && state == GameState.Playing, $"...down through the gap ({robot.Position.Y - fallFrom:0} px)");
        Check(await WaitUntil(() => state == GameState.GameOver, 1) && robot.Crashed, "...then KABOOM");

        // Space junk in the middle crashes him while he flips through it
        GoToTitle();
        await StartStationGame("TDangers3");
        LetBoltECrash();
        PutDanger(StationDangerKind.Junk, StationSide.Middle, 120);
        FlipPressed();
        Check(await WaitUntil(() => state != GameState.Playing, 1) && state == GameState.GameOver, "flipping right into space junk: KABOOM");
    }

    // ---------- 5. Fair patterns ----------

    /// <summary>
    /// Thousands of planned patterns (always the same dice): a danger is never on the floor and the ceiling at the same
    /// spot, each pattern has dangers on one side only (one flip or none), there's always time to react AND flip between
    /// sides (with no junk in the way), the first 20 seconds are easy, and junk only comes later. Then a real ride checks
    /// the dangers really go on the track that way.
    /// </summary>
    async Task StationFairness()
    {
        await NextFrame();
        var test = new StationPlanner();
        var dice = new Random(2026);
        var all = new List<(StationDanger Danger, float Speed, float Seconds, string Pattern)>();
        var one = new List<StationDanger>();
        float start = 1000, seconds = 0, speed = StartSpeed;
        test.Reset(0);
        bool easyOk = true, junkOk = true, oneSideOk = true;
        float easyRoom = float.MaxValue;
        float lastSideEnd = 0;
        for (int i = 0; i < 4000; i++)
        {
            speed = Mathf.Min(MaxSpeed, StartSpeed + SpeedUpPerSecond * seconds);
            float end = test.Plan(start, speed, seconds, dice, one);
            foreach (var d in one) all.Add((d, speed, seconds, test.LastPattern));
            var sides = one.Where(d => d.Side != StationSide.Middle).Select(d => d.Side).Distinct().Count();
            oneSideOk &= sides <= 1;
            if (seconds < StationPlanner.EasySeconds)
            {
                easyOk &= one.Count == 1 && one[0].Side != StationSide.Middle && test.LastPattern == "easy";
                if (one.Count == 1) easyRoom = Mathf.Min(easyRoom, (one[0].Left - lastSideEnd) / speed);
            }
            if (seconds < StationPlanner.JunkFromSeconds) junkOk &= one.All(d => d.Side != StationSide.Middle);
            if (one.Any(d => d.Side != StationSide.Middle)) lastSideEnd = one.Last(d => d.Side != StationSide.Middle).Right;
            float next = end + speed * (0.2f + 0.3f * dice.NextSingle());
            seconds += (next - start) / speed;
            start = next;
        }
        Check(all.Count > 4000 && seconds > 200, $"{all.Count} dangers planned, over {seconds:0} seconds of riding");
        Check(oneSideOk, "every pattern has its floor-or-ceiling dangers on ONE side (one flip, or none, gets through it)");
        Check(easyOk && easyRoom >= 1.5f, $"the first {StationPlanner.EasySeconds:0} seconds: one danger at a time, at least {easyRoom:0.0} seconds apart");
        Check(junkOk && all.Any(x => x.Danger.Kind == StationDangerKind.Junk), $"space junk only after {StationPlanner.JunkFromSeconds:0} seconds (and then it comes)");
        Check(all.Select(x => x.Danger.Kind).Distinct().Count() == 4 && all.Select(x => x.Pattern).Distinct().Count() >= 4,
              $"all 4 kinds of danger, in {string.Join(", ", all.Select(x => x.Pattern).Distinct())} patterns");

        // Never on both sides at the same spot, and always time to switch sides
        var sorted = all.OrderBy(x => x.Danger.Left).ToList();
        float floorEnd = float.MinValue, ceilingEnd = float.MinValue;
        int bothSides = 0, switches = 0;
        float shortest = float.MaxValue;
        (StationDanger Danger, float Speed, float Seconds, string Pattern)? previous = null;
        float junkEnd = float.MinValue; // the end of the last junk since the previous floor-or-ceiling danger
        float longestClear = 0;         // the longest stretch with no junk since then
        foreach (var x in sorted)
        {
            var d = x.Danger;
            if (d.Side == StationSide.Middle)
            {
                if (previous is { } p) longestClear = Mathf.Max(longestClear, d.Left - Mathf.Max(p.Danger.Right, junkEnd));
                junkEnd = Mathf.Max(junkEnd, d.Right);
                continue;
            }
            if (d.Side == StationSide.Floor && ceilingEnd > d.Left) bothSides++;
            if (d.Side == StationSide.Ceiling && floorEnd > d.Left) bothSides++;
            if (previous is { } before && before.Danger.Side != d.Side)
            {
                // From being past the first danger (his whole body) to reaching the next one: the longest junk-free stretch
                float clear = Mathf.Max(longestClear, d.Left - Mathf.Max(before.Danger.Right, junkEnd));
                shortest = Mathf.Min(shortest, (clear - 2 * StationPlanner.HalfBody) / x.Speed);
                switches++;
            }
            if (d.Side == StationSide.Floor) floorEnd = Mathf.Max(floorEnd, d.Right);
            else ceilingEnd = Mathf.Max(ceilingEnd, d.Right);
            previous = x;
            junkEnd = float.MinValue;
            longestClear = 0;
        }
        GD.Print($"  (UPSIDE-DOWN STATION: the shortest time between needing one side and the other is {shortest:0.00} s)");
        Check(bothSides == 0, $"never a danger on the floor and the ceiling at the same spot ({bothSides} times)");
        Check(switches > 1000 && shortest >= StationPlanner.ReactionSeconds,
              $"{switches} side switches, each with at least {shortest:0.00} seconds to react and flip (at least {StationPlanner.ReactionSeconds})");

        // A real ride: the dangers on the track are where the planner put them, never on both sides at once
        await StartStationGame("TFair");
        robot.BlinkTime = 1e9f;
        spawnCountdown = 0;
        await Wait(3);
        var floorThings = obstacles.Where(o => o.Kind is ObstacleKind.SpikeStrip or ObstacleKind.LaserGate && !o.Hanging).Select(o => (o.Hitbox.Position.X, o.Hitbox.End.X))
            .Concat(stationGaps.Where(g => !g.Ceiling).Select(g => (g.Left, g.Right))).ToList();
        var ceilingThings = obstacles.Where(o => o.Kind is ObstacleKind.SpikeStrip or ObstacleKind.LaserGate && o.Hanging).Select(o => (o.Hitbox.Position.X, o.Hitbox.End.X))
            .Concat(stationGaps.Where(g => g.Ceiling).Select(g => (g.Left, g.Right))).ToList();
        bool overlap = floorThings.Any(f => ceilingThings.Any(c => f.Item1 < c.Item2 && c.Item1 < f.Item2));
        Check(floorThings.Count + ceilingThings.Count >= 2 && !overlap && planner.LastPattern == "easy",
              $"a real ride: {floorThings.Count} floor and {ceilingThings.Count} ceiling dangers, never at the same spot, easy patterns first");
    }

    // ---------- 6. The magic rebuild ----------

    /// <summary>
    /// A crash on the ceiling with a spare battery: KABOOM (his pieces fall to the floor), and he's rebuilt ON THE FLOOR,
    /// the right way up, with normal gravity, and the gap under him is gone.
    /// </summary>
    async Task StationRebuild()
    {
        await StartStationGame("TStationRebuild");
        Check(await FlipAndLand() > 0 && robot.UpsideDown, "up on the ceiling");
        PutDanger(StationDangerKind.Gap, StationSide.Floor, 0, 220); // (a gap in the floor right under him)
        spareBatteries = 1;
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.Rebuilding && robot.Crashed && robot.HasPiece("head"), "a crash on the ceiling: KABOOM, in pieces");
        Check(!robot.UpsideDown && robot.RollAngle == 0, "gravity is normal again, so his pieces fall down to the floor");
        Check(await WaitUntil(() => state == GameState.Playing, 5), "the magic rebuild");
        Check(robot.Position.Y == GroundY && robot.OnGround && !robot.UpsideDown && robot.FlipMode,
              $"he's rebuilt on the floor, the right way up (feet at y {robot.Position.Y:0}), and he can flip again");
        Check(stationGaps.Count == 0, "the gap under him puffed away");
        robot.BlinkTime = 0;
        await Wait(1);
        Check(state == GameState.Playing && !robot.FallingIntoSpace, "...so he doesn't fall through it");
        Check(await FlipAndLand() > 0 && robot.UpsideDown, "and flips up again");
    }

    // ---------- 7. Boss fights in the station ----------

    /// <summary>
    /// The boss warning in the Station: GRAVITY ON! Bolt-E drops to the floor, the station's dangers puff away and the
    /// ceiling slides open. The fight is like everywhere else (normal jumps), and after a win the next world is Sunny Hills.
    /// A fight that ends without a new world: the ceiling comes back, and so does flipping.
    /// </summary>
    async Task StationBoss()
    {
        await StartStationGame("TStationBoss");
        robot.BlinkTime = 1e9f;
        Check(await FlipAndLand() > 0 && robot.UpsideDown, "up on the ceiling");
        PutDanger(StationDangerKind.Spikes, StationSide.Ceiling, 700);
        PutDanger(StationDangerKind.Gap, StationSide.Floor, 900);
        nextBossAt = Meters + 20; // (the warning comes right away)
        await NextFrame();
        await NextFrame();
        Check(hud.BannerText == "WARNING!  BOSS AHEAD!" && popups.Texts.Contains("GRAVITY ON!"), "WARNING! BOSS AHEAD!... GRAVITY ON!");
        Check(!robot.FlipMode && !robot.UpsideDown && !hud.GravityArrow.Visible, "gravity points down again (no more flipping)");
        Check(StationObstacles == 0 && stationGaps.Count == 0, "the station's dangers puff away");
        Check(await WaitUntil(() => robot.OnGround, 1.5) && robot.Position.Y == GroundY, "Bolt-E drops to the floor");
        Check(await WaitUntil(() => ceilingOpen >= 1, 1.5) && robot.CeilingY == Robot.NoCeiling, "the ceiling slides open, off the screen");

        Check(await WaitUntil(() => boss is not null, 4), $"the boss comes ({boss?.DisplayName})");
        await Wait(0.5);
        var (height, _) = await MeasureJump();
        Check(Mathf.Abs(height - 212) < 15 && !robot.UpsideDown, $"a normal jump in the fight ({height:0} px)");

        var theBoss = boss!;
        theBoss.LoseHealth(BossBase.MaxHealth);
        Check(await WaitUntil(() => theBoss.HasBlownUp, 6), "the boss blows up");
        Check(await WaitUntil(() => worldNumber == 6, 4) && CurrentWorld.Name == "VOLCANO ISLAND" && hud.BannerText == "WELCOME TO VOLCANO ISLAND!",
              $"then on to the next world: {CurrentWorld.Name} ('{hud.BannerText}')");
        await Wait(1);
        Check(!robot.FlipMode && robot.GravityScale == 1 && robot.CeilingY == Robot.NoCeiling, "with normal gravity, and no ceiling");

        // A fight that ends without a new world (it can't really happen, but just in case): the ceiling comes back
        GoToTitle();
        await StartStationGame("TStationBoss2");
        nextBossAt = Meters + 20;
        Check(await WaitUntil(() => boss is not null, 4) && !robot.FlipMode, "another boss comes");
        boss!.QueueFree();
        boss = null;
        bossWarningShown = false;
        nextBossAt = 1e9f;
        Check(await WaitUntil(() => robot.FlipMode, CeilingBackSeconds + 2) && ceilingOpen == 0, "no new world: the ceiling slides shut again, and flipping is back");
    }

    // ---------- 8. Powers ----------

    /// <summary>
    /// The ? box powers in the Station: MEGA BOLT-E fits in the corridor (on the floor and on the ceiling), smashes junk
    /// and strides over gaps. The MAGNET pulls bolts in upside-down. The ROCKET flies through the middle of the corridor,
    /// and the parachute floats down to the floor. Nothing ever goes through the ceiling.
    /// </summary>
    async Task StationPowers()
    {
        await StartStationGame("TStationPowers");
        LetBoltECrash();

        // MEGA
        StartPowerUp(PowerUp.Mega);
        await Wait(1);
        var floorBox = robot.Hitbox;
        Check(Mathf.IsEqualApprox(robot.Size, PowerUps.MegaSize) && floorBox.Position.Y > CeilingSurfaceY, $"MEGA BOLT-E on the floor fits (top at y {floorBox.Position.Y:0})");
        float megaUp = await FlipAndLand();
        var ceilingBox = robot.Hitbox;
        float tall = 130 * robot.Size;
        GD.Print($"  (UPSIDE-DOWN STATION: MEGA BOLT-E is {tall:0} px tall in the {GroundY - CeilingSurfaceY:0} px corridor; his flip takes {megaUp:0.000} s)");
        Check(megaUp > 0 && robot.UpsideDown && ceilingBox.Position.Y >= CeilingSurfaceY && ceilingBox.End.Y < GroundY && tall < GroundY - CeilingSurfaceY,
              $"MEGA flips up and fits on the ceiling too (y {ceilingBox.Position.Y:0} to {ceilingBox.End.Y:0})");
        PutDanger(StationDangerKind.Junk, StationSide.Middle, 200);
        var junk = obstacles.Last();
        Check(await WaitUntil(() => junk.Smashed, 2) && state == GameState.Playing, "MEGA smashes space junk (SMASH!)");
        await FlipAndLand();
        PutDanger(StationDangerKind.Gap, StationSide.Floor, 150, 200);
        await Wait(1);
        Check(state == GameState.Playing && !robot.FallingIntoSpace, "MEGA strides right over a gap (too big to fall in)");
        EndPowerUp(quiet: true);
        PuffAway(stationGaps, _ => true);

        // MAGNET, upside-down
        Check(await FlipAndLand() > 0 && robot.UpsideDown, "up on the ceiling again");
        StartPowerUp(PowerUp.Magnet);
        await NextFrame();
        await NextFrame();
        Check(roulette.Position.Y > robot.Position.Y, "the slot machine is 'over his head': under him, on the ceiling");
        int got = boltsCollected;
        AddBolt(robot.Position.X + 250, CorridorMiddle + 60);
        Check(await WaitUntil(() => boltsCollected > got, 2), "the magnet pulls a bolt up to him");
        EndPowerUp(quiet: true);

        // ROCKET, from the ceiling
        StartPowerUp(PowerUp.Rocket);
        float highest = float.MaxValue, lowest = 0;
        for (int i = 0; i < 60; i++)
        {
            await NextFrame();
            highest = Mathf.Min(highest, robot.Hitbox.Position.Y);
            lowest = Mathf.Max(lowest, robot.Hitbox.End.Y);
        }
        Check(robot.Rocketing && !robot.UpsideDown && Mathf.Abs(robot.Position.Y - RocketFeetY) < 2,
              $"the ROCKET turns him the right way up and flies through the middle of the corridor (feet at y {robot.Position.Y:0})");
        Check(highest >= CeilingSurfaceY && lowest <= GroundY, $"...never through the ceiling or the floor (y {highest:0} to {lowest:0})");
        EndPowerUp();
        Check(robot.Parachuting && await WaitUntil(() => robot.OnGround, 4) && robot.Position.Y == GroundY, "the parachute floats down to the floor");
        await Wait(0.3);
        Check(await FlipAndLand() > 0, "and he can flip again");
    }

    // ---------- 9. Everything goes back to normal ----------

    /// <summary>
    /// Going to the title screen or a game over while upside-down: gravity is normal again, Bolt-E is drawn the right way
    /// up, and the next game in Sunny Hills has no ceiling (and jump jumps).
    /// </summary>
    async Task StationResets()
    {
        await StartStationGame("TStationReset");
        Check(await FlipAndLand() > 0 && robot.UpsideDown, "up on the ceiling");
        GoToTitle();
        Check(!robot.UpsideDown && robot.RollAngle == 0 && !robot.FlipMode && robot.CeilingY == Robot.NoCeiling && ceilingOpen == 1 && CurrentWorld.Name == "SUNNY HILLS",
              "the title screen: right way up, normal gravity, no ceiling, Sunny Hills");

        await StartStationGame("TStationReset");
        Check(await FlipAndLand() > 0 && robot.UpsideDown, "up on the ceiling again");
        LetBoltECrash();
        Check(Bonk(null) && state == GameState.GameOver && !robot.UpsideDown && robot.RollAngle == 0, "game over: his pieces are the right way up");
        await Wait(2);
        PlayPressed(); // (play again: the start world is Sunny Hills again)
        QuietRoad();
        await Wait(0.5);
        Check(CurrentWorld.Name == "SUNNY HILLS" && !robot.FlipMode && robot.CeilingY == Robot.NoCeiling && ceilingOpen == 1,
              $"the next game is in Sunny Hills, with no ceiling ({CurrentWorld.Name})");
        Check(!drawnWorldThings.Contains("ceiling") && !drawnWorldThings.Contains("station") && !hud.GravityArrow.Visible, "nothing of the station is drawn");
        var (height, _) = await MeasureJump();
        Check(Mathf.Abs(height - 212) < 15 && !robot.UpsideDown, $"jump jumps ({height:0} px)");
        Check(Engine.TimeScale == 1, "time runs at normal speed");
    }

    // ---------- 10. Drawing ----------

    /// <summary>
    /// The Station draws with every look and robot, the right way up, upside-down and in the middle of the half-roll, with
    /// the glow, every danger (smashed too), the gravity arrow and its postcard, and no engine errors. It keeps Bolt-E
    /// easy to see: its walls are bright enough, and it's a dark world (so he glows).
    /// </summary>
    async Task StationDraws()
    {
        int errors = SelfTestLogger.Errors;
        var station = Worlds.All[Worlds.StationIndex];
        float wall = Worlds.Brightness(station.FarHills), trim = Worlds.Brightness(station.NearHills);
        Check(wall >= 0.38f && trim >= 0.38f && station.Dark && Worlds.Brightness(station.SkyTop) < 0.2f,
              $"the walls behind Bolt-E are bright enough ({wall:0.00}, {trim:0.00}; his body is 0.24), and it's a dark world with the glow");

        await StartStationGame("TStationDraw");
        robot.BlinkTime = 1e9f;
        PutDanger(StationDangerKind.Spikes, StationSide.Floor, 300);
        PutDanger(StationDangerKind.Spikes, StationSide.Ceiling, 420);
        PutDanger(StationDangerKind.Laser, StationSide.Floor, 560);
        PutDanger(StationDangerKind.Laser, StationSide.Ceiling, 680);
        PutDanger(StationDangerKind.Gap, StationSide.Floor, 820);
        PutDanger(StationDangerKind.Gap, StationSide.Ceiling, 1000);
        PutDanger(StationDangerKind.Junk, StationSide.Middle, 900);
        await Wait(0.3);
        var expected = new[] { "station", "ringed planet", "portholes", "arrow signs", "hazard stripes", "ceiling", "floor rail", "night glow" };
        var missing = expected.Where(thing => !drawnWorldThings.Contains(thing)).ToList();
        Check(missing.Count == 0, $"the Station draws its {string.Join(", ", expected)}{(missing.Count > 0 ? " (missing: " + string.Join(", ", missing) + ")" : "")}");

        // Every look, right way up, upside-down and mid-roll
        int drawn = 0;
        for (int i = 0; i < Looks.All.Length; i++)
        {
            Looks.Apply(robot, Looks.All[i]);
            await NextFrame();
            FlipPressed();
            await NextFrame(); // (mid-roll)
            await WaitUntil(() => robot.OnGround, 1);
            await NextFrame();
            if (robot.UpsideDown) drawn++;
            FlipPressed();
            await WaitUntil(() => robot.OnGround, 1);
        }
        Looks.Apply(robot, Looks.All[lookIndex]);
        Check(drawn == Looks.All.Length, $"all {Looks.All.Length} looks (COZMOS and VECKTOR too) draw upside-down and mid-roll");

        // MEGA smashes a hanging spike: its pieces draw too, and the robot in pieces on the ceiling
        StartPowerUp(PowerUp.Mega);
        await Wait(0.6);
        await FlipAndLand();
        PutDanger(StationDangerKind.Spikes, StationSide.Ceiling, 200);
        var spikes = obstacles.Last();
        Check(await WaitUntil(() => spikes.Smashed, 2), "MEGA smashes a ceiling spike");
        await Wait(0.3);
        EndPowerUp(quiet: true);
        robot.BlinkTime = 0;
        spareBatteries = 1;
        Check(Bonk(null) && state == GameState.Rebuilding, "a crash on the ceiling");
        await Wait(0.8);

        GoToTitle();
        hud.ShowPostcards(6);
        await NextFrame();
        await NextFrame();
        Check(hud.Postcards.PicturesDrawn == 6, "the Station's own postcard draws (a ringed planet, a magenta ceiling and a cyan floor)");
        Check(SelfTestLogger.Errors == errors, "no engine errors");
    }
}
