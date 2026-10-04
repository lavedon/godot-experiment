using Godot;

/// <summary>
/// Bolt-E's spare batteries, shown under "Bolts" while playing.
/// Each green battery is one spare (a crash with a spare = the magic rebuild instead of game over!).
/// The dark battery at the end is charging: it fills up with yellow as you grab bolts, and when it's full
/// it turns into a new green spare (1-UP!). Everything is drawn with code, like the rest of the game.
/// </summary>
public partial class BatteryRow : Control
{
    public const float Gap = 46;      // pixels from one battery to the next

    public int Spares;                // how many green spare batteries to draw
    public float Fill;                // how full the charging battery is (0 = empty, 1 = full)
    public bool ShowCharger = true;   // false for the battery that flies to Bolt-E (it's just one green battery)

    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    static readonly Color Green = new(0.35f, 0.85f, 0.4f);
    static readonly Color Dark = new(0.2f, 0.22f, 0.3f);
    static readonly Color Yellow = new(1f, 0.85f, 0.25f);

    public override void _Draw()
    {
        for (int i = 0; i < Spares; i++)
            DrawBattery(i * Gap, charged: true);
        if (ShowCharger && Spares < Main.MaxBatteries)
            DrawBattery(Spares * Gap, charged: false);
    }

    /// <summary>One battery: a dark outline, the inside (green, or dark with a yellow charge), and a little bump on the end.</summary>
    void DrawBattery(float x, bool charged)
    {
        Shapes.RoundRect(this, new Rect2(x, 3, 40, 28), 6, Ink);             // outline
        DrawRect(new Rect2(x + 40, 12, 4, 10), Ink);                          // the bump (the + end)
        Shapes.RoundRect(this, new Rect2(x + 3, 6, 34, 22), 5, charged ? Green : Dark);

        if (charged)
        {
            // A white lightning bolt: a zig-zag shape inside the battery
            var bolt = new[] { new Vector2(7, 0), new Vector2(0, 11), new Vector2(5, 11), new Vector2(3, 20),
                               new Vector2(12, 7), new Vector2(7, 7), new Vector2(10, 0) };
            for (int i = 0; i < bolt.Length; i++) bolt[i] += new Vector2(x + 14, 7);
            DrawColoredPolygon(bolt, Colors.White);
        }
        else if (Fill > 0.01f)
        {
            // The yellow charge grows from left to right as you grab bolts
            Shapes.RoundRect(this, new Rect2(x + 6, 9, 28 * Mathf.Min(Fill, 1f), 16), 3, Yellow);
        }
    }
}
