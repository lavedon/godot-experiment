using Godot;

/// <summary>
/// The look picker, under Bolt-E on the menus: an arrow on each side (click them, or press LEFT / RIGHT),
/// the look's name in the middle, which look it is ("3/15"), and for a locked look a padlock and what it takes
/// to win it, like "Need 120 more bolts!". Everything is drawn with code.
/// Unlike the rest of the strip on the left, it catches mouse clicks (so clicking an arrow doesn't start a game).
/// </summary>
public partial class LookPicker : Control
{
    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    static readonly Color LockGold = new(1f, 0.78f, 0.15f);
    static readonly Color GoalRed = new(1f, 0.75f, 0.75f);

    public string LookName = "CLASSIC";
    public bool Locked;          // true = he's only trying it on: it isn't won yet
    public string Goal = "";     // what it takes to win it, like "Ride to THE MOON!"
    public int Number = 1;       // which look it is: 1 = the first one
    public int Count = 1;        // how many looks there are

    /// <summary>Somebody clicked an arrow: -1 = the left arrow, 1 = the right arrow.</summary>
    public event Action<int>? ArrowClicked;

    /// <summary>How far right anything was drawn last time (the self-test checks it stays inside the picker).</summary>
    public float DrawnRight { get; private set; }

    // The two arrows (triangles pointing left and right), made once
    static readonly Vector2[] LeftArrow = { new(4, 30), new(24, 17), new(24, 43) };
    static readonly Vector2[] RightArrow = { new(302, 30), new(282, 17), new(282, 43) };
    static readonly Vector2[] LeftArrowOutline = { new(4, 30), new(24, 17), new(24, 43), new(4, 30) };
    static readonly Vector2[] RightArrowOutline = { new(302, 30), new(282, 17), new(282, 43), new(302, 30) };

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        DrawnRight = 0;

        // White arrows with a dark outline
        DrawColoredPolygon(LeftArrow, Colors.White);
        DrawPolyline(LeftArrowOutline, Ink, 3, true);
        DrawColoredPolygon(RightArrow, Colors.White);
        DrawPolyline(RightArrowOutline, Ink, 3, true);
        DrawnRight = 302 + 1.5f;

        // The name in the middle, and which look it is ("3/15") under it
        float middle = Size.X / 2;
        DrawCentered(font, LookName, middle, 40, 26, Colors.White);
        DrawCentered(font, $"{Number}/{Count}", middle, 60, 14, Colors.White);

        // Locked: a padlock and what it takes to win it
        if (Locked && Goal != "")
        {
            const float lockWidth = 16, gap = 8;
            float goalWidth = font.GetStringSize(Goal, HorizontalAlignment.Left, -1, 18).X;
            float left = middle - (lockWidth + gap + goalWidth) / 2;
            DrawPadlock(new Vector2(left + lockWidth / 2, 82));
            DrawOutlined(font, new Vector2(left + lockWidth + gap, 88), Goal, 18, GoalRed);
        }
    }

    /// <summary>A little padlock: the loop on top (an arc) and the gold body with a keyhole.</summary>
    void DrawPadlock(Vector2 middle)
    {
        DrawArc(middle + new Vector2(0, -4), 5, Mathf.Pi, Mathf.Tau, 10, Ink, 5, true);
        DrawArc(middle + new Vector2(0, -4), 5, Mathf.Pi, Mathf.Tau, 10, new Color(0.85f, 0.87f, 0.92f), 2.5f, true);
        Shapes.RoundRect(this, new Rect2(middle + new Vector2(-9, -5), new Vector2(18, 14)), 4, Ink);
        Shapes.RoundRect(this, new Rect2(middle + new Vector2(-7.5f, -3.5f), new Vector2(15, 11)), 3, LockGold);
        DrawCircle(middle + new Vector2(0, 1.5f), 2, Ink);
    }

    /// <summary>Outlined words with their middle at x (we measure them, so they're exactly in the middle).</summary>
    void DrawCentered(Font font, string text, float x, float baseline, int size, Color color)
    {
        float width = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        DrawOutlined(font, new Vector2(x - width / 2, baseline), text, size, color);
    }

    /// <summary>Words with a dark outline, starting at "at" (the bottom of the letters).</summary>
    void DrawOutlined(Font font, Vector2 at, string text, int size, Color color)
    {
        int outline = Math.Max(6, size / 6);
        float width = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, outline, Ink);
        DrawString(font, at, text, HorizontalAlignment.Left, -1, size, color);
        DrawnRight = Math.Max(DrawnRight, at.X + width + outline / 2f);
    }

    /// <summary>A click on the left end goes to the look before, a click on the right end to the next look.</summary>
    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click) return;
        if (click.Position.X < 60) ArrowClicked?.Invoke(-1);
        else if (click.Position.X > Size.X - 60) ArrowClicked?.Invoke(1);
        AcceptEvent();
    }
}
