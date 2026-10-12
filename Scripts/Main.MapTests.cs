using Godot;
using Microsoft.Data.Sqlite;

/// <summary>
/// Grown-up testing code for THE WORLD MAP (more of the self-test: see Main.SelfTest.cs).
/// To run only these checks (and the old World Tour and menu tests next to the map's hooks), add --selftest-map after
/// --selftest. The older tests never see the map (see testWorldMap in Main.WorldMap.cs): only these tests turn it on.
/// </summary>
public partial class Main
{
    /// <summary>The tests that --selftest-map runs (and the old World Tour and menu tests next to the map's hooks).</summary>
    static readonly string[] MapTestNames =
    {
        nameof(MapClearsSaved), nameof(MapAfterBossWin), nameof(MapGoButtons), nameof(StartMapPicksAWorld),
        nameof(MapThroughSpace), nameof(MapWaitsForSafety), nameof(MapNeverAfterGameOver), nameof(MapNeverStuck),
        nameof(MapBeatsEveryBoss), nameof(MapDrawsEverywhere), nameof(MapMusicAndSounds),
        nameof(WorldChangeCancelledByNewRun), nameof(BeatRustyNextWorld), nameof(NoWarpInBossFight), nameof(StartWorldPicker),
        nameof(VolcanoPickable), nameof(StationBoss), nameof(VolcanoBoss), nameof(PauseSafety), nameof(DpadUpStaysOnMenu),
        nameof(NoDatabase),
    };

    /// <summary>A map test starts: the WORLD MAP is on (only for this test), and so is the music.</summary>
    void MapTestStart()
    {
        testWorldMap = true;
        sounds.SetMusicOn(true);
    }

    /// <summary>A boss win, without a fight: this world is CLEARED, and the next world is on its way.</summary>
    void PretendBossBeaten()
    {
        RecordWorldCleared(WorldHere);
        NextWorldSoon();
    }

    /// <summary>Pretends a player CLEARED a world in an earlier game (straight into the database).</summary>
    void SeedClear(string name, int world) => database?.SaveWorldCleared(name, world);

    /// <summary>Waits until the map's show gets to this step (at most "max" seconds).</summary>
    Task<bool> MapReaches(MapStep step, double max) => WaitUntil(() => worldMap.Step == step, max);

    /// <summary>The worlds in a list, like "0 2 3 5 6".</summary>
    static string Worldlist(IEnumerable<int> worlds) => string.Join(" ", worlds);

    /// <summary>The start worlds BEFORE the WORLD MAP existed (Sunny Hills, the worlds reached, the Station and the Volcano).</summary>
    static List<int> OldStartWorlds(int farthest)
    {
        var allowed = new List<int>();
        for (int world = 0; world < Math.Min(farthest, Worlds.All.Length); world++) allowed.Add(world);
        if (!allowed.Contains(Worlds.StationIndex)) allowed.Add(Worlds.StationIndex);
        if (!allowed.Contains(Worlds.VolcanoIndex)) allowed.Add(Worlds.VolcanoIndex);
        return allowed;
    }

    // ---------- 1. Saving the CLEARED worlds ----------

    /// <summary>
    /// 1. CLEARED worlds are saved: an old database gets the new world_clears table, a clear is saved once ("Sam" and "sam"
    /// are the same player), old games count (only the first five worlds), every old player's start worlds are exactly the
    /// same as before, and a CLEARED world opens the next one (on the title screen, on the map and in WORLD WARP).
    /// FreshName skips names that have CLEARED a world. A database that breaks just as the start map opens never leaves the
    /// game frozen (this test's one warning, "Could not ask whether the map was seen", is on purpose). And it all works
    /// without a database, both maps too.
    /// </summary>
    async Task MapClearsSaved()
    {
        try
        {
            MapTestStart();
            await NextFrame();

            // An old database (from before the map) gets the new table when it opens, and keeps its games
            string oldPath = Path.Combine(Path.GetDirectoryName(selfTestDbPath)!, "old-map.db");
            foreach (string file in new[] { oldPath, oldPath + "-journal", oldPath + "-wal", oldPath + "-shm" })
                if (File.Exists(file)) File.Delete(file);
            using (var old = new SqliteConnection($"Data Source={oldPath};Pooling=False"))
            {
                old.Open();
                using var cmd = old.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE runs (id INTEGER PRIMARY KEY AUTOINCREMENT, player_name TEXT NOT NULL, score INTEGER NOT NULL,
                                       distance_m INTEGER NOT NULL, bolts INTEGER NOT NULL, duration_s REAL NOT NULL,
                                       played_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')));
                    CREATE TABLE settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
                    INSERT INTO runs (player_name, score, distance_m, bolts, duration_s) VALUES ('TMapOldTimer', 456, 300, 20, 40.0);
                    """;
                cmd.ExecuteNonQuery();
            }
            using (var db = new ScoreDatabase(oldPath))
            {
                Check(AskDatabase(oldPath, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'world_clears';") == 1 &&
                      db.BestScore() == 456 && db.WorldsCleared("TMapOldTimer").Count == 0,
                      "an old database opens, keeps its games, and gets the world_clears table (with nothing CLEARED yet)");
                bool first = db.SaveWorldCleared("TMapSam", 2), again = db.SaveWorldCleared("tmapsam", 2);
                Check(first && !again && Worldlist(db.WorldsCleared("TMAPSAM")) == "2" &&
                      AskDatabase(oldPath, "SELECT COUNT(*) FROM world_clears;") == 1,
                      "a CLEARED world is saved once ('TMapSam' and 'tmapsam' are the same player)");
            }

            // Old games count: worlds 0 up to 2 before the farthest, but only the first five worlds
            var backfill = Enumerable.Range(1, 8).Select(farthest => Worldlist(OldGamesCleared(farthest))).ToArray();
            Check(string.Join(" | ", backfill) == " | 0 | 0 1 | 0 1 2 | 0 1 2 3 | 0 1 2 3 4 | 0 1 2 3 4 | 0 1 2 3 4",
                  $"old games count as CLEARED up to THE MOON ({string.Join(" | ", backfill)})");

            // Every old player (who reached 1 to 8 worlds) can start in exactly the same worlds as before, in the same order
            var different = new List<string>();
            for (int farthest = 1; farthest <= 8; farthest++)
            {
                string player = FreshName($"TMAPF{farthest}");
                if (farthest > 1) SeedRun(player, bolts: 0, world: farthest);
                hud.SetPlayerName(player);
                if (Worldlist(AllowedStartWorlds()) != Worldlist(OldStartWorlds(farthest)))
                    different.Add($"{farthest}: {Worldlist(AllowedStartWorlds())}");
            }
            Check(different.Count == 0, $"every old player's start worlds are just like before{(different.Count > 0 ? ": " + string.Join("; ", different) : "")}");

            // A new player who CLEARED NIGHT CITY (in a game started there) may start in NIGHT CITY and SNOWY PEAKS too
            string night = FreshName("TMAPNIGHT");
            SeedClear(night, 2);
            hud.SetPlayerName(night);
            Check(Worldlist(AllowedStartWorlds()) == "0 2 3 5 6" && Worldlist(WarpWorlds().OrderBy(world => world)) == "0 2 3 5 6",
                  $"a CLEARED world opens the next one: start worlds {Worldlist(AllowedStartWorlds())}, WORLD WARP follows");

            // FreshName skips a name that CLEARED a world (in the database, or today), or whose map was seen
            string only = FreshName("TMAPONLY");
            SeedClear(only, 1);
            string seen = FreshName("TMAPSEEN");
            database?.SetSetting(MapSeenKey(PlayerKey(seen)), "1");
            string today = FreshName("TMAPTODAY");
            ForPlayer(clearedThisSession, PlayerKey(today)).Add(3);
            Check(FreshName("TMAPONLY") != only && FreshName("TMAPSEEN") != seen && FreshName("TMAPTODAY") != today,
                  $"FreshName skips names with a CLEARED world or a seen map ({FreshName("TMAPONLY")}, {FreshName("TMAPSEEN")}, {FreshName("TMAPTODAY")})");

            // A database that breaks just as the start map opens never leaves the game frozen
            var real = database;
            var broken = new ScoreDatabase(":memory:");
            broken.Dispose(); // (a database that can't answer any more, like a file that went bad)
            string oldTimer = FreshName("TMAPBROKEN"), tryingOn = FreshName("TMAPTRYON");
            SeedRun(oldTimer, bolts: 0, world: 3); // (old games: the start map asks the database if their party was seen)
            hud.SetPlayerName(oldTimer);
            database = broken;
            try
            {
                PlayPressed();
                Check(mapOpen && GetTree().Paused && worldMap.Visit.Parties.Count == 0 && worldMap.Visit.OldGames.Count == 0,
                      "the database breaks as the start map opens: the map opens anyway (the old games' party waits for next time)");
            }
            finally
            {
                database = real;
            }
            await TapKey(Key.Escape);
            hud.SetPlayerName(tryingOn);
            lookIndex = Enumerable.Range(0, Looks.All.Length).First(look => !Unlocked(Looks.All[look])); // (trying on a look he hasn't won)
            bool threw = false;
            database = broken;
            try
            {
                PlayPressed(); // (the map asks the database which look he really wears)
            }
            catch (Exception)
            {
                threw = true;
            }
            finally
            {
                database = real;
            }
            Check(threw && !mapOpen && !GetTree().Paused && worldMap.Step == MapStep.Hidden && !sounds.MapMusicPlaying && MenuInputOn,
                  "...and if it breaks while the map gets ready, there's no map and the menu still works (never frozen)");
            PutOn(SavedLookIndex());

            // No database: the clears live for the session, and nothing breaks
            string nobody = FreshName("TMAPNODB");
            database = null;
            try
            {
                hud.SetPlayerName(nobody);
                Check(clearedWorlds.Count == 0 && Worldlist(AllowedStartWorlds()) == "0 5 6", "no database: nothing CLEARED, the usual three start worlds");
                RecordWorldCleared(2);
                SaveNewClears(nobody);
                hud.SetPlayerName(night);
                hud.SetPlayerName(nobody);
                Check(clearedWorlds.Contains(2) && Worldlist(AllowedStartWorlds()) == "0 2 3 5 6",
                      "...a CLEARED world is remembered for the session, and opens the next one");

                // ...and both maps work: NIGHT CITY's flag goes up on the start map, and a boss win rides on into CANDY LAND
                PlayPressed();
                worldMap.SkipShow();
                Check(mapOpen && worldMap.Visit.Parties.SequenceEqual(new[] { 2 }) && worldMap.FlagIsUp(2),
                      "no database: the start map opens, with NIGHT CITY's flag");
                await TapKey(Key.Escape);
                await StartQuietGame(nobody);
                PretendBossBeaten();
                bool opened = await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5);
                worldMap.SkipShow();
                await PressButton(JoyButton.A);
                Check(opened && await WaitUntil(() => !mapOpen && worldNumber == 1, 1.5) && clearedWorlds.Contains(0) && clearedWorlds.Contains(2),
                      "...a boss win opens the map between worlds, and GO rides into CANDY LAND (SUNNY HILLS is CLEARED for the session)");
            }
            finally
            {
                database = real;
                GoToTitle();
            }
        }
        finally
        {
            testWorldMap = false;
        }
    }

    // ---------- 2. Between worlds ----------

    /// <summary>
    /// 2. Beating a boss opens the WORLD MAP: SUNNY HILLS gets its flag, the road to CANDY LAND pops up, the cloud puffs
    /// away, and little Bolt-E travels there. Dad's controller does nothing, a press hurries the show, and after the GO
    /// bubble pops, Y closes the eye over little Bolt-E (even up in his hop) and rides into CANDY LAND right away (the same
    /// score, meters, bolts, batteries and family flags), on an empty road.
    /// </summary>
    async Task MapAfterBossWin()
    {
        try
        {
            MapTestStart();
            string name = FreshName("TMAPWIN");
            await StartQuietGame(name);
            bestScore = 1_000_000; // (so a NEW HIGH SCORE! banner can't cover the victory words)
            SpawnBoss();
            var rusty = (Boss)boss!;
            await Wait(1);
            int flagSounds = sounds.TimesPlayed(Sfx.MapFlag), jingles = sounds.TimesPlayed(Sfx.NewWorld), irises = worldMap.Iris.IrisDrawn;
            rusty.LoseHealth(BossBase.MaxHealth);
            Check(await WaitUntil(() => rusty.HasBlownUp, 3), "he blows up");
            spawnCountdown = 0; // (so the empty road after GO can't come from the quiet road)
            Check(clearedWorlds.Contains(0) && newWorldComing, "SUNNY HILLS is CLEARED the moment he blows up");
            await Wait(1);
            Check(!mapOpen && worldNumber == 0 && hud.BannerText == $"YOU BEAT {rusty.DisplayName}!", "a second later: the victory words, no map yet");
            Check(obstacles.Count == 0 && boxes.Count == 0 && enemies.Count == 0, "...and nothing new comes on the road while the map is on its way");

            Check(await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5), "then the WORLD MAP opens");
            int metersThen = Meters, scoreThen = Score, boltsThen = boltsCollected, sparesThen = spareBatteries;
            var flag = new RecordFlag { Label = "TMAPFLAG", Meters = metersThen + 20, Kind = FlagKind.Other, FlagColor = Colors.Orange };
            flag.Position = new Vector2(RobotX + flag.Meters * PixelsPerMeter - distance, GroundY);
            AddChild(flag);
            flags.Add(flag); // (somebody's flag on the road ahead: it's still there after GO)
            Check(worldMap.Mode == MapMode.Between && worldMap.Here == 0 && worldMap.GoingTo == 1 && worldMap.CloudOn(1) &&
                  worldMap.StateShown(1) == MapNodeState.Locked, "between worlds: from SUNNY HILLS to CANDY LAND (still under a cloud with a '?')");
            Check(worldMap.Visit.Score == scoreThen && worldMap.Visit.Bolts == boltsThen && worldMap.Visit.Spares == sparesThen,
                  $"the corner shows this game's score, bolts and batteries ({scoreThen}, {boltsThen}, {sparesThen})");
            Check(database!.WorldsCleared(name).Contains(0), "SUNNY HILLS is saved as CLEARED by the time the map is open");
            await Wait(0.5);
            Check(GetTree().Paused && Meters == metersThen && sounds.MusicPausedNow && sounds.MapMusicPlaying && !hud.BannerShowing,
                  "the game is frozen, its music waits, and the map plays its own tune");

            Check(await MapReaches(MapStep.FlagUp, 2) && worldMap.BigWords == "SUNNY HILLS CLEAR!" && sounds.TimesPlayed(Sfx.MapFlag) == flagSounds + 1,
                  $"FLAG UP: '{worldMap.BigWords}'");
            await SecondPadButton(JoyButton.A);
            await SecondPadButton(JoyButton.Start);
            await SecondPadButton(JoyButton.DpadRight);
            await SecondPadButton(JoyButton.B); // (B, X and Y are GO too, but only on Bolt-E's controller)
            await SecondPadButton(JoyButton.X);
            await SecondPadButton(JoyButton.Y);
            Check(mapOpen && !worldMap.Hurried, "Dad's controller does nothing on the map (not A, B, X, Y or START)");
            await PressButton(JoyButton.A);
            Check(worldMap.Hurried && mapOpen, "A press hurries the show (it never skips it)");
            Check(await MapReaches(MapStep.Ready, 3) && worldMap.HeroOn == 1 && worldMap.DotsShown(0) == WorldMapArt.DotCount(0) &&
                  !worldMap.CloudOn(1) && worldMap.StateShown(1) == MapNodeState.Open && worldMap.FlagIsUp(0) && worldMap.PartiesLeft == 0,
                  "the flag, the road, the cloud gone: little Bolt-E is on CANDY LAND");
            Check(worldMap.TeaserShowing && worldMap.TeaserBoss == BossKind.BigRusty && worldMap.TeaserOutfit == BossOutfit.PartyHat &&
                  worldMap.GoBubbleShowing && worldMap.NameLine == "CANDY LAND" && worldMap.BigWords == "",
                  $"READY: the GO bubble, and BIG RUSTY waits in a party hat ('{worldMap.NameLine}')");
            Check(!(partiesWaiting.GetValueOrDefault(PlayerKey(name))?.Any() ?? false), "SUNNY HILLS' party happened (it isn't waiting any more)");

            await PressButton(JoyButton.Y);
            Check(mapOpen && worldMap.Step == MapStep.Ready, "a press right when the GO bubble pops is too soon (it waits a moment)");
            await Wait(0.4);
            await PressButton(JoyButton.Y);
            var hero = worldMap.Hero;
            float heroMiddleY = hero is null ? 0 : hero.GlobalPosition.Y - 60 * WorldMap.HeroSize;
            Check(worldMap.Step == MapStep.GoIris && hero is { OnGround: false } && worldMap.Iris.ZIndex > worldMap.HeroZ &&
                  Mathf.Abs(worldMap.Iris.Center.Y - heroMiddleY) < 15,
                  $"GO: little Bolt-E hops, and the eye closes over him, round his middle up in the air (y {worldMap.Iris.Center.Y:0} / {heroMiddleY:0})");
            Check(await WaitUntil(() => !mapOpen && state == GameState.Playing && worldNumber == 1, 1.5), "Y: into CANDY LAND");
            Check(worldBlend == 1 && hud.BannerText == "WELCOME TO CANDY LAND!" && sounds.TimesPlayed(Sfx.NewWorld) == jingles + 1 &&
                  worldsReached == 2 && !GetTree().Paused && !newWorldComing, $"right away: '{hud.BannerText}' (one jingle)");
            Check(boss is null && spawnCountdown >= MapSafeGap - 30 && speed <= StartSpeed && Score >= scoreThen && Meters >= metersThen &&
                  boltsCollected >= boltsThen && spareBatteries == sparesThen && flags.Contains(flag) && IsInstanceValid(flag),
                  $"the run goes on: the same score, meters, bolts, batteries and flags, an empty road ({spawnCountdown:0} px), a gentle start ({speed:0})");
            await Wait(1);
            Check(LooksLike(Worlds.All[1]) && sounds.MusicPlaying && !sounds.MapMusicPlaying && Mathf.Abs(sounds.MusicPitch - 1.05f) < 0.01f,
                  $"CANDY LAND's colors and its music speed ({sounds.MusicPitch:0.00})");
            Check(worldMap.Iris.IrisDrawn > irises && worldMap.Iris.Closed == 0 && worldMap.Step == MapStep.Hidden && worldMap.Hero is null,
                  "the eye closed and opened again, and the map is all done");
        }
        finally
        {
            testWorldMap = false;
        }
    }

    // ---------- 3. The buttons ----------

    /// <summary>
    /// 3. GO is A, B, X, Y, START, Enter, the keypad's Enter and Space (and a click on the GO bubble), and nothing else:
    /// never the D-pad, LB, RB, BACK, other clicks, P or Esc, and never a let-go or a held key. Each GO button opens the
    /// start map on the title screen, and the same button GOes. On the map Dad's controller, LB / RB and M do nothing.
    /// Between worlds START is GO (it never pauses), and P and Esc do nothing.
    /// </summary>
    async Task MapGoButtons()
    {
        try
        {
            MapTestStart();
            await NextFrame();

            // (a) Which presses are GO (and which are BACK)
            InputEvent Pad(JoyButton button, bool pressed = true) =>
                new InputEventJoypadButton { ButtonIndex = button, Pressed = pressed, Device = SelfTestDevice };
            InputEvent KeyDown(Key key, bool echo = false) =>
                new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true, Echo = echo, Device = SelfTestDevice };
            var goes = new (string Name, InputEvent Press)[]
            {
                ("A", Pad(JoyButton.A)), ("B", Pad(JoyButton.B)), ("X", Pad(JoyButton.X)), ("Y", Pad(JoyButton.Y)),
                ("START", Pad(JoyButton.Start)), ("Enter", KeyDown(Key.Enter)), ("KpEnter", KeyDown(Key.KpEnter)), ("Space", KeyDown(Key.Space)),
            };
            var never = new (string Name, InputEvent Press)[]
            {
                ("D-pad up", Pad(JoyButton.DpadUp)), ("D-pad down", Pad(JoyButton.DpadDown)), ("D-pad left", Pad(JoyButton.DpadLeft)),
                ("D-pad right", Pad(JoyButton.DpadRight)), ("LB", Pad(JoyButton.LeftShoulder)), ("RB", Pad(JoyButton.RightShoulder)),
                ("BACK", Pad(JoyButton.Back)), ("a click", new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Device = SelfTestDevice }),
                ("P", KeyDown(Key.P)), ("Esc", KeyDown(Key.Escape)), ("the Up arrow", KeyDown(Key.Up)), ("A let go", Pad(JoyButton.A, pressed: false)),
                ("a held Space", KeyDown(Key.Space, echo: true)),
            };
            var wrong = goes.Where(go => !IsMapGo(go.Press)).Select(go => go.Name + " isn't GO")
                .Concat(never.Where(no => IsMapGo(no.Press)).Select(no => no.Name + " is GO")).ToList();
            Check(wrong.Count == 0, $"A, B, X, Y, START, Enter, the keypad's Enter and Space are GO, and nothing else is{(wrong.Count > 0 ? ": " + string.Join(", ", wrong) : "")}");
            Check(IsMapBack(KeyDown(Key.Escape)) && IsMapBack(Pad(JoyButton.Back)) && !IsMapBack(Pad(JoyButton.B)) && !IsMapBack(KeyDown(Key.P)),
                  "BACK and Esc are the way back to the menu");

            // (b) On the title screen every GO button opens the start map, and the same button GOes
            string name = FreshName("TMAPGO");
            database?.SetSetting("player_name", name);
            var presses = new (string Name, Func<Task> Press)[]
            {
                ("A", () => PressButton(JoyButton.A)), ("B", () => PressButton(JoyButton.B)), ("X", () => PressButton(JoyButton.X)),
                ("Y", () => PressButton(JoyButton.Y)), ("START", () => PressButton(JoyButton.Start)), ("Enter", () => TapKey(Key.Enter)),
                ("Space", () => TapKey(Key.Space)),
            };
            var broken = new List<string>();
            foreach (var (label, press) in presses)
            {
                GoToTitle();
                await NextFrame();
                await press();
                bool opened = mapOpen && state == GameState.Title && GetTree().Paused;
                worldMap.SkipShow(); // (straight to READY: the GO wait is over)
                await press();
                bool started = await WaitUntil(() => state == GameState.Playing && !mapOpen, 1.5) && !GetTree().Paused;
                if (!opened || !started) broken.Add($"{label} (opened {opened}, started {started})");
            }
            Check(broken.Count == 0, $"A, B, X, Y, START, Enter and Space each open the start map on the title screen, and GO{(broken.Count > 0 ? ": " + string.Join(", ", broken) : "")}");

            // (c) One visit: too soon, D-pad UP, Dad's controller, LB / RB, M, and clicks
            GoToTitle();
            await NextFrame();
            await PressButton(JoyButton.A);
            Check(await MapReaches(MapStep.Ready, 2) && state == GameState.Title, "A opens the start map");
            await PressButton(JoyButton.A);
            Check(mapOpen && worldMap.Step == MapStep.Ready, "A right when the GO bubble pops is too soon: the map stays");
            await PressButton(JoyButton.DpadUp);
            Check(mapOpen && worldMap.Steps == 1 && worldMap.Here == Worlds.StationIndex && state == GameState.Title,
                  $"D-pad UP walks up to the Station (it never GOes) (world {worldMap.Here})");
            await SecondPadButton(JoyButton.A);
            await SecondPadButton(JoyButton.Start);
            await SecondPadButton(JoyButton.B); // (B, X and Y are GO, and BACK goes back to the menu: only on Bolt-E's controller)
            await SecondPadButton(JoyButton.X);
            await SecondPadButton(JoyButton.Y);
            await SecondPadButton(JoyButton.Back);
            await SecondPadButton(JoyButton.DpadLeft);
            await SecondPadStick(0.9f);
            await SecondPadStick(0);
            Check(mapOpen && worldMap.Step is MapStep.Ready or MapStep.Stepping && worldMap.Steps == 1 && worldMap.Here == Worlds.StationIndex &&
                  state == GameState.Title, "Dad's controller does nothing on the map (no GO, no BACK, no steps)");
            string player = hud.PlayerName;
            bool music = sounds.MusicOn;
            await PressButton(JoyButton.LeftShoulder);
            await PressButton(JoyButton.RightShoulder);
            await TapKey(Key.Tab);
            await TapKey(Key.M);
            Check(mapOpen && hud.PlayerName == player && sounds.MusicOn == music, "LB, RB, TAB and M do nothing on the map");
            await Click(new Vector2(640, 400));
            Check(mapOpen && state == GameState.Title, "a click on the map (not on the GO bubble) does nothing");
            Check(await WaitUntil(() => worldMap.CanGoNow, 1), "a moment later GO works");
            await Click(WorldMapArt.BubbleRect(worldMap.HeroOn).GetCenter());
            Check(await WaitUntil(() => state == GameState.Playing && !mapOpen, 1.5) && WorldHere == Worlds.StationIndex,
                  "a click on the GO bubble is GO: a game in the Station");

            // (d) Between worlds: START is GO (it never pauses), and P and Esc do nothing
            GoToTitle();
            startWorld = 0; // (the walk above picked the Station: this game starts in Sunny Hills again)
            await StartQuietGame(FreshName("TMAPSTART"));
            PretendBossBeaten();
            Check(await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5), "a boss win: the map");
            worldMap.SkipShow();
            await TapKey(Key.P);
            await TapKey(Key.Escape);
            Check(mapOpen && !paused && !pauseWanted && !pauseMenu.Showing, "P and Esc do nothing between worlds (no pause, no way out)");
            await PressButton(JoyButton.Start);
            Check(await WaitUntil(() => !mapOpen && worldNumber == 1, 1.5) && !paused && !pauseMenu.Showing, "START is GO between worlds");
            await Wait(0.3);
            Check(!paused && !GetTree().Paused && state == GameState.Playing, "...and nothing pauses after GO");
        }
        finally
        {
            testWorldMap = false;
            startWorld = 0;
        }
    }

    // ---------- 4. The start map ----------

    /// <summary>
    /// 4. Before a game: a player who reached NIGHT CITY in old games gets "YOUR OLD GAMES COUNT!" on their first map
    /// (SUNNY HILLS' and CANDY LAND's flags go up, only once: after a restart the database remembers it). The D-pad walks
    /// little Bolt-E the way it's pressed on the screen (along the roads, or a STAR HOP), every world can be reached, and
    /// the start world follows every step. GO starts in the picked world (a picked start wins nothing), BACK and Esc go
    /// back to the menu, NEW PLAYER still goes to the name box, and a newly typed name gets its own map.
    /// </summary>
    async Task StartMapPicksAWorld()
    {
        try
        {
            MapTestStart();
            string name = FreshName("TMAPPICK"), key = PlayerKey(name);
            SeedRun(name, bolts: 0, world: 3);
            database?.SetSetting("player_name", name);
            GoToTitle();
            await NextFrame();
            int flags = sounds.TimesPlayed(Sfx.MapFlag);
            await PressButton(JoyButton.Y);
            Check(mapOpen && state == GameState.Title, "Y opens the start map");

            // The first map: YOUR OLD GAMES COUNT!
            Check(await MapReaches(MapStep.FlagUp, 1.5) && worldMap.BigWords == "YOUR OLD GAMES COUNT!" && worldMap.PartiesLeft == 2 &&
                  worldMap.StateShown(0) == MapNodeState.Cleared && worldMap.StateShown(1) == MapNodeState.Open,
                  $"the first map: '{worldMap.BigWords}' and SUNNY HILLS' flag goes up");
            Check(database?.GetSetting(MapSeenKey(key)) is null, "(not remembered yet)");
            await PressButton(JoyButton.A);
            Check(worldMap.Hurried, "A hurries it");
            Check(await WaitUntil(() => worldMap.Step == MapStep.FlagUp && worldMap.BigWords == "CANDY LAND CLEAR!", 2),
                  $"then CANDY LAND's flag: '{worldMap.BigWords}'");
            Check(await MapReaches(MapStep.Ready, 3), "READY");
            var states = Enumerable.Range(0, 7).Select(world => worldMap.StateShown(world)).ToArray();
            Check(Worldlist(Enumerable.Range(0, 7).Where(world => states[world] == MapNodeState.Cleared)) == "0 1" &&
                  Worldlist(Enumerable.Range(0, 7).Where(world => states[world] == MapNodeState.Open)) == "2 5 6" &&
                  Worldlist(Enumerable.Range(0, 7).Where(world => states[world] == MapNodeState.Locked)) == "3 4",
                  "CLEARED: SUNNY HILLS and CANDY LAND; OPEN: NIGHT CITY, the Station and the Volcano; LOCKED: SNOWY PEAKS and THE MOON");
            Check(worldMap.NextArrowOn == 2 && worldMap.HeroOn == 0 && worldMap.NameLine == "SUNNY HILLS" && sounds.TimesPlayed(Sfx.MapFlag) == flags + 2,
                  $"NEXT points at NIGHT CITY, and little Bolt-E stands on '{worldMap.NameLine}'");
            Check(database?.GetSetting(MapSeenKey(key)) == "1" && worldMap.PartiesLeft == 0, "the old games' party is remembered: it only comes once");

            // The D-pad walks the way it's pressed on the screen
            var walked = new List<int>();
            bool follows = true, nope = false;
            foreach (var button in new[] { JoyButton.DpadRight, JoyButton.DpadRight, JoyButton.DpadRight, JoyButton.DpadUp, JoyButton.DpadLeft, JoyButton.DpadDown })
            {
                int before = worldMap.Here;
                await PressButton(button);
                walked.Add(worldMap.Here);
                follows &= startWorld == worldMap.Here && hud.Postcards.Picked == worldMap.Here;
                if (worldMap.Here == before) nope = Mathf.IsEqualApprox(sounds.PitchOf(Sfx.MenuTick), 0.6f);
            }
            Check(Worldlist(walked) == "1 2 2 5 6 0", $"RIGHT, RIGHT, RIGHT, UP, LEFT, DOWN walk to {Worldlist(walked)} (RIGHT from NIGHT CITY goes nowhere)");
            Check(nope && worldMap.Steps == 5 && worldMap.StarHops == 3, $"a low 'nope' tick when there's nowhere to go; 2 glides along roads and 3 STAR HOPS ({worldMap.StarHops})");
            Check(follows, "the start world (and the title's postcard glow) follows every step");
            await StickX(0.9f);
            await StickX(1f);
            await StickX(0);
            Check(worldMap.Here == 1 && worldMap.Steps == 6, "one push of the stick is one step");

            // Every world this player may start in can be reached with the D-pad (for a new player, this one, and a champion)
            var unreachable = new List<string>();
            var profiles = new[] { new List<int> { 0, 5, 6 }, AllowedStartWorlds(), Enumerable.Range(0, 7).ToList() };
            var ways = new[] { Vector2.Right, Vector2.Left, Vector2.Up, Vector2.Down };
            foreach (var allowed in profiles)
            {
                var reached = new HashSet<int> { 0 };
                var todo = new Queue<int>();
                todo.Enqueue(0);
                while (todo.Count > 0)
                {
                    int from = todo.Dequeue();
                    foreach (var way in ways)
                    {
                        int to = MapStepTarget(from, way, allowed);
                        if (to >= 0 && reached.Add(to)) todo.Enqueue(to);
                    }
                }
                if (!allowed.All(reached.Contains)) unreachable.Add($"[{Worldlist(allowed)}] reaches only [{Worldlist(reached.OrderBy(w => w))}]");
            }
            Check(unreachable.Count == 0, $"every allowed world can be reached from SUNNY HILLS with the D-pad{(unreachable.Count > 0 ? ": " + string.Join("; ", unreachable) : "")}");

            // To the Station (LEFT, UP, UP), and Space: GO
            await PressButton(JoyButton.DpadLeft);
            await PressButton(JoyButton.DpadUp);
            await PressButton(JoyButton.DpadUp);
            Check(worldMap.Here == Worlds.StationIndex && startWorld == Worlds.StationIndex, $"LEFT, UP, UP: the Station (world {worldMap.Here})");
            await TapKey(Key.Space);
            Check(await WaitUntil(() => state == GameState.Playing && !mapOpen, 1.5), "Space: GO!");
            QuietRoad();
            Check(WorldHere == Worlds.StationIndex && ceilingOpen == 0 && hud.BannerText == "WELCOME TO UPSIDE-DOWN STATION!" &&
                  !GetTree().Paused && !sounds.MapMusicPlaying && sounds.StationMusicActive,
                  $"a game in UPSIDE-DOWN STATION: the ceiling is shut, '{hud.BannerText}', its own music");
            await Wait(0.3);
            QuitToTitle();
            Check(LastRun(name, "world_reached") == 1, "a picked start wins nothing (1 world saved)");

            // Esc on the start map: back to the title screen, just as it was. (And as if the game had been switched off and
            // on again: now only the database remembers the old games' party, and it doesn't come again.)
            mapSeenToday.Remove(key);
            GoToTitle();
            await NextFrame();
            await PressButton(JoyButton.A);
            Check(mapOpen && worldMap.Visit.Parties.Count == 0 && worldMap.Visit.OldGames.Count == 0,
                  "A: the start map again (after a restart the database remembers: no 'YOUR OLD GAMES COUNT!' this time)");
            await TapKey(Key.Escape);
            Check(!mapOpen && state == GameState.Title && !GetTree().Paused && hud.MenuStripShowing && !hud.ResultsShowing &&
                  !sounds.MapMusicPlaying && !sounds.MusicPausedNow, "Esc: back to the title screen (the music goes on)");

            // From the results card: START opens the map, and BACK gives the card again
            startWorld = 0;
            await StartQuietGame(name);
            LetBoltECrash();
            Check(Bonk(null) && await WaitUntil(() => hud.ResultsShowing, 3), "game over: the results card");
            await Wait(0.4);
            await PressButton(JoyButton.Start);
            Check(mapOpen && state == GameState.GameOver, "START on the results card opens the map");
            await PressButton(JoyButton.Back);
            Check(!mapOpen && hud.ResultsShowing && MenuInputOn && !GetTree().Paused, "BACK: the results card again");

            // NEW PLAYER, then A: the name box (no map)
            for (int i = 0; i < 12 && !hud.NewPlayerPicked; i++) await TapKey(Key.Tab);
            await PressButton(JoyButton.A);
            Check(state == GameState.Title && hud.IsTypingName && !mapOpen, "NEW PLAYER, then A: the title card's name box (no map)");

            // A new name, then Enter: that player's own map (nothing CLEARED)
            string fresh = FreshName("TMAPNEW");
            foreach (char letter in fresh) await TypeLetter(letter);
            await TapRealKey(Key.Enter);
            await NextFrame();
            Check(mapOpen && hud.PlayerName == fresh && worldMap.Visit.ClearedCount == 0 &&
                  Enumerable.Range(0, 7).All(world => worldMap.Visit.After[world] != MapNodeState.Cleared),
                  $"typing '{fresh}' and Enter: {hud.PlayerName}'s own map, with nothing CLEARED");
            await TapKey(Key.Escape);
        }
        finally
        {
            testWorldMap = false;
            startWorld = 0;
            hud.NameBox.ReleaseFocus();
        }
    }

    // ---------- 5. All the way around ----------

    /// <summary>
    /// 5. From THE MOON through space to UPSIDE-DOWN STATION (in a bubble), down by space capsule to VOLCANO ISLAND, and
    /// surfing home: every arrival works (the Station's ceiling and flipping, the Volcano's safe rock), the Station's
    /// ceiling stays open and the Volcano's solid floor stays while the map waits, and the 7th world CLEARED is a big ALL
    /// CLEAR party (the GOLD MAP). Then "ALL THE WAY AROUND!", and the game saves 8 worlds.
    /// </summary>
    async Task MapThroughSpace()
    {
        try
        {
            MapTestStart();
            string name = FreshName("TMAPSPACE");
            foreach (int world in new[] { 0, 1, 2, 3, 5 }) SeedClear(name, world);
            var moonBoss = StartBossFight(name, world: 4);
            bestScore = 1_000_000; // (so a NEW HIGH SCORE! banner can't cover the words)
            Check(Worldlist(clearedWorlds.OrderBy(world => world)) == "0 1 2 3 5", $"this player CLEARED {Worldlist(clearedWorlds.OrderBy(world => world))}");
            await Wait(0.5);
            moonBoss.LoseHealth(BossBase.MaxHealth);
            Check(await WaitUntil(() => moonBoss.HasBlownUp, 3), "THE MOON's boss blows up");
            Check(await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5) && worldMap.GoingTo == Worlds.StationIndex, "the map: on to the Station");
            Check(await MapReaches(MapStep.Travel, 5) && worldMap.RideNow == Ride.Space, $"through space, in a bubble ({worldMap.RideNow})");
            worldMap.SkipShow();
            await PressButton(JoyButton.A);
            Check(await WaitUntil(() => !mapOpen && WorldHere == Worlds.StationIndex, 1.5), "A: into UPSIDE-DOWN STATION");
            Check(ceilingOpen == 0 && robot.GravityScale == 1 && hud.HintText == FlipTip, $"the ceiling is shut at once, normal gravity, and '{hud.HintText}'");
            Check(await WaitUntil(() => robot.FlipMode, 0.5), "flipping is on");
            await Wait(0.7);
            Check(sounds.StationMusicActive && Mathf.Abs(sounds.MusicPitch - 1.1f) < 0.01f, $"Neon Slipstream at the Station's speed ({sounds.MusicPitch:0.00})");

            // The Station's boss. A power keeps the map waiting until long after his pieces are gone: the ceiling stays open
            QuietRoad();
            SpawnBoss();
            var stationBoss = boss!;
            Check(await WaitUntil(() => !robot.FlipMode && ceilingOpen >= 1, 2), "a boss in the Station: GRAVITY ON, the ceiling slides open");
            stationBoss.LoseHealth(BossBase.MaxHealth);
            Check(await WaitUntil(() => stationBoss.HasBlownUp, 3), "the Station's boss blows up");
            StartPowerUp(PowerUp.Mega);
            powerLeft = 1e9f;
            Check(await FastForwardUntil(() => boss is null && noBossSeconds >= CeilingBackSeconds + 0.2f, 9) && mapWantedFor >= 0 && !mapOpen,
                  "his pieces are long gone, and the map waits (MEGA is on)");
            Check(!robot.FlipMode && ceilingOpen > 0.99f && newWorldComing, "...while the map waits, the ceiling stays open (no flipping)");
            StopPowersNow();
            Check(await WaitUntil(() => mapOpen, 1) && worldMap.GoingTo == Worlds.VolcanoIndex, "the power is over: the map");
            Check(await MapReaches(MapStep.Travel, 3) && worldMap.RideNow == Ride.Capsule, $"down to VOLCANO ISLAND by space capsule ({worldMap.RideNow})");
            worldMap.SkipShow();
            await PressButton(JoyButton.A);
            Check(await WaitUntil(() => !mapOpen && WorldHere == Worlds.VolcanoIndex, 1.5), "A: onto VOLCANO ISLAND");
            Check(RockUnder(robot.Position.X) is { Steady: true } && arena is null && robot.CeilingY == Robot.NoCeiling && !robot.FlipMode &&
                  !sounds.StationMusicActive && !sounds.BossMusicActive, "on a safe rock, no boss floor, no ceiling, the runner music");

            // The Volcano's boss: the solid floor stays while the map waits, and the 7th world CLEARED is ALL CLEAR!
            QuietRoad();
            SpawnBoss();
            var volcanoBoss = boss!;
            Check(await WaitUntil(() => arena is not null, 1), "a boss on VOLCANO ISLAND: SOLID GROUND!");
            volcanoBoss.LoseHealth(BossBase.MaxHealth);
            Check(await WaitUntil(() => volcanoBoss.HasBlownUp, 3), "the Volcano's boss blows up");
            int allClears = sounds.TimesPlayed(Sfx.MapAllClear);
            StartPowerUp(PowerUp.Mega);
            powerLeft = 1e9f;
            Check(await FastForwardUntil(() => boss is null && mapWantedFor >= 0, 6) && !mapOpen, "his pieces are gone, and the map waits");
            await NextFrame();
            await NextFrame();
            Check(arena is not null && newWorldComing, "...while it waits, the solid floor stays");
            StopPowersNow();
            Check(await WaitUntil(() => mapOpen, 1) && worldMap.GoingTo == 0, "the map: on to SUNNY HILLS");
            Check(await MapReaches(MapStep.AllClear, 4) && worldMap.BigWords == "ALL 7 WORLDS CLEAR!", $"ALL CLEAR! '{worldMap.BigWords}'");
            Check(await MapReaches(MapStep.Travel, 4) && worldMap.AllClearShown && worldMap.GoldMap && worldMap.RideNow == Ride.Sea &&
                  sounds.TimesPlayed(Sfx.MapAllClear) == allClears + 1, "the GOLD MAP (one fanfare), then surfing home");
            Check(await MapReaches(MapStep.Ready, 2) && worldMap.BigWords == "ALL THE WAY AROUND!", $"on SUNNY HILLS: '{worldMap.BigWords}'");
            await Wait(0.4);
            await PressButton(JoyButton.A);
            Check(await WaitUntil(() => !mapOpen && worldNumber == 7, 1.5), "A: home to SUNNY HILLS");
            Check(hud.BannerText == "YOU WENT ALL THE WAY AROUND!" && worldsReached == 8 && lavaRocks.Count == 0 && arena is null &&
                  robot.GroundY == GroundY, $"'{hud.BannerText}' ({worldsReached} worlds), solid ground again");
            await Wait(0.3);
            QuitToTitle();
            Check(LastRun(name, "world_reached") == 8, $"the game saves 8 worlds ({LastRun(name, "world_reached")})");
        }
        finally
        {
            testWorldMap = false;
            Engine.TimeScale = 1;
        }
    }

    // ---------- 6. Waiting for a safe moment ----------

    /// <summary>
    /// 6. The map only opens at a safe moment: never while Bolt-E is in pieces or in slow motion (and a moment after
    /// "BOLT-E IS BACK!"), never while the game is paused, never in slow motion. In the air it waits up to 2 seconds, then
    /// opens anyway: GO into UPSIDE-DOWN STATION in mid-air slides the ceiling shut (it never shuts over his head).
    /// Unplugging Bolt-E's controller on the map: the keyboard still GOes, and the game pauses right after GO (but not
    /// if that same controller was plugged back in: Dad's controller taking over while it's gone doesn't count).
    /// </summary>
    async Task MapWaitsForSafety()
    {
        try
        {
            MapTestStart();

            // (a) A crash with a spare battery while the map is on its way
            await StartQuietGame(FreshName("TMAPREBUILD"));
            bestScore = 1_000_000; // (so a NEW HIGH SCORE! banner can't cover "BOLT-E IS BACK!")
            PretendBossBeaten();
            await Wait(0.8);
            spareBatteries = 1;
            robot.BlinkTime = 0;
            Check(Bonk(null) && state == GameState.Rebuilding, "a crash with a spare while the map is on its way");
            bool early = false;
            for (int i = 0; i < 600 && state != GameState.Playing; i++)
            {
                early |= mapOpen;
                await NextFrame();
            }
            Check(!early && state == GameState.Playing && mapWantedFor >= 0, "no map while he's in pieces (or in slow motion)");
            ulong back = Engine.GetProcessFrames();
            Check(await WaitUntil(() => mapOpen, 3), "then the map");
            float after = (Engine.GetProcessFrames() - back) / 60f;
            Check(after >= MapAfterRebuildSeconds - 0.05f && Engine.TimeScale == 1 && hud.BannerText == $"{robot.RobotName} IS BACK!",
                  $"...{after:0.00} seconds after '{hud.BannerText}' (so it can be seen), with normal time");
            worldMap.SkipShow();
            await PressButton(JoyButton.A);
            Check(await WaitUntil(() => !mapOpen && worldNumber == 1, 1.5), "GO: CANDY LAND");
            await Wait(1);
            Check(sounds.MusicPlaying && Mathf.Abs(sounds.MusicPitch - 1.05f) < 0.01f, $"its music at its speed ({sounds.MusicPitch:0.00})");

            // (b) High up in the air on THE MOON when the map is due: it waits up to 2 seconds, then opens anyway, and GO
            // into the Station slides the ceiling shut (it never snaps shut over his head)
            GoToTitle();
            await StartQuietGame(FreshName("TMAPAIR"));
            ChangeWorld(4);
            await Wait(0.2);
            PretendBossBeaten();
            await Wait(NewWorldDelay - 0.15f);
            robot.SetGravityScale(0.1f); // (very floaty, so he stays up a long time)
            robot.Jump();
            await Wait(1.2);
            robot.Jump(); // (the double jump: way up high)
            Check(mapWantedFor >= 0 && !mapOpen && !robot.OnGround, "the map is due, but Bolt-E is up in the air: it waits");
            Check(await WaitUntil(() => mapOpen, 1.5) && !robot.OnGround && robot.Position.Y < GroundY - 250,
                  $"...then it opens anyway, with Bolt-E high in the air (his feet at y {robot.Position.Y:0})");
            worldMap.SkipShow();
            await PressButton(JoyButton.A);
            Check(await WaitUntil(() => !mapOpen && WorldHere == Worlds.StationIndex, 1.5) && !robot.OnGround,
                  "GO in mid-air: into UPSIDE-DOWN STATION");
            Check(ceilingOpen > 0.5f, $"the ceiling doesn't snap shut over him ({ceilingOpen:0.00} open)");
            bool headSafe = true;
            for (int i = 0; i < 180 && !robot.OnGround; i++)
            {
                headSafe &= robot.CeilingY <= Robot.NoCeiling + 1 || robot.Position.Y - 130 * robot.Size >= robot.CeilingY - 1;
                await NextFrame();
            }
            Check(headSafe && robot.OnGround, "he lands, and his head is never above the ceiling on the way");
            Check(await WaitUntil(() => robot.FlipMode, 2) && ceilingOpen == 0, "then the ceiling is shut, and flipping is on");

            // (c) Slow motion when the map is due: it waits
            GoToTitle();
            await StartQuietGame(FreshName("TMAPSLOW"));
            PretendBossBeaten();
            try
            {
                await Wait(NewWorldDelay - 0.2f);
                Engine.TimeScale = 0.4;
                await Wait(1);
                Check(mapWantedFor >= 0 && !mapOpen, "in slow motion the map waits");
                Engine.TimeScale = 1;
                Check(await WaitUntil(() => mapOpen, 0.5), "...and opens when time is normal again");
            }
            finally
            {
                Engine.TimeScale = 1;
            }

            // (d) Paused right after the boss: no map while paused
            GoToTitle();
            await StartQuietGame(FreshName("TMAPPAUSE"));
            PretendBossBeaten();
            await Wait(0.3);
            await PressButton(JoyButton.Start);
            Check(paused, "paused right after the boss");
            await Wait(NewWorldDelay + 0.3);
            Check(paused && !mapOpen && mapWantedFor < 0, "no map while the game is paused (its wait stops too)");
            await PressButton(JoyButton.Start);
            Check(!paused && await WaitUntil(() => mapOpen, NewWorldDelay), "back to the game: the map comes when the wait is over");

            // (e) Bolt-E's controller comes unplugged while the map is up
            GoToTitle();
            await StartQuietGame(FreshName("TMAPPLUG"));
            PretendBossBeaten();
            Check(await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5), "a map between worlds");
            try
            {
                testPluggedIn = new List<int>();
                OnControllerPluggedOrUnplugged(SelfTestDevice, false);
                Check(mapOpen && !paused && pauseWanted && !pauseMenu.Showing, "Bolt-E's controller comes unplugged: no pause screen over the map");
                worldMap.SkipShow();
                await TapKey(Key.Enter);
                Check(await WaitUntil(() => !mapOpen && worldNumber == 1, 1.5), "the keyboard's Enter still GOes");
                Check(await WaitUntil(() => paused, 1) && pauseMenu.Showing, "...and the game pauses right after GO (he never rides without his controller)");
                ResumeGame();

                // Plugged back in before GO: START is GO, and no surprise pause after it
                testPluggedIn = null;
                UpdateControllerStatus(); // (his controller is controller one again)
                GoToTitle();
                await StartQuietGame(FreshName("TMAPREPLUG"));
                PretendBossBeaten();
                Check(await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5), "another map between worlds");
                testPluggedIn = new List<int>();
                OnControllerPluggedOrUnplugged(SelfTestDevice, false);
                Check(mapOpen && !paused && pauseWanted && controllerOne != SelfTestDevice, "unplugged again: the pause waits for GO");
                testPluggedIn = new List<int> { SelfTestDevice };
                OnControllerPluggedOrUnplugged(SelfTestDevice, true);
                worldMap.SkipShow();
                await PressButton(JoyButton.Start);
                Check(await WaitUntil(() => !mapOpen && worldNumber == 1, 1.5), "...plugged back in: START is GO");
                await Wait(0.3);
                Check(!paused && !pauseWanted, "...and there's no surprise pause after GO");

                // Dad's controller stays plugged in, and it becomes controller one while Bolt-E's is gone. That isn't
                // Bolt-E's own controller coming back, so the game still pauses right after GO.
                testPluggedIn = null;
                UpdateControllerStatus();
                GoToTitle();
                await StartQuietGame(FreshName("TMAPTWOPADS"));
                PretendBossBeaten();
                Check(await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5), "a third map between worlds");
                testPluggedIn = new List<int> { SelfTestSecondPad };
                OnControllerPluggedOrUnplugged(SelfTestDevice, false);
                Check(mapOpen && pauseWanted && controllerOne == SelfTestSecondPad,
                      "Bolt-E's controller comes unplugged, and Dad's is controller one now");
                worldMap.SkipShow();
                await TapKey(Key.Enter);
                Check(await WaitUntil(() => !mapOpen && worldNumber == 1, 1.5) && await WaitUntil(() => paused, 1) && pauseMenu.Showing,
                      "Enter: GO, and the game still pauses right after it (Dad's controller isn't Bolt-E's own coming back)");
                ResumeGame();
            }
            finally
            {
                testPluggedIn = null;
                UpdateControllerStatus();
            }
        }
        finally
        {
            testWorldMap = false;
            Engine.TimeScale = 1;
        }
    }

    // ---------- 7. Never after a game over ----------

    /// <summary>
    /// 7. No map after a game over: a game over right after a boss saves the CLEARED world anyway, and its flag party waits
    /// for the next start map (it's never lost, and it only happens once). GoToTitle and StartGame close any map (never
    /// frozen, no tune), and QUIT TO TITLE while the map is on its way means no map.
    /// </summary>
    async Task MapNeverAfterGameOver()
    {
        try
        {
            MapTestStart();

            // (a) Game over right after the boss
            string name = FreshName("TMAPOVER");
            await StartQuietGame(name);
            PretendBossBeaten();
            await Wait(1);
            LetBoltECrash();
            Check(Bonk(null) && state == GameState.GameOver, "game over a moment after the boss");
            Check(database!.WorldsCleared(name).Contains(0) && LastRun(name, "world_reached") == 1,
                  "SUNNY HILLS is saved as CLEARED anyway (and the game reached 1 world)");
            await Wait(NewWorldDelay - 0.4f);
            Check(!mapOpen && !newWorldComing && mapWantedFor < 0, "no map after a game over");
            Check(await WaitUntil(() => hud.ResultsShowing, 2), "the results card");
            await Wait(0.4);
            await PressButton(JoyButton.A);
            Check(mapOpen && worldMap.Visit.Parties.Contains(0) && worldMap.PartiesLeft == 1, "A: the start map, with SUNNY HILLS' party waiting");
            Check(await MapReaches(MapStep.FlagUp, 1.5) && worldMap.BigWords == "SUNNY HILLS CLEAR!", $"its flag goes up now: '{worldMap.BigWords}'");
            Check(await MapReaches(MapStep.Ready, 4) && worldMap.DotsShown(0) == WorldMapArt.DotCount(0) && worldMap.StateShown(1) == MapNodeState.Open &&
                  !worldMap.CloudOn(1) && worldMap.PartiesLeft == 0, "the road pops up and CANDY LAND's cloud puffs away: it was never lost");
            await TapKey(Key.Escape);
            Check(!mapOpen && hud.ResultsShowing, "Esc: the results card");
            await PressButton(JoyButton.A);
            Check(mapOpen && worldMap.Visit.Parties.Count == 0 && worldMap.PartiesLeft == 0, "A again: no party this time (it only happens once)");
            await TapKey(Key.Escape);

            // (b) GoToTitle and StartGame close any map
            GoToTitle();
            await StartQuietGame(FreshName("TMAPRESET"));
            PretendBossBeaten();
            Check(await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5), "a map between worlds");
            await Wait(0.5);
            GoToTitle();
            Check(!mapOpen && !GetTree().Paused && !sounds.MapMusicPlaying && !sounds.MusicPausedNow && worldMap.Step == MapStep.Hidden &&
                  worldMap.Hero is null && worldMap.Iris.Closed == 0, "GoToTitle closes it: never frozen, no tune, no little Bolt-E, the eye open");
            await StartQuietGame(FreshName("TMAPRESET2"));
            LetBoltECrash();
            Check(Bonk(null) && await WaitUntil(() => hud.ResultsShowing, 3), "game over: the results card");
            await Wait(0.4);
            await PressButton(JoyButton.A);
            Check(mapOpen, "A: the start map");
            StartGame();
            Check(!mapOpen && state == GameState.Playing && !GetTree().Paused && !sounds.MapMusicPlaying && worldMap.Step == MapStep.Hidden,
                  "StartGame closes it too");

            // (c) QUIT TO TITLE while the map is on its way
            GoToTitle();
            await StartQuietGame(FreshName("TMAPQUIT"));
            PretendBossBeaten();
            await Wait(0.3);
            await PressButton(JoyButton.Start);
            Check(paused, "paused");
            QuitToTitle();
            await Wait(NewWorldDelay + 0.3);
            Check(!mapOpen && !GetTree().Paused && mapWantedFor < 0 && !newWorldComing && state == GameState.GameOver,
                  "QUIT TO TITLE: the map never opens, and nothing is frozen");
        }
        finally
        {
            testWorldMap = false;
        }
    }

    // ---------- 8. Never stuck ----------

    /// <summary>
    /// 8. Nobody ever gets stuck: if the map can't open for 6 seconds (a power that never ends), the next world comes the
    /// old way, and nothing new came on the road meanwhile (and WORLD WARP is greyed while the map waits). And after 8
    /// seconds on the map, GO always works.
    /// </summary>
    async Task MapNeverStuck()
    {
        try
        {
            MapTestStart();

            // (a) The map can't open
            await StartQuietGame(FreshName("TMAPSTUCK"));
            bestScore = 1_000_000; // (so a NEW HIGH SCORE! banner can't cover the words)
            int irises = sounds.TimesPlayed(Sfx.MapIris);
            NextWorldSoon(); // (no boss: a world CLEARED again)
            StartPowerUp(PowerUp.Mega);
            powerLeft = 1e9f;
            Check(await WaitUntil(() => mapWantedFor >= 0, NewWorldDelay + 0.5), "the map is wanted");
            await PressButton(JoyButton.Start);
            Check(paused && pauseMenu.WarpGreyed && pauseMenu.NameLine == NewWorldIsComing,
                  $"paused while the map waits: WORLD WARP is greyed ('{pauseMenu.NameLine}')");
            await PressButton(JoyButton.Start);
            spawnCountdown = 0;
            bool opened = false, came = false;
            Check(await FastForwardUntil(() =>
            {
                opened |= mapOpen;
                came |= obstacles.Count > 0 || boxes.Count > 0 || enemies.Count > 0;
                return worldNumber == 1;
            }, MapGiveUpSeconds + 1), "after 6 seconds the map gives up");
            Check(!opened && !came && sounds.TimesPlayed(Sfx.MapIris) == irises, "the map never opened, and nothing new came on the road");
            Check(worldBlend < 1 && !newWorldComing && mapWantedFor < 0 && hud.BannerText == "WELCOME TO CANDY LAND!",
                  $"CANDY LAND comes the old way (the colors change, '{hud.BannerText}')");
            StopPowersNow();

            // (b) After 8 seconds GO always works
            GoToTitle();
            await StartQuietGame(FreshName("TMAPVALVE"));
            PretendBossBeaten();
            Check(await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5) && await MapReaches(MapStep.FlagUp, 2), "the map's show");
            worldMap.HoldShowForTest = true;
            await PressButton(JoyButton.A);
            Check(mapOpen && worldMap.Hurried && worldMap.Step == MapStep.FlagUp && !worldMap.CanGoNow, "A only hurries the show (it's held still for the test)");
            Check(await FastForwardUntil(() => worldMap.OpenSeconds >= WorldMap.NeverStuckSeconds, 12) && worldMap.CanGoNow,
                  $"after {WorldMap.NeverStuckSeconds:0} seconds on the map GO works anyway");
            await PressButton(JoyButton.A);
            Check(await WaitUntil(() => !mapOpen && worldNumber == 1, 1.5) && !GetTree().Paused, "A: GO, into CANDY LAND");
        }
        finally
        {
            testWorldMap = false;
            Engine.TimeScale = 1;
        }
    }

    // ---------- 9. Every boss ----------

    /// <summary>
    /// 9. After every boss (BIG RUSTY, VROOMBA, DRONEZILLA and OPTI-MAX) the map comes, and GO leaves nothing behind: no
    /// boss, no bad robots, fireballs, shockwaves, lasers, bombs or boxes, no vacuum hum, rotor buzz or OPTI-MAX sounds, the
    /// level's music, the next world, and the map's badge shows the boss who really comes next (never the one just beaten).
    /// </summary>
    async Task MapBeatsEveryBoss()
    {
        try
        {
            MapTestStart();
            testTakeTurns = true;
            await StartQuietGame(FreshName("TMAPBOSSES"));
            var beatenKinds = new HashSet<BossKind>();
            for (int fight = 1; fight <= 4; fight++)
            {
                int world = worldNumber;
                // Never the boss whose turn comes next (so the badge can't just show the one beaten), and all 4 over the 4 fights
                var kind = (BossKind)(((int)BossForFight(gameNumber, bossFightNumber + 2) + 1) % 4);
                beatenKinds.Add(kind);
                SpawnBoss(kind);
                var beaten = boss!;
                beaten.LoseHealth(BossBase.MaxHealth);
                bool blew = await WaitUntil(() => beaten.HasBlownUp, 4);
                ClearList(bolts); // (no prize bolts to wait for: the map comes right away)
                if (kind == BossKind.Vroomba) sounds.Play(Sfx.VacuumHum); // (as if his vacuum was still humming)
                bool humming = false; // (was the hum still going when the map opened?)
                bool opened = blew && await WaitUntil(() =>
                {
                    if (!mapOpen) humming = sounds.IsPlaying(Sfx.VacuumHum);
                    return mapOpen;
                }, NewWorldDelay + MapWaitMost + 0.5);
                if (opened)
                {
                    worldMap.SkipShow();
                    PlantLeftovers(); // (they wait under the map, frozen with the game)
                }
                var comesNext = BossForFight(gameNumber, bossFightNumber + 1);
                bool badge = worldMap.TeaserShowing && worldMap.TeaserBoss == comesNext && comesNext != KindOf(beaten);
                await PressButton(JoyButton.A);
                bool rode = await WaitUntil(() => !mapOpen && worldNumber == world + 1, 1.5);
                await NextFrame();
                await NextFrame();
                bool quiet = (humming || kind != BossKind.Vroomba) && boss is null && enemies.Count == 0 && blasts.Count == 0 &&
                             shockwaves.Count == 0 && lasers.Count == 0 && bombs.Count == 0 && thrownBoxes.Count == 0 &&
                             !sounds.IsPlaying(Sfx.VacuumHum) && !sounds.IsPlaying(Sfx.RotorBuzz) && OptiSounds.All(sound => !sounds.IsPlaying(sound));
                bool music = !sounds.BossMusicActive && sounds.StationMusicActive == InStation && sounds.MusicPlaying;
                Check(blew && opened && badge && rode && quiet && music,
                      $"{beaten.DisplayName}: the map, then {CurrentWorld.Name} with nothing left behind (badge {worldMap.TeaserBoss} = {comesNext}; " +
                      $"blew {blew}, map {opened}, rode {rode}, quiet {quiet}, music {music})");
            }
            Check(beatenKinds.Count == 4, $"all 4 bosses were beaten ({string.Join(", ", beatenKinds)})");
        }
        finally
        {
            testWorldMap = false;
            testTakeTurns = false;
        }
    }

    /// <summary>
    /// What a beaten boss can leave on the road: a Pup-Bot, a dust bunny, a fireball, a shockwave, a laser, a bomb and a
    /// box. (Made right here, with no whoosh or whistle: the map already hushed the game's short sounds when it opened.)
    /// </summary>
    void PlantLeftovers()
    {
        var pupBot = new Enemy { Kind = EnemyKind.PupBot, Position = new Vector2(RobotX + 400, GroundY), GroundY = GroundY, Chaser = true };
        AddChild(pupBot);
        enemies.Add(pupBot);
        AddDustBunny(new Vector2(RobotX + 500, GroundY - 80), -1);
        var blast = new Blast { GroundY = GroundY, Position = new Vector2(900, GroundY - 40), Velocity = Vector2.Zero };
        AddChild(blast);
        blasts.Add(blast);
        var wave = new Shockwave { Direction = -1, Position = new Vector2(1000, GroundY) };
        AddChild(wave);
        shockwaves.Add(wave);
        var laser = new Laser { Position = new Vector2(1100, GroundY - 90), Direction = -1, ZIndex = 6 };
        AddChild(laser);
        lasers.Add(laser);
        var bomb = new DroneBomb { Position = new Vector2(800, 200), GroundY = GroundY, ZIndex = 4 };
        AddChild(bomb);
        bombs.Add(bomb);
        var box = new ThrownBox { GroundY = GroundY, ZIndex = 6 };
        box.Throw(new Vector2(1150, GroundY - 200), 700, 1);
        AddChild(box);
        thrownBoxes.Add(box);
    }

    // ---------- 10. Drawing ----------

    /// <summary>
    /// 10. The map draws every step of the show for every trip (and the start map for a new player, an old player and a
    /// champion with the GOLD MAP), always inside the screen, with only ASCII words, and with no engine errors. Its
    /// confetti flies (from the map's own pool of 160 bits). Little Bolt-E hides inside the rocket and the space capsule,
    /// and floats in a glass bubble drawn over him in space. He wears every look on the map, and every boss's badge draws
    /// (BIG RUSTY in all 7 outfits). Nothing on the map bumps into anything (see MapLayoutProblems), the words fit, and a
    /// CLEARED world's postcard has a little gold flag, on the title strip and on the pause screen.
    /// </summary>
    async Task MapDrawsEverywhere()
    {
        int errors = SelfTestLogger.Errors;
        try
        {
            MapTestStart();
            var outside = new List<string>();
            var notAscii = new HashSet<string>();
            int frames = 0, draws = 0;

            async Task CheckDrawing(string what, bool board = true)
            {
                int before = worldMap.TimesDrawn;
                await NextFrame();
                await NextFrame();
                frames += board ? 1 : 0;
                if (board && worldMap.TimesDrawn > before) draws++;
                var area = worldMap.DrawnArea;
                if (board && (area.Position.X < 0 || area.Position.Y < 0 || area.End.X > 1280 || area.End.Y > 718)) outside.Add($"{what}: {area}");
                foreach (string text in worldMap.TextsDrawn)
                    if (text.Any(letter => letter > 127)) notAscii.Add(text);
            }

            // Between worlds: SUNNY HILLS is CLEARED for the first time, and its FLAG UP throws confetti (the map's own bits)
            await StartQuietGame(FreshName("TMAPDRAW"));
            PretendBossBeaten();
            Check(await WaitUntil(() => mapOpen, NewWorldDelay + MapWaitMost + 0.5), "a map between worlds");
            bool flagUp = await MapReaches(MapStep.FlagUp, 2);
            await Wait(0.2);
            int flying = worldMap.BitsFlying;
            Check(flagUp && flying >= 20 && flying <= 160 && worldMap.BitsMade >= flying, $"FLAG UP: confetti flies ({flying} bits, out of the map's 160)");

            // Every step of every trip
            worldMap.Preview(MapStep.IrisClose, 0.5f, 0, 1);
            await CheckDrawing("IrisClose", board: false);
            worldMap.Preview(MapStep.Black, 0.5f, 0, 1);
            await CheckDrawing("Black", board: false);
            var steps = new[] { MapStep.IrisOpen, MapStep.FlagUp, MapStep.PathReveal, MapStep.CloudPoof, MapStep.AllClear, MapStep.Sparkle, MapStep.Travel, MapStep.Ready };
            for (int trip = 0; trip < Worlds.All.Length; trip++)
                foreach (var step in steps)
                {
                    worldMap.Preview(step, 0.5f, trip, (trip + 1) % Worlds.All.Length);
                    await CheckDrawing($"{step} {trip}");
                }

            // The rides: inside the rocket and the space capsule only the ride shows; in space a glass bubble is drawn over him
            (int Road, float Progress, bool Shows, Ride? Drawn, string What)[] rides =
            {
                (3, 0.1f, true, null, "riding to the rocket pad"),
                (3, 0.3f, false, Ride.Rocket, "in the rocket, shaking on its pad"),
                (3, 0.7f, false, Ride.Rocket, "in the rocket, flying up to THE MOON"),
                (4, 0.5f, true, Ride.Space, "floating through space"),
                (5, 0.3f, false, Ride.Capsule, "in the space capsule, under its parachute"),
                (5, 0.66f, false, Ride.Capsule, "SPLOOSH"),
                (5, 0.85f, true, Ride.Capsule, "surfing to VOLCANO ISLAND's beach"),
                (6, 0.5f, true, Ride.Sea, "surfing home"),
            };
            var wrongRides = new List<string>();
            foreach (var (road, progress, shows, drawn, what) in rides)
            {
                worldMap.Preview(MapStep.Travel, progress, road, (road + 1) % Worlds.All.Length);
                await CheckDrawing(what);
                bool bubbleRight = worldMap.SpaceBubbleDrawn == (road == 4) && (road != 4 || worldMap.HeroZ == 0); // (the bubble is in the layer over him)
                if (worldMap.HeroShowing != shows || worldMap.RideDrawn != drawn || !bubbleRight)
                    wrongRides.Add($"{what}: Bolt-E {(worldMap.HeroShowing ? "shows" : "hidden")}, ride {worldMap.RideDrawn?.ToString() ?? "-"}, bubble {worldMap.SpaceBubbleDrawn}");
            }
            Check(wrongRides.Count == 0, "the rides: he hides inside the rocket and the space capsule, and floats in a glass bubble drawn over him in space" +
                                         (wrongRides.Count > 0 ? ": " + string.Join("; ", wrongRides) : ""));

            // Little Bolt-E wears every look on the map (his hat and his board, or COZMOS's and VECKTOR's treads)
            worldMap.Preview(MapStep.Ready, 1, 0, 1);
            var wrongLooks = new List<string>();
            foreach (var look in Looks.All)
            {
                var little = worldMap.Hero!;
                Looks.Apply(little, look);
                little.SelfModulate = Colors.White;
                await CheckDrawing($"look {look.Id}");
                bool hatRight = look.Hat == Hat.None ? little.HatDrawn is null : little.HatDrawn == look.Hat;
                bool rideRight = look.BodyKind == RobotBody.BoltE ? little.BoardDrawn == look.Board && !little.TreadsDrawn
                                                                  : little.BoardDrawn is null && little.TreadsDrawn;
                if (!hatRight || !rideRight || !worldMap.HeroShowing) wrongLooks.Add(look.Name);
            }
            Looks.Apply(worldMap.Hero!, Looks.Classic);
            worldMap.Hero!.SelfModulate = Colors.White;
            Check(wrongLooks.Count == 0, $"little Bolt-E wears every look on the map ({Looks.All.Length} looks){(wrongLooks.Count > 0 ? ": not " + string.Join(", ", wrongLooks) : "")}");

            // Every boss's badge, at every world: BIG RUSTY in each of his 7 outfits (the other bosses don't dress up)
            var wrongBadges = new List<string>();
            int badges = 0;
            foreach (var bossKind in Enum.GetValues<BossKind>())
                foreach (var outfit in Enum.GetValues<BossOutfit>())
                {
                    int world = badges++ % Worlds.All.Length;
                    worldMap.Preview(MapStep.Ready, 1, world, (world + 1) % Worlds.All.Length);
                    worldMap.PreviewTeaser(bossKind, outfit);
                    await CheckDrawing($"badge {bossKind} {outfit}");
                    if (worldMap.TeaserDrawn is not { } badge || badge.Boss != bossKind || badge.Outfit != outfit) wrongBadges.Add($"{bossKind} {outfit}");
                }
            Check(badges == 4 * 7 && wrongBadges.Count == 0, $"every boss's badge draws ({badges}: 4 bosses x 7 outfits){(wrongBadges.Count > 0 ? ": not " + string.Join(", ", wrongBadges) : "")}");

            // The start map: a new player, an old player (YOUR OLD GAMES COUNT!) and a champion (the GOLD MAP)
            string newbie = FreshName("TMAPDRAWNEW"), old = FreshName("TMAPDRAWOLD"), champ = FreshName("TMAPDRAWCHAMP");
            SeedRun(old, bolts: 0, world: 3);
            for (int world = 0; world < Worlds.All.Length; world++) SeedClear(champ, world);
            bool gold = false;
            foreach (string player in new[] { newbie, old, champ })
            {
                database?.SetSetting("player_name", player);
                GoToTitle();
                PlayPressed();
                await CheckDrawing($"{player} opening", board: false);
                worldMap.SkipShow();
                await CheckDrawing($"{player} READY");
                worldMap.Preview(MapStep.Stepping, 0.5f, 0, player == champ ? 1 : Worlds.StationIndex);
                await CheckDrawing($"{player} stepping");
                gold |= player == champ && worldMap.GoldMap;
            }
            int expected = steps.Length * Worlds.All.Length + rides.Length + Looks.All.Length + badges + 6;
            Check(draws == frames && frames == expected, $"the map drew every time ({draws} of {frames}, {expected} expected)");
            Check(outside.Count == 0, $"everything it draws is inside the screen{(outside.Count > 0 ? ": " + string.Join("; ", outside.Take(4)) : $" (last: {worldMap.DrawnArea})")}");
            Check(notAscii.Count == 0, $"every word on the map is plain ASCII{(notAscii.Count > 0 ? ": " + string.Join(", ", notAscii) : "")}");
            Check(gold, "a champion who CLEARED all 7 worlds gets the GOLD MAP");

            // The old player's 2 CLEARED worlds: a little gold flag on their postcards, on the title strip and on the pause screen
            database?.SetSetting("player_name", old);
            GoToTitle();
            await NextFrame();
            await NextFrame();
            int stripFlags = hud.Postcards.FlagsDrawn;
            await StartQuietGame(old);
            PauseGame();
            await NextFrame();
            await NextFrame();
            int pauseFlags = pauseMenu.FlagsDrawn;
            ResumeGame(blip: false);
            Check(clearedWorlds.Count == 2 && stripFlags == 2 && pauseFlags == 2,
                  $"a CLEARED world's postcard has a little gold flag: {stripFlags} on the title strip, {pauseFlags} on the pause screen ({clearedWorlds.Count} CLEARED)");

            // Nothing on the map bumps into anything
            var problems = MapLayoutProblems(out int pairs);
            Check(problems.Count == 0, $"nothing on the map bumps into anything ({pairs} pairs checked){(problems.Count > 0 ? ": " + string.Join("; ", problems.Take(6)) : "")}");

            // The words fit
            int smallest = Worlds.All.Min(world => WorldMapArt.FitSize(world.Name, 34, 26, WorldMapArt.NameBoxTextWidth));
            float widestName = Worlds.All.Max(world => TextWidth(world.Name, WorldMapArt.FitSize(world.Name, 34, 26, WorldMapArt.NameBoxTextWidth)));
            float owner = TextWidth(Hud.ShortName(W16, 10), 32);
            float clear = new[] { "ALL 7 WORLDS CLEAR!", "YOUR OLD GAMES COUNT!", "ALL THE WAY AROUND!" }.Concat(WorldMapArt.ClearWords).Max(words => TextWidth(words, 56));
            Check(smallest >= 26 && widestName <= WorldMapArt.NameBoxTextWidth && owner <= WorldMapArt.CornerPanel.Size.X - 16 && clear <= 1180,
                  $"the words fit: world names at {smallest}+ px ({widestName:0} of {WorldMapArt.NameBoxTextWidth:0} px), the widest name {owner:0} px, the big words {clear:0} of 1180 px");
            int promptSize = Worlds.All.Select(world => $"Next: {world.Name}").Append(WorldMap.StartPrompt)
                                       .Min(text => WorldMapArt.FitSize(text, 22, 18, WorldMapArt.NameBoxTextWidth));
            string widest = Hud.ShortName(W16, 10);
            float crownEnd = 24 + TextWidth(widest, WorldMapArt.FitSize(widest, 32, 20, 250)) + 10 + 28; // (the crown comes after the name)
            float numbers = new[] { TextWidth("Score: 999999", 36), TextWidth("Bolts: 99999", 28), TextWidth("WORLDS CLEARED: 7 / 7", 22) }.Max();
            Check(promptSize >= 18 && crownEnd <= WorldMapArt.CornerPanel.End.X - 4 && numbers <= WorldMapArt.CornerPanel.Size.X - 16,
                  $"...and the name box's second line ({promptSize}+ px), the champion's crown after the widest name (to x {crownEnd:0}), and the corner's numbers ({numbers:0} px)");
        }
        finally
        {
            testWorldMap = false;
            GoToTitle();
            await NextFrame();
        }
        Check(SelfTestLogger.Errors == errors, "no engine errors");
    }

    /// <summary>
    /// THE WORLD MAP's layout check, on WorldMapArt's tables: does anything bump into anything? (An empty list = all good.)
    ///   Little Bolt-E standing on a world never touches his GO bubble (it stays at least 10 pixels away), the gold flag,
    ///   the boss badge or the NEXT arrow; the bubble, the flag and the badge (they show together) never touch each other.
    ///   The big words never cover a sign that can show at the same time: "SUNNY HILLS CLEAR!" and the NEXT arrow over the
    ///   next world, any "NEW!", any world's picture; "ALL THE WAY AROUND!" and SUNNY HILLS' GO bubble and badge.
    ///   "NEW!" never touches a NEXT arrow or the parked rocket. No road runs under a GO bubble or a NEXT arrow (except
    ///   right at its own world, where the roads start), and the space bubble stays on the screen all the way through space.
    ///   Every sign stays on the screen and off the two panels. "pairs" = how many things were checked.
    /// </summary>
    static List<string> MapLayoutProblems(out int pairs)
    {
        var problems = new List<string>();
        int count = Worlds.All.Length, checkedPairs = 0;
        void Apart(Rect2 a, Rect2 b, string what, float gap = 0)
        {
            checkedPairs++;
            if (a.Grow(gap).Intersects(b)) problems.Add(what);
        }
        static string Name(int world) => Worlds.All[world].Name;
        var screen = new Rect2(0, 0, 1280, 718);

        // Each world's own signs, and little Bolt-E standing on it
        for (int w = 0; w < count; w++)
        {
            Rect2 hero = WorldMapArt.HeroBox(w), bubble = WorldMapArt.BubbleRect(w), flag = WorldMapArt.FlagRect(w);
            Rect2 badge = WorldMapArt.TeaserRect(w), next = WorldMapArt.NextArrowRect(w);
            Apart(hero, bubble, $"{Name(w)}: little Bolt-E is closer than 10 px to his GO bubble", 10);
            Apart(hero, flag, $"{Name(w)}: little Bolt-E and the flag");
            Apart(hero, badge, $"{Name(w)}: little Bolt-E and the boss badge");
            Apart(hero, next, $"{Name(w)}: little Bolt-E and the NEXT arrow");
            Apart(bubble, badge, $"{Name(w)}: the GO bubble and the boss badge");
            Apart(bubble, flag, $"{Name(w)}: the GO bubble and the flag");
            Apart(flag, badge, $"{Name(w)}: the flag and the boss badge");
            Apart(next, flag, $"{Name(w)}: the NEXT arrow and the flag");
            foreach (var (sign, what) in new[] { (bubble, "GO bubble"), (flag, "flag"), (badge, "boss badge"), (next, "NEXT arrow") })
            {
                checkedPairs++;
                if (!screen.Encloses(sign)) problems.Add($"{Name(w)}: the {what} is off the screen ({sign})");
                Apart(sign, WorldMapArt.CornerPanel, $"{Name(w)}: the {what} and the corner panel");
                Apart(sign, WorldMapArt.NameBox, $"{Name(w)}: the {what} and the name box");
            }
        }

        // "NEW!" (only worlds 1 to 4 can be LOCKED, so only they can get it)
        var news = new List<(int World, Rect2 Area)>();
        for (int w = 1; w <= 4; w++)
        {
            var at = WorldMapArt.NewWordsAt(w);
            var area = WorldMapArt.InkedWords("NEW!", at.X, at.Y + 10, 28, 6);
            news.Add((w, area));
            for (int v = 0; v < count; v++) Apart(area, WorldMapArt.NextArrowRect(v), $"NEW! at {Name(w)} and the NEXT arrow over {Name(v)}");
            Apart(area, WorldMapArt.ParkedRocket, $"NEW! at {Name(w)} and the parked rocket");
            checkedPairs++;
            if (!screen.Encloses(area)) problems.Add($"NEW! at {Name(w)} is off the screen");
        }

        // The big words, and what can show at the same time as them
        Rect2 Big(string words)
        {
            int size = WorldMapArt.FitSize(words, 56, 30, WorldMapArt.BigWordsWidth);
            return WorldMapArt.InkedWords(words, 640, WorldMapArt.BigWordsBaseline, size, size / 5);
        }
        void ClearOfPictures(Rect2 words, string what)
        {
            for (int v = 0; v < count; v++) Apart(words, WorldMapArt.LandmarkBoxes[v], $"'{what}' and {Name(v)}'s picture");
            foreach (var (world, area) in news) Apart(words, area, $"'{what}' and NEW! at {Name(world)}");
        }
        for (int k = 0; k < count; k++)
        {
            var words = Big(WorldMapArt.ClearWords[k]);
            Apart(words, WorldMapArt.NextArrowRect((k + 1) % count), $"'{WorldMapArt.ClearWords[k]}' and the NEXT arrow over {Name((k + 1) % count)}");
            ClearOfPictures(words, WorldMapArt.ClearWords[k]);
        }
        ClearOfPictures(Big("YOUR OLD GAMES COUNT!"), "YOUR OLD GAMES COUNT!");
        ClearOfPictures(Big("ALL 7 WORLDS CLEAR!"), "ALL 7 WORLDS CLEAR!");
        ClearOfPictures(WorldMapArt.InkedWords("The whole map is GOLD now!", 640, WorldMapArt.BigWordsBaseline + 42, 30, 6), "The whole map is GOLD now!");
        var around = Big("ALL THE WAY AROUND!");
        ClearOfPictures(around, "ALL THE WAY AROUND!");
        Apart(around, WorldMapArt.BubbleRect(0), "'ALL THE WAY AROUND!' and SUNNY HILLS' GO bubble");
        Apart(around, WorldMapArt.TeaserRect(0), "'ALL THE WAY AROUND!' and SUNNY HILLS' boss badge");
        Apart(around, WorldMapArt.HeroBox(0), "'ALL THE WAY AROUND!' and little Bolt-E on SUNNY HILLS");

        // No road runs under a GO bubble or a NEXT arrow (only right next to its own world, where the roads start and end)
        var found = new HashSet<string>();
        for (int road = 0; road < count; road++)
            foreach (var point in WorldMapArt.RoadSamples(road))
                for (int w = 0; w < count; w++)
                {
                    checkedPairs += 2;
                    if (point.DistanceTo(WorldMapArt.Spots[w]) < 34) continue;
                    string underBubble = $"road {road} runs under {Name(w)}'s GO bubble", underArrow = $"road {road} runs under {Name(w)}'s NEXT arrow";
                    if (WorldMapArt.BubbleRect(w).HasPoint(point) && found.Add(underBubble)) problems.Add($"{underBubble} (at {point.Round()})");
                    if (WorldMapArt.NextArrowRect(w).HasPoint(point) && found.Add(underArrow)) problems.Add($"{underArrow} (at {point.Round()})");
                }

        // Through space in his glass bubble (radius 34 around his middle, 25 px over his feet, floating 3 px up and down)
        foreach (var point in WorldMapArt.RoadSamples(4))
        {
            checkedPairs++;
            if (screen.Encloses(new Rect2(point + new Vector2(-38, -64), new Vector2(76, 70)))) continue;
            problems.Add($"his space bubble goes off the screen (at {point.Round()})");
            break;
        }
        pairs = checkedPairs;
        return problems;
    }

    // ---------- 11. Music and sounds ----------

    /// <summary>
    /// 11. "Island Hop", the map's tune, is a whole loop with room for the game's sounds. All 11 map sounds play (even while
    /// the game is frozen). The tune only plays while a map is up (Esc, GoToTitle, StartGame and GO all stop it, and so does
    /// StopEverything), never with the music switched off, and a GAME OVER jingle never comes back after the map.
    /// </summary>
    async Task MapMusicAndSounds()
    {
        try
        {
            MapTestStart();
            await NextFrame();

            // "Island Hop"
            float[] samples = MapSong.Render();
            double seconds = (double)samples.Length / Synth.SampleRate;
            Check(Math.Abs(seconds - MapSong.Bars * 4 * 60 / MapSong.BeatsPerMinute) < 0.02 && samples.All(float.IsFinite),
                  $"'{MapSong.Title}' is a whole 16-bar loop ({seconds:0.00} seconds at {MapSong.BeatsPerMinute} BPM)");
            float peak = samples.Max(MathF.Abs);
            double rms = Math.Sqrt(samples.Average(s => (double)s * s));
            float seam = MathF.Abs(samples[0] - samples[^1]);
            Check(peak <= 0.851f && rms is > 0.06 and < 0.35 && Math.Abs(samples.Average(s => (double)s)) < 0.01 && seam < 0.035f,
                  $"it leaves room for the game's sounds (peak {peak:0.000}, RMS {rms:0.000}), no drift, and it loops without a click ({seam:0.0000})");
            using (var stream = Synth.ToAudioStream(samples, loop: true))
            {
                string preview = Path.Combine(Path.GetDirectoryName(selfTestDbPath)!, "island-hop.wav");
                Check(stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward && stream.LoopBegin == 0 && stream.LoopEnd == samples.Length &&
                      stream.SaveToWav(preview) == Error.Ok, $"Godot loops the whole tune (a copy to listen to: {preview})");
            }

            // All 11 map sounds
            var mapSounds = new[]
            {
                Sfx.MapIris, Sfx.MapPip, Sfx.MapFlag, Sfx.MapPoof, Sfx.MapRocket, Sfx.MapSplash, Sfx.MapReady, Sfx.MapGo, Sfx.MapHop,
                Sfx.MapAllClear, Sfx.MapBossSting,
            };
            var silent = new List<Sfx>();
            foreach (var sound in mapSounds)
            {
                int before = sounds.TimesPlayed(sound);
                sounds.Play(sound);
                if (sounds.TimesPlayed(sound) != before + 1 || !sounds.PlaysWhilePaused(sound)) silent.Add(sound);
            }
            Check(mapSounds.Length == 11 && silent.Count == 0, $"all 11 map sounds play, even while the game is frozen{(silent.Count > 0 ? ": not " + string.Join(", ", silent) : "")}");

            // The tune only plays while a map is up
            GoToTitle();
            await NextFrame();
            PlayPressed();
            bool plays = mapOpen && sounds.MapMusicPlaying && sounds.MusicPausedNow;
            await TapKey(Key.Escape);
            bool afterEsc = !sounds.MapMusicPlaying && !sounds.MusicPausedNow && sounds.MusicPlaying;
            PlayPressed();
            GoToTitle();
            bool afterTitle = !sounds.MapMusicPlaying && sounds.MusicPlaying;
            PlayPressed();
            StartGame();
            bool afterStart = !mapOpen && !sounds.MapMusicPlaying && sounds.MusicPlaying;
            GoToTitle();
            PlayPressed();
            worldMap.SkipShow();
            await PressButton(JoyButton.A);
            bool afterGo = await WaitUntil(() => state == GameState.Playing && !mapOpen, 1.5) && !sounds.MapMusicPlaying && sounds.MusicPlaying;
            GoToTitle();
            PlayPressed();
            sounds.StopEverything();
            bool afterStop = mapOpen && !sounds.MapMusicPlaying;
            GoToTitle();
            sounds.SetMusicOn(false);
            PlayPressed();
            bool off = mapOpen && !sounds.MapMusicPlaying;
            await TapKey(Key.Escape);
            sounds.SetMusicOn(true);
            Check(plays && afterEsc && afterTitle && afterStart && afterGo && afterStop && off,
                  $"the map's tune plays only while a map is up (plays {plays}, Esc {afterEsc}, GoToTitle {afterTitle}, StartGame {afterStart}, " +
                  $"GO {afterGo}, StopEverything {afterStop}, music off {off})");

            // The GAME OVER jingle is hushed when the map opens over the results card (it never comes back after the map)
            await StartQuietGame(FreshName("TMAPHUSH"));
            LetBoltECrash();
            Check(Bonk(null) && await WaitUntil(() => hud.ResultsShowing, 3), "game over: the results card");
            await Wait(0.4);
            Check(sounds.IsPlaying(Sfx.GameOver), "the GAME OVER jingle is playing");
            await PressButton(JoyButton.Start);
            Check(mapOpen, "START: the start map");
            await TapKey(Key.Escape);
            await NextFrame();
            Check(!mapOpen && !GetTree().Paused && !sounds.IsPlaying(Sfx.GameOver), "BACK to the card: the jingle was hushed when the map opened (it doesn't come back)");
        }
        finally
        {
            testWorldMap = false;
            sounds.SetMusicOn(true);
        }
    }
}
