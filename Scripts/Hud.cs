using Godot;

/// <summary>
/// Everything written on the screen: the score while playing, the title screen, and the game-over screen.
/// It's all built with code so you can see exactly how each piece is made.
/// </summary>
public partial class Hud : CanvasLayer
{
    [Signal] public delegate void StartPressedEventHandler();

    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    static readonly Color Gold = new(1f, 0.78f, 0.15f);

    Label scoreLabel = null!;
    Label boltsLabel = null!;
    Label bestLabel = null!;
    Label warningLabel = null!;

    Control bossBar = null!;
    ColorRect bossHealth = null!;
    Label banner = null!;
    Tween? bannerFade;
    Label hint = null!;
    Tween? hintFade;

    Control titleScreen = null!;
    LineEdit nameBox = null!;
    GridContainer titleScores = null!;
    Label controllerLabel = null!;

    Control gameOverScreen = null!;
    Label gameOverTitle = null!;
    Label gameOverDetails = null!;
    Label gameOverStats = null!;
    GridContainer gameOverScores = null!;

    /// <summary>The name typed on the title screen.</summary>
    public string PlayerName => nameBox.Text.Trim() is { Length: > 0 } name ? name : "Player";

    public override void _Ready()
    {
        // While playing: score in the top-left, best score in the top-right.
        scoreLabel = MakeLabel("Score: 0", 36, Colors.White, outline: true);
        scoreLabel.Position = new Vector2(24, 14);
        AddChild(scoreLabel);

        boltsLabel = MakeLabel("Bolts: 0", 28, Gold, outline: true);
        boltsLabel.Position = new Vector2(24, 60);
        AddChild(boltsLabel);

        bestLabel = MakeLabel("Best: 0", 28, Colors.White, outline: true);
        bestLabel.HorizontalAlignment = HorizontalAlignment.Right;
        bestLabel.Position = new Vector2(1280 - 24 - 300, 16);
        bestLabel.Size = new Vector2(300, 40);
        AddChild(bestLabel);

        warningLabel = MakeLabel("", 18, new Color(1f, 0.85f, 0.85f), outline: true);
        warningLabel.Position = new Vector2(24, 690);
        AddChild(warningLabel);

        BuildBossBar();

        // Big words in the middle of the screen, like "WARNING!" (they fade away by themselves)
        banner = Centered(MakeLabel("", 60, Gold, outline: true));
        banner.Position = new Vector2(0, 150);
        banner.Size = new Vector2(1280, 90);
        banner.Modulate = Colors.Transparent;
        AddChild(banner);

        // A smaller tip under the banner, like how to move in a boss fight
        hint = Centered(MakeLabel("", 28, Colors.White, outline: true));
        hint.Position = new Vector2(0, 245);
        hint.Size = new Vector2(1280, 40);
        hint.Modulate = Colors.Transparent;
        AddChild(hint);

        BuildTitleScreen();
        BuildGameOverScreen();
    }

    void BuildTitleScreen()
    {
        var box = MakePanel(out titleScreen);

        var title = MakeLabel("ROBOT DASH", 76, Gold, outline: true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);

        box.AddChild(Centered(MakeLabel("Help Bolt-E the robot ride its hoverboard as far as possible!", 24, Ink)));

        var nameRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        nameRow.AddThemeConstantOverride("separation", 12);
        nameRow.AddChild(MakeLabel("Your name:", 28, Ink));
        nameBox = new LineEdit { MaxLength = 16, CustomMinimumSize = new Vector2(280, 0), PlaceholderText = "type your name" };
        nameBox.AddThemeFontSizeOverride("font_size", 28);
        nameBox.TextSubmitted += _ => EmitSignal(SignalName.StartPressed); // pressing Enter starts the game
        nameRow.AddChild(nameBox);
        box.AddChild(nameRow);

        box.AddChild(MakeButton("Start!  (Space or A)"));

        box.AddChild(Centered(MakeLabel(
            "SPACE, UP arrow, click, or the A button = jump.  Press again in the air = DOUBLE JUMP!\n" +
            "Grab bolts.  Ride UNDER the purple drones.  Jump ON bad robots to squash them!\n" +
            "Press M (or BACK on a controller) to turn the music on or off.", 20, Ink)));

        controllerLabel = Centered(MakeLabel("", 20, Ink));
        box.AddChild(controllerLabel);

        box.AddChild(Centered(MakeLabel("Top Scores", 30, Ink)));
        titleScores = MakeScoreGrid();
        box.AddChild(titleScores);
    }

    void BuildGameOverScreen()
    {
        var box = MakePanel(out gameOverScreen);

        gameOverTitle = Centered(MakeLabel("Bonk!", 64, Gold, outline: true));
        box.AddChild(gameOverTitle);
        gameOverDetails = Centered(MakeLabel("", 30, Ink));
        box.AddChild(gameOverDetails);
        gameOverStats = Centered(MakeLabel("", 20, Ink));
        box.AddChild(gameOverStats);

        box.AddChild(Centered(MakeLabel("Top Scores", 30, Ink)));
        gameOverScores = MakeScoreGrid();
        box.AddChild(gameOverScores);

        box.AddChild(MakeButton("Play again!  (Space or A)"));
    }

    // ---------- Showing the different screens ----------

    public void ShowTitle(string lastName, IReadOnlyList<RunRecord> topScores)
    {
        nameBox.Text = lastName;
        FillScores(titleScores, topScores, highlightId: -1);
        titleScreen.Visible = true;
        gameOverScreen.Visible = false;
        SetPlayingLabelsVisible(false);
    }

    public void ShowPlaying()
    {
        HideBossBar();
        bannerFade?.Kill();
        banner.Modulate = Colors.Transparent;
        hintFade?.Kill();
        hint.Modulate = Colors.Transparent;
        titleScreen.Visible = false;
        gameOverScreen.Visible = false;
        SetPlayingLabelsVisible(true);
    }

    public void ShowGameOver(int score, int distanceM, int bolts, int stomps, int bossesBeaten, bool newBest, PlayerStats? stats,
                             IReadOnlyList<RunRecord> topScores, long thisRunId)
    {
        gameOverTitle.Text = newBest ? "NEW HIGH SCORE!" : "Bonk!";
        gameOverDetails.Text = $"Score {score}  ·  {distanceM} m  ·  {Count(bolts, "bolt")}  ·  {Count(stomps, "stomp")}" +
                               (bossesBeaten > 0 ? $"  ·  {Count(bossesBeaten, "boss", "bosses")} beaten!" : "");
        gameOverStats.Text = stats is null ? "" :
            $"{PlayerName}: {Count(stats.GamesPlayed, "game")} played  ·  best {stats.BestScore}  ·  " +
            $"{Count(stats.TotalBolts, "bolt")} collected  ·  {stats.TotalDistanceM} m travelled\n" +
            $"{Count(stats.TotalStomps, "enemy", "enemies")} stomped  ·  {Count(stats.BossesBeaten, "boss", "bosses")} beaten";
        FillScores(gameOverScores, topScores, thisRunId);
        gameOverScreen.Visible = true;
    }

    public void SetScore(int score, int bolts, int best)
    {
        scoreLabel.Text = $"Score: {score}";
        boltsLabel.Text = $"Bolts: {bolts}";
        bestLabel.Text = $"Best: {Math.Max(score, best)}";
    }

    public void ShowWarning(string text) => warningLabel.Text = text;

    /// <summary>"1 bolt", "2 bolts", "1 boss", "3 bosses"...</summary>
    static string Count(int number, string one, string? many = null) => $"{number} {(number == 1 ? one : many ?? one + "s")}";

    // ---------- Boss fight ----------

    void BuildBossBar()
    {
        bossBar = new Control { Position = new Vector2(440, 10), Size = new Vector2(400, 64), Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        var name = Centered(MakeLabel(Boss.DisplayName, 26, new Color(1f, 0.6f, 0.15f), outline: true));
        name.Size = new Vector2(400, 36);
        bossBar.AddChild(name);
        bossBar.AddChild(new ColorRect { Color = Ink, Position = new Vector2(50, 40), Size = new Vector2(300, 20), MouseFilter = Control.MouseFilterEnum.Ignore });
        bossHealth = new ColorRect { Color = new Color(0.95f, 0.3f, 0.2f), Position = new Vector2(53, 43), Size = new Vector2(294, 14), MouseFilter = Control.MouseFilterEnum.Ignore };
        bossBar.AddChild(bossHealth);
        // Little lines that split the bar into 3 pieces (one for each stomp)
        for (int i = 1; i < Boss.MaxHealth; i++)
            bossBar.AddChild(new ColorRect { Color = Ink, Position = new Vector2(53 + 294f * i / Boss.MaxHealth - 1.5f, 43), Size = new Vector2(3, 14), MouseFilter = Control.MouseFilterEnum.Ignore });
        AddChild(bossBar);
    }

    public void ShowBossBar(int health)
    {
        bossBar.Visible = true;
        bossHealth.Size = new Vector2(294f * health / Boss.MaxHealth, 14);
    }

    public void HideBossBar() => bossBar.Visible = false;

    /// <summary>Shows a tip under the banner for a few seconds.</summary>
    public void ShowHint(string text)
    {
        hint.Text = text;
        hint.Modulate = Colors.White;
        hintFade?.Kill();
        hintFade = CreateTween();
        hintFade.TweenInterval(4.0);
        hintFade.TweenProperty(hint, "modulate:a", 0f, 0.6);
    }

    /// <summary>Shows big words in the middle of the screen for a moment.</summary>
    public void ShowBanner(string text, Color color)
    {
        banner.Text = text;
        banner.LabelSettings.FontColor = color;
        banner.Modulate = Colors.White;
        bannerFade?.Kill();
        bannerFade = CreateTween();
        bannerFade.TweenInterval(1.6);
        bannerFade.TweenProperty(banner, "modulate:a", 0f, 0.5);
    }

    /// <summary>Shows which game controller is plugged in (or null if there isn't one).</summary>
    public void SetControllerName(string? name)
    {
        bool connected = !string.IsNullOrEmpty(name);
        controllerLabel.Text = connected
            ? $"Controller connected: {name}!   A, B, X, Y = jump   ·   START = go!"
            : "Got a game controller? Plug it in to play with it!";
        controllerLabel.LabelSettings.FontColor = connected ? new Color(0.15f, 0.55f, 0.3f) : Ink.Lightened(0.35f);
    }

    void SetPlayingLabelsVisible(bool visible)
    {
        scoreLabel.Visible = visible;
        boltsLabel.Visible = visible;
        bestLabel.Visible = visible;
    }

    void FillScores(GridContainer grid, IReadOnlyList<RunRecord> scores, long highlightId)
    {
        foreach (var child in grid.GetChildren()) child.QueueFree();

        if (scores.Count == 0)
        {
            grid.AddChild(MakeLabel("No scores yet.", 22, Ink));
            grid.AddChild(new Control());
            grid.AddChild(MakeLabel("Be the first!", 22, Ink));
            return;
        }

        for (int i = 0; i < scores.Count; i++)
        {
            var run = scores[i];
            var color = run.Id == highlightId ? new Color(0.9f, 0.3f, 0.5f) : Ink;
            grid.AddChild(MakeLabel($"{i + 1}.", 22, color));
            grid.AddChild(MakeLabel(run.PlayerName + (run.Id == highlightId ? "  ← you!" : ""), 22, color));
            var scoreText = MakeLabel(run.Score.ToString(), 22, color);
            scoreText.HorizontalAlignment = HorizontalAlignment.Right;
            grid.AddChild(scoreText);
        }
    }

    // ---------- Little helpers for building the screen ----------

    static Label MakeLabel(string text, int size, Color color, bool outline = false)
    {
        return new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            LabelSettings = new LabelSettings
            {
                FontSize = size,
                FontColor = color,
                OutlineSize = outline ? Math.Max(6, size / 6) : 0,
                OutlineColor = Ink,
            },
        };
    }

    static Label Centered(Label label)
    {
        label.HorizontalAlignment = HorizontalAlignment.Center;
        return label;
    }

    Button MakeButton(string text)
    {
        var button = new Button
        {
            Text = text,
            FocusMode = Control.FocusModeEnum.None, // so Space is handled by the game, not the button
            CustomMinimumSize = new Vector2(320, 60),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        button.AddThemeFontSizeOverride("font_size", 30);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        var pink = new Color(1f, 0.45f, 0.6f);
        button.AddThemeStyleboxOverride("normal", ButtonStyle(pink));
        button.AddThemeStyleboxOverride("hover", ButtonStyle(pink.Lightened(0.15f)));
        button.AddThemeStyleboxOverride("pressed", ButtonStyle(pink.Darkened(0.15f)));
        button.Pressed += () => EmitSignal(SignalName.StartPressed);
        return button;
    }

    static StyleBoxFlat ButtonStyle(Color color)
    {
        var style = new StyleBoxFlat { BgColor = color, BorderColor = Ink, ContentMarginLeft = 26, ContentMarginRight = 26 };
        style.SetCornerRadiusAll(20);
        style.SetBorderWidthAll(3);
        return style;
    }

    static GridContainer MakeScoreGrid()
    {
        var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        grid.AddThemeConstantOverride("h_separation", 28);
        grid.AddThemeConstantOverride("v_separation", 2);
        return grid;
    }

    /// <summary>A rounded white card in the middle of the screen. Returns the column to put things in.</summary>
    VBoxContainer MakePanel(out Control screen)
    {
        var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        center.OffsetLeft = 300; // leave room on the left so we can still see the robot
        AddChild(center);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(1, 1, 1, 0.9f),
            BorderColor = Ink,
            ShadowColor = new Color(0, 0, 0, 0.25f),
            ShadowSize = 12,
        };
        style.SetCornerRadiusAll(28);
        style.SetBorderWidthAll(4);
        style.SetContentMarginAll(28);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        panel.AddChild(column);

        screen = center;
        return column;
    }
}
