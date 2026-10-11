using Godot;

/// <summary>
/// The PAUSE screen, drawn on top of the frozen game (Main.Pause.cs decides what it does). Everything is drawn with code:
/// a dark see-through cover over the game, the big word PAUSED, and three big rows:
///   CONTINUE,
///   WORLD WARP: the World Tour postcards, much bigger (the same little pictures as on the menus, just scaled up).
///               Worlds you can't pick yet are grey with a "?", and the world you're in has a gold YOU ARE HERE ribbon.
///   QUIT TO TITLE.
/// The picked row glows. It also draws the rainbow WARP! flash, after the game goes on.
/// It never freezes itself (ProcessMode.Always, set by Main on its layer), so it still works while the game is paused.
/// </summary>
public partial class PauseMenu : Control
{
    // ---- How it looks (try changing these!) ----
    const float CoverAlpha = 0.62f;          // how dark the cover over the game is (0 = see-through, 1 = black)
    const float CardScale = 3.1f;            // how much bigger than the menu postcards the pictures are (7 must fit!)
    const float FlashSeconds = 0.7f;         // how long the WARP! flash takes to fade away
    const int BigWords = 64, RowWords = 40, CardTitleWords = 30, NameWords = 30, HelpWords = 22;

    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    static readonly Color Gold = new(1f, 0.78f, 0.15f);
    static readonly Color Glow = new(0.35f, 0.95f, 1f);        // the glow around the picked row and card
    static readonly Color RowColor = new(0.2f, 0.27f, 0.45f);  // a row that isn't picked
    static readonly Color PickedRowColor = new(0.25f, 0.42f, 0.7f);
    static readonly Color Grey = new(0.55f, 0.57f, 0.62f);
    static readonly Color Orange = new(1f, 0.55f, 0.25f);
    static readonly Color[] Rainbow =
    {
        new(1f, 0.3f, 0.3f), new(1f, 0.6f, 0.2f), new(1f, 0.9f, 0.25f), new(0.35f, 0.9f, 0.4f), new(0.35f, 0.65f, 1f), new(0.7f, 0.45f, 1f),
    };

    // Where things go on the 1280 x 720 screen
    static readonly Rect2 ContinueBox = new(440, 122, 400, 72);
    static readonly Rect2 WarpBox = new(110, 212, 1060, 240);
    static readonly Rect2 QuitBox = new(440, 470, 400, 72);
    const float CardsTop = 266;              // the top of the postcards (under the words WORLD WARP)
    const float CardSpacing = 150;           // from the left side of one card to the next
    const float Frame = 6;                   // the frame around each picture
    static readonly Vector2 PictureSize = new(40, 28); // a menu postcard's picture (WorldPostcards.DrawLittleWorld)

    /// <summary>True while the pause screen is up.</summary>
    public bool Showing;
    /// <summary>The picked row: 0 = CONTINUE, 1 = WORLD WARP, 2 = QUIT TO TITLE.</summary>
    public int Row;
    /// <summary>The world picked on the WORLD WARP row (0 = Sunny Hills ... 5 = UPSIDE-DOWN STATION, 6 = VOLCANO ISLAND).</summary>
    public int Picked;
    /// <summary>The world we're in (it gets the YOU ARE HERE ribbon).</summary>
    public int Here;
    /// <summary>The worlds that can be picked (the rest are grey with a "?").</summary>
    public HashSet<int> CanWarpTo = new() { 0 };
    /// <summary>Why WORLD WARP is greyed out, like "Beat the boss first!" (null = it isn't).</summary>
    public string? WarpBlocked;
    /// <summary>The WARP! flash: 1 = bright, fading to 0.</summary>
    public float Flash;
    /// <summary>Main's handler for every press while the game is paused (true = it was used up).</summary>
    public Func<InputEvent, bool>? InputWhilePaused;

    // What the last drawing showed (the self-test reads these)
    public int PicturesDrawn { get; private set; }
    public int QuestionMarksDrawn { get; private set; }
    public int HereRibbonOn { get; private set; } = -1;
    public string NameLine { get; private set; } = "";
    public Rect2 DrawnArea { get; private set; }
    public int TimesDrawn { get; private set; }
    public bool WarpGreyed { get; private set; }

    /// <summary>The middle of postcard number i (the self-test reads this too).</summary>
    public static Rect2 CardRect(int i)
    {
        var size = PictureSize * CardScale + new Vector2(Frame, Frame) * 2;
        float left = WarpBox.GetCenter().X - ((Worlds.All.Length - 1) * CardSpacing + size.X) / 2;
        return new Rect2(left + i * CardSpacing, CardsTop, size);
    }

    /// <summary>Starts the rainbow WARP! flash.</summary>
    public void StartFlash()
    {
        Flash = 1;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (Flash <= 0) return;
        Flash = Mathf.MoveToward(Flash, 0, (float)delta / FlashSeconds);
        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (InputWhilePaused?.Invoke(e) == true) GetViewport().SetInputAsHandled();
    }

    public override void _Draw()
    {
        if (Flash > 0) DrawFlash();
        if (!Showing) return;
        TimesDrawn++;
        var font = ThemeDB.FallbackFont;
        var drawn = new Rect2(ContinueBox.Position, Vector2.Zero);

        // A dark see-through cover over the frozen game, and the big word PAUSED
        DrawRect(new Rect2(0, 0, 1280, 720), new Color(0.02f, 0.03f, 0.08f, CoverAlpha));
        drawn = drawn.Merge(Words(font, "PAUSED", 640, 104, BigWords, Colors.White));

        // CONTINUE
        drawn = drawn.Merge(RowBox(ContinueBox, Row == 0, greyed: false));
        drawn = drawn.Merge(Words(font, "CONTINUE", 640, ContinueBox.GetCenter().Y + RowWords * 0.36f, RowWords, Colors.White));

        // WORLD WARP: the postcards
        WarpGreyed = WarpBlocked is not null;
        drawn = drawn.Merge(RowBox(WarpBox, Row == 1, WarpGreyed));
        drawn = drawn.Merge(Words(font, "WORLD WARP", 640, WarpBox.Position.Y + 40, CardTitleWords, WarpGreyed ? Grey : Gold));
        drawn = drawn.Merge(DrawCards(font));

        // Under the postcards: the picked world's name between two arrows, or why warping can't happen now
        float nameY = CardRect(0).End.Y + 44;
        if (WarpGreyed)
        {
            NameLine = WarpBlocked!;
            drawn = drawn.Merge(Words(font, NameLine, 640, nameY, NameWords, Orange));
        }
        else
        {
            NameLine = Worlds.All[Math.Clamp(Picked, 0, Worlds.All.Length - 1)].Name;
            var nameArea = Words(font, NameLine, 640, nameY, NameWords, Row == 1 ? Glow : Colors.White);
            drawn = drawn.Merge(nameArea);
            if (Row == 1)
            {
                // Arrows on both sides of the name: LEFT / RIGHT picks another world
                float middleY = nameY - NameWords * 0.36f;
                Triangle(new Vector2(nameArea.Position.X - 30, middleY), -1, Glow);
                Triangle(new Vector2(nameArea.End.X + 30, middleY), 1, Glow);
                drawn = drawn.Merge(new Rect2(nameArea.Position.X - 44, middleY - 14, nameArea.Size.X + 88, 28));
            }
        }

        // QUIT TO TITLE
        drawn = drawn.Merge(RowBox(QuitBox, Row == 2, greyed: false));
        drawn = drawn.Merge(Words(font, "QUIT TO TITLE", 640, QuitBox.GetCenter().Y + RowWords * 0.36f, RowWords, Colors.White));

        // How it works, at the bottom
        drawn = drawn.Merge(Words(font, "UP / DOWN: choose     A: OK     START: back to the game", 640, 610, HelpWords, new Color(0.85f, 0.9f, 1f)));
        DrawnArea = drawn;
    }

    /// <summary>A row's box: it glows when it's picked (and is darker grey when it's greyed out).</summary>
    Rect2 RowBox(Rect2 box, bool picked, bool greyed)
    {
        var fill = greyed ? new Color(0.25f, 0.26f, 0.3f) : picked ? PickedRowColor : RowColor;
        if (picked)
        {
            Shapes.RoundRect(this, box.Grow(7), 20, greyed ? Grey : Glow);
            // A pointer on the left of the picked row
            Triangle(new Vector2(box.Position.X - 30, box.GetCenter().Y), 1, greyed ? Grey : Glow);
        }
        Shapes.RoundRect(this, box, 16, fill);
        return picked ? new Rect2(box.Position.X - 44, box.Position.Y - 7, box.Size.X + 51, box.Size.Y + 14) : box;
    }

    /// <summary>The seven big postcards. Returns the area they cover.</summary>
    Rect2 DrawCards(Font font)
    {
        PicturesDrawn = QuestionMarksDrawn = 0;
        HereRibbonOn = -1;
        var area = CardRect(0);
        for (int i = 0; i < Worlds.All.Length; i++)
        {
            var card = CardRect(i);
            bool canPick = CanWarpTo.Contains(i) || i == Here;
            if (i == Picked && !WarpGreyed)
            {
                // The picked world glows (brighter when the WORLD WARP row is picked)
                Shapes.RoundRect(this, card.Grow(6), 12, Row == 1 ? Glow : new Color(1, 1, 1, 0.5f));
                area = area.Merge(card.Grow(6));
            }
            Shapes.RoundRect(this, card, 10, i == Here ? Gold : Ink); // the frame (gold where you are)
            var inside = card.Grow(-Frame);

            if (canPick)
            {
                // The same little picture as on the menu postcards, scaled up
                DrawSetTransform(inside.Position, 0, new Vector2(CardScale, CardScale));
                WorldPostcards.DrawLittleWorld(this, new Rect2(Vector2.Zero, PictureSize), Worlds.All[i]);
                DrawSetTransform(Vector2.Zero, 0, Vector2.One);
                PicturesDrawn++;
            }
            else
            {
                DrawRect(inside, Grey);
                Words(font, "?", inside.GetCenter().X, inside.GetCenter().Y + 20, 56, Colors.White);
                QuestionMarksDrawn++;
            }

            if (WarpGreyed) DrawRect(inside, new Color(0.3f, 0.3f, 0.35f, 0.65f)); // greyed out: beat the boss first!

            if (i == Here)
            {
                // A gold ribbon across the bottom of the card: YOU ARE HERE
                var ribbon = new Rect2(inside.Position.X, inside.End.Y - 26, inside.Size.X, 26);
                Shapes.RoundRect(this, ribbon, 6, Gold);
                Words(font, "YOU ARE HERE", ribbon.GetCenter().X, ribbon.GetCenter().Y + 16 * 0.36f, 16, Ink, outline: false);
                HereRibbonOn = i;
            }
            area = area.Merge(card);
        }
        return area;
    }

    /// <summary>
    /// Words with their middle at x, standing on "baseline" (white letters with a dark outline, unless outline is false).
    /// We measure them first, so they're exactly in the middle. Returns the area they cover.
    /// </summary>
    Rect2 Words(Font font, string text, float x, float baseline, int size, Color color, bool outline = true)
    {
        var textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, size);
        var at = new Vector2(x - textSize.X / 2, baseline);
        if (outline) DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, size / 5, Ink);
        DrawString(font, at, text, HorizontalAlignment.Left, -1, size, color);
        return new Rect2(at.X, baseline - font.GetAscent(size), textSize.X, textSize.Y);
    }

    /// <summary>A little triangle pointing left (-1) or right (1), with its middle at "at".</summary>
    void Triangle(Vector2 at, float direction, Color color)
    {
        DrawColoredPolygon(new[]
        {
            at + new Vector2(14 * direction, 0), at + new Vector2(-10 * direction, -14), at + new Vector2(-10 * direction, 14),
        }, color);
    }

    /// <summary>WARP! A white flash that fades away, with rainbow rings rushing out from the middle of the screen.</summary>
    void DrawFlash()
    {
        DrawRect(new Rect2(0, 0, 1280, 720), new Color(1, 1, 1, 0.75f * Flash * Flash));
        float grown = 1 - Flash; // 0 at the start, 1 at the end
        for (int i = 0; i < Rainbow.Length; i++)
        {
            float radius = 60 + grown * 900 + i * 40;
            DrawArc(new Vector2(640, 360), radius, 0, Mathf.Tau, 64, new Color(Rainbow[i], Flash), 22);
        }
    }
}
