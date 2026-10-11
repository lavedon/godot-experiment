using Godot;

/// <summary>
/// UPSIDE-DOWN STATION: Bolt-E FLIPS GRAVITY! (This is more of Robot: "partial" means one class spread over several files.)
///
/// In the station the jump button doesn't jump. It flips gravity: Bolt-E falls UP to the ceiling and rides along it
/// upside-down, and the next press flips him back down. He can flip once more in the air (like the double jump), so
/// a flip that was too early can be fixed. Touching the floor or the ceiling gives him his flips back.
///
/// How it works: Position is always his FEET, on the side gravity pulls him to. The right way up his body is above
/// his feet; upside-down it hangs below them (on the ceiling). When he flips, his feet swap to the other end of his
/// body (so his body stays exactly where it was), and his drawing does a quick half-roll so you SEE him turn over.
/// </summary>
public partial class Robot
{
    // ---- Flipping gravity (try changing these!) ----
    public const float FlipGravity = 3400f;      // how hard the flipped gravity pulls (bigger = a faster trip across)
    public const float FlipKick = 200f;          // how fast he pushes off when he flips (pixels per second)
    public const int MaxFlips = 2;               // 2 = one flip from the floor or ceiling, plus one more in the air
    public const float FlipRollSeconds = 0.18f;  // how long the half-roll takes (the drawing turning over)

    /// <summary>"No ceiling here": CeilingY is set to this everywhere except UPSIDE-DOWN STATION.</summary>
    public const float NoCeiling = -100000f;

    /// <summary>
    /// How far his feet jump when he turns over: from the bottom of his body to the top (he's 130 pixels tall, from his
    /// feet to the top of his head). His body itself doesn't move.
    /// </summary>
    const float FeetSwap = 130f;

    /// <summary>True in UPSIDE-DOWN STATION (with the ceiling closed): the jump button flips gravity.</summary>
    public bool FlipMode { get; private set; }

    /// <summary>True while gravity points UP: he rides (or falls toward) the ceiling, upside-down.</summary>
    public bool UpsideDown { get; private set; }

    /// <summary>
    /// Where the ceiling's surface is (his feet touch it when he rides upside-down). In the other worlds it's NoCeiling.
    /// Even when he isn't flipping, his head can't go through it.
    /// </summary>
    public float CeilingY = NoCeiling;

    /// <summary>True while he's falling out through a gap in the floor or the ceiling (whoops!).</summary>
    public bool FallingIntoSpace { get; private set; }

    /// <summary>How far he has turned over: 0 = the right way up, Pi = upside-down (in between during the half-roll).</summary>
    public float RollAngle { get; private set; }

    /// <summary>True if the last flip went UP (toward the ceiling).</summary>
    public bool LastFlipWentUp { get; private set; }

    /// <summary>True if the last flip was done in the air (the "second chance" flip).</summary>
    public bool LastFlipInAir { get; private set; }

    /// <summary>Flips he can still do before touching the floor or the ceiling again.</summary>
    public int FlipsLeft => flipsLeft;
    int flipsLeft = MaxFlips;

    /// <summary>Which way gravity pulls him: 1 = down, -1 = up.</summary>
    public float GravityDirection => UpsideDown ? -1 : 1;

    /// <summary>The floor or ceiling his feet are on (or falling toward).</summary>
    public float SurfaceY => UpsideDown ? CeilingY : GroundY;

    /// <summary>The middle of his body (60 pixels from his feet, toward his head).</summary>
    public Vector2 BodyMiddle => Position + new Vector2(0, -60 * Size * GravityDirection);

    /// <summary>A spot "distance" pixels past his feet, toward his head: over his head, or under it when upside-down.</summary>
    public Vector2 OverHead(float distance) => Position + new Vector2(0, -distance * Size * GravityDirection);

    /// <summary>
    /// Turns flipping on (UPSIDE-DOWN STATION with its ceiling closed) or off. Turning it off when he's upside-down
    /// turns gravity back to normal right away: he turns over and falls down to the floor.
    /// </summary>
    public void SetFlipMode(bool on)
    {
        if (on == FlipMode) return;
        FlipMode = on;
        flipsLeft = MaxFlips;
        if (!on && UpsideDown && !Crashed)
        {
            TurnOver();                     // the right way up again...
            velocityY = FlipKick;           // ...falling down to the floor
            OnGround = false;
            jumpsLeft = MaxJumps - 1;       // (he can still jump once on the way down)
        }
    }

    /// <summary>
    /// FLIP! Gravity turns around and he falls toward the other side. Returns true if he flipped. (Not on the rocket
    /// board or the parachute, not in pieces, not while falling through a gap, and not with no flips left.)
    /// </summary>
    public bool SwapGravity()
    {
        if (!FlipMode || Crashed || Rocketing || Parachuting || FallingIntoSpace || flipsLeft <= 0) return false;
        LastFlipInAir = !OnGround;
        flipsLeft = OnGround ? MaxFlips - 1 : 0;
        TurnOver();
        LastFlipWentUp = UpsideDown;
        velocityY = FlipKick * GravityDirection; // a push toward the other side
        OnGround = false;
        squash = 1.25f;
        return true;
    }

    /// <summary>Gravity turns around: his feet swap to the other end of his body (the body stays where it is).</summary>
    void TurnOver()
    {
        Position += new Vector2(0, FeetSwap * Size * GravityDirection * -1);
        UpsideDown = !UpsideDown;
        // (the half-roll happens by itself: RollAngle moves toward Pi or 0, see UpdateRoll)
    }

    /// <summary>Back to normal gravity, the right way up, right away (a crash, a new game, the title screen).</summary>
    void ResetGravityFlip()
    {
        UpsideDown = false;
        RollAngle = 0;
        flipsLeft = MaxFlips;
        FallingIntoSpace = false;
    }

    /// <summary>The half-roll: his drawing turns over smoothly (the right way up = 0, upside-down = Pi).</summary>
    void UpdateRoll(float dt) =>
        RollAngle = Mathf.MoveToward(RollAngle, UpsideDown ? Mathf.Pi : 0, Mathf.Pi / FlipRollSeconds * dt);

    /// <summary>
    /// Falling while flipping is on: pulled toward the floor or the ceiling (FlipGravity), and landing on it.
    /// (The parachute after the rocket board floats down slowly, like everywhere else.)
    /// </summary>
    void FlipFall(float dt)
    {
        float pull = Parachuting ? Gravity * GravityScale * 0.12f : FlipGravity;
        velocityY += pull * GravityDirection * dt;
        if (Parachuting) velocityY = Mathf.Min(velocityY, PowerUps.ParachuteFallSpeed);
        Position += new Vector2(0, velocityY * dt);

        bool landed = UpsideDown ? Position.Y <= CeilingY : Position.Y >= GroundY;
        if (!landed) return;
        Position = new Vector2(Position.X, SurfaceY);
        squash = 0.7f; // squish on landing
        OnGround = true;
        velocityY = 0;
        jumpsLeft = MaxJumps;
        flipsLeft = MaxFlips;
        Parachuting = false;
    }

    /// <summary>Not flipping, but there's a ceiling (it's sliding open or shut): his head bonks on it instead of going through.</summary>
    void BonkHeadOnCeiling()
    {
        float headTop = Position.Y - 130 * Size;
        if (CeilingY <= NoCeiling + 1 || headTop >= CeilingY) return;
        Position = new Vector2(Position.X, CeilingY + 130 * Size);
        velocityY = Mathf.Max(velocityY, 0);
    }

    // ---------- Falling out through a gap ----------

    /// <summary>
    /// WHOOPS! He rode onto a gap with gravity pulling him into it: he falls out of the station, spinning.
    /// (Main waits a moment, then the KABOOM: see Main.Station.cs.)
    /// </summary>
    public void FallIntoSpace()
    {
        if (Crashed || FallingIntoSpace) return;
        FallingIntoSpace = true;
        OnGround = false;
        velocityY = 250 * GravityDirection;
        spin = Mathf.Tau * 2; // tumbling head over heels
        squash = 1.2f;
    }

    /// <summary>Falling through the gap: gravity keeps pulling, and there's nothing to land on.</summary>
    void UpdateSpaceFall(float dt)
    {
        velocityY += FlipGravity * 0.5f * GravityDirection * dt;
        Position += new Vector2(0, velocityY * dt);
    }

    /// <summary>The fall is over: he's put back at the edge of the gap (where the KABOOM happens).</summary>
    public void StopFallingAt(float surfaceY)
    {
        FallingIntoSpace = false;
        Position = new Vector2(Position.X, surfaceY);
        velocityY = 0;
    }

    // ---------- Drawing him upside-down ----------

    /// <summary>
    /// The half-roll as a drawing transform: it squashes his drawing flat and opens it out the other way up, turning
    /// around the middle of his body. (cos 0 = 1: the right way up. cos Pi = -1: upside-down.)
    /// </summary>
    Transform2D RollTransform()
    {
        float s = Mathf.Cos(RollAngle);
        float middle = FeetSwap / 2;                     // the middle of his body, above his feet
        float shift = UpsideDown ? middle : -middle;     // where that middle is now, from his feet
        return new Transform2D(new Vector2(1, 0), new Vector2(0, s), new Vector2(0, shift + s * middle));
    }

    /// <summary>
    /// Like DrawSetTransform, but with the half-roll on top. (The right way up, it's exactly DrawSetTransform, so the other
    /// worlds draw him exactly like before.)
    /// </summary>
    void DrawSetBodyTransform(Vector2 offset, float angle, Vector2 scale)
    {
        if (RollAngle == 0 && !UpsideDown)
            DrawSetTransform(offset, angle, scale);
        else
            DrawSetTransformMatrix(RollTransform() * new Transform2D(angle, scale, 0, offset));
    }

    /// <summary>A spot on his body (like his headphones) turned the way he is: upside-down, it's below his feet.</summary>
    Vector2 TurnedSpot(Vector2 spot) => UpsideDown ? new Vector2(spot.X, -spot.Y) : spot;
}
