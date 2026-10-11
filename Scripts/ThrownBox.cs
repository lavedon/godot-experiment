using Godot;

/// <summary>
/// A cardboard box that OPTI-MAX throws! It flies in a high arc toward where Bolt-E was (a shadow on the ground grows
/// where it will land), lands with a crunch, bounces ONCE more the same way, then breaks into a puff of cardboard bits.
/// Touching it while it flies or bounces is a crash (jump over it, or ride out from under it!).
/// Position is the bottom-middle of the box.
/// </summary>
public partial class ThrownBox : Node2D
{
    // ---- Box numbers (try changing these!) ----
    public const float Gravity = 1500f;      // how fast it falls back down (Bolt-E falls with 2600)
    public const float BounceUp = 360f;      // how high it hops when it lands (about 43 pixels)
    public const float BounceSlowDown = 0.5f;// it keeps this much of its sideways speed when it bounces
    public const float Width = 44f, Height = 36f;
    const float ArenaLeft = 20f, ArenaRight = 1260f; // it breaks if it hits the side of the screen

    public float GroundY;
    public float LandX;          // where it's going to land (its shadow is there)
    public Vector2 Velocity;

    /// <summary>It landed the first time (crunch!), here.</summary>
    public event Action<Vector2>? Landed;
    /// <summary>It broke into bits, here.</summary>
    public event Action<Vector2>? Broke;

    int landings;                // 0 = still flying, 1 = bouncing, 2 = broken
    float spin, time;

    /// <summary>True while it flies through the air (before it first lands).</summary>
    public bool Flying => landings == 0;
    /// <summary>True while it hops after landing once.</summary>
    public bool Bouncing => landings == 1;
    /// <summary>True when it's flying or bouncing: touching it is a crash.</summary>
    public bool Dangerous => landings < 2;
    /// <summary>True once it broke (then it can be taken away).</summary>
    public bool Finished => landings >= 2;
    /// <summary>True if its shadow was drawn on the ground in the last frame (the self-test reads this).</summary>
    public bool ShadowDrawn { get; private set; }

    /// <summary>A bit smaller than the drawing, to be fair.</summary>
    public Rect2 Hitbox => new(Position + new Vector2(-Width / 2 + 4, -Height + 3), new Vector2(Width - 8, Height - 3));

    /// <summary>
    /// Throws the box from "from" so it lands at landX after "seconds". (The arc is worked out so it lands right there.)
    /// </summary>
    public void Throw(Vector2 from, float landX, float seconds)
    {
        Position = from;
        LandX = landX;
        float upOrDown = GroundY - from.Y; // how far it falls in total
        Velocity = new Vector2((landX - from.X) / seconds, (upOrDown - 0.5f * Gravity * seconds * seconds) / seconds);
        spin = Mathf.Sign(landX - from.X) * 5f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        if (Finished) return;

        Velocity.Y += Gravity * dt;
        Position += Velocity * dt;

        if (Position.Y >= GroundY && Velocity.Y > 0)
        {
            Position = new Vector2(Position.X, GroundY);
            landings++;
            if (landings == 1)
            {
                // CRUNCH! It lands and hops once more the same way
                Velocity = new Vector2(Velocity.X * BounceSlowDown, -BounceUp);
                spin *= 0.5f;
                Landed?.Invoke(Position);
            }
            else
            {
                Broke?.Invoke(Position); // ...and breaks into bits
            }
        }
        else if (Position.X < ArenaLeft || Position.X > ArenaRight)
        {
            landings = 2; // it hit the side of the screen: break!
            Broke?.Invoke(Position);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        ShadowDrawn = false;
        if (Finished) return;

        // Its shadow on the ground where it will land: it grows and gets darker as the box comes down
        float height = GroundY - Position.Y;
        float shadowX = Flying ? LandX - Position.X : 0;
        float near = Mathf.Clamp(1f - height / 450f, 0.15f, 1f);
        Shapes.Ellipse(this, new Vector2(shadowX, height), new Vector2(8 + 22 * near, 3 + 4 * near), new Color(0, 0, 0, 0.12f + 0.3f * near));
        ShadowDrawn = true;

        // The box, turning as it flies
        float angle = Flying ? time * spin : Mathf.Sin(time * 20) * 0.15f;
        DrawSetTransform(new Vector2(0, -Height / 2), angle, Vector2.One);
        DrawBox(this, Vector2.Zero);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    // ---- Cardboard colors (try changing these!) ----
    public static readonly Color Cardboard = new(0.78f, 0.58f, 0.34f);
    static readonly Color CardboardDark = new(0.56f, 0.39f, 0.21f);
    static readonly Color Tape = new(0.93f, 0.84f, 0.6f);

    /// <summary>Draws a cardboard box (with a flap line, tape and a hand hole) with its middle at "middle".</summary>
    public static void DrawBox(CanvasItem canvas, Vector2 middle)
    {
        var rect = new Rect2(middle - new Vector2(Width / 2, Height / 2), new Vector2(Width, Height));
        canvas.DrawRect(rect, Cardboard);
        canvas.DrawRect(new Rect2(rect.Position, new Vector2(Width, 7)), Cardboard.Lightened(0.12f)); // the top flaps
        canvas.DrawLine(rect.Position + new Vector2(0, 7), rect.Position + new Vector2(Width, 7), CardboardDark, 1.5f);
        canvas.DrawRect(new Rect2(middle.X - 5, rect.Position.Y, 10, 17), Tape);                     // the tape
        Shapes.RoundRect(canvas, new Rect2(middle.X - 8, middle.Y + 4, 16, 5), 2.5f, CardboardDark); // the hand hole
        canvas.DrawRect(rect, CardboardDark, false, 2f);
    }
}
