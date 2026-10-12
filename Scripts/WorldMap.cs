using Godot;

/// <summary>Which WORLD MAP: before a game (pick a world to start in), or between two worlds (the next world comes).</summary>
public enum MapMode { Start, Between }

/// <summary>The steps of the map's show, in the order they come (see WorldMap).</summary>
public enum MapStep { Hidden, IrisClose, Black, IrisOpen, FlagUp, PathReveal, CloudPoof, AllClear, Sparkle, Travel, Ready, Stepping, GoIris, OverGame }

/// <summary>How a world looks on the map: LOCKED (a cloud with a "?"), OPEN (a red dot) or CLEARED (a blue dot and a gold flag).</summary>
public enum MapNodeState { Locked, Open, Cleared }

/// <summary>How little Bolt-E travels a road: riding, by rocket, in a space bubble, by space capsule, surfing, or a STAR HOP.</summary>
public enum Ride { Road, Rocket, Space, Capsule, Sea, StarHop }

/// <summary>Everything Main tells the map when it opens (Main.WorldMap.cs fills it in).</summary>
public sealed class MapVisit
{
    public MapMode Mode;
    public int Here;                 // the world little Bolt-E stands on when the map opens
    public int GoingTo = -1;         // between levels: the world he travels to (on the start map: -1)
    public int Next = -1;            // the world the NEXT arrow points at (-1 = none)
    public readonly MapNodeState[] Before = new MapNodeState[7], After = new MapNodeState[7]; // the show goes from Before to After
    public readonly List<int> Parties = new();       // worlds whose flag goes up on this map (in loop order)
    public readonly HashSet<int> OldGames = new();   // ...the ones of them that are "YOUR OLD GAMES COUNT!"
    public bool AllClearParty, RepeatClear, AllTheWayAround;
    public string Player = "";
    public int ClearedCount;
    public Look Look = Looks.Classic;
    public BossKind Teaser;          // the next boss (his badge pops up beside the world when the map is ready)
    public BossOutfit TeaserOutfit;
    public int Score, Bolts, Spares; // (between levels: the game's corner, so the kid sees it's the same game)
    public float Charge;
}

/// <summary>
/// THE WORLD MAP, drawn with code (Main.WorldMap.cs decides when it opens and what GO does; WorldMapArt.cs says where
/// everything is and how it looks). It has its own little show, step by step:
///   the game closes like an eye, then black, then the map opens like an eye around little Bolt-E;
///   a CLEARED world's gold flag climbs its pole ("SUNNY HILLS CLEAR!", with confetti), the road to the next world pops
///   up dot by dot, and the cloud over a "?" world puffs away ("NEW!");
///   little Bolt-E travels the road: riding along the island, by ROCKET up to THE MOON, in a glass bubble through space,
///   by SPACE CAPSULE (with a parachute and a SPLOOSH!) down to VOLCANO ISLAND, and surfing home;
///   READY: the GO bubble pops up over his head, and the next boss's badge beside him. A, B, X, Y, START, Enter or Space = GO!
/// A press during the show makes it go faster (it never skips it), and GO only counts a moment after the GO bubble pops
/// up. After 8 seconds GO always works (so nobody ever gets stuck). The map never freezes itself (Main puts it on a layer
/// that keeps going while the game waits behind it, frozen), and everything is timed here, frame by frame, with the
/// map's own clock (so it looks the same every time). The confetti, the smoke, the sparkles and the splash drops are 160
/// little "bits" made once and used again and again, and which way they fly comes from the map's own dice.
///
/// DrawnArea (for the self-test) covers everything that must stay on the screen: the panels, the words, the GO bubble,
/// the NEXT arrow, the worlds' dots, flags and gold sparkle rings, the worlds' pictures (with their smoke and snow) and
/// the clouds over them, the boss badge, the road dots, and the rides (the rocket, the space capsule and its splash, and
/// the space bubble). It leaves out what fills the screen anyway or may go off its edges: the sea, space and its stars,
/// the cloud band, the waves, the flying bits (confetti, smoke, sparkles and drops), the fireworks, the shooting star,
/// the sailboat, the fish, and the eye (the iris).
/// </summary>
public partial class WorldMap : Control
{
    // ---- The WORLD MAP's show (try changing these!) ----
    public const float IrisCloseSeconds = 0.35f;     // the game closes like an eye (between levels)...
    public const float StartIrisSeconds = 0.3f;      // ...a little quicker before a game
    public const float BlackSeconds = 0.1f;          // all black in the middle
    public const float IrisOpenSeconds = 0.45f;      // the map opens like an eye...
    public const float StartIrisOpenSeconds = 0.35f; // ...a little quicker before a game
    public const float FlagUpSeconds = 0.8f;         // the CLEARED flag climbs its pole
    public const float ClearWordsSeconds = 1.6f;     // "SUNNY HILLS CLEAR!" stays this long
    public const float DotSeconds = 0.06f;           // a new road dot this often...
    public const float RevealMostSeconds = 1.2f;     // ...but a whole road never takes longer than this
    public const float CloudPoofSeconds = 0.5f;      // the cloud over a "?" world puffs away
    public const float SparkleSeconds = 0.3f;        // a world CLEARED again: just a quick sparkle
    public const float AllClearSeconds = 2.5f;       // the big party when all 7 worlds are CLEARED
    public const float StepSeconds = 0.45f;          // one step on the start map (a glide or a STAR HOP)
    public const float StarHopHeight = 110f;         // how high a STAR HOP goes (it's kept between 80 and 160)
    public const float HurrySpeed = 3f;              // a press during the show makes the rest of it go this many times faster
    public const float GoGraceSeconds = 0.35f;       // GO only counts this long after the GO bubble pops up
    public const float NeverStuckSeconds = 8f;       // after this long on the map, GO always works
    public const float GoIrisSeconds = 0.4f;         // GO! The map closes like an eye...
    public const float GoBlackSeconds = 0.12f;
    public const float OverGameIrisSeconds = 0.45f;  // ...and the game opens like an eye
    public const float HeroSize = 0.42f;             // how big little Bolt-E is on the map
    public const float BubblePopSeconds = 0.25f, TeaserPopSeconds = 0.3f;
    public const float NewWordsSeconds = 1f;         // "NEW!" stays this long after a cloud puffs away
    public const float AroundWordsSeconds = 1.2f;    // "ALL THE WAY AROUND!" stays this long

    // ---- The rides (how far through the ride each part starts: 0 = the start, 1 = the end) ----
    public const float RocketBoardAt = 0.22f;        // the ROCKET: he rides to the rocket pad and hops in...
    public const float RocketLiftAt = 0.42f;         // ...it shakes on its pad, then BLAST OFF! (and it lands on THE MOON)
    public const float CapsuleSplashAt = 0.62f;      // the SPACE CAPSULE: down through the clouds under its parachute, SPLOOSH!...
    public const float CapsuleSurfAt = 0.7f;         // ...he pops out and surfs to the beach (the capsule sinks: bloop!)
    const float TrailEvery = 0.07f;                  // a puff of dust (or foam, or a sparkle) behind him this often
    const float Gravity = 300f;                      // how fast confetti and drops fall (pixels per second, faster every second)
    const int MostBits = 160;                        // how many flying bits the map has (used again and again)
    const float FireworkEvery = 0.35f;               // the ALL CLEAR fireworks
    const int Fireworks = 7;
    public const string StartPrompt = "D-PAD = pick a world    BACK = menu"; // (the name box's second line on the start map)

    /// <summary>Where the ALL CLEAR fireworks burst (in the sky, away from the corner panel and the big words).</summary>
    static readonly Vector2[] FireworkSpots = { new(470, 80), new(880, 70), new(660, 185), new(1200, 205), new(255, 205), new(985, 205), new(585, 135) };

    // ---- Main fills these in (see Main.WorldMap.cs) ----
    public SoundBoard Sounds = null!;               // (only its sounds that play while the game is paused are used)
    public CaveCurtain Iris = null!;                // the eye that closes and opens (the LAVA CAVES' one, reused)
    public Func<InputEvent, bool>? InputWhileOpen;  // every press (and click) while the map is up goes to Main
    public Action? AtBlack;                         // GO! The eye has closed: Main does the rest
    public Action<int>? Celebrated;                 // a world's flag really went up on a map
    public bool HoldShowForTest;                    // (only the self-test sets this) the show stops where it is

    // ---- What the map shows right now (the self-test reads these) ----
    public MapMode Mode { get; private set; }
    public MapStep Step { get; private set; } = MapStep.Hidden;
    public int Here { get; private set; }           // where little Bolt-E is (or is going, on the start map)
    public int GoingTo { get; private set; } = -1;  // between levels: the next world
    public int HeroOn { get; private set; } = -1;   // the world he stands on (-1 while he travels)
    public int NextArrowOn { get; private set; } = -1;
    public bool GoBubbleShowing { get; private set; }
    public bool Hurried { get; private set; }
    public float OpenSeconds { get; private set; }  // seconds since the map opened
    public Ride RideNow { get; private set; }
    public bool TeaserShowing { get; private set; }
    public BossKind TeaserBoss { get; private set; }
    public BossOutfit TeaserOutfit { get; private set; }
    public string BigWords { get; private set; } = "";
    public string SmallWords { get; private set; } = "";
    public string NameLine { get; private set; } = "";
    public string PromptLine { get; private set; } = "";
    public int Steps { get; private set; }
    public int StarHops { get; private set; }
    public int PartiesLeft { get; private set; }
    public bool AllClearShown { get; private set; }
    public bool GoldMap { get; private set; }
    public int FireworksPopped { get; private set; }
    public int TimesDrawn { get; private set; }
    public Rect2 DrawnArea => drawnArea;
    public readonly List<string> TextsDrawn = new(); // every string drawn last frame (the same list, cleared every time)
    public Robot? Hero => hero;
    public int HeroZ => heroSpot?.ZIndex ?? 0;       // 1 while he hops (over the GO bubble), else 0 (the eye is at 2: see Main.WorldMap.cs)
    public MapVisit Visit => visit;
    public bool HeroShowing => heroSpot?.Visible ?? false;   // false while he's inside the rocket or the space capsule
    public Ride? RideDrawn { get; private set; }             // the ride drawn last time (the rocket, the capsule, the surf...)
    public bool SpaceBubbleDrawn { get; private set; }       // the glass bubble was drawn over him last time
    public (BossKind Boss, BossOutfit Outfit)? TeaserDrawn { get; private set; } // the boss badge drawn last time
    public int BitsMade { get; private set; }                // flying bits made since the map opened

    /// <summary>How many little bits (confetti, smoke, sparkles, drops) are flying right now.</summary>
    public int BitsFlying
    {
        get
        {
            int count = 0;
            for (int i = 0; i < bits.Length; i++)
                if (bits[i].Life > 0) count++;
            return count;
        }
    }

    /// <summary>True when GO would go right now (Ready a moment, or the map has been up 8 seconds).</summary>
    public bool CanGoNow => Step switch
    {
        MapStep.Hidden or MapStep.IrisClose or MapStep.Black or MapStep.GoIris or MapStep.OverGame => false,
        MapStep.Ready or MapStep.Stepping => readySeconds >= GoGraceSeconds || OpenSeconds >= NeverStuckSeconds,
        _ => OpenSeconds >= NeverStuckSeconds,
    };

    /// <summary>Start map: little Bolt-E can walk to another world (he's ready, or already walking).</summary>
    public bool CanStep => Mode == MapMode.Start && Step is MapStep.Ready or MapStep.Stepping;

    /// <summary>Start map: BACK (or Esc) can go back to the menu (not once GO was pressed).</summary>
    public bool CanGoBack => Mode == MapMode.Start && Step is not (MapStep.Hidden or MapStep.GoIris or MapStep.OverGame);

    /// <summary>True while little Bolt-E is inside the rocket or the space capsule (then only the ride shows, not him).</summary>
    public bool HeroInVehicle => Step == MapStep.Travel && travelRoad >= 0 &&
                                 ((RideNow == Ride.Rocket && RideT >= RocketBoardAt) || (RideNow == Ride.Capsule && RideT < CapsuleSurfAt));

    /// <summary>The map's board is showing (between the eye opening and the eye closing after GO).</summary>
    bool BoardShowing => Step is >= MapStep.IrisOpen and <= MapStep.GoIris;

    /// <summary>Little Bolt-E floats through space in a glass bubble (between THE MOON and UPSIDE-DOWN STATION).</summary>
    bool InSpaceBubble => (Step == MapStep.Travel && RideNow == Ride.Space) || (Step == MapStep.Stepping && stepRoad == 4);

    public MapNodeState StateShown(int world) => shown[world];
    public bool FlagIsUp(int world) => shown[world] == MapNodeState.Cleared && flagRise[world] >= 1;
    public int DotsShown(int road) => dotsShown[road];
    public bool CloudOn(int world) => cloudOn[world];

    MapVisit visit = new();
    WorldMapCover cover = null!;
    BatteryRow runBatteries = null!;
    Node2D? heroSpot;
    Robot? hero;
    Vector2 irisFrom, overGameAt;
    float stepTime;          // seconds into the step (the show's steps count faster when hurried)
    float mapTime;           // seconds since the map opened (for everything that wiggles, blinks and bounces)
    float readySeconds = -1; // real seconds since the first READY (-1 = not ready yet): GO needs GoGraceSeconds
    float wordsLeft, newLeft, bubbleAge, teaserAge, nextPipIn, trailIn;
    bool smallHopNext, atBlackDone, readySoundsPlayed, splashed, boarded, launched, poppedOut;
    int partyIndex;          // which party is playing (the start map can have several)
    int partyWorld = -1, poofWorld = -1, newWordsOn = -1, travelRoad = -1;
    int stepFrom, stepRoad = -1;
    bool stepForward;
    readonly bool[] partyDone = new bool[7];
    readonly MapNodeState[] shown = new MapNodeState[7];
    readonly float[] flagRise = new float[7];
    readonly int[] dotsShown = new int[7];
    readonly bool[] cloudOn = new bool[7];
    Rect2 drawnArea;
    string ownerName = "", ownerCount = "", scoreText = "", boltsText = "";
    int nameSize = 34, promptSize = 22, bigSize = 56, ownerSize = 32;
    float ownerWidth;

    // The flying bits (made once: a new one takes the place of one that's done), and the map's own dice
    readonly MapBit[] bits = new MapBit[MostBits];
    int nextBit;
    Random dice = new(42);

    /// <summary>What a flying bit looks like.</summary>
    enum BitKind { Dot, Confetti, Sparkle, Puff }

    /// <summary>One little flying bit: confetti, a puff of smoke or dust, a sparkle, or a drop of water.</summary>
    struct MapBit
    {
        public BitKind Kind;
        public Vector2 Position, Velocity;
        public float Life, Lifetime, Size, Spin, Fall; // Fall: 1 = it falls (gravity), 0 = it floats
        public Color Color;
        public bool OnTop;                              // drawn over little Bolt-E and the panels (confetti), or under him
    }

    // The stars in space and the waves on the sea (made once, always the same)
    static readonly Vector2[] SkyStars = MakeStars();
    static readonly Vector2[] Waves =
    {
        new(40, 300), new(330, 300), new(520, 330), new(900, 300), new(1230, 300), new(40, 520), new(230, 470), new(1240, 420),
        new(1220, 520), new(1180, 600), new(1010, 665), new(1150, 690), new(60, 640), new(200, 690), new(215, 620), new(600, 355),
        new(800, 340), new(380, 350),
    };
    static readonly Vector2[] spaceCorners = { new(0, 0), new(1280, 0), new(1280, 240), new(0, 240) };
    static readonly Color[] spaceColors = { new(0.03f, 0.03f, 0.12f), new(0.03f, 0.03f, 0.12f), new(0.14f, 0.1f, 0.32f), new(0.14f, 0.1f, 0.32f) };

    static Vector2[] MakeStars()
    {
        var random = new Random(16);
        var stars = new Vector2[70];
        for (int i = 0; i < stars.Length; i++) stars[i] = new Vector2(random.NextSingle() * 1280, 6 + random.NextSingle() * 219);
        return stars;
    }

    public override void _Ready()
    {
        cover = new WorldMapCover { Map = this, Size = new Vector2(1280, 720), MouseFilter = MouseFilterEnum.Ignore };
        runBatteries = new BatteryRow { Position = new Vector2(24, 104), Size = new Vector2(200, 34), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        cover.AddChild(runBatteries); // (drawn on top of the corner panel)
        AddChild(cover);
    }

    // ---------- Opening and closing ----------

    /// <summary>The map opens: the eye closes on "from" (Bolt-E in the game, or on the menu), then the map's show starts.</summary>
    public void Open(MapVisit newVisit, Vector2 from)
    {
        FreeHero();
        visit = newVisit;
        Mode = visit.Mode;
        Here = visit.Here;
        GoingTo = visit.GoingTo;
        HeroOn = Here;
        for (int w = 0; w < 7; w++)
        {
            shown[w] = visit.Before[w];
            flagRise[w] = shown[w] == MapNodeState.Cleared ? 1 : 0;
            cloudOn[w] = shown[w] == MapNodeState.Locked;
            dotsShown[w] = shown[w] == MapNodeState.Cleared ? WorldMapArt.DotCount(w) : 0;
            partyDone[w] = false;
        }
        if (Mode == MapMode.Between) dotsShown[Here] = visit.Parties.Contains(Here) ? 0 : WorldMapArt.DotCount(Here); // (the trip's road)
        GoldMap = AllCleared(visit.Before);
        PartiesLeft = visit.Parties.Count;
        partyIndex = 0;
        partyWorld = poofWorld = newWordsOn = travelRoad = stepRoad = -1;
        Step = MapStep.IrisClose;
        stepTime = mapTime = OpenSeconds = 0;
        readySeconds = -1;
        Hurried = atBlackDone = readySoundsPlayed = smallHopNext = false;
        wordsLeft = newLeft = 0;
        BigWords = SmallWords = "";
        GoBubbleShowing = TeaserShowing = AllClearShown = false;
        FireworksPopped = Steps = StarHops = BitsMade = 0;
        RideNow = Ride.Road;
        NextArrowOn = -1;
        TeaserBoss = visit.Teaser;
        TeaserOutfit = visit.TeaserOutfit;
        dice = new Random(42); // (the map's own dice: the same every time it opens, so the show looks the same)
        ClearBits();

        // The words (made now, never while drawing)
        NameLine = Worlds.All[Here].Name;
        PromptLine = Mode == MapMode.Start ? StartPrompt : $"Next: {Worlds.All[GoingTo].Name}";
        ownerName = Hud.ShortName(visit.Player, 10);
        ownerCount = $"WORLDS CLEARED: {visit.ClearedCount} / {Worlds.All.Length}";
        scoreText = $"Score: {visit.Score}";
        boltsText = $"Bolts: {visit.Bolts}";
        FitWords();
        runBatteries.Spares = visit.Spares;
        runBatteries.Fill = visit.Charge;
        runBatteries.QueueRedraw();

        irisFrom = from;
        Iris.Center = from;
        Iris.Closed = 0;
        MakeHero(visit.Look);
        Visible = true;
        QueueRedraw();
        cover.QueueRedraw();
    }

    /// <summary>The board goes away and little Bolt-E is freed (the eye may still be opening over the game: see OpenOverGame).</summary>
    public void CloseBoard()
    {
        Visible = false;
        FreeHero();
        ClearBits();
        if (cover is not null) cover.QueueRedraw();
    }

    /// <summary>After GO: the eye opens around Bolt-E in the game (or the new game), and then the map is all done.</summary>
    public void OpenOverGame(Vector2 at)
    {
        Step = MapStep.OverGame;
        stepTime = 0;
        overGameAt = at;
        Iris.Center = at;
        Iris.Closed = 1;
        GoBubbleShowing = TeaserShowing = false;
        NextArrowOn = -1;
    }

    /// <summary>A fresh start: no map, the eye open, no little Bolt-E, no flying bits, and nothing held for the self-test.</summary>
    public void Reset()
    {
        Step = MapStep.Hidden;
        Visible = false;
        HoldShowForTest = false;
        FreeHero();
        ClearBits();
        GoBubbleShowing = TeaserShowing = false;
        NextArrowOn = -1;
        BigWords = SmallWords = "";
        Iris?.Reset();
    }

    // ---------- Presses ----------

    public override void _UnhandledInput(InputEvent e)
    {
        if (InputWhileOpen?.Invoke(e) == true) GetViewport().SetInputAsHandled();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (InputWhileOpen?.Invoke(e) == true) AcceptEvent(); // (every click on the map stays on the map)
    }

    /// <summary>
    /// A GO press (A, B, X, Y, START, Enter or Space). During the show it goes faster (it never skips it). When READY (a
    /// moment after the GO bubble pops up), or after 8 seconds on the map: GO! (While the eye closes or opens: nothing.)
    /// </summary>
    public void GoPressed()
    {
        if (Step is MapStep.Hidden or MapStep.IrisClose or MapStep.Black or MapStep.GoIris or MapStep.OverGame) return;
        if (Step == MapStep.Stepping) FinishStep(); // (a step in progress finishes first)
        if ((Step == MapStep.Ready && readySeconds >= GoGraceSeconds) || OpenSeconds >= NeverStuckSeconds)
        {
            SnapShow();
            StartGoIris();
            return;
        }
        if (Step != MapStep.Ready) Hurried = true; // (the rest of the show goes 3 times faster)
    }

    /// <summary>A click: on the GO bubble (once GO counts) it's GO. Every other click does nothing.</summary>
    public bool ClickedGo(Vector2 at)
    {
        if (!GoBubbleShowing || HeroOn < 0 || !CanGoNow || !WorldMapArt.BubbleRect(HeroOn).HasPoint(at)) return false;
        GoPressed();
        return true;
    }

    /// <summary>Between levels, when READY: the D-pad makes a little "this way!" hop.</summary>
    public void Hop()
    {
        if (Mode != MapMode.Between || Step != MapStep.Ready || hero is null || !hero.OnGround) return;
        SmallHop();
        Sounds.Play(Sfx.MapHop);
    }

    /// <summary>
    /// The start map: little Bolt-E walks to "world". Along a road he has opened, he glides along it; otherwise he does a
    /// STAR HOP. A step that's still going finishes right away first. ("teaser" = the next game's first boss.)
    /// </summary>
    public void StepTo(int world, BossKind teaser, BossOutfit outfit)
    {
        if (!CanStep) return;
        if (Step == MapStep.Stepping) FinishStep();
        if (world == Here) return;
        stepFrom = Here;
        Here = world;
        HeroOn = -1;
        stepRoad = RoadBetween(stepFrom, world, out stepForward);
        RideNow = stepRoad >= 0 ? Ride.Road : Ride.StarHop;
        if (stepRoad < 0)
        {
            StarHops++;
            Sounds.Play(Sfx.Warp, 1.3f);
        }
        Steps++;
        TeaserBoss = teaser;
        TeaserOutfit = outfit;
        TeaserShowing = GoBubbleShowing = false;
        NextArrowOn = -1;
        NameLine = Worlds.All[world].Name;
        FitWords();
        Step = MapStep.Stepping;
        stepTime = 0;
        trailIn = 0;
    }

    /// <summary>The road from world a to world b (they're next to each other and he opened it), or -1. forward = from a to b.</summary>
    int RoadBetween(int a, int b, out bool forward)
    {
        forward = true;
        int count = Worlds.All.Length;
        if (b == (a + 1) % count && dotsShown[a] >= WorldMapArt.DotCount(a)) return a;
        forward = false;
        if (a == (b + 1) % count && dotsShown[b] >= WorldMapArt.DotCount(b)) return b;
        return -1;
    }

    // ---------- For the self-test ----------

    /// <summary>(Only the self-test does this) straight to READY, with every flag, road and cloud done, and the GO wait over.</summary>
    public void SkipShow()
    {
        if (Step is MapStep.Hidden or MapStep.GoIris or MapStep.OverGame) return;
        Iris.Closed = 0;
        SnapShow();
        EnterReady(quiet: true);
        readySeconds = Mathf.Max(readySeconds, GoGraceSeconds);
    }

    /// <summary>
    /// (Only the self-test and the playtest camera do this) shows one moment of the map, "progress" (0 to 1) into a step,
    /// as if little Bolt-E were on world "here" going to "goingTo" (READY: standing on "here", with the NEXT arrow over
    /// "goingTo"). The show stays there (HoldShowForTest) until the map closes, or until HoldShowForTest is switched off.
    /// </summary>
    public void Preview(MapStep step, float progress, int here, int goingTo)
    {
        if (Step is MapStep.Hidden or MapStep.GoIris or MapStep.OverGame) return;
        HoldShowForTest = true;
        Iris.Closed = 0;
        ClearBits();
        progress = Mathf.Clamp(progress, 0, 1);
        Here = here;
        GoingTo = goingTo;
        HeroOn = here;
        partyWorld = here;
        poofWorld = goingTo;
        travelRoad = here;
        stepRoad = -1;
        RideNow = Ride.Road;
        GoBubbleShowing = TeaserShowing = false;
        NextArrowOn = -1;
        BigWords = SmallWords = "";
        wordsLeft = newLeft = 0;
        GoldMap = AllCleared(visit.Before);
        AllClearShown = false;
        FireworksPopped = 0;
        switch (step)
        {
            case MapStep.FlagUp:
                shown[here] = MapNodeState.Cleared;
                cloudOn[here] = false;
                dotsShown[here] = 0;
                SayBigWords(WorldMapArt.ClearWords[here]);
                break;
            case MapStep.PathReveal:
                ShowCleared(here);
                dotsShown[here] = 0;
                SayBigWords(WorldMapArt.ClearWords[here]);
                NextArrowOn = goingTo;
                break;
            case MapStep.CloudPoof:
                ShowCleared(here);
                cloudOn[goingTo] = true;
                NextArrowOn = goingTo;
                break;
            case MapStep.AllClear:
                SayBigWords("ALL 7 WORLDS CLEAR!");
                SmallWords = "The whole map is GOLD now!";
                GoldMap = AllClearShown = true;
                break;
            case MapStep.Sparkle:
                ShowCleared(here);
                break;
            case MapStep.Travel:
                ShowCleared(here);
                Uncover(goingTo);
                RideNow = WorldMapArt.Rides[here];
                HeroOn = -1;
                NextArrowOn = goingTo;
                break;
            case MapStep.Ready:
                Uncover(here);
                GoBubbleShowing = TeaserShowing = true;
                bubbleAge = teaserAge = 1;
                NextArrowOn = goingTo != here && !GoldMap ? goingTo : -1;
                break;
            case MapStep.Stepping:
                stepFrom = here;
                Here = goingTo;
                HeroOn = -1;
                stepRoad = RoadBetween(here, goingTo, out stepForward);
                RideNow = stepRoad >= 0 ? Ride.Road : Ride.StarHop;
                break;
        }
        Step = step;
        stepTime = progress * StepLength(step);
        boarded = RideT >= RocketBoardAt; // (so letting the show go on from here does the rest of the ride right)
        launched = RideT >= RocketLiftAt;
        splashed = RideT >= CapsuleSplashAt;
        poppedOut = RideT >= CapsuleSurfAt;
        trailIn = 0;
        // The name box: the world he's on (like the real show: the second line says where he's going), or where he steps to
        NameLine = Worlds.All[Mathf.Clamp(step == MapStep.Stepping ? goingTo : here, 0, Worlds.All.Length - 1)].Name;
        if (Mode == MapMode.Between) PromptLine = $"Next: {Worlds.All[Mathf.Clamp(step == MapStep.Ready ? here : goingTo, 0, Worlds.All.Length - 1)].Name}";
        FitWords();
    }

    /// <summary>(Only the self-test and the playtest camera do this) the boss badge shows this boss, dressed like this.</summary>
    public void PreviewTeaser(BossKind boss, BossOutfit outfit)
    {
        TeaserBoss = boss;
        TeaserOutfit = outfit;
    }

    /// <summary>A world shown CLEARED, its flag up and its road there (for Preview).</summary>
    void ShowCleared(int world)
    {
        shown[world] = MapNodeState.Cleared;
        flagRise[world] = 1;
        cloudOn[world] = false;
        dotsShown[world] = WorldMapArt.DotCount(world);
    }

    /// <summary>A world shown with no cloud over it (for Preview).</summary>
    void Uncover(int world)
    {
        cloudOn[world] = false;
        if (shown[world] == MapNodeState.Locked) shown[world] = MapNodeState.Open;
    }

    void SayBigWords(string words)
    {
        BigWords = words;
        SmallWords = "";
        wordsLeft = ClearWordsSeconds;
    }

    // ---------- Every frame ----------

    public override void _Process(double delta)
    {
        if (Step == MapStep.Hidden) return;
        float dt = (float)delta;
        mapTime += dt;
        OpenSeconds += dt;
        bool hold = HoldShowForTest && Step is not (MapStep.GoIris or MapStep.OverGame);
        float showDt = hold ? 0 : dt * (Hurried ? HurrySpeed : 1); // the show's steps (faster when hurried)
        float irisDt = hold ? 0 : dt;                               // the eye always opens and closes at normal speed
        if (readySeconds >= 0) readySeconds += dt;                  // the GO wait always counts real time
        wordsLeft = Mathf.Max(0, wordsLeft - showDt);
        if (wordsLeft <= 0 && Step is not MapStep.AllClear) BigWords = SmallWords = "";
        newLeft = Mathf.Max(0, newLeft - showDt);
        bubbleAge += dt;
        teaserAge += dt;

        switch (Step)
        {
            case MapStep.IrisClose:
                stepTime += irisDt;
                Iris.Center = irisFrom;
                Iris.Closed = Mathf.Clamp(stepTime / CloseLength, 0, 1);
                if (stepTime >= CloseLength) NextStep(MapStep.Black);
                break;
            case MapStep.Black:
                stepTime += irisDt;
                Iris.Closed = 1;
                if (stepTime >= BlackSeconds)
                {
                    NextStep(MapStep.IrisOpen);
                    Sounds.Play(Sfx.MapIris, 1.15f);
                }
                break;
            case MapStep.IrisOpen:
                stepTime += irisDt;
                Iris.Center = HeroMiddle;
                Iris.Closed = 1 - Mathf.Clamp(stepTime / OpenLength, 0, 1);
                if (stepTime >= OpenLength)
                {
                    Iris.Closed = 0;
                    StartShow();
                }
                break;
            case MapStep.FlagUp:
                stepTime += showDt;
                flagRise[partyWorld] = Smooth(stepTime / FlagUpSeconds);
                if (stepTime >= FlagUpSeconds)
                {
                    flagRise[partyWorld] = 1;
                    CelebrateParty(partyIndex);
                    StartPathReveal();
                }
                break;
            case MapStep.PathReveal:
                UpdatePathReveal(showDt);
                break;
            case MapStep.CloudPoof:
                stepTime += showDt;
                if (stepTime >= CloudPoofSeconds) EndCloudPoof();
                break;
            case MapStep.AllClear:
                stepTime += showDt;
                int popped = Mathf.Min(Fireworks, (int)(stepTime / FireworkEvery) + 1);
                for (; FireworksPopped < popped; FireworksPopped++)
                {
                    Sounds.Play(Sfx.MapPoof, 1.6f);
                    if (!hold) Confetti(FireworkSpots[FireworksPopped % FireworkSpots.Length], 8, 170);
                }
                if (!hold) ConfettiRain(showDt);
                if (stepTime >= AllClearSeconds)
                {
                    BigWords = SmallWords = "";
                    AfterAllClear();
                }
                break;
            case MapStep.Sparkle:
                stepTime += showDt;
                if (stepTime >= SparkleSeconds) StartTravel();
                break;
            case MapStep.Travel:
                UpdateTravel(showDt);
                break;
            case MapStep.Stepping:
                stepTime += hold ? 0 : dt;
                if (!hold) StepTrail(dt);
                if (stepTime >= StepSeconds)
                {
                    FinishStep();
                    LandingPuff(HeroOn);
                }
                break;
            case MapStep.GoIris:
                stepTime += dt;
                Iris.Center = HeroMiddle;
                Iris.Closed = Mathf.Clamp(stepTime / GoIrisSeconds, 0, 1);
                if (stepTime >= GoIrisSeconds + GoBlackSeconds)
                {
                    if (atBlackDone)
                    {
                        Reset(); // (Main didn't take over: never stuck)
                        return;
                    }
                    atBlackDone = true;
                    if (AtBlack is null) Reset();
                    else AtBlack();
                    return; // (Main has closed the map, and opens the eye over the game)
                }
                break;
            case MapStep.OverGame:
                stepTime += dt;
                Iris.Center = overGameAt;
                Iris.Closed = 1 - Mathf.Clamp(stepTime / OverGameIrisSeconds, 0, 1);
                if (stepTime >= OverGameIrisSeconds)
                {
                    Iris.Closed = 0;
                    Step = MapStep.Hidden;
                }
                return;
        }

        UpdateHero();
        UpdateBits(hold ? 0 : dt);
        runBatteries.Visible = BoardShowing && Mode == MapMode.Between; // (this game's batteries, in the corner panel)
        QueueRedraw();
        cover.QueueRedraw();
    }

    float CloseLength => Mode == MapMode.Start ? StartIrisSeconds : IrisCloseSeconds;
    float OpenLength => Mode == MapMode.Start ? StartIrisOpenSeconds : IrisOpenSeconds;

    float StepLength(MapStep step) => step switch
    {
        MapStep.IrisClose => CloseLength,
        MapStep.Black => BlackSeconds,
        MapStep.IrisOpen => OpenLength,
        MapStep.FlagUp => FlagUpSeconds,
        MapStep.PathReveal => RevealLength(partyWorld),
        MapStep.CloudPoof => CloudPoofSeconds,
        MapStep.AllClear => AllClearSeconds,
        MapStep.Sparkle => SparkleSeconds,
        MapStep.Travel => WorldMapArt.RideSeconds[Mathf.Clamp(travelRoad, 0, 6)],
        MapStep.Stepping => StepSeconds,
        _ => 1,
    };

    static float RevealLength(int road) => Mathf.Min(WorldMapArt.DotCount(Mathf.Clamp(road, 0, 6)) * DotSeconds, RevealMostSeconds);

    void NextStep(MapStep step)
    {
        Step = step;
        stepTime = 0;
    }

    // ---------- The show, step by step ----------

    /// <summary>The eye has opened: the parties (flags going up), or a quick sparkle for a world CLEARED again.</summary>
    void StartShow()
    {
        if (partyIndex < visit.Parties.Count) StartFlagUp();
        else if (Mode == MapMode.Between) NextStepSparkle();
        else EnterReady(quiet: false);
    }

    void NextStepSparkle()
    {
        NextStep(MapStep.Sparkle);
        Sounds.Play(Sfx.MapFlag, 1.12f); // (a lighter "again!")
    }

    /// <summary>FLAG UP: the dot turns blue with a star, the gold flag climbs, "SUNNY HILLS CLEAR!", confetti, and Bolt-E hops with star eyes.</summary>
    void StartFlagUp()
    {
        partyWorld = visit.Parties[partyIndex];
        shown[partyWorld] = MapNodeState.Cleared;
        flagRise[partyWorld] = 0;
        bool firstOldGames = visit.OldGames.Contains(partyWorld) && FirstOldGamesParty(partyIndex);
        BigWords = firstOldGames ? "YOUR OLD GAMES COUNT!" : WorldMapArt.ClearWords[partyWorld];
        SmallWords = "";
        wordsLeft = ClearWordsSeconds;
        FitWords();
        Sounds.Play(Sfx.MapFlag);
        if (!HoldShowForTest) Confetti(WorldMapArt.FlagTop(partyWorld), 34, 260);
        if (hero is not null && hero.OnGround)
        {
            hero.Jump();
            hero.ShowFace(RobotFace.Stars, 1.2f);
        }
        NextStep(MapStep.FlagUp);
    }

    /// <summary>True if no party before this one was one of the old games (so this one says "YOUR OLD GAMES COUNT!").</summary>
    bool FirstOldGamesParty(int index)
    {
        for (int i = 0; i < index; i++)
            if (visit.OldGames.Contains(visit.Parties[i])) return false;
        return true;
    }

    /// <summary>PATH: the road to the next world pops up, one dot at a time, each "pip" a little higher.</summary>
    void StartPathReveal()
    {
        NextStep(MapStep.PathReveal);
        NextArrowOn = Mode == MapMode.Between ? GoingTo : -1;
    }

    void UpdatePathReveal(float showDt)
    {
        stepTime += showDt;
        int road = partyWorld, count = WorldMapArt.DotCount(road);
        float length = RevealLength(road);
        int dots = Mathf.Min(count, (int)(stepTime / (length / count)) + 1);
        for (; dotsShown[road] < dots; dotsShown[road]++)
            if (!HoldShowForTest) Sounds.Play(Sfx.MapPip, 1 + 0.6f * dotsShown[road] / count);
        if (stepTime < length) return;
        dotsShown[road] = count;
        int next = (road + 1) % Worlds.All.Length;
        if (visit.Before[next] == MapNodeState.Locked && visit.After[next] != MapNodeState.Locked)
        {
            poofWorld = next;
            NextStep(MapStep.CloudPoof);
            Sounds.Play(Sfx.MapPoof);
            if (!HoldShowForTest) CloudPuffs(next);
        }
        else
        {
            AfterParty();
        }
    }

    /// <summary>The cloud has puffed away: the world's picture shows, it's OPEN now, and "NEW!".</summary>
    void EndCloudPoof()
    {
        cloudOn[poofWorld] = false;
        shown[poofWorld] = visit.After[poofWorld];
        newWordsOn = poofWorld;
        newLeft = NewWordsSeconds;
        AfterParty();
    }

    /// <summary>One party is done: the next one, or ALL CLEAR, or on (travel between levels, READY on the start map).</summary>
    void AfterParty()
    {
        partyIndex++;
        if (partyIndex < visit.Parties.Count)
        {
            StartFlagUp();
            return;
        }
        if (visit.AllClearParty && !AllClearShown)
        {
            NextStep(MapStep.AllClear);
            AllClearShown = GoldMap = true;
            BigWords = "ALL 7 WORLDS CLEAR!";
            SmallWords = "The whole map is GOLD now!";
            wordsLeft = AllClearSeconds;
            FitWords();
            NextArrowOn = -1;
            Sounds.Play(Sfx.MapAllClear);
            if (hero is not null && hero.OnGround)
            {
                hero.Jump();
                hero.Flip();
            }
            return;
        }
        AfterAllClear();
    }

    void AfterAllClear()
    {
        if (Mode == MapMode.Between) StartTravel();
        else EnterReady(quiet: false);
    }

    /// <summary>TRAVEL: little Bolt-E rides the road to the next world (rocket, space bubble, capsule or surfing).</summary>
    void StartTravel()
    {
        travelRoad = Here;
        dotsShown[travelRoad] = WorldMapArt.DotCount(travelRoad); // (the trip's road is always there)
        RideNow = WorldMapArt.Rides[travelRoad];
        HeroOn = -1;
        NextArrowOn = GoingTo;
        nextPipIn = trailIn = 0;
        splashed = boarded = launched = poppedOut = false;
        NextStep(MapStep.Travel);
    }

    /// <summary>How far through this trip's ride he is (0 = the start, 1 = there).</summary>
    float RideT => travelRoad >= 0 && Step == MapStep.Travel ? Mathf.Clamp(stepTime / WorldMapArt.RideSeconds[travelRoad], 0, 1) : 0;

    /// <summary>How many pixels along the road he (or his rocket, or his capsule) is now. Each ride has its own parts.</summary>
    float TravelAlong()
    {
        float t = RideT, length = WorldMapArt.RoadLengths[travelRoad];
        switch (RideNow)
        {
            case Ride.Rocket:
            {
                float pad = WorldMapArt.PadDistance;
                if (t < RocketBoardAt) return Smooth(t / RocketBoardAt) * pad;                               // he rides to the pad...
                if (t < RocketLiftAt) return pad;                                                            // ...hops in, it shakes...
                return pad + Smooth((t - RocketLiftAt) / (1 - RocketLiftAt)) * (length - pad);               // ...and BLAST OFF!
            }
            case Ride.Capsule:
            {
                float splash = WorldMapArt.SplashDistance;
                if (t < CapsuleSplashAt) return Smooth(t / CapsuleSplashAt) * splash;                        // down by capsule...
                if (t < CapsuleSurfAt) return splash;                                                        // ...SPLOOSH!...
                return splash + Smooth((t - CapsuleSurfAt) / (1 - CapsuleSurfAt)) * (length - splash);       // ...and surfing
            }
            default:
                return Smooth(t) * length;
        }
    }

    void UpdateTravel(float showDt)
    {
        stepTime += showDt;
        float seconds = WorldMapArt.RideSeconds[travelRoad], t = RideT, along = TravelAlong();
        var at = WorldMapArt.PointOnRoad(travelRoad, along);
        var way = WorldMapArt.WayOfRoad(travelRoad, along);
        bool live = showDt > 0; // (a moment held for the self-test makes no new bits and no sounds)
        if (RideNow == Ride.Rocket)
        {
            if (!boarded && t >= RocketBoardAt)
            {
                boarded = true; // he hops into the rocket...
                if (live) SparkleBurst(WorldMapArt.RocketPad + new Vector2(0, -30), 8);
            }
            if (!launched && t >= RocketLiftAt)
            {
                launched = true; // ...and BLAST OFF!
                if (live)
                {
                    Sounds.Play(Sfx.MapRocket);
                    for (int i = 0; i < 12; i++)
                        Smoke(WorldMapArt.RocketPad + new Vector2((Dice() - 0.5f) * 40, -2), new Vector2((Dice() - 0.5f) * 150, -8 - Dice() * 30), 0.9f, 7);
                }
            }
        }
        else if (RideNow == Ride.Capsule)
        {
            if (!splashed && t >= CapsuleSplashAt)
            {
                splashed = true; // SPLOOSH!
                if (live)
                {
                    Sounds.Play(Sfx.MapSplash);
                    for (int i = 0; i < 22; i++)
                        AddBit(BitKind.Dot, WorldMapArt.SplashSpot + new Vector2((Dice() - 0.5f) * 20, 2),
                               new Vector2((Dice() - 0.5f) * 200, -120 - Dice() * 130), 0.9f, 2.8f + Dice() * 1.5f,
                               i % 3 == 0 ? Colors.White : new Color(0.75f, 0.92f, 1f), 1, false);
                }
            }
            if (!poppedOut && t >= CapsuleSurfAt)
            {
                poppedOut = true; // he pops out of the capsule, onto his board
                if (live)
                {
                    SmallHop();
                    SparkleBurst(at + new Vector2(0, -24), 6);
                }
            }
        }

        // Behind him: dust on land, foam on the sea, sparkles in space (or the rocket's smoke)
        trailIn -= showDt;
        if (live && trailIn <= 0)
        {
            trailIn = TrailEvery;
            if (RideNow == Ride.Rocket && launched) Smoke(at - way * 4, -way * 70 + Nudge(25), 0.75f, 5);
            else if (RideNow == Ride.Rocket && boarded) Smoke(WorldMapArt.RocketPad + new Vector2((Dice() - 0.5f) * 30, -3), new Vector2((Dice() - 0.5f) * 70, -15), 0.6f, 5);
            else if (!HeroInVehicle) TrailBehind(at, way, RideNow == Ride.Space ? 2 : RideNow is Ride.Sea or Ride.Capsule ? 1 : 0);
        }

        if (RideNow is Ride.Road or Ride.Sea or Ride.Space)
        {
            nextPipIn -= showDt;
            if (nextPipIn <= 0 && !HoldShowForTest)
            {
                nextPipIn = 0.16f;
                Sounds.Play(Sfx.MapPip, RideNow == Ride.Space ? 1.3f : RideNow == Ride.Sea ? 0.7f : 0.8f);
            }
        }
        if (stepTime >= seconds)
        {
            if (RideNow == Ride.Rocket && live) SparkleBurst(at + new Vector2(0, -30), 14); // the rocket pops into sparkles on THE MOON
            EnterReady(quiet: false);
        }
    }

    /// <summary>
    /// READY: little Bolt-E stands on the world, the GO bubble pops up over his head, the next boss's badge pops up beside
    /// the world, and the name box says where he is. It waits for GO (for ever, like the pause screen).
    /// </summary>
    void EnterReady(bool quiet)
    {
        NextStep(MapStep.Ready);
        HeroOn = Mode == MapMode.Between ? GoingTo : Here;
        if (readySeconds < 0) readySeconds = 0;
        GoBubbleShowing = TeaserShowing = true;
        bubbleAge = teaserAge = 0;
        NameLine = Worlds.All[HeroOn].Name;
        bool around = Mode == MapMode.Between && travelRoad == Worlds.All.Length - 1 && visit.AllTheWayAround;
        BigWords = around ? "ALL THE WAY AROUND!" : ""; // (the CLEAR words end here)
        SmallWords = "";
        wordsLeft = around ? AroundWordsSeconds : 0;
        FitWords();
        NextArrowOn = Mode == MapMode.Start && visit.Next >= 0 && visit.Next != HeroOn && !GoldMap ? visit.Next : -1;
        if (quiet || readySoundsPlayed) return;
        readySoundsPlayed = true;
        Sounds.Play(Sfx.MapReady);
        Sounds.Play(Sfx.MapBossSting);
        SmallHop();
        if (!HoldShowForTest) LandingPuff(HeroOn);
        if (around && !HoldShowForTest) Confetti(WorldMapArt.Spots[HeroOn] + new Vector2(0, -60), 20, 210);
    }

    /// <summary>A step on the start map is done (or a new press finished it early): he's on the new world.</summary>
    void FinishStep()
    {
        EnterReady(quiet: true);
        PlaceHeroOn(Here);
    }

    /// <summary>GO! The map closes like an eye around little Bolt-E (he hops), and then Main does the rest (see AtBlack).</summary>
    void StartGoIris()
    {
        NextStep(MapStep.GoIris);
        GoBubbleShowing = TeaserShowing = false;
        NextArrowOn = -1;
        BigWords = SmallWords = "";
        Sounds.Play(Sfx.MapGo);
        Sounds.Play(Sfx.MapIris, 0.8f);
        if (hero is not null && hero.OnGround) hero.Jump();
    }

    /// <summary>
    /// Everything the show was still going to do happens at once: every flag up, every road there, every cloud gone, the
    /// GOLD MAP if it's time, and little Bolt-E on his world. (GO and BACK do this: every party counts as celebrated.)
    /// </summary>
    public void SnapShow()
    {
        for (int i = 0; i < visit.Parties.Count; i++) CelebrateParty(i);
        partyIndex = visit.Parties.Count;
        for (int w = 0; w < 7; w++)
        {
            shown[w] = visit.After[w];
            flagRise[w] = shown[w] == MapNodeState.Cleared ? 1 : 0;
            cloudOn[w] = shown[w] == MapNodeState.Locked;
            if (shown[w] == MapNodeState.Cleared) dotsShown[w] = WorldMapArt.DotCount(w);
        }
        if (visit.AllClearParty) GoldMap = true;
        BigWords = SmallWords = "";
        wordsLeft = newLeft = 0;
        if (Mode == MapMode.Between)
        {
            dotsShown[visit.Here] = WorldMapArt.DotCount(visit.Here);
            travelRoad = visit.Here;
            HeroOn = GoingTo;
        }
        else if (HeroOn < 0)
        {
            HeroOn = Here;
        }
        if (Step == MapStep.Stepping) FinishStep();
        PlaceHeroOn(HeroOn);
    }

    void CelebrateParty(int index)
    {
        if (index < 0 || index >= visit.Parties.Count || partyDone[index]) return;
        partyDone[index] = true;
        PartiesLeft = Mathf.Max(0, PartiesLeft - 1);
        Celebrated?.Invoke(visit.Parties[index]);
    }

    static bool AllCleared(MapNodeState[] states)
    {
        foreach (var state in states)
            if (state != MapNodeState.Cleared) return false;
        return true;
    }

    /// <summary>Makes movement start and stop gently instead of jerking (0 to 1).</summary>
    static float Smooth(float t)
    {
        t = Mathf.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>The words' sizes: shrunk so they fit (worked out when the words change, never while drawing).</summary>
    void FitWords()
    {
        nameSize = WorldMapArt.FitSize(NameLine, 34, 26, WorldMapArt.NameBoxTextWidth);
        promptSize = WorldMapArt.FitSize(PromptLine, 22, 18, WorldMapArt.NameBoxTextWidth);
        bigSize = BigWords.Length > 0 ? WorldMapArt.FitSize(BigWords, 56, 30, WorldMapArt.BigWordsWidth) : 56;
        ownerSize = WorldMapArt.FitSize(ownerName, 32, 20, GoldMap ? 250 : 306); // (on the GOLD MAP a crown comes after the name)
        ownerWidth = ThemeDB.FallbackFont.GetStringSize(ownerName, HorizontalAlignment.Left, -1, ownerSize).X;
    }

    // ---------- The flying bits: confetti, smoke, dust, sparkles and drops ----------

    /// <summary>A number from 0 to 1 from the map's own dice.</summary>
    float Dice() => dice.NextSingle();

    /// <summary>A little random push, up to "size" pixels each way.</summary>
    Vector2 Nudge(float size) => new((Dice() - 0.5f) * 2 * size, (Dice() - 0.5f) * 2 * size);

    /// <summary>A new flying bit. (If all 160 are flying, it takes the place of the next one in turn.)</summary>
    void AddBit(BitKind kind, Vector2 at, Vector2 velocity, float life, float size, Color color, float fall, bool onTop)
    {
        int slot = nextBit;
        for (int i = 0; i < bits.Length; i++)
        {
            int n = (nextBit + i) % bits.Length;
            if (bits[n].Life > 0) continue;
            slot = n;
            break;
        }
        nextBit = (slot + 1) % bits.Length;
        bits[slot] = new MapBit
        {
            Kind = kind, Position = at, Velocity = velocity, Life = life, Lifetime = life, Size = size, Spin = Dice() * Mathf.Tau,
            Fall = fall, Color = color, OnTop = onTop,
        };
        BitsMade++;
    }

    void ClearBits()
    {
        for (int i = 0; i < bits.Length; i++) bits[i].Life = 0;
    }

    /// <summary>Every frame: the bits fly, fall (gravity), spin, and fade away.</summary>
    void UpdateBits(float dt)
    {
        if (dt <= 0) return;
        for (int i = 0; i < bits.Length; i++)
        {
            ref var bit = ref bits[i];
            if (bit.Life <= 0) continue;
            bit.Life -= dt;
            bit.Velocity.Y += Gravity * bit.Fall * dt;
            if (bit.Kind == BitKind.Puff) bit.Velocity *= 1 - Mathf.Min(1, 2.5f * dt); // (puffs slow down)
            if (bit.Kind == BitKind.Confetti) bit.Velocity.X *= 1 - Mathf.Min(1, 1.2f * dt);
            bit.Position += bit.Velocity * dt;
            bit.Spin += dt * 9;
        }
    }

    /// <summary>A burst of confetti from "at": up and out, then it flutters down.</summary>
    void Confetti(Vector2 at, int count, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = -Mathf.Pi / 2 + (Dice() - 0.5f) * 2.4f;
            var velocity = Vector2.Right.Rotated(angle) * speed * (0.45f + Dice() * 0.75f);
            AddBit(BitKind.Confetti, at, velocity, 1.2f + Dice() * 0.7f, 5 + Dice() * 2.5f,
                   WorldMapArt.PartyColors[(i + (int)(Dice() * 7)) % WorldMapArt.PartyColors.Length], 1, onTop: true);
        }
    }

    float rainIn;

    /// <summary>The ALL CLEAR party: confetti falls from the top of the screen.</summary>
    void ConfettiRain(float dt)
    {
        rainIn -= dt;
        if (rainIn > 0) return;
        rainIn = 0.05f;
        AddBit(BitKind.Confetti, new Vector2(Dice() * 1280, -6), new Vector2((Dice() - 0.5f) * 60, 60 + Dice() * 70), 3.2f, 5 + Dice() * 2,
               WorldMapArt.PartyColors[(int)(Dice() * 7) % WorldMapArt.PartyColors.Length], 0.25f, onTop: true);
    }

    /// <summary>Sparkles bursting out of a spot.</summary>
    void SparkleBurst(Vector2 at, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var way = Vector2.Right.Rotated(i * Mathf.Tau / count + Dice() * 0.5f);
            AddBit(BitKind.Sparkle, at, way * (60 + Dice() * 70), 0.55f + Dice() * 0.25f, 5 + Dice() * 2, WorldMapArt.GoldLight, 0.3f, onTop: false);
        }
    }

    /// <summary>A grey puff of rocket smoke.</summary>
    void Smoke(Vector2 at, Vector2 velocity, float life, float size) =>
        AddBit(BitKind.Puff, at, velocity, life, size, new Color(0.88f, 0.88f, 0.92f, 0.8f), 0, onTop: false);

    /// <summary>The cloud over a world puffs apart: little white puffs fly out of it.</summary>
    void CloudPuffs(int world)
    {
        var box = WorldMapArt.LandmarkBoxes[world];
        for (int i = 0; i < 12; i++)
        {
            var way = Vector2.Right.Rotated(i * Mathf.Tau / 12 + Dice() * 0.4f);
            AddBit(BitKind.Puff, box.GetCenter() + way * box.Size.Y * 0.3f, way * (80 + Dice() * 60), 0.55f, 8 + Dice() * 4,
                   world == 4 ? WorldMapArt.NebulaPurple : WorldMapArt.CloudWhite, 0, onTop: false);
        }
    }

    /// <summary>Behind little Bolt-E as he travels: dust on land (kind 0), foam on the sea (1), sparkles in space (2).</summary>
    void TrailBehind(Vector2 feet, Vector2 way, int kind)
    {
        var back = feet - way * 14;
        if (kind == 2) AddBit(BitKind.Sparkle, back + Nudge(6) + new Vector2(0, -20), -way * 20 + Nudge(12), 0.6f, 4.5f, WorldMapArt.StarDot, 0, onTop: false);
        else if (kind == 1) AddBit(BitKind.Dot, back + Nudge(4), -way * 40 + new Vector2(0, -70 - Dice() * 40) + Nudge(25), 0.5f, 2.3f, Colors.White, 1, onTop: false);
        else AddBit(BitKind.Puff, back + new Vector2(0, -3) + Nudge(3), -way * 25 + new Vector2(0, -14), 0.5f, 5.5f, WorldMapArt.DustColor, 0, onTop: false);
    }

    /// <summary>The start map: a trail behind him while he steps (sparkles for a STAR HOP, else what's under him).</summary>
    void StepTrail(float dt)
    {
        trailIn -= dt;
        if (trailIn > 0) return;
        if (stepRoad < 0)
        {
            trailIn = 0.025f;
            AddBit(BitKind.Sparkle, HeroMiddle + Nudge(12), Nudge(30), 0.65f, 6.5f, WorldMapArt.GoldLight, 0.2f, onTop: false);
            return;
        }
        trailIn = TrailEvery;
        float t = Smooth(stepTime / StepSeconds), length = WorldMapArt.RoadLengths[stepRoad];
        float along = stepForward ? t * length : (1 - t) * length;
        var way = WorldMapArt.WayOfRoad(stepRoad, along) * (stepForward ? 1 : -1);
        TrailBehind(heroFeet, way, WorldMapArt.KindAt(stepRoad, along));
    }

    /// <summary>He lands on a world: a little puff of dust (sparkles up in space).</summary>
    void LandingPuff(int world)
    {
        if (world < 0) return;
        var feet = WorldMapArt.Spots[world];
        bool space = feet.Y < 240;
        for (int i = 0; i < 6; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            if (space) AddBit(BitKind.Sparkle, feet + new Vector2(side * 12, -4), new Vector2(side * (40 + Dice() * 40), -20 - Dice() * 30), 0.45f, 4, WorldMapArt.StarDot, 0, onTop: false);
            else AddBit(BitKind.Puff, feet + new Vector2(side * 12, -2), new Vector2(side * (50 + Dice() * 40), -10 - Dice() * 15), 0.4f, 3.5f, WorldMapArt.DustColor, 0, onTop: false);
        }
    }

    /// <summary>Draws the flying bits: the ones under little Bolt-E (onTop = false), or the ones over everything (confetti).</summary>
    void DrawBits(CanvasItem canvas, bool onTop)
    {
        for (int i = 0; i < bits.Length; i++)
        {
            var bit = bits[i]; // (a copy: it's small)
            if (bit.Life <= 0 || bit.OnTop != onTop) continue;
            float left = bit.Life / bit.Lifetime; // 1 = just made, 0 = gone
            var color = new Color(bit.Color, bit.Color.A * Mathf.Min(1, left * 3));
            switch (bit.Kind)
            {
                case BitKind.Confetti: // a little paper square, fluttering (it turns, so it looks thinner and wider)
                {
                    float wide = Mathf.Max(1, bit.Size * Mathf.Abs(Mathf.Cos(bit.Spin)));
                    canvas.DrawRect(new Rect2(bit.Position - new Vector2(wide / 2, bit.Size * 0.4f), new Vector2(wide, bit.Size * 0.8f)), color);
                    break;
                }
                case BitKind.Sparkle:
                    WorldMapArt.Sparkle(canvas, bit.Position, bit.Size * (0.5f + 0.5f * left), color);
                    break;
                case BitKind.Puff: // it grows as it fades
                    canvas.DrawCircle(bit.Position, Mathf.Max(0.5f, bit.Size * (1.8f - 0.8f * left)), color);
                    break;
                default:
                    canvas.DrawCircle(bit.Position, Mathf.Max(0.5f, bit.Size), color);
                    break;
            }
        }
    }

    // ---------- Little Bolt-E on the map ----------

    /// <summary>
    /// Little Bolt-E is a real Robot (so every look, hat, board and robot shows), held in a little holder: the map only
    /// moves, flips and tilts the holder, because the Robot sets his own size and spot every frame.
    /// </summary>
    void MakeHero(Look look)
    {
        hero = new Robot { GroundY = 0 };  // (RunSpeed stays 0: no trail, no music notes, wheels standing still)
        hero.Reset();                      // (his feet at (HomeX, 0): standing, the right way up)
        Looks.Apply(hero, look);
        hero.SelfModulate = Colors.White;
        var undoHome = new Node2D { Position = new Vector2(-hero.HomeX, 0) }; // (so his feet are exactly on the holder)
        undoHome.AddChild(hero);
        heroSpot = new Node2D { Scale = Vector2.One * HeroSize, Visible = false };
        heroSpot.AddChild(undoHome);
        AddChild(heroSpot);
        MoveChild(heroSpot, 0); // (drawn over the board, under the panels and the GO bubble)
        PlaceHeroOn(Here);
    }

    void FreeHero()
    {
        heroSpot?.QueueFree();
        heroSpot = null;
        hero = null;
    }

    Vector2 heroFeet;

    void PlaceHeroOn(int world)
    {
        if (world >= 0) heroFeet = WorldMapArt.Spots[world];
    }

    /// <summary>The middle of little Bolt-E on the screen, even up in a hop (the eye closes and opens around it).</summary>
    Vector2 HeroMiddle => heroFeet + new Vector2(0, ((hero?.Position.Y ?? 0) - 60) * HeroSize);

    /// <summary>A small "this way!" hop (a jump that lets go right away).</summary>
    void SmallHop()
    {
        if (hero is null || !hero.OnGround) return;
        hero.Jump();
        smallHopNext = true;
    }

    /// <summary>
    /// Every frame: where little Bolt-E is (standing and hovering, travelling a road, or hopping), which way he faces, and
    /// whether he shows (inside the rocket or the space capsule, only the ride shows).
    /// </summary>
    void UpdateHero()
    {
        if (heroSpot is null || hero is null) return;
        if (smallHopNext)
        {
            smallHopNext = false;
            hero.ReleaseJump();
        }
        float facing = 1, lean = 0;
        switch (Step)
        {
            case MapStep.Travel when travelRoad >= 0:
            {
                float along = TravelAlong();
                heroFeet = WorldMapArt.PointOnRoad(travelRoad, along);
                var way = WorldMapArt.WayOfRoad(travelRoad, along);
                facing = way.X < -0.05f ? -1 : 1;
                lean = 0.08f * facing;
                if (RideNow is Ride.Sea or Ride.Capsule) heroFeet += new Vector2(0, Mathf.Sin(mapTime * 14) * 1.5f); // (riding the waves)
                if (RideNow == Ride.Space) heroFeet += new Vector2(0, Mathf.Sin(mapTime * 4) * 3);                  // (floating)
                break;
            }
            case MapStep.Stepping:
            {
                float t = Smooth(stepTime / StepSeconds);
                if (stepRoad >= 0)
                {
                    float length = WorldMapArt.RoadLengths[stepRoad];
                    float along = stepForward ? t * length : (1 - t) * length;
                    heroFeet = WorldMapArt.PointOnRoad(stepRoad, along);
                    var way = WorldMapArt.WayOfRoad(stepRoad, along) * (stepForward ? 1 : -1);
                    facing = way.X < -0.05f ? -1 : 1;
                }
                else
                {
                    var from = WorldMapArt.Spots[stepFrom];
                    var to = WorldMapArt.Spots[Here];
                    float height = Mathf.Clamp(StarHopHeight, 80, 160);
                    heroFeet = from.Lerp(to, t) + new Vector2(0, -height * 4 * t * (1 - t)); // up in an arc, and down again
                    facing = to.X < from.X ? -1 : 1;
                }
                break;
            }
            default:
                if (HeroOn >= 0) heroFeet = WorldMapArt.Spots[HeroOn];
                // Every 5 seconds he looks the other way for a moment
                if (Step == MapStep.Ready && Mathf.PosMod(mapTime, 5f) > 4.4f) facing = -1;
                break;
        }
        bool standing = HeroOn >= 0 && Step is not (MapStep.Travel or MapStep.Stepping);
        float hover = standing ? Mathf.Sin(mapTime * 5) * 3 - 3 : 0;
        heroSpot.Position = heroFeet + new Vector2(0, hover);
        heroSpot.Scale = new Vector2(facing * HeroSize, HeroSize);
        heroSpot.Rotation = lean;
        heroSpot.Visible = BoardShowing && !HeroInVehicle; // (inside the rocket and the capsule, only the ride shows)
        heroSpot.ZIndex = hero.OnGround ? 0 : 1; // (while he hops he's drawn over the GO bubble, never behind it, but under the eye)
    }

    // ---------- Drawing the board (under little Bolt-E) ----------

    public override void _Draw()
    {
        TextsDrawn.Clear();
        drawnArea = new Rect2(WorldMapArt.Spots[0], Vector2.Zero);
        RideDrawn = null;
        TeaserDrawn = null;
        if (!BoardShowing) return;
        TimesDrawn++;

        // The sea, and space at the top (with the GOLD MAP's rainbow, the stars, a shooting star and a ringed planet)
        WorldMapArt.SeaWater(this);
        DrawPolygon(spaceCorners, spaceColors);
        if (GoldMap)
        {
            float fade = Step == MapStep.AllClear ? Mathf.Clamp(stepTime / 1.2f, 0, 1) : 1; // (it fades in at the ALL CLEAR party)
            for (int i = 0; i < WorldMapArt.Rainbow.Length; i++)
                DrawArc(new Vector2(640, 470), 360 + i * 10, Mathf.Pi + 0.45f, Mathf.Tau - 0.45f, 48, new Color(WorldMapArt.Rainbow[i], 0.5f * fade), 10);
        }
        for (int i = 0; i < SkyStars.Length; i++)
        {
            float twinkle = 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(2.5f * mapTime + i));
            DrawCircle(SkyStars[i], 1 + (i % 3) * 0.6f, new Color(1, 1, 1, twinkle));
        }
        WorldMapArt.ShootingStar(this, mapTime);
        DrawCircle(new Vector2(440, 52), 18, new Color(1f, 0.6f, 0.35f));
        DrawSetTransform(new Vector2(440, 52), -0.3f, new Vector2(1, 0.25f));
        DrawArc(Vector2.Zero, 34, 0, Mathf.Tau, 32, new Color(1f, 0.85f, 0.6f), 8);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        // The cloud band between space and the sea (drifting right)
        for (int row = 0; row < 2; row++)
        {
            float y = row == 0 ? 236 : 254;
            var color = row == 0 ? new Color(0.97f, 0.98f, 1f) : new Color(0.88f, 0.93f, 1f);
            for (int i = 0; i < 15; i++)
            {
                float x = Mathf.PosMod(i * 92 + row * 46 + mapTime * 8, 1380) - 50;
                float r = (row == 0 ? 26 : 18) + (i * 7 % 9);
                DrawCircle(new Vector2(x, y), r, color);
            }
        }

        // Waves, the sailboat and a jumping fish
        for (int i = 0; i < Waves.Length; i++)
        {
            var at = Waves[i] + new Vector2(0, Mathf.Sin(mapTime * 2 + i) * 2);
            DrawArc(at, 7, 0.15f * Mathf.Pi, 0.85f * Mathf.Pi, 8, new Color(1, 1, 1, 0.45f + 0.3f * (0.5f + 0.5f * Mathf.Sin(mapTime + i))), 2.5f);
        }
        WorldMapArt.Sailboat(this, mapTime);
        WorldMapArt.Fish(this, mapTime);

        // The islands: shallow water, a beach with a dark edge, and the land (with bushes on the big island)
        WorldMapArt.Island(this, WorldMapArt.MainIsland, WorldMapArt.MainIslandLoop, WorldMapArt.Grass, WorldMapArt.GrassEdge, WorldMapArt.Beach, WorldMapArt.CoastEdge);
        WorldMapArt.Island(this, WorldMapArt.VolcanoIsland, WorldMapArt.VolcanoIslandLoop, WorldMapArt.VolcanoRock, WorldMapArt.VolcanoRockEdge,
                           WorldMapArt.DarkSand, WorldMapArt.VolcanoRockEdge);
        WorldMapArt.Bushes(this);

        // Each world's picture, or the cloud over it (puffing away in a CLOUD POOF). The rocket is off flying during its ride.
        bool rocketParked = !(Step == MapStep.Travel && RideNow == Ride.Rocket && RideT >= RocketBoardAt);
        for (int w = 0; w < 7; w++)
        {
            bool poofing = Step == MapStep.CloudPoof && w == poofWorld;
            if (!cloudOn[w] || poofing) WorldMapArt.Landmark(this, w, mapTime, rocketParked);
            drawnArea = drawnArea.Merge(WorldMapArt.LandmarkBoxes[w]);
        }
        for (int w = 0; w < 7; w++)
        {
            if (!cloudOn[w]) continue;
            float puff = Step == MapStep.CloudPoof && w == poofWorld ? Mathf.Clamp(stepTime / CloudPoofSeconds, 0, 1) : 0;
            drawnArea = drawnArea.Merge(WorldMapArt.Cloud(this, w, puff, mapTime, TextsDrawn));
        }

        // The roads' dots
        for (int road = 0; road < 7; road++) DrawRoad(road);

        // The worlds' dots, flags and (on the GOLD MAP) gold sparkle rings, and the PICKED ring
        for (int w = 0; w < 7; w++) DrawWorldDot(w);
        if (Mode == MapMode.Start && HeroOn >= 0 && Step != MapStep.GoIris)
        {
            float breathe = 0.5f + 0.5f * (0.5f + 0.5f * Mathf.Sin(mapTime * 4));
            DrawArc(WorldMapArt.Spots[HeroOn], 32, 0, Mathf.Tau, 32, new Color(WorldMapArt.Glow, breathe), 4);
        }

        // A world CLEARED again: a quick ring of sparkles round its flag
        if (Step == MapStep.Sparkle)
        {
            var flagTop = WorldMapArt.FlagTop(Here);
            float spread = 10 + 30 * Mathf.Clamp(stepTime / SparkleSeconds, 0, 1);
            for (int i = 0; i < 12; i++)
                Shapes.Star(this, flagTop + Vector2.Right.Rotated(i * Mathf.Tau / 12) * spread, 4, mapTime * 4, WorldMapArt.Gold);
        }

        // The ride (the rocket, the space capsule and its splash, the surf), and the bits flying under little Bolt-E
        DrawRide();
        DrawBits(this, onTop: false);

        // The next boss's badge, beside the world little Bolt-E stands on (it pops in, then hops now and then)
        if (TeaserShowing && HeroOn >= 0) DrawTeaser(HeroOn);
    }

    /// <summary>
    /// One road's dots: beige on land, white on the sea, little gold stars in space (all gold on the GOLD MAP). At the ALL
    /// CLEAR party a bright gold wave runs along the roads, all the way round the loop.
    /// </summary>
    void DrawRoad(int road)
    {
        int shownDots = dotsShown[road];
        if (shownDots <= 0) return;
        var dots = WorldMapArt.Dots[road];
        var kinds = WorldMapArt.DotKinds[road];
        bool revealing = Step == MapStep.PathReveal && road == partyWorld;
        float perDot = RevealLength(road) / Mathf.Max(1, dots.Length);
        float wave = Step == MapStep.AllClear ? (stepTime - road * 0.3f) / 0.4f : -9; // (where the gold wave is on this road: 0 to 1)
        for (int i = 0; i < shownDots && i < dots.Length; i++)
        {
            float scale = 1;
            if (revealing)
            {
                float age = stepTime - i * perDot; // (each dot pops: 0, a bit too big, then just right)
                scale = age < 0.075f ? Mathf.Clamp(age / 0.075f, 0.05f, 1) * 1.3f : age < 0.15f ? Mathf.Lerp(1.3f, 1, (age - 0.075f) / 0.075f) : 1;
            }
            var at = dots[i];
            if (GoldMap)
            {
                float lit = 1 - Mathf.Clamp(Mathf.Abs(i / (float)dots.Length - wave) * 5, 0, 1);
                float twinkle = 0.75f + 0.25f * Mathf.Sin(mapTime * 3 + i);
                DrawCircle(at, Mathf.Max(0.5f, (5.5f + 3 * lit) * scale), WorldMapArt.Ink);
                DrawCircle(at, Mathf.Max(0.5f, (4 + 3 * lit) * scale), new Color(WorldMapArt.GoldDot.Lerp(Colors.White, lit), twinkle + (1 - twinkle) * lit));
            }
            else if (kinds[i] == 2)
            {
                Shapes.Star(this, at, Mathf.Max(0.5f, 4.5f * scale), 0, WorldMapArt.StarDot);
            }
            else if (kinds[i] == 1)
            {
                DrawCircle(at, Mathf.Max(0.5f, 5.5f * scale), WorldMapArt.SeaDotRing);
                DrawCircle(at, Mathf.Max(0.5f, 4 * scale), Colors.White);
            }
            else
            {
                DrawCircle(at, Mathf.Max(0.5f, 5.5f * scale), WorldMapArt.Ink);
                DrawCircle(at, Mathf.Max(0.5f, 4 * scale), WorldMapArt.LandDot);
            }
            drawnArea = drawnArea.Merge(new Rect2(at - new Vector2(9, 9), new Vector2(18, 18)));
        }
    }

    /// <summary>A world's dot: grey stone (LOCKED), a red dot that pulses (OPEN), or blue with a gold star and a flag (CLEARED).</summary>
    void DrawWorldDot(int w)
    {
        var spot = WorldMapArt.Spots[w];
        DrawCircle(spot + new Vector2(0, 4), 26, new Color(0, 0, 0, 0.18f)); // a soft shadow under it
        switch (shown[w])
        {
            case MapNodeState.Locked:
                DrawCircle(spot, 21.5f, new Color(0.36f, 0.38f, 0.45f));
                DrawCircle(spot, 20, WorldMapArt.StoneGrey);
                DrawCircle(spot + new Vector2(-5, -5), 6, new Color(0.68f, 0.7f, 0.75f));
                break;
            case MapNodeState.Open:
                float pulse = 1 + 0.08f * Mathf.Sin(mapTime * 4);
                DrawCircle(spot, 26 * pulse, WorldMapArt.Ink);
                DrawCircle(spot, 22 * pulse, WorldMapArt.OpenRed);
                DrawCircle(spot + new Vector2(-6, -6), 7, new Color(1f, 0.55f, 0.5f));
                break;
            default:
                DrawCircle(spot, 26, WorldMapArt.Ink);
                DrawCircle(spot, 22, GoldMap ? WorldMapArt.GoldDot : WorldMapArt.ClearedBlue);
                DrawCircle(spot + new Vector2(-6, -7), 6, new Color(1, 1, 1, 0.25f));
                Shapes.Star(this, spot, 12, 0, GoldMap ? Colors.White : WorldMapArt.Gold);
                WorldMapArt.Flag(this, w, flagRise[w], Mathf.Sin(6 * mapTime + w) * 3);
                drawnArea = drawnArea.Merge(WorldMapArt.FlagRect(w));
                if (GoldMap) drawnArea = drawnArea.Merge(WorldMapArt.GoldRing(this, w, mapTime));
                break;
        }
        drawnArea = drawnArea.Merge(new Rect2(spot - new Vector2(30, 30), new Vector2(60, 64)));
    }

    /// <summary>The ride little Bolt-E is on (between levels): the rocket, the space capsule with its parachute and its SPLOOSH, or his surf's foam.</summary>
    void DrawRide()
    {
        if (Step != MapStep.Travel || travelRoad < 0) return;
        float t = RideT, along = TravelAlong();
        var at = WorldMapArt.PointOnRoad(travelRoad, along);
        var way = WorldMapArt.WayOfRoad(travelRoad, along);
        switch (RideNow)
        {
            case Ride.Rocket when t >= RocketBoardAt:
            {
                bool flying = t >= RocketLiftAt;
                var bottom = flying ? at : WorldMapArt.RocketPad + new Vector2(Mathf.Sin(mapTime * 70) * 1.5f, -2); // (it shakes before it goes)
                float turn = flying ? Mathf.Atan2(way.X, -way.Y) : 0;                                                  // (turned the way it flies)
                float flame = flying ? 1 : 0.3f + 0.2f * Mathf.Sin(mapTime * 25);
                drawnArea = drawnArea.Merge(WorldMapArt.Rocket(this, bottom, turn, flame, true, mapTime));
                RideDrawn = Ride.Rocket;
                break;
            }
            case Ride.Capsule:
            {
                if (t < CapsuleSplashAt)
                {
                    float chute = Mathf.Clamp((at.Y - 205) / 25, 0, 1); // (the parachute opens at the clouds, at y 205)
                    drawnArea = drawnArea.Merge(WorldMapArt.Capsule(this, at + new Vector2(0, -8), chute, 0, true, mapTime));
                }
                else
                {
                    float since = (t - CapsuleSplashAt) * WorldMapArt.RideSeconds[travelRoad]; // (seconds since SPLOOSH)
                    float sink = t >= CapsuleSurfAt ? (t - CapsuleSurfAt) / (1 - CapsuleSurfAt) * 1.6f : 0; // (bloop: the empty capsule sinks)
                    if (sink < 0.9f)
                    {
                        var bobbing = WorldMapArt.SplashSpot + new Vector2(0, -6 + Mathf.Sin(since * 9) * 2 + sink * 14);
                        drawnArea = drawnArea.Merge(WorldMapArt.Capsule(this, bobbing, Mathf.Max(0, 1 - since * 6), sink, t < CapsuleSurfAt, mapTime)); // (the parachute folds up)
                        WorldMapArt.Afloat(this, WorldMapArt.SplashSpot);
                    }
                    drawnArea = drawnArea.Merge(WorldMapArt.Splash(this, WorldMapArt.SplashSpot, since));
                    if (t >= CapsuleSurfAt) WorldMapArt.Wake(this, heroFeet, way, mapTime);
                }
                RideDrawn = Ride.Capsule;
                break;
            }
            case Ride.Sea:
                WorldMapArt.Wake(this, heroFeet, way, mapTime);
                RideDrawn = Ride.Sea;
                break;
            case Ride.Space:
                RideDrawn = Ride.Space; // (his glass bubble is drawn over him: see DrawCover)
                break;
        }
    }

    /// <summary>The next boss's badge: a round medallion with his face (BIG RUSTY wears the next world's outfit).</summary>
    void DrawTeaser(int world)
    {
        float pop = Mathf.Clamp(teaserAge / TeaserPopSeconds, 0, 1);
        float scale = pop < 0.6f ? pop / 0.6f * 1.15f : Mathf.Lerp(1.15f, 1, (pop - 0.6f) / 0.4f);
        float hop = Mathf.Max(0, Mathf.Sin(mapTime * Mathf.Tau / 1.5f)) * 6;
        var middle = WorldMapArt.Spots[world] + WorldMapArt.TeaserOffsets[world] - new Vector2(0, hop);
        drawnArea = drawnArea.Merge(WorldMapArt.Teaser(this, middle, scale, TeaserBoss, TeaserOutfit, mapTime));
        TeaserDrawn = (TeaserBoss, TeaserOutfit);
    }

    // ---------- Drawing the cover (on top of little Bolt-E): panels, the name box, NEXT, the GO bubble and the words ----------

    internal void DrawCover(CanvasItem canvas)
    {
        SpaceBubbleDrawn = false;
        if (!BoardShowing) return;

        // The corner panel: this game's score, bolts and batteries (between levels), or the player's flags (the start map)
        var panel = WorldMapArt.CornerPanel;
        Shapes.RoundRect(canvas, panel, 16, WorldMapArt.Ink);
        Shapes.RoundRect(canvas, panel.Grow(-3), 13, new Color(0.08f, 0.11f, 0.24f));
        drawnArea = drawnArea.Merge(panel);
        var font = ThemeDB.FallbackFont;
        if (Mode == MapMode.Between)
        {
            LeftWords(canvas, font, scoreText, 24, 14 + font.GetAscent(36), 36, Colors.White);
            LeftWords(canvas, font, boltsText, 24, 58 + font.GetAscent(28), 28, WorldMapArt.Gold);
        }
        else
        {
            LeftWords(canvas, font, ownerName, 24, 48, ownerSize, new Color(1f, 0.45f, 0.6f));
            if (GoldMap) drawnArea = drawnArea.Merge(WorldMapArt.Crown(canvas, new Vector2(24 + ownerWidth + 10, 44))); // a crown: all 7 CLEARED!
            LeftWords(canvas, font, ownerCount, 24, 82, 22, WorldMapArt.Gold);
            for (int w = 0; w < 7; w++)
            {
                var pole = new Vector2(30 + 30 * w, 98);
                canvas.DrawLine(pole, pole + new Vector2(0, 34), Colors.White, 2);
                var cleared = shown[w] == MapNodeState.Cleared;
                WorldMapArt.Quad(canvas, pole, pole + new Vector2(16, 3), pole + new Vector2(16, 13), pole + new Vector2(0, 16),
                                 cleared ? WorldMapArt.Gold : new Color(0.45f, 0.47f, 0.55f));
            }
        }

        // The name box at the bottom: where little Bolt-E is (or is going), and a second line
        var box = WorldMapArt.NameBox;
        Shapes.RoundRect(canvas, box, 14, WorldMapArt.Gold);
        Shapes.RoundRect(canvas, box.Grow(-4), 11, new Color(0.12f, 0.16f, 0.32f));
        drawnArea = drawnArea.Merge(box);
        var nameArea = Say(canvas, NameLine, 640, 666, nameSize, Colors.White, 6);
        if (Mode == MapMode.Start)
        {
            float middleY = 666 - nameSize * 0.36f;
            WorldMapArt.Triangle(canvas, new Vector2(nameArea.Position.X - 44, middleY), new Vector2(nameArea.Position.X - 20, middleY - 14),
                                 new Vector2(nameArea.Position.X - 20, middleY + 14), WorldMapArt.Glow);
            WorldMapArt.Triangle(canvas, new Vector2(nameArea.End.X + 44, middleY), new Vector2(nameArea.End.X + 20, middleY - 14),
                                 new Vector2(nameArea.End.X + 20, middleY + 14), WorldMapArt.Glow);
        }
        Say(canvas, PromptLine, 640, 701, promptSize, new Color(0.8f, 0.88f, 1f), 4);

        // The bouncing NEXT arrow
        if (NextArrowOn >= 0) DrawNextArrow(canvas, NextArrowOn);

        // The glass bubble little Bolt-E floats in through space (over him: he's inside it)
        if (InSpaceBubble && heroSpot is { Visible: true })
        {
            drawnArea = drawnArea.Merge(WorldMapArt.SpaceBubble(canvas, HeroMiddle, mapTime));
            SpaceBubbleDrawn = true;
        }

        // The GO bubble over little Bolt-E
        if (GoBubbleShowing && HeroOn >= 0) DrawGoBubble(canvas, HeroOn);

        // "NEW!" after a cloud puffed away
        if (newLeft > 0 && newWordsOn >= 0)
        {
            var at = WorldMapArt.NewWordsAt(newWordsOn);
            Say(canvas, "NEW!", at.X, at.Y + 10, 28, WorldMapArt.Glow, 6);
        }

        // The ALL CLEAR fireworks, and the confetti
        if (Step == MapStep.AllClear) DrawFireworks(canvas);
        DrawBits(canvas, onTop: true);

        // The big words: "SUNNY HILLS CLEAR!", "ALL 7 WORLDS CLEAR!", "ALL THE WAY AROUND!"...
        if (BigWords.Length > 0) Say(canvas, BigWords, 640, WorldMapArt.BigWordsBaseline, bigSize, WorldMapArt.Gold, bigSize / 5);
        if (SmallWords.Length > 0) Say(canvas, SmallWords, 640, WorldMapArt.BigWordsBaseline + 42, 30, Colors.White, 6);
    }

    /// <summary>
    /// The ALL CLEAR fireworks: each one bursts into two rings of sparks (its color outside, white inside) that spread
    /// out, fall a little and fade away.
    /// </summary>
    void DrawFireworks(CanvasItem canvas)
    {
        const float Life = 1.5f;
        for (int i = 0; i < FireworksPopped; i++)
        {
            float age = stepTime - i * FireworkEvery;
            if (age < 0 || age > Life) continue;
            var middle = FireworkSpots[i % FireworkSpots.Length];
            var color = WorldMapArt.PartyColors[i % WorldMapArt.PartyColors.Length];
            float opened = 1 - Mathf.Pow(1 - Mathf.Min(1, age / 0.6f), 2); // (fast at first, then slower)
            float fade = 1 - (age / Life) * (age / Life);                   // (bright for a while, then it fades)
            float drop = 0.5f * Gravity * 0.3f * age * age;
            if (age < 0.15f) canvas.DrawCircle(middle, 24 - 110 * age, new Color(1, 1, 1, 0.9f)); // the flash
            for (int j = 0; j < 18; j++)
            {
                var way = Vector2.Right.Rotated(j * Mathf.Tau / 18 + i * 0.4f);
                var spark = middle + way * 88 * opened + new Vector2(0, drop);
                canvas.DrawLine(spark - way * 16 * opened, spark, new Color(color, fade * 0.6f), 3); // its tail
                canvas.DrawCircle(spark, 4.5f * fade + 1, new Color(color, fade));
                if (j % 2 == 0) canvas.DrawCircle(middle + way * 48 * opened + new Vector2(0, drop), 3 * fade + 0.8f, new Color(1, 1, 1, fade));
            }
        }
    }

    /// <summary>Words starting at x (on the baseline), in ink-outlined letters.</summary>
    void LeftWords(CanvasItem canvas, Font font, string text, float x, float baseline, int size, Color color)
    {
        var at = new Vector2(x, baseline);
        canvas.DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, Math.Max(6, size / 6), WorldMapArt.Ink);
        canvas.DrawString(font, at, text, HorizontalAlignment.Left, -1, size, color);
        TextsDrawn.Add(text);
    }

    /// <summary>Centered words (see WorldMapArt.Words), remembered for the self-test.</summary>
    Rect2 Say(CanvasItem canvas, string text, float x, float baseline, int size, Color color, int outline)
    {
        TextsDrawn.Add(text);
        var area = WorldMapArt.Words(canvas, text, x, baseline, size, color, outline);
        drawnArea = drawnArea.Merge(area);
        return area;
    }

    /// <summary>The NEXT arrow over a world: "NEXT!" and a gold arrow pointing at it, bouncing.</summary>
    void DrawNextArrow(CanvasItem canvas, int world)
    {
        var box = WorldMapArt.NextRect(world);
        var point = WorldMapArt.NextPoints[world];
        float bounce = Mathf.Sin(mapTime * Mathf.Tau * 2) * 4 - 4; // (up to 8 pixels)
        var shift = point * -bounce;
        if (point == Vector2.Down)
        {
            Say(canvas, "NEXT!", box.GetCenter().X + shift.X, box.Position.Y + 20 + shift.Y, 22, WorldMapArt.Gold, 5);
            WorldMapArt.Arrow(canvas, new Vector2(box.GetCenter().X, box.End.Y) + shift, point, 44, WorldMapArt.Gold);
        }
        else if (point == Vector2.Up)
        {
            WorldMapArt.Arrow(canvas, new Vector2(box.GetCenter().X, box.Position.Y) + shift, point, 44, WorldMapArt.Gold);
            Say(canvas, "NEXT!", box.GetCenter().X + shift.X, box.End.Y + shift.Y, 22, WorldMapArt.Gold, 5);
        }
        else
        {
            Say(canvas, "NEXT!", box.GetCenter().X + shift.X, box.Position.Y + 20 + shift.Y, 22, WorldMapArt.Gold, 5);
            WorldMapArt.Arrow(canvas, new Vector2(box.End.X, box.End.Y - 22) + shift, point, 44, WorldMapArt.Gold);
        }
        drawnArea = drawnArea.Merge(box.Grow(8));
    }

    /// <summary>The GO bubble: a white speech bubble with the 4 colored buttons (one lights up at a time) and "GO!".</summary>
    void DrawGoBubble(CanvasItem canvas, int world)
    {
        var box = WorldMapArt.BubbleRect(world);
        float pop = Mathf.Clamp(bubbleAge / BubblePopSeconds, 0, 1);
        float scale = Mathf.Max(0.05f, pop < 0.6f ? pop / 0.6f * 1.15f : Mathf.Lerp(1.15f, 1, (pop - 0.6f) / 0.4f));
        var middle = box.GetCenter();
        var bubble = new Rect2(middle - box.Size * scale / 2, box.Size * scale);
        var tip = WorldMapArt.Spots[world] + WorldMapArt.TailTips[world];
        // The tail starts at the bubble's edge, as close to the tip as it can (the bubble can be tiny while it pops in)
        float insetX = Mathf.Min(16, bubble.Size.X / 2), insetY = Mathf.Min(16, bubble.Size.Y / 2);
        var near = new Vector2(Mathf.Max(bubble.Position.X + insetX, Mathf.Min(tip.X, bubble.End.X - insetX)),
                               Mathf.Max(bubble.Position.Y + insetY, Mathf.Min(tip.Y, bubble.End.Y - insetY)));
        var side = (tip - near).Normalized().Orthogonal() * 10 * scale;
        WorldMapArt.Triangle(canvas, near + side * 1.3f, tip, near - side * 1.3f, WorldMapArt.Ink); // the tail, pointing at him
        Shapes.RoundRect(canvas, bubble, 14 * scale, WorldMapArt.Ink);
        Shapes.RoundRect(canvas, bubble.Grow(-3 * scale), 11 * scale, Colors.White);
        WorldMapArt.Triangle(canvas, near + side, tip - (tip - near).Normalized() * 4, near - side, Colors.White);
        if (pop >= 0.6f)
        {
            // The 4 face buttons: Y on top, X left, B right, A at the bottom (the lit one is bigger)
            var buttons = bubble.Position + new Vector2(42, 29) * scale;
            int lit = (int)(mapTime / 0.25f) % 4;
            for (int i = 0; i < 4; i++)
            {
                var spot = buttons + ButtonSpots[i] * scale;
                float r = 11 * scale * (i == lit ? 1.25f : 1);
                canvas.DrawCircle(spot, r + 2, WorldMapArt.Ink);
                canvas.DrawCircle(spot, r, ButtonColors[i]);
            }
            var font = ThemeDB.FallbackFont;
            var at = bubble.Position + new Vector2(76, 41) * scale;
            canvas.DrawStringOutline(font, at, "GO!", HorizontalAlignment.Left, -1, 34, 7, WorldMapArt.Ink);
            canvas.DrawString(font, at, "GO!", HorizontalAlignment.Left, -1, 34, WorldMapArt.Gold);
            TextsDrawn.Add("GO!");
        }
        drawnArea = drawnArea.Merge(box);
    }

    static readonly Vector2[] ButtonSpots = { new(0, -14), new(-14, 0), new(14, 0), new(0, 14) };
    static readonly Color[] ButtonColors = { new(1f, 0.82f, 0.15f), new(0.25f, 0.5f, 0.95f), new(0.9f, 0.25f, 0.25f), new(0.35f, 0.8f, 0.3f) };
}

/// <summary>The WORLD MAP's top layer, on top of little Bolt-E: the panels, the name box, NEXT, the GO bubble and the words.</summary>
public partial class WorldMapCover : Control
{
    public WorldMap Map = null!;

    public override void _Draw() => Map.DrawCover(this);
}
