using Godot;

/// <summary>
/// Picking the world to START in! (This is more of Main: "partial" means one class spread over several files.)
///
/// On the title screen, Y on Bolt-E's controller (or W on the keyboard) steps through the worlds this player may start in:
/// Sunny Hills, every world they have already reached, and UPSIDE-DOWN STATION (always, because it's the brand-new world).
/// The picked world's postcard glows, and "START: ..." under the postcards says which one it is. The game remembers
/// the pick until it's closed.
///
/// Fair play: starting somewhere else never wins postcards or looks you haven't earned. In a game that started in another
/// world, a world only counts as "reached" when Bolt-E really rode into it by beating a boss, and only if it's the next
/// world after one he had already reached (so the worlds reached are always Sunny Hills, Candy Land, ... in order).
/// Just starting in UPSIDE-DOWN STATION counts as 1 world (Sunny Hills), so its postcard still has to be won by riding
/// there from the Moon. The saved record is the best game ever, so a game like that never makes it smaller.
/// </summary>
public partial class Main
{
    int startWorld;           // the world the next game starts in (0 = Sunny Hills). Remembered until the game is closed.
    int gameStartWorld;       // the world THIS game started in
    int farthestAtStart = 1;  // the most worlds this player had reached before this game

    /// <summary>The worlds this player may start in: Sunny Hills, the worlds they've reached, and UPSIDE-DOWN STATION.</summary>
    List<int> AllowedStartWorlds()
    {
        var allowed = new List<int>();
        for (int world = 0; world < Math.Min(farthestWorld, Worlds.All.Length); world++) allowed.Add(world);
        if (!allowed.Contains(Worlds.StationIndex)) allowed.Add(Worlds.StationIndex);
        return allowed;
    }

    /// <summary>
    /// The button that picks the start world: Y on Bolt-E's own controller (Dad's second controller never gets here: see
    /// _UnhandledInput), or W on the keyboard.
    /// </summary>
    bool IsStartWorldButton(InputEvent e) =>
        (e is InputEventJoypadButton { ButtonIndex: JoyButton.Y, Pressed: true } && e.Device == controllerOne) ||
        e is InputEventKey { PhysicalKeycode: Key.W, Pressed: true, Echo: false };

    /// <summary>Y on the title screen: the next world to start in (round and round).</summary>
    void StepStartWorld()
    {
        var allowed = AllowedStartWorlds();
        int now = allowed.IndexOf(startWorld);
        startWorld = allowed[(now + 1) % allowed.Count];
        sounds.Play(Sfx.Tick, 1.6f);
        ShowStartWorld();
    }

    /// <summary>Shows the start world on the postcards (back to Sunny Hills if this player can't start in it).</summary>
    void ShowStartWorld()
    {
        if (!AllowedStartWorlds().Contains(startWorld)) startWorld = 0;
        hud.ShowStartWorld(startWorld);
    }

    /// <summary>
    /// The end of StartGame: a picked world? Then Bolt-E starts there: its colors, gravity and music right away, and
    /// "WELCOME TO ...!" (in UPSIDE-DOWN STATION the ceiling is shut from the start, so he can flip right away).
    /// </summary>
    void StartInPickedWorld()
    {
        gameStartWorld = AllowedStartWorlds().Contains(startWorld) ? startWorld : 0;
        farthestAtStart = farthestWorld;
        if (gameStartWorld == 0) return;

        worldNumber = gameStartWorld;
        fromWorld = toWorld = CurrentWorld;
        worldBlend = 1;
        robot.SetGravityScale(toWorld.Gravity);
        sounds.StartMusic(toWorld.MusicSpeed, CurrentLevelMusic); // its own score and speed right away
        hud.ShowBanner($"WELCOME TO {toWorld.Name}!", new Color(1f, 0.85f, 0.2f));
        if (toWorld.Gravity < 1) hud.ShowHint("Moon jumps are FLOATY!");
        if (InStation) ArriveInStation(shutNow: true);
    }

    /// <summary>
    /// How many worlds this game has reached after riding into world number n (it keeps counting: 6 is Sunny Hills again).
    /// A game from Sunny Hills: every world on the way counts, like always. A game that started in another world: see the
    /// fair-play rule at the top of this file.
    /// </summary>
    int WorldsReachedAfter(int n)
    {
        if (gameStartWorld == 0) return Math.Max(worldsReached, n + 1);
        int world = n % Worlds.All.Length;
        bool next = world <= Math.Max(worldsReached, farthestAtStart); // (the next world after one he had really reached)
        return next ? Math.Max(worldsReached, world + 1) : worldsReached;
    }

    /// <summary>The World Tour line on the results card. A game that started in another world says where it went.</summary>
    string ResultsWorldLine()
    {
        if (gameStartWorld == 0) return Worlds.WorldLine(worldsReached);
        string from = Worlds.All[gameStartWorld].Name;
        if (worldNumber == gameStartWorld) return $"World: {from}";
        if (worldNumber - gameStartWorld >= Worlds.All.Length) return "You went all the way around the world!";
        return $"You rode from {from} to {CurrentWorld.Name}!";
    }
}
