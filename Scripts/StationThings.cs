using Godot;

/// <summary>
/// UPSIDE-DOWN STATION's dangers! (This is more of Obstacle: "partial" means one class spread over several files.)
///   SPIKE STRIPS: a low, wide row of glowing spikes.
///   LASER GATES:  a laser beam crackling between two emitters (a short glowing post).
///   SPACE JUNK:   an old broken satellite, slowly spinning in the MIDDLE of the corridor. Don't flip into it!
/// Spikes and laser gates stand on the floor, or hang from the ceiling (then Hanging is true and the drawing is turned
/// upside-down). The GAPS in the floor and the ceiling are StationGap (below), and the planner that puts all of them in
/// fair patterns is StationPlanner (at the bottom).
/// </summary>
public partial class Obstacle
{
    // ---- Station dangers (try changing these!) ----
    public const float SpikeHalfWidth = 46;   // the spike strip's bump box is 92 pixels wide...
    public const float SpikeHeight = 30;      // ...and 30 pixels tall
    public const float LaserHalfWidth = 13;   // the laser gate's bump box is 26 pixels wide...
    public const float LaserHeight = 112;     // ...and 112 pixels tall
    public const float JunkHalfSize = 28;     // the space junk's bump box is 56 x 56
    public const float JunkSpinSpeed = 1.2f;  // how fast the junk spins (turns per second x 6.28)

    /// <summary>True for a spike strip or laser gate hanging from the ceiling (it's drawn upside-down).</summary>
    public bool Hanging;

    /// <summary>How high space junk floats above the floor (its smashed pieces fall that far).</summary>
    public float FloatHeight;

    static readonly Color SpikeMetal = new(0.72f, 0.74f, 0.82f);
    static readonly Color SpikeDark = new(0.3f, 0.3f, 0.4f);
    static readonly Color NeonPink = new(1f, 0.3f, 0.75f);
    static readonly Color LaserRed = new(1f, 0.2f, 0.35f);
    static readonly Color EmitterGrey = new(0.36f, 0.36f, 0.46f);
    static readonly Color PanelBlue = new(0.25f, 0.45f, 0.95f);
    static readonly Color JunkGrey = new(0.62f, 0.62f, 0.7f);

    // Corner points reused every frame (DrawColoredPolygon copies them, so no new lists are made all the time)
    static readonly Vector2[] spike = new Vector2[3];
    static readonly Vector2[] BentPanel = { new(20, -6), new(46, -16), new(46, 4), new(20, 8) };

    /// <summary>The bump box of a station danger. (Upside-down when it hangs from the ceiling.)</summary>
    Rect2 StationHitbox()
    {
        var box = Kind switch
        {
            ObstacleKind.SpikeStrip => new Rect2(-SpikeHalfWidth, -SpikeHeight, SpikeHalfWidth * 2, SpikeHeight),
            ObstacleKind.LaserGate => new Rect2(-LaserHalfWidth, -LaserHeight, LaserHalfWidth * 2, LaserHeight),
            _ => new Rect2(-JunkHalfSize, -JunkHalfSize, JunkHalfSize * 2, JunkHalfSize * 2), // (junk: its middle is at Position)
        };
        if (Hanging) box.Position = new Vector2(box.Position.X, -box.End.Y); // (turned upside-down: it hangs down)
        return new Rect2(Position + box.Position, box.Size);
    }

    /// <summary>Draws a station danger (Hanging ones are turned upside-down by the node's Scale).</summary>
    void DrawStationThing()
    {
        switch (Kind)
        {
            case ObstacleKind.SpikeStrip:
                DrawSpikes();
                break;
            case ObstacleKind.LaserGate:
                DrawLaserBase();
                DrawLaserTop();
                DrawLaserBeam();
                break;
            case ObstacleKind.SpaceJunk:
                DrawSetTransform(Vector2.Zero, time * JunkSpinSpeed, Vector2.One);
                DrawJunk();
                DrawSetTransform(Vector2.Zero, 0, Vector2.One);
                break;
        }
    }

    /// <summary>The pieces a station danger bursts into when MEGA BOLT-E smashes it.</summary>
    void AddStationParts(List<RobotPart> parts)
    {
        switch (Kind)
        {
            case ObstacleKind.SpikeStrip:
                parts.Add(new RobotPart("spikes", new Vector2(0, -14), 30, DrawSpikes, FlatEvery: Mathf.Pi));
                break;
            case ObstacleKind.LaserGate:
                parts.Add(new RobotPart("laser bottom", new Vector2(0, -8), 16, DrawLaserBase, FlatEvery: Mathf.Pi));
                parts.Add(new RobotPart("laser top", new Vector2(0, -105), 16, DrawLaserTop, FlatEvery: Mathf.Pi));
                break;
            default:
                parts.Add(new RobotPart("junk", Vector2.Zero, 30, DrawJunk, FlatEvery: Mathf.Pi / 2));
                break;
        }
    }

    /// <summary>A metal strip with 6 sharp spikes, their tips glowing pink.</summary>
    void DrawSpikes()
    {
        Shapes.RoundRect(this, new Rect2(-52, -8, 104, 8), 3, SpikeDark); // the base plate
        float glow = 0.6f + 0.4f * Mathf.Sin(time * 6);
        for (int i = 0; i < 6; i++)
        {
            float x = -45 + i * 18;
            spike[0] = new Vector2(x - 8, -7);
            spike[1] = new Vector2(x + 8, -7);
            spike[2] = new Vector2(x, -34);
            DrawColoredPolygon(spike, SpikeMetal);
            DrawLine(new Vector2(x, -33), new Vector2(x + 4, -16), Colors.White, 1.5f, true); // shine
            DrawCircle(new Vector2(x, -31), 3.5f, new Color(NeonPink, glow));                 // glowing tip
        }
    }

    /// <summary>The laser gate's bottom emitter (on the floor, or on the ceiling when it hangs).</summary>
    void DrawLaserBase()
    {
        Shapes.RoundRect(this, new Rect2(-18, -16, 36, 16), 4, EmitterGrey);
        DrawRect(new Rect2(-12, -20, 24, 5), SpikeDark);
        DrawCircle(new Vector2(0, -16), 5, LaserRed);
    }

    /// <summary>The laser gate's top emitter, at the other end of the beam.</summary>
    void DrawLaserTop()
    {
        Shapes.RoundRect(this, new Rect2(-14, -LaserHeight, 28, 14), 4, EmitterGrey);
        DrawCircle(new Vector2(0, -LaserHeight + 14), 5, LaserRed);
    }

    /// <summary>The beam itself: a wide soft glow, a bright core, and a white-hot middle that crackles.</summary>
    void DrawLaserBeam()
    {
        float flicker = 0.75f + 0.25f * Mathf.Sin(time * 47);
        var from = new Vector2(0, -18);
        var to = new Vector2(0, -LaserHeight + 14);
        DrawLine(from, to, new Color(LaserRed, 0.25f * flicker), 22);
        DrawLine(from, to, new Color(LaserRed, 0.9f), 8);
        DrawLine(from, to, new Color(1, 0.9f, 0.95f, flicker), 3);
        float spark = Mathf.PosMod(time * 3, 1); // a little spark zips along the beam
        DrawCircle(from.Lerp(to, spark), 4, Colors.White);
    }

    /// <summary>An old broken satellite: a box with a dish, and two blue solar panels (one of them bent).</summary>
    void DrawJunk()
    {
        DrawRect(new Rect2(-46, -9, 26, 18), PanelBlue);                         // left panel
        DrawLine(new Vector2(-33, -9), new Vector2(-33, 9), SpikeDark, 2);
        DrawColoredPolygon(BentPanel, PanelBlue);                               // right panel (bent!)
        Shapes.RoundRect(this, new Rect2(-20, -20, 40, 40), 6, JunkGrey);       // the body
        DrawRect(new Rect2(-12, -12, 24, 24), SpikeDark);
        DrawCircle(new Vector2(0, 0), 6, new Color(NeonPink, 0.6f + 0.4f * Mathf.Sin(time * 5))); // a blinking light
        DrawLine(new Vector2(0, -20), new Vector2(6, -34), JunkGrey, 3, true);    // the antenna...
        DrawCircle(new Vector2(6, -34), 4, Colors.White);                         // ...with a ball on top
    }
}

/// <summary>
/// A GAP in UPSIDE-DOWN STATION's floor or ceiling: a missing piece of track, with space showing through it.
/// Riding onto it while gravity pulls toward it = falling out into space (whoops!). Its Position is the middle of the
/// gap, on the floor's (or the ceiling's) surface.
/// </summary>
public partial class StationGap : Node2D
{
    public bool Ceiling;      // true = a gap in the ceiling (it's drawn upside-down)
    public float Width = 180; // how wide the gap is (pixels)
    float time;

    static readonly Color Space = new(0.03f, 0.01f, 0.08f);
    static readonly Color Hazard = new(1f, 0.8f, 0.1f);
    static readonly Color HazardGlow = new(1f, 0.35f, 0.2f);
    static readonly float[] Sides = { -1, 1 };

    /// <summary>Where the gap starts and ends (left and right).</summary>
    public float Left => Position.X - Width / 2;
    public float Right => Position.X + Width / 2;

    public override void _Ready() => Scale = new Vector2(1, Ceiling ? -1 : 1);

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float half = Width / 2;
        // Deep space, all the way down past the bottom of the screen (or up past the top for the ceiling)
        DrawRect(new Rect2(-half, 0, Width, 230), Space);
        // A soft red glow coming up out of the hole: DANGER!
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 6);
        DrawRect(new Rect2(-half, 0, Width, 10), new Color(HazardGlow, 0.25f + 0.25f * pulse));
        // Stars twinkling far away down there
        for (int i = 0; i < 5; i++)
        {
            float x = -half + 20 + (i * 37 % 100) / 100f * (Width - 40);
            float y = 30 + i * 23 % 90;
            DrawCircle(new Vector2(x, y), 1.5f + i % 2, new Color(1, 1, 1, 0.5f + 0.5f * Mathf.Sin(time * 3 + i)));
        }
        // Yellow hazard edges on both sides of the hole, with black stripes
        foreach (float side in Sides)
        {
            float x = side * half - (side > 0 ? 8 : 0);
            DrawRect(new Rect2(x, 0, 8, 26), Hazard);
            for (int s = 0; s < 3; s++)
                DrawLine(new Vector2(x, 4 + s * 8), new Vector2(x + 8, 9 + s * 8), Colors.Black, 2.5f);
            DrawCircle(new Vector2(x + 4, -4), 3.5f, new Color(HazardGlow, pulse)); // a blinking warning light
        }
    }
}

/// <summary>Where a station danger is: on the floor, on the ceiling, or floating in the middle.</summary>
public enum StationSide { Floor, Ceiling, Middle }

/// <summary>The four kinds of station danger.</summary>
public enum StationDangerKind { Spikes, Laser, Gap, Junk }

/// <summary>
/// One danger the planner put on the track. Left and Right are "track" positions: how far along the whole ride it is
/// (in pixels), so they never change while the world scrolls. Pattern says which pattern it belongs to.
/// </summary>
public readonly record struct StationDanger(StationDangerKind Kind, StationSide Side, float Left, float Right, int Pattern)
{
    public float Middle => (Left + Right) / 2;
}

/// <summary>
/// Puts UPSIDE-DOWN STATION's dangers on the track in FAIR patterns. These rules are kept by the code, not by luck
/// (and the self-test checks them on thousands of patterns):
///   1. All the floor-or-ceiling dangers in one pattern are on the SAME side, so one flip (or none) gets you through.
///   2. A danger is never on the floor and the ceiling at the same spot.
///   3. Between needing to be on one side and needing to be on the other, there's always time to see it coming AND to
///      flip across: ReactionSeconds + FlipSeconds, with no space junk in the way.
///   4. The first EasySeconds in the station are easy: one danger at a time, far apart. Space junk only comes later.
/// </summary>
public sealed class StationPlanner
{
    // ---- Fair patterns (try changing these!) ----
    public const float ReactionSeconds = 0.6f;   // time to see a danger coming before you have to start flipping
    public const float FlipSeconds = 0.37f;      // how long a flip across the corridor takes (see Robot.FlipGravity)
    public const float EasySeconds = 20f;        // the first 20 seconds in the station are easy
    public const float JunkFromSeconds = 40f;    // space junk floats by only after this many seconds in the station
    public const float GapMin = 150, GapMax = 230; // how wide the gaps in the floor and ceiling are (pixels)
    public const float HalfBody = 28;            // half of Bolt-E's bump box (he's 56 pixels wide)

    /// <summary>The side of the last floor or ceiling danger (Bolt-E has to be on the OTHER side for it).</summary>
    public StationSide LastSide { get; private set; } = StationSide.Ceiling;
    /// <summary>Where that danger ends on the track.</summary>
    public float LastEnd { get; private set; }
    /// <summary>Where the last space junk ends on the track (you can't flip through it).</summary>
    public float LastJunkEnd { get; private set; } = float.MinValue;
    /// <summary>The name of the last pattern, like "row" (the self-test reads this).</summary>
    public string LastPattern { get; private set; } = "";
    /// <summary>True if the last pattern needs a flip first: then Bolt-E flips between SwitchFrom and SwitchTo.</summary>
    public bool Switched { get; private set; }
    public float SwitchFrom { get; private set; }
    public float SwitchTo { get; private set; }
    /// <summary>How many patterns were made (each danger remembers its pattern's number).</summary>
    public int Patterns { get; private set; }

    /// <summary>
    /// How much room a flip from one side to the other needs at this speed: time to react, time to flip, and Bolt-E's
    /// own width on both sides (he must be all the way past the first danger, and not yet at the next one).
    /// </summary>
    public static float SwitchRoom(float speed) => (ReactionSeconds + FlipSeconds) * speed + 2 * HalfBody;

    /// <summary>
    /// A fresh start, with Bolt-E on the floor at track position "track": as if the last danger was on the ceiling
    /// and has just gone by.
    /// </summary>
    public void Reset(float track)
    {
        LastSide = StationSide.Ceiling;
        LastEnd = track;
        LastJunkEnd = float.MinValue;
        LastPattern = "";
        Switched = false;
        Patterns = 0;
    }

    static StationSide Other(StationSide side) => side == StationSide.Floor ? StationSide.Ceiling : StationSide.Floor;

    /// <summary>
    /// Plans the next pattern: its dangers go into "into" (track positions, never before "start"). "speed" is how fast
    /// the world will go (pixels per second), "seconds" how long Bolt-E has been in the station. Returns where the pattern
    /// ends on the track.
    /// </summary>
    public float Plan(float start, float speed, float seconds, Random random, List<StationDanger> into)
    {
        into.Clear();
        Patterns++;
        Switched = false;
        bool easy = seconds < EasySeconds;
        bool junkOk = seconds >= JunkFromSeconds;
        int roll = random.Next(100);

        // Space junk all by itself: ride along on either side, just don't flip into it
        if (!easy && junkOk && roll < 15)
        {
            float x = Mathf.Max(start, Mathf.Max(LastEnd, LastJunkEnd) + 0.4f * speed + 60);
            AddJunk(into, x);
            LastPattern = "junk";
            return LastJunkEnd;
        }

        // Every other pattern: dangers on one side only (Bolt-E rides on the other side)
        var side = random.Next(2) == 0 ? StationSide.Floor : StationSide.Ceiling;
        float first = start;
        if (side != LastSide)
        {
            // He has to flip to the other side first: room to react and flip, after the last danger AND after any junk
            float clear = Mathf.Max(LastEnd, LastJunkEnd);
            first = Mathf.Max(first, clear + SwitchRoom(speed));
            Switched = true;
            SwitchFrom = clear;
        }
        else
        {
            first = Mathf.Max(first, LastEnd + 0.3f * speed + 60); // (staying on the same side: a little room to see it)
        }
        if (easy) first = Mathf.Max(first, LastEnd + 1.6f * speed); // easy: lots of room between dangers
        SwitchTo = first;

        float end;
        if (easy || roll < 45)
        {
            end = AddSideDanger(into, RandomKind(random), side, first, random);
            LastPattern = easy ? "easy" : "single";
        }
        else if (junkOk && roll < 65)
        {
            // A tunnel: a danger, space junk in the middle, and another danger. Ride through on the other side!
            end = AddSideDanger(into, RandomKind(random), side, first, random);
            AddJunk(into, end + 90 + 0.15f * speed);
            end = AddSideDanger(into, RandomKind(random), side, LastJunkEnd + 90 + 0.15f * speed, random);
            LastPattern = "junk tunnel";
        }
        else
        {
            // A row: 2 or 3 dangers on the same side
            int count = random.Next(2, 4);
            end = first;
            for (int i = 0; i < count; i++)
                end = AddSideDanger(into, RandomKind(random), side, i == 0 ? first : end + 110 + 0.2f * speed + random.Next(120), random);
            LastPattern = "row";
        }

        LastSide = side;
        LastEnd = end;
        return Mathf.Max(end, LastJunkEnd);
    }

    /// <summary>Spikes (4 in 10), a laser gate (3 in 10) or a gap (3 in 10).</summary>
    static StationDangerKind RandomKind(Random random)
    {
        int roll = random.Next(10);
        return roll < 4 ? StationDangerKind.Spikes : roll < 7 ? StationDangerKind.Laser : StationDangerKind.Gap;
    }

    /// <summary>Adds one floor or ceiling danger starting at "left", and returns where it ends.</summary>
    float AddSideDanger(List<StationDanger> into, StationDangerKind kind, StationSide side, float left, Random random)
    {
        float width = kind switch
        {
            StationDangerKind.Spikes => Obstacle.SpikeHalfWidth * 2,
            StationDangerKind.Laser => Obstacle.LaserHalfWidth * 2,
            _ => Mathf.Round(GapMin + random.NextSingle() * (GapMax - GapMin)),
        };
        into.Add(new StationDanger(kind, side, left, left + width, Patterns));
        return left + width;
    }

    void AddJunk(List<StationDanger> into, float left)
    {
        into.Add(new StationDanger(StationDangerKind.Junk, StationSide.Middle, left, left + Obstacle.JunkHalfSize * 2, Patterns));
        LastJunkEnd = left + Obstacle.JunkHalfSize * 2;
    }
}
