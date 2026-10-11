using Godot;

/// <summary>
/// The World Tour postcards on the menus: one little card for every world. A world you've been to shows a tiny
/// picture of it (its sky, a hill, the ground, and a dot for its sun, moon or Earth). Worlds you haven't reached yet
/// are grey with a "?". Worlds you reached for the first time in the game you just played get a gold border and a
/// gold NEW! ribbon. The world the next game starts in glows (pick it with DOWN on the title screen), and "START: ..."
/// under the cards says which one it is. Everything is drawn with code.
/// </summary>
public partial class WorldPostcards : Control
{
    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    static readonly Color Gold = new(1f, 0.78f, 0.15f);
    static readonly Color NotYet = new(0.55f, 0.57f, 0.62f);   // grey: a world you haven't been to yet
    static readonly Color PickGlow = new(0.35f, 0.95f, 1f);    // the glow around the world the next game starts in

    // (6 cards have to fit in the strip's 306 pixels: 3 + 5 x 50 + 46 = 299, and the glow adds 3 on each side)
    const float CardWidth = 46, CardHeight = 34;
    const float CardSpacing = 50;   // from the left side of one card to the next
    const float CardsLeft = 3;      // room for the glow around the first card
    const float CardsTop = 24;      // the cards go under the word "WORLDS"
    const float StartLineTop = 60;  // "START: SUNNY HILLS" goes under the cards
    const float Frame = 3;          // the frame around each little picture (gold for a NEW world)
    const string PickHint = "DOWN = pick a world";

    /// <summary>How many worlds this player has reached (1 = only Sunny Hills).</summary>
    public int Reached = 1;
    /// <summary>The cards from this one up to Reached get a NEW! ribbon (99 = none of them).</summary>
    public int NewFrom = 99;
    /// <summary>The world the next game starts in (its card glows).</summary>
    public int Picked;
    /// <summary>True on the title screen: "DOWN = pick a world" shows next to the word "WORLDS".</summary>
    public bool ShowPickHint;

    /// <summary>True if card i gets a NEW! ribbon: a world reached for the first time in the last game.</summary>
    public bool IsNew(int i) => i >= NewFrom && i < Reached;

    // What the cards showed the last time they were drawn (the self-test reads these)
    public int PicturesDrawn { get; private set; }
    public int QuestionMarksDrawn { get; private set; }
    public int RibbonsDrawn { get; private set; }
    /// <summary>The "START: ..." words, where the glowing card is, and the right edge of everything drawn.</summary>
    public string StartLine { get; private set; } = "";
    public Rect2 PickedCard { get; private set; }
    public float DrawnRight { get; private set; }
    public bool HintDrawn { get; private set; }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        DrawnRight = 0;

        // "WORLDS" in white with a dark outline, in the top-left corner
        var labelAt = new Vector2(0, font.GetAscent(18));
        DrawStringOutline(font, labelAt, "WORLDS", HorizontalAlignment.Left, -1, 18, 6, Ink);
        DrawString(font, labelAt, "WORLDS", HorizontalAlignment.Left, -1, 18, Colors.White);

        // On the title screen: "DOWN = pick a world", on the right
        HintDrawn = ShowPickHint;
        if (ShowPickHint)
        {
            float hintWidth = font.GetStringSize(PickHint, HorizontalAlignment.Left, -1, 14).X;
            var hintAt = new Vector2(Size.X - hintWidth - 3, font.GetAscent(18));
            DrawStringOutline(font, hintAt, PickHint, HorizontalAlignment.Left, -1, 14, 5, Ink);
            DrawString(font, hintAt, PickHint, HorizontalAlignment.Left, -1, 14, PickGlow);
            DrawnRight = Mathf.Max(DrawnRight, hintAt.X + hintWidth);
        }

        PicturesDrawn = QuestionMarksDrawn = RibbonsDrawn = 0;
        int picked = Mathf.Clamp(Picked, 0, Worlds.All.Length - 1);
        for (int i = 0; i < Worlds.All.Length; i++)
        {
            var card = new Rect2(CardsLeft + i * CardSpacing, CardsTop, CardWidth, CardHeight);
            if (i == picked)
            {
                Shapes.RoundRect(this, card.Grow(3), 8, PickGlow); // the glow around the world the next game starts in
                PickedCard = card;
            }
            Shapes.RoundRect(this, card, 6, IsNew(i) ? Gold : Ink); // the frame
            var inside = card.Grow(-Frame);
            DrawnRight = Mathf.Max(DrawnRight, card.End.X + (i == picked ? 3 : 0));

            if (i < Reached)
            {
                DrawLittleWorld(this, inside, Worlds.All[i]);
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
                var ribbon = new Rect2(card.Position + new Vector2(3, CardHeight - 15), new Vector2(CardWidth - 6, 13));
                Shapes.RoundRect(this, ribbon, 4, Gold);
                DrawCentered(font, "NEW!", ribbon.GetCenter(), 12, Ink);
                RibbonsDrawn++;
            }
        }

        // "START: UPSIDE-DOWN STATION" under the cards (smaller letters if it's too long to fit)
        StartLine = $"START: {Worlds.All[picked].Name}";
        int size = 14;
        while (size > 9 && font.GetStringSize(StartLine, HorizontalAlignment.Left, -1, size).X > Size.X - 6) size--;
        var startAt = new Vector2(2, StartLineTop + font.GetAscent(size));
        DrawStringOutline(font, startAt, StartLine, HorizontalAlignment.Left, -1, size, 5, Ink);
        DrawString(font, startAt, StartLine, HorizontalAlignment.Left, -1, size, Gold);
        DrawnRight = Mathf.Max(DrawnRight, startAt.X + font.GetStringSize(StartLine, HorizontalAlignment.Left, -1, size).X);
    }

    /// <summary>
    /// A tiny picture of a world: its sky, a hill, a strip of ground, and a dot for its sun, moon or Earth.
    /// "canvas" is what it's drawn on (the pause screen draws the same pictures, only bigger: see PauseMenu.cs).
    /// The picture is made for a 40 x 28 box.
    /// </summary>
    public static void DrawLittleWorld(CanvasItem canvas, Rect2 r, World world)
    {
        // The sky, fading from its top color to its bottom color
        canvas.DrawPolygon(
            new[] { r.Position, new Vector2(r.End.X, r.Position.Y), r.End, new Vector2(r.Position.X, r.End.Y) },
            new[] { world.SkyTop, world.SkyTop, world.SkyBottom, world.SkyBottom });

        if (world.Scenery == Scenery.Station)
        {
            DrawLittleStation(canvas, r, world);
            return;
        }

        // Its sun, moon or Earth (a little dot, 6 pixels across)
        canvas.DrawCircle(new Vector2(r.End.X - 9, r.Position.Y + 8), 3, world.ThingColor);

        // A hill: the top half of an oval, standing on the ground
        float groundTop = r.End.Y - 7;
        var hill = new Vector2[11];
        for (int i = 0; i < hill.Length; i++)
        {
            float angle = Mathf.Pi + i * Mathf.Pi / (hill.Length - 1); // from the left side, over the top, to the right side
            hill[i] = new Vector2(r.Position.X + 15 + Mathf.Cos(angle) * 15, groundTop + Mathf.Sin(angle) * 11);
        }
        canvas.DrawColoredPolygon(hill, world.NearHills);

        // The ground
        canvas.DrawRect(new Rect2(r.Position.X, groundTop, r.Size.X, r.End.Y - groundTop), world.Grass);
    }

    /// <summary>UPSIDE-DOWN STATION's postcard: a ringed planet in space, a magenta ceiling and a glowing cyan floor.</summary>
    static void DrawLittleStation(CanvasItem canvas, Rect2 r, World world)
    {
        var planet = new Vector2(r.Position.X + r.Size.X * 0.62f, r.Position.Y + r.Size.Y * 0.5f);
        canvas.DrawCircle(planet, 5, world.ThingColor);
        canvas.DrawLine(planet + new Vector2(-9, 2), planet + new Vector2(9, -2), new Color(1, 0.9f, 0.7f), 1.5f);
        canvas.DrawCircle(new Vector2(r.Position.X + 7, r.Position.Y + 12), 1, Colors.White);
        canvas.DrawCircle(new Vector2(r.End.X - 6, r.End.Y - 10), 1, Colors.White);
        canvas.DrawRect(new Rect2(r.Position.X, r.Position.Y, r.Size.X, 4), world.NearHills);
        canvas.DrawRect(new Rect2(r.Position.X, r.Position.Y + 4, r.Size.X, 1.5f), world.Pebbles);   // the magenta ceiling edge
        canvas.DrawRect(new Rect2(r.Position.X, r.End.Y - 5, r.Size.X, 5), world.Dirt);
        canvas.DrawRect(new Rect2(r.Position.X, r.End.Y - 6, r.Size.X, 1.5f), world.Grass);          // the cyan floor edge
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
