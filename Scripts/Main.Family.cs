using Godot;

/// <summary>
/// THE FAMILY RACE! (This is more of Main: "partial" means one class spread over several files.)
///
/// Who's playing? On the menus, LB / RB on the controller (or TAB, or the LB / RB buttons on the cards) switch between
/// everyone who has played, and Bolt-E's look, Bolt Bank and postcards switch too. TAB or clicking the name picks
/// NEW PLAYER, to type a new name. (A controller never picks NEW PLAYER, so it never gets stuck in the name box.)
/// Every game starts with "GO, MAX!", and when it's somebody else's turn: "DAD'S TURN!" and "Beat MAX's 1520 from today!".
///
/// Flags stand on the road where the best rides ended: YOUR RECORD (gold, with a star), LAST TIME (small and silver)
/// and everybody else's farthest ride ("DAD 812 m"). Zoom past DAD's flag and it tips over: YOU PASSED DAD!
/// Beat the Best score in the corner and NEW HIGH SCORE! cheers right away, in the middle of the ride.
/// After the game, the results card says who's champ today, who won the round, and "So close!" when a flag was near.
/// (The flags themselves are drawn by RecordFlag.cs.)
/// </summary>
public partial class Main
{
    // ---- The family race (try changing these!) ----
    const int KnownPlayersShown = 8;       // how many people LB / RB switch between
    const int ChampionsShown = 5;          // rows in "Family Champions" on the title card
    const int MinFlagMeters = 50;          // rides shorter than this get no flag
    const int MaxOtherFlags = 4;           // how many flags for other people (the best scores get one)
    const int LastTimeAway = 30;           // LAST TIME only gets a flag if it's more than this far from YOUR RECORD (meters)
    const int FlagsClose = 30;             // flags closer than this (meters) lift their words up, so the words don't overlap
    const int SoCloseMeters = 150;         // crash this close to a flag and the results card says "So close!"
    const float FireworksEvery = 0.2f;     // seconds between two fireworks

    static readonly Color TurnPink = new(1f, 0.45f, 0.6f);          // "GO, MAX!" and "DAD'S TURN!"
    static readonly Color RecordGold = new(1f, 0.8f, 0.15f);        // YOUR RECORD's flag (and NEW HIGH SCORE!)
    static readonly Color LastTimeSilver = new(0.8f, 0.82f, 0.88f); // LAST TIME's flag
    // Everybody else's flag gets one of these colors (always the same one for the same name)
    static readonly Color[] FlagColors =
    {
        new(0.3f, 0.6f, 1f), new(0.95f, 0.35f, 0.35f), new(0.4f, 0.85f, 0.4f), new(0.75f, 0.45f, 0.95f), new(1f, 0.55f, 0.2f),
    };
    static readonly Color[] FireworkColors = { new(1f, 0.85f, 0.2f), new(1f, 0.45f, 0.7f), new(0.35f, 0.9f, 1f), Colors.White };

    /// <summary>A flag that waits to be put on the road until Bolt-E gets close to it.</summary>
    record FlagPlan(string Label, int Meters, FlagKind Kind, Color Color, float LabelLift);

    readonly List<FlagPlan> flagsToPlant = new();          // this game's flags, still waiting (nearest first)
    readonly List<RecordFlag> flags = new();               // flags standing on the road
    string? previousPlayer;                                // who rode the game before this one (for "DAD'S TURN!")
    (string Name, int Score)? lastRun;                     // the last finished game (for who won the round)
    readonly Dictionary<string, int> roundsWon = new();    // rounds won since the game was switched on (name in small letters)
    bool highScoreCheered;                                 // NEW HIGH SCORE! only cheers once a game
    int fireworkBursts;                                    // how many fireworks went off (the self-test reads this)

    // ---------- Who's playing? ----------

    /// <summary>
    /// LB / RB (or TAB, or a click on the LB / RB buttons): the player before (-1) or the next one (1).
    /// A tick when it changes (with only one person to pick, nothing changes, so there's no tick).
    /// </summary>
    void PickPlayer(int step, bool canPickNew)
    {
        if (hud.ChangePlayer(step, canPickNew)) sounds.Play(Sfx.Tick, 0.9f);
    }

    /// <summary>
    /// START, A, Space, Enter or a click on the play button: a new game! But on the results card with NEW PLAYER
    /// picked, it goes to the title card, where the name box is ready to type the new name.
    /// </summary>
    void PlayPressed()
    {
        if (state == GameState.GameOver && hud.NewPlayerPicked) GoToTitle(typing: true);
        else StartGame();
    }

    /// <summary>
    /// A game starts: "GO, MAX!" (but not for "Player", the name for games without a name). When somebody else played
    /// the game before: "DAD'S TURN!", and if somebody else has a better score today, who to beat.
    /// </summary>
    void SayWhoseTurn(string? before)
    {
        string name = hud.PlayerName;
        string big = Hud.ShortName(name, 10).ToUpperInvariant();
        if (before is not null && PlayerKey(before) != PlayerKey(name))
        {
            hud.ShowBanner($"{big}'S TURN!", TurnPink);
            var today = database?.TodaysBests() ?? new();
            int mine = today.Where(best => PlayerKey(best.Name) == PlayerKey(name)).Select(best => best.Score).FirstOrDefault();
            var rival = today.FirstOrDefault(best => PlayerKey(best.Name) != PlayerKey(name)); // (the best one first)
            if (rival.Name is not null && rival.Score > mine)
                hud.ShowHint($"Beat {Hud.ShortName(rival.Name, 10)}'s {rival.Score} from today!");
        }
        else if (PlayerKey(name) != "player")
        {
            hud.ShowBanner($"GO, {big}!", TurnPink);
        }
    }

    /// <summary>Taking turns: when somebody else played the game before this one, the higher score wins the round.</summary>
    string? DecideRound(string player, int score)
    {
        string? winner = null;
        if (lastRun is { } before && PlayerKey(before.Name) != PlayerKey(player) && score != before.Score)
        {
            winner = score > before.Score ? player : before.Name;
            roundsWon[PlayerKey(winner)] = roundsWon.GetValueOrDefault(PlayerKey(winner)) + 1;
        }
        lastRun = (player, score);
        return winner;
    }

    /// <summary>
    /// Who's champ today (when 2 or more people played today), like "Today: MAX 1520 (champ!)  -  DAD 980",
    /// and who won the round. null = nobody else played today (then the results card shows the usual line).
    /// </summary>
    static string? TodayLine(List<(string Name, int Score)> today, string? roundWinner)
    {
        if (today.Count < 2) return null;
        var people = today.Take(3).Select((best, i) => $"{Hud.ShortName(best.Name, 10)} {best.Score}" + (i == 0 ? " (champ!)" : ""));
        string line = "Today: " + string.Join("  -  ", people);
        if (roundWinner is not null) line += $"  -  Round to {Hud.ShortName(roundWinner, 10)}!";
        return line;
    }

    // ---------- The flags on the road ----------

    /// <summary>
    /// Gets this game's flags ready: YOUR RECORD (the farthest this player ever rode), LAST TIME (where their last ride
    /// ended, if it's not right next to the record) and up to 4 flags for everybody else's farthest ride (the best
    /// scores first). Rides under 50 m get no flag. They go on the road later, when Bolt-E gets close (see UpdateFlags).
    /// </summary>
    void BuildFlags()
    {
        flagsToPlant.Clear();
        string name = hud.PlayerName;
        var plans = new List<FlagPlan>();

        int record = database?.BestDistanceFor(name) ?? 0;
        if (record >= MinFlagMeters) plans.Add(new FlagPlan("YOUR RECORD", record, FlagKind.MyRecord, RecordGold, 0));
        int last = database?.LastDistanceFor(name) ?? 0;
        if (last >= MinFlagMeters && Math.Abs(last - record) > LastTimeAway)
            plans.Add(new FlagPlan("LAST TIME", last, FlagKind.LastTime, LastTimeSilver, 0));

        int others = 0;
        foreach (var person in database?.PlayerBests(50) ?? new()) // (50 people is plenty to find the 4 best)
        {
            if (others == MaxOtherFlags) break;
            if (PlayerKey(person.Name) == PlayerKey(name) || person.FarthestM < MinFlagMeters) continue;
            plans.Add(new FlagPlan(Hud.ShortName(person.Name, 10).ToUpperInvariant(), person.FarthestM, FlagKind.Other, FlagColorFor(person.Name), 0));
            others++;
        }

        // Nearest first. A flag close to the one before it lifts its words up (30, then 60), so the words don't overlap.
        plans.Sort((a, b) => a.Meters.CompareTo(b.Meters));
        for (int i = 1; i < plans.Count; i++)
            if (plans[i].Meters - plans[i - 1].Meters <= FlagsClose)
                plans[i] = plans[i] with { LabelLift = (plans[i - 1].LabelLift + 30) % 90 };
        flagsToPlant.AddRange(plans);
    }

    /// <summary>Everybody's flag color: add up the letters of their name (in small letters) and pick one of the 5 colors.</summary>
    static Color FlagColorFor(string name) => FlagColors[name.ToLowerInvariant().Sum(letter => (int)letter) % FlagColors.Length];

    /// <summary>
    /// Every frame while playing (and while Bolt-E is in pieces, because the road still slides along): a flag that's
    /// almost on the screen goes on the road, behind everything else. Then, only while playing, a flag Bolt-E has
    /// passed (by the meters, so riding around in a Big Rusty fight never counts) gets its cheer.
    /// </summary>
    void UpdateFlags()
    {
        for (int i = flagsToPlant.Count - 1; i >= 0; i--)
        {
            var plan = flagsToPlant[i];
            float x = RobotX + plan.Meters * PixelsPerMeter - distance; // (where its meters are on the screen)
            if (x >= ScreenWidth + 120) continue;
            var flag = new RecordFlag
            {
                Label = plan.Label, Meters = plan.Meters, Kind = plan.Kind, FlagColor = plan.Color, LabelLift = plan.LabelLift,
                Position = new Vector2(x, GroundY),
            };
            AddChild(flag);
            MoveChild(flag, 0); // the first thing drawn, so it stands behind the obstacles and Bolt-E
            flags.Add(flag);
            flagsToPlant.RemoveAt(i);
        }

        if (state != GameState.Playing) return;
        foreach (var flag in flags)
            if (!flag.Passed && Meters >= flag.Meters) PassFlag(flag);
    }

    /// <summary>
    /// Bolt-E zoomed past a flag! Somebody else's: it tips over with X eyes, confetti, a buzz and YOU PASSED DAD!
    /// LAST TIME: a little "BEAT LAST TIME!". YOUR RECORD: it tips over, NEW RECORD!, fireworks, star eyes and a flip.
    /// </summary>
    void PassFlag(RecordFlag flag)
    {
        flag.Passed = true;
        switch (flag.Kind)
        {
            case FlagKind.Other:
                flag.KnockOver();
                hud.ShowBanner($"YOU PASSED {flag.Label}!", flag.FlagColor);
                sounds.Play(Sfx.PassFlag);
                Rumble(0.4f, 0.1f, 0.25f);
                for (int i = 0; i < 3; i++)
                    Burst(flag.PoleTop, 12, Color.FromHsv(i / 3f, 0.75f, 1f), 260f); // rainbow confetti, from the top of the pole
                robot.BeHappy();
                robot.ShowFace(RobotFace.Stars, 1.5f);
                break;
            case FlagKind.LastTime:
                popups.Add("BEAT LAST TIME!", robot.Position + new Vector2(0, -170), LastTimeSilver, 30);
                sounds.Play(Sfx.Bolt, 1.5f);
                break;
            case FlagKind.MyRecord:
                flag.KnockOver();
                hud.ShowBanner("NEW RECORD!", flag.FlagColor);
                sounds.Play(Sfx.PassFlag);
                Fireworks();
                robot.ShowFace(RobotFace.Stars, 2f);
                robot.Flip();
                break;
        }
    }

    /// <summary>A crash with a flag still close ahead: "So close! Only 42 m to DAD's flag!" ("" when no flag is that close).</summary>
    string SoCloseLine(int meters)
    {
        var ahead = flags.Where(flag => !flag.Passed).Select(flag => (flag.Label, flag.Meters, flag.Kind))
            .Concat(flagsToPlant.Select(plan => (plan.Label, plan.Meters, plan.Kind)))
            .Where(flag => flag.Kind != FlagKind.LastTime && flag.Meters > meters && flag.Meters - meters <= SoCloseMeters)
            .OrderBy(flag => flag.Meters)
            .ToList();
        if (ahead.Count == 0) return "";
        var nearest = ahead[0];
        int togo = nearest.Meters - meters;
        return nearest.Kind == FlagKind.MyRecord
            ? $"So close! Only {togo} m to YOUR RECORD!"
            : $"So close! Only {togo} m to {nearest.Label}'s flag!";
    }

    /// <summary>Takes every flag off the road (a new game, the title screen).</summary>
    void ClearFlags()
    {
        foreach (var flag in flags) flag.QueueFree();
        flags.Clear();
        flagsToPlant.Clear();
    }

    // ---------- NEW HIGH SCORE! ----------

    /// <summary>
    /// Every frame while playing: the moment the score beats the Best number in the corner, NEW HIGH SCORE! cheers right
    /// away (once a game), with the high score jingle, the Best number flashing gold, and fireworks.
    /// (Not in the very first game ever: then there's no Best yet.)
    /// </summary>
    void CheckForHighScore()
    {
        if (highScoreCheered || state != GameState.Playing || bestScore <= 0 || Score <= bestScore) return;
        highScoreCheered = true;
        hud.ShowBanner("NEW HIGH SCORE!", RecordGold);
        sounds.Play(Sfx.HighScore);
        hud.FlashBest();
        Fireworks();
    }

    /// <summary>4 fireworks, one after another, up in the sky: gold, pink, sky blue and white.</summary>
    void Fireworks()
    {
        for (int i = 0; i < FireworkColors.Length; i++)
        {
            var color = FireworkColors[i];
            Later(i * FireworksEvery, () =>
            {
                Burst(new Vector2(Rand(200, ScreenWidth - 200), Rand(90, 300)), 24, color, 320f);
                fireworkBursts++;
            });
        }
    }
}
