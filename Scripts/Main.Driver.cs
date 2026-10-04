using Godot;

/// <summary>
/// DAD DRIVES BIG RUSTY! (This is more of Main: "partial" means one class spread over several files.)
///
/// When Big Rusty shows up, Dad (or Mom) can press a number key on the keyboard to take over:
///   1 = fireballs, 2 = drop Pup-Bot helpers (once he's ANGRY), 3 = GROUND POUND, 4 = giant laser (only when FURIOUS),
///   H = "HA HA HA!"
/// "DAD IS DRIVING BIG RUSTY!" shows, Rusty laughs and puts on a little blue cap, and his bar says "BIG RUSTY (DAD)".
/// Orders only work while three thinking dots bob over his head (he's waiting for an order); otherwise it says "Wait...".
/// Fair-play rules keep the fight winnable (they live in Boss.Request): after 2 attacks he's TIRED and only the ground
/// pound works, and after every ground pound he's dizzy, so Bolt-E can stomp him. Dad can make him laugh only 2 times
/// in a row (then a real move has to come first), so laughing can't stop the fight. If Dad picks nothing for 3 seconds,
/// Rusty's own brain picks the next move. The kid keeps playing on the controller exactly like always.
/// </summary>
public partial class Main
{
    const string DriverName = "DAD"; // change to MOM, GRANDMA...

    // ---- Dad's little order words ("Wait...", "TIRED! Press 3!", "Not yet!") (try changing these!) ----
    const int OrderWordsSize = 28;            // how big they are
    const float OrderWordsAbove = 260f;       // how far above Big Rusty they pop up
    const float SameWordsAgainAfter = 0.5f;   // pressing again sooner doesn't stack the same words on top of each other

    /// <summary>The driver's name, short enough to fit in the big words (see Hud.ShortName).</summary>
    static string DriverShortName => Hud.ShortName(DriverName, 10);

    static readonly Color DriverOrange = new(1f, 0.6f, 0.15f);

    /// <summary>Which key asks Big Rusty for which move. (The keys themselves are set up in SetUpControls.)</summary>
    static readonly (string Action, BossMove Move)[] DriveKeys =
    {
        ("drive_fireballs", BossMove.Fireballs), // 1
        ("drive_helpers", BossMove.Helpers),     // 2
        ("drive_pound", BossMove.Pound),         // 3
        ("drive_laser", BossMove.Laser),         // 4
        ("drive_laugh", BossMove.Laugh),         // H
    };

    /// <summary>True when Dad was driving Big Rusty and the game ended before Bolt-E beat him: "DAD GOT YOU!"</summary>
    bool DriverGotYou => boss is { DriverControlled: true } && !boss.IsBeaten;

    /// <summary>
    /// Called with every key press. In a Big Rusty fight, the keys 1 to 4 and H drive him: the first press takes over,
    /// and every press asks him for a move. Returns true if it was one of Dad's keys (then nothing else happens with it).
    /// </summary>
    bool DriverKeyPressed(InputEvent e)
    {
        if (state != GameState.Playing || boss is not { CanBeDriven: true } || !BossFightActive) return false;
        foreach (var (action, move) in DriveKeys)
        {
            if (!e.IsActionPressed(action)) continue;
            if (!boss.DriverControlled) TakeOverBigRusty();
            GiveOrder(move);
            return true;
        }
        return false;
    }

    /// <summary>Dad takes over! Big words, a laugh, a blue cap on Rusty's head, and Dad's keys as a tip.</summary>
    void TakeOverBigRusty()
    {
        boss!.TakeControl();
        hud.ShowBanner($"{DriverShortName} IS DRIVING {Boss.DisplayName}!", DriverOrange);
        hud.SetBossBarName($"{Boss.DisplayName} ({DriverShortName})");
        hud.ShowHint("1 fireballs  2 helpers  3 POUND  4 laser  H laugh");
        sounds.Play(Sfx.Laugh);
    }

    /// <summary>Asks Big Rusty for a move. OK: a little tick. Otherwise little words over his head say why not.</summary>
    void GiveOrder(BossMove move)
    {
        string words;
        switch (boss!.Request(move))
        {
            case DriveAnswer.Ok:
                sounds.Play(Sfx.Tick, 0.8f);
                return;
            case DriveAnswer.Busy:
                words = "Wait...";
                break;
            case DriveAnswer.Tired:
                words = "TIRED! Press 3!";
                break;
            default: // NotYet
                words = "Not yet!";
                break;
        }
        // (Pressing a key again and again while he's busy doesn't pile up a stack of the same words)
        if (popups.JustAdded(words, SameWordsAgainAfter)) return;
        popups.Add(words, OrderWordsSpot(words), DriverOrange, OrderWordsSize);
    }

    /// <summary>
    /// Where Dad's little words pop up: over Big Rusty's head. But they float up for a second, and they must never cover
    /// his health bar at the top of the screen. So in the middle of the screen (under the bar) they start low enough to
    /// stay below it all the way. When he's way up high for a ground pound, they go there too: in the sky, over his shadow.
    /// </summary>
    Vector2 OrderWordsSpot(string words)
    {
        var spot = new Vector2(boss!.Position.X, boss.Position.Y - OrderWordsAbove); // over his head
        var font = ThemeDB.FallbackFont;
        float halfWide = font.GetStringSize(words, HorizontalAlignment.Left, -1, OrderWordsSize).X / 2 + 8; // (+ the dark outline)
        var bar = hud.BossBarRect;
        bool underTheBar = spot.X + halfWide > bar.Position.X && spot.X - halfWide < bar.End.X;
        bool wayUpHigh = spot.Y < 90; // (up for a ground pound: his head is above the top of the screen)
        // The lowest the words may start: after floating up for a whole second, the top of the letters (and their dark
        // outline) is still 10 pixels below the bar.
        float belowTheBar = bar.End.Y + 10 + ScorePopups.FloatUp + font.GetAscent(OrderWordsSize) + 4;
        if (underTheBar || wayUpHigh) spot.Y = Mathf.Max(spot.Y, belowTheBar);
        return spot;
    }
}
