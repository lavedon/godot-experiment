using Godot;

/// <summary>Everything the results card shows after a game. (Later features can add more things here.)</summary>
public class ResultsInfo
{
    public string Title = "Bonk!";
    public int Score, Meters, Bolts, Stomps, Bosses;
    public PlayerStats? Stats;
    public IReadOnlyList<RunRecord> TopScores = new List<RunRecord>();
    public long RunId = -1;   // this game's row in the database (so it can say "you!" in Top Scores)
    public string WorldLine = "";  // how far the World Tour went, like "You rode all the way to NIGHT CITY!" ("" = no line)
    public string? TodayLine;      // who's champ today, like "Today: MAX 1520 (champ!)  -  DAD 980" (null = the usual stats line)
    public string CloseLine = "";  // "So close! Only 42 m to DAD's flag!" ("" = no line)
}

/// <summary>
/// Everything written on the screen: the score while playing, the title screen (with "Who's playing?" and the Family
/// Champions), the game-over screen, and the strip on the left of the menus (the Bolt Bank, the World Tour postcards,
/// the unlock party words and the look picker). It's all built with code so you can see exactly how each piece is made.
/// </summary>
public partial class Hud : CanvasLayer
{
    [Signal] public delegate void StartPressedEventHandler();

    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    static readonly Color Gold = new(1f, 0.78f, 0.15f);

    /// <summary>How long the spare battery takes to zoom from the corner to Bolt-E (seconds).</summary>
    public const float BatteryFlightSeconds = 0.45f;

    Label scoreLabel = null!;
    Label boltsLabel = null!;
    Label bestLabel = null!;
    Label warningLabel = null!;
    BatteryRow batteryRow = null!;
    Tween? batteryPop;

    Control bossBar = null!;
    Label bossBarName = null!;   // "BIG RUSTY", or "BIG RUSTY (DAD)" while Dad drives him
    ColorRect bossHealth = null!;
    Control powerBar = null!;
    PowerIcon powerIcon = null!;
    ColorRect powerFill = null!;
    Label banner = null!;
    Tween? bannerFade;
    Label hint = null!;
    Tween? hintFade;

    Control titleScreen = null!;
    PanelContainer titleCard = null!;
    LineEdit nameBox = null!;
    Label playerLabel = null!;    // the name of who's playing, between LB and RB (click it to type a new name)
    Button lbButton = null!;
    Button rbButton = null!;
    Label titleHeading = null!;
    GridContainer titleScores = null!;
    Label controllerLabel = null!;

    Control gameOverScreen = null!;
    PanelContainer gameOverCard = null!;
    Label gameOverTitle = null!;
    Label gameOverDetails = null!;
    Label gameOverWorld = null!;
    Label gameOverClose = null!;  // "So close! Only 42 m to DAD's flag!"
    Label statsLine1 = null!;
    Label statsLine2 = null!;     // the player's stomps and bosses, or who's champ today
    GridContainer gameOverScores = null!;
    Button playButton = null!;
    Button lbButton2 = null!;     // LB and RB on the results card
    Button rbButton2 = null!;
    Tween? bestFlash;

    // Who's playing? Everyone who has played (the one who played last first), and who's picked.
    // playerIndex == players.Count means NEW PLAYER (on the title card that's the name box, to type a new name).
    static readonly Color PlayerPink = new(1f, 0.45f, 0.6f);
    readonly List<string> players = new();
    int playerIndex;
    string lastRealName = "";     // the last real person picked (while a new name is being typed, it's still them)

    // The strip down the left side of the menus (title screen and results card): the Bolt Bank, the World Tour
    // postcards, the unlock party words, and the look picker under Bolt-E
    Control menuStrip = null!;
    BankBox bankBox = null!;
    WorldPostcards postcards = null!;
    Control unlockNote = null!;
    Label unlockBig = null!;
    Label unlockSmall = null!;
    Tween? unlockPop;
    LookPicker lookPicker = null!;

    /// <summary>Happens when a different player is picked (with LB / RB, or a name typed in the name box).</summary>
    public event Action? PlayerChanged;
    /// <summary>Somebody clicked an arrow of the look picker: -1 = the look before, 1 = the next look.</summary>
    public event Action<int>? LookArrowClicked;
    /// <summary>Somebody clicked the LB (-1) or RB (1) button on a card: the player before, or the next player.</summary>
    public event Action<int>? PickerClicked;

    /// <summary>
    /// Who's playing. While a new name is being typed, it's the typed name (or, with nothing typed yet, the last person
    /// picked). With NEW PLAYER picked on the results card, it's still the last person picked. Nobody at all: "Player".
    /// </summary>
    public string PlayerName
    {
        get
        {
            if (nameBox.Visible && nameBox.Text.Trim() is { Length: > 0 } typed) return typed;
            if (playerIndex < players.Count) return players[playerIndex];
            return lastRealName.Length > 0 ? lastRealName : "Player";
        }
    }

    /// <summary>True while someone is typing in the name box (then the keyboard is for typing, not for the game).</summary>
    public bool IsTypingName => nameBox.IsVisibleInTree() && nameBox.IsEditing();

    /// <summary>True when NEW PLAYER is picked (on the results card, the play button then goes to the title card to type a name).</summary>
    public bool NewPlayerPicked => players.Count > 0 && playerIndex >= players.Count;
    /// <summary>How many people LB / RB switch between.</summary>
    public int PlayerCount => players.Count;
    /// <summary>The name between LB and RB on the title card ("" while the name box shows instead).</summary>
    public string PlayerLabelText => playerLabel.Visible ? playerLabel.Text : "";
    /// <summary>Where the name between LB and RB is on the screen (click it to type a new name).</summary>
    public Rect2 PlayerLabelRect => playerLabel.GetGlobalRect();
    /// <summary>Where the LB (-1) or RB (1) button of the card on the screen is.</summary>
    public Rect2 PickerButtonRect(int step) =>
        (titleScreen.Visible ? (step < 0 ? lbButton : rbButton) : (step < 0 ? lbButton2 : rbButton2)).GetGlobalRect();
    /// <summary>True when the LB and RB buttons show on the card that's on the screen.</summary>
    public bool PickerButtonsShowing => titleScreen.Visible
        ? lbButton.IsVisibleInTree() && rbButton.IsVisibleInTree()
        : lbButton2.IsVisibleInTree() && rbButton2.IsVisibleInTree();
    /// <summary>The words on the results card's play button, like "MAX's turn!  (A)".</summary>
    public string PlayButtonText => playButton.Text;
    /// <summary>The heading over the scores on the title card ("Family Champions").</summary>
    public string TitleHeading => titleHeading.Text;
    /// <summary>The rows of the title card's scores, like "1. | MAX | 1520".</summary>
    public List<string> TitleRows => GridRows(titleScores);
    /// <summary>The "who's champ today" line on the results card ("" when it isn't there).</summary>
    public string ResultsTodayLine => statsLine2.Visible && statsLine2.Text.StartsWith("Today: ") ? statsLine2.Text : "";
    /// <summary>The "So close!" line on the results card ("" when it isn't there).</summary>
    public string ResultsCloseLine => gameOverClose.Visible ? gameOverClose.Text : "";

    /// <summary>Where the title card is on the screen (the self-test checks it fits).</summary>
    public Rect2 TitleCardRect => titleCard.GetGlobalRect();
    /// <summary>Where the results card is on the screen (the self-test checks it fits).</summary>
    public Rect2 GameOverCardRect => gameOverCard.GetGlobalRect();
    /// <summary>True while the results card (after a game) is on the screen.</summary>
    public bool ResultsShowing => gameOverScreen.Visible;
    /// <summary>The big words at the top of the results card, like "Bonk!".</summary>
    public string ResultsTitle => gameOverTitle.Text;
    /// <summary>True while the score, bolts, best and batteries are showing.</summary>
    public bool PlayingLabelsShowing => scoreLabel.Visible && batteryRow.Visible;
    /// <summary>True when EVERY one of the score, bolts, best and batteries is hidden (the self-test checks each one).</summary>
    public bool PlayingLabelsHidden => !scoreLabel.Visible && !boltsLabel.Visible && !bestLabel.Visible && !batteryRow.Visible;
    /// <summary>How many green spare batteries the battery row is showing.</summary>
    public int BatteriesShown => batteryRow.Spares;
    /// <summary>True while the power bar (top middle) is showing.</summary>
    public bool PowerBarShowing => powerBar.Visible;
    /// <summary>How full the power bar is: 1 = the power just started, 0 = it's over.</summary>
    public float PowerBarFill => powerFill.Size.X / PowerFillWidth;
    /// <summary>True when the power bar has turned red (the power is almost over!).</summary>
    public bool PowerBarRed => powerFill.Color == PowerRed;
    /// <summary>The power the icon in the power bar drew last time it was drawn (the self-test checks it really draws).</summary>
    public PowerUp? PowerIconDrawn => powerIcon.Drawn;
    /// <summary>The big words in the middle of the screen (the last ones shown).</summary>
    public string BannerText => banner.Text;
    /// <summary>True while the big words can be seen (they fade away by themselves).</summary>
    public bool BannerShowing => banner.Modulate.A > 0.5f;
    /// <summary>The color of the big words (the last ones shown).</summary>
    public Color BannerColor => banner.LabelSettings.FontColor;
    /// <summary>True while the Best number flashes gold (NEW HIGH SCORE!).</summary>
    public bool BestFlashing => bestFlash?.IsRunning() == true;
    /// <summary>The color of the Best number right now (white, or gold while it flashes).</summary>
    public Color BestColor => bestLabel.LabelSettings.FontColor;
    /// <summary>The tip under the banner (the last one shown).</summary>
    public string HintText => hint.Text;
    /// <summary>True while the tip under the banner can be seen (it hides when a new game starts, and fades away).</summary>
    public bool HintShowing => hint.Modulate.A > 0.5f;
    /// <summary>True while the strip down the left side of the menus is showing.</summary>
    public bool MenuStripShowing => menuStrip.Visible;
    /// <summary>The World Tour postcards on the menus (the self-test reads them).</summary>
    public WorldPostcards Postcards => postcards;
    /// <summary>The World Tour line on the results card, like "You rode all the way to NIGHT CITY!".</summary>
    public string ResultsWorldLine => gameOverWorld.Visible ? gameOverWorld.Text : "";
    /// <summary>The name box on the title screen (the self-test types in it).</summary>
    public LineEdit NameBox => nameBox;
    /// <summary>The Bolt Bank box in the top-left corner of the menus (the self-test reads it).</summary>
    public BankBox BankBox => bankBox;
    /// <summary>The look picker under Bolt-E on the menus (the self-test reads it).</summary>
    public LookPicker LookPicker => lookPicker;
    /// <summary>True while the unlock party words ("NEW LOOK!") can be seen.</summary>
    public bool UnlockShowing => unlockNote.Visible && unlockNote.Modulate.A > 0.5f;
    /// <summary>The big gold unlock words, like "NEW LOOK!" (the last ones shown).</summary>
    public string UnlockBigText => unlockBig.Text;
    /// <summary>The white unlock words under them, like "COWBOY" (the last ones shown).</summary>
    public string UnlockSmallText => unlockSmall.Text;
    /// <summary>Everything in the strip on the left of the menus (the self-test checks they all fit).</summary>
    public IEnumerable<Control> StripWidgets => menuStrip.GetChildren().OfType<Control>()
        .SelectMany(widget => new[] { widget }.Concat(widget.GetChildren().OfType<Control>()));

    public override void _Ready()
    {
        // While playing: score in the top-left, best score in the top-right.
        scoreLabel = MakeLabel("Score: 0", 36, Colors.White, outline: true);
        scoreLabel.Position = new Vector2(24, 14);
        AddChild(scoreLabel);

        boltsLabel = MakeLabel("Bolts: 0", 28, Gold, outline: true);
        boltsLabel.Position = new Vector2(24, 60);
        AddChild(boltsLabel);

        // Spare batteries, under the bolts
        batteryRow = new BatteryRow { Position = new Vector2(24, 104), Size = new Vector2(200, 34), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(batteryRow);

        bestLabel = MakeLabel("Best: 0", 28, Colors.White, outline: true);
        bestLabel.HorizontalAlignment = HorizontalAlignment.Right;
        bestLabel.Position = new Vector2(1280 - 24 - 300, 16);
        bestLabel.Size = new Vector2(300, 40);
        AddChild(bestLabel);

        warningLabel = MakeLabel("", 18, new Color(1f, 0.85f, 0.85f), outline: true);
        warningLabel.Position = new Vector2(24, 690);
        AddChild(warningLabel);

        BuildBossBar();
        BuildPowerBar();

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

        BuildMenuStrip();
        BuildTitleScreen();
        BuildGameOverScreen();
    }

    /// <summary>
    /// The strip down the left side of the screen, next to the title card and the results card (where Bolt-E is).
    /// It only shows on the menus. From the top: the Bolt Bank, the World Tour postcards, the unlock party words,
    /// then Bolt-E himself (the game draws him, standing at x 172), and the look picker under his feet.
    /// Clicks go right through all of it (MouseFilter Ignore), except the look picker, which catches clicks on its arrows.
    /// </summary>
    void BuildMenuStrip()
    {
        var ignore = Control.MouseFilterEnum.Ignore;
        menuStrip = new Control { Position = Vector2.Zero, Size = new Vector2(340, 720), MouseFilter = ignore };

        bankBox = new BankBox { Position = new Vector2(16, 16), Size = new Vector2(306, 104), MouseFilter = ignore };
        menuStrip.AddChild(bankBox);

        postcards = new WorldPostcards { Position = new Vector2(16, 128), Size = new Vector2(306, 76), MouseFilter = ignore };
        menuStrip.AddChild(postcards);

        // The unlock party words: big gold "NEW LOOK!" and the look's name under it in white
        unlockNote = new Control
        {
            Position = new Vector2(16, 230), Size = new Vector2(306, 90), PivotOffset = new Vector2(153, 45),
            MouseFilter = ignore, Visible = false,
        };
        unlockBig = Centered(MakeLabel("", 30, Gold, outline: true));
        unlockBig.Size = new Vector2(306, 42);
        unlockNote.AddChild(unlockBig);
        unlockSmall = Centered(MakeLabel("", 24, Colors.White, outline: true));
        unlockSmall.Position = new Vector2(0, 44);
        unlockSmall.Size = new Vector2(306, 34);
        unlockNote.AddChild(unlockSmall);
        menuStrip.AddChild(unlockNote);

        // The look picker, right under Bolt-E's feet
        lookPicker = new LookPicker { Position = new Vector2(16, 612), Size = new Vector2(306, 96), MouseFilter = Control.MouseFilterEnum.Stop };
        lookPicker.ArrowClicked += step => LookArrowClicked?.Invoke(step);
        menuStrip.AddChild(lookPicker);

        AddChild(menuStrip);
    }

    /// <summary>
    /// Shows the Bolt Bank: how many bolts are in it, "+31" while it counts up after a game (0 = don't show it),
    /// the next prize (like "Next: COWBOY in 57"), and how full the bar is on the way there (0 to 1).
    /// </summary>
    public void ShowBank(int bank, int plus, string next, float fraction)
    {
        bankBox.Bank = bank;
        bankBox.Plus = plus;
        bankBox.Next = next;
        bankBox.Fraction = fraction;
        bankBox.QueueRedraw();
    }

    /// <summary>
    /// Shows the look Bolt-E is wearing (or trying on) in the look picker: its name, whether it's locked
    /// (then also what it takes to win it), and which look it is (number 3 of 15).
    /// </summary>
    public void ShowLook(string name, bool locked, string goal, int number, int count)
    {
        lookPicker.LookName = name;
        lookPicker.Locked = locked;
        lookPicker.Goal = goal;
        lookPicker.Number = number;
        lookPicker.Count = count;
        lookPicker.QueueRedraw();
    }

    /// <summary>
    /// The unlock party words: big gold words (like "NEW LOOK!") and white words under them (like "COWBOY").
    /// They pop in, stay for 2.5 seconds and fade away. Long big words (like "14 NEW LOOKS!") are drawn a bit smaller.
    /// </summary>
    public void ShowUnlock(string big, string small)
    {
        float wide = ThemeDB.FallbackFont.GetStringSize(big, HorizontalAlignment.Left, -1, 30).X;
        int bigSize = wide > 180 ? 24 : 30;
        SetFontSize(unlockBig, bigSize);
        SetFontSize(unlockSmall, bigSize - 6); // the white words are always a little smaller
        unlockBig.Text = big;
        unlockSmall.Text = small;
        unlockSmall.Position = new Vector2(0, bigSize + 14);

        unlockPop?.Kill();
        unlockNote.Visible = true;
        unlockNote.Modulate = Colors.White;
        unlockNote.Scale = Vector2.One * 0.3f;
        unlockPop = CreateTween();
        unlockPop.TweenProperty(unlockNote, Control.PropertyName.Scale.ToString(), Vector2.One * 1.1f, 0.15)
                 .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        unlockPop.TweenProperty(unlockNote, Control.PropertyName.Scale.ToString(), Vector2.One, 0.1);
        unlockPop.TweenInterval(2.5);
        unlockPop.TweenProperty(unlockNote, "modulate:a", 0f, 0.5);
        unlockPop.TweenCallback(Callable.From(() => unlockNote.Visible = false));
    }

    /// <summary>Hides the unlock party words right away.</summary>
    public void HideUnlock()
    {
        unlockPop?.Kill();
        unlockNote.Visible = false;
        unlockNote.Scale = Vector2.One;
    }

    static void SetFontSize(Label label, int size)
    {
        label.LabelSettings.FontSize = size;
        label.LabelSettings.OutlineSize = Math.Max(6, size / 6);
    }

    /// <summary>
    /// Shows the World Tour postcards: "reached" worlds have a picture (the rest are grey with a "?"),
    /// and the ones from "newFrom" on get a gold NEW! ribbon (99 = none are new).
    /// </summary>
    public void ShowPostcards(int reached, int newFrom = 99)
    {
        postcards.Reached = reached;
        postcards.NewFrom = newFrom;
        postcards.QueueRedraw();
    }

    void BuildTitleScreen()
    {
        var box = MakePanel(out titleScreen, out titleCard);

        var title = MakeLabel("ROBOT DASH", 76, Gold, outline: true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);

        box.AddChild(Centered(MakeLabel("Help Bolt-E the robot ride its hoverboard as far as possible!", 24, Ink)));

        // Who's playing?  [LB]  MAX  [RB]
        // (When NEW PLAYER is picked, the name box shows instead of the name, to type a new name.)
        var nameRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        nameRow.AddThemeConstantOverride("separation", 12);
        var who = MakeLabel("Who's playing?", 28, Ink);
        who.CustomMinimumSize = new Vector2(200, 0);
        who.VerticalAlignment = VerticalAlignment.Center;
        nameRow.AddChild(who);

        lbButton = SmallButton("LB", -1);
        nameRow.AddChild(lbButton);

        playerLabel = Centered(MakeLabel("", 30, PlayerPink, outline: true));
        playerLabel.CustomMinimumSize = new Vector2(240, 0);
        playerLabel.VerticalAlignment = VerticalAlignment.Center;
        playerLabel.MouseFilter = Control.MouseFilterEnum.Stop; // (so it can be clicked)
        playerLabel.GuiInput += e =>
        {
            if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) StartTyping(); // click the name: a NEW PLAYER
        };
        nameRow.AddChild(playerLabel);

        nameBox = new LineEdit { MaxLength = 16, CustomMinimumSize = new Vector2(280, 0), PlaceholderText = "type a name", Visible = false };
        nameBox.AddThemeFontSizeOverride("font_size", 28);
        nameBox.TextSubmitted += _ => NameSubmitted();                   // pressing Enter starts the game
        nameBox.TextChanged += _ => PlayerChanged?.Invoke();             // a different name: that player's bank and look
        nameBox.EditingToggled += editing => { if (!editing) TypingStopped(); };
        nameRow.AddChild(nameBox);

        rbButton = SmallButton("RB", 1);
        nameRow.AddChild(rbButton);
        box.AddChild(nameRow);

        box.AddChild(MakeButton("Start!  (Space or A)"));

        box.AddChild(Centered(MakeLabel(
            "SPACE, UP arrow, click, or the A button = jump.  Press again in the air = DOUBLE JUMP!\n" +
            "Grab bolts.  Ride UNDER the purple drones.  Jump ON bad robots to squash them!\n" +
            "LEFT / RIGHT = change look.   LB / RB or TAB = who's playing.   M or BACK = music.", 20, Ink)));

        controllerLabel = Centered(MakeLabel("", 20, Ink));
        box.AddChild(controllerLabel);

        // One row for each person: their best score ever
        titleHeading = Centered(MakeLabel("Family Champions", 30, Ink));
        box.AddChild(titleHeading);
        titleScores = MakeScoreGrid();
        box.AddChild(titleScores);
    }

    void BuildGameOverScreen()
    {
        var box = MakePanel(out gameOverScreen, out gameOverCard);

        gameOverTitle = Centered(MakeLabel("Bonk!", 64, Gold, outline: true));
        box.AddChild(gameOverTitle);
        gameOverDetails = CardLine(24);
        box.AddChild(gameOverDetails);
        gameOverWorld = CardLine(22);
        box.AddChild(gameOverWorld);
        gameOverClose = CardLine(22);
        box.AddChild(gameOverClose);
        statsLine1 = CardLine(18);
        box.AddChild(statsLine1);
        statsLine2 = CardLine(18);
        box.AddChild(statsLine2);

        box.AddChild(Centered(MakeLabel("Top Scores", 30, Ink)));
        gameOverScores = MakeScoreGrid();
        box.AddChild(gameOverScores);

        // [LB]  DAD's turn!  (A)  [RB]
        var playRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        playRow.AddThemeConstantOverride("separation", 12);
        lbButton2 = SmallButton("LB", -1);
        playRow.AddChild(lbButton2);
        playButton = MakeButton("Play again!  (Space or A)");
        playRow.AddChild(playButton);
        rbButton2 = SmallButton("RB", 1);
        playRow.AddChild(rbButton2);
        box.AddChild(playRow);
    }

    // ---------- Showing the different screens ----------

    /// <summary>The title card, with "Family Champions": one row for each person, the best score first.</summary>
    public void ShowTitle(IReadOnlyList<PlayerBest> champions)
    {
        FillFamily(titleScores, champions);
        HideBannerAndHint();
        HideBossBar();
        titleScreen.Visible = true;
        gameOverScreen.Visible = false;
        menuStrip.Visible = true;
        PlaceWarning(onMenus: true);
        SetPlayingLabelsVisible(false);
        UpdatePickerButtons();
    }

    public void ShowPlaying()
    {
        HideBossBar();
        HidePowerBar();
        HideBannerAndHint();
        titleScreen.Visible = false;
        gameOverScreen.Visible = false;
        menuStrip.Visible = false;
        PlaceWarning(onMenus: false);
        SetPlayingLabelsVisible(true);
        bestFlash?.Kill();
        bestLabel.LabelSettings.FontColor = Colors.White;
    }

    public void ShowGameOver(ResultsInfo r)
    {
        gameOverTitle.Text = r.Title;
        gameOverDetails.Text = $"Score {r.Score}  ·  {r.Meters} m  ·  {Count(r.Bolts, "bolt")}  ·  {Count(r.Stomps, "stomp")}" +
                               (r.Bosses > 0 ? $"  ·  {Count(r.Bosses, "boss", "bosses")} beaten!" : "");
        gameOverWorld.Text = r.WorldLine;
        gameOverWorld.Visible = r.WorldLine != "";
        gameOverClose.Text = r.CloseLine;
        gameOverClose.Visible = r.CloseLine != "";

        var stats = r.Stats;
        statsLine1.Text = stats is null ? "" :
            $"{ShortName(PlayerName, 10)}: {Count(stats.GamesPlayed, "game")} played  ·  best {stats.BestScore}  ·  " +
            $"{Count(stats.TotalBolts, "bolt")} collected  ·  {stats.TotalDistanceM} m travelled";
        // Who's champ today goes in the second line (when two or more people played today)
        statsLine2.Text = r.TodayLine ?? (stats is null ? "" :
            $"{Count(stats.TotalStomps, "enemy", "enemies")} stomped  ·  {Count(stats.BossesBeaten, "boss", "bosses")} beaten");
        statsLine1.Visible = stats is not null;
        statsLine2.Visible = stats is not null || r.TodayLine is not null;

        FillScores(gameOverScores, r.TopScores, r.RunId);
        UpdatePlayButton();
        UpdatePickerButtons();

        // Nothing else on the screen while the card shows, so nothing overlaps
        SetPlayingLabelsVisible(false);
        HideBannerAndHint();
        titleScreen.Visible = false;
        gameOverScreen.Visible = true;
        menuStrip.Visible = true;
        PlaceWarning(onMenus: true);
    }

    public void SetScore(int score, int bolts, int best)
    {
        scoreLabel.Text = $"Score: {score}";
        boltsLabel.Text = $"Bolts: {bolts}";
        bestLabel.Text = $"Best: {Math.Max(score, best)}";
    }

    /// <summary>NEW HIGH SCORE! The Best number in the corner flashes gold 3 times.</summary>
    public void FlashBest()
    {
        bestFlash?.Kill();
        bestFlash = CreateTween();
        for (int i = 0; i < 3; i++)
        {
            bestFlash.TweenProperty(bestLabel.LabelSettings, LabelSettings.PropertyName.FontColor.ToString(), Gold, 0.15);
            bestFlash.TweenProperty(bestLabel.LabelSettings, LabelSettings.PropertyName.FontColor.ToString(), Colors.White, 0.15);
        }
    }

    public void ShowWarning(string text) => warningLabel.Text = text;

    /// <summary>The warning words (like "Could not open the score database...") and where they are on the screen.</summary>
    public string WarningText => warningLabel.Text;
    public Rect2 WarningRect => warningLabel.GetGlobalRect();

    /// <summary>
    /// Puts the warning words where they fit: while playing, along the bottom of the screen; on the menus, in the strip
    /// on the left above Bolt-E's head (the look picker is at the bottom there).
    /// </summary>
    void PlaceWarning(bool onMenus)
    {
        warningLabel.AutowrapMode = onMenus ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
        warningLabel.Position = onMenus ? new Vector2(16, 330) : new Vector2(24, 690);
        warningLabel.Size = onMenus ? new Vector2(306, 0) : Vector2.Zero; // (0 = as small as the words allow)
    }

    // ---------- Who's playing? ----------

    /// <summary>
    /// Everyone who has played (the one who played last first), with "current" picked (it's added at the front if it
    /// isn't there yet). With nobody at all, the empty name box shows, ready for a name (it isn't typing yet).
    /// </summary>
    public void SetPlayers(List<string> names, string current)
    {
        nameBox.Visible = false; // (a fresh start: hiding it also lets go of it, without counting as "typing stopped")
        players.Clear();
        players.AddRange(names.Where(name => name.Trim().Length > 0));
        current = current.Trim();
        int index = IndexOfPlayer(current);
        if (index < 0 && current.Length > 0)
        {
            players.Insert(0, current);
            index = 0;
        }

        if (players.Count == 0)
        {
            // Nobody has played yet: type a name, or just play (then it's "Player")
            playerIndex = 0;
            lastRealName = "";
            playerLabel.Visible = false;
            nameBox.Text = current;
            nameBox.Visible = true;
        }
        else
        {
            SelectPlayer(Math.Max(0, index), tellMain: false);
        }
        UpdatePlayButton();
        UpdatePickerButtons();
    }

    /// <summary>
    /// LB (-1) or RB (1): the player before, or the next one. With canPickNew (TAB, or a click) NEW PLAYER comes after
    /// the last person: on the title card that's the name box, to type a new name. A controller never lands on NEW
    /// PLAYER, so it can never get stuck in the name box (from NEW PLAYER, RB goes to the first person, LB to the last).
    /// Returns true if something changed (false when there's nobody else to pick).
    /// </summary>
    public bool ChangePlayer(int step, bool canPickNew)
    {
        int choices = players.Count + (canPickNew ? 1 : 0);
        if (choices == 0) return false;
        int index = !canPickNew && NewPlayerPicked
            ? (step > 0 ? 0 : players.Count - 1)
            : Mathf.PosMod(playerIndex + step, choices);
        if (index == playerIndex && index < players.Count) return false; // (only one person to pick: nothing changes)

        if (index < players.Count)
        {
            SelectPlayer(index, tellMain: true);
            return true;
        }
        if (titleScreen.Visible)
        {
            if (IsTypingName) return false;
            StartTyping(); // (with nobody to pick yet, TAB goes right into the empty name box)
            return true;
        }
        bool changed = playerIndex != players.Count;
        playerIndex = players.Count; // NEW PLAYER on the results card: the play button goes to the title card to type it
        UpdatePlayButton();
        return changed;
    }

    /// <summary>NEW PLAYER: the name box shows instead of the name, ready to type (TAB, a click on the name, or the results card).</summary>
    public void StartTyping()
    {
        playerIndex = players.Count;
        playerLabel.Visible = false;
        nameBox.Visible = true;
        nameBox.Text = "";
        nameBox.PlaceholderText = "type a name";
        nameBox.GrabFocus();
        nameBox.Edit();
        UpdatePlayButton();
    }

    /// <summary>A typed name needs at least this many letters to make a new person when a game starts (see StopTyping).</summary>
    public const int ShortestNewName = 2;

    /// <summary>
    /// A game starts while the name box shows (A or START on the controller, or a click on Start): the typed name is the
    /// player now (quietly, the game is starting). But just 1 letter isn't a name yet (Dad had only started typing when
    /// the kid pressed A), so then it's the last person picked again, and no 1-letter person is saved.
    /// (Enter in the name box always keeps the typed name: Dad pressed it on purpose. See NameSubmitted.)
    /// </summary>
    public void StopTyping()
    {
        if (nameBox.Visible) FinishTyping(tellMain: false, shortest: ShortestNewName);
    }

    /// <summary>Picks this player, just like choosing them with LB / RB (they're added if they never played).</summary>
    public void SetPlayerName(string name)
    {
        name = name.Trim().Length > 0 ? name.Trim() : "Player";
        int index = IndexOfPlayer(name);
        if (index < 0)
        {
            players.Insert(0, name);
            index = 0;
        }
        SelectPlayer(index, tellMain: true);
    }

    /// <summary>Picks players[index]: the name box goes away (if it was showing), and the name shows between LB and RB.</summary>
    void SelectPlayer(int index, bool tellMain)
    {
        playerIndex = index;
        lastRealName = players[index];
        nameBox.Visible = false;              // (hidden first, so letting go of the name box below doesn't count as typing stopped)
        if (nameBox.HasFocus()) nameBox.ReleaseFocus();
        playerLabel.Text = ShortName(players[index], 12);
        playerLabel.Visible = true;
        UpdatePlayButton();
        UpdatePickerButtons();
        if (tellMain) PlayerChanged?.Invoke();
    }

    /// <summary>
    /// Typing a new name is done: the typed name is the player (added to everyone who plays, if it's new).
    /// Nothing typed (or fewer letters than "shortest")? Then it's the last person picked again (or "Player" if nobody
    /// ever played).
    /// </summary>
    void FinishTyping(bool tellMain, int shortest = 1)
    {
        string typed = nameBox.Text.Trim();
        string name = typed.Length >= shortest ? typed : lastRealName.Length > 0 ? lastRealName : "Player";
        int index = IndexOfPlayer(name);
        if (index < 0)
        {
            players.Insert(0, name);
            index = 0;
        }
        SelectPlayer(index, tellMain);
    }

    /// <summary>Enter in the name box: the typed name is the player now (even a short one), and the game starts.</summary>
    void NameSubmitted()
    {
        if (nameBox.Visible) FinishTyping(tellMain: false);
        EmitSignal(SignalName.StartPressed);
    }

    /// <summary>Typing stopped (Escape, or the name box lost focus): let go of the name box, and the typed name is the player now.</summary>
    void TypingStopped()
    {
        if (nameBox.HasFocus()) nameBox.ReleaseFocus();
        if (nameBox.Visible && titleScreen.Visible) FinishTyping(tellMain: true);
    }

    /// <summary>Where this name is in the list of players ("Sam" and "sam" are the same person), or -1.</summary>
    int IndexOfPlayer(string name) =>
        players.FindIndex(player => string.Equals(player.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>The results card's play button: "MAX's turn!  (A)" when the family takes turns, or "New player?  (Enter)".</summary>
    void UpdatePlayButton()
    {
        if (NewPlayerPicked) playButton.Text = "New player?  (Enter)";
        else if (players.Count >= 2) playButton.Text = $"{ShortName(PlayerName, 10)}'s turn!  (A)";
        else playButton.Text = "Play again!  (Space or A)";
    }

    /// <summary>LB and RB show on the title card when somebody has played, and on the results card when 2 or more have.</summary>
    void UpdatePickerButtons()
    {
        lbButton.Visible = rbButton.Visible = players.Count >= 1;
        lbButton2.Visible = rbButton2.Visible = players.Count >= 2;
    }

    /// <summary>"1 bolt", "2 bolts", "1 boss", "3 bosses"...</summary>
    static string Count(int number, string one, string? many = null) => $"{number} {(number == 1 ? one : many ?? one + "s")}";

    /// <summary>Shortens a long name so it fits: "Alexander" stays, "Maximilian-Joe" with max 10 becomes "Maximilia.".</summary>
    public static string ShortName(string name, int max) => name.Length <= max ? name : name[..(max - 1)] + ".";

    // ---------- Spare batteries ----------

    /// <summary>Shows how many spare batteries Bolt-E has, and how full the charging one is (0 to 1).</summary>
    public void SetBatteries(int spares, float fill)
    {
        batteryRow.Spares = spares;
        batteryRow.Fill = fill;
        batteryRow.PivotOffset = new Vector2(Math.Max(0, spares - 1) * BatteryRow.Gap + 20, 17); // pops around the newest one
        batteryRow.QueueRedraw();
    }

    /// <summary>A new spare battery! The batteries pop bigger for a moment.</summary>
    public void PopBattery()
    {
        batteryPop?.Kill();
        batteryRow.Scale = Vector2.One;
        batteryPop = CreateTween();
        batteryPop.TweenProperty(batteryRow, Control.PropertyName.Scale.ToString(), Vector2.One * 1.4f, 0.12);
        batteryPop.TweenProperty(batteryRow, Control.PropertyName.Scale.ToString(), Vector2.One, 0.2);
    }

    /// <summary>A spare battery zooms from the corner to Bolt-E (target is a spot in the world, like his chest).</summary>
    public void FlyBatteryTo(Vector2 target)
    {
        var flying = new BatteryRow
        {
            Spares = 1,
            ShowCharger = false,
            Size = new Vector2(44, 34),
            PivotOffset = new Vector2(20, 17),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = batteryRow.Position + new Vector2(batteryRow.Spares * BatteryRow.Gap, 0), // the slot it came from
        };
        AddChild(flying);

        var trip = flying.CreateTween();
        trip.TweenProperty(flying, Control.PropertyName.Position.ToString(), target - new Vector2(20, 17), BatteryFlightSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        trip.Parallel().TweenProperty(flying, Control.PropertyName.Rotation.ToString(), Mathf.Tau, BatteryFlightSeconds);
        trip.TweenCallback(Callable.From(flying.QueueFree)); // it goes into Bolt-E, so it disappears
    }

    // ---------- Boss fight ----------

    void BuildBossBar()
    {
        bossBar = new Control { Position = new Vector2(440, 10), Size = new Vector2(400, 64), Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        bossBarName = Centered(MakeLabel(Boss.DisplayName, 26, new Color(1f, 0.6f, 0.15f), outline: true));
        bossBarName.Size = new Vector2(400, 36);
        bossBar.AddChild(bossBarName);
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

    /// <summary>Changes the name over Big Rusty's health bar, like "BIG RUSTY (DAD)" while Dad drives him.</summary>
    public void SetBossBarName(string text) => bossBarName.Text = text;

    /// <summary>The name over Big Rusty's health bar (the self-test reads this).</summary>
    public string BossBarName => bossBarName.Text;

    /// <summary>True while Big Rusty's health bar is showing (the self-test reads this).</summary>
    public bool BossBarShowing => bossBar.Visible;

    /// <summary>Where Big Rusty's health bar is on the screen (Dad's little order words stay below it: see Main.Driver.cs).</summary>
    public Rect2 BossBarRect => bossBar.GetGlobalRect();

    // ---------- Powers from the rainbow ? boxes ----------

    const float PowerFillWidth = 224;                 // the power bar's yellow part when it's full (pixels)
    static readonly Color PowerRed = new(0.95f, 0.3f, 0.2f);

    /// <summary>
    /// The power bar, in the same spot as Big Rusty's bar (they never show together, because powers end when
    /// the boss warning sounds): the power's icon, and a bar that empties as the power runs out.
    /// </summary>
    void BuildPowerBar()
    {
        var ignore = Control.MouseFilterEnum.Ignore;
        powerBar = new Control { Position = new Vector2(490, 14), Size = new Vector2(300, 56), Visible = false, MouseFilter = ignore };
        powerIcon = new PowerIcon { Position = new Vector2(0, 2), Size = new Vector2(52, 52), MouseFilter = ignore };
        powerBar.AddChild(powerIcon);
        powerBar.AddChild(new ColorRect { Color = Ink, Position = new Vector2(60, 20), Size = new Vector2(230, 18), MouseFilter = ignore });
        powerFill = new ColorRect { Color = Gold, Position = new Vector2(63, 23), Size = new Vector2(PowerFillWidth, 12), MouseFilter = ignore };
        powerBar.AddChild(powerFill);
        AddChild(powerBar);
    }

    /// <summary>Shows the power bar: which power, and how much is left (1 = full, 0 = empty). It turns red near the end.</summary>
    public void ShowPowerBar(PowerUp power, float fraction)
    {
        fraction = Mathf.Clamp(fraction, 0, 1);
        if (!powerBar.Visible || powerIcon.Power != power)
        {
            powerIcon.Power = power;
            powerIcon.QueueRedraw();
        }
        powerBar.Visible = true;
        powerFill.Size = new Vector2(PowerFillWidth * fraction, 12);
        powerFill.Color = fraction < 0.25f ? PowerRed : Gold;
    }

    public void HidePowerBar() => powerBar.Visible = false;

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

    /// <summary>Hides the big banner words and the tip right away.</summary>
    void HideBannerAndHint()
    {
        bannerFade?.Kill();
        banner.Modulate = Colors.Transparent;
        hintFade?.Kill();
        hint.Modulate = Colors.Transparent;
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
        batteryRow.Visible = visible;
        if (!visible) HidePowerBar(); // (the power bar only shows while a power is on)
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

    /// <summary>"Family Champions" on the title card: one row for each person (rank, name, best score ever), the best first.</summary>
    void FillFamily(GridContainer grid, IReadOnlyList<PlayerBest> family)
    {
        foreach (var child in grid.GetChildren()) child.QueueFree();

        if (family.Count == 0)
        {
            grid.AddChild(MakeLabel("No scores yet.", 22, Ink));
            grid.AddChild(new Control());
            grid.AddChild(MakeLabel("Be the first!", 22, Ink));
            return;
        }

        for (int i = 0; i < family.Count; i++)
        {
            grid.AddChild(MakeLabel($"{i + 1}.", 22, Ink));
            grid.AddChild(MakeLabel(ShortName(family[i].Name, 16), 22, Ink));
            var scoreText = MakeLabel(family[i].BestScore.ToString(), 22, Ink);
            scoreText.HorizontalAlignment = HorizontalAlignment.Right;
            grid.AddChild(scoreText);
        }
    }

    /// <summary>The rows of a 3-column score grid as words, like "1. | MAX | 1520" (the self-test reads these).</summary>
    static List<string> GridRows(GridContainer grid)
    {
        var cells = grid.GetChildren().Where(cell => !cell.IsQueuedForDeletion()).Select(cell => cell is Label label ? label.Text : "").ToList();
        var rows = new List<string>();
        for (int i = 0; i + 2 < cells.Count; i += 3) rows.Add($"{cells[i]} | {cells[i + 1]} | {cells[i + 2]}");
        return rows;
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

    /// <summary>
    /// A line of words on the results card. It's always 800 pixels wide, and if the words are too long,
    /// the end gets cut off with "..." instead of making the card too big for the screen.
    /// </summary>
    static Label CardLine(int size)
    {
        var label = Centered(MakeLabel("", size, Ink));
        label.CustomMinimumSize = new Vector2(800, 0);
        label.ClipText = true;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        return label;
    }

    /// <summary>
    /// A little pink "LB" or "RB" button: the player before (step -1) or the next one (step 1).
    /// (It's not made with MakeButton, because clicking it must never start a game.)
    /// </summary>
    Button SmallButton(string text, int step)
    {
        var button = new Button
        {
            Text = text,
            FocusMode = Control.FocusModeEnum.None, // so the keys stay with the game (and the name box)
            CustomMinimumSize = new Vector2(56, 44),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        button.AddThemeFontSizeOverride("font_size", 22);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeStyleboxOverride("normal", ButtonStyle(PlayerPink, sides: 8));
        button.AddThemeStyleboxOverride("hover", ButtonStyle(PlayerPink.Lightened(0.15f), sides: 8));
        button.AddThemeStyleboxOverride("pressed", ButtonStyle(PlayerPink.Darkened(0.15f), sides: 8));
        button.Pressed += () => PickerClicked?.Invoke(step);
        return button;
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

    static StyleBoxFlat ButtonStyle(Color color, float sides = 26)
    {
        var style = new StyleBoxFlat { BgColor = color, BorderColor = Ink, ContentMarginLeft = sides, ContentMarginRight = sides };
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
    VBoxContainer MakePanel(out Control screen, out PanelContainer card)
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
        card = panel;
        return column;
    }
}

/// <summary>The little picture of the power on the left of the power bar (a white circle with the power's icon).</summary>
public partial class PowerIcon : Control
{
    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);

    /// <summary>Which power to show.</summary>
    public PowerUp Power;
    /// <summary>The power it drew the last time it was drawn (the self-test checks this).</summary>
    public PowerUp? Drawn { get; private set; }

    public override void _Draw()
    {
        var middle = new Vector2(26, 26);
        DrawCircle(middle, 25, Ink);
        DrawCircle(middle, 22.5f, new Color(1, 1, 1, 0.9f));
        PowerUps.DrawIcon(this, Power, middle, 22);
        Drawn = Power;
    }
}
