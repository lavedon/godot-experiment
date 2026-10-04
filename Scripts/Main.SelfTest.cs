using Godot;
using Microsoft.Data.Sqlite;

/// <summary>
/// Grown-up testing code! (This is more of Main, plus a little error counter at the bottom of the file.)
///
/// Start the game headless with  -- --db=C:\scratch\rd\selftest.db --selftest  (the whole command is in README.md)
/// and it plays by itself: it crashes Bolt-E on purpose, grabs hundreds of bolts, checks that the cards fit on
/// the screen, and more. Every check prints PASS or FAIL. At the end it prints "SELFTEST DONE: N passed, M failed"
/// and quits. Exit code 0 = everything passed, 1 = something failed, 2 = refused to start, 3 = it took too long.
///
/// It's careful: it refuses to start unless --db is a full path to a scratch file (never the real scores!),
/// it ignores real controllers and keyboards, and it never makes a controller rumble.
/// </summary>
public partial class Main
{
    const int SelfTestDevice = 99;          // the self-test's pretend presses come from this "device" (real ones are 0, 1, 2...)
    const string W16 = "WWWWWWWWWWWWWWWW";  // the widest name you can type: 16 W's (W is the fattest letter)

    bool selfTesting;             // true when the game was started with --selftest
    float testMoveAxis;           // the self-test's pretend stick: -1 = left, 0 = stay, 1 = right
    string selfTestDbPath = "";   // the scratch database the self-test uses
    string currentTest = "";      // the test that's running right now (printed with every check)
    int passedChecks, failedChecks;
    SelfTestLogger? errorCounter;

    /// <summary>
    /// The very first thing the game does. Normally it just says "OK!".
    /// With --selftest it makes sure --db is a full path to a scratch file. If it isn't, the game quits right away
    /// (exit code 2) before the database is even opened, so the real scores can never be touched.
    /// </summary>
    bool SelfTestArgsOk()
    {
        string[] args = OS.GetCmdlineUserArgs();
        selfTesting = args.Contains("--selftest");
        if (!selfTesting) return true;

        // The same --db the game will open (if there are several, the last one wins, just like in OpenDatabase)
        string? dbArg = args.Where(arg => arg.StartsWith("--db=")).Select(arg => arg["--db=".Length..]).LastOrDefault();
        string? problem = SelfTestDbProblem(dbArg);
        if (problem is not null)
        {
            GD.PrintErr(problem);
            SetProcess(false);
            SetProcessUnhandledInput(false);
            GetTree().Quit(2);
            return false;
        }

        selfTestDbPath = Path.GetFullPath(dbArg!);
        rumbleOn = false; // never buzz a real controller (someone might be holding it!)
        return true;
    }

    /// <summary>
    /// What's wrong with the self-test's --db path, or null if it's a safe scratch file.
    /// Safe means: a full path that is not in the save folder (where the real scores live) and not in the project folder.
    /// </summary>
    static string? SelfTestDbProblem(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "SELFTEST needs --db=<scratch file>";
        if (!Path.IsPathRooted(path)) return "SELFTEST needs a full path for --db";

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            return "SELFTEST needs a full path for --db";
        }

        string[] forbiddenFolders =
        {
            ProjectSettings.GlobalizePath("user://"), // Godot's save folder (where robotdash.db lives)
            ProjectSettings.GlobalizePath("res://"),  // the project folder
            RealSaveFolder,                           // the real save folder, even if APPDATA points somewhere else
        };
        foreach (string folder in forbiddenFolders)
            if (IsInsideFolder(fullPath, folder)) return "SELFTEST will not use the real save folder or the project folder";
        return null;
    }

    /// <summary>Where the real scores are saved, asked from Windows itself: ...\AppData\Roaming\Godot\app_userdata\Robot Dash</summary>
    static string RealSaveFolder => Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Godot", "app_userdata", "Robot Dash");

    /// <summary>True if the file is inside the folder (or inside a folder in it).</summary>
    static bool IsInsideFolder(string fullPath, string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return false;
        string fullFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)) + Path.DirectorySeparatorChar;
        return (fullPath + Path.DirectorySeparatorChar).StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Runs every test, one after another, then prints the result and quits.</summary>
    async void RunSelfTests()
    {
        // Count every engine error from now on (even ones inside _Draw or _Process)
        errorCounter = new SelfTestLogger();
        OS.AddLogger(errorCounter);

        // The watchdog: if the tests ever get stuck, give up after 15 minutes instead of waiting forever
        GetTree().CreateTimer(900, true, false, true).Timeout += () =>
        {
            GD.Print("FAIL: watchdog - the self-test took too long");
            GetTree().Quit(3);
        };

        GD.Print($"SELFTEST: starting with the scratch database {selfTestDbPath}");
        try
        {
            await Wait(0.5);
            await CheckErrorCounter();

            var tests = new (string Name, Func<Task> Run)[]
            {
                (nameof(OldDatabaseStillOpens), OldDatabaseStillOpens),
                (nameof(RefusesBadDbPaths), RefusesBadDbPaths),
                (nameof(CardsFit), CardsFit),
                (nameof(RebuildWithSpare), RebuildWithSpare),
                (nameof(RebuildInBossFight), RebuildInBossFight),
                (nameof(OneUp), OneUp),
                (nameof(NoDatabase), NoDatabase),
                (nameof(LaterSkipsOldRuns), LaterSkipsOldRuns),
                (nameof(MidAirCrash), MidAirCrash),
                (nameof(ManyCrashesOnTheRoad), ManyCrashesOnTheRoad),
                (nameof(ManyCrashesWithBigRusty), ManyCrashesWithBigRusty),
                (nameof(BossRebuildDetails), BossRebuildDetails),
                (nameof(CrashWhileRustyIsDizzy), CrashWhileRustyIsDizzy),
                (nameof(RoadRebuildDetails), RoadRebuildDetails),
                // Rainbow ? boxes and their powers
                (nameof(BoxSpawnRule), BoxSpawnRule),
                (nameof(BoxComesEventually), BoxComesEventually),
                (nameof(MegaSmashes), MegaSmashes),
                (nameof(MagnetPulls), MagnetPulls),
                (nameof(RocketFlies), RocketFlies),
                (nameof(BossWarningEndsPower), BossWarningEndsPower),
                (nameof(QuietEndOnRebuild), QuietEndOnRebuild),
                (nameof(CrashDuringWarningIsOpaque), CrashDuringWarningIsOpaque),
                (nameof(SmashDrawSmoke), SmashDrawSmoke),
                (nameof(BoxStartsTheSlotMachine), BoxStartsTheSlotMachine),
                (nameof(NeverSamePowerTwice), NeverSamePowerTwice),
                (nameof(PowerBarAndWarning), PowerBarAndWarning),
                (nameof(CrashStopsSlotMachine), CrashStopsSlotMachine),
                (nameof(BigRustyStopsPowers), BigRustyStopsPowers),
                (nameof(FirstBoxBeforeBigRusty), FirstBoxBeforeBigRusty),
                // The World Tour
                (nameof(WorldColorsReadable), WorldColorsReadable),
                (nameof(WorldChange), WorldChange),
                (nameof(MoonJump), MoonJump),
                (nameof(MoonRoadIsFair), MoonRoadIsFair),
                (nameof(WorldsDrawWithoutErrors), WorldsDrawWithoutErrors),
                (nameof(WorldSaved), WorldSaved),
                (nameof(WorldChangeCancelledByNewRun), WorldChangeCancelledByNewRun),
                (nameof(PostcardNewOnlyReached), PostcardNewOnlyReached),
                (nameof(ResetToSunny), ResetToSunny),
                (nameof(BeatRustyNextWorld), BeatRustyNextWorld),
                (nameof(RustyDressesUp), RustyDressesUp),
                (nameof(PowerMusicInWorlds), PowerMusicInWorlds),
                (nameof(SnowFalls), SnowFalls),
                // The Bolt Bank and Bolt-E's looks
                (nameof(LooksReadable), LooksReadable),
                (nameof(BankUnlocks), BankUnlocks),
                (nameof(BrowseLocked), BrowseLocked),
                (nameof(CountUpUnlocksCowboy), CountUpUnlocksCowboy),
                (nameof(QuickRestartKeepsParty), QuickRestartKeepsParty),
                (nameof(SwitchCancelsCountUp), SwitchCancelsCountUp),
                (nameof(SpecialUnlocks), SpecialUnlocks),
                (nameof(FirstLaunchNote), FirstLaunchNote),
                (nameof(OneStickPushOneStep), OneStickPushOneStep),
                (nameof(DpadUpStaysOnMenu), DpadUpStaysOnMenu),
                (nameof(LooksDraw), LooksDraw),
                (nameof(StripFits), StripFits),
                (nameof(ClickTheArrows), ClickTheArrows),
                (nameof(LooksWithoutDatabase), LooksWithoutDatabase),
                (nameof(TypingAName), TypingAName),
                // The family race: who's playing, and the flags on the road
                (nameof(KnownPlayersNoCase), KnownPlayersNoCase),
                (nameof(PlayerIsAPerson), PlayerIsAPerson),
                (nameof(FlagsPlanted), FlagsPlanted),
                (nameof(PassDadFlag), PassDadFlag),
                (nameof(PassYourFlags), PassYourFlags),
                (nameof(FlagsSurviveBoss), FlagsSurviveBoss),
                (nameof(FlagsStayInStepAfterRebuild), FlagsStayInStepAfterRebuild),
                (nameof(MidRunHighScore), MidRunHighScore),
                (nameof(ControllerNeverTypes), ControllerNeverTypes),
                (nameof(TabReachesNewPlayer), TabReachesNewPlayer),
                (nameof(TurnBannerShows), TurnBannerShows),
                (nameof(NewPlayerFromResults), NewPlayerFromResults),
                (nameof(TodayAndRound), TodayAndRound),
                (nameof(SoCloseResults), SoCloseResults),
                (nameof(TypingNeverCelebrates), TypingNeverCelebrates),
                (nameof(PlayerChangeRefreshesPostcards), PlayerChangeRefreshesPostcards),
                (nameof(FamilyWithoutDatabase), FamilyWithoutDatabase),
                // Dad drives Big Rusty
                (nameof(NobodyDrivingIsUnchanged), NobodyDrivingIsUnchanged),
                (nameof(DadTakesOver), DadTakesOver),
                (nameof(BusyAnswersWait), BusyAnswersWait),
                (nameof(AiFallback), AiFallback),
                (nameof(TiredRule), TiredRule),
                (nameof(FirstOrderNotTired), FirstOrderNotTired),
                (nameof(LaserNeedsFurious), LaserNeedsFurious),
                (nameof(LaughCooldown), LaughCooldown),
                (nameof(DriverStillDizzy), DriverStillDizzy),
                (nameof(DrivenCrashWaits), DrivenCrashWaits),
                (nameof(YouBeatDadsRusty), YouBeatDadsRusty),
                (nameof(DadGotYou), DadGotYou),
                (nameof(DriveKeysOnly), DriveKeysOnly),
                (nameof(DriverWordsFit), DriverWordsFit),
                // The final fixes (after the whole update was reviewed)
                (nameof(LaughsCantStallTheFight), LaughsCantStallTheFight),
                (nameof(OrderWordsBelowTheBar), OrderWordsBelowTheBar),
                (nameof(SmoothRiseAfterLaser), SmoothRiseAfterLaser),
                (nameof(TallerFlagPole), TallerFlagPole),
                (nameof(GravityGuard), GravityGuard),
                (nameof(MidAirWorldChange), MidAirWorldChange),
                (nameof(SunnyMusicFromTheStart), SunnyMusicFromTheStart),
                (nameof(ShortNameIsNotAPerson), ShortNameIsNotAPerson),
                (nameof(MusicToggleKeepsTheDip), MusicToggleKeepsTheDip),
                (nameof(FirstNoteNeverLost), FirstNoteNeverLost),
                (nameof(NewGameHasNoOldBoxes), NewGameHasNoOldBoxes),
                (nameof(NoTickWithNobodyElse), NoTickWithNobodyElse),
                (nameof(OnlyTodayCounts), OnlyTodayCounts),
            };

            for (int i = 0; i < tests.Length; i++)
            {
                currentTest = tests[i].Name;
                int errorsBefore = SelfTestLogger.Errors;

                // A fresh start for every test: the same "random" numbers every time, back on the title screen
                GD.Seed((ulong)(12345 + i));
                GoToTitle();
                testMoveAxis = 0;
                Engine.TimeScale = 1;
                await Wait(0.2);

                try
                {
                    await tests[i].Run();
                }
                catch (Exception e)
                {
                    Check(false, $"stopped with {e.GetType().Name}: {e.Message}");
                }
                if (SelfTestLogger.Errors > errorsBefore) Check(false, "caused engine errors (look in the log)");
            }

            currentTest = "Finish";
            int errorsAtEnd = SelfTestLogger.Errors;
            GoToTitle();
            await QuietDown();
            if (SelfTestLogger.Errors > errorsAtEnd) Check(false, "caused engine errors (look in the log)");
        }
        catch (Exception e)
        {
            Check(false, $"the self-test itself broke: {e}");
        }

        // (Every frame is 1/60 of a second of game time, because the self-test runs with --fixed-fps 60)
        GD.Print($"SELFTEST: the tests took {Engine.GetProcessFrames() / 60.0:0} seconds of game time (the watchdog stops at 900)");
        GD.Print($"SELFTEST DONE: {passedChecks} passed, {failedChecks} failed");
        OS.RemoveLogger(errorCounter);
        GetTree().Quit(failedChecks == 0 ? 0 : 1);
    }

    /// <summary>
    /// Makes sure the error counter is really listening: a printed line must reach it too.
    /// (Godot sends errors and printed lines to the same counter object, so if one gets there, both do.)
    /// </summary>
    async Task CheckErrorCounter()
    {
        currentTest = "Harness";
        int linesBefore = SelfTestLogger.Lines;
        GD.Print("SELFTEST: checking that the error counter is listening");
        await NextFrame();
        Check(SelfTestLogger.Lines > linesBefore, "the engine error counter is listening");
    }

    /// <summary>
    /// Stops every sound and gives the sound player a moment of REAL time to let go of them. (The self-test runs much
    /// faster than real time, so without this, sounds would still be playing when it quits and count as "leaked".)
    /// </summary>
    async Task QuietDown()
    {
        sounds.StopEverything();
        for (int i = 0; i < 20; i++)
        {
            OS.DelayMsec(10);
            await NextFrame();
        }
    }

    // ---------- The tests ----------

    /// <summary>A database from the very first Robot Dash still opens, keeps its scores, and gets the new columns.</summary>
    async Task OldDatabaseStillOpens()
    {
        await NextFrame();
        string oldPath = Path.Combine(Path.GetDirectoryName(selfTestDbPath)!, "old-schema.db");
        foreach (string file in new[] { oldPath, oldPath + "-journal", oldPath + "-wal", oldPath + "-shm" })
            if (File.Exists(file)) File.Delete(file);

        // The ORIGINAL tables (no stomps, no bosses, no batteries yet) with one old game in them
        using (var old = new SqliteConnection($"Data Source={oldPath};Pooling=False"))
        {
            old.Open();
            using var cmd = old.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS runs (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    player_name TEXT    NOT NULL,
                    score       INTEGER NOT NULL,
                    distance_m  INTEGER NOT NULL,
                    bolts       INTEGER NOT NULL,
                    duration_s  REAL    NOT NULL,
                    played_at   TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
                );
                CREATE INDEX IF NOT EXISTS idx_runs_score ON runs(score DESC);

                CREATE TABLE IF NOT EXISTS settings (
                    key   TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                );

                INSERT INTO runs (player_name, score, distance_m, bolts, duration_s) VALUES ('TOldTimer', 321, 200, 12, 41.5);
                """;
            cmd.ExecuteNonQuery();
        }

        long newRunId;
        using (var db = new ScoreDatabase(oldPath))
        {
            Check(db.BestScore() == 321, "the old best score is still there");
            var top = db.TopScores(5);
            Check(top.Count == 1 && top[0].PlayerName == "TOldTimer", "the old game is in Top Scores");
            var stats = db.StatsFor("TOldTimer");
            Check(stats.GamesPlayed == 1 && stats.TotalStomps == 0 && stats.BossesBeaten == 0, "old games count as 0 stomps and 0 bosses");
            Check(db.FarthestWorld("toldtimer") == 1, "an old game counts as Sunny Hills only (1 world)");
            newRunId = db.SaveRun("TOldTimer", 500, 300, 20, 2, 1, 60.0, batteriesUsed: 2, worldReached: 4);
            Check(newRunId > 1, "a new game saves into the old database");
            Check(db.StatsFor("TOldTimer").GamesPlayed == 2, "the old database now has 2 games");
            Check(db.FarthestWorld("TOLDTIMER") == 4, $"a new game that rode to Snowy Peaks counts 4 worlds, with the name in any case ({db.FarthestWorld("TOLDTIMER")})");
            Check(db.FarthestWorld("TNobodyAtAll") == 1, "a player who never played has reached Sunny Hills (1 world)");
        }

        var columns = ColumnNames(oldPath, "runs");
        foreach (string column in ColumnsAddedLater)
            Check(columns.Contains(column), $"the old database got the runs.{column} column");
        Check(AskDatabase(oldPath, "SELECT batteries_used FROM runs WHERE id = $id;", ("$id", newRunId)) == 2,
              "batteries used are saved with the game");
        Check(AskDatabase(oldPath, "SELECT COUNT(*) FROM runs WHERE batteries_used = 0 AND player_name = $name COLLATE NOCASE;",
                          ("$name", "toldtimer")) == 1, "the old game counts as 0 batteries used");
        Check(ColumnNames(selfTestDbPath, "runs").Contains("batteries_used"), "the self-test database has runs.batteries_used");
        Check(AskDatabase(oldPath, "SELECT world_reached FROM runs WHERE id = $id;", ("$id", newRunId)) == 4,
              "the worlds reached are saved with the game");
        Check(AskDatabase(oldPath, "SELECT COUNT(*) FROM runs WHERE world_reached = 1 AND id <> $id;", ("$id", newRunId)) == 1,
              "the old game counts as 1 world (Sunny Hills)");
        Check(ColumnNames(selfTestDbPath, "runs").Contains("world_reached"), "the self-test database has runs.world_reached");
    }

    /// <summary>Columns added to the runs table after the very first version. (Later features add theirs here.)</summary>
    static readonly string[] ColumnsAddedLater = { "enemies_stomped", "bosses_defeated", "batteries_used", "world_reached" };

    /// <summary>The self-test refuses any --db that isn't a full path to a scratch file.</summary>
    async Task RefusesBadDbPaths()
    {
        await NextFrame();
        Check(SelfTestDbProblem(null) is not null, "refuses no --db at all");
        Check(SelfTestDbProblem("relative.db") is not null, "refuses a relative path");
        Check(SelfTestDbProblem(ProjectSettings.GlobalizePath("user://robotdash.db")) is not null, "refuses the save folder");
        Check(SelfTestDbProblem(ProjectSettings.GlobalizePath("res://x.db")) is not null, "refuses the project folder");
        Check(SelfTestDbProblem(Path.Combine(RealSaveFolder, "robotdash.db")) is not null, "refuses the real save folder");
        Check(SelfTestDbProblem(selfTestDbPath) is null, "accepts the scratch --db in use");
    }

    /// <summary>
    /// The title card and the results card fit on the screen, even with the longest name, the biggest numbers and the
    /// longest World Tour line, and the World Tour postcards on the left never touch the cards.
    /// The family race makes them as big as they get: 5 "Family Champions" rows with the widest name, the widest name
    /// picked (with LB and RB showing), or the name box instead (typing a new name); and on the results card the
    /// "Today" line for 3 people plus a round, the "So close!" line, and the longest play button.
    /// </summary>
    async Task CardsFit()
    {
        hud.ShowTitle(FakeFamily(5, W16, 99999));
        hud.SetPlayers(new List<string> { W16, "TPB" }, W16);
        await Wait(0.2);
        Check(hud.TitleHeading == "Family Champions" && hud.TitleRows.Count == 5 && hud.TitleRows[4] == $"5. | {W16} | 99999",
              $"the title card shows 'Family Champions', one row per person ({string.Join(", ", hud.TitleRows.Take(2))}...)");
        Check(hud.PickerButtonsShowing && hud.PlayerLabelText == Hud.ShortName(W16, 12),
              $"the widest name is picked, between LB and RB ('{hud.PlayerLabelText}')");
        CheckCardFits(hud.TitleCardRect, "title card");
        CheckPostcardsBesideCard(hud.TitleCardRect, "title card");

        // Typing a new name: the name box shows instead of the name (it's a little taller)
        hud.StartTyping();
        hud.NameBox.Text = W16;
        await Wait(0.2);
        Check(hud.IsTypingName && hud.PlayerLabelText == "", "typing a new name: the name box shows instead of the name");
        CheckCardFits(hud.TitleCardRect, "title card (typing a name)");
        hud.SetPlayers(new List<string> { W16, "TPB" }, W16); // (back to the widest name)

        ResultsInfo BiggestResults(string worldLine) => new()
        {
            Title = "Out of batteries!",
            Score = 99999,
            Meters = 9999,
            Bolts = 999,
            Stomps = 99,
            Bosses = 9,
            Stats = new PlayerStats(999, 99999, 99999, 999999, 9999, 99),
            TopScores = FakeRuns(5, W16, 99999),
            RunId = 1, // so the first row also says "you!" (the widest row)
            WorldLine = worldLine,
            TodayLine = TodayLine(new() { (W16, 99999), (W16, 99999), (W16, 99999) }, W16),
            CloseLine = $"So close! Only {SoCloseMeters} m to {Hud.ShortName(W16, 10).ToUpperInvariant()}'s flag!",
        };

        hud.ShowGameOver(BiggestResults(Worlds.WorldLine(6)));
        await Wait(0.2);
        Check(hud.ResultsWorldLine == "You went all the way around the world!", $"the results card says how far the World Tour went ({hud.ResultsWorldLine})");
        Check(hud.ResultsTodayLine.StartsWith("Today: WWWWWWWWW. 99999 (champ!)") && hud.ResultsCloseLine.StartsWith("So close! Only 150 m"),
              $"the results card has the Today line and the So close line ('{hud.ResultsTodayLine}', '{hud.ResultsCloseLine}')");
        Check(hud.PlayButtonText == "WWWWWWWWW.'s turn!  (A)" && hud.PickerButtonsShowing,
              $"the longest play button, between LB and RB ('{hud.PlayButtonText}')");
        CheckCardFits(hud.GameOverCardRect, "results card");
        CheckPostcardsBesideCard(hud.GameOverCardRect, "results card");

        const string longest = "You rode all the way to SNOWY PEAKS and around again!";
        hud.ShowGameOver(BiggestResults(longest));
        await Wait(0.2);
        CheckCardFits(hud.GameOverCardRect, "results card (longest World Tour line)");
        float width = ThemeDB.FallbackFont.GetStringSize(longest, HorizontalAlignment.Left, -1, 22).X;
        Check(width <= 800, $"the longest World Tour line fits in its 800 px without being cut off ({width:0} px)");

        // The family race's big words fit on the screen too, even with the widest names
        float Wide(string text, int size) => ThemeDB.FallbackFont.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        string widest = Hud.ShortName(W16, 10);
        float turn = Wide($"{widest.ToUpperInvariant()}'S TURN!", 60), passed = Wide($"YOU PASSED {widest.ToUpperInvariant()}!", 60);
        float hint = Wide($"Beat {widest}'s 99999 from today!", 28);
        Check(turn <= 1280 && passed <= 1280 && hint <= 1280,
              $"'S TURN!' ({turn:0} px), 'YOU PASSED ...!' ({passed:0} px) and the 'Beat ...' tip ({hint:0} px) fit in 1280 px");
    }

    /// <summary>The World Tour postcards show on the menu strip on the left, and end before the card starts.</summary>
    void CheckPostcardsBesideCard(Rect2 card, string name)
    {
        var postcards = hud.Postcards.GetGlobalRect();
        Check(hud.MenuStripShowing && hud.Postcards.IsVisibleInTree(), $"the World Tour postcards show next to the {name}");
        Check(postcards.Position == new Vector2(16, 128) && postcards.End.X <= 322 && postcards.End.Y <= 204,
              $"the postcards stay in their spot on the left (x {postcards.Position.X:0} to {postcards.End.X:0}, y {postcards.Position.Y:0} to {postcards.End.Y:0})");
        Check(postcards.End.X < card.Position.X, $"the postcards end before the {name} starts");
    }

    void CheckCardFits(Rect2 card, string name)
    {
        GD.Print($"  ({name}: x {card.Position.X:0}, y {card.Position.Y:0}, {card.Size.X:0} x {card.Size.Y:0})");
        Check(new Rect2(0, 0, 1280, 720).Encloses(card), $"the {name} is inside the screen");
        Check(card.Position.X >= 330, $"the {name} starts at x 330 or more (room to see Bolt-E)");
        Check(card.End.Y <= 718, $"the {name} ends at y 718 or less");
    }

    /// <summary>Crash with a spare: KABOOM, the magic rebuild, blinking... then with no spare left, game over.</summary>
    async Task RebuildWithSpare()
    {
        PlayAs("TRebuild");

        // A real controller (any device except 99) can't start a game while the self-test runs
        Input.ParseInputEvent(new InputEventAction { Action = "start", Pressed = true, Device = 0 });
        await Wait(0.05);
        Input.ParseInputEvent(new InputEventAction { Action = "start", Pressed = false, Device = 0 });
        await Wait(0.05);
        Check(state == GameState.Title, "a real controller can't start a game during the self-test");

        StartGame();
        QuietRoad();
        await Wait(0.5);
        Check(spareBatteries == 1, "Bolt-E starts with 1 spare battery");
        Check(hud.BatteriesShown == 1 && hud.PlayingLabelsShowing, "the battery row shows 1 spare");

        int metersAtCrash = Meters;
        Check(Bonk(null), "Bonk crashes Bolt-E");
        Check(state == GameState.Rebuilding, "a crash with a spare starts the magic rebuild");
        Check(spareBatteries == 0 && batteriesUsed == 1, "the spare battery got used");
        Check(robot.Crashed, "Bolt-E blew apart");
        Check(robot.Visible && robot.Modulate == Colors.White, "the pieces show (they never blink)");
        Check(Engine.TimeScale < 1, "slow motion for the KABOOM");

        // Mashing buttons can't skip the rebuild or restart the game...
        await Press("start");
        await Release("start");
        await Press("jump");
        await Release("jump");
        Check(state == GameState.Rebuilding && robot.Crashed, "jump and start are ignored while rebuilding");

        // ...but the music button still works (BACK on a controller, M on the keyboard)
        bool musicWasOn = sounds.MusicOn;
        await PressButton(JoyButton.Back);
        Check(sounds.MusicOn != musicWasOn, "BACK still toggles the music while rebuilding");
        await TapKey(Key.M);
        Check(sounds.MusicOn == musicWasOn, "M still toggles the music while rebuilding");

        await Wait(3);
        Check(state == GameState.Playing, "BOLT-E IS BACK!");
        Check(!robot.Crashed, "every piece is back together");
        Check(robot.BlinkTime > 0, "Bolt-E is blinking (safe for a moment)");
        Check(Engine.TimeScale == 1, "the slow motion is over");
        Check(Meters > metersAtCrash, "the world kept rolling");
        Check(!Bonk(null) && state == GameState.Playing, "can't crash while blinking");

        await Wait(2.8);
        Check(robot.BlinkTime <= 0 && robot.Visible, "the blinking is over");
        int gamesBefore = database?.StatsFor(hud.PlayerName).GamesPlayed ?? -1;
        bestScore = 1_000_000; // pretend there's a giant best score, so this isn't a new high score
        Check(Bonk(null) && state == GameState.GameOver, "with no spare left, a crash is game over");

        await Wait(2);
        Check(database?.StatsFor(hud.PlayerName).GamesPlayed == gamesBefore + 1, "the game was saved");
        Check(AskDatabase(selfTestDbPath, "SELECT batteries_used FROM runs WHERE player_name = $name COLLATE NOCASE ORDER BY id DESC LIMIT 1;",
                          ("$name", "TRebuild")) == 1, "the saved game says 1 battery was used");
        Check(hud.ResultsShowing, "the results card is showing");
        Check(hud.ResultsTitle == "Out of batteries!", "the results card says 'Out of batteries!'");
        Check(hud.PlayingLabelsHidden, "score, bolts, best and batteries hide behind the results card");
    }

    /// <summary>Crash in a Big Rusty fight: he laughs and waits, his fireballs vanish, and his health stays the same.</summary>
    async Task RebuildInBossFight()
    {
        PlayAs("TBossCrash");
        StartGame();
        QuietRoad();
        SpawnBoss();
        await Wait(4);
        Check(BossFightActive, "Big Rusty is here");

        float speedAfterFight = speedToGetBackTo;
        var blast = new Blast { GroundY = GroundY, Position = new Vector2(robot.Position.X + 300, GroundY - 40) };
        AddChild(blast);
        blasts.Add(blast);

        Check(Bonk(null) && state == GameState.Rebuilding, "a crash in the boss fight starts the magic rebuild");
        Check(boss?.CurrentMove == "Recovering", "Big Rusty laughs and floats back to his spot");

        await Wait(3);
        Check(state == GameState.Playing, "Bolt-E is back in the fight");
        Check(blasts.Count == 0 && shockwaves.Count == 0 && lasers.Count == 0, "his fireballs, shockwaves and lasers vanished");
        Check(boss is not null && !boss.IsBeaten && BossFightActive, "Big Rusty is still there");
        Check(boss?.Health == Boss.MaxHealth, "his health stayed the same");
        Check(speedToGetBackTo == speedAfterFight, "the speed for after the fight stayed the same");
        Check(robot.CanMove, "Bolt-E can ride left and right again");

        // Blinking Bolt-E rides around the arena as usual
        float xBefore = robot.Position.X;
        testMoveAxis = 1;
        await Wait(0.3);
        testMoveAxis = 0;
        Check(robot.Position.X > xBefore + 50, "blinking Bolt-E can ride to the right");
    }

    /// <summary>
    /// A real bump in mid-air (not a pretend Bonk): Bolt-E is rebuilt standing on the ground, close dangers puff away
    /// (far ones stay), the world speeds back up to 85%, and while blinking bad robots can't hurt him but bolts still count.
    /// </summary>
    async Task MidAirCrash()
    {
        PlayAs("TMidAir");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        speed = 800; // going fast, so we can check the speed after the rebuild

        // (Far enough that it's still far after the world slides along while Bolt-E is in pieces)
        var farCrate = new Obstacle { Kind = ObstacleKind.Crate, Position = new Vector2(robot.Position.X + 1150, GroundY) };
        AddChild(farCrate);
        obstacles.Add(farCrate);

        robot.Jump();
        Check(await WaitUntil(() => robot.Position.Y < GroundY - 100, 1), "Bolt-E jumped up high");
        var drone = new Obstacle { Kind = ObstacleKind.Drone, Position = robot.Position + new Vector2(0, -60) };
        AddChild(drone);
        obstacles.Add(drone);

        Check(await WaitUntil(() => state == GameState.Rebuilding, 1), "bumping into a drone in mid-air starts the magic rebuild");
        float crashSpeed = speedAtCrash;
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        Check(robot.OnGround && robot.Position.Y == GroundY, "he's rebuilt standing on the ground");
        Check(!obstacles.Contains(drone), "the drone close by puffed away");
        Check(obstacles.Contains(farCrate), "the crate far away is still there");
        Check(Mathf.Abs(speedToGetBackTo - SpeedAfterRebuild * crashSpeed) < 0.01f, "the world speeds back up to 85% of the old speed");

        // While blinking: a bad robot bumping into him from the side doesn't hurt, and bolts still count
        var pupBot = new Enemy { Kind = EnemyKind.PupBot, Position = robot.Position + new Vector2(10, 0) };
        AddChild(pupBot);
        enemies.Add(pupBot);
        int boltsBefore = boltsCollected;
        AddBolt(robot.Position.X + 5, GroundY - 60);
        await Wait(0.2);
        Check(state == GameState.Playing && !robot.Crashed, "while blinking, a bad robot can't hurt Bolt-E");
        Check(boltsCollected > boltsBefore, "while blinking, Bolt-E still grabs bolts");
        Check(speed > 0, "the world is speeding back up");
    }

    /// <summary>100 bolts charge a new spare (up to 3). With 3 already, 100 bolts give bonus points.</summary>
    async Task OneUp()
    {
        await NextFrame();
        PlayAs("TOneUp");
        StartGame();
        QuietRoad();

        for (int i = 0; i < 100; i++) GotBolt(robot.Position);
        Check(spareBatteries == 2, "100 bolts charge a new spare battery (1-UP!)");
        Check(hud.BatteriesShown == 2, "the battery row shows 2 spares");

        for (int i = 0; i < 200; i++) GotBolt(robot.Position);
        Check(spareBatteries == 3, "never more than 3 spares");
        Check(bonusPoints >= 100, "with 3 spares, 100 bolts give FULL! +100");
        Check(boltsCollected == 300 && boltsTowardBattery == 0, "all 300 bolts counted");
    }

    /// <summary>Everything still works when the database can't open (for example while DB Browser has it locked).</summary>
    async Task NoDatabase()
    {
        var realDatabase = database;
        database = null;
        try
        {
            GoToTitle();
            PlayAs("TNoDb");
            StartGame();
            QuietRoad();
            spareBatteries = 0;
            bestScore = 1_000_000; // pretend there's a giant best score, so this isn't a new high score
            Check(Bonk(null), "a crash with no spare and no database");
            await Wait(2.5);
            Check(state == GameState.GameOver, "game over works without a database");
            Check(hud.ResultsShowing, "the results card shows without a database");
            Check(hud.ResultsTitle == "Bonk!", "with no batteries used, the card says 'Bonk!' like before");

            // Big Rusty blowing up after the game is over doesn't add anything (the game is already finished)
            int bossesBefore = bossesBeaten, bonusBefore = bonusPoints;
            BossBeaten();
            Check(bossesBeaten == bossesBefore && bonusPoints == bonusBefore, "a boss blowing up after game over changes nothing");
        }
        finally
        {
            database = realDatabase;
            GoToTitle();
        }
    }

    /// <summary>Later() never lets something from an old game happen in a new one.</summary>
    async Task LaterSkipsOldRuns()
    {
        PlayAs("TLater");
        StartGame();
        QuietRoad();
        bool oldGameFired = false, newGameFired = false;
        Later(0.3, () => oldGameFired = true);

        GoToTitle(); // the old game ends...
        StartGame(); // ...and a new one starts
        QuietRoad();
        Later(0.3, () => newGameFired = true);

        await Wait(0.6);
        Check(!oldGameFired, "something from an old game never happens in a new one");
        Check(newGameFired, "something from this game happens");
    }

    /// <summary>Real crates, cones and bad robots, and Bolt-E never jumps: 3 rebuilds (one per spare), then game over.</summary>
    async Task ManyCrashesOnTheRoad()
    {
        PlayAs("TSoakRoad");
        StartGame();
        nextBossAt = 1e9f; // no Big Rusty in this one
        await CrashUntilGameOver(spares: 3);
    }

    /// <summary>Big Rusty comes right away, and Bolt-E never dodges: 3 rebuilds (one per spare), then game over.</summary>
    async Task ManyCrashesWithBigRusty()
    {
        PlayAs("TSoakBoss");
        StartGame();
        spawnCountdown = 1e9f; // nothing but Big Rusty
        nextBossAt = 25;       // he comes almost right away
        await CrashUntilGameOver(spares: 3);
        Check(bossesBeaten == 0, "Bolt-E never stomped him (he never jumped)");
    }

    /// <summary>
    /// A closer look at a crash in a Big Rusty fight: he laughs once, every fireball, shockwave and laser vanishes
    /// (even ones that would still be on the screen), he waits a moment before attacking again,
    /// and the speed for after the fight stays the same.
    /// </summary>
    async Task BossRebuildDetails()
    {
        PlayAs("TBossDetails");
        StartGame();
        QuietRoad();
        speed = 800;                     // going fast before the fight, so the after-fight speed is worth checking
        SpawnBoss();
        float speedAfterFight = speedToGetBackTo;
        Check(speedAfterFight >= 799, $"Big Rusty remembers the speed for after the fight ({speedAfterFight:0})");
        robot.BlinkTime = 1000;          // safe while Big Rusty arrives
        await Wait(4);
        robot.BlinkTime = 0;
        await NextFrame();
        Check(BossFightActive && boss is not null, "Big Rusty is here");

        int laughs = 0;
        boss!.Laughed += () => laughs++;

        // One of each thing he fires, placed so it would still be on the screen when Bolt-E is back
        var blast = new Blast { GroundY = GroundY, Position = new Vector2(900, GroundY - 40), Velocity = Vector2.Zero };
        AddChild(blast);
        blasts.Add(blast);
        var wave = new Shockwave { Direction = 1, Position = new Vector2(-50, GroundY) };
        AddChild(wave);
        shockwaves.Add(wave);

        Check(Bonk(null) && state == GameState.Rebuilding, "a crash in the fight starts the magic rebuild");
        Check(laughs == 1, $"Big Rusty laughs 'HA HA HA!' (laughs: {laughs})");

        Check(await WaitUntil(() => rebuildStarted, 3), "the pieces start flying back");
        var laser = new Laser { Position = new Vector2(900, GroundY - 90), Direction = -1, ZIndex = 6 };
        AddChild(laser);
        lasers.Add(laser);

        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back in the fight");
        Check(blasts.Count == 0, $"the fireball vanished ({blasts.Count} left)");
        Check(shockwaves.Count == 0, $"the shockwave vanished ({shockwaves.Count} left)");
        Check(lasers.Count == 0, $"the laser vanished ({lasers.Count} left)");
        Check(speedToGetBackTo == speedAfterFight, $"the speed for after the fight stayed {speedAfterFight:0} (now {speedToGetBackTo:0})");

        await Wait(0.4);
        Check(boss?.CurrentMove == "Recovering", $"Big Rusty still waits just after Bolt-E is back (he's {boss?.CurrentMove})");
        await Wait(1.5);
        Check(boss?.CurrentMove != "Recovering", $"then Big Rusty attacks again (he's {boss?.CurrentMove})");
    }

    /// <summary>A crash while Big Rusty is dizzy must not take away the chance to stomp him: he stays dizzy.</summary>
    async Task CrashWhileRustyIsDizzy()
    {
        PlayAs("TDizzyCrash");
        StartGame();
        QuietRoad();
        SpawnBoss();
        robot.BlinkTime = 1000;          // Bolt-E can't crash while we wait for Big Rusty's ground pound
        Check(await WaitUntil(() => boss?.CurrentMove == "Dizzy", 30), $"Big Rusty gets dizzy after a ground pound (he's {boss?.CurrentMove})");

        // Clear away his shockwaves and helpers first, so only our pretend bump crashes Bolt-E (in this same frame)
        PuffAway(blasts, _ => true);
        PuffAway(shockwaves, _ => true);
        PuffAway(enemies, _ => true);
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.Rebuilding, "a crash while he's dizzy starts the magic rebuild");
        Check(boss?.CurrentMove == "Dizzy", $"he stays dizzy, so Bolt-E can still stomp him (he's {boss?.CurrentMove})");
    }

    /// <summary>
    /// A closer look at a crash on the road: the world keeps sliding while Bolt-E is in pieces, the music dips and
    /// comes back, a bad robot close by puffs away, there's a breather before new obstacles, Big Rusty arriving keeps
    /// the after-rebuild speed, the battery row hides behind the results card, and the title screen ends slow motion.
    /// </summary>
    async Task RoadRebuildDetails()
    {
        PlayAs("TRoadDetails");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        speed = 800;

        Check(sounds.MusicOn && sounds.MusicPlaying, "the music is playing");
        float distanceAtCrash = distance;
        Check(Bonk(null) && state == GameState.Rebuilding, "a crash on the road starts the magic rebuild");
        spawnCountdown = 100;            // (QuietRoad made it huge) - Bolt-E coming back should give a breather of at least 600
        await Wait(0.8);
        Check(state == GameState.Rebuilding, "still in pieces after 0.8 s");
        Check(distance > distanceAtCrash + 50, $"the world keeps sliding while he's in pieces ({distance - distanceAtCrash:0} px)");
        Check(Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.05f, $"the music dips down (pitch {sounds.MusicPitch:0.00})");
        sounds.SetMusicSpeed(1.15f);     // while it's dipped, a new speed is only remembered for later
        await Wait(0.2);
        Check(Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.05f, $"the music stays dipped until he's back (pitch {sounds.MusicPitch:0.00})");

        Check(await WaitUntil(() => rebuildStarted, 3), "the pieces start flying back");
        var pupBot = new Enemy { Kind = EnemyKind.PupBot, Position = robot.Position + new Vector2(150, 0) };
        AddChild(pupBot);
        enemies.Add(pupBot);

        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        Check(!enemies.Contains(pupBot), "the bad robot close by puffed away");
        Check(spawnCountdown >= 600, $"a breather before new obstacles ({spawnCountdown:0})");
        float target = speedToGetBackTo;
        Check(speed < target, $"the world is still speeding back up ({speed:0} < {target:0})");
        SpawnBoss();
        robot.BlinkTime = 1000;          // safe while Big Rusty arrives
        Check(speedToGetBackTo == target, $"Big Rusty arriving keeps the after-rebuild speed {target:0} (now {speedToGetBackTo:0})");

        await Wait(1.0);
        // (Big Rusty asked for 1.1 after we asked for 1.15 while it was dipped: the newest one wins)
        Check(Mathf.Abs(sounds.MusicPitch - 1.1f) < 0.05f, $"the music comes back up to the speed it should be (pitch {sounds.MusicPitch:0.00})");

        // Game over: the battery row hides with the other labels
        await Wait(3);
        PuffAway(blasts, _ => true);     // (nothing of Big Rusty's may bump Bolt-E before our pretend crash)
        PuffAway(shockwaves, _ => true);
        PuffAway(lasers, _ => true);
        spareBatteries = 0;
        bestScore = 1_000_000;           // so this isn't a new high score
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.GameOver, "with no spare left, a crash is game over");
        await Wait(2);
        Check(hud.ResultsShowing && hud.PlayingLabelsHidden, "score, bolts, best and the battery row all hide behind the results card");

        Engine.TimeScale = 0.3;
        GoToTitle();
        Check(Engine.TimeScale == 1, "going back to the title puts time back to normal speed");
    }

    // ---------- Rainbow ? boxes and their powers ----------

    /// <summary>When a ? box may come: only when it's time, with no power on, far from Big Rusty, and with lots of room.</summary>
    async Task BoxSpawnRule()
    {
        await NextFrame();
        PlayAs("TBoxRule");
        StartGame();
        QuietRoad();
        distance = 160 * PixelsPerMeter;
        nextBoxAt = 150;
        nextBossAt = 1e9f;
        spawnCountdown = 1000;
        TrySpawnBox(1380, 1400, 800, ObstacleKind.Crate);
        Check(boxes.Count == 1 && boxes[0].Position == new Vector2(2080, GroundY - 200),
              $"one box floats in the middle of the gap, 200 px up ({(boxes.Count > 0 ? boxes[0].Position : Vector2.Zero)})");
        Check(spawnCountdown == 1350, $"a little more room after the box (countdown {spawnCountdown:0})");
        Check(nextBoxAt >= 160 + PowerUps.BoxEveryMin && nextBoxAt <= 160 + PowerUps.BoxEveryMax, $"the next box can come 300 to 450 m later (at {nextBoxAt:0} m)");
        TrySpawnBox(1380, 1400, 800, ObstacleKind.Crate);
        Check(boxes.Count == 1, "a second call adds nothing");

        // Each rule on its own: everything else is fine, but one thing says "no box"
        bool BoxComes(Action setUp, ObstacleKind kind = ObstacleKind.Crate, float gap = 1400)
        {
            foreach (var box in boxes) box.QueueFree();
            boxes.Clear();
            nextBoxAt = 150;
            nextBossAt = 1e9f;
            setUp();
            int before = boxes.Count;
            TrySpawnBox(1380, gap, 800, kind);
            bool added = boxes.Count > before;
            power = null;     // (put back anything the setUp changed)
            roulette.Stop();
            return added;
        }
        Check(BoxComes(() => { }), "when everything is fine, a box comes");
        Check(!BoxComes(() => AddBox(900)), "never two boxes at once");
        Check(!BoxComes(() => nextBoxAt = 161), "not before it's time");
        Check(!BoxComes(() => power = PowerUp.Magnet), "not while a power is on");
        Check(!BoxComes(() => roulette.Start(PowerUp.Mega)), "not while the slot machine spins");
        Check(!BoxComes(() => nextBossAt = 160 + PowerUps.NoBoxesNearBoss), $"not within {PowerUps.NoBoxesNearBoss:0} m of Big Rusty");
        Check(BoxComes(() => nextBossAt = 160 + PowerUps.NoBoxesNearBoss + 1), $"but {PowerUps.NoBoxesNearBoss + 1:0} m before Big Rusty is fine");
        Check(!BoxComes(() => { }, ObstacleKind.Drone), "not after a drone");
        Check(!BoxComes(() => { }, gap: 800 + 150), "not without lots of room after the obstacle");
        Check(BoxComes(() => { }, gap: 800 + 160), "but with a bit more room, yes");
    }

    /// <summary>On a real road a ? box comes along. It floats so high that riding along under it never opens it.</summary>
    async Task BoxComesEventually()
    {
        PlayAs("TBoxComes");
        StartGame();
        robot.BlinkTime = 1e9f; // he can't crash in this test
        distance = 160 * PixelsPerMeter;
        nextBoxAt = 150;
        nextBossAt = 1e9f;
        spawnCountdown = 0;
        Check(await WaitUntil(() => boxes.Count > 0, 30), "a rainbow ? box comes along");
        if (boxes.Count == 0) return;

        var box = boxes[0];
        Check(box.Position.Y == GroundY - 200 && box.Position.X > ScreenWidth, $"it comes in from the right, 200 px up ({box.Position.X:0}, {box.Position.Y:0})");
        // (If Bolt-E opened it, it's gone: then stop waiting, and the next check says FAIL)
        Check(await WaitUntil(() => !boxes.Contains(box) || box.Position.X < RobotX - 80, 10), "it floats past Bolt-E");
        Check(boxes.Contains(box) && !roulette.Spinning && power is null, "riding along under it never opens it (you have to jump)");
    }

    /// <summary>
    /// MEGA BOLT-E grows giant in Mario steps, smashes a crate and a drone, flattens a bad robot from the side,
    /// shakes the ground, lasts the whole 6 seconds, and shrinks back in steps.
    /// </summary>
    async Task MegaSmashes()
    {
        PlayAs("TMega");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        int grows = sounds.TimesPlayed(Sfx.Grow), smashSounds = sounds.TimesPlayed(Sfx.Smash);
        int slams = sounds.TimesPlayed(Sfx.Slam), shrinks = sounds.TimesPlayed(Sfx.Shrink);

        StartPowerUp(PowerUp.Mega);
        float megaStartedAt = playTime;
        Check(power == PowerUp.Mega && hud.BannerText == "MEGA BOLT-E!", $"the big words say {hud.BannerText}");
        Check(sounds.TimesPlayed(Sfx.Grow) == grows + 1, "bwip-bwip-BWIIIP!");
        var growing = await WatchSizes(48); // (0.8 seconds)
        Check(EndsWithSizes(growing, 1.3f, 1.1f, 1.5f, 1.3f, PowerUps.MegaSize), $"he grows in steps, like Mario: bigger, smaller, BIGGER... ({SizesText(growing)})");
        Check(robot.Size > 1.7f, $"he grows giant (size {robot.Size:0.00})");
        Check(robot.Hitbox.Size.Y > 200 && robot.Hitbox.End.Y < GroundY, $"his bump box grows with him ({robot.Hitbox.Size.Y:0} px tall)");
        Check(Mathf.Abs(sounds.MusicPitch - 1.12f) < 0.02f, $"the music speeds up (pitch {sounds.MusicPitch:0.00})");

        var crate = AddObstacle(ObstacleKind.Crate, robot.Position.X + 10);
        int bonusBefore = bonusPoints, boltsBefore = boltsCollected;
        await Wait(0.1);
        Check(state == GameState.Playing, "bumping into a crate doesn't crash MEGA Bolt-E");
        Check(crate.Smashed && crate.InPieces, "SMASH! the crate bursts into pieces");
        Check(bonusPoints >= bonusBefore + 25, $"SMASH! +25 (got {bonusPoints - bonusBefore})");
        Check(sounds.TimesPlayed(Sfx.Smash) > smashSounds, "a crunchy SMASH sound");

        var drone = AddObstacle(ObstacleKind.Drone, robot.Position.X + 10);
        await Wait(0.1);
        Check(drone.Smashed && drone.InPieces && state == GameState.Playing, "a drone gets smashed too (he's too tall to ride under it now)");

        var pupBot = new Enemy { Kind = EnemyKind.PupBot, Position = robot.Position + new Vector2(40, 0) };
        AddChild(pupBot);
        enemies.Add(pupBot);
        int stompsBefore = stomps;
        await Wait(0.1);
        Check(pupBot.Squashed && stomps == stompsBefore + 1 && state == GameState.Playing, "SPLAT! a bad robot gets flattened from the side");
        await Wait(0.3);
        Check(boltsCollected >= boltsBefore + 4, $"each smash pops out 2 bolts ({boltsCollected - boltsBefore} grabbed)");

        robot.Jump();
        await Wait(0.4); // (a giant jump gets drawn)
        Check(await WaitUntil(() => sounds.TimesPlayed(Sfx.Slam) > slams, 2), "KA-THOOM! MEGA Bolt-E lands");
        Check(shake > 0.1f, $"every MEGA landing shakes the screen (shake {shake:0.00})");

        Check(await WaitUntil(() => power is null, 6), "MEGA BOLT-E lasts 6 seconds");
        float megaLasted = playTime - megaStartedAt;
        Check(Mathf.Abs(megaLasted - PowerUps.MegaSeconds) < 0.1f, $"...the whole {PowerUps.MegaSeconds:0} seconds, not a moment less ({megaLasted:0.00} s)");
        Check(sounds.TimesPlayed(Sfx.Shrink) == shrinks + 1, "pfffft...");
        Check(robot.BlinkTime > 0.8f, $"he blinks (safe) for a moment after the power ends ({robot.BlinkTime:0.00} s)");
        Check(!hud.PowerBarShowing, "the power bar hides");
        var shrinking = await WatchSizes(36); // (0.6 seconds)
        Check(EndsWithSizes(shrinking, 1.4f, 1.6f, 1.2f, 1.4f, 1f), $"he shrinks back in steps too ({SizesText(shrinking)})");
        Check(robot.Size < 1.05f && power == null, $"he shrinks back to normal size ({robot.Size:0.00})");
        await Wait(0.4);
        Check(Mathf.Abs(sounds.MusicPitch - 1f) < 0.02f, $"the music goes back to normal speed (pitch {sounds.MusicPitch:0.00})");
    }

    /// <summary>The BOLT MAGNET: a magnet over his head, heart eyes, nearby bolts zoom in, extra bolts sprinkle in, and the notes climb two octaves.</summary>
    async Task MagnetPulls()
    {
        PlayAs("TMagnet");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        StartPowerUp(PowerUp.Magnet);
        float magnetStartedAt = playTime;
        Check(robot.ShowMagnet && robot.Face == RobotFace.Hearts, "a red magnet over his head, and heart eyes");

        AddBolt(RobotX + 300, GroundY - 200);  // this bolt would fly right over his head...
        AddBolt(RobotX + 900, GroundY - 200);  // ...and this one is too far away to feel the magnet yet
        var farBolt = bolts[^1];
        int boltsBefore = boltsCollected;
        await NextFrame();
        await NextFrame();
        Check(Mathf.Abs(farBolt.Position.Y - (GroundY - 200)) < 0.01f, "a bolt far away doesn't feel the magnet yet");
        await Wait(1);
        Check(boltsCollected >= boltsBefore + 1, $"the magnet pulls in the bolt that would have flown over his head ({boltsCollected - boltsBefore} grabbed)");
        Check(bolts.Count(b => b.Position.X > RobotX + 450) >= 2, $"extra bolts sprinkle in from the right ({bolts.Count} on the way)");

        // Bolts grabbed quickly one after another climb up two whole octaves
        for (int i = 0; i < 20; i++) GotBolt(robot.Position);
        Check(Mathf.Abs(sounds.PitchOf(Sfx.Bolt) - 4f) < 0.01f, $"do-re-mi... up two octaves (the top note plays at pitch {sounds.PitchOf(Sfx.Bolt):0.00})");

        Check(await WaitUntil(() => power is null, 8), "the magnet lasts 8 seconds");
        float magnetLasted = playTime - magnetStartedAt;
        Check(Mathf.Abs(magnetLasted - PowerUps.MagnetSeconds) < 0.1f, $"...the whole {PowerUps.MagnetSeconds:0} seconds, not a moment less ({magnetLasted:0.00} s)");
        Check(!robot.ShowMagnet && robot.Face == RobotFace.Normal, "then the magnet goes away and his eyes are normal again");
    }

    /// <summary>
    /// The ROCKET BOARD: he zooms up high (no jumping), the world rushes by faster, nothing new comes, nothing can reach
    /// him, he grabs the bolts in the sky; then the parachute pops, he floats down slowly, and the landing spot is cleared.
    /// </summary>
    async Task RocketFlies()
    {
        PlayAs("TRocket");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        int rocketSounds = sounds.TimesPlayed(Sfx.Rocket), jumpSounds = sounds.TimesPlayed(Sfx.Jump);
        StartPowerUp(PowerUp.Rocket);
        float rocketStartedAt = playTime;
        Check(robot.Rocketing && sounds.TimesPlayed(Sfx.Rocket) == rocketSounds + 1, "FWOOOSH! the rocket board blasts off");
        await Wait(1);
        Check(robot.Position.Y < GroundY - 250, $"he zooms up high (his feet are at y {robot.Position.Y:0})");
        Check(!robot.Jump(), "no jumping on the rocket board");
        Check(boost > 1.45f, $"the world rushes by faster (boost {boost:0.00})");

        // Watch the meters, and a crate far ahead on the road, for about half a second (30 frames)
        var farCrate = AddObstacle(ObstacleKind.Crate, ScreenWidth - 40);
        float distanceBefore = distance, speedNow = speed, crateXBefore = farCrate.Position.X, timeBefore = playTime;
        for (int i = 0; i < 30; i++) await NextFrame();
        float seconds = playTime - timeBefore, metersMoved = distance - distanceBefore, crateMoved = crateXBefore - farCrate.Position.X;
        Check(metersMoved > speedNow * seconds * 1.4f && metersMoved < speedNow * seconds * 1.6f,
              $"the meters go up 1.5 times as fast ({metersMoved:0} px in {seconds:0.00} s at speed {speedNow:0})");
        Check(Mathf.Abs(crateMoved - metersMoved) < 1f, $"the road and everything on it rush by just as fast (the crate slid {crateMoved:0} px)");
        Check(Mathf.Abs(robot.RunSpeed - speed * PowerUps.RocketBoost) < 1f, $"his music notes drift back faster too (RunSpeed {robot.RunSpeed:0} at speed {speed:0})");
        PuffAway(obstacles, _ => true);

        spawnCountdown = 0;
        await NextFrame();
        await NextFrame();
        Check(spawnCountdown >= 600 && obstacles.Count == 0 && enemies.Count == 0, $"nothing new comes while he's up on the rocket board (countdown {spawnCountdown:0})");

        // Things that would crash him on the road can't reach him up here
        AddObstacle(ObstacleKind.Crate, robot.Position.X);
        var drone = AddObstacle(ObstacleKind.Drone, robot.Position.X);
        drone.Position = robot.Position + new Vector2(0, -60); // right where he is!
        await Wait(0.1);
        Check(state == GameState.Playing && !drone.Smashed, "nothing can crash him up on the rocket board");
        PuffAway(obstacles, _ => true);

        Check(await WaitUntil(() => boltsCollected > 0, 3), "he grabs the line of bolts waiting up in the sky");

        // Pop! The parachute
        Check(await WaitUntil(() => robot.Parachuting, 5), "after 5 seconds a little parachute pops open");
        float rocketLasted = playTime - rocketStartedAt;
        Check(Mathf.Abs(rocketLasted - PowerUps.RocketSeconds) < 0.1f, $"...the rocket board flew the whole {PowerUps.RocketSeconds:0} seconds, not a moment less ({rocketLasted:0.00} s)");
        Check(power is null && !robot.Rocketing && sounds.TimesPlayed(Sfx.Jump) == jumpSounds + 1, "pop!");
        Check(!robot.Jump(), "no jumping on the parachute either");
        // The parachute opens gently: right after the pop he only drops a little (with full gravity it would be about 27 px)
        float poppedAt = robot.Position.Y;
        for (int i = 0; i < 12; i++) await NextFrame(); // (0.2 seconds)
        Check(robot.Position.Y - poppedAt < 12, $"the parachute opens gently (he drops {robot.Position.Y - poppedAt:0} px in the first 0.2 seconds)");
        spawnCountdown = 0;
        await NextFrame();
        await NextFrame();
        Check(Mathf.IsEqualApprox(spawnCountdown, 600) && obstacles.Count == 0 && enemies.Count == 0,
              $"nothing new comes while he floats down, not even a bad robot (countdown {spawnCountdown:0})");

        // A bad robot right under his feet: he doesn't bounce off it while floating down
        await Wait(0.8);
        var pupBot = new Enemy { Kind = EnemyKind.PupBot, Position = robot.Position + new Vector2(0, 60) };
        AddChild(pupBot);
        enemies.Add(pupBot);
        int stompsBefore = stomps;
        float yBefore = robot.Position.Y;
        for (int i = 0; i < 30; i++) await NextFrame(); // exactly half a second (30 frames)
        float fell = robot.Position.Y - yBefore;
        Check(!pupBot.Squashed && stomps == stompsBefore && robot.Parachuting, "bad robots can't reach him on the parachute (no stomping either)");
        Check(fell > 40 && fell <= PowerUps.ParachuteFallSpeed * 0.5f + 1, $"he floats down slowly ({fell:0} px in half a second)");
        Check(boost < 1.01f, $"the world is back to normal speed (boost {boost:0.00})");
        PuffAway(enemies, _ => true);

        // A crate right where he's about to land puffs away (and so do a crate and a bad robot a bit farther away)
        Check(await WaitUntil(() => robot.Position.Y > GroundY - 40, 4), "he's nearly down");
        var landingCrate = AddObstacle(ObstacleKind.Crate, robot.Position.X + 150);
        var fartherCrate = AddObstacle(ObstacleKind.Crate, robot.Position.X + 250);
        var landingPupBot = new Enemy { Kind = EnemyKind.PupBot, Position = robot.Position with { Y = GroundY } + new Vector2(100, 0) };
        AddChild(landingPupBot);
        enemies.Add(landingPupBot);
        Check(await WaitUntil(() => robot.OnGround, 2), "he lands");
        // (In a real game the parachute already keeps the next obstacle 600 px away or more. Here we bring it close
        //  for a moment, to see the landing rule work on its own: after the parachute there's always a little breather.)
        spawnCountdown = 100;
        await NextFrame();
        Check(spawnCountdown >= 450, $"a little breather after the parachute landing (the next obstacle is {spawnCountdown:0} px away)");
        spawnCountdown = 1e9f; // (back to a quiet road)
        await Wait(0.6);
        Check(!obstacles.Contains(landingCrate), "a crate close to where he landed puffs away");
        Check(!obstacles.Contains(fartherCrate) && !enemies.Contains(landingPupBot),
              "so do a crate 250 px away and a bad robot 100 px away (everything within 300 px)");
        Check(state == GameState.Playing && robot.OnGround && !robot.Parachuting && power is null, "he lands safely and rides on");
        Check(robot.Jump(), "and he can jump again");
    }

    /// <summary>Powers end when the boss warning sounds, no new power can start, and Big Rusty's arrival puffs away boxes.</summary>
    async Task BossWarningEndsPower()
    {
        PlayAs("TBossWarning");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        int shrinks = sounds.TimesPlayed(Sfx.Shrink);
        StartPowerUp(PowerUp.Mega);
        await Wait(0.5);
        nextBossAt = Meters + 20;
        await Wait(0.5);
        Check(bossWarningShown, "the boss warning sounds");
        Check(power == null, "the boss warning ends the power");
        Check(sounds.TimesPlayed(Sfx.Shrink) == shrinks + 1 && !hud.PowerBarShowing, "MEGA Bolt-E shrinks back, and the power bar hides");
        StartPowerUp(PowerUp.Magnet);
        Check(power == null && !robot.ShowMagnet, "no power can start once the boss warning has sounded");

        var box = AddBox(1200);
        Check(await WaitUntil(() => boss is not null, 5), "Big Rusty arrives");
        Check(!boxes.Contains(box), "his arrival puffs away the ? box");
        Check(robot.Size < 1.05f, "Bolt-E is normal size for the fight");
        robot.BlinkTime = 1000; // (safe while the test finishes)
    }

    /// <summary>A crash ends a power right away and quietly: no parachute, no blinking, no sounds. Game over too.</summary>
    async Task QuietEndOnRebuild()
    {
        PlayAs("TQuietEnd");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        StartPowerUp(PowerUp.Rocket);
        await Wait(1);
        int jumps = sounds.TimesPlayed(Sfx.Jump);
        Check(robot.BlinkTime == 0, "he isn't blinking");
        Check(Bonk(null) && state == GameState.Rebuilding, "a crash on the rocket board starts the magic rebuild");
        Check(power == null && !robot.Rocketing && !robot.Parachuting, "the rocket board stops right away (no parachute)");
        Check(robot.BlinkTime == 0 && sounds.TimesPlayed(Sfx.Jump) == jumps, "quietly: no blinking and no parachute 'pop'");
        Check(!hud.PowerBarShowing && boost == 1, "the power bar hides and the world stops rushing");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        Check(robot.OnGround && robot.Position.Y == GroundY && !robot.Rocketing, "rebuilt standing on the ground, with no rocket board");

        // MEGA Bolt-E with no spare left: game over, and he's back to normal size right away
        Check(await WaitUntil(() => robot.BlinkTime <= 0, 4), "the blinking ends");
        StartPowerUp(PowerUp.Mega);
        await Wait(0.6);
        Check(robot.Size > 1.7f, "MEGA BOLT-E again");
        spareBatteries = 0;
        bestScore = 1_000_000; // (so this isn't a new high score)
        Check(Bonk(null) && state == GameState.GameOver, "with no spare left, a crash is game over (even for MEGA Bolt-E)");
        Check(power == null && robot.Size == 1f && !hud.PowerBarShowing, "the power ends right away: normal size, no power bar");
        await Wait(2);
        Check(hud.ResultsShowing && !hud.PowerBarShowing, "the results card shows (and no power bar)");
    }

    /// <summary>A crash while Bolt-E blinks for the "almost over" warning: his pieces never blink, and the blinking stops.</summary>
    async Task CrashDuringWarningIsOpaque()
    {
        PlayAs("TWarnCrash");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        StartPowerUp(PowerUp.Magnet);
        powerLeft = 1.0f;
        await Wait(0.1);
        Check(robot.PowerWarning, "in the last 1.5 seconds he blinks as a warning");
        Check(await WaitUntil(() => !robot.Visible, 0.5), "so for a split second he hides (that's the blink)");
        Check(Bonk(null) && state == GameState.Rebuilding, "a crash right then");
        Check(robot.Visible && !robot.PowerWarning, "the pieces show (no blinking in pieces)");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        Check(await WaitUntil(() => robot.BlinkTime <= 0, 4), "the blinking after the rebuild ends");
        bool flickered = false;
        for (int i = 0; i < 20; i++)
        {
            await NextFrame();
            if (!robot.Visible) flickered = true;
        }
        Check(!flickered, "and then he doesn't blink anymore (the warning ended with the crash)");
    }

    /// <summary>
    /// Every kind of obstacle bursts into pieces (the KABOOM starts right inside it) that draw without errors,
    /// can't crash Bolt-E, and go away after a while.
    /// </summary>
    async Task SmashDrawSmoke()
    {
        PlayAs("TSmashDraw");
        StartGame();
        QuietRoad();
        await Wait(0.2);
        var kinds = new[] { ObstacleKind.Crate, ObstacleKind.Cone, ObstacleKind.CrateStack, ObstacleKind.Drone };
        var smashed = kinds.Select((kind, i) => AddObstacle(kind, 900 + 110 * i)).ToList();
        foreach (var obstacle in smashed) obstacle.Smash();
        AddObstacle(ObstacleKind.Crate, robot.Position.X).Smash(); // smashed pieces right where Bolt-E is
        Check(smashed.All(o => o.BoomCenter is Vector2 boom && o.Hitbox.HasPoint(o.Position + boom)),
              $"each KABOOM starts right inside the smashed thing ({string.Join(", ", smashed.Select(o => $"{o.Kind} {o.BoomCenter}"))})");
        await Wait(1);
        Check(smashed.All(o => o.Smashed && o.InPieces && obstacles.Contains(o)), "crates, cones, crate stacks and drones all burst into bouncing pieces");
        Check(state == GameState.Playing, "smashed pieces can't crash Bolt-E");
        await Wait(1);
        Check(smashed.All(o => !obstacles.Contains(o)), "after a while the pieces go away");
    }

    /// <summary>
    /// Jumping into a box: the slot machine over Bolt-E's head goes tick-tick-tick (8 flicks, each higher), slows down,
    /// lands on a power that's not the last one, pops, and the power starts with its name and the power bar.
    /// </summary>
    async Task BoxStartsTheSlotMachine()
    {
        PlayAs("TSlotMachine");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        lastPower = PowerUp.Rocket; // (so this box gives MEGA BOLT-E or the BOLT MAGNET)
        spawnCountdown = 5000;
        int ticksBefore = sounds.TimesPlayed(Sfx.Tick), powerUpSounds = sounds.TimesPlayed(Sfx.PowerUp);

        // A box comes along, and Bolt-E jumps so he's at the top of his jump just as he gets to it
        var box = AddBox(RobotX + speed * 0.4f);
        robot.Jump();
        Check(await WaitUntil(() => !boxes.Contains(box), 1), "jumping into the box opens it");
        Check(roulette.Spinning && roulette.Visible, "the slot machine starts spinning");
        Check(roulette.Position.Y < robot.Position.Y - 150, "over his head");
        Check(spawnCountdown > 5000, $"the next obstacle waits a little longer (countdown {spawnCountdown:0})");

        int frames = 0;
        Check(await WaitUntil(() => { frames++; return power is not null; }, 3), "the slot machine lands on a power");
        float seconds = frames / 60f;
        Check(seconds > 1.0f && seconds < 1.4f, $"tick-tick-tick, slowing down, then the winner pops ({seconds:0.00} s)");
        Check(sounds.TimesPlayed(Sfx.Tick) - ticksBefore == 8, $"8 ticks ({sounds.TimesPlayed(Sfx.Tick) - ticksBefore})");
        Check(Mathf.Abs(sounds.PitchOf(Sfx.Tick) - (1f + 0.06f * 7)) < 0.001f, "each tick is a little higher than the last");
        Check(power is not null && power != PowerUp.Rocket, $"never the same power twice in a row (got {power})");
        Check(lastPower == power, $"...and it's remembered, so the next box gives a different one (lastPower {lastPower})");
        Check(roulette.Showing == power && !roulette.Spinning && !roulette.Visible, "the window showed that power, then hid");
        Check(power is not null && hud.BannerText == PowerUps.Name(power.Value), $"the name shows big ({hud.BannerText})");
        Check(sounds.TimesPlayed(Sfx.PowerUp) == powerUpSounds + 1, "ta-da!");
        await NextFrame();
        await NextFrame();
        Check(hud.PowerBarShowing && hud.PowerBarFill > 0.95f && hud.PowerIconDrawn == power, $"the power bar shows at the top, full, with its icon ({hud.PowerIconDrawn})");
    }

    /// <summary>A box never gives the same power twice in a row, but all three powers come up.</summary>
    async Task NeverSamePowerTwice()
    {
        await NextFrame();
        lastPower = null;
        var seen = new HashSet<PowerUp>();
        bool sameTwice = false;
        for (int i = 0; i < 40; i++)
        {
            var picked = PickBoxPower();
            if (picked == lastPower) sameTwice = true;
            lastPower = picked; // (StartPowerUp remembers it like this)
            seen.Add(picked);
        }
        Check(!sameTwice, "40 boxes, never the same power twice in a row");
        Check(seen.Count == 3, "all three powers come up");
    }

    /// <summary>The power bar empties as the power runs out and turns red near the end, the clock ticks, then he blinks safely.</summary>
    async Task PowerBarAndWarning()
    {
        PlayAs("TPowerBar");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        StartPowerUp(PowerUp.Magnet);
        await NextFrame();
        await NextFrame();
        Check(hud.PowerBarShowing && hud.PowerBarFill > 0.98f && !hud.PowerBarRed, "a full yellow power bar");
        Check(hud.PowerIconDrawn == PowerUp.Magnet, "with the magnet icon");
        powerLeft = PowerUps.MagnetSeconds / 2;
        await NextFrame();
        await NextFrame();
        Check(Mathf.Abs(hud.PowerBarFill - 0.5f) < 0.02f && !hud.PowerBarRed, $"half the time left: half a bar ({hud.PowerBarFill:0.00})");

        powerLeft = 1.9f;
        await Wait(0.3);
        Check(hud.PowerBarRed, "the bar turns red near the end");
        Check(!robot.PowerWarning, "with 1.6 seconds left he doesn't blink yet");
        int ticks = sounds.TimesPlayed(Sfx.Tick);
        Check(await WaitUntil(() => power is null, 3), "the power runs out");
        int warningTicks = sounds.TimesPlayed(Sfx.Tick) - ticks;
        Check(warningTicks is >= 3 and <= 4, $"tick... tick... tick in the last 1.5 seconds ({warningTicks} ticks)");
        Check(Mathf.Abs(sounds.PitchOf(Sfx.Tick) - 1.4f) < 0.001f, "the warning ticks are high");
        Check(robot.BlinkTime > 0.8f && !robot.PowerWarning, "then he blinks (and is safe) for a moment");
        Check(!hud.PowerBarShowing, "and the power bar hides");

        // The power bar hides behind the results card and the title card
        hud.ShowPowerBar(PowerUp.Rocket, 0.5f);
        hud.ShowGameOver(new ResultsInfo());
        Check(!hud.PowerBarShowing, "the results card hides the power bar");
        hud.ShowPowerBar(PowerUp.Rocket, 0.5f);
        hud.ShowTitle(new List<PlayerBest>());
        Check(!hud.PowerBarShowing, "the title card hides the power bar");
    }

    /// <summary>A crash stops the slot machine (no power later), and a new game never gets a power from the old game's slot machine.</summary>
    async Task CrashStopsSlotMachine()
    {
        PlayAs("TSlotCrash");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        OpenBox(AddBox(RobotX));
        Check(roulette.Spinning, "the slot machine spins");
        await Wait(0.3);
        Check(Bonk(null) && state == GameState.Rebuilding, "Bolt-E crashes while it spins");
        Check(!roulette.Spinning && !roulette.Visible, "the crash stops the slot machine");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        Check(power is null, "no power came from the stopped slot machine");

        OpenBox(AddBox(RobotX));
        await Wait(0.3);
        GoToTitle(); // the old game ends while the slot machine spins...
        PlayAs("TSlotCrash");
        StartGame(); // ...and a new one starts
        QuietRoad();
        await Wait(1.5);
        Check(power is null && !roulette.Visible, "a new game never gets a power from the old game's slot machine");
    }

    /// <summary>If a power were still on when Big Rusty arrives, it stops right away (and the ? boxes puff away).</summary>
    async Task BigRustyStopsPowers()
    {
        PlayAs("TRustyStops");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        StartPowerUp(PowerUp.Rocket);
        await Wait(0.5);
        var box = AddBox(1000);
        robot.BlinkTime = 1000; // (safe while Big Rusty arrives)
        SpawnBoss();
        Check(power is null && !robot.Rocketing && !robot.Parachuting, "Big Rusty arriving stops the rocket board right away");
        Check(!boxes.Contains(box) && !hud.PowerBarShowing, "the box puffs away and the power bar hides");
        Check(await WaitUntil(() => robot.OnGround, 2), "Bolt-E drops back down to the ground");
        Check(boost < 1.01f, $"the world isn't rushing anymore (boost {boost:0.00})");
    }

    /// <summary>
    /// With the normal numbers (nothing changed for the test), the first ? box comes after 100 m and before the first
    /// Big Rusty fight. (The first try at the box rule never let one come there: it wanted 150 m done AND 250 m still
    /// to go before Big Rusty, but he comes at 400 m.)
    /// </summary>
    async Task FirstBoxBeforeBigRusty()
    {
        PlayAs("TFirstBox");

        // 1. The rule, meter by meter, with the numbers a new game starts with
        StartGame();
        float firstBoxAt = nextBoxAt, bossAt = nextBossAt;
        Check(firstBoxAt == PowerUps.FirstBoxAt && bossAt == FirstBossAt, $"a new game: boxes can start at {firstBoxAt:0} m, and Big Rusty comes at {bossAt:0} m");
        int boxesEndAt = (int)(bossAt - PowerUps.NoBoxesNearBoss); // no boxes from here on, until after the fight
        var boxMeters = new List<int>();
        for (int meters = 0; meters <= bossAt; meters++)
        {
            distance = meters * PixelsPerMeter;
            nextBoxAt = firstBoxAt;
            TrySpawnBox(ScreenWidth + 100, 1400, 800, ObstacleKind.Crate); // (a crate with a big gap after it)
            if (boxes.Count > 0) boxMeters.Add(meters);
            foreach (var box in boxes) box.QueueFree();
            boxes.Clear();
        }
        Check(boxMeters.Count == boxesEndAt - (int)firstBoxAt && boxMeters.FirstOrDefault() == (int)firstBoxAt && boxMeters.LastOrDefault() == boxesEndAt - 1,
              $"a box can come from {firstBoxAt:0} m until {PowerUps.NoBoxesNearBoss:0} m before Big Rusty " +
              $"({(boxMeters.Count > 0 ? $"{boxMeters[0]} to {boxMeters[^1]} m" : "never!")})");

        // 2. Real rides on the normal road, where Bolt-E can't crash. Once in a while a ride never gets a gap big enough
        //    in time (then the first box comes right after the fight), so it gets up to 3 rides.
        int boxAt = -1, rides = 0;
        while (boxAt < 0 && rides < 3)
        {
            rides++;
            GoToTitle();
            StartGame();
            robot.BlinkTime = 1e9f; // he can't crash, so he rides all the way to Big Rusty's warning
            await WaitUntil(() => boxes.Count > 0 || bossWarningShown, 60);
            if (boxes.Count > 0 && !bossWarningShown) boxAt = Meters;
        }
        Check(boxAt >= 0, $"on a real road a rainbow ? box comes before the first Big Rusty fight (at {boxAt} m, in ride {rides})");
        Check(boxAt >= firstBoxAt && boxAt <= boxesEndAt, $"...somewhere from {firstBoxAt:0} m to {boxesEndAt} m ({boxAt} m)");
    }

    // ---------- The World Tour ----------

    /// <summary>
    /// Every world keeps Bolt-E easy to see: the hills are never too dark, and a world is "Dark" (with the glow around
    /// Bolt-E) exactly when the top of its sky is very dark. Also the worlds' order and numbers, and the results card words.
    /// </summary>
    async Task WorldColorsReadable()
    {
        await NextFrame();
        Check(string.Join(", ", Worlds.All.Select(w => w.Name)) == "SUNNY HILLS, CANDY LAND, NIGHT CITY, SNOWY PEAKS, THE MOON",
              $"the 5 worlds, in order ({string.Join(", ", Worlds.All.Select(w => w.Name))})");
        foreach (var world in Worlds.All)
        {
            float near = Worlds.Brightness(world.NearHills), far = Worlds.Brightness(world.FarHills), sky = Worlds.Brightness(world.SkyTop);
            Check(near >= 0.38f && far >= 0.38f, $"{world.Name}: the hills are bright enough to see Bolt-E's dark body (near {near:0.00}, far {far:0.00}, his body 0.24)");
            Check(world.Dark == (sky < 0.2f), $"{world.Name}: Dark = {world.Dark}, and the top of the sky has brightness {sky:0.00}");
        }

        string Numbers(Func<World, object> pick) =>
            string.Join(" ", Worlds.All.Select(w => Convert.ToString(pick(w), System.Globalization.CultureInfo.InvariantCulture)));
        Check(Numbers(w => w.MusicSpeed) == "1 1.05 0.95 1.05 0.92", $"each world has its own music speed ({Numbers(w => w.MusicSpeed)})");
        Check(Numbers(w => w.Gravity) == "1 1 1 1 0.55", $"only the Moon has floaty gravity ({Numbers(w => w.Gravity)})");
        Check(Numbers(w => w.RustyOutfit) == "None PartyHat Sunglasses BobbleHat SpaceHelmet", $"Big Rusty's outfits ({Numbers(w => w.RustyOutfit)})");
        var sunny = Worlds.All[0];
        Check(sunny.SkyTop == new Color(0.45f, 0.75f, 1f) && sunny.SkyBottom == new Color(0.85f, 0.95f, 1f) &&
              sunny.NearHills == new Color(0.45f, 0.78f, 0.45f) && sunny.Grass == new Color(0.38f, 0.75f, 0.3f) &&
              sunny.Dirt == new Color(0.72f, 0.52f, 0.33f) && sunny.Pebbles == new Color(0.6f, 0.42f, 0.27f),
              "Sunny Hills keeps Robot Dash's old colors");

        Check(Worlds.WorldLine(1) == "World: SUNNY HILLS", $"1 world: '{Worlds.WorldLine(1)}'");
        Check(Worlds.WorldLine(3) == "You rode all the way to NIGHT CITY!", $"3 worlds: '{Worlds.WorldLine(3)}'");
        Check(Worlds.WorldLine(5) == "You rode all the way to THE MOON!", $"5 worlds: '{Worlds.WorldLine(5)}'");
        Check(Worlds.WorldLine(6) == "You went all the way around the world!" && Worlds.WorldLine(9) == Worlds.WorldLine(6),
              $"6 or more: '{Worlds.WorldLine(6)}'");
    }

    /// <summary>
    /// A new world: a banner and a jingle, then the sky, hills and ground change color smoothly over 3 seconds.
    /// A second new world during the change starts from where the first one was going.
    /// </summary>
    async Task WorldChange()
    {
        PlayAs("TWorldChange");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        var sunnySky = Worlds.All[0].SkyTop;
        var candySky = Worlds.All[1].SkyTop;
        Check(LooksLike(Worlds.All[0]) && CurrentWorld.Name == "SUNNY HILLS", $"a new game starts in Sunny Hills: {DrawnColorsText()}");
        int jingles = sounds.TimesPlayed(Sfx.NewWorld);

        ChangeWorld(1);
        Check(hud.BannerText == "WELCOME TO CANDY LAND!", $"the big words say '{hud.BannerText}'");
        Check(sounds.TimesPlayed(Sfx.NewWorld) == jingles + 1, "a sparkly jingle");
        // (Another tip may still show, like "Beat MAX's 1520 from today!" from the start of the game, but not the Moon's)
        Check(!(hud.HintShowing && hud.HintText == "Moon jumps are FLOATY!"), "no Moon tip in Candy Land");
        Check(CurrentWorld.Name == "CANDY LAND" && worldsReached == 2 && worldBlend < 0.05f, $"now in Candy Land, the 2nd world (blend {worldBlend:0.00})");

        await Wait(1.5);
        float skyRed = DrawnColor("sky").R;
        Check(worldBlend > 0.4f && worldBlend < 0.6f, $"after 1.5 seconds the colors are halfway there (blend {worldBlend:0.00})");
        Check(skyRed > sunnySky.R + 0.1f && skyRed < candySky.R - 0.1f,
              $"the sky is part-way between blue and pink (red {skyRed:0.00}, from {sunnySky.R:0.00} to {candySky.R:0.00})");
        Check(Mathf.Abs(sounds.MusicPitch - 1.05f) < 0.01f, $"Candy Land's music is a little faster (pitch {sounds.MusicPitch:0.000})");

        await Wait(2);
        Check(worldBlend == 1 && CurrentWorld.Name == "CANDY LAND", $"after 3.5 seconds it's all Candy Land (blend {worldBlend:0.00})");
        Check(LooksLike(Worlds.All[1]), $"the sky, hills and ground are all Candy Land colors: {DrawnColorsText()}");
        Check(drawnWorldThings.Contains("lollipops") && drawnWorldThings.Contains("sprinkles") && !drawnWorldThings.Contains("pebbles"),
              $"lollipops and rainbow sprinkles instead of pebbles ({string.Join(", ", drawnWorldThings)})");

        // Night City, and halfway through that change, Snowy Peaks: the second change starts from Night City's colors
        ChangeWorld(2);
        await Wait(1.5);
        ChangeWorld(3);
        await NextFrame();
        await NextFrame();
        float nightRed = Worlds.All[2].SkyTop.R;
        Check(Mathf.Abs(DrawnColor("sky").R - nightRed) < 0.05f,
              $"a new world during a change starts from where the last change was going (sky red {DrawnColor("sky").R:0.00}, Night City {nightRed:0.00})");
        Check(worldsReached == 4 && CurrentWorld.Name == "SNOWY PEAKS", $"now in Snowy Peaks, the 4th world ({worldsReached})");
    }

    /// <summary>The color of a part of the world (like "grass") the last time it was drawn.</summary>
    Color DrawnColor(string name) => drawnColors.GetValueOrDefault(name, Colors.Transparent);

    /// <summary>True if the sky, the hills and the ground were just drawn in exactly this world's colors.</summary>
    bool LooksLike(World world) =>
        DrawnColor("sky").IsEqualApprox(world.SkyTop) && DrawnColor("far hills").IsEqualApprox(world.FarHills) &&
        DrawnColor("near hills").IsEqualApprox(world.NearHills) && DrawnColor("grass").IsEqualApprox(world.Grass) &&
        DrawnColor("dirt").IsEqualApprox(world.Dirt);

    /// <summary>The colors just drawn, as words (for the PASS and FAIL lines).</summary>
    string DrawnColorsText() => string.Join(", ", drawnColors.Select(c => $"{c.Key} {c.Value.ToHtml(false)}"));

    /// <summary>
    /// On the Moon gravity is weaker: a jump goes just as high (so do a small hop and a bounce), but Bolt-E hangs in
    /// the air longer.
    /// </summary>
    async Task MoonJump()
    {
        PlayAs("TMoonJump");
        StartGame();
        QuietRoad();
        await Wait(0.3);

        var (sunnyHeight, sunnyAir) = await MeasureJump();
        Check(Mathf.Abs(sunnyHeight - 212) < 15 && sunnyAir < 0.9f, $"in Sunny Hills a jump goes {sunnyHeight:0} px high and lasts {sunnyAir:0.00} s");

        ChangeWorld(4);
        Check(robot.GravityScale == 0.55f, $"on the Moon gravity is 0.55 ({robot.GravityScale})");
        Check(hud.BannerText == "WELCOME TO THE MOON!" && hud.HintShowing && hud.HintText == "Moon jumps are FLOATY!",
              $"'{hud.BannerText}' and the tip '{hud.HintText}'");

        var (height, air) = await MeasureJump();
        Check(Mathf.Abs(height - 212) < 15, $"a Moon jump goes just as high ({height:0} px)");
        Check(air > 1.0f, $"...but Bolt-E hangs in the air longer ({air:0.00} s, it was {sunnyAir:0.00} s)");

        var (hop, _) = await MeasureJump(letGoRightAway: true);
        Check(hop > 15 && hop < 40, $"letting go of jump right away is still a small hop ({hop:0} px)");

        var (bounce, bounceAir) = await MeasureJump(bounce: true);
        Check(Mathf.Abs(bounce - 254) < 15 && bounceAir > 1.1f, $"a big bounce off a bad robot goes just as high too ({bounce:0} px, {bounceAir:0.00} s)");

        Check(Mathf.Abs(sounds.MusicPitch - 0.92f) < 0.01f, $"the Moon's music is slower (pitch {sounds.MusicPitch:0.000})");
        await Wait(1);
        Check(LooksLike(Worlds.All[4]) && drawnWorldThings.Contains("earth") && !drawnWorldThings.Contains("clouds") && !drawnWorldThings.Contains("sun"),
              $"the Moon: grey ground, the Earth in the sky, and no clouds or sun ({string.Join(", ", drawnWorldThings)})");
    }

    /// <summary>Jumps (or bounces), then watches until Bolt-E lands: how high he got (pixels) and how long he was in the air (seconds).</summary>
    async Task<(float Height, float AirTime)> MeasureJump(bool letGoRightAway = false, bool bounce = false)
    {
        await NextFrame();
        float groundY = robot.Position.Y, highest = robot.Position.Y;
        if (bounce) robot.Bounce(high: true);
        else robot.Jump();
        if (letGoRightAway) robot.ReleaseJump();
        int frames = 0;
        while (frames < 180)
        {
            await NextFrame();
            frames++;
            highest = Mathf.Min(highest, robot.Position.Y);
            if (robot.OnGround) break;
        }
        return (groundY - highest, frames / 60f);
    }

    /// <summary>On the Moon the floaty jumps take longer, so obstacles and bad robots come farther apart (and still land in the gap).</summary>
    async Task MoonRoadIsFair()
    {
        PlayAs("TMoonGaps");
        StartGame();
        QuietRoad();
        await Wait(0.2);
        const float testSpeed = 420;
        float obstacleGap = testSpeed * 0.9f + 220, enemyGap = testSpeed * 0.9f + 420; // the smallest gaps in Sunny Hills

        // Lets 40 things come along (right away, without moving) and finds the smallest gap after an obstacle and after a bad robot
        (float Obstacle, float Enemy) SmallestGaps()
        {
            float smallestAfterObstacle = float.MaxValue, smallestAfterEnemy = float.MaxValue;
            for (int i = 0; i < 40; i++)
            {
                distance = 100 * PixelsPerMeter; // (at 100 m: crates, cones and bad robots)
                speed = testSpeed;
                spawnCountdown = 0;
                int enemiesBefore = enemies.Count;
                SpawnThings(0);
                if (enemies.Count > enemiesBefore) smallestAfterEnemy = Mathf.Min(smallestAfterEnemy, spawnCountdown);
                else smallestAfterObstacle = Mathf.Min(smallestAfterObstacle, spawnCountdown);
            }
            PuffAway(obstacles, _ => true);
            PuffAway(enemies, _ => true);
            PuffAway(bolts, _ => true);
            QuietRoad();
            return (smallestAfterObstacle, smallestAfterEnemy);
        }

        var sunny = SmallestGaps();
        Check(sunny.Obstacle >= obstacleGap - 0.5f && sunny.Enemy >= enemyGap - 0.5f && sunny.Obstacle < 2000 && sunny.Enemy < 2000,
              $"in Sunny Hills the gaps are the usual size (smallest {sunny.Obstacle:0} px after an obstacle, {sunny.Enemy:0} px after a bad robot)");

        ChangeWorld(4);
        float stretch = 1 / Mathf.Sqrt(0.55f);
        var moon = SmallestGaps();
        Check(moon.Obstacle >= obstacleGap * stretch - 0.5f && moon.Obstacle < 3000,
              $"on the Moon obstacles come farther apart (smallest gap {moon.Obstacle:0} px, at least {obstacleGap * stretch:0})");
        Check(moon.Enemy >= enemyGap * stretch - 0.5f && moon.Enemy < 3000,
              $"...and so do bad robots (smallest gap {moon.Enemy:0} px, at least {enemyGap * stretch:0})");
        Check(sunny.Obstacle < obstacleGap * stretch, "(in Sunny Hills some gaps were smaller than that)");
    }

    /// <summary>
    /// Rides through every world, all the way around: each one draws its own special things (and no engine errors),
    /// and back in Sunny Hills it looks exactly like it always did.
    /// </summary>
    async Task WorldsDrawWithoutErrors()
    {
        PlayAs("TWorldDraw");
        StartGame();
        QuietRoad();
        robot.BlinkTime = 1e9f;
        await Wait(0.2);
        string[][] specialThings =
        {
            new[] { "sun", "clouds", "pebbles" },                                         // Sunny Hills
            new[] { "sun", "clouds", "lollipops", "sprinkles" },                          // Candy Land
            new[] { "moon", "clouds", "stars", "city", "road dashes", "night glow" },     // Night City
            new[] { "sun", "clouds", "mountains", "pines", "pebbles", "snow" },           // Snowy Peaks
            new[] { "earth", "stars", "craters", "night glow" },                          // The Moon
        };
        for (int i = 0; i <= 5; i++)
        {
            int errorsBefore = SelfTestLogger.Errors;
            ChangeWorld(i);
            Burst(robot.Position, 10, Colors.White, 200f); // (some puffs on top, too)
            await Wait(0.4);
            var expected = specialThings[i % 5];
            var missing = expected.Where(thing => !drawnWorldThings.Contains(thing)).ToList();
            Check(missing.Count == 0, $"{CurrentWorld.Name} draws its {string.Join(", ", expected)}" +
                                      (missing.Count > 0 ? $" (missing: {string.Join(", ", missing)})" : ""));
            Check(SelfTestLogger.Errors == errorsBefore, $"{CurrentWorld.Name} draws without errors");
        }
        Check(hud.BannerText == "YOU WENT ALL THE WAY AROUND!" && CurrentWorld.Name == "SUNNY HILLS" && worldsReached == 6,
              $"after the Moon: '{hud.BannerText}', back in {CurrentWorld.Name} (worlds reached: {worldsReached})");

        await Wait(3);
        Check(drawnWorldThings.SetEquals(specialThings[0]), $"back in Sunny Hills it looks like it always did ({string.Join(", ", drawnWorldThings)})");
        Check(LooksLike(Worlds.All[0]) && robot.GravityScale == 1, $"Sunny Hills colors and normal gravity again: {DrawnColorsText()}");
    }

    /// <summary>A game that rode to Night City saves 3 worlds, and the results card and the postcards say so.</summary>
    async Task WorldSaved()
    {
        PlayAs("TWORLD");
        int before = database?.FarthestWorld("TWORLD") ?? 1; // (1 in a new scratch database)
        StartGame();
        QuietRoad();
        await Wait(0.3);
        ChangeWorld(2);
        Check(worldsReached == 3, $"riding into Night City makes 3 worlds ({worldsReached})");
        spareBatteries = 0;
        bestScore = 1_000_000; // (so this isn't a new high score)
        Check(Bonk(null) && state == GameState.GameOver, "a crash with no spare: game over");
        Check(AskDatabase(selfTestDbPath, "SELECT world_reached FROM runs WHERE player_name = $name COLLATE NOCASE ORDER BY id DESC LIMIT 1;",
                          ("$name", "tworld")) == 3, "the saved game says it rode through 3 worlds");
        Check((database?.FarthestWorld("TWORLD") ?? 0) >= 3, $"FarthestWorld is 3 or more ({database?.FarthestWorld("TWORLD")})");

        await Wait(2);
        Check(hud.ResultsShowing && hud.ResultsWorldLine == "You rode all the way to NIGHT CITY!", $"the results card says '{hud.ResultsWorldLine}'");
        var postcards = hud.Postcards;
        Check(hud.MenuStripShowing && postcards.Reached == Math.Max(3, Math.Min(5, before)) && postcards.NewFrom == Math.Min(5, before),
              $"the postcards show the worlds reached ({postcards.Reached}), NEW from the old farthest world ({postcards.NewFrom})");
        if (before < 2)
            Check(postcards.IsNew(1) && postcards.IsNew(2) && !postcards.IsNew(0) && !postcards.IsNew(3), "Candy Land and Night City get a NEW! ribbon");
        await NextFrame();
        await NextFrame();
        Check(postcards.PicturesDrawn == postcards.Reached && postcards.QuestionMarksDrawn == 5 - postcards.Reached,
              $"pictures of the worlds reached, '?' for the rest ({postcards.PicturesDrawn} pictures, {postcards.QuestionMarksDrawn} question marks)");

        // The next game goes all the way around (the results card says so, and every postcard has a picture)
        before = database?.FarthestWorld("TWORLD") ?? 1;
        StartGame();
        QuietRoad();
        ChangeWorld(5);
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over after going all the way around");
        await Wait(2);
        Check(hud.ResultsWorldLine == "You went all the way around the world!", $"the results card says '{hud.ResultsWorldLine}'");
        Check(postcards.Reached == 5 && postcards.NewFrom == Math.Min(5, before), $"all 5 postcards ({postcards.Reached}), NEW from {postcards.NewFrom}");
        if (before == 3)
            Check(postcards.IsNew(3) && postcards.IsNew(4) && !postcards.IsNew(2), "Snowy Peaks and the Moon get a NEW! ribbon (Night City isn't new anymore)");
        Check(AskDatabase(selfTestDbPath, "SELECT world_reached FROM runs WHERE player_name = $name COLLATE NOCASE ORDER BY id DESC LIMIT 1;",
                          ("$name", "TWORLD")) == 6, "the saved game says 6 worlds (all the way around)");

        // Back on the title screen: the postcards of every world this player has been to, and no NEW! ribbons
        GoToTitle();
        Check(hud.PlayerName == "TWORLD" && postcards.Reached == 5 && postcards.NewFrom == 99 && !postcards.IsNew(4),
              $"the title screen shows {hud.PlayerName}'s postcards ({postcards.Reached}), none NEW");
    }

    /// <summary>
    /// The "next world soon" after beating Big Rusty comes 2.2 seconds later, but never into a new game, never after a
    /// game over, and it still comes while Bolt-E is in pieces (then the music comes back at the new world's speed).
    /// </summary>
    async Task WorldChangeCancelledByNewRun()
    {
        PlayAs("TWorldCancel");
        StartGame();
        QuietRoad();
        await Wait(0.2);

        NextWorldSoon(); // (the same as after beating Big Rusty)
        await Wait(1.5);
        Check(worldNumber == 0, "after 1.5 seconds, not yet (YOU BEAT BIG RUSTY! shows first)");
        Check(await WaitUntil(() => worldNumber == 1, 1.5), "then the next world comes");

        // A new game starts while the next world is on its way
        GoToTitle();
        StartGame();
        QuietRoad();
        NextWorldSoon();
        await Wait(1);
        GoToTitle();
        StartGame();
        QuietRoad();
        await Wait(2);
        Check(worldNumber == 0 && CurrentWorld.Name == "SUNNY HILLS", $"the old game's new world never comes into the new game ({CurrentWorld.Name})");

        // Game over while it's on its way
        NextWorldSoon();
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over");
        await Wait(3); // (2.2 seconds of game time, plus a little for the slow motion)
        Check(worldNumber == 0, "no new world after the game is over");

        // In pieces when it comes: the new world still comes, and the music comes back up at the new world's speed
        GoToTitle();
        StartGame();
        QuietRoad();
        await Wait(0.2);
        NextWorldSoon();
        await Wait(1.2);
        Check(Bonk(null) && state == GameState.Rebuilding, "Bolt-E crashes with a spare");
        Check(await WaitUntil(() => worldNumber == 1, 2.5), "the new world comes while he's in pieces");
        Check(state == GameState.Rebuilding && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.05f,
              $"(he's still in pieces, and the music stays dipped: pitch {sounds.MusicPitch:0.00})");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        await Wait(1);
        Check(Mathf.Abs(sounds.MusicPitch - 1.05f) < 0.01f, $"the music comes back at Candy Land's speed (pitch {sounds.MusicPitch:0.000})");
    }

    /// <summary>The postcards: pictures of the worlds reached, '?' for the rest, and NEW! ribbons only on new worlds that were reached.</summary>
    async Task PostcardNewOnlyReached()
    {
        await NextFrame();
        Check(hud.MenuStripShowing, "the menu strip shows on the title screen");
        Check(hud.Postcards.MouseFilter == Control.MouseFilterEnum.Ignore &&
              hud.Postcards.GetParent<Control>().MouseFilter == Control.MouseFilterEnum.Ignore, "clicks go right through the postcards");

        hud.ShowPostcards(2, 1);
        Check(hud.Postcards.IsNew(1), "Candy Land's postcard is NEW");
        Check(!hud.Postcards.IsNew(0) && !hud.Postcards.IsNew(2) && !hud.Postcards.IsNew(4), "Sunny Hills and the worlds not reached yet are not NEW");
        await NextFrame();
        await NextFrame();
        var postcards = hud.Postcards;
        Check(postcards.PicturesDrawn == 2 && postcards.QuestionMarksDrawn == 3 && postcards.RibbonsDrawn == 1,
              $"2 pictures, 3 question marks and 1 NEW! ribbon ({postcards.PicturesDrawn}, {postcards.QuestionMarksDrawn}, {postcards.RibbonsDrawn})");

        hud.ShowPostcards(5);
        await NextFrame();
        await NextFrame();
        Check(postcards.PicturesDrawn == 5 && postcards.QuestionMarksDrawn == 0 && postcards.RibbonsDrawn == 0,
              $"all 5 worlds reached: 5 pictures, and no ribbons on the title screen ({postcards.PicturesDrawn}, {postcards.RibbonsDrawn})");

        PlayAs("TPostcards");
        StartGame();
        Check(!hud.MenuStripShowing, "the postcards hide while playing");
    }

    /// <summary>A new game (and the title screen) always starts in Sunny Hills, with normal gravity and music.</summary>
    async Task ResetToSunny()
    {
        PlayAs("TResetSunny");
        StartGame();
        QuietRoad();
        ChangeWorld(4);
        await Wait(1);
        Check(CurrentWorld.Name == "THE MOON" && robot.GravityScale < 1, "on the Moon");

        GoToTitle();
        Check(worldNumber == 0 && robot.GravityScale == 1 && worldsReached == 1 && worldBlend == 1, "the title screen is back in Sunny Hills");

        ChangeWorld(4); // (pretend: the Moon again, right before a new game)
        StartGame();
        Check(worldNumber == 0 && robot.GravityScale == 1, $"a new game starts in Sunny Hills with normal gravity (world {worldNumber}, gravity {robot.GravityScale})");
        Check(fromWorld == Worlds.All[0] && toWorld == Worlds.All[0] && worldBlend == 1 && worldsReached == 1,
              "right away, with no color change still going on");
        await Wait(1);
        Check(LooksLike(Worlds.All[0]), $"the sky, hills and ground are Sunny Hills colors: {DrawnColorsText()}");
        Check(Mathf.Abs(sounds.MusicPitch - 1f) < 0.01f, $"the music plays at normal speed (pitch {sounds.MusicPitch:0.000})");
    }

    /// <summary>
    /// Beating Big Rusty in Candy Land: his party hat flies off when he blows up, "YOU BEAT BIG RUSTY!" shows first,
    /// and a moment later Bolt-E rides on into Night City. The fight music is faster than Candy Land's own speed.
    /// </summary>
    async Task BeatRustyNextWorld()
    {
        PlayAs("TBeatRusty");
        StartGame();
        QuietRoad();
        ChangeWorld(1);
        robot.BlinkTime = 1e9f; // (nothing Big Rusty does can crash Bolt-E in this test)
        SpawnBoss();
        var rusty = boss!;
        Check(rusty.Outfit == BossOutfit.PartyHat && rusty.OutfitOnHead, "in Candy Land Big Rusty wears a party hat");
        await Wait(2);
        Check(Mathf.Abs(sounds.MusicPitch - 1.1f * 1.05f) < 0.01f, $"the fight music is 1.1 times Candy Land's speed (pitch {sounds.MusicPitch:0.000})");

        rusty.LoseHealth(2); // (as if he was stomped twice)
        Check(await WaitUntil(() => rusty.CurrentMove == "GettingAngry", 3), "he gets ANGRY");
        await Wait(1);
        Check(Mathf.Abs(sounds.MusicPitch - 1.15f * 1.05f) < 0.01f, $"the ANGRY music is 1.15 times Candy Land's speed (pitch {sounds.MusicPitch:0.000})");

        int jingles = sounds.TimesPlayed(Sfx.NewWorld);
        rusty.LoseHealth(4); // (the rest of his health)
        Check(await WaitUntil(() => rusty.HasBlownUp, 3), "he breaks down and blows up");
        Check(rusty.HatFlewOff && !rusty.OutfitOnHead, "his party hat flies off as its own piece");
        Check(hud.BannerText == $"YOU BEAT {Boss.DisplayName}!" && bossesBeaten == 1, $"'{hud.BannerText}'");
        await Wait(1);
        Check(worldNumber == 1 && hud.BannerText == $"YOU BEAT {Boss.DisplayName}!", "a second later it's still Candy Land, so the victory words show first");
        Check(Mathf.Abs(sounds.MusicPitch - 1.05f) < 0.01f, $"the music goes back to Candy Land's speed (pitch {sounds.MusicPitch:0.000})");

        Check(await WaitUntil(() => worldNumber == 2, 2), "then on to the next world");
        Check(CurrentWorld.Name == "NIGHT CITY" && hud.BannerText == "WELCOME TO NIGHT CITY!" && worldsReached == 3 &&
              sounds.TimesPlayed(Sfx.NewWorld) == jingles + 1, $"'{hud.BannerText}' with a jingle (worlds reached: {worldsReached})");
        await Wait(1);
        Check(Mathf.Abs(sounds.MusicPitch - 0.95f) < 0.01f, $"Night City's music is a little slower (pitch {sounds.MusicPitch:0.000})");
    }

    /// <summary>
    /// Big Rusty dresses up for every world, and every outfit draws without errors. The sunglasses go up on his forehead
    /// when he's hurt (so you see his X eyes), the hats fly off when he blows up, and the sunglasses and helmet stay on.
    /// </summary>
    async Task RustyDressesUp()
    {
        var outfits = new[] { BossOutfit.None, BossOutfit.PartyHat, BossOutfit.Sunglasses, BossOutfit.BobbleHat, BossOutfit.SpaceHelmet };
        for (int i = 0; i < Worlds.All.Length; i++)
        {
            PlayAs("TRustyOutfits");
            StartGame(); // (a new game for each world, so the "next world soon" after he blows up never happens)
            QuietRoad();
            if (i > 0) ChangeWorld(i);
            robot.BlinkTime = 1e9f;
            int errorsBefore = SelfTestLogger.Errors;
            SpawnBoss();
            var rusty = boss!;
            string world = CurrentWorld.Name;
            Check(rusty.Outfit == outfits[i] && rusty.OutfitOnHead, $"in {world} Big Rusty wears: {rusty.Outfit}");
            await Wait(2); // he flies in and laughs

            if (rusty.Outfit == BossOutfit.Sunglasses)
            {
                // Over his eyes while he laughs and shoots. Up on his forehead when you need to see his eyes.
                Check(!rusty.SunglassesUp, $"his sunglasses are over his eyes while he's {rusty.CurrentMove}");
                Check(await WaitUntil(() => rusty.CurrentMove == "Shooting", 3) && !rusty.SunglassesUp, "...and while he shoots");
                Check(await WaitUntil(() => rusty.CurrentMove == "PoundRise", 8) && rusty.SunglassesUp,
                      "they go up on his forehead when he winds up a ground pound (so you see his eyes flash)");
                Check(await WaitUntil(() => rusty.CurrentMove == "Dizzy", 5) && rusty.SunglassesUp, "...while he's dizzy (so you see his spinning eyes)");
                rusty.LoseHealth(1);
                Check(rusty.CurrentMove == "Hurt" && rusty.SunglassesUp, "...and when he gets hurt (so you see his X eyes)");
                await Wait(0.3);
            }

            rusty.LoseHealth(Boss.MaxHealth);
            if (rusty.Outfit == BossOutfit.Sunglasses)
                Check(rusty.CurrentMove == "Exploding" && rusty.SunglassesUp, "...and while he breaks down");
            Check(await WaitUntil(() => rusty.HasBlownUp, 3), $"in {world} he blows up");
            bool hat = rusty.Outfit is BossOutfit.PartyHat or BossOutfit.BobbleHat;
            Check(rusty.HatFlewOff == hat && rusty.OutfitOnHead == !hat,
                  hat ? "his hat flies off as its own piece" : "his outfit stays on his head (no hat flies off)");
            if (rusty.Outfit == BossOutfit.Sunglasses) Check(rusty.SunglassesUp, "the sunglasses stay up, so you see his X eyes");
            await Wait(0.5); // (his pieces fly around, outfit and all)
            Check(SelfTestLogger.Errors == errorsBefore, $"Big Rusty in {world} draws without errors");
            GoToTitle();
        }
    }

    /// <summary>MEGA BOLT-E's faster music is based on the world's own music speed, and goes back to it when the power ends.</summary>
    async Task PowerMusicInWorlds()
    {
        PlayAs("TPowerMusic");
        StartGame();
        QuietRoad();
        ChangeWorld(2); // Night City: the music is a little slower
        await Wait(1);
        Check(Mathf.Abs(sounds.MusicPitch - 0.95f) < 0.01f, $"Night City's music (pitch {sounds.MusicPitch:0.000})");
        StartPowerUp(PowerUp.Mega);
        await Wait(1);
        Check(Mathf.Abs(sounds.MusicPitch - 1.12f * 0.95f) < 0.01f, $"MEGA BOLT-E speeds up Night City's music (pitch {sounds.MusicPitch:0.000})");
        powerLeft = 0.05f;
        Check(await WaitUntil(() => power is null, 1), "the power ends");
        await Wait(1);
        Check(Mathf.Abs(sounds.MusicPitch - 0.95f) < 0.01f, $"then it's back to Night City's speed, not the usual speed (pitch {sounds.MusicPitch:0.000})");
    }

    /// <summary>The snow keeps falling (down, and left in the wind) and comes back around at the top, so no snowflake gets lost.</summary>
    async Task SnowFalls()
    {
        await NextFrame();
        var before = snowflakes.Select(flake => flake.At).ToArray();
        await Wait(0.5);
        int moved = snowflakes.Where((flake, i) => (flake.At.Y > before[i].Y || flake.At.Y < before[i].Y - 300) &&
                                                   (flake.At.X < before[i].X || flake.At.X > before[i].X + 300)).Count();
        Check(moved == snowflakes.Length, $"every snowflake falls down and drifts left ({moved} of {snowflakes.Length})");

        await Wait(6);
        int wrapped = snowflakes.Where((flake, i) => flake.At.Y < before[i].Y).Count();
        bool allOnScreen = snowflakes.All(flake => flake.At.X >= -15 && flake.At.X <= ScreenWidth + 15 && flake.At.Y >= -15 && flake.At.Y <= ScreenHeight + 15);
        Check(wrapped > 0 && allOnScreen, $"they come back around at the top, and none get lost ({wrapped} came around)");
    }

    // ---------- The Bolt Bank and Bolt-E's looks ----------

    /// <summary>
    /// The 15 looks, in order, with their prices, hats, boards and trails. Every look keeps Bolt-E's face easy to read
    /// (bright eyes on a dark screen), the classic look is the colors he always had, light looks get a dark outline,
    /// and every name fits in the look picker.
    /// </summary>
    async Task LooksReadable()
    {
        await NextFrame();
        string List(Func<Look, object> pick) => string.Join(" ", Looks.All.Select(look => pick(look).ToString()));
        Check(List(l => l.Id) == "classic party bubblegum firetruck cowboy ninja ocean hotdog pirate galaxy gold king rainbow rustyjr astronaut",
              $"the 15 looks, in order ({List(l => l.Id)})");
        Check(List(l => $"{l.By}{l.Need}") == "Bolts0 Bolts100 Bolts250 Bolts450 Bolts700 Bolts1000 Bolts1400 Bolts1900 Bolts2500 " +
                                               "Bolts3200 Bolts4000 Bolts5500 Bolts7500 BossesBeaten3 World5",
              $"what each look takes to win ({List(l => $"{l.By}{l.Need}")})");
        Check(List(l => l.Hat) == "None Party None None Cowboy None None Propeller Pirate None None Crown None None SpaceHelmet",
              $"the hats ({List(l => l.Hat)})");
        Check(List(l => l.Board) == "Classic Classic Classic Classic Skateboard Classic Classic HotDog Classic Classic Classic Rainbow Rainbow Classic Cloud",
              $"the boards ({List(l => l.Board)})");
        Check(List(l => l.Trail) == "None None Hearts None None None Sparkles None None Sparkles None Rainbow Rainbow None None",
              $"the trails ({List(l => l.Trail)})");
        Check(List(l => l.RainbowEyes ? l.Id : "-") == "- - - - - - - - - - - - rainbow - -", "only RAINBOW has rainbow eyes");
        Look gold = Looks.All[10], king = Looks.All[11];
        Check(king.Body == gold.Body && king.Bezel == gold.Bezel && king.Screen == gold.Screen && king.Eyes == gold.Eyes &&
              king.Headphones == gold.Headphones && king.Accent == gold.Accent && king.BoardColor == gold.BoardColor,
              "KING BOLT-E wears GOLD BOLT-E's colors");

        foreach (var look in Looks.All)
        {
            float eyes = Worlds.Brightness(look.Eyes), screen = Worlds.Brightness(look.Screen);
            Check(eyes >= 0.6f && screen <= 0.1f, $"{look.Name}: bright eyes ({eyes:0.00}) on a dark screen ({screen:0.00})");
        }

        var classic = Looks.Classic;
        Check(classic.Body == new Color(0.23f, 0.24f, 0.3f) && classic.Bezel == new Color(0.66f, 0.68f, 0.75f) &&
              classic.Screen == new Color(0.04f, 0.05f, 0.07f) && classic.Eyes == new Color(0.35f, 0.95f, 0.95f) &&
              classic.Headphones == new Color(0.4f, 0.35f, 0.7f) && classic.Accent == new Color(0.3f, 0.92f, 0.85f) &&
              classic.BoardColor == new Color(0.2f, 0.21f, 0.26f) && Looks.All[0] == classic,
              "the classic look (the first one) is the colors Bolt-E always had");

        // Try every look on Bolt-E: the light ones get the dark outline
        var outlined = new List<string>();
        foreach (var look in Looks.All)
        {
            Looks.Apply(robot, look);
            if (robot.Rim) outlined.Add(look.Id);
        }
        Check(string.Join(" ", outlined) == "bubblegum gold king rustyjr astronaut", $"the light looks get a dark outline ({string.Join(" ", outlined)})");
        Looks.Apply(robot, classic);
        Check(!robot.Rim && Mathf.Abs(Worlds.Brightness(robot.BodyColor) - 0.24f) < 0.01f, "the classic look has no outline (his body's brightness is 0.24)");

        var font = ThemeDB.FallbackFont;
        float NameWidth(Look look) => font.GetStringSize(look.Name, HorizontalAlignment.Left, -1, 26).X;
        var widest = Looks.All.MaxBy(NameWidth)!;
        Check(NameWidth(widest) <= 176, $"every name fits in the look picker (the widest, {widest.Name}, is {NameWidth(widest):0} px of 176)");
    }

    /// <summary>
    /// The Bolt Bank adds up the bolts of every game ever (with the name in any case), and bolts win looks:
    /// 480 bolts win CLASSIC, PARTY BOLT-E, BUBBLEGUM and FIRE TRUCK. The menu shows the bank and the next prize,
    /// and because it's the first time this name is loaded, the 3 looks its old games won are announced together
    /// (they count as seen once the note has been showing long enough to read it).
    /// </summary>
    async Task BankUnlocks()
    {
        await NextFrame();
        string name = FreshName("TBANK"), key = PlayerKey(name);
        SeedRun(name, bolts: 200);
        SeedRun(name.ToLowerInvariant(), bolts: 280); // (the same player, with the name typed in small letters)
        int jingles = sounds.TimesPlayed(Sfx.Unlock);

        hud.SetPlayerName(name);
        Check(Bank == 480 && playerStats?.GamesPlayed == 2, $"the Bolt Bank adds up both games, even with the name typed differently ({Bank} bolts)");
        Check(UnlockedIds() == "classic party bubblegum firetruck", $"480 bolts win: {UnlockedIds()}");
        var bank = hud.BankBox;
        Check(bank.Bank == 480 && bank.Plus == 0 && bank.Next == "Next: COWBOY in 220" && Mathf.Abs(bank.Fraction - 0.12f) < 0.001f,
              $"the menu shows {bank.Bank} bolts and '{bank.Next}' (the bar is {bank.Fraction:0.00} full: 30 of the 250 from FIRE TRUCK to COWBOY)");
        Check(hud.UnlockShowing && hud.UnlockBigText == "3 NEW LOOKS!" && hud.UnlockSmallText == "Your old games count!" &&
              sounds.TimesPlayed(Sfx.Unlock) == jingles + 1,
              $"the first time, the looks the old games won are announced together: '{hud.UnlockBigText}' '{hud.UnlockSmallText}'");
        Check(database?.GetSetting("looks_seen:" + key) is null, "(they don't count as seen until the note has been showing for a moment)");

        await Wait(2.7);
        Check(database?.GetSetting("looks_seen:" + key) == "classic,party,bubblegum,firetruck",
              $"...then they're remembered as seen ({database?.GetSetting("looks_seen:" + key)})");
        Check(sounds.TimesPlayed(Sfx.Unlock) == jingles + 1 && lookIndex == 0 && database?.GetSetting("look:" + key) is null,
              "so no separate parties come for them later, and his look stays the same");

        // The next prize, all along the way
        string NextAt(int bolts)
        {
            ShowBankNumber(bolts, counting: false);
            return $"{hud.BankBox.Next} ({hud.BankBox.Fraction:0.00})";
        }
        Check(NextAt(0) == "Next: PARTY BOLT-E in 100 (0.00)", $"0 bolts: {NextAt(0)}");
        Check(NextAt(700) == "Next: NINJA in 300 (0.00)", $"700 bolts: {NextAt(700)}");
        Check(NextAt(7000) == "Next: RAINBOW in 500 (0.75)", $"7000 bolts: {NextAt(7000)}");
        Check(NextAt(7500) == "You got them ALL! (1.00)", $"7500 bolts: {NextAt(7500)}");
    }

    /// <summary>
    /// On the title screen Bolt-E stands in the corner on the left. RIGHT (the arrow key, the D-pad or the stick)
    /// tries on the next look: one he hasn't won is a dark shadow, the picker shows what it takes to win it, and it isn't
    /// saved. Starting a game puts his saved look back on, in full color, at his usual riding spot. (The A and D keys
    /// do nothing on the menus: the buttons say "(A)" for the controller's A button.)
    /// </summary>
    async Task BrowseLocked()
    {
        string name = FreshName("TBROWSE"), key = PlayerKey(name);
        hud.SetPlayerName(name);
        await Wait(0.3);
        Check(state == GameState.Title && robot.Position == new Vector2(MenuHomeX, GroundY),
              $"on the title screen Bolt-E stands in the corner on the left (feet at {robot.Position})");
        var picker = hud.LookPicker;
        Check(lookIndex == 0 && robot.SelfModulate == Colors.White && picker.LookName == "CLASSIC" && !picker.Locked &&
              picker.Number == 1 && picker.Count == 15, $"a new player wears the classic look ({picker.LookName} {picker.Number}/{picker.Count})");

        int ticks = sounds.TimesPlayed(Sfx.Tick);
        await TapKey(Key.A);
        await TapKey(Key.D);
        Check(lookIndex == 0 && state == GameState.Title && sounds.TimesPlayed(Sfx.Tick) == ticks,
              "the A and D keys don't change his look on the menus (and don't start a game)");
        await TapKey(Key.Right);
        await TapKey(Key.Right);
        await PressButton(JoyButton.DpadRight);
        await StickX(0.9f);
        await StickX(0);
        await TapKey(Key.Right);
        var ninja = Looks.All[lookIndex];
        Check(ninja.Id == "ninja", $"RIGHT, RIGHT, the D-pad, the stick, RIGHT: 5 steps to NINJA ({ninja.Id})");
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks + 5 && Mathf.Abs(sounds.PitchOf(Sfx.Tick) - 1.3f) < 0.001f, "a tick for every step");
        Check(robot.BodyColor == ninja.Body && robot.EyeColor == ninja.Eyes, "he tries the look on");
        Check(robot.SelfModulate == LockedShadow, $"a look he hasn't won is a dark shadow ({robot.SelfModulate})");
        Check(picker.LookName == "NINJA" && picker.Locked && picker.Goal == "Need 1000 more bolts!" && picker.Number == 6,
              $"the picker: {picker.LookName} {picker.Number}/{picker.Count}, locked: '{picker.Goal}'");
        Check(database?.GetSetting("look:" + key) is null, "a look he hasn't won isn't saved");

        // All the way round to the left: ASTRONAUT comes just before CLASSIC
        for (int i = 0; i < 6; i++) await TapKey(Key.Left);
        Check(picker.LookName == "ASTRONAUT" && picker.Locked && picker.Goal == "Ride to THE MOON!" && picker.Number == 15,
              $"left of CLASSIC comes the last look: {picker.LookName} {picker.Number}/15 ('{picker.Goal}')");
        await TapKey(Key.Right);
        Check(lookIndex == 0 && robot.SelfModulate == Colors.White && !picker.Locked && database?.GetSetting("look:" + key) == "classic",
              "back to CLASSIC: full color, and it's saved as his look");

        // Trying on a locked look, then starting a game: the saved look goes back on
        for (int i = 0; i < 5; i++) await TapKey(Key.Right);
        Check(Looks.All[lookIndex].Id == "ninja" && robot.SelfModulate == LockedShadow, "trying on NINJA again");
        StartGame();
        Check(lookIndex == 0 && robot.BodyColor == Looks.Classic.Body && robot.SelfModulate == Colors.White,
              "starting a game puts his saved look back on, in full color");
        Check(robot.Position == new Vector2(RobotX, GroundY), $"...and he rides from his usual spot (x {robot.Position.X:0})");
    }

    /// <summary>
    /// After a game the Bolt Bank counts up (tick, tick, tick, higher and higher). Passing 700 bolts wins COWBOY:
    /// NEW LOOK!, a jingle, and Bolt-E, still in pieces, zips back together wearing it, right on the results screen
    /// (it's not a magic rebuild: the game stays over). The look is saved, and only now counts as seen.
    /// </summary>
    async Task CountUpUnlocksCowboy()
    {
        string name = FreshName("TCOW"), key = PlayerKey(name);
        SeedRun(name, bolts: 680);
        database?.SetSetting("looks_seen:" + key, "classic,party,bubblegum,firetruck");
        hud.SetPlayerName(name);
        int jingles = sounds.TimesPlayed(Sfx.Unlock);
        StartGame();
        QuietRoad();
        await Wait(0.2);
        boltsCollected = 30;
        spareBatteries = 0;
        bestScore = 1_000_000; // (so this isn't a new high score)
        Check(Bonk(null) && state == GameState.GameOver, "game over with 30 bolts");
        Check(await WaitUntil(() => hud.ResultsShowing, 3), "the results card shows");
        // (It already took its first little step in the same moment the card showed)
        Check(bankFrom == 680 && bankTarget == 710 && hud.BankBox.Bank <= 682 && hud.BankBox.Plus == 30 && bankCounting && robot.Crashed,
              $"the Bolt Bank starts counting from the old 680 ({hud.BankBox.Bank} on the screen) with +{hud.BankBox.Plus}, while Bolt-E is still in pieces");
        int ticks = sounds.TimesPlayed(Sfx.Tick);
        await Wait(0.2);
        Check(bankShown > 680 && bankShown < 710, $"it counts up bit by bit ({bankShown:0} after 0.2 seconds)");
        Check(database?.GetSetting("looks_seen:" + key)?.Contains("cowboy") == false && !hud.UnlockShowing, "COWBOY hasn't had its party yet");

        Check(await WaitUntil(() => hud.UnlockShowing, 2), "an unlock party starts");
        Check(hud.UnlockBigText == "NEW LOOK!" && hud.UnlockSmallText == "COWBOY" && sounds.TimesPlayed(Sfx.Unlock) == jingles + 1,
              $"'{hud.UnlockBigText}' '{hud.UnlockSmallText}' with a jingle");
        Check(bankShown >= 700 && bankShown < 706, $"...just as the bank passes 700 ({bankShown:0})");

        await Wait(3.5);
        Check(bankShown == 710 && !bankCounting && hud.BankBox.Bank == 710 && hud.BankBox.Plus == 0,
              $"the bank counted up to {hud.BankBox.Bank} and stopped, and the +30 is gone");
        int ticked = sounds.TimesPlayed(Sfx.Tick) - ticks;
        Check(ticked >= 6 && sounds.PitchOf(Sfx.Tick) > 1.35f,
              $"tick, tick, tick, higher and higher ({ticked} more ticks, the last one at pitch {sounds.PitchOf(Sfx.Tick):0.00})");
        Check(database?.GetSetting("look:" + key) == "cowboy", "COWBOY is saved as his look");
        Check(database?.GetSetting("looks_seen:" + key) == "classic,party,bubblegum,firetruck,cowboy",
              $"...and now it counts as seen ({database?.GetSetting("looks_seen:" + key)})");
        Check(!robot.Crashed && robot.Hat == Hat.Cowboy && robot.BoardStyle == Board.Skateboard && lookIndex == 4,
              "Bolt-E zipped back together in a cowboy hat, on a skateboard");
        Check(state == GameState.GameOver && hud.ResultsShowing && robot.BlinkTime == 0 && Engine.TimeScale == 1,
              "the game stays over (it wasn't a magic rebuild: no blinking, no 'BOLT-E IS BACK!')");
        Check(Mathf.Abs(robot.Position.X - MenuHomeX) < 2 && robot.Position.Y == GroundY,
              $"he rides over to his corner on the left (x {robot.Position.X:0})");
    }

    /// <summary>
    /// Starting the next game before the party happens: the count-up stops and COWBOY doesn't count as seen.
    /// Its party isn't lost: it happens on the next menu instead.
    /// </summary>
    async Task QuickRestartKeepsParty()
    {
        string name = FreshName("TQUICK"), key = PlayerKey(name);
        const string seenBefore = "classic,party,bubblegum,firetruck";
        SeedRun(name, bolts: 670);
        database?.SetSetting("looks_seen:" + key, seenBefore);
        hud.SetPlayerName(name);
        StartGame();
        QuietRoad();
        await Wait(0.2);
        boltsCollected = 30; // 670 + 30 = 700: just enough for COWBOY
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over: the bank will reach 700 bolts");
        Check(await WaitUntil(() => hud.ResultsShowing, 3), "the results card shows");
        StartGame(); // (START pressed right away)
        Check(state == GameState.Playing && !bankCounting && unlockQueue.Count == 0 && !hud.UnlockShowing,
              "the next game starts right away, and the count-up stops");
        Check(database?.GetSetting("looks_seen:" + key) == seenBefore && database?.GetSetting("look:" + key) is null,
              "COWBOY didn't have its party, so it doesn't count as seen");
        await Wait(2);
        Check(database?.GetSetting("looks_seen:" + key) == seenBefore && !hud.UnlockShowing, "no party during the game");

        GoToTitle();
        Check(!hud.UnlockShowing && unlockQueue.Count == 1, "on the title screen COWBOY waits in line...");
        Check(await WaitUntil(() => hud.UnlockShowing, 2) && hud.UnlockSmallText == "COWBOY", $"...and has its party after all ('{hud.UnlockSmallText}')");
        Check(database?.GetSetting("looks_seen:" + key) == seenBefore + ",cowboy" && database?.GetSetting("look:" + key) == "cowboy" &&
              robot.Hat == Hat.Cowboy, "now COWBOY counts as seen, and Bolt-E wears it");

        // Two prizes in one game: FIRE TRUCK has its party, and COWBOY waits in line behind it... when the next game starts.
        // COWBOY was in line but never had its party, so it still isn't seen, and its party comes on the next menu.
        string second = FreshName("TQUICKB"), secondKey = PlayerKey(second);
        SeedRun(second, bolts: 440);
        database?.SetSetting("looks_seen:" + secondKey, "classic,party,bubblegum");
        hud.SetPlayerName(second);
        StartGame();
        QuietRoad();
        await Wait(0.2);
        boltsCollected = 260; // 440 + 260 = 700: passes FIRE TRUCK (450) and COWBOY (700)
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over: the bank will pass 450 and 700 bolts");
        Check(await WaitUntil(() => unlockQueue.Any(look => look.Id == "cowboy"), 5) && hud.UnlockSmallText == "FIRE TRUCK",
              $"FIRE TRUCK has its party, and COWBOY waits in line ({string.Join(", ", unlockQueue.Select(look => look.Id))})");
        StartGame(); // (START pressed just then)
        Check(database?.GetSetting("looks_seen:" + secondKey) == "classic,party,bubblegum,firetruck",
              $"FIRE TRUCK counts as seen, but COWBOY (still in line) doesn't ({database?.GetSetting("looks_seen:" + secondKey)})");
        GoToTitle();
        Check(await WaitUntil(() => hud.UnlockShowing && hud.UnlockSmallText == "COWBOY", 2.5), "on the next menu COWBOY has its party");
        Check(database?.GetSetting("looks_seen:" + secondKey) == "classic,party,bubblegum,firetruck,cowboy", "and now it counts as seen");
    }

    /// <summary>
    /// Typing another name while the bank counts up stops the count-up and empties the party line, so one player's new
    /// look is never saved for another player. The first player's waiting looks still count as unseen.
    /// </summary>
    async Task SwitchCancelsCountUp()
    {
        string name = FreshName("TSWITCH"), key = PlayerKey(name);
        string other = FreshName("TOTHER"), otherKey = PlayerKey(other);
        hud.SetPlayerName(name);
        StartGame();
        QuietRoad();
        await Wait(0.2);
        boltsCollected = 1000; // a giant game: the bank will pass 5 prizes
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over with 1000 bolts");
        Check(await WaitUntil(() => bankCounting && unlockQueue.Count > 0, 4),
              $"the bank counts up, and won looks wait in line for their parties ({string.Join(", ", unlockQueue.Select(look => look.Id))})");
        int jingles = sounds.TimesPlayed(Sfx.Unlock);
        string? seenBefore = database?.GetSetting("looks_seen:" + key);

        hud.SetPlayerName(other);
        Check(!bankCounting && unlockQueue.Count == 0, "typing another name stops the count-up and empties the line");
        Check(database?.GetSetting("look:" + otherKey) is null, "the other player's look is unchanged");
        Check(hud.BankBox.Bank == 0 && hud.LookPicker.LookName == "CLASSIC", "the menu shows the other player's bank and look");

        await Wait(4);
        Check(sounds.TimesPlayed(Sfx.Unlock) == jingles, "no more parties");
        Check(database?.GetSetting("look:" + otherKey) is null && database?.GetSetting("looks_seen:" + otherKey) is null,
              "nothing was saved for the other player");
        Check(database?.GetSetting("looks_seen:" + key) == seenBefore && seenBefore?.Contains("ninja") == false,
              $"{name}'s waiting looks still haven't had their party ({seenBefore}), so it comes later");
    }

    /// <summary>
    /// RUSTY JR. is won by beating Big Rusty 3 times (all games together), and ASTRONAUT by riding to THE MOON.
    /// Until then the picker says what it takes. They get their party on the results card, right after the bank counts.
    /// </summary>
    async Task SpecialUnlocks()
    {
        string name = FreshName("TSPECIAL"), key = PlayerKey(name);
        SeedRun(name, bolts: 0, bosses: 1);
        SeedRun(name, bolts: 0, bosses: 1);
        hud.SetPlayerName(name);
        Look rustyJr = Looks.All[13], astronaut = Looks.All[14];
        Check(!Unlocked(rustyJr) && Goal(rustyJr) == "Beat BIG RUSTY 3 times (2/3)", $"after 2 Big Rusty wins: '{Goal(rustyJr)}'");
        Check(!Unlocked(astronaut) && Goal(astronaut) == "Ride to THE MOON!", $"ASTRONAUT: '{Goal(astronaut)}'");
        BrowseLook(-1);
        BrowseLook(-1);
        var picker = hud.LookPicker;
        Check(picker.LookName == "RUSTY JR." && picker.Locked && picker.Goal == "Beat BIG RUSTY 3 times (2/3)",
              $"the picker shows {picker.LookName}: '{picker.Goal}'");

        // The third Big Rusty win, in a real game
        StartGame();
        QuietRoad();
        await Wait(0.2);
        bossesBeaten = 1;
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over after beating Big Rusty once more");
        Check(await WaitUntil(() => hud.UnlockShowing, 4) && hud.UnlockSmallText == "RUSTY JR.", $"NEW LOOK! '{hud.UnlockSmallText}' on the results card");
        Check(database?.GetSetting("look:" + key) == "rustyjr" && database?.GetSetting("looks_seen:" + key)?.Contains("rustyjr") == true &&
              Unlocked(rustyJr) && Goal(rustyJr) == "", "RUSTY JR. is won, seen and saved");
        Check(await WaitUntil(() => !robot.Crashed, 2) && robot.BodyColor == rustyJr.Body, "Bolt-E zips back together as RUSTY JR.");

        // All the way to the Moon
        StartGame();
        QuietRoad();
        ChangeWorld(4);
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over on THE MOON");
        Check(await WaitUntil(() => hud.UnlockShowing && hud.UnlockSmallText == "ASTRONAUT", 4), $"NEW LOOK! '{hud.UnlockSmallText}'");
        Check(database?.GetSetting("look:" + key) == "astronaut" && Unlocked(astronaut), "ASTRONAUT is won and saved");
        Check(await WaitUntil(() => !robot.Crashed, 2) && robot.Hat == Hat.SpaceHelmet && robot.BoardStyle == Board.Cloud,
              "Bolt-E zips back together in a space helmet, on a cloud");
    }

    /// <summary>
    /// The first time the game opens for a player who already has old games, the looks those games won are announced
    /// together ("2 NEW LOOKS!  Your old games count!") with a jingle, and once the note has been showing for 2.5 seconds
    /// they're remembered as seen. Opening the title screen again announces nothing. (A name that never played, like a
    /// name half typed, saves nothing.)
    /// </summary>
    async Task FirstLaunchNote()
    {
        string name = FreshName("TFIRST"), key = PlayerKey(name);
        SeedRun(name, bolts: 300);
        int jingles = sounds.TimesPlayed(Sfx.Unlock);
        database?.SetSetting("player_name", name);
        GoToTitle();
        ulong shownAt = Engine.GetProcessFrames();
        Check(hud.PlayerName == name && Bank == 300, $"the game opens for {hud.PlayerName}, who has {Bank} bolts from old games");
        Check(hud.UnlockShowing && hud.UnlockBigText == "2 NEW LOOKS!" && hud.UnlockSmallText == "Your old games count!" &&
              sounds.TimesPlayed(Sfx.Unlock) == jingles + 1, $"'{hud.UnlockBigText}' '{hud.UnlockSmallText}' with a jingle");
        Check(database?.GetSetting("looks_seen:" + key) is null, "the looks don't count as seen yet: first the note has to be read");
        bool saved = await WaitUntil(() => database?.GetSetting("looks_seen:" + key) is not null, 3.5);
        double after = (Engine.GetProcessFrames() - shownAt) / 60.0;
        Check(saved && after > 2.3 && database?.GetSetting("looks_seen:" + key) == "classic,party,bubblegum",
              $"after the note has been showing for {after:0.0} seconds, the looks are remembered as seen ({database?.GetSetting("looks_seen:" + key)})");
        Check(sounds.TimesPlayed(Sfx.Unlock) == jingles + 1 && unlockQueue.Count == 0 && lookIndex == 0 && database?.GetSetting("look:" + key) is null,
              "no separate parties come for them, and his look stays the same");
        Check(await WaitUntil(() => !hud.UnlockShowing, 2), "the words fade away");

        GoToTitle();
        await Wait(1.5);
        Check(!hud.UnlockShowing && sounds.TimesPlayed(Sfx.Unlock) == jingles + 1, "the next time the title screen opens, there's nothing to announce");

        string halfTyped = FreshName(name[..^1]);
        hud.SetPlayerName(halfTyped);
        Check(database?.GetSetting("looks_seen:" + PlayerKey(halfTyped)) is null, $"a name that never played ('{halfTyped}', like a name half typed) saves nothing");
    }

    /// <summary>
    /// One push of the stick is one step, however far it goes (and a little nudge is nothing). During a game left and
    /// right don't change his look, and after a crash they only do once the results card shows.
    /// </summary>
    async Task OneStickPushOneStep()
    {
        hud.SetPlayerName(FreshName("TSTICK"));
        await Wait(0.2);
        int start = lookIndex;
        int Moved() => Mathf.PosMod(lookIndex - start + 7, Looks.All.Length) - 7; // (-1 = one step to the left)
        await StickX(-0.7f);
        await StickX(-0.9f);
        await StickX(-0.95f);
        Check(Moved() == -1, $"pushing the stick to the left (0.7, then 0.9, then 0.95) is one step ({Moved()})");
        await StickX(0);
        await StickX(-0.8f);
        Check(Moved() == -2, $"let go and push again: one more step ({Moved()})");
        await StickX(0);
        await StickX(-0.5f);
        await StickX(0);
        Check(Moved() == -2, $"a little nudge (0.5) does nothing ({Moved()})");
        await StickX(0.8f);
        await StickX(1);
        await StickX(0);
        Check(Moved() == -1, $"pushing it to the right goes back one step ({Moved()})");

        // During a game, left and right don't change his look
        StartGame();
        QuietRoad();
        int during = lookIndex;
        await TapKey(Key.Left);
        await PressButton(JoyButton.DpadRight);
        await StickX(-0.9f);
        await StickX(0);
        Check(lookIndex == during && state == GameState.Playing, "during a game, left and right don't change his look");

        // After a crash: not while he blows up, only once the results card shows
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over");
        await TapKey(Key.Right);
        Check(lookIndex == during, "not while he's blowing up (before the results card)");
        Check(await WaitUntil(() => hud.ResultsShowing, 3), "the results card shows");
        await TapKey(Key.Right);
        Check(lookIndex == Mathf.PosMod(during + 1, Looks.All.Length), "once the results card shows, RIGHT changes his look");
    }

    /// <summary>
    /// On the menus, D-pad up never starts a game (it's easy to press by accident while pressing left or right on the
    /// D-pad). The A button and START still do, and in a game D-pad up still jumps.
    /// </summary>
    async Task DpadUpStaysOnMenu()
    {
        hud.SetPlayerName(FreshName("TDPAD"));
        await Wait(0.2);
        await PressButton(JoyButton.DpadUp);
        await Wait(0.2);
        Check(state == GameState.Title, "D-pad up doesn't start a game from the title screen");
        await PressButton(JoyButton.A);
        Check(state == GameState.Playing, "the A button still does");
        QuietRoad();
        await Wait(0.3);
        int jumps = sounds.TimesPlayed(Sfx.Jump);
        await PressButton(JoyButton.DpadUp);
        Check(sounds.TimesPlayed(Sfx.Jump) == jumps + 1, "in a game, D-pad up still jumps");
        await Wait(1);

        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over");
        Check(await WaitUntil(() => hud.ResultsShowing, 3), "the results card shows");
        await Wait(0.5); // (jump and START work again a moment after the results card shows)
        await PressButton(JoyButton.DpadUp);
        await Wait(0.2);
        Check(state == GameState.GameOver, "D-pad up doesn't start a game from the results card either");
        await PressButton(JoyButton.Start);
        Check(state == GameState.Playing, "START still plays again");
    }

    /// <summary>
    /// Every look draws without errors: its hat, its board, its trail (which drifts away behind him) and, for light
    /// looks, the dark outline. When he crashes, a hat flies off as its own piece (the space helmet too), and when he's
    /// put back together, the hat is back on his head. Rainbow eyes change color.
    /// </summary>
    async Task LooksDraw()
    {
        await NextFrame();
        foreach (var look in Looks.All)
        {
            int errorsBefore = SelfTestLogger.Errors;
            Looks.Apply(robot, look);
            robot.SelfModulate = Colors.White;
            await Wait(0.15);
            string drew = $"hat {robot.HatDrawn?.ToString() ?? "-"}, board {robot.BoardDrawn}, " +
                          $"trail {robot.TrailDrawn?.ToString() ?? "-"} ({robot.TrailBits} bits), outline {robot.RimDrawn}";
            bool hatOk = look.Hat == Hat.None ? robot.HatDrawn is null : robot.HatDrawn == look.Hat;
            bool trailOk = look.Trail == Trail.None ? robot.TrailDrawn is null && robot.TrailBits == 0 : robot.TrailDrawn == look.Trail && robot.TrailBits > 0;
            Check(hatOk && robot.BoardDrawn == look.Board && trailOk && robot.RimDrawn == robot.Rim, $"{look.Name} draws: {drew}");
            if (robot.OldestTrailBit is Vector2 bit)
                Check(bit.X < robot.Position.X - 46 - 5 && Mathf.Abs(bit.Y - (robot.Position.Y - 14)) < 0.5f,
                      $"{look.Name}: the trail comes out of the back of the board and drifts away ({bit.X - robot.Position.X:0} px)");

            robot.Crash();
            Check(robot.HasPiece("hat") == (look.Hat != Hat.None),
                  look.Hat == Hat.None ? $"{look.Name}: no hat to fly off" : $"{look.Name}: the {look.Hat} hat flies off as its own piece");
            await Wait(0.3);
            if (look.Hat != Hat.None) Check(robot.HatDrawn == look.Hat, $"{look.Name}: the flying hat draws");
            robot.Reset();
            await NextFrame();
            Check(SelfTestLogger.Errors == errorsBefore, $"{look.Name} draws without errors");
        }

        // Rainbow eyes change color (and other eyes don't)
        Looks.Apply(robot, Looks.All[12]);
        var eyesBefore = robot.EyeColor;
        await Wait(0.5);
        Check(robot.EyeColor != eyesBefore && robot.EyeColor != Looks.All[12].Eyes,
              $"RAINBOW's eyes change color ({eyesBefore.ToHtml(false)}, then {robot.EyeColor.ToHtml(false)})");
        Looks.Apply(robot, Looks.Classic);
        await Wait(0.2);
        Check(robot.EyeColor == Looks.Classic.Eyes, "other looks keep their eye color");

        // The hat goes back on when he's put back together
        Looks.Apply(robot, Looks.All[1]); // PARTY BOLT-E
        robot.Crash();
        await Wait(0.5);
        robot.Rebuild(LookRebuildSeconds);
        Check(await WaitUntil(() => !robot.Crashed, 2), "Bolt-E is put back together");
        await NextFrame();
        await NextFrame();
        Check(robot.HatDrawn == Hat.Party && !robot.HasPiece("hat"), "his party hat is back on his head");
    }

    /// <summary>
    /// The strip on the left of the menus, with the biggest numbers and the longest words: everything stays in its own
    /// spot and ends at x 322 or less, so it never touches the cards. Clicks go right through all of it, except the
    /// look picker.
    /// </summary>
    async Task StripFits()
    {
        hud.ShowUnlock("14 NEW LOOKS!", "Your old games count!");
        hud.ShowLook("PARTY BOLT-E", true, "Beat BIG RUSTY 3 times (0/3)", 15, 15);
        hud.ShowBank(99999, 999, "Next: PARTY BOLT-E in 100", 1);
        await Wait(0.5); // (the unlock words have popped in)
        Check(hud.MenuStripShowing && hud.UnlockShowing, "the strip shows on the title screen, with the unlock words");

        string Rects(IEnumerable<Control> controls) =>
            string.Join(", ", controls.Select(w => $"{w.GetType().Name} x {w.GetGlobalRect().Position.X:0}-{w.GetGlobalRect().End.X:0} y {w.GetGlobalRect().Position.Y:0}-{w.GetGlobalRect().End.Y:0}"));
        var widgets = hud.StripWidgets.Where(w => w.IsVisibleInTree()).ToList();
        var tooWide = widgets.Where(w => w.GetGlobalRect().End.X > 322).ToList();
        Check(widgets.Count >= 6 && tooWide.Count == 0, $"all {widgets.Count} things in the strip end at x 322 or less ({Rects(widgets)})");
        Check(hud.BankBox.DrawnRight <= 306 && hud.LookPicker.DrawnRight <= 306,
              $"the drawn words stay inside their boxes too (the bank to {hud.BankBox.DrawnRight:0}, the picker to {hud.LookPicker.DrawnRight:0}, of 306)");
        Check(hud.BankBox.GetGlobalRect() == new Rect2(16, 16, 306, 104) && hud.LookPicker.GetGlobalRect() == new Rect2(16, 612, 306, 96),
              $"the Bolt Bank is at the top (16,16 to 322,120) and the look picker under Bolt-E (16,612 to 322,708): {Rects(new Control[] { hud.BankBox, hud.LookPicker })}");
        var words = widgets.OfType<Label>().ToList();
        Check(words.Count == 2 && words.All(w => new Rect2(16, 230, 306, 90).Encloses(w.GetGlobalRect())),
              $"the unlock words stay between y 230 and 320 ({Rects(words)})");
        Check(words.Count == 2 && words[0].LabelSettings.FontSize == 24 && words[1].LabelSettings.FontSize == 18,
              "'14 NEW LOOKS!' is long, so it's drawn a bit smaller (24), and the words under it smaller still (18)");
        Check(widgets.All(w => w.MouseFilter == (w == hud.LookPicker ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore)),
              "clicks go right through everything in the strip except the look picker");
        Check(widgets.All(w => w.GetGlobalRect().End.X < hud.TitleCardRect.Position.X), "nothing in the strip touches the title card");

        hud.ShowUnlock("NEW LOOK!", "PARTY BOLT-E");
        Check(words[0].LabelSettings.FontSize == 30 && words[1].LabelSettings.FontSize == 24, "'NEW LOOK!' is drawn big (30), the look's name 24");

        hud.ShowGameOver(new ResultsInfo { Title = "Out of batteries!", Score = 99999, Meters = 9999, Bolts = 999, WorldLine = Worlds.WorldLine(6) });
        await Wait(0.3);
        var onResults = hud.StripWidgets.Where(w => w.IsVisibleInTree()).ToList();
        Check(hud.MenuStripShowing && onResults.Count >= 6 && onResults.All(w => w.GetGlobalRect().End.X < hud.GameOverCardRect.Position.X),
              $"next to the results card it all shows too, and nothing touches the card (it starts at x {hud.GameOverCardRect.Position.X:0})");
        hud.HideUnlock();
        Check(!hud.UnlockShowing, "the unlock words can be hidden right away");
    }

    /// <summary>
    /// Clicking the arrows of the look picker changes the look; clicking anywhere on the picker never starts a game.
    /// A click on the empty sky still starts a game, like always.
    /// </summary>
    async Task ClickTheArrows()
    {
        hud.SetPlayerName(FreshName("TCLICK"));
        await Wait(0.2);
        int start = lookIndex;
        await Click(new Vector2(16 + 14, 612 + 30)); // the left arrow
        Check(lookIndex == Mathf.PosMod(start - 1, Looks.All.Length) && state == GameState.Title,
              $"clicking the left arrow: the look before ({hud.LookPicker.LookName}, {state})");
        await Click(new Vector2(16 + 292, 612 + 30)); // the right arrow
        await Click(new Vector2(16 + 292, 612 + 30));
        Check(lookIndex == Mathf.PosMod(start + 1, Looks.All.Length) && state == GameState.Title,
              $"clicking the right arrow twice: the next look ({hud.LookPicker.LookName})");
        await Click(new Vector2(16 + 153, 612 + 40)); // the look's name, in the middle of the picker
        Check(lookIndex == Mathf.PosMod(start + 1, Looks.All.Length) && state == GameState.Title,
              "clicking the middle of the picker does nothing (and never starts a game)");
        await Click(new Vector2(172, 380)); // the empty sky above Bolt-E
        Check(state == GameState.Playing, "a click on the sky still starts a game, like always");
    }

    /// <summary>
    /// Without a database (for example while DB Browser has it locked): only the classic look, a Bolt Bank of 0,
    /// nothing is saved and no parties, but everything still works. On the menus the warning fits in the strip.
    /// </summary>
    async Task LooksWithoutDatabase()
    {
        var realDatabase = database;
        string oldWarning = hud.WarningText;
        database = null;
        try
        {
            GoToTitle();
            hud.SetPlayerName("TNoDbLooks");
            Check(UnlockedIds() == "classic" && Bank == 0 && hud.BankBox.Bank == 0 && hud.BankBox.Next == "Next: PARTY BOLT-E in 100",
                  $"only the classic look ({UnlockedIds()}), and a bank of {hud.BankBox.Bank}");
            BrowseLook(1);
            Check(robot.SelfModulate == LockedShadow && hud.LookPicker.Locked && hud.LookPicker.Goal == "Need 100 more bolts!", "every other look is locked");

            hud.ShowWarning("Could not open the score database, so scores won't be saved this time.");
            await NextFrame();
            var warning = hud.WarningRect;
            Check(warning.Position == new Vector2(16, 330) && warning.End.X <= 322 && warning.End.Y <= 420,
                  $"on the menus the warning fits in the strip, above Bolt-E's head (x {warning.Position.X:0}-{warning.End.X:0}, y {warning.Position.Y:0}-{warning.End.Y:0})");

            StartGame();
            QuietRoad();
            Check(lookIndex == 0 && robot.SelfModulate == Colors.White, "a game starts in the classic look");
            await NextFrame();
            Check(hud.WarningRect.Position == new Vector2(24, 690), "while playing, the warning is at the bottom, like before");
            boltsCollected = 500;
            spareBatteries = 0;
            bestScore = 1_000_000;
            Check(Bonk(null) && state == GameState.GameOver, "game over with 500 bolts");
            Check(await WaitUntil(() => hud.ResultsShowing, 3), "the results card shows");
            await Wait(2);
            Check(hud.BankBox.Bank == 0 && !hud.UnlockShowing && lookIndex == 0 && !bankCounting, "no bank and no parties without a database");
        }
        finally
        {
            hud.ShowWarning(oldWarning);
            database = realDatabase;
            GoToTitle();
        }
        Check(realDatabase?.GetSetting("look:tnodblooks") is null && realDatabase?.GetSetting("looks_seen:tnodblooks") is null &&
              realDatabase?.StatsFor("TNoDbLooks").GamesPlayed == 0, "nothing was saved");
    }

    /// <summary>
    /// Typing a name (NEW PLAYER) loads that player (their bank and look) letter by letter. While typing, the keyboard
    /// is for the name box (LEFT moves along the name instead of changing the look), but the controller still works.
    /// When typing stops, the name box lets go, the typed name is the player, and the keys work for the game again.
    /// </summary>
    async Task TypingAName()
    {
        // A name with one letter still to type, like "TTYP" then "E" (both never used in this database)
        string halfTyped = "TTYP", name = "TTYPE";
        for (int number = 2; FreshName(halfTyped) != halfTyped || FreshName(name) != name; number++)
        {
            halfTyped = $"TTYP{number}";
            name = halfTyped + "E";
        }
        SeedRun(name, bolts: 120);
        hud.SetPlayerName(halfTyped);
        await Wait(0.2);
        Check(Bank == 0, $"'{hud.PlayerName}' is someone else (with no bolts)");
        var box = hud.NameBox;
        hud.StartTyping();               // NEW PLAYER (like pressing TAB, or clicking the name)...
        box.Text = halfTyped;            // ...and the first letters are typed already
        box.CaretColumn = box.Text.Length;
        await NextFrame();
        Check(hud.IsTypingName, "typing in the name box");

        await TypeLetter(name[^1]);
        Check(hud.PlayerName == name && Bank == 120 && hud.BankBox.Bank == 120, $"typing the last letter loads {hud.PlayerName}: {Bank} bolts in the bank");
        int index = lookIndex;
        await TapKey(Key.Left);
        Check(lookIndex == index && box.Text == name, "while typing, LEFT doesn't change the look");
        await PressButton(JoyButton.DpadRight);
        Check(lookIndex == index + 1, "...but the controller still does");

        // Escape stops the typing (the real keyboard's Escape: the name box only listens to the real keyboard for that)
        await TapRealKey(Key.Escape);
        Check(!hud.IsTypingName && !box.HasFocus() && hud.PlayerName == name && hud.PlayerLabelText == name,
              $"Escape stops the typing, the name box lets go, and {hud.PlayerName} is the player (typing {hud.IsTypingName}, focus {box.HasFocus()})");
        await TapKey(Key.Left);
        Check(lookIndex == index, "and LEFT changes the look again");

        // Enter starts the game, and the name box lets go too
        hud.StartTyping();
        await NextFrame();
        Check(hud.IsTypingName, "typing again");
        await TapRealKey(Key.Enter);
        Check(state == GameState.Playing && !box.HasFocus(), $"Enter starts the game, and the name box lets go ({state}, focus {box.HasFocus()})");
        box.ReleaseFocus();
    }

    // ---------- The family race: who's playing, and the flags on the road ----------

    /// <summary>
    /// "TDAD" and "tdad" are one person: KnownPlayers lists them once, written the way they were typed last (and first,
    /// because they played last), and so do PlayerBests and today's scores. The title card's "Family Champions" has
    /// one row per person, the best score first.
    /// </summary>
    async Task KnownPlayersNoCase()
    {
        await NextFrame();
        database?.SaveRun("TDAD", 5, 10, 0, 0, 0, 5.0);
        database?.SaveRun("tdad", 6, 10, 0, 0, 0, 5.0);
        bool IsDad(string name) => name.Equals("tdad", StringComparison.OrdinalIgnoreCase);

        var known = database?.KnownPlayers(KnownPlayersShown) ?? new();
        Check(known.Count(IsDad) == 1 && known.FirstOrDefault() == "tdad",
              $"KnownPlayers lists TDAD once, as 'tdad' (the newest spelling), first because he played last ({string.Join(", ", known)})");
        Check(database?.KnownPlayers(2).Count == 2, "KnownPlayers gives at most the number of names asked for");
        var bests = database?.PlayerBests(1000) ?? new();
        var dad = bests.Where(best => IsDad(best.Name)).ToList();
        Check(dad.Count == 1 && dad[0] == new PlayerBest("tdad", 6, 10),
              $"PlayerBests has him once too, with his best score and farthest ride ({string.Join(", ", dad)})");
        Check(bests.Zip(bests.Skip(1)).All(pair => pair.First.BestScore >= pair.Second.BestScore), "PlayerBests: the best score first");
        var today = database?.TodaysBests() ?? new();
        Check(today.Count(best => IsDad(best.Name)) == 1 && today.First(best => IsDad(best.Name)).Score == 6,
              "today's scores have him once, with his best today (6)");

        GoToTitle();
        var champions = database?.PlayerBests(ChampionsShown) ?? new();
        var rows = hud.TitleRows;
        bool rowsRight = rows.Count == champions.Count &&
                         rows.Select((row, i) => row == $"{i + 1}. | {Hud.ShortName(champions[i].Name, 16)} | {champions[i].BestScore}").All(same => same);
        Check(hud.TitleHeading == "Family Champions" && champions.Count == ChampionsShown && rowsRight,
              $"the title card: 'Family Champions', one row per person, the best first ({string.Join(", ", rows)})");
    }

    /// <summary>
    /// Games played without a name count as a person called "Player" (in the picker, and in everyone's bests). With
    /// nobody in the database the empty name box shows (not typing yet), and a game is played as "Player"
    /// (with no "GO, PLAYER!").
    /// </summary>
    async Task PlayerIsAPerson()
    {
        await NextFrame();
        database?.SaveRun("Player", 7, 10, 0, 0, 0, 5.0);
        Check((database?.KnownPlayers(KnownPlayersShown) ?? new()).Contains("Player"), "'Player' is in KnownPlayers, like anybody");
        Check((database?.PlayerBests(1000) ?? new()).Any(best => best.Name == "Player"), "'Player' is in PlayerBests too");

        hud.SetPlayers(new List<string>(), "");
        await NextFrame();
        Check(hud.PlayerCount == 0 && hud.NameBox.IsVisibleInTree() && !hud.IsTypingName && hud.PlayerLabelText == "" && hud.PlayerName == "Player",
              "with nobody to pick, the empty name box shows (not typing yet), and the player is 'Player'");
        await TapKey(Key.Tab);
        Check(hud.IsTypingName && hud.PlayerName == "Player", "TAB goes right into the empty name box");
        previousPlayer = "Player";
        StartGame();
        Check(hud.PlayerCount == 1 && hud.PlayerName == "Player" && !hud.NameBox.Visible, "just playing: the game is played as 'Player'");
        Check(!hud.BannerShowing, $"and there's no 'GO, PLAYER!' ({hud.BannerText})");
    }

    /// <summary>
    /// The flags for a game (spec: TFDAD 800 m and TFKID 500 m): YOUR RECORD (500 m), LAST TIME (420 m, more than 30 m
    /// from the record), and flags for the 4 best other people, never for the player and never for a ride under 50 m,
    /// nearest first. Flags close together lift their words: 30, then 60. Each person always gets the same color.
    /// </summary>
    async Task FlagsPlanted()
    {
        await NextFrame();
        string dad = FreshName("TFDAD"), kid = FreshName("TFKID"), mom = FreshName("TFMOM"), aunt = FreshName("TFAUNT");
        string gran = FreshName("TFGRAN"), uncle = FreshName("TFUNCLE"), tiny = FreshName("TFTINY"), near = FreshName("TFNEAR");
        int top = (database?.BestScore() ?? 0) + 1; // (these are the best scores of all, so these are the 4 best other people)
        void Ride(string name, int score, int meters) => database?.SaveRun(name, score, meters, 0, 0, 0, 30.0);
        Ride(kid, top + 7, 500);   // the kid's record (and the best score of all: still never one of the "other" flags)
        Ride(kid, 1, 420);         // ...and the kid's last ride
        Ride(tiny, top + 6, 49);   // too short for a flag
        Ride(dad, top + 5, 800);
        Ride(mom, top + 4, 820);   // close to DAD's flag: its words go up 30
        Ride(aunt, top + 3, 840);  // close to MOM's flag too: its words go up 60
        Ride(gran, top + 2, 525);  // close to YOUR RECORD: its words go up 30
        Ride(uncle, top + 1, 310); // the 5th best other person: no flag (4 is the most)

        hud.SetPlayerName(kid);
        StartGame();
        QuietRoad();
        Check(flagsToPlant.Count == 0 && flags.Count == 0, "(a quiet road has no flags)");
        BuildFlags();
        string got = string.Join(", ", flagsToPlant.Select(plan => $"{plan.Label} {plan.Meters} {plan.LabelLift}"));
        string expected = $"LAST TIME 420 0, YOUR RECORD 500 0, {FlagLabel(gran)} 525 30, {FlagLabel(dad)} 800 0, " +
                          $"{FlagLabel(mom)} 820 30, {FlagLabel(aunt)} 840 60";
        Check(got == expected, $"the flags, nearest first, with words lifted when they're close ({got})");
        var dadFlag = flagsToPlant.FirstOrDefault(plan => plan.Label == FlagLabel(dad));
        Color DadColor = FlagColors[dad.ToLowerInvariant().Sum(letter => (int)letter) % 5];
        Check(dadFlag is { Kind: FlagKind.Other, Meters: 800 } && dadFlag.Color == DadColor && FlagColorFor(dad.ToUpperInvariant()) == DadColor,
              $"{FlagLabel(dad)}'s flag at 800 m has his own color (the same however his name is written)");
        Check(flagsToPlant.Any(plan => plan is { Label: "YOUR RECORD", Kind: FlagKind.MyRecord } && plan.Color == RecordGold) &&
              flagsToPlant.Any(plan => plan is { Label: "LAST TIME", Kind: FlagKind.LastTime } && plan.Color == LastTimeSilver),
              "YOUR RECORD is gold, LAST TIME is silver");
        Check(database?.BestDistanceFor(kid.ToLowerInvariant()) == 500 && database?.LastDistanceFor(kid) == 420 &&
              database?.BestDistanceFor("TNobodyAtAll") == 0 && database?.LastDistanceFor("TNobodyAtAll") == 0,
              "BestDistanceFor and LastDistanceFor (0 for somebody who never played)");

        // A last ride only 20 m from the record: no LAST TIME flag
        Ride(near, 1, 500);
        Ride(near, 1, 480);
        hud.SetPlayerName(near);
        BuildFlags();
        Check(flagsToPlant.Any(plan => plan is { Kind: FlagKind.MyRecord, Meters: 500 }) && !flagsToPlant.Any(plan => plan.Kind == FlagKind.LastTime),
              "a last ride 20 m from the record (480 m and 500 m) gets no LAST TIME flag");
        QuietRoad();
    }

    /// <summary>
    /// Zooming past DAD's flag (spec: 790 m, then 801 m). It goes on the road when it comes close, standing right at
    /// 800 m, behind everything else. Passing it: it tips over with X eyes, the big words say YOU PASSED ...! in its
    /// color, a cheer, rainbow confetti and star eyes. Bolt-E keeps riding.
    /// </summary>
    async Task PassDadFlag()
    {
        string dad = FreshName("TPDAD"), kid = FreshName("TPKID"), label = FlagLabel(dad);
        database?.SaveRun(dad, (database?.BestScore() ?? 0) + 1, 800, 0, 0, 0, 30.0);
        hud.SetPlayerName(kid);
        StartGame();
        QuietRoad();
        BuildFlags();
        flagsToPlant.RemoveAll(plan => plan.Label != label); // (just DAD's flag, not the other test people's)
        bestScore = 1_000_000;                               // (no NEW HIGH SCORE! in this test)
        Check(flagsToPlant.Count == 1 && FlagCalled(label) is null, "DAD's flag waits: 800 m is far away");

        distance = 790 * PixelsPerMeter;
        var crate = AddObstacle(ObstacleKind.Crate, ScreenWidth + 50); // (on the road before the flag)
        await Wait(0.3);
        var flag = FlagCalled(label);
        Check(flag is { Passed: false } && flagsToPlant.Count == 0, "at 790 m DAD's flag is on the road");
        if (flag is null) return;
        float shouldBe = RobotX + 800 * PixelsPerMeter - distance;
        Check(Mathf.Abs(flag.Position.X - shouldBe) < 2 && flag.Position.Y == GroundY,
              $"standing on the ground, right at 800 m ({flag.Position.X:0} px, should be {shouldBe:0})");
        Check(flag.GetIndex() < crate.GetIndex() && flag.GetIndex() < robot.GetIndex(), "it stands behind the crate and Bolt-E (it's drawn first)");
        PuffAway(obstacles, _ => true);
        int cheers = sounds.TimesPlayed(Sfx.PassFlag);

        distance = 801 * PixelsPerMeter;
        await Wait(0.2);
        Check(flag.Passed && state == GameState.Playing, "at 801 m DAD's flag is passed, and Bolt-E keeps riding");
        Check(hud.BannerText == $"YOU PASSED {label}!" && hud.BannerShowing && hud.BannerColor == flag.FlagColor,
              $"the big words say '{hud.BannerText}', in the flag's color");
        Check(sounds.TimesPlayed(Sfx.PassFlag) == cheers + 1, "a cheer: ta-da-DA!");
        Check(robot.Face == RobotFace.Stars, $"star eyes! ({robot.Face})");
        Check(Enumerable.Range(0, 3).All(i => particles.Any(p => p.Color.IsEqualApprox(Color.FromHsv(i / 3f, 0.75f, 1f)))), "rainbow confetti");
        Check(flag.Rotation < -0.3f && flag.XEyesDrawn, $"the flag tips over, with X eyes (turned {flag.Rotation:0.00})");
        await Wait(0.5);
        Check(Mathf.Abs(flag.Rotation + 1.35f) < 0.02f, $"...until it lies down ({flag.Rotation:0.00})");
        Check(flag.LabelRect.Size.X > 0 && Mathf.Abs(flag.LabelRect.GetCenter().X) < 1 && flag.LabelRect.End.Y < -RecordFlag.PoleHeight,
              $"its words '{flag.Words}' are measured, in the middle, above the pole");
    }

    /// <summary>
    /// Passing your own flags: LAST TIME (small and silver) gives a little "BEAT LAST TIME!" and a high "bling", and stays
    /// standing. YOUR RECORD (gold, with a star) tips over: NEW RECORD!, a cheer, fireworks, star eyes and a flip.
    /// </summary>
    async Task PassYourFlags()
    {
        string kid = FreshName("TYOURS");
        database?.SaveRun(kid, 1, 500, 0, 0, 0, 30.0);
        database?.SaveRun(kid, 1, 420, 0, 0, 0, 30.0);
        hud.SetPlayerName(kid);
        StartGame();
        QuietRoad();
        BuildFlags();
        flagsToPlant.RemoveAll(plan => plan.Kind == FlagKind.Other); // (just the kid's own flags)
        bestScore = 1_000_000;

        distance = 410 * PixelsPerMeter;
        await Wait(0.2);
        var lastTime = FlagCalled("LAST TIME");
        Check(lastTime is { Passed: false, StarDrawn: false } && lastTime.DrawnSize == 0.7f && FlagCalled("YOUR RECORD") is null,
              "at 410 m the LAST TIME flag is on the road, and it's smaller (YOUR RECORD is still far away)");
        if (lastTime is null) return;
        Check(lastTime.LabelRect.End.Y < -RecordFlag.PoleHeight * 0.7f && Mathf.Abs(lastTime.LabelRect.GetCenter().X) < 1,
              "its words are above its shorter pole, in the middle");
        int blings = sounds.TimesPlayed(Sfx.Bolt), cheers = sounds.TimesPlayed(Sfx.PassFlag);
        distance = 421 * PixelsPerMeter;
        await Wait(0.1);
        Check(lastTime.Passed && popups.Texts.Contains("BEAT LAST TIME!"), "passing it: 'BEAT LAST TIME!'");
        Check(sounds.TimesPlayed(Sfx.Bolt) == blings + 1 && Mathf.Abs(sounds.PitchOf(Sfx.Bolt) - 1.5f) < 0.001f, "and a high 'bling'");
        Check(lastTime.Rotation == 0 && sounds.TimesPlayed(Sfx.PassFlag) == cheers, "(it stays standing, and there's no big cheer for it)");

        distance = 495 * PixelsPerMeter;
        await Wait(0.2);
        var record = FlagCalled("YOUR RECORD");
        Check(record is { Passed: false, StarDrawn: true } && record.DrawnSize == 1, "at 495 m YOUR RECORD is on the road: gold, with a white star");
        if (record is null) return;
        int bursts = fireworkBursts;
        distance = 501 * PixelsPerMeter;
        await Wait(0.1);
        Check(record.Passed && hud.BannerText == "NEW RECORD!" && hud.BannerShowing && hud.BannerColor == RecordGold,
              $"passing it: '{hud.BannerText}'");
        Check(sounds.TimesPlayed(Sfx.PassFlag) == cheers + 1 && robot.Face == RobotFace.Stars && robot.Flipping, "a cheer, star eyes and a flip!");
        await Wait(0.8);
        Check(fireworkBursts == bursts + 4, $"4 fireworks ({fireworkBursts - bursts})");
        Check(Mathf.Abs(record.Rotation + 1.35f) < 0.02f && !record.XEyesDrawn, "the record flag lies down (no X eyes: it's yours)");
    }

    /// <summary>
    /// A flag on the road stays when Big Rusty arrives (he blows everything else away), and riding right past it in
    /// his arena doesn't count as passing it: only the meters count.
    /// </summary>
    async Task FlagsSurviveBoss()
    {
        string dad = FreshName("TBDAD"), kid = FreshName("TBKID"), label = FlagLabel(dad);
        database?.SaveRun(dad, (database?.BestScore() ?? 0) + 1, 800, 0, 0, 0, 30.0);
        hud.SetPlayerName(kid);
        StartGame();
        QuietRoad();
        BuildFlags();
        flagsToPlant.RemoveAll(plan => plan.Label != label);
        bestScore = 1_000_000;
        distance = 790 * PixelsPerMeter;
        await Wait(0.2);
        var flag = FlagCalled(label);
        Check(flag is not null, "DAD's flag is on the road");
        if (flag is null) return;

        var crate = AddObstacle(ObstacleKind.Crate, 900);
        robot.BlinkTime = 1e9f; // (nothing Big Rusty does can crash Bolt-E in this test)
        SpawnBoss();
        await NextFrame();
        Check(!obstacles.Contains(crate) && flags.Contains(flag) && IsInstanceValid(flag) && !flag.IsQueuedForDeletion(),
              "Big Rusty blows the crate away, but the flag stays");
        await Wait(3);
        Check(BossFightActive && flags.Contains(flag) && !flag.Passed && Meters < 800, $"in the fight the flag is still there, not passed ({Meters} m)");
        testMoveAxis = 1;
        Check(await WaitUntil(() => robot.Position.X > flag.Position.X + 40, 3), "Bolt-E rides right past the flag in the arena");
        testMoveAxis = 0;
        await NextFrame();
        Check(!flag.Passed && Meters < 800, "...and that doesn't count as passing it (only the meters count)");
    }

    /// <summary>
    /// A flag stays in step with the road while Bolt-E is put back together (the road still slides along while he's in
    /// pieces), and the rebuild's "poof" never takes a flag away. A flag the road slides past while he's in pieces only
    /// counts as passed once he's riding again (no cheering for a robot in pieces).
    /// </summary>
    async Task FlagsStayInStepAfterRebuild()
    {
        hud.SetPlayerName(FreshName("TSTEP"));
        StartGame();
        QuietRoad();
        await Wait(0.2);
        bestScore = 1_000_000;
        int meters = (int)((distance + 600) / PixelsPerMeter); // (about 600 px ahead)
        int nearMeters = (int)((distance + 120) / PixelsPerMeter); // (about 2 m ahead: the road slides past it while he's in pieces)
        flagsToPlant.Add(new FlagPlan("TEST", meters, FlagKind.Other, FlagColors[0], 0));
        flagsToPlant.Add(new FlagPlan("NEAR", nearMeters, FlagKind.Other, FlagColors[1], 0));
        await NextFrame();
        await NextFrame();
        var flag = FlagCalled("TEST");
        var near = FlagCalled("NEAR");
        Check(flag is not null && Mathf.Abs(flag.Position.X - (RobotX + 600)) < 60, $"a flag goes on the road, about 600 px ahead ({flag?.Position.X:0})");
        Check(near is not null && near.Position.X > RobotX, $"and another one (NEAR) just ahead of Bolt-E ({near?.Position.X:0})");
        if (flag is null || near is null) return;

        speed = 600;
        float distanceAtCrash = distance;
        Check(Bonk(null) && state == GameState.Rebuilding, "a crash with a spare battery");
        Check(await WaitUntil(() => Meters >= nearMeters, 1.5) && state == GameState.Rebuilding,
              $"while he's in pieces the road slides past the NEAR flag ({Meters} m, the flag is at {nearMeters} m)");
        Check(!near.Passed, "...but it doesn't count as passed while he's in pieces");
        await Wait(3);
        Check(near.Passed, "once he's riding again, it counts as passed");
        float shouldBe = RobotX + flag.Meters * PixelsPerMeter - distance;
        Check(state == GameState.Playing && flags.Contains(flag), "Bolt-E is back, and the flag is still on the road");
        Check(distance - distanceAtCrash > 200, $"the road slid along ({distance - distanceAtCrash:0} px)");
        Check(Mathf.Abs(flag.Position.X - shouldBe) < 2, $"the flag stayed in step with the meters (at {flag.Position.X:0} px, it should be at {shouldBe:0})");
    }

    /// <summary>
    /// The score beats the Best number in the corner in the middle of a ride (spec: Best 100, then 200 points):
    /// NEW HIGH SCORE! right then, with the jingle, the Best number flashing gold and 4 fireworks. Only once a game
    /// (the next frames don't cheer again), never when there's no Best yet, and the next game can cheer again.
    /// </summary>
    async Task MidRunHighScore()
    {
        PlayAs(FreshName("THIGH"));
        StartGame();
        QuietRoad();
        await Wait(0.3);
        int jingles = sounds.TimesPlayed(Sfx.HighScore), bursts = fireworkBursts;
        bestScore = 100;
        bonusPoints = 200;
        await NextFrame();
        await NextFrame();
        Check(hud.BannerText == "NEW HIGH SCORE!" && hud.BannerShowing && sounds.TimesPlayed(Sfx.HighScore) == jingles + 1,
              $"'{hud.BannerText}' right away, with the jingle");
        Check(hud.BestFlashing, "the Best number starts flashing");
        await Wait(0.12);
        Check(hud.BestColor.B < 0.6f, $"...gold ({hud.BestColor.ToHtml(false)})");
        await Wait(0.7);
        Check(fireworkBursts == bursts + 4, $"4 fireworks ({fireworkBursts - bursts})");
        bonusPoints = 5000;
        for (int i = 0; i < 5; i++) await NextFrame();
        Check(sounds.TimesPlayed(Sfx.HighScore) == jingles + 1, "only once a game (the next frames and more points don't cheer again)");
        await Wait(0.3);
        Check(!hud.BestFlashing && hud.BestColor == Colors.White, "after 3 flashes the Best number is white again");

        GoToTitle();
        StartGame();
        QuietRoad();
        await Wait(0.2);
        bestScore = 0;
        bonusPoints = 200;
        for (int i = 0; i < 5; i++) await NextFrame();
        Check(sounds.TimesPlayed(Sfx.HighScore) == jingles + 1, "no cheer when there's no Best yet (the very first game)");
        bestScore = 100;
        await NextFrame();
        await NextFrame();
        Check(sounds.TimesPlayed(Sfx.HighScore) == jingles + 2 && hud.BannerText == "NEW HIGH SCORE!", "the next game can cheer again");
    }

    /// <summary>
    /// LB / RB on a controller only switch between real people (spec: TPA and TPB): the controller never ends up in the
    /// name box. Every press is a tick, the name between LB and RB changes, and that player is loaded.
    /// </summary>
    async Task ControllerNeverTypes()
    {
        string a = FreshName("TPA"), b = FreshName("TPB");
        hud.SetPlayers(new List<string> { a, b }, a);
        await Wait(0.1);
        int ticks = sounds.TimesPlayed(Sfx.Tick);
        var seen = new List<string>();
        bool everTyping = false;
        for (int i = 0; i < 3; i++)
        {
            await PressButton(JoyButton.LeftShoulder);
            everTyping |= hud.IsTypingName || hud.NameBox.IsVisibleInTree();
            seen.Add(hud.PlayerName);
        }
        Check(!everTyping, "LB never opens the name box");
        Check(string.Join(" ", seen) == $"{b} {a} {b}", $"LB, LB, LB: {string.Join(", ", seen)} (only real people, round and round)");
        Check(hud.PlayerLabelText == b && bankKey == PlayerKey(b), "the name between LB and RB changes, and that player is loaded");
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks + 3 && Mathf.Abs(sounds.PitchOf(Sfx.Tick) - 0.9f) < 0.001f, "a tick for every press");
        for (int i = 0; i < 3; i++)
        {
            await PressButton(JoyButton.RightShoulder);
            everTyping |= hud.IsTypingName || hud.NameBox.IsVisibleInTree();
        }
        Check(!everTyping && hud.PlayerName == a && state == GameState.Title, $"RB, RB, RB: never the name box either ({hud.PlayerName})");
    }

    /// <summary>
    /// TAB (a key) goes past the last person to NEW PLAYER: the name box, ready to type. RB on a controller gets out of
    /// it (to the first person), and LB to the last one. Clicking the name opens the name box too, and clicking the
    /// LB / RB buttons switches players (no click here ever starts a game).
    /// </summary>
    async Task TabReachesNewPlayer()
    {
        string a = FreshName("TTABA"), b = FreshName("TTABB");
        hud.SetPlayers(new List<string> { a, b }, a);
        await Wait(0.1);
        int taps = 0;
        while (!hud.IsTypingName && taps < 4)
        {
            await TapKey(Key.Tab);
            taps++;
        }
        Check(hud.IsTypingName && taps == 2 && hud.NameBox.HasFocus() && hud.PlayerLabelText == "",
              $"TAB, TAB: {b}, then NEW PLAYER: the name box, ready to type ({taps} taps)");
        Check(hud.PlayerName == b && state == GameState.Title, "(until a name is typed, the player is still the last person picked)");
        await PressButton(JoyButton.RightShoulder);
        Check(!hud.IsTypingName && hud.PlayerName == a && hud.PlayerLabelText == a, $"RB on the controller: out of the name box, to the first person ({hud.PlayerName})");
        await TapKey(Key.Tab);
        await TapKey(Key.Tab);
        Check(hud.IsTypingName, "TAB, TAB: NEW PLAYER again");
        await PressButton(JoyButton.LeftShoulder);
        Check(!hud.IsTypingName && hud.PlayerName == b, $"LB: out of the name box, to the last person ({hud.PlayerName})");

        await Click(hud.PlayerLabelRect.GetCenter());
        Check(hud.IsTypingName && state == GameState.Title, "clicking the name opens the name box (and no game starts)");
        await Click(hud.PickerButtonRect(1).GetCenter());
        Check(!hud.IsTypingName && hud.PlayerName == a && state == GameState.Title, $"clicking RB: the first person ({hud.PlayerName})");
        await Click(hud.PickerButtonRect(1).GetCenter());
        Check(hud.PlayerName == b && state == GameState.Title, $"clicking RB again: the next person ({hud.PlayerName})");
    }

    /// <summary>
    /// Every game starts with big words (spec: TPA plays, then TPB): "TPB'S TURN!" when somebody else played before,
    /// with who to beat today; "GO, TPB!" when it's the same player again. "Player" (no name) never gets "GO".
    /// </summary>
    async Task TurnBannerShows()
    {
        string a = FreshName("TPA"), b = FreshName("TPB");
        hud.SetPlayerName(a);
        StartGame();
        QuietRoad();
        Check(hud.BannerShowing && (hud.BannerText == $"GO, {a}!" || hud.BannerText == $"{a}'S TURN!"), $"{a}'s game starts with big words ('{hud.BannerText}')");
        bonusPoints = 500; // (a score to beat today)
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, $"{a}'s game is over");

        GoToTitle();
        hud.SetPlayerName(b);
        StartGame();
        Check(hud.BannerText == $"{b}'S TURN!" && hud.BannerShowing, $"then it's {b}'s turn: '{hud.BannerText}'");
        var rival = (database?.TodaysBests() ?? new()).FirstOrDefault(best => PlayerKey(best.Name) != PlayerKey(b));
        string beat = $"Beat {Hud.ShortName(rival.Name ?? "?", 10)}'s {rival.Score} from today!";
        Check(hud.HintShowing && hud.HintText == beat, $"with who to beat today: '{hud.HintText}'");
        await Wait(0.3);
        Check(hud.BannerShowing && hud.HintShowing, "the words stay up for a moment");

        GoToTitle();
        previousPlayer = b.ToLowerInvariant(); // (the same person, written in small letters)
        StartGame();
        Check(hud.BannerText == $"GO, {b}!" && hud.BannerShowing && !hud.HintShowing, $"{b} again (even written in small letters): '{hud.BannerText}'");

        GoToTitle();
        hud.SetPlayerName("Player");
        StartGame();
        Check(hud.BannerText == "PLAYER'S TURN!", $"then a game without a name: '{hud.BannerText}'");
        GoToTitle();
        StartGame();
        Check(!hud.BannerShowing, $"'Player' again: no 'GO, PLAYER!' ({hud.BannerText})");
    }

    /// <summary>
    /// On the results card (with 2 people the button says whose turn it is, and LB / RB switch it), TAB goes to NEW
    /// PLAYER: the button says "New player?  (Enter)", and START goes to the title card with the name box ready.
    /// Typing a name and Enter starts that new player's game.
    /// </summary>
    async Task NewPlayerFromResults()
    {
        string a = FreshName("TNEWA"), b = FreshName("TNEWB"), newName = FreshName("TNEWC");
        hud.SetPlayers(new List<string> { a, b }, a);
        StartGame();
        QuietRoad();
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over");
        Check(await WaitUntil(() => hud.ResultsShowing, 3), "the results card shows");
        await Wait(0.4); // (START only works a moment after the card shows)
        Check(hud.PlayButtonText == $"{a}'s turn!  (A)" && hud.PickerButtonsShowing,
              $"with 2 people the button says whose turn it is ('{hud.PlayButtonText}'), between LB and RB");
        await PressButton(JoyButton.RightShoulder);
        Check(hud.PlayButtonText == $"{b}'s turn!  (A)" && hud.PlayerName == b, $"RB: '{hud.PlayButtonText}'");
        await TapKey(Key.Tab);
        Check(hud.NewPlayerPicked && hud.PlayButtonText == "New player?  (Enter)" && state == GameState.GameOver && !hud.IsTypingName,
              $"TAB: NEW PLAYER, and the button says '{hud.PlayButtonText}'");
        await Press("start");
        await Release("start");
        Check(state == GameState.Title && hud.IsTypingName && hud.NameBox.HasFocus(), $"START: the title card, with the name box ready to type ({state})");

        foreach (char letter in newName) await TypeLetter(letter);
        await TapRealKey(Key.Enter);
        Check(state == GameState.Playing && hud.PlayerName == newName && hud.BannerText == $"{newName}'S TURN!",
              $"typing '{newName}' and Enter: {hud.PlayerName}'s game starts ('{hud.BannerText}')");
    }

    /// <summary>
    /// After a game the results card says who's champ today ("Today: ... (champ!)  -  ...", up to 3 people) instead of
    /// the stomps line. Taking turns: when somebody else played the game before, the higher score wins the round.
    /// </summary>
    async Task TodayAndRound()
    {
        string a = FreshName("TROUNDA"), b = FreshName("TROUNDB");
        lastRun = null; // (a fresh start for the rounds)
        async Task<bool> PlayOneGame(string name, int bolts)
        {
            GoToTitle();
            hud.SetPlayerName(name);
            StartGame();
            QuietRoad();
            boltsCollected = bolts;
            spareBatteries = 0;
            bestScore = 1_000_000;
            return Bonk(null) && state == GameState.GameOver && await WaitUntil(() => hud.ResultsShowing, 3);
        }
        // What the line should say (written out again here, so a mistake in TodayLine can't hide)
        string Expected(string? winner)
        {
            var today = database?.TodaysBests() ?? new();
            return "Today: " + string.Join("  -  ", today.Take(3).Select((best, i) => $"{Hud.ShortName(best.Name, 10)} {best.Score}" + (i == 0 ? " (champ!)" : ""))) +
                   (winner is null ? "" : $"  -  Round to {Hud.ShortName(winner, 10)}!");
        }

        Check(await PlayOneGame(a, 30), $"{a} plays a game (300 points)");
        Check((database?.TodaysBests().Count ?? 0) >= 2 && hud.ResultsTodayLine == Expected(null),
              $"the results card says who's champ today: '{hud.ResultsTodayLine}'");
        Check(await PlayOneGame(b, 50), $"then {b} plays (500 points)");
        Check(hud.ResultsTodayLine == Expected(b) && roundsWon.GetValueOrDefault(PlayerKey(b)) == 1,
              $"{b} had the higher score, so {b} wins the round: '{hud.ResultsTodayLine}'");
        Check(await PlayOneGame(a, 10), $"then {a} again (100 points)");
        Check(hud.ResultsTodayLine.EndsWith($"  -  Round to {b}!") && roundsWon.GetValueOrDefault(PlayerKey(b)) == 2 &&
              roundsWon.GetValueOrDefault(PlayerKey(a)) == 0, $"{a} didn't beat {b}'s 500: that round goes to {b} too");
    }

    /// <summary>
    /// Crashing close to a flag (spec: 42 m before DAD's): "So close! Only 42 m to DAD's flag!", or "... to YOUR
    /// RECORD!" when that's the nearest. More than 150 m away, or a LAST TIME flag: no line.
    /// </summary>
    async Task SoCloseResults()
    {
        string dad = FreshName("TCDAD"), kid = FreshName("TCKID"), label = FlagLabel(dad);
        database?.SaveRun(dad, (database?.BestScore() ?? 0) + 1, 800, 0, 0, 0, 30.0);
        hud.SetPlayerName(kid);
        StartGame();
        QuietRoad();
        BuildFlags();
        flagsToPlant.RemoveAll(plan => plan.Kind == FlagKind.Other && plan.Label != label);
        distance = 758 * PixelsPerMeter;
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "a crash at 758 m, 42 m before DAD's flag");
        Check(await WaitUntil(() => hud.ResultsShowing, 3) && hud.ResultsCloseLine == $"So close! Only 42 m to {label}'s flag!",
              $"the results card says '{hud.ResultsCloseLine}'");

        // The next game: the kid's own record (758 m) is nearer than DAD's flag
        GoToTitle();
        StartGame();
        QuietRoad();
        BuildFlags();
        flagsToPlant.RemoveAll(plan => plan.Kind == FlagKind.Other && plan.Label != label);
        Check(SoCloseLine(700) == "So close! Only 58 m to YOUR RECORD!", $"58 m before YOUR RECORD: '{SoCloseLine(700)}'");
        Check(SoCloseLine(600) == "", "158 m before: too far away for 'So close!'");
        flagsToPlant.Add(new FlagPlan("LAST TIME", 720, FlagKind.LastTime, LastTimeSilver, 0));
        Check(SoCloseLine(710) == "So close! Only 48 m to YOUR RECORD!", "a LAST TIME flag doesn't count for 'So close!'");
    }

    /// <summary>
    /// (The fix from the Bolt Bank's review.) Typing a longer name never uses up somebody else's first-launch note:
    /// typing "TSAMANTHA" goes past "TSAM" (who has old games and a "2 NEW LOOKS!" note waiting), even with a pause on
    /// "TSAM", and nothing is announced or saved for TSAM. Picking TSAM for real does announce it. And a party waiting in
    /// line never pops while a name is typed: it waits until the typing stops.
    /// </summary>
    async Task TypingNeverCelebrates()
    {
        string sam = FreshName("TSAM"), samantha = sam + "ANTHA";
        SeedRun(sam, bolts: 300); // (300 bolts win PARTY BOLT-E and BUBBLEGUM: "2 NEW LOOKS!" is waiting for TSAM)
        int jingles = sounds.TimesPlayed(Sfx.Unlock);
        hud.StartTyping();
        await NextFrame();
        bool pausedOnSam = false;
        foreach (char letter in samantha)
        {
            await TypeLetter(letter);
            if (hud.PlayerName == sam)
            {
                pausedOnSam = true;
                await Wait(1.3); // (a pause, longer than the wait before the first party)
            }
        }
        Check(pausedOnSam && hud.NameBox.Text == samantha && bankKey == PlayerKey(samantha),
              $"typed '{hud.NameBox.Text}' (with a pause on '{sam}'), and that's who is loaded");
        Check(sounds.TimesPlayed(Sfx.Unlock) == jingles && !hud.UnlockShowing, $"no note and no jingle while typing (not even on '{sam}')");
        Check(database?.GetSetting("looks_seen:" + PlayerKey(sam)) is null, $"nothing was saved for {sam}: the note still waits for {sam}");
        await TapRealKey(Key.Escape);
        Check(!hud.IsTypingName && hud.PlayerName == samantha && sounds.TimesPlayed(Sfx.Unlock) == jingles,
              $"the typing stops: {samantha} plays (a new name: nothing to announce)");
        hud.SetPlayerName(sam); // (picking TSAM for real, like with LB / RB)
        Check(hud.UnlockShowing && hud.UnlockBigText == "2 NEW LOOKS!" && sounds.TimesPlayed(Sfx.Unlock) == jingles + 1,
              $"picking {sam} for real: '{hud.UnlockBigText}'");

        // A party waiting in line doesn't pop while a name is typed
        string waiting = FreshName("TWAIT");
        SeedRun(waiting, bolts: 120);
        database?.SetSetting("looks_seen:" + PlayerKey(waiting), "classic"); // (PARTY BOLT-E never had its party)
        hud.SetPlayerName(waiting);
        Check(unlockQueue.Count == 1 && !hud.UnlockShowing, "PARTY BOLT-E waits in line for its party");
        hud.StartTyping(); // (nothing typed yet, so the player is still TWAIT)
        await Wait(1.6);
        Check(sounds.TimesPlayed(Sfx.Unlock) == jingles + 1 && unlockQueue.Count == 1, "no party while the name box is open");
        await TapRealKey(Key.Escape);
        Check(hud.PlayerName == waiting && await WaitUntil(() => hud.UnlockShowing, 2.5) && hud.UnlockSmallText == "PARTY BOLT-E",
              $"the typing stops (still {waiting}): now the party happens ('{hud.UnlockSmallText}')");
    }

    /// <summary>(The other fix from the Bolt Bank's review.) Switching who's playing also switches the World Tour postcards.</summary>
    async Task PlayerChangeRefreshesPostcards()
    {
        string traveller = FreshName("TTRAVEL"), homebody = FreshName("THOME");
        SeedRun(traveller, bolts: 0, world: 3); // (rode all the way to Night City)
        hud.SetPlayers(new List<string> { traveller, homebody }, homebody);
        LoadPlayer();
        Check(hud.Postcards.Reached == 1, $"{homebody} never rode anywhere: 1 postcard ({hud.Postcards.Reached})");
        await PressButton(JoyButton.LeftShoulder);
        Check(hud.PlayerName == traveller && hud.Postcards.Reached == 3, $"LB: {traveller} rode to Night City: 3 postcards ({hud.Postcards.Reached})");
        await PressButton(JoyButton.RightShoulder);
        Check(hud.PlayerName == homebody && hud.Postcards.Reached == 1, $"RB: back to {homebody}: 1 postcard ({hud.Postcards.Reached})");
    }

    /// <summary>
    /// Without a database (for example while DB Browser has it locked): nobody to pick (the empty name box), no
    /// "Family Champions" yet, no flags and no Today line, but the game still plays and the results card shows.
    /// </summary>
    async Task FamilyWithoutDatabase()
    {
        var realDatabase = database;
        database = null;
        try
        {
            GoToTitle();
            Check(hud.PlayerCount == 0 && hud.PlayerName == "Player" && hud.TitleRows.FirstOrDefault()?.StartsWith("No scores yet.") == true,
                  $"nobody to pick, and 'No scores yet.' ({string.Join(", ", hud.TitleRows)})");
            StartGame();
            Check(flagsToPlant.Count == 0 && flags.Count == 0 && hud.PlayerName == "Player", "no flags (and the player is 'Player')");
            await Wait(0.2);
            spareBatteries = 0;
            bestScore = 1_000_000;
            Check(Bonk(null) && state == GameState.GameOver, "game over without a database");
            Check(await WaitUntil(() => hud.ResultsShowing, 3) && hud.ResultsTodayLine == "" && hud.ResultsCloseLine == "",
                  "the results card shows (no Today line, no So close line)");
        }
        finally
        {
            database = realDatabase;
            GoToTitle();
        }
    }

    // ---------- Dad drives Big Rusty ----------

    /// <summary>
    /// Nobody touches Dad's keys: Big Rusty fights exactly like before. He flies in, laughs and starts shooting by himself
    /// (he never stops to wait for orders), with no blue cap and no thinking dots, and his bar just says "BIG RUSTY".
    /// </summary>
    async Task NobodyDrivingIsUnchanged()
    {
        var rusty = StartBossFight("TNoDriver");
        var moves = new List<string>();
        bool capSeen = false, dotsSeen = false;
        for (int frame = 0; frame < 5 * 60; frame++) // 5 seconds with no input
        {
            if (moves.Count == 0 || moves[^1] != rusty.CurrentMove) moves.Add(rusty.CurrentMove);
            capSeen |= rusty.CapDrawn;
            dotsSeen |= rusty.DotsDrawn > 0;
            await NextFrame();
        }
        string went = string.Join(" -> ", moves);
        Check(!rusty.DriverControlled, "nobody is driving Big Rusty");
        Check(went == "Entering -> Laughing -> Shooting", $"he went {went}");
        Check(!capSeen && !dotsSeen, "no blue cap and no thinking dots");
        Check(hud.BossBarShowing && hud.BossBarName == Boss.DisplayName, $"his bar says '{hud.BossBarName}'");
    }

    /// <summary>
    /// In Candy Land Dad presses 3 in the middle of the fight: "DAD IS DRIVING BIG RUSTY!", a laugh, Dad's blue cap
    /// instead of the party hat, "BIG RUSTY (DAD)" on his bar, and Dad's keys as a tip. Rusty is busy shooting, so it
    /// says "Wait...". When the thinking dots show, 3 makes him GROUND POUND right away, with a little tick.
    /// </summary>
    async Task DadTakesOver()
    {
        var rusty = StartBossFight("TDadDrives", world: 1); // (in Candy Land he wears a party hat)
        await Wait(3.5);
        Check(rusty.CurrentMove == "Shooting" && !rusty.DriverControlled, $"he's shooting by himself ({rusty.CurrentMove})");
        Check(rusty.OutfitDrawn && !rusty.CapDrawn, "he wears his party hat");
        int laughs = sounds.TimesPlayed(Sfx.Laugh), ticks = sounds.TimesPlayed(Sfx.Tick);

        await Press("drive_pound");
        await Release("drive_pound");
        Check(rusty.DriverControlled, "pressing 3 takes over");
        Check(hud.BannerShowing && hud.BannerText == $"{DriverShortName} IS DRIVING {Boss.DisplayName}!" && hud.BannerColor == new Color(1f, 0.6f, 0.15f),
              $"orange words: '{hud.BannerText}'");
        Check(hud.BossBarName == $"{Boss.DisplayName} ({DriverShortName})", $"his bar says '{hud.BossBarName}'");
        Check(hud.HintShowing && hud.HintText == "1 fireballs  2 helpers  3 POUND  4 laser  H laugh", $"Dad's keys as a tip: '{hud.HintText}'");
        Check(sounds.TimesPlayed(Sfx.Laugh) == laughs + 1, "Big Rusty laughs");
        Check(rusty.CurrentMove == "Shooting" && popups.Texts.Contains("Wait...") && sounds.TimesPlayed(Sfx.Tick) == ticks,
              "he's still busy shooting, so it says 'Wait...' (and no tick)");
        await NextFrame();
        await NextFrame();
        Check(rusty.CapDrawn && !rusty.OutfitDrawn && rusty.DotsDrawn == 0,
              "Dad's blue cap replaces the party hat (and no thinking dots while he shoots)");

        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 8), $"after his fireballs he waits for Dad's order ({rusty.CurrentMove})");
        await NextFrame();
        await NextFrame();
        Check(rusty.DotsDrawn == 3 && rusty.IsDangerous, "three thinking dots over his head (and bumping into him is still a crash)");
        bool facedLeft = rusty.Scale.X > 0;
        robot.Position = new Vector2(1150, GroundY); // (Bolt-E zips past him, to the right)
        await NextFrame();
        await NextFrame();
        Check(facedLeft && rusty.Scale.X < 0 && rusty.CurrentMove == "WaitingForDriver", "while he waits, he turns around to face Bolt-E");
        await Press("drive_pound");
        await Wait(0.1);
        Check(rusty.CurrentMove == "PoundRise", $"3: GROUND POUND, right away ({rusty.CurrentMove})");
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks + 1 && Mathf.IsEqualApprox(sounds.PitchOf(Sfx.Tick), 0.8f), "a little low tick says OK");
        await Release("drive_pound");
    }

    /// <summary>
    /// While the driven Big Rusty is busy shooting, every order gets "Wait...", and none of them is kept for later:
    /// when he's done shooting, he just waits for Dad's next order.
    /// </summary>
    async Task BusyAnswersWait()
    {
        var rusty = StartBossFight("TDriveBusy");
        Check(await WaitUntil(() => rusty.CurrentMove == "Shooting", 5), "he starts shooting by himself");
        await TapKey(Key.Key1); // (the 1 on top of the keyboard)
        Check(rusty.DriverControlled && popups.Texts.Contains("Wait..."), "1 takes over, and he's busy: 'Wait...'");
        var answers = new[] { BossMove.Fireballs, BossMove.Helpers, BossMove.Pound, BossMove.Laser, BossMove.Laugh }
            .Select(move => rusty.Request(move)).ToList();
        Check(answers.All(answer => answer == DriveAnswer.Busy), $"every order while he's shooting is Busy: {string.Join(", ", answers)}");
        await TapKey(Key.Kp3); // (the number pad's 3)
        Check(rusty.CurrentMove == "Shooting", "he keeps on shooting");
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 6), "when he's done, he waits for Dad's order");
        await Wait(0.2);
        Check(rusty.CurrentMove == "WaitingForDriver", $"the orders from while he was busy were not kept for later ({rusty.CurrentMove})");
    }

    /// <summary>
    /// Dad is driving but presses nothing: after 3 seconds of thinking dots, Rusty's own brain picks his usual next attack,
    /// so the fight never stops. And when he's TIRED (2 attacks in a row), even his own brain has to pick the ground pound.
    /// </summary>
    async Task AiFallback()
    {
        var rusty = StartBossFight("TDriveAuto");
        rusty.TakeControl();
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 5), "after laughing he waits for Dad's order");
        ulong waitingSince = Engine.GetProcessFrames();
        await Wait(2.5);
        Check(rusty.CurrentMove == "WaitingForDriver", "2.5 seconds later he's still waiting (Dad has 3 seconds to pick)");
        Check(await WaitUntil(() => rusty.CurrentMove != "WaitingForDriver", 1.5), "then he stops waiting");
        double waited = (Engine.GetProcessFrames() - waitingSince) / 60.0;
        Check(rusty.CurrentMove == "Shooting" && Math.Abs(waited - Boss.DriverWaitBeforeAI) < 0.15,
              $"after {waited:0.00} s his own brain picks his usual first attack ({rusty.CurrentMove})");

        // Now make him TIRED: his own fireballs (1), then Dad's fireballs (2)
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 6), "after the fireballs he waits again");
        Check(rusty.Request(BossMove.Fireballs) == DriveAnswer.Ok, "Dad: more fireballs!");
        Check(await WaitUntil(() => rusty.CurrentMove == "Shooting", 0.5) && await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 6),
              "he shoots, then waits again");
        Check(rusty.IsTired, $"2 attacks in a row: he's TIRED ({rusty.AttacksSincePound} attacks)");
        Check(await WaitUntil(() => rusty.CurrentMove != "WaitingForDriver", 3.5), "Dad presses nothing");
        Check(rusty.CurrentMove == "PoundRise", $"so his own brain has to pick the GROUND POUND, not his usual fireballs ({rusty.CurrentMove})");
    }

    /// <summary>
    /// The fair-play rule: after 2 attacks Big Rusty is TIRED. Then fireballs get "TIRED! Press 3!", but he can still
    /// laugh, and the GROUND POUND works. After the pound he's dizzy (stomp him!) and not tired any more.
    /// </summary>
    async Task TiredRule()
    {
        var rusty = StartBossFight("TDriveTired");
        rusty.TakeControl();
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 5), "he waits for Dad's order");
        Check(rusty.Request(BossMove.Fireballs) == DriveAnswer.Ok, "1st order: fireballs, OK");
        Check(await WaitUntil(() => rusty.CurrentMove == "Shooting", 0.5) && await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 6),
              "he shoots, then waits again");
        Check(!rusty.IsTired, "after 1 attack he's not tired yet");
        int ticks = sounds.TimesPlayed(Sfx.Tick);
        await TapKey(Key.Key1);
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks + 1 && rusty.CurrentMove == "Shooting", "2nd order (the key 1): fireballs, OK (tick!)");
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 6), "he shoots, then waits again");

        Check(rusty.IsTired && rusty.AttacksSincePound == 2, $"2 attacks in a row: he's TIRED ({rusty.AttacksSincePound} attacks)");
        Check(rusty.Request(BossMove.Fireballs) == DriveAnswer.Tired, "more fireballs: TIRED!");
        await TapKey(Key.Key1);
        Check(popups.Texts.Contains("TIRED! Press 3!") && sounds.TimesPlayed(Sfx.Tick) == ticks + 1 && rusty.CurrentMove == "WaitingForDriver",
              "the key 1: 'TIRED! Press 3!' over his head, and nothing happens");
        var laugh = rusty.Request(BossMove.Laugh);
        var pound = rusty.Request(BossMove.Pound); // (asked right after the laugh, so the pound is the order that happens)
        Check(laugh != DriveAnswer.Tired, $"a laugh is never too tiring ({laugh})");
        Check(pound == DriveAnswer.Ok, $"the GROUND POUND works ({pound})");
        Check(await WaitUntil(() => rusty.CurrentMove == "PoundRise", 0.5), $"up he goes for the pound ({rusty.CurrentMove})");
        Check(await WaitUntil(() => rusty.CanBeStomped, 4), "after the pound lands he's dizzy: stomp him!");
        Check(!rusty.IsTired && rusty.AttacksSincePound == 0, "and he's not tired any more");
    }

    /// <summary>
    /// Big Rusty's own attacks from before Dad took over don't count: when he's ANGRY he shoots and then drops helpers
    /// by himself (2 attacks in a row), then Dad takes over, and he's not TIRED for Dad's first order.
    /// </summary>
    async Task FirstOrderNotTired()
    {
        var rusty = StartBossFight("TDriveFirst");
        rusty.LoseHealth(2); // ANGRY (as if he was stomped twice): fireballs, helpers, then a ground pound
        Check(await WaitUntil(() => rusty.CurrentMove == "DroppingHelpers", 12) && rusty.AttacksSincePound == 2,
              $"by himself he shoots, then drops helpers: 2 attacks in a row ({rusty.AttacksSincePound})");
        await TapKey(Key.Key2); // (Dad takes over: he's busy, so "Wait...")
        Check(rusty.DriverControlled && !rusty.IsTired && rusty.AttacksSincePound == 0,
              $"Dad takes over: Rusty's own attacks don't count, so he isn't TIRED ({rusty.AttacksSincePound} attacks)");
    }

    /// <summary>
    /// Big Rusty's moves come with his moods. Phase 1: the helpers and the laser say "Not yet!". ANGRY: the helpers
    /// work (the number pad's 2), the laser still doesn't. FURIOUS: 4 fires the giant laser.
    /// </summary>
    async Task LaserNeedsFurious()
    {
        var rusty = StartBossFight("TDriveLaser");
        rusty.TakeControl();
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 5), "he waits for Dad's order");
        Check(rusty.Request(BossMove.Laser) == DriveAnswer.NotYet, "phase 1: the laser says 'Not yet!'");
        Check(rusty.Request(BossMove.Helpers) == DriveAnswer.NotYet, "phase 1: the helpers say 'Not yet!' too");
        await TapKey(Key.Key4);
        Check(popups.Texts.Contains("Not yet!") && rusty.CurrentMove == "WaitingForDriver", "the key 4: 'Not yet!' over his head, and he keeps waiting");

        rusty.LoseHealth(2); // ANGRY (as if he was stomped twice)
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 4) && rusty.Phase == 2, "ANGRY, and waiting for Dad's order");
        Check(rusty.Request(BossMove.Laser) == DriveAnswer.NotYet, "ANGRY: still no laser");
        int enemiesBefore = enemies.Count;
        await TapKey(Key.Kp2);
        Check(rusty.CurrentMove == "DroppingHelpers" && await WaitUntil(() => enemies.Count > enemiesBefore, 1),
              "ANGRY: the number pad's 2 drops a Pup-Bot helper");

        rusty.LoseHealth(2); // FURIOUS
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 4) && rusty.Phase == 3, "FURIOUS, and waiting for Dad's order");
        await TapKey(Key.Key4);
        Check(rusty.CurrentMove == "Laser" && await WaitUntil(() => lasers.Count > 0, 1.5), "FURIOUS: 4 fires the giant laser");
    }

    /// <summary>H makes Big Rusty laugh "HA HA HA!". For 2 seconds he can't laugh again ("Wait..."), then he can.</summary>
    async Task LaughCooldown()
    {
        var rusty = StartBossFight("TDriveLaugh");
        rusty.TakeControl();
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 5), "he waits for Dad's order");
        int laughs = sounds.TimesPlayed(Sfx.Laugh);
        await TapKey(Key.H);
        Check(rusty.CurrentMove == "Laughing" && popups.Texts.Contains("HA HA HA!") && sounds.TimesPlayed(Sfx.Laugh) == laughs + 1, "H: HA HA HA!");
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 2.5), "then he waits for Dad again");
        Check(rusty.Request(BossMove.Laugh) == DriveAnswer.Busy, "laughing again right away: Busy");
        await TapKey(Key.H);
        Check(popups.Texts.Contains("Wait...") && rusty.CurrentMove == "WaitingForDriver" && sounds.TimesPlayed(Sfx.Laugh) == laughs + 1,
              "the key H: 'Wait...', and no laugh");
        await Wait(0.5);
        Check(rusty.Request(BossMove.Laugh) == DriveAnswer.Ok, "2 seconds after the last laugh he can laugh again");
    }

    /// <summary>
    /// Dad's ground pound works just like Big Rusty's own: his shadow follows Bolt-E, he slams down, and then he's DIZZY,
    /// so Bolt-E can stomp him. When he's FURIOUS, Dad's 3 makes him pound twice, and he's dizzy after the second one.
    /// </summary>
    async Task DriverStillDizzy()
    {
        var rusty = StartBossFight("TDriveDizzy");
        rusty.TakeControl();
        rusty.LoseHealth(4); // FURIOUS (as if he was stomped 4 times)
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 5) && rusty.Phase == 3, "FURIOUS, and waiting for Dad's order");
        var slams = new List<float>();
        rusty.Slammed += x => slams.Add(x);
        await TapKey(Key.Key3);
        Check(rusty.CurrentMove == "PoundRise", $"3: GROUND POUND! ({rusty.CurrentMove})");
        Check(await WaitUntil(() => slams.Count == 1, 4), "he slams down");
        float boltE = robot.Position.X, aim = Mathf.Clamp(boltE, 270, 1030);
        Check(Mathf.Abs(slams[0] - aim) < 40, $"right where Bolt-E is: his shadow followed him (he landed at x {slams[0]:0}, Bolt-E is at x {boltE:0})");
        Check(rusty.CurrentMove == "PoundRise" && !rusty.CanBeStomped, $"FURIOUS: up he goes again for a second pound ({rusty.CurrentMove})");
        Check(await WaitUntil(() => rusty.CanBeStomped, 4) && slams.Count == 2, $"after the second pound he's dizzy: stomp him! ({slams.Count} pounds)");
    }

    /// <summary>
    /// Bolt-E crashes while Dad drives Big Rusty: Rusty laughs and waits until Bolt-E is back together (fair play, like
    /// always). Dad's keys do nothing while Bolt-E is in pieces, and afterwards the thinking dots come back.
    /// </summary>
    async Task DrivenCrashWaits()
    {
        var rusty = StartBossFight("TDriveCrash");
        rusty.TakeControl();
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 5), "he waits for Dad's order");
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.Rebuilding, "Bolt-E crashes (with a spare battery)");
        Check(rusty.CurrentMove == "Recovering", $"HA HA HA! Big Rusty floats back to his spot and waits ({rusty.CurrentMove})");
        int ticks = sounds.TimesPlayed(Sfx.Tick);
        await TapKey(Key.Key1);
        await TapKey(Key.Key3);
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks && !popups.Texts.Contains("Wait...") && rusty.CurrentMove == "Recovering",
              "Dad's keys do nothing while Bolt-E is in pieces");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        robot.BlinkTime = 1e9f;
        await Wait(0.3);
        Check(rusty.CurrentMove == "Recovering", $"Big Rusty still waits a moment after Bolt-E is back ({rusty.CurrentMove})");
        Check(await WaitUntil(() => rusty.CurrentMove != "Recovering", 3) && rusty.CurrentMove == "WaitingForDriver",
              $"then the thinking dots come back: Dad picks the next move ({rusty.CurrentMove})");
    }

    /// <summary>
    /// Bolt-E beats Dad's Big Rusty in Candy Land: "YOU BEAT DAD'S BIG RUSTY!", +250 and the rainbow of bolts like always.
    /// Dad's blue cap flies off as its own piece (the party hat never shows: Dad's cap replaced it). The next Big Rusty
    /// is nobody's again: his bar just says "BIG RUSTY".
    /// </summary>
    async Task YouBeatDadsRusty()
    {
        var rusty = StartBossFight("TDriveBeaten", world: 1); // (in Candy Land he wears a party hat)
        Check(await WaitUntil(() => rusty.CanBeDriven, 3), "Big Rusty flew in");
        await TapKey(Key.Key3);
        Check(rusty.DriverControlled && hud.BossBarName == $"{Boss.DisplayName} ({DriverShortName})", $"Dad drives him ('{hud.BossBarName}')");
        int bonus = bonusPoints, beaten = bossesBeaten, boltsBefore = bolts.Count;
        rusty.LoseHealth(Boss.MaxHealth); // (as if Bolt-E stomped him 6 times)
        Check(await WaitUntil(() => rusty.HasBlownUp, 3), "he breaks down and blows up");
        Check(hud.BannerShowing && hud.BannerText == $"YOU BEAT {DriverShortName}'S {Boss.DisplayName}!", $"'{hud.BannerText}'");
        Check(bossesBeaten == beaten + 1 && bonusPoints == bonus + PointsForBeatingBoss && bolts.Count >= boltsBefore + 12,
              $"+{bonusPoints - bonus} and a rainbow of {bolts.Count - boltsBefore} bolts, like always");
        Check(rusty.CapFlewOff && !rusty.HatFlewOff, "Dad's cap flies off as its own piece (no party hat)");
        await NextFrame();
        await NextFrame();
        Check(rusty.CapDrawn && !rusty.OutfitDrawn, "the cap flies around, and the party hat never shows");
        Check(await WaitUntil(() => boss is null, 5), "his pieces are cleared away");
        SpawnBoss();
        Check(boss is { DriverControlled: false } && hud.BossBarName == Boss.DisplayName, $"the next Big Rusty is nobody's: his bar says '{hud.BossBarName}'");
    }

    /// <summary>
    /// The last battery runs out while Dad drives Big Rusty: the results card says "DAD GOT YOU!" (instead of "Out of
    /// batteries!"). A NEW HIGH SCORE! still comes first, and if Bolt-E already beat him, or nobody drove him, the card
    /// says what it always said.
    /// </summary>
    async Task DadGotYou()
    {
        string title = await ResultsAfterFight(drive: true);
        Check(title == $"{DriverShortName} GOT YOU!", $"Dad drove him: '{title}'");
        title = await ResultsAfterFight(drive: true, newBest: true);
        Check(title == "NEW HIGH SCORE!", $"Dad drove him, but it's a new high score: '{title}'");
        title = await ResultsAfterFight(drive: true, beatHimFirst: true);
        Check(title == "Out of batteries!", $"Dad drove him, but Bolt-E already beat him: '{title}'");
        title = await ResultsAfterFight(drive: false);
        Check(title == "Out of batteries!", $"nobody drove him: '{title}'");
    }

    /// <summary>A Big Rusty fight that ends with Bolt-E's last battery (one spare was used already). Returns the results card's title.</summary>
    async Task<string> ResultsAfterFight(bool drive, bool newBest = false, bool beatHimFirst = false)
    {
        GoToTitle();
        var rusty = StartBossFight("TDriveGotYou");
        if (drive)
        {
            await WaitUntil(() => rusty.CanBeDriven, 3);
            await TapKey(Key.Key3);
        }
        if (beatHimFirst) rusty.LoseHealth(Boss.MaxHealth);
        spareBatteries = 0;
        batteriesUsed = 1;                         // (one spare was used already: so it isn't just "Bonk!")
        bestScore = newBest ? -1 : 1_000_000;      // (-1: any score is a new high score)
        robot.BlinkTime = 0;
        if (!Bonk(null) || state != GameState.GameOver) return "(no game over)";
        return await WaitUntil(() => hud.ResultsShowing, 3) ? hud.ResultsTitle : "(no results card)";
    }

    /// <summary>
    /// Dad's keys are 1 to 4 (on top of the keyboard and on the number pad) and H: nothing else, and no other action
    /// uses them. They do nothing on the title screen, on the road, or while Big Rusty flies in, and a controller can
    /// never drive him. The kid's controller works just like before, even while Dad drives.
    /// </summary>
    async Task DriveKeysOnly()
    {
        var dadKeys = new Dictionary<string, Key[]>
        {
            ["drive_fireballs"] = new[] { Key.Key1, Key.Kp1 },
            ["drive_helpers"] = new[] { Key.Key2, Key.Kp2 },
            ["drive_pound"] = new[] { Key.Key3, Key.Kp3 },
            ["drive_laser"] = new[] { Key.Key4, Key.Kp4 },
            ["drive_laugh"] = new[] { Key.H },
        };
        foreach (var (action, keys) in dadKeys)
        {
            var events = InputMap.ActionGetEvents(action);
            var bound = events.OfType<InputEventKey>().Select(key => key.PhysicalKeycode).OrderBy(key => key).ToList();
            Check(events.Count == keys.Length && events.All(input => input is InputEventKey) && bound.SequenceEqual(keys.OrderBy(key => key)),
                  $"{action} is only the keys {string.Join(" and ", bound)}");
        }
        var allDadKeys = dadKeys.Values.SelectMany(keys => keys).ToHashSet();
        string[] otherActions = { "jump", "start", "music", "left", "right", "menu_left", "menu_right", "player_next", "player_prev" };
        var clashes = otherActions.Where(action => InputMap.ActionGetEvents(action).OfType<InputEventKey>()
                                            .Any(key => allDadKeys.Contains(key.PhysicalKeycode) || allDadKeys.Contains(key.Keycode))).ToList();
        Check(clashes.Count == 0, $"no other action uses Dad's keys {string.Join(", ", clashes)}");

        await TapKey(Key.Key1);
        await TapKey(Key.H);
        Check(state == GameState.Title, "on the title screen Dad's keys do nothing (no game starts)");

        PlayAs("TDriveKeys");
        StartGame();
        QuietRoad();
        robot.BlinkTime = 1e9f;
        int ticks = sounds.TimesPlayed(Sfx.Tick);
        await TapKey(Key.Key3);
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks && !popups.Texts.Any(), "on the road (no Big Rusty) Dad's keys do nothing");

        SpawnBoss();
        var rusty = boss!;
        await TapKey(Key.Key3);
        Check(rusty.CurrentMove == "Entering" && !rusty.DriverControlled, "while Big Rusty flies in, Dad can't take over yet");
        Check(await WaitUntil(() => rusty.CanBeDriven, 2), "he flew in");
        for (int button = 0; button <= (int)JoyButton.Touchpad; button++)
            if ((JoyButton)button != JoyButton.Back) await PressButton((JoyButton)button); // (BACK switches the music)
        await StickX(1);
        await StickX(0);
        Check(!rusty.DriverControlled && hud.BossBarName == Boss.DisplayName, "no controller button can drive him");
        await TapKey(Key.Kp1);
        Check(rusty.DriverControlled, "the number pad's 1 takes over");
        Check(await WaitUntil(() => robot.OnGround, 3), "Bolt-E is on the ground");
        int jumps = sounds.TimesPlayed(Sfx.Jump);
        await PressButton(JoyButton.A);
        Check(sounds.TimesPlayed(Sfx.Jump) == jumps + 1 && !robot.OnGround, "the kid's A button still jumps while Dad drives");
    }

    /// <summary>
    /// Dad's words fit: the big words across the screen (the label is 1280 px wide), the tip, the name on Big Rusty's bar
    /// (400 px wide) and "DAD GOT YOU!" on the results card (at least 800 px wide). They follow DriverName, so a family
    /// that changes it to GRANDMA finds out here if it still fits.
    /// </summary>
    async Task DriverWordsFit()
    {
        await NextFrame();
        float Wide(string text, int size) => ThemeDB.FallbackFont.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        float driving = Wide($"{DriverShortName} IS DRIVING {Boss.DisplayName}!", 60);
        float beaten = Wide($"YOU BEAT {DriverShortName}'S {Boss.DisplayName}!", 60);
        float tip = Wide("1 fireballs  2 helpers  3 POUND  4 laser  H laugh", 28);
        float bar = Wide($"{Boss.DisplayName} ({DriverShortName})", 26);
        float gotYou = Wide($"{DriverShortName} GOT YOU!", 64);
        Check(driving < 1240 && beaten < 1240, $"the big words fit across the screen ({driving:0} and {beaten:0} px of 1280)");
        Check(tip < 1240, $"the tip with Dad's keys fits ({tip:0} px of 1280)");
        Check(bar < 390, $"the name on his bar fits ({bar:0} px of 400)");
        Check(gotYou < 780, $"'{DriverShortName} GOT YOU!' fits on the results card ({gotYou:0} px of 800)");
    }

    // ---------- The final fixes (after the whole update was reviewed) ----------

    /// <summary>
    /// Dad can't stop the fight by pressing H over and over. Two laughs in a row, then a 3rd H says "Wait..." until Big
    /// Rusty has done a real move: Dad's pick (like fireballs), or his own brain's after 3 seconds. Then H works again.
    /// And however long Dad keeps pressing H, Rusty still attacks, gets TIRED, does his ground pound and gets dizzy:
    /// the kid always gets his chance to stomp him.
    /// </summary>
    async Task LaughsCantStallTheFight()
    {
        var rusty = StartBossFight("TDriveLaughs");
        rusty.TakeControl();
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 5), "he waits for Dad's order");

        // Dad presses H again and again (10 times a second) for at most this many seconds, or until "stop".
        // Returns how many times Big Rusty laughed.
        async Task<int> PressHAgainAndAgain(double seconds, Func<bool>? stop = null)
        {
            int laughsBefore = sounds.TimesPlayed(Sfx.Laugh);
            var timeUp = GetTree().CreateTimer(seconds, true, false, true);
            while (timeUp.TimeLeft > 0 && stop?.Invoke() != true) await TapKey(Key.H);
            return sounds.TimesPlayed(Sfx.Laugh) - laughsBefore;
        }

        // 1. Two laughs in a row, then "Wait..." (without the rule, a 3rd laugh would come after about 4 seconds)
        int laughed = await PressHAgainAndAgain(5.5);
        Check(laughed == Boss.LaughsInARow, $"H, H, H...: he laughs {laughed} times in a row ({Boss.LaughsInARow} is the most)");
        Check(rusty.CurrentMove == "WaitingForDriver" && rusty.Request(BossMove.Laugh) == DriveAnswer.Busy && popups.Texts.Contains("Wait..."),
              "then H says 'Wait...': first a real move has to come");
        await TapKey(Key.Key1);
        Check(rusty.CurrentMove == "Shooting", $"Dad presses 1: fireballs, a real move ({rusty.CurrentMove})");
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 6), "he shoots, then waits for Dad again");
        Check(rusty.Request(BossMove.Laugh) == DriveAnswer.Ok, "after Dad's real move, H works again");

        // 2. Now Dad only presses H: after the 2nd laugh in a row, Rusty's own brain picks a real move after 3 seconds
        Check(await WaitUntil(() => rusty.CurrentMove == "Laughing", 0.5), "HA HA HA! (1 in a row)");
        laughed = await PressHAgainAndAgain(10, () => rusty.CurrentMove == "Shooting");
        Check(rusty.CurrentMove == "Shooting" && laughed == 1,
              $"one more laugh, then his own brain picks fireballs ({rusty.CurrentMove}, after {laughed} more laugh)");
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 6), "he shoots, then waits for Dad again");
        Check(rusty.IsTired && rusty.Request(BossMove.Laugh) == DriveAnswer.Ok,
              "after his own brain's real move, H works again (and he's TIRED now: 2 attacks)");

        // 3. Dad keeps on pressing H: Rusty still has to do his ground pound, and then he's dizzy
        Check(await WaitUntil(() => rusty.CurrentMove == "Laughing", 0.5), "HA HA HA!");
        await PressHAgainAndAgain(15, () => rusty.CanBeStomped);
        Check(rusty.CanBeStomped, $"however long Dad presses H, the ground pound comes and he's dizzy: stomp him! ({rusty.CurrentMove})");
    }

    /// <summary>
    /// Dad's little words ("Wait...", "TIRED! Press 3!", "Not yet!") never float up into Big Rusty's health bar at the top
    /// of the screen: not while he's way up high for a ground pound over the middle of the road, and not while he flies
    /// back to his spot. At his spot, they pop up right over his head. Pressing again and again doesn't stack them up.
    /// </summary>
    async Task OrderWordsBelowTheBar()
    {
        var rusty = StartBossFight("TDriveWords");
        rusty.TakeControl();
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 5), "he waits for Dad's order");
        var bar = hud.BossBarRect;
        var font = ThemeDB.FallbackFont;
        string[] dadsWords = { "Wait...", "TIRED! Press 3!", "Not yet!" };
        int looks = 0;
        var tooHigh = new List<string>();

        // Where each of Dad's words is drawn right now (the letters and their dark outline) must never touch the bar
        void LookAtTheWords()
        {
            foreach (var (text, at, size) in popups.Spots)
            {
                if (!dadsWords.Contains(text)) continue;
                float wide = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X + 8;
                var box = new Rect2(at.X - wide / 2, at.Y - font.GetAscent(size) - 4, wide, font.GetAscent(size) + font.GetDescent(size) + 8);
                looks++;
                if (box.Intersects(bar)) tooHigh.Add($"'{text}' at ({at.X:0}, {at.Y:0})");
            }
        }

        // 1. At his spot: right over his head
        await TapKey(Key.Key2); // (helpers, but he isn't ANGRY yet: "Not yet!")
        var notYet = popups.Spots.FirstOrDefault(spot => spot.Text == "Not yet!");
        Check(notYet.Text is not null && Mathf.Abs(notYet.At.X - rusty.Position.X) < 1 && Mathf.Abs(notYet.At.Y - (rusty.Position.Y - 260)) < 15,
              $"at his spot, 'Not yet!' pops up right over his head (at {notYet.At.X:0}, {notYet.At.Y:0})");

        // 2. A ground pound over the middle of the road (under the bar), and Dad presses 1 again and again while he's busy
        robot.Position = new Vector2(640, GroundY); // (Bolt-E is in the middle, so Rusty's shadow follows him there)
        await TapKey(Key.Key3);
        Check(rusty.CurrentMove == "PoundRise", $"3: up he goes for a ground pound ({rusty.CurrentMove})");
        GiveOrder(BossMove.Fireballs);
        GiveOrder(BossMove.Fireballs);
        GiveOrder(BossMove.Fireballs);
        Check(popups.Texts.Count(text => text == "Wait...") == 1, "pressing 1 three times quickly: just one 'Wait...', not a stack of them");
        var highUp = new List<(float FromRusty, float Y)>(); // (words that popped up while he was way up high)
        for (int frame = 0; frame < 4 * 60; frame++)
        {
            if (frame % 20 == 0)
            {
                int before = popups.Texts.Count();
                GiveOrder(BossMove.Fireballs); // ("Wait...": he's busy)
                if (popups.Texts.Count() > before && rusty.Position.Y < 0) highUp.Add((popups.Spots.Last().At.X - rusty.Position.X, popups.Spots.Last().At.Y));
            }
            LookAtTheWords();
            await NextFrame();
        }
        Check(highUp.Count > 0 && highUp.All(spot => Mathf.Abs(spot.FromRusty) < 1 && spot.Y > bar.End.Y + ScorePopups.FloatUp + 20 && spot.Y < GroundY - 200),
              $"while he's way up high, the words pop up in the sky over his shadow, low enough to float up without reaching the bar " +
              $"(at y {string.Join(", ", highUp.Select(spot => spot.Y.ToString("0")))})");

        // 3. Dizzy in the middle of the road, then hurt: he flies back up to his spot, under the bar
        Check(rusty.CanBeStomped && Mathf.Abs(rusty.Position.X - 640) < 60, $"after the pound he's dizzy in the middle of the road (x {rusty.Position.X:0})");
        rusty.LoseHealth(1); // (as if Bolt-E stomped him)
        for (int frame = 0; frame < 132; frame++) // (2.2 seconds)
        {
            if (frame % 10 == 0) GiveOrder(BossMove.Fireballs);
            LookAtTheWords();
            await NextFrame();
        }
        Check(looks > 150 && tooHigh.Count == 0,
              $"Dad's words never floated up into the health bar ({looks} looks{(tooHigh.Count > 0 ? "; too high: " + string.Join(", ", tooHigh.Take(3)) : "")})");
    }

    /// <summary>
    /// After the giant laser Big Rusty is down low. While he waits for Dad's order he floats back up to his spot smoothly,
    /// and if Dad quickly picks something (here H), he still floats up smoothly: never a big jump in one go. Lasers count
    /// as attacks for the TIRED rule too: after 2 lasers, a 3rd one gets "TIRED!".
    /// </summary>
    async Task SmoothRiseAfterLaser()
    {
        var rusty = StartBossFight("TDriveLaserUp");
        rusty.TakeControl();
        rusty.LoseHealth(4); // FURIOUS (as if he was stomped 4 times)
        Check(await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 6) && rusty.Phase == 3, "FURIOUS, and waiting for Dad's order");
        float hoverY = GroundY - 230; // (his spot)

        // Watches him for some frames: the biggest jump up or down between two frames, and where he ends up
        async Task<(float BiggestStep, float EndY)> WatchHeight(int frames)
        {
            float biggest = 0, last = rusty.Position.Y;
            for (int i = 0; i < frames; i++)
            {
                await NextFrame();
                biggest = Mathf.Max(biggest, Mathf.Abs(rusty.Position.Y - last));
                last = rusty.Position.Y;
            }
            return (biggest, last);
        }

        // 1. A laser, and then Dad picks nothing
        Check(rusty.Request(BossMove.Laser) == DriveAnswer.Ok, "4: the giant laser");
        Check(await WaitUntil(() => rusty.CurrentMove == "Laser", 0.5) && await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 4),
              "he fires it, then waits for Dad's order");
        Check(rusty.Position.Y > GroundY - 80, $"(right after the laser he's still down low: y {rusty.Position.Y:0})");
        Check(rusty.AttacksSincePound == 1 && !rusty.IsTired, $"the laser counts as an attack ({rusty.AttacksSincePound})");
        var (step, endY) = await WatchHeight(45); // (0.75 seconds)
        Check(step < 15, $"while he waits, he floats back up smoothly (the biggest step in one frame: {step:0} px)");
        Check(Mathf.Abs(endY - hoverY) <= 13 && rusty.CurrentMove == "WaitingForDriver", $"...all the way up to his spot (y {endY:0}, his spot is {hoverY:0})");

        // 2. Another laser, and this time Dad picks H right away, while he's still down low
        Check(rusty.Request(BossMove.Laser) == DriveAnswer.Ok, "4 again: another giant laser");
        Check(await WaitUntil(() => rusty.CurrentMove == "Laser", 0.5) && await WaitUntil(() => rusty.CurrentMove == "WaitingForDriver", 4),
              "he fires it, then waits for Dad's order");
        Check(rusty.IsTired && rusty.Request(BossMove.Laser) == DriveAnswer.Tired,
              $"2 lasers in a row: he's TIRED, so a 3rd laser gets 'TIRED! Press 3!' ({rusty.AttacksSincePound} attacks)");
        Check(rusty.Request(BossMove.Laugh) == DriveAnswer.Ok, "H, right away (he's still down low)");
        (step, endY) = await WatchHeight(45);
        Check(rusty.CurrentMove == "Laughing", $"he laughs ({rusty.CurrentMove})");
        Check(step < 15, $"...and floats back up smoothly while he laughs, never a jump (the biggest step in one frame: {step:0} px)");
        Check(Mathf.Abs(endY - hoverY) <= 13, $"...all the way up to his spot (y {endY:0})");
    }

    /// <summary>
    /// RecordFlag.PoleHeight really works: with a taller pole (300 instead of 230) the cloth, the star, the X eyes and the
    /// words all go up with it (on the smaller LAST TIME flag too), and the confetti flies from the top of the pole.
    /// </summary>
    async Task TallerFlagPole()
    {
        float normalPole = RecordFlag.PoleHeight;
        try
        {
            RecordFlag.PoleHeight = 300;
            PlayAs(FreshName("TPOLE"));
            StartGame();
            QuietRoad();
            bestScore = 1_000_000;
            int m = Meters;
            flagsToPlant.Add(new FlagPlan("YOUR RECORD", m + 12, FlagKind.MyRecord, RecordGold, 0));
            flagsToPlant.Add(new FlagPlan("LAST TIME", m + 14, FlagKind.LastTime, LastTimeSilver, 0));
            flagsToPlant.Add(new FlagPlan("TPOLEDAD", m + 16, FlagKind.Other, FlagColors[0], 0));
            for (int i = 0; i < 3; i++) await NextFrame();
            RecordFlag? record = FlagCalled("YOUR RECORD"), lastTime = FlagCalled("LAST TIME"), dad = FlagCalled("TPOLEDAD");
            Check(record is not null && lastTime is not null && dad is not null, "three flags on the road, with 300 px poles");
            if (record is null || lastTime is null || dad is null) return;

            Check(record.ClothTopY == -300 && record.StarDrawn && record.StarY == -283,
                  $"the cloth is at the top of the taller pole (y {record.ClothTopY:0}), with the star in its middle (y {record.StarY:0})");
            Check(record.LabelRect.End.Y < -300 && record.LabelRect.End.Y > -330, $"...and the words are just above the pole (their bottom at y {record.LabelRect.End.Y:0})");
            Check(Mathf.IsEqualApprox(lastTime.ClothTopY, -300 * 0.7f) && lastTime.LabelRect.End.Y < -210 && lastTime.LabelRect.End.Y > -240,
                  $"the smaller LAST TIME flag too (cloth at y {lastTime.ClothTopY:0}, words at y {lastTime.LabelRect.End.Y:0})");

            PassFlag(dad); // (Bolt-E zooms past)
            var top = dad.PoleTop;
            int confetti = particles.Count(p => p.Position == top);
            Check(top == dad.Position + new Vector2(0, -300) && confetti == 36, $"the rainbow confetti flies from the top of the pole ({confetti} bits at y {top.Y:0})");
            await NextFrame();
            await NextFrame();
            Check(dad.XEyesDrawn && dad.StarY == -283, $"X eyes, in the middle of the cloth (y {dad.StarY:0})");
        }
        finally
        {
            RecordFlag.PoleHeight = normalPole;
        }
    }

    /// <summary>
    /// A world with Gravity 0 (or even less) in Worlds.cs can't break the game: gravity never counts as less than 0.1, so
    /// things still come along the road (not never, and not every frame), and a jump still goes just as high and comes
    /// back down (floaty!).
    /// </summary>
    async Task GravityGuard()
    {
        var moon = Worlds.All[4];
        try
        {
            PlayAs("TGravityZero");
            StartGame();
            QuietRoad();
            await Wait(0.2);
            foreach (float gravity in new[] { 0f, -1f })
            {
                Worlds.All[4] = moon with { Gravity = gravity };
                ChangeWorld(4);
                Check(Mathf.IsEqualApprox(robot.GravityScale, Robot.LeastGravity),
                      $"a world with Gravity {gravity}: Bolt-E feels {robot.GravityScale} (never less than {Robot.LeastGravity})");
                float stretch = GapStretch;
                Check(float.IsFinite(stretch) && stretch > 3 && stretch < 3.2f, $"...and things come {stretch:0.00} times farther apart (not never)");
                spawnCountdown = 0;
                int before = obstacles.Count + enemies.Count;
                for (int i = 0; i < 30; i++) await NextFrame(); // (half a second)
                int came = obstacles.Count + enemies.Count - before;
                Check(came == 1 && float.IsFinite(spawnCountdown) && spawnCountdown > 1000,
                      $"...one thing comes, and the next one waits (not one every frame): {came} came, the next in {spawnCountdown:0} px");
                PuffAway(obstacles, _ => true);
                PuffAway(enemies, _ => true);
                PuffAway(bolts, _ => true);
                QuietRoad();
            }
            var (height, air) = await MeasureJump();
            Check(Mathf.Abs(height - 212) < 15 && robot.OnGround && air > 2,
                  $"a jump still goes just as high ({height:0} px) and comes back down (after {air:0.00} seconds: floaty!)");
        }
        finally
        {
            Worlds.All[4] = moon;
        }
    }

    /// <summary>
    /// A new world in the middle of a jump: the jump still goes just as high (212 px), from Sunny Hills to the floaty Moon
    /// and back again. And a double jump goes just as high on the Moon as in Sunny Hills (it just takes longer).
    /// </summary>
    async Task MidAirWorldChange()
    {
        PlayAs("TMidAir");
        StartGame();
        QuietRoad();
        await Wait(0.3);

        // Jumps, and goes to world number n once he's 70 px up. Returns how high the jump went.
        async Task<float> JumpAndChangeWorldOnTheWay(int n)
        {
            await NextFrame();
            float groundY = robot.Position.Y, highest = groundY;
            bool changed = false;
            robot.Jump();
            for (int frame = 0; frame < 240 && !robot.OnGround; frame++)
            {
                await NextFrame();
                highest = Mathf.Min(highest, robot.Position.Y);
                if (!changed && groundY - robot.Position.Y > 70)
                {
                    ChangeWorld(n);
                    changed = true;
                }
            }
            return groundY - highest;
        }
        float toMoon = await JumpAndChangeWorldOnTheWay(4);
        Check(CurrentWorld.Name == "THE MOON" && Mathf.Abs(toMoon - 212) < 15,
              $"the Moon comes in the middle of a jump: it still goes {toMoon:0} px high (it would have gone about 330)");
        float toSunny = await JumpAndChangeWorldOnTheWay(5);
        Check(CurrentWorld.Name == "SUNNY HILLS" && Mathf.Abs(toSunny - 212) < 15,
              $"Sunny Hills comes in the middle of a Moon jump: it still goes {toSunny:0} px high (it would have gone about 150)");

        // A double jump: the 2nd jump right at the top of the 1st
        async Task<(float Height, float Air)> DoubleJump()
        {
            await NextFrame();
            float groundY = robot.Position.Y, highest = groundY;
            bool second = false;
            int frames = 0;
            robot.Jump();
            while (frames < 300 && !robot.OnGround)
            {
                await NextFrame();
                frames++;
                highest = Mathf.Min(highest, robot.Position.Y);
                if (!second && robot.IsFalling)
                {
                    robot.Jump();
                    second = true;
                }
            }
            return (groundY - highest, frames / 60f);
        }
        var sunnyDouble = await DoubleJump();
        ChangeWorld(4);
        var moonDouble = await DoubleJump();
        Check(sunnyDouble.Height > 330 && Mathf.Abs(moonDouble.Height - sunnyDouble.Height) < 15,
              $"a double jump goes just as high on the Moon ({moonDouble.Height:0} px) as in Sunny Hills ({sunnyDouble.Height:0} px)");
        Check(moonDouble.Air > sunnyDouble.Air + 0.3f, $"...it just takes longer ({moonDouble.Air:0.00} seconds instead of {sunnyDouble.Air:0.00})");
    }

    /// <summary>
    /// The title screen and every new game play the music at Sunny Hills' own MusicSpeed (in Worlds.cs) right from the
    /// start, so changing that number really changes the music. (With the normal numbers it's 1, normal speed.)
    /// </summary>
    async Task SunnyMusicFromTheStart()
    {
        await NextFrame();
        Check(sounds.MusicOn && Mathf.Abs(sounds.MusicPitch - Worlds.All[0].MusicSpeed) < 0.001f,
              $"the title screen plays the music at Sunny Hills' speed ({sounds.MusicPitch:0.00})");
        var sunny = Worlds.All[0];
        try
        {
            Worlds.All[0] = sunny with { MusicSpeed = 1.2f };
            GoToTitle();
            Check(Mathf.Abs(sounds.MusicPitch - 1.2f) < 0.001f, $"Sunny Hills' MusicSpeed changed to 1.2: the title music plays at 1.2 right away ({sounds.MusicPitch:0.00})");
            PlayAs("TMusicSpeed");
            StartGame();
            Check(Mathf.Abs(sounds.MusicPitch - 1.2f) < 0.001f, $"...and so does a new game, from the very start ({sounds.MusicPitch:0.00})");
            await Wait(0.3);
            Check(Mathf.Abs(sounds.MusicPitch - 1.2f) < 0.001f, $"...and it stays at 1.2 ({sounds.MusicPitch:0.00})");
        }
        finally
        {
            Worlds.All[0] = sunny;
            GoToTitle();
        }
    }

    /// <summary>
    /// Dad starts typing a new name, and the kid presses A on the controller when only 1 letter is typed: the game starts
    /// as the last person picked, and no 1-letter person is saved. With 2 letters or more, the new name plays.
    /// </summary>
    async Task ShortNameIsNotAPerson()
    {
        string real = FreshName("TREAL"), two = FreshName("TQ");
        hud.SetPlayers(new List<string> { real }, real);
        await Wait(0.1);
        hud.StartTyping();
        await NextFrame();
        await TypeLetter('Q');
        Check(hud.IsTypingName && hud.NameBox.Text == "Q", $"Dad starts typing a new name ('{hud.NameBox.Text}')");
        await PressButton(JoyButton.A);
        Check(state == GameState.Playing && hud.PlayerName == real && !hud.NameBox.Visible,
              $"the kid presses A with only 1 letter typed: the game starts as {hud.PlayerName}, the last person picked");
        Check(hud.PlayerCount == 1 && database?.GetSetting("player_name") == real, "...and no 1-letter person 'Q' was added");

        GoToTitle();
        hud.SetPlayers(new List<string> { real }, real);
        hud.StartTyping();
        await NextFrame();
        foreach (char letter in two) await TypeLetter(letter);
        await PressButton(JoyButton.A);
        Check(state == GameState.Playing && hud.PlayerName == two && hud.PlayerCount == 2,
              $"with {two.Length} letters typed ('{two}'), A starts the new player's game ({hud.PlayerName})");
    }

    /// <summary>
    /// Music off and on (M, or BACK on the controller) while Bolt-E is in pieces: it comes back still wobbled down, and
    /// goes back up when he's back together. On the results card after a Big Rusty fight it comes back at normal speed,
    /// not at the fight's speed.
    /// </summary>
    async Task MusicToggleKeepsTheDip()
    {
        PlayAs("TMusicToggle");
        StartGame();
        QuietRoad();
        await Wait(0.3);
        Check(sounds.MusicOn && sounds.MusicPlaying, "the music is playing");
        Check(Bonk(null) && state == GameState.Rebuilding, "a crash with a spare battery");
        await Wait(0.7);
        Check(Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.05f, $"the music wobbles down ({sounds.MusicPitch:0.00})");
        await TapKey(Key.M);
        Check(!sounds.MusicOn && !sounds.MusicPlaying, "M: music off");
        await TapKey(Key.M);
        Check(sounds.MusicOn && sounds.MusicPlaying && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.01f && state == GameState.Rebuilding,
              $"M again: it comes back still wobbled down, because he's still in pieces ({sounds.MusicPitch:0.00})");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "Bolt-E is back");
        await Wait(0.8);
        Check(Mathf.Abs(sounds.MusicPitch - CurrentWorld.MusicSpeed) < 0.01f, $"...and the music goes back up ({sounds.MusicPitch:0.00})");

        // A Big Rusty fight that ends with the last battery
        robot.BlinkTime = 1e9f;
        SpawnBoss();
        await Wait(0.8);
        Check(sounds.MusicPitch > 1.05f, $"the fight music is faster ({sounds.MusicPitch:0.00})");
        spareBatteries = 0;
        bestScore = 1_000_000;
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.GameOver, "game over in the fight");
        Check(await WaitUntil(() => hud.ResultsShowing, 3) && !sounds.MusicPlaying, "the results card shows, and the music has powered down");
        await TapKey(Key.M);
        await TapKey(Key.M);
        Check(sounds.MusicOn && sounds.MusicPlaying && Mathf.Abs(sounds.MusicPitch - 1f) < 0.01f,
              $"music off and on: it comes back at normal speed, not at the fight's speed ({sounds.MusicPitch:0.00})");
    }

    /// <summary>
    /// The first-launch note ("2 NEW LOOKS! Your old games count!") is never lost: its looks only count as seen once it has
    /// been showing for 2.5 seconds. The kid pressing A right away (or LB / RB) cuts it short, and then it comes back: on
    /// the results card after the game (with the game's new looks in it too), or the next time that player is picked.
    /// </summary>
    async Task FirstNoteNeverLost()
    {
        string kid = FreshName("TEAGER"), key = PlayerKey(kid);
        SeedRun(kid, bolts: 300); // (300 bolts win PARTY BOLT-E and BUBBLEGUM)
        database?.SetSetting("player_name", kid);
        int jingles = sounds.TimesPlayed(Sfx.Unlock);
        GoToTitle();
        Check(hud.UnlockShowing && hud.UnlockBigText == "2 NEW LOOKS!", $"the game opens for {kid}: '{hud.UnlockBigText}'");
        await Wait(0.3);
        StartGame(); // (the kid presses A right away)
        Check(!hud.UnlockShowing && database?.GetSetting("looks_seen:" + key) is null, "A after 0.3 seconds: the note goes away, and its looks don't count as seen");

        // The game ends: the note comes on the results card, after the count-up (700 bolts now: FIRE TRUCK and COWBOY too)
        QuietRoad();
        boltsCollected = 400;
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver, "game over with 400 bolts");
        Check(await WaitUntil(() => hud.ResultsShowing, 3), "the results card shows");
        Check(await WaitUntil(() => hud.UnlockShowing, 4) && hud.UnlockBigText == "4 NEW LOOKS!" && hud.UnlockSmallText == "Your old games count!",
              $"after the count-up: '{hud.UnlockBigText}' '{hud.UnlockSmallText}'");
        Check(sounds.TimesPlayed(Sfx.Unlock) == jingles + 2 && unlockQueue.Count == 0,
              "(one note for all of them: no separate party for FIRE TRUCK or COWBOY)");
        GoToTitle(); // (START pressed quickly again)
        Check(database?.GetSetting("looks_seen:" + key) is null, "cut short again: still not seen");
        Check(hud.UnlockShowing && hud.UnlockBigText == "4 NEW LOOKS!", "so on the title screen the note comes back");
        bool saved = await WaitUntil(() => database?.GetSetting("looks_seen:" + key) is not null, 3.5);
        Check(saved && database?.GetSetting("looks_seen:" + key) == "classic,party,bubblegum,firetruck,cowboy",
              $"this time it shows long enough to read, and now the looks count as seen ({database?.GetSetting("looks_seen:" + key)})");
        GoToTitle();
        await Wait(1.2);
        Check(!hud.UnlockShowing, "after that it doesn't come again");

        // LB / RB through the family: a note that was only up for a moment isn't used up
        string a = FreshName("TRBA"), b = FreshName("TRBB");
        SeedRun(a, bolts: 150); // (1 NEW LOOK!)
        SeedRun(b, bolts: 300); // (2 NEW LOOKS!)
        hud.SetPlayers(new List<string> { kid, a, b }, kid);
        LoadPlayer();
        await PressButton(JoyButton.RightShoulder);
        Check(hud.PlayerName == a && hud.UnlockShowing && hud.UnlockBigText == "1 NEW LOOK!", $"RB: {a}'s note ('{hud.UnlockBigText}')");
        await PressButton(JoyButton.RightShoulder);
        Check(hud.PlayerName == b && hud.UnlockShowing && hud.UnlockBigText == "2 NEW LOOKS!", $"RB again right away: {b}'s note ('{hud.UnlockBigText}')");
        Check(database?.GetSetting("looks_seen:" + PlayerKey(a)) is null, $"{a}'s note was only up for a moment, so it isn't used up");
        await PressButton(JoyButton.LeftShoulder);
        Check(hud.PlayerName == a && hud.UnlockShowing && hud.UnlockBigText == "1 NEW LOOK!", $"LB: back to {a}, and the note comes again");
        Check(database?.GetSetting("looks_seen:" + PlayerKey(b)) is null, $"(and {b}'s isn't used up either)");
    }

    /// <summary>A ? box still on the road when a game ends never comes along into the next game.</summary>
    async Task NewGameHasNoOldBoxes()
    {
        await NextFrame();
        PlayAs("TOldBox");
        StartGame();
        QuietRoad();
        var box = AddBox(900);
        GoToTitle();
        StartGame();
        Check(boxes.Count == 0 && (!IsInstanceValid(box) || box.IsQueuedForDeletion()), "a new game starts without the last game's ? box");
    }

    /// <summary>LB / RB with only one person to pick: nothing changes, so there's no tick. With two people: a tick.</summary>
    async Task NoTickWithNobodyElse()
    {
        string only = FreshName("TONLY");
        hud.SetPlayers(new List<string> { only }, only);
        await Wait(0.1);
        int ticks = sounds.TimesPlayed(Sfx.Tick);
        await PressButton(JoyButton.LeftShoulder);
        await PressButton(JoyButton.RightShoulder);
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks && hud.PlayerName == only, "LB and RB with only one person: nothing changes, and no tick");
        hud.SetPlayers(new List<string> { only, FreshName("TOTHERONE") }, only);
        await PressButton(JoyButton.RightShoulder);
        Check(sounds.TimesPlayed(Sfx.Tick) == ticks + 1 && hud.PlayerName != only, "with two people, RB switches (tick!)");
    }

    /// <summary>
    /// "Today" really means today: a game from long ago (even with the best score of all) is never in today's scores,
    /// the results card's Today line, or the "Beat ... from today!" tip. (This test is the last one, because it saves the
    /// best score of all in the scratch database.)
    /// </summary>
    async Task OnlyTodayCounts()
    {
        string old = FreshName("TLONGAGO"), a = FreshName("TTODAYA"), b = FreshName("TTODAYB");
        int top = (database?.BestScore() ?? 0) + 1000;
        SaveGameFromLongAgo(old, top);
        Check(database?.StatsFor(old).BestScore == top, $"{old} has the best score of all ({top}), from the year 2000");
        var today = database?.TodaysBests() ?? new();
        Check(today.Count >= 2 && !today.Any(best => PlayerKey(best.Name) == PlayerKey(old)),
              $"...but it isn't one of today's scores ({today.Count} people played today)");

        lastRun = null;
        hud.SetPlayerName(a);
        StartGame();
        QuietRoad();
        bonusPoints = 50;
        spareBatteries = 0;
        bestScore = 1_000_000;
        Check(Bonk(null) && state == GameState.GameOver && await WaitUntil(() => hud.ResultsShowing, 3), $"{a} plays a game");
        Check(hud.ResultsTodayLine.StartsWith("Today: ") && !hud.ResultsTodayLine.Contains(old),
              $"the results card's Today line doesn't have {old}: '{hud.ResultsTodayLine}'");

        GoToTitle();
        hud.SetPlayerName(b);
        StartGame();
        Check(hud.HintShowing && hud.HintText.StartsWith("Beat ") && !hud.HintText.Contains(old), $"{b}'s turn: '{hud.HintText}'");
    }

    /// <summary>
    /// Saves a pretend game from long ago (the year 2000) straight into the scratch database file, with a connection of its
    /// own, like DB Browser would. (The game itself always saves games with today's date and time.)
    /// </summary>
    void SaveGameFromLongAgo(string name, int score)
    {
        using var connection = new SqliteConnection($"Data Source={selfTestDbPath};Pooling=False");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO runs (player_name, score, distance_m, bolts, duration_s, played_at)
            VALUES ($name, $score, 10, 0, 5.0, '2000-01-01 12:00:00');
            """;
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$score", score);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Lets Bolt-E crash again and again (he never dodges) and checks every rebuild finishes, until game over.</summary>
    async Task CrashUntilGameOver(int spares)
    {
        spareBatteries = spares;
        int rebuilds = 0;
        bool stuck = false;
        while (state != GameState.GameOver && !stuck)
        {
            if (!await WaitUntil(() => state != GameState.Playing, 90)) stuck = true;
            else if (state == GameState.Rebuilding)
            {
                rebuilds++;
                if (!await WaitUntil(() => state == GameState.Playing, 5)) stuck = true;
            }
        }
        Check(!stuck, "every crash ends in a rebuild or a game over (nothing gets stuck)");
        Check(rebuilds == spares, $"Bolt-E was rebuilt {spares} times, once for each spare");
        Check(state == GameState.GameOver && batteriesUsed == spares, "after the last spare, a crash is game over");

        await Wait(2);
        Check(Engine.TimeScale == 1, "time is back to normal speed");
        Check(hud.ResultsShowing && (hud.ResultsTitle is "Out of batteries!" or "NEW HIGH SCORE!"), "the results card is showing");
        Check(AskDatabase(selfTestDbPath, "SELECT batteries_used FROM runs WHERE player_name = $name COLLATE NOCASE ORDER BY id DESC LIMIT 1;",
                          ("$name", hud.PlayerName)) == spares, $"the saved game says {spares} batteries were used");
    }

    // ---------- Little helpers for the tests ----------

    /// <summary>Waits for the next frame.</summary>
    async Task NextFrame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

    /// <summary>Waits this many seconds. (Real seconds: slow motion doesn't make the wait longer.)</summary>
    async Task Wait(double seconds) =>
        await ToSignal(GetTree().CreateTimer(seconds, true, false, true), SceneTreeTimer.SignalName.Timeout);

    /// <summary>Waits until "done" is true, but at most maxSeconds. Returns false if it never happened.</summary>
    async Task<bool> WaitUntil(Func<bool> done, double maxSeconds)
    {
        var timeUp = GetTree().CreateTimer(maxSeconds, true, false, true);
        while (!done())
        {
            if (timeUp.TimeLeft <= 0) return false;
            await NextFrame();
        }
        return true;
    }

    /// <summary>Watches Bolt-E's size for some frames and lists every size he shows, in order (starting with the size he has now).</summary>
    async Task<List<float>> WatchSizes(int frames)
    {
        var sizes = new List<float> { robot.Size };
        for (int i = 0; i < frames; i++)
        {
            await NextFrame();
            if (!Mathf.IsEqualApprox(robot.Size, sizes[^1])) sizes.Add(robot.Size);
        }
        return sizes;
    }

    /// <summary>True if the list of sizes ends with exactly these steps.</summary>
    static bool EndsWithSizes(List<float> sizes, params float[] steps) =>
        sizes.Count >= steps.Length && steps.Select((size, i) => Mathf.IsEqualApprox(sizes[sizes.Count - steps.Length + i], size)).All(same => same);

    /// <summary>A list of sizes as words, like "1.0, 1.3, 1.1".</summary>
    static string SizesText(List<float> sizes) => string.Join(", ", sizes.Select(size => size.ToString("0.0")));

    /// <summary>Prints PASS or FAIL for one check, and counts it.</summary>
    void Check(bool ok, string what)
    {
        if (ok) passedChecks++;
        else failedChecks++;
        GD.Print($"{(ok ? "PASS" : "FAIL")}: {currentTest} - {what}");
    }

    /// <summary>No obstacles, enemies, bosses, ? boxes or flags on the road, so nothing can surprise a test.</summary>
    void QuietRoad()
    {
        spawnCountdown = 1e9f;
        nextBossAt = 1e9f;
        foreach (var box in boxes) box.QueueFree();
        boxes.Clear();
        nextBoxAt = 1e9f;
        ClearFlags();
    }

    /// <summary>Picks who's playing on the title screen (quietly: it doesn't load them yet). (Test names start with T.)</summary>
    void PlayAs(string name) => hud.SetPlayers(database?.KnownPlayers(KnownPlayersShown) ?? new(), name);

    /// <summary>
    /// A new game with nothing on the road, and Big Rusty flies in right away (in world number "world": 1 = Candy Land).
    /// Nothing in the fight can crash Bolt-E, unless a test lets it (robot.BlinkTime = 0).
    /// </summary>
    Boss StartBossFight(string name, int world = 0)
    {
        PlayAs(name);
        StartGame();
        QuietRoad();
        if (world > 0) ChangeWorld(world);
        robot.BlinkTime = 1e9f;
        SpawnBoss();
        return boss!;
    }

    /// <summary>
    /// A test player name with no games and no saved looks in this database yet: the name itself, or with a number on
    /// the end ("TCOW2") if the self-test already ran on this database before.
    /// </summary>
    string FreshName(string name)
    {
        for (int number = 1; ; number++)
        {
            string tryName = number == 1 ? name : $"{name}{number}";
            string key = PlayerKey(tryName);
            if (database is null || (database.StatsFor(tryName).GamesPlayed == 0 &&
                                     database.GetSetting("look:" + key) is null && database.GetSetting("looks_seen:" + key) is null))
                return tryName;
        }
    }

    /// <summary>Saves a pretend old game for a test player straight into the database (score 1, so it's never in Top Scores).</summary>
    void SeedRun(string name, int bolts, int bosses = 0, int world = 1) =>
        database?.SaveRun(name, 1, 10, bolts, 0, bosses, 5.0, 0, world);

    /// <summary>The looks the loaded player has won, like "classic party".</summary>
    string UnlockedIds() => string.Join(" ", Looks.All.Where(Unlocked).Select(look => look.Id));

    /// <summary>The flag on the road with these words (like "YOUR RECORD"), or null if it isn't on the road.</summary>
    RecordFlag? FlagCalled(string label) => flags.FirstOrDefault(flag => flag.Label == label);

    /// <summary>The words on somebody's flag: their name, short and in big letters ("TFDAD").</summary>
    static string FlagLabel(string name) => Hud.ShortName(name, 10).ToUpperInvariant();

    /// <summary>Pretend Top Scores, all with the same name and score.</summary>
    static List<RunRecord> FakeRuns(int count, string name, int score) =>
        Enumerable.Range(1, count).Select(i => new RunRecord(i, name, score, 9999, 999, "2026-01-01 12:00:00")).ToList();

    /// <summary>Pretend "Family Champions", all with the same name and best score.</summary>
    static List<PlayerBest> FakeFamily(int count, string name, int score) =>
        Enumerable.Range(1, count).Select(_ => new PlayerBest(name, score, 9999)).ToList();

    /// <summary>The names of the columns in a table of a database file. (It opens the file read-only.)</summary>
    static List<string> ColumnNames(string dbPath, string table)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM pragma_table_info($table);";
        cmd.Parameters.AddWithValue("$table", table);
        var names = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) names.Add(reader.GetString(0));
        return names;
    }

    /// <summary>Asks a database file one question that has a number for an answer. (It opens the file read-only.)</summary>
    static long AskDatabase(string dbPath, string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    // ---- Pretend input: it comes from device 99, the only device the game listens to during the self-test ----

    /// <summary>Presses an action like "jump" or "start" and keeps holding it (see Release).</summary>
    async Task Press(string action) => await SendInput(new InputEventAction { Action = action, Pressed = true });

    /// <summary>Lets go of an action.</summary>
    async Task Release(string action) => await SendInput(new InputEventAction { Action = action, Pressed = false });

    /// <summary>Taps a controller button (down, then up).</summary>
    async Task PressButton(JoyButton button)
    {
        await SendInput(new InputEventJoypadButton { ButtonIndex = button, Pressed = true });
        await SendInput(new InputEventJoypadButton { ButtonIndex = button, Pressed = false });
    }

    /// <summary>Pushes the left stick left (-1) or right (1), or lets it go back to the middle (0).</summary>
    async Task StickX(float value) => await SendInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = value });

    /// <summary>Taps a key on the keyboard (down, then up).</summary>
    async Task TapKey(Key key)
    {
        await SendInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        await SendInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
    }

    /// <summary>
    /// Taps a key like the REAL keyboard does: Godot calls the keyboard device 16, and the name box only stops typing
    /// for an Escape or Enter from there. (The game itself still ignores it: during the self-test it only listens to 99.)
    /// </summary>
    async Task TapRealKey(Key key)
    {
        const int RealKeyboard = 16;
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true, Device = RealKeyboard });
        await Wait(0.05);
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false, Device = RealKeyboard });
        await Wait(0.05);
    }

    /// <summary>Types one letter on the keyboard, like "E" (down, then up).</summary>
    async Task TypeLetter(char letter)
    {
        var key = (Key)char.ToUpperInvariant(letter); // (the keys A to Z have the same numbers as the letters)
        await SendInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = letter, Pressed = true });
        await SendInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = letter, Pressed = false });
    }

    /// <summary>
    /// Clicks the left mouse button at a spot on the game's 1280 x 720 screen (down, then up).
    /// (A click comes from the window, and the window can be a different size than the game, so the spot is turned into
    /// a spot in the window first. Headless, the window is tiny!)
    /// </summary>
    async Task Click(Vector2 at)
    {
        var inWindow = GetViewport().GetFinalTransform() * at;
        await SendInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = inWindow, GlobalPosition = inWindow });
        await SendInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = inWindow, GlobalPosition = inWindow });
    }

    /// <summary>Sends one pretend input from device 99. It reaches _UnhandledInput by the next frame.</summary>
    async Task SendInput(InputEvent input)
    {
        input.Device = SelfTestDevice;
        Input.ParseInputEvent(input);
        await Wait(0.05);
    }
}

/// <summary>
/// Counts every error the engine reports while the self-test runs (even ones inside _Draw or _Process),
/// so a test that causes an error fails. Godot calls this from other threads too, so it only does a safe, simple count.
/// </summary>
public partial class SelfTestLogger : Logger
{
    public static int Errors;   // errors (warnings don't count)
    public static int Lines;    // printed lines (only used to check that this counter is listening)

    public override void _LogError(string function, string file, int line, string code, string rationale,
                                   bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType != (int)ErrorType.Warning) Interlocked.Increment(ref Errors);
    }

    public override void _LogMessage(string message, bool error) => Interlocked.Increment(ref Lines);
}
