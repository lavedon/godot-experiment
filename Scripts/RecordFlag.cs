using Godot;

/// <summary>What kind of flag stands on the road (see RecordFlag).</summary>
public enum FlagKind
{
    MyRecord,  // gold, with a white star: the farthest YOU ever rode
    LastTime,  // small and silver: where your last ride ended
    Other,     // everybody else's farthest ride, each in their own color, like "DAD 812 m"
}

/// <summary>
/// A flag on the road, standing where somebody's best ride ended (see Main.Family.cs).
/// A tall pole, a cloth that waves in the wind, and words above it like "DAD 812 m".
/// Its Position is the foot of the pole, on the ground. When Bolt-E zooms past, it tips over (with X eyes!).
/// Everything is drawn with code.
/// </summary>
public partial class RecordFlag : Node2D
{
    // ---- Try changing these! ----
    // How tall the pole is (pixels). The cloth, the star, the X eyes, the words and the confetti all go up and down with it.
    // (It isn't a "const" like the others, so the self-test can try a taller pole too.)
    public static float PoleHeight = 230f;
    const float LastTimeSize = 0.7f;      // the LAST TIME flag is a bit smaller
    const float FallenOver = -1.35f;      // how far it tips over (in "radians": -1.35 is almost lying down, backwards)
    const float FallSeconds = 0.5f;       // how long tipping over takes
    const int LabelSize = 22;             // how big the words above the flag are

    static readonly Color PoleGrey = new(0.8f, 0.8f, 0.85f);
    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);

    public string Label = "";             // like "DAD", "YOUR RECORD" or "LAST TIME"
    public int Meters;                    // where it stands on the road
    public FlagKind Kind;
    public Color FlagColor = Colors.White;
    public bool Passed;                   // true once Bolt-E zoomed past it
    public float LabelLift;               // flags close together lift their words higher, so the words don't overlap

    float time;                           // seconds, for the waving cloth
    Tween? tipping;
    readonly Vector2[] cloth = new Vector2[6]; // the cloth's corners (made once, changed every frame as it waves)
    string words = "";                    // the words above the flag, and how big they are: worked out once, not
    Vector2 wordsSize;                    // every frame (only again if the Label or the Meters change)...
    string? wordsLabel;                   // ...so we remember which Label and Meters they were made from
    int wordsMeters;

    // ---- What was drawn last time (the self-test reads these) ----
    public bool StarDrawn { get; private set; }
    public bool XEyesDrawn { get; private set; }
    public float DrawnSize { get; private set; }
    public Rect2 LabelRect { get; private set; }
    /// <summary>Where the top of the pole (and the top of the cloth) was drawn: pixels above the foot of the pole (so it's negative).</summary>
    public float ClothTopY { get; private set; }
    /// <summary>Where the star or the X eyes were drawn (the middle of the cloth), like ClothTopY.</summary>
    public float StarY { get; private set; }

    /// <summary>The words above the flag, like "DAD 812 m".</summary>
    public string Words => $"{Label} {Meters} m";

    /// <summary>How big this flag is drawn: the LAST TIME flag is a bit smaller.</summary>
    public float Size => Kind == FlagKind.LastTime ? LastTimeSize : 1f;

    /// <summary>The top of the pole, where the cloth is (the confetti flies from here when Bolt-E zooms past).</summary>
    public Vector2 PoleTop => Position + new Vector2(0, -PoleHeight * Size);

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    /// <summary>Bolt-E zoomed past: the flag tips over backwards, with a little bounce at the end.</summary>
    public void KnockOver()
    {
        tipping?.Kill();
        tipping = CreateTween();
        tipping.TweenProperty(this, Node2D.PropertyName.Rotation.ToString(), FallenOver, FallSeconds)
               .SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
    }

    public override void _Draw()
    {
        float size = Size;
        DrawnSize = size;
        DrawSetTransform(Vector2.Zero, 0, Vector2.One * size);

        // The pole. Everything else on the flag is measured from the top of it (so it all goes up with a taller pole).
        float top = -PoleHeight;
        DrawLine(Vector2.Zero, new Vector2(0, top), PoleGrey, 5f);
        ClothTopY = top * size;

        // The cloth (34 pixels tall), waving in the wind (each corner bobs up and down a little, at its own moment)
        float s1 = Mathf.Sin(6 * time + 1), s2 = Mathf.Sin(6 * time + 2), s3 = Mathf.Sin(6 * time + 3);
        cloth[0] = new Vector2(0, top);
        cloth[1] = new Vector2(36, top + 4 * s1);
        cloth[2] = new Vector2(72, top + 2 + 6 * s2);
        cloth[3] = new Vector2(72, top + 34 + 6 * s3);
        cloth[4] = new Vector2(36, top + 32 + 4 * s1);
        cloth[5] = new Vector2(0, top + 34);
        DrawColoredPolygon(cloth, FlagColor);

        // YOUR RECORD has a white star. Somebody else's flag gets X eyes when Bolt-E zooms past it! (In the middle of the cloth.)
        float middle = top + 17;
        StarY = middle * size;
        StarDrawn = Kind == FlagKind.MyRecord;
        if (StarDrawn) Shapes.Star(this, new Vector2(30, middle), 9, 0, Colors.White);
        XEyesDrawn = Kind == FlagKind.Other && Passed;
        if (XEyesDrawn)
        {
            Shapes.XEye(this, new Vector2(24, middle), 5, Ink);
            Shapes.XEye(this, new Vector2(44, middle), 5, Ink);
        }

        // The words, right above the pole. We measure them, so they're exactly in the middle (and never cut off).
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        var font = ThemeDB.FallbackFont;
        if (Label != wordsLabel || Meters != wordsMeters) // (only the first time, or if the words ever change)
        {
            wordsLabel = Label;
            wordsMeters = Meters;
            words = Words;
            wordsSize = font.GetStringSize(words, HorizontalAlignment.Left, -1, LabelSize);
        }
        var at = new Vector2(-wordsSize.X / 2, (top - 15) * size - LabelLift); // (the bottom of the letters, 15 pixels above the pole)
        DrawStringOutline(font, at, words, HorizontalAlignment.Left, -1, LabelSize, 6, Ink);
        DrawString(font, at, words, HorizontalAlignment.Left, -1, LabelSize, Colors.White);
        LabelRect = new Rect2(at.X, at.Y - font.GetAscent(LabelSize), wordsSize.X, wordsSize.Y);
    }
}
