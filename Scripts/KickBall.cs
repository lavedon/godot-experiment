using Godot;

/// <summary>
/// OPTI-MAX's soccer ball! He drops it and KICKS it: it bounces along the ground toward Bolt-E in big bouncy hops
/// (boing! boing!), bounces off the far wall once, and pops when it lands after that.
/// Touching the ball is a crash: jump over it, or ride under it while it's high up in a hop!
/// Position is the middle of the ball.
/// </summary>
public partial class KickBall : Node2D
{
    // ---- Ball numbers (try changing these!) ----
    public const float Radius = 17f;
    public const float KickSpeed = 380f;   // how fast it rolls sideways (Bolt-E rides 430)
    public const float FirstHop = 520f;    // how hard the kick lifts it
    public const float HopSpeed = 700f;    // how high every bounce goes (about 160 pixels: ride under it!)
    public const float Gravity = 1500f;
    const float WallLeft = 25f, WallRight = 1255f; // the walls at the sides of the arena
    const float LongestLife = 9f;          // (just in case) it always pops after this many seconds

    public float GroundY;
    public Vector2 Velocity;

    /// <summary>Boing! It bounced on the ground, here.</summary>
    public event Action<Vector2>? Bounced;
    /// <summary>Bonk! It bounced off a wall, here.</summary>
    public event Action<Vector2>? HitWall;
    /// <summary>POP! It's gone, here.</summary>
    public event Action<Vector2>? Popped;

    bool hitWall, popped;
    float time, spin;

    /// <summary>True while it bounces around (touching it is a crash).</summary>
    public bool Dangerous => !popped;
    /// <summary>True once it popped (then it can be taken away).</summary>
    public bool Finished => popped;
    /// <summary>True once it bounced off a wall (the self-test reads this).</summary>
    public bool BouncedOffWall => hitWall;

    /// <summary>A bit smaller than the drawing, to be fair.</summary>
    public Rect2 Hitbox => new(Position - new Vector2(Radius - 3, Radius - 3), new Vector2(Radius * 2 - 6, Radius * 2 - 6));

    /// <summary>KICK! Off it goes, this way (-1 = left, 1 = right).</summary>
    public void Kick(float direction)
    {
        Velocity = new Vector2(direction * KickSpeed, -FirstHop);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (popped) return;
        time += dt;
        Velocity.Y += Gravity * dt;
        Position += Velocity * dt;
        spin += Velocity.X / Radius * dt; // it rolls as it goes

        // Off the wall (only once)
        bool atWall = (Position.X < WallLeft + Radius && Velocity.X < 0) || (Position.X > WallRight - Radius && Velocity.X > 0);
        if (atWall && !hitWall)
        {
            hitWall = true;
            Velocity = new Vector2(-Velocity.X, Velocity.Y);
            HitWall?.Invoke(Position);
        }

        // On the ground: boing! (or POP, once it has been off the wall, or if it somehow got stuck)
        if (Position.Y >= GroundY - Radius && Velocity.Y > 0)
        {
            Position = new Vector2(Position.X, GroundY - Radius);
            if (hitWall || time > LongestLife)
            {
                Pop();
                return;
            }
            Velocity = new Vector2(Velocity.X, -HopSpeed);
            Bounced?.Invoke(Position);
        }
        else if (time > LongestLife || Position.X < -60 || Position.X > 1340)
        {
            Pop();
            return;
        }
        QueueRedraw();
    }

    void Pop()
    {
        popped = true;
        Popped?.Invoke(Position);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (popped) return;
        // Its shadow on the ground (smaller when it's high up)
        float height = GroundY - Position.Y;
        float near = Mathf.Clamp(1f - height / 300f, 0.3f, 1f);
        Shapes.Ellipse(this, new Vector2(0, height), new Vector2(18 * near, 4 * near), new Color(0, 0, 0, 0.25f * near));
        DrawBall(this, Vector2.Zero, spin);
    }

    // ---- The ball's look (made once) ----
    static readonly Color BallWhite = new(0.97f, 0.97f, 0.97f);
    static readonly Color BallBlack = new(0.1f, 0.1f, 0.12f);
    static readonly Vector2[] Pentagon = MakePentagon(6.5f);
    static readonly Vector2[] turned = new Vector2[5];

    static Vector2[] MakePentagon(float size)
    {
        var points = new Vector2[5];
        for (int i = 0; i < 5; i++) points[i] = Vector2.Up.Rotated(i * Mathf.Tau / 5) * size;
        return points;
    }

    /// <summary>Draws a black-and-white soccer ball with its middle at "middle", turned by "angle".</summary>
    public static void DrawBall(CanvasItem canvas, Vector2 middle, float angle)
    {
        canvas.DrawCircle(middle, Radius, BallWhite);
        for (int i = 0; i < 5; i++) turned[i] = middle + Pentagon[i].Rotated(angle);
        canvas.DrawColoredPolygon(turned, BallBlack);
        for (int i = 0; i < 5; i++)
        {
            // The lines out to the edge, and a bit of the next black patch at the edge
            var out1 = Vector2.Up.Rotated(angle + i * Mathf.Tau / 5);
            canvas.DrawLine(middle + out1 * 6.5f, middle + out1 * 13f, BallBlack, 1.5f, true);
            canvas.DrawCircle(middle + out1 * 14f, 3f, BallBlack);
        }
        canvas.DrawArc(middle, Radius, 0, Mathf.Tau, 24, BallBlack, 1.5f, true);
        canvas.DrawCircle(middle + new Vector2(-6, -7), 3, new Color(1, 1, 1, 0.8f)); // a shine
    }
}
