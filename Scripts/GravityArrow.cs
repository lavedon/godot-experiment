using Godot;

/// <summary>
/// UPSIDE-DOWN STATION's gravity arrow, in the top-right corner (under the Best score): a round neon badge with a big
/// arrow that points the way DOWN is right now. When Bolt-E flips, it turns around with him. Everything is drawn with code.
/// </summary>
public partial class GravityArrow : Control
{
    static readonly Color Ink = new(0.08f, 0.06f, 0.16f, 0.85f);
    static readonly Color NeonCyan = new(0.35f, 0.95f, 1f);
    static readonly Color NeonMagenta = new(1f, 0.35f, 0.85f);

    // The arrow pointing down, around its middle (made once; turned with the drawing transform)
    static readonly Vector2[] ArrowShape =
    {
        new(-7, -18), new(7, -18), new(7, 2), new(15, 2), new(0, 19), new(-15, 2), new(-7, 2),
    };

    /// <summary>How far the arrow has turned: 0 = pointing down, Pi = pointing up (Robot.RollAngle).</summary>
    public float Angle { get; private set; }

    /// <summary>Points the arrow (0 = down, Pi = up). It's only redrawn when it really turned.</summary>
    public void Point(float angle)
    {
        if (Mathf.IsEqualApprox(angle, Angle)) return;
        Angle = angle;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var middle = Size / 2;
        float radius = Mathf.Min(Size.X, Size.Y) / 2 - 2;
        bool up = Angle > Mathf.Pi / 2;
        var ring = up ? NeonMagenta : NeonCyan; // magenta when the ceiling is "down", cyan for the floor
        DrawCircle(middle, radius, Ink);
        DrawArc(middle, radius, 0, Mathf.Tau, 32, ring, 3, true);
        DrawSetTransform(middle, Angle, Vector2.One);
        DrawColoredPolygon(ArrowShape, ring);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
