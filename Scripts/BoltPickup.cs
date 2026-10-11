using Godot;

/// <summary>
/// A shiny golden bolt (a hexagon nut). Robots love these! Each one is worth bonus points.
/// </summary>
public partial class BoltPickup : Node2D
{
    public const float Radius = 13f;
    float time = (float)GD.RandRange(0.0, 10.0);

    /// <summary>
    /// Most bolts just float. A bolt that VROOMBA spits back out flies with this speed (pixels per second), falls,
    /// and stops when it gets down to LandY.
    /// </summary>
    public Vector2 Velocity;
    public float LandY;
    const float FlyingGravity = 1600f;

    /// <summary>Does this bolt touch the box? (We're generous: a little extra reach.)</summary>
    public bool Touches(Rect2 box)
    {
        var closest = Position.Clamp(box.Position, box.End);
        return closest.DistanceTo(Position) < Radius + 8f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        if (Velocity != Vector2.Zero)
        {
            // Flying out of VROOMBA like a fountain! It bounces off the edges of the screen, so it stays in the arena.
            Velocity.Y += FlyingGravity * dt;
            Position += Velocity * dt;
            if ((Position.X < 40 && Velocity.X < 0) || (Position.X > 1240 && Velocity.X > 0)) Velocity.X = -Velocity.X;
            if (Velocity.Y > 0 && Position.Y >= LandY)
            {
                Position = new Vector2(Position.X, LandY); // landed: now it floats, like every bolt
                Velocity = Vector2.Zero;
            }
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float bob = Mathf.Sin(time * 5f) * 4f;
        float turn = time * 2f;
        var gold = new Color(1f, 0.82f, 0.2f);

        DrawCircle(new Vector2(0, bob), Radius + 6, new Color(1f, 0.9f, 0.4f, 0.25f)); // glow
        var hex = new Vector2[6];
        for (int i = 0; i < 6; i++)
            hex[i] = new Vector2(0, bob) + Vector2.Right.Rotated(turn + i * Mathf.Tau / 6f) * Radius;
        DrawColoredPolygon(hex, gold);
        DrawCircle(new Vector2(0, bob), 5, gold.Darkened(0.35f));
        DrawCircle(new Vector2(-4, bob - 5), 2.5f, new Color(1, 1, 1, 0.8f)); // sparkle
    }
}
