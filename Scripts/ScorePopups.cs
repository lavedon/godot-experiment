using Godot;

/// <summary>Little floating numbers like "+25" that pop up when you earn points, then float away.</summary>
public partial class ScorePopups : Node2D
{
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
        popups.Add(new Popup { Text = text, Position = at, Color = color, Life = 1.0f, Size = size });
    }

    public void Clear() => popups.Clear();

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            popups[i].Life -= dt;
            popups[i].Position += new Vector2(0, -70 * dt); // float upward
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
            var at = popup.Position - new Vector2(100, 0); // centered in a 200-pixel-wide box
            DrawStringOutline(font, at, popup.Text, HorizontalAlignment.Center, 200, popup.Size, 8, new Color(0.13f, 0.18f, 0.32f, fade));
            DrawString(font, at, popup.Text, HorizontalAlignment.Center, 200, popup.Size, new Color(popup.Color, fade));
        }
    }
}
