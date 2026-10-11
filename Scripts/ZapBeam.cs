using Godot;

/// <summary>
/// DRONEZILLA's ZAP BEAM! First a red target circle on the ground follows Bolt-E (it moves along under DRONEZILLA),
/// then it LOCKS: it stops and glows brighter (ride out of it!). Then a big zap beam fires straight down on that spot.
/// Being inside the beam is a crash. (It looks like Big Rusty's laser, but it goes down instead of sideways.)
/// Position is the middle of the circle, on the ground. Main.Dronezilla.cs moves it along while it follows.
/// </summary>
public partial class ZapBeam : Node2D
{
    public const float HalfWidth = 32f;    // the beam is 64 pixels wide where it can hurt Bolt-E...
    public const float RingRadius = 48f;   // ...and the circle on the ground is a bit wider (fair play!)

    public float GroundY;
    /// <summary>Where the beam starts: DRONEZILLA's camera eye.</summary>
    public float TopY;
    public event Action? Locked;          // the circle stops: get out!
    public event Action? StartedFiring;   // ZAP!

    float time;
    static float TrackSeconds => Dronezilla.ZapTrackSeconds;
    static float LockSeconds => Dronezilla.ZapLockSeconds;
    static float FireSeconds => Dronezilla.ZapFireSeconds;

    /// <summary>True while the circle follows Bolt-E.</summary>
    public bool Tracking => time < TrackSeconds;
    /// <summary>True while the circle is LOCKED (it stopped, and the beam is about to fire).</summary>
    public bool IsLocked => !Tracking && time < TrackSeconds + LockSeconds;
    /// <summary>True while the beam fires: being inside it is a crash.</summary>
    public bool Firing => time >= TrackSeconds + LockSeconds && !Finished;
    public bool Finished => time >= TrackSeconds + LockSeconds + FireSeconds;
    /// <summary>True if the locked (brighter) circle was drawn in the last frame (the self-test reads this).</summary>
    public bool LockedDrawn { get; private set; }

    public Rect2 Hitbox => new(new Vector2(Position.X - HalfWidth, TopY), new Vector2(HalfWidth * 2, GroundY - TopY));

    /// <summary>Main calls this every frame: the circle moves along under DRONEZILLA (only while it follows Bolt-E).</summary>
    public void Follow(float x, float topY)
    {
        if (Tracking) Position = new Vector2(x, GroundY);
        TopY = topY;
    }

    public override void _Process(double delta)
    {
        bool wasLocked = IsLocked, wasFiring = Firing;
        time += (float)delta;
        if (IsLocked && !wasLocked) Locked?.Invoke();
        if (Firing && !wasFiring) StartedFiring?.Invoke();
        QueueRedraw();
    }

    public override void _Draw()
    {
        LockedDrawn = false;
        if (Finished) return;
        if (!Firing)
        {
            // The red target circle: dim while it follows Bolt-E, bright and blinking fast once it's LOCKED
            bool locked = IsLocked;
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * (locked ? 40 : 10));
            var color = locked ? new Color(1f, 0.15f, 0.1f, 0.75f + 0.25f * pulse) : new Color(1f, 0.3f, 0.25f, 0.4f + 0.2f * pulse);
            DrawSetTransform(Vector2.Zero, 0, new Vector2(1, 0.28f));
            DrawCircle(Vector2.Zero, RingRadius, new Color(1f, 0.2f, 0.1f, locked ? 0.3f : 0.1f));
            DrawArc(Vector2.Zero, RingRadius, 0, Mathf.Tau, 36, color, locked ? 8 : 4, true);
            DrawArc(Vector2.Zero, RingRadius * 0.4f, 0, Mathf.Tau, 20, color, locked ? 6 : 3, true);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            // Little crosshair marks on both sides
            DrawLine(new Vector2(-RingRadius - 14, 0), new Vector2(-RingRadius + 10, 0), color, 3, true);
            DrawLine(new Vector2(RingRadius - 10, 0), new Vector2(RingRadius + 14, 0), color, 3, true);
            if (locked)
            {
                // A blinking dashed line from DRONEZILLA's camera down to the circle: here it comes!
                if ((int)(time * 12) % 2 == 0)
                    for (float y = TopY - GroundY; y < -10; y += 28)
                        DrawLine(new Vector2(0, y), new Vector2(0, Mathf.Min(-10, y + 14)), new Color(1f, 0.2f, 0.2f, 0.8f), 3);
                LockedDrawn = true;
            }
            return;
        }

        // ZAP! A big buzzing beam straight down, with a splash of light on the ground
        float wobble = 1f + 0.15f * Mathf.Sin(time * 60);
        var top = new Vector2(0, TopY - GroundY);
        DrawLine(top, Vector2.Zero, new Color(1f, 0.2f, 0.2f, 0.3f), 80 * wobble);   // glow
        DrawLine(top, Vector2.Zero, new Color(1f, 0.25f, 0.2f), 44 * wobble);        // beam
        DrawLine(top, Vector2.Zero, new Color(1f, 0.9f, 0.9f), 14 * wobble);         // hot white middle
        Shapes.Ellipse(this, Vector2.Zero, new Vector2(70 * wobble, 14), new Color(1f, 0.5f, 0.4f, 0.6f));
        Shapes.Ellipse(this, Vector2.Zero, new Vector2(40 * wobble, 8), new Color(1f, 0.95f, 0.9f, 0.9f));
        DrawCircle(top, 14 * wobble, new Color(1f, 0.85f, 0.8f));
    }
}
