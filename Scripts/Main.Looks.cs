using Godot;

/// <summary>
/// The Bolt Bank and Bolt-E's looks! (This is more of Main: "partial" means one class spread over several files.)
///
/// Every bolt a player has ever grabbed goes into their Bolt Bank, which never goes down. Bolts, Big Rusty wins and
/// the World Tour win new looks for Bolt-E (they're all in Looks.cs). On the menus he stands in the corner on the left,
/// and LEFT / RIGHT (or the arrows under him) change his look.
/// After a game the bank counts up, and every new look gets an unlock party: NEW LOOK!, confetti and a jingle, and
/// Bolt-E puts it on. Parties are never lost: a look only counts as "seen" once its party has really happened, so if
/// you start the next game too fast, the party happens on the next menu instead. (The same goes for the first-launch
/// note "5 NEW LOOKS! Your old games count!": cut short, it comes back.)
/// Each player has their own bank and look (saved in the database's settings, see ScoreDatabase.cs).
/// </summary>
public partial class Main
{
    // ---- The Bolt Bank and the looks (try changing these!) ----
    const float MenuHomeX = 172f;            // where Bolt-E stands on the menus (in the strip on the left)
    const float PartyEvery = 1.6f;           // seconds between two unlock parties
    const float FirstPartyWait = 1f;         // on the title screen, the first party waits a moment
    const float LookRebuildSeconds = 0.8f;   // in pieces? He zips back together this fast, wearing his new look
    const float CountUpSeconds = 1.5f;       // how long the bank takes to count up after a game...
    const float CountUpSlowest = 60f;        // ...but it always counts at least this many bolts every second
    const float CountUpTickEvery = 0.05f;    // seconds between two "ticks" while it counts up
    const float FirstNoteReadSeconds = 2.5f; // "5 NEW LOOKS! Your old games count!" has to show this long before its looks count as seen
    static readonly Color LockedShadow = new(0.12f, 0.12f, 0.18f); // a look that isn't won yet shows as a dark shadow
    static readonly Color[] ConfettiColors = { new(1f, 0.85f, 0.2f), new(1f, 0.45f, 0.7f), new(0.35f, 0.9f, 1f), new(0.45f, 1f, 0.5f) };

    PlayerStats? playerStats;                // the loaded player's all-time numbers (null without a database)
    int farthestWorld = 1;                   // the most worlds the loaded player ever rode through in one game
    int lookIndex;                           // the look Bolt-E is wearing (or trying on), in Looks.All
    string bankKey = "";                     // whose Bolt Bank is showing: their name in small letters (see PlayerKey)
    float bankShown, bankTarget;             // the number the Bolt Bank shows, and the number it's counting up to
    float bankFrom;                          // the number the count-up started from (the bank before the game)
    float tickTimer;                         // seconds until the next "tick" of the count-up
    float unlockTimer;                       // seconds until the next unlock party can start
    bool bankCounting;                       // true while the Bolt Bank counts up after a game
    readonly Queue<Look> unlockQueue = new(); // new looks waiting in line for their unlock party
    string? firstNoteLooks;                  // the looks the first-launch note is showing, like "classic,party,bubblegum"...
    string firstNoteKey = "";                // ...for this player (their looks_seen key)...
    float firstNoteTimer;                    // ...and how long it has been showing (they count as seen after FirstNoteReadSeconds)

    /// <summary>A player's name in small letters, so "Sam", "SAM" and "sam " are the same player.</summary>
    static string PlayerKey(string name) => name.Trim().ToLowerInvariant();

    /// <summary>The loaded player's Bolt Bank: every bolt they ever grabbed.</summary>
    int Bank => playerStats?.TotalBolts ?? 0;

    /// <summary>Where the loaded player's unlock parties are remembered: "looks_seen:sam" = "classic,party,cowboy".</summary>
    string SeenKey => "looks_seen:" + bankKey;

    /// <summary>True on the menus, where LEFT / RIGHT change Bolt-E's look: the title screen, or once the results card shows.</summary>
    bool MenuInputOn => state == GameState.Title || (state == GameState.GameOver && gameOverTimer > ResultsDelay && showResults is null);

    /// <summary>Has the loaded player won this look? (Without a database, only the classic look.)</summary>
    bool Unlocked(Look look)
    {
        if (database is null) return look == Looks.Classic;
        return look.By switch
        {
            UnlockBy.Bolts => Bank >= look.Need,
            UnlockBy.BossesBeaten => (playerStats?.BossesBeaten ?? 0) >= look.Need,
            UnlockBy.World => farthestWorld >= look.Need,
            _ => false,
        };
    }

    /// <summary>What it takes to win a look, like "Need 120 more bolts!" ("" once it's won).</summary>
    string Goal(Look look)
    {
        if (Unlocked(look)) return "";
        int beaten = Math.Min(playerStats?.BossesBeaten ?? 0, look.Need);
        return look.By switch
        {
            UnlockBy.Bolts => look.Need - Bank == 1 ? "Need 1 more bolt!" : $"Need {look.Need - Bank} more bolts!",
            UnlockBy.BossesBeaten => $"Beat {Boss.DisplayName} {(look.Need == 1 ? "once" : $"{look.Need} times")} ({beaten}/{look.Need})",
            UnlockBy.World => $"Ride to {Worlds.All[Math.Clamp(look.Need, 1, Worlds.All.Length) - 1].Name}!",
            _ => "",
        };
    }

    // ---------- Loading a player ----------

    /// <summary>
    /// Stops the count-up and every unlock party still waiting (a new game, a different player...). A first-launch note
    /// that was cut short doesn't count as seen, so it comes back next time.
    /// </summary>
    void CancelCelebrations()
    {
        bankCounting = false;
        unlockQueue.Clear();
        firstNoteLooks = null;
        hud.HideUnlock();
    }

    /// <summary>
    /// Loads the player who's playing (see Main.Family.cs): their Bolt Bank, their look and their World Tour postcards.
    /// With celebrate = true, unlock parties that are still waiting happen too (one by one).
    /// The very first time a player is loaded, the looks their old games won are all announced at once.
    /// (While a name is still being typed, celebrate is false: half a name must never use up somebody else's parties.)
    /// </summary>
    void LoadPlayer(bool celebrate = true)
    {
        CancelCelebrations();
        string name = hud.PlayerName;
        bankKey = PlayerKey(name);
        playerStats = database?.StatsFor(name);
        farthestWorld = database?.FarthestWorld(name) ?? 1;

        PutOn(SavedLookIndex());
        ShowBankNumber(Bank, counting: false);
        hud.ShowPostcards(Math.Min(Worlds.All.Length, farthestWorld)); // this player's World Tour postcards

        unlockTimer = celebrate ? FirstPartyWait : 0;
        if (!celebrate || database is null) return;
        if (database.GetSetting(SeenKey) is null) FirstTimeForThisPlayer();
        else EnqueueUnseenLooks();
    }

    /// <summary>
    /// The first time a player is loaded: the looks their old games already won get one big announcement
    /// ("5 NEW LOOKS!  Your old games count!") instead of a party each. They only count as seen once the note has
    /// been showing for 2.5 seconds (see UpdateLooks): if a game starts (or another player is picked) before that,
    /// nothing is saved, and the note comes back on the next menu, or on the results card after the game.
    /// (A name that never played is skipped, so typing a name letter by letter doesn't save anything.)
    /// </summary>
    void FirstTimeForThisPlayer()
    {
        if ((playerStats?.GamesPlayed ?? 0) == 0) return;
        var won = Looks.All.Where(Unlocked).ToList();
        string wonIds = string.Join(",", won.Select(look => look.Id)); // like "classic,party,bubblegum"
        int newLooks = won.Count(look => look != Looks.Classic);
        if (newLooks == 0)
        {
            database?.SetSetting(SeenKey, wonIds); // (nothing new to announce: just remember the classic look as seen)
            return;
        }
        hud.ShowUnlock(newLooks == 1 ? "1 NEW LOOK!" : $"{newLooks} NEW LOOKS!", "Your old games count!");
        sounds.Play(Sfx.Unlock);
        firstNoteLooks = wonIds;
        firstNoteKey = SeenKey;
        firstNoteTimer = 0;
    }

    /// <summary>The look this player saved (the classic look if they never picked one, or haven't won it).</summary>
    int SavedLookIndex()
    {
        int index = Looks.IndexOf(database?.GetSetting("look:" + bankKey));
        return index >= 0 && Unlocked(Looks.All[index]) ? index : 0;
    }

    /// <summary>The looks this player already had an unlock party for, in the order they had them.</summary>
    List<string> SeenLooks() => (database?.GetSetting(SeenKey) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

    void SaveSeen(IEnumerable<string> ids) => database?.SetSetting(SeenKey, string.Join(",", ids));

    /// <summary>Every look this player has won but never had a party for gets in line for one (in the picker's order).</summary>
    void EnqueueUnseenLooks()
    {
        var seen = SeenLooks();
        foreach (var look in Looks.All)
            if (look != Looks.Classic && Unlocked(look) && !seen.Contains(look.Id) && !unlockQueue.Contains(look))
                unlockQueue.Enqueue(look);
    }

    /// <summary>
    /// A new game starts. (A different name? Then that player is loaded first.) A look he was only trying on comes off,
    /// and he wears his saved look. The very first game of a brand-new name remembers the looks won so far (just the
    /// classic one) as seen, so the looks this new player wins later get a real party, not "Your old games count!".
    /// </summary>
    void GetLookReadyToRide()
    {
        if (PlayerKey(hud.PlayerName) != bankKey) LoadPlayer(celebrate: false);
        if (!Unlocked(Looks.All[lookIndex])) PutOn(SavedLookIndex());
        robot.SelfModulate = Colors.White;
        bool brandNew = (playerStats?.GamesPlayed ?? 0) == 0;
        if (database is not null && brandNew && database.GetSetting(SeenKey) is null)
            SaveSeen(Looks.All.Where(Unlocked).Select(look => look.Id));
    }

    // ---------- Wearing and browsing looks ----------

    /// <summary>Bolt-E puts on look number "index" (a look he has won, so no dark shadow).</summary>
    void PutOn(int index)
    {
        lookIndex = index;
        Looks.Apply(robot, Looks.All[index]);
        robot.SelfModulate = Colors.White;
        ShowLookInPicker();
    }

    void ShowLookInPicker()
    {
        var look = Looks.All[lookIndex];
        hud.ShowLook(look.Name, !Unlocked(look), Goal(look), lookIndex + 1, Looks.All.Length);
    }

    /// <summary>
    /// LEFT / RIGHT on the menus: Bolt-E tries on the look before (-1) or the next one (1), round and round.
    /// A look he has won is saved as his look (and if he's in pieces, he zips back together wearing it).
    /// A look he hasn't won yet shows as a dark shadow, and the picker says what it takes to win it.
    /// </summary>
    void BrowseLook(int step)
    {
        lookIndex = Mathf.PosMod(lookIndex + step, Looks.All.Length);
        sounds.Play(Sfx.Tick, 1.3f);
        var look = Looks.All[lookIndex];
        Looks.Apply(robot, look);
        if (Unlocked(look))
        {
            robot.SelfModulate = Colors.White;
            database?.SetSetting("look:" + PlayerKey(hud.PlayerName), look.Id);
            if (robot.Crashed) robot.Rebuild(LookRebuildSeconds);
        }
        else
        {
            robot.SelfModulate = LockedShadow;
        }
        ShowLookInPicker();
    }

    // ---------- The Bolt Bank ----------

    /// <summary>Shows the Bolt Bank with this many bolts in it, and the next prize those bolts are saving up for.</summary>
    void ShowBankNumber(int shown, bool counting)
    {
        var next = Looks.NextBoltPrize(shown);
        int lastPrize = Looks.LastBoltPrizeNeed(shown);
        string nextText = next is null ? "You got them ALL!" : $"Next: {next.Name} in {next.Need - shown}";
        float fraction = next is null ? 1 : (shown - lastPrize) / (float)(next.Need - lastPrize);
        hud.ShowBank(shown, counting ? (int)(bankTarget - bankFrom) : 0, nextText, fraction);
    }

    /// <summary>
    /// The results card is showing: Bolt-E's spot is the corner on the left again (he stays in pieces until he puts on
    /// a look), and the Bolt Bank counts up from "before" to "after" (with the bolts of the game that just ended).
    /// </summary>
    void StartBankCountUp(string name, int before, int after)
    {
        robot.HomeX = MenuHomeX;
        LoadPlayer(celebrate: false);
        bankKey = PlayerKey(name);
        bankFrom = bankShown = before;
        bankTarget = after;
        bankCounting = true;
        tickTimer = 0;
        ShowBankNumber(before, counting: true);
    }

    /// <summary>Every frame: the Bolt Bank counts up after a game, and the looks waiting in line get their parties.</summary>
    void UpdateLooks(float dt)
    {
        if (bankCounting && state == GameState.GameOver) CountUp(dt);

        // The first-launch note has been showing long enough to read it: now its looks count as seen
        if (firstNoteLooks is not null)
        {
            firstNoteTimer += dt;
            if (firstNoteTimer >= FirstNoteReadSeconds)
            {
                database?.SetSetting(firstNoteKey, firstNoteLooks);
                firstNoteLooks = null;
            }
        }

        // One party every 1.6 seconds, on the menus, for the player whose bank is showing (never while a name is typed)
        unlockTimer -= dt;
        if (unlockTimer <= 0 && unlockQueue.Count > 0 && MenuInputOn && !hud.IsTypingName && PlayerKey(hud.PlayerName) == bankKey)
        {
            Celebrate(unlockQueue.Dequeue());
            unlockTimer = PartyEvery;
        }
    }

    /// <summary>The Bolt Bank counts up (tick, tick, tick, higher and higher). Passing a prize starts its party!</summary>
    void CountUp(float dt)
    {
        float before = bankShown;
        float bolts = bankTarget - bankFrom;
        bankShown = Mathf.MoveToward(bankShown, bankTarget, Mathf.Max(CountUpSlowest, bolts / CountUpSeconds) * dt);

        // Passing the bolts a look needs: if it never had its party, it gets in line for one. (Unless this player's
        // first-launch note was never read: then that note comes at the end instead, and it shows this look too.)
        foreach (var look in Looks.All)
        {
            if (look.By != UnlockBy.Bolts || look.Need <= before || look.Need > bankShown) continue;
            if (FirstNoteNeverRead) continue;
            if (!SeenLooks().Contains(look.Id) && !unlockQueue.Contains(look)) unlockQueue.Enqueue(look);
        }

        if (bolts > 0)
        {
            tickTimer -= dt;
            if (tickTimer <= 0)
            {
                tickTimer = CountUpTickEvery;
                float progress = (bankShown - bankFrom) / bolts; // 0 = just started, 1 = done
                sounds.Play(Sfx.Tick, 1 + 0.5f * progress);
                ShowBankNumber((int)bankShown, counting: true);
            }
        }

        if (bankShown >= bankTarget)
        {
            bankCounting = false;
            ShowBankNumber((int)bankTarget, counting: false);
            // Looks won another way (Big Rusty wins, a world) get their party now too. If this player's first-launch note
            // was never read (the game was started too fast), "5 NEW LOOKS! Your old games count!" comes now instead.
            if (FirstNoteNeverRead) FirstTimeForThisPlayer();
            else EnqueueUnseenLooks();
        }
    }

    /// <summary>
    /// True when the loaded player has never had their first-launch note (no looks_seen yet), so their old games' looks
    /// still wait to be announced. (A brand-new player gets looks_seen when their first game starts: see GetLookReadyToRide.)
    /// </summary>
    bool FirstNoteNeverRead => database is not null && database.GetSetting(SeenKey) is null;

    /// <summary>
    /// An unlock party! "NEW LOOK!" with the look's name, a jingle, a rumble and confetti, and Bolt-E puts the look on
    /// (zipping back together first if he's in pieces). Only now does the look count as seen.
    /// </summary>
    void Celebrate(Look look)
    {
        hud.ShowUnlock("NEW LOOK!", look.Name);
        sounds.Play(Sfx.Unlock);
        Rumble(0.4f, 0.3f, 0.3f);
        for (int round = 0; round < 3; round++)
            foreach (var color in ConfettiColors)
                Burst(new Vector2(MenuHomeX, 330), 5, color, 240f + 60f * round);

        PutOn(Array.IndexOf(Looks.All, look));
        database?.SetSetting("look:" + PlayerKey(hud.PlayerName), look.Id);
        if (robot.Crashed) robot.Rebuild(LookRebuildSeconds);

        var seen = SeenLooks();
        if (!seen.Contains(look.Id))
        {
            seen.Add(look.Id);
            SaveSeen(seen);
        }
    }
}
