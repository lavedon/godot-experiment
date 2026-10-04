using Godot;

public enum ObstacleKind
{
    Crate,       // a wooden box: jump over it!
    Cone,        // an orange traffic cone: jump over it!
    CrateStack,  // two boxes: a big jump!
    Drone,       // a grumpy flying drone: DON'T jump, ride under it!
}

/// <summary>
/// Something the robot must not bump into. Ground obstacles sit with their bottom-middle at Position.
/// Drones float in the air with their middle at Position.
/// MEGA BOLT-E can SMASH them: then they burst into bouncing pieces (the same kind of KABOOM as Bolt-E's crash).
/// </summary>
public partial class Obstacle : Node2D
{
    public ObstacleKind Kind;
    float time;

    /// <summary>True once MEGA BOLT-E has smashed it into pieces (then it can't bump anyone).</summary>
    public bool Smashed { get; private set; }
    /// <summary>True when the smashed pieces have bounced around long enough and can be taken away.</summary>
    public bool Finished => Smashed && smashTime > 1.6f;
    /// <summary>True while it's drawn as bouncing pieces (instead of in one piece).</summary>
    public bool InPieces => explosion is not null;
    /// <summary>Where its KABOOM started, measured from its Position (null until it's smashed).</summary>
    public Vector2? BoomCenter => explosion?.BoomCenter;
    float smashTime;               // seconds since it was smashed
    RobotExplosion? explosion;     // the bouncing pieces (not null once it's smashed)

    /// <summary>The bump box. A little smaller than the drawing so close calls feel fair.</summary>
    public Rect2 Hitbox => Kind switch
    {
        ObstacleKind.Crate => new Rect2(Position + new Vector2(-23, -50), new Vector2(46, 50)),
        ObstacleKind.CrateStack => new Rect2(Position + new Vector2(-23, -106), new Vector2(46, 106)),
        ObstacleKind.Cone => new Rect2(Position + new Vector2(-13, -56), new Vector2(26, 56)),
        _ => new Rect2(Position + new Vector2(-30, -16), new Vector2(60, 32)),
    };

    /// <summary>SMASH! It bursts into bouncing pieces, like Bolt-E does when he crashes.</summary>
    public void Smash()
    {
        if (Smashed) return;
        Smashed = true;
        smashTime = 0;

        // Its pieces: each one knows where its middle is, how big it is, and how to draw itself
        bool drone = Kind == ObstacleKind.Drone;
        var parts = new List<RobotPart>();
        switch (Kind)
        {
            case ObstacleKind.Crate:
                parts.Add(new RobotPart("crate", new Vector2(0, -28), 28, () => DrawCrate(Vector2.Zero)));
                break;
            case ObstacleKind.CrateStack:
                parts.Add(new RobotPart("crate", new Vector2(0, -28), 28, () => DrawCrate(Vector2.Zero)));
                parts.Add(new RobotPart("top crate", new Vector2(0, -84), 28, () => DrawCrate(new Vector2(0, -56))));
                break;
            case ObstacleKind.Cone:
                parts.Add(new RobotPart("cone", new Vector2(0, -33), 26, DrawCone, FlatEvery: Mathf.Pi));
                break;
            default:
                parts.Add(new RobotPart("drone", Vector2.Zero, 34, DrawDrone, FlatEvery: Mathf.Pi));
                break;
        }
        // The KABOOM starts right in its middle, so the pieces pop up and out.
        var middle = Kind switch
        {
            ObstacleKind.Crate => new Vector2(0, -28),
            ObstacleKind.CrateStack => new Vector2(0, -56),
            ObstacleKind.Cone => new Vector2(0, -33),
            _ => Vector2.Zero,
        };
        // Drones are little robots, and crates are full of spare robot parts, so gears, screws and springs fly out.
        // A cone is just orange plastic.
        int littleBits = Kind == ObstacleKind.Cone ? 0 : 12;
        // (A drone floats 175 pixels above the ground, so its pieces fall that far before they bounce.)
        explosion = new RobotExplosion(parts, new Vector2(0, drone ? 0 : -30), 0, drone ? 175f : 0f, new Color(1f, 0.85f, 0.4f),
                                       boomAt: middle, littleBits: littleBits);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        if (Smashed)
        {
            explosion?.Update(dt, 0); // (0, because the obstacle itself already slides along with the world)
            smashTime += dt;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (explosion is not null)
        {
            explosion.Draw(this); // in pieces!
            return;
        }

        switch (Kind)
        {
            case ObstacleKind.Crate:
                DrawCrate(Vector2.Zero);
                break;
            case ObstacleKind.CrateStack:
                DrawCrate(Vector2.Zero);
                DrawCrate(new Vector2(0, -56));
                break;
            case ObstacleKind.Cone:
                DrawCone();
                break;
            case ObstacleKind.Drone:
                DrawDrone();
                break;
        }
    }

    void DrawCrate(Vector2 bottom)
    {
        var wood = new Color(0.85f, 0.6f, 0.32f);
        var dark = new Color(0.6f, 0.38f, 0.18f);
        var box = new Rect2(bottom + new Vector2(-28, -56), new Vector2(56, 56));
        Shapes.RoundRect(this, box, 6, dark);
        Shapes.RoundRect(this, box.Grow(-5), 4, wood);
        DrawLine(box.Position + new Vector2(8, 8), box.End - new Vector2(8, 8), dark, 5f, true);
        DrawLine(new Vector2(box.Position.X + 8, box.End.Y - 8), new Vector2(box.End.X - 8, box.Position.Y + 8), dark, 5f, true);
    }

    void DrawCone()
    {
        var orange = new Color(1f, 0.5f, 0.15f);
        DrawRect(new Rect2(-26, -8, 52, 8), orange.Darkened(0.3f));
        DrawColoredPolygon(new[] { new Vector2(-20, -8), new Vector2(20, -8), new Vector2(5, -66), new Vector2(-5, -66) }, orange);
        DrawColoredPolygon(new[] { new Vector2(-15, -26), new Vector2(15, -26), new Vector2(11, -40), new Vector2(-11, -40) }, Colors.White);
    }

    void DrawDrone()
    {
        float hover = Mathf.Sin(time * 4f) * 6f;
        var c = new Vector2(0, hover);
        var metal = new Color(0.45f, 0.45f, 0.55f);

        // Spinning propellers
        float blade = Mathf.Abs(Mathf.Sin(time * 40f)) * 22f + 4f;
        foreach (float x in new[] { -24f, 24f })
        {
            DrawLine(c + new Vector2(x, -14), c + new Vector2(x, -24), metal, 3f);
            DrawLine(c + new Vector2(x - blade, -25), c + new Vector2(x + blade, -25), new Color(0.3f, 0.3f, 0.35f), 3f, true);
        }

        // Body
        Shapes.RoundRect(this, new Rect2(c + new Vector2(-34, -16), new Vector2(68, 32)), 14, new Color(0.62f, 0.4f, 0.75f));

        // One grumpy eye with an angry eyebrow
        DrawCircle(c, 10, Colors.White);
        DrawCircle(c + new Vector2(-3, 1), 5, new Color(0.9f, 0.15f, 0.2f));
        DrawLine(c + new Vector2(-13, -15), c + new Vector2(11, -8), new Color(0.2f, 0.1f, 0.25f), 4f, true);
    }
}
