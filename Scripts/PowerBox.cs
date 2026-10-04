using Godot;

/// <summary>
/// A rainbow mystery "?" box floating over the road. Jump into it and the slot machine over Bolt-E's head
/// spins to pick a power! (It floats so high that you only reach it by jumping.) Position is the middle of the box.
/// </summary>
public partial class PowerBox : Node2D
{
    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    float time;

    /// <summary>Does the box touch this bump box? (We're generous: 8 pixels of extra reach all around.)</summary>
    public bool Touches(Rect2 bumpBox) => new Rect2(Position - new Vector2(28, 28), new Vector2(56, 56)).Grow(8).Intersects(bumpBox);

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float bob = Mathf.Sin(time * 4f) * 5f;                   // it floats gently up and down
        var rainbow = Color.FromHsv(time * 0.5f % 1, 0.75f, 1); // the color slowly goes all the way around the rainbow

        DrawStars(bob, inFront: false);

        // The box: a rainbow edge, and a light rainbow inside (a little further around the rainbow)
        var box = new Rect2(-28, -28 + bob, 56, 56);
        Shapes.RoundRect(this, box, 12, rainbow);
        Shapes.RoundRect(this, box.Grow(-5), 8, Color.FromHsv((time * 0.5f + 0.15f) % 1, 0.3f, 1));

        // A big "?" in the middle. We measure it, so it's exactly in the middle.
        var font = ThemeDB.FallbackFont;
        const int fontSize = 44;
        var size = font.GetStringSize("?", HorizontalAlignment.Left, -1, fontSize);
        var at = new Vector2(-size.X / 2, bob - size.Y / 2 + font.GetAscent(fontSize));
        var questionColor = Color.FromHsv((time * 0.5f + 0.5f) % 1, 0.8f, 0.95f); // the opposite side of the rainbow
        DrawStringOutline(font, at, "?", HorizontalAlignment.Left, -1, fontSize, 8, Ink);
        DrawString(font, at, "?", HorizontalAlignment.Left, -1, fontSize, questionColor);

        DrawStars(bob, inFront: true);
    }

    /// <summary>Two little stars fly around the box. When they're behind it, the box covers them.</summary>
    void DrawStars(float bob, bool inFront)
    {
        for (int i = 0; i < 2; i++)
        {
            float angle = time * 3f + i * Mathf.Pi;
            bool front = Mathf.Sin(angle) > 0; // the bottom half of the circle is the side closer to us
            if (front != inFront) continue;
            var at = new Vector2(Mathf.Cos(angle) * 42, bob + Mathf.Sin(angle) * 12 - 4);
            Shapes.Star(this, at, 7, time * 5f, new Color(1f, 0.92f, 0.35f));
        }
    }
}
