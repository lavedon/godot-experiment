using Godot;

/// <summary>
/// A glowing teal shockwave from OPTI-MAX's SUPER STOMP! It runs along the ground, away from him, until it leaves the
/// screen. It's low (about 40 pixels high), so Bolt-E can jump over it. Touching it on the ground is a crash.
/// (Big Rusty's ground pound makes the orange Shockwave.cs ones.) Position is the bottom-middle of the wave.
/// </summary>
public partial class StompWave : Node2D
{
    // ---- Wave numbers (try changing these!) ----
    public const float Speed = 420f;   // how fast it runs along the ground (Bolt-E rides 430)
    public const float Height = 40f;   // how high it is
    const float HalfWidth = 13f;       // its bump box is 26 pixels wide

    public float Direction = 1;        // -1 = runs left, 1 = runs right
    float time;

    /// <summary>True once it ran off the screen (then it can be taken away).</summary>
    public bool Finished => Position.X < -80 || Position.X > 1360;
    public Rect2 Hitbox => new(Position + new Vector2(-HalfWidth, -Height + 4), new Vector2(HalfWidth * 2, Height - 4));

    // ---- Its shape (made once) and colors ----
    static readonly Color Teal = new(0.25f, 0.95f, 0.88f);
    static readonly Color DeepTeal = new(0.05f, 0.6f, 0.62f);
    static readonly Vector2[] Wave = { new(-26, 0), new(-14, -22), new(0, -40), new(11, -30), new(20, 0) };
    static readonly Vector2[] Inside = { new(-14, 0), new(-5, -16), new(4, -27), new(11, -18), new(13, 0) };

    public override void _Ready() => Scale = new Vector2(Direction, 1); // (it's drawn running right: Scale turns it around)

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        Position += new Vector2(Direction * Speed * dt, 0);
        QueueRedraw();
    }

    public override void _Draw()
    {
        // A soft glow behind, the wave (it shimmers), and little ripples trailing behind it
        float shimmer = 0.75f + 0.25f * Mathf.Sin(time * 30);
        DrawCircle(new Vector2(0, -14), 26, new Color(Teal, 0.18f * shimmer));
        DrawColoredPolygon(Wave, new Color(DeepTeal, 0.9f));
        DrawColoredPolygon(Inside, new Color(Teal, shimmer));
        DrawLine(new Vector2(-6, -24), new Vector2(3, -33), new Color(1, 1, 1, 0.7f), 2.5f, true);
        for (int i = 1; i <= 3; i++)
        {
            float x = -24 - i * 14;
            DrawArc(new Vector2(x, 2), 9 - i, Mathf.Pi, Mathf.Tau, 8, new Color(Teal, 0.55f - i * 0.14f), 3, true);
        }
    }
}
