using Godot;

/// <summary>
/// VOLCANO ISLAND's things! Everything here is drawn with code.
///   LavaPlatform:  a chunky floating rock with glowing cracks. It SINKS after Bolt-E lands on it. Some have a HOLE
///                  (a cave entrance) with a sign that shows how HARD the cave is: 1, 2 or 3 flames.
///   LavaBubble:    a glowing spot in a gap between the rocks, then a blob of lava that pops up and falls back in.
///   CavePit:       a pit of lava in a LAVA CAVE's floor.
///   CaveRock:      rock spikes on a cave's floor, or a STALACTITE that shakes, then falls and sticks in the floor.
///   Geyser:        the way out of a cave: a glowing vent with EXIT arrows. WHOOSH!
///   CaveCurtain:   the screen closing and opening like an eye when Bolt-E drops in (or blasts out), and the flames
///                  under the "LAVA CAVE" banner.
///   FireBat:       a fiery bat that swoops up and down (a kind of Enemy: stomp it!).
///   VolcanoPlanner and CavePlanner (at the bottom) put everything in FAIR patterns.
/// Main.Volcano.cs and Main.LavaCave.cs put them in the world, and VolcanoScenery.cs draws the world around them.
/// </summary>
public static class VolcanoArt
{
    // Corner points reused every time (DrawColoredPolygon copies them, so no new lists are made all the time)
    static readonly Vector2[] flameOuter = new Vector2[3], flameInner = new Vector2[3];

    static readonly Color FlameOrange = new(1f, 0.45f, 0.1f);
    static readonly Color FlameYellow = new(1f, 0.88f, 0.3f);

    /// <summary>The words for a hardness: 1 = EASY, 2 = MEDIUM, 3 = HARD.</summary>
    public static string HardnessName(int hardness) => hardness switch { 1 => "EASY", 2 => "MEDIUM", _ => "HARD" };

    /// <summary>The color for a hardness: green = easy, orange = medium, red = hard (like traffic lights!).</summary>
    public static Color HardnessColor(int hardness) => hardness switch
    {
        1 => new Color(0.35f, 0.95f, 0.4f),
        2 => new Color(1f, 0.62f, 0.15f),
        _ => new Color(1f, 0.22f, 0.2f),
    };

    /// <summary>
    /// A little flame, standing on "bottom" (its middle at the bottom), "size" pixels tall: an orange teardrop with a
    /// yellow middle that flickers.
    /// </summary>
    public static void Flame(CanvasItem c, Vector2 bottom, float size, float flicker)
    {
        float r = size * 0.3f;
        var ball = bottom + new Vector2(0, -r);
        c.DrawCircle(ball, r, FlameOrange);
        flameOuter[0] = ball + new Vector2(-r * 0.95f, -r * 0.3f);
        flameOuter[1] = ball + new Vector2(r * 0.95f, -r * 0.3f);
        flameOuter[2] = bottom + new Vector2(r * 0.3f * flicker, -size);
        c.DrawColoredPolygon(flameOuter, FlameOrange);
        float small = r * 0.55f;
        c.DrawCircle(ball + new Vector2(0, r * 0.25f), small, FlameYellow);
        flameInner[0] = ball + new Vector2(-small, r * 0.1f);
        flameInner[1] = ball + new Vector2(small, r * 0.1f);
        flameInner[2] = bottom + new Vector2(-r * 0.2f * flicker, -size * 0.62f);
        c.DrawColoredPolygon(flameInner, FlameYellow);
    }

    /// <summary>Words with their middle at x, standing on "baseline" (measured first, so they're exactly in the middle).</summary>
    public static void Words(CanvasItem c, string text, float x, float baseline, int size, Color color)
    {
        var font = ThemeDB.FallbackFont;
        float width = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        c.DrawString(font, new Vector2(x - width / 2, baseline), text, HorizontalAlignment.Left, -1, size, color);
    }
}

/// <summary>
/// A floating rock on VOLCANO ISLAND's lava river. Its Position is the middle of its TOP (Main moves it down as it sinks).
/// Rocks with a HOLE (Hole = 1, 2 or 3: how hard the cave is) never sink, and neither do Steady ones (the safe rocks
/// you land on after a crash, a rocket ride or a cave, and the solid rock floor for a boss fight).
/// </summary>
public partial class LavaPlatform : Node2D
{
    // ---- How a rock looks (try changing these!) ----
    public const float HoleHalfWidth = 62;   // the hole is 124 pixels wide
    const float SignX = -HoleHalfWidth - 78; // the sign post stands just before the hole

    public float Width = 600;
    /// <summary>0 = no hole. 1, 2 or 3 = a cave entrance, and how hard the cave is (1 flame = EASY ... 3 = HARD).</summary>
    public int Hole;
    /// <summary>True for a rock that never sinks.</summary>
    public bool Steady;
    /// <summary>True for the big solid rock floor of a boss fight.</summary>
    public bool Arena;
    /// <summary>True once Bolt-E has landed on it (then it starts sinking after a moment).</summary>
    public bool Touched;
    /// <summary>Seconds since Bolt-E first landed on it.</summary>
    public float SinkClock;
    /// <summary>How far it has sunk (pixels). Main.SinkDepth or more = it went under the lava.</summary>
    public float Sunk;

    float time;

    public float Left => Position.X - Width / 2;
    public float Right => Position.X + Width / 2;
    /// <summary>Where its top is: where Bolt-E's feet go.</summary>
    public float Top => Position.Y;
    /// <summary>True when it has sunk all the way under the lava.</summary>
    public bool IsUnder => Sunk >= Main.SinkDepth;

    static readonly Color Basalt = new(0.2f, 0.12f, 0.12f);
    static readonly Color BasaltTop = new(0.4f, 0.27f, 0.24f);
    static readonly Color BasaltDark = new(0.12f, 0.07f, 0.07f);
    static readonly Color CrackGlow = new(1f, 0.45f, 0.08f);
    static readonly Color LavaTop = new(1f, 0.62f, 0.12f);
    static readonly Color LavaShine = new(1f, 0.92f, 0.45f);
    static readonly Color Wood = new(0.36f, 0.22f, 0.13f);
    static readonly Color BoardInk = new(0.16f, 0.1f, 0.07f);

    // Corner points reused every frame (so no new lists are made all the time)
    static readonly Vector2[] body = new Vector2[8];
    static readonly Vector2[] topFace = new Vector2[4];
    static readonly Vector2[] crack = new Vector2[4];
    static readonly string[] BoltsWords = { "", "BOLTS x1", "BOLTS x2", "BOLTS x3" }; // (made once, not every frame)

    public override void _Process(double delta)
    {
        time += (float)delta;
        // A sinking rock wobbles (a little more the deeper it goes)
        float wobble = Touched && !Steady && Sunk > 0 ? 0.004f + 0.01f * Mathf.Min(1, Sunk / Main.SinkDepth) : 0;
        Rotation = Mathf.Sin(time * 9) * wobble;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float half = Width / 2;
        float lava = Main.LavaSurfaceY - Position.Y; // where the lava's surface is, measured from the rock's top
        float glow = Mathf.Clamp(Sunk / Main.SinkDepth, 0, 1);

        // The rock: a chunky shape with bevelled corners, going down into the lava. (Its bottom is never higher than
        // 14 pixels under its top, even when it has sunk under the lava: then the lava covers it anyway.)
        float bottom = Mathf.Max(lava, 14);
        body[0] = new Vector2(-half, 7);
        body[1] = new Vector2(-half + 9, 0);
        body[2] = new Vector2(half - 9, 0);
        body[3] = new Vector2(half, 7);
        body[4] = new Vector2(half - 7, bottom + 6);
        body[5] = new Vector2(half - 22, bottom + 18);
        body[6] = new Vector2(-half + 22, bottom + 18);
        body[7] = new Vector2(-half + 7, bottom + 6);
        DrawColoredPolygon(body, Arena ? Basalt.Lightened(0.05f) : Basalt);

        // The lighter top (where Bolt-E rides)
        topFace[0] = new Vector2(-half + 9, 0);
        topFace[1] = new Vector2(half - 9, 0);
        topFace[2] = new Vector2(half - 4, 9);
        topFace[3] = new Vector2(-half + 4, 9);
        DrawColoredPolygon(topFace, BasaltTop);
        DrawLine(new Vector2(-half + 10, 1), new Vector2(half - 10, 1), BasaltTop.Lightened(0.25f), 2);

        // Glowing cracks: brighter while it sinks (and they pulse)
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * (4 + 8 * glow));
        var crackColor = new Color(CrackGlow, 0.35f + 0.35f * glow + 0.3f * pulse * (Touched ? 1 : 0.4f));
        int cracks = Mathf.Max(1, (int)(Width / 120));
        for (int i = 0; i < cracks; i++)
        {
            float x = -half + 45 + i * (Width - 90) / Mathf.Max(1, cracks - 1) + (i * 37 % 23) - 11;
            if (cracks == 1) x = 0;
            float screenX = Position.X + x;
            if (screenX < -60 || screenX > 1340) continue; // (the boss arena is very wide: only draw what's on the screen)
            crack[0] = new Vector2(x, 11);
            crack[1] = new Vector2(x + 9, 18 + i % 3 * 2);
            crack[2] = new Vector2(x - 5, 26);
            crack[3] = new Vector2(x + 7, lava + 4);
            DrawPolyline(crack, new Color(crackColor, crackColor.A * 0.35f), 7);
            DrawPolyline(crack, crackColor, 2.5f);
        }
        // Little dark facets on the side
        for (int i = 0; i < cracks; i++)
        {
            float x = -half + 80 + i * 120;
            if (x > half - 30 || Position.X + x < -60 || Position.X + x > 1340) continue;
            DrawLine(new Vector2(x, 14), new Vector2(x - 12, lava), BasaltDark, 3);
        }

        if (Hole > 0) DrawHole();

        // Where it goes into the lava: a band of lava over the bottom of the rock, and a bright shiny line
        DrawRect(new Rect2(-half - 8, lava, Width + 16, 20), LavaTop);
        DrawLine(new Vector2(-half - 8, lava), new Vector2(half + 8, lava), new Color(LavaShine, 0.6f + 0.4f * pulse), 3);
    }

    /// <summary>How many times its hole and sign were drawn (the self-test reads this).</summary>
    public int SignsDrawn { get; private set; }

    /// <summary>A dark round HOLE in the middle (with a rim glowing in its hardness color), and a sign post next to it.</summary>
    void DrawHole()
    {
        var rim = VolcanoArt.HardnessColor(Hole);
        float pulse = 0.6f + 0.4f * Mathf.Sin(time * 5);
        var middle = new Vector2(0, 4);
        Shapes.Ellipse(this, middle, new Vector2(HoleHalfWidth + 12, 17), new Color(rim, 0.3f * pulse)); // the glow
        Shapes.Ellipse(this, middle, new Vector2(HoleHalfWidth + 4, 12), rim);                              // the rim
        Shapes.Ellipse(this, middle, new Vector2(HoleHalfWidth, 9), new Color(0.03f, 0.01f, 0.02f));        // the hole...
        Shapes.Ellipse(this, middle + new Vector2(0, 3), new Vector2(HoleHalfWidth * 0.55f, 4), new Color(0.6f, 0.12f, 0.05f, 0.8f)); // ...glowing deep down

        // The sign: a post, and a board with the flames, the word, and how much the crystal bolts are worth
        var post = new Vector2(SignX, 0);
        DrawRect(new Rect2(post.X - 4, -96, 8, 96), Wood);
        var board = new Rect2(post.X - 58, -164, 116, 70);
        Shapes.RoundRect(this, board.Grow(3), 9, rim);
        Shapes.RoundRect(this, board, 7, BoardInk);
        float flicker = Mathf.Sin(time * 11);
        for (int i = 0; i < Hole; i++)
            VolcanoArt.Flame(this, new Vector2(post.X + (i - (Hole - 1) / 2f) * 26, -133), 22, flicker * (i % 2 == 0 ? 1 : -1));
        VolcanoArt.Words(this, VolcanoArt.HardnessName(Hole), post.X, -114, 16, Colors.White);
        VolcanoArt.Words(this, BoltsWords[Math.Clamp(Hole, 1, 3)], post.X, -99, 13, rim);
        SignsDrawn++;
    }
}

/// <summary>
/// A LAVA BUBBLE in a gap between two rocks. Position is the middle of the gap, on the lava's surface.
/// First a glowing spot and a "blub blub" (Warning), then a blob of lava pops up in an arc and falls back into the same
/// gap (Flying). Touching the blob = KABOOM. It never lands on a rock.
/// </summary>
public partial class LavaBubble : Node2D
{
    // ---- Lava bubbles (try changing these!) ----
    public const float WarnSeconds = 0.8f;    // the glowing spot shows this long before the blob pops up
    public const float LaunchSpeed = 760f;    // how fast it pops up (pixels per second)
    public const float BlobGravity = 1700f;   // how fast it falls back
    public const float Radius = 20f;          // how big the blob is
    /// <summary>How long the blob is up in the air (about 0.9 seconds).</summary>
    public static float FlightSeconds => 2 * LaunchSpeed / BlobGravity;
    /// <summary>How high it goes (about 170 pixels).</summary>
    public static float Height => LaunchSpeed * LaunchSpeed / (2 * BlobGravity);

    public enum Step { Waiting, Warning, Flying, Done }

    public float GapWidth = 150;
    /// <summary>True: it pops up just BEHIND Bolt-E (when the world goes too fast to see it in front of him).</summary>
    public bool Behind;
    /// <summary>Which way it drifts while it flies (-1 or 1).</summary>
    public float Drift = 1;
    public Step Now { get; private set; } = Step.Waiting;
    /// <summary>True for one moment when the blob pops up (Main plays the "plop").</summary>
    public bool JustLaunched;

    float stepTime, time;
    Vector2 blob; // where the blob is, from Position

    public float GapLeft => Position.X - GapWidth / 2;
    public float GapRight => Position.X + GapWidth / 2;
    /// <summary>How far the blob drifts sideways (it always stays inside its gap).</summary>
    public float DriftReach => GapWidth * 0.16f;
    /// <summary>True while the blob is up out of the lava (then it's dangerous).</summary>
    public bool Dangerous => Now == Step.Flying && blob.Y < -4;
    /// <summary>The blob's bump box (a little smaller than the blob, to be fair).</summary>
    public Rect2 Hitbox => new(Position + blob - new Vector2(Radius - 4, Radius - 4), new Vector2(Radius - 4, Radius - 4) * 2);
    /// <summary>Where the blob is in the world (the self-test reads this).</summary>
    public Vector2 BlobAt => Position + blob;

    /// <summary>The blob pops up right away, with no warning (only the self-test does this).</summary>
    public void LaunchNow()
    {
        Now = Step.Flying;
        stepTime = 0;
        JustLaunched = true;
    }

    public void StartWarning()
    {
        if (Now != Step.Waiting) return;
        Now = Step.Warning;
        stepTime = 0;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        stepTime += dt;
        if (Now == Step.Warning && stepTime >= WarnSeconds)
        {
            Now = Step.Flying;
            stepTime = 0;
            JustLaunched = true;
        }
        if (Now == Step.Flying)
        {
            float t = stepTime;
            blob = new Vector2(Drift * DriftReach * t / FlightSeconds, -LaunchSpeed * t + BlobGravity * t * t / 2);
            if (t >= FlightSeconds)
            {
                Now = Step.Done;
                stepTime = 0;
                blob = Vector2.Zero;
            }
        }
        QueueRedraw();
    }

    static readonly Color Hot = new(1f, 0.45f, 0.08f);
    static readonly Color Hotter = new(1f, 0.8f, 0.25f);

    public override void _Draw()
    {
        float y = 6; // (just under the lava's shiny top line)
        switch (Now)
        {
            case Step.Warning:
            {
                // A glowing spot that grows, and little bubbles: blub, blub!
                float f = stepTime / WarnSeconds;
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 20);
                Shapes.Ellipse(this, new Vector2(0, y), new Vector2(22 + 26 * f, 6 + 4 * f), new Color(1f, 0.95f, 0.55f, 0.45f + 0.45f * pulse));
                Shapes.Ellipse(this, new Vector2(0, y), new Vector2(12 + 12 * f, 3 + 2 * f), Colors.White);
                for (int i = 0; i < 3; i++)
                {
                    float rise = Mathf.PosMod(time * 2.5f + i * 0.33f, 1);
                    DrawCircle(new Vector2((i - 1) * 14, y - rise * 26), 3 + 3 * rise * f, new Color(Hotter, 1 - rise));
                }
                break;
            }
            case Step.Flying:
            {
                // A little trail of drops behind it, then the blob itself (with a shine)
                float t = stepTime;
                for (int i = 3; i >= 1; i--)
                {
                    float back = Mathf.Max(0, t - i * 0.045f);
                    var at = new Vector2(Drift * DriftReach * back / FlightSeconds, -LaunchSpeed * back + BlobGravity * back * back / 2);
                    DrawCircle(at, Radius * (1 - i * 0.22f), new Color(Hot, 0.5f - i * 0.1f));
                }
                DrawCircle(blob, Radius + 8, new Color(Hot, 0.3f));
                DrawCircle(blob, Radius, Hot);
                DrawCircle(blob + new Vector2(2, 2), Radius * 0.62f, Hotter);
                DrawCircle(blob + new Vector2(-6, -7), 4.5f, new Color(1, 1, 1, 0.85f));
                if (t < 0.25f) DrawArc(new Vector2(0, y), 16 + t * 160, Mathf.Pi, Mathf.Tau, 16, new Color(Hotter, 1 - t * 4), 4); // splash!
                break;
            }
            case Step.Done:
                if (stepTime < 0.3f) DrawArc(new Vector2(0, y), 14 + stepTime * 120, Mathf.Pi, Mathf.Tau, 16, new Color(Hotter, 1 - stepTime / 0.3f), 4);
                break;
        }
    }
}

/// <summary>A pit of lava in a LAVA CAVE's floor. Position is the middle of the pit, on the floor.</summary>
public partial class CavePit : Node2D
{
    public float Width = 140;
    float time;

    public float Left => Position.X - Width / 2;
    public float Right => Position.X + Width / 2;

    static readonly Color Deep = new(0.08f, 0.02f, 0.02f);
    static readonly Color PitLava = new(1f, 0.5f, 0.08f);
    static readonly Color PitShine = new(1f, 0.88f, 0.4f);
    static readonly Color RimRock = new(0.3f, 0.18f, 0.16f);

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float half = Width / 2;
        float lava = Main.LavaSurfaceY - Main.CaveFloorY;
        DrawRect(new Rect2(-half, 0, Width, 140), Deep);                                 // the hole...
        DrawRect(new Rect2(-half, lava - 16, Width, 16), new Color(PitLava, 0.25f));     // ...a hot glow...
        DrawRect(new Rect2(-half, lava, Width, 120), PitLava);                           // ...and lava at the bottom
        float wave = Mathf.Sin(time * 3) * 2;
        DrawLine(new Vector2(-half, lava + wave), new Vector2(half, lava - wave), PitShine, 3);
        // A bubble that plops now and then
        float plop = Mathf.PosMod(time * 0.8f, 1);
        if (plop < 0.7f) DrawCircle(new Vector2(-half * 0.3f, lava + 8), 2 + plop * 9, new Color(PitShine, 0.9f));
        // Rocky edges, lit up orange from below
        foreach (float side in Sides)
        {
            DrawCircle(new Vector2(side * half, 4), 10, RimRock);
            DrawLine(new Vector2(side * (half - 3), 6), new Vector2(side * (half - 3), lava), new Color(PitLava, 0.5f), 4);
        }
    }

    static readonly float[] Sides = { -1, 1 };
}

/// <summary>What a CaveRock is: rock spikes on the floor, or a stalactite hanging from the ceiling.</summary>
public enum CaveRockKind { Spikes, Stalactite }

/// <summary>
/// ROCK SPIKES on a cave's floor (jump over them!), or a STALACTITE: it hangs from the ceiling, shakes (with dust and a
/// shadow on the floor showing where it will land), falls, and sticks in the floor (then jump over it too).
/// Position is on the floor, under it.
/// </summary>
public partial class CaveRock : Node2D
{
    // ---- Stalactites (try changing these!) ----
    public const float ShakeSeconds = 0.6f;   // it shakes this long before it falls
    public const float FallGravity = 3000f;   // how fast it falls
    public const float Length = 86;           // how long it is (pixels)
    public const float SpikesHalfWidth = 30, SpikesHeight = 38;

    public enum Step { Hanging, Shaking, Falling, Landed }

    public CaveRockKind Kind;
    public Step Now { get; private set; } = Step.Hanging;
    /// <summary>True for one moment when a stalactite lands (Main plays the crack and the dust).</summary>
    public bool JustLanded;

    float time, stepTime, drop, fallSpeed;

    /// <summary>How far the ceiling is above the floor, from Position (a negative number: up).</summary>
    static float CeilingY => Main.CaveCeilingY - Main.CaveFloorY;
    /// <summary>How far a stalactite falls until its tip touches the floor.</summary>
    static float FallDistance => -CeilingY - Length;
    /// <summary>How long a falling stalactite takes to reach the floor (about half a second).</summary>
    public static float FallSeconds => Mathf.Sqrt(2 * FallDistance / FallGravity);

    /// <summary>True while it can crash Bolt-E: spikes always, a stalactite while it falls and after it lands.</summary>
    public bool Dangerous => Kind == CaveRockKind.Spikes || Now is Step.Falling or Step.Landed;

    public Rect2 Hitbox => Kind == CaveRockKind.Spikes
        ? new Rect2(Position + new Vector2(-SpikesHalfWidth, -SpikesHeight), new Vector2(SpikesHalfWidth * 2, SpikesHeight))
        : Now == Step.Landed
            ? new Rect2(Position + new Vector2(-15, -54), new Vector2(30, 54))
            : new Rect2(Position + new Vector2(-14, CeilingY + drop + 10), new Vector2(28, Length - 10));

    /// <summary>The stalactite starts to shake: CRACK! (It falls by itself after ShakeSeconds.)</summary>
    public void StartShaking()
    {
        if (Kind != CaveRockKind.Stalactite || Now != Step.Hanging) return;
        Now = Step.Shaking;
        stepTime = 0;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        stepTime += dt;
        if (Now == Step.Shaking && stepTime >= ShakeSeconds)
        {
            Now = Step.Falling;
            stepTime = 0;
        }
        if (Now == Step.Falling)
        {
            fallSpeed += FallGravity * dt;
            drop += fallSpeed * dt;
            if (drop >= FallDistance)
            {
                drop = FallDistance;
                Now = Step.Landed;
                JustLanded = true;
            }
        }
        QueueRedraw();
    }

    static readonly Color Rock = new(0.34f, 0.2f, 0.18f);
    static readonly Color RockLight = new(0.52f, 0.34f, 0.3f);
    static readonly Color Glow = new(1f, 0.45f, 0.1f);
    static readonly Vector2[] shard = new Vector2[3];
    static readonly Vector2[] stub = new Vector2[4];
    static readonly float[] SpikeX = { -20, -4, 13, 24 };
    static readonly float[] SpikeTall = { 30, 42, 34, 22 };

    public override void _Draw()
    {
        if (Kind == CaveRockKind.Spikes)
        {
            DrawSpikes();
            return;
        }

        switch (Now)
        {
            case Step.Hanging:
            case Step.Shaking:
            case Step.Falling:
            {
                float shake = Now == Step.Shaking ? Mathf.Sin(time * 70) * 3 : 0;
                float top = CeilingY + drop;
                shard[0] = new Vector2(-17 + shake, top);
                shard[1] = new Vector2(17 + shake, top);
                shard[2] = new Vector2(shake, top + Length);
                DrawColoredPolygon(shard, Rock);
                DrawLine(new Vector2(-9 + shake, top + 4), new Vector2(shake - 1, top + Length - 10), RockLight, 3);
                if (Now == Step.Shaking)
                {
                    // Dust falling from the ceiling, and a shadow on the floor where it will land
                    float f = stepTime / ShakeSeconds;
                    for (int i = 0; i < 5; i++)
                    {
                        float fall = Mathf.PosMod(time * 260 + i * 53, 220);
                        DrawCircle(new Vector2((i - 2) * 8, CeilingY + Length * 0.4f + fall), 2.5f, new Color(0.75f, 0.65f, 0.6f, 1 - fall / 220));
                    }
                }
                if (Now != Step.Hanging)
                {
                    float f = Now == Step.Falling ? 1 : stepTime / ShakeSeconds;
                    Shapes.Ellipse(this, new Vector2(0, -2), new Vector2(10 + 18 * f, 3 + 3 * f), new Color(0, 0, 0, 0.25f + 0.25f * f));
                }
                break;
            }
            case Step.Landed:
                // Stuck in the floor, tip first: its thick end sticks up (jump over it!)
                stub[0] = new Vector2(-16, -56);
                stub[1] = new Vector2(16, -56);
                stub[2] = new Vector2(6, 0);
                stub[3] = new Vector2(-6, 0);
                DrawColoredPolygon(stub, Rock);
                DrawLine(new Vector2(-8, -52), new Vector2(-3, -4), RockLight, 3);
                DrawLine(new Vector2(-16, -56), new Vector2(16, -56), RockLight, 3);
                break;
        }
    }

    /// <summary>4 sharp rock spikes with glowing orange tips.</summary>
    void DrawSpikes()
    {
        float pulse = 0.6f + 0.4f * Mathf.Sin(time * 5);
        for (int i = 0; i < SpikeX.Length; i++)
        {
            float x = SpikeX[i], tall = SpikeTall[i];
            shard[0] = new Vector2(x - 12, 0);
            shard[1] = new Vector2(x + 12, 0);
            shard[2] = new Vector2(x + 2, -tall);
            DrawColoredPolygon(shard, i % 2 == 0 ? Rock : Rock.Darkened(0.2f));
            DrawLine(new Vector2(x - 4, -3), new Vector2(x + 1, -tall + 6), RockLight, 2);
            DrawCircle(new Vector2(x + 2, -tall + 4), 3, new Color(Glow, pulse));
        }
    }
}

/// <summary>
/// The GEYSER at the end of a LAVA CAVE: a glowing vent with EXIT arrows. Ride onto it and WHOOSH! a fountain of lava
/// and steam blasts Bolt-E back up to the surface. Position is the middle of the vent, on the floor.
/// </summary>
public partial class Geyser : Node2D
{
    public const float Width = 220;
    public bool Fired { get; private set; }
    float time, firedTime;

    public float Left => Position.X - Width / 2;
    public float Right => Position.X + Width / 2;

    static readonly Color Vent = new(0.28f, 0.17f, 0.15f);
    static readonly Color Hot = new(1f, 0.55f, 0.12f);
    static readonly Color Hotter = new(1f, 0.88f, 0.4f);
    static readonly Color ExitGreen = new(0.3f, 0.9f, 0.4f);
    static readonly Vector2[] arrow = new Vector2[7];

    /// <summary>WHOOSH!</summary>
    public void Fire()
    {
        Fired = true;
        firedTime = 0;
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        if (Fired) firedTime += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 6);

        // The vent: a mound of rock with a glowing hole
        Shapes.Ellipse(this, new Vector2(0, -2), new Vector2(Width / 2, 20), Vent);
        Shapes.Ellipse(this, new Vector2(0, -8), new Vector2(46, 9), new Color(Hot, 0.7f + 0.3f * pulse));
        Shapes.Ellipse(this, new Vector2(0, -8), new Vector2(26, 5), Hotter);

        if (!Fired)
        {
            // Steam puffing up, and big glowing arrows pointing UP: this way out!
            for (int i = 0; i < 4; i++)
            {
                float rise = Mathf.PosMod(time * 0.9f + i * 0.25f, 1);
                DrawCircle(new Vector2(Mathf.Sin(time * 2 + i) * 10, -20 - rise * 120), 10 + rise * 18, new Color(1, 1, 1, 0.35f * (1 - rise)));
            }
            for (int i = 0; i < 3; i++)
            {
                float lit = Mathf.PosMod(time * 2.5f - i * 0.33f, 1) < 0.5f ? 1f : 0.4f;
                UpArrow(new Vector2(0, -70 - i * 46), new Color(ExitGreen, lit));
            }
            // The EXIT sign
            var board = new Rect2(-62, -262, 124, 46);
            Shapes.RoundRect(this, board.Grow(3), 10, Colors.White);
            Shapes.RoundRect(this, board, 8, ExitGreen.Darkened(0.35f));
            VolcanoArt.Words(this, "EXIT", 0, -227, 30, Colors.White);
            return;
        }

        // The fountain! A column of lava and steam shooting all the way up
        float f = Mathf.Min(1, firedTime / 0.25f);
        float fade = Mathf.Clamp(1.4f - firedTime, 0, 1);
        float top = -760 * f;
        DrawRect(new Rect2(-46, top, 92, -top), new Color(Hot, 0.55f * fade));
        DrawRect(new Rect2(-24, top, 48, -top), new Color(Hotter, 0.9f * fade));
        DrawRect(new Rect2(-9, top, 18, -top), new Color(1, 1, 1, fade));
        for (int i = 0; i < 10; i++)
        {
            float y = Mathf.PosMod(firedTime * 900 + i * 76, 760);
            DrawCircle(new Vector2(Mathf.Sin(i * 2.1f) * 44, -y), 9, new Color(Hot, fade));
        }
    }

    /// <summary>A big arrow pointing UP, its middle at "at".</summary>
    void UpArrow(Vector2 at, Color color)
    {
        arrow[0] = at + new Vector2(0, -20);
        arrow[1] = at + new Vector2(22, 2);
        arrow[2] = at + new Vector2(9, 2);
        arrow[3] = at + new Vector2(9, 18);
        arrow[4] = at + new Vector2(-9, 18);
        arrow[5] = at + new Vector2(-9, 2);
        arrow[6] = at + new Vector2(-22, 2);
        DrawColoredPolygon(arrow, color);
    }
}

/// <summary>
/// On top of the whole game (but under the pause screen): the screen closing like an eye (an "iris") when Bolt-E drops
/// into a hole or blasts out of a cave, and the flame pictures under the "LAVA CAVE" banner.
/// </summary>
public partial class CaveCurtain : Node2D
{
    /// <summary>0 = open (you see everything), 1 = closed (all black).</summary>
    public float Closed;
    /// <summary>Where the iris closes to (Bolt-E).</summary>
    public Vector2 Center = new(240, 540);
    /// <summary>The flames under the banner: how many, and seconds left to show them.</summary>
    public int Flames;
    public float FlamesLeft;
    float time;

    /// <summary>How many times the iris and the flames were drawn (the self-test reads these).</summary>
    public int IrisDrawn { get; private set; }
    public int FlamesDrawn { get; private set; }

    /// <summary>Everything back to normal: open, no flames.</summary>
    public void Reset()
    {
        Closed = 0;
        Flames = 0;
        FlamesLeft = 0;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        FlamesLeft = Mathf.Max(0, FlamesLeft - (float)delta);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Closed > 0.001f)
        {
            // A black ring that gets thicker and thicker until only a dot around Bolt-E is left... then black
            float hole = (1 - Mathf.Clamp(Closed, 0, 1)) * 1500;
            if (Closed >= 0.999f) DrawRect(new Rect2(-40, -40, 1360, 800), Colors.Black);
            else DrawArc(Center, hole + 1200, 0, Mathf.Tau, 72, Colors.Black, 2400);
            IrisDrawn++;
        }
        if (FlamesLeft > 0 && Flames > 0)
        {
            float alpha = Mathf.Min(1, FlamesLeft / 0.5f);
            Modulate = new Color(1, 1, 1, alpha);
            for (int i = 0; i < Flames; i++)
                VolcanoArt.Flame(this, new Vector2(640 + (i - (Flames - 1) / 2f) * 54, 292), 42, Mathf.Sin(time * 11 + i));
            FlamesDrawn++;
        }
        else
        {
            Modulate = Colors.White;
        }
    }
}

/// <summary>A FIRE BAT: it swoops up and down in a wave, flying at Bolt-E. Stomp it like an Egg-Bot!</summary>
public partial class Enemy
{
    // ---- Fire bats (try changing these!) ----
    public const float BatMiddleY = 452f;      // the middle of its swoop (the ground is at 600)
    public const float BatSwoop = 78f;         // how far up and down it swoops from the middle
    public const float BatSwoopSeconds = 1.5f; // one swoop down and up takes this long
    public const float BatExtraSpeed = 70f;    // how much faster than the ground it flies at Bolt-E

    /// <summary>Where in its swoop a bat starts (so they don't all swoop together).</summary>
    public float BatPhase;

    Rect2 BatHitbox => new(Position + new Vector2(-24, -18), new Vector2(48, 36));

    static readonly Color BatBody = new(0.36f, 0.08f, 0.06f);
    static readonly Color BatWing = new(0.55f, 0.12f, 0.06f);
    static readonly Color BatFire = new(1f, 0.5f, 0.1f);
    static readonly Color BatEyes = new(1f, 0.85f, 0.2f);
    static readonly Vector2[] batWing = new Vector2[5];
    static readonly Vector2[] batEar = new Vector2[3];

    /// <summary>Swooping up and down (or, squashed, dropping to the ground).</summary>
    void FlyLikeABat(float dt)
    {
        if (Squashed)
        {
            fallSpeed += 2000f * dt;
            Position = new Vector2(Position.X, Mathf.Min(GroundY, Position.Y + fallSpeed * dt));
            return;
        }
        Position = new Vector2(Position.X, BatMiddleY + BatSwoop * Mathf.Sin(time * Mathf.Tau / BatSwoopSeconds + BatPhase));
    }

    /// <summary>A fiery bat (looking left, at Bolt-E): a glow of fire, flapping wings, pointy ears, glowing eyes and fangs.</summary>
    void DrawFireBat()
    {
        float flap = Squashed ? 0 : Mathf.Sin(time * 18);
        DrawCircle(Vector2.Zero, 30, new Color(BatFire, Squashed ? 0 : 0.22f + 0.08f * flap));
        foreach (float side in BatSides)
        {
            batWing[0] = new Vector2(side * 8, -4);
            batWing[1] = new Vector2(side * 24, -16 - 10 * flap);
            batWing[2] = new Vector2(side * 42, -6 - 16 * flap);
            batWing[3] = new Vector2(side * 30, 6 - 4 * flap);
            batWing[4] = new Vector2(side * 16, 4);
            DrawColoredPolygon(batWing, BatWing);
        }
        foreach (float side in BatSides)
        {
            batEar[0] = new Vector2(side * 4, -10);
            batEar[1] = new Vector2(side * 12, -10);
            batEar[2] = new Vector2(side * 9, -22);
            DrawColoredPolygon(batEar, BatBody);
        }
        DrawCircle(Vector2.Zero, 14, BatBody);
        DrawCircle(new Vector2(-4, -6), 4, new Color(1, 1, 1, 0.12f));
        if (Squashed)
        {
            Shapes.XEye(this, new Vector2(-6, -2), 3, BatEyes);
            Shapes.XEye(this, new Vector2(4, -2), 3, BatEyes);
            return;
        }
        DrawCircle(new Vector2(-6, -2), 3.5f, BatEyes);
        DrawCircle(new Vector2(4, -2), 3.5f, BatEyes);
        DrawLine(new Vector2(-6, 6), new Vector2(-5, 10), Colors.White, 2); // fangs
        DrawLine(new Vector2(-1, 6), new Vector2(-2, 10), Colors.White, 2);
    }

    static readonly float[] BatSides = { -1, 1 };
}

/// <summary>
/// One rock the volcano planner made. Left and Right are "track" positions: how far along the whole ride it is (in
/// pixels), so they never change while the world scrolls. Hole = 0 for a normal rock, or 1-3 for a cave entrance.
/// Bubble = a lava bubble pops up in the gap BEFORE this rock. Speed and Seconds = what it was planned for.
/// </summary>
public readonly record struct PlannedRock(float Left, float Right, int Hole, bool Bubble, float Speed, float Seconds)
{
    public float Width => Right - Left;
}

/// <summary>
/// Puts VOLCANO ISLAND's rocks on the lava in FAIR patterns. These rules are kept by the code, not by luck (and the
/// self-test checks them on thousands of rocks):
///   1. Every gap is jumpable with ONE jump at the speed it was planned for, with lots to spare: at most GapShare of a
///      full jump (even a jump from a rock that sank as deep as it can). A double jump is never needed.
///   2. Every rock (that sinks) can be crossed in CrossShare of the time it takes to sink, so riding along and jumping at
///      the end is always in time.
///   3. The first EasySeconds are easy: wide rocks, small gaps, no lava bubbles.
///   4. Lava bubbles only pop up in gaps (never under a rock), and stay inside their gap.
///   5. Holes come on wide rocks that never sink, sometimes 2 or 3 close together (to choose!), each with its own hardness.
/// </summary>
public sealed class VolcanoPlanner
{
    // ---- Fair rocks (try changing these!) ----
    public const float AirSeconds = 0.76f;        // a full jump (from a rock that sank 40 pixels) lands after this long
    public const float GapShare = 0.5f;           // a gap is at most half a full jump...
    public const float EasyGapShare = 0.33f;      // ...and only a third in the first easy seconds
    public const float GapMin = 90, GapMax = 330; // how wide the gaps are (pixels)
    public const float EasySeconds = 20f;         // the first 20 seconds on VOLCANO ISLAND are easy
    public const float EasyWidthMin = 600, EasyWidthMax = 820; // the wide rocks at first
    public const float WidthMin = 260;            // the narrowest a rock gets
    public const float NarrowSeconds = 100f;      // rocks get narrower until this many seconds
    public const float CrossShare = 0.7f;         // a rock is crossed in at most 70% of the time it takes to sink
    public const float HoleRockWidth = 600;       // how wide a rock with a hole is
    public const float HoleGroupGap = 110;        // the little gaps between hole rocks that come together
    public const float BubbleChance = 0.35f;      // how often a gap gets a lava bubble (after the easy seconds)
    public const float BubbleGapMin = 130;        // only gaps at least this wide get one

    /// <summary>Where the last rock ends on the track.</summary>
    public float LastEnd { get; private set; }
    /// <summary>How many rocks were planned.</summary>
    public int Rocks { get; private set; }

    /// <summary>How far one full jump goes at this speed.</summary>
    public static float JumpReach(float speed) => speed * AirSeconds;

    /// <summary>The widest gap allowed at this speed.</summary>
    public static float BiggestGap(float speed, bool easy) => Mathf.Min(GapMax, (easy ? EasyGapShare : GapShare) * JumpReach(speed));

    /// <summary>The widest sinking rock allowed at this speed (so it's always crossed before it sinks).</summary>
    public static float WidestRock(float speed) => CrossShare * speed * Main.SinkSeconds;

    /// <summary>A fresh start: the next rock comes after the rock that ends at "track".</summary>
    public void Reset(float track) => LastEnd = track;

    /// <summary>
    /// Plans the next rock (or, with holeDue, a group of 1 to 3 rocks with holes) after LastEnd. "speed" is how fast the
    /// world goes (pixels per second), "seconds" how long Bolt-E has been on VOLCANO ISLAND.
    /// </summary>
    public void Plan(float speed, float seconds, bool holeDue, Random random, List<PlannedRock> into)
    {
        into.Clear();
        bool easy = seconds < EasySeconds;
        float gap = RandomGap(speed, easy, random);
        bool bubble = !easy && gap >= BubbleGapMin && random.NextSingle() < BubbleChance;

        if (holeDue)
        {
            // 1 hole (6 in 10), 2 holes (3 in 10) or 3 holes (1 in 10), each with a different hardness
            int roll = random.Next(10);
            int count = roll < 6 ? 1 : roll < 9 ? 2 : 3;
            int first = random.Next(3);
            float left = LastEnd + gap;
            for (int i = 0; i < count; i++)
            {
                int hardness = (first + i) % 3 + 1;
                Add(into, left, left + HoleRockWidth, hardness, i == 0 && bubble, speed, seconds);
                left = LastEnd + Mathf.Min(HoleGroupGap, BiggestGap(speed, easy));
            }
            return;
        }

        float width;
        if (easy)
        {
            width = EasyWidthMin + random.NextSingle() * (EasyWidthMax - EasyWidthMin);
        }
        else
        {
            // Wide at first, narrower later
            float later = Mathf.Clamp((seconds - EasySeconds) / (NarrowSeconds - EasySeconds), 0, 1);
            float most = Mathf.Lerp(640, 420, later), least = Mathf.Lerp(380, WidthMin, later);
            width = least + random.NextSingle() * (most - least);
        }
        width = Mathf.Round(Mathf.Max(WidthMin, Mathf.Min(width, WidestRock(speed))));
        Add(into, LastEnd + gap, LastEnd + gap + width, 0, bubble, speed, seconds);
    }

    float RandomGap(float speed, bool easy, Random random)
    {
        float most = BiggestGap(speed, easy);
        return Mathf.Round(GapMin + random.NextSingle() * Mathf.Max(0, most - GapMin));
    }

    void Add(List<PlannedRock> into, float left, float right, int hole, bool bubble, float speed, float seconds)
    {
        into.Add(new PlannedRock(left, right, hole, bubble, speed, seconds));
        LastEnd = right;
        Rocks++;
    }
}

/// <summary>The dangers in a LAVA CAVE.</summary>
public enum CaveDangerKind { Spikes, Pit, Stalactite, Bat }

/// <summary>One danger the cave planner put on the track (track positions, like PlannedRock), and the speed it was planned for.</summary>
public readonly record struct CaveDanger(CaveDangerKind Kind, float Left, float Right, float Speed)
{
    public float Middle => (Left + Right) / 2;
}

/// <summary>
/// Puts a LAVA CAVE's dangers on the track in FAIR patterns, by hardness (1 = EASY, 2 = MEDIUM, 3 = HARD):
///   1. Every lava pit is jumpable with ONE jump, with lots to spare (at most PitShare of a full jump).
///   2. After every danger there's room to land AND see the next one coming (ReactionSeconds, less in harder caves).
///   3. Fire bats fly at you, so there's extra room around them. EASY caves have no bats.
/// </summary>
public sealed class CavePlanner
{
    // ---- Each cave's numbers: { EASY, MEDIUM, HARD } (try changing these!) ----
    public static readonly float[] SpeedTimes = { 1f, 1.15f, 1.3f };         // how much faster the world goes in the cave
    public static readonly float[] ReactionSeconds = { 0.75f, 0.55f, 0.4f };  // room after landing to see the next danger
    public static readonly float[] ExtraRoom = { 520f, 340f, 200f };          // up to this much more room, by luck (pixels)
    public static readonly float[] StalactiteChance = { 0.15f, 0.25f, 0.35f }; // how often a danger is a falling stalactite
    public static readonly float[] BatChance = { 0f, 0.2f, 0.32f };           // how often it's a fire bat
    public static readonly float[] PitChance = { 0.3f, 0.35f, 0.4f };         // how often it's a lava pit
    public static readonly float[] PitMin = { 100f, 120f, 140f };             // how wide the pits are: at least...
    public static readonly float[] PitMax = { 150f, 185f, 220f };             // ...and at most (but always single-jumpable)
    public const float PitShare = 0.5f;          // a pit is at most half a full jump
    public const float BatRoom = 1.4f;           // bats get this much more room (they fly at you!)
    public const float SpikesWidth = CaveRock.SpikesHalfWidth * 2, StalactiteWidth = 34, BatWidth = 48;

    /// <summary>Where the last danger ends (and started) on the track, and what it was.</summary>
    public float LastEnd { get; private set; }
    public float LastStart { get; private set; }
    public CaveDangerKind LastKind { get; private set; } = CaveDangerKind.Spikes;
    public int Dangers { get; private set; }

    /// <summary>The widest pit allowed at this speed (always single-jumpable).</summary>
    public static float BiggestPit(float speed, int hardness) =>
        Mathf.Min(PitMax[hardness - 1], PitShare * VolcanoPlanner.JumpReach(speed));

    /// <summary>
    /// The least room from the START of one danger to the start of the next: a whole jump, time to see the next one,
    /// and a little more (more around bats).
    /// </summary>
    public static float LeastRoom(float speed, int hardness, bool bat) =>
        speed * (VolcanoPlanner.AirSeconds + ReactionSeconds[hardness - 1]) * (bat ? BatRoom : 1) + 60;

    /// <summary>A fresh start: the first danger can come at "track".</summary>
    public void Reset(float track)
    {
        LastEnd = LastStart = track - 100000; // (as if the last danger was long ago)
        nextFrom = track;
        Dangers = 0;
    }

    float nextFrom;

    /// <summary>Plans the next danger in a cave of this hardness, for a world going "speed" pixels per second.</summary>
    public void Plan(float speed, int hardness, Random random, List<CaveDanger> into)
    {
        into.Clear();
        int h = Math.Clamp(hardness, 1, 3);
        float roll = random.NextSingle();
        CaveDangerKind kind;
        if (roll < BatChance[h - 1]) kind = CaveDangerKind.Bat;
        else if (roll < BatChance[h - 1] + StalactiteChance[h - 1]) kind = CaveDangerKind.Stalactite;
        else if (roll < BatChance[h - 1] + StalactiteChance[h - 1] + PitChance[h - 1]) kind = CaveDangerKind.Pit;
        else kind = CaveDangerKind.Spikes;

        bool bat = kind == CaveDangerKind.Bat || LastKind == CaveDangerKind.Bat;
        float left = Mathf.Max(nextFrom, Mathf.Max(LastStart + LeastRoom(speed, h, bat), LastEnd + 80));
        left += Mathf.Round(random.NextSingle() * ExtraRoom[h - 1]);
        float width = kind switch
        {
            CaveDangerKind.Pit => Mathf.Round(Mathf.Min(PitMin[h - 1] + random.NextSingle() * (PitMax[h - 1] - PitMin[h - 1]), BiggestPit(speed, h))),
            CaveDangerKind.Stalactite => StalactiteWidth,
            CaveDangerKind.Bat => BatWidth,
            _ => SpikesWidth,
        };
        into.Add(new CaveDanger(kind, left, left + width, speed));
        LastStart = left;
        LastEnd = left + width;
        LastKind = kind;
        Dangers++;
    }
}
