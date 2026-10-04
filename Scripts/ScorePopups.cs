using Godot;

/// <summary>Little floating numbers like "+25" that pop up when you earn points, then float away.</summary>
public partial class ScorePopups : Node2D
{
    // ---- Try changing these! ----
    public const float LifeSeconds = 1f;    // how long the words stay on the screen
    public const float FloatSpeed = 70f;    // how fast they float up (pixels per second)

    /// <summary>How far the words float up before they're gone (pixels).</summary>
    public const float FloatUp = FloatSpeed * LifeSeconds;

    class Popup
    {
        public Vector2 Position;
        public string Text = "";
        public float Life;
        public Color Color;
        public int Size;
    }

    readonly List<Popup> popups = new();

    public void Add(string text, Vector2 at, Color color, int size = 30)
    {
        popups.Add(new Popup { Text = text, Position = at, Color = color, Life = LifeSeconds, Size = size });
    }

    public void Clear() => popups.Clear();

    /// <summary>True if these words popped up less than "seconds" ago (so the same words don't pile up on top of each other).</summary>
    public bool JustAdded(string text, float seconds) => popups.Any(popup => popup.Text == text && LifeSeconds - popup.Life < seconds);

    /// <summary>The words floating on the screen right now (the self-test reads these).</summary>
    public IEnumerable<string> Texts => popups.Select(popup => popup.Text);

    /// <summary>The words floating on the screen right now, where they are (the bottom middle of the letters) and how big (the self-test reads these).</summary>
    public IEnumerable<(string Text, Vector2 At, int Size)> Spots => popups.Select(popup => (popup.Text, popup.Position, popup.Size));

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            popups[i].Life -= dt;
            popups[i].Position += new Vector2(0, -FloatSpeed * dt); // float upward
            if (popups[i].Life <= 0) popups.RemoveAt(i);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        foreach (var popup in popups)
        {
            float fade = Mathf.Clamp(popup.Life * 2, 0, 1);
            // Measure the words, then center them in a box that's always wide enough (so long ones like "FULL! +100" never get cut off)
            float measured = font.GetStringSize(popup.Text, HorizontalAlignment.Left, -1, popup.Size).X;
            float width = Mathf.Max(200, measured + 20);
            var at = popup.Position - new Vector2(width / 2, 0);
            DrawStringOutline(font, at, popup.Text, HorizontalAlignment.Center, width, popup.Size, 8, new Color(0.13f, 0.18f, 0.32f, fade));
            DrawString(font, at, popup.Text, HorizontalAlignment.Center, width, popup.Size, new Color(popup.Color, fade));
        }
    }
}
