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

    /// <summary>
    /// A glowing CRYSTAL BOLT from a LAVA CAVE (see Main.LavaCave.cs). It counts as Worth bolts: 1 in an EASY cave,
    /// 2 in a MEDIUM one and 3 in a HARD one.
    /// </summary>
    public bool Crystal;
    public int Worth = 1;

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
        if (Crystal)
        {
            DrawCrystal();
            return;
        }
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

    // The crystal colors: cyan (x1), purple (x2) and pink (x3)
    static readonly Color[] CrystalColors = { new(0.35f, 0.95f, 1f), new(0.72f, 0.4f, 1f), new(1f, 0.4f, 0.75f) };
    static readonly Vector2[] crystal = new Vector2[6]; // (corner points reused every frame)
    static readonly string[] WorthWords = { "", "x1", "x2", "x3" }; // (made once, not every frame)

    /// <summary>A glowing CRYSTAL BOLT: a long six-sided gem that turns and sparkles, with "x2" or "x3" under it.</summary>
    void DrawCrystal()
    {
        float bob = Mathf.Sin(time * 5f) * 4f;
        var color = CrystalColors[Math.Clamp(Worth, 1, 3) - 1];
        var middle = new Vector2(0, bob);
        float pulse = 0.6f + 0.4f * Mathf.Sin(time * 6f);
        DrawCircle(middle, Radius + 9, new Color(color, 0.22f * pulse)); // glow
        float turn = Mathf.Cos(time * 2.5f); // turning around: it looks thinner and wider
        float w = 6 + 4 * Mathf.Abs(turn), tall = Radius + 3;
        crystal[0] = middle + new Vector2(0, -tall);
        crystal[1] = middle + new Vector2(w, -tall * 0.4f);
        crystal[2] = middle + new Vector2(w, tall * 0.4f);
        crystal[3] = middle + new Vector2(0, tall);
        crystal[4] = middle + new Vector2(-w, tall * 0.4f);
        crystal[5] = middle + new Vector2(-w, -tall * 0.4f);
        DrawColoredPolygon(crystal, color);
        DrawLine(middle + new Vector2(0, -tall + 3), middle + new Vector2(0, tall - 3), color.Lightened(0.5f), 2f);
        DrawLine(middle + new Vector2(-w * 0.5f, -tall * 0.3f), middle + new Vector2(-2, -tall * 0.7f), new Color(1, 1, 1, 0.9f), 2f); // shine
        if (Worth > 1)
            VolcanoArt.Words(this, WorthWords[Math.Clamp(Worth, 1, 3)], 0, bob + tall + 15, 14, Colors.White);
    }
}
