using Godot;

/// <summary>
/// THE WORLD MAP! (This is more of Main: "partial" means one class spread over several files.)
///
/// Between worlds: after a boss blows up and "YOU BEAT ...!" has shown, the game closes like an eye and the WORLD MAP
/// opens. The world he beat gets a gold flag, the road to the next world pops up dot by dot, the cloud over the next
/// world puffs away, and little Bolt-E travels there (by rocket up to THE MOON, in a bubble to the STATION, by capsule
/// down to VOLCANO ISLAND, and surfing home!). A, B, X, Y, START, Enter or Space = GO: the next world, right away, with
/// the same score, bolts and batteries. (Pressing while the map's show plays makes it go faster.)
/// Before every game the map shows too: the D-pad walks little Bolt-E to the world to start in (the same worlds as DOWN on
/// the title screen), GO starts there, and BACK (or Esc) goes back to the menu.
/// While the map is up the game is frozen, just like the pause screen (see Main.Pause.cs). Dad's second controller does
/// nothing on the map. Which worlds are CLEARED is saved for every player (see ScoreDatabase.cs: world_clears), and old
/// games count too: "YOUR OLD GAMES COUNT!" (the first five worlds).
/// The map is drawn in WorldMap.cs and WorldMapArt.cs, and its tune "Island Hop" is in Audio/MapSong.cs.
/// </summary>
public partial class Main
{
    // ---- The WORLD MAP (try changing these!) ----
    const float MapWaitMost = 2f;            // after a boss, the map waits at most this long for Bolt-E to land and grab his prize bolts
    const float MapAfterRebuildSeconds = 1f; // after the magic rebuild, the map waits this long (so "BOLT-E IS BACK!" shows)
    const float MapGiveUpSeconds = 6f;       // the map couldn't open for this long? Then the next world comes the old way
    const float SpeedAfterMap = StartSpeed;  // how fast Bolt-E rides into the new world (like the start of a game)
    const float MapSafeGap = 1500f;          // after the map: this many pixels of empty road
    const int OldGamesClearUpTo = 4;         // old games count as CLEARED up to THE MOON (world number 4)

    /// <summary>What a boss win did (remembered from the blow-up until its flag goes up on a map).</summary>
    record MapWin(int World, bool FirstClear, BossKind Boss);

    CanvasLayer mapLayer = null!;
    WorldMap worldMap = null!;
    bool mapOpen;              // true while the map is up (and the game waits behind it, frozen)
    int mapWantedFor = -1;     // a boss was beaten: the map opens as soon as it can, for a trip to world number mapWantedFor
    int mapTripTo = -1;        // the world number the open map rides into (it keeps counting: 7 is Sunny Hills again)
    float mapWaitTime;         // seconds of riding while the map waited
    float sinceRebuild = 99;   // seconds since Bolt-E was last in pieces
    MapWin? pendingWin;        // the last boss win, for the map's flag and road
    HashSet<int> clearedWorlds = new();                                     // the loaded player's CLEARED worlds (0 = Sunny Hills...)
    readonly Dictionary<string, HashSet<int>> clearedThisSession = new();   // clears since the game was switched on (by PlayerKey)
    readonly Dictionary<string, HashSet<int>> clearsInDatabase = new();     // ...and the ones the database already has
    readonly Dictionary<string, List<MapWin>> partiesWaiting = new();       // first clears whose flag hasn't gone up on a map yet
    readonly HashSet<string> mapSeenToday = new();  // players whose old games already had their party (also without a database)
    readonly HashSet<int> oldGamesLeft = new();     // the worlds of "YOUR OLD GAMES COUNT!" still to celebrate on this map
    int padOnMap = -1;         // Bolt-E's controller when the map opened (see EnterWorldFromMap)
    bool testWorldMap;         // (the self-test) true = the WORLD MAP is used during the self-test too (only the map tests turn it on)

    /// <summary>
    /// The map is used: always, except in the self-test's older tests (see testWorldMap). Then a boss win rides into the
    /// next world by itself after 2.2 seconds, and a play press starts the game, exactly like before the map existed.
    /// </summary>
    bool WorldMapOn => !selfTesting || testWorldMap;

    /// <summary>
    /// The map may open right now: riding (not in pieces, not in slow motion, not paused or about to pause), not falling
    /// through a gap or flipping, not in a LAVA CAVE, no boss coming or still fighting, no power or slot machine, and a
    /// moment after a rebuild. (Like CanPauseNow, and a bit stricter.)
    /// </summary>
    bool CanOpenMapNow =>
        state == GameState.Playing && !paused && !pauseWanted && !mapOpen && Engine.TimeScale == 1 &&
        !robot.Crashed && fallingThrough is null && !robot.FallingIntoSpace && !robot.FlipMode &&
        !VolcanoBusy && !InCave && !bossWarningShown && !BossFightActive &&
        power is null && !roulette.Spinning && !RocketSafe && robot.Size <= 1.05f &&
        sinceRebuild >= MapAfterRebuildSeconds;

    /// <summary>True when no bolt is still ahead of Bolt-E on the screen (he has had his chance at the rainbow).</summary>
    bool RainbowPassed
    {
        get
        {
            foreach (var bolt in bolts)
                if (bolt.Position.X > robot.Position.X - 10 && bolt.Position.X < ScreenWidth + 60) return false;
            return true;
        }
    }

    /// <summary>Makes the map (once, when the game starts). It lives on its own layer: over the game, under the pause screen.</summary>
    void SetUpWorldMap()
    {
        mapLayer = new CanvasLayer { Layer = 48, ProcessMode = ProcessModeEnum.Always }; // (it keeps going while the game is frozen)
        worldMap = new WorldMap
        {
            Size = new Vector2(ScreenWidth, ScreenHeight), MouseFilter = Control.MouseFilterEnum.Ignore, Sounds = sounds, Visible = false,
        };
        worldMap.InputWhileOpen = MapInput;
        worldMap.AtBlack = MapAtBlack;
        worldMap.Celebrated = MapPartyCelebrated;
        var iris = new CaveCurtain { ZIndex = 2 }; // (the LAVA CAVES' eye, reused: a ring that closes to a dot. ZIndex 2: over
                                                   //  everything on the map, even little Bolt-E up in a hop, who is at 1)
        mapLayer.AddChild(worldMap);
        mapLayer.AddChild(iris);      // (after the map: drawn on top of everything, even after the map is gone)
        worldMap.Iris = iris;
        AddChild(mapLayer);           // (after the pause screen's layer: the map hears every press first)
    }

    // ---------- Between worlds: waiting for a safe moment ----------

    /// <summary>Every frame (while the game isn't frozen): a map that's waiting opens as soon as it safely can.</summary>
    void UpdateMap(float dt)
    {
        sinceRebuild = state == GameState.Rebuilding ? 0 : sinceRebuild + dt;
        if (mapWantedFor < 0) return;
        if (state is not (GameState.Playing or GameState.Rebuilding))
        {
            ForgetTheMap(); // (the game ended: never mind)
            return;
        }
        if (state != GameState.Playing) return; // (in pieces: just wait)
        mapWaitTime += dt;
        bool niceMoment = (robot.OnGround && RainbowPassed) || mapWaitTime >= MapWaitMost;
        if (CanOpenMapNow && niceMoment)
        {
            OpenBetweenMap();
            return;
        }
        if (mapWaitTime < MapGiveUpSeconds) return;

        // Something very unusual keeps the map from opening: never get stuck, the next world comes the old way
        int n = mapWantedFor;
        ForgetTheMap();
        ChangeWorld(n);
    }

    /// <summary>No map is coming after all (and no new world is on its way).</summary>
    void ForgetTheMap()
    {
        mapWantedFor = -1;
        newWorldComing = false;
        pendingWin = null;
    }

    // ---------- Opening and closing ----------

    /// <summary>Between worlds: freeze, and the map shows the world just beaten and the trip to the next one.</summary>
    void OpenBetweenMap()
    {
        // What the map shows is worked out FIRST, before anything changes (see OpenStartMap)
        var visit = MakeMapVisit(MapMode.Between, WorldHere, mapWantedFor % Worlds.All.Length, pendingWin);
        mapTripTo = mapWantedFor;
        mapWantedFor = -1;   // (newWorldComing stays true until GO)
        pendingWin = null;
        OpenMapCommon();
        SaveNewClears(hud.PlayerName); // (saved now, while the game is frozen: a slow database can't spoil the KABOOM)
        hud.HideBigWords();            // (their fades would freeze, then come back after the map)
        worldMap.Open(visit, robot.BodyMiddle);
    }

    /// <summary>Before a game (a play press on the title screen or the results card): the map, to pick a world and GO.</summary>
    void OpenStartMap()
    {
        if (mapOpen || state is GameState.Playing or GameState.Rebuilding) return;
        hud.StopTyping();                                                       // (like StartGame: a half-typed name is the player)
        if (PlayerKey(hud.PlayerName) != bankKey) LoadPlayer(celebrate: false); // (the map shows THIS player's worlds)
        ShowStartWorld();                                                       // (back to Sunny Hills if he can't start there)
        // What the map shows is worked out FIRST: it may ask the database, and if the database breaks, the menu just stays
        // as it was (the game is only frozen once nothing can go wrong any more)
        var visit = MakeMapVisit(MapMode.Start, startWorld, -1, null);
        mapTripTo = -1;
        OpenMapCommon();
        oldGamesLeft.Clear();
        oldGamesLeft.UnionWith(visit.OldGames);
        worldMap.Open(visit, robot.BodyMiddle);
    }

    /// <summary>Freeze! (Both maps.) The game's music waits, its short sounds stop, and the map gets its tune.</summary>
    void OpenMapCommon()
    {
        mapOpen = true;
        padOnMap = controllerOne; // (whose controller this is: see EnterWorldFromMap)
        GetTree().Paused = true;
        GetViewport().GuiReleaseFocus();          // (a HUD button with the focus must never get A, Space or Enter under the map)
        sounds.HushGameSounds();
        sounds.StartMapMusic();
        sounds.Play(Sfx.MapIris, 0.8f);
        worldMap.MouseFilter = Control.MouseFilterEnum.Stop; // (the map catches every click: nothing under it is clicked)
    }

    /// <summary>The map goes away and the game runs again. (The eye may still be opening over the game.)</summary>
    void CloseMap()
    {
        mapOpen = false;
        GetTree().Paused = false;     // FIRST: the music's fades, the banner and the NewWorld jingle all belong to the frozen game
        sounds.StopMapMusic();        // the map's tune stops, the level's music goes on
        worldMap.MouseFilter = Control.MouseFilterEnum.Ignore;
        worldMap.CloseBoard();        // (the board hides, little Bolt-E is freed)
    }

    /// <summary>A fresh start (ClearWorld): no map, nothing waiting, never frozen, no eye left half open.</summary>
    void ClearMap()
    {
        CloseMap();
        worldMap.Reset();
        mapWantedFor = mapTripTo = -1;
        mapWaitTime = 0;
        sinceRebuild = 99;
        pendingWin = null;
        oldGamesLeft.Clear(); // (an old games' party cut short comes back on the next start map)
    }

    /// <summary>BACK or Esc on the start map: back to the title screen or the results card, just as they were.</summary>
    void LeaveStartMap()
    {
        worldMap.SnapShow(); // (every flag that was going up counts as celebrated)
        sounds.Play(Sfx.PauseBlip, 0.8f);
        CloseMap();
        worldMap.Reset();
    }

    // ---------- Presses on the map ----------

    /// <summary>A press (or a click) while the map is up: the map sends them all here. true = it was used up.</summary>
    bool MapInput(InputEvent e)
    {
        if (!mapOpen) return false;
        if (selfTesting && e.Device != SelfTestDevice && e.Device != SelfTestSecondPad) return true;
        if ((e is InputEventJoypadButton or InputEventJoypadMotion) && e.Device != controllerOne) return true; // Dad's: nothing
        if (e is InputEventKey && hud.IsTypingName) return true;
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F11 })
        {
            ToggleFullScreen();
            return true;
        }
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F9 })
        {
            SwapScreens();
            return true;
        }

        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) worldMap.ClickedGo(click.Position); // (only the GO bubble)
        else if (IsMapGo(e)) worldMap.GoPressed();
        else if (IsMapBack(e))
        {
            if (worldMap.CanGoBack) LeaveStartMap();
        }
        else if (Input.IsActionJustPressedByEvent("menu_right", e)) StepOnMap(Vector2.Right);
        else if (Input.IsActionJustPressedByEvent("menu_left", e)) StepOnMap(Vector2.Left);
        else if (Input.IsActionJustPressedByEvent("menu_up", e)) StepOnMap(Vector2.Up);
        else if (Input.IsActionJustPressedByEvent("menu_down", e)) StepOnMap(Vector2.Down);
        return true; // (everything else does nothing on the map)
    }

    /// <summary>
    /// GO: A, B, X or Y, START, Enter or Space. (Never the "jump" action: it has D-pad UP and the mouse in it.) Presses
    /// only: a held key's repeat isn't one. (IsActionPressed also works on made-up events, so the self-test checks every one.)
    /// </summary>
    static bool IsMapGo(InputEvent e) =>
        e.IsActionPressed("menu_ok") || e.IsActionPressed("start") ||
        e is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.B or JoyButton.X or JoyButton.Y };

    /// <summary>BACK on Bolt-E's controller, or Esc: the grown-ups' way back to the menu (only on the start map).</summary>
    static bool IsMapBack(InputEvent e) =>
        e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape } ||
        e is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.Back };

    /// <summary>
    /// The D-pad (or the stick, or the arrow keys) on the map, "way" = the way pressed on the screen. Before a game: little
    /// Bolt-E walks to the next world he may start in that way. Between worlds: just a happy "this way!" hop.
    /// </summary>
    void StepOnMap(Vector2 way)
    {
        if (worldMap.Mode == MapMode.Between)
        {
            worldMap.Hop();   // (the road is decided: a happy hop)
            return;
        }
        if (!worldMap.CanStep) return; // (still opening, or GO was pressed)
        int to = MapStepTarget(worldMap.Here, way, AllowedStartWorlds());
        if (to < 0)
        {
            sounds.Play(Sfx.MenuTick, 0.6f); // (nowhere to go that way: a low "nope")
            return;
        }
        startWorld = to;
        ShowStartWorld(); // (the title's postcard glow follows, under the map)
        worldMap.StepTo(to, NextBossTeaser(newGame: true), Worlds.All[to].RustyOutfit);
        sounds.Play(Sfx.MenuTick, 1.7f);
    }

    /// <summary>
    /// Which world a D-pad press walks to, from world "here": the next world he may start in, going round the loop
    /// forwards, or the one going backwards. The one that is more in the way pressed ON THE SCREEN wins (a tie goes to the
    /// forward one), but only if it really is that way (more than 0.3). -1 = nowhere that way.
    /// </summary>
    static int MapStepTarget(int here, Vector2 way, List<int> allowed)
    {
        int count = Worlds.All.Length, forward = -1, backward = -1;
        for (int i = 1; i < count && forward < 0; i++)
            if (allowed.Contains((here + i) % count)) forward = (here + i) % count;
        for (int i = 1; i < count && backward < 0; i++)
            if (allowed.Contains((here - i + count) % count)) backward = (here - i + count) % count;
        if (forward < 0) return -1;
        float forwardScore = HowMuchThatWay(here, forward, way), backwardScore = HowMuchThatWay(here, backward, way);
        int pick = backwardScore > forwardScore ? backward : forward;
        return Mathf.Max(forwardScore, backwardScore) > 0.3f ? pick : -1;
    }

    /// <summary>How much world "to" is in the direction "way" from world "from" on the map (1 = exactly that way, -1 = backwards).</summary>
    static float HowMuchThatWay(int from, int to, Vector2 way) =>
        (WorldMapArt.Spots[to] - WorldMapArt.Spots[from]).Normalized().Dot(way);

    // ---------- GO ----------

    /// <summary>The map's eye has closed after GO: ride into the next world, or start the game.</summary>
    void MapAtBlack()
    {
        if (worldMap.Mode == MapMode.Start) StartFromMap(worldMap.Here);
        else if (state == GameState.Playing && mapTripTo >= 0) EnterWorldFromMap(mapTripTo);
        else
        {
            ClearMap(); // (can't happen: but never stuck anyway)
            newWorldComing = false;
        }
    }

    /// <summary>
    /// GO between worlds! The map goes away, and Bolt-E is in the next world right away (no 3-second color change: the map
    /// hid it). It counts as riding there (ChangeWorld), so the fair rules are the same as always. The beaten boss's last
    /// pieces and every danger puff away, he rolls in at the starting speed, and the road stays empty for a while.
    /// </summary>
    void EnterWorldFromMap(int n)
    {
        bool wasInStation = InStation;                // (before ChangeWorld moves the worlds on)
        CloseMap();                                   // 1. unpause, and the level music goes on FIRST (see SoundBoard.HushGameSounds)
        mapTripTo = -1;
        newWorldComing = false;
        if (boss is not null)                         // 2. his bouncing pieces (or the Station's ceiling would stay
        {                                             //    open, and VOLCANO ISLAND would build a new boss floor)
            boss.QueueFree();
            boss = null;
        }
        hud.HideBossBar();
        sounds.Stop(Sfx.VacuumHum);
        StopPowersNow();                              // 3. every leftover danger puffs away (bolts and the family's flags stay)
        PuffAway(obstacles, _ => true);
        PuffAway(enemies, _ => true);                 //    (Rusty's Pup-Bots and VROOMBA's dust bunnies survive a win)
        PuffAway(boxes, _ => true);
        PuffAway(blasts, _ => true);
        PuffAway(shockwaves, _ => true);
        PuffAway(lasers, _ => true);
        PuffAway(stationGaps, _ => true);
        ClearDroneDangers();
        ClearOptiDangers();
        ClearJumpDust();
        ChangeWorld(n);                               // 4. counts the ride, WELCOME TO ...! (or ALL THE WAY AROUND!), ONE jingle,
                                                      //    gravity, the music, and arriving in (or leaving) the Station and the Volcano
        fromWorld = toWorld;                          // 5. ...right away: AFTER ChangeWorld, because StationWorldChanged and
        worldBlend = 1;                               //    VolcanoWorldChanged look at the OLD fromWorld
        if (InStation)
        {
            if (robot.OnGround) ceilingOpen = 0;      //    (the ceiling is shut at once, like WORLD WARP: flipping starts next frame.
        }                                             //    In the air it slides shut instead, so it never closes over his head)
        else if (wasInStation)
        {
            LeaveStationNow();                        //    (the right way up, no ceiling)
        }
        speed = Mathf.Min(speed, SpeedAfterMap);      // 6. a gentle rolling start: it speeds back up to speedToGetBackTo
        spawnCountdown = Mathf.Max(spawnCountdown, MapSafeGap); // 7. empty road (VOLCANO ISLAND has its 1500 px shore rock too)
        // 8. His controller came unplugged while the map was up, and now that SAME controller is back (and is his again)?
        //    Then no surprise pause after GO. (Another controller that took over while his was gone, like Dad's, doesn't
        //    count: then the game still pauses right after GO, so he never rides without his own controller.)
        if (pauseWanted && controllerOne == padOnMap && PluggedInControllers().Contains(padOnMap)) pauseWanted = false;
        worldMap.OpenOverGame(robot.BodyMiddle);      // 9. the game opens like an eye on Bolt-E
    }

    /// <summary>GO before a game: it starts in the picked world (the same rules as DOWN on the title screen).</summary>
    void StartFromMap(int pick)
    {
        if (AllowedStartWorlds().Contains(pick)) startWorld = pick;
        ShowStartWorld();
        StartGame();                                  // (unchanged: its ClearWorld closes the map and unpauses; StartInPickedWorld does the rest)
        worldMap.OpenOverGame(robot.BodyMiddle);      // (AFTER StartGame: its ClearMap would reset the eye)
    }

    // ---------- Which worlds are CLEARED ----------

    /// <summary>
    /// A boss was beaten in world "world": it's CLEARED for this player. (Here it's only remembered: it's saved in the
    /// database when the map opens, or at game over. See SaveNewClears.) The first time, its flag goes up on the next map.
    /// </summary>
    void RecordWorldCleared(int world)
    {
        string key = PlayerKey(hud.PlayerName);
        bool firstClear = !clearedWorlds.Contains(world);
        clearedWorlds.Add(world);
        ForPlayer(clearedThisSession, key).Add(world);
        var win = new MapWin(world, firstClear, KindOf(boss));
        if (firstClear) ForPlayer(partiesWaiting, key).Add(win);
        pendingWin = win;
    }

    /// <summary>
    /// Saves this player's clears from today that the database doesn't have yet. It never waits long for a busy database,
    /// and it stops at the first one that can't be saved (they're tried again at the next map, or at game over).
    /// </summary>
    void SaveNewClears(string player)
    {
        if (database is null) return;
        string key = PlayerKey(player);
        if (!clearedThisSession.TryGetValue(key, out var today)) return;
        var saved = ForPlayer(clearsInDatabase, key);
        foreach (int world in today)
        {
            if (saved.Contains(world)) continue;
            try
            {
                database.SaveWorldCleared(player, world);
                saved.Add(world);
            }
            catch (Exception e)
            {
                GD.PushWarning($"Could not save the cleared world yet (it's tried again later): {e.Message}");
                return;
            }
        }
    }

    /// <summary>The player's CLEARED worlds: from old games, from the database, and from today.</summary>
    HashSet<int> LoadClearedWorlds(string name)
    {
        string key = PlayerKey(name);
        var saved = database?.WorldsCleared(name) ?? new HashSet<int>();
        clearsInDatabase[key] = saved;
        var cleared = new HashSet<int>(OldGamesCleared(farthestWorld));
        cleared.UnionWith(saved);
        if (clearedThisSession.TryGetValue(key, out var today)) cleared.UnionWith(today);
        return cleared;
    }

    /// <summary>
    /// Old games count too: a game that rode through "farthest" worlds beat the boss of every world before the last one.
    /// Only the first five worlds count this way: older versions of Robot Dash had only 5 or 6 worlds, so an old game
    /// can't tell us whether it really beat UPSIDE-DOWN STATION or VOLCANO ISLAND.
    /// </summary>
    static IEnumerable<int> OldGamesCleared(int farthest)
    {
        for (int world = 0; world <= Math.Min(farthest - 2, OldGamesClearUpTo); world++) yield return world;
    }

    /// <summary>
    /// A player's very first start map: the worlds their old games CLEARED get their flags one by one ("YOUR OLD GAMES
    /// COUNT!"). Only once (see MarkMapSeen), and only the worlds the database doesn't know as cleared already.
    /// </summary>
    List<int> OldGamesToShow(string key)
    {
        var worlds = new List<int>();
        if (mapSeenToday.Contains(key)) return worlds;
        var saved = clearsInDatabase.GetValueOrDefault(key);
        var today = clearedThisSession.GetValueOrDefault(key);
        foreach (int world in OldGamesCleared(farthestWorld))
            if (saved?.Contains(world) != true && today?.Contains(world) != true) worlds.Add(world);
        if (worlds.Count > 0 && MapSeenBefore(key)) worlds.Clear(); // (the database is only asked when there's a party to show)
        return worlds;
    }

    /// <summary>
    /// Did this player's old games already have their party, in an earlier game? (If the database can't answer, it counts
    /// as yes: no party this time, and it's asked again next time.)
    /// </summary>
    bool MapSeenBefore(string key)
    {
        try
        {
            return database?.GetSetting(MapSeenKey(key)) is not null;
        }
        catch (Exception e)
        {
            GD.PushWarning($"Could not ask whether the map was seen (no old games' party this time): {e.Message}");
            return true;
        }
    }

    /// <summary>Where it's remembered that a player's old games had their party on the map: "map_seen:sam" = "1".</summary>
    static string MapSeenKey(string key) => "map_seen:" + key;

    /// <summary>A world's flag really went up on a map: its party isn't waiting any more.</summary>
    void MapPartyCelebrated(int world)
    {
        string key = PlayerKey(hud.PlayerName);
        partiesWaiting.GetValueOrDefault(key)?.RemoveAll(win => win.World == world);
        if (oldGamesLeft.Remove(world) && oldGamesLeft.Count == 0) MarkMapSeen(key);
    }

    /// <summary>"YOUR OLD GAMES COUNT!" has been celebrated: it never comes again for this player.</summary>
    void MarkMapSeen(string key)
    {
        mapSeenToday.Add(key);
        try
        {
            database?.SetSetting(MapSeenKey(key), "1");
        }
        catch (Exception e)
        {
            GD.PushWarning($"Could not remember that the map was seen: {e.Message}");
        }
    }

    /// <summary>
    /// What the map shows (made when it opens): which flags go up, how every world looks before and after the show, the
    /// next boss's badge, and the corner panel. ("win" = the boss win that opened this map, or null.)
    /// </summary>
    MapVisit MakeMapVisit(MapMode mode, int here, int goingTo, MapWin? win)
    {
        string key = PlayerKey(hud.PlayerName);
        var visit = new MapVisit { Mode = mode, Here = here, GoingTo = goingTo };
        var waiting = partiesWaiting.GetValueOrDefault(key);
        if (mode == MapMode.Between)
        {
            // The world just beaten, if its flag hasn't gone up yet (no win = a world CLEARED again: just a sparkle)
            if (win is not null && waiting?.Any(party => party.World == here) == true) visit.Parties.Add(here);
        }
        else
        {
            // Every first clear whose flag never went up (the game ended too soon), and the old games, in loop order
            var worlds = new SortedSet<int>();
            if (waiting is not null)
                foreach (var party in waiting)
                    if (clearedWorlds.Contains(party.World)) worlds.Add(party.World);
            foreach (int world in OldGamesToShow(key))
            {
                worlds.Add(world);
                visit.OldGames.Add(world);
            }
            visit.Parties.AddRange(worlds);
        }

        var before = new HashSet<int>(clearedWorlds);
        before.ExceptWith(visit.Parties);
        var allowedBefore = AllowedStartWorlds(before);
        var allowedAfter = AllowedStartWorlds(clearedWorlds);
        bool allBefore = true, allAfter = true;
        for (int w = 0; w < Worlds.All.Length; w++)
        {
            visit.Before[w] = before.Contains(w) ? MapNodeState.Cleared : allowedBefore.Contains(w) ? MapNodeState.Open : MapNodeState.Locked;
            visit.After[w] = clearedWorlds.Contains(w) ? MapNodeState.Cleared : allowedAfter.Contains(w) ? MapNodeState.Open : MapNodeState.Locked;
            allBefore &= visit.Before[w] == MapNodeState.Cleared;
            allAfter &= visit.After[w] == MapNodeState.Cleared;
        }
        visit.AllClearParty = allAfter && !allBefore;
        visit.RepeatClear = mode == MapMode.Between && !visit.Parties.Contains(here);
        visit.AllTheWayAround = mode == MapMode.Between && goingTo == 0 &&
                                worldNumber + 1 - gameStartWorld >= Worlds.All.Length && !warpedThisRun;
        visit.Next = mode == MapMode.Between ? goingTo : FrontierWorld();
        visit.Player = hud.PlayerName;
        visit.ClearedCount = clearedWorlds.Count;
        visit.Look = Looks.All[RideLookIndex()];
        visit.Teaser = NextBossTeaser(newGame: mode == MapMode.Start);
        visit.TeaserOutfit = Worlds.All[mode == MapMode.Between ? goingTo : here].RustyOutfit;
        if (mode == MapMode.Between)
        {
            visit.Score = Score;
            visit.Bolts = boltsCollected;
            visit.Spares = spareBatteries;
            visit.Charge = boltsTowardBattery / (float)BoltsPerBattery;
        }
        return visit;
    }

    /// <summary>The first world (from Sunny Hills on) that isn't CLEARED yet: the NEXT arrow points at it (-1 = all CLEARED).</summary>
    int FrontierWorld()
    {
        for (int world = 0; world < Worlds.All.Length; world++)
            if (!clearedWorlds.Contains(world)) return world;
        return -1;
    }

    /// <summary>The boss who comes next: in this game, or the first one of the next game (see BossForFight).</summary>
    BossKind NextBossTeaser(bool newGame) =>
        selfTesting && !testTakeTurns ? BossKind.BigRusty
        : newGame ? BossForFight(gameNumber + 1, 1) : BossForFight(gameNumber, bossFightNumber + 1);

    /// <summary>The look Bolt-E rides in (a look he's only trying on comes off, like at the start of a game).</summary>
    int RideLookIndex() => Unlocked(Looks.All[lookIndex]) ? lookIndex : SavedLookIndex();

    /// <summary>This player's set (or list) in a "by player" dictionary, made the first time it's needed.</summary>
    static T ForPlayer<T>(Dictionary<string, T> byPlayer, string key) where T : new()
    {
        if (!byPlayer.TryGetValue(key, out var things)) byPlayer[key] = things = new T();
        return things;
    }
}
