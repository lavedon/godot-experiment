using Godot;

/// <summary>A bouncing fireball shot by the boss. It hops along the ground toward Bolt-E. Jump over it!</summary>
public partial class Blast : Node2D
{
    public const float Radius = 14f;
    const float Gravity = 1400f;
    const float BounceSpeed = 380f; // how high it hops (about 50 pixels)

    public float GroundY;
    public Vector2 Velocity = new(-360, 0);
    float time;

    public Rect2 Hitbox => new(Position - new Vector2(11, 11), new Vector2(22, 22));

    /// <summary>Moves the fireball. It flies left on its own, plus a bit extra as the ground scrolls by.</summary>
    public void Advance(float dt, float worldSpeed)
    {
        time += dt;
        Scale = new Vector2(Velocity.X > 0 ? -1 : 1, 1); // the fiery tail trails behind it
        Velocity.Y += Gravity * dt;
        Position += new Vector2(Velocity.X - worldSpeed * 0.35f, Velocity.Y) * dt;
        float floor = GroundY - Radius;
        if (Position.Y > floor)
        {
            Position = new Vector2(Position.X, floor);
            Velocity.Y = -BounceSpeed; // boing!
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float flicker = 0.85f + 0.15f * Mathf.Sin(time * 40f);
        DrawCircle(Vector2.Zero, Radius + 9, new Color(1f, 0.5f, 0.1f, 0.25f));             // glow
        DrawCircle(new Vector2(11, -1), Radius * 0.75f, new Color(1f, 0.55f, 0.1f, 0.45f));  // fiery tail
        DrawCircle(new Vector2(21, -3), Radius * 0.45f, new Color(1f, 0.6f, 0.1f, 0.3f));
        DrawCircle(Vector2.Zero, Radius * flicker, new Color(1f, 0.5f, 0.1f));               // fire
        DrawCircle(new Vector2(-2, -2), Radius * 0.6f, new Color(1f, 0.88f, 0.35f));         // hot middle
        DrawCircle(new Vector2(-4, -4), Radius * 0.25f, Colors.White);
    }
}
