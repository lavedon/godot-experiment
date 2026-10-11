using Godot;

/// <summary>
/// Grown-up testing code for the PAUSE screen and WORLD WARP (more of the self-test: see Main.SelfTest.cs).
/// To run only these checks, add --selftest-pause after --selftest.
/// </summary>
public partial class Main
{
    /// <summary>The tests that --selftest-pause runs.</summary>
    static readonly string[] PauseTestNames =
    {
        nameof(PauseFreezes), nameof(PauseMenuNav), nameof(WarpToStation), nameof(NoWarpInBossFight), nameof(WarpSavesFairly),
        nameof(QuitToTitleSaves), nameof(PauseSafety), nameof(PauseDraws),
    };

    /// <summary>A new game for player "name" (loaded, like picking them on the title screen), with nothing on the road.</summary>
    async Task StartQuietGame(string name)
    {
        hud.SetPlayerName(name);
        await NextFrame();
        StartGame();
        QuietRoad();
        robot.BlinkTime = 1e9f; // (nothing can crash him, unless a test lets it)
    }

    /// <summary>Pauses with START on Bolt-E's controller, goes DOWN to WORLD WARP, steps LEFT / RIGHT, and presses A.</summary>
    async Task WarpWithButtons(int steps)
    {
        await PressButton(JoyButton.Start);
        await PressButton(JoyButton.DpadDown);
        for (int i = 0; i < Math.Abs(steps); i++) await PressButton(steps > 0 ? JoyButton.DpadRight : JoyButton.DpadLeft);
        await PressButton(JoyButton.A);
    }

    /// <summary>The newest saved game of a player: one number from it, like "score".</summary>
    long LastRun(string name, string column) =>
        AskDatabase(selfTestDbPath, $"SELECT {column} FROM runs WHERE player_name = $name COLLATE NOCASE ORDER BY id DESC LIMIT 1;", ("$name", name));

    /// <summary>
    /// 1. START on Bolt-E's controller pauses a game: the tree is paused, the world doesn't move, Bolt-E hangs in the air,
    /// a Later() doesn't fire and the music waits. START again goes on. P and Esc work too, Enter doesn't, Dad's START does
    /// nothing, and on the title screen and the results card START still starts a game.
    /// </summary>
    async Task PauseFreezes()
    {
        await StartQuietGame("TPauseFreeze");
        await Wait(0.5);
        robot.Jump();
        await Wait(0.1);
        int blips = sounds.TimesPlayed(Sfx.PauseBlip);
        await PressButton(JoyButton.Start);
        Check(paused && GetTree().Paused && pauseMenu.Showing && state == GameState.Playing, "START on Bolt-E's controller pauses the game");
        Check(sounds.TimesPlayed(Sfx.PauseBlip) == blips + 1 && sounds.MusicPausedNow, "a little blip, and the music waits");

        float distanceThen = distance, robotY = robot.Position.Y;
        int metersThen = Meters;
        bool laterFired = false;
        Later(0.3, () => laterFired = true);
        await Wait(1);
        Check(distance == distanceThen && Meters == metersThen, $"the world doesn't move for 1 second ({Meters} m)");
        Check(robot.Position.Y == robotY && !robot.OnGround, "Bolt-E hangs in the middle of his jump");
        Check(!laterFired, "a Later() doesn't fire while the game is paused");

        await PressButton(JoyButton.Start);
        Check(!paused && !GetTree().Paused && !pauseMenu.Showing && !sounds.MusicPausedNow, "START again: back to the game, and the music goes on");
        await Wait(0.5);
        Check(distance > distanceThen && laterFired && robot.Position.Y != robotY, "everything moves again, and the Later() fired after the pause");

        await TapKey(Key.P);
        bool pPaused = paused;
        await TapKey(Key.P);
        Check(pPaused && !paused, "P pauses, and P again goes on");
        await TapKey(Key.Escape);
        bool escPaused = paused;
        await TapKey(Key.Escape);
        Check(escPaused && !paused && !GetTree().Paused, "Esc pauses, and Esc again goes on");
        await TapKey(Key.Enter);
        Check(!paused && state == GameState.Playing, "Enter doesn't pause (it's only START, P and Esc)");

        await SecondPadButton(JoyButton.Start);
        Check(!paused && !GetTree().Paused, "Dad's controller's START doesn't pause");
        await PressButton(JoyButton.Start);
        await SecondPadButton(JoyButton.Start);
        await SecondPadButton(JoyButton.A);
        Check(paused && pauseMenu.Showing, "...and while paused, Dad's START and A do nothing either");
        await PressButton(JoyButton.Start);

        // On the menus, START keeps its old job: it starts a game (and never pauses)
        GoToTitle();
        await Wait(0.2);
        await PressButton(JoyButton.Start);
        Check(state == GameState.Playing && !paused && !GetTree().Paused, "START on the title screen still starts a game");
        QuietRoad();
        spareBatteries = 0;
        robot.BlinkTime = 0;
        Bonk(null);
        Check(await WaitUntil(() => hud.ResultsShowing, 3), "game over: the results card shows");
        await Wait(0.5);
        await PressButton(JoyButton.Start);
        Check(state == GameState.Playing && !paused && !GetTree().Paused, "START on the results card still plays again");
    }

    /// <summary>
    /// 2. On the pause screen UP / DOWN pick a row (stopping at the top and the bottom), and on the WORLD WARP row
    /// LEFT / RIGHT pick a world, skipping the grey "?" ones. The world we're in gets YOU ARE HERE. A on CONTINUE goes on.
    /// </summary>
    async Task PauseMenuNav()
    {
        string name = FreshName("TPAUSENAV");
        await StartQuietGame(name);
        await PressButton(JoyButton.Start);
        await NextFrame();
        Check(pauseMenu.Row == ContinueRow && pauseMenu.Picked == 0 && pauseMenu.HereRibbonOn == 0,
              $"CONTINUE is picked first, and Sunny Hills says YOU ARE HERE (row {pauseMenu.Row}, ribbon on {pauseMenu.HereRibbonOn})");
        Check(pauseMenu.PicturesDrawn == 2 && pauseMenu.QuestionMarksDrawn == 4,
              $"a new player can warp to Sunny Hills and the Station: 2 pictures, 4 grey '?' cards ({pauseMenu.PicturesDrawn}, {pauseMenu.QuestionMarksDrawn})");

        int ticks = sounds.TimesPlayed(Sfx.MenuTick);
        await PressButton(JoyButton.DpadRight);
        Check(pauseMenu.Picked == 0, "LEFT / RIGHT do nothing on the CONTINUE row");
        await PressButton(JoyButton.DpadDown);
        Check(pauseMenu.Row == WarpRow && sounds.TimesPlayed(Sfx.MenuTick) == ticks + 1, "D-pad DOWN: WORLD WARP (tick!)");
        await PressButton(JoyButton.DpadRight);
        Check(pauseMenu.Picked == Worlds.StationIndex, $"RIGHT skips the grey worlds, to the Station ({pauseMenu.Picked})");
        await PressButton(JoyButton.DpadRight);
        Check(pauseMenu.Picked == Worlds.StationIndex, "RIGHT again stays at the end");
        await PressButton(JoyButton.DpadLeft);
        Check(pauseMenu.Picked == 0, "LEFT goes back to Sunny Hills");
        await StickX(0.9f);
        await StickX(1f);
        await StickX(0);
        Check(pauseMenu.Picked == Worlds.StationIndex, "the stick works too (one push = one step)");
        await TapKey(Key.Left);
        Check(pauseMenu.Picked == 0, "...and the arrow keys");
        await SecondPadButton(JoyButton.DpadRight);
        await SecondPadButton(JoyButton.DpadDown);
        Check(pauseMenu.Picked == 0 && pauseMenu.Row == WarpRow, "Dad's controller can't move the picks");

        await PressButton(JoyButton.DpadDown);
        Check(pauseMenu.Row == QuitRow, "DOWN: QUIT TO TITLE");
        await PressButton(JoyButton.DpadDown);
        Check(pauseMenu.Row == QuitRow, "DOWN again stays at the bottom");
        await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = -0.9f });
        await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0 });
        Check(pauseMenu.Row == WarpRow, "the stick pushed up: WORLD WARP");
        await TapKey(Key.Up);
        Check(pauseMenu.Row == ContinueRow, "the Up arrow: CONTINUE");
        await PressButton(JoyButton.DpadUp);
        Check(pauseMenu.Row == ContinueRow && paused, "UP again stays at the top (and D-pad UP never starts or ends anything here)");
        await PressButton(JoyButton.A);
        Check(!paused && !GetTree().Paused && state == GameState.Playing && WorldHere == 0, "A on CONTINUE: back to the game");

        // A player who reached Night City can warp to Sunny Hills, Candy Land, Night City and the Station
        string farther = FreshName("TPAUSEFAR");
        SeedRun(farther, bolts: 0, world: 3);
        GoToTitle();
        await StartQuietGame(farther);
        await PressButton(JoyButton.Start);
        await PressButton(JoyButton.DpadDown);
        var steps = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            await PressButton(JoyButton.DpadRight);
            steps.Add(pauseMenu.Picked);
        }
        await NextFrame();
        Check(string.Join(" ", steps) == "1 2 5 5" && pauseMenu.PicturesDrawn == 4 && pauseMenu.QuestionMarksDrawn == 2,
              $"a player who reached Night City steps through worlds {string.Join(" ", steps)} (Snowy Peaks and the Moon are grey)");
        await PressButton(JoyButton.Start);
        Check(!paused, "START goes straight back to the game from any row");
    }

    /// <summary>
    /// 3. From Sunny Hills, WORLD WARP to UPSIDE-DOWN STATION: the ceiling shuts, flipping is on, the tip shows, the screen
    /// is cleared, and the score, bolts and batteries stay. Then warp back to Sunny Hills: normal gravity, the right way
    /// up, no ceiling. Warping to the world you're in just goes on.
    /// </summary>
    async Task WarpToStation()
    {
        string name = FreshName("TWARP");
        await StartQuietGame(name);
        await Wait(0.5);
        boltsCollected = 37;
        bonusPoints = 150;
        spareBatteries = 2;
        AddObstacle(ObstacleKind.Crate, 900);
        var enemy = new Enemy { Kind = EnemyKind.PupBot, Position = new Vector2(1000, GroundY) };
        AddChild(enemy);
        enemies.Add(enemy);
        AddBox(1100);
        int warps = sounds.TimesPlayed(Sfx.Warp);
        await PressButton(JoyButton.Start);
        float distanceThen = distance;
        int scoreThen = Score;

        await PressButton(JoyButton.DpadDown);
        await PressButton(JoyButton.DpadRight);
        await PressButton(JoyButton.A);
        Check(!paused && !GetTree().Paused && Engine.TimeScale == 1, "A on a world: the game goes on");
        Check(WorldHere == Worlds.StationIndex && CurrentWorld.Name == "UPSIDE-DOWN STATION" && InStation, $"WHOOSH: warped to {CurrentWorld.Name}");
        Check(sounds.TimesPlayed(Sfx.Warp) == warps + 1 && pauseMenu.Flash > 0, "the warp whoosh and the rainbow flash");
        Check(obstacles.Count == 0 && enemies.Count == 0 && boxes.Count == 0 && spawnCountdown >= WarpSafeGap,
              "the crate, the bad robot and the ? box puffed away, and there's a gap before new things come");
        Check(boltsCollected == 37 && bonusPoints == 150 && spareBatteries == 2 && distance >= distanceThen && Score >= scoreThen,
              $"the score, the bolts, the batteries and the meters are kept (score {Score})");
        Check(hud.BannerText == "WELCOME TO UPSIDE-DOWN STATION!" && hud.HintText == FlipTip, $"'{hud.BannerText}' and '{hud.HintText}'");
        Check(await WaitUntil(() => robot.FlipMode, 0.5) && ceilingOpen == 0 && robot.CeilingY == CeilingSurfaceY,
              "the ceiling is shut, and flipping is on");
        Check(await WaitUntil(() => sounds.StationMusicActive && Mathf.Abs(sounds.MusicPitch - CurrentWorld.MusicSpeed) < 0.05f, 1.5),
              $"the Station's own music, at its speed ({sounds.MusicPitch:0.00})");
        Check(startWorld == Worlds.StationIndex && worldsReached == 1, "the Station is the next game's start world, and warping isn't riding (still 1 world)");
        Check(await WaitUntil(() => pauseMenu.Flash <= 0, 1.5), "the flash fades away");

        // Up to the ceiling, then warp back home from up there
        await WaitUntil(() => robot.OnGround, 1);
        FlipPressed();
        Check(await WaitUntil(() => robot.UpsideDown && robot.OnGround, 2), "Bolt-E flips up to the ceiling");
        await PressButton(JoyButton.Start);
        await NextFrame();
        Check(pauseMenu.HereRibbonOn == Worlds.StationIndex && pauseMenu.Picked == Worlds.StationIndex, "the Station says YOU ARE HERE");
        await PressButton(JoyButton.DpadDown);
        await PressButton(JoyButton.DpadLeft);
        Check(pauseMenu.Picked == 0, "LEFT skips the grey worlds, back to Sunny Hills");
        await PressButton(JoyButton.A);
        Check(WorldHere == 0 && !InStation && hud.BannerText == "WELCOME TO SUNNY HILLS!", $"warped home: '{hud.BannerText}'");
        Check(!robot.FlipMode && robot.CeilingY == Robot.NoCeiling && ceilingOpen == 1 && robot.GravityScale == 1,
              "no more flipping, no ceiling, and normal gravity");
        Check(await WaitUntil(() => robot.OnGround && !robot.UpsideDown && robot.Position.Y == GroundY, 3), "Bolt-E is the right way up, on the floor");
        Check(await WaitUntil(() => !sounds.StationMusicActive, 1.5), "the runner music again");
        await Wait(0.3);
        Check(!robot.FlipMode && ceilingOpen == 1 && stationGaps.Count == 0, "...and the ceiling stays away");

        // Warping to the world you're in just goes on
        warps = sounds.TimesPlayed(Sfx.Warp);
        await WarpWithButtons(0);
        Check(!paused && WorldHere == 0 && sounds.TimesPlayed(Sfx.Warp) == warps, "warping to Sunny Hills while in Sunny Hills just goes on");
        Check(boltsCollected == 37 && spareBatteries == 2 && state == GameState.Playing, "still the same game");
    }

    /// <summary>
    /// 4. In a boss fight (and while the boss warning is up, and while the next world is on its way) pausing works, but
    /// WORLD WARP is greyed: "Beat the boss first!", and A does nothing there. CONTINUE works.
    /// </summary>
    async Task NoWarpInBossFight()
    {
        StartBossFight("TPauseBoss");
        await Wait(0.5);
        await PressButton(JoyButton.Start);
        await NextFrame();
        Check(paused && GetTree().Paused, "pausing works in a boss fight");
        Check(pauseMenu.WarpGreyed && pauseMenu.NameLine == BeatTheBossFirst, $"WORLD WARP is greyed: '{pauseMenu.NameLine}'");
        await PressButton(JoyButton.DpadDown);
        int picked = pauseMenu.Picked;
        await PressButton(JoyButton.DpadRight);
        await PressButton(JoyButton.A);
        Check(paused && pauseMenu.Row == WarpRow && pauseMenu.Picked == picked && WorldHere == 0 && boss is not null,
              "RIGHT and A do nothing on the greyed row (still paused, still the fight)");
        await PressButton(JoyButton.DpadUp);
        await PressButton(JoyButton.A);
        Check(!paused && !GetTree().Paused && BossFightActive, "CONTINUE: back to the fight");

        // The boss warning: the boss is on the way
        GoToTitle();
        await Wait(0.2);
        await StartQuietGame("TPauseWarn");
        nextBossAt = Meters + 20;
        Check(await WaitUntil(() => bossWarningShown, 1), "WARNING! BOSS AHEAD!");
        await PressButton(JoyButton.Start);
        await NextFrame();
        Check(paused && pauseMenu.WarpGreyed && pauseMenu.NameLine == BeatTheBossFirst, "during the warning: greyed too");
        await PressButton(JoyButton.DpadDown);
        await PressButton(JoyButton.DpadRight);
        await PressButton(JoyButton.A);
        Check(paused && WorldHere == 0, "...and A does nothing");
        await PressButton(JoyButton.Start);
        Check(!paused, "START goes back to the game");

        // A boss was just beaten: the next world is on its way (and it waits while the game is paused)
        GoToTitle();
        await Wait(0.2);
        await StartQuietGame("TPauseNext");
        NextWorldSoon();
        await PressButton(JoyButton.Start);
        await NextFrame();
        Check(pauseMenu.WarpGreyed && pauseMenu.NameLine == NewWorldIsComing, $"after a boss: '{pauseMenu.NameLine}'");
        await Wait(NewWorldDelay + 0.5);
        Check(WorldHere == 0, "the next world waits while the game is paused");
        await PressButton(JoyButton.Start);
        Check(await WaitUntil(() => WorldHere == 1, NewWorldDelay + 0.5), "...and comes after it goes on: CANDY LAND");
        await PressButton(JoyButton.Start);
        await NextFrame();
        Check(!pauseMenu.WarpGreyed && pauseMenu.HereRibbonOn == 1, "now WORLD WARP works again (Candy Land says YOU ARE HERE)");
        await PressButton(JoyButton.Start);
    }

    /// <summary>
    /// 5. A game that warps to the Station and then ends: the Station's postcard isn't won (only riding there from the
    /// Moon wins it), beating a boss there doesn't count every world on the way, no look is won, and the score is saved.
    /// </summary>
    async Task WarpSavesFairly()
    {
        string name = FreshName("TWARPFAIR");
        hud.SetPlayerName(name);
        await NextFrame();
        int looksBefore = Looks.All.Count(Unlocked);
        await StartQuietGame(name);
        await Wait(0.5);
        await WarpWithButtons(1);
        Check(WorldHere == Worlds.StationIndex, "warped to the Station");
        ChangeWorld(worldNumber + 1); // (like beating a boss in the Station: on to Sunny Hills)
        Check(WorldHere == 0 && worldsReached == 1, $"beating a boss after a warp doesn't count every world on the way ({worldsReached} world)");

        boltsCollected = 23;
        spareBatteries = 0;
        bestScore = 1_000_000;
        robot.BlinkTime = 0;
        int score = Score, meters = Meters;
        Check(Bonk(null) && state == GameState.GameOver, "game over");
        Check(LastRun(name, "world_reached") == 1 && (database?.FarthestWorld(name) ?? 0) == 1, "the saved game says 1 world: no postcard won");
        Check(LastRun(name, "score") == score && LastRun(name, "bolts") == 23 && LastRun(name, "distance_m") == meters,
              $"the score ({score}), the bolts and the meters are saved, where the game really ended");
        await Wait(2);
        Check(hud.ResultsShowing && hud.ResultsWorldLine == "World: SUNNY HILLS", $"the results card says '{hud.ResultsWorldLine}'");
        Check(hud.Postcards.Reached == 1 && hud.Postcards.RibbonsDrawn == 0 && unlockQueue.Count == 0, "no new postcards");
        Check(Looks.All.Count(Unlocked) == looksBefore, "no look was won");
        ShowStartWorld();
        Check(startWorld == Worlds.StationIndex && hud.Postcards.Picked == Worlds.StationIndex,
              "the next game starts in the Station (where the warp went), and its postcard glows");
    }

    /// <summary>
    /// 6. QUIT TO TITLE: the game is saved like any game over (no KABOOM), the results card shows "Game saved!", the
    /// game isn't paused and time runs at normal speed. START plays again from there.
    /// </summary>
    async Task QuitToTitleSaves()
    {
        string name = FreshName("TQUIT");
        await StartQuietGame(name);
        await Wait(1);
        boltsCollected = 12;
        bestScore = 1_000_000;
        int booms = sounds.TimesPlayed(Sfx.Explosion);
        await PressButton(JoyButton.Start);
        int score = Score, meters = Meters;
        await PressButton(JoyButton.DpadDown);
        await PressButton(JoyButton.DpadDown);
        Check(pauseMenu.Row == QuitRow, "QUIT TO TITLE is picked");
        await PressButton(JoyButton.A);
        Check(!paused && !GetTree().Paused && Engine.TimeScale == 1, "not paused, and time runs at normal speed");
        Check(state == GameState.GameOver && !robot.Crashed && sounds.TimesPlayed(Sfx.Explosion) == booms, "the game ended with no KABOOM");
        Check(LastRun(name, "score") == score && LastRun(name, "bolts") == 12 && LastRun(name, "distance_m") == meters,
              $"the game is saved: score {LastRun(name, "score")}, 12 bolts, {meters} m");
        Check(await WaitUntil(() => hud.ResultsShowing, 0.5) && hud.ResultsTitle == QuitTitle, $"the results card shows right away: '{hud.ResultsTitle}'");
        await Wait(0.5);
        await PressButton(JoyButton.Start);
        Check(state == GameState.Playing && !paused, "START plays again");

        // A new high score is still a NEW HIGH SCORE!
        QuietRoad();
        bestScore = 0;
        await Wait(0.5);
        await PressButton(JoyButton.Start);
        await PressButton(JoyButton.DpadDown);
        await PressButton(JoyButton.DpadDown);
        await PressButton(JoyButton.A);
        Check(await WaitUntil(() => hud.ResultsShowing, 0.5) && hud.ResultsTitle == "NEW HIGH SCORE!", $"quitting with a new best: '{hud.ResultsTitle}'");
    }

    /// <summary>
    /// 7. Pausing can never get stuck: START while Bolt-E is in pieces (or in slow motion) waits, and pauses when he's
    /// back, with normal time. ClearWorld, GoToTitle and StartGame never leave the game paused. Unplugging Bolt-E's
    /// controller pauses (another one doesn't). Typing a P in the name box doesn't pause.
    /// </summary>
    async Task PauseSafety()
    {
        await StartQuietGame("TPauseSafe");
        await Wait(0.3);
        spareBatteries = 1;
        robot.BlinkTime = 0;
        Bonk(null);
        Check(state == GameState.Rebuilding && Engine.TimeScale < 1, "KABOOM: in pieces, in slow motion");
        await PressButton(JoyButton.Start);
        Check(!paused && !GetTree().Paused && pauseWanted, "START while he's in pieces doesn't pause yet");
        Check(await WaitUntil(() => paused, 8), "...it pauses as soon as he's back together");
        Check(state == GameState.Playing && Engine.TimeScale == 1 && GetTree().Paused, "riding again, normal time, paused");
        await PressButton(JoyButton.Start);
        Check(!paused && !GetTree().Paused, "START goes on");

        // Slow motion (like the last stomp on a boss): the pause waits until time is normal again
        robot.BlinkTime = 1e9f;
        Engine.TimeScale = 0.4;
        await TapKey(Key.P);
        bool waited = !paused;
        Engine.TimeScale = 1;
        Check(waited && await WaitUntil(() => paused, 0.5), "in slow motion the pause waits, then pauses with normal time");
        await TapKey(Key.P);

        // Unplugging Bolt-E's controller pauses; another controller doesn't
        OnControllerPluggedOrUnplugged(SelfTestSecondPad, false);
        Check(!paused, "unplugging another controller doesn't pause");
        OnControllerPluggedOrUnplugged(SelfTestDevice, false);
        Check(paused && GetTree().Paused, "unplugging Bolt-E's controller pauses the game");
        await PressButton(JoyButton.Start);
        Check(!paused, "...and START goes on");

        // Nothing ever leaves the game paused
        await PressButton(JoyButton.Start);
        ClearWorld();
        Check(!paused && !GetTree().Paused && !pauseMenu.Showing && !sounds.MusicPausedNow, "ClearWorld un-pauses");
        await PressButton(JoyButton.Start); // (the game state is still "playing" after ClearWorld)
        GoToTitle();
        Check(!paused && !GetTree().Paused && !pauseMenu.Showing && Engine.TimeScale == 1 && state == GameState.Title, "GoToTitle un-pauses");
        await PressButton(JoyButton.Start);
        Check(state == GameState.Playing && !paused, "a new game");
        QuietRoad();
        await PressButton(JoyButton.Start);
        Check(paused, "paused");
        state = GameState.GameOver; // (StartGame only starts a game from the menus)
        StartGame();
        Check(!paused && !GetTree().Paused && state == GameState.Playing && Engine.TimeScale == 1, "StartGame un-pauses");

        // On the menus, a controller coming unplugged doesn't pause
        GoToTitle();
        OnControllerPluggedOrUnplugged(SelfTestDevice, false);
        await PressButton(JoyButton.Start);
        Check(state == GameState.Playing && !paused && !GetTree().Paused, "on the title screen nothing pauses, and START still starts");

        // Typing a name: P is just a letter
        GoToTitle();
        await Wait(0.2);
        var box = hud.NameBox;
        hud.StartTyping();
        box.Text = "TPA";
        box.CaretColumn = box.Text.Length;
        await NextFrame();
        await TypeLetter('P');
        Check(!paused && !GetTree().Paused && state == GameState.Title && box.Text.EndsWith("P", StringComparison.OrdinalIgnoreCase),
              $"typing P in the name box types it ('{box.Text}') and doesn't pause");
        await TapRealKey(Key.Escape);
        box.ReleaseFocus();
        Check(Engine.TimeScale == 1 && !GetTree().Paused, "time is normal and nothing is paused at the end");
    }

    /// <summary>8. The pause screen draws in every world (and greyed in a boss fight), on every row, inside the screen.</summary>
    async Task PauseDraws()
    {
        int errors = SelfTestLogger.Errors;
        string name = FreshName("TPAUSEDRAW");
        SeedRun(name, bolts: 0, world: Worlds.All.Length); // (this player has reached every world)
        await StartQuietGame(name);
        var outside = new List<string>();
        int draws = 0;
        for (int world = 0; world < Worlds.All.Length; world++)
        {
            if (world != WorldHere)
            {
                await PressButton(JoyButton.Start);
                pauseMenu.Row = WarpRow;
                pauseMenu.Picked = world;
                PickPauseRow(); // (warp!)
                await Wait(0.2);
            }
            await PressButton(JoyButton.Start);
            for (int row = ContinueRow; row <= QuitRow; row++)
            {
                pauseMenu.Row = row;
                pauseMenu.QueueRedraw();
                int before = pauseMenu.TimesDrawn;
                await NextFrame();
                await NextFrame();
                if (pauseMenu.TimesDrawn > before) draws++;
                var area = pauseMenu.DrawnArea;
                if (area.Position.X < 0 || area.Position.Y < 0 || area.End.X > 1280 || area.End.Y > 718)
                    outside.Add($"{Worlds.All[world].Name} row {row}: {area}");
            }
            Check(WorldHere == world && pauseMenu.HereRibbonOn == world && pauseMenu.PicturesDrawn == Worlds.All.Length && pauseMenu.QuestionMarksDrawn == 0,
                  $"{Worlds.All[world].Name}: 6 pictures, and YOU ARE HERE on it");
            await PressButton(JoyButton.Start);
        }
        Check(draws == Worlds.All.Length * 3, $"the pause screen drew every time ({draws} of {Worlds.All.Length * 3})");
        Check(outside.Count == 0, $"everything fits on the screen{(outside.Count > 0 ? ": " + string.Join("; ", outside) : $" (last: {pauseMenu.DrawnArea})")}");

        // Greyed in a boss fight
        GoToTitle();
        await Wait(0.2);
        StartBossFight("TPauseDrawBoss");
        await PressButton(JoyButton.Start);
        pauseMenu.Row = WarpRow;
        pauseMenu.QueueRedraw();
        await NextFrame();
        await NextFrame();
        var greyed = pauseMenu.DrawnArea;
        Check(pauseMenu.WarpGreyed && greyed.Position.X >= 0 && greyed.End.X <= 1280 && greyed.End.Y <= 718, $"the greyed screen fits too ({greyed})");
        await PressButton(JoyButton.Start);
        Check(SelfTestLogger.Errors == errors, "no engine errors");
    }
}
