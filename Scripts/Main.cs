using Godot;

/// <summary>
/// The boss of the game (not Big Rusty, the other kind of boss!). It draws the world, moves things along,
/// spawns obstacles, enemies and bolts, checks for bumps and stomps, and saves scores to the SQLite database.
/// More of Main lives in other files: Main.Batteries.cs (spare batteries and the magic rebuild),
/// Main.PowerUps.cs (rainbow ? boxes and their powers), Main.Worlds.cs (the World Tour: a new world after every
/// Big Rusty win), Main.Looks.cs (the Bolt Bank and Bolt-E's looks), Main.Family.cs (the family race: who's playing,
/// and the flags on the road), Main.Driver.cs (Dad drives Big Rusty with a second controller), Main.Vroomba.cs
/// (VROOMBA, the second boss, and how the bosses take turns) and Main.SelfTest.cs (grown-up testing code).
/// </summary>
public partial class Main : Node2D
{
    // Rebuilding = Bolt-E crashed with a spare battery and is putting himself back together
    enum GameState { Title, Playing, Rebuilding, GameOver }

    // ---- Game tuning numbers (fun to experiment with!) ----
    const float ScreenWidth = 1280f;
    const float ScreenHeight = 720f;
    const float GroundY = 600f;          // where the ground is
    const float RobotX = 240f;           // how far from the left the robot rolls
    const float StartSpeed = 420f;       // pixels per second at the start
    const float MaxSpeed = 1000f;        // fastest the game can go
    const float SpeedUpPerSecond = 9f;   // how quickly the game gets faster
    const float PixelsPerMeter = 50f;
    const int PointsPerBolt = 10;
    const float ResultsDelay = 1.4f;     // seconds to watch the robot blow apart before the scores pop up
    const int PointsPerStomp = 25;       // doubles for each stomp in a row without landing: 25, 50, 100, 200
    const int PointsPerBossHit = 50;
    const int PointsForBeatingBoss = 250;
    const float FirstBossAt = 400;       // meters
    const float MetersBetweenBosses = 700;
    const float StompForgiveness = 30f;  // how far below the top of a head your feet can be and still count as a stomp

    GameState state = GameState.Title;
    Robot robot = null!;
    Hud hud = null!;
    SoundBoard sounds = null!;
    ScoreDatabase? database;

    readonly List<Obstacle> obstacles = new();
    readonly List<BoltPickup> bolts = new();
    readonly List<Particle> particles = new();
    readonly List<Enemy> enemies = new();
    readonly List<Blast> blasts = new();
    readonly List<Shockwave> shockwaves = new();
    readonly List<Laser> lasers = new();
    BossBase? boss;          // Big Rusty or VROOMBA (see BossBase.cs)
    ScorePopups popups = null!;

    float speed;
    float distance;          // in pixels
    int boltsCollected;
    float playTime;
    float spawnCountdown;    // pixels until the next obstacle
    float gameOverTimer;
    Action? showResults;     // the game-over card, waiting to be shown
    float shake;
    int bestScore;
    bool wasOnGround = true;
    int boltCombo;           // how many bolts in a row (each one sounds a little higher)
    float lastBoltTime = -10;
    int bonusPoints;         // points from stomping enemies and beating bosses
    int stomps;              // enemies stomped this game
    int stompCombo;          // stomps in a row without touching the ground
    int bossesBeaten;
    float nextBossAt;        // in meters
    bool bossWarningShown;

    bool rumbleOn = true;    // controller shaking
    bool watchingControllers; // true once we listen for controllers being plugged in
    int controllerOne;       // Bolt-E's controller (the kid's): the first one plugged in. Any other one is a SECOND controller.
    int lastBuzzedPad = -1;  // the controller that was asked to shake last (the self-test reads this: it never really shakes one)

    float speedToGetBackTo;  // after a boss fight (or a magic rebuild) the world speeds back up to this
    int runNumber;           // goes up every time the world is cleared, so old delayed things are skipped (see Later)

    /// <summary>During a boss fight the world stops and Bolt-E can ride left and right.</summary>
    bool BossFightActive => boss is not null && !boss.HasBlownUp;

    // Each bolt in a row plays the next note up the scale: do, re, mi, fa, so, la, ti, do... and on up, two whole octaves!
    static readonly int[] ScaleSteps = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24 };

    // How far each background layer has scrolled
    float cloudScroll, farHillScroll, nearHillScroll, groundScroll;

    int Meters => (int)(distance / PixelsPerMeter);
    int Score => Meters + boltsCollected * PointsPerBolt + bonusPoints;

    public override void _Ready()
    {
        if (!SelfTestArgsOk()) return; // the self-test refuses to start without a safe scratch database (see Main.SelfTest.cs)

        controllerOne = PickBoltEsController(); // the first controller plugged in is Bolt-E's (see "Controls" below)
        SetUpControls();
        OpenDatabase();
        if (!selfTesting) PutGameOnItsScreen(); // the TV, if one is plugged in (F9 swaps screens)

        robot = new Robot { GroundY = GroundY, Position = new Vector2(RobotX, GroundY), ZIndex = 10 };
        AddChild(robot);

        popups = new ScorePopups { ZIndex = 20 };
        AddChild(popups);

        hud = new Hud();
        hud.StartPressed += PlayPressed; // (see Main.Family.cs)
        AddChild(hud);

        sounds = new SoundBoard();
        AddChild(sounds);
        sounds.SetMusicOn(database?.GetSetting("music") != "off"); // remembers if you turned the music off
        // Big parts go "clonk" (low), little gears and screws go "tink" (high).
        robot.PieceBounced += bigPart => sounds.Play(Sfx.Clink, bigPart ? Rand(0.55f, 0.75f) : Rand(1.1f, 1.5f));
        // When every piece is back together after the magic rebuild, Bolt-E rides again!
        robot.Rebuilt += () => { if (state == GameState.Rebuilding) FinishRebuild(); };

        SetUpPowerUps(); // the slot machine for the rainbow ? boxes (see Main.PowerUps.cs)

        // A different player picked (or a name typed in): that player's Bolt Bank, look and postcards. Clicking the
        // picker's arrows changes the look. (See Main.Looks.cs.) While a name is still being typed, there are no
        // unlock parties: half a name like "Sam" (of "Samantha") must never use up Sam's parties.
        hud.PlayerChanged += () => { if (state is GameState.Title or GameState.GameOver) LoadPlayer(celebrate: !hud.IsTypingName); };
        hud.LookArrowClicked += step => { if (MenuInputOn) BrowseLook(step); };
        hud.PickerClicked += step => { if (MenuInputOn) PickPlayer(step, canPickNew: true); }; // the LB / RB buttons (see Main.Family.cs)

        if (database is null)
            hud.ShowWarning("Could not open the score database, so scores won't be saved this time.");

        // Notice when a game controller is plugged in or unplugged (and which one is Bolt-E's then).
        Input.JoyConnectionChanged += OnControllerPluggedOrUnplugged;
        watchingControllers = true;
        UpdateControllerStatus();

        GoToTitle();

        if (selfTesting) RunSelfTests();
    }

    // ---------- Controls: keyboard, mouse, and game controllers (like an Xbox controller) ----------

    /// <summary>
    /// "jump": Space, Up arrow, W, a mouse click, or A / B / X / Y / D-pad up on a controller.
    /// "start": Enter, or the START button on a controller (starts or restarts the game).
    /// "left" / "right": arrow keys, A / D, the D-pad or the left stick (riding around in boss fights).
    /// "menu_left" / "menu_right": the arrow keys, the D-pad or the stick, for changing Bolt-E's look on the menus.
    /// The stick has to be pushed more than halfway (0.6) for those, and one push is one step. (Not the A and D keys:
    /// the menus say "(A)" for the controller's A button, so the A key must not do something else there.)
    /// "player_prev" / "player_next": LB / RB on a controller (and TAB for the next one): who's playing, on the menus.
    /// The keyboard and the mouse work for everything. The controller buttons and the stick here are on Bolt-E's own
    /// controller (controller one, the first one plugged in: see PickControllerOne). A SECOND controller never moves
    /// Bolt-E: it can only drive Big Rusty in a boss fight (see Main.Driver.cs).
    /// </summary>
    void SetUpControls()
    {
        AddAction("jump",
            KeyPress(Key.Space), KeyPress(Key.Up), KeyPress(Key.W),
            new InputEventMouseButton { ButtonIndex = MouseButton.Left, Device = AnyDevice },
            ControllerButton(JoyButton.A), ControllerButton(JoyButton.B),
            ControllerButton(JoyButton.X), ControllerButton(JoyButton.Y),
            ControllerButton(JoyButton.DpadUp));
        AddAction("start", KeyPress(Key.Enter), KeyPress(Key.KpEnter), ControllerButton(JoyButton.Start));
        AddAction("music", KeyPress(Key.M), ControllerButton(JoyButton.Back));
        AddAction("left", KeyPress(Key.Left), KeyPress(Key.A), ControllerButton(JoyButton.DpadLeft), Stick(-1));
        AddAction("right", KeyPress(Key.Right), KeyPress(Key.D), ControllerButton(JoyButton.DpadRight), Stick(1));
        AddAction("menu_left", KeyPress(Key.Left), ControllerButton(JoyButton.DpadLeft), Stick(-1));
        AddAction("menu_right", KeyPress(Key.Right), ControllerButton(JoyButton.DpadRight), Stick(1));
        InputMap.ActionSetDeadzone("menu_left", 0.6f);
        InputMap.ActionSetDeadzone("menu_right", 0.6f);
        AddAction("player_next", KeyPress(Key.Tab), ControllerButton(JoyButton.RightShoulder));
        AddAction("player_prev", ControllerButton(JoyButton.LeftShoulder));

        // Godot's own menu actions (ui_left, ui_select... every ui_ action with controller buttons) listen to Bolt-E's
        // controller only too, so a second controller can never press a menu button or reach the name box
        BindControllerOne(controllerOne);
    }

    /// <summary>Everything Bolt-E does (the kid's actions). Their controller buttons and stick only listen to controller one.</summary>
    static readonly string[] KidActions = { "jump", "start", "music", "left", "right", "menu_left", "menu_right", "player_next", "player_prev" };

    // A button, or the left stick, on Bolt-E's controller (controller one)
    InputEventJoypadButton ControllerButton(JoyButton button) => new() { ButtonIndex = button, Device = controllerOne };
    InputEventJoypadMotion Stick(float direction) => new() { Axis = JoyAxis.LeftX, AxisValue = direction, Device = controllerOne };

    static float Rand(float min, float max) => (float)GD.RandRange(min, max);

    /// <summary>Full screen <-> window. (The game always starts in full screen: see window/size/mode in project.godot.)</summary>
    static void ToggleFullScreen()
    {
        bool full = DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
        DisplayServer.WindowSetMode(full ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
    }

    // ---------- Which screen the game is on ----------
    // With two screens (like a laptop plus a TV), the game starts full screen on the OTHER screen, not the main one,
    // so grown-ups can keep their work on the laptop. F9 swaps, and the game remembers the choice ("screen" setting).

    /// <summary>
    /// Which screen the game should be on. count = how many screens, main = the main (laptop) screen,
    /// choice = the saved setting: "main" means the main screen, anything else means the other screen.
    /// </summary>
    static int PickGameScreen(int count, int main, string? choice)
    {
        if (count <= 1 || choice == "main") return main;
        for (int screen = 0; screen < count; screen++)
            if (screen != main) return screen; // the first screen that isn't the main one (the TV!)
        return main;
    }

    /// <summary>Moves the game to its screen when it starts.</summary>
    void PutGameOnItsScreen()
    {
        int screen = PickGameScreen(DisplayServer.GetScreenCount(), DisplayServer.GetPrimaryScreen(), database?.GetSetting("screen"));
        MoveToScreen(screen);
    }

    /// <summary>F9: the game hops to the other screen, and remembers it.</summary>
    void SwapScreens()
    {
        if (DisplayServer.GetScreenCount() <= 1) return;
        bool onMain = DisplayServer.WindowGetCurrentScreen() == DisplayServer.GetPrimaryScreen();
        string choice = onMain ? "other" : "main";
        database?.SetSetting("screen", choice);
        MoveToScreen(PickGameScreen(DisplayServer.GetScreenCount(), DisplayServer.GetPrimaryScreen(), choice));
    }

    /// <summary>Puts the window on a screen. (A full-screen window has to become a normal window for a moment to move.)</summary>
    static void MoveToScreen(int screen)
    {
        if (DisplayServer.WindowGetCurrentScreen() == screen) return;
        var mode = DisplayServer.WindowGetMode();
        bool full = mode is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
        if (full) DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetCurrentScreen(screen);
        if (full) DisplayServer.WindowSetMode(mode);
    }

    void ToggleMusic()
    {
        sounds.SetMusicOn(!sounds.MusicOn);
        database?.SetSetting("music", sounds.MusicOn ? "on" : "off");
    }

    const int AnyDevice = -1; // -1 means "any keyboard, any mouse, any controller"

    static void AddAction(string name, params InputEvent[] inputs)
    {
        if (InputMap.HasAction(name)) return;
        InputMap.AddAction(name);
        foreach (var input in inputs) InputMap.ActionAddEvent(name, input);
    }

    static InputEventKey KeyPress(Key key) => new() { PhysicalKeycode = key, Device = AnyDevice };

    // ---------- Which controller is whose? ----------

    /// <summary>
    /// Which controller is Bolt-E's (controller one), from the numbers of the controllers that are plugged in:
    /// the lowest number that Godot knows is a real game controller. (Some keyboards, mice and steering wheels pretend to
    /// be a controller too, and those must never steal Bolt-E!) If Godot knows none of them: the lowest number.
    /// Nothing plugged in: 0, so the first controller that gets plugged in works right away.
    /// </summary>
    static int PickControllerOne(IReadOnlyList<int> connected, Func<int, bool> isKnown)
    {
        var lowestFirst = connected.OrderBy(device => device).ToList();
        foreach (int device in lowestFirst)
            if (isKnown(device)) return device;
        return lowestFirst.Count > 0 ? lowestFirst[0] : 0;
    }

    /// <summary>
    /// Bolt-E's controller right now. (During the self-test it's always its pretend controller 99, so a real one can't
    /// change a test. Only a test that pretends some controllers are plugged in changes it: see testPluggedIn.)
    /// </summary>
    int PickBoltEsController() =>
        selfTesting && testPluggedIn is null ? SelfTestDevice : PickControllerOne(PluggedInControllers(), PadIsKnown);

    /// <summary>The numbers of the controllers that are plugged in right now. (A test can pretend: see testPluggedIn.)</summary>
    IReadOnlyList<int> PluggedInControllers() => testPluggedIn ?? Input.GetConnectedJoypads().ToList();

    /// <summary>
    /// Is this a real game controller that Godot knows (Godot knows where all its buttons are)? Phantom joysticks, some
    /// steering wheels, and keyboards or mice that say they're a controller aren't. They can't drive Big Rusty, and the
    /// title card doesn't count them. (The self-test's pretend controllers 99 and 98 count as real ones, and a test can
    /// pretend: see testKnownPads.)
    /// </summary>
    bool PadIsKnown(int device)
    {
        if (testKnownPads is not null) return testKnownPads(device);
        if (selfTesting && device is SelfTestDevice or SelfTestSecondPad) return true;
        return Input.IsJoyKnown(device);
    }

    /// <summary>
    /// Makes "device" Bolt-E's controller: from now on, the controller buttons and the stick of the kid's actions (and of
    /// Godot's own ui_ actions) only listen to it. The keyboard and the mouse aren't changed. Anything that was held down
    /// on the old controller is let go, so nothing stays stuck down (like Bolt-E riding right all by himself).
    /// </summary>
    void BindControllerOne(int device)
    {
        controllerOne = device;
        foreach (var action in InputMap.GetActions())
        {
            string name = action;
            if (!KidActions.Contains(name) && !name.StartsWith("ui_")) continue;
            bool hasControllerParts = false;
            foreach (var input in InputMap.ActionGetEvents(action))
            {
                if (input is not (InputEventJoypadButton or InputEventJoypadMotion)) continue;
                input.Device = device; // (this changes the button inside the action itself)
                hasControllerParts = true;
            }
            if (hasControllerParts) Input.ActionRelease(action);
        }
    }

    void OnControllerPluggedOrUnplugged(long device, bool connected) => UpdateControllerStatus();

    /// <summary>
    /// A controller was plugged in or unplugged: which one is Bolt-E's now (his controls follow it when it changed),
    /// and the title card's controller line.
    /// </summary>
    void UpdateControllerStatus()
    {
        int one = PickBoltEsController();
        if (one != controllerOne) BindControllerOne(one);

        // The controllers' names, Bolt-E's first. Only real controllers count (and Bolt-E's own, of course), so a
        // phantom one that Godot doesn't know never makes the title card say "2 controllers!"
        var names = PluggedInControllers().Where(device => device == one || PadIsKnown(device))
                                          .OrderBy(device => device == one ? 0 : 1).ThenBy(device => device)
                                          .Select(ControllerName).ToList();
        hud.SetControllers(names);
    }

    /// <summary>A controller's name. (Windows calls every Xbox controller "XInput Controller".)</summary>
    static string ControllerName(int device)
    {
        string name = Input.GetJoyName(device);
        return name.Contains("XInput", StringComparison.OrdinalIgnoreCase) ? "Xbox controller" : name;
    }

    /// <summary>
    /// Makes Bolt-E's controller shake (only his: Dad's controller doesn't buzz for every bolt the kid grabs).
    /// "weak" is a fast buzz, "strong" is a big rumble (0 to 1).
    /// </summary>
    void Rumble(float weak, float strong, float seconds) => Buzz(controllerOne, weak, strong, seconds);

    /// <summary>Makes one controller shake, if it's plugged in (and the rumble is on).</summary>
    void Buzz(int pad, float weak, float strong, float seconds)
    {
        lastBuzzedPad = pad;
        if (rumbleOn && Input.GetConnectedJoypads().Contains(pad))
            Input.StartJoyVibration(pad, weak, strong, seconds);
    }

    void OpenDatabase()
    {
        // "user://" is a special folder Godot gives every game for saving things.
        // On Windows it's %APPDATA%\Godot\app_userdata\Robot Dash\
        string path = ProjectSettings.GlobalizePath("user://robotdash.db");

        // To use a different database (for testing), start the game with:  -- --db=C:\some\folder\test.db
        foreach (string arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--db=")) path = arg["--db=".Length..];
        try
        {
            database = new ScoreDatabase(path);
            GD.Print($"Score database: {path}");
        }
        catch (Exception e)
        {
            GD.PushError($"Could not open database at {path}: {e.Message}");
        }
    }

    public override void _ExitTree()
    {
        if (watchingControllers) Input.JoyConnectionChanged -= OnControllerPluggedOrUnplugged;
        database?.Dispose();
    }

    // ---------- Changing between title / playing / game over ----------

    /// <summary>
    /// Takes everything away for a fresh start: obstacles, bolts, enemies, ? boxes, Big Rusty and his fireballs,
    /// shockwaves and lasers, the flags on the road, the floating numbers and the little puffs. Any power stops (and so
    /// does the slot machine), and we're back in Sunny Hills with normal gravity. The Bolt Bank stops counting and waiting
    /// unlock parties are called off (they come back on the next menu: see Main.Looks.cs).
    /// It also starts a new run number, so anything still waiting to happen from the old game (see Later) is skipped.
    /// </summary>
    void ClearWorld()
    {
        StopPowersNow();
        CancelCelebrations();
        ResetWorlds(); // back to Sunny Hills (see Main.Worlds.cs)
        ClearFlags();  // (see Main.Family.cs)
        ClearJumpDust();
        foreach (var o in obstacles) o.QueueFree();
        foreach (var b in bolts) b.QueueFree();
        foreach (var e in enemies) e.QueueFree();
        foreach (var b in boxes) b.QueueFree();
        foreach (var b in blasts) b.QueueFree();
        foreach (var w in shockwaves) w.QueueFree();
        foreach (var l in lasers) l.QueueFree();
        boss?.QueueFree();
        obstacles.Clear();
        bolts.Clear();
        enemies.Clear();
        boxes.Clear();
        blasts.Clear();
        shockwaves.Clear();
        lasers.Clear();
        boss = null;
        sounds.Stop(Sfx.VacuumHum); // (VROOMBA's vacuum, if it was still humming)
        popups.Clear();
        particles.Clear();
        runNumber++;
    }

    /// <summary>
    /// Does something a little later (in game seconds), but only if it's still the same game by then.
    /// If a new game started or we went back to the title, it's skipped, so nothing from an old game pops up in a new one.
    /// </summary>
    void Later(double seconds, Action action)
    {
        int run = runNumber;
        GetTree().CreateTimer(seconds).Timeout += () => { if (run == runNumber) action(); };
    }

    /// <summary>
    /// The title screen: "Family Champions", and who's playing (the last player picked). With typing = true
    /// (NEW PLAYER picked on the results card) the name box is ready to type a new name.
    /// </summary>
    void GoToTitle(bool typing = false)
    {
        ClearWorld();
        Engine.TimeScale = 1;
        robot.HomeX = MenuHomeX; // on the menus Bolt-E stands in the corner on the left
        robot.Reset();
        sounds.StartMusic(CurrentWorld.MusicSpeed); // (Sunny Hills' music speed: see Worlds.cs)
        showResults = null;

        state = GameState.Title;
        speed = 150f; // the world rolls by slowly behind the title screen
        bestScore = database?.BestScore() ?? 0;
        hud.ShowTitle(database?.PlayerBests(ChampionsShown) ?? new());
        hud.SetPlayers(database?.KnownPlayers(KnownPlayersShown) ?? new(), database?.GetSetting("player_name") ?? "");
        if (typing) hud.StartTyping();
        // This player's Bolt Bank, look and postcards, and any unlock parties still waiting (see Main.Looks.cs).
        // (No parties while a new name is being typed.)
        LoadPlayer(celebrate: !hud.IsTypingName);
    }

    void StartGame()
    {
        if (state is GameState.Playing or GameState.Rebuilding) return;

        hud.StopTyping(); // (a name still being typed is the player now, if it has at least 2 letters)
        string? before = previousPlayer;
        ClearWorld();
        database?.SetSetting("player_name", hud.PlayerName);
        GetLookReadyToRide(); // a look he was only trying on comes off (see Main.Looks.cs)
        bestScore = database?.BestScore() ?? 0;
        speedToGetBackTo = 0;

        state = GameState.Playing;
        Engine.TimeScale = 1;
        showResults = null;
        speed = StartSpeed;
        distance = 0;
        boltsCollected = 0;
        playTime = 0;
        spawnCountdown = 500;
        boltCombo = 0;
        bonusPoints = 0;
        stomps = 0;
        stompCombo = 0;
        bossesBeaten = 0;
        gameNumber++;          // the bosses take turns: who comes first depends on the game number (see Main.Vroomba.cs)
        bossFightNumber = 0;
        nextBossAt = FirstBossAt;
        bossWarningShown = false;
        nextBoxAt = PowerUps.FirstBoxAt;
        spareBatteries = StartingBatteries;
        boltsTowardBattery = 0;
        batteriesUsed = 0;
        rebuildTimer = 0;
        rebuildStarted = batteryFlown = crashedInBossFight = false;
        robot.HomeX = RobotX; // back to his riding spot
        robot.Reset();
        hud.ShowPlaying();
        hud.SetBatteries(spareBatteries, 0);
        sounds.Play(Sfx.Start);
        sounds.StartMusic(CurrentWorld.MusicSpeed); // (every game starts in Sunny Hills, at its music speed)

        // The family race (see Main.Family.cs): the flags on the road, and "GO, MAX!" or "DAD'S TURN!"
        // (at the very end, because ShowPlaying hides the big words)
        highScoreCheered = false;
        BuildFlags();
        previousPlayer = hud.PlayerName;
        SayWhoseTurn(before);
    }

    void GameOver()
    {
        state = GameState.GameOver;
        gameOverTimer = 0;
        shake = 0.5f;
        StopPowersNow(); // any power ends right away, quietly (see Main.PowerUps.cs)
        robot.CanMove = false;
        robot.MoveInput = 0;
        robot.Crash();
        Rumble(0.6f, 1.0f, 0.6f); // big rumble: KABOOM!
        sounds.Play(Sfx.Explosion);
        sounds.PowerDownMusic();
        boss?.FlyAway(); // Big Rusty zooms off
        hud.HideBossBar();

        // Slow motion for a moment, so you can see the boom! (TimeScale 0.3 = everything moves at 30% speed.)
        // The timer ignores the time scale, so it ends after 0.35 real seconds.
        Engine.TimeScale = 0.3;
        GetTree().CreateTimer(0.35, ignoreTimeScale: true).Timeout += () =>
        {
            if (state == GameState.GameOver) Engine.TimeScale = 1;
        };

        int score = Score;
        int meters = Meters;
        bool newBest = score > bestScore;
        long runId = -1;
        PlayerStats? stats = null;
        int farthestBefore = 1, farthestNow = 1; // the most worlds this player ever reached, before and after this game
        int bankBefore = 0, bankAfter = 0;       // the player's Bolt Bank before and after this game (see Main.Looks.cs)
        string player = hud.PlayerName;
        string? roundWinner = DecideRound(player, score);      // taking turns: who won this round? (see Main.Family.cs)
        var today = new List<(string Name, int Score)>();      // everyone's best score today (this game too)
        try
        {
            if (database is not null)
            {
                farthestBefore = database.FarthestWorld(hud.PlayerName); // (read BEFORE saving, so we know which postcards are NEW)
                bankBefore = bankAfter = database.StatsFor(hud.PlayerName).TotalBolts; // (and so the bank can count up)
                runId = database.SaveRun(hud.PlayerName, score, meters, boltsCollected, stomps, bossesBeaten, playTime,
                                         batteriesUsed, worldsReached);
                stats = database.StatsFor(hud.PlayerName);
                bankAfter = stats.TotalBolts;
                farthestNow = database.FarthestWorld(hud.PlayerName);
                today = database.TodaysBests();
            }
        }
        catch (Exception e)
        {
            GD.PushError($"Could not save the score: {e.Message}");
        }

        // The World Tour postcards: every world reached (in this game too, even if it couldn't be saved),
        // and the ones reached for the very first time get a gold NEW! ribbon.
        int postcardsReached = Math.Min(Worlds.All.Length, Math.Max(worldsReached, farthestNow));
        int postcardsNewFrom = Math.Min(Worlds.All.Length, farthestBefore);
        string worldLine = Worlds.WorldLine(worldsReached);

        // Don't cover up the explosion: show the scores a little later (see _Process).
        var topScores = database?.TopScores(5) ?? new();
        int boltsThisGame = boltsCollected, stompsThisGame = stomps, bossesThisGame = bossesBeaten;
        string title = newBest ? "NEW HIGH SCORE!"
                     : DriverGotYou ? $"{DriverShortName} GOT YOU!" // Dad was driving Big Rusty (see Main.Driver.cs)
                     : batteriesUsed > 0 ? "Out of batteries!"
                     : "Bonk!";
        string? todayLine = TodayLine(today, roundWinner); // "Today: MAX 1520 (champ!)  -  DAD 980  -  Round to MAX!"
        string closeLine = SoCloseLine(meters);            // "So close! Only 42 m to DAD's flag!"
        showResults = () =>
        {
            hud.ShowGameOver(new ResultsInfo
            {
                Title = title,
                Score = score,
                Meters = meters,
                Bolts = boltsThisGame,
                Stomps = stompsThisGame,
                Bosses = bossesThisGame,
                Stats = stats,
                TopScores = topScores,
                RunId = runId,
                WorldLine = worldLine,
                TodayLine = todayLine,
                CloseLine = closeLine,
            });
            StartBankCountUp(player, bankBefore, bankAfter); // Bolt-E's corner, and the Bolt Bank counts up (see Main.Looks.cs)
            hud.ShowPostcards(postcardsReached, postcardsNewFrom);
            sounds.Play(newBest ? Sfx.HighScore : Sfx.GameOver);
        };
    }

    // ---------- Input ----------

    public override void _UnhandledInput(InputEvent e)
    {
        // During the self-test only its pretend presses count (from its pretend controller one, and its pretend second
        // controller), so a real controller or keyboard can't mess up the tests
        if (selfTesting && e.Device != SelfTestDevice && e.Device != SelfTestSecondPad) return;

        // A SECOND controller (any controller that isn't Bolt-E's) never moves Bolt-E: in a boss fight it drives
        // Big Rusty, and that's all it does (see Main.Driver.cs). A phantom one that Godot doesn't know does nothing.
        if ((e is InputEventJoypadButton or InputEventJoypadMotion) && e.Device != controllerOne)
        {
            SecondControllerInput(e);
            return;
        }

        // F11 switches between full screen (how the game always starts) and a window, and back again
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F11 })
        {
            ToggleFullScreen();
            return;
        }

        // F9 moves the game to the other screen (when there are two), and remembers it for next time
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F9 })
        {
            SwapScreens();
            return;
        }

        // While someone types a name, the keyboard is for typing. (A controller always works.)
        if (e is InputEventKey && hud.IsTypingName) return;

        if (e.IsActionPressed("music"))
        {
            ToggleMusic();
            return;
        }

        // On the menus, LEFT / RIGHT change Bolt-E's look (one push of the stick = one step, see Main.Looks.cs),
        // and LB / RB (or TAB) change who's playing (see Main.Family.cs). Only TAB (a key) can pick NEW PLAYER:
        // the controller never ends up in the name box.
        if (MenuInputOn)
        {
            if (Input.IsActionJustPressedByEvent("menu_left", e))
            {
                BrowseLook(-1);
                return;
            }
            if (Input.IsActionJustPressedByEvent("menu_right", e))
            {
                BrowseLook(1);
                return;
            }
            if (Input.IsActionJustPressedByEvent("player_prev", e))
            {
                PickPlayer(-1, canPickNew: e is InputEventKey);
                return;
            }
            if (Input.IsActionJustPressedByEvent("player_next", e))
            {
                PickPlayer(1, canPickNew: e is InputEventKey);
                return;
            }
        }

        // On the menus, D-pad up never starts a game (it's easy to press by accident while pressing left or right)
        if ((state is GameState.Title or GameState.GameOver) && e is InputEventJoypadButton { ButtonIndex: JoyButton.DpadUp }) return;

        bool jump = e.IsActionPressed("jump");
        bool start = e.IsActionPressed("start");
        if (jump || start)
        {
            switch (state)
            {
                case GameState.Title:
                    PlayPressed();
                    break;
                case GameState.Playing:
                    if (jump && robot.Jump())
                    {
                        LeaveJumpDust();
                        Burst(robot.Position, 8, new Color(0.9f, 0.85f, 0.75f), 120f);
                        sounds.Play(robot.DidDoubleJump ? Sfx.DoubleJump : Sfx.Jump);
                    }
                    break;
                case GameState.Rebuilding:
                    break; // mashing buttons can't skip the magic rebuild (or restart the game)
                case GameState.GameOver:
                    // Only after the scores are showing, so you don't skip them by accident.
                    // (With NEW PLAYER picked, it goes to the title card to type the new name: see PlayPressed.)
                    if (gameOverTimer > ResultsDelay + 0.3f) PlayPressed();
                    break;
            }
        }
        else if (e.IsActionReleased("jump") && state == GameState.Playing)
        {
            robot.ReleaseJump();
        }
    }

    /// <summary>
    /// Left (-1) to right (1) from the arrow keys, A / D, or the D-pad or stick of Bolt-E's controller.
    /// (The self-test pretends with testMoveAxis, except in the test that checks the real controls: see testRealStick.)
    /// </summary>
    float MoveAxis() => selfTesting && !testRealStick ? testMoveAxis : Input.GetAxis("left", "right");

    /// <summary>Is jump being held down? (Never during the self-test, so a real controller can't change a test.)</summary>
    bool JumpHeld() => !selfTesting && Input.IsActionPressed("jump");

    // ---------- Every frame ----------

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        UpdateJumpDust(dt);

        if (state == GameState.Playing)
        {
            playTime += dt;
            if (BossFightActive)
                speed = Mathf.MoveToward(speed, 0, 500f * dt);                 // the world stops for the boss fight
            else if (speed < speedToGetBackTo)
                speed = Mathf.MoveToward(speed, speedToGetBackTo, 450f * dt);  // speeding back up after a boss fight or a rebuild
            else
                speed = Mathf.Min(MaxSpeed, speed + SpeedUpPerSecond * dt);
            robot.CanMove = BossFightActive;
            robot.MoveInput = MoveAxis();
            if (boss is not null) boss.TargetX = robot.Position.X;
            boost = Mathf.MoveToward(boost, power == PowerUp.Rocket ? PowerUps.RocketBoost : 1f, 1.5f * dt); // the rocket board rushes along!
            distance += speed * boost * dt;
            SpawnThings(dt);
            UpdatePowerUps(dt); // (see Main.PowerUps.cs)
            UpdateVroomba(dt);  // VROOMBA's vacuum pulls Bolt-E and the bolts (see Main.Vroomba.cs)
            CheckBumps();
            hud.SetScore(Score, boltsCollected, bestScore);
            CheckForHighScore(); // NEW HIGH SCORE! right away (see Main.Family.cs)
        }
        else if (state == GameState.Rebuilding)
        {
            UpdateRebuild(dt); // Bolt-E is putting himself back together (see Main.Batteries.cs)
        }
        else if (state == GameState.GameOver)
        {
            gameOverTimer += dt;
            speed = Mathf.MoveToward(speed, 0, 900f * dt); // skid to a stop
            if (showResults is not null && gameOverTimer > ResultsDelay)
            {
                showResults();
                showResults = null;
            }
        }

        robot.RunSpeed = speed * boost; // (so his music notes drift along with the world)
        MoveWorld(dt);
        // Flags come onto the road, and YOU PASSED DAD! (see Main.Family.cs). (After the road moved, so a new flag
        // stands exactly at its meters. While Bolt-E is in pieces the road still slides along, so flags still come.)
        if (state is GameState.Playing or GameState.Rebuilding) UpdateFlags();

        // Puff of dust (and a soft "thup") when landing
        if (robot.OnGround && !wasOnGround && !robot.Crashed)
        {
            Burst(robot.Position, 10, new Color(0.9f, 0.85f, 0.75f), 150f);
            sounds.Play(Sfx.Land);
            stompCombo = 0; // touching the ground ends a stomp streak
            LandedWithPowers(); // after the parachute, or MEGA Bolt-E shaking the ground (see Main.PowerUps.cs)
        }
        wasOnGround = robot.OnGround;

        UpdateParticles(dt);
        UpdateWorlds(dt); // the colors change into a new world, and snow falls (see Main.Worlds.cs)
        UpdateLooks(dt);  // the Bolt Bank counts up, and unlock parties (see Main.Looks.cs)

        // Screen shake after a bump
        shake = Mathf.Max(0, shake - dt);
        Position = shake > 0 ? new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0)) * shake * 30f : Vector2.Zero;

        QueueRedraw();
    }

    void MoveWorld(float dt)
    {
        float worldSpeed = speed * boost; // (boost is 1.5 on the rocket board, so everything rushes by faster)
        float move = worldSpeed * dt;
        cloudScroll += move * 0.1f;
        farHillScroll += move * 0.25f;
        nearHillScroll += move * 0.5f;
        groundScroll += move;

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            obstacles[i].Position -= new Vector2(move, 0);
            if (obstacles[i].Position.X < -150 || obstacles[i].Finished) // (Finished = smashed pieces are done bouncing)
            {
                obstacles[i].QueueFree();
                obstacles.RemoveAt(i);
            }
        }

        for (int i = bolts.Count - 1; i >= 0; i--)
        {
            bolts[i].Position -= new Vector2(move, 0);
            if (power == PowerUp.Magnet) PullTowardMagnet(bolts[i], dt);
            if (bolts[i].Position.X < -150)
            {
                bolts[i].QueueFree();
                bolts.RemoveAt(i);
            }
        }

        for (int i = boxes.Count - 1; i >= 0; i--)
        {
            boxes[i].Position -= new Vector2(move, 0);
            if (boxes[i].Position.X < -150)
            {
                boxes[i].QueueFree();
                boxes.RemoveAt(i);
            }
        }

        // The flags stand on the road, so they move with it (see Main.Family.cs)
        for (int i = flags.Count - 1; i >= 0; i--)
        {
            flags[i].Position -= new Vector2(move, 0);
            if (flags[i].Position.X < -200)
            {
                flags[i].QueueFree();
                flags.RemoveAt(i);
            }
        }

        // Enemies move with the ground, plus a bit extra because they're coming at you!
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (enemy.Chaser && !enemy.Squashed)
            {
                // Big Rusty's helpers (and VROOMBA's dust bunnies) chase Bolt-E
                float direction = robot.Position.X < enemy.Position.X ? -1 : 1;
                enemy.Facing = direction;
                enemy.Position += new Vector2(direction * enemy.ChaseSpeed * dt - move, 0);
            }
            else
            {
                enemy.Position -= new Vector2(move + enemy.ExtraSpeed * dt, 0);
            }
            if (enemy.Position.X < -150 || enemy.Position.X > ScreenWidth + 400 || enemy.Finished)
            {
                enemy.QueueFree();
                enemies.RemoveAt(i);
            }
        }

        for (int i = blasts.Count - 1; i >= 0; i--)
        {
            blasts[i].Advance(dt, worldSpeed);
            if (blasts[i].Position.X < -100 || blasts[i].Position.X > ScreenWidth + 100)
            {
                blasts[i].QueueFree();
                blasts.RemoveAt(i);
            }
        }

        for (int i = shockwaves.Count - 1; i >= 0; i--)
        {
            shockwaves[i].Advance(dt, worldSpeed);
            if (shockwaves[i].Position.X < -100 || shockwaves[i].Position.X > ScreenWidth + 100)
            {
                shockwaves[i].QueueFree();
                shockwaves.RemoveAt(i);
            }
        }

        for (int i = lasers.Count - 1; i >= 0; i--)
        {
            if (lasers[i].Finished)
            {
                lasers[i].QueueFree();
                lasers.RemoveAt(i);
            }
        }

        if (boss is not null)
        {
            boss.WorldSpeed = worldSpeed;
            if (boss.Finished)
            {
                boss.QueueFree();
                boss = null;
            }
        }
    }

    void CheckBumps()
    {
        var robotBox = robot.Hitbox;
        robotWasFalling = robot.IsFalling; // remember this before any bounce, so landing on two enemies at once squashes both

        // Every crash goes through Bonk. It returns true if Bolt-E really crashed (then we stop checking).
        foreach (var obstacle in obstacles)
        {
            if (obstacle.Smashed) continue; // already in pieces (MEGA Bolt-E smashed it)
            if (robotBox.Intersects(obstacle.Hitbox))
            {
                if (Bonk(obstacle)) return;
            }
        }

        // Bad robots: land on their heads to squash them, but bumping into them is a crash!
        // (Up on the rocket board or the parachute, they can't reach Bolt-E at all.)
        bool upHigh = RocketSafe;
        foreach (var enemy in enemies)
        {
            if (upHigh || enemy.Squashed || !robotBox.Intersects(enemy.Hitbox)) continue;
            if (CameDownOnTop(enemy.Top))
            {
                StompEnemy(enemy);
            }
            else
            {
                if (Bonk(enemy)) return;
            }
        }

        foreach (var blast in blasts)
        {
            if (robotBox.Intersects(blast.Hitbox))
            {
                if (Bonk(blast)) return;
            }
        }

        foreach (var wave in shockwaves)
        {
            if (robotBox.Intersects(wave.Hitbox))
            {
                if (Bonk(wave)) return;
            }
        }

        foreach (var laser in lasers)
        {
            if (laser.Firing && robotBox.Intersects(laser.Hitbox))
            {
                if (Bonk(laser)) return;
            }
        }

        if (boss is not null && robotBox.Intersects(boss.Hitbox))
        {
            if (boss.CanBeStomped && CameDownOnTop(boss.Top))
            {
                StompBoss();
            }
            else if (boss.IsDangerous) // (bumping into him while he's dizzy doesn't hurt)
            {
                if (Bonk(boss)) return;
            }
        }

        for (int i = bolts.Count - 1; i >= 0; i--)
        {
            if (bolts[i].Touches(robotBox))
            {
                var at = bolts[i].Position;
                bolts[i].QueueFree();
                bolts.RemoveAt(i);
                GotBolt(at);
            }
        }

        // Rainbow ? boxes: jump into one and the slot machine spins! (see Main.PowerUps.cs)
        for (int i = boxes.Count - 1; i >= 0; i--)
        {
            if (boxes[i].Touches(robotBox)) OpenBox(boxes[i]);
        }
    }

    /// <summary>
    /// BONK! Bolt-E bumped into something dangerous ("thing", or null for a pretend crash that no power can stop).
    /// This is the only way to crash. While Bolt-E is blinking after a rebuild nothing happens (returns false).
    /// MEGA Bolt-E smashes things instead of crashing, and nothing can reach him on the rocket board or the parachute.
    /// With a spare battery he blows up and puts himself back together; without one, it's game over.
    /// </summary>
    bool Bonk(Node2D? thing)
    {
        // MEGA BOLT-E is too big to crash: SMASH! and SPLAT! instead (see Main.PowerUps.cs)
        if (thing is not null && robot.Size > 1.05f)
        {
            if (thing is Obstacle obstacle) SmashObstacle(obstacle);
            else if (thing is Enemy enemy) SplatEnemy(enemy);
            return false;
        }
        // Up on the rocket board (or floating down on the parachute), nothing can reach him
        if (thing is not null && RocketSafe) return false;

        if (state != GameState.Playing || robot.BlinkTime > 0) return false;
        if (spareBatteries > 0) StartRebuild();
        else GameOver();
        return true;
    }

    /// <summary>Got a bolt! Count it, smile, play the next note up the scale, sparkle, and charge the spare battery.</summary>
    void GotBolt(Vector2 at)
    {
        boltsCollected++;
        robot.BeHappy();
        Rumble(0.3f, 0f, 0.06f); // tiny buzz: got a bolt!

        // Bolts grabbed quickly one after another go up the scale
        boltCombo = playTime - lastBoltTime < 0.6f ? boltCombo + 1 : 0;
        lastBoltTime = playTime;
        int semitones = ScaleSteps[Math.Min(boltCombo, ScaleSteps.Length - 1)];
        sounds.Play(Sfx.Bolt, Mathf.Pow(2f, semitones / 12f));
        Burst(at, 10, new Color(1f, 0.85f, 0.25f), 220f);

        ChargeSpareBattery(); // every 100 bolts = a new spare battery (see Main.Batteries.cs)
    }

    // ---------- Stomping (like in Mario!) ----------

    /// <summary>True if Bolt-E is falling and its feet were above this spot a moment ago (so it came from above, not the side).</summary>
    bool robotWasFalling;

    bool CameDownOnTop(float top) => robotWasFalling && robot.LastFeetY <= top + StompForgiveness;

    void StompEnemy(Enemy enemy)
    {
        enemy.Stomp();
        stomps++;
        stompCombo++;
        int points = PointsPerStomp * (1 << Math.Min(stompCombo - 1, 3)); // 25, 50, 100, 200
        bonusPoints += points;
        popups.Add($"+{points}", new Vector2(enemy.Position.X, enemy.Top - 10), new Color(1f, 0.85f, 0.25f));
        robot.Bounce(high: JumpHeld()); // hold jump to bounce higher
        sounds.Play(Sfx.Stomp, 1f + 0.12f * (stompCombo - 1));
        Rumble(0.5f, 0.2f, 0.1f);
        Burst(new Vector2(enemy.Position.X, enemy.Top + 10), 12, Colors.White, 200f);
    }

    void StompBoss()
    {
        boss!.Stomp();
        robot.Bounce(high: true);
        bonusPoints += PointsPerBossHit;
        popups.Add($"+{PointsPerBossHit}", new Vector2(boss.Position.X, boss.Top - 10), new Color(1f, 0.6f, 0.15f), 36);
        sounds.Play(Sfx.BossHit);
        shake = 0.25f;
        Rumble(0.6f, 0.6f, 0.25f);
        if (boss.DriverControlled) BuzzTheDriver(); // Dad feels the stomp on his controller too (see Main.Driver.cs)
        Burst(new Vector2(boss.Position.X, boss.Top + 10), 16, new Color(1f, 0.85f, 0.3f), 260f);
        hud.ShowBossBar(boss.Health);
        if (boss.IsBeaten)
        {
            // The last stomp! Slow motion while he shakes and pops... then KABOOM (see BossBeaten)
            Engine.TimeScale = 0.4;
            GetTree().CreateTimer(0.6, ignoreTimeScale: true).Timeout += () =>
            {
                if (state == GameState.Playing) Engine.TimeScale = 1;
            };
        }
    }

    void BossBeaten()
    {
        // He blew up after Bolt-E's last crash: the game is already over and saved,
        // so no more points (and the music keeps powering down).
        if (state == GameState.GameOver) return;

        bossesBeaten++;
        bonusPoints += PointsForBeatingBoss;
        popups.Add($"+{PointsForBeatingBoss}", boss!.Position + new Vector2(0, -200), new Color(1f, 0.85f, 0.2f), 46);
        // (When Dad was driving him: "YOU BEAT DAD'S BIG RUSTY!" - see Main.Driver.cs)
        hud.ShowBanner(boss.DriverControlled ? $"YOU BEAT {DriverShortName}'S {boss.DisplayName}!" : $"YOU BEAT {boss.DisplayName}!",
                       new Color(1f, 0.85f, 0.2f));
        sounds.Play(Sfx.Explosion);
        sounds.Play(Sfx.Victory);
        sounds.SetBossMusic(false, CurrentWorld.MusicSpeed); // back to the runner theme after any boss
        shake = 0.5f;
        Rumble(0.8f, 1f, 0.6f);
        NextWorldSoon(); // ...and then on to the next world! (see Main.Worlds.cs)

        // A rainbow of bolts as a prize!
        for (int i = 0; i < 12; i++)
            AddBolt(RobotX + 350 + i * 55, GroundY - 40 - 170 * Mathf.Sin(i / 11f * Mathf.Pi));

        GetTree().CreateTimer(1.5).Timeout += () => { if (!BossFightActive) hud.HideBossBar(); };
        nextBossAt = Meters + MetersBetweenBosses;
        bossWarningShown = false;
        spawnCountdown = 1200; // a little rest before obstacles come back
    }

    /// <summary>
    /// A boss arrives! With no kind given, the bosses take turns (see Main.Vroomba.cs): BIG RUSTY or VROOMBA.
    /// </summary>
    void SpawnBoss(BossKind? kind = null)
    {
        EndPowerUp(quiet: true); // (just in case: powers already end when the boss warning sounds)

        // Remember how fast to go after the fight. (If Bolt-E was just rebuilt and is still speeding back up,
        // keep aiming for that speed instead of the slow speed right now.)
        speedToGetBackTo = Mathf.Max(speedToGetBackTo, Mathf.Max(speed, StartSpeed));

        // The boss's arrival blows away anything still on the screen
        foreach (var o in obstacles)
        {
            Burst(o.Position + new Vector2(0, -30), 10, Colors.White, 200f);
            o.QueueFree();
        }
        obstacles.Clear();
        foreach (var e in enemies)
        {
            Burst(e.Position + new Vector2(0, -40), 10, Colors.White, 200f);
            e.QueueFree();
        }
        enemies.Clear();
        PuffAway(boxes, _ => true); // (no ? boxes in a boss fight)

        bossFightNumber++;
        BossBase theBoss = (kind ?? NextBossKind()) == BossKind.Vroomba ? MakeVroomba() : MakeBigRusty();
        boss = theBoss;

        // What every boss does: gets angry, pops, blows up and his pieces bounce
        theBoss.GotAngry += phase =>
        {
            hud.ShowBanner(phase == 2 ? $"{theBoss.DisplayName} IS ANGRY!" : $"{theBoss.DisplayName} IS FURIOUS!", new Color(1f, 0.35f, 0.25f));
            sounds.Play(Sfx.Roar, theBoss is Vroomba ? 1.5f : 1f); // (VROOMBA's roar is a higher, robot-vacuum "GRRR!")
            sounds.SetMusicSpeed((phase == 2 ? 1.15f : 1.22f) * CurrentWorld.MusicSpeed);
            shake = 0.35f;
            Rumble(0.5f, 0.6f, 0.5f);
        };
        theBoss.Popped += at =>
        {
            Burst(at, 10, new Color(1f, 0.8f, 0.3f), 260f);
            sounds.Play(Sfx.Explosion, Rand(1.8f, 2.4f));
            shake = 0.2f;
        };
        theBoss.BlewUp += BossBeaten;
        theBoss.PieceBounced += bigPiece => sounds.Play(Sfx.Clink, bigPiece ? Rand(0.45f, 0.6f) : Rand(1.1f, 1.5f));

        AddChild(theBoss);
        hud.SetBossBarName(theBoss.DisplayName); // (nobody drives a new Big Rusty yet: see Main.Driver.cs)
        driverPad = -1;
        hud.ShowBossBar(theBoss.Health);
        hud.ShowHint(theBoss is Vroomba
            ? "Jump to leave dust! Lure him, then STOMP when clogged!  ← → / stick to ride"
            : "Ride left and right with  ← →  or the stick!");
        sounds.SetBossMusic(true, 1.1f * CurrentWorld.MusicSpeed); // a dedicated battle theme for every boss
    }

    /// <summary>BIG RUSTY flies in, dressed up for the world! (Everything only he does is hooked up here.)</summary>
    Boss MakeBigRusty()
    {
        var rusty = new Boss { GroundY = GroundY, ZIndex = 5, Outfit = CurrentWorld.RustyOutfit }; // dressed up for the world!

        rusty.Laughed += () =>
        {
            popups.Add("HA HA HA!", rusty.Position + new Vector2(0, -235), new Color(1f, 0.6f, 0.15f), 36);
            sounds.Play(Sfx.Laugh);
        };
        rusty.Fired += (at, direction) =>
        {
            var blast = new Blast { GroundY = GroundY, Position = at, Velocity = new Vector2(direction * (rusty.Phase == 3 ? 420 : 360), 0) };
            AddChild(blast);
            blasts.Add(blast);
            sounds.Play(Sfx.Shoot);
        };
        rusty.HelperDropped += at =>
        {
            var helper = new Enemy { Kind = EnemyKind.PupBot, Position = at, GroundY = GroundY, Chaser = true };
            AddChild(helper);
            enemies.Add(helper);
            sounds.Play(Sfx.Jump, 0.6f);
        };
        rusty.LaserFired += (at, direction) =>
        {
            var laser = new Laser { Position = at, Direction = direction, ZIndex = 6 };
            laser.StartedFiring += () =>
            {
                sounds.Play(Sfx.LaserBeam);
                shake = 0.3f;
                Rumble(0.4f, 0.3f, 0.6f);
            };
            AddChild(laser);
            lasers.Add(laser);
            sounds.Play(Sfx.LaserCharge);
        };
        rusty.Warned += () => sounds.Play(Sfx.BossAlarm);
        rusty.Slammed += x =>
        {
            // Two shockwaves roll away from where he landed
            foreach (float direction in new[] { -1f, 1f })
            {
                var wave = new Shockwave { Direction = direction, Position = new Vector2(x + direction * 70, GroundY) };
                AddChild(wave);
                shockwaves.Add(wave);
            }
            sounds.Play(Sfx.Slam);
            shake = 0.45f;
            Rumble(0.7f, 0.9f, 0.35f);
            Burst(new Vector2(x, GroundY), 24, new Color(0.85f, 0.75f, 0.6f), 320f);
        };
        return rusty;
    }

    // ---------- Making new obstacles, enemies and bolts ----------

    void SpawnThings(float dt)
    {
        int meters = Meters;

        // Boss time! First a warning, then (a little later) the boss flies in.
        if (boss is null && !bossWarningShown && meters >= nextBossAt - 20)
        {
            bossWarningShown = true;
            hud.ShowBanner("WARNING!  BOSS AHEAD!", new Color(1f, 0.35f, 0.25f));
            sounds.Play(Sfx.BossAlarm);
        }
        if (boss is null && bossWarningShown && meters >= nextBossAt) SpawnBoss();
        if (bossWarningShown) return; // nothing else shows up just before and during a boss fight

        // Nothing new comes while Bolt-E is up on the rocket board or floating down on the parachute
        if (power == PowerUp.Rocket || robot.Parachuting)
        {
            spawnCountdown = Mathf.Max(spawnCountdown, 600);
            return;
        }

        spawnCountdown -= speed * dt;
        if (spawnCountdown > 0) return;

        float x = ScreenWidth + 100;
        float stretch = GapStretch; // floaty Moon jumps take longer, so things come farther apart there (see Main.Worlds.cs)

        // Sometimes a bad robot comes instead of an obstacle
        if (meters > 60 && GD.Randf() < 0.3f)
        {
            var enemyKind = meters > 150 && GD.Randf() < 0.45f ? EnemyKind.EggBot : EnemyKind.PupBot;
            var enemy = new Enemy { Kind = enemyKind, Position = new Vector2(x, GroundY) };
            AddChild(enemy);
            enemies.Add(enemy);
            spawnCountdown = (speed * 0.9f + 420) * stretch + (float)GD.RandRange(0, 450); // extra room, because enemies come at you
            return;
        }

        double roll = GD.Randf();

        // Harder obstacles only show up after you've gone a little way.
        ObstacleKind kind;
        if (meters > 250 && roll < 0.2) kind = ObstacleKind.Drone;
        else if (meters > 120 && roll < 0.4) kind = ObstacleKind.CrateStack;
        else kind = GD.Randf() < 0.5f ? ObstacleKind.Crate : ObstacleKind.Cone;

        AddObstacle(kind, x);

        if (kind == ObstacleKind.Drone)
        {
            // Bolts on the ground under the drone, to show you should stay low!
            for (int i = -1; i <= 1; i++) AddBolt(x + i * 50, GroundY - 40);
        }
        else if (GD.Randf() < 0.6f)
        {
            // An arc of bolts over the obstacle, like a jump path.
            float top = kind == ObstacleKind.CrateStack ? 260 : 190;
            for (int i = -2; i <= 2; i++)
            {
                float t = i / 2.5f;
                AddBolt(x + i * 55, GroundY - top * (1 - t * t) - 30);
            }
        }

        // The faster we go, the more space between obstacles (so there's always time to land).
        float minGap = (speed * 0.9f + 220) * stretch;
        float gap = (float)GD.RandRange(minGap, minGap + 550);
        spawnCountdown = gap;
        TrySpawnBox(x, gap, minGap, kind); // maybe a rainbow ? box in the gap (see Main.PowerUps.cs)

        // Sometimes put a row of bolts in the empty space.
        if (gap > minGap + 250 && GD.Randf() < 0.5f)
        {
            float middle = x + gap / 2f;
            for (int i = -2; i <= 2; i++) AddBolt(middle + i * 50, GroundY - 40);
        }
    }

    /// <summary>Puts a crate, cone, crate stack or drone on the road at x.</summary>
    Obstacle AddObstacle(ObstacleKind kind, float x)
    {
        float y = kind == ObstacleKind.Drone ? GroundY - 175 : GroundY; // high enough to ride under, low enough to bonk if you jump
        var obstacle = new Obstacle { Kind = kind, Position = new Vector2(x, y) };
        AddChild(obstacle);
        obstacles.Add(obstacle);
        return obstacle;
    }

    void AddBolt(float x, float y)
    {
        var bolt = new BoltPickup { Position = new Vector2(x, y) };
        AddChild(bolt);
        bolts.Add(bolt);
    }

    // ---------- Particles (little puffs and sparkles) ----------

    class Particle
    {
        public Vector2 Position, Velocity;
        public float Life, MaxLife, Size;
        public Color Color;
    }

    void Burst(Vector2 at, int count, Color color, float power)
    {
        for (int i = 0; i < count; i++)
        {
            float life = (float)GD.RandRange(0.3, 0.7);
            particles.Add(new Particle
            {
                Position = at,
                Velocity = Vector2.Up.Rotated((float)GD.RandRange(-1.4, 1.4)) * power * (float)GD.RandRange(0.4, 1.0),
                Life = life,
                MaxLife = life,
                Size = (float)GD.RandRange(3.0, 7.0),
                Color = color,
            });
        }
    }

    void UpdateParticles(float dt)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            p.Life -= dt;
            if (p.Life <= 0) { particles.RemoveAt(i); continue; }
            p.Velocity += new Vector2(-speed * 0.5f, 500f) * dt; // drift back and fall
            p.Position += p.Velocity * dt;
        }
    }

    // ---------- Drawing the world ----------

    // The sky's 4 corners (made once), and its 4 colors (changed every frame, as the worlds change)
    static readonly Vector2[] skyCorners =
    {
        new(-40, -40), new(ScreenWidth + 40, -40), new(ScreenWidth + 40, GroundY), new(-40, GroundY), // (40 = extra room for screen shake)
    };
    readonly Color[] skyColors = new Color[4];
    static readonly float[] CloudHeights = { 90, 170, 60, 140, 110 };

    /// <summary>
    /// Draws the world. Every color comes from the world we're in (see Worlds.cs), mixed with the last world's colors
    /// while the worlds change. The special things in each world are drawn by Main.Worlds.cs.
    /// </summary>
    public override void _Draw()
    {
        const float margin = 40; // extra room so screen shake never shows an edge
        drawnWorldThings.Clear();

        // Sky: the world's sky color at the top, fading to a paler color near the ground
        var skyTop = Mix(w => w.SkyTop);
        var skyBottom = Mix(w => w.SkyBottom);
        skyColors[0] = skyColors[1] = skyTop;
        skyColors[2] = skyColors[3] = skyBottom;
        DrawPolygon(skyCorners, skyColors);

        DrawStars();                              // twinkle, twinkle (Night City and the Moon)
        DrawSkyThing(fromWorld, 1 - worldBlend);  // the old world's sun, moon or Earth fades out...
        DrawSkyThing(toWorld, worldBlend);        // ...while the new world's fades in

        // Fluffy clouds
        var cloudColor = Mix(w => w.Clouds);
        for (int i = 0; i < CloudHeights.Length; i++)
        {
            float x = Scrolled(i * 340f, cloudScroll, 340f * CloudHeights.Length, 80);
            DrawCloud(new Vector2(x, CloudHeights[i]), cloudColor);
        }

        // Far hills (move slowly) and near hills (move faster) — this is called "parallax"
        var farHills = Mix(w => w.FarHills);
        for (int i = 0; i < 7; i++)
            DrawCircle(new Vector2(Scrolled(i * 260f, farHillScroll, 260f * 7, 260), GroundY + 150), 260, farHills);
        DrawCity(farHills);        // skyscrapers (Night City)
        DrawMountains(farHills);   // snowy mountains (Snowy Peaks)

        var nearHills = Mix(w => w.NearHills);
        for (int i = 0; i < 6; i++)
            DrawCircle(new Vector2(Scrolled(i * 300f + 120, nearHillScroll, 300f * 6, 160), GroundY + 90), 160, nearHills);
        DrawLollipops();           // giant lollipops (Candy Land)
        DrawPineTrees();           // pine trees (Snowy Peaks)

        // Ground: grass on top of dirt (or frosting on chocolate, a curb by the road, snow, moon dust...)
        var grass = Mix(w => w.Grass);
        var dirt = Mix(w => w.Dirt);
        DrawRect(new Rect2(-margin, GroundY, ScreenWidth + margin * 2, ScreenHeight - GroundY + margin), dirt);
        DrawRect(new Rect2(-margin, GroundY, ScreenWidth + margin * 2, 18), grass);
        for (int i = 0; i < 18; i++)
            DrawCircle(new Vector2(Scrolled(i * 80f, groundScroll, 80f * 18, 20), GroundY + 18), 10, grass); // grass bumps
        DrawGroundDots();          // pebbles, sprinkles, road dashes or craters

        // (The self-test checks that these colors really change with the world)
        drawnColors["sky"] = skyTop;
        drawnColors["far hills"] = farHills;
        drawnColors["near hills"] = nearHills;
        drawnColors["grass"] = grass;
        drawnColors["dirt"] = dirt;

        DrawSnow();                // (Snowy Peaks)
        DrawNightGlow();           // a soft glow around Bolt-E in the dark worlds (Bolt-E himself is drawn on top)

        // Particles
        foreach (var p in particles)
        {
            var c = p.Color;
            c.A = p.Life / p.MaxLife;
            DrawCircle(p.Position, p.Size * (0.5f + 0.5f * p.Life / p.MaxLife), c);
        }
    }

    /// <summary>A fluffy cloud made of 4 circles. (On the Moon the clouds are see-through, so they're skipped.)</summary>
    void DrawCloud(Vector2 at, Color color)
    {
        if (color.A < 0.02f) return;
        DrawCircle(at, 32, color);
        DrawCircle(at + new Vector2(-34, 10), 24, color);
        DrawCircle(at + new Vector2(34, 8), 26, color);
        DrawCircle(at + new Vector2(10, -16), 26, color);
        drawnWorldThings.Add("clouds");
    }

    /// <summary>Slides a thing to the left as we scroll, wrapping around to the right side when it goes off-screen.</summary>
    /// The loop must be at least ScreenWidth + 2 * pad long, so things wrap around while they're hidden.
    static float Scrolled(float startX, float scroll, float loopLength, float pad)
        => Mathf.PosMod(startX - scroll, loopLength) - pad;
}
