using Godot;

/// <summary>
/// DAD DRIVES BIG RUSTY! (This is more of Main: "partial" means one class spread over several files.)
///
/// Normally the computer drives Big Rusty, just like always. But plug in a SECOND game controller, and in a boss fight
/// Dad (or Mom) can press one of its buttons to take over:
///   X = fireballs, Y = drop Pup-Bot helpers (once he's ANGRY), A = GROUND POUND, B = giant laser (only when FURIOUS),
///   LB or RB = "HA HA HA!"
/// "DAD IS DRIVING BIG RUSTY!" shows, Rusty laughs and puts on a little blue cap, and his bar says "BIG RUSTY (DAD)".
/// Orders only work while three thinking dots bob over his head (he's waiting for an order); otherwise it says "Wait...".
/// Fair-play rules keep the fight winnable (they live in Boss.Request): after 2 attacks he's TIRED and only the ground
/// pound works, and after every ground pound he's dizzy, so Bolt-E can stomp him. Dad can make him laugh only 2 times
/// in a row (then a real move has to come first), so laughing can't stop the fight. If Dad picks nothing for 3 seconds,
/// Rusty's own brain picks the next move.
/// The FIRST controller is always Bolt-E's (the kid's: see PickControllerOne in Main.cs). The second controller never
/// moves Bolt-E: it only drives Big Rusty, and only in a boss fight. If nobody touches it, the computer keeps driving,
/// so a spare controller lying around changes nothing. Only a real game controller can drive him: a phantom joystick
/// or a steering wheel that Godot doesn't know does nothing (see PadIsKnown in Main.cs).
/// Dad's tip and his "TIRED! Press A!" words are made from DriveButtons, so they stay right if you swap the buttons.
/// </summary>
public partial class Main
{
    const string DriverName = "DAD"; // change to MOM, GRANDMA...

    // ---- Dad's little order words ("Wait...", "TIRED! Press A!", "Not yet!") (try changing these!) ----
    const int OrderWordsSize = 28;            // how big they are
    const float OrderWordsAbove = 260f;       // how far above Big Rusty they pop up
    const float SameWordsAgainAfter = 0.5f;   // pressing again sooner doesn't stack the same words on top of each other

    /// <summary>The driver's name, short enough to fit in the big words (see Hud.ShortName).</summary>
    static string DriverShortName => Hud.ShortName(DriverName, 10);

    static readonly Color DriverOrange = new(1f, 0.6f, 0.15f);

    /// <summary>
    /// Which button on the second controller asks Big Rusty for which move. Every other button does nothing.
    /// (Try swapping them around! Dad's tip and his "TIRED!" words follow along.)
    /// </summary>
    static readonly (JoyButton Button, BossMove Move)[] DriveButtons =
    {
        (JoyButton.X, BossMove.Fireballs),
        (JoyButton.Y, BossMove.Helpers),
        (JoyButton.A, BossMove.Pound),
        (JoyButton.B, BossMove.Laser),
        (JoyButton.LeftShoulder, BossMove.Laugh),  // LB
        (JoyButton.RightShoulder, BossMove.Laugh), // RB
    };

    /// <summary>What the buttons are called: the letters printed on the controller.</summary>
    static readonly Dictionary<JoyButton, string> ButtonNames = new()
    {
        [JoyButton.X] = "X",
        [JoyButton.Y] = "Y",
        [JoyButton.A] = "A",
        [JoyButton.B] = "B",
        [JoyButton.LeftShoulder] = "LB",
        [JoyButton.RightShoulder] = "RB",
    };

    /// <summary>Dad's tip names the moves in this order, with these words.</summary>
    static readonly (BossMove Move, string Words)[] TipWords =
    {
        (BossMove.Fireballs, "fireballs"),
        (BossMove.Helpers, "helpers"),
        (BossMove.Pound, "POUND"),
        (BossMove.Laser, "laser"),
        (BossMove.Laugh, "laugh"),
    };

    /// <summary>
    /// The name of the first button in DriveButtons that asks for this move, like "A" for the ground pound
    /// (or null if no button does). A button that isn't in ButtonNames gets Godot's name for it, like "DpadUp".
    /// </summary>
    static string? ButtonFor(BossMove move)
    {
        foreach (var (button, buttonMove) in DriveButtons)
            if (buttonMove == move) return ButtonNames.GetValueOrDefault(button, button.ToString());
        return null;
    }

    /// <summary>
    /// Dad's tip when he takes over, made from DriveButtons: each move once, with its first button, like
    /// "X fireballs  Y helpers  A POUND  B laser  LB laugh". (A move that no button does isn't in it.)
    /// </summary>
    static string DriveTip()
    {
        var tip = new List<string>();
        foreach (var (move, words) in TipWords)
            if (ButtonFor(move) is string button) tip.Add($"{button} {words}"); // like "X fireballs"
        return string.Join("  ", tip);
    }

    /// <summary>What a TIRED Big Rusty says: which button makes him do his ground pound, like "TIRED! Press A!".</summary>
    static string TiredWords() => ButtonFor(BossMove.Pound) is string pound ? $"TIRED! Press {pound}!" : "TIRED!";

    int driverPad = -1; // the second controller that's driving Big Rusty in this fight (-1 = nobody)

    /// <summary>True when Dad was driving Big Rusty and the game ended before Bolt-E beat him: "DAD GOT YOU!"</summary>
    bool DriverGotYou => boss is { DriverControlled: true } && !boss.IsBeaten;

    /// <summary>
    /// Everything a SECOND controller does (any controller that isn't Bolt-E's: see _UnhandledInput). It never moves
    /// Bolt-E. It only drives Big Rusty, and only in a boss fight: the first button press takes over, and every press
    /// asks him for a move. Letting go of a button, the stick, and the buttons that aren't Dad's do nothing.
    /// A phantom controller that Godot doesn't know (a pretend joystick, a steering wheel...) can't drive him at all.
    /// (Only Big Rusty can be driven: VROOMBA's CanBeDriven is always false, so in his fights these buttons do nothing.)
    /// </summary>
    void SecondControllerInput(InputEvent e)
    {
        if (e is not InputEventJoypadButton { Pressed: true } press) return;
        if (!PadIsKnown(press.Device)) return; // (only a real game controller can drive Big Rusty)
        if (state != GameState.Playing || boss is not { CanBeDriven: true } || !BossFightActive) return;
        foreach (var (button, move) in DriveButtons)
        {
            if (press.ButtonIndex != button) continue;
            if (!boss.DriverControlled) TakeOverBigRusty(press.Device);
            GiveOrder(move);
            return;
        }
    }

    /// <summary>Dad takes over! Big words, a laugh, a blue cap on Rusty's head, and Dad's buttons as a tip.</summary>
    void TakeOverBigRusty(int pad)
    {
        driverPad = pad;
        boss!.TakeControl();
        hud.ShowBanner($"{DriverShortName} IS DRIVING {boss.DisplayName}!", DriverOrange);
        hud.SetBossBarName($"{boss.DisplayName} ({DriverShortName})");
        hud.ShowHint(DriveTip());
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
                words = TiredWords(); // "TIRED! Press A!"
                break;
            default: // NotYet
                words = "Not yet!";
                break;
        }
        // (Pressing a button again and again while he's busy doesn't pile up a stack of the same words)
        if (popups.JustAdded(words, SameWordsAgainAfter)) return;
        popups.Add(words, OrderWordsSpot(words), DriverOrange, OrderWordsSize);
    }

    /// <summary>Bolt-E stomped Dad's Big Rusty: Dad's controller gives a short buzz too, so he feels the hit!</summary>
    void BuzzTheDriver()
    {
        if (driverPad >= 0) Buzz(driverPad, 0.5f, 0.5f, 0.2f);
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
