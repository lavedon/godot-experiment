using System.Globalization;
using Godot;

/// <summary>
/// The Bolt Bank, in the top-left corner of the menus: every bolt this player has EVER grabbed (it never goes down!),
/// and a bar that fills up on the way to the next prize, like "Next: COWBOY in 57". After a game the number counts up
/// and shows "+31" for the bolts that game added. Everything is drawn with code.
/// </summary>
public partial class BankBox : Control
{
    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    static readonly Color Gold = new(1f, 0.78f, 0.15f);
    static readonly Color PlusGreen = new(0.45f, 1f, 0.5f);

    public int Bank;           // the bolts in the bank
    public int Plus;           // the bolts the last game added ("+31" while the bank counts up, 0 = don't show it)
    public string Next = "";   // the next prize, like "Next: COWBOY in 57"
    public float Fraction;     // how full the bar is: 0 = empty, 1 = full

    /// <summary>How far right anything was drawn last time (the self-test checks it stays inside the box).</summary>
    public float DrawnRight { get; private set; }

    // The 6 corners of the gold hexagon (reused every time, so the game doesn't make new lists all the time)
    static readonly Vector2[] hexagon = new Vector2[6];

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        DrawnRight = 0;

        // "BOLT BANK" in white with a dark outline, in the top-left corner
        DrawOutlined(font, new Vector2(0, 20), "BOLT BANK", 18, Colors.White);

        // A gold hexagon with a hole in the middle (the nut that goes on a bolt), then the number
        var nut = new Vector2(14, 51);
        DrawHexagon(nut, 16, Ink);
        DrawHexagon(nut, 14, Gold);
        DrawCircle(nut, 5, Ink);
        DrawnRight = Math.Max(DrawnRight, nut.X + 16);
        string number = Bank.ToString("N0", CultureInfo.InvariantCulture); // 12345 -> "12,345"
        float numberWidth = DrawOutlined(font, new Vector2(36, 63), number, 34, Gold);
        if (Plus > 0) DrawOutlined(font, new Vector2(36 + numberWidth + 10, 63), $"+{Plus}", 22, PlusGreen);

        // The bar on the way to the next prize
        DrawRect(new Rect2(0, 72, 300, 14), Ink);
        float fill = Mathf.Clamp(Fraction, 0, 1);
        if (fill > 0) DrawRect(new Rect2(2, 74, 296 * fill, 10), Gold);
        DrawnRight = Math.Max(DrawnRight, 300);

        // The next prize (or "You got them ALL!")
        DrawOutlined(font, new Vector2(0, 104), Next, 16, Colors.White);
    }

    /// <summary>A six-sided shape with its middle at "middle".</summary>
    void DrawHexagon(Vector2 middle, float radius, Color color)
    {
        for (int i = 0; i < 6; i++)
            hexagon[i] = middle + Vector2.Right.Rotated(i * Mathf.Tau / 6) * radius;
        DrawColoredPolygon(hexagon, color);
    }

    /// <summary>Words with a dark outline, starting at "at" (the bottom of the letters). Returns how wide they are.</summary>
    float DrawOutlined(Font font, Vector2 at, string text, int size, Color color)
    {
        int outline = Math.Max(6, size / 6);
        float width = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, outline, Ink);
        DrawString(font, at, text, HorizontalAlignment.Left, -1, size, color);
        DrawnRight = Math.Max(DrawnRight, at.X + width + outline / 2f);
        return width;
    }
}
