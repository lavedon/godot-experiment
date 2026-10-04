using Godot;

/// <summary>
/// Little drawing helpers shared by everything in the game.
/// Godot can draw circles and rectangles, so we combine them into friendlier shapes.
/// </summary>
public static class Shapes
{
    /// <summary>A rectangle with round corners (made from 2 rectangles + 4 circles).</summary>
    public static void RoundRect(CanvasItem canvas, Rect2 rect, float radius, Color color)
    {
        radius = Mathf.Min(radius, Mathf.Min(rect.Size.X, rect.Size.Y) / 2f);
        canvas.DrawRect(new Rect2(rect.Position.X + radius, rect.Position.Y, rect.Size.X - radius * 2, rect.Size.Y), color);
        canvas.DrawRect(new Rect2(rect.Position.X, rect.Position.Y + radius, rect.Size.X, rect.Size.Y - radius * 2), color);
        canvas.DrawCircle(rect.Position + new Vector2(radius, radius), radius, color);
        canvas.DrawCircle(rect.Position + new Vector2(rect.Size.X - radius, radius), radius, color);
        canvas.DrawCircle(rect.Position + new Vector2(radius, rect.Size.Y - radius), radius, color);
        canvas.DrawCircle(rect.End - new Vector2(radius, radius), radius, color);
    }

    /// <summary>A happy "^" shape, used for smiling eyes.</summary>
    public static void HappyEye(CanvasItem canvas, Vector2 center, float size, Color color, float width = 4f)
    {
        canvas.DrawArc(center + new Vector2(0, size * 0.4f), size, Mathf.Pi * 1.15f, Mathf.Pi * 1.85f, 12, color, width, true);
    }

    /// <summary>
    /// The 32 corner points of an oval, made once and used again and again. (DrawColoredPolygon copies the points,
    /// so the next oval can reuse them. That way the game doesn't make a new list for every oval in every frame.)
    /// </summary>
    static readonly Vector2[] scratch = new Vector2[32];

    /// <summary>An oval (a stretched circle). It can be turned with "rotation".</summary>
    public static void Ellipse(CanvasItem canvas, Vector2 center, Vector2 radius, Color color, float rotation = 0)
    {
        for (int i = 0; i < scratch.Length; i++)
        {
            float angle = i * Mathf.Tau / scratch.Length;
            scratch[i] = center + new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y).Rotated(rotation);
        }
        canvas.DrawColoredPolygon(scratch, color);
    }

    /// <summary>A glowing eye with a slanted "angry eyebrow" cut out of its top inner corner.</summary>
    public static void AngryEye(CanvasItem canvas, Vector2 center, Vector2 size, Color color, Color screen, bool innerOnRight)
    {
        RoundRect(canvas, new Rect2(center - size / 2 - new Vector2(3, 3), size + new Vector2(6, 6)), 6, new Color(color, 0.22f));
        RoundRect(canvas, new Rect2(center - size / 2, size), 3.5f, color);
        float top = center.Y - size.Y / 2 - 3;
        float outer = innerOnRight ? center.X - size.X / 2 - 3 : center.X + size.X / 2 + 3;
        float inner = innerOnRight ? center.X + size.X / 2 + 3 : center.X - size.X / 2 - 3;
        canvas.DrawColoredPolygon(new[] { new Vector2(outer, top), new Vector2(inner, top), new Vector2(inner, top + size.Y * 0.6f) }, screen);
    }

    /// <summary>A 5-pointed star (points go out to "size", the dents go in to less than half).</summary>
    public static void Star(CanvasItem canvas, Vector2 center, float size, float rotation, Color color)
    {
        var points = new Vector2[10];
        for (int i = 0; i < 10; i++)
            points[i] = center + Vector2.Up.Rotated(rotation + i * Mathf.Pi / 5f) * (i % 2 == 0 ? size : size * 0.45f);
        canvas.DrawColoredPolygon(points, color);
    }

    /// <summary>An "X" shape, used when the robot bonks into something.</summary>
    public static void XEye(CanvasItem canvas, Vector2 center, float size, Color color)
    {
        canvas.DrawLine(center + new Vector2(-size, -size), center + new Vector2(size, size), color, 4f, true);
        canvas.DrawLine(center + new Vector2(-size, size), center + new Vector2(size, -size), color, 4f, true);
    }

    /// <summary>A heart, used for love-struck eyes: two circles on top and a triangle pointing down.</summary>
    public static void HeartEye(CanvasItem canvas, Vector2 center, float size, Color color)
    {
        canvas.DrawCircle(center + new Vector2(-0.45f * size, -0.2f * size), 0.5f * size, color);
        canvas.DrawCircle(center + new Vector2(0.45f * size, -0.2f * size), 0.5f * size, color);
        canvas.DrawColoredPolygon(new[]
        {
            center + new Vector2(-0.9f * size, 0), center + new Vector2(0.9f * size, 0), center + new Vector2(0, 0.95f * size),
        }, color);
    }
}
