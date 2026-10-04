using Godot;

/// <summary>
/// The World Tour postcards on the menus: one little card for every world. A world you've been to shows a tiny
/// picture of it (its sky, a hill, the ground, and a dot for its sun, moon or Earth). Worlds you haven't reached yet
/// are grey with a "?". Worlds you reached for the first time in the game you just played get a gold border and a
/// gold NEW! ribbon. Everything is drawn with code.
/// </summary>
public partial class WorldPostcards : Control
{
    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    static readonly Color Gold = new(1f, 0.78f, 0.15f);
    static readonly Color NotYet = new(0.55f, 0.57f, 0.62f);   // grey: a world you haven't been to yet

    const float CardWidth = 54, CardHeight = 42;
    const float CardSpacing = 61;   // from the left side of one card to the next
    const float CardsTop = 26;      // the cards go under the word "WORLDS"
    const float Frame = 3;          // the frame around each little picture (gold for a NEW world)

    /// <summary>How many worlds this player has reached (1 = only Sunny Hills).</summary>
    public int Reached = 1;
    /// <summary>The cards from this one up to Reached get a NEW! ribbon (99 = none of them).</summary>
    public int NewFrom = 99;

    /// <summary>True if card i gets a NEW! ribbon: a world reached for the first time in the last game.</summary>
    public bool IsNew(int i) => i >= NewFrom && i < Reached;

    // What the cards showed the last time they were drawn (the self-test reads these)
    public int PicturesDrawn { get; private set; }
    public int QuestionMarksDrawn { get; private set; }
    public int RibbonsDrawn { get; private set; }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;

        // "WORLDS" in white with a dark outline, in the top-left corner
        var labelAt = new Vector2(0, font.GetAscent(18));
        DrawStringOutline(font, labelAt, "WORLDS", HorizontalAlignment.Left, -1, 18, 6, Ink);
        DrawString(font, labelAt, "WORLDS", HorizontalAlignment.Left, -1, 18, Colors.White);

        PicturesDrawn = QuestionMarksDrawn = RibbonsDrawn = 0;
        for (int i = 0; i < Worlds.All.Length; i++)
        {
            var card = new Rect2(2 + i * CardSpacing, CardsTop, CardWidth, CardHeight);
            Shapes.RoundRect(this, card, 6, IsNew(i) ? Gold : Ink); // the frame
            var inside = card.Grow(-Frame);

            if (i < Reached)
            {
                DrawLittleWorld(inside, Worlds.All[i]);
                PicturesDrawn++;
            }
            else
            {
                DrawRect(inside, NotYet);
                DrawCentered(font, "?", inside.GetCenter(), 22, Colors.White);
                QuestionMarksDrawn++;
            }

            if (IsNew(i))
            {
                // A gold ribbon across the bottom of the card (inside it, so it never covers the word "WORLDS")
                var ribbon = new Rect2(card.Position + new Vector2(4, 28), new Vector2(46, 13));
                Shapes.RoundRect(this, ribbon, 4, Gold);
                DrawCentered(font, "NEW!", ribbon.GetCenter(), 12, Ink);
                RibbonsDrawn++;
            }
        }
    }

    /// <summary>A tiny picture of a world: its sky, a hill, a strip of ground, and a dot for its sun, moon or Earth.</summary>
    void DrawLittleWorld(Rect2 r, World world)
    {
        // The sky, fading from its top color to its bottom color
        DrawPolygon(
            new[] { r.Position, new Vector2(r.End.X, r.Position.Y), r.End, new Vector2(r.Position.X, r.End.Y) },
            new[] { world.SkyTop, world.SkyTop, world.SkyBottom, world.SkyBottom });

        // Its sun, moon or Earth (a little dot, 6 pixels across)
        DrawCircle(new Vector2(r.End.X - 9, r.Position.Y + 8), 3, world.ThingColor);

        // A hill: the top half of an oval, standing on the ground
        float groundTop = r.End.Y - 7;
        var hill = new Vector2[11];
        for (int i = 0; i < hill.Length; i++)
        {
            float angle = Mathf.Pi + i * Mathf.Pi / (hill.Length - 1); // from the left side, over the top, to the right side
            hill[i] = new Vector2(r.Position.X + 17 + Mathf.Cos(angle) * 17, groundTop + Mathf.Sin(angle) * 12);
        }
        DrawColoredPolygon(hill, world.NearHills);

        // The ground
        DrawRect(new Rect2(r.Position.X, groundTop, r.Size.X, r.End.Y - groundTop), world.Grass);
    }

    /// <summary>Draws words with their middle at "middle". (We measure them, so they're exactly in the middle.)</summary>
    void DrawCentered(Font font, string text, Vector2 middle, int size, Color color)
    {
        float width = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        // Capital letters are about 0.72 of the font size tall, so this puts their middle right on "middle"
        var at = new Vector2(middle.X - width / 2, middle.Y + size * 0.36f);
        DrawString(font, at, text, HorizontalAlignment.Left, -1, size, color);
    }
}
