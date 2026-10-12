using Godot;

/// <summary>
/// PAUSE, and WORLD WARP! (This is more of Main: "partial" means one class spread over several files.)
///
/// In the middle of a game, START on Bolt-E's controller (or P or Esc on the keyboard) pauses everything: the world,
/// the bad robots, the bosses, the timers, the puffs and the powers all freeze, and the music waits. A pause screen
/// shows three big choices (UP / DOWN picks one, A picks it, START goes straight back to the game):
///   CONTINUE       back to the game.
///   WORLD WARP     a row of world postcards. LEFT / RIGHT picks one, and A warps there: WHOOSH! Same score, same bolts,
///                  same batteries, just a new world. (Not in a boss fight: beat the boss first!)
///   QUIT TO TITLE  the game ends and is saved, just like a game over (without the KABOOM): the results card shows.
/// Dad's second controller can't pause or pick anything. If Bolt-E's controller comes unplugged in the middle of a game,
/// the game pauses by itself.
///
/// How the freezing works: Godot's "tree pause" (GetTree().Paused = true) stops every node of the game. Only the pause
/// screen (and its own little sounds) keep going, because they are ProcessMode.Always. Later() waits stop too. While
/// Bolt-E is in pieces, in slow motion, or falling through a gap in UPSIDE-DOWN STATION, a START press waits, and the game
/// pauses as soon as he's riding normally again (so the slow motion and the rebuild always finish properly).
///
/// Fair play: warping never wins postcards or looks. A world only counts as reached when Bolt-E rides into it by
/// beating a boss (the same rule as picking a start world: see Main.StartWorld.cs).
/// </summary>
public partial class Main
{
    // ---- The pause screen (try changing these!) ----
    const float WarpSafeGap = 1200f;   // after a warp, this many pixels of empty road before new things come (like after a boss)
    const string BeatTheBossFirst = "Beat the boss first!";
    const string NewWorldIsComing = "A new world is coming!";
    const string QuitTitle = "Game saved!"; // the big words on the results card after QUIT TO TITLE

    // The three rows of the pause screen, from the top
    const int ContinueRow = 0, WarpRow = 1, QuitRow = 2;

    CanvasLayer pauseLayer = null!;
    PauseMenu pauseMenu = null!;
    bool paused;          // true while the pause screen is up (and the game is frozen)
    bool pauseWanted;     // START was pressed while Bolt-E couldn't pause yet: it pauses as soon as he can
    bool warpedThisRun;   // true once this game used WORLD WARP (it counts the worlds reached the fair way)
    bool newWorldComing;  // true between beating a boss and the next world arriving (see NextWorldSoon)

    /// <summary>The world we're in now, as a postcard number: 0 = Sunny Hills ... 5 = UPSIDE-DOWN STATION, 6 = VOLCANO ISLAND.</summary>
    int WorldHere => worldNumber % Worlds.All.Length;

    /// <summary>Makes the pause screen. It lives on its own layer, on top of everything, and never gets frozen itself.</summary>
    void SetUpPause()
    {
        pauseLayer = new CanvasLayer { Layer = 50, ProcessMode = ProcessModeEnum.Always };
        pauseMenu = new PauseMenu { Size = new Vector2(ScreenWidth, ScreenHeight), MouseFilter = Control.MouseFilterEnum.Ignore };
        pauseMenu.InputWhilePaused = PauseScreenInput;
        pauseLayer.AddChild(pauseMenu);
        AddChild(pauseLayer);
    }

    // ---------- Pausing and going on ----------

    /// <summary>
    /// Bolt-E can pause right now: a game is going, nothing is in slow motion, and he isn't in pieces or falling
    /// through a gap. (Otherwise the pause waits a moment: see UpdatePause.)
    /// </summary>
    bool CanPauseNow => state == GameState.Playing && !paused && Engine.TimeScale == 1 && fallingThrough is null && !robot.Crashed &&
                        !VolcanoBusy && // (not while dropping into a LAVA CAVE, blasting out, or hopping in the lava: see Main.Volcano.cs)
                        !mapOpen;       // (not over the WORLD MAP: the game is already frozen there, see Main.WorldMap.cs)

    /// <summary>START (or P or Esc), or Bolt-E's controller came unplugged: pause now, or as soon as he can.</summary>
    void AskToPause()
    {
        if (CanPauseNow) PauseGame();
        else if ((state is GameState.Playing or GameState.Rebuilding) && !paused) pauseWanted = true;
    }

    /// <summary>Every frame (while the game isn't paused): a pause that was waiting happens as soon as it can.</summary>
    void UpdatePause()
    {
        if (state is not (GameState.Playing or GameState.Rebuilding)) pauseWanted = false; // (the game ended: never mind)
        if (pauseWanted && CanPauseNow) PauseGame();
    }

    /// <summary>Freeze! The pause screen shows, with CONTINUE picked and the world we're in picked on the postcards.</summary>
    void PauseGame()
    {
        paused = true;
        pauseWanted = false;
        GetTree().Paused = true;
        sounds.HoldMusicForPause(true);
        sounds.Play(Sfx.PauseBlip);
        pauseMenu.Row = ContinueRow;
        pauseMenu.Picked = WorldHere;
        pauseMenu.Showing = true;
        RefreshPauseMenu();
    }

    /// <summary>Back to the game: everything goes on exactly where it stopped. ("blip" = the little resume sound.)</summary>
    void ResumeGame(bool blip = true)
    {
        if (!paused) return;
        paused = false;
        GetTree().Paused = false;
        sounds.HoldMusicForPause(false);
        if (blip) sounds.Play(Sfx.PauseBlip, 1.5f);
        pauseMenu.Showing = false;
        pauseMenu.QueueRedraw();
    }

    /// <summary>A fresh start (ClearWorld): never paused, no waiting pause, no warp flash, and no warps this game yet.</summary>
    void ClearPause()
    {
        paused = false;
        pauseWanted = false;
        warpedThisRun = false;
        newWorldComing = false;
        GetTree().Paused = false;
        sounds.HoldMusicForPause(false);
        pauseMenu.Showing = false;
        pauseMenu.Flash = 0;
        pauseMenu.QueueRedraw();
    }

    /// <summary>Tells the pause screen what to draw: the row picked, the worlds, and why WORLD WARP is greyed (if it is).</summary>
    void RefreshPauseMenu()
    {
        pauseMenu.Here = WorldHere;
        pauseMenu.CanWarpTo = WarpWorlds();
        pauseMenu.Cleared = clearedWorlds; // (a CLEARED world's postcard gets a little gold flag: see Main.WorldMap.cs)
        pauseMenu.WarpBlocked = WhyNoWarp();
        pauseMenu.QueueRedraw();
    }

    // ---------- WORLD WARP: which worlds, and when ----------

    /// <summary>
    /// The worlds WORLD WARP can go to: the same as the title screen's start worlds (Sunny Hills, every world this player
    /// has reached, UPSIDE-DOWN STATION and VOLCANO ISLAND), plus the worlds this game has really reached so far, and the world
    /// we're in.
    /// </summary>
    HashSet<int> WarpWorlds()
    {
        var worlds = new HashSet<int>(AllowedStartWorlds());
        for (int world = 0; world < Math.Min(worldsReached, Worlds.All.Length); world++) worlds.Add(world);
        worlds.Add(WorldHere);
        return worlds;
    }

    /// <summary>Why WORLD WARP can't be used right now (the words on its greyed-out row), or null if it can.</summary>
    string? WhyNoWarp()
    {
        if (InCave || CaveBusy) return FindTheWayOut;                 // (in a LAVA CAVE: see Main.LavaCave.cs)
        if (newWorldComing) return NewWorldIsComing;                   // (a boss was just beaten: the next world is on its way)
        if (BossFightActive || bossWarningShown) return BeatTheBossFirst; // (a boss fight, or a boss on the way)
        return null;
    }

    // ---------- The pause screen's buttons ----------

    /// <summary>
    /// A press while the game is paused (the pause screen sends every press here). Returns true when it was used up,
    /// so nothing else gets it. Only Bolt-E's own controller and the keyboard work here, never Dad's second controller.
    /// </summary>
    bool PauseScreenInput(InputEvent e)
    {
        if (!paused) return false;
        // (During the self-test only its pretend presses count, like in _UnhandledInput)
        if (selfTesting && e.Device != SelfTestDevice && e.Device != SelfTestSecondPad) return true;
        if ((e is InputEventJoypadButton or InputEventJoypadMotion) && e.Device != controllerOne) return true; // Dad's controller: nothing
        if (e is InputEventKey && hud.IsTypingName) return true;
        // F11 (full screen or a window) and F9 (the other screen) work while paused too
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F11 }) ToggleFullScreen();
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F9 }) SwapScreens();

        if (Input.IsActionJustPressedByEvent("pause", e)) ResumeGame();          // START (or P or Esc) again: go on!
        else if (Input.IsActionJustPressedByEvent("menu_up", e)) MovePauseRow(-1);
        else if (Input.IsActionJustPressedByEvent("menu_down", e)) MovePauseRow(1);
        else if (Input.IsActionJustPressedByEvent("menu_left", e)) StepWarpWorld(-1);
        else if (Input.IsActionJustPressedByEvent("menu_right", e)) StepWarpWorld(1);
        else if (Input.IsActionJustPressedByEvent("menu_ok", e)) PickPauseRow();
        return true; // (everything else does nothing while the game is paused)
    }

    /// <summary>UP (-1) or DOWN (1): the next row. It stops at the top and the bottom.</summary>
    void MovePauseRow(int step)
    {
        int row = Math.Clamp(pauseMenu.Row + step, ContinueRow, QuitRow);
        if (row == pauseMenu.Row) return;
        pauseMenu.Row = row;
        sounds.Play(Sfx.MenuTick, 1.4f);
        RefreshPauseMenu();
    }

    /// <summary>
    /// LEFT (-1) or RIGHT (1) on the WORLD WARP row: the next world that can be picked (grey "?" worlds are skipped).
    /// It stops at the ends. (Nothing happens on the other rows, or while warping isn't allowed.)
    /// </summary>
    void StepWarpWorld(int step)
    {
        if (pauseMenu.Row != WarpRow || WhyNoWarp() is not null) return;
        var allowed = WarpWorlds();
        for (int world = pauseMenu.Picked + step; world >= 0 && world < Worlds.All.Length; world += step)
        {
            if (!allowed.Contains(world)) continue;
            pauseMenu.Picked = world;
            sounds.Play(Sfx.MenuTick, 1.7f);
            RefreshPauseMenu();
            return;
        }
    }

    /// <summary>A (or Enter or Space): do what the picked row says.</summary>
    void PickPauseRow()
    {
        switch (pauseMenu.Row)
        {
            case ContinueRow:
                ResumeGame();
                break;
            case WarpRow:
                if (WhyNoWarp() is not null || !WarpWorlds().Contains(pauseMenu.Picked))
                {
                    sounds.Play(Sfx.MenuTick, 0.6f); // a low "nope" tick: can't warp right now
                    break;
                }
                WarpTo(pauseMenu.Picked);
                break;
            case QuitRow:
                QuitToTitle();
                break;
        }
    }

    // ---------- WARP! ----------

    /// <summary>
    /// WORLD WARP to world number "world": the game goes on (same score, bolts, batteries and meters) in that world,
    /// with a WHOOSH, a rainbow flash, and "WELCOME TO ...!". Everything dangerous on the screen puffs away, and new things
    /// come after a little gap. The warped-to world is also where the next game starts (see Main.StartWorld.cs).
    /// Warping to the world we're already in just goes back to the game.
    /// </summary>
    void WarpTo(int world)
    {
        ResumeGame(blip: false);
        if (world == WorldHere) return;

        warpedThisRun = true;
        startWorld = world; // (the title screen's start world for the next game)
        bool wasInStation = InStation;
        bool wasInVolcano = InVolcano;

        // Clear the road: any power ends quietly, and the dangers and ? boxes puff away
        StopPowersNow();
        PuffAway(obstacles, _ => true);
        PuffAway(enemies, _ => true);
        PuffAway(boxes, _ => true);
        PuffAway(stationGaps, _ => true);
        spawnCountdown = Mathf.Max(spawnCountdown, WarpSafeGap);

        // Into the new world right away (the flash hides the change). worldsReached doesn't change: warping isn't riding!
        worldNumber = world;
        fromWorld = toWorld = CurrentWorld;
        worldBlend = 1;
        robot.SetGravityScale(toWorld.Gravity);
        sounds.SetLevelMusic(CurrentLevelMusic, toWorld.MusicSpeed);
        hud.ShowBanner($"WELCOME TO {toWorld.Name}!", new Color(1f, 0.85f, 0.2f));
        if (toWorld.Gravity < 1) hud.ShowHint("Moon jumps are FLOATY!");

        if (InStation) ArriveInStation(shutNow: true); // the ceiling is shut, and JUMP flips gravity
        else if (wasInStation) LeaveStationNow();       // gravity is normal again, and the ceiling is gone
        if (InVolcano) ArriveInVolcano();                // a safe rock to start on, over the lava
        else if (wasInVolcano) LeaveVolcanoNow();       // the rocks puff away, and the ground is solid again

        sounds.Play(Sfx.Warp);
        sounds.Play(Sfx.NewWorld);
        Rumble(0.4f, 0.2f, 0.3f);
        Burst(robot.BodyMiddle, 20, Colors.White, 320f);
        pauseMenu.StartFlash();
    }

    /// <summary>Warping out of UPSIDE-DOWN STATION: no more flipping, Bolt-E turns the right way up, and the ceiling is gone.</summary>
    void LeaveStationNow()
    {
        fallingThrough = null;
        spaceFallTimer = 0;
        ceilingOpen = 1;
        robot.SetFlipMode(false); // (if he was on the ceiling, he turns over and drops to the floor)
        robot.CeilingY = Robot.NoCeiling;
        hud.SetGravityArrow(false, 0);
    }

    // ---------- QUIT TO TITLE ----------

    /// <summary>
    /// QUIT TO TITLE: the game ends and is saved like any game over (the score, the Bolt Bank, the family race), but with
    /// no KABOOM: the results card ("Game saved!") shows right away. From there START plays again, like after any game.
    /// </summary>
    void QuitToTitle()
    {
        ResumeGame(blip: false);
        sounds.Play(Sfx.MenuTick, 1.2f);
        GameOver(quit: true);
    }
}
