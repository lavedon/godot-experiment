using Godot;

/// <summary>
/// The boss of the game (not Big Rusty, the other kind of boss!). It draws the world, moves things along,
/// spawns obstacles, enemies and bolts, checks for bumps and stomps, and saves scores to the SQLite database.
/// </summary>
public partial class Main : Node2D
{
    enum GameState { Title, Playing, GameOver }

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
    Boss? boss;
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

    float speedBeforeBoss;   // so we can speed back up after a boss fight

    /// <summary>During a boss fight the world stops and Bolt-E can ride left and right.</summary>
    bool BossFightActive => boss is not null && !boss.HasBlownUp;

    // Each bolt in a row plays the next note up the scale: do, re, mi, fa, so, la, ti, do!
    static readonly int[] ScaleSteps = { 0, 2, 4, 5, 7, 9, 11, 12 };

    // How far each background layer has scrolled
    float cloudScroll, farHillScroll, nearHillScroll, groundScroll;

    int Meters => (int)(distance / PixelsPerMeter);
    int Score => Meters + boltsCollected * PointsPerBolt + bonusPoints;

    public override void _Ready()
    {
        SetUpControls();
        OpenDatabase();

        robot = new Robot { GroundY = GroundY, Position = new Vector2(RobotX, GroundY), ZIndex = 10 };
        AddChild(robot);

        popups = new ScorePopups { ZIndex = 20 };
        AddChild(popups);

        hud = new Hud();
        hud.StartPressed += StartGame;
        AddChild(hud);

        sounds = new SoundBoard();
        AddChild(sounds);
        sounds.SetMusicOn(database?.GetSetting("music") != "off"); // remembers if you turned the music off
        // Big parts go "clonk" (low), little gears and screws go "tink" (high).
        robot.PieceBounced += bigPart => sounds.Play(Sfx.Clink, bigPart ? Rand(0.55f, 0.75f) : Rand(1.1f, 1.5f));

        if (database is null)
            hud.ShowWarning("Could not open the score database, so scores won't be saved this time.");

        // Notice when a game controller is plugged in or unplugged.
        Input.JoyConnectionChanged += OnControllerPluggedOrUnplugged;
        UpdateControllerStatus();

        GoToTitle();
    }

    // ---------- Controls: keyboard, mouse, and game controllers (like an Xbox controller) ----------

    /// <summary>
    /// "jump": Space, Up arrow, W, a mouse click, or A / B / X / Y / D-pad up on a controller.
    /// "start": Enter, or the START button on a controller (starts or restarts the game).
    /// "left" / "right": arrow keys, A / D, the D-pad or the left stick (riding around in boss fights).
    /// </summary>
    static void SetUpControls()
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
    }

    static InputEventJoypadMotion Stick(float direction) => new() { Axis = JoyAxis.LeftX, AxisValue = direction, Device = AnyDevice };

    static float Rand(float min, float max) => (float)GD.RandRange(min, max);

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
    static InputEventJoypadButton ControllerButton(JoyButton button) => new() { ButtonIndex = button, Device = AnyDevice };

    void OnControllerPluggedOrUnplugged(long device, bool connected) => UpdateControllerStatus();

    void UpdateControllerStatus()
    {
        var controllers = Input.GetConnectedJoypads();
        if (controllers.Count == 0)
        {
            hud.SetControllerName(null);
            return;
        }
        string name = Input.GetJoyName(controllers[0]);
        if (name.Contains("XInput", StringComparison.OrdinalIgnoreCase))
            name = "Xbox controller"; // Windows calls every Xbox controller "XInput Controller"
        hud.SetControllerName(name);
    }

    /// <summary>Makes every connected controller shake. "weak" is a fast buzz, "strong" is a big rumble (0 to 1).</summary>
    void Rumble(float weak, float strong, float seconds)
    {
        if (!rumbleOn) return;
        foreach (int device in Input.GetConnectedJoypads())
            Input.StartJoyVibration(device, weak, strong, seconds);
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
        Input.JoyConnectionChanged -= OnControllerPluggedOrUnplugged;
        database?.Dispose();
    }

    // ---------- Changing between title / playing / game over ----------

    void GoToTitle()
    {
        state = GameState.Title;
        speed = 150f; // the world rolls by slowly behind the title screen
        bestScore = database?.BestScore() ?? 0;
        hud.ShowTitle(database?.GetSetting("player_name") ?? "", database?.TopScores(5) ?? new());
    }

    void StartGame()
    {
        if (state == GameState.Playing) return;

        database?.SetSetting("player_name", hud.PlayerName);
        bestScore = database?.BestScore() ?? 0;

        foreach (var o in obstacles) o.QueueFree();
        foreach (var b in bolts) b.QueueFree();
        foreach (var e in enemies) e.QueueFree();
        foreach (var b in blasts) b.QueueFree();
        foreach (var w in shockwaves) w.QueueFree();
        foreach (var l in lasers) l.QueueFree();
        boss?.QueueFree();
        obstacles.Clear();
        bolts.Clear();
        enemies.Clear();
        blasts.Clear();
        shockwaves.Clear();
        lasers.Clear();
        boss = null;
        speedBeforeBoss = 0;
        popups.Clear();

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
        nextBossAt = FirstBossAt;
        bossWarningShown = false;
        robot.Reset();
        hud.ShowPlaying();
        sounds.Play(Sfx.Start);
        sounds.StartMusic();
    }

    void GameOver()
    {
        state = GameState.GameOver;
        gameOverTimer = 0;
        shake = 0.5f;
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
        try
        {
            if (database is not null)
            {
                runId = database.SaveRun(hud.PlayerName, score, meters, boltsCollected, stomps, bossesBeaten, playTime);
                stats = database.StatsFor(hud.PlayerName);
            }
        }
        catch (Exception e)
        {
            GD.PushError($"Could not save the score: {e.Message}");
        }

        // Don't cover up the explosion: show the scores a little later (see _Process).
        var topScores = database?.TopScores(5) ?? new();
        int boltsThisGame = boltsCollected, stompsThisGame = stomps, bossesThisGame = bossesBeaten;
        showResults = () =>
        {
            hud.ShowGameOver(score, meters, boltsThisGame, stompsThisGame, bossesThisGame, newBest, stats, topScores, runId);
            sounds.Play(newBest ? Sfx.HighScore : Sfx.GameOver);
        };
    }

    // ---------- Input ----------

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("music"))
        {
            ToggleMusic();
            return;
        }

        bool jump = e.IsActionPressed("jump");
        bool start = e.IsActionPressed("start");
        if (jump || start)
        {
            switch (state)
            {
                case GameState.Title:
                    StartGame();
                    break;
                case GameState.Playing:
                    if (jump && robot.Jump())
                    {
                        Burst(robot.Position, 8, new Color(0.9f, 0.85f, 0.75f), 120f);
                        sounds.Play(robot.DidDoubleJump ? Sfx.DoubleJump : Sfx.Jump);
                    }
                    break;
                case GameState.GameOver:
                    // Only after the scores are showing, so you don't skip them by accident
                    if (gameOverTimer > ResultsDelay + 0.3f) StartGame();
                    break;
            }
        }
        else if (e.IsActionReleased("jump") && state == GameState.Playing)
        {
            robot.ReleaseJump();
        }
    }

    // ---------- Every frame ----------

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (state == GameState.Playing)
        {
            playTime += dt;
            if (BossFightActive)
                speed = Mathf.MoveToward(speed, 0, 500f * dt);               // the world stops for the boss fight
            else if (speed < speedBeforeBoss)
                speed = Mathf.MoveToward(speed, speedBeforeBoss, 450f * dt);  // speeding back up after a boss fight
            else
                speed = Mathf.Min(MaxSpeed, speed + SpeedUpPerSecond * dt);
            robot.CanMove = BossFightActive;
            robot.MoveInput = Input.GetAxis("left", "right");
            if (boss is not null) boss.TargetX = robot.Position.X;
            distance += speed * dt;
            SpawnThings(dt);
            CheckBumps();
            hud.SetScore(Score, boltsCollected, bestScore);
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

        robot.RunSpeed = speed;
        MoveWorld(dt);

        // Puff of dust (and a soft "thup") when landing
        if (robot.OnGround && !wasOnGround && !robot.Crashed)
        {
            Burst(robot.Position, 10, new Color(0.9f, 0.85f, 0.75f), 150f);
            sounds.Play(Sfx.Land);
            stompCombo = 0; // touching the ground ends a stomp streak
        }
        wasOnGround = robot.OnGround;

        UpdateParticles(dt);

        // Screen shake after a bump
        shake = Mathf.Max(0, shake - dt);
        Position = shake > 0 ? new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0)) * shake * 30f : Vector2.Zero;

        QueueRedraw();
    }

    void MoveWorld(float dt)
    {
        float move = speed * dt;
        cloudScroll += move * 0.1f;
        farHillScroll += move * 0.25f;
        nearHillScroll += move * 0.5f;
        groundScroll += move;

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            obstacles[i].Position -= new Vector2(move, 0);
            if (obstacles[i].Position.X < -150)
            {
                obstacles[i].QueueFree();
                obstacles.RemoveAt(i);
            }
        }

        for (int i = bolts.Count - 1; i >= 0; i--)
        {
            bolts[i].Position -= new Vector2(move, 0);
            if (bolts[i].Position.X < -150)
            {
                bolts[i].QueueFree();
                bolts.RemoveAt(i);
            }
        }

        // Enemies move with the ground, plus a bit extra because they're coming at you!
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (enemy.Chaser && !enemy.Squashed)
            {
                // Big Rusty's helpers chase Bolt-E
                float direction = robot.Position.X < enemy.Position.X ? -1 : 1;
                enemy.Facing = direction;
                enemy.Position += new Vector2(direction * 150f * dt - move, 0);
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
            blasts[i].Advance(dt, speed);
            if (blasts[i].Position.X < -100 || blasts[i].Position.X > ScreenWidth + 100)
            {
                blasts[i].QueueFree();
                blasts.RemoveAt(i);
            }
        }

        for (int i = shockwaves.Count - 1; i >= 0; i--)
        {
            shockwaves[i].Advance(dt, speed);
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
            boss.WorldSpeed = speed;
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

        foreach (var obstacle in obstacles)
        {
            if (robotBox.Intersects(obstacle.Hitbox))
            {
                GameOver();
                return;
            }
        }

        // Bad robots: land on their heads to squash them, but bumping into them is a crash!
        foreach (var enemy in enemies)
        {
            if (enemy.Squashed || !robotBox.Intersects(enemy.Hitbox)) continue;
            if (CameDownOnTop(enemy.Top))
            {
                StompEnemy(enemy);
            }
            else
            {
                GameOver();
                return;
            }
        }

        foreach (var blast in blasts)
        {
            if (robotBox.Intersects(blast.Hitbox))
            {
                GameOver();
                return;
            }
        }

        foreach (var wave in shockwaves)
        {
            if (robotBox.Intersects(wave.Hitbox))
            {
                GameOver();
                return;
            }
        }

        foreach (var laser in lasers)
        {
            if (laser.Firing && robotBox.Intersects(laser.Hitbox))
            {
                GameOver();
                return;
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
                GameOver();
                return;
            }
        }

        for (int i = bolts.Count - 1; i >= 0; i--)
        {
            if (bolts[i].Touches(robotBox))
            {
                boltsCollected++;
                robot.BeHappy();
                Rumble(0.3f, 0f, 0.06f); // tiny buzz: got a bolt!

                // Bolts grabbed quickly one after another go up the scale
                boltCombo = playTime - lastBoltTime < 0.6f ? boltCombo + 1 : 0;
                lastBoltTime = playTime;
                int semitones = ScaleSteps[Math.Min(boltCombo, ScaleSteps.Length - 1)];
                sounds.Play(Sfx.Bolt, Mathf.Pow(2f, semitones / 12f));
                Burst(bolts[i].Position, 10, new Color(1f, 0.85f, 0.25f), 220f);
                bolts[i].QueueFree();
                bolts.RemoveAt(i);
            }
        }
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
        robot.Bounce(high: Input.IsActionPressed("jump")); // hold jump to bounce higher
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
        bossesBeaten++;
        bonusPoints += PointsForBeatingBoss;
        popups.Add($"+{PointsForBeatingBoss}", boss!.Position + new Vector2(0, -200), new Color(1f, 0.85f, 0.2f), 46);
        hud.ShowBanner($"YOU BEAT {Boss.DisplayName}!", new Color(1f, 0.85f, 0.2f));
        sounds.Play(Sfx.Explosion);
        sounds.Play(Sfx.Victory);
        sounds.SetMusicSpeed(1f);
        shake = 0.5f;
        Rumble(0.8f, 1f, 0.6f);

        // A rainbow of bolts as a prize!
        for (int i = 0; i < 12; i++)
            AddBolt(RobotX + 350 + i * 55, GroundY - 40 - 170 * Mathf.Sin(i / 11f * Mathf.Pi));

        GetTree().CreateTimer(1.5).Timeout += () => { if (!BossFightActive) hud.HideBossBar(); };
        nextBossAt = Meters + MetersBetweenBosses;
        bossWarningShown = false;
        spawnCountdown = 1200; // a little rest before obstacles come back
    }

    void SpawnBoss()
    {
        speedBeforeBoss = Mathf.Max(speed, StartSpeed);

        // Big Rusty's arrival blows away anything still on the screen
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

        var rusty = new Boss { GroundY = GroundY, ZIndex = 5 };
        boss = rusty;

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
        rusty.GotAngry += phase =>
        {
            hud.ShowBanner(phase == 2 ? $"{Boss.DisplayName} IS ANGRY!" : $"{Boss.DisplayName} IS FURIOUS!", new Color(1f, 0.35f, 0.25f));
            sounds.Play(Sfx.Roar);
            sounds.SetMusicSpeed(phase == 2 ? 1.15f : 1.22f);
            shake = 0.35f;
            Rumble(0.5f, 0.6f, 0.5f);
        };
        rusty.Popped += at =>
        {
            Burst(at, 10, new Color(1f, 0.8f, 0.3f), 260f);
            sounds.Play(Sfx.Explosion, Rand(1.8f, 2.4f));
            shake = 0.2f;
        };
        rusty.BlewUp += BossBeaten;
        rusty.PieceBounced += bigPiece => sounds.Play(Sfx.Clink, bigPiece ? Rand(0.45f, 0.6f) : Rand(1.1f, 1.5f));

        AddChild(rusty);
        hud.ShowBossBar(rusty.Health);
        hud.ShowHint("Ride left and right with  ← →  or the stick!");
        sounds.SetMusicSpeed(1.1f);
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

        spawnCountdown -= speed * dt;
        if (spawnCountdown > 0) return;

        float x = ScreenWidth + 100;

        // Sometimes a bad robot comes instead of an obstacle
        if (meters > 60 && GD.Randf() < 0.3f)
        {
            var enemyKind = meters > 150 && GD.Randf() < 0.45f ? EnemyKind.EggBot : EnemyKind.PupBot;
            var enemy = new Enemy { Kind = enemyKind, Position = new Vector2(x, GroundY) };
            AddChild(enemy);
            enemies.Add(enemy);
            spawnCountdown = speed * 0.9f + 420 + (float)GD.RandRange(0, 450); // extra room, because enemies come at you
            return;
        }

        double roll = GD.Randf();

        // Harder obstacles only show up after you've gone a little way.
        ObstacleKind kind;
        if (meters > 250 && roll < 0.2) kind = ObstacleKind.Drone;
        else if (meters > 120 && roll < 0.4) kind = ObstacleKind.CrateStack;
        else kind = GD.Randf() < 0.5f ? ObstacleKind.Crate : ObstacleKind.Cone;

        float y = kind == ObstacleKind.Drone ? GroundY - 175 : GroundY; // high enough to ride under, low enough to bonk if you jump
        var obstacle = new Obstacle { Kind = kind, Position = new Vector2(x, y) };
        AddChild(obstacle);
        obstacles.Add(obstacle);

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
        float minGap = speed * 0.9f + 220;
        float gap = (float)GD.RandRange(minGap, minGap + 550);
        spawnCountdown = gap;

        // Sometimes put a row of bolts in the empty space.
        if (gap > minGap + 250 && GD.Randf() < 0.5f)
        {
            float middle = x + gap / 2f;
            for (int i = -2; i <= 2; i++) AddBolt(middle + i * 50, GroundY - 40);
        }
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

    public override void _Draw()
    {
        const float margin = 40; // extra room so screen shake never shows an edge

        // Sky: light blue at the top fading to pale near the ground
        var skyTop = new Color(0.45f, 0.75f, 1f);
        var skyBottom = new Color(0.85f, 0.95f, 1f);
        DrawPolygon(
            new[] { new Vector2(-margin, -margin), new Vector2(ScreenWidth + margin, -margin),
                    new Vector2(ScreenWidth + margin, GroundY), new Vector2(-margin, GroundY) },
            new[] { skyTop, skyTop, skyBottom, skyBottom });

        // Sun
        var sunAt = new Vector2(1080, 120);
        DrawCircle(sunAt, 80, new Color(1f, 0.95f, 0.6f, 0.3f));
        DrawCircle(sunAt, 55, new Color(1f, 0.9f, 0.4f));

        // Fluffy clouds
        float[] cloudHeights = { 90, 170, 60, 140, 110 };
        for (int i = 0; i < cloudHeights.Length; i++)
        {
            float x = Scrolled(i * 340f, cloudScroll, 340f * cloudHeights.Length, 80);
            DrawCloud(new Vector2(x, cloudHeights[i]));
        }

        // Far hills (move slowly) and near hills (move faster) — this is called "parallax"
        for (int i = 0; i < 7; i++)
            DrawCircle(new Vector2(Scrolled(i * 260f, farHillScroll, 260f * 7, 260), GroundY + 150), 260, new Color(0.62f, 0.85f, 0.75f));
        for (int i = 0; i < 6; i++)
            DrawCircle(new Vector2(Scrolled(i * 300f + 120, nearHillScroll, 300f * 6, 160), GroundY + 90), 160, new Color(0.45f, 0.78f, 0.45f));

        // Ground: grass on top of dirt
        DrawRect(new Rect2(-margin, GroundY, ScreenWidth + margin * 2, ScreenHeight - GroundY + margin), new Color(0.72f, 0.52f, 0.33f));
        DrawRect(new Rect2(-margin, GroundY, ScreenWidth + margin * 2, 18), new Color(0.38f, 0.75f, 0.3f));
        for (int i = 0; i < 18; i++)
        {
            float x = Scrolled(i * 80f, groundScroll, 80f * 18, 20);
            DrawCircle(new Vector2(x, GroundY + 18), 10, new Color(0.38f, 0.75f, 0.3f));              // grass bumps
            DrawCircle(new Vector2(x + 40, GroundY + 60 + (i % 3) * 18), 5, new Color(0.6f, 0.42f, 0.27f)); // pebbles
        }

        // Particles
        foreach (var p in particles)
        {
            var c = p.Color;
            c.A = p.Life / p.MaxLife;
            DrawCircle(p.Position, p.Size * (0.5f + 0.5f * p.Life / p.MaxLife), c);
        }
    }

    void DrawCloud(Vector2 at)
    {
        var white = new Color(1, 1, 1, 0.92f);
        DrawCircle(at, 32, white);
        DrawCircle(at + new Vector2(-34, 10), 24, white);
        DrawCircle(at + new Vector2(34, 8), 26, white);
        DrawCircle(at + new Vector2(10, -16), 26, white);
    }

    /// <summary>Slides a thing to the left as we scroll, wrapping around to the right side when it goes off-screen.</summary>
    /// The loop must be at least ScreenWidth + 2 * pad long, so things wrap around while they're hidden.
    static float Scrolled(float startX, float scroll, float loopLength, float pad)
        => Mathf.PosMod(startX - scroll, loopLength) - pad;
}
