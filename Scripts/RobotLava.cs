using Godot;

/// <summary>
/// VOLCANO ISLAND: Bolt-E on floating rocks, in hot lava and in the LAVA CAVES! (This is more of Robot: "partial" means
/// one class spread over several files.)
///
/// On VOLCANO ISLAND the "ground" under Bolt-E is a rock floating on the lava, and it SINKS when he stands on it, so
/// Main tells him where his rock's top is (RideOn) and he goes down with it. If he ends up in the lava, he does a
/// "HOT HOT HOT!" hop first (HotHop), then KABOOM. Dropping into a cave and getting blasted out by a geyser use DropInFrom
/// and LaunchUp. (Main.Volcano.cs and Main.LavaCave.cs decide when.)
/// </summary>
public partial class Robot
{
    // ---- Lava! (try changing these!) ----
    public const float HotHopSpeed = 700f;     // how high the "HOT HOT HOT!" hop goes (pixels per second upward)
    const float RideOnSpeed = 400f;            // how fast his feet follow a sinking rock (pixels per second)

    /// <summary>True while his feet are in the lava (the "HOT HOT HOT!" hop). He can't jump out of it!</summary>
    public bool HotFeet { get; private set; }

    /// <summary>
    /// Bolt-E rides on a rock whose top is at "top" (it sinks!). While he's on it, his feet go up and down with it
    /// (smoothly); while he's in the air, it's where he'll land.
    /// </summary>
    public void RideOn(float top, float dt)
    {
        GroundY = top;
        if (OnGround && !Crashed && !Rocketing && !FallingIntoSpace)
            Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, top, RideOnSpeed * dt));
    }

    /// <summary>
    /// OUCH, LAVA! He hops up ("HOT HOT HOT!") and comes back down INTO the lava at lavaY (then Main does the KABOOM).
    /// No jumping out of it: that's how lava works.
    /// </summary>
    public void HotHop(float lavaY)
    {
        if (Crashed) return;
        HotFeet = true;
        FallingIntoSpace = false;
        GroundY = lavaY;
        OnGround = false;
        velocityY = -HotHopSpeed;
        jumpsLeft = 0;
        squash = 1.35f;
    }

    /// <summary>Not in the lava any more (a crash, a new game, the title screen).</summary>
    public void CoolFeet() => HotFeet = false;

    /// <summary>
    /// He drops in from high up at y (into a LAVA CAVE, or back onto the surface after the geyser), and falls down to
    /// GroundY. No jumping until he lands (so he can't fly off while he drops in).
    /// </summary>
    public void DropInFrom(float y)
    {
        FallingIntoSpace = false;
        HotFeet = false;
        Position = new Vector2(Position.X, y);
        OnGround = false;
        velocityY = 120;
        jumpsLeft = 0;
        squash = 1.2f;
    }

    /// <summary>WHOOSH! The geyser blasts him straight up, spinning, faster than any jump.</summary>
    public void LaunchUp(float speedUp)
    {
        if (Crashed) return;
        FallingIntoSpace = false;
        OnGround = false;
        velocityY = -speedUp;
        jumpsLeft = 0;
        spin = Mathf.Tau * 2;
        squash = 1.4f;
    }
}
