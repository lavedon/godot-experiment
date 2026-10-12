using Godot;

/// <summary>
/// Picking the world to START in! (This is more of Main: "partial" means one class spread over several files.)
///
/// On the title screen, DOWN steps through the worlds this player may start in (the D-pad or the stick on Bolt-E's
/// controller, or the Down arrow or S on the keyboard; A, B, X and Y all start the game, like they always did):
/// Sunny Hills, every world they have already reached, and UPSIDE-DOWN STATION and VOLCANO ISLAND (always, because they're
/// the newest worlds).
/// The picked world's postcard glows, and "START: ..." under the postcards says which one it is. The game remembers
/// the pick until it's closed. On the WORLD MAP before every game (see Main.WorldMap.cs), every world this player has
/// CLEARED, and the world right after it, may be picked too: beating a world's boss opens the road to the next one.
///
/// Fair play: starting somewhere else never wins postcards or looks you haven't earned. In a game that started in another
/// world, a world only counts as "reached" when Bolt-E really rode into it by beating a boss, and only if it's the next
/// world after one he had already reached (so the worlds reached are always Sunny Hills, Candy Land, ... in order).
/// Just starting in UPSIDE-DOWN STATION (or VOLCANO ISLAND) counts as 1 world (Sunny Hills), so its postcard still has to be
/// won by riding there (from the Moon, or from the Station). The saved record is the best game ever, so a game like that
/// never makes it smaller.
/// </summary>
public partial class Main
{
    int startWorld;           // the world the next game starts in (0 = Sunny Hills). Remembered until the game is closed.
    int gameStartWorld;       // the world THIS game started in
    int farthestAtStart = 1;  // the most worlds this player had reached before this game

    /// <summary>
    /// The worlds this player may start in: Sunny Hills, the worlds they've reached, UPSIDE-DOWN STATION and VOLCANO ISLAND
    /// (always, because they're the newest worlds), and every CLEARED world and the world right after it (see
    /// Main.WorldMap.cs). This one list is used by DOWN on the title screen, the WORLD MAP and WORLD WARP.
    /// </summary>
    List<int> AllowedStartWorlds() => AllowedStartWorlds(clearedWorlds);

    /// <summary>The worlds this player may start in, if these were the worlds they have CLEARED (the WORLD MAP's show uses this).</summary>
    List<int> AllowedStartWorlds(IReadOnlySet<int> cleared)
    {
        var allowed = new List<int>();
        for (int world = 0; world < Worlds.All.Length; world++)
        {
            int before = (world + Worlds.All.Length - 1) % Worlds.All.Length; // (the world whose road leads here)
            if (world < Math.Min(farthestWorld, Worlds.All.Length) || world is Worlds.StationIndex or Worlds.VolcanoIndex ||
                cleared.Contains(world) || cleared.Contains(before))
                allowed.Add(world);
        }
        return allowed;
    }

    /// <summary>
    /// The button that picks the start world: DOWN ("menu_down": the D-pad or the stick pushed down on Bolt-E's own
    /// controller, or the Down arrow or S). One push of the stick is one step. (Dad's second controller never gets
    /// here: see _UnhandledInput.)
    /// </summary>
    static bool IsStartWorldButton(InputEvent e) => Input.IsActionJustPressedByEvent("menu_down", e);

    /// <summary>DOWN on the title screen: the next world to start in (round and round).</summary>
    void StepStartWorld()
    {
        var allowed = AllowedStartWorlds();
        int now = allowed.IndexOf(startWorld);
        startWorld = allowed[(now + 1) % allowed.Count];
        sounds.Play(Sfx.Tick, 1.6f);
        ShowStartWorld();
    }

    /// <summary>
    /// Shows the start world on the postcards (back to Sunny Hills if this player can't start in it), and a little gold
    /// flag on the postcard of every world this player has CLEARED (see Main.WorldMap.cs).
    /// </summary>
    void ShowStartWorld()
    {
        if (!AllowedStartWorlds().Contains(startWorld)) startWorld = 0;
        hud.ShowStartWorld(startWorld);
        hud.ShowClearedFlags(clearedWorlds);
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
        if (InVolcano) ArriveInVolcano(); // (a safe rock to start on: see Main.Volcano.cs)
    }

    /// <summary>
    /// How many worlds this game has reached after riding into world number n (it keeps counting: 7 is Sunny Hills again).
    /// A game from Sunny Hills: every world on the way counts, like always. A game that started in another world: see the
    /// fair-play rule at the top of this file. A game that used WORLD WARP on the pause screen counts the same fair way
    /// (see Main.Pause.cs), so warping somewhere and beating one boss can't win every postcard on the way.
    /// </summary>
    int WorldsReachedAfter(int n)
    {
        if (gameStartWorld == 0 && !warpedThisRun) return Math.Max(worldsReached, n + 1);
        int world = n % Worlds.All.Length;
        bool next = world <= Math.Max(worldsReached, farthestAtStart); // (the next world after one he had really reached)
        return next ? Math.Max(worldsReached, world + 1) : worldsReached;
    }

    /// <summary>The World Tour line on the results card. A game that started in another world says where it went.</summary>
    string ResultsWorldLine()
    {
        if (warpedThisRun) return $"World: {CurrentWorld.Name}"; // (a WORLD WARP game: where it ended)
        if (gameStartWorld == 0) return Worlds.WorldLine(worldsReached);
        string from = Worlds.All[gameStartWorld].Name;
        if (worldNumber == gameStartWorld) return $"World: {from}";
        if (worldNumber - gameStartWorld >= Worlds.All.Length) return "You went all the way around the world!";
        return $"You rode from {from} to {CurrentWorld.Name}!";
    }
}
